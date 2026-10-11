// 4차 덧붙임(사용자 10-11): 코스트(AP) 회복 탱커 — 막아 낸 것(티파티) · 손님 카드 비용 -1 로 파티 AP 를 돌린다(우이는 넘친 회복 → AP, 리뉴아는 쥐면 싸짐).
// 실비아 — 탱커 · 광기 · 용족. 설계: _measure/엘다인_엔진/01_사도별.md §10 (4차 10-11 — 결투 + 은방울 보호막)
// 엔진: 장갑을 던져 결투 상대 한 명(「어머니와 딸」 — 그 적 주는 피해 -20%, 실비아에게 받는 피해 +50%) → 손님(「티파티 초대장」)이 티파티 적을 치면 피해 증가 · 실비아 실드(★)
//   → 「은분수」 로 파티에 은방울 — 실드가 깨지거나 턴이 끝나면 피해 감소. 자동 규칙은 손님 덤 하나.
// 4차 결정 지점: 「은분수」 보존 + 「공격 예고 중인 적이면 은방울 +2」 — 공격이 없는 턴엔 쥐고 넘긴다.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '실비아', D = '티파티', G = '티파티 초대장';   // 2026-10-11 사용자: 적 표식 「어머니와 딸」 → 「티파티」   // 2026-10-11 사용자: 「그 어머니에 그 딸」 → 「티파티 초대장」
const duel = (target = 'oneEnemy') => stack(D, 1, { target });
const invite = (target = 'oneAlly') => stack(G, 1, { target });
const bell = status('은방울', 2);   // ▶E65 — 파티 상태(실드가 깨지거나 턴이 끝나면 피해 감소 2)
const atk = { k: 'ifFoe', id: 'anyAttack' };   // 공격하려는 적이 있으면
const gauge = (v, target) => ({ k: 'gauge', v, target });

