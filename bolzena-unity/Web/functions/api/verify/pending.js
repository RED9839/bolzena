// GET /api/verify/pending[?slotAfter=id&scoreAfter=id&limit=20] — 검증기 전용(관리자 토큰). 검증 대기 슬롯 · 점수를 오래된 차례로.
// 검증기는 건너뛴 것(버전이 달라 미룬 것) 뒤를 보려고 slotAfter · scoreAfter 로 넘겨 가며 빌 때까지 받는다.
// 시험 점수(test = 1 — migrations/0009)는 주지 않는다(늘 skipped 라 pending 이 아니지만 운영 조치 등으로 바뀌어도 검증하지 않는다 — 시험 칸은 규칙 검사 밖일 수 있다).
// → { slots: [{ id, hero, hash, json }], scores: [{ id, attempt, seed, season(지문 포함), heroes, slots(시작 때 떠 둔 HeroSlot 글들), prog(시작 때 떠 둔 잠재력 — 없으면 null, migrations/0010), claimed, tape(글) }] }
import { requireVerifier } from "../../../lib/verifier.js";
import { json, fail } from "../../../lib/util.js";
import { seasonOf, verifierSeason } from "../../../lib/season.js";
import { progTables } from "../../../lib/prog.js";

export async function onRequestGet({ request, env }) {
  const bad = await requireVerifier(request, env); if (bad) return bad;
  const q = new URL(request.url).searchParams;
  const slotAfter = Number(q.get("slotAfter")) || 0, scoreAfter = Number(q.get("scoreAfter")) || 0;
  const limit = Math.min(50, Math.max(1, Number(q.get("limit")) || 20));
  const slots = (await env.DB.prepare("SELECT id, hero, hash, json FROM hero_slots WHERE status = 'pending' AND id > ?1 ORDER BY id LIMIT ?2").bind(slotAfter, limit).all()).results;
  const progCol = (await progTables(env)) ? "a.prog_json" : "NULL AS prog_json";   // 0010 전 D1 엔 열이 없다
  const rows = (await env.DB.prepare(`SELECT s.id, s.attempt_id, s.season_id, s.heroes, s.claimed, s.tape, a.seed, a.slots_json, ${progCol}
    FROM scores s JOIN attempts a ON a.id = s.attempt_id WHERE s.status = 'pending' AND s.test = 0 AND s.id > ?1 ORDER BY s.id LIMIT ?2`).bind(scoreAfter, limit).all()).results;
  const seasons = {};
  for (const r of rows) if (!(r.season_id in seasons)) { const s = await seasonOf(env, r.season_id); seasons[r.season_id] = s ? verifierSeason(s) : null; }
  return json({
    slots,
    scores: rows.map((r) => ({ id: r.id, attempt: r.attempt_id, seed: r.seed, season: seasons[r.season_id], heroes: JSON.parse(r.heroes), slots: JSON.parse(r.slots_json), prog: r.prog_json ? JSON.parse(r.prog_json) : null, claimed: r.claimed, tape: r.tape })),
  });
}

export const onRequest = () => fail(405, "method not allowed");
