// 빌드 피드백 — 묶음 마녀 · 미스틱(피드백.md 「묶음 마녀_미스틱」 · 인연_자원공유.md §4). 같은 글 신탁은 lib.numOnly 손질로 풀렸다.
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
export const FB_MAJO = {
  '마녀/레비_졸업': (j, H) => {
    // 퇴근 ④ — 실드를 남기고 방어 기반 광역(메인딜, 시험함)
    H.O(j, '레비_졸업_u3', 4, [{ k: 'shield', ratio: 0.81 }, { k: 'perStack', id: '신입의 패기' }, { k: 'dmg', ratio: 0.35, base: 'def', target: 'allEnemies' }, { k: 'spend', id: '신입의 패기', all: true }]);
    // 논문 — 다섯 칸 모두 게재를 쌓고 읽는다
    H.O(j, '레비_졸업_u5', 4, [{ k: 'shield', ratio: 0.96 }, { k: 'perCardSt', id: '게재' }, { k: 'shield', ratio: 0.42 }, { k: 'ifGained', id: '신입의 패기', n: 4 }, { k: 'cardStatus', id: '게재', v: 1, max: 5 }]);
    H.M(j, '레비_졸업_u5', [2, 3, 5], fx => [...fx.filter(f => f.k !== 'draw' && !(f.k === 'stack' && f.id === '신입의 패기')), { k: 'ifGained', id: '신입의 패기', n: 6 }, { k: 'cardStatus', id: '게재', v: 1, max: 5 }]);
    H.O(j, '레비_졸업_u1', 5, [st('신입의 패기', 2), { k: 'shield', ratio: 0.81 }, { k: 'empower', ratio: 0.3 }]);
  },
  '마녀/롤렛': (j, H) => {
    // 「관객의 환호」 패시브 중복 → 지우고, 인연(클로에 생성 · 버림 공급)을 받는 갈채 계기
    H.del(j, '관객의 환호');
    H.rule(j, { name: '관객의 환호', when: { on: 'play', who: 'other' }, fx: [st('갈채', 1)] });
    H.rule(j, { name: '사라진 카드 마술', when: { on: 'play', who: 'any', token: true }, fx: [st('갈채', 1)] });
  },
  '마녀/마카샤': (j, H) => {
    H.O(j, '마카샤_u1', 1, [st('운명의 시계', 3, 'oneEnemy'), { k: 'dmg', ratio: 0.52, base: 'def', target: 'oneEnemy' }, { k: 'draw', v: 2 }]);
    H.O(j, '마카샤_u1', 2, [st('운명의 시계', 3, 'topEnemy'), { k: 'dmg', ratio: 0.51, base: 'def', target: 'allEnemies' }]);
    H.O(j, '마카샤_u1', 3, [st('운명의 시계', 3, 'oneEnemy'), { k: 'hasten', v: 1 }, { k: 'draw', v: 1 }]);
    H.O(j, '마카샤_u1', 4, [st('운명의 시계', 3, 'oneEnemy'), { k: 'power', rules: [{ when: { on: 'reserveFire' }, fx: [{ k: 'draw', v: 1 }] }] }], { power: true });
    H.O(j, '마카샤_u1', 5, [st('운명의 시계', 3, 'oneEnemy'), { k: 'status', id: '취약', v: 2, target: 'oneEnemy' }]);
    H.O(j, '마카샤_u2', 4, [{ k: 'dmg', ratio: 0.26, base: 'def', target: 'allEnemies' }, { k: 'power', rules: [{ when: { on: 'turnStart' }, fx: [{ k: 'hasten', v: 1 }, { k: 'dmg', ratio: 0.15, base: 'def', target: 'allEnemies' }] }] }], { power: true });
  },
  '마녀/벨리타': (j, H) => {
    // 다시 리뉴얼(고유 효과): 당 5 저절로 모두 쓰기 → 다음 카드 강화만(겹은 남아 「간식 시간」 이 턴다)
    H.kw(j, '당 수치').onMax = { empower: 'next', ratio: 0.5 };
    H.O(j, '벨리타_u4', 3, [{ k: 'dmg', ratio: 2.1, target: 'oneEnemy' }, { k: 'perStack', id: '당 수치' }, { k: 'dmg', ratio: 0.6, target: 'oneEnemy' }]);
    // 생성 소비(인연 I04 · 엔진 요청 — 손의 생성 카드 수): 몰래 초콜릿만 세던 것을 생성 카드 공용으로
    const c = H.card(j, '벨리타_u1');
    for (const fx of [c.fx, ...c.oracles.map(o => o.fx)]) for (const f of fx) if (f.k === 'perTag' && f.id === '벨리타_t1') { f.k = 'perPile'; delete f.id; f.from = 'token'; }
  },
  '마녀/벨벳': (j, H) => { H.O(j, '벨벳_u2', 1, [{ k: 'shield', ratio: 1.5 }, { k: 'status', id: '반격', v: 3 }, { k: 'status', id: '피해 감소', v: 1 }]); },
  '마녀/비비_신성': (j, H) => {
    H.kw(j, '새싹').onMax = { empower: 'any', ratio: 0.6 };   // 「모두의 빛」(새싹 전부 소모)과 다투지 않게
    H.O(j, '비비_신성_u4', 3, [st('새싹', 2), { k: 'power', rules: [{ when: { on: 'drawn', who: 'other' }, fx: [st('새싹', 1)] }] }], { power: true });
    H.rule(j, { name: '모두가 기른 숲', when: { on: 'make', who: 'any' }, fx: [st('새싹', 1)] });   // 인연: 우이(기억) 생성
  },
  '마녀/셰럼': (j, H) => {
    H.O(j, '셰럼_u1', 5, [{ k: 'shield', ratio: 0.5 }, st('원고지', 2), st('회한의 영역', 1)]);
    H.rule(j, { name: '남의 이야기 받아 적기', when: { on: 'make', who: 'other' }, fx: [st('원고지', 1)] });
  },
  '마녀/스노키': (j, H) => {
    H.O(j, '스노키_u3', 3, [{ k: 'perGuarded', per: 20, max: 6 }, { k: 'dmg', ratio: 0.45, base: 'def', target: 'oneEnemy' }, st('밀두유', 1)]);
    H.O(j, '스노키_u4', 3, [{ k: 'heal', ratio: 1.67 }, st('밀두유', 3), { k: 'draw', v: 2 }]);
  },
  '마녀/아사나': (j, H) => {
    H.M(j, '아사나_u2', [2, 3, 5], fx => fx.map(f => (f.k === 'stack' && f.id === '시범 자세' ? st('시범 자세', 1) : f)).concat([{ k: 'draw', v: 1, who: 'other' }]).filter((f, i, a) => a.filter(x => !/^(if|per)/.test(x.k)).length <= 3 || f.k !== 'draw'));
    H.cap(j, '아사나_u1', [4], '마력 호흡', 3);
  },
  '마녀/아야': (j, H) => {
    H.O(j, '아야_u2', 2, [{ k: 'dmg', ratio: 0.6, target: 'oneEnemy' }, { k: 'perDebuff' }, { k: 'dmg', ratio: 0.3, target: 'oneEnemy' }]);   // 디버프 소비
  },
  '마녀/요미': (j, H) => {
    H.O(j, '요미_u3', 2, [{ k: 'later', n: 1, then: [{ k: 'dmg', ratio: 6.75, target: 'allEnemies' }] }, { k: 'nextAp', v: -1 }, st('달바라기', 2)]);
    H.O(j, '요미_u3', 5, [{ k: 'dmg', ratio: 1.5, target: 'allEnemies' }, { k: 'later', n: 2, then: [{ k: 'dmg', ratio: 6.0, target: 'allEnemies' }] }, st('달바라기', 2)]);
    H.cap(j, '요미_u1', [2, 3], '달바라기', 3);
    H.rule(j, { name: '기억해 둘게요', when: { on: 'exhaust', who: 'any' }, fx: [st('달바라기', 1)] });   // 소멸 소비
  },
  '마녀/포셔': (j, H) => {
    // 다시 리뉴얼(패시브 · 고유 효과): 넘친 회복을 적에게(동료의 넘친 회복도) · 숨은 중복 규칙 하나로 · 아무도 안 만드는 카드 지우기
    H.del(j, '입에 쓴 약'); H.del(j, '약값은 칼같이');
    H.rule(j, { name: '약값은 칼같이', when: { on: 'play', type: '공격' }, fx: [st('부작용', 2, 'oneEnemy')] });
    H.rule(j, { name: '입에 쓴 약', when: { on: 'overheal', who: 'any' }, fx: [{ k: 'dmg', ratio: 1, ofEvent: 1, target: 'oneEnemy' }, { k: 'cleanse', v: 1 }] });
    j.cards = j.cards.filter(c => c.id !== '포셔_t1');
  },
};
