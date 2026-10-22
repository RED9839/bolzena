-- 프론티어 3단계 — 계정 · 사도 슬롯 · 시즌 · 시도 · 점수(D1, 바인딩 DB). 설명: ../../Docs/프론티어_서버.md
-- 시각은 전부 유닉스 초(UTC). 사람을 알아볼 것은 받지 않는다 — 공급자 사용자 번호도 ID_PEPPER 로 HMAC 한 값만 둔다.

-- 사용자 — 공급자(discord · google · test) + 공급자 번호의 HMAC. 화면 이름은 사용자가 정한다(공급자 닉네임을 가져오지 않는다).
CREATE TABLE users (
  id           INTEGER PRIMARY KEY AUTOINCREMENT,
  provider     TEXT    NOT NULL CHECK (provider IN ('discord', 'google', 'test')),
  provider_sub TEXT    NOT NULL,
  name         TEXT    NOT NULL,
  created      INTEGER NOT NULL,
  UNIQUE (provider, provider_sub)
);

-- 세션 — 무작위 32바이트 토큰의 SHA-256 만 둔다. web = HttpOnly 쿠키, pc = 기기 코드로 받은 Bearer 토큰.
CREATE TABLE sessions (
  token_hash TEXT    PRIMARY KEY,
  user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  kind       TEXT    NOT NULL CHECK (kind IN ('web', 'pc')),
  created    INTEGER NOT NULL,
  expires    INTEGER NOT NULL
);
CREATE INDEX sessions_user ON sessions(user_id);
CREATE INDEX sessions_expires ON sessions(expires);

-- PC 기기 코드 로그인 — 게임이 받은 긴 기기 코드(해시만) · 사람이 웹에 넣는 짧은 코드 · 승인한 사용자.
CREATE TABLE device_codes (
  device_hash TEXT    PRIMARY KEY,
  user_code   TEXT    NOT NULL UNIQUE,
  user_id     INTEGER REFERENCES users(id) ON DELETE CASCADE,
  status      TEXT    NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'approved', 'denied')),
  created     INTEGER NOT NULL,
  expires     INTEGER NOT NULL,
  last_poll   INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX device_codes_expires ON device_codes(expires);

-- 사도 슬롯 — 사용자 · 사도마다 칸 0~2. json 은 받은 HeroSlot 글 그대로, hash 는 그 글의 SHA-256 앞 16자리(덮어쓰면 바뀐다).
-- status: pending(검증기가 아직 안 봄) · ok(HeroSlot.Check 통과) · bad(규칙 위반 — why 에 까닭).
CREATE TABLE hero_slots (
  id      INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  hero    TEXT    NOT NULL,
  n       INTEGER NOT NULL CHECK (n BETWEEN 0 AND 2),
  json    TEXT    NOT NULL,
  hash    TEXT    NOT NULL,
  status  TEXT    NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'ok', 'bad')),
  why     TEXT,
  created INTEGER NOT NULL,
  updated INTEGER NOT NULL,
  checked INTEGER,
  UNIQUE (user_id, hero, n)
);
CREATE INDEX hero_slots_status ON hero_slots(status, id);

-- 시즌 — 보스 · 턴 제한 · 적 배율(엔진 FrontierSeason 과 같은 값). preseason 1 = 판 전체 검증 전 시범 시즌(「프리시즌」 표시).
-- rules · content 를 채우면 검증기가 그 지문의 엔진 · 콘텐츠로만 검증한다(비우면 검증기의 지금 것).
CREATE TABLE seasons (
  id        TEXT    PRIMARY KEY,
  label     TEXT    NOT NULL,
  boss      TEXT    NOT NULL,
  turns     INTEGER NOT NULL,
  hp        REAL    NOT NULL DEFAULT 1,
  dmg       REAL    NOT NULL DEFAULT 1,
  preseason INTEGER NOT NULL DEFAULT 1,
  rules     TEXT,
  content   TEXT,
  starts    INTEGER NOT NULL,
  ends      INTEGER,
  active    INTEGER NOT NULL DEFAULT 0
);

-- 프론티어 시도 — 서버가 준 씨앗 · 편성 슬롯(시작 때 슬롯 글을 그대로 떠 둔다 — 그 뒤 덮어써도 이 시도는 그 슬롯으로 검증) · 만료.
CREATE TABLE attempts (
  id         TEXT    PRIMARY KEY,
  user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  season_id  TEXT    NOT NULL REFERENCES seasons(id),
  seed       INTEGER NOT NULL,
  slot_ids   TEXT    NOT NULL,
  heroes     TEXT    NOT NULL,
  slots_json TEXT    NOT NULL,
  day        TEXT    NOT NULL,
  started    INTEGER NOT NULL,
  expires    INTEGER NOT NULL,
  submitted  INTEGER NOT NULL DEFAULT 0
);
CREATE INDEX attempts_user_day ON attempts(user_id, day);

-- 점수 제출 — 시도 하나에 하나. claimed = 클라이언트가 주장한 점수, score = 검증기가 다시 돌려 얻은 점수(verified 때만).
-- status: pending(검증 대기) · verified · rejected(why · turn — 어긋난 턴 지문 번호).
CREATE TABLE scores (
  id         INTEGER PRIMARY KEY AUTOINCREMENT,
  attempt_id TEXT    NOT NULL UNIQUE REFERENCES attempts(id) ON DELETE CASCADE,
  user_id    INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  season_id  TEXT    NOT NULL REFERENCES seasons(id),
  heroes     TEXT    NOT NULL,
  claimed    INTEGER NOT NULL,
  score      INTEGER,
  tape       TEXT    NOT NULL,
  status     TEXT    NOT NULL DEFAULT 'pending' CHECK (status IN ('pending', 'verified', 'rejected')),
  why        TEXT,
  turn       INTEGER,
  submitted  INTEGER NOT NULL,
  verified   INTEGER
);
CREATE INDEX scores_rank ON scores(season_id, status, score DESC, submitted);
CREATE INDEX scores_status ON scores(status, id);
CREATE INDEX scores_user ON scores(user_id, submitted DESC);

-- 속도 제한 — 「무엇|누구」 열쇠 · 창 번호마다 횟수. IP 는 해시 앞자리만 열쇠에 넣고, 창이 지나면 지운다.
CREATE TABLE rate (
  k     TEXT    NOT NULL,
  win   INTEGER NOT NULL,
  n     INTEGER NOT NULL,
  until INTEGER NOT NULL,
  PRIMARY KEY (k, win)
);
CREATE INDEX rate_until ON rate(until);
