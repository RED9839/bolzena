// 디스코드 · 구글 OAuth — 받는 범위 최소(디스코드 identify · 구글 openid). 이메일 · 닉네임 · 사진은 받지 않는다(사용자 번호만).
// 클라이언트 id · 시크릿은 환경 변수(DISCORD_CLIENT_ID · DISCORD_CLIENT_SECRET · GOOGLE_CLIENT_ID · GOOGLE_CLIENT_SECRET) — 값은 저장소에 두지 않는다.
import { b64url } from "./util.js";

export const PROVIDERS = {
  discord: {
    id: (env) => env.DISCORD_CLIENT_ID, secret: (env) => env.DISCORD_CLIENT_SECRET,
    authorize: "https://discord.com/oauth2/authorize", token: "https://discord.com/api/oauth2/token",
    scope: "identify", pkce: false,
    /** 토큰 → 사용자 번호. */
    async sub(tok) {
      const r = await fetch("https://discord.com/api/users/@me", { headers: { authorization: `Bearer ${tok.access_token}` } });
      if (!r.ok) throw new Error("discord users/@me " + r.status);
      const u = await r.json();
      if (typeof u.id !== "string" || !/^\d{1,24}$/.test(u.id)) throw new Error("discord id");
      return u.id;
    },
  },
  google: {
    id: (env) => env.GOOGLE_CLIENT_ID, secret: (env) => env.GOOGLE_CLIENT_SECRET,
    authorize: "https://accounts.google.com/o/oauth2/v2/auth", token: "https://oauth2.googleapis.com/token",
    scope: "openid", pkce: true,
    /** id_token 은 구글 토큰 주소에서 TLS 로 바로 받은 것이라 서명 검사 없이 내용만 본다(OpenID Connect Core 3.1.3.7) — iss · aud · exp 는 본다. */
    async sub(tok, env) {
      const part = String(tok.id_token || "").split(".")[1];
      if (!part) throw new Error("google id_token 없음");
      const p = JSON.parse(atob(part.replace(/-/g, "+").replace(/_/g, "/")));
      if (p.iss !== "https://accounts.google.com" && p.iss !== "accounts.google.com") throw new Error("google iss");
      if (p.aud !== env.GOOGLE_CLIENT_ID) throw new Error("google aud");
      if (!(p.exp * 1000 > Date.now())) throw new Error("google exp");
      if (typeof p.sub !== "string" || !/^\d{1,40}$/.test(p.sub)) throw new Error("google sub");
      return p.sub;
    },
  },
};

export const redirectUri = (request, provider) => new URL(`/api/auth/${provider}/callback`, request.url).toString();

/** PKCE S256 — verifier → challenge. */
export async function challenge(verifier) {
  const d = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(verifier));
  return b64url(new Uint8Array(d));
}

/** 코드 → 토큰(공급자 토큰 주소). */
export async function exchange(p, env, request, provider, code, verifier) {
  const body = new URLSearchParams({
    grant_type: "authorization_code", code, redirect_uri: redirectUri(request, provider),
    client_id: p.id(env), client_secret: p.secret(env),
  });
  if (p.pkce) body.set("code_verifier", verifier);
  const r = await fetch(p.token, { method: "POST", headers: { "content-type": "application/x-www-form-urlencoded", accept: "application/json" }, body });
  if (!r.ok) throw new Error(`${provider} token ${r.status}`);
  return r.json();
}

/** 짧은 쿠키에 담는 OAuth 진행 상태(state · PKCE verifier · 돌아갈 곳) — base64url JSON. 서명은 없다: 이 사이트 쿠키는 남이 못 심고, state 를 공급자가 돌려준 값과 맞춰 볼 뿐이다. */
export const packState = (o) => b64url(new TextEncoder().encode(JSON.stringify(o)));
export function unpackState(s) {
  try { return JSON.parse(new TextDecoder().decode(Uint8Array.from(atob(s.replace(/-/g, "+").replace(/_/g, "/")), (c) => c.charCodeAt(0)))); } catch { return null; }
}
