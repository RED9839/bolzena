if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 카드 % 를 보기 좋게(2026-10-10 사용자) — 사도 카드 · 신탁 · 축복 · 고유 능력 · 고학년 · 생성 카드 · 변신의 % 값을 10% 단위로(20% 미만은 5% 단위, 0 이 되지 않게).
// 스탯은 역할 × 등급 표 하나(role_stats.json), 사도별 세기는 고유 카드 수치(tune.json) — 그 결과를 10% 칸으로. 데이터 단계 맨 끝에 둔다(몇 번 돌려도 같다).
// node _gen/deep/round10.mjs <데이터 폴더>
import fs from 'fs';
export const step = x => { if (typeof x !== 'number' || Number.isInteger(x)) return x; const a = Math.abs(x), s = Math.sign(x); const r = a >= 0.2 ? Math.round(a * 10) / 10 : Math.max(0.05, Math.round(a * 20) / 20); return Math.round(s * r * 100) / 100; };
/** 늘릴 때(값어치 맞춤) — 그 수 이상인 가장 가까운 칸. */
export const stepUp = x => { const a = Math.abs(x), s = Math.sign(x) || 1; const u = a >= 0.2 ? 0.1 : 0.05; return Math.round(s * Math.max(0.05, Math.ceil(a / u - 1e-9) * u) * 100) / 100; };
/** 내릴 때 — 그 수 이하인 가장 가까운 칸. */
export const stepDown = x => { const a = Math.abs(x), s = Math.sign(x) || 1; const u = a >= 0.2 ? 0.1 : 0.05; return Math.round(s * Math.max(0.05, Math.floor(a / u + 1e-9) * u) * 100) / 100; };
const V_PCT = new Set(['dealtMod', 'atkMod', 'defMod', 'critMod', 'takenMod', 'healMod', 'payHpPct', 'ripen', 'cutHit']);
const PCT_KEYS = new Set(['ratio', 'pct', 'dealt', 'taken', 'atk', 'cut']);
function walk(o, parentKey) {
  if (Array.isArray(o)) { for (const x of o) walk(x, parentKey); return; }
  if (!o || typeof o !== 'object') return;
  for (const [k, v] of Object.entries(o)) {
    if (typeof v === 'number') {
      if (PCT_KEYS.has(k) && parentKey !== 'heroes' && !(k === 'atk' && parentKey !== 'mods')) o[k] = step(v);
      else if (k === 'v' && (V_PCT.has(o.k) || parentKey === 'per')) o[k] = step(v);
    } else walk(v, k);
  }
}
/** 반올림으로 같은 글이 된 신탁 — 갈래를 달리한다(fb.mjs dedupe 와 같은 길, 수치는 칸에 맞춰). */
function unDup(j) {
  for (const c of j.cards.filter(c => c.unique)) {
    const seen = [];
    for (const o of c.oracles || []) {
      const key = () => JSON.stringify([o.fx, o.cost ?? null, !!o.power]);
      if (seen.includes(key())) {
        const d = o.fx.find(f => f.k === 'dmg' && f.target === 'oneEnemy');
        if (d) { d.target = 'allEnemies'; d.ratio = step(d.ratio * 0.6); }
        else if (o.fx.filter(f => !/^(if|per|when)/.test(f.k)).length < 3) o.fx.push({ k: 'draw', v: 1 });
        else { const s = o.fx.find(f => f.k === 'stack' || f.k === 'draw'); if (s) s.v = (s.v || 1) + 1; else { const r = o.fx.find(f => typeof f.ratio === 'number'); if (r) r.ratio = stepUp(r.ratio * 1.1); } }
      }
      seen.push(key());
    }
  }
}
/** 비용만 다르고 효과가 같은 신탁(2026-10-11 사용자 — 뮤트) — 비용이 높은 쪽(신탁)의 수치를 비용 +1 몫(×1.6)만큼 올린다. 기본보다 싼 같은 효과(비용 −1 신탁)는 그대로. */
function costDup(j) {
  for (const c of j.cards.filter(c => c.unique)) {
    const vs = [{ cost: c.cost, o: null, fx: c.fx }, ...(c.oracles || []).map(o => ({ cost: o.cost ?? c.cost, o, fx: o.fx }))];
    for (let a = 0; a < vs.length; a++) for (let b = 0; b < vs.length; b++) {
      const lo = vs[a], hi = vs[b];
      if (!hi.o || hi.cost <= lo.cost || JSON.stringify(lo.fx) !== JSON.stringify(hi.fx)) continue;
      let n = 0; const bump = fx => (fx || []).forEach(q => { if (typeof q.ratio === 'number' && q.ratio > 0) { q.ratio = stepUp(q.ratio * 1.6); n++; } bump(q.then); for (const r of q.rules || []) bump(r.fx); });
      bump(hi.o.fx);
      if (!n) { const s = hi.o.fx.find(q => q.k === 'stack' || q.k === 'status' || q.k === 'draw'); if (s) s.v = (s.v || 1) + 2; }
      hi.fx = hi.o.fx;
    }
  }
}
export function round10(j) { walk(j, null); unDup(j); costDup(j); }
if (process.argv[1] && process.argv[1].endsWith('round10.mjs')) {
  const dir = process.argv[2]; if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
  let n = 0;
  for (const r of fs.readdirSync(`${dir}/heroes`)) for (const f of fs.readdirSync(`${dir}/heroes/${r}`)) {
    if (!f.endsWith('.json')) continue;
    const p = `${dir}/heroes/${r}/${f}`; const s0 = fs.readFileSync(p, 'utf8'); const j = JSON.parse(s0); round10(j);
    const s1 = JSON.stringify(j, null, 2) + '\n'; if (s1 !== s0) { fs.writeFileSync(p + '.tmp', s1); fs.renameSync(p + '.tmp', p); n++; }
  }
  console.log('10% 단위 반올림', n, '파일');
}
