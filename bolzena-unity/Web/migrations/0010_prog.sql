-- 사도 성장 — 잠재력 하나만 + 도전 사다리(난이도 단계) 기록(2026-10-21 밤 사용자 최종 결정 「판 밖 성장 = 잠재력만」).
-- 설계: ../../Docs/성장_설계_카제나판.md 맨 위 결정 상자 · §12. 주소: functions/api/prog/*(GET /api/prog · POST earn · potential) · 계산 · 값 표: lib/prog.js.
-- **스위치**: 서버 환경 변수 PROG = "1" 일 때만 주소가 열린다(아니면 404 prog off) — 표만 먼저 만들어 두어도 공개판은 지금과 같다.
-- 성장 값은 서버가 주인이다 — 게임은 「판 결과」 만 보내고 재료 보상 · 잠재력 올리기는 서버가 계산한다(판 전체 검증 전이라 주간 상한이 위조 이득의 상한).
--
-- 2026-10-21 밤 이 파일을 다시 썼다: 예전 0010(레벨 · 진급 · 교주 레벨 · 왕사탕 · 모카롱 · 교본 · 골드) · 0011(던전) · 0012(잠재력 기록)를
--   잠재력만의 이 한 파일로 합쳤다. 셋 다 원격(bolzena-frontier)에 아직 넣지 않은 파일이었다(원격은 0009 까지 — 메모 bolzena-frontier-accounts 10-20 배포 3635a94b).
--   **이미 옛 0010~0012 를 넣은 D1(로컬 시험 상태 등)은 지우고 다시 만들 것** — 이름이 같은 0010 이라 다시 넣지 않는다(test/run-local.mjs 는 늘 새로 만든다,
--   C:\projects\bolzena-play-prog 는 성장_시험_처음부터.cmd 또는 launch.mjs 가 옛 표를 알아보고 새로 만든다).

-- 계정 — 사용자당 한 줄(처음 쓸 때 만든다).
--   best_tier = 완주한 가장 높은 난이도 단계(0 = 없음 — 고를 수 있는 단계 = best_tier + 1, 엔진 Tiers.Open).
--   bag = 잠재력 재료 JSON {"mm1", "mm2", "mm3": 수}(마시멜로 하급 · 중급 · 상급 — 2026-10-22, 열쇠 · 이름 표는 lib/prog.js MM 한 곳).
--   week = 주 번호(월요일 0시 UTC 기준 — lib/prog.js weekOf) · week_got = 이번 주 받은 양 JSON {mm1, mm2, mm3}(주간 상한 셈).
--   runs = 보상을 받은 판 수 · rev = 바뀔 때마다 +1(화면 맞추기 · 낙관적 잠금).
CREATE TABLE prog_account (
  user_id   INTEGER PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
  best_tier INTEGER NOT NULL DEFAULT 0 CHECK (best_tier >= 0),
  bag       TEXT    NOT NULL DEFAULT '{}',
  week      INTEGER NOT NULL DEFAULT 0,
  week_got  TEXT    NOT NULL DEFAULT '{}',
  runs      INTEGER NOT NULL DEFAULT 0,
  rev       INTEGER NOT NULL DEFAULT 1,
  tx        TEXT,
  updated   INTEGER NOT NULL
);
-- tx = 마지막으로 쓴 요청의 무작위 표(한 batch 안에서 사도 줄 · 기록 줄 쓰기를 「계정 줄을 내가 바꿨을 때만」 으로 묶는다 — lib/prog.js save).

-- 사도별 잠재력 — 줄이 없으면 잠재력 없음. pot = 잠재력 칸 JSON {"01": 1, "02": 5, …}(아는 칸 · 1 이상만 — 없으면 NULL).
--   프론티어 시도를 열 때 lib/prog.js snapshot 이 떠 둔다.
CREATE TABLE prog_hero (
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  hero    TEXT    NOT NULL,
  pot     TEXT,
  updated INTEGER NOT NULL,
  PRIMARY KEY (user_id, hero)
);

-- 받은 판 보상 — 같은 판을 두 번 받지 못하게(ref = 판 씨앗 · 편성에서 게임이 짓는 열쇠). amount = 실제로 준 양(상한 자른 뒤) JSON.
CREATE TABLE prog_earn_log (
  id      INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  ref     TEXT    NOT NULL,
  tier    INTEGER NOT NULL,
  result  TEXT    NOT NULL,
  floor   INTEGER NOT NULL,
  heroes  TEXT    NOT NULL,
  amount  TEXT    NOT NULL,
  at      INTEGER NOT NULL,
  UNIQUE (user_id, ref)
);
CREATE INDEX prog_earn_user ON prog_earn_log(user_id, at DESC);

-- 잠재력 올린 기록 — 한 번(한 단계)에 한 줄. lv = 올린 뒤 단계, cost = 쓴 재료 JSON({"mm2": 4, …}).
--   계정 줄 쓰기와 같은 batch(lib/prog.js save extra — 계정 tx 가 이긴 때만 들어간다). 문의 · 되돌림 · 통계용(값 계산에는 쓰지 않는다).
CREATE TABLE prog_pot_log (
  id      INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  hero    TEXT    NOT NULL,
  node    TEXT    NOT NULL,
  lv      INTEGER NOT NULL CHECK (lv >= 1),
  cost    TEXT    NOT NULL,
  at      INTEGER NOT NULL
);
CREATE INDEX prog_pot_log_user ON prog_pot_log(user_id, at DESC);

-- 프론티어 시도를 열 때 떠 둔 편성 사도의 잠재력(엔진 ProgState JSON — {"heroes": {사도: {"pot": {칸: 단계}}}}). NULL = 성장 끔 또는 편성에 잠재력 없음(지금 판과 같은 셋업).
--   게임은 시작 응답의 prog 로 Frontier.Start(…, prog), 검증기(bz verify)는 pending 의 prog 로 셋업을 다시 지어 견준다.
ALTER TABLE attempts ADD COLUMN prog_json TEXT;
