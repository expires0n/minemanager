using System;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;

namespace MineManager
{
    internal static class Native
    {
        // ------------------------------------------------------------------ memory

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetPhysicallyInstalledSystemMemory(out ulong kilobytes);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EmptyWorkingSet(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr h);

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const uint PROCESS_SET_QUOTA = 0x0100;

        // -------------------------------------------------------------- cpu times

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);

        // -------------------------------------------------------------- privileges

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES { public LUID Luid; public uint Attributes; }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID_AND_ATTRIBUTES Privilege; }

        public const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const uint TOKEN_QUERY = 0x0008;
        public const uint SE_PRIVILEGE_ENABLED = 0x0002;

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LookupPrivilegeValue(string system, string name, out LUID luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll,
            ref TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LocalFree(IntPtr h);

        // ------------------------------------------------------- native api memory

        [DllImport("ntdll.dll")]
        public static extern int NtSetSystemInformation(int infoClass, ref int info, int infoLength);

        public const int SystemMemoryListInformation = 80;
        public const int MemoryEmptyWorkingSets = 2;
        public const int MemoryFlushModifiedList = 3;
        public const int MemoryPurgeStandbyList = 4;
        public const int MemoryPurgeLowPriorityStandbyList = 5;

        [DllImport("ntdll.dll")]
        public static extern int NtQuerySystemInformation(int infoClass, IntPtr info, int infoLength, out int returnLength);


        // ------------------------------------------------------------------- dwm

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        // ------------------------------------------------------------ window chrome

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        public const int SB_VERT = 1;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(int action, int param, out int value, int winIni);

        public const int SPI_GETCLIENTAREAANIMATION = 0x1042;

        public static bool AnimationsEnabled()
        {
            try
            {
                int value = 1;
                if (!SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, out value, 0)) return true;
                return value != 0;
            }
            catch { return true; }
        }

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int WM_NCHITTEST = 0x84;
        public const int WM_GETMINMAXINFO = 0x24;
        public const int HTCAPTION = 2;
        public const int HTLEFT = 10;
        public const int HTRIGHT = 11;
        public const int HTTOP = 12;
        public const int HTTOPLEFT = 13;
        public const int HTTOPRIGHT = 14;
        public const int HTBOTTOM = 15;
        public const int HTBOTTOMLEFT = 16;
        public const int HTBOTTOMRIGHT = 17;

        // -------------------------------------------------------------- recycle bin

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHQUERYRBINFO
        {
            public uint cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        public const uint SHERB_NOCONFIRMATION = 0x1;
        public const uint SHERB_NOPROGRESSUI = 0x2;
        public const uint SHERB_NOSOUND = 0x4;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHQueryRecycleBin(string rootPath, ref SHQUERYRBINFO info);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        public static extern int SHEmptyRecycleBin(IntPtr hwnd, string rootPath, uint flags);

        // ---------------------------------------------------------------- file ops

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool MoveFileEx(string existing, string next, uint flags);

        public const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        // --------------------------------------------------------------------- misc

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool GetDiskFreeSpaceEx(string drive, out ulong free,
            out ulong total, out ulong totalFree);

        [DllImport("kernel32.dll")]
        public static extern ulong GetTickCount64();

        public static bool IsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal p = new WindowsPrincipal(id);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        public static bool EnablePrivilege(string name)
        {
            IntPtr token = IntPtr.Zero;
            try
            {
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
                    return false;
                LUID luid;
                if (!LookupPrivilegeValue(null, name, out luid)) return false;
                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Privilege.Luid = luid;
                tp.Privilege.Attributes = SE_PRIVILEGE_ENABLED;
                if (!AdjustTokenPrivileges(token, false, ref tp,
                        (uint)Marshal.SizeOf(typeof(TOKEN_PRIVILEGES)), IntPtr.Zero, IntPtr.Zero))
                    return false;
                return Marshal.GetLastWin32Error() == 0;
            }
            catch { return false; }
            finally
            {
                if (token != IntPtr.Zero) CloseHandle(token);
            }
        }

        public static MEMORYSTATUSEX MemoryStatus()
        {
            MEMORYSTATUSEX m = new MEMORYSTATUSEX();
            m.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref m);
            return m;
        }

        public static long RecycleBinSize(out long items)
        {
            items = 0;
            SHQUERYRBINFO info = new SHQUERYRBINFO();
            info.cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO));
            int hr = SHQueryRecycleBin(null, ref info);
            if (hr != 0) return 0;
            items = info.i64NumItems;
            return info.i64Size;
        }
    }

    internal static class CpuLoad
    {
        private static ulong _prevIdle, _prevKernel, _prevUser;
        private static bool _primed;

        public static double Next()
        {
            Native.FILETIME idle, kernel, user;
            if (!Native.GetSystemTimes(out idle, out kernel, out user)) return 0;
            ulong i = ((ulong)idle.dwHighDateTime << 32) | idle.dwLowDateTime;
            ulong k = ((ulong)kernel.dwHighDateTime << 32) | kernel.dwLowDateTime;
            ulong u = ((ulong)user.dwHighDateTime << 32) | user.dwLowDateTime;
            if (!_primed)
            {
                _prevIdle = i; _prevKernel = k; _prevUser = u;
                _primed = true;
                return 0;
            }
            ulong di = i - _prevIdle, dk = k - _prevKernel, du = u - _prevUser;
            _prevIdle = i; _prevKernel = k; _prevUser = u;
            ulong total = dk + du;
            if (total == 0) return 0;
            double busy = (double)(total - di) / total * 100.0;
            if (busy < 0) busy = 0;
            if (busy > 100) busy = 100;
            return busy;
        }
    }
}
