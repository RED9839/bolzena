// 빌드 피드백 — 묶음 수인(피드백.md 「묶음 수인」 · 인연_자원공유.md §4)
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const pw = (on, fx, w = {}) => ({ k: 'power', rules: [{ when: { on, ...w }, fx }] });
export const FB_SUIN = {
  '수인/델리아': (j, H) => {
    H.O(j, '델리아_u4', 4, [{ k: 'payHp', v: 30 }, { k: 'ap', v: 1 }, { k: 'draw', v: 1 }]);
    H.O(j, '델리아_u3', 3, [{ k: 'payHp', v: 30 }, { k: 'dmg', ratio: 1.4, target: 'oneEnemy' }, { k: 'perDebuff' }, { k: 'dmg', ratio: 0.35, target: 'oneEnemy' }]);   // 디버프 소비
  },
  '수인/디아나': (j, H) => {
    H.O(j, '디아나_u3', 5, [{ k: 'heal', ratio: 1.32 }, { k: 'status', id: '사기', v: 1 }, { k: 'make', id: '디아나_rice', v: 2 }]);
    H.O(j, '디아나_u1', 5, [{ k: 'perStack', id: '기혈' }, { k: 'dmg', ratio: 0.55, target: 'allEnemies' }, { k: 'spend', id: '기혈', all: true }]);
  },
  '수인/디아나_왕년': (j, H) => {
    H.O(j, '디아나_왕년_u2', 2, [{ k: 'discard', all: true }, { k: 'perDiscarded' }, { k: 'dmg', ratio: 0.4, target: 'randomEnemy' }, st('연격', 3)]);
    H.O(j, '디아나_왕년_u2', 4, [st('연격', 3), pw('discard', [st('연격', 1)], { who: 'any' })], { power: true });   // 버림 소비(인연 바리에)
  },
  '수인/란': (j, H) => {
    H.M(j, '란_u1', [1, 3], fx => fx.map(f => (f.k === 'stack' && f.id === '사냥감' ? st('사냥감', 1, f.target) : f)));
    H.M(j, '란_u4', [4], fx => fx.map(f => (f.k === 'stack' && f.id === '사냥감' ? st('사냥감', 1, f.target) : f)));
    H.O(j, '란_u3', 3, [st('늑대 표식', 3), { k: 'status', id: '약화', v: 3, target: 'oneEnemy' }]);
    H.O(j, '란_u2', 3, [{ k: 'perDebuff' }, { k: 'dmg', ratio: 0.6, target: 'oneEnemy' }, { k: 'ifDebuffs', n: 3 }, st('사냥감', 1)]);   // 디버프 소비(인연 우로스)
  },
  '수인/루포': (j, H) => { H.O(j, '루포_u2', 4, [st('계획', 1), pw('make', [st('계획', 1)], { who: 'other' })], { power: true }); },
  '수인/리온': (j, H) => { H.O(j, '리온_u1', 1, [st('유죄', 2, 'allEnemies'), { k: 'shield', ratio: 1.35 }, { k: 'ifWounded' }, { k: 'heal', ratio: 0.7 }]); },
  '수인/모모': (j, H) => {
    // 드로우 소비(인연 루포 — 남이 뽑은 카드로 분신)
    H.O(j, '모모_u1', 2, [st('분신', 2), pw('drawn', [st('분신', 1)], { who: 'other' })], { power: true });
    H.O(j, '모모_u1', 3, [st('분신', 3), { k: 'draw', v: 1 }]);
    H.cap(j, '모모_u3', [2], '분신', 3);
  },
  '수인/베니_베니': (j, H) => {
    H.O(j, '베니_베니_u4', 1, [{ k: 'perStack', id: '꿀범벅' }, { k: 'dmg', ratio: 0.72, target: 'oneEnemy' }, { k: 'ifStack', id: '꿀범벅', n: 4 }, { k: 'status', id: '기절', v: 1, target: 'oneEnemy' }, { k: 'spend', id: '꿀범벅', all: true }]);
    H.O(j, '베니_베니_u4', 3, [{ k: 'perDebuff' }, { k: 'dmg', ratio: 0.7, target: 'oneEnemy' }, { k: 'status', id: '약화', v: 1, target: 'oneEnemy' }], { cost: 2 });
  },
  '수인/슈로': (j, H) => {
    H.O(j, '슈로_u4', 1, [{ k: 'status', id: '결의', v: 1 }, pw('turnStart', [st('검의 영역', 2)])], { tags: ['개전'] });
    H.M(j, '슈로_u4', [2, 3], fx => [{ k: 'status', id: '결의', v: 1 }, ...fx.filter(f => f.k !== 'draw')]);
  },
  '수인/스패럿': (j, H) => {
    H.kw(j, '해적의 낙인').cap = 3; H.kw(j, '해적의 낙인').per = [{ stat: 'taken', v: 0.08 }];
    H.O(j, '스패럿_u1', 4, [{ k: 'dmg', ratio: 0.78, target: 'topEnemy' }, st('해적의 낙인', 3, 'topEnemy'), { k: 'status', id: '약점 노출', v: 1, target: 'topEnemy' }], { tags: ['분쇄'] });
    H.O(j, '스패럿_u3', 4, [{ k: 'make', id: '스패럿_t1', v: 1 }, pw('exhaust', [st('노획물', 1)], { who: 'other' })], { power: true });   // 소멸 소비
  },
  '수인/에피카': (j, H) => { H.O(j, '에피카_u5', 4, [st('연주 중', 2), st('에피콘', 2), { k: 'empower', ratio: 0.4 }]); },
  '수인/코미': (j, H) => {
    // 다시 리뉴얼: 거대화 = 깨어 있는 동안 막은 만큼 치는 몸(다 막으면 반격) · 사료 1코 · 낮잠은 반만
    const fm = j.heroes[0].forms.find(f => f.id === '코미_거대화');
    fm.passives = [{ name: '깔아뭉개기', when: { on: 'blocked' }, fx: [{ k: 'status', id: '반격', v: 1 }] }];
    const c = H.card(j, '코미_u5'); c.cost = 1;
    c.fx = [{ k: 'perStack', id: '낮잠' }, { k: 'dmg', ratio: 0.3, base: 'def', target: 'allEnemies' }, { k: 'spend', id: '낮잠', v: 2 }, { k: 'form', id: '코미_거대화' }];
    H.M(j, '코미_u5', [1, 3], fx => fx.map(f => (f.k === 'dmg' ? { ...f, base: 'def' } : f.k === 'spend' && f.all ? { k: 'spend', id: f.id, v: 2 } : f)));
    H.M(j, '코미_u5', [2, 5], fx => fx.filter(f => f.k !== 'spend').map(f => (f.k === 'dmg' ? { ...f, base: 'def' } : f)));   // 글 45자 — 소모 없이(낮잠을 남기는 갈래)
    for (const o of c.oracles) if (o.cost === 1) delete o.cost; else if (o.cost === 2) o.cost = 1;
    H.M(j, '코미_u5', [5], fx => fx.map(f => (f.k === 'heal' ? { ...f, ratio: Math.round(f.ratio * 1.95 * 100) / 100 } : f)));
  },
  '수인/코미_수영복': (j, H) => {
    // 다시 리뉴얼: 재고가 동료 스킬과 특제 주스 사이에서 갈린다
    H.del(j, '판매 개시');
    H.rule(j, { name: '판매 개시', when: { on: 'play', type: '스킬', who: 'other' }, conds: [{ c: 'stack', id: '주스 재고', n: 1 }], fx: [{ k: 'spend', id: '주스 재고', v: 1 }, { k: 'heal', ratio: 0.4 }] });
    H.O(j, '코미_수영복_u2', 3, [{ k: 'heal', ratio: 0.75 }, { k: 'spend', id: '주스 재고', v: 1 }, { k: 'draw', v: 1 }], { cost: 0 });
    H.O(j, '코미_수영복_u4', 2, [st('주스 재고', 2), pw('spend', [{ k: 'dmg', ratio: 0.45, target: 'randomEnemy' }], { id: '주스 재고' })], { cost: 0, power: true });
  },
  '수인/티그': (j, H) => {
    H.O(j, '티그_u4', 4, [{ k: 'atkMod', v: 0.12, target: 'self' }, pw('drawn', [st('장작 패기', 1)], { who: 'any', type: '공격' })], { power: true });   // 드로우 소비
    H.rule(j, { name: '장작 받기', when: { on: 'make', who: 'other' }, fx: [st('장작 패기', 1)] });   // 생성 소비(인연 디아나)
  },
  '수인/티그_영웅': (j, H) => { H.O(j, '티그_영웅_u4', 2, [{ k: 'dmg', ratio: 0.93, target: 'oneEnemy' }, { k: 'ifStack', id: '겨눈 칼끝', n: 2 }, { k: 'status', id: '기절', v: 1, target: 'oneEnemy' }]); },
  // 인연 — 그윈 사기 → 델리아(다타): 델리아 쪽은 위 · 그윈 공급은 그대로
};
