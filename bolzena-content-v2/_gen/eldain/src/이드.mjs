// 이드 — 탱커 · 냉정 · 엘프. 설계: _measure/엘다인_엔진/01_사도별.md §2 (4차 10-11)
// 엔진: 손에 쥔 「흐릿한 경계」(천상 · 보존)가 비용 2 이상 카드마다 실드 · 회피 · 꿈의 막 → 막 1개당 방어 +10% → 「나타로 들이받기」 가 파티 실드 절반을 헐어 친다.
// 2026-10-12 밤 사용자: 생성 카드 「나타 레이저」(꿈의 막 3 → 0코 소멸 공격)를 없애고 시동 「흐릿한 경계」 에 합침 — 막이 차면 다음 무거운 카드 때 경계가 펼쳐지며 레이저가 터진다.
// 4차 결정 지점: 들이받기는 보존 — 쥐고 넘기면 실드 유지 1(지금 헐까 · 다음 턴으로 넘길까). 즉사 무효는 회피(D15) · 끈기는 경계 ⑤ 갈래. 착한 마음씨는 2코(스스로 천상을 깨움).
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '이드', K = '이드 더 이터널 불릿';
const S = (v = 1, x) => stack(K, v, x);
const ANY = (r) => empower(r, { who: 'any' });
const OT = (r, x) => empower(r, { who: 'other', ...x });   // 다른 아군의 다음 카드
const SKY = ['천상', '보존'];
const dodge = status('회피', 1);
// 나타 레이저(옛 생성 카드 이드_t1) — 꿈의 막 3(최대)이면 모두 써서 적 전체에 방어 기반 피해 + 파티 실드의 n% 고정 피해
//   카드 글 45자 안에 들게 옛 「적 전체 방어 기반 80% + 파티 실드의 20% 고정 피해」 를 방어 기반 한 줄로 합쳤다(실드 보존 덤도 글 길이로 못 넣음 — 막은 비용 2 카드로 곧 다시 찬다)
//   쏜 뒤 회피 1(옛 「막 2 이상이면 회피」 를 여기로 — 터뜨린 뒤 한 대는 피한다)
const laser = (r = 1.6, ev = true) => [{ k: 'ifStack', id: K, n: 2 }, spend(K, 2), ddmg(r, 'allEnemies'), ...(ev ? [dodge] : [])];
const hold = [{ k: 'when', on: 'handEnd' }, status('실드 보존', 1)];   // 쥐고 넘기면: 실드 보존 1(남은 실드를 다음 턴으로 — 4차 측정 뒤 실드 유지에서 바꿈)

