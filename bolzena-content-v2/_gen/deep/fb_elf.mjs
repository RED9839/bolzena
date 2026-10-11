// 빌드 피드백 — 묶음 엘프(피드백.md 「묶음 엘프」 · 인연_자원공유.md §4)
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const pw = (on, fx, w = {}) => ({ k: 'power', rules: [{ when: { on, ...w }, fx }] });
const fixOverheal = j => {
  const walk = l => { for (const f of l || []) { if (f.k === 'perOverheal') { f.pct = 0.8; f.per = 25; } if (f.then) walk(f.then); if (f.rules) f.rules.forEach(r => walk(r.fx)); } };
  for (const c of j.cards) { walk(c.fx); (c.oracles || []).forEach(o => walk(o.fx)); }
};
export const FB_ELF = {
  '엘프/리뉴아': (j, H) => {
    H.O(j, '리뉴아_u4', 1, [{ k: 'hasten', v: 2 }, pw('reserveFire', [{ k: 'dmg', ratio: 0.54, hits: 2, target: 'allEnemies' }])], { power: true });
    H.O(j, '리뉴아_u2', 2, [st('둠스데이 시계', 2), { k: 'hasten', v: 1 }, { k: 'draw', v: 1 }]);
  },
  '엘프/리스티': (j, H) => {
    H.O(j, '리스티_u3', 2, [{ k: 'dmg', ratio: 0.6, target: 'oneEnemy' }, { k: 'make', id: '리스티_can', v: 1 }, { k: 'when', on: 'discard' }, { k: 'make', id: '리스티_can', v: 1 }]);
    H.M(j, '리스티_u2', [3], fx => [...fx, { k: 'when', on: 'drawAny' }, st('개인정보', 1)]);
    H.rule(j, { name: '버린 것 줍기', when: { on: 'discard', who: 'any' }, fx: [st('개인정보', 1)] });   // 버림 소비
  },
  '엘프/아멜리아_R41': (j, H) => {
    // 다시 리뉴얼: 완성품 ×0.62 · 개전 시험 가동 2 · 시제품의 숨은 확률 대가를 숫자로 · 전자포는 소멸이 쌓일수록 크게
    const t2 = H.card(j, '아멜리아_R41_t2'); for (const f of t2.fx) if (f.ratio) f.ratio = Math.round(f.ratio * 0.62 * 100) / 100;
    const t1 = H.card(j, '아멜리아_R41_t1'); t1.fx = t1.fx.filter(f => f.k !== 'ifRandom' && f.k !== 'payHpPct'); t1.fx[0].ratio = 1.5;
    H.cap(j, '아멜리아_R41_u3', [4], '시험 가동', 2);
    H.O(j, '아멜리아_R41_u4', 1, [{ k: 'dmg', ratio: 0.72, target: 'allEnemies' }, { k: 'perGone' }, { k: 'dmg', ratio: 0.2, target: 'allEnemies' }]);
    fixOverheal(j);
  },
  '엘프/엘레나': (j, H) => {
    H.O(j, '엘레나_u1', 4, [{ k: 'dmg', ratio: 1.28, target: 'allEnemies' }, { k: 'status', id: '충격', v: 1, target: 'allEnemies' }, st('냥이 드론', 3)]);
    H.O(j, '엘레나_u2', 3, [st('냥이 드론', 2), { k: 'status', id: '충격', v: 1, target: 'allEnemies' }, { k: 'draw', v: 1 }]);
    H.rule(j, { name: '드론 부품', when: { on: 'exhaust', who: 'any' }, fx: [st('냥이 드론', 1)] });   // 소멸 소비(인연 R41)
  },
  '엘프/오르': (j, H) => {
    H.O(j, '오르_u2', 3, [{ k: 'later', n: 1, then: [{ k: 'perStack', id: '응원' }, { k: 'dmg', ratio: 0.3, base: 'def', target: 'topEnemy' }] }, { k: 'spend', id: '응원', all: true }]);
    H.O(j, '오르_u2', 5, [{ k: 'perStack', id: '응원' }, { k: 'shield', ratio: 0.7 }, { k: 'empower', ratio: 0.25, who: 'any' }, { k: 'spend', id: '응원', all: true }]);
  },
  '엘프/이드': (j, H) => {
    H.O(j, '이드_u2', 4, [pw('turnStart', [{ k: 'status', id: '반격', v: 1 }, st('나타 경보', 1)])], { power: true });
    H.O(j, '이드_u5', 4, [{ k: 'perStack', id: '나타 경보' }, { k: 'dmg', ratio: 0.22, base: 'def', target: 'oneEnemy' }, { k: 'status', id: '반격', v: 2 }]);
  },
  '엘프/이드_재활': (j, H) => { H.rule(j, { name: '같이 꾸는 꿈', when: { on: 'drawn', who: 'other' }, fx: [{ k: 'shield', ratio: 0.05 }] }); },   // 드로우 소비(인연 뮤트)
  '엘프/아이시아': (j, H) => { H.rule(j, { name: '버린 것도 금고로', when: { on: 'exhaust', who: 'any' }, fx: [st('회사 금고', 1)] }); },
  '엘프/로네_시장': (j, H) => { H.rule(j, { name: '주민의 손', when: { on: 'make', who: 'other' }, fx: [st('지지율', 1)] }); },   // 생성 소비
  '엘프/칸나': (j, H) => { H.O(j, '칸나_u1', 3, [{ k: 'dmg', ratio: 2.2, target: 'oneEnemy' }, { k: 'tough', v: 2 }, { k: 'ifFoe', id: 'broken' }, { k: 'dmg', ratio: 1.0, target: 'oneEnemy' }]); },   // 격파 소비
  '엘프/캐시': (j, H) => {
    H.P(j, '과충전 테이저', r => { for (const f of r.fx) if (f.k === 'dmg') f.ratio = 0.45; });
    H.M(j, '캐시_u1', [5], fx => fx.map(f => (f.k === 'draw' ? st('히익 전류', 1) : f)));
  },
  '엘프/하이디': (j, H) => {
    // 다시 리뉴얼: 고정 대상 → 고른 적 · 잠복 절반만 쓰기 · 변신판 그릇을 고유 「셀카 한 장」 으로
    const u4 = H.card(j, '하이디_u4');
    u4.fx = [{ k: 'perStack', id: '잠복' }, { k: 'dmg', ratio: 0.32, target: 'oneEnemy' }, { k: 'spend', id: '잠복', v: 2 }];
    for (const o of u4.oracles) for (const f of o.fx) if (f.target === 'markedEnemy') f.target = 'oneEnemy';
    H.O(j, '하이디_u4', 3, [{ k: 'perStack', id: '잠복' }, { k: 'dmg', ratio: 0.12, target: 'allEnemies' }, { k: 'spend', id: '잠복', all: true }]);
    const u3 = H.card(j, '하이디_u3'); for (const fx of [u3.fx, ...u3.oracles.map(o => o.fx)]) for (const f of fx) if (f.target === 'lowEnemy') f.target = 'oneEnemy';
    const u2 = H.card(j, '하이디_u2'); u2.fx = [{ k: 'draw', v: 1 }, st('잠복', 1), { k: 'when', on: 'drawAny' }, st('잠복', 1)];   // 드로우 소비
  },
  '엘프/헤일리_멀쩡': (j, H) => { H.O(j, '헤일리_멀쩡_u4', 3, [{ k: 'perStack', id: '극복의 훈장' }, { k: 'dmg', ratio: 0.27, base: 'def', target: 'allEnemies' }, { k: 'spend', id: '극복의 훈장', all: true }]); },
  '엘프/힐데': (j, H) => {
    H.O(j, '힐데_u3', 4, [{ k: 'shield', ratio: 0.69 }, pw('overheal', [{ k: 'dmg', ratio: 0.36, base: 'def', target: 'allEnemies' }], { pct: 0.8 })], { power: true });
    H.O(j, '힐데_u5', 4, [{ k: 'dmg', ratio: 0.39, base: 'def', target: 'randomEnemy', hits: 3 }, st('진료 차트', 3)]);
    fixOverheal(j);
  },
};
