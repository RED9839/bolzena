// 2026-10-11 사용자 「벨라 힐러 겸 탱커로」: 촛불이 탈 때 실드 + 파티 회복(burn heal), 나눠 주기 · 터뜨리기 신탁 · 고학년에 회복.
// 벨라 — 탱커 · 활발 · 유령. 설계: _measure/엘다인_엔진/01_사도별.md §15 (벨라 5차 — 「존속의 촛불」, 사용자 승인 10-11)
// 2026-10-12 밤 사용자 「아군에게 걸어야 하는 게 너무 별로」 → 파티 촛불: 「존속」 은 벨라가 쥔 촛불(carrier self · 최대 7) —
//   벨라가 아닌 동료가 카드를 낼 때마다 1 타며 벨라 방어력 기준 실드 · 회복(키워드 burn — 엔진 셈). 고를 동료 없음(oneAlly · holder · moveMark 모두 뺌).
//   결정 지점은 「누구에게 켤까」 대신 「얼마나 쌓고 언제 터뜨릴까」 — 촛불이 7 을 넘으면 버려지고, 「너의 존재를 비춰 줘」 는 남은 촛불 1당 세진다.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '벨라', K = '존속';
const lit = (v = 1) => stack(K, v);   // 파티 촛불 +v(벨라가 쥠 — 고를 것 없음)
const perK = { k: 'perStack', id: K };   // 존속 1당(소모 없음)

