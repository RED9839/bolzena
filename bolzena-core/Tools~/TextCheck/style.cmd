@echo off
rem Style and term check (see Docs). usage: style.cmd [--stage2] [--only area] [--show N]
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\DotNetSdk\dotnet.exe" build -c Release -nologo -v q -o "%TEMP%\bz_style" "%~dp0Style\Style.csproj" >nul || exit /b 2
"%TEMP%\bz_style\StyleDump.exe" C:\projects\bolzena-content-v2 heroes > "%TEMP%\bz_style_all.json"
python "%~dp0Style\style_check.py" %*
