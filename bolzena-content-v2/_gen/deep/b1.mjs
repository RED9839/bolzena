// 묶음 1 — 예약형 9 + 마리(2성 예외) + 버리기형 2. node _gen/deep/b1.mjs [사도 일부]
// 고유 id: 원작 저학년 카드는 cardart_low 의 id 에, 나머지는 u1~u4 가운데 남는 자리.
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, FAR, ALLY, ALLIES, OTHERS, dmg, ddef, hits, sh, heal, st, stk, spendAll, spendN, per, draw, make, gauge, disc, ifStack, notStack, ifKill, ifFoe,
  empower, tough, later, dmod, cost, power, rule, pw, pas, O, B, U, S, T, hero } = L;
const hasten = v => ({ k: 'hasten', v });
const ripen = (id, v, t) => ({ k: 'ripen', id, ...(v ? { v } : {}), ...(t ? { target: t } : {}) });
const rush = (v, t = E1) => ({ k: 'rushDown', v, target: t });
const dispel = n => ({ k: 'dispel', n });
const strip = (t = E1) => ({ k: 'strip', target: t });
const resv = (name, desc, carrier, cap, gone, { ruleName, ...o } = {}) => ({ name, desc, carrier, cap, reserve: true, decay: 1, ...o, rules: [{ name: ruleName || name, when: { on: 'stackGone', id: name, decay: true }, fx: gone }] });

// ── 마리 — 예약형 · 딜러(2성 예외, 사용자 10-09) ──
// 엔진: 빨간 막대기(폭탄)를 곳곳에 묻어 두고 → 「기폭 스위치」로 묻은 만큼 한꺼번에. 넷 넘게 한 번에 터지면 「저도 여기 있어요!」. 공격마다 폭탄을 만들던 고리(강화 폭탄)는 없앴다
function mari(j) {
  // 팀: 「특제 폭탄」을 동료 몫으로 건넨다(빨간 막대기 묻기 ③) · 「저도 여기 있어요!」 파티 다음 카드
  // 원작: 저학년 특제 폭탄 + 화상 · 고학년 고폭탄을 「설치한 뒤」 폭발 · 지하 통로에 묻은 폭약 · 「마지막 순간을 위해 숨겨 둔 한 방」(자폭) · 황금손 금고털이 · 존재감 없음
  const H = '마리', K = '묻은 폭탄', BM = '마리_bomb';
  const basics = [
    S(`${H}_s1`, '폭죽 던지기', 1, '공격', [dmg(0.95, E1), st('잔불', 1, E1)]),
    S(`${H}_s2`, '왕폭죽 던지기', 2, '공격', [dmg(2.0, E1), stk(K, 1)]),
    S(`${H}_s3`, '방폭 헬멧', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const tokens = [T(BM, '특제 폭탄', H, '공격', [dmg(0.3, EA), stk(K, 1)], { tags: ['소멸'], blurb: '황금손이 직접 만든 특제 폭탄입니다' })];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '빨간 막대기 묻기', 0, '스킬', [stk(K, 2), draw(1)], [                 // 시동 — 지하 통로에 묻어 둔 폭약
      O([stk(K, 3), draw(1)]),                                                    // 수치
      O([stk(K, 1), pw('turnStart', [stk(K, 1)])], { power: true }),              // 매일 밤 굴 파기
      O([stk(K, 1), make(BM, 1, { owner: 'other' })]),                            // 친구 몫 폭탄
      O([later(2, [dmg(0.8, EA)]), stk(K, 1)]),                                   // 시한 신관
      O([stk(K, 1), gauge(15), draw(1)]),                                         // 황금손 금고털이
    ], null),
    U(`${H}_u1`, '폭탄 배달 왔어용~', 1, '공격', [dmg(0.55, EA), st('잔불', 1, EA), stk(K, 1)], [ // 연료 — 원작 저학년(특제 폭탄 + 화상)
      O([dmg(0.72, EA), st('잔불', 1, EA), stk(K, 1)]),                          // 수치
      O([dmg(1.1, E1), st('잔불', 2, E1), stk(K, 1)]),                           // 한 집만 배달
      O([dmg(0.35, EA), pw('turnStart', [stk(K, 1)])], { power: true }),         // 정기 배달
      O([dmg(0.4, EA), stk(K, 2)]),                                              // 배달하며 묻기
      O([make(BM, 2)]),                                                          // 특제 폭탄 두 상자
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u2`, '기폭 스위치', 1, '공격', [per(K, { n: 1, each: false }), dmg(0.32, EA), spendAll(K)], [ // 결정타 — 원작 고학년 「설치한 뒤 폭발」
      O([per(K, { n: 1 }), dmg(0.42, EA), spendAll(K)]),                         // 수치
      O([per(K, { n: 1 }), dmg(0.65, E1), spendAll(K)]),                         // 한 곳에 몰아 터뜨리기
      O([per(K, { n: 1 }), dmg(0.3, EA), spendN(K, 4)]),                         // 반만 터뜨리기
      O([per(K, { n: 1 }), dmg(0.28, EA), spendAll(K), ifKill, stk(K, 3)]),      // 연쇄 폭발(처치하면 다시 묻기)
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K), st('잔불', 2, EA)]),     // 불바다
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '숨겨 둔 한 방', 1, '공격', [{ k: 'payHp', v: 40 }, per(K, { n: 1 }), dmg(0.45, EA), spendAll(K)], [ // 원작 자유 — 마녀 왕궁의 자폭
      O([{ k: 'payHp', v: 40 }, per(K, { n: 1 }), dmg(0.58, EA), spendAll(K)]), // 수치
      O([{ k: 'payHp', v: 60 }, per(K, { n: 1 }), dmg(0.4, EA)]),                // 남은 폭탄은 둔다
      O([{ k: 'payHp', v: 40 }, per(K, { n: 1 }), dmg(0.85, E1), spendAll(K)]), // 유적째 한 놈
      O([per(K, { n: 1 }), dmg(0.32, EA), spendAll(K)]),                         // 대가 없이(덜 세게)
      O([{ k: 'payHp', v: 80 }, per(K, { n: 1 }), dmg(0.62, EA), spendAll(K)]),  // 두 수 앞(크게 치르고 크게)
    ], [B('power'), B('ap'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '곳곳에 묻어 둔 빨간 막대기', carrier: 'self', cap: 8, per: [{ stat: 'dealt', v: 0.03 }] },
    passives: [
      pas('황금손 마리', 'kill', [make(BM, 1)], { when: { mine: true } }),
      pas('강화 폭탄', 'play', [stk(K, 1)]),
    ],
    ult: { name: '발파 합니다!', cost: 300, fx: [stk(K, 4), per(K, { n: 1 }), dmg(0.35, EA), spendAll(K)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`],
    basics, tokens, uniques,
  });
}

