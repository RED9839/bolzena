// 18갈래 재설계 3단계 — 마녀A 묶음 7명(레비 · 롤렛 · 바리에 · 벨리타 · 비비(신성) · 셰럼 · 스노키). 2026-10-08
// 지침: _gen/rework/BRIEF_118.md · 기준: _measure/갈래_세분화/05_시범16_결과.md · 기록: _measure/갈래_세분화/06_118/마녀A.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(쓰기 · 갈래 부품 · 원작 자유 · 둘째) · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// 생성 카드 사도: 원래 만들던 바리에 · 벨리타 · 셰럼은 그대로 + 새로 롤렛 하나(원작 「모자에서 무엇이 나올지 모르는 물건을 꺼낸다」 — 턴 시작 비둘기 · 다 차면 피날레 상자).
// node _gen/rework/마녀A_18.mjs [사도 이름 일부]  → heroes/마녀/<파일>.json (백업 heroes_before_118_20261008 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, st, stk, spendAll, per, perTag, draw, make, ifStack,
  ALLY, STRONG, disc, nextAp, gauge, exile, atkRun, srch, pw, pas, token, cs, perCs,
  Or, bl, U, setCards, setOpener } = L;
const pick = id => ({ k: 'spend', id, pick: true });
const perEv = { k: 'perEvent' };
const handEnd = { k: 'when', on: 'handEnd' };
const onDiscard = { k: 'when', on: 'discard' };
const perG = p => ({ k: 'perGuarded', per: p });
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
const cardOf = (j, id) => j.cards.find(c => c.id === id);

