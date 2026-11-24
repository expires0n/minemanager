using System.Collections.Generic;

namespace MineManager
{
    /// <summary>
    /// Turkish strings for the diagnostics toolbox, the error-code decoder, the
    /// Restart Manager query, the wider leftover scan and the backup cleanup.
    /// Registered from L's static constructor — English is always the fallback.
    /// </summary>
    internal static class LangDiag
    {
        public static void Register(Dictionary<string, string> d)
        {
            // -------------------------------------------------------- diagnostics
            d["Diagnostics"] = "Tanılama";
            d["Read-only tools · nothing is changed"] = "Salt okunur araçlar · hiçbir şey değiştirilmez";
            d["Restart Manager"] = "Yeniden Başlatma Yöneticisi";
            d["File or folder path"] = "Dosya veya klasör yolu";
            d["Browse"] = "Gözat";
            d["Check"] = "Kontrol Et";
            d["Enter a file path first."] = "Önce bir dosya yolu girin.";
            d["Not found: {0}"] = "Bulunamadı: {0}";
            d["Error code decoder"] = "Hata Kodu Çözümleyici";
            d["Code (decimal or 0x…)"] = "Kod (ondalık ya da 0x…)";
            d["Explain"] = "Açıkla";
            d["That is not a number. Examples: 5, 0x80070005, -2147024891"] =
                "Bu bir sayı değil. Örnekler: 5, 0x80070005, -2147024891";
            d["Battery & sleep"] = "Pil ve Uyku";
            d["powercfg report"] = "powercfg raporu";
            d["Reported capacity vs. design capacity"] = "Bildirilen kapasite / tasarım kapasitesi";
            d["Read"] = "Oku";
            d["Reading..."] = "Okunuyor...";
            d["Battery: {0}"] = "Pil: {0}";
            d["Design capacity: {0}"] = "Tasarım kapasitesi: {0}";
            d["Reported full charge: {0}"] = "Bildirilen tam dolum kapasitesi: {0}";
            d["Reported / design: {0}"] = "Bildirilen / tasarım: {0}";
            d["Capacity ratio: no data"] = "Kapasite oranı: veri yok";
            d["Cycle count: {0}"] = "Döngü sayısı: {0}";
            d["A missing value is not zero — it means the data is not reported."] =
                "Eksik değer sıfır demek değildir — bu verinin bildirilmediği anlamına gelir.";
            d["No battery information on this machine."] = "Bu makinede pil bilgisi yok.";
            d["No battery report was produced."] = "Pil raporu üretilemedi.";
            d["Creating the battery report..."] = "Pil raporu oluşturuluyor...";
            d["no data"] = "veri yok";
            d["System summary"] = "Sistem özeti";
            d["read-only overview"] = "salt okunur özet";
            d["Build"] = "Derleme";
            d["Administrator"] = "Yönetici";
            d["Standard user"] = "Standart kullanıcı";
            d["System drive unavailable"] = "Sistem sürücüsü kullanılamıyor";
            d["{0}d {1}h {2}m"] = "{0}g {1}sa {2}dk";
            d["{0}h {1}m"] = "{0}sa {1}dk";
            d["{0}m"] = "{0}dk";
            d["{0} {1} free / {2}"] = "{0} {1} boş / {2}";
            d["Time since the last boot."] = "Son açılıştan bu yana geçen süre.";
            d["Free space on the Windows drive."] = "Windows sürücüsündeki boş alan.";
            d["Running as administrator."] = "Yönetici olarak çalışıyor.";
            d["Running as a standard user."] = "Standart kullanıcı olarak çalışıyor.";
            d["Choose the folder to check."] = "Kontrol edilecek klasörü seçin.";
            d["File…"] = "Dosya…";
            d["Folder…"] = "Klasör…";
            d["Choose a file or folder, then run the check."] = "Bir dosya veya klasör seçin, sonra kontrolü çalıştırın.";
            d["Enter a code to see what it means."] = "Anlamını görmek için bir kod girin.";
            d["Read the battery report — no changes."] = "Pil raporunu okuyun — hiçbir şey değişmez.";
            d["Measure now; measure again after a change to compare."] = "Şimdi ölçün; değişiklikten sonra karşılaştırmak için tekrar ölçün.";
            d["Enter an IP address to see the local route."] = "Yerel rotayı görmek için bir IP adresi girin.";
            d["Read recent maintenance events."] = "Son bakım olaylarını okuyun.";
            d["Checking..."] = "Kontrol ediliyor...";
            d["Reading the battery report..."] = "Pil raporu okunuyor...";
            d["Reading event records..."] = "Olay kayıtları okunuyor...";
            d["Look back at most 720 hours (30 days)."] = "En fazla 720 saat (30 gün) geriye bakın.";

            d["Display connections"] = "Ekran Bağlantıları";
            d["measure · compare"] = "ölç · karşılaştır";
            d["No measurement yet"] = "Henüz ölçüm yok";
            d["Measure"] = "Ölç";
            d["Compare"] = "Karşılaştır";
            d["Measurement: {0} active path(s)"] = "Ölçüm: {0} etkin yol";
            d["Baseline stored — measure again after the change"] =
                "Temel ölçüm alındı — değişiklikten sonra tekrar ölçün";
            d["Compared — the new measurement is the baseline now"] =
                "Karşılaştırıldı — yeni ölçüm artık temel ölçümdür";
            d["Unnamed display"] = "Adsız ekran";
            d["not active"] = "etkin değil";
            d["No active display path was reported."] = "Etkin bir ekran yolu bildirilmedi.";
            d["The display topology changed while it was read."] =
                "Ekran topolojisi okunurken değişti.";
            d["Internal"] = "Dahili";
            d["+ appeared: {0}"] = "+ eklendi: {0}";
            d["- disappeared: {0}"] = "- kayboldu: {0}";
            d["· changed: {0} → {1}"] = "· değişti: {0} → {1}";
            d["No difference between the two measurements."] = "İki ölçüm arasında fark yok.";
            d["A missing active path is not proof of a broken cable; only the topology is compared."] =
                "Etkin yolun görünmemesi bozuk kablo kanıtı değildir; yalnızca topoloji karşılaştırılır.";

            d["Which path does Windows use?"] = "Windows hangi yolu kullanır?";
            d["route lookup"] = "rota sorgusu";
            d["Destination IP (numeric)"] = "Hedef IP (sayısal)";
            d["The route Windows picked for this destination:"] =
                "Windows'un bu hedef için seçtiği rota:";
            d["Destination"] = "Hedef";
            d["Source address"] = "Kaynak adres";
            d["Interface"] = "Arabirim";
            d["Next hop"] = "Sonraki atlama";
            d["This is a local routing decision only — it does not test the destination."] =
                "Bu yalnızca yerel bir yönlendirme kararıdır — hedefi test etmez.";
            d["Enter a numeric IPv4 or IPv6 address. DNS names are not resolved here."] =
                "Sayısal bir IPv4 veya IPv6 adresi girin. DNS adları burada çözülmez.";
            d["The route query failed (error {0})."] = "Rota sorgusu başarısız (hata {0}).";
            d["Interface #{0}"] = "Arabirim #{0}";

            d["Maintenance timeline"] = "Bakım Zaman Çizelgesi";
            d["Windows event records"] = "Windows olay kayıtları";
            d["Look back (hours)"] = "Geriye dön (saat)";
            d["Enter a positive number of hours."] = "Pozitif bir saat sayısı girin.";
            d["No matching event in this period."] = "Bu dönemde eşleşen olay yok.";
            d["These events are close in time; the records alone do not show that one caused the other."] =
                "Bu olaylar zaman bakımından yakın; kayıtlar tek başına birinin diğerine neden olduğunu göstermez.";
            d["{0}: {1}"] = "{0}: {1}";

            // ------------------------------------------------------------ file lock
            d["What is using this file?"] = "Bu dosyayı ne kullanıyor?";
            d["No file path."] = "Dosya yolu yok.";
            d["Restart Manager could not be started (error {0})."] =
                "Yeniden Başlatma Yöneticisi başlatılamadı (hata {0}).";
            d["The file could not be registered (error {0})."] =
                "Dosya kaydedilemedi (hata {0}).";
            d["Restart Manager reported no application for this file."] =
                "Yeniden Başlatma Yöneticisi bu dosya için bir uygulama bildirmedi.";
            d["Query failed (error {0})."] = "Sorgu başarısız (hata {0}).";
            d["The query failed (error {0})."] = "Sorgu başarısız (hata {0}).";
            d["Close these programs (save your work first) and try the operation again."] =
                "Bu programları kapatın (önce işinizi kaydedin) ve işlemi yeniden deneyin.";
            d["An empty result is not a guarantee that the file is free."] =
                "Boş sonuç dosyanın kesin serbest olduğunun garantisi değildir.";
            d["{0} application(s):"] = "{0} uygulama:";
            d["restartable"] = "yeniden başlatılabilir";
            d["Service"] = "Hizmet";
            d["Console"] = "Konsol";
            d["Critical process"] = "Kritik süreç";
            d["Application"] = "Uygulama";
            d["These items could not be deleted. Restart Manager reports who holds them."] =
                "Bu öğeler silinemedi. Yeniden Başlatma Yöneticisi kimin tuttuğunu bildiriyor.";

            // ------------------------------------------------------- error decoder
            d["Success."] = "Başarılı.";
            d["Success. No error occurred."] = "Başarılı. Hata oluşmadı.";
            d["No local description."] = "Yerel açıklama yok.";
            d["No local description. The value is often a vendor exit code — its meaning is defined by that program."] =
                "Yerel açıklama yok. Bu değer çoğu zaman üreticinin kendi çıkış kodudur — anlamını o program belirler.";
            d["Value: {0}  (0x{1:X8})"] = "Değer: {0}  (0x{1:X8})";
            d["HRESULT 0x{0:X8} → Win32 {1}: "] = "HRESULT 0x{0:X8} → Win32 {1}: ";
            d["HRESULT 0x{0:X8} · facility {1} · code {2}"] = "HRESULT 0x{0:X8} · tesis {1} · kod {2}";
            d["HRESULT · facility {0} · code {1}"] = "HRESULT · tesis {0} · kod {1}";
            d["Win32 {0}: "] = "Win32 {0}: ";
            d["Windows Installer (MSI) code {0}"] = "Windows Installer (MSI) kodu {0}";
            d["The queue waits and retries the same item a few times before it stops."] =
                "Kuyruk durmadan önce aynı öğeyi birkaç kez bekleyip yeniden dener.";
            d["Run the uninstall again with an MSI log to see the failing step."] =
                "Başarısız adımı görmek için kaldırmayı MSI günlüğüyle yeniden çalıştırın.";
            d["This is a success result: the change is applied, a restart completes it."] =
                "Bu bir başarı sonucudur: değişiklik uygulandı, tamamlanması için yeniden başlatma gerekir.";
            d["This HRESULT does not come from a Win32 error code; the raw value is the reliable information here."] =
                "Bu HRESULT bir Win32 hata kodundan gelmiyor; burada güvenilir bilgi ham değerin kendisidir.";
            d["Exit code {0} · {1}"] = "Çıkış kodu {0} · {1}";
            d["Windows Installer service could not be accessed."] =
                "Windows Installer hizmetine erişilemedi.";
            d["Cancelled by the user."] = "Kullanıcı iptal etti.";
            d["Fatal error during installation. The detailed MSI log has the real cause."] =
                "Kurulum sırasında ölümcül hata. Gerçek neden ayrıntılı MSI günlüğündedir.";
            d["This product is not installed (unknown product code)."] =
                "Bu ürün kurulu değil (bilinmeyen ürün kodu).";
            d["The installation source for this product is not available."] =
                "Bu ürünün kurulum kaynağı bulunamıyor.";
            d["Another installation is in progress. Wait and try again."] =
                "Başka bir kurulum sürüyor. Bekleyip yeniden deneyin.";
            d["The installation package could not be opened."] =
                "Kurulum paketi açılamadı.";
            d["The installation package could not be opened (invalid Windows Installer package)."] =
                "Kurulum paketi açılamadı (geçersiz Windows Installer paketi).";
            d["System policy forbids this installation."] =
                "Sistem ilkesi bu kurulumu engelliyor.";
            d["Another version of this product is already installed."] =
                "Bu ürünün başka bir sürümü zaten kurulu.";
            d["Invalid command line argument."] = "Geçersiz komut satırı argümanı.";
            d["Success — the installer started a restart."] =
                "Başarılı — kurulum programı yeniden başlatma başlattı.";
            d["Success — a restart is required to finish."] =
                "Başarılı — tamamlanması için yeniden başlatma gerekir.";

            // ------------------------------------------------------------- leftovers
            d["Scheduled task"] = "Zamanlanmış Görev";
            d["PATH entry"] = "PATH Girdisi";
            d["Registry integrations..."] = "Kayıt defteri entegrasyonları taranıyor...";
            d["Scanning scheduled tasks..."] = "Zamanlanmış görevler taranıyor...";
            d["Scanning loose files..."] = "Dağınık dosyalar taranıyor...";
            d["Clean backups"] = "Yedekleri Temizle";
            d["Delete MineManager's .reg backup packages"] =
                "MineManager'ın .reg yedek paketlerini sil";
            d["There are no backup packages."] = "Yedek paketi yok.";
            d["Deleting backup packages..."] = "Yedek paketleri siliniyor...";
            d["{0} files"] = "{0} dosya";
            d["{0} backup packages · {1} will be deleted permanently."] =
                "{0} yedek paketi · {1} kalıcı olarak silinecek.";
            d["Undo through MineManager ends for these packages. Registry keys cannot be restored from them anymore."] =
                "Bu paketler için MineManager ile geri alma sona erer. Kayıt defteri anahtarları artık bunlardan geri yüklenemez.";
            d["{0} leftovers will be permanently deleted. Registry keys and values are exported to .reg first. Continue?"] =
                "{0} kalıntı kalıcı olarak silinecek. Kayıt defteri anahtarları ve değerleri önce .reg olarak dışa aktarılır. Devam edilsin mi?";

            // ---------------------------------------------------------- uninstaller
            d["Waiting (the queue stopped after a restart was started)."] =
                "Bekliyor (kuyruk, yeniden başlatma başladığı için durdu).";
            d["Another installation is in progress — waiting {0}s (retry {1}/4)..."] =
                "Başka bir kurulum sürüyor — {0} sn bekleniyor (yeniden deneme {1}/4)...";

            // ---------------------------------------------------------------- memory
            d["No clean has run yet."] = "Henüz bir temizlik çalışmadı.";
            d["Available: {0} → {1}"] = "Kullanılabilir: {0} → {1}";
            d["  ·  after 30 s: {0}"] = "  ·  30 sn sonra: {0}";
            d["30 s later: available {0}"] = "30 sn sonra: kullanılabilir {0}";
            d["system-wide change"] = "sistem genelindeki değişim";
            d["The change is system-wide; it is not all freed by MineManager."] =
                "Değişim sistem genelindedir; tamamı MineManager tarafından boşaltılmış sayılmaz.";

            // ------------------------------------------------------- safety / sharing
            d["Cleaned · {0} files changed since the scan were left alone"] =
                "Temizlendi · taramadan sonra değişen {0} dosyaya dokunulmadı";
            d["SKIPPED {0} - it changed after MineManager applied it (now: {1}, MineManager wrote: {2}). Left as it is."] =
                "ATLANDI {0} - MineManager uyguladıktan sonra değişmiş (şimdi: {1}, MineManager yazdığı: {2}). Olduğu gibi bırakıldı.";
            d["Verification differs because {0} step(s) were skipped (changed by something else)."] =
                "Doğrulama farklı çıktı çünkü {0} adım atlandı (başkası değiştirmiş).";
            d["{0}: the source files are not on this machine and Windows Update is not used (offline policy). Add a local source or install the feature from Settings > Apps > Optional features."] =
                "{0}: kaynak dosyalar bu makinede yok ve Windows Update kullanılmıyor (çevrimdışı ilke). Yerel bir kaynak ekleyin ya da Ayarlar > Uygulamalar > İsteğe bağlı özellikler yolundan kurun.";
            d["Changes since the last reading"] = "Son okumadan bu yana değişenler";
            d["No change since the last reading."] = "Son okumadan bu yana değişiklik yok.";
            d[" · {0} changes since the last reading"] = " · son okumadan bu yana {0} değişiklik";
            d["+ data appeared: {0} = {1}"] = "+ veri eklendi: {0} = {1}";
            d["- no data this time: {0} (was: {1})"] = "- bu kez veri yok: {0} (önceki: {1})";
            d["- missing: {0} (was: {1})"] = "- eksik: {0} (önceki: {1})";
            d["Share the hardware report"] = "Donanım raporunu paylaş";
            d["This is the exact text that will be shared. Identifiers are masked by default."] =
                "Paylaşılacak metin aynen budur. Tanımlayıcılar varsayılan olarak maskelenir.";
            d["Mask identifiers (computer, user, serial numbers, addresses)"] =
                "Tanımlayıcıları maskele (bilgisayar, kullanıcı, seri numaraları, adresler)";
            d["Continue"] = "Devam";
        }
    }
}