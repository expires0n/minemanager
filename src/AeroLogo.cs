using System;
using System.Drawing;
using System.IO;

namespace MineManager
{
    // Marka isareti: build sirasinda gomulen 64px seffaf PNG.
    // Kaynak yoksa (or. ekransiz test) cagiran taraf glyph'e duser.
    internal static class AeroLogo
    {
        private static Bitmap _image;
        private static bool _loaded;

        public static Bitmap Image
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    try
                    {
                        using (Stream s = typeof(AeroLogo).Assembly.GetManifestResourceStream(
                            "MineManager.Resources.logo.png"))
                        {
                            if (s != null)
                                using (Bitmap b = new Bitmap(s))
                                    _image = new Bitmap(b);
                        }
                    }
                    catch { }
                }
                return _image;
            }
        }
    }
}
