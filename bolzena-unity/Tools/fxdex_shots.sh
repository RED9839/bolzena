#!/bin/sh
# 장 도감 캡처(2026-10-15) — 사도마다 원작 장을 하나씩 시전자 발밑에 제 크기로 틀어 0.3초 · 0.9초 두 장(fxNN_<갈래>_a|b) → Tools/fxdex_sheet.py 로 사도별 시트(Captures/fxdex/_sheets)
#   ./Tools/motionlab_shots.sh                         엘다인 20명 전부(모션 확인 화면의 사도 목록) → Captures/motionlab/
#   ./Tools/motionlab_shots.sh 티그_영웅,아야            고른 사도만(코어 id, 쉼표)
#   ./Tools/motionlab_shots.sh -ult [티그_영웅,아야]     고학년만
# 찍는 것: <번호>_<사도>_<1평타|2강공|3스킬|4추가탄환>_<a|b|c>.png — 첫 타격 순간 · +0.15초 · +0.4초(타격이 없으면 _x 한 장)
#          <번호>_<사도>_5고학년_<nn>.png + .tsv — 적 전체 · 컷인 포함, 몸짓 시작부터 0.25초마다(16장까지) + 타격 순간마다
#   _done.txt — 동작마다 몸짓 · 길이 · 이펙트 계획 점검 글. 소리 0. 다 찍으면 스스로 끈다(지킴이 45분 + 여기서 3000초)
#   끝나면 Tools/motionlab_sheet.py 로 고학년 시트(Captures/motionlab/_sheets/)를 붙인다
# 빌드(Build/Bolzena.exe)가 있어야 한다 — 이 스크립트는 빌드하지 않는다
cd /c/projects/bolzena-unity
OUT=Captures/fxdex
ult=""
[ "$1" = "-ult" ] && { ult="-motionlab-ult"; shift; }
heroes=""
[ $# -gt 0 ] && heroes="-motionlab-heroes $1"
rm -rf "$OUT"; mkdir -p "$OUT"
timeout -k 10 3000 ./Build/Bolzena.exe -motionlab-fxdex -motionlab-shots "C:\\projects\\bolzena-unity\\Captures\\fxdex" $ult $heroes \
  -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile "C:\\projects\\bolzena-unity-tmp\\fxdex_shots.log"
echo "exit=$?"
echo "== $(ls $OUT/*.png 2>/dev/null | wc -l)장  $(grep -c -E 'Exception' /c/projects/bolzena-unity-tmp/fxdex_shots.log) 예외"
grep -E "Exception|\[MotionLab\]" /c/projects/bolzena-unity-tmp/fxdex_shots.log | head -8
python Tools/fxdex_sheet.py "C:/projects/bolzena-unity/$OUT"
