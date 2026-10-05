// H6 용족 사도 생성기 — 짧은 꼴로 쓰고 JSON 으로 펼친다
const fs = require('fs');
const path = require('path');
const OUT = 'C:/projects/bolzena-content-v2/heroes/용족';

const d = (r, o = {}) => ({ k: 'dmg', ratio: r, target: 'oneEnemy', ...o });
const dAll = (r, o = {}) => d(r, { target: 'allEnemies', ...o });
const dRnd = (r, hits, o = {}) => d(r, { target: 'randomEnemy', hits, ...o });
const dDef = (r, o = {}) => d(r, { base: 'def', ...o });
const sh = (r, o = {}) => ({ k: 'shield', ratio: r, ...o });
const hl = (r) => ({ k: 'heal', ratio: r });
const st = (id, v, target) => (target ? { k: 'status', id, v, target } : { k: 'status', id, v });
const stk = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const spend = (id, v) => (v === 'all' ? { k: 'spend', id, all: true } : { k: 'spend', id, v });
const ifS = (id, n, o = {}) => ({ k: 'ifStack', id, n, ...o });
const per = (id) => ({ k: 'perStack', id });
const draw = (v) => ({ k: 'draw', v });
const ap = (v) => ({ k: 'ap', v });
const tough = (v, target = 'oneEnemy') => ({ k: 'tough', v, target });
const make = (id, v = 1, to) => (to ? { k: 'make', id, v, to } : { k: 'make', id, v });
const when = (on) => ({ k: 'when', on });
const K = (k, o = {}) => ({ k, ...o });

const RATIO_K = new Set(['dmg', 'shield', 'heal', 'extra']);
/** 피해 · 실드 · 회복 계수를 m 배(소수 둘째 자리) */
function up(fx, m) {
  return fx.map((f) => (RATIO_K.has(f.k) ? { ...f, ratio: Math.round(f.ratio * m * 100) / 100 } : { ...f }));
}
/** 상태 · 키워드 겹을 +n (첫째 것만, 없으면 그대로) */
function more(fx, n = 1, which) {
  let done = false;
  return fx.map((f) => {
    if (!done && (f.k === 'status' || f.k === 'stack') && (!which || f.id === which)) { done = true; return { ...f, v: f.v + n }; }
    return { ...f };
  });
}

const O = (name, fx, o = {}) => ({ name, ...(o.cost !== undefined ? { cost: o.cost } : {}), ...(o.tags ? { tags: o.tags } : {}), fx });
const B = (name, o = {}) => ({ name, ...o });

function hero(h) {
  const id = h.id;
  const cards = [];
  const s = h.starter; // [[name, fx], [name, fx]] — 공격 · 스킬
  cards.push({ id: `${id}_s1`, name: s[0][0], hero: id, cost: 1, type: '공격', fx: s[0][1] });
  cards.push({ id: `${id}_s2`, name: s[1][0], hero: id, cost: 1, type: '스킬', fx: s[1][1] });
  h.uniques.forEach((u, i) => {
    const c = { id: `${id}_u${i + 1}`, name: u.name, hero: id, unique: true, cost: u.cost, type: u.type };
    if (u.tags && u.tags.length) c.tags = u.tags;
    if (u.payWith) c.payWith = u.payWith;
    if (u.choices) c.choices = u.choices;
    c.fx = u.fx;
    c.oracles = u.oracles;
    c.blesses = u.blesses;
    if (u.blurb) c.blurb = u.blurb;
    cards.push(c);
  });
  for (const t of h.tokens || []) cards.push({ hero: id, token: true, ...t, id: t.id });
  const H = {
    id, name: h.name, nature: h.nature, race: '용족', row: h.row, role: h.role, star: h.star,
    hp: h.hp, atk: h.atk, def: h.def, crit: h.crit, blurb: h.blurb,
    keyword: h.keyword,
  };
  if (h.keywords) H.keywords = h.keywords;
  H.passives = h.passives || [];
  H.ult = h.ult;
  H.starter = [`${id}_s1`, `${id}_s1`, `${id}_s2`, `${id}_s2`];
  const out = { heroes: [H], cards };
  if (h.equips) out.equips = h.equips;
  fs.mkdirSync(OUT, { recursive: true });
  fs.writeFileSync(path.join(OUT, `${id}.json`), JSON.stringify(out, null, 2) + '\n', 'utf8');
  return out;
}

module.exports = { d, dAll, dRnd, dDef, sh, hl, st, stk, spend, ifS, per, draw, ap, tough, make, when, K, up, more, O, B, hero };
