// 운영 도구 공용 — 운영자 인증(ADMIN_TOKEN 시크릿) · 까닭 검사 · 운영 기록 · 사용자 요약 · 대상 찾기.
// 운영 주소(functions/api/admin/…)와 이 PC 명령 bz admin(bolzena-core/Tools~/Dev/Admin.cs)이 쓴다. 설명: ../../Docs/프론티어_서버.md §운영
// 개인정보: OAuth 원 번호(provider_sub — 이미 HMAC) · 세션 토큰 해시 · IP 는 운영 주소도 내보내지 않는다.
import { now, sameSecret, fail, crossOrigin, memLimited, ipKey } from "./util.js";
import { banOf } from "./auth.js";
import { nickShow, nickKey } from "./nick.js";

export const REASON_MAX = 300;

/**
 * 운영자를 요구한다 — { actor }(운영 기록에 남길 이름 — ADMIN_NAME 시크릿, 없으면 「admin」) 또는 { res }.
 * ADMIN_TOKEN 이 없거나 32자보다 짧으면 운영 주소 전부 503(닫힘). 검증기 토큰(VERIFY_TOKEN)과는 따로 — 같은 값이면 503(설정 실수).
 * 다른 사이트에서 온 요청(Origin 호스트가 다름) 403. IP 해시마다 분당 120번(토큰을 보기 전에 — 맞히기 막기. 쓰기 조치는 따로 운영자마다 분당 60번 — act.js). 토큰 비교는 해시를 견주는 상수 시간.
 */
export async function requireAdmin(request, env) {
  if (!env.DB) return { res: fail(503, "no db") };
  if (!env.ADMIN_TOKEN || env.ADMIN_TOKEN.length < 32) return { res: fail(503, "admin not configured") };
  if (env.VERIFY_TOKEN && (await sameSecret(env.ADMIN_TOKEN, env.VERIFY_TOKEN))) return { res: fail(503, "admin token must differ from verify token") };
  if (crossOrigin(request)) return { res: fail(403, "cross origin") };
  const lim = memLimited("admin|" + (await ipKey(request)), 120); if (lim) return { res: lim };
  const a = request.headers.get("authorization") || "";
  if (!a.startsWith("Bearer ") || !(await sameSecret(a.slice(7).trim(), env.ADMIN_TOKEN))) return { res: fail(401, "admin token required") };
  const name = typeof env.ADMIN_NAME === "string" && /^[A-Za-z0-9_.-]{1,32}$/.test(env.ADMIN_NAME) ? env.ADMIN_NAME : "admin";
  return { actor: name };
}

/** 까닭 — { reason }(앞뒤 공백 정리 · 2~300자) 또는 { res }(400). 쓰기 조치는 반드시 까닭이 있어야 한다. */
export function checkReason(raw) {
  if (typeof raw !== "string") return { res: fail(400, "reason required", { why: "쓰기 조치는 까닭(reason)이 있어야 한다" }) };
  const reason = raw.normalize("NFC").replace(/[\u0000-\u001f\u007f]/g, " ").trim();
  if ([...reason].length < 2) return { res: fail(400, "reason required", { why: "까닭은 2자 이상" }) };
  if ([...reason].length > REASON_MAX) return { res: fail(400, "reason too long", { why: `까닭은 ${REASON_MAX}자까지` }) };
  return { reason };
}

/** 운영 기록 한 줄(배치에 같이 넣는 문장) — 조치와 같은 트랜잭션으로 남긴다. */
export const logStmt = (env, { actor, action, userId, target, reason, detail }) =>
  env.DB.prepare("INSERT INTO admin_log (at, actor, action, user_id, target, reason, detail) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7)")
    .bind(now(), actor, action, userId ?? null, target ?? null, reason, detail === undefined ? null : JSON.stringify(detail));

/** 운영 기록 줄 → 밖에 보일 모양. */
export const logOut = (r) => ({ id: r.id, at: r.at, actor: r.actor, action: r.action, user: r.user_id, name: r.name ?? null, target: r.target, reason: r.reason, detail: r.detail ? JSON.parse(r.detail) : null });

