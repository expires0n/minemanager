using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;

namespace MineManager
{
    // Dark Aero palette (graphite + glass, not Fluent). The light palette is
    // captured once from the field initializers, so switching back is lossless.
    internal static partial class Aero
    {
        public static bool Dark;
        private static Dictionary<string, object> _light;

        static Aero()
        {
            _light = Capture();
        }

        private static Dictionary<string, object> Capture()
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            FieldInfo[] fields = typeof(Aero).GetFields(BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (f.FieldType == typeof(Color))
                    map[f.Name] = f.GetValue(null);
                else if (f.FieldType == typeof(Color[]))
                {
                    Color[] a = f.GetValue(null) as Color[];
                    if (a != null) map[f.Name] = a.Clone();
                }
            }
            return map;
        }

        private static void RestoreLight()
        {
            FieldInfo[] fields = typeof(Aero).GetFields(BindingFlags.Static | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (!_light.ContainsKey(f.Name)) continue;
                object v = _light[f.Name];
                if (v is Color[]) v = ((Color[])v).Clone();
                f.SetValue(null, v);
            }
        }

        public static void SetDark(bool dark)
        {
            Dark = dark;
            if (dark) ApplyDark();
            else RestoreLight();
            Recompute();
        }

        private static Color C(int r, int g, int b) { return Color.FromArgb(r, g, b); }
        private static Color A(int a, int r, int g, int b) { return Color.FromArgb(a, r, g, b); }

        private static void ApplyDark()
        {
            Ink = C(0xE8, 0xF1, 0xF9);
            InkSoft = C(0xA9, 0xBD, 0xD0);
            Disabled = C(0x6F, 0x82, 0x96);

            Paper = C(0x20, 0x2B, 0x38);
            Chrome = C(0x18, 0x21, 0x2C);
            PanelLine = C(0x3A, 0x4A, 0x5B);
            ToolbarTop = C(0x26, 0x34, 0x42);
            ToolbarBottom = C(0x1D, 0x29, 0x36);
            ToolbarLine = C(0x3A, 0x4A, 0x5B);
            HeaderTop = C(0x2A, 0x39, 0x48);
            HeaderBottom = C(0x22, 0x30, 0x3D);
            RowHoverTop = C(0x2A, 0x3D, 0x50);
            RowHoverBottom = C(0x24, 0x36, 0x4A);
            StatusTop = C(0x1E, 0x2A, 0x36);
            StatusBottom = C(0x18, 0x23, 0x2F);
            SelectionEdge = C(0x5A, 0xA3, 0xDC);
            FocusEdge = C(0x8E, 0xC3, 0xEA);
            SelectionBack = C(0x1F, 0x5C, 0x8F);
            Field = C(0x17, 0x21, 0x2C);
            ConsoleBg = C(0x14, 0x1C, 0x25);
            MenuBg = C(0x23, 0x2F, 0x3D);

            WindowTop = C(0x22, 0x38, 0x4C);
            WindowMid = C(0x1B, 0x2C, 0x3D);
            WindowBottom = C(0x16, 0x23, 0x2F);
            TitleShadow = A(130, 0, 0, 0);
            CaptionFaceTop = A(38, 255, 255, 255);
            CaptionFaceBottom = A(18, 255, 255, 255);
            CaptionEdge = A(130, 0, 0, 0);
            CaptionHoverTop = C(0x35, 0x51, 0x6B);
            CaptionHoverBottom = C(0x26, 0x3E, 0x54);
            CaptionPressedTop = C(0x1E, 0x3A, 0x52);
            CaptionPressedBottom = C(0x19, 0x32, 0x47);
            Focus = A(240, 142, 200, 255);

            GlossFace = new Color[]
            {
                C(0x3C, 0x4C, 0x5D), C(0x33, 0x42, 0x53),
                C(0x2A, 0x38, 0x48), C(0x2F, 0x3E, 0x4E)
            };
            GlossHover = new Color[]
            {
                C(0x35, 0x5A, 0x78), C(0x2E, 0x5D, 0x84),
                C(0x25, 0x5A, 0x85), C(0x2C, 0x58, 0x7B)
            };
            Selection = new Color[]
            {
                C(0x1D, 0x4A, 0x70), C(0x1A, 0x5F, 0x93),
                C(0x1B, 0x54, 0x84)
            };

            GlassRim = A(150, 0, 0, 0);
            GlassEdge = A(60, 255, 255, 255);
            GlassTintTop = A(235, 20, 32, 45);
            GlassTintMid = A(205, 28, 42, 58);
            GlassTintBottom = A(220, 22, 34, 48);
            GlassFallbackTop = C(0x1A, 0x24, 0x30);
            GlassFallbackMid = C(0x24, 0x34, 0x44);
            GlassFallbackBottom = C(0x1E, 0x2C, 0x3B);
            GlowColor = A(90, 90, 163, 220);
        }

        private static void Recompute()
        {
            WarningText = Draw.Mix(Warning, Dark ? White : Ink, 0.45f);
            SuccessText = Draw.Mix(Success, Dark ? White : Ink, 0.40f);
            DangerText = Dark ? Draw.Mix(Danger, White, 0.45f) : Danger;

            FieldBorder = Draw.Mix(PanelLine, InkSoft, 0.35f);
            ButtonBorder = Draw.Mix(PanelLine, InkSoft, 0.28f);
            GridLine = Draw.Mix(PanelLine, Dark ? Black : White, 0.35f);
            DisabledFace = Draw.Mix(Chrome, PanelLine, 0.45f);
            DisabledBorder = Draw.Mix(PanelLine, Dark ? Black : White, 0.30f);

            Color[] p = new Color[4];
            for (int i = 0; i < 4; i++) p[i] = Draw.Mix(GlossFace[i], Blue400, 0.30f);
            GlossPressed = p;
        }
    }
}
