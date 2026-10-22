// 닉네임 규칙 — 정하기 · 바꾸기(/api/me/name) · 가짜 로그인(test.js)이 같이 쓴다. 설명: ../../Docs/프론티어_서버.md §닉네임
import { now, fail } from "./util.js";

export const NICK_MIN = 2, NICK_MAX = 12;
export const NICK_CHANGE_SEC = 24 * 60 * 60;   // 처음 정한 뒤 바꾸기는 하루 한 번

// 쓸 수 있는 글자 — 한글(완성형 · 자모) · 영문 · 숫자 · 공백(사이에 하나씩) · _ - .
const ALLOWED = /^[가-힣ㄱ-ㅎㅏ-ㅣA-Za-z0-9 _.\-]+$/u;
const HAS_WORD = /[가-힣ㄱ-ㅎㅏ-ㅣA-Za-z0-9]/u;
// 운영자 · 원작사 사칭 막기 — 열쇠(소문자 · 기호 뺌)에 들어 있으면 안 됨 / 열쇠가 딱 이것이면 안 됨
const BANNED_IN = ["운영자", "운영진", "운영팀", "관리자", "개발자", "볼제나", "bolzena", "admin", "moderator", "official", "공식", "에피드", "epidgames"];
const BANNED_IS = ["gm", "mod", "staff", "system", "시스템", "운영", "관리"];

/**
 * 보이는 이름 — NFKC(전각 → 반각 · 조합형 한글 → 완성형) · 앞뒤 공백 정리 · 사이 공백은 하나로.
 * 한글 호환 자모(ㄱ-ㅎ ㅏ-ㅣ — 「ㅋㅋ」)는 NFKC 에 넣지 않는다 — NFKC 는 이것을 첫가끝 자모(U+1100~)로 바꿔 쓸 수 있는 글자에서 빠지고,
 * 「ㄱㅏ」 를 「가」 로 붙여 버린다(2026-10-19 겹침 시험에서 찾음). 반각 자모(ﾡ)는 NFKC 가 첫가끝 자모로 바꿔 쓸 수 없는 글자가 된다(겹칠 일이 없다).
 */
export const nickShow = (x) => String(x).replace(/[^ㄱ-ㅣ]+/gu, (s) => s.normalize("NFKC")).trim().replace(/\s+/g, " ");
/**
 * 겹침 검사 열쇠 — 보이는 이름의 소문자에서 공백 · _ - . 를 뺀 것(「Bol Zena」 = 「bolzena」 = 「ＢＯＬＺＥＮＡ」).
 * 겹침은 DB 가 막는다 — users.nick_key 에 UNIQUE 인덱스(migrations/0003). 동시에 같은 이름을 내도 하나만 들어가고 나머지는 409.
 * 비슷한 글자(o/0 · l/I/1)는 같은 이름으로 보지 않는다(정책 — Docs/프론티어_서버.md §닉네임). 키릴 · 그리스 글자는 쓸 수 있는 글자가 아니라 막힌다.
 */
export const nickKey = (show) => show.toLowerCase().replace(/[ _.\-]/g, "");

/** 검사 — { show, key } 또는 { error }(사람이 읽을 까닭). */
export function checkNick(raw) {
  if (typeof raw !== "string" || raw.length > 64) return { error: "닉네임은 글이어야 한다" };
  const show = nickShow(raw), n = [...show].length;
  if (n < NICK_MIN || n > NICK_MAX) return { error: `닉네임은 ${NICK_MIN}~${NICK_MAX}자` };
  if (!ALLOWED.test(show)) return { error: "쓸 수 있는 글자는 한글 · 영문 · 숫자 · 공백 · _ - . 뿐" };
  if (!HAS_WORD.test(show)) return { error: "글자나 숫자가 하나는 있어야 한다" };
  const key = nickKey(show);
  if (BANNED_IS.includes(key) || BANNED_IN.some((w) => key.includes(w))) return { error: "쓸 수 없는 닉네임(운영자 · 공식 사칭)" };
  return { show, key };
}

/**
 * 닉네임 정하기 · 바꾸기 — { name } 또는 { res }(400 규칙 · 409 겹침 · 429 바꾸기 제한).
 * 처음 정하기는 언제든, 바꾸기는 NICK_CHANGE_SEC 에 한 번(force 면 제한 없음 — 가짜 로그인만). 같은 이름으로 다시 내면 그대로 성공(횟수 안 씀).
 */
export async function setNick(env, userId, raw, force = false) {
  const c = checkNick(raw);
  if (c.error) return { res: fail(400, "bad name", { why: c.error }) };
  const u = await env.DB.prepare("SELECT nick, nick_changed FROM users WHERE id = ?1").bind(userId).first();
  if (!u) return { res: fail(404, "no user") };
  if (u.nick === c.show) return { name: c.show };
  const t = now();
  if (!force && u.nick && u.nick_changed && t - u.nick_changed < NICK_CHANGE_SEC)
    return { res: fail(429, "닉네임은 하루에 한 번만 바꿀 수 있다", { retryAfter: u.nick_changed + NICK_CHANGE_SEC - t }) };
  try {
    await env.DB.prepare("UPDATE users SET nick = ?1, nick_key = ?2, nick_changed = ?3 WHERE id = ?4").bind(c.show, c.key, t, userId).run();
  } catch (e) {
    if (/UNIQUE/i.test(String(e && e.message))) return { res: fail(409, "이미 누가 쓰는 닉네임", { why: "대소문자 · 전각/반각 · 공백 · _ - . 만 다른 이름도 같은 이름으로 본다" }) };
    throw e;
  }
  return { name: c.show };
}
