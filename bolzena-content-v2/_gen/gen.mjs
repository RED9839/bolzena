// H1 — 요정 22명 생성기. node gen.mjs → C:\projects\bolzena-content-v2\heroes\요정\<사도키>.json
import fs from 'fs';
import path from 'path';
const design = (await import('file:///C:/projects/볼제나/js/data/design.js')).default.heroes;
const OUT = 'C:/projects/bolzena-content-v2/heroes/요정';
fs.mkdirSync(OUT, { recursive: true });

// ── 조각 ──
const D = (r, t = 'oneEnemy', o = {}) => ({ k: 'dmg', ratio: r, target: t, ...o });
const DD = (r, t = 'oneEnemy', o = {}) => D(r, t, { base: 'def', ...o });
const SH = r => ({ k: 'shield', ratio: r });
const HE = r => ({ k: 'heal', ratio: r });
const ST = (id, v, t) => (t ? { k: 'status', id, v, target: t } : { k: 'status', id, v });
const K = (id, v, t) => (t ? { k: 'stack', id, v, target: t } : { k: 'stack', id, v });
const SPA = (id, t) => (t ? { k: 'spend', id, all: true, target: t } : { k: 'spend', id, all: true });
const SPV = (id, v, t) => (t ? { k: 'spend', id, v, target: t } : { k: 'spend', id, v });
const IS = (id, n, o = {}) => ({ k: 'ifStack', id, n, ...o });
const PS = id => ({ k: 'perStack', id });
const DR = v => ({ k: 'draw', v });
const AP = v => ({ k: 'ap', v });
const ATK = v => ({ k: 'atkMod', v, run: true, target: 'self' });
const DEF = v => ({ k: 'defMod', v, run: true, target: 'self' });
const CH = n => ({ k: 'ifChoice', n });
const MK = (id, v = 1, to) => (to ? { k: 'make', id, v, to } : { k: 'make', id, v });
const TG = (v, t) => (t ? { k: 'tough', v, target: t } : { k: 'tough', v });
const W = on => ({ k: 'when', on });
const P = (name, when, fx, o = {}) => ({ name, when, ...o, fx });
const T1 = { per: 'turn', n: 1 }, T2 = { per: 'turn', n: 2 }, T3 = { per: 'turn', n: 3 };

// ── 신탁 · 축복 자동 ──
const isEff = k => !(k.startsWith('if') || k === 'when' || k.startsWith('per'));
const effN = fx => fx.filter(f => isEff(f.k)).length;
const r20 = x => Math.round(x * 20) / 20;
const hasRatio = fx => fx.some(f => ['dmg', 'shield', 'heal', 'extra'].includes(f.k));
function scale(fx, m) {
  const out = fx.map(f => ({ ...f }));
  if (hasRatio(out)) { for (const f of out) if (['dmg', 'shield', 'heal', 'extra'].includes(f.k)) f.ratio = r20(f.ratio * m); }
  else if (m >= 1) { const f = out.find(f => ['status', 'stack', 'draw', 'atkMod', 'defMod'].includes(f.k)); if (f) { if (f.k === 'atkMod' || f.k === 'defMod') f.v = r20(f.v + 0.05) ; else f.v += 1; } }
  return out;
}
const BOOST = fs.existsSync('boost.json') ? JSON.parse(fs.readFileSync('boost.json', 'utf8')) : {};
function bump(fx, b) {
  if (!b) return fx;
  let out = fx.map(f => ({ ...f }));
  for (const f of out) if (['dmg', 'shield', 'heal', 'extra'].includes(f.k)) f.ratio = r20(f.ratio * (1 + 0.1 * b));
  if (b >= 2 || !hasRatio(out)) { const f = out.find(f => ['status', 'stack', 'draw'].includes(f.k) && f.v > 0 && !['기절'].includes(f.id)); if (f) f.v += Math.ceil(b / 2); }
  return out;
}
function oracles(c, h) { const o = oracles0(c, h); o.forEach((x, i) => { x.fx = bump(x.fx, BOOST[`${c.id}#${i + 1}`] || 0); }); return o; }
function addSyn(fx, sy) {
  const same = fx.find(f => f.k === sy.k && f.id === sy.id && (f.target || '') === (sy.target || ''));
  if (same) { same.v += sy.v; return fx; }
  return [...fx, sy];
}
function oracles0(c, h) {
  const adj = h.adj;
  const n = c.noun;
  const tags0 = (c.tags || []).filter(t => t !== '보존' && t !== '축복');
  const keep = (c.tags || []).includes('보존');
  const o = [];
  // ① 세게
  o.push({ name: `${adj[0]} ${n}`, tags: c.tags || [], fx: scale(c.fx, 1.3) });
  // ② 가볍게(코스트 -1) — 0코 · X 는 세게
  if (!c.x && c.cost >= 1) o.push({ name: `${adj[1]} ${n}`, cost: c.cost - 1, tags: c.tags || [], fx: scale(c.fx, c.cost === 1 ? 0.75 : 0.8) });
  else o.push({ name: `${adj[1]} ${n}`, tags: c.tags || [], fx: scale(c.fx, 1.4) });
  // ③ 사도다운 덤(고유 효과)
  if (c.o3) o.push({ name: `${adj[2]} ${n}`, tags: c.o3.tags || c.tags || [], ...(c.o3.cost != null ? { cost: c.o3.cost } : {}), fx: c.o3.fx });
  else if (effN(c.fx) < 3 && h.syn) o.push({ name: `${adj[2]} ${n}`, tags: c.tags || [], fx: addSyn(scale(c.fx, 1.1), h.syn(c)) });
  else o.push({ name: `${adj[2]} ${n}`, tags: c.tags || [], fx: scale(c.fx, 1.25) });
  // ④ 아껴 두기(보존)
  o.push({ name: `${adj[3]} ${n}`, tags: keep ? [...(c.tags || []), '신속'].filter((v, i, a) => a.indexOf(v) === i) : [...(c.tags || []), '보존'], fx: scale(c.fx, 1.2) });
  // ⑤ 축복
  o.push({ name: `${adj[4]} ${n}`, tags: [...tags0, ...(keep ? ['보존'] : []), '축복'], fx: scale(c.fx, 1.2) });
  return o;
}
function blesses(c) {
  const guardy = c.fx.some(f => f.k === 'shield'), healy = c.fx.some(f => f.k === 'heal');
  if (c.type === '공격') return [{ name: '세찬 숨결', kind: 'power', fx: [] }, { name: '틈새 노리기', kind: 'weakSpot', fx: [] }, { name: '번뜩이는 숨결', kind: 'draw', fx: [] }];
  if (c.type === '강화') return [{ name: '가벼운 숨결', kind: 'cost', fx: [] }, { name: '번뜩이는 숨결', kind: 'draw', fx: [] }, { name: '기세의 숨결', kind: 'atkUp', fx: [] }];
  return [{ name: healy ? '포근한 숨결' : guardy ? '단단한 숨결' : '넘치는 숨결', kind: healy ? 'heal' : guardy ? 'guard' : 'ap', fx: [] }, { name: '번뜩이는 숨결', kind: 'draw', fx: [] }, { name: '버티는 숨결', kind: 'defUp', fx: [] }];
}

