#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Generate src/TweaksData.cs from WinUtil's tweaks.json.

    python tools/gen_winutil.py

Data source : WinUtil - https://github.com/ChrisTitusTech/winutil
              config/tweaks.json (MIT licence, (c) Chris Titus Tech)
Input       : tools/data/winutil-tweaks.json  (WinUtil config/tweaks.json, vendored)
Output      : src/TweaksData.cs   (do not edit by hand)

Why generate instead of parsing JSON at runtime: the catalog only changes when
WinUtil is re-vendored, the C# compiler validates the data, and the literals stay
greppable so Turkish coverage can be checked.

Rules enforced here:
  * C# 5 output only: object initializers, no interpolation, no tuples.
  * registry/service/task metadata becomes RegOp/SvcOp/TaskOp arrays.
  * InvokeScript/UndoScript become Script/UndoScript (newline joined PowerShell).
  * WinUtil-internal helpers are rewritten to built-in tools; nothing is
    downloaded at runtime (no Invoke-WebRequest/RestMethod, curl, winget install,
    choco). Tweaks whose only implementation was a third-party download or a
    picker dialog are excluded (see EXCLUDE) or reimplemented natively.
"""

import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "tools", "data", "winutil-tweaks.json")
OUT = os.path.join(ROOT, "src", "TweaksData.cs")

REMOVE_ENTRY = "<RemoveEntry>"

GROUP_MAP = {
    "Essential Tweaks": "essential",
    "z__Advanced Tweaks - CAUTION": "advanced",
    "Customize Preferences": "prefs",
    "Performance Plans - NOT FOR LAPTOPS": "performance",
}
GROUP_ORDER = ["essential", "advanced", "prefs", "performance"]

# Tweaks that are deliberately not ported, with the reason shown in the report.
EXCLUDE = {
    "WPFOOSUbutton": "downloads and runs the third-party O&O ShutUp10++ executable",
    "WPFchangedns": "needs a DNS picker UI (Combobox with per-provider server lists)",
}

# A restart (or sign-out) is genuinely required to finish these.
NEEDS_RESTART = {
    "WPFTweaksHiber": "Restart Windows to release the hibernation file.",
    "WPFTweaksRestorePoint": "Restart Windows if System Restore was not enabled before.",
    "WPFTweaksReservedStorage": "Restart Windows to finish the reserved storage change.",
    "WPFTweaksWindowsAI": "Restart Windows to finish removing the AI components.",
    "WPFToggleS3Sleep": "Restart Windows to switch the sleep model.",
    "WPFToggleLongPaths": "Restart Windows so the long path support takes effect.",
    "WPFTweaksDisableExplorerAutoDiscovery": "Sign out and back in for Explorer to rebuild its view cache.",
}

# Slow tweaks: DISM/SFC/uninstaller class work needs more than the 180 s default.
TIMEOUTS = {
    "WPFTweaksReservedStorage": 900,
    "WPFTweaksDiskCleanup": 1800,
    "WPFTweaksWindowsAI": 1800,
    "WPFTweaksDeleteTempFiles": 600,
    "WPFTweaksRemoveEdge": 900,
    "WPFTweaksRemoveOneDrive": 900,
    "WPFTweaksRestorePoint": 900,
    "WPFTweaksSvcHostSplitting": 300,
    "WPFTweaksWidget": 300,
    "WPFTweaksBlockAdobeNet": 300,
    "WPFTweaksDisableIPv6": 300,
    "WPFTweaksDisableStoreSearch": 300,
    "WPFAddUltPerf": 300,
    "WPFRemoveUltPerf": 300,
}

# Registry entries stored as a per-choice value map: pick the two states a
# MineManager switch can express. Prefs tweaks must be a real on/off pair.
VALUE_CHOICE = ("Disabled (Compatibility)", "Enabled")

# ------------------------------------------------------------------ rewrites

# The Adobe block list used to be fetched from a third-party GitHub repo on every
# run. MineManager never downloads: the same intent is implemented with a small
# built-in host list that is added to (and removed from) the hosts file.
ADOBE_HOSTS = [
    "adobe-dns.adobe.com",
    "adobe-dns-2.adobe.com",
    "adobe-dns-3.adobe.com",
    "adobe-dns-4.adobe.com",
    "adobe.tt.omtrdc.net",
    "adobe.demdex.net",
    "iab.adobe.com",
    "sstats.adobe.com",
]

ADOBE_ON = "\n".join([
    "$hostsPath = \"$Env:SystemRoot\\System32\\drivers\\etc\\hosts\"",
    "$begin = \"# MineManager Adobe block begin\"",
    "$end = \"# MineManager Adobe block end\"",
    "",
    "# Built-in list: MineManager does not download the third-party block list.",
    "$entries = @(",
] + ["    \"127.0.0.1 " + h + "\"," for h in ADOBE_HOSTS] + [
    ")",
    "",
    "$already = Select-String -Path $hostsPath -Pattern ([regex]::Escape($begin)) -Quiet -ErrorAction SilentlyContinue",
    "if (-not $already) {",
    "    Add-Content -Path $hostsPath -Value $begin",
    "    Add-Content -Path $hostsPath -Value $entries",
    "    Add-Content -Path $hostsPath -Value $end",
    "}",
    "ipconfig /flushdns | Out-Null",
    "Write-Host \"Adobe telemetry hosts were blocked.\"",
])

ADOBE_OFF = "\n".join([
    "$hostsPath = \"$Env:SystemRoot\\System32\\drivers\\etc\\hosts\"",
    "$begin = \"# MineManager Adobe block begin\"",
    "$end = \"# MineManager Adobe block end\"",
    "",
    "$text = Get-Content -Path $hostsPath -Raw -ErrorAction Stop",
    "$pattern = \"(?s)\" + [regex]::Escape($begin) + \".*?\" + [regex]::Escape($end) + \"\\r?\\n?\"",
    "if ($text -match $pattern) {",
    "    $text = [regex]::Replace($text, $pattern, \"\")",
    "    Set-Content -Path $hostsPath -Value $text.TrimEnd() -Encoding ASCII",
    "}",
    "ipconfig /flushdns | Out-Null",
    "Write-Host \"Adobe telemetry hosts were unblocked.\"",
])

# Edge and OneDrive only come back from a vendor installer, which MineManager
# refuses to download. Undo keeps what can be done locally and says the rest.
ONEDRIVE_UNDO = "\n".join([
    "# OneDrive itself must be reinstalled from the official installer.",
    "if (Test-Path -LiteralPath 'HKLM:\\SYSTEM\\CurrentControlSet\\Services\\OneSyncSvc') {",
    "    Set-Service -Name OneSyncSvc -StartupType Automatic",
    "}",
    "Write-Host \"OneSyncSvc was re-enabled. Reinstall OneDrive from the official installer.\"",
])

ULTPERF_ON = "\n".join([
    "$duplicated = powercfg /duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
    "if ($LASTEXITCODE -ne 0) { throw \"powercfg could not duplicate the Ultimate Performance scheme (exit code $LASTEXITCODE).\" }",
    "$guid = ($duplicated | Select-String -Pattern '[A-Fa-f0-9-]{36}').Matches.Value",
    "if (-not $guid) { throw \"powercfg did not report a scheme GUID to activate.\" }",
    "powercfg /setactive $guid",
    "if ($LASTEXITCODE -ne 0) { throw \"powercfg could not activate the Ultimate Performance scheme (exit code $LASTEXITCODE).\" }",
    "Write-Host \"Ultimate Performance plan installed and activated.\"",
])

ULTPERF_OFF = "\n".join([
    "powercfg /restoredefaultschemes",
    "if ($LASTEXITCODE -ne 0) { throw \"powercfg could not restore the default power schemes (exit code $LASTEXITCODE).\" }",
    "Write-Host \"Power plans were reset to defaults.\"",
])

CLSID_CTX = "{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"

# id -> overrides applied after the generic cleanup
OVERRIDES = {
    # WinUtil's theme helper touches its own WPF UI; Explorer restart is the
    # part that actually applies the theme.
    "WPFToggleDarkMode": {"invoke": "Stop-Process -Name explorer -Force",
                          "undo": "Stop-Process -Name explorer -Force"},
    "WPFTweaksBlockAdobeNet": {"invoke": ADOBE_ON, "undo": ADOBE_OFF,
                               "note": "Blocks a built-in list of Adobe telemetry hosts in the hosts file. Nothing is downloaded."},
    "WPFTweaksRemoveOneDrive": {"undo": ONEDRIVE_UNDO,
                                "note": "Reinstalling OneDrive needs the official installer; undo only restores the service."},
    "WPFTweaksRemoveEdge": {"undo": "",
                            "note": "Edge has no offline installer. Reinstall it from Microsoft if you need the browser back."},
    # Set-WinUtilClassicContextMenu is a WinUtil function; the registry form is
    # the same thing and keeps state detection working.
    "WPFTweaksRightClickMenu": {"invoke": "", "undo": "", "registry": [
        {"Path": "HKCU:\\Software\\Classes\\CLSID\\" + CLSID_CTX + "\\InprocServer32",
         "Name": "", "Value": "", "Type": "String", "OriginalValue": REMOVE_ENTRY},
    ]},
    "WPFAddUltPerf": {"invoke": ULTPERF_ON, "undo": "",
                      "desc": "Adds and activates the hidden Ultimate Performance power plan.",
                      "note": "Not for laptops: it keeps the CPU at high clocks and drains the battery."},
    "WPFRemoveUltPerf": {"invoke": ULTPERF_OFF, "undo": "",
                         "desc": "Removes the Ultimate Performance plan and restores the default power schemes.",
                         "note": "Restores the Balanced and High performance plans Windows ships with."},
}

TYPE_MAP = {
    "dword": "DWord",
    "qword": "QWord",
    "string": "String",
    "expandstring": "ExpandString",
    "binary": "Binary",
    "multistring": "MultiString",
}


# ------------------------------------------------------------------- helpers

def cstr(text):
    """C# regular string literal."""
    out = ['"']
    for ch in text:
        if ch == "\\":
            out.append("\\\\")
        elif ch == '"':
            out.append('\\"')
        elif ch == "\n":
            out.append("\\n")
        elif ch == "\r":
            out.append("\\r")
        elif ch == "\t":
            out.append("\\t")
        else:
            out.append(ch)
    out.append('"')
    return "".join(out)


