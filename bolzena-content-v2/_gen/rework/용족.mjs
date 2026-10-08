// 사도 리워크 — 용족 14명. 2단계(2026-10-07) 틀 위에 3단계 「카제나 자료 보강」(2026-10-08)을 얹었다. 루드는 시범(rework.mjs)에서 끝났다.
// 지침: _measure/사도_리워크_지침.md(§12 = 3단계) · 보고: _measure/리워크_용족.md · 공용 부품: lib.mjs
// node _gen/rework/용족.mjs   → heroes/용족/<사도>.json 덮어쓰기(백업에서 읽음)
// 3단계에서 바꾼 것(사도마다 같은 틀):
//  - 신탁 다섯 갈래 = 수치(얕음 하나) · 비용↓/태그(얕음 하나까지) · 재설계 하나 이상 · 강화화(D) 또는 서치(F) — 그 카드의 BEST 후보 · 대가(H) 또는 조건(E)
//  - ④ 완성형은 「장치를 세는 1코 마무리」(쓰지 않고 센다 — ③ 은 다 쓰고 센다). 옛 상시 엔진은 ④ 의 D 갈래로. ④ 강화 카드는 원작이 상시 효과인 넷만(퓨어샤인 · 시스트 · 아네트 · 실비아★)
//  - 개전 강화 시동 셋(퓨어샤인 · 시스트 · 아네트 — ④ 가 시동 카드) · 기본 카드 연료 넷(네티 · 리츠 · 시스트 · 피라)
//  - 축복 12개: 카드마다 [기존 효과 강화 · 유틸 · 장치/아군] — 같은 kind 셋까지, 장치를 거드는 축복 2~3개
import { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifAll, ifBroken, inspire, power, rule, later, O, B, card, starter, run } from './lib.mjs';

// ── 이 스크립트 몫 부품 ──
const LOW = 'lowEnemy';
const spendN = (id, v) => ({ k: 'spend', id, v });
const tough = (v, t = E1) => ({ k: 'tough', v, target: t });
const handEnd = { k: 'when', on: 'handEnd' };
const ifNth1 = { k: 'ifNth', n: 1 };
const rush = (t = EA) => ({ k: 'rushDown', v: 1, target: t });
const xtra = (r, t) => (t ? { k: 'extra', ratio: r, target: t } : { k: 'extra', ratio: r });
const payHp = v => ({ k: 'payHp', v });
const xform = (id, from) => ({ k: 'transform', id, from, n: 1 });
const discard1 = { k: 'discard', v: 1 };
// 3단계 — 서치 · 기본 카드 연료
const srchU = () => draw(1, { who: 'self', unique: true });                       // 자신의 고유 카드 1장 드로우
const srchO = (type) => draw(1, type ? { who: 'other', type } : { who: 'other' }); // 다른 사도 카드 1장 드로우
const pullU = () => ({ k: 'pull', from: 'discard', who: 'self', unique: true });  // 버린 더미의 자신의 고유 카드 1장을 손으로
const pullB = () => ({ k: 'pull', from: 'discard', basic: true });                // 버린 더미의 기본 카드 1장을 손으로
const ifPulledB = { k: 'ifPulled', basic: true };
const exBasic = () => ({ k: 'exileFrom', from: 'hand', basic: true, n: 1 });     // 손의 무작위 기본 카드 1장 소멸
const exTag = id => ({ k: 'exileFrom', from: 'hand', all: true, tag: id });       // 손의 그 생성물 모두 소멸
// 고유 카드 넷을 갈아 끼운다(기본 · 생성 카드는 그대로 두고, 고유만)
const setCards = (j, cards) => { j.cards = [...j.cards.filter(c => !c.unique), ...cards]; };
// 고학년의 고유 효과 참조만 새 이름으로(고학년 fx · cue 순서는 그대로)
const renameUlt = (h, from, to) => {
  const fix = fx => { for (const f of fx) { if ((f.k === 'stack' || f.k === 'spend' || f.k === 'perStack' || f.k === 'ifStack') && f.id === from) f.id = to; } };
  fix(h.ult.fx);
};
const tokenOf = (j, id) => j.cards.find(c => c.id === id);
// 사도 몫 숫자 배율(2단계 측정 뒤) — 고유 · 생성 카드의 기본형과 신탁 피해 · 실드 · 회복을 같은 비율로(신탁 값어치 비는 그대로)
const scaleU = (j, m) => {
  const mul = fx => { for (const x of fx || []) { if (['dmg', 'shield', 'heal'].includes(x.k) && x.ratio) x.ratio = Math.round(x.ratio * m * 100) / 100; if (x.k === 'power') for (const r of x.rules) mul(r.fx); } };
  for (const c of j.cards) if (c.unique || c.token) { mul(c.fx); for (const o of c.oracles || []) mul(o.fx); }
};
// 개전 강화 시동(§12-2): 그 카드에 개전 태그 — 신탁에도(대가 갈래 「개전 빼기」 만 뺀다)
const T_OPEN = { tags: ['개전'] };
// u5(2026-10-08) — 종류 서치 · 비용↓
const srchT = type => draw(1, { who: 'self', type });                                     // 자신의 그 종류 카드 1장 드로우
const costDown = type => ({ k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'self', type });   // 손의 자신의 그 종류 카드 1장 비용 -1

