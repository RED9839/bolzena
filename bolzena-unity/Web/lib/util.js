// 프론티어 서버 공용 — 응답 · 본문 읽기 · 다른 사이트 막기 · 속도 제한 · 해시 · 무작위. functions/ 밖에 두어 주소가 되지 않는다.
// 설명: ../../Docs/프론티어_서버.md

/** 한도(Docs/프론티어_서버.md §남용 막기 — 설계 추천값). */
export const LIMIT = {
  SLOT_BYTES: 16 * 1024,        // 슬롯 하나(봇 슬롯 1KB 안팎 · 카드 40장 상한이라도 8KB 밑)
  TAPE_BYTES: 256 * 1024,       // 프론티어 제출 하나(봇 테이프 18KB 안팎)
  SMALL_BYTES: 4 * 1024,        // 그 밖의 요청 본문
  VERIFY_BYTES: 512 * 1024,     // 검증기 결과 한 번
  // 사도마다 칸 10개까지(2026-10-19 사용자 「사도마다 저장 칸 10칸까지」 — 10-18 의 「한도 없음」 을 바꿈). 11번째 새로 저장은 409 hero slots full
  //   (덮어쓰기 · 이름 · 지우기는 그대로). 이미 10칸을 넘게 가진 사도(한도 전 저장)는 읽기 · 덮어쓰기 · 지우기는 되고 새로 저장만 10칸 아래로 줄 때까지 막힌다 — 칸을 지우지 않는다.
  SLOTS_PER_HERO: 10,
  // 계정 전체 안전 한도(남용 막이) — 사도 한도와 따로 둔다(옛 칸 · 운영 복구가 한도를 넘겨도 목록 응답이 감당하는 선).
  // 1,000칸 × 16KB 상한 = 사용자 하나 최대 16MB(실제 슬롯은 1~2KB 라 1~2MB).
  SLOTS_PER_USER: 1000,
  // 시험 칸(migrations/0009 — 시험 계정만) — 일반 칸 한도(사도 10 · 계정 1,000)와 따로 센다. 계정당 이만큼(넘으면 409 test slots full).
  TEST_SLOTS_PER_USER: 100,
  // 하루 시도 한도는 없다(2026-10-19 사용자 — 도전마다 완전 무작위 씨앗, 다시 하기도 노력으로 인정). 남용 막이는 분당 시작 · 제출 속도 제한(start.js · submit.js).
  ATTEMPT_SEC: 2 * 60 * 60,     // 시도를 시작하고 제출할 수 있는 시간
  WEB_SESSION_SEC: 30 * 24 * 60 * 60,
  PC_SESSION_SEC: 180 * 24 * 60 * 60,
  DEVICE_SEC: 10 * 60,          // 기기 코드가 살아 있는 시간
  DEVICE_INTERVAL: 5,           // 기기 코드 폴링 간격(초)
};

export const now = () => Math.floor(Date.now() / 1000);

export function json(body, status = 200, headers = {}) {
  return new Response(JSON.stringify(body), { status, headers: { "content-type": "application/json; charset=utf-8", "cache-control": "no-store", ...headers } });
}
export const fail = (status, error, extra = {}) => json({ error, ...extra }, status);

/** 본문을 글로 — 크기 한도를 넘으면 null(413 을 돌려줄 것). content-length 와 실제 바이트 둘 다 본다. */
export async function readText(request, max) {
  const len = Number(request.headers.get("content-length") || 0);
  if (len > max) return null;
  const text = await request.text();
  if (new TextEncoder().encode(text).length > max) return null;
  return text;
}

/** 본문 JSON — { value } 또는 { res }(그대로 돌려줄 오류 응답). */
export async function readJson(request, max) {
  const text = await readText(request, max);
  if (text === null) return { res: fail(413, "too big") };
  try { return { value: JSON.parse(text), text }; } catch { return { res: fail(400, "bad json") }; }
}

/** 다른 사이트에서 온 요청이면 true — Origin 이 있고 호스트가 다르면(record.js 와 같은 검사). */
export function crossOrigin(request) {
  const origin = request.headers.get("origin");
  if (!origin) return false;
  try { return new URL(origin).host !== new URL(request.url).host; } catch { return true; }
}

