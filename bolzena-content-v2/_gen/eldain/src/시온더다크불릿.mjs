// 4차 덧붙임(사용자 10-11): 「가장 먼 적」 대상을 뺐다 — 적 1명(고름).
// 시온 더 다크불릿 — 딜러 · 우울 · 유령. 설계: _measure/엘다인_엔진/01_사도별.md §1 (4차 10-11 — 구조 그대로 · 가벼운 덱 · 시동 ④ 눈가림 · 마무리 ⑤ 5발 사기)
// 엔진: 「마탄」 을 만들어 손에 쥐고 → 버리면 쏘고(탄환) · 내면 소멸하며 쏘고 → 어느 쪽이든 「이 몸, 등장★」(이번 턴 장전) → 쥔 마탄 수 · 장전으로 큰 저격
// 쌓기는 카드가 한다(마탄의 안식 · 소각 마디). 자동 규칙은 「어.둠.의.구.원.자★」 하나.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '시온더다크불릿', B = `${H}_t1`, K = '이 몸, 등장★';
const M = (v, x) => make(B, v, x);
const allB = { k: 'discard', all: true, tag: B };
const per = { k: 'perDiscarded', n: 1 };   // 버린 장수(없으면 한 번)

export default hero({
  id: H, name: '시온 더 다크불릿', nature: '우울', race: '유령', role: '딜러', star: 3,
  blurb: '자칭 검은 마탄의 사수, 실상은 편의점 야간 알바. 원 플러스 원으로 받은 마탄을 손에 쥐고, 버리면 탄환으로 쏘고 내면 태워 쏩니다. 쏠수록 이번 턴 장전이 차오릅니다.',
  keyword: {
    name: K, desc: '마탄을 쏠 때마다 차오르는 이번 턴의 장전', carrier: 'self', cap: 5, endClear: true,
    per: [{ stat: 'dealt', v: 0.1 }],
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '사수의 긍지', desc: '쏟아 낸 마탄만큼 굳어지는 사수의 자부심 — 마탄이 더 아프게 박힌다', carrier: 'self', cap: 5 }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '어.둠.의.구.원.자★', when: { on: 'discard', by: 'other', once: true }, fx: [M(1)] },
    { name: '편의점 야간 알바', when: { on: 'keepAp', kind: 'ap' }, fx: [M(1)] },   // AP 를 남기고 턴을 마치면 마탄 1장(원작 알바 — 아껴 둔 몫이 탄창)
  ],
  ult: { name: '종.말.의.폭.발★', cost: 150, fx: [dmg(1.8, 'allEnemies'), M(3)] },
}, {
  basicNames: ['저격', '견제 사격', '검은 망토'], deck: 'light',
  uniques: [
    { // 시동
      name: '원 플러스 원', cost: 1, type: '공격',
      fx: [dmg(1.8, 'oneEnemy'), M(3)],
      oracles: [
        { fx: [dmg(1.4, 'oneEnemy'), M(4)] },
        { type: '스킬', cost: 0, fx: [M(2), draw(1)] },
        { power: true, fx: [M(1), power([rule({ on: 'turnStart' }, [M(1)])])] },
        // 원작 강화 공격 「눈가림」 — 이번 턴 4번째 이후면 마탄 +1 · 약화(▶E77)
        { fx: [dmg(1.4, 'oneEnemy'), M(2), { k: 'ifNth', n: 4, after: true, id: '눈가림' }, M(1), status('약화', 1, { target: 'oneEnemy' })] },
        { fx: [dmg(1.4, 'oneEnemy'), M(1), M(1, { owner: 'other' })] },
      ],
      blesses: ['power', 'draw', [M(1)]],
    },
    {
      name: '다크넷 접속', cost: 0, type: '스킬',
      fx: [discard(2), M(2), stack('사수의 긍지', 1)],
      oracles: [
        { fx: [discard(3), M(3)] },
        { fx: [{ k: 'burn', v: 2 }, M(2)] },
        { fx: [discard(1, { all: true }), draw(4)] },
        { cost: 1, fx: [discard(2), M(2), { k: 'nextCheaper', v: 1 }] },
        { power: true, fx: [discard(2), M(2), power([rule({ on: 'discard' }, [stack(K, 1)])])] },
      ],
      blesses: ['draw', 'ap', [stack(K, 1)]],
    },
    {
      name: '대물 저격총', cost: 2, type: '공격',
      fx: [dmg(2.6, 'oneEnemy'), { k: 'perTag', id: B }, dmg(0.6, 'oneEnemy')],
      oracles: [
        { fx: [dmg(2.6, 'oneEnemy'), { k: 'perTag', id: B }, dmg(0.8, 'oneEnemy')] },
        { cost: 1, fx: [{ k: 'perTag', id: B }, dmg(0.7, 'oneEnemy')] },
        { tags: ['보존'], fx: [dmg(2.8, 'oneEnemy'), { k: 'when', on: 'handEnd' }, M(1)] },
        { fx: [{ k: 'exileFrom', from: 'hand', all: true, tag: B }, dmg(3.1, 'oneEnemy')] },
        { fx: [dmg(2.1, 'allEnemies'), { k: 'perTag', id: B }, dmg(0.3, 'allEnemies')] },
      ],
      blesses: ['power', 'weakSpot', [M(1)]],
    },
    { // 마무리 ◆ — 손의 마탄 모두(없으면 한 번)
      name: '마.탄.의.사.수', cost: 1, type: '공격', tags: ['보존'],
      fx: [allB, per, dmg(1.4, 'oneEnemy'), stack('사수의 긍지', 1)],
      oracles: [
        { power: true, fx: [M(2), power([rule({ on: 'discard', tag: B }, [stack(K, 1)])])] },
        { tags: ['보존'], fx: [{ k: 'perTag', id: B, n: 1 }, dmg(0.8, 'oneEnemy'), { k: 'burn', all: true, tag: B }] },   // 버리지 않고 태움(마탄 소각 마디로 장전)
        { tags: ['보존'], fx: [{ k: 'discard', v: 2, tag: B }, per, dmg(1.7, 'oneEnemy')] },
        { cost: 1, fx: [allB, { k: 'perDiscarded' }, dmg(0.8, 'oneEnemy')] },   // 10-12 사용자 「신탁 소멸 줄여 줘」
        // 원작 보강(10-11 검토): 마탄 5발을 다 채워 쏘면 더 세다
        { fx: [allB, per, dmg(1.2, 'oneEnemy'), { k: 'ifEvent', n: 5 }, status('사기', 1)] },
      ],
      blesses: ['power', 'cost', [M(1)]],
    },
    { // 4차 8장 — 굴리기(장전): 이번 턴 장전을 먼저 채워 큰 카드에 싣는다(원작 강화 공격 — 마탄 1개당 공격력 +)
      name: '장전 완료★', cost: 1, type: '스킬',
      fx: [stack(K, 2), draw(1)],
      oracles: [
        { fx: [stack(K, 3), draw(1)] },
        { cost: 0, fx: [M(1), stack(K, 1)] },
        { fx: [{ k: 'discard', v: 1, tag: B }, draw(2), stack(K, 1)] },
        { power: true, fx: [power([rule({ on: 'turnStart' }, [stack(K, 1)])])] },
        { fx: [stack(K, 2), M(1, { owner: 'other' })] },
      ],
      blesses: ['draw', 'cost', [M(1)]],
    },
  ],
  tokens: [
    {
      id: B, name: '마탄', cost: 0, type: '공격', tags: ['탄환', '소멸'],
      fx: [dmg(1.2, 'oneEnemy'), { k: 'perStack', id: '사수의 긍지' }, dmg(0.1, 'oneEnemy'), { k: 'when', on: 'discard' }, stack(K, 1), { k: 'when', on: 'burn' }, stack(K, 1)],
      blurb: '1+1 로 받은 덤 한 발입니다',
    },
  ],
});
