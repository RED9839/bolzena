#!/usr/bin/env bash
# 시험용 임시 데이터 폴더 — 원본 content-v2(heroes · world · _ref) 복사 + _gen/deep 의 시범 사도 · 마을로 덮어쓰기
# bash _gen/deep/mkdata.sh <폴더>   (없으면 scratchpad 기본)
set -e
SRC=/c/projects/bolzena-content-v2
DST=${1:-/c/Users/User/AppData/Local/Temp/claude/c--projects-------/d694ae17-4ef2-4221-ba37-e5e131b82ad3/scratchpad/deepdata}
rm -rf "$DST"; mkdir -p "$DST"
cp -r "$SRC/heroes" "$SRC/world" "$DST/"
[ -d "$SRC/_ref" ] && cp -r "$SRC/_ref" "$DST/"
if [ "$2" != "orig" ]; then
  cp -r "$SRC/_gen/deep/heroes/." "$DST/heroes/"
  [ -n "$(ls -A $SRC/_gen/deep/villages 2>/dev/null)" ] && cp -r "$SRC/_gen/deep/villages/." "$DST/world/villages/"
fi
echo "→ $DST"
