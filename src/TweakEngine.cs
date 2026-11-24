using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace MineManager
{
    internal enum TweakState
    {
        Unknown = 0,
        NotApplied,
        Applied,
        Mixed
    }

    /// <summary>One registry write. Path uses HKLM\ / HKCU\ / HKU\ (no PowerShell drive prefix).</summary>
    internal class RegOp
    {
        public string Path = "";
        public string Name = "";
        /// <summary>DWord | QWord | String | ExpandString | Binary | MultiString</summary>
        public string Type = "DWord";
        public string Value = "";
        /// <summary>Value to restore on undo. Tweak.RemoveEntry deletes the value instead.</summary>
        public string Original = "";
    }

    internal class SvcOp
    {
        public string Name = "";
        /// <summary>Automatic | Manual | Disabled | AutomaticDelayed</summary>
        public string Startup = "Manual";
        public string Original = "";
    }

    internal class TaskOp
    {
        public string Name = "";
        /// <summary>Enabled | Disabled</summary>
        public string State = "Disabled";
        public string Original = "";
    }

    /// <summary>
    /// One WinUtil tweak. Kind: "toggle" (Customize Preferences switch), "check"
    /// (Essential/Advanced list) or "button" (process action, no state).
    /// </summary>
    internal class Tweak
    {
        public const string RemoveEntry = "<RemoveEntry>";

        public string Id = "";
        /// <summary>Nav group: essential | advanced | prefs | performance</summary>
        public string Group = "essential";
        public string Title = "";
        public string Desc = "";
        public string Note = "";
        public string Link = "";
        public string Kind = "check";

        public RegOp[] Registry = new RegOp[0];
        public SvcOp[] Services = new SvcOp[0];
        public TaskOp[] Tasks = new TaskOp[0];

        /// <summary>PowerShell block run after the registry/services/tasks steps (built-in tools only).</summary>
        public string Script = "";
        public string UndoScript = "";

        public int TimeoutSec = 180;
        public bool Danger;
        public bool NeedsRestart;

        public bool HasState
        {
            get { return Registry.Length > 0 || Services.Length > 0 || Tasks.Length > 0; }
        }

        public bool RequiresAdmin
        {
            get
            {
                for (int i = 0; i < Registry.Length; i++)
                    if (!Registry[i].Path.StartsWith("HKCU", StringComparison.OrdinalIgnoreCase)) return true;
                return Services.Length > 0 || Tasks.Length > 0 ||
                       (Script.Length > 0 && Kind != "button");
            }
        }
    }

    /// <summary>
    /// Registry/service/task engine ported in spirit from WinUtil's tweaks logic.
    /// Detection never runs on the UI thread; callers wrap it in a Task.
    ///
    /// The engine never takes the global job guard (ProcRunner.TryBeginJob) and
    /// never touches the UI: the page owns both. Every failure is reported through
    /// the log callback, nothing is thrown out of the public methods.
    /// </summary>
    internal static class TweakEngine
    {
        /// <summary>Sentinel used to tell "value missing" from "value is empty".</summary>
        private static readonly object Missing = new object();

        // ============================================================ registry

        private static bool SplitPath(string full, out RegistryHive hive, out string sub)
        {
            hive = RegistryHive.LocalMachine;
            sub = "";
            if (full == null) return false;
            string p = full.Trim();
            if (p.Length == 0) return false;
            // Drop a PowerShell drive colon: "HKLM:\SOFTWARE\X" -> "HKLM\SOFTWARE\X".
            int colon = p.IndexOf(':');
            int slash = p.IndexOf('\\');
            if (colon >= 0 && (slash < 0 || colon < slash))
                p = p.Substring(0, colon) + p.Substring(colon + 1);
            int i = p.IndexOf('\\');
            string head = (i < 0 ? p : p.Substring(0, i)).Trim();
            sub = i < 0 ? "" : p.Substring(i + 1);
            if (string.Equals(head, "HKLM", StringComparison.OrdinalIgnoreCase)) { hive = RegistryHive.LocalMachine; return true; }
            if (string.Equals(head, "HKCU", StringComparison.OrdinalIgnoreCase)) { hive = RegistryHive.CurrentUser; return true; }
            if (string.Equals(head, "HKU", StringComparison.OrdinalIgnoreCase)) { hive = RegistryHive.Users; return true; }
            if (string.Equals(head, "HKCR", StringComparison.OrdinalIgnoreCase)) { hive = RegistryHive.ClassesRoot; return true; }
            if (string.Equals(head, "HKCC", StringComparison.OrdinalIgnoreCase)) { hive = RegistryHive.CurrentConfig; return true; }
            return false;
        }

        private static string Display(string path, string name)
        {
            if (name == null || name.Length == 0) return path + "\\ (default)";
            return path + "\\" + name;
        }

        private static bool ReadFromKey(RegistryKey key, string name, out RegistryValueKind kind, out string text)
        {
            kind = RegistryValueKind.String;
            text = "";
            if (key == null) return false;
            object value;
            try { value = key.GetValue(name, Missing); }
            catch { return false; }
            if (value == null || ReferenceEquals(value, Missing)) return false;
            try { kind = key.GetValueKind(name); }
            catch { }
            text = ValueText(value, kind);
            return true;
        }

        private static string ValueText(object value, RegistryValueKind kind)
        {
            if (value == null) return "";
            byte[] bytes = value as byte[];
            if (bytes != null)
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(bytes[i].ToString(CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }
            string[] many = value as string[];
            if (many != null) return string.Join("\n", many);
            if (kind == RegistryValueKind.DWord)
            {
                try { return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture); }
                catch { }
            }
            if (kind == RegistryValueKind.QWord)
            {
                try { return Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture); }
                catch { }
            }
            string s = value as string;
            if (s != null) return s;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        private static RegistryValueKind ToKind(string type)
        {
            if (string.Equals(type, "DWord", StringComparison.OrdinalIgnoreCase)) return RegistryValueKind.DWord;
            if (string.Equals(type, "QWord", StringComparison.OrdinalIgnoreCase)) return RegistryValueKind.QWord;
            if (string.Equals(type, "Binary", StringComparison.OrdinalIgnoreCase)) return RegistryValueKind.Binary;
            if (string.Equals(type, "MultiString", StringComparison.OrdinalIgnoreCase)) return RegistryValueKind.MultiString;
            if (string.Equals(type, "ExpandString", StringComparison.OrdinalIgnoreCase)) return RegistryValueKind.ExpandString;
            return RegistryValueKind.String;
        }

        private static string TypeName(RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord: return "DWord";
                case RegistryValueKind.QWord: return "QWord";
                case RegistryValueKind.Binary: return "Binary";
                case RegistryValueKind.MultiString: return "MultiString";
                case RegistryValueKind.ExpandString: return "ExpandString";
                default: return "String";
            }
        }

        private static object ToRegistryValue(string text, RegistryValueKind kind)
        {
            string s = text == null ? "" : text;
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    {
                        uint u;
                        if (uint.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out u))
                            return unchecked((int)u);
                        int i;
                        if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out i)) return i;
                        return 0;
                    }
                case RegistryValueKind.QWord:
                    {
                        long l;
                        if (long.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) return l;
                        return (long)0;
                    }
                case RegistryValueKind.Binary:
                    return ParseBytes(s);
                case RegistryValueKind.MultiString:
                    return s.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                default:
                    return s;
            }
        }

        private static byte[] ParseBytes(string text)
        {
            List<byte> list = new List<byte>();
            if (text == null) return list.ToArray();
            string[] parts = text.Split(new char[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int b;
                if (int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out b))
                    list.Add(unchecked((byte)b));
            }
            return list.ToArray();
        }

        /// <summary>Reads one value. Returns false when the path cannot be parsed/opened.</summary>
        private static bool CaptureValue(string path, string name, out bool existed,
            out RegistryValueKind kind, out string text)
        {
            existed = false;
            kind = RegistryValueKind.String;
            text = "";
            RegistryHive hive;
            string sub;
            if (!SplitPath(path, out hive, out sub)) return false;
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    if (root == null) return false;
                    using (RegistryKey key = root.OpenSubKey(sub, false))
                    {
                        if (key == null) return true;
                        RegistryValueKind k;
                        string t;
                        if (!ReadFromKey(key, name, out k, out t)) return true;
                        existed = true;
                        kind = k;
                        text = t;
                    }
                }
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// Writes (or deletes, when <paramref name="text"/> is the RemoveEntry sentinel) one value.
        /// The previous value is pushed onto the rollback trail before the write happens.
        /// </summary>
        private static bool WriteValueAt(string path, string name, string text, string type,
            List<UndoStep> trail, Action<string> log)
        {
            RegistryHive hive;
            string sub;
            if (!SplitPath(path, out hive, out sub))
            {
                Say(log, L.F("Invalid registry path: {0}", path));
                return false;
            }

            bool existed;
            RegistryValueKind kind;
            string current;
            CaptureValue(path, name, out existed, out kind, out current);
            int index = -1;
            if (trail != null)
            {
                UndoStep step = new UndoStep();
                step.Path = path;
                step.Name = name;
                step.Type = TypeName(kind);
                step.Existed = existed;
                step.Value = current;
                step.Label = Display(path, name);
                trail.Add(step);
                index = trail.Count - 1;
            }

            bool remove = string.Equals(text, Tweak.RemoveEntry, StringComparison.Ordinal);
            try
            {
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    if (root == null)
                    {
                        DropStep(trail, index);
                        return false;
                    }
                    if (remove)
                    {
                        using (RegistryKey key = root.OpenSubKey(sub, true))
                        {
                            if (key != null) key.DeleteValue(name, false);
                        }
                        Say(log, L.F("Removed {0}", Display(path, name)));
                        return true;
                    }
                    using (RegistryKey key = root.CreateSubKey(sub))
                    {
                        if (key == null)
                        {
                            Say(log, L.F("Could not open {0}", path));
                            DropStep(trail, index);
                            return false;
                        }
                        RegistryValueKind k = ToKind(type);
                        key.SetValue(name, ToRegistryValue(text, k), k);
                    }
                }
                Say(log, L.F("Set {0} = {1}", Display(path, name), OneLine(text)));
                return true;
            }
            catch (Exception ex)
            {
                Say(log, L.F("Could not write {0}: {1}", Display(path, name), ex.Message));
                DropStep(trail, index);
                return false;
            }
        }

        /// <summary>A failed write changed nothing, so its trail entry must not be replayed.</summary>
        private static void DropStep(List<UndoStep> trail, int index)
        {
            if (trail == null || index < 0 || index >= trail.Count) return;
            trail.RemoveAt(index);
        }

        // ---------------------------------------------------------- trail model

        /// <summary>One registry value as it was before this job touched it.</summary>
        private class UndoStep
        {
            public string Path = "";
            public string Name = "";
            public string Type = "String";
            public bool Existed;
            public string Value = "";
            public string Label = "";
        }

        private static void Rollback(List<UndoStep> trail, Action<string> log)
        {
            if (trail.Count == 0)
            {
                Say(log, L.T("No changes to roll back."));
                return;
            }
            Say(log, L.F("Rolling back {0} change(s)...", trail.Count));
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                UndoStep step = trail[i];
                string want = step.Existed ? (step.Value == null ? "" : step.Value) : Tweak.RemoveEntry;
                if (!WriteValueAt(step.Path, step.Name, want, step.Type, null, null))
                    Say(log, L.F("Rollback failed for {0}", step.Label));
            }
        }

        // =============================================================== detect

        public static TweakState Detect(Tweak t)
        {
            if (t == null) return TweakState.Unknown;
            int ok = 0;
            int bad = 0;

            RegOp[] reg = t.Registry ?? new RegOp[0];
            for (int i = 0; i < reg.Length; i++)
            {
                if (reg[i] == null) continue;
                bool match;
                bool known;
                try { known = ProbeRegistry(reg[i], out match); }
                catch { known = false; match = false; }
                if (!known) continue;
                if (match) ok++; else bad++;
            }

            SvcOp[] svc = t.Services ?? new SvcOp[0];
            for (int i = 0; i < svc.Length; i++)
            {
                if (svc[i] == null) continue;
                bool match;
                bool known;
                try { known = ProbeService(svc[i], out match); }
                catch { known = false; match = false; }
                if (!known) continue;
                if (match) ok++; else bad++;
            }

            TaskOp[] tsk = t.Tasks ?? new TaskOp[0];
            for (int i = 0; i < tsk.Length; i++)
            {
                if (tsk[i] == null) continue;
                bool match;
                bool known;
                try { known = ProbeTask(tsk[i], out match); }
                catch { known = false; match = false; }
                if (!known) continue;
                if (match) ok++; else bad++;
            }

            if (ok + bad == 0) return TweakState.Unknown;
            if (bad == 0) return TweakState.Applied;
            if (ok == 0) return TweakState.NotApplied;
            return TweakState.Mixed;
        }

        private static bool ProbeRegistry(RegOp op, out bool match)
        {
            match = false;
            bool existed;
            RegistryValueKind kind;
            string current;
            string path = op.Path;
            if (!string.IsNullOrEmpty(path) && path.Length > 2 &&
                path.StartsWith("HKU\\", StringComparison.OrdinalIgnoreCase) &&
                path.Length > 4 && path[3] == ':')
            {
                // tolerate a PowerShell drive prefix the generator did not strip
                path = path.Substring(0, 3) + path.Substring(4);
            }
            if (!CaptureValue(path, op.Name, out existed, out kind, out current)) return false;
            string want = op.Value ?? "";
            if (string.Equals(want, Tweak.RemoveEntry, StringComparison.Ordinal))
            {
                match = !existed;
                return true;
            }
            match = existed && ValueEquals(current, want, op.Type);
            return true;
        }

        private static bool ProbeService(SvcOp op, out bool match)
        {
            match = false;
            if (op == null || string.IsNullOrEmpty(op.Name)) return false;
            bool existed;
            RegistryValueKind kind;
            string start;
            if (!CaptureValue(ServicePath(op.Name), "Start", out existed, out kind, out start)) return false;
            if (!existed) return false;
            int code;
            if (!int.TryParse(start.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out code)) return false;
            bool delayed = false;
            string flag;
            if (CaptureValue(ServicePath(op.Name), "DelayedAutostart", out existed, out kind, out flag) && existed)
            {
                int d;
                if (int.TryParse(flag.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out d)) delayed = d == 1;
            }
            match = string.Equals(StartupName(code, delayed), NormalizeStartup(op.Startup),
                StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private static bool ProbeTask(TaskOp op, out bool match)
        {
            match = false;
            if (op == null || string.IsNullOrEmpty(op.Name)) return false;
            bool disabled;
            if (!TryTaskDisabled(op.Name, out disabled)) return false;
            string want = string.Equals(op.State, "Enabled", StringComparison.OrdinalIgnoreCase)
                ? "Enabled" : "Disabled";
            match = string.Equals(want, disabled ? "Disabled" : "Enabled", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private static bool ValueEquals(string current, string want, string type)
        {
            if (current == null) current = "";
            if (want == null) want = "";
            if (string.Equals(type, "DWord", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(type, "QWord", StringComparison.OrdinalIgnoreCase))
            {
                long a;
                long b;
                if (TryLong(current, out a) && TryLong(want, out b))
                    return a == b || unchecked((uint)a) == unchecked((uint)b);
            }
            return string.Equals(current.Trim(), want.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryLong(string text, out long value)
        {
            value = 0;
            if (text == null) return false;
            return long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        // ============================================================ services

        private static string ServicePath(string name)
        {
            return "HKLM\\SYSTEM\\CurrentControlSet\\Services\\" + name;
        }

        private static string NormalizeStartup(string startup)
        {
            if (string.IsNullOrEmpty(startup)) return "Manual";
            string s = startup.Trim();
            if (string.Equals(s, "Automatic", StringComparison.OrdinalIgnoreCase)) return "Automatic";
            if (string.Equals(s, "AutomaticDelayed", StringComparison.OrdinalIgnoreCase)) return "AutomaticDelayed";
            if (string.Equals(s, "Delayed", StringComparison.OrdinalIgnoreCase)) return "AutomaticDelayed";
            if (string.Equals(s, "Disabled", StringComparison.OrdinalIgnoreCase)) return "Disabled";
            if (string.Equals(s, "Boot", StringComparison.OrdinalIgnoreCase)) return "Boot";
            if (string.Equals(s, "System", StringComparison.OrdinalIgnoreCase)) return "System";
            return "Manual";
        }

        private static string StartupName(int start, bool delayed)
        {
            switch (start)
            {
                case 0: return "Boot";
                case 1: return "System";
                case 2: return delayed ? "AutomaticDelayed" : "Automatic";
                case 4: return "Disabled";
                default: return "Manual";
            }
        }

        /// <summary>Start-type code for a friendly name. AutomaticDelayed is Automatic plus a flag.</summary>
        private static int StartupCode(string startup)
        {
            string s = NormalizeStartup(startup);
            if (s == "Automatic" || s == "AutomaticDelayed") return 2;
            if (s == "Disabled") return 4;
            if (s == "Boot") return 0;
            if (s == "System") return 1;
            return 3;
        }

        private static bool ApplyService(SvcOp op, string startup, List<UndoStep> trail, Action<string> log)
        {
            if (op == null || string.IsNullOrEmpty(op.Name)) return true;
            string path = ServicePath(op.Name);
            string want = NormalizeStartup(startup);
            int code = StartupCode(want);
            if (!WriteValueAt(path, "Start", code.ToString(CultureInfo.InvariantCulture), "DWord", trail, log))
                return false;
            if (want == "AutomaticDelayed")
                WriteValueAt(path, "DelayedAutostart", "1", "DWord", trail, log);
            else
                WriteValueAt(path, "DelayedAutostart", Tweak.RemoveEntry, "DWord", trail, log);
            Say(log, L.F("SERVICE {0} = {1}", op.Name, want));
            return true;
        }

        // =============================================================== tasks

        private static Dictionary<string, bool> _taskMap;
        private static DateTime _taskStamp = DateTime.MinValue;
        private static readonly object _taskGate = new object();

        /// <summary>One cached schtasks query per detection/apply pass (5 s window).</summary>
        private static Dictionary<string, bool> TaskMap()
        {
            lock (_taskGate)
            {
                if (_taskMap != null && (DateTime.UtcNow - _taskStamp).TotalSeconds < 5.0) return _taskMap;
                _taskMap = QueryTasks();
                _taskStamp = DateTime.UtcNow;
                return _taskMap;
            }
        }

        private static Dictionary<string, bool> QueryTasks()
        {
            Dictionary<string, bool> map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string exe = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
                RunResult r = ProcRunner.Run(exe, "/Query /FO CSV /NH", 20, null, null);
                if (r == null || r.Output == null) return map;
                string[] lines = r.Output.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.Trim().Length == 0) continue;
                    if (line.StartsWith("INFO", StringComparison.OrdinalIgnoreCase)) continue;
                    string name = FirstCsvField(line);
                    if (name.Length == 0) continue;
                    bool disabled = line.IndexOf("\"Disabled\"", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    line.IndexOf("Devre D", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!map.ContainsKey(name)) map[name] = disabled;
                }
            }
            catch { }
            return map;
        }

        private static string FirstCsvField(string line)
        {
            if (line == null || line.Length == 0) return "";
            if (line[0] == '"')
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < line.Length; i++)
                {
                    if (line[i] == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; continue; }
                        break;
                    }
                    sb.Append(line[i]);
                }
                return sb.ToString();
            }
            int comma = line.IndexOf(',');
            return comma < 0 ? line.Trim() : line.Substring(0, comma).Trim();
        }

        private static bool TryTaskDisabled(string name, out bool disabled)
        {
            disabled = false;
            if (string.IsNullOrEmpty(name)) return false;
            Dictionary<string, bool> map;
            try { map = TaskMap(); }
            catch { return false; }
            if (map == null) return false;
            return map.TryGetValue(name, out disabled);
        }

        private static bool ApplyTask(TaskOp op, bool enable, Action<string> log)
        {
            if (op == null || string.IsNullOrEmpty(op.Name)) return true;
            try
            {
                string exe = Path.Combine(Environment.SystemDirectory, "schtasks.exe");
                string args = "/Change /TN \"" + op.Name + "\" " + (enable ? "/ENABLE" : "/DISABLE");
                RunResult r = ProcRunner.Run(exe, args, 60, null, null);
                if (r == null || !r.Success)
                {
                    Say(log, L.F("Could not change task {0}", op.Name));
                    return false;
                }
                Say(log, L.F("TASK {0} = {1}", op.Name, enable ? "Enabled" : "Disabled"));
                return true;
            }
            catch (Exception ex)
            {
                Say(log, L.F("Could not change task {0}: {1}", op.Name, ex.Message));
                return false;
            }
        }

        // ============================================================ snapshot

        private class SnapRec
        {
            public string Id = "";
            public string Path = "";
            public string Name = "";
            public string Type = "DWord";
            public string Value = "";
            public bool Existed;
            /// <summary>Value MineManager wrote when it applied the tweak (conflict check).</summary>
            public string Written = "";
        }

        private static List<SnapRec> _snapshot;
        private static readonly object _snapshotGate = new object();

        private static List<SnapRec> SnapshotRecords()
        {
            lock (_snapshotGate)
            {
                if (_snapshot != null) return _snapshot;
                _snapshot = new List<SnapRec>();
                string file = SnapshotPath;
                if (file.Length == 0 || !File.Exists(file)) return _snapshot;
                try
                {
                    string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (line == null || line.Length == 0 || line[0] == '#') continue;
                        string[] parts = line.Split('\t');
                        if (parts.Length < 6) continue;
                        SnapRec rec = new SnapRec();
                        rec.Id = parts[0];
                        rec.Path = parts[1];
                        rec.Name = parts[2];
                        rec.Existed = parts[3] == "1";
                        rec.Type = parts[4];
                        try { rec.Value = Encoding.UTF8.GetString(Convert.FromBase64String(parts[5])); }
                        catch { rec.Value = ""; }
                        if (parts.Length >= 7)
                        {
                            try { rec.Written = Encoding.UTF8.GetString(Convert.FromBase64String(parts[6])); }
                            catch { rec.Written = ""; }
                        }
                        if (!IsKnownTarget(rec.Id, rec.Path, rec.Name, rec.Type)) continue;
                        if (rec.Value.Length > 8192) continue;
                        _snapshot.Add(rec);
                    }
                }
                catch { }
                return _snapshot;
            }
        }

        private static HashSet<string> _allowedTargets;

        /// <summary>
        /// The snapshot file lives next to the EXE, which may be user-writable while the
        /// app runs elevated. Undo therefore only accepts rows that match a compiled
        /// RegOp or SvcOp of the same tweak; anything else in the file is ignored.
        /// </summary>
        private static bool IsKnownTarget(string id, string path, string name, string type)
        {
            if (_allowedTargets == null)
            {
                HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<Tweak> all = TweakCatalog.All();
                for (int i = 0; i < all.Count; i++)
                {
                    Tweak t = all[i];
                    RegOp[] reg = t.Registry ?? new RegOp[0];
                    for (int j = 0; j < reg.Length; j++)
                    {
                        if (reg[j] == null) continue;
                        set.Add(Key(t.Id, reg[j].Path, reg[j].Name, reg[j].Type));
                    }
                    SvcOp[] svc = t.Services ?? new SvcOp[0];
                    for (int j = 0; j < svc.Length; j++)
                    {
                        if (svc[j] == null) continue;
                        set.Add(Key(t.Id, ServicePath(svc[j].Name), "Start", "DWord"));
                    }
                }
                _allowedTargets = set;
            }
            return _allowedTargets.Contains(Key(id, path, name, type));
        }

        private static string Key(string id, string path, string name, string type)
        {
            return (id ?? "") + "|" + (path ?? "") + "|" + (name ?? "") + "|" + (type ?? "");
        }

        private static SnapRec FindSnapshot(string id, string path, string name)
        {
            List<SnapRec> all;
            try { all = SnapshotRecords(); }
            catch { return null; }
            for (int i = 0; i < all.Count; i++)
            {
                SnapRec rec = all[i];
                if (!string.Equals(rec.Id, id, StringComparison.Ordinal)) continue;
                if (!string.Equals(rec.Path, path, StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.Equals(rec.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                return rec;
            }
            return null;
        }

        /// <summary>
        /// Records the current registry values (and service Start values, which are
        /// registry values too) of this tweak before the first write. The whole file
        /// is rewritten and closed, so it is on disk before anything changes.
        /// </summary>
        private static void WriteSnapshot(Tweak t, Action<string> log)
        {
            string file = SnapshotPath;
            List<SnapRec> fresh = new List<SnapRec>();

            RegOp[] reg = t.Registry ?? new RegOp[0];
            for (int i = 0; i < reg.Length; i++)
            {
                if (reg[i] == null) continue;
                fresh.Add(CaptureRecord(t.Id, reg[i].Path, reg[i].Name));
            }
            SvcOp[] svc = t.Services ?? new SvcOp[0];
            for (int i = 0; i < svc.Length; i++)
            {
                if (svc[i] == null) continue;
                fresh.Add(CaptureRecord(t.Id, ServicePath(svc[i].Name), "Start"));
            }
            if (fresh.Count == 0) return;
            if (file.Length == 0)
            {
                Say(log, L.T("The undo snapshot path is unavailable; undo will use the built-in defaults."));
                return;
            }

            try
            {
                List<SnapRec> all = SnapshotRecords();
                lock (_snapshotGate)
                {
                    for (int i = 0; i < fresh.Count; i++)
                    {
                        SnapRec rec = fresh[i];
                        bool known = false;
                        for (int j = 0; j < all.Count; j++)
                        {
                            if (!string.Equals(all[j].Id, rec.Id, StringComparison.Ordinal)) continue;
                            if (!string.Equals(all[j].Path, rec.Path, StringComparison.OrdinalIgnoreCase)) continue;
                            if (!string.Equals(all[j].Name, rec.Name, StringComparison.OrdinalIgnoreCase)) continue;
                            known = true;
                            break;
                        }
                        // Keep the oldest record: it is the value from before MineManager
                        // ever touched this tweak, which is what undo should restore.
                        if (!known) all.Add(rec);
                    }

                    SaveSnapshotFile(all, file);
                }
                Say(log, L.F("Snapshot saved: {0}", file));
            }
            catch (Exception ex)
            {
                Say(log, L.F("The undo snapshot could not be saved: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Drops a tweak's records after a successful undo: it is back in its original
        /// state, so the next apply has to snapshot the values that exist at that time.
        /// </summary>
        private static void ForgetSnapshot(string id, Action<string> log)
        {
            string file = SnapshotPath;
            if (file.Length == 0) return;
            try
            {
                List<SnapRec> all = SnapshotRecords();
                lock (_snapshotGate)
                {
                    bool changed = false;
                    for (int i = all.Count - 1; i >= 0; i--)
                    {
                        if (!string.Equals(all[i].Id, id, StringComparison.Ordinal)) continue;
                        all.RemoveAt(i);
                        changed = true;
                    }
                    if (changed) SaveSnapshotFile(all, file);
                }
            }
            catch (Exception ex)
            {
                Say(log, L.F("The undo snapshot could not be updated: {0}", ex.Message));
            }
        }

        /// <summary>TSV: id, path, name, existed, type, base64 value, base64 written. Written whole and closed.</summary>
        private static void SaveSnapshotFile(List<SnapRec> all, string file)
        {
            if (all.Count == 0)
            {
                try { if (File.Exists(file)) File.Delete(file); }
                catch { }
                return;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("# MineManager undo snapshot v2 - id, path, name, existed, type, base64 value, base64 written");
            sb.Append(Environment.NewLine);
            for (int i = 0; i < all.Count; i++)
            {
                SnapRec rec = all[i];
                sb.Append(rec.Id).Append('\t');
                sb.Append(rec.Path).Append('\t');
                sb.Append(rec.Name).Append('\t');
                sb.Append(rec.Existed ? "1" : "0").Append('\t');
                sb.Append(rec.Type).Append('\t');
                sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(rec.Value ?? "")));
                sb.Append('\t');
                sb.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(rec.Written ?? "")));
                sb.Append(Environment.NewLine);
            }
            File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
        }

        private static SnapRec CaptureRecord(string id, string path, string name)
        {
            SnapRec rec = new SnapRec();
            rec.Id = id;
            rec.Path = path;
            rec.Name = name;
            bool existed;
            RegistryValueKind kind;
            string text;
            CaptureValue(path, name, out existed, out kind, out text);
            rec.Existed = existed;
            rec.Type = TypeName(kind);
            rec.Value = existed ? text : "";
            return rec;
        }

        private static string UndoValue(Tweak t, RegOp op)
        {
            SnapRec rec = FindSnapshot(t.Id, op.Path, op.Name);
            if (rec != null) return rec.Existed ? (rec.Value == null ? "" : rec.Value) : Tweak.RemoveEntry;
            if (string.IsNullOrEmpty(op.Original)) return Tweak.RemoveEntry;
            return op.Original;
        }

        /// <summary>
        /// After a verified apply, remember the value MineManager wrote for every target.
        /// Undo compares against this and never overwrites a value that changed since.
        /// </summary>
        private static void RecordWritten(Tweak t, Action<string> log)
        {
            string file = SnapshotPath;
            if (file.Length == 0) return;
            try
            {
                List<SnapRec> all = SnapshotRecords();
                lock (_snapshotGate)
                {
                    bool changed = false;
                    for (int i = 0; i < all.Count; i++)
                    {
                        SnapRec rec = all[i];
                        if (!string.Equals(rec.Id, t.Id, StringComparison.Ordinal)) continue;
                        bool existed;
                        RegistryValueKind kind;
                        string text;
                        CaptureValue(rec.Path, rec.Name, out existed, out kind, out text);
                        string written = existed ? (text ?? "") : Tweak.RemoveEntry;
                        if (string.Equals(rec.Written, written, StringComparison.Ordinal)) continue;
                        rec.Written = written;
                        changed = true;
                    }
                    if (changed) SaveSnapshotFile(all, file);
                }
            }
            catch (Exception ex)
            {
                Say(log, L.F("The undo snapshot could not be updated: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Conflict-checked undo: when the value no longer equals what MineManager wrote,
        /// the user (Windows, another tool) changed it in between — that newer value is
        /// not ours to overwrite. The step is skipped and reported instead.
        /// </summary>
        private static string ConflictNote(string id, string path, string name)
        {
            SnapRec rec = FindSnapshot(id, path, name);
            if (rec == null || string.IsNullOrEmpty(rec.Written)) return "";
            bool existed;
            RegistryValueKind kind;
            string current;
            CaptureValue(path, name, out existed, out kind, out current);
            string now = existed ? (current ?? "") : Tweak.RemoveEntry;
            if (string.Equals((now ?? "").Trim(), (rec.Written ?? "").Trim(), StringComparison.Ordinal))
                return "";
            return L.F("SKIPPED {0} - it changed after MineManager applied it (now: {1}, MineManager wrote: {2}). Left as it is.",
                Display(path, name), OneLine(now), OneLine(rec.Written));
        }

        private static string UndoType(Tweak t, RegOp op)
        {
            SnapRec rec = FindSnapshot(t.Id, op.Path, op.Name);
            if (rec != null && !string.IsNullOrEmpty(rec.Type)) return rec.Type;
            return op.Type;
        }

        private static string UndoStartup(Tweak t, SvcOp op)
        {
            SnapRec rec = FindSnapshot(t.Id, ServicePath(op.Name), "Start");
            if (rec != null && rec.Existed)
            {
                int code;
                if (int.TryParse(rec.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out code))
                {
                    bool delayed = false;
                    SnapRec flag = FindSnapshot(t.Id, ServicePath(op.Name), "DelayedAutostart");
                    int d;
                    if (flag != null && flag.Existed &&
                        int.TryParse(flag.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out d))
                        delayed = d == 1;
                    return StartupName(code, delayed);
                }
            }
            if (!string.IsNullOrEmpty(op.Original)) return NormalizeStartup(op.Original);
            return "Manual";
        }

        // ============================================================= preview

        /// <summary>Human readable dry-run lines shown in the confirmation dialog.</summary>
        public static List<string> Preview(Tweak t, bool undo)
        {
            List<string> lines = new List<string>();
            if (t == null) return lines;

            RegOp[] reg = t.Registry ?? new RegOp[0];
            for (int i = 0; i < reg.Length; i++)
            {
                RegOp op = reg[i];
                if (op == null) continue;
                string want = undo ? UndoValue(t, op) : op.Value;
                string type = undo ? UndoType(t, op) : op.Type;
                string target = Display(op.Path, op.Name);
                if (string.Equals(want, Tweak.RemoveEntry, StringComparison.Ordinal))
                {
                    lines.Add(L.F("DEL {0}", target));
                }
                else
                {
                    bool existed;
                    RegistryValueKind kind;
                    string current;
                    CaptureValue(op.Path, op.Name, out existed, out kind, out current);
                    lines.Add(L.F("SET {0} = {1} ({2}) [now: {3}]", target, OneLine(want), type,
                        existed ? OneLine(current) : L.T("not set")));
                }
            }

            SvcOp[] svc = t.Services ?? new SvcOp[0];
            for (int i = 0; i < svc.Length; i++)
            {
                if (svc[i] == null) continue;
                string want = undo ? UndoStartup(t, svc[i]) : NormalizeStartup(svc[i].Startup);
                lines.Add(L.F("SERVICE {0} = {1}", svc[i].Name, want));
            }

            TaskOp[] tsk = t.Tasks ?? new TaskOp[0];
            for (int i = 0; i < tsk.Length; i++)
            {
                if (tsk[i] == null) continue;
                string want = undo ? (string.IsNullOrEmpty(tsk[i].Original) ? "Enabled" : tsk[i].Original)
                                   : tsk[i].State;
                lines.Add(L.F("TASK {0} = {1}", tsk[i].Name, want));
            }

            string script = undo ? t.UndoScript : t.Script;
            if (script != null && script.Trim().Length > 0)
                lines.Add(L.F("RUN powershell: {0}", FirstCodeLine(script)));

            return lines;
        }

        // =============================================================== apply

        public static bool Apply(Tweak t, bool undo, Action<string> log, CancelFlag cancel)
        {
            if (t == null) return false;
            try
            {
                if (t.RequiresAdmin && !ProcRunner.IsElevated())
                {
                    Say(log, L.T("Administrator rights are required for this tweak. Run MineManager as administrator."));
                    return false;
                }
                if (Cancelled(cancel))
                {
                    Say(log, L.T("Cancelled"));
                    return false;
                }

                bool hasState = t.HasState;
                if (!undo && hasState) WriteSnapshot(t, log);

                List<UndoStep> trail = new List<UndoStep>();
                int conflicts = 0;

                // 1. registry
                RegOp[] reg = t.Registry ?? new RegOp[0];
                for (int i = 0; i < reg.Length; i++)
                {
                    if (reg[i] == null) continue;
                    if (Cancelled(cancel))
                    {
                        Say(log, L.T("Cancelled"));
                        Rollback(trail, log);
                        return false;
                    }
                    string want = undo ? UndoValue(t, reg[i]) : reg[i].Value;
                    string type = undo ? UndoType(t, reg[i]) : reg[i].Type;
                    if (undo)
                    {
                        string note = ConflictNote(t.Id, reg[i].Path, reg[i].Name);
                        if (note.Length > 0)
                        {
                            conflicts++;
                            Say(log, note);
                            continue;
                        }
                    }
                    if (!WriteValueAt(reg[i].Path, reg[i].Name, want, type, trail, log))
                    {
                        Rollback(trail, log);
                        return false;
                    }
                }

                // 2. services
                SvcOp[] svc = t.Services ?? new SvcOp[0];
                for (int i = 0; i < svc.Length; i++)
                {
                    if (svc[i] == null) continue;
                    if (Cancelled(cancel))
                    {
                        Say(log, L.T("Cancelled"));
                        Rollback(trail, log);
                        return false;
                    }
                    string want = undo ? UndoStartup(t, svc[i]) : svc[i].Startup;
                    if (undo)
                    {
                        string note = ConflictNote(t.Id, ServicePath(svc[i].Name), "Start");
                        if (note.Length > 0)
                        {
                            conflicts++;
                            Say(log, note);
                            continue;
                        }
                    }
                    if (!ApplyService(svc[i], want, trail, log))
                    {
                        Rollback(trail, log);
                        return false;
                    }
                }

                // 3. scheduled tasks
                TaskOp[] tsk = t.Tasks ?? new TaskOp[0];
                for (int i = 0; i < tsk.Length; i++)
                {
                    if (tsk[i] == null) continue;
                    if (Cancelled(cancel))
                    {
                        Say(log, L.T("Cancelled"));
                        Rollback(trail, log);
                        return false;
                    }
                    string want = undo ? (string.IsNullOrEmpty(tsk[i].Original) ? "Enabled" : tsk[i].Original)
                                       : tsk[i].State;
                    bool enable = !string.Equals(want, "Disabled", StringComparison.OrdinalIgnoreCase);
                    if (!ApplyTask(tsk[i], enable, log))
                    {
                        Rollback(trail, log);
                        return false;
                    }
                }

                // 4. PowerShell last: slowest and least reversible.
                string script = undo ? (t.UndoScript ?? "") : (t.Script ?? "");
                if (script.Trim().Length > 0)
                {
                    int timeout = t.TimeoutSec <= 0 ? 180 : t.TimeoutSec;
                    Say(log, L.F("Running PowerShell ({0}s timeout)...", timeout));
                    RunResult r = ProcRunner.RunPowerShell(script, timeout, log, cancel);
                    if (r == null || !r.Success)
                    {
                        if (r != null && r.TimedOut)
                            Say(log, L.F("Script timed out after {0}s - rolling back changes.", timeout));
                        else if (r != null && r.Cancelled)
                            Say(log, L.T("Cancelled"));
                        else
                            Say(log, L.F("Script failed (exit code {0}) - rolling back changes.",
                                r == null ? -1 : r.ExitCode));
                        Rollback(trail, log);
                        return false;
                    }
                }

                // 5. verify that the requested state was really reached
                if (hasState)
                {
                    TweakState state = TweakState.Unknown;
                    try { state = Detect(t); }
                    catch { state = TweakState.Unknown; }
                    bool reached = undo ? state != TweakState.Applied : state == TweakState.Applied;
                    if (!reached)
                    {
                        if (conflicts > 0)
                            Say(log, L.F("Verification differs because {0} step(s) were skipped (changed by something else).",
                                conflicts));
                        else
                        {
                            Say(log, L.F("Verification failed - detected state is {0}.", StateName(state)));
                            return false;
                        }
                    }
                }

                if (!undo && hasState) RecordWritten(t, log);
                if (undo && hasState) ForgetSnapshot(t.Id, log);
                if (t.NeedsRestart)
                    Say(log, L.T("A restart is required to finish this tweak."));
                return true;
            }
            catch (Exception ex)
            {
                Say(log, L.F("Error: {0}", ex.Message));
                return false;
            }
        }

        public static void ApplyBatch(IList<Tweak> items, bool undo, Action<string> log,
            Action<int, int, Tweak, bool> progress, CancelFlag cancel)
        {
            if (items == null || items.Count == 0) return;
            int total = items.Count;
            for (int i = 0; i < total; i++)
            {
                if (Cancelled(cancel)) break;
                Tweak t = items[i];
                bool ok = false;
                try
                {
                    if (t != null)
                    {
                        string title = Title(t);
                        Say(log, L.F(undo ? "Restoring: {0}" : "Applying: {0}", title));
                        ok = Apply(t, undo, log, cancel);
                        Say(log, L.F(ok ? "Done: {0}" : "Failed: {0}", title));
                    }
                }
                catch (Exception ex)
                {
                    ok = false;
                    Say(log, L.F("Error: {0}", ex.Message));
                }
                if (progress != null)
                {
                    try { progress(i + 1, total, t, ok); }
                    catch { }
                }
            }
        }

        /// <summary>True when at least one selected tweak has a probe-able applied state.</summary>
        public static bool AnyApplied(IList<Tweak> items)
        {
            if (items == null) return false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                try
                {
                    TweakState s = Detect(items[i]);
                    if (s == TweakState.Applied || s == TweakState.Mixed) return true;
                }
                catch { }
            }
            return false;
        }

        public static string SnapshotPath
        {
            get
            {
                try
                {
                    return System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath),
                        "MineManager.undo.tsv");
                }
                catch { return ""; }
            }
        }

        // =============================================================== utils

        private static bool Cancelled(CancelFlag cancel)
        {
            return cancel != null && cancel.Cancelled;
        }

        private static void Say(Action<string> log, string text)
        {
            if (log == null || text == null) return;
            try { log(text); }
            catch { }
        }

        private static string Title(Tweak t)
        {
            if (t == null) return "";
            if (t.Title == null || t.Title.Length == 0) return t.Id;
            return L.T(t.Title);
        }

        private static string StateName(TweakState state)
        {
            switch (state)
            {
                case TweakState.Applied: return L.T("Applied");
                case TweakState.NotApplied: return L.T("Not applied");
                case TweakState.Mixed: return L.T("Mixed");
                default: return L.T("Unknown");
            }
        }

        /// <summary>Single-line, length-capped text for the log and the plan dialog.</summary>
        private static string OneLine(string text)
        {
            if (text == null) return "";
            string s = text.Replace("\r\n", " | ").Replace('\r', ' ').Replace('\n', ' ');
            s = s.Trim();
            if (s.Length > 160) s = s.Substring(0, 159) + "…";
            return s;
        }

        /// <summary>First executable line of a PowerShell block (comments and blanks skipped).</summary>
        private static string FirstCodeLine(string script)
        {
            if (script == null) return "";
            string[] lines = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                if (line.Length > 90) line = line.Substring(0, 89) + "…";
                return line;
            }
            return "";
        }
    }

    /// <summary>Survives page rebuilds (language/theme switch) so selections and the log are kept.</summary>
    internal static class TweakSession
    {
        public static readonly HashSet<string> Selected = new HashSet<string>(StringComparer.Ordinal);
        public static readonly List<string> Log = new List<string>();
        public static bool IncludeAdvanced;

        public static void ClearLog()
        {
            lock (Log) { Log.Clear(); }
        }
    }
}
