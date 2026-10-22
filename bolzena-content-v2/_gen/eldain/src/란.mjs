// 란 — 딜러 · 순수 · 수인. 설계: _measure/엘다인_엔진/01_사도별.md §13 (4차 10-11 — 사냥감 확정 치명 · 규칙은 「동료 약화 → 표식」 · 가벼운 덱)
// 엔진: 「행복한 아기 늑대」(늑대 표식)를 카드로 깔아 턴 끝마다 물어뜯고(★ 키워드) → 「사랑니 발도술」 이 표식 1개당 베고 표식을 하나 더 건다(터뜨리고 다시 걸기 — 원작 보강 10-11).
// 받는 고리는 동료의 약화(우로스 I45)로 좁혔다. 사냥감(발톱)은 쓰러지면 가장 약한 적에게 옮겨 간다.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '란', W = '행복한 아기 늑대', P = '늑대 야수의 발톱';
const wolf = (v = 1, target = 'oneEnemy') => stack(W, v, { target });
const prey = (target = 'oneEnemy') => stack(P, 1, { target });
const crit = { k: 'critNext', now: true, foeStack: P };   // 사냥감이면 이 카드 치명 확정(엔진 요청 — 조건 없이 그 적만 보는 꼴)

export default hero({
  id: H, name: '란', nature: '순수', race: '수인', role: '딜러', star: 3,
  blurb: '요도 「사랑니」 를 찬 아기 늑대 검사. 「행복한 아기 늑대」 를 붙여 두면 턴 끝마다 물어뜯고, 늑대가 붙은 적은 사랑니가 깊게 벱니다. 벤 자리엔 늑대가 다시 붙습니다.',
  keyword: {
    name: W, desc: '적에게 새겨 두는 아기 늑대의 이빨 자국', carrier: 'enemy', cap: 4,
    per: [{ stat: 'dot', ratio: 0.5 }],   // 10-22 란 딜 강화(사용자 「깡딜을」): 40 → 50%   // 10-11 밸런스 40→50% · 10-19 밸런스(센 쪽) 50 → 40%
    rules: [{ name: '함께 속죄하는 벗', when: { on: 'debuff', who: 'other', id: '약화' }, fx: [wolf(2)] }],
  },
  keywords: [{
    // 4차 덧붙임(사용자 10-11): 3겹까지 쌓이는 사냥감 — 1겹당 란에게 받는 피해 +15% · 쓰러지면 가장 약한 적에게 겹째로(늑대 표식은 파티가 거는 지속 피해, 발톱은 란만의 사냥감 강화)
    name: P, desc: '란이 노리는 사냥감', carrier: 'enemy', cap: 3, hunt: true, huntCarry: 'lowEnemy',
    per: [{ stat: 'taken', v: 0.2, from: 'owner' }],   // 10-22 란 딜 강화(사용자 「표식 메커니즘」): 15 → 20%   // 10-11 밸런스 15→20% · 10-19 밸런스(사용자 — 센 쪽) 20 → 15%
  }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '란과 벗님들', when: { on: 'huntDown' }, fx: [draw(1)] },   // 사냥감이 쓰러지면 드로우 1
    { name: '란과 벗님들', when: { on: 'stackReach', id: P, n: 3 }, fx: [draw(1)] },   // 10-19 사용자 「보스전에서 확실히 뜨게」: 사냥감의 발톱이 3겹(최대)이 되면 드로우 1
  ],   // 「란과 벗님들」(처치하면 가장 약한 적 약점 노출)은 자동 규칙 하나(E62)라 발도술 ③ 신탁으로 옮겼다
  ult: { name: '몰이 사냥', cost: 250, fx: [dmg(2.2, 'oneEnemy', { hits: 2 }), { k: 'ifKill', id: 'orBoss' }, dmg(4.4, 'oneEnemy')] },   // 10-19: 처치 → 처치하거나 대상이 보스면(보스전에서도 돈다)   // 10-19 타수(사용자 「고학년 최대 3타」): 4회 × 110% + 처치하면 4회 × 110% → 2회 × 220% + 처치하면 440% 한 방(합 그대로)
}, {
  basicNames: ['발도', '쾌속 발도', '칼집 막기'], deck: 'light',
  uniques: [
    { // 시동
      name: '아랑의 여유', cost: 1, type: '공격',
      fx: [dmg(1.6, 'allEnemies'), wolf(2, 'allEnemies'), prey()],   // 10-22 란 딜 강화: 적 전체 늑대 1 → 2(일반전 초반 속도)   // 10-21 밸런스 조절: 120 → 140 → 160%(2차)   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [dmg(1, 'allEnemies'), wolf(2, 'allEnemies'), prey()] },
        { type: '스킬', cost: 0, fx: [wolf(2, 'allEnemies'), prey()] },   // 10-22: 늑대 3 은 봇이 본판 대신 골라 딜이 줄어(프론티어 −12%) 2 그대로   // 10-13 신탁 점검 3차: 늑대 2   // 10-13 신탁 점검: 발톱 되살림
        { fx: [dmg(1, 'allEnemies'), wolf(2, 'allEnemies'), status('약화', 1, { target: 'allEnemies' })] },   // 10-22: 늑대 1 → 2(본판 따라)
        { fx: [dmg(2.6), wolf(3), prey()] },   // 10-22: 늑대 2 → 3(본판 따라)   // 10-13 신탁 점검: 260% · 발톱 되살림
        { fx: [dmg(1.2, 'allEnemies'), wolf(2, 'allEnemies'), wolf(2)] },   // 10-22: 적 전체 늑대 1 → 2(본판 따라)   // 10-13 신탁 점검: 100→120%
      ],
      blesses: ['power', 'draw', [wolf(1)]],
    },
    {
      name: '투구꽃 독', cost: 0, type: '스킬',
      fx: [wolf(3), status('고통', 2, { target: 'oneEnemy' })],   // 10-21 밸런스 조절: 늑대 2 → 3
      oracles: [
        { fx: [wolf(4), status('고통', 2, { target: 'oneEnemy' })] },   // 10-21: 늑대 3 → 4
        { fx: [wolf(2, 'allEnemies'), status('고통', 2, { target: 'allEnemies' })] },   // 10-21: 늑대 1 → 2   // 10-13 신탁 점검: 고통 1→2
        { fx: [wolf(3), status('약화', 2, { target: 'oneEnemy' })] },   // 10-21: 늑대 2 → 3(본판과 같이)
        { cost: 1, fx: [wolf(3), status('고통', 2, { target: 'oneEnemy' }), draw(2)] },   // 10-21: 늑대 2 → 3   // 10-13 신탁 점검: 고통 되살림
        { fx: [wolf(2), status('고통', 2, { target: 'oneEnemy' }), draw(1, { who: 'other' })] },   // 10-21: 늑대 1 → 2   // 10-13 신탁 점검: 늑대 1
      ],
      blesses: ['draw', 'ap', [wolf(1)]],
    },
    {
      name: '날선 늑대의 발톱', cost: 1, type: '공격',
      fx: [crit, dmg(2.8)],   // 10-21 밸런스 조절 2차: 250 → 280%   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [crit, dmg(2.4)] },
        { fx: [crit, dmg(2.1), { k: 'perStack', id: W }, dmg(0.4)] },   // 10-13 신탁 점검
        { fx: [{ k: 'perStack', id: W, each: true, max: 1 }, dmg(1.8, 'allEnemies')] },   // 10-13 신탁 점검 3차   // 10-13 신탁 점검: 110→150%
        { cost: 1, fx: [{ k: 'ifStack', id: P, n: 1 }, dmg(2.2), { k: 'ifKill', id: 'orBoss' }, { k: 'ap', v: 1 }] },   // 10-19: 처치 → 처치하거나 보스면 AP 1(보스전엔 늘 — 피해 280 → 220%)   // 10-13 신탁 점검 3차   // 10-13 신탁 점검: 170→240%   // 10-12 사용자 「신탁 소멸 줄여 줘」
        { fx: [crit, dmg(1.8), wolf(1)] },
      ],
      blesses: ['power', 'cost', [wolf(1)]],
    },
    { // 마무리 — 표식 1개당(최소 1회) 베고 표식 +1(남는다)
      name: '사랑니 발도술', cost: 1, type: '공격',
      fx: [{ k: 'perStack', id: W, n: 1 }, dmg(2), wolf(1)],   // 10-21 밸런스 조절 2차: 늑대 1당 180 → 200%   // 10-19 밸런스(센 쪽): 늑대 1당 210 → 180% · 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { power: true, fx: [wolf(1, 'allEnemies'), power([rule({ on: 'stackReach', id: W, n: 4 }, [status('약점 노출', 2, { target: 'oneEnemy' })])])] },   // 10-13 신탁 점검: 적 전체 늑대 · 약점 노출 2
        { fx: [{ k: 'spend', id: W, v: 3 }, dmg(2.7, 'oneEnemy', { hits: 2 })] },   // 10-19 밸런스: 2회 × 320 → 270%   // 10-19 타수: 3회 × 210% → 2회 × 320%(신탁 맞춤 ×1.101 · 10% 올림 뒤 3회 × 240% → 2회 × 360% — 합 720% 그대로)
        { fx: [{ k: 'perStack', id: W, n: 1 }, dmg(1.5), { k: 'ifKill', id: 'orBoss' }, status('약점 노출', 1, { target: 'lowEnemy' })] },   // 10-19: 200 → 150% · 처치하거나 보스면   // 10-13 신탁 점검   // 「란과 벗님들」
        { fx: [{ k: 'perStack', id: W, n: 1 }, dmg(1.5), status('약점 노출', 1, { target: 'oneEnemy' })] },   // 10-19: 180 → 150%
        { fx: [{ k: 'perStack', id: W, n: 1 }, dmg(1.8), wolf(2)] },   // 10-19: 210 → 180%   // 10-13 신탁 점검: 기본보다 약하기만 하던 신탁 — 늑대 2   // 10-12 사용자 「신탁 소멸 줄여 줘」
      ],
      blesses: ['power', 'cost', [wolf(1)]],
    },
    { // 4차 8장 — 굴리기(어사이드 「란과 벗님들」): 표식을 걸고 동료 다음 카드를 키운다
      name: '란과 벗님들', cost: 0, type: '스킬',
      fx: [wolf(1), prey(), empower(0.4, { who: 'other' })],   // 10-21 밸런스 조절 8차: 동료 다음 카드 +30 → +40%
      oracles: [
        { fx: [wolf(1), prey(), empower(0.6, { who: 'other' })] },   // 10-21: +50 → +60%   // 10-13 신탁 점검: 발톱 되살림(아래 넷도)
        { fx: [wolf(1, 'allEnemies'), prey()] },
        { fx: [wolf(1), stack(P, 2, { target: 'oneEnemy' })] },   // 10-12 카드 글: 같은 조각 두 번 → 2 하나로
        { fx: [wolf(1), prey(), draw(1, { who: 'other' })] },
        { fx: [status('약화', 1, { target: 'oneEnemy' }), wolf(1), prey()] },
      ],
      blesses: ['draw', 'cost', [wolf(1)]],
    },
  ],
});