// ── 시작 카드(기본 4장 — 피해 · 실드 · 치유만) ──
function starters(id) {
  const d = design[id];
  const ids = [], cards = [], seen = {};
  let n = 0;
  for (const s of d.start) {
    if (!seen[s.ko]) {
      n++;
      const cid = `${id}_s${n}`;
      const pct = Number((s.text.match(/(\d+)%/) || [])[1]) / 100;
      const fx = s.type === '공격' ? [D(pct)] : /회복/.test(s.text) ? [HE(pct)] : [SH(pct)];
      cards.push({ id: cid, name: s.ko, hero: id, cost: s.cost, type: s.type, fx });
      seen[s.ko] = cid;
    }
    ids.push(seen[s.ko]);
  }
  return { ids, cards };
}

// ── 사도 22 ──
const H = [];

// 1 에르핀(왕도) — 곧은 왕도(표적형)
H.push({ id: '에르핀_왕도', adj: ['곧은', '가뿐한', '왕도의', '아껴 둔', '대관식의'],
  keyword: { name: '왕도', desc: '여왕이 곧게 걷는 길 — 같은 적을 잇달아 칠수록 그 적이 무너진다', carrier: 'enemy', hunt: true, cap: 5, per: [{ stat: 'taken', v: 0.1 }],
    rules: [P('왕도의 끝', { on: 'huntDown' }, [ST('사기', 1)], { limit: T1 })] },
  passives: [P('곧은 왕도', { on: 'hit' }, [K('왕도', 1, 'oneEnemy')])],
  ult: { fx: [D(0.6, 'oneEnemy', { hits: 8 }), TG(2), K('왕도', 3, 'oneEnemy')] },
  syn: () => K('왕도', 1, 'oneEnemy'),
  uniques: [
    { name: '에르피엔 왕마력탄', noun: '왕마력탄', cost: 2, type: '공격', tags: ['약점 공격'], fx: [D(2.3), K('왕도', 1, 'oneEnemy')] },
    { name: '마력 난타', noun: '난타', cost: 0, x: true, type: '공격', fx: [D(0.75, 'oneEnemy', { xHits: true }), PS('왕도'), D(0.25)] },
    { name: '축복의 왕관', noun: '왕관', cost: 1, type: '스킬', fx: [K('왕도', 2, 'oneEnemy'), ATK(0.15), DR(1)] },
    { name: '모두를 위한 무게', noun: '무게', cost: 2, type: '강화', fx: [ATK(0.2), ST('불굴', 1)] },
  ] });

// 2 마요(멋짐) — 최고의 수집품(카드 변화형 → 진열품 덤)
H.push({ id: '마요_멋짐', adj: ['번쩍이는', '가벼운', '진열된', '아끼는', '축복받은'],
  keyword: { name: '진열품', desc: '가장 아끼는 수집품 — 다음 피해 한 번이 세진다', carrier: 'self', cap: 1, consumeAll: true, per: [{ stat: 'dealt', v: 0.5 }] },
  passives: [P('최고의 수집품', { on: 'fightStart' }, [K('진열품', 1)]), P('자랑 이어 가기', { on: 'play', type: '스킬' }, [K('진열품', 1)], { limit: T2 })],
  ult: { fx: [D(1.0, 'allEnemies'), ST('약화', 1, 'allEnemies'), K('진열품', 1)] },
  syn: () => K('진열품', 1),
  uniques: [
    { name: '빨리 나 칭찬해줌.', noun: '칭찬', cost: 1, type: '스킬', tags: ['보존'], fx: [K('진열품', 1), DR(2)] },
    { name: '은방울꽃 종', noun: '은방울꽃 종', cost: 1, type: '공격', fx: [D(1.2), ST('약화', 1), TG(1)] },
    { name: '최강의 수집품임.', noun: '수집품', cost: 2, type: '공격', fx: [K('진열품', 1), D(0.55, 'randomEnemy', { hits: 6 })] },
    { name: '나만의 교주', noun: '교주', cost: 2, type: '강화', fx: [ATK(0.15), ST('사기', 1), K('진열품', 1)] },
  ] });

// 3 에르핀 — 무전취식(대가형)
H.push({ id: '에르핀', adj: ['배부른', '가벼운', '외상의', '몰래 둔', '왕관의'],
  keyword: { name: '외상', desc: '갚을 생각 없는 밥값 — 큰 카드를 낼 때마다 쌓이고, 처치하면 탕감돼 AP', carrier: 'self', cap: 2 },
  passives: [
    P('무전취식', { on: 'kill', mine: true }, [MK('에르핀_cake', 1), IS('외상', 1), SPA('외상'), AP(1)], { limit: T1 }),
    P('큰 주문', { on: 'play', minCost: 2 }, [K('외상', 1)]),
  ],
  ult: { fx: [D(2.0, 'allEnemies'), ST('피해 감소', 3), ST('약화', 2, 'allEnemies')] },
  syn: () => MK('에르핀_cake', 1),
  tokens: [{ id: '에르핀_cake', name: '케이크', cost: 0, type: '스킬', tags: ['소멸'], fx: [HE(0.6), DR(1)] }],
  uniques: [
    { name: '맨주먹 결계 부수기', noun: '맨주먹', cost: 3, type: '공격', debt: true, tags: ['분쇄'], fx: [{ k: 'strip' }, D(3.6)] },
    { name: '마력탄 폭주', noun: '마력탄 폭주', cost: 2, type: '공격', debt: true, fx: [D(0.6, 'randomEnemy', { hits: 5 }), { k: 'ifKill' }, DR(1)] },
    { name: '순수 케이크 공격!!!', noun: '케이크 공격', cost: 1, type: '공격', debt: true, tags: ['연계'], fx: [D(0.8), ST('피해 감소', 1)] },
    { name: '무한의 케이크', noun: '케이크 상자', cost: 1, type: '스킬', fx: [MK('에르핀_cake', 1), DR(1)] },
  ] });

