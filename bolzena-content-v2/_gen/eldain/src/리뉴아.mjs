// 리뉴아 — 딜러 · 광기 · 엘프. 설계: _measure/엘다인_엔진/01_사도별.md §9 (4차 덧붙임 10-11 — 사용자: 「시간」 컨셉, 쥐면 싸짐)
// 엔진: 리뉴아 고유 카드는 4 ~ 6코 · 보존이다. 아군(리뉴아 자신 포함)이 카드를 낼 때마다 손의 리뉴아 고유 카드 「시간」 +1 — 1당 비용 -1(최대 6, 낸 카드는 0 으로).
//   비용이 0 이 되어 내면 「정각」 덤(▶ ifCost). 쥐고 기다려 싸게 낼지 · 비싸게 지금 낼지 · 어느 카드를 정각까지 키울지가 결정 지점.
//   리뉴아 자신의 카드는 세지 않는다(who other) — 세면 리뉴아 카드끼리 서로 깎아 공짜 카드가 넘쳤다(혼자 56%). 「동료가 움직이는 동안 쥐고 기다림」 이 손맛(비비(신성)은 쥐면 값이 자람, 이드는 남의 2코로 천상).
// 4번째 카드 박자(ifNth after)는 시계 ④ 한 갈래로 내렸다. 고학년 「타임 브레이크」(이번 턴 남은 카드 신속)는 그대로. 자동 규칙 없음.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '리뉴아', K = '시간';
const JK = { k: 'ifCost', max: 0, id: '정각' };   // 정각이면:(비용이 0 이 되어 냈으면)
const SW = ['신속', '보존'], KEEP = ['보존'];
const slow = (target = 'allEnemies') => status('둔화', 1, { target });
const swift = { k: 'swiftTurn' };   // 이번 턴 남은 카드 신속
const tick = (v = 1) => ({ k: 'cardStatus', id: K, v, to: 'hand', who: 'self', unique: true });   // 손의 리뉴아 고유 카드 모두 「시간」 +v
const ally = { k: 'costMod', v: -1, n: 1, who: 'other', turns: 1 };   // 다른 아군 카드 비용 -1

