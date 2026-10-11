if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 3성 깊이 리뉴얼 공용 부품(2026-10-09) — 묶음 스크립트(b0.mjs …)가 import 한다.
import { STRUCT, POST } from './struct.mjs';
import { roleCoef } from './rolecoef.mjs';
import { completeCard } from './complete.mjs';
import { addBasics } from './basics4.mjs';
import { applyDiverse } from './diverse.mjs';
import { applyFeedback, dedupe } from './fb.mjs';
import { mergePassives } from './merge_pas.mjs';
import { dealerRole } from './dealer.mjs';
// 원본은 백업 C:/projects/_backup/heroes_villages_before_deep_20261009 에서 읽는다(몇 번 돌려도 같은 결과).
// 기준: _measure/깊이_기준/00_기준.md — 기본 카드도 새로(글 ≤30자 · 구조 그대로), 고유 4장 = 시동 · 연료 · 결정타 · 원작 자유, 신탁 다섯 = 방향.
import fs from 'fs'; import path from 'path'; import { pathToFileURL } from 'url';
export const SRC = process.env.DEEPSRC || 'C:/projects/_backup/heroes_villages_before_deep_20261009/heroes';
export const OUT = process.env.DEEPOUT || 'C:/projects/bolzena-content-v2/_gen/deep/heroes';
const HERE = new URL('.', import.meta.url);
const J = f => (fs.existsSync(new URL(f, HERE)) ? JSON.parse(fs.readFileSync(new URL(f, HERE), 'utf8')) : {});
const CUT5 = new Set(J('./cut5.json').list);
export const TUNE = J('./tune.json'), BOOST = J('./boost.json');
let NEWPAS = {}; export function setNewPas(m) { NEWPAS = m; }

// ── 조각 ──
export const E1 = 'oneEnemy', EA = 'allEnemies', ER = 'randomEnemy', TOP = 'topEnemy', LOW = 'lowEnemy', FAR = 'farEnemy';
export const ALLY = 'oneAlly', ALLIES = 'allAllies', OTHERS = 'otherAllies';
export const dmg = (r, t = E1, o = {}) => ({ k: 'dmg', ratio: r, target: t, ...o });
export const ddef = (r, t = E1, o = {}) => ({ k: 'dmg', ratio: r, base: 'def', target: t, ...o });
export const hits = (n, r, t = ER) => ({ k: 'dmg', ratio: r, target: t, hits: n });
export const sh = r => ({ k: 'shield', ratio: r });
export const heal = r => ({ k: 'heal', ratio: r });
export const st = (id, v, t) => (t ? { k: 'status', id, v, target: t } : { k: 'status', id, v });
export const stk = (id, v, t) => (t ? { k: 'stack', id, v, target: t } : { k: 'stack', id, v });
export const spendAll = id => ({ k: 'spend', id, all: true });
export const spendN = (id, v) => ({ k: 'spend', id, v });
export const per = (id, o = {}) => ({ k: 'perStack', id, ...o });
export const perTag = id => ({ k: 'perTag', id });
export const draw = (v, o = {}) => ({ k: 'draw', v, ...o });
export const make = (id, v = 1, o = {}) => ({ k: 'make', id, v, ...o });
export const ap = v => ({ k: 'ap', v });
export const gauge = v => ({ k: 'gauge', v });
export const disc = v => (v === 'all' ? { k: 'discard', all: true } : { k: 'discard', v });
export const ifStack = (id, n = 1, o = {}) => ({ k: 'ifStack', id, n, ...o });
export const notStack = (id, n = 1) => ({ k: 'ifStack', id, n, not: true });
export const ifKill = { k: 'ifKill' }, ifBreak = { k: 'ifBreak' }, ifHunted = { k: 'ifHunted' };
export const ifHand = n => ({ k: 'ifHand', n });
export const ifFoe = (id, o = {}) => ({ k: 'ifFoe', id, ...o });
export const empower = (ratio, who) => (who ? { k: 'empower', ratio, who } : { k: 'empower', ratio });
export const form = id => ({ k: 'form', id });
export const formEnd = { k: 'formEnd' };
export const tough = (v, t) => (t ? { k: 'tough', v, target: t } : { k: 'tough', v });
export const later = (n, then) => ({ k: 'later', n, then });
export const dmod = (v, target = ALLIES, o = {}) => ({ k: 'dealtMod', v, target, ...o });
export const extra = (r, t) => (t ? { k: 'extra', ratio: r, target: t } : { k: 'extra', ratio: r });
export const cost = (v, o = {}) => ({ k: 'cardStatus', id: '비용', v, to: 'hand', n: 1, ...o });
export const power = (...rules) => ({ k: 'power', rules });
export const rule = (on, fx, o = {}) => ({ when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), fx });
export const pw = (on, fx, o = {}) => power(rule(on, fx, o));
export const pas = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), fx });
export const O = (fx, o = {}) => ({ ...(o.cost !== undefined ? { cost: o.cost } : {}), ...(o.tags ? { tags: o.tags } : {}), ...(o.power ? { power: true } : {}), fx });
export const B = x => (typeof x === 'string' ? { kind: x } : Array.isArray(x) ? { fx: x } : x);

