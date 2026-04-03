@echo off
setlocal

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set OUTDIR=%~dp0dist
if not exist "%OUTDIR%" mkdir "%OUTDIR%"

"%CSC%" /nologo /target:winexe /out:"%OUTDIR%\ADHDFocusOverlay.exe" /reference:System.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Core.dll /reference:Microsoft.CSharp.dll Program.cs AppState.cs Geometry.cs DragMode.cs NativeMethods.cs OverlayForm.cs BorderForm.cs SettingsForm.cs OverlayAppContext.cs
if errorlevel 1 exit /b %errorlevel%

echo Built: %OUTDIR%\ADHDFocusOverlay.exe
