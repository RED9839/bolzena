// 네르(빡침) — 딜러(섭딜) · 활발 · 요정 · 변신 「성전」.
// 2026-10-13 저녁 리뉴얼(사용자 「섭딜로」 · 추격형 · 휘장 빼고 단순하게):
//   동료가 공격할 때마다 「꼬마 여왕님」(신앙)이 차고 → 신앙 4로 「성전 선포」 → 성전 동안 동료 공격 카드마다 네르가 벼락으로 따라 친다.
//   메인 딜러가 칠수록 네르도 딜을 넣는 섭딜. 네르 카드는 신앙 · 강인도 · 취약으로 판을 깔고, 성전 중엔 「성전」 : 덤.
//   없앤 것: 「교단의 휘장」(맞으면 신앙) 자원 · 성전 중 자기 공격 ×2 · 턴 시작 벼락 · 끝날 때 AP -1. 이전 판: C:\projects\_backup\ner_before_20261013
import { hero, dmg, ddmg, shield, heal, stack, spend, make, draw, discard, status, empower, ifStack, power, rule } from '../lib.mjs';

const H = '네르_빡침', Q = '꼬마 여왕님', F = `${H}_성전`;
const SF = (...x) => [{ k: 'ifForm', id: F }, ...x];   // 「성전」 : 덤(디아나 「백수공권」 꼴)
const tough = (v) => ({ k: 'tough', v });
const vuln = (v = 1) => status('취약', v, { target: 'allEnemies' });
const enterF = [{ k: 'form', id: F }, status('취약', 1, { target: 'allEnemies' })];   // 성전에 들면 적 전체 취약 1(원작 2성 「가끔은 그리운 순간」 — 10-13 저녁 고유 능력 2개 규칙으로 패시브에서 성전 드는 효과로 옮김)
const declare = (n) => [{ k: 'ifStack', id: Q, n }, { k: 'spend', id: Q, v: n }, ...enterF];
const extend = { k: 'formExtend', id: F, v: 1, max: 5 };

