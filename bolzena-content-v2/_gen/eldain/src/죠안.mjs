// 죠안 — 서포터 · 우울 · 요정 · 변신 「꿈결 형상」. 설계: _measure/엘다인_엔진/01_사도별.md §16 (4차 10-11 — 빵 축복 · 분산은 지킴만 · 목도 신탁 ②~④ 태세 · 꿈결 형상은 태세)
// 엔진: 말 한마디로 「빵」 을 지어 동료 손에 쥐여 주고 · 동료 하나에게 분산(그 아군을 노린 피해를 깎아 파티 실드로 ▶E69 — 공격력 나눔은 우로스 소유라 뺌)을 걸며
//   「주교의 경전」 쪽을 채우고 → 경전 4로 꿈결 형상 — 매 턴 사슬이 적 전체를 치고 준 피해만큼 파티를 고친다. 고유 능력 규칙 없음.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '죠안', Q = '주교의 경전', SH = '분산', F = `${H}_꿈결`;
const BR = `${H}_t1`, CR = `${H}_t4`;
const bread = (v = 1, owner = 'other', x) => make(BR, v, { owner, ...x });
const page = (v = 1) => stack(Q, v);
const share = (target = 'oneAlly') => stack(SH, 1, { target });
const inF = { k: 'ifForm', id: F };
const read = (ratio) => ({ k: 'castOther', from: 'last', ratio });   // ▶E70 바로 앞 동료 카드를 다시
const dream = (n) => [{ k: 'ifStack', id: Q, n }, spend(Q, n), { k: 'form', id: F }];
const TT = ['증발', '소멸'];

