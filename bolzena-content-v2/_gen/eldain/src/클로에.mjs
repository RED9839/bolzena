// 클로에 — 탱커(딜탱) · 광기 · 요정 · 변신 「탑승」.
// 2026-10-14 리뉴얼(사용자 결정 「메리 고 라운드」 딜탱): 자원 하나 · 변신 하나.
//   원작: 날개 없는 요정 재단사, 곰 인형 집사 세바스티안과 함께 — 저학년 「메리 고 라운드」 = 세바스티안에 올라타 큰 보호막 + 평타 3연타,
//   평타가 맞을 때마다 바늘땀(최대 9) · 9면 탑승 연장 · 탑승하지 않을 때는 내려찍기 + 도발, 고학년 = 쁘띠 세바스티안 7마리 돌진.
//   흐름: 클로에 카드가 적을 칠 때마다(타마다) 「바늘땀」 1 → 「메리 고 라운드」 로 3턴 탑승(큰 실드 · 고유 공격 카드 3연타 — 엔진 변신 덤 hits)
//         → 탑승 중 바늘땀 9 를 쓰면 탑승 +1턴 → 탑승이 끝나면 내려찍기(적 전체). 「곰 인형 내려찍기」 는 바늘땀을 모두 쏟는 마무리.
//   걷어 낸 것(10-11~10-13 판): 세바스티안 몸집(cap 5 · 대신 맞기 guard · 따라 치기 summon) · 「바느질」 파티 받는 피해 감소 · 「맞춤옷」 생성 카드와 맞춤 가봉 ·
//     「맞춤 의상」(탐구심 · 보존) · 「인형의 의지」 매 턴 피해 감소 · 「런웨이 오프닝」 — 자원 셋 · 생성 카드 · 남의 카드 손대기가 겹쳐 「무엇을 하는 사도인지」 가 흐렸다.
//     원작의 탑승 · 3연타 · 바늘땀 · 내려찍기 한 줄기로(10-14 도발 뺌) 덜어 냈다. 이전 판: C:\projects\_backup\chloe_before_20261014\클로에.mjs
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '클로에', Q = '바늘땀', F = `${H}_탑승`;
const ride = [{ k: 'form', id: F }];
const SF = (...x) => [{ k: 'ifForm', id: F }, ...x];   // 「탑승」 : 덤(디아나 「백수공권」 꼴)
// 10-14 사용자 「도발 시스템 없는 게 좋지 않음?」 → 도발을 뺐다(쓰는 사도가 클로에뿐이고 파티 공격엔 의미가 없어 읽기만 어려웠다)
const extend = { k: 'formExtend', id: F, v: 1, max: 6 };
const pour = (r, target = 'allEnemies') => [spend(Q, 'all'), { k: 'perEvent', n: 1 }, ddmg(r, target)];   // 바늘땀을 모두 써서 1개당(효과 3개 규칙이라 기본 피해 없이 — 쌓아 둔 만큼 내려찍는다)

