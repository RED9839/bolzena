// 디아나(왕년) — 딜러 · 냉정 · 수인 · 변신 「백수공권」. 설계: _measure/엘다인_엔진/01_사도별.md §14 (4차 10-11 — 흐름 = 연속(같은 성격) · 규칙 하나 · 가벼운 덱 · 백수공권은 태세)
// 엔진: 도끼질로 「나른한 삶」(기혈) → 「도끼 부수기」 로 기혈을 「힘, 그 자체」(연격)로 바꿔 백수공권 → 매 턴 연타 토큰을 디아나 카드만 잇달아 → 「찢는다」 로 쏟고 도끼로
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '디아나_왕년', Q = '나른한 삶', P = '힘, 그 자체', F = `${H}_백수공권`;
const inF = { k: 'ifForm', id: F }, outF = { k: 'ifForm', id: F, not: true };
const tough = (v) => ({ k: 'tough', v });
// 변신에 들면 기혈을 모두 연격으로(1개당 +1) — 키워드 규칙 「백수공권」 이 한다
const enter = [{ k: 'form', id: F }];   // 들 때 기혈을 모두 연격으로 — 변신 onEnter(규칙 아님, E62)
const TT = ['증발', '소멸'];
const vuln = status('취약', 1, { target: 'allEnemies' });   // 원작 보강(10-11 검토): 찢는다 — 땅이 갈라져(균열) 적 전체 취약

