// GET /api/frontier/attempt/:id — 시도 하나의 편성(그때 떠 둔 사도 슬롯 — 카드 · 장비 · 성장)과 결과.
//   내 시도(로그인한 주인)면 상태와 관계없이 전부 — 검증 대기 · 거절 까닭 · 진행 중 · 지난 시도까지.
//   남의 시도면 **랭킹에 오르는 것만** — 검증된(verified) 시도는 편성까지, 검증 전(pending) 시도는 줄만(slots: null · 점수는 주장 점수 · 2026-10-19 「검증된 것만 편성 공개」).
//   그 밖(거절 · 안 냄 · 진행 중)은 404. 정지 중 · 랭킹에서 숨긴 사람 · 시험 계정의 시도와 시험 시도(test = 1 — migrations/0009)도 남에게는 404. 탈퇴하면 시도 줄이 지워져 404.
//   내 것에는 test(시험 시도인가)를 더한다.
//   언제나 빼는 것: 서버 씨앗 · 테이프 글 · 사용자 내부 번호 · 슬롯 번호. 남의 것은 슬롯의 from.seed(뜬 판의 씨앗)도 뺀다.
// → { attempt, mine, name(닉네임), season, heroes, at, status, score, over, played, slots: [{ hero, slot(HeroSlot JSON) }] }
//   내 것은 claimed · why · turn · sent 를 더한다. 남의 것은 status 가 verified 또는 pending, at = 낸 때.
import { current, VISIBLE } from "../../../../lib/auth.js";
import { now, json, fail, memLimited } from "../../../../lib/util.js";

export async function onRequestGet({ request, env, params }) {
  if (!env.DB) return fail(503, "no db");
  const lim = memLimited("attempt|" + (request.headers.get("cf-connecting-ip") || "?"), 120); if (lim) return lim;
  const id = String(params.id || "");
  if (!/^[0-9a-f]{32}$/.test(id)) return fail(400, "bad attempt");
  const r = await env.DB.prepare(`
    SELECT a.user_id, a.season_id, a.heroes, a.slots_json, a.started, a.expires, a.submitted AS done, a.test, u.nick,
           s.status, s.claimed, s.score, s.why, s.turn, s.submitted AS sent,
           json_extract(s.tape, '$.over') AS over, json_extract(s.tape, '$.played') AS played,
           CASE WHEN ${VISIBLE("u", "a.season_id")} THEN 1 ELSE 0 END AS visible
    FROM attempts a JOIN users u ON u.id = a.user_id LEFT JOIN scores s ON s.attempt_id = a.id
    WHERE a.id = ?1`).bind(id).first();
  if (!r) return fail(404, "no attempt");
  const me = await current(request, env);
  const mine = !!me && me.user.id === r.user_id;
  if (!mine && ((r.status !== "verified" && r.status !== "pending") || !r.nick || !r.visible || r.test)) return fail(404, "no attempt");
  const heroes = JSON.parse(r.heroes);
  if (!mine && r.status === "pending")
    return json({ attempt: id, mine, name: r.nick, season: r.season_id, heroes, score: r.claimed, over: r.over ?? null, played: r.played ?? null, slots: null, status: "pending", at: r.sent });
  const slots = JSON.parse(r.slots_json).map((text, i) => {
    const slot = JSON.parse(text);
    if (!mine && slot.from && typeof slot.from === "object") delete slot.from.seed;
    return { hero: heroes[i], slot };
  });
  const base = { attempt: id, mine, name: r.nick, season: r.season_id, heroes, score: r.score ?? null, over: r.over ?? null, played: r.played ?? null, slots };
  if (!mine) return json({ ...base, status: "verified", at: r.sent });
  return json({
    ...base, at: r.started, sent: r.sent ?? null,
    status: r.status || (r.done || r.expires < now() ? "expired" : "open"),
    claimed: r.claimed ?? null, why: r.why ?? null, turn: r.turn ?? null, test: !!r.test,
  });
}

export const onRequest = () => fail(405, "method not allowed");
