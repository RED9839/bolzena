// GET /api/frontier/ranking[?season=id][&limit=100] — 랭킹(검증된 점수 + 검증 전 점수 · 사람당 최고 하나 · 같은 점수는 먼저 낸 쪽이 위). 로그인 없이.
// → { season(preseason 이면 화면에 「프리시즌」), rows: [{ rank, name, score, heroes, at, attempt, verified }], pending(이 시즌 검증 대기 수 — 랭킹에 안 보이는 것 포함) }
//   규칙(2026-10-19 사용자 「중간 방법 1」 · Docs/프론티어_서버.md §랭킹):
//     낸 점수는 곧바로 오른다 — verified 는 검증기가 다시 돌린 점수, pending 은 주장 점수(제출 상한 검사를 지난 것)에 verified: false(화면 「검증 전」).
//     rejected 는 빠진다(그 사람의 다음 줄 — 검증됐거나 검증 전인 것 — 이 대신 오른다). 사람마다 가장 높은 줄 하나 — 검증 전 점수가 그 사람 최고면 그 줄이 오른다.
//     같은 사람의 같은 점수면 검증된 줄을 먼저 고른다. 순서는 점수 높은 차례 · 같으면 먼저 낸 쪽.
//   attempt = 그 줄의 시도 id — /api/frontier/attempt/:id 로 그 사람의 편성(카드 · 장비 · 성장)을 본다. 편성은 검증된 시도만 공개 — 검증 전 줄은 줄만(편성 slots 는 null).
// 정지 중인 사람 · 운영자가 그 시즌(또는 모든 시즌) 랭킹에서 숨긴 사람 · 시험 계정(role tester)은 빠진다(lib/auth.js VISIBLE — 점수 줄은 그대로라 풀면 다시 보인다).
// 시험 점수(scores.test = 1 — 시험 계정이 낸 것, migrations/0009)는 역할을 회수해도 빠진다.
// 사람을 알아볼 것은 내보내지 않는다 — 사용자가 정한 화면 이름 · 점수 · 편성 사도 · 낸 날만.
import { json, fail, memLimited } from "../../../lib/util.js";
import { seasonOf, publicSeason, SEASON_ID } from "../../../lib/season.js";
import { VISIBLE } from "../../../lib/auth.js";

export async function onRequestGet({ request, env }) {
  if (!env.DB) return fail(503, "no db");
  const lim = memLimited("rank|" + (request.headers.get("cf-connecting-ip") || "?"), 60); if (lim) return lim;
  const q = new URL(request.url).searchParams;
  const id = q.get("season");
  if (id !== null && !SEASON_ID.test(id)) return fail(400, "bad season");
  const limit = Math.min(200, Math.max(1, Number(q.get("limit")) || 100));
  const season = await seasonOf(env, id);
  if (!season) return fail(404, "no season");
  const { results } = await env.DB.prepare(`
    SELECT name, score, verified, heroes, submitted, attempt_id FROM (
      SELECT u.nick AS name, COALESCE(s.score, s.claimed) AS score, s.status = 'verified' AS verified, s.heroes, s.submitted, s.attempt_id,
             ROW_NUMBER() OVER (PARTITION BY s.user_id ORDER BY COALESCE(s.score, s.claimed) DESC, s.status = 'verified' DESC, s.submitted ASC, s.id ASC) AS k
      FROM scores s JOIN users u ON u.id = s.user_id
      WHERE s.season_id = ?1 AND s.status IN ('verified', 'pending') AND s.test = 0 AND u.nick IS NOT NULL AND ${VISIBLE("u", "?1")})
    WHERE k = 1 ORDER BY score DESC, submitted ASC LIMIT ?2`).bind(season.id, limit).all();
  const pending = await env.DB.prepare("SELECT COUNT(*) AS c FROM scores WHERE season_id = ?1 AND status = 'pending' AND test = 0").bind(season.id).first();
  return json({
    season: publicSeason(season),
    rows: results.map((r, i) => ({ rank: i + 1, name: r.name, score: r.score, heroes: JSON.parse(r.heroes), at: r.submitted, attempt: r.attempt_id, verified: !!r.verified })),
    pending: pending.c,
  }, 200, { "cache-control": "public, max-age=30" });
}

export const onRequest = () => fail(405, "method not allowed");
