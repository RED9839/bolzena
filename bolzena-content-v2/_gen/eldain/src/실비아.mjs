// 실비아 — 서포터 · 광기 · 용족.
// 2026-11-01 갈라내기(사용자 안 A 「손님 고르기」 — _measure/실비아_벨라_우로스_설계안_20261101.md §2-1):
//   찻잔(자기 주머니 · 쥐면 파티 받는 피해 -4%/개 · 은분수로 털기)을 걷고, 동료에게 붙이는 「초청객」(동료마다 최대 2 · 실비아 자신에게는 안 붙음)으로.
//   「초청객」 1당 그 동료의 고학년 요구 게이지 -20%. 그 동료가 고학년을 쓰면 표시가 사라지고 파티가 은방울 2 · 실드(「진은의 대공」).
//   초청객은 실비아 카드 · 고학년 · 전투 시작 한 번(「어머니와 딸」)으로만 생긴다 — 저절로 차는 길이 없다(옛 찻잔은 가득 79% · 넘침 47%).
//   결정: 누구에게 붙이나(딜러 고학년 · 탱커 고학년) · 2겹 찬 동료에게 더 붙여 쓰게 하나, 나눠 두나 · 티파티를 여나(대가 = 실비아 카드로 게이지가 안 오름).
//   「궁극의 유희」 = 원작대로 — 이 전투 동안 턴 시작에 적 전체 받는 피해 + · 회복, 대가 게이지 정지(따라 치기 · 동료 비용 -1 · 은분수 AP 를 뺐다 — 네르 · 에피카 · 우이와 겹침).
//   원작: 저학년 「초청객 — 고학년 쿨감 35%」 · 「티파티 중 SP 회복 중단」 · A2 「초청객에게 보호막 · 충격파로 아군 SP 회복」 · 고학년 「은방울 보호막」. 은의 용족 · 비비의 양녀 · 진은의 대공.
//   엔진(2026-11-01 더함): 계기 ult + marked(그 표시가 붙은 동료가 고학년을 쓰면) · 키워드 noOwner(주인 자신에게는 안 붙음) · 고학년 규칙의 「아군 1명」 = 쓴 사도.
// 없앤 것: 고유 효과 「찻잔」(cap 5 · 1개당 파티 받는 피해 -4%) · 규칙 「초청객」(동료 스킬마다 찻잔 1) · 「진은의 대공」(턴 시작 찻잔 · 가득이면 실드) ·
//   「초대장」 동료 비용 -1 · 「은분수」 털기(1개당 실드 · 3개 이상 AP) · 「궁극의 유희」 따라 치기(동료 스킬마다 무작위 적 50%).
//   이전 판(찻잔 · 혼자 76.7%): C:\projects\_backup\hero_sep_20261101 · 그 앞(탱커 · 혼자 88.2%): C:\projects\_backup\silvia_before_20261014
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '실비아', K = '초청객';
const guest = (v = 1, target = 'oneAlly') => stack(K, v, { target });   // 동료 앞에 찻잔을 놓는다(주인 자신에게는 안 붙음 — noOwner)
const bell = (v = 2) => status('은방울', v);   // ▶E65 — 파티 상태(실드가 깨지거나 턴이 끝나면 피해 감소)
const glove = { k: 'dealtMod', v: -0.5, turns: 2, target: 'topEnemy' };   // 가장 센 적 주는 피해 -50%(2턴)
const gauge = (v) => ({ k: 'gauge', v });   // 고학년 게이지 +v%(티파티 중에는 게이지 정지가 막는다)
const lock = { k: 'gaugeLock' };   // 대가 — 실비아 카드로는 고학년 게이지가 오르지 않는다
const party = (fx) => power([rule({ on: 'turnStart' }, fx)]);   // 티파티 — 이 전투 동안 턴 시작에
const soft = (v) => ({ k: 'takenMod', v, target: 'allEnemies' });   // 적 전체 받는 피해 +v(1턴)

