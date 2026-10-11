// 빌드 피드백 — 묶음 유령(피드백.md 「묶음 유령」 · 인연_자원공유.md §4)
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const pw = (on, fx, w = {}) => ({ k: 'power', rules: [{ when: { on, ...w }, fx }] });
export const FB_YURYEONG = {
  '유령/림': (j, H) => {
    H.O(j, '림_u4', 3, [st('낫 자국', 2, 'allEnemies'), { k: 'draw', v: 1 }, pw('tally', [st('낫 자국', 1, 'allEnemies')])], { power: true });
    H.O(j, '림_u1', 3, [{ k: 'dmg', ratio: 0.75, target: 'allEnemies' }, st('낫 자국', 1, 'allEnemies'), { k: 'ifBalanced' }, { k: 'draw', v: 1 }]);
    H.O(j, '림_u3', 5, [{ k: 'perDebuff' }, { k: 'dmg', ratio: 0.75, target: 'oneEnemy' }, st('낫 자국', 1, 'oneEnemy')]);   // 디버프 소비(인연 셰이디)
  },
  '유령/림_혼돈': (j, H) => {
    H.O(j, '림_혼돈_u4', 3, [{ k: 'dmg', ratio: 1.0, target: 'allEnemies' }, { k: 'perGone' }, { k: 'dmg', ratio: 0.15, target: 'allEnemies' }]);
    H.O(j, '림_혼돈_u3', 3, [{ k: 'status', id: '약화', v: 2, target: 'oneEnemy' }, st('혼돈', 2), pw('exhaust', [st('혼돈', 1)], { who: 'any' })], { power: true });
    H.O(j, '림_혼돈_u1', 4, [{ k: 'dmg', ratio: 0.79, target: 'allEnemies' }, { k: 'exileFrom', from: 'discard', n: 1, who: 'other' }, st('혼돈', 2)]);
    H.O(j, '림_혼돈_u3', 4, [{ k: 'rushDown', v: 2, target: 'oneEnemy' }, st('혼돈', 1)]);
    // 소멸 소비(인연 마요(멋짐) · 죠안): 자신의 소멸만 세던 것을 파티 소멸 수로
    for (const c of j.cards) for (const fx of [c.fx, ...(c.oracles || []).map(o => o.fx)]) for (const f of fx) if (f.k === 'perGone' && f.who === 'self') delete f.who;
  },
  '유령/바롱': (j, H) => { H.O(j, '바롱_u3', 3, [{ k: 'dmg', ratio: 1.3, target: 'oneEnemy' }, { k: 'perDebuff' }, { k: 'dmg', ratio: 0.9, target: 'oneEnemy' }]); },   // 디버프 소비
  '유령/셀리네': (j, H) => {
    H.O(j, '셀리네_u1', 4, [{ k: 'dmg', ratio: 0.95, target: 'allEnemies' }, pw('debuff', [st('표정', 1)])], { power: true });
    H.O(j, '셀리네_u2', 3, [st('표정', 2), { k: 'status', id: '약화', v: 1, target: 'allEnemies' }, { k: 'draw', v: 2 }]);
    H.O(j, '셀리네_u2', 5, [st('표정', 4), { k: 'draw', v: 1 }, pw('make', [st('표정', 1)], { who: 'any' })], { power: true });   // 생성 소비
  },
  '유령/셰이디': (j, H) => {
    H.O(j, '셰이디_u5', 1, [st('장난', 2, 'oneEnemy'), { k: 'draw', v: 2 }]);
    H.O(j, '셰이디_u5', 2, [st('장난', 2, 'oneEnemy'), { k: 'hasten', v: 1 }, { k: 'draw', v: 1 }]);
    H.O(j, '셰이디_u3', 4, [{ k: 'dmg', ratio: 0.75, target: 'oneEnemy' }, st('장난', 2, 'oneEnemy'), pw('reserveFire', [st('장난', 1, 'oneEnemy')])], { power: true });
    H.O(j, '셰이디_u3', 5, [{ k: 'dmg', ratio: 1.13, target: 'oneEnemy' }, { k: 'rushDown', v: 3, target: 'oneEnemy' }]);
    H.O(j, '셰이디_u1', 3, [{ k: 'dmg', ratio: 0.24, hits: 10, target: 'farEnemy' }, { k: 'ifFoe', id: 'broken' }, { k: 'dmg', ratio: 0.2, hits: 6, target: 'farEnemy' }]);   // 격파 소비
  },
  '유령/셰이디_역전': (j, H) => {
    H.O(j, '셰이디_역전_u1', 4, [{ k: 'dmg', ratio: 2.2, target: 'oneEnemy' }, { k: 'status', id: '약화', v: 2, target: 'oneEnemy' }, st('울분', 1)]);
    H.O(j, '셰이디_역전_u2', 3, [{ k: 'dmg', ratio: 1.9, target: 'allEnemies' }, { k: 'spend', id: '울분', all: true }, { k: 'empower', ratio: 0.6, who: 'any' }]);
    H.O(j, '셰이디_역전_u5', 3, [{ k: 'heal', ratio: 2.4 }, { k: 'perOverheal', per: 25, max: 8 }, { k: 'dmg', ratio: 0.3, target: 'allEnemies' }]);   // 넘친 회복 → 딜
  },
  '유령/스피키_메이드': (j, H) => {
    H.O(j, '스피키_메이드_u4', 3, [{ k: 'castOther', ratio: 0.6 }, st('잡동사니', 4)]);
    H.O(j, '스피키_메이드_u4', 5, [{ k: 'castOther', ratio: 0.9 }, st('잡동사니', 2)]);
    H.O(j, '스피키_메이드_u2', 3, [{ k: 'perStack', id: '잡동사니' }, { k: 'dmg', ratio: 0.77, target: 'randomEnemy' }, { k: 'spend', id: '잡동사니', v: 3 }]);
    H.rule(j, { name: '흘린 것도 흡입', when: { on: 'make', who: 'any' }, fx: [st('잡동사니', 1)] });   // 생성 소비
  },
  '유령/시온더다크불릿': (j, H) => {
    H.O(j, '시온더다크불릿_u3', 4, [{ k: 'dmg', ratio: 1.93, target: 'oneEnemy' }, pw('stackOver', [{ k: 'perEvent' }, { k: 'dmg', ratio: 0.5, target: 'farEnemy' }], { id: '마탄' })], { power: true });
    H.O(j, '시온더다크불릿_u1', 3, [{ k: 'perStack', id: '마탄' }, { k: 'dmg', ratio: 0.82, target: 'oneEnemy' }, { k: 'spend', id: '마탄', all: true }, st('마탄', 2)]);
    H.O(j, '시온더다크불릿_u3', 1, [{ k: 'dmg', ratio: 2.32, target: 'oneEnemy' }, { k: 'strip', target: 'oneEnemy' }, { k: 'ifFoe', id: 'broken' }, st('마탄', 3)]);   // 격파 소비
    H.rule(j, { name: '방패 너머 조준', when: { on: 'blocked' }, fx: [st('마탄', 1)] });   // 실드 소비(인연 리온)
  },
  '유령/앨리스': (j, H) => {
    H.O(j, '앨리스_u2', 3, [{ k: 'dmg', ratio: 1.84, target: 'oneEnemy' }, { k: 'perStack', id: '행운' }, { k: 'dmg', ratio: 0.65, target: 'oneEnemy' }, { k: 'ifStack', id: '행운', n: 5 }, { k: 'empower', ratio: 0.3, who: 'any' }]);
    H.O(j, '앨리스_u4', 5, [{ k: 'draw', v: 1 }, pw('tally', [st('행운', 1)])], { power: true });
    H.O(j, '앨리스_u3', 5, [st('행운', 2), { k: 'draw', v: 1 }, pw('drawn', [st('행운', 1)], { who: 'any', unique: true })], { power: true, tags: ['개전'] });   // 드로우 소비
    H.rule(j, { name: '버려진 카드 점괘', when: { on: 'discard', who: 'any' }, fx: [st('행운', 1)] });   // 버림 소비(인연 에스피 · 레테)
  },
  '유령/키샤': (j, H) => { H.O(j, '키샤_u4', 2, [{ k: 'heal', ratio: 0.91 }, { k: 'perOverheal', per: 25, max: 8 }, { k: 'dmg', ratio: 0.55, target: 'allEnemies' }]); },
};
