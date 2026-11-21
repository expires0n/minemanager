using System;
using System.Collections.Generic;

namespace MineManager
{
    /// <summary>
    /// Port of the Sift4 string-similarity used by Bulk Crap Uninstaller
    /// (UninstallTools/Junk/Confidence/ConfidenceGenerators.cs).
    /// Returns -1 when names are not related, 0..1 for an almost exact match,
    /// 2 when one name contains the other, and higher for fuzzy matches.
    /// </summary>
    internal static class Sift
    {
        /// <summary>Simplest Sift4 distance (maxOffset = 1, matching BCU).</summary>
        public static int Distance(string s1, string s2, int maxOffset)
        {
            if (string.IsNullOrEmpty(s1)) return string.IsNullOrEmpty(s2) ? 0 : s2.Length;
            if (string.IsNullOrEmpty(s2)) return s1.Length;

            int l1 = s1.Length, l2 = s2.Length;
            int c1 = 0, c2 = 0, lcss = 0, localCss = 0, transpositions = 0;
            double offset = maxOffset <= 0 ? 1 : maxOffset;

            while (c1 < l1 && c2 < l2)
            {
                if (s1[c1] == s2[c2]) { localCss++; }
                else
                {
                    lcss += localCss;
                    localCss = 0;
                    if (c1 != c2) { c1 = c2 = Math.Min(c1, c2); }
                    for (int i = 0; i < offset && (c1 + i < l1 || c2 + i < l2); i++)
                    {
                        if (c1 + i < l1 && c2 + i < l2 && s1[c1 + i] == s2[c2 + i])
                        {
                            localCss++;
                            transpositions++;
                            continue;
                        }
                        if (c1 + i < l1 && c2 + i + 1 < l2 && s1[c1 + i] == s2[c2 + i + 1])
                        {
                            localCss++;
                            transpositions++;
                            c1 += i;
                            break;
                        }
                        if (c1 + i + 1 < l1 && c2 + i < l2 && s1[c1 + i + 1] == s2[c2 + i])
                        {
                            localCss++;
                            transpositions++;
                            c2 += i;
                            break;
                        }
                    }
                }
                c1++;
                c2++;
            }
            lcss += localCss;
            return Math.Max(l1, l2) - lcss + transpositions;
        }

        /// <summary>Normalizes a product or folder name for comparison.</summary>
        public static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            string t = s.Replace('_', ' ').Replace('.', ' ').Trim().ToLowerInvariant();
            while (t.IndexOf("  ", StringComparison.Ordinal) >= 0)
                t = t.Replace("  ", " ");
            return t;
        }

        /// <summary>
        /// BCU-compatible product/folder match: -1 = no match, 0..n = match quality.
        /// Short names are never matched and a huge difference is rejected.
        /// </summary>
        public static int Match(string productName, string itemName, string publisher)
        {
            string name = Norm(productName);
            string item = Norm(itemName);
            if (name.Length == 0 || item.Length == 0) return -1;
            int lowest = Math.Min(name.Length, item.Length);
            if (lowest <= 4) return -1;

            int d = Distance(name, item, 1);
            if (d <= 1) return d;

            string pub = Norm(publisher);
            if (pub.Length > 4 && name.IndexOf(pub, StringComparison.Ordinal) >= 0)
            {
                string trimmed = name.Replace(pub, "").Trim();
                if (trimmed.Length > 4)
                {
                    int td = Distance(trimmed, item, 1);
                    if (td <= 1) return td;
                }
            }

            if (item.IndexOf(name, StringComparison.Ordinal) >= 0 ||
                name.IndexOf(item, StringComparison.Ordinal) >= 0) return 2;

            if (d < lowest / 3) return d;
            return -1;
        }

        /// <summary>Splits a name into lowercase word tokens (>= 3 chars).</summary>
        public static List<string> Tokens(string s)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrEmpty(s)) return list;
            string t = Norm(s);
            string[] parts = t.Split(new char[] { ' ', '-', '/', '\\', ',', '(', ')', '[', ']', '+', '&', ':' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Length >= 3) list.Add(parts[i]);
            return list;
        }
    }
}
