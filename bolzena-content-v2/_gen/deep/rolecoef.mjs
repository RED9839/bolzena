import fs from 'fs';
// 역할다움을 카드 계수로(2026-10-10 사용자 결정 — 사도 스탯 카제나 배수 · 엔진 역할 배율 없앰과 함께)
//  · 기본 방어 카드(실드 하나뿐인 기본 스킬)는 역할 무관 1.35
//  · 탱커: 고유 카드에 2코 실드 2.35(+반격 1 — 실드로 다 막으면 반격이 커진다 = 막은 양 → 피해) · 방어 기반 피해가 없으면 가장 센 고유 공격 하나를 방어 기반으로
//  · 서포터: 고유 카드 회복 · 실드 계수 ×4.0(엔진 서포터 배율을 없앤 몫 + 「힐이 너무 적다」) · 기본 회복 카드 1.6
//  · 탱커 공격 기반 피해 ×0.45 · 딜러 피해 · 추가 공격 · 강화 ×1.33 — weaksim 목표(딜+탱+서 ≥ 딜러 3 + 15%p · 탱2+서 ≤ 딜+탱+서)로 맞춘 값(SUPMUL · TDMG · DMUL 로 시험)
// boost · tune 뒤에 값을 정해 넣는다(lib run · apply_struct — apply_struct 는 orig4 스냅숏을 입력으로 써서 거듭 곱해지지 않는다).
export const STAT_MUL = { 딜러: { def: 1.36, atk: 1.33 }, 탱커: { hp: 0.83, atk: 1.33, def: 0.69 }, 서포터: { def: 0.86 } };
const SUP = +(process.env.SUPMUL || 4.0);   // 서포터 회복 · 실드
const TDMG = +(process.env.TDMG || 0.45);   // 2026-10-10 사용자: 역할 공통 배율은 카드에서 뺀다(탱커 공격은 카제나 스탯 ×1.33 그대로)   // 탱커 공격 기반 피해(공격 ×1.33 을 덜어 — 탱커 둘이 딜러 자리를 넘보지 않게)
const DMUL = +(process.env.DMUL || 1.5);   // 딜러 고유 카드 피해 배율(역할 개성은 고유 카드로 — 2026-10-10 카제나식, 기본 카드는 기준값)
const TSH = +(process.env.TSH || 2.0);   // 탱커 2코 실드 카드(카드 설계 수치 — 탱2+서가 세면 이것부터 내린다)
const TDEF = +(process.env.TDEF || 0.8);
const TROLE = +(process.env.TROLE || 0.9);   // 탱커 고유 카드 전체 수치 배율(탱2+서가 딜+탱+서를 넘지 않게 — 스탯이 아니라 카드로)   // 탱커 방어 기반 피해로 바꾼 카드의 배율
const r2 = x => Math.round(x * 100) / 100;
/** 기본 카드 기준값(2026-10-10 사용자 — 역할과 상관없이 같은 값, 10% 단위). 「사도_sN」 기본 카드만, 3성 · 마리. */
/**
 * 기본 카드 = 한 줄 수치뿐(2026-10-11 사용자 — 카제나 기본 카드에는 덧효과가 없다). 3성 · 마리의 「사도_sN」 기본 3장:
 * 1코 공격 100% · 2코 강타 220%(탱커는 카제나 뱅가드처럼 방어 기반 피해) · 막기 실드 140%(회복이 기본이던 서포터는 회복 140%).
 * 빠진 덧효과 가운데 고유 효과를 쌓는 유일한 길이던 것은 시동 카드로 옮긴다(movedExtras).
 */
