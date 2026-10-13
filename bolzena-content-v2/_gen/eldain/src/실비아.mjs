// 실비아 — 서포터 · 광기 · 용족.
// 2026-10-14 리뉴얼(사용자 「은빛 티파티 주최」 — 탱커에서 서포터로):
//   동료가 스킬 카드를 낼 때마다 「찻잔」(최대 5)이 찬다(초청객). 쥐고 있으면 1개당 파티 받는 피해 -4%(가득이면 -20% · 턴 시작 실드).
//   찻잔은 저절로 터지지 않는다 — 「은분수」 로 모두 털어 1개당 파티 실드 · 3개 이상이면 AP. 털면 피해 감소도 사라진다 — 쥘까, 털까.
//   「궁극의 유희」(강화) = 티파티를 연다: 동료 스킬마다 무작위 적 추가 피해, 대가로 실비아 카드는 고학년 게이지를 채우지 않는다(원작 SP 회복 불가).
//   「장갑 던지기」 = 원작 강화 공격(가장 센 적 — HP 가 가장 많은 적 — 을 치고 그 적 주는 피해 -40%).
//   원작: 은의 용족 · 비비의 양녀 · 진은의 대공 · 티타임 취미 · 「꼬마」 소리에 발끈(보석 타르트로 달랜다).
// 없앤 것: 적 표식 「티파티」(주는 피해 -20% · 받는 피해 +30%) · 「숙녀의 환대」(동료가 티파티 적을 치면 실드) · 「티파티의 주인」(실비아가 치면 회복) ·
//   「막아 낸 피해 100 이면 턴 시작 AP」(guardSum) · 고유 카드 「숙녀의 결투」. 이전 판(탱커 · 혼자 88.2%): C:\projects\_backup\silvia_before_20261014
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '실비아', K = '찻잔';
const cup = (v = 1, x) => stack(K, v, x);
const guest = (n = 1) => ({ k: 'costMod', v: -1, n, who: 'other', turns: 1 });   // 다음에 내는 동료 카드 비용 -1(n 장)
const bell = (v = 2) => status('은방울', v);   // ▶E65 — 파티 상태(실드가 깨지거나 턴이 끝나면 피해 감소)
const glove = { k: 'dealtMod', v: -0.4, target: 'topEnemy' };   // 가장 센 적 주는 피해 -40%(1턴)
const pour = [spend(K, 'all'), { k: 'perEvent' }];   // 찻잔을 모두 털어 1개당
const atLeast = (n) => ({ k: 'ifEvent', n });   // 턴 찻잔이 n 개 이상이었으면
const host = (fx, on = '스킬') => power([rule({ on: 'play', who: 'other', type: on }, fx)]);   // 티파티 — 동료가 그 카드를 낼 때마다
const lock = { k: 'gaugeLock' };   // 대가 — 실비아 카드로는 고학년 게이지가 오르지 않는다

