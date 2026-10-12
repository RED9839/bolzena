// 4차 덧붙임(사용자 10-11): 코스트(AP) 회복 탱커 — 막아 낸 것(티파티) · 손님 카드 비용 -1 로 파티 AP 를 돌린다(우이는 넘친 회복 → AP, 리뉴아는 쥐면 싸짐).
// 실비아 — 탱커 · 광기 · 용족. 설계: _measure/엘다인_엔진/01_사도별.md §10 (4차 10-11 — 결투 + 은방울 보호막)
// 엔진: 장갑을 던져 결투 상대 한 명(「티파티」 — 그 적 주는 피해 -20%, 받는 피해 증가) → 동료가 티파티 적을 치면 실드(숙녀의 환대) · 실비아가 치면 회복(티파티의 주인)
// 2026-10-12 밤 사용자: 「티파티 초대장」(oneAlly 로 고르던 손님) 없앰 — 티파티 적은 파티 누구에게서든 더 아프게 맞고, 손님 비용 -1 은 「다음에 내는 동료 카드 비용 -1」 로.
//   「은방울 보호막」(턴 끝 실드 · 은방울) 패시브도 없애고 회복 패시브로(「실비아에게도 힐을」) — 은방울은 「은분수」 · 고학년 자원으로만.
//   → 「은분수」 로 파티에 은방울 — 실드가 깨지거나 턴이 끝나면 피해 감소. 자동 규칙은 손님 덤 하나.
// 4차 결정 지점: 「은분수」 보존 + 「공격 예고 중인 적이면 은방울 +2」 — 공격이 없는 턴엔 쥐고 넘긴다.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '실비아', D = '티파티';   // 2026-10-11 사용자: 적 표식 「어머니와 딸」 → 「티파티」
const duel = (target = 'oneEnemy') => stack(D, 1, { target });
const guest = (n = 1) => ({ k: 'costMod', v: -1, n, who: 'other', turns: 1 });   // 다음에 내는 동료 카드 비용 -1(옛 손님 카드 비용 -1 — AP 탱커)
const bell = status('은방울', 2);   // ▶E65 — 파티 상태(실드가 깨지거나 턴이 끝나면 피해 감소 2)
const atk = { k: 'ifFoe', id: 'anyAttack' };   // 공격하려는 적이 있으면
const gauge = (v, target) => ({ k: 'gauge', v, target });