export default hero({
  id: H, name: '클로에', nature: '광기', race: '요정', role: '탱커', star: 3,
  blurb: '날개 없는 요정 재단사. 곰 인형 집사 세바스티안에 올라타 큰 실드를 두르고, 꿰매듯 몰아치는 연타로 바늘땀을 채워 탑승을 이어 갑니다.',
  keyword: { name: Q, desc: '세바스티안을 꿰매는 재단사의 바늘 자국', carrier: 'self', cap: 9, per: [{ stat: 'taken', v: -0.05, who: 'allies' }] },   // 1개당 파티가 받는 피해 -5%(탱커 — 쌓아 두면 단단하고, 쏟으면 세다)
  passives: [
    { name: '한 땀 한 땀', when: { on: 'hit', each: true }, fx: [stack(Q, 1)] },   // 클로에 카드가 적을 칠 때마다(연타면 타마다) 바늘땀 1 — 엔진 hit each(10-14)
    { name: '한 바퀴 더', when: { on: 'hit', each: true }, conds: [{ c: 'stack', id: Q, n: 9 }], fx: [...SF(spend(Q, 9), extend)] },   // 탑승 중 바늘땀 9 → 탑승 +1턴(최대 6턴)
  ],
  forms: [{
    id: F, name: '탑승', desc: '세바스티안에 올라탄 클로에 — 3턴', stance: true, anim: 'Idle_Change', turns: 3,
    bonus: [{ type: '공격', unique: true, ratio: 0.5, hits: 3 }],   // 고유 공격 카드 3연타(한 대 50% — 합 150%) · 엔진 변신 덤 hits(10-14)
    off: [ddmg(0.8, 'allEnemies')],   // 내려오며 내려찍기(10-14 사용자: 도발 시스템 뺌)
  }],
  ult: { name: '쁘띠 세바스티안 돌격', cost: 250, fx: [ddmg(0.6, 'randomEnemy', { hits: 7 }), stack(Q, 9)] },   // 쁘띠 세바스티안 7마리 무작위 돌진 + 바늘땀 가득
}, {
  basicNames: ['곰 인형 휘두르기', '곰 인형 찌르기', '재단사의 자세'], deck: 'light',
  uniques: [
    { // 시동 — 탑승
      name: '메리 고 라운드', cost: 1, type: '스킬',
      fx: [shield(2), ...ride],
      oracles: [
        { fx: [shield(2.6), ...ride] },
        { fx: [shield(1.6), ...ride, stack(Q, 4)] },
        { fx: [shield(1.6), ...ride, draw(1)] },
        { cost: 0, fx: [shield(1), ...ride] },
        { fx: [shield(1.6), ...ride, status('피해 감소', 1)] },
      ],
      blesses: ['guard', 'draw', [stack(Q, 1)]],
    },
    { // 연타 공격 — 탑승 중 3연타
      name: '세바스티안, 공격!', cost: 1, type: '공격',
      fx: [ddmg(1.2)],
      oracles: [
        { fx: [ddmg(1.6)] },
        { fx: [ddmg(0.6, 'oneEnemy', { hits: 2 }), stack(Q, 1)] },   // 바늘땀 갈래 — 탑승 중 6연타(꼴이 본판과 같으면 빌드가 비율을 본판으로 올려 바늘땀 1 을 붙여 꼴을 가른다)
        { fx: [ddmg(0.7, 'allEnemies')] },
        { cost: 0, fx: [ddmg(0.8)] },
        { fx: [ddmg(1), shield(0.6)] },
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
    { // 마무리 — 내려찍기 · 도발 · 바늘땀을 모두 쏟는다
      name: '곰 인형 내려찍기', cost: 2, type: '공격',
      fx: [ddmg(0.4, 'allEnemies'), ...pour(0.3)],   // 10-14 사용자: 바늘땀 0 이어도 기본 피해 — 도발 뺀 뒤 혼자 84% 라 60 → 40%
      oracles: [
        { fx: [...pour(0.4)] },
        { fx: [...pour(0.6, 'oneEnemy')] },
        { cost: 1, fx: [...pour(0.2)] },
        { fx: [ddmg(1.2, 'allEnemies'), status('피해 감소', 1)] },   // 쏟지 않는 갈래
        { fx: [ddmg(1, 'allEnemies'), stack(Q, 3)] },   // 채우는 갈래
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
    { // 실드 · 바늘땀
      name: '건치 스마일', cost: 1, type: '스킬',
      fx: [shield(1.2), stack(Q, 2)],
      oracles: [
        { fx: [shield(1.6), stack(Q, 2)] },
        { fx: [shield(0.8), stack(Q, 4)] },
        { power: true, fx: [shield(1.2), power([rule({ on: 'turnStart' }, [stack(Q, 2)])])] },
        { cost: 0, fx: [shield(0.8), stack(Q, 1)] },
        { fx: [shield(1), stack(Q, 2), draw(1)] },
      ],
      blesses: ['guard', 'draw', [stack(Q, 1)]],
    },
    { // 실드 · 회복 — 탑승 중이면 +1턴
      name: '찢긴 곳 꿰매기', cost: 1, type: '스킬',
      fx: [shield(0.8), heal(0.6)],
      oracles: [
        { fx: [shield(1), heal(0.8)] },
        { fx: [shield(0.6), heal(0.6), stack(Q, 3)] },
        { fx: [shield(0.6), heal(0.6), ...SF(extend)] },
        { cost: 0, fx: [shield(0.5), heal(0.4)] },
        { fx: [heal(1.2), status('피해 감소', 1)] },
      ],
      blesses: ['guard', 'ap', [stack(Q, 1)]],
    },
  ],
});
