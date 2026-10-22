// GET /api/auth/discord/start · /api/auth/google/start ?back=/경로 — 공급자 로그인 화면으로 보낸다.
// state(+ 구글은 PKCE verifier)를 10분짜리 쿠키(Path=/api/auth)에 두고, callback 이 맞춰 본다.
import { PROVIDERS, redirectUri, challenge, packState } from "../../../../lib/oauth.js";
import { cookie, OAUTH_COOKIE, safeBack } from "../../../../lib/auth.js";
import { randomToken, ipKey, limited, fail } from "../../../../lib/util.js";

export async function onRequestGet({ request, env, params }) {
  const provider = params.provider, p = PROVIDERS[provider];
  if (!p) return fail(404, "unknown provider");
  if (!p.id(env) || !p.secret(env)) return fail(503, `${provider} login not configured`);
  if (env.DB) { const r = await limited(env, "auth|" + await ipKey(request), 20, 60); if (r) return r; }
  const state = randomToken(24), verifier = p.pkce ? randomToken(48) : undefined;
  const back = safeBack(new URL(request.url).searchParams.get("back"));
  const q = new URLSearchParams({ response_type: "code", client_id: p.id(env), redirect_uri: redirectUri(request, provider), scope: p.scope, state });
  if (provider === "discord") q.set("prompt", "none");
  if (p.pkce) { q.set("code_challenge", await challenge(verifier)); q.set("code_challenge_method", "S256"); }
  return new Response(null, {
    status: 302,
    headers: { location: `${p.authorize}?${q}`, "set-cookie": cookie(OAUTH_COOKIE, packState({ p: provider, state, verifier, back }), 600, "/api/auth"), "cache-control": "no-store" },
  });
}

export const onRequest = () => fail(405, "method not allowed");