def vstr(text):
    """C# verbatim string literal (used for PowerShell blocks, keeps backslashes)."""
    return '@"' + text.replace('"', '""') + '"'


def norm_type(value):
    return TYPE_MAP.get(str(value or "dword").strip().lower(), "DWord")


def norm_reg_path(path):
    """"HKLM:\\SOFTWARE\\X" -> "HKLM\\SOFTWARE\\X" (engine takes no PS drive)."""
    p = path.replace("\\\\", "\\").strip()
    m = re.match(r"^(HKLM|HKCU|HKU|HKCR|HKCC):\\(.*)$", p, re.IGNORECASE)
    if m:
        return m.group(1).upper() + "\\" + m.group(2)
    return p


def norm_value(raw):
    """Registry value as the engine's text form (binary arrays become CSV bytes)."""
    if raw is None:
        return ""
    if isinstance(raw, list):
        return ",".join(str(int(b)) for b in raw)
    if isinstance(raw, bool):
        return "1" if raw else "0"
    return str(raw)


def clean_script(text):
    """Drop WinUtil-internal plumbing and rewrite its Explorer helper."""
    if not text:
        return ""
    keep = []
    for line in str(text).replace("\r\n", "\n").replace("\r", "\n").split("\n"):
        stripped = line.strip()
        if not stripped:
            keep.append("")
            continue
        if stripped.startswith("Write-WinUtilLog"):
            continue
        if stripped.startswith("Step-WinUtilJob"):
            continue
        line = line.replace('Invoke-WinUtilExplorerUpdate -action "restart"',
                            "Stop-Process -Name explorer -Force")
        line = line.replace("Invoke-WinUtilExplorerUpdate",
                            "Stop-Process -Name explorer -Force")
        keep.append(line.rstrip())
    while keep and not keep[0].strip():
        keep.pop(0)
    while keep and not keep[-1].strip():
        keep.pop()
    return "\n".join(keep)


