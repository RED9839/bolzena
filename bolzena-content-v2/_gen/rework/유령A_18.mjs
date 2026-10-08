// 18갈래 재설계 3단계 — 유령A 묶음 9명(레테 · 림 · 림(혼돈) · 메죵 · 바롱 · 베루 · 벨라 · 사리 · 셀리네). 2026-10-08
// 기준: 시범 17명(_measure/갈래_세분화/05_시범16_결과.md) · 지침 _gen/rework/BRIEF_118.md · 기록 _measure/갈래_세분화/06_118/유령A.md
// 틀: 시작 덱 = 기본 3 + 시동 1 · 은총 고유 4장(만들기 · 쓰기 · 갈래 부품 · 원작 자유) · 신탁 5갈래 · 축복 4장 × 3(시동 카드는 공용 축복 풀).
// 다 차면 저절로 터짐 없음(onMax make/empower) · 인원 세기 없음 · 신탁 · 축복 이름은 짓지 않는다(자리 표시).
// node _gen/rework/유령A_18.mjs [사도 파일 이름 일부]  → heroes/유령/<파일>.json (백업 SRC 에서 읽음 — 몇 번 돌려도 같은 결과)
import * as L from './lib18.mjs';
const { E1, EA, ER, dmg, ddef, hits, sh, heal, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, pas, pw, U, Or, card, bl, setCards, setOpener,
  token, disc, exile, srch, drawType, pull, xtra, nextAp, spendN, OTHER } = L;
const M = list => list.map((o, i) => ({ name: `신탁 ${i + 1}`, ...o }));
const ifBal = { k: 'ifBalanced' };
const ifHeld = n => ({ k: 'ifHeld', n });
const perDisc = { k: 'perDiscarded' };
const perGuard = p => ({ k: 'perGuarded', per: p });
const onEnter = { k: 'when', on: 'discard' };   // 안식 — 버려지면
const stkEv = (id, of) => ({ k: 'stack', id, v: 1, ofEvent: of });

