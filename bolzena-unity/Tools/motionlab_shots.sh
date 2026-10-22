#!/bin/sh
# 모션 확인 화면 캡처(2026-10-13) — 사도마다 평타 · 강공 · 스킬 · 추가 공격(탄환) · 고학년을 실제 전투 길로 틀고 PNG 로 남긴다.
#   ./Tools/motionlab_shots.sh                         엘다인 20명 전부(모션 확인 화면의 사도 목록) → Captures/motionlab/
#   ./Tools/motionlab_shots.sh 티그_영웅,아야            고른 사도만(코어 id, 쉼표)
#   ./Tools/motionlab_shots.sh -ult [티그_영웅,아야]     고학년만
# 찍는 것: <번호>_<사도>_<1평타|2강공|3스킬|4추가탄환>_<a|b|c>.png — 첫 타격 순간 · +0.15초 · +0.4초(타격이 없으면 _x 한 장)
#          <번호>_<사도>_5고학년_<nn>.png + .tsv — 적 전체 · 컷인 포함, 몸짓 시작부터 0.25초마다(16장까지) + 타격 순간마다
#   _done.txt — 동작마다 몸짓 · 길이 · 이펙트 계획 점검 글. 소리 0. 다 찍으면 스스로 끈다(지킴이 45분 + 여기서 3000초)
#   끝나면 Tools/motionlab_sheet.py 로 고학년 시트(Captures/motionlab/_sheets/)를 붙인다
# 빌드(Build/Bolzena.exe)가 있어야 한다 — 이 스크립트는 빌드하지 않는다
cd /c/projects/bolzena-unity
OUT=Captures/motionlab
ult=""
seq=""
[ "$1" = "-seq" ] && { seq="-motionlab-seq"; shift; }
[ "$1" = "-video" ] && { seq="-motionlab-seq -motionlab-seqfps 30"; video=1; shift; }   # (10-15) 매 프레임 → Tools/vs_video.py 로 원작 · 우리 나란히 mp4   # (2026-10-15) 0.1초 연속 그림 → python Tools/vs_orig.py Captures/motionlab 로 원작과 나란히
[ "$1" = "-ult" ] && { ult="-motionlab-ult"; shift; }
heroes=""
[ $# -gt 0 ] && heroes="-motionlab-heroes $1"
rm -rf "$OUT"; mkdir -p "$OUT"
timeout -k 10 3000 ./Build/Bolzena.exe -motionlab-shots "C:\\projects\\bolzena-unity\\Captures\\motionlab" $ult $seq $heroes \
  -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "C:\\projects\\bolzena-unity-tmp\\motionlab_shots.log"
echo "exit=$?"
: # (2026-10-15) 다른 Bolzena 창(사용자가 켜 둔 게임)까지 닫던 정리 줄을 뺐다 — 캡처는 스스로 끈다
echo "== $(ls $OUT/*.png 2>/dev/null | wc -l)장  $(grep -c -E 'Exception' /c/projects/bolzena-unity-tmp/motionlab_shots.log) 예외"
grep -E "Exception|\[MotionLab\]" /c/projects/bolzena-unity-tmp/motionlab_shots.log | head -8
python Tools/motionlab_sheet.py "C:/projects/bolzena-unity/$OUT"
if [ -n "$video" ]; then python Tools/vs_video.py "C:/projects/bolzena-unity/$OUT"; elif [ -n "$seq" ]; then python Tools/vs_orig.py "C:/projects/bolzena-unity/$OUT"; fi
