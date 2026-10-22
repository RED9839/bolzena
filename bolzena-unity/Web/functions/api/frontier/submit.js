// POST /api/frontier/submit { attempt, score, tape(FrontierTape JSON 을 「글」 로 — 엔진 FrontierTape.Json() 그대로. 서버는 글 그대로 두어 검증기가 클라이언트와 같은 바이트를 읽는다) } — 끝난 프론티어를 낸다 → 검증 대기(pending).
//   여기서는 모양 · 크기 · 시도 주인 · 만료 · 시도 하나에 한 번 · 테이프의 시즌 · 보스 · 턴 · 씨앗 · 편성이 시도와 같은지 · 주장 점수 = 테이프 점수만 본다.
//   제출 상한(migrations/0008 · Docs/프론티어_서버.md §제출 상한) — 받자마자 400 으로 거절(점수 줄을 만들지 않고 시도도 「낸 것」 으로 치지 않는다):
//     점수 음수 · 소수 · 정수 아님 · 점수 > 시즌 score_max(보스 최대 HP) · 낸 턴 < 1 또는 > 시즌 턴 · 「time」 인데 낸 턴 ≠ 시즌 턴 ·
//     턴 지문 수가 낸 턴 · 시즌 턴과 맞지 않음(연 직후 1 + 넘긴 턴마다 1 — 낸 턴 또는 낸 턴 + 1, 시즌 턴 + 1 이하) · 점수 > 낸 턴 × 시즌 turn_max(한 턴 피해 상한) ·
//     테이프 256KB · 수 5,000 · 턴 지문 200 초과. 테이프에는 턴별 피해가 없다(턴 지문은 해시) — 한 턴 상한은 낸 턴 수로 곱해 본다.
//   다시 돌려 점수를 믿을지는 검증기(bz verify — Frontier.Verify)가 정한다. 낸 점수는 곧바로 랭킹에 「검증 전」 으로 오른다(ranking.js).
//   검증은 자기 최고 기록 후보만(lib/score.js) — 내 지금 최고(verified · pending)보다 높으면 pending, 아니면 skipped(「검증 안 함」 — 검증기가 가져가지 않는다).
//   시험 시도(attempts.test = 1 — 시험 계정이 연 것, migrations/0009)는 늘 skipped + test = 1 — 검증기가 가져가지 않고(검증 안 함) 랭킹 · 남의 편성 · 내 순위 · 다음 후보 올리기에서 빠진다.
//     모양 · 상한 검사는 같다(시험이라도 서버가 받는 글의 크기 · 형식은 같은 선).
//   → 202 { id, status: "pending" | "skipped", test } · 400 { error: "bad tape: …" | "bad score" | "over cap: …", why }
import { requireUser } from "../../../lib/auth.js";
import { LIMIT, ID, isInt, now, readJson, limited, json, fail } from "../../../lib/util.js";
import { seasonOf } from "../../../lib/season.js";
import { statusForNew } from "../../../lib/score.js";

const TOP = new Set(["kind", "v", "season", "boss", "turns", "seed", "heroes", "slots", "score", "played", "over", "input"]);
const OVERS = new Set(["win", "lose", "time"]);

function shape(t, a, season) {
  if (!t || typeof t !== "object" || Array.isArray(t)) return "tape not object";
  for (const k of Object.keys(t)) if (!TOP.has(k)) return "tape field " + k.slice(0, 20);
  if (t.kind !== "bolzena-frontier" || t.v !== 1) return "tape kind/version";
  if (t.season !== season.id || t.boss !== season.boss || t.turns !== season.turns) return "다른 시즌의 테이프";
  if (t.seed !== a.seed) return "서버가 준 씨앗이 아니다";
  const heroes = JSON.parse(a.heroes);
  if (!Array.isArray(t.heroes) || t.heroes.length !== heroes.length || t.heroes.some((h, i) => h !== heroes[i] || !ID.test(h))) return "편성이 시도와 다르다";
  if (!Array.isArray(t.slots) || t.slots.length !== heroes.length || !t.slots.every((s) => typeof s === "string" && /^[0-9a-f]{16}$/.test(s))) return "슬롯 지문";
  if (!isInt(t.score, 0, 1e9) || !isInt(t.played, 0, 999) || !OVERS.has(t.over)) return "score/played/over";
  const i = t.input;
  if (!i || typeof i !== "object" || !Array.isArray(i.ops) || !Array.isArray(i.digests) || !i.setup || typeof i.setup !== "object") return "input";
  if (i.ops.length > 5000 || i.digests.length > 200) return "input 너무 김";
  return null;
}

