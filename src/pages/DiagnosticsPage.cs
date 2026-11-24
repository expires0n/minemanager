using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Diagnostics toolbox: a read-only overview plus six small tools (file locks,
    /// error codes, battery health, display topology, route lookup, event timeline).
    /// Nothing here changes the system. The layout is one column on narrow windows
    /// and two columns on wide ones so the cards do not stretch into empty space.
    /// </summary>
    internal class DiagnosticsPage : UserControl
    {
        private const int SummaryH = 92;
        private const int CardH = 140;
        private const int CardOpenH = 272;
        private const int Gap = 8;

        private AeroCommandBar _cmd;
        private Panel _scroll;

        private AeroCard _summary;
        private Label _factOs, _factUp, _factDisk, _factSession;
        private ToolTip _tip;

        private AeroConsole _outLock, _outErr, _outBatt, _outDisp, _outRoute, _outTime;
        private TextBox _txtPath, _txtCode, _txtIp, _txtHours;
        private AeroButton _btnBrowse, _btnLock, _btnErr, _btnBatt, _btnMeasure, _btnCompare, _btnRoute, _btnTime;
        private Label _lblDispState;
        private readonly Dictionary<AeroConsole, Label> _placeholder = new Dictionary<AeroConsole, Label>();
        private readonly Dictionary<AeroConsole, AeroCard> _cardOf = new Dictionary<AeroConsole, AeroCard>();

        private List<DisplayEntry> _dispBefore;
        private int _busy;
        private bool _layingOut;

        public DiagnosticsPage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;
            _tip = new ToolTip();
            _tip.InitialDelay = 400;

            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Diagnostics"), L.T("Read-only tools · nothing is changed"));
            Controls.Add(_cmd);

            _scroll = new Panel();
            _scroll.Dock = DockStyle.Fill;
            _scroll.BackColor = Theme.Bg;
            _scroll.AutoScroll = true;
            _scroll.Padding = new Padding(0);
            Controls.Add(_scroll);
            _scroll.BringToFront();

            BuildSummary();
            BuildFileLockCard(0);
            BuildErrorCard(1);
            BuildBatteryCard(2);
            BuildDisplayCard(3);
            BuildRouteCard(4);
            BuildTimelineCard(5);

            _scroll.Resize += delegate { LayoutCards(); };
            RefreshFacts();
            LayoutCards();
        }

        public void OnShown()
        {
            RefreshFacts();
            LayoutCards();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RefreshFacts();
            LayoutCards();
        }

        // --------------------------------------------------------------- layout

        private void LayoutCards()
        {
            if (_scroll == null || _summary == null || _layingOut) return;
            _layingOut = true;
            try
            {
                int w = Math.Max(240, _scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
                int cols = w >= 760 ? 2 : 1;
                int cardW = cols == 2 ? (w - Gap) / 2 : w;

                _summary.SetBounds(0, 0, w, SummaryH);
                LayoutSummary();

                int top = SummaryH + Gap;
                int used = 0;
                int maxSlot = -1;
                for (int i = 0; i < _scroll.Controls.Count; i++)
                {
                    Control c = _scroll.Controls[i];
                    if (c == _summary) continue;
                    int slot;
                    if (!int.TryParse(c.Tag as string, out slot)) continue;
                    if (slot > maxSlot) maxSlot = slot;
                    used++;
                }
                int rows = used == 0 ? 0 : (maxSlot + cols) / cols;
                int[] rowH = new int[rows];
                for (int i = 0; i < _scroll.Controls.Count; i++)
                {
                    Control c = _scroll.Controls[i];
                    if (c == _summary) continue;
                    int slot;
                    if (!int.TryParse(c.Tag as string, out slot)) continue;
                    int row = cols == 2 ? slot / 2 : slot;
                    if (row >= 0 && row < rows && c.Height > rowH[row]) rowH[row] = c.Height;
                }
                int[] rowY = new int[rows];
                int y = top;
                for (int r = 0; r < rows; r++)
                {
                    rowY[r] = y;
                    if (rowH[r] < CardH) rowH[r] = CardH;
                    y += rowH[r] + Gap;
                }
                for (int i = 0; i < _scroll.Controls.Count; i++)
                {
                    Control c = _scroll.Controls[i];
                    if (c == _summary) continue;
                    int slot;
                    if (!int.TryParse(c.Tag as string, out slot)) continue;
                    int col = cols == 2 ? slot % 2 : 0;
                    int row = cols == 2 ? slot / 2 : slot;
                    c.SetBounds(col * (cardW + Gap), rowY[row], cardW, rowH[row]);
                    LayoutCardChildren(c);
                }
                _scroll.AutoScrollMinSize = new Size(0, y + 4);
            }
            finally { _layingOut = false; }
        }

        private void LayoutSummary()
        {
            int inner = _summary.ClientSize.Width - 24;
            if (inner < 120) return;
            int cols = inner >= 720 ? 4 : 2;
            int cell = Math.Max(80, inner / cols);
            Label[] facts = { _factOs, _factUp, _factDisk, _factSession };
            for (int i = 0; i < facts.Length; i++)
            {
                int x = 12 + (i % cols) * cell;
                int y = 36 + (i / cols) * 23;
                facts[i].SetBounds(x, y, Math.Max(60, cell - 10), 18);
            }
        }

        /// <summary>
        /// Children are placed explicitly (not by anchor deltas): the anchor offsets
        /// would be negative for a card that starts at default width and push the
        /// right-side buttons out of the card.
        /// </summary>
        private void LayoutCardChildren(Control card)
        {
            int inner = card.ClientSize.Width - 24;
            if (inner < 120) return;
            for (int i = 0; i < card.Controls.Count; i++)
            {
                Control c = card.Controls[i];
                if (c is AeroConsole)
                    c.SetBounds(12, 72, inner, Math.Max(70, card.ClientSize.Height - 72 - 12));
                else if (c is Panel)
                    c.SetBounds(12, 44, inner, Aero.ButtonH);
                else if (c is Label && "ph".Equals(c.Tag as string))
                    c.SetBounds(16, 78, Math.Max(60, inner - 8), 18);
            }
        }

        // -------------------------------------------------------------- summary

        private void BuildSummary()
        {
            _summary = new AeroCard();
            _summary.Caption = L.T("System summary");
            _summary.CaptionHint = L.T("read-only overview");
            _summary.Tag = "summary";
            _scroll.Controls.Add(_summary);

            _factOs = FactLabel();
            _factUp = FactLabel();
            _factDisk = FactLabel();
            _factSession = FactLabel();
        }

        private Label FactLabel()
        {
            Label l = Ui.Lbl("", Aero.SmallSemibold, Theme.Text);
            l.AutoSize = false;
            l.AutoEllipsis = true;
            l.UseMnemonic = false;
            l.TextAlign = ContentAlignment.MiddleLeft;
            _summary.Controls.Add(l);
            return l;
        }

        private void RefreshFacts()
        {
            if (_factOs == null) return;
            SystemFactsData d = SystemFacts.Read();
            _factOs.Text = d.OsLine;
            _factUp.Text = d.UptimeLine;
            _factDisk.Text = d.DiskLine;
            _factSession.Text = d.SessionLine;
            _tip.SetToolTip(_factOs, d.OsLine + "\r\n" + d.BuildLine);
            _tip.SetToolTip(_factUp, L.T("Time since the last boot."));
            _tip.SetToolTip(_factDisk, L.T("Free space on the Windows drive."));
            _tip.SetToolTip(_factSession, d.Admin ? L.T("Running as administrator.") : L.T("Running as a standard user."));
        }

        // --------------------------------------------------------------- cards

        private AeroCard MakeCard(int slot, string caption, string hint, string placeholder, out AeroConsole console)
        {
            AeroCard card = new AeroCard();
            card.Caption = L.T(caption);
            card.CaptionHint = L.T(hint);
            card.Tag = slot.ToString();
            _scroll.Controls.Add(card);

            console = new AeroConsole();
            console.Visible = false;
            card.Controls.Add(console);

            Label ph = Ui.Lbl(L.T(placeholder), Aero.Small, Theme.Dim);
            ph.AutoSize = false;
            ph.AutoEllipsis = true;
            ph.UseMnemonic = false;
            ph.Tag = "ph";
            card.Controls.Add(ph);
            _placeholder[console] = ph;
            _cardOf[console] = card;
            return card;
        }

        /// <summary>
        /// Input row inside a card. The row keeps its children in place on resize:
        /// buttons stack from the right edge, the input takes what is left.
        /// </summary>
        private Panel MakeRow(AeroCard card)
        {
            Panel row = new Panel();
            row.BackColor = Color.Transparent;
            row.SetBounds(12, 44, 200, Aero.ButtonH);
            row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Resize += delegate { LayoutRow(row); };
            card.Controls.Add(row);
            return row;
        }

        private TextBox RowInput(Panel row, string text, string mode)
        {
            TextBox t = new TextBox();
            t.Font = Aero.Body;
            t.Text = text;
            t.Tag = mode;
            t.BorderStyle = BorderStyle.FixedSingle;
            t.BackColor = Aero.Field;
            t.ForeColor = Aero.Ink;
            t.SetBounds(0, 1, 100, Aero.FieldH);
            row.Controls.Add(t);
            t.KeyDown += delegate(object s, KeyEventArgs e)
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                RunFor(t);
            };
            return t;
        }

        private AeroButton RowButton(Panel row, string en, int w, bool primary, EventHandler onClick)
        {
            AeroButton b = primary ? Ui.Btn(L.T(en), w) : Ui.Outline(L.T(en), w);
            b.Tag = "btn";
            if (onClick != null) b.Click += onClick;
            row.Controls.Add(b);
            return b;
        }

        private void LayoutRow(Panel row)
        {
            int w = row.ClientSize.Width;
            if (_btnBrowse != null && row.Controls.Contains(_btnBrowse))
                _btnBrowse.Visible = w >= 240;

            int x = w;
            for (int i = row.Controls.Count - 1; i >= 0; i--)
            {
                Control c = row.Controls[i];
                if (!"btn".Equals(c.Tag as string) || !c.Visible) continue;
                x -= c.Width + 6;
                c.Location = new Point(Math.Max(0, x), 0);
            }

            Control input = null;
            string mode = "";
            for (int i = 0; i < row.Controls.Count; i++)
            {
                string tag = row.Controls[i].Tag as string;
                if (tag != null && tag.StartsWith("input", StringComparison.Ordinal))
                {
                    input = row.Controls[i];
                    mode = tag;
                    break;
                }
            }
            if (input == null) return;

            int avail = Math.Max(60, x - 6);
            int width = avail;
            if (mode == "inputmid") width = Math.Min(320, avail);
            else if (mode == "inputsmall") width = Math.Min(120, avail);
            input.SetBounds(0, 1, width, Aero.FieldH);
        }

        private Label MakeLabel(AeroCard card, int x, int y, string en, string tag)
        {
            Label l = Ui.Lbl(L.T(en), Aero.Small, Theme.Dim);
            l.SetBounds(x, y, 320, 16);
            l.AutoSize = false;
            l.AutoEllipsis = true;
            l.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            if (tag != null) l.Tag = tag;
            card.Controls.Add(l);
            return l;
        }

        // ------------------------------------------------------------ file lock

        private void BuildFileLockCard(int slot)
        {
            AeroConsole outp;
            AeroCard card = MakeCard(slot, "What is using this file?",
                "Restart Manager", "Choose a file or folder, then run the check.", out outp);
            _outLock = outp;

            MakeLabel(card, 12, 28, "File or folder path", null);
            Panel row = MakeRow(card);
            _txtPath = RowInput(row, "", "input");
            _btnBrowse = RowButton(row, "Browse", 82, false, null);
            _btnBrowse.Click += delegate { ShowBrowseMenu(_btnBrowse); };
            _btnLock = RowButton(row, "Check", 82, true, delegate { RunFileLock(); });
        }

        private void ShowBrowseMenu(Control anchor)
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.ShowImageMargin = false;
            m.Items.Add(L.T("File…"), null, delegate { BrowseFile(); });
            m.Items.Add(L.T("Folder…"), null, delegate { BrowseFolder(); });
            m.Closed += delegate { m.Dispose(); };
            m.Show(anchor, new Point(0, anchor.Height));
        }

        private void BrowseFile()
        {
            using (OpenFileDialog d = new OpenFileDialog())
            {
                d.Title = L.T("What is using this file?");
                d.CheckFileExists = false;
                d.FileName = "";
                if (d.ShowDialog(this) == DialogResult.OK)
                    _txtPath.Text = d.FileName;
            }
        }

        private void BrowseFolder()
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = L.T("Choose the folder to check.");
                d.ShowNewFolderButton = false;
                if (d.ShowDialog(this) == DialogResult.OK)
                    _txtPath.Text = d.SelectedPath;
            }
        }

        private void RunFor(TextBox input)
        {
            if (input == _txtPath) RunFileLock();
            else if (input == _txtCode) RunError();
            else if (input == _txtIp) RunRoute();
            else if (input == _txtHours) RunTimeline();
        }

        private void RunFileLock()
        {
            string path = _txtPath.Text.Trim();
            if (path.Length == 0)
            {
                ClearOut(_outLock);
                Out(_outLock, L.T("Enter a file path first."));
                return;
            }
            string full;
            try { full = Path.GetFullPath(path); }
            catch { full = path; }
            if (!Paths.Exists(full))
            {
                ClearOut(_outLock);
                Out(_outLock, L.F("Not found: {0}", path));
                return;
            }
            if (!BeginJob()) return;

            _btnLock.Enabled = false;
            ClearOut(_outLock);
            Out(_outLock, L.T("Checking..."));

            string target = full;
            Task.Run(delegate
            {
                string report = "";
                string error = "";
                try { report = FileLock.Report(target); }
                catch (Exception ex) { error = ex.Message; }
                EndJob();
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        _btnLock.Enabled = true;
                        _outLock.Clear();
                        if (error.Length > 0) Out(_outLock, L.F("Error: {0}", error));
                        else Out(_outLock, report);
                        SafeStatus(L.T("Diagnostics"));
                    }));
                }
                catch { }
            });
        }

        // ---------------------------------------------------------------- error

        private void BuildErrorCard(int slot)
        {
            AeroConsole outp;
            AeroCard card = MakeCard(slot, "Error code decoder",
                "Win32 · HRESULT · MSI", "Enter a code to see what it means.", out outp);
            _outErr = outp;

            MakeLabel(card, 12, 28, "Code (decimal or 0x…)", null);
            Panel row = MakeRow(card);
            _txtCode = RowInput(row, "0x80070005", "inputmid");
            _btnErr = RowButton(row, "Explain", 90, true, delegate { RunError(); });
        }

        private void RunError()
        {
            long code;
            if (!WinErr.TryParse(_txtCode.Text, out code))
            {
                Out(_outErr, L.T("That is not a number. Examples: 5, 0x80070005, -2147024891"));
                return;
            }
            ClearOut(_outErr);
            Out(_outErr, WinErr.Explain(code));
        }

        // -------------------------------------------------------------- battery

        private void BuildBatteryCard(int slot)
        {
            AeroConsole outp;
            AeroCard card = MakeCard(slot, "Battery & sleep",
                "powercfg report", "Read the battery report — no changes.", out outp);
            _outBatt = outp;

            MakeLabel(card, 12, 28, "Reported capacity vs. design capacity", null);
            Panel row = MakeRow(card);
            _btnBatt = RowButton(row, "Read", 90, true, delegate { RunBattery(); });
        }

        private void RunBattery()
        {
            if (!BeginJob()) return;
            _btnBatt.Enabled = false;
            ClearOut(_outBatt);
            Out(_outBatt, L.T("Reading..."));
            SafeStatus(L.T("Reading the battery report..."));
            Task.Run(delegate
            {
                BatteryInfo b = null;
                string error = "";
                try { b = PowerTools.ReadBattery(delegate(string s) { SafeStatus(s); }); }
                catch (Exception ex) { error = ex.Message; }
                finally { EndJob(); }
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        _btnBatt.Enabled = true;
                        _outBatt.Clear();
                        SafeStatus(L.T("Diagnostics"));
                        if (error.Length > 0) { Out(_outBatt, L.F("Error: {0}", error)); return; }
                        if (b == null) { Out(_outBatt, L.T("No battery report was produced.")); return; }
                        if (b.Error.Length > 0) { Out(_outBatt, L.F("Error: {0}", b.Error)); return; }
                        if (!b.Present) { Out(_outBatt, L.T("No battery information on this machine.")); return; }
                        Out(_outBatt, L.F("Battery: {0}", b.Name.Length > 0 ? b.Name : L.T("no data")));
                        Out(_outBatt, L.F("Design capacity: {0}", b.DesignCapacity));
                        Out(_outBatt, L.F("Reported full charge: {0}", b.FullChargeCapacity));
                        if (b.HealthPercent >= 0)
                            Out(_outBatt, L.F("Reported / design: {0}", L.Percent((int)Math.Round(b.HealthPercent))));
                        else
                            Out(_outBatt, L.T("Capacity ratio: no data"));
                        Out(_outBatt, L.F("Cycle count: {0}", b.CycleCount));
                        Out(_outBatt, L.T("A missing value is not zero — it means the data is not reported."));
                    }));
                }
                catch { }
            });
        }

        // -------------------------------------------------------------- display

        private void BuildDisplayCard(int slot)
        {
            AeroConsole outp;
            AeroCard card = MakeCard(slot, "Display connections",
                "measure · compare", "Measure now; measure again after a change to compare.", out outp);
            _outDisp = outp;

            _lblDispState = MakeLabel(card, 12, 28, "No measurement yet", "state");
            Panel row = MakeRow(card);
            _btnMeasure = RowButton(row, "Measure", 100, true, delegate { MeasureDisplays(); });
            _btnCompare = RowButton(row, "Compare", 100, false, delegate { CompareDisplays(); });
            _btnCompare.Enabled = false;
        }

        private void MeasureDisplays()
        {
            string note;
            List<DisplayEntry> now = DisplayTools.Capture(out note);
            ClearOut(_outDisp);
            Out(_outDisp, L.F("Measurement: {0} active path(s)", now.Count));
            for (int i = 0; i < now.Count; i++) Out(_outDisp, "  " + now[i].Line);
            if (note.Length > 0) Out(_outDisp, note);

            if (_dispBefore == null)
            {
                _dispBefore = now;
                _lblDispState.Text = L.T("Baseline stored — measure again after the change");
                _lblDispState.ForeColor = Theme.Accent;
                _btnCompare.Enabled = false;
            }
            else
            {
                Out(_outDisp, DisplayTools.Compare(_dispBefore, now));
                _dispBefore = now;
                _lblDispState.Text = L.T("Compared — the new measurement is the baseline now");
                _lblDispState.ForeColor = Theme.Dim;
                _btnCompare.Enabled = true;
            }
        }

        private void CompareDisplays()
        {
            if (_dispBefore == null) return;
            string note;
            List<DisplayEntry> now = DisplayTools.Capture(out note);
            ClearOut(_outDisp);
            Out(_outDisp, DisplayTools.Compare(_dispBefore, now));
            if (note.Length > 0) Out(_outDisp, note);
            _dispBefore = now;
            _lblDispState.Text = L.T("Compared — the new measurement is the baseline now");
            _lblDispState.ForeColor = Theme.Dim;
        }

        // ---------------------------------------------------------------- route

        private void BuildRouteCard(int slot)
        {
            AeroConsole outp;
            AeroCard card = MakeCard(slot, "Which path does Windows use?",
                "route lookup", "Enter an IP address to see the local route.", out outp);
            _outRoute = outp;

            MakeLabel(card, 12, 28, "Destination IP (numeric)", null);
            Panel row = MakeRow(card);
            _txtIp = RowInput(row, "8.8.8.8", "inputmid");
            _btnRoute = RowButton(row, "Run", 90, true, delegate { RunRoute(); });
        }

        private void RunRoute()
        {
            RouteInfo r = RouteTools.Query(_txtIp.Text);
            ClearOut(_outRoute);
            if (r.Error.Length > 0) { Out(_outRoute, L.F("Error: {0}", r.Error)); return; }
            Out(_outRoute, L.T("The route Windows picked for this destination:"));
            Out(_outRoute, "  " + L.T("Destination") + ": " + r.Destination);
            Out(_outRoute, "  " + L.T("Source address") + ": " + (r.SourceIp.Length > 0 ? r.SourceIp : "-"));
            Out(_outRoute, "  " + L.T("Interface") + ": " + r.InterfaceName +
                "  (#" + r.InterfaceIndex.ToString(L.Culture) + ")");
            Out(_outRoute, "  " + L.T("Next hop") + ": " + (r.NextHop.Length > 0 ? r.NextHop : "-"));
            Out(_outRoute, L.T("This is a local routing decision only — it does not test the destination."));
        }

        // ------------------------------------------------------------- timeline

        private void BuildTimelineCard(int slot)
        {
            AeroConsole outp;
            AeroCard card = MakeCard(slot, "Maintenance timeline",
                "Windows event records", "Read recent maintenance events.", out outp);
            _outTime = outp;

            MakeLabel(card, 12, 28, "Look back (hours)", null);
            Panel row = MakeRow(card);
            _txtHours = RowInput(row, "24", "inputsmall");
            _btnTime = RowButton(row, "Read", 90, true, delegate { RunTimeline(); });
        }

        private void RunTimeline()
        {
            int hours;
            if (!int.TryParse(_txtHours.Text.Trim(), out hours) || hours <= 0)
            {
                Out(_outTime, L.T("Enter a positive number of hours."));
                return;
            }
            if (hours > 720)
            {
                Out(_outTime, L.T("Look back at most 720 hours (30 days)."));
                return;
            }
            if (!BeginJob()) return;
            _btnTime.Enabled = false;
            ClearOut(_outTime);
            Out(_outTime, L.T("Reading..."));
            SafeStatus(L.T("Reading event records..."));
            Task.Run(delegate
            {
                List<TimelineEntry> list = null;
                string note = "";
                string error = "";
                try { list = EventTimeline.Recent(hours, out note); }
                catch (Exception ex) { error = ex.Message; }
                finally { EndJob(); }
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        _btnTime.Enabled = true;
                        _outTime.Clear();
                        SafeStatus(L.T("Diagnostics"));
                        if (error.Length > 0) { Out(_outTime, L.F("Error: {0}", error)); return; }
                        if (list == null || list.Count == 0)
                        {
                            Out(_outTime, L.T("No matching event in this period."));
                            if (note.Length > 0) Out(_outTime, L.F("Error: {0}", note));
                            return;
                        }
                        for (int i = 0; i < list.Count; i++)
                        {
                            TimelineEntry e = list[i];
                            string line = e.TimeText + "  " + e.Channel + "  " + e.Provider + "/" +
                                e.Id.ToString(L.Culture);
                            Out(_outTime, line);
                            if (e.Summary.Length > 0) Out(_outTime, "      " + e.Summary);
                        }
                        Out(_outTime, EventTimeline.Warning);
                        if (note.Length > 0) Out(_outTime, L.F("Error: {0}", note));
                    }));
                }
                catch { }
            });
        }

        // -------------------------------------------------------------- helpers

        private bool BeginJob()
        {
            if (_busy > 0)
            {
                Shell.Toast(L.T("Another task is running. Wait for it to finish."), Aero.Warning);
                return false;
            }
            _busy = 1;
            return true;
        }

        private void EndJob()
        {
            _busy = 0;
        }

        private void Out(AeroConsole c, string text)
        {
            if (c == null || text == null) return;
            Label ph;
            if (_placeholder.TryGetValue(c, out ph)) ph.Visible = false;
            c.Visible = true;
            AeroCard card;
            if (_cardOf.TryGetValue(c, out card) && card.Height != CardOpenH)
            {
                card.Height = CardOpenH;
                LayoutCards();
            }
            c.Append(text);
        }

        private void ClearOut(AeroConsole c)
        {
            if (c == null) return;
            c.Clear();
            c.Visible = false;
            Label ph;
            if (_placeholder.TryGetValue(c, out ph)) ph.Visible = true;
            AeroCard card;
            if (_cardOf.TryGetValue(c, out card) && card.Height != CardH)
            {
                card.Height = CardH;
                LayoutCards();
            }
        }

        private void SafeStatus(string s)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(delegate { Shell.Status.LeftText = s; }));
            }
            catch { }
        }
    }
}