/** 이 요청이 로컬 시험인가 — 호스트가 localhost · 127.0.0.1 일 때만. */
export function isLocal(request) {
  const h = new URL(request.url).hostname;
  return h === "localhost" || h === "127.0.0.1" || h === "[::1]";
}

const enc = new TextEncoder();
const hex = (buf) => [...new Uint8Array(buf)].map((b) => b.toString(16).padStart(2, "0")).join("");
export async function sha256(text) { return hex(await crypto.subtle.digest("SHA-256", enc.encode(text))); }
export async function hmac(key, text) {
  const k = await crypto.subtle.importKey("raw", enc.encode(key), { name: "HMAC", hash: "SHA-256" }, false, ["sign"]);
  return hex(await crypto.subtle.sign("HMAC", k, enc.encode(text)));
}
export function b64url(bytes) {
  let s = ""; for (const b of bytes) s += String.fromCharCode(b);
  return btoa(s).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}
export const randomToken = (n = 32) => b64url(crypto.getRandomValues(new Uint8Array(n)));
export const randomHex = (n = 16) => hex(crypto.getRandomValues(new Uint8Array(n)).buffer);
/** 서버 씨앗 — 1 ~ 2^31-1(엔진 Rng 는 32비트로 자르니 이 범위면 C# long · JS 수 어느 쪽도 그대로). */
export function randomSeed() {
  const v = crypto.getRandomValues(new Uint32Array(1))[0] & 0x7fffffff;
  return v === 0 ? 1 : v;
}

/** 두 글이 같은가 — 해시를 견주어 길이 · 앞자리로 새지 않게. */
export async function sameSecret(a, b) {
  if (typeof a !== "string" || typeof b !== "string" || !a || !b) return false;
  const [x, y] = await Promise.all([sha256(a), sha256(b)]);
  let d = 0; for (let i = 0; i < x.length; i++) d |= x.charCodeAt(i) ^ y.charCodeAt(i);
  return d === 0;
}

export const ipKey = async (request) => (await sha256("ip|" + (request.headers.get("cf-connecting-ip") || "?"))).slice(0, 16);

/**
 * 속도 제한(D1) — 열쇠마다 sec 초 창에 max 번까지. 넘으면 429 응답, 아니면 null. 쓰는 요청에만 쓴다(읽기는 memLimited).
 * 창이 지난 줄은 가끔(요청 50번에 한 번쯤) 지운다.
 */
export async function limited(env, key, max, sec) {
  const t = now(), win = Math.floor(t / sec);
  const row = await env.DB.prepare("INSERT INTO rate (k, win, n, until) VALUES (?1, ?2, 1, ?3) ON CONFLICT (k, win) DO UPDATE SET n = n + 1 RETURNING n")
    .bind(key, win, (win + 1) * sec).first();
  if (Math.random() < 0.02) await env.DB.prepare("DELETE FROM rate WHERE until < ?1").bind(t).run();
  if (row && row.n > max) return fail(429, "slow down", { retryAfter: (win + 1) * sec - t });
  return null;
}

const hits = new Map();   // 「열쇠|분」 → 횟수(이 서버 조각 메모리 — 느슨함, record.js 와 같은 방식)
/** 읽기용 느슨한 속도 제한 — 분당 max. 넘으면 429 응답. */
export function memLimited(key, max) {
  const min = Math.floor(Date.now() / 60000), k = key + "|" + min;
  const n = (hits.get(k) || 0) + 1;
  hits.set(k, n);
  if (hits.size > 5000) for (const x of hits.keys()) if (!x.endsWith("|" + min)) hits.delete(x);
  return n > max ? fail(429, "slow down", { retryAfter: 60 }) : null;
}

/** 사도 키 — 한글 포함, 공백 · 꺾쇠 · 따옴표 · 빗금 없이(record.js 의 ID 와 같다). */
export const ID = /^[^\s<>"'`\\/]{1,64}$/u;
export const isInt = (x, lo, hi) => Number.isSafeInteger(x) && x >= lo && x <= hi;
export const yyyymmdd = (t = now()) => new Date(t * 1000).toISOString().slice(0, 10).replace(/-/g, "");