// ════════════════════════════════════════════════════════════════════
// 1. 레테 — 버리기형 · 탱커 · 냉정. 손패를 잠깐 잊어(버려) 「지운 기억」을 모으고, 다 차면 잊은 기억이 손으로 한꺼번에 돌아온다
// 원작: 레이저로 기억을 지움 · 지운 기억은 잠깐 뒤 돌아온다 · 저학년 다친 기억을 잊고 회복 · 고학년 플래시(눈가림) · 평타 4연타
// 장치: 「지운 기억」(다 차면 「돌아온 기억」 카드 — 버린 더미 2장을 손으로) · 시동: u3 눈앞의 레이저(0)
// ════════════════════════════════════════════════════════════════════
function lethe(j) {
  const H = '레테', K = '지운 기억', MEM = '레테_memory';
  const h = j.heroes[0];
  h.blurb = '레이저 포인터로 싫은 기억을 지우는 망각의 유령. 손패를 잠깐 잊어 버릴 때마다 「지운 기억」 이 모이고, 다 차면 잊었던 기억이 카드째 손으로 밀려옵니다. 포인터를 몇 번 휘두르면 레이저가 연달아 꽂힙니다.';
  h.keyword = { name: K, desc: '레이저로 잠깐 지워 둔 아팠던 기억', carrier: 'self', cap: 4, onMax: { make: MEM, consume: true } };
  h.passives = [
    pas('아팠던 기억', 'discard', [stk(K, 1)], { limit: 2 }),
    pas('아팠던 기억', 'hurt', [stk(K, 1)], { limit: 1 }),
    pas('레이저 테러범', 'play', [{ k: 'extra', ratio: 0.15, hits: 4, target: E1 }], { when: { type: '공격', every: 2 } }),
  ];
  const tokens = [token(MEM, '돌아온 기억', H, '스킬', [pull({ n: 2 }), sh(1.0), st('피해 감소', 1)], { blurb: '잠깐 잊었던 기억이 한꺼번에 밀려옵니다' })];
  setCards(j, [
    // 쓰기 — 다친 기억을 잊고 회복: 실드 + 「지운 기억」 1개당 회복, 전부 소모(다 차기 전에 털지)
    U(H, 1, '뉴럴 링크 스따트', 1, '스킬', [sh(1.2), per(K), heal(0.4), spendAll(K)], [
      'A', 'B',
      Or([disc(1), sh(1.0), per(K), heal(0.45)]),
      ['D', 'discard', [heal(0.15)], { limit: 2 }],
      'Hn',
    ], bl('guard', 'heal', [stk(K, 1)])),
    // 갈래 부품 — 포인터 4연타: 안식(버려지면) 2연타 + 기억
    U(H, 2, '포인터 4연타', 1, '공격', [dmg(0.3, E1, { hits: 4 }), onEnter, dmg(0.25, E1, { hits: 2 }), stk(K, 1)], [
      'A',
      Or([dmg(0.3, E1, { hits: 4 }), pw('discard', [dmg(0.35, E1)], { limit: 2 }), onEnter, stk(K, 1)], { power: true }),
      Or([dmg(0.25, EA, { hits: 3 }), onEnter, dmg(0.25, EA, { hits: 2 }), stk(K, 1)]),
      Or([dmg(0.3, E1, { hits: 4 }), onEnter, stk(K, 2), srch()]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 시동 — 눈앞의 레이저(0): 1장 뽑고 1장 잊기, 적 약화
    card(H, 3, '눈앞의 레이저', 0, '스킬', [draw(1), disc(1), st('약화', 1, E1)], M([
      Or([draw(2), disc(1), st('약화', 1, E1)]),
      Or([draw(1), disc(1), st('약화', 1, EA)]),
      Or([draw(1), disc(1), stk(K, 2)]),
      Or([draw(1), disc(1), pw('discard', [stk(K, 1), sh(0.2)], { limit: 2 })], { power: true }),
      Or([draw(3), disc(2)]),
    ]), null),
    // 만들기 둘째 — 기억 소거: 2장 뽑고 2장 잊기, 버린 1장당 실드
    card(H, 4, '기억 소거', 1, '스킬', [draw(2), disc(2), perDisc, sh(0.6)], M([
      Or([draw(3), disc(2), perDisc, sh(0.6)]),
      Or([draw(2), disc(2), perDisc, sh(0.5)], { cost: 0 }),
      Or([draw(2), disc(2), perDisc, heal(0.85)]),
      Or([draw(2), disc(2), pw('discard', [sh(0.5), stk(K, 1)], { limit: 2 })], { power: true }),
      Or([draw(3), { k: 'discard', all: true }, stk(K, 2)]),
    ]), bl('guard', 'ap', [stk(K, 1)])),
    // 원작 자유 — 기습 플래시(고학년 눈가림 → 약화)
    U(H, 5, '기습 플래시', 1, '공격', [dmg(0.45, E1, { hits: 2 }), st('약화', 2, E1), sh(0.5)], [
      'A', 'B',
      Or([dmg(0.42, EA, { hits: 2 }), st('약화', 1, EA), sh(0.4)]),
      ['D', 'hurt', [st('약화', 1, E1)], { limit: 1 }],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 2. 림 — 계산형 · 딜러 · 우울. 공격과 스킬을 같은 장수로 맞춰 저울을 세운다 — 저울이 서면(셈) 무게추가 파티를 회복하고 낫 자국이 깊어진다
// 원작: 균형의 유령(저울 반대편의 무게추) · 저학년 자기 회복 + 범위 참격 + 쓰라림 · 고학년 범위 2회 + 흡혈 · 애착 아티팩트 돌아오는 낫 · 썰렁 아재개그
// 장치: 「낫 자국」(적 · 턴 끝 지속 피해 — 터짐 없음) + 계기 tally(셈이 서면) · 시동: u1 스크래치 사이드(그대로)
// ════════════════════════════════════════════════════════════════════
function rim(j) {
  const H = '림', K = '낫 자국';
  const h = j.heroes[0];
  h.blurb = '입만 열면 아재개그로 분위기를 얼리는 질서의 2인자. 공격과 스킬을 같은 장수 낸 턴엔 저울이 맞아 무게추가 파티를 다독이고, 적의 낫 자국이 깊어집니다.';
  h.passives = [
    pas('쓰라림', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }),
    pas('무게추', 'tally', [heal(0.3), stk(K, 1, EA)], { limit: 1 }),
  ];
  setCards(j, [
    // 시동 — 스크래치 사이드(원작 저학년): 적 전체 + 낫 자국, 저울이 서면 회복
    U(H, 1, '스크래치 사이드', 1, '공격', [dmg(0.75, EA), stk(K, 1, EA), ifBal, heal(0.45)], [
      'A', 'B',
      Or([dmg(0.7, EA), stk(K, 1, EA), drawType('스킬')]),
      Or([dmg(0.75, EA), stk(K, 2, EA)]),
      'Hd',
    ], null),
    // 원작 자유 — 날아가는 기역(돌아오는 낫): 단일 + 낫 자국 2, 저울이 서면 자신의 공격 카드 1장
    U(H, 2, '날아가는 기역', 1, '공격', [dmg(1.05), stk(K, 2, E1), ifBal, drawType('공격')], [
      'A',
      ['D', 'tally', [stk(K, 1, EA)], { limit: 1 }],
      Or([dmg(0.8, EA), stk(K, 2, EA), ifBal, drawType('공격')]),
      Or([dmg(1.0), stk(K, 2, E1), srch()]),
      'Hn',
    ], bl('power', 'draw', [stk(K, 1, E1)])),
    // 쓰기 — 기역의 추수: 낫 자국 1개당, 전부 거둠. 저울이 서면 흡혈(회복)
    U(H, 3, '기역의 추수', 1, '공격', [per(K, { n: 1 }), dmg(0.5), spendAll(K), ifBal, heal(0.4)], [
      'A',
      ['D', 'tally', [dmg(0.4, E1)], { limit: 1 }],
      Or([per(K, { n: 1 }), dmg(0.4, EA), spendAll(K), ifBal, heal(0.4)]),
      ['C', [st('약화', 1, E1)]],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 갈래 부품 — 균형의 저울(0): 모자란 쪽(스킬)을 채우는 추 — 낫 자국 + 공격 카드 1장, 저울이 서면 AP +1
    card(H, 4, '균형의 저울', 0, '스킬', [stk(K, 1, EA), drawType('공격'), ifBal, ap(1)], M([
      Or([stk(K, 2, EA), drawType('공격'), ifBal, ap(1)]),
      Or([stk(K, 1, EA), drawType('공격', 2), ifBal, ap(1)]),
      Or([dmg(0.5, EA), drawType('공격'), ifBal, ap(1)]),
      Or([stk(K, 1, EA), drawType('공격'), pw('tally', [ap(1)], { limit: 1 })], { power: true }),
      Or([stk(K, 2, EA), drawType('공격', 3), disc(1)]),
    ]), bl('draw', { tags: ['보존'] }, [stk(K, 1, EA)])),
    // 만들기 둘째 — 슴슴한 호박 스프(0): 낫 자국 + 회복 + 고유 카드
    U(H, 5, '슴슴한 호박 스프', 0, '스킬', [stk(K, 1, EA), heal(0.42), srch()], [
      Or([stk(K, 2, EA), heal(0.5), srch()]),
      'B',
      Or([{ k: 'rushDown', v: 1, target: EA }, heal(0.5), draw(2)]),
      Or([stk(K, 1, EA), srch(), pw('play', [stk(K, 1, EA)], { when: { type: '스킬' }, limit: 1 })], { power: true }),
      Or([stk(K, 2, EA), draw(2), disc(1)]),
    ], bl('heal', 'draw', [stk(K, 1, EA)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 3. 림(혼돈) — 소멸형 · 딜러 · 광기. 동료가 버린 카드를 떠맡아 치우고(소멸) 혼돈을 키운다 — 다 차면 다음 카드가 크게(넘치는 혼돈)
// 원작: 셰이디의 빈자리를 떠안은 혼돈 · 남이 떠넘긴 음식 · 청소를 다 떠맡음 · 저학년 순간이동 휩쓸기(맞힌 적 수만큼) · 고학년 차원의 눈(평타 강화)
// 장치: 「혼돈」(다 차면 다음 카드 +50%) · 시동: u5 남은 음식 떠맡기(0 · 옛 u1 혼돈에 이끌림)
// ════════════════════════════════════════════════════════════════════
function rimChaos(j) {
  const H = '림_혼돈', K = '혼돈';
  const h = j.heroes[0];
  h.blurb = '혼돈의 빈자리를 대신 떠안은 질서의 유령. 동료가 버린 카드를 떠맡아 치울수록 혼돈이 차오르고, 다 차면 다음 한 수가 크게 넘쳐흐릅니다. 차원의 눈을 열면 한동안 낫이 차원 너머까지 닿습니다.';
  h.keyword = { name: K, desc: '억지로 떠안은 어설픈 혼돈', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.06 }], onMax: { empower: 'next', ratio: 0.5, consume: true } };
  h.passives = [
    pas('떠맡은 뒤처리', 'exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 2 }),
    pas('어엿한 혼돈', 'play', [stk(K, 1)], { when: { type: '공격' }, conds: [{ c: 'foes', n: 2 }], limit: 2 }),
  ];
  const ex1 = exile('discard', { n: 1, who: 'other' });
  setOpener(j, H, 'u1', 'u5');
  setCards(j, [
    // 원작 자유 — 혼돈에 이끌림(저학년 휩쓸기)
    U(H, 1, '혼돈에 이끌림', 1, '공격', [dmg(0.8, EA), stk(K, 2)], [
      'A', 'B',
      ['C', [st('기절', 1, ER)]],
      Or([dmg(0.9, EA), stk(K, 2), ex1]),
      'Hd',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 만들기 둘째 — 차원 베기(고학년 강화 공격): 단일, 혼돈 셋 이상이면 더
    U(H, 2, '차원 베기', 1, '공격', [dmg(1.1), stk(K, 1), ifStack(K, 3), dmg(0.55)], [
      'A',
      ['D', 'turnStart', [stk(K, 1)]],
      Or([dmg(0.8, EA), stk(K, 1), ifStack(K, 3), dmg(0.4, EA)]),
      Or([dmg(1.15), stk(K, 2), srch()]),
      'Hn',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 무서워하십시오!: 어설프게 겁주며 동료 카드 둘을 치운다
    card(H, 3, '무서워하십시오!', 1, '스킬', [st('약화', 2, E1), exile('discard', { n: 2, who: 'other' }), stk(K, 1)], M([
      Or([st('약화', 3, E1), exile('discard', { n: 2, who: 'other' }), stk(K, 1)]),
      Or([st('약화', 2, E1), exile('discard', { n: 2, who: 'other' }), stk(K, 1)], { cost: 0 }),
      Or([dmg(0.5, EA), exile('discard', { n: 2, who: 'other' }), stk(K, 2)]),
      Or([st('약화', 2, E1), stk(K, 1), pw('turnStart', [exile('discard', { n: 1, who: 'other' }), stk(K, 1)])], { power: true }),
      Or([disc(1), exile('discard', { n: 3 }), stk(K, 4)]),
    ]), bl('draw', 'ap', [stk(K, 1)])),
    // 쓰기 — 꿀밤: 혼돈 1개당 적 전체, 전부 소모(넘치기 전에 털지)
    U(H, 4, '꿀밤', 1, '공격', [dmg(0.5, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      'A',
      ['D', 'exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 1 }],
      Or([per(K, { n: 1 }), dmg(0.6), spendAll(K), st('기절', 1, E1)]),
      ['C', [st('기절', 1, ER)]],
      'Hx',
    ], bl('power', 'draw', [stk(K, 1)])),
    // 시동 — 남은 음식 떠맡기(0): 버린 더미의 동료 카드 1장 소멸, 혼돈 2
    card(H, 5, '남은 음식 떠맡기', 0, '스킬', [ex1, stk(K, 2)], M([
      Or([ex1, stk(K, 3)]),
      Or([ex1, stk(K, 2), sh(0.6)]),
      Or([ex1, stk(K, 1), dmg(0.4, EA)]),
      Or([ex1, stk(K, 1), pw('turnStart', [stk(K, 1)])], { power: true }),
      Or([disc(1), ex1, stk(K, 4)]),
    ]), null),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 4. 메죵 — 아껴 두기형 · 딜러 · 광기 · 1성. 보존 카드(집문서 · 수리검)를 손에 눌러앉힌 채 턴을 넘길수록 「입주」가 는다
// 원작: 안식의 유령 — 마음에 들면 어디든 제 집으로 · 집문서 집착 · 저학년 수리검 3개 · 고학년 단일 피해
// 장치: 「입주」(보존 카드를 쥐고 턴을 마치면 +1 · keepAp keep) + 「집문서」 카드(보존) · 시동: u3 눌러앉기(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function mezyong(j) {
  const H = '메죵', K = '입주', DEED = '메죵_deed';
  const h = j.heroes[0];
  h.blurb = '어디든 들어가면 거기가 내 집이 되는 태평한 유령. 집문서와 수리검을 손에 눌러앉힌 채 턴을 넘길수록 「입주」 가 늘고, 집주인이 쓰러지면 짐 싸서 다음 집으로.';
  h.passives = [
    pas('여기가 내 집', 'keepAp', [stk(K, 1)]),
    pas('새 집 찾기', 'kill', [make(DEED, 1), stk(K, 1)], { when: { mine: true }, limit: 1 }),
  ];
  const deed = j.cards.find(c => c.id === DEED);
  Object.assign(deed, { tags: ['보존', '소멸'], fx: [dmg(0.8, ER), stk(K, 1), draw(1)], blurb: '어디든 내 집 — 쥐고 있으면 눌러앉고, 내면 짐을 풉니다' });
  const tp = ['보존'];
  setCards(j, [
    // 원작 자유 — 수리검 날아갑니다!(보존): 손에 1턴 묵혔으면 2회 더
    U(H, 1, '수리검 날아갑니다!', 1, '공격', [dmg(0.5, E1, { hits: 3 }), ifHeld(1), dmg(0.5, E1, { hits: 2 })], [
      'A', 'B',
      Or([dmg(0.45, ER, { hits: 4 }), ifHeld(1), dmg(0.45, ER, { hits: 2 })], { tags: tp }),
      Or([dmg(0.5, E1, { hits: 3 }), pw('keepAp', [stk(K, 1), dmg(0.4, ER, { hits: 2 })], { when: { kind: 'keep' } })], { power: true, tags: tp }),
      ['Ht', '보존'],
    ], bl('power', 'draw', [stk(K, 1)]), tp),
    // 쓰기 — 집문서 내놔: 입주 1개당, 전부 소모
    U(H, 2, '집문서 내놔', 1, '공격', [dmg(1.0), per(K), dmg(0.4), spendAll(K)], [
      'A',
      ['D', 'kill', [make(DEED, 1)], { limit: 1 }],
      Or([dmg(0.9), per(K), dmg(0.35), make(DEED, 1)]),
      ['C', [make(DEED, 1)]],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 시동 — 눌러앉기(개전 강화 0): 집문서 + 매 턴 집문서
    card(H, 3, '눌러앉기', 0, '강화', [make(DEED, 1), pw('turnStart', [make(DEED, 1)])], M([
      Or([make(DEED, 2), pw('turnStart', [make(DEED, 1)])], { tags: ['개전'] }),
      Or([make(DEED, 1), pw('turnStart', [make(DEED, 1), stk(K, 1)])], { tags: ['개전'] }),
      Or([make(DEED, 1), stk(K, 1), L.power(L.rule('turnStart', [make(DEED, 1)]), L.rule('keepAp', [stk(K, 1)], { when: { kind: 'keep' } }))], { tags: ['개전'] }),
      Or([make(DEED, 1), srch(), pw('turnStart', [make(DEED, 1)])], { tags: ['개전'] }),
      Or([make(DEED, 2), stk(K, 2), pw('turnStart', [make(DEED, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 갈래 부품 — 제가 주인인 양: 손의 집문서 1장당, 모두 소멸(쥔 집을 한 번에)
    U(H, 4, '제가 주인인 양', 1, '공격', [dmg(0.8), perTag(DEED), dmg(0.6), exile('hand', { all: true, tag: DEED })], [
      'A',
      Or([dmg(0.9), perTag(DEED), dmg(0.7)]),
      Or([dmg(0.6, EA), perTag(DEED), dmg(0.4, EA), exile('hand', { all: true, tag: DEED })]),
      ['D', 'keepAp', [make(DEED, 1)], { when: { kind: 'keep' }, limit: 1 }],
      'Hn',
    ], bl('power', 'weakSpot', [make(DEED, 1)])),
    // 만들기 둘째 — 안방 차지: 실드 + 입주 + 집문서
    U(H, 5, '안방 차지', 1, '스킬', [sh(1.2), stk(K, 1), make(DEED, 1)], [
      'A', 'B',
      Or([sh(1.0), make(DEED, 2)]),
      Or([sh(1.1), make(DEED, 1), pw('keepAp', [stk(K, 1), sh(0.5)], { when: { kind: 'keep' } })], { power: true }),
      Or([sh(1.4), make(DEED, 2), disc(1)]),
    ], bl('guard', 'ap', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 5. 바롱 — 돈형 · 딜러 · 냉정. 뒷담 건 적을 칠 때마다 「장난 밑천」을 벌고, 모은 밑천을 한 번에 털어 「전 재산 장난」 카드를 산다
// 원작: 주식을 저점에 사 고점에 팔아 크게 벌고 장난 한 번에 몽땅 탕진 · 사재기로 시장을 막음 · 저학년 저주(받은 피해가 다른 적에게 번짐) · 못 2연타 평타
// 장치: 「장난 밑천」(전투 안 벌이 — 판 골드 아님) → 사재기로 소모해 자기 카드 만들기 + 적 표식 「뒷담」 · 시동: u5 주식 단타(0 · 옛 u1)
// ════════════════════════════════════════════════════════════════════
function barong(j) {
  const H = '바롱', K = '뒷담', MN = '장난 밑천', PR = '바롱_prank';
  const h = j.heroes[0];
  h.blurb = '토끼 인형을 꿰매며 거짓말과 이간질을 즐기는 착각의 유령. 뒷담 건 적을 칠 때마다 장난 밑천을 벌고, 모은 밑천은 한 번에 털어 큰 장난을 사 들입니다. 저주받은 못이 따라 꽂힙니다.';
  h.keyword = { name: K, desc: '인형 속 주인님이 퍼뜨린 거짓말', carrier: 'enemy', cap: 3, per: [{ stat: 'taken', v: 0.12 }] };
  h.keywords = [{ name: MN, desc: '인형 주인님 명의로 굴리는 장난 밑천', carrier: 'self', cap: 6 }];
  h.passives = [
    pas('저주받은 못질', 'play', [{ k: 'extra', ratio: 0.35, hits: 2, target: E1 }, stk(MN, 1)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }], limit: 2 }),
    pas('주인님의 시간', 'fightStart', [sh(1.0)]),
  ];
  const tokens = [token(PR, '전 재산 장난', H, '공격', [dmg(1.3, EA), stk(K, 1, EA)], { blurb: '모은 밑천을 장난 한 번에 몽땅 털어 넣습니다' })];
  const buy = (n = 3, v = 1) => [ifStack(MN, n), spendN(MN, n), make(PR, v)];
  setOpener(j, H, 'u1', 'u5');
  setCards(j, [
    // 원작 자유 — 뒷담까기 인형(저학년 저주): 뒷담 + 다른 적에게 번지는 피해
    U(H, 1, '뒷담까기 인형', 1, '공격', [dmg(1.0), stk(K, 1, E1), dmg(0.35, OTHER)], [
      'A', 'B',
      Or([dmg(0.7, EA), stk(K, 1, EA)]),
      Or([dmg(1.0), stk(K, 1, E1), pw('hit', [dmg(0.35, OTHER)], { limit: 1 })], { power: true }),
      'Hd',
    ], bl('power', 'weakSpot', [stk(MN, 1)])),
    // 만들기 둘째 — 사람 사이 갈라놓기: 뒷담 2 · 약화 · 밑천
    card(H, 2, '사람 사이 갈라놓기', 1, '스킬', [stk(K, 2, E1), st('약화', 1, E1), stk(MN, 1)], M([
      Or([stk(K, 3, E1), st('약화', 1, E1), stk(MN, 1)]),
      Or([stk(K, 2, E1), st('약화', 1, E1), stk(MN, 1)], { cost: 0 }),
      Or([stk(K, 2, E1), dmg(0.6, OTHER), stk(MN, 2)]),
      Or([stk(K, 2, E1), stk(MN, 1), pw('turnStart', [stk(MN, 1)])], { power: true }),
      Or([stk(K, 2, E1), stk(MN, 2), srch()]),
    ]), bl('draw', 'ap', [stk(MN, 1)])),
    // 쓰기 — 못 날리기: 2연타 + 뒷담 1개당, 전부 소모
    U(H, 3, '못 날리기', 1, '공격', [dmg(0.5, E1, { hits: 2 }), per(K), dmg(0.4), spendAll(K)], [
      'A',
      ['D', 'play', [stk(MN, 1)], { when: { type: '공격' }, limit: 1 }],
      Or([dmg(0.5, E1, { hits: 2 }), per(K), dmg(0.35), stk(MN, 1)]),
      Or([dmg(0.4, EA, { hits: 2 }), per(K, { each: true }), dmg(0.25, EA), spendAll(K)]),
      'Hd',
    ], bl('power', 'cost', [stk(K, 1, E1)])),
    // 갈래 부품 — 사재기: 밑천 3 을 털어 「전 재산 장난」 1장(밑천이 모자라면 실드만)
    card(H, 4, '사재기', 1, '스킬', [sh(0.7), ...buy()], M([
      Or([sh(1.0), ...buy()]),
      Or([sh(0.9), ...buy(2)]),
      Or([...buy(5, 2), draw(1)]),
      Or([sh(0.8), stk(MN, 2), pw('stackReach', [spendN(MN, 3), make(PR, 1)], { when: { id: MN, n: 3 } })], { power: true }),
      Or([...buy(), draw(1)], { cost: 0 }),
    ]), bl('guard', 'draw', [stk(MN, 1)])),
    // 시동 — 주식 단타(0): 뒷담 1 · 밑천 1 · 공격 카드 1장
    card(H, 5, '주식 단타', 0, '스킬', [stk(K, 1, E1), stk(MN, 1), drawType('공격')], M([
      Or([stk(K, 1, E1), stk(MN, 2), drawType('공격', 2)]),
      Or([stk(K, 2, E1), stk(MN, 2), drawType('공격')]),
      Or([stk(K, 1, E1), stk(MN, 1), dmg(0.8)]),
      Or([stk(K, 1, E1), stk(MN, 2), pull({ who: 'self', type: '공격' })]),
      Or([disc(1), stk(K, 1, E1), stk(MN, 4)]),
    ]), null),
  ], tokens);
}

// ════════════════════════════════════════════════════════════════════
// 6. 베루 — 표적형 · 딜러 · 우울 · 1성. 한 놈만 찍어 흠을 판다(찍기 — 한 적에게만) — 흠 붙은 적은 아군 카드가 약점으로 친다
// 원작: 결점의 유령 — 약점 하나를 콕 집어 놀림 · 말만 세고 약함 · 저학년 도끼 3개 · 남의 실수를 구경하며 팝콘. 갈래 근거는 약해 원작 느낌은 「원작 자유」(팝콘 구경)에
// 장치: 「흠」(적 · 찍기 · 약점 공격 — 찍은 적이 쓰러지면 HP 가장 낮은 적으로) · 시동: u1 도끼 날아가요~(그대로)
// ════════════════════════════════════════════════════════════════════
function beru(j) {
  const H = '베루', K = '흠';
  const h = j.heroes[0];
  h.blurb = '남의 흠만 쏙쏙 찾아 험하게 늘어지는 결점의 유령. 한 놈만 찍어 「흠」 을 파고, 흠 붙은 적을 치는 아군 공격은 한 장마다 약점으로 꽂힙니다. 찍은 적이 쓰러지면 다음 놀림거리를 찾습니다.';
  h.keyword = { name: K, desc: '베루가 짚어 낸 적의 결점', carrier: 'enemy', cap: 4, hunt: true, weakens: true, per: [{ stat: 'taken', v: 0.05 }],
    rules: [{ name: '다음 놀림거리', when: { on: 'huntDown' }, fx: [stk(K, 2, 'lowEnemy')] }] };
  setCards(j, [
    // 시동 — 도끼 날아가요~(원작 저학년 도끼 3개)
    U(H, 1, '도끼 날아가요~', 1, '공격', [dmg(0.48, E1, { hits: 3 }), stk(K, 1, E1)], [
      'A', 'B',
      Or([dmg(0.42, E1, { hits: 3 }), stk(K, 1, E1), draw(1, { who: 'other', type: '공격' })]),
      Or([dmg(0.45, E1, { hits: 3 }), ifStack(K, 2), stk(K, 2, E1)]),
      'Hd',
    ], null),
    // 쓰기 — 흠 하나 물고: 흠 1개당, 전부 소모
    U(H, 2, '흠 하나 물고', 1, '공격', [dmg(0.9), per(K), dmg(0.35), spendAll(K)], [
      'A',
      ['D', 'huntDown', [draw(1)], { limit: 1 }],
      Or([dmg(1.0), per(K), dmg(0.4)]),
      ['C', [L.tough(1)]],
      'Hn',
    ], bl('power', 'weakSpot', [stk(K, 1, E1)])),
    // 만들기 둘째 — 친구 계약서(메죵 집문서를 빼앗은 계약): 흠 2 · 취약 · 드로우
    card(H, 3, '친구 계약서', 1, '스킬', [stk(K, 2, E1), st('취약', 1, E1), draw(1)], M([
      Or([stk(K, 3, E1), st('취약', 1, E1), draw(1)]),
      Or([stk(K, 2, E1), st('취약', 1, E1), draw(1)], { cost: 0 }),
      Or([stk(K, 2, E1), st('취약', 2, E1), dmg(0.6)]),
      Or([stk(K, 2, E1), draw(1), pw('play', [stk(K, 1, E1)], { when: { who: 'other', type: '공격' }, limit: 1 })], { power: true }),
      Or([disc(1), stk(K, 4, E1), st('취약', 2, E1)]),
    ]), bl('draw', 'ap', [stk(K, 1, E1)])),
    // 원작 자유 — 캐러멜 팝콘 구경(강화): 다른 아군 카드가 적을 치면 그 적에게 흠(구경하며 놀리기)
    card(H, 4, '캐러멜 팝콘 구경', 1, '강화', [draw(1), pw('hit', [stk(K, 1, E1)], { when: { who: 'other' }, limit: 1 })], M([
      Or([draw(2), pw('hit', [stk(K, 1, E1)], { when: { who: 'other' }, limit: 1 })]),
      Or([pw('hit', [stk(K, 1, E1)], { when: { who: 'other' }, limit: 1 })], { cost: 0 }),
      Or([draw(1), st('취약', 1, E1), pw('hit', [stk(K, 1, E1)], { when: { who: 'other' }, limit: 1 })]),
      Or([srch(), draw(1), pw('hit', [stk(K, 1, E1)], { when: { who: 'other' }, limit: 1 })]),
      Or([sh(0.6), draw(1), pw('huntDown', [draw(1), stk(K, 1, 'lowEnemy')], { limit: 1 })]),
    ]), bl('draw', 'cost', [stk(K, 1, E1)])),
    // 갈래 부품 — 흠 들추기(0): 흠 1 + 다른 아군의 공격 카드 1장(찍은 적을 아군이 치게)
    card(H, 5, '흠 들추기', 0, '스킬', [stk(K, 1, E1), draw(1, { who: 'other', type: '공격' })], M([
      Or([stk(K, 2, E1), draw(1, { who: 'other', type: '공격' })]),
      Or([stk(K, 1, E1), draw(2, { who: 'other', type: '공격' })]),
      Or([stk(K, 1, E1), draw(1, { who: 'other', type: '공격' }), sh(0.4)]),
      Or([stk(K, 1, E1), draw(1, { who: 'other', type: '공격' }), st('취약', 1, E1)]),
      Or([disc(1), stk(K, 3, E1), draw(1, { who: 'other', type: '공격' })]),
    ]), bl('draw', { tags: ['보존'] }, [stk(K, 1, E1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 7. 벨라 — 버티형 · 탱커 · 활발 · 엘다인. 막아 낸 양이 「존속」(불꽃)으로 바뀌고, 다 차면 파티의 다음 카드에 불꽃을 나눈다 — 한 번은 쓰러지지 않는다(영속)
// 원작: 소설 속 주인공의 유령 · 사라질까 불안 · 저학년 도발 + 존속 + 영속(한 번 버팀) · 고학년 불꽃을 빼앗아 아군에게 나눔 · 강화 평타(직접 피해마다 주위 피해 + 회복 + 보호막)
// 장치: 「존속」(변환 — 지난 판 막아 낸 50당 +1 · 다 차면 파티 다음 카드 +50%) · 엘다인 한 단계: 영속 · 시동: u1 경계선상의 유령(그대로)
// ════════════════════════════════════════════════════════════════════
function bella(j) {
  const H = '벨라', K = '존속';
  const h = j.heroes[0];
  h.blurb = '소설 속 주인공이 현실로 걸어 나온 유령 소녀. 막아 낸 공격이 작은 불꽃 「존속」 으로 바뀌어 모이고, 다 차면 그 불꽃을 파티의 다음 한 수에 나눠 줍니다. 쓰러질 한 대를 한 번 버텨 내 자신을 증명합니다.';
  h.keyword = { name: K, desc: '벨라가 나눠 준 작은 불꽃', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.02, who: 'allies' }], onMax: { empower: 'any', ratio: 0.5, consume: true } };
  h.passives = [
    pas('다시금 타오르는 존재', 'guardSum', [stkEv(K, 0.02)]),
    pas('영속', 'lowHp', [st('끈기', 1), sh(2.4)], { when: { pct: 0.4 } }),
  ];
  setCards(j, [
    // 시동 — 경계선상의 유령: 적 전체 + 실드 + 존속
    U(H, 1, '경계선상의 유령', 1, '공격', [ddef(0.3, EA), sh(0.9), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.3, EA), sh(1.0), st('피해 감소', 2)]),
      Or([ddef(0.3, EA), sh(0.8), srch()]),
      'Hd',
    ], null),
    // 만들기 둘째 — 존재의 실드(2): 크게 막고, 존속 1개당 더, 실드 유지
    U(H, 2, '존재의 실드', 2, '스킬', [sh(2.1), per(K), sh(0.19), st('실드 유지', 1)], [
      'A', 'B',
      Or([sh(2.1), per(K), sh(0.2), stk(K, 2)]),
      Or([sh(2.0), per(K), sh(0.18), pw('guardSum', [stk(K, 1), sh(0.4)])], { power: true }),
      'Hn',
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 쓰기 — 서툰 챙김: 존속 1개당 회복, 전부 소모(다 차서 파티에 나눌지, 지금 털지)
    U(H, 3, '서툰 챙김', 1, '스킬', [heal(0.5), per(K), heal(0.24), spendAll(K)], [
      'A',
      Or([per(K, { n: 1 }), sh(0.45), spendAll(K)]),
      ['D', 'guardSum', [heal(0.2)]],
      Or([heal(0.5), per(K), heal(0.22), srch()]),
      'Hd',
    ], bl('heal', 'draw', [stk(K, 1)])),
    // 원작 자유 — 도깨비불(강화 평타): 공격을 받으면 적 전체 + 회복(턴 1회)
    card(H, 4, '도깨비불', 1, '강화', [sh(0.6), pw('hurt', [ddef(0.25, EA), heal(0.15)], { when: { guarded: true }, limit: 1 })], M([
      Or([sh(0.8), pw('hurt', [ddef(0.25, EA), heal(0.15)], { when: { guarded: true }, limit: 1 })]),
      Or([pw('hurt', [ddef(0.25, EA), heal(0.15)], { when: { guarded: true }, limit: 1 })], { cost: 0 }),
      Or([sh(0.5), stk(K, 1), pw('guardSum', [ddef(0.35, EA), stk(K, 1)])]),
      Or([sh(0.5), srch(), pw('hurt', [ddef(0.25, EA), heal(0.15)], { when: { guarded: true }, limit: 1 })]),
      Or([sh(1.2), pw('hurt', [ddef(0.3, EA), heal(0.18)], { when: { guarded: true }, limit: 1 })], { tags: ['종극'] }),
    ]), bl('guard', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 둥실둥실 한 바퀴(변환): 적 전체 + 지난 차례 막아 낸 60당 1타
    U(H, 5, '둥실둥실 한 바퀴', 1, '공격', [ddef(0.3, EA), perGuard(60), ddef(0.12), stk(K, 1)], [
      'A',
      Or([ddef(0.3, EA), perGuard(40), sh(0.3), stk(K, 2)]),
      Or([ddef(0.3, EA), stk(K, 1), pw('guardSum', [ddef(0.25, EA)])], { power: true }),
      ['C', [st('피해 감소', 1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 8. 사리 — 거드는 형 · 딜러 · 순수 · 1성. 동료가 운을 띄운 만큼(다른 아군 카드) 맞장구가 쌓이고, 받아쳐 한 방에 몰아 친다
// 원작: 맞장구 · 포즈 따라 하기 · 먼저 말을 꺼내지 않음 · 저학년 가장 먼 적에게 사슬낫 · 메죵 · 베루와 찢긴 수의 한 조각
// 장치: 「맞장구」(다른 아군 카드마다 +1 · 터짐 없음 — 1개당 주는 피해) · 시동: u4 셋으로 찢긴 수의(개전 강화, 그대로)
// ════════════════════════════════════════════════════════════════════
function sari(j) {
  const H = '사리', K = '맞장구';
  const h = j.heroes[0];
  const lk = ['연계'];
  setCards(j, [
    // 쓰기 — 장난스런 웃음: 맞장구 1개당, 전부 소모
    U(H, 1, '장난스런 웃음', 1, '공격', [dmg(1.2), per(K), dmg(0.42), spendAll(K)], [
      'A',
      ['D', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      Or([dmg(0.8, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      Or([dmg(1.1), per(K), dmg(0.4), srch()]),
      'Hx',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 만들기 — 똑같은 포즈(0): 맞장구 + 다른 아군의 카드 1장
    card(H, 2, '똑같은 포즈', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], M([
      Or([stk(K, 2), draw(1, { who: 'other' })]),
      Or([stk(K, 1), draw(2, { who: 'other' })]),
      Or([stk(K, 1), draw(1, { who: 'other' }), st('협공', 1)]),
      Or([stk(K, 1), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 })], { power: true }),
      Or([disc(1), stk(K, 3), draw(1, { who: 'other' })]),
    ]), bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
    // 갈래 부품 — 뼈 있는 맞장구(연계): 다른 사도 카드에 따라 저절로
    U(H, 3, '뼈 있는 맞장구', 1, '공격', [dmg(1.36), stk(K, 1)], [
      'A',
      Or([dmg(1.0, EA), stk(K, 1)], { tags: lk }),
      Or([dmg(1.2), stk(K, 1), draw(1, { who: 'other' })], { tags: lk }),
      ['D', 'play', [dmg(0.3)], { when: { who: 'other', type: '공격' }, limit: 1 }],
      ['Ht', '연계'],
    ], bl('power', 'draw', [stk(K, 1)]), lk),
    // 시동 · 원작 자유 — 셋으로 찢긴 수의(개전 강화): 협공 + 매 턴 맞장구
    card(H, 4, '셋으로 찢긴 수의', 1, '강화', [st('협공', 1), stk(K, 2), pw('turnStart', [stk(K, 1)])], M([
      Or([st('협공', 2), stk(K, 2), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),
      Or([stk(K, 2), pw('turnStart', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),
      Or([st('협공', 1), stk(K, 2), pw('play', [stk(K, 1)], { when: { who: 'other' }, limit: 2 })], { tags: ['개전'] }),
      Or([st('협공', 1), draw(2), pw('turnStart', [stk(K, 2)])], { tags: ['개전'] }),
      Or([st('협공', 2), draw(1), pw('turnStart', [stk(K, 1)])], { tags: [] }),
    ]), null, ['개전']),
    // 만들기 둘째 — 쿠사리가마: 피해 + 맞장구 + 다른 아군의 카드 1장
    U(H, 5, '쿠사리가마', 1, '공격', [dmg(0.9), stk(K, 1), draw(1, { who: 'other' })], [
      'A', 'B',
      Or([dmg(0.65, EA), stk(K, 1), st('협공', 1)]),
      ['D', 'turnStart', [stk(K, 1)]],
      'Hd',
    ], bl('power', 'weakSpot', [stk(K, 1)])),
  ]);
}

// ════════════════════════════════════════════════════════════════════
// 9. 셀리네 — 버티형 · 탱커 · 활발. 약 올려 끌어온 공격을 막아 낸 양이 「표정」으로 바뀌고, 다 차면 「조회수 폭발 셀카」가 손에 온다
// 원작: 도발의 유령 · 화난 표정 수집 · 엘튜버 「고스 쉐리」 업로드 · 강화 평타 자기 회복 + 도발 · 어사이드 맞으면 SP · 고학년 처치하면 보호막
// 장치: 「표정」(변환 — 지난 판 막아 낸 50당 +1 · 다 차면 「조회수 폭발 셀카」 카드) · 시동: u1 온 마음을 다해(그대로)
// ════════════════════════════════════════════════════════════════════
function seline(j) {
  const H = '셀리네', K = '표정', SELFIE = '셀리네_selfie', VIRAL = '셀리네_viral';
  const h = j.heroes[0];
  h.blurb = '남의 화난 표정을 수집하는 도발의 여왕 엘튜버 유령. 약 올려 끌어온 공격을 막아 낼수록 「표정」 이 모이고, 다 차면 조회수 폭발 셀카를 올립니다. 모은 표정은 갤러리째 들이밉니다.';
  h.keyword = { name: K, desc: '약 올려서 모은 화난 표정', carrier: 'self', cap: 5, onMax: { make: VIRAL, consume: true } };
  h.passives = [
    pas('방금 그 표정 좋았어', 'guardSum', [stkEv(K, 0.02)]),
    pas('방금 그 표정 좋았어', 'debuff', [stk(K, 1)], { limit: 1 }),
    pas('세 번째 도발', 'play', [heal(0.4), st('반격', 1)], { when: { every: 3 } }),
  ];
  setCards(j, [
    // 시동 · 원작 자유 — 온 마음을 다해(저학년): 적 전체 + 약화 + 표정
    U(H, 1, '온 마음을 다해', 1, '공격', [ddef(0.33, EA), st('약화', 1, EA), stk(K, 1)], [
      'A', 'B',
      Or([ddef(0.5), st('약화', 2, E1), stk(K, 1)]),
      Or([ddef(0.3, EA), st('약화', 1, EA), make(SELFIE, 1)]),
      'Hd',
    ], null),
    // 만들기 둘째 — 인증샷: 실드 + 표정 + 셀카
    U(H, 2, '인증샷', 1, '스킬', [sh(1.2), stk(K, 1), make(SELFIE, 1)], [
      'A', 'B',
      Or([sh(1.0), make(SELFIE, 2)]),
      Or([sh(1.2), make(SELFIE, 1), pw('guardSum', [make(SELFIE, 1)])], { power: true }),
      Or([sh(1.6), make(SELFIE, 2), disc(1)]),
    ], bl('guard', 'ap', [stk(K, 1)])),
    // 쓰기 — 화난 얼굴 갤러리: 표정 1개당, 전부 소모(지금 털지 · 다 모아 셀카를 받을지)
    U(H, 3, '화난 얼굴 갤러리', 1, '공격', [ddef(0.5), per(K), ddef(0.2), spendAll(K)], [
      'A',
      ['C', [st('기절', 1, E1)]],
      Or([ddef(0.4, EA), per(K), ddef(0.12, EA), spendAll(K)]),
      ['D', 'guardSum', [ddef(0.3)]],
      'Hd',
    ], bl('power', 'cost', [stk(K, 1)])),
    // 갈래 부품 — 사근사근 반말(강화): 공격을 다 막아 내면 표정
    card(H, 4, '사근사근 반말', 1, '강화', [sh(0.8), pw('blocked', [stk(K, 1)], { limit: 2 })], M([
      Or([sh(1.1), pw('blocked', [stk(K, 1)], { limit: 2 })]),
      Or([pw('blocked', [stk(K, 1)], { limit: 2 })], { cost: 0 }),
      Or([sh(0.7), stk(K, 1), pw('blocked', [{ k: 'reflect', ratio: 0.4 }], { limit: 2 })]),
      Or([sh(0.6), srch(), pw('blocked', [stk(K, 1)], { limit: 2 })]),
      Or([sh(1.5), pw('blocked', [stk(K, 1)], { limit: 2 })], { tags: ['종극'] }),
    ]), bl('guard', 'draw', [stk(K, 1)])),
    // 만들기 — 어그로 썸네일(0): 표정 + 셀카 + 드로우
    card(H, 5, '어그로 썸네일', 0, '스킬', [stk(K, 1), make(SELFIE, 1), draw(1)], M([
      Or([stk(K, 2), make(SELFIE, 1), draw(1)]),
      Or([stk(K, 1), make(SELFIE, 2), draw(1)]),
      Or([stk(K, 2), make(SELFIE, 1), sh(1.0)]),
      Or([stk(K, 1), make(SELFIE, 1), pw('turnStart', [make(SELFIE, 1)])], { power: true }),
      Or([disc(1), stk(K, 2), make(SELFIE, 2)]),
    ]), bl('draw', { tags: ['보존'] }, [stk(K, 1)])),
  ]);
}

// ── 돌리기 ──
const JOBS = [
  ['유령/레테', lethe], ['유령/림', rim], ['유령/림_혼돈', rimChaos], ['유령/메죵', mezyong], ['유령/바롱', barong],
  ['유령/베루', beru], ['유령/벨라', bella], ['유령/사리', sari], ['유령/셀리네', seline],
];
const TUNE = { '유령/레테': 1.2, '유령/메죵': 1.3, '유령/바롱': 1.35, '유령/사리': 1.15, '유령/셀리네': 0.9, '유령/베루': 0.8, '유령/벨라': 0.8 };   // 벨라 0.85 — 118명 최종 측정(엘다인 상한 넘음)
L.run18(JOBS, TUNE, new URL('./boost_유령A_18.json', import.meta.url));
