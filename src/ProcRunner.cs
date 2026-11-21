using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace MineManager
{
    internal class CancelFlag
    {
        private volatile bool _cancelled;
        public bool Cancelled
        {
            get { return _cancelled; }
            set { _cancelled = value; }
        }
        public void ThrowIfCancelled() { if (_cancelled) throw new OperationCanceledException(); }
    }

    internal class RunResult
    {
        public int ExitCode;
        public bool TimedOut;
        public bool Cancelled;
        public string Output = "";
        public bool Ok { get { return !TimedOut && !Cancelled && ExitCode == 0; } }
        /// <summary>MSI convention: 3010 / 1641 mean success with a pending reboot.</summary>
        public bool NeedsReboot { get { return ExitCode == 3010 || ExitCode == 1641; } }
        public bool Success { get { return Ok || NeedsReboot; } }
    }

    /// <summary>
    /// Process helper shared by the uninstaller queue, tweaks and config actions.
    /// C# 5 / .NET 4.8 only: no async, no Process.Kill(true) (we use taskkill /T /F).
    /// </summary>
    internal static class ProcRunner
    {
        private static int _job;

        public static bool IsElevated()
        {
            try { return Native.IsAdmin(); }
            catch { return false; }
        }

        /// <summary>One global long-running job at a time (uninstall queue, tweak batch, config action).</summary>
        public static bool Busy { get { return _job != 0; } }

        public static bool TryBeginJob()
        {
            return Interlocked.CompareExchange(ref _job, 1, 0) == 0;
        }

        public static void EndJob()
        {
            Interlocked.Exchange(ref _job, 0);
        }

        public static string PowerShellPath
        {
            get
            {
                // 32-bit process on 64-bit Windows must use Sysnative, otherwise the
                // 32-bit PowerShell is loaded and DISM/Appx cmdlets are unavailable.
                string sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
                if (!Environment.Is64BitProcess && Environment.Is64BitOperatingSystem)
                {
                    string native = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");
                    string p = Path.Combine(native, "WindowsPowerShell\\v1.0\\powershell.exe");
                    if (File.Exists(p)) return p;
                }
                return Path.Combine(sys, "WindowsPowerShell\\v1.0\\powershell.exe");
            }
        }

        public static RunResult Run(string exe, string args, int timeoutSec, Action<string> onLine, CancelFlag cancel)
        {
            RunResult r = new RunResult();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args ?? "");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
                // Uninstallers/legacy panels must see a desktop-sized console, not 80x25.
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                using (Process p = new Process())
                {
                    p.StartInfo = psi;
                    p.EnableRaisingEvents = true;
                    StringBuilder sb = new StringBuilder();
                    object gate = new object();
                    DataReceivedEventHandler h = delegate(object s, DataReceivedEventArgs e)
                    {
                        if (e.Data == null) return;
                        lock (gate) { if (sb.Length < 200000) sb.AppendLine(e.Data); }
                        if (onLine != null) { try { onLine(e.Data); } catch { } }
                    };
                    p.OutputDataReceived += h;
                    p.ErrorDataReceived += h;
                    p.Start();
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();

                    // timeoutSec < 0 means "no deadline": transactional installers (msiexec)
                    // must never be killed, a half-applied MSI is worse than a slow one.
                    int timeout = timeoutSec < 0 ? -1 : (timeoutSec == 0 ? 120 : timeoutSec);
                    DateTime deadline = timeout < 0 ? DateTime.MaxValue : DateTime.UtcNow.AddSeconds(timeout);
                    while (!p.HasExited)
                    {
                        if (cancel != null && cancel.Cancelled) { KillTree(p); r.Cancelled = true; break; }
                        if (DateTime.UtcNow > deadline) { KillTree(p); r.TimedOut = true; break; }
                        Thread.Sleep(60);
                    }
                    try { p.WaitForExit(4000); } catch { }
                    try { r.ExitCode = p.HasExited ? p.ExitCode : -1; } catch { r.ExitCode = -1; }
                    lock (gate) { r.Output = sb.ToString(); }
                }
            }
            catch (Exception ex)
            {
                r.ExitCode = -1;
                r.Output = ex.Message;
                if (onLine != null) { try { onLine(ex.Message); } catch { } }
            }
            return r;
        }

        /// <summary>Runs a PowerShell script from a temp .ps1 (avoids -EncodedCommand AV heuristics).</summary>
        public static RunResult RunPowerShell(string script, int timeoutSec, Action<string> onLine, CancelFlag cancel)
        {
            string dir = Path.Combine(Path.GetTempPath(), "MineManager");
            string file = Path.Combine(dir, "mm-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".ps1");
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(file,
                    "$ProgressPreference='SilentlyContinue';" + Environment.NewLine +
                    "[Console]::OutputEncoding=[Text.Encoding]::UTF8;" + Environment.NewLine +
                    "$ErrorActionPreference='Continue';" + Environment.NewLine +
                    (script ?? "") + Environment.NewLine,
                    new UTF8Encoding(false));
                string args = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + file + "\"";
                return Run(PowerShellPath, args, timeoutSec, onLine, cancel);
            }
            catch (Exception ex)
            {
                RunResult r = new RunResult();
                r.ExitCode = -1;
                r.Output = ex.Message;
                if (onLine != null) { try { onLine(ex.Message); } catch { } }
                return r;
            }
            finally
            {
                try { if (File.Exists(file)) File.Delete(file); } catch { }
            }
        }

        /// <summary>Runs through cmd.exe /c so shell built-ins and quoting behave like WinUtil's InvokeScript.</summary>
        public static RunResult RunShell(string command, int timeoutSec, Action<string> onLine, CancelFlag cancel)
        {
            return Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
                "/c " + command, timeoutSec, onLine, cancel);
        }

        public static void KillTree(Process p)
        {
            try
            {
                if (p == null || p.HasExited) return;
                Run(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "taskkill.exe"),
                    "/PID " + p.Id + " /T /F", 15, null, null);
            }
            catch { }
        }

        /// <summary>Starts a detached process (legacy control panels, program uninstallers).</summary>
        public static bool StartDetached(string exe, string args, string workingDir, out string error)
        {
            error = "";
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, args ?? "");
                psi.UseShellExecute = true;
                if (!string.IsNullOrEmpty(workingDir) && Directory.Exists(workingDir))
                    psi.WorkingDirectory = workingDir;
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static List<string> Lines(string text)
        {
            List<string> l = new List<string>();
            if (string.IsNullOrEmpty(text)) return l;
            string[] parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < parts.Length; i++)
                if (parts[i].Trim().Length > 0) l.Add(parts[i].TrimEnd());
            return l;
        }
    }
}
