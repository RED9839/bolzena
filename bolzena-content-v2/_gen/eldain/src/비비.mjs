// 비비 — 탱커 · 순수 · 용족. 설계: _measure/엘다인_엔진/01_사도별.md §5 (4차 10-11 — 반격 탱커 · 도발)
// 엔진: 반격을 쌓고 → 「오시려구요?」 로 이번 적 차례 공격을 끌어와(▶E63 도발) 실드로 다 막아 반격 300% · 반격이 터진 적은 수은에 젖어 취약
//   → 반격이 터질 때마다 맞은 적에게 「수은 중독」(적 표식 · 1겹당 받는 피해 +8% — 10-12 밤 누구에게서든으로, 사용자 10-11 — 옛 「쇠사슬로 잠긴 상자」) → 「세계수 뿌리에 수은을」 로 반격을 모두 쏟는다 ◆. 자동 규칙은 수은 하나.
// 4차 결정 지점: 「오시려구요?」 0코 · 보존(적 의도를 보고 도발) · 「세계수 뿌리에 수은을」 보존(반격 · 취약이 깔린 뒤 쏟기).
// 고유 5장(01): 반격 쌓기 · 도발 · 반격 모양 · 반격으로 생긴 자원 쓰기 · 반격 쏟기 — 사용자가 정한 다섯 요소.
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '비비', K = '수은 중독';
const S = (v = 1, x) => stack(K, v, x);
const ctr = (v = 1) => status('반격', v);
const taunt = (v = 0.5) => ({ k: 'taunt', v });   // ▶E63 — 그 대 비비 실드 +v
const brk = (n) => [ifStack(K, n)];   // 수은 중독 n 이상인 적이면
const dumpCtr = { k: 'spend', id: '반격', all: true };   // 반격(상태)을 모두 씀 — 쓴 수가 일의 값

