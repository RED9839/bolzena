// 우이(기억) — 서포터 · 냉정 · 정령. 설계: _measure/엘다인_엔진/01_사도별.md §8 (4차 10-11 — 구조 숙제: 고학년 예열)
// 엔진: 「세잎클로버」 를 쥐면 큰 한 대를 15%로 막고(게이지) · 내면 게이지 · 드로우 → 고학년 「이젠 피하지 않아」 를 쓸 때마다 「행복한 에루」(예열)가 올라
//   다음 고학년 요구 게이지가 25%씩 싸지고(▶E78) 2면 피해 2배 → 고학년이 클로버를 다시 만들고 계몽(고학년마다 드로우)으로 손을 채운다.
// 자동 규칙은 「행복왕 우이」(고학년을 쓰면 예열 +1) 하나. 클로버의 반응은 카드 마디(손에 있으면 — ▶E48).
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '우이_기억', K = '행복한 에루', R = '에루 리턴즈', C = `${H}_t1`;
const vulnAll = (v = 1) => status('취약', v, { target: 'allEnemies' });
const gauge = (v, x) => ({ k: 'gauge', v, ...x });
const clover = (v = 1, x) => make(C, v, x);
const warm = { k: 'perStack', id: K };   // 예열 1개당
const gone = { k: 'perGone' };   // 이번 전투 소멸한 카드 1장당
const revive = (x) => ({ k: 'pull', from: 'gone', n: 1, ...x });   // 소멸 더미의 카드 1장을 손으로(되찾기)

