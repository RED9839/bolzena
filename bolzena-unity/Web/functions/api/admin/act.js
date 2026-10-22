// POST /api/admin/act { action, reason, dry?, …대상 } — 운영자 전용(Authorization: Bearer <ADMIN_TOKEN>). 다른 사용자 데이터를 바꾸는 조치.
//   reason(2~300자)은 늘 필요(없으면 400). dry: true 면 바꾸지 않고 무엇을 할지(plan)만 돌려준다(운영 기록에도 남기지 않는다 — bz admin 의 기본).
//   조치와 운영 기록 한 줄(누가 · 언제 · 무엇을 · 대상 · 까닭 · 바꾸기 전 값)은 한 트랜잭션(D1 batch)으로 쓴다.
//
//   action           대상 · 인자                         하는 일
//   rename           user · name                         닉네임 강제 변경(규칙 · 겹침 검사는 그대로 — 하루 한 번 제한은 운영자에겐 없음. 바꾼 때를 지금으로 적어 본인은 24시간 뒤에 다시 바꿀 수 있다)
//   clearname        user                                닉네임 비우기 → 본인이 다시 정해야 한다(needsName). 그동안 랭킹에서 빠진다
//   score.delete     attempt                             그 시도의 점수 줄 지우기(시도 줄은 남는다 — 내 기록에 「안 냄」 처럼) · 그 사람의 다음 후보(skipped)를 올린다(lib/score.js)
//   score.reject     attempt                             그 점수를 rejected 로(why = 「운영자 거절 — 까닭」 — 본인 기록에 보인다) · 다음 후보를 올린다
//   hide · unhide    user · season?(없으면 모든 시즌 「*」) 랭킹 · 남의 편성 보기에서 빼기 · 되돌리기(unhide 에 season 이 없으면 그 사용자의 숨김 전부)
//   slot.delete      slot(칸 id)                         칸 하나 지우기
//   slots.purge      user · confirm: true                그 사용자의 칸 전부 지우기(confirm 이 없으면 400 — dry 는 confirm 없이도 몇 칸인지 보여 준다)
//   ban              user · days(1~3650) | forever: true 정지 — 쓰기(슬롯 저장 · 프론티어 시작 · 제출 · 닉네임 변경 …)가 403 + 까닭 · 풀리는 때, 랭킹 · 남의 편성 보기에서 빠진다
//   unban            user                                정지 풀기
//   role             user · role: "tester" | null         시험 계정 부여 · 회수(migrations/0009) — tester 는 「덱 만들기」 · 시험 칸 주소가 열리고, 그 사람의 도전 · 점수는 랭킹 · 남의 편성 보기 · 내 순위에서 빠진다
// → 200 { ok: true, action, dry, plan, user(조치 뒤 사용자 요약 — dry 면 지금), log(운영 기록 id — dry 면 null) } · 400 · 404(대상 없음) · 409(이미 그 상태 · 닉네임 겹침)
import { requireAdmin, checkReason, logStmt, userSummary } from "../../../lib/admin.js";
import { BAN_FOREVER, banOf } from "../../../lib/auth.js";
import { checkNick } from "../../../lib/nick.js";
import { SEASON_ID, seasonOf } from "../../../lib/season.js";
import { promoteNext } from "../../../lib/score.js";
import { LIMIT, now, isInt, readJson, limited, json, fail } from "../../../lib/util.js";

const DAY = 24 * 60 * 60;
const userId = (v) => (isInt(v, 1, Number.MAX_SAFE_INTEGER) ? v : null);
const ATTEMPT = /^[0-9a-f]{32}$/;

async function loadUser(env, v) {
  const id = userId(v && v.user);
  if (!id) return { res: fail(400, "user(사용자 번호) 가 필요하다") };
  const u = await env.DB.prepare("SELECT id, nick, banned_until, ban_reason, role FROM users WHERE id = ?1").bind(id).first();
  return u ? { u } : { res: fail(404, "no user", { user: id }) };
}

async function loadScore(env, v) {
  if (typeof v.attempt !== "string" || !ATTEMPT.test(v.attempt)) return { res: fail(400, "attempt(시도 id 32자리) 가 필요하다") };
  const s = await env.DB.prepare("SELECT id, user_id, season_id, heroes, status, claimed, score, why FROM scores WHERE attempt_id = ?1").bind(v.attempt).first();
  return s ? { s } : { res: fail(404, "no score", { attempt: v.attempt }) };
}

