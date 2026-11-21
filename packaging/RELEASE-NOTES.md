# MineManager — portable build

**MineManager is in public beta.** It is a single-EXE, install-free Windows system tool:
cleanup, RAM tools, a bulk uninstaller, WinUtil-style tweaks and config actions, and a
CPU-Z-style hardware report — all in one Aero-themed window.

> Beta: pages, tweak/config catalogues and languages keep growing, and behaviour or on-disk
> formats can still change between beta releases. Read the confirmation dialogs and back up what
> matters.

## Download (portable)

1. Download **`MineManager-<version>-portable.zip`** from the assets below.
2. Extract it anywhere — USB stick, desktop, a tools folder.
3. Run `MineManager.exe` and accept the UAC prompt.

No installer, no service and no runtime download: .NET Framework 4.8 is already part of
Windows 10/11. The app writes only next to itself (`MineManager.ini`, `MineManager.undo.tsv`,
`MineManager-backup\`), so deleting the extracted folder removes everything.

Verify the download with `SHA256SUMS.txt` (also shown as the asset digest on GitHub).

## Requirements

- Windows 10 or 11 (x64 or x86). The Aero theme also renders on Windows 7, where only
  registry-based features are meaningful.
- .NET Framework 4.8 (in-box on Windows 10/11).
- Administrator rights for cleanup, uninstall, tweak, DISM and feature actions.

## Links

- Features, screenshots and build instructions: [README.md](https://github.com/expires0n/minemanager#readme)
- Changes in this release: [CHANGELOG.md](https://github.com/expires0n/minemanager/blob/main/CHANGELOG.md)
- What is planned next: [ROADMAP.md](https://github.com/expires0n/minemanager/blob/main/ROADMAP.md)
- Bug reports: https://github.com/expires0n/minemanager/issues

## Licence

MIT — [LICENSE](https://github.com/expires0n/minemanager/blob/main/LICENSE). Adapted third-party
concepts and data are credited in [NOTICE](https://github.com/expires0n/minemanager/blob/main/NOTICE).

---

## Türkçe

**MineManager herkese açık beta aşamasındadır.** Tek EXE olarak çalışan, kurulum gerektirmeyen bir
Windows sistem aracı: temizlik, RAM araçları, toplu kaldırıcı, WinUtil tarzı ince ayarlar ve
yapılandırma, CPU-Z tarzı donanım raporu — tek Aero penceresinde.

**Kurulum:** `MineManager-<sürüm>-portable.zip` dosyasını indirip istediğiniz yere açın,
`MineManager.exe` dosyasını çalıştırıp UAC onayını verin. Kurulum, hizmet ve çalışma anında indirme
yoktur; .NET Framework 4.8 Windows 10/11'de hazırdır. Uygulama yalnızca kendi klasörüne yazar,
klasörü silmek kaldırmak için yeterlidir.

İndirmeyi `SHA256SUMS.txt` ile doğrulayabilirsiniz. Beta süresince sayfalar, kataloglar ve diller
genişlemeye devam edecek; onay pencerelerini okuyun ve önemli verilerinizi yedekleyin.

Yol haritası: https://github.com/expires0n/minemanager/blob/main/ROADMAP.md ·
Hata bildirimi: https://github.com/expires0n/minemanager/issues
