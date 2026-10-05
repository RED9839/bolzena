// H5 — 유령 17명 생성기. node gen.mjs → C:\projects\bolzena-content-v2\heroes\유령\<사도키>.json
import fs from 'fs';
import path from 'path';
import { pathToFileURL } from 'url';

const OUT = 'C:/projects/bolzena-content-v2/heroes/유령';
const design = (await import(pathToFileURL('C:/projects/볼제나/js/data/design.js').href)).default.heroes;

// ── 조각 ──
const dmg = (ratio, target = 'oneEnemy', o = {}) => ({ k: 'dmg', ratio, target, ...o });
const hits = (ratio, n, target = 'oneEnemy', o = {}) => ({ k: 'dmg', ratio, hits: n, target, ...o });
const shield = (ratio, o = {}) => ({ k: 'shield', ratio, ...o });
const heal = (ratio) => ({ k: 'heal', ratio });
const st = (id, v, target) => (target ? { k: 'status', id, v, target } : { k: 'status', id, v });
const stack = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const spendAll = (id) => ({ k: 'spend', id, all: true });
const spend = (id, v) => ({ k: 'spend', id, v });
const ifStack = (id, n = 1, o = {}) => ({ k: 'ifStack', id, n, ...o });
const perStack = (id) => ({ k: 'perStack', id });
const draw = (v) => ({ k: 'draw', v });
const ap = (v) => ({ k: 'ap', v });
const when = (on) => ({ k: 'when', on });
const make = (id, v = 1, to) => (to ? { k: 'make', id, v, to } : { k: 'make', id, v });
const R = (x) => Math.round(x * 20) / 20;

function scale(fx, m) {
  const out = fx.map((f) => ({ ...f }));
  const hasRatio = out.some((f) => f.ratio != null);
  for (const f of out) if (f.ratio != null && f.k !== 'drain') f.ratio = R(f.ratio * m);
  if (!hasRatio && m > 1) {
    const f = out.find((x) => ['status', 'stack', 'draw'].includes(x.k));
    if (f) f.v += 1;
  }
  return out;
}

const uniq = (a) => [...new Set(a)];

// 고유 카드 하나 — 신탁 다섯: ① 세게 ② 싸게 ③ 보존 ④ 손으로 쓴 것 ⑤ 축복
function U(key, n, c) {
  const tags = c.tags || [];
  const o = c.on; // 신탁 이름 다섯
  const cheap = c.cost >= 1
    ? { name: o[1], cost: c.cost - 1, tags, fx: scale(c.fx, c.cost === 1 ? 0.75 : 0.85) }
    : { name: o[1], tags: uniq([...tags, '개전']), fx: scale(c.fx, 1.3) };
  const oracles = [
    c.o1 || { name: o[0], tags, fx: scale(c.fx, 1.45) },
    c.o2 || cheap,
    c.o3 || { name: o[2], tags: uniq([...tags, '보존']), fx: scale(c.fx, 1.35) },
    c.o4,
    c.o5 || { name: o[4], tags: uniq([...tags, '축복']), fx: scale(c.fx, 1.3) },
  ].map((x, i) => ({ ...x, name: x.name || o[i] }));
  for (const x of oracles) if (x.tags && x.tags.length === 0) delete x.tags;
  const card = { id: `${key}_u${n}`, name: c.name, hero: key, unique: true, cost: c.cost, type: c.type };
  if (tags.length) card.tags = tags;
  if (c.choices) card.choices = c.choices;
  card.fx = c.fx;
  card.oracles = oracles;
  card.blesses = c.b.map(([name, kind, fx, btags]) => {
    const b = { name };
    if (kind) b.kind = kind;
    if (fx) b.fx = fx;
    if (btags) b.tags = btags;
    return b;
  });
  if (c.blurb) card.blurb = c.blurb;
  return card;
}

// 시작 카드 — 기본 4장(피해 · 실드 · 치유만)
function starters(key, role, names) {
  const [a, b, c2] = names;
  const cards = [{ id: `${key}_s1`, name: a, hero: key, cost: 1, type: '공격', fx: [dmg(1.2)] }];
  if (role === '서포터') {
    cards.push({ id: `${key}_s2`, name: b, hero: key, cost: 1, type: '스킬', fx: [heal(2.1)] });
    return { cards, starter: [`${key}_s1`, `${key}_s1`, `${key}_s2`, `${key}_s2`] };
  }
  if (role === '탱커') {
    cards.push({ id: `${key}_s2`, name: b, hero: key, cost: 1, type: '스킬', fx: [shield(2.0)] });
    return { cards, starter: [`${key}_s1`, `${key}_s1`, `${key}_s2`, `${key}_s2`] };
  }
  cards.push({ id: `${key}_s2`, name: b, hero: key, cost: 1, type: '스킬', fx: [shield(2.0)] });
  cards.push({ id: `${key}_s3`, name: c2, hero: key, cost: 2, type: '공격', fx: [dmg(2.4)] });
  return { cards, starter: [`${key}_s1`, `${key}_s1`, `${key}_s3`, `${key}_s2`] };
}

function hero(key, spec) {
  const d = design[key];
  const h = {
    id: key, name: d.ko, nature: d.nature, race: '유령', row: d.row, role: d.role, star: d.star,
    hp: d.hp, atk: d.atk, def: d.def, crit: d.crit, blurb: spec.blurb,
  };
  if (spec.keyword) h.keyword = spec.keyword;
  if (spec.keywords) h.keywords = spec.keywords;
  h.passives = spec.passives;
  h.ult = { name: d.ult.ko, cost: d.ult.cost, fx: spec.ult };
  const s = starters(key, d.role, spec.start);
  h.starter = s.starter;
  const cards = [...s.cards, ...spec.uniques.map((c, i) => U(key, i + 1, c)), ...(spec.tokens || [])];
  return { heroes: [h], cards };
}

const H = {};

// ═══ 스피키 — 서포터 · 순수 · 완벽한 따라쟁이(카드 변화형) ═══
H['스피키'] = {
  blurb: '남을 흉내 내 보는 이의 인식을 비트는 따라쟁이 유령. 다른 아군이 카드를 낼 때마다 「변장」 이 쌓이고, 스피키 카드는 변장을 써서 손에 든 아군 카드의 효과를 대신 꺼내 쓴다.',
  start: ['주문 발사', '사탕 나눠주기'],
  keyword: { name: '변장', desc: '방금 본 아군을 흉내 낸 어설픈 분장 — 보는 이에게는 완벽해 보인다', carrier: 'self', cap: 3 },
  passives: [
    { name: '완벽한 따라쟁이', when: { on: 'play', who: 'other' }, limit: { per: 'turn', n: 2 }, fx: [stack('변장', 1)] },
  ],
  ult: [st('피해 감소', 2), heal(3.0), stack('변장', 2)],
  uniques: [
    { name: '펌킨 매직', cost: 1, type: '스킬', fx: [shield(1.0), ifStack('변장'), { k: 'castOther' }, spend('변장', 1)],
      on: ['호박 마술쇼', '손장난 마술', '숨겨 둔 마술', null, '축복의 호박'],
      o1: { name: '호박 마술쇼', fx: [shield(1.6), ifStack('변장'), { k: 'castOther' }, spend('변장', 1)] },
      o4: { name: '두 번 흉내', fx: [shield(1.5), ifStack('변장'), { k: 'castOther' }] },
      b: [['호박 등불', 'guard'], ['잽싼 손', 'draw'], ['분장 고치기', null, [stack('변장', 1)]]] },
    { name: '사제장 대리', cost: 2, type: '스킬', fx: [st('사기', 1), st('피해 감소', 2), ifStack('변장'), st('협공', 1)],
      on: ['그럴듯한 설교', '짧은 설교', '대리 직함', null, '축복 기도'],
      o4: { name: '진짜인 척', fx: [st('사기', 2), st('피해 감소', 2), ifStack('변장'), st('협공', 1)] },
      b: [['엄숙한 목소리', 'defUp'], ['서두르는 사제', 'cost'], ['믿음의 증표', null, [st('결의', 1)]]] },
    { name: '호박 바구니', cost: 1, type: '스킬', fx: [heal(1.4), stack('변장', 1), draw(1)],
      on: ['꽉 찬 바구니', '작은 바구니', '아껴 둔 사탕', null, '축복의 사탕'],
      o4: { name: '친구 몫까지', tags: ['연계'], fx: [heal(1.4), stack('변장', 1), draw(1)] },
      b: [['큰 사탕', 'heal'], ['덤 사탕', 'draw'], ['호박 친구', null, [st('피해 감소', 1)]]] },
    { name: '한 번 맞아보실래요오?', cost: 2, type: '공격', fx: [dmg(1.4, 'allEnemies'), ifStack('변장'), { k: 'castOther', id: '공격' }, spend('변장', 1)],
      on: ['진심 펀치', '가벼운 펀치', '참았던 펀치', null, '축복 펀치'],
      o4: { name: '흉내 낸 필살기', fx: [dmg(1.8, 'allEnemies'), ifStack('변장'), { k: 'castOther', id: '공격' }] },
      b: [['험한 입', 'power'], ['얄미운 웃음', 'frost'], ['다시 한 번', 'draw']] },
  ],
};

