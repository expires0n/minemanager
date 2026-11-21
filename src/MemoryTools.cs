using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MineManager
{
    internal class MemResult
    {
        public long TotalPhys;
        public long AvailPhys;
        public long TotalPageFile;
        public long AvailPageFile;
        public long CommitTotal;
        public int LoadPercent;

        public long UsedPhys { get { return TotalPhys - AvailPhys; } }
        public double UsedRatio
        {
            get { return TotalPhys <= 0 ? 0 : (double)UsedPhys / TotalPhys; }
        }
    }

    internal class ProcMem
    {
        public string Name;
        public int Pid;
        public long WorkingSet;
    }

    internal static class MemoryTools
    {
        public static MemResult Status()
        {
            Native.MEMORYSTATUSEX m = Native.MemoryStatus();
            MemResult r = new MemResult();
            r.TotalPhys = (long)m.ullTotalPhys;
            r.AvailPhys = (long)m.ullAvailPhys;
            r.TotalPageFile = (long)m.ullTotalPageFile;
            r.AvailPageFile = (long)m.ullAvailPageFile;
            r.LoadPercent = (int)m.dwMemoryLoad;
            r.CommitTotal = r.TotalPageFile - r.AvailPageFile;
            return r;
        }

        public static long InstalledRamBytes()
        {
            ulong kb;
            if (Native.GetPhysicallyInstalledSystemMemory(out kb) && kb > 0)
                return (long)kb * 1024;
            return 0;
        }

        public static List<ProcMem> TopProcesses(int count)
        {
            List<ProcMem> list = new List<ProcMem>();
            Process[] ps = Process.GetProcesses();
            for (int i = 0; i < ps.Length; i++)
            {
                try
                {
                    ProcMem p = new ProcMem();
                    p.Name = ps[i].ProcessName;
                    p.Pid = ps[i].Id;
                    p.WorkingSet = ps[i].WorkingSet64;
                    list.Add(p);
                }
                catch { }
                finally { try { ps[i].Dispose(); } catch { } }
            }
            list.Sort(delegate(ProcMem a, ProcMem b) { return b.WorkingSet.CompareTo(a.WorkingSet); });
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        /// <summary>Trims working sets of every accessible process.</summary>
        public static int TrimWorkingSets(out long freedBytes)
        {
            freedBytes = 0;
            int ok = 0;
            long before = (long)Native.MemoryStatus().ullAvailPhys;
            Process[] ps = Process.GetProcesses();
            for (int i = 0; i < ps.Length; i++)
            {
                IntPtr h = IntPtr.Zero;
                try
                {
                    h = Native.OpenProcess(Native.PROCESS_QUERY_LIMITED_INFORMATION | Native.PROCESS_SET_QUOTA,
                        false, ps[i].Id);
                    if (h != IntPtr.Zero && Native.EmptyWorkingSet(h))
                    {
                        ok++;
                    }
                }
                catch { }
                finally
                {
                    if (h != IntPtr.Zero) Native.CloseHandle(h);
                    try { ps[i].Dispose(); } catch { }
                }
            }
            try
            {
                Native.SetProcessWorkingSetSize(Native.GetCurrentProcess(), new IntPtr(-1), new IntPtr(-1));
            }
            catch { }

            System.Threading.Thread.Sleep(350);
            long after = (long)Native.MemoryStatus().ullAvailPhys;
            freedBytes = after - before;
            if (freedBytes < 0) freedBytes = 0;
            return ok;
        }

        /// <summary>Purges standby / modified page lists. Needs SeProfileSingleProcessPrivilege.</summary>
        public static string PurgeSystemMemory()
        {
            if (!Native.EnablePrivilege("SeProfileSingleProcessPrivilege"))
                return L.T("Privilege not acquired (run as administrator)");

            List<string> results = new List<string>();

            int cmd = Native.MemoryFlushModifiedList;
            int rc = Native.NtSetSystemInformation(Native.SystemMemoryListInformation, ref cmd, 4);
            results.Add(L.F("Modified page list: {0}", rc == 0 ? "OK" : "0x" + rc.ToString("X8")));

            cmd = Native.MemoryPurgeStandbyList;
            rc = Native.NtSetSystemInformation(Native.SystemMemoryListInformation, ref cmd, 4);
            results.Add(L.F("Standby list: {0}", rc == 0 ? "OK" : "0x" + rc.ToString("X8")));

            cmd = Native.MemoryPurgeLowPriorityStandbyList;
            rc = Native.NtSetSystemInformation(Native.SystemMemoryListInformation, ref cmd, 4);
            results.Add(L.F("Low priority standby: {0}", rc == 0 ? "OK" : "0x" + rc.ToString("X8")));

            cmd = Native.MemoryEmptyWorkingSets;
            rc = Native.NtSetSystemInformation(Native.SystemMemoryListInformation, ref cmd, 4);
            results.Add(L.F("System working sets: {0}", rc == 0 ? "OK" : "0x" + rc.ToString("X8")));

            return string.Join(" · ", results.ToArray());
        }
    }
}
