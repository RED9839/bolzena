// 사도 성장 — 잠재력 하나만(2026-10-21 밤 사용자 최종 결정) + 도전 사다리(난이도 단계) — 표 · 계산 · 읽기/쓰기(functions/api/prog/* · frontier/start.js 가 쓴다). 표: migrations/0010.
// 설계: ../../Docs/성장_설계_카제나판.md 맨 위 결정 상자 · §12(잠재력만 — 재료 보상 식 · 도전 사다리 · 측정).
//   레벨 · 진급 · 교주 레벨 · 던전 · 왕사탕(행동력) · 모카롱 · 역할 교본 · 골드(판 밖)는 없앴다. 잠재력 재료(마시멜로 하급 · 중급 · 상급)는 모험 끝 보상으로만 얻는다.
//
// 스위치: 환경 변수 PROG = "1" 일 때만 성장이 켜진다(progOn). 꺼져 있으면 /api/prog/* 는 404 prog off, 프론티어 시작은 성장을 떠 두지 않는다(지금과 같은 셋업).
// 게임 쪽 스위치는 콘텐츠의 성장 표(prog)가 있는가 — 둘을 같은 날 켠다(배포 순서는 설계 §12.7).
//
// 엔진 · 콘텐츠와 같아야 하는 값(test/run-local.mjs 가 견준다):
//   TIER_MAX = core Runtime/Rules/Rules.cs Tiers.MAX, POTENTIAL = 성장 표 potential(id · max · kind · after), HEROES = 콘텐츠 사도 id · 역할.
import { now, randomHex } from "./util.js";

export const progOn = (env) => env && env.PROG === "1";

/** 이 D1 에 성장 표(migrations/0010 — prog_* 표 · attempts.prog_json 열)가 있는가. 공개판은 성장 켜는 날 0010 을 건다 —
 *  그 전(꺼짐 · 0009 까지)에도 검증기 대기 줄 · 탈퇴가 없는 표 · 열을 부르지 않게(2026-10-22 배포). */
export async function progTables(env) {
  return !!(await env.DB.prepare("SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'prog_account'").first());
}

// ── 도전 사다리(난이도 단계 — 엔진 Tiers) ──
export const TIER_MAX = 9;
/** 고를 수 있는 가장 높은 단계 — 처음 1, N 을 완주하면 N + 1(엔진 Tiers.Open). */
export const tierOpen = (best) => Math.max(1, Math.min(TIER_MAX, (best | 0) + 1));

// ── 잠재력(카제나식 — 성장_설계_카제나판.md §11) ──
/**
 * 잠재력 11칸 — id · 최대 단계 · 종류 · 앞 칸(after — 이 칸을 열려면 앞 칸이 1단계 이상) = 성장 표 potential(run-local 이 견준다).
 * 카제나 그대로: 줄기 01 → 02 → 03 → 04 → 05 → 06 → 07, 갈래 03-1(03 에서) · 05-1 · 05-2(05 에서). 단계를 올리는 데는 앞 칸 단계가 상관없다.
 * 여는 조건 = 앞 칸(after) 1단계 이상 + 도전 사다리 tier 단계 완주(그 계정의 최고 완주 단계 best_tier — 2026-10-22 사용자 결정, 예전 진급 문턱 자리:
 *   진급 0 → 없음 · 1 → 단계 1 · 2 → 단계 3 · 3 → 단계 5 · 4 → 단계 7). 성장 표 potential 의 tier 와 같다(run-local 이 견준다).
 * 04-1 「치명 감각」(2026-10-22 사용자 「치확 잠재도」) — 사도 공통 치명 확률 +3%p × 5, 04 에서 갈라짐 · 단계 4 완주. 새 칸 id 만 더했다 — 이미 연 칸(prog_hero.pot)은 그대로.
 */
export const POTENTIAL = [
  { id: "01", max: 1, kind: "start", after: null, tier: 0 }, { id: "02", max: 10, kind: "card", after: "01", tier: 1 },
  { id: "03", max: 10, kind: "card", after: "02", tier: 1 }, { id: "03-1", max: 1, kind: "hero", after: "03", tier: 3 },
  { id: "04", max: 10, kind: "card", after: "03", tier: 3 }, { id: "04-1", max: 5, kind: "stat", after: "04", tier: 4 },
  { id: "05", max: 5, kind: "stat", after: "04", tier: 5 },
  { id: "05-1", max: 1, kind: "hero", after: "05", tier: 5 }, { id: "05-2", max: 1, kind: "shin", after: "05", tier: 5 },
  { id: "06", max: 5, kind: "stat", after: "05", tier: 7 }, { id: "07", max: 1, kind: "hero", after: "06", tier: 7 },
];

