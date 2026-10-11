// 딜러 역할 정체성(2026-10-10 사용자 결정) — 딜러(엘다인 딜러 포함)는 실드 · 방어 부여 0, 회복 · 피해 감소 같은 생존 효과도 빼고
// 피해 · 강인도 · 디버프 · 손패/자원 쪽으로. 탱커 · 서포터는 그대로.
// 바꾸는 법: 피해가 있는 효과 줄 → 생존 효과를 빼고 첫 피해를 (1 + 생존량 × 0.4)배로 / 피해가 없는 줄 → 다음 카드 강화(empower)로.
// 기본 방어 카드(실드만 있는 기본 스킬)는 딜러다운 기본 스킬 「약점 노리기」 류로: 적 1명 약화 1 + 드로우 1.
// lib.run 이 고유 능력 합치기 뒤, 신탁 「수치만」 계산 앞에 부른다. countSurvival 은 셈(보고용).
const DEALER_SKILL = {"롤렛": "관객 시선 돌리기", "벨리타": "달콤한 유혹", "비비_신성": "빛 비추기", "아야": "서리 눈빛", "프리클": "가시 겨누기", "델리아": "뒤뚱 따돌리기", "란": "칼끝 겨누기", "루포": "작전 쪽지 엿보기", "모모": "연막 수리검", "베니_베니": "꿀 냄새 풍기기", "에피카": "무대 조명", "티그": "간파", "티그_영웅": "수인의 눈썰미", "로네_시장": "선거 유세", "리스티": "해킹 탐색", "아이시아": "감사 통보", "캐시": "겁먹은 눈치", "헤일리": "외계인 수색", "네르_빡침": "빡친 눈빛", "로니": "현상수배 전단", "마요_멋짐": "경매 호가", "샤샤": "물 뿌리기", "스키아": "침묵의 눈빛", "에르핀_왕도": "여왕의 명령", "에슈르": "빵 냄새 풍기기", "에슈르_마도": "마력 탐지", "다야": "보석 감정", "제이드": "옥 감정", "키디언": "그림자 추적", "림": "저울 달기", "림_혼돈": "그림자 꿰뚫기", "바롱": "으흐흐 속삭임", "니콜": "슬레이트 딱", "라이카": "정전기 찌릿", "블랑셰": "장미 향기", "실라": "바람의 눈", "아르코": "리듬 타기", "에르핀": "왕관 째려보기", "셰이디": "장난 준비"};   // 기본 방어 → 딜러 기본 스킬 이름
const SURV_ST = new Set(['피해 감소', '결의', '불굴', '끈기', '초재생', '면역', '반격']);
const isSurv = f => f && (f.k === 'shield' || f.k === 'block' || f.k === 'heal' || f.k === 'drain' || (f.k === 'status' && SURV_ST.has(f.id) && (!f.target || /self|Ally|Allies|party/.test(f.target)))
  || (f.k === 'takenMod' && f.v < 0) || f.k === 'defMod');
