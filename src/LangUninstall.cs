using System;
using System.Collections.Generic;

namespace MineManager
{
    /// <summary>Turkish strings for the uninstaller page. Owner: uninstaller work package.</summary>
    internal static class LangUninstall
    {
        public static void Register(Dictionary<string, string> d)
        {
            // ------------------------------------------------------- page / buttons
            d["Remove selected"] = "Seçilenleri Kaldır";
            d["Details"] = "Ayrıntılar";
            d["Export CSV"] = "CSV Dışa Aktar";
            d["Quiet"] = "Sessiz";
            d["Stop"] = "Durdur";
            d["Stopping after the current program..."] = "Geçerli programdan sonra durdurulacak...";
            d["Invalid"] = "Geçersiz";
            d["Yes"] = "Evet";
            d["No"] = "Hayır";
            d["(none)"] = "(yok)";
            d["(synthesized)"] = "(otomatik oluşturuldu)";
            d["installed-programs"] = "kurulu-programlar";
            d["CSV file|*.csv"] = "CSV dosyası|*.csv";
            d["Program list exported"] = "Program listesi dışa aktarıldı";
            d["Export failed"] = "Dışa aktarma başarısız";
            d["Copied to clipboard"] = "Panoya kopyalandı";

            d["Quiet mode asks for confirmation before every silent uninstall."] =
                "Sessiz mod, her sessiz kaldırmadan önce onay ister.";
            d["Uninstall without dialogs (MSI, Inno Setup and NSIS only). Asks for confirmation."] =
                "İletişim kutusu göstermeden kaldır (yalnızca MSI, Inno Setup ve NSIS). Onay ister.";
            d["Loading the installed program list..."] = "Kurulu program listesi yükleniyor...";
            d[" · {0} selected"] = " · {0} seçili";
            d["Invalid entry: the uninstaller no longer exists. Use Remove to clean up the registry key."] =
                "Geçersiz kayıt: kaldırıcı artık mevcut değil. Kayıt defteri anahtarını temizlemek için Kaldır'ı kullanın.";

            // ------------------------------------------------------------ installers
            d["MSI (Windows Installer)"] = "MSI (Windows Installer)";
            d["Inno Setup"] = "Inno Setup";
            d["NSIS"] = "NSIS";
            d["InstallShield"] = "InstallShield";
            d["Application shim (sdbinst)"] = "Uygulama düzeltmesi (sdbinst)";
            d["PowerShell"] = "PowerShell";
            d["Store app"] = "Mağaza uygulaması";
            d["Unknown"] = "Bilinmiyor";
            d["Windows Installer"] = "Windows Installer";

            // -------------------------------------------------------------- details
            d["Program details"] = "Program ayrıntıları";
            d["64-bit"] = "64 bit";
            d["System component"] = "Sistem bileşeni";
            d["No remove"] = "Kaldırılamaz";
            d["Invalid entry"] = "Geçersiz kayıt";
            d["Comment"] = "Açıklama";
            d["Install location"] = "Kurulum konumu";
            d["Install source"] = "Kurulum kaynağı";
            d["Display icon"] = "Simge";
            d["Modify command"] = "Değiştirme komutu";
            d["About URL"] = "Web adresi";
            d["Release type"] = "Sürüm türü";
            d["Product code"] = "Ürün kodu";
            d["Uninstall command"] = "Kaldırma komutu";
            d["Quiet command"] = "Sessiz kaldırma komutu";
            d["Quiet uninstall possible"] = "Sessiz kaldırma desteklenir";
            d["Quiet uninstall command"] = "Sessiz kaldırma komutu";
            d["Install date"] = "Kurulum tarihi";
            d["Size (bytes)"] = "Boyut (bayt)";

            // ------------------------------------------------------------ uninstall
            d["\"{0}\" will be uninstalled with its own uninstaller. Continue?"] =
                "\"{0}\" kendi kaldırıcısıyla kaldırılacak. Devam edilsin mi?";
            d["{0} programs will be uninstalled with their own uninstallers. Continue?"] =
                "{0} program kendi kaldırıcılarıyla kaldırılacak. Devam edilsin mi?";
            d["... and {0} more"] = "... ve {0} tane daha";
            d["{0} invalid entries were skipped."] = "{0} geçersiz girdi atlandı.";
            d["Another task is running. Wait for it to finish."] =
                "Başka bir görev çalışıyor. Bitmesini bekleyin.";
            d["Starting the uninstall queue..."] = "Kaldırma sırası başlatılıyor...";
            d["{0}/{1} · {2} · {3}"] = "{0}/{1} · {2} · {3}";
            d["Running quiet uninstall..."] = "Sessiz kaldırma çalışıyor...";
            d["Waiting for the uninstaller to finish..."] = "Kaldırıcının bitmesi bekleniyor...";
            d["Uninstalled."] = "Kaldırıldı.";
            d["Uninstalled. A restart is required."] = "Kaldırıldı. Yeniden başlatma gerekiyor.";
            d["Uninstalled (registry entry removed)."] = "Kaldırıldı (kayıt defteri girdisi silindi).";
            d["Already removed (the product is not installed)."] = "Zaten kaldırılmış (ürün kurulu değil).";
            d["Skipped (the uninstaller was cancelled)."] = "Atlandı (kaldırıcı iptal edildi).";
            d["Skipped (invalid registry entry)."] = "Atlandı (geçersiz kayıt defteri girdisi).";
            d["Cancelled."] = "İptal edildi.";
            d["Failed."] = "Başarısız.";
            d["Failed (exit code {0})."] = "Başarısız (çıkış kodu {0}).";
            d["Another installation is in progress. Try again later."] =
                "Başka bir kurulum sürüyor. Daha sonra tekrar deneyin.";
            d["Timed out after 10 minutes; the process tree was closed."] =
                "10 dakika sonra zaman aşımına uğradı; süreç ağacı kapatıldı.";
            d["The uninstaller finished but the registry entry is still present."] =
                "Kaldırıcı tamamlandı ancak kayıt defteri girdisi hâlâ duruyor.";
            d["The uninstaller could not be started: {0}"] = "Kaldırıcı başlatılamadı: {0}";
            d["Uninstall finished."] = "Kaldırma tamamlandı.";
            d["Uninstall finished. A restart is required to complete it."] =
                "Kaldırma tamamlandı. Tamamlanması için yeniden başlatma gerekiyor.";

            // ------------------------------------------------------- quiet confirm
            d["Quiet uninstall"] = "Sessiz kaldırma";
            d["These programs will be uninstalled without any dialogs. Review every command line before you continue."] =
                "Bu programlar hiçbir iletişim kutusu gösterilmeden kaldırılacak. Devam etmeden önce her komut satırını inceleyin.";
            d["These programs have no silent command and will run interactively:"] =
                "Bu programlar için sessiz komut yok, etkileşimli çalışacaklar:";
            d["Uninstall quietly"] = "Sessiz kaldır";
            d["None of the selected programs supports a quiet uninstall. They will run interactively."] =
                "Seçilen programların hiçbiri sessiz kaldırmayı desteklemiyor. Etkileşimli olarak çalıştırılacaklar.";

            // ------------------------------------------------- invalid entry cleanup
            d["Remove entry"] = "Kaydı Sil";
            d["\"{0}\" points to an uninstaller that no longer exists. The registry key will be backed up and then deleted. Continue?"] =
                "\"{0}\" artık var olmayan bir kaldırıcıyı gösteriyor. Kayıt defteri anahtarı yedeklenip silinecek. Devam edilsin mi?";
            d["{0} invalid entries will have their registry keys backed up and then deleted. Continue?"] =
                "{0} geçersiz girdinin kayıt defteri anahtarı yedeklenip silinecek. Devam edilsin mi?";
            d["Removing the registry entries..."] = "Kayıt defteri girdileri siliniyor...";
            d["{0} entries removed."] = "{0} girdi silindi.";
            d[" · backup: {0}"] = " · yedek: {0}";
            d[" · {0} failed"] = " · {0} başarısız";
            d["The registry key could not be backed up, so it was not removed."] =
                "Kayıt defteri anahtarı yedeklenemediği için silinmedi.";
            d["The registry key could not be deleted."] = "Kayıt defteri anahtarı silinemedi.";
            d["This entry has no registry key."] = "Bu girdinin kayıt defteri anahtarı yok.";

            // ------------------------------------------------------------ leftovers
            d["No leftovers found"] = "Kalıntı bulunamadı";
            d["{0} leftovers found · {1}"] = "{0} kalıntı bulundu · {1}";
        }
    }
}