export default hero({
  id: H, name: '실비아', nature: '광기', race: '용족', role: '탱커', star: 3,
  blurb: '은빛 티파티의 주인. 장갑을 던져 결투 상대를 티파티에 붙들면 그 적은 무뎌지고 파티 모두에게 더 아프게 맞습니다. 동료가 그 적을 치면 실드를, 실비아가 치면 회복을 얻습니다.',
  keyword: {
    name: D, desc: '장갑을 맞고 티파티에 붙들린 결투 상대', carrier: 'enemy', cap: 1, hunt: true,
    // 10-12 밤: 옛 「실비아에게 +30% · 초대장 손님에게 +30%」 → 누구에게서든 하나로
    per: [{ stat: 'dealt', v: -0.2 }, { stat: 'taken', v: 0.3 }],
    // 손님 덤: 동료(실비아 말고)가 티파티 적을 공격하면 실드 — 「hit」 은 카드 한 장에 한 번만 돈다(옛 「칠 때마다」 한 턴 30번 폭주 없음 · 한 턴은 낸 공격 카드 수까지)
    rules: [{ name: '숙녀의 환대', when: { on: 'hit', who: 'other', foeStack: D }, fx: [shield(0.4)] }],
  },
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    // 10-12 밤: 옛 「은방울 보호막」(턴 끝 파티 실드 · 은방울 1) → 실비아가 티파티 적을 치면 회복(카드 한 장에 한 번) — 결투의 주인이 손님(실드)과 나눠 지킨다
    { name: '티파티의 주인', when: { on: 'hit', foeStack: D }, fx: [heal(0.3)] },
  ],
  ult: { name: '소녀에게 오시려고요?', cost: 250, fx: [ddmg(1.8, 'allEnemies'), shield(4), status('은방울', 4), { k: 'ap', v: 1 }] },   /* 10-13 사용자: 2.96 으로 높음 — AP +2 → +1 */   // 10-12 고학년 손질: 실드 250→400% · 은방울 2→4 · 게이지 10 → AP 2(AP 탱커)   // 10-12 사용자: 비용 300 고학년은 모두 250 으로
}, {
  basicNames: ['부채 휘두르기', '부채 내려치기', '숙녀의 자세'], deck: 'heavy',
  uniques: [
    { // 시동
      name: '장갑 던지기', cost: 1, type: '공격',
      fx: [ddmg(1.1), duel(), shield(0.5)],   // 10-11 사용자 「방어 만들 카드 부족」 — 실드 50%
      oracles: [
        { fx: [ddmg(1.6), duel(), shield(0.5)] },   // 10-13 신탁 점검: 실드 되살림(탱커 카드)
        { type: '스킬', cost: 0, fx: [duel(), shield(0.5)] },   // 10-13 신탁 점검: 실드 되살림
        { fx: [ddmg(1.1), duel(), status('취약', 2, { target: 'oneEnemy' })] },
        { tags: ['회수 3'], fx: [ddmg(1.1), duel(), shield(0.5)] },   // 10-13 신탁 점검: 실드 되살림
        { fx: [duel('topEnemy'), ddmg(2, 'markedEnemy', { id: D })] },   // 10-12 카드 글: 티파티를 먼저 걸고 그 적을 친다
      ],
      blesses: ['power', 'draw', [duel()]],
    },
    { // 10-12 밤: 옛 「초대장 건네기」(동료 한 명에게 초대장 · 손님 카드 비용 -1) → 고를 것 없이 다음 동료 카드 비용 -1
      name: '초대장 건네기', cost: 0, type: '스킬',
      fx: [guest(), shield(1.2)],   // 10-11 실드 80→120%
      oracles: [
        { fx: [guest(), draw(1), shield(1)] },
        { fx: [duel(), guest()] },   // 옛 「다른 아군 모두에게 초대장」 → 티파티 걸기 · 비용 -1
        { fx: [guest(), shield(1.2), gauge(20, 'allAllies')] },   // 옛 「아군 1명 게이지 30」 → 파티 게이지 10
        { fx: [guest(), shield(0.8), empower(0.4, { who: 'any' })] },   // 10-13 신탁 점검: 실드 되살림
        { fx: [shield(0.8), empower(1)] },
      ],
      blesses: ['draw', 'guard', [guest()]],
    },
    {
      name: '궁극의 유희', cost: 2, type: '강화',
      // 티파티(사용자 10-11 — 코스트 회복 탱커): 지난 판 막아 낸 피해가 100 이상이면 내 턴 시작 AP +1 · 대가로 실비아 게이지 정지(원작 SP 회복 못 함)
      fx: [power([rule({ on: 'guardSum', n: 100 }, [{ k: 'ap', v: 1 }])]), { k: 'gaugeLock' }],   // 10-12 밤: 옛 「파티 모두에게 초대장」 뺌
      oracles: [
        { fx: [power([rule({ on: 'guardSum', n: 60 }, [{ k: 'ap', v: 1 }])]), { k: 'gaugeLock' }] },
        { cost: 1, fx: [power([rule({ on: 'guardSum', n: 100 }, [{ k: 'ap', v: 1 }])])] },
        { fx: [power([rule({ on: 'guardSum', n: 100 }, [{ k: 'ap', v: 1 }])]), { k: 'gaugeLock' }, status('취약', 2, { target: 'allEnemies' })] },   // 10-13 신탁 점검
        { fx: [{ k: 'gaugeLock' }, power([rule({ on: 'turnStart' }, [shield(0.8)])])] },
        { tags: ['개전'], fx: [power([rule({ on: 'guardSum', n: 80 }, [{ k: 'ap', v: 1 }])]), { k: 'gaugeLock' }] },   // 10-13 신탁 점검: 100→80
      ],
      blesses: ['cost', 'draw', [duel()]],
    },
    { // 마무리 — 보존: 적이 칠 턴에 내면 은방울이 더(결정 지점)
      name: '은분수', cost: 2, type: '공격', tags: ['보존'],
      fx: [ddmg(0.6, 'allEnemies'), shield(1.2), atk, status('은방울', 4)],   // 은방울은 적이 칠 턴에만(쥘 이유)
      oracles: [
        { power: true, fx: [shield(1.2), bell, power([rule({ on: 'turnEnd' }, [bell])])] },   // 10-13 신탁 점검: 실드 140% · 은방울 바로 2
        { tags: ['보존'], fx: [ddmg(4, 'markedEnemy', { id: D }), atk, bell] },   // 10-13 신탁 점검: 280→340%
        { tags: ['보존'], fx: [ddmg(1, 'allEnemies'), shield(1.2), atk, status('은방울', 3)] },   // 10-13 신탁 점검: 게이지 대신 실드 되살림
        { type: '스킬', cost: 1, tags: ['보존'], fx: [shield(1.6), bell, { k: 'nextAp', v: 1 }] },   // 10-13 신탁 점검
        { type: '스킬', tags: ['보존'], fx: [shield(1.8), status('피해 감소', 3)] },   // 10-13 신탁 점검
      ],
      blesses: ['guard', 'cost', [duel()]],
    },
    { // 4차 8장 — 터뜨리기(탱커 메인딜 갈래 · 원작 강화 공격 — 가장 센 적)
      name: '숙녀의 결투', cost: 2, type: '공격',
      fx: [ddmg(1.2, 'markedEnemy', { id: D }), shield(1.2)],   // 10-11 실드 60→120%
      oracles: [
        { fx: [ddmg(2, 'markedEnemy', { id: D }), shield(0.6)] },
        { fx: [duel('topEnemy'), ddmg(1.6, 'markedEnemy', { id: D })] },
        { fx: [ddmg(1.9, 'markedEnemy', { id: D }), guest()] },   // 10-13 신탁 점검: 160→190%   // 10-12 밤: 옛 초대장 → 다음 동료 카드 비용 -1
        { fx: [ddmg(1.6, 'markedEnemy', { id: D }), shield(0.8), status('취약', 1, { target: 'oneEnemy' })] },   // 10-13 신탁 점검: 실드 되살림
        { cost: 1, fx: [ddmg(1.2, 'markedEnemy', { id: D }), shield(0.8)] },   // 10-13 신탁 점검: 실드 되살림
      ],
      blesses: ['power', 'cost', [duel()]],
    },
  ],
});
