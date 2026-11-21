# Roadmap

MineManager is a **beta** project: the current build is usable, but the tool will keep growing in
breadth and depth. This file is the public plan — items move, and nothing here is a promise of a
date. Issues and pull requests that match the list are welcome.

Status legend: **Next** → planned for the upcoming beta · **Later** → after 1.0 · **Idea** →
under evaluation.

## Next

| Item | Notes |
|---|---|
| **Startup manager page** | Read and toggle `Run`/`RunOnce`, Startup folder and scheduled-task autostarts, with a per-entry backup before disabling. |
| **Services manager page** | Read-only service list first, then start type changes with a restore point-independent snapshot. |
| **Cleanup profiles** | Save a category selection as a named profile and run it from one button. |
| **Tweak search & filters** | Search across the 67-item catalogue; filter by applied state and risk level. |
| **More locales** | German, Spanish and Dutch UI packs on top of English/Turkish, with a translator-friendly key file. |
| **Better leftover matching** | Widen heuristic coverage (Windows Installer cache, `ProgramData` rules) while keeping the manual review step. |
| **Signed builds** | Sign the portable EXE and the zip so SmartScreen warnings can be reduced. |

## Later

| Item | Notes |
|---|---|
| **Disk space analyzer** | Treemap of the largest folders with jump-to-folder and safe delete. |
| **Windows Update cleanup tools** | Component store analysis (`DISM /AnalyzeComponentStore`) and guided cleanup. |
| **Driver inventory** | Installed driver list with vendor, version, date and device state. |
| **UWP / Store app management** | Needs a move away from the current C# 5 + single-file constraint. |
| **CLI mode** | `MineManager.exe /clean:profile` style non-interactive runs for scripting. |
| **ARM64 build** | Separate binary for Windows on ARM. |
| **Import/export settings** | Portable profile with language, theme, cleanup selection and tweak snapshot. |
| **Automated UI regression suite** | Grow `tests/Smoke.cs` into a repeatable render-diff check in CI. |

## Idea / evaluating

- Restore-point creation before high-risk batches (where System Restore is enabled).
- Scheduled cleanup through Task Scheduler with an explicit opt-in.
- Optional, manual update check (no background updater, no telemetry).
- Per-tweak documentation links in the info dialog.
- Community translation files loaded from a folder next to the EXE.

## Not planned

- Telemetry, analytics or crash reporting.
- Installers, services or background agents.
- Runtime downloads of third-party binaries (Sysinternals, O&O ShutUp10++, etc.).
- Silent, unattended removal of programs without an explicit confirmation dialog.

---

# Yol haritası (Türkçe özet)

MineManager bir **beta** projedir: bugünkü sürüm kullanılabilir, ancak araç genişlemeye devam
edecek. Bu dosya herkese açık plandır; maddeler değişebilir ve hiçbiri tarih taahhüdü değildir.

**Sıradaki:** başlangıç (autostart) yöneticisi, hizmetler sayfası, temizlik profilleri, tweak
arama/filtre, yeni diller (Almanca, İspanyolca, Hollandaca), kalıntı eşleştirmesinin genişletilmesi
ve imzalı derlemeler.

**Sonrası:** disk alanı analizi (treemap), Windows Update bileşen deposu temizliği, sürücü
envanteri, UWP/Store uygulama yönetimi, CLI modu, ARM64 derlemesi, ayar içe/dışa aktarma ve
otomatik UI regresyon testleri.

**Planlanmayan:** telemetri, arka plan ajanı/güncelleyici, çalışma anında üçüncü taraf ikili indirme
ve onay istemeden sessiz program kaldırma.
