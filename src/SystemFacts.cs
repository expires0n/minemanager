using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MineManager
{
    internal class SystemFactsData
    {
        public string OsLine = "";
        public string BuildLine = "";
        public string UptimeLine = "";
        public string DiskLine = "";
        public string SessionLine = "";
        public bool Admin;
    }

    /// <summary>
    /// Small, read-only facts for the diagnostics overview. Everything is read
    /// from documented sources (the CurrentVersion key, DriveInfo, the process
    /// token and GetTickCount64); nothing is changed.
    /// </summary>
    internal static class SystemFacts
    {
        [DllImport("kernel32.dll")]
        private static extern ulong GetTickCount64();

        public static SystemFactsData Read()
        {
            SystemFactsData d = new SystemFactsData();
            d.Admin = ProcRunner.IsElevated();

            string product = "";
            string display = "";
            string build = "";
            string ubr = "";
            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (k != null)
                    {
                        product = Str(k.GetValue("ProductName"));
                        display = Str(k.GetValue("DisplayVersion"));
                        build = Str(k.GetValue("CurrentBuildNumber"));
                        ubr = Str(k.GetValue("UBR"));
                    }
                }
            }
            catch { }

            int buildNo;
            if (int.TryParse(build, out buildNo) && buildNo >= 22000 &&
                product.StartsWith("Windows 10", StringComparison.OrdinalIgnoreCase))
                product = "Windows 11" + product.Substring("Windows 10".Length);

            if (product.Length == 0) product = "Windows";
            d.OsLine = product;
            if (display.Length > 0) d.OsLine += " · " + display;
            if (build.Length > 0) d.OsLine += " · " + build;
            d.BuildLine = L.T("Build") + " " + build +
                (ubr.Length > 0 ? "." + ubr : "") + " · " +
                (Environment.Is64BitOperatingSystem ? "x64" : "x86");

            TimeSpan up;
            try { up = TimeSpan.FromMilliseconds(GetTickCount64()); }
            catch { up = TimeSpan.FromMilliseconds((uint)Environment.TickCount); }
            d.UptimeLine = L.T("Uptime") + " " + ShortSpan(up);

            try
            {
                string root = Path.GetPathRoot(Environment.SystemDirectory);
                if (string.IsNullOrEmpty(root)) root = "C:\\";
                DriveInfo di = new DriveInfo(root);
                d.DiskLine = L.F("{0} {1} free / {2}", root.TrimEnd('\\'),
                    Fmt.Size(di.AvailableFreeSpace), Fmt.Size(di.TotalSize));
            }
            catch { d.DiskLine = L.T("System drive unavailable"); }

            d.SessionLine = (d.Admin ? L.T("Administrator") : L.T("Standard user")) +
                " · " + (Environment.Is64BitProcess ? "x64" : "x86");
            return d;
        }

        private static string Str(object o)
        {
            return o == null ? "" : o.ToString().Trim();
        }

        private static string ShortSpan(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            if (t.TotalDays >= 1) return L.F("{0}d {1}h {2}m", (int)t.TotalDays, t.Hours, t.Minutes);
            if (t.TotalHours >= 1) return L.F("{0}h {1}m", (int)t.TotalHours, t.Minutes);
            return L.F("{0}m", Math.Max(0, t.Minutes));
        }
    }
}
