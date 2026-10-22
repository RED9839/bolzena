// GET /api/auth/device/approve?code=XXXX-XXXX — PC 게임이 연 브라우저 화면. 로그인 안 했으면 구글 · 디스코드 로그인 단추(돌아오면 이 화면),
// 했으면 「이 코드가 게임에 뜬 코드와 같은지」 보여 주고 승인 · 거절 단추.
// POST(같은 주소, 폼 code · action=approve|deny) — 웹 세션 쿠키 + 같은 사이트 요청만. 승인하면 게임의 다음 폴링이 PC 토큰을 받는다.
import { now, readText, LIMIT, fail } from "../../../../lib/util.js";
import { current, requireUser } from "../../../../lib/auth.js";

const CODE = /^[A-Z2-9]{4}-[A-Z2-9]{4}$/;

function page(title, body, status = 200) {
  return new Response(`<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>볼제나 — ${title}</title>
<style>body{font-family:system-ui,sans-serif;background:#1d1a24;color:#f3eefc;max-width:28rem;margin:3rem auto;padding:0 16px;line-height:1.6}
.code{font-size:2rem;letter-spacing:.2em;font-weight:700;background:#2c2738;padding:.5rem 1rem;border-radius:8px;display:inline-block}
a.b,button{display:inline-block;margin:.4rem .4rem 0 0;padding:.6rem 1.1rem;border-radius:8px;border:0;font:inherit;cursor:pointer;text-decoration:none;color:#fff;background:#6b55c9}
button.n{background:#4a4458}</style>
<h1>${title}</h1>${body}</html>`, { status, headers: { "content-type": "text/html; charset=utf-8", "cache-control": "no-store", "x-frame-options": "DENY", "content-security-policy": "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'" } });
}

export async function onRequestGet({ request, env }) {
  if (!env.DB) return fail(503, "no db");
  const code = (new URL(request.url).searchParams.get("code") || "").toUpperCase();
  if (!CODE.test(code)) return page("코드가 이상하다", "<p>게임에 뜬 주소를 그대로 열면 된다.</p>", 400);
  const row = await env.DB.prepare("SELECT status, expires FROM device_codes WHERE user_code = ?1").bind(code).first();
  if (!row || row.expires < now()) return page("코드가 지났다", "<p>게임에서 로그인을 다시 누르면 새 코드를 받을 수 있다.</p>", 410);
  if (row.status !== "pending") return page("이미 처리한 코드", "<p>게임으로 돌아가면 된다.</p>");
  const me = await current(request, env);
  const self = `/api/auth/device/approve?code=${code}`;
  if (!me) {
    const back = encodeURIComponent(self);
    return page("PC 게임 로그인", `<p>먼저 로그인해야 한다 — 로그인하면 이 화면으로 돌아온다.</p>
<a class="b" href="/api/auth/google/start?back=${back}">구글로 로그인</a><a class="b" href="/api/auth/discord/start?back=${back}">디스코드로 로그인</a>`);
  }
  return page("PC 게임 로그인", `<p>아래 코드가 게임 화면의 코드와 같아야 한다.</p><p class="code">${code}</p>
<p>${escapeHtml(me.user.name || "(닉네임을 아직 안 정한 새)")} 계정으로 이 PC 게임에 로그인한다.</p>
<form method="post" action="${self}"><input type="hidden" name="code" value="${code}">
<button name="action" value="approve">승인</button><button class="n" name="action" value="deny">거절</button></form>`);
}

export async function onRequestPost({ request, env }) {
  const { me, res } = await requireUser(request, env, true, false, true); if (res) return res;   // 승인 = 로그인 — 정지돼도 된다
  if (me.bearer || !request.headers.get("origin")) return fail(403, "browser form only");
  const text = await readText(request, LIMIT.SMALL_BYTES);
  if (text === null) return fail(413, "too big");
  const f = new URLSearchParams(text);
  const code = (f.get("code") || "").toUpperCase(), action = f.get("action");
  if (!CODE.test(code) || (action !== "approve" && action !== "deny")) return fail(400, "bad form");
  const r = await env.DB.prepare("UPDATE device_codes SET status = ?1, user_id = ?2 WHERE user_code = ?3 AND status = 'pending' AND expires >= ?4 RETURNING user_code")
    .bind(action === "approve" ? "approved" : "denied", me.user.id, code, now()).first();
  if (!r) return page("코드가 지났다", "<p>게임에서 로그인을 다시 누를 수 있다.</p>", 410);
  return action === "approve" ? page("승인했다", "<p>게임으로 돌아가면 몇 초 안에 로그인된다.</p>") : page("거절했다", "<p>이 코드로는 로그인되지 않는다.</p>");
}

const escapeHtml = (s) => String(s).replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);

export const onRequest = () => fail(405, "method not allowed");
