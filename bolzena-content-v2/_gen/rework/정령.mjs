// 사도 리워크 — 정령 18명. 멜루나는 시범(rework.mjs)에서 끝났다.
// 2단계(2026-10-07): 장치 하나 + 카드 넷의 역할. 3단계(2026-10-08): 카제나 자료 보강 — 지침 §12
//   신탁 다섯 갈래(재설계 1~2 · 얕은 갈래 2개 이하 · D 강화화 / F 서치 · 대가 갈래 카드 2장 중 1장) · ④ 「장치를 세는 1코 마무리」
//   · 개전 강화 시동(라이카 · 시저 · 잉클 · 쥬비) · 생성 카드(가비아 흙 조각상 · 이프리트 불씨) · 기본 카드 연료
//   · 편성 퍼즐(니콜 채우기 · 실라 끊기) · 축복(같은 kind 셋까지 · 장치를 거드는 축복 2~3개)
// 지침: _measure/사도_리워크_지침.md · 보고: _measure/리워크_정령.md · 공용 부품: lib.mjs
// node _gen/rework/정령.mjs   → heroes/정령/<사도>.json 덮어쓰기(백업에서 읽음)
import { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifAll, ifBroken, inspire, power, rule, later, O, B, card, starter, run } from './lib.mjs';

// ── 이 스크립트 몫 부품 ──
const TOP = 'topEnemy';
const tough = (v, t = E1) => ({ k: 'tough', v, target: t });
const cleanse = v => ({ k: 'cleanse', v });
const burn = v => ({ k: 'burn', v });
const discard = v => ({ k: 'discard', v });
const payPct = v => ({ k: 'payHpPct', v });
const xtra = r => ({ k: 'extra', ratio: r });
const cut = v => ({ k: 'cutHit', v });
const ATK = { k: 'atkMod', v: 0.1, run: true };
const spendN = (id, v) => ({ k: 'spend', id, v });
const spendEA = id => ({ k: 'spend', id, all: true, target: EA });
const perEach = id => ({ k: 'perStack', id, each: true });
const exileAll = id => ({ k: 'exileFrom', from: 'hand', all: true, tag: id });
const hitsT = (n, r, t) => ({ k: 'dmg', ratio: r, target: t, hits: n });
// 3단계 부품 — 서치 · 기본 카드 연료
const srch = (o = {}) => draw(1, { who: 'self', unique: true, ...o });           // 자신의 고유 카드 1장 드로우(지정 서치)
const pullD = (o = {}) => ({ k: 'pull', from: 'discard', n: 1, ...o });          // 버린 더미에서 손으로
const exB = { k: 'exileFrom', from: 'hand', basic: true, n: 1 };                  // 손의 기본 카드 1장 소멸
const toBasic = id => ({ k: 'transform', id, from: 'hand', basic: true, n: 1 });  // 손의 기본 카드 1장을 생성물로(이 전투)
const KAI = ['개전'];
// u5(2026-10-08) — 손의 자신의 카드 1장 비용 -1(거르개)
const csOwn = (o = {}) => ({ k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'self', ...o });
const setCards = (j, cards) => { j.cards = [...j.cards.filter(c => !c.unique), ...cards]; };
const renameUlt = (h, from, to, extra = {}) => {
  for (const f of h.ult.fx) if (['stack', 'spend', 'perStack', 'ifStack'].includes(f.k) && f.id === from) { f.id = to; Object.assign(f, extra); for (const k of Object.keys(extra)) if (extra[k] === undefined) delete f[k]; }
};
const P = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: { per: o.per || 'turn', n: o.limit } } : {}), fx });
const tokenOf = (j, id) => j.cards.find(c => c.id === id);
const addToken = (j, c) => { const old = tokenOf(j, c.id); if (old) Object.assign(old, c); else j.cards.push(c); };

