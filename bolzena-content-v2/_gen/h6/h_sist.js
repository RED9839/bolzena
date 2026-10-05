const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, ifS, draw, ap, K, O, B } = L;
const KW = '밀수 장부';

L.hero({
  id: '시스트', name: '시스트', nature: '광기', row: 'mid', role: '딜러', star: 3, hp: 580, atk: 125, def: 28, crit: 10,
  blurb: '힘 대신 돈으로 가치를 증명하는 자수정의 용족 밀수꾼. 쓰러뜨리고 깨뜨린 만큼 장부에 금화가 쌓이고, 비싼 물건은 금화로만 판다.',
  keyword: {
    name: KW, desc: '장부에 적힌 금화. 적을 격파 · 처치하면 쌓이고, 1개당 시스트의 피해 +5% — 「금화로만」 파는 카드의 비용이 된다', carrier: 'self', cap: 6,
    per: [{ stat: 'dealt', v: 0.05 }],
    rules: [
      { name: '장사 수완', when: { on: 'kill' }, fx: [stk(KW, 2)] },
      { name: '다음 손님', when: { on: 'break' }, fx: [stk(KW, 2)] },
    ],
  },
  passives: [
    { name: '개업 자금', when: { on: 'fightStart' }, fx: [stk(KW, 2)] },
  ],
  ult: { name: '플렉스 건', cost: 150, fx: [K('atkMod', { v: 0.15, run: true, target: 'self' }), ap(1), stk(KW, 2)] },
  starter: [['한 발', [d(1.0)]], ['가짜 날개', [sh(1.5)]]],
  uniques: [
    { name: '총알 배송', cost: 1, type: '공격', fx: [d(1.1), stk(KW, 1)],
      oracles: [
        O('특급 배송', [d(1.45), stk(KW, 1)]),
        O('대량 주문', [d(0.6, { hits: 2 }), stk(KW, 2)]),
        O('반품 불가', [d(1.1), stk(KW, 1), K('ifKill'), stk(KW, 2)]),
        O('웃돈 얹기', [d(1.1), stk(KW, 1)], { cost: 0 }),
        O('응원 대행 서비스', [d(2.4), stk(KW, 3)], { cost: 2 }),
      ],
      blesses: [B('자수정 탄두', { kind: 'power' }), B('배송 추적', { kind: 'draw' }), B('덤 하나', { fx: [stk(KW, 1)] })] },
    { name: '떨이 판매', cost: 1, type: '스킬', fx: [draw(1), stk(KW, 2)],
      oracles: [
        O('할인 행사', [draw(1), stk(KW, 2)], { cost: 0 }),
        O('묶음 판매', [draw(2), stk(KW, 2)]),
        O('VIP 패키지', [draw(1), stk(KW, 3), sh(1.0)]),
        O('하자품', [draw(1), stk(KW, 2), st('취약', 1)]),
        O('엘리아스 통째로', [draw(1), stk(KW, 4)], { tags: ['보존'] }),
      ],
      blesses: [B('계산기', { kind: 'draw' }), B('단골 손님', { fx: [stk(KW, 1)] }), B('가짜 뿔', { tags: ['보존'] })] },
    { name: '4차원 바구니', cost: 2, type: '공격', payWith: KW, fx: [dAll(0.8, { hits: 2 }), draw(1)],
      blurb: '비용은 「밀수 장부」 로만 — 이 바구니에는 무엇이든 들어간다.',
      oracles: [
        O('골디의 저금통', [dAll(1.0, { hits: 2 }), draw(1)]),
        O('초록 괴물', [dAll(0.8, { hits: 2 }), draw(1), st('취약', 1, 'allEnemies')]),
        O('주머니 바구니', [dAll(0.6, { hits: 2 }), draw(1)], { cost: 1 }),
        O('덤 하나 더', [dAll(0.8, { hits: 2 }), draw(2)]),
        O('아기 요정까지', [dAll(0.8, { hits: 3 }), draw(1)]),
      ],
      blesses: [B('자수정 탄창', { kind: 'power' }), B('바구니 끈', { kind: 'frost' }), B('영수증', { kind: 'ap' })] },
    { name: '장사천재 시스트', cost: 1, type: '강화', fx: [K('atkMod', { v: 0.15, run: true, target: 'self' }), stk(KW, 2)],
      oracles: [
        O('개업 전단지', [K('atkMod', { v: 0.15, run: true, target: 'self' }), stk(KW, 2)], { cost: 0 }),
        O('재산 2위', [K('atkMod', { v: 0.2, run: true, target: 'self' }), stk(KW, 4)]),
        O('대리 응원', [K('atkMod', { v: 0.15, run: true, target: 'self' }), stk(KW, 2), st('사기', 1)]),
        O('위조지폐는 상종 안 해', [K('atkMod', { v: 0.15, run: true, target: 'self' }), stk(KW, 2), draw(2)]),
        O('좌판 펼치기', [K('atkMod', { v: 0.15, run: true, target: 'self' }), stk(KW, 3)], { tags: ['개전'] }),
      ],
      blesses: [B('금고', { fx: [stk(KW, 1)] }), B('장부', { tags: ['개전'] }), B('자수정 반지', { kind: 'atkUp' })] },
  ],
});
