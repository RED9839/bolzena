const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, ifS, draw, tough, K, O, B } = L;
const KW = '그림자 마무리';
const BRK = K('ifFoe', { id: 'broken' });
const RD = (v = 1) => K('rushDown', { v, target: 'oneEnemy' });

L.hero({
  id: '키디언', name: '키디언', nature: '우울', row: 'front', role: '딜러', star: 3, hp: 750, atk: 144, def: 35, crit: 10,
  blurb: '흑요석의 용족 닌자. 적이 격파되어 무너진 틈에 그림자처럼 파고들어 마무리하고, 쓰러진 적을 딛고 다음 칼을 바로 꺼낸다.',
  keyword: {
    name: KW, desc: '무너진 틈을 노리는 그림자. 적이 격파되면 +2 — 1개당 키디언의 피해 +25%, 키디언이 공격 카드를 내면 모두 사라진다', carrier: 'self', cap: 2, consumeAll: true,
    per: [{ stat: 'dealt', v: 0.25 }],
  },
  passives: [
    { name: '그림자 추적', when: { on: 'break' }, fx: [stk(KW, 2)] },
    { name: '흑요석 단면', when: { on: 'kill' }, conds: [{ c: 'targetBroken' }], limit: { per: 'turn', n: 1 }, fx: [K('nextCheaper', { v: 2 }), draw(1)] },
  ],
  ult: { name: '쉐도우 다이브', cost: 150, fx: [st('잔불', 2), d(2.5), stk(KW, 2)] },
  starter: [['쿠나이 투척', [d(1.0)]], ['그림자 숨기', [sh(1.5)]]],
  uniques: [
    { name: '급소 찌르기', cost: 1, type: '공격', tags: ['약점 공격'], fx: [d(0.9), BRK, d(0.5), RD()],
      oracles: [
        O('바늘 찌르기', [d(0.8), BRK, d(0.4), RD()], { cost: 0, tags: ['약점 공격'] }),
        O('흑요석 관통', [d(1.15), BRK, d(0.6), RD()], { tags: ['약점 공격'] }),
        O('이중 찌르기', [d(0.6, { hits: 2 }), BRK, d(0.5), RD()], { tags: ['약점 공격'] }),
        O('숨통', [d(0.9), BRK, d(1.0), RD()], { tags: ['약점 공격'] }),
        O('그림자 일격', [d(2.2), BRK, d(1.2), RD(2)], { cost: 2, tags: ['약점 공격'] }),
      ],
      blesses: [B('흑요석 날', { kind: 'power' }), B('독 바른 칼끝', { fx: [st('잔불', 1)] }), B('칼집', { tags: ['보존'] })] },
    { name: '급소 찾기', cost: 0, type: '스킬', fx: [tough(1), st('잔불', 1)],
      oracles: [
        O('눈 감고 듣기', [tough(1), st('잔불', 2)]),
        O('완벽한 해부', [tough(2), st('잔불', 1)]),
        O('약점 노출', [tough(1), st('잔불', 1), st('취약', 1)]),
        O('그림자 관찰', [tough(1), st('잔불', 1), draw(1)]),
        O('인내', [tough(1), st('잔불', 1)], { tags: ['보존'] }),
      ],
      blesses: [B('예리한 눈', { kind: 'draw' }), B('숨 죽이기', { kind: 'frost' }), B('잠입', { tags: ['개전'] })] },
    { name: '거대 쿠나이', cost: 2, type: '공격', fx: [d(2.0), tough(1)],
      oracles: [
        O('쿠나이 투척', [d(1.3), tough(1)], { cost: 1 }),
        O('흑요석 폭풍', [d(2.5), tough(1)]),
        O('관통 쿠나이', [d(2.1), tough(1)], { tags: ['분쇄', '약점 공격'] }),
        O('회수 쿠나이', [d(2.2), tough(1)], { tags: ['회수'] }),
        O('급소 직격', [d(2.0), tough(1), BRK, d(1.0)]),
      ],
      blesses: [B('무게 중심', { kind: 'power' }), B('그림자 분신', { kind: 'weakSpot' }), B('쿠나이 줄', { kind: 'draw' })] },
    { name: '그림자 수련', cost: 1, type: '강화', fx: [st('잔광', 2), stk(KW, 2)],
      oracles: [
        O('새벽 수련', [st('잔광', 2), stk(KW, 2)], { cost: 0 }),
        O('비전 오의', [st('잔광', 3), stk(KW, 2), st('사기', 1)]),
        O('흑요석 연마', [st('잔광', 4), stk(KW, 2)]),
        O('그림자 숨기', [st('잔광', 2), stk(KW, 2), draw(2)]),
        O('닌자의 길', [st('잔광', 2), stk(KW, 2)], { tags: ['개전'], cost: 0 }),
      ],
      blesses: [B('검은 두건', { fx: [st('잔불', 1)] }), B('수련 일지', { tags: ['개전'] }), B('표창', { kind: 'draw' })] },
  ],
});
