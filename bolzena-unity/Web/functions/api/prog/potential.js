// POST /api/prog/potential { hero, node } — 잠재력 칸 하나를 한 단계 올린다(성장_설계_카제나판.md §11 · §12 · migrations/0010). 서버만 판단한다.
//   조건(lib/prog.js potCheck): 앞 칸(after) 1단계 이상(첫 칸 01 은 없음 — 카제나 줄기 · 갈래) · 최대 단계 아님 · 재료(마시멜로 하급 · 중급 · 상급 — 성격 구분 없음). 진급 · 골드는 없앴다(2026-10-21 밤).
//   값 표: lib/prog.js POTENTIAL · POT_COST(카제나 칸 · 레벨별 표에서 골드를 뺀 것 — 특수 재료는 상급 마시멜로로 바꿈 — 사도 하나 10칸 = 하급 24 · 중급 125 · 상급 440).
//   못 하면 409 { error: "cannot", why(화면 글), cost, locked } · 최대면 409 { error: "max" } · 모르는 사도 · 칸 400.
//   스위치 꺼짐 404 · 로그인 안 함 401 · 가입 전 403 · 정지 403. 분당 60번(칸을 여러 번 누르는 화면이라 넉넉히).
//   쓰기: 계정 줄(재료) · 사도 줄(pot JSON) · 기록 줄(prog_pot_log — cost JSON)을 한 batch 로(lib/prog.js save — 낙관적 잠금).
//   → 200 { hero, node, lv: [전, 뒤], spent: { items }, prog(GET /api/prog 와 같은 꼴) }
import { requireUser } from "../../../lib/auth.js";
import { LIMIT, now, readJson, limited, json, fail } from "../../../lib/util.js";
import { progOn, loadAccount, loadHeroes, heroOf, view, save, potCheck, POTENTIAL, HEROES } from "../../../lib/prog.js";

export async function onRequestPost({ request, env }) {
  if (!progOn(env)) return fail(404, "prog off");
  const { me, res } = await requireUser(request, env, true, true); if (res) return res;
  const lim = await limited(env, "potential|" + me.user.id, 60, 60); if (lim) return lim;
  const { value: v, res: bad } = await readJson(request, LIMIT.SMALL_BYTES); if (bad) return bad;
  const hero = v && typeof v.hero === "string" ? v.hero.normalize("NFC") : "";
  if (!Object.prototype.hasOwnProperty.call(HEROES, hero)) return fail(400, "bad hero");
  const node = v && typeof v.node === "string" ? v.node : "";
  if (!POTENTIAL.some((n) => n.id === node)) return fail(400, "bad node");
  const uid = me.user.id, t = now();
  for (let tries = 0; tries < 4; tries++) {
    const [acc, heroes] = await Promise.all([loadAccount(env, uid, t), loadHeroes(env, uid)]);
    const h = { ...heroOf(heroes, hero) }; h.pot = { ...(h.pot || {}) };
    const c = potCheck(acc, h, node);
    if (!c.cost) return fail(409, "max", { why: c.why });
    if (!c.ok) return fail(409, "cannot", { why: c.why, cost: c.cost, locked: c.locked });
    for (const [k, n] of Object.entries(c.cost.items)) acc.bag[k] = (acc.bag[k] | 0) - n;
    const from = c.lv;
    h.pot[node] = from + 1;
    heroes[hero] = h;
    const log = (tx) => env.DB.prepare("INSERT INTO prog_pot_log (user_id, hero, node, lv, cost, at) SELECT ?1, ?2, ?3, ?4, ?5, ?6 WHERE (SELECT tx FROM prog_account WHERE user_id = ?1) = ?7")
      .bind(uid, hero, node, from + 1, JSON.stringify(c.cost.items), t, tx);
    if (!(await save(env, uid, acc, heroes, [hero], t, [log]))) continue;
    return json({ hero, node, lv: [from, from + 1], spent: c.cost, prog: view(acc, heroes) });
  }
  return fail(409, "busy", { why: "다른 기기가 동시에 바꾸는 중 — 잠시 뒤에 다시" });
}

export const onRequest = () => fail(405, "method not allowed");
