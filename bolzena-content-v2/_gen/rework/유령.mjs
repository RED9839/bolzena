// 사도 리워크 2단계 — 유령 16명(키샤는 1단계 rework.mjs). 2026-10-07
// 3단계(카제나 자료 보강 — 지침 §12) 2026-10-08: 신탁 갈래 정리(얕은 갈래 ≤2 · 재설계 · 대가 · 강화화/서치 BEST) ·
//   ④ 완성형 1코 마무리 · 개전 강화 시동(메죵 · 사리 · 셰이디(역전) · 앨리스) · 생성 카드(메죵 · 셀리네 · 셰이디(역전) · 스피키 · 앨리스 + 시온) ·
//   기본 카드 연료(레테 · 림(혼돈) · 사리 · 스피키(메이드)) · 연계 딜러(사리 · 베루) · 축복 손질
// 지침: _measure/사도_리워크_지침.md · 보고: _measure/리워크_유령.md · 공용 부품: lib.mjs
// node _gen/rework/유령.mjs [사도]  → heroes/유령/<사도>.json 덮어쓰기(백업에서 읽음)
import { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifAll, ifBroken, inspire, power, rule, later, O, B, card, starter, run } from './lib.mjs';

// ── 이 스크립트만의 부품 ──
const rush = (v, t) => (t ? { k: 'rushDown', v, target: t } : { k: 'rushDown', v });
const gauge = v => ({ k: 'gauge', v });
const discard = v => ({ k: 'discard', v });
const extra = (r, o = {}) => ({ k: 'extra', ratio: r, ...o });
const spend = (id, v) => ({ k: 'spend', id, v });
const perEach = id => ({ k: 'perStack', id, each: true });
const perDebuff = { k: 'perDebuff' };
const c1 = { k: 'ifChoice', n: 1 }, c2 = { k: 'ifChoice', n: 2 };
// 기본 카드 연료(§12-3): 손의 기본 카드 소멸 · 고유 카드로 바꾸기
const exileBasic = (n = 1) => ({ k: 'exileFrom', from: 'hand', n, basic: true });
const turnBasic = (into, n = 1) => ({ k: 'transform', id: into, from: 'hand', basic: true, n });
// 생성물 — 손의 그 카드 전부 소멸 · 지정 서치
const exileAll = id => ({ k: 'exileFrom', from: 'hand', all: true, tag: id });
const mySearch = { who: 'self', unique: true };
const P = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: { per: o.per || 'turn', n: o.limit } } : {}), fx });
// 고유 카드 넷을 갈아 끼운다(시작 카드 · 생성 카드는 그대로)
const put = (j, cards) => { j.cards = [...j.cards.filter(c => !c.unique), ...cards]; };
// 새 생성 카드(token)를 넣는다(백업에 없으면)
const token = (j, def) => { j.cards = j.cards.filter(c => c.id !== def.id); j.cards.push({ hero: j.heroes[0].id, token: true, ...def }); };
// 고학년의 고유 효과 참조 이름만 바꾼다(fx 순서 · 내용 그대로)
const renameUlt = (h, from, to) => { for (const f of h.ult.fx) if ((f.k === 'stack' || f.k === 'spend') && f.id === from) f.id = to; };
const KAI = { tags: ['개전'] };

// ════════════════════════════════════════════════════════════════════
// 1. 레테 — 탱커 · 냉정. 맞은 기억을 「지운 기억」으로 쌓았다가, 회복(뉴럴 링크)으로 쓰거나 넷에 저절로 리셋
//    3단계: 기본 카드를 지워 기억으로(기억 소거) · ④ 병상 곁 = 기억을 세는 1코 마무리(옛 강화 엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function lethe(j) {
  const H = '레테', K = '지운 기억';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '레이저로 잠깐 지워 둔 아팠던 기억', carrier: 'self', cap: 4,
    rules: [{ name: '으히히 리셋', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), heal(0.55), st('피해 감소', 1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('아팠던 기억', 'hurt', [stk(K, 1)], { limit: 2 }),
    P('레이저 테러범', 'play', [extra(0.15, { hits: 4, target: E1 })], { when: { type: '공격', every: 2 } }),
  ];
  put(j, [
    // u1 터뜨리기(방어) — 원작 저학년: 맞았던 기억을 잊고 크게 회복
    card(H, 1, '뉴럴 링크 스따트', 1, '스킬', [sh(1.0), per(K), heal(0.3), spendAll(K)], [
      O('완전 동기화', [sh(1.25), per(K), heal(0.38), spendAll(K)]),
      O('간이 접속', [sh(0.7), per(K), heal(0.3), spendAll(K)], { cost: 0 }),
      O('기억 소거', [exileBasic(1), stk(K, 2), per(K), heal(0.35)]),
      O('잊지 않을게', [sh(1.0), per(K), heal(0.3)], { tags: ['보존'] }),
      O('흑역사 차단', [sh(0.8), power(rule('hurt', [stk(K, 1)], { limit: 1 }))], { power: true }),
    ], [B('방화벽', 'guard'), B('빠른 접속', 'cost'), B('저장된 기억', [stk(K, 1)])]),
    // u2 굴리기 — 레이저 포인터 4연타(제 눈에도 한 번)
    card(H, 2, '포인터 4연타', 1, '공격', [hits(4, 0.28, E1), stk(K, 1)], [
      O('빨간 점 고정', [hits(4, 0.36, E1), stk(K, 1)]),
      O('레이저 파티', [hits(6, 0.22, ER), stk(K, 1)]),
      O('기억 지우개', [hits(4, 0.25, E1), per(K), dmg(0.25, E1), spendAll(K)]),
      O('포인터 장전', [hits(4, 0.26, E1), stk(K, 1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))], { power: true }),
      O('누구야아아?', [hits(4, 0.28, E1), stk(K, 1), ifStack(K, 3), st('약화', 2, E1)]),
    ], [B('매일 연습한 폼', 'power'), B('눈부심', 'frost'), B('빠른 손목', 'draw')]),
    // u3 열기(시동) — 눈앞의 빨간 점: 적 눈가림(약화) + 기억 하나
    card(H, 3, '눈앞의 레이저', 0, '스킬', [st('약화', 1, E1), stk(K, 1), draw(1)], [
      O('빨간 점을 봐', [st('약화', 2, E1), stk(K, 1), draw(1)]),
      O('번쩍', [st('약화', 1, EA), stk(K, 1), draw(1)]),
      O('환자복 주머니', [stk(K, 1), draw(2, { who: 'self' })]),
      O('헛기억', [exileBasic(1), stk(K, 2), draw(1)]),
      O('으히히', [st('약화', 1, E1), draw(1), inspire, stk(K, 3)]),
    ], [B('렌즈 닦기', [stk(K, 1)]), B('주운 포인터', 'draw'), B('눈앞이 하얘짐', [st('약화', 1, E1)])]),
    // u4 완성형(④ 1코 마무리) — 헤일리 곁을 지키는 간호사: 결정화 + 기억 1개당 실드(쓰지 않고 센다)
    card(H, 4, '병상 곁', 1, '스킬', [st('불굴', 1), per(K), sh(0.35)], [
      O('정성 간호', [st('불굴', 1), per(K), sh(0.45)]),
      O('밤샘 간호', [st('결정화', 1), st('불굴', 1), power(rule('turnStart', [stk(K, 1), sh(0.3)]))], { power: true }),
      O('헤일리 곁에서', [st('불굴', 1), per(K), heal(0.45)]),
      O('남겨 둔 기억', [st('불굴', 2), per(K), sh(0.5), spendAll(K)]),
      O('잠깐 들르기', [st('불굴', 1), per(K), sh(0.3)], { cost: 0 }),
    ], [B('링거 거치대', 'heal'), B('환자복', { tags: ['보존'] }), B('쓴 약', [{ k: 'cleanse', v: 1 }])]),
  ]);
  starter(j, '레테_u3');
}

// ════════════════════════════════════════════════════════════════════
// 2. 림 — 딜러 · 우울. 낫이 남긴 「낫 자국」(적 표식)이 턴 끝마다 쓰라리고, 다 거두면 한 방
//    3단계: ④ 균형의 저울 = 적마다 제 자국을 세는 1코 광역 마무리(옛 「매 턴 자국」 엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function rim(j) {
  const H = '림', K = '낫 자국';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '낫에 베여 쓰라린 자리', carrier: 'enemy', cap: 4, per: [{ stat: 'dot', ratio: 0.15 }] };
  delete h.keywords;
  h.passives = [
    P('쓰라림', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }),
    P('썰렁한 아재개그', 'play', [rush(1, EA)], { when: { type: '스킬' }, limit: 1 }),
  ];
  put(j, [
    // u1 열기(시동) — 원작 저학년 스크래치 사이드: 광역 참격 · 쓰라림 · 회복
    card(H, 1, '스크래치 사이드', 1, '공격', [dmg(0.6, EA), stk(K, 1, EA), heal(0.4)], [
      O('어둠이 내린 참격', [dmg(0.78, EA), stk(K, 1, EA), heal(0.4)]),
      O('회복해버림', [dmg(0.6, EA), stk(K, 1, EA), heal(0.9)]),
      O('기역 부르기', [dmg(0.55, EA), stk(K, 1, EA), draw(1, mySearch)]),
      O('그림 스크래치', [dmg(1.0, EA), stk(K, 2, EA), ifStack(K, 3), heal(1.0)], { cost: 2 }),
      O('쓰라린 밤', [dmg(0.55, EA), stk(K, 1, EA), power(rule('turnEnd', [stk(K, 1, EA)]))], { power: true }),
    ], [B('명상', 'heal'), B('다기 세트', 'draw'), B('어둠 한 자락', 'power')]),
    // u2 굴리기 — 낫 「기역」 던지기
    card(H, 2, '날아가는 기역', 1, '공격', [dmg(1.0), stk(K, 2, E1)], [
      O('기역 회전', [dmg(1.3), stk(K, 2, E1)]),
      O('돌아오는 기역', [dmg(0.9), stk(K, 2, E1), ifStack(K, 4), dmg(0.8)]),
      O('기역에게 개그', [dmg(1.05), rush(1, E1), draw(1, { who: 'self', type: '스킬' })]),
      O('혼자 남은 탑', [discard(1), dmg(1.4), stk(K, 3, E1)]),
      O('라크로스 림크로스', [dmg(1.5, EA), stk(K, 2, EA)], { cost: 2 }),
    ], [B('날 선 기역', 'power'), B('쓰라린 상처', 'frost'), B('낫 손질', 'cost')]),
    // u3 터뜨리기 — 자국을 다 거두는 한 방(쥐면 턴 끝 쓰라림, 거두면 지금)
    card(H, 3, '기역의 추수', 1, '공격', [dmg(0.6), per(K), dmg(0.3), spendAll(K)], [
      O('질서의 추수', [dmg(0.75), per(K), dmg(0.36), spendAll(K)]),
      O('남겨 둔 자국', [dmg(0.6), per(K), dmg(0.3)]),
      O('균형 회복', [dmg(0.6), per(K), dmg(0.3), ifKill, stk(K, 2, EA)]),
      O('수확의 낫', [per(K), dmg(0.4), spendAll(K), power(rule('turnEnd', [stk(K, 1, EA)]))], { power: true }),
      O('마지막 추수', [dmg(1.4), per(K), dmg(0.65), spendAll(K)], { tags: ['소멸'] }),
    ], [B('예리한 날', 'weakSpot'), B('외딴 탑', 'ap'), B('친구 기다리기', [stk(K, 1, E1)])]),
    // u4 완성형(④ 1코 마무리) — 균형의 저울: 적마다 제 자국 1개당 한 번 더(자국은 남긴다)
    card(H, 4, '균형의 저울', 1, '공격', [dmg(0.3, EA), perEach(K), dmg(0.15, EA)], [
      O('흔들리지 않는 저울', [dmg(0.4, EA), perEach(K), dmg(0.18, EA)]),
      O('균형 맞추기', [stk(K, 1, EA), dmg(0.35, EA), power(rule('turnStart', [stk(K, 1, EA)]))], { power: true }),
      O('명상 시간', [dmg(0.4, EA), stk(K, 1, EA), heal(0.6)]),
      O('파티 호스트', [dmg(0.25, EA), perEach(K), dmg(0.12, EA)], { cost: 0 }),
      O('혼돈의 반대편', [discard(1), dmg(0.4, EA), perEach(K), dmg(0.2, EA)]),
    ], [B('아모르 파티', 'atkUp'), B('추위 안 탐', 'defUp'), B('저울추', [stk(K, 1, EA)])]),
  ]);
  starter(j, '림_u1');
  // 애착 장비(전설) — 범용 몫(다 기운 적 거두기)은 그대로, 애착 몫은 전투 시작에 자국
  for (const e of j.equips || []) e.affinityEffect = [P('저울추', 'fightStart', [stk(K, 1, EA)])];
}

