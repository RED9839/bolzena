// 18갈래 재설계 — 나머지 118명 공용 부품(2026-10-08). 시범16.mjs 의 조각 · 신탁 틀(five · unshallow) · 세기 맞춤(tune) · 값어치 보정(boost)을 그대로 옮겼다.
// 종족 스크립트(<종족>_18.mjs)가 import 한다. 원본은 백업 SRC 에서 읽는다(몇 번 돌려도 같은 결과). 저장은 사도 단위 원자적(임시 파일 → rename).
// 쓰는 법: import * as L from './lib18.mjs'; … L.run18(JOBS, TUNE, new URL('./boost_<종족>_18.json', import.meta.url));
import fs from 'fs';
import { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifBroken, inspire, power, rule, later, B } from './lib.mjs';
export { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifBroken, inspire, power, rule, later, B };

export const SRC = 'C:/projects/_backup/heroes_before_118_20261008';
export const OUT = process.env.BZOUT || 'C:/projects/bolzena-content-v2/heroes';
export const TOP = 'topEnemy', OTHER = 'otherEnemy', ALLY = 'oneAlly', ALLIES = 'allAllies', STRONG = 'strongestAlly';

// ── 조각 ──
export const R2 = x => Math.round(x * 100) / 100;
export const tough = (v, t) => (t ? { k: 'tough', v, target: t } : { k: 'tough', v });
export const disc = v => ({ k: 'discard', v });
export const burn = v => ({ k: 'burn', v });
export const nextAp = v => ({ k: 'nextAp', v });
export const hasten = v => ({ k: 'hasten', v });
export const gauge = v => ({ k: 'gauge', v });
export const empower = (ratio, who) => (who ? { k: 'empower', ratio, who } : { k: 'empower', ratio });
export const ripen = (id, v, target) => ({ k: 'ripen', id, ...(v ? { v } : {}), ...(target ? { target } : {}) });
export const summon = (id, ratio, o = {}) => ({ k: 'summon', id, ratio, ...o });
export const perGone = who => (who ? { k: 'perGone', who } : { k: 'perGone' });
export const ifGained = (id, n, not) => ({ k: 'ifGained', id, n, ...(not ? { not: true } : {}) });
export const ifPricier = { k: 'ifPricier' };
export const ifStreak = (n, type) => ({ k: 'ifStreak', n, ...(type ? { type } : {}) });
export const ifNth = n => ({ k: 'ifNth', n });
export const ifFoe = (id, o = {}) => ({ k: 'ifFoe', id, ...o });
export const ifHand = n => ({ k: 'ifHand', n });
export const ifBreak = { k: 'ifBreak' };
export const killElite = { k: 'ifKill', id: 'elite' };
export const stage = (K, n) => ifStack(K, n, { max: n });
export const notStack = (id, n = 1) => ({ k: 'ifStack', id, n, not: true });
export const spendN = (id, v) => ({ k: 'spend', id, v });
export const costMod = (v, o = {}) => ({ k: 'costMod', v, turns: 1, ...o });
export const cs = (id, v, o = {}) => ({ k: 'cardStatus', id, v, ...o });
export const perCs = (id, o = {}) => ({ k: 'perCardSt', id, ...o });
export const perOver = (pct, per) => ({ k: 'perOverheal', pct, per });
export const exile = (from, o = {}) => ({ k: 'exileFrom', from, ...o });
export const form = id => ({ k: 'form', id });
export const formEnd = { k: 'formEnd' };
export const dmod = (v, target, turns) => ({ k: 'dealtMod', v, ...(target ? { target } : {}), ...(turns ? { turns } : {}) });
export const atkRun = v => ({ k: 'atkMod', v, run: true, target: 'self' });
export const srch = (o = {}) => draw(1, { who: 'self', unique: true, ...o });
export const drawType = (type, v = 1) => draw(v, { who: 'self', type });
export const pull = (o = {}) => ({ k: 'pull', from: 'discard', n: 1, ...o });
export const xtra = (r, t) => ({ k: 'extra', ratio: r, ...(t ? { target: t } : {}) });
export const pw = (on, fx, o = {}) => power(rule(on, fx, o));
export const pas = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: { per: o.per || 'turn', n: o.limit } } : {}), fx });
export const token = (id, name, hero, type, fx, o = {}) => ({ id, name, hero, token: true, cost: o.cost ?? 0, type, tags: o.tags ?? ['소멸'], fx, ...(o.blurb ? { blurb: o.blurb } : {}) });

