#!/bin/sh
# 짝 완주율 — 짝 둘 + 셋째(유령 아닌 무작위 사도 고정 목록 순환) 로 run 을 씨앗 N 개
G="C:/Users/User/AppData/Local/Temp/claude/c--projects-------/d694ae17-4ef2-4221-ba37-e5e131b82ad3/scratchpad/ghost"
F="$G/full"; O="$G/pairruns"; mkdir -p "$O"
N=${1:-30}
THIRD="비비 다야 리츠 에르핀 쥬비 리코타 버터 코미 이드 엘레나 우로스 마요 시저 나이아 아멜리아 실피르 칸나 네르 티그 캬롯"
jobs=""
for pair in 스피키,사리 바롱,셰이디 앨리스,셰이디 림,림_혼돈 메죵,바롱 베루,메죵 셰이디_역전,셰이디 키샤,사리 레테,시온더다크불릿 벨라,앨리스 셀리네,베루 스피키_메이드,셰이디_역전 에스피,키샤 시온더다크불릿,스피키; do
  i=0
  for t in $THIRD; do
    [ $i -ge $N ] && break
    for s in 1 2; do echo "$pair,$t $((i*7+s))"; done
    i=$((i+2))
  done
done | xargs -P 8 -L 1 sh -c 'r=$("/c/projects/bolzena-core/Tools~/Dev/bz.cmd" run $0 $1 --data "'"$F"'" | tail -1); echo "$0|$r"' > "$O/all.txt"
