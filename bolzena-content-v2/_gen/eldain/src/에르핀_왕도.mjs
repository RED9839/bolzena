// 에르핀(왕도) — 딜러 · 순수 · 요정. 설계: _measure/엘다인_엔진/01_사도별.md §12 (4차 10-11 — 다시 짬: X 난타 + 근면, D11)
// 엔진: 시동 「축복의 왕관」 으로 근면(처치 → AP 1 · 드로우 1, 파티 상태라 누가 처치하든)을 깔고 → 싼 카드 · 약점 공격으로 처치각을 만든 뒤
//   → X 「마력 난타」 로 남은 AP + 왕마력탄으로 모은 왕마력만큼 무작위 적에게 쏟는다(10-13 — 옛 처치마다 한 대 더 뺌) → 근면이 AP · 드로우를 되돌려 다음 카드로 잇는다.
// 왕관 겹 자원은 없앴다(X 코스트는 에르핀 소유). 자동 규칙 없음.
// 2026-10-12 밤 사용자 「아군 강화를 모두 없애고 자신만 강화하는 퓨어 딜러로」: 「축복의 왕관」 은 동료에게 씌우던 표시 → 여왕 자신이 쓰는 왕관(자신 · 최대 3 · 1개당 공격력 +10%),
//   동료 다음 카드 강화 · 동료 드로우 · 동료 게이지 · 고학년 아군 피해 +20% → 자신의 다음 카드 · 드로우 · 게이지 · 왕관으로. 근면(파티 상태 — 처치 엔진)은 그대로.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '에르핀_왕도', BR = `${H}_t1`;
const crown = (v = 1) => stack('축복의 왕관', v);   // 여왕 자신의 왕관
const gaugeMe = (v) => ({ k: 'gauge', v, target: 'self' });
const WK = ['약점 공격'];
const dil = (v = 1) => status('근면', v);
// 10-13 사용자 「마력 난타 처치하면 1회 더 없애고 리뉴얼」: X(남은 AP) + 쌓인 왕마력 수만큼 쏘고 왕마력을 모두 쓴다 — 왕마력탄으로 모아 난타로 쏟는다
const barrage = (ratio, target = 'randomEnemy', x) => dmg(ratio, target, { xHits: true, xStack: '왕마력', ...x });
const dumpMana = spend('왕마력', 'all');