// 4 캬롯 — 사탕수수 텃밭(생성 카드형)
H.push({ id: '캬롯', adj: ['무성한', '가벼운', '텃밭의', '아껴 둔', '햇살의'],
  keyword: { name: '텃밭', desc: '새싹이 자라는 동안 쌓이는 기운 — 새싹을 심으면 처음부터', carrier: 'self', cap: 3 },
  passives: [
    P('사탕수수 텃밭', { on: 'play', type: '스킬' }, [MK('캬롯_sprout', 1, 'draw'), SPA('텃밭')], { limit: T1 }),
    P('일등 정원사', { on: 'turnStart' }, [K('텃밭', 1)]),
  ],
  ult: { fx: [D(1.5, 'allEnemies'), AP(2), K('텃밭', 3)] },
  syn: () => K('텃밭', 1),
  tokens: [{ id: '캬롯_sprout', name: '새싹', cost: 0, type: '스킬', tags: ['소멸'], fx: [HE(0.8), IS('텃밭', 2), ST('사기', 1), IS('텃밭', 3), AP(1)] }],
  uniques: [
    { name: '탄산수액 발사', noun: '탄산수액', cost: 2, type: '스킬', fx: [ST('사기', 1), HE(2.0), K('텃밭', 2)] },
    { name: '특제 사탕수수 커피', noun: '커피', cost: 1, type: '스킬', tags: ['신속'], fx: [DR(2), K('텃밭', 1)] },
    { name: '당근 신선도 유지', noun: '당근', cost: 1, type: '스킬', fx: [SH(3.0), IS('텃밭', 1, { not: true }), K('텃밭', 2)] },
    { name: '내 정원에 놀러 올래?', noun: '정원 초대', cost: 2, type: '스킬', fx: [HE(3.0), MK('캬롯_sprout', 1), K('텃밭', 1)] },
  ] });

// 5 큐이 — 몰래 오이 심기(아군 연동형으로)
H.push({ id: '큐이', adj: ['싱싱한', '가벼운', '덩굴진', '절여 둔', '축복의'],
  keyword: { name: '오이', desc: '몰래 심은 오이 — 붙은 아군이 카드를 내면 한 입 먹고 떨어진다', carrier: 'hero', cap: 2 },
  passives: [P('몰래 오이 심기', { on: 'play', marked: '오이' }, [HE(0.4), DR(1), SPV('오이', 1, 'oneAlly')], { limit: T2 })],
  ult: { fx: [HE(6.0), K('오이', 2, 'otherAllies')] },
  syn: () => K('오이', 1, 'oneAlly'),
  uniques: [
    { name: '몰래 오이 심기', noun: '오이 심기', cost: 1, type: '스킬', tags: ['연계'], fx: [K('오이', 1, 'oneAlly'), DR(1)] },
    { name: '오이 오일', noun: '오이 오일', cost: 2, type: '스킬', fx: [HE(3.0), K('오이', 2, 'otherAllies'), ST('결의', 1)] },
    { name: '오이를 권하는 행렬', noun: '오이 행렬', cost: 2, type: '스킬', fx: [HE(2.5), ST('사기', 1), K('오이', 1, 'otherAllies')] },
    { name: '오이 샌드위치 공세', noun: '샌드위치', cost: 1, type: '공격', fx: [D(1.1), ST('약화', 1), K('오이', 1, 'oneAlly')] },
  ] });

// 6 리코타 — 풀코스(순서형)
const COURSE = '풀코스';
H.push({ id: '리코타', adj: ['진한', '가벼운', '코스의', '숙성한', '축복의'],
  keyword: { name: '코스', desc: '한 턴의 식탁 — 0코 → 1코 → 2코 이상 카드를 차례로 내면 완성', carrier: 'self', cap: 2, endClear: true,
    rules: [
      P(COURSE, { on: 'play', who: 'any', maxCost: 0 }, [K('코스', 1)], { conds: [{ c: 'stack', id: '코스', n: 1, not: true }] }),
      P(COURSE, { on: 'play', who: 'any', minCost: 1, maxCost: 1 }, [K('코스', 1)], { conds: [{ c: 'stack', id: '코스', n: 1 }] }),
      P(COURSE, { on: 'play', who: 'any', minCost: 2 }, [SH(1.5), DD(1.0), SPA('코스')], { conds: [{ c: 'stack', id: '코스', n: 2 }], limit: T1 }),
    ] },
  passives: [],
  ult: { fx: [DD(2.5), TG(2), SH(2.0)] },
  syn: () => K('코스', 1),
  uniques: [
    { name: '아뮤즈 부쉬', noun: '전채', cost: 0, type: '스킬', fx: [SH(1.2), K('코스', 1)] },
    { name: '다 조려버리겠습니다', noun: '조림', cost: 1, type: '공격', tags: ['분쇄'], fx: [DD(0.8), SH(1.0)] },
    { name: '리코타 풀코스', noun: '메인 요리', cost: 2, type: '스킬', fx: [SH(3.5), IS('코스', 1), DD(1.2)] },
    { name: '요리를 무시하지 마십시오', noun: '셰프의 자존심', cost: 2, type: '강화', fx: [DEF(0.2), ST('반격', 2)] },
  ] });

