using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace MineManager
{
    /// <summary>
    /// Runs Config page actions: DISM features, repair commands, legacy panels, the
    /// PowerShell profile and OpenSSH. Blocking; the page calls it from a Task and owns
    /// the global job guard (ProcRunner.TryBeginJob/EndJob), so nothing here takes it.
    /// Owner: config work package.
    /// </summary>
    internal static class ConfigEngine
    {
        // Feature state is read from DISM once and cached for the session; the page calls
        // Invalidate() when the user presses Refresh.
        private static readonly object Gate = new object();
        private static readonly Dictionary<string, bool> StateCache =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static bool _loadedAll;
        private static bool _lastRestart;

        /// <summary>Set by the last Run(): the action finished but Windows needs a restart.</summary>
        public static bool LastRunNeedsRestart { get { return _lastRestart; } }

        /// <summary>Drops the cached DISM state so the next query reads the machine again.</summary>
        public static void Invalidate()
        {
            lock (Gate)
            {
                StateCache.Clear();
                _loadedAll = false;
            }
        }

        /// <summary>Runs one item to completion. Blocking: call from a Task. Never throws.</summary>
        public static bool Run(ConfigItem item, Action<string> log, CancelFlag cancel, out string error)
        {
            error = "";
            _lastRestart = false;
            if (item == null)
            {
                error = L.T("No action was selected.");
                return false;
            }
            try
            {
                if (item.Group == "panels") return OpenPanel(item, log, out error);

                string h = item.Handler == null ? "" : item.Handler;
                if (h == "ntp") return RunNtp(item, log, cancel, out error);
                if (h == "network") return RunNetwork(item, log, cancel, out error);
                if (h == "systemrepair") return RunSystemRepair(item, log, cancel, out error);
                if (h == "updatereset") return RunUpdateReset(item, log, cancel, out error);
                if (h == "winget") return RunWinget(item, log, cancel, out error);
                if (h == "autologon") return RunAutoLogon(log, out error);
                if (h == "regbackup") return RunRegBackup(item, log, cancel, out error);
                if (h == "f8enable") return RunBootMenu(true, item, log, cancel, out error);
                if (h == "f8disable") return RunBootMenu(false, item, log, cancel, out error);
                if (h == "psprofile-install") return RunProfile(true, log, cancel, out error);
                if (h == "psprofile-remove") return RunProfile(false, log, cancel, out error);
                if (h == "ssh") return RunSsh(item, log, cancel, out error);

                if (item.HasFeatures) return InstallFeatures(item, log, cancel, out error);

                error = L.T("This action has no handler.");
                return false;
            }
            catch (Exception ex)
            {
                error = L.T("The action failed unexpectedly.");
                Log(log, L.T("The action failed unexpectedly."));
                Log(log, "  " + ex.Message);
                return false;
            }
        }

        /// <summary>DISM query for one optional feature. Cheap enough for a background task.</summary>
        public static bool IsFeatureEnabled(string name)
        {
            if (name == null || name.Length == 0) return false;
            lock (Gate)
            {
                bool known;
                if (StateCache.TryGetValue(name, out known)) return known;
            }
            // An unanswered query is not cached, so a later read can still resolve it.
            int state = QueryFeature(name);
            if (state < 0) return false;
            lock (Gate) StateCache[name] = state == 1;
            return state == 1;
        }

        /// <summary>Batch query — one DISM pass for many feature names.</summary>
        public static Dictionary<string, bool> FeatureStates(string[] names)
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (names == null || names.Length == 0) return result;

            bool need = false;
            lock (Gate)
            {
                if (!_loadedAll)
                {
                    for (int i = 0; i < names.Length; i++)
                        if (!StateCache.ContainsKey(names[i])) need = true;
                }
            }
            if (need) LoadAllFeatures();

            lock (Gate)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    // Names the query could not resolve are left out, so the page can show
                    // "unknown" instead of claiming a feature is off when DISM did not answer.
                    bool on;
                    if (StateCache.TryGetValue(names[i], out on)) result[names[i]] = on;
                    else if (_loadedAll) result[names[i]] = false;
                }
            }
            return result;
        }

        // ================================================================ features

        private static bool InstallFeatures(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int timeout = item.Timeout;
            bool restart = false;

            for (int i = 0; i < item.Features.Length; i++)
            {
                string name = item.Features[i];
                if (Stopped(cancel))
                {
                    error = L.T("Cancelled.");
                    return false;
                }

                Log(log, L.F("Installing {0}...", name));
                RunResult r = ProcRunner.Run(Sys("dism.exe"),
                    "/online /enable-feature /featurename:" + name + " /all /norestart",
                    timeout, log, cancel);

                if (Stopped(cancel) || r.Cancelled)
                {
                    error = L.T("Cancelled.");
                    return false;
                }
                if (r.TimedOut)
                {
                    error = L.F("{0} did not finish within {1} seconds.", name, timeout);
                    Log(log, error);
                    return false;
                }
                if (r.ExitCode == 3010)
                {
                    restart = true;
                    SetCached(name, true);
                    Log(log, L.F("{0} is enabled and needs a restart.", name));
                    continue;
                }
                if (r.ExitCode != 0)
                {
                    error = L.F("{0} failed with exit code {1}.", name, r.ExitCode);
                    Log(log, error);
                    return false;
                }
                SetCached(name, true);
                Log(log, L.F("{0} is enabled.", name));
            }

            if (restart)
            {
                Log(log, L.T("A restart is required to finish installing the features."));
                _lastRestart = true;
            }
            return true;
        }

        /// <summary>One DISM pass for every optional feature; fills the whole cache.</summary>
        private static void LoadAllFeatures()
        {
            RunResult r = ProcRunner.Run(Sys("dism.exe"), "/online /get-features /format:table",
                300, null, null);
            if (!r.Ok) return;

            Dictionary<string, bool> found = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            List<string> lines = ProcRunner.Lines(r.Output);
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                int bar = line.IndexOf('|');
                if (bar <= 0) continue;
                string name = line.Substring(0, bar).Trim();
                string state = line.Substring(bar + 1).Trim();
                if (name.Length == 0 || state.Length == 0) continue;
                if (name[0] == '-' || name[0] == '=') continue;
                if (IsHeaderRow(name)) continue;
                found[name] = LooksEnabled(state);
            }

            lock (Gate)
            {
                foreach (KeyValuePair<string, bool> kv in found) StateCache[kv.Key] = kv.Value;
                if (found.Count > 0) _loadedAll = true;
            }
        }

        private static bool IsHeaderRow(string name)
        {
            return string.Equals(name, "Feature Name", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "Feature", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "Özellik Adı", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// DISM prints the state in the UI language. Enabled is "Enabled" (en) / "Etkin" (tr);
        /// a handful of other locales are accepted so the page is not silently always "off".
        /// Anything unrecognised counts as not enabled, which is the safe reading.
        /// </summary>
        private static bool LooksEnabled(string state)
        {
            string s = (state ?? "").Trim();
            if (s.Length == 0) return false;
            if (string.Equals(s, "Enabled", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Enable Pending", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Etkin", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Aktiviert", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Activé", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Habilitado", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Abilitato", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Ativado", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Ingeschakeld", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "Включено", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>1 enabled, 0 disabled, -1 the query did not answer.</summary>
        private static int QueryFeature(string name)
        {
            RunResult r = ProcRunner.Run(Sys("dism.exe"),
                "/online /get-featureinfo /featurename:" + name, 180, null, null);
            if (!r.Ok) return -1;
            List<string> lines = ProcRunner.Lines(r.Output);
            for (int i = 0; i < lines.Count; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon <= 0) continue;
                string key = lines[i].Substring(0, colon).Trim();
                if (!string.Equals(key, "State", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(key, "Durum", StringComparison.OrdinalIgnoreCase)) continue;
                return LooksEnabled(lines[i].Substring(colon + 1)) ? 1 : 0;
            }
            return -1;
        }

        private static void SetCached(string name, bool on)
        {
            lock (Gate) StateCache[name] = on;
        }

        // ================================================================== fixes

        private static bool RunNtp(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int t = item.Timeout;

            Log(log, L.T("Starting the Windows Time service..."));
            ProcRunner.Run(Sys("net.exe"), "start w32time", t, log, cancel);
            if (Stopped(cancel)) { error = L.T("Cancelled."); return false; }

            Log(log, L.T("Setting pool.ntp.org as the time server..."));
            RunResult r = ProcRunner.Run(Sys("w32tm.exe"),
                "/config /update /manualpeerlist:\"pool.ntp.org,0x8\" /syncfromflags:MANUAL",
                t, log, cancel);
            if (!r.Success)
            {
                error = ExitError(L.T("Configuring the time server"), r);
                Log(log, error);
                return false;
            }

            Log(log, L.T("Restarting the Windows Time service..."));
            ProcRunner.Run(Sys("net.exe"), "stop w32time", t, log, cancel);
            ProcRunner.Run(Sys("net.exe"), "start w32time", t, log, cancel);

            Log(log, L.T("Synchronising the clock..."));
            RunResult resync = ProcRunner.Run(Sys("w32tm.exe"), "/resync", t, log, cancel);
            if (!resync.Success)
                Log(log, L.T("The clock could not be synchronised yet; Windows retries automatically."));

            Log(log, L.T("The time server is now pool.ntp.org."));
            return true;
        }

        private static bool RunNetwork(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int t = item.Timeout;

            // winsock/int ip reset wipes static IPs and DNS. Keep a restorable dump first.
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MineManager");
                Directory.CreateDirectory(dir);
                string dump = Path.Combine(dir,
                    "network-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".txt");
                RunResult saved = ProcRunner.RunShell("netsh -c interface dump > \"" + dump + "\"", 60, null, null);
                if (saved.Ok) Log(log, L.F("Network settings saved to {0}.", dump));
                else Log(log, L.T("The current network settings could not be saved."));
            }
            catch (Exception ex) { Log(log, "  " + ex.Message); }

            Log(log, L.T("Resetting Winsock..."));
            RunResult r = ProcRunner.Run(Sys("netsh.exe"), "winsock reset", t, log, cancel);
            if (!r.Success)
            {
                error = ExitError(L.T("Resetting Winsock"), r);
                Log(log, error);
                return false;
            }
            if (Stopped(cancel)) { error = L.T("Cancelled."); return false; }

            Log(log, L.T("Resetting the TCP/IP stack..."));
            r = ProcRunner.Run(Sys("netsh.exe"), "int ip reset", t, log, cancel);
            if (!r.Success)
            {
                error = ExitError(L.T("Resetting the TCP/IP stack"), r);
                Log(log, error);
                return false;
            }

            Log(log, L.T("The network stack has been reset. Restart the computer to finish."));
            _lastRestart = true;
            return true;
        }

        private static bool RunSystemRepair(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int t = item.Timeout;

            Log(log, L.T("Checking the disk for errors..."));
            RunResult r = ProcRunner.Run(Sys("chkdsk.exe"), "/scan /perf", t, log, cancel);
            if (Stopped(cancel) || r.Cancelled) { error = L.T("Cancelled."); return false; }
            if (r.TimedOut)
            {
                error = L.T("The disk check did not finish in time.");
                Log(log, error);
                return false;
            }
            if (r.ExitCode == 3)
            {
                error = L.T("The disk could not be checked, so the scan was stopped here.");
                Log(log, error);
                return false;
            }
            if (r.ExitCode == 1) Log(log, L.T("Disk errors were found and fixed."));
            else if (r.ExitCode == 2) Log(log, L.T("The disk is clean."));
            else if (r.ExitCode != 0)
            {
                error = L.F("The disk check failed with exit code {0}.", r.ExitCode);
                Log(log, error);
                return false;
            }

            Log(log, L.T("Repairing the Windows image..."));
            r = ProcRunner.Run(Sys("dism.exe"), "/online /cleanup-image /restorehealth", t, log, cancel);
            if (Stopped(cancel) || r.Cancelled) { error = L.T("Cancelled."); return false; }
            if (r.ExitCode == 3010)
            {
                Log(log, L.T("The image was repaired. A restart is required."));
                _lastRestart = true;
            }
            else if (!r.Ok)
            {
                // SFC cannot repair files when the image it reads from is corrupt, but a
                // failed DISM should not hide the file scan result either.
                error = ExitError(L.T("The image repair"), r);
                Log(log, error);
                return false;
            }

            Log(log, L.T("Scanning protected system files..."));
            r = ProcRunner.Run(Sys("sfc.exe"), "/scannow", t, log, cancel);
            if (Stopped(cancel) || r.Cancelled) { error = L.T("Cancelled."); return false; }
            if (!r.Ok)
            {
                error = ExitError(L.T("The system file scan"), r);
                Log(log, error);
                return false;
            }

            Log(log, L.T("The system repair finished."));
            return true;
        }

        private static bool RunUpdateReset(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int t = item.Timeout;
            string[] services = new string[] { "wuauserv", "bits", "cryptsvc", "msiserver" };
            bool cancelled = false;

            Log(log, L.T("Stopping Windows Update services..."));
            for (int i = 0; i < services.Length; i++)
            {
                if (Stopped(cancel)) { cancelled = true; break; }
                ProcRunner.Run(Sys("net.exe"), "stop " + services[i] + " /y", t, log, cancel);
            }

            string root = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            bool renamed = false;
            if (!cancelled)
            {
                renamed = RenameFolder(Path.Combine(root, "SoftwareDistribution"), stamp, log);
                renamed |= RenameFolder(Path.Combine(Path.Combine(root, "System32"), "catroot2"), stamp, log);
            }

            // The services must come back up even when the rename or the cancel failed,
            // otherwise Windows Update stays broken until the next reboot. RunUrgent so the
            // restore still happens while the app is closing.
            Log(log, L.T("Starting Windows Update services..."));
            for (int i = 0; i < services.Length; i++)
                ProcRunner.RunUrgent(Sys("net.exe"), "start " + services[i], t, log, null);

            if (cancelled) { error = L.T("Cancelled."); return false; }
            if (!renamed)
            {
                error = L.T("The update cache could not be renamed. Close any program using Windows Update and try again.");
                Log(log, error);
                return false;
            }

            Log(log, L.T("Windows Update has been reset. Restart the computer."));
            _lastRestart = true;
            return true;
        }

        /// <summary>Renames a cache folder with a timestamp suffix; a missing folder is not an error.</summary>
        private static bool RenameFolder(string path, string stamp, Action<string> log)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Log(log, L.F("{0} was not found, nothing to rename.", path));
                    return true;
                }
                string target = path + "." + stamp;
                int n = 1;
                while (Directory.Exists(target))
                {
                    target = path + "." + stamp + "-" + n.ToString(CultureInfo.InvariantCulture);
                    n++;
                }
                Directory.Move(path, target);
                Log(log, L.F("Renamed {0}.", path));
                return true;
            }
            catch (Exception ex)
            {
                Log(log, L.F("{0} could not be renamed.", path));
                Log(log, "  " + ex.Message);
                return false;
            }
        }

        // ================================================================== winget

        private static bool RunWinget(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int t = item.Timeout;

            Log(log, L.T("Checking WinGet..."));
            RunResult r = ProcRunner.Run("winget.exe", "--info", t, log, cancel);
            if (r.Ok)
            {
                Log(log, L.T("WinGet answers correctly. There is nothing to repair."));
                return true;
            }
            if (r.Cancelled || Stopped(cancel)) { error = L.T("Cancelled."); return false; }
            Log(log, L.T("WinGet did not answer. Repairing the App Installer package..."));

            RunResult fix = ProcRunner.RunPowerShell(WingetRepairScript, t, log, cancel);
            string text = fix.Output == null ? "" : fix.Output;
            if (text.IndexOf("MM:RESET", StringComparison.Ordinal) >= 0)
                Log(log, L.T("The App Installer package was reset."));
            else if (text.IndexOf("MM:REGISTERED", StringComparison.Ordinal) >= 0)
                Log(log, L.T("The App Installer package was re-registered from its existing files."));
            else if (text.IndexOf("MM:NOTINSTALLED", StringComparison.Ordinal) >= 0)
            {
                error = L.T("WinGet is not installed on this system. Install App Installer from the Microsoft Store.");
                Log(log, error);
                return false;
            }
            else if (text.IndexOf("MM:NOMANIFEST", StringComparison.Ordinal) >= 0)
            {
                error = L.T("The App Installer package is damaged beyond repair. Install it again from the Microsoft Store.");
                Log(log, error);
                return false;
            }
            else
            {
                error = L.T("The App Installer package could not be repaired.");
                Log(log, error);
                return false;
            }

            Log(log, L.T("Checking WinGet again..."));
            r = ProcRunner.Run("winget.exe", "--info", t, log, cancel);
            if (r.Ok)
            {
                Log(log, L.T("WinGet works again."));
                return true;
            }
            error = L.T("WinGet still does not answer. Repair or reinstall App Installer from the Microsoft Store.");
            Log(log, error);
            return false;
        }

        /// <summary>
        /// In-place repair only: reset the package, else re-register the manifest that is already
        /// on disk. Nothing is downloaded. Markers on stdout tell the caller what happened.
        /// </summary>
        private const string WingetRepairScript = @"
$p = Get-AppxPackage -Name Microsoft.DesktopAppInstaller -ErrorAction SilentlyContinue
if ($null -eq $p) { Write-Output 'MM:NOTINSTALLED'; exit 2 }
try {
    Reset-AppxPackage -Package $p.PackageFullName -ErrorAction Stop
    Write-Output 'MM:RESET'
    exit 0
} catch {
    Write-Output ('MM:RESETFAILED ' + $_.Exception.Message)
}
$manifest = Join-Path $p.InstallLocation 'AppxManifest.xml'
if (Test-Path -LiteralPath $manifest) {
    try {
        Add-AppxPackage -Register $manifest -DisableDevelopmentMode -ErrorAction Stop
        Write-Output 'MM:REGISTERED'
        exit 0
    } catch {
        Write-Output ('MM:REGISTERFAILED ' + $_.Exception.Message)
        exit 1
    }
}
Write-Output 'MM:NOMANIFEST'
exit 1
";

        // ================================================================ autologon

        private const string Winlogon = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";

        /// <summary>
        /// WinUtil downloads Sysinternals Autologon; this port only edits the Winlogon values and
        /// never stores a password. Windows needs DefaultPassword before it really signs in, so
        /// the log points at netplwiz and warns that the value is stored in plain text.
        /// </summary>
        private static bool RunAutoLogon(Action<string> log, out string error)
        {
            error = "";
            bool on = false;
            using (RegistryKey k = Reg.Open(Winlogon))
                on = string.Equals(Reg.Str(k, "AutoAdminLogon"), "1", StringComparison.Ordinal);

            if (on)
            {
                try
                {
                    using (RegistryKey k = Reg.Open(Winlogon, true))
                    {
                        if (k == null)
                        {
                            error = L.T("The Winlogon registry key could not be opened.");
                            Log(log, error);
                            return false;
                        }
                        k.SetValue("AutoAdminLogon", "0", RegistryValueKind.String);
                        k.DeleteValue("DefaultPassword", false);
                    }
                }
                catch (Exception ex)
                {
                    error = L.T("Automatic sign-in could not be turned off.");
                    Log(log, error);
                    Log(log, "  " + ex.Message);
                    return false;
                }
                Log(log, L.T("Automatic sign-in is now off."));
                return true;
            }

            string user = Environment.UserName;
            string domain = Environment.UserDomainName;
            try
            {
                using (RegistryKey k = Reg.Open(Winlogon, true))
                {
                    if (k == null)
                    {
                        error = L.T("The Winlogon registry key could not be opened.");
                        Log(log, error);
                        return false;
                    }
                    k.SetValue("AutoAdminLogon", "1", RegistryValueKind.String);
                    k.SetValue("DefaultUserName", user, RegistryValueKind.String);
                    k.SetValue("DefaultDomainName", domain, RegistryValueKind.String);
                }
            }
            catch (Exception ex)
            {
                error = L.T("Automatic sign-in could not be turned on.");
                Log(log, error);
                Log(log, "  " + ex.Message);
                return false;
            }

            Log(log, L.F("Automatic sign-in is set up for {0}.", user));
            Log(log, L.T("Windows still needs the password before it can sign in on its own."));
            Log(log, L.T("Run netplwiz, clear \"Users must enter a user name and password\" and enter it there."));
            Log(log, L.T("Windows keeps that password in plain text in the registry. Run this action again to turn it off."));
            return true;
        }

        // =============================================================== regbackup

        private const string LegacyRegBackupTask = "MineManager Registry Backup";

        /// <summary>
        /// Turns on Windows' own periodic registry backup and triggers the built-in
        /// RegIdleBackup task. A custom SYSTEM task running a .cmd from ProgramData would
        /// be a privilege-escalation vector (a standard user can own that folder) and
        /// reg.exe export files are not hives, so they cannot be used for recovery.
        /// </summary>
        private static bool RunRegBackup(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int t = item.Timeout;
            string root = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string target = Path.Combine(Path.Combine(Path.Combine(root, "System32"), "config"), "RegBack");

            try
            {
                using (RegistryKey k = Registry.LocalMachine.CreateSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Configuration Manager"))
                {
                    if (k == null)
                    {
                        error = L.T("The periodic registry backup setting could not be enabled.");
                        Log(log, error);
                        return false;
                    }
                    k.SetValue("EnablePeriodicBackup", 1, RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                error = L.T("The periodic registry backup setting could not be enabled.");
                Log(log, error);
                Log(log, "  " + ex.Message);
                return false;
            }
            Log(log, L.T("Windows periodic registry backup is enabled."));

            // Remove a task left behind by an earlier build of this tool.
            ProcRunner.Run(Sys("schtasks.exe"),
                "/Delete /TN \"" + LegacyRegBackupTask + "\" /F", 60, null, null);

            Log(log, L.T("Triggering the built-in RegIdleBackup task..."));
            RunResult run = ProcRunner.Run(Sys("schtasks.exe"),
                "/Run /TN \"\\Microsoft\\Windows\\Registry\\RegIdleBackup\"", t, log, cancel);
            if (Stopped(cancel)) { error = L.T("Cancelled."); return false; }
            if (!run.Ok)
            {
                Log(log, L.T("The backup task is registered but could not be started now; Windows runs it when the PC is idle."));
                return true;
            }

            bool ok = false;
            for (int i = 0; i < 40 && !ok; i++)
            {
                if (Stopped(cancel)) break;
                ok = NewestFile(target) != DateTime.MinValue;
                if (!ok) System.Threading.Thread.Sleep(500);
            }
            if (ok) Log(log, L.F("Backup files are in {0}.", target));
            else Log(log, L.T("The first run could not be verified yet; Windows repeats the backup when the PC is idle."));
            return true;
        }

        private static DateTime NewestFile(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) return DateTime.MinValue;
                DateTime best = DateTime.MinValue;
                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    DateTime w = File.GetLastWriteTimeUtc(files[i]);
                    if (w > best) best = w;
                }
                return best;
            }
            catch { return DateTime.MinValue; }
        }

        // ================================================================ boot menu

        private static bool RunBootMenu(bool legacy, ConfigItem item, Action<string> log,
            CancelFlag cancel, out string error)
        {
            error = "";
            // {current} is the running OS; {default} can point at a different entry.
            // Export the store first so a bad edit can be rolled back.
            string backupDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MineManager");
            string backup = Path.Combine(backupDir,
                "bcd-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            try
            {
                Directory.CreateDirectory(backupDir);
                RunResult exp = ProcRunner.Run(Sys("bcdedit.exe"), "/export \"" + backup + "\"",
                    60, null, null);
                if (exp.Ok) Log(log, L.F("Boot configuration backed up to {0}.", backup));
                else Log(log, L.T("The boot configuration could not be backed up; the change is still applied."));
            }
            catch { }

            RunResult r = ProcRunner.Run(Sys("bcdedit.exe"),
                legacy ? "/set {current} bootmenupolicy Legacy" : "/set {current} bootmenupolicy Standard",
                item.Timeout, log, cancel);
            if (!r.Success)
            {
                error = ExitError(L.T("Changing the boot menu policy"), r);
                Log(log, error);
                return false;
            }
            Log(log, legacy ? L.T("The legacy boot menu (F8) is enabled.")
                            : L.T("The standard boot menu is restored."));
            return true;
        }

        // ================================================================= profile

        private const string ProfileMarker = "# MineManager";

        private static bool RunProfile(bool install, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            bool ok = true;
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string[] files = new string[]
            {
                Path.Combine(Path.Combine(docs, "PowerShell"), "Microsoft.PowerShell_profile.ps1"),
                Path.Combine(Path.Combine(docs, "WindowsPowerShell"), "Microsoft.PowerShell_profile.ps1")
            };
            for (int i = 0; i < files.Length; i++)
            {
                if (Stopped(cancel)) { error = L.T("Cancelled."); return false; }
                bool one = install ? InstallProfile(files[i], log) : RemoveProfile(files[i], log);
                if (!one) ok = false;
            }
            if (!ok) error = L.T("One or more profile files could not be updated.");
            return ok;
        }

        private static bool InstallProfile(string file, Action<string> log)
        {
            try
            {
                if (File.Exists(file))
                {
                    string existing = File.ReadAllText(file);
                    if (existing.IndexOf(ProfileMarker, StringComparison.Ordinal) >= 0)
                    {
                        Log(log, L.F("{0} already has the MineManager profile.", file));
                        return true;
                    }
                    string backup = file + ".minemanager-" +
                        DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".bak";
                    File.Copy(file, backup, false);
                    Log(log, L.F("Backed up {0}.", file));
                }
                string dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(file, ProfileText(), new UTF8Encoding(true));
                Log(log, L.F("Installed {0}.", file));
                return true;
            }
            catch (Exception ex)
            {
                Log(log, L.F("{0} could not be written.", file));
                Log(log, "  " + ex.Message);
                return false;
            }
        }

        private static string FirstLine(string text)
        {
            if (text == null) return "";
            int nl = text.IndexOf('\n');
            string line = nl < 0 ? text : text.Substring(0, nl);
            return line.Trim('\uFEFF', '\r', ' ', '\t');
        }

        private static bool RemoveProfile(string file, Action<string> log)
        {
            try
            {
                if (!File.Exists(file))
                {
                    Log(log, L.F("{0} does not exist.", file));
                    return true;
                }
                // Only a file whose first line is our marker may be replaced or deleted;
                // a marker anywhere in the text could belong to the user's own profile.
                string first = FirstLine(File.ReadAllText(file));
                if (first.IndexOf(ProfileMarker, StringComparison.Ordinal) != 0)
                {
                    Log(log, L.F("{0} was not created by MineManager and was left alone.", file));
                    return true;
                }
                string dir = Path.GetDirectoryName(file);
                string pattern = Path.GetFileName(file) + ".minemanager-*.bak";
                string[] backups = string.IsNullOrEmpty(dir) ? new string[0] : Directory.GetFiles(dir, pattern);
                Array.Sort(backups, StringComparer.Ordinal);
                if (backups.Length > 0)
                {
                    string latest = backups[backups.Length - 1];
                    File.Copy(latest, file, true);
                    Log(log, L.F("Restored {0} from the backup.", file));
                    for (int i = 0; i < backups.Length; i++)
                        try { File.Delete(backups[i]); } catch { }
                    return true;
                }
                File.Delete(file);
                Log(log, L.F("Removed {0}.", file));
                return true;
            }
            catch (Exception ex)
            {
                Log(log, L.F("{0} could not be removed.", file));
                Log(log, "  " + ex.Message);
                return false;
            }
        }

        /// <summary>A commented starter profile: nothing is executed until the user uncomments it.</summary>
        private static string ProfileText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(ProfileMarker + " PowerShell profile");
            sb.AppendLine("# Installed by MineManager > Config > PowerShell Profile.");
            sb.AppendLine("# Nothing here changes your session until you remove the leading '#'.");
            sb.AppendLine("# Remove it again with the same card; the previous file is restored from backup.");
            sb.AppendLine("");
            sb.AppendLine("# Set-PSReadLineOption -EditMode Windows");
            sb.AppendLine("# Set-PSReadLineOption -PredictionViewStyle ListView");
            sb.AppendLine("# Set-PSReadLineOption -BellStyle None");
            sb.AppendLine("# Set-PSReadLineOption -HistorySearchCursorMovesToEnd");
            sb.AppendLine("# $OutputEncoding = [Console]::OutputEncoding = [Text.Encoding]::UTF8");
            sb.AppendLine("");
            sb.AppendLine("# Tab completion and history are on by default in PowerShell 7;");
            sb.AppendLine("# Windows PowerShell users can enable them with:");
            sb.AppendLine("# Import-Module PSReadLine");
            sb.AppendLine("# Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete");
            sb.AppendLine("");
            sb.AppendLine("# The upstream WinUtil (CTT) profile lives at https://winutil.christitus.com");
            return sb.ToString();
        }

        // ===================================================================== ssh

        private static bool RunSsh(ConfigItem item, Action<string> log, CancelFlag cancel,
            out string error)
        {
            error = "";
            int t = item.Timeout;

            Log(log, L.T("Installing the OpenSSH Server capability..."));
            RunResult r = ProcRunner.Run(Sys("dism.exe"),
                "/online /add-capability /capabilityname:OpenSSH.Server~~~~0.0.1.0", t, log, cancel);
            if (Stopped(cancel) || r.Cancelled) { error = L.T("Cancelled."); return false; }
            if (r.ExitCode == 3010)
            {
                Log(log, L.T("The OpenSSH Server capability is installed. A restart is required."));
                _lastRestart = true;
            }
            else if (!r.Ok)
            {
                error = ExitError(L.T("Installing the OpenSSH Server capability"), r);
                Log(log, error);
                return false;
            }
            else Log(log, L.T("The OpenSSH Server capability is installed."));

            ProcRunner.Run(Sys("sc.exe"), "config sshd start= auto", t, log, cancel);
            Log(log, L.T("Starting the SSH server..."));
            RunResult start = ProcRunner.Run(Sys("sc.exe"), "start sshd", t, log, cancel);
            if (!start.Ok)
                Log(log, L.T("The SSH server could not be started yet; it starts automatically after a restart."));

            // Microsoft's own rule is matched by display name; ours is separate so a missing
            // rule is detected reliably. Scope it to private/domain networks and to sshd.exe
            // instead of opening port 22 on every profile for every program.
            string sshd = Path.Combine(Path.Combine(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32"), "OpenSSH"), "sshd.exe");
            RunResult show = ProcRunner.Run(Sys("netsh.exe"),
                "advfirewall firewall show rule name=OpenSSH Server (MineManager)", t, log, cancel);
            if (!show.Ok)
            {
                Log(log, L.T("Adding a firewall rule for port 22 (private and domain networks only)..."));
                RunResult add = ProcRunner.Run(Sys("netsh.exe"),
                    "advfirewall firewall add rule name=\"OpenSSH Server (MineManager)\" dir=in action=allow " +
                    "protocol=TCP localport=22 program=\"" + sshd + "\" profile=domain,private", t, log, cancel);
                if (!add.Ok)
                    Log(log, L.T("The firewall rule could not be added. Allow port 22 by hand if you need it."));
                else
                    Log(log, L.T("Port 22 is now reachable from private and domain networks."));
            }

            string host = Environment.MachineName;
            Log(log, L.F("OpenSSH Server is enabled. Connect with: ssh {0}@{1}",
                Environment.UserName, host));
            return true;
        }

        // ================================================================== panels

        private static bool OpenPanel(ConfigItem item, Action<string> log, out string error)
        {
            error = "";
            string command = item.Command == null ? "" : item.Command.Trim();
            if (command.Length == 0)
            {
                error = L.T("This panel has no command.");
                return false;
            }
            string exe = command;
            string args = "";
            int sp = command.IndexOf(' ');
            if (sp > 0)
            {
                exe = command.Substring(0, sp);
                args = command.Substring(sp + 1).Trim();
            }

            Log(log, L.F("Opening {0}...", L.T(item.Title)));
            string raw;
            if (ProcRunner.StartDetached(exe, args, null, out raw)) return true;

            error = L.F("{0} could not be opened.", L.T(item.Title));
            Log(log, error);
            if (!string.IsNullOrEmpty(raw)) Log(log, "  " + raw);
            return false;
        }

        // ================================================================= helpers

        private static bool Stopped(CancelFlag cancel)
        {
            return cancel != null && cancel.Cancelled;
        }

        private static void Log(Action<string> log, string text)
        {
            if (log == null || string.IsNullOrEmpty(text)) return;
            try { log(text); }
            catch { }
        }

        /// <summary>Localised one-line reason for a failed process run.</summary>
        private static string ExitError(string label, RunResult r)
        {
            if (r == null) return L.F("{0} failed.", label);
            if (r.Cancelled) return L.T("Cancelled.");
            if (r.TimedOut) return L.F("{0} timed out.", label);
            return L.F("{0} failed with exit code {1}.", label, r.ExitCode);
        }

        /// <summary>
        /// System32 helper that goes through Sysnative from a 32-bit process, so DISM is the
        /// 64-bit copy and sees the 64-bit feature store.
        /// </summary>
        private static string Sys(string file)
        {
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
            {
                string native = Path.Combine(Path.Combine(win, "Sysnative"), file);
                try { if (File.Exists(native)) return native; }
                catch { }
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), file);
        }
    }
}
