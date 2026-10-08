// 18갈래 재설계 3단계 — 유령B 묶음 8명(셰이디 · 셰이디(역전) · 스피키 · 스피키(메이드) · 시온 더 다크불릿 · 앨리스 · 에스피 · 키샤). 2026-10-08
// 기준: 시범 17명(_measure/갈래_세분화/05_시범16_결과.md) · 지침 _gen/rework/BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/유령B.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(만들기 · 쓰기 · 갈래 부품 · 원작 자유) · 신탁 5갈래 · 축복 4장 × 3(시동 카드는 공용 축복 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/유령B_18.mjs [사도 파일 이름 일부]  → heroes/유령/<파일>.json (백업 SRC 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, st, stk, spendAll, per, draw, make, ap, ifStack, pas, pw, U, Or, card, bl, setCards, setOpener,
  token, disc, srch, drawType, pull, notStack, ripen, empower, tough, cs, ALLY } = L;
const M = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
const pick = id => ({ k: 'spend', id, pick: true });
const perEv = { k: 'perEvent' };
const feed = (v, target) => ({ k: 'feed', v, ...(target ? { target } : {}) });
const ifSpent = n => ({ k: 'ifSpent', n });
const ifHunted = { k: 'ifHunted' };
const ifPile = (n, from = 'discard') => ({ k: 'ifPile', n, from });
const rush = (v, target) => ({ k: 'rushDown', v, ...(target ? { target } : {}) });
const cast = ratio => ({ k: 'castOther', ratio });
const cheap = (o = {}) => cs('비용', -1, { to: 'hand', n: 1, ...o });

