@echo off
setlocal
title Build Acts from the Past: Config Sub-Mod
cd /d "%~dp0"
echo ============================================================
echo   Building "Acts from the Past: Config Sub-Mod"
echo   (No Visual Studio needed. First run downloads ~200 MB of
echo    Microsoft's free .NET 9 SDK into a ".dotnet" folder here.)
echo ============================================================
echo.

if exist ".dotnet\dotnet.exe" goto build

echo Downloading the .NET 9 SDK (one time only)...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12;" ^
  "$s = Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1';" ^
  "& ([scriptblock]::Create($s.Content)) -Channel 9.0 -InstallDir '%~dp0.dotnet' -NoPath"
if not exist ".dotnet\dotnet.exe" (
  echo.
  echo Could not download the .NET SDK. Check your internet connection and try again.
  goto end
)

:build
echo.
echo Building...
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1
".dotnet\dotnet.exe" build "ActsFromThePastConfig.csproj" -c Release -nologo -v minimal
if errorlevel 1 (
  echo.
  echo ********  BUILD FAILED  ********
  echo Take a screenshot of the red lines above and send it to Claude.
) else (
  echo.
  echo ********  DONE  ********
  echo The mod was copied into your Slay the Spire 2\mods\ActsFromThePastConfig folder.
)

:end
echo.
pause
