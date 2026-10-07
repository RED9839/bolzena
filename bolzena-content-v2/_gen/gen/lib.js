// 엘프 사도 생성 도우미 — 효과 조각 · 신탁 변형 · 축복 · 파일 쓰기
const fs = require('fs');
const path = require('path');

const r2 = x => Math.round(x * 100) / 100;
const D = (r, t = 'oneEnemy', o = {}) => ({ k: 'dmg', ratio: r, target: t, ...o });
const DH = (r, h, t = 'oneEnemy') => ({ k: 'dmg', ratio: r, hits: h, target: t });
const DA = (r, o = {}) => D(r, 'allEnemies', o);
const DF = (r, t = 'oneEnemy', o = {}) => ({ k: 'dmg', ratio: r, base: 'def', target: t, ...o });
const DFA = r => DF(r, 'allEnemies');
const SH = r => ({ k: 'shield', ratio: r });
const HL = r => ({ k: 'heal', ratio: r });
const S = (id, v, t) => t ? { k: 'status', id, v, target: t } : { k: 'status', id, v };
const K = (id, v, t) => t ? { k: 'stack', id, v, target: t } : { k: 'stack', id, v };
const SP = (id, v) => v ? { k: 'spend', id, v } : { k: 'spend', id, all: true };
const IFS = (id, n = 1, o = {}) => ({ k: 'ifStack', id, n, ...o });
const NOT = id => ({ k: 'ifStack', id, not: true });
const PER = id => ({ k: 'perStack', id });
const X = (k, o = {}) => ({ k, ...o });
const DRAW = v => ({ k: 'draw', v });
const AP = v => ({ k: 'ap', v });
const MOD = (k, v, t = 'self', run = true) => run ? { k, v, target: t, run: true } : { k, v, target: t };

const EFFECT_FREE = new Set(['ifBroken', 'ifTune', 'ifChain', 'ifStack', 'perStack', 'when', 'ifKill', 'ifBreak', 'ifWounded', 'ifChoice', 'ifRandom', 'ifHand',
  'ifPile', 'ifNth', 'ifStreak', 'ifAllHeroes', 'ifFoe', 'ifCardSt', 'perTag', 'perPlayed', 'perPile', 'perCardSt', 'perEvent', 'ifHp', 'perApLeft']);
const effects = fx => fx.filter(f => !EFFECT_FREE.has(f.k)).length;
const RATIO_K = new Set(['dmg', 'shield', 'heal', 'extra', 'drain']);
const hasRatio = fx => fx.some(f => RATIO_K.has(f.k) && f.k !== 'drain');

/** 효과 키우기 — 비율 · 증감 · 게이지는 m 배, 비율이 하나도 없으면 상태 · 키워드 겹 +1 */
function scale(fx, m) {
  const ratio = hasRatio(fx);
  return fx.map(f => {
    const g = { ...f };
    if (RATIO_K.has(g.k) && g.k !== 'drain') g.ratio = r2(g.ratio * m);
    else if (['atkMod', 'defMod', 'critMod', 'dealtMod', 'takenMod'].includes(g.k)) g.v = r2(g.v * m);
    else if (g.k === 'gauge') g.v = Math.round(g.v * m);
    else if (!ratio && (g.k === 'status' || g.k === 'stack') && m >= 1.2) g.v = g.v + 1;
    if (g.then) g.then = scale(g.then, m);
    return g;
  });
}

/**
 * 신탁 다섯. spec 칸: 문자열 = 자동 변형 · 객체 = 손으로 쓴 신탁({cost, tags, fx, power}).
 *  'up'    — 효과 ×1.3
 *  'cheap' — 코스트 -1(0 이면 ×1.4)
 *  'draw'  — 드로우 1 덧붙임(효과 셋이면 ×1.25 + 보존)
 *  'keep'  — 보존 + 효과 ×1.2
 *  'swift' — 신속 + 효과 ×1.2
 *  'kw'    — 고유 효과 +1 덧붙임(kwFx 주어짐 · 효과 셋이면 ×1.3)
 *  'power' — 강화 카드가 된다 + ×1.25 (공격 · 스킬에만)
 */
