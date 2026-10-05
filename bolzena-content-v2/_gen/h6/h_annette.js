const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, spend, ifS, draw, K, O, B } = L;
const KW = '내 챔피언';
const CH = '관중 함성';
const CHAMP = stk(KW, 1, 'oneAlly');

L.hero({
  id: '아네트', name: '아네트', nature: '광기', row: 'mid', role: '서포터', star: 3, hp: 580, atk: 75, def: 60, crit: 5,
  blurb: '결투를 붙이고 판정하는 싸움 구경꾼 석류석 용족. 아군 하나를 챔피언으로 찍어 두고, 챔피언이 칠 때마다 관중석이 들끓는다.',
  keyword: {
    name: KW, desc: '아네트가 찍은 오늘의 챔피언. 이 아군이 공격 카드를 낼 때마다 「관중 함성」 +1', carrier: 'hero', cap: 1,
  },
  keywords: [{
    name: CH, desc: '관중석의 함성. 셋이면 모두 써서 사기 1 · 협공 1', carrier: 'self', cap: 3,
    rules: [{ name: '최강자는 누구냐!', when: { on: 'stackReach', id: CH, n: 3 }, fx: [spend(CH, 'all'), st('사기', 1), st('협공', 1)] }],
  }],
  passives: [
    { name: '너는 나의 챔피언', when: { on: 'play', marked: KW, type: '공격' }, fx: [stk(CH, 1)] },
    { name: '관중석', when: { on: 'fightStart' }, fx: [stk(KW, 1, 'otherAllies')] },
  ],
  ult: { name: '편파 중계', cost: 250, fx: [dRnd(0.2, 12), st('약화', 2), stk(CH, 3)] },
  starter: [['자극적인 멘트', [d(1.0)]], ['응원 도시락', [sh(1.5)]]],
  uniques: [
    { name: '결투 성사', cost: 0, type: '스킬', fx: [CHAMP, draw(1)],
      blurb: '「너, 저 녀석이랑 붙어 봐.」',
      oracles: [
        O('지하 투기장', [CHAMP, draw(1), stk(CH, 1)]),
        O('재밌어야 한다', [CHAMP, draw(2)]),
        O('최강자는 아직', [CHAMP, draw(1), stk(CH, 2)]),
        O('한 판 더', [CHAMP, draw(1)], { tags: ['보존', '개전'] }),
        O('결투 신청서', [CHAMP, draw(1), sh(0.8)]),
      ],
      blesses: [B('판정', { kind: 'draw' }), B('결투 장갑', { fx: [stk(CH, 1)] }), B('관중 동원', { tags: ['개전'] })] },
    { name: '허접~', cost: 1, type: '스킬', fx: [st('약화', 1), st('취약', 1), stk(CH, 1)],
      oracles: [
        O('허접허접~', [st('약화', 1), st('취약', 1), stk(CH, 2)]),
        O('편파 판정', [st('약화', 2), st('취약', 2), stk(CH, 1)]),
        O('깐죽깐죽', [st('약화', 1), st('취약', 1), stk(CH, 1)], { cost: 0 }),
        O('석류 아니고 석류석', [st('약화', 1, 'allEnemies'), st('취약', 1, 'allEnemies'), stk(CH, 1)]),
        O('피식', [st('약화', 1), st('취약', 1), stk(CH, 1)], { tags: ['보존'], cost: 0 }),
      ],
      blesses: [B('깐죽', { fx: [st('약화', 1)] }), B('레드카드', { kind: 'draw' }), B('허접 소리', { fx: [stk(CH, 1)] })] },
    { name: 'MVP 예측', cost: 1, type: '스킬', fx: [sh(1.4), stk(CH, 1)],
      oracles: [
        O('오늘의 주인공', [sh(1.4), stk(CH, 1), CHAMP]),
        O('MVP 전광판', [sh(1.8), stk(CH, 1)]),
        O('반칙은 금물', [sh(1.4), stk(CH, 1), st('약화', 1)]),
        O('특제 메달', [sh(1.4), stk(CH, 2)]),
        O('예상 적중', [sh(1.3), stk(CH, 1)], { cost: 0 }),
      ],
      blesses: [B('전광판', { kind: 'guard' }), B('석류석', { fx: [stk(CH, 1)] }), B('축포', { kind: 'draw' })] },
    { name: '싸움 구경', cost: 1, type: '강화', fx: [stk(CH, 2), st('사기', 1)],
      oracles: [
        O('명당자리', [stk(CH, 2), st('사기', 1)], { cost: 0 }),
        O('지하 투기장 구상', [stk(CH, 3), st('사기', 1), st('협공', 1)]),
        O('레드카드는 무서워', [stk(CH, 2), st('사기', 1), st('약화', 2, 'allEnemies')]),
        O('최강자가 정해지지 않길', [stk(CH, 2), st('사기', 1), draw(2)]),
        O('팝콘', [stk(CH, 2), st('사기', 1)], { tags: ['개전'], cost: 0 }),
      ],
      blesses: [B('응원 봉', { fx: [sh(1.0)] }), B('관중석 맨 앞', { tags: ['개전'] }), B('함성', { fx: [stk(CH, 1)] })] },
  ],
});
