// 118명 신탁 값어치 맞춤 — node loop18.mjs <묶음>   (예: 요정A → 요정A_18.mjs · boost_요정A_18.json · chk_요정A_18.txt)
// 스크립트를 돌리고 bz check 의 「기본보다 낫지 않다」 · 「코스트를 올렸으면 1.6배」 주의가 없어질 때까지 그 신탁의 피해 · 실드 · 회복만 조금씩.
// 대상은 그 스크립트 JOBS 의 사도만(소스에서 '종족/파일' 을 읽음). 다른 묶음 에이전트와 동시에 돌려도 된다.
import fs from 'fs'; import { execSync } from 'child_process';
const BZ = 'C:/projects/bolzena-core/Tools~/Dev/bz.cmd', DATA = process.env.BZDATA || 'C:/projects/bolzena-content-v2';
const key = process.argv[2];
if (!key) { console.error('묶음 이름을 주시오(예: 요정A)'); process.exit(1); }
const SCRIPT = new URL(`./${key}_18.mjs`, import.meta.url);
const jobs = [...fs.readFileSync(SCRIPT, 'utf8').matchAll(/\[\s*'([^'/]+\/[^']+)'\s*,/g)].map(m => m[1]);
if (!jobs.length) { console.error('JOBS 를 못 읽음'); process.exit(1); }
const mine = l => jobs.some(f => l.includes(`heroes/${f}.json`));
const BF = new URL(`./boost_${key}_18.json`, import.meta.url);
for (let i = 1; i <= 10; i++) {
  execSync(`node ${key}_18.mjs`, { cwd: new URL('.', import.meta.url) });
  let out = ''; try { out = execSync(`"${BZ}" check --data "${DATA}"`, { encoding: 'utf8', maxBuffer: 1e8 }); } catch (e) { out = e.stdout; }
  const lines = out.split(/\r?\n/);
  fs.writeFileSync(new URL(`./chk_${key}_18.txt`, import.meta.url), lines.filter(l => mine(l) || /^(검사|오류)/.test(l)).join('\n'));
  const B = fs.existsSync(BF) ? JSON.parse(fs.readFileSync(BF, 'utf8')) : {};
  let n = 0;
  for (const l of lines) {
    if (!mine(l)) continue;
    let m = l.match(/카드 (\S+) \([^)]*\) 신탁(\d) 「[^」]*」: 기본보다 낫지 않다\(코스트 기준 (-?[\d.]+)배/), f = 0;
    if (m) f = Math.min(1.25, Math.max(1.04, 1.18 / Math.max(0.3, +m[3])));
    else if ((m = l.match(/카드 (\S+) \([^)]*\) 신탁(\d) 「[^」]*」: 코스트를 올렸으면 값어치가 기본의 1.6배 이상\(지금 ([\d.]+)배/))) f = Math.min(1.25, Math.max(1.03, 1.63 / +m[3]));
    if (!f) continue;
    const k = m[1] + '|' + m[2]; B[k] = Math.round((B[k] || 1) * f * 100) / 100; n++;
  }
  fs.writeFileSync(BF, JSON.stringify(B, null, 1));
  console.log('바퀴', i, '고칠 신탁', n);
  if (!n) break;
}
const B = JSON.parse(fs.readFileSync(BF, 'utf8'));
console.log('1.4배 넘는 보정:', Object.entries(B).filter(([, v]) => v > 1.4).map(x => x.join('=')).join('  ') || '없음');