// ════════════════════════════════════════════════════════════════════
// 1. 셰이디 — 예약형 · 딜러 · 광기. 적에게 「장난」 을 떠벌려 걸어 두고(예약 — 다 닳으면 자세 붕괴), 그 사이 적의 차례를 뒤로 미룬다
// 원작: 혼돈의 유령 우두머리 · 달력 · 전산망을 한 달 뒤로 미룬 장난 · 계략을 떠벌림 · 저학년 가장 뒤 적 순간이동 연타 · 고학년 타임 오브 셰이디(침묵) · 애착 아티팩트 사슬낫(방어 깎기)
// 장치: 「장난」(적 · 예약 2칸 — 다 닳으면 피해 · 강인도 · 셰이디 다음 카드 강화) · 시동: u5 파인애플 피자 협박(0 · 옛 u1)
// 마카샤(시계를 당겨 씀)와 달리 셰이디는 적을 매달아 미루고(기절 · 행동 늦춤) 장난이 먼저 터지게 한다
// ════════════════════════════════════════════════════════════════════
function shady(j) {
  const H = '셰이디', K = '장난';
  const h = j.heroes[0];
  h.blurb = '역대 최고의 장난을 평생 소원으로 삼은 유령 우두머리. 적에게 「장난」 을 떠벌려 걸어 두면 두 차례 뒤 그 적이 자세를 무너뜨리며 얻어맞습니다. 그 사이 적을 매달아 두거나 차례를 뒤로 미뤄 장난이 먼저 터지게 하고, 급하면 순간이동해 지금 터뜨립니다.';
  h.keyword = {
    name: K, desc: '셰이디가 미리 떠벌려 둔 장난', carrier: 'enemy', cap: 2, decay: 1, reserve: true,
    rules: [{ name: '자세 붕괴', when: { on: 'stackGone', id: K, decay: true }, fx: [dmg(0.55), empower(0.1)] }],   // 3단계 손질(2026-10-09): 0.8 · +20% → 0.55 · +10%(봇이 걸린 강화를 두 수 앞으로 보게 되어 세짐)
  };
  h.passives = [
    pas('장난질', 'play', [notStack(K), stk(K, 1, E1)], { when: { type: '공격' }, limit: 1 }),
    pas('다음 장난 예고', 'kill', [stk(K, 1, ER)], { when: { mine: true }, limit: 1 }),   // 3단계 손질(2026-10-09): 적 전체 → 무작위 적 하나(새 바탕에서 딜러 상한 넘음)
  ];
  setOpener(j, H, 'u1', 'u5');
  setCards(j, [
    // 쓰기 — 불 좀 꺼줄래?(저학년 순간이동 연타): 4연타 + 고른 적의 장난을 지금 터뜨림(남은 1칸당 -30%)
    U(H, 1, '불 좀 꺼줄래?', 1, '공격', [dmg(0.25, E1, { hits: 4 }), ripen(K, 0.3)], [
      'A', 'B',
      Or([dmg(0.2, ER, { hits: 5 }), ripen(K, 0.3, EA)]),
      ['D', 'turnStart', [stk(K, 1, E1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 원작 자유 — 사슬낫 네 자루(애착 아티팩트 사슬낫 1~4호 · 방어 깎기): 무작위 4회 + 취약
    U(H, 2, '사슬낫 네 자루', 1, '공격', [dmg(0.3, ER, { hits: 4 }), st('취약', 1, E1)], [
      'A', 'B',
      ['C', [stk(K, 1, EA)]],
      ['D', 'play', [dmg(0.2, ER)], { when: { type: '공격' }, limit: 2 }],
      'Hd',
    ], bl('power', 'draw', [stk(K, 1, E1)])),
    // 갈래 부품 — 대롱대롱 매달기(미루기): 적 하나를 매달아 다음 차례를 막고 장난 1 · 소멸(전투에 한 번)
    card(H, 3, '대롱대롱 매달기', 1, '스킬', [st('기절', 1, E1), stk(K, 1, E1)], M([
      Or([st('기절', 1, E1), stk(K, 1, E1), st('취약', 2, E1)], { tags: ['소멸'] }),
      Or([st('기절', 1, E1), stk(K, 1, E1)], { cost: 0, tags: ['소멸'] }),
      Or([rush(1, EA), stk(K, 1, EA), draw(1)]),
      Or([st('기절', 1, E1), stk(K, 1, E1), pw('turnStart', [rush(1, E1)])], { power: true }),
      Or([st('기절', 1, E1), stk(K, 2, E1), draw(2)], { tags: ['소멸'] }),
    ]), bl('draw', 'ap', [stk(K, 1, E1)]), ['소멸']),
    // 둘째 — 차원 주머니: 2연타, 장난이 걸린 적이면 2연타 더(기다린 만큼)
    U(H, 4, '차원 주머니', 1, '공격', [dmg(0.35, E1, { hits: 2 }), ifStack(K, 1), dmg(0.35, E1, { hits: 2 })], [
      'A',
      ['D', 'play', [stk(K, 1, E1)], { when: { type: '스킬' }, limit: 1 }],
      Or([dmg(0.3, EA, { hits: 2 }), stk(K, 1, EA)]),
      Or([dmg(0.35, E1, { hits: 2 }), stk(K, 1, E1), srch()]),
      'Hn',
    ], bl('draw', 'cost', [stk(K, 1, E1)])),
    // 시동 — 파인애플 피자 협박(0): 장난 1 + 공격 카드 1장
    card(H, 5, '파인애플 피자 협박', 0, '스킬', [stk(K, 1, E1), drawType('공격')], M([
      Or([stk(K, 1, E1), drawType('공격', 2)]),
      Or([stk(K, 2, E1), drawType('공격'), st('약화', 1, E1)]),
      Or([stk(K, 1, EA), drawType('공격'), sh(0.6)]),
      Or([stk(K, 1, E1), drawType('공격'), pw('kill', [stk(K, 1, EA)], { limit: 1 })], { power: true }),
      Or([disc(1), stk(K, 1, E1), dmg(0.9)]),
    ]), null),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 셰이디(역전) — 쌓고 고르기 · 서포터 · 활발. 착한 척 참은 「울분」 을 인형 샌드백으로 풀지(딜), 쓰다듬기로 파티에 나눌지(지원) 고른다
// 원작: 늪의 파수꾼 · 착한 척 성깔을 꾹 참음 · 말썽꾼 인형 샌드백 · 저학년 차원의 틈(스피키 호박 · 레테 레이저 · 앨리스 냥의자) · 강화 평타 위험한 아군 회복
// 장치: 「울분」(스킬 · 맞음으로 쌓임 — 다 차면 파티 다음 카드 +50%, 옛 「인형 패기」 저절로 터짐 없앰) · 시동: u4 내 방식대로 지키겠어(개전 강화, 그대로)
// 셰이디(예약 — 적에게 걸고 기다림)와 달리 역전은 제 안에 쌓아 두고 털 곳을 고른다
// ════════════════════════════════════════════════════════════════════
function shadyRev(j) {
  const H = '셰이디_역전', K = '울분', PUMP = '셰이디_역전_pumpkin';
  const h = j.heroes[0];
  h.blurb = '나약함을 인정하고 유령 늪의 파수꾼이 된 장난꾼. 착한 척 스킬을 쓰거나 맞을 때마다 「울분」 을 꾹 삼키고, 모은 울분을 인형 샌드백에 고른 만큼 풀지 파티를 쓰다듬는 데 나눌지 고릅니다. 셋이 다 차면 참다 터진 기세가 파티의 다음 한 수에 실립니다.';
  h.keyword = { name: K, desc: '착한 척하느라 삼킨 화', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.05, who: 'allies' }], onMax: { empower: 'any', ratio: 0.6, consume: true } };
  h.passives = [
    pas('착한 척은 힘들어', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 2 }),
    pas('참다 터짐', 'hurt', [stk(K, 1), sh(0.4)], { limit: 2 }),
  ];
  setCards(j, [
    // 원작 자유 — 차원의 틈(저학년 소품): 울분 + 스피키의 호박 2장 / 레테의 레이저(피해 감소)
    Object.assign(U(H, 1, '차원의 틈', 1, '스킬', [stk(K, 1), { k: 'ifChoice', n: 1 }, make(PUMP, 2), { k: 'ifChoice', n: 2 }, st('피해 감소', 2)], [
      'A', 'B',
      Or([stk(K, 1), { k: 'ifChoice', n: 1 }, make(PUMP, 2), { k: 'ifChoice', n: 2 }, dmg(1.1, EA)]),
      Or([stk(K, 2), { k: 'ifChoice', n: 1 }, make(PUMP, 2), { k: 'ifChoice', n: 2 }, st('피해 감소', 3)]),
      Or([stk(K, 2), { k: 'ifChoice', n: 1 }, make(PUMP, 3), { k: 'ifChoice', n: 2 }, draw(2, { who: 'other' })]),
    ], bl('heal', [sh(0.4)], [make(PUMP, 1)])), { choices: ['스피키의 호박', '레테의 레이저'] }),
    // 쓰기(딜) — 유령 인형 샌드백: 적 전체 + 울분을 고른 만큼 소모, 1개당 적 전체 한 번 더
    U(H, 2, '유령 인형 샌드백', 1, '공격', [dmg(0.6, EA), pick(K), perEv, dmg(0.38, EA)], M([
      Or([dmg(0.8, EA), pick(K), perEv, dmg(0.45, EA)]),
      Or([dmg(0.5, EA), pick(K), perEv, dmg(0.34, EA)], { cost: 0 }),
      Or([dmg(1.2), pick(K), perEv, dmg(0.7)]),
      Or([pick(K), perEv, dmg(0.6, EA), pw('spend', [st('약화', 1, EA)], { when: { id: K }, limit: 1 })], { power: true }),
      Or([dmg(0.55, EA), per(K), dmg(0.3, EA), stk(K, 1)]),
    ]), bl('power', 'weakSpot', [stk(K, 1)])),
    // 둘째 — 파수꾼의 낫질: 피해 + 실드 + 울분
    U(H, 3, '파수꾼의 낫질', 1, '공격', [dmg(1.27), sh(0.9), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.9, EA), sh(0.8), stk(K, 1)]),
      ['D', 'hurt', [stk(K, 1)], { limit: 1 }],
      'Hd',
    ], bl('power', 'guard', [stk(K, 1)])),
    // 시동 — 내 방식대로 지키겠어(개전 강화): 사기 + 매 턴 울분
    card(H, 4, '내 방식대로 지키겠어', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1)])], M([
      Or([st('사기', 1), stk(K, 1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([st('사기', 1), stk(K, 1), L.power(L.rule('turnStart', [stk(K, 1)]), L.rule('spend', [heal(0.3)], { when: { id: K }, limit: 1 }))], { tags: ['개전'] }),
      Or([st('사기', 1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([st('사기', 1), sh(1.0), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 쓰기(지원) · 갈래 부품 — 헤벌레 쓰다듬기: 울분을 고른 만큼 소모, 1개당 회복 · 아군 전원의 키워드 +1(짝의 재료를 채움)
    U(H, 5, '헤벌레 쓰다듬기', 1, '스킬', [pick(K), perEv, heal(0.45), feed(1)], M([
      Or([pick(K), perEv, heal(0.55), feed(1)]),
      Or([pick(K), perEv, heal(0.4), feed(1)], { cost: 0 }),
      Or([pick(K), perEv, sh(0.5), make(PUMP, 1)]),
      Or([pick(K), perEv, heal(0.35), pw('spend', [feed(1)], { when: { id: K }, limit: 1 })], { power: true }),
      Or([heal(0.8), stk(K, 1), make(PUMP, 1)]),
    ]), bl('heal', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 스피키 — 손 만들기형 · 서포터 · 순수. 다른 아군 카드를 흉내 내(대신 발동) 변장을 쌓고, 셋이 차면 「호박 사탕」(AP)이 손에 온다
// 원작: 정체성의 유령 — 남을 흉내 내면 보는 이의 인식이 비틀려 진짜로 보임(네르 · 크레페 · 마요 흉내) · 저학년 아군 SP 회복 · 고학년 아군 받는 피해 감소 · 호박 친구
// 장치: 「변장」(다른 아군 카드마다 +1 — 다 차면 「호박 사탕」 카드, 옛 「완벽한 흉내」 저절로 터짐 없앰) · 시동: u1 펌킨 매직(그대로)
// 스피키(메이드)(버린 더미의 양을 쏟는 딜러)와 달리 스피키는 손에 든 아군 카드를 흉내 내고 AP 를 붓는 지원
// ════════════════════════════════════════════════════════════════════
function spiky(j) {
  const H = '스피키', K = '변장', CANDY = '스피키_candy', MIMIC = '스피키_mimic';
  const h = j.heroes[0];
  h.blurb = '남을 흉내 내 보는 이의 인식을 비트는 따라쟁이 유령. 다른 아군이 카드를 낼 때마다 「변장」 이 쌓이고, 손에 든 아군 카드를 그 사도인 척 대신 꺼내 씁니다. 변장 셋이 다 차면 「완벽한 흉내」 가 손에 와, 아군 카드 한 장을 그대로 따라 하고 AP 까지 붓습니다.';
  h.keyword = { name: K, desc: '아군을 흉내 낸 어설픈 분장', carrier: 'self', cap: 3, onMax: { make: MIMIC, consume: true } };
  h.passives = [
    pas('완벽한 따라쟁이', 'play', [stk(K, 1)], { when: { who: 'other' }, limit: 3 }),
    pas('호박밭 산책', 'turnEnd', [st('저장', 1)], { conds: [{ c: 'apLeft', n: 1 }] }),
  ];
  const tokens = [token(MIMIC, '완벽한 흉내', H, '스킬', [cast(1.0), ap(1)], { blurb: '보는 이의 인식까지 비틀어 그 아군 그대로 — 호박 사탕 한 알은 덤' })];
  setCards(j, [
    // 시동 — 펌킨 매직: AP +1 · 변장 · 다른 아군의 카드 1장
    U(H, 1, '펌킨 매직', 1, '스킬', [ap(1), stk(K, 1), draw(1, { who: 'other' })], M([
      Or([ap(1), stk(K, 2), draw(1, { who: 'other' })]),
      Or([ap(1), stk(K, 1)], { cost: 0 }),
      Or([ap(1), stk(K, 1), draw(2, { who: 'other' })]),
      Or([ap(1), stk(K, 1), sh(1.6)]),
      Or([ap(2), stk(K, 1), disc(1)]),
    ]), null),
    // 갈래 부품 — 사제장 대리(흉내): 손의 다른 사도 카드 하나를 대신 발동(효과 80%, 카드는 손에 남음) · 변장
    card(H, 2, '사제장 대리', 1, '스킬', [cast(1.0), stk(K, 1)], M([
      Or([cast(1.0), stk(K, 1), sh(0.9)]),
      Or([cast(0.8), stk(K, 1)], { cost: 0 }),
      Or([cast(1.0), stk(K, 1), draw(1, { who: 'other' })]),
      Or([cast(0.8), stk(K, 1), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { power: true }),
      Or([cast(1.0), cast(0.8), disc(1)]),
    ]), bl('draw', [heal(0.4)], 'cost')),
    // 만들기 둘째 — 호박 바구니(0): 다른 아군 카드 1장 비용 -1 · 변장
    card(H, 3, '호박 바구니', 0, '스킬', [cheap({ who: 'other' }), stk(K, 1)], M([
      Or([cheap({ who: 'other' }), stk(K, 1), draw(1)]),
      Or([cheap({ who: 'other' }), stk(K, 2)]),
      Or([cheap({ who: 'other' }), stk(K, 1), heal(0.5)]),
      Or([cheap({ who: 'other' }), stk(K, 1), pw('turnStart', [cheap({ who: 'other' })])], { power: true }),
      Or([cheap({ who: 'other' }), make(CANDY, 1), disc(1)]),
    ]), bl('draw', [heal(0.3)], [stk(K, 1)])),
    // 쓰기 — 한 번 맞아보실래요오?: 적 전체 + 변장 1개당, 전부 소모(사탕을 받을지 지금 털지)
    U(H, 4, '한 번 맞아보실래요오?', 1, '공격', [dmg(0.68, EA), per(K), dmg(0.34, EA), spendAll(K)], [
      'A',
      ['D', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([dmg(1.0), per(K), dmg(0.5), spendAll(K)]),
      ['C', [st('약화', 1, EA)]],
      'Hd',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 원작 자유 — 주말농장 막말: 피해 + 변장 + 다른 아군의 카드 1장
    U(H, 5, '주말농장 막말', 1, '공격', [dmg(1.08), stk(K, 1), draw(1, { who: 'other' })], [
      'A', 'B',
      Or([dmg(0.75, EA), st('약화', 1, EA), stk(K, 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hd',
    ], bl('guard', 'ap', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 4. 스피키(메이드) — 버리기형 · 딜러 · 활발. 고장 난 청소기가 버려진 카드를 「잡동사니」 로 빨아들였다가, 쌓인 양만큼 한꺼번에 뱉는다
// 원작: 크레페를 흉내 낸 메이드 · 고장 난 청소기로 잡동사니 셋 중 하나를 뱉음 · 고학년 터보 클린 모드(폭주 5연타) · 설탕 보고서를 태워 청소 · 독설
// 장치: 「잡동사니」(카드가 버려지면 +1 — 다 차면 다음 카드 +60%, 옛 「터보 배출」 저절로 터짐 없앰) · 시동: u3 호박 장식 시즌(0 · 옛 u1)
// 레테(손을 잊어 버림을 만듦)와 달리 메이드는 버린 더미의 양을 먹는다(버린 더미 5장 이상이면 덤)
// ════════════════════════════════════════════════════════════════════
function spikyMaid(j) {
  const H = '스피키_메이드', K = '잡동사니';
  const h = j.heroes[0];
  h.blurb = '메이드 크레페의 일을 통째로 뒤집어쓴 꼬마 유령. 카드가 버려질 때마다 고장 난 청소기가 「잡동사니」 로 빨아들이고, 쌓인 잡동사니를 흡입구로 한꺼번에 뱉습니다. 넷이 다 차면 청소기가 폭주해 다음 한 수가 크게 나가고, 버린 더미가 수북할수록 특훈 솜씨가 붙습니다.';
  h.keyword = { name: K, desc: '고장 난 청소기에 빨려 든 것들', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.06 }], onMax: { empower: 'next', ratio: 0.6, consume: true } };
  h.passives = [
    pas('고장난 청소기', 'discard', [stk(K, 1)], { when: { who: 'any' }, limit: 2 }),
    pas('고장난 청소기', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }),
    pas('자동 청소 모드에요오!', 'ult', [st('사기', 1)]),
  ];
  setOpener(j, H, 'u1', 'u3');
  setCards(j, [
    // 원작 자유 — 깨끗하면 할 일이 없어!(저학년 잡동사니 뱉기): 적 전체 + 약화(전단지) + 잡동사니
    U(H, 1, '깨끗하면 할 일이 없어!', 1, '공격', [dmg(0.75, EA), st('약화', 1, EA), stk(K, 1)], [
      'A', 'B',
      Or([dmg(1.0, EA), disc(1), stk(K, 2)]),
      Or([dmg(0.9, EA), stk(K, 1), pw('discard', [dmg(0.3, ER)], { when: { who: 'any' }, limit: 2 })], { power: true }),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 고장난 흡입구: 무작위 3회 + 잡동사니 1개당 무작위 1회, 전부 소모
    U(H, 2, '고장난 흡입구', 1, '공격', [dmg(0.41, ER, { hits: 3 }), per(K), dmg(0.3, ER), spendAll(K)], [
      'A',
      ['D', 'discard', [stk(K, 1)], { when: { who: 'any' }, limit: 1 }],
      Or([dmg(0.5, E1, { hits: 3 }), per(K), dmg(0.36), spendAll(K)]),
      Or([dmg(0.4, ER, { hits: 3 }), per(K), dmg(0.28, ER), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', 'ap')),
    // 시동 — 호박 장식 시즌(0): 손패 1장 버리고 1장 뽑기 · 잡동사니
    card(H, 3, '호박 장식 시즌', 0, '스킬', [disc(1), draw(1), stk(K, 1)], M([
      Or([disc(1), draw(2), stk(K, 1)]),
      Or([disc(2), draw(2), sh(0.8)]),
      Or([disc(1), draw(1), sh(0.9)]),
      Or([disc(1), draw(2), pw('discard', [stk(K, 1)], { when: { who: 'any' }, limit: 1 })], { power: true }),
      Or([{ k: 'discard', all: true }, draw(3), stk(K, 2)]),
    ]), null),
    // 갈래 부품 — 크레페 선배의 특훈: 무작위 2회, 버린 더미가 5장 이상이면 2회 더
    U(H, 4, '크레페 선배의 특훈', 1, '공격', [dmg(0.4, ER, { hits: 2 }), ifPile(5), dmg(0.4, ER, { hits: 2 })], [
      'A', 'B',
      Or([dmg(0.55, ER, { hits: 2 }), disc(1), stk(K, 1)]),
      Or([dmg(0.4, ER, { hits: 2 }), ifPile(5), dmg(0.4, ER, { hits: 2 }), pw('turnStart', [stk(K, 1)])], { power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 둘째 — 베이킹 소다 한 줌(0): 손패 1장 버리기 · 잡동사니 · 손의 자신의 공격 카드 비용 -1
    card(H, 5, '베이킹 소다 한 줌', 0, '스킬', [disc(1), stk(K, 1), cheap({ who: 'self', type: '공격' })], M([
      Or([disc(1), stk(K, 2), cheap({ who: 'self', type: '공격' })]),
      Or([disc(1), stk(K, 1), sh(0.8)]),
      Or([disc(1), draw(1), cheap({ who: 'self', type: '공격' })]),
      Or([disc(1), stk(K, 1), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([disc(2), stk(K, 3)]),
    ]), bl('ap', 'guard', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 시온 더 다크불릿 — 표적형 · 딜러 · 우울 · 엘다인. 저격 「좌표」 를 한 적에게 찍고(한 적에게만 — 진혼의 마탄 · 디 엑시트로가 더 깊이), 마탄을 다섯까지 재웠다 쏟는다
// 원작: 자칭 마탄의 사수(편의점 야간 알바) · 모든 공격이 가장 먼 적 저격 · 마탄 최대 5(1개당 공격력) · 저학년 마탄을 다 쏟아 연사 · 화합(엘다인)
// 장치: 「검은 탄창」(다 차면 「진혼의 마탄」 카드 — 옛 「다섯이면 턴마다 마탄」 을 다 쓰고 한 장으로) + 새 「좌표」(적 · 찍기) · 시동: u3 좌표 잡기(0, 그대로)
// ════════════════════════════════════════════════════════════════════
function sion(j) {
  const H = '시온더다크불릿', K = '검은 탄창', Z = '좌표', T1 = '시온더다크불릿_t1';
  const h = j.heroes[0];
  h.blurb = '자칭 검은 마탄의 사수, 실상은 편의점 야간 알바. 저격할 적 1명에게 「좌표」 를 찍어 두면 그 적에게는 진혼의 마탄이 더 깊이 박히고, 공격할 때마다 검은 탄창에 마탄이 재워집니다. 다섯 발이 차면 진혼의 마탄이 손에 장전됩니다.';
  h.keywords = [{ name: Z, desc: '시온이 찍어 둔 저격 좌표', carrier: 'enemy', cap: 2, hunt: true,
    rules: [{ name: '다음 좌표', when: { on: 'huntDown' }, fx: [stk(Z, 1, 'topEnemy')] }] }];
  h.passives = [h.passives[0], pas('화합', 'lowHp', [st('피해 감소', 2), make(T1, 1)], { when: { pct: 0.4 } })];
  h.keyword = { ...h.keyword, per: [{ stat: 'atk', v: 0.03 }], rules: undefined, onMax: { make: T1, consume: true } };
  Object.assign(j.cards.find(c => c.id === T1), { fx: [dmg(0.85), ifHunted, dmg(0.35)] });
  setCards(j, [
    // 쓰기 — 마.탄.의.사.수: 탄창 1개당 1발, 전부 소모. 좌표 찍힌 적이면 한 발 더
    U(H, 1, '마.탄.의.사.수', 1, '공격', [dmg(0.7), per(K), dmg(0.3), spendAll(K)], [
      'A',
      ['D', 'huntDown', [stk(K, 2)], { limit: 1 }],
      Or([dmg(0.6), per(K), dmg(0.3), stk(Z, 1, E1)]),
      ['C', [stk(Z, 1, E1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 원작 자유 — 디 엑시트로(정체를 들킨 편의점 퇴장): 피해 + 탄창, 좌표 찍힌 적이면 탄창 1 더
    U(H, 2, '디 엑시트로', 1, '공격', [dmg(1.0), stk(K, 1), ifHunted, stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.85, EA), stk(K, 1)]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 }, limit: 2 }],
      'Hn',
    ], bl('power', 'draw', [stk(Z, 1, E1)])),
    // 시동 — 좌표 잡기(0): 좌표 1 · 탄창 1 · 드로우
    card(H, 3, '좌표 잡기', 0, '스킬', [stk(Z, 1, E1), stk(K, 1), draw(1)], M([
      Or([stk(Z, 2, E1), stk(K, 1), draw(1)]),
      Or([stk(Z, 1, E1), stk(K, 2), draw(1)]),
      Or([stk(Z, 1, E1), stk(K, 1), dmg(0.85, E1)]),
      Or([stk(Z, 1, E1), draw(1), pw('huntDown', [make(T1, 1)], { limit: 1 })], { power: true }),
      Or([disc(1), stk(Z, 2, E1), make(T1, 2)]),
    ]), null),
    // 갈래 부품 — 진혼의 탄환(강화): 마탄 1장 + 0코 공격을 낼 때마다 탄창 +1
    card(H, 4, '진혼의 탄환', 1, '강화', [make(T1, 1), pw('play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 } })], M([
      Or([make(T1, 2), pw('play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 } })]),
      Or([make(T1, 1), stk(K, 1), pw('play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 } })]),
      Or([make(T1, 1), stk(Z, 1, E1), pw('play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 } })]),
      Or([make(T1, 1), srch(), pw('play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 } })]),
      Or([make(T1, 1), L.power(L.rule('play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 } }), L.rule('huntDown', [make(T1, 1)], { limit: 1 }))], { tags: ['종극'] }),
    ]), bl('draw', 'cost', [stk(K, 1)])),
    // 둘째 — 원 플러스 원(편의점): 탄창 · 마탄 · 실드
    U(H, 5, '원 플러스 원', 1, '스킬', [stk(K, 1), make(T1, 1), sh(0.4)], [
      Or([stk(K, 2), make(T1, 1), sh(0.6)]), 'B',
      Or([stk(Z, 1, E1), make(T1, 2)]),
      Or([stk(K, 1), make(T1, 1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 2), make(T1, 2), disc(1)]),
    ], bl('ap', 'guard', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 앨리스 — 계산형 · 딜러 · 광기. 카드마다 적힌 점괘(이번 턴 쓴 AP 가 꼭 n)를 맞혀 내면 「타로 한 장」 이 손에 펼쳐진다 — 행운은 한 방에 몰아 쓴다
// 원작: 운명의 유령 — 타로점 · 밑장 빼기 · 예지는 진짜 · 「행운 카드」 는 앨리스만 만진다 · 저학년 약식 점괘 셋 중 하나 · 어사이드 맞을수록 회복
// 장치: 「점괘 적중」(셈이 서면 = tally → 「타로 한 장」 카드, 턴 1 — 새 생성 장치) + 「행운」(1개당 주는 피해) · 시동: u3 비추는 거울(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function alice(j) {
  const H = '앨리스', K = '행운', TAROT = '앨리스_tarot';
  const h = j.heroes[0];
  h.blurb = '타로로 남의 미래를 봐 주다 슬쩍 골려 먹는 점술사 유령. 카드마다 적힌 점괘대로 이번 턴 쓴 AP 를 꼭 맞혀 내면 손에 타로 한 장이 펼쳐집니다. 남의 불운을 먹고 차오른 「행운」 은 한 방에 몰아 씁니다.';
  h.passives = [
    pas('완전 럭키 앨리스잖아', 'hurt', [stk(K, 1)], { limit: 2 }),
    pas('아르카나 한 장', 'turnStart', [{ k: 'ifRandom', pct: 0.3 }, st('사기', 1), { k: 'ifRandom', pct: 0.4 }, st('취약', 1, EA), { k: 'ifRandom', pct: 0.5 }, stk(K, 1)]),
    pas('아르카나 한 장', 'tally', [make(TAROT, 1)], { limit: 1 }),
  ];
  setCards(j, [
    // 원작 자유 · 만들기 — 아르카나(저학년 점괘): 적 전체 + 행운, 이번 턴 쓴 AP 가 꼭 2면 적 전체 취약
    U(H, 1, '아르카나', 1, '공격', [dmg(0.88, EA), stk(K, 1), ifSpent(2), st('취약', 1, EA)], [
      'A', 'B',
      Or([dmg(0.8, EA), stk(K, 1), ifSpent(1), st('취약', 1, EA)]),
      ['D', 'tally', [stk(K, 1)], { limit: 1 }],
      'Hd',
    ], bl('power', 'draw', [make(TAROT, 1)])),
    // 쓰기 — 남의 불운: 행운 1개당, 전부 소모
    U(H, 2, '남의 불운', 1, '공격', [dmg(1.08), per(K), dmg(0.41), spendAll(K)], [
      'A',
      ['D', 'tally', [dmg(0.4, E1)], { limit: 1 }],
      Or([dmg(1.0), per(K), dmg(0.4), ifSpent(3), stk(K, 2)]),
      ['C', [st('취약', 1, E1)]],
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 비추는 거울(개전 강화): 행운 2 + 매 턴 행운
    card(H, 3, '비추는 거울', 1, '강화', [stk(K, 2), pw('turnStart', [stk(K, 1)])], M([
      Or([stk(K, 2), draw(1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 2), L.power(L.rule('turnStart', [stk(K, 1)]), L.rule('tally', [stk(K, 1)], { limit: 1 }))], { tags: ['개전'] }),
      Or([stk(K, 2), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 3), make(TAROT, 1), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 갈래 부품 — 밑장 빼기(0): 1장 버리고 1장 뽑기, 이번 턴 쓴 AP 가 꼭 3이면 행운 2(맞춰 끼우는 0코)
    card(H, 4, '밑장 빼기', 0, '스킬', [disc(1), draw(1), ifSpent(3), stk(K, 2)], M([
      Or([disc(1), draw(2), ifSpent(3), stk(K, 2)]),
      Or([draw(1), ifSpent(3), stk(K, 3)]),
      Or([disc(1), draw(2), ifSpent(3), make(TAROT, 1)]),
      Or([disc(1), draw(1), pw('tally', [draw(1)], { limit: 1 })], { power: true }),
      Or([disc(2), draw(2), ap(1)], { tags: ['소멸'] }),
    ]), bl('draw', [make(TAROT, 1)], 'cost')),
    // 둘째 — 뒤집힌 카드: 피해 + 행운, 이번 턴 쓴 AP 가 꼭 3이면 타로 한 장
    U(H, 5, '뒤집힌 카드', 1, '공격', [dmg(1.08), stk(K, 1), ifSpent(3), make(TAROT, 1)], [
      'A', 'B',
      Or([dmg(0.75, EA), stk(K, 1), ifSpent(2), make(TAROT, 1)]),
      Or([dmg(1.0), stk(K, 1), srch()]),
      'Hd',
    ], bl('ap', 'guard', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 에스피 — 손 만들기형 · 서포터 · 냉정. 이미 꾼 꿈(버린 더미의 카드)을 다시 손으로 불러오고, 그때마다 꿈 일기를 적는다 — 새 꿈은 못 꾼다
// 원작: 꿈의 유령 — 남의 꿈을 훔쳐봄 · 이미 꾼 꿈을 다시 보거나 결말을 바꿀 수는 있지만 새 꿈은 못 꾸게 함 · 저학년 촛불 2회 + 침묵 · 꿈 함정
// 장치: 「꿈 일기」(디버프 · 불러오기로 쌓임 — 다 차면 파티 다음 카드 +40%) · 시동: u1 헤롱헤롱 촛불(그대로)
// ════════════════════════════════════════════════════════════════════
function espi(j) {
  const H = '에스피', K = '꿈 일기';
  const h = j.heroes[0];
  h.blurb = '남의 꿈을 훔쳐보며 대리만족하는 유령. 새 꿈은 못 꾸지만 이미 꾼 꿈은 다시 볼 수 있어, 버린 더미의 카드를 손으로 불러오며 「꿈 일기」 를 적습니다. 일기가 가득 차면 파티의 다음 한 수가 꿈처럼 커지고, 촛불로 적을 잠재웁니다.';
  h.keyword = { name: K, desc: '몰래 훔쳐본 꿈을 적은 일기장', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.05, who: 'allies' }], onMax: { empower: 'any', ratio: 0.5, consume: true } };
  setCards(j, [
    // 시동 · 원작 자유 — 헤롱헤롱 촛불(저학년): 2회 + 약화 2, 꿈 일기 셋 이상이면 기절
    U(H, 1, '헤롱헤롱 촛불', 1, '공격', [dmg(0.57, E1, { hits: 2 }), st('약화', 2, E1), ifStack(K, 3), st('기절', 1, E1)], [
      'A', 'B',
      Or([dmg(0.75, E1, { hits: 2 }), st('약화', 2, E1), pull()]),
      Or([dmg(0.75, E1, { hits: 2 }), st('약화', 2, E1), stk(K, 2)]),
      'Hd',
    ], null),
    // 둘째 — 개꿈 발사!: 무작위 5회 + 꿈 일기
    U(H, 2, '개꿈 발사!', 1, '공격', [dmg(0.3, ER, { hits: 5 }), stk(K, 1)], [
      'A', 'B',
      Or([dmg(0.3, ER, { hits: 4 }), st('약화', 1, EA), stk(K, 1)]),
      ['D', 'debuff', [stk(K, 1)], { limit: 1 }],
      'Hd',
    ], bl('power', [heal(0.3)], 'draw')),
    // 만들기 — 꿈 훔쳐보기(0): 버린 더미 맨 위 카드 1장을 손으로 · 꿈 일기
    card(H, 3, '꿈 훔쳐보기', 0, '스킬', [pull(), stk(K, 1)], M([
      Or([pull(), stk(K, 2)]),
      Or([pull({ n: 2 }), stk(K, 1)]),
      Or([pull(), stk(K, 1), heal(0.5)]),
      Or([pull(), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([pull({ n: 2 }), disc(1), stk(K, 2)]),
    ]), bl('draw', [stk(K, 1)], [heal(0.3)])),
    // 쓰기 — 막말 가면: 피해 + 꿈 일기 1개당, 전부 소모(다 차서 파티에 나눌지 지금 털지)
    U(H, 4, '막말 가면', 1, '공격', [dmg(1.08), per(K), dmg(0.41), spendAll(K)], [
      'A',
      ['D', 'turnStart', [pull()], { limit: 1 }],
      Or([dmg(0.8, EA), per(K), dmg(0.3, EA), spendAll(K)]),
      ['C', [st('약화', 2, E1)]],
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 꿈 다시 보기(0): 다른 아군이 이미 꾼 꿈 — 버린 더미의 다른 아군 카드 1장을 손으로 · 꿈 일기
    card(H, 5, '꿈 다시 보기', 0, '스킬', [pull({ who: 'other' }), stk(K, 1)], M([
      Or([pull({ who: 'other' }), stk(K, 2)]),
      Or([pull({ who: 'other', n: 2 }), stk(K, 1)]),
      Or([pull({ who: 'other' }), stk(K, 1), st('약화', 1, E1)]),
      Or([pull({ who: 'other' }), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { power: true }),
      Or([disc(1), pull({ who: 'other', n: 2 }), stk(K, 2)]),
    ]), bl('draw', 'ap', [st('약화', 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 키샤 — 거드는 형 · 서포터 · 우울. 아군 하나를 「짱팬」 으로 찍어 그 아군의 공격에 팬 서비스(추가 공격)를 얹고, 공연을 열어 아군이 준 피해를 모아 터뜨린다
// 원작: 도취의 유령 언더돌 · 한 줌 팬을 진심으로 아낌 · 저학년 궁극의 멜로디(아군 피해↑ · 회복 → 팬 서비스) · 고학년 공연 동안 아군 피해를 모아 마지막에 터뜨림 · 떼창 지진
// 장치: 「짱팬」(아군 하나 · 그 아군 공격에 추가 공격) + 「공연 중」(아군 피해 저장 → 다음 턴 시작 무대 폭발 — 터짐 아님, 턴 시작 규칙) · 시동: u1 궁극의 멜로디(그대로)
// ════════════════════════════════════════════════════════════════════
function kisha(j) {
  const H = '키샤', K = '짱팬', SHOW = '공연 중', CHEER = '저장된 환호';
  const fanAtk = { when: { marked: K, type: '공격' }, limit: 1 };
  setCards(j, [
    // 시동 — 궁극의 멜로디: 아군 하나를 짱팬으로 · 회복 · 사기
    U(H, 1, '궁극의 멜로디', 1, '스킬', [stk(K, 1, ALLY), heal(1.0), st('사기', 1)], [
      'A', 'B',
      Or([stk(K, 1, ALLY), st('사기', 2), draw(1)]),
      Or([stk(K, 1, ALLY), st('사기', 2), pw('play', [heal(0.2)], { when: { marked: K }, limit: 2 })], { power: true }),
      Or([disc(1), stk(K, 1, ALLY), st('사기', 2)]),
    ], null),
    // 갈래 부품 — 체리 레드 조명봉(팬 서비스): 협공 + 다른 아군의 카드 1장
    card(H, 2, '체리 레드 조명봉', 1, '스킬', [st('협공', 1), draw(1, { who: 'other' })], M([
      Or([st('협공', 1), draw(2, { who: 'other' })]),
      Or([st('협공', 1), draw(1, { who: 'other' })], { cost: 0 }),
      Or([st('협공', 1), draw(1, { who: 'other', type: '공격' }), stk(K, 1, ALLY)]),
      Or([st('협공', 1), draw(1, { who: 'other' }), pw('play', [{ k: 'extra', ratio: 0.3 }], fanAtk)], { power: true }),
      Or([st('협공', 2), draw(1, { who: 'other' }), disc(1)]),
    ]), bl('draw', 'cost', [stk(K, 1, ALLY)])),
    // 둘째 — 하트 파동: 피해 + 약화 2
    U(H, 3, '하트 파동', 1, '공격', [dmg(0.9), st('약화', 2, E1)], [
      'A', 'B',
      Or([dmg(0.65, EA), st('약화', 1, EA)]),
      Or([dmg(0.8), st('약화', 2, E1), pw('play', [st('약화', 1, E1)], fanAtk)], { power: true }),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1, ALLY)])),
    // 쓰기 · 원작 자유 — 떼창 지진(공연): 공연을 열고 적 전체 — 다음 턴 시작에 모은 환호가 터진다
    U(H, 4, '떼창 지진', 1, '공격', [stk(SHOW, 1), dmg(0.6, EA)], [
      'A',
      Or([stk(SHOW, 1), dmg(0.5, EA), stk(CHEER, 3)]),
      ['D', 'turnStart', [stk(CHEER, 2)]],
      Or([dmg(0.5, EA), st('협공', 1), draw(1)]),
      'Hd',
    ], bl('power', [st('약화', 1, EA)], [stk(CHEER, 3)])),
    // 만들기 — 립싱크 무대(0): 짱팬 · 자신의 고유 카드 1장
    card(H, 5, '립싱크 무대', 0, '스킬', [stk(K, 1, ALLY), srch()], M([
      Or([stk(K, 1, ALLY), srch(), st('사기', 1)]),
      Or([stk(K, 1, ALLY), srch({ v: 2 })]),
      Or([stk(K, 1, ALLY), heal(0.6), draw(1, { who: 'other' })]),
      Or([stk(K, 1, ALLY), srch(), pw('play', [{ k: 'extra', ratio: 0.25 }], fanAtk)], { power: true }),
      Or([disc(1), stk(K, 1, ALLY), srch({ v: 2 })]),
    ]), bl('guard', 'heal', [stk(K, 1, ALLY)])),
  ]);
}

// ── 돌리기 ──
const JOBS = [
  ['유령/셰이디', shady], ['유령/셰이디_역전', shadyRev], ['유령/스피키', spiky], ['유령/스피키_메이드', spikyMaid],
  ['유령/시온더다크불릿', sion], ['유령/앨리스', alice], ['유령/에스피', espi], ['유령/키샤', kisha],
];
const TUNE = { '유령/셰이디': 0.6, '유령/시온더다크불릿': 0.7, '유령/앨리스': 0.75, '유령/키샤': 0.95, '유령/셰이디_역전': 1.3, '유령/스피키': 1.2, '유령/에스피': 1.35, '유령/스피키_메이드': 1.3 };
L.run18(JOBS, TUNE, new URL('./boost_유령B_18.json', import.meta.url));
