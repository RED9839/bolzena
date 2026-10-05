const L = require('./lib');
const { d, dAll, dRnd, dDef, sh, st, stk, spend, ifS, per, draw, tough, K, up, more, O, B } = L;
const KW = '수은 재채기';

L.hero({
  id: '비비', name: '비비', nature: '순수', row: 'front', role: '탱커', star: 3, hp: 990, atk: 84, def: 79, crit: 5,
  blurb: '은의 용족 행세를 하는 수은의 용족 귀족. 보호막을 든 채 재채기를 참아 두었다가, 누가 막을 깨면 그 적에게 수은을 뿜는다.',
  keyword: {
    name: KW, desc: '보호막 뒤에서 참아 둔 수은 재채기. 파티 실드가 적의 공격에 깨지면 1 써서 그 적에게 뿜는다', carrier: 'self', cap: 3,
    rules: [
      { name: '수은 재채기', when: { on: 'shieldBreak' }, conds: [{ c: 'stack', id: KW, n: 1 }], limit: { per: 'turn', n: 2 },
        fx: [K('perEvent', { per: 100 }), dDef(0.3)] },
      { name: '수은 재채기', when: { on: 'shieldBreak' }, conds: [{ c: 'stack', id: KW, n: 1 }], limit: { per: 'turn', n: 2 },
        fx: [st('고통', 3), st('손상', 1), spend(KW, 1)] },
    ],
  },
  passives: [
    { name: '수은 보호막', when: { on: 'turnEnd' }, conds: [{ c: 'guarded', kind: 'shield' }], fx: [stk(KW, 1)] },
    { name: '소녀가 보호하겠사와요', when: { on: 'lowHp', pct: 0.4 }, fx: [sh(4.0), st('피해 감소', 2)] },
  ],
  ult: { name: '퀵실버 랜스', cost: 200, fx: [dDef(1.0), tough(2), st('고통', 5)] },
  starter: [['은빛 발톱', [d(1.0)]], ['귀족의 품위', [sh(1.5)]]],
  uniques: [
    { name: '소녀에게 오시려구요?', cost: 1, type: '스킬', fx: [sh(2.0), stk(KW, 1)],
      oracles: [
        O('먼저 맞이하는 숙녀', [sh(2.2), stk(KW, 1)], { tags: ['개전'] }),
        O('잠긴 기억의 상자', [sh(3.8), stk(KW, 2), st('반격', 1)], { cost: 2 }),
        O('보호막이 터지면', [sh(2.0), stk(KW, 1), st('반격', 1)]),
        O('수은 증기', [sh(2.2), stk(KW, 2)]),
        O('실비아를 위해', [sh(2.0), stk(KW, 1), st('면역', 1)]),
      ],
      blesses: [B('오호호호', { kind: 'guard' }), B('비비아나 아르겐툼', { tags: ['보존'] }), B('은의 용족 행세', { fx: [st('손상', 1)] })] },
    { name: '독성 재채기', cost: 1, type: '공격', fx: [dRnd(0.3, 3, { base: 'def' }), st('고통', 2, 'allEnemies')],
      oracles: [
        O('참는 중이와요', [dRnd(0.3, 3, { base: 'def' }), st('고통', 2, 'allEnemies'), stk(KW, 1)]),
        O('연쇄 재채기', [dRnd(0.2, 6, { base: 'def' }), st('고통', 2, 'allEnemies')]),
        O('비말 차단', [dRnd(0.3, 3, { base: 'def' }), st('고통', 2, 'allEnemies'), st('약화', 1, 'allEnemies')]),
        O('공기 중 수은', [dAll(0.55, { base: 'def' }), st('고통', 3, 'allEnemies')]),
        O('정면 재채기', [dDef(1.0), st('고통', 4)]),
      ],
      blesses: [B('더러운 건 질색이와요', { fx: [st('약화', 1)] }), B('만성 몸살', { kind: 'power' }), B('예고 없는 재채기', { tags: ['개전'] })] },
    { name: '은빛 창 네 자루', cost: 2, type: '공격', tags: ['분쇄'], fx: [dDef(0.3, { hits: 4 }), st('손상', 2)],
      oracles: [
        O('창 두 자루만', [dDef(0.3, { hits: 3 }), st('손상', 1)], { cost: 1, tags: ['분쇄'] }),
        O('은빛 창 세례', [dDef(0.4, { hits: 4 }), st('손상', 2)], { tags: ['분쇄'] }),
        O('자매들을 위한 창', [dDef(0.3, { hits: 4 }), st('손상', 2), sh(1.5)], { tags: ['분쇄'] }),
        O('수은 묻은 창끝', [dDef(0.3, { hits: 4 }), st('손상', 2), st('고통', 4)], { tags: ['분쇄'] }),
        O('한 점에 꽂기', [dDef(0.35, { hits: 4 }), st('손상', 2)], { tags: ['분쇄', '약점 공격'] }),
      ],
      blesses: [B('품격 있는 흡혈', { fx: [K('drain', { ratio: 0.3 })] }), B('등 뒤의 꼬리 창', { kind: 'cost' }), B('삼킨 은', { kind: 'power' })] },
    { name: '세계수 뿌리에 수은을', cost: 1, type: '강화', fx: [st('결의', 2), stk(KW, 2)],
      oracles: [
        O('취미 생활', [st('결의', 2), stk(KW, 3)], { tags: ['개전'] }),
        O('우로스 강림 계획', [st('결의', 4), stk(KW, 3), st('반격', 2)], { cost: 2 }),
        O('명예로운 비비', [st('결의', 2), stk(KW, 2), st('피해 감소', 2)]),
        O('뿌리 깊이', [st('결의', 3), stk(KW, 2)]),
        O('독을 먹였더니 기운이', [st('결의', 2), stk(KW, 2), st('불굴', 1)]),
      ],
      blesses: [B('은세공 목걸이', { fx: [sh(1.0)] }), B('엄마라 부른 나무', { tags: ['개전'] }), B('자매들의 세상', { fx: [st('면역', 1)] })] },
  ],
});
