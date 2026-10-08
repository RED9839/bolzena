// 18갈래 재설계 2단계 — 시범 사도 17명(시범 16 + 코미, 돈형 없음). 2026-10-08
// 원안: _measure/갈래_세분화/02_시범16_컨셉.md · 검토 D: 03_재설계_검토.md · 1단계 엔진: 04_1단계_엔진봇.md · 결과: 05_시범16_결과.md
// 틀: 시작 덱 = 기본 3 + 시동 1(고유 카드 하나 — 엔진 IsOpener) · 은총 고유 카드 4장(쓰기 · 갈래 부품 · 원작 자유 · 만들기/굴리기 둘째) ·
//     신탁 5갈래(얕은 ≤2 · 재설계 1~2 · 강화화/서치 · 대가) · 축복 = 은총 4장 × 3 = 12(시동 카드는 공용 축복 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/시범16.mjs [사도 이름 일부]  → heroes/<종족>/<사도>.json 덮어쓰기(백업에서 읽음 — 몇 번 돌려도 같은 결과)
import fs from 'fs';
import { E1, EA, ER, dmg, ddef, hits, sh, heal, st, stk, spendAll, per, draw, make, ap, ifStack, ifKill, ifWounded, power, rule, later, B } from './lib.mjs';

const SRC = 'C:/projects/_backup/heroes_before_trial16_20261008';
const OUT = process.env.BZOUT || 'C:/projects/bolzena-content-v2/heroes';
const TOP = 'topEnemy', OTHER = 'otherEnemy', ALLY = 'oneAlly', ALLIES = 'allAllies', STRONG = 'strongestAlly';

// ── 조각 ──
const R2 = x => Math.round(x * 100) / 100;
const tough = (v, t) => (t ? { k: 'tough', v, target: t } : { k: 'tough', v });
const disc = v => ({ k: 'discard', v });
const burn = v => ({ k: 'burn', v });
const nextAp = v => ({ k: 'nextAp', v });
const hasten = v => ({ k: 'hasten', v });
const gauge = v => ({ k: 'gauge', v });
const empower = (ratio, who) => (who ? { k: 'empower', ratio, who } : { k: 'empower', ratio });
const ripen = (id, v, target) => ({ k: 'ripen', id, ...(v ? { v } : {}), ...(target ? { target } : {}) });
const summon = (id, ratio, o = {}) => ({ k: 'summon', id, ratio, ...o });
const perGone = who => (who ? { k: 'perGone', who } : { k: 'perGone' });
const ifGained = (id, n, not) => ({ k: 'ifGained', id, n, ...(not ? { not: true } : {}) });
const ifPricier = { k: 'ifPricier' };
const ifStreak = (n, type) => ({ k: 'ifStreak', n, ...(type ? { type } : {}) });
const ifNth = n => ({ k: 'ifNth', n });
const ifFoe = (id, o = {}) => ({ k: 'ifFoe', id, ...o });
const ifHand = n => ({ k: 'ifHand', n });
const ifBreak = { k: 'ifBreak' };
const killElite = { k: 'ifKill', id: 'elite' };
const stage = (K, n) => ifStack(K, n, { max: n });
const notStack = (id, n = 1) => ({ k: 'ifStack', id, n, not: true });
const spendN = (id, v) => ({ k: 'spend', id, v });
const costMod = (v, o = {}) => ({ k: 'costMod', v, turns: 1, ...o });
const cs = (id, v, o = {}) => ({ k: 'cardStatus', id, v, ...o });
const perCs = (id, o = {}) => ({ k: 'perCardSt', id, ...o });
const perOver = (pct, per) => ({ k: 'perOverheal', pct, per });
const exile = (from, o = {}) => ({ k: 'exileFrom', from, ...o });
const form = id => ({ k: 'form', id });
const formEnd = { k: 'formEnd' };
const dmod = (v, target, turns) => ({ k: 'dealtMod', v, ...(target ? { target } : {}), ...(turns ? { turns } : {}) });
const atkRun = v => ({ k: 'atkMod', v, run: true, target: 'self' });
const srch = (o = {}) => draw(1, { who: 'self', unique: true, ...o });
const drawType = (type, v = 1) => draw(v, { who: 'self', type });
const pull = (o = {}) => ({ k: 'pull', from: 'discard', n: 1, ...o });
const xtra = (r, t) => ({ k: 'extra', ratio: r, ...(t ? { target: t } : {}) });
const pw = (on, fx, o = {}) => power(rule(on, fx, o));
const pas = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: { per: o.per || 'turn', n: o.limit } } : {}), fx });
const token = (id, name, hero, type, fx, o = {}) => ({ id, name, hero, token: true, cost: o.cost ?? 0, type, tags: o.tags ?? ['소멸'], fx, ...(o.blurb ? { blurb: o.blurb } : {}) });

