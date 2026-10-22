// 우이 — 서포터 · 활발 · 정령. 설계: _measure/엘다인_엔진/01_사도별.md §7 (4차 10-11 — 누구의 넘친 회복이든 저금 · 개굴비 연쇄 · 혓바닥은 해제 + 넘친 회복 비례(X 는 에르핀 소유))
// 엔진: 아군 회복이 넘치면 「언해피 우이」(웃음 저금)가 쌓이고 → 저금을 깨 AP 를 되돌리거나 동료의 가장 비싼 카드 비용을 깎고 → 개굴비는 연쇄로 다음 턴 한 번 더.
// 자동 규칙은 「넘치는 웃음」(회복이 넘치면 +1, 카드 한 장에 한 번) 하나.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '우이', K = '언해피 우이', R = `${H}_t1`;
const S = (v = 1, x) => stack(K, v, x);
const rain = (v = 1, x) => make(R, v, x);
const brk = (n) => [ifStack(K, n), spend(K, n)];   // 저금 n 을 깨면
const OT = { who: 'other' };
const clean = (v) => ({ k: 'cleanse', v });
const over = { k: 'perOverheal', per: 20 };   // 이번 판 넘친 회복 20당
const cheap = (v, x) => ({ k: 'costMod', v, n: 1, turns: 1, choose: 'priciest', ...x });

