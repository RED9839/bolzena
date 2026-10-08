// 사도 리워크 구조 지표 — node metrics.mjs <데이터 폴더(heroes 가 든)> [--json]
// 신탁 갈래 유형(자동 분류 · 한 갈래가 여럿에 걸림) · 얕은 갈래 · 대가 · 강화화 · 서치 · 생성 장치 · 시동 카드 · 축복 덤 · 기본 카드 연료
import fs from 'fs'; import path from 'path';
const ARG = process.argv.slice(2).find(a => !a.startsWith('--') && !a.includes(',') && !/^[가-힣_A-Za-z0-9]+$/.test(a));
const ROOT = path.join(ARG || 'C:/projects/bolzena-content-v2', 'heroes');
// --heroes 파일이름,파일이름 — 그 사도들만(예: --heroes 네르,네르_빡침)
const HI = process.argv.indexOf('--heroes'), ONLY = HI > 0 ? new Set(process.argv[HI + 1].split(',')) : null;
const files = fs.readdirSync(ROOT).flatMap(d => fs.readdirSync(path.join(ROOT, d)).filter(f => !ONLY || ONLY.has(f.replace(/.json$/, ''))).map(f => path.join(ROOT, d, f)));
const T = { heroes: 0, cards: 0, oracles: 0, type: {}, shallowPerCard: [], cardsWith: {}, bless: {}, blessKw: 0, heroBlessKw: [], gen: 0, openerPower: 0, openerOpen: 0, opener2: 0, basicFuel: 0, fin1: 0, finPower: 0, twoCost: 0 };
const COND = new Set(['ifStack', 'ifKill', 'ifBreak', 'ifWounded', 'ifAllHeroes', 'ifFoe', 'when', 'ifHand', 'ifNth', 'ifStreak', 'ifShield', 'ifPrevSame', 'ifTypeNew', 'ifInHand', 'ifRandom', 'ifChoice']);
const PER = new Set(['perStack', 'perTag', 'perPlayed', 'perPile', 'perEvent', 'perCardSt', 'perDebuff']);
const sig = fx => JSON.stringify((fx || []).map(f => [f.k, f.id || '', f.target || '', f.hits || '', f.base || '', f.k === 'power' ? JSON.stringify((f.rules || []).map(r => r.when)) : '']));
const kinds = fx => new Set((fx || []).map(f => f.k));
const tgts = fx => (fx || []).filter(f => f.k === 'dmg').map(f => f.target || 'oneEnemy');
const inc = (o, k) => (o[k] = (o[k] || 0) + 1);
for (const f of files) {
  const j = JSON.parse(fs.readFileSync(f, 'utf8')); const h = j.heroes[0]; T.heroes++;
  const kwNames = [h.keyword, ...(h.keywords || [])].filter(Boolean).map(k => k.name);
  const tokens = j.cards.filter(c => c.token && !(h.forms || []).some(fm => Object.values(fm.cards || {}).includes(c.id)));
  if (tokens.length) T.gen++;
  const uniq = j.cards.filter(c => c.unique);
  const opener = uniq.find(c => h.starter.includes(c.id));
  if (opener) { if (opener.type === '강화') T.openerPower++; if ((opener.tags || []).includes('개전')) T.openerOpen++; if (opener.cost >= 2) T.opener2++; }
  const basics = new Set(h.starter.filter(id => !uniq.some(c => c.id === id)));
  const usesBasic = JSON.stringify([h.passives, h.keyword, h.keywords, uniq.map(c => [c.fx, c.oracles])]).match(/"basic":true|"who":"basic"/) || [...basics].some(id => JSON.stringify(uniq).includes(`"${id}"`));
  if (usesBasic) T.basicFuel++;
  const u4 = uniq.find(c => c.id.endsWith('_u4'));
  let hb = 0;
  for (const c of uniq) {
    T.cards++; if (c.cost === 2) T.twoCost++;
    let shallow = 0; const seen = new Set();
    for (const o of c.oracles || []) {
      T.oracles++;
      const t = []; const bk = kinds(c.fx), ok = kinds(o.fx);
      const same = sig(o.fx) === sig(c.fx);
      const ocost = o.cost ?? c.cost;
      if (o.power) t.push('강화화');
      if (ocost < c.cost) t.push('비용↓'); if (ocost > c.cost) t.push('비용↑');
      const bt = new Set(c.tags || []), ot = new Set(o.tags || []);
      if ([...ot].some(x => !bt.has(x))) t.push('태그+'); if ([...bt].some(x => !ot.has(x))) t.push('태그-');
      if (same && !o.power) t.push(ocost === c.cost && t.every(x => !x.startsWith('태그')) ? '수치만' : '수치+비용·태그');
      const diff = [...ok].filter(k => !bk.has(k)).length + [...bk].filter(k => !ok.has(k)).length;
      if (!same && !o.power && diff >= Math.max(2, Math.ceil(bk.size / 2))) t.push('재설계');
      else if (!same && !o.power) t.push('일부 바꿈');
      if ([...ok].some(k => COND.has(k)) && ![...bk].some(k => COND.has(k))) t.push('조건 덤');
      if ([...ok].some(k => PER.has(k)) && ![...bk].some(k => PER.has(k))) t.push('스케일링');
      if ((o.fx || []).some(x => x.k === 'pull' || (x.k === 'draw' && (x.who || x.type || x.tag || x.unique)))) t.push('서치');
      if (ok.has('make') && !bk.has('make')) t.push('생성');
      if (tgts(o.fx).includes('allEnemies') && !tgts(c.fx).includes('allEnemies')) t.push('광역화');
      // 대가: 태그 빼기 · 소멸/종극 붙이기 · 같은 비용인데 장치를 덜 씀 · 손패 버리기 · HP 치르기
      const spends = fx => (fx || []).filter(x => x.k === 'spend').length;
      if (t.includes('태그-') || (ot.has('소멸') && !bt.has('소멸')) || (ot.has('종극') && !bt.has('종극')) || (ok.has('discard') && !bk.has('discard')) || (ok.has('payHp') || ok.has('payHpPct')) || ok.has('burn') || (spends(o.fx) > spends(c.fx))) t.push('대가');
      for (const x of t) inc(T.type, x);
      for (const x of new Set(t)) seen.add(x);
      if (same || (!o.power && diff === 0)) shallow++;
    }
    for (const x of seen) inc(T.cardsWith, x);
    T.shallowPerCard.push(shallow);
    for (const b of c.blesses || []) {
      const k = b.kind ? b.kind : b.tags?.length ? '태그:' + b.tags.join('+') : (b.fx || []).map(x => x.k === 'status' ? x.id : x.k === 'stack' ? '고유 효과' : x.k).join('+');
      inc(T.bless, k); if (JSON.stringify(b).match(new RegExp(kwNames.map(n => `"${n.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}"`).join('|') || '^$'))) hb++;
      if (tokens.some(tk => JSON.stringify(b).includes(`"${tk.id}"`))) hb++;
    }
  }
  T.heroBlessKw.push(hb);
  if (u4) { if (u4.cost <= 1 && u4.type !== '강화') T.fin1++; if (u4.type === '강화') T.finPower++; }
}
const pct = (a, b) => (100 * a / b).toFixed(0) + '%';
const med = a => a.slice().sort((x, y) => x - y)[Math.floor(a.length / 2)];
const dist = a => { const d = [0, 0, 0, 0, 0, 0]; for (const x of a) d[Math.min(5, x)]++; return d.map((n, i) => `${i}:${n}`).join(' '); };
const out = {
  사도: T.heroes, 고유카드: T.cards, 신탁: T.oracles,
  '2코 고유': pct(T.twoCost, T.cards),
  '생성 카드 사도': `${T.gen} (${pct(T.gen, T.heroes)})`,
  '시동 = 강화': `${T.openerPower} (${pct(T.openerPower, T.heroes)})`, '시동 = 개전': `${T.openerOpen} (${pct(T.openerOpen, T.heroes)})`, '시동 2코 이상': T.opener2,
  '기본 카드 연료 사도(대략)': `${T.basicFuel} (${pct(T.basicFuel, T.heroes)})`,
  '④ 1코 마무리(강화 아님)': `${T.fin1} (${pct(T.fin1, T.heroes)})`, '④ 강화 카드': `${T.finPower} (${pct(T.finPower, T.heroes)})`,
  '얕은 갈래(카드당) 중앙 · 분포': `${med(T.shallowPerCard)} · ${dist(T.shallowPerCard)}`,
  '갈래 유형(갈래 %)': Object.fromEntries(Object.entries(T.type).sort((a, b) => b[1] - a[1]).map(([k, v]) => [k, `${v} (${pct(v, T.oracles)})`])),
  '갈래 유형(카드 % — 다섯 중 하나라도)': Object.fromEntries(Object.entries(T.cardsWith).sort((a, b) => b[1] - a[1]).map(([k, v]) => [k, pct(v, T.cards)])),
  '축복 덤 유형': Object.fromEntries(Object.entries(T.bless).sort((a, b) => b[1] - a[1]).slice(0, 25)),
  '축복 덤 종류 수': Object.keys(T.bless).length,
  '사도당 장치를 거드는 축복(중앙 · 0개 사도)': `${med(T.heroBlessKw)} · ${T.heroBlessKw.filter(x => x === 0).length}`,
};
console.log(process.argv.includes('--json') ? JSON.stringify(out) : JSON.stringify(out, null, 1));
