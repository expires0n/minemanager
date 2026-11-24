using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32;

namespace MineManager
{
    /// <summary>
    /// Installer family of an uninstaller. Detection is ported from Bulk Crap
    /// Uninstaller's UninstallerTypeAdder (UninstallTools/Factory/InfoAdders).
    /// </summary>
    internal enum UninstallerKind
    {
        Unknown = 0,
        Msiexec,
        InnoSetup,
        Nsis,
        InstallShield,
        SdbInst,
        PowerShell,
        StoreApp
    }

    internal class InstalledProgram
    {
        // ------------------------------------------------------------- basics
        public string DisplayName = "";
        public string Version = "";
        public string Publisher = "";
        public string InstallLocation = "";
        public string UninstallString = "";
        public string QuietUninstallString = "";
        public string InstallDate = "";
        public string RegPath = "";
        public string KeyName = "";
        public bool SystemComponent;
        public bool IsUpdate;
        public long Size;

        // ------------------------------------------------- BCU style extras
        public string Comment = "";
        public string DisplayIcon = "";
        public string InstallSource = "";
        public string ModifyPath = "";
        public string AboutUrl = "";
        public string ReleaseType = "";
        public string ProductCode = "";
        public bool Is64Bit;
        public bool WindowsInstaller;
        public bool NoRemove;
        public bool IsInvalid;
        public UninstallerKind Kind = UninstallerKind.Unknown;
        public string QuietCommand = "";
        public bool QuietIsSynthesized;

        // Page state (multi-select column, not persisted).
        public bool Checked;

        public string SizeText
        {
            get { return Size > 0 ? Fmt.Size(Size) : "-"; }
        }

        public string RegistryKeyPath
        {
            get { return RegPath; }
        }

        public bool IsValid
        {
            get { return !IsInvalid; }
        }

        public string KindText
        {
            get { return UninstallerCatalog.KindText(Kind); }
        }

        public bool UninstallPossible
        {
            get { return UninstallString.Length > 0 || QuietUninstallString.Length > 0; }
        }

        /// <summary>True when a silent command exists and policy allows using it.</summary>
        public bool CanQuiet
        {
            get
            {
                if (IsInvalid || SystemComponent || NoRemove) return false;
                return !string.IsNullOrEmpty(QuietCommand);
            }
        }
    }

    internal static class Reg
    {
        /// <summary>
        /// Explicit registry view. Reading/writing with an explicit view keeps
        /// Wow6432Node paths and AnyCPU builds consistent (never double-redirected).
        /// </summary>
        private static RegistryView View
        {
            get
            {
                return Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            }
        }

        public static RegistryKey Open(string fullPath)
        {
            return Open(fullPath, false);
        }

        public static RegistryKey Open(string fullPath, bool writable)
        {
            if (string.IsNullOrEmpty(fullPath)) return null;
            int i = fullPath.IndexOf('\\');
            if (i < 0) return null;
            string hive = fullPath.Substring(0, i);
            string rest = fullPath.Substring(i + 1);

            try
            {
                if (hive == "HKLM") return OpenBase(RegistryHive.LocalMachine, rest, writable);
                if (hive == "HKCU") return OpenBase(RegistryHive.CurrentUser, rest, writable);
                if (hive == "HKCR") return OpenBase(RegistryHive.ClassesRoot, rest, writable);
                if (hive == "HKU") return OpenBase(RegistryHive.Users, rest, writable);
                if (hive == "HKCC") return OpenBase(RegistryHive.CurrentConfig, rest, writable);
            }
            catch { }
            return null;
        }