// ═══ 사리 — 딜러 · 순수 · 최고의 조연(아군 연동형) ═══
H['사리'] = {
  blurb: '누가 무엇을 하든 맞장구치며 포즈까지 따라 하는 리액션 장인. 아군이 공격할 때마다 「맞장구」 가 쌓이고, 사리의 공격은 맞장구를 몽땅 털어 한 마디 촌철살인을 박는다.',
  start: ['낫 휘두르기', '흐릿해지기', '빙글빙글 사슬낫'],
  keyword: { name: '맞장구', desc: '아군이 칠 때마다 「그렇지!」 — 모아 두었다가 제 공격에 실어 보낸다', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }] },
  passives: [
    { name: '최고의 조연', when: { on: 'play', who: 'other', type: '공격' }, limit: { per: 'turn', n: 3 }, fx: [stack('맞장구', 1)] },
  ],
  ult: [dmg(4.0), { k: 'tough', v: 3 }, stack('맞장구', 3)],
  uniques: [
    { name: '장난스런 웃음', cost: 2, type: '공격', fx: [dmg(2.0), perStack('맞장구'), dmg(0.5), spendAll('맞장구')],
      on: ['깔깔 웃음', '킥킥 웃음', '참은 웃음', null, '축복 웃음'],
      o4: { name: '박수 갈채', fx: [dmg(1.9), perStack('맞장구'), dmg(0.6, 'allEnemies'), spendAll('맞장구')] },
      b: [['날 선 한마디', 'power'], ['맞장구 박자', 'draw'], ['찡긋', null, [stack('맞장구', 1)]]] },
    { name: '똑같은 포즈', cost: 1, type: '스킬', fx: [st('협공', 1), stack('맞장구', 2)],
      on: ['완벽한 포즈', '슬쩍 포즈', '굳힌 포즈', null, '축복 포즈'],
      o4: { name: '셋이 같은 포즈', fx: [st('협공', 1), stack('맞장구', 2), draw(1)] },
      b: [['거울 포즈', 'draw'], ['재빠른 포즈', 'cost'], ['단체 사진', null, [st('사기', 1)]]] },
    { name: '뼈 있는 맞장구', cost: 1, type: '스킬', tags: ['연계'], fx: [stack('맞장구', 1), st('취약', 1), draw(1)],
      on: ['뼈 때리기', '가벼운 맞장구', '아껴 둔 한마디', null, '축복의 맞장구'],
      o4: { name: '촌철살인', fx: [stack('맞장구', 1), st('취약', 2), draw(1)], tags: ['연계'] },
      b: [['콕 찌르기', 'frost'], ['맞받아치기', 'draw'], ['따라 웃기', null, [stack('맞장구', 1)]]] },
    { name: '셋으로 찢긴 수의', cost: 2, type: '공격', fx: [hits(0.75, 3), ifStack('맞장구', 2), st('사기', 1), spend('맞장구', 2)],
      on: ['펄럭이는 수의', '가벼운 수의', '접어 둔 수의', null, '축복의 수의'],
      o4: { name: '세 갈래 그림자', fx: [hits(0.6, 3, 'allEnemies'), ifStack('맞장구', 2), st('사기', 1), spend('맞장구', 2)] },
      b: [['찢긴 끝자락', 'power'], ['펄럭임', 'weakSpot'], ['잊힌 이름', null, [stack('맞장구', 1)]]] },
  ],
};

// ═══ 바롱 — 딜러 · 냉정 · 이간질(표적형) ═══
H['바롱'] = {
  blurb: '토끼 인형을 꿰매며 거짓말과 이간질을 즐기는 착각의 유령. 적 하나에게 「뒷담」 을 쌓아 기를 꺾고, 셋이 차면 그 적이 엉뚱한 동료를 치게 만든다.',
  start: ['유령 손톱', '으흐흐 흐릿해지기', '한 맺힌 할퀴기'],
  keyword: {
    name: '뒷담', desc: '바롱이 속삭인 거짓말 — 들은 적은 동료를 의심한다', carrier: 'enemy', hunt: true, cap: 3,
    per: [{ stat: 'dealt', v: -0.1 }],
    rules: [{ name: '이간질', when: { on: 'stackReach', id: '뒷담', n: 3 }, limit: { per: 'turn', n: 1 }, fx: [{ k: 'confuse', target: 'oneEnemy' }, st('약화', 1, 'oneEnemy')] }],
  },
  passives: [
    { name: '소문 퍼뜨리기', when: { on: 'huntDown', id: '뒷담' }, fx: [stack('뒷담', 2, 'topEnemy')] },
  ],
  ult: [dmg(1.5, 'allEnemies'), stack('뒷담', 3), st('피해 감소', 2)],
  uniques: [
    { name: '뒷담까기 인형', cost: 1, type: '공격', fx: [dmg(1.1), stack('뒷담', 1)],
      on: ['수다쟁이 인형', '작은 인형', '품에 둔 인형', null, '축복의 인형'],
      o4: { name: '두 배로 부풀리기', fx: [dmg(1.0), stack('뒷담', 2)] },
      b: [['날 선 바늘', 'power'], ['귓속말', 'frost'], ['한 땀 더', null, [stack('뒷담', 1)]]] },
    { name: '사람 사이 갈라놓기', cost: 2, type: '스킬', fx: [stack('뒷담', 2), { k: 'confuse' }, st('약화', 1)],
      on: ['깊은 이간', '짧은 이간', '묵혀 둔 이간', null, '축복의 이간'],
      o4: { name: '모두를 의심하게', fx: [stack('뒷담', 2), { k: 'confuse' }, st('약화', 2, 'allEnemies')] },
      b: [['그럴듯한 거짓말', 'draw'], ['재빠른 혀', 'cost'], ['흐린 눈', null, [st('피해 감소', 1)]]] },
    { name: '못 날리기', cost: 1, type: '공격', fx: [hits(0.55, 2), ifStack('뒷담'), st('취약', 1)],
      on: ['녹슨 못 세례', '못 하나', '아껴 둔 못', null, '축복의 못'],
      o4: { name: '저주받은 못', fx: [hits(0.55, 3), ifStack('뒷담'), st('취약', 1)] },
      b: [['날카로운 못', 'power'], ['약한 곳 찌르기', 'weakSpot'], ['뒤틀린 못', 'frost']] },
    { name: '토끼 인형 꿰매기', cost: 1, type: '스킬', fx: [shield(1.2), stack('뒷담', 1), draw(1)],
      on: ['촘촘한 바느질', '대충 꿰매기', '꿰매 둔 인형', null, '축복의 바느질'],
      o4: { name: '주인님이 보고 있다', fx: [shield(1.2), stack('뒷담', 2), draw(1)] },
      b: [['두꺼운 솜', 'guard'], ['재빠른 바늘', 'draw'], ['인형의 속삭임', null, [stack('뒷담', 1)]]] },
  ],
};

