// 이드 — 탱커 · 냉정 · 엘프. 설계: _measure/엘다인_엔진/01_사도별.md §2 (4차 10-11)
// 엔진: 손에 쥔 「흐릿한 경계」(천상 · 보존)가 비용 2 이상 카드마다 실드 · 회피 · 꿈의 막 → 막 1개당 방어 +10% → 「나타로 들이받기」 가 파티 실드 절반을 헐어 친다.
// 4차 결정 지점: 들이받기는 보존 — 쥐고 넘기면 실드 유지 1(지금 헐까 · 다음 턴으로 넘길까). 즉사 무효는 회피(D15) · 끈기는 경계 ⑤ 갈래. 착한 마음씨는 2코(스스로 천상을 깨움).
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '이드', K = '이드 더 이터널 불릿', L = `${H}_t1`;
const S = (v = 1, x) => stack(K, v, x);
const ANY = (r) => empower(r, { who: 'any' });
const OT = (r, x) => empower(r, { who: 'other', ...x });   // 다른 아군의 다음 카드
const SKY = ['천상', '보존'];
const dodge = status('회피', 1);
const hold = [{ k: 'when', on: 'handEnd' }, status('실드 보존', 1)];   // 쥐고 넘기면: 실드 보존 1(남은 실드를 다음 턴으로 — 4차 측정 뒤 실드 유지에서 바꿈)

export default hero({
  id: H, name: '이드', nature: '냉정', race: '엘프', role: '탱커', star: 3,
  blurb: '모든 게 꿈이라 믿고 늘 눈을 감은 영원살이 막내 작은 언니. 무거운 카드가 나갈 때마다 손에 쥔 경계가 저절로 펼쳐지고, 쌓인 실드를 나타와 함께 들이받아 피해로 바꿉니다.',
  keyword: {
    name: K, desc: '꿈과 현실 사이에 겹겹이 쌓이는 꿈의 막', carrier: 'self', cap: 3,
    per: [{ stat: 'def', v: 0.1 }],
    onMax: { make: L, n: 1, consume: true },
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '나타', desc: '이드 곁을 지키는 나타 — 이드가 치는 실드가 두꺼워진다', carrier: 'self', cap: 3, per: [{ stat: 'guard', v: 0.05 }] }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '꿈에서 깨지 않았다', when: { on: 'fightStart' }, fx: [S(1)] },   // 원작 2성 — 전투 시작 꿈의 막 +1
    { name: '꿈의 막', when: { on: 'ult' }, fx: [S(1)] },   // 고학년을 쓰면 꿈의 막 +1
  ],
  ult: { name: '너와 나의 우주', cost: 250, fx: [shield(2.5), ddmg(0.4, 'allEnemies', { hits: 4 }), S(2), ANY(0.3)] },
}, {
  basicNames: ['몽중탄', '몽중 연사', '나타 격벽'], deck: 'heavy',
  uniques: [
    { // 시동 — 천상 · 보존: 비용 2 이상 카드가 나가면 손에서 저절로
      name: '흐릿한 경계', cost: 2, type: '스킬', tags: SKY,
      fx: [shield(0.8), S(1), { k: 'ifStack', id: K, n: 2 }, dodge],   // 회피는 막 2 이상일 때(4차 파티 다듬기 — 회피 횟수 줄임)
      oracles: [
        { tags: SKY, fx: [shield(2.4), dodge, S(1)] },
        { type: '공격', tags: SKY, fx: [ddmg(1.4, 'allEnemies'), S(1)] },
        { tags: ['개막', ...SKY], fx: [status('절대 무적', 1), S(1)] },   // 원작 2성 「꿈에서 깨지 않았다」
        { tags: SKY, fx: [shield(1), { k: 'perPile', from: 'hand' }, shield(0.3), S(1)] },
        { tags: SKY, fx: [shield(1.2), status('끈기', 1), S(1)] },
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    {
      name: '함께 꾸는 꿈', cost: 1, type: '스킬',
      fx: [shield(1), draw(1, { tag: '천상' }), stack('나타', 1)],
      oracles: [
        { fx: [shield(1.8), draw(1, { tag: '천상' })] },
        { fx: [shield(1.2), draw(1, { minCost: 2 })] },
        { cost: 2, tags: ['천상'], fx: [shield(1.5), S(1)] },
        { fx: [shield(1.2), { k: 'perStack', id: K }, shield(0.4)] },
        { fx: [status('실드 보존', 1), shield(1)] },
      ],
      blesses: ['guard', 'cost', [S(1)]],
    },
    { // 4차: 2코 — 이드 스스로 천상을 깨운다
      name: '착한 마음씨 이드', cost: 2, type: '스킬',
      fx: [shield(1.2), S(1), OT(0.3)],
      oracles: [
        { tags: SKY, fx: [shield(1.2), S(1), OT(0.2)] },   // 천상 갈래(사용자 10-11) — 비용 2 이상 카드가 나가면 손에서 저절로 · 본판보다 낮게
        { fx: [S(1), OT(0.6)] },
        { cost: 1, fx: [shield(1.6), { k: 'nextAp', v: 1 }] },
        { fx: [shield(2.2), S(1), OT(0.6, { minCost: 2 })] },
        { fx: [shield(2), { k: 'ifShield' }, draw(2)] },
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    { // 마무리 — 보존: 쥐고 넘기면 실드 유지(결정 지점)
      name: '나타로 들이받기', cost: 1, type: '공격', tags: ['보존'],
      fx: [ddmg(0.6), { k: 'spendShield', half: true }, dmg(1, 'oneEnemy', { ofEvent: 1.2 }), ...hold],
      oracles: [
        { type: '스킬', tags: ['보존'], fx: [status('실드 보존', 1), shield(0.6)] },
        { tags: ['보존'], fx: [{ k: 'spendShield', all: true }, dmg(1, 'allEnemies', { ofEvent: 2 }), ...hold] },
        { tags: ['보존'], fx: [{ k: 'spendShield', half: true }, dmg(1, 'oneEnemy', { ofEvent: 1.5 }), S(1)] },
        { cost: 2, tags: ['천상'], fx: [{ k: 'spendShield', half: true }, dmg(1, 'oneEnemy', { ofEvent: 2 })] },
        { fx: [dmg(1, 'oneEnemy', { ofEvent: 1, ofShield: true }), draw(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」
      ],
      blesses: ['cost', 'draw', [S(1)]],
    },
    { // 4차 8장 — 터뜨리기(탱커 메인딜 갈래): 꿈의 막 1개당 나타가 크게 친다
      name: '꿈속의 나타', cost: 2, type: '공격',
      fx: [stack('나타', 1), ddmg(0.8, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.3, 'allEnemies')],
      oracles: [
        { fx: [ddmg(0.8, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.4, 'allEnemies')] },
        { fx: [ddmg(1.2), { k: 'perStack', id: K }, ddmg(0.6)] },
        { tags: SKY, fx: [ddmg(0.8, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.2, 'allEnemies')] },
        { fx: [shield(0.8), { k: 'perStack', id: K }, ddmg(0.4)] },
        { cost: 1, fx: [ddmg(0.5, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.2, 'allEnemies')] },
      ],
      blesses: ['power', 'cost', [S(1)]],
    },
  ],
  tokens: [
    {
      id: L, name: '나타 레이저', cost: 0, type: '공격', tags: ['소멸'],
      fx: [ddmg(0.8, 'allEnemies'), dmg(1, 'allEnemies', { ofEvent: 0.2, ofShield: true })],
      blurb: '나타가 모아 둔 꿈의 막을 한 번에 쏘아 보냅니다',
    },
  ],
});
