using System;
using System.Collections.Generic;
using System.Globalization;

namespace MineManager
{
    // English is the source language: L.T("Clean") returns "Clean" unless
    // a Turkish translation is registered. This keeps call sites readable
    // and makes a missing translation fall back safely.
    internal static class L
    {
        public static string Lang = "en";
        public static CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        private static readonly Dictionary<string, string> Tr =
            new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ---------------------------------------------------------- shell
            { "SYSTEM", "SİSTEM" },
            { "MANAGEMENT", "YÖNETİM" },
            { "APP", "UYGULAMA" },
            { "Cleanup", "Temizlik" },
            { "Uninstaller", "Kaldırıcı" },
            { "Tweaks", "İnce Ayarlar" },
            { "Config", "Yapılandırma" },
            { "Hardware", "Donanım" },
            { "Settings", "Ayarlar" },
            { "Minimize", "Simge durumuna küçült" },
            { "Maximize", "Büyüt" },
            { "Restore", "Geri yükle" },
            { "Close", "Kapat" },
            { "Close anyway", "Yine de kapat" },
            { "A job is still running", "Bir işlem hâlâ çalışıyor" },
            { "A background operation has not finished. Closing MineManager now also stops the running Windows tool. Close anyway?", "Arka planda süren bir işlem var. MineManager'ı şimdi kapatmak çalışan Windows aracını da durdurur. Yine de kapatılsın mı?" },
            { "MineManager title bar", "MineManager başlık çubuğu" },
            { "{0} sections · portable · {1}", "{0} bölüm · portable · {1}" },
            { "administrator", "yönetici" },
            { "limited mode", "sınırlı mod" },
            { "C: {0} free   ·   {1}", "C: {0} boş   ·   {1}" },

            // ------------------------------------------------------- settings
            { "Language · appearance", "Dil · görünüm" },
            { "Language", "Dil" },
            { "Interface language", "Arayüz dili" },
            { "Choose the interface language", "Arayüz dilini seçin" },
            { "Appearance", "Görünüm" },
            { "Window and control colors", "Pencere ve kontrol renkleri" },
            { "Light", "Açık" },
            { "Dark", "Koyu" },
            { "Changes apply immediately.", "Değişiklikler hemen uygulanır." },
            { "Settings are stored in MineManager.ini next to the application.",
              "Ayarlar uygulamanın yanındaki MineManager.ini dosyasında saklanır." },

            // --------------------------------------------------------- common
            { "OK", "Tamam" },
            { "Cancel", "İptal" },
            { "More", "Diğer" },
            { "Refresh", "Yenile" },
            { "Clean", "Temizle" },
            { "Error", "Hata" },
            { "Version number", "Sürüm numarası" },
            { "Copy", "Kopyala" },
            { "Save TXT", "TXT Kaydet" },
            { "Error: {0}", "Hata: {0}" },
            { "Done", "Tamamlandı" },
            { "No data", "Veri yok" },
            { "High", "Yüksek" },
            { "Folder", "Klasör" },
            { "Registry value", "Kayıt Değeri" },
            { "Registry key", "Kayıt Defteri" },
            { "Registry", "Kayıt Defteri" },

