// 묶음 2 — 두 얼굴형 7 + 버티형 5. node _gen/deep/b2.mjs [사도 일부]
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, LOW, FAR, ALLY, ALLIES, OTHERS, dmg, ddef, hits, sh, heal, st, stk, spendAll, spendN, per, perTag, draw, make, gauge, disc, ifStack, notStack, ifKill, ifFoe, ifHand,
  empower, form, formEnd, tough, later, dmod, extra, cost, power, rule, pw, pas, O, B, U, S, T, hero } = L;
const cleanse = v => ({ k: 'cleanse', v });
const ifShieldNot = { k: 'ifShield', not: true };
const roll = n => ({ k: 'roll', n }), ifRoll = n => ({ k: 'ifRoll', n });
const exileTag = tag => ({ k: 'exileFrom', from: 'hand', all: true, tag });

// ── 코미 — 두 얼굴형 · 탱커 ──
// 엔진: AP 를 남기고 턴을 마치면 낮잠이 쌓이고(쿨쿨 코미 — 자는 동안 받는 피해↓) → 「엘프산 특제 사료」로 낮잠을 다 먹어 거대 코미(2턴, 냥펀치가 광역) — 언제 깨어날지 고른다
function komi(j) {
  // 팀: 「코미버터의 낮잠시간」 — 파티 피해 감소(원작 어사이드 「모든 아군 받는 피해 감소」) · 낮잠 1개당 턴 끝 파티 회복
  // 원작: 저학년 「푹신푹신 타임」(잠 — 회복 · 디버프 면역) · 고학년 「엘프산 특제 사료」(거대화 · 충격파) · 「일하면 지는 거야」 · 버터와 낮잠
  const H = '코미', K = '낮잠', FS = '코미_잠', FG = '코미_거대화';
  const basics = [
    S(`${H}_s1`, '냥펀치', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '몸 말기', 1, '스킬', [sh(1.4), stk(K, 1)]),
  ];
  const tokens = [T(`${H}_f1`, '거대 냥펀치', H, '공격', [dmg(0.75, EA)], { cost: 1, blurb: '거대해진 코미의 앞발입니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u4`, '엘프산 사료 한 그릇', 1, '강화', [stk(K, 1), pw('keepAp', [stk(K, 1)])], [ // 시동 — 일하면 지는 거야
      O([stk(K, 1), sh(0.6), pw('keepAp', [stk(K, 1), sh(0.3)])]),                  // 남긴 AP 로 실드도
      O([pw('turnEnd', [stk(K, 1)])], { cost: 0 }),                                 // 매일 낮잠(AP 상관없이)
      O([stk(K, 2), pw('keepAp', [heal(0.3)])]),                                    // 낮잠 회복
      O([stk(K, 1), pw('keepAp', [stk(K, 1), draw(1)])]),                           // 내일 할게(드로우)
      O([st('피해 감소', 1), pw('keepAp', [stk(K, 1)])]),                           // 숨바꼭질 중 낮잠
    ], null, ['개전']),
    U(`${H}_u1`, '푹신푹신 타임', 1, '스킬', [sh(0.9), cleanse(1), form(FS)], [         // 연료 — 원작 저학년(잠 · 디버프 면역)
      O([sh(1.0), form(FS), stk(K, 2)]),  // 잠꼬대 낮잠
      O([heal(0.8), stk(K, 2)], { cost: 0 }),  // 그냥 푹 자기(회복)
      O([sh(0.6), form(FS), stk(K, 1)], { cost: 0 }),                              // 꾸벅
      O([sh(0.9), cleanse(2)], { cost: 0 }),  // 디버프 털어 내기
      O([sh(0.6), form(FS), pw('turnStart', [cleanse(1), stk(K, 1)])], { power: true }),  // 매일 늦잠
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u5`, '엘프산 특제 사료', 2, '공격', [per(K, { n: 1 }), dmg(0.35, EA), spendAll(K), form(FG)], [ // 결정타 — 원작 고학년(거대화 · 충격파)
      O([per(K, { n: 1 }), dmg(0.35, EA), spendAll(K), form(FG)]),                // (수치)
      O([per(K, { n: 1 }), dmg(0.7, E1), spendAll(K), form(FG)]),                 // 한 놈만 깔아뭉개기
      O([per(K, { n: 1 }), dmg(0.3, EA), form(FG)]),                              // 낮잠은 남긴다
      O([form(FG), dmg(0.5, E1), st('기절', 1, E1)], { cost: 1 }),                               // 착지 충격파(기절 — 애착 베개)
      O([per(K, { n: 1 }), heal(0.4), spendAll(K), form(FG)], { cost: 1 }),  // 먹고 회복
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '코미버터의 낮잠시간', 1, '스킬', [sh(0.8), st('피해 감소', 1), stk(K, 1)], [ // 원작 자유 — 어사이드(버터와 낮잠 · 아군 받는 피해↓)
      O([sh(0.8), st('피해 감소', 1), stk(K, 1)]),                                 // (수치)
      O([sh(0.6), st('피해 감소', 2)], { cost: 0 }),  // 다 같이 누워
      O([stk(K, 2), dmod(0.15, OTHERS)]),                                          // 버터 깨우기(아군 피해)
      O([sh(0.5), pw('turnEnd', [heal(0.2)], { conds: [{ c: 'stack', id: K, n: 1 }] })], { cost: 0, power: true }),  // 낮잠 동안 회복
      O([sh(0.6), stk(K, 1), draw(1)], { cost: 0 }),                               // 슬쩍 눕기
    ], [B('guard'), B('heal'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '일하지 않고 모아 둔 낮잠', carrier: 'self', cap: 4, per: [{ stat: 'hot', ratio: 0.06 }] },
    passives: [
      pas('쿨쿨', 'keepAp', [stk(K, 1)]),
      pas('낮잠의 미학', 'foeActBefore', [spendN(K, 1), sh(0.35)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }] }),
    ],
    forms: [
      { id: FS, name: '쿨쿨 코미', desc: '푹 잠든 코미 — 받는 피해가 줄고 낮잠이 쌓임', turns: 1, mods: { taken: -0.3 }, passives: [{ name: '꿀잠', when: { on: 'turnEnd' }, fx: [stk(K, 1)] }], skin: null, anim: null },
      { id: FG, name: '거대화', desc: '특제 사료를 먹고 거대해진 코미 — 냥펀치가 광역', turns: 2, mods: { dealt: 0.25 }, cards: { [`${H}_s1`]: `${H}_f1` }, skin: null, anim: null },
    ],
    ult: { name: '엘프산 특제 사료', cost: 300, fx: [dmg(1.5, EA), st('기절', 1, E1), form(FG)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`],
    basics, tokens, uniques,
  });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '베개 끌어안기', when: { on: 'fightStart' }, fx: [stk(K, 2)] }];
}

