// 네르(빡침) — 딜러 · 활발 · 요정 · 변신 「성전」. 설계: _measure/엘다인_엔진/01_사도별.md §19 (4차 10-11 — 「교단의 휘장」 카드 · 이름 정리 · 성전은 태세 · 도끼창은 성전 중에만)
// 엔진: 「교단의 휘장」 을 펼쳐 둔 적 차례에 파티가 맞을 때마다 「꼬마 여왕님」(신앙) → 신앙 5로 「성전 선포」 → 벼락(취약) → 참격 → 도끼창 순서로 몰아치고 격파마다 성전 +1턴 → 끝나면 허기
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '네르_빡침', Q = '꼬마 여왕님', F = `${H}_성전`;
const inF = { k: 'ifForm', id: F };
const tough = (v) => ({ k: 'tough', v });
const TT = ['증발', '소멸'];
const BAN = '교단의 휘장';
const ban = (v = 1) => stack(BAN, v);
const declare = (n) => [{ k: 'ifStack', id: Q, n }, { k: 'spend', id: Q, v: n }, { k: 'form', id: F }];

export default hero({
  id: H, name: '네르(빡침)', nature: '활발', race: '요정', role: '딜러', star: 3,
  blurb: '여왕님을 지키려다 끝내 폭발한 사제장. 맞아 주며 쌓은 신앙으로 성전을 선포하면 벼락 · 참격 · 도끼창이 손에 들고, 적이 무너질 때마다 성전이 길어집니다.',
  keyword: {
    name: Q, desc: '여왕님을 지키려는 사제장의 신앙', carrier: 'self', cap: 5,
  },
  keywords: [{
    // 휘장이 펼쳐진 동안(적의 차례가 끝나면 1 감소) 파티가 맞을 때마다 신앙 — 자동 규칙은 이것 하나(3차 「맞으면 신앙」 을 카드로 옮김)
    name: BAN, desc: '여왕님을 대신해 맞아 주는 교단의 휘장', carrier: 'self', cap: 2, decay: 1,
    rules: [{ name: BAN, when: { on: 'hurt', guarded: true }, fx: [stack(Q, 1)] }],
  }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '가끔은 그리운 순간', when: { on: 'formOn', id: F }, fx: [status('취약', 1, { target: 'allEnemies' })] },   // 원작 2성 — 성전에 들면 적 전체 취약
  ],
  forms: [{
    // 4차 태세(사용자 10-11): 신앙 5로 들고 · 격파마다 +1턴(최대 5)으로 지키고 · 시간이 다 되면 허기
    id: F, name: '성전', desc: '본인은 성전, 다들 빡침이라 부르는 폭주 — 네르의 공격이 거세진다', stance: true, anim: 'Idle_Change',   // 원작 변신 쉬는 동작(태세로 바꾸며 빠졌던 것 — 10-11 되살림)
    turns: 3,
    bonus: [{ type: '공격', ratio: 2, fx: [{ k: 'tough', v: 1 }] }],   // 성전 중 네르 공격 카드 피해 ×2 · 강인도 1(10-11 사용자 0.5→1)
    passives: [{ name: '새로이 쌓아갈 추억', when: { on: 'break', who: 'any' }, fx: [{ k: 'formExtend', id: F, v: 1, max: 5 }] }, { name: '심판의 벼락', when: { on: 'turnStart' }, fx: [dmg(0.8, 'allEnemies'), draw(1)] }],
    offExpire: [{ k: 'nextAp', v: -1 }, { k: 'spend', id: Q, all: true }],
  }],
  ult: { name: '교단의 심판', cost: 300, fx: [dmg(2.1, 'allEnemies'), { k: 'form', id: F }] },
}, {
  basicNames: ['도끼창 휘두르기', '적벽돌 던지기', '사제장의 망토'], deck: 'light', starter: true,
  uniques: [
    { // 시동
      name: '교주님한테서 손 떼!', cost: 1, type: '공격',
      fx: [dmg(2.6), stack(Q, 2)],
      oracles: [
        { fx: [dmg(2.7), stack(Q, 2)] },
        { fx: [dmg(1.1), stack(Q, 3)] },
        { needForm: F, fx: [dmg(1.5, 'allEnemies'), dmg(1, 'allEnemies', { ofForm: 0.1 })] },   // 거대 도끼창 — 성전 중에만 · 이번 성전 준 피해 10%(▶E76)
        { fx: [dmg(1.4), status('약화', 1), stack(Q, 2)] },
        { fx: [dmg(1.6), stack(Q, 2), { k: 'ifBreak' }, { k: 'ap', v: 1 }] },
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
    { // 0코 · 보존 — 큰 공격 예고 앞에 펼친다(결정 지점)
      name: BAN, cost: 0, type: '스킬', tags: ['보존'],
      fx: [stack(Q, 1), ban(1)],
      oracles: [
        { tags: ['보존'], fx: [stack(Q, 2), ban(1)] },
        { tags: ['보존'], fx: [stack(Q, 1), ban(1), inF, { k: 'formExtend', id: F, v: 1, max: 5 }] },
        { power: true, fx: [ban(1), power([rule({ on: 'turnStart' }, [stack(Q, 1)])])] },
        { tags: ['보존'], fx: [stack(Q, 1), ban(2)] },
        // 08 — 기본 카드를 남길 이유
        { power: true, fx: [ban(1), power([rule({ on: 'play', basic: true }, [stack(Q, 1)])])] },
      ],
      blesses: ['draw', 'ap', [stack(Q, 1)]],
    },
    {
      name: '벽돌 한 장', cost: 0, type: '공격',
      fx: [dmg(1.3), tough(1)],
      oracles: [
        { fx: [dmg(0.7), tough(2)] },
        { fx: [dmg(0.7), tough(1), { k: 'ifBreak' }, stack(Q, 2)] },
        { fx: [dmg(0.5, 'allEnemies'), tough(1)] },
        { tags: ['소멸'], fx: [dmg(0.7), tough(1), inF, { k: 'ap', v: 1 }] },
        { fx: [dmg(0.7), tough(1), { k: 'ifFoe', id: 'broken' }, dmg(2.8)] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    { // 마무리 — 신앙 +2, 5 가 되면 5 를 써서 성전(설계의 두 갈래 고르기는 45자에 안 들어 「차면 든다」 로)
      name: '성전 선포', cost: 1, type: '공격',
      fx: [dmg(1.9, 'allEnemies'), stack(Q, 2), ...declare(4)],
      oracles: [
        { type: '스킬', fx: [stack(Q, 2), { k: 'ifStack', id: Q, n: 5 }, { k: 'spend', id: Q, v: 5 }, empower(0.6, { who: 'any' })] },
        { fx: [dmg(1.4, 'allEnemies'), stack(Q, 2), ...declare(4)] },
        { fx: [dmg(1.4, 'allEnemies'), status('취약', 2, { target: 'allEnemies' }), stack(Q, 1)] },
        { fx: [dmg(1.4, 'allEnemies'), stack(Q, 2), inF, { k: 'formExtend', id: F, v: 2, max: 5 }] },
        { fx: [dmg(2.8), stack(Q, 2), ...declare(4)] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    { // 4차 8장 — 굴리기(원작 2성 「가끔은 그리운 순간」 — 휘장 동안 벼락)
      name: '가끔은 그리운 순간', cost: 1, type: '공격',
      fx: [dmg(1.1), tough(1), inF, status('취약', 1, { target: 'allEnemies' })],
      oracles: [
        { fx: [dmg(1.1), tough(1), inF, status('취약', 1, { target: 'allEnemies' })] },
        { fx: [dmg(0.8), tough(1), stack(Q, 1)] },
        { fx: [dmg(0.5, 'allEnemies'), tough(1)] },
        { fx: [dmg(0.8), status('취약', 1, { target: 'oneEnemy' })] },
        { fx: [dmg(0.8), tough(1), ban(1)] },
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
  ],
});
