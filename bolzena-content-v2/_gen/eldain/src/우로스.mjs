// 우로스 — 서포터 · 공명 · 수인. 설계: _measure/엘다인_엔진/01_사도별.md §17
// 5차 리뉴얼(사용자 10-11 「카드들과 패시브를 순환에 맞게 완전 리뉴얼」): 순환이 주 고유 효과.
// 엔진: 우로스가 자기 카드를 낼 때마다 태세가 「허물」(돕는 얼굴) ↔ 「본성」(압도하는 얼굴)으로 번갈아 바뀐다(태세 toggle).
//   허물 → 본성 → 허물 한 바퀴마다 「순환」 +1 · AP +1(한 턴 1번, 사용자: AP 와 공증 5% 는 살린다).
//   순환은 쌓이는 동안 파티 공격력을 올리고(1당 +5%, 최대 3), 「거대한 뱀」 이 다 찬 순환 3을 써서 크게 터뜨린다 —
//   쌓아 둔 공격력을 계속 누릴지 · 다 써서 한 번에 쏟고 다시 돌릴지가 결정 지점.
//   「백사의 비수」 는 태세를 바꾸지 않고(noToggle) 순서를 맞춘다. 옛 「변치 않는 것」(공격력 나눔)은 없앴다 — 「영역 위」 가 동료 하나를 북돋는다.
//   태세 이름(사용자 10-11): 「허물」 · 「본성」. 사도 이름 「슈로」 는 클론 목록에 있어 검사기가 막는다.
// 2026-11-01 갈라내기(사용자 안 A 「넓어지는 영역」 — _measure/실비아_벨라_우로스_설계안_20261101.md §2-2):
//   순환은 쌓이기만 한다(털지 않음 — 옛 털기는 싸움마다 0.05번). 순환 1당 「영역 위」 동료의 공격력 +10%(파티 전체 +5% 대신 — ▶E81 who marked).
//   순환 3이 된 뒤에는 한 바퀴마다 사기 1(원작 「3번 넓어지면 전열까지」 — stackReach 는 최대에서 다시 안 돌아 formLap + 조건 stack 3 으로).
//   「거대한 뱀」 은 순환 1당 덤(쓰지 않음) · 「지배의 영역」 본성 「동료 다음 카드 +40%」 → 「영역 위 아군 이번 턴 공격력 +20%」(점검 §6-5).
//   고학년 「압도」 = (순환 + 1)회 × 60% 적 전체(원작 「같은 성격 아군 수만큼 추가」 를 순환으로). 허물 ↔ 본성 순서 퍼즐 · 비수 · 순찰 · 죽도는 그대로.
//   없앤 것: 순환 1당 파티 공격력 +5% · 거대한 뱀의 「순환 3을 써서」(full) · 지배의 영역 empower · 고학년의 순환 +1.
//   이전 판: C:\projects\_backup\hero_sep_20261101
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '우로스', C = '순환', U = '영역 위';
const GOOD = `${H}_허물`, EVIL = `${H}_본성`;
const up = (target = 'oneAlly') => stack(U, 1, { target });   // 아군 1명을 영역 위로(옮겨 감)
const G = { k: 'ifForm', id: GOOD }, E = { k: 'ifForm', id: EVIL };
const keep = { k: 'noToggle' };
const per = { k: 'perStack', id: C };                          // 순환 1당(쓰지 않음)
const rise = (v, target = 'oneAlly') => ({ k: 'atkMod', v, target });   // 이번 턴 공격력(영역 위로 세운 그 아군 · 1턴)
const lap = (v = 1) => stack(C, v);

