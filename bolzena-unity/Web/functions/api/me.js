// /api/me — 로그인한 사용자.
//   GET → { user: { id, name(닉네임 — 가입 전엔 null), nameChanged, provider, created, role }, needsName, session: { kind, expires }, limits }
//     role = 「tester」(시험 계정 — migrations/0009, 운영 조치로만 부여 · 회수) 또는 null. 게임은 이것으로 「덱 만들기」 를 보인다 — 권한 판단은 서버가 다시 한다.
//     ban = 정지면 { why, until(영구면 null), forever } · 아니면 null(정지되면 쓰기가 403 banned — 로그인 · 읽기 · 탈퇴는 된다).
//     needsName true = 가입 전(첫 로그인 뒤 닉네임을 아직 안 정함) — 화면이 닉네임 창을 띄운다. 닉네임 정하기 · 바꾸기는 /api/me/name.
//   DELETE → 계정 지우기(탈퇴) — 세션 · 슬롯 · 시험 칸 · 교주 보드 진행 · 성장(0010 — 계정 · 사도 잠재력 · 받은 보상 기록 · 잠재력 기록) · 시도 · 점수(랭킹 줄 포함)를 모두 지운다. 되돌릴 수 없다.
import { requireUser, cookie, COOKIE } from "../../lib/auth.js";
import { LIMIT, fail, json } from "../../lib/util.js";
import { NICK_MIN, NICK_MAX, NICK_CHANGE_SEC } from "../../lib/nick.js";
import { progTables } from "../../lib/prog.js";

export async function onRequestGet({ request, env }) {
  const { me, res } = await requireUser(request, env); if (res) return res;
  return json({
    user: me.user, needsName: !me.user.name, ban: me.ban, session: { kind: me.session.kind, expires: me.session.expires },
    name: { min: NICK_MIN, max: NICK_MAX, changeEverySec: NICK_CHANGE_SEC },
    limits: { slotsPerHero: null, slotsPerUser: LIMIT.SLOTS_PER_USER, attemptsPerDay: null, attemptSec: LIMIT.ATTEMPT_SEC },
  });
}

export async function onRequestDelete({ request, env }) {
  const { me, res } = await requireUser(request, env, true, false, true); if (res) return res;   // 정지돼도 탈퇴는 된다
  const id = me.user.id, db = env.DB;
  // 성장 표(0010)는 있을 때만 — 0010 전 D1(공개판 2026-10-22)에서 없는 표를 부르면 batch 전체가 실패한다
  const prog = (await progTables(env)) ? ["prog_pot_log", "prog_earn_log", "prog_hero", "prog_account"].map((t) => db.prepare(`DELETE FROM ${t} WHERE user_id = ?1`).bind(id)) : [];
  await db.batch([
    db.prepare("DELETE FROM scores WHERE user_id = ?1").bind(id),
    db.prepare("DELETE FROM attempts WHERE user_id = ?1").bind(id),
    db.prepare("DELETE FROM hero_slots WHERE user_id = ?1").bind(id),
    db.prepare("DELETE FROM test_slots WHERE user_id = ?1").bind(id),
    db.prepare("DELETE FROM board_progress WHERE user_id = ?1").bind(id),
    ...prog,
    db.prepare("DELETE FROM device_codes WHERE user_id = ?1").bind(id),
    db.prepare("DELETE FROM rank_hidden WHERE user_id = ?1").bind(id),
    db.prepare("DELETE FROM sessions WHERE user_id = ?1").bind(id),
    db.prepare("DELETE FROM users WHERE id = ?1").bind(id),
  ]);
  return new Response(null, { status: 204, headers: { "set-cookie": cookie(COOKIE, "", 0) } });
}

export const onRequest = () => fail(405, "method not allowed");
