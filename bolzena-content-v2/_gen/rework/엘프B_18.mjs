// 18갈래 재설계 3단계 — 엘프B 묶음 10명(2026-10-08). 기준: 시범 17명(05_시범16_결과.md) · 지침 BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/엘프B.md
// 오르 · 이드 · 이드(재활) · 칸나 · 캐시 · 타이다 · 페스타 · 하이디 · 헤일리 · 헤일리(멀쩡)
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장 · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀). 다 차면 저절로 터짐 없음(onMax make/empower).
// 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/엘프B_18.mjs [사도 이름 일부]  → heroes/엘프/<파일>.json (원본은 백업 SRC 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, later,
  TOP, nextAp, disc, hasten, gauge, ripen, srch, drawType, pull, xtra, pw, pas, token, Or, bl, U, setCards, setOpener, empower,
  exile, cs, dmod, spendN, tough, form, formEnd, ifNth } = L;
const MARK = 'markedEnemy', LOW = 'lowEnemy';
const perG = p => ({ k: 'perGuarded', per: p });
const perDebuff = { k: 'perDebuff' };
const stkEv = (id, of) => ({ k: 'stack', id, v: 1, ofEvent: of });
const onDisc = { k: 'when', on: 'discard' };   // 안식 — 버려지면
const cleanse = v => ({ k: 'cleanse', v });
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));

