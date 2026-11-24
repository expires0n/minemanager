using System;
using System.Collections.Generic;
using System.IO;

namespace MineManager
{
    internal class CleanTarget
    {
        public string Name;
        public string Note;
        public bool DefaultChecked = true;
        public bool IsRecycleBin;
        public List<string> Folders = new List<string>();
        public List<string[]> Patterns = new List<string[]>();

        public long Size;
        public int Files;
        public int Errors;
        public bool Measured;
        public string Status = "";
        /// <summary>UTC time of the scan; files that changed after it are not deleted.</summary>
        public DateTime ScannedAtUtc = DateTime.MinValue;

        public CleanTarget(string name, string note)
        {
            Name = name;
            Note = note;
        }

        public CleanTarget Folder(string path)
        {
            if (!string.IsNullOrEmpty(path)) Folders.Add(path);
            return this;
        }

        public CleanTarget Pattern(string dir, string pattern)
        {
            if (!string.IsNullOrEmpty(dir)) Patterns.Add(new string[] { dir, pattern });
            return this;
        }

        public string SizeText
        {
            get { return Measured ? Fmt.Size(Size) : "-"; }
        }
    }

    internal static class Cleaner
    {
        private const int MaxBrowserDepth = 5;

        public static List<CleanTarget> BuildTargets()
        {
            string temp = Path.GetTempPath();
            string local = Paths.LocalAppData;
            string windir = Paths.Win;

            List<CleanTarget> list = new List<CleanTarget>();

            CleanTarget t1 = new CleanTarget(L.T("User temporary files"), "%TEMP%");
            t1.Folder(temp);
            t1.Folder(Path.Combine(local, "Temp"));
            list.Add(t1);

            CleanTarget t2 = new CleanTarget(L.T("Windows temporary files"), "Windows\\Temp");
            t2.Folder(Path.Combine(windir, "Temp"));
            list.Add(t2);

            CleanTarget t3 = new CleanTarget(L.T("Windows Update cache"), "SoftwareDistribution\\Download");
            t3.Note = L.T("Downloaded update files");
            t3.Folder(Path.Combine(windir, "SoftwareDistribution\\Download"));
            list.Add(t3);

            CleanTarget t4 = new CleanTarget(L.T("Delivery Optimization cache"), "Delivery Optimization");
            t4.DefaultChecked = false;
            t4.Folder(Path.Combine(windir, "SoftwareDistribution\\DeliveryOptimization"));
            t4.Folder(Path.Combine(windir, "ServiceProfiles\\NetworkService\\AppData\\Local\\Microsoft\\Windows\\DeliveryOptimization\\Cache"));
            t4.Folder(Path.Combine(Paths.CommonFiles, "Microsoft\\Network\\Downloader"));
            list.Add(t4);

            CleanTarget t5 = new CleanTarget(L.T("Prefetch data"), "Windows\\Prefetch");
            t5.Note = L.T("First launches may be a little slower");
            t5.DefaultChecked = false;
            t5.Folder(Path.Combine(windir, "Prefetch"));
            list.Add(t5);

            CleanTarget t6 = new CleanTarget(L.T("Recycle Bin"), L.T("All drives"));
            t6.IsRecycleBin = true;
            list.Add(t6);

            CleanTarget t7 = new CleanTarget(L.T("Browser caches"), "Chrome, Edge, Brave, Firefox");
            t7.Note = L.T("Browsers must be closed");
            AddBrowserCaches(t7);
            list.Add(t7);

            CleanTarget t8 = new CleanTarget(L.T("Error reports (WER)"), "Windows Error Reporting");
            t8.Folder(Path.Combine(Paths.CommonFiles, "Microsoft\\Windows\\WER\\ReportQueue"));
            t8.Folder(Path.Combine(Paths.CommonFiles, "Microsoft\\Windows\\WER\\ReportArchive"));
            t8.Folder(Path.Combine(Paths.CommonFiles, "Microsoft\\Windows\\WER\\Temp"));
            t8.Folder(Path.Combine(local, "Microsoft\\Windows\\WER"));
            list.Add(t8);

            CleanTarget t9 = new CleanTarget(L.T("Crash dumps"), "CrashDumps / Minidump");
            t9.Folder(Path.Combine(local, "CrashDumps"));
            t9.Folder(Path.Combine(windir, "Minidump"));
            t9.Folder(Path.Combine(windir, "LiveKernelReports"));
            list.Add(t9);

            CleanTarget t10 = new CleanTarget(L.T("Windows logs"), "CBS, DISM, WindowsUpdate");
            t10.Folder(Path.Combine(windir, "Logs\\CBS"));
            t10.Folder(Path.Combine(windir, "Logs\\DISM"));
            t10.Folder(Path.Combine(windir, "Logs\\WindowsUpdate"));
            t10.Folder(Path.Combine(windir, "Logs\\SIH"));
            list.Add(t10);

            CleanTarget t11 = new CleanTarget(L.T("Thumbnail cache"), "thumbcache / iconcache");
            t11.Note = L.T("Explorer must be restarted");
            string explorer = Path.Combine(local, "Microsoft\\Windows\\Explorer");
            t11.Pattern(explorer, "thumbcache_*.db");
            t11.Pattern(explorer, "iconcache_*.db");
            list.Add(t11);

            CleanTarget t12 = new CleanTarget(L.T("INetCache / Web cache"), "Internet Explorer / WinINet");
            t12.Folder(Path.Combine(local, "Microsoft\\Windows\\INetCache"));
            t12.Folder(Path.Combine(local, "Microsoft\\Windows\\WebCache"));
            t12.Folder(Path.Combine(local, "Microsoft\\Windows\\Temporary Internet Files"));
            list.Add(t12);

            CleanTarget t13 = new CleanTarget(L.T("Setup leftovers"), L.T("setup / MSI cache"));
            t13.DefaultChecked = false;
            t13.Folder(Path.Combine(windir, "Panther"));
            t13.Folder(Path.Combine(local, "Downloaded Installations"));
            t13.Folder(Path.Combine(windir, "Installer\\$PatchCache$\\"));
            list.Add(t13);

            return list;
        }