// ═══ 레테 — 탱커 · 냉정 · 번쩍 리셋(전환형) ═══
H['레테'] = {
  blurb: '레이저 포인터로 싫은 기억을 지우는 망각의 유령. 레테 카드를 낼 때마다 적의 버프 한 겹을 번쩍 지워 그만큼 실드로 바꾸고, 지운 것이 쌓이면 파티를 해로운 것에서 지킨다.',
  start: ['포인터 휘두르기', '유령 가드'],
  keyword: {
    name: '지운 기억', desc: '레이저로 지워 낸 기억 조각 — 레테가 실드로 바꿔 쥔다', carrier: 'self', cap: 5,
    per: [{ stat: 'guard', v: 0.06 }],
    rules: [{ name: '말끔한 망각', when: { on: 'stackReach', id: '지운 기억', n: 5 }, fx: [spendAll('지운 기억'), st('면역', 1), shield(1.0)] }],
  },
  passives: [
    { name: '번쩍 리셋', when: { on: 'play' }, limit: { per: 'turn', n: 2 }, fx: [{ k: 'dispel', v: 1 }, shield(0.25), stack('지운 기억', 1)] },
  ],
  ult: [dmg(2.5), st('약화', 2), stack('지운 기억', 2)],
  uniques: [
    { name: '뉴럴 링크 스따트', cost: 2, type: '스킬', fx: [shield(2.4), { k: 'cleanse', v: 2 }, stack('지운 기억', 2)],
      on: ['깊은 링크', '짧은 링크', '대기 중인 링크', null, '축복의 링크'],
      o4: { name: '병동 전체 리셋', fx: [shield(3.0), { k: 'cleanse', v: 2 }, st('면역', 2)] },
      b: [['두꺼운 막', 'guard'], ['빠른 접속', 'cost'], ['차분한 기억', null, [st('결의', 1)]]] },
    { name: '포인터 4연타', cost: 1, type: '공격', fx: [hits(0.3, 4), { k: 'dispel', v: 1 }],
      on: ['포인터 난타', '포인터 두 번', '쥐고 있던 포인터', null, '축복의 포인터'],
      o4: { name: '포인터 5연타', fx: [hits(0.3, 5), { k: 'dispel', v: 2 }, stack('지운 기억', 1)] },
      b: [['강한 출력', 'power'], ['눈부심', 'frost'], ['빠른 손목', 'draw']] },
    { name: '눈앞의 레이저', cost: 2, type: '공격', fx: [dmg(1.6), perStack('지운 기억'), shield(0.3), st('약화', 1)],
      on: ['초점 레이저', '약한 레이저', '충전된 레이저', null, '축복의 레이저'],
      o4: { name: '기억째 지우기', fx: [dmg(1.6), perStack('지운 기억'), shield(0.4), st('약화', 2)] },
      b: [['출력 최대', 'power'], ['눈앞이 하얘짐', 'frost'], ['반사판', 'guard']] },
    { name: '병상 곁', cost: 2, type: '강화', fx: [st('불굴', 1), st('결정화', 2), stack('지운 기억', 2)],
      on: ['밤샘 간호', '잠깐 들르기', '지키는 자리', null, '축복의 간호'],
      o4: { name: '아픈 기억은 두고', fx: [st('불굴', 1), st('결정화', 3), st('면역', 2)] },
      b: [['따뜻한 손', 'defUp'], ['서둘러 오기', 'cost'], ['조용한 자장가', null, [{ k: 'cleanse', v: 1 }]]] },
  ],
};

// ═══ 에스피 — 서포터 · 냉정 · 꿈 훔쳐보기(더미 조작형) ═══
H['에스피'] = {
  blurb: '남의 꿈을 훔쳐보며 대리만족하는 유령. 카드가 뽑힐 때마다 「꿈 일기」 를 적고, 셋이 차면 다음 장면을 미리 끌어온다. 꿈을 엿본 카드는 뽑히는 순간 적의 기운을 꺾는다.',
  start: ['촛불 튕기기', '달콤한 꿈'],
  keyword: {
    name: '꿈 일기', desc: '훔쳐본 꿈을 몰래 적어 둔 일기장', carrier: 'self', cap: 3,
    rules: [{ name: '다음 화 미리보기', when: { on: 'stackReach', id: '꿈 일기', n: 3 }, fx: [spendAll('꿈 일기'), draw(2)] }],
  },
  passives: [
    { name: '꿈 훔쳐보기', when: { on: 'drawn' }, limit: { per: 'turn', n: 2 }, fx: [stack('꿈 일기', 1)] },
  ],
  ult: [dmg(3.0), st('약화', 2), stack('꿈 일기', 3)],
  uniques: [
    { name: '헤롱헤롱 촛불', cost: 2, type: '공격', fx: [dmg(1.2, 'allEnemies'), when('drawAny'), st('약화', 1, 'allEnemies')],
      on: ['아찔한 촛불', '작은 촛불', '꺼지지 않는 촛불', null, '축복의 촛불'],
      o4: { name: '몽롱한 연기', fx: [dmg(1.2, 'allEnemies'), st('약화', 1, 'allEnemies'), when('drawAny'), st('약화', 1, 'allEnemies')] },
      b: [['큰 불꽃', 'power'], ['흔들리는 불빛', 'frost'], ['촛농 방울', 'draw']] },
    { name: '개꿈 발사!', cost: 1, type: '공격', fx: [dmg(1.1), stack('꿈 일기', 1), when('drawAny'), stack('꿈 일기', 1)],
      on: ['악몽 발사', '토막 꿈', '꿔 둔 꿈', null, '축복의 꿈'],
      o4: { name: '연속 개꿈', fx: [hits(0.6, 2), stack('꿈 일기', 1), when('drawAny'), stack('꿈 일기', 2)] },
      b: [['생생한 꿈', 'power'], ['깨기 직전', 'draw'], ['꿈속 비명', 'frost']] },
    { name: '꿈 훔쳐보기', cost: 1, type: '스킬', fx: [{ k: 'pull', from: 'discard', n: 1 }, stack('꿈 일기', 1), heal(0.8)],
      on: ['정주행', '살짝 엿보기', '저장해 둔 꿈', null, '축복의 꿈 일기'],
      o1: { name: '정주행', fx: [{ k: 'pull', from: 'discard', n: 2 }, stack('꿈 일기', 1), heal(1.0)] },
      o4: { name: '두 편 이어 보기', fx: [{ k: 'pull', from: 'discard', n: 1 }, stack('꿈 일기', 2), heal(1.0)] },
      b: [['편안한 꿈', 'heal'], ['빨리 감기', 'draw'], ['꿈 일기 한 줄', null, [stack('꿈 일기', 1)]]] },
    { name: '막말 가면', cost: 2, type: '공격', fx: [dmg(1.8), perStack('꿈 일기'), dmg(0.4), spendAll('꿈 일기')],
      on: ['독설 가면', '얇은 가면', '숨겨 둔 가면', null, '축복의 가면'],
      o4: { name: '가면 너머 진심', fx: [dmg(2.0), perStack('꿈 일기'), dmg(0.5), spend('꿈 일기', 2)] },
      b: [['날 선 혀', 'power'], ['가면 속 눈빛', 'weakSpot'], ['다시 소심', 'defUp']] },
  ],
};

