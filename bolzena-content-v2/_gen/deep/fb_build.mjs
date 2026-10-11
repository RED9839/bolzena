// 카제나 키워드 빌드(2026-10-10 사용자 승인) — 안 쓰던 키워드로 빌드 8개. 한 빌드 = 소비 사도(키워드 카드 1~2장) + 생산 사도(그 조건을 자주 · 싸게).
// 문서: _measure/사도_빌드/키워드_빌드.md. 카드 문구는 카제나를 베끼지 않고 구조만.
const tag = (c, ...t) => { c.tags = [...new Set([...(c.tags || []), ...t])]; };
const mulDmg = (c, m) => { for (const fx of [c.fx, ...(c.oracles || []).map(o => o.fx)]) for (const q of fx) if (q.k === 'dmg' && q.ratio) q.ratio = Math.round(q.ratio * m * 100) / 100; };
export const FB_BUILD = {
  // ① 탄환 · 연결 · 공명 — 버려서 쏜다
  '유령/시온더다크불릿': (j, H) => {
    const t = H.card(j, '시온더다크불릿_t1'); t.tags = ['탄환', '보존', '사용 불가']; t.fx = [{ k: 'when', on: 'discard' }, { k: 'stack', id: '마탄', v: 1 }];   // 안식: 마탄 1 — 버린 탄환이 「마탄」 몰아치기(시온_u1)로   // 탄환은 쏘는 카드가 아니라 버려서 쏜다 — 손에 모아 두고 버리기(짝)로
    for (const q of H.card(j, '시온더다크불릿_u4').fx) if (q.k === 'make') q.v = 2;   // 탄환 둘씩
    H.card(j, '시온더다크불릿_u2').fx = [{ k: 'stack', id: '마탄', v: 2 }, { k: 'draw', v: 1 }, { k: 'discard', v: 1 }];   // 스스로는 한 장 — 버리기는 짝(잉클 · 롤렛)이 더 잘
  },
  '정령/잉클': (j, H) => { tag(H.card(j, '잉클_u3'), '연결'); tag(H.card(j, '잉클_u4'), '연결'); for (const q of H.card(j, '잉클_u1').fx) if (q.k === 'discard') q.v = 2; },
  '마녀/롤렛': (j, H) => { H.card(j, '롤렛_u1').fx = [{ k: 'discard', v: 2 }, { k: 'draw', v: 2 }, { k: 'status', id: '공명', v: 2 }]; },
  // ② 결속 — 다이아 조각이 셋 겹치면 진짜 다이아
  '용족/다야': (j, H) => { const g = H.card(j, '다야_gem'); tag(g, '결속'); g.bondCard = '다야_real'; for (const q of H.card(j, '다야_real').fx) if (q.k === 'dmg') q.ratio = Math.round(q.ratio * 1.3 * 100) / 100; for (const q of H.card(j, '다야_u3').fx) if (q.k === 'make') q.v = 2; },   // 원석 둘씩 — 두 번이면 결속 3
  '용족/피라': (j, H) => { H.card(j, '피라_u1').fx = [{ k: 'stack', id: '부유함', v: 2 }, { k: 'ap', v: 1 }, { k: 'draw', v: 1 }]; },
  '용족/키디언': (j, H) => { H.card(j, '키디언_u2').fx = [{ k: 'tough', v: 1 }, { k: 'stack', id: '별빛 조각', v: 1 }, { k: 'nextCheaper', v: 1 }]; },
  // ③ 주도 — 턴 시작에 싸지는 광역 · 지속 딜
  '엘프/아이시아': (j, H) => { for (const id of ['아이시아_u3', '아이시아_u4']) { const c = H.card(j, id); c.cost = 2; mulDmg(c, 1.6); }   // 무거운 카드 — 주도로 싸게
    tag(H.card(j, '아이시아_u3'), '주도', '보존');   // 쥐고 있다가 턴 시작에 싸진다 — 드로우 짝이 손에 모아 준다
    const c = H.card(j, '아이시아_u4'); tag(c, '주도', '보존'); for (const q of c.fx) if (q.k === 'dmg' && q.target === 'oneEnemy') { q.target = 'allEnemies'; q.ratio = Math.round(q.ratio * 0.6 * 100) / 100; } },
  '엘프/리스티': (j, H) => { for (const q of H.card(j, '리스티_u2').fx) if (q.k === 'draw') q.v = 2; },   // 주도 짝 — 드로우
  // ④ 축복 — 버려지면 실드
  '용족/오팔': (j, H) => { tag(H.card(j, '오팔_u1'), '축복'); tag(H.card(j, '오팔_tap'), '축복'); },   // 톡톡 카드가 버려져도 실드
  '요정/네르': (j, H) => { tag(H.card(j, '네르_dream'), '축복'); H.card(j, '네르_u1').fx = [{ k: 'stack', id: '꾸벅 기도', v: 2 }, { k: 'discard', v: 1 }, { k: 'draw', v: 1 }]; },   // 졸다 떨군 카드 — 축복 짝
  // ⑤ 소각 — 소멸할 때 몰아치는 방어 기반 딜(탱커 메인딜)
  '정령/오로라': (j, H) => {
    tag(H.card(j, '오로라_u2'), '소멸'); H.card(j, '오로라_u2').fx = [{ k: 'burn', v: 1 }, { k: 'draw', v: 1 }, { k: 'stack', id: '빛울림', v: 1 }];
    const c = H.card(j, '오로라_u3'); c.fx = [{ k: 'dmg', ratio: 0.7, base: 'def', target: 'oneEnemy' }, { k: 'when', on: 'burn' }, { k: 'dmg', ratio: 1.6, base: 'def', target: 'allEnemies' }];
  },
  '정령/아일라': (j, H) => { H.card(j, '아일라_u4').fx.push({ k: 'burn', v: 1 }); },
  // ⑥ 열정 — 열정 카드를 낼수록
  '용족/리츠': (j, H) => { tag(H.card(j, '리츠_u4'), '열정'); H.card(j, '리츠_u2').fx = [{ k: 'stack', id: '정당방위', v: 1 }, { k: 'empower', ratio: 0.5 }, { k: 'when', on: 'passion' }, { k: 'stack', id: '정당방위', v: 2 }]; },
  '수인/버터': (j, H) => { tag(H.card(j, '버터_u1'), '열정'); tag(H.card(j, '버터_f1'), '열정'); },
  '용족/아네트': (j, H) => { const c = H.card(j, '아네트_u2'); const s = c.fx.find(q => q.k === 'status'); if (s) { s.id = '열정 약점'; s.v = 1; } },
  // ⑦ 회수 — 무덤에서 손으로, 한 턴에 몰아치기
  '유령/앨리스': (j, H) => { const c = H.card(j, '앨리스_u1'); tag(c, '회수 3'); mulDmg(c, 0.75); },
  '유령/림': (j, H) => { H.card(j, '림_u4').fx = [{ k: 'stack', id: '낫 자국', v: 1 }, { k: 'draw', v: 1 }, { k: 'ap', v: 2 }]; },
  // ⑧ 천상 · 종극 — 무거운 덱
  '용족/루드': (j, H) => { const r = H.card(j, '루드_rep'); tag(r, '천상'); for (const q of r.fx) if (q.k === 'dmg') q.ratio = 0.5; for (const q of H.card(j, '루드_u1').fx) if (q.k === 'make') q.v = 2;   // 렙 둘씩
    const f = H.card(j, '루드_u4'); f.cost = 2; tag(f, '종극'); mulDmg(f, 2.0); },
  '용족/시스트': (j, H) => { const c = H.card(j, '시스트_u1'); c.cost = 2; mulDmg(c, 1.9); },   // 천상 짝 — 2코 카드
  '정령/실라': (j, H) => { const c = H.card(j, '실라_u2'); c.cost = 2; mulDmg(c, 1.9); for (const o of c.oracles || []) for (const q of o.fx) if (q.k === 'extra') q.ratio = Math.round(q.ratio * 1.9 * 100) / 100; for (const q of c.fx) if (q.k === 'extra') q.ratio = Math.round(q.ratio * 1.9 * 100) / 100; },
  // 나이아는 원작 서포터 — 물탱크를 써서 치는 카드를 회복으로(피해는 보조), 고학년도 회복 쪽으로(2026-10-11 사용자)
  '정령/나이아': (j, H) => {
    const c = H.card(j, '나이아_u3'); c.type = '스킬';
    const heal = fx => fx.forEach(q => { if (q.k === 'dmg') { q.k = 'heal'; delete q.target; delete q.hits; } });
    heal(c.fx); for (const o of c.oracles || []) heal(o.fx);
    const u = j.heroes[0].ult; for (const q of u.fx) { if (q.k === 'dmg') q.ratio = 0.3; if (q.k === 'heal') q.ratio = 2.2; }
  },
  // 벨라 다시 리워크(2026-10-11 사용자) — 존속(불꽃)을 모으기만 하고 쓸 곳이 없던 것을 원작 「서툰 챙김」(불꽃을 회복으로) 결정타로 되살리고,
  // 고학년을 원작 「적의 불꽃을 빼앗아 아군에게 나눠 회복」 쪽으로. 쓰기는 「2개 써서 · 1개당」(전부 소모 아님).
  '유령/벨라': (j, H) => {
    const c = H.card(j, '벨라_u3'); c.name = '서툰 챙김'; c.type = '스킬'; c.cost = 1;
    const per = (k, r) => [{ k: 'perStack', id: '존속' }, { k, ratio: r }];
    c.fx = [{ k: 'heal', ratio: 1.0 }, ...per('heal', 0.3), { k: 'spend', id: '존속', v: 2 }];
    c.oracles = [
      { name: '신탁 1', fx: [{ k: 'heal', ratio: 1.5 }, ...per('heal', 0.45), { k: 'spend', id: '존속', v: 2 }] },
      { name: '신탁 2', fx: [{ k: 'heal', ratio: 0.8 }, { k: 'spend', id: '존속', v: 2 }, { k: 'empower', ratio: 0.6, who: 'any' }] },   // 불씨 건네기
      { name: '신탁 3', fx: [{ k: 'shield', ratio: 1.0 }, ...per('shield', 0.4), { k: 'spend', id: '존속', v: 2 }] },                 // 존재의 실드
      { name: '신탁 4', fx: [{ k: 'later', n: 1, then: [{ k: 'heal', ratio: 1.6 }] }, { k: 'shield', ratio: 1.0 }, { k: 'stack', id: '존속', v: 1 }] },   // 모닥불
      { name: '신탁 5', cost: 0, fx: [{ k: 'heal', ratio: 1.0 }, ...per('heal', 0.3), { k: 'spend', id: '존속', v: 2 }] },
    ];
    const u = j.heroes[0].ult; u.fx = [{ k: 'heal', ratio: 1.5 }, { k: 'stack', id: '존속', v: 2 }, { k: 'dmg', ratio: 1.0, base: 'def', target: 'allEnemies' }];
  },
};