/**
 * 잠재력 재료 = **마시멜로**(2026-10-22 사용자 — 재화 종류를 줄인다): 하급 · 중급 · 상급 셋(열쇠 mm1 · mm2 · mm3, 원작 선택 상자 그림 하늘 · 금 · 무지개).
 *   예전 성격 재료 6 × ★1~★3 + 특수 재료 가 · 나 · 다(카제나식 19가지)를 셋으로 줄였다 — 성격 구분 없음(어느 사도에게나 같은 마시멜로).
 *   화면 이름은 여기 MM 과 게임 ProgStore.MM 두 곳(열쇠는 그대로).
 */
export const MM = [
  { key: "mm1", name: "하급 마시멜로", grade: 1 }, { key: "mm2", name: "중급 마시멜로", grade: 2 }, { key: "mm3", name: "상급 마시멜로", grade: 3 },
];
export const MM_KEYS = MM.map((m) => m.key);

/**
 * 단계 하나 올리는 값 — 카제나 칸 · 레벨별 표(note.com 2025-12-29 표, 참고_카제나_잠재력.md §4)에서 골드를 빼고(10-21 밤),
 * 특수 재료를 상급 마시멜로로 바꿨다(10-22 — 가 1 = 상급 4 · 나 1 = 상급 6 · 다 1 = 상급 10: 흔할수록 싸게, 다 = 가장 드묾).
 *   칸 [L-1] = [하급, 중급, 상급]. 05-2 는 카제나 값을 못 찾아 가안(상급 10).
 *   사도 하나 10칸 전부 = 하급 24 · 중급 125 · 상급 440(카제나 성장석 344 + 특수 몫 96).
 *   04-1(10-22 더함 — 04 의 중급 → 상급 곡선을 따라 중급 10 · 12 · 14 · 상급 10 · 14) → 사도 하나 11칸 = 하급 24 · 중급 161 · 상급 464.
 */
const C = (m1 = 0, m2 = 0, m3 = 0) => [m1, m2, m3];
export const POT_COST = {
  "01": [C(8)],
  "02": [C(4), C(6), C(6), C(0, 3), C(0, 3), C(0, 3), C(0, 3), C(0, 4), C(0, 4), C(0, 4)],
  "03": [C(0, 4), C(0, 5), C(0, 5), C(0, 6), C(0, 6), C(0, 7), C(0, 0, 5), C(0, 0, 9), C(0, 0, 10), C(0, 0, 16)],
  "03-1": [C(0, 16)],
  "04": [C(0, 8), C(0, 8), C(0, 10), C(0, 12), C(0, 14), C(0, 0, 8), C(0, 0, 8), C(0, 0, 14), C(0, 0, 14), C(0, 0, 16)],
  "04-1": [C(0, 10), C(0, 12), C(0, 14), C(0, 0, 10), C(0, 0, 14)],
  "05": [C(0, 0, 16), C(0, 0, 16), C(0, 0, 16), C(0, 0, 24), C(0, 0, 34)],
  "05-1": [C(0, 0, 38)],
  "05-2": [C(0, 0, 10)],
  "06": [C(0, 0, 20), C(0, 0, 20), C(0, 0, 24), C(0, 0, 30), C(0, 0, 44)],
  "07": [C(0, 0, 48)],
};
/** 그 칸을 L 단계로 올리는 값 — { items: { mm1 · mm2 · mm3: 수 } }. */
export function potCost(id, L) {
  const c = POT_COST[id][L - 1], items = {};
  c.forEach((n, i) => { if (n > 0) items[MM_KEYS[i]] = n; });
  return { items };
}
/** 사도 하나 11칸 전부 — { mm1, mm2, mm3 }(문서 · 시험). */
export const potTotal = () => Object.values(POT_COST).flat().reduce((a, c) => { c.forEach((n, i) => (a[MM_KEYS[i]] += n)); return a; }, { mm1: 0, mm2: 0, mm3: 0 });
/** 사도 성장 줄의 잠재력 단계(없으면 0). */
export const potOf = (h, id) => (h.pot && Number.isInteger(h.pot[id]) ? h.pot[id] : 0);
/** 그 재화의 화면 이름. */
export const itemName = (k) => (MM.find((m) => m.key === k) || {}).name || k;
/**
 * 그 칸을 한 단계 올릴 수 있나 — { ok, why(못 하면 화면 글), cost({items} | null — 최대면 null), lv, max, locked }.
 *   잠김(앞 칸)은 why 에 까닭 + locked, 재료 모자람도 why(이때 ok false).
 */
