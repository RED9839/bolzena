if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 묶음 신탁 값어치 맞춤 — node _gen/deep/loop.mjs <묶음 스크립트 이름(b0 …)> [바퀴]
// <묶음>.mjs → 임시 데이터 폴더(BZDATA, 그 묶음 사도만 덮어쓰기) → bz check → 「기본보다 낫지 않다」 · 「1.6배」 · 변신판 주의를 boost.json 으로
import fs from 'fs'; import { execSync } from 'child_process'; import * as L from './lib.mjs';
const BZ = 'C:/projects/bolzena-core/Tools~/Dev/bz.cmd';
const DATA = process.env.BZDATA || 'C:/Users/User/AppData/Local/Temp/claude/c--projects-------/d694ae17-4ef2-4221-ba37-e5e131b82ad3/scratchpad/deepdata';
const HERE = new URL('.', import.meta.url);
const key = process.argv[2]; if (!key) { console.error('묶음 이름(b0 …)'); process.exit(1); }
const FILES = [...fs.readFileSync(new URL(`./${key}.mjs`, HERE), 'utf8').matchAll(/\['([^'/]+\/[^']+)',\s*\w+\]/g)].map(m => m[1]);
const mine = l => FILES.some(f => l.includes(`heroes/${f}.json`));
const BF = new URL('./boost.json', import.meta.url);
const N = +(process.argv[3] || 8);
for (let i = 1; i <= N; i++) {
  execSync(`node ${key}.mjs`, { cwd: HERE });
  for (const f of FILES) fs.copyFileSync(new URL(`./heroes/${f}.json`, HERE), `${DATA}/heroes/${f}.json`);
  let out = ''; try { out = execSync(`"${BZ}" --data "${DATA}" check --quiet`, { encoding: 'utf8', maxBuffer: 1e8 }); } catch (e) { out = e.stdout; }
  const all = out.split(/\r?\n/), lines = all.filter(mine);
  fs.writeFileSync(new URL(`./chk_${key}.txt`, HERE), [all[0], ...all.filter(l => l.startsWith('오류')), ...lines].join('\n'));
  if (all.some(l => l.startsWith('오류')) || all[0].startsWith('읽기 실패')) { console.log('오류 있음 — chk 파일'); break; }
  const B = fs.existsSync(BF) ? JSON.parse(fs.readFileSync(BF, 'utf8')) : {};
  let n = 0;
  for (const l of lines) {
    let m = l.match(/카드 (\S+) \([^)]*\) 신탁(\d) 「[^」]*」: 기본보다 낫지 않다\(코스트 기준 (-?[\d.]+)배/), f = 0, k;
    if (m) { f = Math.min(1.3, Math.max(1.04, 1.18 / Math.max(0.3, +m[3]))); k = m[1] + '|' + m[2]; }
    else if ((m = l.match(/카드 (\S+) \([^)]*\) 신탁(\d) 「[^」]*」: 코스트를 올렸으면 값어치가 기본의 1.6배 이상\(지금 ([\d.]+)배/))) { f = Math.min(1.3, Math.max(1.03, 1.63 / +m[3])); k = m[1] + '|' + m[2]; }
    else if ((m = l.match(/변신판 「([^」]*)」 이 「[^」]*」 보다 낫지 않다\(코스트 기준 ([\d.]+)배/))) { f = Math.min(1.3, Math.max(1.04, 1.2 / +m[2])); k = 'FORM:' + m[1]; }
    else if ((m = l.match(/변신판 「([^」]*)」 이 「[^」]*」 의 ([\d.]+)배 — 1.8배 이하로/))) { f = Math.max(0.8, 1.7 / +m[2]); k = 'FORM:' + m[1]; }
    if (!f) continue;
    // 수치가 없거나 보정이 1.6 을 넘으면 배율 대신 쌓기 · 생성 수를 +1(최대 3)
    if (!k.startsWith('FORM:')) {
      const [cid, on] = k.split('|'); const hf = FILES.find(x => cid.startsWith(x.split('/')[1] + '_'));
      const card = hf && JSON.parse(fs.readFileSync(new URL(`./heroes/${hf}.json`, HERE), 'utf8')).cards.find(c => c.id === cid);
      const o = card && card.oracles[+on - 1];
      if (o && (!L.hasNum(o.fx) || (B[k] || 1) * f > 2.0)) { const kb = k + '|+'; if ((B[kb] || 0) < 2) { B[kb] = (B[kb] || 0) + 1; n++; } continue; }
    }
    B[k] = Math.round((B[k] || 1) * f * 100) / 100; n++;
  }
  fs.writeFileSync(BF, JSON.stringify(B, null, 1));
  console.log('바퀴', i, '고칠 것', n, '·', lines.length, '줄');
  if (!n) break;
}
const B = JSON.parse(fs.readFileSync(BF, 'utf8'));
const big = Object.entries(B).filter(([k, v]) => v > 1.5 && FILES.some(f => k.startsWith(f.split('/')[1] + '_') || k.startsWith('FORM:')));
console.log('1.5배 넘는 보정:', big.map(x => x.join('=')).join('  ') || '없음');
