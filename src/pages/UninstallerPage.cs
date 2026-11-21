using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    internal class UninstallerPage : UserControl
    {
        private AeroGridView _grid, _loGrid;
        private AeroSearchBox _search;
        private AeroCheck _chkSystem, _chkQuiet;
        private AeroButton _btnRefresh, _btnRemove, _btnRemoveSel, _btnRemoveScan, _btnForce, _btnStop,
            _btnDeleteLo, _btnSelectAll, _btnSelectNone, _btnScanLo, _btnDetails, _btnExport;
        private Label _lblCount, _lblLoHint;
        private AeroCard _loCard;
        private AeroCommandBar _cmd;
        private Panel _tools;
        private TableLayoutPanel _root;
        private ToolTip _tip;

        private List<InstalledProgram> _all = new List<InstalledProgram>();
        private List<LeftoverItem> _leftovers = new List<LeftoverItem>();
        private UninstallQueue _queue;
        private bool _busy;

        private static readonly string DefaultHint = "Uninstall · scan leftovers · force delete";

        public UninstallerPage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;

            _tip = new ToolTip();

            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Uninstaller"), L.T(DefaultHint));
            Controls.Add(_cmd);

            _btnRemoveScan = Ui.Btn(L.T("Remove + Scan"), 112);
            _btnRemoveScan.Click += delegate { Uninstall(true); };
            _cmd.AddButton(_btnRemoveScan);

            _btnRemove = Ui.Outline(L.T("Remove"), 76);
            _btnRemove.Click += delegate { Uninstall(false); };
            _cmd.AddButton(_btnRemove);

            _btnRemoveSel = Ui.Outline(L.T("Remove selected"), 116);
            _btnRemoveSel.Click += delegate { UninstallSelected(); };
            _cmd.AddButton(_btnRemoveSel);

            _btnStop = Ui.Outline(L.T("Stop"), 66);
            _btnStop.Enabled = false;
            _btnStop.Click += delegate { StopQueue(); };
            _cmd.AddButton(_btnStop);

            _btnDetails = Ui.Outline(L.T("Details"), 74);
            _btnDetails.Click += delegate { ShowDetails(); };
            _cmd.AddButton(_btnDetails);

            _btnExport = Ui.Outline(L.T("Export CSV"), 88);
            _btnExport.Click += delegate { ExportPrograms(); };
            _cmd.AddButton(_btnExport);

            _btnScanLo = Ui.Outline(L.T("Scan leftovers"), 100);
            _btnScanLo.Click += delegate { ScanLeftoversOnly(); };
            _cmd.AddButton(_btnScanLo);

            _btnForce = Ui.Outline(L.T("Force delete"), 88);
            _btnForce.Click += delegate { ForceRemove(); };
            _cmd.AddButton(_btnForce);

            _btnRefresh = Ui.Outline(L.T("Refresh"), 70);
            _btnRefresh.Click += delegate { RefreshPrograms(); };
            _cmd.AddButton(_btnRefresh);

            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.BackColor = Theme.Bg;
            body.Padding = new Padding(12, 8, 12, 10);
            Controls.Add(body);
            body.BringToFront();

            _root = new TableLayoutPanel();
            _root.Dock = DockStyle.Fill;
            _root.Margin = new Padding(0);
            _root.Padding = new Padding(0);
            _root.BackColor = Theme.Bg;
            _root.ColumnCount = 1;
            _root.RowCount = 3;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            body.Controls.Add(_root);

            // ---------------------------------------------------------- toolbar
            _tools = new Panel();
            _tools.Dock = DockStyle.Fill;
            _tools.Margin = new Padding(0);
            _tools.BackColor = Theme.Bg;
            _root.Controls.Add(_tools, 0, 0);

            _search = new AeroSearchBox(L.T("Search programs..."));
            _search.SetBounds(0, 4, 200, Aero.FieldH);
            _search.Box.TextChanged += delegate { Filter(); };
            _tools.Controls.Add(_search);

            _chkSystem = new AeroCheck(L.T("Show system components"));
            _chkSystem.Checked = false;
            _chkSystem.CheckStateChanged += delegate { RefreshPrograms(); };
            _tools.Controls.Add(_chkSystem);
            _tip.SetToolTip(_chkSystem, L.T("Show system components"));

            _chkQuiet = new AeroCheck(L.T("Quiet"));
            _chkQuiet.Checked = false;
            _chkQuiet.CheckStateChanged += delegate
            {
                if (_chkQuiet.Checked)
                    SetStatus(L.T("Quiet mode asks for confirmation before every silent uninstall."));
                else
                    RestoreHint();
            };
            _tools.Controls.Add(_chkQuiet);
            _tip.SetToolTip(_chkQuiet,
                L.T("Uninstall without dialogs (MSI, Inno Setup and NSIS only). Asks for confirmation."));

            _lblCount = Ui.Lbl("", Aero.Small, Theme.Dim);
            _lblCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _lblCount.AutoSize = false;
            _lblCount.TextAlign = ContentAlignment.MiddleRight;
            _tools.Controls.Add(_lblCount);
            _tools.Resize += delegate { LayoutTools(); };

            // --------------------------------------------------------- programs
            AeroCard card = new AeroCard();
            card.Caption = L.T("Installed Programs");
            card.CaptionHint = L.T("registry");
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 4, 0, 6);
            _root.Controls.Add(card, 0, 1);

            _grid = new AeroGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.EmptyText = L.T("No programs found");
            _grid.EmptyHint = L.T("Clear the search filter.");
            _grid.Columns.Add(Ui.Chk("chk", "", 32));
            _grid.Columns.Add(Ui.Col("name", L.T("Program"), 34, DataGridViewContentAlignment.MiddleLeft));
            _grid.Columns.Add(Ui.Col("pub", L.T("Publisher"), 20, DataGridViewContentAlignment.MiddleLeft));
            _grid.Columns.Add(Ui.Col("ver", L.T("Version"), 10, DataGridViewContentAlignment.MiddleLeft));
            _grid.Columns.Add(Ui.Col("type", L.T("Type"), 14, DataGridViewContentAlignment.MiddleLeft));
            _grid.Columns.Add(Ui.Col("size", L.T("Size"), 9, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Ui.Col("date", L.T("Installed"), 11, DataGridViewContentAlignment.MiddleLeft));
            _grid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
                InstalledProgram p = _grid.Rows[e.RowIndex].Tag as InstalledProgram;
                if (p == null) return;
                object v = _grid.Rows[e.RowIndex].Cells[0].Value;
                p.Checked = v is bool && (bool)v;
                UpdateButtons();
                UpdateCountLabel();
            };
            _grid.CurrentCellDirtyStateChanged += delegate
            {
                if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.SelectionChanged += delegate { UpdateButtons(); };
            _grid.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0) return;
                if (e.ColumnIndex == 0) return;
                Uninstall(true);
            };
            card.Controls.Add(_grid);

            // -------------------------------------------------------- leftovers
            _loCard = new AeroCard();
            _loCard.Caption = L.T("Leftovers");
            _loCard.CaptionHint = L.T("Revo-style scan");
            _loCard.Dock = DockStyle.Fill;
            _loCard.Margin = new Padding(0);
            _root.Controls.Add(_loCard, 0, 2);

            Panel loBar = new Panel();
            loBar.Dock = DockStyle.Bottom;
            loBar.Height = 30;
            loBar.BackColor = Theme.Surface;
            _loCard.Controls.Add(loBar);

            _btnDeleteLo = Ui.Outline(L.T("Delete selected"), 112);
            _btnDeleteLo.Location = new Point(0, 2);
            _btnDeleteLo.Enabled = false;
            _btnDeleteLo.Click += delegate { DeleteLeftovers(); };
            loBar.Controls.Add(_btnDeleteLo);

            _btnSelectAll = Ui.Outline(L.T("Select all"), 90);
            _btnSelectAll.Location = new Point(110, 2);
            _btnSelectAll.Visible = false;
            _btnSelectAll.Click += delegate { SetAllLeftovers(true); };
            loBar.Controls.Add(_btnSelectAll);

            _btnSelectNone = Ui.Outline(L.T("None"), 68);
            _btnSelectNone.Location = new Point(200, 2);
            _btnSelectNone.Visible = false;
            _btnSelectNone.Click += delegate { SetAllLeftovers(false); };
            loBar.Controls.Add(_btnSelectNone);

            _lblLoHint = Ui.Lbl(L.T("Select a program and use \"Remove + Scan\" to find leftovers."),
                Aero.Small, Theme.Dim);
            _lblLoHint.AutoSize = false;
            _lblLoHint.TextAlign = ContentAlignment.MiddleRight;
            _lblLoHint.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            loBar.Controls.Add(_lblLoHint);
            loBar.Resize += delegate
            {
                bool show = loBar.Width >= 560;
                _lblLoHint.Visible = show;
                if (show) _lblLoHint.SetBounds(266, 4, Math.Max(80, loBar.Width - 270), 20);
            };

            _loGrid = new AeroGridView();
            _loGrid.Dock = DockStyle.Fill;
            _loGrid.EditMode = DataGridViewEditMode.EditOnEnter;
            _loGrid.EmptyText = L.T("No leftovers");
            _loGrid.EmptyHint = L.T("Leftovers are listed here.");
            _loGrid.Columns.Add(Ui.Chk("chk", "", 34));
            _loGrid.Columns.Add(Ui.Col("type", L.T("Type"), 12, DataGridViewContentAlignment.MiddleLeft));
            _loGrid.Columns.Add(Ui.Col("where", L.T("Location"), 62, DataGridViewContentAlignment.MiddleLeft));
            _loGrid.Columns.Add(Ui.Col("size", L.T("Size"), 10, DataGridViewContentAlignment.MiddleRight));
            _loGrid.Columns.Add(Ui.Col("conf", L.T("Confidence"), 10, DataGridViewContentAlignment.MiddleLeft));
            _loGrid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex != 0) return;
                if (e.RowIndex < _leftovers.Count)
                {
                    object v = _loGrid.Rows[e.RowIndex].Cells[0].Value;
                    _leftovers[e.RowIndex].Checked = v is bool && (bool)v;
                }
                UpdateLoButtons();
            };
            _loGrid.CurrentCellDirtyStateChanged += delegate
            {
                if (_loGrid.IsCurrentCellDirty) _loGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _loCard.Controls.Add(_loGrid);
            _loGrid.BringToFront();

            Load += delegate
            {
                LayoutTools();
                RefreshPrograms();
            };
        }

        private void LayoutTools()
        {
            int w = _tools.Width;
            if (w <= 0) return;

            // Below ~430 px the toolbar stacks: search on one row, the two
            // checkboxes on the next one, so nothing is clipped at 320 px.
            bool stacked = w < 430;
            int rowH = stacked ? 56 : 30;
            try
            {
                if ((int)_root.RowStyles[0].Height != rowH) _root.RowStyles[0].Height = rowH;
            }
            catch { }

            if (stacked)
            {
                _search.SetBounds(0, 3, Math.Max(80, w), Aero.FieldH);
                int leftW = Math.Max(60, (int)(w * 0.62));
                _chkSystem.SetBounds(0, 29, leftW, 22);
                _chkQuiet.SetBounds(leftW + 6, 29, Math.Max(50, w - leftW - 6), 22);
                _lblCount.Visible = false;
                return;
            }

            bool wide = w >= 700;
            int sw = Math.Max(90, Math.Min(wide ? 260 : 200, w - 170));
            _search.SetBounds(0, 4, sw, Aero.FieldH);

            int x = sw + 10;
            int rightReserve = wide ? 130 : 0;
            int avail = Math.Max(120, w - x - rightReserve);
            int quietW = Math.Max(58, Math.Min(90, avail / 4));
            int sysW = Math.Max(70, avail - quietW - 6);

            _chkSystem.SetBounds(x, 3, sysW, 22);
            _chkQuiet.SetBounds(x + sysW + 6, 3, quietW, 22);

            bool showCount = w >= 520;
            _lblCount.Visible = showCount;
            if (showCount) _lblCount.SetBounds(w - 128, 4, 128, 18);
        }

        private void SetLeftoverHeight(int h)
        {
            try { _root.RowStyles[2].Height = h; }
            catch { }
        }

        // ------------------------------------------------------------------ list

        public void RefreshPrograms()
        {
            if (_busy) return;
            _busy = true;
            Cursor = Cursors.WaitCursor;
            SetStatus(L.T("Loading the installed program list..."));
            UpdateButtons();

            bool includeSystem = _chkSystem.Checked;
            Task.Run(delegate
            {
                List<InstalledProgram> found = null;
                try { found = UninstallerCatalog.All(includeSystem); }
                catch { found = new List<InstalledProgram>(); }

                List<InstalledProgram> list = found == null ? new List<InstalledProgram>() : found;
                SafeInvoke(delegate
                {
                    _all = list;
                    _busy = false;
                    Cursor = Cursors.Default;
                    Filter();
                    UpdateButtons();
                    RestoreHint();
                    Shell.ClearProgress();
                });
            });
        }

        private void Filter()
        {
            string q = _search.Box.Text.Trim().ToLowerInvariant();
            _grid.Rows.Clear();
            int shown = 0;
            for (int i = 0; i < _all.Count; i++)
            {
                InstalledProgram p = _all[i];
                if (q.Length > 0)
                {
                    string hay = (p.DisplayName + " " + p.Publisher + " " + p.KindText).ToLowerInvariant();
                    if (hay.IndexOf(q, StringComparison.Ordinal) < 0) continue;
                }

                string typeText = p.IsInvalid ? L.T("Invalid") : p.KindText;
                int r = _grid.Rows.Add(new object[]
                {
                    p.Checked, p.DisplayName, p.Publisher, p.Version, typeText, p.SizeText,
                    UninstallerCatalog.FormatDate(p.InstallDate)
                });
                DataGridViewRow row = _grid.Rows[r];
                row.Tag = p;
                row.Cells[0].Value = p.Checked;

                row.Cells[2].Style.ForeColor = Theme.Dim;
                row.Cells[3].Style.ForeColor = Theme.Dim;
                row.Cells[4].Style.ForeColor = Theme.Dim;
                row.Cells[5].Style.ForeColor = Theme.Dim;
                row.Cells[6].Style.ForeColor = Theme.Dim;
                row.Cells[1].ToolTipText = p.DisplayName + "   " + p.RegPath;

                if (p.IsInvalid)
                {
                    for (int c = 0; c < row.Cells.Count; c++)
                        row.Cells[c].Style.ForeColor = Theme.Dim;
                    row.Cells[4].Style.ForeColor = Theme.Danger;
                    row.Cells[1].ToolTipText = L.T("Invalid entry: the uninstaller no longer exists. Use Remove to clean up the registry key.");
                    row.Cells[4].ToolTipText = row.Cells[1].ToolTipText;
                }
                shown++;
            }
            Ui.ResetScroll(_grid);
            UpdateCountLabel();
            UpdateButtons();
        }

        private void UpdateCountLabel()
        {
            int selected = CheckedPrograms().Count;
            string text = L.Count(_grid.Rows.Count, "program", "programs", "program") +
                          " · " + Fmt.Size(TotalSize());
            if (selected > 0) text = text + L.F(" · {0} selected", selected);
            _lblCount.Text = text;
        }

        private long TotalSize()
        {
            long t = 0;
            for (int i = 0; i < _all.Count; i++) t += _all[i].Size;
            return t;
        }

        private InstalledProgram Selected
        {
            get
            {
                if (_grid.SelectedRows.Count == 0) return null;
                return _grid.SelectedRows[0].Tag as InstalledProgram;
            }
        }

        private List<InstalledProgram> CheckedPrograms()
        {
            List<InstalledProgram> list = new List<InstalledProgram>();
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].Checked) list.Add(_all[i]);
            return list;
        }

        private void UpdateButtons()
        {
            int selected = CheckedPrograms().Count;
            InstalledProgram sel = Selected;
            _btnRemoveSel.Enabled = selected > 0 && !_busy;
            _btnRemove.Enabled = sel != null && !_busy;
            _btnRemoveScan.Enabled = sel != null && !_busy;
            _btnDetails.Enabled = sel != null;
            _btnForce.Enabled = sel != null && !_busy;
            _btnScanLo.Enabled = sel != null && !_busy;
            _btnRefresh.Enabled = !_busy;
            _btnExport.Enabled = _all.Count > 0 && !_busy;
            _btnStop.Enabled = _queue != null && !_queue.Cancelled;
            _btnDeleteLo.Enabled = !_busy && CountCheckedLeftovers() > 0;
        }

        private void StopQueue()
        {
            if (_queue == null) return;
            _queue.Cancel();
            _btnStop.Enabled = false;
            SetStatus(L.T("Stopping after the current program..."));
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            UpdateButtons();
        }

        private void RestoreHint()
        {
            _cmd.SetHint(L.T(DefaultHint));
            if (_leftovers.Count == 0)
                _lblLoHint.Text = L.T("Select a program and use \"Remove + Scan\" to find leftovers.");
        }

        private void SetStatus(string text)
        {
            _lblLoHint.Text = text;
            _cmd.SetHint(text);
        }

        private void WarnSelect()
        {
            SetStatus(L.T("Select a program from the list first."));
            Shell.Toast(L.T("Select a program from the list first."), Aero.Warning);
        }

        // ------------------------------------------------------------- uninstall

        private void Uninstall(bool scanAfter)
        {
            if (_busy) return;
            InstalledProgram p = Selected;
            if (p == null) { WarnSelect(); return; }

            List<InstalledProgram> items = new List<InstalledProgram>();
            items.Add(p);
            BeginUninstall(items, scanAfter);
        }

        private void UninstallSelected()
        {
            if (_busy) return;
            List<InstalledProgram> items = CheckedPrograms();
            if (items.Count == 0) { WarnSelect(); return; }
            BeginUninstall(items, false);
        }

        private void BeginUninstall(List<InstalledProgram> items, bool scanAfter)
        {
            if (items == null || items.Count == 0) return;

            List<InstalledProgram> invalid = new List<InstalledProgram>();
            List<InstalledProgram> valid = new List<InstalledProgram>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                if (items[i].IsInvalid) invalid.Add(items[i]);
                else valid.Add(items[i]);
            }

            if (valid.Count == 0)
            {
                OfferRemoveEntry(invalid);
                return;
            }
            if (invalid.Count > 0)
                Shell.Toast(L.F("{0} invalid entries were skipped.", invalid.Count), Aero.Warning);

            bool wantQuiet = _chkQuiet.Checked;
            List<InstalledProgram> quietItems = new List<InstalledProgram>();
            List<InstalledProgram> interactive = new List<InstalledProgram>();
            for (int i = 0; i < valid.Count; i++)
            {
                if (wantQuiet && valid[i].CanQuiet) quietItems.Add(valid[i]);
                else interactive.Add(valid[i]);
            }

            if (wantQuiet && quietItems.Count == 0)
            {
                AeroDialog.Info(FindForm(), L.T("Quiet uninstall"),
                    L.T("None of the selected programs supports a quiet uninstall. They will run interactively."),
                    "info");
                wantQuiet = false;
            }

            if (wantQuiet)
            {
                if (!ConfirmQuiet(quietItems, interactive)) return;
            }
            else
            {
                string title = valid.Count == 1 ? L.T("Remove program") : L.T("Remove selected");
                string msg;
                if (valid.Count == 1)
                {
                    msg = L.F("\"{0}\" will be uninstalled with its own uninstaller. Continue?",
                        valid[0].DisplayName);
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(L.F("{0} programs will be uninstalled with their own uninstallers. Continue?",
                        valid.Count));
                    sb.Append("\r\n\r\n");
                    for (int i = 0; i < valid.Count && i < 12; i++)
                    {
                        sb.Append("• ");
                        sb.Append(valid[i].DisplayName);
                        sb.Append("\r\n");
                    }
                    if (valid.Count > 12)
                    {
                        sb.Append(L.F("... and {0} more", valid.Count - 12));
                        sb.Append("\r\n");
                    }
                    msg = sb.ToString();
                }
                if (!AeroDialog.Confirm(FindForm(), title, msg, L.T("Remove"), L.T("Cancel"), "warn", true))
                    return;
            }

            if (!ProcRunner.TryBeginJob())
            {
                Shell.Toast(L.T("Another task is running. Wait for it to finish."), Aero.Warning);
                return;
            }

            SetBusy(true);
            Shell.SetProgress(0);
            SetStatus(L.T("Starting the uninstall queue..."));

            _queue = new UninstallQueue(valid, wantQuiet);
            _queue.ItemChanged += delegate(int index, int count, InstalledProgram p, string status)
            {
                Progress(index, count, p, status);
            };
            _queue.Finished += delegate
            {
                ProcRunner.EndJob();
                SafeInvoke(delegate { QueueFinished(scanAfter, valid); });
            };
            _queue.Start();
            UpdateButtons();
        }

        private void Progress(int index, int count, InstalledProgram p, string status)
        {
            string line = L.F("{0}/{1} · {2} · {3}", index + 1, count,
                p == null ? "" : p.DisplayName, status);
            SafeInvoke(delegate
            {
                if (count > 0) Shell.SetProgress((double)(index + 1) / count);
                SetStatus(line);
                _loCard.CaptionHint = line;
                _loCard.Invalidate();
            });
        }

        private void QueueFinished(bool scanAfter, List<InstalledProgram> items)
        {
            bool reboot = _queue != null && _queue.RebootRequired;
            _queue = null;
            SetBusy(false);
            Shell.ClearProgress();
            RefreshPrograms();

            string msg = reboot
                ? L.T("Uninstall finished. A restart is required to complete it.")
                : L.T("Uninstall finished.");
            SetStatus(msg);
            Shell.Toast(msg, reboot ? Aero.Warning : Aero.Success);

            _loCard.CaptionHint = L.T("Revo-style scan");
            _loCard.Invalidate();

            if (scanAfter && items.Count == 1)
            {
                InstalledProgram p = items[0];
                bool again = AeroDialog.Confirm(FindForm(), L.T("Leftover scan"),
                    L.T("If the uninstall finished, we can scan for leftovers. Start the scan?"),
                    L.T("Scan"), L.T("Cancel"), "info", false);
                if (again)
                {
                    SetLeftoverHeight(252);
                    List<InstalledProgram> one = new List<InstalledProgram>();
                    one.Add(p);
                    StartScan(one, true);
                }
            }
        }

        /// <summary>
        /// Lists every program and its exact command line. Cancel is the default
        /// button and the destructive action is never preselected.
        /// </summary>
        private bool ConfirmQuiet(List<InstalledProgram> quietItems, List<InstalledProgram> interactive)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < quietItems.Count; i++)
            {
                InstalledProgram p = quietItems[i];
                sb.Append(p.DisplayName);
                if (p.Version.Length > 0) sb.Append("  ").Append(p.Version);
                sb.Append("\r\n    ");
                sb.Append(p.QuietCommand);
                if (p.QuietIsSynthesized) sb.Append("   ").Append(L.T("(synthesized)"));
                sb.Append("\r\n\r\n");
            }
            if (interactive.Count > 0)
            {
                sb.Append(L.T("These programs have no silent command and will run interactively:"));
                sb.Append("\r\n");
                for (int i = 0; i < interactive.Count; i++)
                {
                    sb.Append("• ");
                    sb.Append(interactive[i].DisplayName);
                    sb.Append("\r\n");
                }
            }

            return ReportDialog.Show(FindForm(), L.T("Quiet uninstall"),
                L.T("These programs will be uninstalled without any dialogs. Review every command line before you continue."),
                sb.ToString(), L.T("Uninstall quietly"), L.T("Cancel"), false);
        }

        private void OfferRemoveEntry(List<InstalledProgram> items)
        {
            if (items == null || items.Count == 0) return;

            string msg;
            if (items.Count == 1)
            {
                msg = L.F("\"{0}\" points to an uninstaller that no longer exists. The registry key will be backed up and then deleted. Continue?",
                    items[0].DisplayName);
            }
            else
            {
                msg = L.F("{0} invalid entries will have their registry keys backed up and then deleted. Continue?",
                    items.Count);
            }

            if (!AeroDialog.Confirm(FindForm(), L.T("Remove entry"), msg,
                    L.T("Remove entry"), L.T("Cancel"), "warn", true))
                return;

            SetBusy(true);
            SetStatus(L.T("Removing the registry entries..."));
            List<InstalledProgram> list = new List<InstalledProgram>(items);
            Task.Run(delegate
            {
                int removed = 0;
                int failed = 0;
                string lastBackup = "";
                for (int i = 0; i < list.Count; i++)
                {
                    string backup;
                    string error;
                    if (UninstallerCatalog.RemoveInvalidEntry(list[i], out backup, out error))
                    {
                        removed++;
                        lastBackup = backup;
                    }
                    else failed++;
                }

                int r = removed;
                int f = failed;
                string b = lastBackup;
                SafeInvoke(delegate
                {
                    SetBusy(false);
                    RefreshPrograms();
                    string text = L.F("{0} entries removed.", r);
                    if (b.Length > 0) text = text + L.F(" · backup: {0}", Path.GetFileName(b));
                    if (f > 0) text = text + L.F(" · {0} failed", f);
                    SetStatus(text);
                    Shell.Toast(text, f > 0 ? Aero.Warning : Aero.Success);
                });
            });
        }

        private void ShowDetails()
        {
            InstalledProgram p = Selected;
            if (p == null) { WarnSelect(); return; }
            ReportDialog.Show(FindForm(), L.T("Program details"), p.DisplayName,
                BuildDetails(p), L.T("OK"), null, true);
        }

        private static string YesNo(bool value)
        {
            return value ? L.T("Yes") : L.T("No");
        }

        private static void DetailLine(StringBuilder sb, string label, string value)
        {
            sb.Append(label);
            for (int i = label.Length; i < 20; i++) sb.Append(' ');
            sb.Append(": ");
            sb.Append(string.IsNullOrEmpty(value) ? "-" : value);
            sb.Append("\r\n");
        }

        private string BuildDetails(InstalledProgram p)
        {
            StringBuilder sb = new StringBuilder();
            DetailLine(sb, L.T("Name"), p.DisplayName);
            DetailLine(sb, L.T("Publisher"), p.Publisher);
            DetailLine(sb, L.T("Version"), p.Version);
            DetailLine(sb, L.T("Install date"), UninstallerCatalog.FormatDate(p.InstallDate));
            DetailLine(sb, L.T("Size"), p.SizeText);
            DetailLine(sb, L.T("Type"), p.KindText);
            DetailLine(sb, L.T("64-bit"), YesNo(p.Is64Bit));
            DetailLine(sb, L.T("System component"), YesNo(p.SystemComponent));
            DetailLine(sb, L.T("Windows Installer"), YesNo(p.WindowsInstaller));
            DetailLine(sb, L.T("No remove"), YesNo(p.NoRemove));
            DetailLine(sb, L.T("Invalid entry"), YesNo(p.IsInvalid));
            DetailLine(sb, L.T("Comment"), p.Comment);
            DetailLine(sb, L.T("Install location"), p.InstallLocation);
            DetailLine(sb, L.T("Install source"), p.InstallSource);
            DetailLine(sb, L.T("Display icon"), p.DisplayIcon);
            DetailLine(sb, L.T("Modify command"), p.ModifyPath);
            DetailLine(sb, L.T("About URL"), p.AboutUrl);
            DetailLine(sb, L.T("Release type"), p.ReleaseType);
            DetailLine(sb, L.T("Product code"), p.ProductCode);
            DetailLine(sb, L.T("Registry key"), p.RegPath);
            DetailLine(sb, L.T("Uninstall command"), p.UninstallString);
            string quiet = p.QuietCommand;
            if (quiet.Length == 0) quiet = L.T("(none)");
            else if (p.QuietIsSynthesized) quiet = quiet + "   " + L.T("(synthesized)");
            DetailLine(sb, L.T("Quiet command"), quiet);
            return sb.ToString();
        }

        private void ExportPrograms()
        {
            if (_all.Count == 0) return;
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = L.T("CSV file|*.csv");
                dlg.FileName = L.T("installed-programs") + "-" + Environment.MachineName + ".csv";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    UninstallerCatalog.ExportCsv(_all, dlg.FileName);
                    SetStatus(L.F("Saved: {0}", dlg.FileName));
                    Shell.Toast(L.T("Program list exported"), Aero.Success);
                }
                catch (Exception ex)
                {
                    AeroDialog.Info(FindForm(), L.T("Export failed"), ex.Message, "warn");
                }
            }
        }

        // ------------------------------------------------------------------ scan

        private void ScanLeftoversOnly()
        {
            if (_busy) return;
            InstalledProgram p = Selected;
            if (p == null) { WarnSelect(); return; }
            SetLeftoverHeight(252);
            List<InstalledProgram> one = new List<InstalledProgram>();
            one.Add(p);
            StartScan(one, true);
        }

        private void ForceRemove()
        {
            if (_busy) return;
            InstalledProgram p = Selected;
            if (p == null) { WarnSelect(); return; }

            string msg = L.F("\"{0}\" will be FORCE deleted. • Program processes are closed • The install folder is deleted • Registry leftovers are deleted This cannot be undone.", p.DisplayName);
            if (!AeroDialog.Confirm(FindForm(), L.T("Force delete"), msg, L.T("Force delete"), L.T("Cancel"), "danger", true))
                return;

            SetBusy(true);
            SetStatus(L.T("Force delete is running..."));

            Task.Run(delegate
            {
                int killed = LeftoverScanner.KillProcessesIn(p.InstallLocation);
                SafeStatus(L.F("{0} processes closed · searching for leftovers...", killed));
                List<LeftoverItem> items = LeftoverScanner.Scan(p, delegate(string s) { SafeStatus(s); });
                for (int i = 0; i < items.Count; i++) items[i].Checked = true;
                int errors;
                LeftoverScanner.BackupRegistry(items, null);
                long freed = LeftoverScanner.Delete(items, null, out errors);
                long freedFinal = freed;
                int err = errors;

                SafeInvoke(delegate
                {
                    SetBusy(false);
                    _leftovers = new List<LeftoverItem>();
                    _loGrid.Rows.Clear();
                    UpdateLoButtons();
                    RefreshPrograms();
                    string text = L.F("Force deleted · {0} freed", Fmt.Size(freedFinal)) +
                        (err > 0 ? L.F(" · {0} errors", err) : "");
                    SetStatus(text);
                    Shell.Toast(L.F("Force deleted · {0} freed", Fmt.Size(freedFinal)), Aero.Success);
                });
            });
        }

        private void StartScan(List<InstalledProgram> programs, bool clearFirst)
        {
            if (programs == null || programs.Count == 0) return;
            if (clearFirst)
            {
                _leftovers.Clear();
                _loGrid.Rows.Clear();
                UpdateLoButtons();
            }
            SetBusy(true);
            SetStatus(L.T("Scanning..."));

            Task.Run(delegate
            {
                List<LeftoverItem> found = new List<LeftoverItem>();
                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    for (int i = 0; i < programs.Count; i++)
                    {
                        InstalledProgram p = programs[i];
                        SafeStatus(L.F("Scanning: {0}", p.DisplayName));
                        List<LeftoverItem> items = LeftoverScanner.Scan(p,
                            delegate(string s) { SafeStatus(L.F("Scanning: {0}", s)); });
                        for (int j = 0; j < items.Count; j++)
                        {
                            if (seen.Contains(items[j].Where)) continue;
                            seen.Add(items[j].Where);
                            found.Add(items[j]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    SafeStatus(L.F("Scan error: {0}", ex.Message));
                }

                List<LeftoverItem> f = found;
                SafeInvoke(delegate
                {
                    _leftovers = f;
                    _loGrid.Rows.Clear();
                    for (int i = 0; i < f.Count; i++)
                    {
                        LeftoverItem it = f[i];
                        int r = _loGrid.Rows.Add(new object[]
                        {
                            it.Checked, it.TypeText, it.Where, it.SizeText, L.T(it.Confidence)
                        });
                        _loGrid.Rows[r].Cells[0].Value = it.Checked;
                        _loGrid.Rows[r].Cells[2].Style.ForeColor = Theme.Text;
                        _loGrid.Rows[r].Cells[3].Style.ForeColor = Theme.Dim;
                        _loGrid.Rows[r].Cells[4].Style.ForeColor =
                            it.Confidence == "High" ? Theme.Ok : Theme.Dim;
                        _loGrid.Rows[r].Cells[1].Style.ForeColor = Theme.Dim;
                    }
                    Ui.ResetScroll(_loGrid);
                    SetBusy(false);
                    UpdateLoButtons();
                    SetLeftoverHeight(f.Count > 0 ? 252 : 108);

                    string label = programs.Count == 1 ? programs[0].DisplayName : "";
                    string text;
                    if (f.Count == 0)
                        text = label.Length > 0
                            ? L.F("No leftovers found · \"{0}\"", label)
                            : L.T("No leftovers found");
                    else
                        text = label.Length > 0
                            ? L.F("{0} leftovers found · \"{1}\"", L.Count(f.Count, "leftover", "leftovers", "kalıntı"), label)
                            : L.F("{0} leftovers found · {1}", L.Count(f.Count, "leftover", "leftovers", "kalıntı"),
                                L.Count(programs.Count, "program", "programs", "program"));

                    if (f.Count > 0) _loCard.Caption = L.F("Leftovers ({0})", f.Count);
                    else _loCard.Caption = L.T("Leftovers");
                    _loCard.CaptionHint = L.T("Revo-style scan");
                    _loCard.Invalidate();
                    SetStatus(text);
                    Shell.ClearProgress();
                });
            });
        }

        private int CountCheckedLeftovers()
        {
            int n = 0;
            for (int i = 0; i < _leftovers.Count; i++) if (_leftovers[i].Checked) n++;
            return n;
        }

        private void UpdateLoButtons()
        {
            int n = CountCheckedLeftovers();
            _btnDeleteLo.Enabled = n > 0 && !_busy;
            _btnDeleteLo.Text = n > 0 ? L.F("Delete selected ({0})", n) : L.T("Delete selected");
            _btnSelectAll.Visible = _leftovers.Count > 0;
            _btnSelectNone.Visible = _leftovers.Count > 0;
        }

        private void SetAllLeftovers(bool state)
        {
            for (int i = 0; i < _leftovers.Count; i++)
            {
                _leftovers[i].Checked = state;
                if (i < _loGrid.Rows.Count) _loGrid.Rows[i].Cells[0].Value = state;
            }
            UpdateLoButtons();
        }

        private void DeleteLeftovers()
        {
            if (_busy || _leftovers.Count == 0) return;
            int n = CountCheckedLeftovers();
            if (n == 0) return;

            if (!AeroDialog.Confirm(FindForm(), L.T("Delete leftovers"),
                    L.F("{0} leftovers will be permanently deleted. Continue?", n),
                    L.T("Delete"), L.T("Cancel"), "danger", true))
                return;

            SetBusy(true);
            List<LeftoverItem> items = new List<LeftoverItem>(_leftovers);

            Task.Run(delegate
            {
                int errors;
                string backup = LeftoverScanner.BackupRegistry(items, null);
                long freed = LeftoverScanner.Delete(items, null, out errors);
                long freedFinal = freed;
                string backupName = backup != null ? Path.GetFileName(backup) : "";
                SafeInvoke(delegate
                {
                    SetBusy(false);
                    _leftovers.RemoveAll(delegate(LeftoverItem x) { return x.Checked; });
                    _loGrid.Rows.Clear();
                    for (int i = 0; i < _leftovers.Count; i++)
                    {
                        LeftoverItem it = _leftovers[i];
                        int r = _loGrid.Rows.Add(new object[]
                        {
                            it.Checked, it.TypeText, it.Where, it.SizeText, L.T(it.Confidence)
                        });
                        _loGrid.Rows[r].Cells[2].Style.ForeColor = Theme.Text;
                        _loGrid.Rows[r].Cells[3].Style.ForeColor = Theme.Dim;
                        _loGrid.Rows[r].Cells[4].Style.ForeColor =
                            it.Confidence == "High" ? Theme.Ok : Theme.Dim;
                        _loGrid.Rows[r].Cells[1].Style.ForeColor = Theme.Dim;
                    }
                    Ui.ResetScroll(_loGrid);
                    _loCard.Caption = _leftovers.Count > 0 ? L.F("Leftovers ({0})", _leftovers.Count) : L.T("Leftovers");
                    _loCard.Invalidate();
                    if (_leftovers.Count == 0) SetLeftoverHeight(200);
                    SetStatus(L.F("Deleted · {0} freed", Fmt.Size(freedFinal)) +
                        (backupName.Length > 0 ? L.F(" · backup: MineManager-backup\\{0}", backupName) : "") +
                        (errors > 0 ? L.F(" · {0} errors", errors) : ""));
                    UpdateLoButtons();
                    RefreshPrograms();
                    Shell.Toast(L.F("Deleted · {0} freed", Fmt.Size(freedFinal)), Aero.Success);
                });
            });
        }

        // -------------------------------------------------------------- helpers

        private void SafeStatus(string s)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(new Action(delegate { SetStatus(s); }));
            }
            catch { }
        }

        private void SafeInvoke(Action a)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                BeginInvoke(a);
            }
            catch { }
        }

        /// <summary>
        /// Scrollable read-only report dialog (program details and the quiet
        /// uninstall confirmation). Cancel is the default button when present,
        /// so a destructive action is never triggered by Enter.
        /// </summary>
        private class ReportDialog : Form
        {
            private readonly AeroButton _ok;
            private readonly AeroButton _cancel;
            private readonly string _body;

            private ReportDialog(string title, string intro, string body, string okText, string cancelText,
                bool wrap)
            {
                _body = body == null ? "" : body;

                Text = title;
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.Sizable;
                MinimizeBox = false;
                MaximizeBox = true;
                ShowInTaskbar = false;
                BackColor = Theme.Bg;
                ForeColor = Theme.Text;
                Font = Aero.Body;
                ClientSize = new Size(620, 420);
                MinimumSize = new Size(420, 300);
                KeyPreview = true;

                Label head = new Label();
                head.Text = intro == null ? "" : intro;
                head.Font = Aero.Body;
                head.ForeColor = Theme.Text;
                head.BackColor = Theme.Bg;
                head.UseMnemonic = false;
                int introH = TextRenderer.MeasureText(head.Text, Aero.Body,
                    new Size(ClientSize.Width - 28, 240), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
                introH = Math.Max(20, Math.Min(76, introH));
                head.SetBounds(14, 12, ClientSize.Width - 28, introH);
                head.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                Controls.Add(head);

                int top = 12 + introH + 8;
                int buttonH = Aero.ButtonH;

                TextBox box = new TextBox();
                box.Multiline = true;
                box.ReadOnly = true;
                box.ScrollBars = ScrollBars.Both;
                box.WordWrap = wrap;
                box.TabStop = false;
                box.Font = Aero.Mono;
                box.BackColor = Aero.Field;
                box.ForeColor = Theme.Text;
                box.BorderStyle = BorderStyle.FixedSingle;
                box.Text = _body;
                box.SetBounds(14, top, ClientSize.Width - 28, ClientSize.Height - top - buttonH - 24);
                box.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                Controls.Add(box);

                int y = ClientSize.Height - 12 - buttonH;

                AeroButton copy = new AeroButton(L.T("Copy"));
                copy.Size = new Size(74, buttonH);
                copy.SurroundColor = Theme.Bg;
                copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
                copy.SetBounds(14, y, 74, buttonH);
                copy.Click += delegate
                {
                    try { Clipboard.SetText(_body); Shell.Toast(L.T("Copied to clipboard"), Aero.Success); }
                    catch { }
                };
                Controls.Add(copy);

                int right = ClientSize.Width - 14;

                _ok = new AeroButton(okText);
                _ok.Size = new Size(Math.Max(78, TextRenderer.MeasureText(okText, Aero.Body).Width + 26), buttonH);
                _ok.SurroundColor = Theme.Bg;
                _ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                _ok.SetBounds(right - _ok.Width, y, _ok.Width, buttonH);
                _ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
                Controls.Add(_ok);
                right -= _ok.Width + 8;

                if (!string.IsNullOrEmpty(cancelText))
                {
                    _cancel = new AeroButton(cancelText);
                    _cancel.Size = new Size(Math.Max(78, TextRenderer.MeasureText(cancelText, Aero.Body).Width + 26), buttonH);
                    _cancel.SurroundColor = Theme.Bg;
                    _cancel.Default = true;
                    _cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                    _cancel.SetBounds(right - _cancel.Width, y, _cancel.Width, buttonH);
                    _cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
                    Controls.Add(_cancel);
                    _ok.Default = false;
                }
                else
                {
                    _ok.Default = true;
                }
            }

            protected override void OnKeyDown(KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    // Safe default: Enter never confirms a destructive action.
                    if (_cancel != null) _cancel.PerformClick();
                    else if (_ok != null) _ok.PerformClick();
                    e.Handled = true;
                }
                base.OnKeyDown(e);
            }

            protected override void OnShown(EventArgs e)
            {
                base.OnShown(e);
                // Keep focus off the read-only log so nothing looks selected, and
                // leave Enter on the safe (cancel) button.
                if (_cancel != null) _cancel.Focus();
                else if (_ok != null) _ok.Focus();
            }

            public static bool Show(Form owner, string title, string intro, string body,
                string okText, string cancelText, bool wrap)
            {
                using (ReportDialog d = new ReportDialog(title, intro, body, okText, cancelText, wrap))
                {
                    DialogResult r = owner != null ? d.ShowDialog(owner) : d.ShowDialog();
                    return r == DialogResult.OK;
                }
            }
        }
    }
}
