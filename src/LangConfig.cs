using System;
using System.Collections.Generic;

namespace MineManager
{
    /// <summary>
    /// Turkish strings for the config page, its data and its engine.
    /// Owner: config work package. Keys are the exact English text used at runtime;
    /// anything the WinUtil data already covers in another fragment is not repeated here.
    /// </summary>
    internal static class LangConfig
    {
        public static void Register(Dictionary<string, string> d)
        {
            // ------------------------------------------------------- group cards
            d["Features"] = "Özellikler";
            d["Fixes"] = "Onarımlar";
            d["Legacy Windows Panels"] = "Eski Windows Panelleri";
            d["Powershell Profile Powershell 7+ Only"] = "PowerShell Profili Yalnızca PowerShell 7+";
            d["Remote Access"] = "Uzak Erişim";
            d["Features · fixes · legacy panels"] = "Özellikler · onarımlar · eski paneller";

            // ------------------------------------------------------------- page
            d["Install"] = "Kur";
            d["Install Features"] = "Özellikleri Kur";
            d["Tick at least one feature first."] = "Önce en az bir özellik işaretleyin.";
            d["These Windows features will be installed:"] = "Şu Windows özellikleri kurulacak:";
            d["A restart may be required. DISM output is written to the log."] =
                "Yeniden başlatma gerekebilir. DISM çıktısı günlüğe yazılır.";
            d["… and {0} more"] = "… ve {0} tane daha";
            d["Not enabled"] = "Etkin değil";
            d["Partly enabled"] = "Kısmen etkin";
            // L.Count uses the Turkish form directly; the English forms are listed so a full
            // key audit of the page shows no gaps.
            d["feature selected"] = "özellik seçili";
            d["features selected"] = "özellik seçili";
            d["Reading feature state..."] = "Özellik durumu okunuyor...";
            d["Reading feature state again..."] = "Özellik durumu yeniden okunuyor...";
            d["Administrator rights are required for this action."] =
                "Bu işlem için yönetici hakları gerekiyor.";
            d["Started: {0}"] = "Başlatıldı: {0}";
            d["Working: {0}"] = "Çalışıyor: {0}";
            d["Done: {0}"] = "Tamamlandı: {0}";
            d["Failed: {0}"] = "Başarısız: {0}";
            d["Restart the computer to finish."] = "Tamamlamak için bilgisayarı yeniden başlatın.";
            d["Cancel requested — stopping after the current step..."] =
                "İptal istendi — geçerli adımdan sonra durulacak...";
            d["Cancelling..."] = "İptal ediliyor...";

            // ---------------------------------------------------------- failures
            d["No action was selected."] = "Hiçbir işlem seçilmedi.";
            d["This action has no handler."] = "Bu işlemin bir uygulayıcısı yok.";
            d["This panel has no command."] = "Bu panelin bir komutu yok.";
            d["The action failed unexpectedly."] = "İşlem beklenmedik şekilde başarısız oldu.";
            d["Cancelled."] = "İptal edildi.";
            d["{0} failed."] = "{0} başarısız oldu.";
            d["{0} timed out."] = "{0} zaman aşımına uğradı.";
            d["{0} failed with exit code {1}."] = "{0} {1} çıkış koduyla başarısız oldu.";
            d["{0} could not be opened."] = "{0} açılamadı.";
            d["Opening {0}..."] = "{0} açılıyor...";

            // ---------------------------------------------------------- features
            d["Installing {0}..."] = "{0} kuruluyor...";
            d["{0} is enabled."] = "{0} etkinleştirildi.";
            d["{0} is enabled and needs a restart."] = "{0} etkinleştirildi ve yeniden başlatma gerekiyor.";
            d["{0} did not finish within {1} seconds."] = "{0} {1} saniye içinde tamamlanmadı.";
            d["A restart is required to finish installing the features."] =
                "Özelliklerin kurulumunu tamamlamak için yeniden başlatma gerekiyor.";

            // --------------------------------------------------------------- ntp
            d["Starting the Windows Time service..."] = "Windows Saat hizmeti başlatılıyor...";
            d["Setting pool.ntp.org as the time server..."] =
                "Zaman sunucusu pool.ntp.org olarak ayarlanıyor...";
            d["Configuring the time server"] = "Zaman sunucusunu ayarlama";
            d["Restarting the Windows Time service..."] =
                "Windows Saat hizmeti yeniden başlatılıyor...";
            d["Synchronising the clock..."] = "Saat eşitleniyor...";
            d["The clock could not be synchronised yet; Windows retries automatically."] =
                "Saat henüz eşitlenemedi; Windows otomatik olarak yeniden deneyecek.";
            d["The time server is now pool.ntp.org."] = "Zaman sunucusu artık pool.ntp.org.";

            // ----------------------------------------------------------- network
            d["Resetting Winsock..."] = "Winsock sıfırlanıyor...";
            d["Resetting Winsock"] = "Winsock sıfırlama";
            d["Resetting the TCP/IP stack..."] = "TCP/IP yığını sıfırlanıyor...";
            d["Resetting the TCP/IP stack"] = "TCP/IP yığınını sıfırlama";
            d["The network stack has been reset. Restart the computer to finish."] =
                "Ağ yığını sıfırlandı. Tamamlamak için bilgisayarı yeniden başlatın.";

            // ------------------------------------------------------ system repair
            d["Checking the disk for errors..."] = "Diskteki hatalar denetleniyor...";
            d["The disk check did not finish in time."] = "Disk denetimi zamanında tamamlanmadı.";
            d["The disk could not be checked, so the scan was stopped here."] =
                "Disk denetlenemedi, bu yüzden tarama burada durduruldu.";
            d["The disk check failed with exit code {0}."] =
                "Disk denetimi {0} çıkış koduyla başarısız oldu.";
            d["Disk errors were found and fixed."] = "Disk hataları bulundu ve onarıldı.";
            d["The disk is clean."] = "Disk temiz.";
            d["Scanning protected system files..."] = "Korunan sistem dosyaları taranıyor...";
            d["The system file scan"] = "Sistem dosyası taraması";
            d["Repairing the Windows image..."] = "Windows imajı onarılıyor...";
            d["The image was repaired. A restart is required."] =
                "İmaj onarıldı. Yeniden başlatma gerekiyor.";
            d["The image repair"] = "İmaj onarımı";
            d["The system repair finished."] = "Sistem onarımı tamamlandı.";

            // ------------------------------------------------------ update reset
            d["Stopping Windows Update services..."] = "Windows Update hizmetleri durduruluyor...";
            d["Starting Windows Update services..."] = "Windows Update hizmetleri başlatılıyor...";
            d["Windows Update has been reset. Restart the computer."] =
                "Windows Update sıfırlandı. Bilgisayarı yeniden başlatın.";
            d["{0} was not found, nothing to rename."] =
                "{0} bulunamadı, yeniden adlandırılacak bir şey yok.";
            d["Renamed {0}."] = "{0} yeniden adlandırıldı.";
            d["{0} could not be renamed."] = "{0} yeniden adlandırılamadı.";

            // ------------------------------------------------------------ winget
            d["Checking WinGet..."] = "WinGet denetleniyor...";
            d["WinGet answers correctly. There is nothing to repair."] =
                "WinGet düzgün yanıt veriyor. Onarılacak bir şey yok.";
            d["WinGet did not answer. Repairing the App Installer package..."] =
                "WinGet yanıt vermedi. App Installer paketi onarılıyor...";
            d["The App Installer package was reset."] = "App Installer paketi sıfırlandı.";
            d["The App Installer package was re-registered from its existing files."] =
                "App Installer paketi mevcut dosyalarından yeniden kaydedildi.";
            d["WinGet is not installed on this system. Install App Installer from the Microsoft Store."] =
                "WinGet bu sistemde kurulu değil. App Installer'ı Microsoft Store'dan kurun.";
            d["The App Installer package is damaged beyond repair. Install it again from the Microsoft Store."] =
                "App Installer paketi onarılamayacak kadar bozuk. Microsoft Store'dan yeniden kurun.";
            d["The App Installer package could not be repaired."] =
                "App Installer paketi onarılamadı.";
            d["Checking WinGet again..."] = "WinGet yeniden denetleniyor...";
            d["WinGet works again."] = "WinGet yeniden çalışıyor.";
            d["WinGet still does not answer. Repair or reinstall App Installer from the Microsoft Store."] =
                "WinGet hâlâ yanıt vermiyor. App Installer'ı Microsoft Store'dan onarın veya yeniden kurun.";

            // ---------------------------------------------------------- autologon
            d["The Winlogon registry key could not be opened."] =
                "Winlogon kayıt defteri anahtarı açılamadı.";
            d["Automatic sign-in could not be turned off."] = "Otomatik oturum açma kapatılamadı.";
            d["Automatic sign-in is now off."] = "Otomatik oturum açma artık kapalı.";
            d["Automatic sign-in could not be turned on."] = "Otomatik oturum açma açılamadı.";
            d["Automatic sign-in is set up for {0}."] = "Otomatik oturum açma {0} için ayarlandı.";
            d["Windows still needs the password before it can sign in on its own."] =
                "Windows'un kendi kendine oturum açabilmesi için parola hâlâ gerekiyor.";
            d["Run netplwiz, clear \"Users must enter a user name and password\" and enter it there."] =
                "netplwiz'i çalıştırın, \"Kullanıcı adı ve parola girilmesi gerekir\" seçeneğini " +
                "kaldırın ve parolayı orada girin.";
            d["Windows keeps that password in plain text in the registry. Run this action again to turn it off."] =
                "Windows bu parolayı kayıt defterinde düz metin olarak saklar. Kapatmak için bu " +
                "işlemi yeniden çalıştırın.";

            // ---------------------------------------------------------- regbackup
            d["The registry backup script could not be written."] =
                "Kayıt defteri yedekleme betiği yazılamadı.";
            d["Created {0}."] = "{0} oluşturuldu.";
            d["Creating the scheduled task"] = "Zamanlanmış görevi oluşturma";
            d["The daily registry backup task is created (12:30, as SYSTEM)."] =
                "Günlük kayıt defteri yedekleme görevi oluşturuldu (12:30, SYSTEM olarak).";
            d["Running the backup once to check it works..."] =
                "Çalıştığını denetlemek için yedek bir kez çalıştırılıyor...";
            d["Backup files are in {0}."] = "Yedek dosyaları {0} içinde.";
            d["The first run could not be verified yet; the task runs daily."] =
                "İlk çalıştırma henüz doğrulanamadı; görev her gün çalışır.";
            d["The first run could not be started; the task still runs daily."] =
                "İlk çalıştırma başlatılamadı; görev yine de her gün çalışır.";

            // ---------------------------------------------------------- boot menu
            d["Changing the boot menu policy"] = "Önyükleme menüsü ilkesini değiştirme";
            d["The legacy boot menu (F8) is enabled."] = "Eski önyükleme menüsü (F8) etkinleştirildi.";
            d["The standard boot menu is restored."] = "Standart önyükleme menüsü geri yüklendi.";

            // ------------------------------------------------------------ profile
            d["One or more profile files could not be updated."] =
                "Bir veya daha fazla profil dosyası güncellenemedi.";
            d["{0} already has the MineManager profile."] =
                "{0} dosyasında zaten MineManager profili var.";
            d["Backed up {0}."] = "{0} yedeklendi.";
            d["Installed {0}."] = "{0} kuruldu.";
            d["{0} could not be written."] = "{0} yazılamadı.";
            d["Restored {0} from the backup."] = "{0} yedekten geri yüklendi.";
            d["{0} does not exist."] = "{0} mevcut değil.";
            d["{0} was not created by MineManager and was left alone."] =
                "{0} MineManager tarafından oluşturulmadı, olduğu gibi bırakıldı.";
            d["Removed {0}."] = "{0} kaldırıldı.";
            d["{0} could not be removed."] = "{0} kaldırılamadı.";

            // ---------------------------------------------------------------- ssh
            d["Installing the OpenSSH Server capability..."] =
                "OpenSSH Sunucu bileşeni kuruluyor...";
            d["Installing the OpenSSH Server capability"] =
                "OpenSSH Sunucu bileşenini kurma";
            d["The OpenSSH Server capability is installed."] =
                "OpenSSH Sunucu bileşeni kuruldu.";
            d["The OpenSSH Server capability is installed. A restart is required."] =
                "OpenSSH Sunucu bileşeni kuruldu. Yeniden başlatma gerekiyor.";
            d["Starting the SSH server..."] = "SSH sunucusu başlatılıyor...";
            d["The SSH server could not be started yet; it starts automatically after a restart."] =
                "SSH sunucusu henüz başlatılamadı; yeniden başlatmadan sonra kendiliğinden başlar.";
            d["Adding the firewall rule for port 22..."] =
                "22 numaralı bağlantı noktası için güvenlik duvarı kuralı ekleniyor...";
            d["The firewall rule could not be added. Allow port 22 by hand if you need it."] =
                "Güvenlik duvarı kuralı eklenemedi. Gerekirse 22 numaralı bağlantı noktasına elle " +
                "izin verin.";
            d["OpenSSH Server is enabled. Connect with: ssh {0}@{1}"] =
                "OpenSSH Sunucu etkinleştirildi. Bağlanmak için: ssh {0}@{1}";

            // ================================================= data: features
            d[".NET Framework (Versions 2, 3, 4) - Enable"] =
                ".NET Framework (Sürüm 2, 3, 4) - Etkinleştir";
            d[".NET and .NET Framework is a developer platform made up of tools, programming languages, and libraries for building many different types of applications."] =
                ".NET ve .NET Framework; birçok farklı uygulama geliştirmek için araçlar, " +
                "programlama dilleri ve kitaplıklardan oluşan bir geliştirici platformudur.";

            d["Hyper-V - Enable"] = "Hyper-V - Etkinleştir";
            d["Hyper-V is a hardware virtualization product developed by Microsoft that allows users to create and manage virtual machines."] =
                "Hyper-V, Microsoft tarafından geliştirilen ve sanal makineler oluşturup yönetmeyi " +
                "sağlayan bir donanım sanallaştırma ürünüdür.";

            d["Legacy Media Components (WMP, DirectPlay) - Enable"] =
                "Eski Medya Bileşenleri (WMP, DirectPlay) - Etkinleştir";
            d["Enables legacy programs from previous versions of Windows."] =
                "Windows'un önceki sürümlerinden kalan eski programları etkinleştirir.";

            d["Windows Subsystem for Linux (WSL) - Enable"] =
                "Linux için Windows Alt Sistemi (WSL) - Etkinleştir";
            d["Windows Subsystem for Linux is an optional feature of Windows that allows Linux programs to run natively on Windows without the need for a separate virtual machine or dual booting."] =
                "Linux için Windows Alt Sistemi, Linux programlarının ayrı bir sanal makine veya " +
                "çift önyükleme gerektirmeden Windows üzerinde yerel olarak çalışmasını sağlayan " +
                "isteğe bağlı bir Windows özelliğidir.";

            d["Network File System (NFS) - Enable"] = "Ağ Dosya Sistemi (NFS) - Etkinleştir";
            d["Network File System (NFS) is a mechanism for storing files on a network."] =
                "Ağ Dosya Sistemi (NFS), dosyaların bir ağ üzerinde saklanmasını sağlayan bir " +
                "mekanizmadır.";

            d["Registry Backup (Daily Task 12:30am) - Enable"] =
                "Kayıt Defteri Yedeği (Günlük Görev 12:30) - Etkinleştir";
            d["Creates a daily scheduled task that exports the SAM, SECURITY, SYSTEM and SOFTWARE hives to System32\\config\\RegBack."] =
                "SAM, SECURITY, SYSTEM ve SOFTWARE kümelerini System32\\config\\RegBack klasörüne " +
                "aktaran günlük bir zamanlanmış görev oluşturur.";
            d["A scheduled task named \"MineManager Registry Backup\" is created and runs daily at 12:30 as SYSTEM. It writes copies of the registry hives to System32\\config\\RegBack. Copies contain password hashes — protect that folder."] =
                "\"MineManager Registry Backup\" adlı bir zamanlanmış görev oluşturulur ve her gün " +
                "12:30'da SYSTEM olarak çalışır. Kayıt defteri kümelerinin kopyalarını " +
                "System32\\config\\RegBack klasörüne yazar. Kopyalar parola özetleri içerir — " +
                "o klasörü koruyun.";

            d["Legacy F8 Boot Recovery - Enable"] = "Eski F8 Önyükleme Kurtarma - Etkinleştir";
            d["Enables the Advanced Boot Options screen that lets you start Windows in advanced troubleshooting modes."] =
                "Windows'u gelişmiş sorun giderme kipleriyle başlatmanızı sağlayan Gelişmiş " +
                "Önyükleme Seçenekleri ekranını etkinleştirir.";
            d["The boot configuration is changed with bcdedit. On UEFI systems with fast startup the legacy menu may still not appear."] =
                "Önyükleme yapılandırması bcdedit ile değiştirilir. Hızlı başlatmanın açık olduğu " +
                "UEFI sistemlerde eski menü yine de görünmeyebilir.";

            d["Legacy F8 Boot Recovery - Disable"] = "Eski F8 Önyükleme Kurtarma - Devre Dışı";
            d["Disables the Advanced Boot Options screen and restores the standard boot menu policy."] =
                "Gelişmiş Önyükleme Seçenekleri ekranını devre dışı bırakır ve standart önyükleme " +
                "menüsü ilkesini geri yükler.";

            d["Windows Sandbox - Enable"] = "Windows Sandbox - Etkinleştir";
            d["Windows Sandbox is a lightweight virtual machine that provides a temporary desktop environment to safely run applications and programs in isolation."] =
                "Windows Sandbox, uygulamaları ve programları yalıtılmış olarak güvenle çalıştırmak " +
                "için geçici bir masaüstü ortamı sunan hafif bir sanal makinedir.";

            // ==================================================== data: fixes
            d["AutoLogon - Run"] = "AutoLogon - Çalıştır";
            d["Signs in automatically at startup. When automatic sign-in is already on, this turns it off again."] =
                "Açılışta otomatik oturum açar. Otomatik oturum açma zaten açıksa bu işlem onu " +
                "kapatır.";
            d["The Winlogon key is edited so Windows skips the sign-in screen. Windows stores the password in plain text in the registry; enter it with netplwiz. If automatic sign-in is already on, this turns it off."] =
                "Windows oturum açma ekranını atlayacak şekilde Winlogon anahtarı düzenlenir. " +
                "Windows parolayı kayıt defterinde düz metin olarak saklar; parolayı netplwiz ile " +
                "girin. Otomatik oturum açma zaten açıksa bu işlem onu kapatır.";

            d["Network - Reset"] = "Ağ - Sıfırla";
            d["Resets Winsock and the TCP/IP stack. A restart is required."] =
                "Winsock ve TCP/IP yığınını sıfırlar. Yeniden başlatma gerekir.";
            d["Winsock and the TCP/IP stack are reset to their defaults. Network connections drop and static IP or DNS settings may have to be entered again. A restart is required."] =
                "Winsock ve TCP/IP yığını varsayılanlarına döndürülür. Ağ bağlantıları kopar, " +
                "statik IP veya DNS ayarlarının yeniden girilmesi gerekebilir. Yeniden başlatma " +
                "gerekir.";

            d["NTP Server - Enable"] = "NTP Sunucusu - Etkinleştir";
            d["Replaces the default Windows NTP server (time.windows.com) with pool.ntp.org for improved time synchronization accuracy and reliability."] =
                "Varsayılan Windows NTP sunucusunu (time.windows.com), daha iyi saat eşitleme " +
                "doğruluğu ve güvenilirliği için pool.ntp.org ile değiştirir.";

            d["System Corruption Scan - Run"] = "Sistem Bozulması Taraması - Çalıştır";
            d["Runs chkdsk, SFC and DISM to check the disk and repair protected system files."] =
                "Diski denetlemek ve korunan sistem dosyalarını onarmak için chkdsk, SFC ve DISM " +
                "çalıştırır.";
            d["chkdsk, sfc and DISM are run one after another. This can take 30 minutes or more and the progress output is shown in the log."] =
                "chkdsk, sfc ve DISM art arda çalıştırılır. Bu işlem 30 dakika veya daha uzun " +
                "sürebilir ve ilerleme çıktısı günlükte gösterilir.";

            d["Windows Update - Reset"] = "Windows Update - Sıfırla";
            d["Stops the update services, renames the update cache and the signature store, then starts the services again."] =
                "Güncelleme hizmetlerini durdurur, güncelleme önbelleğini ve imza deposunu yeniden " +
                "adlandırır, ardından hizmetleri yeniden başlatır.";
            d["Windows Update, BITS, Cryptographic and Windows Installer services are stopped. SoftwareDistribution and catroot2 are renamed with a timestamp and recreated by Windows. Update history is lost and updates are downloaded again."] =
                "Windows Update, BITS, Şifreleme ve Windows Installer hizmetleri durdurulur. " +
                "SoftwareDistribution ve catroot2 zaman damgasıyla yeniden adlandırılır ve Windows " +
                "tarafından yeniden oluşturulur. Güncelleme geçmişi kaybolur ve güncellemeler " +
                "yeniden indirilir.";

            d["WinGet - Reinstall"] = "WinGet - Yeniden Kur";
            d["Checks WinGet and repairs the App Installer package in place. Nothing is downloaded."] =
                "WinGet'i denetler ve App Installer paketini yerinde onarır. Hiçbir şey indirilmez.";

            // =================================================== data: panels
            d["Computer Management"] = "Bilgisayar Yönetimi";
            d["Disks, services, devices and event logs in one console."] =
                "Diskler, hizmetler, aygıtlar ve olay günlükleri tek konsolda.";
            d["Control Panel"] = "Denetim Masası";
            d["Classic Control Panel home."] = "Klasik Denetim Masası girişi.";
            d["Mouse Properties"] = "Fare Özellikleri";
            d["Buttons, pointer and pointer options."] = "Düğmeler, işaretçi ve işaretçi seçenekleri.";
            d["Network Connections"] = "Ağ Bağlantıları";
            d["Network adapters and connection settings."] = "Ağ bağdaştırıcıları ve bağlantı ayarları.";
            d["Power Panel"] = "Güç Paneli";
            d["Power plans and sleep settings."] = "Güç planları ve uyku ayarları.";
            d["Printer Panel"] = "Yazıcı Paneli";
            d["Printers, scanners and other devices."] = "Yazıcılar, tarayıcılar ve diğer aygıtlar.";
            d["Programs and Features"] = "Programlar ve Özellikler";
            d["Installed programs and Windows features."] = "Kurulu programlar ve Windows özellikleri.";
            d["Region"] = "Bölge";
            d["Formats, location and keyboard layouts."] = "Biçimler, konum ve klavye düzenleri.";
            d["Security and Maintenance"] = "Güvenlik ve Bakım";
            d["Security and maintenance messages."] = "Güvenlik ve bakım iletileri.";
            d["Sound Settings"] = "Ses Ayarları";
            d["Playback and recording devices."] = "Kayıttan yürütme ve kayıt aygıtları.";
            d["System Properties"] = "Sistem Özellikleri";
            d["Computer name, performance and system protection."] =
                "Bilgisayar adı, performans ve sistem koruması.";
            d["Time and Date"] = "Saat ve Tarih";
            d["Clock, time zone and internet time."] = "Saat, saat dilimi ve internet saati.";
            d["Windows Defender Firewall"] = "Windows Defender Güvenlik Duvarı";
            d["Firewall profiles and allowed apps."] = "Güvenlik duvarı profilleri ve izin verilen uygulamalar.";
            d["Windows Restore"] = "Windows Geri Yükleme";
            d["System restore points."] = "Sistem geri yükleme noktaları.";

            // ================================================== data: profile
            d["PowerShell Profile - Install"] = "PowerShell Profili - Kur";
            d["Writes a commented starter profile for PowerShell 7 and Windows PowerShell and backs up any existing profile file first."] =
                "PowerShell 7 ve Windows PowerShell için açıklamalı bir başlangıç profili yazar ve " +
                "varsa mevcut profil dosyasını önce yedekler.";
            d["PowerShell Profile - Remove"] = "PowerShell Profili - Kaldır";
            d["Restores the profile from the backup, or deletes only the files this tool created."] =
                "Profili yedekten geri yükler veya yalnızca bu aracın oluşturduğu dosyaları siler.";

            // =================================================== data: remote
            d["OpenSSH Server - Enable"] = "OpenSSH Sunucusu - Etkinleştir";
            d["Installs the OpenSSH Server capability, starts the sshd service and opens firewall port 22."] =
                "OpenSSH Sunucu bileşenini kurar, sshd hizmetini başlatır ve güvenlik duvarında 22 " +
                "numaralı bağlantı noktasını açar.";
            d["OpenSSH Server lets other computers log in to this PC over the network. The sshd service is set to start automatically and a firewall rule for port 22 is added."] =
                "OpenSSH Sunucusu, diğer bilgisayarların ağ üzerinden bu bilgisayara bağlanmasını " +
                "sağlar. sshd hizmeti otomatik başlayacak şekilde ayarlanır ve 22 numaralı bağlantı " +
                "noktası için güvenlik duvarı kuralı eklenir.";

            // ------------------------------------------------- safety review fixes
            d["The periodic registry backup setting could not be enabled."] =
                "Periyodik kayıt defteri yedekleme ayarı etkinleştirilemedi.";
            d["Windows periodic registry backup is enabled."] =
                "Windows'un periyodik kayıt defteri yedeklemesi etkinleştirildi.";
            d["Triggering the built-in RegIdleBackup task..."] =
                "Yerleşik RegIdleBackup görevi çalıştırılıyor...";
            d["The backup task is registered but could not be started now; Windows runs it when the PC is idle."] =
                "Yedekleme görevi kayıtlı ancak şimdi başlatılamadı; Windows bilgisayar boştayken çalıştırır.";
            d["The first run could not be verified yet; Windows repeats the backup when the PC is idle."] =
                "İlk çalıştırma henüz doğrulanamadı; Windows bilgisayar boştayken yedeklemeyi yineler.";
            d["Boot configuration backed up to {0}."] =
                "Önyükleme yapılandırması yedeklendi: {0}.";
            d["The boot configuration could not be backed up; the change is still applied."] =
                "Önyükleme yapılandırması yedeklenemedi; değişiklik yine de uygulanacak.";
            d["The update cache could not be renamed. Close any program using Windows Update and try again."] =
                "Güncelleme önbelleği yeniden adlandırılamadı. Windows Update kullanan programları kapatıp yeniden deneyin.";
            d["Adding a firewall rule for port 22 (private and domain networks only)..."] =
                "22 numaralı bağlantı noktası için güvenlik duvarı kuralı ekleniyor (yalnızca özel ve etki alanı ağları)...";
            d["Port 22 is now reachable from private and domain networks."] =
                "22 numaralı bağlantı noktası artık özel ve etki alanı ağlarından erişilebilir.";
            d["Network settings saved to {0}."] =
                "Ağ ayarları kaydedildi: {0}.";
            d["The current network settings could not be saved."] =
                "Mevcut ağ ayarları kaydedilemedi.";
        }
    }
}
