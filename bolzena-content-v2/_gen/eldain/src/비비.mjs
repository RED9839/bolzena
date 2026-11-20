// 비비 — 탱커 · 순수 · 용족. 설계: _measure/엘다인_엔진/01_사도별.md §5 (4차 10-11 — 반격 탱커 · 도발)
// 엔진: 반격을 쌓고 → 「오시려구요?」 로 이번 적 차례 공격을 끌어와(▶E63 도발) 실드로 다 막아 반격 300% · 반격이 터진 적은 수은에 젖어 취약
//   → 반격이 터질 때마다 맞은 적에게 「수은 중독」(적 표식 · 1겹당 받는 피해 +8% — 10-12 밤 누구에게서든으로, 사용자 10-11 — 옛 「쇠사슬로 잠긴 상자」) → 「세계수 뿌리에 수은을」 로 반격을 모두 쏟는다 ◆. 자동 규칙은 수은 하나.
// 4차 결정 지점: 「오시려구요?」 0코 · 보존(적 의도를 보고 도발) · 「세계수 뿌리에 수은을」 보존(반격 · 취약이 깔린 뒤 쏟기).
// 고유 5장(01): 반격 쌓기 · 도발 · 반격 모양 · 반격으로 생긴 자원 쓰기 · 반격 쏟기 — 사용자가 정한 다섯 요소.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, perStack, power, rule } from '../lib.mjs';

const H = '비비', K = '수은 중독';
const S = (v = 1, x) => stack(K, v, x);
const ctr = (v = 1) => status('반격', v);
const taunt = (v = 0.5) => ({ k: 'taunt', v });   // ▶E63 — 그 대 비비 실드 +v
const brk = (n) => [ifStack(K, n)];   // 수은 중독 n 이상인 적이면
const dumpCtr = { k: 'spend', id: '반격', all: true };   // 반격(상태)을 모두 씀 — 쓴 수가 일의 값
const MINE = { on: 'guard', kind: 'shield', mine: true };   // (10-13 원작 「능력으로 실드 획득 시」) 비비 자신의 카드로 실드를 얻으면

