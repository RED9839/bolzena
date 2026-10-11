// 고유 능력(규칙 · 패시브)의 자동 피해(dmg · extra · summon) 세기 — node _gen/deep/autodmg_count.mjs [데이터 폴더]
import fs from 'fs';
const DIR = process.argv[2] || 'C:/projects/bolzena-content-v2';
const hit = fx => (fx || []).some(f => ['dmg', 'extra', 'summon', 'reflect'].includes(f.k) || (f.then && hit(f.then)));
let tot = 0; const per = []; const multi = [];
for (const r of fs.readdirSync(`${DIR}/heroes`)) for (const f of fs.readdirSync(`${DIR}/heroes/${r}`)) {
  const h = JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, 'utf8')).heroes[0]; if (h.star < 3 && h.id !== '마리' && h.id !== '쵸피') continue;
  const rules = [...(h.passives || []), ...[h.keyword, ...(h.keywords || [])].filter(Boolean).flatMap(k => k.rules || [])].filter(x => hit(x.fx));
  tot += rules.length; per.push(rules.length); if (rules.length > 1) multi.push(`${h.id}:${rules.map(x => x.name).join('/')}`);
}
console.log(`자동 피해 규칙 ${tot}개 · 둘 넘는 사도 ${multi.length}`); console.log(multi.join('\n'));
