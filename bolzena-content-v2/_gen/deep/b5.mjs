// 묶음 5 — 쌓고 고르기 9 + 대가형 3. node _gen/deep/b5.mjs [사도 일부]
// 고유 효과 · 패시브는 10-08 갈래 재설계(원작 근거)를 이어 쓰는 사도가 많다(kw0 · pas0). 고유 4장 · 신탁 · 기본 카드 · 고학년은 새로.
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, LOW, FAR, ALLY, ALLIES, OTHERS, dmg, ddef, sh, heal, st, stk, spendAll, spendN, per, perTag, draw, make, ap, gauge,
  ifStack, ifKill, empower, form, tough, later, dmod, cost, pw, pas, O, B, U, S, T, hero } = L;
const cleanse = v => ({ k: 'cleanse', v });
const perEvent = { k: 'perEvent' }, perPaid = n => ({ k: 'perPaid', per: n });
const payHp = v => ({ k: 'payHp', v }), payPct = v => ({ k: 'payHpPct', v });
const feed = (v, t) => (t ? { k: 'feed', v, target: t } : { k: 'feed', v });
const ifWounded = { k: 'ifWounded' }, nextAp = v => ({ k: 'nextAp', v });
const ifChoice = n => ({ k: 'ifChoice', n });
const pull = (o = {}) => ({ k: 'pull', from: 'discard', n: 1, ...o });
const kw0 = j => { const h = j.heroes[0]; return { keyword: h.keyword, ...(h.keywords ? { keywords: h.keywords } : {}) }; };
const pas0 = j => j.heroes[0].passives;
const forms0 = j => j.heroes[0].forms;
const tok0 = (j, id) => j.cards.find(c => c.id === id);

