// 시험 칸(migrations/0009 — 2026-10-20 사용자 「시험 계정 권한」) — 시험 계정(users.role 'tester')만. 아니면 403 { error: "tester only" }(로그인 전 401).
//   일반 저장 칸(/api/slots)과 따로 둔다 — 사도당 10칸 한도와 별개 · 검증기 규칙 검사 없음(상태가 없다) · 화면 「시험」 표시.
//   크기 · 형식 검사는 일반 칸과 같다(LIMIT.SLOT_BYTES · lib/slot.js shape — 엔진 HeroSlot.Parse 가 읽을 수 있는 모양).
// GET /api/test/slots[?hero=키] — 내 시험 칸 목록(최근 차례).
//   → { slots: [{ id, hero, name, hash, created, updated, test: true, slot(HeroSlot JSON) }], count, max }
// POST /api/test/slots?hero=키[&name=이름] (본문 = HeroSlot JSON 글 그대로) — 새 시험 칸. 계정당 LIMIT.TEST_SLOTS_PER_USER 칸(넘으면 409 test slots full).
//   → 201 { id, hero, name, hash, created, updated, test: true }
// 덮어쓰기 · 이름 · 지우기는 /api/test/slot/:id. 이 칸으로 도전: POST /api/frontier/start { slots: [{ test: id }, …] } — 시도 · 점수가 시험(test = 1)으로 남고 랭킹에 오르지 않는다.
import { requireTester } from "../../../lib/auth.js";
import { LIMIT, ID, now, sha256, readText, limited, json, fail } from "../../../lib/util.js";
import { parseSlot, checkSlotName, testSlotOut } from "../../../lib/slot.js";

export async function onRequestGet({ request, env }) {
  const { me, res } = await requireTester(request, env); if (res) return res;
  const hero = new URL(request.url).searchParams.get("hero");
  if (hero !== null && !ID.test(hero)) return fail(400, "bad hero");
  const { results } = await (hero !== null
    ? env.DB.prepare("SELECT id, hero, name, json, hash, created, updated FROM test_slots WHERE user_id = ?1 AND hero = ?2 ORDER BY updated DESC, id DESC").bind(me.user.id, hero.normalize("NFC"))
    : env.DB.prepare("SELECT id, hero, name, json, hash, created, updated FROM test_slots WHERE user_id = ?1 ORDER BY updated DESC, id DESC").bind(me.user.id)).all();
  return json({ slots: results.map((r) => testSlotOut(r, { slot: JSON.parse(r.json) })), count: results.length, max: LIMIT.TEST_SLOTS_PER_USER });
}

export async function onRequestPost({ request, env }) {
  const { me, res } = await requireTester(request, env, true); if (res) return res;
  const q = new URL(request.url).searchParams;
  const hero = (q.get("hero") || "").normalize("NFC");
  if (!ID.test(hero)) return fail(400, "bad hero");
  const nm = checkSlotName(q.get("name"));
  if (nm.error) return fail(400, "bad name", { why: nm.error });
  const lim = await limited(env, "slot|" + me.user.id, 30, 60); if (lim) return lim;
  const text = await readText(request, LIMIT.SLOT_BYTES);
  if (text === null) return fail(413, "too big", { max: LIMIT.SLOT_BYTES });
  const p = parseSlot(text, hero);
  if (p.why) return fail(400, p.why);
  const hash = (await sha256(text)).slice(0, 16), t = now();
  // 한도를 한 문장에서 — 동시에 여러 번 눌러도 넘지 않는다(넘으면 줄이 생기지 않아 RETURNING 이 빈다)
  const row = await env.DB.prepare(`
    INSERT INTO test_slots (user_id, hero, name, json, hash, created, updated)
    SELECT ?1, ?2, ?3, ?4, ?5, ?6, ?6 WHERE (SELECT COUNT(*) FROM test_slots WHERE user_id = ?1) < ?7
    RETURNING id, hero, name, hash, created, updated`).bind(me.user.id, hero, nm.name, text, hash, t, LIMIT.TEST_SLOTS_PER_USER).first();
  if (!row) return fail(409, "test slots full", { max: LIMIT.TEST_SLOTS_PER_USER });
  return json(testSlotOut(row), 201);
}

export const onRequest = () => fail(405, "method not allowed");
