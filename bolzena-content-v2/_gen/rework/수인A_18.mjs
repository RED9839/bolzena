// 18갈래 재설계 3단계 — 수인A 묶음 10명(그윈 · 델리아 · 디아나 · 란 · 루포 · 리온 · 마고 · 밍스 · 바나 · 버터). 2026-10-08
// 기준: 05_시범16_결과.md · 지침 _gen/rework/BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/수인A.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(쓰기 · 갈래 부품 · 원작 자유 · 둘째) · 신탁 5갈래 · 축복 12(시동은 공용 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/수인A_18.mjs [사도 이름 일부]  → heroes/수인/<파일>.json 덮어쓰기(백업 heroes_before_118 에서 읽음)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, draw, make, ap, ifStack, ifKill, ifWounded, power, rule,
  TOP, ALLY, STRONG, tough, disc, nextAp, gauge, empower, spendN, cs, perCs, perOver, pw, pas, token, xtra, srch, drawType, pull,
  Or, U, bl, setCards } = L;
const MARK = 'markedEnemy', LOW = 'lowEnemy';
const payHp = v => ({ k: 'payHp', v });
const perPaid = p => ({ k: 'perPaid', per: p });
const perGuarded = p => ({ k: 'perGuarded', per: p });
const ifSpent = n => ({ k: 'ifSpent', n });
const named = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));