/** 조치마다 — { userId, target, plan, stmts, detail } 또는 { res }. stmts 는 바꾸는 문장(운영 기록 문장은 디스패처가 붙인다). */
const ACTIONS = {
  async rename(env, v) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    const c = checkNick(v.name);
    if (c.error) return { res: fail(400, "bad name", { why: c.error }) };
    if (u.nick === c.show) return { res: fail(409, "이미 그 닉네임", { name: c.show }) };
    const other = await env.DB.prepare("SELECT id FROM users WHERE nick_key = ?1 AND id != ?2").bind(c.key, u.id).first();
    if (other) return { res: fail(409, "이미 누가 쓰는 닉네임", { user: other.id }) };
    return {
      userId: u.id, target: "user " + u.id, plan: { from: u.nick ?? null, to: c.show }, detail: { from: u.nick ?? null, to: c.show },
      stmts: [env.DB.prepare("UPDATE users SET nick = ?1, nick_key = ?2, nick_changed = ?3 WHERE id = ?4").bind(c.show, c.key, now(), u.id)],
    };
  },
  async clearname(env, v) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    if (!u.nick) return { res: fail(409, "이미 닉네임이 없다") };
    return {
      userId: u.id, target: "user " + u.id, plan: { from: u.nick, to: null, needsName: true }, detail: { from: u.nick },
      stmts: [env.DB.prepare("UPDATE users SET nick = NULL, nick_key = NULL, nick_changed = NULL WHERE id = ?1").bind(u.id)],
    };
  },
  async "score.delete"(env, v) {
    const { s, res } = await loadScore(env, v); if (res) return { res };
    const was = { season: s.season_id, heroes: JSON.parse(s.heroes), status: s.status, claimed: s.claimed, score: s.score };
    return {
      userId: s.user_id, season: s.season_id, target: "attempt " + v.attempt, plan: { delete: was }, detail: was,
      stmts: [env.DB.prepare("DELETE FROM scores WHERE id = ?1").bind(s.id)],
    };
  },
  async "score.reject"(env, v, reason) {
    const { s, res } = await loadScore(env, v); if (res) return { res };
    const why = ("운영자 거절 — " + reason).slice(0, 500);
    const was = { season: s.season_id, heroes: JSON.parse(s.heroes), status: s.status, claimed: s.claimed, score: s.score, why: s.why };
    return {
      userId: s.user_id, season: s.season_id, target: "attempt " + v.attempt, plan: { from: s.status, to: "rejected", why }, detail: was,
      stmts: [env.DB.prepare("UPDATE scores SET status = 'rejected', score = NULL, why = ?1, turn = NULL, verified = ?2 WHERE id = ?3").bind(why, now(), s.id)],
    };
  },
  async hide(env, v, reason) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    let season = "*";
    if (v.season !== undefined && v.season !== null) {
      if (typeof v.season !== "string" || !SEASON_ID.test(v.season)) return { res: fail(400, "bad season") };
      if (!(await seasonOf(env, v.season))) return { res: fail(404, "no season", { season: v.season }) };
      season = v.season;
    }
    const had = await env.DB.prepare("SELECT reason FROM rank_hidden WHERE user_id = ?1 AND season_id = ?2").bind(u.id, season).first();
    return {
      userId: u.id, target: "user " + u.id + " · season " + season, plan: { season, already: !!had }, detail: { season, before: had ? had.reason : null },
      stmts: [env.DB.prepare("INSERT INTO rank_hidden (user_id, season_id, reason, at) VALUES (?1, ?2, ?3, ?4) ON CONFLICT (user_id, season_id) DO UPDATE SET reason = excluded.reason, at = excluded.at")
        .bind(u.id, season, reason, now())],
    };
  },
  async unhide(env, v) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    if (v.season !== undefined && v.season !== null && (typeof v.season !== "string" || (v.season !== "*" && !SEASON_ID.test(v.season)))) return { res: fail(400, "bad season") };
    const one = v.season !== undefined && v.season !== null;
    const { results } = await (one
      ? env.DB.prepare("SELECT season_id FROM rank_hidden WHERE user_id = ?1 AND season_id = ?2").bind(u.id, v.season)
      : env.DB.prepare("SELECT season_id FROM rank_hidden WHERE user_id = ?1").bind(u.id)).all();
    if (results.length === 0) return { res: fail(409, "숨김이 없다", one ? { season: v.season } : {}) };
    const seasons = results.map((r) => r.season_id);
    return {
      userId: u.id, target: "user " + u.id + " · season " + seasons.join(","), plan: { seasons }, detail: { seasons },
      stmts: [one ? env.DB.prepare("DELETE FROM rank_hidden WHERE user_id = ?1 AND season_id = ?2").bind(u.id, v.season)
        : env.DB.prepare("DELETE FROM rank_hidden WHERE user_id = ?1").bind(u.id)],
    };
  },
  async "slot.delete"(env, v) {
    if (!isInt(v.slot, 1, Number.MAX_SAFE_INTEGER)) return { res: fail(400, "slot(칸 id) 가 필요하다") };
    const r = await env.DB.prepare("SELECT id, user_id, hero, n, name, hash, status FROM hero_slots WHERE id = ?1").bind(v.slot).first();
    if (!r) return { res: fail(404, "no slot", { slot: v.slot }) };
    const was = { hero: r.hero, n: r.n, name: r.name ?? null, hash: r.hash, status: r.status };
    return {
      userId: r.user_id, target: "slot " + r.id, plan: { delete: was }, detail: was,
      stmts: [env.DB.prepare("DELETE FROM hero_slots WHERE id = ?1").bind(r.id)],
    };
  },
  async "slots.purge"(env, v, reason, dry) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    if (!dry && v.confirm !== true) return { res: fail(400, "confirm required", { why: "칸 전부 지우기는 confirm: true 가 있어야 한다" }) };
    const c = await env.DB.prepare("SELECT COUNT(*) AS c FROM hero_slots WHERE user_id = ?1").bind(u.id).first();
    if (c.c === 0) return { res: fail(409, "지울 칸이 없다") };
    return {
      userId: u.id, target: "user " + u.id, plan: { delete: c.c, confirm: v.confirm === true }, detail: { deleted: c.c },
      stmts: [env.DB.prepare("DELETE FROM hero_slots WHERE user_id = ?1").bind(u.id)],
    };
  },
  async ban(env, v, reason) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    const byDays = v.days !== undefined && v.days !== null, forever = v.forever === true;
    if (byDays === forever) return { res: fail(400, "days(1~3650) 나 forever: true 가운데 하나") };
    if (byDays && !isInt(v.days, 1, 3650)) return { res: fail(400, "days 는 1~3650") };
    const until = forever ? BAN_FOREVER : now() + v.days * DAY;
    const prev = banOf(u);
    const after = { until: forever ? null : until, forever, why: reason };
    return {
      userId: u.id, target: "user " + u.id, plan: { ban: after, before: prev }, detail: { ...after, before: prev },
      stmts: [env.DB.prepare("UPDATE users SET banned_until = ?1, ban_reason = ?2 WHERE id = ?3").bind(until, reason, u.id)],
    };
  },
  async role(env, v) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    if (v.role !== null && v.role !== "tester") return { res: fail(400, "role 은 \"tester\" 또는 null(회수)") };
    const from = u.role ?? null, to = v.role;
    if (from === to) return { res: fail(409, to ? "이미 그 역할" : "역할이 없다", { role: from }) };
    return {
      userId: u.id, target: "user " + u.id, plan: { from, to }, detail: { from, to },
      stmts: [env.DB.prepare("UPDATE users SET role = ?1 WHERE id = ?2").bind(to, u.id)],
    };
  },
  async unban(env, v) {
    const { u, res } = await loadUser(env, v); if (res) return { res };
    const prev = banOf(u);
    if (!prev) return { res: fail(409, "정지 중이 아니다") };
    return {
      userId: u.id, target: "user " + u.id, plan: { unban: prev }, detail: { before: prev },
      stmts: [env.DB.prepare("UPDATE users SET banned_until = NULL, ban_reason = NULL WHERE id = ?1").bind(u.id)],
    };
  },
};

