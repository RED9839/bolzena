if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 마지막 신탁 값어치 맞춤(2026-10-10 — 역할 계수 · 스탯 배수 뒤) — bz check 의 「기본보다 낫지 않다」 · 「1.6배」 주의를 그 신탁의 수치로 바로 고친다
// node _gen/deep/valfix.mjs <데이터 폴더> [바퀴 6]   주의가 없으면 아무것도 안 바꾼다(몇 번 돌려도 같다)
import fs from 'fs'; import { execSync } from 'child_process'; import { stepUp, stepDown } from './round10.mjs';
const BZ = 'C:/projects/bolzena-core/Tools~/Dev/bz.cmd';
const [dir, rounds = 6] = process.argv.slice(2); if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
const NUM = new Set(['dmg', 'heal', 'shield', 'extra', 'empower', 'dealtMod', 'drain']);
const walk = (fx, cb) => (fx || []).forEach(f => { cb(f); walk(f.then, cb); for (const r of f.rules || []) walk(r.fx, cb); });
for (let i = 1; i <= +rounds; i++) {
  let out = ''; try { out = execSync(`"${BZ}" --data "${dir}" check`, { encoding: 'utf8', maxBuffer: 1e8 }); } catch (e) { out = e.stdout; }
  const todo = [];
  for (const l of out.split(/\r?\n/)) {
    let m = l.match(/카드 (\S+) \((heroes\/[^)]*)\) 신탁(\d) 「[^」]*」: 기본보다 낫지 않다\(코스트 기준 (-?[\d.]+)배/);
    if (m) { todo.push({ id: m[1], p: m[2], n: +m[3], f: Math.min(1.6, Math.max(1.04, 1.2 / Math.max(0.3, +m[4]))) }); continue; }
    m = l.match(/카드 (\S+) \((heroes\/[^)]*)\) 신탁(\d) 「[^」]*」: 코스트를 올렸으면 값어치가 기본의 1.6배 이상\(지금 ([\d.]+)배/);
    if (m) { todo.push({ id: m[1], p: m[2], n: +m[3], f: Math.min(1.6, Math.max(1.03, 1.65 / +m[4])) }); continue; }
    // 변신판(변신이 바꿔 넣은 카드) — 1.8배를 넘으면 내리고, 1.15배가 안 되면 올린다
    m = l.match(/사도 \S+ \((heroes\/[^)]*)\) 변신 \S+: 변신판 「([^」]*)」 이 「[^」]*」 의 ([\d.]+)배 — 1.8배 이하로/);
    if (m) { todo.push({ p: m[1], name: m[2], f: 1.7 / +m[3], down: true }); continue; }
    m = l.match(/사도 \S+ \((heroes\/[^)]*)\) 변신 \S+: 변신판 「([^」]*)」 이 「[^」]*」 보다 낫지 않다\(코스트 기준 ([\d.]+)배/);
    if (m) todo.push({ p: m[1], name: m[2], f: Math.min(1.6, Math.max(1.03, 1.2 / +m[3])) });
  }
  if (!todo.length) { console.log('값어치 맞춤 — 남은 주의 0'); break; }
  const files = {};
  for (const t of todo) {
    const fp = `${dir}/${t.p}`; const j = files[fp] ||= JSON.parse(fs.readFileSync(fp, 'utf8'));
    if (t.name) {   // 변신판 카드 자체
      const c = j.cards.find(c => c.name === t.name && c.token) || j.cards.find(c => c.name === t.name); if (!c) continue;
      walk(c.fx, q => { if (NUM.has(q.k) && q.ratio) q.ratio = t.down ? Math.max(0.05, stepDown(q.ratio * t.f)) : stepUp(q.ratio * t.f); });
      continue;
    }
    const o = j.cards.find(c => c.id === t.id).oracles[t.n - 1]; let n = 0;
    walk(o.fx, q => { if (NUM.has(q.k) && q.ratio) { q.ratio = stepUp(q.ratio * t.f); n++; } });   // 10% 칸 위로(round10)
    if (!n) { const st = o.fx.find(q => q.k === 'stack' || q.k === 'draw' || q.k === 'make' || q.k === 'status'); if (st) st.v = (st.v || 1) + 1; else if (o.fx.length < 3) o.fx.push({ k: 'draw', v: 1 }); }
  }
  for (const [fp, j] of Object.entries(files)) { fs.writeFileSync(fp + '.tmp', JSON.stringify(j, null, 2) + '\n'); fs.renameSync(fp + '.tmp', fp); }
  console.log('바퀴', i, '고친 신탁', todo.length);
}