// ── 마카샤 — 예약형 · 딜러 ──
// 엔진: 적 하나에 운명의 시계(3칸)를 걸고 → 떠벌린 예언 · 바바가 시계를 돌려 결말을 앞당긴다 → 시계 단 적이 먼저 쓰러지면 결말이 적 전체로 번진다
function makasha(j) {
  // 팀: 시계 단 적을 누가 쓰러뜨려도 결말이 적 전체로(번지는 결말) · 바바의 발자국(아군 회복 · 적 취약)
  // 원작: 저학년 「언젠가 일어날 일」(운명의 시계 — 아군 공격력 합만큼) · 강화 평타 「예언 떠벌리기」 · 고학년 바바(시계 감소) · 어사이드(시계 발동 시 기절 · 발자국)
  const H = '마카샤', K = '운명의 시계';
  const basics = [
    S(`${H}_s1`, '절굿공이 톡', 1, '공격', [ddef(0.5, E1), hasten(1)]),
    S(`${H}_s2`, '안개 약초차', 1, '스킬', [heal(1.9), hasten(1)]),
  ];
  const BK = [hasten(1)];
  const uniques = [
    U(`${H}_u1`, '언젠가 일어날 일', 1, '스킬', [stk(K, 3, E1), ddef(0.35, E1), draw(1)], [ // 시동 — 원작 저학년
      O([stk(K, 3, E1), ddef(0.5, E1), draw(1)]),                                 // 수치
      O([stk(K, 3, TOP), ddef(0.3, EA)]),                                         // 가장 버티는 적에게
      O([stk(K, 2, E1), draw(2)]),                                                // 두 칸짜리 예언
      O([stk(K, 3, E1), pw('reserveFire', [draw(1)])], { power: true }),          // 맞아떨어질 때마다 드로우
      O([stk(K, 3, E1), st('취약', 2, E1)]),                                      // 결말을 귀띔
    ], null),
    U(`${H}_u2`, '예언 떠벌리기', 1, '공격', [ddef(0.35, EA), hasten(1)], [          // 연료 — 원작 강화 평타(시계 감소)
      O([ddef(0.47, EA), hasten(1)]),                                             // 수치
      O([ddef(0.7, E1), hasten(2)]),                                              // 한 사람에게 쏟아 놓기
      O([hasten(1), draw(1)], { cost: 0 }),                                       // 슬쩍 흘리기
      O([ddef(0.25, EA), pw('turnStart', [hasten(1)])], { power: true }),         // 수다쟁이(매 턴)
      O([ddef(0.35, EA), hasten(1), stk(K, 1, E1)]),                              // 다음 예언까지
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u3`, '비상하는 절구통', 1, '스킬', [ripen(K, 0.25), st('기절', 1, E1)], [ // 결정타 — 어사이드(시계 발동 시 기절)
      O([ripen(K, 0.15), st('기절', 1, E1), draw(1)]),                            // 수치(덜 깎임)
      O([ripen(K, 0.15), draw(2)]),                                              // 터뜨리고 다음 예언
      O([ripen(K, 0.25, EA), st('기절', 1, E1), st('취약', 1, EA)]),              // 모든 결말을 지금
      O([ddef(0.5, E1), st('기절', 1, E1), stk(K, 2, E1)]),                       // 결말은 미루고 기절만
      O([ripen(K, 0.25), st('취약', 2, E1)], { cost: 0 }),                        // 탁상공론(0코)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '바바의 발자국', 1, '스킬', [st('취약', 1, EA), heal(0.5), hasten(1)], [ // 원작 자유 — 고학년 바바가 남긴 발자국
      O([st('취약', 1, EA), heal(0.8), hasten(1)]),                               // 수치
      O([st('취약', 2, E1), hasten(2)]),                                         // 한 발자국 깊게
      O([heal(0.4), hasten(1), pw('turnStart', [st('취약', 1, EA)])], { power: true }), // 집이 매일 지나간다
      O([st('취약', 1, EA), heal(0.5), dmod(0.15, OTHERS)]),                      // 마음에 쏙 들어(아군 피해)
      O([ddef(0.4, EA), st('취약', 1, EA)]),                                      // 바바가 들이받기
    ], [B('heal'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '두 칸 뒤에 결말이 오는 시곗바늘', carrier: 'enemy', cap: 3, reserve: true, decay: 1,
      rules: [{ name: '정해진 결말', when: { on: 'stackGone', id: K, decay: true }, fx: [ddef(2.2, E1), st('취약', 1, E1)] }] },
    passives: [
      pas('이미 본 결말', 'fightStart', [stk(K, 3, TOP)]),
      pas('번지는 결말', 'kill', [per(K, { n: 1 }), ddef(0.45, EA)]),
    ],
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, uniques,
  });
}

// ── 요미 — 예약형 · 딜러 ──
// 엔진: 달바라기 영역을 깔아 두면(2턴) 턴마다 적 전체 달빛 · 파티 받는 피해↓ → 기도로 영역을 늘리고, 운석을 걸어 두면 두 턴 뒤 크게 떨어진다(대가 — 다음 턴 AP)
function yomi(j) {
  // 팀: 영역이 깔린 동안 아군이 받는 피해 -5%/개(달바라기) · 운석이 터지면 파티 회복
  // 원작: 저학년 「달바라기」(달의 영역 — 적 피해 · 아군 받는 피해↓, 그동안 별빛 강화 평타) · 고학년 「지극정성의 마중」(나아가는 달빛) · 운석 기도 뒤 앓아눕기 · 「잊히는 게 무섭다」
  const H = '요미', K = '달바라기';
  const basics = [
    S(`${H}_s1`, '달빛 탄', 1, '공격', [dmg(0.95, E1), ifStack(K, 1), dmg(0.3, ER)]),
    S(`${H}_s2`, '만월 탄', 2, '공격', [dmg(2.0, E1), stk(K, 1)]),
    S(`${H}_s3`, '사제의 베일', 1, '스킬', [sh(1.8), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '달바라기', 1, '스킬', [stk(K, 2), dmg(0.3, EA)], [                  // 시동 — 원작 저학년(달의 영역)
      O([stk(K, 3), dmg(0.35, EA)]),                                              // 수치
      O([stk(K, 2), st('약화', 2, EA)]),                                         // 영역 위의 적은 약해진다
      O([stk(K, 2), draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),    // 달바라기 꽃밭(매 턴)
      O([stk(K, 2), draw(1)], { cost: 0 }),                                       // 서둘러 펼치기
      O([stk(K, 2), sh(0.8)]),                                                    // 영역 안의 아군
    ], null),
    U(`${H}_u2`, '별빛 두 줄기', 1, '공격', [hits(2, 0.5), ifStack(K, 1), st('둔화', 1, ER)], [ // 연료 — 원작 강화 평타(별빛 · 공속 감소)
      O([hits(2, 0.65), ifStack(K, 1), st('둔화', 1, ER)]),                       // 수치
      O([hits(3, 0.45)]),                                                         // 별빛 하나 더(어사이드)
      O([dmg(0.95, E1), stk(K, 1)]),                                              // 한 별에 몰아
      O([hits(2, 0.4), pw('play', [dmg(0.2, ER)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }] })], { power: true }), // 영역 동안 공격마다 별빛
      O([hits(2, 0.4), stk(K, 1)]),                                               // 별빛이 꽃을 키운다
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '달님께 비는 운석', 2, '공격', [dmg(0.4, EA), later(2, [dmg(1.8, EA)]), { k: 'nextAp', v: -1 }], [ // 결정타 — 사도 스토리의 운석(뒤에 앓아눕기)
      O([dmg(0.5, EA), later(2, [dmg(2.3, EA)]), { k: 'nextAp', v: -1 }]),        // 수치
      O([later(1, [dmg(1.6, EA)]), { k: 'nextAp', v: -1 }]),                       // 짧은 기도
      O([later(2, [dmg(3.0, E1), st('기절', 1, E1)]), { k: 'nextAp', v: -1 }]),    // 한 점 운석
      O([dmg(0.5, EA), later(2, [dmg(1.6, EA)])], { cost: 3 }),                    // 앓아눕지 않는 기도(3코)
      O([dmg(0.4, EA), later(2, [dmg(1.8, EA), heal(1.0)]), { k: 'nextAp', v: -1 }]), // 운석 뒤 달빛 치유
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u4`, '보름달 아래 춤', 1, '공격', [dmg(0.45, EA), stk(K, 1), hasten(1)], [  // 원작 자유 — 좋아하는 것(보름달 아래 춤)
      O([dmg(0.6, EA), stk(K, 1), hasten(1)]),                                    // 수치
      O([dmg(0.4, EA), hasten(2)]),                                               // 빠른 스텝(재촉)
      O([dmg(0.4, EA), heal(0.6)]),                                               // 달빛 회복
      O([dmg(0.3, EA), stk(K, 1), pw('turnEnd', [dmg(0.2, EA)], { conds: [{ c: 'stack', id: K, n: 1 }] })], { power: true }), // 밤새 추는 춤
      O([dmg(0.9, E1), stk(K, 2)]),                                               // 달님에게 바치는 독무
    ], [B('power'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '보름달을 기다리며 피는 꽃밭', carrier: 'self', cap: 3, endDecay: 1, per: [{ stat: 'taken', v: -0.05, who: 'allies' }],
      rules: [{ name: '달빛 꽃밭', when: { on: 'turnEnd' }, conds: [{ c: 'stack', id: K, n: 1 }], fx: [dmg(0.25, EA)] },
        { name: '별빛', when: { on: 'play', type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }], fx: [dmg(0.25, ER)] }] },
    passives: [
      pas('지극정성의 기도', 'reserveFire', [stk(K, 1), heal(0.3)]),
      pas('부디 저를 기억해 주세요', 'lowHp', [heal(1.0), st('피해 감소', 1)], { when: { pct: 0.3 } }),
    ],
    ult: { name: '지극정성의 마중', cost: 200, fx: [dmg(0.6, EA), st('둔화', 1, EA), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

// ── 리뉴아 — 예약형 · 딜러 ──
// 엔진: 멈춘 둠스데이 시계가 2턴마다 정각에 미사일을 떨어뜨린다(저절로 되풀이) → 크게 맞거나 재촉하면 정각이 앞당겨지고 → 「정지된 6초」가 시계를 지금 터뜨리며 6연타
function renewa(j) {
  // 팀: 예약이 터질 때마다 이번 턴 아군 주는 피해 +10%(우리들의 평화로운 시간)
  // 원작: 저학년 「시공의 메아리」(5연타 · 버프 · 실드 해제) · 고학년 「타임 브레이크」(정지 중 6연타) · 어사이드 둠스데이 클락(6~8초마다 · 크게 맞으면 미사일) · 시간선 여행자
  const H = '리뉴아', K = '둠스데이 시계';
  const basics = [
    S(`${H}_s1`, '시공 사격', 1, '공격', [dmg(1.05, E1), dispel(1)]),
    S(`${H}_s2`, '시공 관통탄', 2, '공격', [dmg(2.3, E1), hasten(1)]),
    S(`${H}_s3`, '무대 뒤로', 1, '스킬', [sh(1.8), hasten(1)]),
  ];
  const BK = [hasten(1)];
  const uniques = [
    U(`${H}_u2`, '멈춰 버린 시계', 0, '스킬', [stk(K, 2), draw(1)], [                   // 시동 — 어사이드 둠스데이 클락
      O([stk(K, 2), draw(2)]),                                                    // 수치
      O([stk(K, 1), draw(1)]),                                                    // 정각 직전으로 맞추기
      O([stk(K, 2), pw('reserveFire', [dmg(0.25, EA)])], { power: true }),       // 미사일마다 잔해
      O([stk(K, 2), sh(0.8)]),                                                    // 시간 정지 방벽
      O([stk(K, 2), gauge(20)]),                                                  // 고학년 대기 시간 감소(원작)
    ], null),
    U(`${H}_u1`, '시공의 메아리', 1, '공격', [dmg(0.22, E1, { hits: 5 }), dispel(1), strip()], [ // 연료 — 원작 저학년(5연타 · 버프 · 실드 해제)
      O([dmg(0.29, E1, { hits: 5 }), dispel(1), strip()]),                        // 수치
      O([dmg(0.15, EA, { hits: 5 }), dispel(1)]),                                 // 넓은 포격
      O([dmg(0.22, E1, { hits: 5 }), hasten(1)]),                                 // 메아리가 시계를 민다
      O([dmg(0.18, E1, { hits: 5 }), pw('turnStart', [dispel(1)])], { power: true }), // 시간선 정리
      O([dmg(0.3, E1, { hits: 6 }), strip()], { cost: 2 }),                       // 여섯 번째 메아리
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '정지된 6초', 2, '공격', [dmg(0.15, EA, { hits: 6 }), ripen(K, 0.25)], [ // 결정타 — 원작 고학년(정지 중 6연타)
      O([dmg(0.2, EA, { hits: 6 }), ripen(K, 0.25)]),                             // 수치
      O([dmg(0.15, EA, { hits: 6 }), st('둔화', 1, EA)]),                        // 시계는 두고 멈추기만
      O([dmg(0.3, E1, { hits: 6 }), ripen(K, 0.25)]),                             // 한 놈만 멈춘 세계
      O([ripen(K, 0.25), stk(K, 1)], { cost: 0 }),                                // 정각을 당기고 다시 감기
      O([dmg(0.12, EA, { hits: 6 }), ripen(K, 0.25)], { cost: 1 }),                // 1코 정지
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '오천 년 뒤의 편지', 1, '강화', [hasten(1), pw('reserveFire', [dmg(0.3, EA)])], [ // 원작 자유 — 5천 년 뒤 엘레나가 보낸 여행자
      O([hasten(1), pw('reserveFire', [dmg(0.4, EA)])]),                         // 수치
      O([hasten(1), pw('reserveFire', [draw(1)])]),                              // 편지마다 한 장
      O([hasten(2), pw('reserveFire', [sh(0.4)])]),                              // 패러독스 방지
      O([pw('turnStart', [hasten(1)])], { cost: 0 }),                             // 매일 한 칸씩
      O([hasten(1), pw('reserveFire', [dmg(0.25, EA), gauge(5)])]),              // 고장 난 둠스데이
    ], [B('draw'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '멸망 타이머가 멈춘 채 정각마다 울리는 시계', carrier: 'self', cap: 2, reserve: true, decay: 1,
      rules: [{ name: '정각', when: { on: 'stackGone', id: K, decay: true }, fx: [dmg(0.9, EA), stk(K, 2)] }] },
    passives: [
      pas('째깍', 'hurt', [hasten(1)], { when: { pct: 0.07 } }),
      pas('우리들의 평화로운 시간', 'reserveFire', [dmod(0.1, ALLIES)]),
    ],
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

// ── 엘레나 — 예약형 · 딜러 ──
// 엔진: 냥이 드론을 여러 기 띄우고 자폭 타이머를 건다 → 타이머가 다 닳으면 드론 수만큼 적 전체 폭발(충격) → 「쓸데없는 자폭 기능」으로 지금 터뜨린다. 터질 때마다 「다른 버그로 고치기」(드로우)
function elena(j) {
  // 팀: 충격 걸린 적을 다른 아군이 치면 드론 1(맑은 눈의 엘레나 — 원작 아군 공속↑)
  // 원작: 저학년 「전술 드론 MK-2」(펄스파 · 감전) · 고학년 「코드네임 D-CAT」(6번 펄스 뒤 폭파) · 발명품마다 쓸데없는 자폭 기능 · 「다른 버그를 만들면 고쳐질 거야」 · 고양이 모양
  const H = '엘레나', D = '냥이 드론', K = '자폭 타이머';
  const basics = [
    S(`${H}_s1`, '공구 던지기', 1, '공격', [dmg(0.95, E1), st('충격', 1, E1)]),
    S(`${H}_s2`, '대형 렌치 투척', 2, '공격', [dmg(2.2, E1), stk(D, 1)]),
    S(`${H}_s3`, '방호 패널', 1, '스킬', [sh(1.8), stk(D, 1)]),
  ];
  const BD = [stk(D, 1)];
  const uniques = [
    U(`${H}_u2`, '냥이 드론 출격', 1, '스킬', [stk(D, 2), stk(K, 2), draw(1)], [        // 시동
      O([stk(D, 3), stk(K, 2), draw(1)]),                                         // 수치
      O([stk(D, 2), draw(1), pw('turnStart', [stk(D, 1)])], { power: true }),     // 생산 라인
      O([stk(D, 3), stk(K, 1), draw(1)]),                                         // 짧은 타이머
      O([stk(D, 2), sh(0.9)]),                                                    // 방패 드론
      O([stk(D, 2), stk(K, 2), gauge(15)]),                                       // 커피 한 잔
    ], null),
    U(`${H}_u1`, '전술 드론 MK-2', 1, '공격', [dmg(0.6, EA), st('충격', 1, EA), stk(D, 1)], [ // 연료 — 원작 저학년(펄스파 · 감전)
      O([dmg(0.8, EA), st('충격', 1, EA), stk(D, 1)]),                            // 수치
      O([dmg(1.2, E1), st('충격', 2, E1)]),                                       // 직선 펄스(한 줄)
      O([dmg(0.4, EA), pw('turnStart', [st('충격', 1, EA)])], { power: true }),   // 상시 펄스
      O([dmg(0.5, EA), stk(D, 2)]),                                               // 펄스 대신 드론
      O([dmg(0.6, EA), st('충격', 1, EA), stk(K, 1)]),                            // 타이머 다시 감기
    ], [B('power'), B('draw'), B(BD)]),
    U(`${H}_u3`, '쓸데없는 자폭 기능', 1, '공격', [dmg(0.4, EA), ripen(K, 0.2)], [       // 결정타 — 발명품마다 붙은 자폭
      O([dmg(0.55, EA), ripen(K, 0.2)]),                                          // 수치
      O([ripen(K, 0.2), stk(D, 2)], { cost: 0 }),                                 // 터뜨리고 새로 띄우기
      O([dmg(0.9, E1), ripen(K, 0.2)]),                                           // 한 놈 앞에서 자폭
      O([ripen(K, 0.2), sh(1.0)]),                                               // 터뜨리고 아멜리아 뒤로
      O([dmg(0.3, EA), ripen(K, 0.2), st('충격', 2, EA)]),                       // 과충전 자폭
    ], [B('power'), B('cost'), B(BD)]),
    U(`${H}_u4`, '코드네임 D-CAT', 2, '공격', [dmg(0.22, EA, { hits: 6 }), st('충격', 1, EA)], [ // 원작 자유 — 고학년(6번 펄스 뒤 폭파)
      O([dmg(0.28, EA, { hits: 6 }), st('충격', 1, EA)]),                         // 수치
      O([dmg(0.4, E1, { hits: 6 })]),                                             // 한 놈 추적
      O([dmg(0.18, EA, { hits: 6 }), stk(D, 2)]),                                 // 특수 드론 귀환
      O([dmg(0.15, EA, { hits: 6 }), pw('reserveFire', [dmg(0.3, EA)])], { power: true }), // 폭파 연쇄
      O([dmg(0.17, EA, { hits: 6 }), stk(K, 1)], { cost: 1 }),                     // 시제품(1코)
    ], [B('power'), B('weakSpot'), B(BD)]),
  ];
  hero(j, {
    keyword: { name: D, desc: '자폭 기능이 달린 고양이 드론', carrier: 'self', cap: 5, per: [{ stat: 'dot', ratio: 0.25 }] },
    keywords: [{ name: K, desc: '드론이 터지기까지 남은 시간', carrier: 'self', cap: 2, reserve: true, decay: 1,
      rules: [{ name: '쓸데없는 자폭', when: { on: 'stackGone', id: K, decay: true }, fx: [per(D, { n: 1 }), dmg(0.35, EA), spendAll(D), stk(K, 2)] }] }],
    passives: [
      pas('드론 출격', 'play', [stk(D, 1)]),
      pas('맑은 눈의 엘레나', 'hit', [ifFoe('충격'), stk(D, 1)], { when: { who: 'other' } }),
    ],
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

// ── 오르 — 예약형 · 서포터 ──
// 엔진: 카드를 낼 때마다 녹음기의 응원이 쌓여 아군 주는 피해가 오르고 → 「2인용 로켓」이 다음 아침 응원을 실드로 바꿔 보낸다(쌓아 둘까 보낼까). 가장 센 적에겐 기상 드론(시한 폭탄)
function or(j) {
  // 팀: 「응원」 1개당 아군 주는 피해 +6%(쌓아 둘지 로켓으로 실드를 보낼지) · 예약이 터진 턴 아군 피해 +15%(포기 없는 발명)
  // 원작: 저학년 「강제 기상 장치」(가장 센 적 · 소음 · 시한 폭발) · 고학년 「휴대용 돔 전개기」(맞을 때마다 회복 · 유성우) · 어사이드 2인용 로켓 · 응원의 녹음기 · 정해진 때 떨어지는 발명품
  const H = '오르', C = '응원', K = '기상 드론';
  const basics = [
    S(`${H}_s1`, '렌치 휘두르기', 1, '공격', [ddef(0.5, E1), stk(C, 1)]),
    S(`${H}_s2`, '응급 수리', 1, '스킬', [heal(1.8), stk(C, 1)]),
  ];
  const BC = [stk(C, 1)];
  const uniques = [
    U(`${H}_u1`, '강제 기상 장치', 1, '스킬', [stk(K, 3, TOP), st('약화', 1, TOP), draw(1)], [ // 연료 — 원작 저학년(가장 센 적 · 시한 폭발)
      O([stk(K, 3, TOP), st('약화', 2, TOP), draw(1)]),                           // 수치
      O([stk(K, 3, E1), st('약화', 2, E1)]),                                       // 골라서 붙이기
      O([stk(K, 2, TOP), stk(C, 2)]),                                              // 짧은 알람
      O([stk(K, 3, TOP), draw(1), pw('reserveFire', [stk(C, 1)])], { power: true }), // 알람마다 응원
      O([stk(K, 3, TOP), ddef(0.5, TOP)]),                                         // 기계팔 크앙
    ], [B('draw'), B('cost'), B(BC)]),
    U(`${H}_u2`, '2인용 로켓', 1, '스킬', [later(1, [per(C, { n: 1 }), sh(0.35), spendAll(C)]), draw(1)], [ // 시동 — 어사이드 2인용 로켓(다음 아침)
      O([later(1, [per(C, { n: 1 }), sh(0.45), spendAll(C)]), draw(1)]),          // 수치
      O([per(C, { n: 1 }), sh(0.25), empower(0.3, 'any')]),                      // 지금 바로 발사(응원은 둔다)
      O([later(1, [per(C, { n: 1 }), heal(0.3), spendAll(C)]), draw(1)]),         // 회복 물자(HP 50% 밑)
      O([stk(C, 1), pw('turnStart', [empower(0.2, 'any')])], { power: true }),    // 정기 배송
      O([later(1, [per(C, { n: 1 }), sh(0.3), spendAll(C), gauge(15)]), draw(1)]), // SP 물자(게이지)
    ], null),
    U(`${H}_u3`, '한발 늦은 발명', 1, '스킬', [ripen(K, 0.25, TOP), stk(C, 1), draw(1)], [ // 결정타 — 늘 한발 늦게 나온 발명품
      O([ripen(K, 0.15, TOP), stk(C, 1), draw(1)]),                               // 수치(덜 깎임)
      O([ripen(K, 0.2, TOP), st('기절', 1, TOP), draw(1)]),                       // 알람에 놀라 기절
      O([later(1, [ddef(1.0, EA)]), stk(C, 1)]),                                  // 진짜 늦게 터지는 발명
      O([ripen(K, 0.25, TOP), stk(C, 2), pw('reserveFire', [sh(0.4)])], { power: true }), // 실패작 방패
      O([ripen(K, 0.25, TOP), stk(C, 3)]),                                         // 우주선 발사(응원 셋)
    ], [B('draw'), B('cost'), B(BC)]),
    U(`${H}_u4`, '휴대용 돔', 1, '강화', [sh(0.6), pw('hurt', [heal(0.12), stk(C, 1)], { when: { guarded: true } })], [ // 원작 자유 — 고학년(맞을 때마다 회복)
      O([sh(0.9), pw('hurt', [heal(0.15), stk(C, 1)], { when: { guarded: true } })]), // 수치
      O([sh(0.8), stk(C, 1), pw('hurt', [ddef(0.2, ER)], { when: { guarded: true } })]), // 밀어내기(반격 유성)
      O([sh(0.6), pw('turnStart', [later(1, [ddef(0.35, EA)])])]),                    // 유성우 예약
      O([sh(0.6), pw('hurt', [stk(C, 1), empower(0.1, 'any')], { when: { guarded: true } })]), // 응원 녹음
      O([st('피해 감소', 2), stk(C, 2), pw('hurt', [heal(0.15), stk(C, 1)], { when: { guarded: true } })]), // 두꺼운 돔
    ], [B('guard'), B('draw'), B(BC)]),
  ];
  hero(j, {
    keyword: { name: C, desc: '녹음기에 담긴 엘리아스의 응원', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.06, who: 'allies' }] },
    keywords: [resv(K, '적에게 붙어 울리다 터지는 자명종 드론', 'enemy', 3, [ddef(1.4, E1), st('약화', 1, E1)], { ruleName: '강제 기상' })],
    passives: [
      pas('응원의 녹음기', 'play', [stk(C, 1)]),
      pas('포기 없는 발명', 'reserveFire', [dmod(0.15, ALLIES)]),
    ],
    ult: { name: '휴대용 돔 전개기', cost: 200, fx: [dmg(0.3, ER, { hits: 4 }), sh(2.0), stk(C, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`],
    basics, uniques,
  });
}

// ── 칸나 — 예약형 · 딜러 ──
// 엔진: 가장 위험한 적에게 포격 조준(2칸)을 걸어 두고 → 「명 받았습니다!」로 지금 쏘거나 기다린다. 포탄이 떨어질 때마다 반려 도장 — 셋이면 다음 카드 +50%. 다른 아군의 예약이 터져도 즉시 출동
function kanna(j) {
  // 팀: 다른 아군의 예약이 터지면 반려 도장 · 드로우(즉시 출동 — 명령이 오면 칼같이)
  // 원작: 저학년 「큰 거 한 방」(특수 포탄 · 범위) · 고학년 양자폭탄(가장 센 적 추적) · 강화 평타 충격 포탄(기절) · 휴가 신청은 늘 반려
  const H = '칸나', S1 = '반려 도장', K = '포격 조준';
  const basics = [
    S(`${H}_s1`, '제압 사격', 1, '공격', [dmg(1.05, E1), stk(S1, 1)]),
    S(`${H}_s2`, '제압 연사', 2, '공격', [dmg(2.3, E1)]),
    S(`${H}_s3`, '진압 방패', 1, '스킬', [sh(1.8), stk(S1, 1)]),
  ];
  const BS = [stk(S1, 1)];
  const uniques = [
    U(`${H}_u5`, '포격 요청', 1, '스킬', [stk(K, 2, E1), draw(1)], [                      // 시동
      O([stk(K, 2, E1), draw(2)]),                                                 // 수치
      O([stk(K, 2, TOP), stk(S1, 1)]),                                             // 가장 센 적 자동 조준
      O([stk(K, 2, E1), pw('reserveFire', [stk(S1, 1)])], { power: true }),       // 포격마다 도장
      O([stk(K, 1, E1), draw(1)], { cost: 0 }),                                   // 급한 요청(한 칸)
      O([stk(K, 2, EA)]),                                                         // 전면 포격 요청
    ], null),
    U(`${H}_u1`, '큰 거 한 방', 2, '공격', [dmg(1.4, E1), dmg(0.4, EA), stk(S1, 1)], [   // 연료 — 원작 저학년(특수 포탄 · 범위)
      O([dmg(1.8, E1), dmg(0.5, EA), stk(S1, 1)]),                                // 수치
      O([dmg(0.9, EA), stk(S1, 1)]),                                              // 넓게 한 방
      O([dmg(2.0, E1), tough(2)]),                                                 // 바늘 끝 포탄(격파)
      O([dmg(1.2, E1), stk(K, 2, E1)]),                                           // 맞히고 조준
      O([dmg(1.0, E1), dmg(0.3, EA)], { cost: 1 }),                               // 작게 쏘기(1코)
    ], [B('power'), B('ap'), B(BS)]),
    U(`${H}_u3`, '명 받았습니다!', 0, '스킬', [ripen(K, 0.2), stk(S1, 1)], [              // 결정타 — 상관 명령이면 즉시
      O([ripen(K, 0.15), stk(S1, 1)]),                                             // 수치(덜 깎임)
      O([ripen(K, 0.2, EA), stk(S1, 1)]),                                         // 전 포대 발사
      O([ripen(K, 0.2), draw(1)]),                                                 // 쏘고 다음 명령
      O([stk(S1, 2), pw('reserveFire', [dmg(0.35, E1)])], { power: true }),        // 포탄마다 추가 사격
      O([ripen(K, 0.25), st('둔화', 1, E1), stk(S1, 1)]),                         // 충격 포탄으로(둔화)
    ], [B('draw'), B('ap'), B(BS)]),
    U(`${H}_u4`, '충격 포탄', 1, '공격', [dmg(0.85, E1), st('둔화', 1, E1), stk(S1, 1)], [ // 원작 자유 — 강화 평타(충격 포탄 · 기절)
      O([dmg(1.1, E1), st('둔화', 1, E1), stk(S1, 1)]),                           // 수치
      O([dmg(1.0, E1), st('기절', 1, E1), stk(S1, 1)], { cost: 2 }),               // 진짜 기절탄
      O([dmg(0.6, EA), st('둔화', 1, EA)]),                                       // 산탄
      O([later(1, [heal(1.0)]), stk(S1, 2)]),                                     // 휴가 신청서(반려 확정)
      O([dmg(0.7, E1), pw('play', [stk(S1, 1)], { when: { type: '공격' } })], { power: true }), // 명포수(공격마다 도장)
    ], [B('power'), B('weakSpot'), B(BS)]),
  ];
  hero(j, {
    keyword: { name: S1, desc: '휴가 신청서마다 찍히는 반려 도장', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.06 }], onMax: { empower: 'next', ratio: 0.5, consume: true } },
    keywords: [resv(K, '포격을 요청해 둔 좌표', 'enemy', 2, [dmg(1.5, E1), tough(1), stk(S1, 1)], { ruleName: '포탄 낙하' })],
    passives: [
      pas('즉시 출동', 'reserveFire', [stk(S1, 1), draw(1)], { when: { who: 'any' } }),
      pas('명포수', 'fightStart', [stk(K, 2, TOP)]),
    ],
    starter: [`${H}_s1`, `${H}_u5`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

// ── 캬롯 — 예약형 · 서포터 ──
// 엔진: 아군에게 씨앗(2칸)을 심어 여물기를 기다린다 → 다 여물면 그 아군 주는 피해↑ · 실드 · 드로우 → 「일찍 캐기」로 덜 여문 걸 당겨 쓰거나 영양제로 키운다
function carrot(j) {
  // 팀: 씨앗 자체가 동료에게 심는 예약(여물면 그 동료 2턴 주는 피해 +30%)
  // 원작: 저학년 「탄산수액 발사」(쏘아 올린 수액이 잠시 뒤 떨어져 아군 피해↑ · 받는 피해↓) · 애착(공격력이 가장 높은 아군에게도) · 고학년 수액 펌프 · 특제 영양제를 너무 부어 잡초 · 첫 당근 햇님이
  const H = '캬롯', K = '씨앗', SP = '캬롯_sprout';
  const basics = [
    S(`${H}_s1`, '수액 방울', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '당근 주스', 1, '스킬', [heal(1.8), stk(K, 1, ALLY)]),
  ];
  const tokens = [T(SP, '당근 새싹', H, '스킬', [heal(0.3), stk(K, 1, 'strongestAlly'), draw(1)], { tags: ['소멸'], blurb: '먼저 일군 텃밭에서 난 새싹입니다' })];
  const BK = [stk(K, 1, 'strongestAlly')];
  const uniques = [
    U(`${H}_u1`, '탄산수액 발사', 1, '스킬', [stk(K, 2, ALLY), st('피해 감소', 1), draw(1)], [ // 연료 — 원작 저학년
      O([stk(K, 2, ALLY), st('피해 감소', 2), draw(1)]),                          // 수치
      O([stk(K, 1, ALLIES), st('피해 감소', 2), draw(1)]),                       // 넓게 뿌리기
      O([stk(K, 2, 'strongestAlly'), stk(K, 1, ALLY)]),                           // 급성장 수액(애착 — 가장 센 아군에게도)
      O([stk(K, 2, ALLY), draw(1), pw('reserveFire', [st('피해 감소', 1)])], { power: true }), // 수확마다 보호
      O([stk(K, 2, ALLY), heal(0.6)]),                                            // 달콤한 수액
    ], [B('draw'), B('heal'), B(BK)]),
    U(`${H}_u2`, '아침 물 주기', 1, '스킬', [heal(0.5), stk(K, 1, ALLIES), make(SP, 1)], [ // 시동 — 일등 정원사의 아침
      O([heal(0.8), stk(K, 1, ALLIES), make(SP, 1)]),                             // 수치
      O([stk(K, 1, ALLIES), pw('turnStart', [make(SP, 1)])], { power: true }),    // 매일 새싹
      O([heal(0.5), make(SP, 2)]),                                                // 새싹만 잔뜩
      O([stk(K, 2, ALLY), draw(1)], { cost: 0 }),                                 // 한 그루에 정성
      O([heal(0.5), stk(K, 1, ALLIES), gauge(25)]),                               // 성장 비료(원작 강화 평타 SP)
    ], null),
    U(`${H}_u3`, '일찍 캐기', 0, '스킬', [ripen(K, 0.3, ALLY), sh(0.4)], [                // 결정타 — 덜 자란 당근
      O([ripen(K, 0.3, ALLY), sh(0.6), draw(1)]),                                 // 캐고 한 장 더
      O([ripen(K, 0.3), draw(1)]),                                                // 밭 전체 수확
      O([ripen(K, 0.3, ALLY), make(SP, 1)]),                                      // 캐고 다시 심기
      O([ripen(K, 0.3, ALLY), dmod(0.2, ALLY)]),                                  // 갓 캔 당근의 힘
      O([ripen(K, 0.3, ALLY), heal(0.6)]),                                        // 샌드위치로(카렌)
    ], [B('draw'), B('cost'), B(BK)]),
    U(`${H}_u4`, '사탕수수 몽둥이', 1, '공격', [dmg(0.75, EA), hasten(1)], [              // 원작 자유 — 평타(사탕수수 범위) · 애착 사탕수수
      O([dmg(0.95, EA), hasten(1)]),                                              // 수치
      O([dmg(1.4, E1), hasten(1)]),                                               // 한 놈 혼내기
      O([dmg(0.5, EA), hasten(1), gauge(15)]),                                    // 특제 영양제(넘쳐서 게이지)
      O([dmg(0.5, EA), pw('reserveFire', [dmg(0.25, EA)])], { power: true }),     // 수확 축제
      O([dmg(0.6, EA), make(SP, 1)]),                                             // 몽둥이로 땅 고르기
    ], [B('power'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: resv(K, '아군 곁에 심어 기다리는 탄산수액', 'hero', 2, [dmod(0.3, ALLY, { turns: 2 }), sh(0.9), draw(1)], { ruleName: '수확' }),
    passives: [
      pas('일등 정원사', 'play', [stk(K, 1, 'strongestAlly')], { when: { type: '스킬' } }),
      pas('햇님이', 'reserveFire', [sh(0.5)]),
    ],
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`],
    basics, tokens, uniques,
  });
}

// ── 셰이디 — 예약형 · 딜러 ──
// 엔진: 적에게 장난을 예고(2칸)해 두고 → 「달력 한 장 넘기기」로 적의 차례를 뒤로 미룬다 → 미뤄진 틈에 예고가 터지고, 터질 때마다 셰이디의 다음 카드가 세진다(VIP석)
function shady(j) {
  // 팀: 적의 행동을 미루면 파티 모두가 먼저 친다(달력 한 장 넘기기) · 「소악마의 대변인」(아군 주는 피해) 신탁
  // 원작: 저학년 「불 좀 꺼줄래?」(가장 뒤 적 13연타) · 고학년 「타임 오브 셰이디」(6연타 · 침묵) · 달력 · 전산망을 한 달 뒤로 미룬 장난 · 장난 노트 · 사슬낫 1~4호
  const H = '셰이디', K = '장난';
  const basics = [
    S(`${H}_s1`, '사슬낫 휘두르기', 1, '공격', [dmg(1.05, E1)]),
    S(`${H}_s2`, '순간이동', 1, '스킬', [sh(1.6), rush(1)]),
    S(`${H}_s3`, '사슬낫 회전베기', 2, '공격', [dmg(2.1, E1), stk(K, 1, E1)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u5`, '장난 노트', 0, '스킬', [stk(K, 2, E1), draw(1)], [                       // 시동 — 빼곡한 장난 목록
      O([stk(K, 2, E1), draw(2)]),                                                 // 수치
      O([stk(K, 1, E1), draw(1)]),                                                 // 바로 다음 장난
      O([stk(K, 2, E1), pw('reserveFire', [draw(1)])], { power: true }),           // 장난 성공 일지
      O([stk(K, 2, ER), stk(K, 2, ER)]),                                           // 아무나 둘
      O([stk(K, 2, E1), st('취약', 1, E1)]),                                       // 그물 덫
    ], null),
    U(`${H}_u1`, '불 좀 꺼줄래?', 1, '공격', [dmg(0.1, FAR, { hits: 10 }), ripen(K, 0.3, FAR)], [ // 연료 — 원작 저학년(가장 뒤 적 13연타)
      O([dmg(0.13, FAR, { hits: 10 }), ripen(K, 0.3, FAR)]),                      // 수치
      O([dmg(0.1, E1, { hits: 10 }), ripen(K, 0.3)]),                              // 골라서 순간이동
      O([dmg(0.09, FAR, { hits: 13 }), stk(K, 1, FAR)]),                           // 13연타 그대로
      O([dmg(0.1, FAR, { hits: 10 }), stk(K, 2, FAR)]),                         // 연타 뒤 예고
      O([dmg(0.11, FAR, { hits: 10 }), strip(FAR)]),                               // 차원 사슬낫(애착 — 방어 깎기)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '달력 한 장 넘기기', 1, '공격', [dmg(0.5, E1), rush(2)], [                // 결정타 — 한 달 뒤로 미룬 장난
      O([dmg(0.65, E1), rush(2)]),                                                 // 수치
      O([dmg(0.3, EA), rush(1, EA)]),                                              // 모두의 달력
      O([dmg(0.4, E1), rush(2), stk(K, 1, E1)]),                                   // 미루고 예고
      O([dmg(0.4, E1), stk(K, 1, E1), pw('reserveFire', [rush(1)])], { power: true }), // 장난마다 미루기
      O([dmg(0.5, E1), st('기절', 1, E1)], { cost: 2 }),                           // 대롱대롱 매달기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '사슬낫 네 자루', 1, '공격', [hits(4, 0.28), st('취약', 1, E1)], [          // 원작 자유 — 사슬낫 1~4호
      O([hits(4, 0.36), st('취약', 1, E1)]),                                       // 수치
      O([dmg(0.28, E1, { hits: 4 }), stk(K, 1, E1)]),                              // 한 놈에게 네 자루
      O([hits(4, 0.25), dmod(0.15, OTHERS)]),                                      // 소악마의 대변인(아군 피해)
      O([hits(2, 0.3), pw('play', [dmg(0.2, ER)], { when: { type: '공격' } })], { power: true }), // 이름 붙인 낫들
      O([hits(6, 0.25)], { cost: 2 }),                                             // 타임 오브 셰이디(6연타)
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, {
    keyword: resv(K, '셰이디가 미리 떠벌려 둔 장난', 'enemy', 2, [dmg(1.1, E1), tough(1)], { ruleName: '장난 성공' }),
    passives: [
      pas('VIP석', 'reserveFire', [empower(0.3)]),
      pas('다음 장난 예고', 'kill', [stk(K, 2, ER)], { when: { mine: true } }),
    ],
    starter: [`${H}_s1`, `${H}_u5`, `${H}_s3`, `${H}_s2`],
    basics, uniques,
  });
}

// ── 시저 — 예약형 · 서포터 ──
// 엔진: 좋은 걸 먼저 맛보이고(한정판의 맛 — 이번 턴 아군 피해↑) 적에게 단종 예고를 건다 → 단종되면 그 적 약화 · 취약 → 지정한 「빌런의 수하」는 시저 덕을 같이 본다
function caesar(j) {
  // 팀: 「빌런의 수하」로 지정한 아군이 카드를 낼 때마다 회복(원작 강화 평타 — 전열 아군 지정 · 회복)
  // 원작: 저학년 「실이 보인다」(6회 · SP 뺏기, 수하도 같은 효과) · 고학년 「가지치기」 · 「좋은 걸 맛보게 해 놓고 다시는 못 얻게」 · 한정판 햄버거 단종 · 어사이드 방해꾼
  const H = '시저', K = '단종 예고', M = '빌런의 수하';
  const basics = [
    S(`${H}_s1`, '싹둑', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '간식 나눠주기', 1, '스킬', [heal(1.3), stk(M, 1, ALLY)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '한정판의 맛', 1, '스킬', [dmod(0.2, ALLIES), stk(K, 2, E1), draw(1)], [ // 시동 — 맛보이고 거둬 가기
      O([dmod(0.27, ALLIES), stk(K, 2, E1), draw(1)]),                            // 수치
      O([st('사기', 1), stk(K, 2, E1)]),                                          // 오래가는 맛(사기)
      O([dmod(0.2, ALLIES), stk(K, 2, TOP), draw(1)]),                            // 방해꾼 1호(가장 센 적)
      O([stk(K, 2, E1), draw(1), pw('reserveFire', [dmod(0.15, ALLIES)])], { power: true }), // 단종 기념 세일
      O([dmod(0.2, ALLIES), stk(K, 1, E1), draw(1)], { cost: 0 }),                 // 맛보기만
    ], null),
    U(`${H}_u2`, '보이는 실', 1, '공격', [dmg(0.14, E1, { hits: 6 }), gauge(8)], [         // 연료 — 원작 저학년(6회 · SP 뺏기)
      O([dmg(0.18, E1, { hits: 6 }), gauge(8)]),                                  // 수치
      O([dmg(0.14, E1, { hits: 6 }), rush(1)]),                                   // 고학년 늦추기(실을 끊어)
      O([dmg(0.12, E1, { hits: 6 }), stk(K, 1, E1)]),                             // 실을 끊어 단종
      O([dmg(0.1, E1, { hits: 6 }), pw('play', [gauge(4)], { when: { marked: M } })], { power: true }), // 수하도 같은 효과
      O([dmg(0.1, EA, { hits: 6 })], { cost: 2 }),                                // 모든 실
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u3`, '싹둑싹둑', 1, '공격', [dmg(0.45, E1), ripen(K, 0.3)], [               // 결정타 — 단종 앞당기기
      O([dmg(0.6, E1), ripen(K, 0.3)]),                                           // 수치
      O([ripen(K, 0.2), sh(0.6), draw(1)]),                                      // 덜 아픈 단종
      O([dmg(0.3, EA), ripen(K, 0.3, EA)]),                                       // 전 품목 단종
      O([dmg(0.65, E1), st('약화', 2, E1)]),                                      // 자르기만(예고는 둔다)
      O([ripen(K, 0.3), dispel(1), st('약화', 2, E1)]),                            // 버프 실도 싹둑
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '빌런의 품격', 1, '스킬', [stk(M, 1, ALLY), heal(0.6), draw(1)], [        // 원작 자유 — 강화 평타(수하 지정 · 회복) · 어사이드 이름
      O([stk(M, 1, ALLY), heal(0.9), draw(1)]),                                   // 수치
      O([stk(M, 1, 'strongestAlly'), dmod(0.2, 'strongestAlly')]),                // 가장 센 수하에게 품격
      O([stk(M, 1, ALLY), heal(0.3), pw('play', [heal(0.12)], { when: { marked: M } })], { power: true }), // 매초 회복(원작)
      O([stk(M, 1, ALLY), st('피해 감소', 2)]),                                    // 중열 보호(어사이드)
      O([heal(0.5), stk(K, 1, E1), draw(1)], { cost: 0 }),                        // 의외로 빌런이 이기는 전개
    ], [B('heal'), B('draw'), B(BK)]),
  ];
  hero(j, {
    keyword: resv(K, '두 차례 뒤 단종되는 한정판', 'enemy', 2, [dmg(1.3, E1), st('약화', 1, E1), st('취약', 2, E1)], { ruleName: '단종' }),
    keywords: [{ name: M, desc: '시저가 지정한 빌런의 수하', carrier: 'hero', cap: 1 }],
    passives: [
      pas('빌런의 수하', 'play', [heal(0.15)], { when: { marked: M } }),
      pas('의외로 빌런이 이기는 전개', 'fightStart', [stk(K, 2, TOP)]),
    ],
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`],
    basics, uniques,
  });
}

// ── 스피키(메이드) — 버리기형 · 딜러 ──
// 엔진: 카드가 버려질 때마다 고장난 청소기가 잡동사니를 빨아들이고 → 「터보 흡입」이 잡동사니 수만큼 무작위 연타로 뱉는다 · 버린 카드 하나로 전단지 · 사탕 · 호박 중 하나
function spikyMaid(j) {
  // 팀: 「두 명의 크레페」 — 손의 다른 아군 카드 효과를 따라 해 한 번 더(정체성 흉내)
  // 원작: 저학년 「깨끗하면 할 일이 없어!」(잡동사니 셋 중 하나) · 고학년 「터보 클린 모드」(끌려다니며 5연타) · 크레페를 따라 하는 정체성의 유령 · 청소기를 터뜨려 먼지판 · 독설
  const H = '스피키_메이드', K = '잡동사니';
  const basics = [
    S(`${H}_s1`, '먼지털이 휘두르기', 1, '공격', [dmg(1.05, E1), stk(K, 1)]),
    S(`${H}_s2`, '호박 쟁반 방패', 1, '스킬', [sh(1.6), disc(1), draw(1)]),
    S(`${H}_s3`, '먼지털이 대회전', 2, '공격', [dmg(2.1, E1), stk(K, 1)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '먼지판 만들기', 0, '스킬', [disc(2), draw(2)], [                          // 시동 — 크레페가 둘 필요하도록
      O([disc(2), draw(3)]),                                                      // 수치
      O([disc(1), draw(1), stk(K, 2)]),                                           // 한 장만 털기
      O([disc('all'), { k: 'perDiscarded' }, dmg(0.85, ER)]),                      // 대청소(버린 만큼 무작위 적)
      O([disc(1), pw('turnStart', [stk(K, 1)])], { power: true }),                 // 매일 쓸고 닦기
      O([disc(2), draw(2), gauge(10)]),                                            // 자동 청소 모드
    ], null),
    U(`${H}_u1`, '깨끗하면 할 일이 없어!', 1, '공격', [{ k: 'roll', n: 3 }, { k: 'ifRoll', n: 1 }, dmg(0.75, EA), { k: 'ifRoll', n: 2 }, st('약화', 2, EA), { k: 'ifRoll', n: 3 }, dmg(1.3, E1)], [ // 연료 — 원작 저학년(전단지 · 사탕 · 호박)
      O([{ k: 'roll', n: 3 }, { k: 'ifRoll', n: 1 }, dmg(0.95, EA), { k: 'ifRoll', n: 2 }, st('약화', 3, EA), { k: 'ifRoll', n: 3 }, dmg(1.65, E1)]), // 수치
      O([dmg(0.9, EA), stk(K, 3)]),                                               // 전단지만(광역)
      O([dmg(1.7, E1), stk(K, 1)]),                                               // 호박만(떨어지는 한 방)
      O([dmg(0.65, EA), st('약화', 3, EA), stk(K, 3)]),                            // 사탕만(약화)
      O([dmg(0.7, EA), stk(K, 3), pw('discard', [dmg(0.3, ER)])], { power: true }), // 버릴 때마다 튀는 잡동사니
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u2`, '터보 흡입', 1, '공격', [per(K, { n: 1 }), dmg(0.35, ER), spendAll(K)], [  // 결정타 — 원작 고학년(끌려다니며 연타)
      O([per(K, { n: 1 }), dmg(0.45, ER), spendAll(K)]),                          // 수치
      O([per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)]),                            // 한 놈에게 쏟기
      O([per(K, { n: 1 }), dmg(0.3, ER), spendN(K, 3)]),                           // 조금 남기기
      O([per(K, { n: 1 }), dmg(0.3, ER), spendAll(K), st('피해 감소', 2)]),       // 무적 폭주(받는 피해↓)
      O([per(K, { n: 1 }), dmg(0.3, ER), spendAll(K), draw(2)]),                  // 뱉고 다시 줍기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '두 명의 크레페', 1, '스킬', [{ k: 'castOther', ratio: 0.6 }, stk(K, 1)], [  // 원작 자유 — 따라 하면 원본만큼(정체성 흉내)
      O([{ k: 'castOther', ratio: 0.8 }, stk(K, 1)]),                             // 수치
      O([{ k: 'castOther', ratio: 0.6 }, draw(1)], { cost: 0 }),                  // 재빠른 흉내
      O([{ k: 'castOther', ratio: 0.6 }, stk(K, 2)]),                             // 흉내 내며 줍기
      O([{ k: 'castOther', ratio: 0.5 }, st('사기', 1)]),                     // 원본 응원(독설)
      O([{ k: 'castOther', ratio: 0.6 }, stk(K, 3)]),                             // 흉내 내며 잡동사니
    ], [B('draw'), B('cost'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '고장난 청소기가 빨아들인 잡동사니', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.04 }] },
    passives: [
      pas('고장난 청소기', 'discard', [stk(K, 1)], { when: { who: 'any' } }),
      pas('고장난 청소기', 'play', [stk(K, 1)], { when: { type: '공격' } }),
      pas('자동 청소 모드에요오!', 'ult', [dmod(0.15, OTHERS)]),
    ],
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s3`, `${H}_s2`],
    basics, uniques,
  });
}

// ── 잉클 — 버리기형 · 딜러 ──
// 엔진: 획마다 적에게 먹빛(지워지지 않는 3겹 — 받는 피해↑)을 남기고 → 버려진 원고를 건져 다시 쓰면 먹빛이 더 묻는다 → 「마무리 펜 터치」가 적마다 먹빛만큼 적 전체에 쏟는다
function inkle(j) {
  // 팀: 먹빛 1개당 그 적이 받는 피해 +10% — 동료의 공격도 덕을 본다
  // 원작: 평타 획(무작위 적 · 먹빛 최대 3, 해제 불가) · 강화 평타 「마무리 펜 터치」(먹빛만큼 적 전체 · 소모) · 저학년 「먹물 세례」 · 고학년 「한 폭 수묵화」 · 버려진 동인지에서 사라질 뻔 · 고쳐 쓴 이야기
  const H = '잉클', K = '먹빛';
  const basics = [
    S(`${H}_s1`, '붓끝 찌르기', 1, '공격', [dmg(0.95, E1), stk(K, 1, E1)]),
    S(`${H}_s2`, '큰붓 내려긋기', 2, '공격', [dmg(2.0, E1), stk(K, 1, E1)]),
    S(`${H}_s3`, '먹 번짐 가림막', 1, '스킬', [sh(1.5), disc(1), draw(1)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '고쳐 쓴 원고', 0, '스킬', [{ k: 'pull', from: 'discard', n: 1 }, stk(K, 1, E1)], [ // 시동 — 버려진 원고를 다시 읽고 고쳐 쓰기
      O([{ k: 'pull', from: 'discard', n: 1 }, stk(K, 2, E1)]),                   // 수치
      O([{ k: 'pull', from: 'discard', n: 2 }]),                                  // 두 장 건지기
      O([disc(1), draw(2), stk(K, 1, ER)]),                                       // 버리고 새로 쓰기
      O([stk(K, 1, E1), pw('discard', [stk(K, 1, ER)])], { power: true }),        // 버린 원고마다 먹빛
      O([{ k: 'pull', from: 'discard', n: 1 }, stk(K, 1, E1), gauge(20)]),         // 셰럼의 작가 꿈
    ], null),
    U(`${H}_u3`, '먹물 세례', 1, '공격', [dmg(0.5, EA), st('약화', 1, EA), stk(K, 1, EA)], [ // 연료 — 원작 저학년(범위 · 눈가림 · 먹빛)
      O([dmg(0.65, EA), st('약화', 1, EA), stk(K, 1, EA)]),                       // 수치
      O([dmg(1.0, E1), stk(K, 2, E1)]),                                           // 한 획 진하게
      O([dmg(0.4, EA), stk(K, 1, EA), disc(1)]),                                  // 세례 뒤 원고 버리기
      O([dmg(0.3, EA), pw('turnStart', [stk(K, 1, EA)])], { power: true }),       // 한 폭 수묵화(매 턴)
      O([dmg(0.55, EA), stk(K, 2, EA)], { cost: 2 }),                             // 먹물 폭탄(지워지지 않는)
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u2`, '마무리 펜 터치', 1, '공격', [dmg(0.3, EA), per(K, { n: 1, each: true }), dmg(0.3, EA), spendAll(K)], [ // 결정타 — 원작 강화 평타
      O([dmg(0.4, EA), per(K, { n: 1, each: true }), dmg(0.4, EA), spendAll(K)]), // 수치
      O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)]),                            // 한 장면 마무리
      O([dmg(0.3, EA), per(K, { n: 1, each: true }), dmg(0.3, EA)]),              // 먹빛은 남긴다
      O([per(K, { n: 1, each: true }), dmg(0.4, EA), spendAll(K), st('기절', 1, E1)]), // 펜 끝 기절(어사이드)
      O([per(K, { n: 1, each: true }), dmg(0.4, EA), spendAll(K), draw(1)]),      // 다음 장으로
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '다시 쓰는 이야기', 1, '공격', [dmg(0.8, E1), stk(K, 2, E1)], [            // 원작 자유 — 대체 역사(현명한 여왕 에르핀)
      O([dmg(1.05, E1), stk(K, 2, E1)]),                                          // 수치
      O([dmg(0.5, EA), stk(K, 1, EA)]),                                           // 모두의 이야기
      O([dmg(0.7, E1), stk(K, 2, E1), { k: 'pull', from: 'discard', n: 1 }]),      // 앞 장 다시 넘기기
      O([dmg(0.6, E1), pw('play', [stk(K, 1, E1)], { when: { type: '공격' } })], { power: true }), // 들어주길 바라오
      O([dmg(1.3, E1), stk(K, 3, E1), tough(1)], { cost: 2 }),                   // 셰럼의 문장(격파)
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, {
    keyword: { name: K, desc: '지울 수 없는 먹 자국', carrier: 'enemy', cap: 3, per: [{ stat: 'taken', v: 0.1 }] },
    passives: [
      pas('획 긋기', 'play', [stk(K, 1, E1)], { when: { type: '공격' } }),
      pas('세상에 한 획', 'discard', [stk(K, 1, ER)]),
    ],
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`],
    basics, uniques,
  });
}

export const JOBS = [['요정/마리', mari], ['마녀/마카샤', makasha], ['마녀/요미', yomi], ['엘프/리뉴아', renewa], ['엘프/엘레나', elena], ['엘프/오르', or], ['엘프/칸나', kanna], ['요정/캬롯', carrot], ['유령/셰이디', shady], ['정령/시저', caesar], ['유령/스피키_메이드', spikyMaid], ['정령/잉클', inkle]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
