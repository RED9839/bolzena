// 「스택을 전부 써서 1개당 n% 피해 · 회복 · 실드」 틀을 쓰는 리뉴얼 사도 세기 — node _gen/deep/spendall_count.mjs [데이터 폴더]
// 기준: 고유 카드(본판) · 패시브 · 고유 효과 규칙의 fx 에 perStack(id) 뒤 피해 · 회복 · 실드가 있고, 같은 id 를 전부 소모(spend all)하는 것.
import fs from 'fs';
const DIR = process.argv[2] || 'C:/projects/bolzena-content-v2';
const hit = fx => { const out = []; (fx || []).forEach((f, i) => { if (f.k === 'perStack' && (fx.slice(i + 1).some(g => ['dmg', 'heal', 'shield'].includes(g.k))) && fx.some(g => g.k === 'spend' && g.all && g.id === f.id)) out.push(f.id); }); return out; };
const res = [];
for (const r of fs.readdirSync(`${DIR}/heroes`)) for (const f of fs.readdirSync(`${DIR}/heroes/${r}`)) {
  const j = JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, 'utf8')); const h = j.heroes[0]; if (h.star < 3 && h.id !== '마리') continue;
  const where = [];
  for (const c of j.cards.filter(c => c.hero === h.id && c.unique)) if (hit(c.fx).length) where.push(`카드 ${c.name}`);
  for (const p of h.passives || []) if (hit(p.fx).length) where.push(`패시브 ${p.name}`);
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const p of k.rules || []) if (hit(p.fx).length) where.push(`규칙 ${p.name}`);
  if (where.length) res.push(`${r}/${h.id}: ${where.join(' · ')}`);
}
console.log(`전부 소모 틀 ${res.length}명`); console.log(res.join('\n'));
