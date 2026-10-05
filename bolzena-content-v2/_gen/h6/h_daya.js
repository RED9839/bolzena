const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, spend, ifS, draw, tough, make, K, O, B } = L;
const KW = '감정 의뢰';
const GEM = '다야_gem', REAL = '다야_real';

L.hero({
  id: '다야', name: '다야', nature: '순수', row: 'back', role: '딜러', star: 3, hp: 600, atk: 155, def: 25, crit: 10,
  blurb: '보석 감정이 취미인 다이아몬드의 용족. 약점을 찔러 원석을 캐내고, 원석이 둘 모이면 진품 다이아로 친다 — 혼자면 그냥 유리다.',
  keyword: {
    name: KW, desc: '들어온 보석 감정 의뢰. 셋이면 「원석」 한 장을 손에 받는다', carrier: 'self', cap: 3,
    rules: [{ name: '감정 완료', when: { on: 'stackReach', id: KW, n: 3 }, fx: [spend(KW, 'all'), make(GEM, 1)] }],
  },
  passives: [
    { name: '다이아몬드 커터', when: { on: 'break', mine: true }, fx: [make(GEM, 1)] },
    { name: '완벽주의', when: { on: 'play', tag: '약점 공격' }, fx: [stk(KW, 1)] },
  ],
  ult: { name: '다이아 브레…츄!', cost: 250, fx: [dAll(2.0), tough(1, 'allEnemies'), make(GEM, 2)] },
  starter: [['보석 파편', [d(1.0)]], ['다이아 비늘', [sh(1.5)]]],
  tokens: [
    { id: GEM, name: '원석', cost: 0, type: '공격', tags: ['증발'], evolve: { n: 2, into: REAL }, fx: [d(0.6), draw(1)] },
    { id: REAL, name: '진품 다이아', cost: 0, type: '공격', tags: ['증발', '약점 공격'], fx: [d(1.8)] },
  ],
  uniques: [
    { name: '완벽한 일격', cost: 2, type: '공격', tags: ['약점 공격'], fx: [d(2.2), K('ifBreak'), draw(1)],
      oracles: [
        O('깔끔한 일격', [d(2.6), K('ifBreak'), draw(1)], { tags: ['약점 공격'] }),
        O('완벽 그 이상', [d(3.8), K('ifBreak'), draw(2)], { cost: 3, tags: ['약점 공격'] }),
        O('보석 세공', [d(2.35), K('ifBreak'), draw(1), make(GEM, 1)], { tags: ['약점 공격'] }),
        O('흠집 하나 없이', [d(2.3), st('취약', 1), K('ifBreak'), draw(1)], { tags: ['약점 공격'] }),
        O('찍은 대로', [d(2.4), K('ifBreak'), K('ap', { v: 1 })], { tags: ['약점 공격'] }),
      ],
      blesses: [B('브릴리언트 컷', { kind: 'power' }), B('광택', { kind: 'weakSpot' }), B('완벽한 자세', { tags: ['보존'] })] },
    { name: '다이아 박기', cost: 1, type: '공격', tags: ['약점 공격'], fx: [d(1.0), st('취약', 1)],
      oracles: [
        O('파편', [d(0.9), st('취약', 1)], { cost: 0, tags: ['약점 공격'] }),
        O('원석 통째로', [d(1.3), st('취약', 1)], { tags: ['약점 공격'] }),
        O('쐐기 다이아', [d(1.0), st('취약', 2)], { tags: ['약점 공격', '분쇄'] }),
        O('반짝이는 낙인', [d(1.0), st('취약', 1), stk(KW, 1)], { tags: ['약점 공격'] }),
        O('다이아 비', [dAll(0.8), st('취약', 1, 'allEnemies')], { tags: ['약점 공격'] }),
      ],
      blesses: [B('단단한 결', { kind: 'frost' }), B('반사광', { kind: 'power' }), B('보석함', { fx: [stk(KW, 1)] })] },
    { name: '단골 손님', cost: 1, type: '스킬', fx: [make(GEM, 1), stk(KW, 1)],
      blurb: '유리 구슬을 다이아라며 들고 오는 단골 — 다야는 매번 사 준다.',
      oracles: [
        O('유리를 다이아라며', [make(GEM, 1), stk(KW, 1)], { cost: 0 }),
        O('사기 단골', [make(GEM, 2), stk(KW, 1)]),
        O('감정서 한 장', [make(GEM, 1), stk(KW, 1), draw(1)]),
        O('VIP 손님', [make(GEM, 1), stk(KW, 2)], { tags: ['보존'] }),
        O('보석상 개업', [make(GEM, 3), stk(KW, 2)], { cost: 2 }),
      ],
      blesses: [B('돋보기', { kind: 'draw' }), B('보증서', { tags: ['보존'] }), B('흥정', { fx: [K('ap', { v: 1 })] })] },
    { name: '완벽해지는 소원', cost: 2, type: '강화', fx: [st('잔광', 3), stk(KW, 2)],
      oracles: [
        O('소원 빌기', [st('잔광', 3), stk(KW, 2)], { cost: 1 }),
        O('완벽한 용', [st('잔광', 4), stk(KW, 3), st('사기', 1)]),
        O('흠 없는 하루', [st('잔광', 3), stk(KW, 2), make(GEM, 1)]),
        O('거울 앞에서', [st('잔광', 5), stk(KW, 2)]),
        O('작은 소원', [st('잔광', 3), stk(KW, 2)], { tags: ['개전'], cost: 1 }),
      ],
      blesses: [B('다이아 왕관', { fx: [st('사기', 1)] }), B('별똥별', { tags: ['개전'] }), B('반짝임', { kind: 'draw' })] },
  ],
});
