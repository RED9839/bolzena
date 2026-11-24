#!/bin/sh
# 재기 — 캡처 없이 실제 시계로 전투 데모를 돌려 프레임 시간(전체 · 고학년 구간)을 로그에 남긴다
# -battle 이 있어야 [Perf] 를 찍는 DemoRunner 로 간다. -demo 만이면 판 데모로 가서 [Perf] 가 안 나오고 판 저장을 지운다(2026-11-05)
cd /c/projects/bolzena-unity
timeout -k 10 360 ./Build/Bolzena.exe -demo -battle -perf -mute -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "C:\projects\bolzena-unity-tmp\perf.log"
echo "player exit=$?"
grep -E "\[Perf\]|Exception" /c/projects/bolzena-unity-tmp/perf.log
