using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MineManager
{
    /// <summary>
    /// Off-screen UI smoke test. Builds the real MainForm, walks every page in both
    /// languages and both themes, and writes PNGs so layout regressions are visible
    /// without clicking through the app. Not part of the shipped EXE.
    /// </summary>
    internal static class Smoke
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string outDir = args.Length > 0 ? args[0] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shots");
            Directory.CreateDirectory(outDir);

            AppSettings.Load();
            AppSettings.Dark = false;
            DarkMode.Prepare(true);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            int[] sizes = new int[] { 1100, 940, 620, 320 };
            string[] langs = new string[] { "en", "tr" };

            foreach (string lang in langs)
            {
                L.SetLanguage(lang);
                foreach (bool dark in new bool[] { false, true })
                {
                    AppSettings.Dark = dark;
                    Aero.SetDark(dark);
                    foreach (int w in sizes)
                    {
                        using (MainForm f = new MainForm())
                        {
                            f.ClientSize = new Size(w, w < 700 ? 620 : 720);
                            f.Show();
                            Application.DoEvents();
                            for (int page = 0; page < 7; page++)
                            {
                                try
                                {
                                    MethodInfo m = typeof(MainForm).GetMethod("ShowPage",
                                        BindingFlags.Instance | BindingFlags.NonPublic);
                                    if (m != null) m.Invoke(f, new object[] { page });
                                    Application.DoEvents();
                                    System.Threading.Thread.Sleep(60);
                                    Application.DoEvents();
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("page " + page + " failed: " + ex.Message);
                                }
                                Dump(f, Path.Combine(outDir,
                                    string.Format("{0}-{1}-{2}-p{3}.png", lang, dark ? "dark" : "light", w, page)));
                            }
                            f.Hide();
                        }
                    }
                }
            }
            Console.WriteLine("shots written to " + outDir);
        }

        private static void Dump(Form f, string path)
        {
            try
            {
                using (Bitmap bmp = new Bitmap(Math.Max(1, f.Width), Math.Max(1, f.Height)))
                {
                    f.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                    bmp.Save(path, ImageFormat.Png);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("dump failed: " + ex.Message);
            }
        }
    }
}
