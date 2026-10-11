// 엘다인 20명 원본 공용 조각(2026-10-11) — 옛 _gen/deep 135명 생성기를 대신한다.
// 사도 하나 = src/<사도키>.mjs 한 장(손으로 쓴 설계). 여기는 「기본 카드 3장」 과 조각 줄임말만 둔다 — 규칙 · 수치는 사도 파일에.

// 역할 × 등급 스탯(사용자 확정 — 3성)
export const STATS = {
  딜러: { hp: 650, atk: 140, def: 40, crit: 10 },
  탱커: { hp: 810, atk: 115, def: 48, crit: 5 },
  서포터: { hp: 650, atk: 95, def: 46, crit: 5 },   // 10-11 밸런스: 서포터 편성이 가장 쉬워 공격력 100→95 · 방어력 50→46
};

// ── 조각 줄임말 ──
const T = (o, x) => Object.assign(o, x || {});
export const dmg = (ratio, target = 'oneEnemy', x) => T({ k: 'dmg', ratio, target }, x);
export const ddmg = (ratio, target = 'oneEnemy', x) => T({ k: 'dmg', ratio, base: 'def', target }, x); // 방어 기반
export const shield = (ratio, x) => T({ k: 'shield', ratio }, x);
export const heal = (ratio, x) => T({ k: 'heal', ratio }, x);
export const stack = (id, v = 1, x) => T({ k: 'stack', id, v }, x);
export const spend = (id, v, x) => T(v === 'all' ? { k: 'spend', id, all: true } : v === 'pick' ? { k: 'spend', id, pick: true } : { k: 'spend', id, v }, x);
export const make = (id, v = 1, x) => T({ k: 'make', id, v }, x);
export const draw = (v = 1, x) => T({ k: 'draw', v }, x);
export const discard = (v = 1, x) => T({ k: 'discard', v }, x);
export const status = (id, v = 1, x) => T({ k: 'status', id, v }, x);
export const empower = (ratio, x) => T({ k: 'empower', ratio }, x);
export const ifStack = (id, n, x) => T({ k: 'ifStack', id, n }, x);
export const perStack = (id, x) => T({ k: 'perStack', id }, x);
export const power = (rules, id) => (id ? { k: 'power', id, rules } : { k: 'power', rules });
export const rule = (when, fx, x) => T({ when, fx }, x);

// ── 기본 카드 3장 — 덧효과 · 태그 · 조건 없는 한 줄 수치 ──
// 딜러 · 서포터: 1코 공격 100% · 2코 공격 220% · 1코 스킬 실드 140%(서포터는 heal 이면 회복 140%)
// 탱커: 방어 기반 피해 100% · 220% · 실드 140%
// 덱 무게(사용자 2026-10-11 「가벼운 덱 / 무거운 컨셉 덱」):
//   light  = 1코 공격 100% 같은 카드 2장 · 1코 실드(회복) 140% — 많이 내고 · 버리고 · 생성 카드를 굴리는 사도
//   heavy  = 1코 공격 100% · 2코 공격 220% · 1코 실드(회복) 140% — 큰 카드를 기다리는 사도(옛 꼴)
//   heavy2 = 1코 공격 100% · 2코 공격 220% · 2코 실드(회복) 250% — 무거움이 엔진 몸통(천상 등)
export function basics(hero, role, [n1, n2, n3], opt = {}) {
  const tank = role === '탱커';
  const hit = tank ? ddmg : dmg;
  const deck = opt.deck || 'heavy';
  const big3 = deck === 'heavy2';
  const third = opt.heal ? heal(big3 ? 2.5 : 1.4) : shield(big3 ? 2.5 : 1.4);
  // 가벼운 덱(사용자 10-11): 1코 기본 공격은 같은 카드 2장 — 카드는 _s1 하나, 시작 덱에 두 번(_s2 없음)
  return [
    { id: `${hero}_s1`, name: n1, hero, cost: 1, type: '공격', fx: [hit(1)] },
    ...(deck === 'light' ? [] : [{ id: `${hero}_s2`, name: n2, hero, cost: 2, type: '공격', fx: [hit(2.2)] }]),
    { id: `${hero}_s3`, name: n3, hero, cost: big3 ? 2 : 1, type: '스킬', fx: [third] },
  ];
}

// 사도 한 장을 묶음으로 — 스탯은 역할 표, 시작 덱 = 기본 3 + 시동(u1)
export function hero(h, { basicNames, heal: healBasic, deck, uniques, tokens = [], starter = true }) {
  const id = h.id;
  // 시작 덱 = 기본 3 + 시동(u1) — 시동 없이 엔진이 도는 사도는 starter:false(기본 3장만, 시동은 은총으로)
  const out = { ...h, ...STATS[h.role], starter: [`${id}_s1`, deck === 'light' ? `${id}_s1` : `${id}_s2`, `${id}_s3`, ...(starter ? [`${id}_u${starter === true ? 1 : starter}`] : [])] };   // starter: true = u1 · 숫자 = 그 고유 카드(사용자 10-11: 20명 모두 시작 덱 4장)
  // 키 순서를 읽기 좋게
  const order = ['id', 'name', 'nature', 'race', 'role', 'star', 'hp', 'atk', 'def', 'crit', 'blurb', 'keyword', 'keywords', 'passives', 'forms', 'ult', 'starter'];
  const H = {}; for (const k of order) if (out[k] !== undefined) H[k] = out[k];
  for (const k in out) if (!(k in H)) H[k] = out[k];
  // 신탁 · 축복 이름은 차례 번호(화면은 번호를 안 보인다). 축복 칸: 문자열 = 배율 꼴(kind), 배열 = 덤 효과(fx)
  const named = (c) => {
    const o = { ...c };
    if (o.oracles) o.oracles = o.oracles.map((x, i) => ({ name: `신탁 ${i + 1}`, ...x }));
    if (o.blesses) o.blesses = o.blesses.map((b, i) => (typeof b === 'string' ? { name: `축복 ${i + 1}`, kind: b } : Array.isArray(b) ? { name: `축복 ${i + 1}`, fx: b } : { name: `축복 ${i + 1}`, ...b }));
    return o;
  };
  const U = uniques.map((c, i) => named({ id: `${id}_u${i + 1}`, name: c.name, hero: id, unique: true, ...c }));
  const K = tokens.map(c => ({ hero: id, token: true, ...c }));
  return { heroes: [H], cards: [...basics(id, h.role, basicNames, { heal: healBasic, deck }), ...U, ...K] };
}
