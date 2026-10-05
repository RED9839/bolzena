const L = require('./lib');
const { d, dAll, sh, st, stk, ifS, draw, tough, K, O, B } = L;
const KW = '명분이 서면';
const RD = (v, t = 'oneEnemy') => K('rushDown', { v, target: t });

L.hero({
  id: '리츠', name: '리츠', nature: '광기', row: 'front', role: '딜러', star: 3, hp: 880, atk: 119, def: 40, crit: 10,
  blurb: '소심하고 공손한 은둔 고수 용족. 먼저 맞기 전까진 몸을 사리지만, 적이 손을 대는 순간 버튼이 눌려 반말로 박살을 낸다.',
  keyword: {
    name: KW, desc: '먼저 맞았으니 이제 박살 내도 될 명분. 1개당 리츠의 피해 +25%, 리츠의 턴이 끝나면 1 감소', carrier: 'self', cap: 2, endDecay: 1,
    per: [{ stat: 'dealt', v: 0.25 }],
  },
  passives: [
    { name: '먼저 손대셨죠?', when: { on: 'hurt', guarded: true }, limit: { per: 'turn', n: 1 }, fx: [stk(KW, 2)] },
    { name: '참는 자세', when: { on: 'always' }, conds: [{ c: 'stack', id: KW, not: true }],
      fx: [K('takenMod', { v: -0.2, target: 'party' }), K('dealtMod', { v: -0.3, target: 'self' })] },
  ],
  ult: { name: '벼리기', cost: 250, fx: [st('잔광', 1), dAll(0.6, { hits: 3 }), stk(KW, 2)] },
  starter: [['칼등 치기', [d(1.0)]], ['강철 자세', [sh(1.5)]]],
  uniques: [
    { name: '박살내주겠어!', cost: 1, type: '공격', fx: [d(1.3), ifS(KW, 1), tough(1)],
      oracles: [
        O('한 방 더', [d(1.7), ifS(KW, 1), tough(1)]),
        O('버튼 눌림', [d(1.3), stk(KW, 1), ifS(KW, 1), tough(1)]),
        O('정당방위', [d(1.3), ifS(KW, 1), tough(2)]),
        O('반말 모드', [d(1.1), ifS(KW, 1), tough(1)], { cost: 0 }),
        O('명분 충분', [d(2.8), ifS(KW, 1), tough(2)], { cost: 2 }),
      ],
      blesses: [B('박살', { kind: 'power' }), B('반말의 시작', { kind: 'weakSpot' }), B('스위치', { fx: [stk(KW, 1)] })] },
    { name: '담금질', cost: 1, type: '스킬', fx: [sh(1.4), st('잔광', 1)],
      oracles: [
        O('예열', [sh(1.3), st('잔광', 1)], { cost: 0 }),
        O('통곡의 벽', [sh(2.0), st('잔광', 1)]),
        O('완벽히 준비될 때까지', [sh(1.4), st('잔광', 2)]),
        O('직접 만든 갑옷', [sh(1.4), st('잔광', 1), st('불굴', 1)]),
        O('대지 분쇄', [sh(1.4), st('잔광', 1), stk(KW, 1)]),
      ],
      blesses: [B('방구석 강철', { kind: 'guard' }), B('망치질', { fx: [st('잔광', 1)] }), B('사료스탕스 명예 대원', { tags: ['보존'] })] },
    { name: '대검 밀어내기', cost: 2, type: '공격', fx: [dAll(1.1), RD(1, 'allEnemies')],
      oracles: [
        O('한 걸음 밀기', [dAll(0.75), RD(1, 'allEnemies')], { cost: 1 }),
        O('대륙의 강자', [dAll(1.45), RD(1, 'allEnemies')]),
        O('갑옷 자랑', [dAll(1.1), RD(1, 'allEnemies'), sh(1.5)]),
        O('전투광의 대검', [dAll(1.1), RD(1, 'allEnemies'), ifS(KW, 1), tough(1, 'allEnemies')]),
        O('넉백', [dAll(1.1), RD(2, 'allEnemies'), st('약화', 1, 'allEnemies')]),
      ],
      blesses: [B('손수 벼린 대검', { kind: 'power' }), B('로네의 취향', { kind: 'cost' }), B('밀려난 자리', { fx: [st('약화', 1)] })] },
    { name: '은둔 초고수', cost: 1, type: '강화', fx: [stk(KW, 2), st('불굴', 1)],
      oracles: [
        O('상상 수련', [stk(KW, 2), st('불굴', 1)], { cost: 0 }),
        O('떠돌이 무사', [stk(KW, 2), st('불굴', 1), st('잔광', 2)]),
        O('대련 상대', [stk(KW, 2), st('불굴', 2)]),
        O('소심한 각오', [stk(KW, 2), st('불굴', 1), draw(2)]),
        O('은둔의 깨달음', [stk(KW, 2), st('불굴', 1)], { tags: ['개전'], cost: 0 }),
      ],
      blesses: [B('산속 오두막', { fx: [sh(1.0)] }), B('소심한 다짐', { tags: ['개전'] }), B('명예 대원증', { fx: [st('잔광', 1)] })] },
  ],
});
