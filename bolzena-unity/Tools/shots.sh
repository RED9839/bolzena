#!/bin/sh
# 화면 크기 · 비율별 전투 시범 캡처를 한꺼번에(나란히) — Captures/<이름>/ 에 남긴다. 진짜 마우스는 건드리지 않는다(가짜 손가락).
#   ./Tools/shots.sh                 기본 묶음(pc · phone · fhd · wqhd · uhd · uw · uwqhd(3440×1440) · w1610 · status · status_phone)
#   ./Tools/shots.sh pc phone        고른 것만
# 4K · WQHD 는 창이 모니터보다 커서 1920×1080 · 1280×720 창을 -supersize 2 로 다시 그려 찍는다(글 · 그림 선명도 확인용).
# 지킴이: 하나당 330초, 끝나면 남은 Bolzena.exe 를 지운다.
cd /c/projects/bolzena-unity
LOGS=/c/projects/bolzena-unity-tmp
STATUS="-party 니콜,포셔,벨라 -foes fairymobcloserange_naive,fairymoblongrange_jolly"
all="pc phone fhd wqhd uhd uw uwqhd w1610 status status_phone"
[ $# -gt 0 ] && all="$*"
for n in $all; do
  case $n in
    pc) a="-screen-width 1600 -screen-height 900" ;;
    phone) a="-screen-width 844 -screen-height 390" ;;
    fhd) a="-screen-width 1920 -screen-height 1080" ;;
    wqhd) a="-screen-width 1280 -screen-height 720 -supersize 2" ;;
    uhd) a="-screen-width 1920 -screen-height 1080 -supersize 2" ;;
    uw) a="-screen-width 2560 -screen-height 1080" ;;
    uwqhd) a="-screen-width 1720 -screen-height 720 -supersize 2" ;;
    w1610) a="-screen-width 1920 -screen-height 1200" ;;
    status) a="-screen-width 1600 -screen-height 900 $STATUS" ;;
    status_phone) a="-screen-width 844 -screen-height 390 $STATUS" ;;
    bless) a="-screen-width 1600 -screen-height 900 -blesstest" ;;
    v2foes) a="-screen-width 1600 -screen-height 900 -party 니콜,포셔,벨라 -foes hatchling_cool,golem_cool_elite,magicfork_mad" ;;
    v2foes_phone) a="-screen-width 844 -screen-height 390 -party 니콜,포셔,벨라 -foes hatchling_cool,golem_cool_elite,magicfork_mad" ;;
    v2summon) a="-screen-width 1600 -screen-height 900 -party 리코타,캬롯,시온더다크불릿 -foes clone_canna" ;;
    v2boss) a="-screen-width 1600 -screen-height 900 -party 리코타,캬롯,시온더다크불릿 -foes clone_rude" ;;
    *) echo "모름: $n"; continue ;;
  esac
  rm -rf "Captures/$n"; mkdir -p "Captures/$n"
  ( timeout -k 10 330 ./Build/Bolzena.exe -demo -battle -captures "C:\\projects\\bolzena-unity\\Captures\\$n" $a -screen-fullscreen 0 -logFile "C:\\projects\\bolzena-unity-tmp\\shots_$n.log"; echo "$n exit=$?" ) &
  sleep 2
done
wait
taskkill //F //IM Bolzena.exe >/dev/null 2>&1 && echo "남은 Bolzena.exe 를 지움"
for n in $all; do
  echo "== $n: $(ls Captures/$n/*.png 2>/dev/null | wc -l)장  $(grep -c -E 'Exception' $LOGS/shots_$n.log) 예외"
  grep -E "Exception|\[Demo\] (진행|시간)" $LOGS/shots_$n.log | head -5
done
