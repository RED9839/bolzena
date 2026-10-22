// 디아나(왕년) — 딜러 · 냉정 · 수인 · 변신 「백수공권」.
// 2026-10-13 리뉴얼(사용자 「너무 복잡 — 디아나식으로 덜어내기」): 자원 하나 · 변신 하나.
//   도끼로 치며 「나른한 삶」(기혈)을 모으고 → 「도끼 부수기」 · 고학년으로만 2턴 「백수공권」(피해 +50% · 고유 카드 덤 — 사용자 10-13: 기혈 가득 자동 변신 뺌) → 「하늘을 호령하는 포효」 로 기혈을 쏟는다.
//   없앤 것: 「힘, 그 자체」(연격) · 「연속」 · AP 남기기 규칙 · 카드의 변신 안팎 두 얼굴 · 다른 아군 공격에 풀리는 태세 · 「찢는다」 로 끝내기. 이전 판: C:\projects\_backup\diana_before_20261013
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '디아나_왕년', Q = '나른한 삶', F = `${H}_백수공권`;
const tough = (v) => ({ k: 'tough', v });
const enter = [{ k: 'form', id: F }];
const SF = (...x) => [{ k: 'ifForm', id: F }, ...x];
const inSF = { k: 'ifForm', id: F };   // 10-19 타수: 「백수공권」 덤 피해를 친 대에 더하는 조건(plusIf)   // 「백수공권」 : 덤(태세 중이면 — 린 「흑운검법 :」 꼴, 사용자 「고유 카드에 백수공권 추가 효과」)
const vuln = status('취약', 1, { target: 'allEnemies' });   // 찢는다 — 땅이 갈라져 적 전체 취약
const pour = (r, target = 'oneEnemy') => [spend(Q, 'all'), { k: 'perEvent', n: 1 }, dmg(r, target)];   // 기혈을 모두 써서 1개당

