// 완성형 카드(2026-10-10 사용자 승인 — 카제나 9할이 완성형 1장) — 3성 사도 고유 4장 가운데 「터뜨리기 · 마무리」 한 장을 신탁 없는 완성형으로.
// 신탁 대신 처음부터 완성된 수치: 비율 수치 ×1.3(신탁 평균 값어치 — 신탁은 기본의 1.15~1.6배), 비율이 없으면 쌓기 · 드로우 +1.
// 남기는 1할: 변신(forms)이 있는 사도 · 원본 그대로 두는 넷 — 신탁 갈래가 기믹의 몸통이다.
const NUM = new Set(['dmg', 'heal', 'shield', 'extra', 'empower', 'dealtMod', 'drain']);
const walk = (fx, cb) => (fx || []).forEach(f => { cb(f); walk(f.then, cb); for (const r of f.rules || []) walk(r.fx, cb); });
const KEEP = new Set(['에르핀', '캬롯', '셰이디', '시저']);
/** 마무리 카드 고르기 — 쓰기(spend · perStack · perCardSt · perTag · perEvent) 공격 > 쓰기 카드 > 비용 높은 공격 > 아무 고유. */
export function pickFinisher(j) {
  const h = j.heroes[0];
  const u = j.cards.filter(c => c.hero === h.id && c.unique && !c.token && (c.oracles || []).length === 5 && !(h.starter || []).includes(c.id));
  const pay = c => c.fx.some(q => /^(spend|perStack|perCardSt|perTag|perEvent|perGone|perPile|perPlayed)$/.test(q.k));
  const dmg = c => c.fx.filter(q => q.k === 'dmg').reduce((a, q) => a + (q.ratio || 0), 0);
  return u.filter(c => pay(c) && c.type === '공격')[0] || u.filter(pay)[0] || u.filter(c => c.type === '공격').sort((a, b) => b.cost - a.cost || dmg(b) - dmg(a))[0] || u[0];
}
export function completeCard(j) {
  const h = j.heroes[0]; if (h.star < 3 || KEEP.has(h.id) || (h.forms || []).length) return null;
  const c = pickFinisher(j); if (!c) return null;
  let n = 0; walk(c.fx, q => { if (NUM.has(q.k) && q.ratio) { q.ratio = Math.round(q.ratio * 1.3 * 100) / 100; n++; } });
  if (!n) { const s = c.fx.find(q => q.k === 'stack' || q.k === 'draw'); if (s) s.v = (s.v || 1) + 1; }
  delete c.oracles; c.complete = true;
  return c.id;
}
