# EditMode 테스트를 유니티 batchmode 로 돌린다.
#   powershell -File run-tests.ps1 [-Filter 이름조각]
# 결과: bolzena-core-test\TestResults.xml · 로그: bolzena-core-test\test.log
param([string]$Filter = "", [string]$Project = "C:\projects\bolzena-core-test")
$u = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
$res = Join-Path $Project "TestResults.xml"; $log = Join-Path $Project "test.log"
Remove-Item $res -ErrorAction SilentlyContinue
$a = @('-batchmode','-nographics','-projectPath',$Project,'-runTests','-testPlatform','EditMode','-testResults',$res,'-logFile',$log)
if ($Filter) { $a += @('-testFilter',$Filter) }
$p = Start-Process -FilePath $u -ArgumentList $a -Wait -PassThru
"exit $($p.ExitCode)"
Select-String -Path $log -Pattern "error CS\d+" | % { $_.Line } | Sort-Object -Unique | Select-Object -First 40
if (Test-Path $res) {
  $x = [xml](Get-Content $res -Encoding UTF8)
  $r = $x.'test-run'
  "tests $($r.total) passed $($r.passed) failed $($r.failed) skipped $($r.skipped)"
  $x.SelectNodes("//test-case[@result='Failed']") | % { "FAIL $($_.fullname)`n  $($_.failure.message.'#cdata-section')" } | Select-Object -First 30
}
