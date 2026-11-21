MineManager - portable build
============================

This folder is the complete application. There is nothing to install.

1. Run MineManager.exe
2. Accept the Windows UAC prompt (the app needs administrator rights for cleanup,
   uninstaller, tweak and feature actions).
3. Pick a page in the left navigation.
4. Optional: Settings -> Language -> Turkce, Settings -> Appearance -> Dark.

Files the app may create next to the EXE:
  MineManager.ini          your settings (language, theme)
  MineManager.undo.tsv     tweak undo snapshot
  MineManager-backup\      registry exports taken before deletions

To uninstall: delete this folder. Nothing is written to Program Files,
no service is installed and no registry entry is added for the app itself.

STATUS: public beta. Actively developed - pages, tweak/config catalogues and
languages keep growing. Read the release notes before updating and back up
anything important. Feedback: https://github.com/expires0n/minemanager/issues

Requires Windows 10 or 11 (the Aero theme also renders on Windows 7, where only
registry-based features are meaningful). .NET Framework 4.8 is included in
Windows 10/11, so no runtime download is needed.

MineManager is licensed under the MIT License (see LICENSE). Third-party
attribution is in NOTICE.
