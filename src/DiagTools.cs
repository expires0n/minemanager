using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace MineManager
{
    // ============================================================== battery

    internal class BatteryInfo
    {
        public bool Present;
        public string Name = "";
        public string DesignCapacity = "";
        public string FullChargeCapacity = "";
        public string CycleCount = "";
        public double HealthPercent = -1;
        public string Error = "";
    }

    /// <summary>Reads "powercfg /batteryreport /xml" and reports capacity health.</summary>
    internal static class PowerTools
    {
        public static BatteryInfo ReadBattery(Action<string> progress)
        {
            BatteryInfo b = new BatteryInfo();
            string file = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                "mm-battery-" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".xml");
            try
            {
                if (progress != null) progress(L.T("Creating the battery report..."));
                RunResult r = ProcRunner.Run("powercfg",
                    "/batteryreport /output \"" + file + "\" /xml", 90, null, null);
                if (!System.IO.File.Exists(file))
                {
                    b.Error = r.Output.Trim().Length > 0
                        ? r.Output.Trim()
                        : L.T("No battery report was produced.");
                    return b;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                XmlNodeList batteries = doc.SelectNodes("//*[local-name()='Battery']");
                if (batteries == null || batteries.Count == 0)
                {
                    // A report without batteries is normal on desktop machines.
                    b.Error = L.T("No battery information on this machine.");
                    return b;
                }

                XmlNode n = batteries[0];
                b.Present = true;
                b.Name = FirstValue(n, "Manufacturer") + " " + FirstValue(n, "SerialNumber");
                b.Name = b.Name.Trim();
                b.DesignCapacity = FirstValue(n, "DesignCapacity");
                b.FullChargeCapacity = FirstValue(n, "FullChargeCapacity");
                b.CycleCount = FirstValue(n, "CycleCount");
                if (b.CycleCount.Length == 0) b.CycleCount = L.T("no data");

                double design, full;
                if (double.TryParse(b.DesignCapacity, NumberStyles.Any, CultureInfo.InvariantCulture, out design) &&
                    double.TryParse(b.FullChargeCapacity, NumberStyles.Any, CultureInfo.InvariantCulture, out full) &&
                    design > 0)
                {
                    // "reported full charge / design capacity" — values over 100 % are kept.
                    b.HealthPercent = 100.0 * full / design;
                }
                return b;
            }
            catch (Exception ex)
            {
                b.Error = ex.Message;
                return b;
            }
            finally
            {
                try { if (System.IO.File.Exists(file)) System.IO.File.Delete(file); }
                catch { }
            }
        }

        private static string FirstValue(XmlNode parent, string localName)
        {
            XmlNodeList kids = parent.ChildNodes;
            for (int i = 0; i < kids.Count; i++)
                if (string.Equals(kids[i].LocalName, localName, StringComparison.OrdinalIgnoreCase))
                {
                    string v = kids[i].InnerText == null ? "" : kids[i].InnerText.Trim();
                    return v;
                }
            return "";
        }
    }

    // ============================================================== display

    internal class DisplayEntry
    {
        public string Key = "";
        public string Name = "";
        public string Technology = "";
        public bool Available;

        public string Line
        {
            get
            {
                string s = Name.Length > 0 ? Name : L.T("Unnamed display");
                if (Technology.Length > 0) s += "  ·  " + Technology;
                if (!Available) s += "  ·  " + L.T("not active");
                return s;
            }
        }
    }

    /// <summary>
    /// Two display snapshots compared: what appeared, what disappeared. It only reads
    /// the active path topology — it never changes a display mode.
    /// </summary>
    internal static class DisplayTools
    {
        private const uint QDC_ONLY_ACTIVE_PATHS = 2;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private const uint GET_TARGET_NAME = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RATIONAL { public uint Numerator; public uint Denominator; }

        [StructLayout(LayoutKind.Sequential)]
        private struct PATH_SOURCE
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PATH_TARGET
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public RATIONAL refreshRate;
            public uint scanLineOrdering;
            public int targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_INFO
        {
            public PATH_SOURCE sourceInfo;
            public PATH_TARGET targetInfo;
            public uint flags;
        }

        // The mode info ends with a 48-byte inline union; it stays opaque here because
        // this tool only compares paths and reads target names.
        [StructLayout(LayoutKind.Explicit, Size = 64)]
        private struct DISPLAYCONFIG_MODE_INFO
        {
            [FieldOffset(0)] public uint infoType;
            [FieldOffset(4)] public uint id;
            [FieldOffset(8)] public LUID adapterId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVICE_INFO_HEADER
        {
            public uint type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TARGET_DEVICE_NAME
        {
            public DEVICE_INFO_HEADER header;
            public uint flags;
            public uint outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string friendlyName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string devicePath;
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint paths, out uint modes);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int QueryDisplayConfig(uint flags, ref uint paths,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray, ref uint modes,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeArray, IntPtr nullTopologyId);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int DisplayConfigGetDeviceInfo(ref TARGET_DEVICE_NAME packet);

        public static List<DisplayEntry> Capture(out string note)
        {
            note = "";
            List<DisplayEntry> list = new List<DisplayEntry>();
            try
            {
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    uint pathCount, modeCount;
                    int err = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount);
                    if (err != 0) { note = L.F("Query failed (error {0}).", err); return list; }
                    if (pathCount == 0) { note = L.T("No active display path was reported."); return list; }

                    DISPLAYCONFIG_PATH_INFO[] paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
                    DISPLAYCONFIG_MODE_INFO[] modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
                    uint pc = pathCount;
                    uint mc = modeCount;
                    err = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pc, paths, ref mc, modes, IntPtr.Zero);
                    if (err == ERROR_INSUFFICIENT_BUFFER) continue;   // topology changed — ask again
                    if (err != 0) { note = L.F("Query failed (error {0}).", err); return list; }

                    int n = (int)Math.Min(pc, (uint)paths.Length);
                    for (int i = 0; i < n; i++)
                    {
                        DisplayEntry e = new DisplayEntry();
                        e.Key = KeyOf(paths[i]);
                        e.Available = paths[i].targetInfo.targetAvailable != 0;
                        e.Technology = TechName(paths[i].targetInfo.outputTechnology);
                        TARGET_DEVICE_NAME name = new TARGET_DEVICE_NAME();
                        name.header.type = GET_TARGET_NAME;
                        name.header.size = (uint)Marshal.SizeOf(typeof(TARGET_DEVICE_NAME));
                        name.header.adapterId = paths[i].targetInfo.adapterId;
                        name.header.id = paths[i].targetInfo.id;
                        if (DisplayConfigGetDeviceInfo(ref name) == 0 &&
                            name.friendlyName != null && name.friendlyName.Trim().Length > 0)
                            e.Name = name.friendlyName.Trim();
                        list.Add(e);
                    }
                    return list;
                }
                note = L.T("The display topology changed while it was read.");
            }
            catch (Exception ex) { note = ex.Message; }
            return list;
        }

        private static string KeyOf(DISPLAYCONFIG_PATH_INFO p)
        {
            return p.targetInfo.adapterId.LowPart.ToString("X8", CultureInfo.InvariantCulture) + "-" +
                   p.targetInfo.adapterId.HighPart.ToString("X8", CultureInfo.InvariantCulture) + ":" +
                   p.targetInfo.id.ToString(CultureInfo.InvariantCulture) + "←" +
                   p.sourceInfo.id.ToString(CultureInfo.InvariantCulture);
        }

        private static string TechName(uint t)
        {
            switch (t)
            {
                case 1: return "VGA";
                case 2: return "S-Video";
                case 3: return "Composite";
                case 4: return "Component";
                case 5: return "DVI";
                case 6: return "HDMI";
                case 7: return "LVDS";
                case 8: return "D-JPN";
                case 9: return "SDI";
                case 10: return "DisplayPort";
                case 11: return "eDP";
                case 12: return "UDI";
                case 13: return "UDI (embedded)";
                case 14: return "SDTV dongle";
                case 15: return "Miracast";
                case 16: return "Indirect (wired)";
                case 17: return "Indirect (virtual)";
                case 0x80000000: return L.T("Internal");
                default: return "";
            }
        }

        /// <summary>Human diff of two snapshots. It is a topology diff, not a cable test.</summary>
        public static string Compare(List<DisplayEntry> before, List<DisplayEntry> after)
        {
            StringBuilder sb = new StringBuilder();
            int added = 0, removed = 0;

            for (int i = 0; i < after.Count; i++)
            {
                if (Find(before, after[i].Key) == null)
                {
                    added++;
                    sb.Append(L.F("+ appeared: {0}", after[i].Line));
                    sb.Append("\r\n");
                }
            }
            for (int i = 0; i < before.Count; i++)
            {
                if (Find(after, before[i].Key) == null)
                {
                    removed++;
                    sb.Append(L.F("- disappeared: {0}", before[i].Line));
                    sb.Append("\r\n");
                }
            }
            for (int i = 0; i < after.Count; i++)
            {
                DisplayEntry old = Find(before, after[i].Key);
                if (old != null && old.Line != after[i].Line)
                {
                    sb.Append(L.F("· changed: {0} → {1}", old.Line, after[i].Line));
                    sb.Append("\r\n");
                }
            }
            if (added == 0 && removed == 0 && sb.Length == 0)
                sb.Append(L.T("No difference between the two measurements."));
            sb.Append("\r\n");
            sb.Append(L.T("A missing active path is not proof of a broken cable; only the topology is compared."));
            return sb.ToString();
        }

        private static DisplayEntry Find(List<DisplayEntry> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].Key == key) return list[i];
            return null;
        }
    }

    // ================================================================== route

    internal class RouteInfo
    {
        public string Destination = "";
        public string SourceIp = "";
        public string NextHop = "";
        public string InterfaceName = "";
        public int InterfaceIndex;
        public string Error = "";
    }

    /// <summary>
    /// "Windows'un bu hedef için seçtiği rota" — the route Windows would pick for one
    /// destination. It says nothing about reachability: no ping, no DNS, no remote test.
    /// </summary>
    internal static class RouteTools
    {
        [StructLayout(LayoutKind.Explicit, Size = 28)]
        private struct SOCKADDR_INET
        {
            [FieldOffset(0)] public ushort family;
            [FieldOffset(2)] public ushort port;
            [FieldOffset(4)] public uint v4;
            [FieldOffset(4)] public uint flowInfo;
            [FieldOffset(8)] public uint v6a;
            [FieldOffset(12)] public uint v6b;
            [FieldOffset(16)] public uint v6c;
            [FieldOffset(20)] public uint v6d;
            [FieldOffset(24)] public uint scopeId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADDRESS_PREFIX
        {
            public SOCKADDR_INET Prefix;
            public byte PrefixLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_IPFORWARD_ROW2
        {
            public ulong InterfaceLuid;
            public uint InterfaceIndex;
            public IP_ADDRESS_PREFIX DestinationPrefix;
            public SOCKADDR_INET NextHop;
            public byte SitePrefixLength;
            public uint ValidLifetime;
            public uint PreferredLifetime;
            public uint Metric;
            public uint Protocol;
            public byte Loopback;
            public byte AutoconfigureAddress;
            public byte Publish;
            public byte Immortal;
            public uint Age;
            public uint Origin;
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern uint GetBestRoute2(IntPtr optionalInterfaceLuid, uint interfaceIndex,
            IntPtr optionalSourceAddress, ref SOCKADDR_INET destinationAddress,
            uint addressSortOptions, out MIB_IPFORWARD_ROW2 bestRoute,
            out SOCKADDR_INET bestSourceAddress);

        public static RouteInfo Query(string host)
        {
            RouteInfo info = new RouteInfo();
            info.Destination = host == null ? "" : host.Trim();
            IPAddress ip;
            if (!IPAddress.TryParse(info.Destination, out ip))
            {
                info.Error = L.T("Enter a numeric IPv4 or IPv6 address. DNS names are not resolved here.");
                return info;
            }

            try
            {
                SOCKADDR_INET dest = new SOCKADDR_INET();
                byte[] bytes = ip.GetAddressBytes();
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    dest.family = 2;               // AF_INET
                    dest.v4 = BitConverter.ToUInt32(bytes, 0);
                }
                else
                {
                    dest.family = 23;              // AF_INET6
                    dest.flowInfo = 0;
                    dest.v6a = BitConverter.ToUInt32(bytes, 0);
                    dest.v6b = BitConverter.ToUInt32(bytes, 4);
                    dest.v6c = BitConverter.ToUInt32(bytes, 8);
                    dest.v6d = BitConverter.ToUInt32(bytes, 12);
                    try { dest.scopeId = (uint)ip.ScopeId; }
                    catch { }
                }

                MIB_IPFORWARD_ROW2 row;
                SOCKADDR_INET source;
                uint err = GetBestRoute2(IntPtr.Zero, 0, IntPtr.Zero, ref dest, 0, out row, out source);
                if (err != 0)
                {
                    info.Error = L.F("The route query failed (error {0}).", err);
                    return info;
                }

                info.InterfaceIndex = (int)row.InterfaceIndex;
                info.SourceIp = AddrToString(source);
                info.NextHop = AddrToString(row.NextHop);
                info.InterfaceName = NameOf(row.InterfaceIndex);
            }
            catch (Exception ex)
            {
                info.Error = ex.Message;
            }
            return info;
        }

        private static string AddrToString(SOCKADDR_INET a)
        {
            try
            {
                if (a.family == 2)
                {
                    byte[] b = BitConverter.GetBytes(a.v4);
                    return b[0].ToString(CultureInfo.InvariantCulture) + "." +
                           b[1].ToString(CultureInfo.InvariantCulture) + "." +
                           b[2].ToString(CultureInfo.InvariantCulture) + "." +
                           b[3].ToString(CultureInfo.InvariantCulture);
                }
                if (a.family == 23)
                {
                    byte[] b = new byte[16];
                    BitConverter.GetBytes(a.v6a).CopyTo(b, 0);
                    BitConverter.GetBytes(a.v6b).CopyTo(b, 4);
                    BitConverter.GetBytes(a.v6c).CopyTo(b, 8);
                    BitConverter.GetBytes(a.v6d).CopyTo(b, 12);
                    IPAddress ip = new IPAddress(b);
                    return ip.ToString() + (a.scopeId != 0 ? "%" + a.scopeId.ToString(CultureInfo.InvariantCulture) : "");
                }
            }
            catch { }
            return "";
        }

        private static string NameOf(uint index)
        {
            try
            {
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                for (int i = 0; i < nics.Length; i++)
                {
                    try
                    {
                        IPInterfaceProperties props = nics[i].GetIPProperties();
                        IPv4InterfaceProperties v4 = props.GetIPv4Properties();
                        if (v4 != null && (uint)v4.Index == index) return nics[i].Name;
                    }
                    catch { }
                }
            }
            catch { }
            return L.F("Interface #{0}", index);
        }
    }

    // ============================================================== timeline

    internal class TimelineEntry
    {
        public DateTime Time;
        public string Channel = "";
        public string Provider = "";
        public int Id;
        public string Summary = "";

        public string TimeText
        {
            get { return Time.ToString("yyyy-MM-dd HH:mm", L.Culture); }
        }
    }

    /// <summary>
    /// Maintenance timeline: Windows event records next to each other in time.
    /// Records close in time do not prove that one caused the other.
    /// </summary>
    internal static class EventTimeline
    {
        private class Watch
        {
            public string Channel;
            public string Provider;
            public int Id;
            public Watch(string channel, string provider, int id)
            {
                Channel = channel; Provider = provider; Id = id;
            }
        }

        private static readonly Watch[] Watches = new Watch[]
        {
            new Watch("System", "Microsoft-Windows-Kernel-Power", 41),
            new Watch("System", "EventLog", 6008),
            new Watch("System", "Microsoft-Windows-WER-SystemErrorReporting", 1001),
            new Watch("Application", "Application Error", 1000),
            new Watch("Application", "Windows Error Reporting", 1001),
        };

        public static List<TimelineEntry> Recent(int hours, out string note)
        {
            note = "";
            List<TimelineEntry> list = new List<TimelineEntry>();
            DateTime since = DateTime.UtcNow.AddHours(-Math.Max(1, hours));
            string stamp = since.ToString("yyyy-MM-ddTHH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

            for (int w = 0; w < Watches.Length; w++)
            {
                Watch x = Watches[w];
                string xpath = "*[System[(EventID=" + x.Id.ToString(CultureInfo.InvariantCulture) +
                    ") and Provider[@Name='" + x.Provider + "'] and " +
                    "TimeCreated[@SystemTime>='" + stamp + "']]]";
                try
                {
                    EventLogQuery q = new EventLogQuery(x.Channel, PathType.LogName, xpath);
                    using (EventLogReader reader = new EventLogReader(q))
                    {
                        int taken = 0;
                        EventRecord rec;
                        while (taken < 40 && (rec = reader.ReadEvent()) != null)
                        {
                            using (rec)
                            {
                                TimelineEntry e = new TimelineEntry();
                                e.Channel = x.Channel;
                                e.Provider = x.Provider;
                                e.Id = x.Id;
                                e.Time = rec.TimeCreated.HasValue
                                    ? rec.TimeCreated.Value.ToLocalTime() : DateTime.MinValue;
                                try { e.Summary = rec.FormatDescription() ?? ""; }
                                catch { e.Summary = ""; }
                                if (e.Summary.Length > 160) e.Summary = e.Summary.Substring(0, 160) + "…";
                                list.Add(e);
                                taken++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // A channel can be missing or restricted; keep the rest of the timeline.
                    if (note.Length == 0) note = L.F("{0}: {1}", x.Channel, ex.Message);
                }
            }

            list.Sort(delegate(TimelineEntry a, TimelineEntry b)
            {
                return b.Time.CompareTo(a.Time);
            });
            return list;
        }

        public static string Warning
        {
            get
            {
                return L.T("These events are close in time; the records alone do not show that one caused the other.");
            }
        }
    }
}