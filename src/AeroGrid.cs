using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MineManager
{
    // Aero detail view: 22px header, 24px rows, hover/selection gradients,
    // 1px column separators, keyboard focus ring, empty state.
    internal class AeroGridView : DataGridView
    {
        private int _hoverRow = -1;
        private string _emptyText = "";
        private string _emptyHint = "";

        public AeroGridView()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Draw.DoubleBuffer(this);

            BackgroundColor = Theme.Surface;
            BorderStyle = BorderStyle.None;
            CellBorderStyle = DataGridViewCellBorderStyle.None;
            GridColor = Aero.GridLine;
            EnableHeadersVisualStyles = false;
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            ColumnHeadersDefaultCellStyle.BackColor = Aero.HeaderTop;
            ColumnHeadersDefaultCellStyle.ForeColor = Theme.Dim;
            ColumnHeadersDefaultCellStyle.Font = Aero.SmallSemibold;
            ColumnHeadersDefaultCellStyle.SelectionBackColor = Aero.HeaderTop;
            ColumnHeadersDefaultCellStyle.SelectionForeColor = Theme.Dim;
            ColumnHeadersDefaultCellStyle.Padding = new Padding(0);
            ColumnHeadersHeight = 22;
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            DefaultCellStyle.BackColor = Theme.Surface;
            DefaultCellStyle.ForeColor = Theme.Text;
            DefaultCellStyle.SelectionBackColor = Aero.SelectionBack;
            DefaultCellStyle.SelectionForeColor = Theme.Text;
            DefaultCellStyle.Font = Aero.Body;
            DefaultCellStyle.Padding = new Padding(0);
            DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            RowTemplate.Height = Aero.RowH;

            AllowUserToAddRows = false;
            AllowUserToDeleteRows = false;
            AllowUserToResizeRows = false;
            AllowUserToOrderColumns = false;
            RowHeadersVisible = false;
            SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            MultiSelect = false;
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            ScrollBars = ScrollBars.Both;
            ShowCellToolTips = true;
            StandardTab = true;
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;

            CellMouseEnter += delegate(object s, DataGridViewCellEventArgs e)
            {
                int r = e.RowIndex;
                SetHover(r);
            };
            MouseLeave += delegate { SetHover(-1); };
            CellValueChanged += delegate { Invalidate(); };
            SelectionChanged += delegate { Invalidate(); };
            GotFocus += delegate { Invalidate(); };
            LostFocus += delegate { Invalidate(); };
            Resize += delegate { UpdateScrollBars(); };
            RowsAdded += delegate { UpdateScrollBars(); Invalidate(); };
            RowsRemoved += delegate { UpdateScrollBars(); Invalidate(); };
            Scroll += delegate { Invalidate(); };
        }

        private void UpdateScrollBars()
        {
            int need = 0;
            for (int i = 0; i < Columns.Count; i++)
                need += Math.Max(30, Columns[i].MinimumWidth);
            bool horizontal = Rows.Count > 0 && need > ClientSize.Width - 4;
            ScrollBars mode = horizontal ? ScrollBars.Both : ScrollBars.Vertical;
            if (ScrollBars != mode) ScrollBars = mode;
            DarkMode.Apply(this, Aero.Dark);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DarkMode.Apply(this, Aero.Dark);
        }

        public string EmptyText
        {
            get { return _emptyText; }
            set { _emptyText = value ?? ""; Invalidate(); }
        }

        public string EmptyHint
        {
            get { return _emptyHint; }
            set { _emptyHint = value ?? ""; Invalidate(); }
        }

        private void SetHover(int row)
        {
            if (row == _hoverRow) return;
            int old = _hoverRow;
            _hoverRow = row;
            if (old >= 0 && old < Rows.Count) InvalidateRow(old);
            if (row >= 0 && row < Rows.Count) InvalidateRow(row);
        }

        private Rectangle RowRect(int row)
        {
            Rectangle r = GetRowDisplayRectangle(row, false);
            if (r.Height <= 0) r = new Rectangle(0, 0, ClientSize.Width, RowTemplate.Height);
            r.Width = ClientSize.Width;
            return r;
        }

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            if (e.ColumnIndex < 0) { e.Handled = true; return; }

            if (e.RowIndex == -1)
            {
                PaintHeader(e);
                return;
            }
            if (e.RowIndex >= Rows.Count) { e.Handled = true; return; }
            PaintCell(e);
        }

        private void PaintHeader(DataGridViewCellPaintingEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle cb = e.CellBounds;
            bool hc = AeroEnv.HighContrast;

            using (LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(cb.X, cb.Y, Math.Max(1, cb.Width), Math.Max(1, cb.Height)),
                hc ? SystemColors.Control : Aero.HeaderTop,
                hc ? SystemColors.Control : Aero.HeaderBottom, 90f))
                g.FillRectangle(b, cb);

            using (Pen p = new Pen(hc ? SystemColors.WindowFrame : Aero.PanelLine))
                g.DrawLine(p, cb.X, cb.Bottom - 1, cb.Right, cb.Bottom - 1);
            using (Pen p = new Pen(hc ? SystemColors.WindowFrame : Aero.ToolbarLine))
                g.DrawLine(p, cb.Right - 1, cb.Y + 2, cb.Right - 1, cb.Bottom - 4);

            string text = Convert.ToString(e.FormattedValue);
            if (!string.IsNullOrEmpty(text))
            {
                Rectangle tr = new Rectangle(cb.X + 6, cb.Y, Math.Max(0, cb.Width - 10), cb.Height - 1);
                TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding |
                                        TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis;
                if (e.CellStyle.Alignment == DataGridViewContentAlignment.MiddleRight ||
                    e.CellStyle.Alignment == DataGridViewContentAlignment.TopRight ||
                    e.CellStyle.Alignment == DataGridViewContentAlignment.BottomRight)
                    flags |= TextFormatFlags.Right;
                else if (e.CellStyle.Alignment == DataGridViewContentAlignment.MiddleCenter ||
                         e.CellStyle.Alignment == DataGridViewContentAlignment.TopCenter ||
                         e.CellStyle.Alignment == DataGridViewContentAlignment.BottomCenter)
                    flags |= TextFormatFlags.HorizontalCenter;
                else
                    flags |= TextFormatFlags.Left;

                TextRenderer.DrawText(g, text, Aero.SmallSemibold, tr,
                    hc ? SystemColors.WindowText : Aero.InkSoft, flags);
            }
            e.Handled = true;
        }

        private void PaintCell(DataGridViewCellPaintingEventArgs e)
        {
            Graphics g = e.Graphics;
            Rectangle cb = e.CellBounds;
            Rectangle row = RowRect(e.RowIndex);
            bool selected = (e.State & DataGridViewElementStates.Selected) == DataGridViewElementStates.Selected;
            bool hover = e.RowIndex == _hoverRow;
            bool hc = AeroEnv.HighContrast;

            // ---- background
            if (hc)
            {
                using (SolidBrush b = new SolidBrush(selected ? SystemColors.Highlight : SystemColors.Window))
                    g.FillRectangle(b, cb);
            }
            else if (selected)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new Rectangle(row.X, row.Y, Math.Max(1, row.Width), Math.Max(1, row.Height)),
                    Aero.Selection[0], Aero.Selection[2], 90f))
                {
                    ColorBlend blend = new ColorBlend(3);
                    blend.Colors = Aero.Selection;
                    blend.Positions = new float[] { 0f, 0.52f, 1f };
                    b.InterpolationColors = blend;
                    g.FillRectangle(b, cb);
                }
            }
            else if (hover)
            {
                using (LinearGradientBrush b = new LinearGradientBrush(
                    new Rectangle(row.X, row.Y, Math.Max(1, row.Width), Math.Max(1, row.Height)),
                    Aero.RowHoverTop, Aero.RowHoverBottom, 90f))
                    g.FillRectangle(b, cb);
            }
            else
            {
                using (SolidBrush b = new SolidBrush(hc ? SystemColors.Window : Theme.Surface))
                    g.FillRectangle(b, cb);
            }

            // ---- selection edge (row outline)
            if (selected && !hc)
            {
                using (Pen p = new Pen(Aero.SelectionEdge))
                {
                    g.DrawLine(p, cb.X, row.Y, cb.Right, row.Y);
                    g.DrawLine(p, cb.X, row.Y + row.Height - 1, cb.Right, row.Y + row.Height - 1);
                    if (e.ColumnIndex == 0) g.DrawLine(p, cb.X, row.Y, cb.X, row.Y + row.Height - 1);
                    if (e.ColumnIndex == ColumnCount - 1)
                        g.DrawLine(p, cb.Right - 1, row.Y, cb.Right - 1, row.Y + row.Height - 1);
                }
            }

            // ---- separators
            using (Pen p = new Pen(hc ? SystemColors.WindowFrame : Aero.GridLine))
                g.DrawLine(p, cb.X, cb.Bottom - 1, cb.Right, cb.Bottom - 1);
            if (e.ColumnIndex < ColumnCount - 1)
            {
                using (Pen p = new Pen(hc ? SystemColors.WindowFrame : Aero.GridLine))
                    g.DrawLine(p, cb.Right - 1, cb.Y, cb.Right - 1, cb.Bottom - 1);
            }

            // ---- content
            bool isCheck = Rows[e.RowIndex].Cells[e.ColumnIndex] is DataGridViewCheckBoxCell;
            if (isCheck)
            {
                bool val = e.Value is bool && (bool)e.Value;
                Rectangle box = new Rectangle(cb.X + (cb.Width - 13) / 2, cb.Y + (cb.Height - 13) / 2, 13, 13);
                if (hc)
                {
                    using (SolidBrush b = new SolidBrush(val ? SystemColors.Highlight : SystemColors.Window))
                        g.FillRectangle(b, box);
                    using (Pen p = new Pen(SystemColors.WindowFrame)) g.DrawRectangle(p, box);
                    if (val) Draw.Glyph(g, box, "check", SystemColors.HighlightText);
                }
                else if (val)
                {
                    Draw.FillVGrad(g, box, Aero.RadiusControl, Aero.Blue400, Aero.Blue600);
                    Draw.StrokeRound(g, box, Aero.RadiusControl, Aero.Blue700);
                    Draw.Glyph(g, new Rectangle(box.X + 1, box.Y + 1, box.Width - 2, box.Height - 2), "check", Aero.White);
                }
                else
                {
                    using (SolidBrush b = new SolidBrush(Aero.Field))
                        g.FillRectangle(b, box);
                    Draw.StrokeRound(g, box, Aero.RadiusControl,
                        hover ? Aero.Blue500 : Aero.FieldBorder);
                }
            }
            else
            {
                string text = Convert.ToString(e.FormattedValue);
                if (!string.IsNullOrEmpty(text))
                {
                    Color fore = selected ? (hc ? SystemColors.HighlightText : Aero.Ink)
                        : (hc ? SystemColors.WindowText : e.CellStyle.ForeColor);
                    if (fore == Theme.Surface || fore == Color.White) fore = Aero.Ink;

                    TextFormatFlags flags = TextFormatFlags.VerticalCenter |
                                            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
                                            TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
                    DataGridViewContentAlignment al = e.CellStyle.Alignment;
                    if (al == DataGridViewContentAlignment.MiddleRight ||
                        al == DataGridViewContentAlignment.TopRight ||
                        al == DataGridViewContentAlignment.BottomRight)
                        flags |= TextFormatFlags.Right;
                    else if (al == DataGridViewContentAlignment.MiddleCenter ||
                             al == DataGridViewContentAlignment.TopCenter ||
                             al == DataGridViewContentAlignment.BottomCenter)
                        flags |= TextFormatFlags.HorizontalCenter;
                    else flags |= TextFormatFlags.Left;

                    TextRenderer.DrawText(g, text, e.CellStyle.Font ?? Aero.Body,
                        new Rectangle(cb.X + 6, cb.Y, Math.Max(0, cb.Width - 10), cb.Height), fore, flags);
                }
            }

            // ---- keyboard focus
            if (Focused && CurrentCellAddress.X == e.ColumnIndex && CurrentCellAddress.Y == e.RowIndex)
                Draw.FocusRing(g, new Rectangle(cb.X + 1, cb.Y + 1, cb.Width - 2, cb.Height - 2), 0);

            e.Handled = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Rows.Count == 0 && _emptyText.Length > 0) PaintEmpty(e.Graphics);
        }

        private void PaintEmpty(Graphics g)
        {
            int top = ColumnHeadersHeight;
            int w = ClientSize.Width;
            int h = ClientSize.Height;
            if (w < 40 || h < top + 40) return;

            Rectangle area = new Rectangle(0, top, w, h - top);
            if (area.Width < 20 || area.Height < 20) return;

            using (SolidBrush b = new SolidBrush(AeroEnv.HighContrast ? SystemColors.Window : Theme.Surface))
                g.FillRectangle(b, area);

            if (area.Height < 92)
            {
                TextRenderer.DrawText(g, _emptyText, Aero.Small,
                    new Rectangle(8, area.Y, Math.Max(0, area.Width - 16), Math.Min(area.Height, 24)),
                    AeroEnv.HighContrast ? SystemColors.GrayText : Aero.InkSoft,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
                return;
            }

            bool hc = AeroEnv.HighContrast;
            Color title = hc ? SystemColors.WindowText : Aero.InkSoft;
            Color hint = hc ? SystemColors.GrayText : Aero.InkSoft;

            Size ts = TextRenderer.MeasureText(g, _emptyText, Aero.BodySemibold,
                new Size(Math.Max(40, w - 60), 60), TextFormatFlags.WordBreak);
            int hintH = _emptyHint.Length > 0 ? 18 : 0;
            int blockH = 24 + ts.Height + hintH + 8;
            int y = area.Y + Math.Max(8, (area.Height - blockH) / 2);

            Draw.Glyph(g, new Rectangle(w / 2 - 9, y, 18, 18), "info", hc ? SystemColors.WindowText : Aero.Blue500);
            y += 24;

            TextRenderer.DrawText(g, _emptyText, Aero.BodySemibold,
                new Rectangle(20, y, w - 40, ts.Height), title,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.WordBreak |
                TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            y += ts.Height + 4;

            if (_emptyHint.Length > 0)
            {
                TextRenderer.DrawText(g, _emptyHint, Aero.Small,
                    new Rectangle(20, y, w - 40, hintH),
                    hint, TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
