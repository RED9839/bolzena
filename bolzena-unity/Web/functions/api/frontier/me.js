// GET /api/frontier/me[?season=id] — 내 프론티어: 오늘 쓴 시도 · 최고 · 순위 · 최근 낸 점수 20개(검증 대기 · 검증 안 함 · 거절 까닭 포함).
// → { season, today: { used, max: null(하루 한도 없음 — 2026-10-19) }, best: { score, heroes, at, verified } | null, rank | null, scores: [{ id, claimed, score, status, why, turn, heroes, at, verified }] }
//   status: pending(검증 대기) · verified · rejected · skipped(검증 안 함 — 낼 때 내 최고보다 높지 않았다, lib/score.js).
//   best · rank 는 랭킹(ranking.js)과 같은 규칙 — 검증된 점수와 검증 전(pending — 주장 점수) 가운데 가장 높은 줄. best.verified false 면 화면 「검증 전」.
//   rank = 그 줄의 순위(랭킹과 같은 범위 · 내가 정지 · 숨김 · 시험 계정이면 null). 거절(rejected)은 best · rank 에 들지 않는다.
//   시험 점수(test = 1 — migrations/0009)는 best · rank 에 들지 않고 scores 목록에만 test: true 로(화면 「시험」).
import { requireUser, VISIBLE } from "../../../lib/auth.js";
import { yyyymmdd, json, fail } from "../../../lib/util.js";
import { seasonOf, publicSeason, SEASON_ID } from "../../../lib/season.js";

export async function onRequestGet({ request, env }) {
  const { me, res } = await requireUser(request, env); if (res) return res;
  const id = new URL(request.url).searchParams.get("season");
  if (id !== null && !SEASON_ID.test(id)) return fail(400, "bad season");
  const season = await seasonOf(env, id);
  if (!season) return fail(404, "no season");
  const uid = me.user.id;
  const used = await env.DB.prepare("SELECT COUNT(*) AS c FROM attempts WHERE user_id = ?1 AND day = ?2").bind(uid, yyyymmdd()).first();
  const best = await env.DB.prepare(`SELECT COALESCE(score, claimed) AS score, status = 'verified' AS verified, heroes, submitted FROM scores
    WHERE user_id = ?1 AND season_id = ?2 AND status IN ('verified', 'pending') AND test = 0 ORDER BY COALESCE(score, claimed) DESC, status = 'verified' DESC, submitted ASC, id ASC LIMIT 1`).bind(uid, season.id).first();
  let rank = null;
  const shown = best && (await env.DB.prepare(`SELECT 1 AS ok FROM users u WHERE u.id = ?2 AND u.nick IS NOT NULL AND ${VISIBLE("u", "?1")}`).bind(season.id, uid).first());
  if (shown) {
    const above = await env.DB.prepare(`SELECT COUNT(*) AS c FROM (SELECT s.user_id, MAX(COALESCE(s.score, s.claimed)) AS m FROM scores s JOIN users u ON u.id = s.user_id
      WHERE s.season_id = ?1 AND s.status IN ('verified', 'pending') AND s.test = 0 AND s.user_id != ?2 AND u.nick IS NOT NULL AND ${VISIBLE("u", "?1")} GROUP BY s.user_id) WHERE m > ?3`)
      .bind(season.id, uid, best.score).first();
    rank = above.c + 1;
  }
  const { results } = await env.DB.prepare("SELECT id, claimed, score, status, why, turn, heroes, submitted, verified, test FROM scores WHERE user_id = ?1 AND season_id = ?2 ORDER BY submitted DESC, id DESC LIMIT 20").bind(uid, season.id).all();
  return json({
    season: publicSeason(season), today: { used: used.c, max: null },
    best: best ? { score: best.score, heroes: JSON.parse(best.heroes), at: best.submitted, verified: !!best.verified } : null, rank,
    scores: results.map((r) => ({ id: r.id, claimed: r.claimed, score: r.score, status: r.status, why: r.why, turn: r.turn, heroes: JSON.parse(r.heroes), at: r.submitted, verified: r.verified, test: !!r.test })),
  });
}

export const onRequest = () => fail(405, "method not allowed");