const amount = f => f.ratio || (f.k === 'status' ? 0.4 * (f.v || 1) : f.k === 'takenMod' ? -f.v * 3 : 0.5);
export function countSurvival(j) {
  const h = j.heroes[0]; const out = [];
  const scan = (where, fx) => { const walk = l => { for (const f of l || []) { if (isSurv(f)) out.push(`${where}:${f.k}${f.id ? '(' + f.id + ')' : ''}`); if (f.then) walk(f.then); if (f.rules) for (const r of f.rules) walk(r.fx); } }; walk(fx); };
  for (const c of j.cards.filter(c => c.hero === h.id && !c.token)) { scan(c.name, c.fx); (c.oracles || []).forEach((o, i) => scan(`${c.name}/신탁${i + 1}`, o.fx)); (c.blesses || []).forEach((o, i) => scan(`${c.name}/축복${i + 1}`, o.fx)); }
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) scan(`규칙 ${r.name}`, r.fx);
  for (const p of h.passives || []) scan(`패시브 ${p.name}`, p.fx);
  if (h.ult) scan('고학년', h.ult.fx);
  return out;
}
function fixList(fx) {
  if (!fx) return fx;
  for (const f of fx) if (f.then) f.then = fixList(f.then);   // 예약 · 함정 안쪽도
  if (!fx.some(isSurv)) return fx;
  // 「1개당」 바로 뒤의 생존 효과는 같은 몫의 피해로(1개당 뒤에는 피해 · 실드 · 회복이 와야 한다)
  fx = fx.map((f, i) => isSurv(f) && i > 0 && /^per/.test(fx[i - 1].k) ? { k: 'dmg', ratio: Math.round(amount(f) * 0.6 * 100) / 100, target: 'oneEnemy' } : f);
  if (!fx.some(isSurv)) return fx;
  const surv = fx.filter(isSurv).reduce((a, f) => a + amount(f), 0);
  let rest = fx.filter(f => !isSurv(f));
  const d = rest.find(f => f.k === 'dmg');
  if (d) d.ratio = Math.round(d.ratio * (1 + Math.min(1, surv * 0.4)) * 100) / 100;
  else if (rest.filter(f => !/^(if|per)/.test(f.k)).length >= 2) { /* 이미 효과가 둘 이상이면 생존 효과만 덜어 낸다 */ }
  else {
    const add = Math.round(Math.min(0.5, Math.max(0.2, surv * 0.3)) * 100) / 100, e = rest.find(f => f.k === 'empower' && !f.who);
    if (e) e.ratio = Math.round((e.ratio + add) * 100) / 100; else rest.push({ k: 'empower', ratio: add });   // 이미 다음 카드 강화가 있으면 더한다
  }
  return rest;
}
export function dealerRole(j) {
  const h = j.heroes[0]; if (h.role !== '딜러' || (h.star < 3 && h.id !== '마리')) return;
  for (const c of j.cards.filter(c => c.hero === h.id && !c.token)) {
    if (!c.unique && c.fx.length && c.fx.every(isSurv)) { c.fx = [{ k: 'status', id: '약화', v: 1, target: 'oneEnemy' }, { k: 'draw', v: 1 }]; if (DEALER_SKILL[h.id]) c.name = DEALER_SKILL[h.id]; continue; }   // 기본 방어 → 딜러 기본 스킬
    const walkRules = l => { for (const f of l || []) if (f.k === 'power') for (const r of f.rules) { r.fx = fixList(r.fx); walkRules(r.fx); } };
    c.fx = fixList(c.fx); walkRules(c.fx);
    for (const o of c.oracles || []) { o.fx = fixList(o.fx); walkRules(o.fx); }
    for (const b of c.blesses || []) if (b.fx) b.fx = fixList(b.fx);
  }
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) {
    if (!r.fx.some(isSurv)) continue;
    if (r.when && r.when.on === 'always') { r.fx = [{ k: 'dealtMod', v: 0.05, target: 'self' }]; continue; }   // 「항상」 생존 → 자신의 주는 피해 +5%
    r.fx = fixList(r.fx);
  }
  if (h.ult) h.ult.fx = fixList(h.ult.fx);
  // 손 보정 — 정수만 있는 신탁이라 boost 가 안 닿는 것(생존 효과를 피해로 바꾼 본판보다 낫게)
  const FIX = {
    '버터_u2': c => { c.oracles[2].fx[0].v = 5; },
    '베니_베니_u2': c => { c.oracles[4].fx = [{ k: 'stack', id: '꿀범벅', v: 4, target: 'allEnemies' }, { k: 'status', id: '약화', v: 1, target: 'allEnemies' }, { k: 'draw', v: 1 }]; },
    '베니_베니_u5': c => { c.oracles[2].fx = [{ k: 'stack', id: '꿀범벅', v: 4, target: 'allEnemies' }, { k: 'draw', v: 2 }]; },
    '마카샤_u4': c => { c.oracles[3].fx[1].v = 0.25; },
    '요미_u1': c => { c.oracles[1].fx = [{ k: 'stack', id: '달바라기', v: 4 }, { k: 'status', id: '약화', v: 2, target: 'allEnemies' }, { k: 'draw', v: 1 }]; },
    '루포_u2': c => { c.oracles[2].fx = [{ k: 'stack', id: '계획', v: 4 }, { k: 'draw', v: 2 }]; },
    '모모_u3': c => { c.oracles[1].fx = [{ k: 'stack', id: '분신', v: 4 }, { k: 'draw', v: 2 }]; },
    '라이카_u5': c => { c.oracles[4].fx[1].v = 4; },
    '림_혼돈_u3': c => { c.oracles[4].fx = [{ k: 'status', id: '약화', v: 2, target: 'oneEnemy' }, { k: 'exileFrom', from: 'discard', n: 3, who: 'other' }, { k: 'stack', id: '혼돈', v: 2 }]; },
  };
  for (const c of j.cards) if (FIX[c.id]) FIX[c.id](c);
}