export const BASIC = { atk1: 1.0, atk2: 2.2, guard: 1.4, heal: 1.4 };
export const MOVED = [];   // 덧효과를 고유 카드로 옮긴 사도(보고용)
export function basicStd(j) {
  const h = j.heroes[0];
  const bs = j.cards.filter(c => c.hero === h.id && !c.unique && !c.token && /_s\d+$/.test(c.id));
  const lost = [];
  for (const c of bs) {
    const keep = c.type === '공격' ? (c.cost >= 2 ? BASIC.atk2 : BASIC.atk1) : null;
    for (const q of c.fx) if (q.k === 'stack' && q.id) lost.push(q);
    if (c.type === '공격') c.fx = [h.role === '탱커' ? { k: 'dmg', ratio: keep, base: 'def', target: 'oneEnemy' } : { k: 'dmg', ratio: keep, target: 'oneEnemy' }];
    else {
      const heal = c.fx.some(q => q.k === 'heal') && !c.fx.some(q => q.k === 'shield');
      if (!c.fx.some(q => q.k === 'heal' || q.k === 'shield')) c.name = '방어 자세';   // 약화 · 드로우 같던 딜러 기본 스킬 — 이제 실드 한 줄
      c.type = '스킬'; c.fx = [heal ? { k: 'heal', ratio: BASIC.heal } : { k: 'shield', ratio: BASIC.guard }];
    }
    delete c.tags;
  }
  // 시작 덱(기본 3 + 시동)과 고유 능력 규칙 · 고학년으로 첫 고유 효과를 쌓을 길이 없으면 시동 카드에 쌓기 하나(2026-10-11 — 기믹이 끊기지 않게)
  const walk = (fx, cb) => (fx || []).forEach(f => { cb(f); walk(f.then, cb); for (const r of f.rules || []) walk(r.fx, cb); });
  const K = h.keyword && h.keyword.name; if (!K) return;
  let ok = false; const mark = q => { if ((q.k === 'stack' && q.id === K) || q.k === 'feed') ok = true; };
  for (const id of h.starter || []) { const c = j.cards.find(c => c.id === id); if (c) walk(c.fx, mark); }
  for (const kk of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of kk.rules || []) if (!r.when || r.when.on !== 'stackGone') walk(r.fx, mark);
  for (const r of h.passives || []) walk(r.fx, mark);
  if (h.ult) walk(h.ult.fx, mark);
  if (ok) return;
  let src = lost.find(q => q.id === K);
  if (!src) for (const c of j.cards.filter(c => c.hero === h.id && c.unique)) walk(c.fx, q => { if (!src && q.k === 'stack' && q.id === K) src = q; });
  const op = j.cards.find(c => (h.starter || []).includes(c.id) && c.unique);
  if (!src || !op) return;
  const add = fx => {   // 효과 셋 상한 — 차 있으면 드로우 · 디버프 · 생성 자리에
    if (fx.some(x => x.k === 'stack' && x.id === K)) return;
    const st = { ...src, v: 1 }; const real = fx.filter(x => !/^(if|per|when)/.test(x.k));
    if (real.length < 3) { fx.push(st); return; }
    for (const k of ['draw', 'status', 'make']) { const i = fx.findIndex(x => x.k === k); if (i >= 0) { fx[i] = st; return; } }
  };
  add(op.fx); for (const o of op.oracles || []) add(o.fx);
  MOVED.push(`${h.id}: 「${K}」 쌓기 → 시동 「${op.name}」`);
  try { fs.appendFileSync(new URL('./_moved.txt', import.meta.url), `${h.id}	${K}	${op.name}
`); } catch { }
}
const walk = (fx, cb) => (fx || []).forEach(f => { cb(f); walk(f.then, cb); for (const r of f.rules || []) walk(r.fx, cb); });
export function roleCoef(j, { mulSupport = true } = {}) {
  const h = j.heroes[0]; if (h.star < 3 && h.id !== '마리') return;
  const mine = j.cards.filter(c => c.hero === h.id && !c.token);
  basicStd(j);
  if (h.role === '딜러' && DMUL !== 1) for (const c of mine.filter(c => c.unique)) {   // 역할 개성은 고유 카드로 — 기본 카드는 기준값
    const dm = fx => { let n = 0; walk(fx, q => { if (['dmg', 'extra', 'empower'].includes(q.k) && q.ratio) { q.ratio = r2(q.ratio * DMUL); n++; } }); return n; };
    const b = dm(c.fx);
    for (const o of c.oracles || []) if (!dm(o.fx) && b) { const st = o.fx.find(q => q.k === 'stack'); if (st) st.v += 1; }   // 피해가 없는 신탁 — 쌓기 +1
  }
  if (h.role === '탱커') {
    if (TROLE !== 1) for (const c of mine.filter(c => c.unique)) for (const fx of [c.fx, ...(c.oracles || []).map(o => o.fx)]) walk(fx, q => { if (['dmg', 'shield', 'heal'].includes(q.k) && q.ratio) q.ratio = r2(q.ratio * TROLE); });
    if (TDMG !== 1) for (const c of mine.filter(c => c.unique)) for (const fx of [c.fx, ...(c.oracles || []).map(o => o.fx)]) walk(fx, q => { if (q.k === 'dmg' && !q.base && q.ratio) q.ratio = r2(q.ratio * TDMG); });
    const shOf = fx => Math.max(0, ...fx.filter(q => q.k === 'shield').map(q => q.ratio));
    const u = mine.filter(c => c.unique);
    if (!u.some(c => shOf(c.fx) >= 2.2)) {
      const best = u.filter(c => c.type === '스킬' && c.cost >= 1 && shOf(c.fx) > 0).sort((a, b) => shOf(b.fx) - shOf(a.fx))[0];
      if (best) {
        const m = TSH / shOf(best.fx); best.cost = Math.max(2, best.cost);
        const put = fx => { let done = false; for (const q of fx) if (q.k === 'shield' && !done) { q.ratio = r2(q.ratio * m); done = true; }
          if (fx.length < 3 && !fx.some(q => q.k === 'status' && q.id === '반격')) fx.push({ k: 'status', id: '반격', v: 1 }); };
        put(best.fx); best.fx.find(q => q.k === 'shield').ratio = TSH;
        for (const o of best.oracles || []) if (o.cost != null && o.cost < 2) { if (!o.fx.some(q => q.k === 'shield') && o.fx.length < 3) o.fx.push({ k: 'shield', ratio: 1.2 }); }   // 비용 1 신탁(이제 −1) — 실드 반 장
        else {
          if (o.fx.some(q => q.k === 'shield')) { put(o.fx); continue; }
          // 실드가 없는 신탁 — 회복이 있으면 같은 배율로, 없으면 드로우 자리(없으면 끝)에 기본만큼 실드(기본보다 1.15배 이상 남게)
          const hl = o.fx.find(q => q.k === 'heal');
          if (hl) { hl.ratio = r2(hl.ratio * m); if (o.fx.length < 3) o.fx.push({ k: 'status', id: '반격', v: 1 }); }
          else { const di = o.fx.findIndex(q => q.k === 'draw'); const sh = { k: 'shield', ratio: 2.35 }; if (di >= 0) o.fx[di] = sh; else if (o.fx.length < 3) o.fx.push(sh); }
        }
      }
    }
    let hasDef = false; for (const c of u) walk([...c.fx, ...(c.oracles || []).flatMap(o => o.fx)], q => { if (q.base === 'def') hasDef = true; });
    if (!hasDef) {
      const atk = 115, def = 48, k = TDEF * atk / (def * 2.1 + atk * 0.3);   // 같은 피해가 되게 — 3성 탱커 표 스탯(role_stats.json), R.DefDmgStat = 방어×2.1 + 공격×0.3
      const dmgOf = c => Math.max(0, ...c.fx.filter(q => q.k === 'dmg' && !q.base).map(q => q.ratio));
      const best = u.filter(c => c.type === '공격' && dmgOf(c) > 0).sort((a, b) => dmgOf(b) - dmgOf(a))[0];
      if (best) for (const fx of [best.fx, ...(best.oracles || []).map(o => o.fx)]) for (const q of fx) if (q.k === 'dmg' && !q.base && !q.dot && !q.fixed) { q.base = 'def'; q.ratio = r2(q.ratio * k); }
    }
  }
  if (h.role === '서포터' && mulSupport)
    for (const c of mine.filter(c => c.unique)) {
      const sv = fx => { let n = 0; walk(fx, q => { if ((q.k === 'heal' || q.k === 'shield') && q.ratio) { q.ratio = r2(q.ratio * SUP); n++; } }); return n; };
      if (!sv(c.fx)) { for (const o of c.oracles || []) sv(o.fx); continue; }
      // 기본이 커졌으니 회복 · 실드가 없는 신탁도 같이 — 피해 ×1.3, 피해도 없으면 쌓기 +1
      // 회복 · 실드가 없는 신탁 — 기본의 회복 · 실드 하나를 빌려 온다(자리가 차 있으면 드로우 · 디버프 · 쌓기 자리에)
      const lend = c.fx.find(q => (q.k === 'heal' || q.k === 'shield') && q.ratio);
      for (const o of c.oracles || []) if (!sv(o.fx) && lend) {
        const cp = { ...lend, ratio: r2(lend.ratio * 0.6) };
        if (o.fx.length < 3) o.fx.push(cp);
        else { const i = o.fx.findIndex(q => q.k === 'draw'); if (i >= 0) o.fx[i] = cp; else { const st = o.fx.find(q => q.k === 'stack'); if (st) st.v += 1; } }
      }
    }
}
