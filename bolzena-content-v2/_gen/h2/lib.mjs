import fs from 'fs';
export const P='C:/projects/bolzena-content-v2/heroes/수인/';
const COND = new Set(['ifBroken','ifTune','ifChain','ifStack','perStack','ifKill','ifBreak','ifWounded','perTag','ifChoice','ifRandom','ifHand','ifPile','ifNth','ifStreak','ifAllHeroes','ifFoe','ifCardSt','perPlayed','perPile','perCardSt','perEvent','when','ifHp']);
const effects = (fx) => fx.filter((f) => !COND.has(f.k)).length;
const r2 = (x) => Math.round(x * 20) / 20;
export function scale(fx, k, bump) {
  let any = false;
  const out = fx.map((f) => { if (['dmg','shield','heal','extra'].includes(f.k)) { any = true; return { ...f, ratio: r2(f.ratio * k) }; } return { ...f }; });
  if (!any || bump) { const i = out.findIndex((f) => (f.k==='status'||f.k==='stack'||f.k==='draw') && f.id !== '기절'); if (i >= 0) out[i] = { ...out[i], v: out[i].v + 1 }; }
  return out;
}
export function oracles(card, theme) {
  const { fx, cost, type } = card; const tags = card.tags || []; const n = card.name; const attack = type === '공격'; const o = [];
  o.push({ name: `힘준 ${n}`, tags, fx: scale(fx, 1.3, true) });
  if (cost >= 1) o.push({ name: `가벼운 ${n}`, cost: cost - 1, tags, fx: scale(fx, 0.9) });
  else o.push({ name: `쥐고 있는 ${n}`, tags: [...new Set([...tags, '보존'])], fx: scale(fx, 1.25) });
  const t = theme.add; const same = fx.findIndex((f) => f.k === t.k && f.id && f.id === t.id && f.target === t.target); let f3;
  if (same >= 0) { f3 = fx.map((f) => ({ ...f })); f3[same].v += t.v; f3 = scale(f3, 1.2); }
  else if (effects(fx) < 3) f3 = [...scale(fx, 1.2), t]; else f3 = scale(fx, 1.3, true);
  o.push({ name: `${theme.word} ${n}`, tags, fx: f3 });
  if (attack) o.push({ name: `재빠른 ${n}`, tags: [...new Set([...tags, '신속'])], fx: scale(fx, 1.25) });
  else o.push({ name: `든든한 ${n}`, tags: type === '강화' ? tags : [...new Set([...tags, '보존'])], fx: scale(fx, 1.25, true) });
  if (attack) o.push({ name: `꿰뚫는 ${n}`, tags: [...new Set([...tags, '약점 공격'])], fx: scale(fx, 1.2) });
  else o.push({ name: `복 받은 ${n}`, tags: [...new Set([...tags, '축복'])], fx: scale(fx, 1.25, true) });
  return o.map((x) => { const y = { name: x.name }; if (x.cost !== undefined) y.cost = x.cost; if (x.tags && x.tags.length) y.tags = x.tags; y.fx = x.fx; return y; });
}
export function load(k){return JSON.parse(fs.readFileSync(P+k+'.json','utf8'));}
export function save(k,j){fs.writeFileSync(P+k+'.json',JSON.stringify(j,null,2)+'\n');}
// 고유 카드 바꾸기 — fx(·cost·tags) 를 바꾸고 신탁을 다시 짓는다(테마 = 셋째 축복 · 셋째 신탁 이름 앞말)
export function setCard(j, id, patch) {
  const c = j.cards.find((x) => x.id === id); if (!c) throw new Error('no card '+id);
  const o3 = c.oracles ? c.oracles[2].name : ''; const word = o3.endsWith(' ' + c.name) ? o3.slice(0, o3.length - c.name.length - 1) : o3.split(' ')[0];
  Object.assign(c, patch);
  if (c.unique) {
    const theme = { word, add: c.blesses[2].fx[0] };
    c.oracles = oracles(c, theme);
  }
  return c;
}
export const hero=(j)=>j.heroes[0];