/** 고유 카드 — 신탁 다섯(O), 축복 셋(시동은 null → 공용 풀). */
export function U(id, name, cost, type, fx, oracles, blesses, tags) {
  if (oracles.length !== 5) throw new Error(`${id}: 신탁 다섯이 아니다`);
  const c = { id, name, hero: id.replace(/_u\d+$/, ''), unique: true, cost, type, ...(tags && tags.length ? { tags } : {}), fx,
    oracles: oracles.map((o, i) => ({ name: `신탁 ${i + 1}`, ...(o.tags === undefined && tags ? { tags } : {}), ...o })) };
  if (blesses) c.blesses = blesses.map((b, i) => ({ name: `축복 ${i + 1}`, ...B(b) }));
  return c;
}
/** 기본 카드(시작 카드) — 단순 · 짧게. */
export const S = (id, name, cost, type, fx, tags) => ({ id, name, hero: id.replace(/_s\d+$/, ''), cost, type, ...(tags && tags.length ? { tags } : {}), fx });
/** 생성 · 변신판 카드. */
export const T = (id, name, hero, type, fx, o = {}) => ({ id, name, hero, token: true, cost: o.cost ?? 0, type, ...(o.tags ? { tags: o.tags } : {}), fx, ...(o.blurb ? { blurb: o.blurb } : {}) });

const R2 = x => Math.round(x * 100) / 100;
const NUM = ['dmg', 'shield', 'heal', 'extra'];
function mulFx(fx, m, withEmp) {
  for (const f of fx || []) {
    if ((NUM.includes(f.k) || (withEmp && f.k === 'empower')) && f.ratio && !f.ofEvent) f.ratio = R2(f.ratio * m);
    if (f.k === 'power') for (const r of f.rules) mulFx(r.fx, m, withEmp);
    if (f.then) mulFx(f.then, m, withEmp);
  }
}
/** 세기 맞춤 — 고유 · 생성 카드 · 규칙(기본 카드는 안 건드린다). */
export function tune(j, m) {
  for (const c of j.cards) if (c.unique || c.token) { mulFx(c.fx, m); for (const o of c.oracles || []) mulFx(o.fx, m); for (const b of c.blesses || []) mulFx(b.fx, m); }
  const h = j.heroes[0];
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) mulFx(r.fx, m);
  for (const r of h.passives || []) mulFx(r.fx, m);
  for (const f of h.forms || []) for (const r of f.passives || []) mulFx(r.fx, m);
  if (h.ult && !h.ult.fx.some(f => f.k === 'form')) mulFx(h.ult.fx, m);   // 고학년도 함께(2026-10-10) — 변신을 드는 고학년은 따로(검사기 상한)
}
/** 신탁 값어치 보정(loop.mjs 가 boost.json 을 채운다). */
export function hasNum(fx) { return (fx || []).some(f => ((NUM.includes(f.k) || f.k === 'empower') && f.ratio && !f.ofEvent) || (f.k === 'power' && f.rules.some(r => hasNum(r.fx))) || (f.then && hasNum(f.then))); }
/** 수치 없는 신탁의 보정 — 첫 쌓기(없으면 생성 · 드로우) 수를 +n(규칙 안까지). */
function bump(fx, n) {
  const all = [];
  const walk = l => { for (const f of l || []) { all.push(f); if (f.k === 'power') for (const r of f.rules) walk(r.fx); if (f.then) walk(f.then); } };
  walk(fx);
  const t = all.find(f => f.k === 'stack' && f.v > 0 && !/Ally|Allies/.test(f.target || '')) ||   // 아군에게 붙이는 표시(초청객 · MVP · 짱팬)는 늘려도 뜻이 없다
    all.find(f => f.k === 'make') || all.find(f => f.k === 'draw');
  if (t) t.v += n;
}
export function boost(j) {
  for (const c of j.cards) (c.oracles || []).forEach((o, i) => { if (o.numOnly) return; const b = BOOST[`${c.id}|${i + 1}`]; if (b) mulFx(o.fx, b, true); const u = BOOST[`${c.id}|${i + 1}|+`]; if (u) bump(o.fx, u); });
  for (const c of j.cards) { const b = BOOST[`${c.id}|0`] || (c.token ? BOOST[`FORM:${c.name}`] : 0); if (b) mulFx(c.fx, b, true); }
}
/** 「수치만」 신탁 = 기본형 ×1.5(사용자 10-10, 카제나 수치만 77개 중앙값) — 기본과 효과 꼴(종류 · id · 대상 · 타수)이 같고 비용이 같은 신탁을
 *  기본 효과를 복사해 배율(피해 · 실드 · 회복 · 추가 공격 · 다음 카드 강화 · 증감)과 수(쌓기 · 생성 · 드로우 · 상태 · 재촉 · 게이지 — 반올림)를 모두 ×1.5. 손으로 쓴 수는 버린다. */
