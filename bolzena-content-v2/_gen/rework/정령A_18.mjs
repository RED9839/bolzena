// 18갈래 재설계 3단계 — 정령A 묶음 8명(2026-10-08). 기준: 시범 17명(05_시범16_결과.md) · 지침 BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/정령A.md
// 가비아 · 나이아 · 라이카 · 멜루나 · 뮤트 · 미로 · 블랑셰 · 빅우드
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장 · 신탁 5갈래 · 축복 12(시동 카드는 공용 축복 풀). 다 차면 저절로 터짐 없음(onMax make/empower).
// 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// 생성 카드 사도: 원래 만들던 블랑셰(커튼콜) · 빅우드(황금 사과)는 그대로 + 새로 가비아 하나(원작 「99번 참았다」 · 땅울림 — 다 차면 「땅울림」 카드).
// node _gen/rework/정령A_18.mjs [사도 이름 일부]  → heroes/정령/<파일>.json (원본은 백업 SRC 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill,
  TOP, ALLY, nextAp, disc, gauge, srch, drawType, pull, xtra, pw, pas, token, Or, bl, U, setCards,
  exile, cs, spendN, tough, perOver, empower } = L;
const nm = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
const pick = id => ({ k: 'spend', id, pick: true });
const perEv = { k: 'perEvent' };
const payPct = v => ({ k: 'payHpPct', v });
const perPaid = p => ({ k: 'perPaid', per: p });
const ifSpent = n => ({ k: 'ifSpent', n });
const feed = (v, target) => ({ k: 'feed', v, target });
const cleanse = v => ({ k: 'cleanse', v });
const crit = v => ({ k: 'critMod', v, run: true });
const stkEv = (id, of) => ({ k: 'stack', id, v: 1, ofEvent: of });
const cardOf = (j, id) => j.cards.find(c => c.id === id);