// ════════════════════════════════════════════════════════════════════
// 1. 그윈 — 표적형 · 서포터 · 냉정. 먼저 깃발을 꽂은 적 하나를 끝까지 찜한다(찍기 — 옮기면 처음부터). 찜한 적이 쓰러지면 지도를 그리고 다음 땅으로
// 원작: 어디든 깃발 「먼저 찜한 쪽이 임자」 · 새 땅에 동생 이름을 붙이고 지도를 만든다 · 저학년 깃발 안개(적 피해↓) · 동상 적 기절
// 시동: u1 스노우포그(그대로) · 장치가 자기 카드를 만든다(찜한 적이 쓰러지면 「탐험 지도」)
// ════════════════════════════════════════════════════════════════════
function gwin(j) {
  const H = '그윈', K = '깃발', MAP = '그윈_map';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '먼저 찜한 적에게 꽂는 탐험 깃발', carrier: 'enemy', cap: 3, hunt: true, weakens: true };
  delete h.keywords;
  h.passives = [
    pas('먼저 찜한 쪽이 임자', 'play', [stk(K, 1, MARK)], { when: { type: '공격' }, limit: 2 }),
    pas('델리아 섬', 'huntDown', [make(MAP, 1), stk(K, 1, TOP)]),
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === K ? { ...f, target: MARK } : f));
  const tokens = [token(MAP, '탐험 지도', H, '스킬', [draw(1), stk(K, 1, MARK)], { blurb: '찜한 땅에 동생 이름을 붙여 그린 지도 한 장' })];
  setCards(j, [
    // 시동 — 고른 적에게 깃발 둘(찜) + 적 전체 약화(안개)
    U(H, 1, '스노우포그', 1, '스킬', [stk(K, 2), st('약화', 1, EA)], named([
      Or([stk(K, 2), st('약화', 1, EA), sh(0.6)]),
      Or([stk(K, 1), st('약화', 1, EA)], { cost: 0 }),
      Or([stk(K, 2), st('약화', 1, EA), srch()]),
      Or([stk(K, 1), st('약화', 1, EA), pw('turnStart', [stk(K, 1, MARK)])], { power: true }),
      Or([stk(K, 3), st('약화', 2, EA), disc(1)]),
    ]), null),
    // 원작 자유 — 얼음 가방: 적 전체 방어 기반 + 깃발 + 실드
    U(H, 2, '얼음 가방', 1, '공격', [ddef(0.5, EA), stk(K, 1), sh(0.6)], [
      'A', ['D', 'hit', [sh(0.45)], { when: { who: 'any', weak: true }, limit: 1 }],
      Or([ddef(0.95), stk(K, 2)]),
      Or([ddef(0.45, EA), stk(K, 1, MARK), srch()]),
      'Hd',
    ], bl('power', 'frost', [stk(K, 1, MARK)])),
    // 둘째 — 신나는 모험이다!(강화): 실드 + 매 턴 찜한 적에게 깃발
    U(H, 3, '신나는 모험이다!', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1, MARK)])], named([
      Or([st('사기', 1), stk(K, 1, MARK), pw('turnStart', [stk(K, 1, MARK)])]),
      Or([st('사기', 1), pw('turnStart', [stk(K, 1, MARK)])], { cost: 0 }),
      Or([st('사기', 1), power(rule('turnStart', [stk(K, 1, MARK)]), rule('huntDown', [draw(1)], { limit: 1 }))]),
      Or([st('사기', 1), srch(), pw('turnStart', [stk(K, 1, MARK)])]),
      Or([st('사기', 2), pw('turnStart', [stk(K, 1, MARK)]), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1, MARK)])),
    // 쓰기 — 깃발 꽂고 인증샷: 깃발 1개당 피해 · 실드, 전부 뽑아 든다
    U(H, 4, '깃발 꽂고 인증샷', 1, '스킬', [per(K), dmg(0.42), per(K), sh(0.42), L.spendAll(K)], [
      'A', 'B',
      Or([per(K, { n: 1 }), dmg(0.45), L.spendAll(K), ifStack(K, 3), st('기절', 1)]),
      Or([per(K), dmg(0.4), per(K), sh(0.4), srch()]),
      'Hn',
    ], bl('guard', 'cost', [stk(K, 1, MARK)])),
    // 갈래 부품 — 전설의 나침반(0): 찜한 적에게 깃발(옮기지 않음) + 드로우
    U(H, 5, '전설의 나침반', 0, '스킬', [stk(K, 1, MARK), draw(1)], [
      'A', 'B',
      Or([stk(K, 1, MARK), make(MAP, 1)]),
      Or([stk(K, 1, MARK), draw(1), pw('huntDown', [draw(1)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('ap', { tags: ['보존'] }, [stk(K, 1, MARK)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 2. 델리아 — 대가형 · 딜러 · 순수. 꼬마 대장은 겁이 없다 — HP · 다음 턴 AP 를 치르고 그만큼 다음 카드가 세진다(「맨몸 수영」)
// 원작: 친구를 구하려 맨몸으로 바다를 헤엄친 용기 · 수족열증(델리아에게 받는 피해↑) · 어사이드 「이글루 도망」(HP 50% 밑 — 디버프 털고 숨어 회복) → 원작 자유 카드
// 시동: u1 델리아 화났어!(그대로)
// ════════════════════════════════════════════════════════════════════
function delia(j) {
  const H = '델리아', K = '수족열증';
  const h = j.heroes[0];
  h.passives = [
    pas('펭귄 탐험대', 'play', [stk(K, 1)], { when: { type: '공격', who: 'other' }, limit: 1 }),
    pas('맨몸 수영', 'pay', [empower(0.3)], { limit: 1 }),
  ];
  setCards(j, [
    // 시동 — 적 전체 피해 + 수족열증
    U(H, 1, '델리아 화났어!', 1, '공격', [dmg(0.45, EA), stk(K, 1, EA)], named([
      Or([dmg(0.6, EA), stk(K, 1, EA)]),
      Or([dmg(0.4, EA), stk(K, 1, EA)], { cost: 0 }),
      Or([payHp(40), dmg(0.7, EA), stk(K, 2, EA)]),
      Or([dmg(0.35, EA), stk(K, 1, EA), pw('pay', [stk(K, 1, EA)], { limit: 1 })], { power: true }),
      Or([dmg(0.4, EA), stk(K, 1, EA), srch({ type: '공격' })]),
    ]), null),
    // 원작 자유 — 이글루 도망: 디버프를 털고 회복, 부상이면 숨는다(피해 감소)
    U(H, 2, '이글루 도망', 1, '스킬', [{ k: 'cleanse', v: 1 }, heal(0.9), ifWounded, st('피해 감소', 2)], [
      'A', 'B',
      Or([{ k: 'cleanse', v: 1 }, sh(1.2), ifWounded, heal(0.8)]),
      Or([{ k: 'cleanse', v: 1 }, heal(0.8), srch({ type: '공격' })]),
      Or([{ k: 'cleanse', v: 1 }, sh(1.6), st('피해 감소', 2)]),
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 쓰기 — 이글루 배치기(2): HP 를 치르고 몸통 박치기, 수족열증 1개당 · 이번 턴 치른 HP 40당
    U(H, 3, '이글루 배치기', 2, '공격', [payHp(30), dmg(1.6), perPaid(40), dmg(0.35)], [
      'A', 'B',
      Or([payHp(30), dmg(1.0, EA), perPaid(40), dmg(0.2, EA)]),
      Or([dmg(1.6), per(K), dmg(0.3), srch()]),
      Or([payHp(60), dmg(2.0), perPaid(40), dmg(0.4)]),
    ], bl('power', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 일어나! 탐험 가야지!(0): 내일 힘을 당겨 쓴다(다음 턴 AP -1 · 이번 턴 AP +1 · 드로우)
    U(H, 4, '일어나! 탐험 가야지!', 0, '스킬', [nextAp(-1), ap(1), draw(1)], named([
      Or([nextAp(-1), ap(1), draw(2)]),
      Or([ap(1), stk(K, 2, EA), nextAp(-1)]),
      Or([payHp(40), ap(1), draw(1)]),
      Or([nextAp(-1), ap(1), pw('pay', [draw(1), stk(K, 1, EA)], { limit: 1 })], { power: true }),
      Or([nextAp(-1), ap(1), drawType('공격', 2)]),
    ]), bl('draw', 'cost', [stk(K, 1, EA)])),
    // 둘째 — 탐험대 소집: 적 전체 수족열증 + 실드 + 드로우
    U(H, 5, '탐험대 소집', 1, '스킬', [stk(K, 1, EA), sh(0.75), draw(1)], [
      'A', 'B',
      Or([stk(K, 1, EA), per(K, { each: true }), dmg(0.15, EA), sh(0.6)]),
      Or([stk(K, 1, EA), sh(0.6), pw('turnStart', [stk(K, 1, EA)])], { power: true }),
      Or([stk(K, 2, EA), sh(1.3), disc(1)]),
    ], bl('guard', 'weakSpot', [stk(K, 1, EA)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 디아나(촌장) — 회복형 · 서포터 · 광기. 때려서 고치고, 넘치게 고친 몫은 「기혈」로 — 기혈이 차오르면 실눈이 떠진다(다음 카드 강화)
// 원작: 저학년 아군 전체 회복 두 번 · 평타가 피해를 주며 아군 회복 · 고학년 기의 격류 · 「눈 뜨는 중…」 · 주먹밥
// 디아나(왕년)(도끼 ↔ 맨주먹 두 얼굴 · 백수공권)과 가름 — 촌장은 회복 → 기혈 변환, 털 곳이 회복(u1) · 공격(u4) 둘
// 시동: u2 기공 주문(그대로)
// ════════════════════════════════════════════════════════════════════
function diana(j) {
  const H = '디아나', K = '기혈', RICE = '디아나_rice';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '몸 안을 도는 은퇴한 무인의 기', carrier: 'self', cap: 5, per: [{ stat: 'hot', ratio: 0.1 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('진짜 치료법', 'overheal', [{ k: 'stack', id: K, v: 1, ofEvent: 0.05 }, { k: 'perEvent', per: 20 }, dmg(0.2, ER, { fixed: true })], { when: { pct: 0.8 }, limit: 2 }),
    pas('때리는 치료', 'play', [heal(0.25)], { when: { type: '공격' }, limit: 2 }),
  ];
  setCards(j, [
    // 쓰기(회복) — 자연 치유: 기혈 1개당 회복, 전부 소모 + 결의
    U(H, 1, '자연 치유', 1, '스킬', [per(K, { n: 1 }), heal(0.38), spendAll(K), st('결의', 1)], [
      'A', 'B',
      Or([per(K, { n: 1 }), sh(0.5), spendAll(K), st('결의', 1)]),
      Or([per(K, { n: 1 }), heal(0.3), st('결의', 1), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([heal(0.8), st('결의', 1), stk(K, 2)]),
    ], bl('heal', 'defUp', [stk(K, 1)])),
    // 시동 — 기공 주문: 피해 + 흡수(회복) + 기혈
    U(H, 2, '기공 주문', 1, '공격', [dmg(1.0), drain(0.6), stk(K, 1)], named([
      Or([dmg(1.1), drain(0.6), stk(K, 2)]),
      Or([dmg(0.8), drain(0.5), stk(K, 1)], { cost: 0 }),
      Or([dmg(1.0, EA), heal(0.7), stk(K, 1)]),
      Or([dmg(1.1), drain(0.6), pw('play', [heal(0.3)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([dmg(1.2), drain(0.6), srch({ type: '스킬' })]),
    ]), null),
    // 원작 자유 — 수인 마을 잔칫상(2): 파티 회복 + 주먹밥 둘
    U(H, 3, '수인 마을 잔칫상', 2, '스킬', [heal(1.3), make(RICE, 2)], [
      'A', 'B',
      Or([heal(1.0), make(RICE, 2), stk(K, 2)]),
      Or([heal(1.0), make(RICE, 1), pw('turnStart', [make(RICE, 1)])], { power: true }),
      'Hd',
    ], bl('heal', 'ap', [make(RICE, 1)])),
    // 쓰기(공격) — 은퇴한 무인의 수련: 기혈 1개당 피해, 전부 소모 + 결정화
    U(H, 4, '은퇴한 무인의 수련', 1, '공격', [per(K, { n: 1 }), dmg(0.38), spendAll(K), st('결정화', 1)], [
      'A',
      ['C', [tough(1)]],
      Or([per(K, { n: 1 }), dmg(0.3, EA), spendAll(K), st('결정화', 1)]),
      Or([per(K, { n: 1 }), dmg(0.4), st('결정화', 1), srch()]),
      Or([per(K, { n: 1 }), dmg(0.6), st('결정화', 1), disc(1)]),
    ], bl('power', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 해바라기 톡톡(0): 피해 + 이번 판 넘친 회복 20당 한 대 더
    U(H, 5, '해바라기 톡톡', 0, '공격', [dmg(0.4), perOver(0.8, 20), dmg(0.12)], [
      'A', 'B',
      Or([dmg(0.35), drain(0.5), stk(K, 1)]),
      Or([dmg(0.35), perOver(0.8, 20), dmg(0.1), pw('overheal', [dmg(0.25, ER)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([dmg(0.4), perOver(0.8, 20), dmg(0.12), draw(1)]),
    ], bl('weakSpot', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 란 — 표적형 · 딜러 · 순수 · 엘다인. 늑대 표식은 여럿에게 퍼뜨리고, 그중 한 놈을 「사냥감」으로 문다 — 쓰러지면 바로 다음 사냥감
// 원작: 저학년 범위 베기로 맞은 적 모두에게 표식 · 고학년 몰이 사냥(표식 · 약한 적 먼저, 처치하면 다시) · 4타째 강화 참격 · 투구꽃 독
// 엘다인 한 단계: 사냥감이 쓰러지면 HP 가장 낮은 적이 새 사냥감 + 표식 2 + 고학년 게이지
// 시동: u1 아랑의 여유(그대로)
// ════════════════════════════════════════════════════════════════════
function ran(j) {
  const H = '란', K = '늑대 표식', P = '사냥감';
  const h = j.heroes[0];
  h.keywords = [{ name: P, desc: '무리 사냥에서 먼저 물고 늘어질 한 놈', carrier: 'enemy', cap: 1, hunt: true, per: [{ stat: 'taken', v: 0.1, from: 'owner' }] }];
  h.passives = [
    pas('허공 가르기', 'play', [dmg(0.45, EA), stk(K, 1, EA)], { when: { type: '공격', every: 3 } }),
    pas('란의 공포', 'huntDown', [stk(P, 1, LOW), stk(K, 2, LOW), gauge(20)]),
  ];
  setCards(j, [
    // 시동 — 적 전체 피해 + 표식, HP 가장 낮은 적을 사냥감으로
    U(H, 1, '아랑의 여유', 1, '공격', [dmg(0.7, EA), stk(K, 1, EA), stk(P, 1, LOW)], named([
      Or([dmg(0.9, EA), stk(K, 1, EA), stk(P, 1, LOW)]),
      Or([dmg(0.55, EA), stk(K, 1, EA), stk(P, 1, LOW)], { cost: 0 }),
      Or([dmg(1.1), stk(K, 2), stk(P, 1)]),
      Or([dmg(0.65, EA), stk(K, 1, EA), pw('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 })], { power: true }),
      Or([dmg(0.75, EA), stk(K, 1, EA), srch({ type: '공격' })]),
    ]), null),
    // 쓰기 — 사랑니 발도술: 표식 1개당 발도, 전부 소모. 처치하면 HP 가장 낮은 적에게 추가 공격
    U(H, 2, '사랑니 발도술', 1, '공격', [per(K, { n: 1 }), dmg(0.5), spendAll(K), ifKill, xtra(0.8, LOW)], [
      'A', 'B',
      Or([per(K, { n: 1 }), dmg(0.34, EA), spendAll(K)]),
      Or([per(K, { n: 1 }), dmg(0.5), ifStack(P, 1), xtra(0.8), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 원작 자유 — 투구꽃 독: 표식 둘 + 고통 + 드로우
    U(H, 3, '투구꽃 독', 1, '스킬', [stk(K, 2), st('고통', 3), draw(1)], [
      'A', 'B',
      Or([stk(K, 2), st('고통', 3), dmg(0.7)]),
      Or([stk(K, 2), st('고통', 4), pw('turnStart', [st('고통', 3, MARK)])], { power: true }),
      Or([stk(K, 3), dmg(1.2), disc(1)]),
    ], bl('cost', [st('고통', 2)], 'draw')),
    // 갈래 부품 — 날선 늑대의 발톱: 피해 + 표식, 사냥감이면 한 번 더
    U(H, 4, '날선 늑대의 발톱', 1, '공격', [dmg(0.75), stk(K, 1), ifStack(P, 1), dmg(0.6)], [
      'A',
      ['D', 'huntDown', [dmg(0.7, EA)], { limit: 1 }],
      Or([dmg(0.7, EA), ifStack(P, 1), dmg(0.6)]),
      Or([dmg(0.95), stk(K, 1), srch()]),
      'Hn',
    ], bl('atkUp', 'weakSpot', [stk(K, 1, EA)])),
    // 둘째 — 아기 늑대의 기억(0): 고른 적을 사냥감으로 + 표식 + 드로우
    U(H, 5, '아기 늑대의 기억', 0, '스킬', [stk(P, 1), stk(K, 1), draw(1)], [
      'A', 'B',
      Or([stk(P, 1), stk(K, 1), dmg(0.7)]),
      Or([stk(P, 1), stk(K, 1), pw('huntDown', [stk(K, 1, EA), draw(1)], { limit: 1 })], { power: true }),
      Or([stk(P, 1), stk(K, 2, EA), dmg(0.6, EA)]),
    ], bl('ap', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 루포 — 계산형 · 딜러 · 활발. 작전 쪽지로 계획을 세우고 이번 턴 쓴 AP 를 꼭 맞춘다 — 셈이 맞으면 계획 +1, 계획이 다 차면 다음 카드에 몰아준다
// 원작: 사료스탕스의 책사 · 쪽지에 장난 계획을 쓰고 지우고 · 게임 룰을 슬쩍 바꿔 이김 · 신속베기는 아티팩트 수만큼 타수
// 시동: u2 작전 지도 펼치기(그대로) · 장치(전투 시작)가 작전 쪽지를 만든다(전과 같음)
// ════════════════════════════════════════════════════════════════════
function rufo(j) {
  const H = '루포', K = '계획', MEMO = '루포_memo';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '루포 님의 치밀한 작전', carrier: 'self', cap: 4, onMax: { make: '루포_plan', consume: true } };
  h.passives = [
    pas('치밀한 계획', 'tally', [stk(K, 1), draw(1)], { limit: 1 }),
    pas('삼총사의 대모험', 'fightStart', [make(MEMO, 1)]),
  ];
  const tokens = [token('루포_plan', '계획대로!', H, '공격', [hits(4, 0.4, ER), draw(1)], { blurb: '쪽지에 적어 둔 그대로 — 여우가 한 바퀴 돌며 베어 냅니다' })];
  setCards(j, [
    // 원작 자유 — 루포류 신속베기(신속): 3회 + 고통 + 계획
    U(H, 1, '루포류 신속베기', 1, '공격', [dmg(0.45, E1, { hits: 3 }), st('고통', 2), stk(K, 1)], [
      'A', ['D', 'tally', [dmg(0.5, ER)], { limit: 1 }],
      Or([dmg(0.6, EA, { hits: 2 }), stk(K, 1)]),
      Or([dmg(0.5, E1, { hits: 3 }), stk(K, 1), srch()]),
      ['Ht', '신속'],
    ], bl('power', 'weakSpot', [stk(K, 1)]), ['신속']),
    // 시동 — 작전 지도 펼치기: 계획 + 작전 쪽지 + 드로우
    U(H, 2, '작전 지도 펼치기', 1, '스킬', [stk(K, 1), make(MEMO, 1), draw(1)], named([
      Or([stk(K, 2), make(MEMO, 1), draw(1)]),
      Or([stk(K, 1), make(MEMO, 1)], { cost: 0 }),
      Or([make(MEMO, 2), stk(K, 1), ifSpent(2), draw(1)]),
      Or([stk(K, 1), make(MEMO, 1), pw('tally', [make(MEMO, 1)], { limit: 1 })], { power: true }),
      Or([stk(K, 2), make(MEMO, 2), disc(1)]),
    ]), null),
    // 갈래 부품 — 눈속임 단검: 피해. 이번 턴 쓴 AP 가 꼭 3이면 계획 +2 · 드로우
    U(H, 3, '눈속임 단검', 1, '공격', [dmg(1.0), ifSpent(3), stk(K, 2), draw(1)], [
      'A', 'B',
      Or([dmg(0.8), { k: 'perTag', id: MEMO }, dmg(0.35), ifSpent(3), stk(K, 1)]),
      Or([dmg(1.3), ifSpent(2), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 — 오의 여우 꼬리(2): 피해 + 계획 1개당, 전부 소모
    U(H, 4, '오의 여우 꼬리', 2, '공격', [dmg(2.3), per(K), dmg(0.55), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.4, EA), per(K), dmg(0.35, EA), spendAll(K)]),
      Or([dmg(2.1), per(K), dmg(0.55), ifSpent(3), ap(1)]),
      'Hn',
    ], bl('power', 'ap', [make(MEMO, 1)])),
    // 둘째 — 여우의 꼼수(0): 작전 쪽지 + 손의 자신의 공격 카드 비용 -1(셈을 맞추는 룰 바꾸기)
    U(H, 5, '여우의 꼼수', 0, '스킬', [make(MEMO, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })], [
      Or([make(MEMO, 2), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })]),
      Or([make(MEMO, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), draw(1)]),
      Or([make(MEMO, 1), cs('비용', 1, { to: 'hand', n: 1, who: 'self', type: '공격' }), draw(1)]),
      Or([make(MEMO, 1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), pw('tally', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([make(MEMO, 2), cs('비용', -1, { to: 'hand', n: 2, who: 'self', type: '공격' }), nextAp(-1)]),
    ], bl('cost', { tags: ['보존'] }, [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 6. 리온 — 버티형 · 탱커 · 우울. 막아 낸 만큼 적에게 「유죄」 판결 — 유죄인 적은 주먹이 무뎌지고, 판결봉은 막아 낸 양을 되돌려 친다
// 원작: 저학년 유죄 선언(앞의 적들에 심판 — 리온에게 주는 피해↓, 잃은 HP 회복) · 고학년 즉결심판(범위 3타 · 실드 깨기) · 도발 · 아군 보호 없음
// 시동: u1 유죄 선언(그대로)
// ════════════════════════════════════════════════════════════════════
function lion(j) {
  const H = '리온', K = '유죄';
  const h = j.heroes[0];
  h.passives = [
    pas('판결봉', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    pas('정당방위', 'guardSum', [stk(K, 1, EA)]),
  ];
  setCards(j, [
    // 시동 — 유죄 선언: 적 전체 유죄 + 실드, 부상이면 회복
    U(H, 1, '유죄 선언', 1, '스킬', [stk(K, 1, EA), sh(1.1), ifWounded, heal(0.6)], named([
      Or([stk(K, 1, EA), sh(1.4), ifWounded, heal(0.6)]),
      Or([stk(K, 1, EA), sh(0.8)], { cost: 0 }),
      Or([stk(K, 2), sh(1.0), draw(1)]),
      Or([stk(K, 1, EA), sh(0.8), pw('blocked', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([stk(K, 1, EA), sh(1.0), srch({ type: '공격' })]),
    ]), null),
    // 둘째 — 수수께끼 하나 더: 실드 + 드로우 + 유죄
    U(H, 2, '수수께끼 하나 더', 1, '스킬', [sh(1.3), draw(1), stk(K, 1)], [
      'A', 'B',
      Or([sh(1.9), stk(K, 1, EA), ifWounded, heal(0.9)]),
      Or([sh(1.6), stk(K, 1), pw('guardSum', [sh(0.7)])], { power: true }),
      'Hd',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 쓰기 — 심판의 망치: 방어 기반 + 유죄 1개당, 전부 소모
    U(H, 3, '심판의 망치', 1, '공격', [ddef(0.55), per(K), ddef(0.28), spendAll(K)], [
      'A',
      ['C', [{ k: 'strip' }]],
      Or([ddef(0.4, EA), per(K, { each: true }), ddef(0.2, EA), spendAll(K)]),
      Or([ddef(0.5), per(K), ddef(0.25), srch()]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 라이온 시그널(강화): 실드 + 공격을 다 막을 때마다 그 적에게 유죄
    U(H, 4, '라이온 시그널', 1, '강화', [st('결의', 1), pw('blocked', [stk(K, 1)], { limit: 2 })], named([
      Or([st('결의', 1), sh(0.8), pw('blocked', [stk(K, 1)], { limit: 2 })]),
      Or([st('결의', 1), pw('blocked', [stk(K, 1)], { limit: 2 })], { cost: 0 }),
      Or([st('결의', 1), pw('blocked', [stk(K, 1), sh(0.35)], { limit: 2 })]),
      Or([st('결의', 1), srch(), pw('blocked', [stk(K, 1)], { limit: 2 })]),
      Or([st('결의', 2), pw('blocked', [stk(K, 1)], { limit: 2 }), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1)])),
    // 원작 자유 — 히어로 펀치: 방어 기반 + 막아 낸 양 30당 더 + 유죄
    U(H, 5, '히어로 펀치', 1, '공격', [ddef(0.5), perGuarded(30), ddef(0.12), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.45, EA), perGuarded(30), ddef(0.1, EA), stk(K, 1, EA)]),
      Or([ddef(0.55), perGuarded(30), ddef(0.12), pw('guardSum', [ddef(0.5)])], { power: true }),
      'Hd',
    ], bl('weakSpot', 'guard', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 마고 — 회복형 · 서포터 · 순수. 오래 도는 회복을 넘치게 — 넘친 몫은 「목장 친구」가 되고, 넷이 모이면 양 친구를 보낸다(손에 카드)
// 원작: 저학년 「마고 마구 회복해」 매초 아군 전체 회복 · 고학년 「메…에~류겐!」 양 친구를 보내 적 하나에 큰 피해 · 작은 친구들 · 사료
// 시동: u2 사료가 제일 맛있어(그대로) · 장치가 자기 카드를 만든다(목장 친구가 다 차면 「양 친구」)
// ════════════════════════════════════════════════════════════════════
function mago(j) {
  const H = '마고', K = '목장 친구', FEED = '마고_feed', SHEEP = '마고_sheep';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '곁에 모여드는 작은 동물들', carrier: 'self', cap: 4, per: [{ stat: 'hot', ratio: 0.2 }, { stat: 'taken', v: -0.04 }], onMax: { make: SHEEP, consume: true } };
  h.passives = [
    pas('비스트 로드', 'overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 2 }),
    pas('아이들은 건드리지 마', 'hurt', [per(K), ddef(0.2)], { when: { guarded: true }, limit: 1 }),
  ];
  const tokens = [token(SHEEP, '양 친구', H, '공격', [ddef(1.3), tough(1)], { blurb: '메⋯에~! 목장 친구들이 한데 뭉쳐 들이받습니다' })];
  setCards(j, [
    // 원작 자유 — 마고 마구 회복해: 회복 + 목장 친구 + 초재생(여러 턴)
    U(H, 1, '마고 마구 회복해', 1, '스킬', [heal(0.8), stk(K, 1), st('초재생', 1)], [
      'A', 'B',
      Or([st('초재생', 1), stk(K, 2), perOver(0.8, 15), sh(0.4)]),
      Or([heal(0.7), st('초재생', 2), pw('turnStart', [heal(0.3)])], { power: true }),
      Or([heal(1.1), st('초재생', 2), disc(1)]),
    ], bl('heal', 'defUp', [stk(K, 1)])),
    // 시동 — 사료가 제일 맛있어: 사료 둘 + 드로우
    U(H, 2, '사료가 제일 맛있어', 1, '스킬', [make(FEED, 2), draw(1)], named([
      Or([make(FEED, 3), draw(1)]),
      Or([make(FEED, 1), draw(1)], { cost: 0 }),
      Or([make(FEED, 3), heal(0.5)]),
      Or([make(FEED, 2), draw(1), pw('overheal', [make(FEED, 1)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([make(FEED, 3), draw(2), disc(1)]),
    ]), null),
    // 쓰기 — 내 목장에서 나가: 방어 기반 + 목장 친구 1개당, 전부 소모(양 친구를 기다리지 않고)
    U(H, 3, '내 목장에서 나가', 1, '공격', [ddef(0.5), per(K), ddef(0.25), spendAll(K)], [
      'A',
      Or([ddef(0.8), per(K), ddef(0.4), st('기절', 1)], { cost: 2 }),
      Or([ddef(0.4, EA), per(K), ddef(0.18, EA), spendAll(K)]),
      Or([ddef(0.5), per(K), ddef(0.22), srch()]),
      'Hd',
    ], bl('power', 'frost', [stk(K, 1)])),
    // 갈래 부품 — 아기 동물 돌보기(강화): 결의 + 회복이 넘칠 때마다 목장 친구
    U(H, 4, '아기 동물 돌보기', 1, '강화', [st('결의', 1), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })], named([
      Or([st('결의', 1), heal(0.5), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })]),
      Or([heal(0.4), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })], { cost: 0 }),
      Or([st('결의', 1), pw('overheal', [stk(K, 1), sh(0.5)], { when: { pct: 0.8 }, limit: 1 })]),
      Or([st('결의', 1), srch(), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 1 })]),
      Or([st('결의', 2), pw('overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 2 }), nextAp(-1)]),
    ]), bl('heal', 'draw', [make(FEED, 1)])),
    // 둘째 — 양몰이 돌진: 방어 기반 + 목장 친구 + 사료
    U(H, 5, '양몰이 돌진', 1, '공격', [ddef(0.6), stk(K, 1), make(FEED, 1)], [
      'A', 'B',
      Or([ddef(0.5, EA), make(FEED, 1), heal(0.3)]),
      Or([ddef(0.6), make(FEED, 1), srch({ type: '스킬' })]),
      Or([ddef(1.0), make(FEED, 2), disc(1)]),
    ], bl('weakSpot', 'ap', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 8. 밍스 — 성장형 · 딜러 · 활발 · 1성. 졸업을 미루며 제자(아군)의 주먹을 키우다가, 꽉 찼을 때 「졸업 시험」으로 털면 그 시험지가 판 내내 자란다
// 원작: 촌장을 졸라 교사가 됨 · 루포 · 베니를 하루 만에 졸업시킨 뒤 허전해 쵸피 졸업을 미룸 · 가르치며 나도 배운다 · 저학년 고함 · 고학년 단일 한 방
// 시동: u4 오늘은 자습(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function minx(j) {
  const H = '밍스', K = '졸업 미룸', G = '졸업생';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '제자를 붙잡아 두는 핑계', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.12, who: 'allies' }] };
  h.passives = [
    pas('시범 수업', 'play', [stk(K, 1)], { when: { type: '공격', who: 'other' }, limit: 1 }),
    pas('개학', 'fightStart', [stk(K, 1)]),
  ];
  setCards(j, [
    // 원작 자유 — 캬오오~: 적 전체 + 약화 + 졸업 미룸
    U(H, 1, '캬오오~', 1, '공격', [dmg(0.75, EA), st('약화', 1, EA), stk(K, 1)], [
      'A', ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(1.1), st('약화', 2), stk(K, 1)]),
      Or([dmg(0.7, EA), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'frost', [stk(K, 1)])),
    // 둘째 — 요령은 없고 시범만: 피해 + 졸업 미룸 1개당 + 시작 카드 1장 뽑기
    U(H, 2, '요령은 없고 시범만', 1, '공격', [dmg(0.85), per(K), dmg(0.28), draw(1, { basic: true })], [
      'A', 'B',
      Or([dmg(0.75, EA), per(K), dmg(0.22, EA)]),
      Or([dmg(0.8), per(K), dmg(0.25), pw('play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 1 })], { power: true }),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 쓰기 · 성장 — 졸업 시험: 「졸업생」 1당 적 전체(최소 1). 졸업 미룸이 셋이면 다 털고 이 카드 「졸업생」 +1(판 내내)
    U(H, 3, '졸업 시험', 1, '공격', [perCs(G, { n: 1 }), dmg(0.65, EA), ifStack(K, 2), spendAll(K), cs(G, 1, { max: 6 })], named([
      Or([perCs(G, { n: 1 }), dmg(0.65, EA), ifStack(K, 3), spendAll(K), cs(G, 1, { max: 6 })]),
      Or([perCs(G, { n: 1 }), dmg(0.4, EA), ifStack(K, 3), spendAll(K), cs(G, 1, { max: 6 })], { cost: 0 }),
      Or([perCs(G, { n: 2 }), dmg(0.95), ifKill, cs(G, 1, { max: 6 })]),
      Or([perCs(G, { n: 1 }), dmg(0.55, EA), ifStack(K, 3), cs(G, 1, { max: 6 }), srch()]),
      Or([perCs(G, { n: 1 }), dmg(0.75, EA), disc(1), ifStack(K, 3), cs(G, 1, { max: 6 })]),
    ]), bl('power', 'ap', [stk(K, 1)])),
    // 시동 — 오늘은 자습(개전 강화): 협공 + 매 턴 졸업 미룸
    U(H, 4, '오늘은 자습', 1, '강화', [st('협공', 1), pw('turnStart', [stk(K, 1)])], named([
      Or([st('협공', 1), stk(K, 1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([st('협공', 1), stk(K, 1), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { tags: ['개전'] }),
      Or([st('협공', 1), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([st('협공', 2), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 갈래 부품 — 보충 수업: 졸업 미룸 둘 + 시작 카드 1장 뽑기 + 실드
    U(H, 5, '보충 수업', 1, '스킬', [stk(K, 2), draw(1, { basic: true }), sh(0.7)], [
      'A', 'B',
      Or([stk(K, 3), sh(0.8), cs(G, 1, { to: 'hand', n: 1, who: 'self', max: 6 })]),
      Or([stk(K, 2), sh(0.7), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 3), draw(3, { basic: true }), disc(1)]),
    ], bl('draw', 'cost', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 9. 바나 — 거드는 형 · 서포터 · 활발. 아군 카드가 나갈 때마다 망치질(단조) — 다 벼리면 「벼린 검」이 손에, 그 검으로 손의 동료 카드를 멋대로 개조한다
// 원작: 곱슬쥐 대장장이 · 남의 물건을 허락 없이 개조 · 꿈은 영웅의 검 · 저학년 호두까기 장인(자신 · 후열 실드) · 고학년 모루 탕탕이 · 평타 방깎
// 시동: u3 정령 일꾼들아!(그대로) · 장치가 자기 카드를 만든다(단조가 다 차면 「벼린 검」)
// ════════════════════════════════════════════════════════════════════
function bana(j) {
  const H = '바나', K = '단조', BLADE = '바나_blade';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '두드려 벼리는 영웅의 검', carrier: 'self', cap: 2, per: [{ stat: 'guard', v: 0.1 }], onMax: { make: BLADE, consume: true } };
  h.passives = [
    pas('풀무질', 'play', [stk(K, 1)], { when: { who: 'any' }, limit: 2 }),
    pas('정령 일꾼', 'fightStart', [stk(K, 1), sh(1.5)]),
  ];
  const blade = j.cards.find(c => c.id === BLADE);
  Object.assign(blade, { fx: [st('사기', 1), cs('비용', -1, { to: 'hand', n: 1, who: 'other' })], blurb: '바나의 망치를 거친 쇠붙이 — 동료의 손에 든 것까지 슬쩍 손봐 줍니다' });
  const modOther = (n = 1) => cs('비용', -1, { to: 'hand', n, who: 'other' });
  setCards(j, [
    // 원작 자유 — 호두까기 장인: 큰 실드 + 단조
    U(H, 1, '호두까기 장인', 1, '스킬', [sh(1.7), stk(K, 1)], [
      'A', 'B',
      Or([sh(1.5), stk(K, 1), modOther()]),
      Or([sh(1.5), pw('play', [sh(0.5)], { when: { who: 'other' }, limit: 1 })], { power: true }),
      'Hn',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 둘째 — 모루 위의 망치: 적 전체 + 취약 + 단조
    U(H, 2, '모루 위의 망치', 1, '공격', [dmg(0.9, EA), st('취약', 1, EA), stk(K, 1)], [
      'A', ['D', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([dmg(1.3), st('취약', 2), stk(K, 1)]),
      Or([dmg(0.8, EA), st('취약', 1, EA), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 정령 일꾼들아!: 단조 + 시작 공격 카드 1장을 벼린 검으로 + 드로우
    U(H, 3, '정령 일꾼들아!', 1, '스킬', [stk(K, 1), { k: 'transform', id: BLADE, from: '바나_s1', n: 1 }, draw(1)], named([
      Or([stk(K, 2), { k: 'transform', id: BLADE, from: '바나_s1', n: 1 }, draw(1)]),
      Or([stk(K, 1), { k: 'transform', id: BLADE, from: '바나_s1', n: 1 }], { cost: 0 }),
      Or([stk(K, 1), make(BLADE, 1), sh(0.6)]),
      Or([stk(K, 2), draw(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 2), make(BLADE, 2), disc(1)]),
    ]), null),
    // 쓰기 — 이야기책에 남을 검: 단조 1개당 실드, 전부 소모 + 손의 동료 카드 1장 비용 -1
    U(H, 4, '이야기책에 남을 검', 1, '스킬', [per(K, { n: 1 }), sh(0.5), spendAll(K), modOther()], [
      'A', 'B',
      Or([per(K, { n: 1 }), sh(0.4), spendAll(K), modOther(2)]),
      Or([per(K, { n: 1 }), sh(0.45), modOther(), srch()]),
      'Hn',
    ], bl('guard', 'ap', [make(BLADE, 1)])),
    // 갈래 부품 — 시험 베기: 피해 + 단조 + 파티의 다음 카드 강화
    U(H, 5, '시험 베기', 1, '공격', [dmg(0.9), stk(K, 1), empower(0.35, 'any')], [
      'A', 'B',
      Or([dmg(0.75, EA), modOther(), empower(0.35, 'any')]),
      Or([dmg(0.9), stk(K, 1), pw('play', [empower(0.15, 'any')], { when: { who: 'other', type: '공격' }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'frost', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 10. 버터 — 쌓고 고르기 · 딜러 · 활발. 맞을 때마다 옐로카드를 참고 — 지금 매그넘으로 털까, 넷까지 참아 「레드카드」(손에 카드)를 받을까. 레드카드가 나오면 처음부터
// 원작: 부당한 대우를 참는 한계 · 넘으면 레드카드(난폭한 자아) · 방전되면 0으로 · 애착 아티팩트(분노 100 → 강화 공격이 총으로) · 튕기는 짱돌
// 시동: u2 부탁은 거절 안 해(그대로) · 장치가 자기 카드를 만든다(옐로카드가 다 차면 「레드카드」)
// ════════════════════════════════════════════════════════════════════
function butter(j) {
  const H = '버터', K = '옐로카드', RED = '버터_red';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '꾹 참는 부당한 대우', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.08 }], onMax: { make: RED, consume: true } };
  h.passives = [
    pas('참다 참다', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 3 }),
    pas('심부름 목록', 'play', [stk(K, 1)], { when: { type: '스킬', who: 'other' }, limit: 1 }),
  ];
  const tokens = [token(RED, '레드카드', H, '공격', [dmg(0.8, E1, { hits: 3 }), st('취약', 2)], { blurb: '참고 참던 골든 리트리버가 웃음기를 거둡니다 — 앞치마 속 총이 불을 뿜고, 다시 처음부터' })];
  setCards(j, [
    // 원작 자유 — 버터 플라이!: 튕기는 짱돌 4회 + 옐로카드
    U(H, 1, '버터 플라이!', 1, '공격', [dmg(0.35, ER, { hits: 4 }), stk(K, 1)], [
      'A', ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      Or([dmg(0.3, ER, { hits: 4 }), stk(K, 1), srch()]),
      Or([dmg(1.1), stk(K, 2)]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 시동 — 부탁은 거절 안 해: 실드 + 옐로카드 둘 + 드로우
    U(H, 2, '부탁은 거절 안 해', 1, '스킬', [sh(1.0), stk(K, 2), draw(1)], named([
      Or([sh(1.3), stk(K, 2), draw(1)]),
      Or([sh(0.7), stk(K, 1), draw(1)], { cost: 0 }),
      Or([dmg(0.8), stk(K, 3)]),
      Or([sh(1.0), stk(K, 2), pw('play', [stk(K, 1)], { when: { type: '스킬', who: 'other' }, limit: 1 })], { power: true }),
      Or([sh(1.1), stk(K, 2), pull({ who: 'self', unique: true })]),
    ]), null),
    // 갈래 부품 — 구덩이 파기(보존): 옐로카드 1개당 더, 쓰지 않는다(참는 동안 쓰는 카드)
    U(H, 3, '구덩이 파기', 1, '공격', [dmg(0.75), per(K), dmg(0.22)], [
      'A', 'B',
      Or([dmg(0.55, EA), per(K), dmg(0.15, EA)]),
      Or([dmg(0.7), per(K), dmg(0.2), pw('turnEnd', [stk(K, 1)], { limit: 1 })], { power: true }),
      ['Ht', '보존'],
    ], bl('power', 'frost', [stk(K, 1)]), ['보존']),
    // 쓰기 — 에이프런 매그넘(2): 피해 + 옐로카드 1개당, 전부 소모(레드카드 전에 턴다)
    U(H, 4, '에이프런 매그넘', 2, '공격', [dmg(1.7), per(K), dmg(0.4), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.2, EA), per(K), dmg(0.28, EA), spendAll(K)]),
      Or([dmg(1.6), per(K), dmg(0.38), srch()]),
      'Hn',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 둘째 — 공 물어 오기: 옐로카드 + 실드 + 버린 더미의 고유 카드 1장
    U(H, 5, '공 물어 오기', 1, '스킬', [stk(K, 1), sh(0.9), pull({ who: 'self', unique: true })], [
      'A', 'B',
      Or([stk(K, 2), dmg(0.7), pull({ who: 'self', unique: true })]),
      Or([stk(K, 1), sh(0.9), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 3), sh(1.5), disc(1)]),
    ], bl('draw', 'cost', [stk(K, 1)])),
  ], tokens);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '수인/바나': 1.3, '수인/밍스': 1.4, '수인/디아나': 1.3, '수인/루포': 1.25, '수인/마고': 1.3, '수인/리온': 1.15 };
// ── 돌리기 ──
const JOBS = [
  ['수인/그윈', gwin], ['수인/델리아', delia], ['수인/디아나', diana], ['수인/란', ran], ['수인/루포', rufo],
  ['수인/리온', lion], ['수인/마고', mago], ['수인/밍스', minx], ['수인/바나', bana], ['수인/버터', butter],
];
L.run18(JOBS, TUNE, new URL('./boost_수인A_18.json', import.meta.url));
