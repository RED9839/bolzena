// 사도 리워크 공용 부품(2026-10-07) — 종족별 스크립트(<종족>.mjs)가 import 한다.
// 원본은 백업에서 읽는다(몇 번 돌려도 같은 결과). 카드 id(<사도>_u1~u4)는 그대로.
import fs from 'fs';
export const SRC = 'C:/projects/_backup/hero_rework_20261007';
export const OUT = 'C:/projects/bolzena-content-v2/heroes';
// ── 조각 ──
export const E1 = 'oneEnemy', EA = 'allEnemies', ER = 'randomEnemy';
export const dmg = (r, t = E1, o = {}) => ({ k: 'dmg', ratio: r, target: t, ...o });
export const ddef = (r, t = E1, o = {}) => ({ k: 'dmg', ratio: r, base: 'def', target: t, ...o });
export const hits = (n, r, t = ER) => ({ k: 'dmg', ratio: r, target: t, hits: n });
export const sh = r => ({ k: 'shield', ratio: r });
export const heal = r => ({ k: 'heal', ratio: r });
export const drain = r => ({ k: 'drain', ratio: r });
export const st = (id, v, t) => (t ? { k: 'status', id, v, target: t } : { k: 'status', id, v });
export const stk = (id, v, t) => (t ? { k: 'stack', id, v, target: t } : { k: 'stack', id, v });
export const spendAll = id => ({ k: 'spend', id, all: true });
export const per = (id, o = {}) => ({ k: 'perStack', id, ...o });
export const perTag = id => ({ k: 'perTag', id });
export const draw = (v, o = {}) => ({ k: 'draw', v, ...o });
export const make = (id, v = 1, o = {}) => ({ k: 'make', id, v, ...o });
export const ap = v => ({ k: 'ap', v });
export const ifStack = (id, n, o = {}) => ({ k: 'ifStack', id, n, ...o });
export const ifKill = { k: 'ifKill' }, ifWounded = { k: 'ifWounded' }, ifAll = { k: 'ifAllHeroes' };
export const ifBroken = { k: 'ifFoe', id: 'broken' };
export const inspire = { k: 'when', on: 'draw' };
export const power = (...rules) => ({ k: 'power', rules });
export const rule = (on, fx, o = {}) => ({ when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: { per: 'turn', n: o.limit } } : {}), fx });
export const later = (n, then) => ({ k: 'later', n, then });

// 카드 · 신탁 · 축복
export const O = (name, fx, o = {}) => ({ name, ...(o.cost !== undefined ? { cost: o.cost } : {}), ...(o.tags ? { tags: o.tags } : {}), ...(o.power ? { power: true } : {}), fx });
export const B = (name, x) => (typeof x === 'string' ? { name, kind: x } : Array.isArray(x) ? { name, fx: x } : { name, ...x });

export function card(hero, n, name, cost, type, fx, oracles, blesses, o = {}) {
  return { id: `${hero}_u${n}`, name, hero, unique: true, cost, type, ...(o.tags ? { tags: o.tags } : {}), fx, oracles, blesses };
}


// 시동 카드(사용자 2026-10-07 승인): 시작 4장 가운데 기본 카드 1장을 ①「열기」 카드로.
// 두 장인 비공격 카드(실드 · 회복) 하나 → 없으면 두 장인 공격 카드 하나 → 없으면 마지막 장.
export function starter(j, openerId) {
  if (process.env.NOSTARTER) return;   // 시동 카드 없는 판(비교 측정용)
  const h = j.heroes[0], st = h.starter, cards = Object.fromEntries(j.cards.map(c => [c.id, c]));
  const cnt = id => st.filter(x => x === id).length;
  let i = st.findIndex(id => cnt(id) >= 2 && cards[id]?.type !== '공격');
  if (i < 0) i = st.findIndex(id => cnt(id) >= 2);
  if (i < 0) i = st.length - 1;
  // 같은 카드 둘 가운데 뒤쪽을 바꾼다
  const id = st[i]; i = st.lastIndexOf(id);
  st[i] = openerId;
}

// 신탁 값어치 맞춤(loop.sh 가 boost 파일을 채운다) — 그 신탁의 피해 · 실드 · 회복 배율만 곱한다
function boost(j, B) {
  const mul = (fx, b) => { for (const f of fx) { if (['dmg', 'shield', 'heal'].includes(f.k) && f.ratio) f.ratio = Math.round(f.ratio * b * 100) / 100; if (f.k === 'power') for (const r of f.rules) mul(r.fx, b); } };
  for (const c of j.cards) (c.oracles || []).forEach((o, i) => { const b = B[`${c.id}|${i + 1}`]; if (b) mul(o.fx, b); });
}

// jobs: [['종족/사도', fn(j)], …]  boostFile: 이 스크립트 몫 boost json
export function run(jobs, boostFile) {
  const B = fs.existsSync(boostFile) ? JSON.parse(fs.readFileSync(boostFile, 'utf8')) : {};
  const only = process.argv[2];
  for (const [f, fn] of jobs) {
    if (only && !f.includes(only)) continue;
    const j = JSON.parse(fs.readFileSync(`${SRC}/${f}.json`, 'utf8'));
    fn(j);
    boost(j, B);
    fs.writeFileSync(`${OUT}/${f}.json`, JSON.stringify(j, null, 2) + '\n', 'utf8');
    console.log('썼다', f);
  }
}
