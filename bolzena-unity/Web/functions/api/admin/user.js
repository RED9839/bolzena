// GET /api/admin/user?id=|nick=|attempt=|slot= — 운영자 전용(Authorization: Bearer <ADMIN_TOKEN>). 사용자 찾기 · 보기(읽기 — 운영 기록에 남기지 않는다).
//   id = 사용자 번호 · nick = 닉네임(겹침 열쇠가 같은 사람 먼저, 그다음 부분 일치 10명까지) · attempt = 시도 id(랭킹 줄의 attempt) · slot = 칸 id.
// → { by, exact(nick 일 때 — 열쇠가 딱 같은 사람이 있나), users: [lib/admin.js userSummary — 닉네임 · 가입 · 마지막 활동 · 세션 수 · 슬롯 · 시도 · 점수 · 시즌별 최고 · 정지 · 숨김 · 최근 시도 · 최근 운영 기록] }
//   OAuth 원 번호 · 세션 토큰 · IP 는 내보내지 않는다.
import { requireAdmin, findUsers, userSummary } from "../../../lib/admin.js";
import { json, fail } from "../../../lib/util.js";

export async function onRequestGet({ request, env }) {
  const { res } = await requireAdmin(request, env); if (res) return res;
  const f = await findUsers(env, new URL(request.url).searchParams);
  if (f.res) return f.res;
  const users = [];
  for (const id of f.ids) { const s = await userSummary(env, id); if (s) users.push(s); }
  return json({ by: f.by, ...(f.by === "nick" ? { exact: f.exact } : {}), users });
}

export const onRequest = () => fail(405, "method not allowed");
