// 검증기(bz verify) 인증 — Authorization: Bearer <VERIFY_TOKEN 시크릿>. 시크릿이 없으면 이 주소들은 닫힌다(503).
import { sameSecret, fail } from "./util.js";

export async function requireVerifier(request, env) {
  if (!env.DB) return fail(503, "no db");
  if (!env.VERIFY_TOKEN || env.VERIFY_TOKEN.length < 32) return fail(503, "verifier not configured");
  const a = request.headers.get("authorization") || "";
  if (!a.startsWith("Bearer ") || !(await sameSecret(a.slice(7).trim(), env.VERIFY_TOKEN))) return fail(401, "verifier token required");
  return null;
}