// ════════════════════════════════════════════════════════════════════
// 1. 오르 — 예약형 · 서포터 · 우울. 적에게 「기상 드론」 을 붙여 두고(예약 2칸) 다 닳으면 터진다 — 기다릴까, 지금 당겨 쓸까(남은 칸만큼 약함).
//    사라진 카드에서 주운 고물이 셋이면 발명품(손에 카드 — 원래 만들던 장치 그대로)
// 원작: 저학년 강제 기상 장치(드론이 붙어 소음 · 시간이 다하면 마지막 대상에게 터짐) · 고학년 휴대용 돔(맞을 때마다 회복 + 유성우) · 어사이드 2인용 로켓 · 응원의 녹음기
//       · 늘 「한발 늦게」 나오는 발명품 · 정해진 때에 떨어지는 배식기
// 시동: u2 2인용 로켓(그대로 — 발명품 만들기)
// ════════════════════════════════════════════════════════════════════
function or(j) {
  const H = '오르', K = '고물 부품', D = '기상 드론', T1 = '오르_t1';
  const h = j.heroes[0];
  h.blurb = '자재난 속에서도 우주선을 띄운 노력파 발명가. 적에게 붙인 기상 드론은 두 차례 뒤 요란하게 터지고, 사라진 카드에서 주운 고물이 셋 모이면 발명품을 뚝딱 만듭니다.';
  h.keyword = { name: K, desc: '사라진 카드에서 주운 부품', carrier: 'self', cap: 3, onMax: { make: T1, consume: true } };
  h.keywords = [{
    name: D, desc: '적에게 붙어 울리다 터지는 자명종 드론', carrier: 'enemy', cap: 2, reserve: true, decay: 1,
    rules: [{ name: '강제 기상', when: { on: 'stackGone', id: D, decay: true }, fx: [dmg(1.5), st('약화', 1)] }],
  }];
  h.passives = [
    pas('고물 줍기', 'exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 2 }),
    pas('응원의 녹음기', 'reserveFire', [stk(K, 1), dmod(0.15, 'allAllies')], { limit: 1 }),
    pas('응원의 녹음기', 'fightStart', [stk(K, 1), stk(D, 1, TOP)]),
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === K ? stk(K, 2) : f));
  const t1 = j.cards.find(c => c.id === T1);
  const tokens = [{ ...t1, fx: [{ k: 'ifChoice', n: 1 }, sh(1.3), { k: 'ifChoice', n: 2 }, dmg(1.0), stk(D, 1)] }];
  setCards(j, [
    // 갈래 부품(만들기) — 강제 기상 장치: 고른 적에게 기상 드론 2 + 약화(소음)
    U(H, 1, '강제 기상 장치', 1, '스킬', [stk(D, 2, E1), st('약화', 1)], [
      'A', 'B',
      Or([stk(D, 1, EA), st('약화', 1, EA)]),
      Or([stk(D, 2, E1), st('약화', 1), pw('reserveFire', [st('약화', 1, EA)], { limit: 1 })], { power: true }),
      Or([stk(D, 2, E1), st('약화', 1), srch()]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 시동 — 2인용 로켓: 발명품 하나 + 드로우
    U(H, 2, '2인용 로켓', 1, '스킬', [make(T1, 1), draw(1)], nm([
      Or([make(T1, 1), draw(1), sh(0.7)]),
      Or([make(T1, 1)], { cost: 0 }),
      Or([make(T1, 1), draw(1), stk(D, 1, E1)]),
      Or([make(T1, 1), draw(1), pw('reserveFire', [sh(0.4)], { limit: 1 })], { power: true }),
      Or([make(T1, 2), draw(1), disc(1)]),
    ]), null),
    // 원작 자유 — 이동식 자기장(휴대용 돔): 실드 + 한 차례 뒤 유성우
    U(H, 3, '이동식 자기장', 1, '스킬', [sh(1.0), later(1, [dmg(0.22, ER, { hits: 4 })])], [
      'A', 'B',
      Or([sh(0.9), heal(0.5), later(1, [dmg(0.2, ER, { hits: 4 })])]),
      ['D', 'turnStart', [sh(0.4)]],
      'Hn',
    ], bl('guard', 'defUp', [stk(K, 1)])),
    // 쓰기 — 필요는 발명의 어머니: 고른 적의 기상 드론을 지금 울린다(남은 1칸당 -30%) + 고물
    U(H, 4, '필요는 발명의 어머니', 1, '스킬', [ripen(D, 0.3), stk(K, 1)], [
      Or([ripen(D, 0.3), stk(K, 1), sh(0.6)]),
      Or([ripen(D, 0.3)], { cost: 0 }),
      Or([ripen(D, 0.3, EA), stk(K, 1), sh(0.5)]),
      Or([ripen(D, 0.3), srch(), stk(K, 1)]),
      Or([ripen(D, 0.3), stk(K, 2), disc(1)]),
    ], bl('ap', { tags: ['보존'] }, [stk(D, 1, E1)])),
    // 둘째 — 기계팔 크앙: 단일 + 재촉 1(파티의 모든 예약)
    U(H, 5, '기계팔 크앙', 1, '공격', [dmg(0.8), hasten(1)], [
      'A', 'B',
      Or([dmg(0.7, EA), hasten(1)]),
      ['D', 'turnStart', [hasten(1)], { limit: 1 }],
      Or([dmg(0.75), hasten(1), srch()]),
    ], bl('power', 'cost', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 2. 이드 — 버티형(변환) · 탱커 · 냉정 · 엘다인. 나타가 막아 낸 충격을 「나타 경보」 로 센다 — 다섯이면 다음 카드에 몰아 되받아친다. 쓰러짐은 꿈결이 한 번 막는다
// 원작: 직접 피해 5회마다 반격 + 회복(강화 평타) · 저학년 흐릿한 경계(아군 전투불능을 무효로, 최대 2번) · 고학년 너와 나의 우주 · 나타 생명 유지 장치
// 엘다인 한 단계: 전투 시작 끈기(꿈결이 한 번 버팀) — 수치 틀은 그대로
// 이드(재활)과 가르기: 이드 = 막아 낸 양을 그때그때 경보로(변환) · 재활 = 잠든 얼굴과 깬 얼굴(저장 → 붕괴)
// 시동: u2 전기로 지져 줘(그대로)
// ════════════════════════════════════════════════════════════════════
function ide(j) {
  const H = '이드', K = '나타 경보';
  const h = j.heroes[0];
  h.blurb = '세상 전부를 자기가 꾸는 꿈이라 여기는 영원살이. 나타가 막아 낸 충격이 경보로 쌓이고, 다섯이 차면 다음 한 수에 몰아 되받아칩니다. 쓰러질 고비는 꿈결이 한 번 막아 줍니다.';
  h.keyword = { name: K, desc: '나타가 막아 내며 세는 꿈속의 충격', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.03 }], onMax: { empower: 'next', ratio: 0.6, consume: true } };
  h.passives = [
    pas('함께 꾸는 꿈', 'guardSum', [stkEv(K, 0.012)]),
    pas('함께 꾸는 꿈', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
    pas('깨지 않은 꿈', 'fightStart', [st('피해 감소', 1), stk(K, 1)]),
  ];
  setCards(j, [
    // 갈래 부품 — 나타에게 먼저: 실드 + 경보 + 지난 판에 막아 낸 양 60당 실드
    U(H, 1, '나타에게 먼저', 1, '스킬', [sh(1.1), stk(K, 1), perG(60), sh(0.1)], [
      'A', 'B',
      Or([sh(1.0), stk(K, 2), st('반격', 1)]),
      Or([sh(1.2), stk(K, 1), pw('guardSum', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([sh(1.7), stk(K, 2), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 시동 — 전기로 지져 줘: 반격 + 경보 + 드로우
    U(H, 2, '전기로 지져 줘', 1, '스킬', [st('반격', 1), stk(K, 1), draw(1)], nm([
      Or([st('반격', 1), stk(K, 2), draw(1)]),
      Or([st('반격', 1), draw(1)], { cost: 0 }),
      Or([st('반격', 2), stk(K, 1), draw(1)]),
      Or([st('반격', 1), draw(1), pw('hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 })], { power: true }),
      Or([st('반격', 2), stk(K, 2), disc(1)]),
    ]), null),
    // 원작 자유 — 흐릿한 경계(2): 큰 실드 + 끈기(쓰러질 피해를 꿈결이 한 번)
    U(H, 3, '흐릿한 경계', 2, '스킬', [sh(2.0), st('끈기', 1)], [
      'A', 'B',
      Or([sh(1.7), st('끈기', 1), stk(K, 2)]),
      Or([sh(1.6), st('끈기', 1), srch()]),
      'Hn',
    ], bl('guard', 'defUp', [stk(K, 1)])),
    // 둘째 — 착한 마음씨 이드: 실드 + 경보 1개당 실드(쓰지 않음)
    U(H, 4, '착한 마음씨 이드', 1, '스킬', [sh(0.7), per(K), sh(0.2)], [
      'A', 'B',
      Or([sh(0.6), per(K), sh(0.18), stk(K, 1)]),
      ['D', 'guardSum', [sh(0.55)], { limit: 1 }],
      'Hd',
    ], bl('heal', 'ap', [stk(K, 1)])),
    // 쓰기 — 나타로 들이받기: 방어 기반 + 경보 1개당, 경보 전부 소모(다섯을 기다려 강화를 받을지)
    U(H, 5, '나타로 들이받기', 1, '공격', [ddef(0.5), per(K), ddef(0.28), spendAll(K)], [
      'A',
      ['D', 'hurt', [ddef(0.2)], { when: { guarded: true }, limit: 1 }],
      Or([ddef(0.45, EA), per(K), ddef(0.2, EA), spendAll(K)]),
      Or([ddef(0.45), per(K), ddef(0.26), srch()]),
      'Hx',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 이드(재활) — 두 얼굴형 · 탱커 · 순수. 깨어 있는 얼굴(악몽을 정면으로 마주해 둔화 · 실드)과 잠든 얼굴(공격하지 않고 회복하며 맞은 아픔을 저장)을 오간다 —
//    잠에서 깨는 순간 꿈의 공간이 무너져 적 전체 + 저장한 만큼 실드. 공격 카드를 내면 깬다(언제 깰까)
// 원작: 고학년 「이젠 스스로 마주하겠어」 — 잠든 동안 매초 회복 · 받는 피해 감소 · 받은 피해 저장 → 깨면 붕괴 범위 피해 + 저장량만큼 보호막(01 형태 종합 1순위)
//       · 저학년 뚜렷한 경계(범위 + 회복 흔적) · 「넘어져도 다시 일어난다」
// 시동: u3 잠든 동안(만들기 — 잠든 얼굴로)
// ════════════════════════════════════════════════════════════════════
function ideRehab(j) {
  const H = '이드_재활', K = '악몽', SLEEP = '이드_재활_잠';
  const h = j.heroes[0];
  h.blurb = '넘어져도 다시 일어서는 이드. 깨어 있을 땐 악몽을 정면으로 마주해 방패로 바꾸고, 잠든 동안엔 맞은 아픔을 고스란히 받아 둡니다 — 깨어나는 순간 꿈의 공간이 무너져 적을 덮치고 파티를 감쌉니다.';
  h.keyword = { name: K, desc: '잠결에 맞은 아픔까지 쌓이는 밤의 악몽', carrier: 'self', cap: 6, per: [{ stat: 'guard', v: 0.06 }] };
  delete h.keywords;
  h.forms = [{
    id: SLEEP, name: '잠든 이드', desc: '공격을 멈추고 상처를 아물리며 맞은 아픔을 받아 두는 꿈속의 얼굴',
    turns: 1, until: { on: 'play', type: '공격' }, mods: { taken: -0.2 },
    passives: [
      { name: '아픔 저장', when: { on: 'hurt', guarded: true }, fx: [{ k: 'cue', id: 'ide_store' }, stkEv(K, 0.03)] },
      { name: '꿈속 회복', when: { on: 'turnEnd' }, fx: [heal(0.35), stk(K, 1)] },
    ],
    off: [{ k: 'cue', id: 'ide_wake', xStack: K }, ddef(0.8, EA), per(K), sh(0.45), spendAll(K)],
    replace: false, skin: null, anim: null,
  }];
  h.passives = [
    pas('악몽 마주하기', 'hurt', [stk(K, 1)], { limit: 3 }),
    pas('정면으로 마주하기', 'play', [per(K), sh(0.3), spendN(K, 2), st('둔화', 1, EA)], { when: { type: '스킬' }, conds: [{ c: 'stack', id: K, n: 3 }], limit: 1 }),
  ];
  h.ult.fx = [{ k: 'cue', id: 'ide_sleep' }, form(SLEEP), st('피해 감소', 2), stk(K, 3), st('초재생', 1)];
  setCards(j, [
    // 원작 자유 — 뚜렷한 경계: 적 전체 방어 기반 + 회복 + 악몽(공격 — 잠든 얼굴이면 깬다)
    U(H, 1, '뚜렷한 경계', 1, '공격', [ddef(0.55, EA), heal(0.5), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.6, EA), heal(0.5), st('둔화', 1, EA)]),
      Or([ddef(0.5, EA), heal(0.4), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([ddef(0.8, EA), heal(0.7), disc(1)]),
    ], bl('power', 'heal', [stk(K, 1)])),
    // 둘째 — 꿈의 파장: 방어 기반 + 둔화 + 악몽(깨우는 공격)
    U(H, 2, '꿈의 파장', 1, '공격', [ddef(0.7), st('둔화', 1), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.6, EA), st('둔화', 1, EA)]),
      Or([ddef(0.65), st('둔화', 1), srch({ type: '스킬' })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 잠든 동안: 실드 + 악몽 + 잠든 얼굴로(다음 내 턴까지 — 공격하면 깬다)
    U(H, 3, '잠든 동안', 1, '스킬', [sh(0.8), stk(K, 1), form(SLEEP)], nm([
      Or([sh(1.1), stk(K, 2), form(SLEEP)]),
      Or([stk(K, 1), form(SLEEP)], { cost: 0 }),
      Or([st('피해 감소', 1), stk(K, 3), form(SLEEP)]),
      Or([stk(K, 2), form(SLEEP), srch()]),
      Or([sh(1.4), stk(K, 2), form(SLEEP)]),
    ]), null),
    // 쓰기 — 소중한 순간: 악몽 1개당 실드, 악몽 전부 소모 + 적 전체 둔화(깨어 있는 얼굴의 마주하기)
    U(H, 4, '소중한 순간', 1, '스킬', [per(K), sh(0.35), spendAll(K), st('둔화', 1, EA)], [
      'A',
      ['D', 'hurt', [stk(K, 1)], { limit: 1 }],
      Or([per(K), sh(0.35), spendAll(K), heal(0.7)]),
      Or([per(K), sh(0.32), spendAll(K), srch()]),
      'Hd',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 같이 걷는 산책: 실드 + 악몽 + 동료 카드 뽑기 · 잠든 얼굴이면 깨어난다(손으로 깨기)
    U(H, 5, '같이 걷는 산책', 1, '스킬', [sh(0.9), draw(1, { who: 'other' }), formEnd], [
      'A', 'B',
      Or([sh(1.0), stk(K, 2), formEnd]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([sh(1.4), stk(K, 2), formEnd]),
    ], bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 칸나 — 예약형 · 딜러 · 활발. 가장 위험한 적에게 포격을 「조준」 해 두고(예약 2칸) 다 닳으면 포탄이 떨어진다 — 명령(재촉)이 오면 즉시 출동, 당겨 쏘면 남은 칸만큼 약하다.
//    쏠 때마다 휴가 신청서에 반려 도장 — 셋이면 다음 한 방에 화풀이
// 원작: 평타 · 저학년 · 고학년 모두 공격력이 가장 높은 적 · 저학년 큰 거 한 방(특수 포탄) · 고학년 양 자폭탄(추적) · 충격 포탄 기절 · 늘 반려되는 휴가
// 알레트(엘프A)는 칸나를 「반장님」 으로 찍는다(hero:칸나) — 칸나 쪽 장치 이름은 알레트 글에 나오지 않는다
// 시동: u1 큰 거 한 방(2코) → u5 포격 요청(1코 · 만들기 칸)
// ════════════════════════════════════════════════════════════════════
function kanna(j) {
  const H = '칸나', K = '반려 도장', A = '포격 조준', SHELL = '칸나_shell';
  const h = j.heroes[0];
  h.blurb = '명령엔 칼 같고 휴가 신청서는 늘 반려되는 진압반장. 위험한 적에게 포격을 조준해 두면 두 차례 뒤 포탄이 떨어지고, 명령이 떨어지면 기다리지 않고 바로 쏩니다. 쏠 때마다 반려 도장이 늘어 화풀이 한 방이 됩니다.';
  h.keyword = { name: K, desc: '휴가 신청서마다 찍히는 반려 도장', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.06 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.keywords = [{
    name: A, desc: '포격을 요청해 둔 좌표', carrier: 'enemy', cap: 2, reserve: true, decay: 1,
    rules: [{ name: '포탄 낙하', when: { on: 'stackGone', id: A, decay: true }, fx: [dmg(1.8), tough(1), stk(K, 1)] }],
  }];
  h.passives = [
    pas('즉시 출동', 'reserveFire', [stk(K, 1), draw(1)], { when: { who: 'any' }, limit: 1 }),
    pas('양자폭탄 결재', 'play', [stk(K, 1)], { when: { type: '공격', minCost: 2 } }),
    pas('양자폭탄 결재', 'fightStart', [stk(A, 1, TOP)]),
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === K ? stk(K, 1) : f));
  setOpener(j, H, 'u1', 'u5');
  const sh0 = j.cards.find(c => c.id === SHELL);
  const tokens = [{ ...sh0, fx: [dmg(0.7), stk(K, 1)] }];
  setCards(j, [
    // 원작 자유 — 큰 거 한 방(2): 큰 단일 + 특수 포탄 하나
    U(H, 1, '큰 거 한 방', 2, '공격', [dmg(1.9), make(SHELL, 1)], [
      'A', 'B',
      Or([dmg(1.7), stk(A, 1), make(SHELL, 1)]),
      ['D', 'reserveFire', [dmg(0.6)], { limit: 1 }],
      Or([dmg(1.8), make(SHELL, 1), srch({ type: '공격' })]),
    ], bl('power', 'ap', [make(SHELL, 1)])),
    // 갈래 부품 — 명 받았습니다!(0): 재촉 1(파티의 모든 예약 — 명령이 오면 즉시) + 공격 카드 1장
    U(H, 2, '명 받았습니다!', 0, '스킬', [hasten(1), drawType('공격')], [
      Or([hasten(1), drawType('공격'), stk(K, 1)]),
      Or([hasten(1), drawType('공격')], { tags: ['신속'] }),
      Or([hasten(1), make(SHELL, 1)]),
      Or([hasten(1), pw('reserveFire', [make(SHELL, 1)], { limit: 1 })], { power: true }),
      Or([hasten(2), drawType('공격', 2), disc(1)]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
    // 둘째 — 오락실 패왕: 단일 + 반려 도장 1개당(쓰지 않음)
    U(H, 3, '오락실 패왕', 1, '공격', [dmg(0.9), per(K), dmg(0.3)], [
      'A',
      ['C', [st('기절', 1)]],
      Or([dmg(0.8), per(K), dmg(0.26), stk(A, 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.85), per(K), dmg(0.28), srch()]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 포탄 일제 사격: 단일 + 그 적의 조준을 지금 쏜다(남은 1칸당 -25%)
    U(H, 4, '포탄 일제 사격', 1, '공격', [dmg(0.6), ripen(A, 0.25)], [
      'A', 'B',
      Or([dmg(0.5, EA), ripen(A, 0.25, EA)]),
      Or([dmg(0.55), ripen(A, 0.25), srch({ type: '공격' })]),
      'Hd',
    ], bl('power', 'cost', [stk(A, 1, E1)])),
    // 시동 — 포격 요청(옛 시정 보고): 고른 적에게 조준 2 + 드로우
    U(H, 5, '포격 요청', 1, '스킬', [stk(A, 2, E1), draw(1)], nm([
      Or([stk(A, 2, E1), draw(1), sh(0.6)]),
      Or([stk(A, 2, E1)], { cost: 0 }),
      Or([stk(A, 2, E1), make(SHELL, 1)]),
      Or([stk(A, 2, E1), draw(1), pw('reserveFire', [dmg(0.3, TOP)], { limit: 1 })], { power: true }),
      Or([stk(A, 2, TOP), draw(2), disc(1)]),
    ]), null),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 5. 캐시 — 박자형 · 딜러 · 순수. 동료가 먼저 쳐야 놀라서 전기가 찬다(「히익 전류」 · 턴 끝 사라짐) — 동료 공격 뒤에 캐시 카드를 낼 순서를 맞춘다.
//    셋이면 과충전(다음 카드 강화), 자신의 공격 셋째마다 가운데 적에게 과충전 테이저
// 원작: 강화 평타 = 세 번째 공격마다 과충전 테이저(범위) · 저학년 바삭 지짐이(연쇄 테이저 · 받는 스킬 피해↑) · 어사이드 불가사의한 현상(공속 · SP 감소)
//       · 유령을 무서워해 들킬 것 같으면 먼저 지진다 · 동굴 301일
// 리스티(셋이면 넷째 공격 카드) · 티그(제 공격만 잇기)와 가르기: 캐시는 「동료 공격 → 캐시 카드」 순서(놀람)
// 시동: u2 더플백 은신(그대로)
// ════════════════════════════════════════════════════════════════════
function cathy(j) {
  const H = '캐시', K = '히익 전류';
  const h = j.heroes[0];
  h.blurb = '모든 것에 「히익」 떠는 겁쟁이 정찰병. 동료가 먼저 공격하면 깜짝 놀라 전기가 차오르고, 그 전기로 곧장 지져 버립니다 — 놀람은 턴이 끝나면 가라앉습니다. 세 번째 공격마다 과충전 테이저가 터집니다.';
  h.keyword = { name: K, desc: '동료 공격에 깜짝 놀라 차오르는 전기', carrier: 'self', cap: 3, endClear: true, onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('히익!', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 2 }),
    pas('과충전 테이저', 'play', [dmg(0.35, EA)], { when: { type: '공격', every: 3 } }),
  ];
  setCards(j, [
    // 쓰기 — 바삭 지짐이: 단일 + 전류 1개당 1타, 전류 전부 소모
    U(H, 1, '바삭 지짐이', 1, '공격', [dmg(0.5), per(K), dmg(0.32), spendAll(K)], [
      'A',
      ['D', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([dmg(0.45, ER, { hits: 3 }), per(K), dmg(0.2, ER), spendAll(K)]),
      Or([dmg(0.45), per(K), dmg(0.3), st('취약', 1)]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 더플백 은신: 실드 + 전류 + 드로우
    U(H, 2, '더플백 은신', 1, '스킬', [sh(0.75), stk(K, 1), draw(1)], nm([
      Or([sh(0.95), stk(K, 1), draw(1)]),
      Or([sh(0.4), stk(K, 1), draw(1)], { cost: 0 }),
      Or([sh(0.7), st('회피', 1), draw(1)]),
      Or([sh(0.75), stk(K, 1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([sh(1.3), stk(K, 2), disc(1)]),
    ]), null),
    // 원작 자유 — 불가사의한 현상: 적 전체 + 둔화. 이번 턴 셋째 카드 이상이면 적 전체 약화
    U(H, 3, '불가사의한 현상', 1, '공격', [dmg(0.55, EA), st('둔화', 1, EA), ifNth(3), st('약화', 1, EA)], [
      'A', 'B',
      Or([dmg(0.5, EA), stk(K, 1), ifNth(3), dmg(0.3, EA)]),
      Or([dmg(0.5, EA), st('둔화', 1, EA), srch({ type: '공격' })]),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 둘째 — 301일의 동굴: 단일 + 전류 1개당(쓰지 않음)
    U(H, 4, '301일의 동굴', 1, '공격', [dmg(0.5), per(K), dmg(0.22)], [
      'A', 'B',
      Or([dmg(0.45), per(K), dmg(0.2), st('취약', 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 갈래 부품 — 무서워 보이는 꿀팁: 실드 + 전류 + 동료 공격 카드 1장(동료가 먼저 치게)
    U(H, 5, '무서워 보이는 꿀팁', 1, '스킬', [sh(0.8), stk(K, 1), draw(1, { who: 'other', type: '공격' })], [
      'A', 'B',
      Or([sh(0.9), stk(K, 2), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })]),
      ['D', 'play', [stk(K, 1), sh(0.25)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([sh(1.3), stk(K, 2), disc(1)]),
    ], bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 타이다 — 버리기형 · 딜러 · 활발 · 1성. 제 일(카드)을 버려 떠넘기면 땡땡이가 쌓이고, 떠넘긴 일은 「밀린 업무」 로 동료 손에 간다(주인 = 동료) —
//    땡땡이를 DX-슈터로 털까, 넷 채워 다음 한 방에 진심을 실을까
// 원작: 농땡이의 달인 경비원 · 심부름 문서를 다른 엘프에게 떠넘기고 시청 전체가 떠넘기다 문서가 사라짐 · 상관 서랍 털이 · 저학년 DX-슈터(강한 탄환 한 발)
// 생성 카드 사도(묶음에서 새로 1명): 장치(패시브 「떠넘긴 일」)가 「밀린 업무」 를 동료 몫으로 만든다 — 원작 「떠넘기기」 가 근거
// 시동: u2 일 떠넘기기(그대로)
// ════════════════════════════════════════════════════════════════════
function taida(j) {
  const H = '타이다', K = '땡땡이', CH = '타이다_chore';
  const h = j.heroes[0];
  h.blurb = '일은 떠넘기고 한 방 쏠 때만 진심인 경비원. 제 카드를 버려 일을 떠넘길 때마다 땡땡이가 쌓이고, 떠넘긴 일은 「밀린 업무」 가 되어 동료 몫으로 돌아갑니다. 쌓인 땡땡이는 DX-슈터 한 발에 쏟습니다.';
  h.keyword = { name: K, desc: '일 대신 아껴 둔 힘', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.15 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('떠넘긴 일', 'discard', [stk(K, 1)], { limit: 2 }),
    pas('떠넘긴 일', 'discard', [make(CH, 1, { owner: 'other' }), { k: 'nextCheaper', v: 1 }], { limit: 1 }),
    pas('짱박힐 시간', 'turnStart', [stk(K, 1)]),
  ];
  const ch = j.cards.find(c => c.id === CH);
  const tokens = [{ ...ch, fx: [draw(1), sh(0.35)], blurb: '타이다가 슬쩍 떠넘긴 일 — 받은 동료가 대신 처리합니다' }];
  setCards(j, [
    // 쓰기 — DX - 슈터(2): 큰 단일 + 땡땡이 1개당, 땡땡이 전부 소모
    U(H, 1, 'DX - 슈터', 2, '공격', [dmg(1.9), per(K), dmg(0.4), spendAll(K)], [
      'A', 'B',
      Or([dmg(2.0), per(K), dmg(0.42), ifKill, ap(1)]),
      ['D', 'discard', [stk(K, 1)], { limit: 1 }],
      Or([dmg(1.8), per(K), dmg(0.38), srch()]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 일 떠넘기기: 1장 버리고 2장 뽑기 + 밀린 업무(동료 몫)
    U(H, 2, '일 떠넘기기', 1, '스킬', [disc(1), draw(2), stk(K, 1)], nm([
      Or([disc(1), draw(3), stk(K, 1)]),
      Or([disc(1), draw(2)], { cost: 0 }),
      Or([disc(1), draw(2), make(CH, 1, { owner: 'other' })]),
      Or([disc(1), draw(2), pw('discard', [stk(K, 1), dmg(0.45)], { limit: 2 })], { power: true }),
      Or([{ k: 'discard', all: true }, draw(3), stk(K, 2)]),
    ]), null),
    // 원작 자유 — 야근 수당 사격: 단일. 안식(버려지면): 땡땡이 2(추가 수당)
    U(H, 3, '야근 수당 사격', 1, '공격', [dmg(1.15), onDisc, stk(K, 2)], [
      'A', 'B',
      Or([dmg(0.55, E1, { hits: 2 }), onDisc, stk(K, 2), draw(1)]),
      Or([dmg(1.1), onDisc, stk(K, 2), pw('discard', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([dmg(1.1), srch(), onDisc, stk(K, 2)]),
    ], bl('power', 'draw', [stk(K, 1)])),
    // 둘째 — 유령 늪 출장: 단일 + 땡땡이 1개당(쓰지 않음)
    U(H, 4, '유령 늪 출장', 1, '공격', [dmg(0.7), per(K), dmg(0.3)], [
      'A', 'B',
      Or([dmg(0.78), per(K), dmg(0.33), disc(1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hn',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 상관 서랍 털이(0): 동료 카드 1장 뽑고 1장 버리기
    U(H, 5, '상관 서랍 털이', 0, '스킬', [draw(1, { who: 'other' }), disc(1)], [
      Or([draw(2, { who: 'other' }), disc(1)]),
      Or([draw(1, { who: 'other' }), disc(1), stk(K, 1)], { tags: ['신속'] }),
      Or([draw(1, { who: 'other' }), disc(1), make(CH, 1, { owner: 'other' })]),
      Or([draw(1, { who: 'other' }), disc(1), pw('discard', [sh(0.3)], { limit: 2 })], { power: true }),
      Or([draw(1, { who: 'other' }), disc(1), srch()]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 7. 페스타 — 대가형 · 서포터 · 우울 · 2성. 목 · 손목을 혹사해(다음 턴 AP) 연주를 거세게 — 누가 대가를 치르든 「반항」 이 끓고, 넷이면 파티의 다음 한 수에 힘을 싣는다
// 원작: 전설의 싱어송라이터인데 일부러 노숙 버스킹 · 인후염 · 손목 입원(「퇴락도 락」) · 저학년 락 앤 피스(소음 + 적 이로운 효과 해제) · 고학년 스포트라이트(조명 셋)
//       · 어사이드 역조공 이벤트(아군 피해↑ · 받는 피해↓)
// 시동: u3 버스킹(그대로)
// ════════════════════════════════════════════════════════════════════
function festa(j) {
  const H = '페스타', K = '반항';
  const h = j.heroes[0];
  h.blurb = '규칙과 통제를 혐오하는 무정부주의 락커. 목과 손목을 혹사해 가며 연주를 거세게 몰아치고, 누가 대가를 치르든 반항심이 끓어 넷이 차면 파티의 다음 한 수가 함께 터집니다.';
  h.keyword = { name: K, desc: '몸을 혹사할수록 끓어오르는 반항심', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.06, who: 'allies' }], onMax: { empower: 'any', ratio: 0.5, consume: true } };
  h.passives = [
    pas('퇴락도 락', 'pay', [stk(K, 1), st('약화', 1, EA)], { when: { who: 'any' }, limit: 2 }),
    pas('역조공 이벤트!', 'fightStart', [stk(K, 2), st('약화', 1, EA)]),
  ];
  setCards(j, [
    // 쓰기 — 락 앤 피스!: 적 전체 + 반항 1개당 광역, 반항 전부 소모
    U(H, 1, '락 앤 피스!', 1, '공격', [dmg(0.45, EA), per(K), dmg(0.18, EA), spendAll(K)], [
      'A',
      ['D', 'pay', [dmg(0.2, EA)], { when: { who: 'any' }, limit: 1 }],
      Or([dmg(0.4, EA), per(K), dmg(0.16, EA), { k: 'dispel', n: 1 }]),
      Or([dmg(0.4, EA), per(K), dmg(0.16, EA), srch()]),
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 둘째 — 노숙 락커: 회복 + 반항
    U(H, 2, '노숙 락커', 1, '스킬', [heal(0.9), stk(K, 1)], [
      'A', 'B',
      Or([heal(0.85), stk(K, 1), st('협공', 1)]),
      ['D', 'pay', [heal(0.4)], { when: { who: 'any' }, limit: 1 }],
      Or([heal(0.85), stk(K, 1), srch()]),
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 시동 — 버스킹: 반항 2 + 협공
    U(H, 3, '버스킹', 1, '스킬', [stk(K, 2), st('협공', 1)], nm([
      Or([stk(K, 2), st('협공', 1), sh(0.7)]),
      Or([stk(K, 1), st('협공', 1)], { cost: 0 }),
      Or([nextAp(-1), stk(K, 3), st('협공', 2)]),
      Or([stk(K, 2), st('협공', 1), pw('pay', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([stk(K, 2), st('협공', 1), srch()]),
    ]), null),
    // 원작 자유 — 갓 오브 뮤직!(강화 · 스포트라이트): 반항 + 아군 누구든 대가를 치르면 실드 · 반항
    U(H, 4, '갓 오브 뮤직!', 1, '강화', [stk(K, 1), pw('pay', [sh(0.5), stk(K, 1)], { when: { who: 'any' }, limit: 1 })], nm([
      Or([stk(K, 2), pw('pay', [sh(0.6), stk(K, 1)], { when: { who: 'any' }, limit: 1 })]),
      Or([pw('pay', [sh(0.5), stk(K, 1)], { when: { who: 'any' }, limit: 1 })], { cost: 0 }),
      Or([stk(K, 1), pw('pay', [sh(0.45), draw(1)], { when: { who: 'any' }, limit: 1 })]),
      Or([srch(), pw('pay', [sh(0.5), stk(K, 1)], { when: { who: 'any' }, limit: 1 })]),
      Or([nextAp(-1), stk(K, 3), pw('pay', [sh(1.0), stk(K, 1)], { when: { who: 'any' }, limit: 1 })]),
    ]), bl('atkUp', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 기괴한 연주: 다음 턴 AP -1(손목 혹사) + 적 전체 큰 피해 + 반항
    U(H, 5, '기괴한 연주', 0, '공격', [nextAp(-1), dmg(0.95, EA), stk(K, 1)], [
      'A',
      Or([nextAp(-1), dmg(1.05, EA), { k: 'dispel', n: 1 }]),
      Or([{ k: 'payHp', v: 40 }, dmg(0.95, EA), stk(K, 1)]),
      ['D', 'pay', [dmg(0.3, EA)], { when: { who: 'any' }, limit: 1 }],
      Or([nextAp(-1), dmg(0.9, EA), srch()]),
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 하이디 — 두 얼굴형 · 딜러 · 광기. 상자에 숨는 얼굴(잠입 취재 — 맞지 않고 셔터를 모은다, 공격은 눈가림)과 특종만 쫓는 얼굴(초 집중 취재 — 찍은 적에 셔터를 몰아침)을 오간다.
//    특종감이 쓰러지면 셀카 한 장(손에 카드 — 원래 만들던 장치 그대로)
// 원작: 저학년 잠입 취재 중!(상자 · 눈속임 · 평타 다연타 + 눈가림, SP 회복 멈춤) / 고학년 현장 중계 중!(초 집중 취재 — HP 비율 낮은 적을 특종감으로, 처치하면 다음) — 두 상태 모두 해제 불가
// 시동: u1 잠입 취재 중!(그대로 — 숨는 얼굴로)
// ════════════════════════════════════════════════════════════════════
function heidi(j) {
  const H = '하이디', K = '특종감', S = '잠복', T1 = '하이디_t1', HIDE = '하이디_잠입', FOCUS = '하이디_집중';
  const h = j.heroes[0];
  h.blurb = '세상 최고의 특종은 자기 자신인 기자. 상자 속에 숨어 셔터를 모으다가, 특종감을 찍으면 그 적만 쫓으며 셔터를 몰아칩니다. 특종감이 쓰러지면 셀카 한 장이 1면 기사가 됩니다.';
  h.keyword = { ...h.keyword, per: [{ stat: 'taken', v: 0.2, from: 'owner' }] };
  h.keywords = [{ name: S, desc: '상자 속에서 노리는 한 컷', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.05 }] }];
  h.forms = [
    {
      id: HIDE, name: '잠입 취재 중', desc: '상자에 숨어 셔터를 모으고 플래시로 눈을 가리는 얼굴',
      turns: 2, mods: { taken: -0.1 },
      passives: [
        { name: '상자 속 대기', when: { on: 'turnStart' }, fx: [stk(S, 1), gauge(-5)] },
        { name: '눈가림', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [st('약화', 1)] },
      ],
      replace: false, skin: null, anim: null,
    },
    {
      id: FOCUS, name: '초 집중 취재', desc: '셔터를 모두 특종감에게 모는 얼굴',
      turns: 2,
      bonus: [{ type: '공격', fx: [xtra(0.15, MARK)] }],
      passives: [{ name: '다음 특종', when: { on: 'huntDown', id: K }, fx: [{ k: 'cue', id: 'heidi_next_scoop' }, stk(S, 2)] }],
      replace: false, skin: null, anim: null,
    },
  ];
  h.passives = [
    pas('목표 포착!', 'fightStart', [stk(K, 1, LOW)]),
    pas('목표 포착!', 'hit', [ifStack(K, 1), stk(S, 1)], { when: { who: 'other' }, limit: 1 }),
    pas('특종 셀카', 'huntDown', [make(T1, 1), stk(K, 1, LOW)], { when: { id: K } }),
  ];
  h.ult.fx = [stk(K, 1, LOW), form(FOCUS), dmg(0.5, LOW, { hits: 6 }), st('회피', 1)];
  const t1 = j.cards.find(c => c.id === T1);
  const tokens = [{ ...t1, fx: [draw(1), stk(S, 2)] }];
  setCards(j, [
    // 시동 — 잠입 취재 중!: 잠복 2 + 회피 + 숨는 얼굴로
    U(H, 1, '잠입 취재 중!', 1, '스킬', [stk(S, 2), st('회피', 1), form(HIDE)], nm([
      Or([stk(S, 3), st('회피', 2), form(HIDE)]),
      Or([stk(S, 1), form(HIDE)], { cost: 0 }),
      Or([stk(S, 3), form(HIDE), make(T1, 1)]),
      Or([stk(S, 4), form(HIDE), drawType('공격')]),
      Or([stk(S, 4), st('회피', 2), form(HIDE)]),
    ]), null),
    // 원작 자유 — 눈가림 플래시: 단일 + 약화(숨은 얼굴이면 한 겹 더)
    U(H, 2, '눈가림 플래시', 1, '공격', [dmg(1.0), st('약화', 1)], [
      'A', 'B',
      Or([dmg(0.45, EA), st('약화', 1, EA)]),
      ['D', 'play', [stk(S, 1)], { when: { type: '공격' }, limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(S, 1)])),
    // 갈래 부품 — 특종감 지정: 고른 적을 특종감으로(찍기) + 피해 + 쫓는 얼굴로
    U(H, 3, '특종감 지정', 1, '공격', [stk(K, 1, E1), dmg(0.8), form(FOCUS)], [
      'A', 'B',
      Or([stk(K, 1, E1), xtra(0.95), form(FOCUS)]),
      Or([stk(K, 1, E1), form(FOCUS), pw('huntDown', [dmg(0.6, MARK), draw(1)], { limit: 1 })], { power: true }),
      Or([stk(K, 1, E1), form(FOCUS), make(T1, 2)]),
    ], bl('power', 'weakSpot', [stk(S, 1)])),
    // 쓰기 — 셀카 한 장(셔터 연사): 특종감에게 잠복 1개당 1타, 잠복 전부 소모
    U(H, 4, '셀카 한 장', 1, '공격', [dmg(0.4, MARK), per(S, { n: 1 }), dmg(0.3, MARK), spendAll(S)], [
      'A',
      ['D', 'huntDown', [stk(S, 2)], { limit: 1 }],
      Or([dmg(0.35, EA), per(S, { n: 1 }), dmg(0.2, EA), spendAll(S)]),
      Or([dmg(0.38, MARK), per(S, { n: 1 }), dmg(0.28, MARK), srch()]),
      'Hx',
    ], bl('power', 'draw', [stk(S, 1)])),
    // 둘째 — 음모론 완성: 실드 + 잠복 2 + 공격 카드 1장
    U(H, 5, '음모론 완성', 1, '스킬', [sh(0.7), stk(S, 2), drawType('공격')], [
      Or([sh(0.95), stk(S, 3), drawType('공격')]), 'B',
      Or([sh(0.7), stk(S, 2), make(T1, 1)]),
      ['D', 'turnStart', [stk(S, 1)]],
      Or([sh(1.3), stk(S, 4), disc(1)]),
    ], bl('guard', 'ap', [stk(S, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 9. 헤일리 — 표적형 · 딜러 · 순수. 턴마다 적 하나를 외계인으로 착각한다(찍기 — 무작위, 손으로 덮어쓸 수 있음) — 그놈을 치면 한 발 더, 동료가 치면 고통이 쌓이고,
//    외계인이 쓰러지면 잠깐 제정신이 돌아와 파티를 감싼다
// 원작: 작화증 — 대상 하나에 엉뚱한 정체를 덮어씌워 그것만 집요하게 · 저학년 논 그라타(디버프 종류 수만큼 더 아픈 채찍) · 고학년 플랜 B(연막 지속 피해 + 눈가림)
//       · 넷째 공격마다 채찍 정비 · 딜포터(평가 「서브 딜러 겸 딜포터」)
// 헤일리(멀쩡)과 가르기: 헤일리 = 착각한 적 하나를 쫓는 딜러(표적), 멀쩡 = 막아 낸 공격으로 훈장을 따는 탱커(버티)
// 시동: u3 우주 전함 헤일리(그대로)
// ════════════════════════════════════════════════════════════════════
function hailey(j) {
  const H = '헤일리', K = '외계인';
  const h = j.heroes[0];
  h.blurb = '엘리아스를 동맹국으로 착각하는 PTSD 장교. 턴마다 적 1명을 외계인으로 착각해 그놈만 집요하게 갈기고, 외계인이 쓰러지면 잠깐 제정신이 돌아와 파티를 감쌉니다.';
  h.keyword = { name: K, desc: '외계인이라 믿는 적 표시', carrier: 'enemy', cap: 1, hunt: true, per: [{ stat: 'taken', v: 0.1, from: 'owner' }] };
  h.passives = [
    pas('외계인 격퇴', 'hit', [ifStack(K, 1), xtra(0.45)], { limit: 2 }),
    pas('외계인 격퇴', 'hit', [ifStack(K, 1), st('고통', 1)], { when: { who: 'other' }, limit: 2 }),
    pas('착각과 제정신', 'turnStart', [stk(K, 1, ER)]),
    pas('착각과 제정신', 'huntDown', [sh(0.8), draw(1), stk(K, 1, ER)], { when: { id: K } }),
  ];
  setCards(j, [
    // 쓰기 — 논 그라타: 적 전체 3타 + 고통, 외계인의 디버프 1가지당 외계인에게
    U(H, 1, '논 그라타', 1, '공격', [dmg(0.18, EA, { hits: 3 }), st('고통', 1, EA), perDebuff, dmg(0.15, MARK)], [
      'A',
      ['D', 'turnStart', [st('고통', 1, EA)]],
      Or([dmg(0.3, MARK, { hits: 3 }), st('고통', 2, MARK), perDebuff, dmg(0.15, MARK)]),
      Or([dmg(0.17, EA, { hits: 3 }), st('고통', 1, EA), srch()]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 둘째 — 플랜 A: 정면 돌파: 단일 + 그 적을 외계인으로(덮어쓰기)
    U(H, 2, '플랜 A: 정면 돌파', 1, '공격', [dmg(1.1), stk(K, 1, E1)], [
      'A', 'B',
      Or([dmg(1.0), stk(K, 1, E1), st('약화', 1)]),
      ['D', 'huntDown', [dmg(0.4, MARK)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 시동 — 우주 전함 헤일리: 실드 + HP 가장 높은 적을 외계인으로 + 드로우
    U(H, 3, '우주 전함 헤일리', 1, '스킬', [sh(0.8), stk(K, 1, TOP), draw(1)], nm([
      Or([sh(1.0), stk(K, 1, TOP), draw(1)]),
      Or([stk(K, 1, TOP), draw(1)], { cost: 0 }),
      Or([sh(0.8), stk(K, 1, E1), draw(2)]),
      Or([sh(0.8), stk(K, 1, TOP), pw('huntDown', [sh(0.6), draw(1)], { limit: 1 })], { power: true }),
      Or([stk(K, 1, TOP), draw(3), disc(1)]),
    ]), null),
    // 원작 자유 — 엘프군 최전선(강화 · 플랜 B 연막): 외계인 + 이 전투 동안 적을 치면 그 적 고통
    U(H, 4, '엘프군 최전선', 1, '강화', [stk(K, 1, TOP), pw('hit', [st('고통', 1)], { limit: 2 })], nm([
      Or([stk(K, 1, TOP), st('고통', 2, EA), pw('hit', [st('고통', 1)], { limit: 2 })]),
      Or([pw('hit', [st('고통', 1)], { limit: 2 })], { cost: 0 }),
      Or([stk(K, 1, TOP), pw('huntDown', [st('고통', 2, EA), draw(1)], { limit: 1 })]),
      Or([srch({ type: '공격' }), pw('hit', [st('고통', 1)], { limit: 2 })]),
      Or([st('약화', 2, EA), pw('hit', [st('고통', 1)], { limit: 3 }), disc(1)]),
    ]), bl('atkUp', 'draw', [stk(K, 1, E1)])),
    // 갈래 부품 — 배역 나눠 주기(0): 외계인 지정 + 동료 공격 카드 1장(동료에게 외계인을 치게)
    U(H, 5, '배역 나눠 주기', 0, '스킬', [stk(K, 1, E1), draw(1, { who: 'other', type: '공격' })], [
      Or([stk(K, 1, E1), draw(1, { who: 'other', type: '공격' }), st('고통', 1)]),
      Or([stk(K, 1, E1), draw(1, { who: 'other', type: '공격' })], { tags: ['신속'] }),
      Or([stk(K, 1, E1), draw(1, { who: 'other', type: '공격' }), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })]),
      Or([stk(K, 1, E1), draw(1, { who: 'other', type: '공격' }), pw('hit', [st('고통', 1)], { when: { who: 'other' }, limit: 1 })], { power: true }),
      Or([stk(K, 1, E1), draw(2, { who: 'other', type: '공격' }), disc(1)]),
    ], bl('draw', { tags: ['보존'] }, [stk(K, 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 10. 헤일리(멀쩡) — 버티형(변환) · 탱커 · 광기. 공격을 다 막아 낼 때마다 「극복의 훈장」 — 훈장을 털어 동료의 족쇄(해로운 효과)를 끊거나 장교 검법으로 치고,
//     넷이면 전함 보급(파티의 다음 카드 강화). 막아 낸 양은 검법 · 기록의 실드로 바뀐다
// 원작: 맞힌 적마다 훈장(최대 4, 해제 불가) · 훈장이 아군 기절 · 변이를 대신 풀고 소모 · 넷째 공격마다 장교 검법(도발) · 고학년 전함 보급(아군 공속 · 피해↑) · 무릎 꿇는 교섭
// 이드(막아 낸 양 → 경보 → 자기 다음 카드)와 가르기: 멀쩡은 막아 낸 「횟수」 → 훈장 → 동료 해제 · 파티 강화
// 시동: u3 이번엔 진짜 전장(그대로)
// ════════════════════════════════════════════════════════════════════
function haileyOk(j) {
  const H = '헤일리_멀쩡', K = '극복의 훈장';
  const h = j.heroes[0];
  h.blurb = '망상에서 걸어 나와 진짜 전장을 지휘하는 헤일리. 공격을 다 막아 낼 때마다 훈장이 늘고, 훈장으로 동료의 족쇄를 끊거나 검을 휘두르며, 넷이 모이면 전함 보급이 파티의 다음 한 수를 받쳐 줍니다.';
  h.keyword = { name: K, desc: '마주 본 과거의 훈장', carrier: 'self', cap: 4, per: [{ stat: 'taken', v: -0.04 }], onMax: { empower: 'any', ratio: 0.4, consume: true } };
  h.passives = [
    pas('다 막아 낸 전열', 'blocked', [stk(K, 1)], { limit: 2 }),
    pas('잊을 수 없는 기억', 'fightStart', [stk(K, 2)]),
    pas('잊을 수 없는 기억', 'turnStart', [cleanse(1)], { conds: [{ c: 'stack', id: K, n: 1 }] }),
  ];
  setCards(j, [
    // 원작 자유 — 나아가는 결의: 적 전체 방어 기반 + 회복 + 훈장
    U(H, 1, '나아가는 결의', 1, '공격', [ddef(0.45, EA), heal(0.5), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.52, EA), heal(0.55), cleanse(1)]),
      Or([ddef(0.45, EA), heal(0.5), pw('blocked', [ddef(0.22, EA)], { limit: 1 })], { power: true }),
      Or([ddef(0.7, EA), heal(0.7), disc(1)]),
    ], bl('power', 'heal', [stk(K, 1)])),
    // 쓰기 — 엘피니아 장교 검법: 방어 기반 + 훈장 1개당, 훈장 전부 소모
    U(H, 2, '엘피니아 장교 검법', 1, '공격', [ddef(0.5), per(K), ddef(0.22), spendAll(K)], [
      'A',
      ['D', 'blocked', [ddef(0.25)], { limit: 1 }],
      Or([ddef(0.45, EA), per(K), ddef(0.18, EA), spendAll(K)]),
      Or([ddef(0.45), per(K), ddef(0.2), st('약화', 1)]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 이번엔 진짜 전장: 실드 + 훈장 + 드로우
    U(H, 3, '이번엔 진짜 전장', 1, '스킬', [sh(0.8), stk(K, 1), draw(1)], nm([
      Or([sh(1.0), stk(K, 1), draw(1)]),
      Or([sh(0.4), stk(K, 1), draw(1)], { cost: 0 }),
      Or([sh(0.7), stk(K, 2), cleanse(1)]),
      Or([sh(0.8), stk(K, 1), pw('blocked', [stk(K, 1), sh(0.2)], { limit: 1 })], { power: true }),
      Or([sh(1.3), stk(K, 2), disc(1)]),
    ]), null),
    // 갈래 부품 — 극복의 기록: 실드 + 지난 판에 막아 낸 양 40당 실드 + 해로운 효과 하나 해제
    U(H, 4, '극복의 기록', 1, '스킬', [sh(0.75), perG(40), sh(0.15), cleanse(1)], [
      'A', 'B',
      Or([sh(0.7), perG(40), sh(0.14), stk(K, 1)]),
      ['D', 'guardSum', [sh(0.5)], { limit: 1 }],
      Or([sh(1.25), perG(40), sh(0.25), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 둘째 — 무릎 꿇는 교섭(0): 실드 + 훈장 + 적 1명 약화
    U(H, 5, '무릎 꿇는 교섭', 0, '스킬', [sh(0.55), stk(K, 1), st('약화', 1)], [
      'A', 'B',
      Or([sh(0.6), stk(K, 1), st('약화', 1, EA)]),
      ['D', 'turnStart', [sh(0.35)]],
      Or([sh(0.9), stk(K, 2), disc(1)]),
    ], bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '엘프/하이디': 0.65, '엘프/이드': 0.85, '엘프/이드_재활': 0.78, '엘프/오르': 1.25, '엘프/타이다': 1.4, '엘프/페스타': 1.0, '엘프/칸나': 0.9 };
// ── 돌리기 ──
const JOBS = [
  ['엘프/오르', or], ['엘프/이드', ide], ['엘프/이드_재활', ideRehab], ['엘프/칸나', kanna], ['엘프/캐시', cathy],
  ['엘프/타이다', taida], ['엘프/페스타', festa], ['엘프/하이디', heidi], ['엘프/헤일리', hailey], ['엘프/헤일리_멀쩡', haileyOk],
];
L.run18(JOBS, TUNE, new URL('./boost_엘프B_18.json', import.meta.url));
