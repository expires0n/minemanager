using System;
using System.Collections.Generic;

namespace MineManager
{
    /// <summary>
    /// Turkish strings for the tweaks page UI. Owner: tweaks UI work package.
    /// Tweak titles/descriptions are translated by LangTweaks (data package); only
    /// chrome, actions, state badges and dialogs live here.
    /// </summary>
    internal static class LangTweaksUi
    {
        public static void Register(Dictionary<string, string> d)
        {
            // ------------------------------------------------------- command bar
            d["Registry tweaks · preferences · performance plans"] =
                "Kayıt defteri ayarları · tercihler · performans planları";
            d["Run Tweaks"] = "Ayarları Uygula";
            d["Undo Selected"] = "Seçilenleri Geri Al";
            d["Working..."] = "Çalışıyor...";
            d["Refreshing detected state..."] = "Algılanan durum yenileniyor...";

            // -------------------------------------------------------- group cards
            d["Essential Tweaks"] = "Temel Ayarlar";
            d["Advanced Tweaks - CAUTION"] = "Gelişmiş Ayarlar - DİKKAT";
            d["Customize Preferences"] = "Tercihleri Özelleştir";
            d["Performance Plans - NOT FOR LAPTOPS"] = "Performans Planları - DİZÜSTÜ BİLGİSAYARLAR İÇİN DEĞİL";
            d["I understand — enable advanced tweaks"] = "Anladım — gelişmiş ayarları etkinleştir";

            // ------------------------------------------------------- state badges
            d["Applied"] = "Uygulandı";
            d["Not applied"] = "Uygulanmadı";
            d["Mixed"] = "Karma";
            d["Details"] = "Ayrıntılar";

            // -------------------------------------------------------- activity log
            d["Collapse"] = "Daralt";
            d["Expand"] = "Genişlet";
            d["Cancel requested — stopping after the current tweak..."] =
                "İptal istendi — geçerli ayardan sonra durulacak...";

            // ------------------------------------------------------------ dialogs
            d["Run selected tweaks"] = "Seçilen ayarları uygula";
            d["Undo selected tweaks"] = "Seçilen ayarları geri al";
            d["Run the selected tweaks:"] = "Şu ayarlar uygulanacak:";
            d["Undo the selected tweaks:"] = "Şu ayarlar geri alınacak:";
            d["Run"] = "Uygula";
            d["Undo"] = "Geri Al";
            d["… and {0} more lines"] = "… ve {0} satır daha";
            d["Select at least one tweak."] = "En az bir ayar seçin.";
            d["Nothing to undo."] = "Geri alınacak bir şey yok.";
            d["Nothing to change."] = "Değiştirilecek bir şey yok.";
            d["A job is already running."] = "Zaten bir işlem çalışıyor.";
            d["Administrator rights are required for the selected tweaks."] =
                "Seçilen ayarlar için yönetici hakları gerekiyor.";

            // --------------------------------------------------------- log lines
            d["Run started"] = "Uygulama başladı";
            d["Undo started"] = "Geri alma başladı";
            d["Applying: {0}"] = "Uygulanıyor: {0}";
            d["Restoring: {0}"] = "Geri alınıyor: {0}";
            d["Applied: {0}"] = "Uygulandı: {0}";
            d["Restored: {0}"] = "Geri alındı: {0}";
            d["Could not apply: {0}"] = "Uygulanamadı: {0}";
            d["Could not restore: {0}"] = "Geri alınamadı: {0}";
            d["{0} completed"] = "{0} tamamlandı";
            d["{0} completed · {1} failed"] = "{0} tamamlandı · {1} başarısız";
            d["Cancelled"] = "İptal edildi";
            d["Cancelled · {0} completed"] = "İptal edildi · {0} tamamlandı";
        }
    }
}
