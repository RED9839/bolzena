const L = require('./lib');
const { d, dAll, dDef, sh, st, stk, ifS, draw, tough, K, O, B } = L;
const KW = '한 세트 더';

L.hero({
  id: '루드', name: '루드', nature: '활발', row: 'front', role: '탱커', star: 3, hp: 950, atk: 101, def: 67, crit: 5,
  blurb: '근육으로 다 해결하는 루비의 용족 헬창. 쉬지 않고 자기 카드만 잇달아 내면 세트가 쌓여 실드가 두꺼워진다 — 다른 사람이 끼어들면 세트가 끊긴다.',
  keyword: {
    name: KW, desc: '쉬지 않고 이어 가는 세트 — 루드의 카드를 잇달아 낼수록 실드가 붙고, 넷째에 결의', carrier: 'self', cap: 4, wipe: true, endClear: true,
    rules: [
      { name: '2세트', when: { on: 'stackReach', id: KW, n: 2 }, fx: [sh(0.3)] },
      { name: '3세트', when: { on: 'stackReach', id: KW, n: 3 }, fx: [sh(0.6)] },
      { name: '4세트', when: { on: 'stackReach', id: KW, n: 4 }, fx: [sh(0.9), st('결의', 1)] },
    ],
  },
  passives: [
    { name: '단백질 보충', when: { on: 'play' }, fx: [stk(KW, 1)] },
  ],
  ult: { name: '임팩트 프레스', cost: 300, fx: [dAll(0.3, { hits: 5, base: 'def' }), st('기절', 1), stk(KW, 3)] },
  starter: [['맨손 스쿼트 킥', [d(1.0)]], ['코어 버티기', [sh(1.5)]]],
  uniques: [
    { name: '한 세트 더!', cost: 1, type: '공격', fx: [d(0.9), sh(0.8)],
      oracles: [
        O('가벼운 세트', [d(0.8), sh(0.7)], { cost: 0 }),
        O('무한 세트', [d(1.15), sh(1.0)]),
        O('기합 소리', [d(0.9), sh(0.8), st('약화', 1)]),
        O('루드 짐 개장', [d(0.9), sh(0.8), stk(KW, 1)]),
        O('다야 님을 위하여', [d(1.0), sh(0.9)], { tags: ['보존'] }),
      ],
      blesses: [B('루비 아령', { kind: 'power' }), B('헬씨 레드', { kind: 'guard' }), B('소음 공해', { fx: [st('약화', 1)] })] },
    { name: '미숫가루 프로틴', cost: 0, type: '스킬', fx: [sh(0.7)],
      blurb: '식물에게도 프로틴을.',
      oracles: [
        O('한 스쿱', [sh(0.9)]),
        O('벌크업', [sh(0.7), st('결의', 1)]),
        O('식물에게도 프로틴', [sh(0.7), K('heal', { ratio: 0.6 })]),
        O('유산소는 적', [sh(0.7), draw(1)]),
        O('루드의 운동 교본', [sh(1.8), st('결의', 1)], { cost: 1 }),
      ],
      blesses: [B('쉐이커', { kind: 'guard' }), B('탄수화물 금지', { kind: 'draw' }), B('단백질 바', { tags: ['보존'] })] },
    { name: '다야 님을 지켜라', cost: 2, type: '스킬', fx: [sh(2.6), st('불굴', 1)],
      oracles: [
        O('한발 앞으로', [sh(1.7), st('불굴', 1)], { cost: 1 }),
        O('용족 2인자의 의무', [sh(3.4), st('불굴', 1)]),
        O('약한 용족들을 위해', [sh(2.6), st('불굴', 1), st('반격', 2)]),
        O('근육 방패', [sh(2.6), st('불굴', 2)]),
        O('근손실 방지', [sh(3.1), st('불굴', 1)], { tags: ['보존'] }),
      ],
      blesses: [B('수장 경호', { kind: 'guard' }), B('루비 이두근', { fx: [st('결의', 1)] }), B('패배는 인정', { fx: [st('피해 감소', 1)] })] },
    { name: '실피르보다 세게', cost: 1, type: '공격', fx: [dDef(0.5), ifS(KW, 2), tough(1)],
      oracles: [
        O('잽', [dDef(0.4), ifS(KW, 2), tough(1)], { cost: 0 }),
        O('재도전', [dDef(0.65), ifS(KW, 2), tough(1)]),
        O('노는 셈 치고', [dDef(0.65), ifS(KW, 1), tough(1)]),
        O('마지막 한 개', [dDef(0.5), ifS(KW, 2), tough(2)]),
        O('악우의 주먹', [dDef(1.1), ifS(KW, 2), tough(2)], { cost: 2 }),
      ],
      blesses: [B('정면 승부', { kind: 'power' }), B('하체 운동', { kind: 'frost' }), B('날개는 장식', { fx: [sh(0.6)] })] },
  ],
});