// ════════════════════════════════════════════════════════════════════
// 1. 네티 — 탱커 · 광기. 드릴로 캐낸 광물을 「발굴 자루」에 담는다 — 들고 있으면 모든 실드가 두꺼워지고, 쏟으면 파티를 한 번에 덮는다
// ════════════════════════════════════════════════════════════════════
function netty(j) {
  const H = '네티', K = '발굴 자루';
  const h = j.heroes[0];
  renameUlt(h, '유물 발굴', K);
  h.keyword = {
    name: K, desc: '드릴로 캐낸 광물 꾸러미', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.12 }],
    rules: [{ name: '자루가 터진다용', when: { on: 'stackOver', id: K }, limit: { per: 'turn', n: 1 }, fx: [sh(0.8)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '드릴 굴착', when: { on: 'play' }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1)] },
    { name: '너로 정했다! 초정밀 탐사!', when: { on: 'shieldBreak' }, limit: { per: 'turn', n: 1 }, fx: [st('피해 감소', 1)] },
  ];
  setCards(j, [
    // u1 열기 — 원작 저학년 「낙석 조심」(파티 보호막) · 시동 카드
    card(H, 1, '낙석 조심', 1, '스킬', [sh(1.2), stk(K, 2)], [
      O('대형 낙석', [sh(1.6), stk(K, 2)]),
      O('흔들린 지층', [sh(1.2), stk(K, 2), ifWounded, sh(0.8)]),
      O('광맥 발견', [exBasic(), stk(K, 3), per(K), sh(0.3)]),                 // 재설계 · 기본 카드 연료(대가)
      O('발굴 지도', [sh(1.1), stk(K, 2), srchU()]),                           // F 서치 — BEST
      O('무너진 갱도', [sh(2.6), stk(K, 4)], { tags: ['소멸'] }),               // H 소멸 한 방
    ], [B('최신형 지형 스캐너', 'guard'), B('느긋한 수면법', { tags: ['보존'] }), B('안전모 하나 더', [stk(K, 1)])]),
    // u2 굴리기 — 드릴 3회 · 분쇄(원작 평타)
    card(H, 2, '드릴 굴착', 1, '공격', [dmg(0.38, E1, { hits: 3 }), stk(K, 1)], [
      O('다이아 드릴', [dmg(0.5, E1, { hits: 3 }), stk(K, 1)], { tags: ['분쇄'] }),
      O('포크레인 드래곤', [dmg(0.42, EA, { hits: 3 }), stk(K, 2)], { cost: 2, tags: ['분쇄'] }),   // 비용↑ = 광역 + 자루 2
      O('광맥 따라', [dmg(0.35, E1, { hits: 3 }), per(K), sh(0.3)], { tags: ['분쇄'] }),             // 재설계(쥐고 실드)
      O('쉬지 않는 드릴', [stk(K, 1), dmg(0.3, E1, { hits: 2 }), power(rule('turnEnd', [dmg(0.2, ER, { hits: 2 }), stk(K, 1)]))], { power: true, tags: ['분쇄'] }),  // D — BEST
      O('폭탄은 질색', [dmg(0.35, E1, { hits: 3 }), per(K), dmg(0.3), spendAll(K)], { tags: ['분쇄'] }),   // H 다 쏟음
    ], [B('다이아 드릴 날', 'power'), B('기름칠', 'ap'), B('생선학자 길냥이', [stk(K, 1)])], { tags: ['분쇄'] }),
    // u3 터뜨리기 — 자루를 다 쏟아 파티를 덮는다(보모 언니)
    card(H, 3, '보모 언니의 나눔', 1, '스킬', [sh(0.8), per(K), sh(0.3), spendAll(K)], [
      O('반짝이는 광물', [sh(1.0), per(K), sh(0.4), spendAll(K)]),
      O('광물 감별', [sh(0.6), per(K), sh(0.3), spendAll(K)], { cost: 0 }),
      O('조금만 나눠 줄게용', [sh(1.0), ifStack(K, 4), sh(1.0)]),               // 재설계 — 쓰지 않고 쥔 채로
      O('발굴 천재 네티', [sh(0.9), stk(K, 1), power(rule('turnEnd', [per(K), sh(0.12)]))], { power: true }),   // D — BEST
      O('신생 용족 알 돌보기', [sh(0.6), per(K), heal(0.4), spendAll(K)]),      // 실드 ↔ 회복
    ], [B('보모의 손', 'heal'), B('모래 털기', 'draw'), B('유물 감정서', [st('피해 감소', 1)])]),
    // u4 완성형 — 자성 꼬리(1코 마무리: 자루를 쓰지 않고 세어 후려친다 — 결정화는 탱커 담당 버프)
    card(H, 4, '자성 꼬리', 1, '공격', [st('결정화', 1), ddef(0.45), per(K), ddef(0.15)], [
      O('초강력 자석', [st('결정화', 1), ddef(0.6), per(K), ddef(0.2)]),
      O('쇠붙이 수집', [st('결정화', 1), ddef(0.5), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('광물 자력', [st('결정화', 1), ddef(0.35, EA), stk(K, 2)]),            // 재설계 · 광역
      O('리츠까지 들러붙음', [st('결정화', 1), per(K), ddef(0.5), spendAll(K)]),   // H 다 쓴다
      O('자석 끌어당기기', [st('결정화', 1), per(K), ddef(0.3), pullU()]),     // F 회수
    ], [B('두 갈래 꼬리', 'power'), B('자철석', 'cost'), B('끌려온 칼끝', [st('반격', 1)])]),
    // u5 유틸(서치) — 잠금장치는 꼬리로 연다: 실드 + 자루 + 고유 카드 서치
    card(H, 5, '꼬리 열쇠', 1, '스킬', [sh(0.8), stk(K, 1), srchU()], [
      O('신탁 1', [sh(1.05), stk(K, 1), srchU()]),
      O('신탁 2', [sh(0.65), stk(K, 1), srchU()], { cost: 0 }),
      O('신탁 3', [sh(1.2), srchO(), ifWounded, heal(0.5)]),                                    // 재설계 — 장물 거래(동료 카드 · 회복)
      O('신탁 4', [sh(0.8), pullU(), srchU()]),                                          // F 회수 + 서치 — BEST
      O('신탁 5', [payHp(40), sh(1.2), stk(K, 3)]),                                        // H HP
    ], [B('축복 1', 'draw'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.45);
  starter(j, '네티_u1');
  for (const e of j.equips || []) e.affinityEffect = [{ name: '흔들린 지층', when: { on: 'hurt', guarded: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] }];
}

// ════════════════════════════════════════════════════════════════════
// 2. 다야 — 딜러 · 순수. 「다이아 쓰라림」이 박힌 적은 턴마다 갉히고, 「원석」은 던질까(쓰라림) · 둘 모아 진품 다이아로 다듬을까
// ════════════════════════════════════════════════════════════════════
function daya(j) {
  const H = '다야', K = '다이아 쓰라림', GEM = '다야_gem', REAL = '다야_real';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '살갗에 박힌 다이아 조각', carrier: 'enemy', cap: 3, decay: 1, per: [{ stat: 'dot', ratio: 0.25 }] };
  delete h.keywords;
  h.passives = [
    { name: '연쇄 피어스', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 2 }, fx: [make(GEM, 1)] },
    { name: '다이아의 반짝임', when: { on: 'break', mine: true }, fx: [stk(K, 2, E1)] },
  ];
  Object.assign(tokenOf(j, GEM), { cost: 0, type: '공격', tags: ['보존', '소멸'], evolve: { n: 2, into: REAL }, fx: [dmg(0.5), stk(K, 1, E1)] });
  Object.assign(tokenOf(j, REAL), { cost: 0, type: '공격', tags: ['소멸', '약점 공격'], fx: [dmg(1.4), stk(K, 2, E1)] });
  const each = per(K, { each: true });
  setCards(j, [
    // u1 터뜨리기 — 원작 저학년 다이아 피어스(넓은 광역 · 처치하면 다시 솟음)
    card(H, 1, '다이아 피어스', 2, '공격', [dmg(1.05, EA), each, dmg(0.2, EA), ifKill, make(GEM, 1)], [
      O('찬란한 피어스', [dmg(1.35, EA), each, dmg(0.25, EA), ifKill, make(GEM, 1)]),
      O('작은 피어스', [dmg(0.75, EA), each, dmg(0.15, EA), ifKill, make(GEM, 1)], { cost: 1 }),
      O('다이아 브레…', [dmg(1.7, EA), each, dmg(0.3, EA), st('고통', 2, EA)], { cost: 3 }),      // 비용↑ = 고통 2 덤 · 재설계
      O('완벽주의', [dmg(1.55, EA), each, dmg(0.3, EA)], { tags: ['종극'] }),                    // H 종극
      O('원석 박힌 피어스', [dmg(0.9, EA), perTag(GEM), dmg(0.25, EA), ifKill, make(GEM, 1)]), // F 손의 원석 1장당
    ], [B('다이아몬드 옥좌', 'power'), B('보석 세공 일정', 'ap'), B('빛나는 짙은 밤', [make(GEM, 1)])]),
    // u2 열기 — 다이아를 박아 쓰라림을 연다 · 시동 카드
    card(H, 2, '다이아 박기', 1, '공격', [dmg(0.9), stk(K, 2, E1)], [
      O('완벽한 박기', [dmg(1.2), stk(K, 2, E1)]),
      O('흠집 찾기', [dmg(0.95), stk(K, 2, E1)], { tags: ['약점 공격'] }),
      O('쓰라린 상처', [dmg(0.9), stk(K, 2, E1), ifStack(K, 3), dmg(0.7)]),
      O('원석 캐기', [dmg(0.75), make(GEM, 2)]),                                  // 재설계 — 쓰라림 대신 원석
      O('보석 감정', [dmg(0.8), stk(K, 2, E1), srchU()]),                         // F 서치 — BEST
    ], [B('다이아 손톱', 'weakSpot'), B('파워 그라인더', 'draw'), B('중금속 팩', [stk(K, 1, E1)])]),
    // u3 굴리기 — 단골 손님(원석을 사들인다)
    card(H, 3, '단골 손님', 1, '스킬', [make(GEM, 2), draw(1)], [
      O('VIP 단골', [make(GEM, 3), draw(1)]),
      O('덤으로 하나', [make(GEM, 2)], { cost: 0 }),
      O('유리도 다이아', [make(GEM, 1), draw(1), power(rule('turnStart', [make(GEM, 1)]))], { power: true }),   // D — BEST
      O('감정 의뢰', [make(GEM, 2), draw(2, { who: 'self', type: '공격' })]),     // 서치
      O('반지 원정대', [make(GEM, 1), perTag(GEM), dmg(0.6, ER), exTag(GEM)]),    // 재설계 · 대가 — 손의 원석을 다 던진다
    ], [B('보석 상자', 'power'), B('사기 단골', { tags: ['보존'] }), B('반짝 포장', 'cost')]),
    // u4 완성형 — 완벽해지는 소원(1코 마무리: 그 적의 쓰라림을 센다)
    card(H, 4, '완벽해지는 소원', 1, '공격', [dmg(0.7), per(K), dmg(0.35)], [
      O('완벽한 소원', [dmg(0.9), per(K), dmg(0.45)]),
      O('자기 관리', [dmg(1.0), make(GEM, 1), power(rule('play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('소원 성취', [dmg(1.0), make(REAL, 1)]),                                   // 재설계 — 바로 진품 다이아
      O('다 털어 넣기', [dmg(1.6), per(K), dmg(0.8)], { tags: ['소멸'] }),         // H 소멸 한 방
      O('첫 소원', [dmg(0.7), per(K), dmg(0.35), srchU()]),                        // F 서치
    ], [B('완벽한 자세', 'atkUp'), B('아침 몸단장', 'draw'), B('반짝임', [make(GEM, 1)])]),
    // u5 유틸(서치) — 망치로 쳐 봐야 안다: 쓰라림 + 공격 서치(원석은 신탁 쪽 — 혼자 +3.3%p 라 기본형에서 뺐다)
    card(H, 5, '망치 감별', 0, '스킬', [stk(K, 1, E1), srchT('공격')], [
      O('신탁 1', [make(GEM, 2), srchT('공격')]),
      O('신탁 2', [make(GEM, 1), srchT('공격'), stk(K, 1, E1)], { tags: ['보존'] }),
      O('신탁 3', [heal(0.8), make(GEM, 1), srchO()]),                                                             // 재설계 — 선의의 마사지(동료 카드)
      O('신탁 4', [make(GEM, 1), draw(2, { who: 'self', type: '공격' })]),                          // F 공격 둘 — BEST
      O('신탁 5', [payHp(40), dmg(0.8), make(REAL, 1)]),                                                       // H 거금 — 바로 진품
    ], [B('축복 1', 'ap'), B('축복 2', 'cost'), B('축복 3', [make(GEM, 1)])]),
  ]);
  scaleU(j, 1.2);
  starter(j, '다야_u2');
  for (const e of j.equips || []) e.affinityEffect = [{ name: '감정 접수', when: { on: 'fightStart' }, fx: [make(GEM, 1)] }];
}

// ════════════════════════════════════════════════════════════════════
// 3. 다야(퓨어샤인) — 서포터(회복 축) · 광기. 발밑의 「퓨어☆영역」 이 턴마다 파티를 낫게 한다 — 영역을 쥐고 있을까, 스타☆샤인으로 터뜨릴까
// 3단계: ④ 「매지컬☆샤인 영역」(원작 상시 영역)이 개전 강화 시동 카드
// ════════════════════════════════════════════════════════════════════
function pureshine(j) {
  const H = '다야_퓨어샤인', K = '퓨어☆영역', FIN = '다야_퓨어샤인_finish';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '발밑에 펼친 반짝이는 마법진', carrier: 'self', cap: 3, endDecay: 1, per: [{ stat: 'hot', ratio: 0.25 }] };
  delete h.keywords;
  h.passives = [
    { name: '퓨어☆포즈', when: { on: 'play', who: 'any', nth: 4 }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1), heal(0.3)] },
    { name: '마법 소녀는 편지 쓰는 중', when: { on: 'lowHp', pct: 0.3 }, fx: [heal(1.5), st('피해 감소', 1)] },
  ];
  Object.assign(tokenOf(j, FIN), { cost: 0, type: '공격', tags: ['증발'], fx: [dmg(0.2, E1, { hits: 4 })] });
  setCards(j, [
    // u1 원작 저학년 매지컬☆샤인(보호막 + 영역)
    card(H, 1, '매지컬☆샤인', 1, '스킬', [sh(1.0), stk(K, 2)], [
      O('매지컬☆샤인 MAX', [sh(1.3), stk(K, 2)]),
      O('서둘러 끝내자', [sh(1.0), stk(K, 2), ifNth1, draw(1)]),
      O('반짝 반창고', [heal(1.2), stk(K, 2)]),                                    // 재설계 — 실드 ↔ 회복
      O('나의 아름다운 장미꽃에게', [sh(0.9), stk(K, 2), srchO()]),                 // F 동료 카드 서치 — BEST
      O('매지컬☆변신', [sh(1.0), stk(K, 1), power(rule('turnStart', [sh(0.4)]))], { power: true }),   // D
    ], [B('매지컬☆스태프', 'guard'), B('별빛 리본', 'draw'), B('하트 스티커', [stk(K, 1)])]),
    // u2 굴리기 — 매지컬☆에너지(원작 평타)
    card(H, 2, '매지컬☆에너지', 0, '공격', [dmg(0.6), stk(K, 1)], [
      O('에너지 충전', [dmg(0.8), stk(K, 1)]),
      O('에너지 파동', [dmg(0.45, EA), stk(K, 1)]),
      O('얼른 끝내자!', [dmg(0.6), stk(K, 1), ifNth1, stk(K, 1)]),
      O('샤이닝 피니시', [dmg(0.5), make(FIN, 1)]),                                // 재설계 · 생성
      O('영역 해방', [dmg(0.5), per(K), dmg(0.35), spendAll(K)]),                  // H 다 쓴다
    ], [B('반짝 별', 'power'), B('매지컬☆윙크', 'ap'), B('한 번 더 변신', [stk(K, 1)])]),
    // u3 터뜨리기 — 스타☆샤인(어사이드: 아군 회복 · 받는 피해 감소) — 영역을 다 써서 큰 실드
    card(H, 3, '스타☆샤인', 1, '스킬', [sh(0.6), per(K), sh(0.45), spendAll(K)], [
      O('스타☆샤인 MAX', [sh(0.8), per(K), sh(0.5), spendAll(K)]),
      O('편지 한 장 더', [sh(0.9), per(K), sh(0.3)]),                              // 쥐는 쪽
      O('기다림 끝의 답장', [heal(0.7), per(K), heal(0.55), spendAll(K)]),          // 재설계
      O('영원한 팬레터', [per(K), sh(0.55), spendAll(K), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — BEST
      O('팬레터 답장', [per(K), sh(0.6), spendAll(K), srchU()]),           // F 서치
    ], [B('하트 편지지', 'heal'), B('우체통', { tags: ['보존'] }), B('답장 기다림', [st('사기', 1)])]),
    // u4 완성형 — 매지컬☆샤인 영역(원작 상시 영역 · 개전 강화 시동 카드)
    card(H, 4, '매지컬☆샤인 영역', 1, '강화', [st('결의', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('반짝 영역 확장', [st('결의', 1), power(rule('turnStart', [stk(K, 2)]))], T_OPEN),
      O('반짝이 가루', [st('결의', 1), sh(0.6), power(rule('turnStart', [stk(K, 1)]))], T_OPEN),
      O('위기의 마법 소녀', [sh(1.2), heal(0.4), power(rule('turnStart', [stk(K, 1)]), rule('hurt', [heal(0.35)], { limit: 1 }))], T_OPEN),   // 재설계 — 버프 대신 즉시 보호
      O('응원 편지', [st('결의', 1), srchO(), power(rule('turnStart', [stk(K, 1)]))], T_OPEN),   // F 서치
      O('늦은 등장', [st('결의', 2), power(rule('turnStart', [stk(K, 1)]))]),                   // H 개전 빼기
    ], [B('마법진', 'defUp'), B('공연 준비', 'cost'), B('반짝 왕관', [heal(0.4)])], T_OPEN),
    // u5 굴리기(공격) — 변신이 길어지는 게 싫어 1초 만에 끝낸다: 영역 + 피해
    card(H, 5, '1초 컷', 1, '공격', [dmg(0.65), stk(K, 1)], [
      O('신탁 1', [dmg(0.85), stk(K, 1)]),
      O('신탁 2', [dmg(0.5), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [sh(0.8), stk(K, 1), heal(0.5)]),                                                 // 재설계 — 부끄러움 극복(지키기)
      O('신탁 4', [dmg(0.55), stk(K, 1), srchO()]),                                                  // F 동료 카드 — BEST
      O('신탁 5', [per(K), dmg(0.35), spendAll(K), make(FIN, 1)]),                                  // H 영역을 다 써서 피니시
    ], [B('축복 1', 'power'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.7);
  starter(j, '다야_퓨어샤인_u4');
}

// ════════════════════════════════════════════════════════════════════
// 4. 리츠 — 딜러 · 광기. 먼저 맞아야 폭발한다 — 맞을수록 「정당방위」가 쌓여 대검이 무거워지고, 「박살내주겠어!」로 다 쏟는다
// ════════════════════════════════════════════════════════════════════
function leets(j) {
  const H = '리츠', K = '정당방위';
  const h = j.heroes[0];
  renameUlt(h, '명분이 서면', K);
  h.keyword = { name: K, desc: '먼저 맞고 나서야 서는 명분', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }] };
  delete h.keywords;
  h.passives = [
    { name: '먼저 손대셨죠?', when: { on: 'hurt', guarded: true }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1)] },
    { name: '참는 자세', when: { on: 'always' }, conds: [{ c: 'stack', id: K, not: true }], fx: [{ k: 'takenMod', v: -0.2, target: 'party' }] },
  ];
  setCards(j, [
    // u1 터뜨리기 — 버튼이 눌렸다(정당방위를 다 쏟는 한 방)
    card(H, 1, '박살내주겠어!', 1, '공격', [dmg(0.75), per(K), dmg(0.25), spendAll(K)], [
      O('반말 모드', [dmg(0.95), per(K), dmg(0.3), spendAll(K)]),
      O('정당방위 성립', [dmg(0.9), ifStack(K, 2), dmg(0.8)]),                   // 재설계 — 쓰지 않고 쥔 채로
      O('되갚아 주기', [dmg(0.8), power(rule('hurt', [dmg(0.4, ER)], { limit: 2 }))], { power: true }),   // D — 맞을 때마다 되받아침 · BEST
      O('정당방위 과잉', [dmg(1.0), ifWounded, per(K), dmg(0.5), spendAll(K)]),
      O('대지 분쇄', [dmg(1.5), per(K), dmg(0.5), spendAll(K)], { tags: ['소멸', '분쇄'] }),   // H 소멸 한 방
    ], [B('대검 연마', 'power'), B('큰 웃음소리', 'draw'), B('뽀개기', [stk(K, 1)])]),
    // u2 열기 — 원작 저학년 담금질(집중 · 버티기) · 시동 카드
    card(H, 2, '담금질', 1, '스킬', [sh(1.1), stk(K, 2)], [
      O('완벽한 담금질', [sh(1.4), stk(K, 2)]),
      O('준비가 덜 됐는데요…', [sh(1.1), stk(K, 2), ifWounded, stk(K, 2)]),
      O('은둔 초고수의 수련', [sh(1.0), stk(K, 1), power(rule('hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 }))], { power: true }),   // D — BEST
      O('방구석 강철', [sh(1.2), stk(K, 2)], { tags: ['보존'] }),
      O('강철 제련', [sh(1.0), pullB(), ifPulledB, stk(K, 3)]),       // 재설계 · 서치 — 기본 카드를 다시 벼린다(기본 카드 연료)
    ], [B('직접 만든 갑옷', 'guard'), B('갑옷 수선', 'ap'), B('숫돌', [stk(K, 1)])]),
    // u3 굴리기 — 대검 밀어내기(원작 강화 평타: 넓은 범위 · 넉백)
    card(H, 3, '대검 밀어내기', 1, '공격', [dmg(0.65, EA), rush(), stk(K, 1)], [
      O('힘껏 밀어내기', [dmg(0.8, EA), rush(), stk(K, 1)]),
      O('한 놈만', [dmg(1.15), rush(E1), stk(K, 1)]),
      O('먼저 치셨으니까', [dmg(0.65, EA), stk(K, 1), ifStack(K, 3), dmg(0.4, EA)]),
      O('힘 빼기', [dmg(0.55, EA), per(K), dmg(0.15, EA), spendAll(K)]),  // 재설계 · 대가
      O('떠돌이 무사', [dmg(0.6, EA), stk(K, 1), srchU()]),                 // F 서치 — BEST
    ], [B('넓은 칼날', 'weakSpot'), B('강철 날', 'cost'), B('기합', [st('사기', 1)])]),
    // u4 완성형 — 은둔 초고수(1코 마무리: 정당방위를 쓰지 않고 센다)
    card(H, 4, '은둔 초고수', 1, '공격', [dmg(0.65), per(K), dmg(0.25)], [
      O('초고수의 경지', [dmg(0.85), per(K), dmg(0.3)]),
      O('이미지 트레이닝', [dmg(0.8), stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('대륙의 강자들', [dmg(0.55, EA), stk(K, 2)]),                              // 재설계 · 광역
      O('작심삼일', [dmg(0.7), per(K), dmg(0.45), spendAll(K)]),                   // H 다 쓴다
      O('사료스탕스 명예 대원', [dmg(0.65), per(K), dmg(0.25), srchU()]),           // F 서치
    ], [B('갑옷 애호가', 'atkUp'), B('로네의 조언', 'draw'), B('층간소음', [stk(K, 1)])]),
    // u5 유틸(서치) — 하위 용족의 「통곡의 벽」: 실드 + 정당방위 + 고유 카드 서치
    card(H, 5, '통곡의 벽', 1, '스킬', [sh(0.85), stk(K, 1), srchU()], [
      O('신탁 1', [sh(1.1), stk(K, 1), srchU()]),
      O('신탁 2', [sh(0.7), stk(K, 1), srchU()], { cost: 0 }),
      O('신탁 3', [sh(1.0), srchO(), ifWounded, stk(K, 2)]),                                    // 재설계 — 시스트를 방패로(동료 카드)
      O('신탁 4', [sh(0.85), stk(K, 1), pullU()]),                                              // F 회수 — BEST
      O('신탁 5', [payHp(40), sh(1.0), stk(K, 3)]),                                                  // H HP · 서치 빼기
    ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ]);
  scaleU(j, 1.35);
  starter(j, '리츠_u2');
  for (const e of j.equips || []) e.affinityEffect = [{ name: '먼저 치셨으니까', when: { on: 'hurt', guarded: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] }];
}

// ════════════════════════════════════════════════════════════════════
// 5. 비비 — 탱커 · 순수 · 엘다인. 수은 보호막이 깨지면 때린 적이 「수은 중독」 — 넷이면 갑옷이 녹는다(취약). 창으로 먼저 터뜨릴지, 넷까지 기다릴지
// 3단계: ④ 는 적 전체의 수은을 세는 광역 마무리(엘다인 한 단계 — 적마다 제 수은), 옛 상시 엔진은 ④ 의 D 갈래
// ════════════════════════════════════════════════════════════════════
function vivi(j) {
  const H = '비비', K = '수은 중독';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '스며드는 은빛 독', carrier: 'enemy', cap: 4, per: [{ stat: 'dot', ratio: 0.25 }],
    rules: [{ name: '녹아내린 갑옷', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), st('취약', 2, E1)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '수은 장막', when: { on: 'shieldBreak' }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 2, E1)] },
    { name: '소녀가 보호하겠사와요', when: { on: 'lowHp', pct: 0.4 }, fx: [sh(3.0), st('피해 감소', 2)] },
  ];
  const reach = (fx) => rule('stackReach', fx, { when: { id: K, n: 4 } });
  const each = per(K, { each: true });
  setCards(j, [
    // u1 열기 — 원작 저학년 「소녀에게 오시려구요?」(수은 보호막) · 시동 카드
    card(H, 1, '소녀에게 오시려구요?', 1, '스킬', [sh(1.3), stk(K, 1, E1)], [
      O('오호호', [sh(1.6), stk(K, 1, E1)]),
      O('명예로운 비비', [sh(1.4), stk(K, 1, E1)], { tags: ['보존'] }),
      O('귀족의 품격', [ddef(0.5), stk(K, 2, E1)]),                                // 재설계 — 막기 ↔ 찌르기
      O('궁지의 사와요', [sh(1.3), stk(K, 1, E1), ifWounded, sh(1.0)]),
      O('쇠사슬로 잠긴 상자', [sh(1.2), stk(K, 1, E1), srchU()]),                   // F 서치 — BEST
    ], [B('보석 목걸이', 'guard'), B('세탁의 용족', 'draw'), B('은 위장', [stk(K, 1, E1)])]),
    // u2 굴리기 — 독성 재채기(주변이 수은 중독)
    card(H, 2, '독성 재채기', 1, '공격', [ddef(0.35, EA), stk(K, 1, EA)], [
      O('대형 재채기', [ddef(0.45, EA), stk(K, 1, EA)]),
      O('연쇄 재채기', [ddef(0.75, EA), stk(K, 2, EA)], { cost: 2 }),              // 비용↑ = 수은 2(장치 한 단계)
      O('먼지 알레르기', [ddef(0.35, EA), stk(K, 1, EA), inspire, stk(K, 1, EA)]),
      O('알레르기 체질', [ddef(0.3, EA), power(rule('turnEnd', [stk(K, 1, EA)]))], { power: true }),   // D — 매 턴 재채기 · BEST
      O('콜록콜록', [ddef(0.3, EA), each, ddef(0.1, EA)]),                          // 재설계 — 쌓지 않고 센다
    ], [B('에취', 'power'), B('수은 안개', 'ap'), B('손수건', [sh(0.4)])]),
    // u3 터뜨리기 — 은빛 창 네 자루(원작 강화 평타) — 쌓인 수은을 창으로 터뜨린다
    card(H, 3, '은빛 창 네 자루', 1, '공격', [ddef(0.4), per(K), ddef(0.15), spendAll(K)], [
      O('퀵실버 창', [ddef(0.5), per(K), ddef(0.18), spendAll(K)], { tags: ['분쇄'] }),
      O('창 한 자루', [ddef(0.3), per(K), ddef(0.12), spendAll(K)], { cost: 0, tags: ['분쇄'] }),
      O('녹아내리는 갑옷', [ddef(0.5), ifStack(K, 3), st('취약', 2, E1)], { tags: ['분쇄'] }),   // 재설계 — 쓰지 않고 취약
      O('사와요체 창술', [ddef(0.35), per(K), ddef(0.12), power(reach([ddef(0.3)]))], { power: true, tags: ['분쇄'] }),   // D — BEST
      O('창 회수', [per(K), ddef(0.5), spendAll(K), pullU()], { tags: ['분쇄'] }),   // F 회수
    ], [B('은빛 창날', 'weakSpot'), B('창 자루 밀기', 'cost'), B('창끝 독', [stk(K, 1, E1)])], { tags: ['분쇄'] }),
    // u4 완성형(엘다인 한 단계) — 적마다 제 수은을 세어 뿌리째 친다(쓰지 않음)
    card(H, 4, '세계수 뿌리에 수은을', 1, '공격', [st('결의', 1), ddef(0.25, EA), each, ddef(0.12, EA)], [
      O('뿌리 깊이', [st('결의', 1), ddef(0.32, EA), each, ddef(0.15, EA)]),
      O('우로스 강림 계획', [st('결의', 1), ddef(0.35, EA), power(reach([ddef(0.4, EA)]))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('엄마라고 부른 날', [st('결의', 1), ddef(0.45, EA), stk(K, 2, EA)]),                       // 재설계 — 세지 않고 붓는다
      O('수은 범람', [discard1, ddef(0.45, EA), each, ddef(0.2, EA)]),   // H 손패 버리기
      O('작은 복수', [st('결의', 1), each, ddef(0.3, EA), srchU()]),   // F 서치
    ], [B('세계수의 원한', 'defUp'), B('수은 양동이', { tags: ['개전'] }), B('은빛 독', [st('피해 감소', 1)])]),
    // u5 유틸(동료 카드) — 수은 주스 대접(본인은 선의): 실드 + 수은 + 동료 카드
    card(H, 5, '수은 주스 대접', 1, '스킬', [sh(0.85), stk(K, 1, E1), srchO()], [
      O('신탁 1', [sh(1.1), stk(K, 1, E1), srchO()]),
      O('신탁 2', [sh(0.7), stk(K, 1, E1), srchO()], { cost: 0 }),
      O('신탁 3', [sh(1.2), { k: 'cleanse', v: 1 }, stk(K, 1, E1)]),                                               // 재설계 — 결벽증(정화)
      O('신탁 4', [sh(0.8), srchU(), srchO()]),                                      // F 고유 + 동료 — BEST
      O('신탁 5', [payHp(40), ddef(0.4, EA), stk(K, 2, EA)]),                                                       // H HP — 재채기 참다 터짐(적 전체)
    ], [B('축복 1', 'draw'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  scaleU(j, 1.25);
  starter(j, '비비_u1');
}

// ════════════════════════════════════════════════════════════════════
// 6. 시스트 — 딜러 · 광기. 처치 · 격파마다 「수수료」 — 쥐고 있으면 단가(피해)가 오르고, 바구니 값으로 치를 수도 있다. 쓰러뜨리면 「특급 배송」이 또 온다(킬스트릭)
// 3단계: ④ 「장사천재 시스트」(원작 상시 어사이드)가 개전 강화 시동 카드 · 「떨이 판매」 가 기본 카드를 팔아 수수료로(기본 카드 연료)
// ════════════════════════════════════════════════════════════════════
function sist(j) {
  const H = '시스트', K = '수수료', EXP = '시스트_express';
  const h = j.heroes[0];
  renameUlt(h, '밀수 장부', K);
  h.keyword = { name: K, desc: '거래마다 슬쩍 얹는 웃돈', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.1 }] };
  delete h.keywords;
  h.passives = [
    { name: '장사 수완', when: { on: 'kill', mine: true }, fx: [stk(K, 2)] },
    { name: '장사 수완', when: { on: 'break', mine: true }, fx: [stk(K, 2)] },
    { name: '개업 자금', when: { on: 'fightStart' }, fx: [stk(K, 2)] },
  ];
  // 생성 카드(2단계 새 카드) — 원작 저학년의 킬스트릭: 쓰러뜨리면 다음 총알이 배달된다
  j.cards.push({ id: EXP, name: '특급 배송', hero: H, token: true, cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(0.7, LOW), ifKill, make(EXP, 1)] });
  const atk10 = () => ({ k: 'atkMod', v: 0.1, run: true, target: 'self' });
  const shop = (...more) => power(rule('kill', [make(EXP, 1)], { limit: 2 }), ...more);
  const u3 = card(H, 3, '4차원 바구니', 2, '공격', [dmg(0.75, EA, { hits: 2 }), draw(1)], [
    O('잡동사니 폭탄', [dmg(0.95, EA, { hits: 2 }), draw(1)]),
    O('가벼운 척', [dmg(0.55, EA, { hits: 2 }), draw(1)], { cost: 1 }),
    O('초록 괴물™', [dmg(0.75, EA, { hits: 2 }), ifKill, stk(K, 3)]),             // 재설계
    O('바구니 털기', [dmg(0.6, EA, { hits: 2 }), per(K), dmg(0.2, EA), spendAll(K)]),   // 재설계 · 대가 — 수수료를 다 턴다
    O('배송 조회', [dmg(0.7, EA, { hits: 2 }), pullU()]),                           // F 회수 — BEST
  ], [B('무거운 척', 'power'), B('4차원 주머니', 'ap'), B('덤 끼워 주기', [stk(K, 1)])]);
  Object.assign(u3, { payWith: K, payRate: 2, payMix: true });
  setCards(j, [
    // u1 굴리기 — 원작 저학년 총알 배송(HP 가 가장 낮은 적 · 처치하면 다음 총알)
    card(H, 1, '총알 배송', 1, '공격', [dmg(0.95, LOW), stk(K, 1), ifKill, make(EXP, 1)], [
      O('레이징 불', [dmg(1.25, LOW), stk(K, 1), ifKill, make(EXP, 1)]),
      O('M1911', [dmg(1.0, LOW), stk(K, 1), ifKill, make(EXP, 1)], { tags: ['약점 공격'] }),
      O('잔불 배송', [dmg(0.95, LOW), ifBroken, dmg(0.8), ifKill, make(EXP, 1)]),
      O('택배 묶음', [dmg(0.8, LOW), per(K), dmg(0.15, LOW), ifKill, make(EXP, 1)]),   // 재설계 — 쌓지 않고 센다
      O('몰아 쏘기', [per(K), dmg(0.35, LOW), spendAll(K), ifKill, make(EXP, 2)]),   // H 다 쓴다
    ], [B('글록', 'weakSpot'), B('특급 포장', 'draw'), B('배송비', [stk(K, 1)])]),
    // u2 열기 — 떨이 판매(웃돈 + 손)
    card(H, 2, '떨이 판매', 1, '스킬', [stk(K, 2), draw(2)], [
      O('대박 세일', [stk(K, 3), draw(2)]),
      O('헤헤', [stk(K, 2)], { cost: 0 }),
      O('재고 정리', [exBasic(), stk(K, 3), ap(1)]),                                     // 재설계 — 기본 카드를 팔아 웃돈(기본 카드 연료)
      O('아군 타겟 상품', [stk(K, 3), draw(2, { who: 'self', type: '공격' })]),    // 서치
      O('장사 밑천', [stk(K, 2), draw(2), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — BEST
    ], [B('명품백', 'guard'), B('흥정', 'cost'), B('사장님', [st('사기', 1)])]),
    // u3 터뜨리기 — 4차원 바구니(값을 수수료로 치를 수 있다)
    u3,
    // u4 완성형 — 장사천재 시스트(원작 상시 · 개전 강화 시동 카드 — 처치할 때마다 배송)
    card(H, 4, '장사천재 시스트', 1, '강화', [atk10(), stk(K, 2), shop()], [
      O('엘리아스 2위 재산', [atk10(), stk(K, 3), power(rule('kill', [make(EXP, 1), stk(K, 1)], { limit: 2 }))], T_OPEN),
      O('우정을 건 승부', [atk10(), make(EXP, 2), shop()], T_OPEN),                  // 재설계 — 웃돈 대신 총알
      O('화성행 우주선', [atk10(), ifStack(K, 3), make(EXP, 2), shop()], T_OPEN),
      O('응원 대행 서비스', [dmg(0.95, LOW), srchU(), shop()], T_OPEN),          // F 서치 — BEST
      O('소상인 협회', [atk10(), stk(K, 4), shop()]),                                // H 개전 빼기
    ], [B('황금 저금통', 'atkUp'), B('호구 탐지', 'draw'), B('개업 준비', [make(EXP, 1)])], T_OPEN),
    // u5 유틸(비용) — 교주에게서 받는 바보세: 수수료 + 공격 카드 비용↓
    card(H, 5, '바보세 징수', 0, '스킬', [stk(K, 1), costDown('공격')], [
      O('신탁 1', [stk(K, 2), costDown('공격')]),
      O('신탁 2', [stk(K, 1), costDown('공격')], { tags: ['보존'] }),
      O('신탁 3', [make(EXP, 1), draw(1)]),                                                          // 재설계 — 가짜 뿔 날개(총알 배송)
      O('신탁 4', [stk(K, 1), srchU(), costDown('공격')]),                                          // F 서치 + 비용↓ — BEST
      O('신탁 5', [payHp(40), stk(K, 3), costDown('공격')]),                                        // H 자수정 엄살(HP)
    ], [B('축복 1', 'ap'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.7);
  starter(j, '시스트_u4');
}

// ════════════════════════════════════════════════════════════════════
// 7. 실비아 — 탱커 · 광기 · 엘다인. 티파티의 「초청객」 — 손님이 스킬을 내면 은방울이 거든다. 누구를 들일지가 수
// ════════════════════════════════════════════════════════════════════
function silvia(j) {
  const H = '실비아', K = '초청객';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '은석의 티파티에 들인 손님', carrier: 'hero', cap: 1 };
  delete h.keywords;
  h.passives = [
    { name: '그 어머니에 그 딸', when: { on: 'play', marked: K, type: '스킬' }, limit: { per: 'turn', n: 2 }, fx: [ddef(0.3, ER)] },
    { name: '어머니의 특별 강의', when: { on: 'lowHp', pct: 0.4 }, fx: [{ k: 'cleanse', v: 1 }, sh(2.5)] },
  ];
  const g1 = stk(K, 1, 'oneAlly'), gAll = stk(K, 1, 'allAllies');
  const tea = (r) => rule('play', [sh(r)], { when: { marked: K, type: '스킬' }, limit: 2 });
  setCards(j, [
    // u1 열기 — 초대장 건네기(손님 하나 + 실드) · 시동 카드
    card(H, 1, '초대장 건네기', 1, '스킬', [g1, sh(1.3)], [
      O('금박 초대장', [g1, sh(1.65)]),
      O('모두 모여라', [gAll, sh(1.2)]),
      O('외로움은 싫어', [g1, sh(1.3), ifWounded, heal(0.9)]),
      O('결투 장갑', [g1, ddef(0.6)]),                                               // 재설계 — 막기 ↔ 때리기
      O('초대 명단', [g1, sh(1.1), srchO('스킬')]),                                   // F 손님의 스킬 서치 — BEST
    ], [B('파스텔 보닛', 'guard'), B('세바스티안 인형', { tags: ['보존'] }), B('오호호', [g1])]),
    // u2 원작 저학년 궁극의 유희(티파티 — 모두 손님 · 적이 무르게) — 2코 하나
    card(H, 2, '궁극의 유희', 2, '스킬', [gAll, sh(2.0), st('취약', 1, EA)], [
      O('완벽한 티타임', [gAll, sh(2.5), st('취약', 1, EA)]),
      O('간단한 다과', [g1, sh(1.4), st('취약', 1, EA)], { cost: 1 }),
      O('진은의 대공', [gAll, heal(2.2), st('취약', 1, EA)]),                        // 재설계 — 실드 ↔ 회복
      O('소녀의 단아한 발걸음', [sh(2.4), st('취약', 2, EA), ddef(0.5, EA)], { tags: ['종극'] }),   // H 종극
      O('매일 티타임', [gAll, sh(2.0), power(rule('turnStart', [sh(0.35)]))], { power: true }),   // D — BEST
    ], [B('은분수', 'heal'), B('찻잔 세트', 'ap'), B('은방울', [st('피해 감소', 1)])]),
    // u3 굴리기 — 부채로 꾸짖기(원작 강화 평타: 센 적의 주는 피해 감소 — 약화 담당)
    card(H, 3, '부채로 꾸짖기', 1, '공격', [ddef(0.5), st('약화', 2, E1)], [
      O('따끔한 꾸중', [ddef(0.65), st('약화', 2, E1)]),
      O('꼬마라고 했죠?', [ddef(0.5), st('약화', 2, E1), ifWounded, ddef(0.45)]),
      O('찻잔 깨기', [g1, ddef(0.4), sh(0.6)]),                                      // 재설계 — 약화 대신 손님 · 실드
      O('체육관의 유망주', [ddef(0.45), st('약화', 2, E1), draw(1, { who: 'self', type: '스킬' })]),   // F 서치 — BEST
      O('잔소리 폭탄', [discard1, ddef(0.9), st('약화', 3, E1)]),                    // H 손패 버리기
    ], [B('은 부채', 'power'), B('볼 빵빵', 'draw'), B('잔기침', [sh(0.4)])]),
    // u4 완성형(엘다인 한 단계) — 티파티 준비: 손님이 스킬을 낼 때마다 실드까지(원작 엘다인 상시)
    card(H, 4, '티파티 준비', 1, '강화', [st('결의', 1), power(tea(0.3))], [
      O('성대한 티파티', [st('결의', 1), sh(0.6), power(tea(0.3))]),
      O('초대장 대량 발송', [gAll, sh(1.2), power(tea(0.3))]),                       // 재설계 — 결의 대신 모두 손님
      O('오후의 티타임', [st('결의', 1), power(tea(0.3))], { tags: ['개전'] }),
      O('어머니와 딸', [st('결의', 1), heal(0.5), power(tea(0.3))]),
      O('작은 다과회', [st('결의', 1), srchO('스킬'), power(tea(0.3))]),             // F 서치 — BEST
    ], [B('어머니와 딸', 'defUp'), B('은 찻주전자', 'cost'), B('각설탕', [gAll])]),
    // u5 굴리기(공격) — 결투를 걸고 다니며 장갑을 던진다: 손님 + 방어 기반 피해
    card(H, 5, '장갑 던지기', 1, '공격', [g1, ddef(0.65)], [
      O('신탁 1', [g1, ddef(0.85)]),
      O('신탁 2', [g1, ddef(0.5)], { cost: 0 }),
      O('신탁 3', [gAll, sh(1.6)]),                                                                  // 재설계 — 진은의 대공 개입(모두 손님 · 막기)
      O('신탁 4', [g1, ddef(0.5), power(rule('play', [ddef(0.25, ER)], { when: { marked: K, type: '공격' }, limit: 1 }))], { power: true }),   // D 손님이 칠 때마다 — BEST
      O('신탁 5', [payHp(40), ddef(1.0), st('약화', 1, E1)]),                                      // H HP · 손님 빼기
    ], [B('축복 1', 'power'), B('축복 2', 'ap'), B('축복 3', [g1])]),
  ]);
  scaleU(j, 1.15);
  starter(j, '실비아_u1');
}

// ════════════════════════════════════════════════════════════════════
// 8. 실피르 — 딜러 · 우울(동료 연계 — 채우기형). 언제나 두 번째 — 파티의 두 번째 카드마다 「2인자의 단검」을 챙기고, 한꺼번에 던질지 쥐고 셀지
// ════════════════════════════════════════════════════════════════════
function silphir(j) {
  const H = '실피르', K = '2인자의 단검';
  const h = j.heroes[0];
  renameUlt(h, '2인자 쟁탈전', K);
  h.keyword = { name: K, desc: '루드보다 하나 더 챙긴 단검', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }] };
  delete h.keywords;
  h.passives = [
    { name: '넘버 투', when: { on: 'play', who: 'any', nth: 2 }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 2)] },
    { name: '청금이라 부르지 마', when: { on: 'stackOver', id: K }, limit: { per: 'turn', n: 1 }, fx: [{ k: 'perEvent' }, dmg(0.6, ER)] },
  ];
  const follow = (n) => rule('play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: n });
  const spd = { tags: ['신속'] };
  setCards(j, [
    // u1 터뜨리기 — 단검 세례(모은 단검을 다 던진다 — 고학년 아이콘 칸)
    card(H, 1, '단검 세례', 1, '공격', [dmg(0.8), per(K), dmg(0.2, ER), spendAll(K)], [
      O('킹갓zi존 단검비', [dmg(1.0), per(K), dmg(0.25, ER), spendAll(K)]),
      O('정정당당', [dmg(0.85), per(K), dmg(0.2, ER), spendAll(K)], { tags: ['약점 공격'] }),
      O('몇 자루는 아껴 두기', [dmg(0.9), ifStack(K, 3), dmg(0.25, ER, { hits: 3 })]),   // 재설계 — 쥔 채로
      O('2인자의 집념', [per(K), dmg(0.5, ER), spendAll(K), ifKill, stk(K, 2)]),
      O('단검 줍기', [per(K), dmg(0.35, ER), spendAll(K), pullU()]),         // F 회수 — BEST
    ], [B('사파이어 날', 'power'), B('호수 잠수', 'ap'), B('나이아의 응원', [stk(K, 1)])]),
    // u2 열기 — 2인자의 자존심(단검 챙기기) · 시동 카드
    card(H, 2, '2인자의 자존심', 1, '스킬', [stk(K, 3), draw(1)], [
      O('진짜 2인자', [stk(K, 4), draw(1)]),
      O('질투의 일격', [dmg(0.6), stk(K, 3)]),                                       // 재설계 — 손 대신 칼
      O('루드보다 먼저', [stk(K, 3), draw(2, { who: 'self', type: '공격' })]),       // 서치
      O('다야 님 곁으로', [stk(K, 3), draw(1), inspire, stk(K, 2)]),
      O('노력 끝의 보상', [stk(K, 2), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — BEST
    ], [B('반짝이는 것', 'guard'), B('쇼핑 봉투', 'cost'), B('웨스트 블루 제독님', [st('사기', 1)])]),
    // u3 굴리기 — 원작 저학년 창공의 지배자(광역 + 적을 늦춤)
    card(H, 3, '창공의 지배자', 1, '공격', [dmg(0.6, EA), st('둔화', 1, EA), stk(K, 1)], [
      O('창공의 패자', [dmg(0.75, EA), st('둔화', 1, EA), stk(K, 1)], spd),
      O('급강하', [dmg(1.1), st('둔화', 1, E1), stk(K, 1)], spd),
      O('청금이라 부르지 마!', [dmg(0.6, EA), stk(K, 1), ifStack(K, 4), dmg(0.4, EA)], spd),   // 재설계
      O('깃털 단검', [dmg(0.6, EA), per(K), dmg(0.12, EA), spendAll(K)], spd),   // H 다 던진다
      O('바람의 지배자', [dmg(0.7, EA), power(rule('turnStart', [stk(K, 1)]))], { power: true, tags: ['신속'] }),   // D — BEST
    ], [B('푸른 날개', 'weakSpot'), B('바람 가르기', 'draw'), B('비늘 깃', [stk(K, 1)])], spd),
    // u4 완성형 — 보석이라고 외쳐도(1코 마무리: 단검을 쓰지 않고 센다 · 사기 담당). 동료 연계 엔진은 D 갈래
    card(H, 4, '보석이라고 외쳐도', 1, '공격', [st('사기', 1), dmg(0.5), per(K), dmg(0.25)], [
      O('2인자의 품격', [st('사기', 1), dmg(0.65), per(K), dmg(0.32)]),
      O('영원한 라이벌', [st('사기', 1), dmg(1.1), power(follow(2))], { power: true }),   // D — 동료가 공격하면 단검(옛 상시 엔진) · BEST
      O('악우의 응원', [st('사기', 1), dmg(0.6, EA), stk(K, 3)]),                                   // 재설계 — 세지 않고 챙김
      O('전교 1등 목표', [st('사기', 1), per(K), dmg(0.45), spendAll(K)]),  // H 다 쓴다
      O('셀프 칭찬', [st('사기', 1), per(K), dmg(0.35), srchU()]),          // F 서치
    ], [B('사파이어 광택', 'atkUp'), B('도전장', { tags: ['개전'] }), B('풋풋한 학창 시절', [draw(1)])]),
    // u5 유틸(서치) — 전설의 검 「엑박스칼리버」를 찾아서: 실드 + 단검 + 공격 서치
    card(H, 5, '엑박스칼리버 탐색', 1, '스킬', [sh(0.7), stk(K, 2), srchT('공격')], [
      O('신탁 1', [sh(0.9), stk(K, 2), srchT('공격')]),
      O('신탁 2', [sh(0.55), stk(K, 2), srchT('공격')], { cost: 0 }),
      O('신탁 3', [srchO(), stk(K, 1), st('사기', 1)]),                                             // 재설계 — 나이아 받아 주기(동료 카드)
      O('신탁 4', [sh(0.75), stk(K, 2), pullU()]),                                            // F 회수 — BEST
      O('신탁 5', [discard1, sh(0.8), stk(K, 4)]),                                                   // H 손패 버리기 · 서치 빼기
    ], [B('축복 1', 'draw'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.55);
  starter(j, '실피르_u2');
}

// ════════════════════════════════════════════════════════════════════
// 9. 아네트 — 서포터(버퍼 축) · 광기. 한 명을 「오늘의 MVP」로 찍어 몰아준다 — 겹을 올릴수록 그 동료가 세지지만, 옮기면 처음부터
// 3단계: ④ 「싸움 구경」(원작 어사이드 상시 — 너는 나의 챔피언)이 개전 강화 시동 카드(MVP 는 패시브 「관중석」 이 전투 시작에 연다)
// ════════════════════════════════════════════════════════════════════
function arnet(j) {
  const H = '아네트', K = '오늘의 MVP';
  const h = j.heroes[0];
  // 고학년의 「관중 함성」 +3 은 MVP 겹으로(이름만 — cue 없음)
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' && f.id === '관중 함성' ? { ...f, id: K, target: 'strongestAlly' } : f));
  h.keyword = { name: K, desc: '아네트가 점찍은 오늘의 주인공', carrier: 'hero', cap: 2, hunt: true, per: [{ stat: 'dealt', v: 0.3, who: 'holder' }] };
  delete h.keywords;
  h.passives = [
    { name: '관중석', when: { on: 'fightStart' }, fx: [stk(K, 1, 'strongestAlly')] },
    { name: '싸워라 싸워~', when: { on: 'play', marked: K, type: '공격' }, limit: { per: 'turn', n: 2 }, fx: [heal(0.4)] },
  ];
  const mvp = (v = 1) => stk(K, v, 'oneAlly');
  const cheer = (r, n = 1) => rule('play', [xtra(r)], { when: { marked: K, type: '공격' }, limit: n });
  setCards(j, [
    // u1 굴리기 — 싸움 붙이기(협공 + MVP)
    card(H, 1, '결투 성사', 1, '스킬', [st('협공', 2), mvp()], [
      O('대결투', [st('협공', 3), mvp()]),
      O('즉석 매치', [st('협공', 2), dmg(0.6, EA)]),                                             // 재설계 — 협공 대신 MVP 둘
      O('지하 투기장', [st('협공', 2), mvp(), srchO('공격')]),                        // F 서치
      O('최강자는 누구냐!', [st('협공', 2), mvp(), ifAll, st('협공', 1)]),
      O('매일 밤 결투', [st('협공', 1), power(rule('turnStart', [st('협공', 1)]))], { power: true }),   // D — BEST
    ], [B('심판 호루라기', 'guard'), B('빠른 매칭', 'cost'), B('구경꾼 함성', [mvp()])]),
    // u2 연계 — 허접~(동료가 움직일 때 저절로 깐죽 — 취약 담당)
    card(H, 2, '허접~', 1, '공격', [dmg(0.6), st('취약', 2, E1)], [
      O('허접허접~', [dmg(0.8), st('취약', 2, E1)], { tags: ['연계'] }),
      O('깐죽 중계', [dmg(0.4, EA), mvp(), heal(0.3)], { tags: ['연계'] }),                  // 재설계 — 피해 대신 MVP
      O('대놓고 도발', [dmg(1.0), st('취약', 3, E1), mvp()]),                          // H 연계 빼기
      O('참교육 각', [dmg(0.6), st('취약', 2, E1), ifBroken, dmg(0.6)], { tags: ['연계'] }),
      O('레드카드 무서워', [dmg(0.55), st('취약', 2, E1), srchO()], { tags: ['연계'] }),  // F 서치 — BEST
    ], [B('얄미운 혀', 'weakSpot'), B('석류 아니야', 'draw'), B('메스가키', [mvp()])], { tags: ['연계'] }),
    // u3 원작 저학년 MVP 예측(가장 센 아군에게 몰아주기)
    card(H, 3, 'MVP 예측', 1, '스킬', [mvp(), sh(1.1)], [
      O('확실한 예측', [mvp(), sh(1.45)]),
      O('연속 선정', [mvp(2), sh(0.9)]),
      O('역전의 주인공', [mvp(), sh(1.1), ifWounded, heal(1.0)]),
      O('응원 도시락', [mvp(), heal(1.5)]),                                           // 재설계 — 실드 ↔ 회복
      O('승부 예측표', [mvp(), sh(1.0), srchO('공격')]),                               // F 서치 — BEST
    ], [B('확성기', 'heal'), B('특제 메달', 'ap'), B('판정 깃발', [st('사기', 1)])]),
    // u4 완성형 — 싸움 구경(원작 상시 · 개전 강화 시동 카드 — MVP 가 칠 때마다 함께)
    card(H, 4, '싸움 구경', 1, '강화', [st('사기', 1), power(cheer(0.35))], [
      O('VIP 관람석', [st('사기', 1), sh(0.5), power(cheer(0.35))], T_OPEN),
      O('한 판 더', [st('사기', 1), power(cheer(0.35), rule('kill', [draw(1)], { limit: 1 }))], T_OPEN),
      O('하루의 마무리', [mvp(), sh(1.6), power(cheer(0.35))], T_OPEN),            // 재설계 — 사기 대신 MVP · 회복
      O('개막전', [st('사기', 1), srchO('공격'), power(cheer(0.35))], T_OPEN),         // F 서치 — BEST
      O('팝콘', [st('사기', 1), sh(0.6), power(cheer(0.45))]),                        // H 개전 빼기
    ], [B('관람 예절', 'atkUp'), B('명당 자리', 'draw'), B('응원봉', [heal(0.3)])], T_OPEN),
    // u5 굴리기(공격) — 싸우라며 꿀밤: MVP + 피해
    card(H, 5, '결투 붙이는 꿀밤', 1, '공격', [dmg(0.75), mvp()], [
      O('신탁 1', [dmg(0.98), mvp()]),
      O('신탁 2', [dmg(0.55), mvp()], { cost: 0 }),
      O('신탁 3', [st('협공', 1), heal(0.6)]),                                                     // 재설계 — 채소끼리 결투(협공 · 회복)
      O('신탁 4', [dmg(0.6), mvp(), power(rule('play', [st('협공', 1)], { when: { marked: K, type: '공격' }, limit: 1 }))], { power: true }),   // D MVP 가 칠 때 협공 — BEST
      O('신탁 5', [payHp(40), dmg(1.1), st('취약', 2, E1)]),                                       // H HP · MVP 빼기
    ], [B('축복 1', 'power'), B('축복 2', 'ap'), B('축복 3', [mvp()])]),
  ]);
  scaleU(j, 1.35);
  starter(j, '아네트_u4');
}

// ════════════════════════════════════════════════════════════════════
// 10. 아라그니아 — 서포터(회복 축) · 냉정. 「진주 구슬」을 띄워 두면 파티가 다칠 때 알아서 날아가 낫게 한다 — 남은 구슬은 짐이 삼켜 실드로
// ════════════════════════════════════════════════════════════════════
function aragnia(j) {
  const H = '아라그니아', K = '진주 구슬';
  const h = j.heroes[0];
  renameUlt(h, '모래섬 왕국', K);
  h.keyword = {
    name: K, desc: '다친 백성에게 날아가는 구슬', carrier: 'self', cap: 6,
    rules: [{ name: '백성을 향한 구슬', when: { on: 'hurt' }, conds: [{ c: 'stack', id: K, n: 1 }], limit: { per: 'turn', n: 2 }, fx: [spendN(K, 1), heal(0.4)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '산호 씹기', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1)] },
    { name: '여왕은 백성을 버리지 않느니라', when: { on: 'lowHp', pct: 0.3 }, fx: [stk(K, 3), st('불굴', 1)] },
  ];
  setCards(j, [
    // u1 열기 — 원작 저학년 마음속의 펄(구슬 띄우기) · 시동 카드
    card(H, 1, '마음속의 펄', 1, '스킬', [stk(K, 3), sh(0.8)], [
      O('최강 진주', [stk(K, 4), sh(0.9)]),
      O('작은 펄', [stk(K, 2), sh(0.6)], { cost: 0 }),
      O('첫 백성 미로', [stk(K, 3), sh(0.8), ifWounded, heal(0.8)]),
      O('구슬 아홉', [stk(K, 3), sh(0.6), draw(1, { who: 'self', type: '스킬' })]),  // F 서치 — BEST
      O('짐이 지켜주겠느니라', [stk(K, 3), heal(1.5)]),                              // 재설계 — 실드 ↔ 회복
    ], [B('보석 산호', 'guard'), B('신성 진주 왕국', { tags: ['보존'] }), B('조개껍데기', [stk(K, 1)])]),
    // u2 터뜨리기 — 남은 구슬은 짐의 것(삼켜서 실드 + 손)
    card(H, 2, '남은 구슬은 짐의 것', 1, '스킬', [per(K), sh(0.35), spendAll(K), draw(1)], [
      O('최강의 식욕', [per(K), sh(0.45), spendAll(K), draw(1)]),
      O('반만 삼키기', [per(K, { max: 3 }), sh(0.45), spendN(K, 3), draw(1)]),
      O('진주 회복식', [per(K), heal(0.55), spendAll(K), draw(1)]),                  // 재설계
      O('생식 요리', [per(K), sh(0.3), spendAll(K), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — BEST
      O('진주 목록', [per(K), sh(0.3), spendAll(K), pullU()]),                       // F 회수
    ], [B('해양 간척', 'heal'), B('빙수 알바', 'ap'), B('짐의 하사품', [st('사기', 1)])]),
    // u3 굴리기 — 파도 한 줄기(약화 담당 — 원작 고학년의 주는 피해 감소)
    card(H, 3, '파도 한 줄기', 1, '공격', [dmg(0.6, EA), st('약화', 1, EA), stk(K, 1)], [
      O('해일', [dmg(0.75, EA), st('약화', 1, EA), stk(K, 1)]),
      O('물기둥', [dmg(1.05), st('약화', 2, E1), stk(K, 1)]),
      O('두 종류의 물방울', [dmg(0.65, EA), stk(K, 1), ifStack(K, 4), heal(0.9)]),   // 재설계
      O('썰물', [dmg(0.6, EA), per(K), dmg(0.15, EA), spendAll(K)]),   // H 다 쓴다
      O('바다의 흐름', [dmg(0.55, EA), st('약화', 1, EA), draw(1, { who: 'self', type: '스킬' })]),   // F 서치 — BEST
    ], [B('최강 꼬리', 'power'), B('잔잔한 바다', 'draw'), B('물보라', [stk(K, 1)])]),
    // u4 완성형 — 진주 왕국의 보물고(1코 마무리: 구슬을 쓰지 않고 센다 · 불굴은 담당 버프)
    card(H, 4, '진주 왕국의 보물고', 1, '스킬', [st('불굴', 1), sh(0.5), per(K), sh(0.15)], [
      O('최강 보물고', [st('불굴', 1), sh(0.65), per(K), sh(0.2)]),
      O('신성 진주 왕국 건설', [st('불굴', 1), sh(0.4), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('보물고 개방', [st('불굴', 1), per(K), heal(0.5), spendAll(K)]),              // 재설계 · 대가
      O('있는 그대로의 특별함', [st('불굴', 1), per(K), sh(0.3), ifWounded, heal(1.2)]),
      O('모래성', [st('불굴', 1), per(K), sh(0.2), srchO()]),               // F 서치
    ], [B('산호 왕관', 'defUp'), B('해안가 새 터', 'cost'), B('진주 한 알', [stk(K, 1)])]),
    // u5 굴리기(공격) — 바다의 흐름으로 배를 뒤집는다: 구슬 + 피해(공격 한 장뿐이라)
    card(H, 5, '바다 뒤집기', 1, '공격', [dmg(0.8), stk(K, 1)], [
      O('신탁 1', [dmg(1.05), stk(K, 1)]),
      O('신탁 2', [dmg(0.6), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [sh(1.25), stk(K, 2)]),                                                             // 재설계 — 산호 모래성(막기)
      O('신탁 4', [dmg(0.65), stk(K, 1), srchU()]),                                                  // F 서치 — BEST
      O('신탁 5', [per(K), dmg(0.35), spendAll(K), draw(1)]),                                                  // H 구슬을 다 쓴다
    ], [B('축복 1', 'power'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ]);
  scaleU(j, 1.0);
  starter(j, '아라그니아_u1');
}

// ════════════════════════════════════════════════════════════════════
// 11. 오팔 — 서포터(AP 축) · 순수. 선배님들이 맞을 때마다 「눈물 그렁」 — 셋이면 뚝 그치고 탭댄스(AP). 그 전에 울음을 쏟아 낫게 할 수도
// ════════════════════════════════════════════════════════════════════
function opal(j) {
  const H = '오팔', K = '눈물 그렁', TAP = '오팔_tap';
  const h = j.heroes[0];
  renameUlt(h, '눈물 보석', K);
  h.keyword = {
    name: K, desc: '선배님이 다칠 때마다 차오르는 눈물', carrier: 'self', cap: 3,
    rules: [{ name: '뚝!', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), ap(1), make(TAP, 1)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '울먹울먹', when: { on: 'hurt', guarded: true }, limit: { per: 'turn', n: 2 }, fx: [stk(K, 1)] },
    { name: '파사삭 보석', when: { on: 'play', type: '공격', every: 3 }, fx: [dmg(0.5, EA)] },
  ];
  j.cards.push({ id: TAP, name: '탭댄스', hero: H, token: true, cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.5), st('협공', 1), draw(1)] });
  setCards(j, [
    // u1 터뜨리기 — 선배님~!(울음을 쏟아 낫게)
    card(H, 1, '선배님~!', 1, '스킬', [heal(0.5), per(K), heal(0.3), spendAll(K)], [
      O('선배니임~!', [heal(0.65), per(K), heal(0.35), spendAll(K)]),
      O('눈물 방패', [sh(0.6), per(K), sh(0.4), spendAll(K)]),                       // 재설계 — 회복 ↔ 실드
      O('참는 중', [heal(0.7), per(K), heal(0.25)]),                                  // 쥐는 쪽
      O('칭찬받고 싶어', [heal(0.6), stk(K, 1), power(rule('stackReach', [heal(0.5)], { when: { id: K, n: 3 } }))], { power: true }),   // D — BEST
      O('훌쩍', [heal(0.35), per(K), heal(0.25), spendAll(K)], { cost: 0 }),
    ], [B('손수건', 'heal'), B('선배님 사진', { tags: ['보존'] }), B('에헤헤', [stk(K, 1)])]),
    // u2 열기 — 원작 저학년 가십 드래곤(양산 — 협공 + 실드) · 시동 카드
    card(H, 2, '가십 드래곤', 1, '스킬', [st('협공', 1), sh(0.8), stk(K, 1)], [
      O('특급 가십', [st('협공', 1), sh(1.1), stk(K, 1)]),
      O('양산 활짝', [st('협공', 2), sh(1.6), stk(K, 2)], { cost: 2 }),              // 비용↑ = 협공 2 · 눈물 2
      O('이달의 용족', [st('협공', 1), stk(K, 2), draw(2, { who: 'other' })]),        // 재설계 · 동료 카드 서치 — BEST
      O('에헤', [sh(0.8), stk(K, 1), ifStack(K, 2), st('협공', 2)]),
      O('뚝 그쳐', [sh(1.6), spendAll(K), ap(1)]),                     // H 눈물을 다 써서 AP
    ], [B('양산', 'guard'), B('파티 초대장', 'draw'), B('봉제 인형', [stk(K, 1)])]),
    // u3 굴리기 — 보석 던지기(원작 강화 평타)
    card(H, 3, '보석 던지기', 1, '공격', [dmg(0.5, ER, { hits: 2 }), stk(K, 1)], [
      O('큰 보석', [dmg(0.65, ER, { hits: 2 }), stk(K, 1)]),
      O('파사삭', [dmg(0.55, EA), stk(K, 1)]),
      O('후드리 찹찹', [dmg(0.5, ER, { hits: 2 }), stk(K, 1), ifStack(K, 2), dmg(0.5, ER)]),
      O('반짝 가루 뿌리기', [dmg(0.4, ER, { hits: 2 }), make(TAP, 1)]),               // 재설계 · 생성
      O('보석 수집', [dmg(0.4, ER, { hits: 2 }), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — 매 턴 눈물 · BEST
    ], [B('오팔 원석', 'power'), B('깨진 뿔', 'ap'), B('반짝 가루', [make(TAP, 1)])]),
    // u4 완성형 — 반짝이는 모든 것들(1코 마무리: 눈물을 쓰지 않고 세어 감싼다 · 사기 담당)
    card(H, 4, '반짝이는 모든 것들', 1, '스킬', [st('사기', 1), sh(0.5), per(K), sh(0.3)], [
      O('이달의 우수 용족', [st('사기', 1), sh(0.65), per(K), sh(0.4)]),
      O('눈물의 탭댄스', [st('사기', 1), sh(0.4), power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [draw(1)], { when: { id: K, n: 3 } }))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('하늘 위의 서프라이즈', [per(K), heal(0.8), spendAll(K), make(TAP, 1)]),   // 재설계 · 대가
      O('작은 칭찬', [st('사기', 1), per(K), sh(0.45), ifWounded, heal(0.9)]),
      O('파티 준비', [st('사기', 1), per(K), sh(0.35), srchO()]),            // F 서치
    ], [B('반짝 왕관', 'defUp'), B('탭슈즈', 'cost'), B('첫 파티', [heal(0.3)])]),
    // u5 굴리기(공격) — 총을 쥐면 「때찌때찌」: 눈물 + 피해
    card(H, 5, '때찌때찌', 1, '공격', [dmg(0.8), stk(K, 1)], [
      O('신탁 1', [dmg(1.05), stk(K, 1)]),
      O('신탁 2', [dmg(0.6), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [make(TAP, 1), heal(1.0), stk(K, 1)]),                                                        // 재설계 — 봉제 인형 선물
      O('신탁 4', [dmg(0.6), stk(K, 1), power(rule('stackReach', [dmg(0.4, EA)], { when: { id: K, n: 3 } }))], { power: true }),   // D 뚝! 마다 광역 — BEST
      O('신탁 5', [payHp(40), dmg(1.1), stk(K, 2)]),                                                 // H 뿔이 먼저 부서짐(HP)
    ], [B('축복 1', 'power'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.5);
  starter(j, '오팔_u2');
}

// ════════════════════════════════════════════════════════════════════
// 12. 제이드 — 딜러 · 냉정. 옥을 씹어 「비취옥」을 셋까지 — 들고 있으면 공격력 · 넘치면 마법서가 저절로 펼쳐지고, 옥장판에 다 털어 넣으면 한 방. 책은 쥐고 넘긴다(보존)
// ════════════════════════════════════════════════════════════════════
function jade(j) {
  const H = '제이드', K = '비취옥';
  const h = j.heroes[0];
  renameUlt(h, '소장 가치', K);
  h.keyword = {
    name: K, desc: '씹어 삼킨 옥돌의 마력', carrier: 'self', cap: 3, per: [{ stat: 'atk', v: 0.15 }],
    rules: [{ name: '마법서 펼치기', when: { on: 'stackOver', id: K }, limit: { per: 'turn', n: 1 }, fx: [dmg(0.8, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    { name: '포도맛 옥구슬', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
    { name: '마음의 양식', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1), draw(1)] },
  ];
  const keep = { tags: ['보존'] };
  setCards(j, [
    // u1 터뜨리기 — 원작 저학년 게르마늄 옥장판(비취옥 비례 광역 · 다 털어 넣음)
    card(H, 1, '게르마늄 옥장판', 2, '공격', [dmg(0.9, EA), per(K), dmg(0.3, EA), spendAll(K)], [
      O('프리미엄 옥장판', [dmg(1.1, EA), per(K), dmg(0.35, EA), spendAll(K)]),
      O('휴대용 옥장판', [dmg(0.6, EA), per(K), dmg(0.2, EA), spendAll(K)], { cost: 1 }),
      O('옥은 남겨 두기', [dmg(0.9, EA), ifStack(K, 3), dmg(0.9, EA)]),              // 재설계 — 쥔 채로
      O('책까지 데우기', [discard1, per(K), dmg(0.55, EA), spendAll(K)]),   // H 손패 버리기
      O('게르마늄 수면법', [dmg(0.8, EA), per(K), dmg(0.25, EA), power(rule('stackOver', [dmg(0.3, EA)], { when: { id: K }, limit: 1 }))], { power: true }),   // D — BEST
    ], [B('게르마늄 열기', 'power'), B('옥의 소공녀', 'ap'), B('완벽', [stk(K, 1)])]),
    // u2 열기 — 책에서 봤는데(보존 — 쥐고 넘기면 옥) · 시동 카드
    card(H, 2, '책에서 봤는데', 1, '스킬', [stk(K, 1), draw(2), handEnd, stk(K, 1)], [
      O('백과사전', [stk(K, 2), draw(2), handEnd, stk(K, 1)], keep),
      O('밑줄 긋기', [dmg(0.95), stk(K, 2)], keep),
      O('마법서 찾기', [stk(K, 2), pullU(), draw(2)], keep),                  // 회수
      O('절판 도서', [stk(K, 2), draw(3)]),                                           // H 보존 빼기
      O('독서 습관', [stk(K, 2), draw(2), power(rule('turnEnd', [stk(K, 1)], { conds: [{ c: 'held', n: 1 }] }))], { power: true, tags: ['보존'] }),   // D — 쥐고 넘기기를 상시로 · BEST
    ], [B('도수 없는 안경', 'guard'), B('책갈피', 'cost'), B('만년필', [st('사기', 1)])], keep),
    // u3 굴리기 — 비취 구슬탄(보존)
    card(H, 3, '비취 구슬탄', 1, '공격', [dmg(1.0), stk(K, 1), handEnd, stk(K, 1)], [
      O('옥구슬 연사', [dmg(1.3), stk(K, 1), handEnd, stk(K, 1)], keep),
      O('구슬 흩뿌리기', [dmg(0.7, EA), stk(K, 1), handEnd, stk(K, 1)], keep),
      O('꽉 찬 비취', [dmg(1.0), stk(K, 1), ifStack(K, 3), dmg(0.6)], keep),          // 재설계
      O('포도맛 사탕', [dmg(1.4), stk(K, 2)]),                                        // H 보존 빼기
      O('책갈피 탄', [dmg(0.9), draw(1, { who: 'self', type: '스킬' }), handEnd, stk(K, 2)], keep),   // F 서치 — BEST
    ], [B('비취 광택', 'weakSpot'), B('작은 녹색 현자', 'draw'), B('사탕 한 알', [stk(K, 1)])], keep),
    // u4 완성형 — 초대 교주의 수양록(1코 마무리: 비취옥을 쓰지 않고 센다)
    card(H, 4, '초대 교주의 수양록', 1, '공격', [dmg(0.6), per(K), dmg(0.3)], [
      O('수양록 완독', [dmg(0.8), per(K), dmg(0.38)]),
      O('서점털이 계획', [stk(K, 1), dmg(1.0), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('실내용 제이드', [dmg(0.8, EA), stk(K, 1)]),                                  // 재설계 · 광역
      O('베낀 사본', [dmg(0.6), per(K), dmg(0.5), spendAll(K)]),                      // H 다 쓴다
      O('서고 정리', [dmg(0.6), per(K), dmg(0.3), srchU()]),                          // F 서치
    ], [B('초대 교주의 필체', 'atkUp'), B('기록소', { tags: ['보존'] }), B('밀린 만화책', [stk(K, 1)])]),
    // u5 유틸(서치 · 보존) — 지식은 혼자 알아야 똑똑해진다: 비취옥 + 고유 카드 서치
    card(H, 5, '지식 독점', 0, '스킬', [stk(K, 1), srchU()], [
      O('신탁 1', [stk(K, 2), srchU()], keep),
      O('신탁 2', [dmg(0.7), handEnd, stk(K, 1)], keep),                                            // 재설계 — 왜곡된 지식(유사과학 일격)
      O('신탁 3', [stk(K, 1), srchU(), handEnd, stk(K, 1)], keep),                                 // E 쥐고 넘기면
      O('신탁 4', [stk(K, 1), pullU(), srchU()], keep),                                             // F 회수 + 서치 — BEST
      O('신탁 5', [stk(K, 2), draw(2)]),                                                             // H 보존 빼기
    ], [B('축복 1', 'draw'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1), sh(0.3)])], keep),
  ]);
  scaleU(j, 1.35);
  starter(j, '제이드_u2');
  for (const e of j.equips || []) e.affinityEffect = [{ name: '책값 메모', when: { on: 'turnEnd' }, conds: [{ c: 'held', n: 1 }], fx: [stk(K, 1)] }];
}

// ════════════════════════════════════════════════════════════════════
// 13. 키디언 — 딜러 · 우울(원작 방식 고학년 유지 — 처치하면 다시 뛰어든다). 깨뜨릴수록 「별빛 조각」이 모이고 — 쥐면 칼끝이 급소를 찾고, 다이브로 다 쏟는다
// 동료 연계(채우기형): 누가 깨뜨려도(동료의 격파) 별빛 2
// ════════════════════════════════════════════════════════════════════
function kidian(j) {
  const H = '키디언', K = '별빛 조각';
  const h = j.heroes[0];
  renameUlt(h, '그림자 마무리', K);
  h.keyword = { name: K, desc: '깨진 흑요석으로 그린 별자리', carrier: 'self', cap: 4, per: [{ stat: 'crit', v: 0.1 }, { stat: 'dealt', v: 0.06 }] };
  delete h.keywords;
  h.passives = [
    { name: '그림자 추적', when: { on: 'break' }, fx: [stk(K, 2)] },
    { name: '그림자 추적', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },
    { name: '흑요석 단면', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [dmg(0.6, ER)] },
  ];
  setCards(j, [
    // u1 굴리기 — 원작 저학년 아웃사이드 커터(한 명을 여러 번 베기)
    card(H, 1, '아웃사이드 커터', 1, '공격', [dmg(0.4, E1, { hits: 3 }), stk(K, 1)], [
      O('아웃사이더 헤딩', [dmg(0.4, E1, { hits: 4 }), stk(K, 1)]),
      O('급소 베기', [dmg(0.42, E1, { hits: 3 }), stk(K, 1)], { tags: ['약점 공격'] }),
      O('격파 틈', [dmg(0.4, E1, { hits: 3 }), stk(K, 1), ifBroken, dmg(0.6)]),
      O('별자리 긋기', [dmg(0.35, E1, { hits: 3 }), per(K), dmg(0.2)]),               // 재설계 — 쌓지 않고 센다
      O('그림자 수련', [dmg(0.3, E1, { hits: 3 }), power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // D — BEST
    ], [B('흑요석 날', 'power'), B('그림자 발걸음', 'ap'), B('조각 녹여 먹기', [stk(K, 1)])]),
    // u2 열기 — 급소 찾기(강인도 + 별빛) · 시동 카드
    card(H, 2, '급소 찾기', 0, '스킬', [tough(1), stk(K, 1)], [
      O('정확한 급소', [tough(1), stk(K, 2)]),
      O('그림자 숨기', [tough(1), stk(K, 1), draw(1, { who: 'self', type: '공격' })]),   // F 서치 — BEST
      O('깨진 흑요석', [payHp(40), tough(2), stk(K, 2)]),                              // H HP 치름
      O('밤눈', [tough(1), stk(K, 1), inspire, stk(K, 1)]),
      O('쿠나이 표시', [dmg(0.6), tough(1)]),                                         // 재설계
    ], [B('반창고', 'draw'), B('붕대', [sh(0.3)]), B('어둠 속', { tags: ['보존'] })]),
    // u3 터뜨리기 — 밤하늘 다이브(별빛을 다 쏟는 한 방)
    card(H, 3, '밤하늘 다이브', 1, '공격', [dmg(0.8), per(K), dmg(0.3), spendAll(K)], [
      O('쉐도우 커터', [dmg(1.0), per(K), dmg(0.35), spendAll(K)]),
      O('별똥별', [dmg(0.55, EA), per(K), dmg(0.2, EA), spendAll(K)]),
      O('별 하나는 남겨', [dmg(0.9), ifStack(K, 3), dmg(0.8)]),                        // 재설계 — 쥔 채로
      O('마지막 다이브', [discard1, per(K), dmg(0.55), spendAll(K)]),          // H 손패 버리기
      O('다시 뛰어들기', [per(K), dmg(0.45), spendAll(K), ifKill, pullU()]),  // F 처치하면 회수 — BEST(원작 처치 재시전)
    ], [B('별빛 쿠나이', 'weakSpot'), B('그림자 틈', 'cost'), B('친구따라 지상 나들이', [st('사기', 1)])]),
    // u4 완성형 — 별 관찰(1코 마무리: 별빛을 쓰지 않고 센다 · 잔광은 담당 버프)
    card(H, 4, '별 관찰', 1, '공격', [st('잔광', 1), dmg(0.5), per(K), dmg(0.25)], [
      O('마음속의 은하수', [st('잔광', 1), dmg(0.65), per(K), dmg(0.32)]),
      O('밤하늘의 별자리', [st('잔광', 1), dmg(0.9), power(rule('turnStart', [stk(K, 1)]), rule('break', [draw(1)], { limit: 1 }))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('동굴 밖으로', [st('잔광', 1), dmg(0.4, EA), stk(K, 2)]),                          // 재설계
      O('별 하나', [st('잔광', 1), per(K), dmg(0.45), spendAll(K)]),          // H 다 쓴다
      O('내가 빛내줄게…', [st('잔광', 1), per(K), dmg(0.35), srchU()]),       // F 서치
    ], [B('교주의 별자리', 'atkUp'), B('별 관찰 노트', 'draw'), B('흑요석 조각', [stk(K, 1)])]),
    // u5 유틸(서치) — 그늘에서 내다보는 정도는 괜찮다: 붕대(실드) + 별빛 + 공격 서치
    card(H, 5, '그늘에서 내다보기', 1, '스킬', [sh(0.7), stk(K, 1), srchT('공격')], [
      O('신탁 1', [sh(0.9), stk(K, 1), srchT('공격')]),
      O('신탁 2', [sh(0.55), stk(K, 1), srchT('공격')], { cost: 0 }),
      O('신탁 3', [tough(1), sh(1.1), ifBroken, stk(K, 3)]),                                       // 재설계 — 먼저 물러섰다 틈을(격파 조건)
      O('신탁 4', [sh(0.7), stk(K, 1), pullU()]),                                             // F 회수 — BEST
      O('신탁 5', [payHp(40), sh(0.8), stk(K, 3)]),                                                  // H 베인 손(HP) · 서치 빼기
    ], [B('축복 1', 'ap'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.15);
  starter(j, '키디언_u2');
}

// ════════════════════════════════════════════════════════════════════
// 14. 피라 — 서포터(버퍼 축) · 광기. 디버프를 걸 때마다 「부유함」을 걷는다 — 쥐고 있으면 파티의 눈이 반짝이고(주는 피해), 위조 금화로 쏟으면 한 방
// 기본 카드 연료: 「도금」 이 시작 카드를 도금 카드로 바꾼다
// ════════════════════════════════════════════════════════════════════
function pira(j) {
  const H = '피라', K = '부유함';
  const h = j.heroes[0];
  renameUlt(h, '도금 연금술', K);
  h.keyword = { name: K, desc: '번쩍이는 금 부스러기', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1, who: 'allies' }] };
  delete h.keywords;
  h.passives = [
    { name: '수금', when: { on: 'debuff' }, limit: { per: 'turn', n: 3 }, fx: [stk(K, 1)] },
    { name: '햇살 아래에서', when: { on: 'fightStart' }, fx: [stk(K, 2)] },
  ];
  setCards(j, [
    // u1 열기 — 연금술 실험(취약 + 부유함) · 시동 카드
    card(H, 1, '연금술 실험', 1, '스킬', [st('취약', 1, E1), stk(K, 1), draw(1)], [
      O('화금석 근사치', [st('취약', 2, E1), stk(K, 1), draw(1)]),
      O('도금', [xform('피라_g1', '피라_s1'), xform('피라_g2', '피라_s2'), stk(K, 2)]),   // 재설계 · 기본 카드 연료
      O('불꽃 튕기기', [st('취약', 1, E1), stk(K, 1)], { cost: 0 }),
      O('남의 수작', [st('취약', 1, E1), stk(K, 2), inspire, stk(K, 2)]),
      O('금서고 출입', [st('취약', 2, E1), stk(K, 1), srchU()], { tags: ['보존'] }),     // F 서치 — BEST
    ], [B('연금 솥', 'guard'), B('불꽃 손가락', 'cost'), B('황철석 가루', [stk(K, 1)])]),
    // u2 굴리기 — 원작 저학년 수금 시간이다!(광역 + 받는 피해 증가 — 취약 담당)
    card(H, 2, '수금 시간이다!', 1, '공격', [dmg(0.55, EA), st('취약', 1, EA)], [
      O('추심', [dmg(0.7, EA), st('취약', 1, EA)]),
      O('조각상 박살', [dmg(1.15, EA), st('취약', 2, EA), stk(K, 1)], { cost: 2 }),     // 비용↑ = 취약 2 · 부유함
      O('1대1 수금', [dmg(0.95), st('취약', 2, E1)]),
      O('이자까지', [dmg(0.55, EA), st('취약', 1, EA), ifStack(K, 3), dmg(0.4, EA)]),
      O('장부 정리', [dmg(0.5, EA), stk(K, 2)]),                                      // 재설계 — 취약 대신 부유함
    ], [B('황금 망치', 'power'), B('독촉장', 'draw'), B('이자', [stk(K, 1)])]),
    // u3 터뜨리기 — 위조 금화 뿌리기(원작 강화 평타 — 부유함을 다 뿌린다)
    card(H, 3, '위조 금화 뿌리기', 1, '공격', [dmg(0.3, ER, { hits: 3 }), per(K), dmg(0.25, ER), spendAll(K)], [
      O('금화 폭탄', [dmg(0.38, ER, { hits: 3 }), per(K), dmg(0.3, ER), spendAll(K)]),
      O('피버☆금화', [dmg(0.4, ER, { hits: 5 }), per(K), dmg(0.45, ER), spendAll(K)], { cost: 2 }),   // 비용↑ = 타수 5
      O('가짜도 반짝', [dmg(0.45, ER, { hits: 3 }), ifStack(K, 3), st('취약', 1, EA)]),   // 재설계 — 쥔 채로 취약
      O('뒷골목 사채', [dmg(0.3, ER, { hits: 3 }), stk(K, 1), power(rule('debuff', [dmg(0.3, ER)], { limit: 2 }))], { power: true }),   // D — BEST
      O('금화 회수', [per(K), dmg(0.4, ER), spendAll(K), pullU()]),   // F 회수
    ], [B('번쩍 금박', 'weakSpot'), B('댄스 크루 스파클', 'ap'), B('껌 좀 씹어본', [st('사기', 1)])]),
    // u4 완성형 — 화금석 연구(1코 마무리: 부유함을 쓰지 않고 센다 · 사기 · 결의 담당)
    card(H, 4, '화금석 연구', 1, '공격', [st('사기', 1), st('결의', 1), per(K), dmg(0.25, ER)], [
      O('수십 년의 연구', [st('사기', 1), st('결의', 1), per(K), dmg(0.33, ER)]),
      O('진짜 금', [st('사기', 1), st('결의', 1), power(rule('turnStart', [stk(K, 1), dmg(0.3, ER)]))], { power: true }),   // D — 옛 상시 엔진 · BEST
      O('반짝반짝 우리 사이', [st('사기', 1), dmg(0.7, EA), stk(K, 2)]),     // 재설계
      O('싸구려 플라스크', [st('사기', 1), per(K), dmg(0.4, ER), spendAll(K)]),   // H 다 쓴다
      O('실험 일지', [st('사기', 1), per(K), dmg(0.4, ER), srchU()]),   // F 서치
    ], [B('화금석', 'atkUp'), B('첫 실험', 'draw'), B('블링블링 우정 목걸이', [heal(0.3)])]),
    // u5 유틸(서치) — 사채로 받아 낸 물건 감정: 취약 + 부유함 + 공격 서치
    card(H, 5, '사채 감정', 1, '스킬', [st('취약', 2, E1), stk(K, 1), srchT('공격')], [
      O('신탁 1', [st('취약', 3, E1), stk(K, 1), srchT('공격')]),
      O('신탁 2', [st('취약', 1, E1), stk(K, 1), srchT('공격')], { cost: 0 }),
      O('신탁 3', [sh(0.5), srchO(), st('사기', 1)]),                                                         // 재설계 — 솔직해지라는 말(동료 카드 · 사기)
      O('신탁 4', [st('취약', 2, E1), stk(K, 1), power(rule('debuff', [stk(K, 1), dmg(0.2, ER)], { limit: 1 }))], { power: true }),   // D 디버프마다 부유함 — BEST
      O('신탁 5', [discard1, dmg(0.4, EA), st('취약', 2, EA)]),                                        // H 손패 버리기 — 광역 취약
    ], [B('축복 1', 'ap'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.85);
  starter(j, '피라_u1');
}

run([
  ['용족/네티', netty], ['용족/다야', daya], ['용족/다야_퓨어샤인', pureshine], ['용족/리츠', leets], ['용족/비비', vivi],
  ['용족/시스트', sist], ['용족/실비아', silvia], ['용족/실피르', silphir], ['용족/아네트', arnet], ['용족/아라그니아', aragnia],
  ['용족/오팔', opal], ['용족/제이드', jade], ['용족/키디언', kidian], ['용족/피라', pira],
], new URL('./boost_용족.json', import.meta.url));
