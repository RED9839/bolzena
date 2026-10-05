// 검사기의 신탁 값어치 주의를 읽어 그 신탁의 수치를 올린다(마녀 폴더만)
import fs from 'fs';
import { execSync } from 'child_process';
const DIR = 'C:/projects/bolzena-content-v2/heroes/마녀';
const BZ = '"C:/projects/bolzena-core/Tools~/Dev/bz.cmd" check --data C:/projects/bolzena-content-v2';
const up5 = x => Math.ceil(x * 20 - 1e-9) / 20;

function run() { try { return execSync(BZ, { encoding: 'utf8', maxBuffer: 1 << 26 }); } catch (e) { return e.stdout || ''; } }

function scaleRatios(fx, f) {
  let n = 0;
  for (const x of fx) {
    if (['dmg', 'shield', 'heal', 'extra'].includes(x.k)) { x.ratio = up5(x.ratio * f); n++; }
    if (x.then) n += scaleRatios(x.then, f);
  }
  return n;
}
function bump(fx) {
  for (const x of fx) if (['status', 'stack'].includes(x.k) && x.v > 0) { x.v += 1; return true; }
  for (const x of fx) if (x.then && bump(x.then)) return true;
  for (const x of fx) if (x.k === 'draw') { x.v += 1; return true; }
  for (const x of fx) if (x.k === 'pull') { x.n = (x.n || 1) + 1; return true; }
  return false;
}

for (let it = 0; it < 8; it++) {
  const out = run();
  const lines = out.split('\n').filter(l => l.includes('heroes/마녀/'));
  const fix = new Map();   // "card|n" → factor
  for (const l of lines) {
    let m = l.match(/카드 (\S+) \(heroes\/마녀\/[^)]+\) 신탁(\d) 「[^」]*」: 기본보다 낫지 않다\(코스트 기준 ([\d.]+)배/);
    if (m) { const k = `${m[1]}|${m[2]}`; fix.set(k, Math.max(fix.get(k) || 1, 1.2 / Math.max(0.2, +m[3]))); continue; }
    m = l.match(/카드 (\S+) \(heroes\/마녀\/[^)]+\) 신탁(\d) 「[^」]*」: 코스트를 올렸으면 값어치가 기본의 1.6배 이상\(지금 ([\d.]+)배/);
    if (m) { const k = `${m[1]}|${m[2]}`; fix.set(k, Math.max(fix.get(k) || 1, 1.66 / +m[3])); }
  }
  console.log(`회차 ${it}: 마녀 줄 ${lines.length} · 신탁 고칠 것 ${fix.size}`);
  if (fix.size === 0) { lines.forEach(l => console.log(l)); break; }
  for (const f of fs.readdirSync(DIR)) {
    const p = `${DIR}/${f}`; const j = JSON.parse(fs.readFileSync(p, 'utf8')); let ch = false;
    for (const c of j.cards) for (let n = 1; n <= (c.oracles || []).length; n++) {
      const k = `${c.id}|${n}`; if (!fix.has(k)) continue;
      const o = c.oracles[n - 1]; const fac = fix.get(k);
      if (scaleRatios(o.fx, fac) === 0 || fac > 1.6) { if (!bump(o.fx)) console.log('못 올림', k); }
      ch = true;
    }
    if (ch) fs.writeFileSync(p, JSON.stringify(j, null, 2) + '\n', 'utf8');
  }
}
