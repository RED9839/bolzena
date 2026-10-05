// 신탁 값어치 주의(1.15배 · 1.6배)를 없앨 때까지 그 신탁의 가장 큰 수를 조금씩 올린다. 사용: node tune.js <폴더>
const fs = require('fs'), path = require('path'), cp = require('child_process');
const dir = process.argv[2];
const S = 'C:/Users/User/AppData/Local/Temp/claude/c--projects-------/d694ae17-4ef2-4221-ba37-e5e131b82ad3/scratchpad';
const files = fs.readdirSync(dir).filter(f => f.endsWith('.json')).map(f => path.join(dir, f));
const r05 = x => Math.round(x * 20) / 20;
for (let it = 0; it < 40; it++) {
  let out;
  try { out = cp.execFileSync(S + '/harness/out/Harness.exe', ['check', dir], { encoding: 'utf8' }); } catch (e) { out = e.stdout; }
  const ws = out.split('\n').filter(l => l.startsWith('주의') && /신탁(\d) 「/.test(l) && /(낫지 않다|1\.6배)/.test(l));
  if (!ws.length) { console.log(out.split('\n').slice(0, 30).join('\n')); break; }
  const todo = new Set(ws.map(l => { const m = l.match(/카드 (\S+) .*신탁(\d) 「/); return m[1] + '|' + m[2]; }));
  for (const f of files) {
    const j = JSON.parse(fs.readFileSync(f, 'utf8')); let ch = false;
    for (const c of j.cards) for (let n = 1; n <= 5; n++) {
      if (!todo.has(c.id + '|' + n)) continue;
      const fx = c.oracles[n - 1].fx; ch = true;
      // 비례 바로 뒤 줄은 건너뛰고 가장 큰 ratio
      let best = -1;
      fx.forEach((x, i) => { if (x.ratio && !(i > 0 && (fx[i - 1].k === 'perStack' || fx[i - 1].k === 'perRhythm')) && (best < 0 || x.ratio * (x.hits || 1) > fx[best].ratio * (fx[best].hits || 1))) best = i; });
      if (best < 0) fx.forEach((x, i) => { if (best < 0 && x.ratio) best = i; });
      if (best >= 0) { const x = fx[best]; x.ratio = r05(x.ratio + Math.max(0.05, x.ratio * 0.05 / (x.hits || 1))); continue; }
      const s = fx.find(x => (x.k === 'stack' || x.k === 'status') && x.id !== '기절') || fx.find(x => x.k === 'draw');
      if (s) s.v += 1;
    }
    if (ch) fs.writeFileSync(f, JSON.stringify(j, null, 2) + '\n');
  }
}
