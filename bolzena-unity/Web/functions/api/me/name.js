// POST · PUT /api/me/name { name } — 닉네임 정하기(= 가입 끝) · 바꾸기. 규칙은 lib/nick.js:
//   2~12자 · 앞뒤 공백 정리(사이 공백은 하나로) · 한글 · 영문 · 숫자 · 공백 · _ - . 만(제어 문자 · HTML · 보이지 않는 글자는 못 씀) ·
//   대소문자 · 전각/반각 · 공백 · _ - . 차이는 같은 이름으로 보고 다른 사람과 겹치면 409 · 운영자 · 공식 사칭 금지어 400 ·
//   처음 정하기는 언제든, 바꾸기는 하루(24시간)에 한 번(429 retryAfter).
// → 200 { user: { id, name, … }, needsName: false }
import { requireUser } from "../../../lib/auth.js";
import { LIMIT, readJson, limited, json, fail } from "../../../lib/util.js";
import { setNick } from "../../../lib/nick.js";

async function handle({ request, env }) {
  const { me, res } = await requireUser(request, env, true); if (res) return res;
  const lim = await limited(env, "name|" + me.user.id, 20, 60); if (lim) return lim;
  const { value, res: bad } = await readJson(request, LIMIT.SMALL_BYTES); if (bad) return bad;
  const r = await setNick(env, me.user.id, value && value.name);
  if (r.res) return r.res;
  return json({ user: { ...me.user, name: r.name }, needsName: false });
}

export const onRequestPost = handle;
export const onRequestPut = handle;
export const onRequest = () => fail(405, "method not allowed");
