-- 프리시즌(판 전체 검증 전 시범 시즌 — 2026-10-18 사용자) — 보스 M.E.O.W · 10턴 · 배율 1(엔진 Frontier.PRESEASON 과 같은 값).
-- rules · content 는 비워 둔다(검증기의 지금 엔진 · 콘텐츠로 검증). 정식 시즌부터는 채워 고정한다(Docs/프론티어_서버.md §시즌).
INSERT INTO seasons (id, label, boss, turns, hp, dmg, preseason, rules, content, starts, ends, active)
VALUES ('preseason', '프리시즌', 'frontier_meow', 10, 1, 1, 1, NULL, NULL, 1792281600, NULL, 1);
