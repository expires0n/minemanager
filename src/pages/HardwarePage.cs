using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    internal class HardwarePage : UserControl
    {
        private Panel _scroll;
        private Panel _body;
        private AeroScrollBar _vbar;
        private bool _syncing;
        private bool _laying;
        private AeroButton _btnReload, _btnCopy, _btnSave;
        private AeroCommandBar _cmd;
        private List<AeroCard> _cards = new List<AeroCard>();
        private List<SpecSection> _data = new List<SpecSection>();
        private ToolTip _tip = new ToolTip();
        private bool _busy;
        private bool _loaded;

        public HardwarePage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;

            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Hardware Information"), L.T("Loading..."));
            Controls.Add(_cmd);

            _btnSave = Ui.Outline(L.T("Save TXT"), 90);
            _btnSave.Click += delegate { SaveReport(); };
            _cmd.AddButton(_btnSave);

            _btnCopy = Ui.Outline(L.T("Copy"), 76);
            _btnCopy.Click += delegate { CopyReport(); };
            _cmd.AddButton(_btnCopy);

            _btnReload = Ui.Outline(L.T("Refresh"), 70);
            _btnReload.Click += delegate { LoadSpecs(); };
            _cmd.AddButton(_btnReload);

            _body = new Panel();
            _body.Dock = DockStyle.Fill;
            _body.BackColor = Theme.Bg;
            _body.Padding = new Padding(12, 10, 12, 10);
            Controls.Add(_body);
            _body.BringToFront();

            _scroll = new Panel();
            _scroll.Dock = DockStyle.Fill;
            _scroll.BackColor = Theme.Bg;
            _scroll.AutoScroll = true;
            _scroll.HandleCreated += delegate { DarkMode.Apply(_scroll, Aero.Dark); };
            _body.Controls.Add(_scroll);
            _scroll.Resize += delegate { LayoutCards(); };
            _scroll.Scroll += delegate { SyncBar(); };

            _vbar = new AeroScrollBar();
            _vbar.Visible = false;
            _vbar.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            _vbar.ValueChanged += delegate
            {
                if (_syncing) return;
                _syncing = true;
                try { _scroll.AutoScrollPosition = new Point(0, _vbar.Value); }
                catch { }
                _syncing = false;
            };
            _body.Controls.Add(_vbar);
            _vbar.BringToFront();
            _body.Resize += delegate { LayoutCards(); };

            VisibleChanged += delegate
            {
                if (Visible && !_loaded) LoadSpecs();
            };
        }

        public void OnShown()
        {
            if (!_loaded) LoadSpecs();
        }

        private void SetStatus(string s)
        {
            _cmd.SetHint(s);
        }

        private void LoadSpecs()
        {
            if (_busy) return;
            _busy = true;
            _btnReload.Enabled = false;
            SetStatus(L.T("Collecting hardware information..."));
            _scroll.Controls.Clear();
            _cards.Clear();

            Task.Run(delegate
            {
                List<SpecSection> data = null;
                try
                {
                    data = Hardware.Collect(delegate(string s) { SafeStatus(s); });
                }
                catch (Exception ex)
                {
                    SafeStatus(L.F("Error: {0}", ex.Message));
                }
                List<SpecSection> d = data ?? new List<SpecSection>();
                List<string> diff = HwSnapshot.Compare(d);
                bool hadPrevious = HwSnapshot.HasPrevious;
                HwSnapshot.Save(d);
                SafeInvoke(delegate
                {
                    _data = d;
                    BuildCards(d, diff, hadPrevious);
                    _busy = false;
                    _loaded = true;
                    _btnReload.Enabled = true;
                    SetStatus(L.F("{0} sections read · {1}", d.Count, DateTime.Now.ToString("HH:mm:ss", L.Culture)) +
                        (diff.Count > 0 ? L.F(" · {0} changes since the last reading",
                            L.Count(diff.Count, "change", "changes", "değişiklik")) : ""));
                });
            });
        }

        private void BuildCards(List<SpecSection> data, List<string> diff, bool hadPrevious)
        {
            List<SpecSection> all = new List<SpecSection>();
            if (hadPrevious)
            {
                SpecSection change = new SpecSection(L.T("Changes since the last reading"));
                if (diff.Count == 0)
                    change.Rows.Add(new SpecRow(L.T("No change since the last reading."), " "));
                else
                    for (int i = 0; i < diff.Count; i++) change.Rows.Add(new SpecRow(diff[i], " "));
                all.Add(change);
            }
            for (int i = 0; i < data.Count; i++) all.Add(data[i]);

            _scroll.SuspendLayout();
            _cards.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                SpecSection sec = all[i];
                bool wide = hadPrevious && i == 0;   // the diff card reads as full-width lines
                AeroCard c = new AeroCard();
                c.Caption = sec.Title;
                c.CaptionHint = L.F("{0} items", sec.Rows.Count);
                int y = 30;
                for (int r = 0; r < sec.Rows.Count; r++)
                {
                    if (wide)
                    {
                        Label line = Ui.Lbl(sec.Rows[r].Name, Aero.Body, Theme.Text);
                        line.AutoSize = false;
                        line.AutoEllipsis = true;
                        line.Tag = "w";
                        line.SetBounds(10, y, 600, 20);
                        c.Controls.Add(line);
                        _tip.SetToolTip(line, sec.Rows[r].Name);
                        y += Aero.RowH;
                        continue;
                    }
                    Label n = Ui.Lbl(sec.Rows[r].Name, Aero.Body, Theme.Dim);
                    n.AutoSize = false;
                    n.AutoEllipsis = true;
                    n.SetBounds(10, y, 150, 20);
                    n.Tag = "n";
                    c.Controls.Add(n);

                    Label v = Ui.Lbl(sec.Rows[r].Value, Aero.Body, Theme.Text);
                    v.AutoSize = false;
                    v.AutoEllipsis = true;
                    v.SetBounds(170, y, 300, 20);
                    v.Tag = "v";
                    c.Controls.Add(v);
                    _tip.SetToolTip(v, sec.Rows[r].Value);
                    _tip.SetToolTip(n, sec.Rows[r].Name);

                    y += Aero.RowH;
                }
                c.Height = y + 10;
                c.Tag = sec.Rows.Count;
                _scroll.Controls.Add(c);
                _cards.Add(c);
            }
            if (data.Count == 0)
            {
                Label empty = Ui.Lbl(L.T("Hardware information could not be read · Try Refresh."),
                    Aero.Small, Theme.Dim);
                empty.AutoSize = false;
                empty.TextAlign = ContentAlignment.MiddleCenter;
                empty.SetBounds(0, 10, Math.Max(180, _scroll.ClientSize.Width - 18), 24);
                _scroll.Controls.Add(empty);
            }
            _scroll.ResumeLayout();
            LayoutCards();
        }

        private void LayoutCards()
        {
            int y = 0;
            int w = _scroll.ClientSize.Width - 20;
            if (w < 200) w = 200;
            _scroll.SuspendLayout();
            if (_laying) return;
            _laying = true;
            for (int i = 0; i < _cards.Count; i++)
            {
                AeroCard c = _cards[i];
                bool stack = w < 460;
                int rows = c.Tag is int ? (int)c.Tag : 0;
                int rowH = stack ? 34 : Aero.RowH;
                c.Height = 30 + rows * rowH + 10;
                c.SetBounds(0, y, w, c.Height);

                int row = 0;
                Label name = null;
                foreach (Control ctl in c.Controls)
                {
                    if (ctl.Tag == null) continue;
                    string tag = (string)ctl.Tag;
                    if (tag == "n")
                    {
                        name = (Label)ctl;
                        continue;
                    }
                    if (tag == "w")
                    {
                        ctl.SetBounds(10, 30 + row * rowH, Math.Max(60, w - 20), 20);
                        row++;
                        continue;
                    }
                    if (tag != "v") continue;
                    int ry = 30 + row * rowH;
                    if (stack)
                    {
                        if (name != null)
                            name.SetBounds(10, ry, Math.Max(60, w - 20), 17);
                        ctl.SetBounds(10, ry + 17, Math.Max(60, w - 20), 17);
                    }
                    else
                    {
                        if (name != null)
                            name.SetBounds(10, ry, 164, 20);
                        ctl.SetBounds(194, ry, Math.Max(60, w - 194 - 14), 20);
                    }
                    row++;
                }
                y += c.Height + 8;
            }
            _scroll.ResumeLayout();
            _scroll.AutoScrollMinSize = new Size(0, y);
            Native.ShowScrollBar(_scroll.Handle, Native.SB_VERT, false);

            int view = Math.Max(20, _scroll.ClientSize.Height);
            int max = Math.Max(0, y - view);
            _syncing = true;
            _vbar.LargeChange = view;
            _vbar.Maximum = max;
            if (_vbar.Value > max) _vbar.Value = max;
            _vbar.SetBounds(Math.Max(0, _body.ClientSize.Width - 12 - 14), 10, 14,
                Math.Max(40, _body.ClientSize.Height - 20));
            _vbar.Visible = max > 0;
            _scroll.AutoScrollPosition = new Point(0, _vbar.Value);
            _syncing = false;
            _laying = false;
        }

        private void SyncBar()
        {
            if (_syncing || _vbar == null) return;
            int v = -_scroll.AutoScrollPosition.Y;
            _syncing = true;
            _vbar.Value = Math.Max(0, v);
            _syncing = false;
        }

        private void CopyReport()
        {
            if (_data.Count == 0) return;
            Privacy.Choice choice = Privacy.ShowShareDialog(FindForm(),
                L.T("Share the hardware report"), Hardware.ToText(_data));
            if (!choice.Ok) return;
            try
            {
                Clipboard.SetText(choice.Text);
                SetStatus(L.T("Report copied to clipboard."));
                Shell.Toast(L.T("Hardware report copied to clipboard"), Aero.Success);
            }
            catch (Exception ex) { SetStatus(L.F("Copy failed: {0}", ex.Message)); }
        }

        private void SaveReport()
        {
            if (_data.Count == 0) return;
            Privacy.Choice choice = Privacy.ShowShareDialog(FindForm(),
                L.T("Share the hardware report"), Hardware.ToText(_data));
            if (!choice.Ok) return;
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = L.T("Text file|*.txt");
                dlg.FileName = L.T("hardware-report") + ".txt";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    File.WriteAllText(dlg.FileName, choice.Text, new UTF8Encoding(true));
                    SetStatus(L.F("Saved: {0}", dlg.FileName));
                    Shell.Toast(L.T("Report saved"), Aero.Success);
                }
                catch (Exception ex) { SetStatus(L.F("Save failed: {0}", ex.Message)); }
            }
        }

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
    }
}
