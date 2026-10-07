// H4 정령 사도 생성기 — 공용 손잡이
import fs from 'fs';
const REWORKED = JSON.parse(fs.readFileSync('C:/projects/bolzena-content-v2/_gen/rework/reworked.json', 'utf8')); // 리워크한 사도는 _gen/rework/rework.mjs 가 쓴다 — 여기서 덮어쓰지 않는다
import path from 'path';

export const E1 = 'oneEnemy', EA = 'allEnemies', ER = 'randomEnemy';
const r2 = x => Math.round(x * 100) / 100;

export const D = (ratio, target = E1, o = {}) => ({ k: 'dmg', ratio: r2(ratio), target, ...o });
export const DD = (ratio, target = E1, o = {}) => ({ k: 'dmg', ratio: r2(ratio), target, base: 'def', ...o });
export const SH = ratio => ({ k: 'shield', ratio: r2(ratio) });
export const HL = ratio => ({ k: 'heal', ratio: r2(ratio) });
export const ST = (id, v, target) => target ? { k: 'status', id, v, target } : { k: 'status', id, v };
export const K = (id, v, target) => target ? { k: 'stack', id, v, target } : { k: 'stack', id, v };
export const SP = (id, v) => v === 'all' ? { k: 'spend', id, all: true } : { k: 'spend', id, v };
export const DRAW = v => ({ k: 'draw', v });
export const AP = v => ({ k: 'ap', v });
export const TOUGH = (v, target = E1) => ({ k: 'tough', v, target });
export const F = (k, o = {}) => ({ k, ...o });

const SCALE = new Set(['dmg', 'shield', 'heal', 'extra']);
export function scale(fx, m) {
  return fx.map(f => {
    if (SCALE.has(f.k)) return { ...f, ratio: r2(f.ratio * m) };
    if (f.then) return { ...f, then: scale(f.then, m) };
    return { ...f };
  });
}

const COND = new Set(['ifBroken', 'ifTune', 'ifChain', 'ifStack', 'when', 'ifKill', 'ifBreak', 'ifWounded', 'ifChoice', 'ifRandom', 'ifHand', 'ifPile', 'ifNth', 'ifStreak', 'ifAllHeroes', 'ifFoe', 'ifCardSt', 'ifHp',
  'perStack', 'perTag', 'perPlayed', 'perPile', 'perEvent', 'perCardSt', 'perDebuff', 'perPaid', 'perApLeft']);
export const effects = fx => fx.filter(f => !COND.has(f.k)).length;

// 신탁 꼴 — base 카드를 받아 신탁 한 칸을 만든다
export const up = (name, m = 1.35) => b => ({ name, tags: b.tags, fx: scale(b.fx, m) });
export const cheap = (name, m = 1) => b => ({ name, cost: Math.max(0, b.cost - 1), tags: b.tags, fx: scale(b.fx, m) });
export const keep = (name, m = 1.2, tag = '보존') => b => ({ name, tags: [...(b.tags || []), tag], fx: scale(b.fx, m) });
export const plus = (name, piece, m = 1) => b => ({ name, tags: b.tags, fx: [...scale(b.fx, m), ...[].concat(piece)] });
export const O = (name, fx, o = {}) => b => ({ name, tags: o.tags ?? b.tags, ...(o.cost != null ? { cost: o.cost } : {}), ...(o.power ? { power: true } : {}), fx });

export const B = (name, kind) => ({ name, kind });
export const BF = (name, fx) => ({ name, fx });
export const BT = (name, tags) => ({ name, tags });

const problems = [];

/** 사도 하나를 JSON 으로 — spec: { id, hero, starters:[{name,type,cost,fx,n}], uniques:[{name,type,cost,tags,fx,oracles:[fn],blesses,choices,...}], tokens:[] } */
export function build(spec, outDir) {
  const id = spec.hero.id;
  const cards = [];
  const starter = [];
  spec.starters.forEach((s, i) => {
    const cid = `${id}_s${i + 1}`;
    cards.push({ id: cid, name: s.name, hero: id, cost: s.cost, type: s.type, fx: s.fx });
    for (let k = 0; k < (s.n || 1); k++) starter.push(cid);
  });
  spec.uniques.forEach((u, i) => {
    const cid = `${id}_u${i + 1}`;
    const base = { cost: u.cost, tags: u.tags || [], fx: u.fx };
    const c = { id: cid, name: u.name, hero: id, unique: true, cost: u.cost, type: u.type };
    if (u.tags && u.tags.length) c.tags = u.tags;
    if (u.choices) c.choices = u.choices;
    if (u.signature) c.signature = true;
    if (u.payWith) c.payWith = u.payWith;
    c.fx = u.fx;
    c.oracles = u.oracles.map(fn => { const o = fn(base); if (!o.tags || o.tags.length === 0) delete o.tags; return o; });
    c.blesses = u.blesses;
    if (u.blurb) c.blurb = u.blurb;
    cards.push(c);
    if (effects(c.fx) > 3) problems.push(`${cid} 효과 ${effects(c.fx)}`);
    c.oracles.forEach((o, j) => { if (effects(o.fx) > 3) problems.push(`${cid} 신탁${j + 1} 효과 ${effects(o.fx)}`); });
    if (c.oracles.length !== 5) problems.push(`${cid} 신탁 ${c.oracles.length}`);
    if ((c.blesses || []).length !== 3) problems.push(`${cid} 축복 ${(c.blesses || []).length}`);
  });
  for (const t of spec.tokens || []) cards.push({ ...t, hero: id, token: true });
  const hero = { ...spec.hero, race: '정령', starter };
  const file = path.join(outDir, `${id}.json`);
  if (REWORKED.includes(id)) console.log('건너뜀(리워크)', id); else fs.writeFileSync(file, JSON.stringify({ heroes: [hero], cards }, null, 1) + '\n', 'utf8');
  return file;
}

export function report() { if (problems.length) console.log('문제:\n' + problems.join('\n')); else console.log('효과 수 · 신탁 수 · 축복 수 문제 없음'); }
