# Changelog

All notable changes are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses
[Semantic Versioning](https://semver.org/). Until 1.0 every release is a beta, and a tag with a
pre-release suffix (`-beta`, `-rc`) is published as a GitHub pre-release.

> **MineManager is in public beta.** Every release is usable, but pages, catalogues and
> translations are still growing. See [ROADMAP.md](ROADMAP.md) for what comes next.

## [Unreleased]

### Added

- **CI workflow** — every push and pull request to `main` builds the EXE, checks the Turkish
  translation keys, runs the recovery parser test and the 180-layout smoke test, and uploads the
  screenshots. A smoke page that throws, or fewer than 180 screenshots, fails the run.
- `tools/check_translations.py` — fails when a literal `L.T`/`L.F` text has no exact Turkish key
  (for example a space where the code has a line break) or a key is registered twice.

## [0.9.2] — 2026-11-25

Second beta. Adds the read-only **Diagnostics** page and a guided **Recovery** page, and turns the
repair "Finished" line into a report a non-expert can read. Safety work in cleanup, undo, uninstall
and feature installation rides along.

### Added

- **Diagnostics** — seven read-only checks in one page: system summary (Windows edition/build,
  uptime, free space, elevation), Restart Manager file/folder locks, Win32/HRESULT error decoder,
  `powercfg` battery capacity report, display-path measure/compare, local route lookup and a
  Windows event maintenance timeline.
- **Recovery** — one **Start all** button runs the documented order: DISM `CheckHealth` →
  `ScanHealth` → `RestoreHealth` → `sfc /scannow` → `chkdsk C: /scan`. Live log, Stop button and
  per-step state.
- **Recovery report** — after the run, a warning report gives every step one of six verdicts
  (Clean, Repaired, Issues found, Failed, Stopped, Unclear), the important log line and a restart
  note when needed. The same text stays in the summary strip and opens again from **Report**;
  **Copy log** copies the full log.
- **Hardware change tracking** — the next Hardware reading shows what changed since the previous
  one (value, appearance, missing item) instead of silently overwriting it.
- **Share-safe hardware report** — **Copy** and **Save TXT** preview the exact text first and can
  mask the computer name, user name, profile path, serials, IP and MAC addresses. Nothing is
  uploaded.
- **RAM impact card** — every clean records available memory before, right after and 30 seconds
  later, labelled as a system-wide change rather than memory "freed by MineManager".
- **Clean backup packages** in the Uninstaller deletes MineManager's own `.reg`/task-XML backup
  folders after an explicit confirmation.
- **What is using this file?** after a failed leftover delete, with Restart Manager naming the
  holder.
- **Wider leftover scan** — scheduled tasks, PATH entries, App Paths, ProgID, shell extensions,
  COM, firewall rules, compatibility layers and loose product files. Task XML and parent registry
  keys are backed up before deletion.

### Changed

- **Cleanup is scan-bound:** a file modified after the scan is left alone and counted, so what was
  measured and approved is what gets deleted.
- **Feature installs stay offline:** DISM now uses `/LimitAccess`, and `0x800F081F` explains that
  the payload is not on this machine instead of silently downloading gigabytes.
- **MSI handling:** exit 1618 (another installation) is retried after 15/30/60/60 s without killing
  the other process; exit 1641 stops the queue instead of starting the next uninstaller on a
  half-rebooted machine; MSI codes are only interpreted as MSI when `msiexec` was actually used.
- **Undo is conflict-checked:** a registry value that changed after MineManager wrote it is skipped
  and reported, never overwritten. The snapshot format is v2 and still reads v1.
- **Diagnostics layout** — compact cards instead of six tall empty placeholders. Two columns at
  760 px and wider, one column on narrow windows; only the console of the tool you ran expands.
  All six tools fit at 1100×720 without scrolling.
- **Themed inputs** — diagnostics text boxes and their native scroll bars now follow the light/dark
  palette instead of rendering as white boxes in the dark theme.
- **Honest repair counts** — Windows does not expose a reliable "N files repaired" number for SFC
  or DISM. The report names the repaired step and says so explicitly instead of inventing a count.
- `chkdsk` still runs with `/scan` only: no `/f`, no `/r`, no forced reboot.

### Fixed

- Unrecognised SFC/DISM output was silently treated as **Clean**; it is now **Unclear** and says
  that the result line was not recognised.
- DISM `CheckHealth`/`ScanHealth` reporting a repairable image is now **Issues found**, not
  **Repaired**. Only `RestoreHealth` success is a repair.
- The file-lock result no longer leaves the old "Checking…" line above the report.
- A recovery run started without elevation now recognises the SFC "must be an administrator"
  message instead of reporting an unknown result.
- A recovery run stopped early marks the remaining steps **Stopped** instead of leaving them
  **Waiting** forever.
- Light-theme transitions no longer force the global dark control theme onto native text boxes.
- Cleanup counts only the files it actually deleted; failures stay in the error count.

### Verification

- `build.bat`: 0 errors, 0 warnings.
- Smoke render: 180 layouts (9 pages × 5 widths × 2 languages × 2 themes).
- Recovery parser self-check: clean/repaired/issues/failed/stopped/unknown, English + Turkish,
  SFC UTF-16 and DISM/CHKDSK OEM encodings.

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

[0.9.2]: https://github.com/expires0n/minemanager/releases/tag/v0.9.2-beta
[0.9.1]: https://github.com/expires0n/minemanager/releases/tag/v0.9.1
[0.9.0]: https://github.com/expires0n/minemanager/releases/tag/v0.9.0