#!/bin/sh
# 자동 데모 — 빌드한 exe 를 -demo 로(1600×900, 그리고 폰 가로 844×390) 돌려 Captures/ 에 남긴다
P=${1:-C:/projects/bolzena-runui-test}
rm -rf "$P/Captures" && mkdir -p "$P/Captures"
timeout 600 "$P/Build/BolzenaRunUI.exe" -demo -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -captures "$P/Captures" -save demo_pc.json -logFile "$P/player.log" &
A=$!
if [ "$2" != "nophone" ]; then
  timeout 600 "$P/Build/BolzenaRunUI.exe" -demo -phone -screen-width 844 -screen-height 390 -screen-fullscreen 0 -captures "$P/Captures" -save demo_phone.json -logFile "$P/player_phone.log" &
  B=$!
fi
wait $A; [ -n "$B" ] && wait $B
grep -E "\[Demo\]|Exception|error" "$P/player.log" | head -60
