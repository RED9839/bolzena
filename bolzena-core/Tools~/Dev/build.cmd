@echo off
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\DotNetSdk\dotnet.exe" build -c Release "%~dp0Dev.csproj" -nologo -v q
