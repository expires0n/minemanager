using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Aero on/off switch: 3 px rounded track, gloss, blue when on, focus ring,
    /// Space toggles. Colours are read at paint time so the light and dark
    /// palettes both work after a theme switch (controls are rebuilt on theme
    /// change, but colours are still resolved per paint).
    /// </summary>
    internal class AeroToggle : Control
    {
        private const int TrackW = 30;
        private const int TrackH = 13;

        private bool _checked;
        private bool _hover;
        private bool _focus;
        private bool _indent;

        public AeroToggle(string text)
        {
            Text = text;
            Height = Aero.RowH;
            Font = Aero.Body;
            BackColor = Theme.Surface;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleName = text;
        }

        /// <summary>Switch state. Setting it raises <see cref="CheckedChanged"/>.</summary>
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler CheckedChanged;

        /// <summary>Draws a tree connector so preference rows read as a child list.</summary>
        public bool Indent
        {
            get { return _indent; }
            set
            {
                if (_indent == value) return;
                _indent = value;
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            AccessibleName = Text;
            Invalidate();
        }

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
        protected override void OnEnabledChanged(EventArgs e) { _hover = false; Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                if (Enabled) Checked = !Checked;
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            base.OnKeyDown(e);
        }

        /// <summary>Left edge of the track, honouring the indent connector.</summary>
        private int TrackX { get { return _indent ? 12 : 2; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool on = Enabled;
            int tx = TrackX;
            Rectangle track = new Rectangle(tx, (Height - TrackH) / 2, TrackW, TrackH);

            // ------------------------------------------------- tree connector
            if (_indent)
            {
                using (Pen p = new Pen(Color.FromArgb(AeroEnv.HighContrast ? 255 : 150,
                    Theme.Line.R, Theme.Line.G, Theme.Line.B)))
                {
                    g.DrawLine(p, 5, 0, 5, Height);
                    g.DrawLine(p, 5, Height / 2, tx - 2, Height / 2);
                }
            }

            if (AeroEnv.HighContrast)
            {
                using (SolidBrush b = new SolidBrush(_checked ? SystemColors.Highlight : SystemColors.Window))
                    g.FillRectangle(b, track);
                using (Pen p = new Pen(SystemColors.WindowFrame))
                    g.DrawRectangle(p, track.X, track.Y, track.Width - 1, track.Height - 1);
                if (_checked) Draw.Glyph(g, track, "check", SystemColors.WindowText);
                PaintLabel(g, track.Right + 8);
                if (_focus) Draw.FocusRing(g, new Rectangle(0, 1, Width - 1, Height - 3), Aero.RadiusControl);
                return;
            }

            // ------------------------------------------------------------ track
            if (_checked)
            {
                Draw.FillVGrad(g, track, Aero.RadiusControl, Aero.Blue400, Aero.Blue600);
                Draw.SheenTopHalf(g, track, Aero.RadiusControl, 150);
                Draw.StrokeRound(g, track, Aero.RadiusControl,
                    !on ? Aero.DisabledBorder : Aero.Blue700);
                // inner top shadow keeps the rail readable like the rest of the Aero chrome
                using (Pen p = new Pen(Color.FromArgb(46, 12, 60, 96)))
                    g.DrawLine(p, track.X + 2, track.Y + 1, track.Right - 3, track.Y + 1);
            }
            else
            {
                using (SolidBrush b = new SolidBrush(on ? Aero.Field : Aero.DisabledFace))
                    g.FillPath(b, Draw.Round(track, Aero.RadiusControl));
                using (SolidBrush b = new SolidBrush(Color.FromArgb(22, 0, 0, 0)))
                    g.FillPath(b, Draw.Round(new Rectangle(track.X + 1, track.Y + 1,
                        track.Width - 2, 4), Aero.RadiusControl));
                Draw.StrokeRound(g, track, Aero.RadiusControl,
                    !on ? Aero.DisabledBorder : (_hover ? Aero.Blue500 : Aero.FieldBorder));
            }

            // ------------------------------------------------------------- knob
            int d = TrackH - 4;
            int kx = _checked ? track.Right - d - 2 : track.Left + 2;
            int ky = track.Top + 2;
            using (SolidBrush b = new SolidBrush(Color.FromArgb(on ? 44 : 26, 0, 0, 0)))
                g.FillEllipse(b, kx, ky + 1, d, d);
            using (LinearGradientBrush b = Draw.VGrad(new Rectangle(kx, ky, d, d),
                Aero.White, Draw.Mix(Aero.White, Aero.Chrome, 0.75f)))
                g.FillEllipse(b, kx, ky, d, d);
            using (Pen p = new Pen(on ? Aero.Blue800 : Aero.ButtonBorder, 1f))
                g.DrawEllipse(p, kx, ky, d - 1, d - 1);

            PaintLabel(g, track.Right + 8);
            if (_focus && on)
                Draw.FocusRing(g, new Rectangle(0, 1, Width - 1, Height - 3), Aero.RadiusControl);
        }

        private void PaintLabel(Graphics g, int textX)
        {
            int w = Math.Max(0, Width - textX - 2);
            if (w < 8) return;
            TextRenderer.DrawText(g, Text, Font,
                new Rectangle(textX, 0, w, Height),
                Enabled ? (AeroEnv.HighContrast ? SystemColors.WindowText : Aero.Ink) : Aero.Disabled,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        }
    }
}
