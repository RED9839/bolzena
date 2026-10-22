// POST /api/verify/result — 검증기 전용(관리자 토큰). 검증 결과를 써 넣는다.
//   { slots: [{ id, hash, status: "ok"|"bad", why }], scores: [{ id, status: "verified"|"rejected", score, why, turn }] }
//   슬롯은 hash 가 지금 것과 같을 때만(검증하는 사이에 덮어쓴 슬롯은 다시 pending 으로 남는다). 점수는 pending 일 때만 · verified 는 다시 돌린 점수 = 주장 점수일 때만.
//   rejected 로 바뀐 점수의 주인 · 시즌마다 다음 후보(가장 높은 skipped — 남은 최고보다 높으면)를 pending 으로 올린다(lib/score.js promoteNext) — 다음 검증 때 본다.
// → { slots: 바뀐 수, scores: 바뀐 수, promoted: 올린 점수 id 들 }
import { requireVerifier } from "../../../lib/verifier.js";
import { LIMIT, isInt, now, readJson, json, fail } from "../../../lib/util.js";
import { promoteNext } from "../../../lib/score.js";

const why = (x) => (typeof x === "string" ? x.slice(0, 500) : null);

export async function onRequestPost({ request, env }) {
  const bad = await requireVerifier(request, env); if (bad) return bad;
  const { value, res } = await readJson(request, LIMIT.VERIFY_BYTES); if (res) return res;
  const slots = Array.isArray(value?.slots) ? value.slots : [], scores = Array.isArray(value?.scores) ? value.scores : [];
  if (slots.length + scores.length > 200) return fail(400, "too many");
  const t = now(), db = env.DB, stmts = [];
  for (const s of slots) {
    if (!isInt(s?.id, 1, Number.MAX_SAFE_INTEGER) || typeof s.hash !== "string" || (s.status !== "ok" && s.status !== "bad")) return fail(400, "bad slot result");
    stmts.push(db.prepare("UPDATE hero_slots SET status = ?1, why = ?2, checked = ?3 WHERE id = ?4 AND hash = ?5 AND status = 'pending'").bind(s.status, why(s.why), t, s.id, s.hash));
  }
  const nSlots = stmts.length;
  for (const s of scores) {
    if (!isInt(s?.id, 1, Number.MAX_SAFE_INTEGER) || (s.status !== "verified" && s.status !== "rejected")) return fail(400, "bad score result");
    if (s.status === "verified") {
      if (!isInt(s.score, 0, 1e9)) return fail(400, "bad score");
      stmts.push(db.prepare("UPDATE scores SET status = 'verified', score = ?1, why = NULL, turn = NULL, verified = ?2 WHERE id = ?3 AND status = 'pending' AND claimed = ?1").bind(s.score, t, s.id));
    } else {
      stmts.push(db.prepare("UPDATE scores SET status = 'rejected', score = NULL, why = ?1, turn = ?2, verified = ?3 WHERE id = ?4 AND status = 'pending'")
        .bind(why(s.why), isInt(s.turn, -1, 9999) ? s.turn : null, t, s.id));
    }
  }
  if (stmts.length === 0) return json({ slots: 0, scores: 0, promoted: [] });
  const out = await db.batch(stmts);
  const changed = (rs) => rs.reduce((n, r) => n + (r.meta?.changes || 0), 0);
  // 거절된 것의 주인 · 시즌 → 다음 후보
  const rej = scores.filter((s, i) => s.status === "rejected" && (out[nSlots + i]?.meta?.changes || 0) > 0).map((s) => s.id);
  const promoted = [];
  if (rej.length > 0) {
    const { results } = await db.prepare(`SELECT DISTINCT user_id, season_id FROM scores WHERE id IN (${rej.map(() => "?").join(",")})`).bind(...rej).all();
    for (const r of results) { const p = await promoteNext(env, r.user_id, r.season_id); if (p) promoted.push(p); }
  }
  return json({ slots: changed(out.slice(0, nSlots)), scores: changed(out.slice(nSlots)), promoted });
}

export const onRequest = () => fail(405, "method not allowed");