export default hero({
  id: H, name: '실비아', nature: '광기', race: '용족', role: '탱커', star: 3,
  blurb: '은빛 티파티의 주인. 장갑을 던져 결투 상대를 티파티에 붙들면 그 적은 무뎌지고, 초대장을 받은 손님들이 그 적을 더 아프게 칩니다.',
  keyword: {
    name: D, desc: '장갑을 맞고 티파티에 붙들린 결투 상대', carrier: 'enemy', cap: 1, hunt: true,
    // 10-11 사용자: 「티파티 초대장」 이 있는 아군이 「티파티」 적을 치면 피해 증가(옛: 초대장 붙은 아군 카드가 치면 실비아가 거듦)
    per: [{ stat: 'dealt', v: -0.2 }, { stat: 'taken', v: 0.3, from: 'owner' }, { stat: 'taken', v: 0.3, from: 'marked', mark: G }],
  },
  keywords: [{
    name: G, desc: '실비아의 티파티에 초대받은 손님의 초대장', carrier: 'hero', cap: 1,
    rules: [{ name: '숙녀의 환대', when: { on: 'play', marked: G, type: '공격', token: false }, fx: [shield(0.3)] }],   // 10-11: 손님이 공격 카드를 낼 때마다 실드 30%(옛 「칠 때」 는 한 턴 30번 넘게 터져 멈춤 상한에 걸림)
  }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '은방울 보호막', when: { on: 'fightStart' }, fx: [status('은방울', 2)] },   // 전투 시작 은방울 2
  ],
  ult: { name: '소녀에게 오시려고요?', cost: 300, fx: [ddmg(1.8, 'allEnemies'), shield(2.5), bell, gauge(10, 'allAllies')] },
}, {
  basicNames: ['부채 휘두르기', '부채 내려치기', '숙녀의 자세'], deck: 'heavy',
  uniques: [
    { // 시동
      name: '장갑 던지기', cost: 1, type: '공격',
      fx: [ddmg(1.1), duel(), shield(0.5)],   // 10-11 사용자 「방어 만들 카드 부족」 — 실드 50%
      oracles: [
        { fx: [ddmg(1.6), duel()] },
        { type: '스킬', cost: 0, fx: [duel()] },
        { fx: [ddmg(1.1), duel(), status('취약', 2, { target: 'oneEnemy' })] },
        { tags: ['회수 3'], fx: [ddmg(1.1), duel()] },
        { fx: [ddmg(2, 'topEnemy'), duel('topEnemy')] },
      ],
      blesses: ['power', 'draw', [duel()]],
    },
    {
      name: '초대장 건네기', cost: 0, type: '스킬',
      fx: [invite(), { k: 'costMod', v: -1, n: 1, who: 'other', turns: 1 }, shield(1.2)],   // 10-11 실드 80→120%   // 손님 카드 비용 -1(동료 AP)
      oracles: [
        { fx: [invite(), draw(1), shield(1.4)] },
        { fx: [invite('otherAllies')] },
        { fx: [invite(), draw(1), gauge(30, 'oneAlly')] },
        { fx: [invite(), empower(0.4, { who: 'any' })] },
        { fx: [shield(0.8), empower(1)] },
      ],
      blesses: ['draw', 'guard', [invite()]],
    },
    {
      name: '궁극의 유희', cost: 2, type: '강화',
      // 티파티(사용자 10-11 — 코스트 회복 탱커): 지난 판 막아 낸 피해가 100 이상이면 내 턴 시작 AP +1 · 대가로 실비아 게이지 정지(원작 SP 회복 못 함)
      fx: [invite('allAllies'), power([rule({ on: 'guardSum', n: 150 }, [{ k: 'ap', v: 1 }])]), { k: 'gaugeLock' }],
      oracles: [
        { fx: [invite('allAllies'), power([rule({ on: 'guardSum', n: 60 }, [{ k: 'ap', v: 1 }])]), { k: 'gaugeLock' }] },
        { cost: 1, fx: [invite('otherAllies'), power([rule({ on: 'guardSum', n: 150 }, [{ k: 'ap', v: 1 }])])] },
        { fx: [invite('allAllies'), power([rule({ on: 'guardSum', n: 150 }, [{ k: 'ap', v: 1 }])]), status('취약', 1, { target: 'allEnemies' })] },
        { fx: [invite('allAllies'), { k: 'gaugeLock' }, power([rule({ on: 'turnStart' }, [shield(0.8)])])] },
        { tags: ['개전'], fx: [power([rule({ on: 'guardSum', n: 150 }, [{ k: 'ap', v: 1 }])]), { k: 'gaugeLock' }] },
      ],
      blesses: ['cost', 'draw', [duel()]],
    },
    { // 마무리 — 보존: 적이 칠 턴에 내면 은방울이 더(결정 지점)
      name: '은분수', cost: 2, type: '공격', tags: ['보존'],
      fx: [ddmg(0.6, 'allEnemies'), shield(1.2), atk, status('은방울', 4)],   // 은방울은 적이 칠 턴에만(쥘 이유)
      oracles: [
        { power: true, fx: [shield(1), power([rule({ on: 'turnEnd' }, [bell])])] },
        { tags: ['보존'], fx: [ddmg(2.8, 'markedEnemy', { id: D }), atk, bell] },   // 10-11 「「어머니와 딸」이 가장 많은 적」 글이 길어 실드 100% 를 빼고 피해 250→280%
        { tags: ['보존'], fx: [ddmg(1, 'allEnemies'), gauge(10, 'allAllies'), atk, status('은방울', 3)] },
        { type: '스킬', cost: 1, tags: ['보존'], fx: [shield(1.2), bell, { k: 'nextAp', v: 1 }] },
        { type: '스킬', tags: ['보존'], fx: [shield(1.5), status('피해 감소', 3)] },
      ],
      blesses: ['guard', 'cost', [duel()]],
    },
    { // 4차 8장 — 터뜨리기(탱커 메인딜 갈래 · 원작 강화 공격 — 가장 센 적)
      name: '숙녀의 결투', cost: 2, type: '공격',
      fx: [ddmg(1.2, 'markedEnemy', { id: D }), shield(1.2)],   // 10-11 실드 60→120%
      oracles: [
        { fx: [ddmg(2, 'markedEnemy', { id: D }), shield(0.6)] },
        { fx: [ddmg(1.6, 'topEnemy'), duel('topEnemy')] },
        { fx: [ddmg(1.6, 'markedEnemy', { id: D }), invite()] },
        { fx: [ddmg(1.6, 'markedEnemy', { id: D }), status('취약', 1, { target: 'oneEnemy' })] },
        { cost: 1, fx: [ddmg(1.2, 'markedEnemy', { id: D })] },
      ],
      blesses: ['power', 'cost', [duel()]],
    },
  ],
});
