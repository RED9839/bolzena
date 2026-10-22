// 티그(영웅) — 딜러 · 광기 · 수인. 설계: _measure/엘다인_엔진/01_사도별.md §18 (4차 10-11 — 영웅심 순환 + 결착: 낮은 체력 추가 피해, 사용자 지시로 처형 뺌)
// 엔진(사용자 10-11 개정): 「험난한 영웅의 길」 3칸 순환은 뺐다 — 주 고유 효과는 검의 「영웅의 성장」, 보조는 「용기」
//   → 「영웅의 검」 은 적 HP 가 30% 아래로 내려오면 크게 친다(처형 대신 추가 피해 — 사용자 10-11). 자동 규칙은 길 하나.
// 4차 덧붙임(사용자 10-11 — 카제나 하루 꼴): 특성 카드 「영웅의 검」 은 낼 때마다 이번 전투 동안 「영웅의 성장」 +1(1당 피해 +40%, 최대 10) —
//   다른 티그 카드가 검을 손으로 회수하고 · 비용을 깎고 · 성장을 올려 「같은 검을 여러 번」 돌린다(비비(신성)은 쥐면 자람, 티그는 내고 다시 가져올수록 자람).
//   HP 40% 이하면 추가 피해는 검의 덤으로 남는다.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '티그_영웅';   // 사용자 10-11: 「험난한 영웅의 길」(일격 · 휩쓸기 · 함성 순환) 뺌 — 검의 성장 하나로
const tough = (v) => ({ k: 'tough', v });
const KEEP = ['보존'];
const twice = { k: 'ifFoe', hitsBy: 'self', n: 2 };   // ▶E71 이번 전투 티그가 그 적을 두 번 이상 쳤으면
const SWD = `${H}_u1`, G = '성장';
const grow = (v = 1) => ({ k: 'cardStatus', id: G, v, to: 'this', max: 6, battle: true });   // 10-19 밸런스: 최대 10 → 6   // 낸 뒤 이 카드 성장 +v(이번 전투)
const feed = (v = 1) => ({ k: 'cardStatus', id: G, v, to: 'hand', tag: SWD, max: 6, battle: true });   // 「영웅의 검」 성장 +v(손 → 뽑을 → 버린 더미)
const back = { k: 'pull', from: 'discard', n: 1, tag: SWD };   // 버린 더미의 「영웅의 검」 을 손으로
const low = (pct = 0.3) => ({ k: 'ifFoe', id: 'hp', pct });   // 그 적의 HP 가 pct 이하면(처형 대신 추가 피해 — 사용자 10-11)