// 7 칸타 — 올인(대가형)
const ALLIN = '올인';
H.push({ id: '칸타', adj: ['회전하는', '가벼운', '올인한', '숨겨 둔', '행운의'],
  keyword: { name: '판돈', desc: '다음 턴 AP 를 건 판 — 이번 턴 안에 격파 · 처치하면 AP 로 돌려받는다', carrier: 'self', cap: 1, endClear: true,
    rules: [
      P(ALLIN, { on: 'kill', mine: true }, [AP(3), SPA('판돈')], { conds: [{ c: 'stack', id: '판돈', n: 1 }] }),
      P(ALLIN, { on: 'break', mine: true }, [AP(3), SPA('판돈')], { conds: [{ c: 'stack', id: '판돈', n: 1 }] }),
    ] },
  passives: [],
  ult: { fx: [D(3.0), TG(2), AP(1)] },
  uniques: [
    { name: '올인', noun: '올인', cost: 2, type: '공격', choices: ['한 판', '올인'], fx: [D(2.6), CH(2), { k: 'nextAp', v: -1 }, K('판돈', 1)],
      o3: { fx: [D(3.1), CH(2), { k: 'nextAp', v: -1 }, K('판돈', 1)] } },
    { name: '팽이 난타', noun: '팽이', cost: 1, type: '공격', choices: ['한 판', '올인'], fx: [D(0.7, 'oneEnemy', { hits: 2 }), CH(2), { k: 'nextAp', v: -1 }, K('판돈', 1)],
      o3: { fx: [D(0.8, 'oneEnemy', { hits: 2 }), CH(2), { k: 'nextAp', v: -1 }, K('판돈', 1)] } },
    { name: '탑스핀 블레이드', noun: '탑스핀', cost: 2, type: '공격', tags: ['약점 공격'], fx: [D(0.8, 'allEnemies'), D(1.5)] },
    { name: '소매 속 팽이', noun: '소매 속 팽이', cost: 1, type: '스킬', tags: ['신속'], fx: [DR(2), ST('잔광', 1)] },
  ] });
for (const c of H[H.length - 1].uniques) if (!c.o3) c.o3 = { fx: [...scale(c.fx, 1.1), TG(1)] };

// 8 파트라 — 민트 반죽(쌓아 터뜨리기)
H.push({ id: '파트라', adj: ['상큼한', '가벼운', '민트 듬뿍', '숙성한', '축복의'],
  keyword: { name: '민트', desc: '어떤 반죽에든 들어가는 민트 — 넷이 차면 구워진다', carrier: 'enemy', cap: 4,
    rules: [P('구워짐', { on: 'stackReach', id: '민트', n: 4 }, [D(1.5), ST('약화', 2), SPA('민트', 'oneEnemy')])] },
  passives: [P('민트 반죽', { on: 'hit' }, [K('민트', 1, 'oneEnemy')])],
  ult: { fx: [D(3.5), TG(2), K('민트', 3, 'oneEnemy')] },
  syn: () => K('민트', 1, 'oneEnemy'),
  uniques: [
    { name: '민트머겅!', noun: '민트머겅', cost: 3, type: '공격', fx: [D(3.0), K('민트', 2, 'oneEnemy'), ST('고통', 2)] },
    { name: '민트 반죽 치대기', noun: '반죽', cost: 2, type: '공격', fx: [D(1.0, 'oneEnemy', { hits: 2 }), ST('균열', 2), { k: 'ifChain' }, K('민트', 2, 'oneEnemy')] },
    { name: '중불까지 키워주세요', noun: '중불', cost: 2, type: '강화', fx: [ATK(0.2), K('민트', 2, 'allEnemies')] },
    { name: '샤샤와 소풍 도시락', noun: '소풍 도시락', cost: 1, type: '스킬', tags: ['연계'], fx: [K('민트', 2, 'oneEnemy'), HE(1.5)] },
  ] });

// 9 클로에 — 세바스티안 바느질(생성 카드형 · 결속)
H.push({ id: '클로에', adj: ['촘촘한', '가벼운', '한 땀의', '접어 둔', '축복의'],
  keyword: { name: '바늘땀', desc: '세바스티안에게 한 땀씩 꿰매는 권능 — 둘이면 천 조각', carrier: 'self', cap: 2,
    rules: [P('세바스티안 바느질', { on: 'stackReach', id: '바늘땀', n: 2 }, [SPA('바늘땀'), MK('클로에_cloth', 1)])] },
  passives: [P('재단사의 손', { on: 'play', type: '스킬' }, [K('바늘땀', 1)])],
  ult: { fx: [D(0.6, 'randomEnemy', { hits: 7 }), ST('둔화', 1, 'allEnemies'), MK('클로에_cloth', 2)] },
  syn: () => K('바늘땀', 1),
  tokens: [
    { id: '클로에_cloth', name: '천 조각', cost: 0, type: '스킬', tags: ['결속'], bondCard: '클로에_sebas', fx: [SH(0.6)] },
    { id: '클로에_sebas', name: '세바스티안', cost: 0, type: '공격', fx: [DD(0.8, 'allEnemies'), SH(0.8)] },
  ],
  uniques: [
    { name: '메리 고 라운드', noun: '회전목마', cost: 2, type: '스킬', fx: [SH(3.2), ST('반격', 1), K('바늘땀', 1)] },
    { name: '세바스티안 연타', noun: '연타', cost: 2, type: '공격', fx: [D(0.9, 'oneEnemy', { hits: 3 })] },
    { name: '건치 스마일', noun: '스마일', cost: 1, type: '스킬', fx: [SH(2.0), ST('피해 감소', 2)] },
    { name: '의지를 넘긴 날', noun: '의지', cost: 1, type: '강화', fx: [DEF(0.2), K('바늘땀', 1)] },
  ] });

// 10 로니 — 석양의 결투(표적형)
H.push({ id: '로니', adj: ['날랜', '가벼운', '현상수배', '물고 있던', '보안관의'],
  keyword: { name: '무법자', desc: '보안관이 점찍은 현상범 — 움직이기 직전 로니가 먼저 쏜다', carrier: 'enemy', hunt: true, cap: 1,
    rules: [
      P('다음 현상범', { on: 'fightStart' }, [K('무법자', 1, 'topEnemy')]),
      P('다음 현상범', { on: 'huntDown' }, [ST('사기', 1), K('무법자', 1, 'topEnemy')]),
    ] },
  passives: [P('석양의 결투', { on: 'foeActBefore' }, [D(1.0), TG(1)], { conds: [{ c: 'stack', id: '무법자', n: 1 }] })],
  ult: { fx: [D(0.6, 'oneEnemy', { hits: 6 }), ST('피해 감소', 3), K('무법자', 1, 'oneEnemy')] },
  syn: () => K('무법자', 1, 'oneEnemy'),
  uniques: [
    { name: '진압용 소닉붐', noun: '소닉붐', cost: 2, type: '공격', fx: [D(2.2), K('무법자', 1, 'oneEnemy'), ST('둔화', 1)] },
    { name: '석양이 질 때쯤', noun: '석양', cost: 1, type: '스킬', fx: [ATK(0.15), ST('잔광', 1), DR(1)] },
    { name: '부러진 리코더', noun: '리코더', cost: 2, type: '공격', fx: [D(1.1, 'oneEnemy', { hits: 2 }), IS('무법자', 1), ST('표식', 1)] },
    { name: '오래가는 막대사탕', noun: '막대사탕', cost: 1, type: '스킬', fx: [SH(2.0), ST('피해 감소', 1), K('무법자', 1, 'oneEnemy')] },
  ] });

