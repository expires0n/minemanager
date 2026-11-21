using System;
using System.Drawing;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Thin Aero scrollbar used where a ScrollableControl's native non-client
    /// scrollbar cannot be themed (the Hardware spec panel). The native bar is
    /// hidden and this control drives AutoScrollPosition instead.
    /// </summary>
    internal class AeroScrollBar : Control
    {
        private int _max;
        private int _value;
        private int _large = 100;
        private bool _drag;
        private int _dragOffset;
        private bool _hover;

        public event EventHandler ValueChanged;

        public AeroScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.Selectable, true);
            Width = 14;
            BackColor = Theme.Bg;
            TabStop = true;
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.ScrollBar;
            AccessibleName = L.T("Scroll");
        }

        public int Maximum
        {
            get { return _max; }
            set
            {
                int v = Math.Max(0, value);
                if (_max == v) return;
                _max = v;
                if (_value > _max) _value = _max;
                Invalidate();
            }
        }

        public int Value
        {
            get { return _value; }
            set
            {
                int v = Math.Max(0, Math.Min(_max, value));
                if (_value == v) return;
                _value = v;
                Invalidate();
                if (ValueChanged != null) ValueChanged(this, EventArgs.Empty);
            }
        }

        public int LargeChange
        {
            get { return _large; }
            set { _large = Math.Max(1, value); Invalidate(); }
        }

        private int ThumbHeight()
        {
            if (_max <= 0) return Math.Max(0, Height - 4);
            int total = _max + _large;
            if (total <= 0) return Math.Max(0, Height - 4);
            int h = (int)((double)_large / total * (Height - 4));
            return Math.Max(28, Math.Min(Height - 4, h));
        }

        private int ThumbTop(int thumbH)
        {
            int travel = Height - 4 - thumbH;
            if (_max <= 0 || travel <= 0) return 2;
            return 2 + (int)Math.Round((double)_value / _max * travel);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(AeroEnv.HighContrast ? SystemColors.ScrollBar : Theme.Bg);
            using (Pen edge = new Pen(AeroEnv.HighContrast
                ? SystemColors.WindowFrame : Aero.PanelLine))
                g.DrawLine(edge, 0, 0, 0, Height);

            int thumbH = ThumbHeight();
            if (thumbH <= 0) return;
            int y = ThumbTop(thumbH);
            Rectangle r = new Rectangle(2, y, Math.Max(6, Width - 5), thumbH);
            Color[] face = _hover ? Aero.GlossHover : Aero.GlossFace;
            if (AeroEnv.HighContrast)
            {
                using (SolidBrush b = new SolidBrush(SystemColors.ScrollBar))
                    g.FillRectangle(b, r);
                using (Pen p = new Pen(SystemColors.WindowFrame))
                    g.DrawRectangle(p, r);
            }
            else
            {
                Draw.FillVGrad(g, r, Aero.RadiusControl, face[0], face[3]);
                using (Pen p = new Pen(Draw.Mix(Aero.PanelLine, Aero.Blue400, _hover ? 0.65f : 0.35f)))
                    Draw.StrokeRound(g, r, Aero.RadiusControl, p.Color);
            }
            if (Focused) Draw.FocusRing(g, r, Aero.RadiusControl);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            int thumbH = ThumbHeight();
            int y = ThumbTop(thumbH);
            if (e.Y >= y && e.Y <= y + thumbH)
            {
                _drag = true;
                _dragOffset = e.Y - y;
                Capture = true;
            }
            else
            {
                Value += e.Y < y ? -_large : _large;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_drag) return;
            int thumbH = ThumbHeight();
            int travel = Height - 4 - thumbH;
            if (travel <= 0 || _max <= 0) return;
            int top = Math.Max(2, Math.Min(2 + travel, e.Y - _dragOffset));
            Value = (int)Math.Round((double)(top - 2) / travel * _max);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _drag = false;
            Capture = false;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Value -= (e.Delta / 120) * 3;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down ||
                keyData == Keys.PageUp || keyData == Keys.PageDown ||
                keyData == Keys.Home || keyData == Keys.End) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Up) Value -= 1;
            else if (e.KeyCode == Keys.Down) Value += 1;
            else if (e.KeyCode == Keys.PageUp) Value -= _large;
            else if (e.KeyCode == Keys.PageDown) Value += _large;
            else if (e.KeyCode == Keys.Home) Value = 0;
            else if (e.KeyCode == Keys.End) Value = _max;
            else return;
            e.Handled = true;
        }
    }
}
