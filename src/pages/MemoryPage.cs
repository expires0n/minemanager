using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    internal class MemoryPage : UserControl
    {
        private Label _lblPct, _lblPctSub;
        private AeroProgress _bar;
        private Label _lblTotal, _lblAvail, _lblCommited, _lblInstalled, _lblImpact;
        private AeroGridView _grid;
        private AeroConsole _log;
        private AeroButton _btnQuick, _btnDeep;
        private AeroCommandBar _cmd;
        private TableLayoutPanel _cols;
        private Panel _left, _right;
        private Timer _timer;
        private int _tick;
        private bool _busy;

        // impact card: before → right after → 30 s after the clean
        private MemResult _impactBefore, _impactAfter, _impactLate;
        private int _lateCountdown = -1;

        public MemoryPage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;

            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Memory"), L.T("Physical memory · page file · processes"));
            Controls.Add(_cmd);

            _btnQuick = Ui.Btn(L.T("Quick Free"), 100);
            _btnQuick.Click += delegate { DoClean(false); };
            _cmd.AddButton(_btnQuick);

            _btnDeep = Ui.Outline(L.T("Deep Clean"), 108);
            _btnDeep.Click += delegate { DoClean(true); };
            _cmd.AddButton(_btnDeep);

            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.BackColor = Theme.Bg;
            body.Padding = new Padding(12, 10, 12, 10);
            Controls.Add(body);
            body.BringToFront();

            _cols = new TableLayoutPanel();
            _cols.Dock = DockStyle.Fill;
            _cols.Margin = new Padding(0);
            _cols.Padding = new Padding(0);
            _cols.BackColor = Theme.Bg;
            _cols.ColumnCount = 2;
            _cols.RowCount = 2;
            body.Controls.Add(_cols);

            _left = new Panel();
            _left.Dock = DockStyle.Fill;
            _left.Margin = new Padding(0);
            _left.BackColor = Theme.Bg;
            _cols.Controls.Add(_left, 0, 0);

            _right = new Panel();
            _right.Dock = DockStyle.Fill;
            _right.Margin = new Padding(12, 0, 0, 0);
            _right.BackColor = Theme.Bg;
            _cols.Controls.Add(_right, 1, 0);

            // ---------------------------------------------------------- gauge card
            AeroCard mem = new AeroCard();
            mem.Caption = L.T("Physical Memory");
            mem.CaptionHint = L.T("live");
            mem.Dock = DockStyle.Top;
            mem.Height = 252;
            _left.Controls.Add(mem);

            _lblPct = Ui.Lbl("0 %", Theme.Big, Theme.Text);
            _lblPct.Location = new Point(14, 28);
            mem.Controls.Add(_lblPct);

            _lblPctSub = Ui.Lbl(L.T("in use"), Aero.Small, Theme.Dim);
            _lblPctSub.Location = new Point(16, 78);
            mem.Controls.Add(_lblPctSub);

            _bar = new AeroProgress();
            _bar.Location = new Point(14, 100);
            _bar.Size = new Size(336, 14);
            _bar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            mem.Controls.Add(_bar);

            _lblTotal = Ui.Lbl("-", Aero.Small, Theme.Dim);
            _lblTotal.Location = new Point(14, 122);
            _lblTotal.AutoSize = false;
            _lblTotal.AutoEllipsis = true;
            _lblTotal.Width = 320;
            mem.Controls.Add(_lblTotal);

            _lblAvail = Ui.Lbl("-", Aero.Small, Theme.Dim);
            _lblAvail.Location = new Point(14, 144);
            _lblAvail.AutoSize = false;
            _lblAvail.AutoEllipsis = true;
            _lblAvail.Width = 320;
            mem.Controls.Add(_lblAvail);

            _lblCommited = Ui.Lbl("-", Aero.Small, Theme.Dim);
            _lblCommited.Location = new Point(14, 166);
            _lblCommited.AutoSize = false;
            _lblCommited.AutoEllipsis = true;
            _lblCommited.Width = 320;
            mem.Controls.Add(_lblCommited);

            _lblInstalled = Ui.Lbl("-", Aero.Small, Theme.Dim);
            _lblInstalled.Location = new Point(180, 34);
            _lblInstalled.AutoSize = false;
            _lblInstalled.AutoEllipsis = true;
            _lblInstalled.Width = 160;
            _lblInstalled.Height = 34;
            _lblInstalled.TextAlign = ContentAlignment.TopRight;
            mem.Controls.Add(_lblInstalled);

            mem.Resize += delegate
            {
                _bar.Width = Math.Max(40, mem.Width - 28);
                _lblTotal.Width = Math.Max(80, mem.Width - 28);
                _lblAvail.Width = Math.Max(80, mem.Width - 28);
                _lblCommited.Width = Math.Max(80, mem.Width - 28);
                _lblInstalled.Left = Math.Max(120, mem.Width - 190);
                _lblInstalled.Width = Math.Max(60, mem.Width - _lblInstalled.Left - 14);
                if (_lblImpact != null) _lblImpact.Width = Math.Max(80, mem.Width - 28);
            };

            Label note = Ui.Lbl(L.T("Deep clean empties the standby list;\nprograms may be slower on first use."),
                Aero.Small, Theme.Dim);
            note.AutoSize = false;
            note.AutoEllipsis = true;
            note.UseMnemonic = false;
            note.SetBounds(14, 186, 330, 28);
            note.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            mem.Controls.Add(note);

            _lblImpact = Ui.Lbl(L.T("No clean has run yet."), Aero.Small, Theme.Text);
            _lblImpact.AutoSize = false;
            _lblImpact.AutoEllipsis = true;
            _lblImpact.UseMnemonic = false;
            _lblImpact.SetBounds(14, 218, 330, 28);
            _lblImpact.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            mem.Controls.Add(_lblImpact);

            // ---------------------------------------------------------- log card
            AeroCard logCard = new AeroCard();
            logCard.Caption = L.T("Activity Log");
            logCard.CaptionHint = L.T("last 300 lines");
            logCard.Dock = DockStyle.Fill;
            _left.Controls.Add(logCard);
            _left.Controls.SetChildIndex(logCard, 0);

            _log = new AeroConsole();
            _log.Dock = DockStyle.Fill;
            logCard.Controls.Add(_log);

            // ------------------------------------------------------- process card
            AeroCard proc = new AeroCard();
            proc.Caption = L.T("Processes Using Memory");
            proc.CaptionHint = L.T("top 15");
            proc.Dock = DockStyle.Fill;
            _right.Controls.Add(proc);

            _grid = new AeroGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.EmptyText = L.T("No process data");
            _grid.EmptyHint = L.T("The list refreshes in a few seconds.");
            _grid.Columns.Add(Ui.Col("name", L.T("Process"), 60, DataGridViewContentAlignment.MiddleLeft));
            _grid.Columns.Add(Ui.Col("pid", "PID", 15, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Ui.Col("mem", L.T("Memory"), 25, DataGridViewContentAlignment.MiddleRight));
            proc.Controls.Add(_grid);

            Resize += delegate { LayoutColumns(); };
            LayoutColumns();

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += delegate { OnTick(); };
            VisibleChanged += delegate
            {
                if (Visible) { if (_timer != null) _timer.Start(); RefreshNow(); RefreshProcesses(); }
                else if (_timer != null) _timer.Stop();
            };
            RefreshNow();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
            base.Dispose(disposing);
        }

        private void LayoutColumns()
        {
            if (_cols == null || _left == null) return;
            bool stack = Width < 760;
            _cols.SuspendLayout();
            if (stack)
            {
                _cols.ColumnStyles.Clear();
                _cols.RowStyles.Clear();
                _cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _cols.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
                _cols.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));
                _cols.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                _cols.SetCellPosition(_left, new TableLayoutPanelCellPosition(0, 0));
                _cols.SetColumnSpan(_left, 2);
                _cols.SetCellPosition(_right, new TableLayoutPanelCellPosition(0, 1));
                _cols.SetColumnSpan(_right, 2);
                _left.Margin = new Padding(0);
                _right.Margin = new Padding(0, 10, 0, 0);
            }
            else
            {
                _cols.ColumnStyles.Clear();
                _cols.RowStyles.Clear();
                _cols.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Math.Max(280, Math.Min(372, Width - 300))));
                _cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _cols.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                _cols.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

                _cols.SetCellPosition(_left, new TableLayoutPanelCellPosition(0, 0));
                _cols.SetColumnSpan(_left, 1);
                _cols.SetCellPosition(_right, new TableLayoutPanelCellPosition(1, 0));
                _cols.SetColumnSpan(_right, 1);
                _left.Margin = new Padding(0);
                _right.Margin = new Padding(12, 0, 0, 0);
            }
            _cols.ResumeLayout();
        }

        private void OnTick()
        {
            _tick++;
            RefreshNow();
            if (_tick % 4 == 0) RefreshProcesses();

            // the 30-second measurement of the impact card
            if (_lateCountdown > 0)
            {
                _lateCountdown--;
                if (_lateCountdown == 0)
                {
                    _impactLate = MemoryTools.Status();
                    UpdateImpact();
                    Append(L.F("30 s later: available {0}", Fmt.Size(_impactLate.AvailPhys)));
                }
            }
        }

        /// <summary>
        /// System-wide memory around a clean: before → right after → 30 s later.
        /// The change is what the whole system shows, not a number owned by MineManager.
        /// </summary>
        private void UpdateImpact()
        {
            if (_lblImpact == null) return;
            if (_impactBefore == null || _impactAfter == null)
            {
                _lblImpact.Text = L.T("No clean has run yet.");
                return;
            }
            string text = L.F("Available: {0} → {1}",
                Fmt.Size(_impactBefore.AvailPhys), Fmt.Size(_impactAfter.AvailPhys));
            if (_impactLate != null)
                text += L.F("  ·  after 30 s: {0}", Fmt.Size(_impactLate.AvailPhys));
            text += "  (" + L.T("system-wide change") + ")";
            _lblImpact.Text = text;
        }

        public void OnShown()
        {
            if (_timer != null) _timer.Start();
            RefreshNow();
            RefreshProcesses();
        }

        public void RefreshNow()
        {
            MemResult m = MemoryTools.Status();
            _lblPct.Text = L.Percent(m.LoadPercent);
            double r = m.UsedRatio;
            _bar.Value = r;
            _bar.FillColor = r > 0.88 ? Aero.Danger : (r > 0.72 ? Aero.Warning : Aero.Success);
            _lblTotal.Text = L.F("Used: {0} / {1}", Fmt.Size(m.UsedPhys), Fmt.Size(m.TotalPhys));
            _lblAvail.Text = L.F("Available: {0}", Fmt.Size(m.AvailPhys));
            _lblCommited.Text = L.F("Page file: {0} / {1}", Fmt.Size(m.CommitTotal), Fmt.Size(m.TotalPageFile));
            long inst = MemoryTools.InstalledRamBytes();
            _lblInstalled.Text = inst > 0 ? L.T("Installed RAM") + "\n" + Fmt.Size(inst) : "";
        }

        public void RefreshProcesses()
        {
            if (!Visible || _busy) return;
            Task.Run(delegate
            {
                List<ProcMem> list = MemoryTools.TopProcesses(15);
                try
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke(new Action(delegate
                    {
                        _grid.Rows.Clear();
                        for (int i = 0; i < list.Count; i++)
                        {
                            int r = _grid.Rows.Add(new object[]
                            {
                                list[i].Name, list[i].Pid.ToString(), Fmt.Size(list[i].WorkingSet)
                            });
                            _grid.Rows[r].Cells[2].Style.ForeColor = Theme.Text;
                            _grid.Rows[r].Cells[1].Style.ForeColor = Theme.Dim;
                        }
                        Ui.ResetScroll(_grid);
                    }));
                }
                catch { }
            });
        }

        private void Append(string text)
        {
            _log.Append(DateTime.Now.ToString("HH:mm:ss") + "  " + text);
        }

        private void DoClean(bool deep)
        {
            if (_busy) return;
            _busy = true;
            _btnQuick.Enabled = false;
            _btnDeep.Enabled = false;
            MemResult before = MemoryTools.Status();
            _impactBefore = before;
            _impactAfter = null;
            _impactLate = null;
            _lateCountdown = -1;
            UpdateImpact();
            Append(deep ? L.F("Deep clean started · available {0}", Fmt.Size(before.AvailPhys))
                : L.F("Quick free started · available {0}", Fmt.Size(before.AvailPhys)));

            Task.Run(delegate
            {
                try
                {
                    if (deep)
                    {
                        string res = MemoryTools.PurgeSystemMemory();
                        SafeLog(L.F("System memory: {0}", res));
                        System.Threading.Thread.Sleep(200);
                    }
                    long freed;
                    int n = MemoryTools.TrimWorkingSets(out freed);
                    SafeLog(L.F("{0} process working sets were emptied", n));
                    System.Threading.Thread.Sleep(300);
                    MemResult after = MemoryTools.Status();
                    _impactAfter = after;
                    long gain = after.AvailPhys - before.AvailPhys;
                    SafeLog(L.F("Available memory: {0}  ({1})", Fmt.Size(after.AvailPhys),
                        (gain >= 0 ? "+" : "") + Fmt.Size(gain)));
                    if (after.LoadPercent < before.LoadPercent)
                        SafeLog(L.F("Memory usage {0} → {1}", L.Percent(before.LoadPercent), L.Percent(after.LoadPercent)));
                    SafeLog(L.T("The change is system-wide; it is not all freed by MineManager."));
                }
                catch (Exception ex)
                {
                    SafeLog(L.F("Error: {0}", ex.Message));
                }
                finally
                {
                    try
                    {
                        if (!IsDisposed && IsHandleCreated)
                            BeginInvoke(new Action(delegate
                            {
                                _busy = false;
                                _btnQuick.Enabled = true;
                                _btnDeep.Enabled = true;
                                _lateCountdown = 30;
                                UpdateImpact();
                                RefreshNow();
                                RefreshProcesses();
                                Shell.Toast(L.T(deep ? "Deep clean completed" : "Memory freed"),
                                    Aero.Success);
                            }));
                    }
                    catch { }
                }
            });
        }

        private void SafeLog(string s)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(delegate { Append(s); }));
            }
            catch { }
        }
    }
}
