// 18갈래 재설계 3단계 — 용족A 묶음 8명(2026-10-08). 기준: 시범 17명(05_시범16_결과.md) · 지침 BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/용족A.md
// 네티 · 다야 · 다야(퓨어샤인) · 루드 · 리츠 · 비비 · 시스트 · 실비아
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장 · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀). 다 차면 저절로 터짐 없음(onMax make/empower).
// 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시). 비비(신성)(마녀)은 다른 파일 — 건드리지 않는다.
// node _gen/rework/용족A_18.mjs [사도 이름 일부]  → heroes/용족/<파일>.json (원본은 백업 SRC 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, st, stk, spendAll, per, draw, make, ifStack, ifKill,
  ALLY, ALLIES, STRONG, disc, burn, gauge, srch, drawType, pull, xtra, pw, pas, token, Or, bl, U, setCards, setOpener,
  cs, perCs, perOver, dmod, spendN, form, formEnd } = L;
const LOW = 'lowEnemy';
const stkEv = (id, of) => ({ k: 'stack', id, v: 1, ofEvent: of });
const payPct = v => ({ k: 'payHpPct', v });
const perPaid = p => ({ k: 'perPaid', per: p });
const ifRepeat = { k: 'ifRepeat' };
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));

// ════════════════════════════════════════════════════════════════════
// 1. 네티 — 두 얼굴형 · 탱커 · 광기. 평소엔 아이들을 지키는 보모(파티 실드), 유물 냄새를 맡으면 「유물 집착」 — 드릴질마다 광물을 캐지만 파티는 뒷전.
//    자루가 다 차면 캐낸 광물(손에 카드 — 반짝이 회복 · 단단한 피해 감소 · 신비한 피해 증가)
// 원작: 평타 드릴 3연타마다 무작위 광물(회복 · 피해 감소 · 피해 증가) · 고학년 기가 드릴 차지 = 「집착」(해제 불가 · 거대 드릴 8연타 범위, A2 드릴질마다 집착 연장 · 광물 나눔)
//       · 저학년 낙석 조심(파티 보호막) · 인질 마리도 「나중에 구하면 된다」 · 알 속 오팔 · 피라를 찾은 보모 · 꼬리 자력으로 퍼즐
// 생성 카드 사도(묶음에서 새로 1명): 장치(발굴 자루 onMax)가 「캐낸 광물」 을 만든다 — 원작 「드릴질마다 광물을 캔다」 가 근거
// 시동: u1 낙석 조심(그대로 — 보모 얼굴로 돌아오기)
// ════════════════════════════════════════════════════════════════════
function netti(j) {
  const H = '네티', K = '발굴 자루', OBS = '네티_집착', ORE = '네티_ore';
  const h = j.heroes[0];
  h.blurb = '유물만 보면 눈이 도는 도굴꾼 용족. 평소엔 아이들을 지키는 보모지만, 유물 냄새를 맡으면 집착에 빠져 드릴만 돌립니다. 자루가 차면 캐낸 광물을 나눠 줍니다.';
  h.keyword = { name: K, desc: '드릴로 캐낸 광물 꾸러미', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.1 }], onMax: { make: ORE, consume: true } };
  h.forms = [{
    id: OBS, name: '유물 집착', desc: '파티는 뒷전이고 드릴만 도는 얼굴',
    turns: 2, mods: { guard: -0.1, dealt: 0.15 },
    bonus: [{ type: '공격', fx: [stk(K, 1)] }],
    passives: [{ name: '거대 드릴', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [dmg(0.25, EA)] }],
    replace: false, skin: null, anim: null,
  }];
  h.passives = [
    pas('드릴 굴착', 'play', [stk(K, 1)], { limit: 2 }),
    pas('너로 정했다! 초정밀 탐사!', 'shieldBreak', [st('피해 감소', 1)], { limit: 1 }),
  ];
  h.ult.fx = [dmg(0.24, EA, { hits: 8 }), stk(K, 3), form(OBS)];
  const ore = token(ORE, '캐낸 광물', H, '스킬', [heal(0.3), st('피해 감소', 1), dmod(0.15, ALLIES)],
    { cost: 0, tags: ['소멸'], blurb: '반짝이는 것, 단단한 것, 신비한 것 — 네티가 자루째 나눠 주는 광물' });
  setCards(j, [
    // 시동 — 낙석 조심: 파티 실드 + 자루 2. 집착이면 정신을 차린다(보모 얼굴로)
    U(H, 1, '낙석 조심', 1, '스킬', [sh(1.3), stk(K, 2), formEnd], nm([
      Or([sh(1.55), stk(K, 2), formEnd]),
      Or([sh(0.8), stk(K, 1), formEnd], { cost: 0 }),
      Or([sh(1.2), stk(K, 3)]),
      Or([sh(1.15), stk(K, 2), pw('shieldBreak', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([sh(1.8), stk(K, 3), disc(1)]),
    ]), null),
    // 갈래 부품 — 드릴 굴착(분쇄): 3연타 + 자루 + 유물 집착으로(집착 중이면 시간이 새로 — 드릴질마다 연장)
    U(H, 2, '드릴 굴착', 1, '공격', [dmg(0.45, E1, { hits: 3 }), stk(K, 1), form(OBS)], [
      'A', 'B',
      Or([dmg(0.3, EA, { hits: 3 }), stk(K, 1), form(OBS)]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.5, E1, { hits: 3 }), form(OBS), srch()]),
    ], bl('power', 'weakSpot', [stk(K, 1)]), ['분쇄']),
    // 쓰기 — 보모 언니의 나눔: 실드 + 자루 1개당 실드, 자루 전부 소모(광물을 나눌까 · 다섯을 채워 광물을 캘까)
    U(H, 3, '보모 언니의 나눔', 1, '스킬', [sh(0.8), per(K), sh(0.35), spendAll(K)], [
      'A',
      ['D', 'turnStart', [stk(K, 1)]],
      Or([heal(0.85), per(K), sh(0.35), spendAll(K)]),
      Or([sh(0.75), per(K), sh(0.33), srch()]),
      'Hx',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 원작 자유 — 자성 꼬리: 결정화 + 방어 기반 + 자루 1개당
    U(H, 4, '자성 꼬리', 1, '공격', [st('결정화', 1), ddef(0.6), per(K), ddef(0.2)], [
      'A', 'B',
      Or([st('결정화', 1), ddef(0.5, EA), per(K), ddef(0.15, EA)]),
      ['D', 'turnStart', [st('결정화', 1)]],
      Or([ddef(0.6), per(K), ddef(0.2), pull()]),
    ], bl('power', 'cost', [stk(K, 1)])),
    // 둘째 — 꼬리 열쇠: 실드 + 자루 + 고유 카드 1장(유적 퍼즐)
    U(H, 5, '꼬리 열쇠', 1, '스킬', [sh(1.0), stk(K, 1), srch()], [
      'A', 'B',
      Or([sh(0.9), stk(K, 2), pull()]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([sh(1.4), stk(K, 2), disc(1)]),
    ], bl('guard', 'ap', [stk(K, 1)])),
  ], [ore]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 다야 — 성장형 · 딜러 · 순수. 「갈고, 닦고」 — 손에 쥔 다야 공격 카드에 광택을 올린다(판 내내). 광택이 오른 카드일수록 다이아가 크게 솟는다.
//    쓰러뜨린 적마다 원석이 솟고(원래 만들던 장치), 원석 둘이 모이면 진품
// 원작: 「가공 안 된 다이아는 석탄일 뿐」 · 노력파 · 완벽해지는 소원 · 다이아 솟구치기(여러 적 + 쓰라림) · 처치하면 다시 쏨 · 망치로 두들기기 전엔 유리와 못 가림
// 같은 갈래 구별: 자라는 카드를 낸다고 자라지 않는다 — 다른 카드(손질)로 손의 카드를 닦는다
// 시동: u2 다이아 박기 → u3 갈고, 닦고(만들기 칸)
// ════════════════════════════════════════════════════════════════════
function daya(j) {
  const H = '다야', K = '다이아 쓰라림', G = '광택', GEM = '다야_gem';
  const polish = (v = 1) => cs(G, v, { to: 'hand', n: 1, who: 'self', type: '공격', max: 5 });
  const h = j.heroes[0];
  h.blurb = '완벽해지기를 꿈꾸는 노력파 다이아몬드 용족. 손에 쥔 카드를 갈고 닦아 광택을 올리면 그 카드가 판 내내 반짝이고, 쓰러뜨린 적마다 원석이 다시 솟습니다.';
  h.passives = [
    pas('연쇄 피어스', 'kill', [make(GEM, 1)], { when: { mine: true }, limit: 2 }),
    pas('연쇄 피어스', 'break', [stk(K, 2, E1)], { when: { mine: true } }),
    pas('완벽한 커팅', 'grow', [stk(K, 1, E1)], { limit: 1 }),
  ];
  setOpener(j, H, 'u2', 'u3');
  setCards(j, [
    // 쓰기 — 다이아 피어스(2): 적 전체 + 이 카드 「광택」 1당 적 전체, 처치하면 원석
    U(H, 1, '다이아 피어스', 2, '공격', [dmg(1.15, EA), perCs(G), dmg(0.22, EA), ifKill, make(GEM, 1)], [
      'A', 'B',
      Or([dmg(1.65), perCs(G), dmg(0.3), stk(K, 2, E1)]),
      ['D', 'grow', [dmg(0.3, EA)], { limit: 1 }],
      Or([dmg(1.1, EA), perCs(G), dmg(0.2, EA), srch()]),
    ], bl('power', 'ap', [make(GEM, 1)])),
    // 둘째 — 다이아 박기: 단일 + 쓰라림 2
    U(H, 2, '다이아 박기', 1, '공격', [dmg(0.95), stk(K, 2, E1)], [
      'A', 'B',
      Or([dmg(0.85), stk(K, 2, E1), polish()]),
      ['D', 'turnStart', [stk(K, 1, E1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 시동 — 갈고, 닦고: 손의 다야 공격 카드 1장에 광택 +1(판 내내, 최대 5) + 드로우
    U(H, 3, '갈고, 닦고', 1, '스킬', [polish(), draw(1), make(GEM, 1)], nm([
      Or([polish(), draw(2), make(GEM, 1)]),
      Or([polish(), make(GEM, 1)], { cost: 0 }),
      Or([polish(), make(GEM, 2)]),
      Or([polish(), make(GEM, 2), pw('grow', [draw(1)], { limit: 1 })], { power: true }),
      Or([polish(2), draw(2), make(GEM, 1)]),
    ]), null),
    // 갈래 부품 — 완벽해지는 소원: 단일 + 이 카드 광택 1당, 치면서 손의 다른 공격 카드를 닦는다
    U(H, 4, '완벽해지는 소원', 1, '공격', [dmg(0.8), per(K), dmg(0.35), polish()], [
      'A', 'B',
      Or([dmg(0.65, EA), per(K, { each: true }), dmg(0.25, EA), polish()]),
      ['D', 'grow', [stk(K, 1, E1)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 원작 자유 — 망치 감별(0): 쓰라림 + 공격 카드 1장, 반쯤은 진짜 원석
    U(H, 5, '망치 감별', 0, '스킬', [stk(K, 1, E1), drawType('공격'), { k: 'ifRandom', pct: 0.5 }, make(GEM, 1)], [
      'A',
      Or([stk(K, 2, E1), drawType('공격', 2)]),
      Or([stk(K, 2, E1), drawType('공격'), make(GEM, 1)]),
      Or([drawType('공격'), make(GEM, 1), pw('make', [stk(K, 1, E1)], { limit: 1 })], { power: true }),
      Or([stk(K, 1, E1), make(GEM, 1), drawType('공격')]),
    ], bl('draw', { tags: ['보존'] }, [make(GEM, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 다야(퓨어샤인) — 회복형(변환) · 서포터 · 광기. 발밑의 퓨어☆영역이 턴 끝마다 파티를 아물리고, 넘친 빛(회복 뒤 HP 80%를 넘긴 몫)은 「반짝임」 으로 모인다 —
//    넷이면 파티의 다음 한 수가 빛난다. 넘친 빛은 「1초 컷」 빔이 되기도 한다
// 원작: 저학년 매지컬☆샤인(아군 보호막 + 회복 영역) · 4타째마다 포즈로 아군 회복 · 어사이드 스타☆샤인(회복 + 피해 감소) · 고학년 샤이닝 빔 · 변신이 창피해 빨리 끝내고 싶음
// 다 차면: 반짝임 넷 → 파티의 다음 카드 +40%(onMax empower). 넷째 카드 포즈는 영역만(옛 회복 덤 뺌 — 봇이 넷째 카드 덤으로 45% 까지 올랐던 것)
// 시동: u4 매지컬☆샤인 영역(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function dayaPure(j) {
  const H = '다야_퓨어샤인', K = '퓨어☆영역', K2 = '반짝임';
  const h = j.heroes[0];
  h.blurb = '마법 소녀로 변신한 다야. 변신이 부끄러워 누구보다 빨리 끝내고 싶습니다. 발밑의 영역이 파티를 아물리고, 넘친 빛은 반짝임으로 모여 파티의 다음 한 수를 빛냅니다.';
  h.keywords = [{ name: K2, desc: '넘친 빛이 모인 반짝이', carrier: 'self', cap: 4, onMax: { empower: 'any', ratio: 0.4, consume: true } }];
  h.passives = [
    pas('퓨어☆포즈', 'play', [stk(K, 1), heal(0.15)], { when: { who: 'any', nth: 4 }, limit: 1 }),
    pas('마법 소녀는 편지 쓰는 중', 'overheal', [stkEv(K2, 0.04)], { when: { pct: 0.8 }, limit: 2 }),
    pas('마법 소녀는 편지 쓰는 중', 'lowHp', [heal(1.5), st('피해 감소', 1)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 원작 자유 — 매지컬☆샤인: 실드 + 회복 + 영역 2
    U(H, 1, '매지컬☆샤인', 1, '스킬', [sh(1.2), heal(0.5), stk(K, 2)], [
      'A', 'B',
      Or([sh(1.0), stk(K, 2), st('사기', 1)]),
      Or([sh(1.35), heal(0.55), pw('turnStart', [stk(K, 1)])], { power: true }),
      'Hn',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 둘째 — 매지컬☆에너지(0): 단일 + 영역
    U(H, 2, '매지컬☆에너지', 0, '공격', [dmg(0.95), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.5, EA), stk(K, 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.9), stk(K, 1), srch()]),
    ], bl('power', 'cost', [stk(K, 1)])),
    // 쓰기 — 스타☆샤인: 영역 1개당 회복, 영역 전부 소모 + 피해 감소(넘치면 반짝임)
    U(H, 3, '스타☆샤인', 1, '스킬', [per(K), heal(0.55), spendAll(K), st('피해 감소', 1)], [
      'A',
      ['D', 'turnStart', [heal(0.35)]],
      Or([per(K), sh(0.6), spendAll(K), heal(0.4)]),
      Or([per(K), heal(0.5), spendAll(K), srch()]),
      'Hn',
    ], bl('heal', 'ap', [stk(K2, 1)])),
    // 시동 — 매지컬☆샤인 영역(개전 강화): 결의 + 매 턴 영역
    U(H, 4, '매지컬☆샤인 영역', 1, '강화', [st('결의', 1), pw('turnStart', [stk(K, 1)])], nm([
      Or([st('결의', 1), stk(K, 1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([st('결의', 1), stk(K, 2), pw('overheal', [stk(K2, 1)], { when: { pct: 0.8 }, limit: 1 })], { tags: ['개전'] }),
      Or([st('결의', 1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([st('결의', 2), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 갈래 부품 — 1초 컷: 단일 + 이번 판 HP 80%를 넘긴 회복 25당 1타(넘친 빛 → 빔) + 반짝임
    U(H, 5, '1초 컷', 1, '공격', [dmg(0.75), perOver(0.8, 25), dmg(0.18), stk(K2, 1)], [
      'A', 'B',
      Or([dmg(0.55, EA), perOver(0.8, 25), dmg(0.12, EA)]),
      ['D', 'overheal', [dmg(0.35)], { when: { pct: 0.8 }, limit: 1 }],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K2, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 루드 — 박자형 · 탱커 · 활발. 똑같은 동작을 되풀이해 한 세트를 채운다 — 바로 앞과 같은 카드(같은 id)를 잇달아 내면 「렙」 +1, 넷이면 한 세트(다음 카드 +50%).
//    렙이 쌓일수록 포즈로 버틴다(1개당 받는 피해 -4%). 황금 덤벨은 내도 손으로 돌아온다(회수)
// 원작: 강화 평타 「네 번째 공격마다 포즈 → 피해 감소, 끝나면 회복」 · 저학년 한 세트 더!(고함 범위 + 소음) · 고학년 임팩트 프레스 · 제이드에게 「마지막 한 번 더」 무한 반복
// 다 차면: 옛 「포즈」(저절로 피해 감소 + 회복) → 다음 카드 강화. 티그(제 공격이면 아무 카드) · 리코타(비싼 순)와 가름: 루드는 「똑같은 카드」
// 시동: u2 미숫가루 프로틴(그대로)
// ════════════════════════════════════════════════════════════════════
function rude(j) {
  const H = '루드', K = '렙';
  const h = j.heroes[0];
  h.blurb = '무엇이든 운동으로 바꾸는 용족 서열 2위 헬창. 똑같은 동작을 잇달아 되풀이할 때마다 렙이 쌓이고, 네 번째 렙에 한 세트를 끝내며 다음 한 수에 힘을 싣습니다.';
  h.keyword = { name: K, desc: '쉬지 않고 세는 반복 횟수', carrier: 'self', cap: 4, per: [{ stat: 'taken', v: -0.04 }], onMax: { empower: 'next', ratio: 0.4, consume: true } };
  h.passives = [
    pas('한 세트 더', 'play', [stk(K, 1)], { when: { repeat: true }, limit: 2 }),
    pas('단백질 보충', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }),
    pas('단백질 보충', 'fightStart', [stk(K, 1)]),
  ];
  setCards(j, [
    // 원작 자유 — 한 세트 더!: 적 전체 방어 기반 + 약화(소음) + 렙. 되풀이면 렙 하나 더
    U(H, 1, '한 세트 더!', 1, '공격', [ddef(0.5, EA), st('약화', 1, EA), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.65, EA), heal(1.25), stk(K, 1)]),   // 배율 0.65 로 값어치 보정이 1.4 를 넘어 회복 1.0 → 1.25(2026-10-09)
      ['D', 'play', [stk(K, 1)], { when: { repeat: true }, limit: 1 }],
      Or([ddef(0.45, EA), st('약화', 1, EA), srch()]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 미숫가루 프로틴(0): 렙 + 드로우
    U(H, 2, '미숫가루 프로틴', 0, '스킬', [stk(K, 1), draw(1)], nm([
      Or([stk(K, 2), draw(1)]),
      Or([stk(K, 1), draw(1)], { tags: ['신속'] }),
      Or([stk(K, 1), draw(1), sh(0.6)]),
      Or([stk(K, 1), draw(1), pw('play', [sh(0.25)], { when: { repeat: true }, limit: 1 })], { power: true }),
      Or([stk(K, 2), draw(2), disc(1)]),
    ]), null),
    // 둘째 — 다야 님을 지켜라: 큰 실드 + 렙 1개당 실드(쓰지 않음)
    U(H, 3, '다야 님을 지켜라', 1, '스킬', [sh(1.2), per(K), sh(0.3)], [
      'A', 'B',
      Or([sh(1.1), per(K), sh(0.28), st('피해 감소', 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hd',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 쓰기 — 실피르보다 세게: 방어 기반 + 렙 1개당, 렙 전부 소모(털까 · 넷을 채워 세트를 끝낼까)
    U(H, 4, '실피르보다 세게', 1, '공격', [ddef(0.65), per(K), ddef(0.3), spendAll(K)], [
      'A',
      ['D', 'play', [ddef(0.2)], { when: { repeat: true }, limit: 1 }],
      Or([ddef(0.55, EA), per(K), ddef(0.22, EA), spendAll(K)]),
      Or([ddef(0.6), per(K), ddef(0.28), srch()]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 황금 덤벨(0 · 회수 3): 방어 기반 + 렙. 내도 손으로 돌아온다 — 바로 다시 내면 되풀이
    U(H, 5, '황금 덤벨', 0, '공격', [ddef(0.35), stk(K, 1)], [
      'A',
      Or([ddef(0.35), stk(K, 2)], { tags: ['회수 3'] }),
      Or([ddef(0.2, EA), stk(K, 1)], { tags: ['회수 3'] }),
      ['D', 'play', [sh(0.25)], { when: { repeat: true }, limit: 2 }],
      Or([ddef(0.3), stk(K, 1), srch()], { tags: ['회수 3'] }),
    ], bl('guard', 'ap', [stk(K, 1)]), ['회수 3']),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 리츠 — 두 얼굴형 · 딜러 · 광기. 소심하게 참는 얼굴(파티 받는 피해 감소)과, 먼저 맞아 「정당방위」 가 서면 버튼이 눌리는 얼굴(반말 · 박살)을 오간다 —
//    은둔 초고수로 넘어가고, 박살내주겠어!로 다 털면 평소로 돌아온다(간신히 이겼다)
// 원작: PV 「두 얼굴의 전사」 · 「버튼 눌리면 못 막음」 · 먼저 친 쪽이 명분을 잃음 · 저학년 담금질(집중 — 받는 피해↓, 아군이 맞을수록 쌓임) · 고학년 벼리기 · 어사이드 「한 놈만 팬다」
//       · 01 형태 종합: 모드가 실제로 뒤집히는 대표
// 시동: u2 담금질(그대로)
// ════════════════════════════════════════════════════════════════════
function ritz(j) {
  const H = '리츠', K = '정당방위', F = '리츠_광전';
  const h = j.heroes[0];
  h.blurb = '소심하고 공손한 은둔 고수 용족. 먼저 맞기 전까진 몸을 사리며 파티를 감싸지만, 정당방위가 서면 버튼이 눌려 반말로 박살을 내고 — 다 털고 나면 간신히 이겼다며 돌아옵니다.';
  h.keyword = { name: K, desc: '먼저 맞고 나서야 서는 명분', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.08 }] };
  h.forms = [{
    id: F, name: '버튼 눌린 리츠', desc: '반말로 박살 내는 강철의 얼굴',
    turns: 2, mods: { dealt: 0.15, taken: 0.1 },
    bonus: [{ card: `${H}_u1`, fx: [formEnd] }],
    passives: [{ name: '한 놈만 노리기', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [xtra(0.35)] }],
    replace: false, skin: null, anim: null,
  }];
  h.passives = [
    pas('먼저 손대셨죠?', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
    { name: '참는 자세', when: { on: 'always' }, conds: [{ c: 'stack', id: K, not: true }], fx: [{ k: 'takenMod', v: -0.15, target: 'party' }] },
  ];
  setCards(j, [
    // 쓰기 — 박살내주겠어!: 단일 + 정당방위 1개당, 전부 소모 — 버튼 눌린 얼굴이면 평소로 돌아온다
    U(H, 1, '박살내주겠어!', 1, '공격', [dmg(0.95), per(K), dmg(0.32), spendAll(K)], [
      'A',
      ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      Or([dmg(0.55, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      Or([dmg(0.9), per(K), dmg(0.3), st('취약', 1)]),
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 담금질: 실드 + 피해 감소 + 정당방위
    U(H, 2, '담금질', 1, '스킬', [sh(1.25), st('피해 감소', 1), stk(K, 1)], nm([
      Or([sh(1.5), st('피해 감소', 1), stk(K, 1)]),
      Or([sh(0.7), stk(K, 1)], { cost: 0 }),
      Or([sh(1.1), st('피해 감소', 1), stk(K, 2)]),
      Or([sh(1.1), stk(K, 1), pw('hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 })], { power: true }),
      Or([sh(1.2), st('피해 감소', 1), srch()]),
    ]), null),
    // 원작 자유 — 대검 밀어내기(벼리기 반동): 적 전체 + 즉시 행동 늦춤 + 정당방위
    U(H, 3, '대검 밀어내기', 1, '공격', [dmg(0.8, EA), { k: 'rushDown', v: 1, target: EA }, stk(K, 1)], [
      'A', 'B',
      Or([dmg(1.3), { k: 'rushDown', v: 1 }, stk(K, 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hn',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 은둔 초고수: 단일 + 정당방위 1개당. 정당방위가 둘 이상이면 버튼이 눌린다(얼굴 넘기기)
    U(H, 4, '은둔 초고수', 1, '공격', [dmg(0.8), per(K), dmg(0.22), ifStack(K, 2), form(F)], [
      'A', 'B',
      Or([dmg(1.2), st('약화', 1), ifStack(K, 1), form(F)]),
      Or([dmg(0.95), ifStack(K, 2), form(F), pw('hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 })], { power: true }),
      Or([dmg(1.05), per(K), dmg(0.3), srch()]),
    ], bl('power', 'draw', [stk(K, 1)])),
    // 만들기 — 통곡의 벽: 실드 + 정당방위 + 고유 카드 1장(소심하게 버틴다)
    U(H, 5, '통곡의 벽', 1, '스킬', [sh(1.1), stk(K, 1), srch()], [
      'A', 'B',
      Or([sh(1.0), stk(K, 2), st('피해 감소', 1)]),
      ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      'Hd',
    ], bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 비비 — 대가형 · 탱커 · 순수 · 엘다인. 제 몸(파티 HP)을 수은에 내주는 사랑 — HP 를 치를 때마다 「은빛 맹세」, 넷이면 파티의 다음 한 수를 감싼다.
//    재채기 한 번에 적이 수은에 중독되고, 수은 장막이 깨지면 깬 적이 중독된다
// 원작: 세계수에 복수하려 수은을 받아들였다가 본인도 만성 몸살 · 재채기 · 양딸 실비아에게 제 힘을 전부 넘김 · 저학년 수은 보호막(깨지면 범위 + 방어↓) · 고학년 퀵실버 랜스(도발)
// 다 차면: 옛 「녹아내린 갑옷」(수은 넷이면 저절로 취약) → 뺌. 은빛 맹세 넷 → 파티의 다음 카드 +40%
// 엘다인 한 단계: 전투 시작 은빛 맹세 +1(수치 틀 그대로)
// 시동: u1 소녀에게 오시려구요?(그대로)
// ════════════════════════════════════════════════════════════════════
function bibi(j) {
  const H = '비비', K = '수은 중독', K2 = '은빛 맹세';
  const h = j.heroes[0];
  h.blurb = '은의 용족 행세를 하는 수은의 용족. 제 몸을 수은에 내주며 버티는 만성 몸살 환자라, HP 를 치를 때마다 은빛 맹세가 쌓여 넷이면 파티의 다음 한 수를 감쌉니다. 재채기 한 번에 적이 중독됩니다.';
  h.keyword = { name: K, desc: '스며드는 은빛 독', carrier: 'enemy', cap: 4, per: [{ stat: 'dot', ratio: 0.25 }] };
  h.keywords = [{ name: K2, desc: '제 몸을 내준 사랑의 맹세', carrier: 'self', cap: 4, per: [{ stat: 'guard', v: 0.05 }], onMax: { empower: 'any', ratio: 0.4, consume: true } }];
  h.passives = [
    pas('수은 몸살', 'turnStart', [stk(K, 1, E1)]),
    pas('수은 몸살', 'shieldBreak', [stk(K, 2, E1)], { limit: 2 }),
    pas('소녀가 보호하겠사와요', 'pay', [stk(K2, 1)], { limit: 2 }),
    pas('소녀가 보호하겠사와요', 'fightStart', [stk(K2, 1)]),
    pas('소녀가 보호하겠사와요', 'lowHp', [sh(3.0), st('피해 감소', 2)], { when: { pct: 0.4 } }),
  ];
  setCards(j, [
    // 시동 · 갈래 부품 — 소녀에게 오시려구요?: 실드 + 이번 턴 치른 HP 40당 실드 + 수은
    U(H, 1, '소녀에게 오시려구요?', 1, '스킬', [sh(1.3), perPaid(40), sh(0.15), stk(K, 1, E1)], nm([
      Or([sh(1.5), perPaid(40), sh(0.15), stk(K, 1, E1)]),
      Or([sh(0.8), stk(K, 1, E1)], { cost: 0 }),
      Or([payPct(0.03), sh(1.6), stk(K2, 1)]),
      Or([sh(1.2), stk(K, 1, E1), pw('pay', [sh(0.3)], { limit: 1 })], { power: true }),
      Or([sh(1.4), stk(K, 2, E1), srch()]),
    ]), null),
    // 만들기 — 독성 재채기: HP 3% 를 치르고 적 전체 방어 기반 + 수은
    U(H, 2, '독성 재채기', 1, '공격', [payPct(0.03), ddef(0.45, EA), stk(K, 1, EA)], [
      'A', 'B',
      Or([payPct(0.03), ddef(0.75), stk(K, 2, E1)]),
      Or([payPct(0.03), ddef(0.5, EA), pw('pay', [stk(K, 1, EA)], { limit: 1 })], { power: true }),
      Or([payPct(0.03), ddef(0.4, EA), srch()]),
    ], bl('power', 'cost', [stk(K, 1, EA)])),
    // 쓰기 — 은빛 창 네 자루(분쇄): 방어 기반 + 그 적의 수은 1개당, 수은 전부 소모
    U(H, 3, '은빛 창 네 자루', 1, '공격', [ddef(0.5), per(K), ddef(0.2), spendAll(K)], [
      'A',
      ['D', 'pay', [ddef(0.25)], { limit: 1 }],
      Or([ddef(0.45), per(K), ddef(0.18), st('약화', 1)]),
      Or([ddef(0.45), per(K), ddef(0.18), srch()]),
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)]), ['분쇄']),
    // 원작 자유 — 세계수 뿌리에 수은을: 결의 + 적 전체 방어 기반 + 적마다 수은 1개당
    U(H, 4, '세계수 뿌리에 수은을', 1, '공격', [st('결의', 1), ddef(0.3, EA), per(K, { each: true }), ddef(0.15, EA)], [
      'A', 'B',
      Or([payPct(0.03), ddef(0.62, EA), per(K, { each: true }), ddef(0.25, EA)]),
      Or([st('결의', 1), ddef(0.5, EA), pw('turnStart', [stk(K, 1, EA)])], { power: true }),
      Or([ddef(0.4, EA), per(K, { each: true }), ddef(0.2, EA), srch()]),
    ], bl('power', 'ap', [stk(K2, 1)])),
    // 둘째 — 수은 주스 대접: 실드 + 수은 + 동료 카드 1장
    U(H, 5, '수은 주스 대접', 1, '스킬', [sh(1.0), stk(K, 1, E1), draw(1, { who: 'other' })], [
      'A', 'B',
      Or([sh(1.1), stk(K2, 2), draw(1, { who: 'other' })]),
      ['D', 'turnStart', [stk(K, 1, E1)]],
      Or([sh(1.5), stk(K, 2, E1), disc(1)]),
    ], bl('guard', 'draw', [stk(K2, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 시스트 — 돈형 · 딜러 · 광기. 처치 · 격파 · 손패 떨이로 「수수료」 를 벌고(1개당 주는 피해 +6% — 쥐면 세진다),
//    수수료를 치르고 4차원 바구니에서 특급 배송을 꺼낸다(사기 — 손에 카드). 판 골드는 건드리지 않는다(BRIEF 돈형 규칙)
// 원작: 자수정 용족 밀수꾼 · 일반 상점 NPC · 「싼 걸 비싸게」 · 저학년 총알 배송(HP 비율 낮은 적 · 처치하면 이어 쏨) · 고학년 플렉스 건(자기 피해↑) · 바보세 · 「뭐라도 맡기라」
// 시동: u4 장사천재 시스트(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function syst(j) {
  const H = '시스트', K = '수수료', EXP = '시스트_express';
  const h = j.heroes[0];
  h.blurb = '힘 대신 돈으로 가치를 증명하는 자수정의 용족 밀수꾼. 쓰러뜨리고 깨뜨리고 손패를 떨이로 넘겨 수수료를 벌고, 그 수수료로 4차원 바구니에서 특급 배송을 꺼냅니다.';
  h.keyword = { name: K, desc: '거래마다 슬쩍 얹는 웃돈', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.1 }] };
  h.passives = [
    pas('장사 수완', 'kill', [stk(K, 2)], { when: { mine: true } }),
    pas('장사 수완', 'break', [stk(K, 2)], { when: { mine: true } }),
    pas('개업 자금', 'fightStart', [stk(K, 2)]),
  ];
  setCards(j, [
    // 원작 자유 — 총알 배송: HP 가장 낮은 적 저격 + 수수료, 처치하면 특급 배송
    U(H, 1, '총알 배송', 1, '공격', [dmg(1.4, LOW), stk(K, 1), ifKill, make(EXP, 1)], [
      'A', 'B',
      Or([dmg(1.5, LOW), ifKill, make(EXP, 1), stk(K, 2)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 만들기(벌이) — 떨이 판매: 손패 1장을 팔아(소멸) 수수료 3 + 드로우 2
    U(H, 2, '떨이 판매', 1, '스킬', [burn(1), stk(K, 3), draw(2)], [
      'A',
      Or([burn(1), stk(K, 4), draw(2)], { tags: ['신속'] }),
      Or([stk(K, 3), draw(2), make(EXP, 1)]),
      Or([stk(K, 3), draw(2), pw('exhaust', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([stk(K, 3), draw(3)]),
    ], bl('draw', 'ap', [stk(K, 1)])),
    // 쓰기(사기) — 4차원 바구니(2): 적 전체 2회. 수수료 3 이상이면 3을 치르고 특급 배송 둘
    U(H, 3, '4차원 바구니', 2, '공격', [dmg(1.0, EA, { hits: 2 }), ifStack(K, 3), spendN(K, 3), make(EXP, 2)], [
      'A', 'B',
      Or([dmg(1.0, EA, { hits: 2 }), spendN(K, 2), make(EXP, 2)]),
      Or([dmg(1.0, EA, { hits: 2 }), make(EXP, 2), pw('make', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([dmg(1.4, EA, { hits: 2 }), ifKill, make(EXP, 2)]),
    ], bl('power', 'cost', [make(EXP, 1)])),
    // 시동 — 장사천재 시스트(개전 강화): 공격력 + 수수료 2 + 처치하면 특급 배송(턴 2)
    U(H, 4, '장사천재 시스트', 1, '강화', [{ k: 'atkMod', v: 0.1, run: true, target: 'self' }, stk(K, 2), pw('kill', [make(EXP, 1)], { limit: 2 })], nm([
      Or([{ k: 'atkMod', v: 0.1, run: true, target: 'self' }, stk(K, 4), pw('kill', [make(EXP, 1)], { limit: 2 })], { tags: ['개전'] }),
      Or([stk(K, 2), pw('kill', [make(EXP, 1)], { limit: 2 })], { cost: 0, tags: ['개전'] }),
      Or([{ k: 'atkMod', v: 0.1, run: true, target: 'self' }, stk(K, 2), L.power(L.rule('kill', [make(EXP, 1)], { limit: 2 }), L.rule('make', [stk(K, 1)], { limit: 1 }))], { tags: ['개전'] }),
      Or([{ k: 'atkMod', v: 0.1, run: true, target: 'self' }, stk(K, 2), L.power(L.rule('kill', [make(EXP, 1)], { limit: 2 }), L.rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      Or([{ k: 'atkMod', v: 0.2, run: true, target: 'self' }, stk(K, 4), pw('kill', [make(EXP, 1)], { limit: 2 })], { tags: [] }),
    ]), null, ['개전']),
    // 갈래 부품 — 바보세 징수(0): 수수료 + 손의 시스트 공격 카드 1장 비용 -1
    U(H, 5, '바보세 징수', 0, '스킬', [stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })], [
      'A', 'B',
      Or([stk(K, 2), drawType('공격')]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([stk(K, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), srch({ type: '공격' })]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 실비아 — 거드는 형 · 탱커 · 광기 · 엘다인. 아군을 티파티 「초청객」 으로 들이면 그 손님이 스킬을 낼 때마다 은방울이 울려 무작위 적을 치고 「은방울」 이 쌓인다 —
//    셋이면 파티의 다음 한 수(티파티의 절정), 궁극의 유희로 다 울리면 파티 실드
// 원작: 저학년 궁극의 유희(모든 아군 초청객 — 스킬이 맞으면 추가 피해) · 고학년 은분수 충격파 + 은방울 실드 · 강화 평타(가장 센 적 주는 피해↓) · 장갑을 던져 결투 신청
// 엘다인 한 단계: 전투 시작 가장 센 아군에게 초청객(수치 틀 그대로)
// 시동: u1 초대장 건네기(그대로)
// ════════════════════════════════════════════════════════════════════
function sylvia(j) {
  const H = '실비아', K = '초청객', K2 = '은방울';
  const h = j.heroes[0];
  h.blurb = '우아한 숙녀를 자처하는 은의 용족. 아군을 티파티 손님으로 들이면 손님이 스킬을 낼 때마다 은방울이 울려 적을 치고, 은방울이 셋 모이면 파티의 다음 한 수가 빛납니다.';
  h.keywords = [{ name: K2, desc: '티파티에 울리는 은방울', carrier: 'self', cap: 3, per: [{ stat: 'guard', v: 0.05 }], onMax: { empower: 'any', ratio: 0.4, consume: true } }];
  h.passives = [
    pas('그 어머니에 그 딸', 'play', [ddef(0.3, ER), stk(K2, 1)], { when: { marked: K, type: '스킬' }, limit: 2 }),
    pas('그 어머니에 그 딸', 'lowHp', [{ k: 'cleanse', v: 1 }, sh(2.5)], { when: { pct: 0.4 } }),
    pas('티파티 초대', 'fightStart', [stk(K, 1, STRONG)]),
  ];
  setCards(j, [
    // 시동 · 만들기 — 초대장 건네기: 아군 1명 초청 + 실드
    U(H, 1, '초대장 건네기', 1, '스킬', [stk(K, 1, ALLY), sh(1.4)], nm([
      Or([stk(K, 1, ALLY), sh(1.65)]),
      Or([stk(K, 1, ALLY), sh(0.7)], { cost: 0 }),
      Or([stk(K, 1, ALLY), sh(1.2), stk(K2, 1)]),
      Or([stk(K, 1, ALLY), sh(1.2), pw('play', [sh(0.2)], { when: { marked: K, type: '스킬' }, limit: 1 })], { power: true }),
      Or([stk(K, 1, ALLY), sh(1.2), srch()]),
    ]), null),
    // 쓰기 — 궁극의 유희(2): 아군 전원 초청 + 은방울 1개당 실드(최소 1), 은방울 전부 소모
    U(H, 2, '궁극의 유희', 2, '스킬', [stk(K, 1, ALLIES), per(K2, { n: 1 }), sh(0.7), spendAll(K2)], [
      'A', 'B',
      Or([stk(K, 1, ALLIES), per(K2, { n: 1 }), ddef(0.3, EA), spendAll(K2)]),
      ['D', 'play', [stk(K2, 1)], { when: { marked: K, type: '스킬' }, limit: 1 }],
      Or([stk(K, 1, ALLIES), per(K2, { n: 1 }), sh(0.65), st('취약', 1, EA)]),
    ], bl('guard', 'ap', [stk(K2, 1)])),
    // 원작 자유 — 부채로 꾸짖기: 방어 기반 + 약화 2(가장 센 적의 기를 꺾는다)
    U(H, 3, '부채로 꾸짖기', 1, '공격', [ddef(0.6), st('약화', 2)], [
      'A', 'B',
      Or([ddef(0.5, EA), st('약화', 1, EA)]),
      ['D', 'turnStart', [st('약화', 1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K2, 1)])),
    // 갈래 부품 — 티파티 준비(강화): 결의 + 초청객이 스킬을 내면 실드 · 은방울(턴 2)
    U(H, 4, '티파티 준비', 1, '강화', [st('결의', 1), pw('play', [sh(0.35), stk(K2, 1)], { when: { marked: K, type: '스킬' }, limit: 2 })], nm([
      Or([st('결의', 1), stk(K2, 1), pw('play', [sh(0.4), stk(K2, 1)], { when: { marked: K, type: '스킬' }, limit: 2 })]),
      Or([st('결의', 1), pw('play', [sh(0.3), stk(K2, 1)], { when: { marked: K, type: '스킬' }, limit: 2 })], { cost: 0 }),
      Or([st('결의', 1), stk(K, 1, ALLY), pw('play', [sh(0.3), stk(K2, 1)], { when: { marked: K, type: '스킬' }, limit: 2 })]),
      Or([st('결의', 1), srch(), pw('play', [sh(0.35), stk(K2, 1)], { when: { marked: K, type: '스킬' }, limit: 2 })]),
      Or([st('결의', 1), pw('play', [sh(0.35), stk(K2, 1), draw(1)], { when: { marked: K, type: '스킬' }, limit: 1 })], { tags: ['개전'] }),
    ]), bl('defUp', 'draw', [stk(K2, 1)])),
    // 둘째 — 장갑 던지기: 아군 1명 초청 + 방어 기반(결투 신청)
    U(H, 5, '장갑 던지기', 1, '공격', [stk(K, 1, ALLY), ddef(0.75)], [
      'A', 'B',
      Or([stk(K, 1, ALLY), ddef(0.65), st('약화', 1)]),
      ['D', 'turnStart', [stk(K2, 1)]],
      'Hd',
    ], bl('power', 'cost', [stk(K2, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '용족/비비': 0.95, '용족/루드': 0.65, '용족/네티': 1.2, '용족/다야': 1.2, '용족/시스트': 1.3, '용족/실비아': 0.9 };   // 실비아 0.9 — 118명 최종 측정(엘다인 상한 넘음)
// ── 돌리기 ──
const JOBS = [
  ['용족/네티', netti], ['용족/다야', daya], ['용족/다야_퓨어샤인', dayaPure], ['용족/루드', rude],
  ['용족/리츠', ritz], ['용족/비비', bibi], ['용족/시스트', syst], ['용족/실비아', sylvia],
];
L.run18(JOBS, TUNE, new URL('./boost_용족A_18.json', import.meta.url));