export function potCheck(acc, h, id) {
  const n = POTENTIAL.find((x) => x.id === id);
  if (!n) return null;
  const lv = potOf(h, id);
  if (lv >= n.max) return { ok: false, why: "최대 단계다", cost: null, lv, max: n.max, locked: false };
  const cost = potCost(id, lv + 1);
  let why = null, locked = false;
  if ((acc.bestTier | 0) < n.tier) { why = `난이도 단계 ${n.tier} 을 완주하면 열린다`; locked = true; }
  else if (n.after && potOf(h, n.after) < 1) { why = `앞 칸(${n.after})을 먼저 열어야 한다`; locked = true; }
  else for (const [k, v] of Object.entries(cost.items)) if ((acc.bag[k] | 0) < v) { why = `${itemName(k)}가 모자라다(${acc.bag[k] | 0}/${v})`; break; }
  return { ok: !why, why, cost, lv, max: n.max, locked };
}
/** 화면에 주는 사도 하나의 잠재력 11칸 — [{ id, lv, max, after, tier, next: {items} | null, ok, why, locked }]. */
export const potView = (acc, h) => POTENTIAL.map((n) => { const c = potCheck(acc, h, n.id); return { id: n.id, lv: c.lv, max: n.max, after: n.after, tier: n.tier, next: c.cost, ok: c.ok, why: c.why, locked: c.locked }; });

// ── 재화 — 마시멜로 셋뿐 ──
export const CURRENCY = MM.map((m) => ({ key: m.key, name: m.name, use: "잠재력 칸 열기 · 단계 올리기 — 모험 끝 보상" }));

// ── 모험 끝 보상(마시멜로) ──
/**
 * 단계별 완주 한 판의 마시멜로 [하급, 중급, 상급](편성 크기와 상관없이 판마다 — 마시멜로는 공용) — 2026-10-22 사용자 목표 「사도 한 명 10칸 ≈ 완주 10판」.
 *   기준 길: 그 칸을 여는 단계 근처에서 완주(01~03 = 단계 1~2 두 판 · 03-1 · 04 = 3~4 두 판 · 05 갈래 = 5~6 세 판 · 06 · 07 = 7~8 세 판) → 한 사도 10칸(하급 24 · 중급 125 · 상급 440)이 꼭 10판.
 *   낮은 단계일수록 하급 · 중급, 높을수록 상급(카제나 성장석 등급이 단계를 따라 오르는 꼴). 하급 · 중급은 기준 길에서 남는 만큼 다음 사도 몫으로 쌓인다.
 *   (예전 「배율 × 기본 + 완주 덤」 식과 엔진 Tiers.REWARD 는 이 표로 바꿨다 — 단계마다 등급이 바뀌어 배율 하나로는 못 그린다.)
 *   2026-10-22 04-1 「치명 감각」 을 더하며 상급을 단계 3 · 4 +6(36 → 42) · 단계 5~9 +2 올렸다 — 기준 길 10판 상급 합 440 → 464 = 사도 하나 11칸 상급(그대로 10판).
 *   중급은 그대로(기준 길 214 ≥ 11칸 161).
 */
