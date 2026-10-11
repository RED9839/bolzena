// 빌드 피드백 — 묶음 정령(피드백.md 「묶음 정령」 · 인연_자원공유.md §4)
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const pw = (on, fx, w = {}) => ({ k: 'power', rules: [{ when: { on, ...w }, fx }] });
/** 「1개당」 뒤 피해 계수만 m 배(본판 + 신탁). */
const perScale = (c, m) => { for (const fx of [c.fx, ...(c.oracles || []).map(o => o.fx)]) fx.forEach((f, i) => { if (i > 0 && /^per/.test(fx[i - 1].k) && f.k === 'dmg') f.ratio = Math.round(f.ratio * m * 100) / 100; }); };
export const FB_JEONGRYEONG = {
  '정령/나이아': (j, H) => {
    H.O(j, '나이아_u3', 2, [{ k: 'spend', id: '물탱크', all: true }, { k: 'feed', v: 2, target: 'oneAlly' }, { k: 'draw', v: 1, who: 'other' }]);
    H.O(j, '나이아_u2', 2, [{ k: 'heal', ratio: 1.2 }, { k: 'cleanse', v: 2 }, { k: 'draw', v: 1, who: 'other' }]);
    H.M(j, '나이아_u3', [4], fx => fx.map(f => (f.k === 'heal' ? { ...f, ratio: 0.36 } : f)));
  },
  '정령/니콜': (j, H) => {
    H.O(j, '니콜_u3', 2, [pw('play', [{ k: 'make', id: '니콜_ng', v: 1 }], { who: 'other', type: '스킬' })], { power: true });
    for (const c of j.cards) for (const fx of [c.fx, ...(c.oracles || []).map(o => o.fx)]) for (const f of fx) if (f.k === 'perGone' && f.who === 'self') delete f.who;   // 소멸 소비(누구의 소멸이든)
  },
  '정령/멜루나': (j, H) => {
    // 다시 리뉴얼: 주가를 털면 멜론이 나온다(두 엔진을 잇기) · 멜론 회복 ×0.72
    const c = H.card(j, '멜루나_u3');
    c.fx = [{ k: 'perStack', id: '주가' }, { k: 'dmg', ratio: 0.9, target: 'oneEnemy' }, { k: 'spend', id: '주가', all: true }, { k: 'make', id: '멜루나_melon', v: 1 }];
    H.O(j, '멜루나_u3', 1, [{ k: 'perStack', id: '주가' }, { k: 'dmg', ratio: 1.35, target: 'oneEnemy' }, { k: 'spend', id: '주가', all: true }, { k: 'make', id: '멜루나_melon', v: 2 }]);
    H.O(j, '멜루나_u3', 2, [{ k: 'perStack', id: '주가' }, { k: 'dmg', ratio: 0.62, target: 'allEnemies' }, { k: 'spend', id: '주가', all: true }, { k: 'make', id: '멜루나_melon', v: 1 }]);
    H.O(j, '멜루나_u3', 3, [{ k: 'perStack', id: '주가' }, { k: 'dmg', ratio: 1.0, target: 'oneEnemy' }, { k: 'spend', id: '주가', all: true }, { k: 'make', id: '멜루나_melon', v: 2 }]);
    H.O(j, '멜루나_u3', 5, [{ k: 'perStack', id: '주가' }, { k: 'dmg', ratio: 0.8, target: 'oneEnemy' }, { k: 'make', id: '멜루나_melon', v: 1 }]);
    const m = H.card(j, '멜루나_melon'); for (const f of m.fx) if (f.k === 'heal') f.ratio = Math.round(f.ratio * 0.72 * 100) / 100;
  },
  '정령/미로': (j, H) => {
    // 다시 리뉴얼(시험함): 깨진 거울 조각 = 방어 기반 한 방(실드를 안 주는 대가) · 반격 갈래
    const c = H.card(j, '미로_u4');
    c.fx = [{ k: 'dmg', ratio: 0.45, base: 'def', target: 'oneEnemy' }, { k: 'perStack', id: '시선' }, { k: 'dmg', ratio: 0.2, base: 'def', target: 'oneEnemy' }];
    H.O(j, '미로_u4', 1, [{ k: 'dmg', ratio: 0.68, base: 'def', target: 'oneEnemy' }, { k: 'perStack', id: '시선' }, { k: 'dmg', ratio: 0.3, base: 'def', target: 'oneEnemy' }]);
    H.M(j, '미로_u4', [2], fx => fx.map(f => (f.k === 'dmg' ? { ...f, ratio: 0.32 } : f)));
    H.O(j, '미로_u4', 3, [{ k: 'perStack', id: '시선' }, { k: 'dmg', ratio: 0.6, base: 'def', target: 'oneEnemy' }, { k: 'spend', id: '시선', all: true }]);
    H.M(j, '미로_u4', [4], fx => fx.map(f => (f.k === 'shield' ? { ...f, ratio: 0.38 } : f)));
    H.M(j, '미로_u4', [5], fx => fx.map(f => (f.k === 'dmg' ? { ...f, ratio: 0.42 } : f)));
    H.O(j, '미로_u3', 3, [{ k: 'status', id: '반격', v: 2 }, pw('turnStart', [{ k: 'status', id: '반격', v: 1 }])], { power: true });
  },
  '정령/블랑셰': (j, H) => {
    H.O(j, '블랑셰_u2', 3, [{ k: 'perStack', id: '푸른 장미' }, { k: 'dmg', ratio: 1.1, target: 'oneEnemy' }, { k: 'spend', id: '푸른 장미', v: 2 }]);
    H.O(j, '블랑셰_u3', 4, [st('푸른 장미', 2, 'oneEnemy'), { k: 'status', id: '취약', v: 1, target: 'oneEnemy' }, { k: 'draw', v: 1 }]);
    H.O(j, '블랑셰_u2', 2, [{ k: 'dmg', ratio: 1.2, target: 'oneEnemy' }, { k: 'perDebuff' }, { k: 'dmg', ratio: 0.4, target: 'oneEnemy' }]);   // 디버프 소비
  },
  '정령/시저': (j, H) => {
    H.O(j, '시저_u1', 1, [{ k: 'dealtMod', v: 0.3, target: 'allAllies' }, st('단종 예고', 2, 'oneEnemy'), { k: 'draw', v: 2 }]);
    H.O(j, '시저_u1', 2, [{ k: 'status', id: '사기', v: 1 }, st('단종 예고', 2, 'oneEnemy'), { k: 'hasten', v: 1 }]);
    H.O(j, '시저_u1', 3, [{ k: 'dealtMod', v: 0.2, target: 'allAllies' }, st('단종 예고', 2, 'allEnemies'), { k: 'draw', v: 1 }]);
    H.cap(j, '시저_u1', [4], '단종 예고', 2);
  },
  '정령/실라': (j, H) => {
    H.O(j, '실라_u4', 5, [st('정면 승부', 2, 'oneEnemy'), pw('play', [{ k: 'extra', ratio: 0.3, target: 'markedEnemy' }], { who: 'other', type: '공격' })], { power: true });
    H.O(j, '실라_u1', 4, [{ k: 'perStack', id: '정면 승부' }, { k: 'dmg', ratio: 0.66, target: 'oneEnemy' }, { k: 'ifFoe', id: 'broken' }, { k: 'dmg', ratio: 1.0, target: 'oneEnemy' }]);   // 격파 소비
    H.rule(j, { name: '동생의 전류', when: { on: 'debuff', who: 'any', fresh: true }, fx: [st('정면 승부', 1, 'oneEnemy')] });   // 디버프 소비(인연 라이카)
  },
  '정령/아르코': (j, H) => {
    H.O(j, '아르코_u4', 2, [pw('drawn', [{ k: 'dmg', ratio: 0.15, target: 'randomEnemy' }], { who: 'any' })], { cost: 0, power: true });   // 드로우 소비
  },
  '정령/아일라': (j, H) => { H.O(j, '아일라_u4', 3, [{ k: 'perGuarded', per: 40, max: 4 }, { k: 'dmg', ratio: 0.3, target: 'allEnemies' }, { k: 'spend', id: '빠직', v: 1 }]); },   // 실드 소비
  '정령/오로라': (j, H) => { H.O(j, '오로라_u2', 4, [st('오로라 장막', 1), { k: 'draw', v: 2, who: 'other' }]); },
  '정령/우이': (j, H) => {
    const c = H.card(j, '우이_u3'); perScale(c, 0.8);
    H.O(j, '우이_u3', 3, [{ k: 'dmg', ratio: 0.6, target: 'allEnemies' }, { k: 'perOverheal', per: 25, max: 4 }, { k: 'dmg', ratio: 0.2, target: 'allEnemies' }]);   // 넘친 회복 → 딜
  },
  '정령/우이_기억': (j, H) => {
    // 다시 리뉴얼(시험함): 기억은 「다른 아군의 카드가 소멸하면」 · 떠나보내기 ×0.675 · 행복 전파자 ⑤ 기억 3 · 클로버 2
    H.P(j, '도망치지 않는 기억', r => { r.when = { on: 'exhaust', who: 'other' }; });
    perScale(H.card(j, '우이_기억_u3'), 0.675);
    H.cap(j, '우이_기억_u2', [5], '되찾은 기억', 3);
    H.M(j, '우이_기억_u2', [1], fx => fx.map(f => (f.k === 'make' && /clover/.test(f.id) ? { ...f, v: 2 } : f)));
  },
  '정령/잉클': (j, H) => {
    // 다시 리뉴얼(시험함): 버려지면 친다(누구의 버림이든) · 고쳐 쓴 원고 = 버리고 건져 다시 쓰기
    H.del(j, '세상에 한 획');
    H.rule(j, { name: '세상에 한 획', when: { on: 'discard', who: 'any' }, fx: [st('먹빛', 1, 'randomEnemy'), { k: 'extra', ratio: 0.3, target: 'randomEnemy' }] });
    H.card(j, '잉클_u1').fx = [{ k: 'discard', v: 1 }, { k: 'pull', from: 'discard', n: 1 }, st('먹빛', 1, 'oneEnemy')];
    H.O(j, '잉클_u1', 1, [{ k: 'discard', v: 1 }, { k: 'pull', from: 'discard', n: 1 }, st('먹빛', 2, 'oneEnemy')]);
  },
  '정령/라이카': (j, H) => { H.O(j, '라이카_u4', 5, [{ k: 'dmg', ratio: 0.86, target: 'oneEnemy' }, { k: 'perDebuff' }, { k: 'dmg', ratio: 0.3, target: 'oneEnemy' }]); },   // 디버프 소비
};