// 11 스키아 — 지켜 온 침묵(손패형)
H.push({ id: '스키아', adj: ['벼락 같은', '가벼운', '참아 온', '깊이 묻은', '사제의'],
  keyword: { name: '침묵', desc: '참고 또 참은 말 — 둘이면 AP 가 된다', carrier: 'self', cap: 3,
    rules: [P('지켜 온 침묵', { on: 'stackReach', id: '침묵', n: 2 }, [SPV('침묵', 2), AP(1)], { limit: T1 })] },
  passives: [],
  ult: { fx: [D(0.6, 'oneEnemy', { hits: 4 }), ST('기절', 1), K('침묵', 2)] },
  uniques: [
    { name: '오래된 맹세', noun: '맹세', cost: 2, type: '공격', tags: ['신속', '보존'], fx: [D(2.0), IS('침묵', 1), ST('둔화', 1, 'allEnemies'), W('handEnd'), K('침묵', 1)] },
    { name: '…(끄덕)', noun: '끄덕임', cost: 1, type: '스킬', tags: ['신속', '보존'], fx: [DR(2), IS('침묵', 1), ST('둔화', 1, 'allEnemies'), W('handEnd'), K('침묵', 1)] },
    { name: '흐읍!?', noun: '흐읍', cost: 2, type: '공격', tags: ['보존'], fx: [D(1.2, 'allEnemies'), IS('침묵', 1), ST('둔화', 1, 'allEnemies'), W('handEnd'), K('침묵', 1)] },
    { name: '지켜 온 금기', noun: '금기', cost: 1, type: '강화', fx: [ATK(0.15), K('침묵', 2)] },
  ] });

// 12 네르 — 보모의 기도(아군 연동형)
H.push({ id: '네르', adj: ['든든한', '가벼운', '기도하는', '졸면서 둔', '세계수의'],
  keyword: { name: '기도', desc: '돌볼 아이를 지켜보며 모은 기도 — 적이 치기 직전 한 대를 무르게 한다', carrier: 'self', cap: 3,
    rules: [P('기도가 닿다', { on: 'spend', id: '기도' }, [ST('사기', 1)], { limit: T1 })] },
  keywords: [{ name: '돌볼 아이', desc: '네르가 돌보는 아군 — 이 아군이 카드를 내면 기도가 쌓인다', carrier: 'hero', cap: 1 }],
  passives: [
    P('보모의 기도', { on: 'play', marked: '돌볼 아이' }, [K('기도', 1)], { limit: T3 }),
    P('졸다가 기도', { on: 'foeActBefore', type: '공격' }, [SPV('기도', 1), ST('피해 감소', 1)], { conds: [{ c: 'stack', id: '기도', n: 1 }] }),
  ],
  ult: { fx: [DD(1.5, 'allEnemies'), ST('사기', 1), K('기도', 2)] },
  syn: () => K('기도', 1),
  uniques: [
    { name: '꿈으로 올리는 기도', noun: '꿈속 기도', cost: 1, type: '스킬', tags: ['신속'], fx: [K('돌볼 아이', 1, 'oneAlly'), K('기도', 1), SH(2.0)] },
    { name: '세계수의 계시', noun: '계시', cost: 3, type: '스킬', fx: [ST('사기', 1), ST('협공', 2), K('기도', 2)] },
    { name: '뒷골목 오함마', noun: '오함마', cost: 2, type: '공격', tags: ['분쇄'], fx: [DD(2.0), ST('약화', 1), K('기도', 1)] },
    { name: '여왕님 앞은 못 지나가요', noun: '가로막기', cost: 2, type: '스킬', fx: [ST('피해 감소', 3), SH(3.0), K('돌볼 아이', 1, 'oneAlly')] },
  ] });

// 13 폴랑 — 120도 포위(아군 연동형)
H.push({ id: '폴랑', adj: ['정렬된', '가벼운', '포위하는', '대기 중인', '왕국의'],
  keyword: { name: '포위', desc: '셋이서 둘러싼 적 — 한 턴에 셋이 차면 포위 완성', carrier: 'enemy', cap: 3, endClear: true,
    rules: [P('포위 완성', { on: 'stackReach', id: '포위', n: 3 }, [ST('취약', 1), ST('둔화', 1), DR(1)], { limit: T1 })] },
  passives: [P('120도 포위', { on: 'hit', who: 'any' }, [K('포위', 1, 'oneEnemy')])],
  ult: { fx: [D(1.2, 'allEnemies'), HE(1.8), ST('기절', 1)] },
  syn: () => K('포위', 1, 'oneEnemy'),
  uniques: [
    { name: '120도 삼각 편대', noun: '삼각 편대', cost: 1, type: '스킬', tags: ['연계'], fx: [K('포위', 2, 'oneEnemy'), ST('피해 감소', 1)] },
    { name: '일제 사격', noun: '일제 사격', cost: 2, type: '공격', fx: [DD(1.0, 'allEnemies'), K('포위', 1, 'allEnemies'), ST('약화', 1, 'allEnemies')] },
    { name: '요정 왕국에 경례', noun: '경례', cost: 3, type: '스킬', fx: [ST('사기', 1), HE(3.0), ST('협공', 1)] },
    { name: '경비대장 폴랑입니다', noun: '경비대장', cost: 2, type: '스킬', tags: ['개전'], fx: [SH(2.0), ST('협공', 1), AP(1)] },
  ] });