export default hero({
  id: H, name: '우이(기억)', nature: '냉정', race: '정령', role: '서포터', star: 3,
  blurb: '잊으려던 기억을 마주한 원본 우이. 지워진 카드(소멸한 기억)를 다시 손으로 되찾고, 지운 기억만큼 에루의 연못이 데워집니다.',
  keyword: {
    name: K, desc: '고학년을 쓸수록 데워지는 에루의 연못(예열)', carrier: 'self', cap: 2,
    per: [{ stat: 'ultCost', v: -0.25 }],
    // 자동 규칙 하나 — 누구의 카드든 소멸하면 고학년 게이지 +5(지운 기억이 연못을 데움). 예열은 고학년이 직접 쌓는다
    rules: [{ name: '지운 기억', when: { on: 'exhaust', who: 'any' }, fx: [gauge(5)] }],
  },
  keywords: [{ name: R, desc: '세잎클로버가 큰 아픔을 막아 낸 횟수', carrier: 'self', cap: 10 }],   // 10-12 카드 글: 「막아 낸 피해 20당 1」 → 「막을 때마다 1」(카드 글의 「그 값 20당」 을 없앰) · 최대 30 → 10
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '행복 전파자', when: { on: 'fightStart' }, fx: [clover(1)] },   // 전투 시작 세잎클로버 1장
  ],
  ult: { name: '이젠 피하지 않아', cost: 200,   /* 10-13 사용자: 고학년 값어치 3.66 으로 높음 — 비용 150 → 200 */ fx: [stack(K, 1), dmg(1.2, 'allEnemies'), vulnAll(1), clover(1), draw(2), { k: 'ifStack', id: K, n: 2 }, dmg(1.2, 'allEnemies')] },   // 10-12 고학년 손질(「탱 · 폿 고학년은 쓰는 느낌이 안 난다」 — 비용 그대로, 효과만): 드로우 1→2
}, {
  basicNames: ['물방울', '잔물결', '기억 떠올리기'], heal: true, deck: 'light',
  uniques: [
    { // 시동
      name: '행복 전파자', cost: 1, type: '스킬',
      fx: [heal(2.2), clover(1)],
      oracles: [
        { fx: [heal(3), clover(1)] },
        { fx: [heal(1.2), clover(2)] },   // 10-13 신탁 점검: 회복 되살림
        { fx: [heal(1.4), { k: 'healMod', v: 0.3, turns: 2 }, clover(1)] },
        { fx: [heal(1.8), clover(1), draw(1)] },   // 10-13 신탁 점검
        { fx: [heal(1.2), clover(1), gauge(20, { target: 'strongestAlly' })] },   // 원작 강화 공격(에루가 올라탐)
      ],
      blesses: ['heal', 'draw', [gauge(10)]],
    },
    {
      name: '잊으려던 기억', cost: 0, type: '스킬', tags: ['소멸'],
      fx: [{ k: 'burn', v: 1 }, draw(2)],
      oracles: [
        { tags: ['소멸'], fx: [{ k: 'burn', v: 1 }, draw(3)] },
        { tags: ['소멸'], fx: [{ k: 'burn', v: 2 }, draw(3)] },   // 10-13 신탁 점검
        { tags: ['소멸'], fx: [{ k: 'exileFrom', from: 'draw', n: 1, basic: true }, clover(1)] },
        { tags: ['소멸'], fx: [status('계몽', 1), draw(2)] },   // 10-13 신탁 점검
        { tags: ['보존', '회수 3'], fx: [{ k: 'burn', v: 1 }, draw(2)] },
      ],
      blesses: ['draw', 'ap', [gauge(10)]],
    },
    {
      name: '되찾은 기억', cost: 1, type: '스킬',   // 4차 덧붙임(사용자 10-11): 소멸 더미 = 지운 기억 — 되찾는다(사도마다 턴에 한 번 · 엔진)
      fx: [revive(), gauge(20)],
      oracles: [
        { fx: [revive(), gauge(30)] },
        { fx: [revive({ who: 'other' }), draw(1), gauge(20)] },   // 10-13 신탁 점검: 게이지 되살림
        { fx: [revive(), empower(0.5)] },
        { fx: [revive(), vulnAll(1)] },   // 옛 「일렁이는 파동」 · 10-13 신탁 점검: 되찾기 되살림(게이지 대신)
        { fx: [revive({ to: 'top' }), draw(1), gauge(20)] },   // 10-13 신탁 점검: 게이지 되살림
      ],
      blesses: ['cost', 'draw', [gauge(10)]],
    },
    { // 마무리 — 예열 1개당
      name: '떠나보내기', cost: 1, type: '공격',
      fx: [dmg(1.2), gone, dmg(0.1)],   // 이번 전투 소멸한 카드 1장당(지운 기억만큼)
      oracles: [
        { type: '스킬', fx: [gauge(30), draw(1)] },
        { fx: [dmg(0.8, 'allEnemies'), warm, dmg(0.3, 'allEnemies')] },
        { fx: [{ k: 'exileFrom', from: 'hand', n: 1, tag: C }, dmg(1.8), gauge(20)] },
        { fx: [dmg(0.8), warm, dmg(0.6), { k: 'nextAp', v: 1 }] },
        { fx: [dmg(1.2), { k: 'perStack', id: R }, dmg(0.5)] },   // 3차 꼴 — 이번 전투 클로버가 막은 횟수 1당(10-12 단위를 바꿔 10% → 50%)
      ],
      blesses: ['power', 'cost', [gauge(10)]],
    },
    { // 4차 8장 — 굴리기(고학년 「연못 단계」): 예열이 있으면 연못이 파티를 고친다
      name: '에루의 연못', cost: 1, type: '스킬',
      fx: [gauge(20), heal(1.2)],   // 10-12 우이 손질: 옛 「예열이 있으면」 조건을 뺐다(고학년 전엔 못 고쳤다)
      oracles: [
        { fx: [gauge(30), heal(1.2)] },
        { fx: [heal(1), clover(1)] },
        { fx: [gauge(20, { target: 'strongestAlly' }), heal(0.8), draw(1)] },   // 10-13 신탁 점검: 회복 되살림
        { fx: [gauge(20), heal(0.8), { k: 'perStack', id: K }, heal(0.6)] },   // 10-13 신탁 점검: 회복 되살림
        { cost: 0, tags: ['소멸'], fx: [gauge(30)] },
      ],
      blesses: ['heal', 'cost', [gauge(10)]],
    },
  ],
  tokens: [
    {
      id: C, name: '세잎클로버', cost: 0, type: '스킬', tags: ['보존', '소멸'],
      fx: [gauge(20), draw(1), { k: 'when', on: 'handHurtBig', pct: 0.15 }, gauge(30), stack(R, 1)],
      blurb: '큰 아픔을 조금만 남기는 행운',
    },
  ],
});
