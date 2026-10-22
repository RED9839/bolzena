// 에피카 — 딜러 · 활발 · 수인. 설계: _measure/엘다인_엔진/01_사도별.md §4 (4차 10-11 — 3차 그대로 · 연계 단독 소유)
// 엔진: 0 · 1코로 손을 갈며(뽑고 · 버리고 · 내고) 그 장수만큼 「에피콘의 분신술」(무대 열기 — 엔진이 세는 수)을 띄우고
//   → 열기를 「에피칸」(에피콘 무리)으로 바꿔 남기면 동료 카드마다 따라 치고(★) · 아니면 「교주에게 바칩니다」 로 그 수만큼 쏟는다.
// 원작 보강(10-11 검토): 영감은 「에피콘 부르기」 ⑤ 한 갈래로 · 동료 템포 공급은 「극적인 연출」 ⑤. 자동 규칙은 「아군에게 바칩니다」 하나.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '에피카', HEAT = '에피콘의 분신술', K = '에피칸';
const S = (v = 1, x) => stack(K, v, x);
const heat = { k: 'perStack', id: HEAT, n: 2 };   // 열기 1개당(최소 2회)

export default hero({
  id: H, name: '에피카', nature: '활발', race: '수인', role: '딜러', star: 3,
  blurb: '하프를 켜는 유니콘 음유시인. 손을 빠르게 갈며 무대를 달구고, 똑같은 차림의 에피콘 제자들은 동료가 카드를 낼 때마다 따라 칩니다.',
  keyword: {
    name: HEAT, desc: '카드를 낼수록 달아오르는 무대', carrier: 'self', cap: 10, endClear: true,
    tally: ['play', 'discardBy'],
  },
  keywords: [{
    name: K, desc: '똑같은 차림으로 공연하는 에피콘 제자들', carrier: 'self', cap: 4, start: 2,   /* 10-21 밸런스 조절 3차: 시작 1 → 2 */
    rules: [{ name: '아군에게 바칩니다', when: { on: 'play', who: 'other' }, conds: [{ c: 'stack', id: K, n: 1 }], fx: [{ k: 'extra', ratio: 0.5, target: 'oneEnemy' }] }],   // 10-21 밸런스 조절: 10 → 50%(1 · 2 · 6 · 10차)
  }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '극적인 연출', when: { on: 'turnStart' }, conds: [{ c: 'stack', id: K, n: 3 }], fx: [status('사기', 1)] },   // 에피칸 3 이상이면(10-21 3차에 2 로 내렸다가 혼자 80% 로 넘쳐 되돌림) 매 턴 사기 1
  ],
  ult: { name: '교주에게 바칩니다(공연)', cost: 250, fx: [dmg(1.6, 'allEnemies'), S(3), status('사기', 2), draw(3)] },   // 10-21 밸런스 조절: 적 전체 160% 더함(1 · 9차)
}, {
  basicNames: ['하프 튕기기', '현 뜯기', '용각산 한 숟갈'], deck: 'light', starter: true,
  uniques: [
    { // 시동(은총으로)
      name: '극적인 연출', cost: 0, type: '스킬',
      fx: [draw(2), discard(1)],
      oracles: [
        { fx: [draw(3), discard(1)] },
        { fx: [draw(2), stack(HEAT, 2)] },
        { fx: [discard(1, { all: true }), draw(5)] },
        { power: true, fx: [draw(2), power([rule({ on: 'play', every: 3 }, [draw(1)])])] },   // 10-13 신탁 점검
        { fx: [{ k: 'costMod', v: -1, n: 1, who: 'other', turns: 1 }, draw(1)] },
      ],
      blesses: ['draw', 'ap', [stack(HEAT, 1)]],
    },
    {
      name: '앵콜', cost: 0, type: '스킬',
      fx: [discard(2), draw(2)],
      oracles: [
        { fx: [discard(3), draw(3)] },
        { fx: [discard(2), draw(2), S(1)] },
        { type: '공격', cost: 1, fx: [discard(2), { k: 'perDiscarded' }, dmg(0.7, 'randomEnemy'), draw(2)] },
        { fx: [discard(1), draw(3)] },
        { fx: [discard(2), draw(1), { k: 'pull', from: 'discarded', n: 1 }] },   // 10-13 신탁 점검: 드로우 1 되살림
      ],
      blesses: ['draw', 'ap', [stack(HEAT, 1)]],
    },
    {
      name: '에피콘 부르기', cost: 1, type: '스킬',
      fx: [S(1), draw(1), ifStack(HEAT, 3), S(1)],
      oracles: [
        { fx: [S(1), draw(1), ifStack(HEAT, 3), S(2)] },
        { cost: 0, fx: [S(1), ifStack(HEAT, 3), spend(HEAT, 3), S(1)] },   // 10-13 신탁 점검: 에피칸 1 은 늘
        { fx: [draw(1), ifStack(HEAT, 2), spend(HEAT, 2), S(1), status('사기', 1)] },   // 10-13 신탁 점검: 드로우 되살림
        { fx: [discard(1), draw(2), S(1)] },   // 10-13 신탁 점검: 드로우 2
        { fx: [draw(1), ifStack(HEAT, 3), S(1), { k: 'when', on: 'draw' }, S(1)] },
      ],
      blesses: ['cost', 'draw', [S(1)]],
    },
    { // 마무리 — 열기만큼(최소 2회), 열기를 쓰지 않는다
      name: '교주에게 바칩니다', cost: 1, type: '공격',
      fx: [heat, dmg(0.8, 'randomEnemy'), status('사기', 1)],   // 10-21 밸런스 조절: 열기 1개당 40 → 80%   // 10-21 밸런스 조절: 열기 1개당 40 → 50%
      oracles: [
        { power: true, fx: [S(1), status('사기', 1), power([rule({ on: 'play', nth: 4 }, [S(1)])])] },   // 10-13 신탁 점검: 사기 되살림
        { fx: [heat, dmg(0.8), status('사기', 1)] },
        { fx: [{ k: 'perStack', id: K, n: 1 }, dmg(0.8, 'randomEnemy', { hits: 2 })] },   // 10-13 신탁 점검
        { cost: 0, fx: [heat, dmg(0.3, 'randomEnemy'), draw(1)] },
        { fx: [heat, dmg(0.8, 'randomEnemy'), { k: 'nextAp', v: 1 }] },
      ],
      blesses: ['power', 'cost', [S(1)]],
    },
    { // 4차 8장 — 터뜨리기(에피칸 쪽): 무대에 남긴 에피콘과 함께 인사한다
      name: '무대 인사', cost: 1, type: '공격',
      fx: [dmg(1.5, 'randomEnemy'), { k: 'perStack', id: K }, dmg(0.5, 'randomEnemy')],   /* 10-21 8차: 130 → 150% */   // 10-21 밸런스 조절: 80 → 130% · 1당 30 → 50%(2 · 6차)   // 10-19 타수: 2회 × 40% + 1당 30% → 80% + 1당 30%(2타 · 합 그대로)
      oracles: [
        { fx: [dmg(1.5, 'randomEnemy'), { k: 'perStack', id: K }, dmg(0.6, 'randomEnemy')] },   /* 10-21 8차: 130 → 150% */   // 10-21: 80 → 130% · 1당 40 → 60%   // 10-19 타수: 2회 × 40% → 80%
        { fx: [dmg(0.4, 'randomEnemy', { hits: 2 }), S(1)] },
        { fx: [{ k: 'perStack', id: K, n: 1 }, dmg(0.8)] },   // 10-13 신탁 점검
        { cost: 0, fx: [{ k: 'perStack', id: K }, dmg(0.3, 'randomEnemy'), draw(1)] },
        { fx: [dmg(0.4, 'randomEnemy', { hits: 2 }), status('사기', 1)] },
      ],
      blesses: ['power', 'cost', [S(1)]],
    },
  ],
});