// ── 신탁 틀(칸 — 지침 §3 · §12-1) ──
// 피해 · 실드 · 회복 · 추가 공격 · 소환물 몫만 곱한다(조건 · 비례 · 장치는 그대로)
const scl = (fx, m) => fx.map(f => {
  const g = { ...f };
  if (['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent) g.ratio = R2(f.ratio * m);
  if (f.k === 'power') g.rules = f.rules.map(r => ({ ...r, fx: scl(r.fx, m) }));
  if (f.then) g.then = scl(f.then, m);
  return g;
});
const Or = (fx, o = {}) => ({ ...(o.cost !== undefined ? { cost: o.cost } : {}), ...(o.tags ? { tags: o.tags } : {}), ...(o.power ? { power: true } : {}), fx });
// 한 장의 신탁 다섯 — base: { fx, cost, tags }. 칸: A 수치 · B 비용↓ · C 비싼 한 방 · H 대가(손패 버리기 · 다음 턴 AP · 태그 빼기 · 소멸) + 손으로 짠 둘(재설계 · 강화화/서치)
const isCond = f => f.k.startsWith('if') || f.k === 'when' || f.k.startsWith('per') || f.k === 'cue';
const dropLast = fx => { const a = fx.slice(); for (let i = a.length - 1; i >= 0; i--) if (!isCond(a[i]) && a[i].k !== 'power' && a[i].k !== 'form') { a.splice(i, 1); break; } while (a.length && isCond(a[a.length - 1])) a.pop(); return a; };
const hasNum = fx => fx.some(f => ['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent);
// 수치가 없는 카드의 얕은 갈래 · 대가 갈래는 장치를 하나 더(첫 「쌓기」 +1)
const bump = (fx, on) => { if (!on) return fx; let done = false; return fx.map(f => (!done && f.k === 'stack' && f.v > 0 ? (done = true, { ...f, v: f.v + 1 }) : f)); };
const nFx = fx => fx.filter(f => !(f.k.startsWith('if') || f.k === 'when' || f.k.startsWith('per') || f.k === 'cue')).length;
function five(base, list) {
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
const kindsOf = fx => new Set((fx || []).map(f => f.k));
function unshallow(base, os) {
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
const bl = (...xs) => xs.map((x, i) => B(`축복 ${i + 1}`, x));
// 고유 카드 — oracles 는 five 의 결과, blesses 가 null 이면 시동 카드(공용 축복 풀)
function card(H, n, name, cost, type, fx, oracles, blesses, tags) {
  const c = { id: `${H}_u${n}`, name, hero: H, unique: true, cost, type, ...(tags && tags.length ? { tags } : {}), fx, oracles };
  if (blesses) c.blesses = blesses;
  return c;
}
const U = (H, n, name, cost, type, fx, list, blesses, tags) => card(H, n, name, cost, type, fx, five({ fx, cost, tags }, list), blesses, tags);
const setCards = (j, cards, tokens = []) => {
  const ids = new Set(tokens.map(t => t.id));
  j.cards = [...j.cards.filter(c => !c.unique && !ids.has(c.id)), ...tokens, ...cards];
};
const setOpener = (j, H, from, to) => { const h = j.heroes[0]; const i = h.starter.indexOf(`${H}_${from}`); if (i >= 0) h.starter[i] = `${H}_${to}`; };
const renameKw = (h, from, to) => { const fix = fx => fx.map(f => (f.id === from ? { ...f, id: to } : f)); h.ult.fx = fix(h.ult.fx); };

// ════════════════════════════════════════════════════════════════════
// 1. 리코타 — 박자형 · 탱커 · 냉정. 가벼운 접시에서 무거운 접시로 — 바로 앞 카드보다 비싼 카드가 나오면 「코스」 +1, 셋째 접시가 풀코스
// 원작: 넷째 평타마다 웍질(범위 + 회복) · 저학년 파티 회복 + 센 아군 피해↑ · 사물을 똑같이 본뜨는 특기
// 시동: u1 아뮤즈 부쉬(0코 · 턴 첫 접시)
// ════════════════════════════════════════════════════════════════════
function ricotta(j) {
  const H = '리코타', K = '코스';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '가벼운 접시에서 무거운 접시로 이어지는 차림', carrier: 'self', cap: 3, endClear: true };
  delete h.keywords;
  h.passives = [
    pas('코스 진행', 'play', [stk(K, 1)], { when: { who: 'any' }, conds: [{ c: 'pricier' }] }),
    pas('웍질', 'play', [ddef(0.22, EA), heal(0.2)], { when: { who: 'any', nth: 4 } }),
  ];
  const bz = ['분쇄'];
  setCards(j, [
    // 시동 — 0코 실드 · 드로우, 턴 첫 카드면 첫 접시
    U(H, 1, '아뮤즈 부쉬', 0, '스킬', [sh(0.7), draw(1), ifNth(1), stk(K, 1)], [
      'A', 'B',
      Or([sh(0.5), draw(1), srch({ type: '스킬' })]),
      Or([sh(0.6), draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      'Hd',
    ], null),
    // 쓰기 — 「코스」 1개당 회복, 전부 소모. 바로 앞보다 비싸면(셋째 접시) 파티의 다음 카드 강화
    U(H, 3, '리코타 풀코스', 2, '스킬', [per(K, { n: 1 }), heal(0.6), spendAll(K), ifPricier, empower(0.5, 'any')], [
      'A', 'B',
      Or([per(K, { n: 1 }), sh(0.7), spendAll(K), ifPricier, empower(0.6, 'any')]),
      Or([per(K, { n: 1 }), heal(0.55), ifPricier, empower(0.4, 'any')]),
      'Hn',
    ], bl('heal', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 손의 카드 1장을 한 단계 무겁게(비용 +1) 하고 파티의 다음 카드 강화(복사 대신 — 엔진 부품을 늘리지 않음)
    U(H, 5, '똑같이 본뜬 요리', 1, '스킬', [sh(0.6), cs('비용', 1, { to: 'hand', n: 1 }), empower(0.5, 'any')], [
      'A',
      Or([sh(0.7), empower(0.6, 'any'), stk(K, 1)]),
      Or([sh(0.6), cs('비용', 1, { to: 'hand', n: 1, who: 'other' }), empower(0.7, 'any')]),
      Or([sh(0.6), empower(0.5, 'any'), pw('play', [empower(0.15, 'any')], { when: { who: 'any' }, conds: [{ c: 'pricier' }], limit: 1 })], { power: true }),
      Or([sh(0.9), cs('비용', 1, { to: 'hand', n: 2 }), empower(1.0, 'any')]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 원작 자유 — 반격 담당 · 코스 셋이면 웍질 광역
    U(H, 4, '요리를 무시하지 마십시오', 1, '공격', [st('반격', 2), ddef(0.55), stage(K, 3), ddef(0.5, EA)], [
      'A', ['D', 'play', [st('반격', 1)], { when: { type: '공격' }, limit: 1 }],
      Or([st('반격', 2), ddef(0.7, EA), ifPricier, sh(1.2)]),
      Or([st('반격', 2), ddef(1.0), srch()]),
      Or([st('반격', 3), ddef(0.9), disc(1)]),
    ], bl('defUp', { tags: ['보존'] }, [st('결의', 1)])),
    // 만들기 둘째 — 메인 요리(방어 기반), 바로 앞보다 비싸면 더 조린다
    U(H, 2, '다 조려버리겠습니다', 1, '공격', [ddef(0.7), ifPricier, ddef(0.5)], [
      'A',
      ['C', [stk(K, 1)]],
      Or([ddef(0.6, EA), ifStack(K, 2), sh(0.9)], { tags: bz }),
      Or([ddef(0.6), pw('play', [ddef(0.2, EA)], { when: { type: '공격' }, limit: 1 })], { tags: bz, power: true }),
      ['Ht', '분쇄'],
    ], bl('power', 'cost', [stk(K, 1)]), bz),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 티그 — 박자형 · 딜러 · 활발. 제 공격 카드만 끊김 없이 셋 이어 셋째 칼(마이웨이) — 스킬이든 동료 카드든 끼면 손맛이 식는다
// 원작: 셋째 공격마다 내려치기 + 범위 + SP 회복 · 고학년 오버드라이브
// 시동: u2 쌍검 휘두르기(1코 · 다음 공격 비용 -1)
// ════════════════════════════════════════════════════════════════════
function tig(j) {
  const H = '티그', K = '장작 패기';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '이번 턴 휘두를수록 붙는 손맛', carrier: 'self', cap: 3, endClear: true, wipe: true, per: [{ stat: 'dealt', v: 0.05 }] };
  h.passives = [
    pas('마이웨이', 'play', [stk(K, 1)], { when: { type: '공격' } }),
    pas('마이웨이', 'play', [spendAll(K)], { when: { type: '스킬' } }),
    pas('공격 일변도', 'turnEnd', [later(1, [drawType('공격')])], { conds: [{ c: 'ownNone', type: '스킬' }, { c: 'playedMin', n: 2 }] }),
  ];
  const sw = ['신속'];
  const atkCheap = costMod(-1, { n: 1, who: 'self', type: '공격' });
  setCards(j, [
    U(H, 1, '소닉 블레이드', 1, '공격', [dmg(0.8, EA), ifStack(K, 2), dmg(0.4, EA)], [
      'A', 'B',
      Or([dmg(1.0, EA), ifStreak(3, '공격'), dmg(0.5, EA), ap(1)], { tags: sw }),
      Or([dmg(1.0, EA), pw('play', [dmg(0.2, EA)], { when: { type: '공격' }, limit: 1 })], { tags: sw, power: true }),
      ['Ht', '신속'],
    ], bl('power', 'draw', 'weakSpot'), sw),
    // 시동 — 2회 + 다음 자신의 공격 비용 -1
    U(H, 2, '쌍검 휘두르기', 1, '공격', [dmg(0.55, E1, { hits: 2 }), atkCheap], [
      'A', 'B',
      Or([dmg(0.5, E1, { hits: 2 }), drawType('공격')]),
      Or([dmg(0.5, E1, { hits: 2 }), ifStreak(2, '공격'), stk(K, 1)]),
      'Hd',
    ], null),
    // 쓰기 — 셋째 칼: 손맛 1개당 적 전체 · AP +1
    U(H, 3, '내려치고 휘두르기', 1, '공격', [dmg(0.85), ifStreak(3, '공격'), per(K), dmg(0.3, EA), ap(1)], [
      'A',
      ['D', 'play', [dmg(0.5, EA)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.6, EA), ifStreak(3, '공격'), per(K), dmg(0.3, EA), ap(1)]),
      Or([dmg(0.85), srch({ type: '공격' }), ifStreak(3, '공격'), per(K), dmg(0.4, EA)]),
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 원작 자유 — 오버드라이브(강화): 공격력 + 이번 턴 공격 비용 -1
    U(H, 4, '백호 비전서', 1, '강화', [atkRun(0.12), costMod(-1, { n: 2, who: 'self', type: '공격' })], [
      Or([atkRun(0.16), costMod(-1, { n: 2, who: 'self', type: '공격' })]),
      Or([atkRun(0.1), costMod(-1, { n: 2, who: 'self', type: '공격' })], { cost: 0 }),
      Or([atkRun(0.12), costMod(-1, { n: 2, who: 'self', type: '공격' }), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })]),
      Or([atkRun(0.1), drawType('공격', 2)]),
      Or([atkRun(0.2), costMod(-1, { n: 2, who: 'self', type: '공격' }), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('atkUp', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 0코 공격: 소량 + 자신의 공격 카드 1장. 바로 앞이 자신의 공격이면 손맛 +1
    U(H, 5, '백호 자세', 0, '공격', [dmg(0.5), drawType('공격'), ifStreak(2, '공격'), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.4, EA), drawType('공격'), ifStreak(2, '공격'), stk(K, 1)]),
      Or([dmg(0.55), pull({ who: 'self', type: '공격' }), ifStreak(2, '공격'), stk(K, 1)]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 디아나(왕년) — 두 얼굴형 · 딜러 · 냉정 · 엘다인. 도끼(찢기 — 넓게)와 맨주먹(부순다 — 좁고 세게)을 손으로 오간다
// 원작: 고학년 「부순다」로 맨주먹(해제 불가) · 저학년 「찢는다」로만 끝 · 단순하게 · 바위 던지기
// 시동: u3 거르지 않는 수련(개전 강화)
// ════════════════════════════════════════════════════════════════════
function dianaOld(j) {
  const H = '디아나_왕년', K = '백수공권', F = '디아나_왕년_맨주먹';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '도끼 없이 맨주먹으로 모으는 기세', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }], onMax: { empower: 'next', ratio: 0.4 } };
  const fm = h.forms[0];
  fm.passives = [
    { name: '되찾는 숨', when: { on: 'turnEnd' }, fx: [heal(0.15)] },
    { name: '되찾는 숨', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
  ];
  fm.bonus = [{ card: `${H}_u1`, fx: [formEnd] }];
  setCards(j, [
    // 도끼 얼굴 — 적 전체 찢기, 맨주먹이면 도끼로 돌아온다(변신 덤 formEnd)
    U(H, 1, '찢기', 1, '공격', [dmg(0.75, EA), st('균열', 2, EA)], [
      'A', ['D', 'turnStart', [st('균열', 1, EA)]],
      Or([dmg(0.65, EA), per(K), dmg(0.16, EA)]),
      Or([dmg(0.6, EA), st('균열', 2, EA), srch({ type: '공격' })]),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 부순다: 백수공권을 다 털어 비례, 맨주먹으로
    U(H, 2, '도끼 부수기', 1, '공격', [per(K, { n: 1 }), dmg(0.55), spendAll(K), form(F)], [
      'A',
      ['C', [tough(1)]],
      Or([per(K, { n: 1 }), dmg(0.4, EA), spendAll(K), form(F)]),
      Or([per(K, { n: 1 }), dmg(0.45), tough(1), form(F)]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    U(H, 3, '거르지 않는 수련', 1, '강화', [stk(K, 1), pw('turnStart', [stk(K, 1)])], [
      Or([stk(K, 2), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), sh(0.8), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null, ['개전']),
    // 원작 자유 — 바위 던지기(적 전체 + 둔화)
    U(H, 4, '바위 던지기', 1, '공격', [dmg(0.8, EA), st('둔화', 1, EA)], [
      'A', 'B',
      Or([dmg(1.2), tough(1), st('둔화', 1)]),
      Or([dmg(0.7, EA), ifHand(1), stk(K, 2)]),
      'Hd',
    ], bl('power', { tags: ['보존'] }, [stk(K, 1)])),
    // 갈래 부품 — 단순하게(0): 손패 하나 버리고 백수공권
    U(H, 5, '도끼 내려놓기', 0, '스킬', [disc(1), stk(K, 2)], [
      'A', 'B',
      Or([stk(K, 1), ifHand(1), draw(2)]),
      Or([disc(1), stk(K, 2), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([{ k: 'discard', all: true }, stk(K, 3)]),
    ], bl('draw', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 죠안 — 두 얼굴형 · 서포터 · 우울 · 엘다인. 평소엔 빵을 지어 동료에게(주인 바꾸기), 지을수록 어지럽다 — 꿈결 형상에 들면 공격이 파티 회복
// 원작: 고학년 꿈결 형상(20초 · 해제 불가) 동안 평타가 범위 사슬 + 파티 회복 · 오병이어 · 분산
// 시동: u1 빵이 있으라(0코)
// ════════════════════════════════════════════════════════════════════
function joanne(j) {
  const H = '죠안', K = '형상', F = '죠안_꿈결', BREAD = '죠안_bread', DIZZY = '죠안_dizzy';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '꿈 계시를 받던 사제의 모습이 차오르는 정도', carrier: 'self', cap: 3, onMax: { empower: 'any', ratio: 0.5, consume: true } };
  h.keywords = [{ name: '반죽', desc: '이번 턴 빵을 지은 횟수', carrier: 'self', cap: 9, endClear: true }];
  h.passives = [
    pas('빵 짓는 사제', 'make', [stk(K, 1)], { limit: 2 }),
    pas('주교의 경전', 'lowHp', [st('피해 감소', 2), heal(1.5)], { when: { pct: 0.3 } }),   // 3단계 손질: 회복 1.0 → 1.5
  ];
  const fm = h.forms[0];
  Object.assign(fm, {
    desc: '꿈속 계시를 받던 때의 모습 — 휘두르는 사슬이 그대로 파티를 감싸는 형상',
    turns: 3, replace: false,   // 3단계 손질(2026-10-09): 꿈결 2턴 → 3턴(엘다인 하한 아래)
    cards: { [`${H}_s1`]: `${H}_f1` },
    bonus: [{ card: `${H}_u1`, fx: [exile('hand', { all: true, tag: DIZZY })] }, { type: '공격', fx: [heal(0.25)] }],
    passives: [], off: [stk(K, 1)],
  });
  const f1 = j.cards.find(c => c.id === `${H}_f1`);
  Object.assign(f1, { name: '꿈결의 사슬', type: '공격', fx: [dmg(0.65, EA), heal(0.45)], blurb: '꿈결 형상의 사슬이 적을 휘감고 파티를 감싸 줍니다' });
  const tokens = [
    token(BREAD, '빵', H, '스킬', [heal(0.6), sh(0.7)],   // 3단계 손질(2026-10-09): 회복 0.5 → 0.6 · 실드 0.4 → 0.7
    { blurb: '말 한마디로 지어 낸 빵 — 받은 아군의 손으로 나갑니다' }),
    token(DIZZY, '현기증', H, '스킬', [{ k: 'when', on: 'handEnd' }, nextAp(-1)], { tags: ['사용 불가', '증발'], blurb: '빵을 지을수록 핑 도는 머리' }),
  ];
  const bread = (v = 1) => make(BREAD, v, { owner: 'other' });
  setCards(j, [
    // 시동 — 빵 하나(주인: 다음 아군) + 현기증 하나(꿈결이면 지운다 — 변신 덤)
    U(H, 1, '빵이 있으라', 0, '스킬', [bread(), stk('반죽', 1), ifStack('반죽', 3), make(DIZZY, 1)], [
      Or([bread(2), stk('반죽', 1), ifStack('반죽', 3), make(DIZZY, 1)]),
      Or([bread(2), draw(2)], { tags: ['신속'] }),
      Or([make(BREAD, 2), sh(1.0), stk(K, 1)]),
      Or([bread(2), stk(K, 1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([bread(2), make(DIZZY, 2)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null),
    // 쓰기 — 꿈결의 계시: 적 전체 + 실드, 꿈결 형상으로(2턴)
    // 3단계 손질(2026-10-09): 꿈결 진입 2코 → 1코(실드 1.5 → 1.1) — 엘다인 하한 아래(봇이 2코 진입을 늦게 골랐다)
    U(H, 2, '꿈결의 계시', 1, '스킬', [dmg(0.7, EA), sh(1.1), form(F)], [
      'A', 'B',
      Or([dmg(1.0, EA), sh(1.4), form(F)]),
      Or([dmg(0.9, EA), form(F), draw(2)]),
      'Hn',
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 오병이어: 빵 둘, 손의 현기증 전부 소멸
    U(H, 3, '오병이어', 1, '스킬', [bread(2), exile('hand', { all: true, tag: DIZZY })], [
      Or([bread(3), exile('hand', { all: true, tag: DIZZY })]),
      Or([bread(1), exile('hand', { all: true, tag: DIZZY })], { cost: 0 }),
      Or([bread(2), stk(K, 1), exile('hand', { all: true, tag: DIZZY })]),
      Or([bread(1), exile('hand', { all: true, tag: DIZZY }), pw('turnStart', [bread(1)])], { power: true }),
      Or([bread(3), make(DIZZY, 1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
    // 원작 자유 — 교리를 행하고(강화): 사기 · 빵을 지을 때마다 실드
    U(H, 4, '교리를 행하고', 1, '강화', [st('사기', 1), pw('make', [sh(0.5)], { limit: 1 })], [
      Or([st('사기', 1), pw('make', [sh(0.7)], { limit: 1 })]),
      Or([st('사기', 1), pw('make', [sh(0.3)], { limit: 1 })], { cost: 0 }),
      Or([st('사기', 1), pw('make', [stk(K, 1), sh(0.3)], { limit: 1 })]),
      Or([st('사기', 1), bread(), pw('make', [sh(0.4)], { limit: 1 })]),
      Or([st('사기', 2), nextAp(-1), pw('make', [sh(0.5)], { limit: 1 })]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('defUp', 'draw', [stk(K, 1)])),
    // 만들기 둘째 — 사슬 심판: 방어 기반 + 형상
    U(H, 5, '사슬 심판', 1, '공격', [ddef(0.75), stk(K, 1)], [
      'A', ['D', 'make', [ddef(0.25)], { limit: 1 }],
      Or([ddef(0.5, EA), stk(K, 1), bread()]),
      Or([ddef(0.6), stk(K, 1), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 5. 캬롯 — 예약형 · 서포터 · 순수. 아군에게 「씨앗」을 심고 여물 때까지 기다린다 — 일찍 캐면 남은 칸만큼 작다. 예약형 중 유일하게 아군에게
// 원작: 수액이 잠시 뒤 아군에게 떨어져 피해↑ · 받는 피해↓ · 저학년 탄산수액 · 고학년 수액 펌프
// 시동: u2 탄산수액 발사
// ════════════════════════════════════════════════════════════════════
function carrot(j) {
  const H = '캬롯', K = '씨앗';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '아군 곁에 심어 기다리는 탄산수액', carrier: 'hero', cap: 2, reserve: true, decay: 1,
    rules: [{ name: '수확', when: { on: 'stackGone', id: K, decay: true }, fx: [dmod(0.4, ALLY, 2), sh(1.0), draw(1)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('일등 정원사', 'play', [stk(K, 1, STRONG)], { when: { type: '스킬' }, limit: 2 }),
    pas('쑥쑥 자라라!', 'reserveFire', [draw(1)], { limit: 1 }),
  ];
  h.ult.fx = [dmg(1.5, EA), ap(2), stk(K, 1, ALLIES)];
  const SPROUT = '캬롯_sprout';
  for (const c of j.cards.filter(c => c.token)) c.fx = c.fx.map(f => (f.k === 'stack' ? stk(K, 1, c.id === SPROUT ? STRONG : ALLIES) : f));
  setCards(j, [
    // 원작 자유 둘 — 텃밭 돌보기: 회복 + 아군 전원에게 씨앗 1
    U(H, 1, '당근 신선도 유지', 1, '스킬', [heal(0.7), stk(K, 1, ALLIES)], [
      'A', 'B',
      Or([sh(1.0), stk(K, 1, ALLIES)]),
      Or([heal(0.6), stk(K, 1, ALLIES), pw('turnStart', [stk(K, 1, STRONG)])], { power: true }),
      'Hd',
    ], bl('heal', 'draw', [stk(K, 1, STRONG)])),
    // 시동 — 아군 하나에게 씨앗 2(2턴 뒤 여문다) + 드로우
    U(H, 2, '탄산수액 발사', 1, '스킬', [stk(K, 2, ALLY), draw(1)], [
      Or([stk(K, 2, ALLY), draw(1), sh(0.5)]),
      Or([stk(K, 1, ALLY), draw(1)], { cost: 0 }),
      Or([stk(K, 3, ALLY), srch()]),
      Or([stk(K, 2, ALLY), draw(1), pw('reserveFire', [sh(0.4)], { limit: 1 })], { power: true }),
      Or([stk(K, 3, ALLY), draw(2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null),
    // 쓰기 — 일찍 캐기(0): 고른 아군의 씨앗을 지금 수확 — 남은 1칸당 효과 -30%
    U(H, 3, '일찍 캐기', 0, '스킬', [ripen(K, 0.3, ALLY), sh(0.3)], [
      Or([ripen(K, 0.3, ALLY), sh(0.6)]),
      Or([ripen(K, 0.3, ALLIES), sh(0.3)], { tags: ['신속'] }),
      Or([ripen(K, 0.3, ALLY), draw(1)]),
      Or([ripen(K, 0.3, ALLY), sh(0.3), srch()]),
      Or([ripen(K, 0.3, ALLIES), sh(0.9), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('guard', { tags: ['보존'] }, [stk(K, 1, ALLY)])),
    // 갈래 부품 — 특제 영양제: 재촉 1(파티의 모든 예약 — 마카샤 시계 · 아멜리아 예약도) + 실드
    U(H, 4, '특제 영양제', 1, '스킬', [hasten(1), sh(0.9)], [
      'A', 'B',
      Or([hasten(1), heal(0.6), stk(K, 1, STRONG)]),
      Or([sh(0.6), pw('turnStart', [hasten(1)], { limit: 1 })], { power: true }),
      Or([hasten(2), sh(1.1), disc(1)]),
    ], bl('guard', 'ap', [stk(K, 1, ALLY)])),
    // 원작 자유 — 사탕수수 몽둥이: 적 전체 + 가장 센 아군에게 씨앗
    U(H, 5, '사탕수수 몽둥이', 1, '공격', [dmg(0.75, EA), make(SPROUT, 1)], [
      'A', ['D', 'turnStart', [make(SPROUT, 1)]],
      Or([dmg(1.1), st('약화', 2), make(SPROUT, 1)]),
      Or([dmg(0.7, EA), make(SPROUT, 1), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'cost', [make(SPROUT, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 마카샤 — 예약형 · 딜러 · 활발. 적에게 운명의 시계를 걸고 그날을 앞당긴다 — 당기면 남은 칸만큼 덜 아프고, 먼저 쓰러지면 결말이 번진다
// 원작: 운명의 시계(24초 뒤 피해, 먼저 쓰러지면 전체로) · 고학년이 시계를 줄인다 · 닭다리 집
// 시동: u1 언젠가 일어날 일
// ════════════════════════════════════════════════════════════════════
function makasha(j) {
  const H = '마카샤', K = '운명의 시계';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '두 칸 뒤에 결말이 오는 시곗바늘', carrier: 'enemy', cap: 2, decay: 1, reserve: true,
    rules: [{ name: '정해진 결말', when: { on: 'stackGone', id: K, decay: true }, fx: [ddef(2.4), st('취약', 1)] }],
  };
  h.passives = [
    pas('번지는 결말', 'kill', [per(K), ddef(0.5, EA)]),
    pas('이미 본 결말', 'fightStart', [stk(K, 3, TOP)]),
  ];
  setCards(j, [
    // 시동 — 시계 셋 + 드로우
    U(H, 1, '언젠가 일어날 일', 1, '스킬', [stk(K, 3, E1), draw(1)], [
      Or([stk(K, 3, E1), draw(1), ddef(0.5)]),
      Or([stk(K, 3, E1)], { cost: 0 }),
      Or([stk(K, 2, EA), draw(1), ddef(0.4, EA)]),
      Or([stk(K, 3, E1), srch(), ddef(0.4)]),
      Or([stk(K, 3, E1), draw(2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null),
    // 쓰기 — 적혀 있던 결말: 그 적 시계를 지금 터뜨린다(남은 1칸당 -25%) + 방어 기반
    U(H, 2, '적혀 있던 결말', 1, '공격', [ddef(0.45), ripen(K, 0.25)], [
      'A', 'B',
      Or([ddef(0.45, EA), ripen(K, 0.3, EA)]),
      Or([ddef(0.4), ripen(K, 0.25), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1, E1)])),
    // 갈래 부품 — 예언 떠벌리기(0): 재촉 1 — 파티의 모든 예약(캬롯 씨앗 · 아멜리아 예약도)
    U(H, 3, '예언 떠벌리기', 0, '스킬', [hasten(1), draw(1)], [
      Or([hasten(1), draw(1), ddef(0.3)]),
      Or([hasten(1), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 1, EA), draw(1), ddef(0.3, EA)]),
      Or([hasten(1), draw(1), pw('reserveFire', [draw(1)], { limit: 1 })], { power: true }),
      Or([hasten(2), draw(2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('ap', { tags: ['보존'] }, [stk(K, 1, E1)])),
    // 원작 자유 둘 — 책략관의 탁상: 시계 1개당(쓰지 않음)
    U(H, 4, '책략관의 탁상', 1, '공격', [ddef(0.6), per(K), ddef(0.28)], [
      'A', 'B',
      Or([ddef(0.95), stk(K, 2, E1)]),
      Or([ddef(0.5), per(K), ddef(0.25), pw('reserveFire', [ddef(0.3, EA)], { limit: 1 })], { power: true }),
      'Hn',
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 원작 자유 — 닭다리 집 바바: 적 전체 + 시계 1씩
    U(H, 5, '닭다리 집 바바', 1, '공격', [ddef(0.55, EA), stk(K, 1, EA)], [
      'A',
      ['D', 'turnStart', [stk(K, 1, E1)]],
      Or([sh(1.4), stk(K, 2, E1)]),
      Or([ddef(0.5, EA), stk(K, 1, EA), srch({ type: '공격' })]),
      'Hd',
    ], bl('guard', 'draw', [stk(K, 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 벨벳 — 격파형 · 탱커 · 냉정. 맞아서 쌓은 근력으로 도끼를 돌려 여러 적의 강인도를 함께 깎는다
// 원작: 도발 + 받는 피해 감소 · 고학년 회전 11타 넉백 · 솔즙 무료 나눔
// 시동: u1 이두근 펌핑(개전 강화 — 적이 안 쳐도 근력이 차는 대안)
// ════════════════════════════════════════════════════════════════════
function velvet(j) {
  const H = '벨벳', K = '근력 강화';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '마법으로 부풀린 힘의 마녀의 근육', carrier: 'self', cap: 5, per: [{ stat: 'taken', v: -0.04 }] };
  h.passives = [
    pas('스스로 지키는 힘', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
    pas('진영을 흔드는 근육', 'break', [gauge(20), stk(K, 1)], { limit: 1 }),
  ];
  const bz = ['분쇄'];
  setCards(j, [
    U(H, 1, '이두근 펌핑', 1, '강화', [stk(K, 1), sh(0.6), pw('turnStart', [stk(K, 1)])], [
      Or([stk(K, 1), sh(0.9), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), sh(0.9), pw('guard', [stk(K, 1)], { when: { kind: 'shield' }, limit: 2 })], { tags: ['개전'] }),
      Or([sh(0.8), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), sh(0.6), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null, ['개전']),
    // 만들기 둘째 — 그냥 다 덤벼!: 실드 + 반격 + 근력
    U(H, 2, '그냥 다 덤벼!', 1, '스킬', [sh(1.0), st('반격', 1), stk(K, 1)], [
      'A', ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      Or([sh(1.6), st('피해 감소', 1), stk(K, 2)]),
      Or([sh(1.1), stk(K, 2), srch({ type: '공격' })]),
      'Hn',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 쓰기 — 원심 분리: 근력 1개당 적 전체 방어 기반 1타, 적마다 강인도 1, 근력 전부 소모
    U(H, 3, '원심 분리 펀치', 1, '공격', [per(K, { n: 1 }), ddef(0.24, EA), tough(1, EA), spendAll(K)], [
      'A',
      ['C', [st('둔화', 1, EA)]],
      Or([per(K, { n: 1 }), ddef(0.4), tough(2), spendAll(K)]),
      Or([per(K, { n: 1 }), ddef(0.28, EA), tough(1, EA)]),
      ['Ht', '분쇄'],
    ], bl('power', 'cost', [stk(K, 1)]), bz),
    // 갈래 부품 — 진영 붕괴: 고른 적이 격파 상태면 적 전체 강인도 + 둔화
    U(H, 4, '진영 붕괴', 1, '공격', [ddef(0.7), ifFoe('broken'), tough(1, EA), st('둔화', 1, EA)], [
      'A', 'B',
      Or([ddef(0.5, EA), ifFoe('tough', { pct: 0.5 }), tough(1, EA)]),
      Or([ddef(0.65), ifFoe('broken'), tough(1, EA), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 원작 자유 — 솔즙 무료 나눔(0): 회복 + 근력
    U(H, 5, '솔즙 무료 나눔', 0, '스킬', [heal(0.5), stk(K, 1)], [
      'A', 'B',
      Or([sh(0.6), st('피해 감소', 1)]),
      Or([heal(0.4), stk(K, 1), draw(1)]),
      'Hd',
    ], bl('heal', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 샤샤 — 격파형 · 딜러 · 우울. 텀블러를 열어 가장 단단한 놈을 통째로 — 수압이 넘치면 텀블러가 폭주(손에 폭주 카드)
// 원작: 고학년 HP 가장 높은 적에 물줄기 18타 · 넷째 공격마다 범위 · 「부수적인 피해」
// 시동: u5 텀블러 달래기(옛 시동 u1 투척 → 은총으로)
// ════════════════════════════════════════════════════════════════════
function shasha(j) {
  const H = '샤샤', K = '텀블러 수압', RUN = '샤샤_runaway';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '제멋대로 차오르는 텀블러 물', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }], onMax: { make: RUN, consume: true } };
  h.passives = [
    pas('대용량 텀블러', 'fightStart', [stk(K, 1)]),
    pas('대용량 텀블러', 'play', [stk(K, 1)], { when: { type: '공격' } }),
    pas('남겨 둔 물', 'break', [stk(K, 2)], { when: { mine: true }, limit: 1 }),
  ];
  const tokens = [token(RUN, '텀블러 폭주', H, '공격', [hits(3, 0.45, ER), { k: 'payHp', v: 30 }], { blurb: '꽉 찬 텀블러가 멋대로 터집니다 — 아무 데나 물벼락' })];
  setOpener(j, H, 'u1', 'u5');
  setCards(j, [
    U(H, 1, '텀블러 투척!', 1, '공격', [dmg(0.35, EA, { hits: 2 }), st('약화', 1, EA), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(1.25), tough(1), stk(K, 1)]),
      Or([dmg(0.3, EA, { hits: 2 }), st('약화', 1, EA), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 물줄기 발사: 수압 1개당 HP 가장 높은 적 1타, 다 쓴다. 붕괴: 안 친 적에게 한 번 더
    U(H, 2, '물줄기 발사', 1, '공격', [per(K, { n: 1 }), dmg(0.34, TOP), spendAll(K), ifBreak, dmg(0.7, OTHER)], [
      'A',
      ['C', [tough(1, TOP)]],
      Or([per(K, { n: 1 }), dmg(0.3, TOP), spendAll(K), { k: 'ifBreak', not: true }, stk(K, 2)]),
      Or([per(K, { n: 1 }), dmg(0.42, TOP), ifBreak, srch()]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 갈래 부품 — 부수적인 피해입니다: 적 전체, 고른 적이 격파 상태면 한 번 더 + 둔화
    U(H, 3, '부수적인 피해입니다', 1, '공격', [dmg(0.55, EA), ifFoe('broken'), dmg(0.5, EA), st('둔화', 1, EA)], [
      'A', 'B',
      Or([dmg(1.1), ifFoe('broken'), dmg(0.9), ap(1)]),
      Or([dmg(0.5, EA), pw('break', [dmg(0.35, EA)], { when: { mine: true }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    U(H, 4, '불 하나는 잘 꺼요', 1, '공격', [{ k: 'cleanse', v: 1 }, dmg(0.45, EA), per(K), dmg(0.12, EA)], [
      'A', 'B',
      Or([{ k: 'cleanse', v: 2 }, sh(1.0), dmg(0.45, EA)]),
      Or([{ k: 'cleanse', v: 1 }, dmg(0.6, EA), srch({ type: '공격' })]),
      'Hd',
    ], bl('power', { tags: ['보존'] }, [stk(K, 1)])),
    // 시동 — 텀블러 달래기: 실드 + 수압 2
    U(H, 5, '텀블러 달래기', 1, '스킬', [sh(1.0), stk(K, 2)], [
      'A', 'B',
      Or([dmg(0.7), stk(K, 2)]),
      Or([sh(0.8), stk(K, 2), srch({ type: '공격' })]),
      'Hd',
    ], null),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 9. 이프리트 — 소멸형 · 딜러 · 광기. 누가 태운 것이든 장작으로 — 남의 쓰레기(버린 더미의 동료 카드 · 상태 · 저주)를 태워 캠프파이어
// 원작: 화염 지대 · 캠프파이어 11타 + 화상(장작 · 캠프파이어는 설정) · 애완 돌 · 숙적 은박지
// 시동: u1 땔감 던지기(0코 · 소멸 — 니콜 「재촬영 파이어」(손패 태우기)와 가름)
// ════════════════════════════════════════════════════════════════════
function ifrit(j) {
  const H = '이프리트', K = '장작', EMBER = '이프리트_ember';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '소멸로 태워 넣은 땔감', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.08 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('장작 넣기', 'exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 3 }),
    pas('불의 검', 'play', [st('고통', 2, E1)], { when: { type: '공격' }, limit: 2 }),
  ];
  setCards(j, [
    // 시동 — 땔감 던지기(0 · 소멸): 버린 더미의 동료 카드 1장 소멸, 장작 1, 드로우 1
    U(H, 1, '땔감 던지기', 0, '스킬', [exile('discard', { n: 1, who: 'other' }), stk(K, 1), draw(1)], [
      Or([exile('discard', { n: 1, who: 'other' }), stk(K, 2), draw(1)], { tags: ['소멸'] }),
      Or([exile('discard', { n: 2 }), stk(K, 2), draw(1)], { tags: ['소멸'] }),
      Or([exile('discard', { n: 1, who: 'other' }), stk(K, 1), dmg(0.5, EA)], { tags: ['소멸'] }),
      Or([exile('discard', { n: 1, who: 'other' }), stk(K, 2), dmg(0.4, ER)], { tags: ['소멸'] }),
      Or([exile('discard', { n: 2, who: 'other' }), stk(K, 1), sh(0.6)], { tags: ['소멸', '신속'] }),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null, ['소멸']),
    // 쓰기 — 새까만 대검: 장작 1개당, 전부 소모
    U(H, 2, '새까만 대검', 1, '공격', [dmg(0.8), per(K), dmg(0.22), spendAll(K)], [
      'A', ['D', 'exhaust', [dmg(0.3)], { when: { who: 'any' }, limit: 1 }],
      Or([dmg(0.6, EA), per(K), dmg(0.15, EA), spendAll(K)]),
      Or([dmg(0.8), per(K), dmg(0.2), srch()]),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 원작 자유 — 모닥불 피우기(2): 적 전체 + 이번 전투 소멸 1장당 · 고통
    U(H, 3, '모닥불 피우기', 2, '공격', [dmg(0.9, EA), perGone(), dmg(0.1, EA), st('고통', 2, EA)], [
      'A', 'B',
      Or([dmg(1.6), perGone(), dmg(0.2), st('고통', 3)]),
      Or([dmg(0.95, EA), perGone(), dmg(0.1, EA), pw('exhaust', [dmg(0.25, ER)], { when: { who: 'any' }, limit: 2 })], { power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 만들기 — 애완 돌 데우기: 불씨 둘 + 장작
    U(H, 4, '애완 돌 데우기', 1, '스킬', [make(EMBER, 2), stk(K, 1)], [
      Or([make(EMBER, 3), stk(K, 1)]),
      Or([make(EMBER, 1), stk(K, 1)], { cost: 0 }),
      Or([sh(0.8), make(EMBER, 2), stk(K, 2)]),
      Or([make(EMBER, 1), pw('turnStart', [make(EMBER, 1)])], { power: true }),
      Or([make(EMBER, 3), stk(K, 2), nextAp(-1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
    // 갈래 부품 — 쓰레기 정화: 버린 더미의 상태 · 저주 전부 소멸 + 실드
    U(H, 5, '쓰레기 정화', 1, '스킬', [exile('discard', { all: true, type: '상태' }), exile('discard', { all: true, type: '저주' }), sh(0.9)], [
      Or([exile('discard', { all: true, type: '상태' }), exile('discard', { all: true, type: '저주' }), sh(1.2)]),
      Or([exile('discard', { all: true, type: '상태' }), sh(0.7)], { cost: 0 }),
      Or([exile('discard', { n: 2, who: 'other' }), dmg(0.6, EA)]),
      Or([exile('discard', { all: true, type: '상태' }), sh(0.8), srch()]),
      Or([exile('hand', { all: true, type: '상태' }), exile('discard', { all: true, type: '상태' }), dmg(0.6, EA)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 10. 니콜 — 소멸형 · 딜러 · 냉정. 제 손으로 NG 컷을 만들어 태우고, 이번 전투에 소멸한 「내 카드」 만큼 마지막 한 장에 몰아준다
// 원작: 주연 배우 소환 · 「재촬영」 돌진 · NG · 컷 어휘
// 시동: u3 NG 컷(옛 시동 u1 액션! → 은총으로)
// ════════════════════════════════════════════════════════════════════
function nicole(j) {
  const H = '니콜', K = '테이크', OK = '니콜_t1', NG = '니콜_ng';
  const h = j.heroes[0];
  h.passives = [
    pas('레디 액션', 'exhaust', [stk(K, 1)], { limit: 2 }),
    pas('느와르 니콜', 'kill', [draw(1)], { limit: 1 }),
  ];
  const tokens = [token(NG, 'NG 필름', H, '스킬', [draw(1), { k: 'when', on: 'burn' }, stk(K, 1)], { blurb: '다시 갑니다 — 쓰다 버린 필름 한 롤' })];
  setOpener(j, H, 'u1', 'u3');
  setCards(j, [
    U(H, 1, '액션!', 0, '스킬', [stk(K, 2)], [
      Or([stk(K, 3)]),
      Or([stk(K, 2)], { tags: ['신속'] }),
      Or([stk(K, 1), make(NG, 1)]),
      Or([stk(K, 1), srch()]),
      Or([stk(K, 3), burn(1), draw(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('draw', 'ap', [stk(K, 1)])),
    // 쓰기 — 다시 갑니다(2 · 소멸): 이번 전투에 소멸한 자신의 카드 1장당
    U(H, 2, '다시 갑니다', 2, '공격', [dmg(1.3), perGone('self'), dmg(0.3)], [
      'A', ['D', 'exhaust', [dmg(0.5)], { limit: 2 }],
      Or([dmg(0.9, EA), perGone('self'), dmg(0.2, EA)]),
      Or([dmg(1.1), perGone('self'), dmg(0.3), srch()]),
      ['Ht', '소멸'],
    ], bl('power', 'cost', [stk(K, 1)]), ['소멸']),
    // 시동 — NG 컷: 피해 + 손에 NG 필름
    U(H, 3, 'NG 컷', 1, '공격', [dmg(0.9), make(NG, 1)], [
      'A', 'B',
      Or([dmg(0.6, EA), make(NG, 1)]),
      Or([dmg(0.8), make(NG, 1), srch()]),
      Or([dmg(1.3), make(NG, 2), disc(1)]),
    ], null),
    // 원작 자유 — 디렉터스 컷(보존): 소멸한 자신의 카드 1장당
    U(H, 4, '디렉터스 컷', 1, '공격', [dmg(0.7), perGone('self'), dmg(0.18)], [
      'A', 'B',
      Or([dmg(0.5), perGone('self'), dmg(0.12), make(NG, 1)]),
      Or([dmg(0.6), perGone('self'), dmg(0.15), pw('exhaust', [dmg(0.3)], { limit: 1 })], { power: true }),
      ['Ht', '보존'],
    ], bl('power', 'weakSpot', [stk(K, 1)]), ['보존']),
    // 갈래 부품 — 재촬영 파이어(0 · 소멸): 손패 1장 태우고 1장 뽑기
    U(H, 5, '재촬영 파이어', 0, '스킬', [burn(1), draw(1)], [
      Or([burn(1), draw(2)], { tags: ['소멸'] }),
      Or([burn(1), draw(1)], { tags: ['소멸', '신속'] }),
      Or([burn(1), stk(K, 1), dmg(0.5)], { tags: ['소멸'] }),
      Or([burn(1), srch(), stk(K, 1)], { tags: ['소멸'] }),
      Or([burn(2), draw(2)], { tags: ['소멸'] }),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('draw', 'ap', [stk(K, 1)]), ['소멸']),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 11. 쵸피 — 성장형 · 딜러 · 우울. 강한 상대를 무너뜨린 「그 카드」가 자란다(엘리트 · 보스면 더) — 같은 훈련을 되풀이해 자라는 게 아니다
// 원작: 1성 · 소리 지르기 · 도끼, 성장 근거는 설정(수련 · 질려서 모험)
// 시동: u5 장작 지게(옛 시동 u4 → 은총으로)
// ════════════════════════════════════════════════════════════════════
function choppy(j) {
  const H = '쵸피', K = '수련', LOG = '쵸피_log', L = '배움';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '하루도 거르지 않는 장작 패기', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }], onMax: { empower: 'next', ratio: 0.4 },
    rules: [{ name: '하루도 거르지 않는 수련', when: { on: 'turnStart' }, fx: [stk(K, 1)] }] };
  h.passives = [
    pas('강한 사람은 모두 스승', 'break', [stk(K, 2)], { limit: 1 }),
    pas('새로운 모험', 'grow', [draw(1)], { limit: 1 }),
  ];
  h.ult.fx = h.ult.fx.map(f => f);
  setOpener(j, H, 'u4', 'u5');
  setCards(j, [
    U(H, 1, '퍄오오~', 1, '공격', [dmg(0.8, EA), stk(K, 1)], [
      'A', ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(1.1), st('약화', 2), stk(K, 1)]),
      Or([dmg(0.7, EA), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 결대로 쪼개기(2 · 약점 공격): 수련 1개당, 전부 소모
    U(H, 2, '결대로 쪼개기', 2, '공격', [dmg(1.9), per(K), dmg(0.27), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.3, EA), per(K), dmg(0.18, EA), spendAll(K)]),
      Or([dmg(1.7), per(K), dmg(0.25), ifKill, cs(L, 1, { max: 10 })]),
      ['Ht', '약점 공격'],
    ], bl('power', 'cost', [stk(K, 1)]), ['약점 공격']),
    // 갈래 부품 — 사부님 찾기: 「배움」 1당 1타(최소 3). 처치: 이 카드 배움 +1 · 엘리트 · 보스면 +2 더(판 내내)
    U(H, 3, '사부님 찾기', 1, '공격', [perCs(L, { n: 3 }), dmg(0.3), ifKill, cs(L, 1, { max: 10 }), killElite, cs(L, 2, { max: 10 })], [
      Or([perCs(L, { n: 3 }), dmg(0.4), ifKill, cs(L, 1, { max: 10 }), killElite, cs(L, 2, { max: 10 })]),
      Or([perCs(L, { n: 2 }), dmg(0.3), ifKill, cs(L, 1, { max: 10 }), killElite, cs(L, 2, { max: 10 })], { cost: 0 }),
      Or([dmg(1.0), ifBreak, cs(L, 1, { max: 10 }), killElite, cs(L, 2, { max: 10 })]),
      Or([perCs(L, { n: 3 }), dmg(0.28), ifKill, cs(L, 1, { max: 10 }), srch()]),
      Or([perCs(L, { n: 3 }), dmg(0.42), ifKill, cs(L, 1, { max: 10 }), nextAp(-1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('power', 'weakSpot', [stk(K, 1)])),
    // 원작 자유 — 새로운 수련(0): 수련 + 드로우
    U(H, 4, '새로운 수련', 0, '스킬', [stk(K, 1), draw(1)], [
      Or([stk(K, 2), draw(1)]),
      Or([stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([sh(0.7), stk(K, 2)]),
      Or([stk(K, 2), drawType('공격')]),
      Or([stk(K, 2), draw(2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('draw', 'ap', [stk(K, 1)])),
    // 시동 — 장작 지게: 쪼갠 장작 생성 + 수련 + 실드
    U(H, 5, '장작 지게', 1, '스킬', [make(LOG, 1), stk(K, 1), sh(0.8)], [
      Or([make(LOG, 1), stk(K, 1), sh(1.1)]),
      Or([make(LOG, 1), stk(K, 1)], { cost: 0 }),
      Or([make(LOG, 2), sh(0.5)]),
      Or([make(LOG, 2), stk(K, 1), srch()]),
      Or([make(LOG, 2), stk(K, 2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 12. 레비(졸업) — 성장형 · 탱커 · 활발. 전투마다 쌓은 노력(이번 전투에 쌓은 패기)이 문턱을 넘으면 논문이 게재된다(판 내내)
// 원작: 롤모델(가장 센 아군)에게 무적 + 피해↑ · 각성 드링크 · 보조 탱커 · 졸업 논문(서사)
// 시동: u1 출근 도장
// ════════════════════════════════════════════════════════════════════
function leviGrad(j) {
  const H = '레비_졸업', K = '신입의 패기', P = '게재';
  const h = j.heroes[0];
  h.keyword = { ...h.keyword, rules: [], onMax: { empower: 'next', ratio: 0.4 } };
  delete h.keyword.rules;
  setCards(j, [
    // 시동 — 출근 도장: 패기 2 + 실드
    U(H, 1, '출근 도장', 1, '스킬', [stk(K, 2), sh(0.7)], [
      'A', 'B',
      Or([stk(K, 1), ddef(0.5), sh(0.4)]),
      Or([stk(K, 2), sh(0.5), srch()]),
      Or([stk(K, 3), sh(1.0), { k: 'payHp', v: 40 }]),
    ], null),
    U(H, 2, '패기 가득한 인재', 1, '공격', [ddef(0.12, EA, { hits: 3 }), st('약화', 1, EA), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([ddef(0.6), st('약화', 2), stk(K, 1)]),
      Or([ddef(0.13, EA, { hits: 3 }), st('약화', 1, EA), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 정시 퇴근: 패기 1개당 실드, 전부 소모(이번 전투에 쌓은 양은 남는다)
    U(H, 3, '정시 퇴근', 1, '스킬', [sh(0.7), per(K), sh(0.31), spendAll(K)], [
      'A', 'B',
      Or([heal(0.9), per(K), heal(0.38), spendAll(K)]),
      Or([sh(0.6), per(K), sh(0.3), pw('spend', [sh(0.3)], { when: { id: K }, limit: 1 })], { power: true }),
      'Hn',
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 원작 자유 — 롤모델 찾기: 피해 감소 + 파티의 다음 카드 강화
    U(H, 4, '롤모델 찾기', 1, '스킬', [st('피해 감소', 1), empower(0.4, 'any'), stk(K, 1)], [
      Or([st('피해 감소', 1), empower(0.55, 'any'), stk(K, 1)]),
      Or([empower(0.35, 'any'), stk(K, 1)], { cost: 0 }),
      Or([sh(0.9), dmod(0.2, STRONG), stk(K, 1)]),
      Or([st('피해 감소', 1), empower(0.55, 'any'), draw(1, { who: 'other' })]),
      Or([st('피해 감소', 2), empower(0.8, 'any'), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('defUp', { tags: ['보존'] }, [stk(K, 1)])),
    // 갈래 부품 — 논문 초안: 게재 1당 실드. 이번 전투에 쌓은 패기가 6 이상이면 이 카드 게재 +1(판 내내, 최대 5)
    U(H, 5, '논문 초안', 1, '스킬', [sh(0.7), perCs(P), sh(0.3), ifGained(K, 6), cs(P, 1, { max: 5 })], [
      Or([sh(0.9), perCs(P), sh(0.35), ifGained(K, 6), cs(P, 1, { max: 5 })]),
      Or([sh(0.5), perCs(P), sh(0.25), ifGained(K, 6), cs(P, 1, { max: 5 })], { cost: 0 }),
      Or([ddef(0.5), perCs(P), ddef(0.2), ifGained(K, 5), cs(P, 1, { max: 5 })]),
      Or([sh(0.6), perCs(P), sh(0.3), ifGained(K, 6), srch()]),
      Or([sh(1.0), perCs(P), sh(0.4), ifGained(K, 8), cs(P, 2, { max: 5 })]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('guard', 'draw', [stk(K, 2)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 13. 쥬비 — 소환형 · 딜러 · 활발. 작은 쥬비(벌)를 여럿 불러 떼로 덮친다 — 공격하면 벌이 따라 치고(한 카드에 5대까지), 파티가 크게 맞으면 흩어진다
// 원작: 친구 꿀벌을 불러 가장 약한 적을 침 · 고학년이 자신과 벌 피해↑ · 약점(큰 피해에 흩어짐)
// 시동: u2 대장 쥬비 호출(개전 강화)
// ════════════════════════════════════════════════════════════════════
function jubee(j) {
  const H = '쥬비', K = '벌';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '따라다니며 쏘는 꿀벌 떼', carrier: 'self', cap: 6,
    rules: [
      { name: '꿀벌 친구', when: { on: 'fightStart' }, fx: [stk(K, 3)] },
      { name: '꿀벌 친구', when: { on: 'turnStart' }, fx: [stk(K, 1)] },
    ],
  };
  h.passives = [
    pas('벌떼 습격', 'play', [summon(K, 0.5, { max: 5, target: 'lowEnemy' })], { when: { type: '공격' } }),
    pas('흩어지는 벌', 'hurt', [spendN(K, 2)], { when: { pct: 0.15 }, limit: 1 }),
  ];
  setCards(j, [
    U(H, 1, '친구 꿀벌', 1, '공격', [dmg(0.9), stk(K, 2)], [
      'A', ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.6, EA), stk(K, 2)]),
      Or([dmg(0.8), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    U(H, 2, '대장 쥬비 호출', 1, '강화', [stk(K, 2), pw('turnStart', [stk(K, 1)])], [
      Or([stk(K, 3), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 3), pw('summonAct', [stk(K, 1)], { when: { id: K, kind: 'atk' }, limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 2), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 4), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null, ['개전']),
    // 쓰기 — 벌떼 돌격: 벌이 모두 따라 친다(무작위 적 · 5대까지), 벌 전부 소모
    U(H, 3, '벌떼 돌격', 1, '공격', [dmg(0.5), summon(K, 0.42), spendAll(K)], [
      'A', 'B',
      Or([dmg(0.4), summon(K, 0.4, { target: EA, max: 3 }), spendAll(K)]),
      Or([dmg(0.5), summon(K, 0.35), srch()]),
      'Hn',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 원작 자유 — 복수의 벌떼: 고른 적에게 벌이 몰려간다 + 실드
    U(H, 4, '복수의 벌떼', 1, '공격', [summon(K, 0.32, { target: E1 }), sh(0.5)], [
      'A', 'B',
      Or([summon(K, 0.3, { target: E1 }), st('약화', 1)]),
      Or([summon(K, 0.3, { target: E1 }), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 갈래 부품 — 꿀벌 훈련(강화): 벌이 따라 치면 하나가 돌아온다
    U(H, 5, '꿀벌 훈련', 1, '강화', [stk(K, 2), pw('summonAct', [stk(K, 1)], { when: { id: K, kind: 'atk' }, limit: 1 })], [
      Or([stk(K, 3), pw('summonAct', [stk(K, 1)], { when: { id: K, kind: 'atk' }, limit: 1 })]),
      Or([stk(K, 1), pw('summonAct', [stk(K, 1)], { when: { id: K, kind: 'atk' }, limit: 1 })], { cost: 0 }),
      Or([stk(K, 3), pw('summonAct', [stk(K, 1), sh(0.3)], { when: { id: K, kind: 'atk' }, limit: 1 })]),
      Or([stk(K, 2), srch(), pw('summonAct', [stk(K, 1)], { when: { id: K, kind: 'atk' }, limit: 1 })]),
      Or([stk(K, 4), disc(1), pw('summonAct', [stk(K, 1)], { when: { id: K, kind: 'atk' }, limit: 1 })]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('draw', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 14. 모모 — 소환형 · 딜러 · 활발. 분신을 불러 대신 맞게 하고(분신 하나가 두 대를 받는다), 깨질 때 터뜨린다 — 소환물의 「끝」을 쓴다
// 원작: 분신 소환(3회 맞거나 시간 끝나면 깨지며 범위 + 감전) · 번개 수리검 · 카게닌자
// 시동: u3 환영술
// ════════════════════════════════════════════════════════════════════
function momo(j) {
  const H = '모모', K = '분신', SH = '모모_shuriken';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '따라 던지고 대신 맞는 람쥐 분신', carrier: 'self', cap: 3, guard: true, cut: 0.2, uses: 2 };   // 3단계 손질(2026-10-09): 대신 맞기 30% → 20%(딜러 평균 +6.6)
  h.passives = [
    pas('분신 자폭', 'summonAct', [dmg(0.6, ER), st('충격', 1, ER)], { when: { id: K, kind: 'lost' } }),
    pas('분신술', 'play', [summon(K, 0.25, { max: 3 })], { when: { type: '공격' }, limit: 1 }),
  ];
  setCards(j, [
    // 만들기 — 두배로 안아준닷: 분신 둘 + 실드
    U(H, 1, '두배로 안아준닷', 1, '스킬', [stk(K, 2), sh(0.5)], [
      'A', 'B',
      Or([stk(K, 1), dmg(0.7), make(SH, 1)]),
      Or([stk(K, 1), sh(0.5), pw('turnStart', [stk(K, 1)])], { power: true }),
      'Hn',
    ], bl('guard', 'draw', [stk(K, 1)])),
    U(H, 2, '전기 수리검', 1, '공격', [dmg(0.5, E1, { hits: 2 }), st('충격', 1), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.42, EA, { hits: 2 }), stk(K, 1)]),
      Or([dmg(0.45, E1, { hits: 2 }), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 환영술: 분신 + 번개 수리검 + 드로우
    U(H, 3, '환영술', 1, '스킬', [stk(K, 1), make(SH, 1), draw(1)], [
      Or([stk(K, 2), make(SH, 1), draw(1)]),
      Or([stk(K, 1), make(SH, 1)], { cost: 0 }),
      Or([stk(K, 1), make(SH, 2)]),
      Or([stk(K, 2), make(SH, 1), srch()]),
      Or([stk(K, 2), make(SH, 2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null),
    U(H, 4, '카게닌자의 극의', 1, '공격', [dmg(0.6), { k: 'perTag', id: SH }, dmg(0.3), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.5, EA), { k: 'perTag', id: SH }, dmg(0.2, EA)]),
      Or([dmg(0.6), { k: 'perTag', id: SH }, dmg(0.25), srch()]),
      Or([dmg(0.9), { k: 'perTag', id: SH }, dmg(0.45), exile('hand', { all: true, tag: SH })]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 분신 폭발: 분신 1개당 적 전체, 분신 전부 소모 + 충격
    U(H, 5, '분신 폭발', 1, '공격', [per(K, { n: 1 }), dmg(0.38, EA), spendAll(K), st('충격', 1, EA)], [
      'A',
      ['C', [stk(K, 1)]],
      Or([per(K, { n: 1 }), dmg(0.7), spendAll(K), tough(1)]),
      Or([per(K, { n: 1 }), dmg(0.34, EA), st('충격', 1, EA)]),
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 15. 힐데 — 회복형 · 서포터 · 우울. 멀쩡해도 일단 처방하고(과잉 진료), 회복 뒤 파티 HP 80%를 넘긴 몫을 「파티 실드」로
// 원작: 파티 회복 · 80% 아래 둘 지속 회복 · 과잉진료 · 전용 아티팩트 「힐데의 간호복」(넘친 회복 → 보호막)
// 시동: u2 정밀 회진
// ════════════════════════════════════════════════════════════════════
function hilde(j) {
  const H = '힐데', K = '진료 차트', NOTE = '힐데_note';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '진찰할 때마다 채우는 차트', carrier: 'self', cap: 3, onMax: { make: NOTE, consume: true } };
  const over = (r = 1, limit = 1) => pw('overheal', [{ k: 'shield', ratio: 1, ofEvent: r }], { when: { pct: 0.8 }, limit });
  setCards(j, [
    // 쓰기 — 피톤치드 파동: 차트 1개당 회복, 전부 소모. 그 뒤 HP 80%를 넘긴 회복 20당 실드
    U(H, 1, '피톤치드 파동', 1, '스킬', [per(K, { n: 1 }), heal(0.45), spendAll(K), perOver(0.8, 20), sh(0.12)], [
      'A', 'B',
      Or([per(K, { n: 1 }), sh(0.5), spendAll(K), heal(0.3)]),
      Or([per(K, { n: 1 }), heal(0.4), spendAll(K), srch()]),
      'Hn',
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 시동 — 정밀 회진: 차트 + 회복 + 드로우
    U(H, 2, '정밀 회진', 1, '스킬', [stk(K, 1), heal(0.6), draw(1)], [
      Or([stk(K, 1), heal(0.85), draw(1)]),
      Or([stk(K, 1), heal(0.4)], { cost: 0 }),
      Or([stk(K, 2), sh(0.8), draw(1)]),
      Or([stk(K, 2), heal(0.5), srch()]),
      Or([stk(K, 2), draw(2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null),
    // 갈래 부품 — 의료인 보호법(강화): 회복 뒤 HP 80%를 넘긴 몫만큼 파티 실드(턴 1회)
    U(H, 3, '의료인 보호법', 1, '강화', [sh(0.6), over(1, 1)], [
      Or([sh(0.8), over(1.2, 1)]),
      Or([over(1, 1)], { cost: 0 }),
      Or([heal(0.5), sh(0.5), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })]),
      Or([sh(0.5), srch(), over(1, 1)]),
      Or([sh(0.9), over(1, 2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), bl('guard', 'ap', [stk(K, 1)])),
    U(H, 4, '주사기총 난사', 1, '공격', [hits(3, 0.42, ER), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 1 }],
      Or([dmg(0.6, EA), heal(0.3), stk(K, 1)]),
      Or([hits(3, 0.38, ER), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 원작 자유 — 과잉진료: 적 전체 + 약화, 부상이면 초재생
    U(H, 5, '판을 뒤집는 수', 1, '공격', [dmg(0.6, EA), st('약화', 1, EA), ifWounded, st('초재생', 1)], [
      'A', 'B',
      Or([dmg(1.0), st('약화', 2), heal(0.4)]),
      Or([dmg(0.55, EA), st('약화', 1, EA), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 16. 큐이 — 회복형 · 서포터 · 순수. 오이 팩을 고른 아군에게 붙여 두면 그 아군이 카드를 낼 때마다 조금씩 낫는다 — 넘친 회복은 공격으로
// 원작: 저학년 · 고학년 모두 「HP 비율 가장 낮은 아군 회복」 → 파티 HP 하나라 「고른 아군 1명」 으로(문구 정리)
// 시동: u1 몰래 오이 심기
// ════════════════════════════════════════════════════════════════════
function kyui(j) {
  const H = '큐이', K = '오이 팩', CU = '큐이_cucumber';
  setCards(j, [
    U(H, 1, '몰래 오이 심기', 0, '스킬', [stk(K, 1, ALLY), draw(1)], [
      Or([stk(K, 2, ALLY), draw(1)]),
      Or([stk(K, 1, ALLY), draw(1)], { tags: ['신속'] }),
      Or([heal(0.6), stk(K, 2, ALLY)]),
      Or([stk(K, 2, ALLY), srch()]),
      Or([stk(K, 2, ALLY), draw(2), disc(1)]),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null),
    // 원작 자유 — 오이 오일: 크게 회복 + 고른 아군에게 팩
    U(H, 2, '오이 오일', 1, '스킬', [heal(1.0), stk(K, 1, ALLY)], [
      'A', 'B',
      Or([heal(1.1), perOver(0.8, 20), sh(0.3), stk(K, 1, ALLY)]),
      Or([heal(0.8), stk(K, 1, ALLY), srch()]),
      'Hn',
    ], bl('heal', 'draw', [stk(K, 1, ALLY)])),
    U(H, 3, '오이를 권하는 행렬', 2, '스킬', [st('사기', 1), heal(1.1), stk(K, 1, 'allAllies')], [
      'A', 'B',
      Or([st('사기', 1), sh(1.4), stk(K, 1, ALLY)]),
      Or([st('사기', 1), heal(0.9), pw('turnStart', [stk(K, 1, ALLY)])], { power: true }),
      Or([st('사기', 1), heal(2.0)]),
    ], bl('heal', 'ap', [stk(K, 1, ALLY)])),
    // 쓰기 — 오이 샌드위치 공세: 단일 피해 + 이번 판 HP 80%를 넘긴 회복 25당 1타
    U(H, 4, '오이 샌드위치 공세', 1, '공격', [dmg(0.8), perOver(0.8, 25), dmg(0.15), stk(K, 1, ALLY)], [
      'A', ['D', 'turnStart', [stk(K, 1, ALLY)]],
      Or([dmg(0.6, EA), perOver(0.8, 25), dmg(0.1, EA)]),
      Or([dmg(0.75), perOver(0.8, 25), dmg(0.15), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1, ALLY)])),
    U(H, 5, '오이 투척', 0, '공격', [dmg(0.45), make(CU, 1)], [
      'A', 'B',
      Or([heal(0.6), make(CU, 2)]),
      Or([dmg(0.4), make(CU, 1), stk(K, 1, ALLY)]),
      Or([dmg(0.7), make(CU, 2), nextAp(-1)]),
    ], bl('power', { tags: ['보존'] }, [stk(K, 1, ALLY)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 17. 코미 — 두 얼굴형 · 탱커 · 우울(일반 사도 두 얼굴 시범). 잠든 얼굴(회복 · 면역 · 낮잠이 쌓인다)과 거대한 얼굴(피해)을 손으로 오간다
// 원작: 저학년 잠(회복 · 디버프 면역) / 고학년 거대화(피해 · 공속) — 두 상태가 원작에 다 있다
// 시동: u4 엘프산 사료 한 그릇(개전 강화)
// ════════════════════════════════════════════════════════════════════
function komi(j) {
  const H = '코미', K = '낮잠', BIG = '코미_거대화', SLEEP = '코미_잠';
  const h = j.heroes[0];
  h.keyword = { ...h.keyword };
  h.forms = [
    h.forms.find(f => f.id === BIG),
    {
      id: SLEEP, name: '쿨쿨 코미', desc: '어디서든 몸을 말고 잠든 얼굴 — 자는 동안 상처가 아물고 나쁜 기운도 비켜 가는 꿀잠',
      turns: 2, until: { on: 'play', type: '공격' }, mods: { def: 0.2 },
      passives: [{ name: '꿀잠', when: { on: 'turnStart' }, fx: [heal(0.4), stk(K, 1), st('면역', 1)] }],
      skin: null, anim: null,
    },
  ];
  setCards(j, [
    // 잠든 얼굴로 — 푹신푹신 타임: 실드 + 쿨쿨 코미(공격하면 깬다)
    U(H, 1, '푹신푹신 타임', 1, '스킬', [sh(0.9), st('불굴', 1), form(SLEEP)], [
      'A', 'B',
      Or([heal(0.9), st('불굴', 2), form(SLEEP)]),
      Or([st('불굴', 2), form(SLEEP), srch()]),
      Or([st('불굴', 2), form(SLEEP), stk(K, 2)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    U(H, 2, '베개 강타', 1, '공격', [ddef(0.6), stk(K, 1), { k: 'ifRandom', pct: 0.2 }, st('기절', 1)], [
      'A', ['D', 'turnEnd', [sh(0.3)]],
      Or([ddef(0.45, EA), stk(K, 1)]),
      Or([ddef(0.55), stk(K, 1), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    U(H, 3, '일하기 싫으면 안 해도 돼', 1, '스킬', [per(K), sh(0.22), sh(0.4), exile('hand', { n: 1, basic: true })], [
      'A', 'B',
      Or([per(K), heal(0.35), stk(K, 1)]),
      Or([per(K), sh(0.2), sh(0.3), srch()]),
      ['Ht', '보존'],
    ], bl('guard', 'ap', [stk(K, 1)]), ['보존']),
    U(H, 4, '엘프산 사료 한 그릇', 1, '강화', [stk(K, 1), pw('turnStart', [stk(K, 1)])], [
      Or([stk(K, 2), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 2), sh(0.7), pw('keepAp', [stk(K, 1)], { when: { kind: 'ap' }, limit: 1 })], { tags: ['개전'] }),
      Or([stk(K, 1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ].map((o, i) => ({ name: `신탁 ${i + 1}`, ...o })), null, ['개전']),
    // 쓰기 — 사료 한 그릇 더: 낮잠 셋이면 다 쓰고 거대 코미로
    U(H, 5, '사료 한 그릇 더', 1, '공격', [ddef(0.55), ifStack(K, 3), spendAll(K), form(BIG)], [
      'A', 'B',
      Or([ddef(0.45, EA), ifStack(K, 2), spendAll(K), form(BIG)]),
      Or([ddef(0.5), ifStack(K, 3), form(BIG), srch()]),
      Or([ddef(1.2), stk(K, 2), disc(1)]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = {
  '요정/리코타': 0.85, '수인/모모': 0.85, '수인/티그': 0.7, '정령/니콜': 0.92, '요정/큐이': 1.0, '수인/코미': 1.05, '엘프/힐데': 1.2, '정령/이프리트': 1.35,
  '요정/샤샤': 1.2, '요정/캬롯': 1.1, '요정/죠안': 1.4, '마녀/마카샤': 1.3, '수인/쵸피': 1.45, '정령/쥬비': 1.3,
};
function tune(j, m) {
  const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent) f.ratio = R2(f.ratio * m); if (f.k === 'power') for (const r of f.rules) mul(r.fx); if (f.then) mul(f.then); } };
  // 변신판(_f1)은 바꾸는 카드와 견주므로 빼고
  for (const c of j.cards) if (c.unique || (c.token && !/_f\d+$/.test(c.id))) { mul(c.fx); for (const o of c.oracles || []) mul(o.fx); for (const b of c.blesses || []) mul(b.fx); }
  const h = j.heroes[0];
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) mul(r.fx);
  for (const r of h.passives || []) mul(r.fx);
}

// ── 돌리기 ──
const JOBS = [
  ['요정/리코타', ricotta], ['수인/티그', tig], ['수인/디아나_왕년', dianaOld], ['요정/죠안', joanne], ['요정/캬롯', carrot], ['마녀/마카샤', makasha],
  ['마녀/벨벳', velvet], ['요정/샤샤', shasha], ['정령/이프리트', ifrit], ['정령/니콜', nicole], ['수인/쵸피', choppy], ['마녀/레비_졸업', leviGrad],
  ['정령/쥬비', jubee], ['수인/모모', momo], ['엘프/힐데', hilde], ['요정/큐이', kyui], ['수인/코미', komi],
];
function boost(j, Bst) {
  const mul = (fx, b) => { for (const f of fx) { if (['dmg', 'shield', 'heal', 'extra', 'summon'].includes(f.k) && f.ratio && !f.ofEvent) f.ratio = R2(f.ratio * b); if (f.k === 'power') for (const r of f.rules) mul(r.fx, b); } };
  for (const c of j.cards) (c.oracles || []).forEach((o, i) => { const b = Bst[`${c.id}|${i + 1}`]; if (b) mul(o.fx, b); });
}
const boostFile = new URL('./boost_시범16.json', import.meta.url);
const Bst = fs.existsSync(boostFile) ? JSON.parse(fs.readFileSync(boostFile, 'utf8')) : {};
const only = process.argv[2];
for (const [f, fn] of JOBS) {
  if (only && !f.includes(only)) continue;
  const j = JSON.parse(fs.readFileSync(`${SRC}/${f}.json`, 'utf8'));
  fn(j);
  boost(j, Bst);
  if (TUNE[f]) tune(j, TUNE[f]);
  fs.writeFileSync(`${OUT}/${f}.json`, JSON.stringify(j, null, 2) + '\n', 'utf8');
  console.log('썼다', f);
}