export default hero({
  id: H, name: '벨라', nature: '활발', race: '유령', role: '탱커', star: 3,
  blurb: '경계선 위를 떠도는 앳된 유령. 파티에 존속의 촛불을 켜 두면, 동료들이 움직일 때마다 촛불이 타며 파티를 감싸고 상처를 아물게 합니다.',
  keyword: {
    name: K, desc: '벨라가 파티에 켜 둔 존속의 촛불', carrier: 'self', cap: 7,
    burn: { on: 'play', shield: 0.5, heal: 0.15 },   // 10-12 밤 파티 촛불(벨라 말고 누가 내든 탐) — 수치는 측정으로   // 10-12 탱커 손질 실드 80→90%   // 10-11 힐러 겸 탱커 실드 80% + 회복 30%
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '존재의 불꽃', desc: '촛불이 남긴 존재의 불꽃', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.05 }] }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '영속', when: { on: 'lowHp', pct: 0.5 }, fx: [heal(1)] },   // 10-12 탱커 손질: 파티 HP 가 50% 아래로 떨어지면 회복(위기에 붙잡는 탱커)
    { name: '경계선상의 유령', when: { on: 'fightStart' }, fx: [shield(1)] },   // 전투 시작 실드
  ],
  ult: { name: '앳된 존재의 증명', cost: 150, fx: [ddmg(1.5, 'allEnemies'), lit(3), shield(1.5), heal(1.5)] },   // 10-12 고학년 손질: 실드 100→150% · 회복 100→150% · 10-12 밤 촛불은 파티에
}, {
  basicNames: ['유령 손짓', '촛농 떨구기', '반투명 가드'], deck: 'heavy',
  uniques: [
    { // 시동 · 보존 — 첫 촛불
      name: '첫 번째 촛불', cost: 1, type: '스킬', tags: ['보존'],
      fx: [shield(2), lit(3)],
      oracles: [
        { tags: ['보존'], fx: [shield(1), lit(4)] },
        { tags: ['보존'], fx: [shield(1), lit(3), draw(1)] },   // 10-12 밤: 옛 「다른 동료의 존속을 이 동료에게 모아 옮김」(moveMark) → 촛불 2 · 뽑기
        { cost: 0, tags: ['보존'], fx: [shield(1), lit(3)] },   // 10-13 신탁 점검 3차: 실드 120%   // 10-13 신탁 점검: 존속 2→3
        { power: true, fx: [shield(1), lit(2), power([rule({ on: 'play', who: 'other', every: 2 }, [lit(1)])])] },   // 10-12 밤: 동료 카드마다 +1 은 타는 만큼 다시 차 끝이 없어 2장마다
        { tags: ['개전', '보존'], fx: [shield(1), lit(2)] },
      ],
      blesses: ['guard', 'draw', [lit(1)]],
    },
    { // 10-12 밤: 옛 「존속이 없으면 회복」 → 촛불이 많이 쌓였으면 회복(얼마나 쌓을까)
      name: '존재감 나눠 주기', cost: 0, type: '스킬',
      fx: [lit(2), stack('존재의 불꽃', 1), ifStack(K, 5), heal(0.8)],
      oracles: [
        { fx: [lit(3), stack('존재의 불꽃', 1), draw(1, { who: 'other' })] },   // 10-13 신탁 점검: 존재의 불꽃 되살림(아래 넷도)
        { fx: [lit(3), stack('존재의 불꽃', 1), empower(0.3, { who: 'other' })] },
        { tags: ['보존'], fx: [lit(4), stack('존재의 불꽃', 1)] },
        { cost: 1, fx: [lit(2), stack('존재의 불꽃', 1), shield(1.4)] },
        { fx: [lit(2), stack('존재의 불꽃', 1), status('사기', 1)] },
      ],
      blesses: ['guard', 'draw', [lit(1)]],
    },
    {
      name: '랜턴 가드', cost: 2, type: '스킬',
      fx: [shield(3.6), { k: 'when', on: 'shieldLeft' }, lit(2), stack('존재의 불꽃', 1)],
      oracles: [
        { fx: [shield(3.6), { k: 'when', on: 'shieldLeft' }, lit(3), stack('존재의 불꽃', 1)] },   // 10-13 신탁 점검: 실드를 깎기만 하던 신탁 — 기본 + 존속 1
        { fx: [shield(2.3), status('실드 유지', 1)] },   // 10-13 신탁 점검
        { cost: 1, fx: [shield(1.5), { k: 'when', on: 'shieldLeft' }, lit(2)] },   // 10-13 신탁 점검
        { fx: [shield(2.5), { k: 'when', on: 'shieldLeft' }, lit(3), stack('존재의 불꽃', 1)] },   // 10-13 신탁 점검
        { cost: 1, fx: [status('끈기', 1), lit(4)] },   // 10-13 신탁 점검 3차   // 10-13 신탁 점검: 비용 1 · 존속 3
      ],
      blesses: ['guard', 'cost', [lit(1)]],
    },
    { // 터뜨리기 — 존속 1당(소모 없음) · 언제 터뜨릴까
      name: '너의 존재를 비춰 줘', cost: 2, type: '공격',
      fx: [ddmg(2.6), perK, ddmg(0.3)],
      oracles: [
        { fx: [ddmg(1, 'allEnemies'), perK, ddmg(0.1, 'allEnemies')] },
        { type: '스킬', fx: [{ k: 'spend', id: K, all: true }, { k: 'perEvent', n: 1 }, heal(0.6)] },
        { fx: [ddmg(1.5), perK, ddmg(0.4)] },
        { cost: 1, fx: [ddmg(1), perK, ddmg(0.1)] },
        { fx: [ddmg(1.5), perK, ddmg(0.2), { k: 'nextAp', v: 1 }] },
      ],
      blesses: ['power', 'cost', [lit(1)]],
    },
    { // 강화 — 끈기 · 매 턴 존속 보충
      name: '존재란 무엇인가?', cost: 1, type: '강화',
      fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [lit(1)])])],
      oracles: [
        { fx: [status('끈기', 2), power([rule({ on: 'turnStart' }, [lit(1)])])] },
        { fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [draw(1, { who: 'other' })])])] },
        { tags: ['개전'], fx: [status('끈기', 1), lit(1), power([rule({ on: 'turnStart' }, [lit(1)])])] },   // 10-13 신탁 점검
        { fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [lit(1)]), rule({ on: 'play', who: 'other' }, [shield(0.1)])])] },
        { fx: [status('피해 감소', 3), power([rule({ on: 'turnStart' }, [lit(1)])])] },   // 10-13 신탁 점검
      ],
      blesses: ['cost', 'draw', [lit(1)]],
    },
  ],
});