// 14 마요 — 움직이지 않는 수집품(쌓아 터뜨리기)
H.push({ id: '마요', adj: ['음침한', '가벼운', '마취된', '모셔 둔', '수집가의'],
  keyword: { name: '마취', desc: '독침에 굳어 가는 수집 후보 — 쌓일수록 무르고, 셋이면 굼떠진다', carrier: 'enemy', cap: 3, per: [{ stat: 'taken', v: 0.1 }],
    rules: [P('완전 마취', { on: 'stackReach', id: '마취', n: 3 }, [ST('둔화', 2)], { limit: T1 })] },
  passives: [P('움직이지 않는 수집품', { on: 'hit' }, [K('마취', 1, 'oneEnemy')])],
  ult: { fx: [D(0.5, 'randomEnemy', { hits: 8 }), K('마취', 2, 'allEnemies')] },
  syn: () => K('마취', 1, 'oneEnemy'),
  uniques: [
    { name: '수집의 법칙임.', noun: '법칙', cost: 3, type: '공격', fx: [D(1.3, 'oneEnemy', { hits: 3 }), K('마취', 2, 'oneEnemy')] },
    { name: '경쟁자 견제', noun: '견제', cost: 2, type: '공격', tags: ['분쇄'], fx: [D(2.2), ST('취약', 2)] },
    { name: '마취 독침 세례', noun: '독침 세례', cost: 2, type: '공격', fx: [D(0.6, 'randomEnemy', { hits: 5 }), K('마취', 1, 'allEnemies')] },
    { name: '흐흐…', noun: '흐흐', cost: 1, type: '스킬', fx: [ATK(0.15), K('마취', 2, 'oneEnemy'), DR(1)] },
  ] });

// 15 네르(빡침) — 분노의 공명(피격 반응형)
H.push({ id: '네르_빡침', adj: ['격노한', '가벼운', '빡친', '삭여 둔', '성전의'],
  keyword: { name: '빡침', desc: '교주와 여왕에게 손댄 놈을 향한 분노 — 맞을 때마다 쌓이고 다섯이면 터진다', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }] },
  passives: [P('분노의 공명', { on: 'hurt' }, [K('빡침', 1)])],
  ult: { fx: [D(2.5, 'allEnemies'), K('빡침', 3), ATK(0.15)] },
  syn: () => K('빡침', 1),
  uniques: [
    { name: '교주님한테서 손 떼!', noun: '손 떼', cost: 1, type: '공격', tags: ['연계'], fx: [D(0.75, 'oneEnemy', { hits: 2 }), IS('빡침', 5), TG(2), SPA('빡침')],
      o3: { fx: [D(0.8, 'oneEnemy', { hits: 2 }), IS('빡침', 4), TG(2), SPA('빡침')] } },
    { name: '거대 도끼창', noun: '도끼창', cost: 3, type: '공격', fx: [D(1.8, 'allEnemies'), IS('빡침', 5), TG(2, 'allEnemies'), SPA('빡침')],
      o3: { fx: [D(1.9, 'allEnemies'), IS('빡침', 4), TG(2, 'allEnemies'), SPA('빡침')] } },
    { name: '성전 선포', noun: '성전', cost: 2, type: '스킬', fx: [D(1.2, 'allEnemies'), ST('사기', 1)] },
    { name: '양익의 맹세', noun: '맹세', cost: 1, type: '강화', fx: [ATK(0.15), K('빡침', 3)] },
  ] });

// 16 에슈르(마도) — 연구 노트(더미 조작형)
H.push({ id: '에슈르_마도', adj: ['증폭된', '가벼운', '기록된', '덮어 둔', '마도의'],
  keyword: { name: '노트', desc: '사라진 마법의 연구 기록 — 소멸할 때마다 쌓이고, 셋이면 한 장을 되살린다', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }],
    rules: [P('증명 완료', { on: 'stackReach', id: '노트', n: 3 }, [{ k: 'pull', from: 'gone', to: 'top', n: 1 }])] },
  passives: [P('연구 노트', { on: 'exhaust' }, [K('노트', 1)])],
  ult: { fx: [D(1.5, 'allEnemies'), K('노트', 2), ATK(0.15)] },
  syn: () => K('노트', 1),
  uniques: [
    { name: '빵타지아', noun: '빵타지아', cost: 2, type: '공격', tags: ['소멸 2'], fx: [D(1.5, 'allEnemies'), ST('취약', 1, 'allEnemies')] },
    { name: '마력 레이저', noun: '레이저', cost: 3, type: '공격', tags: ['소멸'], fx: [D(4.4)] },
    { name: '억까의 시간은 끝', noun: '반격의 시간', cost: 1, type: '스킬', tags: ['소멸 2'], fx: [DR(2), K('노트', 1), ST('잔광', 1)] },
    { name: '연구 집중', noun: '연구', cost: 1, type: '강화', fx: [K('노트', 2), ATK(0.1)] },
  ] });

// 17 슈팡 — 과속 배달(순서형)
H.push({ id: '슈팡', adj: ['총알 같은', '가벼운', '과속한', '쟁여 둔', '배송의'],
  keyword: { name: '배송', desc: '한 턴에 몰아치는 과속 배달 — 신속 카드로 쌓고, 셋째 건에 소포가 날아간다', carrier: 'self', cap: 3, endClear: true,
    rules: [P('소포 투척', { on: 'stackReach', id: '배송', n: 3 }, [D(0.8, 'randomEnemy'), ST('다음 턴 드로우', 1), SPA('배송')], { limit: T1 })] },
  passives: [P('과속 배달', { on: 'play', who: 'any', tag: '신속' }, [K('배송', 1)])],
  ult: { fx: [D(1.5, 'allEnemies'), ST('약화', 2, 'allEnemies'), ST('피해 감소', 2)] },
  syn: () => K('배송', 1),
  uniques: [
    { name: '무책임 배달부', noun: '배달부', cost: 1, type: '스킬', tags: ['신속'], fx: [HE(1.8), ST('행동 둔화', 1), K('배송', 1)] },
    { name: '슈팡은⋯ 달리고 싶다!', noun: '질주', cost: 1, type: '스킬', tags: ['신속'], fx: [DR(2), K('배송', 1)] },
    { name: '케이크 위 코너링', noun: '코너링', cost: 2, type: '공격', tags: ['신속'], fx: [DD(1.0, 'allEnemies'), ST('약화', 1, 'allEnemies'), K('배송', 1)] },
    { name: '손가락 인사', noun: '손가락 인사', cost: 0, type: '스킬', tags: ['신속'], fx: [ST('약화', 2), K('배송', 1)] },
  ] });

