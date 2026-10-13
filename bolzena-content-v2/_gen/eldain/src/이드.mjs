// 이드 — 탱커 · 냉정 · 엘프. 설계: _measure/엘다인_엔진/01_사도별.md §2 (4차 10-11)
// 엔진: 비용 2 이상 카드마다 꿈의 막(막 1개당 방어 +10%) → 3겹 가득이면 패시브 「꿈에서 깨지 않았다」 가 모두 터뜨린다(10-13). 손에 쥔 「흐릿한 경계」(천상 · 보존)는 비용 2 카드에 실드를 펼치고, 「나타로 들이받기」 가 파티 실드 절반을 헐어 친다.
// 2026-10-12 밤 사용자: 생성 카드 「나타 레이저」(꿈의 막 3 → 0코 소멸 공격)를 없애고 시동 「흐릿한 경계」 에 합침 — 막이 차면 다음 무거운 카드 때 경계가 펼쳐지며 레이저가 터진다.
// 4차 결정 지점: 들이받기는 보존 — 쥐고 넘기면 실드 유지 1(지금 헐까 · 다음 턴으로 넘길까). 즉사 무효는 회피(D15) · 끈기는 경계 ⑤ 갈래. 착한 마음씨는 2코(스스로 천상을 깨움).
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '이드', K = '이드 더 이터널 불릿';
const S = (v = 1, x) => stack(K, v, x);
const ANY = (r) => empower(r, { who: 'any' });
const OT = (r, x) => empower(r, { who: 'other', ...x });   // 다른 아군의 다음 카드
const SKY = ['천상', '보존'];
// 10-13 사용자: 옛 나타 레이저(laser) · 회피 1(dodge) 뺌 — 꿈의 막은 패시브 「꿈에서 깨지 않았다」 가 가득 차면 터뜨린다
const hold = [{ k: 'when', on: 'handEnd' }, status('실드 보존', 1)];   // 쥐고 넘기면: 실드 보존 1(남은 실드를 다음 턴으로 — 4차 측정 뒤 실드 유지에서 바꿈)

