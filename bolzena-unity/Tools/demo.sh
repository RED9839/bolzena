#!/bin/sh
# 자동 데모 — 빌드한 exe 를 -demo 로 돌려 Captures/ 에 캡처를 남긴다.
#   ./Tools/demo.sh          한 판(로비 → 마을 → 편성 → 지도 → 전투 · 보상 · 이벤트 · 캠프 · 상점 → 2층 보스 → 끝 → 지기)
#   ./Tools/demo.sh battle   전투 시범만(예전 데모 — 시범 파티 · 웨이브 둘)
# 지킴이: 데모는 예외 · 시간 초과면 스스로 오류 코드로 꺼진다(판 데모 2 예외 · 3 시간 초과 / 전투 3 예외 · 4 시간 초과 / DemoGuard).
# 그래도 남으면 여기서 끊고(timeout) 남은 Bolzena.exe 를 지운다.
cd /c/projects/bolzena-unity
rm -rf Captures && mkdir -p Captures
if [ "$1" = "battle" ]; then ARGS="-battle"; LIMIT=330; else ARGS="-demo-timeout 1500"; LIMIT=1560; fi
timeout -k 10 $LIMIT ./Build/Bolzena.exe -demo $ARGS -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "C:\projects\bolzena-unity-tmp\player.log"
code=$?
echo "player exit=$code"
if [ $code -eq 124 ] || [ $code -eq 137 ]; then echo "시간 제한으로 끊음"; fi
taskkill //F //IM Bolzena.exe >/dev/null 2>&1 && echo "남은 Bolzena.exe 를 지움"
grep -E "\[Demo\]|\[Bridge\]|\[Capture\]|Exception|Error" /c/projects/bolzena-unity-tmp/player.log | head -120
exit $code
