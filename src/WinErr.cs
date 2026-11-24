using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace MineManager
{
    /// <summary>
    /// Offline error-code explainer: Win32 messages (FormatMessageW), HRESULT decoding
    /// (FACILITY_WIN32 extraction) and a small built-in MSI/MSIExec table.
    /// No network, no downloads — everything is resolved locally.
    /// </summary>
    internal static class WinErr
    {
        private const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x0200;
        private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        private static extern uint FormatMessageW(uint flags, IntPtr source, uint messageId,
            uint languageId, [Out] StringBuilder buffer, uint size, IntPtr arguments);

        private class MsiEntry
        {
            public int Code;
            public string En;
            public MsiEntry(int code, string en) { Code = code; En = en; }
        }

        // Built-in MSI/MSIExec exit code table (see "MsiExec.exe and InstMsi.exe error
        // messages" on Microsoft Learn). Vendor EXE exit codes are never mapped through it.
        private static readonly MsiEntry[] Msi = new MsiEntry[]
        {
            new MsiEntry(0, "Success."),
            new MsiEntry(1601, "Windows Installer service could not be accessed."),
            new MsiEntry(1602, "Cancelled by the user."),
            new MsiEntry(1603, "Fatal error during installation. The detailed MSI log has the real cause."),
            new MsiEntry(1605, "This product is not installed (unknown product code)."),
            new MsiEntry(1612, "The installation source for this product is not available."),
            new MsiEntry(1618, "Another installation is in progress. Wait and try again."),
            new MsiEntry(1619, "The installation package could not be opened."),
            new MsiEntry(1620, "The installation package could not be opened (invalid Windows Installer package)."),
            new MsiEntry(1625, "System policy forbids this installation."),
            new MsiEntry(1638, "Another version of this product is already installed."),
            new MsiEntry(1639, "Invalid command line argument."),
            new MsiEntry(1641, "Success — the installer started a restart."),
            new MsiEntry(3010, "Success — a restart is required to finish."),
        };

        public static string MsiMeaning(int code)
        {
            for (int i = 0; i < Msi.Length; i++)
                if (Msi[i].Code == code) return L.T(Msi[i].En);
            return "";
        }

        /// <summary>True when the code is a known MSI/MSIExec result.</summary>
        public static bool IsMsiCode(int code)
        {
            return MsiMeaning(code).Length > 0;
        }

        /// <summary>Success for MSI convention: 0, 1605, 1641, 3010.</summary>
        public static bool MsiSucceeded(int code)
        {
            return code == 0 || code == 1605 || code == 1641 || code == 3010;
        }

        /// <summary>Success but the machine must restart (1641 / 3010).</summary>
        public static bool MsiNeedsReboot(int code)
        {
            return code == 1641 || code == 3010;
        }

        /// <summary>1618: another Windows Installer transaction is running.</summary>
        public static bool MsiBusy(int code)
        {
            return code == 1618;
        }

        /// <summary>Local Win32 message for a plain (non-HRESULT) error code.</summary>
        public static string Win32Message(int code)
        {
            if (code == 0) return "";
            StringBuilder sb = new StringBuilder(512);
            // languageId 0 = let Windows pick the message language; size is a character count.
            uint n = FormatMessageW(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero, (uint)code, 0, sb, (uint)sb.Capacity, IntPtr.Zero);
            if (n == 0) return "";
            string s = sb.ToString().Trim();
            return s;
        }

        /// <summary>
        /// Accepts decimal, negative and 0x-hex forms, e.g. "0x80070005", "5", "-2147024891".
        /// </summary>
        public static bool TryParse(string text, out long code)
        {
            code = 0;
            if (string.IsNullOrEmpty(text)) return false;
            string t = text.Trim();
            if (t.Length == 0) return false;
            try
            {
                if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    code = unchecked((long)Convert.ToUInt64(t.Substring(2), 16));
                    return true;
                }
                if (t.StartsWith("-0x", StringComparison.OrdinalIgnoreCase))
                {
                    code = unchecked(-(long)Convert.ToUInt64(t.Substring(3), 16));
                    return true;
                }
                code = long.Parse(t, L.Culture);
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// One-line meaning for any code: Win32 message, HRESULT decode or MSI table entry.
        /// </summary>
        public static string OneLine(long raw)
        {
            uint u = unchecked((uint)raw);
            if (u == 0) return L.T("Success.");

            // HRESULT: facility is bits 16..28, severity is the top bit.
            bool hresult = (u & 0x80000000u) != 0;
            int facility = (int)((u >> 16) & 0x1FFFu);
            int sub = (int)(u & 0xFFFFu);

            if (hresult && facility == 7)
            {
                // FACILITY_WIN32 — the low 16 bits are a Win32 error code.
                string m = Win32Message(sub);
                string ms = MsiMeaning(sub);
                string head = ms.Length > 0 ? ms : m;
                if (head.Length == 0) head = L.T("No local description.");
                return L.F("HRESULT 0x{0:X8} → Win32 {1}: ", u, sub) + head;
            }
            if (hresult)
            {
                return L.F("HRESULT 0x{0:X8} · facility {1} · code {2}", u, facility, sub);
            }
            // Plain code: try Win32 first, then the MSI table.
            string wm = Win32Message((int)u);
            if (wm.Length > 0) return L.F("Win32 {0}: ", u) + wm;
            string mm = MsiMeaning((int)u);
            if (mm.Length > 0) return L.F("MSI {0}: ", u) + mm;
            return L.T("No local description.");
        }

        /// <summary>
        /// Full multi-line explanation for the error dialog / diagnostics page.
        /// </summary>
        public static string Explain(long raw)
        {
            StringBuilder sb = new StringBuilder();
            uint u = unchecked((uint)raw);

            sb.Append(L.F("Value: {0}  (0x{1:X8})", raw, u));
            sb.Append("\r\n");

            if (u == 0)
            {
                sb.Append(L.T("Success. No error occurred."));
                return sb.ToString();
            }

            bool hresult = (u & 0x80000000u) != 0;
            int facility = (int)((u >> 16) & 0x1FFFu);
            int sub = (int)(u & 0xFFFFu);

            if (hresult)
            {
                sb.Append(L.F("HRESULT · facility {0} · code {1}", facility, sub));
                sb.Append("\r\n");
                if (facility == 7)
                {
                    string m = Win32Message(sub);
                    sb.Append(L.F("Win32 {0}: ", sub));
                    sb.Append(m.Length > 0 ? m : L.T("No local description."));
                    sb.Append("\r\n");
                }
            }
            else
            {
                string m = Win32Message((int)u);
                if (m.Length > 0)
                {
                    sb.Append(L.F("Win32 {0}: ", u));
                    sb.Append(m);
                    sb.Append("\r\n");
                }
            }

            // MSI table: direct code, or a FACILITY_WIN32 HRESULT whose sub-code is an MSI code.
            int msiCode = 0;
            if (!hresult && IsMsiCode((int)u)) msiCode = (int)u;
            else if (hresult && facility == 7 && IsMsiCode(sub)) msiCode = sub;

            if (msiCode != 0)
            {
                sb.Append("\r\n");
                sb.Append(L.F("Windows Installer (MSI) code {0}", msiCode));
                sb.Append("\r\n    ");
                sb.Append(MsiMeaning(msiCode));
                sb.Append("\r\n");
                if (msiCode == 1618)
                    sb.Append(L.T("The queue waits and retries the same item a few times before it stops."));
                else if (msiCode == 1603)
                    sb.Append(L.T("Run the uninstall again with an MSI log to see the failing step."));
                else if (msiCode == 3010 || msiCode == 1641)
                    sb.Append(L.T("This is a success result: the change is applied, a restart completes it."));
                sb.Append("\r\n");
            }

            if (hresult && facility != 7 && !IsMsiCode(sub))
                sb.Append(L.T("This HRESULT does not come from a Win32 error code; the raw value is the reliable information here."));
            if (!hresult && Win32Message((int)u).Length == 0 && msiCode == 0)
                sb.Append(L.T("No local description. The value is often a vendor exit code — its meaning is defined by that program."));
            return sb.ToString().TrimEnd();
        }

        /// <summary>
        /// Short status text for an uninstaller exit code. MSI semantics are only applied
        /// when the run really went through msiexec (see UninstallQueue).
        /// </summary>
        public static string DescribeExit(int code, bool msi)
        {
            if (code == 0) return L.T("Uninstalled.");
            if (msi)
            {
                string m = MsiMeaning(code);
                if (m.Length > 0) return L.F("Exit code {0} · {1}", code, m);
            }
            string win = Win32Message(code);
            if (win.Length > 0) return L.F("Exit code {0} · {1}", code, win);
            return L.F("Failed (exit code {0}).", code);
        }
    }
}