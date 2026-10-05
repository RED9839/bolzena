const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, spend, ifS, draw, ap, when, K, O, B } = L;
const KW = '소장 가치';
const HOLD = [when('handEnd'), stk(KW, 1)];

L.hero({
  id: '제이드', name: '제이드', nature: '냉정', row: 'mid', role: '딜러', star: 3, hp: 700, atk: 125, def: 28, crit: 10,
  blurb: '절판 도서를 모으고 누구에게도 빌려주지 않는 비취의 용족 책벌레. 아끼는 책은 손에 쥔 채 넘길수록 값이 오르고, 꺼내 휘두를 때 그 값을 다 쓴다.',
  keyword: {
    name: KW, desc: '손에 쥐고 넘긴 책의 값 — 턴마다 쌓이고, 공격 카드 한 장에 모두 쓴다', carrier: 'self', cap: 3, consumeAll: true,
    per: [{ stat: 'dealt', v: 0.3 }],
  },
  passives: [
    { name: '독서광', when: { on: 'turnEnd' }, fx: [stk(KW, 1)] },
  ],
  ult: { name: '게르마늄 탐지법', cost: 150, fx: [dAll(0.3, { hits: 4 }), stk(KW, 3), ap(1)] },
  starter: [['옥구슬', [d(1.0)]], ['비늘 막기', [sh(1.5)]]],
  equips: [{ id: 'eq_jadepen', name: '제이드의 비싼 만년필', grade: '희귀', slot: '장신구', stats: { atk: 10, crit: 4 },
    effect: [{ name: '사치품의 필기감', when: { on: 'play', type: '스킬', every: 2 }, fx: [K('dealtMod', { v: 0.15, target: 'self' })] }],
    affinity: '제이드', affinityStats: { hp: 30, atk: 8 },
    affinityEffect: [{ name: '책값 메모', when: { on: 'turnEnd' }, conds: [{ c: 'held', n: 1 }], fx: [stk(KW, 1)] }],
    blurb: '책값을 쪼개 산 사치품. 손에 묵혀 둔 책이 있으면 그 값을 장부에 적어 둔다.' }],
  uniques: [
    { name: '게르마늄 옥장판', cost: 2, type: '공격', tags: ['보존'], fx: [dAll(0.9), ...HOLD],
      blurb: '절판본 사이에 끼워 둔 옥장판. 오래 데울수록 뜨겁다.',
      oracles: [
        O('휴대용 옥장판', [dAll(0.8), ...HOLD], { cost: 1, tags: ['보존'] }),
        O('옥장판 공장', [dAll(1.2), ...HOLD], { tags: ['보존'] }),
        O('원적외선', [dAll(0.9), st('약화', 1, 'allEnemies'), ...HOLD], { tags: ['보존'] }),
        O('따끈한 바닥', [dAll(0.95), draw(1), ...HOLD], { tags: ['보존'] }),
        O('광역 난방', [dAll(1.8), ...HOLD], { cost: 3, tags: ['보존'] }),
      ],
      blesses: [B('게르마늄 함량', { kind: 'power' }), B('보증서', { kind: 'draw' }), B('사은품', { fx: [stk(KW, 1)] })] },
    { name: '책에서 봤는데', cost: 1, type: '스킬', tags: ['보존'], fx: [draw(2), ...HOLD],
      oracles: [
        O('목차만 봤는데', [draw(1), ...HOLD], { cost: 0, tags: ['보존'] }),
        O('전집을 봤는데', [draw(3), ...HOLD], { tags: ['보존'] }),
        O('확신에 차서', [draw(2), stk(KW, 1), ...HOLD], { tags: ['보존'] }),
        O('잘못된 상식', [draw(2), sh(1.2), ...HOLD], { tags: ['보존'] }),
        O('밑줄 그어 둔 곳', [draw(2), st('다음 턴 드로우', 1), ...HOLD], { tags: ['보존'] }),
      ],
      blesses: [B('밑줄', { fx: [stk(KW, 1)] }), B('책갈피', { kind: 'cost' }), B('독서등', { fx: [sh(1.0)] })] },
    { name: '비취 구슬탄', cost: 1, type: '공격', tags: ['보존'], fx: [d(1.2), ifS(KW, 3), draw(1), ...HOLD],
      oracles: [
        O('잔돈', [d(1.0), ifS(KW, 3), draw(1), ...HOLD], { cost: 0, tags: ['보존'] }),
        O('통장째로', [d(1.6), ifS(KW, 3), draw(1), ...HOLD], { tags: ['보존'] }),
        O('귀한 판본', [d(1.8), st('취약', 1), ...HOLD], { tags: ['보존'] }),
        O('비취 동전', [d(1.45), ifS(KW, 2), draw(1), ...HOLD], { tags: ['보존', '약점 공격'] }),
        O('환불 불가', [d(2.8), ifS(KW, 3), draw(2), ...HOLD], { cost: 2, tags: ['보존'] }),
      ],
      blesses: [B('비취 각인', { kind: 'power' }), B('영수증', { kind: 'weakSpot' }), B('귀가 얇아서', { kind: 'draw' })] },
    { name: '초대 교주의 수양록', cost: 1, type: '강화', fx: [stk(KW, 3), draw(1)],
      oracles: [
        O('필사본', [stk(KW, 3), draw(1)], { cost: 0 }),
        O('원본 수양록', [stk(KW, 3), draw(2)]),
        O('주석 달기', [stk(KW, 3), draw(1), st('사기', 1)]),
        O('절판 도서 컬렉션', [stk(KW, 3), draw(1)], { tags: ['개전'], cost: 0 }),
        O('밤새 읽기', [stk(KW, 3), draw(3), st('사기', 1)], { cost: 2 }),
      ],
      blesses: [B('지식 컬렉션', { kind: 'draw' }), B('표지 수선', { tags: ['개전'] }), B('양장본', { fx: [sh(1.0)] })] },
  ],
});
