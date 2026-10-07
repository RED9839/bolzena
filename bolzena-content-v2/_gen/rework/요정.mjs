// 사도 리워크 2단계 — 요정 21명(에르핀 제외 — 시범에서 끝남). 2026-10-07
// 3단계(카제나 자료 보강, 2026-10-08): 신탁 얕은 갈래 2개 이하 · 재설계 1~2 · 강화화/서치 BEST · 대가 갈래 · ④ 1코 마무리 · 개전 강화 시동 ·
//   생성 카드(마요 · 슈팡 · 에슈르 · 큐이 · 파트라 새로) · 기본 카드 연료(리코타 · 샤샤 · 에슈르(마도) · 칸타 · 폴랑) · 서포터 축 · 연계 딜러 · 축복 kind 고르기.
// 지침: _measure/사도_리워크_지침.md(§12) · 보고: _measure/리워크_요정.md · 공용 부품: lib.mjs
// node _gen/rework/요정.mjs [사도 이름 일부]  → heroes/요정/<사도>.json 덮어쓰기(백업에서 읽음)
import { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifAll, ifBroken, inspire, power, rule, later, O, B, card, starter, run } from './lib.mjs';

// ── 이 스크립트 몫 부품 ──
const dmod = (v) => ({ k: 'dealtMod', v, target: 'allAllies' });               // 이번 턴 파티 주는 피해 +v
const spendE = id => ({ k: 'spend', id, all: true, target: E1 });                // 적 표식 전부 소모(그 적)
const extra = (ratio, o = {}) => ({ k: 'extra', ratio, ...o });
const cleanse = v => ({ k: 'cleanse', v });
const exileAll = tag => ({ k: 'exileFrom', from: 'hand', all: true, tag });
const reach = (K, n, fx, o = {}) => rule('stackReach', fx, { ...o, when: { id: K, n } });
const stage = (K, n) => ifStack(K, n, { max: n });                               // 태세 단계 조건
const swapCards = (j, list) => { j.cards = [...j.cards.filter(c => !c.unique), ...list]; };
// 3단계 부품
const srch = (v = 1) => draw(v, { who: 'self', unique: true });                  // 자신의 고유 카드 서치
const disc = v => ({ k: 'discard', v });                                         // 손패 v장 버리기(대가)
const exileBasic = (n = 1) => ({ k: 'exileFrom', from: 'hand', basic: true, n }); // 손의 시작 카드 소멸(기본 카드 연료)
const drawBasic = v => draw(v, { who: 'self', basic: true });                    // 자신의 시작 카드 드로우
const pullBasic = { k: 'pull', from: 'discard', who: 'self', basic: true };       // 버린 더미의 시작 카드 1장을 손으로
const token = (id, name, hero, type, fx, tags = ['소멸']) => ({ id, name, hero, token: true, cost: 0, type, tags, fx });
const pw = (on, fx, o = {}) => power(rule(on, fx, o));

// ════════════════════════════════════════════════════════════════════
// 1. 네르 — 서포터(버퍼) · 광기. 졸면서 올리는 「꾸벅 기도」 셋이 차면 계시(이번 턴 파티 피해↑) — 아니면 오함마에 다 실어 친다
// 원작: 저학년 「세계수의 계시」(모든 아군 피해량 크게, 7초만) · 어사이드 「사제장의 무적권」(받는 피해↓) · 졸면 기도라 우김 · 오함마 불량배 시절
// ④ 「여왕님 앞은 못 지나가요」 는 강화로 남김 — 원작 어사이드 「사제장의 무적권」 이 상시 효과
// ════════════════════════════════════════════════════════════════════
function ner(j) {
  const H = '네르', K = '꾸벅 기도';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '졸다 들키면 우기는 기도', carrier: 'self', cap: 3,
    rules: [{ name: '세계수의 계시', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), dmod(0.3), draw(1)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '잠이 아니라 기도', when: { on: 'turnStart' }, fx: [stk(K, 1)] },
    { name: '사제장의 무적권', when: { on: 'hurt' }, limit: { per: 'turn', n: 1 }, fx: [st('피해 감소', 1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 2) : f));
  const sw = ['신속'], bz = ['분쇄'];
  swapCards(j, [
    // u1 ① 열기 — 0코 기도 + 드로우(시동 카드)
    card(H, 1, '꿈으로 올리는 기도', 0, '스킬', [stk(K, 1), draw(1)], [
      O('졸린 눈', [stk(K, 2), draw(1)], { tags: sw }),
      O('꿈속 대예배', [stk(K, 1), draw(1), pw('turnStart', [stk(K, 1)])], { tags: sw, power: true }),
      O('육아일기 한 페이지', [stk(K, 1), draw(2, { who: 'other' })], { tags: sw }),
      O('마시멜로 마카롱', [stk(K, 1), inspire, stk(K, 3)], { tags: sw }),
      O('기도 중이었어요', [stk(K, 3), draw(1), disc(1)], { tags: ['보존'] }),
    ], [B('세계수의 꿈', 'draw'), B('갓난 여왕의 보모', { tags: ['보존'] }), B('잠꼬대 기도', [stk(K, 1)])], { tags: sw }),
    // u2 ③ 터뜨리기 — 기도를 다 실어 오함마(계시와 맞바꿈)
    card(H, 2, '뒷골목 오함마', 1, '공격', [ddef(0.7), per(K), ddef(0.25), spendAll(K)], [
      O('불량배 시절의 한 방', [ddef(0.9), per(K), ddef(0.3), spendAll(K)], { tags: bz }),
      O('빵 나눠 자르기', [ddef(0.55, EA), per(K), ddef(0.2, EA), spendAll(K)], { tags: [] }),
      O('오함마 챙기기', [ddef(1.15), srch(1)], { tags: bz }),
      O('여왕을 노린 죄', [ddef(0.7), per(K), ddef(0.25), ifBroken, ddef(0.6)], { tags: bz }),
      O('반창고와 오함마', [ddef(0.6), per(K), ddef(0.2), power(reach(K, 3, [ddef(0.3, EA)]))], { tags: bz, power: true }),
    ], [B('오함마 손질', 'weakSpot'), B('사탕 껍데기', 'cost'), B('불량배의 기도', [stk(K, 1)])], { tags: bz }),
    // u3 원작 저학년 — 기도를 단숨에 채워 계시를 지금(그림 짝 = 저학년 아이콘 칸)
    card(H, 3, '세계수의 계시', 1, '스킬', [st('사기', 1), stk(K, 3)], [
      O('워리어즈 생츄어리', [st('사기', 1), stk(K, 3), st('협공', 1)]),
      O('서둘러 내린 계시', [stk(K, 3), sh(0.6)], { cost: 0 }),
      O('세계수의 이름으로!', [st('사기', 1), stk(K, 3), power(reach(K, 3, [st('협공', 1)]))], { power: true }),
      O('예언의 꿈', [st('사기', 1), stk(K, 3), draw(1)], { tags: ['개전'] }),
      O('성군 에르핀', [st('사기', 1), stk(K, 3), ifAll, draw(2, { who: 'other' })]),
    ], [B('엘드르의 깃발', 'ap'), B('세계수 화단', 'cost'), B('계시의 여운', [stk(K, 1)])]),
    // u4 ④ 완성형(강화 유지 — 원작 상시) — 피해 감소 담당 + 계시마다 실드
    card(H, 4, '여왕님 앞은 못 지나가요', 1, '강화', [st('피해 감소', 2), power(reach(K, 3, [sh(0.8)]))], [
      O('왕좌 앞의 사제장', [st('피해 감소', 2), power(reach(K, 3, [sh(1.1)]))]),
      O('왕실 경호', [st('피해 감소', 1), power(reach(K, 3, [sh(0.7)]))], { cost: 0 }),
      O('도끼를 진짜로', [ddef(0.6, EA), power(reach(K, 3, [ddef(0.4, EA)]))]),
      O('무적의 기도', [stk(K, 2), power(rule('turnStart', [stk(K, 1)]), reach(K, 3, [sh(0.8)]))]),
      O('갓난 여왕을 지키는 도끼', [st('피해 감소', 2), power(reach(K, 3, [sh(0.8)])), ifWounded, heal(1.0)]),
    ], [B('여왕 특별 보좌관', 'guard'), B('화단 관리', 'defUp'), B('사제장의 축복', [st('사기', 1)])]),
  ]);
  starter(j, '네르_u1');
  for (const e of j.equips || []) {
    e.effect = [
      { name: '행사 깃발', when: { on: 'turnStart' }, fx: [sh(0.4)] },
      { name: '행사 깃발', when: { on: 'play', type: '스킬', every: 3 }, fx: [heal(0.6)] },
    ];
    e.affinityEffect = [{ name: '깃발 아래 기도', when: { on: 'fightStart' }, fx: [stk(K, 1)] }];
  }
}

