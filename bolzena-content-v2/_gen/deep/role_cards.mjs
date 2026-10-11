import fs from 'fs';
const [DIR, role] = process.argv.slice(2);
const walk = (fx, cb) => (fx || []).forEach(f => { cb(f); walk(f.then, cb); for (const r of f.rules || []) walk(r.fx, cb); });
for (const r of fs.readdirSync(`${DIR}/heroes`)) for (const f of fs.readdirSync(`${DIR}/heroes/${r}`)) {
  const j = JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, 'utf8')); const h = j.heroes[0]; if (h.role !== role || (h.star < 3 && h.id !== '마리')) continue;
  const u = j.cards.filter(c => c.hero === h.id && c.unique);
  let sh2 = 0, maxSh = 0, ctr = 0, def = 0, blk = 0;
  for (const c of u) walk(c.fx, q => { if (q.k === 'shield') { maxSh = Math.max(maxSh, q.ratio); if (c.cost >= 2) sh2 = Math.max(sh2, q.ratio); } if (q.k === 'status' && q.id === '반격') ctr++; if (q.base === 'def') def++; if (/perGuarded|perBlocked/.test(q.k)) blk++; });
  const rules = [...(h.passives || []), ...[h.keyword, ...(h.keywords || [])].filter(Boolean).flatMap(k => k.rules || [])];
  const rb = rules.filter(x => x.when && (x.when.on === 'blocked' || (x.when.on === 'hurt' && x.when.guarded))).length;
  const basic = j.cards.filter(c => c.hero === h.id && !c.unique && !c.token).map(c => c.fx.filter(q => q.k === 'shield' || q.k === 'heal').map(q => q.k[0] + q.ratio).join('')).join(' ');
  console.log(`${h.id}\t2코실드 ${sh2}\t최대 ${maxSh}\t반격 ${ctr}\tdef피해 ${def}\t막은양 ${blk}+${rb}\t기본 ${basic}`);
}
