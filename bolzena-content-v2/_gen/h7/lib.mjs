// H7 생성기 공용 부품 — 마녀 14 + 미스틱 2
import fs from 'fs';
import path from 'path';

export const OUT = 'C:/projects/bolzena-content-v2/heroes/마녀';

const r5 = x => Math.round(x * 20) / 20;
// 효과 조각
export const D = (ratio, target = 'oneEnemy', o = {}) => ({ k: 'dmg', ratio, target, ...o });
export const DD = (ratio, target = 'oneEnemy', o = {}) => ({ k: 'dmg', ratio, target, base: 'def', ...o });
export const SH = ratio => ({ k: 'shield', ratio });
export const HE = ratio => ({ k: 'heal', ratio });
export const ST = (id, v, target) => target ? { k: 'status', id, v, target } : { k: 'status', id, v };
export const STK = (id, v, target) => target ? { k: 'stack', id, v, target } : { k: 'stack', id, v };
export const SPA = id => ({ k: 'spend', id, all: true });
export const SP = (id, v) => ({ k: 'spend', id, v });
export const MK = (id, v = 1, to) => to ? { k: 'make', id, v, to } : { k: 'make', id, v };
export const DR = v => ({ k: 'draw', v });
export const AP = v => ({ k: 'ap', v });
export const IFS = (id, n, max) => { const o = { k: 'ifStack', id }; if (n) o.n = n; if (max) o.max = max; return o; };
export const IFNOT = id => ({ k: 'ifStack', id, not: true });
export const PERS = id => ({ k: 'perStack', id });
export const WHEN = on => ({ k: 'when', on });
export const IFC = n => ({ k: 'ifChoice', n });

// 수치 키우기 — 피해 · 실드 · 회복 · 추가 공격의 ratio 를 m 배(0.05 단위)
export function sc(fx, m) {
  return fx.map(f => {
    const g = { ...f };
    if (['dmg', 'shield', 'heal', 'extra'].includes(f.k)) g.ratio = Math.max(0.05, r5(f.ratio * m));
    if (f.then) g.then = sc(f.then, m);
    return g;
  });
}

// 신탁 만들기 — c 는 기본 카드
export const O = {
  up: (name, m = 1.3) => c => ({ name, ...(c.tags?.length ? { tags: [...c.tags] } : {}), fx: sc(c.fx, m) }),
  cheap: (name, m = 0.8) => c => ({ name, cost: c.cost - 1, ...(c.tags?.length ? { tags: [...c.tags] } : {}), fx: sc(c.fx, m) }),
  big: (name, m = 2.0, extra = []) => c => ({ name, cost: c.cost + 1, ...(c.tags?.length ? { tags: [...c.tags] } : {}), fx: [...sc(c.fx, m), ...extra] }),
  plus: (name, extra, m = 1.0) => c => ({ name, ...(c.tags?.length ? { tags: [...c.tags] } : {}), fx: [...sc(c.fx, m), ...extra] }),
  tag: (name, tags, m = 1.15) => c => ({ name, tags: [...(c.tags || []), ...tags], fx: sc(c.fx, m) }),
  raw: (name, fx, o = {}) => c => ({ name, ...(c.tags?.length && !o.tags ? { tags: [...c.tags] } : {}), ...o, fx }),
};

export function hero(key, h) {
  const cards = [];
  const id = s => `${key}_${s}`;
  // 시작 카드(기본 4장) — 피해 · 실드 · 치유만
  for (const s of h.start) cards.push({ id: id(s.id), name: s.name, hero: key, cost: s.cost, type: s.type, fx: s.fx });
  for (const t of h.tokens || []) cards.push({ id: id(t.id), name: t.name, hero: key, token: true, cost: t.cost, type: t.type, ...(t.tags ? { tags: t.tags } : {}), fx: t.fx, ...(t.blurb ? { blurb: t.blurb } : {}) });
  for (const u of h.uniques) {
    const base = { cost: u.cost, tags: u.tags || [], fx: u.fx };
    const c = { id: id(u.id), name: u.name, hero: key, unique: true, cost: u.cost, type: u.type };
    if (u.sig) c.signature = true;
    if (u.tags?.length) c.tags = u.tags;
    if (u.choices) c.choices = u.choices;
    c.fx = u.fx;
    if (u.blurb) c.blurb = u.blurb;
    c.oracles = u.oracles.map(f => f(base));
    c.blesses = u.blesses;
    cards.push(c);
  }
  const H = {
    id: key, name: h.name, nature: h.nature, race: h.race, role: h.role, star: h.star,
    hp: h.hp, atk: h.atk, def: h.def, crit: h.crit, blurb: h.blurb,
    keyword: h.keyword,
  };
  if (h.keywords) H.keywords = h.keywords;
  if (h.passives) H.passives = h.passives;
  H.ult = h.ult;
  H.starter = h.starter.map(id);
  fs.mkdirSync(OUT, { recursive: true });
  fs.writeFileSync(path.join(OUT, `${key}.json`), JSON.stringify({ heroes: [H], cards, ...(h.equips ? { equips: h.equips } : {}) }, null, 2) + '\n', 'utf8');
}

// 기본 카드 틀(design.js 시작 카드 — block 을 shield 로)
export const START = {
  dealer: (n1, n2, n3) => ({
    start: [
      { id: 's1', name: n1, cost: 1, type: '공격', fx: [D(1.0)] },
      { id: 's2', name: n2, cost: 2, type: '공격', fx: [D(2.2)] },
      { id: 's3', name: n3, cost: 1, type: '스킬', fx: [SH(2.0)] },
    ], starter: ['s1', 's1', 's2', 's3'],
  }),
  tank: (n1, n2) => ({
    start: [
      { id: 's1', name: n1, cost: 1, type: '공격', fx: [DD(0.6)] },
      { id: 's2', name: n2, cost: 1, type: '스킬', fx: [SH(2.0)] },
    ], starter: ['s1', 's1', 's2', 's2'],
  }),
  support: (n1, n2) => ({
    start: [
      { id: 's1', name: n1, cost: 1, type: '공격', fx: [DD(0.6)] },
      { id: 's2', name: n2, cost: 1, type: '스킬', fx: [HE(2.1)] },
    ], starter: ['s1', 's1', 's2', 's2'],
  }),
};
