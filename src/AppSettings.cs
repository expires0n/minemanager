using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace MineManager
{
    // Tiny portable settings file: MineManager.ini next to the EXE.
    // If that location is read-only, %APPDATA%\MineManager\settings.ini is used.
    internal static class AppSettings
    {
        public static bool Dark;
        public static string Language { get { return L.Lang; } }
        private static string _path = "";

        public static string SettingsPath
        {
            get
            {
                if (_path.Length == 0) _path = ExeDirPath();
                return _path;
            }
        }

        private static string ExeDirPath()
        {
            try
            {
                return Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "MineManager.ini");
            }
            catch { return Path.Combine(AppDataDir(), "MineManager.ini"); }
        }

        private static string AppDataDir()
        {
            string d = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MineManager");
            try { Directory.CreateDirectory(d); } catch { }
            return d;
        }

        public static void Load()
        {
            L.SetLanguage("en");
            Dark = false;

            string p = ExeDirPath();
            if (!File.Exists(p))
            {
                string alt = Path.Combine(AppDataDir(), "settings.ini");
                p = File.Exists(alt) ? alt : p;
            }
            if (!File.Exists(p)) { _path = p; return; }

            try
            {
                string[] lines = File.ReadAllLines(p, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string s = lines[i].Trim();
                    int eq = s.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = s.Substring(0, eq).Trim().ToLowerInvariant();
                    string v = s.Substring(eq + 1).Trim().ToLowerInvariant();
                    if (k == "language") L.SetLanguage(v);
                    else if (k == "theme") Dark = v == "dark";
                }
                _path = p;
            }
            catch { _path = p; }
        }

        public static void SetLanguage(string code)
        {
            L.SetLanguage(code);
            Save();
        }

        public static void SetTheme(bool dark)
        {
            Dark = dark;
            Save();
        }

        public static void Save()
        {
            string text = "[ui]\nlanguage=" + L.Lang + "\ntheme=" + (Dark ? "dark" : "light") + "\n";
            string[] tries = new string[] { ExeDirPath(), Path.Combine(AppDataDir(), "settings.ini") };
            for (int i = 0; i < tries.Length; i++)
            {
                try
                {
                    File.WriteAllText(tries[i], text, new UTF8Encoding(false));
                    _path = tries[i];
                    return;
                }
                catch { }
            }
        }
    }
}
