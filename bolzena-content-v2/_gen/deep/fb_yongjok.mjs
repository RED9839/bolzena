// 빌드 피드백 — 묶음 용족(피드백.md 「묶음 용족」 · 인연_자원공유.md §4)
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const pw = (on, fx, w = {}, extra = {}) => ({ k: 'power', rules: [{ when: { on, ...w }, ...extra, fx }] });
export const FB_YONGJOK = {
  '용족/루드': (j, H) => {
    // 다시 리뉴얼: 「한 번 더!」 가 자기를 다시 만드는 고리를 끊고 0코 방어 기반으로 · 동료 주인으로도 만든다
    const r = H.card(j, '루드_rep'); r.cost = 0; r.fx = [{ k: 'dmg', ratio: 0.45, base: 'def', target: 'oneEnemy' }];
    H.O(j, '루드_u3', 5, [{ k: 'shield', ratio: 1.6 }, pw('turnEnd', [{ k: 'perStack', id: '렙' }, { k: 'shield', ratio: 0.3 }])], { power: true });
    H.O(j, '루드_u1', 4, [{ k: 'dmg', ratio: 0.99, base: 'def', target: 'allEnemies' }, { k: 'make', id: '루드_rep', v: 2, owner: 'other' }]);
  },
  '용족/시스트': (j, H) => {
    // 다시 리뉴얼: 배송 고리 끊기 · 바구니 ① 털기 · 장사천재 ④ 쌓아 두면 AP · 떨이 수수료(소멸 소비)
    const e = H.card(j, '시스트_express'); e.fx = [{ k: 'dmg', ratio: 1.94, target: 'lowEnemy' }, { k: 'ifKill' }, st('수수료', 1), { k: 'draw', v: 1 }];
    H.O(j, '시스트_u3', 1, [{ k: 'dmg', ratio: 2.0, target: 'allEnemies' }, { k: 'perStack', id: '수수료' }, { k: 'dmg', ratio: 0.4, target: 'allEnemies' }, { k: 'spend', id: '수수료', all: true }]);
    H.O(j, '시스트_u4', 4, [st('수수료', 1), pw('turnStart', [st('수수료', 1), { k: 'ifStack', id: '수수료', n: 6 }, { k: 'ap', v: 1 }])], { cost: 0, tags: ['개전'], power: true });
    H.del(j, '바보세 징수');
    H.rule(j, { name: '떨이 수수료', when: { on: 'exhaust', who: 'any' }, fx: [st('수수료', 1)] });
  },
  '용족/오팔': (j, H) => {
    // 다시 리뉴얼(고유 효과 한 줄): 탭은 「닿으면」 이 아니라 「넘치면」 보석 — 셋을 쥐고 보석 던지기로 쓸지 고른다
    const k = H.kw(j, '탭'); delete k.onMax;
    k.rules = [{ name: '깨지는 보석', when: { on: 'stackOver', id: '탭' }, fx: [{ k: 'spend', id: '탭', all: true }, { k: 'make', id: '오팔_gem', v: 1 }] }];
    H.O(j, '오팔_u4', 3, [{ k: 'make', id: '오팔_tap', v: 4 }], { cost: 1 });
  },
  '용족/비비': (j, H) => {
    H.O(j, '비비_u4', 4, [{ k: 'status', id: '결의', v: 1 }, { k: 'perStack', id: '수은 중독' }, { k: 'dmg', ratio: 0.6, base: 'def', target: 'allEnemies' }]);
    H.M(j, '비비_u1', [3], fx => { const walk = l => { for (const f of l) { if (f.k === 'dmg' && f.base === 'def' && f.ratio > 0.5) f.ratio = 0.5; if (f.rules) f.rules.forEach(r => walk(r.fx)); } }; walk(fx); });
  },
  '용족/실비아': (j, H) => {
    H.O(j, '실비아_u4', 2, [pw('play', [{ k: 'dmg', ratio: 0.35, base: 'def', target: 'randomEnemy' }], { type: '스킬', who: 'any', marked: '초청객' })], { cost: 0, power: true });
    H.rule(j, { name: '은빛 손님', when: { on: 'blocked' }, fx: [st('은방울', 1)] });   // 실드 소비(인연 비비)
  },
  '용족/아라그니아': (j, H) => {
    H.O(j, '아라그니아_u1', 3, [st('진주 구슬', 3), { k: 'shield', ratio: 0.57 }, { k: 'when', on: 'handEnd' }, st('진주 구슬', 2)], { tags: ['보존'] });
    H.O(j, '아라그니아_u2', 5, [{ k: 'perStack', id: '진주 구슬' }, { k: 'dmg', ratio: 0.2, base: 'def', target: 'allEnemies' }, { k: 'spend', id: '진주 구슬', all: true }]);
    H.O(j, '아라그니아_u2', 3, [{ k: 'perStack', id: '진주 구슬' }, { k: 'shield', ratio: 0.28 }, { k: 'ifStack', id: '진주 구슬', n: 4 }, { k: 'ap', v: 1 }, { k: 'spend', id: '진주 구슬', all: true }], { cost: 0 });   // AP 공급
  },
  '용족/다야_퓨어샤인': (j, H) => {
    H.O(j, '다야_퓨어샤인_u4', 1, [{ k: 'status', id: '결의', v: 2 }, { k: 'power', rules: [{ when: { on: 'turnStart' }, fx: [st('퓨어☆영역', 1)] }, { when: { on: 'turnEnd' }, conds: [{ c: 'stack', id: '퓨어☆영역', n: 3 }], fx: [st('반짝임', 1)] }] }], { power: true, tags: ['개전'] });
  },
  '용족/아네트': (j, H) => {
    H.O(j, '아네트_u4', 5, [st('관중 함성', 2), pw('turnStart', [st('오늘의 MVP', 1, 'strongestAlly'), st('관중 함성', 1)])], { tags: ['개전'], power: true });
    H.del(j, '싸워라 싸워~');
    H.rule(j, { name: '싸워라 싸워~', when: { on: 'play', who: 'other', marked: '오늘의 MVP' }, fx: [{ k: 'heal', ratio: 0.27 }] });
  },
  '용족/리츠': (j, H) => {
    H.card(j, '리츠_u1').fx = [{ k: 'perStack', id: '정당방위' }, { k: 'dmg', ratio: 0.45, target: 'oneEnemy' }, { k: 'spend', id: '정당방위', all: true }, { k: 'formEnd' }];
    H.O(j, '리츠_u1', 1, [{ k: 'perStack', id: '정당방위' }, { k: 'dmg', ratio: 0.68, target: 'oneEnemy' }, { k: 'spend', id: '정당방위', all: true }, { k: 'formEnd' }]);
    H.rule(j, { name: '든든한 등', when: { on: 'blocked' }, fx: [st('정당방위', 1)] });   // 실드 소비(인연 루드)
  },
  '용족/다야': (j, H) => {
    H.O(j, '다야_u3', 4, [{ k: 'draw', v: 1 }, pw('turnStart', [{ k: 'cardStatus', id: '광택', v: 1, to: 'hand', n: 1, who: 'self', type: '공격', max: 5 }])], { power: true });
    H.O(j, '다야_u4', 2, [{ k: 'perStack', id: '다이아 쓰라림' }, { k: 'dmg', ratio: 0.73, target: 'oneEnemy' }, { k: 'spend', id: '다이아 쓰라림', all: true }, { k: 'make', id: '다야_gem', v: 1 }]);
    H.O(j, '다야_u1', 3, [{ k: 'dmg', ratio: 1.38, target: 'allEnemies' }, { k: 'perDebuff' }, { k: 'dmg', ratio: 0.3, target: 'oneEnemy' }]);   // 디버프 소비
  },
  '용족/피라': (j, H) => {
    H.O(j, '피라_u1', 2, [{ k: 'status', id: '취약', v: 1, target: 'oneEnemy' }, st('부유함', 2), { k: 'dealtMod', v: 0.15, target: 'otherAllies' }]);
    H.del(j, '수금');
    H.rule(j, { name: '수금', when: { on: 'debuff', who: 'any', fresh: true }, fx: [st('부유함', 1)] });   // 디버프 소비
  },
  '용족/제이드': (j, H) => {
    H.O(j, '제이드_u3', 4, [{ k: 'dmg', ratio: 1.1, target: 'oneEnemy' }, st('비취옥', 1), { k: 'when', on: 'drawAny' }, st('비취옥', 1)], { tags: ['보존'] });   // 드로우 소비
  },
  '용족/키디언': (j, H) => {
    H.rule(j, { name: '오르의 망원경', when: { on: 'drawn', who: 'other' }, fx: [st('별빛 조각', 1)] });   // 드로우 소비(인연 오르)
  },
};