export default hero({
  id: H, name: '디아나(왕년)', nature: '냉정', race: '수인', role: '딜러', star: 3,
  blurb: '전성기의 디아나. 평소엔 귀찮아서 도끼만 휘두르지만, 기혈이 차오르면 도끼를 박살 내고 맨주먹으로 땅과 하늘을 호령합니다.',
  keyword: { name: Q, desc: '도끼질로 모이는 기혈', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.08 }] },   // 10-22 디아나 딜 보강(퓨어 딜러 — 키워 가는 요소): 1당 주는 피해 +8%
  passives: [
    { name: '귀찮아도 도끼질', when: { on: 'play', type: '공격' }, fx: [stack(Q, 1)] },   // 디아나 공격 카드마다 기혈 1
  ],
  forms: [{
    id: F, name: '백수공권', desc: '도끼를 부순 맨주먹 전성기 — 2턴', stance: true, anim: 'Idle_Change', turns: 2,
    mods: { dealt: 0.5 },
  }],
  ult: { name: '부수기', cost: 250, fx: [dmg(2.4, 'allEnemies'), ...enter] },
}, {
  basicNames: ['도끼질', '도끼 휘두르기', '털가죽 버티기'], starter: 2, deck: 'light',
  uniques: [
    { // 시동 — 적 전체 · 취약
      name: '찢는다', cost: 1, type: '공격',
      fx: [dmg(1.6, 'allEnemies'), vuln, ...SF(dmg(0.6, 'allEnemies'))],   // 10-21 밸런스 조절: 120 → 140 → 160%(2차)
      oracles: [
        { fx: [dmg(1.6, 'allEnemies'), vuln, ...SF(dmg(0.6, 'allEnemies'))] },
        { fx: [dmg(1, 'allEnemies'), vuln, stack(Q, 2), ...SF(dmg(0.6, 'allEnemies'))] },
        { fx: [dmg(1.6, 'allEnemies'), { k: 'takenMod', v: 0.2, turns: 2, target: 'allEnemies' }, ...SF(dmg(0.6, 'allEnemies'))] },
        { fx: [dmg(1.2, 'allEnemies', { hits: 2, plus: 0.3, plusIf: inSF }), vuln] },   // 10-19 타수: 2회 × 120%(신탁 맞춤이 기본 120% 로 올리던 값) + 백수공권 60% → 백수공권이면 대마다 +30%
        { fx: [dmg(1, 'allEnemies'), vuln, { k: 'nextAp', v: 1 }, ...SF(dmg(0.6, 'allEnemies'))] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    { // 손 고르기 · 기혈
      name: '귀찮아, 단순하게', cost: 0, type: '스킬',
      fx: [discard(2), draw(2), stack(Q, 1), ...SF(stack(Q, 2))],
      oracles: [
        { fx: [discard(2), draw(2), stack(Q, 2), ...SF(stack(Q, 2))] },
        { fx: [discard(3), draw(3), stack(Q, 1), ...SF(stack(Q, 2))] },
        { fx: [stack(Q, 3), ...SF(stack(Q, 2))] },
        { cost: 1, fx: [discard(1), draw(3), stack(Q, 2), ...SF(stack(Q, 2))] },
        { power: true, fx: [stack(Q, 1), power([rule({ on: 'turnStart' }, [stack(Q, 1)])])] },
      ],
      blesses: ['draw', 'ap', [stack(Q, 1)]],
    },
    { // 백수공권에 든다
      name: '도끼 부수기', cost: 2, type: '공격',
      fx: [dmg(4.2), ...enter],   // 10-21 밸런스 조절: 340 → 420%(1 · 7차)
      oracles: [
        { fx: [dmg(4.8), ...enter] },   // 10-21: 400 → 480%
        { cost: 1, fx: [dmg(2.4), ...enter] },
        { fx: [dmg(2.4), ...enter, stack(Q, 2)] },
        { fx: [dmg(1.6, 'allEnemies'), ...enter] },
        { fx: [dmg(2.6), ...enter, draw(1)] },
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
    { // 마무리 — 기혈을 모두 쏟는다
      name: '하늘을 호령하는 포효', cost: 1, type: '공격',
      fx: [dmg(2, 'oneEnemy', { plus: 1.5, plusIf: inSF }), ...pour(0.4)],   /* 10-21 8차: 180 → 200% */   // 10-21 밸런스 조절 2차: 150 → 180%   // 10-19 타수: 150% + 1개당 40% + 백수공권 150% → 150%(백수공권 300%) + 1개당 40%(2타 · 합 그대로)   // 10-13 저녁 너프(사용자): 기혈 1개당 60→40% — 백수공권 한 방 900→750%
      oracles: [
        { fx: [dmg(2.4, 'oneEnemy', { plus: 1.5, plusIf: inSF }), ...pour(0.5)] },   // 10-21 2차: 200 → 240%
        { fx: [dmg(1, 'allEnemies', { plus: 1.5, plusIf: inSF }), ...pour(0.3, 'allEnemies')] },   // 10-19 타수: 백수공권 한 대 150% 를 적 전체 한 대에 더함
        { tags: ['보존'], fx: [dmg(1.5, 'oneEnemy', { plus: 1.5, plusIf: inSF }), ...pour(0.4)] },
        { cost: 0, fx: [...pour(0.4), ...SF(dmg(1.5))] },
        { fx: [dmg(1.5), ...pour(0.4), ...SF(tough(2))] },   // 백수공권이면 강인도 2
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    { // 강인도
      name: '바위 던지기', cost: 1, type: '공격',
      fx: [dmg(2.8), tough(1), stack(Q, 1), ...SF(tough(1))],   // 10-21 밸런스 조절: 220 → 280%(1 · 6차)
      oracles: [
        { fx: [dmg(3.2), tough(1), stack(Q, 1), ...SF(tough(1))] },   // 10-21: 260 → 320%
        { fx: [dmg(1, 'allEnemies'), tough(1), stack(Q, 1), ...SF(tough(1))] },
        { fx: [dmg(1.2), tough(2), ...SF(tough(1))] },
        { cost: 0, fx: [dmg(1.2), tough(1), stack(Q, 1), ...SF(tough(1))] },
        { fx: [dmg(1.6), tough(1), stack(Q, 2), ...SF(tough(1))] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
  ],
});
