// 묶음 7 — 회복형 5 + 돈형 6 + 소환형 3. node _gen/deep/b7.mjs [사도 일부]
// 고유 효과 · 패시브는 10-08 갈래 재설계(원작 근거)를 이어 쓰는 사도가 많다(kw0 · pas0). 고유 4장 · 신탁 · 기본 카드 · 고학년은 새로.
// 돈형: 전투 안 골드 효과는 엔진에 없어 「돈」 은 지금처럼 각자의 고유 효과(금고 · 수수료 · 주가 …)로 센다.
import * as L from './lib.mjs';
const { E1, EA, ER, TOP, LOW, FAR, ALLY, ALLIES, OTHERS, dmg, ddef, sh, heal, st, stk, spendAll, spendN, per, perTag, draw, make, gauge,
  ifStack, ifKill, empower, tough, dmod, cost, pw, pas, O, B, U, S, T, hero } = L;
const cleanse = v => ({ k: 'cleanse', v });
const roll = n => ({ k: 'roll', n }), ifRoll = n => ({ k: 'ifRoll', n });
const perOver = { k: 'perOverheal' }, perEvent = { k: 'perEvent' }, ifWounded = { k: 'ifWounded' };
const drain = r => ({ k: 'drain', ratio: r });
const kw0 = j => { const h = j.heroes[0]; return { keyword: h.keyword, ...(h.keywords ? { keywords: h.keywords } : {}) }; };
const pas0 = j => j.heroes[0].passives;
const tok0 = (j, id) => j.cards.find(c => c.id === id);
const fx0 = (j, id) => JSON.parse(JSON.stringify(j.cards.find(c => c.id === id).fx));

