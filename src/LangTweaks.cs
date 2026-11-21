using System;
using System.Collections.Generic;

namespace MineManager
{
    /// <summary>
    /// Turkish strings for the tweaks data and engine: every generated tweak title and
    /// description, the notes, and the TweakEngine log/error messages.
    /// Keys are the exact English text; a missing key falls back to English.
    /// </summary>
    internal static class LangTweaks
    {
        public static void Register(Dictionary<string, string> d)
        {
            if (d == null) return;

            // -------------------------------------------- essential tweaks
            // WPFTweaksActivity
            d["Activity History - Disable"] = "Etkinlik Geçmişi - Devre dışı";
            d["Stops Windows from publishing or uploading user activities while preserving clipboard history."] = "Windows'un kullanıcı etkinliklerini yayımlamasını veya yüklemesini engeller; pano geçmişi korunur.";

            // WPFTweaksHiber
            d["Hibernation - Disable"] = "Hazırda Beklet - Devre dışı";
            d["Hibernation is really meant for laptops as it saves what's in memory before turning the PC off. It really should never be used."] = "Hazırda bekletme, bilgisayar kapanmadan önce bellekteki verileri diske yazdığı için aslında dizüstü bilgisayarlar içindir. Masaüstünde kullanılmamalıdır.";

            // WPFTweaksWidget
            d["Widgets - Remove"] = "Widget'lar - Kaldır";
            d["Removes the annoying widgets in the bottom left of the Taskbar."] = "Görev çubuğunun sol alt köşesindeki can sıkıcı widget'ları kaldırır.";

            // WPFTweaksRevertStartMenu
            d["Start Menu Previous Layout - Enable"] = "Başlat Menüsü Önceki Düzen - Etkinleştir";
            d["Bring back the old Start Menu layout from before the gradual rollout of the new one in 25H2. On newer versions of Windows !!THIS TWEAK WILL NOT WORK!!"] = "25H2'de kademeli olarak yayılan yeni düzenden önceki eski Başlat Menüsü düzenini geri getirir. Daha yeni Windows sürümlerinde !!BU AYAR ÇALIŞMAZ!!";

            // WPFTweaksDisableStoreSearch
            d["Microsoft Store Recommended Search Results - Disable"] = "Microsoft Store Önerilen Arama Sonuçları - Devre dışı";
            d["Will not display recommended Microsoft Store apps when searching for apps in the Start menu."] = "Başlat menüsünde uygulama ararken önerilen Microsoft Store uygulamalarını göstermez.";

            // WPFTweaksLocation
            d["Location Tracking - Disable"] = "Konum Takibi - Devre dışı";
            d["Disables Location Tracking."] = "Konum takibini devre dışı bırakır.";

            // WPFTweaksServices
            d["Services - Set to Manual"] = "Hizmetler - Elle'ye ayarla";
            d["Sets some services to Manual startup."] = "Bazı hizmetleri Elle başlatmaya ayarlar.";

            // WPFTweaksConsumerFeatures
            d["ConsumerFeatures - Disable"] = "Tüketici Özellikleri - Devre dışı";
            d["Stops promoted app installs and reduces app suggestions from Microsoft Store content."] = "Tanıtım amaçlı uygulama kurulumlarını durdurur ve Microsoft Store içeriğinden gelen uygulama önerilerini azaltır.";

            // WPFTweaksTelemetry
            d["Telemetry - Disable"] = "Telemetri - Devre dışı";
            d["Disables Microsoft Telemetry."] = "Microsoft telemetrisini devre dışı bırakır.";

            // WPFTweaksDeliveryOptimization
            d["Delivery Optimization - Disable"] = "Teslim İyileştirme - Devre dışı";
            d["Stops Windows from using your bandwidth to upload updates to other PCs on the internet or local network."] = "Windows'un güncellemeleri internetteki veya yerel ağdaki diğer bilgisayarlara yüklemek için bant genişliğinizi kullanmasını engeller.";

            // WPFTweaksDisableBitLocker
            d["BitLocker - Disable"] = "BitLocker - Devre dışı";
            d["Disables BitLocker."] = "BitLocker'ı devre dışı bırakır.";

            // WPFTweaksRestorePoint
            d["Restore Point - Create"] = "Geri Yükleme Noktası - Oluştur";
            d["Creates a restore point at runtime in case a revert is needed from WinUtil modifications."] = "Değişiklikleri geri almak gerekebileceği için çalışma anında bir geri yükleme noktası oluşturur.";

            // WPFTweaksEndTaskOnTaskbar
            d["End Task With Right Click - Enable"] = "Sağ Tıkla Görevi Sonlandır - Etkinleştir";
            d["Enables option to end task when right-clicking a program in the taskbar."] = "Görev çubuğundaki bir programa sağ tıklandığında görevi sonlandırma seçeneğini etkinleştirir.";

            // WPFTweaksWPBT
            d["Windows Platform Binary Table (WPBT) - Disable"] = "Windows Platform İkili Tablosu (WPBT) - Devre dışı";
            d["If enabled, WPBT allows your computer vendor to execute programs at boot time, such as anti-theft software, software drivers, as well as force install software without user consent. Poses potential security risk."] = "Etkinleştirildiğinde WPBT, bilgisayar üreticinizin önyükleme sırasında program çalıştırmasına izin verir: hırsızlık önleme yazılımları, sürücüler ve kullanıcı izni olmadan yazılım kurulumu. Güvenlik riski taşır.";

            // WPFTweaksPreventDeviceMetadataFromNetwork
            d["Prevent Device Companion Apps"] = "Cihaz Yardımcı Uygulamalarını engelle";
            d["Prevents additional software from being installed when plugging in devices (e.g. Ads when plugging in a monitor). Poses potential security risk."] = "Cihaz takıldığında ek yazılım kurulumunu engeller (örneğin monitör takıldığında reklamlar). Olası güvenlik riski taşır.";

            // WPFTweaksDiskCleanup
            d["Disk Cleanup - Run"] = "Disk Temizliği - Çalıştır";
            d["Runs Disk Cleanup on Drive C: and removes old Windows Updates."] = "C: sürücüsünde Disk Temizliği'ni çalıştırır ve eski Windows güncellemelerini kaldırır.";

            // WPFTweaksDeleteTempFiles
            d["Temporary Files - Remove"] = "Geçici Dosyalar - Sil";
            d["Erases TEMP Folders."] = "TEMP klasörlerini temizler.";

            // WPFTweaksDisableExplorerAutoDiscovery
            d["File Explorer Automatic Folder Discovery - Disable"] = "Dosya Gezgini Otomatik Klasör Algılama - Devre dışı";
            d["Windows Explorer automatically tries to guess the type of the folder based on its contents, slowing down the browsing experience. WARNING! Will disable File Explorer grouping."] = "Windows Gezgini klasörün türünü içeriğine göre tahmin etmeye çalışır ve bu da gezinmeyi yavaşlatır. UYARI! Dosya Gezgini gruplamasını devre dışı bırakır.";

            // -------------------------------------------- advanced tweaks
            // WPFTweaksSvcHostSplitting
            d["svchost.exe Splitting - Disable"] = "svchost.exe Bölünmesi - Devre dışı";
            d["Disables service-host splitting to reduce the number of svchost.exe processes and modestly lower memory overhead. Recommended for low-performance systems with 12 GB of RAM or less. Can interfere with Xbox services. Restart Windows after changing this setting."] = "Hizmet ana bilgisayarı bölünmesini devre dışı bırakarak svchost.exe süreçlerinin sayısını azaltır ve bellek yükünü bir miktar düşürür. 12 GB veya daha az belleğe sahip düşük performanslı sistemler için önerilir. Xbox hizmetlerini etkileyebilir. Bu ayardan sonra Windows'u yeniden başlatın.";

            // WPFTweaksBraveDebloat
            d["Brave Browser - Debloat"] = "Brave Tarayıcı - Gereksizlerden arındır";
            d["Disables various annoyances like Brave Rewards, Leo AI, Crypto Wallet and VPN."] = "Brave Rewards, Leo AI, Kripto Cüzdanı ve VPN gibi çeşitli can sıkıcı öğeleri devre dışı bırakır.";

            // WPFTweaksDisableWarningForUnsignedRdp
            d["RDP Unsigned File Warnings - Disable"] = "RDP İmzasız Dosya Uyarıları - Devre dışı";
            d["Disables warnings shown when launching unsigned RDP files introduced with the latest Windows 10 and 11 updates."] = "Windows 10 ve 11'in son güncellemeleriyle gelen, imzasız RDP dosyaları açılırken gösterilen uyarıları devre dışı bırakır.";

            // WPFTweaksEdgeDebloat
            d["Microsoft Edge - Debloat"] = "Microsoft Edge - Gereksizlerden arındır";
            d["Disables various telemetry options, popups, and other annoyances in Edge."] = "Edge'deki çeşitli telemetri seçeneklerini, açılır pencereleri ve diğer can sıkıcı öğeleri devre dışı bırakır.";

            // WPFTweaksRemoveEdge
            d["Microsoft Edge - Remove"] = "Microsoft Edge - Kaldır";
            d["Uninstalls Microsoft Edge by creating dummy MicrosoftEdge.exe file in the legacy Edge folder. This tricks Windows into unlocking the official Edge uninstaller allowing for a system-level removal."] = "Eski Edge klasörüne sahte bir MicrosoftEdge.exe dosyası oluşturarak Microsoft Edge'i kaldırır. Bu, Windows'u resmi Edge kaldırıcısının kilidini açmaya yönlendirir ve sistem düzeyinde kaldırmaya izin verir.";

            // WPFTweaksUTC
            d["Date & Time - Set Time to UTC"] = "Tarih ve Saat - UTC'ye ayarla";
            d["Essential for computers that are dual booting. Fixes the time sync with Linux systems."] = "Çift önyüklemeli bilgisayarlar için gereklidir. Linux sistemleriyle saat uyumunu düzeltir.";

            // WPFTweaksRemoveOneDrive
            d["Microsoft OneDrive - Remove"] = "Microsoft OneDrive - Kaldır";
            d["Denies permission to remove OneDrive user files, then uses its own uninstaller to remove it and restores the original permission afterward."] = "OneDrive kullanıcı dosyalarının silinmesini engeller, ardından kendi kaldırıcısıyla OneDrive'ı kaldırır ve izinleri eski haline getirir.";

            // WPFTweaksRemoveHomeAndGallery
            d["File Explorer Home and Gallery - Disable"] = "Dosya Gezgini Giriş ve Galeri - Devre dışı";
            d["Removes the Home and Gallery from Explorer and sets This PC as default."] = "Gezgin'den Giriş ve Galeri bölümlerini kaldırır ve varsayılan olarak Bu Bilgisayar'ı ayarlar.";

            // WPFTweaksDisplay
            d["Visual Effects - Set to Best Performance"] = "Görsel Efektler - En iyi performansa ayarla";
            d["Sets the system preferences to performance. You can do this manually with sysdm.cpl as well."] = "Sistem tercihlerini performansa ayarlar. Bunu sysdm.cpl ile elle de yapabilirsiniz.";

            // WPFTweaksReservedStorage
            d["Disable Reserved Storage"] = "Ayrılmış Depolamayı devre dışı bırak";
            d["Disables Windows Reserved Storage (7-10 GB held for updates/temp files). Recommended only on small drives. Re-enable before major Windows feature updates to avoid installation failures."] = "Windows Ayrılmış Depolamasını (güncellemeler ve geçici dosyalar için ayrılan 7-10 GB) devre dışı bırakır. Yalnızca küçük disklerde önerilir. Kurulum hatalarını önlemek için büyük Windows özellik güncellemelerinden önce yeniden etkinleştirin.";

            // WPFTweaksStorage
            d["Storage Sense - Disable"] = "Depolama Algılayıcısı - Devre dışı";
            d["Storage Sense deletes temp files automatically."] = "Depolama Algılayıcısı geçici dosyaları otomatik olarak siler.";

            // WPFTweaksWindowsAI
            d["Windows AI - Disable And Remove"] = "Windows AI - Devre dışı bırak ve kaldır";
            d["Removes and disables all AI features/packages"] = "Tüm yapay zekâ özelliklerini ve paketlerini kaldırır ve devre dışı bırakır.";

            // WPFTweaksRazerBlock
            d["Razer Software Auto-Install - Disable"] = "Razer Yazılımı Otomatik Kurulum - Devre dışı";
            d["Blocks ALL Razer Software installations. The hardware works fine without any software."] = "TÜM Razer yazılımı kurulumlarını engeller. Donanım yazılım olmadan da sorunsuz çalışır.";

            // WPFTweaksLogiBlock
            d["Logitech Download Assistant Auto-Install - Disable"] = "Logitech Download Assistant Otomatik Kurulum - Devre dışı";
            d["Blocks the Logi Download Assistant that Windows Update keeps reinstalling with Logitech device drivers. Logitech hardware keeps working without it."] = "Windows Update'in Logitech sürücüleriyle birlikte sürekli yeniden kurduğu Logi Download Assistant'ı engeller. Logitech donanımı onsuz da çalışır.";

            // WPFTweaksDisableNotifications
            d["System Tray Notifications & Calendar - Disable"] = "Sistem Tepsisi Bildirimleri ve Takvim - Devre dışı";
            d["Disables all Notifications INCLUDING Calendar."] = "Takvim DAHİL tüm bildirimleri devre dışı bırakır.";

            // WPFTweaksBlockAdobeNet
            d["Adobe URL Block List - Enable"] = "Adobe URL Engelleme Listesi - Etkinleştir";
            d["Reduces user interruptions by selectively blocking connections to Adobe's activation and telemetry servers. Credit: Ruddernation-Designs"] = "Adobe'nin etkinleştirme ve telemetri sunucularına bağlantıları seçerek engelleyip kesintileri azaltır. Katkı: Ruddernation-Designs";

            // WPFTweaksRightClickMenu
            d["Right-Click Menu Previous Layout - Enable"] = "Sağ Tık Menüsü Önceki Düzen - Etkinleştir";
            d["Attempts to restore the classic Windows 11 File Explorer context menu on newer builds. Compatibility may vary."] = "Daha yeni sürümlerde klasik Windows 11 Dosya Gezgini sağ tık menüsünü geri yüklemeyi dener. Uyumluluk değişebilir.";

            // WPFTweaksIPv46
            d["IPv6 - Set IPv4 as Preferred"] = "IPv6 - IPv4'ü tercih edilen yap";
            d["Setting the IPv4 preference can have latency and security benefits on private networks where IPv6 is not configured."] = "IPv4 tercihi, IPv6 yapılandırılmamış özel ağlarda gecikme ve güvenlik açısından avantaj sağlayabilir.";

            // WPFTweaksTeredo
            d["Teredo - Disable"] = "Teredo - Devre dışı";
            d["Teredo network tunneling is an IPv6 feature that can cause additional latency, but may cause problems with some games."] = "Teredo ağ tünellemesi ek gecikmeye neden olabilen bir IPv6 özelliğidir; ancak bazı oyunlarda sorun çıkarabilir.";

            // WPFTweaksDisableSearchIndexing
            d["Windows Search Indexing - Disable"] = "Windows Arama Dizini - Devre dışı";
            d["Stops and disables the Windows Search service (WSearch) to cut background disk activity, mainly useful on mechanical hard drives. Warning: file searches in Explorer and the Start menu become much slower because Windows no longer maintains a search index. Undo restores the service and starts it again."] = "Windows Arama hizmetini (WSearch) durdurup devre dışı bırakarak arka plan disk etkinliğini azaltır; özellikle mekanik sabit disklerde yararlıdır. Uyarı: Windows artık arama dizini tutmadığı için Gezgin ve Başlat menüsündeki dosya aramaları belirgin şekilde yavaşlar. Geri alma hizmeti yeniden etkinleştirip başlatır.";

            // WPFTweaksDisableIPv6
            d["IPv6 - Disable"] = "IPv6 - Devre dışı";
            d["Disables IPv6."] = "IPv6'yı devre dışı bırakır.";

            // WPFTweaksDisableBGapps
            d["Background Apps - Disable"] = "Arka Plan Uygulamaları - Devre dışı";
            d["Disables all Microsoft Store apps from running in the background, which has to be done individually since Windows 11."] = "Tüm Microsoft Store uygulamalarının arka planda çalışmasını engeller; Windows 11'den beri bu işlem tek tek yapılmak zorunda.";

            // -------------------------------------------- customize preferences
            // WPFToggleDetailedBSoD
            d["BSoD Verbose Mode"] = "Mavi Ekran Ayrıntılı Mod";
            d["Gives more information when you blue screen."] = "Mavi ekran hatasında daha fazla bilgi verir.";

            // WPFToggleBatteryPercentage
            d["System Tray Battery Percentage"] = "Sistem Tepsisi Pil Yüzdesi";
            d["Shows numeric battery percentage next to the battery icon in the system tray."] = "Sistem tepsisindeki pil simgesinin yanında sayısal pil yüzdesini gösterir.";

            // WPFToggleDarkMode
            d["Dark Theme for Windows"] = "Windows için Koyu Tema";
            d["Dark Mode for the system and applications."] = "Sistem ve uygulamalar için koyu tema.";

            // WPFToggleShowExt
            d["File Explorer File Extensions"] = "Dosya Gezgini Dosya Uzantıları";
            d["Shows .file extensions in Explorer (.exe, .png, etc.)"] = "Gezgin'de dosya uzantılarını gösterir (.exe, .png vb.).";

            // WPFToggleHiddenFiles
            d["File Explorer Hidden Files"] = "Dosya Gezgini Gizli Dosyalar";
            d["Reveals hidden files in Explorer."] = "Gezgin'de gizli dosyaları gösterir.";

            // WPFToggleVerboseLogon
            d["Logon Verbose Mode"] = "Oturum Açma Ayrıntılı Mod";
            d["Show detailed messages during startup/shutdown."] = "Açılış ve kapanış sırasında ayrıntılı iletiler gösterir.";

            // WPFToggleNewOutlook
            d["Microsoft Outlook New Version"] = "Microsoft Outlook Yeni Sürüm";
            d["This will ensure the new Outlook application is used."] = "Yeni Outlook uygulamasının kullanılmasını sağlar.";

            // WPFToggleScrollbars
            d["Scrollbars Always Visible"] = "Kaydırma Çubukları Her Zaman Görünür";
            d["If enabled, scrollbars will always be visible. If disabled, Windows will automatically hide scrollbars when not in use."] = "Etkinleştirilirse kaydırma çubukları her zaman görünür olur. Devre dışı bırakılırsa Windows kullanılmadığında bunları otomatik gizler.";

            // WPFMultiplaneOverlay
            d["Multiplane Overlay"] = "Çok Katmanlı Kaplama (MPO)";
            d["Multiplane Overlay composes multiple image layers, which can sometimes cause issues with graphics cards. Changes to this preference are applied immediately."] = "Çok katmanlı kaplama (MPO) birden çok görüntü katmanını birleştirir ve bazen ekran kartlarında sorunlara yol açabilir. Bu tercihteki değişiklikler hemen uygulanır.";

            // WPFToggleMouseAcceleration
            d["Mouse Acceleration"] = "Fare Hızlandırma";
            d["Makes it so Cursor movement is affected by the speed of your physical mouse movements."] = "İmleç hareketinin, fareyi ne kadar hızlı hareket ettirdiğinizden etkilenmesini sağlar.";

            // WPFToggleNumLock
            d["Num Lock on Startup"] = "Açılışta Num Lock";
            d["Toggle the Num Lock key state when your computer starts."] = "Bilgisayar açılırken Num Lock tuşunun durumunu ayarlar.";

            // WPFToggleWindowSnapping
            d["Window Snapping"] = "Pencere Yapıştırma";
            d["Toggles the window snapping feature when dragging windows."] = "Pencereleri sürüklerken pencere yapıştırma özelliğini açar veya kapatır.";

            // WPFToggleStandbyFix
            d["S0 Sleep Network Connectivity"] = "S0 Uyku Ağ Bağlantısı";
            d["Toggles network connectivity during S0 Sleep which is low power idle in modern laptops."] = "Modern dizüstü bilgisayarlardaki düşük güçlü bekleme olan S0 Uykusu sırasında ağ bağlantısını açar veya kapatır.";

            // WPFToggleS3Sleep
            d["S3 Sleep"] = "S3 Uyku";
            d["Toggles between Modern Standby and S3 Sleep, which cuts off power to the CPU while continuing to refresh the memory."] = "Modern Bekleme ile S3 Uykusu arasında geçiş yapar; S3, belleği tazelemeye devam ederken işlemciye giden gücü keser.";

            // WPFToggleHideSettingsHome
            d["Settings Home Page"] = "Ayarlar Giriş Sayfası";
            d["Toggles the Home Page in the Windows Settings app."] = "Windows Ayarlar uygulamasındaki Giriş sayfasını açar veya kapatır.";

            // WPFToggleBingSearch
            d["Start Menu Bing Search"] = "Başlat Menüsü Bing Araması";
            d["Toggles Bing web search results in Windows Search."] = "Windows Arama'da Bing web arama sonuçlarını açar veya kapatır.";

            // WPFToggleLoginBlur
            d["Logon Screen Acrylic Blur"] = "Oturum Açma Ekranı Akrilik Bulanıklık";
            d["Toggles the acrylic blur effect on login screen background."] = "Oturum açma ekranı arka planındaki akrilik bulanıklık efektini açar veya kapatır.";

            // WPFToggleDisableLockscreen
            d["Lock Screen - Disable"] = "Kilit Ekranı - Devre dışı";
            d["Skips the lock screen entirely and goes directly to the sign-in screen on boot and wake."] = "Kilit ekranını tamamen atlar; açılışta ve uyanmada doğrudan oturum açma ekranına geçer.";

            // WPFToggleStartMenuRecommendations
            d["Start Menu Recommendations"] = "Başlat Menüsü Önerileri";
            d["Toggles the recommendations section in the Start Menu. WARNING: This will also disable Windows Spotlight on your Lock Screen as a side effect."] = "Başlat Menüsü'ndeki öneriler bölümünü açar veya kapatır. UYARI: Yan etki olarak Kilit Ekranınızdaki Windows Spotlight da devre dışı kalır.";

            // WPFToggleStickyKeys
            d["Sticky Keys"] = "Yapışkan Tuşlar";
            d["Toggles the Sticky Keys, which activate when clicking shift rapidly."] = "Shift tuşuna hızlıca basıldığında devreye giren Yapışkan Tuşlar'ı açar veya kapatır.";

            // WPFToggleTaskbarAlignment
            d["Taskbar Centered Icons"] = "Görev Çubuğu Ortalanmış Simgeler";
            d["Toggles the Taskbar alignment either to the left or center."] = "Görev çubuğu hizalamasını sola veya ortaya alır.";

            // WPFToggleTaskbarSearch
            d["Taskbar Search Icon"] = "Görev Çubuğu Arama Simgesi";
            d["Toggles the Search Button on the Taskbar."] = "Görev çubuğundaki Arama düğmesini açar veya kapatır.";

            // WPFToggleTaskView
            d["Taskbar Task View Icon"] = "Görev Çubuğu Görev Görünümü Simgesi";
            d["Toggles the Task View Button in the Taskbar."] = "Görev çubuğundaki Görev Görünümü düğmesini açar veya kapatır.";

            // WPFToggleGameMode
            d["Game Mode"] = "Oyun Modu";
            d["Toggles Windows prioritizes gaming performance by allocating system resources to games."] = "Windows'un sistem kaynaklarını oyunlara ayırarak oyun performansını önceliklendirmesini açar veya kapatır.";

            // WPFToggleLongPaths
            d["Enable Long Paths"] = "Uzun Yolları Etkinleştir";
            d["Toggles support for file paths longer than 260 characters in Explorer."] = "Gezgin'de 260 karakterden uzun dosya yolları desteğini açar veya kapatır.";

            // -------------------------------------------- performance plans
            // WPFAddUltPerf
            d["Ultimate Performance Profile - Enable"] = "Nihai Performans Profili - Etkinleştir";
            d["Adds and activates the hidden Ultimate Performance power plan."] = "Gizli Nihai Performans güç planını ekler ve etkinleştirir.";

            // WPFRemoveUltPerf
            d["Ultimate Performance Profile - Disable"] = "Nihai Performans Profili - Devre dışı";
            d["Removes the Ultimate Performance plan and restores the default power schemes."] = "Nihai Performans planını kaldırır ve varsayılan güç planlarını geri yükler.";

            // ------------------------------------------------------------- notes
            d["Restart Windows to release the hibernation file."] = "Hazırda bekletme dosyasını serbest bırakmak için Windows'u yeniden başlatın.";
            d["Restart Windows if System Restore was not enabled before."] = "Sistem Geri Yükleme daha önce etkin değildiyse Windows'u yeniden başlatın.";
            d["Sign out and back in for Explorer to rebuild its view cache."] = "Gezgin'in görünüm önbelleğini yeniden oluşturması için oturumu kapatıp yeniden açın.";
            d["Edge has no offline installer. Reinstall it from Microsoft if you need the browser back."] = "Edge'in çevrimdışı yükleyicisi yoktur. Tarayıcıyı geri istiyorsanız Microsoft'un sitesinden yeniden kurun.";
            d["Reinstalling OneDrive needs the official installer; undo only restores the service."] = "OneDrive'ı yeniden kurmak resmi yükleyiciyi gerektirir; geri alma yalnızca hizmeti eski haline getirir.";
            d["Restart Windows to finish the reserved storage change."] = "Ayrılmış depolama değişikliğinin tamamlanması için Windows'u yeniden başlatın.";
            d["Restart Windows to finish removing the AI components."] = "Yapay zekâ bileşenlerinin kaldırılmasını tamamlamak için Windows'u yeniden başlatın.";
            d["Blocks a built-in list of Adobe telemetry hosts in the hosts file. Nothing is downloaded."] = "Adobe telemetri adreslerinden oluşan yerleşik bir listeyi hosts dosyasına ekler. Hiçbir şey indirilmez.";
            d["Restart Windows to switch the sleep model."] = "Uyku modelini değiştirmek için Windows'u yeniden başlatın.";
            d["Restart Windows so the long path support takes effect."] = "Uzun yol desteğinin etkinleşmesi için Windows'u yeniden başlatın.";
            d["Not for laptops: it keeps the CPU at high clocks and drains the battery."] = "Dizüstü bilgisayarlar için uygun değildir: işlemciyi yüksek hızda tutar ve pili tüketir.";
            d["Restores the Balanced and High performance plans Windows ships with."] = "Windows ile gelen Dengeli ve Yüksek performans planlarını geri yükler.";

            // ------------------------------------------- engine log and errors
            d["Administrator rights are required for this tweak. Run MineManager as administrator."] = "Bu ayar için yönetici hakları gerekiyor. MineManager'ı yönetici olarak çalıştırın.";
            d["No changes to roll back."] = "Geri alınacak değişiklik yok.";
            d["The undo snapshot path is unavailable; undo will use the built-in defaults."] = "Geri alma kaydı yolu kullanılamıyor; geri alma yerleşik varsayılanları kullanacak.";
            d["A restart is required to finish this tweak."] = "Bu ayarın tamamlanması için yeniden başlatma gerekiyor.";
            d["not set"] = "tanımsız";
            d["Applied"] = "Uygulandı";
            d["Not applied"] = "Uygulanmadı";
            d["Mixed"] = "Karma";
            d["Unknown"] = "Bilinmiyor";
            d["Invalid registry path: {0}"] = "Geçersiz kayıt defteri yolu: {0}";
            d["Removed {0}"] = "Kaldırıldı: {0}";
            d["Set {0} = {1}"] = "Ayarlandı {0} = {1}";
            d["Could not open {0}"] = "{0} açılamadı";
            d["Could not write {0}: {1}"] = "{0} yazılamadı: {1}";
            d["Rolling back {0} change(s)..."] = "{0} değişiklik geri alınıyor...";
            d["Rollback failed for {0}"] = "{0} geri alınamadı";
            d["Snapshot saved: {0}"] = "Geri alma kaydı oluşturuldu: {0}";
            d["The undo snapshot could not be saved: {0}"] = "Geri alma kaydı oluşturulamadı: {0}";
            d["The undo snapshot could not be updated: {0}"] = "Geri alma kaydı güncellenemedi: {0}";
            d["DEL {0}"] = "SİL {0}";
            d["SET {0} = {1} ({2}) [now: {3}]"] = "YAZ {0} = {1} ({2}) [şimdi: {3}]";
            d["SERVICE {0} = {1}"] = "HİZMET {0} = {1}";
            d["TASK {0} = {1}"] = "GÖREV {0} = {1}";
            d["RUN powershell: {0}"] = "ÇALIŞTIR powershell: {0}";
            d["Running PowerShell ({0}s timeout)..."] = "PowerShell çalıştırılıyor ({0} sn zaman aşımı)...";
            d["Script timed out after {0}s - rolling back changes."] = "Betik {0} sn sonra zaman aşımına uğradı - değişiklikler geri alınıyor.";
            d["Script failed (exit code {0}) - rolling back changes."] = "Betik başarısız oldu (çıkış kodu {0}) - değişiklikler geri alınıyor.";
            d["Verification failed - detected state is {0}."] = "Doğrulama başarısız - algılanan durum: {0}.";
            d["Could not change task {0}"] = "Görev değiştirilemedi: {0}";
            d["Could not change task {0}: {1}"] = "Görev değiştirilemedi {0}: {1}";
            d["Applying: {0}"] = "Uygulanıyor: {0}";
            d["Restoring: {0}"] = "Geri alınıyor: {0}";
            d["Done: {0}"] = "Tamamlandı: {0}";
            d["Failed: {0}"] = "Başarısız: {0}";
        }
    }
}
