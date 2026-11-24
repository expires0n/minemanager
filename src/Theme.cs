using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MineManager
{
    // =====================================================================
    //  Aero (2007-2012) design tokens - single source of truth.
    //  Windows 7 Aero: glass frame + gloss gradient + 1px white inner line
    //  + narrow metrics. No Fluent/Mica radii, no 44px targets, no Inter.
    // =====================================================================
    internal static partial class Aero
    {
        // ---------------------------------------------------------- primitives
        public static Color Blue100 = Color.FromArgb(0xEA, 0xF5, 0xFF);
        public static Color Blue200 = Color.FromArgb(0xD5, 0xEA, 0xFD);
        public static Color Blue300 = Color.FromArgb(0xB7, 0xD9, 0xF4);
        public static Color Blue400 = Color.FromArgb(0x8E, 0xC3, 0xEA);
        public static Color Blue500 = Color.FromArgb(0x5A, 0xA3, 0xDC);
        public static Color Blue600 = Color.FromArgb(0x2F, 0x7B, 0xBD);
        public static Color Blue700 = Color.FromArgb(0x1D, 0x5B, 0x91);
        public static Color Blue800 = Color.FromArgb(0x12, 0x3C, 0x5E);
        public static Color Blue900 = Color.FromArgb(0x0B, 0x2B, 0x45);
        public static Color Ink = Color.FromArgb(0x16, 0x28, 0x3A);
        public static Color InkSoft = Color.FromArgb(0x3D, 0x55, 0x6B);
        public static Color Disabled = Color.FromArgb(0x93, 0xA3, 0xB1);
        public static Color Success = Color.FromArgb(0x6C, 0xBB, 0x3C);
        public static Color Warning = Color.FromArgb(0xE8, 0xA3, 0x3D);
        public static Color WarningText = Draw.Mix(Warning, Ink, 0.45f);
        public static Color SuccessText = Draw.Mix(Success, Ink, 0.40f);
        public static Color DangerText = Danger;
        public static Color Danger = Color.FromArgb(0xC0, 0x39, 0x2B);
        public static Color White = Color.White;
        public static Color Black = Color.Black;

        // ------------------------------------------------------- neutral surfaces
        public static Color Paper = Color.White;
        public static Color Chrome = Color.FromArgb(0xF1, 0xF5, 0xF9);
        public static Color PanelLine = Color.FromArgb(0xC9, 0xD4, 0xDE);
        public static Color ToolbarTop = Color.FromArgb(0xF6, 0xF9, 0xFC);
        public static Color ToolbarBottom = Color.FromArgb(0xE6, 0xED, 0xF4);
        public static Color ToolbarLine = Color.FromArgb(0xCB, 0xD6, 0xE0);
        public static Color HeaderTop = Color.FromArgb(0xFB, 0xFD, 0xFF);
        public static Color HeaderBottom = Color.FromArgb(0xEA, 0xF0, 0xF6);
        public static Color RowHoverTop = Color.FromArgb(0xF4, 0xFB, 0xFF);
        public static Color RowHoverBottom = Color.FromArgb(0xE7, 0xF4, 0xFE);
        public static Color StatusTop = Color.FromArgb(0xEE, 0xF3, 0xF8);
        public static Color StatusBottom = Color.FromArgb(0xE2, 0xE9, 0xF1);
        public static Color SelectionEdge = Color.FromArgb(0x86, 0xBF, 0xE7);
        public static Color FocusEdge = Color.FromArgb(0x4A, 0x9B, 0xDC);
        public static Color SelectionBack = Color.FromArgb(0xB7, 0xD9, 0xF4);
        public static Color Field = Color.White;
        public static Color ConsoleBg = Color.White;
        public static Color MenuBg = Color.White;
        public static Color WindowTop = Color.FromArgb(0xEA, 0xF5, 0xFF);
        public static Color WindowMid = Color.FromArgb(0xD5, 0xEA, 0xFD);
        public static Color WindowBottom = Color.FromArgb(0xB7, 0xD9, 0xF4);
        public static Color TitleShadow = Color.FromArgb(140, 255, 255, 255);
        public static Color CaptionFaceTop = Color.FromArgb(120, 255, 255, 255);
        public static Color CaptionFaceBottom = Color.FromArgb(70, 255, 255, 255);
        public static Color CaptionEdge = Color.FromArgb(90, 24, 56, 88);
        public static Color CaptionHoverTop = Color.White;
        public static Color CaptionHoverBottom = Color.FromArgb(0xEA, 0xF5, 0xFF);
        public static Color CaptionPressedTop = Color.FromArgb(0xD5, 0xEA, 0xFD);
        public static Color CaptionPressedBottom = Color.FromArgb(0xB7, 0xD9, 0xF4);
        public static Color Focus = Color.FromArgb(230, 88, 168, 238);

        // derived from the palette only (no new hues)
        public static Color FieldBorder = Draw.Mix(PanelLine, InkSoft, 0.35f);
        public static Color ButtonBorder = Draw.Mix(PanelLine, InkSoft, 0.28f);
        public static Color GridLine = Draw.Mix(PanelLine, White, 0.35f);
        public static Color DisabledFace = Draw.Mix(Chrome, PanelLine, 0.45f);
        public static Color DisabledBorder = Draw.Mix(PanelLine, White, 0.30f);

        // gloss stops (button face/hover are prescribed; pressed is derived)
        public static Color[] GlossFace =
        {
            Color.FromArgb(0xFD, 0xFE, 0xFE), Color.FromArgb(0xF2, 0xF6, 0xF9),
            Color.FromArgb(0xE4, 0xEA, 0xEF), Color.FromArgb(0xEE, 0xF3, 0xF7)
        };
        public static Color[] GlossHover =
        {
            Color.FromArgb(0xF7, 0xFC, 0xFF), Color.FromArgb(0xE2, 0xF2, 0xFF),
            Color.FromArgb(0xCF, 0xE8, 0xFD), Color.FromArgb(0xE4, 0xF3, 0xFF)
        };
        public static Color[] GlossPressed = BuildPressed();

        public static Color[] Selection =
        {
            Color.FromArgb(0xD9, 0xED, 0xFF), Color.FromArgb(0xBF, 0xE0, 0xF9),
            Color.FromArgb(0xCF, 0xE8, 0xFC)
        };

        private static Color[] BuildPressed()
        {
            Color[] a = new Color[4];
            for (int i = 0; i < 4; i++) a[i] = Draw.Mix(GlossFace[i], Blue400, 0.30f);
            return a;
        }
        public static Color TasksTop = Color.FromArgb(38, 54, 74);
        public static Color TasksBottom = Color.FromArgb(10, 18, 28);

        // ------------------------------------------------------------- cam (glass)
        public static Color GlassRim = Color.FromArgb(82, 24, 56, 88);
        public static Color GlassEdge = Color.FromArgb(140, 255, 255, 255);
        public static Color GlassTintTop = Color.FromArgb(230, 233, 245, 255);
        public static Color GlassTintMid = Color.FromArgb(189, 180, 214, 244);
        public static Color GlassTintBottom = Color.FromArgb(204, 196, 226, 250);
        public static Color GlassFallbackTop = Color.FromArgb(0xE9, 0xF5, 0xFF);
        public static Color GlassFallbackMid = Color.FromArgb(0xB4, 0xD6, 0xF4);
        public static Color GlassFallbackBottom = Color.FromArgb(0xC4, 0xE2, 0xFA);
        public static Color GlowColor = Color.FromArgb(97, 122, 190, 250);

        // -------------------------------------------------------------- metrics
        public const int TitleBarH = 30;
        public const int CaptionW = 26;
        public const int CaptionH = 17;
        public const int CaptionGap = 2;
        public const int ButtonH = 23;
        public const int FieldH = 22;
        public const int RowH = 24;
        public const int StatusH = 22;
        public const int RadiusWindow = 8;
        public const int RadiusControl = 3;
        public const int RadiusField = 2;
        public const int GlassPad = 4;
        public const int NavW = 190;
        public const int NavCompactW = 46;

        // --------------------------------------------------------------- type
        public static readonly Font Body = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font BodySemibold = new Font("Segoe UI Semibold", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Small = new Font("Segoe UI", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font SmallSemibold = new Font("Segoe UI Semibold", 8.25f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Heading = new Font("Segoe UI Semibold", 11.25f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Mono = new Font("Consolas", 9f, FontStyle.Regular, GraphicsUnit.Point);
    }

    // Environment switches: reduced transparency / contrast / motion.
    internal static class AeroEnv
    {
        public static readonly bool HighContrast = SystemInformation.HighContrast;
        public static readonly bool ReducedMotion = SystemInformation.HighContrast || !Native.AnimationsEnabled();

        private static bool? _transparency;
        public static bool TransparencyEnabled
        {
            get
            {
                if (!_transparency.HasValue)
                {
                    bool on = true;
                    try
                    {
                        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(
                            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                        {
                            if (k != null)
                            {
                                object v = k.GetValue("EnableTransparency");
                                if (v is int) on = (int)v != 0;
                            }
                        }
                    }
                    catch { }
                    _transparency = on;
                }
                return _transparency.Value;
            }
        }

        public static bool ReducedTransparency
        {
            get { return HighContrast || !TransparencyEnabled; }
        }
    }

    // Legacy facade kept so pages express intent, not hex values.
    internal static class Theme
    {
        public static Color Bg { get { return AeroEnv.HighContrast ? SystemColors.Window : Aero.Chrome; } }
        public static Color Surface { get { return AeroEnv.HighContrast ? SystemColors.Window : Aero.Paper; } }
        public static Color Text { get { return AeroEnv.HighContrast ? SystemColors.WindowText : Aero.Ink; } }
        public static Color Dim { get { return AeroEnv.HighContrast ? SystemColors.GrayText : Aero.InkSoft; } }
        public static Color Line { get { return AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.PanelLine; } }
        public static Color Accent { get { return AeroEnv.HighContrast ? SystemColors.Highlight : Aero.Blue600; } }
        public static Color AccentSoft { get { return AeroEnv.HighContrast ? SystemColors.Highlight : Aero.Blue200; } }
        public static Color Ok { get { return AeroEnv.HighContrast ? SystemColors.WindowText : Aero.SuccessText; } }
        public static Color Warn { get { return AeroEnv.HighContrast ? SystemColors.WindowText : Aero.WarningText; } }
        public static Color Danger { get { return AeroEnv.HighContrast ? SystemColors.WindowText : Aero.DangerText; } }
        public static Color Focus { get { return AeroEnv.HighContrast ? SystemColors.Highlight : Aero.Focus; } }

        // type scale: 12/16 body, 15/600 heading, 11 secondary, 12/400 button
        public static readonly Font F = Aero.Body;
        public static readonly Font Fb = Aero.BodySemibold;
        public static readonly Font Fs = Aero.Small;
        public static readonly Font FsBold = Aero.SmallSemibold;
        public static readonly Font H1 = Aero.Heading;
        public static readonly Font H2 = Aero.BodySemibold;
        public static readonly Font Big = new Font("Segoe UI Semibold", 24f, FontStyle.Regular, GraphicsUnit.Point);
        public static readonly Font Mono = Aero.Mono;
    }

    // ------------------------------------------------------------------ paint

    internal static class Draw
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d < 2) d = 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(c))
                g.FillPath(b, p);
        }

        public static void StrokeRound(Graphics g, Rectangle r, int radius, Color c)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(c, 1f))
                g.DrawPath(pen, p);
        }

        public static Color Mix(Color a, Color b, float t)
        {
            if (t < 0f) t = 0f;
            if (t > 1f) t = 1f;
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        public static LinearGradientBrush VGrad(Rectangle r, params Color[] stops)
        {
            int w = Math.Max(1, r.Width);
            int h = Math.Max(1, r.Height);
            LinearGradientBrush b = new LinearGradientBrush(
                new Rectangle(r.X, r.Y, w, h), stops[0], stops[stops.Length - 1], 90f);
            if (stops.Length > 2)
            {
                ColorBlend cb = new ColorBlend(stops.Length);
                cb.Colors = stops;
                cb.Positions = new float[stops.Length];
                for (int i = 0; i < stops.Length; i++)
                    cb.Positions[i] = (float)i / (stops.Length - 1);
                b.InterpolationColors = cb;
            }
            return b;
        }

        public static void FillVGrad(Graphics g, Rectangle r, int radius, params Color[] stops)
        {
            if (r.Width <= 0 || r.Height <= 0) return;
            using (GraphicsPath p = Round(r, radius))
            using (LinearGradientBrush b = VGrad(r, stops))
                g.FillPath(b, p);
        }

        // Aero glass: tint + sheen + 1px white inner edge + 1px dark rim.
        public static void Glass(Graphics g, Rectangle r, int radius, bool withSheen)
        {
            if (r.Width <= 1 || r.Height <= 1) return;
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rim = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);

            if (AeroEnv.HighContrast)
            {
                using (GraphicsPath p = Round(r, radius))
                using (SolidBrush b = new SolidBrush(SystemColors.Window))
                    g.FillPath(b, p);
            }
            else if (AeroEnv.ReducedTransparency)
            {
                FillVGrad(g, r, radius, Aero.GlassFallbackTop, Aero.GlassFallbackMid, Aero.GlassFallbackBottom);
            }
            else
            {
                using (GraphicsPath p = Round(rim, radius))
                {
                    g.SetClip(p, CombineMode.Intersect);
                    using (LinearGradientBrush b = VGrad(r,
                        Aero.GlassTintTop, Aero.GlassTintMid, Aero.GlassTintBottom))
                        g.FillRectangle(b, r);
                    if (withSheen)
                    {
                        using (LinearGradientBrush s = VGrad(r,
                            Color.FromArgb(184, 255, 255, 255),
                            Color.FromArgb(31, 255, 255, 255),
                            Color.FromArgb(10, 255, 255, 255),
                            Color.FromArgb(87, 255, 255, 255)))
                            g.FillRectangle(s, r);
                    }
                    g.ResetClip();
                }
            }

            Rectangle ir = Rectangle.Inflate(rim, -1, -1);
            if (ir.Width > 2 && ir.Height > 2)
                StrokeRound(g, ir, Math.Max(1, radius - 1),
                    AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.GlassEdge);
            StrokeRound(g, rim, radius,
                AeroEnv.HighContrast ? SystemColors.WindowFrame : Aero.GlassRim);
            g.SmoothingMode = old;
        }

        public static void GlassFlat(Graphics g, Rectangle r, int radius)
        {
            Glass(g, r, radius, false);
        }

        public static void Glow(Graphics g, Rectangle r, int radius)
        {
            if (AeroEnv.HighContrast) return;
            for (int i = 3; i >= 1; i--)
            {
                Rectangle gr = Rectangle.Inflate(r, i, i);
                Color c = Color.FromArgb(16 - i * 4, Aero.GlowColor);
                StrokeRound(g, gr, radius + i, c);
            }
        }

        public static void FocusRing(Graphics g, Rectangle r, int radius)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = Round(Rectangle.Inflate(r, -1, -1), Math.Max(1, radius)))
            {
                // 1px dark contour keeps the 2px light-blue ring above 3:1 on light surfaces
                if (!AeroEnv.HighContrast)
                    using (Pen halo = new Pen(Color.FromArgb(180, Aero.Blue700), 3.5f))
                        g.DrawPath(halo, path);
                using (Pen p = new Pen(Theme.Focus, 2f))
                    g.DrawPath(p, path);
            }
            g.SmoothingMode = old;
        }

        public static void SheenTopHalf(Graphics g, Rectangle r, int radius, int alpha)
        {
            if (AeroEnv.ReducedTransparency || r.Height < 4) return;
            Rectangle half = new Rectangle(r.X, r.Y, r.Width, r.Height / 2 + 1);
            using (GraphicsPath clip = Round(r, radius))
            {
                g.SetClip(clip, CombineMode.Intersect);
                using (LinearGradientBrush b = VGrad(half,
                    Color.FromArgb(alpha, 255, 255, 255), Color.FromArgb(0, 255, 255, 255)))
                    g.FillRectangle(b, half);
                g.ResetClip();
            }
        }

        public static void DoubleBuffer(Control c)
        {
            try
            {
                PropertyInfo pi = typeof(Control).GetProperty("DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (pi != null) pi.SetValue(c, true, null);
            }
            catch { }
        }

        // ------------------------------------------------------------ glyphs
        public static void Bolt(Graphics g, Rectangle r, Color c)
        {
            float w = r.Width, h = r.Height;
            PointF[] pts = new PointF[]
            {
                new PointF(r.X + 0.58f * w, r.Y + 0.05f * h),
                new PointF(r.X + 0.28f * w, r.Y + 0.58f * h),
                new PointF(r.X + 0.47f * w, r.Y + 0.58f * h),
                new PointF(r.X + 0.40f * w, r.Y + 0.95f * h),
                new PointF(r.X + 0.73f * w, r.Y + 0.42f * h),
                new PointF(r.X + 0.53f * w, r.Y + 0.42f * h)
            };
            using (SolidBrush b = new SolidBrush(c))
                g.FillPolygon(b, pts);
        }

        public static void Glyph(Graphics g, Rectangle r, string kind, Color c)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(c, 1f))
            using (SolidBrush brush = new SolidBrush(c))
            {
                int cx = r.X + r.Width / 2;
                int cy = r.Y + r.Height / 2;
                switch (kind)
                {
                    case "min":
                        g.DrawLine(pen, cx - 3, cy + 3, cx + 3, cy + 3);
                        break;
                    case "max":
                        g.DrawRectangle(pen, cx - 4, cy - 3, 8, 7);
                        g.DrawLine(pen, cx - 4, cy - 2, cx + 3, cy - 2);
                        break;
                    case "restore":
                        g.DrawRectangle(pen, cx - 4, cy - 4, 7, 6);
                        g.DrawRectangle(pen, cx - 2, cy - 2, 7, 6);
                        break;
                    case "close":
                        g.DrawLine(pen, cx - 3, cy - 3, cx + 3, cy + 3);
                        g.DrawLine(pen, cx + 3, cy - 3, cx - 3, cy + 3);
                        break;
                    case "check":
                        using (Pen p2 = new Pen(c, 1.8f))
                        {
                            p2.StartCap = LineCap.Round;
                            p2.EndCap = LineCap.Round;
                            g.DrawLines(p2, new PointF[]
                            {
                                new PointF(r.X + 0.20f * r.Width, r.Y + 0.53f * r.Height),
                                new PointF(r.X + 0.42f * r.Width, r.Y + 0.75f * r.Height),
                                new PointF(r.X + 0.82f * r.Width, r.Y + 0.27f * r.Height)
                            });
                        }
                        break;
                    case "search":
                        using (Pen p3 = new Pen(c, 1.4f))
                        {
                            g.DrawEllipse(p3, r.X + 1, r.Y + 1, 7, 7);
                            g.DrawLine(p3, r.X + 7, r.Y + 7, r.X + 10, r.Y + 10);
                        }
                        break;
                    case "down":
                        using (Pen p4 = new Pen(c, 1.2f))
                        {
                            g.DrawLine(p4, cx - 2, cy - 1, cx, cy + 1);
                            g.DrawLine(p4, cx, cy + 1, cx + 2, cy - 1);
                        }
                        break;
                    case "spark":
                        g.FillPolygon(brush, new PointF[]
                        {
                            new PointF(cx, cy - 6), new PointF(cx + 2, cy - 1),
                            new PointF(cx + 6, cy), new PointF(cx + 2, cy + 1),
                            new PointF(cx, cy + 6), new PointF(cx - 2, cy + 1),
                            new PointF(cx - 6, cy), new PointF(cx - 2, cy - 1)
                        });
                        g.FillEllipse(brush, cx + 3, cy - 6, 3, 3);
                        break;
                    case "ram":
                        g.DrawRectangle(pen, r.X + 3, r.Y + 4, 9, 8);
                        g.DrawRectangle(pen, r.X + 5, r.Y + 6, 5, 4);
                        for (int i = 0; i < 3; i++)
                        {
                            g.DrawLine(pen, r.X + 5 + i * 3, r.Y + 2, r.X + 5 + i * 3, r.Y + 4);
                            g.DrawLine(pen, r.X + 5 + i * 3, r.Y + 12, r.X + 5 + i * 3, r.Y + 14);
                        }
                        break;
                    case "box":
                        g.DrawRectangle(pen, r.X + 2, r.Y + 5, 12, 9);
                        g.DrawLine(pen, r.X + 2, r.Y + 8, r.X + 14, r.Y + 8);
                        g.DrawLine(pen, r.X + 5, r.Y + 2, r.X + 11, r.Y + 2);
                        g.DrawLine(pen, r.X + 2, r.Y + 5, r.X + 2, r.Y + 3);
                        break;
                    case "chip":
                        g.DrawRectangle(pen, r.X + 3, r.Y + 3, 10, 10);
                        g.DrawRectangle(pen, r.X + 6, r.Y + 6, 4, 4);
                        g.DrawLine(pen, r.X + 5, r.Y + 1, r.X + 5, r.Y + 3);
                        g.DrawLine(pen, r.X + 11, r.Y + 1, r.X + 11, r.Y + 3);
                        break;
                    case "sliders":
                        for (int i = 0; i < 3; i++)
                        {
                            int y = r.Y + 4 + i * 4;
                            g.DrawLine(pen, r.X + 2, y, r.Right - 2, y);
                            int kx = r.X + (i == 0 ? 4 : (i == 1 ? 9 : 6));
                            g.FillRectangle(brush, kx - 1, y - 2, 3, 5);
                        }
                        break;
                    case "gear":
                        g.DrawEllipse(pen, cx - 2, cy - 2, 4, 4);
                        for (int i = 0; i < 8; i++)
                        {
                            double a = i * Math.PI / 4.0;
                            int x1 = cx + (int)Math.Round(3.2 * Math.Cos(a));
                            int y1 = cy + (int)Math.Round(3.2 * Math.Sin(a));
                            int x2 = cx + (int)Math.Round(6.2 * Math.Cos(a));
                            int y2 = cy + (int)Math.Round(6.2 * Math.Sin(a));
                            g.DrawLine(pen, x1, y1, x2, y2);
                        }
                        break;
                    case "info":
                        g.DrawEllipse(pen, r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
                        g.FillEllipse(brush, cx - 1, cy - 4, 2, 2);
                        g.DrawLine(pen, cx, cy - 1, cx, cy + 4);
                        break;
                    case "warn":
                        g.DrawPolygon(pen, new PointF[]
                        {
                            new PointF(cx, r.Y + 3), new PointF(r.X + r.Width - 3, r.Y + r.Height - 4),
                            new PointF(r.X + 3, r.Y + r.Height - 4)
                        });
                        g.DrawLine(pen, cx, cy - 2, cx, cy + 2);
                        g.FillEllipse(brush, cx - 1, cy + 4, 2, 2);
                        break;
                    case "shield":
                        g.DrawPolygon(pen, new PointF[]
                        {
                            new PointF(cx, r.Y + 2), new PointF(r.X + r.Width - 4, r.Y + 5),
                            new PointF(r.X + r.Width - 5, r.Y + r.Height - 6),
                            new PointF(cx, r.Y + r.Height - 2),
                            new PointF(r.X + 5, r.Y + r.Height - 6),
                            new PointF(r.X + 4, r.Y + 5)
                        });
                        break;
                    case "repair":
                        g.DrawArc(pen, r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4, 35, 285);
                        g.DrawLine(pen, r.Right - 7, r.Y + 3, r.Right - 3, r.Y + 7);
                        g.DrawLine(pen, r.Right - 3, r.Y + 7, r.Right - 8, r.Y + 8);
                        break;
                    case "menu":
                        g.DrawLine(pen, cx - 4, cy - 3, cx + 4, cy - 3);
                        g.DrawLine(pen, cx - 4, cy, cx + 4, cy);
                        g.DrawLine(pen, cx - 4, cy + 3, cx + 4, cy + 3);
                        break;
                }
            }
            g.SmoothingMode = old;
        }

        public static void VerticalDot(Graphics g, int x, int y, int h, Color c)
        {
            using (Pen p = new Pen(Color.FromArgb(120, c)))
                g.DrawLine(p, x, y, x, y + h);
        }
    }

    internal static class Fmt
    {
        public static string Size(double bytes)
        {
            if (bytes < 1024) return bytes.ToString("0", L.Culture) + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("0.#", L.Culture) + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("0.#", L.Culture) + " MB";
            double gb = mb / 1024.0;
            return gb.ToString("0.##", L.Culture) + " GB";
        }

        public static string Num(long n)
        {
            return n.ToString("N0", L.Culture);
        }
    }

    internal static class Ui
    {
        public static Label Lbl(string text, Font f, Color c)
        {
            Label l = new Label();
            l.Text = text;
            l.Font = f;
            l.ForeColor = c;
            l.AutoSize = true;
            l.BackColor = Color.Transparent;
            l.UseMnemonic = false;
            return l;
        }

        public static AeroButton Btn(string text, int w)
        {
            AeroButton b = new AeroButton(text);
            b.Default = true;
            b.Size = new Size(w, Aero.ButtonH);
            return b;
        }

        public static AeroButton Outline(string text, int w)
        {
            AeroButton b = new AeroButton(text);
            b.Size = new Size(w, Aero.ButtonH);
            return b;
        }

        public static DataGridViewTextBoxColumn Col(string name, string header, float weight,
            DataGridViewContentAlignment align)
        {
            DataGridViewTextBoxColumn c = new DataGridViewTextBoxColumn();
            c.Name = name;
            c.HeaderText = header;
            c.FillWeight = weight;
            c.MinimumWidth = 40 + (int)(weight * 3);
            c.ReadOnly = true;
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
            c.DefaultCellStyle.Alignment = align;
            c.HeaderCell.Style.Alignment = align;
            return c;
        }

        public static DataGridViewCheckBoxColumn Chk(string name, string header, int width)
        {
            DataGridViewCheckBoxColumn c = new DataGridViewCheckBoxColumn();
            c.Name = name;
            c.HeaderText = header;
            c.Width = width;
            c.MinimumWidth = width;
            c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            c.FillWeight = 1;
            c.SortMode = DataGridViewColumnSortMode.NotSortable;
            c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            return c;
        }

        public static void ResetScroll(DataGridView g)
        {
            try
            {
                if (g.Rows.Count > 0) g.FirstDisplayedScrollingRowIndex = 0;
                g.ClearSelection();
                g.CurrentCell = null;
            }
            catch { }
        }
    }
}
