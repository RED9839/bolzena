// POST /api/prog/earn { ref, tier, result, floor, min, party: [사도 1~3] } — 모험 끝 보상(잠재력 재료 = 마시멜로) 받기. 게임은 판 결과만 보내고 보상은 서버가 계산한다(lib/prog.js rewardOf).
//   ref = 판 열쇠(게임이 씨앗 · 편성으로 짓는다 — 같은 판을 두 번 받지 못한다: 두 번째는 200 { dup: true } 에 받은 것 없이)
//   tier = 고른 난이도 단계 1~9 — 이 계정이 연 단계(최고 완주 + 1)까지만(넘으면 409 tier locked) — 도전 사다리
//   result = win(완주) · lose(쓰러짐) · abandon(포기) · floor = 멈춘 층 1~2 · min = 걸린 분(완주인데 3분 미만이면 400 too fast — 모양 검사)
//   완주면 최고 완주 단계를 올린다(다음 단계가 열린다).
//   보상 = 판마다 마시멜로 하급 · 중급 · 상급(단계 표 MATS × 진행 비율 — 쓰러짐 · 포기는 약 1/4) — 행동력 없이 판마다(lib/prog.js runMats).
//   판 전체 검증 전이라 판 결과는 지어낼 수 있다 → **주간 상한이 위조 이득의 상한**(Docs/성장_설계_카제나판.md §12.2). 분당 6번.
//   스위치 꺼짐 404 prog off · 로그인 안 함 401 · 가입 전 403 needsName · 정지 403 banned.
//   → 200 { dup, reward: { progress, want, give: { items }, capped }, bestTier, opened(새로 열린 단계 | null), prog(GET /api/prog 와 같은 꼴) }
import { requireUser } from "../../../lib/auth.js";
import { LIMIT, isInt, now, readJson, limited, json, fail } from "../../../lib/util.js";
import { progOn, loadAccount, loadHeroes, view, rewardOf, save, tierOpen, HEROES, TIER_MAX, MIN_CLEAR_MIN, KEYS } from "../../../lib/prog.js";

const RESULTS = new Set(["win", "lose", "abandon"]);
const REF = /^[0-9A-Za-z_.:-]{8,80}$/;

export async function onRequestPost({ request, env }) {
  if (!progOn(env)) return fail(404, "prog off");
  const { me, res } = await requireUser(request, env, true, true); if (res) return res;
  const lim = await limited(env, "earn|" + me.user.id, 6, 60); if (lim) return lim;
  const { value: v, res: bad } = await readJson(request, LIMIT.SMALL_BYTES); if (bad) return bad;
  if (!v || typeof v !== "object") return fail(400, "bad body");
  if (typeof v.ref !== "string" || !REF.test(v.ref)) return fail(400, "bad ref");
  if (!isInt(v.tier, 1, TIER_MAX)) return fail(400, "bad tier");
  if (!RESULTS.has(v.result)) return fail(400, "bad result");
  if (!isInt(v.floor, 1, 2)) return fail(400, "bad floor");
  if (!isInt(v.min, 0, 24 * 60)) return fail(400, "bad min");
  if (v.result === "win" && v.min < MIN_CLEAR_MIN) return fail(400, "too fast", { why: `${MIN_CLEAR_MIN}분보다 빠른 완주는 보상을 받지 않는다` });
  if (!Array.isArray(v.party) || v.party.length < 1 || v.party.length > 3) return fail(400, "bad party");
  const party = v.party.map((h) => (typeof h === "string" ? h.normalize("NFC") : ""));
  if (party.some((h) => !Object.prototype.hasOwnProperty.call(HEROES, h)) || new Set(party).size !== party.length) return fail(400, "bad party", { why: "모르는 사도이거나 같은 사도 둘" });
  const uid = me.user.id, t = now();

  // 같은 판 두 번 막기 — 먼저 적은 쪽만(동시에 두 번 보내도 하나만)
  const claim = await env.DB.prepare("INSERT INTO prog_earn_log (user_id, ref, tier, result, floor, heroes, amount, at) VALUES (?1, ?2, ?3, ?4, ?5, ?6, '{}', ?7) ON CONFLICT (user_id, ref) DO NOTHING RETURNING id")
    .bind(uid, v.ref, v.tier, v.result, v.floor, JSON.stringify(party), t).first();
  if (!claim) {
    const [acc, heroes] = await Promise.all([loadAccount(env, uid, t), loadHeroes(env, uid)]);
    return json({ dup: true, reward: null, prog: view(acc, heroes) });
  }

  for (let tries = 0; tries < 4; tries++) {
    const [acc, heroes] = await Promise.all([loadAccount(env, uid, t), loadHeroes(env, uid)]);
    if (v.tier > tierOpen(acc.bestTier)) {
      await env.DB.prepare("DELETE FROM prog_earn_log WHERE id = ?1").bind(claim.id).run();
      return fail(409, "tier locked", { why: `${v.tier}단계는 아직 잠겨 있다 — ${v.tier - 1}단계를 완주하면 열린다`, open: tierOpen(acc.bestTier) });
    }
    const r = rewardOf(acc, { tier: v.tier, result: v.result, floor: v.floor, party });
    const bestBefore = acc.bestTier;
    for (const [k, n] of Object.entries(r.give.items)) acc.bag[k] = (acc.bag[k] | 0) + n;
    for (const k of KEYS) acc.got[k] = (acc.got[k] | 0) + r.got[k];
    if (v.result === "win" && v.tier > acc.bestTier) acc.bestTier = v.tier;
    acc.runs++;
    if (!(await save(env, uid, acc, heroes, [], t))) continue;   // 다른 요청이 먼저 썼다 — 다시 읽고 다시 셈
    const reward = { progress: r.progress, want: r.want, give: r.give, capped: r.capped };
    await env.DB.prepare("UPDATE prog_earn_log SET amount = ?2 WHERE id = ?1").bind(claim.id, JSON.stringify(reward)).run();
    return json({
      dup: false, reward, bestTier: acc.bestTier,
      opened: acc.bestTier > bestBefore && tierOpen(acc.bestTier) > tierOpen(bestBefore) ? tierOpen(acc.bestTier) : null,
      prog: view(acc, heroes),
    });
  }
  await env.DB.prepare("DELETE FROM prog_earn_log WHERE id = ?1").bind(claim.id).run();
  return fail(409, "busy", { why: "다른 기기가 동시에 바꾸는 중 — 잠시 뒤에 다시" });
}

export const onRequest = () => fail(405, "method not allowed");
