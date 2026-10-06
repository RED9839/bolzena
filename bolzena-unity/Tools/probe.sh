#!/bin/sh
# 사람 입력 재현 — 가상 마우스(Input System)로 카드를 끌어 낸다. 진짜 커서는 건드리지 않는다. 결과: [Probe] 로그 · Captures/probe/
#   ./Tools/probe.sh battle [추가 인자…]   전투만(-battle). 예: ./Tools/probe.sh battle -oldbox -party 란,에르핀_왕도,비비 -foes goldring_elite,marshmallowsupporter
#   ./Tools/probe.sh run                   판 화면은 데모가 넘기고(-demo -humanfight) 첫 싸움을 가상 마우스로
# 프로브는 240초 · 싸움 하나 끝에 스스로 꺼진다. 그래도 남으면 여기서 끊고 지운다.
cd /c/projects/bolzena-unity
mode=${1:-battle}; shift
name=${PROBE_NAME:-$mode}
if [ "$mode" = "run" ]; then ARGS="-demo -humanfight -demo-timeout 400"; else ARGS="-battle"; fi
timeout -k 10 300 ./Build/Bolzena.exe $ARGS -inputprobe "$@" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "C:\projects\bolzena-unity-tmp\probe_$name.log"
echo "exit=$?"
taskkill //F //IM Bolzena.exe >/dev/null 2>&1 && echo "남은 Bolzena.exe 를 지움"
grep -E "\[Probe\]|\[Battle\]|\[Hand\]|\[Bridge\]|Exception" "/c/projects/bolzena-unity-tmp/probe_$name.log" | head -80
