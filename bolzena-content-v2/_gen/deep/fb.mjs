// 빌드 피드백 반영(2026-10-10 — _measure/사도_빌드/피드백.md · 인연_자원공유.md) — 묶음별 손질을 lib.run 이 틀 다양성 뒤 · 고유 능력 합치기 앞에 부른다.
// 신탁 번호는 1부터(문서와 같다). 문서 fx 는 스냅샷(12:47) 꼴 — 그 뒤 틀 다양성 · 딜러 손질로 바뀐 카드는 문서의 뜻(fx 꼴)대로 넣는다.
import { FB_MAJO } from './fb_majo.mjs';
import { FB_SUIN } from './fb_suin.mjs';
import { FB_ELF } from './fb_elf.mjs';
import { FB_YOJEONG } from './fb_yojeong.mjs';
import { FB_YONGJOK } from './fb_yongjok.mjs';
import { FB_YURYEONG } from './fb_yuryeong.mjs';
import { FB_JEONGRYEONG } from './fb_jeongryeong.mjs';
import { FB_PARTY } from './fb_party.mjs';
import { FB_KAZENA } from './fb_kazena.mjs';
import { FB_TRIG, trigAny } from './fb_trig.mjs';
import { FB_BUILD } from './fb_build.mjs';
const card = (j, id) => { const c = j.cards.find(c => c.id === id); if (!c) throw new Error('피드백: 카드 없음 ' + id); return c; };
export const H = {
  /** 신탁 n 을 fx 로(덧붙일 칸: power · cost 등). */
  O(j, id, n, fx, extra = {}) { const o = card(j, id).oracles[n - 1]; if (!o) throw new Error(`피드백: ${id} 신탁 ${n} 없음`); o.fx = fx; delete o.numOnly; Object.assign(o, extra); },
  /** 신탁 n 의 fx 를 고쳐 쓴다. */
  M(j, id, ns, fn) { for (const n of ns) { const o = card(j, id).oracles[n - 1]; o.fx = fn(o.fx) || o.fx; } },
  /** 쌓기 수를 상한으로 — 그 신탁 안의 stack(id) v 를 v 로. */
  cap(j, id, ns, kid, v) { H.M(j, id, ns, fx => { for (const f of fx) if (f.k === 'stack' && f.id === kid) f.v = v; }); },
  /** 고유 효과(이름)를 찾는다. */
  kw(j, name) { const h = j.heroes[0]; const k = [h.keyword, ...(h.keywords || [])].find(x => x && x.name === name); if (!k) throw new Error('피드백: 고유 효과 없음 ' + name); return k; },
  /** 고유 능력 규칙 더하기(같은 이름이 있으면 바꾼다) — 패시브로 넣어 합치기가 옮긴다. */
  rule(j, r) { const h = j.heroes[0]; h.passives = (h.passives || []).filter(p => p.name !== r.name); h.passives.push(r); },
  /** 패시브(또는 이미 옮긴 규칙) 지우기. */
  del(j, name) { const h = j.heroes[0]; h.passives = (h.passives || []).filter(p => p.name !== name); for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) if (k.rules) k.rules = k.rules.filter(r => r.name !== name); },
  /** 패시브(또는 고유 능력 규칙) 하나를 이름으로 고친다. */
  P(j, name, fn) { const h = j.heroes[0]; const all = [...(h.passives || []), ...[h.keyword, ...(h.keywords || [])].filter(Boolean).flatMap(k => k.rules || [])]; const r = all.find(x => x.name === name); if (!r) throw new Error('피드백: 규칙 없음 ' + name); fn(r); },
  card,
};
const ALL = { ...FB_MAJO, ...FB_SUIN, ...FB_ELF, ...FB_YOJEONG, ...FB_YONGJOK, ...FB_YURYEONG, ...FB_JEONGRYEONG };
// 공통 피드백(§2) — 압도 기절 신탁 → 적의 다음 공격 한 대 -50%(대응 불가 끊기 대신) · 「다 차면 모두 써서」 vs 「전부 소모」 다툼 · 넘친 회복 per 누락
const STUN = { '셀리네_u1': [2] };   // 셰이디(역전) · 셰이디 · 림(혼돈) 은 사도 절대로(약화 · 늦춤)   // 베니(베니) · 티그(영웅) 기절은 사도 절대로(기절 1 · 칼끝 2 조건)
function common(j) {
  const h = j.heroes[0];
  for (const c of j.cards) if (STUN[c.id]) for (const n of STUN[c.id]) { const o = c.oracles[n - 1]; o.fx = o.fx.map(f => (f.k === 'status' && f.id === '기절' ? { k: 'cutHit', v: 0.5 } : f)); delete o.numOnly; }
  const spendsAll = id => j.cards.some(c => c.unique && [c.fx, ...(c.oracles || []).map(o => o.fx)].some(fx => fx.some(f => f.k === 'spend' && f.all && f.id === id)));
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) if (k.onMax && k.onMax.empower && k.onMax.consume && spendsAll(k.name)) delete k.onMax.consume;
  const walk = l => { for (const f of l || []) { if (f.k === 'perOverheal' && !f.per) f.per = 20; if (f.then) walk(f.then); if (f.rules) for (const r of f.rules) walk(r.fx); } };
  for (const c of j.cards) { walk(c.fx); for (const o of c.oracles || []) walk(o.fx); }
  for (const p of h.passives || []) walk(p.fx);
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) walk(r.fx);
}
/** 같은 글 신탁이 남으면 뒤 칸을 다른 방향으로(대상 → 전체 · 아니면 드로우 덤 · 아니면 쌓기 +1). */
export function dedupe(j) {
  for (const c of j.cards.filter(c => c.unique)) {
    const seen = [];
    for (const o of c.oracles || []) {
      const key = JSON.stringify([o.fx, o.cost ?? null, !!o.power]);
      if (seen.includes(key)) {
        const d = o.fx.find(f => f.k === 'dmg' && f.target === 'oneEnemy');
        if (d) { d.target = 'allEnemies'; d.ratio = Math.round(d.ratio * 0.6 * 100) / 100; }
        else if (o.fx.filter(f => !/^(if|per|when)/.test(f.k)).length < 3) o.fx.push({ k: 'draw', v: 1 });
        else { const s = o.fx.find(f => f.k === 'stack'); if (s) s.v += 1; }
        delete o.numOnly;
      }
      seen.push(JSON.stringify([o.fx, o.cost ?? null, !!o.power]));
    }
  }
}
export function applyFeedback(j, f) { if (j.heroes[0].star < 3 && j.heroes[0].id !== '마리') return; common(j); if (ALL[f]) ALL[f](j, H); if (FB_PARTY[f]) FB_PARTY[f](j, H); if (FB_KAZENA[f]) FB_KAZENA[f](j, H); if (FB_TRIG[f]) FB_TRIG[f](j, H); if (FB_BUILD[f] && !process.env.NOBUILD) FB_BUILD[f](j, H); trigAny(j); }
