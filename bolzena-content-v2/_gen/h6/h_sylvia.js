const L = require('./lib');
const { d, dAll, dDef, sh, st, stk, ifS, draw, K, O, B } = L;
const KW = '티파티 초대장';
const INV = (v = 1) => stk(KW, v, 'oneEnemy');

L.hero({
  id: '실비아', name: '실비아', nature: '광기', row: 'front', role: '탱커', star: 3, hp: 1040, atk: 84, def: 79, crit: 5,
  blurb: '은의 용족 공녀. 무례한 손님에게 티파티 초대장을 건네고, 그 손님이 손을 올리기 직전 부채로 손등을 탁 친다.',
  keyword: {
    name: KW, desc: '실비아의 티파티 초대장. 한 번에 한 손님만 — 초대받은 적이 공격하려 하면 실비아가 먼저 꾸짖는다', carrier: 'enemy', cap: 1, hunt: true,
  },
  passives: [
    { name: '숙녀의 접대', when: { on: 'foeActBefore', type: '공격' }, conds: [{ c: 'stack', id: KW, n: 1 }], limit: { per: 'turn', n: 2 },
      fx: [st('약화', 1), dDef(0.5)] },
    { name: '다음 손님', when: { on: 'huntDown' }, fx: [K('nextCheaper', { v: 1 }), stk(KW, 1, 'topEnemy')] },
  ],
  ult: { name: '소녀에게 오시려고요?', cost: 300, fx: [dAll(0.6, { base: 'def' }), sh(4.0), st('기절', 1)] },
  starter: [['은빛 손짓', [d(1.0)]], ['숙녀의 자세', [sh(1.5)]]],
  uniques: [
    { name: '초대장 건네기', cost: 1, type: '스킬', fx: [INV(), sh(1.5)],
      oracles: [
        O('은빛 티파티', [INV(), sh(1.4)], { cost: 0 }),
        O('진은의 대공', [INV(), sh(2.0)]),
        O('손님은 보호받고', [INV(), sh(1.5), st('피해 감소', 1)]),
        O('무례한 자들은', [INV(), sh(1.5), st('취약', 1)]),
        O('차 한 잔', [INV(), sh(1.6)], { tags: ['개전'] }),
      ],
      blesses: [B('파스텔 보닛', { kind: 'guard' }), B('찻잔', { fx: [st('약화', 1)] }), B('은방울', { kind: 'draw' })] },
    { name: '궁극의 유희', cost: 2, type: '스킬', fx: [sh(2.5), st('반격', 2)],
      oracles: [
        O('가벼운 유희', [sh(1.2), st('반격', 2)], { cost: 1 }),
        O('은빛 무도회', [sh(3.6), st('반격', 2)]),
        O('맨 앞을 막아선다', [sh(2.5), st('반격', 3)]),
        O('숙녀의 여흥', [sh(2.8), st('반격', 2), INV()]),
        O('끝나지 않는 연회', [sh(4.0), st('반격', 4)], { cost: 3 }),
      ],
      blesses: [B('세바스티안 인형', { kind: 'guard' }), B('단아한 발걸음', { tags: ['보존'] }), B('빵빵한 볼', { fx: [st('반격', 1)] })] },
    { name: '부채로 꾸짖기', cost: 1, type: '공격', fx: [dDef(0.5), ifS(KW, 1), st('약화', 1)],
      oracles: [
        O('탁', [dDef(0.4), ifS(KW, 1), st('약화', 1)], { cost: 0 }),
        O('장갑 던지기', [dDef(0.5), INV(), st('약화', 1)]),
        O('손등을 탁', [dDef(0.65), ifS(KW, 1), st('약화', 1)]),
        O('숙녀의 꾸중', [dDef(0.5), st('약화', 1), st('취약', 1)]),
        O('부채질', [dAll(0.4, { base: 'def' }), st('약화', 1, 'allEnemies')]),
      ],
      blesses: [B('은 부채', { kind: 'power' }), B('꾸짖음', { kind: 'frost' }), B('체육관 유망주', { fx: [sh(1.0)] })] },
    { name: '티파티 준비', cost: 1, type: '강화', fx: [st('반격', 2), st('결의', 1)],
      oracles: [
        O('청소 대작전', [st('반격', 2), st('결의', 2)]),
        O('어머니와 딸', [st('반격', 3), st('결의', 1)]),
        O('그 어머니에 그 딸', [st('반격', 2), st('결의', 1), st('불굴', 1)]),
        O('초대 명단', [st('반격', 2), st('결의', 1), INV()], { tags: ['개전'] }),
        O('각설탕', [st('반격', 2), st('결의', 1)], { cost: 0 }),
      ],
      blesses: [B('티세트', { fx: [sh(1.0)] }), B('초대 손님 명부', { tags: ['개전'] }), B('은분수', { fx: [st('피해 감소', 1)] })] },
  ],
});
