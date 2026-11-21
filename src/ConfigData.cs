using System;
using System.Collections.Generic;

namespace MineManager
{
    /// <summary>
    /// One row on the Config page: a Windows optional feature, a repair action,
    /// a legacy control panel applet, or a PowerShell/remote-access action.
    /// </summary>
    internal class ConfigItem
    {
        public string Id = "";
        /// <summary>features | fixes | panels | profile | remote</summary>
        public string Group = "fixes";
        public string Title = "";
        public string Desc = "";
        public string Link = "";
        public string ConfirmText = "";

        /// <summary>DISM feature names for group "features".</summary>
        public string[] Features = new string[0];

        /// <summary>Shell command run by the engine (never shown as-is in the UI).</summary>
        public string Command = "";
        /// <summary>Native handler id for actions that need C# code (e.g. "ntp", "systemrepair").</summary>
        public string Handler = "";

        public string TimeoutSec = "300";
        public bool Danger;
        public bool Interactive;
        public bool NeedsRestart;

        /// <summary>True when the engine can install/query DISM features for this row.</summary>
        public bool HasFeatures
        {
            get { return Features != null && Features.Length > 0; }
        }

        public int Timeout
        {
            get
            {
                int t = 0;
                try { t = int.Parse(TimeoutSec, System.Globalization.CultureInfo.InvariantCulture); }
                catch { t = 0; }
                return t > 0 ? t : 300;
            }
        }
    }

    /// <summary>
    /// Curated from WinUtil config/feature.json (33 entries) — owner: config work package.
    /// Titles and descriptions are raw English dictionary keys; the page and the engine
    /// translate them with L.T/L.F. Scripts and paths never pass through L.F.
    /// </summary>
    internal static class ConfigCatalog
    {
        private const string DocRoot = "https://winutil.christitus.com/code-reference/features/";

        private static List<ConfigItem> _all;

        public static List<ConfigItem> All()
        {
            if (_all == null) _all = Build();
            return _all;
        }

        /// <summary>Feature names touched by the "features" group, in page order.</summary>
        public static string[] FeatureNames()
        {
            List<string> names = new List<string>();
            List<ConfigItem> all = All();
            for (int i = 0; i < all.Count; i++)
            {
                if (!string.Equals(all[i].Group, "features", StringComparison.Ordinal)) continue;
                if (!all[i].HasFeatures) continue;
                for (int j = 0; j < all[i].Features.Length; j++) names.Add(all[i].Features[j]);
            }
            return names.ToArray();
        }

        private static ConfigItem Item(string id, string group, string title, string desc, string link)
        {
            ConfigItem c = new ConfigItem();
            c.Id = id;
            c.Group = group;
            c.Title = title;
            c.Desc = desc;
            c.Link = link;
            return c;
        }