def join_script(data, key):
    parts = data.get(key) or []
    if isinstance(parts, str):
        parts = [parts]
    return clean_script("\n".join(str(p) for p in parts))


def registry_ops(data, tid):
    entries = data.get("registry") or []
    if "registry" in OVERRIDES.get(tid, {}):
        entries = OVERRIDES[tid]["registry"]

    ops = []
    for entry in entries:
        value = norm_value(entry.get("Value", ""))
        original = norm_value(entry.get("OriginalValue", REMOVE_ENTRY))
        if entry.get("Values"):
            # Combobox-form entry: collapse to the on/off pair.
            values = entry["Values"]
            value = norm_value(values.get(VALUE_CHOICE[0], REMOVE_ENTRY))
            original = norm_value(values.get(VALUE_CHOICE[1],
                                            entry.get("DefaultValue", REMOVE_ENTRY)))
        ops.append({
            "Path": norm_reg_path(entry.get("Path", "")),
            "Name": str(entry.get("Name", "")),
            "Type": norm_type(entry.get("Type", "DWord")),
            "Value": value,
            "Original": original,
        })
    return ops


def service_ops(data):
    ops = []
    for entry in data.get("service") or []:
        startup = str(entry.get("StartupType", "Manual")).strip()
        if startup.lower() == "automaticdelayed":
            startup = "AutomaticDelayed"
        ops.append({
            "Name": str(entry.get("Name", "")),
            "Startup": startup,
            "Original": str(entry.get("OriginalType", "Manual")).strip(),
        })
    return ops