// ════════════════════════════════════════════════════════════════════
// 1. 가비아 — 서포터(실드) · 순수. 맞을 때마다 「삼킨 고함」 — 다섯이면 땅울림, 그 전에 「그만해!!!」로 쏟거나 「지하에 누워」로 쥔 만큼 실드
//    3단계: 생성(흙 조각상 — 조각 취미) · 기본 카드 연료(조각칼: 기본 카드를 조각상으로) · ④ 1코 마무리
// ════════════════════════════════════════════════════════════════════
function gabia(j) {
  const H = '가비아', K = '삼킨 고함', ST = '가비아_statue';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '꾹 삼켜 둔 거친 말', carrier: 'self', cap: 5,
    rules: [{ name: '땅울림', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), ddef(0.6, EA)] }],
  };
  delete h.keywords;
  h.passives = [P('우웅⋯', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 3 }), P('조용한 대지', 'turnEnd', [sh(1.0)], { conds: [{ c: 'ownNone' }] })];
  addToken(j, { id: ST, name: '흙 조각상', hero: H, token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [sh(0.5), stk(K, 1)], blurb: '가비아가 심심풀이로 깎아 둔 작은 흙 인형입니다.' });
  setCards(j, [
    // u1 열기 — 속삭임 한 마디(시동 카드)
    card(H, 1, '작은 목소리', 0, '스킬', [stk(K, 1), draw(1)], [
      O('으응?', [stk(K, 2), draw(1)]),                                                   // A 수치
      O('교주만 알아듣는 말', [stk(K, 1), draw(1), inspire, stk(K, 2)]),                    // E 영감
      O('조각칼', [exB, make(ST, 1), stk(K, 1)]),                                              // 재설계 — 기본 카드를 조각상으로
      O('땅속의 고요', [stk(K, 1), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }), // D
      O('말줄임표', [stk(K, 3), draw(2)], { tags: ['소멸'] }),                              // H 소멸
    ], [B('누워서 숨만', { tags: ['보존'] }), B('흙냄새', 'draw'), B('조각 선물', [make(ST, 1)])]),
    // u2 터뜨리기 — 아흔아홉 번 참은 끝의 고함(삼킨 말 1개당 광역)
    card(H, 2, '그만해!!!', 1, '공격', [ddef(0.4, EA), per(K), ddef(0.12, EA), spendAll(K)], [
      O('참을 만큼 참았어', [ddef(0.5, EA), per(K), ddef(0.14, EA), spendAll(K)]),          // A
      O('도시를 묻는 목소리', [ddef(0.9, EA), per(K), ddef(0.22, EA)], { cost: 2 }),         // C — 다 쓰지 않는다
      O('지켜 주려고 낸 소리', [ddef(0.4, EA), per(K), sh(0.45), spendAll(K)]),              // 공격 ↔ 방어
      O('유령 늪 간척', [per(K), ddef(0.2, EA), spendAll(K), ifKill, stk(K, 3)]),            // 재설계 — 처치하면 되삼킴
      O('조각상 깨기', [ddef(0.4, EA), perTag(ST), ddef(0.3, EA), exileAll(ST)]),           // 재설계 — 조각상을 센다
    ], [B('유창한 장문', 'power'), B('꺼지는 땅', 'cost'), B('몸 사리는 정령들', 'frost')]),
    // u3 굴리기 — 원작 저학년: 보호막이 받은 만큼 돌려준다(실드 + 반격 — 반격 담당)
    card(H, 3, '돌려⋯줄⋯게⋯', 1, '스킬', [sh(1.3), st('반격', 1), stk(K, 1)], [
      O('단단한 돌벽', [sh(1.6), st('반격', 1), stk(K, 1)]),                                 // A
      O('돌벽 셋', [sh(2.2), st('반격', 2), make(ST, 1)], { cost: 2 }),                     // C — 조각상 덤
      O('흡수한 만큼', [sh(1.2), st('반격', 1), per(K), sh(0.32)]),                          // F 스케일링
      O('바위 같은 침묵', [sh(1.0), st('반격', 1), power(rule('hurt', [st('반격', 1)], { when: { guarded: true }, limit: 1 }))], { power: true }), // D
      O('가장 위험한 친구부터', [sh(1.3), st('반격', 1), stk(K, 1)], { tags: ['보존'] }),      // G 보존
    ], [B('흙 갑옷', 'guard'), B('반사 피해', 'ap'), B('잔돌', [stk(K, 1)])]),
    // u4 마무리 — 지하에 누워 숨만(결정화 담당 · 쥔 고함 1개당 실드 — 「그만해!!!」 와 맞바꿈)
    card(H, 4, '지하에 누워', 1, '스킬', [st('결정화', 1), per(K), sh(0.3)], [
      O('깊은 잠', [st('결정화', 1), per(K), sh(0.4)]),                                      // A
      O('땅속의 잠', [st('결정화', 1), power(rule('turnEnd', [stk(K, 1)]))], { power: true }),   // D — 옛 상시 엔진
      O('운동은 싫어', [per(K), sh(0.45), spendAll(K), draw(1)]),                             // 재설계 · 대가 — 다 쓴다
      O('최애 응원 준비', [st('결정화', 1), per(K), sh(0.3), srch()]),                         // F 서치
      O('조각 취미', [st('결정화', 1), per(K), sh(0.25)], { cost: 0 }),                       // B
    ], [B('푹신한 흙', 'defUp'), B('곰팡이 없는 동굴', 'cost'), B('조각 하나 더', [make(ST, 1)])]),
    // u5 굴리기(2026-10-08 u5) — 위층 쿵쿵 금지: 땅을 울려 치고 고함 · 조각상
    card(H, 5, '위층 쿵쿵 금지', 1, '공격', [ddef(0.6), stk(K, 1), make(ST, 1)], [
      O('신탁 1', [ddef(0.75), stk(K, 1), make(ST, 1)]),
      O('신탁 2', [ddef(0.4, EA), stk(K, 1), make(ST, 1)]),
      O('신탁 3', [ddef(0.5), stk(K, 1), power(rule('turnEnd', [make(ST, 1)]))], { power: true }),
      O('신탁 4', [ddef(0.75), make(ST, 1), ifStack(K, 3), sh(1.0)]),
      O('신탁 5', [ddef(1.25), stk(K, 3)]),
    ], [B('축복 1', 'power'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '가비아_u1');
}

// ════════════════════════════════════════════════════════════════════
// 2. 나이아 — 서포터(버퍼) · 순수. 씻길 때마다 「물탱크」 — 들고 있으면 파티 피해↑, 「물대포」로 쏟으면 한 방
// ════════════════════════════════════════════════════════════════════
function naia(j) {
  const H = '나이아', K = '물탱크';
  const h = j.heroes[0];
  renameUlt(h, '물총', K, { target: undefined });
  h.keyword = {
    name: K, desc: '돌고래 물총에 채운 호숫물', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.05, who: 'allies' }],
    rules: [{ name: '호수 대청소', when: { on: 'stackReach', id: K, n: 5 }, fx: [cleanse(1), heal(1.0)] }],
  };
  delete h.keywords;
  h.passives = [
    P('퓨퓨~', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 3 }),
    P('뽀득뽀득 씻어요', 'ult', [{ k: 'gauge', v: 30 }]),
  ];
  setCards(j, [
    // u1 열기 — 심심해! 친구 카드를 끌어온다(시동 카드)
    card(H, 1, '같이 놀자!', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], [
      O('심심해!', [stk(K, 2), draw(1, { who: 'other' })]),                                  // A
      O('얼굴에 물총', [stk(K, 1), draw(1, { who: 'other' }), dmg(0.5)]),
      O('수문장 교대', [draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { power: true }), // D
      O('놀아 줄 때까지', [stk(K, 1), draw(1, { who: 'other' })], { tags: ['회수'] }),         // G
      O('엘리아스의 인싸', [stk(K, 2), pullD({ who: 'other' })]),                             // 재설계 · 서치 — 버린 더미의 친구 카드
    ], [B('돌고래 물총', 'draw'), B('물장구', [stk(K, 1)]), B('수영 모자', { tags: ['보존'] })]),
    // u2 굴리기 — 원작 저학년: 스무 번 헹궈 크게 채우고 해제
    card(H, 2, '그게 씻은거야?', 1, '스킬', [heal(1.2), cleanse(1), stk(K, 1)], [
      O('스무 번 헹구기', [heal(1.4), cleanse(1), stk(K, 1)]),                               // A
      O('호수 통째로', [heal(2.4), stk(K, 2), ifStack(K, 5), st('사기', 1)], { cost: 2 }),    // C · 재설계 — 가득 차면 사기
      O('넘치는 물', [heal(1.0), cleanse(1), per(K), heal(0.3)]),                            // F
      O('뽀득뽀득', [heal(2.6), cleanse(2), stk(K, 3)], { tags: ['소멸'] }),                   // H 소멸
      O('물 한 바가지', [heal(1.0), stk(K, 1), draw(1, { who: 'other' })]),                  // 서치
    ], [B('맑은 호숫물', 'heal'), B('거품 목욕', [stk(K, 1)]), B('결벽증', { tags: ['보존'] })]),
    // u3 터뜨리기 — 강화 평타 물총 세 발 + 탱크 1개당
    card(H, 3, '물대포', 1, '공격', [hitsT(3, 0.3, E1), per(K), dmg(0.2), spendAll(K)], [
      O('돌고래 물대포', [hitsT(3, 0.38, E1), per(K), dmg(0.25), spendAll(K)]),               // A
      O('물보라 사격', [hitsT(3, 0.22, ER), per(K), dmg(0.15, EA), spendAll(K)]),             // G 광역
      O('쏘고 또 쏘고', [hitsT(3, 0.3, E1), per(K), dmg(0.2)]),                              // 쥐는 쪽
      O('물총 결투', [hitsT(3, 0.3, E1), per(K), dmg(0.2), ifKill, stk(K, 2)]),              // 재설계
      O('물총 장전', [hitsT(3, 0.3, E1), power(rule('play', [dmg(0.3)], { when: { type: '스킬' }, limit: 2 }))], { power: true }), // D
    ], [B('세찬 물살', 'power'), B('물에 젖은 적', 'frost'), B('빠른 장전', 'ap')]),
    // u4 마무리 — 외로운 수문장(사기 담당 · 쥔 탱크 1개당 회복)
    card(H, 4, '외로운 수문장', 1, '스킬', [st('사기', 1), per(K), heal(0.2)], [
      O('아가미가 생기면', [st('사기', 1), heal(0.5), per(K), heal(0.2)]),                              // A
      O('물이 전하는 메시지', [st('사기', 1), stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }), // D — 옛 상시 엔진
      O('정령산 입구', [st('사기', 1), per(K), heal(0.2)], { tags: KAI }),                    // G
      O('잔소리의 용족', [st('사기', 1), per(K), heal(0.2), srch()]),                          // F 서치
      O('수문장의 물벼락', [per(K), dmg(0.3, EA), spendAll(K)]),                               // 재설계 · 대가
    ], [B('수문장의 긍지', 'atkUp'), B('호수의 품', 'defUp'), B('폐품 공작', 'cost')]),
    // u5 굴리기 — 인사 대신 물총: 두 발 · 물탱크 · 회복
    card(H, 5, '인사 대신 물총', 1, '공격', [hitsT(2, 0.42, E1), stk(K, 1), heal(0.4)], [
      O('신탁 1', [hitsT(2, 0.55, E1), stk(K, 1), heal(0.5)]),
      O('신탁 2', [dmg(0.45, EA), stk(K, 1), heal(0.4)]),
      O('신탁 3', [hitsT(2, 0.35, E1), stk(K, 1), power(rule('turnStart', [heal(0.3)]))], { power: true }),
      O('신탁 4', [hitsT(2, 0.42, E1), stk(K, 1), per(K), heal(0.15)]),
      O('신탁 5', [discard(1), hitsT(3, 0.45, E1), stk(K, 2)]),
    ], [B('축복 1', 'guard'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '나이아_u1');
}

// ════════════════════════════════════════════════════════════════════
// 3. 니콜 — 딜러 · 냉정. 카드마다 「테이크」 — 셋이면 오케이 컷(생성 카드). 3단계: 동료가 카드를 내면 한 컷(채우기 연계)
// ════════════════════════════════════════════════════════════════════
function nicole(j) {
  const H = '니콜', K = '테이크', OK = '니콜_t1';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '한 컷씩 쌓이는 촬영분', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.08 }],
    rules: [
      { name: '오케이 컷', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), make(OK, 1)] },
      { name: '깜짝 출연', when: { on: 'play', who: 'other', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
    ],
  };
  delete h.keywords;
  h.passives = [
    P('레디 액션', 'play', [stk(K, 1)], { limit: 1 }),
    P('느와르 니콜', 'kill', [draw(1)], { when: { mine: true }, limit: 1 }),
  ];
  Object.assign(tokenOf(j, OK), { cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(1.0, EA), st('취약', 1, EA)] });
  setCards(j, [
    // u1 열기 — 슬레이트를 친다(시동 카드)
    card(H, 1, '액션!', 0, '스킬', [stk(K, 2)], [
      O('컷! 다시!', [stk(K, 3)]),                                                          // A
      O('32테이크', [stk(K, 2), draw(1, { type: '공격' })]),                                // F 서치
      O('레디', [stk(K, 2)], { tags: KAI }),                                                // G
      O('큐 사인', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),     // D
      O('번아웃', [payPct(0.03), stk(K, 3), draw(1)]),                                      // 재설계 · 대가
    ], [B('아이스 아메리카노', 'draw'), B('캬라멜 팝콘', [stk(K, 1)]), B('조명 켜기', 'ap')]),
    // u2 터뜨리기 — 쌓인 테이크를 몽땅 먹고 다시 찍는다
    card(H, 2, '다시 갑니다', 1, '공격', [dmg(0.8), per(K), dmg(0.3), spendAll(K)], [
      O('재촬영', [dmg(1.0), per(K), dmg(0.35), spendAll(K)]),                               // A
      O('감독판', [dmg(1.1, EA), per(K), dmg(0.3, EA), spendAll(K)], { cost: 2 }),          // C 광역
      O('편집실', [dmg(0.8), per(K), dmg(0.3)], { tags: ['보존'] }),                         // 쥐는 쪽
      O('트로피 걷어차기', [per(K), dmg(0.45), spendAll(K), ifKill, ap(1)]),                  // 재설계
      O('오케이 사인', [dmg(0.8), per(K), dmg(0.3), ifStack(K, 2), make(OK, 1)]),            // 재설계 — 다 쓰지 않고 컷
    ], [B('파팝!', 'power'), B('와바박', 'weakSpot'), B('조감독', 'cost')]),
    // u3 굴리기 — NG 컷: 치고 한 컷
    card(H, 3, 'NG 컷', 1, '공격', [dmg(1.0), stk(K, 1)], [
      O('NG 연발', [dmg(1.3), stk(K, 1)]),                                                  // A
      O('화염방사기', [dmg(0.75, EA), stk(K, 1)]),                                           // G 광역
      O('촬영본 태우기', [dmg(1.0), burn(1), draw(1)]),                                      // 재설계 · 대가
      O('다크서클', [dmg(1.0), stk(K, 1), ifBroken, stk(K, 2)]),                             // E
      O('콘티 확인', [dmg(1.0), stk(K, 1), srch()]),                                         // F 서치
    ], [B('팝콘 폭탄', 'power'), B('필름 감기', [stk(K, 1)]), B('컷 사인', 'draw')]),
    // u4 마무리 — 원작 저학년: 주연 배우를 불러 받는 피해↑ · 다섯 번 · 마지막 큰 타격(2코 하나)
    card(H, 4, '초천재의 연출', 2, '공격', [st('취약', 1, EA), hitsT(5, 0.3, ER), dmg(0.5)], [
      O('천재의 컷', [st('취약', 1, EA), hitsT(5, 0.38, ER), dmg(0.6)]),                    // A
      O('열연의 흔적', [st('취약', 1, EA), hitsT(5, 0.3, ER), later(1, [dmg(0.6, EA)])]),
      O('천재 감독의 시야', [st('취약', 1, EA), hitsT(5, 0.3, ER), per(K), dmg(0.25)]),       // F
      O('저예산 단편', [st('취약', 1, EA), hitsT(4, 0.28, ER), dmg(0.45)], { cost: 1 }),      // B
      O('블록버스터', [hitsT(8, 0.34, ER), per(K), dmg(0.3, EA), spendAll(K)], { cost: 3 }), // 재설계 · 대가
    ], [B('크헤헷', 'power'), B('팝콘 리필', 'draw'), B('촬영 일정 당기기', 'cost')]),
    // u5 유틸(0코 비용) — 캬라멜 팝콘 한 줌: 테이크 + 손의 공격 카드 비용 -1
    card(H, 5, '캬라멜 팝콘 한 줌', 0, '스킬', [stk(K, 1), csOwn({ type: '공격' })], [
      O('신탁 1', [stk(K, 2), csOwn({ type: '공격' })]),
      O('신탁 2', [stk(K, 1), srch({ type: '공격' })]),
      O('신탁 3', [dmg(0.4), stk(K, 1), power(rule('stackReach', [draw(1)], { when: { id: K, n: 3 } }))], { power: true }),
      O('신탁 4', [stk(K, 1), csOwn({ type: '공격' }), inspire, stk(K, 2)]),
      O('신탁 5', [payPct(0.03), stk(K, 2), csOwn({ type: '공격' })]),
    ], [B('축복 1', 'atkUp'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '니콜_u1');
}

// ════════════════════════════════════════════════════════════════════
// 4. 라이카 — 딜러 · 순수(변신). 「과충전」 셋이면 최대 출력. 3단계: 시동 = 개전 강화 「급속 충전!」(매 턴 충전) · ④ 풀 스윙(1코 마무리)
// ════════════════════════════════════════════════════════════════════
function laika(j) {
  const H = '라이카', K = '과충전';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '제 몸에 꽂은 220V', carrier: 'self', cap: 3, onMax: { form: '라이카_최대출력' }, per: [{ stat: 'dealt', v: 0.2 }] };
  delete h.keywords;
  h.passives = [
    P('220V 콘센트', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    P('번개 펀치', 'stackReach', [hitsT(5, 0.2, ER)], { when: { id: K, n: 3 } }),
  ];
  const eng = () => power(rule('turnStart', [stk(K, 1)]));
  setCards(j, [
    // u1 열기 — 원작 저학년: 제 몸에 감전(개전 강화 시동 카드 — 매 턴 충전)
    card(H, 1, '급속 충전!', 1, '강화', [payPct(0.02), stk(K, 2), eng()], [
      O('터보 엔진 배터리', [payPct(0.02), stk(K, 3), eng()], { tags: KAI, power: true }),     // A
      O('카페 충전', [stk(K, 2), draw(1), eng()], { tags: KAI, power: true }), // 엔진 한 단계
      O('발전소 출근', [stk(K, 2), srch({ type: '공격' }), eng()], { tags: KAI, power: true }),  // F 서치
      O('110V는 싫어', [stk(K, 3), eng()], { power: true }),                    // H 개전 떼기
      O('찌릿찌릿', [stk(K, 2), draw(1), { k: 'atkMod', v: 0.1, run: true }], { tags: KAI }),                                    // 재설계 — 한 번에 몰아 충전
    ], [B('효율 만땅', 'draw'), B('절전 모드', 'cost'), B('전력 공급', [stk(K, 1)])], { tags: KAI }),
    // u2 터뜨리기 — 감전된 몸으로 질주(과충전 1개당 · 다 씀 — 최대 출력과 맞바꿈)
    card(H, 2, '감전 질주', 1, '공격', [dmg(0.6, EA), per(K), dmg(0.15, EA), spendAll(K)], [
      O('전력 질주', [dmg(0.75, EA), per(K), dmg(0.18, EA), spendAll(K)]),                   // A
      O('충전된 몸', [dmg(0.6, EA), ifStack(K, 2), dmg(0.6, EA)]),                           // 재설계
      O('일직선 돌파', [dmg(1.0), per(K), dmg(0.3), spendAll(K)]),                           // G 단일
      O('경기장 정전', [per(K), dmg(0.45, EA), spendAll(K), st('충격', 1, EA)], { cost: 2 }), // C 충격 광역
      O('방전 직전', [discard(1), dmg(0.75, EA), per(K), dmg(0.2, EA)]),                     // 재설계 · 대가 — 다 쓰지 않음
    ], [B('고전압', 'power'), B('감전된 바닥', 'frost'), B('스파크', 'ap')]),
    // u3 굴리기 — 원작 평타 두 번 + 감전(충격 담당)
    card(H, 3, '220V 스트레이트', 1, '공격', [hitsT(2, 0.55, E1), st('충격', 1, E1)], [
      O('원투 펀치', [hitsT(2, 0.7, E1), st('충격', 1, E1)]),                                 // A
      O('도루', [hitsT(2, 0.55, E1), st('충격', 1, E1), draw(1, { type: '공격' })]),          // F 서치
      O('강속구', [hitsT(2, 0.55, E1), st('충격', 1, E1), ifBroken, hitsT(2, 0.5, E1)]),      // E
      O('가운뎃손가락', [hitsT(2, 0.5, E1), st('충격', 1, E1)], { cost: 0 }),                 // B
      O('정의의 주먹', [hitsT(2, 0.55, E1), stk(K, 2)]),                                     // 재설계 — 충격 대신 충전
    ], [B('하핫', 'draw'), B('볼 당기기 금지', 'ap'), B('도루 사인', 'cost')]),
    // u4 마무리 — 풀 스윙(과충전 1개당 · 쓰지 않아 최대 출력을 지킨다)
    card(H, 4, '풀 스윙', 1, '공격', [dmg(0.6), per(K), dmg(0.3)], [
      O('홈런', [dmg(0.75), per(K), dmg(0.36)]),                                             // A
      O('찌릿 시그널', [dmg(1.0), power(rule('play', [xtra(0.35)], { when: { type: '공격' }, limit: 1 }))], { power: true }), // D
      O('전기세 폭탄', [payPct(0.03), stk(K, 2), per(K), dmg(0.45)]),                        // 재설계 · 대가
      O('번트', [dmg(0.5), per(K), dmg(0.25)], { cost: 0 }),                                 // B
      O('지니어스 분석', [dmg(0.6), per(K), dmg(0.3), ifStack(K, 3), srch()]),               // F 서치 · 조건
    ], [B('최첨단 배터리', 'power'), B('절연 장갑', 'defUp'), B('충전 케이블', [stk(K, 1)])]),
    // u5 유틸(0코 회수) — 전기 지짐이: 과충전 + 버린 공격 카드 한 장
    card(H, 5, '전기 지짐이', 0, '스킬', [stk(K, 1), pullD({ type: '공격' })], [
      O('신탁 1', [stk(K, 2), pullD({ type: '공격' })]),
      O('신탁 2', [stk(K, 1), srch({ type: '공격' }), draw(1)]),
      O('신탁 3', [stk(K, 1), pullD({ type: '공격' }), power(rule('stackReach', [draw(1), sh(0.5)], { when: { id: K, n: 3 } }))], { power: true }),
      O('신탁 4', [stk(K, 1), pullD({ type: '공격' }), ifStack(K, 3), draw(1)]),
      O('신탁 5', [payPct(0.03), stk(K, 2), pullD({ type: '공격' })]),
    ], [B('축복 1', 'guard'), B('축복 2', 'atkUp'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '라이카_u1');
}

// ════════════════════════════════════════════════════════════════════
// 5. 뮤트 — 서포터(AP) · 순수. 가장 튼튼한 적을 「홀로그램」 여섯 겹으로 — 다 차면 에러 메시지 · 파티 넷째 카드마다 AP 주유
// ════════════════════════════════════════════════════════════════════
function mute(j) {
  const H = '뮤트', K = '홀로그램';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '표적을 에워싸는 홀로그램 그물', carrier: 'enemy', cap: 6, per: [{ stat: 'dot', ratio: 0.15 }],
    rules: [{ name: '에러 메시지', when: { on: 'stackReach', id: K, n: 6 }, fx: [spendAll(K), dmg(1.6), st('취약', 2, E1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('웹트래핑', 'turnStart', [stk(K, 2, TOP)]),
    P('SP 주유', 'play', [ap(1), draw(1)], { when: { who: 'any', nth: 4 } }),
  ];
  setCards(j, [
    // u1 열기 — 원작 저학년: 한 적에게 홀로그램 포위(시동 카드)
    card(H, 1, '웹트래핑', 0, '스킬', [stk(K, 3, E1)], [
      O('9중 포위', [stk(K, 5, E1)]),                                                       // A
      O('성공률 계산', [stk(K, 3, E1), srch()]),                                             // F 서치
      O('다크넷 관찰', [stk(K, 2, E1), power(rule('turnStart', [stk(K, 1, TOP)]))], { power: true }), // D
      O('홀로그램 변장', [stk(K, 4, E1)], { tags: ['보존'] }),                               // G
      O('데이터 덤프', [stk(K, 2, E1), ap(1), discard(1)]),                                   // 재설계 · 대가
    ], [B('데이터 분석', 'draw'), B('확률 맹신', [stk(K, 1, E1)]), B('규칙과 학습', { tags: ['보존'] })]),
    // u2 터뜨리기 — 그물을 걷어 한 방(1개당 · 다 씀)
    card(H, 2, '강제 종료', 1, '공격', [dmg(0.7), per(K), dmg(0.15), spendAll(K)], [
      O('강제 포맷', [dmg(0.9), per(K), dmg(0.18), spendAll(K)]),                            // A
      O('시스템 다운', [dmg(1.0, EA), per(K), dmg(0.15, EA), spendAll(K)], { cost: 2 }),     // C 광역
      O('싸늘한 말투', [discard(1), dmg(0.9), per(K), dmg(0.15)]),                           // 재설계 · 대가 — 걷지 않음
      O('일괄 종료', [dmg(0.5, EA), perEach(K), dmg(0.08, EA)]),                             // 쥐는 쪽 광역
      O('침묵', [dmg(0.5), per(K), dmg(0.15), ifKill, draw(2)]),                             // 재설계
    ], [B('오버클럭', 'power'), B('취약점 공격', 'weakSpot'), B('빠른 실행', 'cost')]),
    // u3 굴리기 — 원작 평타 와이어 함정
    card(H, 3, '와이어 함정', 1, '공격', [dmg(0.9), stk(K, 2, E1)], [
      O('정밀 와이어', [dmg(1.15), stk(K, 2, E1)]),                                          // A
      O('와이어 그물', [dmg(0.75, EA), stk(K, 2, EA)]),                                      // G 광역
      O('기본 공격 감지', [dmg(0.9), stk(K, 2, E1), ifStack(K, 5), dmg(0.6)]),               // E
      O('나타의 손', [dmg(0.9), stk(K, 2, E1), draw(1, { who: 'other' })]),                  // F 서치
      O('물이 무서워', [stk(K, 3, E1), sh(1.4)]),                                            // 재설계 — 공격 ↔ 방어
    ], [B('강철 와이어', 'power'), B('엉킨 실', 'frost'), B('실뜨기', [stk(K, 1, E1)])]),
    // u4 완성형 — 원작 어사이드 「뮤트의 뜻대로」(상시 — 사기 담당 · 매 턴 가장 튼튼한 적에게 그물)
    card(H, 4, '뮤트의 뜻대로', 1, '강화', [st('사기', 1), power(rule('turnStart', [stk(K, 1, TOP)]))], [
      O('완전한 평화', [st('사기', 1), power(rule('turnStart', [stk(K, 2, TOP)]))]),         // A
      O('실뜨기 전문가', [st('사기', 1), power(rule('turnStart', [stk(K, 1, TOP)]), rule('stackReach', [draw(1)], { when: { id: K, n: 6 } }))]),
      O('언젠가 우리', [st('사기', 1), stk(K, 2, E1), power(rule('turnStart', [stk(K, 1, TOP)]))], { tags: KAI }),
      O('한 시간 연설', [st('사기', 1), stk(K, 3, E1), ap(1)]),                              // 재설계 — 한 번에
      O('교단 쪽 첩보', [st('사기', 1), draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1, TOP)]))]), // F 서치
    ], [B('엘레나와 함께', 'atkUp'), B('산들바람', 'defUp'), B('규칙 준수', 'ap')]),
    // u5 유틸(0코 동료 서치) — 옆집 반찬 돌리기: 홀로그램 둘 + 동료 카드
    card(H, 5, '옆집 반찬 돌리기', 0, '스킬', [stk(K, 2, E1), draw(1, { who: 'other' })], [
      O('신탁 1', [stk(K, 3, E1), draw(1, { who: 'other' })]),
      O('신탁 2', [stk(K, 2, E1), draw(2, { who: 'other' })]),
      O('신탁 3', [dmg(0.4), stk(K, 1, E1), power(rule('stackReach', [ap(1)], { when: { id: K, n: 6 }, limit: 1 }))], { power: true }),
      O('신탁 4', [stk(K, 2, E1), draw(1, { who: 'other' }), ifStack(K, 4), dmg(0.6)]),
      O('신탁 5', [payPct(0.03), stk(K, 3, E1), draw(1, { who: 'other' })]),
    ], [B('축복 1', 'guard'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  starter(j, '뮤트_u1');
}

// ════════════════════════════════════════════════════════════════════
// 6. 미로 — 탱커 · 활발(원작 방식 고학년 「거울 속」 · 「반사된 시선」 그대로). 파티를 친 적에게 「시선」 — 「깨진 거울 조각」으로 되돌림
// ════════════════════════════════════════════════════════════════════
function miro(j) {
  const H = '미로', K = '시선';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '파티를 친 적에게 꽂히는 눈', carrier: 'enemy', cap: 3, per: [{ stat: 'dealt', v: -0.08 }] };
  const mirror = h.passives.find(p => p.name === '거울에 맺힌 공격');
  h.passives = [P('지켜보고 있어요', 'hurt', [stk(K, 1, E1)], { when: { guarded: true }, limit: 3 }), mirror];
  setCards(j, [
    // u1 굴리기 — 거울 빛으로 치고 막는다
    card(H, 1, '거울 반사', 1, '공격', [ddef(0.45), stk(K, 1, E1), sh(0.5)], [
      O('눈부신 빛', [ddef(0.6), stk(K, 1, E1), sh(0.5)]),                                  // A
      O('세 갈래 빛', [ddef(0.35, EA), stk(K, 1, EA), sh(0.5)]),                            // G 광역
      O('반사각', [ddef(0.45), stk(K, 1, E1), ifStack(K, 3), ddef(0.45)]),                  // 재설계 — 막기 대신 되쏘기
      O('넘어지며 박치기', [ddef(0.75), stk(K, 1, E1)]),
      O('거울 너머', [ddef(0.45), stk(K, 1, E1), srch()]),                                  // F 서치
    ], [B('눈부심', 'power'), B('거울 파편', 'frost'), B('좌우 반전', [stk(K, 1, E1)])]),
    // u2 열기 — 원작 저학년 「이야기를 듣고 싶어」: 거울을 세워 모든 눈길을 모은다(시동 카드)
    card(H, 2, '거울 속으로', 1, '스킬', [sh(1.1), stk(K, 1, EA)], [
      O('거울 사이로', [sh(1.4), stk(K, 1, EA)]),                                           // A
      O('수면에 비친 동심', [sh(1.1), stk(K, 1, EA), srch({ type: '공격' })]),                // F 서치
      O('거울 깨뜨리기', [discard(1), sh(1.5), stk(K, 1, EA)]),                             // H 버리기
      O('겁쟁이 거울', [sh(1.6), ifWounded, heal(0.6)]),                                    // 재설계
      O('교주의 방 거울', [sh(0.8), power(rule('turnStart', [stk(K, 1, EA)]))], { power: true }), // D
    ], [B('맑은 거울', 'guard'), B('헤헤', 'draw'), B('거울 너머 응원', [stk(K, 1, EA)])]),
    // u3 완성형 — 원작 어사이드 「이젠 내가 지켜줄 거야⋯!」(상시 — 피해 감소 · 매 턴 끝 실드)
    card(H, 3, '이젠 내가 지켜줄 거야', 1, '강화', [st('피해 감소', 1), power(rule('turnEnd', [sh(0.35)]))], [
      O('사이드 미로', [st('피해 감소', 1), power(rule('turnEnd', [sh(0.45)]))]),            // A
      O('지켜보기만 하는 건 싫어', [st('피해 감소', 1), sh(0.4), power(rule('turnEnd', [sh(0.35)]), rule('lowHp', [sh(1.5)], { when: { pct: 0.4 } }))]),
      O('셋째 딸', [st('피해 감소', 1), power(rule('turnEnd', [sh(0.35)]))], { tags: KAI }),
      O('민트초코', [st('결정화', 1), sh(0.6), stk(K, 1, EA)]),                          // 재설계 — 한 번에
      O('상담사', [st('피해 감소', 1), draw(1, { who: 'other' }), power(rule('turnEnd', [sh(0.35)]))]), // F 서치
    ], [B('작은 응원', 'defUp'), B('거울 닦기', 'cost'), B('혼잣말 대꾸', 'ap')]),
    // u4 터뜨리기 — 시선을 깨진 거울 조각으로 되돌린다(1개당 · 다 씀)
    card(H, 4, '깨진 거울 조각', 1, '공격', [ddef(0.4), per(K), ddef(0.2), spendAll(K)], [
      O('산산조각', [ddef(0.5), per(K), ddef(0.25), spendAll(K)]),                          // A
      O('파편 비', [ddef(0.3, EA), perEach(K), ddef(0.12, EA)]),                            // 쥐는 쪽 광역
      O('금 간 거울', [discard(1), ddef(0.5), per(K), ddef(0.25)]),                          // 재설계 · 대가
      O('거울 폭발', [per(K), ddef(0.5), spendAll(K), st('기절', 1, E1)], { cost: 2 }),      // C 기절
      O('다시 맞추기', [per(K), sh(0.5), spendAll(K), heal(0.4)]),                           // 재설계 — 공격 ↔ 방어
    ], [B('날카로운 조각', 'power'), B('반짝이는 조각', 'weakSpot'), B('작은 조각', 'cost')]),
    // u5 유틸(0코 동료 서치) — 거울처럼 따라 하기: 작은 실드 · 시선 · 동료 카드
    card(H, 5, '거울처럼 따라 하기', 0, '스킬', [sh(0.5), stk(K, 1, EA), draw(1, { who: 'other' })], [
      O('신탁 1', [sh(0.7), stk(K, 1, EA), draw(1, { who: 'other' })]),
      O('신탁 2', [sh(0.5), draw(2, { who: 'other' })]),
      O('신탁 3', [sh(0.5), stk(K, 1, EA), power(rule('hurt', [draw(1)], { when: { guarded: true }, limit: 1 }))], { power: true }),
      O('신탁 4', [sh(0.5), draw(1, { who: 'other' }), inspire, sh(1.0)]),
      O('신탁 5', [discard(1), sh(1.1), stk(K, 2, EA)]),
    ], [B('축복 1', 'heal'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1, EA)])]),
  ]);
  starter(j, '미로_u2');
}

// ════════════════════════════════════════════════════════════════════
// 7. 블랑셰 — 딜러 · 우울. 공격마다 적에게 「푸른 장미」 — 넷이면 만개(큰 피해 · 강인도 · 커튼콜)
// ════════════════════════════════════════════════════════════════════
function blanchet(j) {
  const H = '블랑셰', K = '푸른 장미', CC = '블랑셰_t1';
  const h = j.heroes[0];
  renameUlt(h, '붉은 장미', K, { target: EA });
  h.keyword = {
    name: K, desc: '기적을 바라며 피는 꽃', carrier: 'enemy', cap: 4, per: [{ stat: 'taken', v: 0.06 }],
    rules: [{ name: '만개', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(1.3), tough(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('싱크로즈', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 3 }),
    P('백만 송이 피어나', 'stackReach', [make(CC, 1)], { when: { id: K, n: 4 }, limit: 1 }),
  ];
  Object.assign(tokenOf(j, CC), { cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(0.8, EA), st('사기', 1)] });
  const bloomDraw = (fx = [draw(1)]) => rule('stackReach', fx, { when: { id: K, n: 4 } });
  setCards(j, [
    // u1 굴리기 — 원작 저학년 싱크로즈: 세 번 튕긴다
    card(H, 1, '싱크로즈', 1, '공격', [hitsT(3, 0.42, ER)], [
      O('기적의 꽃말', [hitsT(3, 0.52, ER)]),                                               // A
      O('확정 치명', [hitsT(3, 0.4, E1), ifStack(K, 3), dmg(0.6)]),                         // E
      O('장미 넝쿨', [stk(K, 1, EA), hitsT(3, 0.35, ER), draw(1, { type: '공격' })]),        // 재설계 · 서치
      O('흩날리는 파랑새', [hitsT(5, 0.42, ER), stk(K, 1, EA)], { cost: 2 }),               // C 장미 광역
      O('무대 인사', [hitsT(3, 0.42, ER)], { tags: ['약점 공격'] }),                         // G
    ], [B('푸른 가시', 'power'), B('파랑새의 눈', 'ap'), B('가벼운 줄기', 'cost')]),
    // u2 터뜨리기 — 정원의 장미를 꺾는다(1송이당 · 다 씀)
    card(H, 2, '장미 정원 산책', 1, '공격', [dmg(0.8), per(K), dmg(0.2), spendAll(K)], [
      O('만개한 정원', [dmg(1.0), per(K), dmg(0.25), spendAll(K)]),                          // A
      O('정원 손질', [dmg(0.8), per(K), dmg(0.2)], { tags: ['보존'] }),                      // 쥐는 쪽
      O('붉어진 장미', [per(K), dmg(0.35), spendAll(K), ifKill, stk(K, 2, EA)]),             // 재설계
      O('가시 줄기', [dmg(0.55, EA), perEach(K), dmg(0.15, EA)]),                            // 광역 · 쥐는 쪽
      O('시상식의 액트리스', [per(K), dmg(0.6), spendAll(K), make(CC, 1)], { cost: 2 }),      // C 커튼콜
    ], [B('장미 향', 'power'), B('새벽 산책', 'draw'), B('정원 지름길', 'cost')]),
    // u3 열기 — 앙코르: 한 적에게 장미를 심고 손을 굴린다(시동 카드)
    card(H, 3, '앙코르', 0, '스킬', [stk(K, 2, E1), draw(1)], [
      O('커튼 뒤', [stk(K, 3, E1), draw(1)]),                                               // A
      O('대본 읽기', [stk(K, 3, E1), pullD({ type: '공격' })]),                              // 재설계 · 서치
      O('처음 연기한 대본', [stk(K, 2, E1), draw(1)], { tags: KAI }),                        // G
      O('파랑새 돌보기', [stk(K, 1, E1), draw(1), power(rule('turnStart', [stk(K, 1, TOP)]))], { power: true }), // D
      O('표정 연습', [payPct(0.03), stk(K, 3, E1), draw(1)]),                                // H HP
    ], [B('푸른 꽃향기', 'draw'), B('후후', [stk(K, 1, E1)]), B('조명 아래', { tags: ['보존'] })]),
    // u4 완성형 — 무대 위의 배우(상시 — 치명 · 만개마다 드로우)
    card(H, 4, '무대 위의 배우', 1, '강화', [{ k: 'critMod', v: 0.15, run: true }, power(bloomDraw())], [
      O('주연 배우', [{ k: 'critMod', v: 0.2, run: true }, power(bloomDraw())]),              // A
      O('성대모사', [{ k: 'critMod', v: 0.15, run: true }, payPct(0.03), power(bloomDraw([draw(1), ap(1)]))]), // H HP
      O('응원단 비주얼', [{ k: 'critMod', v: 0.15, run: true }, stk(K, 2, E1), power(bloomDraw())], { tags: KAI }),
      O('배역 몰입', [{ k: 'critMod', v: 0.15, run: true }, stk(K, 3, E1), hitsT(2, 0.4, E1)]),                                    // 재설계 — 한 번에
      O('백만송이 푸른장미', [{ k: 'critMod', v: 0.15, run: true }, stk(K, 1, E1), power(rule('play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }))]),
    ], [B('검은 드레스', 'atkUp'), B('스포트라이트', 'ap'), B('파랑새 날개', [stk(K, 1, EA)])]),
    // u5 유틸(0코 비용) — 모노레일 출퇴근: 장미 + 손의 공격 카드 비용 -1
    card(H, 5, '모노레일 출퇴근', 0, '스킬', [stk(K, 1, E1), csOwn({ type: '공격' })], [
      O('신탁 1', [stk(K, 2, E1), csOwn({ type: '공격' })]),
      O('신탁 2', [stk(K, 1, E1), srch({ type: '공격' })]),
      O('신탁 3', [dmg(0.4), stk(K, 1, E1), power(rule('stackReach', [csOwn({ type: '공격' })], { when: { id: K, n: 4 } }))], { power: true }),
      O('신탁 4', [stk(K, 1, E1), csOwn({ type: '공격' }), inspire, stk(K, 2, E1)]),
      O('신탁 5', [payPct(0.03), stk(K, 2, EA), draw(1)]),
    ], [B('축복 1', 'guard'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  starter(j, '블랑셰_u3');
  for (const e of j.equips || []) e.affinityEffect = [P('파랑새의 표식', 'fightStart', [stk(K, 1, EA)])];
}

// ════════════════════════════════════════════════════════════════════
// 8. 빅우드 — 탱커 · 순수. 맞을수록 「나이테」 — 다섯이면 현자 모드로 황금 사과(생성 카드). 3단계: 기본 카드 연료(열매 맺기) · ④ 1코 마무리
// ════════════════════════════════════════════════════════════════════
function bigwood(j) {
  const H = '빅우드', K = '나이테', APL = '빅우드_apple';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '맞으며 늘어난 나이테', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.12 }],
    rules: [{ name: '현자 모드', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), make(APL, 2)] }],
  };
  delete h.keywords;
  h.passives = [
    P('더 세게~', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 3 }),
    P('피톤치드', 'fightStart', [st('피해 감소', 1)]),
  ];
  addToken(j, { id: APL, name: '황금 사과', hero: H, token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.6), sh(0.6)], blurb: '빅우드가 기꺼이 뜯겨 주는 황금빛 열매입니다.' });
  setCards(j, [
    // u1 굴리기 — 원작 저학년: 제 몸에 보호막
    card(H, 1, '자연을 지키자', 1, '스킬', [sh(1.3), stk(K, 1)], [
      O('나무껍질 갑옷', [sh(1.65), stk(K, 1)]),                                             // A
      O('세계수 큰형님', [sh(2.4), stk(K, 2), make(APL, 1)], { cost: 2 }),                  // C 사과 덤
      O('뿌리 깊은 나무', [sh(1.1), stk(K, 1), per(K), sh(0.15)]),                          // F
      O('불도 물도 싫어', [sh(1.3), stk(K, 1), ifWounded, heal(1.0)]),                       // 재설계 · 조건
      O('후욱', [sh(1.3), stk(K, 1)], { tags: ['보존'] }),                                  // G
    ], [B('두꺼운 껍질', 'guard'), B('나뭇잎 하나', [stk(K, 1)]), B('뿌리 박기', 'defUp')]),
    // u2 열기 — 열매를 나눠 준다(시동 카드)
    card(H, 2, '열매 나눠 주기', 0, '스킬', [make(APL, 1), stk(K, 1)], [
      O('주렁주렁', [make(APL, 2), stk(K, 1)]),                                             // A
      O('베이커리 납품', [make(APL, 1), stk(K, 1), draw(1, { who: 'other' })]),             // F 서치
      O('요정들의 관심', [make(APL, 1), stk(K, 1), sh(0.4)], { tags: KAI }),
      O('무보수 봉사', [make(APL, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }), // D
      O('열매 맺기', [exB, make(APL, 2), stk(K, 1)]),                                            // 재설계 — 기본 카드를 사과로
    ], [B('탄수화물', 'draw'), B('잘 익은 열매', [make(APL, 1)]), B('가지 흔들기', { tags: ['보존'] })]),
    // u3 터뜨리기 — 나이테를 다 굴린다(1개당 · 다 씀)
    card(H, 3, '통나무 굴리기', 1, '공격', [ddef(0.45), per(K), ddef(0.13), spendAll(K)], [
      O('굴러간다~', [ddef(0.55), per(K), ddef(0.16), spendAll(K)]),                         // A
      O('숲 정리', [ddef(0.3, EA), per(K), ddef(0.09, EA), spendAll(K)]),                   // G 광역
      O('나 잡아 봐라~', [discard(1), ddef(0.55), per(K), ddef(0.16)]),                      // 재설계 · 대가
      O('큰 나무 쓰러뜨리기', [ddef(0.9), per(K), ddef(0.22), st('기절', 1, E1)], { cost: 2 }), // C 기절
      O('맞은 만큼', [ddef(0.45), per(K), ddef(0.13), make(APL, 1)]),                         // 재설계 — 다 쓰지 않고 사과
    ], [B('묵직한 통나무', 'power'), B('나뭇결', 'frost'), B('가벼운 가지', 'cost')]),
    // u4 마무리 — 숲의 품(결정화 담당 · 쥔 나이테 1개당 실드)
    card(H, 4, '숲의 품', 1, '스킬', [st('결정화', 1), per(K), sh(0.3)], [
      O('오래된 숲', [st('결정화', 1), per(K), sh(0.4)]),                                    // A
      O('현자의 말씀', [st('결정화', 2), power(rule('hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }))], { power: true }), // D — 옛 상시 엔진
      O('가을 수확', [spendAll(K), make(APL, 2), sh(0.6)]),                                  // 재설계 · 대가
      O('요정 왕국에 뿌리', [st('결정화', 1), per(K), sh(0.3)], { tags: KAI }),               // G
      O('메이드장', [st('결정화', 1), per(K), sh(0.3), draw(1, { who: 'other' })]),           // F 서치
    ], [B('나무 그늘', 'defUp'), B('새싹', 'cost'), B('맑은 공기', [st('피해 감소', 1)])]),
    // u5 굴리기 — 메이드장의 채찍: 치고 막고 나이테
    card(H, 5, '메이드장의 채찍', 1, '공격', [ddef(0.6), stk(K, 1), sh(0.5)], [
      O('신탁 1', [ddef(0.75), stk(K, 1), sh(0.6)]),
      O('신탁 2', [ddef(0.4, EA), stk(K, 1), sh(0.5)]),
      O('신탁 3', [ddef(0.5), sh(0.5), power(rule('turnEnd', [stk(K, 1)]))], { power: true }),
      O('신탁 4', [ddef(0.45), per(K), ddef(0.12), stk(K, 1)]),
      O('신탁 5', [discard(1), ddef(0.9), make(APL, 1)]),
    ], [B('축복 1', 'ap'), B('축복 2', 'heal'), B('축복 3', [make(APL, 1)])]),
  ]);
  starter(j, '빅우드_u2');
}

// ════════════════════════════════════════════════════════════════════
// 9. 시저 — 서포터(회복) · 냉정. 가장 센 적을 「방해꾼」으로 찍어 약하게 — 쓰러뜨리면 파티 회복
//    3단계: 시동 = 개전 강화 「한정판의 맛」(옛 「빌런의 품격」 엔진 — 사기 · 매 턴 찍기) · ④ 「빌런의 품격」 1코 마무리
// ════════════════════════════════════════════════════════════════════
function scizor(j) {
  const H = '시저', K = '방해꾼';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '빌런이 찍어 둔 훼방꾼', carrier: 'enemy', cap: 3, hunt: true, per: [{ stat: 'dealt', v: -0.1 }] };
  delete h.keywords;
  h.passives = [
    P('싹둑 표적', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 1 }),
    P('의외로 빌런이 이기는 전개', 'huntDown', [heal(0.8), stk(K, 1, TOP)]),
  ];
  const mark = (n = 1) => rule('turnStart', [stk(K, n, TOP)]);
  setCards(j, [
    // u1 열기 — 호강을 맛보게 한 뒤 매 턴 찍는다(개전 강화 시동 카드)
    card(H, 1, '한정판의 맛', 1, '강화', [st('사기', 1), power(mark())], [
      O('성장형 빌런', [st('사기', 1), power(mark(2))], { tags: KAI, power: true }),           // A
      O('스핑크스의 라이벌', [st('사기', 1), power(mark(), rule('huntDown', [draw(1)]))], { tags: KAI, power: true }),
      O('외래종 교도소 출소', [st('사기', 1), heal(0.8), power(mark())], { tags: KAI, power: true }),
      O('예고장', [st('사기', 1), stk(K, 2, E1), power(mark())], { power: true }),             // H 개전 떼기
      O('최고급 헤어스타일', [st('사기', 1), stk(K, 2, E1), srch()], { tags: KAI }),              // 재설계 · 서치 — 한 번에
    ], [B('길고양이 밥', 'draw'), B('천상천하 유아독존', [stk(K, 1, E1)]), B('빌런 코믹스', 'cost')], { tags: KAI }),
    // u2 굴리기 — 원작 저학년: 머리 위에서 여섯 번
    card(H, 2, '보이는 실', 1, '공격', [hitsT(3, 0.33, E1), stk(K, 1, E1)], [
      O('여섯 번 싹둑', [hitsT(6, 0.21, E1), stk(K, 1, E1)]),                                // A
      O('빌런의 수하', [hitsT(3, 0.33, E1), stk(K, 1, E1), heal(0.5)]),
      O('SP 빼앗기', [hitsT(3, 0.33, E1), stk(K, 1, E1), ifStack(K, 2), draw(1)]),           // 재설계 · 조건
      O('가위 바위', [hitsT(3, 0.33, E1), stk(K, 1, E1)], { tags: ['약점 공격'] }),           // G
      O('인터넷 선 자르기', [hitsT(3, 0.4, E1), stk(K, 1, E1), discard(1)]),                 // H 버리기
    ], [B('잘 드는 가위', 'power'), B('핑킹가위', 'frost'), B('가벼운 손놀림', 'ap')]),
    // u3 터뜨리기 — 찍어 둔 방해꾼을 싹둑(1개당 · 다 씀)
    card(H, 3, '싹둑싹둑', 1, '공격', [dmg(0.75), per(K), dmg(0.25), spendAll(K)], [
      O('가지치기 연습', [dmg(0.95), per(K), dmg(0.3), spendAll(K)]),                         // A
      O('미용실 예약', [dmg(0.8), per(K), dmg(0.27), spendAll(K)], { tags: ['보존'] }),       // G
      O('남 탓', [dmg(0.75), per(K), dmg(0.25), heal(0.6)]),                                 // 재설계 — 쥐고 회복
      O('악어 이빨', [per(K), dmg(0.45), spendAll(K), ifKill, ap(1)]),                        // 재설계
      O('대형 가위', [per(K), dmg(0.75), spendAll(K), heal(1.2)], { cost: 2 }),               // C 회복
    ], [B('아하핫', 'power'), B('뾰족한 이빨', 'weakSpot'), B('색종이 오리기', 'draw')]),
    // u4 마무리 — 원작 어사이드 「빌런의 품격」: 찍힌 방해꾼 1개당 파티 회복(쥐는 쪽)
    card(H, 4, '빌런의 품격', 1, '공격', [dmg(0.6), per(K), heal(0.2)], [
      O('빌런의 미소', [dmg(0.7), per(K), heal(0.25)]),                                      // A
      O('시저는 이런 게 시저~', [dmg(0.4), power(rule('huntDown', [heal(0.6), ap(1)]))], { power: true }), // D
      O('악어 비니', [dmg(0.6), per(K), heal(0.2)], { cost: 0 }),                            // B
      O('남 탓하기', [per(K), heal(0.35), spendAll(K), draw(1)]),                            // 재설계 · 대가
      O('빌런 연합', [dmg(0.6), per(K), heal(0.2), draw(1, { who: 'other' })]),               // F 서치
    ], [B('악어 잠옷', 'heal'), B('천연 미용실', 'cost'), B('찍어 둔 표적', [stk(K, 1, E1)])]),
    // u5 굴리기(회복) — 화단 물 주기: 파티 회복 · 방해꾼 · 작은 실드
    card(H, 5, '화단 물 주기', 1, '스킬', [heal(0.8), stk(K, 1, E1), sh(0.4)], [
      O('신탁 1', [heal(1.0), stk(K, 1, E1), sh(0.5)]),
      O('신탁 2', [heal(0.6), stk(K, 1, E1)], { cost: 0 }),
      O('신탁 3', [heal(0.6), stk(K, 1, E1), power(rule('turnEnd', [heal(0.2), stk(K, 1, TOP)]))], { power: true }),
      O('신탁 4', [heal(0.8), stk(K, 1, E1), per(K), heal(0.15)]),
      O('신탁 5', [discard(1), heal(1.1), stk(K, 2, E1)]),
    ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  starter(j, '시저_u1');
}

// ════════════════════════════════════════════════════════════════════
// 10. 실라 — 딜러 · 냉정. 한 적과 「정면 승부」. 3단계: 동료의 공격이 끼어들면 승부 하나가 깨진다(끊기 연계) — 대신 1개당 받는 피해가 크다
// ════════════════════════════════════════════════════════════════════
function sylla(j) {
  const H = '실라', K = '정면 승부';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '한 적과 마주 선 승부', carrier: 'enemy', cap: 3, hunt: true, per: [{ stat: 'taken', v: 0.13 }],
    rules: [
      { name: '바람의 정령', when: { on: 'stackReach', id: K, n: 3 }, fx: [tough(1)] },
      { name: '끼어들기', when: { on: 'play', who: 'other', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [{ k: 'spend', id: K, v: 1, target: 'markedEnemy' }] },
    ],
  };
  delete h.keywords;
  h.passives = [
    P('맞바람', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }),
    P('정령의 수호자', 'huntDown', [stk(K, 2, TOP)]),
  ];
  const whirl = (r = 0.3) => rule('play', [xtra(r)], { when: { type: '공격' }, limit: 1 });
  setCards(j, [
    // u1 터뜨리기 — 정조준(쌓은 승부 1개당 — 쥐고 있을수록)
    card(H, 1, '정조준', 1, '공격', [dmg(0.85), per(K), dmg(0.25)], [
      O('명중', [dmg(1.05), per(K), dmg(0.3)]),                                             // A
      O('헥토파스칼', [dmg(1.0), per(K), dmg(0.45), spendAll(K)]),                           // H 다 씀
      O('정면 돌파', [dmg(0.85), per(K), dmg(0.25), ifKill, draw(1)]),                       // 재설계 · 처치
      O('바람 정령 날리기', [dmg(2.0), per(K), dmg(0.5), tough(1)], { cost: 2 }),           // C 강인도
      O('셰이디 저격', [dmg(0.85), per(K), dmg(0.25)], { tags: ['약점 공격'] }),             // G
    ], [B('팽팽한 시위', 'power'), B('질서의 눈', 'weakSpot'), B('꼿꼿한 자세', 'ap')]),
    // u2 굴리기 — 원작 저학년 래피드 샷: 다섯 발
    card(H, 2, '래피드 샷', 1, '공격', [hitsT(5, 0.25, E1)], [
      O('보이지도 않는 화살', [hitsT(5, 0.31, E1)]),                                        // A
      O('바람살', [hitsT(5, 0.25, E1), ifStack(K, 3), hitsT(2, 0.3, E1)]),                   // E
      O('회오리 화살', [dmg(0.85, EA)]),                                                    // G 광역
      O('연사 연습', [hitsT(5, 0.22, E1), stk(K, 1, E1), discard(1)]),                       // 재설계 · 대가
      O('질서의 화살', [hitsT(5, 0.25, E1), draw(1, { type: '공격' })]),                      // F 서치
    ], [B('북서부 고기압', 'draw'), B('난기류', [stk(K, 1, E1)]), B('가벼운 시위', 'cost')]),
    // u3 열기 — 바람을 읽고 맞설 적을 고른다(시동 카드)
    card(H, 3, '바람 읽기', 0, '스킬', [stk(K, 1, E1), draw(1)], [
      O('바람의 흐름', [stk(K, 2, E1), draw(1)]),                                           // A
      O('돌멩이 하나까지', [stk(K, 1, E1), draw(2, { type: '공격' })]),                       // F 서치
      O('기후 관리', [draw(1), power(rule('turnStart', [stk(K, 1, TOP)]))], { power: true }), // D
      O('정령산 정상', [stk(K, 2, E1), sh(0.6)], { tags: KAI }),                             // 재설계
      O('밀린 만화책', [stk(K, 1, E1), draw(1), inspire, stk(K, 2, E1)]),                    // E 영감
    ], [B('손풍기', 'draw'), B('민들레 씨앗', [stk(K, 1, E1)]), B('맏언니의 눈', { tags: ['보존'] })]),
    // u4 완성형 — 원작 어사이드 「회오리 바람이 분다」(상시 — 공격마다 회오리)
    card(H, 4, '맏언니의 각오', 1, '강화', [stk(K, 1, E1), power(whirl())], [
      O('회오리 바람', [stk(K, 1, E1), power(whirl(0.4))]),                                 // A
      O('정령의 수장', [stk(K, 1, E1), draw(1, { type: '공격' }), power(whirl(), rule('huntDown', [draw(1)]))]),
      O('공사 구분', [stk(K, 1, E1), power(whirl())], { tags: KAI }),                        // G
      O('웃는 연습', [{ k: 'critMod', v: 0.1, run: true }, stk(K, 2, E1), hitsT(3, 0.3, E1)]),                                   // 재설계 — 한 번에
      O('전투력 측정기', [payPct(0.03), stk(K, 1, E1), power(whirl(), rule('turnStart', [stk(K, 1, TOP)]))]), // H HP · 엔진 한 단계
    ], [B('우정의 증표', 'atkUp'), B('추억 속의 정령들', 'ap'), B('바람막이', 'defUp')]),
    // u5 유틸(0코 비용) — 하늬바람: 승부 + 손의 공격 카드 비용 -1
    card(H, 5, '하늬바람', 0, '스킬', [stk(K, 1, E1), csOwn({ type: '공격' })], [
      O('신탁 1', [stk(K, 2, E1), csOwn({ type: '공격' })]),
      O('신탁 2', [stk(K, 1, E1), srch({ type: '공격' })]),
      O('신탁 3', [dmg(0.4), stk(K, 1, E1), power(rule('stackReach', [csOwn({ type: '공격' })], { when: { id: K, n: 3 } }))], { power: true }),
      O('신탁 4', [stk(K, 1, E1), sh(0.8)]),
      O('신탁 5', [payPct(0.03), stk(K, 2, E1), csOwn({ type: '공격' })]),
    ], [B('축복 1', 'guard'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  starter(j, '실라_u3');
  for (const e of j.equips || []) e.affinityEffect = [P('맞바람 한 발', 'fightStart', [stk(K, 1, TOP)])];
}

// ════════════════════════════════════════════════════════════════════
// 11. 아르코 — 딜러 · 활발. 카드마다 「스텝」 — 셋이면 브레이크(음파 두 번 · AP), 그 전에 「토네이도 스핀」으로 돌릴 수도
// ════════════════════════════════════════════════════════════════════
function arco(j) {
  const H = '아르코', K = '스텝';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '박자에 맞춰 밟는 스텝', carrier: 'self', cap: 3,
    rules: [{ name: '브레이크', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), hitsT(2, 0.5, ER), ap(1)] }],
  };
  delete h.keywords;
  h.passives = [P('리듬에 몸을 맡겨', 'play', [stk(K, 1)], { limit: 2 })];
  const brk = (r = 0.3) => rule('stackReach', [hitsT(2, r, ER)], { when: { id: K, n: 3 } });
  setCards(j, [
    // u1 굴리기 — 원작 저학년: 눈부신 댄스로 네 번 광역
    card(H, 1, '정정당당 댄스배틀', 1, '공격', [hitsT(4, 0.2, EA)], [
      O('눈부신 댄스', [hitsT(4, 0.25, EA)]),                                               // A
      O('실드 깨기', [{ k: 'strip' }, hitsT(4, 0.2, EA), stk(K, 1)]),                        // 재설계
      O('댄스 크루', [hitsT(4, 0.2, EA), draw(1, { who: 'other' })]),                        // F 서치
      O('솔로 무대', [hitsT(4, 0.32, E1)]),                                                 // G 단일
      O('청적전쟁', [hitsT(4, 0.2, EA), ifStack(K, 2), hitsT(2, 0.2, EA)]),                  // E
    ], [B('스트릿 감성', 'power'), B('떠다니는 스피커', 'ap'), B('인이어', 'draw')]),
    // u2 터뜨리기 — 스텝을 다 써서 회전(1개당 · 다 씀 — 브레이크와 맞바꿈)
    card(H, 2, '토네이도 스핀', 1, '공격', [dmg(0.55, EA), per(K), dmg(0.17, EA), spendAll(K)], [
      O('회전 가속', [dmg(0.7, EA), per(K), dmg(0.2, EA), spendAll(K)]),                     // A
      O('한 바퀴 더', [dmg(0.55, EA), per(K), dmg(0.17, EA)]),                              // 쥐는 쪽
      O('댄스 어쌔신', [dmg(1.0), per(K), dmg(0.3), spendAll(K)]),                          // G 단일
      O('퍼플 스핀', [discard(1), dmg(0.8, EA), per(K), dmg(0.22, EA)]),                     // 재설계 · 대가
      O('흥겨운 파동', [per(K), dmg(0.3, EA), spendAll(K), ifKill, stk(K, 2)]),              // 재설계
    ], [B('토네이도', 'power'), B('스핀 킥', 'weakSpot'), B('가벼운 발', 'cost')]),
    // u3 열기 — 프리즈: 멈춘 박자에서 손을 굴린다(시동 카드)
    card(H, 3, '프리즈', 0, '스킬', [stk(K, 1), draw(1)], [
      O('멈춘 박자', [stk(K, 2), draw(1)]),                                                 // A
      O('페스타 신곡', [stk(K, 1), draw(2, { basic: true })]),                               // F 서치 — 기본 카드가 스텝 연료
      O('무한 스피너', [draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D
      O('100번째 사도', [stk(K, 1), hitsT(2, 0.25, ER)], { tags: KAI }),                     // 재설계
      O('잠 없이 연습', [payPct(0.03), stk(K, 2), draw(1)]),                                 // H HP
    ], [B('과일 주스', 'draw'), B('리듬 타기', [stk(K, 1)]), B('가짜 주스 거절', { tags: ['보존'] })]),
    // u4 완성형 — 원작 어사이드 「적포도가 근본」(상시 — 협공 · 브레이크마다 음파 더)
    card(H, 4, '적포도 스트리트', 1, '강화', [st('협공', 1), power(brk())], [
      O('적포도 원리주의', [st('협공', 1), power(brk(0.4))]),                               // A
      O('우상의 무대', [st('협공', 1), stk(K, 1), power(brk(), rule('turnStart', [stk(K, 1)]))]),
      O('샤인그레이프', [st('협공', 1), power(brk())], { tags: KAI }),                      // G
      O('병상에서도 춤', [st('협공', 1), stk(K, 2), { k: 'dealtMod', v: 0.1, run: true }]),                     // 재설계 — 한 번에
      O('크루 결성', [st('협공', 1), draw(1, { who: 'other' }), power(brk())]),             // F 서치
    ], [B('심각한 댄스 중독', 'atkUp'), B('박자 감각', 'cost'), B('무대 의상', [stk(K, 1)])]),
    // u5 유틸(1코 동료 서치) — 엇박 스텝: 스텝 + 동료 카드 둘
    card(H, 5, '엇박 스텝', 1, '스킬', [stk(K, 1), draw(2, { who: 'other' })], [
      O('신탁 1', [stk(K, 2), draw(2, { who: 'other' })]),
      O('신탁 2', [stk(K, 1), draw(1, { who: 'other' })], { cost: 0 }),
      O('신탁 3', [hitsT(2, 0.3, ER), stk(K, 1), power(rule('play', [stk(K, 1)], { when: { who: 'other' }, limit: 1 }))], { power: true }),
      O('신탁 4', [stk(K, 1), draw(2, { who: 'other' }), ifStack(K, 2), hitsT(2, 0.3, ER)]),
      O('신탁 5', [payPct(0.03), stk(K, 2), draw(3, { who: 'other' })]),
    ], [B('축복 1', 'guard'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '아르코_u3');
}

// ════════════════════════════════════════════════════════════════════
// 12. 아일라 — 서포터(실드) · 순수. 「빠직」 네 단계 — 초폭망이면 대폭발, 그 전에 「화를 삭이는 명상」으로 실드로 바꿀 수도
// ════════════════════════════════════════════════════════════════════
function ayla(j) {
  const H = '아일라', K = '빠직';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '세며 참는 화산섬의 분노', carrier: 'self', cap: 4, stages: ['쫌폭망', '소폭망', '대폭망', '초폭망'],
    rules: [{ name: '대폭발', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(1.0, EA), sh(2.4)] }],
  };
  delete h.keywords;
  h.passives = [
    P('부글부글', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 3 }),
    P('화산 정령', 'shieldBreak', [dmg(0.6), st('약화', 1, E1)], { limit: 1 }),
  ];
  setCards(j, [
    // u1 열기 — 초폭망 직전: 작은 화산 보호막 + 분노 한 단계(시동 카드)
    card(H, 1, '초폭망 직전', 0, '스킬', [stk(K, 1), sh(0.9)], [
      O('걍폭망', [stk(K, 2), sh(0.9)]),                                                    // A
      O('나 좀 냅두라바!!', [stk(K, 1), sh(0.9)], { tags: KAI }),                           // G
      O('땅콩 아니라바', [stk(K, 1), heal(0.7), srch()]),                                    // 재설계 · 서치
      O('일광욕', [sh(0.5), power(rule('turnStart', [stk(K, 1)]))], { power: true }),       // D
      O('벨라의 장난', [stk(K, 1), sh(0.9), ifWounded, stk(K, 2)]),                         // E 조건
    ], [B('따스한 햇살', 'guard'), B('해파리 친구', 'draw'), B('끓는 속', [stk(K, 1)])]),
    // u2 굴리기 — 원작 저학년: 화산 보호막(깨지면 화산 정령이 나온다 — 패시브)
    card(H, 2, '휴가 중 잠적', 1, '스킬', [sh(1.8), stk(K, 1)], [
      O('화산 장벽', [sh(2.2), stk(K, 1)]),                                                 // A
      O('섬 전체 장벽', [sh(3.0), stk(K, 1), st('피해 감소', 1)], { cost: 2 }),             // C 피해 감소
      O('휴양지 온천', [discard(1), sh(2.0), stk(K, 2)]),
      O('볼케니카 설계', [sh(1.4), stk(K, 1), per(K), sh(0.2)]),                           // F
      O('해적 퇴치', [sh(1.3), stk(K, 1), ifStack(K, 3), dmg(0.6, EA)]),                    // 재설계 · 조건
    ], [B('굳은 용암', 'guard'), B('후드티', 'defUp'), B('마그마 찌개', 'ap')]),
    // u3 터뜨리기 — 분노를 삭여 실드로(1개당 · 다 씀 — 대폭발과 맞바꿈)
    card(H, 3, '화를 삭이는 명상', 1, '스킬', [heal(0.9), per(K), sh(0.35), spendAll(K)], [
      O('대지의 명상', [heal(1.05), per(K), sh(0.42), spendAll(K)]),                         // A
      O('가비아의 가르침', [per(K), sh(0.4), spendAll(K), draw(1)]),                          // 재설계
      O('참지 않기', [per(K), dmg(0.3, EA), spendAll(K)]),                                   // 재설계 — 방어 ↔ 공격
      O('명상 수행 중', [heal(0.7), per(K), sh(0.25)], { tags: ['보존'] }),                  // 쥐는 쪽
      O('깊은 호흡', [heal(0.75), per(K), sh(0.3), spendAll(K)], { cost: 0 }),               // B
    ], [B('파도 소리', 'heal'), B('해먹', 'draw'), B('느긋한 오후', 'cost')]),
    // u4 완성형 — 원작 어사이드 「화산섬의 주인」(상시 — 결의 담당 · 대폭발마다 큰 실드). 3단계 측정에서 1코 마무리로 바꾸자 완주율이 21.8 → 17%대로 빠져 강화 카드로 남겼다
    card(H, 4, '화산섬의 주인', 1, '강화', [st('결의', 2), power(rule('stackReach', [sh(1.0)], { when: { id: K, n: 4 } }))], [
      O('활화산', [st('결의', 2), power(rule('stackReach', [sh(2.0)], { when: { id: K, n: 4 } }))]),                                   // A
      O('분노 5단계', [st('결의', 2), stk(K, 1), power(rule('stackReach', [sh(1.0)], { when: { id: K, n: 4 } }), rule('turnStart', [stk(K, 1)]))]), // 엔진 한 단계
      O('섬의 수호자', [st('결의', 2), sh(0.6), power(rule('stackReach', [sh(1.0)], { when: { id: K, n: 4 } }))], { tags: KAI }),                   // G
      O('평화 박살 해파리', [st('결의', 2), per(K), dmg(0.55, EA), spendAll(K)]),                                                          // 재설계 · 대가 — 한 번에
      O('그윈과 델리아', [st('결의', 2), draw(1, { who: 'other' }), power(rule('stackReach', [sh(1.0)], { when: { id: K, n: 4 } }))]),       // F 서치
    ], [B('꿀렁 해파리', 'defUp'), B('짱킹갓', [stk(K, 1)]), B('섬 순찰', 'cost')]),
    // u5 굴리기 — 영구 추방령: 화의 원인을 섬 밖으로(피해 · 빠직 · 실드)
    card(H, 5, '영구 추방령', 1, '공격', [dmg(0.8), stk(K, 1), sh(0.6)], [
      O('신탁 1', [dmg(1.0), stk(K, 1), sh(0.7)]),
      O('신탁 2', [dmg(0.5, EA), stk(K, 1), sh(0.6)]),
      O('신탁 3', [dmg(0.7), stk(K, 1), power(rule('shieldBreak', [stk(K, 1), dmg(0.4)], { limit: 1 }))], { power: true }),
      O('신탁 4', [dmg(0.8), stk(K, 1), ifStack(K, 3), dmg(0.6, EA)]),
      O('신탁 5', [discard(1), dmg(1.2), stk(K, 2)]),
    ], [B('축복 1', 'power'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '아일라_u1');
}

// ════════════════════════════════════════════════════════════════════
// 13. 오로라 — 탱커 · 우울(원작 방식 고학년 「오로라 장막」 그대로). 맞을수록 「빛울림」 — 다섯이면 오로라 빛
// ════════════════════════════════════════════════════════════════════
function aurora(j) {
  const H = '오로라', K = '빛울림';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '맞을수록 모이는 빛의 떨림', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.1 }],
    rules: [{ name: '오로라 빛', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), ddef(0.4, EA), heal(1.0)] }],
  };
  h.passives = [
    P('다정한 빛마중', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 3 }),
    P('긍정 주머니', 'lowHp', [heal(1.5), st('피해 감소', 2)], { when: { pct: 0.3 } }),
  ];
  const full = (fx = [draw(1)]) => rule('stackReach', fx, { when: { id: K, n: 5 } });
  setCards(j, [
    // u1 굴리기 — 원작 저학년 은은한 빛무늬(피해 감소 담당)
    card(H, 1, '은은한 빛무늬', 1, '스킬', [sh(1.3), st('피해 감소', 1), stk(K, 1)], [
      O('긍정의 기운', [sh(1.5), st('피해 감소', 1), stk(K, 1)]),                           // A
      O('모두에게 빛무늬', [sh(2.6), st('피해 감소', 3), heal(1.0)], { cost: 2 }),          // C 회복
      O('황색 아우라', [sh(1.3), st('피해 감소', 1), heal(1.0)]),                           // 재설계
      O('빛무늬 유지', [sh(1.1), st('피해 감소', 1), stk(K, 1)], { tags: ['보존'] }),       // G
      O('선명한 빛울림', [sh(1.3), st('피해 감소', 1), per(K), sh(0.3)]),                   // F
    ], [B('따뜻한 빛', 'guard'), B('밤하늘', [stk(K, 1)]), B('흰 리본', 'ap')]),
    // u2 열기 — 아우라 읽기: 동료 카드를 끌어온다(시동 카드)
    card(H, 2, '아우라 읽기', 0, '스킬', [stk(K, 1), draw(1, { who: 'other' })], [
      O('녹색 아우라', [stk(K, 2), draw(1, { who: 'other' })]),                              // A
      O('청색 아우라', [stk(K, 1), draw(1, { who: 'other' }), heal(0.5)]),
      O('천궁도', [stk(K, 2), pullD({ who: 'other' })]),                                    // 재설계 · 서치
      O('별자리 지도', [draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { power: true }), // D
      O('직접 대화하기', [stk(K, 1), draw(1, { who: 'other' }), inspire, stk(K, 2)]),        // E 영감
    ], [B('예언가 놀이', 'draw'), B('고양이 파자마', { tags: ['보존'] }), B('반짝 눈빛', [stk(K, 1)])]),
    // u3 터뜨리기 — 빛을 흩뿌린다(1개당 · 다 씀 — 오로라 빛과 맞바꿈)
    card(H, 3, '빛 조각 흩뿌리기', 1, '공격', [ddef(0.3, EA), per(K), ddef(0.08, EA), spendAll(K)], [
      O('플라즈마 조각', [ddef(0.38, EA), per(K), ddef(0.1, EA), spendAll(K)]),              // A
      O('오~로로로로라!', [per(K), ddef(0.25, EA), spendAll(K), heal(1.0)], { cost: 2 }),     // C 회복
      O('한 점 집중', [ddef(0.5), per(K), ddef(0.13), spendAll(K)]),                         // G 단일
      O('빛 아끼기', [discard(1), ddef(0.35, EA), per(K), ddef(0.1, EA)]),                   // 재설계 · 대가
      O('목장 불태우기', [per(K), ddef(0.15, EA), spendAll(K), ifKill, stk(K, 2)]),          // 재설계
    ], [B('눈부신 극광', 'power'), B('빛의 잔상', 'frost'), B('가벼운 빛', 'cost')]),
    // u4 마무리 — 함께 있는 밤(불굴 담당 · 쥔 빛울림 1개당 회복)
    card(H, 4, '함께 있는 밤', 1, '스킬', [st('불굴', 1), per(K), heal(0.2)], [
      O('고요한 밤', [st('불굴', 1), per(K), heal(0.26), draw(1)]),                                   // A
      O('벨벳과 함께', [st('불굴', 1), power(full(), rule('turnStart', [stk(K, 1)]))], { power: true }), // D — 옛 상시 엔진
      O('외톨이 아니야', [st('불굴', 1), per(K), heal(0.2)], { tags: KAI }),                 // G
      O('깜짝 파티', [st('불굴', 1), per(K), ddef(0.25, EA), spendAll(K)]),                                 // 재설계 · 대가
      O('마녀 왕국 생활', [st('불굴', 1), per(K), heal(0.2), draw(1, { who: 'other' })]),     // F 서치
    ], [B('밤하늘 바라보기', 'defUp'), B('따뜻한 담요', 'cost'), B('작은 별', [sh(0.4)])]),
    // u5 굴리기 — 꺼지지 않는 불꽃: 플라즈마로 치고 빛울림 · 회복
    card(H, 5, '꺼지지 않는 불꽃', 1, '공격', [ddef(0.6), stk(K, 1), heal(0.4)], [
      O('신탁 1', [ddef(0.75), stk(K, 1), heal(0.5)]),
      O('신탁 2', [ddef(0.4, EA), stk(K, 1), heal(0.4)]),
      O('신탁 3', [ddef(0.5), heal(0.3), power(rule('turnEnd', [stk(K, 1)]))], { power: true }),
      O('신탁 4', [ddef(0.6), stk(K, 1), ifStack(K, 4), st('취약', 1, E1)]),
      O('신탁 5', [discard(1), ddef(0.9), stk(K, 2)]),
    ], [B('축복 1', 'heal'), B('축복 2', 'atkUp'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '오로라_u2');
}

// ════════════════════════════════════════════════════════════════════
// 14. 우이 — 서포터(회복) · 활발 · 엘다인. 개굴비(생성 카드)를 내리고, 먹으면(소멸) 「행복 지수」 — 다섯이면 긍정왕
// ════════════════════════════════════════════════════════════════════
function ui(j) {
  const H = '우이', K = '행복 지수', RAIN = '우이_t1';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '덮어 둔 만큼 차오르는 웃음', carrier: 'self', cap: 5,
    rules: [{ name: '긍정왕', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), heal(1.0), st('사기', 1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('에루의 우산', 'exhaust', [stk(K, 1)], { limit: 3 }),
    P('싫은 건 안 보면 돼', 'lowHp', [cleanse(3), heal(1.2), make(RAIN, 2)], { when: { pct: 0.3 } }),
  ];
  Object.assign(tokenOf(j, RAIN), { cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.5), dmg(0.3, EA)] });
  setCards(j, [
    // u1 열기 — 원작 저학년 개굴비(시동 카드)
    card(H, 1, '개굴비 내리기', 1, '스킬', [make(RAIN, 2), heal(0.5)], [
      O('장대비', [make(RAIN, 3), heal(0.5)]),                                              // A
      O('보슬보슬', [make(RAIN, 2), heal(0.45)], { cost: 0 }),                              // B
      O('에루가 부르는 비', [make(RAIN, 2), heal(0.5), power(rule('turnStart', [heal(0.4)]))], { power: true }), // D
      O('비 온 뒤 무지개', [make(RAIN, 2), stk(K, 1), inspire, make(RAIN, 1)]),             // 재설계 · 영감
      O('친구 상담', [make(RAIN, 2), heal(0.5), draw(1, { who: 'other' })]),                // F 서치
    ], [B('네잎클로버', 'draw'), B('비옷', [sh(0.5)]), B('개구리 모자', [make(RAIN, 1)])]),
    // u2 굴리기 — 소나기: 광역 + 개굴비
    card(H, 2, '소나기', 1, '공격', [dmg(0.6, EA), make(RAIN, 1)], [
      O('한여름 소나기', [dmg(0.78, EA), make(RAIN, 1)]),                                   // A
      O('에루의 혓바닥', [dmg(1.4), cleanse(1), heal(0.7)]),                                 // 재설계
      O('천둥 개굴', [dmg(0.6, EA), make(RAIN, 1), ifStack(K, 3), dmg(0.4, EA)]),            // E
      O('행복한 빗소리', [dmg(0.5, EA), make(RAIN, 3), discard(1)]),                        // H 버리기
      O('폭우', [dmg(1.0, EA), make(RAIN, 2), stk(K, 2)], { cost: 2 }),                     // C 지수 덤
    ], [B('굵은 빗방울', 'power'), B('젖은 땅', 'frost'), B('가랑비', 'cost')]),
    // u3 터뜨리기 — 손의 개굴비 전부 합창(1장당 · 전부 소멸 → 행복 지수)
    card(H, 3, '개굴개굴 합창', 1, '공격', [dmg(0.5, EA), perTag(RAIN), dmg(0.25, EA), exileAll(RAIN)], [
      O('떼창', [dmg(0.6, EA), perTag(RAIN), dmg(0.3, EA), exileAll(RAIN)]),                // A
      O('한 마리씩', [make(RAIN, 2), stk(K, 1), sh(0.6)]),                                  // 재설계 — 쥐고 실드
      O('대합창', [perTag(RAIN), dmg(0.6, EA), exileAll(RAIN), stk(K, 3)], { cost: 2 }),    // C 지수 덤
      O('개구리 독창', [dmg(0.85), perTag(RAIN), dmg(0.4), exileAll(RAIN)]),                // G 단일
      O('행복 전파', [dmg(0.6, EA), perTag(RAIN), heal(0.55), exileAll(RAIN)]),             // 공격 ↔ 회복
    ], [B('목청 큰 에루', 'power'), B('개굴 박자', 'draw'), B('지퍼 입', 'ap')]),
    // u4 마무리 — 에루와 단짝(개굴비 하나 더 내리고, 손의 개굴비 1장당 회복 — 쥐는 쪽)
    card(H, 4, '에루와 단짝', 1, '스킬', [make(RAIN, 1), heal(0.5), perTag(RAIN), heal(0.12)], [
      O('에루는 개굴개굴', [make(RAIN, 2), heal(0.5), perTag(RAIN), heal(0.12)]),                       // A
      O('긍정왕 우이', [heal(0.4), power(rule('turnStart', [make(RAIN, 1)]), rule('stackReach', [st('사기', 1)], { when: { id: K, n: 5 }, limit: 1 }))], { power: true }), // D — 옛 상시 엔진 · 엘다인 한 단계
      O('장화신은 우이', [make(RAIN, 1), heal(0.5), perTag(RAIN), heal(0.12)], { tags: KAI }),          // G
      O('언해피 우이', [payPct(0.03), make(RAIN, 3), stk(K, 1)]),                           // 재설계 · 대가
      O('행복하기를 빌어', [make(RAIN, 1), perTag(RAIN), heal(0.15), srch()]),               // F 서치
    ], [B('에루 쓰다듬기', 'heal'), B('작은 개구리', 'cost'), B('우산', [stk(K, 1)])]),
    // u5 유틸(1코 동료 서치) — 선생님한테 고자질: 개굴비 · 동료 카드
    card(H, 5, '선생님한테 고자질', 1, '스킬', [make(RAIN, 1), draw(1, { who: 'other' })], [
      O('신탁 1', [make(RAIN, 2), draw(1, { who: 'other' })]),
      O('신탁 2', [make(RAIN, 1), draw(1, { who: 'other' })], { cost: 0 }),
      O('신탁 3', [make(RAIN, 1), draw(1, { who: 'other' }), power(rule('stackReach', [make(RAIN, 2)], { when: { id: K, n: 5 } }))], { power: true }),
      O('신탁 4', [make(RAIN, 1), draw(1, { who: 'other' }), inspire, make(RAIN, 2)]),
      O('신탁 5', [payPct(0.03), make(RAIN, 2), draw(2, { who: 'other' })]),
    ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '우이_u1');
}

// ════════════════════════════════════════════════════════════════════
// 15. 우이(기억) — 서포터(드로우) · 냉정 · 엘다인(원작 방식 고학년 「기억의 연못」 · 「연못 깊이」 그대로). 세잎클로버(생성 카드) · 소멸마다 「되찾은 기억」
//     3단계: 기본 카드 연료(흐려진 기억 — 기본 카드를 잊어 기억으로) · ④ 1코 마무리
// ════════════════════════════════════════════════════════════════════
function uiMemory(j) {
  const H = '우이_기억', K = '되찾은 기억', CL = '우이_기억_clover';
  const h = j.heroes[0];
  h.passives = [
    P('도망치지 않는 기억', 'exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 3 }),
    P('행복왕 우이', 'lowHp', [st('회피', 2), heal(1.0)], { when: { pct: 0.3 } }),
  ];
  addToken(j, { id: CL, name: '세잎클로버', hero: H, token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [sh(0.5), cut(0.3)], blurb: '잎 한 장이 큰 한 대를 대신 받아 줍니다.' });
  const SM = ['소멸'];
  setCards(j, [
    // u1 굴리기 — 잊으려던 기억: 뽑고 사라진다(소멸 → 기억 · 연못)
    card(H, 1, '잊으려던 기억', 0, '스킬', [draw(2)], [
      O('떠오르는 얼굴', [draw(3)], { tags: SM }),                                          // A
      O('자매들의 기억', [draw(3, { who: 'other' })], { tags: SM }),                        // F 서치
      O('지워진 기억', [draw(2), stk(K, 1)], { tags: SM }),
      O('마주하는 용기', [draw(2), heal(0.5)], { tags: ['보존'] }),                         // 소멸 떼기
      O('흐려진 기억', [exB, stk(K, 1), draw(2)], { tags: SM }),                                     // 재설계 — 기본 카드를 잊는다
    ], [B('맑은 연못', 'ap'), B('에루의 노래', [cleanse(1)]), B('물빛 기억', [stk(K, 1)])], { tags: SM }),
    // u2 열기 — 원작 저학년 행복 전파자: 회복 + 세잎클로버(시동 카드)
    card(H, 2, '행복 전파자', 1, '스킬', [heal(0.6), make(CL, 2)], [
      O('세 장의 잎', [heal(0.6), make(CL, 3)]),                                            // A
      O('행복을 기도해', [heal(0.6), make(CL, 2), stk(K, 2)]),                                    // 재설계 · 서치
      O('기도 마치기', [heal(0.6), make(CL, 2)], { cost: 0 }),                             // G
      O('에루 리턴즈', [heal(0.4), make(CL, 1), power(rule('turnStart', [make(CL, 1)]))], { power: true }), // D
      O('행운 나누기', [sh(1.0), make(CL, 2), draw(1, { who: 'other' })]),                 // F 서치
    ], [B('네잎 하나 더', 'draw'), B('따뜻한 기억', 'heal'), B('연못가', [make(CL, 1)])]),
    // u3 터뜨리기 — 떠나보내기(기억 1개당 · 다 씀 — 고학년에 쓸 기억과 맞바꿈)
    card(H, 3, '떠나보내기', 1, '공격', [dmg(0.8), per(K), dmg(0.25), spendAll(K)], [
      O('흘려보내기', [dmg(1.0), per(K), dmg(0.3), spendAll(K)]),                            // A
      O('기억의 물결', [dmg(0.55, EA), per(K), dmg(0.17, EA), spendAll(K)]),                 // G 광역
      O('간직하기', [dmg(0.8), per(K), dmg(0.25)]),                                         // 쥐는 쪽
      O('다시 떠오른 이름', [per(K), dmg(0.4), spendAll(K), ifKill, stk(K, 2)]),             // 재설계
      O('일렁이는 파동', [per(K), dmg(0.6), spendAll(K), make(CL, 2)], { cost: 2 }),         // C 클로버 덤
    ], [B('파문', 'power'), B('젖은 발', 'frost'), B('잔물결', 'cost')]),
    // u4 마무리 — 다시 마주한 기억(쥔 기억 1개당 실드 + 드로우)
    card(H, 4, '다시 마주한 기억', 1, '스킬', [per(K), sh(0.25), draw(1)], [
      O('다시 마주한 얼굴', [per(K), sh(0.3), draw(1)]),                                     // A
      O('이젠 피하지 않아', [heal(0.5), make(CL, 1), power(rule('exhaust', [sh(0.4)], { when: { who: 'any' }, limit: 2 }))], { power: true }), // D — 옛 상시 엔진 · 엘다인 한 단계
      O('세잎클로버 화분', [per(K), sh(0.25), draw(1)], { cost: 0 }),                        // B
      O('슬픔도 기억', [burn(1), stk(K, 2), draw(2)]),                                      // 재설계 · 대가
      O('연못의 에루', [per(K), sh(0.25), pullD({ who: 'self' })]),                          // 재설계 · 서치
    ], [B('기억의 온기', 'guard'), B('맑은 마음', 'cost'), B('에루 타기', [stk(K, 1)])]),
    // u5 굴리기 — 끝없는 클로버 밭: 덩굴로 덮어 광역 · 기억(클로버는 신탁 갈래에서)
    card(H, 5, '끝없는 클로버 밭', 1, '공격', [dmg(0.4, EA), stk(K, 1)], [
      O('신탁 1', [dmg(0.55, EA), stk(K, 1)]),
      O('신탁 2', [dmg(0.8), stk(K, 1)]),
      O('신탁 3', [dmg(0.4, EA), make(CL, 1), power(rule('exhaust', [dmg(0.3, ER)], { when: { who: 'any' }, limit: 2 }))], { power: true }),
      O('신탁 4', [dmg(0.4, EA), make(CL, 1), perTag(CL), dmg(0.15, EA)]),
      O('신탁 5', [burn(1), dmg(0.6, EA), stk(K, 3)]),
    ], [B('축복 1', 'atkUp'), B('축복 2', 'draw'), B('축복 3', [make(CL, 1)])]),
  ]);
  starter(j, '우이_기억_u2');
}

// ════════════════════════════════════════════════════════════════════
// 16. 이프리트 — 딜러 · 광기. 태울 때마다(소멸) 「장작」 — 다섯이면 캠프파이어 불꽃 · 공격마다 화상(고통 담당)
//     3단계: 생성(불씨 — 내면 소멸해 장작) · 기본 카드 연료(쓰레기 정화) · ④(모닥불) 1코 마무리
// ════════════════════════════════════════════════════════════════════
function ifrit(j) {
  const H = '이프리트', K = '장작', EM = '이프리트_ember';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '소멸로 태워 넣은 땔감', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [{ name: '캠프파이어 불꽃', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), dmg(1.0, EA), st('고통', 3, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    P('장작 넣기', 'exhaust', [stk(K, 1)], { when: { who: 'any' }, limit: 3 }),
    P('불의 검', 'play', [st('고통', 2, E1)], { when: { type: '공격' }, limit: 2 }),
  ];
  addToken(j, { id: EM, name: '불씨', hero: H, token: true, cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(0.4), st('고통', 1, E1)], blurb: '이프리트가 흘리고 다니는 작은 불똥입니다. 다 타면 장작이 됩니다.' });
  setCards(j, [
    // u1 열기 — 손패 하나를 태워 넣는다(시동 카드)
    card(H, 1, '땔감 던지기', 0, '스킬', [burn(1), stk(K, 1), draw(1)], [
      O('장작 한 아름', [burn(1), stk(K, 2), draw(1)]),                                     // A
      O('쓰레기 정화', [exB, stk(K, 2), draw(1)]),                                          // 재설계 — 기본 카드를 태운다
      O('불씨만 살려 줘', [stk(K, 1), draw(1)], { tags: ['보존'] }),                        // G
      O('라떼 정령', [draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),    // D
      O('불씨 나누기', [burn(1), make(EM, 2)]),                                             // 재설계 — 불씨 생성
    ], [B('마른 장작', 'draw'), B('시꺼먼 숯', [stk(K, 1)]), B('불쏘시개 마이크', 'ap')]),
    // u2 터뜨리기 — 장작을 몽땅 태운 대검(1개당 · 다 씀)
    card(H, 2, '새까만 대검', 1, '공격', [dmg(0.85), per(K), dmg(0.22), spendAll(K)], [
      O('다 태울 기세', [dmg(1.05), per(K), dmg(0.27), spendAll(K)]),                        // A
      O('불의 의지', [per(K), dmg(0.7), spendAll(K), st('고통', 3, E1)], { cost: 2 }),        // C 고통
      O('잿더미', [dmg(0.6, EA), per(K), dmg(0.15, EA), spendAll(K)]),                       // G 광역
      O('하얗게 탄 재', [dmg(0.85), per(K), dmg(0.22)]),                                    // 쥐는 쪽
      O('웰던', [per(K), dmg(0.4), spendAll(K), ifKill, stk(K, 3)]),                         // 재설계
    ], [B('시뻘건 날', 'power'), B('그을린 칼등', 'weakSpot'), B('가벼운 검', 'cost')]),
    // u3 마무리 — 모닥불 피우기(쥔 장작 1개당 광역 — 「새까만 대검」 과 맞바꿈)
    card(H, 3, '모닥불 피우기', 1, '공격', [dmg(0.5, EA), per(K), dmg(0.12, EA)], [
      O('캠프파이어', [dmg(0.6, EA), per(K), dmg(0.15, EA)]),                                // A
      O('밤 산책', [dmg(0.5, EA), make(EM, 1), power(rule('exhaust', [dmg(0.35, ER)], { limit: 2 }))], { power: true }), // D — 옛 상시 엔진
      O('불씨 보관', [make(EM, 2), per(K), dmg(0.12, EA)]),                                  // 생성
      O('히어로 수행', [dmg(0.5, EA), per(K), dmg(0.12, EA), srch()]),                       // F 서치
      O('불로 지지기', [per(K), dmg(0.25, EA), spendAll(K), st('고통', 2, EA)]),              // 재설계 · 대가
    ], [B('뜨거운 몸', 'atkUp'), B('장작 패기', 'cost'), B('불길', [make(EM, 1)])]),
    // u4 굴리기 — 원작 저학년 지글짝 보글짝: 화염 지대 + 화상
    card(H, 4, '지글짝 보글짝', 1, '공격', [dmg(0.9), st('고통', 2, E1)], [
      O('화염 지대', [dmg(1.1), st('고통', 2, E1)]),                                        // A
      O('보글보글', [dmg(0.6, EA), st('고통', 1, EA)]),                                     // G 광역
      O('탄 냄새', [dmg(0.9), st('고통', 2, E1), ifStack(K, 3), dmg(0.6)]),                 // E
      O('불씨 남기기', [burn(1), dmg(1.1), st('고통', 2, E1)]),                              // H 태우기
      O('겁쟁이 불꽃', [make(EM, 2), stk(K, 1)]),                                           // 재설계 — 불씨 생성
    ], [B('화상', 'power'), B('불티', 'ap'), B('지글지글', [stk(K, 1)])]),
    // u5 유틸(1코 생성) — 애완 돌 데우기: 불씨 · 장작
    card(H, 5, '애완 돌 데우기', 1, '스킬', [make(EM, 2), stk(K, 1)], [
      O('신탁 1', [make(EM, 3), stk(K, 1)]),
      O('신탁 2', [make(EM, 2), stk(K, 1), srch()]),
      O('신탁 3', [make(EM, 1), stk(K, 1), power(rule('turnStart', [make(EM, 1)]))], { power: true }),
      O('신탁 4', [make(EM, 2), stk(K, 1), inspire, draw(1)]),
      O('신탁 5', [burn(1), make(EM, 3), stk(K, 2)]),
    ], [B('축복 1', 'guard'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '이프리트_u1');
}

// ════════════════════════════════════════════════════════════════════
// 17. 잉클 — 딜러 · 냉정. 획마다 적에게 「먹빛」 — 「마무리 펜 터치」가 적마다 제 먹빛만큼 거두어 간다
//     3단계: 시동 = 개전 강화 「세상에 한 획」(옛 「먹물 정령의 소원」 엔진 — 매 턴 적 전체 먹빛) · ④ 1코 마무리(쥔 먹빛을 센다)
// ════════════════════════════════════════════════════════════════════
function inkle(j) {
  const H = '잉클', K = '먹빛';
  const h = j.heroes[0];
  renameUlt(h, '번짐', K);
  h.keyword = {
    name: K, desc: '획마다 스며드는 먹물 얼룩', carrier: 'enemy', cap: 3, per: [{ stat: 'taken', v: 0.12 }],
    rules: [{ name: '먹빛 현상', when: { on: 'stackReach', id: K, n: 3 }, fx: [tough(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('획 긋기', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }),
    P('들어주길 바라오', 'fightStart', [{ k: 'dealtMod', v: 0.2, turns: 2, target: 'self' }]),
  ];
  const ink = () => power(rule('turnStart', [stk(K, 1, EA)]));
  setCards(j, [
    // u1 열기 — 세상에 한 획(개전 강화 시동 카드 — 매 턴 적 전체 먹빛)
    card(H, 1, '세상에 한 획', 1, '강화', [stk(K, 1, EA), ink()], [
      O('일필휘지', [stk(K, 2, EA), ink()], { tags: KAI, power: true }),                    // A
      O('가장 가까운 곳에서', [stk(K, 1, EA), power(rule('turnStart', [stk(K, 1, EA)]), rule('kill', [draw(1)], { limit: 1 }))], { tags: KAI, power: true }),
      O('셰럼의 동인지', [stk(K, 1, EA), srch({ type: '공격' }), ink()], { tags: KAI, power: true }), // F 서치
      O('작가의 꿈', [stk(K, 2, EA), ink()], { power: true }),                             // H 개전 떼기
      O('아니, 아니, 아니라오', [{ k: 'dealtMod', v: 0.1, run: true }, stk(K, 2, E1), draw(1)], { tags: KAI }),                   // 재설계 — 한 번에
    ], [B('다도', 'draw'), B('족자 펼치기', 'cost'), B('잉잉', [stk(K, 1, EA)])], { tags: KAI }),
    // u2 터뜨리기 — 원작 강화 평타 마무리 펜 터치: 적마다 제 먹빛만큼 · 다 씀
    card(H, 2, '마무리 펜 터치', 1, '공격', [dmg(0.5, EA), perEach(K), dmg(0.3, EA), spendEA(K)], [
      O('화룡점정', [dmg(0.62, EA), perEach(K), dmg(0.38, EA), spendEA(K)]),                 // A
      O('한 폭 완성', [perEach(K), dmg(0.68, EA), spendEA(K), tough(1, EA)], { cost: 2 }),    // C 강인도
      O('낙관 찍기', [dmg(0.9), per(K), dmg(0.45), spendAll(K)]),                           // G 단일
      O('덧칠', [dmg(0.6, EA), perEach(K), dmg(0.42, EA)]),                                  // 쥐는 쪽
      O('기절할 만큼', [perEach(K), dmg(0.55, EA), spendEA(K), ifKill, draw(1)]),            // 재설계
    ], [B('굵은 붓', 'power'), B('펜촉', 'weakSpot'), B('가벼운 붓대', 'ap')]),
    // u3 굴리기 — 원작 저학년 먹물 세례: 광역 + 먹빛
    card(H, 3, '먹물 세례', 1, '공격', [dmg(0.7, EA), stk(K, 1, EA)], [
      O('먹물 폭포', [dmg(0.88, EA), stk(K, 1, EA)]),                                       // A
      O('먹물 한 방울', [dmg(1.15), stk(K, 2, E1)]),                                        // G 단일
      O('먹물 범벅', [discard(1), dmg(0.9, EA), stk(K, 1, EA)]),                             // H 버리기
      O('번지는 먹', [dmg(0.7, EA), stk(K, 1, EA), ifKill, draw(1)]),                        // 재설계 · 처치
      O('대작 수묵화', [dmg(1.2, EA), stk(K, 2, EA), tough(1, EA)], { cost: 2 }),            // C 강인도
    ], [B('진한 먹', 'power'), B('먹빛 얼룩', 'frost'), B('묽은 먹', 'cost')]),
    // u4 마무리 — 먹물 정령의 소원(적마다 쥔 먹빛만큼 — 「마무리 펜 터치」 와 맞바꿈)
    card(H, 4, '먹물 정령의 소원', 1, '공격', [dmg(0.5, EA), perEach(K), dmg(0.2, EA)], [
      O('나만의 새로운 이야기', [dmg(0.6, EA), perEach(K), dmg(0.25, EA)]),                   // A
      O('삐뚤빼뚤 수묵화', [dmg(0.6, EA), stk(K, 1, EA), power(rule('kill', [stk(K, 2, EA)], { limit: 1 }))], { power: true }), // D
      O('먹 한 방울의 소원', [dmg(0.45, EA), perEach(K), dmg(0.18, EA)], { cost: 0 }),       // B
      O('미니 셰럼', [dmg(0.5, EA), perEach(K), dmg(0.2, EA), srch()]),                      // F 서치
      O('먹물 정령의 꿈', [perEach(K), dmg(0.3, EA), spendEA(K), draw(1)]),                  // 재설계 · 대가
    ], [B('먹 가는 소리', 'atkUp'), B('명상', 'cost'), B('잉크병', [stk(K, 1, EA)])]),
    // u5 유틸(0코 서치) — 먹물 파스타: 먹빛 · 작은 실드 · 공격 카드 한 장
    card(H, 5, '먹물 파스타', 0, '스킬', [stk(K, 1, EA), sh(0.4), draw(1, { type: '공격' })], [
      O('신탁 1', [stk(K, 1, EA), sh(0.6), draw(1, { type: '공격' })]),
      O('신탁 2', [sh(0.4), draw(2, { type: '공격' })]),
      O('신탁 3', [sh(0.6), draw(1, { type: '공격' }), power(rule('turnEnd', [sh(0.45)]))], { power: true }),
      O('신탁 4', [stk(K, 1, EA), sh(0.4), ifStack(K, 3), draw(2)]),
      O('신탁 5', [discard(1), stk(K, 2, EA), draw(2, { type: '공격' })]),
    ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1, EA)])]),
  ]);
  starter(j, '잉클_u1');
}

// ════════════════════════════════════════════════════════════════════
// 18. 쥬비 — 딜러 · 활발. 공격마다 「벌」이 모인다 — 「벌떼 돌격」이 한 마리마다 한 대씩 쏘고 다 날려 보낸다
//     3단계: 시동 = 개전 강화 「대장 쥬비 호출」(옛 「밀랍 둥지」 엔진) · ④ 「밀랍 둥지」 1코 마무리(벌 1개당 실드 — 쥐는 쪽)
// ════════════════════════════════════════════════════════════════════
function jubee(j) {
  const H = '쥬비', K = '벌';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '따라다니며 쏘는 꿀벌 떼', carrier: 'self', cap: 8,
    rules: [
      { name: '벌집 출발', when: { on: 'fightStart' }, fx: [stk(K, 3)] },
      { name: '윙윙 집합', when: { on: 'turnStart' }, fx: [stk(K, 1)] },
      { name: '꿀 도둑 처단', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
    ],
  };
  delete h.keywords;
  h.passives = [
    P('친구왔쮸비', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: 2 }),
    P('벌떼 습격', 'play', [per(K), dmg(0.38)], { when: { type: '공격' } }),
  ];
  const hive = (n = 1) => power(rule('turnStart', [stk(K, n)]));
  setCards(j, [
    // u1 굴리기 — 원작 저학년: 친구 꿀벌을 부르며 쏜다
    card(H, 1, '친구 꿀벌', 1, '공격', [dmg(1.0), stk(K, 2)], [
      O('친구 꿀벌 떼', [dmg(1.25), stk(K, 2)]),                                            // A
      O('가장 약한 적부터', [dmg(0.8), stk(K, 2), ifKill, stk(K, 2)]),                      // E
      O('벌침 세례', [dmg(0.7, EA), stk(K, 2)]),                                            // G 광역
      O('꿀 재고 점검', [dmg(0.9), per(K, { max: 4 }), dmg(0.28, ER)]),                      // 재설계 — 쌓기 대신 세기
      O('벌떼 소환', [dmg(1.4), stk(K, 4), tough(1)], { cost: 2 }),                         // C 강인도
    ], [B('독침', 'power'), B('따끔', 'frost'), B('날갯짓', 'ap')]),
    // u2 열기 — 대장 쥬비 호출(개전 강화 시동 카드 — 공격력 · 매 턴 벌)
    card(H, 2, '대장 쥬비 호출', 1, '강화', [stk(K, 2), ATK, hive()], [
      O('89호와 210호', [stk(K, 4), ATK, hive()], { tags: KAI, power: true }),              // A
      O('행복했쮸비', [stk(K, 2), ATK, power(rule('turnStart', [stk(K, 1)]), rule('play', [per(K, { max: 3 }), dmg(0.1, ER)], { when: { type: '공격' }, limit: 1 }))], { tags: KAI, power: true }),
      O('꿀 재고 확인', [stk(K, 3), ATK, power(rule('turnStart', [stk(K, 1)]), rule('kill', [draw(1)], { limit: 1 }))], { tags: KAI, power: true }),
      O('벌집 비상', [stk(K, 4), ATK, hive(2)], { power: true }),                            // H 개전 떼기
      O('귀여움 뽐내기', [stk(K, 4), ATK, draw(2)], { tags: KAI }),                              // 재설계 — 한 번에
    ], [B('꿀 한 숟갈', 'draw'), B('윙윙', [stk(K, 1)]), B('꿀단지', 'cost')], { tags: KAI }),
    // u3 터뜨리기 — 벌떼 돌격(한 마리마다 한 대 · 다 씀)
    card(H, 3, '벌떼 돌격', 1, '공격', [dmg(0.5), per(K), dmg(0.09, ER), spendAll(K)], [
      O('총공격', [dmg(0.6), per(K), dmg(0.11, ER), spendAll(K)]),                           // A
      O('복수와 유혈', [per(K), dmg(0.2, ER), spendAll(K), stk(K, 3)], { cost: 2 }),         // C 되모으기
      O('일부만 출격', [dmg(0.5), per(K, { max: 4 }), dmg(0.13, ER), spendN(K, 4)]),         // 쥐는 쪽
      O('벌떼 포위', [dmg(0.35, EA), per(K), dmg(0.06, EA)]),                               // 광역 · 쥐는 쪽
      O('꿀 도둑 추격', [per(K), dmg(0.14, ER), spendAll(K), ifKill, stk(K, 3)]),            // 재설계
    ], [B('성난 벌떼', 'power'), B('벌집 쑤시기', 'weakSpot'), B('가벼운 날개', 'cost')]),
    // u4 마무리 — 밀랍 둥지(쥔 벌 1개당 실드)
    card(H, 4, '밀랍 둥지', 1, '스킬', [stk(K, 1), per(K), sh(0.2)], [
      O('큰 벌집', [stk(K, 1), per(K), sh(0.25)]),                                          // A
      O('꽃밭 돌보기', [stk(K, 1), per(K), sh(0.2)], { tags: KAI }),                        // G
      O('꿀벌 경호대', [per(K), sh(0.45), spendAll(K)]),                                    // 재설계 · 대가
      O('온난화 걱정', [stk(K, 1), power(rule('hurt', [stk(K, 1)], { limit: 1 }))], { power: true }), // D
      O('행복한 꿀벌', [stk(K, 1), per(K), sh(0.2), srch()]),                                // F 서치
    ], [B('꿀벌 군단', 'atkUp'), B('밀랍 벽', 'guard'), B('꿀 냄새', [stk(K, 1)])]),
    // u5 굴리기 — 만우벌 협박: 거칠어진 꿀벌 떼가 광역으로 쏘고 벌 둘
    card(H, 5, '만우벌 협박', 1, '공격', [dmg(0.55, EA), stk(K, 2)], [
      O('신탁 1', [dmg(0.7, EA), stk(K, 2)]),
      O('신탁 2', [dmg(1.0), stk(K, 2), heal(0.3)]),
      O('신탁 3', [dmg(0.45, EA), stk(K, 1), power(rule('turnStart', [dmg(0.35, ER)]))], { power: true }),
      O('신탁 4', [dmg(0.55, EA), stk(K, 2), ifStack(K, 6), dmg(0.4, EA)]),
      O('신탁 5', [payPct(0.03), dmg(0.75, EA), stk(K, 3)]),
    ], [B('축복 1', 'power'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '쥬비_u2');
}

// 마무리(2026-10-08) — 기준 밖 사도의 고유 · 생성 카드 피해 · 실드 · 회복 배율(혼자 완주율을 역할 평균 안으로)
function scaleM(j, m) {
  const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal'].includes(f.k) && f.ratio) f.ratio = Math.round(f.ratio * m * 100) / 100; if (f.k === 'power') for (const r of f.rules) mul(r.fx); } };
  for (const c of j.cards) { if (!c.unique && !c.token) continue; mul(c.fx); for (const o of c.oracles || []) mul(o.fx); for (const b of c.blesses || []) mul(b.fx); }
}
const SM = (fn, m) => j => { fn(j); scaleM(j, m); };
// u5(2026-10-08) — 새 카드 몫만 배율(혼자 완주율이 u5 로 튄 사도)
const U5 = (fn, m) => j => { fn(j); const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal'].includes(f.k) && f.ratio) f.ratio = Math.round(f.ratio * m * 100) / 100; if (f.k === 'power') for (const r of f.rules) mul(r.fx); } }; for (const c of j.cards) if (/_u5$/.test(c.id)) { mul(c.fx); for (const o of c.oracles || []) mul(o.fx); } };
run([
  ['정령/가비아', U5(gabia, 1.3)], ['정령/나이아', naia], ['정령/니콜', nicole], ['정령/라이카', laika], ['정령/뮤트', mute], ['정령/미로', miro],
  ['정령/블랑셰', blanchet], ['정령/빅우드', bigwood], ['정령/시저', scizor], ['정령/실라', sylla], ['정령/아르코', arco], ['정령/아일라', U5(ayla, 1.3)],
  ['정령/오로라', SM(aurora, 0.7)], ['정령/우이', ui], ['정령/우이_기억', uiMemory], ['정령/이프리트', ifrit], ['정령/잉클', inkle], ['정령/쥬비', jubee],
], new URL('./boost_정령.json', import.meta.url));
