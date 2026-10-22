// GET /api/frontier/season[?id=] — 지금 시즌(active) 또는 그 시즌. 로그인 없이.
// → { season: { id, label, boss, turns, hp, dmg, preseason, starts, ends, active } }
import { json, fail, memLimited } from "../../../lib/util.js";
import { seasonOf, publicSeason, SEASON_ID } from "../../../lib/season.js";

export async function onRequestGet({ request, env }) {
  if (!env.DB) return fail(503, "no db");
  const lim = memLimited("season|" + (request.headers.get("cf-connecting-ip") || "?"), 120); if (lim) return lim;
  const id = new URL(request.url).searchParams.get("id");
  if (id !== null && !SEASON_ID.test(id)) return fail(400, "bad season");
  const s = await seasonOf(env, id);
  return s ? json({ season: publicSeason(s) }) : fail(404, "no season");
}

export const onRequest = () => fail(405, "method not allowed");
