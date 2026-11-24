using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MineManager
{
    internal enum RecoveryVerdict
    {
        Clean,
        Repaired,
        Issues,
        Failed,
        Stopped,
        Unknown
    }

    internal class RecoveryFinding
    {
        public int Index;
        public RecoveryStep Step;
        public RecoveryVerdict Verdict;
        public string Detail = "";
        public string Evidence = "";

        public bool Ok
        {
            get { return Verdict == RecoveryVerdict.Clean || Verdict == RecoveryVerdict.Repaired; }
        }

        public string StateKey
        {
            get
            {
                switch (Verdict)
                {
                    case RecoveryVerdict.Clean: return "No problems";
                    case RecoveryVerdict.Repaired: return "Repaired";
                    case RecoveryVerdict.Issues: return "Issues found";
                    case RecoveryVerdict.Failed: return "Failed";
                    case RecoveryVerdict.Stopped: return "Stopped";
                    default: return "Unclear";
                }
            }
        }
    }

    /// <summary>
    /// Turns the console output of the documented repair commands into a small,
    /// honest report. SFC and DISM never report a repaired-file count, so the
    /// report says which step repaired something and never invents a number.
    /// English and Turkish result lines are both recognised; the raw Windows
    /// line is kept as evidence for anything that is not a clean success.
    /// </summary>
    internal static class RecoveryReport
    {
        public static RecoveryFinding Parse(int index, RecoveryStep step, RunResult r)
        {
            RecoveryFinding f = new RecoveryFinding();
            f.Index = index;
            f.Step = step;
            f.Verdict = RecoveryVerdict.Unknown;
            f.Evidence = KeyLine(r.Output);

            if (r.Cancelled)
            {
                f.Verdict = RecoveryVerdict.Stopped;
                f.Detail = L.T("The step was stopped before it finished.");
                return f;
            }
            if (r.TimedOut)
            {
                f.Verdict = RecoveryVerdict.Failed;
                f.Detail = L.F("The step timed out after {0} minutes.", step.TimeoutSec / 60);
                return f;
            }

            string text = (r.Output ?? "").ToLowerInvariant();
            string tool = Path.GetFileNameWithoutExtension(step.Exe).ToLowerInvariant();
            if (tool == "sfc") ParseSfc(f, text);
            else if (tool == "dism") ParseDism(f, step, text);
            else if (tool == "chkdsk") ParseChkdsk(f, r, text);

            if (f.Verdict == RecoveryVerdict.Unknown && !r.Success)
            {
                f.Verdict = RecoveryVerdict.Failed;
                f.Detail = L.F("The command failed with exit code {0}.", "0x" + ((uint)r.ExitCode).ToString("X8"));
            }
            if (f.Verdict == RecoveryVerdict.Unknown)
                f.Detail = L.T("The command completed, but its result line was not recognised. See the log.");
            if (r.NeedsReboot)
                f.Detail = f.Detail + " " + L.T("A restart is required to finish the repair.");
            return f;
        }

        private static void ParseSfc(RecoveryFinding f, string t)
        {
            if (HasAny(t, "did not find any integrity violations",
                "bütünlük ihlali bulamadı", "bütünlük ihlalleri bulunmadı",
                "hiçbir bütünlük ihlali", "bütünlük ihlali algılanmadı",
                "herhangi bir bütünlük ihlali"))
            {
                f.Verdict = RecoveryVerdict.Clean;
                f.Detail = L.T("No integrity violations found. Protected system files are intact.");
                return;
            }
            if (HasAny(t, "unable to fix", "could not fix", "düzeltemedi", "onaramadı",
                "düzeltilemedi", "onarılamadı"))
            {
                f.Verdict = RecoveryVerdict.Issues;
                f.Detail = L.T("Corrupt files were found; some could not be repaired. Check the log and the CBS log.");
                return;
            }
            if (HasAny(t, "successfully repaired", "başarıyla onardı", "başarıyla onarıldı",
                "onarıldı"))
            {
                f.Verdict = RecoveryVerdict.Repaired;
                f.Detail = L.T("Corrupt files were found and repaired. Windows does not report how many files.");
                return;
            }
            if (HasAny(t, "found corrupt files", "bozuk dosya"))
            {
                f.Verdict = RecoveryVerdict.Issues;
                f.Detail = L.T("Corrupt files were reported, but the output does not say whether they were repaired. See the log.");
                return;
            }
            if (HasAny(t, "could not perform", "must be an administrator", "yönetici olarak",
                "gerçekleştiremedi", "çalıştırılamadı"))
            {
                f.Verdict = RecoveryVerdict.Failed;
                f.Detail = L.T("The system file check could not run. Run the app as administrator and try again.");
            }
        }

        private static void ParseDism(RecoveryFinding f, RecoveryStep step, string t)
        {
            bool restore = step.Args.IndexOf("RestoreHealth", StringComparison.OrdinalIgnoreCase) >= 0;
            if (HasAny(t, "no component store corruption", "bileşen deposu bozulması algılanmadı",
                "bileşen deposunda bozulma algılanmadı", "bileşen deposu bozulması yok"))
            {
                f.Verdict = RecoveryVerdict.Clean;
                f.Detail = L.T("No component store corruption detected. The Windows image is healthy.");
                return;
            }
            if (restore && HasAny(t, "restore operation completed successfully",
                "geri yükleme işlemi başarıyla tamamlandı", "operation completed successfully",
                "işlem başarıyla tamamlandı"))
            {
                f.Verdict = RecoveryVerdict.Repaired;
                f.Detail = L.T("The image repair step completed successfully.");
                return;
            }
            if (HasAny(t, "component store is repairable", "bileşen deposu onarılabilir"))
            {
                f.Verdict = restore ? RecoveryVerdict.Repaired : RecoveryVerdict.Issues;
                f.Detail = restore
                    ? L.T("The image was reported as repairable and the repair step completed.")
                    : L.T("Image corruption was detected. The restore step can repair it.");
                return;
            }
            if (HasAny(t, "error", "hata", "failed", "başarısız"))
            {
                f.Verdict = RecoveryVerdict.Failed;
                f.Detail = L.T("The image command reported an error. See the log for the exact line.");
            }
        }

        private static void ParseChkdsk(RecoveryFinding f, RunResult r, string t)
        {
            if (r.ExitCode == 1
                || HasAny(t, "found problems", "problems found", "errors found",
                    "sorun buldu", "sorunlar bulundu", "hata bulundu"))
            {
                f.Verdict = RecoveryVerdict.Issues;
                f.Detail = L.T("File-system problems were found. Repair needs chkdsk /f and a restart; this page only scans.");
                return;
            }
            if (HasAny(t, "found no problems", "no problems", "no further action",
                "sorun bulamadı", "sorun yok", "hiçbir sorun", "sorun algılanmadı"))
            {
                f.Verdict = RecoveryVerdict.Clean;
                f.Detail = L.T("No file-system problems found on the system drive.");
            }
        }

        private static bool HasAny(string haystack, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
                if (haystack.IndexOf(needles[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>The last useful, non-progress line from the console output.</summary>
        private static string KeyLine(string output)
        {
            List<string> lines = ProcRunner.Lines(output);
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string s = lines[i].Trim();
                if (s.Length < 6) continue;
                if (s.IndexOf('%') >= 0 && s.Length < 100) continue;
                string low = s.ToLowerInvariant();
                if (low.StartsWith("deployment image servicing") ||
                    low.StartsWith("copyright") || low.StartsWith("version:") ||
                    low.StartsWith("the operation completed successfully"))
                    continue;
                return s.Length > 220 ? s.Substring(0, 220) + "…" : s;
            }
            return "";
        }

        public static string Headline(List<RecoveryFinding> findings, bool cancelled, string took)
        {
            int clean = 0, repaired = 0, issues = 0, failed = 0, unknown = 0;
            for (int i = 0; i < findings.Count; i++)
            {
                switch (findings[i].Verdict)
                {
                    case RecoveryVerdict.Clean: clean++; break;
                    case RecoveryVerdict.Repaired: repaired++; break;
                    case RecoveryVerdict.Issues: issues++; break;
                    case RecoveryVerdict.Failed: failed++; break;
                    case RecoveryVerdict.Unknown: unknown++; break;
                }
            }
            string counts = L.F("{0} repaired · {1} clean · {2} issues · {3} failed",
                repaired, clean, issues, failed);
            if (cancelled) return L.F("Stopped after {0} step(s) · {1}", findings.Count, counts);
            if (failed > 0) return L.F("Finished with errors · {0}", counts);
            if (issues > 0) return L.F("Finished with findings · {0}", counts);
            if (unknown > 0) return L.F("Finished · {0} · {1} unclear", counts, unknown);
            if (repaired > 0) return L.F("Repair completed · {0}", counts);
            return L.F("All clean · {0} step(s) · {1}", findings.Count, took);
        }

        public static string BuildReport(List<RecoveryFinding> findings, bool cancelled, bool reboot, string took)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < findings.Count; i++)
            {
                RecoveryFinding f = findings[i];
                if (i > 0) sb.AppendLine();
                sb.AppendLine(L.F("{0}. {1} — {2}", f.Index + 1, L.T(f.Step.Name), L.T(f.StateKey)));
                sb.AppendLine("   " + f.Detail);
                if (f.Evidence.Length > 0 &&
                    (f.Verdict == RecoveryVerdict.Issues || f.Verdict == RecoveryVerdict.Failed ||
                     f.Verdict == RecoveryVerdict.Unknown))
                    sb.AppendLine("   " + L.F("Log: {0}", f.Evidence));
            }
            sb.AppendLine();
            sb.AppendLine(L.F("Result: {0}", Headline(findings, cancelled, took)));
            sb.AppendLine(L.T("SFC and DISM do not report a repaired-file count; the report names the step that repaired, not a number."));
            if (reboot) sb.AppendLine(L.T("A restart is required to finish the repair."));
            sb.AppendLine(L.F("Elapsed: {0}", took));
            return sb.ToString();
        }
    }
}
