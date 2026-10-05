#!/bin/bash
# usage: simall.sh N outdir
N=$1; OUT=$2; mkdir -p $OUT
DATA="C:/Users/User/AppData/Local/Temp/claude/c--projects-------/d694ae17-4ef2-4221-ba37-e5e131b82ad3/scratchpad/h2/chk"
ls chk/heroes/수인 | sed 's/.json$//' | xargs -P 6 -I{} sh -c "\"C:/projects/bolzena-core/Tools~/Dev/bz.cmd\" sim $N --with {} --data \"$DATA\" > $OUT/{}.txt 2>&1"
for f in $OUT/*.txt; do k=$(basename $f .txt); r=$(head -1 $f | grep -o '전체 완주 [0-9.]*%'); echo "$k $r"; done | sort -k4 -t' ' -n
