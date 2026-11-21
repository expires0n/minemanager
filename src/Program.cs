using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace MineManager
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            AppSettings.Load();
            Aero.SetDark(AppSettings.Dark);
            DarkMode.Prepare(true);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Global\ so two logged-in users (or a second elevated instance) cannot run
            // machine-wide operations at the same time. Falls back to a per-session mutex
            // when the account cannot create a global object.
            bool created;
            Mutex single = null;
            try { single = new Mutex(true, "Global\\MineManager.SingleInstance.v1", out created); }
            catch
            {
                try { single = new Mutex(true, "MineManager.SingleInstance.v1", out created); }
                catch { single = null; created = true; }
            }
            using (single)
            {
                if (!created)
                {
                    AeroDialog.Info(null, "MineManager", L.T("MineManager is already running. Use the open window."), "info");
                    return;
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
                {
                    ShowError(e.Exception);
                };
                AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
                {
                    ShowError(e.ExceptionObject as Exception);
                };

                try
                {
                    Application.Run(new MainForm());
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }

                if (single != null) GC.KeepAlive(single);
            }
        }

        private static void ShowError(Exception ex)
        {
            if (ex == null) return;
            try
            {
                // Crash path: system dialog is intentional, it must render even if
                // the custom chrome or GDI+ theme failed.
                MessageBox.Show(L.T("Unexpected error:\n\n") + ex.Message + "\n\n" +
                    ex.GetType().Name + "\n" + ex.StackTrace,
                    "MineManager · " + L.T("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}
