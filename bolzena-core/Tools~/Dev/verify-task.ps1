# 프론티어 검증기 예약 작업(2026-10-19 사용자 승인 — 「중간 방법 3」). 설명: bolzena-unity/Docs/프론티어_서버.md §검증기 예약 작업
#   인자 없이  : 한 번 돈다(작업 스케줄러가 5분마다 부른다) — bz verify(Release 빌드 · 콘텐츠 bolzena-content-v2) → 날짜별 로그 · 오래된 로그 정리.
#   -Install   : 작업 「Bolzena bz verify」 등록(로그인했을 때만 · 창 없이 · 5분마다 · 겹치면 건너뜀 · 30분 넘으면 멈춤).
#   -Uninstall : 작업 지우기.
# 주소 · 토큰은 사용자 환경 변수 BZ_VERIFY_URL · BZ_VERIFY_TOKEN(값은 로그에 찍지 않는다 — 있는지만).
# 엔진 · 콘텐츠를 바꾸면 build.cmd 로 Release 를 다시 빌드할 것(작업이 도는 동안엔 Dev.dll 이 잠겨 빌드가 실패할 수 있다 — 작업이 끝난 뒤 · 또는 -Uninstall 뒤).
param([switch]$Install, [switch]$Uninstall)

$TaskName = "Bolzena bz verify"
$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$Bz = Join-Path $Here "bz.cmd"
$Content = "C:\projects\bolzena-content-v2"
$LogDir = Join-Path $env:USERPROFILE ".bolzena\verify-logs"
$KeepDays = 14

if ($Uninstall) {
  Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
  Write-Output "지움 — $TaskName"
  return
}

if ($Install) {
  # 창 없이 — conhost --headless 로 PowerShell 을 띄운다(-WindowStyle Hidden 은 잠깐 창이 번쩍인다)
  $ps = Join-Path $env:SystemRoot "System32\WindowsPowerShell\v1.0\powershell.exe"
  $action = New-ScheduledTaskAction -Execute (Join-Path $env:SystemRoot "System32\conhost.exe") `
    -Argument "--headless `"$ps`" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File `"$($MyInvocation.MyCommand.Path)`"" -WorkingDirectory $Here
  $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 5)
  $logon = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
  $settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Minutes 30) `
    -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
  $principal = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
  Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger @($trigger, $logon) -Settings $settings -Principal $principal `
    -Description "볼제나 프론티어 검증기 — bz verify 를 5분마다(bolzena-core/Tools~/Dev/verify-task.ps1)" -Force | Out-Null
  Write-Output "등록 — $TaskName (5분마다 · 로그인했을 때만 · 창 없이 · 겹치면 건너뜀) · 로그 $LogDir"
  return
}

# ── 한 번 돌기 ──
New-Item -ItemType Directory -Force $LogDir | Out-Null
$log = Join-Path $LogDir ("verify-" + (Get-Date -Format "yyyyMMdd") + ".log")
function Say($m) { [System.IO.File]::AppendAllText($log, (Get-Date -Format "yyyy-MM-dd HH:mm:ss") + " [작업] " + $m + "`r`n", (New-Object System.Text.UTF8Encoding($false))) }

# 오래된 로그 정리
Get-ChildItem $LogDir -Filter "verify-*.log" -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$KeepDays) } | Remove-Item -Force -ErrorAction SilentlyContinue

$url = [Environment]::GetEnvironmentVariable("BZ_VERIFY_URL", "User"); if (-not $url) { $url = $env:BZ_VERIFY_URL }
$tok = [Environment]::GetEnvironmentVariable("BZ_VERIFY_TOKEN", "User"); if (-not $tok) { $tok = $env:BZ_VERIFY_TOKEN }
if (-not $url -or -not $tok) { Say "환경 변수 BZ_VERIFY_URL · BZ_VERIFY_TOKEN 이 없다(주소 $([bool]$url) · 토큰 $([bool]$tok)) — 건너뜀"; exit 2 }
$env:BZ_VERIFY_URL = $url; $env:BZ_VERIFY_TOKEN = $tok

& $Bz verify --data $Content --log $log *> $null
$code = $LASTEXITCODE
$what = switch ($code) { 0 { "돌았음" } 1 { "서버 · 네트워크 오류" } 2 { "설정 오류" } 3 { "다른 검증기가 도는 중 — 건너뜀" } default { "알 수 없음" } }
Say "끝 코드 $code($what)"
exit $code
