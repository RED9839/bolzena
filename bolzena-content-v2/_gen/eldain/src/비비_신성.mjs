// 비비(신성) — 딜러 · 공명 · 미스틱. 설계: _measure/엘다인_엔진/01_사도별.md §11 (4차 10-11 — 파티 축복 + 고학년 「신성」 3턴 · 셋의 몫 2코)
// 엔진: 손에 쥐고 넘긴 비비(신성) 카드마다 「사랑받은 새싹」(카드 값)이 자라고(키워드 cardValue — 턴 끝 손에 있으면 +1 · 낸 뒤 0) → 다 자란(새싹 3) 카드를 내면 파티가 축복받는다
//   (이번 턴 아군 피해 + · 다음 동료 카드 치명 · 영원한 포옹). 낸 뒤 새싹 0. 고학년 「신성」 3턴: 아군 치명 피해 +40%(▶E83 — 치명 소유). 고유 능력 규칙 없음.
// 딜러 생존 금지의 예외: 「영원한 포옹」 의 끈기(쓰러짐 1번 무효)는 사용자 결정(원작 핵심 — ▶E67).
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '비비_신성', K = '사랑받은 새싹';
const grow = (v, x) => ({ k: 'cardStatus', id: K, v, to: 'hand', n: 1, who: 'self', unique: true, max: 4, battle: true, ...x });
const ifSt = (n) => ({ k: 'ifCardSt', id: K, n });
const team = (v = 0.2) => ({ k: 'dealtMod', v, target: 'allAllies' });
const crit = (v = 1) => ({ k: 'critNext', who: 'other', v });   // ▶E66 다음 동료 카드 치명
const KEEP = ['보존'];

