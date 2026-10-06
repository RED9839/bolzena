import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const L = require('./lib.js');
const H = require('./heroes.js');
const design = (await import('file:///C:/projects/볼제나/js/data/design.js')).default.heroes;
const OUT = 'C:/projects/bolzena-content-v2/heroes/엘프';
const EQUIPS = {
  '알레트': [{ id: 'eq_aletteshovel', name: '알레트의 큰 삽', grade: '희귀', slot: '무기', stats: { atk: 6, def: 8 },
    effect: [{ name: '삽치기', when: { on: 'play', type: '공격', minCost: 2 }, limit: { per: 'turn', n: 1 }, fx: [L.SH(0.6)] }],
    affinity: '알레트', affinityStats: { hp: 40, def: 6 },
    affinityEffect: [{ name: '반장님 앞은 못 지나간다', when: { on: 'blocked' }, limit: { per: 'turn', n: 1 }, fx: [L.K('명령 수행', 1)] }],
    blurb: '방패와 짝을 이루는 진압반 하사의 큰 삽. 반장님 앞을 막을 때 가장 잘 든다.' }],
  '아멜리아': [{ id: 'eq_ameliaplanner', name: '비서의 업무 수첩', grade: '희귀', slot: '장신구', stats: { hp: 50, def: 6 },
    effect: [{ name: '일정 정리', when: { on: 'turnStart' }, conds: [{ c: 'firstTurn' }], fx: [L.DRAW(1)] }],
    affinity: '아멜리아', affinityStats: { hp: 40, def: 6 },
    affinityEffect: [{ name: '결재함 동기화', when: { on: 'fightStart' }, fx: [L.K('결재 서류', 2)] }],
    blurb: '시장님 일정이 빼곡히 적힌 수첩. 펼치면 오늘 할 일이 먼저 보인다.' }],
};
const fs = require('fs');
const BF = new URL('./boost.json', import.meta.url);
L.setBoost(fs.existsSync(BF) ? JSON.parse(fs.readFileSync(BF, 'utf8')) : {});
const only = process.argv[2];
for (const [key, s] of Object.entries(H)) {
  if (only && key !== only) continue;
  const d = design[key]; if (!d) throw new Error('design 없음 ' + key);
  const b = L.basics(key, d.role, d.start.map(c => c.ko).filter((v, i, a) => a.indexOf(v) === i));
  const h = { id: key, name: d.ko, nature: d.nature, race: d.race, role: d.role, star: d.star,
    hp: d.hp, atk: d.atk, def: d.def, crit: d.crit, blurb: s.blurb, keyword: s.kw };
  if (s.kws) h.keywords = s.kws;
  h.passives = (s.passives || []).map(p => { const q = { name: p.name, when: p.when }; if (p.conds) q.conds = p.conds; if (p.limit) q.limit = p.limit; q.fx = p.fx; return q; });
  h.ult = { name: d.ult.ko, cost: s.ult.cost, fx: s.ult.fx };
  h.starter = b.ids;
  const kwDefault = s.kwBless || (s.kw.carrier === 'self' ? L.K(s.kw.name, 1) : null);
  const uniques = s.u.map((u, i) => {
    const du = d.unique[i];
    const flash = (du.flash || []).map(f => f.ko);
    const bn = [0, 2, 4].map(k => s.bless[(i + k) % s.bless.length]);
    return { id: `${key}_u${i + 1}`, name: du.ko, cost: u.cost, type: u.type, tags: u.tags, choices: u.choices, fx: u.fx,
      oNames: flash, oSpecs: u.specs, bNames: bn, kwFx: u.noKwBless ? null : kwDefault, bFx: u.noKwBless ? null : kwDefault };
  });
  const obj = L.hero({ h, starters: b.starters, uniques, tokens: s.tokens || [] });
  if (EQUIPS[key]) obj.equips = EQUIPS[key];
  L.write(OUT, key, obj);
}
console.log('done', Object.keys(H).length);
