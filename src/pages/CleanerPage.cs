using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    internal class CleanerPage : UserControl
    {
        private AeroGridView _grid;
        private AeroButton _btnScan, _btnClean;
        private AeroCommandBar _bar;
        private Label _lblTotal, _lblStatus;
        private List<CleanTarget> _targets = new List<CleanTarget>();
        private bool _busy;

        public CleanerPage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;

            // ------------------------------------------------------- command bar
            _bar = new AeroCommandBar();
            _bar.Dock = DockStyle.Top;
            _bar.SetTitle(L.T("Disk Cleanup"), L.T("Temporary files, caches and leftovers"));
            Controls.Add(_bar);

            _btnClean = Ui.Btn(L.T("Clean now"), 78);
            _btnClean.Enabled = false;
            _btnClean.Click += delegate { CleanSelected(); };
            _bar.AddButton(_btnClean);

            _btnScan = Ui.Outline(L.T("Scan"), 66);
            _btnScan.Click += delegate { ScanAll(); };
            _bar.AddButton(_btnScan);

            // ------------------------------------------------------------- body
            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.BackColor = Theme.Bg;
            body.Padding = new Padding(12, 10, 12, 10);
            Controls.Add(body);
            body.BringToFront();

            Panel foot = new Panel();
            foot.Dock = DockStyle.Bottom;
            foot.Height = 26;
            foot.BackColor = Theme.Bg;
            body.Controls.Add(foot);

            _lblTotal = Ui.Lbl(L.F("Selected: {0}", Fmt.Size(0)), Aero.BodySemibold, Theme.Text);
            _lblTotal.Location = new Point(2, 5);
            foot.Controls.Add(_lblTotal);

            _lblStatus = Ui.Lbl(L.T("Ready"), Aero.Small, Theme.Dim);
            _lblStatus.AutoSize = false;
            _lblStatus.TextAlign = ContentAlignment.MiddleRight;
            _lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _lblStatus.SetBounds(300, 4, 300, 18);
            foot.Controls.Add(_lblStatus);
            foot.Resize += delegate { LayoutFoot(); };

            AeroCard card = new AeroCard();
            card.Caption = L.T("Categories");
            card.CaptionHint = L.F("{0} categories", 13);
            card.Dock = DockStyle.Fill;
            body.Controls.Add(card);
            card.BringToFront();

            _grid = new AeroGridView();
            _grid.Dock = DockStyle.Fill;
            _grid.EditMode = DataGridViewEditMode.EditOnEnter;
            _grid.Columns.Add(Ui.Chk("chk", "", 34));
            _grid.Columns.Add(Ui.Col("name", L.T("Category"), 30, DataGridViewContentAlignment.MiddleLeft));
            _grid.Columns.Add(Ui.Col("note", L.T("Description"), 34, DataGridViewContentAlignment.MiddleLeft));
            _grid.Columns.Add(Ui.Col("size", L.T("Size"), 12, DataGridViewContentAlignment.MiddleRight));
            _grid.Columns.Add(Ui.Col("status", L.T("Status"), 24, DataGridViewContentAlignment.MiddleLeft));
            _grid.CellValueChanged += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 0) UpdateTotals();
            };
            _grid.CurrentCellDirtyStateChanged += delegate
            {
                if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            card.Controls.Add(_grid);

            _targets = Cleaner.BuildTargets();
            for (int i = 0; i < _targets.Count; i++)
            {
                CleanTarget t = _targets[i];
                int idx = _grid.Rows.Add(new object[] { t.DefaultChecked, t.Name, t.Note, "-", "" });
                _grid.Rows[idx].Cells[3].Style.ForeColor = Theme.Text;
                _grid.Rows[idx].Cells[4].Style.ForeColor = Theme.Dim;
                _grid.Rows[idx].Cells[2].Style.ForeColor = Theme.Dim;
            }
            Ui.ResetScroll(_grid);
            UpdateTotals();
        }

        public void OnShown()
        {
            Ui.ResetScroll(_grid);
            if (!_busy && AllUnmeasured()) ScanAll();
        }

        private bool AllUnmeasured()
        {
            for (int i = 0; i < _targets.Count; i++) if (_targets[i].Measured) return false;
            return true;
        }

        private bool IsChecked(int row)
        {
            object v = _grid.Rows[row].Cells[0].Value;
            return v is bool && (bool)v;
        }

        private void UpdateRow(int row)
        {
            if (row < 0 || row >= _grid.Rows.Count) return;
            CleanTarget t = _targets[row];
            _grid.Rows[row].Cells[3].Value = t.SizeText;
            _grid.Rows[row].Cells[4].Value = t.Status;
            _grid.Rows[row].Cells[4].Style.ForeColor = t.Errors > 0 ? Theme.Warn : Theme.Dim;
            _grid.Rows[row].Cells[3].Style.ForeColor = t.Size > 0 ? Theme.Text : Theme.Dim;
        }

        private void UpdateTotals()
        {
            long total = 0;
            for (int i = 0; i < _targets.Count && i < _grid.Rows.Count; i++)
                if (IsChecked(i)) total += _targets[i].Size;
            _lblTotal.Text = L.F("Selected: {0}", Fmt.Size(total));
            LayoutFoot();
        }

        private void LayoutFoot()
        {
            if (_lblStatus == null || _lblStatus.Parent == null) return;
            int w = _lblStatus.Parent.ClientSize.Width;
            int lw = TextRenderer.MeasureText(_lblTotal.Text, _lblTotal.Font).Width;
            int sw = TextRenderer.MeasureText(_lblStatus.Text, _lblStatus.Font).Width;
            bool show = lw + sw + 18 <= w;
            _lblStatus.Visible = show;
            if (show) _lblStatus.SetBounds(w - sw - 2, 4, sw, 18);
        }

        private void SetBusy(bool busy, string status)
        {
            _busy = busy;
            _btnScan.Enabled = !busy;
            _btnClean.Enabled = !busy;
            if (status != null) _lblStatus.Text = status;
            LayoutFoot();
            if (!busy)
            {
                Shell.ClearProgress();
                _bar.Invalidate();
            }
        }

        private void ScanAll()
        {
            if (_busy) return;
            SetBusy(true, L.T("Scanning..."));
            Shell.SetProgress(0);
            List<CleanTarget> targets = _targets;

            Task.Run(delegate
            {
                try
                {
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Cleaner.Measure(targets[i]);
                        int idx = i;
                        SafeInvoke(delegate
                        {
                            UpdateRow(idx);
                            Shell.SetProgress((double)(idx + 1) / targets.Count);
                            UpdateTotals();
                        });
                    }
                    SafeInvoke(delegate
                    {
                        SetBusy(false, L.F("Scan complete · {0}", Fmt.Size(TotalChecked())));
                        UpdateTotals();
                    });
                }
                catch (Exception ex)
                {
                    SafeInvoke(delegate { SetBusy(false, L.F("Error: {0}", ex.Message)); });
                }
            });
        }

        private long TotalChecked()
        {
            long total = 0;
            for (int i = 0; i < _targets.Count && i < _grid.Rows.Count; i++)
                if (IsChecked(i)) total += _targets[i].Size;
            return total;
        }

        private void CleanSelected()
        {
            if (_busy) return;
            List<int> selected = new List<int>();
            for (int i = 0; i < _targets.Count; i++)
                if (IsChecked(i) && _targets[i].Size > 0) selected.Add(i);

            if (selected.Count == 0)
            {
                _lblStatus.Text = L.T("Nothing to clean (scan first)");
                return;
            }

            long total = 0;
            for (int i = 0; i < selected.Count; i++) total += _targets[selected[i]].Size;

            bool go = AeroDialog.Confirm(FindForm(), L.T("Confirm cleanup"),
                L.F("About {0} of data will be deleted from {1} selected categories.\n\nContinue?",
                    Fmt.Size(total), selected.Count),
                L.T("Clean now"), L.T("Cancel"), "warn", true);
            if (!go) return;

            SetBusy(true, L.T("Cleaning..."));
            Shell.SetProgress(0);
            long freedTotal = 0;
            List<int> items = selected;

            Task.Run(delegate
            {
                try
                {
                    for (int i = 0; i < items.Count; i++)
                    {
                        int idx = items[i];
                        long freed = Cleaner.Clean(_targets[idx]);
                        int done = i + 1;
                        SafeInvoke(delegate
                        {
                            UpdateRow(idx);
                            Shell.SetProgress((double)done / items.Count);
                            _lblStatus.Text = L.F("Cleaning: {0}", Fmt.Size(freedTotal + freed));
                        });
                        freedTotal += freed;
                    }
                    long totalFreed = freedTotal;
                    SafeInvoke(delegate
                    {
                        UpdateTotals();
                        SetBusy(false, L.F("Done · {0} freed", Fmt.Size(totalFreed)));
                        _lblTotal.Text = L.F("Selected: {0}", Fmt.Size(TotalChecked()));
                        if (totalFreed > 0)
                            Shell.Toast(L.F("Cleanup complete · {0} freed", Fmt.Size(totalFreed)),
                                Aero.Success);
                    });
                }
                catch (Exception ex)
                {
                    SafeInvoke(delegate { SetBusy(false, L.F("Error: {0}", ex.Message)); });
                }
            });
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
    }
}
