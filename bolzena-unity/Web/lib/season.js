// 시즌 읽기 — seasons 표(migrations/0002_preseason.sql). 엔진 FrontierSeason(Id · Boss · Turns · Hp · Dmg)과 같은 값.
export const SEASON_ID = /^[a-z0-9_-]{1,32}$/;

/** 그 시즌, 또는 id 가 없으면 지금 시즌(active 가운데 가장 늦게 시작한 것). */
export function seasonOf(env, id) {
  const cols = "id, label, boss, turns, hp, dmg, preseason, rules, content, starts, ends, active, score_max, turn_max";   // score_max · turn_max = 제출 상한(migrations/0008)
  return (id
    ? env.DB.prepare(`SELECT ${cols} FROM seasons WHERE id = ?1`).bind(id)
    : env.DB.prepare(`SELECT ${cols} FROM seasons WHERE active = 1 ORDER BY starts DESC LIMIT 1`)).first();
}

/** 밖에 보이는 시즌 — preseason 이면 화면이 「프리시즌」 으로 표시한다(판 전체 검증 전 시범 시즌). */
export const publicSeason = (s) => ({
  id: s.id, label: s.label, boss: s.boss, turns: s.turns, hp: s.hp, dmg: s.dmg,
  preseason: !!s.preseason, starts: s.starts, ends: s.ends, active: !!s.active,
});

/** 검증기에 주는 시즌 — 지문(rules · content)까지. */
export const verifierSeason = (s) => ({ ...publicSeason(s), rules: s.rules, content: s.content });