export default hero({
  id: H, name: '비비(신성)', nature: '공명', race: '미스틱', role: '딜러', star: 3,
  blurb: '세계수의 은총을 받은 비비. 손에 쥐고 아껴 둔 카드마다 새싹이 자라고, 다 자란 카드를 꺼내면 파티 모두가 축복을 받습니다.',
  keyword: { name: K, desc: '손에 쥔 카드마다 자라는 새싹(카드 값)', carrier: 'self', cap: 4, cardValue: { grow: 'handEnd', reset: 'play' } },
  // 치명 정산(비비(신성) 소유 — 07 §3): 다른 아군이 치명을 내면 손의 카드 새싹 +1(P2 신성한 사냥 — 란 사냥감 치명이 새싹을 키움)
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '은총', desc: '세계수의 은총 — 아군 치명이 잦아짐', carrier: 'self', cap: 10, per: [{ stat: 'crit', v: 0.05, who: 'allies' }] }],   // 2026-10-11 사용자: 자신만 → 아군 전체 · 최대 3 → 10 · 치명마다 −1
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [{ name: '신성한 품 속', when: { on: 'crit', who: 'any' }, fx: [{ k: 'spend', id: '은총', v: 1 }, grow(1)] },   // 10-11: 아군 · 자신의 치명마다 은총 −1(사용자) · 새싹 +1 — 자동 규칙 2개 한도라 한 규칙에 합침
    { name: '이른 봄의 기운', when: { on: 'lowHp', pct: 0.3 }, fx: [status('끈기', 1)] },   // 원작 3성 하위 — 파티 HP 30% 이하가 되면 끈기 1(전투당 한 번)
  ],
  ult: { name: '모든 이를 굽어살피리', cost: 250, fx: [dmg(2.8, 'allEnemies'), { k: 'critDmg', v: 0.4, turns: 3, id: '신성', off: [status('취약', 1, { target: 'allEnemies' })] }, grow(1, { n: 0 })] },   // 10-12 사용자: 비용 300 고학년은 거의 안 쓰여 모두 250 으로
}, {
  basicNames: ['빛구슬', '영롱한 빛구슬', '녹발의 베일'], deck: 'heavy',
  uniques: [
    { // 시동
      name: '손잡기', cost: 0, type: '스킬', tags: KEEP,
      fx: [grow(2), draw(1)],
      oracles: [
        { tags: KEEP, fx: [grow(3), draw(2)] },   // 10-13 신탁 점검
        { tags: KEEP, fx: [grow(1, { n: 0 }), draw(2)] },   // 10-13 신탁 점검
        { power: true, fx: [power([rule({ on: 'make', who: 'other' }, [grow(2)])])] },   // 10-13 신탁 점검: 새싹 1→2
        { tags: KEEP, fx: [draw(1, { who: 'other' }), grow(2)] },   // 10-13 신탁 점검: 새싹 1→2
        { tags: KEEP, fx: [{ k: 'costMod', v: -1, n: 1, who: 'self', unique: true, turns: 1 }, draw(1)] },
      ],
      blesses: ['draw', 'ap', [grow(1)]],
    },
    {
      name: '셋의 몫', cost: 2, type: '공격', tags: KEEP,   // 4차 2코 — 이드 천상을 깨운다
      fx: [dmg(2.4), ifSt(3), team(0.2), stack('은총', 2)],
      oracles: [
        { tags: KEEP, fx: [dmg(2.4), ifSt(3), team(0.3)] },
        { tags: KEEP, fx: [stack('은총', 2), dmg(2.4), { k: 'perCardSt', id: K }, dmg(0.5)] },   // 10-13 신탁 점검: 은총 되살림
        { tags: KEEP, fx: [dmg(1.2, 'allEnemies'), ifSt(3), team(0.2)] },
        { tags: KEEP, fx: [stack('은총', 2), dmg(2.4), ifSt(4), dmg(2.4)] },   // 10-13 신탁 점검: 은총 되살림
        { tags: KEEP, fx: [dmg(3.2), { k: 'cardStatus', id: K, to: 'this', half: true }] },   // 10-13 신탁 점검
      ],
      blesses: ['power', 'cost', [grow(1)]],
    },
    {
      name: '작은 뜻이 빛이 되어', cost: 1, type: '스킬', tags: KEEP,
      fx: [status('사기', 1), ifSt(3), crit()],
      oracles: [
        { tags: KEEP, fx: [status('사기', 2)] },
        { tags: KEEP, fx: [status('사기', 1), ifSt(3), crit(2)] },
        { tags: KEEP, cost: 0, fx: [status('사기', 1), grow(2)] },
        { tags: KEEP, type: '공격', fx: [dmg(1), { k: 'perCardSt', id: K }, dmg(0.4)] },
        { tags: KEEP, fx: [status('사기', 1), ifSt(3), { k: 'gauge', v: 40, target: 'oneAlly' }] },   // 10-13 신탁 점검
      ],
      blesses: ['cost', 'draw', [grow(1)]],
    },
    { // 마무리 — 새싹 3: 아군 끈기 1(쓰러짐 1번 무효 — 딜러 예외, D2)
      name: '영원한 포옹', cost: 2, type: '공격', tags: KEEP,
      fx: [dmg(1.3, 'allEnemies'), ifSt(3), status('끈기', 1), stack('은총', 2)],
      oracles: [
        { power: true, fx: [stack('은총', 2), team(0.3), power([rule({ on: 'play', cardSt: K }, [status('끈기', 1)])])] },   // 10-13 신탁 점검: 은총 2 · 30%
        { tags: KEEP, fx: [dmg(1.3, 'allEnemies'), ifSt(3), team(0.3)] },
        { type: '스킬', tags: KEEP, fx: [team(0.3), stack('은총', 2), ifSt(3), status('끈기', 1)] },   // 10-13 신탁 점검: 은총 되살림
        { tags: KEEP, cost: 1, fx: [dmg(1.2, 'allEnemies'), stack('은총', 2), ifSt(3), status('끈기', 1)] },   // 10-13 신탁 점검: 은총 되살림
        { tags: KEEP, fx: [dmg(1.3, 'allEnemies'), ifSt(3), { k: 'nextAp', v: 1 }] },
      ],
      blesses: ['power', 'cost', [grow(1)]],
    },
    { // 4차 8장 — 굴리기(원작 저학년 「작은 뜻」 의 공격 쪽): 묵힌 새싹이 3 이면 다음 동료 카드 치명
      name: '녹발의 기도', cost: 1, type: '공격', tags: KEEP,
      fx: [dmg(0.5, 'randomEnemy', { hits: 3 }), ifSt(3), crit()],
      oracles: [
        { tags: KEEP, fx: [dmg(0.6, 'randomEnemy', { hits: 3 }), ifSt(3), crit()] },
        { tags: KEEP, fx: [dmg(0.5, 'randomEnemy', { hits: 3 }), ifSt(3), team(0.2)] },
        { tags: KEEP, fx: [dmg(0.5, 'randomEnemy', { hits: 3 }), grow(1)] },
        { tags: KEEP, fx: [{ k: 'perCardSt', id: K, n: 1 }, dmg(0.7, 'randomEnemy')] },   // 10-13 신탁 점검
        { cost: 0, tags: KEEP, fx: [dmg(0.4, 'randomEnemy', { hits: 3 }), ifSt(3), crit()] },   // 10-13 신탁 점검
      ],
      blesses: ['power', 'cost', [grow(1)]],
    },
  ],
});
