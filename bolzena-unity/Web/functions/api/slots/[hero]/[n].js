// 옛 주소(칸 번호) — 0005 전 게임과의 호환. 새 게임은 POST /api/slots(새로 저장) · /api/slot/:id(덮어쓰기 · 이름 · 지우기)를 쓴다.
// PUT /api/slots/:hero/:n — 완주한 판에서 뜬 사도 슬롯(HeroSlot JSON — 엔진 Runtime/Run/HeroSlot.cs)을 그 사도의 차례 번호 n 칸에 올린다(있으면 덮어쓴다 — 이름은 그대로).
//   n 은 0 이상 아무 번호(예전엔 0~2). 없는 칸을 새로 만들 때만 한도(사도 10칸 LIMIT.SLOTS_PER_HERO · 계정 LIMIT.SLOTS_PER_USER)를 본다 — 있는 칸 덮어쓰기는 한도와 상관없다.
//   여기서는 모양 · 크기 · 사도 키만 본다. 진짜 규칙 검사(HeroSlot.Check)는 검증기(bz verify)가 → 그때까지 status pending.
//   → 200 { id, hero, n, name, status: "pending", hash, … }
// DELETE /api/slots/:hero/:n — 그 칸을 비운다(이미 시작한 프론티어 시도는 시작 때 떠 둔 슬롯으로 그대로 검증된다).
import { requireUser } from "../../../../lib/auth.js";
import { LIMIT, ID, isInt, now, sha256, readText, limited, json, fail } from "../../../../lib/util.js";
import { parseSlot, slotOut, slotsFull } from "../../../../lib/slot.js";

const N_MAX = 999999;

function where(params) {
  let hero = params.hero;
  try { hero = decodeURIComponent(hero); } catch { return null; }
  const n = Number(params.n);
  if (!ID.test(hero) || !isInt(n, 0, N_MAX) || String(n) !== params.n) return null;
  return { hero: hero.normalize("NFC"), n };
}

export async function onRequestPut({ request, env, params }) {
  const { me, res } = await requireUser(request, env, true, true); if (res) return res;
  const at = where(params); if (!at) return fail(400, "bad hero or slot number");
  const lim = await limited(env, "slot|" + me.user.id, 30, 60); if (lim) return lim;
  const text = await readText(request, LIMIT.SLOT_BYTES);
  if (text === null) return fail(413, "too big", { max: LIMIT.SLOT_BYTES });
  const p = parseSlot(text, at.hero);
  if (p.why) return fail(400, p.why);
  const has = await env.DB.prepare("SELECT COUNT(*) AS c, SUM(hero = ?2) AS hc, SUM(hero = ?2 AND n = ?3) AS same FROM hero_slots WHERE user_id = ?1").bind(me.user.id, at.hero, at.n).first();
  if (!has.same && ((has.hc || 0) >= LIMIT.SLOTS_PER_HERO || has.c >= LIMIT.SLOTS_PER_USER)) return slotsFull(env, me.user.id, at.hero);
  const hash = (await sha256(text)).slice(0, 16), t = now();
  const row = await env.DB.prepare(`INSERT INTO hero_slots (user_id, hero, n, json, hash, status, why, created, updated, checked) VALUES (?1, ?2, ?3, ?4, ?5, 'pending', NULL, ?6, ?6, NULL)
    ON CONFLICT (user_id, hero, n) DO UPDATE SET json = excluded.json, hash = excluded.hash, status = 'pending', why = NULL, updated = excluded.updated, checked = NULL
    RETURNING id, hero, n, name, status, why, hash, created, updated`)
    .bind(me.user.id, at.hero, at.n, text, hash, t).first();
  return json(slotOut(row));
}

export async function onRequestDelete({ request, env, params }) {
  const { me, res } = await requireUser(request, env, true); if (res) return res;
  const at = where(params); if (!at) return fail(400, "bad hero or slot number");
  const lim = await limited(env, "slot|" + me.user.id, 30, 60); if (lim) return lim;
  const r = await env.DB.prepare("DELETE FROM hero_slots WHERE user_id = ?1 AND hero = ?2 AND n = ?3 RETURNING id").bind(me.user.id, at.hero, at.n).first();
  return r ? new Response(null, { status: 204 }) : fail(404, "no slot");
}

export const onRequest = () => fail(405, "method not allowed");