export default hero({
  id: H, name: '리뉴아', nature: '광기', race: '엘프', role: '딜러', star: 3,
  blurb: '오천 년을 건너온 인조 엘다인. 손에 쥔 시공 포격은 동료가 움직일 때마다 시간이 흘러 싸지고, 비용이 0 이 되는 정각에 가장 셉니다.',
  keyword: {
    name: K, desc: '리뉴아 카드마다 따로 흐르는 시간', carrier: 'self', cap: 6,
    cardValue: { grow: 'allyPlay', reset: 'play', cost: -1, max: 6, who: 'other' },
  },
  // 자동 규칙 하나(사용자 10-11 — 원작 어사이드 「우리들의 평화로운 시간」): 리뉴아 카드를 비용 0(정각)으로 내면 이번 턴 아군 주는 피해 +10%
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '메아리', desc: '정각에 울린 포격의 메아리', carrier: 'self', cap: 3, endClear: true }],
  passives: [
    { name: '우리들의 평화로운 시간', when: { on: 'play', paidMax: 0 }, fx: [{ k: 'dealtMod', v: 0.3, target: 'allAllies' }] },   // 10-21 밸런스 조절: +10 → +30%(1 · 6차)
    { name: '시공간, 저 너머로!', when: { on: 'ult', who: 'any' }, fx: [tick(1)] },   // 원작 2성 — 아군이 고학년을 쓰면 손의 리뉴아 카드 시간 +1
  ],
  ult: { name: '타임 브레이크', cost: 250, fx: [dmg(1, 'allEnemies', { hits: 2 }),   /* 10-19 타수: 4회 × 50% → 2회 × 100% */   /* 10-13 사용자: 고학년 비교에서 가장 약함(1.14) — 4회 × 30% → 50% */ tick(4), swift] },   // 사용자 10-11: 손의 리뉴아 카드 시간 +4(정각 근처로) · 이번 턴 남은 카드 신속 — 그 턴에 4~6코를 몰아 냄
}, {
  basicNames: ['시공 사격', '호버보드 돌진', '호버보드 회피'], deck: 'light',
  uniques: [
    { // 시동 — 4코 · 정각이면 크게
      name: '멈춰 버린 시계', cost: 3,   /* 10-21 밸런스 조절: 4 → 3 */ type: '공격', tags: SW,
      fx: [dmg(1.2, 'allEnemies'), JK, dmg(1.2, 'allEnemies'), stack('메아리', 1)],   // 10-21 밸런스 조절: 80 + 80 → 120 + 120%(2 · 5차)
      oracles: [
        { tags: SW, fx: [dmg(1, 'allEnemies'), JK, dmg(1.4, 'allEnemies')] },
        { tags: SW, fx: [dmg(1, 'allEnemies'), tick(1)] },                                   // 비용 감소 더 빠르게
        { tags: SW, fx: [dmg(1, 'allEnemies'), JK, ally] },                                  // 동료 카드 비용도 -1
        { tags: SW, fx: [dmg(1, 'allEnemies'), { k: 'ifNth', n: 4, after: true }, dmg(1, 'allEnemies')] },   // 4번째 카드 박자(4차 정각의 옛 꼴)
        { cost: 2, tags: SW, fx: [dmg(0.8, 'allEnemies'), JK, { k: 'recast', ratio: 1 }] },   // 10-21: 3 → 2코(본판이 3코로 내려서)  // 0코면 두 번 발동
      ],
      blesses: ['cost', 'draw', [tick(1)]],
    },
    {
      name: '시간 저 너머로', cost: 4,   /* 10-21: 5 → 4 */ type: '공격', tags: KEEP,
      fx: [dmg(1.4, 'randomEnemy', { hits: 2 }), JK, draw(2)],   /* 10-21 8차: 2 × 120 → 140% */   // 10-21 밸런스 조절: 2 × 80 → 120%(2 · 5차)   // 10-19 타수(사용자 「카드 최대 2타」): 무작위 3회 → 2회, 한 대 ×1.5(신탁도 · 10% 단위로 올림 — 본판 75 → 80%)
      oracles: [
        { tags: KEEP, fx: [dmg(1.8, 'randomEnemy', { hits: 2 }), JK, draw(2)] },   // 10-21: 120 → 180%(본판이 오른 만큼)
        { tags: KEEP, fx: [dmg(1.2, 'randomEnemy', { hits: 2 }), tick(1)] },   // 10-13 신탁 점검
        { tags: KEEP, fx: [dmg(1.05, 'randomEnemy', { hits: 2 }), JK, ally] },   // 10-13 신탁 점검
        { tags: KEEP, fx: [dmg(0.6, 'randomEnemy', { hits: 2 }), draw(2), JK, { k: 'gauge', v: 30 }] },   // 원작 쿨 감소 · 10-13 신탁 점검: 피해 되살림 · 드로우는 늘 · 게이지 20→30
        { tags: KEEP, fx: [dmg(1.05, 'randomEnemy', { hits: 2 }), JK, { k: 'recast', ratio: 1 }] },   // 10-13 신탁 점검
      ],
      blesses: ['cost', 'draw', [tick(1)]],
    },
    { // 원작 저학년 — 버프 해제 · 정각이면 실드 파괴
      name: '시공의 메아리', cost: 5,   /* 10-21: 6 → 5 */ type: '공격', tags: KEEP,
      fx: [dmg(1.3, 'oneEnemy', { hits: 2 }), { k: 'dispel', n: 1 }, JK, { k: 'strip', target: 'oneEnemy' }],   // 10-21 밸런스 조절: 2 × 80 → 130%(3 · 6차)   // 10-19 타수: 5회 → 2회, 한 대 ×2.5(신탁도 · 10% 단위로 올림 — 본판 75 → 80% · 신탁 1 · 4 125 → 130%)
      oracles: [
        { tags: KEEP, fx: [dmg(1.3, 'oneEnemy', { hits: 2 }), { k: 'dispel', n: 1 }, JK, { k: 'strip', target: 'oneEnemy' }] },
        { tags: KEEP, fx: [dmg(1, 'oneEnemy', { hits: 2 }), tick(1)] },
        { tags: KEEP, fx: [dmg(0.5, 'allEnemies', { hits: 2 }), JK, { k: 'dispel', n: 1, target: 'allEnemies' }] },
        { tags: KEEP, fx: [dmg(1.3, 'oneEnemy', { hits: 2 }), JK, slow('oneEnemy')] },   // 10-13 신탁 점검
        { tags: KEEP, fx: [dmg(1, 'oneEnemy', { hits: 2 }), JK, { k: 'recast', ratio: 1 }] },
      ],
      blesses: ['power', 'cost', [tick(1)]],
    },
    { // 마무리 — 6코 · 정각이면 이번 턴 남은 카드 신속 · 적 전체 둔화
      name: '정지된 6초', cost: 5,   /* 10-21: 6 → 5 */ type: '공격', tags: KEEP,
      fx: [dmg(0.5, 'allEnemies', { hitsPer: '메아리', hitsAdd: 6 }), JK, swift, slow()],   // 10-21 밸런스 조절: 한 대 30 → 50%(4 · 7차)   // 10-19 타수: 엔진 R.PER_HIT_MERGE 가 (메아리+6)회를 한 대 × (메아리+6) 로 합친다(합 그대로 · 1타)
      oracles: [
        { power: true, fx: [slow(), power([rule({ on: 'play', who: 'any', nth: 4 }, [tick(2)])])] },   // 10-13 신탁 점검: 적 전체 둔화 · 시간 2
        { tags: KEEP, fx: [dmg(2.1, 'oneEnemy', { hits: 2 }), JK, swift] },   // 10-13 신탁 점검 · 10-19 타수: 6회 → 2회, 한 대 ×3(아래 셋도)
        { tags: KEEP, fx: [dmg(1.2, 'allEnemies', { hits: 2 }), tick(2)] },
        { tags: KEEP, fx: [dmg(1.2, 'allEnemies', { hits: 2 }), JK, { k: 'nextAp', v: 1 }] },
        { tags: KEEP, fx: [dmg(1.2, 'allEnemies', { hits: 2 }), JK, ally] },
      ],
      blesses: ['power', 'cost', [tick(1)]],
    },
    { // 4차 8장 — 열기(박자 잇기): 신속 · 정각이면 손을 채운다
      name: '호버보드 질주', cost: 4,   /* 10-21: 5 → 4 */ type: '공격', tags: SW,
      fx: [dmg(1.2), JK, draw(2), stack('메아리', 1)],   // 10-21 밸런스 조절: 60 → 120%(2 · 5차)
      oracles: [
        { tags: SW, fx: [dmg(1.1), JK, draw(2)] },
        { tags: SW, fx: [dmg(1), tick(1), stack('메아리', 1)] },   // 10-13 신탁 점검: 메아리 되살림(아래 넷도)
        { tags: SW, fx: [dmg(1), stack('메아리', 1), JK, ally] },
        { tags: SW, fx: [dmg(1), stack('메아리', 1), JK, slow('oneEnemy')] },
        { cost: 3, tags: SW, fx: [dmg(0.7), stack('메아리', 1), JK, { k: 'recast', ratio: 1 }] },   // 10-13 신탁 점검 3차
      ],
      blesses: ['draw', 'power', [tick(1)]],
    },
  ],
});
