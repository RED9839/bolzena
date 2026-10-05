// 신탁 값어치 주의를 없앨 때까지: 주의난 신탁의 수치를 한 칸씩 올린다(bumps.json 에 기록 → gen 이 적용)
import fs from 'fs';
import { execSync } from 'child_process';
const BZ = '"C:/projects/bolzena-core/Tools~/Dev/bz.cmd"';
const bumps = fs.existsSync('bumps.json') ? JSON.parse(fs.readFileSync('bumps.json', 'utf8')) : {};
for (let it = 0; it < 12; it++) {
  execSync('node gen.mjs', { stdio: 'ignore' });
  let out = '';
  try { out = execSync(`${BZ} check --data C:/projects/bolzena-content-v2`, { encoding: 'utf8' }); } catch (e) { out = e.stdout; }
  const lines = out.split('\n').filter(l => l.includes('heroes/정령/'));
  const keys = new Set();
  for (const l of lines) { const m = l.match(/카드 (\S+) \(.*신탁(\d)/); if (m) keys.add(`${m[1]}|${m[2]}`); else console.log('기타:', l); }
  console.log(`회차 ${it}: 주의 ${lines.length}`);
  if (keys.size === 0) break;
  for (const k of keys) bumps[k] = (bumps[k] || 0) + 1;
  fs.writeFileSync('bumps.json', JSON.stringify(bumps, null, 1));
}
