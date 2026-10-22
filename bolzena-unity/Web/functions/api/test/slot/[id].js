// 시험 칸 하나(migrations/0009 — 서버가 준 id) — 시험 계정만(아니면 403 tester only). 남의 칸 · 없는 칸은 404(있는 줄도 모르게).
// PUT /api/test/slot/:id (본문 = HeroSlot JSON 글 그대로) — 덮어쓰기. 슬롯의 사도는 그 칸의 사도와 같아야 한다(400). 크기 · 형식 검사는 일반 칸과 같다.
//   → 200 { id, hero, name, hash, created, updated, test: true }
// PATCH /api/test/slot/:id { name } — 이름 바꾸기(빈 글 · null = 이름 지우기). → 200
// DELETE /api/test/slot/:id — 지우기 → 204(이미 시작한 시도는 시작 때 떠 둔 슬롯 글을 그대로 쓴다).
import { requireTester } from "../../../../lib/auth.js";
import { LIMIT, isInt, now, sha256, readText, readJson, limited, json, fail } from "../../../../lib/util.js";
import { parseSlot, checkSlotName, testSlotOut } from "../../../../lib/slot.js";

const RET = "RETURNING id, hero, name, hash, created, updated";
const idOf = (params) => { const s = String(params.id || ""), id = Number(s); return /^[1-9][0-9]{0,15}$/.test(s) && isInt(id, 1, Number.MAX_SAFE_INTEGER) ? id : null; };

export async function onRequestPut({ request, env, params }) {
  const { me, res } = await requireTester(request, env, true); if (res) return res;
  const id = idOf(params); if (!id) return fail(400, "bad slot id");
  const lim = await limited(env, "slot|" + me.user.id, 30, 60); if (lim) return lim;
  const cur = await env.DB.prepare("SELECT hero FROM test_slots WHERE id = ?1 AND user_id = ?2").bind(id, me.user.id).first();
  if (!cur) return fail(404, "no slot");
  const text = await readText(request, LIMIT.SLOT_BYTES);
  if (text === null) return fail(413, "too big", { max: LIMIT.SLOT_BYTES });
  const p = parseSlot(text, cur.hero);
  if (p.why) return fail(400, p.why);
  const hash = (await sha256(text)).slice(0, 16);
  const row = await env.DB.prepare(`UPDATE test_slots SET json = ?1, hash = ?2, updated = ?3 WHERE id = ?4 AND user_id = ?5 ${RET}`)
    .bind(text, hash, now(), id, me.user.id).first();
  return row ? json(testSlotOut(row)) : fail(404, "no slot");
}

export async function onRequestPatch({ request, env, params }) {
  const { me, res } = await requireTester(request, env, true); if (res) return res;
  const id = idOf(params); if (!id) return fail(400, "bad slot id");
  const lim = await limited(env, "slot|" + me.user.id, 30, 60); if (lim) return lim;
  const { value, res: bad } = await readJson(request, LIMIT.SMALL_BYTES); if (bad) return bad;
  if (!value || typeof value !== "object" || !("name" in value)) return fail(400, "name required");
  const nm = checkSlotName(value.name);
  if (nm.error) return fail(400, "bad name", { why: nm.error });
  const row = await env.DB.prepare(`UPDATE test_slots SET name = ?1 WHERE id = ?2 AND user_id = ?3 ${RET}`).bind(nm.name, id, me.user.id).first();
  return row ? json(testSlotOut(row)) : fail(404, "no slot");
}

export async function onRequestDelete({ request, env, params }) {
  const { me, res } = await requireTester(request, env, true); if (res) return res;
  const id = idOf(params); if (!id) return fail(400, "bad slot id");
  const lim = await limited(env, "slot|" + me.user.id, 30, 60); if (lim) return lim;
  const r = await env.DB.prepare("DELETE FROM test_slots WHERE id = ?1 AND user_id = ?2 RETURNING id").bind(id, me.user.id).first();
  return r ? new Response(null, { status: 204 }) : fail(404, "no slot");
}

export const onRequest = () => fail(405, "method not allowed");