export const MATS = [null, [12, 28, 24], [12, 28, 24], [4, 34, 42], [4, 34, 42], [3, 16, 48], [3, 16, 52], [3, 14, 56], [3, 14, 62], [3, 14, 66]];
/** 못 깬 판 — 그 단계 완주 몫의 비율(1층에서 멈춤 0.15 · 2층에서 멈춤 0.35 — 평균 약 1/4). 완주 1. */
export const progressOf = (result, floor) => (result === "win" ? 1 : floor >= 2 ? 0.35 : 0.15);
/** 판 하나의 마시멜로(자르기 전) — 화면 미리 보기(게임 ProgStore.WinMat · RunMat)와 같은 식. */
export function runMats(tier, result, floor) {
  const k = progressOf(result, floor), o = {};
  MATS[tier].forEach((n, i) => { const v = Math.round(n * k); if (v > 0) o[MM_KEYS[i]] = v; });
  return o;
}
/** 주간 상한(판 결과를 지어낼 수 있으니 위조 이득의 상한, Docs/성장_설계_카제나판.md §12.2) — 등급마다 표의 가장 큰 한 판 몫 × 15판. */
export const WEEK_CAP = { mm1: 180, mm2: 510, mm3: 990 };
export const KEYS = Object.keys(WEEK_CAP);
/** 완주로 치는 가장 짧은 판(분) — 이보다 빠른 완주는 받지 않는다(모양 검사). */
export const MIN_CLEAR_MIN = 3;

// ── 사도(콘텐츠 bolzena-content-v2 heroes — 엘다인 20명) → 역할 ──
export const HEROES = {
  "비비_신성": "딜러", "아야": "딜러", "요미": "서포터", "디아나_왕년": "딜러", "란": "딜러", "에피카": "딜러", "우로스": "서포터",
  "티그_영웅": "딜러", "리뉴아": "딜러", "이드": "탱커", "네르_빡침": "딜러", "에르핀_왕도": "딜러", "죠안": "서포터", "클로에": "탱커",
  "비비": "탱커", "실비아": "서포터", "벨라": "탱커", "시온더다크불릿": "딜러", "우이": "서포터", "우이_기억": "서포터",
};

/** 주 번호 — 월요일 0시(UTC)에 바뀐다(1970-01-01 은 목요일). */
export const weekOf = (t = now()) => Math.floor((Math.floor(t / 86400) + 3) / 7);
export const weekEnds = (w) => (w * 7 + 7 - 3) * 86400;

const parse = (s, d) => { try { const v = JSON.parse(s); return v && typeof v === "object" && !Array.isArray(v) ? v : d; } catch { return d; } };

// ── 읽기 ──
/** 계정 줄(없으면 기본값 — 아직 안 만든 것). 주가 바뀌었으면 받은 양을 0 으로 본다. */
export async function loadAccount(env, uid, t = now()) {
  const r = await env.DB.prepare("SELECT best_tier, bag, week, week_got, runs, rev FROM prog_account WHERE user_id = ?1").bind(uid).first();
  const w = weekOf(t);
  return !r ? { bestTier: 0, bag: {}, week: w, got: {}, runs: 0, rev: 0 }
    : { bestTier: r.best_tier, bag: parse(r.bag, {}), week: w, got: r.week === w ? parse(r.week_got, {}) : {}, runs: r.runs, rev: r.rev };
}

/** 잠재력 칸 JSON → { 칸: 단계 }(아는 칸 · 1~max 정수만). */
const potParse = (s) => { const v = parse(s, {}), o = {}; for (const n of POTENTIAL) if (Number.isInteger(v[n.id]) && v[n.id] > 0) o[n.id] = Math.min(n.max, v[n.id]); return o; };
/** 사도 줄들 → { 사도: { pot } }(줄 없는 사도는 빠진다 — heroOf 가 기본값). */
export async function loadHeroes(env, uid) {
  const rows = (await env.DB.prepare("SELECT hero, pot FROM prog_hero WHERE user_id = ?1").bind(uid).all()).results;
  const o = {};
  for (const r of rows) o[r.hero] = { pot: potParse(r.pot) };
  return o;
}
export const heroOf = (heroes, k) => heroes[k] || { pot: {} };

/** 화면에 주는 내 성장 전부(GET /api/prog · 쓰기 응답). */
export function view(acc, heroes) {
  const hs = {};
  for (const [k, role] of Object.entries(HEROES)) {
    const h = heroOf(heroes, k);
    hs[k] = { role, pot: { ...(h.pot || {}) }, potential: potView(acc, h) };
  }
  return {
    on: true, rev: acc.rev,
    bestTier: acc.bestTier, open: tierOpen(acc.bestTier),
    wallet: Object.fromEntries(MM_KEYS.map((k) => [k, acc.bag[k] | 0])),
    currency: CURRENCY,
    week: { got: Object.fromEntries(KEYS.map((k) => [k, acc.got[k] | 0])), cap: WEEK_CAP, ends: weekEnds(acc.week) },
    heroes: hs,
    rules: { mats: MATS, potential: POTENTIAL, potCost: POT_COST },
    runs: acc.runs,
  };
}