let BOOST = {};
function setBoost(b) { BOOST = b; }
function boostFx(fx, b) {
  if (!b || b === 1) return fx;
  if (hasRatio(fx)) return fx.map(f => {
    const g = { ...f };
    if (RATIO_K.has(g.k) && g.k !== 'drain') g.ratio = r2(g.ratio * b);
    else if (['atkMod', 'defMod', 'critMod'].includes(g.k)) g.v = r2(g.v * b);
    return g;
  });
  let done = false; const steps = Math.max(1, Math.round((b - 1) / 0.1));
  return fx.map(f => {
    const g = { ...f };
    if (!done && ['atkMod', 'defMod', 'critMod'].includes(g.k)) { g.v = r2(g.v * b); done = true; }
    else if (!done && ['status', 'stack', 'draw'].includes(g.k)) { g.v = g.v + steps; done = true; }
    return g;
  });
}
function oracles(base, names, specs, kwFx) {
  return specs.map((s, i) => {
    const name = names[i];
    let o;
    if (typeof s === 'object') o = { ...s };
    else switch (s) {
      case 'up': o = { fx: scale(base.fx, 1.3) }; break;
      case 'up2': o = { fx: scale(base.fx, 1.45) }; break;
      case 'cheap': o = base.cost > 0 ? { cost: base.cost - 1, fx: base.fx } : { fx: scale(base.fx, 1.4) }; break;
      case 'draw': o = effects(base.fx) < 3 ? { fx: [...base.fx, DRAW(1)] } : { fx: scale(base.fx, 1.25), tags: [...(base.tags || []), '보존'] }; break;
      case 'keep': o = { fx: scale(base.fx, 1.2), tags: [...(base.tags || []).filter(t => t !== '보존'), '보존'] }; break;
      case 'swift': o = { fx: scale(base.fx, 1.2), tags: [...(base.tags || []).filter(t => t !== '신속'), '신속'] }; break;
      case 'kw': o = kwFx && effects(base.fx) < 3 ? { fx: [...base.fx, kwFx] } : { fx: scale(base.fx, 1.3) }; break;
      case 'cost+': o = { cost: base.cost + 1, fx: scale(base.fx, 1.8) }; break;
      default: throw new Error('모르는 신탁 꼴 ' + s);
    }
    const out = { name };
    if (o.cost !== undefined && o.cost !== base.cost) out.cost = o.cost;
    const tags = o.tags !== undefined ? o.tags : (base.tags || []);
    if (tags.length) out.tags = tags;
    out.fx = boostFx(o.fx, BOOST[base.id + '|' + (i + 1)]);
    if (o.power) out.power = true;
    return out;
  });
}

function blesses(type, names, kwFx) {
  const kinds = type === '공격' ? ['power', 'ap'] : type === '스킬' ? ['guard', 'draw'] : ['atkUp', 'draw'];
  return [
    { name: names[0], kind: kinds[0] },
    { name: names[1], kind: kinds[1] },
    kwFx ? { name: names[2], fx: [kwFx] } : { name: names[2], kind: type === '공격' ? 'weakSpot' : 'defUp' },
  ];
}

/** 사도 파일 하나 — h: 사도 정의, starters: [{id,name,cost,type,fx}], uniques: [{…, oNames, oSpecs, bNames, kwFx}], tokens */
function hero(def) {
  const { h, starters, uniques, tokens = [] } = def;
  const cards = [];
  for (const s of starters) cards.push({ id: s.id, name: s.name, hero: h.id, cost: s.cost, type: s.type, fx: s.fx });
  for (const t of tokens) cards.push({ ...t, hero: h.id, token: true });
  uniques.forEach((u, i) => {
    const c = { id: u.id, name: u.name, hero: h.id, unique: true };
    if (i === 0) c.signature = true;
    c.cost = u.cost; c.type = u.type;
    if (u.tags && u.tags.length) c.tags = u.tags;
    if (u.choices) c.choices = u.choices;
    if (u.payWith) c.payWith = u.payWith;
    c.fx = u.fx;
    c.oracles = oracles(u, u.oNames, u.oSpecs, u.kwFx);
    c.blesses = blesses(u.type, u.bNames, u.bFx === undefined ? u.kwFx : u.bFx);
    if (u.blurb) c.blurb = u.blurb;
    cards.push(c);
  });
  return { heroes: [h], cards };
}

/** 역할별 기본 카드 넉 장(피해 · 실드 · 치유만) */
function basics(key, role, names) {
  if (role === '탱커') return {
    starters: [
      { id: `${key}_s1`, name: names[0], cost: 1, type: '공격', fx: [DF(0.6)] },
      { id: `${key}_s2`, name: names[1], cost: 1, type: '스킬', fx: [SH(2.0)] },
    ], ids: [`${key}_s1`, `${key}_s1`, `${key}_s2`, `${key}_s2`],
  };
  if (role === '서포터') return {
    starters: [
      { id: `${key}_s1`, name: names[0], cost: 1, type: '공격', fx: [DF(0.6)] },
      { id: `${key}_s2`, name: names[1], cost: 1, type: '스킬', fx: [HL(2.1)] },
    ], ids: [`${key}_s1`, `${key}_s1`, `${key}_s2`, `${key}_s2`],
  };
  return {
    starters: [
      { id: `${key}_s1`, name: names[0], cost: 1, type: '공격', fx: [D(1.2)] },
      { id: `${key}_s2`, name: names[1], cost: 2, type: '공격', fx: [D(2.5)] },
      { id: `${key}_s3`, name: names[2], cost: 1, type: '스킬', fx: [SH(2.0)] },
    ], ids: [`${key}_s1`, `${key}_s1`, `${key}_s2`, `${key}_s3`],
  };
}

function write(dir, key, obj) {
  fs.mkdirSync(dir, { recursive: true });
  if (JSON.parse(fs.readFileSync('C:/projects/bolzena-content-v2/_gen/rework/reworked.json', 'utf8')).includes(key)) { console.log('건너뜀(리워크)', key); return; }   // 리워크한 사도는 _gen/rework 가 쓴다
  fs.writeFileSync(path.join(dir, key + '.json'), JSON.stringify(obj, null, 2) + '\n', 'utf8');
}

module.exports = { setBoost, r2, D, DH, DA, DF, DFA, SH, HL, S, K, SP, IFS, NOT, PER, X, DRAW, AP, MOD, effects, scale, oracles, blesses, hero, basics, write };
