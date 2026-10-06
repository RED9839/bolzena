@echo off
rem Text vs rule check (Docs/Ό³Έν±Ϋ.md). usage: run.cmd [content folder]
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1
set DATA=%1
if "%DATA%"=="" set DATA=C:\projects\bolzena-content-v2
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\DotNetSdk\dotnet.exe" build -c Release -nologo -v q -o "%TEMP%\bz_textcheck" "%~dp0Dump.csproj" >nul || exit /b 2
"%TEMP%\bz_textcheck\Dump.exe" %DATA% json > "%TEMP%\bz_traits.json"
python "%~dp0text_rule_check.py" "%TEMP%\bz_traits.json" %DATA%
