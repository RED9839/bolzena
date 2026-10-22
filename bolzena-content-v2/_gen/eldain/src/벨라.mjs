// 벨라 — 탱커 · 활발 · 유령.
// 2026-10-13 리뉴얼(사용자 「존재감을 채워 파티 피해를 키울지, 소모해 파티 케어를 할지 — 카드를 쓸 때마다 고르게」):
//   「존재감」(최대 7)을 쥐고 있으면 1겹당 파티 모두 피해 +5%. 고유 카드는 모두 두 갈래 — 낼 때 「채우기」(존재감 +) · 「쓰기」(존재감을 모두 써서 1개당 케어 · 한 방).
//   쓰는 순간 파티 피해 덤도 사라진다 — 쌓아 둘까, 지금 돌볼까.
//   없앤 것: 촛불이 동료 카드마다 저절로 타며 주던 실드 · 회복(burn) · 보조 자원 「존재의 불꽃」 · 위기 회복 「영속」. 이전 판: C:\projects\_backup\bella_before_20261013
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '벨라', K = '존재감';
const lit = (v = 1) => stack(K, v);
const CH = ['채우기', '쓰기'];
const fill = (...x) => [{ k: 'ifChoice', n: 1 }, ...x];                                   // 「채우기」 를 고르면
const use = (fx) => [{ k: 'ifChoice', n: 2 }, spend(K, 'all'), { k: 'perEvent' }, fx];    // 「쓰기」 — 존재감을 모두 써서 1개당
const useN = (n, fx) => [{ k: 'ifChoice', n: 2 }, spend(K, n), { k: 'perEvent' }, fx];    // 「쓰기」 — n 개까지