// ═══ 셰이디 — 딜러 · 광기 · 역대 최고의 장난(쌓아 터뜨리기형) ═══
H['셰이디'] = {
  blurb: '역대 최고의 장난을 평생 소원으로 삼은 유령 우두머리. 공격할 때마다 적에게 「장난」 을 걸고, 셋이 다 걸린 적은 정신을 못 차리고 한 대 크게 얻어맞는다.',
  start: ['사슬낫 휘두르기', '순간이동', '사슬낫 회전베기'],
  keyword: {
    name: '장난', desc: '셰이디가 걸어 둔 짓궂은 장난 — 셋이 겹치면 대형 사고', carrier: 'enemy', cap: 3,
    rules: [{ name: '역대 최고의 장난', when: { on: 'stackReach', id: '장난', n: 3 }, fx: [st('기절', 1, 'oneEnemy'), dmg(1.5), spendAll('장난')] }],
  },
  passives: [
    { name: '장난질', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 2 }, fx: [stack('장난', 1), { k: 'ifRandom', pct: 0.5 }, st('둔화', 1)] },
  ],
  ult: [hits(0.7, 6, 'randomEnemy'), stack('장난', 2, 'allEnemies')],
  uniques: [
    { name: '불 좀 꺼줄래?', cost: 2, type: '공격', fx: [dmg(1.0, 'allEnemies'), st('둔화', 1, 'allEnemies')],
      on: ['정전 소동', '촛불 끄기', '꺼 둔 불', null, '축복의 어둠'],
      o4: { name: '깜깜한 늪', fx: [dmg(1.0, 'allEnemies'), st('둔화', 1, 'allEnemies'), stack('장난', 1, 'allEnemies')] },
      b: [['어둠 속 낫', 'power'], ['깜짝이야', 'frost'], ['슬쩍 빠지기', 'draw']] },
    { name: '사슬낫 네 자루', cost: 1, type: '공격', fx: [hits(0.35, 4), { k: 'ifRandom', pct: 0.5 }, st('약화', 1)],
      on: ['1~4호 총출동', '두 자루만', '아끼는 사슬낫', null, '축복의 사슬낫'],
      o4: { name: '사슬로 칭칭', fx: [hits(0.35, 4), stack('장난', 1), st('약화', 1)] },
      b: [['잘 간 날', 'power'], ['얽힌 사슬', 'weakSpot'], ['빠른 회수', 'draw']] },
    { name: '대롱대롱 매달기', cost: 2, type: '스킬', fx: [stack('장난', 2), st('취약', 2)],
      on: ['거꾸로 매달기', '살짝 매달기', '매달아 두기', null, '축복의 밧줄'],
      o4: { name: '빙글빙글 돌리기', fx: [stack('장난', 2), st('취약', 2), st('둔화', 1, 'allEnemies')] },
      b: [['튼튼한 밧줄', 'frost'], ['재빠른 매듭', 'cost'], ['깔깔깔', null, [st('사기', 1)]]] },
    { name: '차원 주머니', cost: 1, type: '스킬', fx: [draw(2), stack('장난', 1)],
      on: ['주머니 뒤집기', '주머니 슬쩍', '꽉 찬 주머니', null, '축복의 주머니'],
      o4: { name: '장난감 쏟아내기', fx: [draw(3), stack('장난', 1, 'allEnemies')] },
      b: [['깊은 주머니', 'draw'], ['빠른 손', 'cost'], ['깜짝 선물', null, [st('취약', 1)]]] },
  ],
};

// ═══ 앨리스 — 딜러 · 광기 · 아르카나 한 장(예고형) ═══
H['앨리스'] = {
  blurb: '타로로 남의 미래를 봐 주다 슬쩍 골려 먹는 점술사 유령. 턴마다 카드 한 장을 펼쳐 파티의 기세나 적의 불운을 점치고, 쌓인 「행운」 을 한 방에 몰아 쓴다.',
  start: ['카드 날리기', '운명 회피', '카드 한 벌 날리기'],
  keyword: { name: '행운', desc: '타로가 점지한 운 — 앨리스의 손끝을 날카롭게 한다', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.1 }] },
  passives: [
    { name: '아르카나 한 장', when: { on: 'turnStart' }, fx: [{ k: 'ifRandom', pct: 0.3 }, st('사기', 1), { k: 'ifRandom', pct: 0.4 }, st('취약', 1, 'allEnemies'), { k: 'ifRandom', pct: 0.5 }, stack('행운', 1)] },
  ],
  ult: [dmg(2.0, 'allEnemies'), st('기절', 1, 'randomEnemy'), stack('행운', 2)],
  uniques: [
    { name: '아르카나', cost: 2, type: '공격', fx: [dmg(1.0, 'allEnemies'), stack('행운', 1), { k: 'ifRandom', pct: 0.5 }, st('취약', 1, 'allEnemies')],
      on: ['대아르카나', '소아르카나', '뒤집어 둔 패', null, '축복의 패'],
      o4: { name: '운명의 수레바퀴', fx: [dmg(1.0, 'allEnemies'), stack('행운', 2), { k: 'ifRandom', pct: 0.5 }, st('취약', 1, 'allEnemies')] },
      b: [['날 선 카드', 'power'], ['불길한 패', 'frost'], ['한 장 더', 'draw']] },
    { name: '남의 불운', cost: 2, type: '공격', fx: [dmg(1.9), perStack('행운'), dmg(0.4), spendAll('행운')],
      on: ['최악의 점괘', '가벼운 불운', '미뤄 둔 불운', null, '축복의 불운'],
      o4: { name: '탑 카드', fx: [dmg(2.1), perStack('행운'), dmg(0.4), st('취약', 1)] },
      b: [['정확한 예언', 'power'], ['뒤틀린 운명', 'weakSpot'], ['액땜', 'defUp']] },
    { name: '비추는 거울', cost: 1, type: '스킬', fx: [stack('행운', 2), draw(1)],
      on: ['선명한 거울', '손거울', '덮어 둔 거울', null, '축복의 거울'],
      o4: { name: '거울 속 별', fx: [stack('행운', 2), draw(1), st('다음 턴 드로우', 1)] },
      b: [['맑은 거울', 'draw'], ['빠른 점', 'cost'], ['비친 미소', null, [st('사기', 1)]]] },
    { name: '밑장 빼기', cost: 1, type: '스킬', fx: [{ k: 'discard', v: 1 }, draw(2), stack('행운', 1)],
      on: ['능숙한 밑장', '살짝 밑장', '숨겨 둔 패', null, '축복의 밑장'],
      o4: { name: '패 바꿔치기', fx: [{ k: 'discard', v: 1 }, draw(3), stack('행운', 1)] },
      b: [['소매 속 패', 'draw'], ['재빠른 손', 'cost'], ['딱 맞는 패', null, [stack('행운', 1)]]] },
  ],
};

// ═══ 림_혼돈 — 딜러 · 광기 · 어엿한 혼돈(대가형) ═══
H['림_혼돈'] = {
  blurb: '혼돈의 빈자리를 대신 떠안은 질서의 유령. 공격은 과녁을 고르지 못하고 아무나 베지만 그만큼 세다. 적이 여럿일 때 「혼돈」 이 차오르면 한 번에 다 쓸어 버린다 — 맞으면 꿀밤 한 방에 정신이 돌아온다.',
  start: ['혼돈의 낫', '그림자 숨기', '혼돈의 대낫'],
  keyword: { name: '혼돈', desc: '어설프게 흉내 낸 혼돈의 기운 — 꿀밤 한 방이면 흩어진다', carrier: 'self', cap: 3 },
  passives: [
    { name: '어엿한 혼돈', when: { on: 'play', type: '공격' }, conds: [{ c: 'foes', n: 2 }], fx: [stack('혼돈', 1)] },
    { name: '꿀밤', when: { on: 'hurt' }, fx: [spendAll('혼돈')] },
  ],
  ult: [dmg(3.0, 'randomEnemy'), st('기절', 1, 'randomEnemy'), stack('혼돈', 2)],
  uniques: [
    { name: '혼돈에 이끌림', cost: 2, type: '공격', fx: [hits(0.75, 3, 'randomEnemy'), ifStack('혼돈', 3), dmg(1.0, 'allEnemies'), spendAll('혼돈')],
      on: ['소용돌이 혼돈', '작은 혼돈', '참아 둔 혼돈', null, '축복의 혼돈'],
      o4: { name: '혼돈의 폭주', fx: [hits(0.75, 4, 'randomEnemy'), ifStack('혼돈', 3), dmg(1.0, 'allEnemies'), spendAll('혼돈')] },
      b: [['날뛰는 낫', 'power'], ['흐트러진 기운', 'frost'], ['다시 혼돈', null, [stack('혼돈', 1)]]] },
    { name: '차원 베기', cost: 2, type: '공격', tags: ['분쇄'], fx: [dmg(2.6, 'randomEnemy'), stack('혼돈', 1)],
      on: ['차원 가르기', '얕은 베기', '벼려 둔 낫', null, '축복의 베기'],
      o4: { name: '틈새 베기', tags: ['분쇄', '약점 공격'], fx: [dmg(2.8, 'randomEnemy'), stack('혼돈', 1)] },
      b: [['예리한 날', 'power'], ['찢어진 공간', 'weakSpot'], ['빠른 회수', 'draw']] },
    { name: '무서워하십시오!', cost: 1, type: '스킬', fx: [stack('혼돈', 2), st('약화', 1, 'allEnemies')],
      on: ['진짜 무서운 얼굴', '살짝 무서운 얼굴', '연습한 얼굴', null, '축복의 위협'],
      o4: { name: '혼돈의 품위', fx: [stack('혼돈', 2), st('약화', 1, 'allEnemies'), st('사기', 1)] },
      b: [['큰 목소리', 'frost'], ['재빠른 위협', 'cost'], ['어설픈 웃음', 'draw']] },
    { name: '꿀밤', cost: 1, type: '스킬', fx: [perStack('혼돈'), dmg(0.8, 'randomEnemy'), spendAll('혼돈'), draw(1)],
      on: ['정신 번쩍', '톡 치기', '아껴 둔 꿀밤', null, '축복의 꿀밤'],
      o4: { name: '정신 차리고 정리', fx: [perStack('혼돈'), dmg(0.9, 'allEnemies'), spendAll('혼돈'), draw(1)] },
      b: [['단단한 주먹', 'power'], ['맑은 정신', 'draw'], ['질서 회복', 'defUp']] },
  ],
};