export default hero({
  id: H, name: '죠안', nature: '우울', race: '요정', role: '서포터', star: 3,
  blurb: '말 한마디로 빵을 짓는 교단 주교. 동료에게 빵을 쥐여 주고 힘을 나눠 지키며 경전을 채우고, 경전으로 꿈결에 들면 사슬로 적을 치며 그 힘으로 파티를 고칩니다.',
  keyword: { name: Q, desc: '교주님 말씀을 받아 적은 책의 쪽수', carrier: 'self', cap: 4 },
  keywords: [{ name: SH, desc: '죠안이 나눠 지키는 아군 — 노려진 피해를 덜고 그만큼 파티 실드로', carrier: 'hero', cap: 1, endDecay: 1, share: 0.3 }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '은혜 갚은 죠안', when: { on: 'fightStart' }, fx: [bread(1)] },   // 원작 3성 — 전투 시작 빵 1장
    { name: '제가 목도한 것을', when: { on: 'formOn', id: F }, fx: [shield(1)] },   // 꿈결에 들 때 파티 실드
  ],
  forms: [{
    // 4차 태세(사용자 10-11): 경전 4로 들고 · 교리 ③ 으로 지키고 · 3턴 뒤 풀림(반동 없음). 태세 중 죠안 카드가 적을 치면 파티 회복(꿈결의 사슬 — 피해 기반 회복 소유)
    id: F, name: '꿈결 형상', desc: '꿈결에 든 주교 — 죠안이 적을 칠 때마다 파티를 고친다', stance: true, anim: 'Idle_DreamForm',   // 원작 꿈결 형상 쉬는 동작(10-11 붙임)
    turns: 3,
    bonus: [{ type: '공격', ratio: 1.6 }],
    passives: [{ name: '꿈결의 사슬', when: { on: 'hit' }, fx: [heal(0.6)] }, { name: '은혜 갚은 죠안', when: { on: 'turnStart' }, fx: [draw(1), bread(1)] }],
  }],
  ult: { name: '제가 목도한 것을', cost: 250, fx: [dmg(0.8, 'allEnemies'), shield(2), { k: 'form', id: F }] },
}, {
  basicNames: ['경건한 일격', '사슬 휘두르기', '갓 구운 빵'], heal: true, deck: 'light',
  uniques: [
    { // 시동
      name: '빵이 있으라', cost: 0, type: '스킬',
      fx: [bread(1), page(2)],
      oracles: [
        { fx: [bread(2), page(1)] },
        { fx: [bread(1, 'strongestAlly'), page(1)] },
        { power: true, fx: [bread(1), power([rule({ on: 'turnStart' }, [bread(1)])])] },
        { fx: [make(CR, 1, { owner: 'other' }), page(1)] },
        { fx: [bread(1), draw(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」
      ],
      blesses: ['draw', 'heal', [page(1)]],
    },
    {
      name: '교리를 행하고', cost: 1, type: '스킬',
      fx: [share(), page(1)],
      oracles: [
        { fx: [share(), { k: 'cutHit', v: 0.2 }, page(1)] },
        { fx: [share('otherAllies'), page(1)] },
        { fx: [shield(1.6), page(1), inF, { k: 'formKeep', turns: 1 }] },   // 태세 지키기
        { cost: 0, fx: [share()] },
        { fx: [share(), bread(1)] },
      ],
      blesses: ['cost', 'draw', [page(1)]],
    },
    {
      name: '경전 낭독', cost: 1, type: '스킬',
      fx: [read(1), page(1), { k: 'ifPile', from: 'record', n: 1, not: true }, draw(1)],
      oracles: [
        { fx: [read(1.2), page(1)] },
        { cost: 1, fx: [read(0.5), page(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」
        { fx: [read(0.6), draw(1)] },
        { fx: [read(0.6), page(1), draw(1, { who: 'other' })] },
        { fx: [read(0.6), page(1), inF, draw(2)] },
      ],
      blesses: ['cost', 'draw', [page(1)]],
    },
    { // 마무리 — 경전 4 를 쓰면 꿈결 형상
      name: '제가 목도한 것을', cost: 1, type: '스킬',
      fx: [bread(1), ...dream(3)],
      oracles: [
        { power: true, fx: [page(1), power([rule({ on: 'turnStart' }, [page(1)])])] },
        // 원작 배치 열 강화 공격 → 고르는 태세(D1): 공세(전열) · 보급(중열) · 축성(후열) — 들 때 덤
        { type: '공격', fx: [page(1), ...dream(4), dmg(0.6, 'allEnemies')] },   // 공세(전열 범위)
        { fx: [page(1), ...dream(4), draw(2)] },   // 보급(중열)
        { fx: [page(1), ...dream(4), status('사기', 1)] },   // 축성(후열)
        { fx: [bread(1), inF, { k: 'formExtend', id: F, v: 1, max: 4 }] },
      ],
      blesses: ['guard', 'draw', [heal(0.5)]],
    },
    { // 4차 8장 — 열기(서포터 메인딜 갈래 · 말 한마디 「가라사대」)
      name: '가라사대', cost: 1, type: '공격',
      fx: [dmg(1.1, 'allEnemies'), bread(1)],
      oracles: [
        { fx: [dmg(1.4, 'allEnemies'), bread(1)] },
        { fx: [dmg(0.6, 'allEnemies'), page(1)] },
        { fx: [dmg(0.6, 'allEnemies'), inF, dmg(0.6, 'allEnemies')] },
        { fx: [dmg(1.2), bread(1, 'strongestAlly')] },
        { cost: 0, fx: [dmg(0.4, 'allEnemies'), page(1)] },
      ],
      blesses: ['power', 'draw', [page(1)]],
    },
  ],
  tokens: [
    { id: BR, name: '빵', cost: 0, type: '스킬', tags: ['축복', '소멸'], fx: [heal(1), shield(0.6)], blurb: '무에서 지어낸 따끈한 빵' },
    { id: CR, name: '생크림 빵', cost: 0, type: '스킬', tags: ['축복', '소멸'], fx: [status('사기', 1)], blurb: '생크림을 듬뿍 얹은 특별한 빵' },
  ],
});
