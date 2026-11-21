using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MineManager
{
    // =====================================================================
    //  Aero primitives. Cam (glass) is only used for frame / dialog / toast;
    //  surfaces here are gloss, group boxes and detail rows.
    // =====================================================================

    internal class AeroButton : Control
    {
        public bool Default;              // aero-btn--default: blue double frame + glow
        public bool Selected;             // segmented/selected state (nav-like blue face)
        public string Glyph;              // optional 16px glyph drawn left of text
        public Color SurroundColor = Color.Empty;

        private bool _hover, _down, _focus;
        private float _hoverT;
        private readonly Timer _anim;

        public AeroButton(string text)
        {
            Text = text;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            Font = Aero.Body;
            Cursor = Cursors.Hand;
            TabStop = true;
            Size = new Size(100, Aero.ButtonH);
            BackColor = Color.Transparent;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = text;
            _anim = new Timer();
            _anim.Interval = 15;
            _anim.Tick += delegate
            {
                float target = _hover ? 1f : 0f;
                if (Math.Abs(_hoverT - target) < 0.06f)
                {
                    _hoverT = target;
                    _anim.Stop();
                }
                else _hoverT += (_hoverT < target ? 0.1f : -0.1f);
                Invalidate();
            };
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            AccessibleName = Text;
        }

        public void PerformClick()
        {
            if (Enabled) OnClick(EventArgs.Empty);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            if (AeroEnv.ReducedMotion) { _hoverT = 1f; Invalidate(); }
            else _anim.Start();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false; _down = false;
            if (AeroEnv.ReducedMotion) { _hoverT = 0f; Invalidate(); }
            else _anim.Start();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _down = true; Focus(); Invalidate(); }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _down = false; Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnGotFocus(EventArgs e) { _focus = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { _focus = false; _down = false; Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { _anim.Stop(); _hoverT = 0f; _hover = false; Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                _down = true; Invalidate();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
            {
                _down = false; Invalidate();
                OnClick(EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(SurroundColor.IsEmpty ? (Parent != null ? Parent.BackColor : Theme.Bg) : SurroundColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            bool on = Enabled;

            if (AeroEnv.HighContrast)
            {
                using (SolidBrush b = new SolidBrush(_down ? SystemColors.ControlDark : SystemColors.Control))
                    g.FillPath(b, Draw.Round(r, Aero.RadiusControl));
                Draw.StrokeRound(g, r, Aero.RadiusControl, SystemColors.WindowFrame);
                int gx = 0;
                if (!string.IsNullOrEmpty(Glyph))
                {
                    Draw.Glyph(g, new Rectangle(8, (Height - 12) / 2, 12, 12), Glyph,
                        on ? SystemColors.WindowText : SystemColors.GrayText);
                    gx = 6;
                }
                TextRenderer.DrawText(g, Text, Font, new Rectangle(gx, 0, Width - gx, Height),
                    on ? SystemColors.WindowText : SystemColors.GrayText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                if (_focus && on) Draw.FocusRing(g, new Rectangle(0, 0, Width, Height), Aero.RadiusControl);
                return;
            }

            Color[] face = Aero.GlossFace;
            Color[] hot = Aero.GlossHover;
            Color[] press = Aero.GlossPressed;

            if (Default && on && !Selected) Draw.Glow(g, r, Aero.RadiusControl);

            Color[] stops = new Color[4];
            for (int i = 0; i < 4; i++)
                stops[i] = _down ? press[i] : Draw.Mix(face[i], hot[i], _hoverT);

            if (!on)
            {
                using (SolidBrush b = new SolidBrush(Aero.DisabledFace))
                    g.FillPath(b, Draw.Round(r, Aero.RadiusControl));
            }
            else if (Selected)
            {
                Draw.FillVGrad(g, r, Aero.RadiusControl,
                    Aero.Selection[0], Aero.Selection[1], Aero.Selection[2]);
            }
            else
            {
                Draw.FillVGrad(g, r, Aero.RadiusControl, stops);
            }

            // pressed: shadow turns inward
            if (_down && on)
            {
                Rectangle ir = new Rectangle(1, 1, Width - 3, 4);
                using (LinearGradientBrush b = Draw.VGrad(ir, Color.FromArgb(56, 0, 0, 0),
                    Color.FromArgb(0, 0, 0, 0)))
                    g.FillRectangle(b, ir);
            }

            // 1px border + 1px top inner highlight
            Color border = !on ? Aero.DisabledBorder
                : Selected ? Aero.SelectionEdge : (Default ? Aero.Blue600 : Aero.ButtonBorder);
            if (_hover && on && !Default && !Selected) border = Aero.Blue500;
            Draw.StrokeRound(g, r, Aero.RadiusControl, border);

            if (on)
            {
                using (Pen p = new Pen(Aero.Dark ? Color.FromArgb(40, 255, 255, 255)
                    : Color.FromArgb(230, 255, 255, 255), 1f))
                    g.DrawLine(p, r.X + 2, r.Y + 1, r.Right - 2, r.Y + 1);
            }

            if (Default && on)
            {
                using (Pen p = new Pen(Color.FromArgb(110, 0x8E, 0xC3, 0xEA), 1f))
                    using (GraphicsPath ip = Draw.Round(Rectangle.Inflate(r, -3, -3), 1))
                        g.DrawPath(p, ip);
            }

            int tx = 0;
            if (!string.IsNullOrEmpty(Glyph))
            {
                Rectangle gr = new Rectangle(8, (Height - 12) / 2, 12, 12);
                Draw.Glyph(g, gr, Glyph, on ? Aero.Ink : Aero.Disabled);
                tx = 6;
            }

            Color tc = !on ? Aero.Disabled : Aero.Ink;
            int oy = _down && on ? 1 : 0;
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(tx, oy, Width - tx, Height - 1), tc,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            if (_focus && on) Draw.FocusRing(g, new Rectangle(0, 0, Width, Height), Aero.RadiusControl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _anim.Stop(); _anim.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal class AeroCheck : Control
    {
        private bool _checked;
        private bool _hover, _focus;

        public AeroCheck(string text)
        {
            Text = text;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Font = Aero.Body;
            Cursor = Cursors.Hand;
            TabStop = true;
            Height = 22;
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleName = text;
        }

        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (CheckStateChanged != null) CheckStateChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler CheckStateChanged;

        protected override void OnClick(EventArgs e)
        {
            if (!Enabled) return;
            Focus();
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { _focus = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { _focus = false; Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                Checked = !Checked;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle box = new Rectangle(1, (Height - 13) / 2, 13, 13);
            bool on = Enabled;

            if (AeroEnv.HighContrast)
            {
                using (SolidBrush b = new SolidBrush(_checked ? SystemColors.Highlight : SystemColors.Window))
                    g.FillRectangle(b, box);
                using (Pen p = new Pen(SystemColors.WindowFrame)) g.DrawRectangle(p, box);
                if (_checked) Draw.Glyph(g, box, "check", SystemColors.HighlightText);
                TextRenderer.DrawText(g, Text, Font,
                    new Rectangle(20, 0, Math.Max(0, Width - 22), Height),
                    on ? SystemColors.WindowText : SystemColors.GrayText,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                if (_focus) Draw.FocusRing(g, new Rectangle(0, 1, Width - 1, Height - 3), Aero.RadiusControl);
                return;
            }

            using (SolidBrush b = new SolidBrush(on ? Aero.Field : Theme.Bg))
                g.FillRectangle(b, box);

            if (_checked)
            {
                Draw.FillVGrad(g, box, Aero.RadiusControl, Aero.Blue400, Aero.Blue600);
                Draw.StrokeRound(g, box, Aero.RadiusControl, on ? Aero.Blue700 : Aero.Disabled);
                Draw.Glyph(g, new Rectangle(box.X + 1, box.Y + 1, box.Width - 2, box.Height - 2), "check", Aero.White);
            }
            else
            {
                Draw.StrokeRound(g, box, Aero.RadiusControl,
                    !on ? Aero.Disabled : (_hover ? Aero.Blue500 : Aero.FieldBorder));
                using (Pen p = new Pen(Color.FromArgb(40, 0, 0, 0)))
                    g.DrawLine(p, box.X + 2, box.Y + 1, box.Right - 3, box.Y + 1);
            }

            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(20, 0, Math.Max(0, Width - 22), Height),
                on ? Aero.Ink : Aero.Disabled,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            if (_focus && on)
                Draw.FocusRing(g, new Rectangle(0, 1, Width - 1, Height - 3), Aero.RadiusControl);
        }
    }

    internal class AeroSearchBox : Panel
    {
        public readonly TextBox Box;
        private readonly string _placeholder;
        private bool _focus;

        public AeroSearchBox(string placeholder)
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            _placeholder = placeholder;
            BackColor = Theme.Surface;
            Height = Aero.FieldH;

            Box = new TextBox();
            Box.BorderStyle = BorderStyle.None;
            Box.BackColor = Theme.Surface;
            Box.ForeColor = Theme.Text;
            Box.Font = Aero.Body;
            Box.AccessibleName = placeholder;
            Box.SetBounds(22, 3, 100, 16);
            Controls.Add(Box);
            Cursor = Cursors.IBeam;

            Box.GotFocus += delegate { _focus = true; Invalidate(); };
            Box.LostFocus += delegate { _focus = false; Invalidate(); };
            Box.TextChanged += delegate { Invalidate(); };
            Box.HandleCreated += delegate { ApplyCue(); };
            ApplyCue();
        }

        private const int EM_SETCUEBANNER = 0x1501;

        /// <summary>
        /// Native cue banner. A painted placeholder (or a child Label) is always
        /// covered by the native EDIT window, so the hint has to live inside it.
        /// </summary>
        private void ApplyCue()
        {
            if (!Box.IsHandleCreated || _placeholder.Length == 0) return;
            IntPtr text = Marshal.StringToHGlobalUni(_placeholder);
            try { Native.SendMessage(Box.Handle, EM_SETCUEBANNER, (IntPtr)1, text); }
            catch { }
            finally { Marshal.FreeHGlobal(text); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // The text box is inset; clicking the field frame should not miss focus.
            Box.Focus();
            base.OnMouseDown(e);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Box != null) Box.Width = Math.Max(20, Width - 30);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (SolidBrush b = new SolidBrush(AeroEnv.HighContrast ? SystemColors.Window : Aero.Field))
                g.FillPath(b, Draw.Round(r, Aero.RadiusField));

            // field inner shadow (inset 0 1px 2px, painted as two 1px steps)
            using (Pen p = new Pen(Color.FromArgb(36, 0, 0, 0)))
                g.DrawLine(p, r.X + 1, r.Y + 1, r.Right - 1, r.Y + 1);
            using (Pen p = new Pen(Color.FromArgb(16, 0, 0, 0)))
                g.DrawLine(p, r.X + 1, r.Y + 2, r.Right - 1, r.Y + 2);

            Draw.Glyph(g, new Rectangle(6, (Height - 12) / 2, 12, 12), "search",
                Box.Text.Length == 0 ? Theme.Dim : Theme.Text);
            Color edge = AeroEnv.HighContrast
                ? (_focus ? SystemColors.Highlight : SystemColors.WindowFrame)
                : (_focus ? Aero.FocusEdge : Theme.Line);
            Draw.StrokeRound(g, r, Aero.RadiusField, edge);
            if (_focus && !AeroEnv.HighContrast)
                using (Pen p = new Pen(Color.FromArgb(102, 120, 190, 250), 2f))
                    using (GraphicsPath ip = Draw.Round(new Rectangle(1, 1, Width - 3, Height - 3), Aero.RadiusField))
                        g.DrawPath(p, ip);
        }
    }

    internal class AeroProgress : Control
    {
        private double _value;
        public Color FillColor = Aero.Success;
        public Color TrackColor = Aero.Field;
        public bool ShowSheen = true;

        public AeroProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(150, 14);
            AccessibleRole = AccessibleRole.ProgressBar;
        }

        public double Value
        {
            get { return _value; }
            set
            {
                double v = value;
                if (v < 0) v = 0;
                if (v > 1) v = 1;
                _value = v;
                Invalidate();
            }
        }

        public static Color[] ToneStops(Color c)
        {
            return new Color[]
            {
                Draw.Mix(c, Aero.White, 0.45f),
                Draw.Mix(c, Aero.White, 0.16f),
                Draw.Mix(c, Aero.Black, 0.18f),
                Draw.Mix(c, Aero.White, 0.34f)
            };
        }

        public static void PaintBar(Graphics g, Rectangle r, double value, Color tone, bool sheen)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle t = new Rectangle(r.X, r.Y, Math.Max(2, r.Width - 1), Math.Max(2, r.Height - 1));

            using (SolidBrush b = new SolidBrush(AeroEnv.HighContrast ? SystemColors.Window : Aero.Field))
                g.FillPath(b, Draw.Round(t, 2));
            using (Pen p = new Pen(AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.FieldBorder))
                using (GraphicsPath tp = Draw.Round(t, 2))
                    g.DrawPath(p, tp);

            int w = (int)Math.Round((t.Width - 2) * value);
            if (w > 1)
            {
                Rectangle f = new Rectangle(t.X + 1, t.Y + 1, w, t.Height - 2);
                if (AeroEnv.HighContrast)
                {
                    using (SolidBrush b = new SolidBrush(SystemColors.Highlight))
                        g.FillRectangle(b, f);
                }
                else
                {
                    Draw.FillVGrad(g, f, 1, ToneStops(tone));
                    if (sheen && f.Width > 6)
                    {
                        Rectangle half = new Rectangle(f.X, f.Y, f.Width, f.Height / 2);
                        using (LinearGradientBrush b = Draw.VGrad(half,
                            Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255)))
                            g.FillRectangle(b, half);
                    }
                }
            }
            g.SmoothingMode = old;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            PaintBar(g, new Rectangle(0, 0, Width, Height), _value, FillColor, ShowSheen);
        }
    }

    internal class AeroCard : Panel
    {
        public string Caption = "";
        public string CaptionHint = "";
        public int CaptionHeight = 24;

        public AeroCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            // white = surface behind transparent child labels (Aero group box)
            BackColor = AeroEnv.HighContrast ? SystemColors.Window : Theme.Surface;
            Padding = new Padding(10, 26, 10, 10);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (SolidBrush b = new SolidBrush(AeroEnv.HighContrast ? SystemColors.Window : Theme.Surface))
                g.FillPath(b, Draw.Round(r, Aero.RadiusControl));
            Draw.StrokeRound(g, r, Aero.RadiusControl,
                AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.PanelLine);

            if (Caption.Length > 0)
            {
                Rectangle cap = new Rectangle(10, 5, Math.Max(10, Width - 20), 18);
                TextRenderer.DrawText(g, Caption, Aero.BodySemibold, cap,
                    AeroEnv.HighContrast ? SystemColors.WindowText : Aero.Ink,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                if (CaptionHint.Length > 0)
                {
                    Size cs = TextRenderer.MeasureText(g, Caption, Aero.BodySemibold,
                        new Size(600, 20), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                    TextRenderer.DrawText(g, CaptionHint, Aero.Small,
                        new Rectangle(10 + cs.Width + 8, 5, Math.Max(0, Width - cs.Width - 30), 18),
                        AeroEnv.HighContrast ? SystemColors.GrayText : Aero.InkSoft,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
                }
            }
        }
    }

    internal class AeroNav : Control
    {
        private class Entry
        {
            public string Text;
            public string Icon;
            public bool Header;
            public int ItemIndex = -1;
            public int Y;
            public int H;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<int> _itemStarts = new List<int>();
        private int _selected;
        private int _hover = -1;
        private bool _focus;
        private ToolTip _tip;
        public bool Compact;

        public AeroNav()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Font = Aero.Body;
            Cursor = Cursors.Hand;
            TabStop = true;
            BackColor = Theme.Bg;
            AccessibleRole = AccessibleRole.List;
            AccessibleName = L.T("Navigation");
            _tip = new ToolTip();
            _tip.InitialDelay = 400;
        }

        public int ItemCount { get { return _itemStarts.Count; } }

        public int Selected
        {
            get { return _selected; }
            set
            {
                int v = value;
                if (v < 0) v = 0;
                if (v >= ItemCount) v = ItemCount - 1;
                if (v == _selected) return;
                _selected = v;
                Invalidate();
                if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler SelectionChanged;

        public void AddHeader(string text)
        {
            Entry e = new Entry();
            e.Header = true;
            e.Text = text;
            _entries.Add(e);
            Measure();
        }

        public void AddItem(string text, string icon)
        {
            Entry e = new Entry();
            e.Text = text;
            e.Icon = icon;
            e.ItemIndex = _itemStarts.Count;
            _entries.Add(e);
            _itemStarts.Add(_entries.Count - 1);
            Measure();
        }

        private void Measure()
        {
            int y = 8;
            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                e.H = e.Header ? 22 : Aero.RowH;
                e.Y = y;
                y += e.H;
            }
            Invalidate();
        }

        private Entry EntryAt(int y)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (y >= _entries[i].Y && y < _entries[i].Y + _entries[i].H && !_entries[i].Header)
                    return _entries[i];
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            Entry en = EntryAt(e.Y);
            int h = en == null ? -1 : en.ItemIndex;
            if (h != _hover)
            {
                _hover = h;
                Invalidate();
                if (Compact) _tip.SetToolTip(this, en == null ? "" : en.Text);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Entry en = EntryAt(e.Y);
            if (en != null && e.Button == MouseButtons.Left)
            {
                Focus();
                Selected = en.ItemIndex;
            }
            base.OnMouseDown(e);
        }

        protected override void OnGotFocus(EventArgs e) { _focus = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { _focus = false; Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down) { Selected = _selected + 1; e.Handled = true; }
            else if (e.KeyCode == Keys.Up) { Selected = _selected - 1; e.Handled = true; }
            else if (e.KeyCode == Keys.Home) { Selected = 0; e.Handled = true; }
            else if (e.KeyCode == Keys.End) { Selected = ItemCount - 1; e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Theme.Bg);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry en = _entries[i];
                if (en.Header)
                {
                    if (Compact) continue;
                    TextRenderer.DrawText(g, en.Text, Aero.SmallSemibold,
                        new Rectangle(10, en.Y + 4, Width - 16, 16),
                        AeroEnv.HighContrast ? SystemColors.GrayText : Aero.InkSoft,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                        TextFormatFlags.EndEllipsis);
                    continue;
                }

                Rectangle row = new Rectangle(4, en.Y, Width - 9, en.H);
                bool sel = en.ItemIndex == _selected;

                if (AeroEnv.HighContrast)
                {
                    if (sel)
                    {
                        using (SolidBrush b = new SolidBrush(SystemColors.Highlight))
                            g.FillRectangle(b, row);
                        using (Pen p = new Pen(SystemColors.HighlightText))
                            g.DrawRectangle(p, row.X, row.Y, row.Width - 1, row.Height - 1);
                    }
                    else if (en.ItemIndex == _hover)
                    {
                        using (SolidBrush b = new SolidBrush(SystemColors.ControlDark))
                            g.FillRectangle(b, row);
                    }
                }
                else if (sel)
                {
                    Draw.FillVGrad(g, row, Aero.RadiusControl,
                        Aero.Selection[0], Aero.Selection[1], Aero.Selection[2]);
                    Draw.StrokeRound(g, row, Aero.RadiusControl, Aero.SelectionEdge);
                }
                else if (en.ItemIndex == _hover)
                {
                    Draw.FillVGrad(g, row, Aero.RadiusControl, Aero.RowHoverTop, Aero.RowHoverBottom);
                    Draw.StrokeRound(g, row, Aero.RadiusControl, Aero.SelectionEdge);
                }

                Color itemInk = AeroEnv.HighContrast
                    ? (sel ? SystemColors.HighlightText : SystemColors.WindowText) : Aero.Ink;
                int iconX = Compact ? (row.X + (row.Width - 16) / 2) : row.X + 7;
                Draw.Glyph(g, new Rectangle(iconX, row.Y + 4, 16, 16), en.Icon, itemInk);

                if (!Compact)
                {
                    TextRenderer.DrawText(g, en.Text, sel ? Aero.BodySemibold : Aero.Body,
                        new Rectangle(row.X + 28, row.Y, row.Width - 32, row.Height), itemInk,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                        TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                        TextFormatFlags.EndEllipsis);
                }

                if (sel && _focus)
                    Draw.FocusRing(g, row, Aero.RadiusControl);
            }

            using (Pen p = new Pen(AeroEnv.HighContrast
                ? SystemColors.WindowFrame : Color.FromArgb(87, 28, 60, 92)))
                g.DrawLine(p, Width - 1, 0, Width - 1, Height);
        }
    }

    internal class AeroCommandBar : Panel
    {
        private readonly List<AeroButton> _buttons = new List<AeroButton>();
        private readonly AeroButton _more;
        private ContextMenuStrip _menu;
        private string _titleText = "";
        private string _hintText = "";
        private Rectangle _titleRect;
        private Rectangle _hintRect;

        public AeroCommandBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 36;
            BackColor = Theme.Bg;
            Padding = new Padding(0);
            AccessibleRole = AccessibleRole.ToolBar;

            _more = new AeroButton(L.T("More"));
            _more.Glyph = "menu";
            _more.Size = new Size(66, Aero.ButtonH);
            _more.Visible = false;
            _more.Click += delegate { ShowOverflow(); };
            Controls.Add(_more);

            Resize += delegate { LayoutBar(); };
        }

        public void SetTitle(string title, string hint)
        {
            _titleText = title ?? "";
            _hintText = hint ?? "";
            AccessibleName = _titleText;
            LayoutBar();
        }

        public void SetHint(string hint)
        {
            _hintText = hint ?? "";
            LayoutBar();
        }

        public void AddButton(AeroButton b)
        {
            _buttons.Add(b);
            Controls.Add(b);
            b.BringToFront();
            _more.BringToFront();
            LayoutBar();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color top = AeroEnv.HighContrast ? SystemColors.Control : Aero.ToolbarTop;
            Color bottom = AeroEnv.HighContrast ? SystemColors.Control : Aero.ToolbarBottom;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), top, bottom, 90f))
                g.FillRectangle(b, ClientRectangle);
            using (Pen p = new Pen(AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.ToolbarLine))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);

            TextFormatFlags ta = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                 TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                                 TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            if (_titleRect.Width > 8)
                TextRenderer.DrawText(g, _titleText, Aero.Heading, _titleRect,
                    AeroEnv.HighContrast ? SystemColors.WindowText : Aero.Ink, ta);
            if (_hintRect.Width > 8)
                TextRenderer.DrawText(g, _hintText, Aero.Small, _hintRect,
                    AeroEnv.HighContrast ? SystemColors.GrayText : Aero.InkSoft, ta);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutBar();
        }

        private void LayoutBar()
        {
            if (_more == null) return;
            int pad = 8;
            int right = Width - pad;
            int visible = 0;

            int minTitle = Width < 560 ? 44 : 140;
            int budget = Width - pad * 2 - minTitle;

            int used = 0;
            for (int i = 0; i < _buttons.Count; i++)
            {
                AeroButton b = _buttons[i];
                bool show = true;
                if (used + b.Width + 6 > budget && visible > 0) show = false;
                b.Visible = show;
                if (show)
                {
                    used += b.Width + 6;
                    visible++;
                    b.SurroundColor = Draw.Mix(Aero.ToolbarTop, Aero.ToolbarBottom, 0.5f);
                    b.SetBounds(right - b.Width, (Height - Aero.ButtonH) / 2, b.Width, Aero.ButtonH);
                    right -= b.Width + 6;
                }
            }

            bool overflow = visible < _buttons.Count;
            _more.Visible = overflow;
            if (overflow)
            {
                _more.SurroundColor = Draw.Mix(Aero.ToolbarTop, Aero.ToolbarBottom, 0.5f);
                _more.SetBounds(right - _more.Width, (Height - Aero.ButtonH) / 2, _more.Width, Aero.ButtonH);
                right -= _more.Width + 6;
                BuildMenu();
            }
            int titleW = Math.Max(0, right - pad - 8);
            int measured = TextRenderer.MeasureText(_titleText, Aero.Heading,
                new Size(400, 22), TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width + 4;
            int showTitleW = Math.Min(Math.Max(0, measured), titleW);
            bool showHint = _hintText.Length > 0 && titleW - showTitleW > 140;
            _titleRect = new Rectangle(pad, 0, showHint ? showTitleW : titleW, Height);
            _hintRect = showHint
                ? new Rectangle(pad + showTitleW + 10, 0, Math.Max(0, titleW - showTitleW - 10), Height)
                : Rectangle.Empty;
            Invalidate();
        }

        private void BuildMenu()
        {
            if (_menu != null) { _menu.Dispose(); _menu = null; }
            _menu = new ContextMenuStrip();
            _menu.Renderer = new AeroMenuRenderer();
            _menu.ShowImageMargin = false;
            _menu.ShowCheckMargin = false;
            _menu.Font = Aero.Body;
            _menu.BackColor = Aero.MenuBg;
            for (int i = 0; i < _buttons.Count; i++)
            {
                AeroButton b = _buttons[i];
                if (b.Visible) continue;
                AeroButton captured = b;
                ToolStripMenuItem item = new ToolStripMenuItem(b.Text);
                item.Font = Aero.Body;
                item.ForeColor = Theme.Text;
                item.AutoSize = false;
                item.Height = Aero.RowH;
                item.Click += delegate { captured.PerformClick(); };
                _menu.Items.Add(item);
            }
            int w = 120;
            for (int i = 0; i < _menu.Items.Count; i++)
                w = Math.Max(w, TextRenderer.MeasureText(_menu.Items[i].Text, Aero.Body).Width + 34);
            for (int i = 0; i < _menu.Items.Count; i++) _menu.Items[i].Width = w;
        }

        private void ShowOverflow()
        {
            if (_menu == null) return;
            _menu.Show(_more, new Point(0, _more.Height));
        }
    }

    internal class AeroMenuRenderer : ToolStripRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(AeroEnv.HighContrast ? SystemColors.Window : Aero.MenuBg);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen p = new Pen(AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.PanelLine))
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle r = new Rectangle(1, 0, e.Item.Width - 2, e.Item.Height);
            if (e.Item.Selected)
            {
                Draw.FillVGrad(e.Graphics, r, Aero.RadiusControl,
                    Aero.Selection[0], Aero.Selection[1], Aero.Selection[2]);
                Draw.StrokeRound(e.Graphics, r, Aero.RadiusControl, Aero.SelectionEdge);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Aero.Ink : Aero.Disabled;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (Pen p = new Pen(Aero.PanelLine))
                e.Graphics.DrawLine(p, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
        }
    }

    internal class AeroStatusBar : Control
    {
        private string _left = "";
        private string _right = "";
        private bool _hasProgress;
        private double _progress;

        public AeroStatusBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = Aero.StatusH;
            BackColor = Theme.Bg;
            AccessibleRole = AccessibleRole.StatusBar;
        }

        public string LeftText
        {
            get { return _left; }
            set { _left = value ?? ""; Invalidate(); }
        }

        public string RightText
        {
            get { return _right; }
            set { _right = value ?? ""; Invalidate(); }
        }

        public void SetProgress(double v)
        {
            _hasProgress = true;
            _progress = Math.Max(0, Math.Min(1, v));
            Invalidate();
        }

        public void ClearProgress()
        {
            _hasProgress = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Color top = AeroEnv.HighContrast ? SystemColors.Control : Aero.StatusTop;
            Color bottom = AeroEnv.HighContrast ? SystemColors.Control : Aero.StatusBottom;
            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), top, bottom, 90f))
                g.FillRectangle(b, ClientRectangle);
            using (Pen p = new Pen(AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.PanelLine))
                g.DrawLine(p, 0, 0, Width, 0);

            Color ink = AeroEnv.HighContrast ? SystemColors.WindowText : Aero.InkSoft;
            TextRenderer.DrawText(g, _left, Aero.Small,
                new Rectangle(8, 0, Math.Max(0, Width - 240), Height), ink,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            int rx = Width - 8;
            Size rs = TextRenderer.MeasureText(g, _right, Aero.Small, new Size(400, 20),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(g, _right, Aero.Small,
                new Rectangle(Math.Max(8, rx - rs.Width), 0, Math.Min(rs.Width, Width - 16), Height), ink,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            rx -= rs.Width + 10;

            if (_hasProgress && Width > 420)
            {
                Rectangle bar = new Rectangle(Math.Max(8, rx - 150), (Height - 14) / 2, 142, 14);
                AeroProgress.PaintBar(g, bar, _progress, Aero.Success, true);
            }
        }
    }

    internal class AeroConsole : Control
    {
        private const int MaxLines = 300;
        private readonly List<string> _lines = new List<string>();
        private readonly List<int> _levels = new List<int>();
        private readonly VScrollBar _scroll;
        private int _top;
        private int _lineH;
        private bool _focus;

        public AeroConsole()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            BackColor = Theme.Surface;
            Font = Aero.Mono;
            TabStop = true;
            AccessibleRole = AccessibleRole.Text;
            AccessibleName = L.T("Activity log");
            _lineH = Font.Height + 3;

            _scroll = new VScrollBar();
            _scroll.Dock = DockStyle.Right;
            _scroll.Width = 15;
            _scroll.SmallChange = 1;
            _scroll.Scroll += delegate { _top = _scroll.Value; Invalidate(); };
            Controls.Add(_scroll);
            UpdateScroll();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DarkMode.Apply(this, Aero.Dark);
        }

        public void Append(string text)
        {
            if (text == null) return;
            string[] parts = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int i = 0; i < parts.Length; i++)
            {
                string line = parts[i];
                if (line.Length == 0 && i == parts.Length - 1) continue;
                int level = 0;
                if (line.IndexOf("Hata", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("hata:", StringComparison.OrdinalIgnoreCase) >= 0) level = 2;
                else if (line.IndexOf("Uyarı", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         line.IndexOf("yönetici", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         line.IndexOf("Warning", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         line.IndexOf("administrator", StringComparison.OrdinalIgnoreCase) >= 0) level = 1;
                _lines.Add(line);
                _levels.Add(level);
            }
            while (_lines.Count > MaxLines)
            {
                _lines.RemoveAt(0);
                _levels.RemoveAt(0);
            }
            StickToBottom();
            Invalidate();
        }

        public void Clear()
        {
            _lines.Clear();
            _levels.Clear();
            _top = 0;
            UpdateScroll();
            Invalidate();
        }

        private int VisibleLines { get { return Math.Max(1, (Height - 4) / _lineH); } }

        private void StickToBottom()
        {
            int max = Math.Max(0, _lines.Count - VisibleLines);
            _top = max;
            UpdateScroll();
        }

        private void UpdateScroll()
        {
            int max = Math.Max(0, _lines.Count - VisibleLines);
            _scroll.Maximum = Math.Max(0, _lines.Count - 1);
            _scroll.LargeChange = VisibleLines;
            _scroll.Enabled = max > 0;
            _scroll.Value = Math.Max(0, Math.Min(_top, Math.Max(0, _scroll.Maximum - _scroll.LargeChange + 1)));
            _scroll.Visible = max > 0;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScroll();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int max = Math.Max(0, _lines.Count - VisibleLines);
            _top = Math.Max(0, Math.Min(max, _top - Math.Sign(e.Delta) * 3));
            UpdateScroll();
            Invalidate();
            base.OnMouseWheel(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Focus();
            base.OnMouseDown(e);
        }

        protected override void OnGotFocus(EventArgs e) { _focus = true; Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { _focus = false; Invalidate(); base.OnLostFocus(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int max = Math.Max(0, _lines.Count - VisibleLines);
            if (e.KeyCode == Keys.PageUp) _top = Math.Max(0, _top - VisibleLines);
            else if (e.KeyCode == Keys.PageDown) _top = Math.Min(max, _top + VisibleLines);
            else if (e.KeyCode == Keys.Home) _top = 0;
            else if (e.KeyCode == Keys.End) _top = max;
            else base.OnKeyDown(e);
            UpdateScroll();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(AeroEnv.HighContrast ? SystemColors.Window : Aero.ConsoleBg);
            g.SmoothingMode = SmoothingMode.None;

            int w = Width - (_scroll.Visible ? _scroll.Width : 0);
            for (int i = 0; i < VisibleLines; i++)
            {
                int idx = _top + i;
                if (idx >= _lines.Count) break;
                Color c = AeroEnv.HighContrast ? SystemColors.WindowText
                    : (_levels[idx] == 2 ? Aero.DangerText : (_levels[idx] == 1 ? Aero.WarningText : Aero.Ink));
                TextRenderer.DrawText(g, _lines[idx], Font,
                    new Rectangle(6, 2 + i * _lineH, Math.Max(0, w - 10), _lineH), c,
                    TextFormatFlags.Left | TextFormatFlags.NoPadding |
                    TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
            if (_lines.Count == 0)
            {
                TextRenderer.DrawText(g, L.T("Log is empty."), Aero.Small,
                    new Rectangle(6, 4, Math.Max(0, w - 12), 20),
                    AeroEnv.HighContrast ? SystemColors.GrayText : Aero.InkSoft,
                    TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            }
            if (_focus)
                Draw.FocusRing(g, new Rectangle(0, 0, Width, Height), Aero.RadiusControl);
        }
    }
}