// ════════════════════════════════════════════════════════════════════
// 1. 가비아 — 아껴 두기형 · 서포터 · 순수 · 2성. 소리를 키우면 땅이 울려 일부러 작게 산다 — 일을 미루고(AP · 보존 카드를 남긴 채) 턴을 마칠 때마다 고함을 삼키고,
//    다섯이 차면 손에 「땅울림」(보존)이 맺힌다. 언제 터뜨릴지는 고른다(쥐고 있어도 아껴 두기 몫이 된다)
// 원작: 「99번 참았다, 한 번만 더면 지하로」 · 딸꾹질이 넘치면 천재지변 · 저학년 돌려⋯줄⋯게⋯(보호막 · 흡수 뒤 반사) · 고학년 지켜⋯줄⋯게⋯ · 「300년쯤 조용히」 · 조각 취미
// 다 차면: 옛 「땅울림」(저절로 적 전체 방어 기반) → 「땅울림」 카드(onMax make — 새 생성 카드 사도). 아껴 두기: keepAp 의 값(남긴 AP + 쥔 보존 수)만큼 고함(마녀A 레비 모양)
// 시동: u1 작은 목소리(0 · 보존) — 그대로
// ════════════════════════════════════════════════════════════════════
function gabia(j) {
  const H = '가비아', K = '삼킨 고함', ST = '가비아_statue', QK = '가비아_quake';
  const h = j.heroes[0];
  h.blurb = '목소리를 키우면 땅이 울리는 대지의 정령. 할 일을 미루고 AP 나 쥔 카드를 남긴 채 턴을 마칠 때마다 고함을 꾹 삼키고, 다섯이 차면 손에 땅울림이 맺힙니다 — 언제 터뜨릴지는 가비아가 고릅니다.';
  h.keyword = { ...h.keyword, onMax: { make: QK, consume: true } };
  delete h.keyword.rules;
  h.passives = [
    pas('조용한 대지', 'keepAp', [stkEv(K, 1), sh(0.3)]),
    pas('우웅⋯', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
  ];
  const statue = cardOf(j, ST);
  const tokens = [
    { ...statue, tags: ['보존', '소멸'] },
    token(QK, '땅울림', H, '공격', [ddef(0.55, EA), tough(1, EA)], { tags: ['보존', '소멸'], blurb: '아흔아홉 번 참은 고함이 마침내 땅을 울립니다' }),
  ];
  setCards(j, [
    // 시동 — 작은 목소리(0, 보존): 고함 · 드로우. 쥐고 넘기면 「조용한 대지」 가 돈다(쓸까 쥘까)
    U(H, 1, '작은 목소리', 0, '스킬', [stk(K, 1), draw(1)], nm([
      Or([stk(K, 2), draw(1)], { tags: ['보존'] }),
      Or([stk(K, 1), draw(2)], { tags: ['보존'] }),
      Or([stk(K, 1), draw(1), pw('keepAp', [stk(K, 1)], { limit: 1 })], { tags: ['보존'], power: true }),
      Or([stk(K, 1), srch()], { tags: ['보존'] }),
      Or([stk(K, 3), draw(1)], { tags: [] }),
    ]), null, ['보존']),
    // 쓰기 — 그만해!!!: 적 전체(방어 기반), 고함 1개당, 전부 소모 — 다섯 채워 땅울림을 받을까, 지금 쏟을까
    U(H, 2, '그만해!!!', 1, '공격', [ddef(0.4, EA), per(K), ddef(0.12, EA), spendAll(K)], [
      'A',
      Or([ddef(0.7), per(K), ddef(0.2), spendAll(K)]),
      Or([ddef(0.35, EA), per(K), ddef(0.1, EA), pw('keepAp', [stk(K, 1)], { limit: 1 })], { power: true }),
      Or([ddef(0.4, EA), perTag(ST), ddef(0.3, EA), exile('hand', { all: true, tag: ST })]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 원작 자유 — 돌려⋯줄⋯게⋯(저학년): 실드 + 반격 + 고함
    U(H, 3, '돌려⋯줄⋯게⋯', 1, '스킬', [sh(1.3), st('반격', 1), stk(K, 1)], [
      'A', 'B',
      ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      Or([sh(1.2), st('반격', 1), srch()]),
      'Hn',
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 지하에 누워: 결정화 · 고함, 다음 턴 AP +1(오늘 참아 내일로)
    U(H, 4, '지하에 누워', 1, '스킬', [st('결정화', 1), stk(K, 1), nextAp(1)], nm([
      Or([st('결정화', 1), stk(K, 2), nextAp(1)]),
      Or([st('결정화', 1), nextAp(1)], { cost: 0 }),
      Or([st('결정화', 1), nextAp(1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([st('결정화', 1), stk(K, 1), make(ST, 2)]),
      Or([stk(K, 2), nextAp(2), disc(1)]),
    ]), bl('defUp', 'draw', [stk(K, 1)])),
    // 둘째 — 위층 쿵쿵 금지: 방어 기반 피해 + 고함 + 흙 조각상(보존 — 쥐면 아껴 두기 몫)
    U(H, 5, '위층 쿵쿵 금지', 1, '공격', [ddef(0.78), stk(K, 1), make(ST, 1)], [
      'A',
      ['D', 'turnEnd', [make(ST, 1)]],
      Or([ddef(0.69, EA), stk(K, 1), make(ST, 1)]),
      Or([ddef(0.8), make(ST, 1), srch()]),
      Or([ddef(1.7), stk(K, 2), disc(1)]),
    ], bl('power', 'weakSpot', [make(ST, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 2. 나이아 — 쌓고 고르기 · 서포터 · 순수. 스킬을 낼 때마다 물탱크가 차고(아군 주는 피해↑ — 쥘 이유), 물총에 몇 발을 실을지 고른다(spend pick).
//    가득 차면 넘친 물이 파티의 다음 한 수에(onMax empower any). 친구 주머니도 채운다(feed)
// 원작: 저학년 「HP 낮은 아군」 20번 씻기기 → 회복 + 해제 · 고학년 파도(아군 회복 · 적 피해) · 강화 평타 물총 세 발 · 어사이드 2성 고학년 두 번 · 외로움 · 결벽증 호수 대청소
// 다 차면: 옛 「호수 대청소」(저절로 해제 + 회복) → 파티의 다음 카드 +40%(겹 다 씀). 고학년 뒤 게이지(뽀득뽀득)는 고유 효과 규칙으로
// 시동: u1 같이 놀자!(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function naia(j) {
  const H = '나이아', K = '물탱크';
  const h = j.heroes[0];
  h.blurb = '인사 대신 물총부터 쏘는 외로운 수문장. 스킬을 낼 때마다 물탱크가 차올라 친구들이 힘을 내고, 물총에 몇 발을 실을지는 나이아가 고릅니다 — 가득 채우면 넘친 물이 파티의 다음 한 수에 실립니다.';
  h.keyword = { ...h.keyword, per: [{ stat: 'dealt', v: 0.07, who: 'allies' }], onMax: { empower: 'any', ratio: 0.5, consume: true }, rules: [pas('뽀득뽀득 씻어요', 'ult', [gauge(30)])] };
  h.passives = [
    pas('퓨퓨~', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 3 }),
    pas('물장난 상대', 'spend', [stk(K, 1)], { when: { who: 'other' }, limit: 2 }),
  ];
  setCards(j, [
    // 시동 — 같이 놀자!(0): 물탱크 + 고른 아군 주머니 +1 + 다른 아군 카드
    U(H, 1, '같이 놀자!', 0, '스킬', [stk(K, 1), feed(1, ALLY), draw(1, { who: 'other' })], nm([
      Or([stk(K, 2), feed(1, ALLY), draw(1, { who: 'other' })]),
      Or([stk(K, 1), feed(1, ALLY), draw(2, { who: 'other' })]),
      Or([stk(K, 2), feed(2, ALLY), draw(1, { who: 'other' })]),
      Or([stk(K, 1), draw(1, { who: 'other' }), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([stk(K, 3), draw(1, { who: 'other' }), disc(1)]),
    ]), null),
    // 원작 자유 — 그게 씻은거야?(저학년 씻기기): 회복 + 해제 + 물탱크
    U(H, 2, '그게 씻은거야?', 1, '스킬', [heal(1.0), cleanse(1), stk(K, 1)], [
      'A', 'B',
      Or([heal(1.2), cleanse(1), pw('spend', [heal(0.5)], { when: { who: 'any' }, limit: 2 })], { power: true }),
      Or([heal(0.9), cleanse(1), srch()]),
      'Hd',
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 쓰기 — 물대포: 2연사 + 물탱크를 고른 만큼 소모, 1개당 한 발
    U(H, 3, '물대포', 1, '공격', [dmg(0.3, E1, { hits: 2 }), pick(K), perEv, dmg(0.32)], [
      'A',
      Or([dmg(0.25, EA, { hits: 2 }), pick(K), perEv, dmg(0.22, EA)]),
      Or([dmg(0.3, E1, { hits: 2 }), per(K), dmg(0.25)]),
      ['D', 'spend', [dmg(0.3, ER)], { when: { id: K }, limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 둘째 — 외로운 수문장(강화): 사기 + 매 턴 물탱크
    U(H, 4, '외로운 수문장', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1)])], nm([
      Or([st('사기', 1), stk(K, 2), pw('turnStart', [stk(K, 1)])]),
      Or([st('사기', 1), pw('turnStart', [stk(K, 1)])], { cost: 0 }),
      Or([st('사기', 1), stk(K, 2), pw('spend', [heal(0.4)], { when: { who: 'any' }, limit: 2 })]),
      Or([st('사기', 1), srch(), pw('turnStart', [stk(K, 1)])]),
      Or([st('사기', 1), disc(1), pw('turnStart', [stk(K, 2)])]),
    ]), bl('atkUp', 'ap', [stk(K, 1)])),
    // 갈래 부품 — 인사 대신 물총(강화 평타 세 발): 3연사 + 물탱크 + 회복
    U(H, 5, '인사 대신 물총', 1, '공격', [dmg(0.3, E1, { hits: 3 }), stk(K, 1), heal(0.3)], [
      'A', 'B',
      Or([dmg(0.28, ER, { hits: 3 }), stk(K, 1), srch()]),
      ['D', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 라이카 — 대가형 · 딜러 · 순수. 파티 HP 를 치러 제 몸에 전류를 꽂으면(대가 — 계기 pay) 「감전」 — 감전된 턴엔 공격마다 한 번 더 질주.
//    과충전(1개당 주는 피해 +20%)이 꽉 차면 다음 주먹 +50%. 원격 충전(고학년)으로만 최대 출력
// 원작: 저학년 스스로 감전(손해) · 강화 평타 질주는 감전이면 피해 +200% · 고학년 최대 출력 · 어사이드 「과출력 → 방전 → 입원」 · 220V 콘센트 · 전기 지짐이 원조
// 다 차면: 옛 「번개 펀치」(저절로 5연타) · onMax 최대 출력(변신) → 다음 카드 +50%(겹은 남음). 최대 출력은 고학년만
// 시동: u1 급속 충전!(개전 강화) — 그대로(매 턴 HP 를 조금 치르고 충전)
// ════════════════════════════════════════════════════════════════════
function laika(j) {
  const H = '라이카', K = '과충전', SH = '감전';
  const h = j.heroes[0];
  h.blurb = '220V 콘센트를 사랑하는 번개 정령. 파티 HP 를 치러 제 몸에 전류를 꽂으면 감전 — 감전된 주먹은 한 번 더 내달리고, 과충전이 꽉 차면 다음 주먹이 무거워집니다. 원격 충전을 터뜨리면 최대 출력에 들어갑니다.';
  h.keyword = { ...h.keyword, onMax: { empower: 'next', ratio: 0.5 } };
  h.keywords = [{ name: SH, desc: '일부러 제 몸에 흘린 220V', carrier: 'self', cap: 1, endClear: true }];
  h.passives = [
    pas('220V 콘센트', 'pay', [stk(SH, 1), stk(K, 1)], { limit: 2 }),
    pas('220V 콘센트', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }),
    pas('찌릿 질주', 'play', [xtra(0.35)], { when: { type: '공격' }, conds: [{ c: 'stack', id: SH, n: 1 }], limit: 2 }),
  ];
  setCards(j, [
    // 시동 — 급속 충전!(개전 강화): HP 를 치러 과충전 2, 매 턴 시작 HP 를 조금 치르고 과충전(→ 감전)
    U(H, 1, '급속 충전!', 1, '강화', [payPct(0.02), stk(K, 2), pw('turnStart', [payPct(0.01), stk(K, 1)])], nm([
      Or([payPct(0.02), stk(K, 3), pw('turnStart', [payPct(0.01), stk(K, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [payPct(0.01), stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([payPct(0.02), stk(K, 2), pw('turnStart', [payPct(0.01), stk(K, 1), sh(0.3)])], { tags: ['개전'] }),
      Or([payPct(0.02), stk(K, 2), pw('turnStart', [payPct(0.01), stk(K, 1), draw(1)])], { tags: ['개전'] }),
      Or([payPct(0.03), stk(K, 3), pw('turnStart', [payPct(0.01), stk(K, 1), dmg(0.42, ER)])], { tags: [] }),
    ]), null, ['개전']),
    // 쓰기 — 감전 질주: 적 전체, 과충전 1개당, 전부 소모
    U(H, 2, '감전 질주', 1, '공격', [dmg(0.6, EA), per(K), dmg(0.15, EA), spendAll(K)], [
      'A', 'B',
      Or([dmg(0.9), per(K), dmg(0.3), spendAll(K)]),
      ['D', 'pay', [dmg(0.3, EA)], { limit: 1 }],
      'Hn',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 원작 자유 — 220V 스트레이트(강화 평타 질주): 2연타 + 충격
    U(H, 3, '220V 스트레이트', 1, '공격', [dmg(0.55, E1, { hits: 2 }), st('충격', 1, E1)], [
      'A', 'B',
      ['D', 'pay', [st('충격', 1)], { limit: 1 }],
      Or([dmg(0.5, E1, { hits: 2 }), st('충격', 1, E1), srch({ type: '공격' })]),
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
    // 둘째 — 풀 스윙: 과충전 1개당(쓰지 않음 — 쥐는 쪽)
    U(H, 4, '풀 스윙', 1, '공격', [dmg(0.6), per(K), dmg(0.3)], [
      'A',
      Or([dmg(0.8), perPaid(30), dmg(0.45)]),
      ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.55), per(K), dmg(0.27), srch()]),
      'Hd',
    ], bl('atkUp', 'draw', [stk(K, 1)])),
    // 갈래 부품 — 전기 지짐이(0): HP 를 치러 감전 · 과충전, 버린 공격 카드 회수
    U(H, 5, '전기 지짐이', 0, '스킬', [payPct(0.03), stk(K, 1), pull({ type: '공격' })], nm([
      Or([payPct(0.03), stk(K, 2), pull({ type: '공격' })]),
      Or([payPct(0.03), stk(K, 1), drawType('공격', 2)]),
      Or([payPct(0.02), pull({ type: '공격' }), pw('pay', [stk(K, 1), dmg(0.2, ER)], { limit: 1 })], { power: true }),
      Or([payPct(0.03), stk(K, 1), pull({ type: '공격', n: 2 })]),
      Or([payPct(0.06), stk(K, 3), pull({ type: '공격' })]),
    ]), bl('draw', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 멜루나 — 돈형 · 서포터 · 냉정. AP 를 남기고(이자) · 적을 쓰러뜨려(플렉스) 주가를 올리고(벌이 — 쥐면 아군 주는 피해↑),
//    주가를 털어 머스크멜론을 사 들인다(사기 — 손에 카드). 멜론을 쥘수록 멜론머스크 그룹이 커진다
// 원작: 저학년 멜론비(준 피해만큼 아군 회복) · 어사이드 멜론 플렉스(코인) · 펫 멜론 「머스크」 · 「팔기 전엔 손해가 아니다」 · 장기 투자 · 능구렁이형 사업가
// 판 골드는 건드리지 않는다(BRIEF 돈형 규칙) — 전투 안 주가만. 사기는 카드(광합성)가 한다(장치가 만들지 않음 — 생성 카드 사도 셈 그대로).
// 다 차면: 옛 「상장」(저절로 AP · 드로우) → 파티의 다음 카드 +40%(겹은 남음)
// 시동: u1 장기 투자(개전 강화) — 그대로
// ════════════════════════════════════════════════════════════════════
function meluna(j) {
  const H = '멜루나', K = '주가', ML = '멜루나_melon';
  const h = j.heroes[0];
  h.blurb = '법의 빈틈으로 회사를 굴리는 멜론 회장. 남긴 AP 와 처치로 주가를 올려 두면 파티가 덩달아 힘을 내고, 주가를 털어 머스크멜론을 사 들이면 — 손에 쥔 멜론만큼 멜론머스크 그룹이 커집니다.';
  h.keyword = { ...h.keyword, cap: 5, per: [{ stat: 'dealt', v: 0.06, who: 'allies' }], onMax: { empower: 'any', ratio: 0.4 } };
  delete h.keyword.rules;
  h.passives = [
    pas('이자 수익', 'turnEnd', [stk(K, 1)], { conds: [{ c: 'apLeft', n: 1 }] }),
    pas('멜론 플렉스', 'kill', [stk(K, 2)], { limit: 1 }),
    pas('멜론 플렉스', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }),
  ];
  const melon = cardOf(j, ML);
  const tokens = [{ ...melon, fx: [heal(0.3), st('협공', 1)] }];
  setCards(j, [
    // 시동 — 장기 투자(개전 강화): 주가 + 저장 + 매 턴 주가
    U(H, 1, '장기 투자', 1, '강화', [stk(K, 1), st('저장', 1), pw('turnStart', [stk(K, 1)])], nm([
      Or([stk(K, 2), st('저장', 1), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([stk(K, 2), st('저장', 1), pw('kill', [make(ML, 1), stk(K, 1)], { limit: 1 })], { tags: ['개전'] }),
      Or([stk(K, 2), srch(), pw('turnStart', [stk(K, 1), heal(0.15)])], { tags: ['개전'] }),
      Or([stk(K, 3), st('저장', 1), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 원작 자유 — 올 때 멜루나~(저학년 멜론비): 무작위 5회 + 준 피해 흡수 + 머스크멜론
    U(H, 2, '올 때 멜루나~', 1, '공격', [dmg(0.3, ER, { hits: 5 }), drain(0.5), make(ML, 1)], [
      'A', 'B',
      Or([dmg(0.3, ER, { hits: 5 }), drain(0.5), pw('turnStart', [make(ML, 1)])], { power: true }),
      Or([dmg(0.36, ER, { hits: 5 }), drain(0.5), stk(K, 2)]),
      'Hd',
    ], bl('power', 'heal', [stk(K, 1)])),
    // 쓰기 — 멜론 씨 기관총: 3연사 + 주가 1개당, 전부 소모(팔아 치우기)
    U(H, 3, '멜론 씨 기관총', 1, '공격', [dmg(0.3, E1, { hits: 3 }), per(K), dmg(0.3), spendAll(K)], [
      'A',
      Or([dmg(0.25, EA, { hits: 3 }), per(K), dmg(0.2, EA), spendAll(K)]),
      ['D', 'turnEnd', [stk(K, 1)], { conds: [{ c: 'apLeft', n: 1 }] }],
      Or([dmg(0.3, E1, { hits: 3 }), per(K), dmg(0.26), srch()]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 둘째 — 멜론머스크 그룹: 손의 머스크멜론 1장당, 멜론 모두 소멸(쥘까 먹을까)
    U(H, 4, '멜론머스크 그룹', 1, '공격', [dmg(0.7), perTag(ML), dmg(0.45), exile('hand', { all: true, tag: ML })], [
      'A', 'B',
      Or([dmg(0.85), perTag(ML), dmg(0.5)]),
      ['D', 'make', [dmg(0.25, ER)], { limit: 1 }],
      'Hd',
    ], bl('atkUp', 'ap', [make(ML, 1)])),
    // 갈래 부품(사기) — 광합성(0): 회복, 주가 2 를 털어 머스크멜론 둘(모자라면 회복만)
    U(H, 5, '광합성', 0, '스킬', [heal(0.3), ifStack(K, 2), spendN(K, 2), make(ML, 2)], nm([
      Or([heal(0.3), ifStack(K, 2), spendN(K, 2), make(ML, 3)]),
      Or([heal(0.3), ifStack(K, 3), spendN(K, 3), make(ML, 3)]),
      Or([heal(0.3), make(ML, 2), draw(1)]),
      Or([make(ML, 2), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([ifStack(K, 2), spendN(K, 2), make(ML, 3), disc(1)]),
    ]), bl('heal', 'draw', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 5. 뮤트 — 계산형 · 서포터 · 순수. 표적을 홀로그램 그물로 감아 지지고(적 표식 · 턴 끝 피해), 이번 턴 쓴 AP 를 「꼭 n」 으로 맞추면(셈 — tally) AP +1(SP 주유)
// 원작: 「확률대로만 움직이려는」 데이터 버릇 · 저학년 최대 HP 가장 높은 적 홀로그램 포위 · 고학년 해킹 · 강화 평타 「SP 75% 미만 아군 SP 가득」 · 어사이드 에러 메시지
// 옛 「SP 주유」(파티 넷째 카드마다 AP +1 · 드로우 — 세 수 탐색 봇이 52% 까지 씀) → 셈이 맞으면 AP +1 · 드로우(턴 1회) + 넷째 카드는 AP +1 만(드로우 뺌).
// 다 차면: 옛 「에러 메시지」(저절로 큰 피해 + 취약) → 여섯이면 모두 써서 파티의 다음 카드 +60%(stackReach → spend + empower). 취약은 셈이 맞으면 거는 카드(와이어 함정)로
// 시동: u1 웹트래핑(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function mute(j) {
  const H = '뮤트', K = '홀로그램';
  const h = j.heroes[0];
  h.blurb = '나타와 뒤섞여 머릿속에서 늘 회의 중인 정보의 정령. 표적을 홀로그램 그물로 감아 지지고, 이번 턴 쓴 AP 를 딱 맞추면 SP 를 채우듯 AP 가 돌아옵니다 — 확률 99% 정도?';
  h.keyword = { ...h.keyword, rules: [pas('에러 메시지', 'stackReach', [spendAll(K), empower(0.6, 'any')], { when: { id: K, n: 6 } })] };
  h.passives = [
    pas('웹트래핑', 'turnStart', [stk(K, 2, TOP)]),
    pas('SP 주유', 'tally', [ap(1), draw(1)], { limit: 1 }),
    pas('SP 주유', 'play', [ap(1)], { when: { who: 'any', nth: 4 } }),
  ];
  setCards(j, [
    // 시동 — 웹트래핑(0): 홀로그램 3, 이번 턴 쓴 AP 가 꼭 3 이면 드로우(셈 → AP +1)
    U(H, 1, '웹트래핑', 0, '스킬', [stk(K, 3, E1), ifSpent(3), draw(1)], nm([
      Or([stk(K, 5, E1), ifSpent(3), draw(1)]),
      Or([stk(K, 3, E1), draw(1), ifSpent(3), draw(1)]),
      Or([stk(K, 3, E1), stk(K, 1, EA), ifSpent(3), draw(1)]),
      Or([stk(K, 4, E1), srch()]),
      Or([stk(K, 5, E1), disc(1)]),
    ]), null),
    // 쓰기 — 강제 종료: 피해 + 홀로그램 1개당, 전부 소모
    U(H, 2, '강제 종료', 1, '공격', [dmg(0.6), per(K), dmg(0.15), spendAll(K)], [
      'A', 'B',
      Or([dmg(0.6), per(K), dmg(0.15), ifSpent(3), st('취약', 2)]),
      ['D', 'tally', [stk(K, 2, TOP)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 원작 자유 — 와이어 함정(어사이드 에러 메시지): 피해 + 홀로그램 2, 셈이 맞으면 취약
    U(H, 3, '와이어 함정', 1, '공격', [dmg(0.8), stk(K, 2, E1), ifSpent(3), st('취약', 1, E1)], [
      'A', 'B',
      ['D', 'turnStart', [stk(K, 1, TOP)]],
      Or([dmg(0.55, EA), stk(K, 1, EA), ifSpent(3), st('취약', 1, EA)]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 둘째 — 뮤트의 뜻대로(강화): 사기 + 매 턴 HP 가장 높은 적에게 홀로그램
    U(H, 4, '뮤트의 뜻대로', 1, '강화', [st('사기', 1), pw('turnStart', [stk(K, 1, TOP)])], nm([
      Or([st('사기', 1), stk(K, 2, TOP), pw('turnStart', [stk(K, 1, TOP)])]),
      Or([stk(K, 2, TOP), pw('turnStart', [stk(K, 1, TOP)])], { cost: 0 }),
      Or([st('사기', 1), stk(K, 2, TOP), pw('tally', [draw(1)], { limit: 1 })]),
      Or([st('사기', 1), srch(), pw('turnStart', [stk(K, 1, TOP)])]),
      Or([stk(K, 3, TOP), disc(1), pw('turnStart', [stk(K, 2, TOP)])]),
    ]), bl('atkUp', 'ap', [stk(K, 1, E1)])),
    // 갈래 부품 — 옆집 반찬 돌리기(0): 홀로그램 + 다른 아군 카드, 이번 턴 쓴 AP 가 꼭 2 면 홀로그램 더(셈)
    U(H, 5, '옆집 반찬 돌리기', 0, '스킬', [stk(K, 2, E1), draw(1, { who: 'other' }), ifSpent(2), stk(K, 2, E1)], nm([
      Or([stk(K, 3, E1), draw(1, { who: 'other' }), ifSpent(2), stk(K, 2, E1)]),
      Or([stk(K, 2, E1), draw(2, { who: 'other' }), ifSpent(2), stk(K, 2, E1)]),
      Or([stk(K, 3, E1), draw(1, { who: 'other' }), ifSpent(3), stk(K, 3, E1)]),
      Or([stk(K, 2, E1), draw(1, { who: 'other' }), pw('tally', [stk(K, 2, TOP)], { limit: 1 })], { power: true }),
      Or([stk(K, 4, E1), draw(1, { who: 'other' }), disc(1)]),
    ]), bl('draw', 'guard', [stk(K, 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 6. 미로 — 거드는 형 · 탱커 · 활발. 거울 속에서 동료를 지켜보며 좌우를 뒤집어 따라 한다 — 동료가 공격하면 실드로, 스킬을 쓰면 반사 피해로(play who:other).
//    동료의 다음 한 수를 거울처럼 한 번 더 비춘다(empower any — castOther 는 스피키와 서로 부르며 스택이 넘쳐 뺐다). 거울 속으로 숨으면 맞은 만큼 광선으로 되쏜다(고학년 — 그대로)
// 원작: 남의 동작을 좌우가 뒤집힌 채 따라 함(모모 수리검 · 에슈르 제빵) · 장점을 바로 찾아 칭찬 · 저학년 거울(버티다 폭발) · 고학년 수많은 시선(숨어서 맞은 횟수만큼 광선) · 어사이드 아군 받는 피해↓
// 저절로 터짐 없음(원래도). 「거울에 맺힌 공격」(고학년 연출 쪽지)은 패시브 → 「거울 속」 규칙으로 옮김(패시브 자리에 거드는 계기)
// 시동: u2 거울 속으로 — 그대로
// ════════════════════════════════════════════════════════════════════
function miro(j) {
  const H = '미로', K = '시선', IN = '거울 속';
  const h = j.heroes[0];
  h.blurb = '거울 속에서 지켜보며 응원하는 정령. 동료가 무언가를 하면 좌우를 뒤집어 따라 해 — 공격엔 실드를, 스킬엔 반사를 — 돌려주고, 거울 속으로 숨으면 그동안 날아든 공격을 광선으로 되돌려 보냅니다.';
  const old = h.passives.find(p => p.name === '거울에 맺힌 공격');
  h.keywords = h.keywords.map(k => (k.name === IN ? { ...k, rules: [...k.rules, old] } : k));
  h.passives = [
    pas('지켜보고 있어요', 'hurt', [stk(K, 1, E1)], { when: { guarded: true }, limit: 2 }),
    pas('좌우가 뒤집힌 응원', 'play', [sh(0.2)], { when: { who: 'other', type: '공격' }, limit: 1 }),
    pas('좌우가 뒤집힌 응원', 'play', [ddef(0.2)], { when: { who: 'other', type: '스킬' }, limit: 1 }),
  ];
  setCards(j, [
    // 원작 자유 — 거울 반사(저학년 거울): 방어 기반 피해 + 시선 + 실드
    U(H, 1, '거울 반사', 1, '공격', [ddef(0.45), stk(K, 1, E1), sh(0.5)], [
      'A', 'B',
      Or([ddef(0.4, EA), stk(K, 1, EA), sh(0.4)]),
      ['D', 'hurt', [stk(K, 1, E1)], { when: { guarded: true }, limit: 1 }],
      'Hd',
    ], bl('power', 'guard', [stk(K, 1, E1)])),
    // 시동 — 거울 속으로: 실드 + 적 전체 시선
    U(H, 2, '거울 속으로', 1, '스킬', [sh(1.1), stk(K, 1, EA)], [
      'A', 'B',
      Or([sh(1.0), stk(K, 1, EA), draw(1, { who: 'other' })]),
      ['D', 'play', [sh(0.2)], { when: { who: 'other' }, limit: 2 }],
      'Hn',
    ], null),
    // 둘째 — 이젠 내가 지켜줄 거야(강화, 어사이드): 피해 감소 + 동료가 카드를 낼 때마다 실드(턴 2)
    U(H, 3, '이젠 내가 지켜줄 거야', 1, '강화', [st('피해 감소', 1), pw('play', [sh(0.25)], { when: { who: 'other' }, limit: 1 })], nm([
      Or([st('피해 감소', 2), pw('play', [sh(0.25)], { when: { who: 'other' }, limit: 2 })]),
      Or([pw('play', [sh(0.25)], { when: { who: 'other' }, limit: 2 })], { cost: 0 }),
      Or([st('피해 감소', 1), pw('play', [stk(K, 1, E1)], { when: { who: 'other', type: '공격' }, limit: 2 })]),
      Or([st('피해 감소', 1), srch(), pw('play', [sh(0.25)], { when: { who: 'other' }, limit: 2 })]),
      Or([st('피해 감소', 1), disc(1), pw('play', [sh(0.4)], { when: { who: 'other' }, limit: 2 })]),
    ]), bl('defUp', 'cost', [stk(K, 1, E1)])),
    // 쓰기 — 깨진 거울 조각: 방어 기반 피해 + 시선 1개당, 전부 소모
    U(H, 4, '깨진 거울 조각', 1, '공격', [ddef(0.4), per(K), ddef(0.2), spendAll(K)], [
      'A', 'B',
      Or([ddef(0.3, EA), per(K, { each: true }), ddef(0.15, EA), spendAll(K)]),
      ['D', 'play', [ddef(0.2)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      'Hd',
    ], bl('power', 'draw', [stk(K, 1, E1)])),
    // 갈래 부품 — 거울처럼 따라 하기(0): 실드 + 적 전체 시선 + 파티의 다음 카드 +30%(동료의 다음 한 수를 거울처럼 한 번 더 비춤)
    U(H, 5, '거울처럼 따라 하기', 0, '스킬', [sh(0.4), stk(K, 1, EA), empower(0.3, 'any')], nm([
      Or([sh(0.5), stk(K, 1, EA), empower(0.4, 'any')]),
      Or([sh(0.4), empower(0.3, 'any'), draw(1, { who: 'other' })]),
      Or([stk(K, 2, EA), empower(0.4, 'any')]),
      Or([sh(0.3), empower(0.2, 'any'), pw('play', [stk(K, 1, E1)], { when: { who: 'other', type: '공격' }, limit: 1 })], { power: true }),
      Or([sh(1.1), empower(0.6, 'any'), disc(1)]),
    ]), bl('guard', 'ap', [stk(K, 1, EA)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 블랑셰 — 쌓고 고르기 · 딜러 · 우울. 공격할 때마다 적에게 푸른 장미가 피고(받는 피해↑ — 쥘 이유), 산책으로 다 꺾어 칠까 넷까지 피워 커튼콜(손에 카드)을 받을까
// 원작: 싱크로즈(안 맞은 적부터 튕김 · 막타 확정 치명) · 고학년 파랑새 정원 · 애착 아티팩트 「푸른 장미를 쌓고 막타에 터뜨림」 · 슬픈 연기 · 머리의 장미가 감정을 대신
// 다 차면: 옛 「만개」(저절로 피해 + 강인도) → 뺐다. 「백만 송이 피어나」(넷이면 커튼콜 카드 — 원래 만들던 장치) 그대로
// 시동: u3 앙코르(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function blanche(j) {
  const H = '블랑셰', K = '푸른 장미', T1 = '블랑셰_t1';
  const h = j.heroes[0];
  h.blurb = '감정 대신 머리의 장미가 빛나는 배우. 공격할 때마다 적에게 푸른 장미가 피어 아프게 하고, 장미를 다 꺾어 한 번에 칠지 넷까지 피워 커튼콜을 받을지 고릅니다.';
  h.keyword = { ...h.keyword, per: [{ stat: 'taken', v: 0.08 }] };
  delete h.keyword.rules;
  setCards(j, [
    // 원작 자유 — 싱크로즈: 무작위 3회
    U(H, 1, '싱크로즈', 1, '공격', [dmg(0.42, ER, { hits: 3 })], [
      'A',
      Or([dmg(0.38, E1, { hits: 3 }), ifStack(K, 3), dmg(0.6)]),
      Or([stk(K, 1, EA), dmg(0.35, ER, { hits: 3 }), drawType('공격')]),
      ['D', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 1 }],
      'Hd',
    ], bl('power', 'ap', [stk(K, 1, E1)])),
    // 쓰기 — 장미 정원 산책: 피해 + 장미 1개당, 전부 소모
    U(H, 2, '장미 정원 산책', 1, '공격', [dmg(0.8), per(K), dmg(0.2), spendAll(K)], [
      'A',
      Or([dmg(0.55, EA), per(K, { each: true }), dmg(0.16, EA), spendAll(K)]),
      Or([per(K), dmg(0.45), spendAll(K), ifKill, stk(K, 2, EA)]),
      ['D', 'spend', [draw(1)], { when: { id: K }, limit: 1 }],
      'Hd',
    ], bl('power', 'draw', [stk(K, 1, E1)])),
    // 시동 — 앙코르(0): 장미 2 + 드로우
    U(H, 3, '앙코르', 0, '스킬', [stk(K, 2, E1), draw(1)], nm([
      Or([stk(K, 3, E1), draw(1)]),
      Or([stk(K, 3, E1), pull({ type: '공격' })]),
      Or([stk(K, 1, E1), draw(1), pw('turnStart', [stk(K, 1, TOP)])], { power: true }),
      Or([stk(K, 3, E1), srch({ type: '공격' })]),
      Or([payPct(0.03), stk(K, 4, E1), draw(1)]),
    ]), null),
    // 둘째 — 무대 위의 배우(강화): 치명 + 장미가 넷이 되면 드로우
    U(H, 4, '무대 위의 배우', 1, '강화', [crit(0.15), pw('stackReach', [draw(1)], { when: { id: K, n: 4 } })], nm([
      Or([crit(0.2), pw('stackReach', [draw(1)], { when: { id: K, n: 4 } })]),
      Or([crit(0.1), pw('stackReach', [draw(1)], { when: { id: K, n: 4 } })], { cost: 0 }),
      Or([crit(0.15), pw('play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 })]),
      Or([crit(0.15), srch(), pw('stackReach', [draw(1)], { when: { id: K, n: 4 } })]),
      Or([crit(0.25), disc(1), pw('stackReach', [draw(1)], { when: { id: K, n: 4 } })]),
    ]), bl('atkUp', 'cost', [stk(K, 1, E1)])),
    // 갈래 부품 — 모노레일 출퇴근(0): 장미 + 손의 자신의 공격 카드 1장 비용 -1
    U(H, 5, '모노레일 출퇴근', 0, '스킬', [stk(K, 1, E1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })], nm([
      Or([stk(K, 2, E1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })]),
      Or([stk(K, 2, E1), cs('비용', -1, { to: 'hand', n: 2, who: 'self', type: '공격' })]),
      Or([stk(K, 1, E1), cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' }), draw(1)]),
      Or([stk(K, 1, E1), pw('turnStart', [cs('비용', -1, { to: 'hand', n: 1, who: 'self', type: '공격' })])], { power: true }),
      Or([stk(K, 3, E1), disc(1)]),
    ]), bl('draw', 'weakSpot', [stk(K, 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 빅우드 — 회복형 · 탱커 · 순수 · 2성(변환). 맞을 때 · HP 80%를 넘겨 회복할 때 나이테가 늘고(1개당 주는 실드↑), 다섯이면 황금 사과 둘(onMax make — 원래 만들던 장치)
//    — 사과를 따 나눌까(회복 → 다시 나이테), 통나무로 굴려 버릴까
// 원작: 「아낌없이 주는 나무」 · 맞는 걸 즐김(뜯기는 만큼 나눔) · 저학년 자연을 지키자(큰 실드) · 고학년 도발 + 자기 회복 · 어사이드 황금 사과 · 욕구가 채워지면 현자 모드
// 다 차면: 옛 「현자 모드」(stackReach → 사과 2) → onMax make(겹 다 씀). 넘친 회복 → 나이테(overheal pct 0.8). 피톤치드(전투 시작 피해 감소)는 고유 효과 규칙으로
// 시동: u2 열매 나눠 주기(0) — 그대로
// ════════════════════════════════════════════════════════════════════
function bigwood(j) {
  const H = '빅우드', K = '나이테', AP = '빅우드_apple';
  const h = j.heroes[0];
  h.blurb = '얻어맞으면서도 웃는 나무 정령. 맞거나 넘치게 회복할 때마다 나이테가 늘어 실드가 두꺼워지고, 나이테가 다 차면 황금 사과 두 알이 열립니다 — 따서 나눌까, 통나무로 굴려 버릴까.';
  h.keyword = { ...h.keyword, onMax: { make: AP, n: 2, consume: true }, rules: [pas('피톤치드', 'fightStart', [st('피해 감소', 1)])] };
  h.passives = [
    pas('더 세게~', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
    pas('아낌없이 주는 나무', 'overheal', [stk(K, 1)], { when: { pct: 0.8 }, limit: 2 }),
  ];
  setCards(j, [
    // 원작 자유 — 자연을 지키자(저학년): 실드 + 나이테
    U(H, 1, '자연을 지키자', 1, '스킬', [sh(1.3), stk(K, 1)], [
      'A', 'B',
      ['D', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }],
      Or([sh(1.2), stk(K, 1), srch()]),
      'Hn',
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 시동 — 열매 나눠 주기(0): 황금 사과 + 나이테
    U(H, 2, '열매 나눠 주기', 0, '스킬', [make(AP, 1), stk(K, 1)], nm([
      Or([make(AP, 1), stk(K, 2)]),
      Or([make(AP, 2)]),
      Or([make(AP, 1), stk(K, 1), draw(1)]),
      Or([make(AP, 1), stk(K, 1), sh(0.6)]),
      Or([make(AP, 2), stk(K, 1), disc(1)]),
    ]), null),
    // 쓰기 — 통나무 굴리기: 방어 기반 + 나이테 1개당, 전부 소모
    U(H, 3, '통나무 굴리기', 1, '공격', [ddef(0.45), per(K), ddef(0.13), spendAll(K)], [
      'A', 'B',
      Or([ddef(0.35, EA), per(K), ddef(0.1, EA), spendAll(K)]),
      Or([ddef(0.4), per(K), ddef(0.12), make(AP, 1)]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 숲의 품: 회복, 이번 판 HP 80%를 넘긴 회복 25당 실드(넘친 회복 → 실드 · 나이테)
    U(H, 4, '숲의 품', 1, '스킬', [heal(0.8), perOver(0.8, 25), sh(0.3)], [
      'A', 'B',
      Or([heal(0.6), perOver(0.8, 25), sh(0.2), pw('overheal', [sh(0.4)], { when: { pct: 0.8 }, limit: 1 })], { power: true }),
      Or([heal(0.7), perOver(0.8, 25), sh(0.25), make(AP, 1)]),
      'Hd',
    ], bl('heal', 'draw', [make(AP, 1)])),
    // 둘째 — 메이드장의 채찍: 방어 기반 피해 + 나이테 + 실드
    U(H, 5, '메이드장의 채찍', 1, '공격', [ddef(0.6), stk(K, 1), sh(0.5)], [
      'A',
      Or([ddef(0.6), stk(K, 1), pw('hurt', [sh(0.3)], { when: { guarded: true }, limit: 2 })], { power: true }),
      Or([ddef(0.5, EA), stk(K, 1), sh(0.4)]),
      Or([ddef(0.55), stk(K, 1), srch()]),
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ── 세기 맞춤(측정 뒤) ──
const TUNE = { '정령/나이아': 1.3, '정령/멜루나': 1.4, '정령/뮤트': 0.8, '정령/미로': 0.7, '정령/블랑셰': 1.3, '정령/빅우드': 1.15 };

// ── 돌리기 ──
const JOBS = [
  ['정령/가비아', gabia], ['정령/나이아', naia], ['정령/라이카', laika], ['정령/멜루나', meluna],
  ['정령/뮤트', mute], ['정령/미로', miro], ['정령/블랑셰', blanche], ['정령/빅우드', bigwood],
];
L.run18(JOBS, TUNE, new URL('./boost_정령A_18.json', import.meta.url));
