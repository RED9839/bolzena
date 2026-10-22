-- 사도 슬롯 칸 수 한도 없앰(2026-10-18 사용자 「사도들 덱 저장 최대 개수 없애 줘」) — 사도마다 칸 0~2 → 「새로 저장」 할 때마다 서버가 id 를 준다.
-- 0001 의 CHECK (n BETWEEN 0 AND 2) 는 ALTER 로 못 풀어서 표를 다시 짓는다. 기존 줄은 id · n · 글 · 지문 · 상태 · 시각 그대로 옮긴다
--   (attempts.slot_ids 가 이 id 를 적어 두었고 · 칸별 최고는 hash 로 견주니 둘 다 바뀌면 안 된다). hero_slots 를 가리키는 외래 키는 없다.
-- n = 그 사용자 · 사도의 저장 차례 번호(0부터 — 새로 저장은 지금 가장 큰 n + 1). 이름이 없을 때 화면이 「〇〇 저장 n+1」 로 쓴다 · 옛 주소 /api/slots/:hero/:n 의 열쇠.
-- name = 칸 이름(선택 · 16자 · 닉네임과 같은 글자 규칙 — lib/slot.js). 주인만 본다.
CREATE TABLE hero_slots_new (
  id      INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  hero    TEXT    NOT NULL,
  n       INTEGER NOT NULL CHECK (n >= 0),
  name    TEXT,
  json    TEXT    NOT NULL,
  hash    TEXT    NOT NULL,
  status  TEXT    NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'ok', 'bad')),
  why     TEXT,
  created INTEGER NOT NULL,
  updated INTEGER NOT NULL,
  checked INTEGER,
  UNIQUE (user_id, hero, n)
);
INSERT INTO hero_slots_new (id, user_id, hero, n, name, json, hash, status, why, created, updated, checked)
  SELECT id, user_id, hero, n, NULL, json, hash, status, why, created, updated, checked FROM hero_slots;
DROP TABLE hero_slots;
ALTER TABLE hero_slots_new RENAME TO hero_slots;
CREATE INDEX hero_slots_status ON hero_slots(status, id);
CREATE INDEX hero_slots_user ON hero_slots(user_id, updated DESC);