// ═══ 메죵 — 딜러 · 광기 · 여기가 내 집(표적형) ═══
H['메죵'] = {
  blurb: '어디든 들어가면 거기가 내 집이 되는 태평한 유령. 적 하나에 「입주」 해 눌러앉고, 적이 실드를 세우면 제 집 담장인 양 파티가 나눠 쓴다. 집주인이 쓰러지면 짐 싸서 다음 집으로.',
  start: ['수리검', '이불 속으로', '왕수리검'],
  keyword: { name: '입주', desc: '메죵이 눌러앉은 집 — 집주인은 영 편하지 않다', carrier: 'enemy', hunt: true, cap: 1, per: [{ stat: 'taken', v: 0.45 }] },
  passives: [
    { name: '여기가 내 집', when: { on: 'foeGuard' }, limit: { per: 'turn', n: 1 }, fx: [shield(1.0)] },
    { name: '이사', when: { on: 'huntDown', id: '입주' }, fx: [draw(1), stack('입주', 1, 'topEnemy')] },
  ],
  ult: [dmg(4.5), { k: 'tough', v: 2 }, stack('입주', 1)],
  uniques: [
    { name: '수리검 날아갑니다!', cost: 2, type: '공격', fx: [hits(0.75, 3), stack('입주', 1)],
      on: ['수리검 소나기', '수리검 두 개', '모아 둔 수리검', null, '축복의 수리검'],
      o4: { name: '집들이 선물', fx: [hits(0.75, 3), stack('입주', 1), draw(1)] },
      b: [['날 선 수리검', 'power'], ['빈틈 노리기', 'weakSpot'], ['굴러온 수리검', 'draw']] },
    { name: '집문서 내놔', cost: 2, type: '공격', tags: ['분쇄'], fx: [dmg(2.4), { k: 'strip' }, { k: 'ifKill' }, ap(1)],
      on: ['강제 집행', '문서 흔들기', '품에 든 문서', null, '축복의 문서'],
      o4: { name: '부동산 뒤엎기', tags: ['분쇄'], fx: [dmg(3.0), { k: 'strip' }, { k: 'ifKill' }, ap(1)] },
      b: [['억지 도장', 'power'], ['빈틈 찾기', 'weakSpot'], ['빠른 서명', 'cost']] },
    { name: '눌러앉기', cost: 1, type: '스킬', fx: [stack('입주', 1), shield(1.4), draw(1)],
      on: ['드러눕기', '살짝 앉기', '이불 깔아 두기', null, '축복의 방석'],
      o4: { name: '내 방 꾸미기', fx: [stack('입주', 1), shield(2.0), st('실드 유지', 1)] },
      b: [['두툼한 이불', 'guard'], ['재빠른 짐 풀기', 'draw'], ['태평한 하품', 'defUp']] },
    { name: '제가 주인인 양', cost: 1, type: '스킬', fx: [stack('입주', 1), st('취약', 2)],
      on: ['당당한 주인 행세', '슬쩍 주인 행세', '익숙한 주인 행세', null, '축복의 집'],
      o4: { name: '문패 바꿔 달기', fx: [stack('입주', 1), st('취약', 2), draw(1)] },
      b: [['큰소리', 'frost'], ['빠른 이사', 'cost'], ['집주인 미소', null, [st('사기', 1)]]] },
  ],
};

// ═══ 벨라 — 탱커 · 활발 · 존재의 증명(태세형) ═══
H['벨라'] = {
  blurb: '소설 속 주인공이 현실로 걸어 나온 유령 소녀. 파티가 위태로울수록 「존속」 이 쌓여 실드도 공격도 단단해지고, 셋이 모이면 쓰러질 한 대를 버텨 내 존재를 증명한다.',
  start: ['유령 손짓', '반투명 가드'],
  keyword: {
    name: '존속', desc: '나는 여기 있다 — 위기가 깊을수록 또렷해지는 존재감', carrier: 'self', cap: 5,
    per: [{ stat: 'guard', v: 0.1 }, { stat: 'dealt', v: 0.1 }],
    rules: [{ name: '존재의 증명', when: { on: 'stackReach', id: '존속', n: 3 }, limit: { per: 'fight', n: 1 }, fx: [st('끈기', 1)] }],
  },
  passives: [
    { name: '앳된 존재감', when: { on: 'turnEnd' }, conds: [{ c: 'hp', pct: 0.5 }], fx: [stack('존속', 1)] },
  ],
  ult: [st('반격', 3), dmg(1.0, 'allEnemies'), stack('존속', 2)],
  uniques: [
    { name: '경계선상의 유령', cost: 1, type: '스킬', fx: [shield(1.6), { k: 'ifHp', pct: 0.5 }, stack('존속', 1)],
      on: ['또렷한 경계', '흐린 경계', '머무는 경계', null, '축복의 경계'],
      o4: { name: '여기 있어', fx: [shield(1.6), stack('존속', 1)] },
      b: [['짙은 윤곽', 'guard'], ['한 걸음 앞', 'draw'], ['존재감', null, [st('결의', 1)]]] },
    { name: '존재의 보호막', cost: 2, type: '스킬', fx: [shield(2.6), st('실드 유지', 1), stack('존속', 1)],
      on: ['찬란한 보호막', '얇은 보호막', '이어지는 보호막', null, '축복의 보호막'],
      o4: { name: '꺼지지 않는 막', fx: [shield(3.2), st('실드 보존', 1), stack('존속', 1)] },
      b: [['두꺼운 막', 'guard'], ['서두른 막', 'cost'], ['단단한 의지', null, [st('불굴', 1)]]] },
    { name: '서툰 챙김', cost: 1, type: '스킬', fx: [heal(1.4), st('피해 감소', 1)],
      on: ['정성 챙김', '대충 챙김', '챙겨 둔 간식', null, '축복의 챙김'],
      o4: { name: '처음 해 보는 간호', fx: [heal(1.4), st('피해 감소', 1), stack('존속', 1)] },
      b: [['따뜻한 손', 'heal'], ['허둥지둥', 'draw'], ['미안해', null, [st('피해 감소', 1)]]] },
    { name: '도깨비불', cost: 1, type: '공격', fx: [dmg(0.5, 'oneEnemy', { base: 'def' }), perStack('존속'), dmg(0.12, 'oneEnemy', { base: 'def' })],
      on: ['푸른 도깨비불', '작은 불씨', '꺼지지 않는 불', null, '축복의 불꽃'],
      o4: { name: '도깨비불 무리', fx: [dmg(0.4, 'allEnemies', { base: 'def' }), perStack('존속'), dmg(0.1, 'allEnemies', { base: 'def' })] },
      b: [['큰 불꽃', 'power'], ['흔들리는 불', 'frost'], ['불씨 나누기', 'draw']] },
  ],
};

