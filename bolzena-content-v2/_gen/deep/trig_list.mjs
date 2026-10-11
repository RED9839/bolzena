import fs from 'fs';
const DIR = process.argv[2]; const only = process.argv[3];
for (const r of fs.readdirSync(`${DIR}/heroes`)) for (const f of fs.readdirSync(`${DIR}/heroes/${r}`)) {
  const j = JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, 'utf8')); const h = j.heroes[0]; if (h.star < 3 && h.id !== '마리' && h.id !== '쵸피') continue;
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const x of k.rules || []) {
    const w = x.when || {}; const key = [w.on, w.who, w.type, w.minCost && 'c' + w.minCost, w.every && 'e' + w.every, w.mine && 'mine'].filter(Boolean).join('/');
    if (only && !new RegExp(only).test(key)) continue;
    console.log(`${r}/${h.id}\t${h.role}\t${k.name}\t${x.name || ''}\t${key}\t${x.fx.map(q => q.k + (q.id ? ':' + q.id : '') + (q.v ? q.v : '') + (q.ratio ? q.ratio : '')).join(',')}`);
  }
}
