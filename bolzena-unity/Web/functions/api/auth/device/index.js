// POST /api/auth/device — PC 빌드 로그인 시작(기기 코드 방식). 게임이 부른다(로그인 없이).
// → { deviceCode(게임만 아는 긴 값), userCode(사람이 보는 짧은 코드 XXXX-XXXX), verifyUrl(브라우저로 열 주소), interval, expiresIn }
// 게임은 verifyUrl 을 브라우저로 열고 interval 초마다 /api/auth/device/poll 에 deviceCode 를 보낸다.
import { LIMIT, now, sha256, randomToken, ipKey, limited, json, fail } from "../../../../lib/util.js";

const ABC = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";   // 헷갈리는 0 · O · 1 · I 뺌
function userCode() {
  const b = crypto.getRandomValues(new Uint8Array(8));
  const s = [...b].map((x) => ABC[x % ABC.length]).join("");
  return s.slice(0, 4) + "-" + s.slice(4);
}

export async function onRequestPost({ request, env }) {
  if (!env.DB) return fail(503, "no db");
  const r = await limited(env, "device|" + await ipKey(request), 10, 60); if (r) return r;
  const t = now(), deviceCode = randomToken(32);
  await env.DB.prepare("DELETE FROM device_codes WHERE expires < ?1").bind(t).run();
  for (let i = 0; i < 5; i++) {
    const code = userCode();
    const ok = await env.DB.prepare("INSERT INTO device_codes (device_hash, user_code, created, expires) VALUES (?1, ?2, ?3, ?4) ON CONFLICT DO NOTHING RETURNING user_code")
      .bind(await sha256(deviceCode), code, t, t + LIMIT.DEVICE_SEC).first();
    if (ok) {
      const verifyUrl = new URL("/api/auth/device/approve?code=" + code, request.url).toString();
      return json({ deviceCode, userCode: code, verifyUrl, interval: LIMIT.DEVICE_INTERVAL, expiresIn: LIMIT.DEVICE_SEC });
    }
  }
  return fail(503, "busy");
}

export const onRequest = () => fail(405, "method not allowed");