// ── 신탁 틀(칸 — 지침 §3 · §12-1) ──
// 피해 · 실드 · 회복 · 추가 공격 · 소환물 몫만 곱한다(조건 · 비례 · 장치는 그대로)
export const scl = (fx, m) => fx.map(f => {
  const g = { ...f };
  if (['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent) g.ratio = R2(f.ratio * m);
  if (f.k === 'power') g.rules = f.rules.map(r => ({ ...r, fx: scl(r.fx, m) }));
  if (f.then) g.then = scl(f.then, m);
  return g;
});
export const Or = (fx, o = {}) => ({ ...(o.cost !== undefined ? { cost: o.cost } : {}), ...(o.tags ? { tags: o.tags } : {}), ...(o.power ? { power: true } : {}), fx });
// 한 장의 신탁 다섯 — base: { fx, cost, tags }. 칸: A 수치 · B 비용↓ · C 비싼 한 방 · H 대가(손패 버리기 · 다음 턴 AP · 태그 빼기 · 소멸) + 손으로 짠 둘(재설계 · 강화화/서치)
export const isCond = f => f.k.startsWith('if') || f.k === 'when' || f.k.startsWith('per') || f.k === 'cue';
export const dropLast = fx => { const a = fx.slice(); for (let i = a.length - 1; i >= 0; i--) if (!isCond(a[i]) && a[i].k !== 'power' && a[i].k !== 'form') { a.splice(i, 1); break; } while (a.length && isCond(a[a.length - 1])) a.pop(); return a; };
export const hasNum = fx => fx.some(f => ['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent);
// 수치가 없는 카드의 얕은 갈래 · 대가 갈래는 장치를 하나 더(첫 「쌓기」 +1)
export const bump = (fx, on) => { if (!on) return fx; let done = false; return fx.map(f => (!done && f.k === 'stack' && f.v > 0 ? (done = true, { ...f, v: f.v + 1 }) : f)); };
export const nFx = fx => fx.filter(f => !(f.k.startsWith('if') || f.k === 'when' || f.k.startsWith('per') || f.k === 'cue')).length;
export function five(base, list) {
  const t = base.tags || [];
  const full = nFx(base.fx) >= 3;   // 효과 셋이면 덤을 더하지 않는다(검사기 — 카드당 셋까지)
  const mk = x => {
    if (full && (x === 'Hd' || x === 'Hn' || (Array.isArray(x) && x[0] === 'C'))) return Or(scl(dropLast(base.fx), 1.8), { tags: t });   // 효과 교환 — 마지막 덤을 빼고 수치↑
    if (x === 'A') return Or(bump(scl(base.fx, 1.35), !hasNum(base.fx)), { tags: t });
    if (x === 'B') return base.cost > 0 ? Or(scl(base.fx, 0.95), { cost: base.cost - 1, tags: t }) : Or(scl(base.fx, 1.15), { tags: [...t, '신속'] });
    if (Array.isArray(x) && x[0] === 'C') return Or([...scl(base.fx, 1.6), ...x[1]], { cost: base.cost + 1, tags: t });
    // D 강화화 — 낼 때 효과 조금(×0.65) + 전투 동안 남는 규칙(카제나 BEST 칸 — 반복하던 일을 강화로)
    if (Array.isArray(x) && x[0] === 'D') return Or([...scl(full ? dropLast(base.fx) : base.fx, full ? 0.9 : 0.65), pw(x[1], x[2], x[3] || {})], { tags: t, power: true });   // 효과 셋이면 마지막 덤 대신 규칙
    if (x === 'Hd') return Or([...bump(scl(base.fx, 1.5), true), disc(1)], { tags: t });
    if (x === 'Hn') return Or([...bump(scl(base.fx, 1.4), true), { k: 'payHp', v: 40 }], { tags: t });
    if (x === 'Hx') return Or(scl(base.fx, 2.1), { tags: [...t.filter(y => y !== '보존'), '소멸'] });
    if (Array.isArray(x) && x[0] === 'Ht') return Or(scl(base.fx, 1.3), { tags: t.filter(y => y !== x[1]) });
    return x;   // 손으로 짠 것(Or)
  };
  return unshallow(base, list.map((x, i) => ({ name: `신탁 ${i + 1}`, ...mk(x) })));
}
// 얕은 갈래(효과 종류가 기본과 같은 것)는 카드당 둘까지(§12-1) — 셋째부터는 기본에 없는 덤 하나(서치 · 없으면 고학년 게이지)로 종류를 바꾼다
export const kindsOf = fx => new Set((fx || []).map(f => f.k));
export function unshallow(base, os) {
  const bk = kindsOf(base.fx); let n = 0;
  return os.map(o => {
    const ok = kindsOf(o.fx);
    const same = [...ok].every(k => bk.has(k)) && [...bk].every(k => ok.has(k));
    if (!same || o.power) return o;
    if (++n <= 2) return o;
    let fx = nFx(o.fx) >= 3 ? dropLast(o.fx) : o.fx;
    const add = !bk.has('draw') ? srch() : gauge(15);
    return { ...o, fx: [...fx, add] };
  });
}
export const bl = (...xs) => xs.map((x, i) => B(`축복 ${i + 1}`, x));
// 고유 카드 — oracles 는 five 의 결과, blesses 가 null 이면 시동 카드(공용 축복 풀)
export function card(H, n, name, cost, type, fx, oracles, blesses, tags) {
  const c = { id: `${H}_u${n}`, name, hero: H, unique: true, cost, type, ...(tags && tags.length ? { tags } : {}), fx, oracles };
  if (blesses) c.blesses = blesses;
  return c;
}
export const U = (H, n, name, cost, type, fx, list, blesses, tags) => card(H, n, name, cost, type, fx, five({ fx, cost, tags }, list), blesses, tags);
export const setCards = (j, cards, tokens = []) => {
  const ids = new Set(tokens.map(t => t.id));
  j.cards = [...j.cards.filter(c => !c.unique && !ids.has(c.id)), ...tokens, ...cards];
};
export const setOpener = (j, H, from, to) => { const h = j.heroes[0]; const i = h.starter.indexOf(`${H}_${from}`); if (i >= 0) h.starter[i] = `${H}_${to}`; };
export const renameKw = (h, from, to) => { const fix = fx => fx.map(f => (f.id === from ? { ...f, id: to } : f)); h.ult.fx = fix(h.ult.fx); };

// ── 세기 맞춤(측정 뒤 사도 전체 배율) — 변신판(_f1)은 빼고 ──
export function tune(j, m) {
  const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent) f.ratio = R2(f.ratio * m); if (f.k === 'power') for (const r of f.rules) mul(r.fx); if (f.then) mul(f.then); } };
  for (const c of j.cards) if (c.unique || (c.token && !/_f\d+$/.test(c.id))) { mul(c.fx); for (const o of c.oracles || []) mul(o.fx); for (const b of c.blesses || []) mul(b.fx); }
  const h = j.heroes[0];
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) mul(r.fx);
  for (const r of h.passives || []) mul(r.fx);
}
// ── 신탁 값어치 보정(loop18.mjs 가 boost 파일을 채운다) ──
export function boost(j, Bst) {
  const mul = (fx, b) => { for (const f of fx) { if (['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent) f.ratio = R2(f.ratio * b); if (f.k === 'power') for (const r of f.rules) mul(r.fx, b); } };
  for (const c of j.cards) (c.oracles || []).forEach((o, i) => { const b = Bst[`${c.id}|${i + 1}`]; if (b) mul(o.fx, b); });
}
// 원자적 저장 — 같은 폴더 임시 파일에 쓰고 rename
export function save(file, j) {
  const tmp = `${file}.tmp${process.pid}`;
  fs.writeFileSync(tmp, JSON.stringify(j, null, 2) + '\n', 'utf8');
  fs.renameSync(tmp, file);
}
// JOBS: [['종족/파일이름', fn(j)], …] · TUNE: { '종족/파일이름': 배율 } · boostFile: URL
// node <종족>_18.mjs [사도 이름 일부]
export function run18(JOBS, TUNE, boostFile) {
  const Bst = fs.existsSync(boostFile) ? JSON.parse(fs.readFileSync(boostFile, 'utf8')) : {};
  const only = process.argv[2];
  for (const [f, fn] of JOBS) {
    if (only && !f.includes(only)) continue;
    const j = JSON.parse(fs.readFileSync(`${SRC}/${f}.json`, 'utf8'));
    fn(j);
    boost(j, Bst);
    if (TUNE[f]) tune(j, TUNE[f]);
    save(`${OUT}/${f}.json`, j);
    console.log('썼다', f);
  }
}