// 18 마리 — 기폭 스위치(쌓아 터뜨리기)
H.push({ id: '마리', adj: ['큼직한', '가벼운', '도화선의', '숨겨 둔', '황금손의'],
  keyword: { name: '폭탄', desc: '저절로는 안 터지는 특제 폭탄 — 마리가 스킬을 쓰면 한꺼번에 터진다', carrier: 'enemy', cap: 5,
    rules: [P('가득 찬 폭탄', { on: 'stackReach', id: '폭탄', n: 5 }, [TG(2)])] },
  passives: [
    P('폭탄 설치', { on: 'hit' }, [K('폭탄', 1, 'oneEnemy')]),
    P('기폭 스위치', { on: 'play', type: '스킬' }, [PS('폭탄'), D(0.8, 'oneEnemy', { fixed: true }), SPA('폭탄', 'oneEnemy')]),
  ],
  ult: { fx: [D(1.8, 'allEnemies'), K('폭탄', 2, 'allEnemies'), ST('잔불', 1, 'allEnemies')] },
  syn: () => K('폭탄', 1, 'oneEnemy'),
  uniques: [
    { name: '폭탄 배달 왔어용~', noun: '폭탄 배달', cost: 2, type: '공격', fx: [D(1.6, 'allEnemies'), K('폭탄', 1, 'allEnemies')] },
    { name: '기폭 스위치', noun: '스위치', cost: 1, type: '스킬', fx: [ST('취약', 1), DR(1), K('폭탄', 1, 'oneEnemy')] },
    { name: '황금손의 추억', noun: '황금손', cost: 1, type: '스킬', tags: ['신속'], fx: [DR(2), K('폭탄', 1, 'oneEnemy')] },
    { name: '저도 여기 있어요!', noun: '존재감', cost: 1, type: '공격', tags: ['연계'], fx: [D(1.3), K('폭탄', 1, 'oneEnemy')] },
  ] });

// 19 카렌 — 생방송 구독자(자원형)
const SUB = '생방송 구독자';
H.push({ id: '카렌', adj: ['화제의', '가벼운', '떡상한', '예약된', '축복의'],
  keyword: { name: '구독자', desc: '다시 모으는 시청자 — 볼수록 힘이 나고 다섯마다 떡상, 쓰러질 듯하면 빠진다', carrier: 'self', cap: 10, per: [{ stat: 'dealt', v: 0.05 }],
    rules: [
      P(SUB, { on: 'fightStart' }, [K('구독자', 2)]),
      P(SUB, { on: 'kill', mine: true }, [K('구독자', 2)]),
      P(SUB, { on: 'break', mine: true }, [K('구독자', 2)]),
      P(SUB, { on: 'overheal' }, [K('구독자', 1)], { limit: T1 }),
      P(SUB, { on: 'lowHp', pct: 0.3 }, [SPV('구독자', 3)]),
      P('떡상', { on: 'stackReach', id: '구독자', n: 5 }, [ST('사기', 1), AP(1), SPV('구독자', 5)]),
    ] },
  passives: [P('켜진 방송', { on: 'play', type: '공격' }, [K('구독자', 1)])],
  ult: { fx: [HE(6.0), ST('사기', 1), K('구독자', 3)] },
  syn: () => K('구독자', 1),
  uniques: [
    { name: '어그로 댓글 추적 방송', noun: '추적 방송', cost: 2, type: '공격', fx: [D(2.8), ST('취약', 2), K('구독자', 1)] },
    { name: '엘튜브 생방송 마법쇼', noun: '마법쇼', cost: 2, type: '공격', fx: [D(0.7, 'randomEnemy', { hits: 4 }), K('구독자', 2)] },
    { name: '당근 치유', noun: '당근 케이크', cost: 2, type: '스킬', fx: [HE(4.2), K('구독자', 2)] },
    { name: '근본 생식 챌린지', noun: '챌린지', cost: 1, type: '스킬', fx: [K('구독자', 3), DR(1)] },
  ] });

// 20 죠안 — 형상의 교리(태세형 · 꿈결 → 심판 → 축복 순환). 옛 「서 있는 자리의 교리」(열 조건 · moveRow)는 사도 열을 걷어 내며 바꿈(2026-10-06)
const DOC = '형상의 교리';
const FORM = n => ({ conds: [{ c: 'stack', id: '형상', n, max: n }] });
H.push({ id: '죠안', adj: ['경건한', '가벼운', '목도한', '간직한', '교리의'],
  keyword: { name: '목도', desc: '교주 곁에서 보고 들은 것', carrier: 'self', cap: 3 },
  keywords: [{ name: '형상', desc: '꿈속 계시를 받던 사제가 걸어온 모습 — 꿈결 → 심판 → 축복 차례로 순환', carrier: 'self', cap: 3, wrap: true, stages: ['꿈결', '심판', '축복'] }],
  passives: [
    P(DOC, { on: 'fightStart' }, [K('형상', 1)]),
    P(DOC, { on: 'play' }, [DR(1)], { ...FORM(1), limit: T1 }),
    P(DOC, { on: 'play' }, [ST('사기', 1)], { ...FORM(2), limit: { per: 'fight', n: 3 } }),
    P(DOC, { on: 'play' }, [SH(0.4)], FORM(3)),
  ],
  ult: { fx: [DD(1.5, 'allEnemies'), SH(3.0), K('목도', 2)] },
  syn: () => K('목도', 1),
  uniques: [
    { name: '계시의 기도', noun: '기도', cost: 0, type: '스킬', fx: [K('목도', 1), K('형상', 1)],
      o3: { tags: ['보존'], fx: [K('목도', 2), K('형상', 1)] } },
    { name: '교리를 행하고', noun: '교리', cost: 3, type: '스킬', fx: [ST('사기', 2), ST('피해 감소', 3), PS('목도'), SH(1.0)] },
    { name: '무에서 빵을', noun: '빵', cost: 2, type: '스킬', fx: [HE(4.2), DR(2), K('목도', 1)] },
    { name: '사슬 심판', noun: '사슬', cost: 2, type: '공격', fx: [DD(1.5, 'allEnemies'), ST('약화', 1, 'allEnemies')] },
  ] });

