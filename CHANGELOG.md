# Changelog

All notable changes are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/). Until 1.0 every release is a beta, and a tag with a
pre-release suffix (`-beta`, `-rc`) is published as a GitHub pre-release.

> **MineManager is in public beta.** Every release is usable, but pages, catalogues and
> translations are still growing. See [ROADMAP.md](ROADMAP.md) for what comes next.

## [0.9.1] — 2026-11-24

First bugfix release, from the first user report. Reported by **Kyotor** — thanks for the
report and the detailed description.

### Fixed

- **Config page** — after a fix or feature job finished, every button on the page stayed
  disabled and only Cancel remained visible (the busy state was restored instead of cleared).
  The page is interactive again as soon as the job ends, and the job guard is now released
  even if a step fails unexpectedly.
- **Closing during a job** — the Windows tools a job started (chkdsk, sfc, DISM, …) are now
  stopped together with the window instead of running invisibly in the background and breaking
  later actions until the next reboot. The "Windows Update - Reset" service restore still
  finishes while closing, so the update services are never left stopped.

## [0.9.0] — 2026-11-22

First public beta. Single portable EXE, no installer, no runtime downloads.

### Added

- **Cleanup** — 13 categories (user/Windows temp, Windows Update cache, Delivery Optimization,
  Prefetch, Recycle Bin, browser caches for 8 browsers, WER reports, crash dumps, Windows logs,
  thumbnail cache, INetCache, setup leftovers), per-category size and file count, in-use files
  scheduled for deletion on next boot.
- **RAM** — live usage readout, Quick Free (`EmptyWorkingSet`) and Deep Clean (standby/modified
  page lists via `NtSetSystemInformation`), top-15 consumers and an activity log.
- **Uninstaller** — registry union (HKLM 64-bit, `WOW6432Node`, HKCU), search, system/update
  hiding, detected installer family (MSI, Inno, NSIS, InstallShield, SdbInst, PowerShell),
  sequential batch removal queue, quiet uninstall behind an explicit confirmation, leftover
  scan, invalid-entry detection with `reg export` backup, CSV export.
- **Tweaks** — 67 tweaks (18 essential, 22 advanced, 25 preferences, 2 performance plans) with
  background state probing, plan preview, pre-write value snapshot, reverse rollback and
  post-write verification.
- **Config** — 9 Windows features, 6 fixes, 14 legacy panels, PowerShell profile and OpenSSH
  remote access; DISM/feature state detection and per-step logging.
- **Hardware** — 8 sections (CPU, motherboard/BIOS, memory modules, GPU, storage, OS, network,
  other) with copy and TXT export.
- **Settings** — English/Turkish, light/dark graphite Aero, applied instantly.
- **Safety** — registry deletions exported first, `requireAdministrator` manifest, single job
  guard, confirmation dialogs for irreversible actions, no telemetry and no updater.
- **Portable packaging** — `MineManager-<version>-portable.zip` in every GitHub release.

### Known limits

- UWP/Microsoft Store apps are not listed.
- Silent uninstall commands are generated only for MSI, Inno Setup and NSIS.
- Browser caches can only be removed while the browsers are closed.
- Windows 7 runs the Aero theme, but only registry-based features are meaningful there.

[0.9.1]: https://github.com/expires0n/minemanager/releases/tag/v0.9.1
[0.9.0]: https://github.com/expires0n/minemanager/releases/tag/v0.9.0
