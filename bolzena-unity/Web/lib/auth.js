// 계정 · 세션 — 세션은 JWT 말고 무작위 토큰 + D1(sessions 에는 토큰 해시만). 웹은 HttpOnly 쿠키, PC 는 Bearer.
// 설명: ../../Docs/프론티어_서버.md §로그인
import { LIMIT, now, sha256, hmac, randomToken, fail, crossOrigin } from "./util.js";

export const COOKIE = "bz_s";
export const OAUTH_COOKIE = "bz_oauth";

export function cookies(request) {
  const out = {};
  for (const part of (request.headers.get("cookie") || "").split(";")) {
    const i = part.indexOf("=");
    if (i > 0) out[part.slice(0, i).trim()] = part.slice(i + 1).trim();
  }
  return out;
}

/** 쿠키 머리 — HttpOnly · Secure · SameSite=Lax. maxAge 0 이면 지운다. */
export function cookie(name, value, maxAge, path = "/") {
  return `${name}=${value}; Path=${path}; Max-Age=${maxAge}; HttpOnly; Secure; SameSite=Lax`;
}

/** 요청의 세션 토큰 — Authorization: Bearer(PC) 가 먼저, 없으면 쿠키(웹). { token, bearer }. */
function tokenOf(request) {
  const a = request.headers.get("authorization") || "";
  if (a.startsWith("Bearer ")) return { token: a.slice(7).trim(), bearer: true };
  const c = cookies(request)[COOKIE];
  return c ? { token: c, bearer: false } : { token: null, bearer: false };
}

// 계정 정지(migrations/0006 — 운영 도구) — users.banned_until = 풀리는 때. 이 값 이상이면 영구(3000-01-01).
export const BAN_FOREVER = 32503680000;

/** 사용자 줄(banned_until · ban_reason) → 지금 정지면 { why, until(유닉스 초 — 영구면 null), forever }, 아니면 null(지난 정지는 저절로 풀린 것). */
export function banOf(row, t = now()) {
  if (!row || row.banned_until === null || row.banned_until === undefined || row.banned_until <= t) return null;
  const forever = row.banned_until >= BAN_FOREVER;
  return { why: row.ban_reason || null, until: forever ? null : row.banned_until, forever };
}

/** 정지된 사람의 쓰기 요청 응답 — 403 { error: "banned", banned: true, why, until, forever }(화면이 「계정이 정지됐다 — 까닭 · 풀리는 때」). */
export const banned = (ban) => fail(403, "banned", { banned: true, ...ban });

/**
 * 랭킹 · 남의 편성 보기에 보이는 사용자인가(SQL 조각) — 정지 중이 아니고 · 시험 계정(role 'tester' — migrations/0009)이 아니고 · 그 시즌(또는 모든 시즌 「*」)에 숨겨지지 않았다.
 * 시험 계정이 연 시도 · 점수는 줄마다 test = 1 로도 적혀(start.js · submit.js) 역할을 회수해도 계속 빠진다 — 점수 줄을 읽는 쿼리는 「s.test = 0」 도 같이 본다.
 * u = users 별칭, season = 시즌 id 를 가리키는 SQL(바인딩 「?1」 이나 열 이름).
 */
export const VISIBLE = (u, season) => `(${u}.banned_until IS NULL OR ${u}.banned_until <= unixepoch()) AND ${u}.role IS NULL AND NOT EXISTS (SELECT 1 FROM rank_hidden h WHERE h.user_id = ${u}.id AND h.season_id IN (${season}, '*'))`;

/** 로그인한 사용자 — { user, session, bearer, ban(정지면 { why, until, forever }) } 또는 null. 만료된 세션은 지운다. */
export async function current(request, env) {
  const { token, bearer } = tokenOf(request);
  if (!token || token.length > 100) return null;
  const h = await sha256(token);
  const row = await env.DB.prepare("SELECT s.token_hash, s.kind, s.expires, u.id, u.provider, u.nick, u.nick_changed, u.created, u.banned_until, u.ban_reason, u.role FROM sessions s JOIN users u ON u.id = s.user_id WHERE s.token_hash = ?1")
    .bind(h).first();
  if (!row) return null;
  if (row.expires < now()) { await env.DB.prepare("DELETE FROM sessions WHERE token_hash = ?1").bind(h).run(); return null; }
  return { user: { id: row.id, provider: row.provider, name: row.nick || null, nameChanged: row.nick_changed || null, created: row.created, role: row.role || null }, session: { hash: h, kind: row.kind, expires: row.expires }, bearer, ban: banOf(row) };
}

