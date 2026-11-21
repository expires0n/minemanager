@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0"

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [HATA] .NET Framework derleyicisi bulunamadi: %CSC%
  pause
  exit /b 1
)

if not exist "assets\app.ico" (
  echo [BILGI] Simge uretiliyor...
  python tools\make_icon.py
)
if not exist "assets\logo64.png" (
  echo [BILGI] Baslik ikonu uretiliyor...
  python tools\make_icon.py
)

if not exist "dist" mkdir "dist"

"%CSC%" /nologo /target:winexe /platform:anycpu /optimize+ /codepage:65001 /debug- ^
 /win32icon:"assets\app.ico" ^
 /resource:"assets\logo64.png",MineManager.Resources.logo.png ^
 /win32manifest:"src\app.manifest" ^
 /out:"dist\MineManager.exe" ^
 /r:System.dll ^
 /r:System.Core.dll ^
 /r:System.Drawing.dll ^
 /r:System.Windows.Forms.dll ^
 /r:System.Management.dll ^
 /recurse:"src\*.cs"

if errorlevel 1 (
  echo.
  echo [HATA] Derleme basarisiz.
  pause
  exit /b 1
)

echo.
echo [OK] Derlendi: dist\MineManager.exe
for %%F in ("dist\MineManager.exe") do echo      Boyut: %%~zF bayt
echo.
