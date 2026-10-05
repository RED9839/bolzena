const L = require('./lib');
const { d, dAll, dRnd, sh, hl, st, stk, spend, ifS, draw, ap, K, O, B } = L;
const KW = '눈물 보석';

L.hero({
  id: '오팔', name: '오팔', nature: '순수', row: 'mid', role: '서포터', star: 3, hp: 660, atk: 75, def: 60, crit: 5,
  blurb: '금방 울음을 터뜨리는 아기 오팔 용. 파티가 다친 다음 날마다 눈물이 맺히고, 셋이 맺히면 보석이 되어 파티를 감싼다.',
  keyword: {
    name: KW, desc: '다친 다음 날 맺히는 오팔빛 눈물. 셋이 맺히면 모두 써서 AP +1 · 결정화 2', carrier: 'self', cap: 3,
    rules: [{ name: '보석이 되다', when: { on: 'stackReach', id: KW, n: 3 }, fx: [spend(KW, 'all'), ap(1), st('결정화', 2)] }],
  },
  passives: [
    { name: '울먹울먹', when: { on: 'turnStart' }, conds: [{ c: 'hurtLast' }], fx: [stk(KW, 1)] },
    { name: '선배님~!', when: { on: 'lowHp', pct: 0.5 }, fx: [stk(KW, 2), hl(2.0)] },
  ],
  ult: { name: '오팔 파우더', cost: 200, fx: [sh(3.0), st('약화', 2, 'allEnemies'), stk(KW, 1)] },
  starter: [['양산 휘두르기', [d(1.0)]], ['반짝 양산', [sh(1.5)]]],
  uniques: [
    { name: '가십 드래곤', cost: 1, type: '스킬', fx: [sh(1.6), stk(KW, 1)],
      blurb: '선배들의 소문을 듣다가 괜히 서러워진다.',
      oracles: [
        O('귓속말', [sh(1.6), stk(KW, 1)], { cost: 0 }),
        O('소문이 소문을', [sh(1.7), stk(KW, 1), draw(1)]),
        O('에헤 에헤', [sh(2.0), stk(KW, 2)]),
        O('어허!', [sh(1.6), stk(KW, 1), st('약화', 1)]),
        O('반짝이는 비늘', [sh(2.2), stk(KW, 1)], { tags: ['보존'] }),
      ],
      blesses: [B('보석 반사', { kind: 'guard' }), B('선배님 이야기', { kind: 'draw' }), B('눈물 자국', { fx: [stk(KW, 1)] })] },
    { name: '오팔빛 보호막', cost: 2, type: '스킬', fx: [sh(2.6), st('결정화', 1)],
      oracles: [
        O('얇은 막', [sh(1.8), st('결정화', 1)], { cost: 1 }),
        O('두꺼운 막', [sh(3.2), st('결정화', 1)]),
        O('겹겹이', [sh(2.6), st('결정화', 2)]),
        O('보석 돔', [sh(2.6), st('결정화', 1), st('불굴', 1)]),
        O('울보의 성', [sh(4.4), st('결정화', 2), stk(KW, 1)], { cost: 3 }),
      ],
      blesses: [B('반짝 구두', { kind: 'guard' }), B('리듬 타기', { tags: ['보존'] }), B('앙코르', { fx: [stk(KW, 1)] })] },
    { name: '보석 던지기', cost: 1, type: '공격', fx: [dRnd(0.6, 2), st('약화', 1)],
      oracles: [
        O('작은 원석', [dRnd(0.5, 2), st('약화', 1)], { cost: 0 }),
        O('보석함 통째로', [dRnd(0.6, 3), st('약화', 1)]),
        O('오팔 파편', [dRnd(0.65, 2), st('약화', 1, 'allEnemies')]),
        O('던지고 또 던지고', [dRnd(0.6, 2), st('약화', 1), draw(1)]),
        O('반짝 반짝', [dRnd(0.7, 2), st('약화', 1), stk(KW, 1)]),
      ],
      blesses: [B('정조준', { kind: 'power' }), B('보석 가루', { kind: 'frost' }), B('줍기', { kind: 'draw' })] },
    { name: '뚝 그치고', cost: 1, type: '강화', fx: [st('결정화', 2), stk(KW, 1)],
      oracles: [
        O('훌쩍', [st('결정화', 2), stk(KW, 2)]),
        O('뚝!', [st('결정화', 3), stk(KW, 1)]),
        O('씩씩한 척', [st('결정화', 2), stk(KW, 1), st('불굴', 1)]),
        O('울지 않을 거야', [st('결정화', 2), stk(KW, 1)], { tags: ['개전'], cost: 0 }),
        O('보석 같은 눈물', [st('결정화', 4), stk(KW, 3)], { cost: 2 }),
      ],
      blesses: [B('손수건', { fx: [hl(1.0)] }), B('선배님 품', { tags: ['개전'] }), B('달래 주기', { fx: [stk(KW, 1)] })] },
  ],
});