def task_ops(data):
    ops = []
    for entry in data.get("ScheduledTask") or []:
        ops.append({
            "Name": str(entry.get("Name", "")),
            "State": str(entry.get("State", "Disabled")).strip(),
            "Original": str(entry.get("OriginalState", "Enabled")).strip(),
        })
    return ops


def build():
    with open(SRC, "r", encoding="utf-8-sig") as handle:
        raw = json.load(handle)

    tweaks = []
    for tid, data in raw.items():
        if tid in EXCLUDE:
            continue
        category = data.get("category", "")
        group = GROUP_MAP.get(category)
        if not group:
            raise SystemExit("unknown category for %s: %s" % (tid, category))

        kind_type = str(data.get("Type", "") or "")
        if category == "Customize Preferences" or kind_type == "Toggle":
            kind = "toggle"
        elif kind_type == "Button":
            kind = "button"
        else:
            kind = "check"

        over = OVERRIDES.get(tid, {})
        invoke = over["invoke"] if "invoke" in over else join_script(data, "InvokeScript")
        undo = over["undo"] if "undo" in over else join_script(data, "UndoScript")

        desc = over.get("desc") or str(data.get("Description", "") or "").strip()
        if not desc:
            desc = str(data.get("Content", tid))

        note = over.get("note", "") or NEEDS_RESTART.get(tid, "")

        tweaks.append({
            "id": tid,
            "group": group,
            "kind": kind,
            "title": str(data.get("Content", tid)),
            "desc": desc,
            "note": note,
            "link": str(data.get("link", "")),
            "registry": registry_ops(data, tid),
            "services": service_ops(data),
            "tasks": task_ops(data),
            "script": invoke,
            "undo": undo,
            "timeout": TIMEOUTS.get(tid, 180),
            "danger": group == "advanced",
            "restart": tid in NEEDS_RESTART,
        })

    # Deterministic order: group priority, then the JSON order inside the group.
    for index, tweak in enumerate(tweaks):
        tweak["seq"] = index
    tweaks.sort(key=lambda t: (GROUP_ORDER.index(t["group"]), t["seq"]))
    return tweaks


# -------------------------------------------------------------------- emit