// ════════════════════════════════════════════════════════════════════
// 2. 네르(빡침) — 딜러 · 활발 · 엘다인. 「빡침」이 다섯 차면 저절로 성전 모드(엘다인 한 단계) — 거대 도끼창에 쏟으면 변신이 풀린다
// 원작: 저학년 「성전 선포」(광역 + 휘장이 아군 피해를 나눔) · 강화 평타 거대 도끼창 · 고학년 빡침 상태 · 「감정이 격해지면 저절로 변신」
// ④ 「양익의 맹세」 → 빡침을 세는 1코 광역 마무리(쏟지 않음 — 성전 모드 유지). 옛 강화 엔진은 신탁 「분노의 화신」 으로
// ════════════════════════════════════════════════════════════════════
function nerRage(j) {
  const H = '네르_빡침', K = '빡침';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '여왕을 지키려다 치미는 분노', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }], onMax: { form: '네르_빡침_성전' } };
  delete h.keywords;
  h.passives = [
    { name: '분노의 공명', when: { on: 'hurt' }, limit: { per: 'turn', n: 3 }, fx: [stk(K, 1)] },
    { name: '가끔은 그리운 순간', when: { on: 'lowHp', pct: 0.3 }, fx: [stk(K, 5), st('피해 감소', 2)] },
  ];
  const lk = ['연계'];
  swapCards(j, [
    // u1 ① 열기 — 원작 저학년 성전 선포(광역 + 빡침 둘)
    card(H, 1, '성전 선포', 1, '공격', [dmg(0.7, EA), stk(K, 2)], [
      O('성전의 휘장', [dmg(0.9, EA), stk(K, 2)]),
      O('대성전 선포', [dmg(1.4, EA), stk(K, 3), st('피해 감소', 1)], { cost: 2 }),
      O('모두의 분노', [dmg(0.6, EA), stk(K, 2), draw(1, { who: 'other' })]),
      O('새로이 쌓아갈 추억', [dmg(0.5, EA), stk(K, 2), pw('play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 })], { power: true }),
      O('맹세의 휘장', [sh(2.1), st('피해 감소', 1), stk(K, 2)]),
    ], [B('일일 요정 여왕', 'power'), B('왕관의 수여자', 'draw'), B('꼬마 여왕님', [stk(K, 1)])]),
    // u2 ③ 터뜨리기 — 빡침을 다 쏟는 거대 도끼창(쏟으면 성전 모드가 풀린다)
    card(H, 2, '거대 도끼창', 2, '공격', [dmg(1.3, EA), per(K), dmg(0.3, EA), spendAll(K)], [
      O('도끼창 낙하', [dmg(1.65, EA), per(K), dmg(0.35, EA), spendAll(K)]),
      O('작은 도끼창', [dmg(0.75, EA), per(K), dmg(0.2, EA), spendAll(K)], { cost: 1 }),
      O('분노 삭이기', [dmg(1.6, EA), ifStack(K, 5), dmg(0.6, EA)]),
      O('휘장의 벼락', [dmg(1.0, EA), per(K), dmg(0.22, EA), pw('hurt', [stk(K, 1)], { limit: 1 })], { power: true }),
      O('사냥감 찾기', [dmg(1.8, EA), draw(1, { who: 'self', type: '공격' }), spendAll(K)]),
    ], [B('빵 자를 칼', 'weakSpot'), B('아침 해장국', 'ap'), B('벽돌 집착', [stk(K, 1)])]),
    // u3 ② 굴리기 — 동료가 움직이면 뛰어드는 연계(공짜 발동이라 ×0.7)
    card(H, 3, '교주님한테서 손 떼!', 1, '공격', [dmg(0.45, E1, { hits: 2 }), stk(K, 1)], [
      O('진심 분노', [dmg(0.6, E1, { hits: 2 }), stk(K, 1)], { tags: lk }),
      O('혼자 뛰어들기', [dmg(0.75, E1, { hits: 2 }), stk(K, 2)], { tags: [] }),
      O('몸으로 막기', [sh(1.7), stk(K, 2)], { tags: lk }),
      O('교주 지키기', [dmg(0.45, E1, { hits: 2 }), stk(K, 1), draw(1, { who: 'other' })], { tags: lk }),
      O('경호 태세', [dmg(0.45, E1, { hits: 2 }), stk(K, 1), pw('play', [dmg(0.45)], { when: { who: 'other', type: '공격' }, limit: 1 })], { tags: [], power: true }),
    ], [B('사제장의 눈', 'frost'), B('교주 바라기', 'cost'), B('잔소리 한 바가지', [stk(K, 1)])], { tags: lk }),
    // u4 ④ 1코 마무리 — 빡침을 세는 광역(쏟지 않음)
    card(H, 4, '양익의 맹세', 1, '공격', [dmg(0.45, EA), per(K), dmg(0.12, EA)], [
      O('굳은 맹세', [dmg(0.6, EA), per(K), dmg(0.15, EA)]),
      O('분노의 화신', [stk(K, 2), power(reach(K, 5, [dmg(0.5, EA)]))], { power: true }),
      O('폴랑과의 약속', [dmg(0.45, EA), per(K), dmg(0.12, EA), draw(1, { who: 'other' })]),
      O('한쪽 날개', [dmg(1.0), per(K), dmg(0.25), spendAll(K)]),
      O('여왕을 위하여', [sh(1.0), st('피해 감소', 1), stk(K, 2)]),
    ], [B('티그와 대련', 'atkUp'), B('해장국 한 그릇', 'draw'), B('맹세의 날개', [st('피해 감소', 1)])]),
  ]);
  starter(j, '네르_빡침_u1');
}

// ════════════════════════════════════════════════════════════════════
// 3. 로니 — 딜러 · 광기. 한 놈에게 「현상금」을 쌓아 끝까지 판다(다른 적을 치면 처음부터) — 결투로 현상금을 몽땅 받아 낸다
// 원작: 강화 평타로 공격력 높은 적을 무법자로 · 저학년 소닉붐 · 어사이드 「정의의 심판」
// ④ 「불침번」 → 현상금을 세는 1코 마무리. 옛 강화 엔진(공격마다 추가 공격)은 신탁 「쌍권총 불침번」 으로
// ════════════════════════════════════════════════════════════════════
function roni(j) {
  const H = '로니', K = '현상금';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '끝까지 쫓는 현상범의 몸값', carrier: 'enemy', hunt: true, cap: 5, per: [{ stat: 'taken', v: 0.12, from: 'owner' }],
    rules: [
      { name: '정의의 심판', when: { on: 'huntDown' }, fx: [dmod(0.2), stk(K, 1, 'topEnemy')] },
      { name: '석양의 결투', when: { on: 'foeActBefore', type: '공격' }, conds: [{ c: 'stack', id: K, n: 2 }], limit: { per: 'turn', n: 1 }, fx: [dmg(0.8, E1)] },
    ],
  };
  delete h.keywords;
  h.passives = [
    { name: '현상범 지정', when: { on: 'fightStart' }, fx: [stk(K, 1, 'topEnemy')] },
    { name: '끝까지 추적', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1, E1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 1, E1) : f));
  swapCards(j, [
    // u1 ① 열기 — 원작 저학년 소닉붐(현상금을 박는다)
    card(H, 1, '진압용 소닉붐', 1, '공격', [dmg(1.0), stk(K, 1, E1)], [
      O('충격파 증폭', [dmg(1.3), stk(K, 1, E1)]),
      O('톤파 견제', [dmg(0.65), stk(K, 1, E1)], { cost: 0 }),
      O('무법자 침묵', [dmg(1.0), stk(K, 1, E1), ifStack(K, 3), st('취약', 2, E1)]),
      O('현상범 재지정', [dmg(1.3), stk(K, 2, E1), disc(1)]),
      O('수배지 돌리기', [dmg(1.0), stk(K, 1, E1), draw(1, { who: 'self', type: '공격' })]),
    ], [B('황금손 배지', 'power'), B('노력의 증표', 'draw'), B('수배 전단', [stk(K, 1, E1)])]),
    // u2 ③ 터뜨리기 — 현상금을 몽땅 받아 내는 결투
    card(H, 2, '석양의 결투', 1, '공격', [dmg(0.8), per(K), dmg(0.25), spendE(K)], [
      O('해 질 녘 일격', [dmg(1.0), per(K), dmg(0.3), spendE(K)]),
      O('마지막 석양', [dmg(1.8), per(K), dmg(0.45), spendE(K)], { cost: 2, tags: ['약점 공격'] }),
      O('미제사건은 끝까지', [dmg(0.8), per(K), dmg(0.25), { k: 'ifWounded', target: E1 }, dmg(0.8)]),
      O('불침번 교대', [dmg(0.8), per(K), dmg(0.2), pw('huntDown', [draw(2)])], { power: true }),
      O('다음 현상범', [dmg(0.8), per(K), dmg(0.25), srch(1)]),
    ], [B('서부의 법칙', 'weakSpot'), B('방아쇠', 'ap'), B('현상금 인상', [stk(K, 1, E1)])]),
    // u3 ② 굴리기 — 두 번 치고, 현상금이 셋이면 취약(취약 담당)
    card(H, 3, '부러진 리코더', 1, '공격', [hits(2, 0.55, E1), ifStack(K, 3), st('취약', 1, E1)], [
      O('새 리코더', [hits(2, 0.72, E1), ifStack(K, 3), st('취약', 1, E1)]),
      O('짧은 한 소절', [hits(2, 0.35, E1), ifStack(K, 3), st('취약', 1, E1)], { cost: 0 }),
      O('리코더 박살', [hits(2, 0.6, E1), per(K), dmg(0.2), spendE(K)]),
      O('울 땐 엉엉', [hits(2, 0.55, E1), ifStack(K, 3), st('취약', 1, E1), ifBroken, dmg(0.8)]),
      O('악보 찾기', [hits(2, 0.55, E1), draw(1, { who: 'self', type: '공격' })]),
    ], [B('불협화음', 'frost'), B('한 박자 쉬기', 'cost'), B('리코더 신호', [stk(K, 1, E1)])]),
    // u4 ④ 1코 마무리 — 현상금을 세어 친다(받아 내지 않음)
    card(H, 4, '불침번', 1, '공격', [dmg(0.7), per(K), dmg(0.2)], [
      O('철야 불침번', [dmg(0.9), per(K), dmg(0.25)]),
      O('잠깐 눈 붙이기', [dmg(0.5), per(K), dmg(0.15)], { cost: 0 }),
      O('쌍권총 불침번', [dmg(0.7), per(K), dmg(0.2), pw('play', [extra(0.35)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      O('석양 아래 불침번', [dmg(0.7), per(K), dmg(0.2), ifKill, draw(2)]),
      O('밤샘 정산', [dmg(0.9), per(K), dmg(0.3), spendE(K)]),
    ], [B('모래바람', 'atkUp'), B('낡은 망토', 'draw'), B('별빛 아래', [heal(0.4)])]),
  ]);
  starter(j, '로니_u1');
}

// ════════════════════════════════════════════════════════════════════
// 4. 리코타 — 탱커 · 냉정. 카드를 낼 때마다 「풀코스 차례」가 전채 → 메인 → 디저트로 — 차례에 맞는 카드를 내면 덤(태세형)
// 원작: 저학년 「리코타 풀코스」 · 넷째 평타 웍질 · 어사이드 퐁듀 분수 · 「유미미? 미미!」
// 기본 카드 연료: 시작 카드를 「재료」 로 — 손의 시작 카드를 손질해(소멸) 드로우 · 버린 시작 카드를 다시 손으로
// ④ 「요리를 무시하지 마십시오」 → 디저트 차례를 보는 1코 마무리. 옛 강화 엔진은 신탁 「개점 준비」 로
// ════════════════════════════════════════════════════════════════════
function ricota(j) {
  const H = '리코타', K = '풀코스 차례';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '전채부터 디저트까지 차리는 코스', carrier: 'self', cap: 3, wrap: true, stages: ['전채', '메인', '디저트'],
    rules: [{ name: '오늘의 디저트', when: { on: 'stackReach', id: K, n: 3 }, fx: [heal(0.5)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '코스 진행', when: { on: 'fightStart' }, fx: [stk(K, 1)] },
    { name: '코스 진행', when: { on: 'play' }, fx: [stk(K, 1)] },
    { name: '웍질', when: { on: 'play', type: '공격', every: 3 }, fx: [ddef(0.3, EA), heal(0.3)] },
  ];
  const bz = ['분쇄'];
  swapCards(j, [
    // u1 ① 열기 — 0코 실드, 전채면 드로우(시동 카드)
    card(H, 1, '아뮤즈 부쉬', 0, '스킬', [sh(0.8), stage(K, 1), draw(1)], [
      O('한입 가득 아뮤즈', [sh(1.05), stage(K, 1), draw(1)]),
      O('코스 건너뛰기', [sh(0.7), stk(K, 1), draw(1)]),
      O('재료 손질', [sh(0.6), exileBasic(1), draw(2)]),
      O('손님 맞이', [sh(0.8), stage(K, 1), draw(1), stage(K, 3), heal(0.6)]),
      O('퐁듀 분수', [sh(0.8), draw(1), power(reach(K, 3, [draw(1)]))], { power: true }),
    ], [B('정갈한 접시', 'guard'), B('식전주', 'draw'), B('셰프의 추천', [stk(K, 1)])]),
    // u2 ② 굴리기 — 메인이면 더 조린다(방어 기반)
    card(H, 2, '다 조려버리겠습니다', 1, '공격', [ddef(0.7), stage(K, 2), ddef(0.45)], [
      O('센 불로 조리기', [ddef(0.9), stage(K, 2), ddef(0.55)], { tags: bz }),
      O('진짜 재료 의혹', [ddef(1.0), ifBroken, heal(0.8)], { tags: bz }),
      O('파인애플 피자 소각', [ddef(1.4)], { tags: ['분쇄', '종극'] }),
      O('풀코스 메인 디쉬', [ddef(1.5), stage(K, 2), ddef(0.8), sh(1.0)], { cost: 2, tags: bz }),
      O('웍질 수련', [ddef(0.6), pw('play', [ddef(0.25, EA)], { when: { type: '공격' }, limit: 1 })], { tags: bz, power: true }),
    ], [B('날 선 식칼', 'power'), B('친절한 접객', 'cost'), B('메인 요리 준비', [stk(K, 1)])], { tags: bz }),
    // u3 원작 저학년 — 실드 + 회복, 디저트면 결의(세기형 하나는 남긴다)
    card(H, 3, '리코타 풀코스', 1, '스킬', [sh(1.2), heal(0.5), stage(K, 3), st('결의', 1)], [
      O('미슐랭 코스', [sh(1.5), heal(0.6), stage(K, 3), st('결의', 1)]),
      O('간단한 코스', [sh(0.8), heal(0.35), stage(K, 3), st('결의', 1)], { cost: 0 }),
      O('유미미? 미미!', [sh(1.2), st('결의', 1), power(reach(K, 3, [heal(0.5), sh(0.5)]))], { power: true }),
      O('코스 서빙', [sh(1.2), st('결의', 1), draw(1, { who: 'other' })]),
      O('남은 재료로', [sh(1.2), st('결의', 1), pullBasic]),
    ], [B('달콤한 마무리', 'heal'), B('대출금 걱정', 'ap'), B('디저트 추가', [stk(K, 1)])]),
    // u4 ④ 1코 마무리 — 반격 담당, 디저트 차례면 웍질 광역
    card(H, 4, '요리를 무시하지 마십시오', 1, '공격', [st('반격', 2), ddef(0.5), stage(K, 3), ddef(0.5, EA)], [
      O('셰프의 분노', [st('반격', 2), ddef(0.65), stage(K, 3), ddef(0.65, EA)]),
      O('한 마디 경고', [st('반격', 1), ddef(0.4), stage(K, 3), ddef(0.4, EA)], { cost: 0 }),
      O('개점 준비', [st('반격', 2), ddef(0.75), power(reach(K, 3, [ddef(0.35, EA), heal(0.3)]))], { tags: ['개전'], power: true }),
      O('진상 손님 퇴출', [st('반격', 3), ddef(0.9), ifWounded, sh(1.6)]),
      O('주방 점검', [st('반격', 2), ddef(1.0), srch(1)]),
    ], [B('하얀 앞치마', 'defUp'), B('우주식량 트라우마', { tags: ['보존'] }), B('셰프의 고집', [st('결의', 1)])]),
  ]);
  starter(j, '리코타_u1');
}

// ════════════════════════════════════════════════════════════════════
// 5. 마리 — 딜러 · 활발. 공격할 때마다 「특제 폭탄」(0코 광역 · 소멸)이 손에 — 던질수록 「폭파 실험」이 들뜨고, 기폭 스위치로 한꺼번에
// 원작: 저학년 「폭탄 배달 왔어용~」 · 강화 평타 확률 강화 폭탄 · 고학년 고폭탄 · 황금손 좀도둑 · 존재감 자학
// ════════════════════════════════════════════════════════════════════
function marie(j) {
  const H = '마리', K = '폭파 실험', BOMB = '마리_bomb';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '폭발에 들뜨는 탐험가의 눈', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.15 }] };
  delete h.keywords;
  h.passives = [
    { name: '강화 폭탄', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [make(BOMB, 1)] },
    { name: '황금손 마리', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [make(BOMB, 1), draw(1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 2) : f));
  j.cards = j.cards.filter(c => c.id !== BOMB);
  j.cards.push(token(BOMB, '특제 폭탄', H, '공격', [dmg(0.35, EA), stk(K, 1)]));
  const lk = ['연계'], sw = ['신속'];
  swapCards(j, [
    // u1 ② 굴리기 — 원작 저학년(광역 + 폭탄 한 개)
    card(H, 1, '폭탄 배달 왔어용~', 1, '공격', [dmg(0.75, EA), make(BOMB, 1)], [
      O('특급 배달', [dmg(0.95, EA), make(BOMB, 1)]),
      O('폭탄 공장', [dmg(0.6, EA), pw('turnStart', [make(BOMB, 1)])], { power: true }),
      O('배달 사고', [make(BOMB, 3), draw(1)]),
      O('반물질 중성자 폭탄', [dmg(0.75, EA), make(BOMB, 1), ifStack(K, 3), dmg(0.4, EA)]),
      O('왕국 하나쯤', [dmg(1.4, EA), make(BOMB, 2), st('잔불', 1, EA)], { cost: 2 }),
    ], [B('화약 듬뿍', 'power'), B('날개 펄럭', 'atkUp'), B('정령 호수 지뢰', [make(BOMB, 1)])]),
    // u2 ③ 터뜨리기 — 「폭파 실험」을 다 쏟는 기폭
    card(H, 2, '기폭 스위치', 1, '스킬', [dmg(0.3, EA), per(K), dmg(0.3, EA), spendAll(K)], [
      O('고폭 기폭', [dmg(0.4, EA), per(K), dmg(0.38, EA), spendAll(K)]),
      O('손 안 대고 기폭', [dmg(0.38, EA), per(K), dmg(0.3, EA)]),
      O('연쇄 기폭', [dmg(0.3, EA), perTag(BOMB), dmg(0.5, EA), exileAll(BOMB)]),
      O('한 놈만 기폭', [dmg(0.5), per(K), dmg(0.45), spendAll(K)]),
      O('실험 일지', [dmg(0.3, EA), per(K), dmg(0.25, EA), pw('exhaust', [stk(K, 1)], { limit: 2 })], { power: true }),
    ], [B('빨간 버튼', 'ap'), B('원격 장치', 'cost'), B('반짝이는 유물', [stk(K, 1)])]),
    // u3 ① 열기 — 0코 폭탄 + 드로우(시동 카드)
    card(H, 3, '황금손의 추억', 0, '스킬', [make(BOMB, 1), draw(1)], [
      O('황금손 전성기', [make(BOMB, 2), draw(1)], { tags: sw }),
      O('금고 털이', [make(BOMB, 1), draw(2, { who: 'self', type: '공격' })], { tags: sw }),
      O('좀도둑 버릇', [make(BOMB, 1), inspire, make(BOMB, 2)], { tags: sw }),
      O('지도 그리기', [draw(1), pw('turnStart', [make(BOMB, 1)])], { tags: sw, power: true }),
      O('가출 통로', [make(BOMB, 3), disc(1)], { tags: ['보존'] }),
    ], [B('슬쩍', 'draw'), B('날쌘 손', { tags: ['보존'] }), B('주머니 속 폭탄', [make(BOMB, 1)])], { tags: sw }),
    // u4 연계 — 존재감 자학: 동료 뒤에 저절로(×0.7)
    card(H, 4, '저도 여기 있어요!', 1, '공격', [dmg(0.85), stk(K, 1)], [
      O('진짜 여기 있어요!', [dmg(1.1), stk(K, 1)], { tags: lk }),
      O('혼자서도 있어요', [dmg(1.5), stk(K, 1)], { tags: [] }),
      O('존재감 폭발', [dmg(0.75), per(K), dmg(0.18)], { tags: lk }),
      O('에르핀의 첫 친구', [dmg(0.85), stk(K, 1), ifAll, make(BOMB, 1)], { tags: lk }),
      O('같이 가요', [dmg(0.85), stk(K, 1), draw(1, { who: 'other' })], { tags: lk }),
    ], [B('큰 날개', 'power'), B('지붕 조심', 'frost'), B('설탕 뿌리기', [heal(0.3)])], { tags: lk }),
  ]);
  starter(j, '마리_u3');
}

// ════════════════════════════════════════════════════════════════════
// 6. 마요 — 딜러 · 광기. 독침으로 적에게 「마취독」 — 넷이면 움직이지 않는 수집품, 경쟁자 견제로 다 거둬 간다
// 원작: 저학년 「수집의 법칙임.」 · 고학년 무작위 여덟 번 · 「더 이상 움직이지 않는 수집품」 · 수집가 · 전당포
// 부 장치(3단계): 처치하면 「박제 수집품」(0코 · 소멸 — 마취독 둘 + 드로우)이 손에
// ④ 「흐흐…」 → 마취독을 세는 1코 마무리. 옛 강화 엔진(매 턴 적 전체 마취독)은 신탁 「이미 내꺼임」 으로
// ════════════════════════════════════════════════════════════════════
function mayo(j) {
  const H = '마요', K = '마취독', TR = '마요_trophy';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '수집품을 재우는 독침의 독', carrier: 'enemy', cap: 4, per: [{ stat: 'dot', ratio: 0.18 }],
    rules: [{ name: '움직이지 않는 수집품', when: { on: 'stackReach', id: K, n: 4 }, limit: { per: 'turn', n: 1 }, fx: [st('약화', 1), { k: 'tough', v: 1 }] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '마취 독침', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 3 }, fx: [stk(K, 1, E1)] },
    { name: '전당포임', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [make(TR, 1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 2, EA) : f));
  j.cards = j.cards.filter(c => c.id !== TR);
  j.cards.push(token(TR, '박제 수집품', H, '스킬', [stk(K, 2, E1), draw(1)]));
  const bz = ['분쇄'];
  swapCards(j, [
    // u1 ① 열기 — 원작 저학년 세 번 + 마취독(시동 카드)
    card(H, 1, '수집의 법칙임.', 1, '공격', [hits(3, 0.38, E1), stk(K, 1, E1)], [
      O('수집가의 눈', [hits(3, 0.5, E1), stk(K, 1, E1)]),
      O('진짜 보물', [hits(5, 0.42, E1), stk(K, 2, E1), st('약화', 1, E1)], { cost: 2 }),
      O('박제 이야기', [hits(3, 0.38, E1), stk(K, 1, E1), ifStack(K, 3), make(TR, 1)]),
      O('특징으로 부르기', [hits(3, 0.4, E1), per(K), dmg(0.15)]),
      O('수집품 점검', [hits(3, 0.38, E1), stk(K, 1, E1), srch(1)]),
    ], [B('권총 코코', 'power'), B('자물쇠 따기', 'draw'), B('독침 장전', [stk(K, 1, E1)])]),
    // u2 ③ 터뜨리기 — 마취독을 다 거둬 가는 견제(분쇄)
    card(H, 2, '경쟁자 견제', 1, '공격', [dmg(0.8), per(K), dmg(0.3), spendE(K)], [
      O('정실 경쟁자 견제', [dmg(1.0), per(K), dmg(0.35), spendE(K)], { tags: bz }),
      O('가벼운 견제', [dmg(0.55), per(K), dmg(0.22), spendE(K)], { cost: 0, tags: bz }),
      O('오래 지켜보기', [dmg(0.95), per(K), dmg(0.28)], { tags: bz }),
      O('밤에 30분', [per(K), dmg(0.5), spendE(K), ifKill, make(TR, 1)], { tags: bz }),
      O('경매장 낙찰', [dmg(0.8), per(K), dmg(0.3), pw('kill', [make(TR, 1)], { limit: 1 })], { tags: bz, power: true }),
    ], [B('경매장', 'weakSpot'), B('무표정', 'cost'), B('진열장 한 칸', [make(TR, 1)])], { tags: bz }),
    // u3 ② 굴리기 — 무작위 독침 + 적 전체 마취독
    card(H, 3, '마취 독침 세례', 1, '공격', [hits(4, 0.25), stk(K, 1, EA)], [
      O('독침 폭우', [hits(4, 0.33), stk(K, 1, EA)]),
      O('독침 대방출', [hits(8, 0.3), stk(K, 2, EA)], { cost: 2 }),
      O('수집품 목록', [hits(4, 0.25), stk(K, 1, EA), draw(1, { who: 'self' })]),
      O('수집품 회수', [dmg(0.7), per(K), dmg(0.3), spendE(K)]),
      O('독 바르기', [dmg(0.5), st('약화', 1, E1), stk(K, 3, E1)]),
    ], [B('마취 효과', 'frost'), B('잠옷 차림', 'ap'), B('바늘 연마', [stk(K, 1, EA)])]),
    // u4 ④ 1코 마무리 — 그 적의 마취독을 세어 친다(거두지 않음)
    card(H, 4, '흐흐…', 1, '공격', [dmg(0.5), per(K), dmg(0.18)], [
      O('흐흐흐…', [dmg(0.65), per(K), dmg(0.22)]),
      O('이미 내꺼임', [dmg(0.5), per(K), dmg(0.18), pw('turnStart', [stk(K, 1, EA)])], { tags: ['개전'], power: true }),
      O('반항하는 수집품', [dmg(0.5), per(K), dmg(0.18), ifKill, make(TR, 1)]),
      O('전당포 진열장', [dmg(0.5), per(K), dmg(0.18), draw(1, { who: 'self', type: '공격' })]),
      O('독 거두기', [dmg(0.6), per(K), dmg(0.25), spendE(K)]),
    ], [B('음침한 웃음', 'atkUp'), B('사진 한 장', 'draw'), B('하나뿐인 수집품', [stk(K, 1, E1)])]),
  ]);
  starter(j, '마요_u1');
}

// ════════════════════════════════════════════════════════════════════
// 7. 마요(멋짐) — 딜러 · 순수(연계 딜러 — 채우기). 동료가 스킬을 낼 때마다 「자랑하고 싶음」 — 여섯이면 공짜 방패가 튕긴다
// 원작: 저학년 「최강의 수집품임.」 + 아군 저학년마다 「자랑하고 싶음」, 열이면 SP 없이 열 번 · 고학년 소음 · 어사이드 위기에 해제 + 회복
// 시동(3단계): 개전 강화 「빨리 나 칭찬해줌.」 — 첫 턴부터 매 턴 자랑
// ④ 「나만의 교주」 → 자랑을 세는 1코 마무리. 옛 강화 엔진은 신탁 「언제 어디서든」 으로
// ════════════════════════════════════════════════════════════════════
function mayoCool(j) {
  const H = '마요_멋짐', K = '자랑하고 싶음';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '멋지다는 말이 듣고 싶은 마음', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.08 }],
    rules: [{ name: '최강의 수집품', when: { on: 'stackReach', id: K, n: 6 }, fx: [spendAll(K), hits(6, 0.35)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '칭찬 요구', when: { on: 'play', who: 'other', type: '스킬' }, limit: { per: 'turn', n: 3 }, fx: [stk(K, 1)] },
    { name: '훌륭한 수집품', when: { on: 'lowHp', pct: 0.3 }, fx: [cleanse(1), heal(1.0), stk(K, 3)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 1) : f));
  const op = ['개전'], tw = pw('turnStart', [stk(K, 1)]);
  swapCards(j, [
    // u1 ① 개전 강화 시동 — 매 턴 자랑 하나
    card(H, 1, '빨리 나 칭찬해줌.', 1, '강화', [stk(K, 2), draw(1), tw], [
      O('더 칭찬해줌.', [stk(K, 3), draw(1), tw], { tags: op }),
      O('멋진 요정 등장', [stk(K, 2), draw(1), tw], { cost: 0, tags: [] }),
      O('교주의 손', [sh(1.0), draw(2, { who: 'other', type: '스킬' }), pw('play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 1 })], { tags: op }),
      O('칭찬 스티커', [sh(1.2), stk(K, 2), tw], { tags: op }),
      O('며칠째 광장에', [stk(K, 3), tw, inspire, stk(K, 3)], { tags: op }),
    ], [B('뽐내기', 'draw'), B('반짝 왕관', 'cost'), B('걸걸한 목소리', [stk(K, 1)])], { tags: op }),
    // u2 ② 굴리기 — 종소리(약화 담당) · 원작 고학년 소음
    card(H, 2, '은방울꽃 종', 1, '공격', [dmg(0.95), st('약화', 1), stk(K, 1)], [
      O('커다란 종', [dmg(1.2), st('약화', 1), stk(K, 1)]),
      O('종소리 증폭', [dmg(0.65, EA), st('약화', 1, EA), stk(K, 1)]),
      O('자랑할 만한 소리', [dmg(0.95), st('약화', 1), ifStack(K, 4), stk(K, 2)]),
      O('종 흔들기', [dmg(0.6), st('약화', 2), draw(1, { who: 'other', type: '스킬' })]),
      O('은방울꽃 화관', [dmg(0.8), st('약화', 1), pw('play', [stk(K, 1)], { when: { type: '스킬' }, limit: 1 })], { power: true }),
    ], [B('은방울꽃', 'power'), B('종 닦기', 'ap'), B('딸랑딸랑', [stk(K, 1)])]),
    // u3 ③ 터뜨리기 — 원작 저학년: 튕기는 방패 + 자랑 1개당 한 번 더
    card(H, 3, '최강의 수집품임.', 1, '공격', [hits(3, 0.3), per(K), dmg(0.25, ER), spendAll(K)], [
      O('최최강의 수집품', [hits(3, 0.4), per(K), dmg(0.3, ER), spendAll(K)]),
      O('열 번 튕기기', [hits(6, 0.35), per(K), dmg(0.35, ER), spendAll(K)], { cost: 2 }),
      O('아끼는 방패', [hits(3, 0.38), per(K), dmg(0.25, ER)]),
      O('한 놈만 튕기기', [hits(3, 0.35, E1), srch(1), spendAll(K)]),
      O('방패 들고 달리기', [hits(3, 0.3), per(K), dmg(0.2, ER), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
    ], [B('수집품의 자아', 'weakSpot'), B('등을 미는 바람', 'draw'), B('실라 닮은 돌', [stk(K, 1)])]),
    // u4 ④ 1코 마무리 — 자랑을 세어 튕긴다(쏟지 않음)
    card(H, 4, '나만의 교주', 1, '공격', [hits(2, 0.3), per(K), dmg(0.12, ER)], [
      O('주머니 속 교주', [hits(2, 0.4), per(K), dmg(0.15, ER)]),
      O('언제 어디서든', [stk(K, 2), power(reach(K, 6, [hits(3, 0.3)]))], { power: true }),
      O('마요꺼임.', [hits(2, 0.3), per(K), dmg(0.12, ER), draw(1, { who: 'other', type: '스킬' })]),
      O('훌륭한 수집품임.', [sh(0.9), stk(K, 2)]),
      O('최고로 멋진 요정', [hits(3, 0.35), per(K), dmg(0.18, ER), spendAll(K)]),
    ], [B('교주의 손길', 'atkUp'), B('실종된 친구 사진', 'draw'), B('세일 때 주운 옷', { tags: ['보존'] })]),
  ]);
  starter(j, '마요_멋짐_u1');
}

// ════════════════════════════════════════════════════════════════════
// 8. 샤샤 — 딜러 · 우울. 공격마다 차오르는 「텀블러 수압」 — 넷이면 텀블러가 멋대로 거대 물방울(광역 + 둔화)
// 원작: 저학년 「텀블러 투척!」 · 넷째 평타 거대 물방울 · 고학년 물대포 + 넉백 · 제멋대로인 텀블러 · 잡일 대행
// 기본 카드 연료: 「물 길어 오기」 — 손의 시작 카드를 물로(소멸) 수압 둘
// ④ 「불 하나는 잘 꺼요」 → 수압을 세는 1코 광역 마무리. 옛 강화 엔진은 신탁 「출동 대기」 로
// ════════════════════════════════════════════════════════════════════
function shasha(j) {
  const H = '샤샤', K = '텀블러 수압';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '제멋대로 차오르는 텀블러 물', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.12 }],
    rules: [{ name: '거대 물방울', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(0.8, EA), st('둔화', 1, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '대용량 텀블러', when: { on: 'fightStart' }, fx: [stk(K, 1)] },
    { name: '대용량 텀블러', when: { on: 'play', type: '공격' }, fx: [stk(K, 1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 2) : f));
  const bz = ['분쇄'];
  swapCards(j, [
    // u1 ① 열기 — 원작 저학년(광역 두 번 + 약화 담당 + 수압)
    card(H, 1, '텀블러 투척!', 1, '공격', [dmg(0.35, EA, { hits: 2 }), st('약화', 1, EA), stk(K, 1)], [
      O('텀블러 내리꽂기', [dmg(0.45, EA, { hits: 2 }), st('약화', 1, EA), stk(K, 1)]),
      O('텀블러 비우기', [dmg(0.55, EA, { hits: 2 }), st('약화', 1, EA), spendAll(K)]),
      O('텀블러 맡기기', [dmg(0.4, EA, { hits: 2 }), stk(K, 2), draw(1)]),
      O('꼭 필요할 때', [dmg(0.35, EA, { hits: 2 }), st('약화', 1, EA), ifStack(K, 2), stk(K, 2)]),
      O('잡일 대행', [dmg(0.35, EA, { hits: 2 }), stk(K, 1), drawBasic(1)]),
    ], [B('정령산 호수 물', 'power'), B('개구리 튀어나옴', 'draw'), B('텀블러 채우기', [stk(K, 1)])]),
    // u2 ③ 터뜨리기 — 수압을 한 놈에게 다 쏟는 물줄기
    card(H, 2, '물줄기 발사', 1, '공격', [hits(2, 0.4, E1), per(K), dmg(0.3), spendAll(K)], [
      O('고압 물줄기', [hits(2, 0.52, E1), per(K), dmg(0.35), spendAll(K)]),
      O('졸졸 물줄기', [hits(2, 0.28, E1), per(K), dmg(0.22), spendAll(K)], { cost: 0 }),
      O('수압 유지', [hits(2, 0.5, E1), per(K), dmg(0.26)]),
      O('물대포 쓸기', [dmg(0.5, EA, { hits: 2 }), st('둔화', 1, EA), spendAll(K)]),
      O('텀블러 훈련', [hits(2, 0.4, E1), per(K), dmg(0.2), pw('turnStart', [stk(K, 1)])], { power: true }),
    ], [B('쫄딱 젖음', 'weakSpot'), B('차원문 물길', 'ap'), B('물 한 모금', [stk(K, 1)])]),
    // u3 ② 굴리기 — 0코 물방울(분쇄)
    card(H, 3, '물방울 연사', 0, '공격', [dmg(0.7), stk(K, 1)], [
      O('큰 물방울', [dmg(0.95), stk(K, 1)], { tags: bz }),
      O('연사 또 연사', [dmg(0.35, E1, { hits: 2 }), stk(K, 1), draw(1, { who: 'self', type: '공격' })], { tags: bz }),
      O('흩뿌리는 물방울', [dmg(0.5, EA), stk(K, 1)], { tags: bz }),
      O('물 길어 오기', [exileBasic(1), stk(K, 2), draw(1)], { tags: bz }),
      O('180도 달라진 샤샤', [dmg(1.6), per(K), dmg(0.4)], { tags: ['소멸'] }),
    ], [B('말 더듬기', 'frost'), B('뽀송한 중심가', 'cost'), B('물방울 탄창', [stk(K, 1)])], { tags: bz }),
    // u4 ④ 1코 마무리 — 해제 + 수압을 세는 광역(쏟지 않음)
    card(H, 4, '불 하나는 잘 꺼요', 1, '공격', [cleanse(1), dmg(0.45, EA), per(K), dmg(0.12, EA)], [
      O('특별 소방 대장', [cleanse(1), dmg(0.58, EA), per(K), dmg(0.15, EA)]),
      O('물 면역 샤샤', [cleanse(1), dmg(0.3, EA), per(K), dmg(0.1, EA)], { cost: 0 }),
      O('출동 대기', [dmg(0.45, EA), per(K), dmg(0.12, EA), pw('turnStart', [stk(K, 1)])], { tags: ['개전'], power: true }),
      O('장난 신고엔 물대포', [dmg(0.6, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      O('잡일 대행 접수', [dmg(0.45, EA), per(K), dmg(0.12, EA), draw(1, { who: 'self', type: '공격' })]),
    ], [B('우비', 'defUp'), B('소방 직함', { tags: ['보존'] }), B('장화', [sh(0.4)])]),
  ]);
  starter(j, '샤샤_u1');
}

// ════════════════════════════════════════════════════════════════════
// 9. 슈팡 — 서포터(AP) · 활발. 누가 내든 신속 카드마다 「배달 속도」 — 넷이면 슈파볼트 질주(회복 + AP)
// 원작: 저학년 「무책임 배달부」(우편 두 번 왕복 · 흘릴 때마다 회복) · 고학년 질주 · 신속 배달 사명감
// 부 장치(3단계): 「택배 상자」(0코 신속 · 소멸 — 무작위 적 피해) — 신속이라 배달 속도를 채운다
// ④ 「손가락 인사」 → 배달 속도를 세는 1코 마무리. 옛 강화 엔진은 신탁 「출발 인사」 로
// ════════════════════════════════════════════════════════════════════
function shoupan(j) {
  const H = '슈팡', K = '배달 속도', PC = '슈팡_parcel';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '슈파볼트를 모는 속도광의 속도', carrier: 'self', cap: 4,
    rules: [{ name: '슈파볼트 질주', when: { on: 'stackReach', id: K, n: 4 }, limit: { per: 'turn', n: 1 }, fx: [spendAll(K), heal(1.0), ap(1)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '과속 배달', when: { on: 'play', who: 'any', tag: '신속' }, limit: { per: 'turn', n: 3 }, fx: [stk(K, 1)] },
    { name: '배달의 요정', when: { on: 'fightStart' }, fx: [{ k: 'gauge', v: 30 }, st('결의', 1)] },
  ];
  j.cards = j.cards.filter(c => c.id !== PC);
  j.cards.push(token(PC, '택배 상자', H, '공격', [dmg(0.4, ER)], ['신속', '소멸']));
  const sw = ['신속'];
  swapCards(j, [
    // u1 ① 열기 — 원작 저학년 회복 + 속도 + 흘린 택배(시동 카드)
    card(H, 1, '무책임 배달부', 1, '스킬', [heal(0.9), stk(K, 1), make(PC, 1)], [
      O('왕복 두 번', [heal(1.5), stk(K, 1), make(PC, 1)], { tags: sw }),
      O('몰아서 배달', [heal(1.3), make(PC, 2), spendAll(K)], { tags: sw }),
      O('문 늦게 여는 집', [heal(2.0), make(PC, 1), ifWounded, heal(1.45)], { tags: sw }),
      O('배송 동선', [heal(1.2), make(PC, 1), draw(1, { tag: '신속' })], { tags: sw }),
      O('특급 배송 계약', [heal(0.8), pw('turnStart', [make(PC, 1)])], { tags: sw, power: true }),
    ], [B('3시간 수면', 'heal'), B('상사 계급장', 'ap'), B('속도위반 딱지', [stk(K, 1)])], { tags: sw }),
    // u2 ② 굴리기 — 0코 속도 + 드로우
    card(H, 2, '슈팡은⋯ 달리고 싶다!', 0, '스킬', [stk(K, 1), draw(1)], [
      O('질주 본능', [stk(K, 2), draw(1)], { tags: sw }),
      O('새 지역', [stk(K, 1), draw(2, { tag: '신속' })], { tags: sw }),
      O('일 없이도 달리기', [draw(1), pw('turnStart', [stk(K, 1)])], { tags: sw, power: true }),
      O('⋯슈팡', [stk(K, 1), inspire, stk(K, 3)], { tags: sw }),
      O('정령산 정상', [stk(K, 3), draw(2), disc(1)], { tags: [] }),
    ], [B('로켓엔진', 'ap'), B('정비', { tags: ['보존'] }), B('바람 맞기', [stk(K, 1)])], { tags: sw }),
    // u3 ③ 터뜨리기 — 속도를 다 써서 광역 들이받기(방어 기반)
    card(H, 3, '케이크 위 코너링', 1, '공격', [ddef(0.4, EA), per(K), ddef(0.15, EA), spendAll(K)], [
      O('드리프트', [ddef(0.52, EA), per(K), ddef(0.18, EA), spendAll(K)], { tags: sw }),
      O('엘리아스 횡단', [ddef(0.85, EA), per(K), ddef(0.28, EA), spendAll(K)], { tags: sw, cost: 2 }),
      O('택배 쏟기', [ddef(0.3, EA), perTag(PC), ddef(0.25, EA), exileAll(PC)], { tags: sw }),
      O('코너링 연습', [ddef(0.35, EA), per(K), ddef(0.12, EA), pw('play', [stk(K, 1)], { when: { tag: '신속' }, limit: 1 })], { tags: sw, power: true }),
      O('경찰 추격전', [ddef(0.4, EA), per(K), ddef(0.15, EA), ifKill, stk(K, 2)], { tags: sw }),
    ], [B('슈파볼트', 'power'), B('미끄러운 크림', 'cost'), B('택배 한 상자', [make(PC, 1)])], { tags: sw }),
    // u4 ④ 1코 마무리 — 약화 담당 + 배달 속도를 세어 던진다(쓰지 않음)
    card(H, 4, '손가락 인사', 1, '공격', [st('약화', 2), dmg(0.4, ER), per(K), dmg(0.15, ER)], [
      O('양손 인사', [st('약화', 2), dmg(0.52, ER), per(K), dmg(0.2, ER)], { tags: sw }),
      O('출발 인사', [st('약화', 2), dmg(0.4, ER), power(reach(K, 4, [dmg(0.5, ER)]))], { tags: ['신속', '개전'], power: true }),
      O('앙탈', [st('약화', 2), make(PC, 2)], { tags: sw }),
      O('휙', [st('약화', 2), dmg(0.5, ER), draw(2, { tag: '신속' })], { tags: sw }),
      O('배달 완료', [dmg(0.6, ER), per(K), dmg(0.25, ER), spendAll(K)], { tags: sw }),
    ], [B('경적', 'frost'), B('헬멧', 'draw'), B('거침없는 반말', [heal(0.3)])], { tags: sw }),
  ]);
  starter(j, '슈팡_u1');
}

// ════════════════════════════════════════════════════════════════════
// 10. 스키아 — 딜러 · 광기(연계 딜러 — 끊기). 카드를 쥔 채 턴을 넘길 때마다 「삼킨 감탄사」 — 동료가 공격하면 하나가 새어 나온다
// 원작: 묵언의 맹세(감탄사만 새어 나옴) · 저학년 「오래된 맹세」(언약의 매듭 — 고학년 장치로 그대로)
// 시동(3단계): 개전 강화 「…(끄덕)」 — 쥔 카드가 둘이면 턴 끝마다 하나 더
// ④ 「지켜 온 금기」 → 감탄사를 세는 1코 마무리. 옛 강화 엔진(턴 끝 벼락)은 ③ 신탁 「터진 말문」 으로
// ════════════════════════════════════════════════════════════════════
function skia(j) {
  const H = '스키아', K = '삼킨 감탄사';
  const h = j.heroes[0];
  const knot = h.keywords.filter(k => k.name === '언약의 매듭');
  h.keyword = {
    name: K, desc: '묵언 수행으로 삼킨 말', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.15 }],
    rules: [{ name: '깨진 묵언', when: { on: 'play', who: 'other', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [{ k: 'spend', id: K, v: 1 }] }],
  };
  h.keywords = knot;
  h.passives = [
    { name: '묵언 수행', when: { on: 'fightStart' }, fx: [stk(K, 1)] },
    { name: '묵언 수행', when: { on: 'turnEnd' }, conds: [{ c: 'heldCards', n: 1 }], fx: [stk(K, 1)] },
    { name: '소리 없는 기도', when: { on: 'spend', id: K }, limit: { per: 'turn', n: 1 }, fx: [draw(1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === '침묵' ? stk(K, 2) : f));
  const kp = ['보존'], op = ['개전'];
  const held = pw('turnEnd', [stk(K, 1)], { conds: [{ c: 'heldCards', n: 2 }] });
  swapCards(j, [
    // u1 ② 굴리기 — 원작 저학년(보존 — 쥐고 넘기면 참은 말)
    card(H, 1, '오래된 맹세', 1, '공격', [dmg(1.0), stk(K, 1)], [
      O('굳은 맹세', [dmg(1.3), stk(K, 1)], { tags: kp }),
      O('짧은 맹세', [dmg(0.65), stk(K, 1)], { tags: kp, cost: 0 }),
      O('매듭 조이기', [dmg(1.0), stk(K, 1), ifStack(K, 3), st('둔화', 1)], { tags: kp }),
      O('맹세 풀기', [dmg(1.2), per(K), dmg(0.2)], { tags: [] }),
      O('꿈속의 기도', [dmg(0.8), stk(K, 1), pw('spend', [dmg(0.3, EA)], { when: { id: K }, limit: 1 })], { tags: kp, power: true }),
    ], [B('깃털', 'power'), B('검은 날개', 'frost'), B('고대의 언약', [stk(K, 1)])], { tags: kp }),
    // u2 ① 개전 강화 시동 — 쥔 카드가 둘이면 턴 끝마다 감탄사 하나 더
    card(H, 2, '…(끄덕)', 1, '강화', [stk(K, 2), draw(1), held], [
      O('…(끄덕끄덕)', [stk(K, 3), draw(1), held], { tags: op }),
      O('길잡이 등불', [stk(K, 2), draw(1), held], { cost: 0, tags: [] }),
      O('죠안 쓰다듬기', [sh(1.0), draw(2, { who: 'other' }), held], { tags: op }),
      O('읏⋯!', [stk(K, 3), held, inspire, stk(K, 3)], { tags: op }),
      O('무릎베개', [sh(2.0), stk(K, 1), held], { tags: op }),
    ], [B('감탄사 참기', 'draw'), B('먼저 깨어난 사제', 'cost'), B('조용한 염탐', [stk(K, 1)])], { tags: op }),
    // u3 ③ 터뜨리기 — 참은 말을 한꺼번에(광역)
    card(H, 3, '흐읍!?', 1, '공격', [dmg(0.6, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      O('크게 흐읍!?', [dmg(0.78, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      O('말문 터짐', [dmg(1.2, EA), per(K), dmg(0.38, EA), spendAll(K)], { cost: 2 }),
      O('한 명에게 흐읍!?', [dmg(1.0), per(K), dmg(0.35), srch(1)]),
      O('터진 말문', [dmg(0.5, EA), per(K), dmg(0.15, EA), pw('turnEnd', [per(K), dmg(0.12, ER)])], { power: true }),
      O('착각으로 깬 묵언', [dmg(0.6, EA), per(K), dmg(0.2, EA), ifStack(K, 4), st('둔화', 1, EA)]),
    ], [B('사제장 시절', 'weakSpot'), B('벼락', 'ap'), B('초코 케이크', [heal(0.3)])]),
    // u4 ④ 1코 마무리 — 감탄사를 세어 친다(쏟지 않음)
    card(H, 4, '지켜 온 금기', 1, '공격', [dmg(0.7), per(K), dmg(0.2)], [
      O('오래 지킨 금기', [dmg(0.9), per(K), dmg(0.25)]),
      O('첫 기도의 금기', [dmg(0.6), per(K), dmg(0.15), pw('turnStart', [stk(K, 1)])], { tags: op, power: true }),
      O('초대 사제장', [dmg(0.7), per(K), dmg(0.2), draw(1, { who: 'self' })]),
      O('알아선 안 될 것', [st('둔화', 1, EA), stk(K, 3), draw(1)]),
      O('묵언 해제', [dmg(0.8), per(K), dmg(0.25), spendAll(K)]),
    ], [B('금기의 무게', 'atkUp'), B('검은 수도복', 'draw'), B('세계수에 올린 기도', [stk(K, 1)])]),
  ]);
  starter(j, '스키아_u2');
}

// ════════════════════════════════════════════════════════════════════
// 11. 에르핀(왕도) — 딜러 · 순수 · 엘다인. 「왕마력」을 쥐고 다섯이면 매번 모두를 위한 힘(엘다인 한 단계) — X 난타에 몽땅 섞어 쏟을 수도
// 원작: 저학년 「마력 난타」 · 어사이드 「모두를 위한 힘」 · 「모두를 위한 배려」
// ④ 「모두를 위한 무게」 는 강화로 남김 — 원작 어사이드 「모두를 위한 힘」 이 상시 효과(엘다인 완성형 한 장)
// ════════════════════════════════════════════════════════════════════
function erpinRoyal(j) {
  const H = '에르핀_왕도', K = '왕마력';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '되찾은 세계수의 힘', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.06 }],
    rules: [{ name: '모두를 위한 힘', when: { on: 'stackReach', id: K, n: 5 }, limit: { per: 'turn', n: 1 }, fx: [dmod(0.25), st('피해 감소', 1)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '망설임 없는 왕도', when: { on: 'turnStart' }, fx: [stk(K, 1)] },
    { name: '망설임 없는 왕도', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
    { name: '모두를 위한 배려', when: { on: 'lowHp', pct: 0.3 }, fx: [cleanse(1), st('면역', 1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 3) : f));
  const wk = ['약점 공격'];
  const xc = (n, name, fx, oracles, blesses) => ({ ...card(H, n, name, 0, '공격', fx, oracles, blesses), x: true });
  swapCards(j, [
    // u1 ② 굴리기 — 약점 공격 왕마력탄
    card(H, 1, '에르피엔 왕마력탄', 1, '공격', [dmg(1.1), stk(K, 1)], [
      O('대왕마력탄', [dmg(1.4), stk(K, 1)], { tags: wk }),
      O('왕국을 되찾은 한 방', [dmg(2.2), stk(K, 2), { k: 'tough', v: 1 }], { cost: 2, tags: wk }),
      O('왕마력 폭발', [dmg(0.8, EA), stk(K, 1)], { tags: [] }),
      O('딸기 케이크 한 입', [dmg(1.05), per(K), dmg(0.15)], { tags: wk }),
      O('동화책 펼치기', [dmg(1.1), stk(K, 1), draw(1, { who: 'self', type: '공격' })], { tags: wk }),
    ], [B('여왕의 의지', 'power'), B('맞춤법 만점', 'draw'), B('왕마력 충전', [stk(K, 1)])], { tags: wk }),
    // u2 ③ 터뜨리기 — 원작 저학년 X 난타: 남은 AP 와 왕마력을 다 쏟는다
    xc(2, '마력 난타', [dmg(0.6, E1, { xHits: true }), per(K), dmg(0.25), spendAll(K)], [
      O('왕마력 난타', [dmg(0.75, E1, { xHits: true }), per(K), dmg(0.3), spendAll(K)]),
      O('흩어지는 왕마력탄', [dmg(0.65, ER, { xHits: true }), per(K), dmg(0.3, ER), spendAll(K)]),
      O('왕마력은 남겨 두기', [dmg(0.68, E1, { xHits: true }), per(K), dmg(0.24)]),
      O('처치하면 다시', [dmg(0.75, E1, { xHits: true }), ifKill, stk(K, 3)]),
      O('끝없는 난타', [dmg(0.62, E1, { xHits: true }), per(K), dmg(0.22), pw('kill', [stk(K, 1)], { limit: 1 })], { power: true }),
    ], [B('대관식', 'weakSpot'), B('반동 엉덩방아', 'ap'), B('SP 통', [stk(K, 1)])]),
    // u3 ① 열기 — 0코 왕마력 + 드로우(시동 카드)
    card(H, 3, '축복의 왕관', 0, '스킬', [stk(K, 1), draw(1)], [
      O('모두의 축복', [stk(K, 2), draw(1)]),
      O('왕관의 무게', [stk(K, 1), draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      O('동화책 읽어 줘', [stk(K, 1), draw(2, { who: 'self', type: '공격' })]),
      O('존경받는 여왕', [stk(K, 1), inspire, stk(K, 2)]),
      O('대관식 날', [stk(K, 3), disc(1)], { tags: ['개전'] }),
    ], [B('예복', 'atkUp'), B('왕관 광택', { tags: ['보존'] }), B('겨우살이', [stk(K, 1)])]),
    // u4 ④ 완성형(엘다인 · 강화 유지) — 왕마력이 다섯에 닿을 때마다 광역
    card(H, 4, '모두를 위한 무게', 1, '강화', [stk(K, 1), power(reach(K, 5, [dmg(0.4, EA)]))], [
      O('더 무거운 무게', [stk(K, 1), power(reach(K, 5, [dmg(0.55, EA)]))]),
      O('첫 여왕의 무게', [stk(K, 1), power(reach(K, 5, [dmg(0.4, EA)]))], { tags: ['개전'] }),
      O('빵 나눠 주기', [stk(K, 1), draw(1, { who: 'other' }), power(reach(K, 5, [dmg(0.4, EA)]))]),
      O('매일 아침 수업', [power(rule('turnStart', [stk(K, 1)]), reach(K, 5, [dmg(0.3, EA)]))]),
      O('모두가 기댈 여왕', [sh(1.0), power(reach(K, 5, [dmg(0.4, EA), sh(0.5)]))]),
    ], [B('세계수의 힘', 'defUp'), B('폴랑의 체육 수업', 'draw'), B('딸기 한 입', [heal(0.3)])]),
  ]);
  starter(j, '에르핀_왕도_u3');
}

// ════════════════════════════════════════════════════════════════════
// 12. 에슈르 — 딜러 · 우울. 「빵 센디오」를 장전하면 공격마다 불타는 빵(추가 공격 + 고통 담당)이 한 덩이씩
// 원작: 저학년 「빵템피드」(빵 여섯) · 어사이드 「빵 센디오」 · 고학년 빵테오 · 수강생 0명 마법학교(실은 빵집)
// 부 장치(3단계): 「갓 구운 빵」(0코 · 소멸 — 빵 센디오 장전 + 작은 회복) — ④ 그리모어가 손의 빵을 몽땅 태운다
// ④ 「궁극의 그리모어」 → 손의 빵을 세는 1코 마무리. 옛 강화 엔진은 신탁 「마법책빵」(매 턴 빵)으로
// ════════════════════════════════════════════════════════════════════
function eshur(j) {
  const H = '에슈르', K = '빵 센디오', BR = '에슈르_bread';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '평타에 얹는 불타는 빵 주문', carrier: 'self', cap: 6,
    rules: [{ name: '불타는 빵', when: { on: 'play', type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }], fx: [extra(0.5), st('고통', 1), { k: 'spend', id: K, v: 1 }] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '빵빵한 브레드', when: { on: 'fightStart' }, fx: [stk(K, 3)] },
    { name: '빵집 아니고 마법학교', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
  ];
  j.cards = j.cards.filter(c => c.id !== BR);
  j.cards.push(token(BR, '갓 구운 빵', H, '스킬', [stk(K, 1), heal(0.25)]));
  swapCards(j, [
    // u1 ① 열기 — 원작 저학년 빵 세례 + 장전 + 갓 구운 빵(시동 카드)
    card(H, 1, '빵템피드', 1, '공격', [hits(3, 0.3, E1), stk(K, 2), make(BR, 1)], [
      O('빵 여섯 개', [hits(3, 0.4, E1), stk(K, 2), make(BR, 1)]),
      O('대-빵템피드', [hits(6, 0.35, E1), stk(K, 4), make(BR, 2)], { cost: 2 }),
      O('흩어지는 빵', [dmg(0.45, EA, { hits: 2 }), st('고통', 2, EA), make(BR, 1)]),
      O('빵 몽땅 굽기', [hits(3, 0.62, E1), per(K), dmg(0.27), spendAll(K)]),
      O('신상 빵 줄', [hits(3, 0.45, E1), stk(K, 2), draw(1, { who: 'self', type: '공격' })]),
    ], [B('버터 듬뿍', 'power'), B('단골손님', 'draw'), B('오븐 예열', [make(BR, 1)])]),
    // u2 ② 굴리기 — 0코 마력탄
    card(H, 2, '입자 이론', 0, '공격', [dmg(0.65), ifStack(K, 2), draw(1)], [
      O('입자 가속', [dmg(0.85), ifStack(K, 2), draw(1)]),
      O('이론 강의', [dmg(0.8), pw('play', [stk(K, 1)], { when: { type: '스킬' }, limit: 1 })], { power: true }),
      O('입자 관측', [dmg(0.65), ifStack(K, 2), draw(1), ifBroken, stk(K, 2)]),
      O('불확정 이론', [dmg(0.65), stk(K, 1), draw(1)]),
      O('입자 폭주', [dmg(2.2), ifStack(K, 2), draw(1)], { tags: ['소멸'] }),
    ], [B('이론 수업', 'frost'), B('엘레나와 협업', 'ap'), B('수강생 0명', [stk(K, 1)])]),
    // u3 ③ 터뜨리기 — 장전한 빵을 한 번에 몰아 쏘는 화염 주문
    card(H, 3, '화염 주문', 2, '공격', [dmg(2.0), per(K), dmg(0.3), spendAll(K)], [
      O('대화염 주문', [dmg(2.5), per(K), dmg(0.35), spendAll(K)]),
      O('작은 불씨', [dmg(1.15), per(K), dmg(0.22), spendAll(K)], { cost: 1 }),
      O('불바다', [dmg(1.4, EA), st('고통', 2, EA)]),
      O('궁극의 마법 이론', [dmg(1.6), per(K), dmg(0.2), pw('turnStart', [stk(K, 1)])], { power: true }),
      O('세금 고지서', [dmg(2.0), st('고통', 2), ifStack(K, 4), dmg(1.0)]),
    ], [B('화상', 'weakSpot'), B('월세 걱정', 'cost'), B('빵테오', [stk(K, 1)])]),
    // u4 ④ 1코 마무리 — 손의 빵을 몽땅 태운다(1장당 피해)
    card(H, 4, '궁극의 그리모어', 1, '공격', [dmg(0.6), perTag(BR), dmg(0.35), exileAll(BR)], [
      O('궁극의 마법서', [dmg(0.8), perTag(BR), dmg(0.45), exileAll(BR)]),
      O('요약본', [dmg(0.4), perTag(BR), dmg(0.25), exileAll(BR)], { cost: 0 }),
      O('마법책빵', [stk(K, 2), make(BR, 1), pw('turnStart', [make(BR, 1)])], { tags: ['개전'], power: true }),
      O('풀리지 않는 난제', [dmg(0.8), per(K), dmg(0.25)]),
      O('고대 마법 서적', [dmg(0.6), perTag(BR), dmg(0.35), draw(1, { who: 'self', type: '공격' })]),
    ], [B('빵 모자', 'atkUp'), B('에심당 간판', { tags: ['보존'] }), B('시식 코너', [heal(0.3)])]),
  ]);
  starter(j, '에슈르_u1');
}

// ════════════════════════════════════════════════════════════════════
// 13. 에슈르(마도) — 딜러 · 활발(원작 방식 고학년 — 「거울 형상」 그대로). 「마력 증폭」이 있는 동안 피해가 굵고, 적의 차례마다 하나씩 식는다
// 원작: 저학년 「빵타지아」 · 강화 평타 마력 레이저 · 고학년 거울 형상 · 억까의 시간은 끝 · 연구 노트
// 기본 카드 연료: 「연구 노트」(소멸하면 증폭) — 손의 시작 카드를 연구 재료로 태운다
// ④ 「연구 집중」 → 증폭을 세는 1코 마무리. 옛 강화 엔진은 신탁 「연구실 불 켜기」 로
// ════════════════════════════════════════════════════════════════════
function eshurMagi(j) {
  const H = '에슈르_마도', K = '마력 증폭';
  const h = j.heroes[0];
  const mirror = h.keywords.filter(k => k.name === '거울 형상');
  h.keyword = { name: K, desc: '마도술로 끌어올린 마력', carrier: 'self', cap: 3, decay: 1, per: [{ stat: 'dealt', v: 0.2 }] };
  h.keywords = mirror;
  h.passives = [
    { name: '연구 노트', when: { on: 'exhaust' }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1)] },
    { name: '노력의 결실', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === '노트' ? stk(K, 2) : f));
  const ex = ['소멸'];
  swapCards(j, [
    // u1 ② 굴리기 — 0코 소멸 드로우(소멸하면 증폭)
    card(H, 1, '억까의 시간은 끝', 0, '스킬', [draw(2), stk(K, 1)], [
      O('억까 종료 선언', [draw(2), stk(K, 2)], { tags: ex }),
      O('억까 보관', [draw(2), stk(K, 1)], { tags: ['보존'] }),
      O('연구 자료', [srch(2), exileBasic(1), stk(K, 2)], { tags: ex }),
      O('증명할 기회', [stk(K, 3), st('취약', 1, EA)], { tags: ex }),
      O('밤샘 연구', [draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
    ], [B('노트 정리', 'draw'), B('평가 뒤집기', { tags: ['신속'] }), B('잔소리 차단', [stk(K, 1)])], { tags: ex }),
    // u2 ③ 터뜨리기 — 증폭을 다 실은 레이저
    card(H, 2, '마력 레이저', 2, '공격', [dmg(2.0), per(K), dmg(0.5), spendAll(K)], [
      O('극대 마력 레이저', [dmg(2.5), per(K), dmg(0.6), spendAll(K)]),
      O('시험 발사', [dmg(1.15), per(K), dmg(0.32), spendAll(K)], { cost: 1 }),
      O('광역 레이저', [dmg(1.6, EA), st('취약', 1, EA)]),
      O('모든 걸 건 증명', [dmg(4.6), per(K), dmg(0.9), exileBasic(2)], { tags: ex }),
      O('억까는 끝', [dmg(1.6), per(K), dmg(0.4), pw('kill', [stk(K, 2)], { limit: 1 })], { power: true }),
    ], [B('빛나는 노트', 'power'), B('세계수 연구', 'ap'), B('증폭 회로', [stk(K, 1)])]),
    // u3 ① 열기 — 원작 저학년 빵타지아(광역 + 취약 + 증폭 둘)
    card(H, 3, '빵타지아', 1, '공격', [dmg(0.7, EA), st('취약', 1, EA), stk(K, 2)], [
      O('화려한 마도술', [dmg(0.9, EA), st('취약', 1, EA), stk(K, 2)]),
      O('한 점 빵타지아', [dmg(1.1), st('취약', 2, E1), stk(K, 2)]),
      O('증명 완료', [dmg(0.7, EA), st('취약', 1, EA), ifStack(K, 1), stk(K, 3)]),
      O('연구 실습', [dmg(0.7, EA), exileBasic(1), stk(K, 3)]),
      O('마법 빵 후보', [dmg(0.72, EA), stk(K, 2), draw(1, { who: 'self', type: '공격' })]),
    ], [B('새 복장', 'frost'), B('빵 뽕', 'cost'), B('밤샘 연구', [stk(K, 1)])]),
    // u4 ④ 1코 마무리 — 증폭을 세어 친다(쓰지 않음)
    card(H, 4, '연구 집중', 1, '공격', [dmg(0.8), per(K), dmg(0.3)], [
      O('깊은 연구 집중', [dmg(1.0), per(K), dmg(0.38)]),
      O('연구실 불 켜기', [dmg(0.6), stk(K, 1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'], power: true }),
      O('대마법 연구 노트', [dmg(0.8), exileBasic(1), draw(2)]),
      O('마법의 빵 비밀 공식', [dmg(0.8), per(K), dmg(0.3), draw(1, { who: 'self', type: '공격' })]),
      O('증명 끝', [dmg(1.0), per(K), dmg(0.4), spendAll(K)]),
    ], [B('공격 속도', 'atkUp'), B('대-에슈르의 조언', 'weakSpot'), B('빵 한 입', [heal(0.3)])]),
  ]);
  starter(j, '에슈르_마도_u3');
}

// ════════════════════════════════════════════════════════════════════
// 14. 죠안 — 서포터(실드) · 우울 · 엘다인. 「형상」(꿈결 → 심판 → 축복)을 넘기며 교리가 바뀐다 — 축복에 닿을 때마다 한 단계 더(엘다인)
// 원작: 저학년 「교리를 행하고」(자신 · 아군 둘에게 분산 + 공격력↑ 받는 피해↓) · 고학년 꿈결 형상 · 무에서 빵 · 교주일지
// ④ 「교리를 행하고」 는 강화로 남김 — 원작 저학년이 상시 버프(엘다인 완성형 한 장)
// ════════════════════════════════════════════════════════════════════
function joanne(j) {
  const H = '죠안', K = '형상';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '꿈 계시를 받던 사제의 모습', carrier: 'self', cap: 3, wrap: true, stages: ['꿈결', '심판', '축복'] };
  delete h.keywords;
  const c = n => [{ c: 'stack', id: K, n, max: n }];
  h.passives = [
    { name: '형상의 교리', when: { on: 'fightStart' }, fx: [stk(K, 1)] },
    { name: '형상의 교리', when: { on: 'play' }, conds: c(1), limit: { per: 'turn', n: 1 }, fx: [draw(1)] },
    { name: '형상의 교리', when: { on: 'play', type: '공격' }, conds: c(2), limit: { per: 'turn', n: 2 }, fx: [ddef(0.3, EA)] },
    { name: '형상의 교리', when: { on: 'play' }, conds: c(3), limit: { per: 'turn', n: 2 }, fx: [sh(0.6)] },
    { name: '형상의 교리', when: { on: 'play' }, conds: c(2), limit: { per: 'fight', n: 1 }, fx: [st('사기', 1)] },
    { name: '주교의 경전', when: { on: 'lowHp', pct: 0.3 }, fx: [st('피해 감소', 2), heal(1.0)] },
  ];
  swapCards(j, [
    // u1 ① 열기 — 형상을 넘기는 기도(시동 카드)
    card(H, 1, '계시의 기도', 0, '스킬', [stk(K, 1), draw(1)], [
      O('연이은 계시', [stk(K, 1), draw(2)]),
      O('축복의 계시', [stk(K, 1), draw(1), stage(K, 3), heal(0.5)]),
      O('교주일지', [stk(K, 1), draw(2, { who: 'other' })]),
      O('교주님 가라사대', [draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      O('흩어진 계시', [stk(K, 2), disc(1), draw(2)]),
    ], [B('세라핌의 눈', 'draw'), B('경건한 아침', { tags: ['개전'] }), B('베일의 날개', [stk(K, 1)])]),
    // u2 ② 굴리기 — 사슬(방어 기반), 심판이면 적 전체 약화(약화 담당)
    card(H, 2, '사슬 심판', 1, '공격', [ddef(0.7), stage(K, 2), st('약화', 1, EA)], [
      O('무거운 사슬', [ddef(0.9), stage(K, 2), st('약화', 1, EA)]),
      O('사슬 휘감기', [ddef(0.45, EA), stage(K, 2), st('약화', 1, EA)]),
      O('사슬 끌어오기', [ddef(0.7), stage(K, 2), st('약화', 1, EA), draw(1, { who: 'other' })]),
      O('심판의 형상', [ddef(0.7), stage(K, 2), st('약화', 1, EA), stage(K, 3), heal(0.6)]),
      O('사슬 방벽', [sh(1.0), st('약화', 2, EA)]),
    ], [B('맨주먹', 'power'), B('사슬 손질', 'ap'), B('비 오는 날 먼지', [stk(K, 1)])]),
    // u3 회복 — 축복이면 빵이 더
    card(H, 3, '무에서 빵을', 1, '스킬', [heal(1.0), stage(K, 3), heal(0.6)], [
      O('빵! 빵! 빵!', [heal(1.3), stage(K, 3), heal(0.7)]),
      O('한 입 빵', [heal(0.6), stage(K, 3), heal(0.4)], { cost: 0 }),
      O('여러 맛 빵', [heal(0.8), draw(1), stage(K, 3), heal(0.6)]),
      O('생크림 빵', [sh(1.2), stage(K, 3), sh(0.6)]),
      O('금욕의 빵', [heal(1.0), stage(K, 3), heal(0.5), power(rule('play', [heal(0.3)], { conds: c(3), limit: 1 }))], { power: true }),
    ], [B('생크림', 'heal'), B('갓 구운 향', 'cost'), B('딸기 케이크 금욕', [sh(0.4)])]),
    // u4 원작 저학년 · ④ 완성형(엘다인 · 강화 유지) — 사기 + 축복에 닿을 때마다 실드 · 피해 감소
    card(H, 4, '교리를 행하고', 1, '강화', [st('사기', 1), power(rule('play', [sh(0.6), st('피해 감소', 1)], { conds: c(3), limit: 1 }))], [
      O('깊은 교리', [st('사기', 1), sh(0.6), power(rule('play', [sh(0.7), st('피해 감소', 1)], { conds: c(3), limit: 1 }))]),
      O('아침 기도', [st('사기', 1), power(rule('play', [sh(0.6), st('피해 감소', 1)], { conds: c(3), limit: 1 }))], { tags: ['개전'] }),
      O('분산', [st('사기', 1), power(rule('play', [sh(0.6), st('피해 감소', 1)], { conds: c(3), limit: 1 }), rule('play', [ddef(0.3, EA)], { when: { type: '공격' }, conds: c(2), limit: 1 }))]),
      O('교주를 향한 헌신', [st('사기', 1), draw(1, { who: 'other' }), power(rule('play', [sh(0.6), st('피해 감소', 1)], { conds: c(3), limit: 1 }))]),
      O('짧은 교리', [sh(0.6), power(rule('play', [sh(0.6), st('피해 감소', 1)], { conds: c(3), limit: 1 }))], { cost: 0 }),
    ], [B('주교의 경전', 'defUp'), B('교주 기록', 'draw'), B('형상의 기도', [stk(K, 1)])]),
  ]);
  starter(j, '죠안_u1');
  for (const f of h.forms || []) for (const p of f.passives || []) if (p.name === '꿈결의 교리') p.fx = [sh(1.1), draw(1), st('사기', 1)];
}

// ════════════════════════════════════════════════════════════════════
// 15. 카렌 — 딜러 · 활발(연계 딜러 — 채우기). 공격할 때마다 「시청자 수」 — 동료가 스킬을 내면 합방으로 하나 더, 당근 치유로 다 써서 회복
// 원작: 1성 힐러 — 저학년 「당근 치유」 · 엘튜버(구독자 · 악플 · 생식 컨셉 위선 논란 · 합방)
// ════════════════════════════════════════════════════════════════════
function karen(j) {
  const H = '카렌', K = '시청자 수';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '생방송을 지켜보는 시청자들', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [
      { name: '떡상 방송', when: { on: 'stackReach', id: K, n: 5 }, limit: { per: 'fight', n: 3 }, fx: [st('사기', 1), ap(1)] },
      { name: '합방', when: { on: 'play', who: 'other', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
    ] };
  delete h.keywords;
  h.passives = [
    { name: '켜진 방송', when: { on: 'fightStart' }, fx: [stk(K, 2)] },
    { name: '켜진 방송', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 3 }, fx: [stk(K, 1)] },
    { name: '떡상', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
    { name: '떡상', when: { on: 'break', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 3) : f.k === 'perStack' ? per(K) : f));
  swapCards(j, [
    // u1 ② 굴리기 — 악플러 추적(취약 담당)
    card(H, 1, '어그로 댓글 추적 방송', 1, '공격', [dmg(1.0), st('취약', 1), stk(K, 1)], [
      O('범인 검거', [dmg(1.3), st('취약', 1), stk(K, 1)]),
      O('UFC 알바생 추적', [dmg(2.2), st('취약', 2), stk(K, 2)], { cost: 2 }),
      O('악플 박제', [dmg(0.8), st('취약', 2), draw(1, { who: 'other', type: '스킬' })]),
      O('방송 종료', [dmg(0.9), per(K), dmg(0.2), spendAll(K)]),
      O('실시간 댓글', [dmg(1.0), st('취약', 1), per(K), dmg(0.12)]),
    ], [B('셀카봉', 'power'), B('조회수', 'draw'), B('알고리즘의 선택', [stk(K, 1)])]),
    // u2 ② 굴리기 — 마법쇼(무작위 세 번)
    card(H, 2, '엘튜브 생방송 마법쇼', 1, '공격', [hits(3, 0.38), stk(K, 1)], [
      O('구독 좋아요', [hits(3, 0.5), stk(K, 1)]),
      O('쇼츠', [hits(2, 0.38), stk(K, 1)], { cost: 0 }),
      O('협찬 광고', [hits(3, 0.38), stk(K, 1), srch(1)]),
      O('합방 무대', [hits(3, 0.3), per(K), dmg(0.1, ER)]),
      O('정기 방송', [hits(3, 0.3), pw('turnStart', [stk(K, 1)])], { power: true }),
    ], [B('화려한 조명', 'atkUp'), B('협찬', 'ap'), B('알림 설정', [stk(K, 1)])]),
    // u3 ③ 터뜨리기 — 원작 저학년 당근 치유: 시청자 1개당 회복, 다 쓴다
    card(H, 3, '당근 치유', 1, '스킬', [heal(0.8), per(K), heal(0.2), spendAll(K)], [
      O('당근 듬뿍 회복', [heal(1.0), per(K), heal(0.25), spendAll(K)]),
      O('생당근 한 입', [heal(0.5), per(K), heal(0.15), spendAll(K)], { cost: 0 }),
      O('당근 텃밭 방송', [heal(0.6), per(K), heal(0.15), power(reach(K, 5, [heal(0.4)]))], { power: true }),
      O('당근 방패', [sh(0.9), per(K), sh(0.25), spendAll(K)]),
      O('기적의 회복 방송', [heal(0.8), per(K), heal(0.2), ifWounded, stk(K, 3)]),
    ], [B('특 플러스 당근', 'heal'), B('생방송 후원', 'cost'), B('언니의 텃밭', [stk(K, 1)])]),
    // u4 ① 열기 — 0코 시청자 + 드로우(시동 카드)
    card(H, 4, '근본 생식 챌린지', 0, '스킬', [stk(K, 1), draw(1)], [
      O('챌린지 2탄', [stk(K, 2), draw(1)]),
      O('구독 이벤트', [draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      O('합방 섭외', [stk(K, 1), draw(2, { who: 'other' })]),
      O('떡상 각', [stk(K, 1), inspire, stk(K, 2)]),
      O('예약 방송', [stk(K, 3), disc(1)], { tags: ['개전'] }),
    ], [B('썸네일', 'draw'), B('해시태그', { tags: ['보존'] }), B('생당근 공포증', 'defUp')]),
  ]);
  starter(j, '카렌_u4');
}

// ════════════════════════════════════════════════════════════════════
// 16. 칸타 — 딜러 · 냉정. 판 위에 「쇠팽이」를 깔아 두면 턴 끝마다 긁고 적의 차례마다 하나씩 멈춘다
// 원작: 넷째 평타마다 쇠팽이 · 저학년 「탑스핀 블레이드」 · 고학년 폭탄팽이 · 도박장 · 진 상대 팽이를 부숴 가져감 · 직접 깎는 팽이
// 시동(3단계): 개전 강화 「소매 속 팽이」(옛 ④ — u2 칸으로) · 기본 카드 연료: 「팽이 깎기」(손의 시작 카드를 깎아 팽이로)
// ④ u4 → 「팽이 회수」 — 깔린 팽이를 세는 1코 마무리
// ════════════════════════════════════════════════════════════════════
function kanta(j) {
  const H = '칸타', K = '쇠팽이';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '판 위를 돌며 긁는 강철 팽이', carrier: 'self', cap: 4,
    rules: [
      { name: '팽이판', when: { on: 'turnEnd' }, fx: [per(K), dmg(0.7, ER)] },
      { name: '튕겨 나간 팽이', when: { on: 'stackOver', id: K }, fx: [{ k: 'perEvent' }, dmg(0.4, ER)] },
    ],
  };
  delete h.keywords;
  h.passives = [
    { name: '네 번째 공격', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 3 }, fx: [stk(K, 1)] },
    { name: '진 놈 팽이 부수기', when: { on: 'break', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
    { name: '진 놈 팽이 부수기', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
  ];
  const op = ['개전'], tw = pw('turnStart', [stk(K, 1)]);
  swapCards(j, [
    // u1 ③ 터뜨리기 — 깔린 팽이를 다 걸고 한 놈에게(도박)
    card(H, 1, '올인', 1, '공격', [dmg(0.8), per(K), dmg(0.35), spendAll(K)], [
      O('한 판 더', [dmg(1.0), per(K), dmg(0.42), spendAll(K)]),
      O('인생 올인', [dmg(1.8), per(K), dmg(0.6), spendAll(K)], { cost: 2 }),
      O('팽이 폭풍', [hits(4, 0.45), st('둔화', 1)]),
      O('반칙은 딴청', [dmg(0.8), per(K), dmg(0.3), ifKill, stk(K, 2)]),
      O('타짜의 손', [dmg(0.7), per(K), dmg(0.3), pw('kill', [stk(K, 2)], { limit: 1 })], { power: true }),
    ], [B('칸타피아', 'power'), B('딴청', 'draw'), B('판돈 올리기', [stk(K, 1)])]),
    // u2 ① 개전 강화 시동 — 매 턴 팽이 하나 더(옛 ④)
    card(H, 2, '소매 속 팽이', 1, '강화', [stk(K, 2), draw(1), tw], [
      O('소매 가득 팽이', [stk(K, 3), draw(1), tw], { tags: op }),
      O('손목 스냅', [stk(K, 2), draw(1), tw], { cost: 0, tags: [] }),
      O('팽이 하우스', [sh(0.7), draw(2, { who: 'self', type: '공격' }), tw], { tags: op }),
      O('팽이 깎기', [exileBasic(1), stk(K, 4), tw], { tags: op }),
      O('들키면 딴청', [sh(1.7), power(rule('turnStart', [stk(K, 1)]), rule('break', [stk(K, 2)], { limit: 1 }))], { tags: op }),
    ], [B('나무 고르는 눈', 'cost'), B('빅우드의 선물', 'draw'), B('팽이채', [stk(K, 1)])], { tags: op }),
    // u3 ② 굴리기 — 원작 저학년(광역 + 마지막 충돌)
    card(H, 3, '탑스핀 블레이드', 1, '공격', [dmg(0.5, EA), dmg(0.45), stk(K, 1)], [
      O('고속 탑스핀', [dmg(0.62, EA), dmg(0.55), stk(K, 1)]),
      O('가벼운 탑스핀', [dmg(0.35, EA), dmg(0.3), stk(K, 1)], { cost: 0 }),
      O('강철팽이 셋', [dmg(0.5, EA), draw(2, { who: 'self', type: '공격' }), stk(K, 1)]),
      O('팽이 거두기', [dmg(0.55, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      O('회전력 유지', [dmg(0.5, EA), dmg(0.45), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
    ], [B('회전력', 'frost'), B('삼각회', 'ap'), B('강철팽이', [stk(K, 1)])]),
    // u4 ④ 1코 마무리 — 깔린 팽이를 세어 친다(걷지 않음)
    card(H, 4, '팽이 회수', 1, '공격', [dmg(0.5), per(K), dmg(0.22)], [
      O('완벽한 회수', [dmg(0.65), per(K), dmg(0.28)]),
      O('미리 숨긴 팽이', [dmg(0.5), per(K), dmg(0.22), power(rule('break', [stk(K, 2)]), rule('kill', [draw(1)], { limit: 1 }))], { tags: op, power: true }),
      O('팽이 방패', [sh(1.4), stk(K, 2), draw(1)]),
      O('반칙', [dmg(0.5), per(K), dmg(0.22), srch(1)]),
      O('판돈 두 배', [dmg(0.8), per(K), dmg(0.35), spendAll(K)]),
    ], [B('커스텀 팽이', 'weakSpot'), B('연회장 계약서', { tags: ['보존'] }), B('스노키와 피라', [heal(0.3)])]),
  ]);
  starter(j, '칸타_u2');
}

// ════════════════════════════════════════════════════════════════════
// 17. 캬롯 — 서포터(드로우) · 순수. 「당근 새싹」(0코 · 소멸 · 드로우)을 심는다 — 바로 먹을까, 두 장 모아 「특 플러스 당근」으로 키울까(진화)
// 원작: 저학년 「탄산수액 발사」 · 넷째 평타 성장 비료 · 어사이드 「당근 신선도 유지」 · 위성에서 보이는 정원
// ④ 「내 정원에 놀러 올래?」 → 손의 새싹을 세는 1코 마무리(먹지 않음). 옛 강화 엔진은 신탁 「먼저 보낸 초대장」 으로
// ════════════════════════════════════════════════════════════════════
function carrot(j) {
  const H = '캬롯', K = '텃밭', SP = '캬롯_sprout', CR = '캬롯_carrot';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '세계수 곁에 일군 정원의 기운', carrier: 'self', cap: 5, per: [{ stat: 'hot', ratio: 0.1 }],
    rules: [{ name: '거름이 된 텃밭', when: { on: 'stackOver', id: K }, fx: [{ k: 'gauge', v: 15 }] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '일등 정원사', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [make(SP, 1)] },
    { name: '쑥쑥 자라라!', when: { on: 'fightStart' }, fx: [make(SP, 1)] },
  ];
  const sp = j.cards.find(c => c.id === SP);
  Object.assign(sp, { name: '당근 새싹', cost: 0, type: '스킬', tags: ['소멸'], evolve: { n: 2, into: CR }, fx: [heal(0.3), stk(K, 1), draw(1)] });
  j.cards = j.cards.filter(c => c.id !== CR);
  j.cards.push({ id: CR, name: '특 플러스 당근', hero: H, token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.6), stk(K, 2), dmod(0.2)] });
  const sw = ['신속'];
  swapCards(j, [
    // u1 원작 저학년 — 이번 턴 파티 주는 피해↑ + 피해 감소 + 새싹
    card(H, 1, '탄산수액 발사', 1, '스킬', [dmod(0.25), st('피해 감소', 1), make(SP, 1)], [
      O('급성장 수액 발사', [dmod(0.35), st('피해 감소', 1), make(SP, 1)]),
      O('수액 소나기', [dmod(0.3), st('피해 감소', 2), make(SP, 1)]),
      O('수액 뒤집어쓰기', [dmg(0.75, EA), st('피해 감소', 1), make(SP, 1)]),
      O('잠시 후 떨어지는 수액', [dmod(0.3), make(SP, 1), ifStack(K, 2), st('피해 감소', 2)]),
      O('햇님이 응원', [dmod(0.25), st('피해 감소', 1), pw('turnStart', [make(SP, 1)])], { power: true }),
    ], [B('톡 쏘는 탄산', 'ap'), B('사탕수수 빨대', 'draw'), B('햇살', [make(SP, 1)])]),
    // u2 ① 열기 — 0코 새싹 + 드로우(시동 카드)
    card(H, 2, '특제 사탕수수 커피', 0, '스킬', [make(SP, 1), draw(1)], [
      O('진한 커피', [make(SP, 2), draw(1)], { tags: sw }),
      O('아침 커피', [draw(1), pw('turnStart', [make(SP, 1)])], { tags: sw, power: true }),
      O('유기농 고집', [make(SP, 1), draw(2, { who: 'other' })], { tags: sw }),
      O('교주도 못 견디는 맛', [make(SP, 1), inspire, make(SP, 1)], { tags: sw }),
      O('테이크아웃', [make(SP, 3), disc(1)], { tags: ['보존'] }),
    ], [B('사탕수수 원두', 'draw'), B('머그컵', { tags: ['보존'] }), B('설탕 한 스푼', [stk(K, 1)])], { tags: sw }),
    // u3 쥐고 있을수록 — 손의 새싹 1장당 실드(원작 어사이드 「당근 신선도 유지」)
    card(H, 3, '당근 신선도 유지', 1, '스킬', [sh(1.1), perTag(SP), sh(0.35)], [
      O('냉장 보관', [sh(1.4), perTag(SP), sh(0.4)]),
      O('신선 포장', [sh(0.75), perTag(SP), sh(0.25)], { cost: 0 }),
      O('아이스 당근당근', [heal(1.3), perTag(SP), heal(0.45)]),
      O('유통 기한', [sh(1.1), perTag(SP), sh(0.3), ifWounded, heal(0.8)]),
      O('싱싱함 가득', [sh(1.2), draw(1), pw('make', [sh(0.3)], { limit: 2 })], { power: true }),
    ], [B('비닐하우스', 'guard'), B('아침 이슬', 'cost'), B('단단한 껍질', 'defUp')]),
    // u4 ④ 1코 마무리 — 손의 새싹을 세어 회복 · 드로우(먹지 않음)
    card(H, 4, '내 정원에 놀러 올래?', 1, '스킬', [heal(0.5), perTag(SP), heal(0.2), draw(1)], [
      O('정원 투어', [heal(0.65), perTag(SP), heal(0.25), draw(1)]),
      O('먼저 보낸 초대장', [make(SP, 2), pw('turnStart', [make(SP, 1)])], { tags: ['개전'], power: true }),
      O('근육 당근', [sh(0.6), make(SP, 2)]),
      O('위성에서 보이는 정원', [heal(0.5), perTag(SP), heal(0.2), draw(2, { who: 'other' })]),
      O('수확 축제', [heal(0.8), perTag(SP), heal(0.45), exileAll(SP)]),
    ], [B('해바라기', 'heal'), B('정원사 가위', 'atkUp'), B('죽창', [make(SP, 1)])]),
  ]);
  starter(j, '캬롯_u2');
  for (const e of j.equips || []) {
    e.effect = [{ name: '달콤한 기운', when: { on: 'play', type: '스킬', every: 3 }, fx: [draw(1)] }];
    e.affinityEffect = [{ name: '먼저 일군 텃밭', when: { on: 'fightStart' }, fx: [make(SP, 1)] }];
  }
}

// ════════════════════════════════════════════════════════════════════
// 18. 큐이 — 서포터(회복) · 순수. 동료에게 「오이 팩」을 붙여 두면 그 동료가 카드를 낼 때 하나씩 떼며 회복
// 원작: 1성 힐러 — 저학년 「오이 오일」 · 거절당해도 또 권하는 오이 포교 · 피부에 바르고 붙이는 데까지
// 부 장치(3단계): 「싱싱한 오이」(0코 · 소멸 — 회복 + 아군 1명에게 오이 팩)
// ════════════════════════════════════════════════════════════════════
function kyui(j) {
  const H = '큐이', K = '오이 팩', CU = '큐이_cucumber';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '동료에게 몰래 붙여 둔 오이', carrier: 'hero', cap: 3, per: [{ stat: 'dealt', v: 0.1 }] };
  delete h.keywords;
  h.passives = [
    { name: '몰래 오이 심기', when: { on: 'play', marked: K }, limit: { per: 'turn', n: 1 }, fx: [heal(0.25), draw(1), { k: 'spend', id: K, v: 1, target: 'oneAlly' }] },
    { name: '오이의 오의', when: { on: 'fightStart' }, fx: [stk(K, 1, 'otherAllies')] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 2, 'otherAllies') : f));
  const A1 = 'oneAlly', AO = 'otherAllies';
  j.cards = j.cards.filter(c => c.id !== CU);
  j.cards.push(token(CU, '싱싱한 오이', H, '스킬', [heal(0.35), stk(K, 1, A1)]));
  swapCards(j, [
    // u1 ① 열기 — 0코 오이 팩 + 드로우(시동 카드)
    card(H, 1, '몰래 오이 심기', 0, '스킬', [stk(K, 1, A1), draw(1)], [
      O('오이 두 개 심기', [stk(K, 2, A1), draw(1)]),
      O('오이 밭 습격', [stk(K, 1, AO), draw(2)]),
      O('거절당해도 또', [stk(K, 1, A1), inspire, make(CU, 1)]),
      O('오이 밭에 물 주기', [draw(1), pw('turnStart', [stk(K, 1, 'strongestAlly')])], { power: true }),
      O('몰래몰래', [make(CU, 2), draw(1, { who: 'other' })]),
    ], [B('오이 꼭지', 'draw'), B('살금살금', { tags: ['보존'] }), B('싱싱한 잎', [make(CU, 1)])]),
    // u2 원작 저학년 — 회복 + 오이 팩
    card(H, 2, '오이 오일', 1, '스킬', [heal(1.1), stk(K, 1, A1)], [
      O('진한 오이 오일', [heal(1.4), stk(K, 1, A1)]),
      O('오일 한 방울', [heal(0.65), stk(K, 1, A1)], { cost: 0 }),
      O('오이 팩 갈아 붙이기', [heal(1.5), stk(K, 2, A1), disc(1)]),
      O('오일 코팅', [sh(1.3), stk(K, 1, A1)]),
      O('오이 나눔', [heal(1.0), make(CU, 2)]),
    ], [B('상큼한 향', 'heal'), B('미끌미끌', 'cost'), B('촉촉한 피부', [stk(K, 1, A1)])]),
    // u3 권유 행렬 — 사기(세기형 하나) + 동료 전원 오이 팩 · 2코 하나
    card(H, 3, '오이를 권하는 행렬', 2, '스킬', [st('사기', 1), heal(1.2), stk(K, 1, AO)], [
      O('오이 대행진', [st('사기', 1), heal(1.6), stk(K, 1, AO)]),
      O('한 입만 권하기', [st('사기', 1), stk(K, 1, AO)], { cost: 1 }),
      O('행렬의 선두', [st('사기', 1), heal(1.2), stk(K, 1, AO)], { tags: ['개전'] }),
      O('온 세상에 오이를', [st('사기', 1), stk(K, 2, AO), draw(1, { who: 'other' })]),
      O('좋아질 때까지', [st('사기', 1), pw('turnStart', [make(CU, 1)])], { power: true }),
    ], [B('오이 냉국', 'heal'), B('행렬 박자', 'ap'), B('흥겨운 장단', [stk(K, 1, AO)])]),
    // u4 공격 — 샌드위치(약화 담당) + 오이 팩
    card(H, 4, '오이 샌드위치 공세', 1, '공격', [dmg(1.0), st('약화', 1), stk(K, 1, A1)], [
      O('두툼한 샌드위치', [dmg(1.3), st('약화', 1), stk(K, 1, A1)]),
      O('샌드위치 뿌리기', [dmg(0.7, EA), st('약화', 1, EA), stk(K, 1, A1)]),
      O('도시락 나눠 먹기', [dmg(0.9), st('약화', 1), draw(2, { who: 'other', type: '공격' })]),
      O('껍질까지 먹어', [dmg(1.0), st('약화', 1), ifBroken, make(CU, 2)]),
      O('오이 다 먹이기', [dmg(0.9), perTag(CU), dmg(0.4), exileAll(CU)]),
    ], [B('바삭한 식빵', 'power'), B('오이 피클', 'frost'), B('소풍', 'draw')]),
  ]);
  starter(j, '큐이_u1');
}

// ════════════════════════════════════════════════════════════════════
// 19. 클로에 — 탱커 · 광기 · 엘다인. 스킬마다 「바늘땀」, 둘이면 「천 조각」 — 천을 셋 겹치면 세바스티안으로(결속), 위기에 세바스티안이 직접(엘다인 한 단계)
// 원작: 저학년 「메리 고 라운드」 · 셋째 평타 내려찍기 · 고학년 쁘띠 세바스티안 일곱 · 재단사
// ④ 「의지를 넘긴 날」 은 강화로 남김 — 언니가 넘긴 의지(결정화)가 상시 효과(엘다인 완성형 한 장)
// ════════════════════════════════════════════════════════════════════
function chloe(j) {
  const H = '클로에', K = '바늘땀', CL = '클로에_cloth', SB = '클로에_sebas';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '인형을 짓는 재단사의 바늘땀', carrier: 'self', cap: 2,
    rules: [{ name: '세바스티안 바느질', when: { on: 'stackReach', id: K, n: 2 }, fx: [spendAll(K), make(CL, 1)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '재단사의 손', when: { on: 'fightStart' }, fx: [stk(K, 1)] },
    { name: '재단사의 손', when: { on: 'play', type: '스킬' }, fx: [stk(K, 1)] },
    { name: '재단사의 손', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
    { name: '셀러브리티 클로에', when: { on: 'lowHp', pct: 0.3 }, fx: [st('피해 감소', 2), make(SB, 1)] },
  ];
  swapCards(j, [
    // u1 ② 굴리기 — 세바스티안 연타(방어 기반 세 번)
    card(H, 1, '세바스티안 연타', 1, '공격', [ddef(0.25, E1, { hits: 3 }), stk(K, 1)], [
      O('인형의 의지', [ddef(0.33, E1, { hits: 3 }), stk(K, 1)]),
      O('휘두르는 앞발', [ddef(0.18, EA, { hits: 3 }), stk(K, 1)]),
      O('마지막 큰 한 방', [ddef(0.25, E1, { hits: 3 }), stk(K, 1), ifStack(K, 1), ddef(0.45)]),
      O('천 조각 휘두르기', [ddef(0.25, E1, { hits: 3 }), perTag(CL), ddef(0.25)]),
      O('세바스티안 훈련', [ddef(0.25, E1, { hits: 3 }), pw('make', [ddef(0.35)], { limit: 2 })], { power: true }),
    ], [B('곰 앞발', 'power'), B('넉백', 'frost'), B('실밥', [stk(K, 1)])]),
    // u2 ① 열기 — 원작 저학년: 실드 + 바늘땀 둘(바로 천 조각)
    card(H, 2, '메리 고 라운드', 1, '스킬', [sh(1.0), stk(K, 2)], [
      O('회전목마', [sh(1.3), stk(K, 2)]),
      O('실밥 뜯기', [sh(1.5), stk(K, 2), disc(1)]),
      O('세바스티안에 올라타기', [sh(0.8), make(SB, 1)]),
      O('흔들리지 않는 의지', [sh(1.0), stk(K, 2), ifWounded, st('피해 감소', 1)]),
      O('재단소 단골', [sh(1.0), stk(K, 2), draw(1, { who: 'other' })]),
    ], [B('콩깍지', 'guard'), B('첫 손님 무료', 'cost'), B('별 문양', [stk(K, 1)])]),
    // u3 쥐고 있을수록 — 손의 천 조각 1장당 실드(피해 감소 담당)
    card(H, 3, '건치 스마일', 1, '스킬', [st('피해 감소', 1), sh(0.9), perTag(CL), sh(0.3)], [
      O('반짝이는 이', [st('피해 감소', 1), sh(1.2), perTag(CL), sh(0.35)]),
      O('살짝 미소', [sh(0.7), perTag(CL), sh(0.25)], { cost: 0 }),
      O('포커페이스', [sh(0.9), make(CL, 1)]),
      O('클로버 문양', [sh(1.1), perTag(CL), sh(0.32), ifWounded, st('피해 감소', 2)]),
      O('흠 있는 인형 뜯기', [perTag(CL), sh(0.8), exileAll(CL), st('피해 감소', 1)]),
    ], [B('다이아몬드', 'guard'), B('물음표', 'draw'), B('새 천 한 필', [make(CL, 1)])]),
    // u4 ④ 완성형(엘다인 · 강화 유지) — 결정화 + 매 턴 바늘땀
    card(H, 4, '의지를 넘긴 날', 1, '강화', [st('결정화', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('넘겨준 모든 힘', [st('결정화', 1), sh(0.8), power(rule('turnStart', [stk(K, 1)]))]),
      O('오래전 황무지', [st('결정화', 1), sh(0.5), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('동생들을 지키라며', [st('결정화', 1), power(rule('turnStart', [stk(K, 1)]), rule('make', [sh(0.3)], { limit: 2 })), sh(0.4)]),
      O('자매의 의지', [st('결정화', 1), draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))]),
      O('작은 의지', [sh(0.8), make(CL, 1), power(rule('turnStart', [stk(K, 1)]))]),
    ], [B('머리 위의 세바스티안', 'defUp'), B('런웨이 오프닝', { tags: ['개전'] }), B('재단 도구', [sh(0.4)])]),
  ]);
  starter(j, '클로에_u2');
}

// ════════════════════════════════════════════════════════════════════
// 20. 파트라 — 딜러 · 냉정. 안 팔리는 「민트 재고」가 매 턴 쌓인다 — 넷이면 재고 처분(광역 + 약화), 그 전에 민트머겅! 으로 한 놈에게
// 원작: 1성 — 저학년 「민트머겅!」 · 민트초코 광 · 안 팔릴 걸 알면서도 잔뜩 만듦 · 샤샤와 소풍
// 시동(3단계): 개전 강화 「중불까지 키워주세요」(u3) · 부 장치: 「민트초코 쿠키」(0코 · 소멸 — 피해 + 재고)
// ════════════════════════════════════════════════════════════════════
function patra(j) {
  const H = '파트라', K = '민트 재고', MT = '파트라_mint';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '아무도 안 사 가는 민트초코', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.12 }],
    rules: [{ name: '재고 처분', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(0.8, EA), st('약화', 1, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '잔뜩 만든 민트', when: { on: 'turnStart' }, fx: [stk(K, 1)] },
    { name: '억지로 먹이기', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 3)] },
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? stk(K, 3) : f));
  j.cards = j.cards.filter(c => c.id !== MT);
  j.cards.push(token(MT, '민트초코 쿠키', H, '공격', [dmg(0.3), stk(K, 1)]));
  const op = ['개전'], tw = pw('turnStart', [stk(K, 1)]);
  swapCards(j, [
    // u1 ③ 터뜨리기 — 원작 저학년: 재고를 한 놈에게 다 먹인다
    card(H, 1, '민트머겅!', 1, '공격', [dmg(0.8), per(K), dmg(0.28), spendAll(K)], [
      O('민트 듬뿍 머겅!', [dmg(1.0), per(K), dmg(0.33), spendAll(K)]),
      O('민트초코 풀코스', [dmg(1.8), per(K), dmg(0.45), spendAll(K)], { cost: 2 }),
      O('다 같이 머겅!', [dmg(0.5, EA), perTag(MT), dmg(0.3, EA), exileAll(MT)]),
      O('요리를 쉽게 보면', [dmg(0.8), per(K), dmg(0.25), ifKill, stk(K, 3)]),
      O('신메뉴 개발', [dmg(0.8), per(K), dmg(0.2), pw('make', [stk(K, 1)], { limit: 2 })], { power: true }),
    ], [B('뒤집개', 'power'), B('빨개진 얼굴', 'weakSpot'), B('민트 향', [stk(K, 1)])]),
    // u2 ② 굴리기 — 반죽 치대기(두 번 + 재고)
    card(H, 2, '민트 반죽 치대기', 1, '공격', [hits(2, 0.55, E1), stk(K, 1)], [
      O('힘껏 치대기', [hits(2, 0.72, E1), stk(K, 1)]),
      O('살살 치대기', [hits(2, 0.36, E1), stk(K, 1)], { cost: 0 }),
      O('반죽 다 쓰기', [hits(2, 0.55, E1), per(K), dmg(0.2), spendAll(K)]),
      O('반죽 던지기', [hits(2, 0.5, E1), make(MT, 1)]),
      O('반죽 재료 챙기기', [hits(2, 0.5, E1), stk(K, 1), draw(1, { who: 'self' })]),
    ], [B('사장님 꿀밤', 'frost'), B('밀가루', 'draw'), B('반죽 숙성', [stk(K, 1)])]),
    // u3 ① 개전 강화 시동 — 매 턴 재고 하나 더
    card(H, 3, '중불까지 키워주세요', 1, '강화', [stk(K, 1), sh(0.6), tw], [
      O('센 불로', [stk(K, 2), sh(0.8), tw], { tags: op }),
      O('약불', [stk(K, 1), sh(0.4), tw], { cost: 0, tags: [] }),
      O('새 조리법', [sh(0.5), draw(2, { who: 'self', type: '공격' }), tw], { tags: op }),
      O('오븐 가득', [make(MT, 2), tw], { tags: op }),
      O('반죽 숙성 중', [stk(K, 2), pw('turnStart', [make(MT, 1)])], { tags: op }),
    ], [B('오븐 장갑', 'defUp'), B('사장님의 비법', 'cost'), B('갓 구운 쿠키', [make(MT, 1)])], { tags: op }),
    // u4 0코 — 소풍 도시락(쿠키 + 드로우)
    card(H, 4, '샤샤와 소풍 도시락', 0, '스킬', [make(MT, 1), draw(1)], [
      O('큰 도시락', [make(MT, 2), draw(1)]),
      O('샤샤와 함께', [make(MT, 1), draw(1, { who: 'other' })], { tags: ['연계'] }),
      O('도시락 나눠 주기', [make(MT, 1), inspire, make(MT, 2)]),
      O('매일 도시락', [draw(1), pw('turnStart', [make(MT, 1)])], { power: true }),
      O('민트 샌드위치', [make(MT, 3), disc(1)]),
    ], [B('돗자리', 'ap'), B('소풍 바구니', { tags: ['보존'] }), B('민트초코 쿠키 한 봉', [heal(0.3)])]),
  ]);
  starter(j, '파트라_u3');
}

// ════════════════════════════════════════════════════════════════════
// 21. 폴랑 — 서포터(버퍼) · 광기. 누가 공격하든 「머스킷 장전」 — 셋이면 일제 사격(광역 + 회복), 셋이서 둘러싸 때리는 왕국의 전통
// 원작: 저학년 「요정 왕국에 경례」 · 셋째 평타 머스킷 장전 · 셋이 둘러싸 때리는 지론 · 경비대장 · 제식 훈련
// 시동(3단계): 개전 강화 「120도 삼각 편대」 · 기본 카드 연료: 「제식 훈련」(시작 카드를 찾아 손에)
// ④ 「경비대장 폴랑입니다」 → 장전을 세는 1코 마무리(협공 + 회복). 옛 강화 엔진은 신탁 「양익」 으로
// ════════════════════════════════════════════════════════════════════
function polan(j) {
  const H = '폴랑', K = '머스킷 장전';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '셋째 발에 맞춰 재는 장전', carrier: 'self', cap: 3,
    rules: [{ name: '일제 사격', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), dmg(0.45, EA), heal(0.4)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '120도 포위', when: { on: 'play', who: 'any', type: '공격' }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1)] },
    { name: '에르피엔 진군깃발', when: { on: 'fightStart' }, fx: [st('협공', 1)] },
  ];
  const op = ['개전'], tw = pw('turnStart', [stk(K, 1)]);
  swapCards(j, [
    // u1 원작 저학년 — 회복 + 이번 턴 파티 주는 피해↑
    card(H, 1, '요정 왕국에 경례', 1, '스킬', [heal(0.9), dmod(0.2), stk(K, 1)], [
      O('힘찬 경례', [heal(1.15), dmod(0.25), stk(K, 1)]),
      O('제식 동작', [heal(0.9), dmod(0.2), stk(K, 2)]),
      O('왕국 전체 경례', [heal(1.8), st('사기', 1), stk(K, 2)], { cost: 2 }),
      O('후열 경례', [sh(1.0), dmod(0.2), stk(K, 1)]),
      O('여왕 체육 수업', [heal(1.0), dmod(0.2), draw(1, { who: 'other', type: '공격' })]),
    ], [B('경비대 제복', 'heal'), B('진법', 'draw'), B('여왕 바라기', [stk(K, 1)])]),
    // u2 ③ 터뜨리기 — 장전을 다 쓰는 일제 사격
    card(H, 2, '일제 사격', 1, '공격', [dmg(0.55, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      O('탄도 계산', [dmg(0.7, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      O('견제 사격', [dmg(0.38, EA), per(K), dmg(0.15, EA), spendAll(K)], { cost: 0 }),
      O('저격', [dmg(1.0), per(K), dmg(0.35), st('취약', 1)]),
      O('매일 사격 훈련', [dmg(0.5, EA), per(K), dmg(0.15, EA), tw], { power: true }),
      O('제 미숙함부터', [dmg(0.55, EA), per(K), dmg(0.2, EA), ifKill, heal(0.6)]),
    ], [B('머스킷', 'power'), B('화약 냄새', 'ap'), B('탄약 보급', [stk(K, 1)])]),
    // u3 ① 개전 강화 시동 — 동료 카드를 끌어오고 매 턴 장전
    card(H, 3, '120도 삼각 편대', 1, '강화', [stk(K, 1), draw(1, { who: 'other' }), tw], [
      O('완벽한 편대', [stk(K, 2), draw(1, { who: 'other' }), tw], { tags: op }),
      O('편대 합류', [stk(K, 1), draw(1, { who: 'other' }), tw], { cost: 0, tags: [] }),
      O('병법', [sh(0.6), draw(2, { who: 'other', type: '공격' }), tw], { tags: op }),
      O('제식 훈련', [drawBasic(2), sh(0.6), tw], { tags: op }),
      O('셋이 둘러싸기', [st('협공', 1), tw], { tags: op }),
    ], [B('부하들의 신뢰', 'cost'), B('둘러싸기', 'draw'), B('대장님', [stk(K, 1)])], { tags: op }),
    // u4 ④ 1코 마무리 — 협공 + 장전을 세는 회복(쓰지 않음)
    card(H, 4, '경비대장 폴랑입니다', 1, '스킬', [st('협공', 1), heal(0.5), per(K), heal(0.15)], [
      O('경비대 총출동', [st('협공', 2), heal(0.5), per(K), heal(0.15)]),
      O('양익', [st('협공', 1), heal(1.1), power(reach(K, 3, [st('피해 감소', 1)]))], { power: true }),
      O('물러서지 않는 대장', [st('협공', 1), st('피해 감소', 2), ifWounded, heal(1.6)]),
      O('나도 소녀랍니다', [st('협공', 1), draw(2, { who: 'other', type: '공격' })]),
      O('경례 한 번', [st('협공', 2), per(K), heal(0.35), spendAll(K)]),
    ], [B('진군깃발', 'atkUp'), B('에르피엔 수호', 'defUp'), B('폴랑 버스터', [st('취약', 1, EA)])]),
  ]);
  starter(j, '폴랑_u3');
}


// 2차 측정 뒤 사도별 세기 맞춤 — 그 사도의 카드(고유 · 신탁 · 축복 · 생성 카드)의 피해 · 실드 · 회복 계수를 같은 배율로(신탁 값어치 비는 그대로)
function scale(j, m) {
  const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal'].includes(f.k) && f.ratio) f.ratio = Math.round(f.ratio * m * 100) / 100; if (f.k === 'power') for (const r of f.rules) mul(r.fx); } };
  for (const c of j.cards) { if (!c.unique && !c.token) continue; mul(c.fx); for (const o of c.oracles || []) mul(o.fx); for (const b of c.blesses || []) mul(b.fx); }
}
const S = (fn, m) => j => { fn(j); scale(j, m); };

run([
  ['요정/네르', S(ner, 1.1)], ['요정/네르_빡침', nerRage], ['요정/로니', S(roni, 1.25)], ['요정/리코타', S(ricota, 0.8)],
  ['요정/마리', marie], ['요정/마요', S(mayo, 1.45)], ['요정/마요_멋짐', S(mayoCool, 1.15)], ['요정/샤샤', shasha],
  ['요정/슈팡', S(shoupan, 0.92)], ['요정/스키아', S(skia, 1.08)], ['요정/에르핀_왕도', erpinRoyal], ['요정/에슈르', S(eshur, 1.0)],
  ['요정/에슈르_마도', eshurMagi], ['요정/죠안', joanne], ['요정/카렌', S(karen, 1.6)], ['요정/칸타', S(kanta, 1.75)],
  ['요정/캬롯', carrot], ['요정/큐이', S(kyui, 0.9)], ['요정/클로에', S(chloe, 0.87)], ['요정/파트라', S(patra, 1.35)], ['요정/폴랑', polan],
], new URL('./boost_요정.json', import.meta.url));
