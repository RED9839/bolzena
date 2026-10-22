-- 회원가입식 닉네임(2026-10-18 사용자) — 첫 로그인 때 사용자 줄만 만들고 닉네임은 비워 둔다. 닉네임을 정해야 가입이 끝난다(/api/me/name).
-- nick = 보이는 이름(NFKC · 앞뒤 공백 정리) · nick_key = 겹침 검사 열쇠(NFKC · 소문자 · 공백과 _ - . 뺌 — lib/nick.js) · nick_changed = 마지막으로 정하거나 바꾼 때.
-- 옛 name 열(0001 — 자동 이름 「교주1234」)은 더 쓰지 않는다. 기존 줄은 이름을 비워 모두 닉네임을 새로 정하게 한다(NOT NULL 이라 빈 글로).
ALTER TABLE users ADD COLUMN nick TEXT;
ALTER TABLE users ADD COLUMN nick_key TEXT;
ALTER TABLE users ADD COLUMN nick_changed INTEGER;
CREATE UNIQUE INDEX users_nick_key ON users(nick_key);
UPDATE users SET name = '', nick = NULL, nick_key = NULL, nick_changed = NULL;