export default hero({
  id: H, name: '우로스', nature: '공명', race: '수인', role: '서포터', star: 3,
  blurb: '백사당의 수호자. 돕는 허물과 압도하는 본성이 번갈아 깨어나며 순환을 돌리고, 순환이 쌓일수록 영역이 넓어져 영역 위에 선 동료가 세집니다.',
  keyword: {
    name: C, desc: '허물과 본성이 한 바퀴 돈 자취 — 쌓일수록 영역이 넓어진다(쓰지 않음)', carrier: 'self', cap: 3,
    per: [{ stat: 'atk', v: 0.1, who: 'marked', mark: U }],   // 11-01: 파티 공격력 +5% → 영역 위 동료 +10%(순환 3 이면 한 명 +30%)
  },
  keywords: [{ name: U, desc: '우로스가 영역 위에 세운 동료', carrier: 'hero', cap: 1, hunt: true, per: [{ stat: 'dealt', v: 0.1, who: 'holder' }] }],   // 10-21 밸런스 조절 3차: +10 → +5%
  // 자동 규칙 둘: 한 바퀴마다 순환 · AP(한 턴 1번 — 태세 한 바퀴 formLap 예외) · 순환이 다 찬 뒤 한 바퀴마다 파티 사기
  passives: [
    { name: '순환', when: { on: 'formLap' }, fx: [lap(1), { k: 'ap', v: 1 }] },   // 10-11 사용자: 턴당 조건 없앰 — 「우로스는 사기인 걸로」(한 바퀴에 우로스 카드 2장이 들어 무한은 아님)
    { name: '한 바퀴의 끝', when: { on: 'formLap' }, conds: [{ c: 'stack', id: C, n: 3 }], fx: [status('사기', 1)] },   // 11-01: 「순환이 3이 되면」 한 번 → 3 이 된 바퀴부터 바퀴마다(앞 규칙이 순환을 먼저 쌓는다)
  ],
  startForm: GOOD,
  forms: [
    { id: GOOD, name: '허물', desc: '벗어 놓은 내면(슈로) — 돕는 얼굴(우로스 카드를 내면 본성으로)', stance: true, toggle: EVIL },
    { id: EVIL, name: '본성', desc: '타락한 뱀 — 압도하는 얼굴(우로스 카드를 내면 허물로)', stance: true, toggle: GOOD },
  ],
  ult: { name: '압도', cost: 250, fx: [dmg(0.6, 'allEnemies', { hitsPer: C, hitsAdd: 1 }), up(), status('사기', 1)] },   // 11-01: 2회 × 80% · 순환 +1 · 사기 2 → (순환 + 1)회 × 60% · 사기 1   // 10-21 7 · 8차: 사기 2 → 1 · 순환 3 → 1   // 10-12 고학년 손질(「탱 · 폿 고학년은 쓰는 느낌이 안 난다」 — 비용 그대로, 효과만): 사기 2 더함   // 10-12 사용자: 비용 300 고학년은 거의 안 쓰여 모두 250 으로
}, {
  basicNames: ['검기 날리기', '백사 일섬', '슈로의 인사'], deck: 'heavy',
  uniques: [
    { // 시동 — 영역 위 동료를 세우고 한 바퀴를 연다
      name: '지배의 영역', cost: 1, type: '스킬',
      fx: [up(), G, shield(1.5), E, rise(0.2)],   // 11-01: 본성 「동료 다음 카드 +40%」 → 영역 위 아군 이번 턴 공격력 +20%   // 10-21 밸런스 조절 6차: 실드 100 → 60%
      oracles: [
        { fx: [up(), G, shield(1.2), E, rise(0.3)] },
        { fx: [up(), draw(1), keep, E, rise(0.3)] },   // 10-13 신탁 점검(11-01 empower → 공격력)
        { type: '강화', power: true, fx: [up(), power([rule({ on: 'formLap' }, [draw(1)])])] },   // 11-01 강화화 — 이 전투 동안 한 바퀴마다 드로우 1
        { fx: [up(), G, shield(0.8), E, rise(0.6, 'self')] },   // 11-01 「영역을 끊다」(원작 「영역이 끝나면 자신 피해 +62%」) — 본성: 우로스 자신 공격력 +60%
        { cost: 0, fx: [up(), G, shield(0.6), E, rise(0.1)] },
      ],
      blesses: ['draw', 'cost', [lap(1)]],
    },
    { // 굴리기 — 허물에선 손을 · 본성에선 적을
      name: '백사당 순찰', cost: 1, type: '스킬',
      fx: [status('약화', 1, { target: 'oneEnemy' }), G, draw(1), E, { k: 'takenMod', v: 0.1, turns: 2, target: 'oneEnemy' }],
      oracles: [
        { fx: [status('약화', 1, { target: 'allEnemies' }), G, draw(1), E, dmg(0.5, 'allEnemies')] },
        { fx: [status('약화', 1, { target: 'oneEnemy' }), draw(2), keep] },   // 10-13 신탁 점검: 드로우 1→2
        { type: '공격', fx: [dmg(0.7, 'allEnemies'), G, draw(1), E, status('약화', 1, { target: 'allEnemies' })] },
        { cost: 0, fx: [status('약화', 1, { target: 'oneEnemy' }), G, draw(1), E, status('취약', 1, { target: 'oneEnemy' })] },
        { fx: [status('약화', 1, { target: 'oneEnemy' }), G, draw(1, { who: 'other' }), E, lap(1)] },
      ],
      blesses: ['draw', 'cost', [lap(1)]],
    },
    { // 쌓인 순환으로 치기(쓰지 않음)
      name: '백사당의 죽도', cost: 1, type: '공격',
      fx: [dmg(1.0, 'oneEnemy', { plus: 0.6, plusIf: E }), per, dmg(0.2)],   // 10-21 밸런스 조절 2차: 100 → 80% · 1당 30 → 20%   // 10-19 타수: 100% + 1당 30% + 본성 60% → 100%(본성 160%) + 1당 30%
      oracles: [
        { fx: [dmg(1.2, 'oneEnemy', { plus: 0.6, plusIf: E }), per, dmg(0.4)] },
        { fx: [dmg(1), per, dmg(0.3), G, empower(0.5, { who: 'other' })] },   // 10-13 신탁 점검
        { cost: 0, fx: [dmg(0.6, 'oneEnemy', { plus: 0.4, plusIf: E }), per, dmg(0.2)] },
        { fx: [dmg(0.6, 'allEnemies'), per, dmg(0.2, 'allEnemies'), E, status('약화', 1, { target: 'allEnemies' })] },
        { fx: [dmg(1.2), per, dmg(0.4), keep] },   // 10-13 신탁 점검
      ],
      blesses: ['power', 'cost', [lap(1)]],
    },
    { // 마무리 — 쌓인 순환만큼 크게(11-01: 순환 3을 써서 쏟던 것을 1당 덤으로 — 털지 않는다)
      name: '거대한 뱀', cost: 2, type: '공격',
      fx: [dmg(1, 'allEnemies', { plus: 0.4, plusIf: E }), per, dmg(0.3, 'allEnemies')],   // 100%(본성 140%) + 순환 1당 30%   // 옛: 순환 3 을 써서 120%
      oracles: [
        { fx: [dmg(1.2, 'allEnemies', { plus: 0.4, plusIf: E }), per, dmg(0.4, 'allEnemies')] },
        { fx: [dmg(2.4, 'oneEnemy', { plus: 1, plusIf: E }), per, dmg(0.8)] },   // 한 적에게
        { type: '스킬', fx: [shield(2.5), per, shield(0.8)] },   // 실드(11-01 사기 뺌 — 효과 셋)
        { cost: 1, fx: [dmg(1, 'allEnemies'), per, dmg(0.4, 'allEnemies')] },   // 1코
        { fx: [dmg(1, 'allEnemies', { plus: 0.4, plusIf: E }), per, dmg(0.3, 'allEnemies'), { k: 'nextAp', v: 1 }] },   // 다음 턴 AP
      ],
      blesses: ['power', 'cost', [lap(1)]],
    },
    { // 순서 맞추기 — 태세를 바꾸지 않는다(원작 2성 「내 마음의 등대」 비수)
      name: '백사의 비수', cost: 0, type: '공격',
      fx: [dmg(0.3, 'randomEnemy', { hits: 2 }), keep],   // 10-21 밸런스 조절 2차: 2 × 40 → 30%
      oracles: [
        { fx: [dmg(0.5, 'randomEnemy', { hits: 2, plus: 0.15, plusIf: E }), keep] },   // 10-19 타수: 2회 × 50% + 본성 30% → 본성이면 대마다 +15%
        { fx: [dmg(0.4, 'randomEnemy', { hits: 2 }), keep, E, { k: 'takenMod', v: 0.1, turns: 2, target: 'oneEnemy' }] },
        { fx: [dmg(0.5, 'randomEnemy', { hits: 2 }), keep, G, up()] },
        { fx: [dmg(0.6, 'randomEnemy'), per, dmg(0.1, 'randomEnemy'), keep] },   // 10-19 타수: 2회 × 30% → 60% 한 대(+ 1당)
        { fx: [dmg(0.6, 'randomEnemy', { hits: 2 }), keep] },   // 10-19 타수: 무작위 3회 × 40% → 2회 × 60%
      ],
      blesses: ['power', 'draw', [lap(1)]],
    },
  ],
});