export default hero({
  id: H, name: '벨라', nature: '활발', race: '유령', role: '탱커', star: 3,
  blurb: '경계선 위를 떠도는 앳된 유령. 존재감을 쌓아 두면 파티가 힘을 얻고, 나눠 주면 파티를 감싸고 상처를 아물게 합니다.',
  keyword: {
    name: K, desc: '벨라가 쌓아 둔 존재감 — 쥐고 있으면 파티가 세지고, 쓰면 파티를 돌본다', carrier: 'self', cap: 5,   // 10-21 밸런스 조절: 최대 7 → 6 → 5(3차)
    per: [{ stat: 'dealt', v: 0.05, who: 'allies' }],
  },
  passives: [
    { name: '앳된 존재감', when: { on: 'play', who: 'other', every: 3 }, fx: [lit(1)] },   // 동료 카드 3장마다 존재감 1(10-21 밸런스 조절: 2 → 3장)
    { name: '경계선상의 유령', when: { on: 'fightStart' }, fx: [shield(0.5), lit(1)] },   // 10-21 밸런스 조절: 존재감 2 → 1 · 4차 실드 120 → 60%
  ],
  ult: { name: '앳된 존재의 증명', cost: 200,   /* 10-21 밸런스 조절: 150 → 200 */ fx: [ddmg(2, 'allEnemies'), lit(3), shield(1), heal(1)] },   // 10-21 7차: 실드 · 회복 180 → 120%
}, {
  basicNames: ['유령 손짓', '촛농 떨구기', '반투명 가드'], deck: 'heavy',
  uniques: [
    // 신탁 = 방향 고르기(사용자 「완전히 리뉴얼」): 더 채우기 · 동료 강화 · 조금만 쓰기 · 쓰기를 실드 · 피해 · AP · 약화 · 사기로 · 쥔 만큼 세지기 · 강인도 · 실드 유지 · 끈기.
    { // 시동 · 보존
      name: '첫 번째 촛불', cost: 1, type: '스킬', tags: ['보존'], choices: CH,
      fx: [shield(1.67), ...fill(lit(2)), ...use(heal(0.15))],   // 10-21 밸런스 조절: 실드 240 → 200%(2차) · 채우기 3 → 2(5차)
      oracles: [
        { tags: ['보존'], fx: [shield(1), ...fill(lit(5)), ...use(heal(0.15))] },                                  // 더 채우기
        { fx: [...fill(lit(2), empower(0.3, { who: 'other' })), ...use(heal(0.15))] },    // 채우며 동료 다음 카드 +30%(글 45자 — 이것만 보존 뺌)
        { cost: 0, tags: ['보존'], fx: [shield(0.8), ...fill(lit(2)), ...useN(2, heal(0.3))] },                    // 0코 · 쓰기는 2개만 크게
        { tags: ['보존'], fx: [shield(1.5), ...fill(lit(3)), ...use(shield(0.4))] },                               // 쓰기 → 실드
        { tags: ['개전', '보존'], fx: [...fill(lit(3)), ...use(ddmg(0.5, 'allEnemies'))] },   // 10-19 밸런스: 1개당 전체 30 → 50%              // 개전 · 쓰기 → 적 전체 피해
      ],
      blesses: ['guard', 'draw', [lit(1)]],
    },
    {
      name: '존재감 나눠 주기', cost: 0, type: '스킬', choices: CH,
      fx: [...fill(lit(1)), ...use(heal(0.15))],   // 10-21 밸런스 조절 6차: 채우기 2 → 1
      oracles: [
        { fx: [draw(1), ...fill(lit(2)), ...use(heal(0.15))] },                                                     // 드로우
        { fx: [...fill(lit(2)), ...use(ddmg(0.5, 'allEnemies'))] },   // 10-19 밸런스: 30 → 50%                                               // 쓰기 → 1개당 적 전체 피해
        { tags: ['소멸'], fx: [...fill(lit(2)), { k: 'ifChoice', n: 2 }, spend(K, 3), { k: 'ifEvent', n: 3 }, { k: 'ap', v: 1 }] },  // 쓰기 → 3개 써서 AP 1(0코 AP 라 소멸)
        { fx: [...fill(lit(1)), ...use(heal(0.25))] },                                                              // 쓰기 쪽으로
        { fx: [...fill(lit(2)), ...use(heal(0.1)), status('사기', 1)] },                                            // 쓰기 → 회복 + 사기 1
      ],
      blesses: ['guard', 'draw', [lit(1)]],
    },
    {
      name: '랜턴 가드', cost: 2, type: '스킬', choices: CH,
      fx: [shield(2), ...fill(lit(2)), ...use(shield(0.5))],   // 10-21 밸런스 조절 2차: 실드 290 → 240%
      oracles: [
        { fx: [shield(3.6), ...fill(lit(1)), ...use(shield(0.3))] },                                                // 큰 실드
        { cost: 1, fx: [shield(1.2), ...fill(lit(2)), ...use(shield(0.5))] },                                      // 1코
        { fx: [shield(2), status('실드 유지', 1), ...fill(lit(2)), ...use(shield(0.4))] },                          // 실드 유지
        { fx: [status('끈기', 1), ...fill(lit(2)), ...use(heal(0.2))] },                                            // 끈기 · 쓰기 → 회복
        { fx: [shield(2), ...fill(lit(2)), { k: 'ifChoice', n: 2 }, spend(K, 'all'), { k: 'ifEvent', n: 3 }, status('약화', 2, { target: 'allEnemies' })] },   // 쓰기 → 3개 넘게 쓰면 적 전체 약화 2
      ],
      blesses: ['guard', 'cost', [lit(1)]],
    },
    {
      name: '너의 존재를 비춰 줘', cost: 2, type: '공격', choices: CH,
      fx: [ddmg(2), ...fill(lit(2)), ...use(ddmg(0.8))],   // 10-19 밸런스: 쓰기 1개당 60 → 80%
      oracles: [
        { fx: [ddmg(1, 'allEnemies'), ...fill(lit(2)), ...use(heal(0.1))] },                                       // 적 전체 · 쓰기 → 회복
        { fx: [ddmg(2), ...fill(lit(1), { k: 'perStack', id: K }, ddmg(0.5))] },   // 10-19: 채우기 1당 30 → 50%                   // 채우기 → 쥔 만큼 더 세게(쓰지 않음)
        { fx: [ddmg(2), ...fill(lit(2)), { k: 'ifChoice', n: 2 }, spend(K, 'all'), { k: 'ifEvent', n: 3 }, { k: 'tough', v: 2 }] },   // 쓰기 → 3개 넘게 쓰면 강인도 2
        { cost: 1, fx: [ddmg(1), ...fill(lit(1)), ...use(shield(0.3))] },                                         // 1코 · 쓰기 → 실드
        { fx: [ddmg(1.5), draw(1), ...fill(lit(2)), ...use(shield(0.3))] },                                       // 드로우 · 쓰기 → 실드
      ],
      blesses: ['power', 'cost', [lit(1)]],
    },
    { // 강화 — 한 번 내면 전투 내내라 두 갈래 없이 신탁으로 방향을 고른다(10-13 — 글에 「채우기: 채우기:」 가 겹쳐 찍혀서)
      name: '존재란 무엇인가?', cost: 1, type: '강화',
      fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [lit(1)])])],
      oracles: [
        { fx: [status('끈기', 2), power([rule({ on: 'turnStart' }, [lit(1)])])] },                              // 끈기 2
        { fx: [power([rule({ on: 'turnStart' }, [lit(2)])])] },                                                  // 매 턴 2개
        { tags: ['개전'], fx: [lit(1), power([rule({ on: 'turnStart' }, [lit(1)])])] },                         // 개전
        { fx: [status('피해 감소', 3), power([rule({ on: 'turnStart' }, [lit(1)])])] },                          // 피해 감소
        { fx: [status('끈기', 1), power([rule({ on: 'turnStart' }, [spend(K, 1), { k: 'perEvent' }, heal(0.3)])])] },   // 매 턴 쓰기 — 1개 써서 회복
      ],
      blesses: ['cost', 'draw', [lit(1)]],
    },
  ],
});
