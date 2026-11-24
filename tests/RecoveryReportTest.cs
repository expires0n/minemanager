using System;
using System.Collections.Generic;
using System.Text;

namespace MineManager
{
    internal static class RecoveryReportTest
    {
        private static int _bad;

        private static void Main()
        {
            L.SetLanguage("en");
            CheckDism("clean check", "/Online /Cleanup-Image /CheckHealth", 0,
                "Deployment Image Servicing and Management tool\nVersion: 10.0\n\nNo component store corruption detected.\nThe operation completed successfully.",
                RecoveryVerdict.Clean);
            CheckDism("repairable scan", "/Online /Cleanup-Image /ScanHealth", 0,
                "The component store is repairable.\nThe operation completed successfully.",
                RecoveryVerdict.Issues);
            CheckDism("restore success", "/Online /Cleanup-Image /RestoreHealth", 0,
                "The restore operation completed successfully.\nThe operation completed successfully.",
                RecoveryVerdict.Repaired);
            CheckSfc("sfc clean", 0,
                "Verification 100% complete.\nWindows Resource Protection did not find any integrity violations.",
                RecoveryVerdict.Clean);
            CheckSfc("sfc repaired", 0,
                "Windows Resource Protection found corrupt files and successfully repaired them.",
                RecoveryVerdict.Repaired);
            CheckSfc("sfc unable", 0,
                "Windows Resource Protection found corrupt files but was unable to fix some of them.",
                RecoveryVerdict.Issues);
            CheckChk("chkdsk clean", 0,
                "Windows has scanned the file system and found no problems.\nNo further action is required.",
                RecoveryVerdict.Clean);
            CheckChk("chkdsk problems", 0,
                "Windows has found problems.",
                RecoveryVerdict.Issues);

            RecoveryStep sfc = Step("sfc.exe", "/scannow");
            CheckSfc("sfc not elevated", 0,
                "You must be an administrator running a console session in order to use the sfc utility.",
                RecoveryVerdict.Failed);

            RunResult cancelled = new RunResult();
            cancelled.Cancelled = true;
            Check("cancelled", RecoveryReport.Parse(0, sfc, cancelled).Verdict, RecoveryVerdict.Stopped);

            RunResult timed = new RunResult();
            timed.TimedOut = true;
            Check("timed out", RecoveryReport.Parse(0, sfc, timed).Verdict, RecoveryVerdict.Failed);

            RunResult unknown = new RunResult();
            unknown.Output = "Something unexpected happened.";
            unknown.ExitCode = 0;
            Check("unknown", RecoveryReport.Parse(0, sfc, unknown).Verdict, RecoveryVerdict.Unknown);

            L.SetLanguage("tr");
            CheckSfc("turkish repaired", 0,
                "Windows Kaynak Koruması bozuk dosyalar buldu ve bunları başarıyla onardı.",
                RecoveryVerdict.Repaired);
            CheckDism("turkish clean", "/Online /Cleanup-Image /CheckHealth", 0,
                "Bileşen deposu bozulması algılanmadı.",
                RecoveryVerdict.Clean);
            CheckChk("turkish clean", 0,
                "Windows dosya sistemini taradı ve sorun bulamadı.",
                RecoveryVerdict.Clean);

            Check("sfc encoding", RecoveryTools.OutputEncoding("C:\\Windows\\System32\\sfc.exe").CodePage, 1200);
            Check("dism encoding", RecoveryTools.OutputEncoding("C:\\Windows\\System32\\dism.exe").CodePage,
                System.Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage);

            L.SetLanguage("en");
            List<RecoveryFinding> findings = new List<RecoveryFinding>();
            findings.Add(RecoveryReport.Parse(0, Step("dism.exe", "/Online /Cleanup-Image /CheckHealth"), Make(0, "No component store corruption detected.")));
            findings.Add(RecoveryReport.Parse(1, Step("sfc.exe", "/scannow"), Make(0, "Windows Resource Protection did not find any integrity violations.")));
            string head = RecoveryReport.Headline(findings, false, "01:00");
            Check("headline all clean", head.IndexOf("All clean", StringComparison.Ordinal) >= 0, true);
            string report = RecoveryReport.BuildReport(findings, false, false, "01:00");
            Check("report no invented count", report.IndexOf("file count", StringComparison.OrdinalIgnoreCase) >= 0 ||
                report.IndexOf("dosya sayısı", StringComparison.OrdinalIgnoreCase) >= 0, true);

            Console.WriteLine(_bad == 0 ? "ALL REPORT TESTS PASSED" : (_bad + " TEST(S) FAILED"));
            Environment.ExitCode = _bad == 0 ? 0 : 1;
        }

        private static RunResult Make(int exit, string output)
        {
            RunResult r = new RunResult();
            r.ExitCode = exit;
            r.Output = output;
            return r;
        }

        private static RecoveryStep Step(string exe, string args)
        {
            return new RecoveryStep { Name = "Test step", Exe = exe, Args = args, TimeoutSec = 600 };
        }

        private static void CheckDism(string name, string args, int exit, string output, RecoveryVerdict expect)
        {
            Check(name, RecoveryReport.Parse(0, Step("dism.exe", args), Make(exit, output)).Verdict, expect);
        }

        private static void CheckSfc(string name, int exit, string output, RecoveryVerdict expect)
        {
            Check(name, RecoveryReport.Parse(0, Step("sfc.exe", "/scannow"), Make(exit, output)).Verdict, expect);
        }

        private static void CheckChk(string name, int exit, string output, RecoveryVerdict expect)
        {
            Check(name, RecoveryReport.Parse(0, Step("chkdsk.exe", "C: /scan"), Make(exit, output)).Verdict, expect);
        }

        private static void Check(string name, object actual, object expect)
        {
            bool ok = object.Equals(actual, expect);
            Console.WriteLine((ok ? "PASS " : "FAIL ") + name + " => " + actual +
                (ok ? "" : " (expected " + expect + ")"));
            if (!ok) _bad++;
        }
    }
}