export default hero({
  id: H, name: '비비', nature: '순수', race: '용족', role: '탱커', star: 3,
  blurb: '수은 실드를 두른 귀족 아가씨. 적의 공격을 자기에게 끌어와 받아 내고, 받은 만큼 수은 창으로 되갚습니다. 되갚은 적은 수은에 중독되어 파티 모두에게 더 아프게 맞습니다.',
  keyword: {
    // 원작 수은(방깎) — 2026-10-12 밤 사용자: 「1겹당 비비에게 받는 피해 +20%」 → 「1겹당 받는 피해 +8%(누구에게서든)」 — 파티 전체 피해 증가(최대 5겹 +40%, 취약 ×1.5 와 따로 곱함)
    name: K, desc: '비비의 수은이 스며든 적', carrier: 'enemy', cap: 5,
    per: [{ stat: 'taken', v: 0.08 }],
    rules: [{ name: '수은에 젖어', when: { on: 'counter' }, fx: [S(1, { target: 'oneEnemy' })] }],   // 반격이 터지면 맞은 적에게 수은 중독 +1(옛 취약 1 은 수은으로 합침)
  },
  // 2026-10-13 사용자: 보조 고유 효과 「수은 갑주」(1겹당 반격 피해 +20%) 지움 — 원작 1:1 로 쓰는 곳이 모두 원작 효과(반격 피해 +30% · 반격 1당)로 바뀌었다
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    // 2026-10-13 원작 1:1(사용자 「패시브도 원작 패시브로」): 옛 「소녀가 보호하겠사와요」(턴 끝 실드 · 반격 1)는 시동 카드와 이름 · 효과가 같아 뺐다.
    //   원작 전투 패시브 = 반격(맞으면 방어 기반 150%, 실드로 다 막으면 300%, 최대 10 — 10-31 고침: 옛 「100/200%」 는 틀림) — 엔진 공통 반격 규칙(150/300% · 최대 10)이 이미 그대로 맡는다.
    //   그래서 패시브 칸에는 원작 수집 문서의 전투 패시브 강화(돌파 6) 「턴 시작 시 반격 강화 1」 — 반격 강화 1 은 그 턴 반격 피해 +30% 로(「창 네 자루」 신탁 3 과 같은 값).
    //   원작 반격 강화는 반격 피해 +30% · 최대 2겹 · 반격이 터지면 사라진다 — 엔진 counterMod 에 겹 · 소모가 없어 「그 턴만 +30%」(10-31).
    { name: '되갚을 준비', when: { on: 'turnStart' }, fx: [{ k: 'counterMod', v: 0.3, turns: 1 }] },   // 원작 값 맞춤(10-31): +50 → +30%(원작 반격 강화 +30% — 10-21 의 「+50%(원작값)」 은 틀림)   // 10-21 밸런스 조절: +100 → +50%   // 세기 맞춤(10-13 사용자): 원작 반격 강화 1(+50% 로 옮김) → 세기 맞춤 +100%
  ],
  ult: { name: '퀵실버 랜스', cost: 200, fx: [ddmg(3), perStack('반격'), ddmg(0.3)] },   // 원작 「절대 영도」 — 방어 기반 300% + 반격 1당 +30%(돌파 3 값 · 10-13 엔진이 파티 상태 「반격」 겹을 센다)
}, {
  basicNames: ['은빛 발톱', '수은 창', '귀족의 품위'], deck: 'heavy',
  uniques: [
    // 2026-10-13 마그나식 교체(사용자 「비비도 마그나와 같이 만들어 줘」): 카드 5장 = 카제나 마그나 시동 · 고유 2~4 · 완성형, 신탁 5갈래 = 마그나 번뜩임 5갈래(수치 카제나 그대로).
    // 2026-10-13 2차(사용자 「원작대로 다 맞추기」): 결정화 = 엔진 상태 「결정화」(파티 · 턴 끝 고정 실드 20%/겹 · 최대 10 — 원작과 같은 규칙) · 반격 전체화 n턴(counterAll turns) ·
    //   반격 수 비례(perStack 반격) · 적 수 비례(perFoe) · 맞은 적 취약(hitFx) · 실드 보존 · 「능력으로 실드를 얻을 때」 = 비비 자신의 카드로(guard mine).
    //   남은 근사: 피해 기반 실드 50% → 실드 150%(엔진에 없음) · 완성형(창 네 자루)은 원작 번뜩임이 없어 에고 발현으로 채움. 이전 판: C:\projects\_backup\bibi_magna2_20261013
    { // 시동 ← 얼음 파편 — 열기: 결정화 2 + 턴 시작 반격 1
      name: '소녀가 보호하겠사와요', cost: 1, type: '강화', tags: ['주도'],
      fx: [status('결정화', 3), power([rule({ on: 'turnStart' }, [ctr(1)])])],   // 원작 결정화 2 → 세기 맞춤 4 → 3(10-21 밸런스 조절 7차)
      oracles: [
        { power: true, tags: ['주도'], fx: [status('결정화', 7), power([rule({ on: 'turnStart' }, [ctr(1)])])] },   // I 결정화 5 · 원작 5 → 세기 맞춤 7
        { power: true, tags: ['주도'], fx: [status('결정화', 5), power([rule({ on: 'turnStart' }, [ctr(2)])])] },   // II 결정화 3 · 반격 2 · 원작 3 → 세기 맞춤 5
        { power: true, tags: ['개전'], fx: [status('결정화', 5), { k: 'counterMod', v: 0.3, run: true }, power([rule({ on: 'turnStart' }, [ctr(1)])])] },   // III 개전 · 반격 피해 +30%(전투 내내 · 원작 추천) · 원작 3 → 세기 맞춤 5
        { power: true, cost: 2, tags: ['유일', '주도'], fx: [status('결정화', 5), power([rule({ on: 'turnEnd' }, [ddmg(2, 'allEnemies')])])] },   // IV 턴 끝 적 전체 방어 기반 200% · 원작 3 → 세기 맞춤 5
        { type: '스킬', tags: ['주도'], fx: [ctr(4), { k: 'counterMod', v: 1, turns: 1 }] },   // V 1코 스킬 — 반격 4 · 다음 반격 +100%(엔진은 이번 턴 반격 ×2 — 적의 차례까지)
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    { // 고유 3 ← 빙벽 — 굴리기: 실드 + 반격 + 2턴간 반격이 적 전체로
      name: '오시려구요?', cost: 2, type: '스킬',
      fx: [shield(2.17), ctr(2), { k: 'counterAll', turns: 2 }],   // 원작 값 맞춤(10-31): 반격 전체화 1 → 2턴(원작 빙벽 2턴)   // 10-21 밸런스 조절 3차: 290 → 260%   // 원작 실드 200%(10-31 고침 — 옛 「180%」 는 틀림) → 세기 맞춤 240%(×1.2 전)
      oracles: [
        { fx: [shield(3), ctr(3), { k: 'counterAll', turns: 2 }] },                                    // I 수치 · 2턴
        { fx: [shield(3), ctr(2), perStack('반격'), shield(0.3)] },                                     // II 반격 1당 실드 +30%
        { power: true, tags: ['주도'], fx: [shield(2), { k: 'counterAll' }] },                       // III 강화 — 반격이 적 전체로(원작 BEST)
        { type: '공격', fx: [shield(2), { k: 'dmg', ofShield: true, ofEvent: 1, target: 'allEnemies' }] },   // IV 실드 200% 뒤 지금 파티 실드만큼 적 전체 고정 피해
        { fx: [shield(2), ctr(3), { k: 'counterAll', turns: 3 }] },                                    // V 3턴 · 원작 값 맞춤(10-31): 2 → 3턴
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    { // 완성형 ← 혹한의 폭풍 — 주도 강화: 자신(비비)의 카드로 실드를 얻을 때마다 반격 1(원작 「능력으로 실드 획득 시」 — 결정화 · 패시브 실드 · 동료 카드는 빼고)
      name: '창 네 자루', cost: 1, type: '강화', tags: ['주도'],
      fx: [status('결정화', 2), power([rule(MINE, [ctr(1)])])],   // 원작 값 맞춤(10-31): 결정화 2 더함(원작 혹한의 폭풍 「결정화 2 + 실드 얻을 때 반격 1」)
      oracles: [
        { power: true, tags: ['주도', '개전'], fx: [status('결정화', 3), power([rule(MINE, [ctr(1)])])] },   // 에고 발현 1 — 개전 · 결정화 +1(원작 값 맞춤 10-31: 결정화 3)
        { power: true, tags: ['주도'], fx: [status('결정화', 2), power([rule(MINE, [ctr(1)]), rule({ on: 'counter' }, [S(1, { target: 'oneEnemy' })])])] },   // 반격한 적에 수은 한 겹 더(에고 발현 2 결빙 대신) · 원작 값 맞춤(10-31): 기본처럼 결정화 2(엔진이 강화 규칙 「이 전투 동안」 을 첫 줄에만 붙여 45자 안에 든다)
        { power: true, tags: ['주도'], fx: [status('결정화', 2), { k: 'counterMod', v: 0.3, run: true }, power([rule(MINE, [ctr(1)])])] },   // 에고 발현 6 — 반격 강화 · 원작 값 맞춤(10-31): +50 → +30%(원작 최대 2겹 · 터지면 사라짐 대신 전투 내내 +30% — 패시브와 겹치면 +60%, 원작 2겹과 같은 끝) · 기본처럼 결정화 2 · 글 45자 안에 들게 「매 턴 시작 시 이번 턴 +30%」 규칙을 같은 효과의 전투 내내(run)로
        { power: true, tags: ['주도'], fx: [{ k: 'counterHits', v: 2, ratio: 1 }, power([rule(MINE, [ctr(1)])]), status('결정화', 2)] },   // 반격이 두 번 친다(옛 창 네 자루) · 원작 값 맞춤(10-31): 기본처럼 결정화 2 — 글 45자 안에 들게 맨 뒤로(쉼표 없이 따로 한 줄)
        { power: true, tags: ['주도'], fx: [status('결정화', 2), { k: 'counterAll' }, power([rule(MINE, [ctr(1)])])] },   // 10-31 기본을 따라 결정화 2
      ],
      blesses: ['cost', 'draw', [S(1)]],
    },
    { // 고유 4 ← 냉기 충전 — 굴리기: 실드 + 적 전체 취약
      name: '수은 방울', cost: 1, type: '스킬',
      fx: [shield(1.4), status('취약', 3, { target: 'allEnemies' })],   // 원작 값 맞춤(10-31): 취약 2 → 3 · 실드 140% 는 원작값(×1.2 전 — 옛 주석 「원작 100% → 세기 맞춤」 은 틀림)
      oracles: [
        { fx: [shield(2.1), status('취약', 3, { target: 'allEnemies' }), { k: 'perFoe' }, shield(0.6)] },   // I 적 1명당 실드 +60%
        { fx: [shield(2.1), status('취약', 4, { target: 'allEnemies' })] },                          // II
        { power: true, fx: [status('결정화', 6), power([rule({ on: 'counter' }, [status('취약', 3, { target: 'oneEnemy' })])])] },   // III 결정화 4 · 반격한 적에 취약 3(원작 BEST) · 원작 결정화 4 → 세기 맞춤 6
        { type: '공격', fx: [ddmg(1.5, 'randomEnemy', { hits: 2, hitFx: [status('취약', 1)] })] },   // IV 무작위 3연타 · 맞은 적 취약 1 · 10-19 타수: 3회 × 100% → 2회 × 150%(취약 3 → 2)
        { fx: [status('취약', 3, { target: 'allEnemies' }), ctr(3)] },                                 // V
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    { // 고유 2 ← 빙하의 철권 — 터뜨리기: 방어 기반 큰 한 방 + 실드
      name: '세계수 뿌리에 수은을', cost: 2, type: '공격',
      fx: [ddmg(3.3), shield(2)],   // 10-21 밸런스 조절: 300 → 330%   // 원작 피해 기반 실드 50%(근사 150%) → 세기 맞춤 200%(×1.2 전)
      oracles: [
        { fx: [ddmg(4.5), shield(1.5)] },                                                              // I 수치
        { fx: [ddmg(3), status('취약', 3, { target: 'oneEnemy' })] },                                 // II 취약 3
        { fx: [ddmg(4.5), { k: 'ifShield' }, ddmg(2.2)] },                                             // III 실드가 있으면 피해 +50%
        { fx: [ddmg(3, 'allEnemies'), shield(1.5)] },                                                  // IV 적 전체
        { tags: ['소멸 2'], fx: [ddmg(4), shield(1.5), status('실드 보존', 1)] },                     // V 소멸 2 · 1턴간 실드 보존 1
      ],
      blesses: ['power', 'cost', [S(1)]],
    },
  ],
});
