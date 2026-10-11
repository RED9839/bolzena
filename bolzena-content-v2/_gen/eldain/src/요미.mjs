// 요미 — 서포터 · 우울 · 미스틱. 설계: _measure/엘다인_엔진/01_사도별.md §20 (4차 10-11 — 요미 두 갈래, 사용자 지시: 역할 서포터)
// 엔진: 「달바라기 꽃」(달 위상)은 요미 카드로만 움직이고(전투 시작 반달) → 요미 고유 카드를 낼 때마다 지금 위상의 덤(▶E75)
//   초승(준비): 드로우 1 · 반달(서포터 — 달의 영역): 적 1명 약화 1 · 실드 60% · 보름(메인딜): 그 카드 피해 +50% · 꽃밭 +1 · 그믐(서포터 — 달빛 마중): 적 1명 둔화 1 · 피해 감소 1
//   → 기도로 위상을 고르고 고정(▶E74) — 어느 위상에 머물지가 결정 지점(서포터 빌드 = 반달 · 그믐 고정, 메인딜 빌드 = 보름 고정, 혼합 = 순환).
// 꽃밭(「소망을 머금은 꽃」)은 턴 끝에 꽃 1개당 적 전체 30% ★(키워드 효과). 둔화 소유 — 별빛이 둔화된 적에게 1회 더. 고유 능력 규칙 없음.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '요미', M = '달바라기 꽃', F = '소망을 머금은 꽃';
const moon = (n) => ({ k: 'ifStack', id: M, n, max: n });
const half = { k: 'ifStack', id: M, n: 2, max: 2 }, dark = { k: 'ifStack', id: M, n: 4, max: 4 };
const step = (v = 1) => stack(M, v);
const pick = { k: 'stack', id: M, pick: true };   // ▶E74 위상을 고름
const lock = { k: 'stackLock', id: M, turns: 1 };   // ▶E74 다음 턴까지 고정
const fl = (v = 1, x) => stack(F, v, x);
const KEEP = ['보존'];
const slowed = { k: 'ifFoe', id: '둔화' };   // 둔화된 적이면(요미 소유) 1회 더