// ═══ 셀리네 — 탱커 · 활발 · 화난 얼굴 갤러리(생성 카드형) ═══
H['셀리네'] = {
  blurb: '남의 화난 표정을 수집하는 엘튜버 유령. 적을 약 올릴 때마다 「표정」 이 모이고, 둘이 차면 「화난 얼굴」 한 장을 갤러리에서 꺼내 다시 약을 올린다.',
  start: ['하트 탄', '팬서비스 가드'],
  keyword: {
    name: '표정', desc: '약 올린 상대의 찡그린 얼굴 — 갤러리에 한 장씩 모은다', carrier: 'self', cap: 2,
    rules: [{ name: '화난 얼굴 갤러리', when: { on: 'stackReach', id: '표정', n: 2 }, fx: [spendAll('표정'), make('셀리네_t1', 1)] }],
  },
  passives: [
    { name: '약 올리기', when: { on: 'debuff' }, limit: { per: 'turn', n: 2 }, fx: [stack('표정', 1)] },
  ],
  ult: [dmg(3.5), stack('표정', 2), { k: 'ifBroken' }, shield(5.0)],
  uniques: [
    { name: '온 마음을 다해', cost: 2, type: '공격', fx: [dmg(1.0, 'oneEnemy', { base: 'def' }), st('약화', 1)],
      on: ['진심 어린 도발', '가벼운 도발', '준비한 멘트', null, '축복의 도발'],
      o4: { name: '구독 좋아요 알림', fx: [dmg(1.0, 'oneEnemy', { base: 'def' }), st('약화', 1), st('반격', 2)] },
      b: [['진한 하트', 'power'], ['약 오르지?', 'frost'], ['생방송', 'draw']] },
    { name: '인증샷', cost: 2, type: '스킬', fx: [shield(2.2), st('약화', 1, 'allEnemies')],
      on: ['단체 인증샷', '셀카 한 장', '저장한 사진', null, '축복의 사진'],
      o4: { name: '라이브 방송', fx: [shield(2.2), st('약화', 1, 'allEnemies'), stack('표정', 1)] },
      b: [['보정 필터', 'guard'], ['연사', 'cost'], ['조회수 폭발', null, [st('결의', 1)]]] },
    { name: '화난 얼굴 갤러리', cost: 2, type: '공격', fx: [dmg(0.8, 'oneEnemy', { base: 'def' }), make('셀리네_t1', 1)],
      on: ['특별 전시', '작은 액자', '소장품', null, '축복의 전시'],
      o4: { name: '갤러리 개관', fx: [dmg(0.8, 'oneEnemy', { base: 'def' }), make('셀리네_t1', 2)] },
      b: [['명작 한 점', 'power'], ['관람객 반응', 'draw'], ['액자 테두리', 'guard']] },
    { name: '사근사근 반말', cost: 1, type: '스킬', fx: [st('약화', 1), st('반격', 2)],
      on: ['얄미운 반말', '살짝 반말', '아껴 둔 반말', null, '축복의 반말'],
      o4: { name: '반말 폭격', fx: [st('약화', 1, 'allEnemies'), st('반격', 3)] },
      b: [['콕 찌르는 말', 'frost'], ['재빠른 말', 'cost'], ['윙크', null, [shield(0.6)]]] },
  ],
  tokens: [
    { id: '셀리네_t1', name: '화난 얼굴', hero: '셀리네', token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [st('약화', 1), shield(0.8)] },
  ],
};

// ═══ 스피키_메이드 — 딜러 · 활발 · 고장 난 청소기(더미 조작형) ═══
H['스피키_메이드'] = {
  blurb: '메이드 크레페의 일을 통째로 뒤집어쓴 꼬마 유령. 카드를 낼 때마다 고장 난 청소기가 버린 더미를 한 장씩 빨아들이고, 「잡동사니」 셋이 차면 이상한 우연으로 일이 술술 풀린다.',
  start: ['먼지털이 휘두르기', '호박 쟁반 방패', '먼지털이 대회전'],
  keyword: {
    name: '잡동사니', desc: '청소기 먼지통에 쌓인 것들 — 셋이면 뜻밖의 행운', carrier: 'self', cap: 3,
    rules: [{ name: '이상한 우연', when: { on: 'stackReach', id: '잡동사니', n: 3 }, fx: [spendAll('잡동사니'), ap(1), draw(1)] }],
  },
  passives: [
    { name: '고장 난 청소기', when: { on: 'play' }, limit: { per: 'turn', n: 2 }, fx: [{ k: 'exileFrom', from: 'discard', n: 1 }, stack('잡동사니', 1)] },
  ],
  ult: [hits(0.7, 6, 'randomEnemy'), { k: 'tough', v: 1, target: 'allEnemies' }, stack('잡동사니', 2)],
  uniques: [
    { name: '깨끗하면 할 일이 없어!', cost: 2, type: '공격', fx: [dmg(1.3, 'allEnemies'), stack('잡동사니', 1)],
      on: ['대청소', '빗자루질', '미뤄 둔 청소', null, '축복의 걸레'],
      o4: { name: '구석구석 청소', fx: [dmg(1.3, 'allEnemies'), { k: 'exileFrom', from: 'discard', n: 1 }, stack('잡동사니', 2)] },
      b: [['힘찬 먼지털이', 'power'], ['먼지 구름', 'frost'], ['반짝반짝', 'draw']] },
    { name: '고장난 흡입구', cost: 2, type: '공격', fx: [{ k: 'exileFrom', from: 'discard', n: 2 }, { k: 'perPile', from: 'gone' }, dmg(0.4)],
      on: ['역류', '덜컹 흡입', '쌓인 먼지통', null, '축복의 흡입'],
      o4: { name: '터보 흡입', fx: [{ k: 'exileFrom', from: 'discard', n: 3 }, { k: 'perPile', from: 'gone' }, dmg(0.5)] },
      b: [['강한 모터', 'power'], ['우연의 일치', 'weakSpot'], ['빠른 비우기', 'draw']] },
    { name: '호박 장식 시즌', cost: 1, type: '스킬', fx: [shield(1.2), stack('잡동사니', 1), draw(1)],
      on: ['호박 축제', '작은 호박', '창고 속 호박', null, '축복의 호박'],
      o4: { name: '호박 랜턴 행렬', fx: [shield(1.2), stack('잡동사니', 2), draw(1)] },
      b: [['단단한 호박', 'guard'], ['빠른 장식', 'draw'], ['호박 웃음', null, [st('사기', 1)]]] },
    { name: '크레페 선배의 특훈', cost: 1, type: '스킬', fx: [draw(2), { k: 'exileFrom', from: 'discard', n: 1 }],
      on: ['지옥 특훈', '가벼운 특훈', '복습', null, '축복의 특훈'],
      o4: { name: '선배 흉내', fx: [draw(2), { k: 'exileFrom', from: 'discard', n: 1 }, st('칼날 벼리기', 1)] },
      b: [['우수 사원', 'draw'], ['재빠른 손', 'cost'], ['칭찬 도장', null, [stack('잡동사니', 1)]]] },
  ],
};

// ═══ 셰이디_역전 — 서포터 · 활발 · 착한 척은 힘들어(자원형) ═══
H['셰이디_역전'] = {
  blurb: '나약함을 인정하고 유령 늪의 파수꾼이 된 장난꾼. 파티를 지켜 줄 때마다 「참음」 이 쌓이고, 넷에서 참다 터지면 파티 전원이 덩달아 달려든다. 지키는 방식은 여전히 제멋대로.',
  start: ['파수 사슬', '늪지 등불 온기'],
  keyword: {
    name: '참음', desc: '착한 척하느라 꾹꾹 눌러 둔 짜증', carrier: 'self', cap: 4,
    rules: [{ name: '참다 터짐', when: { on: 'stackReach', id: '참음', n: 4 }, fx: [spendAll('참음'), st('협공', 1), { k: 'later', n: 1, then: [ap(2)] }] }],
  },
  passives: [
    { name: '착한 척은 힘들어', when: { on: 'play', type: '스킬' }, fx: [stack('참음', 1)] },
  ],
  ult: [hits(0.8, 2, 'allEnemies'), st('취약', 1, 'allEnemies'), st('약화', 1, 'allEnemies')],
  uniques: [
    { name: '차원의 틈', cost: 2, type: '공격', fx: [hits(0.8, 2, 'allEnemies'), st('취약', 1, 'allEnemies')],
      on: ['찢어진 차원', '작은 틈', '열어 둔 틈', null, '축복의 틈'],
      o4: { name: '늪으로 끌어들이기', fx: [hits(0.9, 2, 'allEnemies'), st('취약', 1, 'allEnemies'), stack('참음', 1)] },
      b: [['깊은 틈', 'power'], ['차원 바람', 'frost'], ['틈새 엿보기', 'draw']] },
    { name: '유령 인형 샌드백', cost: 1, type: '공격', fx: [dmg(1.0), perStack('참음'), dmg(0.25)],
      on: ['샌드백 난타', '톡 치기', '아껴 둔 분풀이', null, '축복의 인형'],
      o4: { name: '참은 만큼', fx: [dmg(1.0), perStack('참음'), dmg(0.35)] },
      b: [['힘껏 펀치', 'power'], ['인형의 비명', 'frost'], ['다시 참기', null, [stack('참음', 1)]]] },
    { name: '파수꾼의 낫질', cost: 1, type: '공격', fx: [dmg(0.9), shield(0.8)],
      on: ['늪지 낫질', '가벼운 낫질', '경계 근무', null, '축복의 낫'],
      o4: { name: '등불 아래 경계', fx: [dmg(0.9), shield(0.8), stack('참음', 1)] },
      b: [['예리한 낫', 'power'], ['든든한 등', 'guard'], ['순찰', 'draw']] },
    { name: '내 방식대로 지키겠어', cost: 2, type: '스킬', fx: [shield(2.0), st('반격', 2), stack('참음', 2)],
      on: ['고집스러운 수호', '대충 수호', '버티는 수호', null, '축복의 수호'],
      o1: { name: '고집스러운 수호', fx: [shield(2.6), st('반격', 3), stack('참음', 2)] },
      o3: { name: '버티는 수호', tags: ['보존'], fx: [shield(2.4), st('반격', 3), stack('참음', 2)] },
      o5: { name: '축복의 수호', tags: ['축복'], fx: [shield(2.4), st('반격', 3), stack('참음', 2)] },
      o4: { name: '파수꾼의 맹세', fx: [shield(2.4), st('반격', 2), st('협공', 1)] },
      b: [['두꺼운 등불', 'guard'], ['서두른 수호', 'cost'], ['투덜투덜', null, [stack('참음', 1)]]] },
  ],
};

