-- 시험 계정 권한(2026-10-20 사용자 「사도 카드 원하는 덱 만들어서 프론티어 테스트」 B) — 역할 · 시험 칸 · 시험 시도/점수 표시. 설명: ../../Docs/프론티어_서버.md §시험 계정
-- 열만 더하고 새 표를 짓는다 — 기존 줄 · UNIQUE 와 부딪힐 것이 없다(표를 다시 짓지 않는다).

-- 역할 — NULL = 일반 · 'tester' = 시험 계정. 부여 · 회수는 운영 조치(POST /api/admin/act { action: "role" } · bz admin role)로만 — 운영 기록에 남는다.
--   서버만 판단한다(게임이 보내는 값은 믿지 않는다 — lib/auth.js current 가 이 열을 읽는다).
--   tester 의 도전 · 점수는 랭킹 · 남의 편성 보기 · 내 순위에서 빠진다(lib/auth.js VISIBLE — 숨김과 같은 자리).
ALTER TABLE users ADD COLUMN role TEXT CHECK (role IS NULL OR role IN ('tester'));

-- 시험 시도 · 점수 — tester 가 연 시도는 모두 test = 1(시험 칸이든 일반 칸이든). 회수한 뒤에도 그 줄은 계속 빠진다(역할이 아니라 줄에 적힌 표시라서).
--   시험 점수는 status 'skipped'(검증 안 함)로 들어가고 검증기가 가져가지 않는다 · 다음 후보로 올리지도 않는다(lib/score.js). 본인 기록에는 「시험」 으로 남는다.
ALTER TABLE attempts ADD COLUMN test INTEGER NOT NULL DEFAULT 0 CHECK (test IN (0, 1));
ALTER TABLE scores ADD COLUMN test INTEGER NOT NULL DEFAULT 0 CHECK (test IN (0, 1));

-- 시험 칸 — 일반 저장 칸(hero_slots)과 따로(사도당 10칸 한도와 별개 · 검증기 규칙 검사 없음 · 「시험」 표시). 크기 · 형식 검사는 일반 칸과 같다(lib/slot.js shape).
--   주인만 본다(목록 · 덮어쓰기 · 지우기 — /api/test/slots · /api/test/slot/:id, tester 만 · 아니면 403). 계정당 LIMIT.TEST_SLOTS_PER_USER 칸.
--   회수하면 칸은 남지만(다시 주면 그대로) 주소가 403 이 된다. 탈퇴하면 같이 지운다(me.js).
CREATE TABLE test_slots (
  id      INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  hero    TEXT    NOT NULL,
  name    TEXT,
  json    TEXT    NOT NULL,
  hash    TEXT    NOT NULL,
  created INTEGER NOT NULL,
  updated INTEGER NOT NULL
);
CREATE INDEX test_slots_user ON test_slots(user_id, updated DESC);