export default hero({
  id: H, name: '디아나(왕년)', nature: '냉정', race: '수인', role: '딜러', star: 3,
  blurb: '전성기의 디아나. 평소엔 귀찮아서 도끼만 휘두르지만, 기혈이 차오르면 도끼를 박살 내고 맨주먹으로 땅과 하늘을 호령합니다.',
  keyword: { name: Q, desc: '귀찮아, 단순하게 — 도끼질로 모이는 기혈', carrier: 'self', cap: 6 },
  keywords: [
    {
      name: P, desc: '맨주먹으로 잇달아 칠수록 쌓이는 연격', carrier: 'self', cap: 9, per: [{ stat: 'dealt', v: 0.05 }],
    },
  ],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    // 규칙 하나 — 디아나 카드를 연속(직전 카드와 같은 성격 — 냉정 동료 카드가 이어 줌)으로 내면: 도끼 얼굴 기혈 +1 · 맨주먹 얼굴 연격 +1
    { name: '연속', when: { on: 'play', chain: true }, fx: [outF, stack(Q, 1), inF, stack(P, 1)] },
    { name: '나른한 삶', when: { on: 'keepAp', kind: 'ap' }, fx: [stack(Q, 1)] },   // 귀찮아서 AP 를 남기면 기혈 +1
  ],
  // 4차 태세(사용자 10-11 — 카제나 린 흑운태세 꼴): 「도끼 부수기」 로 들고(기혈 → 연격) · 다른 아군의 공격 카드가 끼면 풀리고 · 「찢는다」 로 스스로 끝낸다(보상)
  forms: [{
    id: F, name: '백수공권', desc: '도끼를 부수고 맨주먹으로 — 다른 아군의 공격 카드가 끼면 풀린다', stance: true, anim: 'Idle_Change',   // 원작 변신 쉬는 동작(10-11 되살림)
   
    until: { on: 'play', who: 'other', type: '공격' },
    onEnter: [spend(Q, 'all'), stack(P, 1, { ofEvent: 1 })],
    bonus: [{ type: '공격', ratio: 2, fx: [stack(P, 1)] }],
    passives: [{ name: '맨주먹 전성기', when: { on: 'turnStart' }, fx: [draw(2)] }],   // 태세 중 매 턴 손을 채움(옛 매 턴 토큰 몫)   // 태세 중 디아나 공격 카드마다 연격 +1(맨주먹 연타)
    off: [spend(P, 'all')],
    offEnd: [stack(Q, 3)],
  }],
  ult: { name: '부수기', cost: 250, fx: [dmg(2.4, 'allEnemies'), ...enter] },
}, {
  basicNames: ['도끼질', '도끼 휘두르기', '털가죽 버티기'], starter: 2, deck: 'light',
  uniques: [
    { // 시동
      name: '찢는다', cost: 1, type: '공격',
      fx: [vuln, inF, { k: 'perStack', id: P }, dmg(1.2, 'allEnemies'), { k: 'formEnd' }],   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [dmg(0.8, 'allEnemies'), inF, { k: 'perStack', id: P }, dmg(0.6, 'allEnemies'), { k: 'formEnd' }] },
        { fx: [dmg(0.8, 'allEnemies'), stack(Q, 2), inF, { k: 'perStack', id: P }, dmg(0.5, 'allEnemies')] },
        { fx: [dmg(1.4, 'allEnemies'), { k: 'takenMod', v: 0.1, turns: 2, target: 'allEnemies' }] },
        { fx: [dmg(0.8, 'allEnemies'), inF, spend(P, 3), dmg(0.5, 'allEnemies', { hits: 3 })] },
        { fx: [{ k: 'nextAp', v: 1 }, inF, { k: 'perStack', id: P }, dmg(0.5, 'allEnemies'), { k: 'formEnd' }] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    {
      name: '귀찮아, 단순하게', cost: 0, type: '스킬',
      fx: [discard(2), outF, stack(Q, 2), inF, stack(P, 2)],
      oracles: [
        { fx: [discard(2), outF, stack(Q, 3), inF, stack(P, 2)] },
        { fx: [discard(1, { all: true }), outF, stack(Q, 3), inF, stack(P, 3)] },
        { fx: [outF, stack(Q, 2), inF, { k: 'formKeep', turns: 1 }] },   // 태세 지키기
        { cost: 1, fx: [discard(1), draw(2), stack(Q, 1)] },
        { power: true, fx: [discard(2), stack(Q, 2), power([rule({ on: 'discard', who: 'other' }, [stack(Q, 1)])])] },
      ],
      blesses: ['draw', 'ap', [stack(Q, 1)]],
    },
    {
      name: '도끼 부수기', cost: 2, type: '공격',
      fx: [dmg(3.4), ...enter, { k: 'formKeep', turns: 1 }],   // 10-11 밸런스
      oracles: [
        { fx: [dmg(3.3), ...enter] },
        { cost: 1, fx: [dmg(2.2), ifStack(Q, 4), ...enter] },
        { fx: [dmg(2), ...enter, { k: 'formExtend', id: F, v: 1, max: 4 }] },
        { fx: [dmg(1.6, 'allEnemies'), ...enter, stack(P, 2)] },
        { fx: [dmg(2.2), ...enter, { k: 'formKeep', turns: 1 }] },
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
    { // 마무리
      name: '하늘을 호령하는 포효', cost: 1, type: '공격',
      fx: [dmg(3.6), inF, { k: 'perStack', id: P }, dmg(0.5)],   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { type: '스킬', fx: [stack(Q, 3)] },
        { fx: [dmg(1.6), outF, { k: 'perStack', id: Q }, dmg(0.5)] },
        { fx: [dmg(1.1, 'allEnemies'), inF, { k: 'perStack', id: P }, dmg(0.2, 'allEnemies')] },
        { fx: [dmg(1.6), inF, spend(P, 3), dmg(0.8, 'oneEnemy', { hits: 3 })] },
        { cost: 0, needForm: F, fx: [{ k: 'perStack', id: P, n: 1 }, dmg(0.4)] },   // 백수공권 중에만
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    { // 4차 8장 — 열기(3차에서 뺀 「바위 던지기」 — 원작 강화 공격 「맞을수록 바위」)
      name: '바위 던지기', cost: 1, type: '공격',
      fx: [dmg(2.2), tough(1), stack(Q, 1)],   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [dmg(1.6), tough(1), stack(Q, 1)] },
        { fx: [dmg(0.8, 'allEnemies'), outF, stack(Q, 1)] },
        { fx: [dmg(1.2), tough(2)] },
        { cost: 0, fx: [dmg(0.8), outF, stack(Q, 1)] },
        { fx: [dmg(1.2), { k: 'ifChain' }, dmg(0.8)] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
  ],
});
