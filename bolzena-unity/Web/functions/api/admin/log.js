// GET /api/admin/log[?user=id][&action=이름][&n=50] — 운영자 전용. 운영 기록(최근 차례) — 누가 · 언제 · 무엇을 · 대상 · 까닭 · 바뀐 것.
// → { rows: [{ id, at, actor, action, user, name(그 사용자의 지금 닉네임 — 탈퇴했으면 null), target, reason, detail }] }
import { requireAdmin, logOut } from "../../../lib/admin.js";
import { json, fail } from "../../../lib/util.js";

export async function onRequestGet({ request, env }) {
  const { res } = await requireAdmin(request, env); if (res) return res;
  const q = new URL(request.url).searchParams;
  const user = q.get("user"), action = q.get("action");
  if (user !== null && !/^[1-9][0-9]{0,15}$/.test(user)) return fail(400, "bad user");
  if (action !== null && !/^[a-z.]{1,24}$/.test(action)) return fail(400, "bad action");
  const n = q.get("n") === null ? 50 : Number(q.get("n"));
  if (!Number.isSafeInteger(n) || n < 1 || n > 500) return fail(400, "n 은 1~500");
  const { results } = await env.DB.prepare(`SELECT l.id, l.at, l.actor, l.action, l.user_id, l.target, l.reason, l.detail, u.nick AS name
    FROM admin_log l LEFT JOIN users u ON u.id = l.user_id
    WHERE (?1 IS NULL OR l.user_id = ?1) AND (?2 IS NULL OR l.action = ?2) ORDER BY l.id DESC LIMIT ?3`).bind(user === null ? null : Number(user), action, n).all();
  return json({ rows: results.map(logOut) });
}

export const onRequest = () => fail(405, "method not allowed");