// 21 샤샤 — 멋대로 텀블러(손패형)
H.push({ id: '샤샤', adj: ['콸콸', '가벼운', '수압 센', '담아 둔', '정령의'],
  keyword: { name: '수압', desc: '텀블러에 차오르는 물 — 물줄기에 실어 쏜다', carrier: 'self', cap: 3 },
  passives: [P('멋대로 텀블러', { on: 'turnEnd' }, [{ k: 'autoPlay', n: 1, id: '탄환' }, SH(0.5), K('수압', 1)])],
  ult: { fx: [D(0.5, 'oneEnemy', { hits: 6 }), ST('둔화', 1), K('수압', 2)] },
  syn: () => K('수압', 1),
  uniques: [
    { name: '텀블러 투척!', noun: '텀블러', cost: 2, type: '공격', tags: ['탄환'], fx: [D(0.5, 'allEnemies', { hits: 2 }), ST('약화', 1, 'allEnemies'), K('수압', 1)] },
    { name: '물줄기 발사', noun: '물줄기', cost: 3, type: '공격', tags: ['탄환'], fx: [D(0.6, 'oneEnemy', { hits: 5 }), PS('수압'), D(0.3), SPA('수압')] },
    { name: '물방울 연사', noun: '물방울', cost: 1, type: '공격', tags: ['탄환', '분쇄'], fx: [D(0.5, 'oneEnemy', { hits: 2 }), PS('수압'), D(0.2), SPA('수압')] },
    { name: '불 하나는 잘 꺼요', noun: '소화', cost: 1, type: '스킬', fx: [{ k: 'cleanse', v: 1 }, SH(2.0), K('수압', 1)] },
  ] });

// 22 에슈르 — 빵집 아니고 마법학교(카드 변화형 · 두 갈래)
H.push({ id: '에슈르', adj: ['갓 구운', '가벼운', '부푼', '발효시킨', '교장의'],
  keyword: { name: '빵 센디오', desc: '빵이냐 마법이냐 — 카드를 낼 때마다 부풀고, 셋이면 둘 다', carrier: 'self', cap: 3,
    rules: [P('빵과 마법', { on: 'stackReach', id: '빵 센디오', n: 3 }, [HE(1.0), ST('고통', 2), SPA('빵 센디오')])] },
  passives: [P('빵집 아니고 마법학교', { on: 'play' }, [K('빵 센디오', 1)])],
  ult: { fx: [D(3.0), D(1.0, 'allEnemies'), ST('기절', 1)] },
  uniques: [
    { name: '빵템피드', noun: '빵템피드', cost: 2, type: '공격', choices: ['빵', '마법'], fx: [D(0.35, 'oneEnemy', { hits: 6 }), CH(1), HE(1.5), CH(2), ST('고통', 3)] },
    { name: '입자 이론', noun: '입자 이론', cost: 1, type: '스킬', choices: ['빵', '마법'], fx: [ATK(0.15), CH(1), HE(2.0), CH(2), ST('고통', 3)] },
    { name: '화염 주문', noun: '화염 주문', cost: 3, type: '공격', choices: ['빵', '마법'], fx: [D(3.0), CH(1), HE(2.0), CH(2), ST('고통', 3, 'allEnemies')] },
    { name: '궁극의 그리모어', noun: '그리모어', cost: 1, type: '강화', fx: [ATK(0.1), K('빵 센디오', 2)] },
  ] });
for (const c of H[H.length - 1].uniques) if (!c.o3) c.o3 = { tags: [...(c.tags || []), '보존'], fx: scale(c.fx, 1.15) };
// 스키아는 효과가 이미 셋 — ③ 은 침묵 문턱을 낮춘다
for (const c of H.find(h => h.id === '스키아').uniques) if (c.fx.some(f => f.k === 'when')) c.o3 = { tags: c.tags, fx: c.fx.map(f => f.k === 'dmg' ? { ...f, ratio: r20(f.ratio * 1.2) } : f.k === 'draw' ? { ...f, v: f.v + 1 } : f) };

// ── 애착 장비(옛 것을 G 가 world 에서 뺐다 — 새 고유 효과에 맞춰 사도 파일로) ──
const EQ = {
  '에르핀': { id: 'eq_erpinstaff', name: '에르핀의 지팡이', grade: '전설', slot: '무기', stats: { atk: 24, hp: 40 },
    effect: [P('여왕의 마력', { on: 'turnStart' }, [{ k: 'gauge', v: 20 }], { conds: [{ c: 'hpMin', pct: 0.99 }] })],
    affinity: '에르핀', affinityStats: { hp: 40, atk: 10 },
    affinityEffect: [P('도시락 챙기기', { on: 'fightStart' }, [MK('에르핀_cake', 1)])],
    blurb: '요정 여왕의 마력 지팡이. 다치지 않은 날에는 마력이 넘친다.' },
  '네르': { id: 'eq_nerflag', name: '네르의 엘드르 깃발', grade: '전설', slot: '무기', stats: { hp: 120, atk: 12 },
    effect: [P('행사 깃발', { on: 'play', type: '스킬', every: 3 }, [HE(0.5)])],
    affinity: '네르', affinityStats: { hp: 60, def: 6 },
    affinityEffect: [P('깃발 아래 기도', { on: 'play', type: '스킬', every: 3 }, [K('기도', 1)])],
    blurb: '교단 행사 때 거는 깃발. 실은 도끼 덕일지도.' },
};

// ── 쓰기 ──
const names = new Set();
for (const h of H) {
  const d = design[h.id];
  if (!d) throw new Error('design 에 없음 ' + h.id);
  const st = starters(h.id);
  const cards = [...st.cards];
  h.uniques.forEach((u, i) => {
    const c = { id: `${h.id}_u${i + 1}`, name: u.name, hero: h.id, unique: true, cost: u.cost, type: u.type };
    if (u.x) c.x = true;
    if (u.debt) c.debt = true;
    if (u.tags) c.tags = u.tags;
    if (u.choices) c.choices = u.choices;
    c.fx = u.fx;
    u.id = c.id;
    c.oracles = oracles(u, h);
    c.blesses = blesses(u);
    cards.push(c);
  });
  for (const t of h.tokens || []) cards.push({ ...t, hero: h.id, token: true });
  const hero = {
    id: h.id, name: d.ko, nature: d.nature, race: d.race, role: d.role, star: d.star,
    hp: d.hp, atk: d.atk, def: d.def, crit: d.crit, blurb: d.blurb,
    keyword: h.keyword, ...(h.keywords ? { keywords: h.keywords } : {}),
    passives: h.passives,
    ult: { name: d.ult.ko, cost: d.ult.cost, fx: h.ult.fx },
    starter: st.ids,
  };
  const out = { heroes: [hero], cards };
  if (EQ[h.id]) out.equips = [EQ[h.id]];
  fs.writeFileSync(path.join(OUT, `${h.id}.json`), JSON.stringify(out, null, 1));
  names.add(h.id);
}
console.log('썼다', names.size, [...names].join(' '));
const miss = Object.entries(design).filter(([k, v]) => v.race === '요정' && !names.has(k)).map(([k]) => k);
console.log('빠진 요정', miss);
