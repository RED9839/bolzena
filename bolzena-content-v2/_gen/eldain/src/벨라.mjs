// 2026-10-11 사용자 「벨라 힐러 겸 탱커로」: 촛불이 탈 때 실드 + 파티 회복(burn heal), 나눠 주기 · 터뜨리기 신탁 · 고학년에 회복.
// 벨라 — 탱커 · 활발 · 유령. 설계: _measure/엘다인_엔진/01_사도별.md §15 (벨라 5차 — 「존속의 촛불」, 사용자 승인 10-11)
// 엔진: 벨라가 다른 동료 한 명에게 「존속」 촛불(벨라 소유 키워드 · 동료에게 붙음 · 최대 5 · 한 번에 한 명)을 켠다 →
//   그 동료가 자기 카드(그 동료가 주인인 생성 카드 포함)를 낼 때마다 1 타며 벨라 방어력 기준 실드 30%(키워드 burn — 엔진 셈, 자동 규칙 아님).
//   벨라 자신은 못 받는다. 누구에게 켜고 · 언제 보충하고 · 터뜨리기로 얼마나 쓸지가 결정 지점. 끈기 · 사기는 걸어 주기만(소유 아님).
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '벨라', K = '존속';
const lit = (v = 1, target = 'oneAlly') => stack(K, v, { target });   // 다른 아군 1명에게 존속 +v(옮겨 감 — 한 번에 한 명)
const holder = (v = 1) => stack(K, v, { target: 'holder' });          // 지금 존속이 붙은 동료에게 +v
const has = { k: 'ifStack', id: K, n: 1, who: 'any' }, none = { k: 'ifStack', id: K, n: 1, who: 'any', not: true };
const perK = { k: 'perStack', id: K, who: 'any' };                      // 존속 1당(소모 없음)

export default hero({
  id: H, name: '벨라', nature: '활발', race: '유령', role: '탱커', star: 3,
  blurb: '경계선 위를 떠도는 앳된 유령. 동료 한 명에게 존속의 촛불을 켜 주면, 그 동료가 움직일 때마다 촛불이 타며 파티를 감싸고 상처를 아물게 합니다.',
  keyword: {
    name: K, desc: '벨라가 동료에게 켜 준 존속의 촛불', carrier: 'hero', cap: 7, hunt: true,
    burn: { on: 'play', shield: 0.8, heal: 0.3 },   // 10-11 힐러 겸 탱커 — 실드 120% → 실드 80% + 회복 30%(60% 는 봇 67% 로 1위라 낮춤)
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '존재의 불꽃', desc: '촛불이 남긴 불꽃 — 벨라의 실드가 두꺼워진다', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.05 }] }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '영속', when: { on: 'endure' }, fx: [heal(1)] },   // 원작 — 끈기로 버티면 회복
    { name: '경계선상의 유령', when: { on: 'fightStart' }, fx: [shield(1)] },   // 전투 시작 실드
  ],
  ult: { name: '앳된 존재의 증명', cost: 150, fx: [ddmg(1.5, 'allEnemies'), holder(3), shield(1), heal(1)] },
}, {
  basicNames: ['유령 손짓', '촛농 떨구기', '반투명 가드'], deck: 'heavy',
  uniques: [
    { // 시동 · 보존 — 첫 촛불
      name: '첫 번째 촛불', cost: 1, type: '스킬', tags: ['보존'],
      fx: [shield(2), lit(3)],
      oracles: [
        { tags: ['보존'], fx: [shield(1), lit(4)] },
        { tags: ['보존'], fx: [shield(1), { k: 'moveMark', id: K, target: 'oneAlly' }] },   // 다른 동료의 존속을 이 동료에게 모두
        { cost: 0, tags: ['보존'], fx: [lit(2)] },
        { power: true, fx: [lit(1), power([rule({ on: 'play', who: 'other' }, [holder(1)])])] },
        { tags: ['개전', '보존'], fx: [shield(1), lit(2)] },
      ],
      blesses: ['guard', 'draw', [holder(1)]],
    },
    {
      name: '존재감 나눠 주기', cost: 0, type: '스킬',
      fx: [holder(2), stack('존재의 불꽃', 1), none, heal(0.8)],
      oracles: [
        { fx: [holder(2), draw(1, { who: 'other' })] },
        { fx: [holder(1), empower(0.2, { who: 'other' })] },
        { tags: ['보존'], fx: [holder(3)] },
        { cost: 1, fx: [holder(2), shield(1)] },
        { fx: [holder(1), status('사기', 1)] },
      ],
      blesses: ['guard', 'draw', [holder(1)]],
    },
    {
      name: '랜턴 가드', cost: 2, type: '스킬',
      fx: [shield(3.6), { k: 'when', on: 'shieldLeft' }, holder(2), stack('존재의 불꽃', 1)],
      oracles: [
        { fx: [shield(2.6), { k: 'when', on: 'shieldLeft' }, holder(2)] },
        { fx: [shield(2), status('실드 유지', 1)] },
        { cost: 1, fx: [shield(1.2), { k: 'when', on: 'shieldLeft' }, holder(1)] },
        { fx: [shield(2), { k: 'when', on: 'shieldLeft' }, holder(3)] },
        { fx: [status('끈기', 1), holder(2)] },
      ],
      blesses: ['guard', 'cost', [holder(1)]],
    },
    { // 터뜨리기 — 존속 1당(소모 없음)
      name: '너의 존재를 비춰 줘', cost: 2, type: '공격',
      fx: [ddmg(2.6), perK, ddmg(0.3)],
      oracles: [
        { fx: [ddmg(1, 'allEnemies'), perK, ddmg(0.1, 'allEnemies')] },
        { type: '스킬', fx: [{ k: 'spend', id: K, all: true, who: 'any' }, { k: 'perEvent', n: 1 }, heal(0.6)] },
        { fx: [ddmg(1.5), perK, ddmg(0.4)] },
        { cost: 1, fx: [ddmg(1), perK, ddmg(0.1)] },
        { fx: [ddmg(1.5), perK, ddmg(0.2), { k: 'nextAp', v: 1 }] },
      ],
      blesses: ['power', 'cost', [holder(1)]],
    },
    { // 강화 — 끈기 · 매 턴 존속 보충
      name: '존재란 무엇인가?', cost: 1, type: '강화',
      fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [holder(1)])])],
      oracles: [
        { fx: [status('끈기', 2), power([rule({ on: 'turnStart' }, [holder(1)])])] },
        { fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [draw(1, { who: 'other' })])])] },
        { tags: ['개전'], fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [holder(1)])])] },
        { fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [holder(1)]), rule({ on: 'play', who: 'other' }, [shield(0.1)])])] },
        { fx: [status('피해 감소', 2), power([rule({ on: 'turnStart' }, [holder(1)])])] },
      ],
      blesses: ['cost', 'draw', [holder(1)]],
    },
  ],
});