export async function onRequestPost({ request, env }) {
  const { actor, res } = await requireAdmin(request, env); if (res) return res;
  const { value: v, res: bad } = await readJson(request, LIMIT.SMALL_BYTES); if (bad) return bad;
  if (!v || typeof v !== "object" || Array.isArray(v)) return fail(400, "bad body");
  const fn = typeof v.action === "string" && Object.hasOwn(ACTIONS, v.action) ? ACTIONS[v.action] : null;
  if (!fn) return fail(400, "bad action", { actions: Object.keys(ACTIONS) });
  const r0 = checkReason(v.reason); if (r0.res) return r0.res;
  const dry = v.dry === true;
  if (!dry) { const lim = await limited(env, "admin|" + actor, 60, 60); if (lim) return lim; }
  const p = await fn(env, v, r0.reason, dry);
  if (p.res) return p.res;
  if (dry) return json({ ok: true, action: v.action, dry: true, plan: p.plan, user: p.userId ? await userSummary(env, p.userId) : null, log: null });
  let out;
  try {
    out = await env.DB.batch([...p.stmts, logStmt(env, { actor, action: v.action, userId: p.userId, target: p.target, reason: r0.reason, detail: p.detail })]);
  } catch (e) {
    if (/UNIQUE/i.test(String(e && e.message))) return fail(409, "이미 누가 쓰는 닉네임");   // 검사와 쓰기 사이에 누가 같은 열쇠로 정함 — 배치는 통째로 되돌려진다
    throw e;
  }
  const promoted = p.season ? await promoteNext(env, p.userId, p.season) : null;   // 점수 조치 뒤 — 그 사람의 다음 검증 후보
  return json({ ok: true, action: v.action, dry: false, plan: p.plan, ...(p.season ? { promoted } : {}), user: p.userId ? await userSummary(env, p.userId) : null, log: out[out.length - 1].meta?.last_row_id ?? null });
}

export const onRequest = () => fail(405, "method not allowed");