// ════════════════════════════════════════════════════════════════════
// 3. 림(혼돈) — 딜러 · 광기(원작 방식 고학년 — 변신 「차원 너머의 낫」 유지). 「혼돈」이 짙을수록 세고, 다섯이면 터지며 삐끗
//    3단계: 혼돈이 기본 카드를 집어삼켜 「차원 베기」로(기본 카드 연료)
// ════════════════════════════════════════════════════════════════════
function rimChaos(j) {
  const H = '림_혼돈', K = '혼돈';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '억지로 떠안은 어설픈 혼돈', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.06 }],
    rules: [{ name: '삐끗', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), dmg(1.0, EA), st('취약', 1, 'party')] }],
  };
  delete h.keywords;
  h.passives = [
    P('어엿한 혼돈', 'play', [stk(K, 1)], { when: { type: '공격' }, conds: [{ c: 'foes', n: 2 }], limit: 2 }),
    P('잘못 찾아온 손님', 'hurt', [gauge(8)], { limit: 1 }),
  ];
  put(j, [
    // u1 열기(시동) — 원작 저학년: 적진에 순간이동해 광역, 맞힌 만큼 혼돈
    card(H, 1, '혼돈에 이끌림', 1, '공격', [dmg(0.65, EA), stk(K, 2)], [
      O('적진 한가운데', [dmg(0.85, EA), stk(K, 2)]),
      O('차원문 열기', [dmg(0.55, EA), stk(K, 2), draw(1, { who: 'self', type: '공격' })]),
      O('혼돈 전염', [dmg(0.5, EA), per(K), dmg(0.12, EA), turnBasic('림_혼돈_u2')]),
      O('카오틱 에이프런', [discard(1), dmg(0.85, EA), stk(K, 3)]),
      O('혼돈 마담의 연회', [dmg(0.55, EA), stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
    ], [B('재잘거리는 림', 'draw'), B('소천사의 대변인', 'power'), B('야시장 꼬치', [stk(K, 1)])]),
    // u2 굴리기 — 강화 평타 차원 베기(혼돈이 짙으면 더)
    card(H, 2, '차원 베기', 1, '공격', [dmg(1.0), stk(K, 1), ifStack(K, 3), dmg(0.5)], [
      O('차원 너머 베기', [dmg(1.3), stk(K, 1), ifStack(K, 3), dmg(0.6)]),
      O('강화 공격', [dmg(1.0), stk(K, 1), ifStack(K, 3), dmg(0.5)], { tags: ['분쇄'] }),
      O('낫의 잔상', [dmg(0.9), per(K), dmg(0.2), stk(K, 1)]),
      O('받아쓰기에서 틀린 낫', [dmg(1.2), stk(K, 1), draw(1, mySearch)]),
      O('꿀밤 맞기 직전', [dmg(1.4), per(K), dmg(0.15), spendAll(K)]),
    ], [B('날카로운 혼돈', 'power'), B('차원의 틈', 'weakSpot'), B('어설픈 위협', [stk(K, 1)])]),
    // u3 굴리기(약화 담당) — 말 많은 악당의 겁주기
    card(H, 3, '무서워하십시오!', 1, '스킬', [st('약화', 2, E1), stk(K, 2)], [
      O('정말 무서워하십시오!', [st('약화', 3, E1), stk(K, 2)]),
      O('모두 무서워하십시오!', [st('약화', 1, EA), stk(K, 2), dmg(0.4, EA)]),
      O('말 많은 악당', [st('약화', 2, E1), stk(K, 2), draw(1, { who: 'self', type: '공격' })]),
      O('큰소리', [st('약화', 2, E1), stk(K, 1)], { cost: 0 }),
      O('셰이디를 찾음', [st('약화', 2, E1), stk(K, 2), ifAll, sh(1.1)]),
    ], [B('과장된 존댓말', 'draw'), B('으스스한 그림자', 'guard'), B('샌드백 신세', 'cost')]),
    // u4 터뜨리기(④ 1코 마무리) — 교주의 꿀밤: 혼돈을 털어 광역(넘쳐 삐끗하기 전에)
    card(H, 4, '꿀밤', 1, '공격', [dmg(0.45, EA), per(K), dmg(0.18, EA), spendAll(K)], [
      O('교주의 꿀밤', [dmg(0.55, EA), per(K), dmg(0.22, EA), spendAll(K)]),
      O('정신 차림', [per(K), dmg(0.2, EA), spendAll(K), draw(2)]),
      O('한 명만 꿀밤', [dmg(0.9), per(K), dmg(0.35), spendAll(K)]),
      O('정신 교육', [dmg(0.5, EA), st('약화', 1, EA), stk(K, 1)]),
      O('얻어맞은 기억', [dmg(0.4, EA), per(K), dmg(0.15, EA), power(rule('spend', [sh(0.5)], { when: { id: K }, limit: 1 }))], { power: true }),
    ], [B('딱밤', 'power'), B('원래대로', 'ap'), B('혼돈 털기', [sh(0.4)])]),
  ]);
  starter(j, '림_혼돈_u1');
}

// ════════════════════════════════════════════════════════════════════
// 4. 메죵 — 딜러 · 광기. 턴마다 「입주」가 늘어 세지고(눌러앉기), 집문서 한 방에 몽땅 건다
//    3단계: 생성 카드 「집문서」(부 장치 — 내면 입주 + 드로우, 쥐면 ④가 센다) · 시동 = 개전 강화 「눌러앉기」(매 턴 집문서)
// ════════════════════════════════════════════════════════════════════
function maison(j) {
  const H = '메죵', K = '입주', DEED = '메죵_deed';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '발 닿는 곳마다 눌러앉은 자리', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }] };
  delete h.keywords;
  h.passives = [
    P('여기가 내 집', 'turnEnd', [stk(K, 1)]),
    P('새 집 찾기', 'kill', [make(DEED, 1), stk(K, 1)], { when: { mine: true }, limit: 1 }),
  ];
  token(j, { id: DEED, name: '집문서', cost: 0, type: '스킬', tags: ['소멸'], fx: [dmg(0.7, ER), stk(K, 1), draw(1)], blurb: '어디서 났는지 모를 남의 집문서. 메죵 이름이 연필로 적혀 있습니다.' });
  put(j, [
    // u1 굴리기 — 원작 저학년 수리검 셋
    card(H, 1, '수리검 날아갑니다!', 1, '공격', [hits(3, 0.4, E1), stk(K, 1)], [
      O('수리검 한 움큼', [hits(3, 0.52, E1), stk(K, 1)]),
      O('흩날리는 수리검', [discard(1), hits(5, 0.42, ER), stk(K, 2)]),
      O('집들이 선물', [hits(3, 0.4, E1), make(DEED, 1)]),
      O('오래 머문 손목', [hits(3, 0.4, E1), stk(K, 1), ifStack(K, 4), hits(2, 0.3, E1)]),
      O('수리검 세트', [hits(3, 0.38, E1), stk(K, 1), power(rule('play', [extra(0.3)], { when: { type: '공격' }, limit: 1 }))], { power: true }),
    ], [B('붕대 감은 손', 'power'), B('평안한 손목', 'draw'), B('어서 오세요~', [make(DEED, 1)])]),
    // u2 터뜨리기 — 집문서 내놔: 쌓은 자리를 몽땅 건다
    card(H, 2, '집문서 내놔', 1, '공격', [dmg(1.0), per(K), dmg(0.3), spendAll(K)], [
      O('집문서 당장 내놔', [dmg(1.2), per(K), dmg(0.36), spendAll(K)]),
      O('부동산 뒤엎기', [dmg(0.7, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      O('집주인 행세', [dmg(0.9), per(K), dmg(0.25), ifKill, make(DEED, 2)]),
      O('집문서 수집', [dmg(0.9), per(K), dmg(0.3), power(rule('turnEnd', [stk(K, 1)]))], { power: true }),
      O('이삿짐 정리', [discard(1), dmg(1.3), per(K), dmg(0.4)]),
    ], [B('도장 쾅', 'power'), B('등기부 등본', 'weakSpot'), B('이사 준비', 'ap')]),
    // u3 열기(시동 · 개전 강화) — 눌러앉기: 첫 턴부터 매 턴 집문서 한 장
    card(H, 3, '눌러앉기', 0, '강화', [make(DEED, 1), power(rule('turnStart', [make(DEED, 1)]))], [
      O('깊숙이 눌러앉기', [make(DEED, 2), power(rule('turnStart', [make(DEED, 1)]))], KAI),
      O('평안한 집', [sh(1.0), stk(K, 2), power(rule('turnStart', [make(DEED, 1)]))], KAI),
      O('셰이디의 아공간', [make(DEED, 1), draw(1, { who: 'self', type: '공격' }), power(rule('turnStart', [make(DEED, 1)]))], KAI),
      O('먼저 맞이하기', [make(DEED, 3), power(rule('turnStart', [make(DEED, 1)]))]),
      O('느긋한 하루', [stk(K, 3), power(rule('turnStart', [make(DEED, 1), stk(K, 1)]))], KAI),
    ], [B('편한 자리', 'draw'), B('집 찾아다니기', [stk(K, 1)]), B('집 보러 오기', 'cost')], KAI),
    // u4 완성형(④ 1코 마무리) — 제가 주인인 양: 손의 집문서를 몽땅 내민다(옛 취약 · 매 턴 입주 엔진은 D 갈래)
    card(H, 4, '제가 주인인 양', 1, '공격', [dmg(0.6), perTag(DEED), dmg(0.45), exileAll(DEED)], [
      O('진짜 주인처럼', [dmg(0.75), perTag(DEED), dmg(0.55), exileAll(DEED)]),
      O('겁줘 보세요', [dmg(0.9), st('취약', 2, E1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('집문서 쥐고 있기', [dmg(0.7), perTag(DEED), dmg(0.4)]),
      O('집 넓히기', [per(K), dmg(0.35), perTag(DEED), dmg(0.35)]),
      O('놀라지 않음', [dmg(0.5), perTag(DEED), dmg(0.4), exileAll(DEED)], { cost: 0 }),
    ], [B('생기 없는 눈', 'atkUp'), B('음침한 웃음', [make(DEED, 1)]), B('새 안식처', 'cost')]),
  ]);
  starter(j, '메죵_u3');
}

// ════════════════════════════════════════════════════════════════════
// 5. 바롱 — 딜러 · 냉정. 적에게 「뒷담」을 쌓아 못이 더 날아가고, 셋이면 소문이 적 전체로 터진다
//    3단계: ④ 토끼 인형 꿰매기 = 적마다 제 뒷담을 세는 1코 광역 마무리(옛 협공 엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function barong(j) {
  const H = '바롱', K = '뒷담';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '인형 속 주인님이 퍼뜨린 거짓말', carrier: 'enemy', cap: 3, per: [{ stat: 'taken', v: 0.12 }],
    rules: [{ name: '소문 확산', when: { on: 'stackReach', id: K, n: 3 }, limit: { per: 'turn', n: 1 }, fx: [spendAll(K), dmg(0.6, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    P('저주받은 못질', 'play', [extra(0.35, { hits: 2, target: E1 })], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }], limit: 2 }),
    P('주인님의 시간', 'fightStart', [sh(1.0)]),
  ];
  put(j, [
    // u1 열기(시동) — 원작 저학년 뒷담까기 인형
    card(H, 1, '뒷담까기 인형', 1, '공격', [dmg(0.9), stk(K, 1, E1)], [
      O('인형의 속삭임', [dmg(1.15), stk(K, 1, E1)]),
      O('단추 안대', [dmg(0.9), stk(K, 1, E1)], { tags: ['약점 공격'] }),
      O('주인님 탓', [dmg(0.8), stk(K, 1, E1), draw(1, { who: 'self', type: '공격' })]),
      O('인형 뒤에 숨기', [stk(K, 2, E1), sh(1.5)]),
      O('새로운 큰손', [dmg(1.5), stk(K, 2, E1), st('협공', 1)], { cost: 2 }),
    ], [B('꿰맨 토끼', 'power'), B('초점 없는 눈', 'frost'), B('웅얼웅얼', [stk(K, 1, E1)])]),
    // u2 굴리기(약화 담당) — 이간질
    card(H, 2, '사람 사이 갈라놓기', 1, '스킬', [stk(K, 2, E1), st('약화', 1, E1), draw(1)], [
      O('철저한 이간질', [stk(K, 2, E1), st('약화', 2, E1), draw(1)]),
      O('허언증', [discard(1), stk(K, 3, E1), draw(2)]),
      O('편 가르기', [stk(K, 2, E1), st('약화', 1, E1), dmg(0.5, EA)]),
      O('엇갈린 말', [stk(K, 2, E1), st('약화', 1, E1)], { cost: 0 }),
      O('거짓말 장부', [stk(K, 2, E1), draw(1), power(rule('turnStart', [stk(K, 1, E1)]))], { power: true }),
    ], [B('혀 꼬임', 'draw'), B('나쁜 소문', [st('약화', 1, E1)]), B('빠른 입', 'cost')]),
    // u3 터뜨리기 — 뒷담 1개당 못 한 대 더, 다 쓴다(소문 확산과 맞바꿈)
    card(H, 3, '못 날리기', 1, '공격', [hits(2, 0.45, E1), per(K), dmg(0.35, E1), spendAll(K)], [
      O('굵은 못', [hits(2, 0.55, E1), per(K), dmg(0.42, E1), spendAll(K)]),
      O('못 박아 두기', [hits(2, 0.5, E1), per(K), dmg(0.35, E1)]),
      O('못 비', [hits(4, 0.32, ER), per(K), dmg(0.35, ER), spendAll(K)]),
      O('저주 폭발', [hits(3, 0.6, E1), per(K), dmg(0.5, E1)], { cost: 2, tags: ['분쇄'] }),
      O('눈속임 못질', [hits(2, 0.45, E1), ifStack(K, 2), per(K), dmg(0.4, E1)]),
    ], [B('녹슨 못', 'weakSpot'), B('으흐흐 못질', 'power'), B('실리콘은 못 지나감', 'draw')]),
    // u4 완성형(④ 1코 마무리) — 토끼 인형 꿰매기: 적마다 제 뒷담 1개당 한 땀 더(뒷담은 남긴다)
    card(H, 4, '토끼 인형 꿰매기', 1, '공격', [dmg(0.35, EA), perEach(K), dmg(0.2, EA)], [
      O('정성껏 꿰매기', [dmg(0.45, EA), perEach(K), dmg(0.24, EA)]),
      O('사랑받는 유령', [dmg(0.4, EA), st('협공', 1), power(rule('play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 1 }))], { power: true }),
      O('바늘 한 땀', [dmg(0.3, EA), perEach(K), dmg(0.16, EA)], { cost: 0 }),
      O('주식 대주주', [st('협공', 2), stk(K, 1, EA)]),
      O('인형 대수선', [discard(1), dmg(0.45, EA), perEach(K), dmg(0.28, EA)]),
    ], [B('단추 눈', 'atkUp'), B('토끼 귀', [sh(0.4)]), B('실밥', 'cost')]),
  ]);
  starter(j, '바롱_u1');
}

// ════════════════════════════════════════════════════════════════════
// 6. 베루 — 딜러 · 우울. 흠 하나를 물고 늘어진다 — 적에게 「흠」을 몰아 파티가 치게 하고, 다 물어뜯어 한 방
//    3단계(연계 딜러 — 채우기): 동료가 공격하면 흠을 짚어 준다(패시브) · ④ 메죵 등 뒤 = 적마다 흠을 세는 1코 광역 마무리
// ════════════════════════════════════════════════════════════════════
function veroo(j) {
  const H = '베루', K = '흠';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '베루가 짚어 낸 적의 결점', carrier: 'enemy', cap: 4, weakens: true, per: [{ stat: 'taken', v: 0.08 }] };
  delete h.keywords;
  h.passives = [
    P('흠잡기', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }),
    P('등 뒤에서 짚기', 'play', [stk(K, 1, E1)], { when: { who: 'other', type: '공격' }, limit: 1 }),
  ];
  put(j, [
    // u1 열기(시동) — 원작 저학년 도끼 셋
    card(H, 1, '도끼 날아가요~', 1, '공격', [hits(3, 0.38, E1), stk(K, 1, E1)], [
      O('도끼 셋 명중', [hits(3, 0.48, E1), stk(K, 1, E1)]),
      O('이히히 도끼', [hits(3, 0.33, E1), stk(K, 1, E1)], { cost: 0 }),
      O('흩어지는 도끼', [hits(3, 0.35, ER), draw(1, { who: 'other', type: '공격' })]),
      O('익살스러운 표정', [hits(3, 0.38, E1), stk(K, 1, E1), ifStack(K, 3), dmg(0.6, E1)]),
      O('베-', [hits(3, 0.35, E1), stk(K, 1, E1), power(rule('play', [stk(K, 1, E1)], { when: { who: 'other', type: '공격' }, limit: 1 }))], { power: true }),
    ], [B('날 선 도끼', 'power'), B('허점 발견', 'weakSpot'), B('이히히', [stk(K, 1, E1)])]),
    // u2 터뜨리기 — 흠 하나 물고: 흠을 다 물어뜯는다
    card(H, 2, '흠 하나 물고', 1, '공격', [dmg(0.8), per(K), dmg(0.3), spendAll(K)], [
      O('물고 늘어지기', [dmg(0.95), per(K), dmg(0.38), spendAll(K)]),
      O('계속 물고', [dmg(0.85), per(K), dmg(0.3)]),
      O('먼저 지적', [dmg(0.8), per(K), dmg(0.3), spendAll(K)], { tags: ['약점 공격'] }),
      O('끈질긴 기억', [dmg(0.8), per(K), dmg(0.3), power(rule('play', [stk(K, 1, E1)], { when: { who: 'other', type: '공격' }, limit: 1 }))], { power: true }),
      O('PC방의 기억', [dmg(0.8), per(K), dmg(0.3), ifKill, draw(1)]),
    ], [B('끈질긴 이빨', 'power'), B('결점 노트', 'draw'), B('들통난 흠', [st('취약', 1, E1)])]),
    // u3 굴리기(취약 담당) — 친구 계약서
    card(H, 3, '친구 계약서', 1, '스킬', [stk(K, 2, E1), st('취약', 1, E1), draw(1)], [
      O('3년짜리 계약', [stk(K, 3, E1), st('취약', 1, E1), draw(1)]),
      O('계약서 던지기', [stk(K, 2, E1), st('취약', 1, E1), dmg(0.6)]),
      O('도장 찍기 전에', [stk(K, 3, E1), draw(2)]),
      O('친구 놀이', [stk(K, 2, E1), st('취약', 1, E1), draw(2, { who: 'other' })]),
      O('집문서 담보', [discard(1), stk(K, 4, E1), dmg(0.6)]),
    ], [B('깨알 조항', 'draw'), B('서명 강요', [stk(K, 1, E1)]), B('빠른 도장', 'cost')]),
    // u4 완성형(④ 1코 마무리) — 메죵 등 뒤: 적마다 제 흠 1개당 한 번 더(옛 「동료 공격마다 흠」 엔진은 D 갈래)
    card(H, 4, '메죵 등 뒤', 1, '공격', [dmg(0.3, EA), perEach(K), dmg(0.18, EA)], [
      O('메죵 등에 착', [dmg(0.4, EA), perEach(K), dmg(0.22, EA)]),
      O('외강내유', [dmg(0.3, EA), st('피해 감소', 1), power(rule('play', [stk(K, 1, E1)], { when: { who: 'other', type: '공격' }, limit: 1 }), rule('hurt', [stk(K, 1, E1)], { limit: 1 }))], { power: true }),
      O('걱정한 게 아니라구', [dmg(0.6, EA), stk(K, 1, EA), st('피해 감소', 2)]),
      O('등 뒤에서 베-', [dmg(0.25, EA), perEach(K), dmg(0.15, EA)], { cost: 0 }),
      O('놀릴 거리 모음', [discard(1), dmg(0.4, EA), perEach(K), dmg(0.22, EA)]),
    ], [B('메죵의 그림자', 'defUp'), B('울음 참기', [sh(0.4)]), B('작은 등', [stk(K, 1, EA)])]),
  ]);
  starter(j, '베루_u1');
}

// ════════════════════════════════════════════════════════════════════
// 7. 벨라 — 탱커 · 활발 · 엘다인. 맞을수록 「존속」 불꽃이 커져 파티가 세지고, 다섯이면 존재의 불꽃(광역 + 회복)
// ════════════════════════════════════════════════════════════════════
function vela(j) {
  const H = '벨라', K = '존속';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '벨라가 나눠 준 작은 불꽃', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.02, who: 'allies' }],
    rules: [{ name: '존재의 불꽃', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), ddef(0.45, EA), heal(0.6)] }],
  };
  delete h.keywords;
  h.passives = [
    P('다시금 타오르는 존재', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
    P('영속', 'lowHp', [st('끈기', 1), sh(2.4)], { when: { pct: 0.4 } }),
  ];
  put(j, [
    // u1 열기(시동) — 원작 저학년: 불꽃길 광역 + 존속 + 버팀
    card(H, 1, '경계선상의 유령', 1, '공격', [ddef(0.32, EA), stk(K, 2), st('피해 감소', 1)], [
      O('불꽃길', [ddef(0.42, EA), stk(K, 2), st('피해 감소', 1)]),
      O('존재의 증명', [ddef(0.35, EA), stk(K, 3), sh(0.5)]),
      O('도발의 불꽃', [ddef(0.32, EA), stk(K, 2), st('반격', 2)]),
      O('불꽃 다 태우기', [ddef(0.45, EA), per(K), ddef(0.14, EA), spendAll(K)]),
      O('질문 폭격', [ddef(0.32, EA), stk(K, 2), draw(1, { who: 'other' })]),
    ], [B('오드아이', 'power'), B('존재감 표출', 'draw'), B('작은 불씨', [stk(K, 1)])]),
    // u2 버티기(2코 하나) — 존재의 보호막
    card(H, 2, '존재의 실드', 2, '스킬', [sh(2.2), per(K), sh(0.2), st('실드 유지', 1)], [
      O('또렷한 존재감', [sh(2.6), per(K), sh(0.25), st('실드 유지', 1)]),
      O('작은 존재감', [sh(1.2), per(K), sh(0.15), st('실드 유지', 1)], { cost: 1 }),
      O('불꽃 장막', [sh(2.4), per(K), sh(0.4), spendAll(K)]),
      O('잊히지 않는 존재', [sh(2.0), per(K), sh(0.2), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('위기의 존재감', [sh(2.4), per(K), sh(0.22), ifWounded, heal(1.6)]),
    ], [B('단단한 랜턴', 'guard'), B('다크넷 지식', 'draw'), B('촛대', 'cost')]),
    // u3 터뜨리기(회복) — 서툰 챙김: 불꽃을 나눠 회복
    card(H, 3, '서툰 챙김', 1, '스킬', [heal(0.6), per(K), heal(0.25), spendAll(K)], [
      O('정성 어린 챙김', [heal(0.75), per(K), heal(0.3), spendAll(K)]),
      O('나눠 준 불꽃', [heal(0.6), per(K), sh(0.35), spendAll(K)]),
      O('조금만 챙김', [heal(0.65), per(K), heal(0.25)]),
      O('해파리 친구 부르기', [heal(0.6), per(K), heal(0.22), draw(1, mySearch)]),
      O('해파리 친구', [heal(0.6), per(K), heal(0.25), ifWounded, heal(0.9)]),
    ], [B('따뜻한 손', 'heal'), B('서툰 손길', 'ap'), B('불씨 나눔', [stk(K, 1)])]),
    // u4 굴리기(④ 1코 — 불꽃 겹을 세는 도깨비불)
    card(H, 4, '도깨비불', 1, '공격', [ddef(0.55), per(K), ddef(0.12), stk(K, 1)], [
      O('푸른 도깨비불', [ddef(0.7), per(K), ddef(0.14), stk(K, 1)]),
      O('구미호 도깨비불', [ddef(0.4, EA), per(K), ddef(0.08, EA), stk(K, 1)]),
      O('랜턴으로 빼앗기', [ddef(0.6), per(K), ddef(0.12), ifKill, stk(K, 2)]),
      O('교주 저금통', [ddef(0.8), per(K), ddef(0.2), spendAll(K)]),
      O('불꽃 휘두르기', [ddef(0.5), per(K), ddef(0.1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))], { power: true }),
    ], [B('도깨비불 강화', 'power'), B('불꽃 냄새', 'frost'), B('히힛', [stk(K, 1)])]),
  ]);
  starter(j, '벨라_u1');
}

// ════════════════════════════════════════════════════════════════════
// 8. 사리 — 딜러 · 순수. 동료가 칠 때마다 「맞장구」가 쌓여 세지고, 장난스런 웃음 한 번에 다 쏟는다
//    3단계(연계 딜러 — 채우기 · 2단계 하한 밖): 시동 = 개전 강화 「셋으로 찢긴 수의」(첫 턴 협공 + 매 턴 맞장구) ·
//    맞장구 최대 5(넘침 37% 줄이기) · 기본 카드를 「뼈 있는 맞장구」로 따라 하기(기본 카드 연료)
// ════════════════════════════════════════════════════════════════════
function sari(j) {
  const H = '사리', K = '맞장구';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '동료가 운을 띄울 때마다 터지는 박수', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }] };
  delete h.keywords;
  h.passives = [
    P('그렇지 그렇지', 'play', [stk(K, 1)], { when: { who: 'other' }, limit: 3 }),
    P('와아', 'break', [stk(K, 2)], { limit: 1 }),
  ];
  const LINK = { tags: ['연계'] };
  put(j, [
    // u1 터뜨리기 — 원작 저학년 장난스런 웃음
    card(H, 1, '장난스런 웃음', 1, '공격', [dmg(0.9), per(K), dmg(0.3), spendAll(K)], [
      O('촌철살인', [dmg(1.1), per(K), dmg(0.36), spendAll(K)]),
      O('가장 먼 적에게', [dmg(0.9), per(K), dmg(0.3), spendAll(K)], { tags: ['약점 공격'] }),
      O('박수 아껴 두기', [dmg(0.95), per(K), dmg(0.3)]),
      O('박수 장인', [dmg(0.9), per(K), dmg(0.3), power(rule('play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 1 }))], { power: true }),
      O('앙코르 리액션', [dmg(0.9), per(K), dmg(0.3), ifKill, stk(K, 2)]),
    ], [B('리액션 장인', 'power'), B('와아아', 'weakSpot'), B('따라 웃기', [stk(K, 1)])]),
    // u2 열기 — 똑같은 포즈: 맞장구 + 동료 카드
    card(H, 2, '똑같은 포즈', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], [
      O('완벽한 포즈', [stk(K, 2), draw(1, { who: 'other' })]),
      O('따라 하기', [stk(K, 1), draw(1, { who: 'other' }), inspire, stk(K, 2)]),
      O('포즈 연습', [draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('먼저 말 걸기', [stk(K, 1), draw(2, { who: 'other', type: '공격' })]),
      O('흉내 연습', [turnBasic('사리_u3'), draw(2, { who: 'other' })]),
    ], [B('박수', 'draw'), B('리액션', [stk(K, 1)]), B('먼저 웃기', { tags: ['개전'] })]),
    // u3 굴리기 — 뼈 있는 맞장구(연계 — 공짜 자동 발동이라 ×0.7)
    card(H, 3, '뼈 있는 맞장구', 1, '공격', [dmg(0.8), stk(K, 1)], [
      O('뼈 때리기', [dmg(1.05), stk(K, 1)], LINK),
      O('맞장구 박수', [dmg(0.55, EA), stk(K, 1)], LINK),
      O('은근한 한마디', [dmg(0.75), stk(K, 1), ifStack(K, 3), dmg(0.5)], LINK),
      O('그렇지!', [dmg(1.3), per(K), dmg(0.2)]),
      O('맞장구 장인', [dmg(0.7), stk(K, 1), power(rule('link', [stk(K, 1)], { limit: 1 }))], { tags: ['연계'], power: true }),
    ], [B('뼈', 'power'), B('찰떡 리액션', 'draw'), B('웃음 한 스푼', 'cost')], LINK),
    // u4 시동(개전 강화) — 셋으로 찢긴 수의: 협공(동행) + 매 턴 맞장구
    card(H, 4, '셋으로 찢긴 수의', 1, '강화', [st('협공', 1), stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], [
      O('수의 세 조각', [st('협공', 2), power(rule('turnStart', [stk(K, 1)]))], KAI),
      O('최고의 조연 소원', [st('협공', 1), stk(K, 2), power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [extra(0.6)], { when: { id: K, n: 5 } }))], KAI),
      O('누군지 모를 유령', [st('협공', 1), stk(K, 4), power(rule('turnStart', [stk(K, 1)]))]),
      O('찢긴 수의 휘날림', [hits(3, 0.4, E1), power(rule('turnStart', [stk(K, 1)]))], KAI),
      O('맞장구 찾아다니기', [st('협공', 1), draw(2, { who: 'other', type: '공격' }), power(rule('turnStart', [stk(K, 2)]))], KAI),
    ], [B('조연의 기쁨', 'atkUp'), B('흐릿해지기', [sh(0.4)]), B('하얀 수의', 'cost')], KAI),
  ]);
  starter(j, '사리_u4');
}

// ════════════════════════════════════════════════════════════════════
// 9. 셀리네 — 탱커 · 활발. 약 올려 「표정」을 모으고, 다섯이면 업로드(실드 · 드로우) 아니면 갤러리로 터뜨린다
//    3단계: 생성 카드 「셀카」(부 장치 — 셋 모이면 「조회수 폭발 셀카」로 진화) · ④ 사근사근 반말은 탱커 상시 버프라 강화 유지
// ════════════════════════════════════════════════════════════════════
function selline(j) {
  const H = '셀리네', K = '표정', PIC = '셀리네_selfie', VIRAL = '셀리네_viral';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '약 올려서 모은 화난 표정', carrier: 'self', cap: 5,
    rules: [{ name: '업로드', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), sh(2.0), draw(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('방금 그 표정 좋았어', 'debuff', [stk(K, 1)], { limit: 2 }),
    P('세 번째 도발', 'play', [heal(0.4), st('반격', 1)], { when: { every: 3 } }),
  ];
  token(j, { id: PIC, name: '셀카', cost: 0, type: '스킬', tags: ['소멸'], evolve: { n: 3, into: VIRAL }, fx: [stk(K, 1), sh(0.35)], blurb: '화난 얼굴 옆에서 브이. 구독자용 비하인드 컷.' });
  token(j, { id: VIRAL, name: '조회수 폭발 셀카', cost: 0, type: '스킬', tags: ['소멸'], fx: [sh(1.2), stk(K, 2), draw(1)], blurb: '세 장을 이어 붙였더니 알고리즘이 반응했습니다.' });
  put(j, [
    // u1 열기(시동) — 원작 저학년 하트 에너지(광역 + 약화 담당)
    card(H, 1, '온 마음을 다해', 1, '공격', [ddef(0.3, EA), st('약화', 1, EA), stk(K, 1)], [
      O('하트 에너지', [ddef(0.4, EA), st('약화', 1, EA), stk(K, 1)]),
      O('하트 폭발', [ddef(0.6, EA), st('약화', 1, EA), st('기절', 1, ER)], { cost: 2 }),
      O('한 명만 노려', [discard(1), ddef(0.75), st('약화', 2, E1)]),
      O('후훗', [ddef(0.35, EA), st('약화', 1, EA), draw(1, { who: 'self', type: '스킬' })]),
      O('관객 몰이', [ddef(0.35, EA), st('약화', 1, EA), power(rule('debuff', [sh(0.5)], { limit: 2 }))], { power: true }),
    ], [B('사근사근', 'power'), B('토끼 펀치', 'frost'), B('구독 버튼', [make(PIC, 1)])]),
    // u2 굴리기 — 인증샷(실드 + 표정 + 셀카 한 장)
    card(H, 2, '인증샷', 1, '스킬', [sh(1.1), stk(K, 1), make(PIC, 1)], [
      O('화난 표정 인증샷', [sh(1.35), stk(K, 1), make(PIC, 1)]),
      O('연속 촬영', [sh(0.9), make(PIC, 2)]),
      O('무한 일회용 카메라', [sh(1.6), stk(K, 2), draw(1, { who: 'self' })]),
      O('셀카 각도', [sh(1.5), make(PIC, 1), ifStack(K, 4), st('피해 감소', 1)]),
      O('채널 No.5', [sh(2.2), make(PIC, 2), st('피해 감소', 1)], { cost: 2 }),
    ], [B('포즈', 'guard'), B('플래시', 'draw'), B('필터', [stk(K, 1)])]),
    // u3 터뜨리기 — 화난 얼굴 갤러리(업로드 전에 쏟기)
    card(H, 3, '화난 얼굴 갤러리', 1, '공격', [ddef(0.45), per(K), ddef(0.18), spendAll(K)], [
      O('명예의 전당', [ddef(0.55), per(K), ddef(0.22), spendAll(K)]),
      O('갤러리 공개', [ddef(0.3, EA), per(K), ddef(0.12, EA), spendAll(K)]),
      O('셀카 전시회', [ddef(0.4), perTag(PIC), ddef(0.3), exileAll(PIC)]),
      O('구독자 0부터', [ddef(0.45), per(K), ddef(0.18), ifKill, stk(K, 3)]),
      O('셀럽 복귀', [ddef(0.4), per(K), ddef(0.16), power(rule('stackReach', [ddef(0.4, EA)], { when: { id: K, n: 5 } }))], { power: true }),
    ], [B('액자', 'power'), B('약 올리기', 'frost'), B('조회수', 'ap')]),
    // u4 완성형 — 사근사근 반말: 결의(탱커 버프 하나) + 매 턴 표정 — 원작 상시 효과라 강화 카드 유지
    card(H, 4, '사근사근 반말', 1, '강화', [st('결의', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('나긋한 반말', [st('결의', 1), power(rule('turnStart', [stk(K, 1), sh(0.3)]))]),
      O('체스의 검은 말', [st('결의', 1), power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [st('반격', 1)], { when: { id: K, n: 5 } }))]),
      O('뒷일 생각 안 함', [st('결의', 2), stk(K, 3), power(rule('turnStart', [stk(K, 1), sh(0.3)]))], { cost: 2 }),
      O('판 짜기', [st('결의', 1), stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('무미호 장난', [st('결의', 1), make(PIC, 2), power(rule('turnStart', [stk(K, 1)]))]),
    ], [B('기부 챌린지', 'defUp'), B('셀럽 미소', [sh(0.4)]), B('아하하', 'cost')]),
  ]);
  starter(j, '셀리네_u1');
}

// ════════════════════════════════════════════════════════════════════
// 10. 셰이디 — 딜러 · 광기. 적에게 「장난」을 셋 걸면 자세가 무너진다(강인도 · 피해) — 그 전에 사슬낫으로 쏟을 수도
//    3단계: ④ 차원 주머니 = 적의 디버프 가짓수를 세는 1코 마무리(옛 「매 턴 장난」 엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function shady(j) {
  const H = '셰이디', K = '장난';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '셰이디가 미리 떠벌려 둔 장난', carrier: 'enemy', cap: 3, per: [{ stat: 'taken', v: 0.06 }],
    rules: [{ name: '자세 붕괴', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), { k: 'tough', v: 2 }, dmg(0.5)] }],
  };
  delete h.keywords;
  h.passives = [
    P('장난질', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }),
    P('다음 장난 예고', 'kill', [stk(K, 1, EA)], { when: { mine: true }, limit: 1 }),
  ];
  put(j, [
    // u1 열기(시동) — 원작 저학년 불 좀 꺼줄래?(연타 + 장난)
    card(H, 1, '불 좀 꺼줄래?', 1, '공격', [hits(4, 0.27, E1), stk(K, 1, E1)], [
      O('불 좀 꺼줄래? Max!', [hits(4, 0.35, E1), stk(K, 1, E1)]),
      O('차원 사슬낫', [hits(4, 0.27, E1), stk(K, 1, E1), ifBroken, dmg(0.8)]),
      O('가장 뒤의 적', [hits(4, 0.27, E1), stk(K, 1, E1)], { tags: ['약점 공격'] }),
      O('불 끄기 장난', [hits(4, 0.25, E1), draw(1, mySearch)]),
      O('장난 예고장', [hits(4, 0.25, E1), stk(K, 1, E1), power(rule('play', [stk(K, 1, E1)], { when: { type: '스킬' }, limit: 1 }))], { power: true }),
    ], [B('사슬낫 1호', 'power'), B('어둠 속', 'weakSpot'), B('히히', [stk(K, 1, E1)])]),
    // u2 터뜨리기 — 사슬낫 네 자루: 장난 1개당 한 대 더, 다 쓴다
    card(H, 2, '사슬낫 네 자루', 1, '공격', [dmg(0.7), per(K), dmg(0.4), spendAll(K)], [
      O('사슬낫 1~4호', [dmg(0.85), per(K), dmg(0.48), spendAll(K)]),
      O('네 방향 사슬낫', [dmg(0.45, EA), perEach(K), dmg(0.25, EA)]),
      O('사슬 감기', [hits(2, 0.7, E1), { k: 'tough', v: 1 }, stk(K, 1, E1)]),
      O('장난의 끝', [dmg(1.5), per(K), dmg(0.85), spendAll(K)], { tags: ['소멸'] }),
      O('다음 계획', [per(K), dmg(0.6), spendAll(K), draw(1)]),
    ], [B('사슬낫 4호', 'power'), B('쓰라림', 'frost'), B('인동 딸기 향', 'draw')]),
    // u3 굴리기(취약 담당) — 대롱대롱 매달기
    card(H, 3, '대롱대롱 매달기', 1, '스킬', [stk(K, 2, E1), st('취약', 2, E1)], [
      O('거꾸로 매달기', [stk(K, 2, E1), st('취약', 3, E1)]),
      O('줄줄이 매달기', [stk(K, 2, E1), st('취약', 2, E1), stk(K, 1, EA)]),
      O('한 달 미룬 날짜', [stk(K, 3, E1), draw(1)]),
      O('계획 떠벌리기', [stk(K, 2, E1), st('취약', 2, E1), draw(1, { who: 'self', type: '공격' })]),
      O('교주도 매달아 봄', [discard(1), stk(K, 3, E1), st('취약', 3, E1)]),
    ], [B('튼튼한 사슬', 'draw'), B('나이 얘기 금지', [st('취약', 1, E1)]), B('빠른 매듭', 'cost')]),
    // u4 완성형(④ 1코 마무리) — 차원 주머니: 적에게 붙은 디버프 1가지당 한 번 더
    card(H, 4, '차원 주머니', 1, '공격', [hits(2, 0.3, E1), perDebuff, dmg(0.3)], [
      O('깊은 차원 주머니', [hits(2, 0.38, E1), perDebuff, dmg(0.36)]),
      O('주머니 뒤적', [hits(2, 0.3, E1), draw(1), power(rule('turnStart', [stk(K, 1, E1)]))], { power: true }),
      O('소악마의 대변인', [st('협공', 1), stk(K, 1, E1)]),
      O('괴물 끌고 오기', [hits(2, 0.25, E1), perDebuff, dmg(0.25)], { cost: 0 }),
      O('주머니 털기', [discard(1), hits(2, 0.4, E1), perDebuff, dmg(0.4)]),
    ], [B('주머니 속 인형', 'draw'), B('소악마의 삼지창', 'atkUp'), B('차원 틈새', [stk(K, 1, E1)])]),
  ]);
  starter(j, '셰이디_u1');
}

