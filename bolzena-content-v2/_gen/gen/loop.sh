cd "$(dirname "$0")"
for i in 1 2 3 4 5 6 7 8; do
  node build.mjs >/dev/null || exit 1
  "C:/projects/bolzena-core/Tools~/Dev/bz.cmd" check --data C:/projects/bolzena-content-v2 > chk.txt 2>&1
  n=$(grep "heroes/엘프" chk.txt | grep -c "신탁")
  echo "round $i: $n"
  [ "$n" = 0 ] && break
  node -e "
const fs=require('fs');const b=fs.existsSync('boost.json')?JSON.parse(fs.readFileSync('boost.json','utf8')):{};
const seen=new Set();
for(const l of fs.readFileSync('chk.txt','utf8').split(/\r?\n/)){const m=l.match(/카드 (\S+) \(heroes\/엘프\/[^)]*\) 신탁(\d)/);if(!m)continue;const k=m[1]+'|'+m[2];if(seen.has(k))continue;seen.add(k);b[k]=Math.round((b[k]||1)*1.1*100)/100;}
fs.writeFileSync('boost.json',JSON.stringify(b,null,1));"
done
