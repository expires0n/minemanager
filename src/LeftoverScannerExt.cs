using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MineManager
{
    /// <summary>
    /// Scheduled-task helpers: the task definition lives in %SystemRoot%\System32\Tasks,
    /// but removal goes through schtasks.exe (the supported mechanism). TaskCache and the
    /// Tasks folder itself are never touched by hand.
    /// </summary>
    internal static class TaskTools
    {
        public static string Root
        {
            get { return Path.Combine(Paths.Win, "System32\\Tasks"); }
        }

        /// <summary>Task name ("\Vendor\Name") to its definition file, or "".</summary>
        public static string XmlPath(string taskPath)
        {
            try
            {
                string rel = (taskPath == null ? "" : taskPath.Trim().Trim('\\')).Replace('/', '\\');
                if (rel.Length == 0) return "";
                string full = Path.Combine(Root, rel);
                return File.Exists(full) ? full : "";
            }
            catch { return ""; }
        }

        public static bool Delete(string taskPath)
        {
            try
            {
                string t = taskPath == null ? "" : taskPath.Trim();
                if (!t.StartsWith("\\", StringComparison.Ordinal)) t = "\\" + t;
                RunResult r = ProcRunner.Run(Path.Combine(Paths.Sys, "schtasks.exe"),
                    "/delete /tn \"" + t + "\" /f", 30, null, null);
                return r.ExitCode == 0;
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// Second pass of the leftover scan: registry integrations (App Paths, ProgID,
    /// shell extensions, COM, firewall rules, compatibility layers, PATH), scheduled
    /// tasks and loose files. "Wide scan, narrow delete": only verified, product-owned
    /// targets are offered, and anything that could belong to another product is left
    /// unchecked or skipped entirely (Windows Installer cache, Driver Store, Prefetch,
    /// WER history, MuiCache, UserChoice, Winlogon, MSI product records).
    /// </summary>
    internal static partial class LeftoverScanner
    {
        private static bool Under(string path, string root)
        {
            if (root.Length == 0 || path == null || path.Length == 0) return false;
            return path.IndexOf(root, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // --------------------------------------------------- registry integrations

        private static void ScanRegistryExtras(InstalledProgram p, Keywords kw,
            List<LeftoverItem> found, HashSet<string> seen)
        {
            string install = kw.InstallDir;

            // ---- App Paths: the default value must point into the product folder.
            string[] appPathRoots = new string[]
            {
                @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths",
                @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths"
            };
            foreach (string root in appPathRoots)
                ScanLeafCommands(root, install, kw, found, seen, "High", true);

            // ---- Classes\Applications\<app.exe>: resolved command targets.
            string[] appRoots = new string[]
            {
                @"HKLM\SOFTWARE\Classes\Applications",
                @"HKCU\SOFTWARE\Classes\Applications"
            };
            foreach (string root in appRoots)
                ScanLeafCommands(root, install, kw, found, seen, "Medium", true);

            // ---- ProgID: only when its open command really references the product.
            string[] classRoots = new string[]
            {
                @"HKLM\SOFTWARE\Classes",
                @"HKCU\SOFTWARE\Classes"
            };
            foreach (string root in classRoots)
                ScanProgIds(root, install, kw, found, seen);

            // ---- FileExts\...\OpenWithProgids values (UserChoice / OpenWithList skipped).
            ScanOpenWithProgids(kw, found, seen);

            // ---- Context menu handlers + Approved list (leaf only).
            ScanShellExtensions(kw, found, seen);

            // ---- COM: InprocServer32 / LocalServer32 paths inside the product folder.
            if (install.Length > 0) ScanComServers(install, found, seen);

            // ---- Compatibility layers for product executables.
            string[] layerRoots = new string[]
            {
                @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers",
                @"HKCU\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"
            };
            foreach (string root in layerRoots)
            {
                try
                {
                    using (RegistryKey k = Reg.Open(root))
                    {
                        if (k == null) continue;
                        string[] names = k.GetValueNames();
                        for (int i = 0; i < names.Length; i++)
                            if (Under(names[i], install))
                                AddValue(found, seen, root, names[i], "High");
                    }
                }
                catch { }
            }

            // ---- Firewall rules whose application path is inside the product folder.
            ScanFirewallRules(install, found, seen);

            // ---- PATH entries equal to the product folder.
            if (install.Length > 0)
            {
                string[] pathRoots = new string[]
                {
                    @"HKCU\Environment",
                    @"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment"
                };
                foreach (string root in pathRoots)
                {
                    try
                    {
                        using (RegistryKey k = Reg.Open(root))
                        {
                            if (k == null) continue;
                            object raw = k.GetValue("Path");
                            if (raw is string[])
                            {
                                string[] parts = (string[])raw;
                                for (int i = 0; i < parts.Length; i++)
                                    if (SameDir(parts[i], install))
                                        AddPathEntry(found, seen, root, parts[i]);
                            }
                            else if (raw is string)
                            {
                                string[] parts = ((string)raw).Split(';');
                                for (int i = 0; i < parts.Length; i++)
                                    if (SameDir(parts[i], install))
                                        AddPathEntry(found, seen, root, parts[i]);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private static bool SameDir(string a, string b)
        {
            if (a == null || b == null) return false;
            return string.Equals(a.Trim().TrimEnd('\\'), b.Trim().TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Leaf keys whose default/verb command points into the product folder.</summary>
        private static void ScanLeafCommands(string root, string install, Keywords kw,
            List<LeftoverItem> found, HashSet<string> seen, string confidence, bool requirePath)
        {
            try
            {
                using (RegistryKey rk = Reg.Open(root))
                {
                    if (rk == null) return;
                    string[] subs = rk.GetSubKeyNames();
                    for (int i = 0; i < subs.Length; i++)
                    {
                        string full = root + "\\" + subs[i];
                        bool match = false;
                        try
                        {
                            using (RegistryKey k = rk.OpenSubKey(subs[i]))
                            {
                                if (k == null) continue;
                                string cmd = Reg.Str(k, "");
                                if (cmd.Length == 0)
                                {
                                    using (RegistryKey verb = k.OpenSubKey("shell\\open\\command"))
                                        cmd = verb == null ? "" : Reg.Str(verb, "");
                                }
                                if (install.Length > 0 && Under(cmd, install)) match = true;
                                if (!match && !requirePath)
                                {
                                    int hits;
                                    match = ContainsProduct(Flat(subs[i]), kw, out hits);
                                }
                            }
                        }
                        catch { }
                        if (match) AddReg(found, seen, full, confidence);
                    }
                }
            }
            catch { }
        }

        private static void ScanProgIds(string root, string install, Keywords kw,
            List<LeftoverItem> found, HashSet<string> seen)
        {
            if (install.Length == 0) return;
            try
            {
                using (RegistryKey rk = Reg.Open(root))
                {
                    if (rk == null) return;
                    string[] subs = rk.GetSubKeyNames();
                    for (int i = 0; i < subs.Length; i++)
                    {
                        int hits;
                        // cheap filter first: only open keys that can belong to the product
                        if (!ContainsProduct(Flat(subs[i]), kw, out hits)) continue;
                        string full = root + "\\" + subs[i];
                        try
                        {
                            using (RegistryKey cmd = rk.OpenSubKey(subs[i] + "\\shell\\open\\command"))
                            {
                                if (cmd == null) continue;
                                string c = Reg.Str(cmd, "");
                                if (Under(c, install)) AddReg(found, seen, full, "High");
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void ScanOpenWithProgids(Keywords kw, List<LeftoverItem> found, HashSet<string> seen)
        {
            string root = @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts";
            try
            {
                using (RegistryKey rk = Reg.Open(root))
                {
                    if (rk == null) return;
                    string[] exts = rk.GetSubKeyNames();
                    for (int i = 0; i < exts.Length; i++)
                    {
                        try
                        {
                            using (RegistryKey ow = rk.OpenSubKey(exts[i] + "\\OpenWithProgids"))
                            {
                                if (ow == null) continue;
                                string[] names = ow.GetValueNames();
                                for (int j = 0; j < names.Length; j++)
                                {
                                    int hits;
                                    if (!ContainsProduct(Flat(names[j]), kw, out hits)) continue;
                                    // name-only evidence: shown, but not preselected
                                    AddValue(found, seen, root + "\\" + exts[i] + "\\OpenWithProgids",
                                        names[j], "Low");
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private static void ScanShellExtensions(Keywords kw, List<LeftoverItem> found, HashSet<string> seen)
        {
            string[] roots = new string[]
            {
                @"HKLM\SOFTWARE\Classes\*\shellex\ContextMenuHandlers",
                @"HKLM\SOFTWARE\Classes\Directory\shellex\ContextMenuHandlers",
                @"HKLM\SOFTWARE\Classes\Folder\shellex\ContextMenuHandlers",
                @"HKLM\SOFTWARE\Classes\Drive\shellex\ContextMenuHandlers",
                @"HKCU\SOFTWARE\Classes\*\shellex\ContextMenuHandlers",
                @"HKCU\SOFTWARE\Classes\Directory\shellex\ContextMenuHandlers"
            };
            foreach (string root in roots)
            {
                try
                {
                    using (RegistryKey rk = Reg.Open(root))
                    {
                        if (rk == null) continue;
                        string[] subs = rk.GetSubKeyNames();
                        for (int i = 0; i < subs.Length; i++)
                        {
                            int hits;
                            if (!ContainsProduct(Flat(subs[i]), kw, out hits)) continue;
                            AddReg(found, seen, root + "\\" + subs[i], "Medium");
                        }
                    }
                }
                catch { }
            }

            // Approved list: the value NAME is the CLSID; only the description is matched.
            string approved = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved";
            try
            {
                using (RegistryKey k = Reg.Open(approved))
                {
                    if (k == null) return;
                    string[] names = k.GetValueNames();
                    for (int i = 0; i < names.Length; i++)
                    {
                        string desc = Reg.Str(k, names[i]);
                        int hits;
                        if (desc.Length > 0 && ContainsProduct(Flat(desc), kw, out hits))
                            AddValue(found, seen, approved, names[i], "Low");
                    }
                }
            }
            catch { }
        }

        private static void ScanComServers(string install, List<LeftoverItem> found, HashSet<string> seen)
        {
            string[] roots = new string[]
            {
                @"HKLM\SOFTWARE\Classes\CLSID",
                @"HKLM\SOFTWARE\WOW6432Node\Classes\CLSID",
                @"HKCU\SOFTWARE\Classes\CLSID"
            };
            foreach (string root in roots)
            {
                try
                {
                    using (RegistryKey rk = Reg.Open(root))
                    {
                        if (rk == null) continue;
                        string[] subs = rk.GetSubKeyNames();
                        for (int i = 0; i < subs.Length; i++)
                        {
                            if (i > 6000) break;
                            try
                            {
                                using (RegistryKey c = rk.OpenSubKey(subs[i]))
                                {
                                    if (c == null) continue;
                                    string inproc = "";
                                    string local = "";
                                    using (RegistryKey s = c.OpenSubKey("InprocServer32")) inproc = Reg.Str(s, "");
                                    using (RegistryKey s = c.OpenSubKey("LocalServer32")) local = Reg.Str(s, "");
                                    if (Under(inproc, install) || Under(local, install))
                                        AddReg(found, seen, root + "\\" + subs[i], "High");
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
        }

        private static void ScanFirewallRules(string install, List<LeftoverItem> found, HashSet<string> seen)
        {
            if (install.Length == 0) return;
            string root = @"HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\FirewallRules";
            try
            {
                using (RegistryKey k = Reg.Open(root))
                {
                    if (k == null) return;
                    string[] names = k.GetValueNames();
                    for (int i = 0; i < names.Length; i++)
                    {
                        string data = Reg.Str(k, names[i]);
                        if (data.Length == 0) continue;
                        // rule data is |key=value|key=value|…; only rules with an
                        // application path inside the product folder are matched.
                        int idx = data.IndexOf("|app=", StringComparison.OrdinalIgnoreCase);
                        if (idx < 0) continue;
                        int end = data.IndexOf('|', idx + 5);
                        string app = end > idx
                            ? data.Substring(idx + 5, end - (idx + 5))
                            : data.Substring(idx + 5);
                        if (Under(app, install)) AddValue(found, seen, root, names[i], "Medium");
                    }
                }
            }
            catch { }
        }

        private static void AddValue(List<LeftoverItem> found, HashSet<string> seen,
            string keyPath, string valueName, string confidence)
        {
            string id = keyPath + "\\" + valueName;
            if (seen.Contains(id)) return;
            LeftoverItem it = new LeftoverItem();
            it.Kind = LeftoverKind.RegValue;
            it.Path = keyPath;
            it.ValueName = valueName;
            it.Confidence = confidence;
            it.Checked = confidence != "Low";
            found.Add(it);
            seen.Add(id);
        }

        private static void AddPathEntry(List<LeftoverItem> found, HashSet<string> seen,
            string keyPath, string entry)
        {
            string id = keyPath + "\\Path=" + entry;
            if (seen.Contains(id)) return;
            LeftoverItem it = new LeftoverItem();
            it.Kind = LeftoverKind.PathEntry;
            it.Path = keyPath;
            it.ValueName = entry;
            it.Confidence = "High";
            it.Checked = true;
            found.Add(it);
            seen.Add(id);
        }

        // -------------------------------------------------------- scheduled tasks

        private static void ScanTasks(InstalledProgram p, Keywords kw,
            List<LeftoverItem> found, HashSet<string> seen)
        {
            string root = TaskTools.Root;

            // The Tasks folder can be listed only when elevated. Try the folder walk
            // first; without enumeration rights fall back to "schtasks /query".
            bool enumerable = false;
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.GetDirectories(root);
                    enumerable = true;
                }
            }
            catch { }

            if (enumerable)
            {
                int scanned = 0;
                ScanTaskDir(root, "", kw, found, seen, ref scanned);
                return;
            }

            try
            {
                RunResult r = ProcRunner.Run(Path.Combine(Paths.Sys, "schtasks.exe"),
                    "/query /fo csv /nh", 60, null, null);
                List<string> lines = ProcRunner.Lines(r.Output);
                for (int i = 0; i < lines.Count; i++)
                {
                    string name = CsvFirst(lines[i]);
                    if (name.Length == 0) continue;
                    if (name.StartsWith("\\Microsoft\\", StringComparison.OrdinalIgnoreCase)) continue;
                    string file = TaskTools.XmlPath(name);
                    if (file.Length == 0) continue;
                    string text;
                    try { text = File.ReadAllText(file); }
                    catch { continue; }
                    MatchTaskFile(name, text, kw, found, seen);
                }
            }
            catch { }
        }

        private static string CsvFirst(string line)
        {
            if (line == null) return "";
            string s = line.Trim();
            if (s.Length < 2) return "";
            if (s[0] == '"')
            {
                int end = s.IndexOf('"', 1);
                if (end <= 1) return "";
                return s.Substring(1, end - 1);
            }
            int comma = s.IndexOf(',');
            return comma < 0 ? s : s.Substring(0, comma);
        }

        private static void MatchTaskFile(string taskName, string text, Keywords kw,
            List<LeftoverItem> found, HashSet<string> seen)
        {
            if (seen.Contains("task" + taskName)) return;
            string flatText = Flat(text.Length > 8192 ? text.Substring(0, 8192) : text);
            bool byPath = kw.InstallDir.Length > 0 &&
                text.IndexOf(kw.InstallDir, StringComparison.OrdinalIgnoreCase) >= 0;
            int hits;
            bool byName = ContainsProduct(Flat(taskName), kw, out hits) ||
                          ContainsProduct(flatText, kw, out hits);
            if (!byPath && !byName) return;

            LeftoverItem it = new LeftoverItem();
            it.Kind = LeftoverKind.Task;
            it.Path = taskName;
            it.Confidence = byPath ? "High" : "Low";
            it.Checked = byPath;
            found.Add(it);
            seen.Add("task" + taskName);
        }

        private static void ScanTaskDir(string dir, string rel, Keywords kw,
            List<LeftoverItem> found, HashSet<string> seen, ref int scanned)
        {
            if (scanned > 4000) return;
            try
            {
                string[] subs = Directory.GetDirectories(dir);
                for (int i = 0; i < subs.Length; i++)
                {
                    string name = Path.GetFileName(subs[i]);
                    // Microsoft's task tree is system territory — never a delete target.
                    if (rel.Length == 0 && string.Equals(name, "Microsoft", StringComparison.OrdinalIgnoreCase))
                        continue;
                    ScanTaskDir(subs[i], rel + "\\" + name, kw, found, seen, ref scanned);
                }

                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    scanned++;
                    if (scanned > 4000) return;
                    string taskName = rel + "\\" + Path.GetFileName(files[i]);
                    if (seen.Contains("task" + taskName)) continue;
                    string text;
                    try
                    {
                        FileInfo fi = new FileInfo(files[i]);
                        if (fi.Length > 262144) continue;
                        text = File.ReadAllText(files[i]);
                    }
                    catch { continue; }
                    MatchTaskFile(taskName, text, kw, found, seen);
                }
            }
            catch { }
        }

        // -------------------------------------------------------------- loose files

        /// <summary>
        /// Loose files that mention the product by name: leftover .reg exports, .ini,
        /// .log and .xml dumps sitting in Program Files, ProgramData, Public or the
        /// user folders. Folders were handled by the folder pass.
        /// </summary>
        private static void ScanLooseFiles(Keywords kw, List<LeftoverItem> found, HashSet<string> seen)
        {
            string publicDir = Environment.GetEnvironmentVariable("PUBLIC");
            string[] roots = new string[]
            {
                Paths.ProgramFiles, Paths.ProgramFilesX86, Paths.CommonFiles,
                publicDir, Paths.Desktop, Paths.Documents
            };
            string[] exts = new string[]
            {
                ".reg", ".ini", ".log", ".xml", ".txt", ".dat", ".bak", ".old", ".tmp", ".json"
            };

            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                string[] files;
                try { files = Directory.GetFiles(root); }
                catch { continue; }
                for (int i = 0; i < files.Length; i++)
                {
                    string name;
                    try { name = Path.GetFileNameWithoutExtension(files[i]); }
                    catch { continue; }
                    bool wanted = false;
                    for (int e = 0; e < exts.Length; e++)
                        if (string.Equals(Path.GetExtension(files[i]), exts[e], StringComparison.OrdinalIgnoreCase))
                        { wanted = true; break; }
                    if (!wanted) continue;

                    int hits;
                    if (!ContainsProduct(Flat(name), kw, out hits)) continue;
                    if (seen.Contains(files[i])) continue;

                    LeftoverItem it = new LeftoverItem();
                    it.Kind = LeftoverKind.File;
                    it.Path = files[i];
                    try { it.Size = new FileInfo(files[i]).Length; }
                    catch { }
                    // loose files may be user documents — review before deleting
                    it.Confidence = "Low";
                    it.Checked = false;
                    found.Add(it);
                    seen.Add(files[i]);
                }
            }
        }

        // ------------------------------------------------------- backup management

        /// <summary>True when the target still exists after a delete attempt.</summary>
        public static bool StillThere(LeftoverItem it)
        {
            if (it == null) return false;
            try
            {
                switch (it.Kind)
                {
                    case LeftoverKind.Folder:
                    case LeftoverKind.File:
                        return Paths.Exists(it.Path);
                    case LeftoverKind.RegKey:
                        return Reg.Exists(it.Path);
                    case LeftoverKind.RegValue:
                        using (RegistryKey k = Reg.Open(it.Path))
                        {
                            if (k == null) return false;
                            string[] names = k.GetValueNames();
                            for (int i = 0; i < names.Length; i++)
                                if (string.Equals(names[i], it.ValueName, StringComparison.OrdinalIgnoreCase))
                                    return true;
                            return false;
                        }
                    case LeftoverKind.Task:
                        return TaskTools.XmlPath(it.Path).Length > 0;
                    case LeftoverKind.PathEntry:
                        using (RegistryKey k = Reg.Open(it.Path))
                        {
                            if (k == null) return false;
                            object raw = k.GetValue("Path");
                            return raw != null && raw.ToString().IndexOf(it.ValueName,
                                StringComparison.OrdinalIgnoreCase) >= 0;
                        }
                    default:
                        return false;
                }
            }
            catch { return false; }
        }

        internal class BackupEntry
        {
            public string Name = "";
            public string FullPath = "";
            public long Size;
            public DateTime Time;
            public int Files;

            public string SizeText { get { return Fmt.Size(Size); } }
            public string TimeText { get { return Time.ToString("yyyy-MM-dd HH:mm", L.Culture); } }
        }

        public static string BackupRoot
        {
            get
            {
                try
                {
                    return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath),
                        "MineManager-backup");
                }
                catch { return ""; }
            }
        }

        /// <summary>Every backup package MineManager created (.reg exports, task XML).</summary>
        public static List<BackupEntry> ListBackups()
        {
            List<BackupEntry> list = new List<BackupEntry>();
            string root = BackupRoot;
            if (root.Length == 0 || !Directory.Exists(root)) return list;
            string[] dirs;
            try { dirs = Directory.GetDirectories(root); }
            catch { return list; }
            for (int i = 0; i < dirs.Length; i++)
            {
                BackupEntry e = new BackupEntry();
                e.FullPath = dirs[i];
                try
                {
                    e.Name = Path.GetFileName(dirs[i]);
                    DirectoryInfo di = new DirectoryInfo(dirs[i]);
                    e.Time = di.CreationTime;
                    FileInfo[] files = di.GetFiles();
                    e.Files = files.Length;
                    for (int j = 0; j < files.Length; j++) e.Size += files[j].Length;
                }
                catch { }
                list.Add(e);
            }
            list.Sort(delegate(BackupEntry a, BackupEntry b) { return b.Time.CompareTo(a.Time); });
            return list;
        }

        /// <summary>
        /// Deletes whole backup packages. This ends MineManager-based restore for them —
        /// that is exactly the "nothing is left behind" step, and it is never automatic.
        /// </summary>
        public static long PurgeBackups(List<string> names, out int errors)
        {
            errors = 0;
            long freed = 0;
            string root = BackupRoot;
            if (root.Length == 0 || names == null) return 0;
            for (int i = 0; i < names.Count; i++)
            {
                string target = Path.Combine(root, names[i]);
                try
                {
                    if (!Directory.Exists(target)) continue;
                    int f;
                    long size = FileTools.DirSize(target, out f);
                    Directory.Delete(target, true);
                    freed += size;
                }
                catch { errors++; }
            }
            return freed;
        }
    }
}