export default hero({
  id: H, name: '요미', nature: '우울', race: '미스틱', role: '서포터', star: 3,
  blurb: '달에 비는 별바라기 사제. 반달 · 그믐엔 달빛으로 동료를 지키고 적을 늦추며, 보름엔 스스로 별빛과 운석을 떨어뜨립니다. 어느 달에 머물지는 요미가 고릅니다.',
  keyword: {
    name: M, desc: '초승 → 반달 → 보름 → 그믐으로 도는 달', carrier: 'self', cap: 4, wrap: true, stages: ['초승', '반달', '보름', '그믐'],
    start: 2,
    stageBonus: [
      { unique: true, fx: [draw(1)] },
      { unique: true, fx: [status('약화', 1, { target: 'oneEnemy' }), shield(0.6)] },
      { unique: true, ratio: 1.5, fx: [fl(1)] },
      { unique: true, fx: [status('둔화', 1, { target: 'oneEnemy' }), status('피해 감소', 1)] },
    ],
  },
  keywords: [{ name: F, desc: '달빛 아래 피어난 꽃밭 — 턴이 끝날 때마다 꽃잎이 적을 찌른다', carrier: 'self', cap: 3, per: [{ stat: 'dot', ratio: 0.3 }] }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '소망을 머금은 꽃', when: { on: 'fightStart' }, fx: [fl(2)] },   // 원작 2성 — 전투 시작 꽃밭 2
    { name: '지극정성', when: { on: 'turnStart' }, conds: [{ c: 'stack', id: M, n: 2 }], fx: [heal(0.3)] },   // 반달 · 보름 · 그믐이면 매 턴 회복(초승 빼고)
  ],
  ult: { name: '지극정성의 마중', cost: 250, fx: [dmg(0.5, 'allEnemies', { hits: 4 }), pick, status('둔화', 1, { target: 'allEnemies' }), { k: 'nextAp', v: 1 }] },
}, {
  basicNames: ['달빛 탄', '석장 휘두르기', '사제의 베일'], deck: 'heavy',
  uniques: [
    { // 시동 — 반달에서 시작해 한 칸씩
      name: '달바라기', cost: 1, type: '공격',
      fx: [dmg(0.9, 'allEnemies'), step(1)],
      oracles: [
        { fx: [dmg(1.5, 'allEnemies'), step(1)] },                       // 메인딜
        { type: '스킬', cost: 0, fx: [fl(1), step(1)] },                                // 메인딜(꽃밭)
        { fx: [dmg(0.9, 'allEnemies'), step(2)] },                        // 순환
        { type: '스킬', fx: [stack(M, 1, { set: 2 }), shield(0.9)] },     // 서포터(반달로)
        { type: '스킬', fx: [stack(M, 1, { set: 4 }), heal(1.2)] },       // 서포터(그믐으로)
      ],
      blesses: ['power', 'draw', [fl(1)]],
    },
    {
      name: '달님께 비는 기도', cost: 0, type: '스킬',
      fx: [pick, draw(1)],
      oracles: [
        { fx: [pick, draw(2)] },
        { fx: [pick, lock] },
        { tags: ['보존'], fx: [pick, draw(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」
        { fx: [pick, empower(0.3, { who: 'other' })] },                  // 서포터
        { fx: [pick, draw(1, { who: 'other' })] },                        // 서포터
      ],
      blesses: ['draw', 'ap', [fl(1)]],
    },
    {
      name: '별빛 두 줄기', cost: 1, type: '공격', tags: KEEP,
      fx: [dmg(1, 'randomEnemy', { hits: 2 }), slowed, dmg(0.6)],
      oracles: [
        { tags: KEEP, fx: [dmg(1.4, 'randomEnemy', { hits: 2 }), slowed, dmg(1.4)] },   // 메인딜
        { tags: KEEP, fx: [dmg(1, 'randomEnemy', { hits: 2, hitFx: [status('약화', 1)] })] },   // 서포터
        { tags: KEEP, cost: 0, fx: [dmg(0.6, 'randomEnemy', { hits: 2 }), slowed, dmg(0.6)] },
        { tags: KEEP, fx: [dmg(1, 'randomEnemy', { hits: 2 }), step(1)] },
        { tags: KEEP, fx: [dmg(1, 'randomEnemy', { hits: 2 }), dark, status('둔화', 1, { target: 'allEnemies' })] },   // 서포터
      ],
      blesses: ['power', 'cost', [fl(1)]],
    },
    { // 마무리 — 보름 메인딜 / 반달 · 그믐 서포터 갈래
      name: '달님께 비는 운석', cost: 2, type: '공격', tags: KEEP,
      fx: [dmg(2.2, 'allEnemies')],
      oracles: [
        { power: true, fx: [fl(1), power([rule({ on: 'play' }, [moon(3), dmg(0.8, 'randomEnemy')])])] },   // 메인딜
        { tags: KEEP, fx: [{ k: 'later', n: 2, then: [dmg(4.2, 'allEnemies')] }] },                         // 메인딜
        { tags: KEEP, fx: [dmg(3, 'allEnemies'), stack(M, 1, { set: 3 }), lock] },                       // 메인딜(보름 고정)
        { type: '스킬', tags: KEEP, fx: [heal(2), dark, { k: 'nextAp', v: 1 }] },                         // 서포터(그믐)
        { type: '스킬', tags: KEEP, fx: [shield(1.5), half, status('약화', 1, { target: 'allEnemies' })] },   // 서포터(반달)
      ],
      blesses: ['power', 'cost', [fl(1)]],
    },
    { // 4차 8장 — 서포터 갈래(원작 고학년 「지극정성의 마중」 — 달빛 마중)
      name: '달빛 마중', cost: 1, type: '스킬',
      fx: [heal(1), dark, status('둔화', 1, { target: 'allEnemies' })],
      oracles: [
        { fx: [heal(1.4), dark, status('둔화', 1, { target: 'allEnemies' })] },
        { fx: [shield(1.2), half, status('약화', 1, { target: 'allEnemies' })] },
        { fx: [heal(1), draw(1, { who: 'other' })] },
        { fx: [heal(1), pick] },
        { fx: [heal(0.6), fl(1)] },
      ],
      blesses: ['heal', 'cost', [fl(1)]],
    },
  ],
});
