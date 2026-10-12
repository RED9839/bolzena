// 아야 — 딜러 · 냉정 · 마녀. 설계: _measure/엘다인_엔진/01_사도별.md §3 (아야 6차 — 격파를 만드는 딜러, 사용자 10-11)
// 엔진: 디버프(냉정 약점 = 동상 · 약화 · 행동 늦춤)를 걸고 → 약점 노출(아야 소유)로 파티 카드를 약점 공격으로 만들고 →
//   「서리」(적 표식 · 1개당 그 적이 받는 강인도 피해 +5%)을 쌓아 강인도를 깎아 격파를 만든다 → 「빙설화도」 는 강인도가 낮은 적에게 크게.
//   격파 보상(붕괴)은 네르 소유라 아야는 「격파를 만드는」 쪽 — 네르와 짝. 약화는 걸어 주기만(란 고리) · 둔화는 쓰지 않음(요미 소유 — 행동 늦춤으로). 자동 규칙 0.
//   현자 결은 이름 · 소개에만(만년설의 현자). 「만년설의 앎」(learn · known)은 데이터에서 뺐다(엔진 기능은 남음).
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '아야', K = '서리';
const frost = (v = 1, target = 'oneEnemy') => stack(K, v, { target });
const tough = (v, target) => ({ k: 'tough', v, ...(target ? { target } : {}) });
const expose = (v = 1, target = 'oneEnemy') => status('약점 노출', v, { target });
const chill = (v = 1, target = 'oneEnemy') => status('냉정 약점', v, { target });   // 원작 동상
const weak = (v = 1, target = 'oneEnemy') => status('약화', v, { target });           // 원작 싸락나비 — 걸어 주기만(란 정산)
const debuffed = { k: 'ifDebuffs', n: 1 };                                            // 디버프가 걸린 적이면
const lowT = (pct = 0.5) => ({ k: 'ifFoe', id: 'tough', pct });                       // 강인도가 pct 이하인 적이면