export default hero({
  id: H, name: '티그(영웅)', nature: '광기', race: '수인', role: '딜러', star: 3,
  blurb: '영웅이 된 티그. 영웅의 검을 휘두를수록 검이 자라고, 다른 카드로 검을 다시 쥐고 · 싸게 하고 · 더 키워 같은 검을 몇 번이고 내리칩니다.',
  keyword: {
    name: G, desc: '영웅의 검을 낼수록 자라는 힘', carrier: 'self', cap: 6, cardValue: { max: 6 },   // 10-19 밸런스(사용자 「센 쪽 — 끝없이 커지는 부분의 상한」): 최대 10 → 6
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '용기', desc: '공격을 거듭하며 붙은 영웅의 용기', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.15 }],   // 1당 주는 피해 +15%(10-21 밸런스 조절 5차: 10 → 15%)(10-11 순환을 빼며 덧붙임)
    rules: [{ name: '영웅심', when: { on: 'play', type: '공격', token: false }, fx: [stack('용기', 1)] }] }],   // 공격마다 용기 +1(10-21 밸런스 조절 7차: 2장마다 → 1장마다 — 일반전 예열)
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '영웅으로 성장', when: { on: 'play', type: '공격', every: 3 }, fx: [{ k: 'atkMod', v: 0.05, run: true }] },   // 10-21 밸런스 조절: 4 → 3장마다 · 원작 4공격마다 성장 — 이 전투 공격력 +5%
  ],
  ult: { name: '맹호류 결착', cost: 250, fx: [dmg(2.6, 'allEnemies'), tough(1), low(), dmg(2)] },
}, {
  basicNames: ['검 휘두르기', '교차 베기', '북부 대공의 자세'], deck: 'light',
  uniques: [
    { // 시동 · 특성 카드 — 낼 때마다 이번 전투 성장 +1(1당 +80% — 10-11 순환을 빼며 올림) · HP 40% 이하면 추가 피해(처형 대신)
      name: '영웅의 검', cost: 2, type: '공격', tags: KEEP,
      fx: [dmg(3), { k: 'perCardSt', id: G }, dmg(0.6), grow(1)],   // 10-21 밸런스 조절: 260 → 300%(3차) · 성장 1당 50 → 60%(4차)   // 10-19 밸런스(센 쪽): 300 → 260% · 성장 1당 80 → 50%(신탁 맞춤이 신탁 2 · 3 의 바닥을 이 값으로 올린다)   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { tags: KEEP, fx: [dmg(1.2), { k: 'perCardSt', id: G }, dmg(0.6), grow(1)] },   // 10-19: 80 → 50% · 10-21: 50 → 60%
        { tags: KEEP, fx: [dmg(1.2), { k: 'perCardSt', id: G }, dmg(0.4), grow(2)] },   // 10-19: 60 → 40%
        { tags: [...KEEP, '회수 3'], fx: [dmg(1), { k: 'perCardSt', id: G }, dmg(0.4), grow(1)] },   // 10-19: 60 → 40%
        { tags: KEEP, fx: [dmg(1.8), low(0.4), dmg(1.8), grow(1)] },   // 낮은 체력 추가 피해(처형 대신)
        { cost: 1, tags: KEEP, fx: [dmg(2), { k: 'perCardSt', id: G }, dmg(0.6), grow(1)] },   // 10-21: 50 → 60%   // 10-19: 70 → 50%(성장 최대 6)   // 10-13 신탁 점검
      ],
      blesses: ['power', 'cost', [grow(1)]],
    },
    {
      name: '사슴류 베기', cost: 1, type: '공격',
      fx: [dmg(0.9, 'allEnemies', { hitsPer: '용기', hitsAdd: 2 }), stack('용기', 1)],   // 10-21 밸런스 조절: 50 → 90%(1 · 2 · 4 · 6차)   // 10-19 타수(사용자 「카드 최대 2타」): 엔진 R.PER_HIT_MERGE 가 (용기+2)회를 한 대 × (용기+2) 로 합친다(합 그대로 · 1타)
      oracles: [
        { fx: [dmg(0.6, 'allEnemies', { hits: 2 }), stack('용기', 1)] },   // 10-13 신탁 점검: 용기 되살림
        { fx: [dmg(1.2, 'oneEnemy', { hits: 2 }), stack('용기', 1)] },   // 10-13 신탁 점검: 용기 되살림 · 10-19 타수: 3회 × 80% → 2회 × 120%
        { fx: [dmg(0.5, 'allEnemies', { hits: 2 }), feed(1)] },   // 10-13 신탁 점검
        { cost: 0, fx: [dmg(0.7, 'allEnemies'), stack('용기', 1)] },   // 10-13 신탁 점검: 용기 되살림
        // 원작 배치 열 덤 → 무작위 덤(D1 기본안)
        { fx: [dmg(0.6, 'allEnemies', { hits: 2 }), { k: 'roll', n: 2 }, { k: 'ifRoll', n: 1 }, status('사기', 1), { k: 'ifRoll', n: 2 }, draw(1)] },
      ],
      blesses: ['power', 'draw', [feed(1)]],
    },
    { // 회수 — 버린 더미의 검을 손으로(원작 「훈련의 성과」)
      name: '훈련의 성과', cost: 0, type: '공격',
      fx: [dmg(2.1), back],   // 10-21 밸런스 조절: 150 → 180 → 210%(3차)   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [dmg(0.9), back] },
        { type: '스킬', fx: [back, feed(2)] },   // 10-13 신탁 점검: 성장 1→2
        { fx: [dmg(0.6), back, tough(1)] },
        { type: '스킬', fx: [{ k: 'pull', from: 'draw', n: 1, tag: SWD }, draw(2)] },   // 10-13 신탁 점검: 드로우 1→2
        { tags: ['회수 3'], fx: [dmg(0.6), back] },
      ],
      blesses: ['power', 'draw', [feed(1)]],
    },
    { // 비용 깎기 — 이번 턴 검을 싸게
      name: '영웅의 길', cost: 1, type: '공격',
      fx: [dmg(2.4), { k: 'costMod', v: -1, n: 1, turns: 1, tag: SWD }, stack('용기', 1)],   // 10-21 밸런스 조절 2차: 210 → 240%   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [dmg(1.6), { k: 'costMod', v: -1, n: 1, turns: 1, tag: SWD }, stack('용기', 2)] },   // 10-13 신탁 점검: 용기 2(같은 꼴이라 피해는 본판 210%)
        { fx: [dmg(1.4), twice, dmg(1.4)] },   // 10-13 신탁 점검
        { fx: [dmg(1.2), { k: 'costMod', v: -2, n: 1, turns: 1, tag: SWD }, stack('용기', 1)] },   // 10-13 신탁 점검: 용기 되살림(아래 둘도)
        { fx: [dmg(1.2), stack('용기', 1), { k: 'ifFoe', id: 'hp', pct: 0.3 }, dmg(1.2)] },
        { fx: [dmg(1), back, stack('용기', 1)] },
      ],
      blesses: ['power', 'cost', [feed(1)]],
    },
    { // 성장 올리기 — 손(없으면 더미)의 검 성장 +1
      name: '맹호의 기세', cost: 0, type: '공격',
      fx: [dmg(2), feed(1)],   // 10-21 밸런스 조절: 130 → 200%(2 · 4 · 6차)   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [dmg(0.8), feed(1)] },
        { fx: [dmg(0.5), feed(2)] },   // 10-13 신탁 점검: 성장 2 에 피해 80%
        { fx: [dmg(0.6), stack('용기', 1), feed(1)] },
        { fx: [dmg(0.6), tough(1), feed(1)] },
        { fx: [dmg(0.6), feed(1), low(0.4), dmg(0.8)] },   // 10-13 신탁 점검: 성장 되살림
      ],
      blesses: ['power', 'draw', [feed(1)]],
    },
  ],
});