/**
 * 로그인을 요구한다 — { me } 또는 { res }(401 · 403). write = 바꾸는 요청: 쿠키로 왔으면 다른 사이트 요청을 막는다
 * (Bearer 는 브라우저가 알아서 붙이지 않으니 CSRF 걱정이 없다). named = 가입을 끝낸(닉네임을 정한) 사람만 — 아니면 403 { needsName: true }.
 * 정지된 사람은 모든 쓰기(write)가 403 { banned: true, why, until, forever } — banOk 인 주소(로그아웃 · 기기 코드 승인 = 로그인 · 탈퇴)만 된다. 읽기는 그대로.
 */
export async function requireUser(request, env, write = false, named = false, banOk = false) {
  if (!env.DB) return { res: fail(503, "no db") };
  const me = await current(request, env);
  if (!me) return { res: fail(401, "login required") };
  if (write && !me.bearer && crossOrigin(request)) return { res: fail(403, "cross origin") };
  if (write && !banOk && me.ban) return { res: banned(me.ban) };
  if (named && !me.user.name) return { res: fail(403, "name required", { needsName: true }) };
  return { me };
}

/**
 * 시험 계정을 요구한다(migrations/0009 — 시험 칸 주소) — { me } 또는 { res }. 역할은 서버의 users.role 만 본다(게임이 보내는 값은 믿지 않는다).
 * 로그인 · 가입(닉네임) · 정지 검사는 requireUser 와 같고, 그 뒤 role 이 'tester' 가 아니면 403 { error: "tester only" }.
 */
export async function requireTester(request, env, write = false) {
  const r = await requireUser(request, env, write, true);
  if (r.res) return r;
  if (r.me.user.role !== "tester") return { res: fail(403, "tester only", { why: "시험 계정만 쓸 수 있는 주소" }) };
  return r;
}

/** 새 세션 — 토큰(본래 값)은 한 번만 돌려준다. */
export async function newSession(env, userId, kind) {
  const token = randomToken(32), t = now();
  const ttl = kind === "pc" ? LIMIT.PC_SESSION_SEC : LIMIT.WEB_SESSION_SEC;
  await env.DB.prepare("INSERT INTO sessions (token_hash, user_id, kind, created, expires) VALUES (?1, ?2, ?3, ?4, ?5)")
    .bind(await sha256(token), userId, kind, t, t + ttl).run();
  if (Math.random() < 0.05) await env.DB.prepare("DELETE FROM sessions WHERE expires < ?1").bind(t).run();
  return { token, maxAge: ttl };
}

/** 공급자 번호 → 사용자(없으면 만든다 — 닉네임은 비워 둔다, 가입은 /api/me/name 에서 끝난다). 번호는 ID_PEPPER 로 HMAC 해서만 둔다. 공급자 이름은 가져오지 않는다. */
export async function userFor(env, provider, sub) {
  if (!env.ID_PEPPER) throw new Error("ID_PEPPER 시크릿이 없다");
  const key = await hmac(env.ID_PEPPER, provider + "|" + sub);
  const found = await env.DB.prepare("SELECT id FROM users WHERE provider = ?1 AND provider_sub = ?2").bind(provider, key).first();
  if (found) return found.id;
  const row = await env.DB.prepare("INSERT INTO users (provider, provider_sub, name, created) VALUES (?1, ?2, '', ?3) ON CONFLICT (provider, provider_sub) DO UPDATE SET provider = provider RETURNING id")
    .bind(provider, key, now()).first();
  return row.id;
}

/** 돌아갈 곳 — 같은 사이트 안의 경로만(「/」 로 시작, 「//」 · 「/\」 아님). */
export function safeBack(back) {
  return typeof back === "string" && back.length <= 300 && back.startsWith("/") && !back.startsWith("//") && !back.startsWith("/\\") ? back : "/";
}

/** 로그인 끝 — 웹 세션 쿠키를 주고 돌아갈 곳으로. */
export async function finishWebLogin(env, userId, back, extraCookies = []) {
  const { token, maxAge } = await newSession(env, userId, "web");
  const h = new Headers({ location: safeBack(back), "cache-control": "no-store" });
  h.append("set-cookie", cookie(COOKIE, token, maxAge));
  for (const c of extraCookies) h.append("set-cookie", c);
  return new Response(null, { status: 302, headers: h });
}
