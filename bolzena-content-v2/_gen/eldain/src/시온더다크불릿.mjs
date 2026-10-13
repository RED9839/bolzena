// 4차 덧붙임(사용자 10-11): 「가장 먼 적」 대상을 뺐다 — 적 1명(고름).
// 시온 더 다크불릿 — 딜러 · 우울 · 유령. 설계: _measure/엘다인_엔진/01_사도별.md §1 (4차 10-11 — 구조 그대로 · 가벼운 덱 · 시동 ④ 눈가림 · 마무리 ⑤ 5발 사기)
// 엔진: 「마탄」 을 만들어 손에 쥐고 → 버리면 쏘고(탄환) · 내면 소멸하며 쏘고 → 어느 쪽이든 「이 몸, 등장★」(이번 턴 장전) → 쥔 마탄 수 · 장전으로 큰 저격
// 쌓기는 카드가 한다(마탄의 안식 · 소각 마디). 자동 규칙은 「어.둠.의.구.원.자★」 하나.
// 2026-10-13 리뉴얼(사용자): 마탄 0코 → 1코 · 탄환(카제나식 — 사용하면 소멸, 버린 더미로 가면 턴 끝 포함 무작위 적에게 추가 공격) · (손 9장 한도 · 넘치면 발사는 10-13 사용자 요청으로 없앰).
//   쓰임 세 갈래 — 써서 강화(장전 완료★ · 대물 ④) · 쥐면 강화(대물 저격총) · 털기(마.탄.의.사.수만 — 보존, 쥔 수에 비례해 3~5장이면 주력 한 방).
// 2026-10-13 밤 「버려서 쏘기」(사용자 결정 1~7 모두) — 원작 → 우리:
//   원작 레노아는 탄환 직접 160% : 버림 120% 라 탄환은 0 AP 로 버려서 쏘고 AP 는 다른 공격에 쓴다(딜 대부분이 AP 밖).
//   우리는 직접 320% + 장전 2 라 직접 쏘기가 2.7배 이득 → 마탄이 1코 공격 카드처럼 굴렀다. 그래서
//   ① 마탄 직접 320 → 낮춤 · 버림 탄환 배율을 시온 마탄만 올림(bulletRatio — 교주 카드 탄환은 그대로)
//   ② 턴 끝 탄환은 엔진이 장전(endClear)이 사라지기 전에 쏜다 ③ 탄창 = 탄환으로 쏠 때마다 쌓임(원작 「장미의 결의」 — 버릴 때마다 +10%, 섞을 때까지 → 엔진에 「섞을 때 사라짐」 이 없어 전투 동안 · 최대 10, 1당 +5%)
//   ④ 전투 시작 마탄 2장(탄창 startMake — 규칙이 아니라 고유 능력 규칙 2개에 안 든다) ⑤ 어.둠.의.구.원.자★ 턴당 1번 제한 풀기 ⑥ 다크넷 접속 기본 3장 버리기(원작 BEST). 백업 C:\projects\_backup\sion_discardshot_20261013
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
    // 10-13 밤(사용자 — 마탄 글 67자 > 55자): 마탄 fx 의 「이 몸, 등장★」 1 세 번(내기 · 안식 · 소각)을 여기로 옮겼다 — 엔진이 세는 수(tally, 규칙 아님 · 고유 능력 규칙 2개에 안 든다).
    //   마탄을 내면 +1 · 버려지면(효과로 — 탄환 발사) +1 · 소멸하면(소각) +1. 동작은 옛 마탄 fx 그대로 — 내면 「내기 +1」 과 제 소멸의 「소각 +1」 이 겹쳐 +2.
    //   (내기를 +1 로만 해 봤더니 봇 완주 63.0 → 57.5% 라 옛 동작을 지킨다.)
    tally: ['play', 'discard', 'exhaust'], tallyTag: B,
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  // 2026-10-13 사용자 「사수의 긍지 말고 다른 패시브」 → A 「탄창」: 손에 쥔 마탄 1장당 시온 피해 +5%(엔진 keyword hand — 손 장수를 겹이 저절로 따라간다, 최대 손 한도 10).
  //   옛 「사수의 긍지」(다크넷 접속 · 그 신탁 V 가 쌓고 마탄이 perStack 1당 +20%)는 쌓는 카드를 따로 내야 해서 엔진(마탄을 쥐고 버리고 내는 손)과 따로 놀았다.
  //   빠진 몫: 다크넷 접속 · 신탁 I 은 이번 턴 장전 1, 신탁 V 는 회수 3 → 4장, 마탄은 직접 피해 300 → 320%.
  //   10-13 밤 「버려서 쏘기」: 손에 쥐면 +5% 는 버리면 0 이 되어 버려서 쏘는 엔진과 거꾸로 놀았다 → 탄환으로 쏠 때마다 +1(전투 동안 · 최대 10, 1당 +5%) · 전투 시작 마탄 2장.
  keywords: [{ name: '탄창', desc: '쏟아 낸 탄환만큼 달아오르는 총신', carrier: 'self', cap: 10, tally: ['bullet'], tallyTag: B, per: [{ stat: 'dealt', v: 0.05 }] }],   // 10-13 밤 사용자: 전투 시작 마탄 0장(알바 턴 시작 1장만) · 탄창 1당 5% · 탄환 공통 120% — 혼자 70.0%
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    { name: '어.둠.의.구.원.자★', when: { on: 'discard', by: 'other' }, fx: [M(1)] },   // 10-13 밤: 턴당 1번(once) 풀기 — 다른 아군이 버리게 한 장수만큼
    { name: '편의점 야간 알바', when: { on: 'turnStart' }, fx: [M(1)] },   // 10-13 밤 사용자: 턴 시작마다 마탄 1장(출근할 때마다 덤 한 발 — 옛 「AP 남기고 턴 마치면」 은 AP 를 마탄에 안 쓰는 흐름과 부딪혀 없앰)
  ],
  ult: { name: '종.말.의.폭.발★', cost: 150, fx: [{ k: 'pull', from: 'draw', all: true, tag: B }, { k: 'pull', from: 'discard', all: true, tag: B }, dmg(3, 'oneEnemy'), stack(K, 5)] },   // 10-13 레노아식(사용자): 「검은 가시」 그대로 — 뽑을 · 버린 더미의 마탄 모두 손으로 + 피해 300%
}, {
  basicNames: ['저격', '견제 사격', '검은 망토'], deck: 'light',
  uniques: [
    // 2026-10-13 레노아식 교체(사용자 「시온 카드들 레노아처럼 교체」): 카드 5장 = 카제나 레노아 시동 · 고유 2~4 · 완성형, 신탁 5갈래 = 레노아 번뜩임 5갈래(수치 카제나 그대로).
    //   엔진에 없는 것만 가까운 꼴로 — 뽑을 더미에서 버리기 → 뽑을 더미의 마탄을 손으로 끌어 버림 · 「탄환 추가 공격 +100%」 → 장전 가득 · 「회수 부여」 → 버린 마탄을 손으로 · 「마탄이 손에 있으면 드로우 1」 → 드로우 3.
    //   완성형(마.탄.의.사.수)은 원작에 번뜩임이 없어 예전 시온 신탁을 레노아 쪽으로 다듬어 둔다. 고유 능력 · 고학년 · 마탄은 그대로. 이전 판: C:\projects\_backup\sion_before_renoa_20261013
    { // 시동 ← 비탄의 메아리 — 열기
      name: '원 플러스 원', cost: 1, type: '공격',
      fx: [dmg(1.4, 'oneEnemy'), M(1)],
      oracles: [
        { fx: [dmg(1.4, 'oneEnemy'), M(2)] },                                                          // I 생성량
        { fx: [dmg(1.4, 'oneEnemy'), M(1), M(2, { to: 'discard' })] },                                // II 버린 더미에도 2장
        { fx: [dmg(1.4, 'oneEnemy'), { k: 'discard', v: 2, tag: B }, M(2)] },                         // III 쥔 마탄 2장을 버려 쏘고 새로 2장(원작 「뽑을 더미의 탄환 버리기」 대신)
        { type: '스킬', fx: [M(3)] },                                                                  // IV 스킬로 3장(원작 BEST)
        { power: true, tags: ['개전'], fx: [M(1), power([rule({ on: 'turnStart' }, [M(1)])])] },     // V 개전 강화 — 턴 시작마다 1장
      ],
      blesses: ['power', 'draw', [M(1)]],
    },
    { // 고유 4 ← 운명을 삼킨 꽃 — 손을 버려(마탄이면 탄환 발사) 그 수만큼 새 마탄
      name: '다크넷 접속', cost: 0, type: '스킬',
      fx: [discard(3), { k: 'perDiscarded' }, M(1), stack(K, 1)],   // 10-13 탄창: 사수의 긍지 1 → 이번 턴 장전 1 · 10-13 밤: 2 → 3장(원작 BEST 를 기본으로 — 버려서 쏘기의 시동)
      oracles: [
        { fx: [{ k: 'discard', all: true }, { k: 'perDiscarded' }, M(1), stack(K, 2)] },                            // I 손패 모두 버리고 1장당 마탄 1 · 장전 2(10-13 밤 — 3장 버리기는 기본으로 올라가 다른 갈래로)
        { fx: [{ k: 'discard', all: true, who: 'other' }, { k: 'perDiscarded' }, M(2)] },   // II 다른 사도의 카드 모두 버리고 1장당 2장(원작 1코 · 소멸 — 검사기 규칙에 맞춰 0코)
        { fx: [discard(2), { k: 'perDiscarded' }, stack(K, 2)] },                                     // III 버린 만큼 이번 턴 장전
        { fx: [draw(2), M(1)] },                                                                       // IV 드로우 2 + 마탄 1(원작 1코 「드로우 2, 탄환 있으면 1 더」)
        { fx: [{ k: 'pull', from: 'discard', n: 4, tag: B }] },                                        // V 회수 대신 — 버린 마탄 4장을 손으로(10-13 탄창: 사수의 긍지 1 대신 3 → 4장 — 쥔 마탄이 곧 탄창)
      ],
      blesses: ['draw', 'ap', [stack(K, 1)]],
    },
    { // 10-13 강인도(사용자 「해 줘」): 단일 공격에 강인도 피해 1 — 탄환 추가 공격은 원작대로 0.5칸이라 격파가 모자랐다(판당 6.5회)
    // 고유 2 ← 즉결 심판 — 굴리기: 쥔 마탄 1장을 버려(탄환) 한 방 더
      name: '대물 저격총', cost: 1, type: '공격',
      fx: [dmg(1.8, 'oneEnemy'), { k: 'tough', v: 1 }, { k: 'discard', v: 1, tag: B }, { k: 'ifEvent', n: 1 }, dmg(1, 'oneEnemy')],
      oracles: [
        { fx: [dmg(2.7, 'oneEnemy'), { k: 'tough', v: 1 }, { k: 'discard', v: 1, tag: B }, { k: 'ifEvent', n: 1 }, dmg(1.5, 'oneEnemy')] },   // I 수치
        { fx: [dmg(2.2, 'oneEnemy'), { k: 'tough', v: 1 }, { k: 'pull', from: 'draw', n: 1, tag: B }] },   // II 뽑을 더미의 마탄을 손으로(원작 「뽑을 더미의 탄환을 버려 +120%」 대신)
        { fx: [dmg(2.2, 'oneEnemy'), { k: 'tough', v: 1 }, { k: 'exileFrom', from: 'discard', n: 1, tag: B }, { k: 'ifEvent', n: 1 }, dmg(2.2, 'oneEnemy')] },   // III 버린 더미 마탄 1장 소멸하고 한 번 더
        { fx: [dmg(1.8, 'oneEnemy'), { k: 'tough', v: 1 }, { k: 'when', on: 'discard' }, dmg(2.5, 'randomEnemy')] },   // IV 이 카드가 버려지면 무작위 적 250%(원작 2코 — 검사기 규칙에 맞춰 1코)
        { power: true, fx: [power([rule({ on: 'turnEnd' }, [dmg(2, 'lowEnemy')])])] },               // V 턴 끝마다 HP 가장 낮은 적 200%(원작 BEST)
      ],
      blesses: ['power', 'weakSpot', [M(1)]],
    },
    { // 완성형 ← 결사의 일격 — 보존 · 쥔 마탄 모두 버려(탄환 발사) 1장당 +50%
      name: '마.탄.의.사.수', cost: 1, type: '공격', tags: ['보존'],
      fx: [dmg(1.5, 'oneEnemy'), { k: 'tough', v: 1 }, allB, { k: 'perDiscarded' }, dmg(0.7, 'oneEnemy')],   // 1장당 50 → 70%(레노아 에고 발현 1 몫)
      oracles: [
        { tags: ['보존'], fx: [dmg(1.5, 'oneEnemy'), { k: 'tough', v: 1 }, allB, { k: 'perDiscarded' }, dmg(0.9, 'oneEnemy')] },   // 1장당 +90%(원작 에고 발현 1 겹)
        { tags: ['보존'], fx: [dmg(1.5, 'oneEnemy'), { k: 'tough', v: 1 }, { k: 'exileFrom', from: 'discard', all: true, tag: B }, { k: 'perEvent', n: 1 }, dmg(0.5, 'oneEnemy')] },   // 버린 더미 마탄 모두 소멸, 1장당
        { cost: 2, tags: ['보존'], fx: [dmg(2.5, 'oneEnemy'), { k: 'tough', v: 1 }, allB, { k: 'perDiscarded' }, dmg(0.8, 'oneEnemy')] },
        { cost: 0, tags: ['보존'], fx: [allB, { k: 'perDiscarded' }, dmg(0.6, 'oneEnemy'), { k: 'tough', v: 1 }] },
        { tags: ['보존'], fx: [allB, { k: 'perDiscarded' }, dmg(0.7, 'oneEnemy'), { k: 'tough', v: 1 }, { k: 'ifEvent', n: 3 }, status('사기', 2)] },   // 3장 넘게 털면 사기
      ],
      blesses: ['power', 'cost', [M(1)]],
    },
    { // 고유 3 ← 칠흑의 송시 — 터뜨리기: 3연타, 쥔 마탄 1장당 +20%(3번)
      name: '장전 완료★', cost: 1, type: '공격',
      fx: [dmg(0.5, 'oneEnemy', { hits: 3 }), { k: 'perTag', id: B }, dmg(0.2, 'oneEnemy', { hits: 3 })],
      oracles: [
        { fx: [dmg(0.8, 'oneEnemy', { hits: 3 }), { k: 'perTag', id: B }, dmg(0.3, 'oneEnemy', { hits: 3 })] },   // I 수치
        { type: '스킬', fx: [{ k: 'pull', from: 'draw', n: 3, tag: B }, { k: 'pull', from: 'discard', n: 3, tag: B }] },   // II 뽑을 · 버린 더미의 마탄을 손으로
        { fx: [dmg(0.5, 'oneEnemy', { hits: 3 }), status('표식', 2, { target: 'oneEnemy' })] },   // III 표식 2(원작 「쥔 탄환 수만큼」 — 엔진은 상태 앞 「1개당」 없음)
        { fx: [dmg(0.5, 'oneEnemy', { hits: 3 }), stack(K, 5)] },                                     // IV 이번 턴 장전 가득(원작 「탄환 추가 공격 +100%」)
        { fx: [dmg(0.5, 'oneEnemy', { hits: 3 }), { k: 'exileFrom', from: 'discard', all: true, tag: B }, { k: 'perEvent', n: 1 }, dmg(0.5, 'oneEnemy', { hits: 3 })] },   // V 버린 더미 마탄 모두 소멸, 1장당 +50%(원작 BEST)
      ],
      blesses: ['draw', 'cost', [M(1)]],
    },
  ],
  tokens: [
    { // 10-13 레노아식 보정(사용자 「해 줘」): 직접 쓰면 240 → 300%(봇 완주 52 → 56%, 교체 전 수준). 10-13 리뉴얼: 0코 → 1코 · 내면 세게(피해 · 장전), 버리면(턴 끝 포함) 공짜 탄환 · 「소멸」 은 탄환 규칙에 들어 있다
      id: B, name: '마탄', cost: 1, type: '공격', tags: ['탄환', '소멸'],   // 10-13 저녁 사용자: 「소멸」 키워드가 카드에 보이게(동작은 탄환 규칙과 같음)
      // 10-13 탄창(사용자): 사수의 긍지 1당 +20% 를 빼고 직접 피해 300 → 320%(손에 쥔 다른 마탄 1장당 +5% 는 탄창이 준다)
      // 10-13 밤 「버려서 쏘기」: 직접 320 → 160%(원작 탄환 직접 160%) · 버림 탄환 120 → 150%(bulletRatio — 시온 마탄만 · 혼자 기준 160% 73.2% → 150% 로 목표 68~72% 에). 원작 160 : 120 보다 버림 쪽을 더 키워 0 AP 로 버려서 쏘는 쪽이 낫게.
      fx: [dmg(1.6, 'oneEnemy')],   // 10-13 밤: bulletRatio 1.5 뺌 — 턴 시작 마탄으로 공급이 늘어 탄환은 공통 120% 로   // 장전 +1(내기 · 버려짐 · 소각)은 「이 몸, 등장★」 tally 로(10-13 밤 — 글 55자)
      blurb: '1+1 로 받은 덤 한 발입니다',
    },
  ],
});
