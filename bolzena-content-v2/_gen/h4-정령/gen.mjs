import fs from 'fs';
const REWORKED = JSON.parse(fs.readFileSync('C:/projects/bolzena-content-v2/_gen/rework/reworked.json', 'utf8')); // 리워크한 사도는 _gen/rework/rework.mjs 가 쓴다 — 여기서 덮어쓰지 않는다
import { build, report } from './lib.mjs';
import { heroesA } from './heroes-a.mjs';
import { heroesB } from './heroes-b.mjs';
const out = 'C:/projects/bolzena-content-v2/heroes/정령';
fs.mkdirSync(out, { recursive: true });
const bumps = fs.existsSync('bumps.json') ? JSON.parse(fs.readFileSync('bumps.json', 'utf8')) : {};
const SC = new Set(['dmg', 'shield', 'heal', 'extra']);
const INC = new Set(['status', 'stack', 'draw', 'make']);
function bump(fx) {
  const sc = fx.filter(f => SC.has(f.k));
  if (sc.length) { for (const f of sc) f.ratio = Math.round(f.ratio * 1.1 * 100) / 100; return; }
  const f = fx.find(f => INC.has(f.k) && f.v > 0) || fx.find(f => f.k === 'later')?.then.find(f => f.k === 'ap');
  if (f) f.v += 1;
}
for (const h of [...heroesA, ...heroesB]) {
  const file = build(h, out);
  const d = JSON.parse(fs.readFileSync(file, 'utf8'));
  let ch = false;
  for (const c of d.cards) (c.oracles || []).forEach((o, i) => { const n = bumps[`${c.id}|${i + 1}`] || 0; for (let k = 0; k < n; k++) { bump(o.fx); ch = true; } });
  if (ch && !REWORKED.includes(h.hero.id)) fs.writeFileSync(file, JSON.stringify(d, null, 1) + '\n', 'utf8');
}
report();
