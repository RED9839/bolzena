// 패시브를 고유 능력으로 합치기(2026-10-10 사용자 결정) — 리뉴얼 사도의 passives 를 고유 효과(keyword) rules 로 옮기고 passives 는 비운다.
// 옮길 곳: 아군에 붙지 않는(carrier ≠ ally) 첫 고유 효과. 아군에 붙는 것만 있으면 그 아래로 옮기되 self: true(그 사도 자신의 계기 — 엔진 PassiveRule.Self).
// 같은 이름 규칙이 이미 있으면(다시 돌릴 때) 지우고 새로 — 몇 번 돌려도 같은 결과.
export function mergePassives(j) {
  const h = j.heroes[0]; if (h.star < 3 && h.id !== '마리') return;
  const ps = h.passives || []; if (!ps.length) return;
  const kws = [h.keyword, ...(h.keywords || [])].filter(Boolean); if (!kws.length) return;
  const k = kws.find(x => (x.carrier || 'self') !== 'ally') || kws[0];
  const self = (k.carrier || 'self') === 'ally';
  const names = new Set(ps.map(p => p.name));
  k.rules = (k.rules || []).filter(r => !names.has(r.name));
  for (const p of ps) k.rules.push(self ? { ...p, self: true } : p);
  h.passives = [];
}