        private static void AddBrowserCaches(CleanTarget t)
        {
            string local = Paths.LocalAppData;
            string roaming = Paths.AppData;

            string[] browsers = new string[]
            {
                Path.Combine(local, "Google\\Chrome\\User Data"),
                Path.Combine(local, "Microsoft\\Edge\\User Data"),
                Path.Combine(local, "BraveSoftware\\Brave-Browser\\User Data"),
                Path.Combine(local, "Vivaldi\\User Data"),
                Path.Combine(local, "Yandex\\YandexBrowser\\User Data"),
                Path.Combine(local, "Opera Software\\Opera Stable"),
                Path.Combine(local, "Opera Software\\Opera GX Stable"),
                Path.Combine(roaming, "Opera Software\\Opera Stable"),
                Path.Combine(roaming, "Opera Software\\Opera GX Stable")
            };

            foreach (string b in browsers)
            {
                if (!Directory.Exists(b)) continue;
                FindCacheDirs(b, 0, t);
            }

            // Firefox
            string ffProfiles = Path.Combine(local, "Mozilla\\Firefox\\Profiles");
            try
            {
                if (Directory.Exists(ffProfiles))
                {
                    foreach (string prof in Directory.GetDirectories(ffProfiles))
                    {
                        t.Folder(Path.Combine(prof, "cache2"));
                        t.Folder(Path.Combine(prof, "startupCache"));
                        t.Folder(Path.Combine(prof, "thumbnails"));
                    }
                }
            }
            catch { }
        }

        private static void FindCacheDirs(string root, int depth, CleanTarget t)
        {
            if (depth > MaxBrowserDepth) return;
            string name;
            try { name = Path.GetFileName(root).ToLowerInvariant(); }
            catch { return; }

            if (name == "cache" || name == "code cache" || name == "gpucache" ||
                name == "shadercache" || name == "dawncache" || name == "grshadercache" ||
                name == "scriptcache" || name == "cachestorage")
                t.Folder(root);

            try
            {
                string[] subs = Directory.GetDirectories(root);
                for (int i = 0; i < subs.Length; i++) FindCacheDirs(subs[i], depth + 1, t);
            }
            catch { }
        }

        public static void Measure(CleanTarget t)
        {
            long size = 0;
            int files = 0;
            t.ScannedAtUtc = DateTime.UtcNow;

            if (t.IsRecycleBin)
            {
                long items;
                size = Native.RecycleBinSize(out items);
                files = (int)items;
            }
            else
            {
                for (int i = 0; i < t.Folders.Count; i++)
                {
                    int f;
                    size += FileTools.DirSize(t.Folders[i], out f);
                    files += f;
                }
                for (int i = 0; i < t.Patterns.Count; i++)
                {
                    int c;
                    size += FileTools.FilePatternSize(t.Patterns[i][0], t.Patterns[i][1], out c);
                    files += c;
                }
            }

            t.Size = size;
            t.Files = files;
            t.Measured = true;
            t.Status = size == 0 ? L.T("Already clean") : L.Count(files, "file", "files", "dosya");
        }

        /// <summary>Returns freed bytes; updates t.Size/t.Status.</summary>
        public static long Clean(CleanTarget t)
        {
            long freed = 0;
            int errors = 0;
            int skipped = 0;

            try
            {
                if (t.IsRecycleBin)
                {
                    long before = t.Size;
                    int hr = Native.SHEmptyRecycleBin(IntPtr.Zero, null,
                        Native.SHERB_NOCONFIRMATION | Native.SHERB_NOPROGRESSUI | Native.SHERB_NOSOUND);
                    freed = hr == 0 ? before : 0;
                    if (hr != 0) errors++;
                }
                else
                {
                    for (int i = 0; i < t.Folders.Count; i++)
                    {
                        int d, e, s;
                        freed += FileTools.EmptyFolder(t.Folders[i], t.ScannedAtUtc, out d, out e, out s);
                        errors += e;
                        skipped += s;
                    }
                    for (int i = 0; i < t.Patterns.Count; i++)
                    {
                        int e, s;
                        freed += FileTools.DeletePattern(t.Patterns[i][0], t.Patterns[i][1],
                            t.ScannedAtUtc, out e, out s);
                        errors += e;
                        skipped += s;
                    }
                }
            }
            catch { errors++; }

            t.Errors = errors;
            Measure(t);
            if (skipped > 0)
                t.Status = L.F("Cleaned · {0} files changed since the scan were left alone",
                    L.Count(skipped, "file", "files", "dosya"));
            else
                t.Status = errors > 0
                    ? L.F("Partially deleted ({0} items in use)", errors)
                    : (freed > 0 ? L.T("Cleaned") : L.T("Already clean"));
            return freed;
        }
    }
}
