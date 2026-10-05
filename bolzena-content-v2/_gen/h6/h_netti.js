const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, spend, ifS, draw, K, O, B } = L;
const KW = '유물 발굴';
const dig = (n = 1, at = 'bottom') => K('pull', { from: 'discard', at, n });

L.hero({
  id: '네티', name: '네티', nature: '광기', row: 'front', role: '탱커', star: 3, hp: 900, atk: 75, def: 76, crit: 5,
  blurb: '유물만 보면 눈이 도는 도굴꾼 용족. 드릴로 버린 더미 밑바닥까지 파 내려가, 묻혀 있던 카드를 캐 손에 올린다.',
  keyword: {
    name: KW, desc: '파 내려간 깊이. 셋이면 버린 더미 맨 아래 카드 1장을 캐 손으로 올리고 실드를 얻는다', carrier: 'self', cap: 3,
    rules: [{ name: '유물이다용!', when: { on: 'stackReach', id: KW, n: 3 }, fx: [spend(KW, 'all'), dig(1), sh(0.8)] }],
  },
  passives: [
    { name: '드릴 굴착', when: { on: 'play' }, limit: { per: 'turn', n: 1 }, fx: [stk(KW, 1)] },
  ],
  ult: { name: '기가 드릴 차지', cost: 200, fx: [dAll(0.2, { hits: 8 }), stk(KW, 3)] },
  starter: [['곡괭이질', [d(1.0)]], ['안전모', [sh(1.5)]]],
  uniques: [
    { name: '드릴 돌진', cost: 1, type: '공격', tags: ['분쇄'], fx: [d(0.35, { hits: 3 }), stk(KW, 1)],
      oracles: [
        O('잔 드릴', [d(0.3, { hits: 3 }), stk(KW, 1)], { cost: 0, tags: ['분쇄'] }),
        O('대형 드릴', [d(0.45, { hits: 3 }), stk(KW, 1)], { tags: ['분쇄'] }),
        O('유적 탐험', [d(0.35, { hits: 3 }), stk(KW, 2)], { tags: ['분쇄'] }),
        O('광맥 따라', [d(0.35, { hits: 3 }), stk(KW, 1), sh(1.0)], { tags: ['분쇄'] }),
        O('기가 드릴', [d(0.35, { hits: 7 }), stk(KW, 2)], { cost: 2, tags: ['분쇄'] }),
      ],
      blesses: [B('다이아 드릴 날', { kind: 'power' }), B('자철석 끌', { fx: [stk(KW, 1)] }), B('발굴 지도', { kind: 'draw' })] },
    { name: '광맥 발견', cost: 1, type: '스킬', fx: [sh(1.6), stk(KW, 1)],
      oracles: [
        O('반짝이는 돌', [sh(1.5), stk(KW, 1)], { cost: 0 }),
        O('광물 산더미', [sh(2.1), stk(KW, 1)]),
        O('보모 언니', [sh(1.6), stk(KW, 1), st('결의', 1)]),
        O('광물 감별', [sh(1.6), stk(KW, 2)]),
        O('신비한 광석', [sh(1.85), stk(KW, 1)], { tags: ['보존'] }),
      ],
      blesses: [B('단단한 광석', { kind: 'guard' }), B('광부의 노래', { fx: [stk(KW, 1)] }), B('안전 제일', { fx: [st('피해 감소', 1)] })] },
    { name: '파묻힌 유물', cost: 0, type: '스킬', fx: [dig(1), sh(0.5)],
      blurb: '어제 버린 줄 알았던 그것.',
      oracles: [
        O('유물이다용!', [dig(1), sh(0.8)]),
        O('신생 용족 알', [dig(1), sh(0.5), stk(KW, 1)]),
        O('초정밀 탐사', [dig(2), sh(0.5)]),
        O('녹슨 왕관', [dig(1), sh(0.6)], { tags: ['보존'] }),
        O('보물 상자', [dig(1), sh(0.5), draw(1)]),
      ],
      blesses: [B('모래 털기', { kind: 'guard' }), B('유물 감정서', { kind: 'draw' }), B('보모의 손', { tags: ['보존'] })] },
    { name: '자성 꼬리', cost: 1, type: '강화', fx: [st('결정화', 2), stk(KW, 1)],
      oracles: [
        O('초강력 자석', [st('결정화', 3), stk(KW, 1)]),
        O('쇠붙이 수집', [st('결정화', 2), stk(KW, 2)]),
        O('광물 자력', [st('결정화', 2), stk(KW, 1), st('결의', 1)]),
        O('리츠까지 들러붙음', [st('결정화', 2), stk(KW, 1), st('반격', 1)]),
        O('찰싹', [st('결정화', 2), stk(KW, 1)], { cost: 0 }),
      ],
      blesses: [B('자철석 비늘', { fx: [sh(1.0)] }), B('나침반', { tags: ['개전'] }), B('철가루', { fx: [st('결의', 1)] })] },
  ],
});