// ═══ 시온더다크불릿 — 딜러 · 우울 · 좌표 잡기(예고형) ═══
H['시온더다크불릿'] = {
  blurb: '자칭 검은 마탄의 사수, 실상은 편의점 야간 알바. 스킬로 적에게 「좌표」 를 찍어 두면 다음 턴 손에 「진혼의 마탄」 이 장전되고, 좌표 찍힌 적은 아군 공격이 약점으로 박힌다.',
  start: ['저격', '그림자 속으로', '관통 저격'],
  keyword: { name: '좌표', desc: '검은 마탄의 사수가 잡아 둔 과녁 — 찍힌 적은 급소가 드러난다', carrier: 'enemy', hunt: true, cap: 2, weakens: true },
  passives: [
    { name: '좌표 잡기', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [stack('좌표', 1), { k: 'later', n: 1, then: [make('시온더다크불릿_t1', 1)] }] },
  ],
  ult: [dmg(1.2, 'allEnemies'), make('시온더다크불릿_t1', 2)],
  uniques: [
    { name: '마.탄.의.사.수', cost: 2, type: '공격', fx: [hits(0.75, 3), { k: 'ifFoe', id: 'broken' }, st('사기', 1)],
      on: ['검은 마탄 세례', '마탄 두 발', '장전해 둔 마탄', null, '축복의 마탄'],
      o4: { name: '어둠의 연사', fx: [hits(0.75, 4), { k: 'ifFoe', id: 'broken' }, st('사기', 1)] },
      b: [['칠흑의 탄두', 'power'], ['급소 조준', 'weakSpot'], ['재장전', 'draw']] },
    { name: '디 엑시트로', cost: 2, type: '공격', fx: [dmg(1.4, 'allEnemies'), stack('좌표', 1)],
      on: ['종말의 서곡', '짧은 서곡', '봉인된 주문', null, '축복의 주문'],
      o4: { name: '어둠의 결계', fx: [dmg(1.4, 'allEnemies'), stack('좌표', 1), st('취약', 1, 'allEnemies')] },
      b: [['마력 폭주', 'power'], ['흩날리는 그림자', 'frost'], ['멋진 포즈', 'draw']] },
    { name: '좌표 잡기', cost: 1, type: '스킬', fx: [stack('좌표', 1), st('취약', 1), draw(1)],
      on: ['정밀 좌표', '대충 좌표', '기억한 좌표', null, '축복의 좌표'],
      o4: { name: '이중 좌표', fx: [stack('좌표', 2), st('취약', 1), draw(1)] },
      b: [['정확한 눈', 'frost'], ['빠른 계산', 'cost'], ['어둠의 속삭임', 'draw']] },
    { name: '진혼의 탄환', cost: 1, type: '공격', tags: ['약점 공격'], fx: [dmg(1.3), { k: 'ifKill' }, make('시온더다크불릿_t1', 1)],
      on: ['진혼의 일격', '가벼운 탄환', '품은 탄환', null, '축복의 탄환'],
      o4: { name: '관통 진혼탄', tags: ['약점 공격', '분쇄'], fx: [dmg(1.5), { k: 'ifKill' }, make('시온더다크불릿_t1', 1)] },
      b: [['은빛 탄두', 'power'], ['급소', 'weakSpot'], ['탄피 줍기', 'draw']] },
  ],
  tokens: [
    { id: '시온더다크불릿_t1', name: '진혼의 마탄', hero: '시온더다크불릿', token: true, cost: 0, type: '공격', tags: ['증발', '약점 공격'], fx: [dmg(1.5)] },
  ],
};

// ═══ 림 — 딜러 · 우울 · 썰렁 개그(대가형) ═══
H['림'] = {
  blurb: '입만 열면 아재개그로 분위기를 얼리는 질서의 2인자. 개그를 치면 적이 얼어붙어 굼떠지지만 파티도 김이 빠진다. 공격과 스킬을 같은 장수 낸 턴엔 저울이 맞아 낫 자국이 깊어진다.',
  start: ['낫 베기', '망령 장막', '대낫 휘두르기'],
  keyword: { name: '낫 자국', desc: '균형을 지키는 낫이 남긴 흔적 — 깊을수록 잘 벌어진다', carrier: 'enemy', cap: 5, per: [{ stat: 'taken', v: 0.06 }] },
  passives: [
    { name: '저울이 맞으면', when: { on: 'turnEnd' }, conds: [{ c: 'balanced' }], fx: [dmg(0.6, 'allEnemies'), stack('낫 자국', 1, 'allEnemies')] },
  ],
  ult: [hits(1.3, 2, 'allEnemies'), heal(3.6), stack('낫 자국', 3, 'allEnemies')],
  uniques: [
    { name: '스크래치 사이드', cost: 2, type: '공격', fx: [dmg(1.2, 'allEnemies'), stack('낫 자국', 1, 'allEnemies')],
      on: ['깊은 할퀴기', '얕은 할퀴기', '겨눠 둔 낫', null, '축복의 낫'],
      o4: { name: '쌍낫 할퀴기', fx: [hits(0.75, 2, 'allEnemies'), stack('낫 자국', 1, 'allEnemies')] },
      b: [['예리한 날', 'power'], ['차가운 바람', 'frost'], ['빠른 회수', 'draw']] },
    { name: '날아가는 기역', cost: 1, type: '공격', fx: [dmg(1.1), stack('낫 자국', 1)],
      on: ['ㄱ자 회전', '작은 기역', '접어 둔 기역', null, '축복의 기역'],
      o4: { name: '돌아오는 기역', tags: ['회수'], fx: [dmg(1.25), stack('낫 자국', 1)] },
      b: [['날 선 획', 'power'], ['꺾인 궤적', 'weakSpot'], ['다음 획', 'draw']] },
    { name: '썰렁 개그', cost: 1, type: '스킬', choices: ['개그', '참기'],
      fx: [{ k: 'ifChoice', n: 1 }, st('둔화', 1, 'allEnemies'), { k: 'dealtMod', v: -0.2, target: 'self' }, { k: 'ifChoice', n: 2 }, shield(1.6)],
      on: [],
      o1: { name: '회심의 개그', fx: [{ k: 'ifChoice', n: 1 }, st('둔화', 1, 'allEnemies'), { k: 'ifChoice', n: 2 }, shield(1.8)] },
      o2: { name: '짧은 개그', cost: 0, fx: [{ k: 'ifChoice', n: 1 }, st('둔화', 1, 'allEnemies'), { k: 'ifChoice', n: 2 }, shield(1.2)] },
      o3: { name: '준비한 개그', tags: ['보존'], fx: [{ k: 'ifChoice', n: 1 }, st('둔화', 1, 'allEnemies'), { k: 'ifChoice', n: 2 }, shield(1.6)] },
      o4: { name: '아무도 안 웃음', fx: [{ k: 'ifChoice', n: 1 }, st('둔화', 2, 'allEnemies'), { k: 'ifChoice', n: 2 }, shield(1.6)] },
      o5: { name: '축복의 개그', tags: ['축복'], fx: [{ k: 'ifChoice', n: 1 }, st('둔화', 1, 'allEnemies'), stack('낫 자국', 1, 'allEnemies'), { k: 'ifChoice', n: 2 }, shield(1.6)] },
      b: [['더 썰렁하게', 'frost'], ['순발력', 'cost'], ['어색한 웃음', 'draw']] },
    { name: '균형의 저울', cost: 1, type: '강화', fx: [st('사기', 1), st('불굴', 1)],
      on: ['정확한 저울', '작은 저울', '지켜 온 저울', null, '축복의 저울'],
      o4: { name: '질서의 무게', fx: [st('사기', 1), st('불굴', 1), stack('낫 자국', 2, 'allEnemies')] },
      b: [['무거운 추', 'defUp'], ['재빠른 눈금', 'draw'], ['균형 감각', null, [st('결의', 1)]]] },
  ],
};

