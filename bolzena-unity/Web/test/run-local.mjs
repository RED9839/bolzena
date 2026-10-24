// 프론티어 서버 로컬 시험 — 실제 Cloudflare 계정 · 네트워크를 쓰지 않는다(wrangler pages dev 로컬 + 로컬 D1).
//   cd C:/projects/bolzena-unity/Web && node test/run-local.mjs
// 하는 일: 로컬 D1 을 새로 만들고 마이그레이션 → 서버 띄움(TEST_LOGIN=1 · 가짜 시크릿) → 가짜 로그인 두 사람(웹 쿠키 · PC Bearer) →
//   가입(닉네임 없이 403 · 규칙 · 금지어 · 겹침 · 바꾸기 제한) → 닉네임 겹침 뚫기(대소문자 · 공백 · 전각 · NFD · 보이지 않는 글자 · 다른 문자 · 동시 신청 · 탈퇴 뒤 재사용) →
//   옛 칸(0004 까지의 표에 넣은 「데이브」 의 칸 0~2)이 마이그레이션 0005 뒤에도 id · 글 · 지문 · 상태 그대로인지 →
//   봇 판 슬롯(bz frontier --save) 올리기(옛 주소 PUT /:hero/:n) · 새로 저장(POST — 사도 하나에 칸 여럿) · 이름 · 덮어쓰기 · 지우기(/api/slot/:id) · 계정 안전 한도(1,000칸) · 사도마다 10칸(넘은 칸 읽기 · 지우기) → 프론티어 시작(서버 씨앗) → 그 씨앗 · 떠 둔 슬롯으로 bz frontier 가 싸운 진짜 테이프 제출 →
//   제출 상한(보스 HP · 턴 · 턴 지문 · 한 턴 피해 · 크기) · 위조 점수 · 위조 테이프 · 위조 슬롯 · 다른 씨앗 · 두 번 내기 · 같은 사도 둘 · 다른 사이트 요청 →
//   검증 전 랭킹(곧바로 「검증 전」 · 남에게는 줄만) → bz verify(이 로컬 서버 대상) →
//   진짜는 verified 로 랭킹에, 위조는 rejected · bad 인지 → 기록 보기(내 기록 · 랭킹 줄의 편성 · 남의 미검증 404 · 칸별 최고) →
//   운영 주소(/api/admin — 토큰 · 찾기 · 닉네임 강제 변경/비우기 · 랭킹 숨김 · 점수 지우기/거절 · 칸 지우기 · 정지/풀기 · 운영 기록)와 bz admin 명령 →
//   교주 보드 진행(/api/me/progress — 2026-10-21 보드 없앰: 쓰기 410 · 쌓인 줄은 읽기 · 운영 보기 · 탈퇴 때 지움) → 기기 코드 로그인 → 로그아웃 · 탈퇴(정지된 채로) →
//   성장 끔(PROG 없음 — 지금 공개판) 확인 → **성장 켬 서버(PROG=1, migrations/0010 — 2026-10-21 밤 잠재력만)**: 표가 엔진 · 콘텐츠와 같은가(단계 보상 · 사도 · 성격 · 잠재력 칸 · 레벨 칸 없음) ·
//   비로그인 · 없앤 주소(레벨업 · 진급 · 던전) · 도전 사다리(단계 잠금 · 완주로 열림) · 모험 끝 마시멜로 보상 계산(등급 · 단계 배율 · 진행 비율 · 완주 덤) · 같은 판 두 번 · 행동력 없음 ·
//   위조 거절 · 주간 상한(등급별) · **잠재력**: 칸 표 = 성장 표 · 여는 차례(앞 칸만 — 진급 문턱 없음) · 재료(골드 없음) · 최대 · 동시 · 기록 · 떠 두기 ·
//   프론티어 잠재력 떠 두기(없으면 안 뜸) → 그 성장으로 싸운 테이프 verified · 성장을 뺀 테이프 rejected · 탈퇴 →
//   TEST_LOGIN · ADMIN_TOKEN 없는 서버에서 가짜 로그인 · 운영 주소가 닫혔는지 · 플레이 기록 tier 칸.
// 결과 · 로그: test/.out/(dev.log · verify.log · tapes · result.json). 기대와 다르면 끝 코드 1.
// 바꿀 것: 환경 변수 WRANGLER(wrangler 실행 파일) · BZ(bz.cmd) · CONTENT(콘텐츠 폴더) · PORT ·
//   MIG_UPTO(예 0009 — 그 번호까지만 마이그레이션 · 성장 켬 시험 건너뜀: 성장 켜기 전 공개판 D1 흉내) ·
//   PROG_TABLE(성장 표 — content-v2 밖에 둔 원본. 성장 켬 시험은 CONTENT 사본 test/.out/prog-content 에 넣어 쓴다) · CORE(엔진 폴더 — 단계 표를 견준다).
import { spawn, spawnSync, execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import crypto from "node:crypto";
import { fileURLToPath, pathToFileURL } from "node:url";

const WEB = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const OUT = path.join(WEB, "test", ".out"), STATE = path.join(WEB, "test", ".state"), STATIC = path.join(OUT, "static");
const BZ = process.env.BZ || "C:\\projects\\bolzena-core\\Tools~\\Dev\\bz.cmd";
const CONTENT = process.env.CONTENT || "C:\\projects\\bolzena-content-v2";
const PROG_TABLE = process.env.PROG_TABLE || "C:\\projects\\_backup\\prog_staging\\카제나판.json";
const CORE = process.env.CORE || "C:\\projects\\bolzena-core";
const PORT = Number(process.env.PORT || 8799), BASE = `http://127.0.0.1:${PORT}`;
/** 마이그레이션을 이 번호(네 자리)까지만 — 예) MIG_UPTO=0009 = 성장 켜기 전 공개판 D1(0010 없음). 비우면 전부 + 성장 켬 시험. */
const MIG_UPTO = process.env.MIG_UPTO || "";
const VERIFY_TOKEN = "local-verify-token-" + "0123456789abcdef".repeat(2);
const ADMIN_TOKEN = "local-admin-token-" + "fedcba9876543210".repeat(2);

function findWrangler() {
  if (process.env.WRANGLER) return process.env.WRANGLER;
  const cache = path.join(os.homedir(), "AppData", "Local", "npm-cache", "_npx");
  if (fs.existsSync(cache)) for (const d of fs.readdirSync(cache)) {
    const bin = path.join(cache, d, "node_modules", ".bin", process.platform === "win32" ? "wrangler.cmd" : "wrangler");
    if (fs.existsSync(bin)) return bin;
  }
  return "npx --no wrangler";
}
const WRANGLER = findWrangler();
const Q = (cmd) => (fs.existsSync(cmd) ? `"${cmd}"` : cmd);   // 실행 파일 경로면 따옴표, 「npx --no wrangler」 면 그대로
const ENV = { ...process.env, WRANGLER_SEND_METRICS: "false", CI: "1" };

// ── 기록 ──
const rows = []; let failed = 0;
function expect(name, ok, detail = "") {
  rows.push({ name, ok: !!ok, detail });
  if (!ok) failed++;
  console.log(`${ok ? "통과" : "실패"} — ${name}${detail ? " · " + detail : ""}`);
}
const short = (x) => { const s = typeof x === "string" ? x : JSON.stringify(x); return s.length > 160 ? s.slice(0, 160) + "…" : s; };

// ── 요청 ──
async function req(method, p, { body, cookie, bearer, headers = {}, raw = false } = {}) {
  const h = { ...headers };
  if (cookie) h.cookie = cookie;
  if (bearer) h.authorization = "Bearer " + bearer;
  let b = body;
  if (body !== undefined && typeof body !== "string") { b = JSON.stringify(body); h["content-type"] = "application/json"; }
  const r = await fetch(BASE + p, { method, headers: h, body: b, redirect: "manual" });
  const text = await r.text();
  let data = null; try { data = JSON.parse(text); } catch { data = text; }
  return { status: r.status, data, headers: r.headers, text: raw ? text : undefined };
}
const cookieOf = (r) => (r.headers.getSetCookie?.() || []).map((c) => c.split(";")[0]).find((c) => c.startsWith("bz_s="));

// ── 서버 ──
function run(cmd, args, opts = {}) {
  const r = spawnSync(cmd, args, { cwd: WEB, env: ENV, encoding: "utf8", shell: true, ...opts });
  if (r.status !== 0) throw new Error(`${cmd} ${args.join(" ")} → ${r.status}\n${r.stdout}\n${r.stderr}`);
  return r.stdout;
}
function startServer(bindings, logName, port = PORT) {
  const args = ["pages", "dev", `"${STATIC}"`, "--port", String(port), "--ip", "127.0.0.1", "--d1", "DB=bolzena-frontier-local", "--persist-to", `"${STATE}"`, "--show-interactive-dev-session=false"];
  for (const [k, v] of Object.entries(bindings)) args.push("-b", `${k}=${v}`);
  const log = fs.openSync(path.join(OUT, logName), "w");
  const p = spawn(Q(WRANGLER), args, { cwd: WEB, env: ENV, shell: true, stdio: ["ignore", log, log] });
  return p;
}
async function waitUp(port = PORT) {
  for (let i = 0; i < 90; i++) {
    try { const r = await fetch(`http://127.0.0.1:${port}/api/frontier/season`); if (r.status === 200 || r.status === 503) return; } catch {}
    await new Promise((r) => setTimeout(r, 1000));
  }
  throw new Error("서버가 뜨지 않는다 — test/.out/dev.log");
}
function stop(p) {
  if (!p || p.exitCode !== null) return;
  try { if (process.platform === "win32") execSync(`taskkill /pid ${p.pid} /T /F`, { stdio: "ignore" }); else p.kill("SIGTERM"); } catch {}
}

// ── bz ──
function bz(args) { return run(Q(BZ), args.map((a) => (/[\s]/.test(a) ? `"${a}"` : a))); }
/** bz admin — 이 로컬 서버 대상. 끝 코드와 출력(끝 코드가 0 이 아니어도 던지지 않는다). 출력은 로그에도 찍는다. */
function bzAdmin(args, token = ADMIN_TOKEN) {
  const r = spawnSync(Q(BZ), ["admin", ...args, "--url", BASE, "--token", token].map((a) => (/\s/.test(a) ? `"${a}"` : a)), { cwd: WEB, env: ENV, encoding: "utf8", shell: true });
  const out = (r.stdout || "") + (r.stderr || "");
  console.log(`  $ bz admin ${args.map((a) => (/\s/.test(a) ? `"${a}"` : a)).join(" ")}  → 끝 코드 ${r.status}\n` + out.trim().split(/\r?\n/).map((l) => "  │ " + l).join("\n"));
  return { code: r.status, out };
}
/** 시작 응답의 떠 둔 슬롯으로 bz frontier 가 싸운 진짜 테이프(글). */
function fight(name, start) {
  const files = start.slots.map((s, i) => { const f = path.join(OUT, `${name}_slot${i}.json`); fs.writeFileSync(f, JSON.stringify(s.slot)); return f; });
  const tape = path.join(OUT, `${name}_tape.json`);
  const o = bz(["frontier", "--data", CONTENT, "--slots", files.join(","), "--seed", String(start.seed), "--fightbot", "smart", "--tape", tape]);
  const line = o.split(/\r?\n/).find((l) => l.startsWith("점수")) || "";
  return { text: fs.readFileSync(tape, "utf8"), line };
}

// ── 성장 켬(PROG=1 — migrations/0010 · lib/prog.js · functions/api/prog/* — 2026-10-21 밤 잠재력만) ──
/** 콘텐츠 사본 + 성장 표(content-v2 밖에 둔 원본) — 성장 켬 프론티어 싸움 · 검증기가 같은 것을 읽는다. */
function progContent() {
  const dir = path.join(OUT, "prog-content");
  fs.rmSync(dir, { recursive: true, force: true });
  fs.cpSync(CONTENT, dir, { recursive: true, filter: (src) => !/[\\/](_gen|_measure|\.git)([\\/]|$)/.test(src.slice(CONTENT.length)) });
  fs.mkdirSync(path.join(dir, "world", "성장"), { recursive: true });
  fs.copyFileSync(PROG_TABLE, path.join(dir, "world", "성장", "카제나판.json"));
  return dir;
}
/** 콘텐츠 사도 id → 역할. */
function contentHeroes(dir) {
  const o = {};
  const walk = (d) => { for (const f of fs.readdirSync(d, { withFileTypes: true })) { const p = path.join(d, f.name); if (f.isDirectory()) walk(p); else if (f.name.endsWith(".json")) for (const h of JSON.parse(fs.readFileSync(p, "utf8")).heroes || []) o[h.id] = h.role; } };
  walk(path.join(dir, "heroes"));
  return o;
}

async function progPhase(slotA) {
  const PP = PORT + 2, B2 = `http://127.0.0.1:${PP}`;
  const PROG_CONTENT = progContent();
  const L = await import(pathToFileURL(path.join(WEB, "lib", "prog.js")).href);
  const tblP = JSON.parse(fs.readFileSync(PROG_TABLE, "utf8")).prog;
  // 표가 엔진 · 콘텐츠와 같은가
  {
    const rules = fs.readFileSync(path.join(CORE, "Runtime", "Rules", "Rules.cs"), "utf8");
    const hp = (rules.match(/double\[\] HP\s*=\s*\{([^}]*)\}/) || [])[1]?.split(",").length;
    const pathSum = [1, 2, 3, 4, 5, 6, 6, 7, 8, 8].reduce((a, t) => a.map((x, i) => x + L.MATS[t][i]), [0, 0, 0]), tot0 = L.potTotal();
    expect("서버 단계 표(lib/prog.js MATS) — 단계 수 = 엔진 Tiers · 상급은 단계마다 줄지 않음 · 기준 길 10판 = 사도 하나 11칸 상급(하급 · 중급은 남음)", L.MATS.length - 1 === L.TIER_MAX && hp - 1 === L.TIER_MAX
      && L.MATS.slice(2).every((m, i) => m[2] >= L.MATS[i + 1][2]) && pathSum[2] === tot0.mm3 && pathSum[0] >= tot0.mm1 && pathSum[1] >= tot0.mm2, short({ pathSum, tot0, hp }));
    const ch = contentHeroes(CONTENT);
    expect("서버 사도 표(lib/prog.js HEROES) = 콘텐츠 사도 · 역할", JSON.stringify(Object.entries(ch).sort()) === JSON.stringify(Object.entries(L.HEROES).sort()), short({ content: Object.keys(ch).length, server: Object.keys(L.HEROES).length }));
    expect("성장 표 — 잠재력만(level · pieces 칸 없음 · 칸에 진급 문턱 promo 없음 · 측정 묶음은 pot 만)", !("level" in tblP) && !("pieces" in tblP) && tblP.potential.every((n) => !("promo" in n))
      && Object.values(tblP.presets).every((p) => Object.keys(p).every((k) => k === "desc" || k === "pot")), short(Object.keys(tblP)));
    const pt = tblP.potential.map((n) => ({ id: n.id, max: n.max ?? 1, kind: n.kind, after: n.after ?? null, tier: n.tier ?? 0 }));
    expect("잠재력 — 서버 칸 표(lib/prog.js POTENTIAL) = 성장 표 potential(id · max · kind · after · 여는 사다리 단계 tier · 차례 — 11칸 줄기 + 갈래 넷 · 04-1 치명 감각)", pt.length === 11 && JSON.stringify(pt) === JSON.stringify(L.POTENTIAL), short({ table: pt.map((n) => n.id + ":" + n.max + ":" + n.after + ":" + n.tier) }));
    expect("잠재력 — 성장 표 사도 칸(heroes)이 서버 사도 20명 모두 · 칸 03-1 · 05 · 05-1 · 06 · 07", Object.keys(L.HEROES).every((h) => ["03-1", "05", "05-1", "06", "07"].every((n) => tblP.heroes?.[h]?.[n]?.desc)), short(Object.keys(tblP.heroes || {}).length));
    const tot = L.potTotal();
    expect("잠재력 — 사도 하나 11칸 = 마시멜로 하급 24 · 중급 161 · 상급 464(10칸 24 · 125 · 440 + 04-1 중급 36 · 상급 24) · 골드 없음", tot.mm1 === 24 && tot.mm2 === 161 && tot.mm3 === 464 && Object.keys(tot).length === 3, short(tot));
  }
  const srv = startServer({ TEST_LOGIN: "1", ID_PEPPER: "local-pepper", VERIFY_TOKEN, PROG: "1" }, "dev-prog.log", PP);
  const rq = (method, p, o = {}) => fetch(B2 + p, { method, headers: { ...(o.bearer ? { authorization: "Bearer " + o.bearer } : {}), ...(o.cookie ? { cookie: o.cookie } : {}), ...(o.body !== undefined ? { "content-type": "application/json" } : {}), ...(o.headers || {}) }, body: o.body === undefined ? undefined : JSON.stringify(o.body), redirect: "manual" })
    .then(async (x) => { const t = await x.text(); let d = null; try { d = JSON.parse(t); } catch { d = t; } return { status: x.status, data: d, headers: x.headers }; });
  const sqlRun = (cmd) => { const f = path.join(OUT, "prog_q.sql"); fs.writeFileSync(f, cmd + ";\n"); return run(Q(WRANGLER), ["d1", "execute", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`, "--json", "--file", `"${f}"`]); };
  try {
    await waitUp(PP);
    let r;
    // 로그인 · 가입 — 그레이스(PC)
    r = await rq("GET", "/api/auth/test?sub=grace&kind=pc"); const G = r.data.token;
    r = await rq("POST", "/api/me/name", { bearer: G, body: { name: "그레이스" } }); expect("성장 켬 — 그레이스 가입", r.status === 200, short(r.data));
    const uid = (await rq("GET", "/api/me", { bearer: G })).data.user.id;
    const earn = async (body, who = { bearer: G }) => {
      for (;;) { const q = await rq("POST", "/api/prog/earn", { ...who, body }); if (q.status !== 429) return q; await new Promise((ok) => setTimeout(ok, ((q.data && q.data.retryAfter) || 5) * 1000 + 500)); }
    };
    const party = ["아야", "이드", "실비아"];
    let refN = 0; const nref = () => `run-${uid}-${Date.now().toString(36)}-${refN++}`;

    // 비로그인 · 가입 전 · 다른 사이트
    r = await rq("GET", "/api/prog"); expect("성장 — 로그인 안 함 GET /api/prog → 401(로그인 안 하면 성장 없음)", r.status === 401);
    r = await rq("POST", "/api/prog/earn", { body: { ref: nref(), tier: 1, result: "win", floor: 2, min: 30, party } }); expect("성장 — 로그인 안 함 모험 끝 보상 → 401", r.status === 401);
    r = await rq("GET", "/api/auth/test?sub=hank&kind=pc"); const H = r.data.token;
    r = await earn({ ref: nref(), tier: 1, result: "win", floor: 2, min: 30, party }, { bearer: H }); expect("성장 — 가입 전 모험 끝 보상 → 403 needsName", r.status === 403 && r.data.needsName === true, short(r.data));
    r = await rq("GET", "/api/auth/test?sub=alice&back=/"); const AC = (r.headers.getSetCookie?.() || []).map((c) => c.split(";")[0]).find((c) => c.startsWith("bz_s="));
    r = await rq("POST", "/api/prog/earn", { cookie: AC, body: { ref: nref(), tier: 1, result: "win", floor: 2, min: 30, party }, headers: { origin: "https://evil.example" } }); expect("성장 — 다른 사이트에서 쿠키로 모험 끝 보상 → 403", r.status === 403, short(r.data));
    for (const p of ["/api/prog/levelup", "/api/prog/promote", "/api/dungeon", "/api/dungeon/start"]) {
      r = await rq(p === "/api/dungeon" ? "GET" : "POST", p, { bearer: G, body: p === "/api/dungeon" ? undefined : {} });
      expect(`성장 — 없앤 주소 ${p} → API 가 없다(레벨 · 진급 · 던전 없음 — 정적 쪽 · 404 · 405)`, r.status === 404 || r.status === 405 || typeof r.data === "string", short({ s: r.status }));
    }

    // 처음 상태
    r = await rq("GET", "/api/prog", { bearer: G });
    const p0 = r.data;
    expect("성장 — 처음: 최고 단계 0 · 열린 단계 1 · 사도 20명 잠재력 없음 · 지갑 = 마시멜로 하급 · 중급 · 상급 셋 모두 0 · 레벨 · 교주 · 골드 · 왕사탕 · 성격 재료 칸 없음",
      r.status === 200 && p0.on === true && p0.bestTier === 0 && p0.open === 1 && Object.keys(p0.heroes).length === 20 && Object.values(p0.heroes).every((h) => Object.keys(h.pot).length === 0 && h.potential.length === 11 && !("lv" in h) && !("promo" in h))
      && JSON.stringify(Object.keys(p0.wallet)) === JSON.stringify(["mm1", "mm2", "mm3"]) && Object.values(p0.wallet).every((x) => x === 0) && p0.currency.length === 3 && p0.currency.some((c) => c.key === "mm2" && c.name === "중급 마시멜로")
      && !("account" in p0) && !("candy" in p0) && !("gold" in p0.wallet), short(Object.keys(p0)));

    // 단계 잠금 · 모양 위조
    r = await earn({ ref: nref(), tier: 2, result: "win", floor: 2, min: 30, party }); expect("도전 사다리 — 잠긴 단계 2 보상 → 409 tier locked(열린 단계 1)", r.status === 409 && r.data.error === "tier locked" && r.data.open === 1, short(r.data));
    for (const [why, body] of [
      ["단계 0", { tier: 0 }], ["단계 10", { tier: 10 }], ["단계 글", { tier: "1" }], ["결과 모름", { result: "clear" }], ["층 3", { floor: 3 }], ["층 0", { floor: 0 }],
      ["모르는 사도", { party: ["아야", "치트"] }], ["같은 사도 둘", { party: ["아야", "아야"] }], ["사도 넷", { party: ["아야", "이드", "실비아", "란"] }], ["빈 편성", { party: [] }],
      ["판 열쇠 모양", { ref: "a b c d e f g" }], ["판 열쇠 짧음", { ref: "abc" }], ["분 음수", { min: -1 }], ["3분 미만 완주", { min: 2 }],
    ]) {
      r = await earn({ ref: nref(), tier: 1, result: "win", floor: 2, min: 30, party, ...body });
      expect(`성장 — 위조 · 모양 틀림(${why}) → 400`, r.status === 400, short(r.data));
    }

    // 단계 1 완주 — 계산식
    // 서버 응답 견본 — 게임 데모(-demo-prog -prog-samples test/.out/prog-samples)가 진짜 응답 꼴로 화면을 찍는다
    const SAMPLES = path.join(OUT, "prog-samples"); fs.rmSync(SAMPLES, { recursive: true, force: true }); fs.mkdirSync(SAMPLES, { recursive: true });
    const sample = (n, d) => fs.writeFileSync(path.join(SAMPLES, n + ".json"), JSON.stringify(d, null, 1));
    sample("view0", p0);
    const ref1 = nref();
    r = await earn({ ref: ref1, tier: 1, result: "win", floor: 2, min: 25, party });
    const e1 = r.data; sample("earn", e1);
    const gi = e1.reward?.give?.items || {};
    expect("모험 끝 보상 — 단계 1 완주: 판마다 마시멜로 하급 12 · 중급 28 · 상급 24(단계 표 · 편성 크기 상관없이 · 성격 없음)", r.status === 200 && e1.dup === false
      && gi.mm1 === 12 && gi.mm2 === 28 && gi.mm3 === 24 && Object.keys(gi).length === 3 && e1.reward.capped.length === 0 && e1.prog.wallet.mm3 === 24 && e1.prog.wallet.mm1 === 12, short(e1.reward));
    expect("도전 사다리 — 단계 1 완주 → 최고 단계 1 · 단계 2 열림", e1.bestTier === 1 && e1.opened === 2 && e1.prog?.open === 2, short({ best: e1.bestTier, opened: e1.opened }));
    {
      const M = (tier, result, floor = 2) => L.runMats(tier, result, floor);
      const a5 = M(5, "win"), a9 = M(9, "win"), l9 = M(9, "lose"), l2 = M(2, "lose", 1);
      expect("모험 끝 보상 계산 — 단계 표: 5 완주 하급 3 · 중급 16 · 상급 48 · 9 완주 3 · 14 · 66 · 9 2층 쓰러짐(×0.35) 1 · 5 · 23 · 2 1층 쓰러짐(×0.15) 2 · 4 · 4(실패 판 ≈ 완주의 1/4)",
        a5.mm1 === 3 && a5.mm2 === 16 && a5.mm3 === 48 && a9.mm1 === 3 && a9.mm2 === 14 && a9.mm3 === 66 && l9.mm1 === 1 && l9.mm2 === 5 && l9.mm3 === 23 && l2.mm1 === 2 && l2.mm2 === 4 && l2.mm3 === 4,
        short({ a5, a9, l9, l2 }));
    }
    r = await earn({ ref: ref1, tier: 1, result: "win", floor: 2, min: 25, party }); expect("모험 끝 보상 — 같은 판(같은 ref) 두 번 → 200 dup(아무것도 안 줌)", r.status === 200 && r.data.dup === true && r.data.reward === null && r.data.prog.wallet.mm3 === 24, short(r.data.prog?.wallet));
    // 진 판 — 진행 비율
    r = await earn({ ref: nref(), tier: 2, result: "lose", floor: 1, min: 5, party });
    expect("모험 끝 보상 — 단계 2 1층에서 쓰러짐(×0.15): 하급 2 · 중급 4 · 상급 4 · 최고 단계 그대로", r.status === 200 && r.data.reward.give.items.mm1 === 2 && r.data.reward.give.items.mm2 === 4 && r.data.reward.give.items.mm3 === 4
      && r.data.bestTier === 1 && r.data.opened === null, short(r.data.reward));
    r = await earn({ ref: nref(), tier: 1, result: "abandon", floor: 2, min: 5, party: ["란"] });
    expect("모험 끝 보상 — 포기 · 2층(×0.35) · 사도 하나: 하급 4 · 중급 10 · 상급 8(편성 크기 상관없이 같은 몫)", r.status === 200 && r.data.reward.give.items.mm1 === 4 && r.data.reward.give.items.mm2 === 10 && r.data.reward.give.items.mm3 === 8, short(r.data.reward));
    // 행동력이 없다 — 판을 여럿 받아도 막히지 않는다(분당 6번 상한만)
    r = await earn({ ref: nref(), tier: 2, result: "win", floor: 2, min: 30, party });
    expect("모험 끝 보상 — 행동력 없음: 넷째 판도 받는다 · 단계 2 완주 → 단계 3 열림", r.status === 200 && r.data.reward.give.items.mm3 === 24 && r.data.bestTier === 2 && r.data.opened === 3, short(r.data.reward?.give?.items));
    // 주간 상한 — 이번 주 받은 양을 상한 바로 밑으로(D1 에 바로) 해 두고 단계 3 완주(하급 4 · 중급 34 · 상급 42)
    const wk = L.weekOf();
    sqlRun(`UPDATE prog_account SET week = ${wk}, week_got = '{"mm1":179,"mm2":510,"mm3":900}' WHERE user_id = ${uid}`);
    r = await earn({ ref: nref(), tier: 3, result: "win", floor: 2, min: 30, party });
    const cg = r.data.reward?.give;
    expect("모험 끝 보상 — 주간 상한: 하급 남은 1 · 중급 0 · 상급 그대로(42) · 잘린 칸 둘", r.status === 200 && cg.items.mm1 === 1 && !cg.items.mm2 && cg.items.mm3 === 42
      && JSON.stringify(r.data.reward.capped.sort()) === JSON.stringify(["mm1", "mm2"])
      && r.data.prog.week.got.mm1 === 180 && r.data.prog.week.got.mm3 === 942, short(r.data.reward));
    expect("도전 사다리 — 상한에 걸려도 완주는 완주(최고 단계 3 · 단계 4 열림)", r.data.bestTier === 3 && r.data.opened === 4, short({ best: r.data.bestTier }));
    r = await earn({ ref: nref(), tier: 9, result: "win", floor: 2, min: 30, party }); expect("도전 사다리 — 단계 9 위조(열린 단계 4) → 409 tier locked", r.status === 409 && r.data.open === 4, short(r.data));
    sqlRun(`UPDATE prog_account SET week_got = '{}' WHERE user_id = ${uid}`);
    sample("view", (await rq("GET", "/api/prog", { bearer: G })).data);

    // 잠재력(카제나식 — lib/prog.js POTENTIAL · POT_COST · potCheck · /api/prog/potential). 재료 = 마시멜로 하급 · 중급 · 상급
    {
      const pot = async (hero, node) => { for (;;) { const q = await rq("POST", "/api/prog/potential", { bearer: G, body: { hero, node } }); if (q.status !== 429) return q; await new Promise((ok) => setTimeout(ok, ((q.data && q.data.retryAfter) || 5) * 1000 + 500)); } };
      let pv = (await rq("GET", "/api/prog", { bearer: G })).data;
      const ap = pv.heroes["아야"].potential, at = (id) => ap.find((n) => n.id === id);
      expect("잠재력 — 처음: 아야 11칸 모두 0 · 01 열림(하급 8 · 골드 없음) · 02 · 03-1 잠김(앞 칸 — 진급 문턱 없음)", ap.length === 11 && ap.every((n) => n.lv === 0 && !("promo" in n)) && at("01").locked === false && at("01").next.items.mm1 === 8 && !("gold" in at("01").next)
        && at("02").locked === true && /앞 칸/.test(at("02").why) && at("03-1").locked === true && /앞 칸\(03\)/.test(at("03-1").why) && at("05-1").after === "05" && Object.keys(pv.heroes["아야"].pot).length === 0, short(ap.slice(0, 4)));
      r = await pot("아야", "02"); expect("잠재력 — 앞 칸(01)을 안 열고 02 → 409 cannot · 잠김", r.status === 409 && r.data.error === "cannot" && r.data.locked === true, short(r.data));
      expect("잠재력 — 칸 여는 사다리 단계(최고 완주 3): 05 · 06 · 07 은 「난이도 단계 5를 · 7을 완주하면 열린다」(조사 10-24) · 04 · 03-1(단계 3)은 단계 조건 통과", /난이도 단계 5를 완주하면/.test(at("05").why) && /난이도 단계 7을 완주하면/.test(at("07").why) && at("05").tier === 5 && !/난이도 단계/.test(at("04").why || "") && !/난이도 단계/.test(at("03-1").why || ""), short([at("04"), at("05"), at("07")].map((n) => n.why)));
      expect("잠재력 — 04-1 「치명 감각」(10-22 새 칸): 04 갈래 · 최대 5 · 「난이도 단계 4를 완주하면 열린다」 · 첫 단계 중급 10", at("04-1").after === "04" && at("04-1").max === 5 && at("04-1").tier === 4 && /난이도 단계 4를 완주하면/.test(at("04-1").why) && at("04-1").next.items.mm2 === 10, short(at("04-1")));
      sqlRun(`UPDATE prog_account SET bag = '{"mm1":5,"mm2":100}' WHERE user_id = ${uid}`);
      r = await pot("아야", "01"); expect("잠재력 — 재료 모자람(하급 5/8 — 중급으로 대신 못 씀) → 409 · 까닭", r.status === 409 && /하급 마시멜로/.test(r.data.why) && r.data.locked === false, short(r.data));
      sqlRun(`UPDATE prog_account SET bag = '{"mm1":1000,"mm2":1000,"mm3":1000}' WHERE user_id = ${uid}`);
      r = await pot("아야", "01");
      expect("잠재력 — 01 열기: 하급 8 만 빼고 단계 0 → 1", r.status === 200 && r.data.lv[0] === 0 && r.data.lv[1] === 1 && r.data.spent.items.mm1 === 8 && !("gold" in r.data.spent) && r.data.prog.wallet.mm1 === 992 && r.data.prog.heroes["아야"].pot["01"] === 1, short(r.data));
      r = await pot("아야", "01"); expect("잠재력 — 01 최대(1)에서 또 → 409 max", r.status === 409 && r.data.error === "max", short(r.data));
      r = await pot("아야", "02"); const s1 = r.data.spent; r = await pot("아야", "02");
      expect("잠재력 — 02 두 번(카제나 표): 하급 4 → 하급 6 → 단계 2", r.status === 200 && s1.items.mm1 === 4 && r.data.spent.items.mm1 === 6 && r.data.lv[1] === 2, short({ s1, s2: r.data.spent }));
      r = await pot("아야", "05"); expect("잠재력 — 단계 5 를 완주하지 않고 05 → 409 잠김(서버가 계정 최고 완주로 판단)", r.status === 409 && r.data.locked === true && /난이도 단계 5/.test(r.data.why), short(r.data));
      r = await pot("치트", "01"); expect("잠재력 — 모르는 사도 → 400", r.status === 400);
      r = await pot("아야", "99"); expect("잠재력 — 모르는 칸 → 400", r.status === 400);
      r = await rq("POST", "/api/prog/potential", { body: { hero: "아야", node: "02" } }); expect("잠재력 — 로그인 안 함 → 401", r.status === 401);
      pv = (await rq("GET", "/api/prog", { bearer: G })).data; sample("pot_view", pv);
      r = await pot("아야", "03"); sample("potential", r.data);
      const n03 = r.data.prog?.heroes?.["아야"]?.potential?.find((n) => n.id === "03");
      expect("잠재력 — 03 열기(앞 칸 02) → 1 · 화면 줄에 다음 값(중급 5)", r.status === 200 && r.data.node === "03" && r.data.lv[1] === 1 && n03?.next?.items?.mm2 === 5, short(n03));
      r = await pot("아야", "03-1"); expect("잠재력 — 03-1(갈래 — 03 이 1 이상이면 진급 없이 열림): 중급 16", r.status === 200 && r.data.lv[1] === 1 && r.data.spent.items.mm2 === 16, short(r.data));
      // 동시에 두 번(다른 기기) — 낙관적 잠금: 받은 만큼만 오르고 쓴 값이 맞아야
      const bagSum = (w) => L.MM_KEYS.reduce((a, k) => a + (w.wallet[k] | 0), 0);
      const w0 = (await rq("GET", "/api/prog", { bearer: G })).data;
      const both = await Promise.all([pot("아야", "02"), pot("아야", "02")]);
      const okN = both.filter((x) => x.status === 200).length;
      const w1 = (await rq("GET", "/api/prog", { bearer: G })).data;
      const spentItem = both.filter((x) => x.status === 200).reduce((a, x) => a + Object.values(x.data.spent.items).reduce((p, q) => p + q, 0), 0);
      expect("잠재력 — 같은 칸 동시에 두 번 → 성공한 만큼만 오름 · 재료가 그만큼만 줄어듦", okN >= 1 && w1.heroes["아야"].pot["02"] === 2 + okN && bagSum(w0) - bagSum(w1) === spentItem, short({ okN, st: both.map((x) => x.status), lv: w1.heroes["아야"].pot["02"] }));
      const logs = sqlRun(`SELECT COUNT(*) AS n FROM prog_pot_log WHERE user_id = ${uid} AND cost LIKE '%mm%'`);
      expect("잠재력 — 올린 기록(prog_pot_log · cost JSON)이 성공한 수만큼", new RegExp(`"n":\\s*${5 + okN}\\b`).test(logs), short(logs));
      // 떠 두기 — 잠재력이 있는 사도만 pot(표 차례), 편성 누구도 없으면 null
      const snap = L.snapshot({ "아야": { pot: { "02": 3, "01": 1 } } }, ["아야", "이드"]);
      expect("잠재력 — 떠 두는 성장(snapshot): 잠재력 있는 사도만 pot(표 차례) · 아무도 없으면 null", JSON.stringify(snap) === JSON.stringify({ heroes: { "아야": { pot: { "01": 1, "02": 3 } } } }) && L.snapshot({}, ["이드"]) === null, short(snap));
    }
    // 프론티어 — 편성에 잠재력이 없으면 떠 두지 않는다 → 잠재력을 넣고 시도를 열면 떠 둔다 → 그 성장으로 싸운 테이프 verified · 성장을 뺀 테이프 rejected
    const putG = (s) => rq("PUT", `/api/slots/${encodeURIComponent(s.hero)}/0`, { bearer: G, body: s });
    for (const s of slotA) { r = await putG(s); expect(`성장 — 그레이스 슬롯 ${s.hero}`, r.status === 200, short(r.data)); }
    const listG = (await rq("GET", "/api/slots", { bearer: G })).data.slots;
    const refsG = slotA.map((s) => ({ id: listG.find((x) => x.hero === s.hero).id }));
    const startG = async () => { for (;;) { const q = await rq("POST", "/api/frontier/start", { bearer: G, body: { slots: refsG } }); if (q.status !== 429) return q.data; await new Promise((ok) => setTimeout(ok, 6000)); } };
    sqlRun(`DELETE FROM prog_hero WHERE user_id = ${uid} AND hero IN (${slotA.map((s) => `'${s.hero}'`).join(",")})`);
    const g0 = await startG();
    expect("성장 — 편성에 잠재력이 없으면 프론티어 시작 응답에 prog 칸 없음(성장 끔과 같은 셋업)", g0.attempt && !("prog" in g0), short(Object.keys(g0)));
    const potA = { "01": 1, "02": 5, "03": 5, "04": 5, "04-1": 3 };   // 04-1 치명 감각(10-22) — 검증기가 같은 치명 확률로 다시 돌리는지
    sqlRun(slotA.map((s) => `INSERT INTO prog_hero (user_id, hero, pot, updated) VALUES (${uid}, '${s.hero}', '${JSON.stringify(potA)}', 0) ON CONFLICT (user_id, hero) DO UPDATE SET pot = excluded.pot`).join(";\n"));
    const g1 = await startG(), g2 = await startG();
    expect("성장 — 프론티어 시작 응답에 떠 둔 잠재력(편성 사도 셋 · 표 차례)", g1.prog && slotA.every((s) => JSON.stringify(g1.prog.heroes[s.hero]?.pot) === JSON.stringify(potA)) && Object.keys(g1.prog).join() === "heroes", short(g1.prog));
    const fightP = (name, st, prog) => {
      const files = st.slots.map((x, i) => { const f = path.join(OUT, `${name}_slot${i}.json`); fs.writeFileSync(f, JSON.stringify(x.slot)); return f; });
      const tape = path.join(OUT, `${name}_tape.json`), args = ["frontier", "--data", PROG_CONTENT, "--slots", files.join(","), "--seed", String(st.seed), "--fightbot", "smart", "--tape", tape];
      if (prog) { const pf = path.join(OUT, `${name}_prog.json`); fs.writeFileSync(pf, JSON.stringify(prog)); args.push("--prog-state", pf); }
      const o = bz(args);
      return { text: fs.readFileSync(tape, "utf8"), line: o.split(/\r?\n/).find((l) => l.startsWith("점수")) || "" };
    };
    const tg1 = fightP("g1", g1, g1.prog), tg2 = fightP("g2", g2, null);
    const sg1 = JSON.parse(tg1.text).score, sg2 = JSON.parse(tg2.text).score;
    // 낮은 점수를 먼저 낸다 — 서버는 그 사람의 최고 후보만 검증 대기로 둔다(0008, 낮은 점수를 뒤에 내면 skipped)
    const subG1 = () => rq("POST", "/api/frontier/submit", { bearer: G, body: { attempt: g1.attempt, score: sg1, tape: tg1.text } });
    const subG2 = () => rq("POST", "/api/frontier/submit", { bearer: G, body: { attempt: g2.attempt, score: sg2, tape: tg2.text } });
    let r1, r2;
    if (sg2 <= sg1) { r2 = await subG2(); r1 = await subG1(); } else { r1 = await subG1(); r2 = await subG2(); }
    expect("성장 — 잠재력 테이프 제출 → 202 pending", r1.status === 202 && r1.data.status === "pending", short(r1.data)); const idG1 = r1.data.id;
    expect("성장 — 잠재력을 뺀 테이프 제출 → 202 pending(서버는 모양만)", r2.status === 202 && r2.data.status === "pending", short({ r2: r2.data, sg1, sg2 }));
    r = await rq("GET", "/api/verify/pending?limit=50", { bearer: VERIFY_TOKEN });
    const pg = (r.data.scores || []).find((x) => x.id === idG1);
    expect("성장 — 검증기 대기 목록에 떠 둔 잠재력(prog) · 던전 줄 없음", pg && JSON.stringify(pg.prog) === JSON.stringify(g1.prog) && !("dungeons" in r.data), short(pg?.prog));
    const vout = bz(["verify", "--data", PROG_CONTENT, "--url", B2, "--token", VERIFY_TOKEN]);
    fs.writeFileSync(path.join(OUT, "verify-prog.log"), vout);
    const stOf = async (att) => (await rq("GET", `/api/frontier/attempt/${att}`, { bearer: G })).data;
    const a1 = await stOf(g1.attempt), a2 = await stOf(g2.attempt);
    expect("성장 — 떠 둔 잠재력으로 싸운 테이프 → verified", a1.status === "verified", short({ a1: a1.status, out: vout.split(/\r?\n/).filter((l) => /verify 끝/.test(l)) }));
    expect("성장 — 잠재력을 뺀 테이프 → rejected(셋업이 다르다)", a2.status === "rejected", short({ a2: a2.status, why: a2.why }));

    // 탈퇴 — 성장 줄도 지운다
    r = await rq("DELETE", "/api/me", { bearer: G }); expect("성장 — 그레이스 탈퇴 → 204", r.status === 204);
    const left = sqlRun(`SELECT (SELECT COUNT(*) FROM prog_account WHERE user_id = ${uid}) + (SELECT COUNT(*) FROM prog_hero WHERE user_id = ${uid}) + (SELECT COUNT(*) FROM prog_earn_log WHERE user_id = ${uid}) + (SELECT COUNT(*) FROM prog_pot_log WHERE user_id = ${uid}) AS n`);
    expect("성장 — 탈퇴하면 계정 · 사도 잠재력 · 보상 기록 · 잠재력 기록 줄도 지움", /"n":\s*0/.test(left), short(left));
  } finally { stop(srv); }
}

async function main() {
  console.log(`wrangler = ${WRANGLER}\nbz = ${BZ}\n콘텐츠 = ${CONTENT}`);
  fs.rmSync(STATE, { recursive: true, force: true });
  fs.mkdirSync(STATIC, { recursive: true });
  fs.writeFileSync(path.join(STATIC, "index.html"), "<!doctype html><title>bolzena local</title>");

  // 봇 판 슬롯 — 같은 판의 사도 셋 두 묶음(사람 A · B)
  const botDir = path.join(OUT, "bot");
  fs.rmSync(botDir, { recursive: true, force: true });
  bz(["frontier", "24", "--data", CONTENT, "--fights", "1", "--fightbot", "smart", "--slotbot", "skilled", "--threads", "4", "--save", botDir]);
  const byRun = {};
  for (const f of fs.readdirSync(path.join(botDir, "slots"))) { const k = f.split("_").slice(0, 2).join("_"); (byRun[k] ||= []).push(path.join(botDir, "slots", f)); }
  const teams = Object.values(byRun).filter((l) => l.length === 3);
  if (teams.length < 2) throw new Error("완주한 봇 판이 둘이 안 된다");
  const slotA = teams[0].map((f) => JSON.parse(fs.readFileSync(f, "utf8")));
  const heroesB = new Set(slotA.map((s) => s.hero));
  const slotB = (teams.find((l, i) => i > 0 && l.every((f) => !heroesB.has(JSON.parse(fs.readFileSync(f, "utf8")).hero))) || teams[1]).map((f) => JSON.parse(fs.readFileSync(f, "utf8")));

  // ── 옛 표(0001~0004)에 옛 칸을 넣고 → 0005 를 건다(기존 칸 보존 시험) ──
  const oldDir = path.join(OUT, "oldmig");
  fs.rmSync(oldDir, { recursive: true, force: true });
  fs.mkdirSync(path.join(oldDir, "migrations"), { recursive: true });
  for (const f of fs.readdirSync(path.join(WEB, "migrations"))) if (f < "0005") fs.copyFileSync(path.join(WEB, "migrations", f), path.join(oldDir, "migrations", f));
  fs.writeFileSync(path.join(oldDir, "wrangler.toml"), fs.readFileSync(path.join(WEB, "test", "wrangler.test.toml"), "utf8").replace('migrations_dir = "../migrations"', 'migrations_dir = "migrations"'));
  run(Q(WRANGLER), ["d1", "migrations", "apply", "DB", "--local", "-c", `"${path.join(oldDir, "wrangler.toml")}"`, "--persist-to", `"${STATE}"`]);
  const legacy = [0, 1, 2].map((n) => JSON.stringify(n === 0 ? slotA[0] : { ...slotA[0], from: { ...slotA[0].from, ended: `2026-10-0${n}T00:00Z` } }));
  const legacyHash = legacy.map((t) => crypto.createHash("sha256").update(t).digest("hex").slice(0, 16));
  const sq = (x) => "'" + String(x).replace(/'/g, "''") + "'";
  const daveSub = crypto.createHmac("sha256", "local-pepper").update("test|dave").digest("hex");
  const legacySql = [
    `INSERT INTO users (id, provider, provider_sub, name, created, nick, nick_key, nick_changed) VALUES (1, 'test', ${sq(daveSub)}, '', 1, '데이브', '데이브', 1);`,
    ...legacy.map((t, n) => `INSERT INTO hero_slots (id, user_id, hero, n, json, hash, status, why, created, updated, checked) VALUES (${n + 1}, 1, ${sq(slotA[0].hero)}, ${n}, ${sq(t)}, ${sq(legacyHash[n])}, ${n === 2 ? "'pending'" : "'ok'"}, NULL, ${100 + n}, ${200 + n}, ${n === 2 ? "NULL" : 300});`),
    `INSERT INTO attempts (id, user_id, season_id, seed, slot_ids, heroes, slots_json, slot_hashes, day, started, expires, submitted) VALUES ('${"d".repeat(32)}', 1, 'preseason', 12345, '[1]', ${sq(JSON.stringify([slotA[0].hero]))}, ${sq(JSON.stringify([legacy[0]]))}, ${sq(JSON.stringify([legacyHash[0]]))}, '20261001', 1, 2, 0);`,
  ].join("\n");
  fs.writeFileSync(path.join(oldDir, "legacy.sql"), legacySql);
  run(Q(WRANGLER), ["d1", "execute", "DB", "--local", "-c", `"${path.join(oldDir, "wrangler.toml")}"`, "--persist-to", `"${STATE}"`, "--file", `"${path.join(oldDir, "legacy.sql")}"`]);
  if (MIG_UPTO) {   // 공개판 D1 흉내 — 그 번호까지만 건다(성장 켬 시험은 건너뜀)
    const upDir = path.join(OUT, "uptomig");
    fs.rmSync(upDir, { recursive: true, force: true });
    fs.mkdirSync(path.join(upDir, "migrations"), { recursive: true });
    for (const f of fs.readdirSync(path.join(WEB, "migrations"))) if (f.slice(0, 4) <= MIG_UPTO) fs.copyFileSync(path.join(WEB, "migrations", f), path.join(upDir, "migrations", f));
    fs.copyFileSync(path.join(oldDir, "wrangler.toml"), path.join(upDir, "wrangler.toml"));
    run(Q(WRANGLER), ["d1", "migrations", "apply", "DB", "--local", "-c", `"${path.join(upDir, "wrangler.toml")}"`, "--persist-to", `"${STATE}"`]);
  } else run(Q(WRANGLER), ["d1", "migrations", "apply", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`]);
  console.log(`로컬 D1 — 옛 마이그레이션(0001~0004) · 옛 칸 셋 · ${MIG_UPTO ? MIG_UPTO + " 까지" : "끝까지"}`);
  /** 서버가 떠 있는 동안 로컬 D1 에 SQL 파일을 건다(안전 한도 시험 — 칸 999개 채우기). */
  const sql = (name, text) => { const f = path.join(OUT, name); fs.writeFileSync(f, text); return run(Q(WRANGLER), ["d1", "execute", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`, "--file", `"${f}"`]); };

  const srv = startServer({ TEST_LOGIN: "1", ID_PEPPER: "local-pepper", VERIFY_TOKEN, ADMIN_TOKEN }, "dev.log");
  try {
    await waitUp();
    let r;
    // ── 로그인 ──
    r = await req("GET", "/api/me"); expect("로그인 전 /api/me → 401", r.status === 401);
    r = await req("GET", "/api/auth/test?sub=alice&back=/frontier");
    const A = cookieOf(r);
    expect("가짜 로그인(웹) → 302 + 세션 쿠키 HttpOnly · Secure · SameSite", r.status === 302 && !!A && /HttpOnly/.test(r.headers.get("set-cookie")) && /SameSite=Lax/.test(r.headers.get("set-cookie")), `location ${r.headers.get("location")}`);
    // ── 가입(닉네임) ──
    const name = (who, n, origin = BASE) => req("POST", "/api/me/name", { ...who, body: { name: n }, headers: { origin } });
    r = await req("GET", "/api/me", { cookie: A }); expect("첫 로그인 → 가입 전(needsName · 이름 없음)", r.status === 200 && r.data.needsName === true && r.data.user.name === null, short(r.data.user));
    r = await req("PUT", `/api/slots/${encodeURIComponent(slotA[0].hero)}/0`, { cookie: A, body: JSON.stringify(slotA[0]), headers: { "content-type": "application/json", origin: BASE } });
    expect("닉네임 없이 슬롯 올리기 → 403 needsName", r.status === 403 && r.data.needsName === true);
    r = await req("POST", "/api/frontier/start", { cookie: A, body: { slots: [{ hero: slotA[0].hero, n: 0 }] }, headers: { origin: BASE } });
    expect("닉네임 없이 프론티어 시작 → 403 needsName", r.status === 403 && r.data.needsName === true);
    r = await req("GET", "/api/frontier/ranking"); expect("닉네임 없어도 랭킹 읽기는 된다", r.status === 200);
    r = await name({ cookie: A }, "x", "https://evil.example"); expect("다른 사이트에서 온 쿠키 요청 → 403", r.status === 403);
    for (const [n, why] of [["<script>", "HTML"], ["a", "1자"], ["열세글자가넘는닉네임입니다요", "13자 넘음"], ["앨​리스", "보이지 않는 글자"], ["___", "글자 없음"]]) {
      r = await name({ cookie: A }, n); expect(`닉네임 규칙 위반(${why}) → 400`, r.status === 400, short(r.data));
    }
    for (const n of ["운영자", "ＡＤＭＩＮ", "볼 제나", "Bolzena_fan", "관리자님", "GM"]) {
      r = await name({ cookie: A }, n); expect(`금지어 「${n}」 → 400`, r.status === 400 && /사칭/.test(r.data.why), short(r.data));
    }
    r = await name({ cookie: A }, "  앨리스   01 "); expect("닉네임 정함(앞뒤 공백 정리 · 사이 공백 하나) → 가입 끝", r.status === 200 && r.data.user.name === "앨리스 01" && r.data.needsName === false, short(r.data.user));
    r = await req("GET", "/api/me", { cookie: A }); expect("/api/me(웹) — needsName false", r.status === 200 && r.data.user.name === "앨리스 01" && !r.data.needsName && r.data.session.kind === "web");
    r = await name({ cookie: A }, "앨리스 01"); expect("같은 이름 다시 → 200(바꾸기 횟수 안 씀)", r.status === 200);
    r = await name({ cookie: A }, "앨리스"); expect("하루 안에 또 바꾸기 → 429", r.status === 429 && r.data.retryAfter > 0, short(r.data));
    r = await req("GET", "/api/auth/test?sub=bob&kind=pc");
    const B = r.data.token; expect("가짜 로그인(PC Bearer) — 가입 전", r.status === 200 && typeof B === "string");
    r = await name({ bearer: B }, "앨리스01"); expect("다른 사람과 겹치는 닉네임(「앨리스01」 = 「앨리스 01」) → 409", r.status === 409, short(r.data));
    r = await name({ bearer: B }, "Ａｌｉｃｅ"); expect("전각 영문 → 반각으로 정함 「Alice」", r.status === 200 && r.data.user.name === "Alice", short(r.data.user));
    r = await req("GET", "/api/auth/test?sub=carol&kind=pc"); const C = r.data.token;
    r = await name({ bearer: C }, "ALICE"); expect("대소문자만 다른 닉네임(「ALICE」 vs 「Alice」) → 409", r.status === 409);
    r = await req("GET", "/api/me", { bearer: B }); expect("/api/me(PC)", r.status === 200 && r.data.session.kind === "pc" && r.data.user.name === "Alice");

    // ── 닉네임 겹침 뚫기(2026-10-19 사용자 「중복 닉네임이 안 되는지 시험」) — 지금 「앨리스 01」(A) · 「Alice」(B) 가 있다 ──
    const pcLogin = async (sub) => (await req("GET", `/api/auth/test?sub=${sub}&kind=pc`)).data.token;
    const N1 = await pcLogin("nk1"), N0 = await pcLogin("nk0");   // 닉네임 요청은 사람마다 분당 20번 — 두 사람에 나눠 낸다
    for (const [n, why] of [
      ["  ALICE ", "대소문자 · 앞뒤 공백"], ["A l i c e", "가운데 공백"], ["a_l-i.c e", "_ - . 섞음"], ["ＡＬＩＣＥ", "전각"], ["Ａ ｌｉｃｅ", "전각 + 공백"],
      ["앨리스01".normalize("NFD"), "한글 조합형(NFD)"], ["앨리스　01", "전각 공백"], ["앨리스 01", "줄 바꿈 없는 공백"],
      ["﻿Alice", "앞에 BOM"], ["Al﻿ice", "가운데 BOM(공백으로 바뀌어 같은 열쇠)"], ["ⓐlice", "동그라미 글자(NFKC → a)"],
    ]) { r = await name({ bearer: N1 }, n); expect(`겹침 뚫기 — ${why} ${JSON.stringify(n)} → 409`, r.status === 409, short(r.data)); }
    for (const [n, why] of [
      ["Ali​ce", "ZWSP"], ["Ali‍ce", "ZWJ"], ["Ali‌ce", "ZWNJ"], ["Ali­ce", "소프트 하이픈"], ["Ali⁠ce", "단어 잇기"], ["앨리스ㅤ", "한글 채움 문자"],
      ["Alíce", "결합 부호"], ["Аlice", "키릴 А"], ["Αlice", "그리스 Α"], ["Aliсe", "키릴 с"],
    ]) { r = await name({ bearer: N0 }, n); expect(`보이지 않는 · 다른 문자 글자 — ${why} → 400`, r.status === 400, short(r.data)); }
    r = await name({ bearer: N1 }, "ㅋㅋ교주"); expect("한글 호환 자모(「ㅋㅋ교주」) → 200(NFKC 가 자모를 깨뜨리던 것 고침)", r.status === 200 && r.data.user.name === "ㅋㅋ교주", short(r.data));
    await req("DELETE", "/api/me", { bearer: N0 });
    const N2 = await pcLogin("nk2");
    r = await name({ bearer: N2 }, "　ㅋㅋ 교주"); expect("호환 자모 이름과 공백만 다름 → 409", r.status === 409, short(r.data));
    r = await name({ bearer: N2 }, "ㅋㅋ교주".normalize("NFD")); expect("호환 자모 이름의 NFD → 409", r.status === 409, short(r.data));
    r = await name({ bearer: N2 }, "A1ice"); expect("비슷한 글자(숫자 1 vs l — 정책: 다른 이름으로 본다) → 200", r.status === 200 && r.data.user.name === "A1ice", short(r.data));
    // 동시에 같은 이름 — DB UNIQUE(users.nick_key) 가 하나만 들인다
    const racers = await Promise.all([3, 4, 5, 6, 7].map((i) => pcLogin("nk" + i)));
    const race = await Promise.all(racers.map((t, i) => name({ bearer: t }, i % 2 ? "동시 신청" : "동시신청")));
    const won = race.filter((x) => x.status === 200).length, lost = race.filter((x) => x.status === 409).length;
    expect("다섯이 동시에 같은 이름 → 하나만 200 · 넷은 409", won === 1 && lost === 4, race.map((x) => x.status).join(","));
    // 탈퇴하면 그 이름은 바로 풀린다(정책 — 문서 §닉네임)
    const winner = racers[race.findIndex((x) => x.status === 200)], loser = racers[race.findIndex((x) => x.status === 409)];
    r = await req("DELETE", "/api/me", { bearer: winner }); expect("동시 신청 이긴 사람 탈퇴 → 204", r.status === 204);
    r = await name({ bearer: loser }, "동시-신청"); expect("탈퇴한 사람의 이름 다시 쓰기 → 200", r.status === 200, short(r.data));
    for (const t of racers) if (t !== winner) await req("DELETE", "/api/me", { bearer: t });

    // ── 슬롯 ──
    const put = (who, s, n = 0, hero = s.hero) => req("PUT", `/api/slots/${encodeURIComponent(hero)}/${n}`, { ...who, body: JSON.stringify(s), headers: { "content-type": "application/json", origin: BASE } });
    for (const s of slotA) { r = await put({ cookie: A }, s); expect(`A 슬롯 올림 ${s.hero}/0 → pending`, r.status === 200 && r.data.status === "pending", short(r.data)); }
    for (const s of slotB) { r = await put({ bearer: B }, s); expect(`B 슬롯 올림 ${s.hero}/0`, r.status === 200 && r.data.status === "pending"); }
    r = await put({ cookie: A }, slotA[0], 0, slotA[1].hero); expect("주소의 사도와 슬롯 사도가 다름 → 400", r.status === 400, short(r.data));
    r = await put({ cookie: A }, { ...slotA[0], from: { ...slotA[0].from, result: "lose" } }, 2); expect("완주 아닌 판의 슬롯 → 400", r.status === 400, short(r.data));
    r = await put({ cookie: A }, slotA[0], -1); expect("옛 주소 칸 번호 -1 → 400", r.status === 400);
    r = await put({ cookie: A }, slotA[0], "01"); expect("옛 주소 칸 번호 모양(01) → 400", r.status === 400);
    r = await put({ cookie: A }, { ...slotA[0], cheat: 1 }, 2); expect("모르는 키 → 400", r.status === 400);
    r = await req("PUT", `/api/slots/${encodeURIComponent(slotA[0].hero)}/2`, { cookie: A, body: "x".repeat(17 * 1024), headers: { origin: BASE } }); expect("큰 슬롯 → 413", r.status === 413);
    // 위조 슬롯 — 고유 카드 원본을 한 장 더(규칙 위반 — 모양은 맞아 서버는 받고, 검증기가 bad 로)
    const forged = JSON.parse(JSON.stringify(slotA[0]));
    const uniq = forged.cards.find((c) => !c.id.includes("^") && !c.id.includes("@")) || forged.cards[0];
    forged.cards.push({ ...uniq });
    forged.growth = { atk: 9999 };
    r = await put({ cookie: A }, forged, 1); expect(`위조 슬롯(고유 카드 원본 두 장 · 성장 9999) 올림 ${forged.hero}/1 → pending(모양은 맞다)`, r.status === 200 && r.data.status === "pending");
    r = await req("GET", "/api/slots", { cookie: A }); expect("A 슬롯 목록 4칸", r.status === 200 && r.data.slots.length === 4);

    // ── 옛 칸 보존(마이그레이션 0005) — 0004 표에 넣은 데이브의 칸 셋 ──
    r = await req("GET", "/api/auth/test?sub=dave&kind=pc"); const D = r.data.token;
    expect("옛 사용자(데이브) 로그인 — 같은 사용자 줄", r.status === 200 && r.data.user === 1);
    r = await req("GET", "/api/slots", { bearer: D });
    const dv = r.data.slots || [];
    expect("옛 칸 셋 그대로 — id 1~3 · n 0~2 · 글 · 지문 · 상태 · 시각 · 이름 없음", r.status === 200 && dv.length === 3 && dv.every((x, i) => x.id === i + 1 && x.n === i && x.hero === slotA[0].hero && x.hash === legacyHash[i]
      && JSON.stringify(x.slot) === legacy[i] && x.status === (i === 2 ? "pending" : "ok") && x.created === 100 + i && x.updated === 200 + i && x.name === null), short(dv.map((x) => `${x.id}/${x.n}/${x.status}/${x.hash}`)));
    expect("목록 — 칸 수 · 한도(사도 10 · 계정 1,000)", r.data.count === 3 && r.data.max.perHero === 10 && r.data.max.perUser === 1000, short({ count: r.data.count, max: r.data.max }));
    r = await req("GET", `/api/frontier/attempt/${"d".repeat(32)}`, { bearer: D });
    expect("옛 시도의 편성도 그대로(떠 둔 글)", r.status === 200 && r.data.mine === true && r.data.status === "expired" && JSON.stringify(r.data.slots[0].slot) === legacy[0]);
    const newSlot = (who, s, name, hero = s.hero) => req("POST", `/api/slots?hero=${encodeURIComponent(hero)}${name !== undefined ? "&name=" + encodeURIComponent(name) : ""}`, { ...who, body: JSON.stringify(s), headers: { "content-type": "application/json", origin: BASE } });
    r = await newSlot({ bearer: D }, slotA[0]); expect("옛 칸 셋이 있는 사도에 새로 저장 → 넷째 칸(n 3 · id 새로)", r.status === 201 && r.data.n === 3 && r.data.id > 3 && r.data.status === "pending", short(r.data));
    r = await put({ bearer: D }, slotA[0], 1); expect("옛 주소로 칸 n 1 덮어쓰기 → 같은 id 2 · pending", r.status === 200 && r.data.id === 2 && r.data.n === 1 && r.data.status === "pending", short(r.data));
    r = await req("DELETE", `/api/slots/${encodeURIComponent(slotA[0].hero)}/2`, { bearer: D }); expect("옛 주소로 칸 n 2 지우기 → 204", r.status === 204);
    r = await req("GET", "/api/slots", { bearer: D }); expect("데이브 칸 — id 1 · 2 · 새 칸", r.data.slots.map((x) => x.id).join(",") === `1,2,${r.data.slots[2]?.id}` && r.data.slots[2]?.id > 3);

    // ── 새로 저장 · 이름 · 덮어쓰기 · 지우기(id) ──
    const h0 = slotA[0].hero;
    r = await newSlot({ cookie: A }, slotA[0], "  보스용   덱 "); const n1 = r.data;
    expect("새로 저장(이름 · 앞뒤 공백 정리) → 201 · 서버 id · 다음 차례 n 2", r.status === 201 && Number.isInteger(n1.id) && n1.n === 2 && n1.name === "보스용 덱" && n1.status === "pending" && n1.hash?.length === 16, short(n1));
    r = await newSlot({ cookie: A }, slotA[0]); const n2 = r.data;
    r = await newSlot({ cookie: A }, { ...slotA[0], from: { ...slotA[0].from, ended: "2026-10-17T00:00Z" } }); const n3 = r.data;
    expect("이름 없이 새로 저장 두 번 → n 3 · 4 · 이름 null", n2.n === 3 && n3.n === 4 && n2.name === null && n3.name === null && new Set([n1.id, n2.id, n3.id]).size === 3, short([n2, n3]));
    r = await req("GET", `/api/slots?hero=${encodeURIComponent(h0)}`, { cookie: A });
    expect("한 사도에 칸 다섯(예전 한도 3 넘음) · 차례 0~4", r.status === 200 && r.data.slots.length === 5 && r.data.slots.map((x) => x.n).join(",") === "0,1,2,3,4" && r.data.count === 7, short(r.data.slots.map((x) => `${x.id}:${x.n}:${x.name}`)));
    for (const [nm, why] of [["<b>x</b>", "HTML"], ["열일곱글자가넘는아주긴칸이름입니다", "17자"], ["___", "글자 없음"], ["앨\u200b리스", "보이지 않는 글자"]]) {
      r = await newSlot({ cookie: A }, slotA[0], nm); expect(`칸 이름 규칙 위반(${why}) → 400 · 칸 안 생김`, r.status === 400 && typeof r.data.why === "string", short(r.data));
    }
    r = await newSlot({ cookie: A }, slotA[1], undefined, h0); expect("새로 저장 — 주소 사도 ≠ 슬롯 사도 → 400", r.status === 400, short(r.data));
    r = await newSlot({ cookie: A }, { ...slotA[0], from: { ...slotA[0].from, result: "lose" } }); expect("새로 저장 — 완주 아닌 판 → 400", r.status === 400);
    r = await req("POST", `/api/slots?hero=${encodeURIComponent(h0)}`, { cookie: A, body: JSON.stringify(slotA[0]), headers: { "content-type": "application/json", origin: "https://evil.example" } }); expect("새로 저장 — 다른 사이트 → 403", r.status === 403);
    r = await req("GET", `/api/slots?hero=${encodeURIComponent(h0)}`, { cookie: A }); expect("거절된 새로 저장은 칸을 만들지 않는다", r.data.slots.length === 5);
    const slotApi = (who, method, id, body) => req(method, `/api/slot/${id}`, { ...who, body: body === undefined ? undefined : typeof body === "string" ? body : JSON.stringify(body), headers: { "content-type": "application/json", origin: BASE } });
    r = await slotApi({ cookie: A }, "PATCH", n2.id, { name: "Raid A-1" }); expect("이름 바꾸기 → 200 · 지문 · 차례 그대로", r.status === 200 && r.data.name === "Raid A-1" && r.data.hash === n2.hash && r.data.n === 3, short(r.data));
    r = await slotApi({ cookie: A }, "PATCH", n1.id, { name: "" }); expect("이름 지우기(빈 글) → null", r.status === 200 && r.data.name === null);
    r = await slotApi({ cookie: A }, "PATCH", n1.id, { name: "보스 덱!" }); expect("이름 규칙 위반(느낌표) → 400", r.status === 400 && /글자/.test(r.data.why), short(r.data));
    r = await slotApi({ bearer: B }, "PATCH", n2.id, { name: "남의 것" }); expect("남의 칸 이름 바꾸기 → 404", r.status === 404);
    const over = JSON.stringify({ ...slotA[0], from: { ...slotA[0].from, ended: "2026-10-16T00:00Z" } });
    r = await slotApi({ cookie: A }, "PUT", n2.id, over); expect("id 로 덮어쓰기 → 200 · 같은 id · n · 이름 그대로 · 지문 바뀜 · pending", r.status === 200 && r.data.id === n2.id && r.data.n === 3 && r.data.name === "Raid A-1" && r.data.hash !== n2.hash && r.data.status === "pending", short(r.data));
    r = await slotApi({ cookie: A }, "PUT", n2.id, slotA[1]); expect("id 로 덮어쓰기 — 다른 사도의 슬롯 → 400", r.status === 400, short(r.data));
    r = await slotApi({ bearer: B }, "PUT", n2.id, over); expect("남의 칸 덮어쓰기 → 404", r.status === 404);
    r = await slotApi({ cookie: A }, "PUT", "abc", over); expect("id 모양 틀림 → 400", r.status === 400);
    r = await slotApi({ cookie: A }, "DELETE", n3.id); expect("id 로 지우기 → 204", r.status === 204);
    r = await slotApi({ cookie: A }, "DELETE", n3.id); expect("지운 칸 다시 지우기 → 404", r.status === 404);
    r = await slotApi({ bearer: B }, "DELETE", n1.id); expect("남의 칸 지우기 → 404", r.status === 404);

    // ── 계정 안전 한도(1,000칸) — 캐롤에게 999칸을 D1 에 바로 채우고 API 로 하나 더 · 또 하나 ──
    r = await name({ bearer: C }, "캐롤"); expect("캐롤 가입", r.status === 200);
    sql("fill.sql", `INSERT INTO hero_slots (user_id, hero, n, json, hash, status, created, updated) WITH RECURSIVE c(i) AS (SELECT 0 UNION ALL SELECT i + 1 FROM c WHERE i < 998)
      SELECT (SELECT id FROM users WHERE nick = '캐롤'), 'filler', i, '{}', 'x', 'ok', 1, 1 FROM c;`);
    r = await req("GET", "/api/slots", { bearer: C }); expect("캐롤 999칸(D1 에 바로)", r.data.count === 999, `${r.data.count}`);
    r = await newSlot({ bearer: C }, slotB[0]); expect("1,000번째 새로 저장 → 201", r.status === 201, short(r.data));
    r = await newSlot({ bearer: C }, slotB[0]); expect("1,001번째 → 409 too many slots(max 1000)", r.status === 409 && r.data.max === 1000, short(r.data));
    r = await put({ bearer: C }, slotB[1], 0); expect("한도에서 옛 주소로 새 칸 → 409", r.status === 409);
    r = await put({ bearer: C }, slotB[0], 0); expect("한도에서도 있는 칸 덮어쓰기(옛 주소)는 된다", r.status === 200 && r.data.n === 0);

    // ── 사도마다 10칸(2026-10-19) — 새 사람 「프랜」: 10칸까지 새로 저장 · 11번째 409 hero slots full · 덮어쓰기 · 이름 · 지우기는 그대로 · 다른 사도는 따로 셈 ──
    //    한도 전에 10칸을 넘게 저장한 사람(공개판에서 생길 수 있는 경우 — D1 에 바로 12칸): 읽기 · 지우기는 되고 새로 저장만 10칸 아래로 줄 때까지 막힌다
    {
      r = await req("GET", "/api/auth/test?sub=fran&kind=pc"); const E = r.data.token;
      r = await name({ bearer: E }, "프랜"); expect("프랜 가입", r.status === 200, short(r.data));
      const he = slotA[0].hero, listE = async (hero = he) => (await req("GET", `/api/slots?hero=${encodeURIComponent(hero)}`, { bearer: E })).data;
      const made = [];
      for (let k = 0; k < 10; k++) { r = await newSlot({ bearer: E }, slotA[0]); if (r.status === 201) made.push(r.data); }
      expect("한 사도에 10칸까지 새로 저장 → 201 × 10 · 차례 0~9", made.length === 10 && made.map((x) => x.n).join(",") === "0,1,2,3,4,5,6,7,8,9", short(made.map((x) => x.n)));
      r = await newSlot({ bearer: E }, slotA[0], "열한째"); expect("11번째 새로 저장 → 409 hero slots full(max 10 · have 10)", r.status === 409 && r.data.error === "hero slots full" && r.data.max === 10 && r.data.have === 10, short(r.data));
      r = await put({ bearer: E }, slotA[0], 50); expect("옛 주소로 11번째 새 칸 → 409 hero slots full", r.status === 409 && r.data.error === "hero slots full", short(r.data));
      let le = await listE(); expect("거절된 11번째는 칸을 만들지 않는다 — 10칸 · 목록 한도 perHero 10", le.slots.length === 10 && le.max.perHero === 10, short({ n: le.slots.length, max: le.max }));
      const over10 = JSON.stringify({ ...slotA[0], from: { ...slotA[0].from, ended: "2026-10-15T00:00Z" } });
      r = await slotApi({ bearer: E }, "PUT", made[3].id, over10); expect("10칸에서도 id 로 덮어쓰기 → 200 · 같은 id", r.status === 200 && r.data.id === made[3].id, short(r.data));
      r = await put({ bearer: E }, slotA[0], 0); expect("10칸에서도 옛 주소로 있는 칸(n 0) 덮어쓰기 → 200", r.status === 200 && r.data.id === made[0].id, short(r.data));
      r = await slotApi({ bearer: E }, "PATCH", made[3].id, { name: "꽉 참" }); expect("10칸에서도 이름 바꾸기 → 200", r.status === 200 && r.data.name === "꽉 참");
      r = await newSlot({ bearer: E }, slotA[1]); expect("다른 사도는 따로 센다 — 새로 저장 → 201", r.status === 201 && r.data.hero === slotA[1].hero, short(r.data));
      r = await slotApi({ bearer: E }, "DELETE", made[9].id); expect("한 칸 지우기 → 204", r.status === 204);
      r = await newSlot({ bearer: E }, slotA[0]); expect("지워서 9칸 → 다시 새로 저장 → 201 · 차례 9(남은 가장 큰 n + 1)", r.status === 201 && r.data.n === 9, short(r.data));
      r = await newSlot({ bearer: E }, slotA[0]); expect("다시 10칸 → 409 hero slots full", r.status === 409 && r.data.error === "hero slots full");
      // 이미 넘은 경우 — D1 에 바로 두 칸 더(12칸)
      sql("over.sql", `INSERT INTO hero_slots (user_id, hero, n, json, hash, status, created, updated) SELECT id, '${he.replace(/'/g, "''")}', 100 + i, '{}', 'x', 'ok', 1, 1 FROM users, (SELECT 0 AS i UNION ALL SELECT 1) WHERE nick = '프랜';`);
      le = await listE(); expect("한도를 넘은 칸(12칸)도 목록에 다 보인다(읽기)", le.slots.length === 12, `${le.slots.length}`);
      r = await newSlot({ bearer: E }, slotA[0]); expect("12칸 — 새로 저장 → 409(have 12)", r.status === 409 && r.data.error === "hero slots full" && r.data.have === 12, short(r.data));
      r = await slotApi({ bearer: E }, "PUT", made[1].id, over10); expect("12칸 — 덮어쓰기는 된다", r.status === 200);
      const extra = le.slots.filter((x) => x.n >= 100);
      r = await slotApi({ bearer: E }, "DELETE", extra[0].id); expect("12칸 — 지우기는 된다 → 11칸", r.status === 204);
      r = await newSlot({ bearer: E }, slotA[0]); expect("11칸 — 아직 새로 저장 409(have 11)", r.status === 409 && r.data.have === 11, short(r.data));
      await slotApi({ bearer: E }, "DELETE", extra[1].id); await slotApi({ bearer: E }, "DELETE", made[2].id);
      r = await newSlot({ bearer: E }, slotA[0]); expect("10칸 아래(9칸)로 줄면 다시 새로 저장 → 201", r.status === 201, short(r.data));
      le = await listE(); expect("프랜 — 다시 10칸", le.slots.length === 10, `${le.slots.length}`);
    }
    // ── 교주 보드 진행(0007 — /api/me/progress) — 2026-10-21 보드를 없애 새로 쓰지 않는다(PUT 410) · 쌓인 줄은 지우지 않고 읽기 · 운영 보기 · 탈퇴 때만 지움 ──
    let carolId = null;
    {
      const prog = (who, method, body, origin = BASE) => req(method, "/api/me/progress", { ...who, body, headers: { origin } });
      const pv = { have: [3, 1, 0, 0], earned: [20, 4, 1, 0], runs: 2, ver: 2, keys: ["atk", "gold"], levels: [5, 1] };
      r = await prog({ bearer: C }, "GET"); expect("보드 진행 — 처음엔 없음(progress null · rev 0)", r.status === 200 && r.data.progress === null && r.data.rev === 0, short(r.data));
      r = await prog({}, "GET"); expect("보드 진행 — 로그인 전 → 401", r.status === 401);
      r = await prog({ bearer: C }, "PUT", { progress: pv, rev: 0 }); expect("보드 진행 — 올리기(PUT) → 410 board removed(새로 쓰지 않음)", r.status === 410 && r.data.error === "board removed", short(r.data));
      r = await prog({ bearer: C }, "GET"); expect("보드 진행 — 410 뒤에도 비어 있음(아무것도 안 씀)", r.status === 200 && r.data.progress === null && r.data.rev === 0, short(r.data));
      carolId = (await req("GET", "/api/me", { bearer: C })).data?.user?.id;
      // 보드가 살아 있던 때(0007) 올려 둔 줄 — 공개판 계정에 남아 있는 꼴. 지우지 않고 읽기만 된다
      const qf = path.join(OUT, "board_q.sql");
      fs.writeFileSync(qf, `INSERT INTO board_progress (user_id, json, rev, updated) VALUES (${Number(carolId)}, '${JSON.stringify(pv)}', 2, 1760000000);\n`);
      run(Q(WRANGLER), ["d1", "execute", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`, "--json", "--file", `"${qf}"`]);
      r = await prog({ bearer: C }, "PUT", { progress: { ...pv, levels: [6, 1] }, rev: 2 }); expect("보드 진행 — 옛 줄이 있어도 PUT → 410", r.status === 410, short(r.data));
      r = await prog({ bearer: C }, "GET"); expect("보드 진행 — 옛 줄은 그대로 읽힌다(rev 2 · 값 그대로)", r.status === 200 && r.data.rev === 2 && JSON.stringify(r.data.progress) === JSON.stringify(pv), short(r.data));
      r = await req("GET", `/api/admin/user?nick=${encodeURIComponent("캐롤")}`, { bearer: ADMIN_TOKEN });
      const cu = r.data.users?.[0];
      expect("운영 사용자 보기 — 보드 진행 있음 · 크기 · 판 2", r.status === 200 && cu?.id === carolId && cu?.board?.rev === 2 && cu.board.bytes > 50, short(cu?.board));
      r = await req("GET", `/api/admin/user?nick=${encodeURIComponent("앨리스 01")}`, { bearer: ADMIN_TOKEN }); expect("보드 진행 없는 사람 — board null", r.status === 200 && r.data.users?.[0]?.board === null, short(r.data.users?.[0]?.board));
    }
    r = await req("DELETE", "/api/me", { bearer: C }); expect("캐롤 탈퇴 → 칸 1,000개도 지움", r.status === 204);
    {
      const n = run(Q(WRANGLER), ["d1", "execute", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`, "--json", "--command", `"SELECT COUNT(*) AS n FROM board_progress WHERE user_id = ${Number(carolId)}"`]);
      expect("탈퇴하면 보드 진행도 지움", /"n":\s*0/.test(n) && carolId > 0, short(n));
    }

    // ── 프론티어 시작 ──
    const start = (who, refs) => req("POST", "/api/frontier/start", { ...who, body: { slots: refs }, headers: { origin: BASE } });
    const listA = (await req("GET", "/api/slots", { cookie: A })).data.slots;
    const refsA = slotA.map((s) => ({ id: listA.find((x) => x.hero === s.hero && x.n === 0).id }));
    r = await start({ cookie: A }, [refsA[0], { hero: slotA[0].hero, n: 1 }]); expect("같은 사도 둘(id 와 옛 { hero, n } 섞어) → 400", r.status === 400, short(r.data));
    r = await start({ bearer: D }, [{ id: 1 }, { id: 2 }]); expect("같은 사도의 두 칸(id 둘) → 400", r.status === 400, short(r.data));
    r = await start({ bearer: B }, [refsA[0]]); expect("남의 칸 id 로 시작 → 404", r.status === 404);
    r = await start({ bearer: D }, [{ id: 1.5 }]); expect("칸 참조 모양 틀림 → 400", r.status === 400);
    r = await start({ cookie: A }, [...refsA, refsA[0]]); expect("넷 → 400", r.status === 400);
    // 시작은 분당 6번 — 걸리면 창이 지날 때까지 기다렸다 다시(넷을 잇달아 연다). 하루 한도는 없다(2026-10-19 — left: null)
    const startOk = async (who, refs) => {
      for (;;) {
        const q = await start(who, refs);
        if (q.status !== 429) return q;
        await new Promise((ok) => setTimeout(ok, ((q.data && q.data.retryAfter) || 5) * 1000 + 500));
      }
    };
    const s1 = (await startOk({ cookie: A }, refsA)).data; expect("A 시도 1 시작(칸 id) → 씨앗 · 떠 둔 칸 id · 하루 한도 없음(left null)", typeof s1.seed === "number" && s1.season.preseason === true && s1.slots.every((x, i) => x.id === refsA[i].id) && s1.left === null, `씨앗 ${s1.seed} · left ${s1.left}`);
    expect("성장 끔(PROG 없음) — 시도 시작 응답에 prog 칸 없음(지금과 같은 셋업)", !("prog" in s1), short(Object.keys(s1)));
    r = await req("GET", "/api/prog", { cookie: A }); expect("성장 끔 — GET /api/prog → 404 prog off", r.status === 404 && r.data.error === "prog off", short(r.data));
    r = await req("POST", "/api/prog/earn", { cookie: A, body: { ref: "off-test-0001", tier: 1, result: "win", floor: 2, min: 30, party: ["아야"] }, headers: { origin: BASE } });
    expect("성장 끔 — 판 끝 보상 주소 → 404 prog off", r.status === 404, short(r.data));
    r = await req("POST", "/api/prog/potential", { cookie: A, body: { hero: "아야", node: "01" }, headers: { origin: BASE } });
    expect("성장 끔 — 잠재력 주소 → 404 prog off", r.status === 404 && r.data.error === "prog off", short(r.data));
    r = await req("GET", "/api/verify/pending?limit=1", { bearer: VERIFY_TOKEN });
    expect("성장 끔 — 검증기 대기 줄에 던전 칸 없음(던전 없앰)", r.status === 200 && !("dungeons" in r.data), short(Object.keys(r.data)));
    const s2 =(await startOk({ cookie: A }, refsA)).data;
    const s3 = (await startOk({ cookie: A }, refsA)).data;
    // 위조 슬롯으로 연 시도 — 검증 전(pending)이라 시작은 되지만, 테이프는 진짜 슬롯으로 싸운 것(슬롯 지문 · 셋업이 안 맞는다)
    const s4 = (await startOk({ cookie: A }, [{ hero: forged.hero, n: 1 }, refsA[1], refsA[2]])).data;
    expect("위조 슬롯(pending)으로 시도 시작", typeof s4.seed === "number");

    // 싸움 넷 먼저 — 진짜 테이프는 그 씨앗 · 떠 둔 슬롯으로 bz frontier
    const t1 = fight("a1", s1);
    const sc1 = JSON.parse(t1.text).score;
    // 위조 점수 — 진짜로 싸운 테이프의 점수만 부풀림(주장도 같게). 글을 그대로 고친다 — JSON.parse/stringify 로 다시 쓰면 「1.0」 이 「1」 이 되어
    // 셋업 견주기에서 먼저 걸린다(그건 그것대로 거절이지만, 여기서는 점수 위조만 시험한다).
    const t2text = fight("a2", s2).text;
    const t2score = JSON.parse(t2text).score + 20000;
    const t2forged = t2text.replace(/"score":\d+/, `"score":${t2score}`);
    // 위조 테이프 — 첫 「카드 내기」 수를 지움(점수 · 끝은 그대로)
    const t3text = fight("a3", s3).text;
    const t3forged = t3text.replace(/\{"k":"play"[^{}]*\},/, "");   // 수 하나는 중괄호 없는 평평한 객체
    const t4 = fight("a4", { ...s4, slots: [{ slot: slotA[0] }, s4.slots[1], s4.slots[2]] });

    // 분당 제출 한도(10번)에 걸리면 창이 지날 때까지 기다렸다 다시(상한 시험이 한 사람으로 여러 번 낸다)
    const submit = async (who, attempt, score, tape) => {
      for (;;) {
        const q = await req("POST", "/api/frontier/submit", { ...who, body: { attempt, score, tape }, headers: { origin: BASE } });
        if (q.status !== 429) return q;
        await new Promise((ok) => setTimeout(ok, ((q.data && q.data.retryAfter) || 5) * 1000 + 500));
      }
    };
    r = await submit({ cookie: A }, s1.attempt, sc1 + 1, t1.text); expect("주장 점수 ≠ 테이프 점수 → 400", r.status === 400, short(r.data));
    r = await submit({ cookie: A }, s2.attempt, sc1, t1.text); expect("서버가 준 씨앗이 아닌 테이프(시도 1 테이프를 시도 2 로) → 400", r.status === 400 && /씨앗/.test(r.data.error), short(r.data));
    // 제출 상한(migrations/0008 — 프리시즌 보스 HP 150,000 · 한 턴 90,000) — 받자마자 400, 시도는 「낸 것」 이 되지 않는다(아래에서 같은 시도를 다시 낸다)
    {
      const t = JSON.parse(t3text);
      const forge = (f) => { const x = JSON.parse(t3text); f(x); return JSON.stringify(x); };
      const capTry = async (why, f, re) => {
        const text = forge(f), x = JSON.parse(text);
        const q = await submit({ cookie: A }, s3.attempt, x.score, text);
        expect(`제출 상한 — ${why} → 400`, q.status === 400 && re.test(q.data.error + " " + (q.data.why || "")), short(q.data));
      };
      await capTry("점수 > 보스 최대 HP(150,001)", (x) => { x.score = 150001; }, /보스 최대 HP 150000/);
      await capTry("낸 턴 > 시즌 턴(11)", (x) => { x.played = 11; }, /낸 턴 11/);
      await capTry("낸 턴 0", (x) => { x.played = 0; x.over = "lose"; }, /낸 턴 0/);
      await capTry("시간 끝인데 낸 턴 5", (x) => { x.over = "time"; x.played = 5; x.input.digests = x.input.digests.slice(0, 6); }, /시간 끝인데/);
      await capTry("턴 지문이 시즌 턴보다 많음", (x) => { x.over = "time"; x.played = 10; while (x.input.digests.length < 14) x.input.digests.push("0".repeat(16)); }, /턴 지문 14개/);
      await capTry("턴 지문이 낸 턴보다 적음", (x) => { x.over = "lose"; x.played = 8; x.input.digests = x.input.digests.slice(0, 3); }, /턴 지문 3개/);
      await capTry("한 턴 상한(1턴에 95,000)", (x) => { x.over = "lose"; x.played = 1; x.score = 95000; x.input.digests = x.input.digests.slice(0, 2); }, /한 턴 상한 90000/);
      for (const [why, sc] of [["음수", -5], ["소수", 1.5], ["글", "100"]]) {
        const q = await submit({ cookie: A }, s3.attempt, sc, forge((x) => { x.score = sc; }));
        expect(`제출 상한 — 점수 ${why} → 400`, q.status === 400, short(q.data));
      }
      let big = { status: 429 };
      for (let k = 0; big.status === 429; k++) { if (k) await new Promise((ok) => setTimeout(ok, ((big.data && big.data.retryAfter) || 5) * 1000 + 500)); big = await req("POST", "/api/frontier/submit", { cookie: A, body: JSON.stringify({ attempt: s3.attempt, score: t.score, tape: t3text.replace('"input":{', '"input":{"pad":"' + "x".repeat(260 * 1024) + '",') }), headers: { "content-type": "application/json", origin: BASE } }); }
      expect("제출 상한 — 테이프 256KB 넘음 → 413", big.status === 413, short(big.data));
    }
    // 넷을 주장 점수가 낮은 차례로 낸다 — 낼 때마다 내 최고보다 높아 모두 검증 대기(pending). 검증은 자기 최고 후보만(lib/score.js — 아래 「승격」 시험)
    const sendA = [[s1, sc1, t1.text, "진짜"], [s2, t2score, t2forged, "위조 점수"], [s3, JSON.parse(t3text).score, t3forged, "위조 테이프(첫 카드 내기 지움)"], [s4, JSON.parse(t4.text).score, t4.text, "위조 슬롯 시도"]]
      .sort((x, y) => x[1] - y[1]);
    const idOf = {};
    for (const [sx, c, text, what] of sendA) {
      r = await submit({ cookie: A }, sx.attempt, c, text);
      expect(`A ${what} 제출(주장 ${c} — 낮은 차례) → 202 pending(서버는 모양만 본다)`, r.status === 202 && r.data.status === "pending", short(r.data));
      idOf[sx.attempt] = r.data.id;
    }
    expect("위조 테이프는 진짜보다 짧다", t3forged.length < t3text.length);
    const idReal = idOf[s1.attempt], idFakeScore = idOf[s2.attempt], idFakeTape = idOf[s3.attempt], idFakeSlot = idOf[s4.attempt];
    r = await submit({ cookie: A }, s1.attempt, sc1, t1.text); expect("같은 시도 두 번 → 409", r.status === 409);
    r = await submit({ bearer: B }, s1.attempt, sc1, t1.text); expect("남의 시도 → 404", r.status === 404);

    // B — 진짜 하나(PC Bearer)
    const sB = (await start({ bearer: B }, slotB.map((s) => ({ hero: s.hero, n: 0 })))).data;
    const tB = fight("b1", sB);
    r = await submit({ bearer: B }, sB.attempt, JSON.parse(tB.text).score, tB.text); expect(`B 진짜 테이프 제출(${tB.line})`, r.status === 202);
    const idB = r.data.id;

    // 검증 전 — 곧바로 랭킹에 「검증 전」(verified false)으로. 사람마다 가장 높은 줄 — A 는 부풀린 점수(t2score)가 최고 줄
    r = await req("GET", "/api/frontier/ranking");
    // A 가 낸 차례(같은 점수면 먼저 낸 것) — 위조 점수는 진짜 + 20,000 이라 대개 이것이 최고
    const topA = [[s1.attempt, sc1], [s2.attempt, t2score], [s3.attempt, JSON.parse(t3text).score], [s4.attempt, JSON.parse(t4.text).score]].reduce((m, x) => (x[1] > m[1] ? x : m));
    const pre = r.data.rows || [], preA = pre.find((x) => x.name === "앨리스 01"), preB = pre.find((x) => x.name === "Alice");
    expect("검증 전 랭킹 — 둘 다 「검증 전」 으로 오름 · 대기 5", r.status === 200 && pre.length === 2 && pre.every((x) => x.verified === false) && r.data.pending === 5, short(pre));
    expect("검증 전 랭킹 — 사람마다 최고 줄 하나(A 는 검증 전 최고 = 부풀린 점수 시도)", preA?.score === topA[1] && preA.attempt === topA[0] && pre[0].score >= pre[1].score, short(preA));
    r = await req("GET", "/api/frontier/me", { cookie: A });
    expect("검증 전 /api/frontier/me — 최고 = 검증 전 줄(verified false) · 순위", r.data.best?.score === preA?.score && r.data.best?.verified === false && r.data.rank === preA?.rank, short({ best: r.data.best, rank: r.data.rank }));
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`, { cookie: A });
    expect("남의 검증 전 시도 보기 → 200 · 줄만(편성 없음 · 주장 점수 · pending)", r.status === 200 && r.data.mine === false && r.data.status === "pending" && r.data.slots === null && r.data.score === JSON.parse(tB.text).score && !("claimed" in r.data) && !("seed" in r.data) && !("tape" in r.data), short(r.data));
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`); expect("로그인 없이 남의 검증 전 시도 → 200 · 편성 없음", r.status === 200 && r.data.slots === null);
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`, { bearer: B }); expect("내 검증 전 시도 → 편성까지", r.status === 200 && r.data.mine === true && r.data.slots.length === 3 && r.data.status === "pending");

    // ── 검증기 ──
    r = await req("GET", "/api/verify/pending", { bearer: "wrong-token" }); expect("검증기 주소 — 틀린 토큰 → 401", r.status === 401);
    r = await req("GET", "/api/verify/pending", { cookie: A }); expect("검증기 주소 — 사용자 쿠키 → 401", r.status === 401);
    const vlog = path.join(OUT, "verify.log"); fs.rmSync(vlog, { force: true });
    const vout = bz(["verify", "--data", CONTENT, "--url", BASE, "--token", VERIFY_TOKEN, "--log", vlog]);
    console.log(vout.trim().split(/\r?\n/).map((l) => "  │ " + l).join("\n"));
    expect("bz verify 한 번 돌고 끝", /verify 끝/.test(vout));

    // ── 결과 ──
    r = await req("GET", "/api/slots", { cookie: A });
    const st = Object.fromEntries(r.data.slots.map((s) => [`${s.hero}/${s.n}`, s]));
    expect("A 진짜 슬롯 셋 → ok", slotA.every((s) => st[`${s.hero}/0`]?.status === "ok"));
    expect("위조 슬롯 → bad", st[`${forged.hero}/1`]?.status === "bad", short(st[`${forged.hero}/1`]?.why || ""));
    r = await req("GET", "/api/frontier/me", { cookie: A });
    const mine = Object.fromEntries(r.data.scores.map((s) => [s.id, s]));
    expect("A 진짜 점수 → verified", mine[idReal]?.status === "verified" && mine[idReal]?.score === sc1, `${mine[idReal]?.score}`);
    expect("위조 점수 → rejected(점수가 다르다)", mine[idFakeScore]?.status === "rejected" && /점수가 다르다/.test(mine[idFakeScore]?.why), short(mine[idFakeScore]?.why || ""));
    expect("위조 테이프 → rejected(어긋난 턴)", mine[idFakeTape]?.status === "rejected" && mine[idFakeTape]?.turn >= 0, short(`${mine[idFakeTape]?.why} · 턴 ${mine[idFakeTape]?.turn}`));
    expect("위조 슬롯 시도 → rejected(슬롯 지문)", mine[idFakeSlot]?.status === "rejected" && /슬롯/.test(mine[idFakeSlot]?.why), short(mine[idFakeSlot]?.why || ""));
    expect("/api/frontier/me 최고 · 순위(검증 뒤 — 거절된 줄은 빠짐)", r.data.best?.score === sc1 && r.data.best?.verified === true && r.data.rank >= 1 && r.data.today.used === 4, short({ best: r.data.best, rank: r.data.rank, today: r.data.today }));
    r = await req("GET", "/api/frontier/me", { bearer: B });
    expect("B 점수 → verified", r.data.scores.find((s) => s.id === idB)?.status === "verified");
    r = await req("GET", "/api/frontier/ranking?season=preseason");
    const rk = r.data.rows;
    expect("검증 뒤 랭킹 — 검증된 둘(표시 사라짐) · 거절된 A 의 부풀린 줄은 빠지고 검증된 sc1 이 A 줄 · 점수 내림차순 · 프리시즌", rk.length === 2 && rk.every((x) => x.verified === true) && rk.find((x) => x.name === "앨리스 01")?.score === sc1 && rk[0].score >= rk[1].score && r.data.season.preseason && r.data.pending === 0,
      rk.map((x) => `${x.rank}위 ${x.name} ${x.score} [${x.heroes.join(",")}]`).join(" / "));

    // ── 기록 보기 — 내 기록 · 랭킹 줄의 편성 · 칸별 최고 ──
    expect("랭킹 줄마다 시도 id", rk.every((x) => /^[0-9a-f]{32}$/.test(x.attempt)) && rk.some((x) => x.attempt === s1.attempt) && rk.some((x) => x.attempt === sB.attempt));
    const hidden = (d) => !("seed" in d) && !("tape" in d) && !("user_id" in d) && !("userId" in d) && !("slot_ids" in d) && !JSON.stringify(d).includes(String(s1.seed));
    r = await req("GET", `/api/frontier/attempt/${s1.attempt}`, { bearer: B });
    expect("남(B)이 A 의 검증된 시도 보기 → 200 · 닉네임 · 편성 셋(카드 · 장비 · 성장)", r.status === 200 && r.data.mine === false && r.data.name === "앨리스 01" && r.data.score === sc1 && r.data.status === "verified"
      && r.data.slots.length === 3 && r.data.slots.every((x, i) => x.hero === slotA[i].hero && Array.isArray(x.slot.cards) && x.slot.cards.length === slotA[i].cards.length && x.slot.kind === "bolzena-hero-slot"), short({ ...r.data, slots: r.data.slots?.map((x) => x.hero) }));
    expect("남에게는 씨앗 · 테이프 · 내부 번호 · 슬롯의 뜬 판 씨앗 · 주장 점수 · 까닭을 주지 않는다", r.status === 200 && hidden(r.data) && r.data.slots.every((x) => !("seed" in (x.slot.from || {}))) && !("claimed" in r.data) && !("why" in r.data),
      Object.keys(r.data).join(","));
    expect("남의 편성 — 끝 · 낸 턴", ["win", "lose", "time"].includes(r.data.over) && Number.isInteger(r.data.played), `${r.data.over} · ${r.data.played}턴`);
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`); expect("로그인 없이 B 의 검증된 시도 보기 → 200", r.status === 200 && r.data.name === "Alice" && r.data.mine === false);
    r = await req("GET", `/api/frontier/attempt/${s2.attempt}`, { bearer: B }); expect("남의 거절된 시도 → 404", r.status === 404);
    r = await req("GET", `/api/frontier/attempt/${s2.attempt}`); expect("로그인 없이 남의 거절된 시도 → 404", r.status === 404);
    r = await req("GET", `/api/frontier/attempt/${s4.attempt}`, { bearer: B }); expect("남의 거절된 시도(위조 슬롯) → 404", r.status === 404);
    r = await req("GET", `/api/frontier/attempt/${s2.attempt}`, { cookie: A });
    expect("내 거절된 시도 보기 → 200 · 거절 까닭 · 주장 점수 · 씨앗 · 테이프는 없음", r.status === 200 && r.data.mine === true && r.data.status === "rejected" && /점수가 다르다/.test(r.data.why) && r.data.claimed === t2score && !("seed" in r.data) && !("tape" in r.data), short(r.data.why));
    r = await req("GET", `/api/frontier/attempt/${s1.attempt}`, { cookie: A });
    expect("내 검증된 시도 보기 → mine · 뜬 판 씨앗은 그대로", r.status === 200 && r.data.mine === true && r.data.score === sc1 && r.data.slots[0].slot.from.seed === slotA[0].from.seed);
    r = await req("GET", "/api/frontier/attempt/xyz"); expect("시도 id 모양이 틀림 → 400", r.status === 400);
    r = await req("GET", "/api/frontier/attempt/" + "0".repeat(32)); expect("없는 시도 → 404", r.status === 404);
    r = await req("GET", "/api/frontier/history"); expect("로그인 없이 내 기록 → 401", r.status === 401);
    r = await req("GET", "/api/frontier/history", { cookie: A });
    const hA = r.data.rows || [];
    const hBy = Object.fromEntries(hA.map((x) => [x.attempt, x]));
    expect("A 내 기록 — 시도 넷 · 최근 순 · 더 없음", r.status === 200 && hA.length === 4 && hA.every((x, i) => i === 0 || hA[i - 1].at >= x.at) && r.data.more === false && r.data.page === 0,
      hA.map((x) => `${x.status}${x.score ?? x.claimed}`).join(" / "));
    expect("A 내 기록 — 검증 · 거절 까닭 · 끝 · 턴", hBy[s1.attempt]?.status === "verified" && hBy[s1.attempt]?.score === sc1 && ["win", "lose", "time"].includes(hBy[s1.attempt]?.over)
      && hBy[s2.attempt]?.status === "rejected" && /점수가 다르다/.test(hBy[s2.attempt]?.why) && hBy[s3.attempt]?.status === "rejected" && hBy[s3.attempt]?.turn >= 0 && hBy[s4.attempt]?.status === "rejected", short(hBy[s3.attempt]));
    expect("내 기록에는 씨앗 · 테이프가 없다", hA.every((x) => !("seed" in x) && !("tape" in x)));
    r = await req("GET", "/api/frontier/history", { bearer: B }); expect("B 내 기록 — 자기 것 하나만", r.status === 200 && r.data.rows.length === 1 && r.data.rows[0].attempt === sB.attempt);
    r = await req("GET", "/api/frontier/history?page=1", { cookie: A }); expect("내 기록 다음 쪽 → 빈 줄", r.status === 200 && r.data.rows.length === 0 && r.data.page === 1);
    r = await req("GET", "/api/frontier/history?page=-1", { cookie: A }); expect("쪽 번호 틀림 → 400", r.status === 400);
    // 칸별 최고 — 서버가 검증한 점수만, 슬롯 지문 기준
    r = await req("GET", "/api/slots", { cookie: A });
    const bst = Object.fromEntries(r.data.slots.map((s) => [`${s.hero}/${s.n}`, s]));
    expect("칸별 최고 — 진짜 칸 셋 = 검증된 점수(그 시도)", r.data.bestSeason === "preseason" && slotA.every((s) => bst[`${s.hero}/0`]?.best?.score === sc1 && bst[`${s.hero}/0`]?.best?.attempt === s1.attempt),
      slotA.map((s) => `${s.hero}:${JSON.stringify(bst[`${s.hero}/0`]?.best)}`).join(" / "));
    expect("칸별 최고 — 거절만 있는 위조 칸 → 없음(null)", bst[`${forged.hero}/1`] && bst[`${forged.hero}/1`].best === null);
    r = await start({ cookie: A }, [{ hero: forged.hero, n: 1 }]);
    if (r.status === 429) { await new Promise((ok) => setTimeout(ok, (r.data.retryAfter + 1) * 1000)); r = await start({ cookie: A }, [{ hero: forged.hero, n: 1 }]); }
    expect("bad 슬롯으로 시작 → 409", r.status === 409, short(r.data));
    // 분당 시작 한도(6번) — 창 경계에 따라 몇 번째에 걸릴지 다르니 걸릴 때까지 두드린다(bad 슬롯이라 시도는 생기지 않는다)
    let hit = 0;
    for (let i = 0; i < 8 && r.status !== 429; i++) { r = await start({ cookie: A }, [{ hero: forged.hero, n: 1 }]); hit++; }
    expect("분당 시작 한도(6번) → 429", r.status === 429 && hit <= 6, `${hit}번째 · retryAfter ${r.data.retryAfter}`);

    // 검증기를 다시 돌려도 더 할 것이 없다
    const v2 = bz(["verify", "--data", CONTENT, "--url", BASE, "--token", VERIFY_TOKEN]);
    expect("두 번째 bz verify — 할 것 없음", /verified 0 · rejected 0/.test(v2) && /ok 0 · bad 0/.test(v2));
    // 칸을 덮어쓰면(검증 대기로 돌아간다 — 그래서 두 번째 검증 뒤에) 칸별 최고는 새로 센다
    r = await put({ cookie: A }, { ...slotA[2], from: { ...slotA[2].from, ended: "2026-10-18T00:00Z" } }); expect("A 칸 덮어씀(다른 글)", r.status === 200);
    r = await req("GET", "/api/slots", { cookie: A });
    const bst2 = Object.fromEntries(r.data.slots.map((s) => [`${s.hero}/${s.n}`, s]));
    expect("덮어쓴 칸은 지문이 바뀌어 최고가 비고, 다른 칸은 그대로", bst2[`${slotA[2].hero}/0`]?.best === null && bst2[`${slotA[0].hero}/0`]?.best?.score === sc1);
    r = await req("GET", `/api/frontier/attempt/${s1.attempt}`, { bearer: B }); expect("덮어써도 지난 시도의 편성은 떠 둔 글 그대로", r.status === 200 && r.data.slots[2].slot.from.ended === slotA[2].from.ended);

    // ── 검증은 자기 최고 후보만 · 거절되면 다음 후보(2026-10-19 사용자) — 새 사람 P 로 따로(끝나면 탈퇴) ──
    {
      const P = await pcLogin("promo");
      r = await name({ bearer: P }, "승격시험"); expect("P 가입", r.status === 200);
      const pRefs = [];
      for (const sl of slotA) { const q = await newSlot({ bearer: P }, sl); pRefs.push({ id: q.data.id }); }
      const ps = [];
      for (let k = 0; k < 4; k++) ps.push((await startOk({ bearer: P }, pRefs)).data);
      expect("P 시도 넷 시작(하루 한도 없음)", ps.every((x) => typeof x.seed === "number" && x.left === null));
      const tp = ps.map((x, k) => fight("p" + k, x).text);
      // 진짜 셋을 낮은 차례로: lo · mid · hi, 넷째는 위조 점수 149,999(가장 높음 · 상한 안 — 검증기가 거절)
      const reals = [0, 1, 2].map((k) => ({ s: ps[k], text: tp[k], c: JSON.parse(tp[k]).score })).sort((x, y) => x.c - y.c);
      const [lo, mid, hi] = reals;
      const fakeC = 149999, fakeText = tp[3].replace(/"score":\d+/, `"score":${fakeC}`);
      const st = {};
      r = await submit({ bearer: P }, lo.s.attempt, lo.c, lo.text); st.lo = r.data; expect(`P 첫 점수(${lo.c}) → pending(최고가 없으니 후보)`, r.status === 202 && r.data.status === "pending", short(r.data));
      r = await submit({ bearer: P }, ps[3].attempt, fakeC, fakeText); st.fake = r.data; expect("P 위조 149,999 → pending(내 최고보다 높음)", r.status === 202 && r.data.status === "pending", short(r.data));
      r = await submit({ bearer: P }, mid.s.attempt, mid.c, mid.text); st.mid = r.data; expect(`P ${mid.c}(내 최고 149,999 보다 낮음) → skipped`, r.status === 202 && r.data.status === "skipped", short(r.data));
      r = await submit({ bearer: P }, hi.s.attempt, hi.c, hi.text); st.hi = r.data; expect(`P ${hi.c} → skipped`, r.status === 202 && r.data.status === "skipped", short(r.data));
      r = await req("GET", "/api/verify/pending", { bearer: VERIFY_TOKEN });
      const pend = (r.data.scores || []).map((x) => x.id);
      expect("검증기 대기열 — 후보 둘만(skipped 는 안 줌)", pend.includes(st.lo.id) && pend.includes(st.fake.id) && !pend.includes(st.mid.id) && !pend.includes(st.hi.id), JSON.stringify(pend));
      r = await req("GET", "/api/frontier/ranking");
      expect("랭킹 P 줄 = 위조 149,999 「검증 전」", r.data.rows.some((x) => x.name === "승격시험" && x.score === fakeC && x.verified === false), short(r.data.rows.map((x) => `${x.name}:${x.score}:${x.verified}`)));
      r = await req("GET", "/api/slots", { bearer: P });
      expect("칸별 최고 — 검증된 것이 없으면 가장 높은 검증 전(pending) 줄", r.data.slots.every((x) => x.best?.score === fakeC && x.best.verified === false && x.best.status === "pending"), short(r.data.slots.map((x) => x.best)));
      // 검증 한 번 — lo verified · 위조 rejected → 다음 후보 hi(남은 최고 lo 보다 높음)가 pending 으로
      let vo = bz(["verify", "--data", CONTENT, "--url", BASE, "--token", VERIFY_TOKEN]);
      r = await req("GET", "/api/frontier/me", { bearer: P });
      const pm = Object.fromEntries(r.data.scores.map((x) => [x.id, x.status]));
      expect("검증 뒤 — lo verified · 위조 rejected · hi 승격(pending) · mid 그대로 skipped", pm[st.lo.id] === "verified" && pm[st.fake.id] === "rejected" && pm[st.hi.id] === "pending" && pm[st.mid.id] === "skipped", JSON.stringify(pm));
      r = await req("GET", "/api/frontier/ranking");
      expect("랭킹 P 줄 = 승격된 hi 「검증 전」(거절된 위조는 빠짐)", r.data.rows.some((x) => x.name === "승격시험" && x.score === hi.c && x.verified === false), short(r.data.rows.map((x) => `${x.name}:${x.score}:${x.verified}`)));
      r = await req("GET", "/api/slots", { bearer: P });
      expect("칸별 최고 — 검증된 것 우선(더 높은 hi 가 검증 전이어도 lo)", r.data.slots.every((x) => x.best?.score === lo.c && x.best.verified === true), short(r.data.slots.map((x) => x.best)));
      r = await req("GET", "/api/frontier/history", { bearer: P });
      expect("내 기록 — skipped 줄(검증 안 함)", r.data.rows.find((x) => x.attempt === mid.s.attempt)?.status === "skipped");
      r = await req("GET", `/api/frontier/attempt/${mid.s.attempt}`, { bearer: A }); expect("남의 skipped 시도 → 404(랭킹에 없다)", r.status === 404);
      // 두 번째 검증 — 승격된 hi verified
      vo = bz(["verify", "--data", CONTENT, "--url", BASE, "--token", VERIFY_TOKEN]);
      r = await req("GET", "/api/frontier/ranking");
      expect("두 번째 검증 뒤 랭킹 P 줄 = hi 검증됨", r.data.rows.some((x) => x.name === "승격시험" && x.score === hi.c && x.verified === true), short(vo.split(/\r?\n/).filter((l) => /점수 #|끝/.test(l))));
      // 운영 거절도 다음 후보를 올린다 — hi 를 거절하면 mid(남은 최고 lo 보다 높음)
      r = await req("POST", "/api/admin/act", { bearer: ADMIN_TOKEN, body: { action: "score.reject", attempt: hi.s.attempt, reason: "시험 — 다음 후보" } });
      expect("운영 score.reject → 다음 후보 mid 를 올림(promoted)", r.status === 200 && r.data.promoted === st.mid.id, short({ promoted: r.data.promoted, mid: st.mid.id }));
      r = await req("GET", "/api/frontier/me", { bearer: P });
      expect("mid → pending", r.data.scores.find((x) => x.id === st.mid.id)?.status === "pending");
      vo = bz(["verify", "--data", CONTENT, "--url", BASE, "--token", VERIFY_TOKEN]);
      r = await req("DELETE", "/api/me", { bearer: P }); expect("P 탈퇴(뒤 시험의 랭킹 수에 안 끼게)", r.status === 204);
    }

    // ── 운영 주소(/api/admin) · bz admin ──
    const adm = (method, p, body, token = ADMIN_TOKEN, headers = {}) => req(method, p, { bearer: token || undefined, body, headers });
    const act = (body) => adm("POST", "/api/admin/act", body);
    const uidA = (await req("GET", "/api/me", { cookie: A })).data.user.id, uidB = (await req("GET", "/api/me", { bearer: B })).data.user.id;
    const rankNames = async () => (await req("GET", "/api/frontier/ranking")).data.rows.map((x) => x.name);
    r = await adm("GET", `/api/admin/user?id=${uidA}`, undefined, null); expect("운영 — 토큰 없음 → 401", r.status === 401);
    r = await adm("GET", `/api/admin/user?id=${uidA}`, undefined, "wrong-" + ADMIN_TOKEN); expect("운영 — 틀린 토큰 → 401", r.status === 401);
    r = await adm("GET", `/api/admin/user?id=${uidA}`, undefined, VERIFY_TOKEN); expect("운영 — 검증기 토큰 → 401(따로)", r.status === 401);
    r = await req("GET", `/api/admin/user?id=${uidA}`, { cookie: A }); expect("운영 — 사용자 쿠키 → 401", r.status === 401);
    r = await adm("GET", `/api/admin/user?id=${uidA}`, undefined, ADMIN_TOKEN, { origin: "https://evil.example" }); expect("운영 — 다른 사이트 요청 → 403", r.status === 403);
    r = await adm("GET", "/api/admin/user?id=1&nick=x"); expect("운영 찾기 — 열쇠 둘 → 400", r.status === 400);
    // 찾기
    r = await adm("GET", "/api/admin/user?nick=" + encodeURIComponent("앨리스01"));
    const ua = r.data.users?.[0];
    expect("운영 찾기 — 닉네임(겹침 열쇠 같은 것) → 앨리스 01 · 슬롯 · 시도 · 점수 · 최근 시도", r.status === 200 && r.data.exact === true && ua?.id === uidA && ua.name === "앨리스 01" && ua.slots.count >= 5 && ua.attempts === 4 && ua.scores.verified === 1 && ua.recent.length === 4 && ua.ban === null && ua.lastActive >= ua.created,
      short({ id: ua?.id, slots: ua?.slots, attempts: ua?.attempts, scores: ua?.scores, best: ua?.best }));
    expect("운영 찾기 — OAuth 번호 · 세션 토큰은 내보내지 않는다", r.status === 200 && !/provider_sub|token_hash|ID_PEPPER/.test(JSON.stringify(r.data)) && !JSON.stringify(r.data).includes(crypto.createHmac("sha256", "local-pepper").update("test|alice").digest("hex")));
    r = await adm("GET", "/api/admin/user?nick=" + encodeURIComponent("앨리")); expect("운영 찾기 — 부분 일치(「앨리」)", r.status === 200 && r.data.exact === false && r.data.users.some((u) => u.id === uidA));
    r = await adm("GET", `/api/admin/user?attempt=${s1.attempt}`); expect("운영 찾기 — 랭킹 줄의 시도 id → 그 사람", r.status === 200 && r.data.users[0]?.id === uidA);
    r = await adm("GET", `/api/admin/user?slot=${n1.id}`); expect("운영 찾기 — 칸 id → 그 사람", r.status === 200 && r.data.users[0]?.id === uidA);
    r = await adm("GET", "/api/admin/user?id=999999"); expect("운영 찾기 — 없는 번호 → 빈 목록", r.status === 200 && r.data.users.length === 0);
    // 까닭 · 조치 이름
    r = await act({ action: "rename", user: uidB, name: "보브" }); expect("운영 조치 — 까닭 없음 → 400", r.status === 400 && r.data.error === "reason required");
    r = await act({ action: "rename", user: uidB, name: "보브", reason: " " }); expect("운영 조치 — 빈 까닭 → 400", r.status === 400);
    r = await act({ action: "nuke", user: uidB, reason: "시험" }); expect("운영 조치 — 모르는 조치 → 400", r.status === 400);
    // 닉네임 강제 변경(미리 보기 → 실제) — B 는 방금 정해서 본인은 하루 동안 못 바꾼다
    r = await act({ action: "rename", user: uidB, name: "보브", reason: "시험 — 미리 보기", dry: true });
    expect("닉네임 강제 변경 미리 보기 → plan · 안 바뀜 · 기록 없음", r.status === 200 && r.data.dry === true && r.data.plan.from === "Alice" && r.data.plan.to === "보브" && r.data.log === null && (await req("GET", "/api/me", { bearer: B })).data.user.name === "Alice", short(r.data));
    r = await act({ action: "rename", user: uidB, name: "앨리스01", reason: "시험" }); expect("닉네임 강제 변경 — 다른 사람과 겹침 → 409", r.status === 409);
    r = await act({ action: "rename", user: uidB, name: "ａ1 ICE", reason: "시험" }); expect("닉네임 강제 변경 — 전각 · 대소문자 · 공백만 다른 남의 이름(A1ice) → 409", r.status === 409, short(r.data));
    r = await act({ action: "rename", user: uidB, name: "ㅋㅋ교주".normalize("NFD"), reason: "시험" }); expect("닉네임 강제 변경 — NFD 로 쓴 남의 이름(ㅋㅋ교주) → 409", r.status === 409, short(r.data));
    r = await act({ action: "rename", user: uidB, name: "운영자", reason: "시험" }); expect("닉네임 강제 변경 — 금지어 → 400", r.status === 400 && /사칭/.test(r.data.why));
    r = await act({ action: "rename", user: uidB, name: "보브", reason: "사칭 의심 — 시험" });
    expect("닉네임 강제 변경(하루 한 번 제한 없음) → 200 · 운영 기록 id", r.status === 200 && r.data.dry === false && Number.isInteger(r.data.log) && r.data.user.name === "보브", short({ log: r.data.log, name: r.data.user?.name }));
    r = await req("GET", "/api/me", { bearer: B }); expect("B /api/me → 보브", r.data.user.name === "보브");
    const NA = (await req("GET", "/api/auth/test?sub=nk_admin&kind=pc")).data.token;
    r = await name({ bearer: NA }, "보 브"); expect("운영자가 바꿔 준 이름과 겹침(「보 브」) → 409", r.status === 409, short(r.data));
    expect("랭킹에도 새 이름", (await rankNames()).includes("보브"));
    // 닉네임 비우기 → 다시 정하게(needsName)
    r = await act({ action: "clearname", user: uidB, reason: "부적절한 닉네임 — 시험" }); expect("닉네임 비우기 → 200", r.status === 200 && r.data.user.name === null);
    r = await req("GET", "/api/me", { bearer: B }); expect("비운 뒤 B → needsName", r.data.needsName === true && r.data.user.name === null);
    expect("이름 없는 동안 랭킹에서 빠짐", JSON.stringify(await rankNames()) === JSON.stringify(["앨리스 01"]));
    r = await newSlot({ bearer: B }, slotB[0]); expect("이름 없는 동안 슬롯 저장 → 403 needsName", r.status === 403 && r.data.needsName === true);
    r = await name({ bearer: NA }, "보브"); expect("운영자가 비운 이름은 풀린다 — 남이 「보브」 → 200", r.status === 200, short(r.data));
    r = await name({ bearer: B }, "보브"); expect("비운 뒤 다시 — 남이 가져간 옛 이름 → 409", r.status === 409, short(r.data));
    await req("DELETE", "/api/me", { bearer: NA });
    r = await name({ bearer: B }, "밥2"); expect("B 가 다시 정함(바로 — 처음 정하기처럼) → 200", r.status === 200 && r.data.user.name === "밥2", short(r.data));
    r = await act({ action: "clearname", user: 999999, reason: "시험" }); expect("없는 사용자 → 404", r.status === 404);
    // 랭킹 숨김 — 한 시즌 · 모든 시즌
    expect("숨기기 전 랭킹 둘", (await rankNames()).length === 2);
    r = await act({ action: "hide", user: uidB, season: "nope", reason: "시험" }); expect("숨김 — 없는 시즌 → 404", r.status === 404);
    r = await act({ action: "hide", user: uidB, season: "preseason", reason: "어뷰징 조사 중 — 시험" }); expect("랭킹 숨김(프리시즌) → 200", r.status === 200 && r.data.user.hidden[0]?.season === "preseason");
    expect("숨긴 B 는 랭킹에서 빠짐", JSON.stringify(await rankNames()) === JSON.stringify(["앨리스 01"]));
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`, { cookie: A }); expect("숨긴 B 의 편성 — 남에게 404", r.status === 404);
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`, { bearer: B }); expect("숨긴 B 의 편성 — 본인은 200", r.status === 200 && r.data.mine === true);
    r = await req("GET", "/api/frontier/me", { bearer: B }); expect("숨긴 B 의 내 순위 → null(최고는 그대로)", r.data.rank === null && r.data.best?.score > 0);
    r = await act({ action: "unhide", user: uidB, reason: "조사 끝 — 시험" }); expect("숨김 되돌리기 → 200", r.status === 200 && r.data.plan.seasons.join() === "preseason");
    expect("되돌리면 다시 둘", (await rankNames()).length === 2);
    r = await act({ action: "hide", user: uidB, reason: "모든 시즌 — 시험" }); expect("랭킹 숨김(모든 시즌 *) → 빠짐", r.status === 200 && (await rankNames()).length === 1);
    r = await act({ action: "unhide", user: uidB, season: "*", reason: "시험" }); expect("모든 시즌 숨김 되돌리기", r.status === 200 && (await rankNames()).length === 2);
    r = await act({ action: "unhide", user: uidB, reason: "시험" }); expect("숨김이 없는데 되돌리기 → 409", r.status === 409);
    // 정지
    r = await act({ action: "ban", user: uidB, reason: "시험" }); expect("정지 — 기간 없음 → 400", r.status === 400);
    r = await act({ action: "ban", user: uidB, days: 0, reason: "시험" }); expect("정지 — 0일 → 400", r.status === 400);
    r = await act({ action: "ban", user: uidB, days: 1, forever: true, reason: "시험" }); expect("정지 — 기간 · 영구 둘 다 → 400", r.status === 400);
    r = await act({ action: "ban", user: uidB, days: 1, reason: "위조 점수 반복 — 시험" });
    const until = r.data.user?.ban?.until;
    expect("정지 1일 → 200 · 풀리는 때", r.status === 200 && r.data.user.ban.forever === false && until > Date.now() / 1000 + 86000, short(r.data.user?.ban));
    r = await req("GET", "/api/me", { bearer: B }); expect("정지된 B — 로그인 · /api/me 는 된다 · ban 표시", r.status === 200 && r.data.ban?.why === "위조 점수 반복 — 시험" && r.data.ban.until === until);
    const isBan = (x) => x.status === 403 && x.data.banned === true && x.data.why === "위조 점수 반복 — 시험" && x.data.until === until && x.data.forever === false;
    r = await newSlot({ bearer: B }, slotB[0]); expect("정지 — 슬롯 새로 저장 → 403 · 까닭 · 풀리는 때", isBan(r), short(r.data));
    r = await put({ bearer: B }, slotB[0]); expect("정지 — 옛 주소 슬롯 올리기 → 403", isBan(r));
    const listB = (await req("GET", "/api/slots", { bearer: B })).data;
    expect("정지 — 슬롯 목록 읽기는 된다", Array.isArray(listB.slots) && listB.slots.length === 3);
    r = await slotApi({ bearer: B }, "PATCH", listB.slots[0].id, { name: "정지 중" }); expect("정지 — 칸 이름 바꾸기 → 403", isBan(r));
    r = await slotApi({ bearer: B }, "DELETE", listB.slots[0].id); expect("정지 — 칸 지우기 → 403", isBan(r));
    r = await start({ bearer: B }, slotB.map((x) => ({ hero: x.hero, n: 0 }))); expect("정지 — 프론티어 시작 → 403", isBan(r));
    r = await submit({ bearer: B }, sB.attempt, 1, tB.text); expect("정지 — 제출 → 403", isBan(r));
    r = await name({ bearer: B }, "밥3"); expect("정지 — 닉네임 변경 → 403", isBan(r));
    r = await req("GET", "/api/frontier/history", { bearer: B }); expect("정지 — 내 기록 보기는 된다", r.status === 200 && r.data.rows.length === 1);
    expect("정지된 B 는 랭킹에서 빠짐", JSON.stringify(await rankNames()) === JSON.stringify(["앨리스 01"]));
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`); expect("정지된 B 의 편성 — 남에게 404", r.status === 404);
    r = await act({ action: "unban", user: uidB, reason: "이의 받아들임 — 시험" }); expect("정지 풀기 → 200", r.status === 200 && r.data.user.ban === null);
    r = await slotApi({ bearer: B }, "PATCH", listB.slots[0].id, { name: "풀림" }); expect("풀린 뒤 쓰기 → 200", r.status === 200 && r.data.name === "풀림");
    expect("풀린 뒤 랭킹에 다시", (await rankNames()).length === 2);
    r = await act({ action: "unban", user: uidB, reason: "시험" }); expect("정지 아닌데 풀기 → 409", r.status === 409);
    // 점수 — 거절로 바꾸기 · 지우기
    r = await act({ action: "score.reject", attempt: sB.attempt, reason: "다중 계정 — 시험" }); expect("점수 거절로 → 200", r.status === 200 && r.data.plan.from === "verified");
    r = await req("GET", "/api/frontier/history", { bearer: B }); expect("본인 기록에 거절 · 운영자 까닭", r.data.rows[0].status === "rejected" && r.data.rows[0].why === "운영자 거절 — 다중 계정 — 시험", short(r.data.rows[0]));
    expect("거절된 점수는 랭킹에서 빠짐", (await rankNames()).length === 1);
    r = await act({ action: "score.delete", attempt: s2.attempt, reason: "시험 — 점수 줄 지우기" }); expect("점수 지우기 → 200", r.status === 200 && r.data.plan.delete.status === "rejected");
    r = await req("GET", "/api/frontier/history", { cookie: A }); expect("지운 점수의 시도 → 내 기록에 「내지 않음」(진행 중 아님)", r.data.rows.find((x) => x.attempt === s2.attempt)?.status === "expired");
    r = await act({ action: "score.delete", attempt: s2.attempt, reason: "시험" }); expect("지운 점수 다시 지우기 → 404", r.status === 404);
    r = await act({ action: "score.delete", attempt: "xyz", reason: "시험" }); expect("시도 id 모양 틀림 → 400", r.status === 400);
    // 칸 — 하나 · 전부(데이브)
    const dSlots = (await req("GET", "/api/slots", { bearer: D })).data.slots;
    r = await act({ action: "slot.delete", slot: dSlots[0].id, reason: "부적절한 칸 이름 — 시험" }); expect("칸 하나 지우기 → 200", r.status === 200 && r.data.plan.delete.hero === dSlots[0].hero);
    r = await req("GET", "/api/slots", { bearer: D }); expect("데이브 칸 하나 줄어듦", r.data.slots.length === dSlots.length - 1);
    r = await act({ action: "slots.purge", user: 1, reason: "시험" }); expect("칸 전부 지우기 — confirm 없음 → 400", r.status === 400 && r.data.error === "confirm required");
    r = await act({ action: "slots.purge", user: 1, reason: "시험", dry: true }); expect("칸 전부 지우기 미리 보기 → 몇 칸", r.status === 200 && r.data.plan.delete === dSlots.length - 1);
    r = await act({ action: "slots.purge", user: 1, confirm: true, reason: "도배 — 시험" }); expect("칸 전부 지우기(confirm) → 200", r.status === 200 && r.data.user.slots.count === 0);
    r = await act({ action: "slots.purge", user: 1, confirm: true, reason: "시험" }); expect("지울 칸 없음 → 409", r.status === 409);
    // 운영 기록
    r = await adm("GET", `/api/admin/log?user=${uidB}`);
    const logB = r.data.rows || [];
    expect("운영 기록 — B 에게 한 조치 아홉(미리 보기 · 거절된 요청은 없음) · 누가 · 까닭 · 바꾸기 전 값", r.status === 200 && logB.length === 9 && logB.every((l) => l.actor === "admin" && l.user === uidB && l.reason.length > 1)
      && logB.map((l) => l.action).reverse().join() === "rename,clearname,hide,unhide,hide,unhide,ban,unban,score.reject" && logB.at(-1).detail.from === "Alice", logB.map((l) => l.action).join(","));
    r = await adm("GET", "/api/admin/log?n=3"); expect("운영 기록 — 최근 n", r.status === 200 && r.data.rows.length === 3 && r.data.rows[0].action === "slots.purge");
    r = await adm("GET", "/api/admin/log?action=score.delete"); expect("운영 기록 — 조치로 거르기", r.status === 200 && r.data.rows.length === 1 && r.data.rows[0].user === uidA);
    r = await adm("GET", "/api/admin/log?n=0"); expect("운영 기록 — n 틀림 → 400", r.status === 400);
    r = await adm("GET", `/api/admin/user?id=${uidB}`); expect("사용자 보기에 최근 운영 기록", r.data.users[0].log.length === 5 && r.data.users[0].log[0].action === "score.reject");
    // bz admin(이 PC 명령) — 로컬 서버 대상
    let c = bzAdmin(["user", "밥2"]); expect("bz admin user 밥2", c.code === 0 && c.out.includes(`#${uidB}`) && c.out.includes("밥2") && c.out.includes("score.reject"));
    c = bzAdmin(["user", s1.attempt]); expect("bz admin user <시도 id> — 점수 지운 시도는 deleted", c.code === 0 && c.out.includes("앨리스 01") && c.out.includes(`${s2.attempt} `) && / deleted/.test(c.out));
    c = bzAdmin(["ban", "밥2", "--days", "3"]); expect("bz admin ban — 까닭 없음 → 끝 코드 2", c.code === 2 && /reason/.test(c.out));
    c = bzAdmin(["ban", "밥2", "--days", "3", "--reason", "봇 사용 — 시험"]); expect("bz admin ban(--yes 없음) → 미리 보기만", c.code === 0 && c.out.includes("미리 보기") && (await req("GET", "/api/me", { bearer: B })).data.ban === null);
    c = bzAdmin(["ban", "밥2", "--forever", "--reason", "봇 사용 — 시험", "--yes"]); expect("bz admin ban --forever --yes → 영구 정지", c.code === 0 && c.out.includes("영구") && (await req("GET", "/api/me", { bearer: B })).data.ban?.forever === true);
    r = await newSlot({ bearer: B }, slotB[0]); expect("영구 정지 — 쓰기 403 · until null · forever", r.status === 403 && r.data.banned && r.data.forever === true && r.data.until === null);
    c = bzAdmin(["rename", "앨리", "앨리스 02", "--reason", "시험"]); expect("bz admin rename — 부분 일치 하나면 그 사람(미리 보기)", c.code === 0 && c.out.includes("부분 일치") && c.out.includes("앨리스 02"));
    c = bzAdmin(["slots", "purge", "앨리스 01", "--reason", "시험", "--yes"]); expect("bz admin slots purge --yes 에 --confirm 없음 → 끝 코드 2 · 안 지움", c.code === 2 && (await req("GET", "/api/slots", { cookie: A })).data.slots.length > 0);
    c = bzAdmin(["log", "밥2", "-n", "3"]); expect("bz admin log 밥2 -n 3", c.code === 0 && c.out.includes("운영 기록 3줄") && c.out.includes("ban"));
    c = bzAdmin(["user", "없는닉네임"]); expect("bz admin user — 없음 → 끝 코드 1", c.code === 1);
    c = bzAdmin(["user", "밥2"], "wrong"); expect("bz admin — 틀린 토큰 → 끝 코드 1 · 401 안내", c.code === 1 && /401/.test(c.out));

    // ── 시험 계정(migrations/0009 — 2026-10-20 사용자 「시험 계정 권한」): 권한 없음 403 · 운영 role 부여 · 시험 칸 저장 · 시험 도전 · 랭킹 제외 · 회수 뒤 막힘 · 운영 기록 ──
    {
      const T = await pcLogin("lab_tester");
      r = await name({ bearer: T }, "시험관"); expect("시험 계정 후보 가입(닉네임 「시험관」)", r.status === 200, short(r.data));
      const uidT = (await req("GET", "/api/me", { bearer: T })).data.user.id;
      const testSlot = (who, s, nm) => req("POST", `/api/test/slots?hero=${encodeURIComponent(s.hero)}${nm ? "&name=" + encodeURIComponent(nm) : ""}`, { ...who, body: typeof s === "string" ? s : JSON.stringify(s), headers: { "content-type": "application/json", origin: BASE } });
      const testApi = (who, method, id, body) => req(method, `/api/test/slot/${id}`, { ...who, body: body === undefined ? undefined : typeof body === "string" ? body : JSON.stringify(body), headers: { "content-type": "application/json", origin: BASE } });
      r = await req("GET", "/api/me", { bearer: T }); expect("/api/me — 역할 없음(role null)", r.status === 200 && r.data.user.role === null, short(r.data.user));
      // 권한 없음 — 일반 계정이 시험 칸 주소를 부르면 403(로그인 전 401)
      r = await req("GET", "/api/test/slots"); expect("시험 칸 목록 — 로그인 전 → 401", r.status === 401);
      r = await req("GET", "/api/test/slots", { bearer: T }); expect("시험 칸 목록 — 일반 계정 → 403 tester only", r.status === 403 && r.data.error === "tester only", short(r.data));
      r = await testSlot({ bearer: T }, slotA[0]); expect("시험 칸 새로 저장 — 일반 계정 → 403", r.status === 403 && r.data.error === "tester only");
      r = await testSlot({ cookie: A }, slotA[0]); expect("시험 칸 새로 저장 — 일반 계정(웹 쿠키) → 403", r.status === 403);
      r = await testApi({ bearer: T }, "PUT", 1, slotA[0]); expect("시험 칸 덮어쓰기 — 일반 계정 → 403", r.status === 403);
      r = await testApi({ bearer: T }, "DELETE", 1); expect("시험 칸 지우기 — 일반 계정 → 403", r.status === 403);
      r = await start({ bearer: T }, [{ test: 1 }]); expect("시험 칸으로 도전 — 일반 계정 → 403", r.status === 403 && r.data.error === "tester only", short(r.data));
      r = await req("GET", "/api/test/slots", { bearer: T, headers: { "x-role": "tester" } }); expect("게임이 역할을 보내도 믿지 않는다(머리 x-role) → 403", r.status === 403);
      // 부여 — 운영 조치(미리 보기 → 실제) · 틀린 값 · 같은 값
      r = await act({ action: "role", user: uidT, role: "admin", reason: "시험" }); expect("운영 role — 모르는 역할 → 400", r.status === 400, short(r.data));
      r = await act({ action: "role", user: uidT, role: null, reason: "시험" }); expect("운영 role — 없는 역할 회수 → 409", r.status === 409, short(r.data));
      r = await act({ action: "role", user: uidT, role: "tester", reason: "시험 — 미리 보기", dry: true });
      expect("운영 role 미리 보기 → plan(null → tester) · 안 바뀜 · 기록 없음", r.status === 200 && r.data.plan.from === null && r.data.plan.to === "tester" && r.data.log === null && (await req("GET", "/api/me", { bearer: T })).data.user.role === null, short(r.data.plan));
      let c = bzAdmin(["role", "시험관", "tester"]); expect("bz admin role — 까닭 없음 → 끝 코드 2", c.code === 2 && /reason/.test(c.out));
      c = bzAdmin(["role", "시험관", "god", "--reason", "시험"]); expect("bz admin role — tester|none 아님 → 끝 코드 2", c.code === 2);
      c = bzAdmin(["role", "시험관", "tester", "--reason", "덱 시험 — 시험"]); expect("bz admin role(--yes 없음) → 미리 보기만", c.code === 0 && c.out.includes("미리 보기") && (await req("GET", "/api/me", { bearer: T })).data.user.role === null, short(c.out));
      c = bzAdmin(["role", "시험관", "tester", "--reason", "덱 시험 — 시험", "--yes"]); expect("bz admin role 시험관 tester --yes → 부여 · 운영 기록", c.code === 0 && /역할 없음 → tester/.test(c.out) && /운영 기록 #\d+/.test(c.out), short(c.out));
      r = await req("GET", "/api/me", { bearer: T }); expect("/api/me — role tester(서버가 내려 준다)", r.data.user.role === "tester");
      r = await act({ action: "role", user: uidT, role: "tester", reason: "시험" }); expect("운영 role — 이미 tester → 409", r.status === 409);
      // 시험 칸 — 저장 · 목록 · 덮어쓰기 · 이름 · 지우기 · 형식 검사 · 일반 칸과 따로
      const broken = { ...slotA[0], growth: { atk: 999, def: 0, crit: 0 } };   // 규칙 밖(성장 상한 500) — 시험 칸은 규칙 검사를 하지 않는다
      r = await testSlot({ bearer: T }, broken, "규칙 밖 덱"); expect("시험 칸 — 규칙 밖 슬롯도 저장(검사 생략) → 201 · test", r.status === 201 && r.data.test === true && r.data.name === "규칙 밖 덱" && !("status" in r.data), short(r.data));
      const brokenId = r.data.id;
      r = await testSlot({ bearer: T }, { ...slotA[0], atk: 999 }); expect("시험 칸 — 모르는 키(형식) → 400", r.status === 400 && /field atk/.test(r.data.error), short(r.data));
      r = await req("POST", `/api/test/slots?hero=${encodeURIComponent(slotA[0].hero)}`, { bearer: T, body: JSON.stringify(slotA[1]), headers: { "content-type": "application/json" } });
      expect("시험 칸 — 주소 사도와 다른 슬롯 → 400", r.status === 400 && /주소와 다르다/.test(r.data.error), short(r.data));
      r = await req("POST", `/api/test/slots?hero=${encodeURIComponent(slotA[0].hero)}`, { bearer: T, body: JSON.stringify({ ...slotA[0], from: { ...slotA[0].from, ended: "x".repeat(17 * 1024) } }), headers: { "content-type": "application/json" } });
      expect("시험 칸 — 16KB 넘음 → 413", r.status === 413, short(r.data));
      r = await testSlot({ bearer: T }, { ...slotA[0], from: { ...slotA[0].from, result: "lose" } }); expect("시험 칸 — 완주 표시 없음(형식) → 400", r.status === 400);
      r = await testSlot({ bearer: T }, slotA[0], "<b>"); expect("시험 칸 — 이름 규칙 → 400", r.status === 400 && r.data.error === "bad name");
      const tIds = [];
      for (const s of slotA) { r = await testSlot({ bearer: T }, s); tIds.push(r.data.id); }
      expect("시험 칸 셋 저장(사도 셋)", tIds.every((x) => Number.isInteger(x)), tIds.join(","));
      r = await req("GET", "/api/test/slots", { bearer: T });
      expect("시험 칸 목록 — 넷 · 슬롯 글 · test · 최대", r.status === 200 && r.data.count === 4 && r.data.slots.every((x) => x.test === true && x.slot?.kind === "bolzena-hero-slot") && r.data.max === 100, short({ count: r.data.count, max: r.data.max }));
      r = await req("GET", "/api/slots", { bearer: T }); expect("일반 칸 목록에는 시험 칸이 없다", r.status === 200 && r.data.slots.length === 0 && r.data.count === 0);
      r = await testApi({ bearer: T }, "PATCH", tIds[0], { name: "시험 A" }); expect("시험 칸 이름 바꾸기 → 200", r.status === 200 && r.data.name === "시험 A");
      r = await testApi({ bearer: T }, "PUT", tIds[0], { ...slotA[0], from: { ...slotA[0].from, ended: "2026-10-20T00:00Z" } }); expect("시험 칸 덮어쓰기 → 200 · 이름 그대로 · 지문 바뀜", r.status === 200 && r.data.name === "시험 A", short(r.data));
      r = await testApi({ bearer: T }, "PUT", tIds[0], slotA[1]); expect("시험 칸 덮어쓰기 — 다른 사도 → 400", r.status === 400);
      r = await testApi({ cookie: A }, "PUT", tIds[0], slotA[0]); expect("남의 시험 칸(일반 계정) → 403", r.status === 403);
      r = await testApi({ bearer: T }, "DELETE", 999999); expect("없는 시험 칸 지우기 → 404", r.status === 404);
      // 시험 도전 — 시험 칸으로 · 규칙 밖 칸도 시작은 된다 · 낸 점수는 skipped + test
      r = await start({ bearer: T }, [{ test: brokenId }]); expect("규칙 밖 시험 칸으로 시작 → 200 test", r.status === 200 && r.data.test === true && r.data.slots[0].test === brokenId, short(r.data.slots?.[0] && { test: r.data.slots[0].test, hero: r.data.slots[0].hero }));
      r = await start({ bearer: T }, [{ test: tIds[0] }, { test: 1.5 }]); expect("시험 칸 참조 모양 틀림 → 400", r.status === 400);
      r = await start({ bearer: T }, [{ test: tIds[0], id: 1 }]); expect("칸 참조에 test · id 둘 다 → 400", r.status === 400);
      r = await startOk({ cookie: A }, [{ test: tIds[0] }]); expect("남(일반 계정)이 시험 칸 id 로 → 403", r.status === 403, short(r.data));
      const sT = (await startOk({ bearer: T }, tIds.map((id) => ({ test: id })))).data;
      expect("시험 칸 셋으로 도전 → 씨앗 · test true", typeof sT.seed === "number" && sT.test === true && sT.slots.length === 3, short({ test: sT.test, heroes: sT.heroes }));
      const tT = fight("tester", sT);
      const scT = JSON.parse(tT.text).score;
      r = await submit({ bearer: T }, sT.attempt, scT, tT.text); expect(`시험 도전 제출(${tT.line}) → 202 skipped · test`, r.status === 202 && r.data.status === "skipped" && r.data.test === true, short(r.data));
      r = await req("GET", "/api/frontier/ranking"); expect("시험 점수는 랭킹에 없다", r.status === 200 && !r.data.rows.some((x) => x.name === "시험관") && !r.data.rows.some((x) => x.attempt === sT.attempt), short(r.data.rows.map((x) => x.name)));
      r = await req("GET", "/api/frontier/me", { bearer: T }); expect("시험 계정 /api/frontier/me — 최고 · 순위 없음 · 목록엔 test", r.data.best === null && r.data.rank === null && r.data.scores.some((x) => x.test === true && x.status === "skipped" && x.claimed === scT), short({ best: r.data.best, rank: r.data.rank, scores: r.data.scores.map((x) => `${x.status}/${x.test}`) }));
      r = await req("GET", "/api/frontier/history", { bearer: T }); expect("내 기록 — 시험 표시(test true)", r.status === 200 && r.data.rows.find((x) => x.attempt === sT.attempt)?.test === true && r.data.rows.find((x) => x.attempt === sT.attempt)?.status === "skipped");
      r = await req("GET", `/api/frontier/attempt/${sT.attempt}`, { bearer: T }); expect("내 시험 시도 보기 → 200 · test · 편성", r.status === 200 && r.data.mine === true && r.data.test === true && r.data.slots.length === 3);
      r = await req("GET", `/api/frontier/attempt/${sT.attempt}`, { cookie: A }); expect("남이 시험 시도 보기 → 404(남의 편성 보기에서 빠짐)", r.status === 404);
      r = await req("GET", "/api/verify/pending", { bearer: VERIFY_TOKEN }); expect("검증기 대기열에 시험 점수 없음(검증 안 함)", r.status === 200 && !r.data.scores.some((x) => x.attempt === sT.attempt));
      // 시험 계정의 일반 칸 도전도 시험(랭킹 밖)
      r = await newSlot({ bearer: T }, slotA[0]); const nT = r.data.id; expect("시험 계정 — 일반 칸 저장 → 201(일반 칸과 같다)", r.status === 201);
      const sT2 = (await startOk({ bearer: T }, [{ id: nT }])).data;
      expect("시험 계정이 일반 칸으로 연 시도도 test", sT2.test === true);
      const tT2 = fight("tester2", sT2);
      r = await submit({ bearer: T }, sT2.attempt, JSON.parse(tT2.text).score, tT2.text); expect("일반 칸 도전 제출 → skipped · test", r.status === 202 && r.data.status === "skipped" && r.data.test === true, short(r.data));
      r = await req("GET", "/api/frontier/ranking"); expect("그래도 랭킹에 없다", !r.data.rows.some((x) => x.name === "시험관"));
      r = await adm("GET", `/api/admin/user?id=${uidT}`); expect("운영 사용자 보기 — role tester · 시험 칸 4 · 시험 점수 2", r.data.users[0].role === "tester" && r.data.users[0].testSlots === 4 && r.data.users[0].scores.test === 2 && r.data.users[0].recent.every((x) => x.test === true), short({ role: r.data.users[0].role, testSlots: r.data.users[0].testSlots, scores: r.data.users[0].scores }));
      c = bzAdmin(["user", "시험관"]); expect("bz admin user 시험관 — 역할 · 시험 칸 · (시험)", c.code === 0 && c.out.includes("역할 tester") && c.out.includes("시험 칸 4") && c.out.includes("(시험)"));
      // 회수 — 시험 칸 주소 · 시험 칸 도전 막힘 · 이미 낸 시험 점수는 계속 랭킹 밖 · 새 도전은 보통
      c = bzAdmin(["role", "시험관", "none", "--reason", "시험 끝 — 시험", "--yes"]); expect("bz admin role 시험관 none --yes → 회수", c.code === 0 && /tester → 없음/.test(c.out), short(c.out));
      r = await req("GET", "/api/me", { bearer: T }); expect("회수 뒤 /api/me — role null", r.data.user.role === null);
      r = await req("GET", "/api/test/slots", { bearer: T }); expect("회수 뒤 시험 칸 목록 → 403", r.status === 403);
      r = await testApi({ bearer: T }, "DELETE", tIds[1]); expect("회수 뒤 시험 칸 지우기 → 403", r.status === 403);
      r = await startOk({ bearer: T }, [{ test: tIds[0] }]); expect("회수 뒤 시험 칸으로 도전 → 403", r.status === 403, short(r.data));
      const sT3 = (await startOk({ bearer: T }, [{ id: nT }])).data;
      expect("회수 뒤 일반 칸 도전 → test false(보통 시도)", sT3.test === false);
      r = await req("GET", "/api/frontier/ranking"); expect("회수 뒤에도 이미 낸 시험 점수는 랭킹 밖", !r.data.rows.some((x) => x.name === "시험관"));
      r = await req("GET", `/api/frontier/attempt/${sT.attempt}`, { cookie: A }); expect("회수 뒤에도 시험 시도는 남에게 404", r.status === 404);
      r = await adm("GET", `/api/admin/log?user=${uidT}&action=role`);
      expect("운영 기록 — role 부여 · 회수 둘(누가 · 까닭 · 바꾸기 전 값)", r.status === 200 && r.data.rows.length === 2 && r.data.rows[1].detail.from === null && r.data.rows[1].detail.to === "tester" && r.data.rows[0].detail.from === "tester" && r.data.rows[0].detail.to === null && r.data.rows.every((l) => l.actor === "admin" && l.reason.includes("시험")),
        short(r.data.rows?.map((l) => `${l.action} ${JSON.stringify(l.detail)} ${l.reason}`)));
      // 다시 주면 칸이 그대로 · 탈퇴하면 시험 칸도 지운다
      r = await act({ action: "role", user: uidT, role: "tester", reason: "다시 — 시험" }); expect("다시 부여 → 200", r.status === 200);
      r = await req("GET", "/api/test/slots", { bearer: T }); expect("다시 주면 시험 칸 넷 그대로", r.status === 200 && r.data.count === 4);
      r = await req("DELETE", "/api/me", { bearer: T }); expect("시험 계정 탈퇴 → 204", r.status === 204);
      r = await adm("GET", `/api/admin/log?user=${uidT}&action=role`); expect("탈퇴 뒤에도 role 운영 기록은 남는다", r.status === 200 && r.data.rows.length === 3);
      const left = JSON.parse(run(Q(WRANGLER), ["d1", "execute", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`, "--json", "--command", `"SELECT COUNT(*) AS c FROM test_slots WHERE user_id = ${uidT}"`]));
      expect("탈퇴하면 시험 칸도 지운다", left?.[0]?.results?.[0]?.c === 0, short(left));
    }

    // ── 기기 코드(PC) 로그인 ──
    r = await req("POST", "/api/auth/device", { body: {} });
    const dev = r.data; expect("기기 코드 시작", r.status === 200 && /^[A-Z2-9]{4}-[A-Z2-9]{4}$/.test(dev.userCode), `${dev.userCode} · ${dev.verifyUrl}`);
    r = await req("POST", "/api/auth/device/poll", { body: { deviceCode: dev.deviceCode } }); expect("폴링 → 202 pending", r.status === 202);
    r = await req("POST", "/api/auth/device/poll", { body: { deviceCode: dev.deviceCode } }); expect("너무 빠른 폴링 → 429 slow_down", r.status === 429);
    r = await req("GET", "/api/auth/device/approve?code=" + dev.userCode, { raw: true }); expect("승인 화면(로그인 전) → 로그인 단추", r.status === 200 && r.text.includes("/api/auth/discord/start?back="));
    r = await req("GET", "/api/auth/device/approve?code=" + dev.userCode, { cookie: A, raw: true }); expect("승인 화면(로그인 뒤) → 코드 · 승인 단추", r.status === 200 && r.text.includes(dev.userCode) && r.text.includes("승인"));
    r = await req("POST", "/api/auth/device/approve", { cookie: A, body: `code=${dev.userCode}&action=approve`, headers: { "content-type": "application/x-www-form-urlencoded", origin: "https://evil.example" } });
    expect("다른 사이트에서 승인 → 403", r.status === 403);
    r = await req("POST", "/api/auth/device/approve", { cookie: A, body: `code=${dev.userCode}&action=approve`, headers: { "content-type": "application/x-www-form-urlencoded", origin: BASE } });
    expect("승인", r.status === 200);
    r = await req("POST", "/api/auth/device/poll", { body: { deviceCode: dev.deviceCode } });
    const pcTok = r.data.token; expect("폴링 → PC 토큰", r.status === 200 && r.data.status === "ok" && !!pcTok);
    r = await req("GET", "/api/me", { bearer: pcTok }); expect("PC 토큰으로 /api/me = 앨리스 01(pc)", r.status === 200 && r.data.user.name === "앨리스 01" && r.data.session.kind === "pc");
    r = await req("POST", "/api/auth/device/poll", { body: { deviceCode: dev.deviceCode } }); expect("받은 기기 코드 다시 폴링 → 410", r.status === 410);

    // ── 슬롯 지우기 · 로그아웃 · 탈퇴 ──
    r = await req("DELETE", `/api/slots/${encodeURIComponent(forged.hero)}/1`, { cookie: A, headers: { origin: BASE } }); expect("슬롯 지우기 → 204", r.status === 204);
    r = await req("POST", "/api/auth/logout", { bearer: pcTok }); expect("PC 로그아웃 → 204", r.status === 204);
    r = await req("GET", "/api/me", { bearer: pcTok }); expect("로그아웃한 토큰 → 401", r.status === 401);
    r = await req("GET", "/api/me", { cookie: A }); expect("웹 세션은 그대로", r.status === 200);
    r = await req("DELETE", "/api/me", { bearer: B }); expect("정지된 B 탈퇴 → 204(정지돼도 탈퇴는 된다)", r.status === 204);
    r = await req("GET", "/api/frontier/ranking"); expect("탈퇴한 B 는 랭킹에서 빠짐", r.data.rows.length === 1 && r.data.rows[0].name === "앨리스 01", short(r.data.rows));
    r = await req("GET", "/api/me", { bearer: B }); expect("탈퇴한 토큰 → 401", r.status === 401);
    r = await req("GET", `/api/frontier/attempt/${sB.attempt}`, { cookie: A }); expect("탈퇴한 B 의 시도 편성 → 404", r.status === 404);
    r = await req("GET", `/api/frontier/attempt/${s1.attempt}`); expect("남은 A 의 시도는 그대로 보인다", r.status === 200);
    r = await req("GET", `/api/admin/log?user=${uidB}`, { bearer: ADMIN_TOKEN }); expect("탈퇴한 B 의 운영 기록은 남는다(닉네임은 null)", r.status === 200 && r.data.rows.length === 10 && r.data.rows.every((l) => l.name === null));
    r = await req("GET", `/api/admin/user?id=${uidB}`, { bearer: ADMIN_TOKEN }); expect("탈퇴한 B — 사용자 보기 빈 목록", r.status === 200 && r.data.users.length === 0);
  } finally { stop(srv); }

  if (MIG_UPTO) console.log(`성장 켬 시험 건너뜀 — MIG_UPTO=${MIG_UPTO}(0010 없음)`);
  else await progPhase(slotA);

  // ── 배포 흉내(TEST_LOGIN · VERIFY_TOKEN 없음) — 가짜 로그인 · 검증기 주소가 닫혔나 ──
  const prod = startServer({ ID_PEPPER: "local-pepper" }, "dev-prod.log", PORT + 1);
  try {
    await waitUp(PORT + 1);
    const P = `http://127.0.0.1:${PORT + 1}`;
    let r = await fetch(P + "/api/auth/test?sub=alice&kind=pc"); expect("TEST_LOGIN 없는 서버 — 가짜 로그인 → 404", r.status === 404);
    r = await fetch(P + "/api/verify/pending", { headers: { authorization: "Bearer " + VERIFY_TOKEN } }); expect("VERIFY_TOKEN 없는 서버 — 검증기 주소 → 503", r.status === 503);
    r = await fetch(P + "/api/admin/user?id=1", { headers: { authorization: "Bearer " + ADMIN_TOKEN } }); expect("ADMIN_TOKEN 없는 서버 — 운영 주소 → 503", r.status === 503);
    r = await fetch(P + "/api/admin/act", { method: "POST", headers: { authorization: "Bearer " + ADMIN_TOKEN }, body: "{}" }); expect("ADMIN_TOKEN 없는 서버 — 운영 조치 → 503", r.status === 503);
    r = await fetch(P + "/api/auth/discord/start"); expect("디스코드 id 없는 서버 — 로그인 시작 → 503", r.status === 503);
    r = await fetch(P + "/api/record", { method: "GET" }); expect("기존 /api/record 그대로(GET 405)", r.status === 405);
    const rec = (extra) => fetch(P + "/api/record", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ kind: "bolzena-record", v: 2, app: "unity", anon: "0123456789abcdef", seed: 7, result: "win", village: "마을", party: [{ id: "아야", slot: 0 }], fights: [], ...extra }) });
    r = await rec({ tier: 3 }); expect("플레이 기록 — tier 칸(3)을 받는다 → 204", r.status === 204, `${r.status} ${await r.text()}`);
    r = await rec({}); expect("플레이 기록 — tier 없는 기록(성장 끔 게임)도 그대로 → 204", r.status === 204, `${r.status}`);
    r = await rec({ tier: 12 }); const rt = await r.text(); expect("플레이 기록 — tier 범위 밖(12) → 400", r.status === 400 && /tier/.test(rt), `${r.status} ${rt}`);
  } finally { stop(prod); }

  fs.writeFileSync(path.join(OUT, "result.json"), JSON.stringify(rows, null, 2));
  console.log(`\n${rows.length - failed}/${rows.length} 통과${failed ? ` · 실패 ${failed}` : ""} → ${path.join(OUT, "result.json")}`);
  process.exit(failed ? 1 : 0);
}

main().catch((e) => { console.error(e); process.exit(2); });
