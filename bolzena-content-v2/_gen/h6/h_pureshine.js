const L = require('./lib');
const { d, dAll, dRnd, sh, st, stk, ifS, draw, make, K, O, B } = L;
const ID = '다야_퓨어샤인';
const KW = '얼른 끝내자';
const FIN = `${ID}_finish`;
const FIRST = [K('ifNth', { n: 1 }), stk(KW, 1)];

L.hero({
  id: ID, name: '다야(퓨어샤인)', nature: '광기', row: 'mid', role: '서포터', star: 3, hp: 600, atk: 75, def: 60, crit: 5,
  blurb: '마법 소녀로 변신한 다야. 변신이 부끄러워 누구보다 빨리 끝내고 싶다 — 파티가 한 턴에 카드를 넉 장 내면 마무리 빔이 손에 들어온다.',
  keyword: {
    name: KW, desc: '서두르는 마음 — 턴 첫 카드가 자신의 것이면 켜져 「샤이닝 피니시」를 한 장 더', carrier: 'self', cap: 1, endClear: true,
  },
  passives: [
    { name: '퓨어☆피니시', when: { on: 'play', who: 'any', nth: 4 }, limit: { per: 'turn', n: 1 },
      fx: [make(FIN, 1), ifS(KW, 1), make(FIN, 1)] },
  ],
  ult: { name: '퓨어☆샤이닝 빔', cost: 250, fx: [sh(3.0), d(0.4, { hits: 6 }), make(FIN, 1)] },
  starter: [['별빛 스틱', [d(1.0)]], ['반짝 반창고', [sh(1.5)]]],
  tokens: [{ id: FIN, name: '샤이닝 피니시', cost: 0, type: '공격', tags: ['증발'], fx: [d(0.3, { hits: 5 })] }],
  uniques: [
    { name: '매지컬☆샤인', cost: 1, type: '스킬', fx: [sh(1.5), draw(1), ...FIRST],
      oracles: [
        O('변신 완료', [sh(1.4), draw(1), ...FIRST], { cost: 0 }),
        O('풀 파워 변신', [sh(2.0), draw(1), ...FIRST]),
        O('매지컬☆포즈', [sh(1.5), draw(2), ...FIRST]),
        O('포즈 한 번 더', [sh(1.5), draw(1), stk(KW, 1)]),
        O('나의 아름다운 장미꽃에게', [sh(1.85), draw(1), ...FIRST], { tags: ['보존'] }),
      ],
      blesses: [B('변신 주문 단축', { kind: 'cost' }), B('반짝이 가루', { kind: 'guard' }), B('지상 주민의 응원', { kind: 'draw' })] },
    { name: '매지컬☆에너지', cost: 0, type: '공격', fx: [d(0.6), ...FIRST],
      oracles: [
        O('에너지 충전', [d(0.8), ...FIRST]),
        O('매지컬☆버스트', [d(0.35, { hits: 3 }), ...FIRST]),
        O('지원 사격', [d(0.6), st('약화', 1), ...FIRST]),
        O('마무리 포즈', [d(0.6), draw(1), ...FIRST]),
        O('부끄럽느니라!', [d(1.6), make(FIN, 1), ...FIRST], { cost: 1 }),
      ],
      blesses: [B('별빛 충전', { kind: 'power' }), B('반짝', { kind: 'draw' }), B('용족의 위엄', { kind: 'frost' })] },
    { name: '교주의 편지', cost: 0, type: '스킬', fx: [draw(1), ...FIRST],
      blurb: '교주에게 칭찬받고 싶어 쓰는 편지. 답장은 언제 오려나.',
      oracles: [
        O('장문의 편지', [draw(2), ...FIRST]),
        O('칭찬을 기다리겠느니라', [draw(1), sh(0.8), ...FIRST]),
        O('빨개진 얼굴', [draw(1), st('사기', 1), ...FIRST], { tags: ['소멸'] }),
        O('편지 쓰는 마법 소녀', [draw(1), ...FIRST], { tags: ['보존'] }),
        O('기다림 끝에 도착한 답장', [draw(1), make(FIN, 1)]),
      ],
      blesses: [B('우표', { kind: 'draw' }), B('편지지 향기', { fx: [sh(0.6)] }), B('답장', { tags: ['보존'] })] },
    { name: '매지컬☆샤인 영역', cost: 2, type: '강화', fx: [st('사기', 1), st('결의', 2)],
      oracles: [
        O('작은 영역', [st('사기', 1), st('결의', 2)], { cost: 1 }),
        O('반짝반짝 결계', [st('사기', 1), st('결의', 3), st('피해 감소', 2)]),
        O('일 초 만에 끝내겠느니라', [st('사기', 1), st('결의', 2), make(FIN, 2)]),
        O('스타☆샤인', [st('사기', 2), st('결의', 2)]),
        O('부끄러운 영역', [st('사기', 1), st('결의', 2)], { cost: 1, tags: ['개전'] }),
      ],
      blesses: [B('따스한 빛', { fx: [sh(1.0)] }), B('영역 확장', { tags: ['개전'] }), B('응원봉', { fx: [st('결의', 1)] })] },
  ],
});
