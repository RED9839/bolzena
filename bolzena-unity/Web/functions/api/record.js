// 플레이 기록 받기 v2(유니티판) — 게임이 판 끝(승리 · 패배 · 포기 · 메인으로)에 한 번 보낸다(runui PlayRecord.cs · core RunRecord.cs).
// 형식 · 개인 정보 원칙 · 배포 절차: bolzena-unity/Docs/플레이 기록.md
//
// - 받는 것: kind "bolzena-record" · v 2 · app "unity" 만. 옛 v1(웹판 「2층 보스 전 · 뒤」)은 웹판이 내려가 보내는 곳이 없어 받지 않는다(410).
// - 쌓는 곳: KV bolzena-records(바인딩 RECORDS) — 키 v2/<yyyymmdd(UTC, 받은 날)>/<익명 id>/<씨앗>/<결과>, 180일 뒤 지운다. 같은 판 · 같은 결과는 덮어쓴다.
// - 읽는 주소는 없다(GET 405). 읽기는 wrangler 로그인한 사람만(bz records pull).
// - 사람을 알아볼 것은 받지도 적지도 않는다 — IP · User-Agent 를 KV 에 남기지 않는다(분당 횟수 막기에 메모리에서만 잠깐 쓴다).
// - 남용 막기: 크기 64KB · 모양 검사(위쪽 키 허용 목록) · 다른 사이트에서 오는 요청 거절 · 같은 IP 분당 6번(이 서버 조각의 메모리 — 느슨함) ·
//   KV 쓰기 한도(무료 하루 1000번)를 넘으면 503 — 게임은 실패한 기록을 기기에 5개까지 두었다 다음 실행 때 다시 보낸다.
const MAX = 64 * 1024;
const TTL = 60 * 60 * 24 * 180;
const PER_MIN = 6;
const RESULTS = new Set(["win", "lose", "abandon", "quit"]);
const TOP = new Set(["kind", "v", "app", "anon", "ver", "build", "os", "phone", "low", "seed", "village", "nature", "began", "ended", "min", "result",
  "party", "floor", "node", "step", "at", "grade", "credits", "board", "fast", "gold", "hp", "maxHp", "removals", "fights", "picks", "deck", "arts", "death", "trim"]);
const ID = /^[^\s<>"'`\\/]{1,64}$/u;   // 사도 · 카드 · 마을 id(한글 포함) — 공백 · 꺾쇠 · 따옴표 · 빗금 없이
const HEX = /^[0-9a-f]{16,64}$/;
const TIME = /^\d{4}-\d\d-\d\dT\d\d:\d\dZ$/;

const hits = new Map();   // 「ip|분」 → 횟수(이 조각 메모리 — 오래된 것은 지운다)
function limited(ip) {
  const min = Math.floor(Date.now() / 60000);
  const k = ip + "|" + min;
  const n = (hits.get(k) || 0) + 1;
  hits.set(k, n);
  if (hits.size > 5000) for (const key of hits.keys()) if (!key.endsWith("|" + min)) hits.delete(key);
  return n > PER_MIN;
}

const str = (x, n = 64) => x === undefined || x === null || (typeof x === "string" && x.length <= n);
const int = (x) => x === undefined || Number.isSafeInteger(x);

/** 틀리면 까닭(글), 맞으면 null. */
function check(rec) {
  if (!rec || typeof rec !== "object" || Array.isArray(rec)) return "not object";
  if (rec.kind !== "bolzena-record") return "kind";
  if (rec.v !== 2 || rec.app !== "unity") return "version";
  for (const k of Object.keys(rec)) if (!TOP.has(k)) return "field " + k.slice(0, 20);
  if (typeof rec.anon !== "string" || !HEX.test(rec.anon)) return "anon";
  if (!Number.isSafeInteger(rec.seed)) return "seed";
  if (!RESULTS.has(rec.result)) return "result";
  if (!str(rec.ver, 24) || !str(rec.build, 24) || !str(rec.os, 12) || !str(rec.at, 16)) return "meta";
  if (typeof rec.village !== "string" || !ID.test(rec.village) || !str(rec.nature, 16)) return "village";
  for (const t of [rec.began, rec.ended]) if (t !== undefined && !TIME.test(t)) return "time";
  for (const n of ["min", "floor", "node", "step", "grade", "credits", "board", "gold", "hp", "maxHp", "removals", "trim"]) if (!int(rec[n])) return "number " + n;
  if (rec.fast !== undefined && !(typeof rec.fast === "number" && rec.fast >= 0 && rec.fast <= 1)) return "fast";
  if (!Array.isArray(rec.party) || rec.party.length < 1 || rec.party.length > 4 || !rec.party.every((p) => p && typeof p.id === "string" && ID.test(p.id) && int(p.slot))) return "party";
  if (!Array.isArray(rec.fights) || rec.fights.length > 80 || !rec.fights.every((f) => f && typeof f === "object" && Array.isArray(f.foes) && (f.log === undefined || (Array.isArray(f.log) && f.log.length <= 60)))) return "fights";
  if (rec.picks !== undefined && (!Array.isArray(rec.picks) || rec.picks.length > 1500)) return "picks";
  if (rec.deck !== undefined && (!Array.isArray(rec.deck) || rec.deck.length > 200)) return "deck";
  return null;
}

export async function onRequestPost({ request, env }) {
  const origin = request.headers.get("origin");
  if (origin && new URL(origin).host !== new URL(request.url).host) return new Response("cross origin", { status: 403 });
  const len = Number(request.headers.get("content-length") || 0);
  if (len > MAX) return new Response("too big", { status: 413 });
  const ip = request.headers.get("cf-connecting-ip") || "?";
  if (limited(ip)) return new Response("slow down", { status: 429, headers: { "retry-after": "60" } });
  const text = await request.text();
  if (new TextEncoder().encode(text).length > MAX) return new Response("too big", { status: 413 });
  let rec;
  try { rec = JSON.parse(text); } catch { return new Response("bad json", { status: 400 }); }
  if (rec && rec.kind === "bolzena-record" && rec.v !== 2) return new Response("old record", { status: 410 });
  const why = check(rec);
  if (why) return new Response("bad record: " + why, { status: 400 });
  if (!env.RECORDS) return new Response("no store", { status: 503 });
  const day = new Date().toISOString().slice(0, 10).replace(/-/g, "");
  const key = `v2/${day}/${rec.anon}/${rec.seed}/${rec.result}`;
  try {
    await env.RECORDS.put(key, text, { expirationTtl: TTL });
  } catch {
    return new Response("store busy", { status: 503 });   // 하루 쓰기 한도 따위 — 게임이 다음에 다시 보낸다
  }
  return new Response(null, { status: 204 });
}

export function onRequest() {
  return new Response("method not allowed", { status: 405, headers: { allow: "POST" } });
}