            // -------------------------------------------------------- dialect
            { "MineManager is already running. Use the open window.",
              "MineManager zaten çalışıyor. Açık pencereyi kullanın." },
            { "Unexpected error:\n\n", "Beklenmeyen bir hata oluştu:\n\n" },
            { "Disk Cleanup", "Disk Temizliği" },
            { "Temporary files, caches and leftovers", "Geçici dosyalar, önbellekler ve gereksiz kalıntılar" },
            { "Scan", "Tara" },
            { "Clean now", "Temizle" },
            { "Selected: {0}", "Seçili: {0}" },
            { "Ready", "Hazır" },
            { "Categories", "Kategoriler" },
            { "{0} categories", "{0} kategori" },
            { "Category", "Kategori" },
            { "Description", "Açıklama" },
            { "Size", "Boyut" },
            { "Status", "Durum" },
            { "Scanning...", "Taranıyor..." },
            { "Scan complete · {0}", "Tarama bitti · {0}" },
            { "Nothing to clean (scan first)", "Temizlenecek bir şey yok (önce tarayın)" },
            { "Cleaning...", "Temizleniyor..." },
            { "Cleaning: {0}", "Temizleniyor: {0}" },
            { "Done · {0} freed", "Tamamlandı · {0} boşaltıldı" },
            { "Cleanup complete · {0} freed", "Temizlik tamamlandı · {0} boşaltıldı" },
            { "Confirm cleanup", "Temizliği Onayla" },
            { "About {0} of data will be deleted from {1} selected categories.\n\nContinue?", "Seçilen {1} kategoriden yaklaşık {0} veri silinecek.\n\nDevam edilsin mi?" },
            { "User temporary files", "Kullanıcı Geçici Dosyaları" },
            { "Windows temporary files", "Windows Geçici Dosyaları" },
            { "Windows Update cache", "Windows Update Önbelleği" },
            { "Downloaded update files", "İndirilen güncelleme dosyaları" },
            { "Delivery Optimization cache", "Teslimat İyileştirme Önbelleği" },
            { "Prefetch data", "Prefetch Verileri" },
            { "First launches may be a little slower", "İlk açılışlar bir miktar yavaşlayabilir" },
            { "Recycle Bin", "Geri Dönüşüm Kutusu" },
            { "All drives", "Tüm sürücüler" },
            { "Browser caches", "Tarayıcı Önbellekleri" },
            { "Browsers must be closed", "Tarayıcılar kapalı olmalı" },
            { "Error reports (WER)", "Hata Raporları (WER)" },
            { "Crash dumps", "Çökme Dökümleri" },
            { "Windows logs", "Windows Günlükleri" },
            { "Thumbnail cache", "Küçük Resim Önbelleği" },
            { "Explorer must be restarted", "Gezgin yeniden başlatılmalı" },
            { "INetCache / Web cache", "INetCache / Web Önbelleği" },
            { "Setup leftovers", "Kurulum Artıkları" },
            { "setup / MSI cache", "setup / MSI önbelleği" },
            { "Already clean", "Temiz" },
            { "Cleaned", "Temizlendi" },
            { "Partially deleted ({0} items in use)", "Kısmen silindi ({0} öğe kullanımda)" },
            { "Memory", "Bellek" },
            { "Physical memory · page file · processes", "Fiziksel bellek · sayfa dosyası · işlemler" },
            { "Quick Free", "Hızlı Boşalt" },
            { "Deep Clean", "Derin Temizlik" },
            { "Physical Memory", "Fiziksel Bellek" },
            { "live", "canlı" },
            { "in use", "kullanımda" },
            { "Deep clean empties the standby list;\nprograms may be slower on first use.", "Derin temizlik bekleme listesini boşaltır;\nprogramlar ilk açılışta yavaşlayabilir." },
            { "Activity Log", "İşlem Günlüğü" },
            { "last 300 lines", "son 300 satır" },
            { "Processes Using Memory", "Bellek Kullanan İşlemler" },
            { "top 15", "ilk 15" },
            { "No process data", "Süreç verisi yok" },
            { "The list refreshes in a few seconds.", "Liste birkaç saniye içinde yenilenir." },
            { "Process", "Süreç" },
            { "Used: {0} / {1}", "Kullanılan: {0} / {1}" },
            { "Available: {0}", "Kullanılabilir: {0}" },
            { "Page file: {0} / {1}", "Sayfa dosyası: {0} / {1}" },
            { "Installed RAM: {0}", "Kurulu RAM: {0}" },
            { "Quick free started · available {0}", "Hızlı boşaltma başladı · kullanılabilir {0}" },
            { "Deep clean started · available {0}", "Derin temizlik başladı · kullanılabilir {0}" },
            { "{0} process working sets were emptied", "{0} sürecin çalışma seti boşaltıldı" },
            { "Available memory: {0}  ({1})", "Kullanılabilir bellek: {0}  ({1})" },
            { "Memory usage {0} → {1}", "Bellek kullanımı {0} → {1}" },
            { "Deep clean completed", "Derin temizlik tamamlandı" },
            { "Memory freed", "Bellek boşaltıldı" },
            { "System memory: {0}", "Sistem belleği: {0}" },
            { "Log is empty.", "Günlük boş." },
            { "Privilege not acquired (run as administrator)", "Ayrıcalık alınamadı (yönetici olarak çalıştırın)" },
            { "Modified page list: {0}", "Değiştirilmiş sayfa listesi: {0}" },
            { "Standby list: {0}", "Bekleme listesi: {0}" },
            { "Low priority standby: {0}", "Düşük öncelikli bekleme: {0}" },
            { "System working sets: {0}", "Sistem çalışma setleri: {0}" },
            { "Uninstall · scan leftovers · force delete", "Program kaldır · kalıntı tara · zorla sil" },
            { "Remove + Scan", "Kaldır + Tara" },
            { "Remove", "Kaldır" },
            { "Scan leftovers", "Kalıntı Tara" },
            { "Force delete", "Zorla Sil" },
            { "Search programs...", "Program ara..." },
            { "Show system components", "Sistem bileşenlerini göster" },
            { "Installed Programs", "Kurulu Programlar" },
            { "registry", "kayıt defteri" },
            { "Program", "Program" },
            { "Publisher", "Yayıncı" },
            { "Version", "Sürüm" },
            { "Installed", "Kurulum" },
            { "No programs found", "Program bulunamadı" },
            { "Clear the search filter.", "Arama filtresini temizleyin." },
            { "Leftovers", "Kalıntılar" },
            { "Revo-style scan", "Revo tarzı tarama" },
            { "Delete selected", "Seçilenleri Sil" },
            { "Select all", "Tümünü Seç" },
            { "None", "Hiçbiri" },
            { "Select a program and use \"Remove + Scan\" to find leftovers.", "Bir program seçip \"Kaldır + Tara\" ile kalıntıları bulun." },
            { "No leftovers", "Kalıntı yok" },
            { "Leftovers are listed here.", "Kalan izler burada listelenir." },
            { "Type", "Tür" },
            { "Location", "Konum" },
            { "Confidence", "Güven" },
            { "Medium", "Orta" },
            { "File", "Dosya" },
            { "Select a program from the list first.", "Önce listeden bir program seçin." },
            { "Remove program", "Programı Kaldır" },
            { "\"{0}\" will be uninstalled. The program's own uninstaller will open, then a leftover scan runs.", "\"{0}\" kaldırılsın mı? Programın kendi kaldırıcısı açılacak. Ardından kalıntı taraması yapılır." },
            { "Uninstaller could not be started", "Kaldırıcı çalıştırılamadı" },
            { "Uninstaller could not be started: {0} You can try \"Force delete\".", "Kaldırıcı çalıştırılamadı: {0} \"Zorla Sil\" seçeneğini deneyebilirsiniz." },
            { "Leftover scan", "Kalıntı Taraması" },
            { "If the uninstall finished, we can scan for leftovers. Start the scan?", "Kaldırma tamamlandıysa kalıntı taramasına geçebiliriz. Tarama başlatılsın mı?" },
            { "\"{0}\" will be FORCE deleted. • Program processes are closed • The install folder is deleted • Registry leftovers are deleted This cannot be undone.", "\"{0}\" ZORLA silinsin mi? • Programın süreçleri kapatılır • Kurulum klasörü silinir • Bulunan kayıt defteri kalıntıları silinir Bu işlem geri alınamaz." },
            { "Force delete is running...", "Zorla silme çalışıyor..." },
            { "{0} processes closed · searching for leftovers...", "{0} süreç kapatıldı · kalıntılar aranıyor..." },
            { "Force deleted · {0} freed", "Zorla silindi · {0} boşaltıldı" },
            { " · {0} errors", " · {0} hata" },
            { "Scanning: {0}", "Taranıyor: {0}" },
            { "Scan error: {0}", "Tarama hatası: {0}" },
            { "{0} leftovers found · \"{1}\"", "{0} kalıntı bulundu · \"{1}\"" },
            { "No leftovers found · \"{0}\"", "Kalıntı bulunamadı · \"{0}\"" },
            { "Leftovers ({0})", "Kalıntılar ({0})" },
            { "Delete selected ({0})", "Seçilenleri Sil ({0})" },
            { "Delete leftovers", "Kalıntıları Sil" },
            { "{0} leftovers will be permanently deleted. Continue?", "{0} kalıntı kalıcı olarak silinecek. Devam edilsin mi?" },
            { "Delete", "Sil" },
            { "Deleted · {0} freed", "Silindi · {0} boşaltıldı" },
            { " · backup: MineManager-backup\\{0}", " · yedek: MineManager-backup\\{0}" },
            { "{0} programs · {1}", "{0} program · {1}" },
            { "No uninstall command is registered for this program.", "Bu program için kaldırma komutu kayıtlı değil." },
            { "The uninstaller could not be started.", "Kaldırıcı başlatılamadı." },
            { "Registry scanning...", "Kayıt defteri taranıyor..." },
            { "Deleted: {0}", "Silindi: {0}" },
            { "Deleted: {0} \\ {1}", "Silindi: {0} \\ {1}" },
            { "Failed: {0}", "Silinemedi: {0}" },
            { "Keywords: {0}", "Anahtar kelimeler: {0}" },
            { "Error: {0} ({1})", "Hata: {0} ({1})" },
            { "Partially: {0}", "Kısmen: {0}" },
            { "CPU (processor)", "İşlemci (CPU)" },
            { "Name", "Ad" },
            { "Manufacturer", "Üretici" },
            { "Identifier", "Tanımlayıcı" },
            { "Cores / threads", "Çekirdek / İş Parçacığı" },
            { "{0} cores / {1} threads", "{0} çekirdek / {1} iş parçacığı" },
            { "Maximum speed", "Maksimum Hız" },
            { "{0} MHz (current {1} MHz)", "{0} MHz (anlık {1} MHz)" },
            { "L2 cache", "L2 Önbellek" },
            { "L3 cache", "L3 Önbellek" },
            { "Socket", "Soket" },
            { "Architecture", "Mimari" },
            { "Virtualization", "Sanallaştırma" },
            { "Current load", "Anlık Yük" },
            { "Motherboard & BIOS", "Anakart & BIOS" },
            { "Motherboard manufacturer", "Anakart Üreticisi" },
            { "Motherboard model", "Anakart Modeli" },
            { "Motherboard version", "Anakart Sürümü" },
            { "Motherboard serial", "Anakart Seri No" },
            { "BIOS manufacturer", "BIOS Üreticisi" },
            { "BIOS version", "BIOS Sürümü" },
            { "BIOS date", "BIOS Tarihi" },
            { "System manufacturer", "Sistem Üreticisi" },
            { "System model", "Sistem Modeli" },
            { "System type", "Sistem Türü" },
            { "Memory (RAM)", "Bellek (RAM)" },
            { "Installed memory", "Kurulu Bellek" },
            { "Installed RAM", "Kurulu RAM" },
            { "Available memory", "Kullanılabilir Bellek" },
            { "{0}  ({1} in use)", "{0}  ({1} kullanımda)" },
            { "Module {0}", "Modül {0}" },
            { "Graphics", "Ekran Kartı" },
            { "Driver {0}", "Sürücü {0}" },
            { "Graphics {0}", "Ekran Kartı {0}" },
            { "Drive {0}", "Sürücü {0}" },
            { "{0} / {1}  ({2} used, {3} free) · {4} {5}", "{0} / {1}  ({2} dolu, {3} boş) · {4} {5}" },
            { "Operating System", "İşletim Sistemi" },
            { "Build {0}", "Derleme {0}" },
            { "Install date", "Kurulum Tarihi" },
            { "Last boot", "Son Açılış" },
            { "Uptime", "Çalışma Süresi" },
            { "{0} days {1} hours {2} min", "{0} gün {1} saat {2} dk" },
            { "Computer name", "Bilgisayar Adı" },
            { "User", "Kullanıcı" },
            { "Network", "Ağ" },
            { "Other devices", "Diğer Aygıtlar" },
            { "Sound", "Ses" },
            { "Battery", "Batarya" },
            { "Monitor", "Monitör" },
            { "Keyboard", "Klavye" },
            { "Enabled", "Etkin" },
            { "Disabled", "Devre dışı" },
            { "Processor information is being read...", "İşlemci bilgisi okunuyor..." },
            { "Motherboard and BIOS are being read...", "Anakart ve BIOS okunuyor..." },
            { "Memory modules are being read...", "Bellek modülleri okunuyor..." },
            { "Graphics card is being read...", "Ekran kartı okunuyor..." },
            { "Disks are being read...", "Diskler okunuyor..." },
            { "Operating system information is being read...", "İşletim sistemi bilgisi okunuyor..." },
            { "Network and other devices are being read...", "Ağ ve diğer aygıtlar okunuyor..." },
            { "Hardware Report", "Donanım Raporu" },
            { "Hardware Information", "Donanım Bilgisi" },
            { "Loading...", "Yükleniyor..." },
            { "Collecting hardware information...", "Donanım bilgileri toplanıyor..." },
            { "{0} sections read · {1}", "{0} bölüm okundu · {1}" },
            { "Hardware information could not be read · Try Refresh.", "Donanım bilgisi okunamadı · Yenile'yi deneyin." },
            { "Report copied to clipboard.", "Rapor panoya kopyalandı." },
            { "Hardware report copied to clipboard", "Donanım raporu panoya kopyalandı" },
            { "Copy failed: {0}", "Kopyalanamadı: {0}" },
            { "Text file|*.txt", "Metin dosyası|*.txt" },
            { "hardware-report", "donanim-raporu" },
            { "Saved: {0}", "Kaydedildi: {0}" },
            { "Report saved", "Rapor kaydedildi" },
            { "Save failed: {0}", "Kaydedilemedi: {0}" },
            { "{0} items", "{0} bilgi" },
            { "Scanning shortcuts...", "Kısayollar taranıyor..." },
            { "Activity log", "İşlem günlüğü" },
            { "Navigation", "Gezinme" },
            { "Scroll", "Kaydırma" },
            { "Notification", "Bildirim" },
            { "{0} keys backed up → {1}", "{0} anahtar yedeklendi → {1}" },
        };

