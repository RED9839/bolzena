// 클로에 — 탱커 · 광기 · 요정. 설계: _measure/엘다인_엔진/01_사도별.md §6 (4차 10-11)
// 4차 덧붙임(사용자 10-11): 실드를 주는 생성 카드 「맞춤옷」 — 동료 몫으로 지어 주고(그 동료 방어력 실드), 누가 입든 세바스티안 +1.
// 엔진: 꿰맬수록 곰 인형 집사 「세바스티안」 몸집이 커져 대신 맞고 · 따라 치고 → 몸집을 풀어 손의 동료 카드에 「맞춤 의상」(탐구심 · 보존)을 입힌다.
// 쁘띠 세바스티안 생성 카드는 뺐다(사용자 10-11). 자동 규칙은 「인형의 의지」 하나. 세바스티안은 적의 공격을 대신 받는 소환물이라 따로 도발을 두지 않는다.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '클로에', K = '세바스티안', D = '탐구심';
const S = (v = 1, x) => stack(K, v, x);
const order = (ratio, target = 'oneEnemy') => ({ k: 'summon', id: K, ratio, target });
const T = `${H}_t1`;
const suit = (v = 1, owner = 'other') => make(T, v, { owner });   // 「맞춤옷」 — 동료 몫으로 지어 줌(사용자 10-11)
const dress = (v = 1, x) => ({ k: 'cardStatus', id: D, v, to: 'hand', n: 1, who: 'other', keep: true, ...x });
const pickDress = (x) => ({ k: 'cardStatus', id: D, v: 1, to: 'hand', who: 'other', keep: true, pick: true, ...x });

export default hero({
  id: H, name: '클로에', nature: '광기', race: '요정', role: '탱커', star: 3,
  blurb: '날개 없는 요정 재단사. 실을 꿰맬수록 곰 인형 집사 세바스티안이 커져 동료 대신 맞고, 명령하면 몸집만큼 따라 칩니다. 천을 풀면 동료의 카드에 맞춤 의상을 지어 줍니다.',
  keyword: {
    name: K, desc: '클로에의 힘을 넘겨받은 곰 인형 집사', carrier: 'self', cap: 5, guard: true, cut: 0.6,
    // 자동 규칙 하나 — 누구든 「맞춤옷」 을 입으면(내면) 세바스티안 +1(재단사 ↔ 집사 고리). 옛 「인형의 의지」(넘치면 맞춤 의상)는 뺐다
    rules: [{ name: '맞춤 가봉', when: { on: 'play', who: 'any', tag: T }, fx: [S(1)] }],
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '바느질', desc: '꿰맬수록 단단해지는 재단사의 솜씨 — 받는 피해가 줄어든다', carrier: 'self', cap: 4, per: [{ stat: 'taken', v: -0.05 }] }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '인형의 의지', when: { on: 'turnStart' }, conds: [{ c: 'stack', id: K, n: 2 }], fx: [status('피해 감소', 1)] },   // 세바스티안이 2 이상이면 매 턴 피해 감소 1(뎀감 정체성)
  ],
  ult: { name: 'F/W 클로에록', cost: 300, fx: [ddmg(1.2, 'allEnemies'), S(3), status('피해 감소', 2)] },
}, {
  basicNames: ['곰 인형 휘두르기', '곰 인형 찌르기', '재단사의 자세'], deck: 'light',
  uniques: [
    { // 시동
      name: '메리 고 라운드', cost: 1, type: '스킬',
      fx: [status('피해 감소', 1), S(2)],   // 클로에 = 피해 감소(사용자 10-11 — 실드 정체성은 이드)
      oracles: [
        { fx: [status('피해 감소', 1), S(3)] },
        { fx: [S(2), empower(0.5)] },
        { power: true, fx: [S(1), power([rule({ on: 'turnStart' }, [S(1)])])] },
        { fx: [S(1), suit(1)] },
        { cost: 2, fx: [status('피해 감소', 2), S(3)] },
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    {
      name: '찢긴 곳 꿰매기', cost: 0, type: '스킬',
      fx: [discard(1), S(1), stack('바느질', 1)],
      oracles: [
        { fx: [discard(1), S(2)] },
        { fx: [discard(1, { who: 'other' }), S(2)] },
        { fx: [empower(0.5), S(1)] },
        { cost: 1, fx: [discard(2), S(2), draw(1)] },
        { fx: [{ k: 'pull', from: 'draw', n: 1, who: 'other' }, S(1)] },
      ],
      blesses: ['draw', 'ap', [S(1)]],
    },
    {
      name: '세바스티안, 공격!', cost: 1, type: '공격',
      fx: [ddmg(1.2), order(0.7)],
      oracles: [
        { fx: [ddmg(0.8), order(0.6)] },
        { fx: [ddmg(0.8), order(0.2, 'allEnemies')] },
        { fx: [order(0.4), suit(1)] },
        { cost: 0, fx: [{ k: 'ifStack', id: K, n: 3 }, order(0.4)] },
        { fx: [order(0.4), S(1)] },
      ],
      blesses: ['power', 'cost', [S(1)]],
    },
    { // 마무리 — 세바스티안을 고른 만큼 풀어 1개당 「맞춤옷」(동료 몫, 최소 1)
      name: '런웨이 오프닝', cost: 1, type: '스킬',
      fx: [spend(K, 'pick'), { k: 'perEvent', n: 1 }, suit(1), draw(1)],
      oracles: [
        { power: true, fx: [suit(1), power([rule({ on: 'turnStart' }, [suit(1)])])] },
        { fx: [spend(K, 'pick'), pickDress(), draw(1)] },   // 옛 「맞춤 의상」(동료 카드에 탐구심 · 보존)은 이 갈래로
        { fx: [spend(K, 'pick'), { k: 'perEvent', n: 1 }, suit(1, 'self'), draw(1)] },
        { cost: 0, tags: ['소멸'], fx: [spend(K, 'pick'), pickDress()] },
        { fx: [spend(K, 'pick'), { k: 'cardStatus', id: '비용', v: -1, to: 'hand', who: 'other', pick: true }] },
      ],
      blesses: ['cost', 'draw', [S(1)]],
    },
    { // 4차 8장 — 터뜨리기(탱커 메인딜 갈래 · 원작 강화 공격 내려찍기)
      name: '곰 인형 내려찍기', cost: 2, type: '공격',
      fx: [ddmg(2.4), { k: 'perStack', id: K }, ddmg(0.3)],
      oracles: [
        { fx: [ddmg(1.4), { k: 'perStack', id: K }, ddmg(0.4)] },
        { fx: [ddmg(0.8, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.2, 'allEnemies')] },
        { fx: [ddmg(1.4), S(2)] },
        { cost: 1, fx: [ddmg(1), { k: 'perStack', id: K }, ddmg(0.2)] },
        { fx: [ddmg(1.4), status('피해 감소', 1)] },
      ],
      blesses: ['power', 'cost', [S(1)]],
    },
  ],
  tokens: [
    { id: T, name: '맞춤옷', cost: 0, type: '스킬', tags: ['소멸'], fx: [shield(0.6)], blurb: '클로에가 치수를 재어 지어 준 옷' },
  ],
});
