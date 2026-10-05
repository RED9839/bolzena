const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, spend, ifS, per, draw, K, O, B } = L;
const KW = '모래섬 왕국';

L.hero({
  id: '아라그니아', name: '아라그니아', nature: '냉정', row: 'mid', role: '서포터', star: 3, hp: 680, atk: 75, def: 60, crit: 5,
  blurb: '산호를 씹어 모래섬 왕국을 쌓는 진주의 용족 여왕. 스킬마다 섬이 한 층 오르고, 파티가 다치면 섬이 무너지며 층마다 단단한 모래벽을 남긴다.',
  keyword: {
    name: KW, desc: '모래로 쌓은 왕국 — 층마다 덜 아프고, 맞으면 무너져 층마다 고정 실드', carrier: 'self', cap: 5,
    per: [{ stat: 'taken', v: -0.04, who: 'allies' }],
    rules: [{ name: '모래섬 붕괴', when: { on: 'hurt' }, conds: [{ c: 'stack', id: KW, n: 1 }], limit: { per: 'turn', n: 1 },
      fx: [per(KW), L.sh(0.4, { fixed: true }), spend(KW, 'all')] }],
  },
  passives: [
    { name: '산호 씹기', when: { on: 'play', type: '스킬' }, fx: [stk(KW, 1)] },
    { name: '여왕은 백성을 버리지 않느니라', when: { on: 'lowHp', pct: 0.3 }, fx: [stk(KW, 3), st('불굴', 1)] },
  ],
  ult: { name: '파도 치는 피날레', cost: 200, fx: [dRnd(1.0, 3), st('약화', 2, 'allEnemies'), stk(KW, 3)] },
  starter: [['최강 꼬리치기', [d(1.0)]], ['짐의 하사품', [sh(1.5)]]],
  uniques: [
    { name: '마음속의 펄', cost: 1, type: '스킬', fx: [sh(1.5), stk(KW, 1)],
      oracles: [
        O('작은 진주', [sh(1.4), stk(KW, 1)], { cost: 0 }),
        O('구슬 아홉', [sh(1.6), stk(KW, 2)]),
        O('산호 진주', [sh(1.9), stk(KW, 1)], { tags: ['보존'] }),
        O('특별함을 받아들일 미래', [sh(1.5), stk(KW, 1), draw(1)]),
        O('진주 왕국의 보물고', [sh(1.5), stk(KW, 1), st('결정화', 1)]),
      ],
      blesses: [B('여왕의 귀걸이', { kind: 'guard' }), B('조개 방패', { fx: [st('결정화', 1)] }), B('파도 한 줌', { kind: 'draw' })] },
    { name: '해양 간척', cost: 1, type: '스킬', tags: ['보존'], fx: [st('불굴', 1), draw(1)],
      oracles: [
        O('한 입', [st('불굴', 1), draw(1)], { cost: 0, tags: ['보존'] }),
        O('모래섬 확장', [st('불굴', 1), draw(1), stk(KW, 2)], { tags: ['보존'] }),
        O('생식 요리', [st('불굴', 1), draw(2)], { tags: ['보존'] }),
        O('방파제', [st('불굴', 1), draw(1), sh(1.2)], { tags: ['보존'] }),
        O('빙수 알바', [st('불굴', 2), draw(2), stk(KW, 1)], { cost: 2, tags: ['보존'] }),
      ],
      blesses: [B('단단한 이빨', { fx: [stk(KW, 1)] }), B('산호 가루', { kind: 'cost' }), B('모래알', { fx: [sh(1.0)] })] },
    { name: '파도 한 줄기', cost: 1, type: '공격', fx: [dAll(0.7), ifS(KW, 3), st('약화', 1, 'allEnemies')],
      oracles: [
        O('잔물결', [dAll(0.6), ifS(KW, 3), st('약화', 1, 'allEnemies')], { cost: 0 }),
        O('큰 파도', [dAll(0.95), ifS(KW, 3), st('약화', 1, 'allEnemies')]),
        O('여왕의 칙령', [dAll(0.9), st('약화', 1, 'allEnemies')]),
        O('밀물', [dAll(0.7), stk(KW, 1), ifS(KW, 3), st('약화', 1, 'allEnemies')]),
        O('해일', [dAll(1.4), st('약화', 2, 'allEnemies')], { cost: 2 }),
      ],
      blesses: [B('왕관의 빛', { kind: 'power' }), B('여왕의 은총', { kind: 'frost' }), B('진주 목걸이', { fx: [stk(KW, 1)] })] },
    { name: '진주 왕국의 보물고', cost: 2, type: '강화', fx: [stk(KW, 3), st('결정화', 2)],
      oracles: [
        O('보물고 열쇠', [stk(KW, 3), st('결정화', 2)], { cost: 1 }),
        O('엘리아스 최강 왕국', [stk(KW, 5), st('결정화', 3)]),
        O('빼앗긴 진주', [stk(KW, 3), st('결정화', 2), st('불굴', 1)]),
        O('앵무 해적단 격퇴', [stk(KW, 3), st('결정화', 2), draw(2)]),
        O('교주와 함께', [stk(KW, 3), st('결정화', 2)], { cost: 1, tags: ['개전'] }),
      ],
      blesses: [B('왕좌', { fx: [st('불굴', 1)] }), B('금고 문', { tags: ['개전'] }), B('보물 지도', { kind: 'draw' })] },
  ],
});
