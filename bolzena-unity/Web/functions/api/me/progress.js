// /api/me/progress — 교주 보드(크레파스 보드) 진행을 계정에 두던 주소(2026-10-19, 표: migrations/0007).
//   2026-10-21 사용자 결정으로 교주 보드를 없앴다 — 새로 쓰지 않는다. 이미 쌓인 진행(board_progress)은 지우지 않는다(되돌림 · 문의용).
//   GET → { progress(없으면 null), rev(없으면 0), updated }(읽기만 그대로 — 게임은 더 부르지 않는다)
//   PUT → 410 { error: "board removed" }(옛 게임이 올려도 쓰지 않는다)
//   계정을 지우면(me.js DELETE) 이 줄도 함께 지운다.
import { requireUser } from "../../../lib/auth.js";
import { json, fail } from "../../../lib/util.js";

const out = (row) => row ? { progress: JSON.parse(row.json), rev: row.rev, updated: row.updated } : { progress: null, rev: 0, updated: null };

export async function onRequestGet({ request, env }) {
  const { me, res } = await requireUser(request, env); if (res) return res;
  const row = await env.DB.prepare("SELECT json, rev, updated FROM board_progress WHERE user_id = ?1").bind(me.user.id).first();
  return json(out(row));
}

export const onRequestPut = () => fail(410, "board removed");

export const onRequest = () => fail(405, "method not allowed");