        public static RegistryKey OpenBase(RegistryHive hive, string path, bool writable)
        {
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, View))
                {
                    if (root == null) return null;
                    return root.OpenSubKey(path, writable);
                }
            }
            catch { return null; }
        }

        public static bool Exists(string fullPath)
        {
            try
            {
                using (RegistryKey k = Open(fullPath))
                    return k != null;
            }
            catch { return false; }
        }

        public static string Str(RegistryKey k, string name)
        {
            try
            {
                if (k == null) return "";
                object o = k.GetValue(name);
                if (o == null) return "";
                string s = o as string;
                if (s != null) return s.Trim();
                return o.ToString();
            }
            catch { return ""; }
        }

        /// <summary>String value, falling back to hidden variants (UninstallString_hidden).</summary>
        public static string StrFuzzy(RegistryKey k, string name)
        {
            string exact = Str(k, name);
            if (exact.Length > 0) return exact;
            try
            {
                if (k == null) return "";
                string[] names = k.GetValueNames();
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    {
                        string v = Str(k, names[i]);
                        if (v.Length > 0) return v;
                    }
                }
            }
            catch { }
            return "";
        }

        public static int Int(RegistryKey k, string name, int def)
        {
            try
            {
                if (k == null) return def;
                object o = k.GetValue(name);
                if (o == null) return def;
                return Convert.ToInt32(o, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch { return def; }
        }

        public static long Long(RegistryKey k, string name, long def)
        {
            try
            {
                if (k == null) return def;
                object o = k.GetValue(name);
                if (o == null) return def;
                return Convert.ToInt64(o, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch { return def; }
        }

        public static void DeleteTree(string fullPath, out bool ok)
        {
            ok = false;
            try
            {
                int i = fullPath.LastIndexOf('\\');
                if (i <= 0) return;
                string parent = fullPath.Substring(0, i);
                string leaf = fullPath.Substring(i + 1);
                using (RegistryKey p = Open(parent, true))
                {
                    if (p == null) return;
                    p.DeleteSubKeyTree(leaf, false);
                    ok = true;
                }
            }
            catch { ok = false; }
        }

        public static void DeleteValue(string fullPath, string valueName, out bool ok)
        {
            ok = false;
            try
            {
                using (RegistryKey k = Open(fullPath, true))
                {
                    if (k == null) return;
                    k.DeleteValue(valueName, false);
                    ok = true;
                }
            }
            catch { ok = false; }
        }

        /// <summary>
        /// Removes ONE directory entry from the PATH value and keeps every other entry,
        /// its order, its %VAR% text and its value kind. The old value is already inside
        /// the .reg backup of this key, so the change can be restored.
        /// </summary>
        public static void RemovePathEntry(string fullPath, string entry, out bool ok)
        {
            ok = false;
            try
            {
                using (RegistryKey k = Open(fullPath, true))
                {
                    if (k == null) return;
                    object raw = k.GetValue("Path");
                    if (raw == null) return;
                    RegistryValueKind kind = RegistryValueKind.String;
                    try { kind = k.GetValueKind("Path"); } catch { }

                    string target = (entry ?? "").Trim().TrimEnd('\\');
                    List<string> keep = new List<string>();
                    if (raw is string[])
                    {
                        string[] parts = (string[])raw;
                        for (int i = 0; i < parts.Length; i++)
                            if (!Same(parts[i], target)) keep.Add(parts[i]);
                        if (keep.Count == parts.Length) return;
                        k.SetValue("Path", keep.ToArray(), kind);
                    }
                    else
                    {
                        string[] parts = raw.ToString().Split(';');
                        bool changed = false;
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (Same(parts[i], target)) { changed = true; continue; }
                            keep.Add(parts[i]);
                        }
                        if (!changed) return;
                        string joined = string.Join(";", keep.ToArray());
                        k.SetValue("Path", joined, kind);
                    }
                    ok = true;
                }
            }
            catch { ok = false; }
        }

        private static bool Same(string a, string b)
        {
            if (a == null || b == null) return false;
            return string.Equals(a.Trim().TrimEnd('\\'), b.Trim().TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class UninstallerCatalog
    {
        // Private P/Invoke: the uninstaller owns it, Native.cs stays untouched.
        [DllImport("msi.dll", CharSet = CharSet.Unicode)]
        private static extern int MsiQueryProductState(string productCode);

        private const int InstallStateUnknown = -1;
        private const int InstallStateInvalidArg = -2;

        private const string NativeRoot = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string WowRoot = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        private class UninstallSource
        {
            public RegistryHive Hive;
            public string Path = "";
            public string Prefix = "";
            public bool Is64;
        }

        private static readonly string[] CommandExtensions =
            new string[] { ".exe", ".com", ".bat", ".cmd", ".msi", ".ps1" };

        private static readonly List<UninstallSource> Sources = BuildSources();

        private static List<UninstallSource> BuildSources()
        {
            List<UninstallSource> list = new List<UninstallSource>();

            UninstallSource m = new UninstallSource();
            m.Hive = RegistryHive.LocalMachine;
            m.Path = NativeRoot;
            m.Prefix = "HKLM";
            m.Is64 = true;
            list.Add(m);

            UninstallSource w = new UninstallSource();
            w.Hive = RegistryHive.LocalMachine;
            w.Path = WowRoot;
            w.Prefix = "HKLM";
            w.Is64 = false;
            list.Add(w);

            UninstallSource u = new UninstallSource();
            u.Hive = RegistryHive.CurrentUser;
            u.Path = NativeRoot;
            u.Prefix = "HKCU";
            u.Is64 = true;
            list.Add(u);

            return list;
        }

        // ------------------------------------------------------------------ read

        public static List<InstalledProgram> All(bool includeSystem)
        {
            List<InstalledProgram> list = new List<InstalledProgram>();
            Dictionary<string, bool> seen = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            for (int s = 0; s < Sources.Count; s++)
            {
                UninstallSource src = Sources[s];
                RegistryKey root = Reg.OpenBase(src.Hive, src.Path, false);
                if (root == null) continue;

                string[] subs;
                try { subs = root.GetSubKeyNames(); }
                catch { subs = new string[0]; }

                for (int i = 0; i < subs.Length; i++)
                {
                    InstalledProgram p = null;
                    try
                    {
                        using (RegistryKey k = root.OpenSubKey(subs[i]))
                        {
                            if (k == null) continue;
                            p = ReadEntry(k, src, subs[i], includeSystem);
                        }
                    }
                    catch { p = null; }

                    if (p == null) continue;

                    // Dedupe dual registrations (same product listed by MSI in
                    // both the 32-bit and 64-bit keys). Distinct products keep
                    // distinct uninstall strings and are preserved.
                    string id = p.DisplayName + "|" + p.Version + "|" + p.Publisher + "|" +
                                TrimForCompare(p.UninstallString);
                    if (p.UninstallString.Length == 0 && p.QuietUninstallString.Length == 0)
                        id = id + "|" + p.RegPath;
                    if (seen.ContainsKey(id)) continue;
                    seen[id] = true;
                    list.Add(p);
                }

                try { root.Close(); } catch { }
            }

            list.Sort(delegate(InstalledProgram a, InstalledProgram b)
            {
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.CurrentCultureIgnoreCase);
            });
            return list;
        }

        private static string TrimForCompare(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Trim().ToLowerInvariant();
        }

        private static InstalledProgram ReadEntry(RegistryKey k, UninstallSource src, string sub, bool includeSystem)
        {
            string name = Reg.Str(k, "DisplayName");
            if (name.Length == 0) return null;

            bool sys = Reg.Int(k, "SystemComponent", 0) != 0;
            string parent = Reg.Str(k, "ParentKeyName");
            bool upd = IsUpdateEntry(name, sub, k);
            if (!includeSystem && (sys || parent.Length > 0 || upd)) return null;

            InstalledProgram p = new InstalledProgram();
            p.DisplayName = name;
            p.Version = Reg.Str(k, "DisplayVersion");
            p.Publisher = Reg.Str(k, "Publisher");
            p.Comment = Reg.Str(k, "Comment");
            p.DisplayIcon = Reg.Str(k, "DisplayIcon").Trim('"', ' ');
            p.InstallLocation = Reg.Str(k, "InstallLocation").Trim('"', ' ');
            p.InstallSource = Reg.Str(k, "InstallSource").Trim('"', ' ');
            p.ModifyPath = Reg.Str(k, "ModifyPath");
            p.UninstallString = Reg.StrFuzzy(k, "UninstallString");
            p.QuietUninstallString = Reg.StrFuzzy(k, "QuietUninstallString");
            p.InstallDate = Reg.Str(k, "InstallDate");
            p.ReleaseType = Reg.Str(k, "ReleaseType");
            p.AboutUrl = ReadAboutUrl(k);
            long kb = Reg.Long(k, "EstimatedSize", 0);
            p.Size = kb > 0 ? kb * 1024 : 0;
            p.KeyName = sub;
            p.RegPath = src.Prefix + "\\" + src.Path + "\\" + sub;
            p.Is64Bit = src.Is64;
            p.WindowsInstaller = Reg.Int(k, "WindowsInstaller", 0) != 0;
            p.NoRemove = Reg.Int(k, "NoRemove", 0) != 0;
            p.SystemComponent = sys || parent.Length > 0;
            p.IsUpdate = upd;
            p.ProductCode = ReadProductCode(k, sub, p.UninstallString);

            p.Kind = DetectKind(p);
            p.QuietCommand = BuildQuietCommand(p);
            p.QuietIsSynthesized = p.QuietCommand.Length > 0 &&
                                   string.IsNullOrEmpty(p.QuietUninstallString);
            p.IsInvalid = IsEntryInvalid(p);
            return p;
        }

        private static string ReadAboutUrl(RegistryKey k)
        {
            string[] names = new string[] { "URLInfoAbout", "URLUpdateInfo", "HelpLink" };
            for (int i = 0; i < names.Length; i++)
            {
                string v = Reg.Str(k, names[i]);
                if (v.Length > 0 && v.IndexOf('.') >= 0) return v;
            }
            return "";
        }

        private static bool IsUpdateEntry(string displayName, string keyName, RegistryKey k)
        {
            if (displayName.StartsWith("Update for ", StringComparison.OrdinalIgnoreCase)) return true;
            if (displayName.StartsWith("Security Update for ", StringComparison.OrdinalIgnoreCase)) return true;
            if (displayName.StartsWith("Hotfix for ", StringComparison.OrdinalIgnoreCase)) return true;
            if (displayName.StartsWith("Güncelleştirme", StringComparison.OrdinalIgnoreCase)) return true;
            if (keyName.Length > 3 && keyName[0] == 'K' && keyName[1] == 'B' && char.IsDigit(keyName[2]))
                return true;

            string type = Reg.Str(k, "ReleaseType");
            if (type.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (type.IndexOf("Hotfix", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            string def = Reg.Str(k, null);
            if (def.Length > 6 && def.StartsWith("KB", StringComparison.Ordinal) &&
                char.IsDigit(def[2]) && char.IsDigit(def[def.Length - 1])) return true;
            return false;
        }

        // ------------------------------------------------------------- commands

        /// <summary>
        /// Splits an uninstall command into executable and arguments. Handles the
        /// common case of an unquoted path containing spaces by growing the token
        /// window until an existing file (or a known extension) is reached.
        /// </summary>
        public static string SplitCommand(string command, out string args)
        {
            args = "";
            if (string.IsNullOrEmpty(command)) return "";

            string c;
            try { c = Environment.ExpandEnvironmentVariables(command.Trim()); }
            catch { c = command.Trim(); }
            if (c.Length == 0) return "";

            if (c[0] == '"')
            {
                int close = c.IndexOf('"', 1);
                if (close > 1)
                {
                    args = c.Substring(close + 1).Trim();
                    return c.Substring(1, close - 1).Trim();
                }
                return c.Trim('"');
            }

            string[] parts = c.Split(' ');
            string missing = "";
            string missingArgs = "";

            for (int n = 1; n <= parts.Length && n <= 10; n++)
            {
                string candidate = JoinParts(parts, 0, n).Trim('"');
                if (candidate.Length == 0) continue;
                string rest = n < parts.Length ? JoinParts(parts, n, parts.Length - n) : "";

                bool exists = false;
                try { exists = File.Exists(candidate); }
                catch { exists = false; }
                if (exists) { args = rest; return candidate; }

                if (!HasKnownExtension(candidate)) continue;

                bool rooted = false;
                try { rooted = Path.IsPathRooted(candidate); }
                catch { rooted = false; }

                if (rooted) { missing = candidate; missingArgs = rest; break; }
                if (n == 1) { args = rest; return candidate; }
                missing = candidate;
                missingArgs = rest;
                break;
            }

            if (missing.Length > 0) { args = missingArgs; return missing; }

            args = parts.Length > 1 ? JoinParts(parts, 1, parts.Length - 1) : "";
            return parts[0].Trim('"');
        }

        private static string JoinParts(string[] parts, int start, int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < start + count && i < parts.Length; i++)
            {
                if (parts[i].Length == 0) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(parts[i]);
            }
            return sb.ToString();
        }

        private static bool HasKnownExtension(string s)
        {
            for (int i = 0; i < CommandExtensions.Length; i++)
                if (s.EndsWith(CommandExtensions[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static string ResolveUninstaller(InstalledProgram p, out string args)
        {
            args = "";
            if (p == null) return "";
            string cmd = !string.IsNullOrEmpty(p.UninstallString) ? p.UninstallString : p.QuietUninstallString;
            return SplitCommand(cmd, out args);
        }

        public static string WorkingDirectory(string exe)
        {
            try
            {
                if (string.IsNullOrEmpty(exe) || !Path.IsPathRooted(exe)) return "";
                string dir = Path.GetDirectoryName(exe);
                if (dir != null && Directory.Exists(dir)) return dir;
            }
            catch { }
            return "";
        }

        /// <summary>msiexec /I{guid} -> /X{guid}: the registry often stores the modify command.</summary>
        public static string NormalizeUninstallCommand(string cmd)
        {
            string c = cmd.Trim();
            if (c.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase))
            {
                int i = c.IndexOf("/I", StringComparison.OrdinalIgnoreCase);
                if (i < 0) i = c.IndexOf("-I", StringComparison.OrdinalIgnoreCase);
                if (i >= 0)
                {
                    string head = c.Substring(0, i);
                    string tail = c.Substring(i + 2);
                    return head + "/X" + tail;
                }
            }
            return c;
        }

        // -------------------------------------------------------------- detection

        public static string KindText(UninstallerKind kind)
        {
            switch (kind)
            {
                case UninstallerKind.Msiexec: return L.T("MSI (Windows Installer)");
                case UninstallerKind.InnoSetup: return L.T("Inno Setup");
                case UninstallerKind.Nsis: return L.T("NSIS");
                case UninstallerKind.InstallShield: return L.T("InstallShield");
                case UninstallerKind.SdbInst: return L.T("Application shim (sdbinst)");
                case UninstallerKind.PowerShell: return L.T("PowerShell");
                case UninstallerKind.StoreApp: return L.T("Store app");
                default: return L.T("Unknown");
            }
        }

        /// <summary>Type detection ported from UninstallerTypeAdder.cs.</summary>
        private static UninstallerKind DetectKind(InstalledProgram p)
        {
            string all = (p.UninstallString + " " + p.QuietUninstallString).Trim();
            string cmd = p.UninstallString.Length > 0 ? p.UninstallString : p.QuietUninstallString;

            if (p.WindowsInstaller) return UninstallerKind.Msiexec;
            if (PointsToMsiexec(cmd)) return UninstallerKind.Msiexec;
            if (LooksLikePackageCache(cmd)) return UninstallerKind.Msiexec;

            if (all.IndexOf("sdbinst", StringComparison.OrdinalIgnoreCase) >= 0 &&
                all.IndexOf(".sdb", StringComparison.OrdinalIgnoreCase) >= 0)
                return UninstallerKind.SdbInst;

            if (all.IndexOf(@"InstallShield Installation Information\{", StringComparison.OrdinalIgnoreCase) >= 0)
                return UninstallerKind.InstallShield;

            if (all.IndexOf("powershell.exe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                all.IndexOf(".ps1", StringComparison.OrdinalIgnoreCase) >= 0)
                return UninstallerKind.PowerShell;

            string args;
            string exe = ResolveUninstaller(p, out args);
            bool fileExists = false;
            try { fileExists = exe.Length > 0 && Path.IsPathRooted(exe) && File.Exists(exe); }
            catch { fileExists = false; }

            if (fileExists)
            {
                string name = "";
                try { name = Path.GetFileNameWithoutExtension(exe); }
                catch { name = ""; }

                if (IsInnoSetupName(name))
                {
                    // A real Inno uninstaller always ships the unins###.dat log.
                    string dat = "";
                    try { dat = Path.ChangeExtension(exe, ".dat"); }
                    catch { dat = ""; }
                    if (dat.Length > 0 && File.Exists(dat)) return UninstallerKind.InnoSetup;
                }

                if (ContainsNullsoft(exe)) return UninstallerKind.Nsis;
            }

            // Naked MSI key: a valid product code and nothing to launch.
            if (p.UninstallString.Length == 0 && p.QuietUninstallString.Length == 0)
            {
                Guid g;
                if (TryParseGuid(p.ProductCode, out g)) return UninstallerKind.Msiexec;
            }

            return UninstallerKind.Unknown;
        }

        private static bool PointsToMsiexec(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return false;
            string args;
            string exe = SplitCommand(cmd, out args);
            if (exe.Length == 0) return false;
            string file = exe;
            int slash = exe.LastIndexOfAny(new char[] { '\\', '/' });
            if (slash >= 0) file = exe.Substring(slash + 1);
            return string.Equals(file, "msiexec.exe", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(file, "msiexec", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Port of ApplicationEntryTools.PathPointsToMsiExec's bundle-exe heuristic.</summary>
        private static bool LooksLikePackageCache(string cmd)
        {
            if (string.IsNullOrEmpty(cmd)) return false;
            if (cmd.IndexOf(@"\Package Cache\{", StringComparison.OrdinalIgnoreCase) < 0) return false;
            if (cmd.IndexOf(@"}\", StringComparison.OrdinalIgnoreCase) < 0) return false;
            return cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsInnoSetupName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            string n = name.ToLowerInvariant();
            int at = n.IndexOf("unins", StringComparison.Ordinal);
            while (at >= 0)
            {
                int digits = 0;
                int i = at + 5;
                while (i < n.Length && char.IsDigit(n[i])) { digits++; i++; }
                if (digits >= 3) return true;
                at = n.IndexOf("unins", at + 1, StringComparison.Ordinal);
            }
            return false;
        }

        /// <summary>Reads at most 64 KB of the executable looking for the NSIS signature.</summary>
        private static bool ContainsNullsoft(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int want = 65536;
                    if (fs.Length < want) want = (int)fs.Length;
                    if (want <= 0) return false;
                    byte[] buf = new byte[want];
                    int read = fs.Read(buf, 0, want);
                    if (read <= 0) return false;
                    string text = Encoding.ASCII.GetString(buf, 0, read);
                    return text.IndexOf("Nullsoft", StringComparison.Ordinal) >= 0;
                }
            }
            catch { return false; }
        }

        // ---------------------------------------------------------------- quiet

        /// <summary>
        /// Registry QuietUninstallString first, then synthesis for MSI, Inno Setup
        /// and NSIS only. Everything else must run interactively.
        /// </summary>
        public static string BuildQuietCommand(InstalledProgram p)
        {
            if (p == null) return "";
            if (p.SystemComponent || p.NoRemove) return "";

            if (!string.IsNullOrEmpty(p.QuietUninstallString))
                return NormalizeUninstallCommand(p.QuietUninstallString);

            if (p.Kind == UninstallerKind.Msiexec)
            {
                // only a real msiexec launch accepts /qn; Burn bundles are .exe files
                if (!LaunchesMsiexec(p) && !IsNakedMsiKey(p)) return "";
                Guid g;
                if (!TryParseGuid(p.ProductCode, out g)) return "";
                return "msiexec.exe /X" + g.ToString("B") + " /qn /norestart";
            }

            if (p.Kind == UninstallerKind.InnoSetup || p.Kind == UninstallerKind.Nsis)
            {
                string args;
                string exe = ResolveUninstaller(p, out args);
                if (exe.Length == 0) return "";
                bool ok = false;
                try { ok = Path.IsPathRooted(exe) && File.Exists(exe); }
                catch { ok = false; }
                if (!ok) return "";

                if (p.Kind == UninstallerKind.InnoSetup)
                    return "\"" + exe + "\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
                return "\"" + exe + "\" /S";
            }

            return "";
        }

        // ------------------------------------------------------------ validation

        /// <summary>True when the entry really runs msiexec (not a Burn bundle .exe).</summary>
        private static bool LaunchesMsiexec(InstalledProgram p)
        {
            if (p.WindowsInstaller) return true;
            string cmd = p.UninstallString.Length > 0 ? p.UninstallString : p.QuietUninstallString;
            if (cmd.Length == 0) return false;
            return PointsToMsiexec(cmd);
        }

        /// <summary>A key that only has a product code, with nothing to launch.</summary>
        private static bool IsNakedMsiKey(InstalledProgram p)
        {
            if (p.UninstallString.Length > 0 || p.QuietUninstallString.Length > 0) return false;
            Guid g;
            return TryParseGuid(p.ProductCode, out g);
        }

        private static bool IsEntryInvalid(InstalledProgram p)
        {
            if (p.Kind == UninstallerKind.Msiexec)
            {
                // A Bundle/exe based "Package Cache" entry (VC++ redist, Python) carries a
                // bundle GUID, not an MSI product code: MsiQueryProductState would report
                // it as unknown and the entry would wrongly look stale.
                if (LaunchesMsiexec(p) || IsNakedMsiKey(p))
                {
                    Guid g;
                    if (TryParseGuid(p.ProductCode, out g))
                    {
                        int state = QueryProductState(g);
                        if (state == InstallStateUnknown || state == InstallStateInvalidArg) return true;
                        return false;
                    }
                    return p.UninstallString.Length == 0 && p.QuietCommand.Length == 0;
                }
                // Fall through: validate the bundle executable instead.
            }

            if (p.UninstallString.Length == 0 && p.QuietUninstallString.Length == 0 &&
                p.QuietCommand.Length == 0) return true;

            string args;
            string exe = ResolveUninstaller(p, out args);
            if (exe.Length == 0) return false;
            try
            {
                if (Path.IsPathRooted(exe) && !File.Exists(exe)) return true;
            }
            catch { return true; }
            return false;
        }

        private static int QueryProductState(Guid g)
        {
            try { return MsiQueryProductState(g.ToString("B")); }
            catch { return 0; }
        }

        // -------------------------------------------------------------- product key

        private static string ReadProductCode(RegistryKey k, string keyName, string uninstallString)
        {
            Guid g;
            if (TryExtractGuid(Reg.Str(k, "BundleProviderKey"), out g)) return g.ToString("B");
            if (TryExtractGuid(keyName, out g)) return g.ToString("B");
            if (TryExtractGuid(uninstallString, out g)) return g.ToString("B");
            if (TryExtractGuid(Reg.Str(k, "ModifyPath"), out g)) return g.ToString("B");
            return "";
        }

        public static bool TryParseGuid(string s, out Guid g)
        {
            g = Guid.Empty;
            if (string.IsNullOrEmpty(s)) return false;
            string t = s.Trim();
            return Guid.TryParse(t, out g);
        }

        /// <summary>Finds the first {GUID} inside a string (paths, msiexec commands, key names).</summary>
        public static bool TryExtractGuid(string s, out Guid g)
        {
            g = Guid.Empty;
            if (string.IsNullOrEmpty(s)) return false;

            int start = s.IndexOf('{');
            while (start >= 0)
            {
                int end = s.IndexOf('}', start + 1);
                if (end > start)
                {
                    string part = s.Substring(start, end - start + 1);
                    Guid parsed;
                    if (Guid.TryParse(part, out parsed)) { g = parsed; return true; }
                }
                if (start + 1 >= s.Length) break;
                start = s.IndexOf('{', start + 1);
            }

            string trimmed = s.Trim().Trim('"');
            Guid direct;
            if (Guid.TryParse(trimmed, out direct)) { g = direct; return true; }
            return false;
        }

        // --------------------------------------------------------------- actions

        /// <summary>Legacy single-shot launcher (the page uses UninstallQueue).</summary>
        public static int RunUninstaller(InstalledProgram p, out string error)
        {
            error = "";
            if (p == null)
            {
                error = L.T("No uninstall command is registered for this program.");
                return -1;
            }

            string args;
            string exe = ResolveUninstaller(p, out args);
            if (exe.Length == 0)
            {
                error = L.T("No uninstall command is registered for this program.");
                return -1;
            }

            exe = NormalizeUninstallCommand(exe);
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args == null ? "" : args);
                psi.UseShellExecute = true;
                string wd = WorkingDirectory(exe);
                if (wd.Length > 0) psi.WorkingDirectory = wd;
                using (Process proc = Process.Start(psi))
                {
                    if (proc == null)
                    {
                        error = L.T("The uninstaller could not be started.");
                        return -1;
                    }
                    proc.WaitForExit();
                    return proc.ExitCode;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return -1;
            }
        }

        /// <summary>Registry-export backup folder next to the executable.</summary>
        public static string BackupRoot()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (string.IsNullOrEmpty(baseDir)) baseDir = Path.GetTempPath();
                return Path.Combine(baseDir, "MineManager-backup");
            }
            catch { return Path.Combine(Path.GetTempPath(), "MineManager-backup"); }
        }

        /// <summary>Exports one registry key with reg.exe. Returns the .reg path or an empty string.</summary>
        public static string BackupKey(string regPath)
        {
            if (string.IsNullOrEmpty(regPath)) return "";

            string dir = Path.Combine(BackupRoot(), DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            try { Directory.CreateDirectory(dir); }
            catch { return ""; }

            string name = regPath.Replace("\\", "_").Replace(":", "");
            char[] bad = Path.GetInvalidFileNameChars();
            for (int i = 0; i < bad.Length; i++) name = name.Replace(bad[i], '_');
            if (name.Length > 120) name = name.Substring(0, 120);
            string file = Path.Combine(dir, name + ".reg");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("reg.exe",
                    "export \"" + regPath + "\" \"" + file + "\" /y");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process pr = Process.Start(psi))
                {
                    if (pr == null) return "";
                    pr.WaitForExit();
                    if (pr.ExitCode == 0 && File.Exists(file) && new FileInfo(file).Length > 0)
                        return file;
                }
            }
            catch { }

            try { if (File.Exists(file)) File.Delete(file); } catch { }
            return "";
        }

        /// <summary>
        /// Backs the key up and then deletes only that key (used for invalid entries
        /// whose uninstaller no longer exists).
        /// </summary>
        public static bool RemoveInvalidEntry(InstalledProgram p, out string backupFile, out string error)
        {
            backupFile = "";
            error = "";
            if (p == null || string.IsNullOrEmpty(p.RegPath))
            {
                error = L.T("This entry has no registry key.");
                return false;
            }

            backupFile = BackupKey(p.RegPath);
            if (backupFile.Length == 0)
            {
                error = L.T("The registry key could not be backed up, so it was not removed.");
                return false;
            }

            bool ok;
            Reg.DeleteTree(p.RegPath, out ok);
            if (!ok) error = L.T("The registry key could not be deleted.");
            return ok;
        }

        // ------------------------------------------------------------------ CSV

        /// <summary>UTF-8 (BOM) CSV with quoted cells and formula-injection guarding.</summary>
        public static void ExportCsv(IList<InstalledProgram> items, string path)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(CsvRow(new string[]
            {
                L.T("Name"), L.T("Publisher"), L.T("Version"), L.T("Install date"),
                L.T("Size (bytes)"), L.T("Type"), L.T("Quiet uninstall possible"),
                L.T("Install location"), L.T("Uninstall command"),
                L.T("Quiet uninstall command"), L.T("Registry key")
            }));

            for (int i = 0; i < items.Count; i++)
            {
                InstalledProgram p = items[i];
                sb.Append(CsvRow(new string[]
                {
                    p.DisplayName, p.Publisher, p.Version, FormatDate(p.InstallDate),
                    p.Size.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    p.KindText, p.CanQuiet ? L.T("Yes") : L.T("No"),
                    p.InstallLocation, p.UninstallString,
                    p.QuietCommand.Length > 0 ? p.QuietCommand : "",
                    p.RegPath
                }));
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static string CsvRow(string[] cells)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"');
                sb.Append(CsvCell(cells[i]));
                sb.Append('"');
            }
            sb.Append("\r\n");
            return sb.ToString();
        }

        private static string CsvCell(string value)
        {
            string v = value == null ? "" : value;
            if (v.Length > 0)
            {
                char c = v[0];
                // Spreadsheet formula injection guard.
                if (c == '=' || c == '+' || c == '-' || c == '@' || c == '\t' || c == '\r')
                    v = "'" + v;
            }
            return v.Replace("\"", "\"\"");
        }

        public static string FormatDate(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 8) return s == null ? "" : s;
            return s.Substring(6, 2) + "." + s.Substring(4, 2) + "." + s.Substring(0, 4);
        }
    }

    /// <summary>
    /// Strictly sequential uninstall worker (msiexec refuses concurrent installs).
    /// Quiet items are killed after 10 minutes; interactive items run without a
    /// timeout through shell-execute so UAC prompts work. Cancel stops after the
    /// current item.
    /// </summary>
    internal sealed class UninstallQueue
    {
        private const int QuietTimeoutSec = 600;
        private const int KeyPollSec = 60;

        private readonly IList<InstalledProgram> _items;
        private readonly bool _quiet;
        private readonly CancelFlag _cancel = new CancelFlag();
        private Thread _thread;
        private volatile bool _reboot;
        private volatile bool _stop;

        public UninstallQueue(IList<InstalledProgram> items, bool quiet)
        {
            _items = items == null ? new List<InstalledProgram>() : items;
            _quiet = quiet;
        }

        /// <summary>Worker thread. Status text is already localised.</summary>
        public event Action<int, int, InstalledProgram, string> ItemChanged;
        public event Action Finished;

        public bool Cancelled
        {
            get { return _cancel.Cancelled; }
        }

        public bool RebootRequired
        {
            get { return _reboot; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public void Start()
        {
            if (_thread != null) return;
            _thread = new Thread(Run);
            _thread.IsBackground = true;
            _thread.Name = "MineManager-Uninstall";
            _thread.Start();
        }

        public void Cancel()
        {
            _cancel.Cancelled = true;
        }

        private void Report(int index, InstalledProgram p, string status)
        {
            Action<int, int, InstalledProgram, string> h = ItemChanged;
            if (h == null) return;
            try { h(index, _items.Count, p, status); }
            catch { }
        }

        private void Run()
        {
            try
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    InstalledProgram p = _items[i];
                    if (p == null) continue;

                    if (_cancel.Cancelled)
                    {
                        Report(i, p, L.T("Cancelled."));
                        break;
                    }
                    if (p.IsInvalid)
                    {
                        Report(i, p, L.T("Skipped (invalid registry entry)."));
                        continue;
                    }

                    bool useQuiet = _quiet && p.CanQuiet;
                    Report(i, p, useQuiet
                        ? L.T("Running quiet uninstall...")
                        : L.T("Waiting for the uninstaller to finish..."));

                    string status;
                    Action<string> report = delegate(string s) { Report(i, p, s); };
                    bool ok = RunOne(p, useQuiet, report, out status);
                    if (status.Length == 0) status = ok ? L.T("Uninstalled.") : L.T("Failed.");
                    Report(i, p, status);

                    // 1641: the installer itself started a restart. Running the next
                    // uninstaller on a half-rebooted machine is worse than stopping here.
                    if (_stop)
                    {
                        for (int j = i + 1; j < _items.Count; j++)
                            Report(j, _items[j], L.T("Waiting (the queue stopped after a restart was started)."));
                        break;
                    }
                }
            }
            catch { }
            finally
            {
                Action f = Finished;
                if (f != null) { try { f(); } catch { } }
            }
        }

        private static bool IsMsiExecutable(string exe)
        {
            try
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(exe);
                return string.Equals(name, "msiexec", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private bool RunOne(InstalledProgram p, bool quiet, Action<string> report, out string status)
        {
            status = "";

            if (quiet)
            {
                string cmd = p.QuietCommand;
                if (string.IsNullOrEmpty(cmd)) cmd = UninstallerCatalog.BuildQuietCommand(p);

                string qargs;
                string qexe = UninstallerCatalog.SplitCommand(cmd, out qargs);
                if (qexe.Length > 0)
                {
                    bool msi = IsMsiExecutable(qexe);
                    // msiexec is transactional: a kill leaves a half-applied package, so it
                    // gets no deadline. NSIS/Inno uninstallers are killed after 10 minutes.
                    int timeout = msi ? -1 : QuietTimeoutSec;
                    RunResult r = msi
                        ? RunMsiWithBusyRetry(qexe, qargs, timeout, report)
                        : ProcRunner.Run(qexe, qargs, timeout, null, null);
                    if (r.TimedOut)
                    {
                        status = L.T("Timed out after 10 minutes; the process tree was closed.");
                        return false;
                    }

                    if (NeedsKeyPoll(p))
                    {
                        if (WaitForKeyGone(p, KeyPollSec))
                        {
                            status = L.T("Uninstalled (registry entry removed).");
                            return true;
                        }
                        status = L.T("The uninstaller finished but the registry entry is still present.");
                        return false;
                    }
                    // ProcRunner reports -1 when the process could not be started at all.
                    if (r.ExitCode == -1)
                    {
                        status = L.T("The uninstaller could not be started.");
                        return false;
                    }
                    return ClassifyExit(r.ExitCode, msi, out status);
                }
            }

            string args;
            string exe = UninstallerCatalog.ResolveUninstaller(p, out args);
            if (exe.Length == 0)
            {
                status = L.T("No uninstall command is registered for this program.");
                return false;
            }

            bool interactiveMsi = IsMsiExecutable(exe);
            int code = StartInteractive(exe, args, out status);
            if (code == -2) return false;   // start error, status already set

            if (NeedsKeyPoll(p))
            {
                if (WaitForKeyGone(p, KeyPollSec))
                {
                    status = L.T("Uninstalled (registry entry removed).");
                    return true;
                }
                status = L.T("The uninstaller finished but the registry entry is still present.");
                return false;
            }
            return ClassifyExit(code, interactiveMsi || p.WindowsInstaller, out status);
        }

        /// <summary>
        /// MSI 1618 = another Windows Installer transaction is running. Policy: wait
        /// 15, 30, 60, 60 seconds and retry the SAME item, at most 5 calls in total.
        /// Another msiexec process is never killed.
        /// </summary>
        private RunResult RunMsiWithBusyRetry(string exe, string args, int timeout, Action<string> report)
        {
            int[] waits = new int[] { 15, 30, 60, 60 };
            RunResult r = ProcRunner.Run(exe, args, timeout, null, null);
            for (int attempt = 0; attempt < waits.Length; attempt++)
            {
                if (!WinErr.MsiBusy(r.ExitCode) || r.Cancelled || r.TimedOut) return r;
                if (report != null)
                    report(L.F("Another installation is in progress — waiting {0}s (retry {1}/4)...",
                        waits[attempt], attempt + 1));
                int left = waits[attempt];
                while (left > 0 && !_cancel.Cancelled)
                {
                    Thread.Sleep(1000);
                    left--;
                }
                if (_cancel.Cancelled) return r;
                r = ProcRunner.Run(exe, args, timeout, null, null);
            }
            return r;
        }

        /// <summary>NSIS and Inno exit codes are unreliable (they re-spawn from %TEMP%).</summary>
        private static bool NeedsKeyPoll(InstalledProgram p)
        {
            return p.Kind == UninstallerKind.Nsis || p.Kind == UninstallerKind.InnoSetup;
        }

        private static bool WaitForKeyGone(InstalledProgram p, int seconds)
        {
            if (p == null || string.IsNullOrEmpty(p.RegPath)) return false;
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (true)
            {
                if (!Reg.Exists(p.RegPath)) return true;
                if (DateTime.UtcNow > deadline) return false;
                Thread.Sleep(700);
            }
        }

        /// <summary>Shell-execute so a non-elevated parent can still trigger UAC.</summary>
        private int StartInteractive(string exe, string args, out string status)
        {
            status = "";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args == null ? "" : args);
                psi.UseShellExecute = true;
                string wd = UninstallerCatalog.WorkingDirectory(exe);
                if (wd.Length > 0) psi.WorkingDirectory = wd;
                using (Process proc = Process.Start(psi))
                {
                    if (proc == null)
                    {
                        status = L.T("The uninstaller could not be started.");
                        return -2;
                    }
                    proc.WaitForExit();
                    return proc.ExitCode;
                }
            }
            catch (Exception ex)
            {
                status = L.F("The uninstaller could not be started: {0}", ex.Message);
                return -2;
            }
        }

        /// <summary>
        /// Exit-code semantics. MSI codes are only trusted when the run really went
        /// through msiexec — a vendor EXE defines its own exit codes.
        /// </summary>
        private bool ClassifyExit(int code, bool msi, out string status)
        {
            status = "";
            if (code == 0) { status = L.T("Uninstalled."); return true; }
            if (code == 1605) { status = L.T("Already removed (the product is not installed)."); return true; }
            if (code == 1602) { status = L.T("Skipped (the uninstaller was cancelled)."); return false; }
            if (code == 1641 || code == 3010)
            {
                _reboot = true;
                if (code == 1641) _stop = true;   // a restart was already started
                status = L.T("Uninstalled. A restart is required.");
                return true;
            }
            if (code == 1618)
            {
                status = L.T("Another installation is in progress. Try again later.");
                return false;
            }
            if (msi)
            {
                // 1603, 1612, 1619, 1620, 1625, 1638, 1639 … — say what MSI meant.
                string m = WinErr.MsiMeaning(code);
                if (m.Length > 0)
                {
                    status = L.F("Exit code {0} · {1}", code, m);
                    return false;
                }
            }
            string win = WinErr.Win32Message(code);
            status = win.Length > 0
                ? L.F("Exit code {0} · {1}", code, win)
                : L.F("Failed (exit code {0}).", code);
            return false;
        }
    }
}