        private static List<ConfigItem> Build()
        {
            List<ConfigItem> list = new List<ConfigItem>();

            // =========================================================== features
            ConfigItem feat;

            feat = Item("WPFFeaturesdotnet", "features",
                ".NET Framework (Versions 2, 3, 4) - Enable",
                ".NET and .NET Framework is a developer platform made up of tools, programming " +
                "languages, and libraries for building many different types of applications.",
                DocRoot + "features/dotnet");
            feat.Features = new string[] { "NetFx4-AdvSrvs", "NetFx3" };
            feat.TimeoutSec = "3600";
            feat.NeedsRestart = true;
            list.Add(feat);

            feat = Item("WPFFeatureshyperv", "features",
                "Hyper-V - Enable",
                "Hyper-V is a hardware virtualization product developed by Microsoft that allows " +
                "users to create and manage virtual machines.",
                DocRoot + "features/hyperv");
            feat.Features = new string[] { "Microsoft-Hyper-V-All" };
            feat.TimeoutSec = "3600";
            feat.NeedsRestart = true;
            list.Add(feat);

            feat = Item("WPFFeatureslegacymedia", "features",
                "Legacy Media Components (WMP, DirectPlay) - Enable",
                "Enables legacy programs from previous versions of Windows.",
                DocRoot + "features/legacymedia");
            feat.Features = new string[] { "WindowsMediaPlayer", "MediaPlayback", "DirectPlay", "LegacyComponents" };
            feat.TimeoutSec = "3600";
            feat.NeedsRestart = true;
            list.Add(feat);

            feat = Item("WPFFeaturewsl", "features",
                "Windows Subsystem for Linux (WSL) - Enable",
                "Windows Subsystem for Linux is an optional feature of Windows that allows Linux " +
                "programs to run natively on Windows without the need for a separate virtual machine " +
                "or dual booting.",
                DocRoot + "features/wsl");
            feat.Features = new string[] { "VirtualMachinePlatform", "Microsoft-Windows-Subsystem-Linux" };
            feat.TimeoutSec = "3600";
            feat.NeedsRestart = true;
            list.Add(feat);

            feat = Item("WPFFeaturenfs", "features",
                "Network File System (NFS) - Enable",
                "Network File System (NFS) is a mechanism for storing files on a network.",
                DocRoot + "features/nfs");
            feat.Features = new string[]
            {
                "ServicesForNFS-ClientOnly", "ClientForNFS-Infrastructure", "NFS-Administration"
            };
            feat.TimeoutSec = "3600";
            feat.NeedsRestart = true;
            list.Add(feat);

            // Handler-only feature rows: no DISM feature name, so the page shows a button
            // instead of a checkbox and the engine runs the handler.
            feat = Item("WPFFeatureRegBackup", "features",
                "Registry Backup (Daily Task 12:30am) - Enable",
                "Creates a daily scheduled task that exports the SAM, SECURITY, SYSTEM and SOFTWARE " +
                "hives to System32\\config\\RegBack.",
                DocRoot + "features/regbackup");
            feat.Handler = "regbackup";
            feat.TimeoutSec = "180";
            feat.ConfirmText = "A scheduled task named \"MineManager Registry Backup\" is created and " +
                "runs daily at 12:30 as SYSTEM. It writes copies of the registry hives to " +
                "System32\\config\\RegBack. Copies contain password hashes — protect that folder.";
            list.Add(feat);

            feat = Item("WPFFeatureEnableLegacyRecovery", "features",
                "Legacy F8 Boot Recovery - Enable",
                "Enables the Advanced Boot Options screen that lets you start Windows in advanced " +
                "troubleshooting modes.",
                DocRoot + "features/enablelegacyrecovery");
            feat.Handler = "f8enable";
            feat.TimeoutSec = "60";
            feat.ConfirmText = "The boot configuration is changed with bcdedit. On UEFI systems with " +
                "fast startup the legacy menu may still not appear.";
            list.Add(feat);

            feat = Item("WPFFeatureDisableLegacyRecovery", "features",
                "Legacy F8 Boot Recovery - Disable",
                "Disables the Advanced Boot Options screen and restores the standard boot menu policy.",
                DocRoot + "features/disablelegacyrecovery");
            feat.Handler = "f8disable";
            feat.TimeoutSec = "60";
            list.Add(feat);

            feat = Item("WPFFeaturesSandbox", "features",
                "Windows Sandbox - Enable",
                "Windows Sandbox is a lightweight virtual machine that provides a temporary desktop " +
                "environment to safely run applications and programs in isolation.",
                DocRoot + "features/sandbox");
            feat.Features = new string[] { "Containers-DisposableClientVM" };
            feat.TimeoutSec = "3600";
            feat.NeedsRestart = true;
            list.Add(feat);

            // ============================================================== fixes
            ConfigItem fix;

            fix = Item("WPFPanelAutologin", "fixes", "AutoLogon - Run",
                "Signs in automatically at startup. When automatic sign-in is already on, this " +
                "turns it off again.",
                DocRoot + "fixes/autologin");
            fix.Handler = "autologon";
            fix.Danger = true;
            fix.TimeoutSec = "60";
            fix.ConfirmText = "The Winlogon key is edited so Windows skips the sign-in screen. " +
                "Windows stores the password in plain text in the registry; enter it with netplwiz. " +
                "If automatic sign-in is already on, this turns it off.";
            list.Add(fix);

            fix = Item("WPFFixesNetwork", "fixes", "Network - Reset",
                "Resets Winsock and the TCP/IP stack. A restart is required.",
                DocRoot + "fixes/network");
            fix.Handler = "network";
            fix.Danger = true;
            fix.NeedsRestart = true;
            fix.TimeoutSec = "300";
            fix.ConfirmText = "Winsock and the TCP/IP stack are reset to their defaults. Network " +
                "connections drop and static IP or DNS settings may have to be entered again. " +
                "A restart is required.";
            list.Add(fix);

            fix = Item("WPFFixesNTPPool", "fixes", "NTP Server - Enable",
                "Replaces the default Windows NTP server (time.windows.com) with pool.ntp.org for " +
                "improved time synchronization accuracy and reliability.",
                DocRoot + "fixes/ntppool");
            fix.Handler = "ntp";
            fix.TimeoutSec = "180";
            list.Add(fix);

            fix = Item("WPFPanelDISM", "fixes", "System Corruption Scan - Run",
                "Runs chkdsk, SFC and DISM to check the disk and repair protected system files.",
                DocRoot + "fixes/dism");
            fix.Handler = "systemrepair";
            fix.TimeoutSec = "7200";
            fix.ConfirmText = "chkdsk, sfc and DISM are run one after another. This can take 30 " +
                "minutes or more and the progress output is shown in the log.";
            list.Add(fix);

            fix = Item("WPFFixesUpdate", "fixes", "Windows Update - Reset",
                "Stops the update services, renames the update cache and the signature store, then " +
                "starts the services again.",
                DocRoot + "fixes/update");
            fix.Handler = "updatereset";
            fix.Danger = true;
            fix.NeedsRestart = true;
            fix.TimeoutSec = "900";
            fix.ConfirmText = "Windows Update, BITS, Cryptographic and Windows Installer services " +
                "are stopped. SoftwareDistribution and catroot2 are renamed with a timestamp and " +
                "recreated by Windows. Update history is lost and updates are downloaded again.";
            list.Add(fix);

            fix = Item("WPFFixesWinget", "fixes", "WinGet - Reinstall",
                "Checks WinGet and repairs the App Installer package in place. Nothing is downloaded.",
                DocRoot + "fixes/winget");
            fix.Handler = "winget";
            fix.TimeoutSec = "600";
            list.Add(fix);

            // ============================================================ panels
            // Exactly the WinUtil "Legacy Windows Panels" set, in its order.
            AddPanel(list, "WPFPanelComputer", "Computer Management",
                "Disks, services, devices and event logs in one console.",
                "compmgmt.msc", "legacy-windows-panels/computer");
            AddPanel(list, "WPFPanelControl", "Control Panel",
                "Classic Control Panel home.", "control.exe", "legacy-windows-panels/control");
            AddPanel(list, "WPFPanelMouse", "Mouse Properties",
                "Buttons, pointer and pointer options.", "main.cpl", "legacy-windows-panels/mouse");
            AddPanel(list, "WPFPanelNetwork", "Network Connections",
                "Network adapters and connection settings.", "ncpa.cpl", "legacy-windows-panels/network");
            AddPanel(list, "WPFPanelPower", "Power Panel",
                "Power plans and sleep settings.", "powercfg.cpl", "legacy-windows-panels/power");
            AddPanel(list, "WPFPanelPrinter", "Printer Panel",
                "Printers, scanners and other devices.", "control.exe printers",
                "legacy-windows-panels/printer");
            AddPanel(list, "WPFPanelPrograms", "Programs and Features",
                "Installed programs and Windows features.", "appwiz.cpl", "legacy-windows-panels/programs");
            AddPanel(list, "WPFPanelRegion", "Region",
                "Formats, location and keyboard layouts.", "intl.cpl", "legacy-windows-panels/region");
            AddPanel(list, "WPFPanelSecurity", "Security and Maintenance",
                "Security and maintenance messages.", "wscui.cpl", "legacy-windows-panels/security");
            AddPanel(list, "WPFPanelSound", "Sound Settings",
                "Playback and recording devices.", "mmsys.cpl", "legacy-windows-panels/sound");
            AddPanel(list, "WPFPanelSystem", "System Properties",
                "Computer name, performance and system protection.", "sysdm.cpl",
                "legacy-windows-panels/system");
            AddPanel(list, "WPFPanelTimedate", "Time and Date",
                "Clock, time zone and internet time.", "timedate.cpl", "legacy-windows-panels/timedate");
            AddPanel(list, "WPFPanelFirewall", "Windows Defender Firewall",
                "Firewall profiles and allowed apps.", "firewall.cpl", "legacy-windows-panels/firewall");
            AddPanel(list, "WPFPanelRestore", "Windows Restore",
                "System restore points.", "rstrui.exe", "legacy-windows-panels/restore");

            // =========================================================== profile
            ConfigItem prof;

            prof = Item("WPFWinUtilInstallPSProfile", "profile", "PowerShell Profile - Install",
                "Writes a commented starter profile for PowerShell 7 and Windows PowerShell and " +
                "backs up any existing profile file first.",
                DocRoot + "powershell-profile-powershell-7--only/installpsprofile");
            prof.Handler = "psprofile-install";
            prof.TimeoutSec = "60";
            list.Add(prof);

            prof = Item("WPFWinUtilUninstallPSProfile", "profile", "PowerShell Profile - Remove",
                "Restores the profile from the backup, or deletes only the files this tool created.",
                DocRoot + "powershell-profile-powershell-7--only/uninstallpsprofile");
            prof.Handler = "psprofile-remove";
            prof.TimeoutSec = "60";
            list.Add(prof);

            // ============================================================ remote
            ConfigItem rem;

            rem = Item("WPFWinUtilSSHServer", "remote", "OpenSSH Server - Enable",
                "Installs the OpenSSH Server capability, starts the sshd service and opens firewall " +
                "port 22.",
                DocRoot + "remote-access/sshserver");
            rem.Handler = "ssh";
            rem.Danger = true;
            rem.TimeoutSec = "900";
            rem.ConfirmText = "OpenSSH Server lets other computers log in to this PC over the " +
                "network. The sshd service is set to start automatically and a firewall rule for " +
                "port 22 is added.";
            list.Add(rem);

            return list;
        }

        private static void AddPanel(List<ConfigItem> list, string id, string title, string desc,
            string command, string link)
        {
            ConfigItem c = Item(id, "panels", title, desc, DocRoot + link);
            c.Command = command;
            c.Interactive = true;
            c.TimeoutSec = "30";
            list.Add(c);
        }

        public static List<ConfigItem> Group(string group)
        {
            List<ConfigItem> list = new List<ConfigItem>();
            List<ConfigItem> all = All();
            for (int i = 0; i < all.Count; i++)
                if (string.Equals(all[i].Group, group, StringComparison.Ordinal)) list.Add(all[i]);
            return list;
        }

        public static string GroupTitle(string group)
        {
            if (group == "features") return L.T("Features");
            if (group == "fixes") return L.T("Fixes");
            if (group == "panels") return L.T("Legacy Windows Panels");
            if (group == "profile") return L.T("Powershell Profile Powershell 7+ Only");
            if (group == "remote") return L.T("Remote Access");
            return group;
        }
    }
}
