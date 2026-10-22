// 검증 대상 고르기(2026-10-19 사용자 「검증은 자기 최고 기록 후보만」) — 제출(submit.js) · 검증 결과(verify/result.js) · 운영 조치(admin/act.js)가 같이 쓴다.
// 설명: ../../Docs/프론티어_서버.md §검증 대상
//   낼 때: 그 사람 · 그 시즌의 지금 최고(verified · pending 가운데 가장 높은 점수 — 검증 전은 주장 점수)보다 **높으면** pending(검증 대기열),
//          아니면(같거나 낮으면) skipped(「검증 안 함」 — 검증기가 가져가지 않고 랭킹에도 들지 않는다. 그 사람의 더 높은 줄이 이미 있으니 랭킹은 같다).
//   최고 후보가 빠지면(검증기 rejected · 운영 거절 · 운영 지우기): 남은 최고보다 높은 skipped 가운데 가장 높은 것 하나를 pending 으로 올린다(promoteNext) —
//          그것도 거절되면 다시 다음 것. 그래야 랭킹에 그 사람의 진짜 최고가 남는다.
//   시험 점수(test = 1 — migrations/0009)는 셈에 넣지 않는다 — 늘 skipped 로 들어가고 · 최고로 치지 않고 · 다음 후보로 올리지 않는다.

/** 그 사람 · 시즌의 지금 최고(verified · pending) — 없으면 null. */
export async function bestOf(env, userId, seasonId) {
  const r = await env.DB.prepare("SELECT MAX(COALESCE(score, claimed)) AS m FROM scores WHERE user_id = ?1 AND season_id = ?2 AND status IN ('verified', 'pending') AND test = 0")
    .bind(userId, seasonId).first();
  return r && r.m !== null && r.m !== undefined ? r.m : null;
}

/** 새로 낸 점수의 상태 — 지금 최고보다 높으면 pending, 아니면 skipped. */
export async function statusForNew(env, userId, seasonId, claimed) {
  const best = await bestOf(env, userId, seasonId);
  return best === null || claimed > best ? "pending" : "skipped";
}

/**
 * 다음 후보 올리기 — 가장 높은 skipped(같은 점수면 먼저 낸 것)가 남은 최고보다 높으면 pending 으로. 올린 점수 id 또는 null.
 * 한 번에 하나만 올린다(그것이 검증되면 나머지는 그보다 낮으니 그대로 skipped).
 */
export async function promoteNext(env, userId, seasonId) {
  const best = await bestOf(env, userId, seasonId);
  const top = await env.DB.prepare("SELECT id, claimed FROM scores WHERE user_id = ?1 AND season_id = ?2 AND status = 'skipped' AND test = 0 ORDER BY claimed DESC, submitted ASC, id ASC LIMIT 1")
    .bind(userId, seasonId).first();
  if (!top || (best !== null && top.claimed <= best)) return null;
  const up = await env.DB.prepare("UPDATE scores SET status = 'pending' WHERE id = ?1 AND status = 'skipped' RETURNING id").bind(top.id).first();
  return up ? up.id : null;
}