// ════════════════════════════════════════════════════════════════════
// 1. 레비 — 아껴 두기형 · 딜러 · 우울. 힘을 평균치로 눌러 두고(AP 를 남기거나 보존 카드를 쥐고 마친 턴마다 괴력), 비장의 장도를 언제 뽑을지 고른다
// 원작: 저학년 님블 컷(단도 3회, 마지막이 가장 셈) · 고학년 레비드 더 레드(비장의 장도 · 범위) · 애착 아티팩트(그림자가 한 번 더) · 포셔의 포션으로 힘을 평균치로 · 휴가를 모르는 알바
// 레비(졸업)(성장 — 이번 전투 쌓은 패기가 문턱을 넘으면 논문이 판 내내 자람)과 가름: 이쪽은 「참기」 — keepAp · 보존 · 다음 턴 AP
// 다 차면: 옛 「참을 수 없는 힘」(넘치면 저절로 무작위 피해) → 다음 카드 +50%(겹은 남음)
// 시동: u4 장도 뽑기(2) → u1 농땡이(0 · 보존 — 쥐고 넘기면 keepAp)
// ════════════════════════════════════════════════════════════════════
function levi(j) {
  const H = '레비', K = '괴력';
  const h = j.heroes[0];
  h.blurb = '약화 포션으로 괴력을 눌러 두고 사는 인턴 마녀. 일을 미루고 힘을 아낀 턴마다 괴력이 차오르고, 비장의 장도를 뽑는 순간 한꺼번에 풀립니다.';
  h.keyword = { ...h.keyword, onMax: { empower: 'next', ratio: 0.5 } };
  delete h.keyword.rules;
  h.passives = [
    pas('몰래 쉬는 시간', 'keepAp', [{ k: 'stack', id: K, v: 1, ofEvent: 1 }]),
    pas('남이 일하는 사이', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 2 }),
  ];
  setOpener(j, H, 'u4', 'u1');
  setCards(j, [
    // 시동 — 농땡이(0, 보존): 괴력 · 드로우. 쥐고 넘기면 「몰래 쉬는 시간」 이 돈다(쓸까 쥘까)
    U(H, 1, '농땡이', 0, '스킬', [stk(K, 2), draw(1)], nm([
      Or([stk(K, 3), draw(1)], { tags: ['보존'] }),
      Or([stk(K, 2), draw(2)], { tags: ['보존'] }),
      Or([stk(K, 2), sh(1.3)], { tags: ['보존'] }),
      Or([stk(K, 3), srch({ type: '공격' })], { tags: ['보존'] }),
      Or([stk(K, 3), draw(2)], { tags: [] }),
    ]), null, ['보존']),
    // 원작 자유 — 님블 컷(저학년): 단도 2회 + 마지막 한 번 + 괴력
    U(H, 2, '님블 컷', 1, '공격', [dmg(0.35, E1, { hits: 2 }), dmg(0.57), stk(K, 1)], [
      'A', ['D', 'keepAp', [dmg(0.3, ER)], { limit: 1 }],
      Or([dmg(0.3, E1, { hits: 3 }), dmg(0.5), st('약화', 1)]),
      Or([dmg(0.3, E1, { hits: 2 }), dmg(0.5), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 둘째 — 쓰다 만 논문(강화): 공격력 · 괴력, 이 전투 동안 쉬는 턴마다 그림자가 대신 한 번(애착 아티팩트)
    U(H, 3, '쓰다 만 논문', 1, '강화', [atkRun(0.1), stk(K, 1), pw('keepAp', [dmg(0.45, ER)], { limit: 1 })], nm([
      Or([atkRun(0.1), stk(K, 2), pw('keepAp', [dmg(0.55, ER)], { limit: 1 })]),
      Or([stk(K, 1), pw('keepAp', [dmg(0.45, ER)], { limit: 1 })], { cost: 0 }),
      Or([atkRun(0.1), stk(K, 1), pw('keepAp', [stk(K, 1), sh(0.7)], { limit: 1 })]),
      Or([atkRun(0.1), srch({ type: '공격' }), pw('keepAp', [dmg(0.45, ER)], { limit: 1 })]),
      Or([atkRun(0.15), disc(1), pw('keepAp', [dmg(0.65, ER)], { limit: 1 })]),
    ]), bl('atkUp', 'cost', [stk(K, 1)])),
    // 쓰기 — 장도 뽑기(2, 분쇄): 괴력 1개당, 전부 소모
    U(H, 4, '장도 뽑기', 2, '공격', [dmg(1.41), per(K), dmg(0.32), spendAll(K)], [
      'A', 'B',
      Or([dmg(1.0, EA), per(K), dmg(0.22, EA), spendAll(K)], { tags: ['분쇄'] }),
      Or([dmg(1.25), per(K), dmg(0.28), pw('keepAp', [stk(K, 1)], { limit: 1 })], { tags: ['분쇄'], power: true }),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)]), ['분쇄']),
    // 갈래 부품 — 평균치 포션: 실드 · 괴력, 다음 턴 AP +1(오늘 힘을 아껴 내일 장도에)
    U(H, 5, '평균치 포션', 1, '스킬', [sh(0.7), stk(K, 1), nextAp(1)], [
      'A',
      Or([sh(0.7), stk(K, 2), nextAp(1)], { tags: ['보존'] }),
      Or([sh(0.7), nextAp(1), pw('keepAp', [stk(K, 1), sh(0.4)], { limit: 1 })], { power: true }),
      Or([sh(0.9), nextAp(1), srch({ type: '공격' })]),
      Or([stk(K, 2), nextAp(2), disc(1)]),
    ], bl('guard', 'draw', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 2. 롤렛 — 손 만들기형 · 딜러 · 광기. 모자에서 매 턴 비둘기를 꺼낸다 — 날리면(내면) 피해, 흘려 보내면(버리면) 갈채가 크게. 갈채가 다 차면 피날레 상자
// 원작: 저학년 비둘기 · 비둘기를 되살리는 마술 · 고학년 상자(끌려옴 · 기절) · 어사이드 무작위 변이 · 「봉봉하게」 · 판을 슬쩍 비트는 방관자
// 새 생성 카드 사도(묶음 몫 1): 패시브 「모자 속 비둘기」 턴 시작에 비둘기 1장 · 다 차면: 옛 「피날레」(저절로 광역) → 「봉봉한 피날레」 카드(onMax make)
// 시동: u1 비둘기 부활 마술 — 그대로
// ════════════════════════════════════════════════════════════════════
function rolet(j) {
  const H = '롤렛', K = '갈채', DOVE = '롤렛_dove', FIN = '롤렛_finale';
  const h = j.heroes[0];
  h.blurb = '판을 슬쩍 굴리는 트릭스터 마녀. 모자에서 매 턴 비둘기를 꺼내 날리거나 흘려 보내고, 박수가 다 차면 마지막 상자를 엽니다.';
  h.keyword = { ...h.keyword, onMax: { make: FIN, consume: true }, rules: [pas('관객의 환호', 'play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })] };
  h.passives = [
    pas('소매 속 트릭', 'discard', [stk(K, 1)], { limit: 3 }),
    pas('모자 속 비둘기', 'turnStart', [make(DOVE, 1)]),
  ];
  const tokens = [
    cardOf(j, DOVE),
    token(FIN, '봉봉한 피날레', H, '공격', [dmg(1.2, EA), st('둔화', 1, EA)], { blurb: '박수가 최고조에 이르면 모자에서 마지막 상자가 나옵니다' }),
  ];
  setCards(j, [
    // 시동 — 비둘기 부활 마술(0): 버리고 뽑기(흘려 보낸 카드가 갈채)
    U(H, 1, '비둘기 부활 마술', 0, '스킬', [disc(1), draw(2)], nm([
      Or([disc(1), draw(2), make(DOVE, 1)]),
      Or([disc(2), draw(3)]),
      Or([disc(1), draw(1), stk(K, 2)]),
      Or([disc(1), draw(2), srch()]),
      Or([disc(1), draw(2), gauge(15)]),
    ]), null),
    // 원작 자유 — 갈채를 먹는 엔터테이너(저학년 비둘기): 적 전체 + 갈채, 버려지면 갈채 2
    U(H, 2, '갈채를 먹는 엔터테이너', 1, '공격', [dmg(0.9, EA), stk(K, 1), onDiscard, stk(K, 2)], [
      'A', ['D', 'discard', [dmg(0.25, ER)], { limit: 2 }],
      Or([dmg(0.8, EA), make(DOVE, 1), onDiscard, stk(K, 2)]),
      Or([dmg(0.8, EA), srch(), onDiscard, stk(K, 2)]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 쓰기 — 트릭 카드: 갈채 1개당, 전부 소모 — 다 채워 피날레를 받을까, 지금 털까
    U(H, 3, '트릭 카드', 1, '공격', [dmg(0.8), per(K), dmg(0.35), spendAll(K)], [
      'A',
      Or([dmg(0.55, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      Or([dmg(0.8), perTag(DOVE), dmg(0.4), exile('hand', { all: true, tag: DOVE })]),
      ['D', 'discard', [stk(K, 1)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 둘째 — 관객의 갈채: 갈채 1개당 무작위 적(쓰지 않음 — 쥐는 쪽)
    U(H, 4, '관객의 갈채', 1, '공격', [dmg(0.6), per(K), dmg(0.22, ER)], [
      'A', 'B', ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.55), per(K), dmg(0.2, ER), srch()]),
      'Hd',
    ], bl('power', 'draw', [make(DOVE, 1)])),
    // 갈래 부품 — 투명 의자: 실드, 손의 비둘기 1장당 실드 더 + 갈채(비둘기를 쥘까 날릴까)
    U(H, 5, '투명 의자', 1, '스킬', [sh(0.8), perTag(DOVE), sh(0.3), stk(K, 1)], [
      'A',
      Or([sh(0.7), make(DOVE, 1), stk(K, 1)]),
      Or([sh(0.7), perTag(DOVE), sh(0.25), srch()]),
      ['D', 'make', [sh(0.65)], { limit: 2 }],
      'Hd',
    ], bl('guard', 'ap', [make(DOVE, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 3. 바리에 — 손 만들기형 · 서포터 · 우울. 카드를 낼 때마다 북카트가 차고, 셋이면 헌 책 두 권을 손에 — 책을 쥐면 실드, 펼치면 동료 카드
// 원작: 「책 찾는 재주」 · 저학년 북카트(공격력 높은 아군에게 보호막 · SP) · 고학년 「당일 반납해주세요오」(대출 버프) · 연체자를 몽둥이로 회수
// 다 차면: 옛 「서가 정리」(만들기 + 빌린 책 저절로) → onMax make 헌 책 2(원래 만들던 사도). 빌린 책은 「서가 정리」 패시브(책을 만들면)로 옮김
// 시동: u1 도서 대출(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function barie(j) {
  const H = '바리에', K = '북카트', BK = '빌린 책', BOOK = '바리에_book';
  const h = j.heroes[0];
  h.keyword = { ...h.keyword, onMax: { make: BOOK, n: 2, consume: true }, rules: [pas('서가 정리', 'make', [stk(BK, 1, STRONG)], { limit: 1 })] };
  h.keywords = h.keywords.map(k => (k.name === BK ? { ...k, per: [{ stat: 'dealt', v: 0.15 }] } : k));
  h.passives = [
    pas('북카트 순회', 'play', [stk(K, 1)], { limit: 3 }),
    pas('당일 반납', 'stackGone', [draw(1), sh(0.5)], { when: { id: BK, decay: true }, limit: 1 }),
  ];
  setCards(j, [
    // 시동 — 도서 대출(0): 고른 아군에게 빌린 책 2 + 다른 아군 카드 드로우
    U(H, 1, '도서 대출', 0, '스킬', [stk(BK, 2, ALLY), draw(1, { who: 'other' })], nm([
      Or([stk(BK, 3, ALLY), draw(1, { who: 'other' })]),
      Or([stk(BK, 2, ALLY), draw(2, { who: 'other' })]),
      Or([stk(BK, 2, ALLY), sh(1.4)]),
      Or([stk(BK, 2, ALLY), draw(1, { who: 'other', type: '공격' }), stk(K, 1)]),
      Or([stk(BK, 4, ALLY), draw(1, { who: 'other' }), disc(1)]),
    ]), null),
    // 원작 자유 — 책을 정리해주세요오(저학년 북카트): 실드 + 북카트 + 가장 센 아군에게 빌린 책
    U(H, 2, '책을 정리해주세요오', 1, '스킬', [sh(1.6), stk(K, 1), stk(BK, 1, STRONG)], [
      'A',
      Or([sh(1.2), stk(K, 1), make(BOOK, 1)]),
      ['D', 'make', [sh(0.6)], { limit: 1 }],
      Or([sh(1.6), stk(K, 1), srch()]),
      'Hn',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 쓰기 — 연체자 독촉: 북카트 1개당(방어 기반), 전부 소모 — 셋 채워 책을 받을까, 지금 독촉할까
    U(H, 3, '연체자 독촉', 1, '공격', [ddef(0.72), per(K), ddef(0.3), spendAll(K)], [
      'A', 'B',
      Or([ddef(0.5, EA), per(K), ddef(0.2, EA), spendAll(K)]),
      Or([ddef(0.7), perTag(BOOK), ddef(0.35), exile('hand', { all: true, tag: BOOK })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 사서의 서가 정리: 실드, 손의 헌 책 1장당 실드 더 + 빌린 책(책을 쥘까 펼칠까)
    U(H, 4, '사서의 서가 정리', 1, '스킬', [sh(0.7), perTag(BOOK), sh(0.35), stk(BK, 1, STRONG)], [
      'A', 'B',
      Or([sh(0.6), perTag(BOOK), sh(0.3), make(BOOK, 1)]),
      ['D', 'turnStart', [sh(0.3)]],
      Or([sh(0.6), perTag(BOOK), sh(0.3), srch()]),
    ], bl('guard', 'ap', [make(BOOK, 1)])),
    // 둘째 — 책 아령: 방어 기반 피해 + 북카트
    U(H, 5, '책 아령', 1, '공격', [ddef(0.8), stk(K, 1)], [
      'A', 'B', ['D', 'turnStart', [stk(K, 1)]],
      Or([ddef(0.75), stk(K, 1), draw(1, { who: 'other' })]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 벨리타 — 쌓고 고르기 · 딜러 · 광기. 몰래 쟁인 초콜릿을 먹어 당을 쌓고, 고른 만큼 털어 심판의 비를 앞당긴다 — 쥔 초콜릿은 차원 폭발의 무게
// 원작: 비밀 창고의 초콜릿 · 당쇼크로 쓰러짐 · 저학년 차원 폭발 · 고학년 크림슨 레인 10회 · 어사이드 적중마다 고학년이 빨라짐
// 다 차면: 옛 「당쇼크」(저절로 털고 게이지) → 다음 카드 +50%. 털기는 「여왕의 간식 시간」 이 고른 만큼(spend pick) · 털면 게이지(여왕의 당 충전)
// 시동: u2 비밀 간식 창고(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function belita(j) {
  const H = '벨리타', K = '당 수치', CHOCO = '벨리타_t1';
  const h = j.heroes[0];
  h.keyword = { ...h.keyword, per: [{ stat: 'dealt', v: 0.12 }], onMax: { empower: 'next', ratio: 0.5, consume: true }, rules: [pas('여왕의 당 충전', 'spend', [gauge(25)], { when: { id: K }, limit: 1 })] };
  h.passives = [
    pas('몰래 초콜릿', 'turnStart', [make(CHOCO, 1)]),
    pas('여왕의 심판', 'ult', [dmg(0.6, EA)]),
  ];
  setCards(j, [
    // 갈래 부품 — 디멘션 오브 위치: 적 전체, 손의 초콜릿 1장당 한 번 더(쥘까 먹을까)
    U(H, 1, '디멘션 오브 위치', 1, '공격', [dmg(1.01, EA), perTag(CHOCO), dmg(0.29, EA)], [
      'A', ['D', 'make', [dmg(0.36, EA)], { limit: 1 }],
      Or([dmg(0.9, EA), perTag(CHOCO), dmg(0.26, EA), srch()]),
      Or([dmg(0.7, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      'Hd',
    ], bl('power', 'cost', [make(CHOCO, 1)])),
    // 시동 — 비밀 간식 창고(0): 초콜릿 둘
    U(H, 2, '비밀 간식 창고', 0, '스킬', [make(CHOCO, 2)], nm([
      Or([make(CHOCO, 3)]),
      Or([make(CHOCO, 2), draw(1)]),
      Or([make(CHOCO, 2), sh(0.8)]),
      Or([make(CHOCO, 2), srch({ type: '공격' })]),
      Or([make(CHOCO, 3), stk(K, 1), nextAp(-1)]),
    ]), null),
    // 원작 자유 — 핏빛 소나기(크림슨 레인): 무작위 5회 + 초콜릿
    U(H, 3, '핏빛 소나기', 1, '공격', [dmg(0.36, ER, { hits: 5 }), make(CHOCO, 1)], [
      'A', 'B', ['D', 'ult', [dmg(0.6, EA)]],
      Or([dmg(0.36, ER, { hits: 5 }), make(CHOCO, 1), gauge(15)]),
      'Hd',
    ], bl('power', 'draw', [make(CHOCO, 1)])),
    // 쓰기 — 여왕의 간식 시간: 당을 고른 만큼 소모해 1개당 무작위 적(다 채워 다음 카드에 줄까, 털어 게이지를 당길까)
    U(H, 4, '여왕의 간식 시간', 1, '공격', [dmg(0.8), pick(K), perEv, dmg(0.3, ER)], [
      'A',
      Or([dmg(0.6, EA), pick(K), perEv, dmg(0.22, EA)]),
      Or([dmg(0.8), per(K), dmg(0.3, ER)]),
      Or([dmg(0.7), pick(K), pw('spend', [dmg(0.35, ER)], { when: { id: K }, limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 둘째 — 엘프깡 한 봉지(0): 실드 + 당 + 자신의 공격 카드 찾기
    U(H, 5, '엘프깡 한 봉지', 0, '스킬', [sh(0.73), stk(K, 1), srch({ type: '공격' })], nm([
      Or([sh(0.95), stk(K, 1), srch({ type: '공격' })]),
      Or([sh(0.6), stk(K, 1), make(CHOCO, 1)]),
      Or([sh(0.8), stk(K, 2), gauge(15)]),
      Or([sh(0.5), stk(K, 2), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([sh(1.3), stk(K, 3), disc(1)]),
    ]), bl('guard', 'draw', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 비비(신성) — 성장형 · 딜러 · 공명 · 엘다인. 새싹을 내지 않고 품어 가꾸면 「잎」 이 판 내내 자란다 — 새싹이 다 차면 세계수가 파티의 다음 카드를 굽어살핀다
// 원작: 영원의 새싹(새 세계수 · 반쪽 힘이지만 차근차근 자람) · 애착 아티팩트 「사랑받은 새싹」(시들지 않게 가꾼다) · 고학년 신성 상태(아군 피해↑) · 우이와 연못가 대화
// 다 차면: 옛 「개화」(저절로 광역) → 파티의 다음 카드 +50%(딜포터). 엘다인 한 단계: 카드가 자라면(잎) 새싹 +1(가꾸는 손)
// 시동: u3 손잡기(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function bibiHoly(j) {
  const H = '비비_신성', K = '새싹', LEAF = '잎';
  const h = j.heroes[0];
  h.blurb = '새 세계수가 된 비비. 새싹 카드를 내지 않고 품어 가꾼 턴마다 잎이 자라고, 새싹이 다 차면 그 빛이 파티의 다음 한 수를 굽어살핍니다.';
  h.keyword = { ...h.keyword, onMax: { empower: 'any', ratio: 0.4, consume: true }, rules: [pas('가꾸는 손', 'grow', [stk(K, 1)], { limit: 1 })] };
  h.passives = [
    pas('함께 걷는 세상', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }),
    pas('이른 봄의 기운', 'lowHp', [{ k: 'cleanse', v: 1 }, st('피해 감소', 1)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // 갈래 부품 — 셋의 몫(보존): 피해, 「잎」 1당 한 번 더. 턴 끝에 손에 있으면 이 카드 「잎」 +1(판 내내, 최대 5) — 낼까 품을까
    U(H, 1, '셋의 몫', 1, '공격', [dmg(0.5), perCs(LEAF, { n: 1 }), dmg(0.2), handEnd, cs(LEAF, 1, { max: 5 })], [
      'A',
      Or([dmg(0.45, EA), perCs(LEAF, { n: 1 }), dmg(0.15, EA), handEnd, cs(LEAF, 1, { max: 5 })], { tags: ['보존'] }),
      Or([perCs(LEAF, { n: 1 }), dmg(0.3), handEnd, cs(LEAF, 1, { max: 5 }), stk(K, 1)], { tags: ['보존'] }),
      Or([perCs(LEAF, { n: 1 }), dmg(0.32), handEnd, cs(LEAF, 1, { max: 5 }), sh(0.4)], { tags: ['보존'] }),
      ['Ht', '보존'],
    ], bl('power', 'draw', [stk(K, 1)]), ['보존']),
    // 쓰기 — 모두의 빛(2): 적 전체, 새싹 1개당, 전부 소모 — 여섯 채워 파티에 줄까, 지금 쏟을까
    U(H, 2, '모두의 빛', 2, '공격', [dmg(0.5, EA), per(K), dmg(0.18, EA), spendAll(K)], [
      'A', 'B',
      Or([dmg(0.9), per(K), dmg(0.32), spendAll(K)]),
      Or([dmg(0.45, EA), per(K), dmg(0.16, EA), pw('turnStart', [stk(K, 1)])], { power: true }),
      'Hd',
    ], bl('power', 'ap', [stk(K, 1)])),
    // 시동 — 손잡기(0): 새싹 + 다른 아군 카드 드로우
    U(H, 3, '손잡기', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], nm([
      Or([stk(K, 2), draw(1, { who: 'other' })]),
      Or([stk(K, 1), draw(2, { who: 'other' })]),
      Or([stk(K, 1), sh(1.2)]),
      Or([stk(K, 1), draw(1, { who: 'other', type: '공격' })]),
      Or([stk(K, 3), draw(1, { who: 'other' }), disc(1)]),
    ]), null),
    // 둘째 — 새 세계수의 서약(강화): 새싹 + 매 턴 새싹
    U(H, 4, '새 세계수의 서약', 1, '강화', [stk(K, 1), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), pw('turnStart', [stk(K, 1)])]),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0 }),
      Or([stk(K, 1), pw('grow', [stk(K, 1), dmg(0.3, EA)], { limit: 1 })]),
      Or([stk(K, 1), srch(), pw('turnStart', [stk(K, 1)])]),
      Or([stk(K, 2), disc(1), pw('turnStart', [stk(K, 1), sh(0.4)])]),
    ]), bl('atkUp', 'cost', [stk(K, 1)])),
    // 원작 자유 — 연못가 대화(0): 새싹 + 실드 + 자신의 고유 카드 찾기(우이와 연못가 대화)
    U(H, 5, '연못가 대화', 0, '스킬', [stk(K, 1), sh(0.4), srch()], nm([
      Or([stk(K, 2), sh(0.5), srch()]),
      Or([stk(K, 2), sh(0.5), draw(1, { who: 'other', type: '공격' })]),
      Or([stk(K, 1), sh(0.3), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 2), sh(0.4), handEnd, stk(K, 1)], { tags: ['보존'] }),
      Or([stk(K, 2), sh(1.2), disc(1)]),
    ]), bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 셰럼 — 손 만들기형 · 탱커 · 순수. 맞은 장면을 받아 적어 원고지가 차면 원고 속 인물이 걸어 나온다 — 원고를 쥘까 낭독할까
// 원작: 궁정 서기관 · 쓴 것이 걸어 나옴(벨라 · 잉클) · 저학년 전승의 커튼(받는 피해 감소) · 고학년 회한의 영역 · 강화 평타는 피격 4번마다
// 다 차면: 원고지 넷이면 「어둠 정령의 상처」(옛 stackReach 규칙 → onMax make — 원래 만들던 사도). 기사 · 공주는 신탁 · 축복이 지어 낸다
// 시동: u1 위치 아카이브 — 그대로
// ════════════════════════════════════════════════════════════════════
function sherum(j) {
  const H = '셰럼', K = '원고지', T1 = '셰럼_t1', T2 = '셰럼_t2', T3 = '셰럼_t3';
  const h = j.heroes[0];
  h.keyword = { ...h.keyword, onMax: { make: T1, consume: true } };
  delete h.keyword.rules;
  setCards(j, [
    // 시동 — 위치 아카이브: 실드 + 원고지 2
    U(H, 1, '위치 아카이브', 1, '스킬', [sh(0.8), stk(K, 2)], nm([
      Or([sh(1.0), stk(K, 2)]),
      Or([sh(0.6), stk(K, 1)], { cost: 0 }),
      Or([sh(0.7), stk(K, 2), make(T3, 1)]),
      Or([sh(0.7), stk(K, 2), srch()]),
      Or([sh(1.2), stk(K, 3), disc(1)]),
    ]), null),
    // 원작 자유 — 전승의 커튼(저학년): 실드 + 피해 감소 + 원고지
    U(H, 2, '전승의 커튼', 1, '스킬', [sh(1.0), st('피해 감소', 2), stk(K, 1)], [
      'A', 'B',
      ['D', 'hurt', [stk(K, 1)], { limit: 1 }],
      Or([sh(0.8), st('피해 감소', 1), make(T2, 1)]),
      'Hn',
    ], bl('guard', 'ap', [make(T2, 1)])),
    // 쓰기 — 흑역사 낭독: 적 전체(방어 기반), 원고지 1개당, 전부 소모 — 넷 채워 원고를 받을까, 지금 낭독할까
    U(H, 3, '흑역사 낭독', 1, '공격', [ddef(0.3, EA), per(K), ddef(0.15, EA), spendAll(K)], [
      'A', 'B',
      Or([ddef(0.55), per(K), ddef(0.28), spendAll(K)]),
      Or([ddef(0.3, EA), perTag(T1), ddef(0.3, EA), exile('hand', { all: true, tag: T1 })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 둘째 — 서기관의 기록체: 원고지 1개당(쓰지 않음 — 쥐는 쪽)
    U(H, 4, '서기관의 기록체', 1, '공격', [ddef(0.6), per(K), ddef(0.22)], [
      'A', ['D', 'make', [stk(K, 1)], { limit: 1 }],
      Or([ddef(0.55), per(K), ddef(0.2), make(T3, 1)]),
      Or([ddef(0.55), per(K), ddef(0.2), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 미행 기록(0): 방어 기반 피해 + 원고지(받아 적기)
    U(H, 5, '미행 기록', 0, '공격', [ddef(0.35), stk(K, 1)], [
      'A', ['D', 'hurt', [sh(0.2)], { limit: 2 }],
      Or([ddef(0.3), make(T2, 1)]),
      Or([ddef(0.3), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 스노키 — 버티형 · 탱커 · 우울. 두유 막을 씌워 막아 낸 만큼 밀두유를 거두고(변환), 막이 깨지면 그 적을 약하게 — 셋이면 파티의 다음 카드에 의리
// 원작: 저학년 불법 두유(아군 실드 · 깨지면 주변 적 받는 피해↑) · 고학년 구역 점거(광역 · 넉백) · 어사이드 의리의 대명사(실드) · 맨주먹
// 버티 변환: 지난 판에 막아 낸 양이 있으면 밀두유 +1(guardSum) · 「불법 유통망」 막아 낸 양 50당 한 번 더(perGuarded)
// 다 차면: 옛 「세 병째」(저절로 실드 + 취약) → 파티의 다음 카드 +40%
// 시동: u1 경호원 출동(개전 강화) — 그대로
// ════════════════════════════════════════════════════════════════════
function snowkey(j) {
  const H = '스노키', K = '밀두유';
  const h = j.heroes[0];
  h.keyword = { ...h.keyword, onMax: { empower: 'any', ratio: 0.5, consume: true }, rules: [pas('구역 수금', 'guardSum', [{ k: 'stack', id: K, v: 1, ofEvent: 0.015 }], { when: { n: 30 } })] };
  h.passives = [
    pas('두유 배달', 'play', [sh(0.2), stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 2 }),
    pas('의리의 대명사', 'shieldBreak', [st('취약', 1, E1)], { limit: 2 }),
  ];
  setCards(j, [
    // 시동 — 경호원 출동(개전 강화): 밀두유 + 실드 + 매 턴 밀두유
    U(H, 1, '경호원 출동', 1, '강화', [stk(K, 1), sh(0.48), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), sh(0.6), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 1), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 1), sh(0.6), pw('turnStart', [stk(K, 1), sh(0.25)])], { tags: ['개전'] }),
      Or([stk(K, 2), srch(), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), sh(1.0), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 원작 자유 — 불법 두유(저학년): 실드 + 밀두유
    U(H, 2, '불법 두유', 1, '스킬', [sh(0.96), stk(K, 1)], [
      'A', 'B', ['D', 'guardSum', [sh(0.3)], { limit: 1 }],
      Or([sh(0.85), stk(K, 1), srch()]),
      'Hn',
    ], bl('guard', 'draw', [stk(K, 1)])),
    // 쓰기 — 두유 상자 던지기: 적 전체(방어 기반), 밀두유 1개당, 전부 소모
    U(H, 3, '두유 상자 던지기', 1, '공격', [ddef(0.28, EA), per(K), ddef(0.12, EA), spendAll(K)], [
      'A', 'B',
      Or([ddef(0.5), per(K), ddef(0.22), spendAll(K)]),
      Or([ddef(0.3, EA), perG(30), ddef(0.15, EA), spendAll(K)]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 불법 유통망: 방어 기반, 막아 낸 양 50당 한 번 더 + 밀두유(버틴 만큼 친다)
    U(H, 4, '불법 유통망', 1, '공격', [ddef(0.45), perG(50), ddef(0.15), stk(K, 1)], [
      'A',
      Or([ddef(0.35, EA), perG(50), ddef(0.1, EA), stk(K, 1)]),
      ['D', 'guardSum', [ddef(0.3)], { limit: 1 }],
      Or([ddef(0.4), perG(50), ddef(0.14), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 둘째 — 명절 선물 세트(0): 실드 + 밀두유 + 다른 아군 공격 카드 드로우
    U(H, 5, '명절 선물 세트', 0, '스킬', [sh(0.32), stk(K, 1), draw(1, { who: 'other', type: '공격' })], nm([
      Or([sh(0.55), stk(K, 1), draw(1, { who: 'other', type: '공격' })]),
      Or([sh(0.3), stk(K, 2), draw(1, { who: 'other' })]),
      Or([sh(0.6), stk(K, 2), draw(1, { who: 'other' })], { tags: ['보존'] }),
      Or([sh(0.45), draw(1, { who: 'other', type: '공격' }), pw('guardSum', [stk(K, 1), sh(0.3)], { limit: 1 })], { power: true }),
      Or([sh(0.5), stk(K, 3), disc(1)]),
    ]), bl('draw', { tags: ['신속'] }, [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '마녀/레비': 1.4, '마녀/롤렛': 0.8, '마녀/바리에': 1.3, '마녀/벨리타': 1.25, '마녀/비비_신성': 0.85, '마녀/셰럼': 0.9, '마녀/스노키': 1.15 };

// ── 돌리기 ──
const JOBS = [
  ['마녀/레비', levi], ['마녀/롤렛', rolet], ['마녀/바리에', barie], ['마녀/벨리타', belita],
  ['마녀/비비_신성', bibiHoly], ['마녀/셰럼', sherum], ['마녀/스노키', snowkey],
];
L.run18(JOBS, TUNE, new URL('./boost_마녀A_18.json', import.meta.url));
