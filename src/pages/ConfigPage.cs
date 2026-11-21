using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Config page (WinUtil "Config" tab) in the app's Win7 Aero style: optional features and
    /// fixes on the left, legacy panels, PowerShell profile and remote access on the right,
    /// activity log at the bottom. Lives entirely on ConfigCatalog/ConfigEngine.
    /// Owner: config work package.
    /// </summary>
    internal class ConfigPage : UserControl
    {
        private const int LogCardH = 132;
        private const int MinLogCardH = 92;       // keeps the console readable on short windows
        private const int StackBreak = 720;       // two columns above, one below
        private const int PanelColBreak = 340;    // legacy panel grid needs room for two labels
        private const int CheckRowH = 22;         // AeroCheck's own metrics
        private const int CardPad = 10;
        private const int PanelButtonGap = 5;

        // ------------------------------------------------------------ row model

        private class FeatureRow
        {
            public ConfigItem Item;
            public AeroCheck Chk;
            public Label State;
        }

        /// <summary>
        /// Vertical scroller with the Aero bar instead of a native scrollbar: the cards live in
        /// a single host panel that is moved by the offset, so the page is deterministic at
        /// 320 px and nothing can clip.
        /// </summary>
        private class ScrollHost : Panel
        {
            private readonly Panel _host;
            private readonly AeroScrollBar _bar;
            private int _contentH;
            private int _offset;
            private bool _sync;

            public ScrollHost()
            {
                BackColor = Theme.Bg;
                DoubleBuffered = true;

                _host = new Panel();
                _host.BackColor = Theme.Bg;
                Controls.Add(_host);

                _bar = new AeroScrollBar();
                _bar.ValueChanged += delegate
                {
                    if (_sync) return;
                    ScrollTo(_bar.Value);
                };
                Controls.Add(_bar);

                Resize += delegate { Place(); };
            }

            public Panel Host { get { return _host; } }

            public int ViewWidth { get { return Math.Max(60, ClientSize.Width - 14); } }

            public int ViewHeight { get { return Math.Max(40, ClientSize.Height); } }

            public void SetContent(int height)
            {
                _contentH = Math.Max(0, height);
                if (_offset > MaxOffset) _offset = MaxOffset;
                Place();
            }

            private int MaxOffset { get { return Math.Max(0, _contentH - ClientSize.Height); } }

            public void ScrollTo(int y)
            {
                int v = y;
                if (v < 0) v = 0;
                if (v > MaxOffset) v = MaxOffset;
                if (v != _offset)
                {
                    _offset = v;
                    _host.Top = -_offset;
                }
                UpdateBar();
            }

            public void Wheel(int delta)
            {
                int steps = delta / 120;
                if (steps == 0 && delta != 0) steps = delta > 0 ? 1 : -1;
                ScrollTo(_offset - steps * (3 * Aero.RowH));
            }

            private void Place()
            {
                bool scroll = MaxOffset > 0;
                int w = scroll ? ViewWidth : ClientSize.Width;
                _host.SetBounds(0, -_offset, Math.Max(40, w),
                    Math.Max(ClientSize.Height, _contentH));
                _bar.Visible = scroll;
                if (scroll)
                {
                    _bar.SetBounds(Math.Max(0, ClientSize.Width - 14), 0, 14,
                        Math.Max(20, ClientSize.Height));
                    _bar.BringToFront();
                }
                UpdateBar();
            }

            private void UpdateBar()
            {
                _sync = true;
                try
                {
                    _bar.LargeChange = Math.Max(20, ClientSize.Height);
                    _bar.Maximum = MaxOffset;
                    _bar.Value = _offset;
                }
                catch { }
                _sync = false;
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                HandledMouseEventArgs h = e as HandledMouseEventArgs;
                if (h != null) h.Handled = true;
                Wheel(e.Delta);
            }
        }

        // --------------------------------------------------------------- state

        private AeroCommandBar _cmd;
        private AeroButton _btnRefresh, _btnInstall, _btnCancel;
        private TableLayoutPanel _grid;
        private ScrollHost _scroll;
        private Panel _host;
        private AeroCard _cardFeat, _cardFix, _cardPanels, _cardProf, _cardRemote, _cardLog;
        private AeroConsole _log;
        private Label _status;
        private ToolTip _tip;

        private readonly List<FeatureRow> _featRows = new List<FeatureRow>();
        private readonly List<AeroButton> _featActionButtons = new List<AeroButton>();
        private readonly List<AeroButton> _fixButtons = new List<AeroButton>();
        private readonly List<AeroButton> _panelButtons = new List<AeroButton>();
        private AeroButton _btnProfInstall, _btnProfRemove, _btnSsh;

        private readonly Queue<string> _pending = new Queue<string>();
        private Timer _flush;
        private CancelFlag _cancel;
        private bool _busy, _loadingStates, _laying, _elevated, _loading;
        private double _spin;
        private bool _realProgress;

        // ============================================================ lifecycle

        public ConfigPage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;
            _elevated = ProcRunner.IsElevated();

            _tip = new ToolTip();
            _tip.InitialDelay = 400;
            _tip.AutoPopDelay = 15000;

            BuildCommandBar();
            BuildBody();
            BuildCards();

            SetBusy(false);
            UpdateHint();
            LayoutAll();

            _flush = new Timer();
            _flush.Interval = 150;
            _flush.Tick += delegate { Tick(); };
            _flush.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_flush != null) { _flush.Stop(); _flush.Dispose(); _flush = null; }
                if (_tip != null) { _tip.Dispose(); _tip = null; }
            }
            base.Dispose(disposing);
        }

        public void OnShown()
        {
            LayoutAll();
            DrainLog();
            LoadStates();
        }

        // ================================================================ build

        private void BuildCommandBar()
        {
            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Config"), L.T("Features · fixes · legacy panels"));
            Controls.Add(_cmd);

            _btnRefresh = Ui.Outline(L.T("Refresh"), 70);
            _btnRefresh.Click += delegate { RefreshClicked(); };
            _cmd.AddButton(_btnRefresh);
        }

        private void BuildBody()
        {
            Panel body = new Panel();
            body.Dock = DockStyle.Fill;
            body.BackColor = Theme.Bg;
            body.Padding = new Padding(12, 8, 12, 10);
            Controls.Add(body);
            body.BringToFront();

            _grid = new TableLayoutPanel();
            _grid.Dock = DockStyle.Fill;
            _grid.Margin = new Padding(0);
            _grid.Padding = new Padding(0);
            _grid.BackColor = Theme.Bg;
            _grid.ColumnCount = 1;
            _grid.RowCount = 2;
            _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, LogCardH));
            body.Controls.Add(_grid);

            _scroll = new ScrollHost();
            _scroll.Dock = DockStyle.Fill;
            _scroll.Margin = new Padding(0, 0, 0, 8);
            _scroll.Resize += delegate { LayoutAll(); };
            _grid.Controls.Add(_scroll, 0, 0);
            _host = _scroll.Host;

            Resize += delegate { LayoutAll(); };
            BuildLogCard();
            _grid.Controls.Add(_cardLog, 0, 1);
        }

        private void BuildLogCard()
        {
            _cardLog = new AeroCard();
            _cardLog.Caption = L.T("Activity Log");
            _cardLog.CaptionHint = L.T("last 300 lines");
            _cardLog.Dock = DockStyle.Fill;
            _cardLog.Margin = new Padding(0);
            _cardLog.Padding = new Padding(CardPad, 26, CardPad, 22);

            _log = new AeroConsole();
            _log.Dock = DockStyle.Fill;
            _cardLog.Controls.Add(_log);

            _btnCancel = Ui.Outline(L.T("Cancel"), 58);
            _btnCancel.Size = new Size(58, 18);
            _btnCancel.Visible = false;
            _btnCancel.Click += delegate { RequestCancel(); };
            _cardLog.Controls.Add(_btnCancel);
            _btnCancel.BringToFront();

            _status = Ui.Lbl(L.T("Ready"), Aero.Small, Theme.Dim);
            _status.AutoSize = false;
            _status.AutoEllipsis = true;
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _cardLog.Controls.Add(_status);

            _cardLog.Resize += delegate { LayoutLogCard(); };
            LayoutLogCard();
        }

        private void BuildCards()
        {
            SuspendLayout();
            _host.SuspendLayout();

            List<ConfigItem> feats = ConfigCatalog.Group("features");
            List<ConfigItem> fixes = ConfigCatalog.Group("fixes");
            List<ConfigItem> panels = ConfigCatalog.Group("panels");

            // --------------------------------------------------------- features
            _cardFeat = NewCard(ConfigCatalog.GroupTitle("features"),
                L.Count(feats.Count, "feature", "features", "özellik"));
            for (int i = 0; i < feats.Count; i++)
            {
                if (feats[i].HasFeatures) AddFeatureRow(feats[i]);
                else AddFeatureAction(feats[i]);
            }
            _btnInstall = new AeroButton(L.T("Install Features"));
            _btnInstall.Default = true;
            _btnInstall.Click += delegate { InstallSelected(); };
            _cardFeat.Controls.Add(_btnInstall);
            _host.Controls.Add(_cardFeat);

            // ------------------------------------------------------------ fixes
            _cardFix = NewCard(ConfigCatalog.GroupTitle("fixes"),
                L.Count(fixes.Count, "action", "actions", "işlem"));
            for (int i = 0; i < fixes.Count; i++)
            {
                ConfigItem it = fixes[i];
                AeroButton b = new AeroButton(L.T(it.Title));
                b.Click += delegate { RunItem(it); };
                _tip.SetToolTip(b, L.T(it.Desc));
                _cardFix.Controls.Add(b);
                _fixButtons.Add(b);
            }
            _host.Controls.Add(_cardFix);

            // ----------------------------------------------------------- panels
            _cardPanels = NewCard(ConfigCatalog.GroupTitle("panels"),
                L.Count(panels.Count, "panel", "panels", "panel"));
            for (int i = 0; i < panels.Count; i++)
            {
                ConfigItem it = panels[i];
                AeroButton b = new AeroButton(L.T(it.Title));
                b.Click += delegate { OpenPanelItem(it); };
                _tip.SetToolTip(b, L.T(it.Title) + " — " + L.T(it.Desc));
                _cardPanels.Controls.Add(b);
                _panelButtons.Add(b);
            }
            _host.Controls.Add(_cardPanels);

            // ---------------------------------------------------------- profile
            _cardProf = NewCard(ConfigCatalog.GroupTitle("profile"), "");
            List<ConfigItem> prof = ConfigCatalog.Group("profile");
            for (int i = 0; i < prof.Count; i++)
            {
                ConfigItem it = prof[i];
                AeroButton b = new AeroButton(L.T(it.Title));
                b.Click += delegate { RunItem(it); };
                _tip.SetToolTip(b, L.T(it.Desc));
                _cardProf.Controls.Add(b);
                if (it.Handler == "psprofile-install") { _btnProfInstall = b; }
                else { _btnProfRemove = b; }
            }
            _host.Controls.Add(_cardProf);

            // ----------------------------------------------------------- remote
            _cardRemote = NewCard(ConfigCatalog.GroupTitle("remote"), "");
            List<ConfigItem> rem = ConfigCatalog.Group("remote");
            for (int i = 0; i < rem.Count; i++)
            {
                ConfigItem it = rem[i];
                _btnSsh = new AeroButton(L.T(it.Title));
                _btnSsh.Click += delegate { RunItem(it); };
                _tip.SetToolTip(_btnSsh, L.T(it.Desc));
                _cardRemote.Controls.Add(_btnSsh);
            }
            _host.Controls.Add(_cardRemote);

            _host.ResumeLayout(false);
            ResumeLayout(true);
        }

        private static AeroCard NewCard(string caption, string hint)
        {
            AeroCard c = new AeroCard();
            c.Caption = caption;
            c.CaptionHint = hint;
            c.Margin = new Padding(0);
            return c;
        }

        private void AddFeatureRow(ConfigItem item)
        {
            FeatureRow r = new FeatureRow();
            r.Item = item;

            r.Chk = new AeroCheck(L.T(item.Title));
            r.Chk.Height = CheckRowH;
            AeroCheck chk = r.Chk;
            chk.CheckStateChanged += delegate { if (!_loading) UpdateHint(); };

            r.State = Ui.Lbl("", Aero.Small, Theme.Dim);
            r.State.AutoSize = false;
            r.State.AutoEllipsis = true;
            r.State.TextAlign = ContentAlignment.MiddleLeft;

            HookWheel(chk);
            HookWheel(r.State);
            _tip.SetToolTip(chk, L.T(item.Title));
            _tip.SetToolTip(r.State, L.T(item.Desc));

            _cardFeat.Controls.Add(chk);
            _cardFeat.Controls.Add(r.State);
            _featRows.Add(r);
        }

        private void AddFeatureAction(ConfigItem item)
        {
            ConfigItem it = item;
            AeroButton b = new AeroButton(L.T(it.Title));
            b.Click += delegate { RunItem(it); };
            _tip.SetToolTip(b, L.T(it.Desc));
            _cardFeat.Controls.Add(b);
            _featActionButtons.Add(b);
        }

        private void HookWheel(Control c)
        {
            c.MouseWheel += delegate(object s, MouseEventArgs e)
            {
                HandledMouseEventArgs h = e as HandledMouseEventArgs;
                if (h != null) h.Handled = true;
                _scroll.Wheel(e.Delta);
            };
        }

        // =============================================================== layout

        private static void Place(Control c, int x, int y, int w, int h)
        {
            c.SetBounds(x, y, Math.Max(40, w), Math.Max(30, h));
        }

        /// <summary>Single pass: lay the cards out inside, then place the cards themselves.</summary>
        private void LayoutAll()
        {
            if (_laying || _host == null || _scroll == null) return;
            if (_cardFeat == null || _cardFix == null || _cardPanels == null ||
                _cardProf == null || _cardRemote == null) return;
            _laying = true;
            int contentH = 0;
            _host.SuspendLayout();
            try
            {
                int w = Math.Max(140, _scroll.ViewWidth - 2);
                bool stack = Width < StackBreak;
                const int gap = 8;
                const int colGap = 12;

                int leftW = stack ? w : (w - colGap) * 56 / 100;
                int rightW = stack ? w : w - colGap - leftW;

                int featH = LayoutFeatures(leftW);
                int fixH = LayoutFixes(leftW);
                int panelsH = LayoutPanels(rightW);
                int profH = LayoutProfile(rightW);
                int remH = LayoutRemote(rightW);

                if (stack)
                {
                    int y = 0;
                    Place(_cardFeat, 0, y, w, featH); y += featH + gap;
                    Place(_cardFix, 0, y, w, fixH); y += fixH + gap;
                    Place(_cardPanels, 0, y, w, panelsH); y += panelsH + gap;
                    Place(_cardProf, 0, y, w, profH); y += profH + gap;
                    Place(_cardRemote, 0, y, w, remH); y += remH;
                    contentH = y;
                }
                else
                {
                    Place(_cardFeat, 0, 0, leftW, featH);
                    Place(_cardFix, 0, featH + gap, leftW, fixH);
                    Place(_cardPanels, leftW + colGap, 0, rightW, panelsH);
                    Place(_cardProf, leftW + colGap, panelsH + gap, rightW, profH);
                    Place(_cardRemote, leftW + colGap, panelsH + gap + profH + gap, rightW, remH);
                    contentH = Math.Max(featH + gap + fixH, panelsH + gap + profH + gap + remH);
                }
            }
            finally
            {
                _host.ResumeLayout(false);
                _laying = false;
            }
            _scroll.SetContent(contentH);
            FitLogCard(contentH);
            LayoutLogCard();
        }

        /// <summary>
        /// The card column is laid out at its natural height. On a short window the
        /// activity log yields room first, so the last Fixes button is not cut off at
        /// the fold; below MinLogCardH the scroll host takes over.
        /// </summary>
        private void FitLogCard(int contentH)
        {
            if (_grid == null || _grid.RowStyles.Count < 2) return;
            int avail = _grid.ClientSize.Height;
            if (avail <= 0) return;
            int maxLog = avail - (contentH + 8);
            int want = LogCardH;
            if (maxLog < want) want = maxLog;
            if (want < MinLogCardH) want = MinLogCardH;
            if (Math.Abs(_grid.RowStyles[1].Height - want) < 0.5f) return;
            _grid.RowStyles[1].Height = want;
        }

        private int LayoutFeatures(int cardW)
        {
            int w = Math.Max(80, cardW - 2 * CardPad);
            int y = 28;
            int stateW = w >= 300 ? 96 : 0;
            for (int i = 0; i < _featRows.Count; i++)
            {
                FeatureRow r = _featRows[i];
                int avail = stateW > 0 ? Math.Max(90, w - stateW - 6) : w;
                r.Chk.SetBounds(CardPad, y, avail, CheckRowH);
                if (stateW > 0)
                {
                    r.State.Visible = true;
                    r.State.SetBounds(CardPad + avail + 6, y + 3, stateW, CheckRowH - 6);
                }
                else r.State.Visible = false;
                y += CheckRowH;
            }
            if (_featActionButtons.Count > 0) y += 6;
            for (int i = 0; i < _featActionButtons.Count; i++)
            {
                _featActionButtons[i].SetBounds(CardPad, y, w, Aero.ButtonH);
                y += Aero.ButtonH + PanelButtonGap;
            }
            y += 6;
            if (_btnInstall != null) _btnInstall.SetBounds(CardPad, y, w, Aero.ButtonH);
            y += Aero.ButtonH;
            return y + 8;
        }

        private int LayoutFixes(int cardW)
        {
            int w = Math.Max(80, cardW - 2 * CardPad);
            int y = 28;
            for (int i = 0; i < _fixButtons.Count; i++)
            {
                _fixButtons[i].SetBounds(CardPad, y, w, Aero.ButtonH);
                y += Aero.ButtonH + PanelButtonGap;
            }
            return y + 2;
        }

        private int LayoutPanels(int cardW)
        {
            int w = Math.Max(80, cardW - 2 * CardPad);
            int cols = w >= PanelColBreak ? 2 : 1;
            int btnW = (w - (cols - 1) * 6) / cols;
            int y = 28;
            for (int i = 0; i < _panelButtons.Count; i++)
            {
                int c = i % cols;
                int r = i / cols;
                _panelButtons[i].SetBounds(CardPad + c * (btnW + 6), y + r * (Aero.ButtonH + PanelButtonGap),
                    btnW, Aero.ButtonH);
            }
            int rows = cols > 0 ? (_panelButtons.Count + cols - 1) / cols : 0;
            return y + rows * (Aero.ButtonH + PanelButtonGap) + 2;
        }

        private int LayoutProfile(int cardW)
        {
            int w = Math.Max(80, cardW - 2 * CardPad);
            int y = 28;
            if (_btnProfInstall != null)
            {
                _btnProfInstall.SetBounds(CardPad, y, w, Aero.ButtonH);
                y += Aero.ButtonH + PanelButtonGap;
            }
            if (_btnProfRemove != null)
            {
                _btnProfRemove.SetBounds(CardPad, y, w, Aero.ButtonH);
                y += Aero.ButtonH + PanelButtonGap;
            }
            return y + 2;
        }

        private int LayoutRemote(int cardW)
        {
            int w = Math.Max(80, cardW - 2 * CardPad);
            int y = 28;
            if (_btnSsh != null)
            {
                _btnSsh.SetBounds(CardPad, y, w, Aero.ButtonH);
                y += Aero.ButtonH + PanelButtonGap;
            }
            return y + 2;
        }

        private void LayoutLogCard()
        {
            if (_cardLog == null || _btnCancel == null || _status == null) return;
            int right = _cardLog.ClientSize.Width - 10;
            _btnCancel.SetBounds(Math.Max(10, right - _btnCancel.Width), 3,
                _btnCancel.Width, 18);
            _status.SetBounds(CardPad, Math.Max(24, _cardLog.ClientSize.Height - 19),
                Math.Max(40, _cardLog.ClientSize.Width - 2 * CardPad), 15);
        }

        // ================================================================ state

        private void RefreshClicked()
        {
            if (_busy) return;
            LogLine(L.T("Reading feature state again..."));
            ConfigEngine.Invalidate();
            LoadStates();
        }

        /// <summary>Feature state is read by DISM on a background Task; nothing touches the UI thread.</summary>
        private void LoadStates()
        {
            if (_loadingStates || _featRows.Count == 0) return;
            if (!_elevated)
            {
                int[] none = new int[_featRows.Count];
                for (int i = 0; i < none.Length; i++) none[i] = -1;
                ApplyStates(none);
                return;
            }

            _loadingStates = true;
            UpdateHint();
            string[] names = ConfigCatalog.FeatureNames();
            List<FeatureRow> rows = new List<FeatureRow>(_featRows);

            Task.Run(delegate
            {
                Dictionary<string, bool> st = null;
                try { st = ConfigEngine.FeatureStates(names); }
                catch { st = null; }
                int[] res = new int[rows.Count];
                for (int i = 0; i < rows.Count; i++)
                {
                    ConfigItem it = rows[i].Item;
                    if (st == null || it.Features == null || it.Features.Length == 0)
                    {
                        res[i] = -1;
                        continue;
                    }
                    int on = 0;
                    int known = 0;
                    for (int j = 0; j < it.Features.Length; j++)
                    {
                        bool v;
                        if (!st.TryGetValue(it.Features[j], out v)) continue;
                        known++;
                        if (v) on++;
                    }
                    if (known == 0) res[i] = -1;
                    else res[i] = on == 0 ? 0 : (on >= it.Features.Length ? 2 : 1);
                }
                SafeUi(delegate
                {
                    _loadingStates = false;
                    ApplyStates(res);
                    UpdateHint();
                });
            });
        }

        /// <summary>state: 2 all features enabled, 1 partly, 0 none, -1 unknown.</summary>
        private void ApplyStates(int[] state)
        {
            if (state == null) return;
            bool old = _loading;
            _loading = true;
            try
            {
                for (int i = 0; i < _featRows.Count && i < state.Length; i++)
                {
                    FeatureRow r = _featRows[i];
                    if (state[i] < 0)
                    {
                        r.Chk.Checked = false;
                        r.State.Text = L.T("Unknown");
                        r.State.ForeColor = Theme.Dim;
                        continue;
                    }
                    r.Chk.Checked = state[i] == 2;
                    if (state[i] == 2)
                    {
                        r.State.Text = L.T("Enabled");
                        r.State.ForeColor = Aero.SuccessText;
                    }
                    else if (state[i] == 1)
                    {
                        r.State.Text = L.T("Partly enabled");
                        r.State.ForeColor = Aero.WarningText;
                    }
                    else
                    {
                        r.State.Text = L.T("Not enabled");
                        r.State.ForeColor = Theme.Dim;
                    }
                }
            }
            finally { _loading = old; }
        }

        private int CheckedCount()
        {
            int n = 0;
            for (int i = 0; i < _featRows.Count; i++)
                if (_featRows[i].Chk.Checked) n++;
            return n;
        }

        private void UpdateHint()
        {
            if (_cmd == null) return;
            if (_busy) { _cmd.SetHint(L.T("Working...")); return; }
            if (_loadingStates) { _cmd.SetHint(L.T("Reading feature state...")); return; }
            int n = CheckedCount();
            if (n > 0)
            {
                _cmd.SetHint(L.Count(n, "feature selected", "features selected", "özellik seçili"));
                return;
            }
            string hint = L.T("Features · fixes · legacy panels");
            if (!_elevated) hint += " · " + L.T("limited mode");
            _cmd.SetHint(hint);
        }

        private void SetStatus(string text)
        {
            if (_status == null) return;
            _status.Text = text == null ? "" : text;
            _tip.SetToolTip(_status, text == null ? "" : text);
        }

        private void SetBusy(bool on)
        {
            bool en = !on;
            bool admin = en && _elevated;
            for (int i = 0; i < _featRows.Count; i++) _featRows[i].Chk.Enabled = admin;
            for (int i = 0; i < _featActionButtons.Count; i++) _featActionButtons[i].Enabled = admin;
            for (int i = 0; i < _fixButtons.Count; i++) _fixButtons[i].Enabled = admin;
            for (int i = 0; i < _panelButtons.Count; i++) _panelButtons[i].Enabled = en;
            if (_btnInstall != null) _btnInstall.Enabled = admin;
            if (_btnSsh != null) _btnSsh.Enabled = admin;
            if (_btnProfInstall != null) _btnProfInstall.Enabled = en;
            if (_btnProfRemove != null) _btnProfRemove.Enabled = en;
            if (_btnRefresh != null) _btnRefresh.Enabled = en;
            if (_btnCancel != null)
            {
                _btnCancel.Visible = on;
                _btnCancel.Enabled = on;
            }
        }

        // ============================================================== actions

        private void RunItem(ConfigItem item)
        {
            if (item == null) return;
            if (_busy || ProcRunner.Busy)
            {
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }
            if (!_elevated && NeedsAdmin(item))
            {
                Shell.Toast(L.T("Administrator rights are required for this action."), Aero.Warning);
                return;
            }

            string message = item.ConfirmText != null && item.ConfirmText.Length > 0
                ? L.T(item.ConfirmText) : L.T(item.Desc);
            string icon = item.Danger ? "danger" : "warn";
            bool go = AeroDialog.Confirm(FindForm(), L.T(item.Title), message,
                L.T("Run"), L.T("Cancel"), icon, item.Danger);
            if (!go) return;

            List<ConfigItem> items = new List<ConfigItem>();
            items.Add(item);
            StartItems(items, L.T(item.Title));
        }

        /// <summary>Panels only start a shell process, so they stay outside the job guard.</summary>
        private void OpenPanelItem(ConfigItem item)
        {
            if (item == null) return;
            string error;
            if (ConfigEngine.Run(item, LogLine, null, out error)) return;
            LogLine(error);
            SetStatus(error);
            Shell.Toast(error, Aero.Danger);
        }

        private void InstallSelected()
        {
            if (_busy || ProcRunner.Busy)
            {
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }
            if (!_elevated)
            {
                Shell.Toast(L.T("Administrator rights are required for this action."), Aero.Warning);
                return;
            }

            List<ConfigItem> items = new List<ConfigItem>();
            List<string> names = new List<string>();
            for (int i = 0; i < _featRows.Count; i++)
            {
                FeatureRow r = _featRows[i];
                if (!r.Chk.Checked) continue;
                items.Add(r.Item);
                for (int j = 0; j < r.Item.Features.Length; j++) names.Add(r.Item.Features[j]);
            }
            if (items.Count == 0)
            {
                Shell.Toast(L.T("Tick at least one feature first."), Aero.Warning);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(L.T("These Windows features will be installed:"));
            sb.Append("\n\n");
            int show = Math.Min(12, names.Count);
            for (int i = 0; i < show; i++)
            {
                sb.Append("+ ");
                sb.Append(names[i]);
                sb.Append("\n");
            }
            if (names.Count > show) sb.Append(L.F("… and {0} more", names.Count - show));
            sb.Append("\n");
            sb.Append(L.T("A restart may be required. DISM output is written to the log."));

            bool go = AeroDialog.Confirm(FindForm(), L.T("Install Features"), sb.ToString(),
                L.T("Install"), L.T("Cancel"), "warn", false);
            if (!go) return;

            StartItems(items, L.T("Install Features"));
        }

        private void StartItems(List<ConfigItem> items, string title)
        {
            if (!ProcRunner.TryBeginJob())
            {
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }

            _busy = true;
            _cancel = new CancelFlag();
            _realProgress = items.Count > 1;
            _spin = 0;
            CancelFlag flag = _cancel;
            SetBusy(true);
            UpdateHint();
            LogLine(L.F("Started: {0}", title));
            SetStatus(L.F("Working: {0}", title));

            List<ConfigItem> batch = new List<ConfigItem>(items);
            int total = batch.Count;

            Task.Run(delegate
            {
                int ok = 0;
                int bad = 0;
                bool restart = false;
                string lastError = "";
                for (int i = 0; i < batch.Count; i++)
                {
                    if (flag.Cancelled) break;
                    ConfigItem it = batch[i];
                    string caption = L.T(it.Title);
                    LogLine("— " + caption);
                    string error;
                    bool good = false;
                    try { good = ConfigEngine.Run(it, LogLine, flag, out error); }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        good = false;
                    }
                    if (ConfigEngine.LastRunNeedsRestart) restart = true;
                    if (good) ok++;
                    else
                    {
                        bad++;
                        if (!string.IsNullOrEmpty(error))
                        {
                            lastError = error;
                            LogLine(error);
                        }
                    }

                    int done = i + 1;
                    bool passed = good;
                    string captionNow = caption;
                    SafeUi(delegate
                    {
                        if (total > 1) Shell.SetProgress((double)done / total);
                        SetStatus(passed ? L.F("Done: {0}", captionNow) : L.F("Failed: {0}", captionNow));
                    });
                }

                ProcRunner.EndJob();
                int okFinal = ok;
                int badFinal = bad;
                bool restartFinal = restart;
                string errFinal = lastError;
                SafeUi(delegate { FinishRun(okFinal, badFinal, errFinal, flag, restartFinal); });
            });
        }

        private void FinishRun(int ok, int bad, string lastError, CancelFlag flag, bool restart)
        {
            _busy = false;
            _cancel = null;
            _realProgress = false;
            Shell.ClearProgress();
            SetBusy(true);

            if (flag != null && flag.Cancelled)
            {
                LogLine(L.T("Cancelled"));
                SetStatus(L.T("Cancelled"));
                Shell.Toast(L.F("Cancelled · {0} completed", ok), Aero.Warning);
            }
            else if (bad == 0)
            {
                string done = L.F("{0} completed", ok);
                LogLine(done);
                SetStatus(done);
                Shell.Toast(done, Aero.Success);
            }
            else
            {
                string done = L.F("{0} completed · {1} failed", ok, bad);
                LogLine(done);
                SetStatus(lastError != null && lastError.Length > 0 ? lastError : done);
                Shell.Toast(done, Aero.Warning);
            }

            if (restart && (flag == null || !flag.Cancelled))
            {
                LogLine(L.T("Restart the computer to finish."));
                Shell.Toast(L.T("Restart the computer to finish."), Aero.Warning);
            }

            UpdateHint();
            ConfigEngine.Invalidate();
            LoadStates();
        }

        private void RequestCancel()
        {
            if (_cancel == null) return;
            _cancel.Cancelled = true;
            _btnCancel.Enabled = false;
            LogLine(L.T("Cancel requested — stopping after the current step..."));
            SetStatus(L.T("Cancelling..."));
        }

        private static bool NeedsAdmin(ConfigItem item)
        {
            if (item == null) return false;
            if (item.Group == "panels") return false;
            if (item.Group == "profile") return false;
            return true;
        }

        // ================================================================== log

        /// <summary>Called from worker threads: queue only; the 150 ms timer flushes.</summary>
        private void LogLine(string text)
        {
            if (text == null) return;
            lock (_pending)
            {
                _pending.Enqueue(text);
                while (_pending.Count > 2000) _pending.Dequeue();
            }
        }

        private void DrainLog()
        {
            if (_log == null || IsDisposed) return;
            List<string> batch;
            lock (_pending)
            {
                if (_pending.Count == 0) return;
                batch = new List<string>(_pending);
                _pending.Clear();
            }
            for (int i = 0; i < batch.Count; i++) _log.Append(batch[i]);
        }

        /// <summary>Log flush plus the indeterminate progress sweep shown while a job runs.</summary>
        private void Tick()
        {
            DrainLog();
            if (!_busy || _realProgress) return;
            _spin += 0.09;
            if (_spin > 1.0) _spin = 0.0;
            Shell.SetProgress(_spin);
        }

        private void SafeUi(Action a)
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