export const NUM_ONLY = 1.5;
/** 꼴이 같은 신탁의 비용 변화별 배율(사용자 10-10, 카제나 가운데 값): 그대로 ×1.5 · −1 ×1.0 · −2 ×0.85 · +1 ×1.6(검사기 「비용을 올리면 1.6배」). */
export const COST_MUL = { 0: NUM_ONLY, '-1': 1.0, '-2': 0.85, 1: 1.6 };
const sigOf = fx => JSON.stringify((fx || []).map(f => [f.k, f.id || '', f.target || '', f.hits || '', f.base || '', f.k === 'power' ? JSON.stringify((f.rules || []).map(r => [r.when, (r.fx || []).map(x => x.k + (x.id || ''))])) : '']));
export function numOnly(j) {
  // 같은 꼴(sig) · 같은 코스트 차의 신탁은 카드마다 첫 칸만 「수치만」 으로 다시 셈 — 둘째부터는 제 수치를 둔다(2026-10-10 빌드 피드백 「같은 글 신탁」 의 뿌리)
  for (const c of j.cards.filter(c => c.unique)) { const seen = new Set(); (c.oracles || []).forEach((o, i) => {
    const dc = (o.cost ?? c.cost) - c.cost;
    if (o.power || sigOf(o.fx) !== sigOf(c.fx) || !(dc in COST_MUL) || seen.has(dc)) return;
    seen.add(dc);
    const m = COST_MUL[dc];
    const fx = JSON.parse(JSON.stringify(c.fx));
    mulFx(fx, m, true);
    const walk = l => { for (const f of l || []) {
      if (['stack', 'make', 'draw', 'status', 'hasten', 'gauge'].includes(f.k) && f.v > 0) f.v = Math.max(1, Math.round(f.v * m));
      if (['dealtMod', 'critMod', 'atkMod', 'defMod', 'takenMod'].includes(f.k) && f.v) f.v = Math.round(f.v * m * 100) / 100;
      if (f.k === 'power') for (const r of f.rules) walk(r.fx); if (f.then) walk(f.then); } };
    walk(fx);
    o.fx = fx; o.numOnly = true;
  }); }
}
/** 그 사도 파일을 백업에서 읽고 fn 으로 고친 뒤 OUT 에 원자적으로 쓴다. 기본 카드 · 시작 덱은 fn 이 정한다. */
/** 상한을 넘는 쌓기(2026-10-10 빌드 피드백 「버려지는 수」) — 그 사도 고유 효과의 상한(cap)보다 큰 쌓기 수는 상한으로 내리고, 넘친 몫은 드로우 1(효과 셋 안일 때)로. */
/** 값어치 덤(boost.json 의 「카드|신탁|d」 = 드로우 덤 수) — 정수만 있는 신탁 · 상한에 막힌 신탁의 마지막 보정. 효과 셋이 차 있으면 있는 드로우에 더한다. */
export function extraDraw(j) {
  for (const c of j.cards) (c.oracles || []).forEach((o, i) => {
    const d = BOOST[`${c.id}|${i + 1}|d`]; if (!d) return;
    const dr = o.fx.find(f => f.k === 'draw' && !f.who);
    if (dr) dr.v += d; else if (o.fx.filter(f => !/^(if|per|when)/.test(f.k)).length < 3) o.fx.push({ k: 'draw', v: d });
    else { const s = o.fx.find(f => f.k === 'stack'); if (s) s.v += d; }
  });
}
export function capStacks(j) {
  const h = j.heroes[0]; const caps = {};
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) if (k.cap > 0) caps[k.name] = k.cap;
  const fix = fx => { if (!fx) return; let over = false;
    for (const f of fx) if (f.k === 'stack' && caps[f.id] && f.v > caps[f.id]) { f.v = caps[f.id]; over = true; }
    const n = fx.filter(f => !/^(if|per)/.test(f.k)).length;
    if (over && n < 3 && !fx.some(f => f.k === 'draw')) fx.push({ k: 'draw', v: 1 }); };
  for (const c of j.cards.filter(c => c.unique)) { fix(c.fx); for (const o of c.oracles || []) fix(o.fx); }
}
export function noFightLimit(o) {
  if (Array.isArray(o)) { for (const x of o) noFightLimit(x); return; }
  if (!o || typeof o !== 'object') return;
  if (o.limit && o.limit.per === 'fight') delete o.limit;
  for (const k in o) noFightLimit(o[k]);
}
export function run(JOBS) {
  const only = process.argv[2];
  for (const [f, fn] of JOBS) {
    if (only && !f.includes(only)) continue;
    const j = JSON.parse(fs.readFileSync(`${SRC}/${f}.json`, 'utf8'));
    fn(j);
    if (CUT5.has(f)) j.cards = j.cards.filter(c => c.id !== `${j.heroes[0].id}_u5`);   // 5장 → 4장(cut5.json)
    if (NEWPAS[f]) j.heroes[0].passives = JSON.parse(JSON.stringify(NEWPAS[f]));   // 패시브 새로 짜기(passives_new.mjs)
    if (STRUCT[f]) STRUCT[f](j);   // 구조 손질(struct.mjs)
    addBasics(j);   // 기본 4장 · 시작 덱(basics4.mjs)
    applyDiverse(j);   // 틀 다양성(diverse.mjs)
    applyFeedback(j, f);   // 빌드 피드백 · 인연 자원 짝(fb.mjs)
    if (!process.env.NOMERGE) mergePassives(j);   // 패시브를 고유 능력으로(merge_pas.mjs) — NOMERGE=1 이면 건너뜀(세기 비교용)
    dealerRole(j);   // 딜러 생존 효과 빼기(dealer.mjs)
    noFightLimit(j);   // 「전투당 n회」 없앰(2026-10-10 사용자 결정 — 턴당 제한은 그대로)
    numOnly(j);
    boost(j);
    for (const c of j.cards) for (const o of c.oracles || []) delete o.numOnly;
    if (TUNE[f]) tune(j, TUNE[f]);   // 사도별 세기 = 고유 카드 · 완성형 · 고학년 수치(2026-10-10 카제나식 — 스탯은 역할 × 등급 표 하나), 끝에 round10
    if (POST[f]) POST[f](j);   // 배율 뒤 손 보정(struct.mjs POST)
    roleCoef(j);   // 역할다움 카드 계수(rolecoef.mjs)
    completeCard(j);   // 완성형 한 장(complete.mjs)
    capStacks(j);   // 상한을 넘는 쌓기 내리기
    dedupe(j);   // 같은 글 신탁 풀기(fb.mjs)
    extraDraw(j);   // 값어치 덤(드로우)
    const out = `${OUT}/${f}.json`;
    fs.mkdirSync(path.dirname(out), { recursive: true });
    const tmp = `${out}.tmp${process.pid}`;
    fs.writeFileSync(tmp, JSON.stringify(j, null, 2) + '\n', 'utf8'); fs.renameSync(tmp, out);
    console.log('썼다', f);
  }
}
/** 사도 한 명을 통째로 — 기본 카드 · 고유 · 생성 카드 · 키워드 · 패시브 · 변신 · 고학년 · 시작 덱. 장비(equips)는 그대로 둔다. */
export function hero(j, o) {
  const h = j.heroes[0];
  if (o.keyword) h.keyword = o.keyword;
  if (o.keywords) h.keywords = o.keywords; else delete h.keywords;
  h.passives = o.passives;
  if (o.forms) h.forms = o.forms; else delete h.forms;
  if (o.ult) h.ult = o.ult;
  if (o.style) h.style = o.style;
  if (o.blurb) h.blurb = o.blurb;
  h.starter = o.starter;
  j.cards = [...o.basics, ...(o.tokens || []), ...o.uniques];
}
/** 묶음 스크립트를 직접 돌렸을 때만 run(다른 스크립트가 import 하면 안 돈다). */
export function main(url, JOBS) { if (process.argv[1] && url === pathToFileURL(process.argv[1]).href) run(JOBS); }
