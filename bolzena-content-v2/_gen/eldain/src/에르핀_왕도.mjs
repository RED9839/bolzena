// 에르핀(왕도) — 딜러 · 순수 · 요정. 설계: _measure/엘다인_엔진/01_사도별.md §12 (4차 10-11 — 다시 짬: X 난타 + 근면, D11)
// 엔진: 시동 「축복의 왕관」 으로 근면(처치 → AP 1 · 드로우 1, 파티 상태라 누가 처치하든)을 깔고 → 싼 카드 · 약점 공격으로 처치각을 만든 뒤
//   → X 「마력 난타」 로 남은 AP 를 무작위 적에게 쏟되 처치할 때마다 한 대 더(▶E79) → 근면이 AP · 드로우를 되돌려 다음 카드로 잇는다.
// 왕관 겹 자원은 없앴다(X 코스트는 에르핀 소유). 자동 규칙 없음.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '에르핀_왕도', BR = `${H}_t1`;
const OT = (r) => empower(r, { who: 'other' });
const WK = ['약점 공격'];
const dil = (v = 1) => status('근면', v);
const barrage = (ratio, target = 'randomEnemy', x) => dmg(ratio, target, { xHits: true, killMore: true, ...x });

export default hero({
  id: H, name: '에르핀(왕도)', nature: '순수', race: '요정', role: '딜러', star: 3,
  blurb: '요정 왕국의 여왕이 된 에르핀. 남은 왕마력을 마력 난타로 모두 쏟아붓고, 적이 쓰러질 때마다 한 발 더 — 부지런한 여왕은 쓰러뜨린 만큼 숨을 돌립니다.',
  keyword: { name: '축복의 왕관', desc: '여왕이 씌워 준 왕관 — 쓴 동료의 공격력이 오른다', carrier: 'hero', cap: 1, hunt: true, per: [{ stat: 'atk', v: 0.1, who: 'holder' }] },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '왕마력', desc: '여왕의 왕마력 — 왕마력탄에 마력탄이 줄줄이 따라붙는다', carrier: 'self', cap: 5 }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '모두를 위한 배려', when: { on: 'fightStart' }, fx: [dil(1)] },   // 원작 2성 — 전투 시작 근면 1
    { name: '여왕의 의지', when: { on: 'kill', who: 'any' }, fx: [empower(0.2)] },   // 처치마다 자신의 다음 카드 +20%(처치 — 에르핀 소유)
  ],
  ult: { name: '에르피엔 펀치', cost: 300, fx: [dmg(0.4, 'oneEnemy', { hits: 8 }), dil(1), { k: 'dealtMod', v: 0.2, target: 'allAllies' }] },
}, {
  basicNames: ['마력탄', '연속 마력탄', '왕관 방패'], deck: 'light',
  uniques: [
    { // 시동
      name: '축복의 왕관', cost: 0, type: '스킬',
      fx: [dil(1), draw(2)],
      oracles: [
        { fx: [dil(2), draw(2)] },
        { power: true, fx: [dil(1), power([rule({ on: 'turnStart' }, [dil(1)])])] },
        { fx: [dil(1), OT(0.3)] },
        { fx: [status('저장', 1), draw(1)] },   // 딸기 케이크(AP 를 남김)
        { tags: ['개전'], fx: [dil(1)] },
      ],
      blesses: ['draw', 'cost', [dil(1)]],
    },
    {
      name: '에르피엔 왕마력탄', cost: 1, type: '공격', tags: WK,
      fx: [dmg(4), dmg(0.3, 'randomEnemy', { hitsPer: '왕마력' }), stack('왕마력', 1)],
      oracles: [
        { tags: WK, fx: [dmg(3.6)] },
        { tags: [...WK, '회수 3'], fx: [dmg(3.2)] },
        { tags: WK, fx: [dmg(1.1, 'allEnemies')] },
        { cost: 0, tags: [...WK, '소멸'], fx: [dmg(1.4)] },
        { tags: WK, fx: [dmg(3.2), { k: 'ifKill' }, dil(1)] },
      ],
      blesses: ['power', 'cost', [dil(1)]],
    },
    {
      name: '반쪽 빵', cost: 0, type: '스킬',
      fx: [OT(1), draw(1)],
      oracles: [
        { fx: [OT(1.2), draw(1)] },
        { fx: [{ k: 'gauge', v: 20, target: 'oneAlly' }, draw(1)] },
        { fx: [make(BR, 1, { owner: 'other' }), draw(1)] },
        { fx: [draw(2)] },
        { tags: ['소멸'], fx: [OT(0.3), { k: 'nextAp', v: 1 }] },
      ],
      blesses: ['draw', 'ap', [dil(1)]],
    },
    { // 마무리 — X: 남은 AP 만큼 · 처치하면 한 대 더(최대 12)
      name: '마력 난타', cost: 0, x: true, type: '공격',
      fx: [dmg(1.4, 'randomEnemy'), barrage(1.4)],   // X = 0 이어도 한 대(설계 0 자원)
      oracles: [
        { power: true, fx: [power([rule({ on: 'kill', mine: true }, [dmg(0.5, 'randomEnemy')])])] },
        { fx: [dmg(1.1), barrage(1.1, 'oneEnemy')] },
        { fx: [dmg(1, 'randomEnemy', { hits: 2 }), barrage(1)] },
        { tags: ['보존'], fx: [dmg(1, 'randomEnemy'), barrage(1)] },   // 10-11 사용자: X 코스트에 회수는 돌아와도 AP 0 이라 무의미 — 보존(AP 많은 턴까지 쥐기)
        { tags: ['소멸'], fx: [status('면역', 1), barrage(1.2), { k: 'nextAp', v: 1 }] },   // 원작 여왕의 의지
      ],
      blesses: ['power', 'draw', [dil(1)]],
    },
    { // 4차 8장 — 열기(원작 2성 「모두를 위한 배려」 — 여왕의 의지)
      name: '모두를 위한 배려', cost: 1, type: '스킬',
      fx: [stack('축복의 왕관', 1, { target: 'oneAlly' }), draw(1, { who: 'other' }), OT(0.5)],
      oracles: [
        { fx: [status('면역', 1), draw(1, { who: 'other' }), OT(0.5)] },
        { fx: [dil(1), draw(1, { who: 'other' })] },
        { cost: 0, fx: [draw(1, { who: 'other' }), OT(0.2)] },
        { fx: [status('면역', 1), status('사기', 1)] },
        { fx: [{ k: 'gauge', v: 20, target: 'otherAllies' }, OT(0.3)] },
      ],
      blesses: ['draw', 'cost', [dil(1)]],
    },
  ],
  tokens: [
    { id: BR, name: '반쪽 빵', cost: 0, type: '스킬', tags: ['소멸'], fx: [empower(0.5)], blurb: '하나 남은 빵을 반으로 나눴습니다' },
  ],
});
