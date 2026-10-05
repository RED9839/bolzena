// 엘프 22명 — 고유효과.md 의 엘프 줄이 설계서
const L = require('./lib');
const { D, DH, DA, DF, DFA, SH, HL, S, K, SP, IFS, NOT, PER, X, DRAW, AP, MOD } = L;
const A = 'allEnemies', R = 'randomEnemy', O = 'oneEnemy';

// 사도마다: kw(keyword) · kws(keywords) · passives · ult(fx · cost) · u(고유 카드 넷: name cost type tags fx specs) · bless(이름 여섯) · tokens
module.exports = {
  // ── 탱커 ───────────────────────────────────────────
  '이드_재활': {
    blurb: '넘어져도 다시 일어서는 이드. 맞을 때마다 쌓이는 악몽을, 스킬을 낼 때 정면으로 마주해 방패와 파동으로 바꾼다.',
    kw: { name: '악몽', desc: '파티가 다칠 때마다 쌓이는 나쁜 꿈 — 이드(재활)가 스킬을 내면 실드와 피해로 몰아낸다', carrier: 'self', cap: 5 },
    passives: [
      { name: '악몽을 마주하다', when: { on: 'hurt' }, fx: [K('악몽', 1)] },
      { name: '악몽을 마주하다', when: { on: 'play', type: '스킬' }, conds: [{ c: 'stack', id: '악몽', n: 1 }], fx: [PER('악몽'), SH(0.3), PER('악몽'), DFA(0.2), SP('악몽')] },
    ],
    ult: { cost: 250, fx: [S('피해 감소', 3), K('악몽', 4), DFA(1.2)] },
    bless: ['다시 일어서는 무릎', '짝꿍의 손', '식은땀', '꿈속의 파동', '뮤트의 응원', '넘어진 자리'],
    u: [
      { cost: 2, type: '공격', fx: [DFA(0.9), S('둔화', 1, A), K('악몽', 1)], specs: ['up', 'keep', 'kw', 'swift', 'cost+'] },
      { cost: 1, type: '공격', tags: ['약점 공격'], fx: [DFA(0.55), K('악몽', 1)], specs: ['up', 'draw', 'cheap', 'keep', 'kw'] },
      { cost: 2, type: '스킬', tags: ['종극'], fx: [SH(3.0), S('피해 감소', 2), K('악몽', 2)], specs: ['up', 'cheap', 'keep', 'up2', 'kw'] },
      { cost: 1, type: '강화', fx: [MOD('defMod', 0.1), K('악몽', 2), S('불굴', 1)], specs: ['up', 'cheap', 'draw', 'up2', 'kw'] },
    ],
  },
  '로네': {
    blurb: '거짓말을 하면 더듬는 외교관 겸 첩보원. 두 얼굴을 바꿔 쓰며 — 외교관일 땐 파티를 감싸고, 첩보원일 땐 갑옷 박치기로 적의 강인도를 부순다.',
    kw: {
      name: '첩보원 모드', desc: '두 얼굴 — 없으면 외교관(덜 맞고 덜 친다), 있으면 첩보원. 바꿀 때마다 적이 늦어진다',
      carrier: 'self', cap: 1, per: [{ stat: 'taken', v: 0.15 }],
      rules: [
        { name: '더듬는 거짓말', when: { on: 'stackReach', id: '첩보원 모드', n: 1 }, fx: [S('둔화', 1, O)] },
        { name: '더듬는 거짓말', when: { on: 'stackGone', id: '첩보원 모드' }, fx: [S('둔화', 1, O)] },
      ],
    },
    passives: [
      { name: '외교관', when: { on: 'always' }, conds: [{ c: 'stack', id: '첩보원 모드', not: true }], fx: [MOD('takenMod', -0.2, 'self', false), MOD('dealtMod', -0.2, 'self', false)] },
      { name: '첩보원', when: { on: 'play', type: '공격' }, conds: [{ c: 'stack', id: '첩보원 모드', n: 1 }], fx: [X('tough', { v: 1 })] },
    ],
    ult: { cost: 250, fx: [S('약화', 3, A), S('피해 감소', 2), SH(3.0)] },
    bless: ['외교 대사의 품위', '연습한 대사', '공기 커틀릿', '갑옷 피규어', '감봉은 안 돼요', '돈까스 공약'],
    u: [
      { cost: 2, type: '스킬', choices: ['외교관', '첩보원'], fx: [SH(3.0), X('ifChoice', { n: 1 }), SP('첩보원 모드'), X('ifChoice', { n: 2 }), K('첩보원 모드', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'], noKwBless: true },
      { cost: 1, type: '스킬', tags: ['보존'], choices: ['외교관', '첩보원'], fx: [SH(1.8), X('ifChoice', { n: 1 }), SP('첩보원 모드'), X('ifChoice', { n: 2 }), K('첩보원 모드', 1)], specs: ['up', 'cheap', 'up2', 'swift', 'keep'], noKwBless: true },
      { cost: 1, type: '스킬', fx: [SH(1.5), NOT('첩보원 모드'), S('피해 감소', 2), IFS('첩보원 모드'), DRAW(2)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'], noKwBless: true },
      { cost: 2, type: '공격', tags: ['분쇄'], fx: [DF(1.5), IFS('첩보원 모드'), X('tough', { v: 2 })], specs: ['up', 'keep', 'up2', 'swift', 'cost+'], noKwBless: true },
    ],
  },
  '알레트': {
    blurb: '칸나 반장 말만 듣는 충견형 하사. 반장님이 공격하면 그 앞을 방패로 막고, 명령을 셋 받으면 반격으로 되갚는다.',
    kw: { name: '반장님', desc: '알레트가 따르는 아군 — 반장님이 공격하면 알레트가 방패를 든다', carrier: 'hero', cap: 1 },
    kws: [{
      name: '명령 수행', desc: '반장님의 공격 하나하나가 명령 — 셋이면 반격 태세', carrier: 'self', cap: 3,
      rules: [{ name: '반장님 명령이라면', when: { on: 'stackReach', id: '명령 수행', n: 3 }, fx: [SP('명령 수행'), S('반격', 1)] }],
    }],
    passives: [
      { name: '보고드립니다!', when: { on: 'fightStart' }, fx: [K('반장님', 1, 'otherAllies')] },
      { name: '반장님 명령이라면', when: { on: 'play', type: '공격', marked: '반장님' }, limit: { per: 'turn', n: 3 }, fx: [SH(0.3), K('명령 수행', 1)] },
    ],
    ult: { cost: 250, fx: [SH(5.0), S('반격', 2), K('명령 수행', 2)] },
    bless: ['큰 방패와 삽', '정렬!', '반장님 주스', '새벽 구보', '기합 넣기', '방패 대열'],
    kwBless: K('명령 수행', 1),
    u: [
      { cost: 2, type: '공격', fx: [DF(1.4), S('둔화', 1, O), K('명령 수행', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 2, type: '공격', fx: [DFA(0.8), PER('명령 수행'), DFA(0.25), SP('명령 수행')], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '공격', fx: [DF(0.7), X('tough', { v: 1 }), K('명령 수행', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '강화', fx: [MOD('defMod', 0.1), S('반격', 2)], specs: ['up', 'cheap', 'draw', 'up2', 'kw'] },
    ],
  },
  '이드': {
    blurb: '세상 전부를 자기가 꾸는 꿈이라 여기는 영원살이. 손에 들어온 카드마다 꿈결이 번져 방패가 되고, 버려진 꿈은 다음 턴의 손패로 돌아온다.',
    kw: { name: '꿈결', desc: '이드의 카드가 뽑힐 때마다 번지는 꿈 — 모아서 한꺼번에 방패로', carrier: 'self', cap: 5 },
    passives: [
      { name: '함께 꾸는 꿈', when: { on: 'drawn' }, limit: { per: 'turn', n: 2 }, fx: [SH(0.3), K('꿈결', 1)] },
      { name: '함께 꾸는 꿈', when: { on: 'discard' }, limit: { per: 'turn', n: 1 }, fx: [S('다음 턴 드로우', 1)] },
    ],
    ult: { cost: 250, fx: [DH(0.3, 4, A), S('사기', 1), S('둔화', 2, A)] },
    bless: ['먼저 펼친 꿈', '나타의 대답', '전뇌 통신', '깊어지는 잠', '끝나지 않는 꿈', '영원살이의 품'],
    u: [
      { cost: 3, type: '스킬', fx: [SH(4.0), PER('꿈결'), SH(0.5), SP('꿈결')], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '스킬', fx: [S('반격', 2), SH(3.0), K('꿈결', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', fx: [SH(2.0), DRAW(1), X('when', { on: 'discard' }), K('꿈결', 2)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '강화', fx: [MOD('defMod', 0.2), DRAW(2), S('면역', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'kw'] },
    ],
  },
  '헤일리_멀쩡': {
    blurb: '망상에서 걸어 나와 진짜 전장을 지휘하는 헤일리. 적의 공격을 실드로 받아낼 때마다 훈장이 늘고, 훈장 하나로 다음 수를 앞당긴다.',
    kw: { name: '극복의 훈장', desc: '다 막아 낸 공격 하나하나가 훈장 — 턴 시작에 하나를 달아 첫 카드를 가볍게', carrier: 'self', cap: 3 },
    passives: [
      { name: '극복의 훈장', when: { on: 'blocked' }, fx: [K('극복의 훈장', 1)] },
      { name: '전군 전진', when: { on: 'turnStart' }, conds: [{ c: 'stack', id: '극복의 훈장', n: 1 }], fx: [SP('극복의 훈장', 1), X('nextCheaper', { v: 1 })] },
    ],
    ult: { cost: 200, fx: [{ k: 'dmg', ratio: 0.3, hits: 5, base: 'def', target: A }, S('사기', 1), K('극복의 훈장', 1)] },
    bless: ['통원 치료 일지', '정석의 자세', '달아 둔 훈장', '작전 회의', '진짜 지휘', '아침 산책'],
    u: [
      { cost: 2, type: '공격', fx: [DFA(0.8), SH(2.0), K('극복의 훈장', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '공격', fx: [DF(0.7), S('반격', 1)], specs: ['up', 'cheap', 'draw', 'up2', 'kw'] },
      { cost: 3, type: '스킬', fx: [S('사기', 1), SH(4.0), K('극복의 훈장', 2)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', fx: [K('극복의 훈장', 2), SH(1.5), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '마에스트로2호': {
    blurb: '온갖 연료로 움직이는 로봇. 파티에 실드가 쌓일 때마다 배터리가 차고, 공격 한 번에 배터리를 몽땅 쏟아붓는다.',
    kw: { name: '배터리', desc: '무엇이든 연료로 — 실드를 얻을 때마다 차고(치유로는 안 참), 카드 한 장에 모두 쏟는다', carrier: 'self', cap: 10, consumeAll: true, per: [{ stat: 'dealt', v: 0.1 }] },
    passives: [
      { name: '무엇이든 연료로', when: { on: 'guard', kind: 'shield' }, limit: { per: 'turn', n: 4 }, fx: [K('배터리', 1)] },
    ],
    ult: { cost: 250, fx: [DFA(2.0), S('약화', 2, A), S('피해 감소', 2)] },
    bless: ['배터리 A3쨩', '방화벽', '수은음료', '자가 회복 기능', '초록불 빨간불', '광석 연료'],
    u: [
      { cost: 2, type: '스킬', fx: [SH(3.5), S('피해 감소', 1), K('배터리', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', fx: [K('배터리', 2), SH(1.5), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 3, type: '공격', fx: [DFA(1.5), X('tough', { v: 1, target: A }), S('둔화', 2, A)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', tags: ['연계'], fx: [SH(1.5), K('배터리', 1)], specs: ['up', 'cheap', 'draw', 'up2', 'keep'] },
    ],
  },

  // ── 딜러 ───────────────────────────────────────────
  '헤일리': {
    blurb: '엘리아스를 동맹국으로 착각하는 PTSD 장교. 턴마다 적 하나를 외계인으로 착각하고, 그 적을 치면 뒤이어 한 발 더 갈긴다.',
    kw: { name: '외계인', desc: '헤일리가 외계인이라 믿는 적 — 헤일리가 치면 추가 공격', carrier: 'enemy', cap: 1, hunt: true },
    passives: [
      { name: '착각 순찰', when: { on: 'turnStart' }, fx: [K('외계인', 1, R)] },
      { name: '외계인 격퇴', when: { on: 'hit' }, fx: [IFS('외계인'), X('extra', { ratio: 0.5, target: O })] },
    ],
    ult: { cost: 250, fx: [DA(1.2), S('고통', 4, A), S('약화', 2, A)] },
    bless: ['경례 후 돌입', '함장의 직감', '선한 원칙주의자', '전면 돌격', '쓰라린 훈계', '작전명 선포'],
    kwBless: K('외계인', 1, O),
    u: [
      { cost: 2, type: '공격', fx: [DH(0.4, 3, A), S('고통', 2, A)], specs: ['up', 'keep', 'draw', 'swift', 'cost+'], noKwBless: true },
      { cost: 1, type: '공격', fx: [D(1.2), IFS('외계인'), D(0.6), S('약화', 1, O)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', fx: [K('외계인', 1, O), S('고통', 3, O), DRAW(1)], specs: ['up', 'cheap', 'keep', 'swift', { fx: [K('외계인', 1, O), S('고통', 4, O), DRAW(2)] }], noKwBless: true },
      { cost: 2, type: '강화', fx: [MOD('atkMod', 0.15), S('고통', 2, A), S('사기', 1)], specs: ['up', 'cheap', 'draw', 'up2', 'kw'] },
    ],
  },
  '캐시': {
    blurb: '모든 것에 「히익」 떠는 겁쟁이. 적이 움직일 때마다 놀라 전기가 차오르고, 다음 한 방에 몽땅 지져 버린다.',
    kw: {
      name: '충전', desc: '적이 움직일 때마다 놀라서 차는 전기 — 카드 한 장에 모두 쓰고, 셋이면 적을 지진다', carrier: 'self', cap: 3, consumeAll: true, per: [{ stat: 'dealt', v: 0.3 }],
      rules: [{ name: '과충전', when: { on: 'stackReach', id: '충전', n: 3 }, fx: [S('약화', 1, O)] }],
    },
    passives: [
      { name: '히익!', when: { on: 'foeAct' }, fx: [K('충전', 1)] },
    ],
    ult: { cost: 150, fx: [K('충전', 3), D(1.5), S('약화', 1, O)] },
    bless: ['히익!', '지퍼 틈으로', '가방 속 응원석', '지글지글', '집에 가고 싶어', '반사적으로 바삭'],
    u: [
      { cost: 2, type: '공격', fx: [D(1.6), DH(0.4, 2, R), K('충전', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', fx: [SH(1.5), K('충전', 1), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '공격', tags: ['약점 공격'], fx: [DH(0.3, 3, A), S('둔화', 1, A)], specs: ['up', 'keep', 'kw', 'swift', 'cost+'] },
      { cost: 2, type: '강화', fx: [MOD('atkMod', 0.15), K('충전', 2), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '엘레나': {
    blurb: '모든 발명품에 자폭 기능을 다는 괴짜 시장. 스킬을 낼 때마다 드론이 뜨고, 턴 끝마다 드론이 쏘고, 공격할 때 한 기씩 날려 버린다.',
    kw: { name: '드론', desc: '엘레나의 발명품 — 턴이 끝날 때마다 쏘고, 공격 카드를 내면 한 기가 자폭한다', carrier: 'self', cap: 3, per: [{ stat: 'dot', ratio: 0.3 }] },
    passives: [
      { name: '드론 출격', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 2 }, fx: [K('드론', 1)] },
      { name: '쓸데없는 자폭 기능', when: { on: 'play', type: '공격' }, conds: [{ c: 'stack', id: '드론', n: 1 }], limit: { per: 'turn', n: 1 }, fx: [DA(0.8), SP('드론', 1)] },
    ],
    ult: { cost: 200, fx: [DH(0.2, 6, A), K('드론', 2)] },
    bless: ['감자 삼백만 개', '자폭은 낭만이다', '편대 비행', '주머니 드론', '감전 펄스', '1인 종신 시장'],
    u: [
      { cost: 2, type: '공격', fx: [DA(1.1), S('약화', 1, A), K('드론', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', tags: ['천상'], fx: [K('드론', 1), DRAW(1)], specs: ['up', 'cheap', 'keep', 'swift', { fx: [K('드론', 2), DRAW(2)] }] },
      { cost: 3, type: '공격', fx: [DA(1.2), PER('드론'), DA(0.6), SP('드론')], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '공격', fx: [D(1.2), X('ifChain'), S('취약', 2, O)], specs: ['up', 'cheap', 'kw', 'up2', 'swift'] },
    ],
  },
  '아이시아': {
    blurb: '악덕 CEO 행세가 번번이 남 좋은 일로 끝나는 회장님. 적을 쓰러뜨리고 넘친 피해가 파티를 치유하고, 그 선행이 장부에 쌓인다.',
    kw: { name: '선행 장부', desc: '본의 아니게 한 선행 — 처치할 때마다 쌓여 「해고야!」에 실린다', carrier: 'self', cap: 5 },
    passives: [
      { name: '의도치 않은 선행', when: { on: 'kill', mine: true }, fx: [X('perEvent', { per: 20 }), HL(0.4), K('선행 장부', 1)] },
      { name: '기부천사', when: { on: 'kill', mine: true }, conds: [{ c: 'hpMin', pct: 0.95 }], fx: [SH(1.6)] },
    ],
    ult: { cost: 250, fx: [S('기절', 1, O), DA(1.6), K('선행 장부', 2)] },
    bless: ['주주 설명회', '유급 휴가', '퇴직 선물', '133차 선전포고', '해고 대신 휴가', '얼음광선 악수'],
    u: [
      { cost: 2, type: '공격', tags: ['약점 공격'], fx: [DH(0.25, 4, A), K('선행 장부', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', fx: [S('취약', 1, O), DRAW(1), K('선행 장부', 1)], specs: ['up', 'cheap', 'keep', 'swift', { fx: [S('취약', 2, O), DRAW(2), K('선행 장부', 1)] }] },
      { cost: 2, type: '공격', fx: [D(2.0), PER('선행 장부'), D(0.35), SP('선행 장부')], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '강화', fx: [MOD('atkMod', 0.1), K('선행 장부', 2), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '레이지': {
    blurb: '못 하는 게 없어 온갖 잡일에 불려 다니는 만능 해결사. 한 턴에 처음 내는 종류마다 그 종류의 작은 일을 덤으로 해치운다.',
    kw: {
      name: '출동 기록', desc: '해치운 잡일 목록 — 모아서 레이저 한 방에 몰아 쏜다', carrier: 'self', cap: 6,
      rules: [{ name: '만능 해결사', when: { on: 'play', type: '강화' }, limit: { per: 'turn', n: 1 }, fx: [S('잔광', 1), K('출동 기록', 1)] }],
    },
    passives: [
      { name: '만능 해결사', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [D(0.7, O), K('출동 기록', 1)] },
      { name: '만능 해결사', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [SH(0.7), K('출동 기록', 1)] },
    ],
    ult: { cost: 250, fx: [D(4.0), K('출동 기록', 3)] },
    bless: ['출근 도장', '안전모와 용접 고글', '팀 보이스', '하루 할당량', '승률 9할', '밤샘 몰아치기'],
    u: [
      { cost: 2, type: '공격', fx: [DA(1.0), PER('출동 기록'), DA(0.2), SP('출동 기록')], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 2, type: '공격', tags: ['연계'], fx: [D(1.6), K('출동 기록', 1)], specs: ['up', 'keep', 'draw', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', fx: [SH(1.2), DRAW(1), K('출동 기록', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '강화', fx: [MOD('atkMod', 0.12), K('출동 기록', 2), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '리뉴아': {
    blurb: '규격에 딱 맞는 1초를 사랑하는 시간 여행자. 남은 AP 를 딱 맞춰 쓸 때 카드가 한 번 더 울리고, 초침이 세 번 돌면 다음 턴이 길어진다.',
    kw: {
      name: '초침', desc: '딱 맞게 흘려보낸 1초 — 조율로 쌓고, 셋이면 다음 턴 AP', carrier: 'self', cap: 3,
      rules: [{ name: '규격에 딱 맞는 1초', when: { on: 'stackReach', id: '초침', n: 3 }, fx: [SP('초침'), X('nextAp', { v: 1 })] }],
    },
    passives: [],
    ult: { cost: 300, fx: [S('기절', 1, A), DH(0.3, 6, A), S('피해 감소', 2)] },
    bless: ['정각', '긴 1초', '째깍째깍', '멈춰버린 시간', '평행세계의 초침', '미래는 비밀'],
    u: [
      { cost: 1, type: '공격', tags: ['신속'], fx: [DH(0.25, 5), X('ifTune'), DH(0.25, 2), K('초침', 1)], specs: ['up', 'keep', 'up2', 'draw', 'cost+'] },
      { cost: 1, type: '공격', tags: ['신속'], fx: [D(1.4), X('ifTune'), D(0.7), K('초침', 1)], specs: ['up', 'keep', 'up2', 'draw', 'cost+'] },
      { cost: 1, type: '스킬', tags: ['신속'], fx: [DRAW(2), X('ifTune'), DRAW(1), K('초침', 1)], specs: ['cheap', 'keep', { fx: [DRAW(2), AP(1), X('ifTune'), K('초침', 1)] }, { fx: [DRAW(3), X('ifTune'), K('초침', 2)] }, { tags: ['신속', '보존'], fx: [DRAW(2), X('ifTune'), DRAW(2), K('초침', 1)] }] },
      { cost: 2, type: '강화', fx: [MOD('atkMod', 0.2), X('gauge', { v: 20 }), X('ifTune'), K('초침', 2)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '하이디': {
    blurb: '세상 최고의 특종은 자기 자신인 기자. 특종감으로 찍은 적은 모두의 표적이 되고, 쓰러지면 셀카 한 장이 1면 기사가 된다.',
    kw: { name: '특종감', desc: '하이디가 쫓는 특종 — 쓰러지면 「특종 기사」를 쓰고 다음 특종을 찾는다', carrier: 'enemy', cap: 1, hunt: true, per: [{ stat: 'taken', v: 0.25 }] },
    passives: [
      { name: '목표 포착!', when: { on: 'fightStart' }, fx: [K('특종감', 1, 'topEnemy')] },
      { name: '셀카 한 장', when: { on: 'huntDown', id: '특종감' }, fx: [X('make', { id: '하이디_t1', v: 1 }), K('특종감', 1, 'topEnemy')] },
    ],
    ult: { cost: 200, fx: [S('피해 감소', 3), S('표식', 2, O), DH(0.4, 5)] },
    bless: ['종군기자 시절', '골판지 두 겹', '연사 모드', '호외 발행', '오늘의 베스트 컷', '어디든 출입 가능 출입증'],
    kwBless: K('특종감', 1, O),
    tokens: [{ id: '하이디_t1', name: '특종 기사', cost: 0, type: '스킬', tags: ['소멸'], fx: [S('사기', 1), S('다음 턴 드로우', 1)] }],
    u: [
      { cost: 2, type: '스킬', fx: [S('피해 감소', 2), DRAW(1), K('특종감', 1, O)], specs: ['up', 'cheap', 'keep', 'swift', { fx: [S('피해 감소', 3), DRAW(2), K('특종감', 1, O)] }], noKwBless: true },
      { cost: 1, type: '공격', fx: [DH(0.55, 2), S('약화', 1, O)], specs: ['up', 'cheap', 'kw', 'up2', 'swift'] },
      { cost: 2, type: '공격', fx: [K('특종감', 1, O), S('표식', 1, O), D(1.9)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'], noKwBless: true },
      { cost: 1, type: '강화', fx: [MOD('critMod', 0.1), X('make', { id: '하이디_t1', v: 1 }), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '칸나': {
    blurb: '명령엔 칼 같고 휴가 신청서는 늘 반려되는 진압반장. 아군이 적을 격파할 때마다 양자폭탄 충전이 빨라지고, 쏘고 나면 반려 도장이 하나 더 쌓인다.',
    kw: { name: '반려 도장', desc: '또 반려된 휴가 신청서 — 고학년 스킬을 쓸 때마다 쌓여 자신의 피해가 오른다', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1 }] },
    passives: [
      { name: '양자폭탄 결재', when: { on: 'break' }, fx: [X('gauge', { v: 25 })] },
      { name: '휴가 신청서 반려', when: { on: 'ult' }, fx: [K('반려 도장', 1), X('nextAp', { v: 1 }), DRAW(1)] },
    ],
    ult: { cost: 250, fx: [D(4.2), X('tough', { v: 2 }), K('반려 도장', 1)] },
    bless: ['사격 준비 완료', '휴가 반려', '긴급 출동', '펀칭머신 최고점', '결재 대기', '사복 출근'],
    u: [
      { cost: 3, type: '공격', tags: ['약점 공격'], fx: [D(3.0), X('gauge', { v: 25 })], specs: ['up', 'cheap', 'keep', 'kw', 'swift'] },
      { cost: 1, type: '스킬', fx: [X('gauge', { v: 20 }), DRAW(1), SH(1.0)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '공격', fx: [S('잔광', 1), D(2.0), X('ifBreak'), X('gauge', { v: 25 })], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 2, type: '강화', fx: [MOD('atkMod', 0.15), K('반려 도장', 1), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '타이다': {
    blurb: '일은 떠넘기고 한 방 쏠 때만 진심인 경비원. 제 카드가 버려질 때마다 땡땡이를 치며 남의 카드를 가볍게 해 주고, 쌓인 땡땡이를 한 방에 쏟는다.',
    kw: { name: '땡땡이', desc: '떠넘기고 쉰 만큼 아낀 힘 — 카드 한 장에 모두 쓴다', carrier: 'self', cap: 3, consumeAll: true, per: [{ stat: 'dealt', v: 0.3 }] },
    passives: [
      { name: '일 떠넘기기', when: { on: 'discard' }, limit: { per: 'turn', n: 2 }, fx: [K('땡땡이', 1), X('nextCheaper', { v: 1 })] },
      { name: '짱박힐 시간', when: { on: 'turnStart' }, fx: [K('땡땡이', 1)] },
    ],
    ult: { cost: 250, fx: [D(5.0), K('땡땡이', 2)] },
    bless: ['근무 시간 종료 직전', '쉬면서 조준', '명당 자리', '퇴근 직전 한 발', '상관 자리 슬쩍', '진흙 위장'],
    u: [
      { cost: 2, type: '공격', fx: [S('잔불', 1, O), D(2.0)], specs: ['up', 'keep', 'kw', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', tags: ['증발'], fx: [X('discard', { v: 1 }), DRAW(2), S('공명', 1)], specs: ['cheap', 'keep', 'up2', { fx: [X('discard', { v: 1 }), DRAW(3), S('공명', 1)] }, { fx: [X('discard', { v: 1 }), DRAW(2), S('공명', 2)] }] },
      { cost: 2, type: '공격', fx: [D(2.4), X('ifKill'), DRAW(2)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', fx: [K('땡땡이', 2), SH(2.0), X('when', { on: 'discard' }), K('땡땡이', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '로네_시장': {
    blurb: '돈까스를 끝내 입에 넣지 못하는 시장. 스킬을 낼 때마다 돈까스 도시락을 돌리고, 도시락이 비워질 때마다 지지율이 올라 시청 앞 축포가 터진다.',
    kw: {
      name: '지지율', desc: '도시락 한 그릇마다 오르는 민심 — 셋이면 축포', carrier: 'self', cap: 3,
      rules: [{ name: '사랑받는 시장', when: { on: 'stackReach', id: '지지율', n: 3 }, fx: [SP('지지율'), DH(0.4, 3, A), S('사기', 1)] }],
    },
    passives: [
      { name: '돈까스 배달', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [X('make', { id: '로네_시장_t1', v: 1 })] },
    ],
    ult: { cost: 200, fx: [DH(0.4, 4, A), X('make', { id: '로네_시장_t1', v: 2 })] },
    bless: ['당선 확률 3%', '곱빼기 주문', '복지 예산 집행', '심야 식당', '지지자들의 응원', '사과는 돈까스로'],
    tokens: [{ id: '로네_시장_t1', name: '돈까스 도시락', cost: 0, type: '스킬', tags: ['소멸'], fx: [HL(0.6), K('지지율', 1)] }],
    u: [
      { cost: 1, type: '공격', fx: [DH(0.3, 4, R), K('지지율', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', fx: [X('make', { id: '로네_시장_t1', v: 1 }), DRAW(1), SH(1.0)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '강화', fx: [MOD('atkMod', 0.1), K('지지율', 2)], specs: ['up', 'cheap', 'draw', 'up2', 'swift'] },
      { cost: 2, type: '공격', fx: [S('잔불', 1, O), D(2.4), X('ifKill'), X('make', { id: '로네_시장_t1', v: 1 })], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
    ],
  },
  '리스티': {
    blurb: '곰인형 속 AI 글러브와 함께하는 해커. 공격할 때마다 콤보가 이어지며 고학년 게이지가 차고, 고학년 한 번이면 적의 다음 행동을 통째로 해킹한다.',
    kw: { name: '콤보', desc: '이어 붙인 입력 — 공격할 때마다 쌓이고, 자신의 카드를 안 낸 턴엔 끊긴다', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.08 }] },
    passives: [
      { name: '글러브 해킹', when: { on: 'play', type: '공격' }, fx: [X('gauge', { v: 8 }), K('콤보', 1)] },
      { name: '콤보 끊김', when: { on: 'turnEnd' }, conds: [{ c: 'ownNone' }], fx: [SP('콤보')] },
    ],
    ult: { cost: 150, fx: [S('기절', 1, O), K('콤보', 2), DRAW(1)] },
    bless: ['빌드 완성', '최적화 루트', '독타 페퍼', '양심 회로', '말동무 AI', '무한 콤보'],
    u: [
      { cost: 3, type: '공격', fx: [DH(0.7, 3, R), D(1.6), K('콤보', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '스킬', fx: [K('콤보', 2), DRAW(2), AP(1)], specs: ['cheap', 'keep', 'swift', { fx: [K('콤보', 3), DRAW(2), AP(1)] }, { fx: [K('콤보', 2), DRAW(3), AP(1)] }] },
      { cost: 2, type: '공격', fx: [D(2.0), X('ifChain'), D(1.0)], specs: ['up', 'keep', 'kw', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', tags: ['연계'], fx: [DRAW(1), X('gauge', { v: 20 })], specs: ['up', 'cheap', 'keep', 'kw', 'swift'] },
    ],
  },

  // ── 서포터 ─────────────────────────────────────────
  '아멜리아': {
    blurb: '시장님 일을 도맡는 자타공인 최고의 비서. 스킬을 내면 손의 다른 아군 카드 한 장을 대신 처리하고(카드는 손에 남는다), 결재가 넷 모이면 일정을 앞당긴다.',
    kw: {
      name: '결재 서류', desc: '아군이 일할 때마다 올라오는 서류 — 넷이면 한꺼번에 처리해 AP', carrier: 'self', cap: 4,
      rules: [{ name: '일괄 결재', when: { on: 'stackReach', id: '결재 서류', n: 4 }, fx: [SP('결재 서류'), AP(1)] }],
    },
    passives: [
      { name: '업무 대행', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [X('castOther')] },
      { name: '서류 접수', when: { on: 'play', who: 'other' }, limit: { per: 'turn', n: 2 }, fx: [K('결재 서류', 1)] },
    ],
    ult: { cost: 200, fx: [DH(0.2, 8, A), S('기절', 1, O), K('결재 서류', 2)] },
    bless: ['밀린 결재', '시장님 일정 관리', '휴가지에서도 일함', '좌표 확인 완료', '감시 보고서', '결재 등급: 그냥'],
    u: [
      { cost: 2, type: '공격', fx: [DA(0.6), PER('결재 서류'), DA(0.3)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 2, type: '공격', fx: [D(2.2), IFS('결재 서류', 2), S('약화', 2, O)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
      { cost: 1, type: '스킬', tags: ['연계'], fx: [DRAW(1), X('castOther')], specs: ['cheap', 'keep', 'kw', 'swift', { fx: [DRAW(2), X('castOther')] }] },
      { cost: 1, type: '공격', fx: [DA(0.8), K('결재 서류', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
    ],
  },
  '힐데': {
    blurb: '「더 센 병이 돌아야 내가 필요해진다」 는 의사. 파티가 부상일 때 처방이 두 배로 들고, 부상 속에서 낸 처방마다 기운을 북돋운다.',
    kw: { name: '처방전', desc: '부상일 때 써 둔 처방 — 모아서 큰 치유로', carrier: 'self', cap: 3 },
    passives: [
      { name: '더 센 병이 돌아야', when: { on: 'play', type: '스킬' }, conds: [{ c: 'wounded' }], limit: { per: 'fight', n: 3 }, fx: [S('사기', 1), K('처방전', 1)] },
      { name: '과잉진료 금지', when: { on: 'turnStart' }, conds: [{ c: 'wounded' }], limit: { per: 'turn', n: 1 }, fx: [K('처방전', 1)] },
    ],
    ult: { cost: 300, fx: [DA(1.8), S('약화', 2, A), S('사기', 1)] },
    bless: ['회진 시작', '왕진 가방', '경과 관찰', '숲의 파동', '예방 접종', '따끔한 소견'],
    u: [
      { cost: 2, type: '스킬', fx: [HL(3.0), X('cleanse', { v: 1 }), X('ifWounded'), HL(3.0)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '스킬', tags: ['연계'], fx: [HL(1.5), PER('처방전'), HL(1.0), SP('처방전')], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', fx: [SH(1.5), S('협공', 1)], specs: ['up', 'cheap', 'draw', 'up2', 'kw'] },
      { cost: 1, type: '공격', fx: [{ k: 'dmg', ratio: 0.25, hits: 4, base: 'def', target: R }, X('ifWounded'), K('처방전', 1)], specs: ['up', 'keep', 'up2', 'swift', 'draw'] },
    ],
  },
  '아멜리아_R41': {
    blurb: '제 손으로 만든 병기가 통제를 벗어난 「이 몸」. 스킬을 낼 때마다 시제품이 떨어지고, 시험 가동이 셋 쌓이면 완성품이 나온다 — 시제품은 가끔 오작동한다.',
    kw: { name: '시험 가동', desc: '시제품을 무사히 쏜 횟수 — 셋이면 다음부터 완성품', carrier: 'self', cap: 3 },
    passives: [
      { name: '이 몸의 시제품', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [IFS('시험 가동', 3, { not: true }), X('make', { id: '아멜리아_R41_t1', v: 1 }), IFS('시험 가동', 3), X('make', { id: '아멜리아_R41_t2', v: 1 })] },
    ],
    ult: { cost: 250, fx: [X('cleanse', { v: 2 }), DA(2.0), S('기절', 1, O)] },
    bless: ['예열 완료', '자동 증폭', '출력 회수 회로', '나의 엘레나', '흑뉴아 설계도', '이 몸의 은혜'],
    tokens: [
      { id: '아멜리아_R41_t1', name: '시제품', cost: 0, type: '공격', tags: ['소멸'], fx: [D(1.5), K('시험 가동', 1), X('ifRandom', { pct: 0.25 }), X('payHpPct', { v: 0.05 })] },
      { id: '아멜리아_R41_t2', name: '완성품', cost: 0, type: '공격', tags: ['소멸'], fx: [D(2.0)] },
    ],
    u: [
      { cost: 1, type: '스킬', fx: [HL(2.1), K('시험 가동', 1)], specs: ['up', 'cheap', 'draw', 'up2', 'keep'] },
      { cost: 1, type: '스킬', fx: [S('결정화', 1), HL(1.2)], specs: ['up', 'cheap', 'draw', 'up2', 'kw'] },
      { cost: 1, type: '스킬', fx: [MOD('atkMod', 0.15, 'oneAlly'), S('협공', 1)], specs: ['up', 'cheap', 'draw', 'keep', 'kw'] },
      { cost: 2, type: '공격', fx: [DF(1.6), S('약화', 2, O), K('시험 가동', 1)], specs: ['up', 'keep', 'up2', 'swift', 'cost+'] },
    ],
  },
  '오르': {
    blurb: '자재난 속에서도 우주선을 띄운 노력파 발명가. 어떤 카드든 소멸하면 자재로 주워 모으고, 스킬을 낼 때 자재 셋으로 발명품을 뚝딱 만든다.',
    kw: { name: '자재', desc: '사라진 카드에서 주운 부품 — 셋이면 발명품 하나', carrier: 'self', cap: 6 },
    passives: [
      { name: '필요는 발명의 어머니', when: { on: 'exhaust', who: 'any' }, fx: [K('자재', 1)] },
      { name: '발명품 조립', when: { on: 'play', type: '스킬' }, conds: [{ c: 'stack', id: '자재', n: 3 }], limit: { per: 'turn', n: 1 }, fx: [SP('자재', 3), X('make', { id: '오르_t1', v: 1 })] },
    ],
    ult: { cost: 200, fx: [{ k: 'dmg', ratio: 0.3, hits: 8, base: 'def', target: R }, SH(2.0), K('자재', 3)] },
    bless: ['알람 맞춰 두기', '발사 버튼', '기계팔 전개', '밤새 설계', '리어카 돔', '오르나르도 엘빈치'],
    tokens: [{ id: '오르_t1', name: '발명품', cost: 0, type: '스킬', tags: ['소멸'], choices: ['휴대용 돔', '2인용 로켓'], fx: [X('ifChoice', { n: 1 }), SH(1.2), X('ifChoice', { n: 2 }), D(2.0)] }],
    u: [
      { cost: 2, type: '스킬', fx: [S('약화', 2, O), S('둔화', 1, A), { k: 'dmg', ratio: 0.4, hits: 3, base: 'def', target: R }], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', tags: ['소멸'], fx: [MOD('atkMod', 0.15, 'oneAlly'), X('nextCheaper', { v: 1 })], specs: ['up', 'keep', 'kw', 'swift', { fx: [MOD('atkMod', 0.2, 'oneAlly'), X('nextCheaper', { v: 1 }), DRAW(1)] }] },
      { cost: 1, type: '스킬', fx: [SH(1.5), K('자재', 1), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '강화', fx: [MOD('defMod', 0.15), K('자재', 2), AP(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
  '페스타': {
    blurb: '규칙과 통제를 혐오하는 무정부주의 락커. 파티가 공격과 스킬을 번갈아 낼 때마다 반항심이 끓어오르고, 셋이 차면 모두가 같이 친다.',
    kw: {
      name: '반항', desc: '정해진 박자를 깨는 쾌감 — 공격과 스킬을 번갈아 내면 쌓이고, 셋이면 협공', carrier: 'self', cap: 3,
      rules: [{ name: '나락도 락이다', when: { on: 'stackReach', id: '반항', n: 3 }, fx: [SP('반항'), S('협공', 1)] }],
    },
    passives: [
      { name: '나락도 락이다', when: { on: 'play', who: 'any', seq: ['공격', '스킬'] }, fx: [K('반항', 1)] },
      { name: '나락도 락이다', when: { on: 'play', who: 'any', seq: ['스킬', '공격'] }, fx: [K('반항', 1)] },
    ],
    ult: { cost: 200, fx: [S('피해 감소', 3), MOD('atkMod', 0.3), K('반항', 3)] },
    bless: ['사운드 체크', '앙코르는 없다', '깡통 들고', '볼륨 업', '브랜디 한 잔', '체제 혐오'],
    u: [
      { cost: 3, type: '공격', tags: ['분쇄'], fx: [DFA(1.5), S('약화', 2, A), S('사기', 1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 2, type: '스킬', fx: [S('반격', 2), K('반항', 2), SH(1.5)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
      { cost: 1, type: '스킬', tags: ['연계'], fx: [MOD('atkMod', 0.15, 'oneAlly'), K('반항', 1)], specs: ['up', 'cheap', 'draw', 'keep', 'swift'] },
      { cost: 2, type: '강화', fx: [MOD('defMod', 0.15), K('반항', 3), DRAW(1)], specs: ['up', 'cheap', 'keep', 'up2', 'swift'] },
    ],
  },
};