export default hero({
  id: H, name: '우이', nature: '활발', race: '정령', role: '서포터', star: 3,
  blurb: '행복을 지키려는 영원살이 막내. 넘치게 고친 웃음은 저금통에 모아 두었다가, 깨서 동료가 큰 카드를 낼 수 있게 숨을 돌려 줍니다.',
  keyword: {
    name: K, desc: '넘치는 웃음을 모아 두는 저금통', carrier: 'self', cap: 3,
    rules: [{ name: '넘치는 웃음', when: { on: 'overheal', who: 'any', once: true, pct: 0.5 }, fx: [S(1)] }],   // 10-12 우이 손질(사용자 「힐량이 낮고 조건이 빡세다」) 70% → 50% · 10-12 사용자 「초과 힐이 너무 힘들다」 — 최대 HP 를 넘길 때 → 회복이 HP 70% 를 넘길 때   // 넘치면 저금 +1(AP 쪽은 부탁해 · 장화로)
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '에루의 우비', desc: '에루가 접어 준 파티의 우비', carrier: 'self', cap: 3, per: [{ stat: 'hot', ratio: 0.2 }] }],   // 10-12 우이 손질(사용자 「힐량이 낮고 조건이 빡세다」) 15→20%   // 10-11 밸런스(서포터 하향 뒤 48.8%) 10→15%
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '긍정왕 우이', when: { on: 'turnStart' }, conds: [{ c: 'hp', pct: 0.5 }], fx: [heal(0.5)] },   // 10-21 밸런스 조절: 조건 65 → 50% · 2차 회복 84 → 60%   // 파티 HP 65% 이하면 매 턴 회복 — 10-12 우이 손질(사용자 「힐량이 낮고 조건이 빡세다」) 조건 50% → 65%(80% · 회복 100% 는 봇 측정에서 너무 셈)
  ],
  // 원작 보강(10-11 검토): 고학년이 무작위 적 행동을 늦춘다
  ult: { name: '말하는대로!', cost: 200, fx: [heal(3), { k: 'cleanse', v: 3 }, { k: 'ap', v: 2 }, draw(2)] },   // 10-12 고학년 손질(「탱 · 폿 고학년은 쓰는 느낌이 안 난다」 — 비용 그대로, 효과만): 회복 200→300% · 둔화 · 저금 1 → 드로우 2(효과 넷 한도)
}, {
  basicNames: ['빗방울', '이슬 튕기기', '보슬보슬'], heal: true, deck: 'light', starter: true,
  uniques: [
    { // 시동(은총으로)
      name: '개굴비 내리기', cost: 1, type: '스킬',
      fx: [heal(1.2), rain(1)],   // 10-12 우이 손질(사용자 「힐량이 낮고 조건이 빡세다」) 100→120%(글 140%)
      oracles: [
        { fx: [heal(2.4), rain(1)] },
        { fx: [heal(0.6), rain(2)] },   // 10-13 신탁 점검: 회복 되살림
        { fx: [heal(1.6), rain(1), S(1)] },
        { cost: 0, tags: ['증발'], fx: [heal(1)] },
        { fx: [heal(0.8), { k: 'nextCheaper', v: 1 }, rain(1)] },   // 10-13 신탁 점검: 회복 되살림
      ],
      blesses: ['heal', 'draw', [S(1)]],
    },
    {
      name: '우비 접어 주기', cost: 0, type: '스킬',   // 10-13 신탁 점검: 신탁 다섯 모두 기본보다 약해(봇) — 우비 되살림 · 1코 둘은 드로우 2 · AP 2
      fx: [draw(1), stack('에루의 우비', 1), ...brk(1), { k: 'ap', v: 1 }],
      oracles: [
        { cost: 1, fx: [draw(2), stack('에루의 우비', 1), ...brk(1), { k: 'ap', v: 2 }] },   // 10-12 사용자 「신탁 소멸 줄여 줘」 — 0코 + AP 는 소멸이어야 해(검사기) 1코로
        { cost: 1, fx: [draw(2), stack('에루의 우비', 1), ...brk(1), { k: 'nextAp', v: 2 }] },   // 10-12 사용자 「신탁 소멸 줄여 줘」 — 1코로
        { fx: [{ k: 'payHpPct', v: 0.05 }, draw(3), stack('에루의 우비', 2)] },   // 10-13 신탁 점검 3차   // 10-12 사용자 「신탁 소멸 줄여 줘」 — AP 대신 드로우 2
        { tags: ['소멸'], fx: [draw(1), stack('에루의 우비', 1), ...brk(2), { k: 'ap', v: 2 }] },   // 10-13 신탁 점검: 드로우 · 우비 되살림
        { fx: [draw(1), stack('에루의 우비', 1), ...brk(1), { k: 'gauge', v: 30, target: 'otherAllies' }] },   // 10-13 신탁 점검: 우비 되살림 · 게이지 20→30
      ],
      blesses: ['draw', 'heal', [S(1)]],
    },
    {
      name: '에루야, 부탁해!', cost: 1, type: '스킬',
      fx: [cheap(-1, OT), ...brk(2), cheap(-1)],
      oracles: [
        { fx: [cheap(-2, OT), ...brk(1), cheap(-1)] },
        { cost: 0, fx: [...brk(2), cheap(-1, OT), draw(1)] },
        { fx: [...brk(1), cheap(-1, OT), empower(0.3, { who: 'any' })] },
        { fx: [empower(0.6, { who: 'any' }), draw(1)] },
        { fx: [cheap(-1), S(2)] },   // 10-13 신탁 점검
      ],
      blesses: ['cost', 'draw', [S(1)]],
    },
    { // 마무리 — 원작 강화 공격(에루 혓바닥 — 해로운 효과 해제) · 이번 턴 넘친 회복 비례
      name: '에루 혓바닥', cost: 1, type: '공격',
      fx: [dmg(1.6), clean(1), over, dmg(0.2)],   // 10-21 밸런스 조절: 120 → 160% · 20당 10 → 20%
      oracles: [
        { type: '스킬', fx: [heal(1), clean(2)] },
        { fx: [dmg(0.5, 'allEnemies'), over, dmg(0.05, 'allEnemies')] },
        { type: '스킬', fx: [clean(1), draw(1, { who: 'other' }), { k: 'ap', v: 1 }] },
        { fx: [dmg(0.8), clean(1), { k: 'perStack', id: K }, dmg(0.8)] },
        { fx: [dmg(0.8), clean(1), ...brk(1), { k: 'nextAp', v: 1 }] },
      ],
      blesses: ['power', 'draw', [S(1)]],
    },
    { // 4차 8장 — 굴리기(어사이드 「장화 신은 우이」): 개굴비를 더 부르고 저금으로 한 장 더
      name: '장화 신은 우이', cost: 0, type: '스킬',
      fx: [rain(1), ...brk(2), { k: 'nextAp', v: 1 }],   // 저금 2 → 동료 다음 턴 AP
      oracles: [
        { fx: [rain(1), ...brk(1), rain(2)] },
        { fx: [rain(1), draw(1, { who: 'other' })] },
        { fx: [heal(0.8), S(1)] },
        { fx: [rain(1), ...brk(1), empower(0.3, { who: 'other' })] },
        { fx: [rain(1), { k: 'gauge', v: 10, target: 'otherAllies' }] },
      ],
      blesses: ['heal', 'draw', [S(1)]],
    },
  ],
  tokens: [
    { id: R, name: '개굴비', cost: 0, type: '스킬', tags: ['연쇄', '소멸'], fx: [heal(0.6)], blurb: '에루가 부르는 개구리 비' },   // 10-21 7차: 84 → 70%   // 10-12 우이 손질(사용자 「힐량이 낮고 조건이 빡세다」) 60→70%(글 80%)
  ],
});