// ═══ 키샤 — 서포터 · 우울 · 떼창 앵콜(순서형) ═══
H['키샤'] = {
  blurb: '엘리아스에 언더돌 문화를 연 유령 아이돌. 아군이 카드를 낼 때마다 무대 「열기」 가 오르고, 넷이 차면 앵콜 — 버린 카드 한 장이 무대로 돌아오고 파티의 기세가 오른다.',
  start: ['진심 하트', '팬레터 낭독'],
  keyword: {
    name: '열기', desc: '손꼽는 팬들이 만든 무대의 열기', carrier: 'self', cap: 4,
    rules: [{ name: '떼창 앵콜', when: { on: 'stackReach', id: '열기', n: 4 }, fx: [spendAll('열기'), { k: 'pull', from: 'discard', n: 1 }, st('사기', 1)] }],
  },
  passives: [
    { name: '무대 뒤 함성', when: { on: 'play', who: 'other' }, limit: { per: 'turn', n: 2 }, fx: [stack('열기', 1)] },
  ],
  ult: [dmg(1.5, 'allEnemies'), st('약화', 1, 'allEnemies'), stack('열기', 4)],
  uniques: [
    { name: '궁극의 멜로디', cost: 1, type: '스킬', tags: ['회수'], fx: [heal(1.2), stack('열기', 1)],
      on: ['클라이맥스', '허밍', '아껴 둔 후렴', null, '축복의 멜로디'],
      o4: { name: '킹짱 멜로디', tags: ['회수'], fx: [heal(1.2), stack('열기', 2)] },
      b: [['맑은 고음', 'heal'], ['빠른 템포', 'draw'], ['팬 서비스', null, [st('결의', 1)]]] },
    { name: '체리 레드 조명봉', cost: 1, type: '스킬', fx: [st('협공', 1), stack('열기', 1)],
      on: ['조명봉 물결', '조명봉 하나', '켜 둔 조명봉', null, '축복의 조명'],
      o4: { name: '객석 전체 점등', fx: [st('협공', 1), stack('열기', 1), st('사기', 1)] },
      b: [['밝은 불빛', 'draw'], ['재빠른 흔들기', 'cost'], ['응원 구호', null, [stack('열기', 1)]]] },
    { name: '하트 파동', cost: 1, type: '공격', fx: [dmg(0.8, 'allEnemies'), st('약화', 1, 'allEnemies')],
      on: ['진심 파동', '작은 하트', '모아 둔 하트', null, '축복의 하트'],
      o4: { name: '연속 하트', fx: [hits(0.5, 2, 'allEnemies'), st('약화', 1, 'allEnemies')] },
      b: [['큰 하트', 'power'], ['하트 눈빛', 'frost'], ['앵콜 요청', null, [stack('열기', 1)]]] },
    { name: '떼창 지진', cost: 2, type: '공격', fx: [perStack('열기'), dmg(0.4, 'allEnemies'), st('약화', 1, 'allEnemies')],
      on: ['객석 대지진', '작은 떼창', '터지기 직전', null, '축복의 떼창'],
      o4: { name: '전원 기립', fx: [dmg(0.6, 'allEnemies'), perStack('열기'), dmg(0.4, 'allEnemies'), st('약화', 1, 'allEnemies')] },
      b: [['우렁찬 함성', 'power'], ['쿵쿵 박자', 'frost'], ['열기 유지', null, [stack('열기', 1)]]] },
  ],
};

// ═══ 베루 — 딜러 · 우울 · 흠 하나 물고(쌓아 터뜨리기형) ═══
H['베루'] = {
  blurb: '남의 흠만 쏙쏙 찾아 험하게 늘어지는 결점의 유령. 베루가 친 적에게는 「흠」 이 남고, 흠 붙은 적을 치는 아군 공격은 한 장마다 약점으로 꽂힌다.',
  start: ['손도끼', '숨기', '손도끼 내려찍기'],
  keyword: { name: '흠', desc: '베루가 물고 늘어진 결점 — 아군 공격이 거기로 꽂힌다', carrier: 'enemy', cap: 3, weakens: true, per: [{ stat: 'taken', v: 0.1 }] },
  passives: [
    { name: '흠 하나 물고', when: { on: 'play', type: '공격' }, fx: [stack('흠', 1)] },
  ],
  ult: [stack('흠', 3), dmg(4.0), { k: 'tough', v: 2 }],
  uniques: [
    { name: '도끼 날아가요~', cost: 2, type: '공격', fx: [dmg(0.9, 'allEnemies'), stack('흠', 1, 'allEnemies')],
      on: ['도끼 회오리', '도끼 하나', '숨겨 둔 도끼', null, '축복의 도끼'],
      o4: { name: '도끼 두 자루', fx: [hits(0.55, 2, 'allEnemies'), stack('흠', 1, 'allEnemies')] },
      b: [['무거운 도끼', 'power'], ['흠 찾기', 'weakSpot'], ['도끼 줍기', 'draw']] },
    { name: '흠 하나 물고', cost: 2, type: '공격', fx: [dmg(2.6), stack('흠', 2)],
      on: ['물고 늘어지기', '살짝 물기', '벼르던 흠', null, '축복의 흠'],
      o4: { name: '끝까지 물기', fx: [dmg(2.9), stack('흠', 2), st('취약', 1)] },
      b: [['험한 말', 'power'], ['트집', 'frost'], ['다음 흠', 'draw']] },
    { name: '친구 계약서', cost: 2, type: '스킬', fx: [stack('흠', 2), st('취약', 1), st('잔광', 1)],
      on: ['3년 계약', '하루 계약', '품에 든 계약서', null, '축복의 계약'],
      o4: { name: '갱신 조항', fx: [stack('흠', 2), st('취약', 2), st('잔광', 1)] },
      b: [['빼곡한 약관', 'frost'], ['빠른 도장', 'cost'], ['친구 하자', null, [st('사기', 1)]]] },
    { name: '메죵 등 뒤', cost: 1, type: '스킬', fx: [stack('흠', 1), st('피해 감소', 1), draw(1)],
      on: ['든든한 등', '살짝 숨기', '늘 숨는 자리', null, '축복의 등'],
      o4: { name: '등 뒤에서 째려보기', fx: [stack('흠', 1), st('피해 감소', 1), draw(2)] },
      b: [['넓은 등', 'guard'], ['재빠른 숨기', 'draw'], ['울먹', null, [st('피해 감소', 1)]]] },
  ],
};

fs.mkdirSync(OUT, { recursive: true });
for (const [key, spec] of Object.entries(H)) {
  if (!design[key] || design[key].race !== '유령') throw new Error('not ghost ' + key);
  fs.writeFileSync(path.join(OUT, key + '.json'), JSON.stringify(hero(key, spec), null, 2) + '\n');
}
console.log('wrote', Object.keys(H).length);
