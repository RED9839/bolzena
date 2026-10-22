// GET /api/auth/discord/callback · /api/auth/google/callback ?code&state — state 를 쿠키와 맞추고 코드를 토큰으로 바꿔 사용자 번호만 받는다.
// → 사용자를 찾거나 만들고(번호는 HMAC 만) 웹 세션 쿠키를 주어 back 으로 돌려보낸다.
import { PROVIDERS, exchange, unpackState } from "../../../../lib/oauth.js";
import { cookies, cookie, OAUTH_COOKIE, userFor, finishWebLogin } from "../../../../lib/auth.js";
import { fail } from "../../../../lib/util.js";

export async function onRequestGet({ request, env, params }) {
  const provider = params.provider, p = PROVIDERS[provider];
  if (!p) return fail(404, "unknown provider");
  if (!env.DB) return fail(503, "no db");
  const url = new URL(request.url);
  const st = unpackState(cookies(request)[OAUTH_COOKIE] || "");
  const clear = cookie(OAUTH_COOKIE, "", 0, "/api/auth");
  if (url.searchParams.get("error")) return back(st?.back, "login_cancelled", clear);
  const code = url.searchParams.get("code"), state = url.searchParams.get("state");
  if (!st || st.p !== provider || !state || state !== st.state || !code) return fail(400, "bad state");
  let sub;
  try {
    const tok = await exchange(p, env, request, provider, code, st.verifier);
    sub = await p.sub(tok, env);
  } catch (e) {
    return fail(502, "provider error", { detail: String(e.message || e).slice(0, 120) });
  }
  const userId = await userFor(env, provider, sub);
  return finishWebLogin(env, userId, st.back, [clear]);
}

function back(to, err, clear) {
  const u = (to || "/") + ((to || "/").includes("?") ? "&" : "?") + "login=" + err;
  return new Response(null, { status: 302, headers: { location: u.startsWith("/") && !u.startsWith("//") ? u : "/", "set-cookie": clear } });
}

export const onRequest = () => fail(405, "method not allowed");
