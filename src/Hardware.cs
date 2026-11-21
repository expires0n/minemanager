using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;
using System.Text;
using Microsoft.Win32;

namespace MineManager
{
    internal class SpecRow
    {
        public string Name;
        public string Value;
        public SpecRow(string name, string value) { Name = name; Value = value; }
    }

    internal class SpecSection
    {
        public string Title;
        public List<SpecRow> Rows = new List<SpecRow>();
        public SpecSection(string title) { Title = title; }
        public SpecSection Add(string name, string value)
        {
            if (!string.IsNullOrEmpty(value)) Rows.Add(new SpecRow(name, value));
            return this;
        }
    }

    internal static class Hardware
    {
        private static string P(ManagementObject o, string name)
        {
            try
            {
                object v = o[name];
                if (v == null) return "";
                if (v is string[]) return string.Join(", ", (string[])v);
                if (v is DateTime) return ((DateTime)v).ToString("dd.MM.yyyy", CultureInfo.CurrentCulture);
                if (v is ulong) return ((ulong)v).ToString(CultureInfo.CurrentCulture);
                if (v is uint) return ((uint)v).ToString(CultureInfo.CurrentCulture);
                if (v is long) return ((long)v).ToString(CultureInfo.CurrentCulture);
                string s = v.ToString();
                s = s.Trim();
                if (s == "0") s = "";
                return s;
            }
            catch { return ""; }
        }