export default hero({
  id: H, name: '이드', nature: '냉정', race: '엘프', role: '탱커', star: 3,
  blurb: '모든 게 꿈이라 믿고 늘 눈을 감은 영원살이 막내 작은 언니. 무거운 카드가 나갈 때마다 손에 쥔 경계가 저절로 펼쳐지고, 쌓인 실드를 나타와 함께 들이받아 피해로 바꿉니다.',
  keyword: {
    name: K, desc: '꿈과 현실 사이에 겹겹이 쌓이는 꿈의 막', carrier: 'self', cap: 3,
    per: [{ stat: 'def', v: 0.1 }],   // 10-12 밤: 옛 onMax(3 이 되면 「나타 레이저」 생성 · 모두 소모) 뺌 — 레이저는 「흐릿한 경계」 가 쏜다
    // 10-12 밤 개편(사용자 「쌓는 길을 하나로」): 옛 전투 시작 +1 · 경계 · 착한 마음씨 · 축복의 +1 → 이드가 비용 2 이상 카드를 낼 때마다 +1(무거운 카드 → 경계 → 레이저)
    // 10-13 칼리페식(사용자): 옛 「꿈이 깊어짐」(비용 2 카드마다 +1) 뺌 — 은빛 장막처럼 카드(흐릿한 경계 · 신탁)로만 쌓는다
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '나타', desc: '이드 곁에서 실드를 받쳐 주는 나타', carrier: 'self', cap: 3, per: [{ stat: 'guard', v: 0.1 }] }],   // 10-12 탱커 손질(실드 정체성) 5→10%
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    // 10-12 밤 개편: 옛 「꿈에서 깨지 않았다」(전투 시작 +1)는 쌓는 길 하나로 합쳐 뺐고, 옛 「꿈의 막」(이름이 고유 효과 뜻과 겹침)은 원작 2성 이름으로
    // 10-13 칼리페식(사용자 「칼리페처럼」): 꿈의 막이 3겹 차면 모두 써서 적 전체 방어 기반 400%(칼리페 은빛 장막)
    { name: '꿈에서 깨지 않았다', when: { on: 'stackReach', id: K, n: 3 }, fx: [spend(K, 'all'), ddmg(4, 'allEnemies')] },
    { name: '영원살이 막내', when: { on: 'fightStart' }, fx: [S(1)] },   // 칼리페 에고 발현 6(전투 시작 장막 2)을 1로
  ],
  ult: { name: '너와 나의 우주', cost: 250, fx: [ddmg(3, 'allEnemies'), make('이드_u1', 1), S(1)] },   // 10-13 칼리페식: 「결의의 카리스마」 — 적 전체 방어 기반 300% · 흐릿한 경계 1장 · 꿈의 막 1(에고 발현 3)
}, {
  basicNames: ['몽중탄', '몽중 연사', '나타 격벽'], deck: 'heavy',
  uniques: [
    // 2026-10-13 칼리페식 교체(사용자 「칼리페처럼 변경」): 카드 5장 = 카제나 칼리페 시동 · 고유 2~4 · 완성형, 신탁 5갈래 = 칼리페 번뜩임(수치 카제나 그대로).
    //   엔진에 없는 것 — 「손패 수만큼 실드」 · 「비용 1 증가」 · 「행동 카운트」 · 「손 전체 보존」 은 줄마다 가까운 꼴로. 이전 판: C:\projects\_backup\id_before_20261013
    { // 시동 ← 벌쳐 사출 — 천상 · 보존: 비용 2 이상 카드가 나가면 손에서 저절로
      name: '흐릿한 경계', cost: 3, type: '공격', tags: SKY,
      fx: [ddmg(1.2, 'allEnemies'), shield(1), S(1)],
      oracles: [
        { tags: SKY, fx: [ddmg(2.8, 'allEnemies'), S(1)] },                                  // I
        { type: '스킬', tags: SKY, fx: [shield(2.8), S(1)] },                                // II(원작 추천)
        { tags: SKY, fx: [ddmg(1.8, 'allEnemies'), shield(1.5), S(1)] },                     // III
        { tags: SKY, fx: [ddmg(1.8, 'allEnemies'), S(1), ...hold] },                          // IV 쥐고 넘기면 실드 보존(원작 「보존: 실드 100%」)
        { tags: SKY, fx: [ddmg(1.8, 'allEnemies'), S(1), stack('나타', 1)] },                // V(원작 「손패 수만큼 실드 +40%」 대신 나타)
      ],
      blesses: ['guard', 'draw', [status('실드 보존', 1)]],
    },
    { // 고유 4 ← 재집결 — 실드 + 무거운 카드 드로우
      name: '함께 꾸는 꿈', cost: 1, type: '스킬',
      fx: [shield(1.2), draw(1, { minCost: 2 })],
      oracles: [
        { fx: [shield(1.5), draw(1, { minCost: 2 }), S(1)] },                                // I
        { fx: [shield(1.5), draw(1, { tag: '천상' }), S(1)] },                               // II
        { cost: 2, tags: ['천상'], fx: [shield(1.5), draw(1, { minCost: 2 }), S(1)] },       // III(원작 BEST)
        { fx: [shield(1.2), draw(1), S(1)] },                                                 // IV(원작 「그 카드 비용만큼 실드」 는 빼고)
        { fx: [shield(1.5), draw(1, { minCost: 2 }), stack('나타', 1)] },                    // V
      ],
      blesses: ['guard', 'cost', [status('실드 보존', 1)]],
    },
    { // 고유 3 ← 위압 — 큰 실드 + 적 전체 강인도
      name: '착한 마음씨 이드', cost: 2, type: '스킬',
      fx: [shield(2.5), { k: 'tough', v: 1, target: 'allEnemies' }],
      oracles: [
        { fx: [shield(3), { k: 'tough', v: 1, target: 'allEnemies' }, status('피해 감소', 1)] },   // I
        { fx: [shield(3), { k: 'tough', v: 1, target: 'allEnemies' }, status('약화', 2, { target: 'allEnemies' })] },   // II
        { fx: [shield(3), { k: 'tough', v: 2, target: 'allEnemies' }] },                     // III
        { fx: [shield(3.5), { k: 'tough', v: 1, target: 'allEnemies' }] },                   // IV(원작 BEST 「손패 수만큼 실드 +40%」 — 실드 값으로)
        { fx: [shield(3), { k: 'tough', v: 1, target: 'allEnemies' }, status('취약', 1, { target: 'allEnemies' })] },   // V(원작 「행동 카운트」 대신 취약)
      ],
      blesses: ['guard', 'draw', [status('실드 보존', 1)]],
    },
    { // 완성형 ← 절대 수호 — 종극: 남은 AP 1개당 실드(= X)
      name: '나타로 들이받기', cost: 0, type: '스킬', tags: ['종극'],
      fx: [{ k: 'perApLeft' }, shield(1)],
      oracles: [
        { tags: ['종극', '보존'], fx: [{ k: 'perApLeft' }, shield(1.2)] },                 // 에고 발현 1 — 보존 · 실드 +20%
        { tags: ['종극'], fx: [{ k: 'perApLeft' }, shield(1), S(1)] },
        { tags: ['종극'], fx: [{ k: 'perApLeft' }, shield(0.8), status('실드 보존', 1)] },  // 원작 「손 전체 보존」 대신 실드 보존
        { type: '공격', tags: ['종극'], fx: [{ k: 'perApLeft' }, ddmg(1, 'allEnemies')] },
        { tags: ['종극'], fx: [{ k: 'perApLeft' }, shield(1), stack('나타', 1)] },
      ],
      blesses: ['cost', 'draw', [status('실드 보존', 1)]],
    },
    { // 고유 2 ← 대검 아퀼라 — 적 전체, 손에 경계가 있으면 더 세게
      name: '꿈속의 나타', cost: 1, type: '공격',
      fx: [ddmg(1.5, 'allEnemies'), { k: 'ifInHand', id: '이드_u1' }, ddmg(1, 'allEnemies')],   // 원작 「비용 1 증가」 는 엔진에 없어 뺌
      oracles: [
        { fx: [ddmg(2.1, 'allEnemies'), { k: 'ifInHand', id: '이드_u1' }, ddmg(1.2, 'allEnemies')] },   // I
        { fx: [ddmg(2.2, 'allEnemies')] },                                           // II(원작 2코 · 「쓸 때마다 +40%」 — 1코 수치로)
        { fx: [ddmg(1.5, 'allEnemies'), { k: 'ifInHand', id: '이드_u1' }, S(1)] },   // III 손에 경계가 있으면 꿈의 막 1(원작 2코 상황 — 1코로)
        { tags: ['보존'], fx: [ddmg(1.5, 'allEnemies'), { k: 'when', on: 'handEnd' }, stack('나타', 1)] },   // IV
        { tags: ['회수 3'], fx: [ddmg(1.5, 'allEnemies'), { k: 'ifInHand', id: '이드_u1' }, ddmg(1, 'allEnemies')] },   // V
      ],
      blesses: ['power', 'cost', [status('실드 보존', 1)]],
    },
  ],
});
