@echo off
rem Bolzena content console - runs the prebuilt Dev.dll (safe to call concurrently). Rebuild with build.cmd after engine changes.
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\DotNetSdk\dotnet.exe" "%~dp0bin\Release\net8.0\Dev.dll" %*
