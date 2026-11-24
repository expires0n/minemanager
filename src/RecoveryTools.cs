using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MineManager
{
    /// <summary>
    /// One Windows repair command. The switches are the documented forms from
    /// Microsoft Learn: DISM /Cleanup-Image (CheckHealth, ScanHealth, RestoreHealth),
    /// sfc /scannow and chkdsk /scan. Nothing here is guessed.
    /// </summary>
    internal class RecoveryStep
    {
        public string Name = "";
        public string Exe = "";
        public string Args = "";
        public int TimeoutSec = 3600;
        public bool DiskScan;

        public string Command
        {
            get { return Path.GetFileName(Exe) + " " + Args; }
        }
    }

    internal static class RecoveryTools
    {
        /// <summary>
        /// The order matches the documented repair flow: check the image, scan it,
        /// repair it, let SFC repair protected files, then scan the system drive.
        /// </summary>
        public static List<RecoveryStep> Steps()
        {
            string sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string drive = SystemDrive();
            List<RecoveryStep> list = new List<RecoveryStep>();

            list.Add(new RecoveryStep
            {
                Name = "Check the image for corruption",
                Exe = Path.Combine(sys, "dism.exe"),
                Args = "/Online /Cleanup-Image /CheckHealth",
                TimeoutSec = 900
            });
            list.Add(new RecoveryStep
            {
                Name = "Scan the image for damage",
                Exe = Path.Combine(sys, "dism.exe"),
                Args = "/Online /Cleanup-Image /ScanHealth",
                TimeoutSec = 3600
            });
            list.Add(new RecoveryStep
            {
                Name = "Repair the Windows image",
                Exe = Path.Combine(sys, "dism.exe"),
                Args = "/Online /Cleanup-Image /RestoreHealth",
                TimeoutSec = 3600
            });
            list.Add(new RecoveryStep
            {
                Name = "Repair protected system files",
                Exe = Path.Combine(sys, "sfc.exe"),
                Args = "/scannow",
                TimeoutSec = 3600
            });
            list.Add(new RecoveryStep
            {
                Name = "Scan the system drive",
                Exe = Path.Combine(sys, "chkdsk.exe"),
                Args = drive + " /scan",
                TimeoutSec = 3600,
                DiskScan = true
            });
            return list;
        }

        /// <summary>
        /// The encoding a repair tool writes when its console output is redirected.
        /// sfc.exe emits UTF-16LE; DISM and CHKDSK use the OEM code page. Decoding
        /// all three as UTF-8 makes SFC output unreadable NUL-separated noise.
        /// </summary>
        public static Encoding OutputEncoding(string exe)
        {
            string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
            try
            {
                if (name == "sfc") return new UnicodeEncoding(false, false, false);
                if (name == "dism" || name == "chkdsk")
                    return Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
            }
            catch { }
            return Encoding.UTF8;
        }

        /// <summary>The drive Windows is installed on, e.g. "C:".</summary>
        public static string SystemDrive()
        {
            try
            {
                string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (win.Length >= 2 && win[1] == ':') return win.Substring(0, 2);
            }
            catch { }
            return "C:";
        }
    }
}