        // Fragment dictionaries (uninstaller, tweaks, config) register here so those
        // files stay independent of this one. Tr[] is initialized first: static field
        // initializers run before the static constructor body.
        static L()
        {
            LangUninstall.Register(Tr);
            LangTweaks.Register(Tr);
            LangTweaksUi.Register(Tr);
            LangConfig.Register(Tr);
        }

        public static bool IsTr { get { return Lang == "tr"; } }

        public static void SetLanguage(string code)
        {
            Lang = string.Equals(code, "tr", StringComparison.OrdinalIgnoreCase) ? "tr" : "en";
            Culture = CultureInfo.GetCultureInfo(IsTr ? "tr-TR" : "en-US");
        }

        public static string T(string en)
        {
            if (en == null) return "";
            if (IsTr)
            {
                string tr;
                if (Tr.TryGetValue(en, out tr)) return tr;
            }
            return en;
        }

        public static string F(string en, params object[] args)
        {
            return string.Format(Culture, T(en), args);
        }

        public static string Percent(int n)
        {
            return IsTr ? "%" + n.ToString(Culture) : n.ToString(Culture) + "%";
        }

        public static string Count(long n, string enOne, string enMany, string trMany)
        {
            if (IsTr) return Fmt.Num(n) + " " + trMany;
            return Fmt.Num(n) + " " + (n == 1 ? enOne : enMany);
        }
    }
}
