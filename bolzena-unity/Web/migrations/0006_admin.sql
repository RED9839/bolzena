-- 운영 도구(2026-10-18 사용자 「운영자가 다른 유저 데이터를 다룰 도구 = 명령으로」) — 운영 기록 · 계정 정지 · 랭킹 숨김.
-- 운영 주소는 /api/admin/…(ADMIN_TOKEN 시크릿 Bearer 만) · 이 PC 명령 bz admin. 설명: ../../Docs/프론티어_서버.md §운영

-- 정지 — banned_until = 풀리는 때(유닉스 초). NULL 이면 정지 아님 · 지난 때면 저절로 풀린 것 · 32503680000(3000-01-01) 이상이면 영구(lib/auth.js BAN_FOREVER).
-- ban_reason = 정지 까닭(정지된 사람의 쓰기 요청 403 응답에 그대로 나간다 — 본인에게 보여 줄 글로 쓴다).
ALTER TABLE users ADD COLUMN banned_until INTEGER;
ALTER TABLE users ADD COLUMN ban_reason TEXT;

-- 랭킹 숨김 — 그 사용자를 그 시즌(season_id) 랭킹 · 남의 편성 보기에서 뺀다. season_id '*' = 모든 시즌. 점수 줄은 그대로 둔다(되돌리면 다시 보인다).
CREATE TABLE rank_hidden (
  user_id   INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  season_id TEXT    NOT NULL,
  reason    TEXT    NOT NULL,
  at        INTEGER NOT NULL,
  PRIMARY KEY (user_id, season_id)
);

-- 운영 기록 — 누가(actor — 토큰 이름, 예 「admin」) · 언제(at) · 무엇을(action) · 대상(user_id · target) · 까닭(reason) · 바뀐 것(detail JSON — 바꾸기 전 값 · 지운 수).
-- user_id 는 외래 키가 아니다 — 대상이 탈퇴하거나 지워져도 기록은 남는다(남용 대응 기록). OAuth 번호 · 세션 · IP 는 적지 않는다.
CREATE TABLE admin_log (
  id      INTEGER PRIMARY KEY AUTOINCREMENT,
  at      INTEGER NOT NULL,
  actor   TEXT    NOT NULL,
  action  TEXT    NOT NULL,
  user_id INTEGER,
  target  TEXT,
  reason  TEXT    NOT NULL,
  detail  TEXT
);
CREATE INDEX admin_log_user ON admin_log(user_id, id DESC);