// ── 이드(재활) — 두 얼굴형 · 탱커 ──
// 엔진: 잠든 동안(1턴) 받은 피해를 악몽으로 저장하고 → 깨어날 때 저장한 악몽만큼 실드 · 적 전체 피해로 돌려준다(꿈의 공간 붕괴)
function ideRehab(j) {
  // 팀: 다른 아군이 스킬 카드를 낼 때마다 파티 실드(환상의 짝꿍 — 원작 「전열 아군 받는 피해 감소」) · 같이 걷는 산책(동료 카드)
  // 원작: 고학년 「이젠 스스로 마주하겠어」(잠든 동안 피해 저장 → 깨면 붕괴 피해 + 보호막) · 저학년 「뚜렷한 경계」(폭발 · 흔적 위 아군 회복) · 뮤트와 산책 · 「넘어져도 다시」
  const H = '이드_재활', K = '악몽', F = '이드_재활_잠';
  const basics = [
    S(`${H}_s1`, '꿈결 두드리기', 1, '공격', [ddef(0.45, E1), stk(K, 1)]),
    S(`${H}_s2`, '한 걸음씩', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '잠든 동안', 1, '스킬', [sh(0.6), stk(K, 1), form(F)], [                    // 시동 — 고학년의 잠
      O([sh(0.6), stk(K, 1), form(F)]),                                            // (수치)
      O([form(F), stk(K, 2)], { cost: 0 }),                                         // 눈 감기(0코)
      O([sh(0.5), form(F), st('초재생', 1)]),                                       // 자는 동안 회복
      O([sh(0.5), form(F), pw('hurt', [stk(K, 1)])], { power: true }),                              // 악몽에 익숙해지기
      O([sh(0.9), stk(K, 2)]),                                                     // 깨어 있는 채 버티기
    ], null),
    U(`${H}_u1`, '뚜렷한 경계', 1, '공격', [ddef(0.3, EA), heal(0.4), stk(K, 1)], [          // 연료 — 원작 저학년(경계 폭발 · 흔적 회복)
      O([ddef(0.3, EA), heal(0.4), stk(K, 1)]),                                    // (수치)
      O([ddef(0.6, E1), stk(K, 2)]),                                               // 좁은 경계
      O([ddef(0.25, EA), stk(K, 1), pw('turnEnd', [heal(0.15)])], { power: true }),  // 꿈의 흔적(매 턴 회복)
      O([ddef(0.3, EA), st('둔화', 1, EA)]),                                       // 꿈의 파장(SP 감소 대신 둔화)
      O([ddef(0.3, EA), empower(0.25, 'any')]),  // 아군 스킬 강화(원작)
    ], [B('power'), B('heal'), B(BK)]),
    U(`${H}_u4`, '꿈의 공간 붕괴', 1, '공격', [per(K, { n: 1 }), ddef(0.15, EA), spendAll(K), sh(0.8)], [ // 결정타 — 깨어날 때 무너지는 꿈
      O([per(K, { n: 1 }), ddef(0.15, EA), spendAll(K), sh(0.8)]),  // (수치)
      O([per(K, { n: 1 }), sh(0.4), spendAll(K), formEnd], { cost: 0 }),                        // 방패만(환장의 짝꿍 2배)
      O([per(K, { n: 1 }), ddef(0.25, E1), spendAll(K), formEnd]),                  // 한 놈에게 무너뜨리기
      O([per(K, { n: 1 }), ddef(0.15, EA), spendN(K, 3)]),  // 반만 깨기
      O([per(K, { n: 1 }), sh(0.25), spendAll(K), st('둔화', 1, EA)], { cost: 0 }),  // 몽롱한 붕괴
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '같이 걷는 산책', 1, '스킬', [sh(0.6), draw(1, { who: 'other' }), stk(K, 1)], [ // 원작 자유 — 뮤트와 산책 · 넘어져도 다시
      O([sh(0.6), draw(1, { who: 'other' }), stk(K, 1)]),                           // (수치)
      O([sh(0.5), draw(2)], { cost: 0 }),                                           // 가벼운 산책
      O([heal(0.7), stk(K, 1), cleanse(1)]),                                        // 넘어져도 다시
      O([sh(0.4), pw('play', [sh(0.15)], { when: { type: '스킬', who: 'other' } })], { cost: 0, power: true }),  // 매일 산책
      O([sh(0.6), formEnd, draw(2)]),                                               // 아침에 깨기
    ], [B('draw'), B('guard'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '꿈속에서 맞아 둔 아픔', carrier: 'self', cap: 6, per: [{ stat: 'guard', v: 0.05 }] },
    passives: [
      pas('악몽 마주하기', 'hurt', [stk(K, 1)]),
      pas('환상의 짝꿍', 'play', [sh(0.15)], { when: { type: '스킬', who: 'other' } }),
    ],
    forms: [{ id: F, name: '잠든 이드', desc: '꿈속에 잠든 이드 — 받는 피해가 줄고 맞을 때마다 악몽이 쌓임', turns: 1, mods: { taken: -0.3 },
      passives: [{ name: '꿈속의 아픔', when: { on: 'hurt' }, fx: [stk(K, 1)] }], off: [per(K, { n: 1 }), ddef(0.08, EA)], skin: null, anim: null }],
    ult: { name: '이젠 스스로 마주하겠어', cost: 250, fx: [form(F), sh(2.5), stk(K, 3), ddef(0.75, EA)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`],
    basics, uniques,
  });
}

// ── 하이디 — 두 얼굴형 · 딜러 ──
// 엔진: 상자에 숨어(잠입) 셔터를 모으고 → 초 집중 취재로 특종감(HP 가장 낮은 적 — 하이디에게 받는 피해 +25%)을 몰아 찍는다 → 특종감이 쓰러지면 바로 다음 특종
function heidi(j) {
  // 팀: 다른 아군이 특종감을 치면 잠복 1(목표 포착!) — 동료가 찍은 적을 같이 몰아붙인다
  // 원작: 저학년 「잠입 취재 중!」(눈속임 · 다연타 · SP 멈춤) · 고학년 「현장 중계 중!」(HP 비율 가장 낮은 적 특종감 · 처치하면 다음) · 셀카 기자 · 음모론
  const H = '하이디', M = '특종감', K = '잠복', FI = '하이디_잠입', FF = '하이디_집중';
  const basics = [
    S(`${H}_s1`, '줌 인 샷', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '망원 풀줌 샷', 2, '공격', [dmg(1.9, E1), stk(M, 1, E1)]),
    S(`${H}_s3`, '삼각대 가드', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const tokens = [
    T(`${H}_f1`, '플래시 연사', H, '공격', [dmg(0.42, E1, { hits: 3 }), st('약화', 1, E1)], { cost: 1, blurb: '상자 틈으로 터뜨리는 플래시입니다' }),
    T(`${H}_f2`, '특종 셔터', H, '공격', [dmg(1.4, 'markedEnemy'), stk(K, 1)], { cost: 1, blurb: '특종감만 쫓는 셔터입니다' }),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '잠입 취재 중!', 1, '스킬', [stk(K, 2), st('회피', 1), form(FI)], [            // 시동 — 원작 저학년
      O([stk(K, 2), st('회피', 1), form(FI)]),                                     // (수치)
      O([stk(K, 3), st('회피', 2)]),                                               // 숨기만
      O([form(FI), draw(2)], { cost: 0 }),                                         // 급히 숨기
      O([stk(K, 2), form(FI), pw('turnStart', [stk(K, 1)])], { power: true }),  // 장기 잠입
      O([stk(K, 2), form(FI), gauge(30)]),  // 특종 패스(고학년 앞당김)
    ], null),
    U(`${H}_u3`, '특종감 지정', 1, '공격', [stk(M, 1, LOW), dmg(0.6, LOW), stk(K, 1)], [      // 연료 — 고학년의 특종감 지정
      O([stk(M, 1, LOW), dmg(0.6, LOW), stk(K, 1)]),                               // (수치)
      O([stk(M, 1, E1), dmg(0.7, E1), form(FF)]),                                  // 골라 찍고 초 집중
      O([stk(M, 1, LOW), st('약점 노출', 1, LOW)]),  // 특종 사진(약점 노출)
      O([stk(M, 1, LOW), dmg(0.5, LOW), draw(1)]),                                 // 다음 취재 준비
      O([stk(M, 1, TOP), dmg(0.6, TOP), stk(K, 2)]),                               // 거물 특종
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u4`, '셔터 연사', 1, '공격', [per(K, { n: 1 }), dmg(0.4, 'markedEnemy'), spendAll(K)], [ // 결정타 — 모은 셔터를 특종감에
      O([per(K, { n: 1 }), dmg(0.4, 'markedEnemy'), spendAll(K)]),                 // (수치)
      O([per(K, { n: 1 }), dmg(0.28, EA), spendAll(K)]),                            // 현장 전체 스케치
      O([per(K, { n: 1 }), dmg(0.35, 'markedEnemy'), spendN(K, 2)]),                // 두 장만 남기기
      O([per(K, { n: 1 }), dmg(0.32, 'markedEnemy'), spendAll(K), ifKill, gauge(25)]), // 특종! 처치하면 고학년
      O([dmg(0.6, 'markedEnemy'), form(FF)]),  // 연사하며 초 집중
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '셀카 한 장', 0, '스킬', [draw(1), stk(K, 1)], [                                 // 원작 자유 — 셀카만 찍다 정직당한 기자
      O([draw(1), stk(K, 1)]),                                                     // (수치)
      O([draw(1), dmod(0.15, OTHERS)]),                                            // 팀 단체 셀카(아군 피해)
      O([stk(K, 2), st('회피', 1)]),                                               // 상자 속 셀카
      O([pw('kill', [draw(1), stk(K, 1)])], { power: true }),  // 특종 셀카(처치마다)
      O([draw(2), st('회피', 1)]),  // 기사 마감
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: M, desc: '하이디가 찍은 오늘의 특종', carrier: 'enemy', cap: 1, hunt: true, per: [{ stat: 'taken', v: 0.25, from: 'owner' }],
      rules: [{ name: '다음 특종', when: { on: 'huntDown' }, fx: [stk(M, 1, LOW), stk(K, 1)] }] },
    keywords: [{ name: K, desc: '숨어서 모은 셔터', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.05 }] }],
    passives: [
      pas('목표 포착!', 'fightStart', [stk(M, 1, LOW)]),
      pas('목표 포착!', 'hit', [ifStack(M), stk(K, 1)], { when: { who: 'other' } }),
    ],
    forms: [
      { id: FI, name: '잠입 취재 중', desc: '상자에 숨은 하이디 — 플래시가 다연타로', turns: 2, mods: { taken: -0.2 }, cards: { [`${H}_s1`]: `${H}_f1` }, skin: null, anim: null },
      { id: FF, name: '초 집중 취재', desc: '특종감만 쫓는 하이디 — 셔터가 특종감에 고정', turns: 2, mods: { dealt: 0.15 }, cards: { [`${H}_s1`]: `${H}_f2` }, skin: null, anim: null },
    ],
    ult: { name: '현장 중계 중!', cost: 200, fx: [stk(M, 1, LOW), form(FF), dmg(2.8, LOW), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`],
    basics, tokens, uniques,
  });
}

// ── 죠안 — 두 얼굴형 · 서포터 ──
// 엔진: 말 한마디로 빵을 지어 동료 손에 쥐여 준다(한 턴에 둘째부터는 죠안 손에 현기증) → 꿈결의 계시로 꿈결 형상(3턴 — 사슬이 광역 + 파티 회복)
function joan(j) {
  // 팀: 빵은 늘 동료의 카드(주인: 다음 아군)로 — 그 동료의 방어력으로 회복 · 실드
  // 원작: 「빵」 이라고 말하면 빵이 생김 · 권능을 자주 쓰면 현기증 · 저학년 「교리를 행하고」(분산 · 공격력↑) · 고학년 「제가 목도한 것을」(꿈결 형상 · 아군 보호막) · 오병이어
  const H = '죠안', K = '반죽', F = '죠안_꿈결', BR = '죠안_bread', DZ = '죠안_dizzy';
  const basics = [
    S(`${H}_s1`, '경건한 일격', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '갓 구운 빵', 1, '스킬', [heal(1.5), make(BR, 1, { owner: 'other' })]),
  ];
  const tokens = [
    T(`${H}_f1`, '꿈결의 사슬', H, '공격', [dmg(0.6, EA), heal(0.45)], { cost: 1, blurb: '꿈결 형상의 사슬입니다' }),
    T(BR, '빵', H, '스킬', [heal(0.7), sh(0.6)], { tags: ['소멸'], blurb: '「빵」 한마디에 생긴 빵입니다' }),
    T(DZ, '현기증', H, '상태', [{ k: 'when', on: 'handEnd' }, { k: 'nextAp', v: -1 }], { tags: ['사용 불가', '증발'], blurb: '권능을 너무 써서 어지럽습니다' }),
  ];
  const BK = [make(BR, 1, { owner: 'other' })];
  const uniques = [
    U(`${H}_u1`, '빵이 있으라', 0, '스킬', [make(BR, 1, { owner: 'other' }), stk(K, 1), ifStack(K, 2), make(DZ, 1)], [ // 시동 — 「빵」 한마디
      O([make(BR, 1, { owner: 'other' }), stk(K, 1), ifStack(K, 2), make(DZ, 1)]), // (수치)
      O([make(BR, 1, { owner: 'other' }), draw(1)]),  // 천천히(현기증 없이)
      O([make(BR, 2, { owner: 'other' }), make(DZ, 1)]),                             // 한꺼번에 둘
      O([pw('turnStart', [make(BR, 1, { owner: 'other' })])], { power: true }),  // 새벽 빵
      O([make(BR, 1), stk(K, 1), ifStack(K, 2), make(DZ, 1)]),                       // 내 몫 빵
    ], null),
    U(`${H}_u3`, '오병이어', 1, '스킬', [make(BR, 2, { owner: 'other' }), make(DZ, 1)], [        // 연료 — 빵을 늘리던 옛 권능
      O([make(BR, 3, { owner: 'other' }), make(DZ, 1)]),                             // (수치에 가까움)
      O([make(BR, 2, { owner: 'other' }), sh(0.5)]),                                // 현기증을 참고
      O([make(BR, 1, { owner: 'other' }), draw(1), pw('make', [heal(0.12)])], { power: true }),  // 빵을 지을 때마다 회복(분수의 딸기주스)
      O([make(BR, 2, { owner: 'other' }), { k: 'exileFrom', from: 'hand', all: true, tag: DZ }]), // 현기증 털기
      O([make(BR, 2, { owner: 'other' }), make(DZ, 1), draw(1)]),                    // 빵집 흑자
    ], [B('draw'), B('heal'), B(BK)]),
    U(`${H}_u2`, '꿈결의 계시', 1, '스킬', [dmg(0.5, EA), sh(1.0), form(F)], [                 // 결정타 — 원작 고학년(꿈결 형상 · 아군 보호막)
      O([dmg(0.5, EA), sh(1.0), form(F)]),                                          // (수치)
      O([sh(1.3), form(F)], { cost: 0 }),                                           // 조용한 계시
      O([dmg(0.9, E1), form(F), { k: 'exileFrom', from: 'hand', all: true, tag: DZ }]), // 계시가 현기증을 걷는다
      O([dmg(0.4, EA), sh(0.8), st('사기', 1)]),                                    // 목도한 것을(사기)
      O([dmg(0.4, EA), form(F), pw('turnStart', [sh(0.3)])], { power: true }),      // 계시의 날
    ], [B('power'), B('guard'), B(BK)]),
    U(`${H}_u4`, '교리를 행하고', 1, '스킬', [st('피해 감소', 2), dmod(0.15, ALLIES, { turns: 2 })], [ // 원작 자유 — 저학년(분산 · 공격력↑)
      O([st('피해 감소', 2), dmod(0.15, ALLIES, { turns: 2 })]),                    // (수치)
      O([st('피해 감소', 1), make(BR, 1, { owner: 'other' }), draw(1)]),             // 교리와 빵
      O([st('피해 감소', 2), pw('make', [sh(0.3)])], { cost: 0, power: true }),  // 지을 때마다 지켜 주기
      O([dmod(0.25, 'strongestAlly', { turns: 2 }), st('피해 감소', 1), make(BR, 1, { owner: 'other' })]),  // 가장 센 아군에게
      O([st('피해 감소', 3)], { cost: 0 }),                                          // 분산만
    ], [B('guard'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '이번 턴에 지은 빵의 수', carrier: 'self', cap: 3, endClear: true },
    passives: [
      pas('빵 짓는 사제', 'make', [sh(0.15)]),
      pas('주교의 경전', 'lowHp', [st('피해 감소', 2), heal(1.5)], { when: { pct: 0.3 } }),
    ],
    forms: [{ id: F, name: '꿈결 형상', desc: '꿈결에 든 죠안 — 사슬이 적 전체를 치고 파티를 회복', turns: 3, cards: { [`${H}_s1`]: `${H}_f1` }, skin: null, anim: null }],
    ult: { name: '제가 목도한 것을', cost: 250, fx: [dmg(0.8, EA), sh(3.0), st('사기', 2), form(F)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, tokens, uniques,
  });
}

// ── 클로에 — 두 얼굴형 · 탱커 ──
// 엔진: 세바스티안에 올라타(3턴 — 앞발이 3연타 휩쓸기, 받는 피해↓) 막으며 바늘땀을 모으고 → 다 차면 천 조각 → 「재단 가위질」로 쥔 천 조각만큼 자르거나 실드로 쥔다
function chloe(j) {
  // 팀: 파티 실드가 깨질 때마다 바늘땀(누구의 실드든 — 찢기면 고친다)
  // 원작: 저학년 「메리 고 라운드」(세바스티안 탑승 · 보호막) · 고학년 「쁘띠 세바스티안」(7마리 자폭) · 세바스티안이 찢기면 고침 · 재단사 · 어사이드 F/W 클로에록(맞으면 고학년 앞당김)
  const H = '클로에', K = '바늘땀', F = '클로에_탑승', CL = '클로에_cloth', SB = '클로에_sebas';
  const basics = [
    S(`${H}_s1`, '세바스티안 앞발', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '원단 두르기', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const tokens = [
    T(CL, '천 조각', H, '스킬', [sh(0.5), stk(K, 1)], { tags: ['소멸'], blurb: '세바스티안을 고칠 천 조각입니다' }),
    T(SB, '쁘띠 세바스티안', H, '공격', [dmg(0.5, ER, { hits: 2 })], { tags: ['소멸'], blurb: '작은 세바스티안이 부딪쳐 터집니다' }),
    T(`${H}_f1`, '세바스티안 휩쓸기', H, '공격', [dmg(0.3, E1, { hits: 3 }), dmg(0.3, EA)], { cost: 1, blurb: '탑승한 세바스티안의 연타입니다' }),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '메리 고 라운드', 1, '스킬', [sh(1.1), stk(K, 1), form(F)], [                 // 시동 — 원작 저학년(탑승 · 보호막)
      O([sh(1.1), stk(K, 1), form(F)]),                                             // (수치)
      O([sh(0.8), form(F), draw(1)], { cost: 0 }),                                  // 훌쩍 올라타기
      O([sh(0.8), form(F), pw('turnStart', [stk(K, 1)])], { power: true }),         // 매일 타는 인형
      O([sh(1.4), st('피해 감소', 1)], { cost: 0 }),  // 타지 않고 막기
      O([sh(0.9), form(F), make(SB, 1)]),                                           // 쁘띠도 같이
    ], null),
    U(`${H}_u1`, '세바스티안 연타', 1, '공격', [dmg(0.3, E1, { hits: 3 }), stk(K, 1)], [        // 연료 — 탑승 평타(3연타)
      O([dmg(0.3, E1, { hits: 3 }), stk(K, 1)]),                                    // (수치)
      O([dmg(0.22, EA, { hits: 3 })]),                                              // 큰 휩쓸기
      O([dmg(0.3, E1, { hits: 3 }), sh(0.5)]),                                      // 치면서 막기
      O([dmg(0.25, E1, { hits: 3 }), pw('play', [stk(K, 1)], { when: { type: '공격' } })], { power: true }), // 연타마다 바늘땀
      O([dmg(0.32, E1, { hits: 3 }), st('취약', 1, E1), stk(K, 1)]),  // 두 팔 내려찍기(도발 대신 취약)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u5`, '재단 가위질', 1, '공격', [dmg(0.4, E1), perTag(CL), dmg(0.45, E1)], [ // 결정타 — 쥔 천 조각만큼 자르기
      O([dmg(0.4, E1), perTag(CL), dmg(0.45, E1)]),  // (수치)
      O([perTag(CL), dmg(0.3, EA), exileTag(CL)]),                                   // 넓게 재단
      O([perTag(CL), sh(0.6), exileTag(CL)], { cost: 0 }),  // 덧대기(실드)
      O([perTag(CL), dmg(0.6, E1), exileTag(CL)]),  // 천을 다 써서 자르기
      O([perTag(CL), dmg(0.4, E1), exileTag(CL), stk(K, 2)]),                        // 자르고 다시 꿰매기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '찢긴 곳 꿰매기', 1, '스킬', [stk(K, 1), ifShieldNot, sh(1.3)], [                 // 원작 자유 — 세바스티안이 찢기면 고친다
      O([stk(K, 1), ifShieldNot, sh(1.3)]),                                          // (수치)
      O([make(CL, 2), sh(0.4)]),                                                     // 천 조각 꺼내기
      O([sh(0.6), pw('shieldBreak', [sh(0.4)])], { cost: 0, power: true }),  // 늘 바느질 중
      O([heal(0.7), stk(K, 2)]),                                                     // 수선 끝
      O([stk(K, 1), ifShieldNot, sh(1.0), gauge(15)]),                               // F/W 클로에록(고학년 앞당김)
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '세바스티안을 고치는 바늘땀', carrier: 'self', cap: 3, onMax: { make: CL, consume: true } },
    passives: [
      pas('재단사의 손', 'shieldBreak', [stk(K, 1)]),
      pas('셀러브리티 클로에', 'lowHp', [st('피해 감소', 2), make(SB, 2)], { when: { pct: 0.3 } }),
    ],
    forms: [{ id: F, name: '인형의 의지', desc: '세바스티안에 올라탄 클로에 — 앞발이 3연타 휩쓸기, 받는 피해 감소', turns: 3, mods: { taken: -0.15 }, cards: { [`${H}_s1`]: `${H}_f1` }, skin: null, anim: null }],
    ult: { name: '쁘띠 세바스티안', cost: 300, fx: [make(SB, 3), st('둔화', 1, EA), sh(1.0)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`],
    basics, tokens, uniques,
  });
}

// ── 네티 — 두 얼굴형 · 탱커 ──
// 엔진: 드릴질(공격)마다 발굴 자루가 차고 다섯이면 광물(회복 · 피해 감소 · 아군 피해 중 하나) → 「기가 드릴 차지」로 자루를 털어 집착(2턴 — 곡괭이가 거대 드릴)
function netty(j) {
  // 팀: 캐낸 광물은 셋 중 하나 — 신비한 광물이면 이번 턴 아군 주는 피해 +15%(어사이드 「광물을 아군에게 나눔」)
  // 원작: 평타 드릴 3연타 → 무작위 광물 2개(반짝 회복 · 단단 피해 감소 · 신비 피해 증가) · 고학년 「기가 드릴 차지」(집착 — 거대 드릴 8연타) · 저학년 「낙석 조심」(아군 보호막) · 꼬리 자력 퍼즐
  const H = '네티', K = '발굴 자루', F = '네티_집착', ORE = '네티_ore';
  const basics = [
    S(`${H}_s1`, '드릴 돌리기', 1, '공격', [dmg(0.9, E1), stk(K, 1)]),
    S(`${H}_s2`, '안전모', 1, '스킬', [sh(1.6), stk(K, 1)]),
  ];
  const tokens = [
    T(ORE, '캐낸 광물', H, '스킬', [roll(3), ifRoll(1), heal(0.5), ifRoll(2), st('피해 감소', 1), ifRoll(3), dmod(0.15, ALLIES)], { tags: ['소멸'], blurb: '반짝이는 광물, 단단한 광물, 신비한 광물 가운데 하나입니다' }),
    T(`${H}_f1`, '거대 드릴', H, '공격', [dmg(0.18, EA, { hits: 4 }), stk(K, 1)], { cost: 1, blurb: '집착한 네티의 거대 드릴입니다' }),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '낙석 조심', 1, '스킬', [sh(1.3), stk(K, 2)], [                                 // 시동 — 원작 저학년(아군 보호막 · 방어력↑)
      O([sh(1.3), stk(K, 2)]),                                                       // (수치)
      O([sh(0.8), st('결정화', 1), stk(K, 1)]),  // 단단한 지층(결정화)
      O([sh(0.9), make(ORE, 1)]),                                                    // 낙석에서 광물
      O([sh(0.7), pw('turnStart', [stk(K, 1)])], { power: true }),                   // 발굴 일정표
      O([sh(1.0), formEnd, draw(1)]),                                                // 유물은 나중에(집착 풀기)
    ], null),
    U(`${H}_u2`, '드릴 굴착', 1, '공격', [dmg(0.28, E1, { hits: 3 }), stk(K, 1)], [                // 연료 — 평타(드릴 3연타)
      O([dmg(0.28, E1, { hits: 3 }), stk(K, 1)]),                                    // (수치)
      O([dmg(0.2, EA, { hits: 3 })]),                                                // 넓게 파기
      O([dmg(0.25, E1, { hits: 3 }), make(ORE, 1)]),                                  // 바로 광물
      O([dmg(0.22, E1, { hits: 3 }), tough(1)]),                                     // 암반 뚫기(강인도)
      O([dmg(0.2, E1, { hits: 3 }), pw('play', [stk(K, 1)], { when: { type: '공격' } })], { power: true }), // 드릴 연습
    ], [B('power'), B('weakSpot'), B(BK)], ['분쇄']),
    U(`${H}_u3`, '기가 드릴 차지', 2, '공격', [per(K, { n: 1 }), dmg(0.18, EA), form(F)], [ // 결정타 — 원작 고학년(집착 · 8연타)
      O([per(K, { n: 1 }), dmg(0.18, EA), form(F)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.4, E1), form(F)]),  // 한 지점 굴착
      O([form(F), make(ORE, 2)], { cost: 1 }),                                       // 집착만(광물 둘)
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)]),  // 자루 털기
      O([per(K, { n: 1 }), dmg(0.15, EA), st('피해 감소', 2)], { cost: 1 }),  // 행동 불가 면역(애착)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '꼬리 열쇠', 0, '스킬', [draw(1, { who: 'self', unique: true }), stk(K, 1)], [     // 원작 자유 — 꼬리의 자력으로 푼 유적 퍼즐
      O([draw(1, { who: 'self', unique: true }), stk(K, 1)]),                        // (수치)
      O([draw(2), disc(1)]),                                                          // 돌은 버리고
      O([make(ORE, 1), stk(K, 1)]),                                                   // 자성 광물
      O([sh(0.6), stk(K, 2)]),  // 자성 꼬리 방패
      O([draw(1, { who: 'self', unique: true }), gauge(20)]),                         // 초정밀 탐사
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '캐낸 것을 담아 두는 자루', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.06 }], onMax: { make: ORE, consume: true } },
    passives: [
      pas('드릴 굴착', 'play', [stk(K, 1)], { when: { type: '공격' } }),
      pas('너로 정했다! 초정밀 탐사!', 'shieldBreak', [st('피해 감소', 1), make(ORE, 1)]),
    ],
    forms: [{ id: F, name: '유물 집착', desc: '유물에 집착한 네티 — 손 드릴이 거대 드릴로', turns: 2, mods: { dealt: 0.15 }, cards: { [`${H}_s1`]: `${H}_f1` }, skin: null, anim: null }],
    ult: { name: '기가 드릴 차지', cost: 200, fx: [dmg(0.44, EA, { hits: 4 }), stk(K, 3), form(F)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, tokens, uniques,
  });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '흔들린 지층', when: { on: 'hurt', guarded: true }, fx: [stk(K, 1)] }];
}

// ── 리츠 — 두 얼굴형 · 딜러 ──
// 엔진: 소심하게 막다가 맞을 때마다 정당방위가 쌓이고 → 셋이면 버튼이 눌려 광전(2턴 — 주는 피해↑) → 「박살내주겠어!」로 정당방위를 다 쏟고 소심으로 돌아온다
function reets(j) {
  // 팀: 소심할 때 파티 받는 피해 -10%(참는 자세) — 동료를 대신해 참는다
  // 원작: 「두 얼굴의 전사」 · 「버튼 눌리면 못 막음」(먼저 맞으면) · 저학년 「담금질」(집중 — 아군이 맞을 때마다 피해 · 피해 감소 쌓임) · 고학년 「벼리기」(3베기 뒤 반동) · 어사이드 「한 놈만 팬다」
  const H = '리츠', K = '정당방위', F = '리츠_광전';
  const basics = [
    S(`${H}_s1`, '칼등 치기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '강철 자세', 1, '스킬', [sh(1.5), stk(K, 1)]),
  ];
  const tokens = [T(`${H}_f1`, '대검 내려치기', H, '공격', [dmg(0.75, EA), stk(K, 1)], { cost: 1, blurb: '버튼이 눌린 리츠의 대검입니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '담금질', 1, '스킬', [sh(1.0), st('피해 감소', 1), stk(K, 1)], [                // 시동 — 원작 저학년(집중 · 아군이 맞을 때마다)
      O([sh(1.0), st('피해 감소', 1), stk(K, 1)]),                                    // (수치)
      O([sh(0.7), pw('hurt', [stk(K, 1), st('피해 감소', 1)], { when: { guarded: true } })], { power: true }), // 집중 상태(매번)
      O([sh(0.8), stk(K, 2)], { cost: 0 }),                                           // 짧은 담금질
      O([sh(0.8), st('피해 감소', 1), later(1, [dmg(0.6, EA)])]),                     // 집중이 끝나면 광역(원작)
      O([sh(1.2), form(F)]),                                                          // 담금질 끝 — 바로 광전
    ], null),
    U(`${H}_u5`, '통곡의 벽', 1, '스킬', [sh(1.3), stk(K, 1), draw(1)], [                         // 연료 — 소심한 얼굴
      O([sh(1.3), stk(K, 1), draw(1)]),                                               // (수치)
      O([sh(0.9), st('반격', 2)]),                                                    // 벽에 부딪혀라(반격)
      O([sh(1.0), stk(K, 2)]),                                                        // 이 악물고 참기
      O([sh(0.8), st('약화', 1, E1), stk(K, 1)]), // 기세 꺾기(약화)
      O([sh(1.0), pw('turnStart', [sh(0.3)])], { cost: 0, power: true }),  // 소심한 일과
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u1`, '박살내주겠어!', 1, '공격', [per(K, { n: 1 }), dmg(0.45, E1), spendAll(K), formEnd], [ // 결정타 — 광전의 한 방, 그리고 「간신히 이겼다」
      O([per(K, { n: 1 }), dmg(0.45, E1), spendAll(K), formEnd]),                     // (수치)
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), formEnd]),                      // 대검 휘두르기(광역)
      O([per(K, { n: 1 }), dmg(0.4, E1), formEnd]),                                   // 화는 남긴다
      O([per(K, { n: 1 }), dmg(0.42, E1), spendAll(K)]),                              // 광전 유지
      O([per(K, { n: 1 }), dmg(0.38, E1), spendAll(K), tough(2)]),                    // 펀치 머신 박살(강인도)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '한 놈만 패기', 1, '공격', [dmg(0.8, E1), st('취약', 1, E1), st('피해 감소', 1)], [ // 원작 자유 — 어사이드(노린 적에게 피해↑ · 다른 적에게 받는 피해↓)
      O([dmg(0.8, E1), st('취약', 1, E1), st('피해 감소', 1)]),                       // (수치)
      O([dmg(0.6, E1), stk(K, 2)]),                                                   // 먼저 친 놈
      O([dmg(1.3, E1), tough(1)], { cost: 2 }),                                       // 비비 샌드백
      O([dmg(0.6, E1), pw('hurt', [dmg(0.25, E1)], { when: { guarded: true } })], { power: true }), // 맞으면 그놈만
      O([dmg(0.7, E1), form(F)]),                                                     // 버튼 눌림
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '먼저 맞아서 생긴 정당한 이유', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.08 }] },
    passives: [
      pas('먼저 손대셨죠?', 'hurt', [stk(K, 1), ifStack(K, 3), form(F)]),
      pas('참는 자세', 'always', [{ k: 'takenMod', v: -0.1, target: 'party' }]),
    ],
    forms: [{ id: F, name: '광전', desc: '버튼이 눌린 리츠 — 대검을 휘두르고 주는 피해가 오름', turns: 2, mods: { dealt: 0.2, taken: 0.1 }, cards: { [`${H}_s1`]: `${H}_f1` }, skin: null, anim: null }],
    ult: { name: '벼리기', cost: 250, fx: [dmg(0.3, EA, { hits: 3 }), stk(K, 2), formEnd] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`],
    basics, tokens, uniques,
  });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '먼저 치셨으니까', when: { on: 'hurt', guarded: true }, fx: [stk(K, 1)] }];
}

// ── 스노키 — 버티형 · 탱커 ──
// 엔진: 두유 막(실드)을 씌우고 동료가 칠 때마다 두유를 배달 → 막이 깨지면 그 적에게 취약(불법 유통망) → 밀두유를 모아 두유 상자를 던지거나 다 차면 파티 다음 카드 +50%
function snoky(j) {
  // 팀: 다른 아군이 공격 카드를 낼 때마다 실드 · 밀두유(두유 배달) · 의리의 대명사(가장 센 아군 피해↑)
  // 원작: 저학년 「불법 두유」(전열 아군 실드 — 깨지면 주변 적 받는 피해↑) · 고학년 「구역 점거」 · 어사이드 「의리의 대명사」(가장 센 아군에게 HP · 전원 보호막) · 무저갱 탈출 · 두유 밀매 두목
  const H = '스노키', K = '밀두유';
  const basics = [
    S(`${H}_s1`, '절도 있는 발차기', 1, '공격', [dmg(0.22, E1, { hits: 3 })]),
    S(`${H}_s2`, '경호 자세', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '경호원 출동', 1, '강화', [stk(K, 1), sh(0.5), pw('turnStart', [sh(0.35)])], [   // 시동 — 교주의 경호원
      O([stk(K, 1), sh(0.5), pw('turnStart', [sh(0.35)])]),                           // (수치)
      O([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0 }),                      // 매일 두유 입고
      O([sh(0.5), stk(K, 1), pw('shieldBreak', [st('취약', 1, E1), stk(K, 1)])]),  // 막 깨지면 응징
      O([stk(K, 2), pw('play', [sh(0.15)], { when: { type: '공격', who: 'other' } })]), // 배달 루트
      O([st('피해 감소', 1), pw('turnStart', [sh(0.3)])], { cost: 0 }),  // 주먹 맛 좀 봐라(어사이드)
    ], null, ['개전']),
    U(`${H}_u2`, '불법 두유', 1, '스킬', [sh(1.1), stk(K, 1), st('실드 유지', 1)], [               // 연료 — 원작 저학년(아군 실드)
      O([sh(1.1), stk(K, 1), st('실드 유지', 1)]),                                    // (수치)
      O([sh(0.9), stk(K, 2)]),                                                        // 두유 두 팩
      O([sh(0.8), stk(K, 1), st('취약', 1, EA)]),  // 깨질 걸 알고(적 취약)
      O([sh(0.8), stk(K, 1), draw(1)], { cost: 0 }),                                  // 몰래 한 모금
      O([sh(0.7), pw('blocked', [stk(K, 1)])], { power: true }),                      // 다 막으면 한 팩 더
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u3`, '두유 상자 던지기', 1, '공격', [per(K, { n: 1 }), ddef(0.25, EA), spendAll(K)], [  // 결정타 — 모은 두유를 던진다
      O([per(K, { n: 1 }), ddef(0.25, EA), spendAll(K)]),                             // (수치)
      O([per(K, { n: 1 }), ddef(0.5, E1), spendAll(K)]),                              // 한 놈에게 상자째
      O([per(K, { n: 1 }), ddef(0.2, EA), spendN(K, 2)]),                             // 두 팩은 남긴다
      O([per(K, { n: 1 }), ddef(0.2, EA), spendAll(K), st('둔화', 1, EA)]),          // 구역 점거(둔화)
      O([per(K, { n: 1 }), sh(0.4), spendAll(K), st('반격', 1)]),                    // 상자를 방패로
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '의리의 대명사', 1, '스킬', [sh(0.9), dmod(0.25, 'strongestAlly', { turns: 2 }), stk(K, 1)], [ // 원작 자유 — 어사이드(가장 센 아군에게 · 전원 보호막)
      O([sh(0.9), dmod(0.25, 'strongestAlly', { turns: 2 }), stk(K, 1)]),             // (수치)
      O([sh(0.8), st('피해 감소', 2), stk(K, 1)]),  // 전원 보호막
      O([heal(0.7), stk(K, 2), draw(1)]),  // 무저갱 탈출(회복)
      O([sh(0.5), pw('turnStart', [dmod(0.15, 'strongestAlly')])], { cost: 0, power: true }),  // 오른팔의 의리
      O([sh(0.7), empower(0.4, 'any'), stk(K, 1)]),  // 빅보스의 한마디
    ], [B('guard'), B('heal'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '조직에서 빼돌린 두유', carrier: 'self', cap: 3, onMax: { empower: 'any', ratio: 0.5, consume: true } },
    passives: [
      pas('두유 배달', 'play', [sh(0.2), stk(K, 1)], { when: { type: '공격', who: 'other' } }),
      pas('불법 유통망', 'shieldBreak', [st('취약', 1, E1)]),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, uniques,
  });
}

// ── 리온 — 버티형 · 탱커 ──
// 엔진: 판결봉으로 유죄(적이 주는 피해↓, 3겹)를 쌓아 적의 주먹을 무디게 하고 → 유죄 3겹 적에게 도장 낙하 → 동료가 유죄인 적을 칠 때마다 파티 회복
function lion(j) {
  // 팀: 다른 아군이 유죄인 적을 치면 파티 회복(히어로 꿈나무 — 원작 「아군 최대 HP↑」)
  // 원작: 저학년 「유죄 선언」(앞의 적 심판 3중첩 · 잃은 HP 회복) · 3타째 강화 평타 범위 심판 · 3중첩 적에 도장 · 고학년 「즉결심판」(보호막 깨기) · 자경단 히어로 스핑크스
  const H = '리온', K = '유죄';
  const basics = [
    S(`${H}_s1`, '판결봉 휘두르기', 1, '공격', [dmg(0.9, E1), stk(K, 1, E1)]),
    S(`${H}_s2`, '히어로 가드', 1, '스킬', [sh(1.6), stk(K, 1, E1)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '유죄 선언', 1, '스킬', [stk(K, 1, EA), sh(0.9), { k: 'ifWounded' }, heal(0.7)], [ // 시동 — 원작 저학년
      O([stk(K, 1, EA), sh(0.9), { k: 'ifWounded' }, heal(0.7)]),                    // (수치)
      O([stk(K, 2, E1), sh(0.8)]),                                                    // 한 사람에게 중형
      O([stk(K, 1, EA), heal(0.6)]),                                                  // 판결 뒤 자기 회복
      O([stk(K, 1, EA), pw('turnStart', [stk(K, 1, ER)])], { power: true }),          // 매 턴 순찰
      O([stk(K, 1, EA), sh(0.6), draw(1)], { cost: 0 }),                              // 약식 재판
    ], null),
    U(`${H}_u2`, '이의 있음!', 1, '스킬', [sh(1.1), stk(K, 1, E1), draw(1)], [                       // 연료 — 수수께끼를 좋아하는 모범생
      O([sh(1.1), stk(K, 1, E1), draw(1)]),                                           // (수치)
      O([sh(0.8), pw('blocked', [stk(K, 1, E1)])], { power: true }),                  // 막을 때마다 판결
      O([sh(0.9), st('반격', 2)]),                                                    // 정당방위
      O([stk(K, 2, E1), draw(1)], { cost: 0 }),                                       // 수수께끼 하나 더
      O([sh(0.9), stk(K, 1, EA)]),                                                    // 집단 소송
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u3`, '도장 낙하', 1, '공격', [per(K, { n: 1 }), ddef(0.3, E1), spendAll(K)], [            // 결정타 — 3중첩 적에 도장
      O([per(K, { n: 1 }), ddef(0.3, E1), spendAll(K)]),                              // (수치)
      O([per(K, { n: 1, each: true }), ddef(0.2, EA), spendAll(K)]),                  // 판결 일괄 집행
      O([per(K, { n: 1 }), ddef(0.25, E1)]),                                          // 집행유예(유죄는 둔다)
      O([per(K, { n: 1 }), ddef(0.25, E1), { k: 'strip', target: E1 }]),  // 즉결심판(보호막 깨기)
      O([per(K, { n: 1 }), ddef(0.22, E1), heal(0.5)]),  // 가중처벌 대신 선처
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '라이온 시그널', 1, '강화', [st('결의', 1), pw('blocked', [stk(K, 1, E1)])], [         // 원작 자유 — 애착 아티팩트(스핑크스 호출 조명)
      O([st('결의', 1), pw('blocked', [stk(K, 1, E1)])]),                              // (수치)
      O([st('결의', 1), pw('turnStart', [stk(K, 1, TOP)])]),                           // 밤하늘의 조명
      O([sh(0.6), pw('hurt', [heal(0.12)])], { cost: 0 }),  // 히어로는 쓰러지지 않는다
      O([pw('blocked', [stk(K, 1, E1), sh(0.15)])], { cost: 0 }),                       // 범죄자들의 공휴일
      O([st('결의', 1), pw('kill', [heal(0.4)])], { cost: 0 }),  // 사건 해결
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '리온이 내린 판결', carrier: 'enemy', cap: 3, per: [{ stat: 'dealt', v: -0.07 }] },
    passives: [
      pas('판결봉', 'play', [stk(K, 1, E1)], { when: { type: '공격' } }),
      pas('히어로 꿈나무', 'hit', [ifStack(K), heal(0.12)], { when: { who: 'other' } }),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, uniques,
  });
}

// ── 이드 — 버티형 · 탱커 ──
// 엔진: 꿈결(끈기)로 쓰러짐을 한 번 막아 두고 → 맞을 때마다 나타 경보가 울려 다섯이면 다음 카드 +60% → 「나타로 들이받기」로 경보를 터뜨린다
function ide(j) {
  // 팀: 「착한 마음씨 이드」 — 아군 주는 피해↑(언니 취미를 같이 해 주는 막내)
  // 원작: 저학년 「흐릿한 경계」(아군 전투불능 무효 · 최대 2번) · 강화 평타(직접 피해 5회마다 반격 + 회복) · 고학년 「너와 나의 우주」 · 생명 유지 장치 나타 · 꿈이라 믿는 막내
  const H = '이드', K = '나타 경보';
  const basics = [
    S(`${H}_s1`, '몽중탄', 1, '공격', [dmg(0.22, E1, { hits: 3 })]),
    S(`${H}_s2`, '나타 격벽', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '흐릿한 경계', 1, '강화', [st('끈기', 1), sh(0.6), pw('endure', [sh(1.0), stk(K, 2)])], [ // 시동 — 원작 저학년(전투불능 무효)
      O([st('끈기', 1), sh(0.6), pw('endure', [sh(1.0), stk(K, 2)])]),                // (수치)
      O([st('끈기', 1), pw('endure', [heal(0.8)])], { cost: 0 }),                      // 가벼운 경계
      O([st('끈기', 1), pw('hurt', [stk(K, 1)])]),                                     // 꿈결의 경보
      O([st('끈기', 2), sh(1.1), pw('endure', [sh(1.1)])], { cost: 2 }),  // 두 번 막기(원작 최대 2번)
      O([st('끈기', 1), sh(0.4), pw('endure', [st('반격', 3), stk(K, 2)])]),  // 깨어나며 되받기
    ], null, ['개전']),
    U(`${H}_u2`, '전기로 지져 줘', 1, '스킬', [st('반격', 1), stk(K, 1), draw(1)], [                   // 연료 — 캐시가 지지면 꿈속으로
      O([st('반격', 1), stk(K, 1), draw(1)]),                                          // (수치)
      O([st('반격', 2), sh(0.6)]),                                                     // 꽉 지지기
      O([stk(K, 2), st('충격', 1, E1)]),                                               // 감전(어사이드)
      O([st('반격', 1), pw('turnStart', [stk(K, 1)])], { power: true }),              // 매일 아침 전기
      O([st('반격', 1), draw(2)], { cost: 0 }),                                        // 살짝 지지기
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u5`, '나타로 들이받기', 1, '공격', [per(K, { n: 1 }), ddef(0.25, E1), spendAll(K)], [       // 결정타 — 나타가 되받아친다
      O([per(K, { n: 1 }), ddef(0.25, E1), spendAll(K)]),                              // (수치)
      O([per(K, { n: 1 }), ddef(0.17, EA), spendAll(K)]),                              // 레이저 4회(광역)
      O([per(K, { n: 1 }), ddef(0.2, E1), heal(0.3)]),  // 되받고 회복(원작 강화 평타)
      O([per(K, { n: 1 }), ddef(0.2, E1), spendN(K, 2)]),                              // 경보는 남긴다
      O([per(K, { n: 1 }), ddef(0.2, E1), spendAll(K), st('약화', 2, E1)]),           // 맞은 적은 약해진다
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '착한 마음씨 이드', 1, '스킬', [sh(0.7), dmod(0.15, OTHERS), stk(K, 1)], [              // 원작 자유 — 어사이드(언니 취미를 함께)
      O([sh(0.7), dmod(0.15, OTHERS), stk(K, 1)]),                                     // (수치)
      O([sh(0.6), empower(0.4, 'any')]),                                               // 언니 먼저
      O([heal(0.8), stk(K, 1)]),                                                       // 함께 꾸는 꿈(회복)
      O([sh(0.5), pw('turnStart', [dmod(0.1, OTHERS)])], { cost: 0, power: true }),  // 매일 같이 놀기
      O([sh(0.6), draw(1, { who: 'other' }), stk(K, 1)]),                              // 언니 카드 챙기기
    ], [B('guard'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '꿈을 깨우려 울리는 나타의 경보', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.03 }], onMax: { empower: 'next', ratio: 0.6, consume: true } },
    passives: [
      pas('함께 꾸는 꿈', 'hurt', [stk(K, 1)]),
      pas('꿈에서 깨지 않은', 'fightStart', [st('피해 감소', 1), stk(K, 1)]),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`],
    basics, uniques,
  });
}

// ── 헤일리(멀쩡) — 버티형 · 탱커 ──
// 엔진: 공격을 막아 낼 때마다 극복의 훈장 → 훈장은 받는 피해를 줄이고 → 「극복의 기록」으로 훈장을 써서 동료의 족쇄(해로운 효과)를 대신 끊는다. 넷이면 파티 다음 카드 +40%
function haileyOk(j) {
  // 팀: 매 턴 파티 디버프 1 해제(잊을 수 없는 기억) · 극복의 기록(훈장 → 해제 · 실드)
  // 원작: 저학년 「나아가는 결의」(범위 · 자기 회복 · 맞힌 적마다 훈장) · 훈장(받는 피해↓ · 아군 기절 · 변이 해제) · 강화 평타(넷째 공격 검법 · 도발) · 고학년 「전함 호출」 · 블랑셰 앞 무릎 꿇는 교섭
  const H = '헤일리_멀쩡', K = '극복의 훈장';
  const basics = [
    S(`${H}_s1`, '군홧발', 1, '공격', [dmg(0.5, E1, { hits: 2 })]),
    S(`${H}_s2`, '방진', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '전열 사수', 1, '강화', [stk(K, 1), pw('blocked', [stk(K, 1)])], [                    // 시동 — 진짜 전장의 전열
      O([stk(K, 1), pw('blocked', [stk(K, 1)])]),                                      // (수치)
      O([stk(K, 1), pw('turnStart', [cleanse(1)])], { cost: 0 }),                      // 매일 점호
      O([stk(K, 1), sh(0.6), pw('blocked', [sh(0.25)])]),  // 방진 유지
      O([stk(K, 2), pw('play', [stk(K, 1)], { when: { type: '공격', every: 4 } })]),  // 넷째 공격마다 검법(원작 강화 평타)
      O([st('피해 감소', 1), pw('blocked', [stk(K, 1)])]),                             // 붙잡는 전열
    ], null, ['개전']),
    U(`${H}_u1`, '나아가는 결의', 1, '공격', [dmg(0.4, EA), heal(0.5), stk(K, 1)], [                   // 연료 — 원작 저학년
      O([dmg(0.4, EA), heal(0.5), stk(K, 1)]),                                         // (수치)
      O([dmg(0.3, EA), stk(K, 2)]),                                                    // 맞힌 만큼 훈장
      O([dmg(0.8, E1), heal(0.6)]),                                                    // 한 놈 앞에서
      O([dmg(0.3, EA), stk(K, 1), pw('turnStart', [heal(0.15)])], { power: true }),  // 결의의 행군
      O([dmg(0.35, EA), heal(0.4), cleanse(1)]),                                       // 족쇄를 끊으며
    ], [B('power'), B('heal'), B(BK)]),
    U(`${H}_u4`, '극복의 기록', 1, '스킬', [per(K, { n: 1 }), sh(0.35), spendAll(K), cleanse(2)], [   // 결정타 — 훈장으로 동료의 족쇄를
      O([per(K, { n: 1 }), sh(0.35), spendAll(K), cleanse(2)]),                         // (수치)
      O([per(K, { n: 1 }), heal(0.3), spendAll(K)], { cost: 0 }),  // 회복 기록
      O([per(K, { n: 1 }), sh(0.3), spendN(K, 2), cleanse(1)]),                         // 두 개만 쓰기
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)]),                                // 장교 검법(공격)
      O([cleanse(3), stk(K, 1)], { cost: 0 }),                                          // 낯선 과거 받아들이기
    ], [B('guard'), B('cost'), B(BK)]),
    U(`${H}_u2`, '무릎 꿇는 교섭', 0, '스킬', [{ k: 'cutHit', v: 0.5 }, stk(K, 1)], [                    // 원작 자유 — 블랑셰 앞 평화 교섭
      O([{ k: 'cutHit', v: 0.5 }, stk(K, 1)]),                                          // (수치)
      O([{ k: 'cutHit', v: 0.3 }, st('약화', 1, EA)]),                                  // 모두와 교섭
      O([{ k: 'cutHit', v: 0.4 }, draw(1)]),                                            // 교섭하며 다음 수
      O([st('약화', 2, E1), stk(K, 1)]),                                                // 엄포
      O([{ k: 'cutHit', v: 0.5 }, sh(0.6)]),  // 전함 호출 협박
    ], [B('draw'), B('guard'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '극복해 온 날의 훈장', carrier: 'self', cap: 4, per: [{ stat: 'taken', v: -0.04 }], onMax: { empower: 'any', ratio: 0.4, consume: true } },
    passives: [
      pas('다 막아 낸 전열', 'blocked', [stk(K, 1)]),
      pas('잊을 수 없는 기억', 'turnStart', [cleanse(1)]),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`],
    basics, uniques,
  });
}

// ── 셀리네 — 버티형 · 탱커 ──
// 엔진: 약 올려(반격 · 막기) 화난 표정을 모으고(동료가 디버프를 걸어도 수집) → 다섯이면 조회수 폭발 셀카 → 「화난 얼굴 갤러리」가 표정만큼 HP 낮은 적을 쳐 처치하면 보호막
function seline(j) {
  // 팀: 아군 누구든 적에게 디버프를 걸면 표정 1(남의 화난 얼굴도 수집)
  // 원작: 도발에서 태어난 유령 · 「방금 그 표정 몹시 좋았어」 · 강화 평타(셋째 공격 대신 회복 + 도발) · 고학년 「핑크 더스트」(HP 비율 낮은 적 · 처치하면 보호막) · 엘튜버 고스 쉐리
  const H = '셀리네', K = '표정', SF = '셀리네_selfie', VR = '셀리네_viral';
  const basics = [
    S(`${H}_s1`, '하트 탄', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '팬서비스 가드', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const tokens = [
    T(SF, '셀카', H, '스킬', [stk(K, 1), sh(0.35)], { tags: ['소멸'], blurb: '화난 얼굴을 배경으로 찍은 셀카입니다' }),
    T(VR, '조회수 폭발 셀카', H, '스킬', [sh(1.0), st('반격', 1), draw(1)], { tags: ['소멸'], blurb: '조회수가 폭발한 셀카입니다' }),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u4`, '사근사근 반말', 1, '강화', [st('반격', 1), pw('blocked', [stk(K, 1)])], [                // 시동 — 약 올리기
      O([st('반격', 1), pw('blocked', [stk(K, 1)])]),                                  // (수치)
      O([st('반격', 2), pw('hurt', [stk(K, 1)])]),                                     // 대놓고 약 올리기
      O([sh(0.6), pw('turnStart', [make(SF, 1)])]),                                    // 매일 업로드
      O([pw('blocked', [stk(K, 1), sh(0.2)])], { cost: 0 }),                            // 시큰둥한 반말
      O([st('반격', 1), pw('blocked', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),       // 개전부터 도발
    ], null),
    U(`${H}_u1`, '온 마음을 다해', 1, '공격', [dmg(0.35, EA), st('약화', 1, EA), stk(K, 1)], [              // 연료 — 원작 저학년(하트 에너지 · 기절)
      O([dmg(0.35, EA), st('약화', 1, EA), stk(K, 1)]),                                // (수치)
      O([dmg(0.6, FAR), st('기절', 1, FAR), stk(K, 1)], { cost: 2 }),  // 가장 먼 적에게 하트(기절)
      O([dmg(0.3, EA), make(SF, 1)]),                                                  // 인증샷
      O([dmg(0.3, EA), pw('debuff', [stk(K, 1)])], { power: true }),                   // 반응 수집
      O([dmg(0.8, E1), stk(K, 2)]),                                                    // 한 사람만 놀리기
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u3`, '화난 얼굴 갤러리', 1, '공격', [per(K, { n: 1 }), dmg(0.3, LOW), spendAll(K), ifKill, sh(1.0)], [ // 결정타 — 고학년 「핑크 더스트」(처치하면 보호막)
      O([per(K, { n: 1 }), dmg(0.3, LOW), spendAll(K), ifKill, sh(1.0)]),               // (수치)
      O([per(K, { n: 1 }), dmg(0.22, EA), spendAll(K)]),                                // 전체 공개
      O([per(K, { n: 1 }), dmg(0.28, LOW), spendN(K, 3)]),                              // 비공개 몇 장
      O([per(K, { n: 1 }), sh(0.35), spendAll(K), st('반격', 2)]),                      // 악플 방패
      O([per(K, { n: 1 }), dmg(0.25, LOW), make(VR, 1)]),  // 조회수 폭발
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '어그로 썸네일', 0, '스킬', [stk(K, 1), make(SF, 1), draw(1)], [                          // 원작 자유 — 도발 엘튜버 고스 쉐리
      O([stk(K, 1), make(SF, 1), draw(1)]),                                            // (수치)
      O([make(SF, 2)]),                                                                // 썸네일 두 장
      O([stk(K, 2), st('약화', 1, EA), draw(1)]),  // 낚시 제목
      O([stk(K, 1), sh(0.7), draw(1)]),  // 정지 먹기 전에
      O([stk(K, 2), draw(1), pw('make', [stk(K, 1)])], { power: true }),  // 채널 키우기
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '모아 둔 화난 얼굴', carrier: 'self', cap: 5, onMax: { make: VR, consume: true } },
    passives: [
      pas('방금 그 표정 좋았어', 'debuff', [stk(K, 1)], { when: { who: 'any' } }),
      pas('세 번째 도발', 'play', [heal(0.3), st('반격', 1)], { when: { every: 3 } }),
    ],
    ult: j.heroes[0].ult,
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`],
    basics, tokens, uniques,
  });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '넘친 기운', when: { on: 'overheal' }, fx: [sh(0.5), stk(K, 1)] }];
}

export const JOBS = [['수인/코미', komi], ['엘프/이드_재활', ideRehab], ['엘프/하이디', heidi], ['요정/죠안', joan], ['요정/클로에', chloe], ['용족/네티', netty], ['용족/리츠', reets],
  ['마녀/스노키', snoky], ['수인/리온', lion], ['엘프/이드', ide], ['엘프/헤일리_멀쩡', haileyOk], ['유령/셀리네', seline]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
