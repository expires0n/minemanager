using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MineManager
{
    internal static class Shell
    {
        public static Form MainForm;
        public static AeroStatusBar Status;

        public static void SetProgress(double v)
        {
            if (Status != null) Status.SetProgress(v);
        }

        public static void ClearProgress()
        {
            if (Status != null) Status.ClearProgress();
        }

        public static void Toast(string message, Color tone)
        {
            AeroToast.Show(MainForm, message, tone);
        }
    }

    internal class AeroDialog : Form
    {
        private readonly AeroButton _ok;
        private readonly AeroButton _cancel;
        private readonly AeroButton _defaultButton;
        private readonly string _title;
        private readonly string _icon;
        private readonly bool _destructive;
        private bool _closeHot;

        private AeroDialog(string title, string message, string okText, string cancelText,
            string icon, bool destructive)
        {
            _title = title;
            _icon = icon;
            _destructive = destructive;
            Text = title;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Theme.Bg;
            Font = Aero.Body;
            DoubleBuffered = true;
            KeyPreview = true;
            AccessibleName = title;
            AccessibleRole = AccessibleRole.Dialog;

            int messageW = 340;
            Size ms = TextRenderer.MeasureText(message, Aero.Body,
                new Size(messageW, 2000), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            int textH = Math.Max(36, ms.Height);
            int w = 4 + 18 + 34 + 14 + messageW + 18 + 4;
            int h = 34 + 14 + Math.Max(34, textH) + 16 + Aero.ButtonH + 12 + 4;

            ClientSize = new Size(w, h);
            MinimumSize = new Size(w, h);
            MaximumSize = new Size(w, h);
            ShowIcon = false;

            Label msg = Ui.Lbl(message, Aero.Body, Theme.Text);
            msg.BackColor = Theme.Bg;   // flat body surface, never the glass frame
            msg.AutoSize = false;
            msg.UseMnemonic = false;
            msg.SetBounds(4 + 18 + 34 + 14, 34 + 14, messageW, textH);
            Controls.Add(msg);

            int okW = Math.Max(78, TextRenderer.MeasureText(okText, Aero.Body).Width + 26);
            _ok = new AeroButton(okText);
            _ok.Default = !destructive;
            _ok.Size = new Size(okW, Aero.ButtonH);
            _ok.SurroundColor = Theme.Bg;
            _ok.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            Controls.Add(_ok);

            if (!string.IsNullOrEmpty(cancelText))
            {
                int cw = Math.Max(78, TextRenderer.MeasureText(cancelText, Aero.Body).Width + 26);
                _cancel = new AeroButton(cancelText);
                _cancel.Size = new Size(cw, Aero.ButtonH);
                _cancel.SurroundColor = Theme.Bg;
                _cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
                Controls.Add(_cancel);
            }

            _defaultButton = destructive && _cancel != null ? _cancel : _ok;
            LayoutButtons();

            MouseMove += delegate(object s, MouseEventArgs e) { OnTitleMouseMove(e); };
            MouseDown += delegate(object s, MouseEventArgs e) { OnTitleMouseDown(e); };
            MouseLeave += delegate { if (_closeHot) { _closeHot = false; Invalidate(); } };

            _ok.TabIndex = 0;
            if (_cancel != null) _cancel.TabIndex = 1;
        }

        private void LayoutButtons()
        {
            int right = ClientSize.Width - 4 - 12;
            int y = ClientSize.Height - 4 - 12 - Aero.ButtonH;
            _ok.Location = new Point(right - _ok.Width, y);
            right -= _ok.Width + 8;
            if (_cancel != null)
            {
                _cancel.Location = new Point(right - _cancel.Width, y);
                _cancel.SurroundColor = Theme.Bg;
            }
        }

        private Rectangle CloseRect
        {
            get { return new Rectangle(ClientSize.Width - 4 - 6 - Aero.CaptionW, 4 + 6, Aero.CaptionW, Aero.CaptionH); }
        }

        private void OnTitleMouseMove(MouseEventArgs e)
        {
            bool hot = CloseRect.Contains(e.Location);
            if (hot != _closeHot) { _closeHot = hot; Invalidate(CloseRect); Invalidate(new Rectangle(CloseRect.X - 2, 0, CloseRect.Width + 4, 34)); }
        }

        private void OnTitleMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (CloseRect.Contains(e.Location))
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return;
            }
            if (e.Y < 34)
            {
                Native.ReleaseCapture();
                Native.SendMessage(Handle, Native.WM_NCLBUTTONDOWN, (IntPtr)Native.HTCAPTION, IntPtr.Zero);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            if (!AeroEnv.HighContrast)
            {
                using (LinearGradientBrush bg = Draw.VGrad(r, Aero.WindowTop, Aero.WindowMid, Aero.WindowBottom))
                    g.FillRectangle(bg, r);
            }
            else g.Clear(Theme.Bg);
            Draw.Glass(g, r, Aero.RadiusWindow, true);
            Draw.Glass(g, new Rectangle(0, 0, r.Width, 34), Aero.RadiusWindow, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle body = new Rectangle(4, 34, ClientSize.Width - 8, ClientSize.Height - 38);
            using (SolidBrush b = new SolidBrush(Theme.Bg))
                g.FillRectangle(b, body);
            using (Pen p = new Pen(AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.PanelLine))
                g.DrawRectangle(p, body.X, body.Y, body.Width - 1, body.Height - 1);

            // title bar: app mark + text
            Bitmap brand = AeroLogo.Image;
            if (brand != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(brand, new Rectangle(12, 9, 16, 16));
            }
            else Draw.Glyph(g, new Rectangle(14, 12, 12, 12), "shield", Theme.Text);
            TextRenderer.DrawText(g, _title, Aero.BodySemibold,
                new Rectangle(32, 4, Math.Max(20, ClientSize.Width - 32 - Aero.CaptionW - 20), 30),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);

            // close caption
            Rectangle c = CloseRect;
            if (AeroEnv.HighContrast)
            {
                using (SolidBrush b = new SolidBrush(_closeHot ? SystemColors.Highlight : SystemColors.Control))
                    g.FillRectangle(b, c);
                using (Pen p = new Pen(SystemColors.WindowFrame))
                    g.DrawRectangle(p, c.X, c.Y, c.Width - 1, c.Height - 1);
                Draw.Glyph(g, c, "close", _closeHot ? SystemColors.HighlightText : SystemColors.WindowText);
            }
            else if (_closeHot)
            {
                Draw.FillVGrad(g, c, Aero.RadiusControl,
                    Draw.Mix(Aero.Danger, Aero.White, 0.28f),
                    Draw.Mix(Aero.Danger, Aero.Black, 0.15f));
                Draw.StrokeRound(g, c, Aero.RadiusControl, Draw.Mix(Aero.Danger, Aero.Black, 0.2f));
            }
            else
            {
                Color face = Aero.Dark ? Aero.GlossFace[0] : Draw.Mix(Aero.White, Aero.Blue200, 0.25f);
                Draw.FillVGrad(g, c, Aero.RadiusControl, Aero.Dark ? Aero.GlossFace[1] : Aero.White, face);
                Draw.StrokeRound(g, c, Aero.RadiusControl, Aero.FieldBorder);
            }
            Draw.Glyph(g, c, "close", _closeHot ? Aero.White : Theme.Text);

            // button strip separator
            int stripY = ClientSize.Height - 4 - 12 - Aero.ButtonH - 10;
            using (Pen p = new Pen(AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.PanelLine))
                g.DrawLine(p, 4, stripY, ClientSize.Width - 5, stripY);
            if (!AeroEnv.HighContrast)
                using (Pen p = new Pen(Color.FromArgb(120, 255, 255, 255)))
                    g.DrawLine(p, 5, stripY + 1, ClientSize.Width - 6, stripY + 1);

            // body icon
            Rectangle ir = new Rectangle(4 + 18, 34 + 14, 32, 32);
            if (AeroEnv.HighContrast)
            {
                Draw.Glyph(g, ir, _icon, SystemColors.WindowText);
            }
            else if (_icon == "warn")
            {
                PointF[] tri = new PointF[]
                {
                    new PointF(ir.X + ir.Width / 2f, ir.Y + 2),
                    new PointF(ir.Right - 1, ir.Bottom - 2),
                    new PointF(ir.X + 1, ir.Bottom - 2)
                };
                using (LinearGradientBrush b = Draw.VGrad(ir,
                    Draw.Mix(Aero.Warning, Aero.White, 0.45f), Draw.Mix(Aero.Warning, Aero.Black, 0.1f)))
                    g.FillPolygon(b, tri);
                using (Pen p = new Pen(Draw.Mix(Aero.Warning, Aero.Black, 0.3f)))
                    g.DrawPolygon(p, tri);
                using (Pen p = new Pen(Aero.Ink, 2.2f))
                {
                    g.DrawLine(p, ir.X + 16, ir.Y + 11, ir.X + 16, ir.Y + 21);
                    g.DrawLine(p, ir.X + 16, ir.Y + 25, ir.X + 16, ir.Y + 27);
                }
            }
            else if (_icon == "danger")
            {
                Draw.FillVGrad(g, ir, 16, Draw.Mix(Aero.Danger, Aero.White, 0.3f),
                    Draw.Mix(Aero.Danger, Aero.Black, 0.12f));
                using (Pen p = new Pen(Aero.White, 2.4f))
                {
                    g.DrawLine(p, ir.X + 11, ir.Y + 11, ir.Right - 11, ir.Bottom - 11);
                    g.DrawLine(p, ir.Right - 11, ir.Y + 11, ir.X + 11, ir.Bottom - 11);
                }
            }
            else
            {
                Draw.FillVGrad(g, ir, 16, Draw.Mix(Aero.Blue500, Aero.White, 0.4f), Aero.Blue700);
                using (SolidBrush b = new SolidBrush(Aero.White))
                {
                    g.FillEllipse(b, ir.X + 14, ir.Y + 7, 4, 4);
                    g.FillRectangle(b, ir.X + 14, ir.Y + 14, 4, 11);
                }
            }

            base.OnPaint(e);
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
                if (_defaultButton != null) _defaultButton.PerformClick();
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_defaultButton != null) _defaultButton.Focus();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            try
            {
                using (GraphicsPath p = Draw.Round(new Rectangle(0, 0, Width, Height), Aero.RadiusWindow))
                {
                    Region old = Region;
                    Region = new Region(p);
                    if (old != null) old.Dispose();
                }
            }
            catch { }
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

        public static DialogResult Show(Form owner, string title, string message,
            string okText, string cancelText, string icon)
        {
            return Show(owner, title, message, okText, cancelText, icon, false);
        }

        public static DialogResult Show(Form owner, string title, string message,
            string okText, string cancelText, string icon, bool destructive)
        {
            using (AeroDialog d = new AeroDialog(title, message, okText, cancelText, icon, destructive))
                return owner != null ? d.ShowDialog(owner) : d.ShowDialog();
        }

        public static bool Confirm(Form owner, string title, string message,
            string okText, string cancelText, string icon, bool destructive)
        {
            return Show(owner, title, message, okText, cancelText, icon, destructive) == DialogResult.OK;
        }

        public static void Info(Form owner, string title, string message, string icon)
        {
            Show(owner, title, message, L.T("OK"), null, icon);
        }
    }

    internal class AeroToast : Form
    {
        private static AeroToast _current;
        private static Form _owner;

        private readonly string _message;
        private readonly Color _tone;
        private readonly Timer _life;
        private readonly Timer _fade;
        private int _fadeStep;

        private AeroToast(string message, Color tone)
        {
            _message = message;
            _tone = tone;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Theme.Bg;
            Font = Aero.Body;
            DoubleBuffered = true;
            Opacity = AeroEnv.ReducedMotion ? 1.0 : 0.0;
            AccessibleName = L.T("Notification");
            AccessibleRole = AccessibleRole.Alert;

            Size ms = TextRenderer.MeasureText(message, Aero.Body,
                new Size(260, 200), TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            int w = 300;
            int h = Math.Max(46, 22 + ms.Height + 10);
            ClientSize = new Size(w, h);

            _life = new Timer();
            _life.Interval = 4000;
            _life.Tick += delegate { CloseToast(); };

            _fade = new Timer();
            _fade.Interval = 16;
            _fade.Tick += delegate
            {
                _fadeStep++;
                Opacity = Math.Min(1.0, _fadeStep / 10.0);
                if (Opacity >= 1.0) _fade.Stop();
            };

            MouseEnter += delegate { _life.Stop(); };
            MouseLeave += delegate { _life.Start(); };
            MouseClick += delegate { CloseToast(); };
            FormClosed += delegate { if (_current == this) _current = null; };
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000;   // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080;   // WS_EX_TOOLWINDOW
                cp.ClassStyle |= 0x20000;   // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle r = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
            if (!AeroEnv.HighContrast)
            {
                using (LinearGradientBrush bg = Draw.VGrad(r, Aero.WindowTop, Aero.WindowMid, Aero.WindowBottom))
                    g.FillRectangle(bg, r);
            }
            else g.Clear(Theme.Bg);
            Draw.Glass(g, r, 6, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle icon = new Rectangle(12, (ClientSize.Height - 16) / 2, 16, 16);
            string glyph = "info";
            if (_tone == Aero.Success) glyph = "check";
            else if (_tone == Aero.Warning) glyph = "warn";
            else if (_tone == Aero.Danger) glyph = "warn";
            Color glyphColor = _tone;
            if (_tone == Aero.Success) glyphColor = Aero.SuccessText;
            else if (_tone == Aero.Warning) glyphColor = Aero.WarningText;
            else if (_tone == Aero.Danger) glyphColor = Aero.DangerText;
            Draw.Glyph(g, icon, glyph, AeroEnv.HighContrast ? SystemColors.WindowText : glyphColor);
            TextRenderer.DrawText(g, _message, Aero.Body,
                new Rectangle(38, 8, ClientSize.Width - 50, ClientSize.Height - 14), Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            base.OnPaint(e);
        }

        private void CloseToast()
        {
            _life.Stop();
            _fade.Stop();
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Place();
            _life.Start();
            if (!AeroEnv.ReducedMotion) _fade.Start();
        }

        private void Place()
        {
            Rectangle area;
            if (_owner != null && _owner.Visible)
                area = _owner.RectangleToScreen(_owner.ClientRectangle);
            else
                area = Screen.PrimaryScreen.WorkingArea;
            int x = area.Right - Width - 6;
            int y = area.Top + Aero.TitleBarH + 4 + 6;
            Location = new Point(x, y);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            try
            {
                using (GraphicsPath p = Draw.Round(new Rectangle(0, 0, Width, Height), 6))
                {
                    Region old = Region;
                    Region = new Region(p);
                    if (old != null) old.Dispose();
                }
            }
            catch { }
        }

        public static void Show(Form owner, string message, Color tone)
        {
            try
            {
                if (_current != null && !_current.IsDisposed)
                {
                    _current._life.Stop();
                    _current._fade.Stop();
                    _current.Close();
                    _current = null;
                }
                _owner = owner;
                AeroToast t = new AeroToast(message, tone);
                t.Owner = owner;
                _current = t;
                t.Show();
                t.BringToFront();
            }
            catch { }
        }
    }
}
