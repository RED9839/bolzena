#!/usr/bin/env bash
# 마지막 손질 묶음 — 묶음 스크립트 다시 생성 → 대상 폴더(원본 또는 시험 데이터)의 heroes 에 반영
# bash _gen/deep/mkfinal.sh <대상 데이터 폴더>   (원본에 쓸 땐 C:/projects/bolzena-content-v2)
# 에르핀 · 캬롯 · 셰이디 · 시저는 원본 파일을 그대로 두고(boost 손실) 패시브(fix17) · 구조 손질(시저)만 그 파일에.
set -e
cd /c/projects/bolzena-content-v2/_gen/deep
DST=$1
for b in b0 b1 b2 b3 b4 b5 b6 b7 b8; do node $b.mjs >/dev/null; done
n=0
for f in $(cd heroes && find . -name "*.json"); do
  case "$f" in *에르핀.json|*캬롯.json|*셰이디.json|*시저.json) continue;; esac
  cmp -s "heroes/$f" "$DST/heroes/$f" || { cp "heroes/$f" "$DST/heroes/$f.tmp" && mv "$DST/heroes/$f.tmp" "$DST/heroes/$f"; n=$((n+1)); }
done
node -e "
const fs=require('fs');const o=JSON.parse(fs.readFileSync('passives_fix17.json'));
for(const k of ['요정/에르핀','요정/캬롯']){const p='$DST/heroes/'+k+'.json';const j=JSON.parse(fs.readFileSync(p));j.heroes[0].passives=o[k];fs.writeFileSync(p+'.tmp',JSON.stringify(j,null,2)+'\n');fs.renameSync(p+'.tmp',p)}"
echo "반영 $n 파일"