export default hero({
  id: H, name: '네르(빡침)', nature: '활발', race: '요정', role: '딜러', star: 3,
  blurb: '여왕님을 지키려다 끝내 폭발한 사제장. 동료가 싸울 때마다 신앙이 차오르고, 성전을 선포하면 동료의 공격마다 벼락으로 따라 칩니다.',
  keyword: {
    name: Q, desc: '여왕님을 지키려는 사제장의 신앙', carrier: 'self', cap: 5,
  },
  passives: [   // 고유 능력 둘 — 신앙 모으기 · 가득 차면 성전
    { name: '여왕님을 위해서라면', when: { on: 'play', who: 'other', type: '공격' }, fx: [stack(Q, 1)] },   // 동료 공격 카드마다 신앙 1
    { name: '성전 선포!', when: { on: 'stackReach', id: Q, n: 5 }, fx: [spend(Q, 5), ...enterF] },   // 10-13 저녁 사용자: 신앙 5 가득이면 저절로 성전(「성전 선포」 카드는 4로 일찍)
  ],
  forms: [{
    id: F, name: '성전', desc: '본인은 성전, 남들은 빡침이라 부르는 폭주', stance: true, anim: 'Idle_Change', turns: 3,
    passives: [
      { name: '심판의 벼락', when: { on: 'play', who: 'other', type: '공격' }, fx: [dmg(0.5, 'randomEnemy', { hitFx: [status('취약', 1)] })] },   // 추격 — 동료 공격 카드마다 무작위 적 50% · 맞은 적 취약 1(10-13 저녁 사용자)
      { name: '새로이 쌓아갈 추억', when: { on: 'break', who: 'any' }, fx: [extend] },   // 격파마다 +1턴(최대 5)
    ],
    offExpire: [{ k: 'spend', id: Q, all: true }],
  }],
  ult: { name: '교단의 심판', cost: 250, fx: [dmg(2.1, 'allEnemies'), ...enterF] },
}, {
  basicNames: ['도끼창 휘두르기', '적벽돌 던지기', '사제장의 망토'], deck: 'light', starter: true,
  uniques: [
    { // 시동 — 한 방 + 신앙
      name: '교주님한테서 손 떼!', cost: 1, type: '공격',
      fx: [dmg(2.2), stack(Q, 2), ...SF(dmg(1))],
      oracles: [
        { fx: [dmg(2.8), stack(Q, 2), ...SF(dmg(1))] },
        { fx: [dmg(1.4), stack(Q, 3), ...SF(dmg(1))] },
        { fx: [dmg(1.4, 'allEnemies'), stack(Q, 2), ...SF(dmg(0.6, 'allEnemies'))] },
        { fx: [dmg(1.8), status('약화', 1), stack(Q, 2)] },
        { fx: [dmg(1.8), stack(Q, 2), { k: 'ifBreak' }, { k: 'ap', v: 1 }] },
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
    { // 0코 · 보존 — 동료 다음 공격을 세게(섭딜의 판 깔기). 이름 · 그림은 옛 휘장 카드 그대로
      name: '교단의 휘장', cost: 0, type: '스킬', tags: ['보존'],
      fx: [empower(0.3, { who: 'other' }), stack(Q, 1), ...SF(extend)],
      oracles: [
        { tags: ['보존'], fx: [empower(0.5, { who: 'other' }), stack(Q, 1), ...SF(extend)] },
        { tags: ['보존'], fx: [empower(0.3, { who: 'other' }), stack(Q, 2), ...SF(extend)] },
        { tags: ['보존'], fx: [empower(0.3, { who: 'other' }), vuln(1)] },
        { power: true, fx: [stack(Q, 1), power([rule({ on: 'turnStart' }, [stack(Q, 1)])])] },
        { tags: ['보존'], fx: [draw(1), stack(Q, 1), ...SF(extend)] },
      ],
      blesses: ['draw', 'ap', [stack(Q, 1)]],
    },
    { // 0코 강인도
      name: '벽돌 한 장', cost: 0, type: '공격',
      fx: [dmg(1), tough(1), ...SF(tough(1))],
      oracles: [
        { fx: [dmg(0.7), tough(2), ...SF(tough(1))] },
        { fx: [dmg(0.7), tough(1), { k: 'ifBreak' }, stack(Q, 3)] },
        { fx: [dmg(0.6, 'allEnemies'), tough(1)] },
        { fx: [dmg(0.8), tough(1), ...SF(draw(1))] },
        { fx: [dmg(0.7), tough(1), { k: 'ifFoe', id: 'broken' }, dmg(2.8)] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    { // 마무리 — 신앙 4로 성전
      name: '성전 선포', cost: 1, type: '공격',
      fx: [dmg(1.6, 'allEnemies'), ...declare(4)],   // 10-13 저녁: 신앙은 동료 공격으로 차서 이 카드의 신앙 +1 은 뺌(글 45자 — 성전 취약이 붙음)
      oracles: [
        { type: '스킬', fx: [...declare(4), empower(0.6, { who: 'any' })] },
        { fx: [dmg(2.2, 'allEnemies'), ...declare(4)] },
        { fx: [dmg(1.4, 'allEnemies'), vuln(2), ...declare(4)] },
        { fx: [dmg(1.4, 'allEnemies'), ...declare(3)] },
        { fx: [dmg(3), ...declare(4)] },
      ],
      blesses: ['power', 'draw', [stack(Q, 1)]],
    },
    { // 굴리기 — 강인도 · 성전이면 적 전체 취약
      name: '가끔은 그리운 순간', cost: 1, type: '공격',
      fx: [dmg(1.2), tough(1), ...SF(vuln(1))],
      oracles: [
        { fx: [dmg(1.6), tough(1), ...SF(vuln(1))] },
        { fx: [dmg(0.8), tough(1), stack(Q, 2)] },
        { fx: [dmg(0.7, 'allEnemies'), tough(1), ...SF(vuln(1))] },
        { fx: [dmg(0.8), tough(1), vuln(1)] },
        { fx: [dmg(0.8), tough(1), empower(0.3, { who: 'other' })] },
      ],
      blesses: ['power', 'cost', [stack(Q, 1)]],
    },
  ],
});