export default hero({
  id: H, name: '아야', nature: '냉정', race: '마녀', role: '딜러', star: 3,
  blurb: '만년설의 현자, 영원살이 맏이. 서리와 동상으로 적을 얼려 약점을 드러내고, 금이 간 자리를 빙설화도로 깨뜨립니다.',
  keyword: {
    name: K, desc: '아야가 새긴 서리 — 강인도가 잘 깎임', carrier: 'enemy', cap: 5,
    per: [{ stat: 'tough', v: 0.05 }],
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '만년설', desc: '현자가 쌓아 온 만년설', carrier: 'self', cap: 3 }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '만년설의 현자님!', when: { on: 'fightStart' }, fx: [draw(1)] },   // 현자 — 전투 시작 드로우 1
    { name: '눈꽃의 날개짓', when: { on: 'debuff' }, fx: [frost(1)] },   // 아야가 디버프를 걸면 그 적 서리 +1
  ],
  ult: { name: '만개설화', cost: 200, fx: [dmg(1.6, 'allEnemies'), chill(1, 'allEnemies'), tough(1, 'allEnemies'), { k: 'ifFoe', id: 'alone' }, dmg(1.5, 'allEnemies')] },
}, {
  basicNames: ['서리 베기', '서리 베기', '은빛 양산'], deck: 'light',
  uniques: [
    { // 시동 — 싸락나비: 치고 약화 · 서리
      name: '싸락나비', cost: 1, type: '공격',
      fx: [dmg(0.4, 'oneEnemy', { hits: 2 }), weak(1), frost(1)],   // 10-11 사용자 「아야 너무 약함」 30→50%
      oracles: [
        { fx: [dmg(0.7, 'oneEnemy', { hits: 2 }), weak(1), frost(1)] },
        { fx: [dmg(0.5, 'randomEnemy', { hits: 3, hitFx: [frost(1)] }), weak(1)] },   // 10-13 신탁 점검: 50% · 약화 되살림
        { fx: [dmg(0.5, 'oneEnemy', { hits: 2 }), weak(1, 'allEnemies')] },
        { fx: [dmg(0.5, 'oneEnemy', { hits: 2 }), weak(1), frost(2)] },   // 10-13 신탁 점검: 약화 되살림
        { fx: [dmg(0.5, 'oneEnemy', { hits: 2 }), chill(1), tough(1)] },
      ],
      blesses: ['power', 'draw', [frost(1)]],
    },
    { // 원작 「차분해질 시간이야」 — 동상 · 행동 늦춤
      name: '차분해질 시간이야', cost: 0, type: '스킬',
      fx: [chill(1), { k: 'rushDown', v: 1, target: 'oneEnemy' }, stack('만년설', 1)],
      oracles: [
        { fx: [chill(2), { k: 'rushDown', v: 1, target: 'oneEnemy' }, frost(1)] },
        { fx: [chill(1, 'allEnemies'), frost(1, 'allEnemies'), stack('만년설', 1)] },   // 10-13 신탁 점검: 만년설 되살림
        { fx: [chill(1), frost(1), draw(1)] },
        { fx: [{ k: 'rushDown', v: 1, target: 'allEnemies' }, frost(1), stack('만년설', 1)] },   // 10-13 신탁 점검: 만년설 되살림
        { fx: [chill(1), { k: 'rushDown', v: 1, target: 'oneEnemy' }, draw(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」 · 10-13 신탁 점검: 신탁 3 과 같은 글이라 늦춤 · 드로우로
      ],
      blesses: ['draw', 'cost', [frost(1)]],
    },
    { // 약점 노출(아야 소유) — 디버프가 걸린 적이면 강인도도
      name: '간파', cost: 0, type: '공격',
      fx: [dmg(0.5), expose(1), debuffed, tough(1)],   // 10-11 30→60%
      oracles: [
        { fx: [dmg(1), expose(1), debuffed, tough(1)] },
        { fx: [dmg(0.8), expose(2)] },
        { fx: [dmg(0.7, 'allEnemies'), expose(1, 'allEnemies'), tough(1, 'allEnemies')] },   // 10-13 신탁 점검: 70% · 강인도 되살림
        { fx: [dmg(0.8), expose(1), debuffed, tough(2)] },
        { fx: [dmg(0.8), expose(1), draw(1)] },
      ],
      blesses: ['power', 'draw', [frost(1)]],
    },
    { // 강인도 깎기 — 서리가 쌓인 적은 더 깎인다
      name: '얼음 결정 깨기', cost: 1, type: '공격', tags: ['약점 공격'],
      fx: [dmg(0.9), { k: 'perStack', id: '만년설' }, tough(1), stack('만년설', 1)],   // 10-11 60→110%   // 만년설 1당 강인도 피해 +1(10-11 사용자 0.5→1)
      oracles: [
        { tags: ['약점 공격'], fx: [dmg(1.2), tough(1), { k: 'ifStack', id: K, n: 2 }, tough(1)] },
        { tags: ['약점 공격'], fx: [dmg(0.6, 'allEnemies'), tough(1, 'allEnemies')] },
        { tags: ['약점 공격'], fx: [dmg(1), tough(2)] },
        { tags: ['약점 공격'], fx: [dmg(1), tough(1), frost(2)] },
        { cost: 1, tags: ['약점 공격'], fx: [dmg(0.8), tough(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」
      ],
      blesses: ['power', 'cost', [frost(1)]],
    },
    { // 마무리 — 2코 · 보존: 강인도가 반 아래인 적에게 크게(붕괴 정산 대신 강인도 선)
      name: '빙설화도', cost: 2, type: '공격', tags: ['보존'],
      fx: [dmg(1.5), tough(1), lowT(0.5), dmg(1.1)],   // 10-11 80+60 → 180+140%
      oracles: [
        { tags: ['보존'], fx: [dmg(2.8), tough(1), lowT(0.5), dmg(2.4)] },
        { tags: ['보존'], fx: [dmg(2.4), tough(1), { k: 'ifFoe', id: '약점 노출' }, dmg(2.4)] },   // 10-13 신탁 점검: 강인도 되살림
        { tags: ['보존'], fx: [dmg(1.4, 'allEnemies'), tough(1, 'allEnemies')] },
        { tags: ['보존'], fx: [dmg(2.4), tough(1), { k: 'ifFoe', id: 'alone' }, dmg(2.4)] },
        { tags: ['보존'], fx: [dmg(2.4), tough(2), lowT(0.3), dmg(1.6)] },
      ],
      blesses: ['power', 'cost', [frost(1)]],
    },
  ],
});
