// 사도 슬롯 공용 — 모양 검사 · 칸 이름 규칙 · 목록 한 줄. 새로 저장(POST /api/slots) · 덮어쓰기(PUT /api/slot/:id · 옛 PUT /api/slots/:hero/:n)가 같이 쓴다.
// 설명: ../../Docs/프론티어_서버.md §사도 슬롯
import { ID, isInt, LIMIT, fail } from "./util.js";

const TOP = new Set(["kind", "v", "hero", "rules", "content", "from", "cards", "gear", "growth"]);
const FROM = new Set(["seed", "result", "village", "nature", "ended", "floor", "party"]);
const CARD = new Set(["id", "o", "b", "vals"]);
const GROWTH = new Set(["atk", "def", "crit"]);
const CARD_ID = /^[^\s<>"'`\\/]{1,96}$/u;
const isObj = (x) => x !== null && typeof x === "object" && !Array.isArray(x);
const str = (x, n) => x === undefined || x === null || (typeof x === "string" && x.length <= n);

/** 슬롯 모양 검사 — 틀리면 까닭, 맞으면 null. 엔진 HeroSlot.Parse 는 모르는 키를 오류로 보니 여기서도 허용 목록으로 막는다. 진짜 규칙 검사는 검증기(bz verify). */
export function shape(s, hero) {
  if (!isObj(s)) return "not object";
  for (const k of Object.keys(s)) if (!TOP.has(k)) return "field " + k.slice(0, 20);
  if (s.kind !== "bolzena-hero-slot") return "kind";
  if (s.v !== 1) return "version";
  if (s.hero !== hero) return "hero 가 주소와 다르다";
  if (!str(s.rules, 32) || !str(s.content, 32)) return "fingerprint";
  const f = s.from;
  if (!isObj(f)) return "from";
  for (const k of Object.keys(f)) if (!FROM.has(k)) return "from." + k.slice(0, 20);
  if (f.result !== "clear") return "완주한 판(from.result clear)만 올릴 수 있다";
  if (f.seed !== undefined && !Number.isSafeInteger(f.seed)) return "from.seed";
  if (f.floor !== undefined && !isInt(f.floor, 0, 9)) return "from.floor";
  if (!str(f.village, 64) || !str(f.nature, 16) || !str(f.ended, 24)) return "from meta";
  if (f.party !== undefined && (!Array.isArray(f.party) || f.party.length > 4 || !f.party.every((p) => typeof p === "string" && ID.test(p)))) return "from.party";
  if (s.cards !== undefined && !Array.isArray(s.cards)) return "cards";
  const cards = s.cards || [];
  if (cards.length > 40) return "cards 40장 넘음";
  for (const c of cards) {
    if (!isObj(c)) return "card";
    for (const k of Object.keys(c)) if (!CARD.has(k)) return "card." + k.slice(0, 20);
    if (typeof c.id !== "string" || !CARD_ID.test(c.id)) return "card.id";
    if (c.o !== undefined && !isInt(c.o, 0, 9)) return "card.o";
    if (!str(c.b, 24)) return "card.b";
    if (c.vals !== undefined && c.vals !== null) {
      if (!isObj(c.vals) || Object.keys(c.vals).length > 8) return "card.vals";
      for (const [k, v] of Object.entries(c.vals)) if (k.length > 24 || !isInt(v, -1e6, 1e6)) return "card.vals";
    }
  }
  if (s.gear !== undefined && s.gear !== null) {
    if (!isObj(s.gear) || Object.keys(s.gear).length > 6) return "gear";
    for (const [k, v] of Object.entries(s.gear)) if (k.length > 16 || typeof v !== "string" || !CARD_ID.test(v)) return "gear";
  }
  if (s.growth !== undefined && s.growth !== null) {
    if (!isObj(s.growth)) return "growth";
    for (const [k, v] of Object.entries(s.growth)) if (!GROWTH.has(k) || !isInt(v, -1e6, 1e6)) return "growth";
  }
  return null;
}

/** 본문 글 → { slot } 또는 { why }(400 까닭). 글은 손대지 않고 그대로 둔다(지문 · 검증이 그 글로). */
export function parseSlot(text, hero) {
  let s; try { s = JSON.parse(text); } catch { return { why: "bad json" }; }
  const why = shape(s, hero);
  return why ? { why: "bad slot: " + why } : { slot: s };
}

// 칸 이름(선택 — 2026-10-18 사용자 「카제나 세이브 데이터처럼 이름」) — 닉네임과 같은 글자 규칙(lib/nick.js)으로 짧게.
//   이름은 주인만 본다(남의 편성 보기 · 랭킹에는 나가지 않는다) — 그래서 사칭 금지어는 보지 않는다.
export const SLOT_NAME_MAX = 16;
const NAME_OK = /^[가-힣ㄱ-ㅎㅏ-ㅣA-Za-z0-9 _.\-]+$/u;
const NAME_WORD = /[가-힣ㄱ-ㅎㅏ-ㅣA-Za-z0-9]/u;

/** 이름 검사 — { name }(빈 글 · null 이면 name null = 이름 없음 → 화면이 「사도 저장 n」) 또는 { error }. */
export function checkSlotName(raw) {
  if (raw === undefined || raw === null) return { name: null };
  if (typeof raw !== "string" || raw.length > 64) return { error: "이름은 글이어야 한다" };
  const name = raw.normalize("NFKC").trim().replace(/\s+/g, " ");
  if (name.length === 0) return { name: null };
  if ([...name].length > SLOT_NAME_MAX) return { error: `이름은 ${SLOT_NAME_MAX}자까지` };
  if (!NAME_OK.test(name)) return { error: "쓸 수 있는 글자는 한글 · 영문 · 숫자 · 공백 · _ - . 뿐" };
  if (!NAME_WORD.test(name)) return { error: "글자나 숫자가 하나는 있어야 한다" };
  return { name };
}

/** 시험 칸(migrations/0009 · /api/test/…) 목록 · 응답 한 줄 — 검증 상태 · 차례 번호가 없다(규칙 검사를 하지 않는다). test: true 로 일반 칸과 가른다. */
export const testSlotOut = (r, extra = {}) => ({ id: r.id, hero: r.hero, name: r.name ?? null, hash: r.hash, created: r.created, updated: r.updated, test: true, ...extra });

/** 목록 · 응답 한 줄(슬롯 글은 withSlot 일 때만). */
export const slotOut = (r, extra = {}) => ({ id: r.id, hero: r.hero, n: r.n, name: r.name ?? null, status: r.status, why: r.why ?? null, hash: r.hash, created: r.created, updated: r.updated, ...extra });

/** 새 칸을 만들지 못한 까닭 — 사도 10칸이 찼으면 hero slots full(화면 「덮어쓸 칸을 고를 수 있다」), 아니면 계정 안전 한도. 옛 주소 PUT 도 쓴다. */
export async function slotsFull(env, uid, hero) {
  const c = await env.DB.prepare("SELECT COUNT(*) AS c FROM hero_slots WHERE user_id = ?1 AND hero = ?2").bind(uid, hero).first();
  if (c.c >= LIMIT.SLOTS_PER_HERO) return fail(409, "hero slots full", { max: LIMIT.SLOTS_PER_HERO, have: c.c });
  return fail(409, "too many slots", { max: LIMIT.SLOTS_PER_USER });
}

