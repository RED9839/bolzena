// 4차 덧붙임(사용자 10-11): 「가장 먼 적」 대상을 뺐다 — 적 1명(고름).
// 시온 더 다크불릿 — 딜러 · 우울 · 유령. 설계: _measure/엘다인_엔진/01_사도별.md §1 (4차 10-11 — 구조 그대로 · 가벼운 덱 · 시동 ④ 눈가림 · 마무리 ⑤ 5발 사기)
// 엔진: 「마탄」 을 만들어 손에 쥐고 → 버리면 쏘고(탄환) · 내면 소멸하며 쏘고 → 어느 쪽이든 「이 몸, 등장★」(이번 턴 장전) → 쥔 마탄 수 · 장전으로 큰 저격
// 쌓기는 카드가 한다(마탄의 안식 · 소각 마디). 자동 규칙은 「어.둠.의.구.원.자★」 하나.
// 2026-10-13 리뉴얼(사용자): 마탄 0코 → 1코 · 탄환(카제나식 — 사용하면 소멸, 버린 더미로 가면 턴 끝 포함 무작위 적에게 추가 공격) · 손에 9장까지(넘치면 그 자리에서 쏜다).
//   쓰임 세 갈래 — 써서 강화(장전 완료★ · 대물 ④) · 쥐면 강화(대물 저격총) · 털기(마.탄.의.사.수만 — 보존, 쥔 수에 비례해 3~5장이면 주력 한 방).
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
  keywords: [{ name: '사수의 긍지', desc: '쏟아 낸 마탄만큼 굳는 사수의 자부심', carrier: 'self', cap: 5 }],
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '어.둠.의.구.원.자★', when: { on: 'discard', by: 'other', once: true }, fx: [M(1)] },
    { name: '편의점 야간 알바', when: { on: 'keepAp', kind: 'ap' }, fx: [M(1)] },   // AP 를 남기고 턴을 마치면 마탄 1장(원작 알바 — 아껴 둔 몫이 탄창)
  ],
  ult: { name: '종.말.의.폭.발★', cost: 150, fx: [dmg(2.2, 'allEnemies'), M(1), { k: 'pull', from: 'draw', n: 9, tag: B }, { k: 'pull', from: 'discard', n: 9, tag: B }] },   // 10-13 리뉴얼: 마탄 3장 → 1장 + 뽑을 더미 · 버린 더미의 마탄 모두 손으로(9장까지 — 레노아 「검은 가시」)
}, {
  basicNames: ['저격', '견제 사격', '검은 망토'], deck: 'light',
  uniques: [
    // 2026-10-13 리뉴얼(사용자): 마탄 0코 → 1코 · 손에 9장까지. 마탄의 쓰임은 세 갈래 —
    //   ① 써서 강화(장전 완료★ · 대물 저격총 신탁 4): 손의 마탄 1장을 버려(탄환 발사) 그 카드가 세진다
    //   ② 쥐면 강화(대물 저격총): 쥔 마탄 1장당 피해 +
    //   ③ 한 번에 털기(마.탄.의.사.수 — 이 카드만): 쥔 마탄을 모두 버려 1장당 치고, 9장을 다 채우면 시온에서 가장 센 한 방
    { // 시동
      name: '원 플러스 원', cost: 1, type: '공격',
      fx: [dmg(1.8, 'oneEnemy'), M(2)],   // 10-13 리뉴얼: 마탄 3 → 2(1코 마탄 — 수 대신 질)
      oracles: [
        { fx: [dmg(1.4, 'oneEnemy'), M(3)] },
        { type: '스킬', cost: 0, fx: [M(2), draw(1)] },
        { power: true, fx: [M(2), power([rule({ on: 'turnStart' }, [M(1)])])] },
        // 원작 강화 공격 「눈가림」 — 이번 턴 4번째 이후면 마탄 +1 · 약화(▶E77)
        { fx: [dmg(1.4, 'oneEnemy'), M(1), { k: 'ifNth', n: 4, after: true, id: '눈가림' }, M(1), status('약화', 1, { target: 'oneEnemy' })] },
        { fx: [dmg(1.4, 'oneEnemy'), M(1), M(1, { owner: 'other' })] },
      ],
      blesses: ['power', 'draw', [M(1)]],
    },
    { // 버리기 · 보급 — 손을 버려 마탄을 쏘고(탄환) 새 마탄을 받는다
      name: '다크넷 접속', cost: 0, type: '스킬',
      fx: [discard(2), M(2), stack('사수의 긍지', 1)],
      oracles: [
        { fx: [discard(3), M(2), stack('사수의 긍지', 1)] },
        { fx: [{ k: 'burn', v: 2 }, M(2), stack('사수의 긍지', 1)] },
        { fx: [discard(3), draw(3)] },   // 10-13 리뉴얼: 손패 전부 버리기 → 3장(털기는 사수만)
        { cost: 1, fx: [discard(2), M(3), { k: 'nextCheaper', v: 1 }] },
        { power: true, fx: [discard(2), M(2), power([rule({ on: 'discard' }, [stack(K, 2)])])] },
      ],
      blesses: ['draw', 'ap', [stack(K, 1)]],
    },
    { // ② 쥐면 강화 — 손에 쥔 마탄 1장당
      name: '대물 저격총', cost: 2, type: '공격',
      fx: [dmg(2.6, 'oneEnemy'), { k: 'perTag', id: B }, dmg(1, 'oneEnemy')],   // 10-13 리뉴얼: 쥔 1장당 60 → 100%
      oracles: [
        { fx: [dmg(2.6, 'oneEnemy'), { k: 'perTag', id: B }, dmg(1.2, 'oneEnemy')] },
        { cost: 1, fx: [{ k: 'perTag', id: B }, dmg(1.4, 'oneEnemy')] },
        { fx: [dmg(3, 'oneEnemy'), { k: 'exileFrom', from: 'discard', n: 1, tag: B }, { k: 'ifEvent', n: 1 }, dmg(1.6, 'oneEnemy')] },   // 무덤 정리 — 버린 더미의 마탄 1장 소멸하고 한 방 더(레노아 「즉결 심판」 번뜩임)
        { fx: [{ k: 'discard', v: 1, tag: B }, dmg(3.4, 'oneEnemy'), { k: 'ifEvent', n: 1 }, dmg(1.6, 'oneEnemy')] },   // ① 써서 강화 — 마탄 1장을 버려(탄환) 한 방 더
        { fx: [dmg(2.1, 'allEnemies'), { k: 'perTag', id: B }, dmg(0.4, 'allEnemies')] },
      ],
      blesses: ['power', 'weakSpot', [M(1)]],
    },
    { // ③ 마무리 ◆ — 보존(쥐고 넘기며 마탄을 모은 턴에) · 손의 마탄 모두(없으면 한 번)
      name: '마.탄.의.사.수', cost: 1, type: '공격', tags: ['보존'],
      fx: [dmg(1.2, 'oneEnemy'), allB, per, dmg(2.2, 'oneEnemy')],   // 쥔 수에 비례 — 4장이면 120 + 880% + 탄환 4발(9장 덤 없음 — 사용자 「굳이 9장 다 안 모아도 세게」)
      oracles: [
        { power: true, fx: [M(2), stack('사수의 긍지', 1), power([rule({ on: 'discard', tag: B }, [stack(K, 1)])])] },
        { tags: ['보존'], fx: [dmg(1.2, 'oneEnemy'), { k: 'exileFrom', from: 'discard', all: true, tag: B }, { k: 'perEvent', n: 1 }, dmg(0.8, 'oneEnemy')] },   // 무덤 정리 — 버린 더미의 마탄 모두 소멸, 1장당(레노아 「칠흑의 송시」 번뜩임)
        { tags: ['보존'], fx: [{ k: 'discard', v: 3, tag: B }, per, dmg(2.6, 'oneEnemy'), stack('사수의 긍지', 1)] },
        { cost: 0, tags: ['보존'], fx: [allB, { k: 'perDiscarded' }, dmg(1.6, 'oneEnemy')] },
        // 원작 보강(10-11 검토): 마탄 5발을 채워 쏘면 사기
        { tags: ['보존'], fx: [allB, per, dmg(2.6, 'oneEnemy'), { k: 'ifEvent', n: 3 }, status('사기', 2)] },
      ],
      blesses: ['power', 'cost', [M(1)]],
    },
    { // ① 써서 강화 — 장전을 채우고, 마탄 1장을 버려(탄환 발사) 긍지를 굳힌다
      name: '장전 완료★', cost: 1, type: '스킬',
      fx: [stack(K, 2), { k: 'discard', v: 1, tag: B }, { k: 'ifEvent', n: 1 }, stack('사수의 긍지', 1), draw(1)],
      oracles: [
        { fx: [stack(K, 3), draw(1)] },
        { cost: 0, fx: [{ k: 'pull', from: 'discard', n: 3, tag: B }, stack(K, 1)] },   // 버린 더미의 마탄 3장까지 손으로
        { fx: [{ k: 'discard', v: 1, tag: B }, draw(2), stack(K, 1)] },
        { power: true, fx: [stack(K, 2), power([rule({ on: 'turnStart' }, [stack(K, 1)])])] },
        { fx: [stack(K, 2), draw(1), M(1, { owner: 'other' })] },
      ],
      blesses: ['draw', 'cost', [M(1)]],
    },
  ],
  tokens: [
    { // 10-13 리뉴얼: 0코 → 1코 · 내면 세게(피해 · 장전), 버리면(턴 끝 포함) 공짜 탄환 · 손에 9장까지(넘치면 바로 쏜다) · 「소멸」 은 탄환 규칙에 들어 있다
      id: B, name: '마탄', cost: 1, type: '공격', tags: ['탄환'], handMax: 9,
      fx: [dmg(2.4, 'oneEnemy'), { k: 'perStack', id: '사수의 긍지' }, dmg(0.2, 'oneEnemy'), stack(K, 1), { k: 'when', on: 'discard' }, stack(K, 1), { k: 'when', on: 'burn' }, stack(K, 1)],
      blurb: '1+1 로 받은 덤 한 발입니다. 손에는 9발까지 — 넘치면 그 자리에서 쏩니다',
    },
  ],
});
