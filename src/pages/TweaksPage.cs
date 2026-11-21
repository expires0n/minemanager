using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Tweaks page (WinUtil "Tweaks" tab) in the app's Win7 Aero style:
    /// essential/advanced check lists on the left, preference switches and
    /// performance plan buttons on the right, activity log at the bottom.
    /// Owner: tweaks UI work package. Lives entirely on TweakCatalog/TweakEngine.
    /// </summary>
    internal class TweaksPage : UserControl
    {
        private const int LogOpenH = 110;
        private const int LogClosedH = 28;
        private const int StackBreak = 720;      // two columns above, one below
        private const int PreviewLines = 12;

        // ------------------------------------------------------------ row model

        private class CheckRow
        {
            public Tweak T;
            public AeroCheck Chk;
            public Label Badge;
            public InfoButton Info;
        }

        private class ToggleRow
        {
            public Tweak T;
            public AeroToggle Tog;
            public InfoButton Info;
        }

        /// <summary>
        /// Vertical list with a hidden native scrollbar: children live in a single
        /// host panel that is moved by the offset, so wheel scrolling is fully
        /// manual (deterministic) and the Aero bar drives the same offset.
        /// </summary>
        private class ScrollList : Panel
        {
            private readonly Panel _host;
            private readonly AeroScrollBar _bar;
            private int _contentH;
            private int _offset;
            private bool _sync;
            private bool _resizing;

            public ScrollList()
            {
                BackColor = Theme.Bg;
                DoubleBuffered = true;

                _host = new Panel();
                _host.BackColor = Theme.Surface;
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

            /// <summary>Outer list that receives the wheel when this one cannot scroll.</summary>
            public ScrollList ParentScroll;

            /// <summary>Called when the reserved bar gutter changed the row width.</summary>
            public Action Relayout;

            public bool CanScroll { get { return MaxOffset > 0; } }

            public int HostWidth { get { return Math.Max(40, _host.Width); } }

            public int ViewWidth { get { return Math.Max(40, ClientSize.Width - 14); } }

            public int ViewHeight { get { return Math.Max(20, ClientSize.Height); } }

            /// <summary>Content width available to one row (small safety margin).</summary>
            public int RowWidth { get { return Math.Max(60, HostWidth - 4); } }

            public void SetContent(int height)
            {
                int wasW = _host.Width;
                _contentH = Math.Max(0, height);
                if (_offset > MaxOffset) _offset = MaxOffset;
                Place();
                if (wasW != _host.Width && Relayout != null && !_resizing)
                {
                    _resizing = true;
                    try { Relayout(); }
                    finally { _resizing = false; }
                }
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
                if (MaxOffset > 0)
                {
                    int steps = delta / 120;
                    if (steps == 0 && delta != 0) steps = delta > 0 ? 1 : -1;
                    ScrollTo(_offset - steps * (3 * Aero.RowH));
                    return;
                }
                // nothing left to scroll here: hand the wheel to the page scroller
                if (ParentScroll != null && ParentScroll != this) ParentScroll.Wheel(delta);
            }

            private void Place()
            {
                bool scroll = MaxOffset > 0;
                int w = scroll ? ViewWidth : ClientSize.Width;
                _host.SetBounds(0, -_offset, w, Math.Max(ClientSize.Height, _contentH));
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

        /// <summary>16 px info glyph button; AeroButton's glyph sits at x=8, too wide for 16 px.</summary>
        private class InfoButton : AeroButton
        {
            private bool _hot;

            public InfoButton(Tweak t)
                : base("")
            {
                Glyph = "info";
                TabStop = false;
                Size = new Size(16, 16);
                AccessibleRole = AccessibleRole.PushButton;
                AccessibleName = L.T(t.Title);
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                _hot = true;
                Invalidate();
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _hot = false;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
                bool on = Enabled;
                if (_hot && on)
                {
                    Draw.FillVGrad(g, r, Aero.RadiusControl,
                        Aero.Selection[0], Aero.Selection[1], Aero.Selection[2]);
                    Draw.StrokeRound(g, r, Aero.RadiusControl, Aero.SelectionEdge);
                }
                Draw.Glyph(g, new Rectangle(2, 2, 12, 12), "info",
                    !on ? Aero.Disabled : (_hot ? Aero.Blue700 : Aero.InkSoft));
            }
        }

        // --------------------------------------------------------------- state

        private AeroCommandBar _cmd;
        private AeroButton _btnRun, _btnUndo, _btnRefresh, _btnCollapse, _btnCancel;
        private TableLayoutPanel _grid, _outer, _leftCol, _rightCol;
        private ScrollList _scroller;
        private AeroCard _cardEss, _cardAdv, _cardPrefs, _cardPerf, _cardLog;
        private ScrollList _listEss, _listAdv, _listPrefs;
        private AeroCheck _chkGuard;
        private AeroConsole _log;
        private ToolTip _tip;

        private readonly List<Tweak> _ess = new List<Tweak>();
        private readonly List<Tweak> _adv = new List<Tweak>();
        private readonly List<Tweak> _prefs = new List<Tweak>();
        private readonly List<Tweak> _perf = new List<Tweak>();
        private readonly List<Tweak> _probe = new List<Tweak>();

        private readonly List<CheckRow> _essRows = new List<CheckRow>();
        private readonly List<CheckRow> _advRows = new List<CheckRow>();
        private readonly List<ToggleRow> _prefRows = new List<ToggleRow>();
        private readonly List<AeroButton> _perfButtons = new List<AeroButton>();

        private readonly Dictionary<string, AeroCheck> _chkMap =
            new Dictionary<string, AeroCheck>(StringComparer.Ordinal);
        private readonly Dictionary<string, Label> _badgeMap =
            new Dictionary<string, Label>(StringComparer.Ordinal);
        private readonly Dictionary<string, AeroToggle> _togMap =
            new Dictionary<string, AeroToggle>(StringComparer.Ordinal);
        private readonly Dictionary<string, TweakState> _states =
            new Dictionary<string, TweakState>(StringComparer.Ordinal);
        private readonly HashSet<string> _touched = new HashSet<string>(StringComparer.Ordinal);

        private readonly Queue<string> _pending = new Queue<string>();
        private Timer _flush;
        private CancelFlag _cancel;
        private int _okCount, _failCount;
        private bool _busy, _detecting, _laying, _logOpen = true;
        private bool _loading = true;
        private bool _elevated;
        private int _badgeW = 64;

        // ============================================================ lifecycle

        public TweaksPage()
        {
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            DoubleBuffered = true;
            _elevated = ProcRunner.IsElevated();

            _tip = new ToolTip();
            _tip.InitialDelay = 400;
            _tip.AutoPopDelay = 15000;

            MeasureBadgeWidth();

            BuildCommandBar();
            BuildBody();
            BuildGroups();
            BuildPerf();
            Rehydrate();
            _loading = false;

            SetLogOpen(true);
            LayoutColumns();
            LayoutLists();
            SetActions(true);
            UpdateHint();

            _flush = new Timer();
            _flush.Interval = 150;
            _flush.Tick += delegate { DrainLog(); };
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
            LayoutColumns();
            LayoutLists();
            DrainLog();
            RefreshStates();
        }

        // ================================================================ build

        /// <summary>Badge column is measured in the current language (Turkish runs longer).</summary>
        private void MeasureBadgeWidth()
        {
            Size cap = new Size(300, 20);
            int w = TextRenderer.MeasureText(L.T("Applied"), Aero.Small, cap, TextFormatFlags.NoPadding).Width;
            w = Math.Max(w, TextRenderer.MeasureText(L.T("Not applied"), Aero.Small, cap,
                TextFormatFlags.NoPadding).Width);
            w = Math.Max(w, TextRenderer.MeasureText(L.T("Mixed"), Aero.Small, cap,
                TextFormatFlags.NoPadding).Width);
            _badgeW = Math.Min(96, w + 2);
        }

        private void BuildCommandBar()
        {
            _cmd = new AeroCommandBar();
            _cmd.Dock = DockStyle.Top;
            _cmd.SetTitle(L.T("Tweaks"), L.T("Registry tweaks · preferences · performance plans"));
            Controls.Add(_cmd);

            _btnRun = Ui.Btn(L.T("Run Tweaks"), 96);
            _btnRun.Click += delegate { RunSelected(false); };
            _cmd.AddButton(_btnRun);

            _btnUndo = Ui.Outline(L.T("Undo Selected"), 104);
            _btnUndo.Click += delegate { RunSelected(true); };
            _cmd.AddButton(_btnUndo);

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
            _grid.RowStyles.Add(new RowStyle(SizeType.Absolute, LogOpenH));
            body.Controls.Add(_grid);

            _scroller = new ScrollList();
            _scroller.Dock = DockStyle.Fill;
            _scroller.Margin = new Padding(0, 0, 0, 8);
            _scroller.Host.BackColor = Theme.Bg;
            _scroller.Resize += delegate { LayoutColumns(); };
            _grid.Controls.Add(_scroller, 0, 0);

            _outer = new TableLayoutPanel();
            _outer.Margin = new Padding(0);
            _outer.Padding = new Padding(0);
            _outer.BackColor = Theme.Bg;
            _outer.ColumnCount = 2;
            _outer.RowCount = 2;
            _scroller.Host.Controls.Add(_outer);

            _leftCol = NewColumn();
            _rightCol = NewColumn();
            _outer.Controls.Add(_leftCol, 0, 0);
            _outer.Controls.Add(_rightCol, 1, 0);

            // page level wheel routing (the inner lists handle their own wheel first)
            HookWheel(_scroller, _outer);
            HookWheel(_scroller, _leftCol);
            HookWheel(_scroller, _rightCol);

            BuildLogCard();
            _grid.Controls.Add(_cardLog, 0, 1);
        }

        private static TableLayoutPanel NewColumn()
        {
            TableLayoutPanel p = new TableLayoutPanel();
            p.Dock = DockStyle.Fill;
            p.Margin = new Padding(0);
            p.Padding = new Padding(0);
            p.BackColor = Theme.Bg;
            p.ColumnCount = 1;
            p.RowCount = 2;
            p.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return p;
        }

        private void BuildLogCard()
        {
            _cardLog = new AeroCard();
            _cardLog.Caption = L.T("Activity Log");
            _cardLog.CaptionHint = L.T("last 300 lines");
            _cardLog.Dock = DockStyle.Fill;
            _cardLog.Margin = new Padding(0);

            _log = new AeroConsole();
            _log.Dock = DockStyle.Fill;
            _cardLog.Controls.Add(_log);

            _btnCollapse = Ui.Outline(L.T("Collapse"), 62);
            _btnCollapse.Size = new Size(62, 18);
            _btnCollapse.Click += delegate { SetLogOpen(!_logOpen); };
            _cardLog.Controls.Add(_btnCollapse);

            _btnCancel = Ui.Outline(L.T("Cancel"), 58);
            _btnCancel.Size = new Size(58, 18);
            _btnCancel.Visible = false;
            _btnCancel.Click += delegate { RequestCancel(); };
            _cardLog.Controls.Add(_btnCancel);

            _btnCollapse.BringToFront();
            _btnCancel.BringToFront();
            _cardLog.Resize += delegate { LayoutLogCard(); };
            LayoutLogCard();
        }

        private void BuildGroups()
        {
            SuspendLayout();

            // -------------------------------------------------- essential card
            _cardEss = NewGroupCard("essential", out _listEss);
            _ess.AddRange(TweakCatalog.Group("essential"));
            for (int i = 0; i < _ess.Count; i++) AddCheckRow(_essRows, _listEss, _ess[i]);
            _leftCol.Controls.Add(_cardEss, 0, 0);

            // --------------------------------------------------- advanced card
            _cardAdv = NewGroupCard("advanced", out _listAdv);
            _chkGuard = new AeroCheck(L.T("I understand — enable advanced tweaks"));
            _chkGuard.Dock = DockStyle.Top;
            _chkGuard.Height = Aero.RowH + 2;
            _chkGuard.CheckStateChanged += delegate { OnGuardChanged(); };
            _cardAdv.Controls.Add(_chkGuard);
            _cardAdv.Controls.SetChildIndex(_listAdv, 0);
            _chkGuard.Checked = TweakSession.IncludeAdvanced;

            _adv.AddRange(TweakCatalog.Group("advanced"));
            for (int i = 0; i < _adv.Count; i++) AddCheckRow(_advRows, _listAdv, _adv[i]);
            _leftCol.Controls.Add(_cardAdv, 0, 1);

            // ------------------------------------------------------- prefs card
            _cardPrefs = NewGroupCard("prefs", out _listPrefs);
            _prefs.AddRange(TweakCatalog.Group("prefs"));
            for (int i = 0; i < _prefs.Count; i++) AddToggleRow(_prefRows, _listPrefs, _prefs[i]);
            _rightCol.Controls.Add(_cardPrefs, 0, 0);

            ResumeLayout(true);
        }

        private AeroCard NewGroupCard(string group, out ScrollList list)
        {
            AeroCard card = new AeroCard();
            card.Caption = TweakCatalog.GroupTitle(group);
            List<Tweak> items = TweakCatalog.Group(group);
            card.CaptionHint = L.Count(items.Count, "tweak", "tweaks", "ayar");
            card.Margin = new Padding(0, 0, 0, 8);
            card.Dock = DockStyle.Fill;

            ScrollList l = new ScrollList();
            l.Dock = DockStyle.Fill;
            l.ParentScroll = _scroller;
            l.Relayout = LayoutLists;
            l.Resize += delegate { LayoutLists(); };
            card.Controls.Add(l);
            card.Resize += delegate { LayoutLists(); };
            list = l;
            return card;
        }

        private void BuildPerf()
        {
            _cardPerf = new AeroCard();
            _cardPerf.Caption = TweakCatalog.GroupTitle("performance");
            _cardPerf.CaptionHint = L.Count(TweakCatalog.Group("performance").Count,
                "action", "actions", "işlem");
            _cardPerf.Margin = new Padding(0);
            _cardPerf.Dock = DockStyle.Fill;

            _perf.AddRange(TweakCatalog.Group("performance"));
            for (int i = 0; i < _perf.Count; i++)
            {
                Tweak t = _perf[i];
                AeroButton b = new AeroButton(L.T(t.Title));
                b.Size = new Size(200, Aero.ButtonH);
                Tweak captured = t;
                b.Click += delegate { RunOne(captured); };
                _tip.SetToolTip(b, L.T(t.Desc));
                _cardPerf.Controls.Add(b);
                _perfButtons.Add(b);
            }
            _cardPerf.Resize += delegate { LayoutPerf(); };
            _rightCol.Controls.Add(_cardPerf, 0, 1);
        }

        private CheckRow AddCheckRow(List<CheckRow> rows, ScrollList list, Tweak t)
        {
            CheckRow r = new CheckRow();
            r.T = t;

            r.Chk = new AeroCheck(L.T(t.Title));
            r.Chk.Height = Aero.RowH;
            AeroCheck chk = r.Chk;
            chk.CheckStateChanged += delegate
            {
                if (_loading) return;
                _touched.Add(t.Id);
                Persist(t.Id, chk.Checked);
                UpdateHint();
            };

            r.Badge = Ui.Lbl("", Aero.Small, Theme.Dim);
            r.Badge.AutoSize = false;
            r.Badge.AutoEllipsis = true;
            r.Badge.TextAlign = ContentAlignment.MiddleLeft;
            r.Badge.Height = Aero.RowH - 4;

            r.Info = new InfoButton(t);
            r.Info.Click += delegate { ShowDetails(t); };

            HookWheel(list, chk);
            HookWheel(list, r.Badge);
            HookWheel(list, r.Info);
            _tip.SetToolTip(chk, L.T(t.Title));
            _tip.SetToolTip(r.Info, L.T("Details"));

            list.Host.Controls.Add(chk);
            list.Host.Controls.Add(r.Badge);
            list.Host.Controls.Add(r.Info);
            rows.Add(r);
            _chkMap[t.Id] = chk;
            _badgeMap[t.Id] = r.Badge;
            if (t.HasState) _probe.Add(t);
            return r;
        }

        private void AddToggleRow(List<ToggleRow> rows, ScrollList list, Tweak t)
        {
            ToggleRow r = new ToggleRow();
            r.T = t;

            r.Tog = new AeroToggle(L.T(t.Title));
            r.Tog.Indent = true;
            r.Tog.Height = Aero.RowH;
            AeroToggle tog = r.Tog;
            tog.CheckedChanged += delegate
            {
                if (_loading) return;
                OnToggleChanged(t, tog);
            };

            r.Info = new InfoButton(t);
            r.Info.Click += delegate { ShowDetails(t); };

            HookWheel(list, tog);
            HookWheel(list, r.Info);
            _tip.SetToolTip(tog, L.T(t.Title));
            _tip.SetToolTip(r.Info, L.T("Details"));

            list.Host.Controls.Add(tog);
            list.Host.Controls.Add(r.Info);
            rows.Add(r);
            _togMap[t.Id] = tog;
            if (t.HasState) _probe.Add(t);
        }

        private void HookWheel(ScrollList list, Control c)
        {
            c.MouseWheel += delegate(object s, MouseEventArgs e)
            {
                HandledMouseEventArgs h = e as HandledMouseEventArgs;
                if (h != null) h.Handled = true;
                list.Wheel(e.Delta);
            };
        }

        /// <summary>Selections and the log survive a page rebuild (language/theme switch).</summary>
        private void Rehydrate()
        {
            _loading = true;
            try
            {
                foreach (KeyValuePair<string, AeroCheck> kv in _chkMap)
                {
                    if (!TweakSession.Selected.Contains(kv.Key)) continue;
                    kv.Value.Checked = true;
                    _touched.Add(kv.Key);
                }
                if (_chkGuard != null) _chkGuard.Checked = TweakSession.IncludeAdvanced;
            }
            finally { _loading = false; }

            // advanced settings stay out of the selection while the guard is off
            if (!TweakSession.IncludeAdvanced)
            {
                _loading = true;
                try
                {
                    for (int i = 0; i < _advRows.Count; i++)
                    {
                        _advRows[i].Chk.Checked = false;
                        TweakSession.Selected.Remove(_advRows[i].T.Id);
                    }
                }
                finally { _loading = false; }
            }

            lock (TweakSession.Log)
            {
                for (int i = 0; i < TweakSession.Log.Count; i++) _log.Append(TweakSession.Log[i]);
            }
        }

        // =============================================================== layout

        private void LayoutColumns()
        {
            if (_laying || _outer == null || _scroller == null || _leftCol == null) return;
            _laying = true;
            _outer.SuspendLayout();
            _leftCol.SuspendLayout();
            _rightCol.SuspendLayout();
            int contentH;
            int viewH = Math.Max(80, _scroller.ViewHeight);
            bool stack = Width < StackBreak;
            try
            {
                int viewW = Math.Max(140, _scroller.ViewWidth - 2);
                int perfH = PerfHeight();

                _outer.ColumnStyles.Clear();
                _outer.RowStyles.Clear();
                _leftCol.RowStyles.Clear();
                _rightCol.RowStyles.Clear();

                if (stack)
                {
                    // single column: every card takes its full content height and the
                    // page itself scrolls, so nothing can be unreachable at 320 px.
                    int essH = CardContentHeight(_essRows.Count, 0);
                    int advH = CardContentHeight(_advRows.Count, Aero.RowH + 2);
                    int prefsH = CardContentHeight(_prefRows.Count, 0);
                    int leftH = essH + 8 + advH;
                    int rightH = prefsH + 8 + perfH;
                    _outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    _outer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 0));
                    _outer.RowStyles.Add(new RowStyle(SizeType.Absolute, leftH));
                    _outer.RowStyles.Add(new RowStyle(SizeType.Absolute, rightH + 8));
                    _outer.SetCellPosition(_leftCol, new TableLayoutPanelCellPosition(0, 0));
                    _outer.SetColumnSpan(_leftCol, 2);
                    _outer.SetCellPosition(_rightCol, new TableLayoutPanelCellPosition(0, 1));
                    _outer.SetColumnSpan(_rightCol, 2);
                    _leftCol.Margin = new Padding(0);
                    _rightCol.Margin = new Padding(0, 8, 0, 0);
                    _leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, essH + 8));
                    _leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, advH));
                    _rightCol.RowStyles.Add(new RowStyle(SizeType.Absolute, prefsH + 8));
                    _rightCol.RowStyles.Add(new RowStyle(SizeType.Absolute, perfH));
                    contentH = leftH + rightH + 8;
                }
                else
                {
                    _outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
                    _outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
                    _outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    _outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
                    _outer.SetCellPosition(_leftCol, new TableLayoutPanelCellPosition(0, 0));
                    _outer.SetColumnSpan(_leftCol, 1);
                    _outer.SetCellPosition(_rightCol, new TableLayoutPanelCellPosition(1, 0));
                    _outer.SetColumnSpan(_rightCol, 1);
                    _leftCol.Margin = new Padding(0);
                    _rightCol.Margin = new Padding(12, 0, 0, 0);
                    _leftCol.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
                    _leftCol.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
                    _rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    _rightCol.RowStyles.Add(new RowStyle(SizeType.Absolute, perfH + 8));
                    contentH = viewH;
                }

                _outer.SetBounds(0, 0, Math.Max(140, viewW), stack ? contentH : Math.Max(contentH, viewH));
            }
            finally
            {
                _leftCol.ResumeLayout();
                _rightCol.ResumeLayout();
                _outer.ResumeLayout(true);
                _laying = false;
            }
            _scroller.SetContent(contentH);
            // the page bar may appear/disappear; keep the outer panel on the host width
            _outer.SetBounds(0, 0, Math.Max(140, _scroller.HostWidth),
                stack ? contentH : Math.Max(contentH, viewH));
            LayoutLists();
        }

        private int PerfHeight()
        {
            int n = _perfButtons.Count;
            int h = 34 + n * (Aero.ButtonH + 6);
            return Math.Max(56, h);
        }

        /// <summary>Card height needed to show every row without an inner scrollbar.</summary>
        private static int CardContentHeight(int rows, int extraTop)
        {
            return 26 + extraTop + rows * Aero.RowH + 16;
        }

        private void LayoutLists()
        {
            LayoutChecks(_essRows, _listEss);
            LayoutChecks(_advRows, _listAdv);
            LayoutToggles(_prefRows, _listPrefs);
            LayoutPerf();
        }

        private void LayoutChecks(List<CheckRow> rows, ScrollList list)
        {
            if (list == null || rows.Count == 0) return;
            int w = list.RowWidth;
            int badgeW = w < 250 ? 0 : Math.Min(w / 3, _badgeW);
            int right = w - 2;
            for (int i = 0; i < rows.Count; i++)
            {
                CheckRow r = rows[i];
                int y = i * Aero.RowH;
                int infoW = 16;
                int avail = right - (infoW + 6) - (badgeW > 0 ? badgeW + 8 : 0);
                if (avail < 60) avail = Math.Max(40, right - infoW - 6);
                r.Chk.SetBounds(2, y, avail, Aero.RowH);
                if (badgeW > 0)
                {
                    r.Badge.Visible = true;
                    r.Badge.SetBounds(2 + avail + 8, y + 2, badgeW, Aero.RowH - 4);
                }
                else
                {
                    r.Badge.Visible = false;
                }
                r.Info.SetBounds(right - infoW, y + (Aero.RowH - 16) / 2, infoW, 16);
            }
            list.SetContent(rows.Count * Aero.RowH + 4);
        }

        private void LayoutToggles(List<ToggleRow> rows, ScrollList list)
        {
            if (list == null || rows.Count == 0) return;
            int w = list.RowWidth;
            int right = w - 2;
            int infoW = 16;
            int avail = Math.Max(60, right - infoW - 8);
            for (int i = 0; i < rows.Count; i++)
            {
                ToggleRow r = rows[i];
                int y = i * Aero.RowH;
                r.Tog.SetBounds(2, y, avail, Aero.RowH);
                r.Info.SetBounds(right - infoW, y + (Aero.RowH - 16) / 2, infoW, 16);
            }
            list.SetContent(rows.Count * Aero.RowH + 4);
        }

        private void LayoutPerf()
        {
            if (_cardPerf == null) return;
            int w = Math.Max(60, _cardPerf.ClientSize.Width - 20);
            int y = 30;
            for (int i = 0; i < _perfButtons.Count; i++)
            {
                _perfButtons[i].SetBounds(10, y, w, Aero.ButtonH);
                y += Aero.ButtonH + 6;
            }
        }

        private void LayoutLogCard()
        {
            if (_cardLog == null || _btnCollapse == null || _btnCancel == null) return;
            int right = _cardLog.ClientSize.Width - 10;
            _btnCollapse.SetBounds(Math.Max(10, right - _btnCollapse.Width), 3,
                _btnCollapse.Width, 18);
            right -= _btnCollapse.Width + 6;
            _btnCancel.SetBounds(Math.Max(10, right - _btnCancel.Width), 3,
                _btnCancel.Width, 18);
        }

        private void SetLogOpen(bool open)
        {
            _logOpen = open;
            if (_log != null) _log.Visible = open;
            if (_btnCollapse != null) _btnCollapse.Text = open ? L.T("Collapse") : L.T("Expand");
            if (_cardLog != null)
            {
                _cardLog.CaptionHint = open ? L.T("last 300 lines") : "";
                LayoutLogCard();
            }
            if (_grid != null && _grid.RowStyles.Count > 1)
                _grid.RowStyles[1].Height = open ? LogOpenH : LogClosedH;
            if (open) DrainLog();
        }

        // ================================================================ state

        private void RefreshClicked()
        {
            if (_busy) return;
            LogLine(L.T("Refreshing detected state..."));
            RefreshStates();
        }

        /// <summary>Detection always runs on a background Task; state lands on the UI thread.</summary>
        private void RefreshStates()
        {
            if (_detecting || _probe.Count == 0) return;
            _detecting = true;
            List<Tweak> items = new List<Tweak>(_probe);
            Task.Run(delegate
            {
                int n = items.Count;
                TweakState[] result = new TweakState[n];
                for (int i = 0; i < n; i++)
                {
                    TweakState s = TweakState.Unknown;
                    try { s = TweakEngine.Detect(items[i]); }
                    catch { s = TweakState.Unknown; }
                    result[i] = s;
                }
                SafeUi(delegate
                {
                    _detecting = false;
                    ApplyStates(items, result);
                });
            });
        }

        private void ApplyStates(List<Tweak> items, TweakState[] states)
        {
            if (items == null || states == null) return;
            bool old = _loading;
            _loading = true;
            try
            {
                for (int i = 0; i < items.Count && i < states.Length; i++)
                {
                    Tweak t = items[i];
                    TweakState s = states[i];
                    _states[t.Id] = s;

                    AeroCheck chk;
                    if (_chkMap.TryGetValue(t.Id, out chk))
                    {
                        if (!_touched.Contains(t.Id)) chk.Checked = s == TweakState.Applied;
                        Label badge;
                        if (_badgeMap.TryGetValue(t.Id, out badge)) UpdateBadge(badge, s);
                    }

                    AeroToggle tog;
                    if (_togMap.TryGetValue(t.Id, out tog) && s != TweakState.Unknown &&
                        !_touched.Contains(t.Id))
                        tog.Checked = s == TweakState.Applied;
                }
            }
            finally { _loading = old; }
            UpdateHint();
        }

        private void UpdateBadge(Label badge, TweakState s)
        {
            switch (s)
            {
                case TweakState.Applied:
                    badge.Text = L.T("Applied");
                    badge.ForeColor = Aero.SuccessText;
                    _tip.SetToolTip(badge, L.T("Applied"));
                    break;
                case TweakState.NotApplied:
                    badge.Text = L.T("Not applied");
                    badge.ForeColor = Theme.Dim;
                    _tip.SetToolTip(badge, L.T("Not applied"));
                    break;
                case TweakState.Mixed:
                    badge.Text = L.T("Mixed");
                    badge.ForeColor = Aero.WarningText;
                    _tip.SetToolTip(badge, L.T("Mixed"));
                    break;
                default:
                    badge.Text = "";
                    _tip.SetToolTip(badge, "");
                    break;
            }
        }

        private int SelectedCount()
        {
            int n = 0;
            foreach (KeyValuePair<string, AeroCheck> kv in _chkMap)
                if (kv.Value.Checked) n++;
            return n;
        }

        private void UpdateHint()
        {
            if (_cmd == null) return;
            if (_busy)
            {
                _cmd.SetHint(L.T("Working..."));
                return;
            }
            int n = SelectedCount();
            if (n > 0)
            {
                _cmd.SetHint(L.Count(n, "tweak selected", "tweaks selected", "ayar seçili"));
                return;
            }
            string hint = L.T("Registry tweaks · preferences · performance plans");
            if (!_elevated) hint += " · " + L.T("limited mode");
            _cmd.SetHint(hint);
        }

        private void Persist(string id, bool on)
        {
            if (on) TweakSession.Selected.Add(id);
            else TweakSession.Selected.Remove(id);
        }

        // ============================================================== actions

        private void SetActions(bool enabled)
        {
            if (_btnRun == null) return;
            _btnRun.Enabled = enabled;
            _btnUndo.Enabled = enabled;
            _btnRefresh.Enabled = enabled;

            bool adv = _chkGuard != null && _chkGuard.Checked;
            for (int i = 0; i < _essRows.Count; i++) _essRows[i].Chk.Enabled = enabled;
            for (int i = 0; i < _advRows.Count; i++) _advRows[i].Chk.Enabled = enabled && adv;
            for (int i = 0; i < _prefRows.Count; i++) _prefRows[i].Tog.Enabled = enabled;
            for (int i = 0; i < _perfButtons.Count; i++) _perfButtons[i].Enabled = enabled;
            if (_chkGuard != null) _chkGuard.Enabled = enabled;
        }

        private void OnGuardChanged()
        {
            if (_chkGuard == null) return;
            TweakSession.IncludeAdvanced = _chkGuard.Checked;
            bool wasLoading = _loading;
            if (!TweakSession.IncludeAdvanced)
            {
                _loading = true;
                for (int i = 0; i < _advRows.Count; i++)
                {
                    _advRows[i].Chk.Checked = false;
                    TweakSession.Selected.Remove(_advRows[i].T.Id);
                }
                _loading = wasLoading;
            }
            if (wasLoading || _busy) return;
            SetActions(true);
            UpdateHint();
        }

        private void ShowDetails(Tweak t)
        {
            if (t == null) return;
            string msg = L.T(t.Desc);
            if (t.Note != null && t.Note.Length > 0) msg += "\n\n" + L.T(t.Note);
            AeroDialog.Info(FindForm(), L.T(t.Title), msg, "info");
        }

        private void RunSelected(bool undo)
        {
            if (_busy || ProcRunner.Busy)
            {
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }
            List<Tweak> items = Collect(undo);
            if (items.Count == 0)
            {
                string why = undo ? L.T("Nothing to undo.") : L.T("Select at least one tweak.");
                if (SelectedCount() > 0)
                    why = undo ? L.T("Nothing to undo.") : L.T("Nothing to change.");
                Shell.Toast(why, Aero.Warning);
                return;
            }
            RunList(items, undo, undo ? L.T("Undo selected tweaks") : L.T("Run selected tweaks"));
        }

        private void RunOne(Tweak t)
        {
            if (t == null) return;
            List<Tweak> items = new List<Tweak>();
            items.Add(t);
            RunList(items, false, L.T(t.Title));
        }

        private List<Tweak> Collect(bool undo)
        {
            List<Tweak> list = new List<Tweak>();
            for (int i = 0; i < _essRows.Count; i++)
                Consider(_essRows[i].T, _essRows[i].Chk.Checked, undo, list);
            if (TweakSession.IncludeAdvanced)
                for (int i = 0; i < _advRows.Count; i++)
                    Consider(_advRows[i].T, _advRows[i].Chk.Checked, undo, list);
            return list;
        }

        /// <summary>Only offer tweaks whose detected state differs from the request.</summary>
        private void Consider(Tweak t, bool selectedNow, bool undo, List<Tweak> list)
        {
            if (!selectedNow || t == null) return;
            TweakState s;
            if (!_states.TryGetValue(t.Id, out s)) s = TweakState.Unknown;
            if (undo)
            {
                if (s == TweakState.Applied || s == TweakState.Mixed) list.Add(t);
                else if (s == TweakState.Unknown && t.UndoScript != null && t.UndoScript.Length > 0)
                    list.Add(t);
                return;
            }
            if (s != TweakState.Applied) list.Add(t);
        }

        private string BuildPreview(List<Tweak> items, bool undo)
        {
            List<string> lines = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                List<string> plan = null;
                try { plan = TweakEngine.Preview(items[i], undo); }
                catch { plan = null; }
                if (plan != null && plan.Count > 0)
                {
                    for (int j = 0; j < plan.Count; j++) lines.Add(plan[j]);
                }
                else
                {
                    lines.Add((undo ? "− " : "+ ") + L.T(items[i].Title));
                }
            }

            List<string> head = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (line == null || line.Length == 0) continue;
                // keep the confirmation dialog readable; full output goes to the log
                if (line.Length > 96) line = line.Substring(0, 95) + "…";
                head.Add(line);
            }

            StringBuilder sb = new StringBuilder();
            sb.Append(undo ? L.T("Undo the selected tweaks:") : L.T("Run the selected tweaks:"));
            sb.Append("\n\n");
            int show = Math.Min(PreviewLines, head.Count);
            for (int i = 0; i < show; i++)
            {
                sb.Append(head[i]);
                sb.Append("\n");
            }
            if (head.Count > show) sb.Append(L.F("… and {0} more lines", head.Count - show));
            return sb.ToString();
        }

        private void RunList(List<Tweak> items, bool undo, string title)
        {
            if (items == null || items.Count == 0) return;
            if (_busy || ProcRunner.Busy)
            {
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }

            bool needAdmin = false, danger = false;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].RequiresAdmin) needAdmin = true;
                if (items[i].Danger) danger = true;
            }
            if (needAdmin && !_elevated)
            {
                Shell.Toast(L.T("Administrator rights are required for the selected tweaks."),
                    Aero.Warning);
                return;
            }

            string message = BuildPreview(items, undo);
            string icon = danger ? "danger" : "warn";
            bool go = AeroDialog.Confirm(FindForm(), title, message,
                undo ? L.T("Undo") : L.T("Run"), L.T("Cancel"), icon, danger);
            if (!go) return;

            StartBatch(items, undo);
        }

        private void StartBatch(List<Tweak> items, bool undo)
        {
            if (!ProcRunner.TryBeginJob())
            {
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }

            _busy = true;
            _okCount = 0;
            _failCount = 0;
            CancelFlag flag = new CancelFlag();
            _cancel = flag;
            SetActions(false);
            _btnCancel.Visible = true;
            _btnCancel.Enabled = true;
            UpdateHint();
            LogLine(L.F("{0} · {1}", undo ? L.T("Undo started") : L.T("Run started"),
                L.Count(items.Count, "tweak", "tweaks", "ayar")));

            Task.Run(delegate
            {
                try
                {
                    TweakEngine.ApplyBatch(items, undo, LogLine, OnProgress, flag);
                }
                catch (Exception ex)
                {
                    LogLine(L.F("Error: {0}", ex.Message));
                }
                finally
                {
                    ProcRunner.EndJob();
                    SafeUi(delegate { FinishBatch(flag); });
                }
            });
        }

        private void FinishBatch(CancelFlag flag)
        {
            _busy = false;
            _cancel = null;
            _btnCancel.Visible = false;
            _btnCancel.Enabled = true;
            Shell.ClearProgress();
            SetActions(true);

            int ok = _okCount;
            int bad = _failCount;
            bool cancelled = flag != null && flag.Cancelled;

            if (cancelled)
            {
                LogLine(L.T("Cancelled"));
                Shell.Toast(L.F("Cancelled · {0} completed", ok), Aero.Warning);
            }
            else if (bad == 0)
            {
                LogLine(L.F("{0} completed", ok));
                Shell.Toast(L.F("{0} completed", ok), Aero.Success);
            }
            else
            {
                LogLine(L.F("{0} completed · {1} failed", ok, bad));
                Shell.Toast(L.F("{0} completed · {1} failed", ok, bad), Aero.Warning);
            }

            _touched.Clear();
            UpdateHint();
            RefreshStates();
        }

        private void RequestCancel()
        {
            if (_cancel == null) return;
            _cancel.Cancelled = true;
            _btnCancel.Enabled = false;
            LogLine(L.T("Cancel requested — stopping after the current tweak..."));
        }

        private void OnToggleChanged(Tweak t, AeroToggle tog)
        {
            if (t == null || tog == null) return;
            bool want = tog.Checked;
            if (_busy || ProcRunner.Busy)
            {
                Revert(t, tog, !want);
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }
            if (t.RequiresAdmin && !_elevated)
            {
                Revert(t, tog, !want);
                Shell.Toast(L.T("Administrator rights are required for the selected tweaks."),
                    Aero.Warning);
                return;
            }
            ApplyToggle(t, want, tog);
        }

        /// <summary>Roll the switch back without re-entering the apply path.</summary>
        private void Revert(Tweak t, AeroToggle tog, bool value)
        {
            bool old = _loading;
            _loading = true;
            try { tog.Checked = value; }
            finally { _loading = old; }
            if (t != null) _touched.Remove(t.Id);
        }

        /// <summary>Switches apply immediately; on failure the switch is rolled back.</summary>
        private void ApplyToggle(Tweak t, bool want, AeroToggle tog)
        {
            if (!ProcRunner.TryBeginJob())
            {
                Revert(t, tog, !want);
                Shell.Toast(L.T("A job is already running."), Aero.Warning);
                return;
            }

            _busy = true;
            _touched.Add(t.Id);
            SetActions(false);
            UpdateHint();
            LogLine(want ? L.F("Applying: {0}", L.T(t.Title)) : L.F("Restoring: {0}", L.T(t.Title)));

            Task.Run(delegate
            {
                bool ok = false;
                try
                {
                    ok = TweakEngine.Apply(t, !want, LogLine, new CancelFlag());
                }
                catch (Exception ex)
                {
                    LogLine(L.F("Error: {0}", ex.Message));
                }
                finally
                {
                    ProcRunner.EndJob();
                    SafeUi(delegate
                    {
                        _busy = false;
                        SetActions(true);
                        if (!ok)
                        {
                            Revert(t, tog, !want);
                            LogLine(want ? L.F("Could not apply: {0}", L.T(t.Title))
                                         : L.F("Could not restore: {0}", L.T(t.Title)));
                            Shell.Toast(want ? L.F("Could not apply: {0}", L.T(t.Title))
                                             : L.F("Could not restore: {0}", L.T(t.Title)),
                                Aero.Danger);
                        }
                        else
                        {
                            Shell.Toast(want ? L.F("Applied: {0}", L.T(t.Title))
                                             : L.F("Restored: {0}", L.T(t.Title)),
                                Aero.Success);
                            _touched.Clear();
                        }
                        UpdateHint();
                        RefreshStates();
                    });
                }
            });
        }

        private void OnProgress(int index, int total, Tweak t, bool ok)
        {
            if (ok) _okCount++;
            else _failCount++;
            string title = t != null ? L.T(t.Title) : "";
            int done = index;
            SafeUi(delegate
            {
                if (total > 0) Shell.SetProgress((double)done / total);
                if (title.Length > 0) _cmd.SetHint(L.F("{0}/{1} · {2}", done, total, title));
            });
        }

        // ================================================================== log

        /// <summary>Called from worker threads: queue only, the 150 ms timer flushes.</summary>
        private void LogLine(string text)
        {
            if (text == null) return;
            lock (_pending)
            {
                _pending.Enqueue(text);
                while (_pending.Count > 2000) _pending.Dequeue();
            }
            lock (TweakSession.Log)
            {
                TweakSession.Log.Add(text);
                while (TweakSession.Log.Count > 400) TweakSession.Log.RemoveAt(0);
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
