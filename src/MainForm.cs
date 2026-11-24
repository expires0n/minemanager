using System;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MineManager
{
    // 30px Aero title bar: 16px icon, Segoe UI 600 title, 26x17 caption buttons.
    internal class AeroTitleBar : Control
    {
        private readonly Form _form;
        private readonly Bitmap _icon;
        private readonly ToolTip _tip;
        private int _hot = -1;
        private int _down = -1;
        private string _tipText = "";

        public AeroTitleBar(Form form, Bitmap icon)
        {
            _form = form;
            _icon = icon;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.StandardDoubleClick | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            _tip = new ToolTip();
            _tip.InitialDelay = 500;
            _tip.ReshowDelay = 200;
            _tip.UseAnimation = !AeroEnv.ReducedMotion;
            AccessibleRole = AccessibleRole.TitleBar;
            AccessibleName = L.T("MineManager title bar");
        }

        private Rectangle CloseRect
        {
            get { return new Rectangle(Width - 6 - Aero.CaptionW, 6, Aero.CaptionW, Aero.CaptionH); }
        }

        private Rectangle MaxRect
        {
            get
            {
                Rectangle c = CloseRect;
                return new Rectangle(c.X - Aero.CaptionGap - Aero.CaptionW, c.Y, Aero.CaptionW, Aero.CaptionH);
            }
        }

        private Rectangle MinRect
        {
            get
            {
                Rectangle m = MaxRect;
                return new Rectangle(m.X - Aero.CaptionGap - Aero.CaptionW, m.Y, Aero.CaptionW, Aero.CaptionH);
            }
        }

        private int Hit(Point p)
        {
            if (CloseRect.Contains(p)) return 2;
            if (MaxRect.Contains(p)) return 1;
            if (MinRect.Contains(p)) return 0;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int h = Hit(e.Location);
            if (h != _hot)
            {
                _hot = h;
                Invalidate();
            }
            string text = h == 0 ? L.T("Minimize") :
                          h == 1 ? (_form.WindowState == FormWindowState.Maximized ? L.T("Restore") : L.T("Maximize")) :
                          h == 2 ? L.T("Close") : "";
            if (text != _tipText)
            {
                _tipText = text;
                _tip.SetToolTip(this, text);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hot = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) { base.OnMouseDown(e); return; }
            int h = Hit(e.Location);
            if (h >= 0)
            {
                _down = h;
                Invalidate();
            }
            else if (e.Clicks == 2)
            {
                ToggleMaximize();
            }
            else
            {
                Native.ReleaseCapture();
                Native.SendMessage(_form.Handle, Native.WM_NCLBUTTONDOWN, (IntPtr)Native.HTCAPTION, IntPtr.Zero);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            int h = Hit(e.Location);
            if (_down >= 0 && h == _down)
            {
                if (h == 0) _form.WindowState = FormWindowState.Minimized;
                else if (h == 1) ToggleMaximize();
                else _form.Close();
            }
            _down = -1;
            Invalidate();
            base.OnMouseUp(e);
        }

        private void ToggleMaximize()
        {
            _form.WindowState = _form.WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal : FormWindowState.Maximized;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_icon != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(_icon, new Rectangle(10, 7, 16, 16));
            }
            else
            {
                Rectangle logo = new Rectangle(10, 7, 16, 16);
                Draw.FillVGrad(g, logo, 2, Aero.Blue400, Aero.Blue700);
                Draw.StrokeRound(g, logo, 2, Aero.Blue800);
                Draw.Bolt(g, Rectangle.Inflate(logo, -3, -3), Aero.White);
            }

            int titleW = Math.Max(20, MinRect.X - 40);
            Rectangle title = new Rectangle(32, 0, titleW, Height);
            // Aero title text: 1px white underlay + ink, no glow blur
            if (!AeroEnv.HighContrast)
                TextRenderer.DrawText(g, "MineManager", Aero.BodySemibold,
                    new Rectangle(title.X, 1, title.Width, title.Height), Aero.TitleShadow,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, "MineManager", Aero.BodySemibold, title, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            PaintCaption(g, MinRect, 0, "min");
            PaintCaption(g, MaxRect, 1,
                _form.WindowState == FormWindowState.Maximized ? "restore" : "max");
            PaintCaption(g, CloseRect, 2, "close");

            using (Pen p = new Pen(AeroEnv.HighContrast
                ? SystemColors.WindowFrame : Color.FromArgb(70, 24, 56, 88)))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }

        private void PaintCaption(Graphics g, Rectangle r, int index, string glyph)
        {
            bool hot = _hot == index;
            bool pressed = hot && _down == index;
            if (AeroEnv.HighContrast)
            {
                using (SolidBrush b = new SolidBrush(hot ? SystemColors.Highlight : SystemColors.Control))
                    g.FillRectangle(b, r);
                using (Pen p = new Pen(SystemColors.WindowFrame))
                    g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
                Draw.Glyph(g, r, glyph, hot ? SystemColors.HighlightText : SystemColors.WindowText);
                return;
            }

            if (hot && index == 2)
            {
                Draw.FillVGrad(g, r, Aero.RadiusControl,
                    pressed ? Draw.Mix(Aero.Danger, Aero.Black, 0.12f) : Draw.Mix(Aero.Danger, Aero.White, 0.26f),
                    pressed ? Draw.Mix(Aero.Danger, Aero.Black, 0.3f) : Draw.Mix(Aero.Danger, Aero.Black, 0.12f),
                    Draw.Mix(Aero.Danger, Aero.Black, 0.24f));
                Draw.StrokeRound(g, r, Aero.RadiusControl, Draw.Mix(Aero.Danger, Aero.Black, 0.22f));
                Draw.Glyph(g, r, glyph, Aero.White);
            }
            else if (hot)
            {
                Draw.FillVGrad(g, r, Aero.RadiusControl,
                    pressed ? Aero.CaptionPressedTop : Aero.CaptionHoverTop,
                    pressed ? Aero.CaptionPressedBottom : Aero.CaptionHoverBottom);
                Draw.StrokeRound(g, r, Aero.RadiusControl, Aero.SelectionEdge);
                Draw.Glyph(g, r, glyph, Aero.Ink);
            }
            else
            {
                Draw.FillVGrad(g, r, Aero.RadiusControl,
                    Aero.CaptionFaceTop, Aero.CaptionFaceBottom);
                Draw.StrokeRound(g, r, Aero.RadiusControl, Aero.CaptionEdge);
                Draw.Glyph(g, r, glyph, Theme.Text);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_tip != null) _tip.Dispose();
                // _icon is the shared AeroLogo.Image; not owned by this control.
            }
            base.Dispose(disposing);
        }
    }

    internal class MainForm : Form
    {
        private AeroTitleBar _title;
        private TableLayoutPanel _layout;
        private AeroNav _nav;
        private Panel _host;
        private AeroStatusBar _status;
        private UserControl[] _pages;
        private const int PageCount = 9;
        private const int PageSettings = 8;
        private CleanerPage _cleaner;
        private MemoryPage _memory;
        private UninstallerPage _uninstaller;
        private TweaksPage _tweaks;
        private ConfigPage _config;
        private HardwarePage _hardware;
        private DiagnosticsPage _diagnostics;
        private RecoveryPage _recovery;
        private SettingsPage _settings;
        private Timer _timer;
        private int _tick;

        public MainForm()
        {
            Text = "MineManager";
            BackColor = Theme.Bg;
            ForeColor = Theme.Text;
            Font = Aero.Body;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1100, 720);
            MinimumSize = new Size(320, 560);
            FormBorderStyle = FormBorderStyle.None;
            DoubleBuffered = true;
            KeyPreview = true;
            Padding = new Padding(Aero.GlassPad, Aero.GlassPad + Aero.TitleBarH, Aero.GlassPad, Aero.GlassPad);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            Aero.SetDark(AppSettings.Dark);
            Shell.MainForm = this;
            BuildUi(0);

            _timer = new Timer();
            _timer.Interval = 1000;
            _timer.Tick += delegate { Tick(); };
            _timer.Start();
            CpuLoad.Next();
            Tick();

            Resize += delegate { UpdateChrome(); };
            Shown += delegate { UpdateChrome(); UpdateShape(); };
            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                // A long job (uninstall queue, tweak batch, DISM) must not be orphaned by
                // closing the window without a word. If the user closes anyway, the tools the
                // job started are stopped with it — chkdsk, sfc or DISM left running invisibly
                // keep the machine busy and break later actions until the next reboot.
                if (!ProcRunner.Busy || e.CloseReason != CloseReason.UserClosing) return;
                bool close = AeroDialog.Confirm(this, L.T("A job is still running"),
                    L.T("A background operation has not finished. Closing MineManager now also stops the running Windows tool. Close anyway?"),
                    L.T("Close anyway"), L.T("Cancel"), "warn", true);
                if (close) ProcRunner.Shutdown();
                e.Cancel = !close;
            };
        }

        private void BuildUi(int selected)
        {
            SuspendLayout();
            List<Control> old = new List<Control>();
            foreach (Control c in Controls) old.Add(c);
            Controls.Clear();
            for (int i = 0; i < old.Count; i++) old[i].Dispose();

            _title = new AeroTitleBar(this, AeroLogo.Image);
            _title.SetBounds(Aero.GlassPad, Aero.GlassPad,
                Math.Max(40, ClientSize.Width - Aero.GlassPad * 2), Aero.TitleBarH);
            _title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(_title);

            _layout = new TableLayoutPanel();
            _layout.Dock = DockStyle.Fill;
            _layout.Margin = new Padding(0);
            _layout.Padding = new Padding(0);
            _layout.BackColor = Theme.Bg;
            _layout.ColumnCount = 2;
            _layout.RowCount = 2;
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Aero.NavW));
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _layout.RowStyles.Add(new RowStyle(SizeType.Absolute, Aero.StatusH));
            Controls.Add(_layout);
            _layout.BringToFront();

            _nav = new AeroNav();
            _nav.Dock = DockStyle.Fill;
            _nav.Margin = new Padding(0);
            _nav.AddHeader(L.T("SYSTEM"));
            _nav.AddItem(L.T("Cleanup"), "spark");
            _nav.AddItem("RAM", "ram");
            _nav.AddHeader(L.T("MANAGEMENT"));
            _nav.AddItem(L.T("Uninstaller"), "box");
            _nav.AddItem(L.T("Tweaks"), "gear");
            _nav.AddItem(L.T("Config"), "shield");
            _nav.AddItem(L.T("Hardware"), "chip");
            _nav.AddItem(L.T("Diagnostics"), "info");
            _nav.AddHeader(L.T("RECOVERY"));
            _nav.AddItem(L.T("Recovery"), "repair");
            _nav.AddHeader(L.T("APP"));
            _nav.AddItem(L.T("Settings"), "sliders");
            _layout.Controls.Add(_nav, 0, 0);

            _host = new Panel();
            _host.Dock = DockStyle.Fill;
            _host.Margin = new Padding(0);
            _host.BackColor = Theme.Bg;
            _layout.Controls.Add(_host, 1, 0);

            _status = new AeroStatusBar();
            _status.Dock = DockStyle.Fill;
            _status.Margin = new Padding(0);
            _layout.Controls.Add(_status, 0, 1);
            _layout.SetColumnSpan(_status, 2);
            Shell.Status = _status;

            _cleaner = new CleanerPage();
            _memory = new MemoryPage();
            _uninstaller = new UninstallerPage();
            _tweaks = new TweaksPage();
            _config = new ConfigPage();
            _hardware = new HardwarePage();
            _diagnostics = new DiagnosticsPage();
            _recovery = new RecoveryPage();
            _settings = new SettingsPage();
            _pages = new UserControl[] { _cleaner, _memory, _uninstaller, _tweaks, _config, _hardware, _diagnostics, _recovery, _settings };
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].Dock = DockStyle.Fill;
                _pages[i].Visible = i == selected;
                _host.Controls.Add(_pages[i]);
            }

            _nav.SelectionChanged += delegate { ShowPage(_nav.Selected); };
            _nav.Selected = selected;

            string mode = Native.IsAdmin() ? L.T("administrator") : L.T("limited mode");
            _status.LeftText = L.F("{0} sections · portable · {1}", _pages.Length, mode);

            UpdateStatus();
            UpdateChrome();
            ShowPage(selected);
            ResumeLayout(true);
            UpdateShape();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            DarkMode.Apply(this, Aero.Dark);
        }

        public void ApplyPreferences(int page)
        {
            Aero.SetDark(AppSettings.Dark);
            L.SetLanguage(AppSettings.Language);
            BuildUi(page < 0 || page >= PageCount ? PageSettings : page);
            Tick();
            DarkMode.Apply(this, Aero.Dark);
            Invalidate(true);
        }

        private void ShowPage(int index)
        {
            if (index < 0 || index >= _pages.Length) return;
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].Visible = i == index;
            if (index == 0) _cleaner.OnShown();
            if (index == 1) _memory.OnShown();
            if (index == 3) _tweaks.OnShown();
            if (index == 4) _config.OnShown();
            if (index == 5) _hardware.OnShown();
            if (index == 6) _diagnostics.OnShown();
            if (index == 7) _recovery.OnShown();
        }

        private void Tick()
        {
            _tick++;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            double cpu = CpuLoad.Next();
            Native.MEMORYSTATUSEX m = Native.MemoryStatus();
            string text = "RAM " + L.Percent((int)m.dwMemoryLoad) + "   ·   CPU " + cpu.ToString("0", L.Culture);
            if (_tick % 5 == 0)
            {
                ulong free, total, totalFree;
                if (Native.GetDiskFreeSpaceEx("C:\\", out free, out total, out totalFree))
                    text = L.F("C: {0} free   ·   {1}", Fmt.Size((double)free), text);
            }
            _status.RightText = text;
        }

        private void UpdateChrome()
        {
            if (_title == null || _layout == null || _nav == null) return;
            _title.SetBounds(Aero.GlassPad, Aero.GlassPad,
                Math.Max(40, ClientSize.Width - Aero.GlassPad * 2), Aero.TitleBarH);

            bool compact = ClientSize.Width < 620;
            _layout.ColumnStyles[0].Width = compact ? Aero.NavCompactW : Aero.NavW;
            _nav.Compact = compact;
            _nav.Invalidate();
            UpdateShape();
        }

        private void UpdateShape()
        {
            if (WindowState == FormWindowState.Maximized)
            {
                Region = null;
                return;
            }
            try
            {
                Rectangle r = new Rectangle(0, 0, Width, Height);
                using (GraphicsPath p = Draw.Round(r, Aero.RadiusWindow))
                {
                    Region old = Region;
                    Region = new Region(p);
                    if (old != null) old.Dispose();
                }
            }
            catch { }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Bg);
            if (!AeroEnv.HighContrast)
            {
                Rectangle r = ClientRectangle;
                using (LinearGradientBrush bg = Draw.VGrad(r,
                    Aero.WindowTop, Aero.WindowMid, Aero.WindowBottom))
                    g.FillRectangle(bg, r);
            }
            Draw.Glass(g, new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1),
                Aero.RadiusWindow, true);
            // compress the glass gradient into the 30px title bar so it reads as Aero,
            // not as a 4%-of-window white slice
            Draw.Glass(g, new Rectangle(0, 0, ClientSize.Width - 1,
                Aero.GlassPad + Aero.TitleBarH), Aero.RadiusWindow, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Pen p = new Pen(AeroEnv.HighContrast
                ? SystemColors.WindowFrame : Aero.TitleShadow))
                e.Graphics.DrawLine(p, Aero.GlassPad, Aero.GlassPad + Aero.TitleBarH,
                    ClientSize.Width - Aero.GlassPad, Aero.GlassPad + Aero.TitleBarH);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result == 1 && WindowState != FormWindowState.Maximized)
                {
                    long lp = m.LParam.ToInt64();
                    Point p = PointToClient(new Point(unchecked((short)(lp & 0xFFFF)),
                        unchecked((short)((lp >> 16) & 0xFFFF))));
                    int g = 5;
                    bool left = p.X < g, right = p.X >= ClientSize.Width - g;
                    bool top = p.Y < g, bottom = p.Y >= ClientSize.Height - g;
                    if (top && left) m.Result = (IntPtr)Native.HTTOPLEFT;
                    else if (top && right) m.Result = (IntPtr)Native.HTTOPRIGHT;
                    else if (bottom && left) m.Result = (IntPtr)Native.HTBOTTOMLEFT;
                    else if (bottom && right) m.Result = (IntPtr)Native.HTBOTTOMRIGHT;
                    else if (left) m.Result = (IntPtr)Native.HTLEFT;
                    else if (right) m.Result = (IntPtr)Native.HTRIGHT;
                    else if (top) m.Result = (IntPtr)Native.HTTOP;
                    else if (bottom) m.Result = (IntPtr)Native.HTBOTTOM;
                }
                return;
            }
            if (m.Msg == Native.WM_GETMINMAXINFO)
            {
                base.WndProc(ref m);
                try
                {
                    Native.MINMAXINFO mmi = (Native.MINMAXINFO)Marshal.PtrToStructure(
                        m.LParam, typeof(Native.MINMAXINFO));
                    Rectangle wa = Screen.FromHandle(Handle).WorkingArea;
                    mmi.ptMaxPosition.X = wa.X;
                    mmi.ptMaxPosition.Y = wa.Y;
                    mmi.ptMaxSize.X = wa.Width;
                    mmi.ptMaxSize.Y = wa.Height;
                    mmi.ptMaxTrackSize.X = wa.Width;
                    mmi.ptMaxTrackSize.Y = wa.Height;
                    Marshal.StructureToPtr(mmi, m.LParam, true);
                }
                catch { }
                return;
            }
            base.WndProc(ref m);
        }
    }
}
