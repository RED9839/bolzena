# 신탁 값어치 주의가 없어질 때까지 그 신탁 숫자만 조금씩 올린다(최대 8바퀴)
# 쓰는 법: bash loop.sh <스크립트.mjs> <boost.json> "<사도 파일 정규식 — 예: 요정/>"
# 세계 데이터가 다른 담당 손질로 안 읽히면 BZDATA="heroes 경로;세계 사본" 을 준다
cd "$(dirname "$0")"
SCRIPT=${1:-rework.mjs}; BOOST=${2:-boost.json}; RE=${3:-"(용족/루드|요정/에르핀|마녀/비비_신성|정령/멜루나|수인/디아나|유령/키샤)\.json"}
CHK=chk_$(basename "$SCRIPT" .mjs).txt
for i in 1 2 3 4 5 6 7 8; do
  node "$SCRIPT" > /dev/null || exit 1
  "C:/projects/bolzena-core/Tools~/Dev/bz.cmd" check --data "${BZDATA:-C:/projects/bolzena-content-v2}" > "$CHK" 2>&1
  n=$(grep -E "$RE" "$CHK" | grep -c "기본보다 낫지 않다")
  echo "바퀴 $i: $n"
  [ "$n" = 0 ] && break
  node -e "
const fs=require('fs');const f='$BOOST';const b=fs.existsSync(f)?JSON.parse(fs.readFileSync(f,'utf8')):{};const re=new RegExp('$RE');
for(const l of fs.readFileSync('$CHK','utf8').split(/\r?\n/)){if(!re.test(l))continue;const m=l.match(/카드 (\S+) \([^)]*\) 신탁(\d) 「[^」]*」: 기본보다 낫지 않다\(코스트 기준 ([\d.]+)배/);if(!m)continue;const k=m[1]+'|'+m[2];const r=+m[3];b[k]=Math.round((b[k]||1)*Math.min(1.25,Math.max(1.04,1.17/r))*100)/100;}
fs.writeFileSync(f,JSON.stringify(b,null,1));"
done
grep -E "$RE|오류" "$CHK" | head -40; head -1 "$CHK"
node -e "const fs=require('fs');const b=fs.existsSync('$BOOST')?JSON.parse(fs.readFileSync('$BOOST','utf8')):{};const big=Object.entries(b).filter(([k,v])=>v>1.4);if(big.length)console.log('1.4배 넘는 보정(구조로 고칠 것):',big.map(x=>x.join('=')).join('  '))"
