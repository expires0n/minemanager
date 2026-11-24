using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Keeps native scrollbars in step with the custom Aero palette. WinForms
    /// scrollbars are OS-painted, so they need the undocumented dark-mode
    /// theme; everything else in the app is painted by Aero itself. Failures
    /// are silent: the fallback is the normal light scrollbar.
    /// </summary>
    internal static class DarkMode
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        private static extern int SetPreferredAppMode(int mode);

        [DllImport("uxtheme.dll", EntryPoint = "#133")]
        private static extern bool AllowDarkModeForWindow(IntPtr hWnd, bool allow);

        private static bool _initialized;
        private static bool _available = true;

        private static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try { SetPreferredAppMode(2); }
            catch { _available = false; }
        }

        public static void Prepare(bool dark)
        {
            if (dark) Init();
        }

        public static void Apply(Control root, bool dark)
        {
            if (root == null || !_available) return;
            if (dark) Init();
            Walk(root, dark);
        }

        private static void Walk(Control c, bool dark)
        {
            // Plain WinForms text boxes are painted by the OS theme, not by Aero.
            // Explicit colors alone are ignored while the global dark app mode is
            // active (light text on a white field after switching back to light),
            // so opt the control out of native theming as well.
            TextBox tb = c as TextBox;
            if (tb != null)
            {
                try
                {
                    tb.BackColor = Aero.Field;
                    tb.ForeColor = Aero.Ink;
                    SetWindowTheme(tb.Handle, "", "");
                }
                catch { }
            }
            if (c is ScrollBar)
            {
                try
                {
                    if (dark)
                    {
                        AllowDarkModeForWindow(c.Handle, true);
                        SetWindowTheme(c.Handle, "DarkMode_Explorer", null);
                    }
                    else
                    {
                        SetWindowTheme(c.Handle, "Explorer", null);
                    }
                    c.Invalidate();
                }
                catch { }
            }
            foreach (Control child in c.Controls) Walk(child, dark);
        }
    }
}