/** 사용자 요약 — 닉네임 · 역할(tester · null) · 가입 · 마지막 활동 · 세션 · 슬롯 · 시험 칸 · 교주 보드 진행 · 시도 · 점수(시험 점수 수 포함) · 시즌별 최고 · 정지 · 숨김 · 최근 시도 5(status: 점수 상태 · unsent 안 냄 · deleted 점수 지움 · test 시험 시도) · 최근 운영 기록 5. 없으면 null. */
export async function userSummary(env, id) {
  const db = env.DB;
  const u = await db.prepare("SELECT id, provider, nick, nick_changed, created, banned_until, ban_reason, role FROM users WHERE id = ?1").bind(id).first();
  if (!u) return null;
  const [slots, attempts, scores, sessions, hidden, best, recent, log, board, tslots] = (await db.batch([
    db.prepare("SELECT COUNT(*) AS c, SUM(status = 'ok') AS ok, SUM(status = 'pending') AS pending, SUM(status = 'bad') AS bad, MAX(updated) AS last FROM hero_slots WHERE user_id = ?1").bind(id),
    db.prepare("SELECT COUNT(*) AS c, MAX(started) AS last FROM attempts WHERE user_id = ?1").bind(id),
    db.prepare("SELECT COUNT(*) AS c, SUM(status = 'verified') AS verified, SUM(status = 'pending') AS pending, SUM(status = 'rejected') AS rejected, SUM(status = 'skipped') AS skipped, SUM(test = 1) AS test, MAX(submitted) AS last FROM scores WHERE user_id = ?1").bind(id),
    db.prepare("SELECT COUNT(*) AS c, SUM(kind = 'web') AS web, SUM(kind = 'pc') AS pc, MAX(created) AS last FROM sessions WHERE user_id = ?1 AND expires > ?2").bind(id, now()),
    db.prepare("SELECT season_id, reason, at FROM rank_hidden WHERE user_id = ?1 ORDER BY season_id").bind(id),
    db.prepare("SELECT season_id, MAX(score) AS best, COUNT(*) AS n FROM scores WHERE user_id = ?1 AND status = 'verified' AND test = 0 GROUP BY season_id ORDER BY season_id").bind(id),
    db.prepare(`SELECT a.id, a.season_id, a.heroes, a.started, a.submitted, a.test, s.status, s.claimed, s.score FROM attempts a LEFT JOIN scores s ON s.attempt_id = a.id
      WHERE a.user_id = ?1 ORDER BY a.started DESC, a.id DESC LIMIT 5`).bind(id),
    db.prepare("SELECT id, at, actor, action, user_id, target, reason, detail FROM admin_log WHERE user_id = ?1 ORDER BY id DESC LIMIT 5").bind(id),
    db.prepare("SELECT LENGTH(json) AS bytes, rev, updated FROM board_progress WHERE user_id = ?1").bind(id),
    db.prepare("SELECT COUNT(*) AS c, MAX(updated) AS last FROM test_slots WHERE user_id = ?1").bind(id),
  ])).map((r) => r.results);
  const s0 = slots[0], a0 = attempts[0], c0 = scores[0], z0 = sessions[0];
  const last = Math.max(u.created || 0, u.nick_changed || 0, s0.last || 0, a0.last || 0, c0.last || 0, z0.last || 0, tslots[0].last || 0);
  return {
    id: u.id, name: u.nick ?? null, role: u.role ?? null, provider: u.provider, created: u.created, nameChanged: u.nick_changed ?? null, lastActive: last || null,
    ban: banOf(u),
    hidden: hidden.map((h) => ({ season: h.season_id, reason: h.reason, at: h.at })),
    sessions: { live: z0.c, web: z0.web || 0, pc: z0.pc || 0 },
    slots: { count: s0.c, ok: s0.ok || 0, pending: s0.pending || 0, bad: s0.bad || 0 },
    testSlots: tslots[0].c,   // 시험 칸(0009) 수
    attempts: a0.c,
    board: board[0] ? { bytes: board[0].bytes, rev: board[0].rev, updated: board[0].updated } : null,   // 교주 보드 진행(0007) — 있으면 크기 · 판 번호 · 바꾼 때
    scores: { count: c0.c, verified: c0.verified || 0, pending: c0.pending || 0, rejected: c0.rejected || 0, skipped: c0.skipped || 0, test: c0.test || 0 },
    best: best.map((b) => ({ season: b.season_id, score: b.best, verified: b.n })),
    recent: recent.map((r) => ({ attempt: r.id, season: r.season_id, heroes: JSON.parse(r.heroes), at: r.started, status: r.status || (r.submitted ? "deleted" : "unsent"), claimed: r.claimed ?? null, score: r.score ?? null, test: !!r.test })),
    log: log.map(logOut),
  };
}

const LIKE_ESC = (s) => s.replace(/[\\%_]/g, (c) => "\\" + c);

/**
 * 대상 찾기 — { by, ids } 또는 { res }(400). q 는 하나만: id(사용자 번호) · nick(닉네임 — 겹침 열쇠로 같은 것 먼저, 그다음 부분 일치 10개까지) ·
 * attempt(시도 id — 랭킹 줄의 attempt) · slot(칸 id).
 */
export async function findUsers(env, q) {
  const keys = ["id", "nick", "attempt", "slot"].filter((k) => q.get(k) !== null);
  if (keys.length !== 1) return { res: fail(400, "id · nick · attempt · slot 가운데 하나만") };
  const by = keys[0], v = q.get(by), db = env.DB;
  if (by === "id" || by === "slot") {
    if (!/^[1-9][0-9]{0,15}$/.test(v)) return { res: fail(400, "bad " + by) };
    const r = by === "id"
      ? await db.prepare("SELECT id FROM users WHERE id = ?1").bind(Number(v)).first()
      : await db.prepare("SELECT user_id AS id FROM hero_slots WHERE id = ?1").bind(Number(v)).first();
    return { by, ids: r ? [r.id] : [] };
  }
  if (by === "attempt") {
    if (!/^[0-9a-f]{32}$/.test(v)) return { res: fail(400, "bad attempt") };
    const r = await db.prepare("SELECT user_id AS id FROM attempts WHERE id = ?1").bind(v).first();
    return { by, ids: r ? [r.id] : [] };
  }
  if (v.length < 1 || v.length > 64) return { res: fail(400, "bad nick") };
  const key = nickKey(nickShow(v));
  if (!key) return { res: fail(400, "bad nick") };
  const exact = await db.prepare("SELECT id FROM users WHERE nick_key = ?1").bind(key).first();
  const { results } = await db.prepare("SELECT id FROM users WHERE nick_key LIKE ?1 ESCAPE '\\' AND nick_key != ?2 ORDER BY id LIMIT 10").bind("%" + LIKE_ESC(key) + "%", key).all();
  return { by, exact: !!exact, ids: [...(exact ? [exact.id] : []), ...results.map((r) => r.id)] };
}