/** 제출 상한 — 넘으면 까닭(사람이 읽을 글), 아니면 null. 모양 검사(shape)를 지난 테이프만. */
function overCap(t, season) {
  const n = t.input.digests.length;
  if (t.played < 1 || t.played > season.turns) return `낸 턴 ${t.played} — 시즌은 1~${season.turns}턴`;
  if (t.over === "time" && t.played !== season.turns) return `시간 끝인데 낸 턴이 ${t.played}(시즌 ${season.turns}턴)`;
  if (n > season.turns + 1 || n < t.played || n > t.played + 1) return `턴 지문 ${n}개 — 낸 턴 ${t.played} · 시즌 ${season.turns}턴과 맞지 않는다`;
  if (isInt(season.score_max, 1, 1e9) && t.score > season.score_max) return `점수 ${t.score} > 보스 최대 HP ${season.score_max}`;
  if (isInt(season.turn_max, 1, 1e9) && t.score > t.played * season.turn_max) return `점수 ${t.score} > 낸 턴 ${t.played} × 한 턴 상한 ${season.turn_max}`;
  return null;
}

export async function onRequestPost({ request, env }) {
  const { me, res } = await requireUser(request, env, true, true); if (res) return res;
  const lim = await limited(env, "submit|" + me.user.id, 10, 60); if (lim) return lim;
  const { value, res: bad } = await readJson(request, LIMIT.TAPE_BYTES); if (bad) return bad.status === 413 ? fail(413, "too big", { max: LIMIT.TAPE_BYTES }) : bad;
  const { attempt, score } = value || {};
  if (typeof (value && value.tape) !== "string") return fail(400, "tape 는 FrontierTape JSON 글");
  let tape; try { tape = JSON.parse(value.tape); } catch { return fail(400, "bad tape json"); }
  if (typeof attempt !== "string" || !/^[0-9a-f]{32}$/.test(attempt)) return fail(400, "bad attempt");
  if (!isInt(score, 0, 1e9)) return fail(400, "bad score");
  const a = await env.DB.prepare("SELECT id, user_id, season_id, seed, heroes, expires, submitted, test FROM attempts WHERE id = ?1").bind(attempt).first();
  if (!a || a.user_id !== me.user.id) return fail(404, "no attempt");
  if (a.submitted) return fail(409, "이미 낸 시도");
  if (a.expires < now()) return fail(410, "시도 시간이 지났다");
  const season = await seasonOf(env, a.season_id);
  if (!season) return fail(409, "no season");
  const why = shape(tape, a, season);
  if (why) return fail(400, "bad tape: " + why);
  if (tape.score !== score) return fail(400, "bad tape: 주장 점수가 테이프 점수와 다르다");
  const cap = overCap(tape, season);
  if (cap) return fail(400, "over cap: " + cap, { why: cap });
  // 시도 하나에 한 번 — submitted 를 먼저 바꾼 쪽만 넣는다(동시에 두 번 내도 하나만)
  const claim = await env.DB.prepare("UPDATE attempts SET submitted = 1 WHERE id = ?1 AND submitted = 0 RETURNING id").bind(a.id).first();
  if (!claim) return fail(409, "이미 낸 시도");
  const test = a.test ? 1 : 0;
  const status = test ? "skipped" : await statusForNew(env, me.user.id, a.season_id, score);
  const row = await env.DB.prepare("INSERT INTO scores (attempt_id, user_id, season_id, heroes, claimed, tape, status, submitted, test) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9) RETURNING id")
    .bind(a.id, me.user.id, a.season_id, a.heroes, score, value.tape, status, now(), test).first();
  return json({ id: row.id, status, test: !!test }, 202);
}

export const onRequest = () => fail(405, "method not allowed");
