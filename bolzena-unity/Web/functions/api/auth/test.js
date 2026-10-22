// GET /api/auth/test?sub=…&back=/ · ?sub=…&kind=pc — 로컬 시험 전용 가짜 로그인(공급자 「test」).
// 두 겹으로 막는다: 환경 변수 TEST_LOGIN 이 「1」 이고(배포에는 절대 넣지 않는다) 요청 호스트가 localhost · 127.0.0.1 일 때만. 아니면 404(있는 줄도 모르게).
// kind=pc 면 쿠키 대신 Bearer 토큰을 JSON 으로 준다(시험 스크립트 · PC 흐름 흉내).
import { userFor, newSession, finishWebLogin } from "../../../lib/auth.js";
import { setNick } from "../../../lib/nick.js";
import { isLocal, fail, json } from "../../../lib/util.js";

export async function onRequestGet({ request, env }) {
  if (env.TEST_LOGIN !== "1" || !isLocal(request)) return fail(404, "not found");
  if (!env.DB) return fail(503, "no db");
  const q = new URL(request.url).searchParams;
  const sub = q.get("sub") || "tester";
  if (!/^[a-z0-9_-]{1,32}$/i.test(sub)) return fail(400, "bad sub");
  const userId = await userFor(env, "test", sub);
  if (q.get("name")) { const r = await setNick(env, userId, q.get("name"), true); if (r.res) return r.res; }   // name 을 주면 가입(닉네임)까지 한 번에 — 안 주면 가입 전 상태
  if (q.get("kind") === "pc") {
    const { token } = await newSession(env, userId, "pc");
    return json({ token, user: userId });
  }
  return finishWebLogin(env, userId, q.get("back"));
}

export const onRequest = () => fail(405, "method not allowed");