export default hero({
  id: H, name: '실비아', nature: '광기', race: '용족', role: '서포터', star: 3,
  blurb: '은빛 티파티를 여는 진은의 대공. 동료가 스킬을 쓸 때마다 찻잔이 차오르고, 찻잔이 찬 동안 파티는 덜 아프게 맞습니다. 은분수로 찻잔을 털면 파티를 감싸고 숨을 돌려 줍니다. 꼬마라고 부르면 화냅니다.',
  keyword: {
    name: K, desc: '초청객이 다녀갈 때마다 채워지는 찻잔 — 은분수로 털어 쓴다', carrier: 'self', cap: 5, per: [{ stat: 'taken', v: -0.04, who: 'allies' }],   // 세기 맞춤(10-14 혼자 측정): 덤 없이 52% · -4% 68% · -6% 79%(최대 5 라 -5% 는 % 단위 규칙에 걸림)
    rules: [{ name: '초청객', when: { on: 'play', who: 'other', type: '스킬' }, fx: [cup(1)] }],   // 동료 스킬 카드마다 찻잔 1(네르는 공격 기준 — 실비아는 스킬)
  },
  // 사용자 10-11: 20명 모두 고유 능력 2개 — 「주최자의 대가」(게이지 정지)는 강화 카드 「궁극의 유희」 효과로 두고(늘 켜진 대가는 패시브로 못 둠), 패시브 칸은 턴 시작 찻잔
  passives: [
    { name: '진은의 대공', when: { on: 'turnStart' }, fx: [cup(1), ifStack(K, 5), shield(0.5)] },   // 턴 시작 찻잔 1 · 가득이면 파티 실드 60%(세기 맞춤 10-14 혼자 — 실드 없이 68% · 60% 면 76% · 100~120% 면 78%)
  ],
  ult: { name: '소녀에게 오시려고요?', cost: 200, fx: [dmg(1.5, 'allEnemies'), shield(1.5), bell(3), cup(5)] },   // 원작 고학년 — 은분수 충격파 · 은방울 보호막, 찻잔 가득
}, {
  basicNames: ['부채 휘두르기', '부채 내려치기', '숙녀의 자세'], deck: 'heavy',
  uniques: [
    { // 시동 — 초대장: 동료 비용 -1 · 찻잔
      name: '초대장 건네기', cost: 0, type: '스킬',
      fx: [guest(), cup(1)],
      oracles: [
        { fx: [guest(), cup(2)] },                                   // 찻잔 더
        { fx: [guest(), cup(1), draw(1)] },                          // 드로우
        { fx: [guest(2), cup(1), draw(1)] },                              // 동료 카드 두 장 -1
        { fx: [guest(), cup(1), empower(0.3, { who: 'other' })] },  // 동료 다음 카드 +30%
        { tags: ['보존'], fx: [guest(), cup(1), shield(0.6)] },      // 쥐고 기다리기 · 실드
      ],
      blesses: ['draw', 'guard', [cup(1)]],
    },
    { // 원작 강화 공격 — 가장 센 적(HP 가 가장 많은 적)을 치고 그 적 주는 피해 -40%
      name: '장갑 던지기', cost: 1, type: '공격',
      fx: [dmg(1.2, 'topEnemy'), glove],
      oracles: [
        { fx: [dmg(1.8, 'topEnemy'), glove] },                                                          // 수치
        { fx: [dmg(1.2, 'topEnemy'), glove, cup(1)] },                                                  // 찻잔
        { cost: 0, fx: [dmg(0.8, 'topEnemy'), glove] },                                                 // 0코
        { fx: [dmg(1.2, 'topEnemy'), glove, draw(1)] },                                                // 드로우
        { fx: [dmg(1, 'allEnemies'), { k: 'dealtMod', v: -0.2, target: 'allEnemies' }] },               // 모든 적을 무디게
      ],
      blesses: ['power', 'cost', [cup(1)]],
    },
    { // 티파티 — 원작 「궁극의 유희」: 초청객 스킬이 적중하면 추가 피해, 대가로 실비아 SP 회복 불가(→ 게이지 잠금)
      name: '궁극의 유희', cost: 2, type: '강화',
      fx: [host([dmg(0.3, 'randomEnemy')]), lock],
      oracles: [
        { cost: 1, fx: [host([dmg(0.3, 'randomEnemy')]), lock] },                 // 1코
        { fx: [host([dmg(0.2, 'randomEnemy')])] },                               // 대가 없이 · 덜 세게
        { fx: [host([cup(1)]), lock] },                                             // 찻잔이 두 배로
        { tags: ['개전'], fx: [host([dmg(0.3, 'randomEnemy')]), lock] },           // 개전
        { fx: [host([dmg(0.4, 'randomEnemy')], '공격'), lock] },                   // 공격 카드 손님
      ],
      blesses: ['cost', 'draw', [cup(1)]],
    },
    { // 회복 — 「꼬마」 소리엔 보석 타르트로 입을 막는다
      name: '보석 타르트', cost: 1, type: '스킬',
      fx: [heal(1.4), cup(1)],
      oracles: [
        { fx: [heal(1.6), cup(1)] },                              // 수치
        { fx: [heal(0.8), { k: 'cleanse', v: 1 }, cup(1)] },      // 해로운 효과 1 지우기
        { fx: [heal(0.6), cup(2)] },                              // 찻잔 더
        { cost: 0, fx: [heal(0.7)] },                             // 0코
        { fx: [heal(0.8), status('사기', 1)] },                   // 사기
      ],
      blesses: ['heal', 'draw', [cup(1)]],
    },
    { // 마무리 — 찻잔을 모두 털어 1개당 실드 · 3개 이상이면 AP(자동 발동 없음 · 은방울은 신탁 · 고학년)
      name: '은분수', cost: 1, type: '스킬',
      fx: [...pour, shield(0.6), atLeast(3), { k: 'ap', v: 1 }],
      oracles: [
        { fx: [...pour, shield(0.4), bell(3)] },                                                       // 은방울
        { fx: [...pour, heal(0.4), atLeast(3), draw(2)] },                                            // 회복 · 드로우
        { type: '공격', fx: [...pour, dmg(0.5, 'allEnemies'), bell(2)] },                              // 충격파
        { tags: ['보존'], fx: [...pour, shield(0.3), atLeast(5), { k: 'ap', v: 2 }] },                 // 가득 채워 AP 2
        { cost: 0, fx: [spend(K, 2), atLeast(2), draw(2)] },                                           // 두 잔만 · 0코 드로우
      ],
      blesses: ['guard', 'cost', [cup(1)]],
    },
  ],
});
