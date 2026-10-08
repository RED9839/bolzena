#!/usr/bin/env bash
# 문체 · TextCheck 를 잠금 안에서 돌린다(두 도구가 %TEMP% 의 같은 파일을 써서 동시에 돌리면 섞인다).
# bash textcheck18.sh <묶음>   → 출력: _gen/rework/text_<묶음>_18.txt (전체 결과. 자기 사도 줄만 grep 해서 볼 것)
L=/c/projects/_locks/textcheck.lock
until mkdir "$L" 2>/dev/null; do sleep 5; done
echo "$1 $(date +%T) pid $$" > "$L/who"
trap 'rm -rf "$L"' EXIT
OUT="/c/projects/bolzena-content-v2/_gen/rework/text_${1:-x}_18.txt"
{ echo '== style --stage2 --only 데이터'; cmd //c "C:\projects\bolzena-core\Tools~\TextCheck\style.cmd --stage2 --only 데이터" 2>&1
  echo '== TextCheck run'; cmd //c "C:\projects\bolzena-core\Tools~\TextCheck\run.cmd" 2>&1; } > "$OUT"
tail -5 "$OUT"
