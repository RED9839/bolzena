const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, spend, ifS, per, draw, ap, K, O, B } = L;
const KW = '2인자 쟁탈전';

L.hero({
  id: '실피르', name: '실피르', nature: '우울', row: 'mid', role: '딜러', star: 2, hp: 590, atk: 115, def: 27, crit: 10,
  blurb: '루드와 티격태격하는 사파이어의 용족, 자칭 2인자. 다른 아군이 공격 카드를 낼 때마다 기록을 세어 두었다가, 제 공격으로 그 기록을 넘으면 기세가 오른다.',
  keyword: {
    name: KW, desc: '다른 아군이 세운 기록. 다른 아군이 공격 카드를 내면 +1, 둘 이상일 때 실피르가 공격 카드를 내면 모두 써서 AP +1 · 사기 1', carrier: 'self', cap: 3,
  },
  passives: [
    { name: '넘버 투', when: { on: 'play', who: 'other', type: '공격' }, limit: { per: 'turn', n: 2 }, fx: [stk(KW, 1)] },
    { name: '기록 경신', when: { on: 'play', type: '공격' }, conds: [{ c: 'stack', id: KW, n: 2 }], limit: { per: 'turn', n: 1 },
      fx: [ap(1), st('사기', 1), spend(KW, 'all')] },
  ],
  ult: { name: '킹갓zi존 실피르 어택', cost: 200, fx: [dRnd(0.4, 8), stk(KW, 2)] },
  starter: [['단검 투척', [d(1.0)]], ['비늘 가드', [sh(1.5)]]],
  uniques: [
    { name: '창공의 지배자', cost: 2, type: '공격', tags: ['신속'], fx: [dRnd(0.55, 4)],
      oracles: [
        O('날갯짓', [dRnd(0.5, 3)], { cost: 1, tags: ['신속'] }),
        O('창공 제패', [dRnd(0.55, 5)], { tags: ['신속'] }),
        O('호수 바람', [dRnd(0.55, 4), st('약화', 1, 'allEnemies')], { tags: ['신속'] }),
        O('한 끗 차 비행', [dRnd(0.55, 4), stk(KW, 1)], { tags: ['신속'] }),
        O('웨스트 블루 제독', [dRnd(0.55, 4), draw(1)], { tags: ['신속'] }),
      ],
      blesses: [B('사파이어 비늘', { kind: 'power' }), B('가벼운 날개', { kind: 'cost' }), B('호숫물', { kind: 'draw' })] },
    { name: '2인자의 자존심', cost: 1, type: '스킬', fx: [stk(KW, 2), draw(1)],
      oracles: [
        O('자칭 2인자', [stk(KW, 2), draw(1)], { cost: 0 }),
        O('진짜 2인자', [stk(KW, 3), draw(2)]),
        O('청금이라 부르지 마', [stk(KW, 2), draw(1), st('사기', 1)]),
        O('다야 님 곁으로', [stk(KW, 2), draw(1), sh(1.2)]),
        O('정정당당', [stk(KW, 2), draw(1)], { tags: ['보존'], cost: 0 }),
      ],
      blesses: [B('도전장', { fx: [stk(KW, 1)] }), B('요란한 선언', { kind: 'draw' }), B('비늘 방패', { fx: [sh(0.8)] })] },
    { name: '사파이어 단검', cost: 1, type: '공격', tags: ['약점 공격'], fx: [d(0.8), per(KW), d(0.25)],
      oracles: [
        O('단검 한 자루', [d(0.7), per(KW), d(0.2)], { cost: 0, tags: ['약점 공격'] }),
        O('엑박스칼리버', [d(1.05), per(KW), d(0.3)], { tags: ['약점 공격'] }),
        O('청금석 빛', [d(0.8), per(KW), d(0.25), st('취약', 1)], { tags: ['약점 공격'] }),
        O('영롱한 날', [d(0.8), per(KW), d(0.4)], { tags: ['약점 공격'] }),
        O('다이아몬드를 향해', [d(1.9), per(KW), d(0.5)], { cost: 2, tags: ['약점 공격'] }),
      ],
      blesses: [B('반짝이는 것', { kind: 'power' }), B('빠른 손', { kind: 'draw' }), B('빨간 건 싫어', { kind: 'weakSpot' })] },
    { name: '보석이라고 외쳐도', cost: 1, type: '강화', fx: [st('사기', 1), stk(KW, 1)],
      oracles: [
        O('다짐', [st('사기', 1), stk(KW, 1)], { cost: 0 }),
        O('보석의 자존심', [st('사기', 2), stk(KW, 2)]),
        O('나이아의 호수', [st('사기', 1), stk(KW, 1), draw(2)]),
        O('3연패 설욕', [st('사기', 1), stk(KW, 3)]),
        O('사파이어의 용족', [st('사기', 1), stk(KW, 1)], { tags: ['개전'], cost: 0 }),
      ],
      blesses: [B('교복', { fx: [sh(0.8)] }), B('제독 모자', { tags: ['개전'] }), B('보석함', { fx: [stk(KW, 1)] })] },
  ],
});
