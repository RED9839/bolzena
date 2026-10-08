// 생성 카드 사도 비율(03_재설계_검토 B-7 정의: 20% = 27명) — 장치(패시브 · 고유 효과 규칙 · onMax · 변신 패시브)가 자기 토큰 카드를 만드는 사도.
// 넓은 셈(고유 카드 본문까지)도 같이 낸다. 신탁 · 축복은 빼고. node gen_ratio.mjs [데이터 폴더] [--list]
import fs from 'fs'; import path from 'path';
const arg = process.argv.slice(2).find(a => !a.startsWith('--'));
const ROOT = path.join(arg || 'C:/projects/bolzena-content-v2', 'heroes');
let n = 0, dev = 0, wide = 0; const yes = [], no = [];
for (const d of fs.readdirSync(ROOT)) for (const f of fs.readdirSync(path.join(ROOT, d))) {
  const j = JSON.parse(fs.readFileSync(path.join(ROOT, d, f), 'utf8')); const h = j.heroes[0]; n++;
  const toks = j.cards.filter(c => c.token && !/_f\d+$/.test(c.id)).map(c => c.id);
  const mk = x => { const s = JSON.stringify(x ?? null); return toks.some(t => s.includes(`"${t}"`)); };
  const devHit = mk(h.passives) || mk([h.keyword, ...(h.keywords || [])]) || mk((h.forms || []).map(fm => fm.passives));
  if (devHit) { dev++; yes.push(h.name); } else no.push(h.name);
  if (devHit || mk(j.cards.filter(c => c.unique).map(c => c.fx))) wide++;
}
console.log(`생성 카드 사도(장치가 만듦) ${dev}/${n} (${(100 * dev / n).toFixed(1)}%) · 넓게(고유 카드 본문 포함) ${wide}/${n} (${(100 * wide / n).toFixed(1)}%)`);
if (process.argv.includes('--list')) console.log('만듦:', yes.join(' · '), '\n안 만듦:', no.join(' · '));