// ════════════════════════════════════════════════════════════════════
// 11. 셰이디(역전) — 서포터(회복 축) · 활발. 착한 척하느라 「울분」을 삼키다 넷이면 인형을 패며 터진다(광역 + 다음 턴 AP)
//    3단계(2단계 하한 밖): 시동 = 개전 강화 「내 방식대로 지키겠어」(첫 턴 사기 + 매 턴 울분) · 생성 카드 「늪지 호박」(회복 + 울분)
// ════════════════════════════════════════════════════════════════════
function shadyT(j) {
  const H = '셰이디_역전', K = '울분', PUMP = '셰이디_역전_pumpkin';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '착한 척하느라 삼킨 화', carrier: 'self', cap: 4,
    rules: [{ name: '인형 패기', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(1.0, EA), later(1, [ap(1)])] }],
  };
  delete h.keywords;
  h.passives = [
    P('착한 척은 힘들어', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 2 }),
    P('참다 터짐', 'hurt', [stk(K, 1)], { limit: 1 }),
  ];
  token(j, { id: PUMP, name: '늪지 호박', cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(1.0), stk(K, 1)], blurb: '스피키가 늪지에 두고 간 호박. 삶아 먹으면 기운이 납니다.' });
  const tear = (a, b, o) => [stk(K, a), c1, make(PUMP, b), c2, st('피해 감소', o)];
  const u1 = card(H, 1, '차원의 틈', 1, '스킬', tear(1, 2, 2), [
    O('호박 한가득', tear(1, 3, 2)),
    O('앨리스의 냥의자', [stk(K, 1), c1, make(PUMP, 2), c2, dmg(0.85, EA)]),
    O('작은 틈', tear(1, 1, 1), { cost: 0 }),
    O('정신 수양', [stk(K, 1), c1, make(PUMP, 3), c2, draw(2, { who: 'other' })]),
    O('차원의 흔적', [power(rule('turnStart', [stk(K, 1)])), c1, make(PUMP, 2), c2, st('피해 감소', 2)], { power: true }),
  ], [B('호박 향', 'heal'), B('레이저 불빛', [sh(0.4)]), B('늪지 텃밭', [make(PUMP, 1)])]);
  u1.choices = ['스피키의 호박', '레테의 레이저'];
  put(j, [
    // u1 열기 — 원작 저학년 차원의 틈: 호박(생성 — 회복) 또는 레이저(피해 감소)
    u1,
    // u2 터뜨리기 — 유령 인형 샌드백: 울분 1개당 광역 한 대
    card(H, 2, '유령 인형 샌드백', 1, '공격', [dmg(0.4, EA), per(K), dmg(0.25, EA), spendAll(K)], [
      O('있는 힘껏 패기', [dmg(0.5, EA), per(K), dmg(0.3, EA), spendAll(K)]),
      O('한 놈만', [dmg(0.7), per(K), dmg(0.4), spendAll(K)]),
      O('호박 던지기', [dmg(0.4, EA), perTag(PUMP), dmg(0.3, EA), exileAll(PUMP)]),
      O('샌드백 상시 대기', [dmg(0.4, EA), per(K), dmg(0.2, EA), power(rule('stackReach', [heal(0.6)], { when: { id: K, n: 4 } }))], { power: true }),
      O('화풀이 시간', [dmg(0.45, EA), per(K), dmg(0.25, EA), ifKill, stk(K, 2)]),
    ], [B('인형 솜', 'power'), B('삼단봉', 'weakSpot'), B('흐흐', [stk(K, 1)])]),
    // u3 굴리기 — 파수꾼의 낫질(피해 + 실드)
    card(H, 3, '파수꾼의 낫질', 1, '공격', [dmg(0.85), sh(0.6), stk(K, 1)], [
      O('늪지 파수', [dmg(1.05), sh(0.75), stk(K, 1)]),
      O('늪 한 바퀴', [dmg(0.6, EA), sh(0.6), stk(K, 1)]),
      O('처리할 일 목록', [dmg(0.85), sh(0.6), draw(1, { who: 'self', type: '스킬' })]),
      O('투덜투덜', [discard(1), dmg(1.1), stk(K, 2)]),
      O('질서와 책임', [dmg(0.85), sh(0.6), power(rule('turnEnd', [sh(0.4)]))], { power: true }),
    ], [B('낫 손질', 'power'), B('파수꾼 망토', 'guard'), B('투덜', 'draw')]),
    // u4 시동(개전 강화 · 사기 담당) — 내 방식대로 지키겠어
    card(H, 4, '내 방식대로 지키겠어', 1, '강화', [st('사기', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('끝까지 지키겠어', [st('사기', 1), power(rule('turnStart', [stk(K, 1), sh(0.3)]))], KAI),
      O('교주에게 인정받기', [st('사기', 1), power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [heal(0.9)], { when: { id: K, n: 4 } }))], KAI),
      O('유치원 선생님', [st('사기', 1), stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], KAI),
      O('활망구', [st('사기', 1), dmg(0.9, EA), power(rule('turnStart', [stk(K, 2)]))], { cost: 2, tags: ['개전'] }),
      O('제멋대로 지키기', [st('사기', 1), make(PUMP, 3), power(rule('turnStart', [stk(K, 1)]))]),
    ], [B('정신 수양 도구', 'defUp'), B('평화로운 늪', [make(PUMP, 1)]), B('나긋한 반말', 'cost')], KAI),
  ]);
  starter(j, '셰이디_역전_u4');
}

