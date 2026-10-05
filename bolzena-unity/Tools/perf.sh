#!/bin/sh
# 재기 — 캡처 없이 실제 시계로 데모를 돌려 프레임 시간(전체 · 고학년 구간)을 로그에 남긴다
cd /c/projects/bolzena-unity
timeout -k 10 360 ./Build/Bolzena.exe -demo -perf -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "C:\projects\bolzena-unity-tmp\perf.log"
echo "player exit=$?"
grep -E "\[Perf\]|Exception" /c/projects/bolzena-unity-tmp/perf.log
