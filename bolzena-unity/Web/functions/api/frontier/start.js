// POST /api/frontier/start { slots: [{ id }, …] (1~3, 같은 사도 둘 금지) } — 프론티어 시도를 연다.
//   칸은 서버가 준 id 로(2026-10-18 칸 수 한도 없앰). 옛 게임의 { hero, n }(사도 · 차례 번호)도 받는다.
//   슬롯은 ok 또는 pending 만(bad 는 거절). 서버가 무작위 씨앗을 정하고, 그 순간의 슬롯 글을 떠 둔다(그 뒤 덮어써도 이 시도는 떠 둔 슬롯으로 검증).
//   떠 둔 슬롯의 지문(hero_slots.hash)도 적어 둔다(slot_hashes — 칸별 최고 · migrations/0004).
//   하루 시도 한도는 없다(2026-10-19 사용자 — 다시 하기도 노력으로 인정). 남용 막이는 분당 시작 6번(아래 limited).
//   시험(migrations/0009): 칸 참조 { test: id } = 시험 칸(test_slots) — 시험 계정(users.role 'tester')만, 아니면 403 tester only(역할은 서버만 본다).
//     시험 계정이 연 시도는 칸 종류와 상관없이 모두 test = 1 — 낸 점수는 랭킹 · 남의 편성 보기 · 내 순위에 들지 않고 검증기도 가져가지 않는다(submit.js).
//   성장(migrations/0010 — 스위치 PROG = "1" 일 때만): 편성 사도의 지금 잠재력(lib/prog.js snapshot)을 attempts.prog_json 에 떠 둔다 —
//     게임은 응답의 prog 로 Frontier.Start(…, prog), 검증기는 pending 의 prog 로 셋업을 다시 지어 견준다.
//     스위치가 꺼져 있거나 편성 누구도 잠재력이 없으면 떠 두지 않는다(prog 칸 없음 = 지금과 같은 셋업).
//   → 200 { attempt, seed, season, heroes, slots(떠 둔 HeroSlot JSON — 게임은 이것으로 싸운다), expires, left: null(옛 게임 호환 — 한도 없음), test(시험 시도인가), prog(떠 둔 잠재력이 있을 때만) }
import { requireUser } from "../../../lib/auth.js";
import { LIMIT, ID, isInt, now, yyyymmdd, randomHex, randomSeed, readJson, limited, json, fail } from "../../../lib/util.js";
import { seasonOf, publicSeason } from "../../../lib/season.js";
import { progOn, loadHeroes, snapshot } from "../../../lib/prog.js";

const PARTY_MAX = 3;   // 엔진 Frontier.PARTY_MAX

export async function onRequestPost({ request, env }) {
  const { me, res } = await requireUser(request, env, true, true); if (res) return res;
  const lim = await limited(env, "start|" + me.user.id, 6, 60); if (lim) return lim;
  const { value, res: bad } = await readJson(request, LIMIT.SMALL_BYTES); if (bad) return bad;
  const want = value && value.slots;
  if (!Array.isArray(want) || want.length < 1 || want.length > PARTY_MAX) return fail(400, `slots 는 1~${PARTY_MAX}개`);
  for (const w of want) {
    const byId = w && w.id !== undefined, byN = w && typeof w.hero === "string", byTest = w && w.test !== undefined;
    if ((byId ? 1 : 0) + (byN ? 1 : 0) + (byTest ? 1 : 0) !== 1 || (byId && !isInt(w.id, 1, Number.MAX_SAFE_INTEGER)) || (byN && (!ID.test(w.hero) || !isInt(w.n, 0, 999999)))
      || (byTest && !isInt(w.test, 1, Number.MAX_SAFE_INTEGER))) return fail(400, "bad slot ref");
  }
  const tester = me.user.role === "tester";
  if (!tester && want.some((w) => w.test !== undefined)) return fail(403, "tester only", { why: "시험 칸은 시험 계정만" });
  const season = await seasonOf(env, null);
  const t = now();
  if (!season || (season.ends && season.ends < t) || season.starts > t) return fail(409, "열린 시즌이 없다");
  const rows = [], seen = new Set();
  for (const w of want) {
    const r = w.test !== undefined
      ? await env.DB.prepare("SELECT id, hero, NULL AS n, json, hash, 'test' AS status, NULL AS why, 1 AS test FROM test_slots WHERE user_id = ?1 AND id = ?2").bind(me.user.id, w.test).first()
      : w.id !== undefined
      ? await env.DB.prepare("SELECT id, hero, n, json, hash, status, why FROM hero_slots WHERE user_id = ?1 AND id = ?2").bind(me.user.id, w.id).first()
      : await env.DB.prepare("SELECT id, hero, n, json, hash, status, why FROM hero_slots WHERE user_id = ?1 AND hero = ?2 AND n = ?3").bind(me.user.id, w.hero.normalize("NFC"), w.n).first();
    if (!r) return fail(404, "no slot", w.test !== undefined ? { test: w.test } : w.id !== undefined ? { id: w.id } : { hero: w.hero, n: w.n });
    if (seen.has(r.hero)) return fail(400, "같은 사도 둘은 한 파티에 넣을 수 없다", { hero: r.hero });
    seen.add(r.hero);
    if (r.status === "bad") return fail(409, "규칙 검사에서 거절된 슬롯", { id: r.id, hero: r.hero, n: r.n, why: r.why });
    rows.push(r);
  }
  const day = yyyymmdd(t);
  const id = randomHex(16), seed = randomSeed(), heroes = rows.map((r) => r.hero), expires = t + LIMIT.ATTEMPT_SEC;
  const prog = progOn(env) ? snapshot(await loadHeroes(env, me.user.id), heroes) : null;
  // slot_ids — 일반 칸은 id 수, 시험 칸은 「t<id>」 글(두 표의 id 가 겹쳐도 갈리게 — 기록용, 읽는 곳 없음)
  //   prog_json 열(0010)은 떠 둔 잠재력이 있을 때만 쓴다 — 성장 꺼짐 · 0010 전 D1(공개판 2026-10-22)에도 시작이 된다
  const bind = [id, me.user.id, season.id, seed, JSON.stringify(rows.map((r) => (r.test ? "t" + r.id : r.id))), JSON.stringify(heroes), JSON.stringify(rows.map((r) => r.json)), JSON.stringify(rows.map((r) => r.hash)), day, t, expires, tester ? 1 : 0];
  if (prog) bind.push(JSON.stringify(prog));
  await env.DB.prepare(`INSERT INTO attempts (id, user_id, season_id, seed, slot_ids, heroes, slots_json, slot_hashes, day, started, expires, test${prog ? ", prog_json" : ""}) VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12${prog ? ", ?13" : ""})`)
    .bind(...bind).run();
  return json({
    attempt: id, seed, season: publicSeason(season), heroes, expires, left: null, test: tester, ...(prog ? { prog } : {}),
    slots: rows.map((r) => (r.test ? { test: r.id, hero: r.hero, slot: JSON.parse(r.json) } : { id: r.id, hero: r.hero, n: r.n, status: r.status, slot: JSON.parse(r.json) })),
  });
}

export const onRequest = () => fail(405, "method not allowed");
