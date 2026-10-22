// GET /api/slots[?hero=키] — 내 사도 슬롯 목록(사도 · 저장 차례).
// → { slots: [{ id, hero, n, name, status(pending|ok|bad), why, hash, created, updated, slot(HeroSlot JSON), best }], count, max: { perHero: 10, perUser }, bestSeason }
//   best = 지금 시즌에 이 칸(지금 글 — 지문 hash 가 같은 것)으로 낸 최고 { score, at, attempt, status, verified } 또는 null.
//     **검증된 것 우선**(검증된 점수가 하나라도 있으면 그 가운데 최고 — 더 높은 검증 전 · 검증 안 함 점수가 있어도). 검증된 것이 없으면
//     검증 전(pending) · 검증 안 함(skipped) 가운데 가장 높은 주장 점수(status 로 표시 — 화면 「검증 전」 · 「검증 안 함」). 거절은 세지 않는다.
//   시도가 시작 때 떠 둔 슬롯 지문(attempts.slot_hashes)과 견준다 — 덮어쓰면 지문이 바뀌어 새로 센다.
// POST /api/slots?hero=키[&name=이름] (본문 = HeroSlot JSON 글 그대로) — **새로 저장**. 새 칸을 만들고 서버가 id 를 준다. n = 그 사도의 다음 차례 번호(지금 가장 큰 n + 1).
//   사도마다 LIMIT.SLOTS_PER_HERO(10칸 — 2026-10-19)까지: 그 사도의 칸이 이미 10개 이상이면 409 { error: "hero slots full", max: 10, have } — 화면은 「덮어쓸 칸을 고르세요」.
//   계정 전체 안전 한도 LIMIT.SLOTS_PER_USER(1,000칸 — 남용 막이)를 넘으면 409 { error: "too many slots", max: 1000 }. 이름은 선택(lib/slot.js — 16자 · 닉네임과 같은 글자).
//   → 201 { id, hero, n, name, status: "pending", why: null, hash, created, updated }
//   덮어쓰기 · 이름 바꾸기 · 지우기는 /api/slot/:id. 옛 주소 PUT · DELETE /api/slots/:hero/:n 도 그대로 된다.
import { requireUser } from "../../../lib/auth.js";
import { LIMIT, ID, now, sha256, readText, limited, json, fail } from "../../../lib/util.js";
import { parseSlot, checkSlotName, slotOut, slotsFull } from "../../../lib/slot.js";
import { seasonOf } from "../../../lib/season.js";

/** 지금 시즌의 내 점수 → 「사도|지문」 마다 최고(검증된 것 우선 · 같은 점수는 먼저 낸 것). */
async function bestBySlot(env, uid, season) {
  const out = new Map();
  if (!season) return out;
  const { results } = await env.DB.prepare(`
    SELECT a.id, a.heroes, a.slot_hashes, CASE WHEN a.slot_hashes IS NULL THEN a.slots_json END AS slots_json, COALESCE(s.score, s.claimed) AS score, s.status, s.submitted
    FROM scores s JOIN attempts a ON a.id = s.attempt_id
    WHERE s.user_id = ?1 AND s.season_id = ?2 AND s.status IN ('verified', 'pending', 'skipped') AND s.test = 0`).bind(uid, season.id).all();
  for (const r of results) {
    const heroes = JSON.parse(r.heroes);
    const hashes = r.slot_hashes ? JSON.parse(r.slot_hashes) : await Promise.all(JSON.parse(r.slots_json).map(async (t) => (await sha256(t)).slice(0, 16)));
    heroes.forEach((h, i) => {
      const k = h + "|" + hashes[i], b = out.get(k);
      const v = r.status === "verified";
      const better = !b || (v && !b.verified) || (v === b.verified && (r.score > b.score || (r.score === b.score && r.submitted < b.at)));
      if (better) out.set(k, { score: r.score, at: r.submitted, attempt: r.id, status: r.status, verified: v });
    });
  }
  return out;
}

const COLS = "id, hero, n, name, status, why, hash, created, updated, json";

export async function onRequestGet({ request, env }) {
  const { me, res } = await requireUser(request, env); if (res) return res;
  const hero = new URL(request.url).searchParams.get("hero");
  if (hero !== null && !ID.test(hero)) return fail(400, "bad hero");
  const q = hero !== null
    ? env.DB.prepare(`SELECT ${COLS} FROM hero_slots WHERE user_id = ?1 AND hero = ?2 ORDER BY n`).bind(me.user.id, hero.normalize("NFC"))
    : env.DB.prepare(`SELECT ${COLS} FROM hero_slots WHERE user_id = ?1 ORDER BY hero, n`).bind(me.user.id);
  const { results } = await q.all();
  const season = await seasonOf(env, null);
  const best = await bestBySlot(env, me.user.id, season);
  const count = hero === null ? results.length : (await env.DB.prepare("SELECT COUNT(*) AS c FROM hero_slots WHERE user_id = ?1").bind(me.user.id).first()).c;
  return json({
    slots: results.map((r) => slotOut(r, { slot: JSON.parse(r.json), best: best.get(r.hero + "|" + r.hash) || null })),
    count,
    max: { perHero: LIMIT.SLOTS_PER_HERO, perUser: LIMIT.SLOTS_PER_USER },
    bestSeason: season ? season.id : null,
  });
}

export async function onRequestPost({ request, env }) {
  const { me, res } = await requireUser(request, env, true, true); if (res) return res;
  const q = new URL(request.url).searchParams;
  const hero = (q.get("hero") || "").normalize("NFC");
  if (!ID.test(hero)) return fail(400, "bad hero");
  const nm = checkSlotName(q.get("name"));
  if (nm.error) return fail(400, "bad name", { why: nm.error });
  const lim = await limited(env, "slot|" + me.user.id, 30, 60); if (lim) return lim;
  const text = await readText(request, LIMIT.SLOT_BYTES);
  if (text === null) return fail(413, "too big", { max: LIMIT.SLOT_BYTES });
  const p = parseSlot(text, hero);
  if (p.why) return fail(400, p.why);
  const hash = (await sha256(text)).slice(0, 16), t = now();
  // 차례 번호 · 두 한도(사도 10칸 · 계정 1,000칸)를 한 문장에서 — 동시에 두 번 눌러도 한도를 넘지 않는다(넘으면 줄이 생기지 않아 RETURNING 이 빈다)
  let row;
  try {
    row = await env.DB.prepare(`
      INSERT INTO hero_slots (user_id, hero, n, name, json, hash, status, why, created, updated, checked)
      SELECT ?1, ?2, nn, ?3, ?4, ?5, 'pending', NULL, ?6, ?6, NULL
      FROM (SELECT COALESCE(MAX(n) + 1, 0) AS nn, COUNT(*) AS hc FROM hero_slots WHERE user_id = ?1 AND hero = ?2)
      WHERE hc < ?8 AND (SELECT COUNT(*) FROM hero_slots WHERE user_id = ?1) < ?7
      RETURNING id, hero, n, name, status, why, hash, created, updated`).bind(me.user.id, hero, nm.name, text, hash, t, LIMIT.SLOTS_PER_USER, LIMIT.SLOTS_PER_HERO).first();
  } catch (e) {
    if (/UNIQUE/i.test(String(e && e.message))) return fail(409, "busy — try again");
    throw e;
  }
  if (!row) return slotsFull(env, me.user.id, hero);
  return json(slotOut(row), 201);
}

export const onRequest = () => fail(405, "method not allowed");
