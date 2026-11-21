using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MineManager
{
    internal static class Paths
    {
        public static string Win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        public static string Sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
        public static string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        public static string ProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        public static string CommonFiles = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public static string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        public static string UserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public static string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        public static string StartMenuCommon = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu);
        public static string StartMenuUser = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
        public static string Documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public static string Combine(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b;
            if (string.IsNullOrEmpty(b)) return a;
            return Path.Combine(a, b);
        }

        public static bool Exists(string p)
        {
            try { return !string.IsNullOrEmpty(p) && (File.Exists(p) || Directory.Exists(p)); }
            catch { return false; }
        }

        public static bool LooksLikeSystemPath(string path)
        {
            try
            {
                string p = path.TrimEnd('\\').ToLowerInvariant();
                string[] bad = new string[]
                {
                    Paths.Win.TrimEnd('\\').ToLowerInvariant(),
                    Paths.Win.TrimEnd('\\').ToLowerInvariant() + "\\system32",
                    Paths.Win.TrimEnd('\\').ToLowerInvariant() + "\\syswow64",
                    Paths.ProgramFiles.TrimEnd('\\').ToLowerInvariant(),
                    Paths.ProgramFilesX86.TrimEnd('\\').ToLowerInvariant(),
                    Paths.CommonFiles.TrimEnd('\\').ToLowerInvariant(),
                    Paths.AppData.TrimEnd('\\').ToLowerInvariant(),
                    Paths.LocalAppData.TrimEnd('\\').ToLowerInvariant(),
                    Paths.UserProfile.TrimEnd('\\').ToLowerInvariant(),
                    Paths.Documents.TrimEnd('\\').ToLowerInvariant(),
                    Paths.Desktop.TrimEnd('\\').ToLowerInvariant()
                };
                foreach (string b in bad) if (p == b) return true;
                return false;
            }
            catch { return true; }
        }
    }

    internal static class FileTools
    {
        /// <summary>Recursive folder size. Never throws.</summary>
        public static long DirSize(string root, out int files)
        {
            files = 0;
            long total = 0;
            if (!Directory.Exists(root)) return 0;
            Stack<string> stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                try
                {
                    FileAttributes attr = File.GetAttributes(dir);
                    if ((attr & FileAttributes.ReparsePoint) != 0) continue;
                }
                catch { continue; }

                try
                {
                    string[] fs = Directory.GetFiles(dir);
                    for (int i = 0; i < fs.Length; i++)
                    {
                        try
                        {
                            FileInfo fi = new FileInfo(fs[i]);
                            total += fi.Length;
                            files++;
                        }
                        catch { }
                    }
                }
                catch { }

                try
                {
                    string[] subs = Directory.GetDirectories(dir);
                    for (int i = 0; i < subs.Length; i++) stack.Push(subs[i]);
                }
                catch { }
            }
            return total;
        }

        /// <summary>Deletes everything inside a folder. Keeps the folder itself.</summary>
        public static long EmptyFolder(string root, out int deleted, out int errors)
        {
            deleted = 0;
            errors = 0;
            long freed = 0;
            if (!Directory.Exists(root)) return 0;

            List<string> dirs = new List<string>();
            Stack<string> stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                try
                {
                    FileAttributes attr = File.GetAttributes(dir);
                    if ((attr & FileAttributes.ReparsePoint) != 0) continue;
                }
                catch { continue; }
                dirs.Add(dir);

                try
                {
                    string[] fs = Directory.GetFiles(dir);
                    for (int i = 0; i < fs.Length; i++)
                    {
                        string f = fs[i];
                        try
                        {
                            long len = 0;
                            try { len = new FileInfo(f).Length; } catch { }
                            File.SetAttributes(f, FileAttributes.Normal);
                            File.Delete(f);
                            freed += len;
                            deleted++;
                        }
                        catch
                        {
                            errors++;
                            try { Native.MoveFileEx(f, null, Native.MOVEFILE_DELAY_UNTIL_REBOOT); } catch { }
                        }
                    }
                }
                catch { errors++; }

                try
                {
                    string[] subs = Directory.GetDirectories(dir);
                    for (int i = 0; i < subs.Length; i++) stack.Push(subs[i]);
                }
                catch { errors++; }
            }

            // deepest first
            for (int i = dirs.Count - 1; i >= 0; i--)
            {
                if (string.Equals(dirs[i].TrimEnd('\\'), root.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    continue;
                try { Directory.Delete(dirs[i], false); }
                catch { }
            }
            return freed;
        }

        public static long DeletePath(string path, out int errors)
        {
            errors = 0;
            try
            {
                if (File.Exists(path))
                {
                    long len = 0;
                    try { len = new FileInfo(path).Length; } catch { }
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                    return len;
                }
                if (Directory.Exists(path))
                {
                    int d, e;
                    long freed = EmptyFolder(path, out d, out e);
                    errors += e;
                    try
                    {
                        File.SetAttributes(path, FileAttributes.Normal);
                        Directory.Delete(path, true);
                    }
                    catch { errors++; }
                    return freed;
                }
            }
            catch { errors++; }
            return 0;
        }

        public static long FilePatternSize(string dir, string pattern, out int count)
        {
            count = 0;
            long total = 0;
            try
            {
                if (!Directory.Exists(dir)) return 0;
                string[] fs = Directory.GetFiles(dir, pattern);
                for (int i = 0; i < fs.Length; i++)
                {
                    try { total += new FileInfo(fs[i]).Length; count++; } catch { }
                }
            }
            catch { }
            return total;
        }

        public static long DeletePattern(string dir, string pattern, out int errors)
        {
            errors = 0;
            long freed = 0;
            try
            {
                if (!Directory.Exists(dir)) return 0;
                string[] fs = Directory.GetFiles(dir, pattern);
                for (int i = 0; i < fs.Length; i++)
                {
                    try
                    {
                        long len = new FileInfo(fs[i]).Length;
                        File.SetAttributes(fs[i], FileAttributes.Normal);
                        File.Delete(fs[i]);
                        freed += len;
                    }
                    catch { errors++; }
                }
            }
            catch { errors++; }
            return freed;
        }
    }
}
