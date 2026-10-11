// 계기별 소비자(그 계기로 도는 규칙을 가진 사도) 집계 — node _gen/deep/trig_count.mjs [데이터 폴더]
import fs from 'fs';
const DIR = process.argv[2] || 'C:/projects/bolzena-content-v2';
const cons = {}, fxk = {};
const walk = (fx, cb) => (fx || []).forEach(f => { cb(f); walk(f.then, cb); walk(f.else, cb); walk(f.fx, cb); });
for (const r of fs.readdirSync(`${DIR}/heroes`)) for (const f of fs.readdirSync(`${DIR}/heroes/${r}`)) {
  const h = JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, 'utf8')).heroes[0]; if (h.star < 3 && h.id !== '마리' && h.id !== '쵸피') continue;
  const rules = [...(h.passives || []), ...[h.keyword, ...(h.keywords || [])].filter(Boolean).flatMap(k => k.rules || [])];
  for (const x of rules) { const w = x.when || {}; const key = w.on + (w.who ? '/' + w.who : '') + (w.type ? '/' + w.type : ''); (cons[key] ||= new Set()).add(h.id); }
  for (const c of JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, "utf8")).cards || []) for (const fx of [c.fx, ...(c.oracles || []).map(o => o.fx)]) walk(fx, q => { (fxk[q.k] ||= new Set()).add(h.id); });
}
const out = o => Object.entries(o).sort((a, b) => b[1].size - a[1].size).map(([k, v]) => `${k}\t${v.size}`).join('\n');
console.log('== 계기(소비 규칙) =='); console.log(out(cons)); console.log('== 카드 효과 =='); console.log(out(fxk));