/**
 * 프론티어 시도에 떠 둘 성장(엔진 ProgState JSON — { heroes: { 사도: { pot } } }) — 편성 사도 가운데 잠재력이 하나라도 있는 사도만(표 차례).
 * 편성 누구도 잠재력이 없으면 null — 성장 끔과 같은 셋업(prog 칸 없음).
 */
export function snapshot(heroes, party) {
  const hs = {};
  for (const k of party) {
    const pot = heroOf(heroes, k).pot || {};
    if (Object.keys(pot).length > 0) hs[k] = { pot: Object.fromEntries(POTENTIAL.filter((n) => pot[n.id] > 0).map((n) => [n.id, pot[n.id]])) };
  }
  return Object.keys(hs).length > 0 ? { heroes: hs } : null;
}

// ── 계산 ──
/**
 * 모험 끝 보상(서버 계산) — 판 결과 → runMats(단계 표 MATS × 진행 비율) → 주간 상한으로 자름. 편성 크기와 상관없이 판마다.
 * → { progress, want(자르기 전), give: { items(열쇠 → 수) }, got(= items, 상한 칸별), capped(잘린 칸들) }
 */
export function rewardOf(acc, { tier, result, floor }) {
  const progress = progressOf(result, floor);
  const want = Object.fromEntries(KEYS.map((c) => [c, 0]));
  Object.assign(want, runMats(tier, result, floor));
  const items = {}, got = Object.fromEntries(KEYS.map((c) => [c, 0]));
  for (const c of KEYS) { const g = Math.min(want[c], Math.max(0, WEEK_CAP[c] - (acc.got[c] | 0))); got[c] = g; if (g > 0) items[c] = g; }
  const capped = KEYS.filter((c) => got[c] < want[c]);
  return { progress, want, give: { items }, got, capped };
}

// ── 쓰기 ──
/**
 * 계정 · 바뀐 사도 줄을 한 batch 로 쓴다 — 계정 줄은 읽을 때의 rev 와 같을 때만(낙관적 잠금), 사도 줄은 그 계정 쓰기가 이긴 때만(tx 표).
 * 이기면 true(acc.rev 를 새 값으로), 다른 요청이 먼저 썼으면 false(다시 읽고 다시 셈할 것).
 * extra — (tx) => 문장. 같은 batch 에 넣을 기록 줄(잠재력 기록 등) — 「WHERE (SELECT tx …) = tx」 로 계정 쓰기가 이긴 때만 들어가게 짓는다.
 */
export async function save(env, uid, acc, heroes, changed, t = now(), extra = []) {
  const db = env.DB, tx = randomHex(8);
  const cols = [acc.bestTier, JSON.stringify(acc.bag), acc.week, JSON.stringify(acc.got), acc.runs, tx, t];
  const head = acc.rev === 0
    ? db.prepare("INSERT INTO prog_account (user_id, best_tier, bag, week, week_got, runs, tx, updated, rev) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, 1) ON CONFLICT (user_id) DO NOTHING")
      .bind(uid, ...cols)
    : db.prepare("UPDATE prog_account SET best_tier = ?2, bag = ?3, week = ?4, week_got = ?5, runs = ?6, tx = ?7, updated = ?8, rev = rev + 1 WHERE user_id = ?1 AND rev = ?9")
      .bind(uid, ...cols, acc.rev);
  const rows = [head];
  for (const k of changed) {
    const h = heroes[k];
    const pot = h.pot && Object.keys(h.pot).length > 0 ? JSON.stringify(h.pot) : null;
    rows.push(db.prepare(`INSERT INTO prog_hero (user_id, hero, pot, updated) SELECT ?1, ?2, ?3, ?4 WHERE (SELECT tx FROM prog_account WHERE user_id = ?1) = ?5
      ON CONFLICT (user_id, hero) DO UPDATE SET pot = excluded.pot, updated = excluded.updated`).bind(uid, k, pot, t, tx));
  }
  for (const s of extra) rows.push(s(tx));   // 같은 batch 에 묶을 기록 줄(계정 쓰기가 이긴 때만 — tx 로 거른다)
  const res = await db.batch(rows);
  if (!res[0].meta || res[0].meta.changes !== 1) return false;
  acc.rev = acc.rev + 1;
  return true;
}