export default hero({
  id: H, name: '에르핀(왕도)', nature: '순수', race: '요정', role: '딜러', star: 3,
  blurb: '요정 왕국의 여왕이 된 에르핀. 왕마력탄으로 모은 왕마력을 마력 난타로 한꺼번에 쏟아붓습니다. 부지런한 여왕은 쓰러뜨린 만큼 숨을 돌립니다.',
  keyword: { name: '축복의 왕관', desc: '여왕이 쓴 축복의 왕관', carrier: 'self', cap: 3, per: [{ stat: 'atk', v: 0.1 }] },   // 10-12 밤: 동료에게 씌우던 표시(최대 1) → 자신(최대 3)
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '왕마력', desc: '마력탄을 줄줄이 부르는 왕마력', carrier: 'self', cap: 5 }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '모두를 위한 배려', when: { on: 'fightStart' }, fx: [dil(1)] },   // 원작 2성 — 전투 시작 근면 1
    { name: '여왕의 의지', when: { on: 'kill', who: 'any' }, fx: [empower(0.2)] },   // 처치마다 자신의 다음 카드 +20%(처치 — 에르핀 소유)
  ],
  ult: { name: '에르피엔 펀치', cost: 250, fx: [dmg(0.2, 'allEnemies', { hits: 8 }), dmg(0.6, 'allEnemies'), dil(1), crown(1)] },   // 10-12 밤: 아군 피해 +20% → 왕관 1(자신 강화)   // 10-12 사용자: 비용 300 고학년은 거의 안 쓰여 모두 250 으로 · 같은 날 밤 사용자 「원작에서 전체 공격」 — 적 1명 8회 × 40% → 적 전체 8회 × 20% + 마지막 한 방 60%(원작: 8회 범위 · 마지막 타격 더 강함)
}, {
  basicNames: ['마력탄', '연속 마력탄', '왕관 방패'], deck: 'light',
  uniques: [
    { // 시동
      name: '축복의 왕관', cost: 0, type: '스킬',
      fx: [dil(1), draw(2)],
      oracles: [
        { fx: [dil(2), draw(2)] },
        { power: true, fx: [dil(1), draw(2), power([rule({ on: 'turnStart' }, [dil(1)])])] },   // 10-13 신탁 점검: 드로우 2 되살림
        { fx: [dil(1), crown(1), draw(2)] },   // 10-12 밤: 다른 아군 다음 카드 +30% → 왕관 · 10-13 신탁 점검: 드로우 1
        { fx: [status('저장', 1), draw(2)] },   // 10-13 신탁 점검: 드로우 1→2   // 딸기 케이크(AP 를 남김)
        { tags: ['개전'], fx: [dil(1), draw(2)] },   // 10-13 신탁 점검: 드로우 2 되살림
      ],
      blesses: ['draw', 'cost', [dil(1)]],
    },
    {
      name: '에르피엔 왕마력탄', cost: 1, type: '공격', tags: WK,
      fx: [dmg(4), dmg(0.3, 'randomEnemy', { hitsPer: '왕마력' }), stack('왕마력', 1)],
      oracles: [
        { tags: WK, fx: [dmg(4), dmg(0.3, 'randomEnemy', { hitsPer: '왕마력' }), stack('왕마력', 2)] },   // 10-13 신탁 점검: 왕마력을 버리던 신탁 — 왕마력 2
        { tags: [...WK, '회수 3'], fx: [dmg(3.2)] },
        { tags: WK, fx: [dmg(1.3, 'allEnemies'), stack('왕마력', 1)] },   // 10-13 신탁 점검: 왕마력 되살림
        { cost: 0, tags: WK, fx: [dmg(1.8)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」 · 10-13 신탁 점검: 같은 비용에 약하기만 해 비용 0
        { tags: WK, fx: [dmg(3.2), { k: 'ifKill' }, dil(1)] },
      ],
      blesses: ['power', 'cost', [dil(1)]],
    },
    {
      name: '반쪽 빵', cost: 0, type: '스킬',
      fx: [empower(0.6), draw(1)],   // 10-12 밤: 다른 아군 다음 카드 +100% → 자신의 다음 카드 +60%
      oracles: [
        { fx: [empower(0.8), draw(1)] },
        { fx: [gaugeMe(30), empower(0.3), draw(1)] },   // 10-12 밤: 아군 1명 게이지 → 자신
        { fx: [make(BR, 2), draw(2)] },   // 10-12 밤: 동료 몫 → 자신 몫
        { fx: [empower(0.3), draw(2)] },   // 10-13 신탁 점검
        { fx: [crown(1), empower(0.4), draw(1)] },   // 10-12 밤: 다른 아군 +30% → 왕관 · 10-13 신탁 점검: 다음 카드 +40%
      ],
      blesses: ['draw', 'ap', [dil(1)]],
    },
    { // 마무리 — X: 남은 AP + 왕마력 수만큼 무작위 적에게, 왕마력을 모두 쓴다(10-13 리뉴얼 — 옛 「처치하면 한 대 더」 뺌)
      name: '마력 난타', cost: 0, x: true, type: '공격',
      fx: [dmg(1.4, 'randomEnemy'), barrage(1.4), dumpMana],   // X = 0 · 왕마력 0 이어도 한 대
      oracles: [
        { power: true, fx: [stack('왕마력', 2), power([rule({ on: 'turnStart' }, [stack('왕마력', 1)])])] },   // 강화 — 턴 시작마다 왕마력 1(옛 「처치마다 120%」 대신)
        { fx: [dmg(1.1), barrage(1.1, 'oneEnemy'), dumpMana] },   // 적 1명에게 모두
        { fx: [dmg(1, 'allEnemies'), barrage(1), dumpMana] },   // 적 전체 한 번 + 난타
        { tags: ['보존'], fx: [dmg(1, 'randomEnemy'), barrage(1)] },   // 보존 · 왕마력을 쓰지 않는다(쌓은 채로 다음 난타)
        { fx: [status('면역', 1), barrage(1.2), dumpMana] },   // 원작 여왕의 의지
      ],
      blesses: ['power', 'draw', [stack('왕마력', 1)]],
    },
    { // 4차 8장 — 열기(원작 2성 「모두를 위한 배려」 — 여왕의 의지)
      name: '모두를 위한 배려', cost: 1, type: '스킬',
      fx: [crown(1), draw(1), empower(0.3)],   // 10-12 밤: 동료에게 왕관 · 동료 드로우 · 다른 아군 강화 → 자신
      oracles: [
        { fx: [status('면역', 1), draw(1), empower(0.4)] },   // 10-13 신탁 점검
        { fx: [dil(1), draw(1), empower(0.3)] },   // 10-13 신탁 점검
        { cost: 0, fx: [draw(1), empower(0.2)] },
        { fx: [status('면역', 1), crown(1), draw(1), empower(0.3)] },
        { fx: [gaugeMe(20), crown(1), draw(1)] },
      ],
      blesses: ['draw', 'cost', [dil(1)]],
    },
  ],
  tokens: [
    { id: BR, name: '반쪽 빵', cost: 0, type: '스킬', tags: ['소멸'], fx: [empower(0.5)], blurb: '하나 남은 빵을 반으로 나눴습니다' },
  ],
});
