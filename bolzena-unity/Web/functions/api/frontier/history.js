// GET /api/frontier/history[?season=id][&page=0] — 내 도전 기록(시도마다 한 줄 · 최근 순 · 한 쪽 20줄). 로그인 필요.
// → { rows: [{ attempt, season, heroes, at(시작), status, claimed, score, why, turn, over, played, sent, verified }], page, size, more }
//   status: open(진행 중 — 아직 안 냄 · 2시간 안) · expired(내지 않고 지남) · pending(검증 대기) · verified · rejected(why · turn).
//   test = 시험 시도(migrations/0009 — 시험 계정이 연 것, 화면 「시험」 · 랭킹에 안 오름).
//   over · played = 낸 테이프의 끝(win · lose · time) · 낸 턴(내지 않았으면 null). 씨앗 · 테이프 글은 주지 않는다(편성은 /api/frontier/attempt/:id).
import { requireUser } from "../../../lib/auth.js";
import { now, json, fail } from "../../../lib/util.js";
import { SEASON_ID } from "../../../lib/season.js";

const SIZE = 20;

export async function onRequestGet({ request, env }) {
  const { me, res } = await requireUser(request, env); if (res) return res;
  const q = new URL(request.url).searchParams;
  const season = q.get("season");
  if (season !== null && !SEASON_ID.test(season)) return fail(400, "bad season");
  const page = q.get("page") === null ? 0 : Number(q.get("page"));
  if (!Number.isSafeInteger(page) || page < 0 || page > 499) return fail(400, "bad page");
  const { results } = await env.DB.prepare(`
    SELECT a.id, a.season_id, a.heroes, a.started, a.expires, a.submitted AS done, a.test, s.status, s.claimed, s.score, s.why, s.turn, s.submitted AS sent, s.verified,
           json_extract(s.tape, '$.over') AS over, json_extract(s.tape, '$.played') AS played
    FROM attempts a LEFT JOIN scores s ON s.attempt_id = a.id
    WHERE a.user_id = ?1 AND (?2 IS NULL OR a.season_id = ?2)
    ORDER BY a.started DESC, a.id DESC LIMIT ?3 OFFSET ?4`).bind(me.user.id, season, SIZE + 1, page * SIZE).all();
  const t = now();
  return json({
    rows: results.slice(0, SIZE).map((r) => ({
      attempt: r.id, season: r.season_id, heroes: JSON.parse(r.heroes), at: r.started,
      status: r.status || (r.done || r.expires < t ? "expired" : "open"),   // 냈는데 점수 줄이 없으면 운영자가 지운 것(admin score.delete) — 「내지 않음」 으로
      claimed: r.claimed ?? null, score: r.score ?? null, why: r.why ?? null, turn: r.turn ?? null,
      over: r.over ?? null, played: r.played ?? null, sent: r.sent ?? null, verified: r.verified ?? null, test: !!r.test,
    })),
    page, size: SIZE, more: results.length > SIZE,
  });
}

export const onRequest = () => fail(405, "method not allowed");