export default hero({
  id: H, name: '비비', nature: '순수', race: '용족', role: '탱커', star: 3,
  blurb: '수은 실드를 두른 귀족 아가씨. 적의 공격을 자기에게 끌어와 받아 내고, 받은 만큼 수은 창으로 되갚습니다. 되갚은 적은 수은에 중독되어 파티 모두에게 더 아프게 맞습니다.',
  keyword: {
    // 원작 수은(방깎) — 2026-10-12 밤 사용자: 「1겹당 비비에게 받는 피해 +20%」 → 「1겹당 받는 피해 +8%(누구에게서든)」 — 파티 전체 피해 증가(최대 5겹 +40%, 취약 ×1.5 와 따로 곱함)
    name: K, desc: '비비의 수은이 스며든 적', carrier: 'enemy', cap: 5,
    per: [{ stat: 'taken', v: 0.08 }],
    rules: [{ name: '수은에 젖어', when: { on: 'counter' }, fx: [S(1, { target: 'oneEnemy' })] }],   // 반격이 터지면 맞은 적에게 수은 중독 +1(옛 취약 1 은 수은으로 합침)
  },
  // 보조 고유 효과(사용자 10-11 — 란처럼 2개씩)
  keywords: [{ name: '수은 갑주', desc: '되갚을수록 두꺼워지는 수은 갑주', carrier: 'self', cap: 5, per: [{ stat: 'counter', v: 0.2 }] }],   // 10-11 밸런스 10→15% · 10-12 탱커 손질(반격 정체성) 15→20%
  // 사용자 10-11: 20명 모두 고유 능력 2개
  passives: [
    // 10-12 탱커 손질: 옛 「공격을 다 막으면 반격 1」(다 막아야 돌아 거의 안 돎) → 턴이 끝날 때 파티 실드 · 반격 1(적의 차례를 받아 내며 되갚을 준비 — 규칙 2개 한도라 하나로)
    { name: '소녀가 보호하겠사와요', when: { on: 'turnEnd' }, fx: [shield(0.5), ctr(1)] },
  ],
  ult: { name: '퀵실버 랜스', cost: 200, fx: [ddmg(3.5, 'oneEnemy'), { k: 'dealtMod', v: -0.3, target: 'allEnemies' }, taunt(1), ctr(5)] },   // 10-12 고학년 손질(「탱 · 폿 고학년은 쓰는 느낌이 안 난다」 — 비용 그대로, 효과만): 적 피해 -20→-30% · 도발 실드 1.5→2배 · 반격 3→5
}, {
  basicNames: ['은빛 발톱', '수은 창', '귀족의 품위'], deck: 'heavy',
  uniques: [
    { // 시동
      name: '소녀가 보호하겠사와요', cost: 1, type: '스킬',
      fx: [shield(2), ctr(2), stack('수은 갑주', 1)],   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [shield(1.6), ctr(3), stack('수은 갑주', 1)] },   // 10-13 신탁 점검: 수은 갑주 되살림
        { fx: [shield(2.6), ctr(1)] },
        { power: true, fx: [shield(1.2), ctr(2), power([rule({ on: 'turnStart' }, [ctr(1)])])] },   // 10-13 신탁 점검 3차   // 10-13 신탁 점검: 실드 140%
        // 원작 수은 보호막(06 · 08) — 실드가 깨지면 적 전체 취약 · 광역
        { power: true, fx: [shield(1.6), power([rule({ on: 'shieldBreak' }, [status('취약', 1, { target: 'allEnemies' }), ddmg(0.8, 'allEnemies')])])] },
        { power: true, fx: [shield(1.6), power([rule({ on: 'hurt', guarded: true }, [shield(0.8)])])] },   // 10-13 신탁 점검: 실드 120→190% · 70→100%
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    { // 4차: 0코 · 보존 — 큰 공격이 오는 턴에(결정 지점)
      name: '오시려구요?', cost: 0, type: '스킬', tags: ['보존'],
      fx: [taunt(), ctr(1)],
      oracles: [
        { tags: ['보존'], fx: [taunt(0.8), ctr(1)] },
        { tags: ['보존'], fx: [taunt(), status('약화', 1, { target: 'allEnemies' })] },
        { tags: ['보존'], fx: [taunt(), ctr(1), status('취약', 1, { target: 'oneEnemy' })] },
        { cost: 1, tags: ['보존'], fx: [taunt(), ctr(3), shield(1)] },   // 10-13 신탁 점검: 실드 덧붙임
        { tags: ['보존'], fx: [ctr(2), S(2)] },   // 10-13 신탁 점검
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    {
      name: '창 네 자루', cost: 1, type: '강화',
      fx: [{ k: 'counterHits', v: 2, ratio: 1 }],   // 10-12 탱커 손질 80→100%
      oracles: [
        { fx: [{ k: 'counterHits', v: 2, ratio: 1.1 }] },
        { fx: [{ k: 'counterAll' }] },
        { fx: [{ k: 'counterHits', v: 2, ratio: 1 }, power([rule({ on: 'counter' }, [S(1)])])] },
        { fx: [{ k: 'counterHits', v: 4, ratio: 0.5 }] },
        { tags: ['개전'], fx: [{ k: 'counterHits', v: 2, ratio: 1.1 }] },   // 10-13 신탁 점검
      ],
      blesses: ['cost', 'draw', [S(1)]],
    },
    {
      name: '수은 방울', cost: 0, type: '스킬',
      fx: [ctr(1), shield(1.2), stack('수은 갑주', 1), ...brk(2), ctr(2)],   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { fx: [ctr(2), shield(1.2), stack('수은 갑주', 1), ...brk(2), ctr(3)] },   // 10-13 신탁 점검: 실드 · 수은 갑주 되살림
        { type: '공격', fx: [ctr(1), shield(0.8), ...brk(2), ddmg(0.8, 'allEnemies')] },
        { fx: [ctr(1), shield(1), ...brk(2), { k: 'counterMod', v: 1, turns: 1 }] },   // 10-13 신탁 점검: 실드 160→190%
        { fx: [ctr(1), shield(1), draw(1)] },   // 10-12 사용자 「신탁 소멸 줄여 줘」 · 10-13 신탁 점검: 실드 덧붙임
        { fx: [ctr(1), shield(0.8), ...brk(2), shield(2)] },
      ],
      blesses: ['guard', 'draw', [S(1)]],
    },
    { // 마무리 ◆ — 보존 · 반격을 모두 써서 1개당(최소 1회)
      name: '세계수 뿌리에 수은을', cost: 2, type: '공격', tags: ['보존'],
      fx: [dumpCtr, { k: 'perEvent', n: 1 }, ddmg(0.7, 'allEnemies')],   // 10-11 밸런스(사용자 「다 해 줘」)
      oracles: [
        { type: '스킬', tags: ['보존'], fx: [dumpCtr, { k: 'perEvent', n: 1 }, shield(0.7)] },   // 10-13 신탁 점검
        { tags: ['보존'], fx: [{ k: 'spend', id: '반격', v: 3 }, { k: 'perEvent', n: 1 }, ddmg(1.2, 'allEnemies')] },   // 10-13 신탁 점검 3차   // 10-13 신탁 점검: 3개까지 · 1당 60→90%
        { tags: ['보존'], fx: [{ k: 'spend', id: K, all: true }, { k: 'perEvent', n: 1 }, ddmg(1.4, 'allEnemies')] },   // 10-13 신탁 점검 3차   // 10-13 신탁 점검: 60→100%
        { cost: 1, tags: ['보존'], fx: [dumpCtr, { k: 'perEvent', n: 1 }, ddmg(1.2)] },   // 10-13 신탁 점검
        // 취약 정산(비비 소유)
        { tags: ['보존'], fx: [ctr(1), { k: 'ifFoe', id: '취약' }, dumpCtr, { k: 'perEvent', n: 1 }, ddmg(1.6)] },   // 10-13 신탁 점검 3차
      ],
      blesses: ['power', 'cost', [S(1)]],
    },
  ],
});
