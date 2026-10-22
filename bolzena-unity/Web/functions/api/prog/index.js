// GET /api/prog — 내 성장(마시멜로 지갑 · 이번 주 받은 양 · 최고 완주 단계 · 사도 20명의 잠재력 10칸). 표 · 계산: lib/prog.js · migrations/0010.
//   스위치(환경 변수 PROG = "1")가 꺼져 있으면 404 { error: "prog off" } — 게임은 성장 화면을 숨긴다.
//   로그인 안 했으면 401(로그인 안 하면 성장 없음 — 설계 G10). 가입(닉네임) 전에도 읽기는 된다(빈 성장).
//   → 200 { on, rev, bestTier, open, wallet: { mm1 · mm2 · mm3 }(마시멜로 하급 · 중급 · 상급), currency(재화 열쇠 · 이름 표),
//          week: { got, cap, ends }, heroes: { 사도: { role, pot, potential: [{ id, lv, max, after, next, ok, why, locked }] } }, rules, runs }
import { requireUser } from "../../../lib/auth.js";
import { json, fail } from "../../../lib/util.js";
import { progOn, loadAccount, loadHeroes, view } from "../../../lib/prog.js";

export async function onRequestGet({ request, env }) {
  if (!progOn(env)) return fail(404, "prog off");
  const { me, res } = await requireUser(request, env); if (res) return res;
  const [acc, heroes] = await Promise.all([loadAccount(env, me.user.id), loadHeroes(env, me.user.id)]);
  return json(view(acc, heroes));
}

export const onRequest = () => fail(405, "method not allowed");
