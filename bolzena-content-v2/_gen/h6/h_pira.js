const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, spend, ifS, per, draw, K, O, B } = L;
const KW = '도금 연금술';
const G1 = '피라_g1', G2 = '피라_g2';
const gild = (n = 1) => [K('transform', { id: G1, from: '피라_s1', n }), K('transform', { id: G2, from: '피라_s2', n })];

L.hero({
  id: '피라', name: '피라', nature: '광기', row: 'back', role: '서포터', star: 3, hp: 600, atk: 86, def: 58, crit: 5,
  blurb: '위조 금화를 끊고 진짜 화금석을 찾는 황철석의 용족 연금술사. 스킬을 쓸 때마다 손의 기본 카드를 금으로 도금한다 — 번쩍이지만 한 번 쓰면 벗겨진다.',
  keyword: {
    name: KW, desc: '도금이 벗겨지며 남은 가루 — 도금한 카드로 쌓고, 큰 한 방이 쓴다', carrier: 'self', cap: 5,
  },
  passives: [
    { name: '도금', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 2 }, fx: gild(1) },
  ],
  ult: { name: '피버☆타임이다!', cost: 200, fx: [dRnd(0.3, 9), per(KW), dRnd(0.6, 1), spend(KW, 'all')] },
  starter: [['명함 투척', [d(1.0)]], ['경품 증정', [sh(1.5)]]],
  tokens: [
    { id: G1, name: '도금 명함', cost: 1, type: '공격', tags: ['소멸'], fx: [d(1.8), stk(KW, 1)] },
    { id: G2, name: '도금 경품', cost: 1, type: '스킬', tags: ['소멸'], fx: [sh(2.7), stk(KW, 1)] },
  ],
  uniques: [
    { name: '연금술 실험', cost: 1, type: '스킬', fx: [sh(1.0), draw(1), stk(KW, 1)],
      oracles: [
        O('플라스크 하나', [sh(0.9), draw(1), stk(KW, 1)], { cost: 0 }),
        O('대실험', [sh(1.2), draw(2), stk(KW, 1)]),
        O('화금석 반응', [sh(1.0), draw(1), stk(KW, 2)]),
        O('폭발 주의', [dAll(0.6), draw(1), stk(KW, 1)]),
        O('진짜 금?', [sh(1.3), draw(1), stk(KW, 1)], { tags: ['보존'] }),
      ],
      blesses: [B('정제', { fx: [stk(KW, 1)] }), B('실험 노트', { kind: 'draw' }), B('보호 장갑', { kind: 'guard' })] },
    { name: '수금 시간이다!', cost: 2, type: '공격', fx: [d(1.0), per(KW), d(0.35), spend(KW, 'all')],
      oracles: [
        O('소액 수금', [d(0.6), per(KW), d(0.3), spend(KW, 'all')], { cost: 1 }),
        O('채권 추심', [d(1.3), per(KW), d(0.4), spend(KW, 'all')]),
        O('복리 이자', [d(1.0), per(KW), d(0.5), spend(KW, 'all')]),
        O('담보 잡기', [d(1.0), per(KW), d(0.35), st('취약', 2)]),
        O('다조!', [dAll(0.8), per(KW), dAll(0.3), spend(KW, 'all')]),
      ],
      blesses: [B('금니', { kind: 'power' }), B('장부 정리', { kind: 'weakSpot' }), B('야로!', { fx: [stk(KW, 1)] })] },
    { name: '위조 금화 뿌리기', cost: 1, type: '공격', fx: [dRnd(0.4, 3), stk(KW, 1)],
      oracles: [
        O('동전 튕기기', [dRnd(0.35, 3), stk(KW, 1)], { cost: 0 }),
        O('금화 소나기', [dRnd(0.4, 4), stk(KW, 1)]),
        O('번쩍번쩍', [dRnd(0.4, 3), stk(KW, 1), st('약화', 1, 'allEnemies')]),
        O('위폐 공장', [dRnd(0.4, 3), stk(KW, 2)]),
        O('황철석 가루', [dRnd(0.5, 3), stk(KW, 1)], { tags: ['분쇄'] }),
      ],
      blesses: [B('진짜 같은', { kind: 'power' }), B('돈 냄새', { kind: 'draw' }), B('고래!', { fx: [stk(KW, 1)] })] },
    { name: '화금석 연구', cost: 1, type: '강화', fx: [stk(KW, 2), st('사기', 1)],
      oracles: [
        O('연구 노트', [stk(KW, 2), st('사기', 1)], { cost: 0 }),
        O('골디를 넘어', [stk(KW, 3), st('사기', 1), draw(1)]),
        O('과거 청산', [stk(KW, 2), st('사기', 1), st('결의', 1)]),
        O('황금 비율', [stk(KW, 4), st('사기', 1)]),
        O('뒷골목 연금술', [stk(KW, 2), st('사기', 1)], { tags: ['개전'], cost: 0 }),
      ],
      blesses: [B('화금석', { fx: [stk(KW, 1)] }), B('연구실', { tags: ['개전'] }), B('투자 유치', { kind: 'draw' })] },
  ],
});