// ── 포셔 — 회복형 · 서포터 ──
// 엔진: 센 약에는 부작용 — 카드를 낼 때마다 적에게 부작용(주는 피해 -4%/겹) · 넘친 회복은 남은 약을 적에게 뿌린다 → 흐물 약화 포션이 부작용만큼 회복
function porsche(j) {
  // 팀: 「남은 약은 적에게」 — 아군 회복이 넘치면 적 전체 부작용 · 손의 쓴맛을 씻고 드로우
  // 원작: 저학년 「무슨 포션 줄까?」(초록 큰 회복 · 빨강 피해 · 노랑 피해 + 기절) · 고학년 「감자 고구마!」 · 효과가 굉장한 만큼 반드시 따르는 부작용 · 입에 쓴 게 몸에 좋다
  const H = '포셔', K = '부작용';
  const basics = [
    S(`${H}_s1`, '약병 투척', 1, '공격', [dmg(0.9, E1)]),
    S(`${H}_s2`, '초록 물약', 1, '스킬', [heal(1.4)]),
  ];
  const tokens = [tok0(j, `${H}_t1`)];
  const BK = [stk(K, 1, E1)];
  const uniques = [
    U(`${H}_u1`, '무슨 포션 줄까?', 1, '스킬', [roll(3), ifRoll(1), heal(1.2), ifRoll(2), dmg(0.8, E1), ifRoll(3), st('기절', 1, E1)], [ // 시동 — 원작 저학년(셋 중 하나)
      O([roll(3), ifRoll(1), heal(1.2), ifRoll(2), dmg(0.8, E1), ifRoll(3), st('기절', 1, E1)]), // (수치)
      O([heal(1.0), stk(K, 1, E1), draw(2)]),  // 초록 포션
      O([dmg(0.7, E1), stk(K, 2, E1)]),                                              // 빨강 포션
      O([dmg(0.5, E1), st('기절', 1, E1), stk(K, 1, E1)]),  // 노랑 포션
      O([roll(2), ifRoll(1), heal(1.1), ifRoll(2), dmg(0.8, E1), draw(1)]),          // 회복 포션 확률↑
    ], null),
    U(`${H}_u2`, '중독 포션 두 병', 1, '공격', [dmg(0.3, E1, { hits: 2 }), stk(K, 2)], [                         // 연료 — 부작용을 쌓는다
      O([dmg(0.3, E1, { hits: 2 }), stk(K, 2)]),                                    // (수치)
      O([dmg(0.25, EA), stk(K, 1, EA)]),                                             // 흩뿌리기
      O([dmg(0.3, E1, { hits: 2 }), st('약화', 1, E1), stk(K, 1)]),  // 흐물 성분
      O([stk(K, 2), draw(1)], { cost: 0 }),                                          // 임상실험(레비)
      O([dmg(0.3, E1, { hits: 2 }), stk(K, 1), heal(0.3)]),                          // 해독제 값은 따로
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '흐물 약화 포션', 1, '스킬', [st('약화', 2, E1), per(K, { n: 1 }), heal(0.22), spendAll(K)], [  // 결정타 — 부작용만큼 아군 회복
      O([st('약화', 2, E1), per(K, { n: 1 }), heal(0.22), spendAll(K)]),              // (수치)
      O([per(K, { n: 1 }), sh(0.3), spendAll(K), draw(1)]),  // 쓴맛 방패
      O([st('약화', 1, EA), per(K, { n: 1 }), heal(0.2)], { cost: 0 }),  // 부작용은 둔다
      O([per(K, { n: 1 }), heal(0.2), spendAll(K), cleanse(1)], { cost: 0 }),  // 해독
      O([per(K, { n: 1 }), dmg(0.3, E1), spendAll(K)]),                               // 부작용 폭발
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u4`, '약장수의 비법서', 1, '공격', [dmg(0.4, E1), per(K, { n: 1 }), dmg(0.16, E1)], [               // 원작 자유 — 커스텀 포션 비법
      O([dmg(0.4, E1), per(K, { n: 1 }), dmg(0.16, E1)]),                             // (수치)
      O([dmg(0.5, E1), stk(K, 2)]),                                                   // 새 처방
      O([dmg(0.35, EA), per(K, { n: 1 }), heal(0.1)]),                                // 모두의 포션(어사이드)
      O([dmg(0.4, E1), per(K, { n: 1 }), dmg(0.14, E1), draw(1)]),                    // 비법서 넘기기
      O([per(K, { n: 1 }), dmg(0.25, E1)], { cost: 0 }),                              // 급처방
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '감자 고구마!', cost: 150, fx: [st('기절', 1, E1), st('기절', 1, ER), stk(K, 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, tokens, uniques });
}

// ── 피코라 — 회복형 · 서포터 ──
// 엔진: 카드를 낼 때마다 스티커(턴 끝 회복 · 아군 받는 피해↓) → 회복이 넘치면 가장 센 아군에게 인증 스티커(주는 피해 +8%/개) · 쇼핑왕 피코라가 스티커만큼 회복
function picora(j) {
  // 팀: 「인증 완료」 — 넘친 회복을 가장 센 아군의 인증 스티커로(멋진 것엔 인증 스티커)
  // 원작: 저학년 「한정 스티커」(HP 낮은 아군 셋 회복 · 받는 회복↑ · 물리 피해↓) · 고학년 「너도 될 수 있다 패션피플」 · 구린 건 스티커로 덮는 수습 마녀 · 「나빴어요」 스티커
  const H = '피코라', K = '스티커', A = '인증 스티커';
  const basics = [
    S(`${H}_s1`, '요술봉 톡', 1, '공격', [dmg(0.9, E1)]),
    S(`${H}_s2`, '반짝 파우더', 1, '스킬', [heal(1.6)]),
  ];
  const tokens = [tok0(j, `${H}_sticker`)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '한정 스티커', 1, '스킬', [heal(0.9), st('피해 감소', 1), stk(K, 1)], [                         // 시동 — 원작 저학년
      O([heal(0.9), st('피해 감소', 1), stk(K, 1)]),                                 // (수치)
      O([heal(1.1), stk(K, 1)]),                                                     // 받는 회복↑
      O([heal(0.7), st('피해 감소', 1), draw(1)]),                                   // 물리 피해↓
      O([heal(0.6), stk(A, 1, 'strongestAlly')], { cost: 0 }),                       // 한정판 인증
      O([heal(0.8), stk(K, 2)]),                                                     // 덤 스티커
    ], null),
    U(`${H}_u1`, '반짝이 스티커', 1, '스킬', [heal(0.7), stk(K, 2)], [                                          // 연료 — 스티커 붙이기
      O([heal(0.7), stk(K, 2)]),                                                     // (수치)
      O([heal(0.5), stk(K, 1), draw(1)]),                                            // 패션 잡지
      O([sh(0.8), stk(K, 2)]),                                                       // 반짝이 갑옷
      O([stk(K, 2), make(`${H}_sticker`, 1)], { cost: 0 }),                          // 쇼핑백(어사이드)
      O([heal(0.6), stk(A, 1, 'strongestAlly'), stk(K, 1)]),                         // 멋진 것엔 인증
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u4`, '쇼핑왕 피코라', 1, '스킬', [per(K, { n: 1 }), heal(0.3), stk(A, 1, ALLY)], [         // 결정타 — 스티커만큼 회복
      O([per(K, { n: 1 }), heal(0.3), stk(A, 1, ALLY)]),  // (수치)
      O([per(K, { n: 1 }), sh(0.25), stk(A, 1, ALLY)]),                              // 패션피플 방패
      O([per(K, { n: 1 }), heal(0.3), draw(1)]),  // 쇼핑 다녀오기
      O([per(K, { n: 1 }), heal(0.3), spendAll(K), draw(1)]),  // 몽땅 붙여 주기
      O([per(K, { n: 1 }), heal(0.3), cleanse(2)]),  // 구린 건 덮기
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u3`, '나빴어요 스티커', 1, '공격', [dmg(0.5, E1), per(K, { n: 1 }), dmg(0.2, E1), st('약화', 1, E1)], [ // 원작 자유 — 놀린 상대 등에 몰래 붙이는 스티커
      O([dmg(0.5, E1), per(K, { n: 1 }), dmg(0.2, E1), st('약화', 1, E1)]),          // (수치)
      O([dmg(0.6, E1), st('약화', 2, E1)]),                                          // 큼직하게
      O([dmg(0.3, EA), st('약화', 1, EA), stk(K, 1)]),  // 모두에게 몰래
      O([dmg(0.5, E1), stk(K, 1), draw(1)]),                                         // 붙이고 도망
      O([per(K, { n: 1 }), dmg(0.3, E1), spendAll(K), st('취약', 1, E1)]),           // 해골 스티커 도배
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '너도 될 수 있다 패션피플', cost: 200, fx: [sh(3.0), stk(K, 2), st('피해 감소', 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, tokens, uniques });
}

// ── 디아나 — 회복형 · 서포터 ──
// 엔진: 때리는 치료 — 공격할 때마다 파티 회복 · 회복이 넘치면 기혈(턴 끝 회복)과 넘친 만큼 기공 → 자연 치유가 기혈만큼 · 다섯이면 다음 카드 +50%(눈을 뜨면 강해진다)
function diana(j) {
  // 팀: 넘친 치유가 무작위 적 피해로(진짜 치료법) · 잔칫상 주먹밥은 누구든 먹으면 기혈
  // 원작: 저학년(아군 전체 회복 두 번) · 고학년 「진정한 치료법」(기의 격류 11타 · 넉백) · 확률로 피해를 주며 가장 다친 아군을 회복하는 평타 · 실눈 촌장 · 「눈 감아 주는 것도 한계가 있다」
  const H = '디아나', K = '기혈', RICE = `${H}_rice`;
  const basics = [
    S(`${H}_s1`, '지팡이 톡', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '에구구', 1, '스킬', [heal(1.4)]),
  ];
  const tokens = [tok0(j, RICE)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '기공 주문', 1, '공격', [dmg(1.1, E1), drain(0.5), stk(K, 1)], [                                // 시동 — 때리면서 고치기
      O([dmg(1.1, E1), drain(0.5), stk(K, 1)]),                                      // (수치)
      O([dmg(0.9, E1), stk(K, 2)]),                                                  // 기를 모으며
      O([dmg(0.5, EA), drain(0.3), stk(K, 1)]),  // 기의 파장
      O([dmg(1.0, E1), drain(0.4), draw(1)]),                                        // 실눈의 여유
      O([dmg(1.0, E1), st('약화', 2, E1), stk(K, 1)]),  // 웬만하면 안 쓰려 했는데
    ], null),
    U(`${H}_u1`, '자연 치유', 1, '스킬', [per(K, { n: 1 }), heal(0.45), spendAll(K), st('결의', 1)], [            // 결정타 — 원작 저학년(전체 회복 두 번)
      O([per(K, { n: 1 }), heal(0.45), spendAll(K), st('결의', 1)]),                 // (수치)
      O([per(K, { n: 1 }), heal(0.35), sh(0.3), stk(K, 1)]),  // 기혈은 둔다
      O([per(K, { n: 1 }), heal(0.4), spendAll(K), cleanse(1)], { cost: 0 }),  // 묵은 기를 풀어
      O([heal(0.8), stk(K, 1)], { cost: 0 }),                                         // 한 번 더 회복
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)]),                              // 눈을 뜬 치료
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u3`, '수인 마을 잔칫상', 2, '스킬', [heal(1.4), make(RICE, 2)], [                                     // 연료 — 촌장의 잔칫상
      O([heal(1.4), make(RICE, 2)]),                                                 // (수치)
      O([heal(1.0), make(RICE, 1)], { cost: 1 }),                                    // 소박한 상
      O([heal(1.2), make(RICE, 2), stk(K, 1)]),                                      // 기를 북돋는 상
      O([make(RICE, 3), draw(1)]),                                                   // 주먹밥 잔뜩
      O([heal(1.2), st('사기', 1), make(RICE, 1)]),  // 마을 잔치
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u5`, '해바라기 톡톡', 0, '공격', [dmg(0.5, E1), perOver, dmg(0.15, E1)], [                             // 원작 자유 — 넘친 치유를 기공으로
      O([dmg(0.5, E1), perOver, dmg(0.15, E1)]),                                     // (수치)
      O([dmg(0.4, E1), heal(0.3)]),                                                  // 톡 치고 쓰담
      O([dmg(0.5, E1), stk(K, 1)]),                                                  // 해바라기 씨
      O([perOver, dmg(0.1, EA), stk(K, 1)], { cost: 0 }),  // 넘친 기의 폭발
      O([dmg(0.4, E1), draw(1)]),                                                    // 촌장의 장난
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '진정한 치료법', cost: 200, fx: [dmg(0.15, EA, { hits: 4 }), st('둔화', 2, EA), st('기절', 1, E1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, tokens, uniques });
}

// ── 힐데 — 회복형 · 서포터 ──
// 엔진: 카드마다 진료 차트 → 셋이면 진료 소견서 · 피톤치드 파동이 차트만큼 회복하고 넘친 만큼 실드(간호복) · 의료인 보호법이 넘친 회복을 실드로
function hilde(j) {
  // 팀: 넘친 회복이 그 아군의 실드로(원작 아티팩트 「힐데의 간호복」)
  // 원작: 저학년 「피톤치드 파동」(주변 아군 회복 · HP 80% 미만 둘에게 지속 회복) · 고학년 「과잉진료」(광역 · 적 주는 물리 피해↓) · 「더 센 병이 돌아야」 · 만년 레지던트
  const H = '힐데', K = '진료 차트';
  const basics = [
    S(`${H}_s1`, '청진기 톡', 1, '공격', [dmg(0.9, E1)]),
    S(`${H}_s2`, '처방전', 1, '스킬', [heal(1.6)]),
  ];
  const tokens = [tok0(j, `${H}_note`)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '정밀 회진', 1, '스킬', [stk(K, 1), heal(0.7), draw(1)], [                                       // 시동 — 일단 처방
      O([stk(K, 1), heal(0.7), draw(1)]),                                            // (수치)
      O([stk(K, 2), heal(0.5)]),                                                     // 꼼꼼한 차트
      O([heal(0.6), st('초재생', 1)]),                                               // 지속 회복(원작)
      O([stk(K, 1), draw(2)]),  // 야간 당직
      O([stk(K, 1), heal(0.5), sh(0.4)]),                                            // 과잉 처방
    ], null),
    U(`${H}_u1`, '피톤치드 파동', 1, '스킬', [per(K, { n: 1 }), heal(0.5), spendAll(K)], [       // 결정타 — 원작 저학년 · 넘친 만큼 실드
      O([per(K, { n: 1 }), heal(0.5), spendAll(K)]),  // (수치)
      O([per(K, { n: 1 }), heal(0.4), spendAll(K), st('초재생', 1)]),                 // HP 80% 미만에게 지속 회복
      O([per(K, { n: 1 }), heal(0.4), perOver, sh(0.12)]),                            // 차트는 남긴다
      O([per(K, { n: 1 }), sh(0.3), spendAll(K)]),                                    // 예방 접종
      O([per(K, { n: 1 }), heal(0.4), cleanse(1)]),  // 확진 · 처방
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u3`, '의료인 보호법', 1, '강화', [sh(0.7), pw('overheal', [sh(0.8)])], [                               // 연료 — 간호복(넘친 회복 → 실드)
      O([sh(0.7), pw('overheal', [sh(0.8)])]),                                        // (수치)
      O([pw('overheal', [sh(0.6), stk(K, 1)])], { cost: 0 }),                         // 넘치면 기록
      O([sh(0.6), pw('turnStart', [heal(0.2)])]),                                     // 회진 일정
      O([sh(0.5), pw('overheal', [sh(0.6), draw(1)])], { cost: 2 }),                  // 꾀병 환자도 받아요
      O([st('피해 감소', 1), pw('overheal', [sh(0.7)])]),                             // 병원 수칙
    ], [B('guard'), B('cost'), B(BK)]),
    U(`${H}_u5`, '판을 뒤집는 수', 1, '공격', [dmg(0.65, EA), st('약화', 1, EA), ifWounded, st('초재생', 1)], [     // 원작 자유 — 고학년 「과잉진료」
      O([dmg(0.65, EA), st('약화', 1, EA), ifWounded, st('초재생', 1)]),             // (수치)
      O([dmg(0.5, EA), st('약화', 2, EA)]),                                          // 적 주는 물리 피해↓
      O([dmg(0.9, E1), heal(0.4), stk(K, 1)]),  // 주사기총 단발
      O([dmg(0.25, ER, { hits: 3 }), stk(K, 1)]),                                    // 주사기총 난사
      O([dmg(0.6, EA), st('사기', 1)]),                                              // 더 센 병이 돌아야
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '과잉진료', cost: 300, fx: [dmg(1.6, EA), st('약화', 2, EA), st('사기', 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u2`], basics, tokens, uniques });
}

// ── 다야(퓨어샤인) — 회복형 · 서포터 ──
// 엔진: 퓨어☆영역(턴 끝 회복)을 깔고 · 이번 턴 넷째 카드마다 창피한 포즈로 파티 회복 · 넘친 회복은 반짝임 → 넷이면 파티 다음 카드 +40% · 1초 컷이 넘친 회복만큼
function dayaPure(j) {
  // 팀: 「퓨어☆포즈」 — 누가 내든 넷째 카드마다 파티 회복(원작 4타째 포즈)
  // 원작: 저학년 「매지컬☆샤인」(중열 보호막 · 공속 · 회복 영역) · 고학년 「퓨어☆샤이닝 빔」(받는 보호막↑ · 가장 먼 적 빔) · 4타째 포즈 · 매일 쓰는 편지 · 창피한 변신 대사
  const H = '다야_퓨어샤인', K = '퓨어☆영역', BR = '반짝임';
  const basics = [
    S(`${H}_s1`, '별빛 스틱', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '반짝 반창고', 1, '스킬', [sh(1.5)]),
  ];
  const tokens = [tok0(j, `${H}_finish`)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u4`, '매지컬☆샤인 영역', 1, '강화', [st('결의', 1), pw('turnStart', [stk(K, 1)])], [                    // 시동 — 영역 전개
      O([st('결의', 1), pw('turnStart', [stk(K, 1)])]),                              // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                // 몰래 변신
      O([stk(K, 2), pw('turnStart', [heal(0.2)])], { cost: 0 }),  // 편지 쓰는 아침
      O([st('결의', 1), stk(K, 1), pw('turnStart', [stk(K, 1), sh(0.3)])]),  // 메타모르포제
      O([stk(K, 1), pw('overheal', [stk(BR, 1), heal(0.1)])], { cost: 0 }),  // 넘치는 반짝임
    ], null, ['개전']),
    U(`${H}_u1`, '매지컬☆샤인', 1, '스킬', [sh(1.1), heal(0.5), stk(K, 2)], [                                       // 연료 — 원작 저학년
      O([sh(1.1), heal(0.5), stk(K, 2)]),                                            // (수치)
      O([sh(1.2), st('피해 감소', 1), stk(K, 1)]),  // 중열 보호막
      O([heal(0.8), stk(K, 2)]),                                                     // 회복 영역
      O([sh(0.8), stk(K, 1), draw(1)]),                                              // 창피해도 히어로
      O([sh(0.9), heal(0.4), stk(BR, 1)]),                                           // 반짝반짝
    ], [B('guard'), B('heal'), B(BK)]),
    U(`${H}_u3`, '스타☆샤인', 1, '스킬', [per(K, { n: 1 }), heal(0.5), spendAll(K), st('피해 감소', 1)], [              // 결정타 — 영역을 한꺼번에
      O([per(K, { n: 1 }), heal(0.5), spendAll(K), st('피해 감소', 1)]),              // (수치)
      O([per(K, { n: 1 }), sh(0.4), spendAll(K), stk(BR, 1)]),  // 별빛 보호막
      O([per(K, { n: 1 }), heal(0.4), st('피해 감소', 1)]),                           // 영역은 둔다
      O([per(K, { n: 1 }), heal(0.4), stk(BR, 2)]),  // 넘치는 별빛
      O([per(K, { n: 1 }), dmg(0.3, FAR), spendAll(K)]),                              // 가장 먼 적에게 빔
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u5`, '1초 컷', 1, '공격', [dmg(0.75, E1), perOver, dmg(0.18, E1), stk(BR, 1)], [                         // 원작 자유 — 창피함을 1초 만에
      O([dmg(0.75, E1), perOver, dmg(0.18, E1), stk(BR, 1)]),                        // (수치)
      O([dmg(0.9, FAR), stk(BR, 1)]),                                                // 샤이닝 피니시
      O([dmg(0.6, E1), heal(0.4), stk(BR, 1)]),  // 포즈 한 번
      O([dmg(0.4, EA), stk(K, 1)]),                                                  // 별빛 흩뿌리기
      O([dmg(0.7, E1), stk(BR, 2)], { cost: 2 }),                                    // 편지 속 각오
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j),
    passives: [
      pas('퓨어☆포즈', 'play', [heal(0.45), stk(K, 1)], { when: { who: 'any', nth: 4 } }),
      ...pas0(j).filter(p => p.name !== '퓨어☆포즈'),
    ],
    ult: { name: '퓨어☆샤이닝 빔', cost: 250, fx: [sh(2.5), st('결의', 1), dmg(0.6, FAR), st('사기', 1)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`], basics, tokens, uniques });
}

// ── 코미(수영복) — 돈형 · 서포터 ──
// 엔진: 주스 재고가 턴마다 차고(넘치면 코미 주스 1장) · 누가 스킬을 내든 주스를 팔아(재고 1 → 회복) 이용료를 받는다 → 특제 코미 주스가 재고만큼
function komiSwim(j) {
  // 팀: 「판매 개시」 — 아군이 스킬을 낼 때마다 주스 한 잔(회복)
  // 원작: 평타 「수제 주스 판매」(HP 가장 낮은 전열 아군 회복 · 가까운 적 피해) · 어사이드(간이 가판대 · HP 낮은 아군 4명에게 주스) · 리조트 사장 · 맹물을 코미 주스라고 판다
  const H = '코미_수영복', K = '주스 재고', JU = `${H}_juice`;
  const basics = [
    S(`${H}_s1`, '물총 한 발', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '수제 주스 한 잔', 1, '스킬', [heal(1.4)]),
  ];
  const tokens = [tok0(j, JU)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '주스 한 병 투척', 1, '공격', [dmg(0.8, E1), heal(0.5), stk(K, 1)], [                            // 시동 — 평타(판매)
      O([dmg(0.8, E1), heal(0.5), stk(K, 1)]),                                       // (수치)
      O([dmg(0.6, E1), stk(K, 2)]),                                                  // 재고 들여오기
      O([dmg(0.7, E1), make(JU, 1)]),                                                // 시음 한 병
      O([dmg(0.4, EA), heal(0.3), stk(K, 1)]),  // 물총 세례
      O([heal(0.7), stk(K, 1)], { cost: 0 }),                                         // 맹물 판매
    ], null),
    U(`${H}_u1`, '물놀이 좋은거 아니야?', 1, '스킬', [heal(0.9), st('사기', 1)], [                                // 연료 — 원작 저학년
      O([heal(0.9), st('사기', 1)]),                                                 // (수치)
      O([heal(0.7), stk(K, 1), draw(1)]),                                            // 물장구
      O([heal(0.6), st('사기', 1), make(JU, 1)]),                                    // 리조트 무료 음료
      O([sh(0.8), st('사기', 1)]),                                                   // 튜브 방패
      O([heal(0.7), st('피해 감소', 1), stk(K, 1)]),  // 받는 피해↓(어사이드)
    ], [B('heal'), B('draw'), B(BK)]),
    U(`${H}_u2`, '특제 코미 주스', 1, '스킬', [per(K, { n: 1 }), heal(0.4), spendAll(K)], [                         // 결정타 — 재고 떨이
      O([per(K, { n: 1 }), heal(0.4), spendAll(K)]),                                 // (수치)
      O([per(K, { n: 1 }), sh(0.35), spendAll(K)]),                                  // 아이스박스
      O([per(K, { n: 1 }), heal(0.3), spendN(K, 2)]),                                // 두 병만
      O([per(K, { n: 1 }), heal(0.3), make(JU, 1)]),  // 한 병은 남겨 판다
      O([per(K, { n: 1 }), dmg(0.25, ER), spendAll(K)]),                             // 맹물 폭탄
    ], [B('heal'), B('cost'), B(BK)]),
    U(`${H}_u4`, '조기 매진!', 1, '강화', [stk(K, 2), pw('spend', [draw(1)], { when: { id: K } })], [                  // 원작 자유 — 블랙 기업 사장
      O([stk(K, 2), pw('spend', [draw(1)], { when: { id: K } })]),                    // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                 // 매일 입고
      O([stk(K, 1), pw('spend', [heal(0.15), draw(1)], { when: { id: K } })]),  // 고객 감사
      O([stk(K, 3), pw('spend', [draw(1), sh(0.2)], { when: { id: K } })]),  // 리조트 재개발
      O([stk(K, 1), pw('turnStart', [make(JU, 1)])]),                                 // 코미 코인
    ], [B('draw'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '풀장 후보지라구', cost: 200, fx: [dmg(0.2, ER, { hits: 3 }), st('약화', 1, EA), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`], basics, tokens, uniques });
}

// ── 아이시아 — 돈형 · 딜러 ──
// 엔진: 공격 · 처치마다 회사 금고 → 여섯이면 신상 냉장고 2장 · 「신제품 시연」이 금고를 털어 냉장고 · 「해고야!」가 금고만큼 · 냉장고를 만들면 의도치 않은 선행(파티 회복)
function icea(j) {
  // 팀: 「의도치 않은 선행」 — 신상을 만들 때마다 파티 회복(악덕 CEO 의 착각 선행)
  // 원작: 저학년 「신제품 시연」(냉장고 4번 범위) · 고학년(냉각 미사일 기절 · 난사 · 받는 마법 피해↑) · 어사이드(아이스박스) · 프로스트 노바 회장 · 「너 해고야」 · 몽구스형 단타
  const H = '아이시아', K = '회사 금고', FR = `${H}_fridge`;
  const basics = [
    S(`${H}_s1`, '냉기탄', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '급속 냉동 광선', 2, '공격', [dmg(2.3, E1)]),
    S(`${H}_s3`, '법인 방패', 1, '스킬', [sh(1.8)]),
  ];
  const tokens = [tok0(j, FR)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u2`, '내가 누군지 몰라?', 0, '스킬', [stk(K, 2), draw(1)], [                                           // 시동 — 금고 채우기
      O([stk(K, 2), draw(1)]),                                                       // (수치)
      O([stk(K, 1), make(FR, 1)]),                                                   // 신상 하나 꺼내기
      O([stk(K, 3)], { cost: 0 }),  // 뇌물
      O([stk(K, 1), sh(0.5)]),                                                       // 법인 보호
      O([stk(K, 1), draw(1), st('약화', 1, E1)]),                                    // 돈다발로 때리기
    ], null),
    U(`${H}_u1`, '신제품 시연', 1, '공격', [dmg(0.25, EA, { hits: 2 }), ifStack(K, 3), spendN(K, 3), make(FR, 2)], [ // 연료 — 원작 저학년(냉장고 떨어뜨리기)
      O([dmg(0.25, EA, { hits: 2 }), ifStack(K, 3), spendN(K, 3), make(FR, 2)]),       // (수치)
      O([dmg(0.25, EA, { hits: 2 }), st('약점 노출', 2, E1), stk(K, 1)]),  // 받는 마법 피해↑ — 약점 노출(고학년)
      O([dmg(0.5, E1, { hits: 2 }), stk(K, 1), draw(1)]),  // 단독 시연
      O([dmg(0.2, EA, { hits: 2 }), make(FR, 1)]),                                     // 시제품
      O([dmg(0.25, EA, { hits: 2 }), later(1, [dmg(0.5, EA), st('기절', 1, E1)])]),    // 아이스박스(어사이드)
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '해고야!', 1, '공격', [per(K, { n: 1 }), dmg(0.45, E1), spendAll(K)], [               // 결정타 — 금고만큼 해고
      O([per(K, { n: 1 }), dmg(0.45, E1), spendAll(K)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.22, EA), make(FR, 1)]),  // 구조 조정
      O([dmg(0.9, E1), per(K, { n: 1 }), dmg(0.2, E1)]),                              // 금고는 지킨다
      O([dmg(0.9, E1), per(K, { n: 1 }), dmg(0.2, E1), ifKill, stk(K, 3)]),  // 퇴직금 아끼기
      O([per(K, { n: 1 }), dmg(0.4, E1), heal(0.3)]),  // 의도치 않은 선행
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '정복, 멜루나!', 1, '공격', [dmg(0.9, E1), perTag(FR), dmg(0.4, E1)], [                             // 원작 자유 — 132차 기업전쟁
      O([dmg(0.9, E1), perTag(FR), dmg(0.4, E1)]),                                    // (수치)
      O([dmg(0.5, EA), perTag(FR), dmg(0.15, EA)]),                                   // 시장 점유
      O([dmg(1.0, E1), stk(K, 2)]),                                                   // 매수
      O([dmg(0.8, E1), make(FR, 1)]),                                                 // 신상 공세
      O([dmg(0.8, E1), perTag(FR), dmg(0.35, E1), st('취약', 1, E1)]),                // 유물 가로채기
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: 'CEO의 비밀 병기', cost: 250, fx: [st('기절', 1, E1), dmg(1.5, EA), stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}
const later = (n, then) => ({ k: 'later', n, then });

// ── 시스트 — 돈형 · 딜러 ──
// 엔진: 처치 · 격파마다 수수료(주는 피해 +10%/개) · 손패를 떨이로 팔아(소멸) 수수료 → 「총알 배송」이 가장 약한 적을 저격하고 처치하면 특급 배송을 이어 간다
function sist(j) {
  // 팀: 처치가 이어지는 동안 특급 배송(동료가 깎아 둔 적을 저격으로 마무리)
  // 원작: 저학년 「총알 배송」(HP 비율 가장 낮은 적 · 처치하면 SP 가 다 차 최대 6번) · 고학년 「플렉스 건」 · 일반 상점 NPC · 싼 걸 비싸게 판다 · 바보세
  const H = '시스트', K = '수수료', EX = `${H}_express`;
  const basics = [
    S(`${H}_s1`, '한 발', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '가짜 날개', 1, '스킬', [sh(1.5)]),
  ];
  const tokens = [tok0(j, EX)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u4`, '장사천재 시스트', 1, '강화', fx0(j, `${H}_u4`), [                                                 // 시동 — 개업
      O(fx0(j, `${H}_u4`)),                                                          // (수치)
      O([{ k: 'atkMod', v: 0.05, target: 'self' }, stk(K, 2), pw('kill', [stk(K, 1), draw(1)])], { cost: 0 }),  // 정보 판매
      O([{ k: 'atkMod', v: 0.1, target: 'self' }, pw('break', [make(EX, 1)])], { cost: 0 }), // 격파 특가
      O([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0 }),  // 명품백(어사이드)
      O([{ k: 'atkMod', v: 0.08, target: 'self' }, pw('turnStart', [stk(K, 1)])], { cost: 0 }),  // 바보세 선납
    ], null, ['개전']),
    U(`${H}_u1`, '총알 배송', 1, '공격', [dmg(1.5, LOW), stk(K, 1), ifKill, make(EX, 1)], [                          // 결정타 — 원작 저학년(처치하면 이어서)
      O([dmg(1.5, LOW), stk(K, 1), ifKill, make(EX, 1)]),                            // (수치)
      O([dmg(1.3, E1), ifKill, make(EX, 1)]),                                        // 지정 배송
      O([dmg(1.2, LOW), stk(K, 2)]),                                                 // 배송비 선불
      O([dmg(1.3, LOW), ifKill, make(EX, 2)]),                                       // 묶음 배송
      O([dmg(1.1, LOW), tough(1), stk(K, 1)]),                                       // 파손 주의
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u2`, '떨이 판매', 1, '스킬', fx0(j, `${H}_u2`), [                                                          // 연료 — 손패를 팔아 수수료로
      O(fx0(j, `${H}_u2`)),                                                          // (수치)
      O([stk(K, 2), draw(1)], { cost: 0 }),                                          // 반값 떨이
      O([stk(K, 2), make(EX, 1)]),                                                   // 덤 배송
      O([stk(K, 3), sh(0.5)]),                                                       // 담보 잡기
      O([stk(K, 2), draw(2)]),                                                       // 재고 정리
    ], [B('draw'), B('ap'), B(BK)]),
    U(`${H}_u3`, '4차원 바구니', 2, '공격', [dmg(1.2, EA), ifStack(K, 3), spendN(K, 3), make(EX, 2)], [               // 원작 자유 — 뭐든 들어가는 바구니
      O([dmg(1.2, EA), ifStack(K, 3), spendN(K, 3), make(EX, 2)]),                    // (수치)
      O([dmg(1.0, EA), stk(K, 2)]),                                                   // 바구니 채우기
      O([dmg(1.5, E1), ifStack(K, 3), spendN(K, 3), make(EX, 2)]),                    // 한 사람에게 쏟기
      O([dmg(0.6, EA), stk(K, 1)], { cost: 1 }),  // 작은 바구니
      O([dmg(1.1, EA), stk(K, 2)]),  // 몽땅 털기
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '플렉스 건', cost: 150, fx: [{ k: 'atkMod', v: 0.15, target: 'self' }, { k: 'ap', v: 1 }, stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u4`], basics, tokens, uniques });
}

// ── 피라 — 돈형 · 서포터 ──
// 엔진: 적에게 해로운 효과를 걸 때마다 부유함(파티 피해 +8%/개) → 다섯이면 도금 명함 · 위조 금화 뿌리기가 부유함만큼 · 만들 때마다 파티 회복
function pira(j) {
  // 팀: 부유함 1개당 파티 주는 피해 +8% · 도금 경품으로 파티 회복 · 치명(어사이드)
  // 원작: 저학년 「수금 시간이다!」(범위 · 받는 피해↑ · 맞힌 적마다 부유함) · 고학년 「피버☆타임」(파칭코 · 부유함 30이면 2배) · 황철석 위조 금화 사채꾼 · 키디언과의 우정
  const H = '피라', K = '부유함', G1 = `${H}_g1`, G2 = `${H}_g2`;
  const basics = [
    S(`${H}_s1`, '명함 투척', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '경품 증정', 1, '스킬', [sh(1.5)]),
  ];
  const tokens = [tok0(j, G1), tok0(j, G2)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '연금술 실험', 1, '스킬', [st('취약', 1, E1), stk(K, 2), draw(1)], [                               // 시동 — 황철석 연성
      O([st('취약', 1, E1), stk(K, 2), draw(1)]),                                    // (수치)
      O([st('취약', 2, E1), stk(K, 1), draw(1)]),  // 사채 감정
      O([stk(K, 2), make(G2, 1)], { cost: 0 }),                                      // 경품 연성
      O([st('약화', 1, EA), stk(K, 2), draw(1)]),  // 이자 독촉
      O([st('취약', 1, EA), draw(1)]),                                               // 위조 장부
    ], null),
    U(`${H}_u2`, '수금 시간이다!', 1, '공격', [dmg(1.0, EA), st('취약', 1, EA)], [                                   // 연료 — 원작 저학년
      O([dmg(1.0, EA), st('취약', 1, EA)]),                                          // (수치)
      O([dmg(0.8, EA), stk(K, 1)]),                                                  // 맞힌 만큼 부유함
      O([dmg(1.4, E1), st('취약', 2, E1)]),                                          // 한 놈에게 독촉
      O([dmg(0.7, EA), st('취약', 1, EA), draw(1)]),                                 // 조각상 파편
      O([dmg(0.8, EA), st('약화', 1, EA)]),                                          // 협박
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '위조 금화 뿌리기', 1, '공격', [per(K, { n: 1 }), dmg(0.45, ER), spendAll(K)], [        // 결정타 — 부유함을 금화로
      O([per(K, { n: 1 }), dmg(0.45, ER), spendAll(K)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)]),                             // 금화 비
      O([dmg(0.5, ER), per(K, { n: 1 }), dmg(0.35, ER)]),                            // 부유함은 둔다
      O([per(K, { n: 1 }), dmg(0.4, E1), spendAll(K), make(G1, 1)]),                 // 도금 명함 한 장
      O([per(K, { n: 1 }), dmg(0.4, ER), heal(0.3)]),  // 경품 회수
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '화금석 연구', 1, '공격', [dmg(0.7, ER), ifStack(K, 3), spendN(K, 3), make(G2, 2)], [                  // 원작 자유 — 특이한 돌 하나로 빚 전체를
      O([dmg(0.7, ER), ifStack(K, 3), spendN(K, 3), make(G2, 2)]),                    // (수치)
      O([dmg(0.6, ER), make(G2, 1)]),                                                 // 경품 뿌리기(어사이드)
      O([dmg(0.9, E1), stk(K, 2), draw(1)]),  // 황철석 코인
      O([dmg(0.6, ER), ifStack(K, 3), spendN(K, 3), make(G1, 1)]),                    // 바보의 금
      O([dmg(0.5, EA), stk(K, 1), draw(1)]),  // 피버 예열
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '피버☆타임이다!', cost: 200, fx: [dmg(0.3, ER), per(K, { n: 1 }), dmg(0.5, ER), spendAll(K)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, tokens, uniques });
}

// ── 바롱 — 돈형 · 딜러 ──
// 엔진: 뒷담(받는 피해 +12%/겹)을 퍼뜨리고 공격마다 못 추가타 + 장난 밑천 → 사재기로 밑천 셋을 털어 전 재산 장난(적 전체 큰 피해) · 못 날리기가 뒷담만큼
function barong(j) {
  // 팀: 뒷담이 붙은 적은 파티 누구에게나 더 아프다 — 싸움을 붙이는 장난꾼
  // 원작: 저학년(맨 뒤 적에게 저주 · 받은 피해가 다른 적에게 번짐 · 평타가 못 2연타) · 어사이드 「새로운 큰손」 · 주식으로 벌어 장난 한 번에 탕진 · 토끼 인형 「주인님」
  const H = '바롱', G = '뒷담', P = '장난 밑천', PR = `${H}_prank`;
  const basics = [
    S(`${H}_s1`, '유령 손톱', 1, '공격', [dmg(1.1, E1)]),
    S(`${H}_s2`, '으흐흐 흐릿해지기', 1, '스킬', [sh(1.8)]),
    S(`${H}_s3`, '한 맺힌 할퀴기', 2, '공격', [dmg(2.3, E1)]),
  ];
  const tokens = [tok0(j, PR)];
  const BK = [stk(G, 1, E1)];
  const uniques = [
    U(`${H}_u5`, '주식 단타', 0, '스킬', [stk(G, 1, E1), stk(P, 1), draw(1)], [                                     // 시동 — 저점 매수
      O([stk(G, 1, E1), stk(P, 1), draw(1)]),                                        // (수치)
      O([stk(P, 2), draw(1)]),                                                       // 고점 매도
      O([stk(G, 2, E1)], { cost: 0 }),  // 소문 퍼뜨리기
      O([stk(G, 1, FAR), st('약점 노출', 1, FAR)]),                                  // 맨 뒤 적에게 저주(약점 노출)
      O([stk(P, 1), sh(0.5)]),                                                       // 주인님 뒤에 숨기
    ], null),
    U(`${H}_u1`, '뒷담까기 인형', 1, '공격', [dmg(1.2, E1), stk(G, 1, E1), dmg(0.4, 'otherEnemy')], [               // 연료 — 원작 저학년(저주가 번진다)
      O([dmg(1.2, E1), stk(G, 1, E1), dmg(0.4, 'otherEnemy')]),                      // (수치)
      O([dmg(1.0, FAR), stk(G, 2, FAR)]),                                            // 맨 뒤 적 저주
      O([dmg(0.9, E1), stk(G, 1, EA)]),                                              // 사람 사이 갈라놓기
      O([dmg(1.0, E1), stk(P, 2)]),                                                  // 큰손의 투자
      O([dmg(0.4, E1, { hits: 2 }), stk(G, 1, E1)]),                                 // 못 2연타
    ], [B('power'), B('weakSpot'), B(BK)]),
    U(`${H}_u3`, '못 날리기', 1, '공격', [dmg(0.6, E1), per(G, { n: 1 }), dmg(0.5, E1), spendAll(G)], [                // 결정타 — 뒷담만큼 못을
      O([dmg(0.6, E1), per(G, { n: 1 }), dmg(0.5, E1), spendAll(G)]),                // (수치)
      O([per(G, { n: 1 }), dmg(0.3, EA), spendAll(G)]),                              // 못 비
      O([dmg(0.6, E1), per(G, { n: 1 }), dmg(0.45, E1)]),                            // 뒷담은 둔다
      O([per(G, { n: 1 }), dmg(0.6, E1), spendAll(G), stk(P, 2)]),                   // 장난 수익
      O([dmg(0.5, E1), per(G, { n: 1 }), dmg(0.4, E1), dmg(0.3, 'otherEnemy')]),  // 저주 번지기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '사재기', 1, '스킬', [sh(0.9), ifStack(P, 3), spendN(P, 3), make(PR, 1)], [                          // 원작 자유 — 천막 사재기 · 전 재산 장난
      O([sh(0.9), ifStack(P, 3), spendN(P, 3), make(PR, 1)]),                        // (수치)
      O([sh(0.8), stk(P, 2)]),                                                       // 몰래 사 모으기
      O([ifStack(P, 3), spendN(P, 3), make(PR, 1), draw(1)], { cost: 0 }),           // 한탕
      O([sh(0.8), st('약화', 1, EA), stk(P, 1)]),  // 시장 막기
      O([sh(1.0), stk(G, 1, EA)]),                                                   // 두 기업 싸움 붙이기
    ], [B('guard'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '유령불 부르기', cost: 250, fx: [dmg(1.4, EA), stk(G, 3), st('피해 감소', 2)] },
    starter: [`${H}_s1`, `${H}_u5`, `${H}_s3`, `${H}_s2`], basics, tokens, uniques });
}

// ── 멜루나 — 돈형 · 서포터 ──
// 엔진: 턴이 끝날 때마다 · 공격 · 처치마다 주가(파티 피해 +6%/개) — 묻어 둘수록 불어나 다섯이면 파티 다음 카드 +40% · 멜론 씨 기관총이 주가를 털어 · 머스크멜론은 파티 회복 · 협공
function meluna(j) {
  // 팀: 주가 1개당 파티 주는 피해 +6% · 머스크멜론(회복 · 협공)을 손에
  // 원작: 저학년 「올 때 멜루나~」(멜론비 5번 · 준 피해의 300% 만큼 아군 회복) · 고학년(거대 멜론) · 어사이드 「멜론 플렉스」(코인을 더 준다) · 능구렁이형 재벌 · 「팔기 전엔 손해가 아니다」
  const H = '멜루나', K = '주가', ML = `${H}_melon`;
  const basics = [
    S(`${H}_s1`, '멜론 씨 튕기기', 1, '공격', [dmg(0.95, E1)]),
    S(`${H}_s2`, '멜론 한 조각', 1, '스킬', [heal(1.3)]),
  ];
  const tokens = [tok0(j, ML)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '장기 투자', 1, '강화', [stk(K, 1), st('저장', 1), pw('turnStart', [stk(K, 1)])], [                    // 시동 — 묻어 두기
      O([stk(K, 1), st('저장', 1), pw('turnStart', [stk(K, 1)])]),                   // (수치)
      O([pw('turnStart', [stk(K, 1)])], { cost: 0 }),                                 // 적립식
      O([stk(K, 1), pw('turnStart', [heal(0.2), stk(K, 1)])]),  // 배당금
      O([stk(K, 1), pw('turnStart', [stk(K, 1), make(ML, 1)])], { cost: 2 }),         // 멜론 농장
      O([st('저장', 2), pw('turnStart', [stk(K, 1)])]),                               // 팔기 전엔 손해가 아니다
    ], null, ['개전']),
    U(`${H}_u2`, '올 때 멜루나~', 1, '공격', [dmg(0.4, ER, { hits: 2 }), drain(0.4), make(ML, 1)], [                   // 연료 — 원작 저학년(멜론비 · 준 피해만큼 회복)
      O([dmg(0.4, ER, { hits: 2 }), drain(0.4), make(ML, 1)]),                       // (수치)
      O([dmg(0.3, ER, { hits: 3 }), drain(0.4), stk(K, 1)]),  // 멜론비 다섯 번
      O([dmg(0.4, ER, { hits: 2 }), stk(K, 2), draw(1)]),  // 주가 부양
      O([make(ML, 2), draw(1)], { cost: 0 }),                                        // 멜론 배달
      O([dmg(0.6, E1), drain(0.5), make(ML, 1)]),  // 뇌물 멜론
    ], [B('power'), B('heal'), B(BK)]),
    U(`${H}_u3`, '멜론 씨 기관총', 1, '공격', [dmg(0.4, E1), per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)], [            // 결정타 — 주가를 털어
      O([dmg(0.4, E1), per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)]),                // (수치)
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K)]),                             // 씨앗 난사
      O([dmg(0.4, E1), per(K, { n: 1 }), dmg(0.35, E1), spendN(K, 2)]),              // 일부 매도
      O([per(K, { n: 1 }), heal(0.25), spendAll(K), draw(1)], { cost: 0 }),  // 사회 공헌
      O([dmg(0.3, E1), per(K, { n: 1 }), dmg(0.3, E1), make(ML, 1)]),  // 멜론머스크 X
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '멜론머스크 그룹', 1, '공격', [dmg(0.9, E1), perTag(ML), dmg(0.5, E1)], [                               // 원작 자유 — 펫 멜론 「머스크」 코인
      O([dmg(0.9, E1), perTag(ML), dmg(0.5, E1)]),                                    // (수치)
      O([dmg(0.5, EA), perTag(ML), dmg(0.2, EA)]),                                    // 코인 상장
      O([dmg(0.8, E1), stk(K, 2)]),                                                   // 반란 임원 승진
      O([dmg(0.7, E1), make(ML, 1), heal(0.3)]),                                      // 멜론 나눔
      O([perTag(ML), dmg(0.6, E1), { k: 'exileFrom', from: 'hand', all: true, tag: ML }]), // 떡락(전량 매도)
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '멜론머스크 X', cost: 150, fx: [dmg(0.9, EA), drain(0.4), { k: 'ap', v: 2 }, stk(K, 2)] },
    starter: [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`], basics, tokens, uniques });
}

// ── 프리클 — 소환형 · 딜러 ──
// 엔진: 가시 촉수(카드마다 · 턴 끝 소환)를 깔아 두면 적이 공격할 때마다 촉수가 그 자리에서 찌른다 → 「따끔한 문지기」가 깔린 촉수를 모두 거둬 한 방으로
function prickle(j) {
  // 팀: 「이미 손을 써 두었습니다」 — 적이 공격하려 하면 촉수가 먼저 찌른다(파티를 노린 적을 덫이 맞는다)
  // 원작: 강화 평타(가시 촉수 소환 — 사라질 때까지 찌르고 사라질 때 앞뒤를 친다) · 저학년 「따끔한 문지기」(촉수를 모두 거둬 더 큰 피해) · 고학년 「조여오는 경비병」 · 마녀 여왕의 보좌관
  const H = '프리클', K = '가시 촉수';
  const basics = [
    S(`${H}_s1`, '가시 투척', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '가시 작살', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '덩굴 장벽', 1, '스킬', [sh(1.8)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u1`, '가시 덫 설치', 1, '스킬', [stk(K, 2), draw(1)], [                                                // 시동 — 덫 깔기
      O([stk(K, 2), draw(1)]),                                                       // (수치)
      O([stk(K, 1), sh(0.6)]),                                                       // 덫 뒤에 숨기
      O([stk(K, 3)]),  // 벨라 봉인 작전
      O([stk(K, 1), draw(1)], { cost: 0 }),                                          // 즉석 계획
      O([stk(K, 2), st('약화', 1, E1)]),                                             // 독설
    ], null),
    U(`${H}_u3`, '따끔한 문지기', 1, '공격', [dmg(0.5, EA), per(K, { n: 1 }), dmg(0.28, EA), spendAll(K)], [           // 결정타 — 원작 저학년(촉수를 모두 거둔다)
      O([dmg(0.5, EA), per(K, { n: 1 }), dmg(0.28, EA), spendAll(K)]),               // (수치)
      O([dmg(0.7, E1), per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)]),                // 한 놈을 감싸
      O([dmg(0.45, EA), per(K, { n: 1 }), dmg(0.22, EA)]),                           // 덫은 둔다
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), st('약화', 1, EA)]),           // 침묵(조여오는 경비병)
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), draw(1)]),  // 계획은 그 자리에서
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u5`, '가시 울타리', 1, '스킬', [sh(1.1), stk(K, 2)], [                                                 // 연료 — 덩굴 장벽 위 덫
      O([sh(1.1), stk(K, 2)]),                                                       // (수치)
      O([sh(0.8), stk(K, 1), draw(1)]),                                              // 일지 쓰기
      O([sh(1.2), st('피해 감소', 1), stk(K, 1)]),  // 두꺼운 울타리
      O([stk(K, 2), st('반격', 1)], { cost: 0 }),                                    // 가시 반격
      O([sh(0.9), stk(K, 2), heal(0.3)]),                                            // 고구마튀김 열두 봉지
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u4`, '마녀의 힘은 대단했다!', 1, '강화', fx0(j, `${H}_u4`), [                                           // 원작 자유 — 여왕 대리 인장(촉수 2개)
      O(fx0(j, `${H}_u4`)),                                                          // (수치)
      O([stk(K, 2), pw('turnStart', [stk(K, 1)])]),                                  // 아침 결재
      O([pw('play', [stk(K, 1)], { when: { type: '스킬' } })], { cost: 0 }),          // 함정 결재
      O([stk(K, 1), pw('kill', [stk(K, 2), draw(1)])]),                              // 계획대로
      O([stk(K, 2), pw('turnEnd', [dmg(0.15, ER)])]),  // 촉수 피해 2배(어사이드)
    ], [B('power'), B('cost'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '조여오는 경비병', cost: 200, fx: [dmg(0.9, EA), stk(K, 2), stk('가시 덩굴', 1)] },
    starter: [`${H}_s1`, `${H}_u1`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

// ── 모모 — 소환형 · 딜러 ──
// 엔진: 공격마다 분신을 부르고 · 분신이 행동할 때마다 자폭 + 충격 → 「분신 폭발」이 분신을 모두 터뜨려 적 전체 · 번개 수리검이 손에
function momo(j) {
  // 팀: 분신 자폭의 충격 — 동료의 공격에도 충격이 쌓인 적이 더 아프다
  // 원작: 저학년 「두배로 안아준닷」(분신 3 · 깨질 때 자폭 + 감전) · 고학년 「초살 람쥐썬더」 · 어사이드(분신 자폭 +50%) · 닌자 만화에 빠진 하늘다람쥐 · 통나무 바꿔치기
  const H = '모모', K = '분신', SH = `${H}_shuriken`;
  const basics = [
    S(`${H}_s1`, '람쥐 수리검', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '왕 람쥐 수리검', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '통나무 바꿔치기', 1, '스킬', [sh(1.6)]),
  ];
  const tokens = [tok0(j, SH)];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '환영술', 1, '스킬', [stk(K, 1), make(SH, 1), draw(1)], [                                          // 시동 — 분신과 수리검
      O([stk(K, 1), make(SH, 1), draw(1)]),                                          // (수치)
      O([stk(K, 2), sh(0.4), draw(1)]),  // 분신 둘
      O([make(SH, 2), draw(1)]),                                                     // 수리검 꺼내기
      O([stk(K, 1), draw(1)], { cost: 0 }),                                          // 적당히 열심히
      O([stk(K, 1), make(SH, 1), st('충격', 1, E1)]),                                // 번개 인술
    ], null),
    U(`${H}_u1`, '두배로 안아준닷', 1, '스킬', [stk(K, 2), sh(0.5)], [                                               // 연료 — 원작 저학년(분신 셋)
      O([stk(K, 2), sh(0.5)]),                                                       // (수치)
      O([stk(K, 1), sh(0.8)]),                                                       // 대신 맞는 분신
      O([stk(K, 3)]),  // 분신 다섯
      O([stk(K, 2), st('충격', 1, EA)]),                                             // 감전 포옹
      O([stk(K, 1), sh(0.4), draw(1)]),                                              // 비급 복제
    ], [B('guard'), B('draw'), B(BK)]),
    U(`${H}_u5`, '분신 폭발', 1, '공격', [per(K, { n: 1 }), dmg(0.32, EA), spendAll(K), st('충격', 1, EA)], [               // 결정타 — 분신을 모두 터뜨린다
      O([per(K, { n: 1 }), dmg(0.32, EA), spendAll(K), st('충격', 1, EA)]),           // (수치)
      O([per(K, { n: 1 }), dmg(0.5, E1), spendAll(K)]),                               // 한 놈에게 몰려가
      O([per(K, { n: 1 }), dmg(0.28, EA), st('충격', 1, EA)]),                        // 분신은 남긴다
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), make(SH, 1)]),                  // 터지며 수리검
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), sh(0.4)]),                      // 통나무 바꿔치기
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u4`, '카게닌자의 극의', 1, '공격', [dmg(0.5, E1), perTag(SH), dmg(0.26, E1), stk(K, 1)], [                   // 원작 자유 — 고학년 「초살 람쥐썬더」
      O([dmg(0.5, E1), perTag(SH), dmg(0.26, E1), stk(K, 1)]),                        // (수치)
      O([dmg(0.3, FAR, { hits: 3 }), stk(K, 1)]),                                     // 맨 뒤 적 연타(고학년)
      O([dmg(0.6, E1), st('충격', 2, E1)]),                                           // 번개 극의
      O([dmg(0.5, E1), make(SH, 1)]),                                                 // 수리검 보충
      O([dmg(0.5, E1), perTag(SH), dmg(0.22, E1), draw(1)]),                          // 야바위 탈출
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '초살 람쥐썬더', cost: 250, fx: [dmg(0.8, E1), st('충격', 2, EA), stk(K, 1)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, tokens, uniques });
}

// ── 에피카 — 소환형 · 딜러(엘다인) ──
// 엔진: 공격마다 에피콘이 따라 치고 · 에피칸으로 에피콘을 모아 → 「용감한 에피콘」이 모은 에피콘으로 적 전체 · 연주 중이면 턴마다 에피콘 · 끝나면 사기 + 드로우
function epica(j) {
  // 팀: 연주가 끝나면 파티 사기 · 「극적인 연출」로 다음 카드 강화
  // 원작: 저학년 「극적인 연출」(공속↑ · 평타 강화 · 한 번 크게 맞는 걸 버팀) · 고학년 「교주에게 바칩니다」(연주 · 에피콘) · 강화 평타 「용감한 에피콘」 · 에피콘 42마리 · 각색 영웅담 공연
  const H = '에피카', K = '에피콘', PL = '연주 중';
  const basics = [
    S(`${H}_s1`, '에피콘 들이받기', 1, '공격', [dmg(1.0, E1)]),
    S(`${H}_s2`, '에피콘 떼 돌진', 2, '공격', [dmg(2.2, E1)]),
    S(`${H}_s3`, '막 뒤로 숨으시오', 1, '스킬', [sh(1.6)]),
  ];
  const BK = [stk(K, 1)];
  const uniques = [
    U(`${H}_u3`, '에피칸', 1, '스킬', [stk(K, 1), draw(1), sh(0.6)], [                                              // 시동 — 에피콘 부르기
      O([stk(K, 1), draw(1), sh(0.6)]),                                              // (수치)
      O([stk(K, 2), draw(1)]),                                                       // 핑크 떼
      O([stk(K, 1), stk(PL, 1)]),                                                    // 서곡
      O([stk(K, 1), draw(1)], { cost: 0 }),                                          // 휘파람
      O([sh(0.9), stk(K, 1)]),                                                       // 에피콘 방패
    ], null),
    U(`${H}_u1`, '극적인 연출', 1, '스킬', [stk(K, 1), empower(0.4), sh(0.45)], [                                    // 연료 — 원작 저학년
      O([stk(K, 1), empower(0.4), sh(0.45)]),                                        // (수치)
      O([empower(0.5), st('끈기', 1)]),                                              // 한 번 크게 맞는 걸 버팀(원작)
      O([stk(K, 2), empower(0.3)]),                                                  // 각색된 영웅담
      O([empower(0.4, 'any'), sh(0.4)]),                                             // 교주님께 바치는 연출
      O([stk(K, 1), empower(0.35), draw(1)]),                                        // 극적인 반전
    ], [B('power'), B('draw'), B(BK)]),
    U(`${H}_u2`, '용감한 에피콘', 1, '공격', [per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)], [                                                    // 결정타 — 강화 평타(모은 에피콘으로)
      O([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K)]),  // (수치)
      O([per(K, { n: 1 }), dmg(0.4, E1), spendAll(K)]),                              // 한 놈에게 돌진
      O([dmg(0.35, EA), stk(K, 1)]),                                                 // 에피콘은 남긴다
      O([per(K, { n: 1 }), dmg(0.25, EA), spendAll(K), st('사기', 1)]),             // 커튼콜
      O([dmg(0.3, EA), per(K, { n: 1 }), dmg(0.15, EA), spendAll(K)]),  // 42마리 총출동
    ], [B('power'), B('cost'), B(BK)]),
    U(`${H}_u5`, '바치는 무대', 1, '공격', [dmg(0.5, E1), stk(K, 1), stk(PL, 1)], [                                  // 원작 자유 — 고학년 「교주에게 바칩니다」
      O([dmg(0.5, E1), stk(K, 1), stk(PL, 1)]),                                      // (수치)
      O([dmg(0.4, EA), stk(PL, 1)]),                                                 // 객석을 향해
      O([dmg(0.7, E1), stk(K, 1)]),                                                  // 독주
      O([stk(PL, 2), sh(0.5)]),  // 앙코르 공연
      O([dmg(0.5, E1), stk(PL, 1), draw(1)]),                                        // 야유도 박수로
    ], [B('power'), B('weakSpot'), B(BK)]),
  ];
  hero(j, { ...kw0(j), passives: pas0(j),
    ult: { name: '교주에게 바칩니다', cost: 300, fx: [dmg(0.45, ER, { hits: 2 }), stk(K, 2), stk(PL, 2)] },
    starter: [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`], basics, uniques });
}

export const JOBS = [['마녀/포셔', porsche], ['마녀/피코라', picora], ['수인/디아나', diana], ['엘프/힐데', hilde], ['용족/다야_퓨어샤인', dayaPure],
  ['수인/코미_수영복', komiSwim], ['엘프/아이시아', icea], ['용족/시스트', sist], ['용족/피라', pira], ['유령/바롱', barong], ['정령/멜루나', meluna],
  ['마녀/프리클', prickle], ['수인/모모', momo], ['수인/에피카', epica]];
import { NEWPAS } from './passives_new.mjs';
L.setNewPas(NEWPAS);
L.main(import.meta.url, JOBS);
