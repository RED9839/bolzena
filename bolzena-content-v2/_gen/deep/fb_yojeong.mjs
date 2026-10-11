// 빌드 피드백 — 묶음 요정 · 마리(피드백.md 「묶음 요정_마리」 · 인연_자원공유.md §4)
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const pw = (on, fx, w = {}) => ({ k: 'power', rules: [{ when: { on, ...w }, fx }] });
export const FB_YOJEONG = {
  '요정/에르핀': (j, H) => {
    H.O(j, '에르핀_u1', 4, [{ k: 'perStack', id: '배부름' }, { k: 'dmg', ratio: 0.78, target: 'randomEnemy' }, { k: 'make', id: '에르핀_cake', v: 1 }]);
    // 생성 소비(인연 에슈르 · 파트라 · 벨리타): 누구의 생성이든 케이크 냄새
    H.M(j, '에르핀_u1', [5], fx => fx.map(f => (f.k === 'power' ? pw('make', [{ k: 'dmg', ratio: 0.76, target: 'randomEnemy' }], { who: 'any' }) : f)));
  },
  '요정/에슈르': (j, H) => {
    // 다시 리뉴얼: 빵 센디오가 엔진 몸 — 시작 센디오 3 · 처치하면 2 · 누구의 생성이든 1, 그리모어는 센디오 수로
    H.del(j, '빵빵한 브레드'); H.del(j, '요정 왕국 명물'); H.del(j, '빵집 아니고 마법학교');
    H.rule(j, { name: '빵빵한 브레드', when: { on: 'fightStart' }, fx: [st('빵 센디오', 3)] });
    H.rule(j, { name: '빵집 아니고 마법학교', when: { on: 'kill', mine: true }, fx: [st('빵 센디오', 2)] });
    H.rule(j, { name: '요정 왕국 명물', when: { on: 'make', who: 'any' }, fx: [st('빵 센디오', 1)] });
    const g = H.card(j, '에슈르_u4');
    g.fx = [{ k: 'dmg', ratio: 0.45, target: 'oneEnemy' }, { k: 'perStack', id: '빵 센디오' }, { k: 'dmg', ratio: 0.3, target: 'oneEnemy' }, { k: 'spend', id: '빵 센디오', all: true }];
    H.O(j, '에슈르_u4', 3, [{ k: 'dmg', ratio: 0.36, target: 'allEnemies' }, { k: 'perStack', id: '빵 센디오' }, { k: 'dmg', ratio: 0.18, target: 'allEnemies' }, { k: 'spend', id: '빵 센디오', all: true }]);
  },
  '요정/에슈르_마도': (j, H) => {
    H.O(j, '에슈르_마도_u4', 3, [{ k: 'dmg', ratio: 0.6, target: 'oneEnemy' }, { k: 'perCardSt', id: '연구', n: 1 }, { k: 'dmg', ratio: 0.2, target: 'oneEnemy' }, { k: 'cardStatus', id: '연구', v: 1, max: 6 }]);
    H.rule(j, { name: '연구 노트', when: { on: 'exhaust', who: 'any' }, fx: [st('거울 형상', 1)] });   // 소멸 소비(인연 파트라)
  },
  '요정/스키아': (j, H) => {
    H.O(j, '스키아_u1', 4, [{ k: 'dmg', ratio: 1.0, target: 'oneEnemy' }, { k: 'later', n: 1, then: [{ k: 'dmg', ratio: 0.52, target: 'allEnemies' }] }, { k: 'later', n: 2, then: [{ k: 'dmg', ratio: 0.52, target: 'allEnemies' }] }], { tags: ['보존'] });
    H.rule(j, { name: '빼앗아 먹기', when: { on: 'make', who: 'other' }, fx: [st('삼킨 감탄사', 1)] });   // 생성 소비(인연 캬롯)
  },
  '요정/로니': (j, H) => {
    H.O(j, '로니_u1', 2, [{ k: 'dmg', ratio: 0.74, target: 'allEnemies' }, st('현상금', 2, 'topEnemy')]);
    H.O(j, '로니_u3', 3, [{ k: 'dmg', ratio: 0.73, target: 'allEnemies' }, { k: 'ifStack', id: '현상금', n: 3 }, { k: 'status', id: '취약', v: 1, target: 'allEnemies' }]);
    H.O(j, '로니_u2', 3, [{ k: 'dmg', ratio: 1.09, target: 'oneEnemy' }, { k: 'perDebuff' }, { k: 'dmg', ratio: 0.34, target: 'oneEnemy' }]);   // 디버프 소비(인연 마리)
  },
  '요정/마리': (j, H) => {
    // 다시 리뉴얼: 「강화 폭탄」 은 공격 카드만 · 넷 넘게 터뜨리면 파티 다음 카드 + · 시동 폭탄 3
    H.del(j, '강화 폭탄');
    H.rule(j, { name: '강화 폭탄', when: { on: 'play', type: '공격' }, fx: [st('묻은 폭탄', 1)] });
    const k = H.kw(j, '묻은 폭탄'); k.rules = (k.rules || []).filter(r => r.name !== '저도 여기 있어요!');
    k.rules.push({ name: '저도 여기 있어요!', when: { on: 'spend', id: '묻은 폭탄', n: 4 }, fx: [{ k: 'empower', ratio: 0.3, who: 'any' }] });
    H.card(j, '마리_u3').fx = [st('묻은 폭탄', 3), { k: 'draw', v: 1 }];
    H.O(j, '마리_u3', 1, [st('묻은 폭탄', 4), { k: 'draw', v: 2 }]);
  },
  '요정/마요_멋짐': (j, H) => { H.M(j, '마요_멋짐_u3', [2], fx => [...fx, { k: 'ifDebuffs', n: 2 }, st('자랑하고 싶음', 2)]); },   // 디버프 소비
  '요정/에르핀_왕도': (j, H) => {
    H.M(j, '에르핀_왕도_u2', [2], fx => [...fx, { k: 'ifFoe', id: 'broken' }, st('왕마력', 2)]);   // 격파 소비
    H.O(j, '에르핀_왕도_u3', 1, [st('왕마력', 2), { k: 'draw', v: 2 }, { k: 'when', on: 'drawAny' }, st('왕마력', 1)]);   // 드로우 소비
  },
  '요정/죠안': (j, H) => {
    H.M(j, '죠안_u4', [4], fx => fx.filter(f => !(f.k === 'status' && f.id === '피해 감소')));   // 글 45자
    const b = H.card(j, '죠안_bread'); for (const f of b.fx) if (f.ratio) f.ratio = Math.round(f.ratio * 0.77 * 100) / 100;
    H.O(j, '죠안_u2', 5, [{ k: 'dmg', ratio: 0.96, target: 'allEnemies' }, { k: 'form', id: '죠안_꿈결' }, pw('turnStart', [{ k: 'form', id: '죠안_꿈결' }])], { power: true });
    H.O(j, '죠안_u2', 3, [{ k: 'perOverheal', per: 25, max: 4 }, { k: 'dmg', ratio: 0.5, target: 'allEnemies' }, { k: 'form', id: '죠안_꿈결' }]);   // 넘친 회복 → 딜(서포터 메인딜)
  },
  '요정/캬롯': (j, H) => { H.M(j, '캬롯_u3', [4], fx => fx.map(f => (f.k === 'dealtMod' ? { k: 'empower', ratio: 0.4, who: 'any' } : f))); },   // 글 45자
  '요정/폴랑': (j, H) => {
    H.O(j, '폴랑_u1', 3, [{ k: 'heal', ratio: 2.04 }, { k: 'status', id: '피해 감소', v: 1 }]);
    H.O(j, '폴랑_u4', 3, [{ k: 'heal', ratio: 1.28 }, st('머스킷 장전', 2), { k: 'draw', v: 1 }]);
    H.O(j, '폴랑_u2', 1, [{ k: 'dmg', ratio: 0.9, target: 'allEnemies' }, { k: 'perStack', id: '머스킷 장전' }, { k: 'dmg', ratio: 0.6, target: 'allEnemies' }, { k: 'spend', id: '머스킷 장전', all: true }]);
  },
  '요정/리코타': (j, H) => { H.O(j, '리코타_u5', 3, [{ k: 'cardStatus', id: '비용', v: 1, to: 'hand', n: 1 }, { k: 'empower', ratio: 0.55, who: 'any' }], { cost: 0 }); },
  '요정/네르': (j, H) => { H.M(j, '네르_u4', [4], fx => { const walk = l => { for (const f of l) { if (f.k === 'dmg' && f.base === 'def') f.ratio = Math.min(f.ratio, 0.55); if (f.rules) f.rules.forEach(r => walk(r.fx)); } }; walk(fx); }); },
  '요정/네르_빡침': (j, H) => { H.rule(j, { name: '여왕의 길을 따라', when: { on: 'break', who: 'any' }, fx: [st('빡침', 1)] }); },   // 격파 소비(인연 에르핀(왕도))
  '요정/클로에': (j, H) => {   // 생성 소비(엔진 요청 — 손의 생성 카드 수)
    const c = H.card(j, '클로에_u5'); for (const fx of [c.fx, ...c.oracles.map(o => o.fx)]) for (const f of fx) if (f.k === 'perTag' && f.id === '클로에_cloth') { f.k = 'perPile'; delete f.id; f.from = 'token'; }
  },
  '요정/슈팡': (j, H) => { H.rule(j, { name: '받은 소포', when: { on: 'make', who: 'other' }, fx: [st('배달 속도', 1)] }); },   // 생성 소비(인연 바나)
};
