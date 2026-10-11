// 자원 공유 카제나식(2026-10-10 사용자 결정) — 쌓기는 계기로 자동, 쓰기는 카드로.
// 고유 능력의 자동 피해(dmg · extra)는 사도마다 하나(정체성이 분명한 것)만 남기고, 나머지는 쌓기만 남긴 뒤 그 힘을 카드(결정타 · 신탁 갈래)로 옮긴다.
// 카드 쪽 쓰기는 「n개 써서」 · 「1개당(소모 없음)」 꼴로 — 「전부 소모」 가 늘지 않게.
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
const perScale = (c, m, ns) => { const fxs = ns ? ns.map(n => c.oracles[n - 1].fx) : [c.fx, ...(c.oracles || []).map(o => o.fx)]; for (const fx of fxs) fx.forEach((f, i) => { if (i > 0 && /^per/.test(fx[i - 1].k) && f.k === 'dmg') f.ratio = Math.round(f.ratio * m * 100) / 100; }); };
const dropDmg = r => { r.fx = r.fx.filter(f => f.k !== 'extra' && f.k !== 'dmg'); };
export const FB_KAZENA = {
  '요정/에르핀': (j, H) => { H.P(j, '케이크 냄새', dropDmg); perScale(H.card(j, '에르핀_u3'), 1.15); },
  '유령/림_혼돈': (j, H) => {
    H.P(j, '뒤처리 정리', dropDmg);
    H.O(j, '림_혼돈_u4', 5, [{ k: 'perStack', id: '혼돈' }, { k: 'dmg', ratio: 0.45, target: 'oneEnemy' }, { k: 'spend', id: '혼돈', v: 3 }]);   // 혼돈 셋을 써서
  },
  '요정/샤샤': (j, H) => {
    H.P(j, '같이 쏘기', r => { r.fx = [st('텀블러 수압', 1)]; });
    H.O(j, '샤샤_u2', 5, [{ k: 'perStack', id: '텀블러 수압' }, { k: 'dmg', ratio: 0.5, target: 'oneEnemy' }, { k: 'spend', id: '텀블러 수압', v: 2 }]);   // 수압 둘을 써서
  },
  '수인/델리아': (j, H) => {
    H.P(j, '선장님 몫', dropDmg);
    H.O(j, '델리아_u1', 5, [{ k: 'dmg', ratio: 0.9, target: 'oneEnemy' }, { k: 'perStack', id: '수족열증' }, { k: 'dmg', ratio: 0.4, target: 'oneEnemy' }]);   // 열증 1개당(소모 없음)
  },
  '마녀/마카샤': (j, H) => {
    H.P(j, '번지는 결말', r => { r.fx = [st('운명의 시계', 1, 'topEnemy')]; });   // 자동 광역 → 시계 쌓기(자동 피해는 「정해진 결말」 하나)
    H.O(j, '마카샤_u3', 5, [{ k: 'perStack', id: '운명의 시계' }, { k: 'dmg', ratio: 0.55, base: 'def', target: 'oneEnemy' }, { k: 'hasten', v: 1 }]);
  },
  '마녀/요미': (j, H) => {
    H.del(j, '별빛');   // 자동 피해는 「달빛 꽃밭」 하나
    perScale(H.card(j, '요미_u2'), 1.2);
  },
  '정령/아르코': (j, H) => {
    H.P(j, '댄스 쇼다운', r => { r.fx = [st('스텝', 1)]; });
    H.O(j, '아르코_u2', 5, [{ k: 'perStack', id: '스텝' }, { k: 'dmg', ratio: 0.6, target: 'oneEnemy' }, { k: 'spend', id: '스텝', v: 2 }]);
  },
  '정령/오로라': (j, H) => {   // 같은 「플라즈마 불꽃」 두 계기 → 턴 시작 하나만
    const k = H.kw(j, '오로라 장막'); k.rules = (k.rules || []).filter(r => !(r.name === '플라즈마 불꽃' && r.when && r.when.on === 'stackGone'));
  },
};
