@echo off
setlocal

where dotnet >nul 2>nul
if errorlevel 1 (
  echo dotnet is not installed.
  exit /b 1
)

for /f "delims=" %%i in ('dotnet --list-sdks') do set HAS_SDK=1
if not defined HAS_SDK (
  echo .NET 8 SDK or newer is required to build this project.
  echo Install it from https://dotnet.microsoft.com/download/dotnet/8.0
  exit /b 1
)

set OUTDIR=%~dp0dist
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

dotnet publish "%~dp0ADHDFocusOverlay.csproj" -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "%OUTDIR%"
if errorlevel 1 exit /b %errorlevel%

echo Built: %OUTDIR%\ADHDFocusOverlay.exe
