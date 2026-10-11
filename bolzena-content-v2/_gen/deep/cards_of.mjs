import fs from 'fs';
const [DIR, ...names] = process.argv.slice(2);
const s = fx => (fx || []).map(q => q.k + (q.id ? ':' + q.id : '') + (q.v != null ? q.v : '') + (q.ratio ? q.ratio : '') + (q.then ? '{' + s(q.then) + '}' : '')).join(',');
for (const r of fs.readdirSync(`${DIR}/heroes`)) for (const f of fs.readdirSync(`${DIR}/heroes/${r}`)) {
  const id = f.replace('.json', ''); if (!names.includes(id)) continue;
  const j = JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, 'utf8'));
  console.log(`== ${id}`); for (const c of j.cards) console.log(`  ${c.id} [${c.cost}] ${c.type} ${(c.tags || []).join('/')} : ${s(c.fx)}${c.oracles ? ' | ' + c.oracles.map((o, i) => (i + 1) + ')' + s(o.fx)).join(' ') : ''}`);
}