// ── 벨리타 — 쌓고 고르기 · 딜러 ──
// 엔진: 턴마다 몰래 초콜릿 1장(먹으면 당 수치 — 주는 피해 +12%/개) → 「여왕의 간식 시간」으로 당을 털면 턴 수만큼 무작위 폭격 · 털 때마다 고학년 게이지 +25(심판의 비를 앞당긴다)
function velita(j) {
  // 팀: 「여왕의 심판」 — 고학년을 쓰면 적 전체 덤 피해(앞당긴 심판이 파티의 판을 연다)
  // 원작: 저학년 「디멘션 오브 위치」(차원 폭발 · 애착판은 잠시 뒤 터짐) · 고학년 「크림슨 레인」(10회 폭격) · 어사이드(적중마다 고학년 쿨↓ · 두 번) · 비밀 창고의 초콜릿 · 당쇼크
  const H = '벨리타', K = '당 수치', T1 = `${H}_t1`;
  const basics = [
    S(`${H}_s1`, '마녀의 지팡이', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '여왕의 마력 폭발', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '뿌리의 수호', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [tok0(j, T1)];
  const BK = [make(T1, 1)];
  const uniques = [
    U(`${H}_u2`, '비밀 간식 창고', 0, '스킬', [make(T1, 2)], [                                          // 시동 — 쟁여 둔 초콜릿
      O([make(T1, 2)]),                                                              // (수치)
      O([make(T1, 1), draw(1)]),                                                     // 한 조각만
      O([make(T1, 1), stk(K, 1), draw(1)]),  // 바로 한 입
      O([make(T1, 2), sh(0.5)]),  // 창고 털기
      O([make(T1, 1), gauge(10)]),                                                   // 양갱 한 줄
    ], null),
    U(`${H}_u4`, '여왕의 간식 시간', 1, '공격', [dmg(0.8, E1), spendAll(K), perEvent, dmg(0.35, ER)], [   // 결정타 — 당을 털어 무작위 폭격
      O([dmg(0.8, E1), spendAll(K), perEvent, dmg(0.35, ER)]),                       // (수치)
      O([dmg(0.4, EA), spendAll(K), perEvent, dmg(0.25, EA)]),  // 크림슨 소나기
      O([dmg(0.8, E1), spendN(K, 2), perEvent, dmg(0.4, ER)]),                       // 두 개만(당쇼크 조심)
      O([spendAll(K), perEvent, dmg(0.5, E1)], { cost: 0 }),                         // 몰래 한 입
      O([dmg(0.7, E1), spendAll(K), perEvent, heal(0.2)]),                           // 단것으로 회복
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u1`, '디멘션 오브 위치', 1, '공격', [dmg(0.8, EA), perTag(T1), dmg(0.25, EA)], [               // 연료 — 원작 저학년(차원 폭발)
      O([dmg(0.8, EA), perTag(T1), dmg(0.25, EA)]),                                  // (수치)
      O([dmg(1.2, E1), perTag(T1), dmg(0.4, E1)]),                                   // 한 점에 차원 압축
      O([dmg(0.7, EA), stk(K, 1)]),                                                  // 차원 사이 초콜릿
      O([dmg(0.7, EA), make(T1, 1)]),                                                // 창고 문이 열린다
      O([dmg(0.7, EA), gauge(20), stk(K, 1)]),  // 적중마다 쿨↓(어사이드)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '블랙홀 오브 위치', 1, '공격', [dmg(0.35, EA), later(1, [dmg(0.75, EA)])], [               // 원작 자유 — 애착 아티팩트(모았다가 잠시 뒤 터짐)
      O([dmg(0.35, EA), later(1, [dmg(0.75, EA)])]),                                 // (수치)
      O([later(1, [dmg(1.1, EA)])], { cost: 0 }),                                    // 조용히 모으기
      O([dmg(0.3, EA), later(1, [dmg(0.6, EA), st('약화', 1, EA)])]),                // 빨려 들어간 힘
      O([dmg(0.5, E1), later(1, [dmg(1.0, E1)])]),                                   // 한 놈만 삼키기
      O([dmg(0.3, EA), later(1, [dmg(0.6, EA)]), stk(K, 1)]),                        // 여왕의 품위
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '크림슨 레인', cost: 200, fx: [dmg(0.15, EA, { hits: 4 }), make(T1, 1)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 베니(베니) — 쌓고 고르기 · 딜러 ──
// 엔진: 적 몸에 꿀범벅(주는 피해 -4%/겹)을 바르고 → 「꿀단지 강타」로 지금 터뜨릴까, 넷까지 모아 기절시킬까 · 꿀을 쓸 때마다 파티 회복
function beni(j) {
  // 팀: 「베니는 베니야」 — 꿀범벅을 쓸 때마다 파티 회복(털어 온 꿀을 퍼 준다)
  // 원작: 저학년 「허니밤」(튕기는 꿀 웅덩이 · 꿀범벅 최대 20) · 넷째 평타마다 꿀 주먹 · 고학년(꿀범벅 가장 많은 적에게 날아가 중첩만큼 · 일정 이상 기절 · 돌아오며 회복)
  const H = '베니_베니', K = '꿀범벅', JAR = `${H}_jar`;
  const basics = [
    S(`${H}_s1`, '곰 발바닥', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '앞발 내려치기', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '털뭉치 방어', 1, '스킬', [sh(1.6)]),
  ];
  const tokens = [tok0(j, JAR)];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '허니밤', 1, '공격', [dmg(0.36, EA), stk(K, 1, EA), dmg(0.36, FAR)], [                      // 시동 — 원작 저학년(튕기는 꿀 웅덩이)
      O([dmg(0.36, EA), stk(K, 1, EA), dmg(0.36, FAR)]),                             // (수치)
      O([dmg(0.3, EA), stk(K, 2, E1)]),                                              // 한 적에게 듬뿍
      O([stk(K, 1, EA), make(JAR, 1)], { cost: 0 }),                                 // 웅덩이만
      O([dmg(0.3, EA), stk(K, 1, EA), draw(1)]),                                     // 튕겨서 다음 수
      O([dmg(0.4, EA), stk(K, 1, EA), st('약화', 1, EA)]),                           // 끈적한 웅덩이
    ], null),
    U(`${H}_u4`, '꿀단지 강타', 2, '공격', [per(K, { n: 1 }), dmg(0.4, E1), ifStack(K, 4), st('기절', 1, E1), spendAll(K)], [ // 결정타 — 원작 고학년(중첩만큼 · 기절)
      O([per(K, { n: 1 }), dmg(0.4, E1), ifStack(K, 4), st('기절', 1, E1), spendAll(K)]), // (수치)
      O([per(K, { n: 1 }), dmg(0.45, E1), spendAll(K)]),                             // 바로 터뜨리기
      O([per(K, { n: 1 }), dmg(0.45, E1), st('약화', 1, E1)]),  // 꿀은 남긴다
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), heal(0.3)]),  // 꿀단지 깨기
      O([per(K, { n: 1 }), dmg(0.35, E1), spendAll(K), heal(0.4)], { cost: 1 }),     // 돌아오며 회복(원작)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '꿀 웅덩이', 1, '스킬', [stk(K, 2, EA), st('약화', 1, EA), draw(1)], [                     // 연료 — 꿀범벅을 넓게
      O([stk(K, 2, EA), st('약화', 1, EA), draw(1)]),                                // (수치)
      O([stk(K, 3, E1), draw(1)]),                                                   // 한 적의 발밑
      O([stk(K, 1, EA), make(JAR, 1)], { cost: 0 }),                                 // 꿀단지 하나 두고
      O([stk(K, 2, EA), sh(0.6)]),                                                   // 꿀벽
      O([stk(K, 2, EA), heal(0.5), draw(1)]),  // 꿀 나눔
    ], [B('draw'), B('cost'), B(BK)]),
    U(`${H}_u5`, '비상 꿀단지', 0, '스킬', [make(JAR, 1), draw(1)], [                                       // 원작 자유 — 쥬비에게 털어 온 꿀 수십 통
      O([make(JAR, 1), draw(1)]),                                                    // (수치)
      O([make(JAR, 2)]),  // 두 통
      O([stk(K, 1, EA), draw(1), heal(0.2)]),  // 꿀 냄새
      O([make(JAR, 1), heal(0.3)]),                                                  // 낚시 간식
      O([make(JAR, 1), st('약화', 1, E1)]),                                          // 롤렛과 서커스
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, { ...kw0(j),
    passives: [
      pas('꿀 주먹', 'play', [stk(K, 1, EA)], { when: { every: 4 } }),
      pas('베니는 베니야', 'spend', [heal(0.4)], { when: { id: K } }),
    ],
    ult: { name: '베니는 꿀도 나눠', cost: 300, fx: [dmg(2.2, E1), per(K, { n: 1 }), dmg(0.5, E1), spendAll(K)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 네르(빡침) — 쌓고 고르기 · 딜러(엘다인) ──
// 엔진: 대신 맞은 분노가 빡침(주는 피해 +8%/개) — 다섯이면 심판의 도끼창 1장 → 거대 도끼창으로 다 쓸까, 양익의 맹세로 하나씩 나눠 쓸까 · 성전 모드는 고학년만 연다
function nerAngry(j) {
  // 팀: 「성전 선포」 — 이번 턴 파티 받는 피해를 줄이고 그 분노를 빡침으로(휘장이 대신 맞는다)
  // 원작: 저학년(휘장 · 범위 피해 · 아군이 받는 피해 25%를 대신) · 고학년(도끼창 범위 · 빡침 상태 평타 3타) · 강화 평타(지난 시간 준 피해에 비례해 폭발) · 에르핀의 보모
  const H = '네르_빡침', K = '빡침', AXE = `${H}_axe`;
  const basics = [
    S(`${H}_s1`, '도끼창 휘두르기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '도끼창 내려찍기', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '보모의 팔', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [T(`${H}_f1`, '격노의 참격', H, '공격', [dmg(0.36, E1, { hits: 3 })], { cost: 1, blurb: '성전 모드의 세 번 베기입니다' }), tok0(j, AXE)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '교주님한테서 손 떼!', 1, '공격', [dmg(0.6, E1), stk(K, 2)], [                           // 시동 — 분노의 공명
      O([dmg(0.6, E1), stk(K, 2)]),                                                  // (수치)
      O([dmg(0.5, E1), stk(K, 1), sh(0.5)]),                                         // 막아서며
      O([stk(K, 2), draw(1)], { cost: 0 }),                                          // 벽돌 개그
      O([dmg(0.4, EA), stk(K, 1)]),                                                  // 마카롱 던지기
      O([dmg(0.8, E1), st('취약', 1, E1)]),                                          // 사제장의 일갈
    ], null),
    U(`${H}_u1`, '성전 선포', 1, '공격', [dmg(0.6, EA), st('피해 감소', 1), stk(K, 2)], [                    // 연료 — 원작 저학년(휘장 · 대신 맞기)
      O([dmg(0.6, EA), st('피해 감소', 1), stk(K, 2)]),                              // (수치)
      O([st('피해 감소', 1), stk(K, 2)], { cost: 0 }),  // 휘장만 세우기
      O([dmg(0.8, E1), stk(K, 2)]),                                                  // 한 놈을 향한 선포
      O([dmg(0.5, EA), st('피해 감소', 1), draw(1)]),                                // 서류 더미 위 선포
      O([dmg(0.5, EA), sh(0.6), stk(K, 1)]),                                         // 보모의 품
    ], [B('power'), B('guard'), B(BK)]),
    U(`${H}_u2`, '거대 도끼창', 2, '공격', [dmg(1.0, EA), spendAll(K), perEvent, dmg(0.3, EA)], [            // 결정타 — 모은 빡침을 한 번에
      O([dmg(1.0, EA), spendAll(K), perEvent, dmg(0.3, EA)]),                        // (수치)
      O([dmg(1.3, E1), spendAll(K), perEvent, dmg(0.45, E1)]),                       // 한 놈을 두 쪽
      O([dmg(0.9, EA), spendN(K, 2), perEvent, dmg(0.3, EA)]),                       // 둘만 쓰기
      O([dmg(1.0, EA), spendAll(K), perEvent, dmg(0.3, EA)], { cost: 1 }),  // 몰아친 분노
      O([dmg(0.8, EA), spendAll(K), perEvent, sh(0.3)]),                             // 분노를 방패로
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '양익의 맹세', 1, '공격', [dmg(1.0, E1), spendN(K, 1), ifStack(K), draw(1)], [               // 원작 자유 — 폴랑과의 양날개 · 나눠 쓰기
      O([dmg(1.0, E1), spendN(K, 1), ifStack(K), draw(1)]),                          // (수치)
      O([dmg(0.5, EA), spendN(K, 1), draw(1)]),  // 양날개 휘두르기
      O([dmg(0.8, E1), stk(K, 1)]),                                                  // 맹세를 다지며
      O([dmg(1.1, E1), spendN(K, 1), st('약화', 1, E1)]),                            // 한 번 더 경고
      O([dmg(0.9, E1), spendN(K, 1), ifStack(K), draw(2)]),  // 다음 날개
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j), forms: forms0(j),
    ult: { name: '교단의 심판', cost: 250, fx: [dmg(1.8, EA), stk(K, 2), form(`${H}_성전`)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 마요(멋짐) — 쌓고 고르기 · 딜러 ──
// 엔진: 다른 아군이 스킬을 낼 때마다 자랑하고 싶음(주는 피해 +6%/개 — 쌓아 둔 채로도 이득) → 「최강의 수집품임.」으로 털면 턴 수만큼 무작위 튕김 · 여섯이면 열 번 튕기는 방패
function mayoCool(j) {
  // 팀: 「칭찬 요구」 — 동료 스킬마다 자랑 +1(아군의 스킬이 마요의 수집품을 깨운다)
  // 원작: 저학년(다섯 번 튕기는 방패 · 다른 아군이 저학년을 쓸 때마다 자랑 · 10이면 열 번) · 강화 평타(중열 아군 스킬 피해↑) · 아껴 두던 수집품을 쓸까(성장 서사)
  const H = '마요_멋짐', K = '자랑하고 싶음', TEN = `${H}_ten`;
  const basics = [
    S(`${H}_s1`, '방패 밀치기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '방패 풀스윙', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '경매장 방패', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [tok0(j, TEN)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '빨리 나 칭찬해줌.', 1, '강화', [stk(K, 1), draw(1), pw('turnStart', [stk(K, 1)])], [       // 시동 — 칭찬 요구
      O([stk(K, 1), draw(1), pw('turnStart', [stk(K, 1)])]),                         // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                // 조용한 자랑
      O([stk(K, 2), pw('play', [stk(K, 1)], { when: { type: '공격', who: 'other' } })]), // 뭐든 칭찬해 줌
      O([stk(K, 1), draw(1), pw('turnStart', [dmod(0.08, OTHERS)])]),  // 스킬 피해↑(강화 평타)
      O([stk(K, 1), pw('turnStart', [draw(1)])]),  // 송편 반 포장
    ], null, ['개전']),
    U(`${H}_u3`, '최강의 수집품임.', 1, '공격', [dmg(0.36, ER), spendAll(K), perEvent, dmg(0.36, ER)], [     // 결정타 — 원작 저학년(튕기는 방패)
      O([dmg(0.36, ER), spendAll(K), perEvent, dmg(0.36, ER)]),                      // (수치)
      O([spendAll(K), perEvent, dmg(0.3, EA)]),                                      // 다 같이 감상
      O([dmg(0.36, ER), spendN(K, 3), perEvent, dmg(0.4, ER)]),                      // 셋만 꺼내기
      O([spendAll(K), perEvent, dmg(0.45, E1)]),                                     // 한 놈에게 자랑
      O([spendAll(K), perEvent, dmg(0.35, ER), st('약화', 1, ER)]),  // 종소리 섞기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u5`, '수집품 발동', 1, '스킬', [stk(K, 1), draw(2, { who: 'other' })], [                         // 연료 — 동료 스킬을 부른다
      O([stk(K, 1), draw(2, { who: 'other' })]),                                     // (수치)
      O([draw(2, { who: 'other' }), ifStack(K, 5), st('사기', 1)]),                  // 아껴 두는 보너스
      O([stk(K, 2), sh(0.6)]),                                                       // 수집품과 대화
      O([stk(K, 1), draw(1, { who: 'other' })], { cost: 0 }),                       // 한 점만 보여 줌
      O([stk(K, 1), dmod(0.2, OTHERS), draw(1)]),  // 중열 강화(원작)
    ], [B('draw'), B('ap'), B(BK)]),
    U(`${H}_u2`, '은방울꽃 종', 1, '공격', [dmg(1.0, E1), st('약화', 1, E1), stk(K, 1)], [                  // 원작 자유 — 아끼던 종
      O([dmg(1.0, E1), st('약화', 1, E1), stk(K, 1)]),                               // (수치)
      O([dmg(0.5, EA), st('약화', 1, EA)]),                                          // 크게 울리기
      O([dmg(0.8, E1), stk(K, 2)]),                                                  // 자랑스러운 종소리
      O([dmg(0.7, E1), st('기절', 1, E1)]),  // 비장의 수단임
      O([dmg(0.8, E1), heal(0.4), stk(K, 1)]),  // 맑은 소리
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '비장의 수단임', cost: 150, fx: [dmg(1.0, EA), st('약화', 1, EA), stk(K, 1)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 에르핀(왕도) — 쌓고 고르기 · 딜러(엘다인) ──
// 엔진: 턴마다 · 처치마다 왕마력(주는 피해 +8%/개) → 「마력 난타」로 바닥까지 쏟되 쓰러뜨리면 다시 채워 이어 간다 · 「반쪽 빵」으로 동료에게 나눠 준다
function erpinKing(j) {
  // 팀: 「반쪽 빵」 — 왕마력을 동료에게 나눠(그 아군 다음 공격↑) · 왕마력이 차면 파티 다음 카드 +50%
  // 원작: 저학년 「마력 난타」(SP 가 바닥날 때까지 · 처치하면 SP 를 돌려받아 더) · 고학년 「에르피엔 펀치」(8연타 · 반동 엉덩방아) · 진짜 여왕 · 빵을 다 나눠 주고 하나만 남긴 일
  const H = '에르핀_왕도', K = '왕마력';
  const basics = [
    S(`${H}_s1`, '왕마력탄', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '대왕마력탄', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '여왕의 예복', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '축복의 왕관', 0, '스킬', [stk(K, 1), draw(1)], [                                         // 시동 — 대관식
      O([stk(K, 1), draw(1)]),                                                       // (수치)
      O([stk(K, 1), sh(0.5)]),                                                       // 왕관의 무게
      O([stk(K, 2)]),  // 세계수의 힘
      O([draw(2)]),  // 맞춤법 나머지 공부
      O([stk(K, 1), empower(0.2, 'any')]),                                           // 여왕의 축복
    ], null),
    U(`${H}_u2`, '마력 난타', 0, '공격', [per(K, { n: 1 }), dmg(0.5, ER), spendAll(K), ifKill, stk(K, 2)], [  // 결정타 — 원작 저학년(바닥까지 · 처치하면 이어서)
      O([per(K, { n: 1 }), dmg(0.5, ER), spendAll(K), ifKill, stk(K, 2)]),            // (수치)
      O([per(K, { n: 1 }), dmg(0.55, E1), spendAll(K), ifKill, stk(K, 2)]),           // 한 놈만 겨냥
      O([per(K, { n: 1 }), dmg(0.45, ER), spendN(K, 3)]),                             // 셋만 쏘기
      O([per(K, { n: 1 }), dmg(0.4, ER), heal(0.3)]),                    // 끝나면 회복(원작)
      O([per(K, { n: 1 }), dmg(0.35, EA), spendAll(K)]),  // 왕마력 폭풍
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u5`, '반쪽 빵', 0, '스킬', [stk(K, 1), feed(1, ALLY), draw(1, { who: 'other' })], [               // 연료 — 빵을 나눠 주는 여왕
      O([stk(K, 1), feed(1, ALLY), draw(1, { who: 'other' })]),                       // (수치)
      O([feed(2, ALLY), draw(1)]),                                                    // 두 조각 나눔
      O([stk(K, 1), dmod(0.15, ALLY)]),                                               // 힘내라는 한 조각
      O([stk(K, 2), draw(1, { who: 'other' })]),  // 마지막 하나는 내 것
      O([feed(1, ALLY), heal(0.4), draw(1)]),  // 따뜻한 빵
    ], [B('draw'), B('ap'), B(BK)]),
    U(`${H}_u1`, '에르피엔 왕마력탄', 1, '공격', [dmg(1.1, E1), stk(K, 1)], [                                 // 원작 자유 — 「에르핀 펀치」 의 왕도판
      O([dmg(1.1, E1), stk(K, 1)]),                                                   // (수치)
      O([dmg(0.5, EA), stk(K, 1)]),                                                   // 왕도의 포격
      O([dmg(0.9, E1), stk(K, 2)]),                                                   // 망설임 없는 한 발
      O([dmg(1.0, E1), tough(1)]),                                                    // 힘 조절 실패
      O([dmg(0.9, E1), ifKill, stk(K, 3)]),                                           // 쓰러뜨리고 다시
    ], [B('power'), B('weakSpot'), B(BK)], ['약점 공격']),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '에르피엔 펀치', cost: 300, fx: [dmg(0.2, EA, { hits: 4 }), tough(2), stk(K, 3)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 셰이디(역전) — 쌓고 고르기 · 서포터 ──
// 엔진: 착한 척(제 스킬) · 참는 것(맞기)마다 울분(파티 주는 피해 +5%/개 — 셋이면 파티 다음 카드 +60%) → 인형 샌드백으로 풀까, 쓰다듬기로 파티를 거들까
function shadyRev(j) {
  // 팀: 「헤벌레 쓰다듬기」 — 울분을 털어 파티 회복 · 아군 주머니(키워드) +1
  // 원작: 저학년 「차원의 틈」(호박 회복 · 레이저 피해 감소 · 냥의자 기절 중 하나) · 강화 평타(위험한 아군 풀회복 + 인형 던지기) · 어사이드 「유령 인형 샌드백」 · 늪의 파수꾼
  const H = '셰이디_역전', K = '울분', PUMP = `${H}_pumpkin`;
  const basics = [
    S(`${H}_s1`, '파수 사슬', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '늪지 등불 온기', 1, '스킬', [heal(1.5)]),
  ];
  const tokens = [tok0(j, PUMP)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u4`, '내 방식대로 지키겠어', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1)])], [         // 시동 — 파수꾼의 다짐
      O([st('사기', 1), pw('turnStart', [stk(K, 1)])]),                              // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                // 참는 하루
      O([stk(K, 1), st('사기', 1), pw('spend', [stk(K, 1)], { when: { who: 'other' } })]),  // 남이 쓰면 나도 울분
      O([st('사기', 2), stk(K, 1)], { cost: 0, power: false }),                      // 썰렁 개그
      O([st('사기', 1), pw('turnStart', [heal(0.15)])], { cost: 0 }),  // 늪 순찰
    ], null, ['개전']),
    U(`${H}_u1`, '차원의 틈', 1, '스킬', [stk(K, 1), ifChoice(1), make(PUMP, 2), ifChoice(2), st('피해 감소', 2)], [ // 연료 — 원작 저학년(소품 셋 중 하나)
      O([stk(K, 1), ifChoice(1), make(PUMP, 2), ifChoice(2), st('피해 감소', 2)]),     // (수치)
      O([stk(K, 1), make(PUMP, 1), draw(1)]),                                         // 스피키의 호박
      O([stk(K, 1), st('피해 감소', 2), draw(1)]),  // 레테의 레이저
      O([dmg(0.6, E1), st('기절', 1, E1)]),                                           // 앨리스의 냥의자
      O([stk(K, 2), make(PUMP, 1)]),  // 차원문 가챠
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u2`, '유령 인형 샌드백', 1, '공격', [dmg(0.6, EA), spendAll(K), perEvent, dmg(0.4, EA)], [       // 결정타 — 울분을 인형에게
      O([dmg(0.6, EA), spendAll(K), perEvent, dmg(0.4, EA)]),                        // (수치)
      O([dmg(0.5, E1), spendAll(K), perEvent, dmg(0.6, E1)]),  // 한 놈 인형 삼아
      O([dmg(0.5, EA), spendN(K, 1), perEvent, dmg(0.4, EA)]),                        // 하나만 풀기
      O([spendAll(K), perEvent, dmg(0.4, EA), st('취약', 2, EA)], { cost: 0 }),  // 사일런트 크래쉬
      O([spendAll(K), perEvent, dmg(0.45, EA), st('협공', 2)]),  // 같이 패자
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u5`, '헤벌레 쓰다듬기', 1, '스킬', [spendAll(K), perEvent, heal(0.5), feed(1)], [               // 원작 자유 — 림에게 대박 난 썰렁 개그 · 파수꾼의 다정함
      O([spendAll(K), perEvent, heal(0.5), draw(1)]),  // 다 쓰다듬고 다음
      O([spendAll(K), perEvent, sh(0.5), feed(1)]),  // 품에 안기
      O([heal(0.8), feed(1)]),                                                       // 울분은 둔다
      O([spendAll(K), perEvent, heal(0.5), cleanse(1)], { cost: 0 }),  // 위험한 아군 풀회복(원작)
      O([spendAll(K), perEvent, heal(0.4), feed(1)], { cost: 0 }),  // 슬쩍 쓰다듬기
    ], [B('heal'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '사일런트 크래쉬', cost: 200, fx: [dmg(0.4, EA, { hits: 2 }), st('취약', 1, EA), st('약화', 1, EA)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`], basics, tokens, uniques });
}

// ── 나이아 — 쌓고 고르기 · 서포터 ──
// 엔진: 스킬을 낼 때마다 · 동료가 키워드를 쓸 때마다 물탱크(파티 주는 피해 +7%/개) → 「물총 연사」로 몇 발 실을지 고르거나, 「같이 놀자!」로 친구 물통(아군 키워드)에 붓는다
function naia(j) {
  // 팀: 「같이 놀자!」 — 아군 하나의 주머니 키워드 +1(남의 주머니를 채우는 유일한 사도) · 물탱크가 차면 파티 다음 카드 +50%
  // 원작: 저학년 「그게 씻은거야?」(HP 가장 낮은 아군 20번 나눠 씻김) · 고학년 「물의 세례를 받아라!」(아군 회복 · 적 피해 파도) · 강화 평타 물총 세 발 · 어사이드(고학년 두 번) · 혼자가 싫은 물총 장난
  const H = '나이아', K = '물탱크';
  const basics = [
    S(`${H}_s1`, '물방울', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '물 뿌리기', 1, '스킬', [heal(1.3)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '같이 놀자!', 0, '스킬', [stk(K, 1), feed(1, ALLY), draw(1, { who: 'other' })], [            // 시동 — 친구 물통 채우기
      O([stk(K, 1), feed(1, ALLY), draw(1, { who: 'other' })]),                       // (수치)
      O([feed(2, ALLY), draw(1)]),  // 물통 가득
      O([stk(K, 2), draw(1)]),                                                        // 내 탱크부터
      O([feed(1, ALLY), heal(0.4), draw(1)]),  // 얼굴에 물총
      O([stk(K, 1), dmod(0.1, ALLY), draw(1)]),                                       // 해적놀이
    ], null),
    U(`${H}_u3`, '물총 연사', 1, '공격', [dmg(0.35, E1), spendAll(K), perEvent, dmg(0.4, E1)], [             // 결정타 — 몇 발을 실을지
      O([dmg(0.35, E1), spendAll(K), perEvent, dmg(0.4, E1)]),                        // (수치)
      O([dmg(0.3, E1), spendN(K, 2), perEvent, dmg(0.45, E1)]),                       // 두 발만
      O([dmg(0.3, EA), spendAll(K), perEvent, dmg(0.25, EA)]),  // 물대포
      O([spendAll(K), perEvent, heal(0.3), draw(1)], { cost: 0 }),  // 물을 친구에게
      O([spendAll(K), perEvent, dmg(0.35, E1), st('약화', 1, E1)]),  // 세 발 연사(강화 평타)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '그게 씻은거야?', 1, '스킬', [heal(1.1), cleanse(1), stk(K, 1)], [                            // 연료 — 원작 저학년(HP 가장 낮은 아군 씻기기)
      O([heal(1.1), cleanse(1), stk(K, 1)]),                                          // (수치)
      O([heal(0.9), cleanse(2), stk(K, 1)]),  // 박박 씻기
      O([heal(0.9), stk(K, 2)]),                                                      // 결벽증 대청소
      O([heal(0.7), cleanse(1), draw(1)], { cost: 0 }),                               // 대충 헹구기
      O([heal(0.8), cleanse(1), feed(1, ALLY)]),                                      // 씻고 같이 놀자
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u4`, '외로운 수문장', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1)])], [                    // 원작 자유 — 모두에게 공평하게 장난치는 수문장
      O([st('사기', 1), pw('turnStart', [stk(K, 1)])]),                              // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                // 혼자 지키는 문
      O([st('사기', 1), pw('turnStart', [heal(0.2)])], { cost: 0 }),  // 호수 대청소
      O([stk(K, 2), draw(1), pw('spend', [stk(K, 1)], { when: { who: 'other' } })]),  // 물장난 상대
      O([st('사기', 2), pw('turnStart', [stk(K, 1)])], { cost: 2 }),                 // 공평한 수문장
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '물의 세례를 받아라!', cost: 150, fx: [dmg(0.9, EA), heal(2.2), cleanse(1), stk(K, 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, uniques });
}

// ── 블랑셰 — 쌓고 고르기 · 딜러 ──
// 엔진: 공격마다 적에게 푸른 장미(받는 피해 +8%/겹)를 피우고 · 넷이 피면 커튼콜 1장 → 「장미 정원 산책」이 장미를 다 터뜨린다 · 앙코르로 장미 둘
function blanche(j) {
  // 팀: 푸른 장미가 핀 적은 파티 누구에게나 더 아프다(받는 피해 +8%/겹 — 무대를 깔아 둔다)
  // 원작: 저학년 「싱크로즈」(안 맞은 적을 먼저 · 3명에게 튕김 · 막타 확정 치명) · 고학년 「파랑새 정원」(가장 먼 적 3번 · 범위 치명 · 침묵) · 애착판(푸른 장미 10중첩 · 막타에 터뜨림) · 슬픈 연기만 하다 웃음을 잊은 배우
  const H = '블랑셰', K = '푸른 장미', CC = `${H}_t1`;
  const basics = [
    S(`${H}_s1`, '장미 가시', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '가시 장미 다발', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '꽃잎 커튼', 1, '스킬', [sh(1.6)]),
  ];
  const tokens = [tok0(j, CC)];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u3`, '앙코르', 0, '스킬', [stk(K, 2, E1), draw(1)], [                                           // 시동 — 무대에 장미
      O([stk(K, 2, E1), draw(1)]),                                                   // (수치)
      O([stk(K, 1, EA), draw(1)]),                                                   // 객석 전체에
      O([stk(K, 3, E1)]),  // 꽃다발
      O([stk(K, 1, E1), draw(2)]),  // 두 번째 앙코르
      O([stk(K, 2, TOP), sh(0.4)]),                                                  // 주연에게
    ], null),
    U(`${H}_u1`, '싱크로즈', 1, '공격', [dmg(0.25, ER, { hits: 3 }), stk(K, 1, ER)], [                           // 연료 — 원작 저학년(3명에게 튕김)
      O([dmg(0.25, ER, { hits: 3 }), stk(K, 1, ER)]),                                // (수치)
      O([dmg(0.22, ER, { hits: 3 }), stk(K, 1, EA)]),                                // 안 맞은 적부터
      O([dmg(0.3, ER, { hits: 3 }), { k: 'critMod', v: 1, target: 'self' }, dmg(0.3, E1)], { cost: 2 }), // 막타 확정 치명
      O([dmg(0.5, E1), stk(K, 2, E1)]),                                              // 한 적에게 집중
      O([dmg(0.25, ER, { hits: 3 }), draw(1)]),                                      // 다음 장면
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u2`, '장미 정원 산책', 1, '공격', [dmg(0.9, E1), per(K, { n: 1 }), dmg(0.25, E1)], [   // 결정타 — 피운 장미를 다 터뜨린다
      O([dmg(0.9, E1), per(K, { n: 1 }), dmg(0.25, E1)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.35, E1), spendAll(K), st('약화', 1, E1)]),          // 침묵(행동 지연)
      O([per(K, { n: 1 }), dmg(0.35, E1), spendAll(K)]),  // 다 꺾기
      O([per(K, { n: 1, each: true }), dmg(0.22, EA), spendAll(K), draw(1)]),  // 정원 전체
      O([{ k: 'critMod', v: 1, target: 'self' }, dmg(0.7, E1), per(K, { n: 1 }), dmg(0.2, E1)], { cost: 2 }), // 커튼콜 치명
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '무대 위의 배우', 1, '강화', [{ k: 'critMod', v: 0.15, run: true }, pw('stackReach', [draw(1)], { when: { id: K, n: 4 } })], [ // 원작 자유 — 웃음을 잊은 배우
      O([{ k: 'critMod', v: 0.15, run: true }, pw('stackReach', [draw(1)], { when: { id: K, n: 4 } })]), // (수치)
      O([pw('play', [stk(K, 1, E1)], { when: { type: '스킬' } })], { cost: 0 }),      // 슬픈 연기
      O([{ k: 'critMod', v: 0.2, run: true }, pw('turnStart', [stk(K, 1, E1)])]),    // 머리 위 장미
      O([pw('stackReach', [draw(1), make(CC, 1)], { when: { id: K, n: 4 } })]),  // 우승한 순간의 웃음
      O([{ k: 'critMod', v: 0.15, run: true }, pw('kill', [draw(1), stk(K, 1, EA)])]),  // 모노레일 출퇴근
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '파랑새 정원', cost: 200, fx: [dmg(0.4, FAR, { hits: 3 }), make(CC, 1), stk(K, 1, EA)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
  for (const e of j.equips || []) e.affinityEffect = [{ name: '파랑새의 표식', when: { on: 'fightStart' }, fx: [stk(K, 1, EA)] }];
}

// ── 아일라 — 쌓고 고르기 · 서포터 ──
// 엔진: 파티 실드가 깨질 때마다 빠직(방어 +10%/개, 다섯 단계 쫌 → 초폭망)과 화산 정령의 일격 → 「화산섬의 주인」으로 대폭망에서 털까, 초폭망까지 참을까
function ayla(j) {
  // 팀: 「휴가 중 잠적」 — 파티 실드(화산 보호막)를 깔아 두면 깨질 때마다 화산 정령이 적을 친다(탱커 · 실드 사도와 맞물림)
  // 원작: 저학년 「휴가 중 잠적」(아군 전체 화산 보호막 · 깨지면 화산 정령 · 약화) · 고학년 「활활화산」(아군 보호막이 깨질 때마다 빠직 · 전부 써서 대분화) · 분노 다섯 단계 · 느긋한 수호 정령
  const H = '아일라', K = '빠직';
  const basics = [
    S(`${H}_s1`, '용암 방울', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '휴양지 온천', 1, '스킬', [heal(1.3)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '휴가 중 잠적', 1, '스킬', [sh(1.5), stk(K, 1)], [                                          // 시동 — 원작 저학년(화산 보호막)
      O([sh(1.5), stk(K, 1)]),                                                       // (수치)
      O([sh(1.1), st('약화', 1, EA)]),                                               // 숨어서 약 올리기
      O([sh(1.2), draw(1)]),                                                         // 느긋하게
      O([sh(0.9), stk(K, 2)], { cost: 0 }),                                          // 벨라를 피해 숨기
      O([sh(1.2), st('실드 유지', 1)]),                                              // 오래 가는 보호막
    ], null),
    U(`${H}_u4`, '화산섬의 주인', 1, '공격', [per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)], [                 // 결정타 — 대분화
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)]),                              // (수치)
      O([per(K, { n: 1 }), dmg(0.5, E1), spendAll(K)]),                              // 화산암(가장 센 적)
      O([per(K, { n: 1 }), heal(0.3), spendAll(K)], { cost: 0 }),  // 화를 삭이고
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K), st('약화', 1, EA)]),          // 섬이 터진다
      O([per(K, { n: 1 }), dmg(0.28, EA)]),                                          // 아직 참는다
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u3`, '화를 삭이는 명상', 1, '스킬', [spendN(K, 1), heal(0.9), sh(0.4)], [                          // 연료 — 빠직을 조금씩 내려놓기
      O([spendN(K, 1), heal(0.9), sh(0.4)]),                                         // (수치)
      O([heal(0.8), stk(K, 1)]),                                                     // 명상 실패
      O([spendN(K, 1), sh(1.0)]),                                                    // 차분한 보호막
      O([spendN(K, 1), heal(0.6), draw(1)], { cost: 0 }),                            // 깊은 호흡
      O([heal(0.7), cleanse(1)]),                                                    // 너무 급하면 늦는다
    ], [B('heal'), B('guard'), B(BK)]),
    U(`${H}_u5`, '영구 추방령', 1, '공격', [dmg(1.1, E1), ifStack(K, 3), st('기절', 1, E1)], [                 // 원작 자유 — 무례한 손님은 추방
      O([dmg(1.1, E1), ifStack(K, 3), st('기절', 1, E1)]),                           // (수치)
      O([dmg(0.9, E1), stk(K, 1)]),                                                  // 걍 화남
      O([dmg(0.6, EA), stk(K, 1), ifStack(K, 3), st('약화', 1, EA)]),  // 섬 전체 추방
      O([dmg(1.0, E1), st('취약', 2, E1)]),                                          // 출입 금지
      O([dmg(1.0, E1), sh(0.7), stk(K, 1)]),  // 화산 장벽
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '활활화산', cost: 300, fx: [sh(2.4), dmg(0.8, EA), per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, uniques });
}

// ── 델리아 — 대가형 · 딜러 ──
// 엔진: 수족열증(적 — 턴 끝 지속 피해 · 델리아에게 받는 피해↑)을 퍼뜨리고 · 대가를 치를 때마다 다음 카드 +30% → 「이글루 배치기」가 치른 HP 만큼 · 「이글루 도망」으로 숨을 때를 고른다
function delia(j) {
  // 팀: 「펭귄 탐험대」 — 다른 아군이 공격하면 수족열증 1(탐험대 대장)
  // 원작: 저학년(점프 범위 · 수족열증 · 날아차기 침묵) · 고학년 「도와줘 친구들아!」(범위 · 긴 기절) · 강화 평타(민초 아이스크림 회복 · 흡혈) · 어사이드(HP 50% 밑이면 이글루) · 맨몸으로 바다를 헤엄친 용기
  const H = '델리아', K = '수족열증';
  const basics = [
    S(`${H}_s1`, '펭귄 박치기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '대왕 펭귄 박치기', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '뒤뚱 방어', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '델리아 화났어!', 1, '공격', [dmg(0.45, EA), stk(K, 1, EA)], [                               // 시동 — 원작 저학년(점프 · 수족열증)
      O([dmg(0.45, EA), stk(K, 1, EA)]),                                             // (수치)
      O([dmg(0.8, E1), stk(K, 2, E1)]),                                              // 날아차기
      O([dmg(0.4, EA), st('순수 약점', 2, E1)]),  // 수족열증 — 순수(델리아) 약점
      O([stk(K, 1, EA), draw(1)], { cost: 0 }),                                      // 화난 척
      O([dmg(0.4, EA), stk(K, 1, EA), sh(0.4)]),                                     // 뒤뚱 착지
    ], null),
    U(`${H}_u3`, '이글루 배치기', 2, '공격', [payHp(30), dmg(1.5, E1), perPaid(40), dmg(0.35, E1)], [            // 결정타 — 치른 만큼 배치기
      O([payHp(30), dmg(1.5, E1), perPaid(40), dmg(0.35, E1)]),                       // (수치)
      O([payHp(30), dmg(0.8, EA), perPaid(40), dmg(0.2, EA)]),                        // 이글루째 굴리기
      O([dmg(1.6, E1), stk(K, 1, E1)]),                                               // 치르지 않는 배치기
      O([payHp(40), dmg(1.8, E1), st('기절', 1, E1)], { cost: 3 }),                   // 대왕 배치기
      O([payHp(30), dmg(1.3, E1), heal(0.4)]),                                        // 민초 한 입(흡혈)
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '이글루 도망', 1, '스킬', [cleanse(1), heal(0.9), ifWounded, st('피해 감소', 2)], [            // 연료 — 숨을 때를 고른다
      O([cleanse(1), heal(0.9), ifWounded, st('피해 감소', 2)]),                      // (수치)
      O([heal(0.7), sh(0.7)]),                                                        // 이글루 벽
      O([cleanse(1), heal(0.6), empower(0.5)]),                                       // 숨었다가 배치기
      O([heal(1.2), ifWounded, draw(1)]),                                             // 민초 아이스크림
      O([cleanse(2), st('피해 감소', 1)], { cost: 0 }),                               // 쏙 숨기
    ], [B('heal'), B('guard'), B(BK)]),
    U(`${H}_u4`, '일어나! 탐험 가야지!', 0, '스킬', [nextAp(-1), ap(1), draw(1)], [                             // 원작 자유 — 탐험대 대장
      O([nextAp(-1), ap(1), draw(1)]),                                                // (수치)
      O([nextAp(-1), ap(1), stk(K, 1, EA)]),                                          // 출발 신호
      O([draw(2)]),  // 탐험 지도
      O([nextAp(-2), ap(2), draw(1)]),  // 밤샘 탐험
      O([nextAp(-1), ap(2), heal(0.3)]),  // 맨몸 수영
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '도와줘 친구들아!', cost: 200, fx: [dmg(1.4, EA), st('기절', 1, E1), stk(K, 1, EA)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 비비 — 대가형 · 탱커(엘다인) ──
// 엔진: 제 HP 를 깎아 수은 재채기 — 적 전체 수은 중독(턴 끝 지속 피해) · 치를 때마다 은빛 맹세(방어↑ · 넷이면 파티 다음 카드 +40%) → 은빛 창 네 자루가 중독을 다 쓴다
function bibi(j) {
  // 팀: 「소녀가 보호하겠사와요」 — 대가를 치를 때마다 은빛 맹세(치른 만큼 실드로 돌려받는 탱커)
  // 원작: 저학년(자기 수은 보호막 · 깨지면 적 전체 피해 · 방어 감소) · 고학년 「퀵실버 랜스」(가장 먼 적 · 공격력 감소 · 도발) · 만성 수은 몸살 · 재채기 한 번에 주변 중독 · 실비아의 어머니
  const H = '비비', K = '수은 중독';
  const basics = [
    S(`${H}_s1`, '은빛 발톱', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '귀족의 품위', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u2`, '독성 재채기', 1, '공격', [payPct(0.03), ddef(0.4, EA), stk(K, 1, EA)], [                      // 시동 — 재채기 한 번에 주변이 중독
      O([payPct(0.03), ddef(0.4, EA), stk(K, 1, EA)]),                               // (수치)
      O([ddef(0.35, EA), stk(K, 1, EA)]),                                            // 참은 재채기
      O([payPct(0.03), stk(K, 2, EA)], { cost: 0 }),                                 // 연속 재채기
      O([payPct(0.03), ddef(0.6, E1), stk(K, 2, E1)]),                               // 한 사람에게 에취
      O([payPct(0.03), ddef(0.35, EA), sh(0.6)]),                                    // 은빛 손수건
    ], null),
    U(`${H}_u1`, '소녀에게 오시려구요?', 1, '스킬', [sh(1.2), perPaid(40), sh(0.14), stk(K, 1, E1)], [            // 연료 — 원작 저학년(수은 보호막)
      O([sh(1.2), perPaid(40), sh(0.14), stk(K, 1, E1)]),                            // (수치)
      O([sh(1.0), st('실드 유지', 1), stk(K, 1, E1)]),                               // 오래 가는 보호막
      O([sh(0.9), pw('shieldBreak', [ddef(0.3, EA)])], { power: true }),             // 깨지면 적 전체(원작)
      O([sh(1.0), st('피해 감소', 1)]),                                              // 귀족의 방패
      O([sh(0.8), draw(1)], { cost: 0 }),                                            // 사와요
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u3`, '은빛 창 네 자루', 1, '공격', [ddef(0.45, E1), per(K, { n: 1 }), ddef(0.2, E1), spendAll(K)], [  // 결정타 — 중독을 창으로
      O([ddef(0.45, E1), per(K, { n: 1 }), ddef(0.2, E1), spendAll(K)]),             // (수치)
      O([per(K, { n: 1 }), ddef(0.2, EA), spendAll(K)]),                             // 창 네 자루를 흩뿌려
      O([ddef(0.4, E1), per(K, { n: 1 }), ddef(0.18, E1)]),                          // 중독은 둔다
      O([per(K, { n: 1 }), ddef(0.3, FAR), spendAll(K)]),                            // 가장 먼 적(퀵실버)
      O([ddef(0.45, E1), per(K, { n: 1 }), ddef(0.2, E1), tough(1)]),  // 은의 꿰뚫기
    ], [B('power'), B('cost'), B(BK)], ['분쇄']),
    U(`${H}_u4`, '세계수 뿌리에 수은을', 1, '공격', [st('결의', 1), ddef(0.28, EA), per(K, { n: 1 }), ddef(0.14, EA)], [ // 원작 자유 — 명예 서열 굴욕 뒤의 복수
      O([st('결의', 1), ddef(0.28, EA), per(K, { n: 1 }), ddef(0.14, EA)]),          // (수치)
      O([ddef(0.35, EA), stk(K, 1, EA), st('결의', 1)]),  // 뿌리째
      O([st('결의', 2), stk(K, 1, EA)], { cost: 0 }),                                // 사랑의 엘다인
      O([payPct(0.03), per(K, { n: 1 }), ddef(0.3, EA)]),            // 제 몸까지 바쳐
      O([ddef(0.35, EA), per(K, { n: 1 }), ddef(0.18, EA), st('결의', 1)]),  // 힐데의 실험 대상
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '퀵실버 랜스', cost: 200, fx: [ddef(1.0, FAR), tough(2, FAR), st('고통', 5, FAR)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, uniques });
}

// ── 라이카 — 대가형 · 딜러 ──
// 엔진: 제 몸에 전류를 꽂아(HP 대가 → 감전 · 과충전) 주먹을 무겁게 — 과충전 1개당 주는 피해 +20%, 셋이면 다음 카드 +50% → 감전 질주가 과충전을 다 쏟는다 · 최대 출력은 고학년이 연다
function laika(j) {
  // 팀: 감전 질주 · 원격 충전이 적 전체에 충격(동료의 다음 공격이 더 아프다)
  // 원작: 저학년 「급속 충전!」(스스로 감전 · 그동안 해로움 면역 · 질주 +200%) · 고학년 「원격 충전!!」(착지 범위 · 기절 · 최대 출력) · 정전을 막으려 과출력하다 방전돼 입원 · 220V 콘센트 사랑
  const H = '라이카', K = '과충전';
  const basics = [
    S(`${H}_s1`, '찌릿 펀치', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '콰지직 펀치', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '정전기 가드', 1, '스킬', [sh(1.6)]),
  ];
  const tokens = [tok0(j, `${H}_f1`)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '급속 충전!', 1, '강화', [payPct(0.02), stk(K, 2), pw('turnStart', [payPct(0.01), stk(K, 1)])], [ // 시동 — 원작 저학년(스스로 감전)
      O([payPct(0.02), stk(K, 2), pw('turnStart', [payPct(0.01), stk(K, 1)])]),     // (수치)
      O([stk(K, 1), pw('turnStart', [stk(K, 1)])]),                                  // 콘센트에 꽂기(대가 없이)
      O([payPct(0.03), stk(K, 2), pw('turnStart', [stk(K, 1)])]),  // 한 번에 충전
      O([payPct(0.02), stk(K, 1), pw('turnStart', [payPct(0.01), stk(K, 1), draw(1)])]), // 아침 충전
      O([stk(K, 1), cleanse(1), pw('turnStart', [stk(K, 1)])]),  // 해로움 면역(원작)
    ], null, ['개전']),
    U(`${H}_u2`, '감전 질주', 1, '공격', [dmg(0.55, EA), per(K, { n: 1 }), dmg(0.15, EA), spendAll(K)], [       // 결정타 — 과충전을 쏟는 질주
      O([dmg(0.55, EA), per(K, { n: 1 }), dmg(0.15, EA), spendAll(K)]),              // (수치)
      O([dmg(0.8, E1), per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)]),                // 일직선 질주
      O([dmg(0.5, EA), per(K, { n: 1 }), dmg(0.12, EA)]),                            // 아껴 둔 전류
      O([dmg(0.45, EA), st('충격', 1, EA), spendAll(K)]),                            // 감전된 적들
      O([dmg(0.55, EA), per(K, { n: 1 }), dmg(0.15, EA), heal(0.3)]),  // 결림이 풀린다
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '풀 스윙', 1, '공격', [dmg(0.6, E1), per(K, { n: 1 }), dmg(0.3, E1)], [                         // 연료 — 무거워진 주먹
      O([dmg(0.6, E1), per(K, { n: 1 }), dmg(0.3, E1)]),                             // (수치)
      O([payPct(0.02), dmg(0.8, E1), stk(K, 1)]),                                    // 몸을 던지는 스윙
      O([dmg(0.6, E1), st('충격', 1, E1), stk(K, 1)]),                               // 220V 스트레이트
      O([dmg(0.5, EA), per(K, { n: 1 }), dmg(0.1, EA)]),                             // 휘두르는 전류
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.25, E1), tough(1)]),                  // 콰지직
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u5`, '전기 지짐이', 0, '스킬', [payPct(0.03), stk(K, 1), pull({ type: '공격' })], [                  // 원작 자유 — 전기 지짐이 원조
      O([payPct(0.03), stk(K, 1), pull({ type: '공격' })]),                           // (수치)
      O([stk(K, 1), draw(1)]),                                                       // 대가 없는 지짐이
      O([payPct(0.03), stk(K, 2)]),                                                  // 바삭하게
      O([payPct(0.02), pull({ type: '공격' }), draw(1)]),  // 두 장 부치기
      O([payPct(0.03), stk(K, 1), heal(0.3)]),                                       // 실라에게 대접
    ], [B('draw'), B('ap'), B(BK)]),
  ];
  const kw = kw0(j); kw.keywords = (kw.keywords || []).filter(k => k.name !== '감전');   // 감전은 옛 패시브만 쓰던 것 — 새 패시브에서 뺐다
  if (!kw.keywords.length) delete kw.keywords;
  hero(j, { ...kw, passives: pas0(j), forms: forms0(j),
    ult: { name: '원격 충전!!', cost: 200, fx: [dmg(1.5, EA), st('충격', 2, EA), stk(K, 1), form(`${H}_최대출력`)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

export const JOBS = [['마녀/벨리타', velita], ['수인/베니_베니', beni], ['요정/네르_빡침', nerAngry], ['요정/마요_멋짐', mayoCool], ['요정/에르핀_왕도', erpinKing], ['유령/셰이디_역전', shadyRev],
  ['정령/나이아', naia], ['정령/블랑셰', blanche], ['정령/아일라', ayla], ['수인/델리아', delia], ['용족/비비', bibi], ['정령/라이카', laika]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