// ════════════════════════════════════════════════════════════════════
// 12. 스피키 — 서포터(AP 축) · 순수. 동료를 흉내 내 「변장」 셋이면 완벽한 흉내 — 동료 카드를 당겨 오고 「호박 사탕」(AP)
//    3단계: 생성 카드 「호박 사탕」(보존 · 내면 AP 1 — AP 축)
// ════════════════════════════════════════════════════════════════════
function speaki(j) {
  const H = '스피키', K = '변장', CANDY = '스피키_candy';
  const h = j.heroes[0];
  const cs = (n = 1) => ({ k: 'cardStatus', id: '비용', v: -1, to: 'hand', n, who: 'other' });
  h.keyword = {
    name: K, desc: '아군을 흉내 낸 어설픈 분장', carrier: 'self', cap: 3,
    rules: [{ name: '완벽한 흉내', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), draw(1, { who: 'other' }), make(CANDY, 1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('완벽한 따라쟁이', 'play', [stk(K, 1)], { when: { who: 'other' }, limit: 2 }),
    P('호박밭 산책', 'turnEnd', [st('저장', 1)], { conds: [{ c: 'apLeft', n: 1 }] }),
  ];
  token(j, { id: CANDY, name: '호박 사탕', cost: 0, type: '스킬', tags: ['소멸', '보존'], fx: [ap(1)], blurb: '호박밭에서 주운 알사탕. 계피 맛이 난다고 우깁니다.' });
  put(j, [
    // u1 열기(시동) — 원작 저학년 펌킨 매직: 동료의 힘을 채운다(AP + 동료 카드)
    card(H, 1, '펌킨 매직', 1, '스킬', [ap(1), stk(K, 1), draw(1, { who: 'other' })], [
      O('계피맛 알사탕', [ap(1), stk(K, 1), draw(2, { who: 'other' })]),
      O('늙은 호박만', [stk(K, 1), draw(1, { who: 'other' })], { cost: 0 }),
      O('펌킨 매직 대성공', [ap(2), make(CANDY, 1), draw(2, { who: 'other' })], { cost: 2 }),
      O('기운내라!', [ap(1), draw(2, { who: 'other' }), inspire, stk(K, 2)]),
      O('호박 친구 산책', [ap(1), draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
    ], [B('호박밭', 'draw'), B('알사탕', [make(CANDY, 1)]), B('흐에엥', { tags: ['보존'] })]),
    // u2 완성형(사기 담당) — 사제장 대리(네르 흉내)
    card(H, 2, '사제장 대리', 1, '강화', [st('사기', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('제사장 자칭', [st('사기', 1), power(rule('turnStart', [stk(K, 1), heal(0.3)]))]),
      O('네르 흉내', [st('사기', 1), power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [heal(0.9)], { when: { id: K, n: 3 } }))]),
      O('메소드 연기', [st('사기', 1), draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))]),
      O('마요가 둘', [st('사기', 1), stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('어설픈 분장', [make(CANDY, 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0 }),
    ], [B('최면', 'atkUp'), B('제사장 모자', [heal(0.4)]), B('분장 도구', 'cost')]),
    // u3 굴리기 — 호박 바구니: 동료 카드 비용 -1 + 변장
    card(H, 3, '호박 바구니', 0, '스킬', [cs(), stk(K, 1)], [
      O('큰 호박 바구니', [cs(), stk(K, 2), draw(1)]),
      O('호박 나눠 주기', [cs(), stk(K, 1), draw(1, { who: 'other' })]),
      O('호박 괴롭히지 마', [discard(1), cs(2), stk(K, 2)]),
      O('호박밭 가꾸기', [cs(), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('바구니 속 사탕', [cs(), stk(K, 1), inspire, draw(1, { who: 'other' })]),
    ], [B('호박 리본', 'draw'), B('사탕 하나', [heal(0.3)]), B('바구니 들기', [stk(K, 1)])]),
    // u4 터뜨리기(④ 1코 마무리) — 험한 입: 변장을 털고 광역
    card(H, 4, '한 번 맞아보실래요오?', 1, '공격', [dmg(0.5, EA), per(K), dmg(0.25, EA), spendAll(K)], [
      O('정말 맞아보실래요오?', [dmg(0.6, EA), per(K), dmg(0.3, EA), spendAll(K)]),
      O('한 명만 맞아요오', [dmg(0.8), per(K), dmg(0.4), spendAll(K)]),
      O('교주님은 빼고', [dmg(0.55, EA), per(K), dmg(0.25, EA)]),
      O('흐에엥 펀치', [dmg(0.5, EA), per(K), dmg(0.25, EA), make(CANDY, 1)]),
      O('험한 입', [dmg(0.5, EA), per(K), dmg(0.25, EA), ifKill, stk(K, 2)]),
    ], [B('호박 펀치', 'power'), B('막말', 'frost'), B('눈치 빠른 녀석', 'ap')]),
  ]);
  starter(j, '스피키_u1');
}

// ════════════════════════════════════════════════════════════════════
// 13. 스피키(메이드) — 딜러 · 활발. 고장 난 청소기가 「잡동사니」를 빨아들이다 넷이면 터보로 뱉는다
//    3단계: 기본 카드를 빨아들여 잡동사니로(청소 — 기본 카드 연료) · ④ 크레페 선배의 특훈 = 잡동사니를 세는 1코 마무리
// ════════════════════════════════════════════════════════════════════
function speakiMaid(j) {
  const H = '스피키_메이드', K = '잡동사니';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '고장 난 청소기에 빨려 든 것들', carrier: 'self', cap: 4,
    rules: [{ name: '터보 배출', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), hits(5, 0.3, ER), ap(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('고장난 청소기', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    P('자동 청소 모드에요오!', 'ult', [st('사기', 1)]),
  ];
  put(j, [
    // u1 열기(시동) — 원작 저학년: 잡동사니 날리기(광역)
    card(H, 1, '깨끗하면 할 일이 없어!', 1, '공격', [dmg(0.7, EA), stk(K, 1)], [
      O('호박 낙하', [dmg(0.9, EA), stk(K, 1)]),
      O('전단지', [dmg(0.6, EA), rush(1, EA), draw(1, { who: 'self', type: '스킬' })]),
      O('먼지 한 톨', [dmg(0.6, EA), stk(K, 1)], { cost: 0 }),
      O('대청소의 날', [dmg(1.1, EA), stk(K, 2), exileBasic(2)], { cost: 2 }),
      O('청소 운', [dmg(0.7, EA), stk(K, 1), ifStack(K, 4), dmg(0.4, EA)]),
    ], [B('터보 버튼', 'power'), B('크레페 배지', 'draw'), B('호박 장식', [stk(K, 1)])]),
    // u2 터뜨리기 — 고장난 흡입구: 넷 전에 일찍 뱉기
    card(H, 2, '고장난 흡입구', 1, '공격', [hits(3, 0.3, ER), per(K), dmg(0.22, ER), spendAll(K)], [
      O('최대 흡입', [hits(3, 0.37, ER), per(K), dmg(0.27, ER), spendAll(K)]),
      O('한 곳만 청소', [hits(3, 0.32, E1), per(K), dmg(0.24, E1), spendAll(K)]),
      O('조금만 뱉기', [hits(3, 0.32, ER), per(K), dmg(0.22, ER)]),
      O('흡입구 역류', [hits(3, 0.35, ER), exileBasic(1), stk(K, 2)]),
      O('물청소 질색', [hits(3, 0.28, ER), per(K), dmg(0.2, ER), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
    ], [B('청소기 수리', 'power'), B('흡입력', 'weakSpot'), B('먼지 봉투', 'ap')]),
    // u3 굴리기 — 호박 장식 시즌(0코 청소: 손의 기본 카드 1장을 빨아들여 잡동사니 + 실드)
    card(H, 3, '호박 장식 시즌', 0, '스킬', [exileBasic(1), stk(K, 2), sh(0.4)], [
      O('호박 테마 저택', [exileBasic(1), stk(K, 2), sh(0.6)]),
      O('세상 가장 청결한 호박', [exileBasic(1), stk(K, 2), heal(0.8)]),
      O('호박 장식 찾기', [exileBasic(1), stk(K, 2), draw(1, { who: 'self', type: '공격' })]),
      O('장식 정리', [exileBasic(1), sh(0.6), inspire, stk(K, 3)]),
      O('시즌 준비', [stk(K, 1), sh(0.4), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
    ], [B('호박 쟁반', 'guard'), B('콧노래', [stk(K, 1)]), B('메이드 미소', 'draw')]),
    // u4 완성형(④ 1코 마무리) — 크레페 선배의 특훈: 잡동사니 1개당 한 대 더(쓰지 않고 센다 — 옛 「스킬마다 잡동사니」 엔진은 D 갈래)
    card(H, 4, '크레페 선배의 특훈', 1, '공격', [hits(2, 0.3, ER), per(K), dmg(0.2, ER)], [
      O('특훈 이수', [hits(2, 0.38, ER), per(K), dmg(0.25, ER)]),
      O('메이드 교육', [hits(2, 0.3, ER), draw(1), power(rule('play', [stk(K, 1)], { when: { type: '스킬' }, limit: 1 }))], { power: true }),
      O('까다로운 업무', [hits(2, 0.25, ER), per(K), dmg(0.17, ER)], { cost: 0 }),
      O('선배의 칭찬', [hits(2, 0.25, ER), draw(1), stk(K, 2)]),
      O('완벽한 메이드', [discard(1), hits(3, 0.3, ER), per(K), dmg(0.22, ER)]),
    ], [B('메이드 복', 'atkUp'), B('칭찬 한마디', 'cost'), B('빗자루', [stk(K, 1)])]),
  ]);
  starter(j, '스피키_메이드_u1');
}

// ════════════════════════════════════════════════════════════════════
// 14. 시온 더 다크불릿 — 딜러 · 우울 · 엘다인. 「검은 탄창」을 채워 공격력을 들고 다닐지, 마탄의 사수로 쏟을지
//     엘다인 한 단계: 탄창이 가득 차면 진혼의 마탄 한 발 · 엘다인 패시브 「화합」(위기 한 번)
//     3단계: ④ 진혼의 탄환은 원작 상시(장전) 결이라 강화 유지 · 신탁 갈래만 정리
// ════════════════════════════════════════════════════════════════════
function xion(j) {
  const H = '시온더다크불릿', K = '검은 탄창', T = '시온더다크불릿_t1';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '어둠의 힘으로 장전한 마탄', carrier: 'self', cap: 5, per: [{ stat: 'atk', v: 0.04 }],
    rules: [{ name: '진혼 장전', when: { on: 'stackReach', id: K, n: 5 }, limit: { per: 'turn', n: 1 }, fx: [make(T, 1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('큭큭, 봉인 해제', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    P('화합', 'lowHp', [st('피해 감소', 2), make(T, 2)], { when: { pct: 0.4 } }),
  ];
  const tok = j.cards.find(c => c.id === T);
  Object.assign(tok, { cost: 0, type: '공격', tags: ['증발', '약점 공격'], fx: [dmg(1.0)] });
  const reload = rule('play', [stk(K, 1)], { when: { type: '공격', maxCost: 0 } });
  put(j, [
    // u1 터뜨리기 — 원작 저학년 마.탄.의.사.수: 마탄 수만큼 쏘고 비운다
    card(H, 1, '마.탄.의.사.수', 1, '공격', [dmg(0.7), per(K), dmg(0.3), spendAll(K)], [
      O('마.탄.전.탄', [dmg(0.85), per(K), dmg(0.36), spendAll(K)]),
      O('반쯤 비운 탄창', [dmg(0.75), per(K), dmg(0.3)]),
      O('어둠의 일제사격', [dmg(0.45, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      O('운.명.의.탄.환', [per(K), dmg(0.45), spendAll(K), make(T, 1)]),
      O('좌표 확인', [dmg(0.7), per(K), dmg(0.3), ifKill, stk(K, 3)]),
    ], [B('오른눈 렌즈', 'power'), B('검은 탄피', 'weakSpot'), B('큭큭', [stk(K, 1)])]),
    // u2 굴리기 — 디 엑시트로(바가지 쓴 고물 총)
    card(H, 2, '디 엑시트로', 1, '공격', [dmg(1.05), stk(K, 1)], [
      O('바가지 쓴 명총', [dmg(1.3), stk(K, 1)]),
      O('관통 사격', [dmg(1.0), stk(K, 1)], { tags: ['분쇄'] }),
      O('네 번째 탄환', [dmg(0.95), stk(K, 1), ifStack(K, 4), dmg(0.6, EA)]),
      O('연속 장전', [dmg(0.9), draw(1, mySearch)]),
      O('고물 총 개조', [dmg(0.95), stk(K, 1), power(reload)], { power: true }),
    ], [B('총열 청소', 'power'), B('눈가림 섬광', 'frost'), B('편의점 야간 알바', 'draw')]),
    // u3 열기(시동) — 좌표 잡기(0코 장전 + 드로우)
    card(H, 3, '좌표 잡기', 0, '스킬', [stk(K, 1), draw(1)], [
      O('정밀 좌표', [stk(K, 2), draw(1)]),
      O('다크넷 서핑', [stk(K, 1), draw(1, { who: 'self', type: '공격' }), draw(1)]),
      O('엄폐 무시', [stk(K, 1), make(T, 1)]),
      O('자기 활약상 웹소설', [stk(K, 1), draw(1), inspire, stk(K, 2)]),
      O('어둠의 의식', [draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
    ], [B('검은 음식', 'draw'), B('덜크 발음', [stk(K, 1)]), B('어두운 곳', { tags: ['보존'] })]),
    // u4 완성형(엘다인 한 단계) — 진혼의 탄환: 마탄 한 발 + 마탄을 쏠 때마다 재장전
    card(H, 4, '진혼의 탄환', 1, '강화', [make(T, 1), power(reload)], [
      O('진혼곡', [make(T, 2), power(reload)]),
      O('구원자의 탄창', [make(T, 1), power(reload, rule('turnStart', [make(T, 1)]))], { cost: 2 }),
      O('화합의 탄환', [make(T, 1), st('협공', 1), power(reload)]),
      O('어.둠.의.구.원.자★', [discard(1), make(T, 2), power(reload, rule('stackReach', [dmg(0.6, EA)], { when: { id: K, n: 5 } }))]),
      O('첫 마탄', [stk(K, 2), power(reload)], { cost: 0 }),
    ], [B('검은 탄두', 'atkUp'), B('진혼의 기도', 'draw'), B('탄피 줍기', 'cost')]),
  ]);
  starter(j, '시온더다크불릿_u3');
}

// ════════════════════════════════════════════════════════════════════
// 15. 앨리스 — 딜러 · 광기(고학년 아르카나 갈래 유지). 남의 불운으로 「행운」을 모으고, 남의 불운 한 장에 쏟는다
//    3단계: 시동 = 개전 강화 「비추는 거울」(매 턴 행운) · 생성 카드 「타로 한 장」(밑장 빼기 · 아르카나 — 남의 불운이 펼친다)
// ════════════════════════════════════════════════════════════════════
function alice(j) {
  const H = '앨리스', K = '행운', TAROT = '앨리스_tarot';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '남의 불운을 먹고 차오르는 행운', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.08 }] };
  delete h.keywords;
  h.passives = [
    P('완전 럭키 앨리스잖아', 'hurt', [stk(K, 1)], { limit: 2 }),
    P('아르카나 한 장', 'turnStart', [{ k: 'ifRandom', pct: 0.3 }, st('사기', 1), { k: 'ifRandom', pct: 0.4 }, st('취약', 1, EA), { k: 'ifRandom', pct: 0.5 }, stk(K, 1)]),
  ];
  token(j, { id: TAROT, name: '타로 한 장', cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(0.45, ER), stk(K, 1)], blurb: '뒤집기 전까지는 누구의 불운인지 모릅니다.' });
  put(j, [
    // u1 굴리기 — 원작 저학년 아르카나: 약식 점괘(신탁 갈래가 셋의 점괘)
    card(H, 1, '아르카나', 1, '공격', [dmg(0.65, EA), stk(K, 1)], [
      O('잔불 조심', [dmg(0.85, EA), stk(K, 1)]),
      O('타로 뽑기', [dmg(0.55, EA), make(TAROT, 1)]),
      O('생채기 주의', [hits(3, 0.7, ER), stk(K, 1), st('기절', 1, ER)], { cost: 2 }),
      O('약식 점괘', [dmg(0.55, EA), stk(K, 1)], { cost: 0 }),
      O('점괘 직접 맞추기', [dmg(0.65, EA), stk(K, 1), ifStack(K, 3), dmg(0.4, EA)]),
    ], [B('네잎클로버', 'power'), B('타로 카드', 'draw'), B('으히히', [make(TAROT, 1)])]),
    // u2 터뜨리기 — 남의 불운: 행운을 다 걸고 한 방
    card(H, 2, '남의 불운', 1, '공격', [dmg(0.8), per(K), dmg(0.3), spendAll(K)], [
      O('남의 대흉', [dmg(0.95), per(K), dmg(0.36), spendAll(K)]),
      O('행운 나눔', [dmg(0.55, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      O('타로 펼치기', [dmg(0.5), perTag(TAROT), dmg(0.45), exileAll(TAROT)]),
      O('액운 몰아주기', [discard(1), dmg(1.2), per(K), dmg(0.4)]),
      O('거울 속 언니', [dmg(0.8), per(K), dmg(0.3), ifKill, stk(K, 2)]),
    ], [B('불운 수집', 'power'), B('대흉 점괘', 'weakSpot'), B('토끼 금지', [stk(K, 1)])]),
    // u3 시동(개전 강화) — 비추는 거울: 매 턴 행운
    card(H, 3, '비추는 거울', 1, '강화', [stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], [
      O('맑은 거울', [stk(K, 3), power(rule('turnStart', [stk(K, 1)]))], KAI),
      O('거울 속 자기 모습', [stk(K, 2), power(rule('turnStart', [stk(K, 1)]), rule('crit', [draw(1)], { limit: 1 }))], KAI),
      O('카드에 손대지 마', [stk(K, 2), sh(0.6), power(rule('turnStart', [stk(K, 1)]))], KAI),
      O('작은 손거울', [stk(K, 1), make(TAROT, 2), power(rule('turnStart', [stk(K, 1)]))]),
      O('운명의 유령', [stk(K, 3), make(TAROT, 1), power(rule('turnStart', [stk(K, 2)]))], { cost: 2, tags: ['개전'] }),
    ], [B('거울 닦기', 'atkUp'), B('카드덱', 'draw'), B('반짝임', 'cost')], KAI),
    // u4 굴리기(버리기 · 생성) — 밑장 빼기: 밑장에서 타로를 빼 온다
    card(H, 4, '밑장 빼기', 1, '스킬', [discard(1), draw(1), make(TAROT, 1)], [
      O('두 장 빼기', [discard(2), draw(2), make(TAROT, 2)]),
      O('들킨 밑장', [discard(1), draw(1), make(TAROT, 1)], { cost: 0 }),
      O('밑장 한 장', [draw(2, { who: 'self' }), stk(K, 2)]),
      O('완벽한 밑장', [discard(1), draw(2, { who: 'self', type: '공격' }), stk(K, 2)]),
      O('버린 패의 행운', [discard(1), draw(2), power(rule('discard', [stk(K, 1), dmg(0.3, ER)], { limit: 2 }))], { power: true }),
    ], [B('빠른 손', 'draw'), B('숨긴 카드', [make(TAROT, 1)]), B('점괘 고치기', 'cost')]),
  ]);
  starter(j, '앨리스_u3');
}

// ════════════════════════════════════════════════════════════════════
// 16. 에스피 — 서포터(버퍼 축) · 냉정. 적을 홀려(약화) 「꿈 일기」를 적고, 펼쳐 둔 만큼 파티가 세다 — 막말 가면으로 다 쏟을 수도
// ════════════════════════════════════════════════════════════════════
function espi(j) {
  const H = '에스피', K = '꿈 일기';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '몰래 훔쳐본 꿈을 적은 일기장', carrier: 'self', cap: 4, decay: 1, per: [{ stat: 'dealt', v: 0.05, who: 'allies' }] };
  delete h.keywords;
  h.passives = [
    P('꿈 엿보기', 'debuff', [stk(K, 1)], { limit: 2 }),
    P('촛불~충전!', 'kill', [draw(1)], { when: { mine: true }, limit: 1 }),
  ];
  const pull = { k: 'pull', from: 'discard', n: 1 };
  put(j, [
    // u1 열기(시동) — 원작 저학년 헤롱헤롱 촛불(침묵 → 약화 담당)
    card(H, 1, '헤롱헤롱 촛불', 1, '공격', [hits(2, 0.42, E1), st('약화', 2, E1), ifStack(K, 3), st('기절', 1, E1)], [
      O('헤롱헤롱 대촛불', [hits(2, 0.55, E1), st('약화', 2, E1), ifStack(K, 3), st('기절', 1, E1)]),
      O('깊은 잠', [hits(2, 0.42, E1), st('약화', 2, E1), ifStack(K, 2), st('기절', 1, E1)]),
      O('촛불 하나 더', [hits(3, 0.45, E1), st('약화', 2, E1), draw(1, { who: 'self', type: '공격' })]),
      O('꿈속 촛불', [hits(2, 0.6, E1), st('약화', 2, E1), power(rule('debuff', [stk(K, 1)], { limit: 1 }))], { power: true }),
      O('악몽의 촛불', [hits(2, 1.0, E1), st('약화', 3, E1), st('기절', 1, E1)], { cost: 2 }),
    ], [B('밝은 심지', 'power'), B('꿈의 모래', 'frost'), B('흐흐', [stk(K, 1)])]),
    // u2 굴리기 — 강화 평타 촛불 다섯
    card(H, 2, '개꿈 발사!', 1, '공격', [hits(5, 0.22, ER), stk(K, 1)], [
      O('개꿈 대방출', [hits(5, 0.28, ER), stk(K, 1)]),
      O('한 놈만 개꿈', [discard(1), hits(6, 0.27, ER), stk(K, 2)]),
      O('일기장 넘기기', [hits(5, 0.2, ER), per(K), dmg(0.15, ER)]),
      O('먹기만 하는 꿈', [hits(5, 0.22, ER), stk(K, 1), ifKill, draw(1)]),
      O('꿈꾸는 모든 자에게!', [hits(5, 0.2, ER), stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
    ], [B('촛불 다섯 개', 'power'), B('카라멜 팝콘', [heal(0.3)]), B('흐흐흐', 'draw')]),
    // u3 서치 — 꿈 훔쳐보기(버린 더미에서 꺼내기)
    card(H, 3, '꿈 훔쳐보기', 0, '스킬', [pull, stk(K, 1)], [
      O('교주의 꿈 정주행', [pull, stk(K, 2)]),
      O('남의 꿈 엿보기', [draw(1, { who: 'other' }), stk(K, 2)]),
      O('꿈 일기 쓰기', [pull, stk(K, 1), inspire, stk(K, 2)]),
      O('가면 제작', [pull, power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('몰래 보관', [pull, stk(K, 1), draw(1)], { tags: ['보존'] }),
    ], [B('꿈 일기장', 'draw'), B('몰래 보기', [stk(K, 1)]), B('가면 벗기', [heal(0.3)])]),
    // u4 터뜨리기(④ 1코 마무리) — 막말 가면: 일기를 다 찢어 한 방
    card(H, 4, '막말 가면', 1, '공격', [dmg(0.8), per(K), dmg(0.3), spendAll(K)], [
      O('독한 막말', [dmg(0.95), per(K), dmg(0.36), spendAll(K)]),
      O('가면 쓴 채로', [dmg(0.85), per(K), dmg(0.3)]),
      O('모두에게 막말', [dmg(0.55, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      O('악몽 선물', [discard(1), dmg(1.1), per(K), dmg(0.4)]),
      O('시시해', [dmg(0.8), per(K), dmg(0.3), ifKill, stk(K, 2)]),
    ], [B('가면 디자인', 'power'), B('소심한 존댓말', 'ap'), B('큭큭', [stk(K, 1)])]),
  ]);
  starter(j, '에스피_u1');
}

// 1차 측정(혼자 완주율 평균 16%) 뒤 사도마다 고유 카드 수치 배율 — 기본형 · 신탁 · 강화 규칙의 피해 · 실드 · 회복을 같이 곱한다(신탁 값어치 비율은 그대로)
const SCALE = { 레테: 1.0, 림: 1.4, 림_혼돈: 1.1, 메죵: 1.5, 바롱: 1.25, 베루: 1.25, 벨라: 0.95, 사리: 1.7, 셀리네: 1.1, 셰이디: 0.8, 셰이디_역전: 1.5, 스피키: 1.35, 스피키_메이드: 1.35, 시온더다크불릿: 1.0, 앨리스: 1.35, 에스피: 1.35 };
const mulFx = (fx, b) => { for (const f of fx) { if (['dmg', 'shield', 'heal'].includes(f.k) && f.ratio) f.ratio = Math.round(f.ratio * b * 100) / 100; if (f.k === 'power') for (const r of f.rules) mulFx(r.fx, b); } };
const scaled = fn => j => { fn(j); const b = SCALE[j.heroes[0].id] || 1; if (b === 1) return; for (const c of j.cards) if (c.unique) { mulFx(c.fx, b); for (const o of c.oracles || []) mulFx(o.fx, b); } };
run([
  ['유령/레테', scaled(lethe)], ['유령/림', scaled(rim)], ['유령/림_혼돈', scaled(rimChaos)], ['유령/메죵', scaled(maison)], ['유령/바롱', scaled(barong)], ['유령/베루', scaled(veroo)],
  ['유령/벨라', scaled(vela)], ['유령/사리', scaled(sari)], ['유령/셀리네', scaled(selline)], ['유령/셰이디', scaled(shady)], ['유령/셰이디_역전', scaled(shadyT)], ['유령/스피키', scaled(speaki)],
  ['유령/스피키_메이드', scaled(speakiMaid)], ['유령/시온더다크불릿', scaled(xion)], ['유령/앨리스', scaled(alice)], ['유령/에스피', scaled(espi)],
], new URL('./boost_유령.json', import.meta.url));
