# Builds dist\MineManager.exe and MineManager-<version>-portable.zip.
# Uses the in-box .NET Framework compiler; no SDK, no NuGet, no Python needed
# because assets\app.ico and assets\logo64.png are committed.
#
#   powershell -ExecutionPolicy Bypass -File packaging\make-portable.ps1
#   powershell -ExecutionPolicy Bypass -File packaging\make-portable.ps1 -Version v0.9.1
#
# The zip contains: MineManager.exe, README.md, LICENSE, NOTICE, PORTABLE-README.txt

param(
    [string]$Version = "dev",
    [string]$OutDir = "dist"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) { throw "The .NET Framework compiler (csc.exe) was not found." }

New-Item -ItemType Directory -Force $OutDir | Out-Null

& $csc /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /debug- `
    /win32icon:assets\app.ico `
    /resource:assets\logo64.png,MineManager.Resources.logo.png `
    /win32manifest:src\app.manifest `
    /out:"$OutDir\MineManager.exe" `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll `
    /r:System.Windows.Forms.dll /r:System.Management.dll `
    /recurse:src\*.cs
if ($LASTEXITCODE -ne 0) { throw "Compilation failed with exit code $LASTEXITCODE" }

$stage = "MineManager-$Version-portable"
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item "$OutDir\MineManager.exe" $stage\
Copy-Item README.md, LICENSE, NOTICE, packaging\PORTABLE-README.txt $stage\

$zip = "$stage.zip"
if (Test-Path $zip) { Remove-Item -Force $zip }
Compress-Archive -Path "$stage\*" -DestinationPath $zip -Force
Remove-Item -Recurse -Force $stage

$exe = Get-Item "$OutDir\MineManager.exe"
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
Write-Host ""
Write-Host ("[OK] {0}  ({1:N0} bytes)" -f $exe.FullName, $exe.Length)
Write-Host ("[OK] {0}  ({1:N0} bytes)" -f (Resolve-Path $zip).Path, (Get-Item $zip).Length)
Write-Host ("     SHA-256 {0}" -f $hash)
Write-Host ""
Write-Host "Attach $zip to the GitHub release:"
Write-Host ("     gh release create {0} {1} --title `"MineManager {0}`" --prerelease --generate-notes" -f $Version, $zip)
