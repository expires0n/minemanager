using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MineManager
{
    internal enum LeftoverKind { Folder, File, RegKey, RegValue }

    internal class LeftoverItem
    {
        public LeftoverKind Kind;
        public string Path = "";
        public string ValueName = "";
        public long Size;
        public string Confidence = "Medium";
        public bool Checked = true;

        public string TypeText
        {
            get
            {
                switch (Kind)
                {
                    case LeftoverKind.Folder: return L.T("Folder");
                    case LeftoverKind.File: return L.T("File");
                    case LeftoverKind.RegValue: return L.T("Registry value");
                    default: return L.T("Registry key");
                }
            }
        }

        public string Where
        {
            get
            {
                if (Kind == LeftoverKind.RegValue)
                    return Path + "  \\  " + ValueName;
                return Path;
            }
        }

        public string SizeText
        {
            get
            {
                if (Kind == LeftoverKind.RegKey || Kind == LeftoverKind.RegValue) return "-";
                return Fmt.Size(Size);
            }
        }
    }

    internal static class LeftoverScanner
    {
        private static readonly string[] Stop =
        {
            "manager","managers","download","downloads","downloader","install","installer","installation",
            "setup","update","updater","updates","upgrade","driver","drivers","service","services","helper",
            "tool","tools","utility","utilities","app","apps","application","program","programs","software",
            "system","systems","network","internet","security","antivirus","player","media","video","audio",
            "studio","studios","viewer","reader","editor","browser","launcher","client","server","desktop",
            "widget","gadget","plugin","plugins","extension","addon","addons","engine","runtime","framework",
            "library","sdk","module","component","components","package","packages","content","folder","cache",
            "temp","support","agent","monitor","control","center","centre","panel","settings","config",
            "configuration","premium","pro","lite","free","trial","demo","beta","alpha","rc","edition",
            "version","ver","portable","win","windows","microsoft","and","the","for","with","inc","ltd",
            "llc","corp","corporation","technologies","technology","group","labs","team","co","company",
            "gmbh","limited","net","www","com","org","x86","x64","bit","amd64","official","website",
            "online","cloud","mobile","web","site","home","page","full","all","plus","max","super","ultra",
            "mega","new","old","classic","global","world","worldwide","international","update","release",
            "latest","stable","bin","data","files","file","pbc","plc","kft","gmbh","srl","spa","ag","bv","nv"
        };

        private static readonly string[] SkipFolderNames =
        {
            "microsoft","windows","common files","package cache","packages","temp","programs","windowsapps",
            "internet explorer","windows defender","windows nt","system32","syswow64","reference assemblies",
            "microsoft.net","windows kits","msbuild","dotnet","powershell","windowspowershell",
            "modifiablewindowsapps","installed updates","windows mail","windows media player","uninstall information",
            "windowsphoto viewer","windows portable devices","windows sidebar","windowsapps"
        };

        private static readonly string[] NoDescend =
        {
            "microsoft","classes","policies","clients","registeredapplications","wow6432node","system",
            "secure","rpc","odbc","cryptography","microsoft.net","windows"
        };

        private static readonly string[] SkipRegRoots =
        {
            "microsoft","classes","policies","clients","registeredapplications","wow6432node","microsoft.net",
            "windows","intel","nvidia","realtek","od","odbc","rpc","secure","system","syswow64","wow6432node",
            "mozillaplugins","cryptography","google","amdfendr"
        };

        private class Keywords
        {
            public List<string> Strong = new List<string>();
            public string FlatName = "";
            public string InstallDir = "";
            public string InstallFolder = "";
        }

        private static string Flat(string s)
        {
            if (s == null) return "";
            StringBuilder sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = char.ToLowerInvariant(s[i]);
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        private static void AddTokens(string text, List<string> into)
        {
            if (string.IsNullOrEmpty(text)) return;
            StringBuilder cur = new StringBuilder();
            for (int i = 0; i <= text.Length; i++)
            {
                char c = i < text.Length ? text[i] : ' ';
                if (char.IsLetterOrDigit(c)) { cur.Append(char.ToLowerInvariant(c)); continue; }
                if (cur.Length >= 3)
                {
                    string t = cur.ToString();
                    bool stop = false;
                    for (int j = 0; j < Stop.Length; j++) if (Stop[j] == t) { stop = true; break; }
                    bool numeric = true;
                    for (int j = 0; j < t.Length; j++) if (!char.IsDigit(t[j])) { numeric = false; break; }
                    if (!stop && !numeric && !into.Contains(t)) into.Add(t);
                }
                cur.Length = 0;
            }
        }

        private static Keywords BuildKeywords(InstalledProgram p)
        {
            Keywords k = new Keywords();
            AddTokens(p.DisplayName, k.Strong);
            AddTokens(p.Publisher, k.Strong);
            k.FlatName = Flat(p.DisplayName);

            if (!string.IsNullOrEmpty(p.InstallLocation) && Directory.Exists(p.InstallLocation))
            {
                k.InstallDir = p.InstallLocation.TrimEnd('\\');
                try { k.InstallFolder = Path.GetFileName(k.InstallDir); } catch { }
                AddTokens(k.InstallFolder, k.Strong);
            }
            return k;
        }

        /// <summary>
        /// Aday adinin en az yarisi kadarini kaplayan bir anahtar kelime ariyoruz.
        /// "manager" gibi genel kelimeler listeye alinmadigi icin yanlis eslesme azalir.
        /// </summary>
        private static bool MatchStrong(string flatCandidate, List<string> tokens, out int hits)
        {
            hits = 0;
            if (flatCandidate.Length == 0) return false;
            for (int i = 0; i < tokens.Count; i++)
            {
                string t = tokens[i];
                if (t.Length < 3) continue;
                if (flatCandidate.IndexOf(t, StringComparison.Ordinal) < 0) continue;
                double cover = (double)t.Length / flatCandidate.Length;
                double need = t.Length >= 5 ? 0.5 : (t.Length == 4 ? 0.62 : 0.75);
                if (cover >= need) { hits++; return true; }
            }
            return hits > 0;
        }

        private static bool ContainsAny(string flatCandidate, List<string> tokens, out int hits)
        {
            return MatchStrong(flatCandidate, tokens, out hits);
        }

        private static bool IsSkippedFolder(string path)
        {
            string name;
            try { name = Path.GetFileName(path.TrimEnd('\\')).ToLowerInvariant(); }
            catch { return true; }
            for (int i = 0; i < SkipFolderNames.Length; i++)
                if (name == SkipFolderNames[i]) return true;
            return false;
        }

        // ---------------------------------------------------------------- folders

        public static List<LeftoverItem> Scan(InstalledProgram p, Action<string> progress)
        {
            List<LeftoverItem> found = new List<LeftoverItem>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Keywords kw = BuildKeywords(p);

            if (progress != null) progress(L.F("Keywords: {0}", string.Join(", ", kw.Strong.ToArray())));

            // 1) install folder itself
            if (!string.IsNullOrEmpty(kw.InstallDir) && Directory.Exists(kw.InstallDir) && !Paths.LooksLikeSystemPath(kw.InstallDir))
            {
                int f;
                LeftoverItem it = new LeftoverItem();
                it.Kind = LeftoverKind.Folder;
                it.Path = kw.InstallDir;
                it.Size = FileTools.DirSize(kw.InstallDir, out f);
                it.Confidence = "High";
                found.Add(it);
                seen.Add(it.Path);
            }

            // 2) folder name scan
            string[] roots = new string[]
            {
                Paths.ProgramFiles, Paths.ProgramFilesX86, Paths.CommonFiles,
                Paths.LocalAppData, Paths.AppData, Paths.Documents
            };

            int scanned = 0;
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                if (progress != null) progress(L.F("Scanning: {0}", root));
                ScanFolders(root, 0, 2, kw, found, seen, ref scanned);
            }

            // 3) registry
            if (progress != null) progress(L.T("Registry scanning..."));
            ScanRegistry(p, kw, found, seen);

            // 4) shortcuts
            if (progress != null) progress(L.T("Scanning shortcuts..."));
            ScanShortcuts(kw, found, seen);

            return found;
        }

        private static void ScanFolders(string dir, int depth, int maxDepth, Keywords kw,
            List<LeftoverItem> found, HashSet<string> seen, ref int scanned)
        {
            if (depth >= maxDepth) return;
            if (scanned > 8000) return;

            string[] subs;
            try { subs = Directory.GetDirectories(dir); }
            catch { return; }

            for (int i = 0; i < subs.Length; i++)
            {
                string sub = subs[i];
                scanned++;
                try
                {
                    FileAttributes at = File.GetAttributes(sub);
                    if ((at & FileAttributes.ReparsePoint) != 0) continue;
                }
                catch { continue; }

                if (IsSkippedFolder(sub)) continue;
                if (Paths.LooksLikeSystemPath(sub)) continue;

                int hits;
                string flat = Flat(Path.GetFileName(sub));
                if (ContainsAny(flat, kw.Strong, out hits) && !seen.Contains(sub))
                {
                    int f;
                    LeftoverItem it = new LeftoverItem();
                    it.Kind = LeftoverKind.Folder;
                    it.Path = sub;
                    it.Size = FileTools.DirSize(sub, out f);
                    it.Confidence = (kw.FlatName.Length > 0 && kw.FlatName.IndexOf(flat, StringComparison.Ordinal) >= 0)
                        ? "High" : "Medium";
                    found.Add(it);
                    seen.Add(sub);
                    continue; // don't dive into a matched folder
                }

                ScanFolders(sub, depth + 1, maxDepth, kw, found, seen, ref scanned);
            }
        }

        // --------------------------------------------------------------- registry

        private static void ScanRegistry(InstalledProgram p, Keywords kw, List<LeftoverItem> found,
            HashSet<string> seen)
        {
            string[][] roots = new string[][]
            {
                new string[] { "HKLM", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "2" },
                new string[] { "HKLM", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", "2" },
                new string[] { "HKCU", @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", "2" },
                new string[] { "HKLM", @"SOFTWARE", "2" },
                new string[] { "HKLM", @"SOFTWARE\WOW6432Node", "2" },
                new string[] { "HKCU", @"SOFTWARE", "2" },
                new string[] { "HKCU", @"SOFTWARE\Classes\Applications", "1" }
            };

            foreach (string[] root in roots)
            {
                string rootPath = root[0] + "\\" + root[1];
                int depth = 2;
                try { depth = int.Parse(root[2]); } catch { }
                ScanRegTree(rootPath, depth, kw, found, seen, p, 0);
            }

            // services
            string svcRoot = @"HKLM\SYSTEM\CurrentControlSet\Services";
            try
            {
                using (RegistryKey k = Reg.Open(svcRoot))
                {
                    if (k != null)
                    {
                        string[] subs = k.GetSubKeyNames();
                        for (int i = 0; i < subs.Length; i++)
                        {
                            string full = svcRoot + "\\" + subs[i];
                            string img = "";
                            try
                            {
                                using (RegistryKey s = k.OpenSubKey(subs[i])) img = Reg.Str(s, "ImagePath");
                            }
                            catch { }

                            bool byPath = kw.InstallDir.Length > 0 &&
                                img.IndexOf(kw.InstallDir, StringComparison.OrdinalIgnoreCase) >= 0;
                            int hits;
                            bool byName = ContainsAny(Flat(subs[i]), kw.Strong, out hits);
                            if (byPath || byName)
                                AddReg(found, seen, full, byPath ? "High" : "Medium");
                        }
                    }
                }
            }
            catch { }

            // run keys
            string[] runRoots = new string[]
            {
                @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
                @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"
            };

            foreach (string rr in runRoots)
            {
                try
                {
                    using (RegistryKey k = Reg.Open(rr))
                    {
                        if (k == null) continue;
                        string[] names = k.GetValueNames();
                        for (int i = 0; i < names.Length; i++)
                        {
                            string data = Reg.Str(k, names[i]);
                            bool byPath = kw.InstallDir.Length > 0 &&
                                data.IndexOf(kw.InstallDir, StringComparison.OrdinalIgnoreCase) >= 0;
                            int hits;
                            bool byName = ContainsAny(Flat(names[i]), kw.Strong, out hits);
                            if (!byPath && !byName) continue;

                            LeftoverItem it = new LeftoverItem();
                            it.Kind = LeftoverKind.RegValue;
                            it.Path = rr;
                            it.ValueName = names[i];
                            it.Confidence = byPath ? "High" : "Medium";
                            string key = rr + "\\" + names[i];
                            if (!seen.Contains(key)) { found.Add(it); seen.Add(key); }
                        }
                    }
                }
                catch { }
            }
        }

        private static void AddReg(List<LeftoverItem> found, HashSet<string> seen, string path, string confidence)
        {
            if (seen.Contains(path)) return;
            LeftoverItem it = new LeftoverItem();
            it.Kind = LeftoverKind.RegKey;
            it.Path = path;
            it.Confidence = confidence;
            found.Add(it);
            seen.Add(path);
        }

        private static void ScanRegTree(string rootPath, int maxDepth, Keywords kw, List<LeftoverItem> found,
            HashSet<string> seen, InstalledProgram p, int depth)
        {
            if (depth > maxDepth) return;
            try
            {
                using (RegistryKey rk = Reg.Open(rootPath))
                {
                    if (rk == null) return;
                    string[] subs = rk.GetSubKeyNames();
                    for (int i = 0; i < subs.Length; i++)
                    {
                        string sub = subs[i];
                        string full = rootPath + "\\" + sub;
                        if (p != null && string.Equals(full, p.RegPath, StringComparison.OrdinalIgnoreCase)) continue;

                        string leafLower = sub.ToLowerInvariant();
                        bool noAdd = Contains(SkipRegRoots, leafLower);
                        bool noDescend = Contains(NoDescend, leafLower);

                        if (!noAdd)
                        {
                            int hits;
                            bool match = MatchStrong(Flat(sub), kw.Strong, out hits);

                            if (!match && rootPath.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                try
                                {
                                    using (RegistryKey k = rk.OpenSubKey(sub))
                                    {
                                        string dn = Reg.Str(k, "DisplayName");
                                        if (dn.Length > 0)
                                        {
                                            string fdn = Flat(dn);
                                            match = (fdn == kw.FlatName && kw.FlatName.Length > 2) ||
                                                    MatchStrong(fdn, kw.Strong, out hits);
                                        }
                                    }
                                }
                                catch { }
                            }

                            if (match)
                            {
                                bool isUninstall = rootPath.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0;
                                AddReg(found, seen, full, isUninstall ? "High" : "Medium");
                                continue;
                            }
                        }

                        if (!noDescend && depth < maxDepth)
                            ScanRegTree(full, maxDepth, kw, found, seen, p, depth + 1);
                    }
                }
            }
            catch { }
        }

        private static bool Contains(string[] list, string value)
        {
            for (int i = 0; i < list.Length; i++) if (list[i] == value) return true;
            return false;
        }

        // -------------------------------------------------------------- shortcuts

        private static void ScanShortcuts(Keywords kw, List<LeftoverItem> found, HashSet<string> seen)
        {
            string[] roots = new string[]
            {
                Paths.StartMenuCommon, Paths.StartMenuUser,
                Paths.Desktop, Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
            };

            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                ScanShortcutDir(root, 0, kw, found, seen);
            }
        }

        private static void ScanShortcutDir(string dir, int depth, Keywords kw, List<LeftoverItem> found,
            HashSet<string> seen)
        {
            if (depth > 3) return;
            try
            {
                string[] files = Directory.GetFiles(dir, "*.lnk");
                for (int i = 0; i < files.Length; i++)
                {
                    string name = Path.GetFileNameWithoutExtension(files[i]);
                    int hits;
                    if (!ContainsAny(Flat(name), kw.Strong, out hits)) continue;
                    if (seen.Contains(files[i])) continue;
                    LeftoverItem it = new LeftoverItem();
                    it.Kind = LeftoverKind.File;
                    it.Path = files[i];
                    try { it.Size = new FileInfo(files[i]).Length; } catch { }
                    it.Confidence = "Medium";
                    found.Add(it);
                    seen.Add(files[i]);
                }
                string[] subs = Directory.GetDirectories(dir);
                for (int i = 0; i < subs.Length; i++) ScanShortcutDir(subs[i], depth + 1, kw, found, seen);
            }
            catch { }
        }

        // --------------------------------------------------------------- deletion

        public static long Delete(List<LeftoverItem> items, Action<string> log, out int errors)
        {
            errors = 0;
            long freed = 0;
            foreach (LeftoverItem it in items)
            {
                if (!it.Checked) continue;
                try
                {
                    if (it.Kind == LeftoverKind.RegKey)
                    {
                        bool ok;
                        Reg.DeleteTree(it.Path, out ok);
                        if (ok) { if (log != null) log(L.F("Deleted: {0}", it.Path)); }
                        else { errors++; if (log != null) log(L.F("Failed: {0}", it.Path)); }
                    }
                    else if (it.Kind == LeftoverKind.RegValue)
                    {
                        bool ok;
                        Reg.DeleteValue(it.Path, it.ValueName, out ok);
                        if (ok) { if (log != null) log(L.F("Deleted: {0} \\ {1}", it.Path, it.ValueName)); }
                        else { errors++; if (log != null) log(L.F("Failed: {0}", it.Path)); }
                    }
                    else
                    {
                        int e;
                        long f = FileTools.DeletePath(it.Path, out e);
                        freed += f;
                        errors += e;
                        if (log != null) log(e == 0 ? L.F("Deleted: {0}", it.Path) : L.F("Partially: {0}", it.Path));
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    if (log != null) log(L.F("Error: {0} ({1})", it.Path, ex.Message));
                }
            }
            return freed;
        }

        /// <summary>Silmeden once kayit defteri anahtarlarini .reg olarak yedekler.</summary>
        public static string BackupRegistry(List<LeftoverItem> items, Action<string> log)
        {
            List<string> keys = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (LeftoverItem it in items)
            {
                if (!it.Checked) continue;
                if (it.Kind != LeftoverKind.RegKey && it.Kind != LeftoverKind.RegValue) continue;
                string key = it.Path.TrimEnd('\\');
                // alt anahtarlari ayrica yedeklemeye gerek yok
                bool covered = false;
                foreach (string k in keys)
                    if (key.StartsWith(k + "\\", StringComparison.OrdinalIgnoreCase)) { covered = true; break; }
                if (covered || seen.Contains(key)) continue;
                seen.Add(key);
                keys.Add(key);
            }
            if (keys.Count == 0) return null;

            string baseDir;
            try
            {
                baseDir = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "MineManager-backup");
            }
            catch { return null; }

            string dir = Path.Combine(baseDir, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            try { Directory.CreateDirectory(dir); }
            catch { return null; }

            int ok = 0;
            foreach (string key in keys)
            {
                try
                {
                    string name = key.Replace("\\", "_").Replace(":", "");
                    foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
                    if (name.Length > 120) name = name.Substring(0, 120);
                    string file = Path.Combine(dir, name + ".reg");

                    ProcessStartInfo psi = new ProcessStartInfo("reg.exe",
                        "export \"" + key + "\" \"" + file + "\" /y");
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardOutput = true;
                    psi.RedirectStandardError = true;
                    using (Process pr = Process.Start(psi))
                    {
                        pr.WaitForExit();
                        if (pr.ExitCode == 0 && File.Exists(file) && new FileInfo(file).Length > 0) ok++;
                    }
                }
                catch { }
            }

            try
            {
                if (ok == 0) { Directory.Delete(dir, true); return null; }
            }
            catch { }
            string result = L.F("{0} keys backed up → {1}", ok, dir);
            if (log != null) log(result);
            return dir;
        }

        public static int KillProcessesIn(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return 0;
            int n = 0;
            System.Diagnostics.Process[] ps = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process p in ps)
            {
                try
                {
                    string path = p.MainModule.FileName;
                    if (path != null && path.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                    {
                        p.Kill();
                        n++;
                    }
                }
                catch { }
                finally { try { p.Dispose(); } catch { } }
            }
            return n;
        }
    }
}