export default hero({
  id: H, name: '이드', nature: '냉정', race: '엘프', role: '탱커', star: 3,
  blurb: '모든 게 꿈이라 믿고 늘 눈을 감은 영원살이 막내 작은 언니. 무거운 카드가 나갈 때마다 손에 쥔 경계가 저절로 펼쳐지고, 쌓인 실드를 나타와 함께 들이받아 피해로 바꿉니다.',
  keyword: {
    name: K, desc: '꿈과 현실 사이에 겹겹이 쌓이는 꿈의 막', carrier: 'self', cap: 3,
    per: [{ stat: 'def', v: 0.1 }],   // 10-12 밤: 옛 onMax(3 이 되면 「나타 레이저」 생성 · 모두 소모) 뺌 — 레이저는 「흐릿한 경계」 가 쏜다
    // 10-12 밤 개편(사용자 「쌓는 길을 하나로」): 옛 전투 시작 +1 · 경계 · 착한 마음씨 · 축복의 +1 → 이드가 비용 2 이상 카드를 낼 때마다 +1(무거운 카드 → 경계 → 레이저)
    rules: [{ name: '꿈이 깊어짐', when: { on: 'play', who: 'any', minCost: 2 }, fx: [S(1)] }],   // 파티 누구든 — 「흐릿한 경계」 가 깨어나는 계기와 같다
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '나타', desc: '이드 곁에서 실드를 받쳐 주는 나타', carrier: 'self', cap: 3, per: [{ stat: 'guard', v: 0.1 }] }],   // 10-12 탱커 손질(실드 정체성) 5→10%
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    // 10-12 밤 개편: 옛 「꿈에서 깨지 않았다」(전투 시작 +1)는 쌓는 길 하나로 합쳐 뺐고, 옛 「꿈의 막」(이름이 고유 효과 뜻과 겹침)은 원작 2성 이름으로
    { name: '꿈에서 깨지 않았다', when: { on: 'turnEnd' }, fx: [{ k: 'perStack', id: K }, shield(0.25)] },   // 10-12 탱커 손질(30% 는 봇 측정에서 너무 세 15%): 옛 「고학년을 쓰면 꿈의 막 +1」 → 턴이 끝날 때 꿈의 막 1개당 파티 실드
  ],
  ult: { name: '너와 나의 우주', cost: 250, fx: [shield(4), ddmg(0.4, 'allEnemies', { hits: 4 }), S(2), status('실드 보존', 1)] },   // 10-12 고학년 손질(「탱 · 폿 고학년은 쓰는 느낌이 안 난다」 — 비용 그대로, 효과만): 실드 250→400% · 파티 다음 카드 +30% → 실드 보존 1(두 적 차례를 막는다)
}, {
  basicNames: ['몽중탄', '몽중 연사', '나타 격벽'], deck: 'heavy',
  uniques: [
    { // 시동 — 천상 · 보존: 비용 2 이상 카드가 나가면 손에서 저절로
      name: '흐릿한 경계', cost: 2, type: '공격', tags: SKY,   // 10-12 밤: 레이저를 쏘니 공격 카드
      fx: [shield(1.5), ...laser()],   // 10-12 밤: 실드 120→150%(봇 측정 — 옛 회피 · 레이저 카드 몫) · 옛 「막 2 이상이면 회피 1」 은 글 길이로 빼고 레이저 뒤 실드 보존으로   // 10-12 밤: S(1) 은 「꿈이 깊어짐」(비용 2 카드)이 대신   // 10-12 탱커 손질 실드 80→120%   // 회피는 막 2 이상일 때(4차 파티 다듬기 — 회피 횟수 줄임)
      oracles: [
        { tags: SKY, fx: [shield(2.4), dodge, ...laser(1.2, false)] },
        { type: '공격', tags: SKY, fx: [dodge, ...laser(2.4, false)] },   // 10-13 신탁 점검: 레이저 140→180%(글 45자)   // 10-12 밤: 글 45자 — 앞 피해를 줄이고 레이저를 키움
        { tags: ['개막', ...SKY], fx: [status('절대 무적', 1), ...laser(1.6, false)] },   // 원작 2성 「꿈에서 깨지 않았다」
        { tags: SKY, fx: [shield(1), ...laser(2.2)] },   // 10-12 밤: 레이저를 키우는 갈래(옛 손패 1장당 실드)
        { tags: SKY, fx: [status('끈기', 1), ...laser(2)] },   // 10-13 신탁 점검 3차: 회피가 몸통(실드 대신 레이저 200%)   // 10-12 밤: 글 45자 — 실드 덜고 끈기 · 레이저(회피)
      ],
      blesses: ['guard', 'draw', [status('실드 보존', 1)]],
    },
    {
      name: '함께 꾸는 꿈', cost: 1, type: '스킬',
      fx: [shield(1.4), draw(1, { tag: '천상' }), stack('나타', 1)],   // 10-12 탱커 손질 실드 100→140%
      oracles: [
        { fx: [shield(2.2), draw(1, { tag: '천상' })] },   // 10-12 본판 실드 140% 에 맞춰 신탁도 +40%p
        { fx: [shield(1.6), draw(1, { minCost: 2 }), stack('나타', 1)] },   // 10-13 신탁 점검: 나타 되살림
        { cost: 2, tags: ['천상'], fx: [shield(2.2)] },   // 10-12 밤: S(1) 은 비용 2 라 「꿈이 깊어짐」 이 대신
        { fx: [stack('나타', 1), shield(1.6), { k: 'perStack', id: K }, shield(0.4)] },   // 10-13 신탁 점검: 나타 되살림
        { fx: [status('실드 보존', 1), shield(1.8), stack('나타', 1)] },   // 10-13 신탁 점검: 나타 되살림
      ],
      blesses: ['guard', 'cost', [status('실드 보존', 1)]],
    },
    { // 4차: 2코 — 이드 스스로 천상을 깨운다
      name: '착한 마음씨 이드', cost: 2, type: '스킬',
      fx: [shield(1.6), OT(0.3)],   // 10-12 밤: S(1) 은 「꿈이 깊어짐」 이 대신 · 10-12 탱커 손질 실드 120→160%
      oracles: [
        { tags: SKY, fx: [shield(1.2), OT(0.2)] },   // 천상 갈래(사용자 10-11) — 비용 2 이상 카드가 나가면 손에서 저절로 · 본판보다 낮게
        { fx: [shield(0.4), OT(0.6)] },   // 10-12 본판 실드 160% 에 맞춰 실드 조금
        { cost: 1, fx: [shield(1.6), { k: 'nextAp', v: 1 }] },
        { fx: [shield(2.2), OT(0.6, { minCost: 2 })] },
        { fx: [shield(2), { k: 'ifShield' }, draw(2)] },
      ],
      blesses: ['guard', 'draw', [status('실드 보존', 1)]],
    },
    { // 마무리 — 보존: 쥐고 넘기면 실드 유지(결정 지점)
      name: '나타로 들이받기', cost: 1, type: '공격', tags: ['보존'],
      fx: [ddmg(0.6), { k: 'spendShield', half: true }, dmg(1, 'oneEnemy', { ofEvent: 1.2 }), ...hold],
      oracles: [
        { type: '스킬', tags: ['보존'], fx: [status('실드 보존', 1), shield(0.8)] },   // 10-13 신탁 점검
        { tags: ['보존'], fx: [{ k: 'spendShield', half: true }, dmg(1, 'allEnemies', { ofEvent: 2 }), ...hold] },   // 10-13 신탁 점검: 실드를 다 쓰던 신탁 — 절반만
        { tags: ['보존'], fx: [{ k: 'spendShield', half: true }, dmg(1, 'oneEnemy', { ofEvent: 2 })] },   // 10-13 신탁 점검 3차   // 10-12 밤: S(1) 덜고 고정 피해 150→160%
        { cost: 2, tags: ['천상'], fx: [{ k: 'spendShield', half: true }, dmg(1, 'oneEnemy', { ofEvent: 2.6 })] },   // 10-13 신탁 점검: 200→260%
        { fx: [dmg(1, 'oneEnemy', { ofEvent: 1.6, ofShield: true }), draw(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」
      ],
      blesses: ['cost', 'draw', [status('실드 보존', 1)]],
    },
    { // 4차 8장 — 터뜨리기(탱커 메인딜 갈래): 꿈의 막 1개당 나타가 크게 친다
      name: '꿈속의 나타', cost: 2, type: '공격',
      fx: [stack('나타', 1), ddmg(0.8, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.3, 'allEnemies')],
      oracles: [
        { fx: [stack('나타', 1), ddmg(0.8, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.4, 'allEnemies')] },   // 10-13 신탁 점검: 나타 되살림(아래 둘도)
        { fx: [stack('나타', 1), ddmg(1.2), { k: 'perStack', id: K }, ddmg(0.6)] },
        { tags: SKY, fx: [ddmg(0.8, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.2, 'allEnemies')] },
        { fx: [stack('나타', 1), shield(0.8), { k: 'perStack', id: K }, ddmg(0.4)] },   // 10-13 신탁 점검: 나타 되살림
        { cost: 1, fx: [ddmg(0.7, 'allEnemies'), { k: 'perStack', id: K }, ddmg(0.2, 'allEnemies')] },   // 10-13 신탁 점검: 50→60%(글 길이로 나타 대신)
      ],
      blesses: ['power', 'cost', [status('실드 보존', 1)]],
    },
  ],
});
