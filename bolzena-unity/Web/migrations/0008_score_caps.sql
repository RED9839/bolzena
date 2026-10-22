-- 제출 상한(2026-10-19 사용자 「중간 방법 2 — 서버 상한 검사」) — 검증기를 기다리지 않고 제출 때 바로 거절할 선. 설명: ../../Docs/프론티어_서버.md §제출 상한
-- 점수는 검증 전에도 랭킹에 「검증 전」 으로 오르니(중간 방법 1), 말이 안 되는 점수는 받는 순간 막는다.
--   score_max = 점수 상한 = 시즌 보스의 실제 최대 HP(보스 데이터 hp × seasons.hp). 점수는 보스가 잃은 HP 합이라 회복이 없는 보스면 이 값을 넘을 수 없다.
--   turn_max  = 한 턴 피해 상한 — 점수 > 낸 턴 × turn_max 이면 거절. 봇 측정 최대치의 약 3배(근거는 문서 §제출 상한).
--   NULL 이면 그 검사를 하지 않는다(새 시즌을 넣을 때 채운다 — 회복하는 보스면 score_max 를 비우거나 넉넉히).
-- 앞 절반은 열만 더한다 — 기존 줄 · UNIQUE 와 부딪힐 것이 없다. 뒤 절반(scores 다시 짓기)은 줄을 그대로 옮긴다.
ALTER TABLE seasons ADD COLUMN score_max INTEGER;
ALTER TABLE seasons ADD COLUMN turn_max INTEGER;
-- 프리시즌 M.E.O.W — 보스 HP 150,000(bolzena-content-v2 world/프론티어/프리시즌.json · 배율 1) · 한 턴 상한 90,000
--   (bz frontier 봇 3,800판 — 다듬은 슬롯 · smart/simple — 한 턴 최대 29,972 · 상위 1% 17,132 → 약 3배. 2026-10-19 콘텐츠 2ac9cf941e516452).
UPDATE seasons SET score_max = 150000, turn_max = 90000 WHERE id = 'preseason';

-- 검증은 자기 최고 기록 후보만(2026-10-19 사용자) — 점수 상태에 skipped(「검증 안 함」)를 더한다. 설명: ../../Docs/프론티어_서버.md §검증 대상
--   낼 때 그 사람(그 시즌)의 지금 최고(verified · pending 가운데 가장 높은 것)보다 높을 때만 pending(검증 대기열), 아니면 skipped — 검증기가 가져가지 않는다.
--   최고 후보가 rejected 되면 가장 높은 skipped 하나를(남은 최고보다 높으면) pending 으로 올린다(lib/score.js promoteNext).
-- 0001 의 CHECK (status IN ('pending', 'verified', 'rejected')) 는 ALTER 로 못 풀어서 표를 다시 짓는다(0005 와 같은 방법). 기존 줄은 id 까지 그대로 옮긴다.
-- scores 를 가리키는 외래 키는 없다.
CREATE TABLE scores_new (
  id         INTEGER PRIMARY KEY AUTOINCREMENT,
  attempt_id TEXT    NOT NULL UNIQUE REFERENCES attempts(id) ON DELETE CASCADE,
  user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  season_id  TEXT    NOT NULL REFERENCES seasons(id),
  heroes     TEXT    NOT NULL,
  claimed    INTEGER NOT NULL,
  score      INTEGER,
  tape       TEXT    NOT NULL,
  status     TEXT    NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'verified', 'rejected', 'skipped')),
  why        TEXT,
  turn       INTEGER,
  submitted  INTEGER NOT NULL,
  verified   INTEGER
);
INSERT INTO scores_new (id, attempt_id, user_id, season_id, heroes, claimed, score, tape, status, why, turn, submitted, verified)
  SELECT id, attempt_id, user_id, season_id, heroes, claimed, score, tape, status, why, turn, submitted, verified FROM scores;
DROP TABLE scores;
ALTER TABLE scores_new RENAME TO scores;
CREATE INDEX scores_rank ON scores(season_id, status, score DESC, submitted);
CREATE INDEX scores_status ON scores(status, id);
CREATE INDEX scores_user ON scores(user_id, submitted DESC);
CREATE INDEX scores_user_season ON scores(user_id, season_id, status, claimed DESC);
