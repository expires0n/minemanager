# Kaynak değerlendirmesi

Bu belge, MineManager'a dışarıdan alınan üç kaynağı **projenin kendi başlıklarına** göre
değerlendirir: hangi fikir alındı, hangisi alınmadı, neden ve geri alma davranışı ne.

| # | Kaynak | Lisans | Projedeki karşılığı |
|---|---|---|---|
| 1 | [Bulk Crap Uninstaller (BCU)](https://github.com/BCUninstaller/Bulk-Crap-Uninstaller) | Apache-2.0 | **Kaldırıcı** sayfası (Revo tarzı motorun yerini alır) |
| 2 | [Chris Titus WinUtil — Tweaks](https://github.com/ChrisTitusTech/winutil) | MIT | **İnce Ayarlar** sayfası |
| 3 | Chris Titus WinUtil — Config | MIT | **Yapılandırma** sayfası |

Kaynak kodlar inceleme sırasında `.ref/` altında yerel olarak tutuldu (depoya dahil değildir).

---

## 1. Kaldırıcı — Revo tarzı motordan BCU tarzı motora

**Neden değişti:** Revo tarzı motor yalnızca `DisplayName` + `UninstallString` okuyordu. Kaldırma
komutunu `cmd /c` ile çalıştırıyor, kaldırıcının türünü bilmiyor, sessiz kaldırmayı
desteklemiyor ve bozuk kayıtları ayırt edemiyordu. BCU aynı dili (C#/.NET/WinForms) kullanıyor,
yani kavramları birebir uyarlanabiliyor.

### Alınanlar

| BCU kavramı | Bizde karşılığı | Not |
|---|---|---|
| `RegistryFactory` — 64-bit + `WOW6432Node` + `HKCU` birleşik okuma | `UninstallerCatalog.All()` | Yinelenen kayıtlar tekilleştirilir |
| Ek kayıt değerleri (`Comment`, `DisplayIcon`, `InstallSource`, `ModifyPath`, `URLInfoAbout`, `WindowsInstaller`, `ParentKeyName`, `BundleProviderKey`) | `InstalledProgram` alanları | Ayrıntı penceresinde gösterilir |
| `UninstallerTypeAdder` — kaldırıcı türü tespiti | `UninstallerKind` | Msiexec / Inno / NSIS / InstallShield / SdbInst / PowerShell |
| `MsiUninstallStringGenerator` — GUID'den `/X … /qn /norestart` üretimi | `BuildQuietCommand()` | `/I` ile gelen kayıtlar `/X`'e çevrilir |
| `InnoSetupQuietUninstallStringGenerator` | `BuildQuietCommand()` | `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART` |
| NSIS sessiz kaldırma | `BuildQuietCommand()` | `/S` |
| "Geçersiz kayıt" tespiti | `InstalledProgram.IsInvalid` | Kaldırıcı dosyası yok / MSI ürün kodu geçersiz |
| Toplu kaldırma kuyruğu (`BulkUninstallTask`) | `UninstallQueue` | Sıralı, iptal edilebilir, MSI 1618 çakışmasını önler |
| Liste dışa aktarma (`ApplicationListExport`) | `ExportCsv()` | UTF-8 BOM + formül enjeksiyonu koruması |
| `ConfidenceGenerators` — Sift4 benzerlik ölçütü | `Sift.Match()` | Kalıntı taramasında isim eşleştirmesi |

### Alınmayanlar

- **Store (UWP) uygulamaları:** `AppxPackage` yönetimi WinRT/`Windows.Management.Deployment`
  gerektiriyor; tek EXE/C# 5 hedefiyle uyumsuz. Ayrı bir sürümde ele alınmalı.
- **Chocolatey / Scoop / Steam / Oculus / Windows Update / Windows Feature fabrikaları:** her biri
  kendi listesini ve kaldırma yolunu gerektiriyor; uygulamanın kapsamı dışında.
- **`Junk/*` tarayıcıları (COM, firewall kuralı, event log, prefetch…):** mevcut
  `LeftoverScanner` korunur; BCU'nun onlarca özel tarayıcısı port edilmedi.
- **Eşzamanlı `UninstallManager` ve UI automatizer:** tek iş parçacıklı, sıralı kuyruk yeterli ve
  çok daha öngörülebilir.
- **Sertifika doğrulama, simge önbelleği, özyinelemeli boyut hesabı:** kazanç/risk oranı düşük.

### Uyarlama farkları

- Sessiz kaldırma **varsayılan olarak kapalı**; açıldığında komut satırları tek tek onaya sunulur.
- `SystemComponent` ve `NoRemove=1` kayıtları sessiz kaldırılamaz.
- Kaldırma sonrası başarı, NSIS/Inno için çıkış koduna değil **kayıt anahtarının kaybolmasına**
  bakılarak doğrulanır (NSIS kendini `%TEMP%`'den yeniden başlatır).
- Hiçbir durumda otomatik yeniden başlatma yapılmaz; 1641/3010 "yeniden başlatma gerekli" olarak raporlanır.

### Riskler

| Risk | Azaltma |
|---|---|
| Yanlış kayıt silinmesi ("geçersiz kaydı kaldır") | Silmeden önce `reg.exe export` ile `MineManager-backup\<tarih>\` yedeği |
| MSI ürün kodu yanlış okunur | GUID `new Guid()` ile doğrulanır, değilse sessiz kaldırma devre dışı |
| Toplu kaldırmada yarım kalma | Kuyruk sıralı; iptal yalnızca geçerli öğeden sonra |
| Kaldırıcı yolu boşluk içeriyor | Tırnaksız yollar için `.exe`'ye kadar genişleyen ayrıştırıcı |

---

## 2. İnce Ayarlar — WinUtil Tweaks

**Projeye uygunluk:** WinUtil'ın tweak modeli (kayıt defteri girdileri + `OriginalValue` +
`InvokeScript`/`UndoScript`) bizim veri modeliyle neredeyse birebir örtüşüyor. WinUtil
PowerShell ile çalışırken biz aynı işlemleri C# ile yapıyoruz; bu yüzden **veri** taşınır,
**motor** yeniden yazılır.

**Sonuç:** 69 tweak'in 67'si taşındı — `18 temel · 22 gelişmiş · 25 tercih · 2 performans planı`.
Veri `tools/data/winutil-tweaks.json` dosyasından `tools/gen_winutil.py` ile
`src/TweaksData.cs` içine üretilir (deterministik; iki kez çalıştırıldığında aynı dosya).

Ekran görüntüsündeki dört bölüm birebir korunur:

| WinUtil bölümü | Bizde grup | Davranış |
|---|---|---|
| Essential Tweaks | `essential` | Onay kutusu listesi, "Run Tweaks" ile toplu uygulama |
| Advanced Tweaks - CAUTION | `advanced` | Aynı liste + `Danger` işareti, onay diyaloğu `destructive` |
| Customize Preferences | `prefs` | Anında uygulanan Aero açma/kapama anahtarları |
| Performance Plans - NOT FOR LAPTOPS | `performance` | `powercfg` ile tek tık düğmeler |

### Alan eşlemesi

| WinUtil (`config/tweaks.json`) | MineManager |
|---|---|
| `Content` | `Tweak.Title` |
| `Description` | `Tweak.Desc` |
| `category` | `Tweak.Group` |
| `Type` (`Toggle`/`Button`) | `Tweak.Kind` (`toggle`/`button`), diğerleri `check` |
| `registry[]` | `RegOp[]` (`Path`, `Name`, `Type`, `Value`, `Original`) |
| `OriginalValue: "<RemoveEntry>"` | `RegOp.Original` → geri alırken değer silinir, **anahtar silinmez** |
| `InvokeScript` / `UndoScript` | `Tweak.Script` / `Tweak.UndoScript` (PowerShell bloğu) |
| `link` | `Tweak.Link` (bilgi penceresinde) |

### Port politikası

1. **Yalnızca kayıt defteri** yazan tweak'ler birebir taşındı.
2. **Betik gerektiren** tweak'lerde yalnızca Windows ile gelen araçlar kullanıldı:
   `powercfg`, `dism`, `netsh`, `icacls`, `schtasks`, `w32tm`, `bcdedit`, `Get-AppxPackage`,
   `Set-MpPreference`, `Checkpoint-Computer`.
3. **Dışarıdan dosya indiren** tweak taşınmadı (`O&O ShutUp10++ - Run`).
4. **Arayüz gerektiren** tweak taşınmadı (`DNS - Set to:` bir seçim kutusu istiyor).
5. WinUtil'ın kendi günlük/ilerleme fonksiyonları betiklerden çıkarıldı; bunun yerine
   uygulamanın kendi günlüğü kullanılıyor.
6. Uygulama içi WinUtil yardımcıları yerleşik karşılıklarına çevrildi:
   `Invoke-WinUtilExplorerUpdate` → `Stop-Process -Name explorer -Force`;
   `Set-WinUtilClassicContextMenu` → aynı etkiyi yapan kayıt defteri değeri (böylece durum
   tespiti de çalışır); `$sync`/tema yardımcıları kaldırıldı.
7. `Invoke-RestMethod` ile GitHub'dan çekilen Adobe engel listesi, 8 yerleşik Adobe telemetri
   sunucusundan oluşan bir listeye çevrildi; `hosts` dosyasına `# MineManager Adobe block`
   işaretleri arasına yazılır, geri alma yalnızca o bloğu siler.
8. `winget install` içeren geri alma betikleri kaldırıldı (Edge/OneDrive yeniden kurulumu
   kullanıcıya bırakılır ve not olarak gösterilir).

### Geri alma

WinUtil'daki `OriginalValue` kullanıcının **o anki** değerini değil, üreticinin varsayılanını
temsil eder. Bu yüzden uygulama ilk yazmadan önce mevcut değerleri
`MineManager.undo.tsv` dosyasına anlık görüntü olarak kaydeder; geri alma önce bu anlık
görüntüyü, yoksa `OriginalValue`'yu kullanır.

### Riskler

| Risk | Azaltma |
|---|---|
| Yarıda kalan tweak | Kayıt defteri/hizmet/görev adımları ters sırayla geri alınır |
| Betik zaman aşımı | 180 sn (DISM/SFC sınıfı 900 sn); süreç ağacı `taskkill /T /F` ile kapatılır |
| Yönetici olmayan oturum | `RequiresAdmin` true ise çalıştırma kapalı, nedeni yazılır |
| Windows 7/8'de çalışmayan tweak | Yalnızca Windows 10/11'de anlamlı olanlar sayfa altında notlanır |
| Dil değişiminde seçimler kaybolur | Seçimler `TweakSession` içinde sayfa yeniden kurulsa da korunur |

---

## 3. Yapılandırma — WinUtil Config

| WinUtil bölümü | Bizde grup | Uygulama |
|---|---|---|
| Features (9) | `features` | `dism /online /enable-feature … /norestart`, 3010 = başarılı + yeniden başlatma |
| Fixes (6) | `fixes` | NTP, ağ sıfırlama, sistem onarımı (chkdsk → sfc → DISM), Windows Update sıfırlama, WinGet onarımı, AutoLogon |
| Legacy Windows Panels (14) | `panels` | Klasik `cpl`/`msc` hedefleri doğrudan açılır |
| Powershell Profile Powershell 7+ Only (2) | `profile` | Kurulum/kaldırma, mevcut profili `.minemanager.bak` olarak yedekleyerek |
| Remote Access (1) | `remote` | OpenSSH Server yeteneği + `sshd` hizmeti + güvenlik duvarı kuralı |

### Bilinçli sapmalar

- **AutoLogon:** WinUtil Sysinternals `Autologon.exe` indirir. Biz indirmiyoruz; belgelenmiş
  `Winlogon` kayıt yöntemi kullanılıyor, **parola hiç sorulmuyor ve saklanmıyor**, kullanıcı
  `netplwiz`'e yönlendiriliyor ve parolanın düz metin saklandığı uyarısı veriliyor. İkinci
  çalıştırma autologon'ı kapatır.
- **WinGet:** WinUtil Chocolatey ile yeniden kurar. Biz indirme yapmadan
  `Reset-AppxPackage` / `Add-AppxPackage -Register` ile onarıyoruz ve sonucu olduğu gibi raporluyoruz.
- **PowerShell profili:** WinUtil'ın tam profil betiği yerine küçük, yorumlu bir MineManager
  profili yazılır (`Documents\PowerShell` ve `Documents\WindowsPowerShell`); var olan dosya
  `*.minemanager.bak` olarak yedeklenir, kaldırma yalnızca kendi yazdığı dosyayı siler.
- **Registry Backup:** WinUtil'ın günlük görevi `reg.exe export` ile SAM/SECURITY/SYSTEM/SOFTWARE
  anahtarlarını `RegBack` klasörüne yazar.

### Riskler

| Risk | Azaltma |
|---|---|
| DISM/SFC uzun sürer | Arka planda, iptal edilebilir, canlı günlük |
| Özellik zaten kurulu | DISM çıktısı yorumlanır, hata olarak gösterilmez |
| Windows Update klasörü kilitli | Hizmetler durdurulur, yeniden adlandırma başarısızsa ayrıntılı hata |
| BCD değişikliği | Yalnızca F8 maddesi, `bcdedit` çıktısı günlüğe yazılır |

---

## 4. Lisans ve atıf

- **Bulk Crap Uninstaller** — Apache License 2.0, © Marcin Szeniak ve katkıda bulunanlar.
  Kaynak koddan kavram ve algoritma (Sift4 eşleştirme, tür tespiti, sessiz kaldırma kuralları)
  uyarlanmıştır; dosyalar birebir kopyalanmamıştır.
- **Windows Utility (WinUtil)** — MIT License, © Chris Titus Tech. Tweak/özellik **verisi**
  `config/tweaks.json` ve `config/feature.json` dosyalarından üretilir; üretim betiği
  `tools/gen_winutil.py`, üretilen dosyanın başında kaynak ve lisans notu bulunur.
- Her iki proje de MineManager'ın bir parçası değildir; burada yalnızca uyarlama kaynağıdır.

---

## 5. Güvenlik incelemesi (Claude Fable 5.2 · yüksek efor)

Uygulama sonrası ikinci bir gözden geçirme yapıldı: **model `claude-5-2-fable`
(Claude Fable 5.2), akıl yürütme eforu "high"**, ekli dosyalar `docs/KAYNAK-DEGERLENDIRME.md`
ve `src/ConfigEngine.cs`. Bulunan sorunlar ve yapılan düzeltmeler:

| Bulgu | Risk | Durum |
|---|---|---|
| Kayıt defteri yedeği, `%ProgramData%` altındaki bir `.cmd` dosyasını SYSTEM görevinde çalıştırıyordu | Standart kullanıcı bu klasörü sahiplenip SYSTEM yetkisiyle kod çalıştırabilirdi; ayrıca `reg export` çıktısı hive değildir, kurtarma için kullanılamaz | **Düzeltildi:** yerleşik `EnablePeriodicBackup=1` + `RegIdleBackup` görevi; eski görev silinir |
| OpenSSH için güvenlik duvarı kuralı 22. portu **tüm profillerde, tüm programlar** için açıyordu | Gereksiz saldırı yüzeyi | **Düzeltildi:** kural `sshd.exe` + `profile=domain,private` ile sınırlandı, günlüğe erişim uyarısı yazılıyor |
| Windows Update sıfırlama, iptal/hata durumunda hizmetleri **kapalı bırakıyordu** | Windows Update reboot'a kadar bozuk kalır | **Düzeltildi:** hizmetler `finally` mantığıyla her durumda yeniden başlatılıyor, yeniden adlandırma başarısızsa işlem başarısız sayılıyor |
| Geri alma anlık görüntüsü (`MineManager.undo.tsv`) yükseltilmiş süreçte, kullanıcı tarafından yazılabilir bir klasörden okunuyordu | Dosyayı değiştiren biri, "Geri Al" ile seçtiği HKLM değerini yazdırabilirdi | **Düzeltildi:** her satır derlenmiş `RegOp`/`SvcOp` ile (id + yol + ad + tür) eşleşmezse yok sayılır |
| PowerShell profil kaldırma, `.bak` varsa kullanıcının o anki dosyasının üzerine yazıyordu; işaret dosyanın herhangi bir yerinde aranıyordu | Kullanıcı profilinin kaybı | **Düzeltildi:** yalnızca **ilk satırı** işaret olan dosya değiştirilir/silinir, yedekler zaman damgalı |
| Sessiz kaldırmada 10 dakikalık zaman aşımı **msiexec**'i de öldürüyordu | Yarım uygulanmış MSI paketi | **Düzeltildi:** `msiexec` için zaman aşımı yok (transactional), diğerleri 10 dakikada kapatılır |
| Onarım sırası `chkdsk → sfc → DISM` idi | Bozuk imajda SFC onaramaz | **Düzeltildi:** `chkdsk → DISM → sfc` |
| `bcdedit {default}` yanlış girdiyi hedefleyebilir | Yanlış önyükleme girdisi | **Düzeltildi:** `{current}` + önce `bcdedit /export` yedeği |
| Ağ sıfırlama statik IP/DNS'i siliyordu | Ağ bağlantısının kaybı | **Düzeltildi:** önce `netsh -c interface dump` ile yedek alınıyor |
| İş sürerken pencere kapatılabiliyordu; tek örnek kilidi oturum başına | Yarım kalan işlem, ikinci kullanıcıyla çakışma | **Düzeltildi:** kapatma onayı + `Global\` mutex (yetki yoksa oturum kilidine düşer) |

Bilinçli olarak **uygulanmayan** (belgelenen) öneriler:

- Yönetici yükseltmesi ile oturum açmış kullanıcının farklı olması (over-the-shoulder UAC) durumunda
  HKCU işlemlerinin yönetici profiline gitmesi — tespit edilip engellenmedi, sınır olarak kaldı.
- Kaldırıcı yavru süreçlerini Job Object ile beklemek; NSIS/Inno için kayıt anahtarı yoklaması yeterli görüldü.
- Etki alanına katılmış makinelerde NTP değişikliğini engellemek.
- Geri yükleme noktası tweak'inin toplu işlemde ilk sıraya alınması (kullanıcı sırası korunur).

## 6. Doğrulama kontrol listesi

**Kaldırıcı**
- [ ] "Sistem bileşenlerini göster" açıkken liste eski sürümle aynı sayıda program buluyor.
- [ ] MSI kaydı ayrıntıda `/X{GUID} /qn /norestart` gösteriyor.
- [ ] Var olmayan bir `.exe`'ye işaret eden sahte HKCU kaydı "geçersiz" olarak işaretleniyor.
- [ ] Geçersiz kaydı silmek `.reg` yedeği üretiyor.
- [ ] CSV Excel'de Türkçe karakterlerle açılıyor, `=` ile başlayan hücreler formül olmuyor.
- [ ] İki öğeli toplu kaldırma iptal edilebiliyor.

**İnce Ayarlar**
- [ ] Sayfa donmadan durumlar (Applied / Not applied / Mixed) görünüyor.
- [ ] Bir HKCU anahtarı uygula → regedit'te doğrula → geri al → eski değer dönüyor.
- [ ] `<RemoveEntry>` durumu geri alırken değeri siliyor, anahtarı bırakıyor.
- [ ] Advanced grubunda onay diyaloğu `destructive` olarak açılıyor.
- [ ] Dil değiştirilip geri dönüldüğünde seçimler korunuyor.

**Yapılandırma**
- [ ] 14 klasik panelin tamamı açılıyor.
- [ ] NTP düzeltmesi `w32tm` çıktısını günlüğe yazıyor.
- [ ] Özellik kurulumu 3010 kodunu "yeniden başlatma gerekli" olarak raporluyor.
- [ ] Yönetici olmayan oturumda düğmeler kapalı ve nedeni görünüyor.

**Genel**
- [ ] `build.bat` sıfır hatayla derliyor, EXE < 1 MB.
- [ ] `python tools/gen_winutil.py` yeniden çalıştırıldığında `src/TweaksData.cs` değişmiyor.
- [ ] Koyu tema ve 320 px genişlikte taşma yok.
- [ ] Durum çubuğu "7 bölüm" yazıyor.