export default hero({
  id: H, name: '실비아', nature: '광기', race: '용족', role: '서포터', star: 3,
  blurb: '은빛 티파티를 여는 진은의 대공. 동료 앞에 찻잔을 놓아 손님으로 맞으면 그 동료의 고학년이 앞당겨지고, 손님이 고학년을 펼치면 파티가 은방울로 감싸입니다. 꼬마라고 부르면 화냅니다.',
  keyword: {
    name: K, desc: '실비아가 찻잔을 따라 준 동료 — 그 동료의 고학년이 앞당겨진다', carrier: 'hero', cap: 2, noOwner: true,
    per: [{ stat: 'ultCost', v: -0.2 }],   // 1당 붙은 동료 고학년 요구 게이지 -20%(2겹 -40% — 200% 고학년이 120%)
  },
  // 자동 규칙 둘(사용자 10-11 「20명 모두 고유 능력 2개」): 손님이 고학년을 쓰면 · 전투 시작 손님 한 명
  passives: [
    { name: '진은의 대공', when: { on: 'ult', marked: K }, fx: [bell(2), shield(1.0), guest(-2)] },   // 「초청객」 붙은 동료가 고학년 → 은방울 2 · 파티 실드 60% · 그 동료의 초청객 지움(아군 1명 = 고학년을 쓴 사도)
    { name: '어머니와 딸', when: { on: 'fightStart' }, fx: [guest(1, 'strongestAlly')] },   // 공격력이 가장 높은 동료에게 초청객 1
  ],
  ult: { name: '소녀에게 오시려고요?', cost: 200, fx: [dmg(1.5, 'allEnemies'), shield(1.5), bell(3), guest(1, 'otherAllies')] },   // 원작 고학년 — 은분수 충격파 · 은방울 보호막, 다른 아군 모두 초청객 1(원작 A2 「충격파로 모든 아군 SP 회복」)
}, {
  basicNames: ['부채 휘두르기', '부채 내려치기', '숙녀의 자세'], deck: 'heavy',
  uniques: [
    { // 시동 — 초대장: 동료 한 명을 손님으로
      name: '초대장 건네기', cost: 0, type: '스킬',
      fx: [guest(1), draw(1)],
      oracles: [
        { fx: [guest(2), draw(1)] },                                                     // 초청객 2 — 한 장으로 다 채움
        { tags: ['보존'], fx: [guest(1), draw(1)] },                                     // 쥐고 기다리기
        { fx: [{ k: 'moveMark', id: K, target: 'oneAlly' }, guest(1), draw(1)] },        // 옮겨 앉히기 — 다른 동료의 초청객을 모두 이 아군에게(흐름 바꿈)
        { fx: [guest(1), shield(0.6)] },                                                 // 실드
        { cost: 1, fx: [guest(1, 'otherAllies'), draw(2)] },                             // 1코 · 다른 아군 모두
      ],
      blesses: ['draw', 'guard', [guest(1)]],
    },
    { // 원작 강화 공격 — 가장 센 적(HP 가 가장 많은 적)을 치고 그 적 주는 피해 -40%
      name: '장갑 던지기', cost: 1, type: '공격',
      fx: [dmg(1.6, 'topEnemy'), glove],   // 10-19 밸런스(사용자 「실비아 딜 카드 계수」): 120 → 160%
      oracles: [
        { fx: [dmg(2.2, 'topEnemy'), glove] },                                                          // 수치
        { fx: [dmg(1.8, 'topEnemy'), guest(1, 'strongestAlly')] },                                      // 맞힌 뒤 가장 센 동료에게 초청객(무디게 대신 — 글 45자)
        { cost: 0, fx: [dmg(1.3, 'topEnemy'), glove] },                                                 // 0코
        { fx: [dmg(1.6, 'topEnemy'), glove, draw(1)] },                                                // 드로우
        { fx: [dmg(1.3, 'allEnemies'), { k: 'dealtMod', v: -0.2, target: 'allEnemies' }] },               // 모든 적을 무디게
      ],
      blesses: ['power', 'cost', [guest(1, 'strongestAlly')]],
    },
    { // 티파티 — 원작 「궁극의 유희」: 주변 적 받는 피해 + · 매초 회복, 대가로 실비아 SP 회복 불가(→ 게이지 정지)
      name: '궁극의 유희', cost: 2, type: '강화',
      fx: [party([soft(0.15), heal(0.25)]), lock],   // 턴 시작 적 전체 받는 피해 +15% · 회복 20%(heal 0.15 × 1.2)
      oracles: [
        { cost: 1, fx: [party([soft(0.15), heal(0.15)]), lock] },                       // 1코
        { fx: [party([soft(0.1), heal(0.1)])] },                                         // 대가 없이 · 덜 세게
        { fx: [power([rule({ on: 'ult', marked: K }, [{ k: 'takenMod', v: 0.2, turns: 2, target: 'allEnemies' }])])] },   // 손님이 고학년을 쓰면 적 전체 받는 피해 +20% 2턴(흐름 바꿈 · 대가 없음)
        { tags: ['개전'], fx: [party([soft(0.15), heal(0.15)]), lock] },                // 개전
        { fx: [party([soft(0.15), bell(1)]), lock] },                                    // 회복 대신 은방울
      ],
      blesses: ['cost', 'draw', [guest(1, 'strongestAlly')]],
    },
    { // 회복 — 「꼬마」 소리엔 보석 타르트로 입을 막는다
      name: '보석 타르트', cost: 1, type: '스킬',
      fx: [heal(2.0), guest(1)],   // 회복 120% · 초청객 1
      oracles: [
        { fx: [heal(1.2), guest(1)] },                            // 수치
        { fx: [heal(0.7), { k: 'cleanse', v: 1 }, guest(1)] },    // 해로운 효과 1 지우기
        { fx: [heal(0.5), guest(2)] },                            // 초청객 더
        { cost: 0, fx: [heal(0.6)] },                             // 0코
        { fx: [heal(0.7), status('사기', 1)] },                   // 사기
      ],
      blesses: ['heal', 'draw', [guest(1)]],
    },
    { // 마무리 — 은분수: 파티를 감싸고 고학년을 당긴다(털지 않는다 · 티파티 중에는 게이지가 막힌다)
      name: '은분수', cost: 1, type: '스킬',
      fx: [shield(1.0), bell(3), gauge(10)],   // 실드 60% · 은방울 2 · 게이지 +10%
      oracles: [
        { fx: [shield(0.5), bell(2), gauge(20)] },                                    // 게이지 더
        { fx: [shield(0.4), bell(3), gauge(10)] },                                    // 은방울 3
        { type: '공격', fx: [dmg(0.8, 'allEnemies'), bell(2)] },                      // 충격파
        { tags: ['보존'], fx: [shield(0.8), bell(2)] },                               // 보존 · 큰 실드
        { cost: 0, fx: [bell(2), draw(1)] },                                          // 0코 드로우
      ],
      blesses: ['guard', 'cost', [guest(1, 'strongestAlly')]],
    },
  ],
});