        private static List<ManagementObject> Query(string wql)
        {
            List<ManagementObject> list = new List<ManagementObject>();
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(wql))
                using (ManagementObjectCollection c = s.Get())
                {
                    foreach (ManagementObject o in c) list.Add(o);
                }
            }
            catch { }
            return list;
        }

        private static string DateOf(string wmiDate)
        {
            if (string.IsNullOrEmpty(wmiDate)) return "";
            try { return ManagementDateTimeConverter.ToDateTime(wmiDate).ToString("dd.MM.yyyy HH:mm"); }
            catch { return wmiDate; }
        }

        private static string MemType(int smbios, int legacy)
        {
            switch (smbios)
            {
                case 20: return "DDR";
                case 21: return "DDR2";
                case 22: return "DDR2 FB-DIMM";
                case 24: return "DDR3";
                case 26: return "DDR4";
                case 27: return "LPDDR";
                case 28: return "LPDDR2";
                case 29: return "LPDDR3";
                case 30: return "LPDDR4";
                case 34: return "DDR5";
                case 35: return "LPDDR5";
            }
            switch (legacy)
            {
                case 2: return "DRAM";
                case 3: return "SDRAM";
                case 20: return "DDR";
                case 21: return "DDR2";
                case 24: return "DDR3";
            }
            return "";
        }

        private static string FormFactor(int ff)
        {
            switch (ff)
            {
                case 8: return "DIMM";
                case 12: return "SODIMM";
                case 11: return "RIMM";
                case 13: return "SRIMM";
                case 2: return "SIP";
                case 9: return "TSOP";
            }
            return "";
        }

        private static string Arch(ushort a)
        {
            switch (a)
            {
                case 0: return "x86";
                case 1: return "MIPS";
                case 2: return "Alpha";
                case 3: return "PowerPC";
                case 5: return "ARM";
                case 6: return "Itanium";
                case 9: return "x64";
                case 12: return "ARM64";
            }
            return "";
        }

        private static string Bool(object v)
        {
            if (v == null) return "";
            try { return ((bool)v) ? L.T("Enabled") : L.T("Disabled"); }
            catch { }
            return v.ToString();
        }

        // ------------------------------------------------------------------ collect

        public static List<SpecSection> Collect(Action<string> progress)
        {
            List<SpecSection> list = new List<SpecSection>();

            if (progress != null) progress(L.T("Processor information is being read..."));
            list.Add(CpuSection());

            if (progress != null) progress(L.T("Motherboard and BIOS are being read..."));
            list.Add(BoardSection());

            if (progress != null) progress(L.T("Memory modules are being read..."));
            list.Add(RamSection());

            if (progress != null) progress(L.T("Graphics card is being read..."));
            list.Add(GpuSection());

            if (progress != null) progress(L.T("Disks are being read..."));
            list.Add(DiskSection());

            if (progress != null) progress(L.T("Operating system information is being read..."));
            list.Add(OsSection());

            if (progress != null) progress(L.T("Network and other devices are being read..."));
            SpecSection net = NetSection();
            if (net.Rows.Count > 0) list.Add(net);

            SpecSection misc = MiscSection();
            if (misc.Rows.Count > 0) list.Add(misc);

            return list;
        }

        private static SpecSection CpuSection()
        {
            SpecSection s = new SpecSection(L.T("CPU (processor)"));
            string name = "", vendor = "", identifier = "", socket = "", maxClock = "", curClock = "";
            string cores = "", threads = "", l2 = "", l3 = "", arch = "", virt = "", load = "", stepping = "";

            List<ManagementObject> cpus = Query("SELECT * FROM Win32_Processor");
            foreach (ManagementObject o in cpus)
            {
                name = P(o, "Name");
                vendor = P(o, "Manufacturer");
                socket = P(o, "SocketDesignation");
                maxClock = P(o, "MaxClockSpeed");
                curClock = P(o, "CurrentClockSpeed");
                cores = P(o, "NumberOfCores");
                threads = P(o, "NumberOfLogicalProcessors");
                l2 = P(o, "L2CacheSize");
                l3 = P(o, "L3CacheSize");
                arch = Arch((ushort)SafeInt(P(o, "Architecture")));
                virt = Bool(o["VirtualizationFirmwareEnabled"]);
                load = P(o, "LoadPercentage");
                try { stepping = o["Revision"] == null ? "" : ((ushort)o["Revision"]).ToString(CultureInfo.CurrentCulture); }
                catch { }
                break;
            }

            try
            {
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                {
                    if (k != null)
                    {
                        string rn = Reg.Str(k, "ProcessorNameString");
                        if (rn.Length > 0) name = rn;
                        string rid = Reg.Str(k, "Identifier");
                        if (rid.Length > 0) identifier = rid;
                        string rv = Reg.Str(k, "VendorIdentifier");
                        if (rv.Length > 0 && vendor.Length == 0) vendor = rv;
                        string rmhz = Reg.Str(k, "~MHz");
                        if (rmhz.Length > 0 && curClock.Length == 0) curClock = rmhz;
                    }
                }
            }
            catch { }

            s.Add(L.T("Name"), name);
            s.Add(L.T("Manufacturer"), vendor);
            s.Add(L.T("Identifier"), identifier);
            if (cores.Length > 0)
                s.Add(L.T("Cores / threads"), L.F("{0} cores / {1} threads", cores,
                    threads.Length > 0 ? threads : cores));
            if (maxClock.Length > 0)
                s.Add(L.T("Maximum speed"), curClock.Length > 0
                    ? L.F("{0} MHz (current {1} MHz)", maxClock, curClock) : maxClock + " MHz");
            if (l2.Length > 0) s.Add(L.T("L2 cache"), l2 + " KB");
            if (l3.Length > 0) s.Add(L.T("L3 cache"), l3 + " KB");
            s.Add(L.T("Socket"), socket);
            s.Add(L.T("Architecture"), arch);
            s.Add(L.T("Virtualization"), virt);
            if (load.Length > 0) s.Add(L.T("Current load"), load + " %");
            if (stepping.Length > 0) s.Add("Stepping", stepping);
            return s;
        }

        private static int SafeInt(string s)
        {
            int v;
            if (int.TryParse(s, out v)) return v;
            return -1;
        }

        private static SpecSection BoardSection()
        {
            SpecSection s = new SpecSection(L.T("Motherboard & BIOS"));
            foreach (ManagementObject o in Query("SELECT * FROM Win32_BaseBoard"))
            {
                s.Add(L.T("Motherboard manufacturer"), P(o, "Manufacturer"));
                s.Add(L.T("Motherboard model"), P(o, "Product"));
                s.Add(L.T("Motherboard version"), P(o, "Version"));
                s.Add(L.T("Motherboard serial"), P(o, "SerialNumber"));
                break;
            }
            foreach (ManagementObject o in Query("SELECT * FROM Win32_BIOS"))
            {
                s.Add(L.T("BIOS manufacturer"), P(o, "Manufacturer"));
                s.Add(L.T("BIOS version"), P(o, "SMBIOSBIOSVersion") != "" ? P(o, "SMBIOSBIOSVersion") : P(o, "Version"));
                s.Add(L.T("BIOS date"), DateOf(P(o, "ReleaseDate")));
                break;
            }
            foreach (ManagementObject o in Query("SELECT * FROM Win32_ComputerSystem"))
            {
                s.Add(L.T("System manufacturer"), P(o, "Manufacturer"));
                s.Add(L.T("System model"), P(o, "Model"));
                s.Add(L.T("System type"), P(o, "SystemType"));
                break;
            }
            return s;
        }

        private static SpecSection RamSection()
        {
            SpecSection s = new SpecSection(L.T("Memory (RAM)"));
            long installed = MemoryTools.InstalledRamBytes();
            Native.MEMORYSTATUSEX m = Native.MemoryStatus();

            s.Add(L.T("Installed memory"), Fmt.Size(installed > 0 ? installed : (long)m.ullTotalPhys));
            s.Add(L.T("Available memory"), L.F("{0}  ({1} in use)", Fmt.Size((long)m.ullAvailPhys),
                L.Percent((int)m.dwMemoryLoad)));

            int idx = 1;
            foreach (ManagementObject o in Query("SELECT * FROM Win32_PhysicalMemory"))
            {
                long cap = 0;
                try { cap = (long)Convert.ToUInt64(o["Capacity"]); } catch { }
                int smb = 0, legacy = 0, ff = 0;
                try { smb = Convert.ToInt32(o["SMBIOSMemoryType"]); } catch { }
                try { legacy = Convert.ToInt32(o["MemoryType"]); } catch { }
                try { ff = Convert.ToInt32(o["FormFactor"]); } catch { }

                string title = L.F("Module {0}", idx);
                string type = MemType(smb, legacy);
                string form = FormFactor(ff);
                if (type.Length > 0) title += " · " + type;
                if (form.Length > 0) title += " " + form;
                if (cap > 0) title += " · " + Fmt.Size(cap);

                StringBuilder sb = new StringBuilder();
                string manuf = P(o, "Manufacturer");
                string part = P(o, "PartNumber");
                string speed = P(o, "ConfiguredClockSpeed");
                if (speed.Length == 0) speed = P(o, "Speed");
                string slot = P(o, "DeviceLocator");
                if (slot.Length == 0) slot = P(o, "BankLabel");

                if (manuf.Length > 0) sb.Append(manuf);
                if (part.Length > 0) { if (sb.Length > 0) sb.Append(" · "); sb.Append(part); }
                if (speed.Length > 0 && speed != "0") { if (sb.Length > 0) sb.Append(" · "); sb.Append(speed + " MHz"); }
                if (slot.Length > 0) { if (sb.Length > 0) sb.Append(" · "); sb.Append(slot); }

                s.Add(title, sb.ToString());
                idx++;
            }
            return s;
        }

        private static SpecSection GpuSection()
        {
            SpecSection s = new SpecSection(L.T("Graphics"));
            int idx = 0;
            foreach (ManagementObject o in Query("SELECT * FROM Win32_VideoController"))
            {
                idx++;
                string name = P(o, "Name");
                StringBuilder sb = new StringBuilder();
                string drv = P(o, "DriverVersion");
                string drvDate = DateOf(P(o, "DriverDate"));
                string res = "";
                string w = P(o, "CurrentHorizontalResolution");
                string h = P(o, "CurrentVerticalResolution");
                string hz = P(o, "CurrentRefreshRate");
                if (w.Length > 0 && h.Length > 0) res = w + " x " + h + (hz.Length > 0 ? " @ " + hz + " Hz" : "");

                long vram = GpuVram(idx - 1);
                if (vram <= 0)
                {
                    try { vram = (long)Convert.ToUInt64(o["AdapterRAM"]); } catch { }
                }

                if (vram > 0) sb.Append(Fmt.Size(vram) + " VRAM");
                if (res.Length > 0) { if (sb.Length > 0) sb.Append(" · "); sb.Append(res); }
                if (drv.Length > 0) { if (sb.Length > 0) sb.Append(" · "); sb.Append(L.F("Driver {0}", drv)); }
                if (drvDate.Length > 0) { if (sb.Length > 0) sb.Append(" ("); sb.Append(drvDate + ")"); }

                s.Add(name.Length > 0 ? name : L.F("Graphics {0}", idx), sb.ToString());
            }
            return s;
        }

        private static long GpuVram(int index)
        {
            try
            {
                string path = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\" +
                              index.ToString("0000", CultureInfo.InvariantCulture);
                using (RegistryKey k = Registry.LocalMachine.OpenSubKey(path))
                {
                    if (k == null) return 0;
                    object v = k.GetValue("HardwareInformation.qwMemorySize");
                    if (v != null) return Convert.ToInt64(v, CultureInfo.InvariantCulture);
                }
            }
            catch { }
            return 0;
        }

        private static SpecSection DiskSection()
        {
            SpecSection s = new SpecSection("Depolama");
            foreach (ManagementObject o in Query("SELECT * FROM Win32_DiskDrive"))
            {
                string model = P(o, "Model");
                long size = 0;
                try { size = (long)Convert.ToUInt64(o["Size"]); } catch { }
                string iface = P(o, "InterfaceType");
                string media = P(o, "MediaType");

                List<string> parts = new List<string>();
                if (size > 0) parts.Add(Fmt.Size(size));
                if (iface.Length > 0) parts.Add(iface);
                if (media.Length > 0) parts.Add(media);
                s.Add(model.Length > 0 ? model : "Disk", string.Join(" · ", parts.ToArray()));
            }

            foreach (ManagementObject o in Query("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3"))
            {
                string id = P(o, "DeviceID");
                long size = 0, free = 0;
                try { size = (long)Convert.ToUInt64(o["Size"]); } catch { }
                try { free = (long)Convert.ToUInt64(o["FreeSpace"]); } catch { }
                if (size <= 0) continue;
                long used = size - free;
                double pct = size > 0 ? (double)used / size * 100 : 0;
                s.Add(L.F("Drive {0}", id), L.F("{0} / {1}  ({2} used, {3} free) · {4} {5}",
                    Fmt.Size(used), Fmt.Size(size), L.Percent((int)pct), Fmt.Size(free),
                    P(o, "FileSystem"), P(o, "VolumeName")));
            }
            return s;
        }

        private static SpecSection OsSection()
        {
            SpecSection s = new SpecSection(L.T("Operating System"));
            foreach (ManagementObject o in Query("SELECT * FROM Win32_OperatingSystem"))
            {
                s.Add(L.T("Version"), P(o, "Caption"));
                s.Add(L.T("Version number"), P(o, "Version") + "  (" + L.F("Build {0}", P(o, "BuildNumber")) + ")");
                s.Add(L.T("Architecture"), P(o, "OSArchitecture"));
                s.Add(L.T("Install date"), DateOf(P(o, "InstallDate")));
                s.Add(L.T("Last boot"), DateOf(P(o, "LastBootUpTime")));
                break;
            }
            long up = 0;
            try { up = (long)Native.GetTickCount64() / 1000; } catch { }
            if (up > 0)
            {
                TimeSpan ts = TimeSpan.FromSeconds(up);
                s.Add(L.T("Uptime"), L.F("{0} days {1} hours {2} min", ts.Days, ts.Hours, ts.Minutes));
            }
            s.Add(L.T("Computer name"), Environment.MachineName);
            s.Add(L.T("User"), Environment.UserName);
            return s;
        }

        private static SpecSection NetSection()
        {
            SpecSection s = new SpecSection(L.T("Network"));
            foreach (ManagementObject o in Query(
                "SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled=True"))
            {
                string desc = P(o, "Description");
                string mac = P(o, "MACAddress");
                StringBuilder sb = new StringBuilder();
                if (mac.Length > 0) sb.Append(mac);
                string ip = P(o, "IPAddress");
                if (ip.Length > 0) { if (sb.Length > 0) sb.Append(" · "); sb.Append("IP " + ip); }
                string gw = P(o, "DefaultIPGateway");
                if (gw.Length > 0) { if (sb.Length > 0) sb.Append(" · "); sb.Append("GW " + gw); }
                s.Add(desc, sb.ToString());
            }
            return s;
        }

        private static SpecSection MiscSection()
        {
            SpecSection s = new SpecSection(L.T("Other devices"));
            foreach (ManagementObject o in Query("SELECT * FROM Win32_SoundDevice"))
            {
                string n = P(o, "Name");
                if (n.Length > 0) s.Add(L.T("Sound"), n);
            }
            foreach (ManagementObject o in Query("SELECT * FROM Win32_Battery"))
            {
                string n = P(o, "Name");
                string ch = P(o, "EstimatedChargeRemaining");
                if (n.Length > 0) s.Add(L.T("Battery"), n + (ch.Length > 0 ? " · " + ch + "%" : ""));
            }
            foreach (ManagementObject o in Query("SELECT * FROM Win32_DesktopMonitor"))
            {
                string n = P(o, "Name");
                if (n.Length > 0) s.Add(L.T("Monitor"), n);
                break;
            }
            foreach (ManagementObject o in Query("SELECT * FROM Win32_Keyboard"))
            {
                string n = P(o, "Description");
                if (n.Length > 0) s.Add(L.T("Keyboard"), n);
                break;
            }
            return s;
        }

        public static string ToText(List<SpecSection> sections)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("MineManager · " + L.T("Hardware Report"));
            sb.AppendLine(new string('=', 62));
            foreach (SpecSection sec in sections)
            {
                sb.AppendLine();
                sb.AppendLine("[" + sec.Title.ToUpperInvariant() + "]");
                foreach (SpecRow r in sec.Rows)
                    sb.AppendLine("  " + r.Name + ": " + r.Value);
            }
            return sb.ToString();
        }
    }
}