def emit(tweaks):
    lines = []
    add = lines.append

    add("// <auto-generated>")
    add("// Generated by tools/gen_winutil.py from WinUtil config/tweaks.json")
    add("// WinUtil - https://github.com/ChrisTitusTech/winutil")
    add("// MIT licence, (c) Chris Titus Tech. Do not edit by hand; re-run the generator.")
    add("// </auto-generated>")
    add("using System;")
    add("using System.Collections.Generic;")
    add("")
    add("namespace MineManager")
    add("{")
    add("    /// <summary>Generated from WinUtil config/tweaks.json - owner: tweaks data work package.</summary>")
    add("    internal static class TweakCatalog")
    add("    {")
    add("        private static List<Tweak> _all;")
    add("")
    add("        public static List<Tweak> All()")
    add("        {")
    add("            if (_all == null) _all = Build();")
    add("            return _all;")
    add("        }")
    add("")
    add("        private static List<Tweak> Build()")
    add("        {")
    add("            List<Tweak> list = new List<Tweak>();")
    add("")

    for tweak in tweaks:
        add("            // " + tweak["id"] + " - " + tweak["group"])
        add("            list.Add(new Tweak")
        add("            {")
        add("                Id = %s," % cstr(tweak["id"]))
        add("                Group = %s," % cstr(tweak["group"]))
        add("                Title = %s," % cstr(tweak["title"]))
        add("                Desc = %s," % cstr(tweak["desc"]))
        add("                Note = %s," % cstr(tweak["note"]))
        add("                Link = %s," % cstr(tweak["link"]))
        add("                Kind = %s," % cstr(tweak["kind"]))
        add("                TimeoutSec = %d," % tweak["timeout"])
        if tweak["danger"]:
            add("                Danger = true,")
        if tweak["restart"]:
            add("                NeedsRestart = true,")

        if tweak["registry"]:
            add("                Registry = new RegOp[]")
            add("                {")
            for op in tweak["registry"]:
                add("                    new RegOp { Path = %s, Name = %s, Type = %s, Value = %s, Original = %s },"
                    % (cstr(op["Path"]), cstr(op["Name"]), cstr(op["Type"]),
                       cstr(op["Value"]), cstr(op["Original"])))
            add("                },")
        if tweak["services"]:
            add("                Services = new SvcOp[]")
            add("                {")
            for op in tweak["services"]:
                add("                    new SvcOp { Name = %s, Startup = %s, Original = %s },"
                    % (cstr(op["Name"]), cstr(op["Startup"]), cstr(op["Original"])))
            add("                },")
        if tweak["tasks"]:
            add("                Tasks = new TaskOp[]")
            add("                {")
            for op in tweak["tasks"]:
                add("                    new TaskOp { Name = %s, State = %s, Original = %s },"
                    % (cstr(op["Name"]), cstr(op["State"]), cstr(op["Original"])))
            add("                },")
        if tweak["script"]:
            add("                Script = %s," % vstr(tweak["script"]))
        if tweak["undo"]:
            add("                UndoScript = %s," % vstr(tweak["undo"]))
        add("            });")
        add("")

    add("            return list;")
    add("        }")
    add("")
    add("        public static Tweak ById(string id)")
    add("        {")
    add("            List<Tweak> all = All();")
    add("            for (int i = 0; i < all.Count; i++)")
    add("                if (string.Equals(all[i].Id, id, StringComparison.Ordinal)) return all[i];")
    add("            return null;")
    add("        }")
    add("")
    add("        /// <summary>Tweaks of one group in display order.</summary>")
    add("        public static List<Tweak> Group(string group)")
    add("        {")
    add("            List<Tweak> list = new List<Tweak>();")
    add("            List<Tweak> all = All();")
    add("            for (int i = 0; i < all.Count; i++)")
    add("                if (string.Equals(all[i].Group, group, StringComparison.Ordinal)) list.Add(all[i]);")
    add("            return list;")
    add("        }")
    add("")
    add("        public static string GroupTitle(string group)")
    add("        {")
    add('            if (group == "essential") return L.T("Essential Tweaks");')
    add('            if (group == "advanced") return L.T("Advanced Tweaks - CAUTION");')
    add('            if (group == "prefs") return L.T("Customize Preferences");')
    add('            if (group == "performance") return L.T("Performance Plans - NOT FOR LAPTOPS");')
    add("            return group;")
    add("        }")
    add("    }")
    add("}")
    add("")
    return "\n".join(lines)


def main():
    tweaks = build()
    text = emit(tweaks)

    # Guard rails: the engine relies on these invariants.
    for tweak in tweaks:
        for word in ("Invoke-WebRequest", "Invoke-RestMethod", "curl ", "winget install", "choco"):
            if word in tweak["script"] or word in tweak["undo"]:
                raise SystemExit("download call %r left in %s" % (word, tweak["id"]))
        if tweak["group"] == "prefs" and (not tweak["registry"] or tweak["kind"] != "toggle"):
            raise SystemExit("prefs tweak without a registry pair: %s" % tweak["id"])

    with open(OUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)

    counts = {}
    for tweak in tweaks:
        counts[tweak["group"]] = counts.get(tweak["group"], 0) + 1
    print("wrote %s" % os.path.relpath(OUT, ROOT).replace("\\", "/"))
    print("tweaks: %d  (%s)" % (len(tweaks),
                                ", ".join("%s=%d" % (g, counts.get(g, 0)) for g in GROUP_ORDER)))
    print("excluded: %s" % ", ".join(sorted(EXCLUDE)))


if __name__ == "__main__":
    main()
