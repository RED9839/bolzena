// POST /api/auth/device/poll { deviceCode } — 게임이 interval 초마다 묻는다.
// → 202 { status: "pending" } · 429 { status: "slow_down" }(너무 자주) · 403 { status: "denied" } · 410 { status: "expired" } ·
//   200 { status: "ok", token, expiresIn } — token 은 PC 세션(Bearer). 한 번 받으면 그 기기 코드는 지운다.
import { LIMIT, now, sha256, readJson, json, fail } from "../../../../lib/util.js";
import { newSession } from "../../../../lib/auth.js";

export async function onRequestPost({ request, env }) {
  if (!env.DB) return fail(503, "no db");
  const { value, res } = await readJson(request, LIMIT.SMALL_BYTES); if (res) return res;
  const code = value && value.deviceCode;
  if (typeof code !== "string" || code.length > 100) return fail(400, "bad deviceCode");
  const h = await sha256(code), t = now();
  const row = await env.DB.prepare("SELECT user_id, status, expires, last_poll FROM device_codes WHERE device_hash = ?1").bind(h).first();
  if (!row || row.expires < t) { if (row) await env.DB.prepare("DELETE FROM device_codes WHERE device_hash = ?1").bind(h).run(); return json({ status: "expired" }, 410); }
  if (row.status === "denied") { await env.DB.prepare("DELETE FROM device_codes WHERE device_hash = ?1").bind(h).run(); return json({ status: "denied" }, 403); }
  if (row.status === "approved" && row.user_id) {
    const del = await env.DB.prepare("DELETE FROM device_codes WHERE device_hash = ?1 RETURNING user_id").bind(h).first();
    if (!del) return json({ status: "expired" }, 410);   // 동시에 두 번 물으면 한쪽만 받는다
    const { token, maxAge } = await newSession(env, del.user_id, "pc");
    return json({ status: "ok", token, expiresIn: maxAge });
  }
  if (t - row.last_poll < LIMIT.DEVICE_INTERVAL - 1) return json({ status: "slow_down", interval: LIMIT.DEVICE_INTERVAL }, 429);
  await env.DB.prepare("UPDATE device_codes SET last_poll = ?1 WHERE device_hash = ?2").bind(t, h).run();
  return json({ status: "pending", interval: LIMIT.DEVICE_INTERVAL }, 202);
}

export const onRequest = () => fail(405, "method not allowed");
