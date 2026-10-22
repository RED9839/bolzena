// POST /api/auth/logout — 지금 세션을 지운다(웹 쿠키 · PC Bearer 어느 쪽이든). ?all=1 이면 그 사용자의 모든 세션(모든 기기)을.
import { requireUser, cookie, COOKIE } from "../../../lib/auth.js";
import { fail } from "../../../lib/util.js";

export async function onRequestPost({ request, env }) {
  const { me, res } = await requireUser(request, env, true, false, true);   // 정지돼도 로그아웃은 된다
  const clear = { "set-cookie": cookie(COOKIE, "", 0), "cache-control": "no-store" };
  if (res) return res.status === 401 ? new Response(null, { status: 204, headers: clear }) : res;
  if (new URL(request.url).searchParams.get("all") === "1") await env.DB.prepare("DELETE FROM sessions WHERE user_id = ?1").bind(me.user.id).run();
  else await env.DB.prepare("DELETE FROM sessions WHERE token_hash = ?1").bind(me.session.hash).run();
  return new Response(null, { status: 204, headers: clear });
}

export const onRequest = () => fail(405, "method not allowed");
