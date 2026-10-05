// 판 기록 받기 — 게임이 2층 보스(판의 끝) 앞 · 뒤에 보낸다(js/ui.js saveRecord · js/run.js recordOf).
// 기록 v 2(2026-10-04, 마을 판 — 두 층 · village)만 받는다. 옛 v 1(세 층 + 우로스)과 섞이지 않게 키에 v2- 를 붙인다. 읽는 길은 열지 않는다(tools/records-pull.js 가 KV 를 직접 읽는다).
// 개인정보는 없다 — 파티 · 덱 · 장비 · 싸움 결과뿐. IP 도 적지 않는다.
// 장난을 막는 것: 크기 64KB, 모양 검사, 한 판(seed)에 단계마다 한 번(같은 키는 덮어쓴다), 90일 뒤 지운다
const MAX = 64 * 1024;
const STAGE = { "2층 보스 전": "pre", "2층 보스 승리": "win", "2층 보스 패배": "lose" };

export async function onRequestPost({ request, env }) {
  const len = Number(request.headers.get("content-length") || 0);
  if (len > MAX) return new Response("too big", { status: 413 });
  const text = await request.text();
  if (text.length > MAX) return new Response("too big", { status: 413 });
  let rec;
  try { rec = JSON.parse(text); } catch { return new Response("bad json", { status: 400 }); }
  const st = rec && STAGE[rec.stage];
  if (!rec || rec.kind !== "bolzena-record" || rec.v !== 2 || typeof rec.village !== "string" || !st || !Number.isFinite(rec.seed) || !Array.isArray(rec.party) || !Array.isArray(rec.fights) || rec.fights.length > 80)
    return new Response("bad record", { status: 400 });
  await env.RECORDS.put(`rec/v2-${rec.seed}/${st}`, text, { expirationTtl: 60 * 60 * 24 * 90 });
  return new Response("ok", { status: 200 });
}
