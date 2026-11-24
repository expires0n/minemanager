using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MineManager
{
    /// <summary>
    /// Hardware inventory diff: the previous reading is stored next to the EXE and the
    /// next reading is compared against it ("32 GB → 16 GB", "module not readable this
    /// time"). Blank values are never treated as a match — they mean "no data".
    /// </summary>
    internal static class HwSnapshot
    {
        private static string File
        {
            get
            {
                try
                {
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "MineManager-hardware.tsv");
                }
                catch { return ""; }
            }
        }

        /// <summary>True when a previous reading exists to compare against.</summary>
        public static bool HasPrevious
        {
            get
            {
                string file = File;
                try { return file.Length > 0 && System.IO.File.Exists(file); }
                catch { return false; }
            }
        }

        /// <summary>
        /// Compares the current reading with the stored one. Empty list = first reading
        /// (or nothing changed). Lines: "· changed", "+ appeared", "- missing".
        /// </summary>
        public static List<string> Compare(List<SpecSection> data)
        {
            List<string> diff = new List<string>();
            Dictionary<string, string> old = Load();
            if (old == null || old.Count == 0) return diff;

            Dictionary<string, string> now = Index(data);
            foreach (KeyValuePair<string, string> kv in now)
            {
                string prev;
                if (!old.TryGetValue(kv.Key, out prev))
                {
                    diff.Add(L.F("+ appeared: {0}", kv.Key + " = " + kv.Value));
                    continue;
                }
                if (string.Equals(Norm(prev), Norm(kv.Value), StringComparison.OrdinalIgnoreCase)) continue;
                if (Norm(prev).Length == 0)
                    diff.Add(L.F("+ data appeared: {0} = {1}", kv.Key, kv.Value));
                else if (Norm(kv.Value).Length == 0)
                    diff.Add(L.F("- no data this time: {0} (was: {1})", kv.Key, prev));
                else
                    diff.Add(L.F("· changed: {0} → {1}", kv.Key + " = " + prev, kv.Value));
            }
            foreach (KeyValuePair<string, string> kv in old)
            {
                if (now.ContainsKey(kv.Key)) continue;
                if (Norm(kv.Value).Length == 0) continue;
                diff.Add(L.F("- missing: {0} (was: {1})", kv.Key, kv.Value));
            }
            return diff;
        }

        /// <summary>Stores the reading for the next comparison. Never throws.</summary>
        public static void Save(List<SpecSection> data)
        {
            string file = File;
            if (file.Length == 0) return;
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("# MineManager hardware snapshot");
                sb.Append(Environment.NewLine);
                Dictionary<string, string> now = Index(data);
                foreach (KeyValuePair<string, string> kv in now)
                {
                    sb.Append(kv.Key.Replace('\t', ' '));
                    sb.Append('\t');
                    sb.Append((kv.Value ?? "").Replace('\t', ' '));
                    sb.Append(Environment.NewLine);
                }
                System.IO.File.WriteAllText(file, sb.ToString(), new UTF8Encoding(false));
            }
            catch { }
        }

        private static Dictionary<string, string> Index(List<SpecSection> data)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (data == null) return map;
            for (int i = 0; i < data.Count; i++)
            {
                SpecSection sec = data[i];
                if (sec == null) continue;
                for (int j = 0; j < sec.Rows.Count; j++)
                {
                    SpecRow r = sec.Rows[j];
                    if (r == null) continue;
                    map[sec.Title + " · " + r.Name] = r.Value ?? "";
                }
            }
            return map;
        }

        private static Dictionary<string, string> Load()
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string file = File;
            if (file.Length == 0 || !System.IO.File.Exists(file)) return map;
            try
            {
                string[] lines = System.IO.File.ReadAllLines(file, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line == null || line.Length == 0 || line[0] == '#') continue;
                    int tab = line.IndexOf('\t');
                    if (tab <= 0) continue;
                    map[line.Substring(0, tab)] = line.Substring(tab + 1);
                }
            }
            catch { }
            return map;
        }

        private static string Norm(string s)
        {
            return (s ?? "").Trim();
        }
    }
}