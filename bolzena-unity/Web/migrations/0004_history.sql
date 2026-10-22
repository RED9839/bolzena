-- 기록 보기(2026-10-18 사용자 「프론티어 기록 확인할 수 있게」) — 내 도전 기록 · 남의 편성 보기 · 칸별 최고(서버 검증 점수).
-- slot_hashes = 시작 때 떠 둔 슬롯들의 지문(hero_slots.hash — 슬롯 글 SHA-256 앞 16자리, heroes 와 같은 차례의 JSON 배열).
--   GET /api/slots 가 지금 칸의 hash 와 견주어 「이 칸(이 글)으로 낸 검증된 최고」 를 준다 — 덮어쓰면 hash 가 바뀌어 새로 센다.
--   이 열이 없던 시도(NULL)는 서버가 slots_json 의 글을 해시해 대신한다.
ALTER TABLE attempts ADD COLUMN slot_hashes TEXT;
CREATE INDEX attempts_user_started ON attempts(user_id, started DESC);
