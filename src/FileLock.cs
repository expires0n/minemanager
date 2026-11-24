using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace MineManager
{
    internal class LockHolder
    {
        public int Pid;
        public string Name = "";
        public string Service = "";
        public string Type = "";
        public bool Restartable;

        public string TypeText
        {
            get
            {
                switch (Type)
                {
                    case "RmService": return L.T("Service");
                    case "RmExplorer": return "Explorer";
                    case "RmConsole": return L.T("Console");
                    case "RmCriticalProcess": return L.T("Critical process");
                    case "RmMainWindow": return L.T("Application");
                    default: return L.T("Process");
                }
            }
        }

        public string Target
        {
            get { return Service.Length > 0 ? Service : Name; }
        }
    }

    /// <summary>
    /// "What is using this file?" — Restart Manager (rstrtmgr.dll) query.
    /// Read-only: we never shut processes down here, we only report who holds the file
    /// so the user can save their work, close that app and try again.
    /// </summary>
    internal static class FileLock
    {
        private const int CCH_RM_SESSION_KEY = 32;
        private const int CCH_RM_MAX_APP_NAME = 255;
        private const int CCH_RM_MAX_SVC_NAME = 63;
        private const int ERROR_MORE_DATA = 234;

        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public uint dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public string strAppName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public string strServiceShortName;
            public RM_APP_TYPE ApplicationType;
            public uint AppStatus;
            public uint TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint RmStartSession(out uint handle, uint flags, [Out] StringBuilder key);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint RmRegisterResources(uint handle, uint fileCount,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] files,
            uint appCount, [In] RM_UNIQUE_PROCESS[] apps,
            uint serviceCount,
            [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)] string[] services);

        [DllImport("rstrtmgr.dll", ExactSpelling = true)]
        private static extern uint RmGetList(uint handle, out uint needed, ref uint count,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] RM_PROCESS_INFO[] info,
            out uint rebootReasons);

        [DllImport("rstrtmgr.dll", ExactSpelling = true)]
        private static extern uint RmEndSession(uint handle);

        /// <summary>
        /// Returns the applications/services Restart Manager associates with the file.
        /// Empty result means "no holder was reported" — not "the file is certainly free".
        /// </summary>
        public static List<LockHolder> WhoUses(string path, out string note)
        {
            note = "";
            List<LockHolder> list = new List<LockHolder>();
            if (string.IsNullOrEmpty(path)) { note = L.T("No file path."); return list; }

            uint handle = 0;
            StringBuilder key = new StringBuilder(CCH_RM_SESSION_KEY + 1);
            uint err = RmStartSession(out handle, 0, key);
            if (err != 0)
            {
                note = L.F("Restart Manager could not be started (error {0}).", err);
                return list;
            }
            try
            {
                string[] files = new string[] { path };
                err = RmRegisterResources(handle, 1, files, 0, null, 0, null);
                if (err != 0)
                {
                    note = L.F("The file could not be registered (error {0}).", err);
                    return list;
                }

                uint needed = 0;
                uint count = 0;
                uint rebootReasons;
                err = RmGetList(handle, out needed, ref count, null, out rebootReasons);

                // ERROR_MORE_DATA: the list grew while we asked — allocate and read again.
                if (err == ERROR_MORE_DATA)
                {
                    RM_PROCESS_INFO[] info = new RM_PROCESS_INFO[needed];
                    count = needed;
                    err = RmGetList(handle, out needed, ref count, info, out rebootReasons);
                    if (err == 0)
                    {
                        int n = (int)Math.Min(count, (uint)info.Length);
                        for (int i = 0; i < n; i++)
                        {
                            LockHolder h = new LockHolder();
                            h.Pid = (int)info[i].Process.dwProcessId;
                            h.Name = info[i].strAppName == null ? "" : info[i].strAppName;
                            h.Service = info[i].strServiceShortName == null ? "" : info[i].strServiceShortName;
                            h.Restartable = info[i].bRestartable;
                            switch (info[i].ApplicationType)
                            {
                                case RM_APP_TYPE.RmService: h.Type = "RmService"; break;
                                case RM_APP_TYPE.RmExplorer: h.Type = "RmExplorer"; break;
                                case RM_APP_TYPE.RmConsole: h.Type = "RmConsole"; break;
                                case RM_APP_TYPE.RmCritical: h.Type = "RmCriticalProcess"; break;
                                case RM_APP_TYPE.RmMainWindow: h.Type = "RmMainWindow"; break;
                                default: h.Type = "RmUnknownApp"; break;
                            }
                            list.Add(h);
                        }
                    }
                }
                else if (err == 0)
                {
                    // count == 0: nothing reported a lock on this resource.
                    note = L.T("Restart Manager reported no application for this file.");
                }
                else
                {
                    note = L.F("The query failed (error {0}).", err);
                }
            }
            finally
            {
                RmEndSession(handle);
            }

            if (list.Count > 0 && note.Length == 0)
                note = L.T("Close these programs (save your work first) and try the operation again.");
            return list;
        }

        /// <summary>Multi-line report for a log or dialog.</summary>
        public static string Report(string path)
        {
            string note;
            List<LockHolder> list = WhoUses(path, out note);
            StringBuilder sb = new StringBuilder();
            sb.Append(L.T("What is using this file?"));
            sb.Append("\r\n");
            sb.Append(path);
            sb.Append("\r\n\r\n");
            if (list.Count == 0)
            {
                sb.Append(note.Length > 0 ? note : L.T("Restart Manager reported no application for this file."));
                sb.Append("\r\n");
                sb.Append(L.T("An empty result is not a guarantee that the file is free."));
            }
            else
            {
                sb.Append(L.F("{0} application(s):", list.Count));
                sb.Append("\r\n");
                for (int i = 0; i < list.Count; i++)
                {
                    LockHolder h = list[i];
                    sb.Append("• ");
                    sb.Append(h.Target);
                    sb.Append("  ·  ");
                    sb.Append(h.TypeText);
                    sb.Append("  ·  PID ");
                    sb.Append(h.Pid.ToString(L.Culture));
                    if (h.Restartable) sb.Append("  ·  ").Append(L.T("restartable"));
                    sb.Append("\r\n");
                }
                if (note.Length > 0) sb.Append("\r\n").Append(note);
            }
            return sb.ToString();
        }
    }
}