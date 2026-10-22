-- 교주 보드(크레파스 보드) 진행을 계정에 — 기기를 바꿔도 이어지게(2026-10-19 사용자). 주소: GET · PUT /api/me/progress(functions/api/me/progress.js).
-- 사용자당 한 줄. json = 진행(게임의 PlayerPrefs bz.crayon 과 같은 꼴 — have · earned · runs · ver · keys · levels, lib/progress.js 가 검사한다).
-- rev = 낙관적 잠금 판 번호(쓸 때마다 +1 — 다른 기기가 먼저 썼으면 409 로 돌려보낸다) · updated = 바꾼 때(유닉스 초). 탈퇴하면 같이 지운다(me.js).
CREATE TABLE board_progress (
  user_id INTEGER PRIMARY KEY REFERENCES users(id) ON DELETE CASCADE,
  json    TEXT    NOT NULL,
  rev     INTEGER NOT NULL CHECK (rev >= 1),
  updated INTEGER NOT NULL
);
