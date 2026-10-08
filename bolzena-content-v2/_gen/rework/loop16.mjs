// 시범16 신탁 값어치 맞춤 — 주의가 없어질 때까지 그 신탁의 피해 · 실드 · 회복만 조금씩(boost_시범16.json). 「기본보다 낫지 않다」 · 「코스트를 올렸으면 1.6배」 둘 다
import fs from 'fs'; import { execSync } from 'child_process';
const BZ = 'C:/projects/bolzena-core/Tools~/Dev/bz.cmd', DATA = process.env.BZDATA || 'C:/projects/bolzena-content-v2';
const RE = /heroes\/[^/]+\/(리코타|티그|디아나_왕년|죠안|캬롯|마카샤|벨벳|샤샤|이프리트|니콜|쵸피|레비_졸업|쥬비|모모|힐데|큐이|코미)\.json/;
const BF = new URL('./boost_시범16.json', import.meta.url);
for (let i = 1; i <= 10; i++) {
  execSync('node 시범16.mjs', { cwd: new URL('.', import.meta.url) });
  let out = ''; try { out = execSync(`"${BZ}" check --data "${DATA}"`, { encoding: 'utf8', maxBuffer: 1e8 }); } catch (e) { out = e.stdout; }
  fs.writeFileSync(new URL('./chk_시범16.txt', import.meta.url), out);
  const B = fs.existsSync(BF) ? JSON.parse(fs.readFileSync(BF, 'utf8')) : {};
  let n = 0;
  for (const l of out.split(/\r?\n/)) {
    if (!RE.test(l)) continue;
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
