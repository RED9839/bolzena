// 프론티어 화면 시험 — 로컬 서버(wrangler pages dev + 로컬 D1 · TEST_LOGIN=1)에 PC 빌드(Build/Bolzena.exe -demo -demo-frontier)를 붙여 한 바퀴.
//   cd C:/projects/bolzena-unity/Web && node test/frontier-ui.mjs [--off] [--phone]
// 하는 일: 새 로컬 D1 → 서버(127.0.0.1:8799) → 게임을 띄우고 그 로그를 읽어
//   ① 「기기 코드 XXXX-XXXX」 가 뜨면 가짜 웹 로그인(test · alice) 쿠키로 승인 화면 폼을 낸다(사람이 브라우저에서 「승인」 을 누른 것과 같은 요청)
//   ② 「검증을 기다림」 이 뜨면 다른 교주 「밥」(가짜 로그인 bob)의 기록을 심는다 — 게임이 남긴 슬롯 셋을 밥의 칸에 올리고 · 서버 씨앗으로 bz frontier 가 싸워 낸다
//      (게임이 랭킹 줄을 눌러 「남의 편성」 을 보는 시험). 그다음 bz verify(이 로컬 서버 · 빌드의 콘텐츠 스냅샷) — 유니티(Mono)가 만든 테이프를 .NET 검증기가 다시 돌리는 교차 확인
//      · 같은 때 게임 계정(앨리스)으로 낮은 점수 하나를 더 낸다 — 내 최고보다 낮아 「검증 안 함」(skipped) — 게임의 내 기록 화면이 보인다
//   (그 전에 게임은 낸 점수가 곧바로 랭킹 · 입구 랭킹 · 내 기록에 「검증 전」 으로 오른 모습을 찍는다 — f20b · f20c · f20d)
//   ③ 게임이 남긴 테이프 파일을 bz frontier --verify 로 한 번 더(서버를 거치지 않는 교차 확인)
//   게임이 끝나면(끝 코드) 서버를 끈다. 캡처는 C:/projects/bolzena-unity-tmp/frontier_ui/(--phone 이면 frontier_ui_phone/ — 1560×720 · Compact). --off 는 서버 없이(「준비 중」 화면만).
// 실제 Cloudflare 계정 · 네트워크는 쓰지 않는다. 결과 test/.out-ui/(dev.log · verify.log · result.json).
import { spawn, spawnSync, execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { fileURLToPath } from "node:url";

const WEB = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const ROOT = path.resolve(WEB, "..");
const OUT = path.join(WEB, "test", ".out-ui"), STATE = path.join(WEB, "test", ".state-ui"), STATIC = path.join(OUT, "static");
const BZ = process.env.BZ || "C:\\projects\\bolzena-core\\Tools~\\Dev\\bz.cmd";
const CONTENT = process.env.CONTENT || path.join(ROOT, "Assets", "StreamingAssets", "CoreData");   // 빌드가 쓰는 스냅샷 — 검증기도 같은 것으로
const EXE = process.env.EXE || path.join(ROOT, "Build", "Bolzena.exe");
const PHONE = process.argv.includes("--phone");   // 폰(Compact) — 1560×720 · -phone(캔버스 높이 720 · 글 · 단추 크게)
const CAPS = process.env.CAPS || "C:\\projects\\bolzena-unity-tmp\\frontier_ui" + (PHONE ? "_phone" : "");
const LOG = process.env.PLAYER_LOG || "C:\\projects\\bolzena-unity-tmp\\frontier_player" + (PHONE ? "_phone" : "") + ".log";
const PORT = Number(process.env.PORT || 8799), BASE = `http://127.0.0.1:${PORT}`;
const VERIFY_TOKEN = "local-verify-token-" + "0123456789abcdef".repeat(2);
const OFF = process.argv.includes("--off");

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
const Q = (cmd) => (fs.existsSync(cmd) ? `"${cmd}"` : cmd);
const ENV = { ...process.env, WRANGLER_SEND_METRICS: "false", CI: "1" };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const notes = [];
const note = (s) => { notes.push(s); console.log(s); };

function run(cmd, args, opts = {}) {
  const r = spawnSync(cmd, args, { cwd: WEB, env: ENV, encoding: "utf8", shell: true, ...opts });
  if (r.status !== 0) throw new Error(`${cmd} ${args.join(" ")} → ${r.status}\n${r.stdout}\n${r.stderr}`);
  return r.stdout;
}
const bz = (args) => run(Q(BZ), args.map((a) => (/[\s]/.test(a) ? `"${a}"` : a)));

async function req(method, p, { body, cookie, bearer, headers = {} } = {}) {
  const h = { ...headers };
  if (cookie) h.cookie = cookie;
  if (bearer) h.authorization = "Bearer " + bearer;
  const r = await fetch(BASE + p, { method, headers: h, body, redirect: "manual" });
  const text = await r.text();
  return { status: r.status, text, headers: r.headers };
}

async function approve(code) {
  const login = await req("GET", "/api/auth/test?sub=alice&back=/");
  const cookie = (login.headers.getSetCookie?.() || []).map((c) => c.split(";")[0]).find((c) => c.startsWith("bz_s="));
  const r = await req("POST", "/api/auth/device/approve", { cookie, body: `code=${code}&action=approve`, headers: { "content-type": "application/x-www-form-urlencoded", origin: BASE } });
  note(`기기 코드 ${code} 승인 → ${r.status} ${/승인했다/.test(r.text) ? "승인했다" : r.text.slice(0, 80)}`);
}

/** 다른 교주 「밥」 — 게임이 남긴 슬롯 글(시도가 떠 둔 것)을 밥의 칸 0 에 올리고, 서버 씨앗으로 bz frontier 가 싸운 테이프를 낸다(검증은 뒤의 bz verify). */
async function seedOther(slotFiles) {
  const login = await req("GET", "/api/auth/test?sub=bob&kind=pc");
  const bearer = JSON.parse(login.text).token;
  const nm = await req("POST", "/api/me/name", { bearer, body: JSON.stringify({ name: "밥돌이" }), headers: { "content-type": "application/json" } });
  const refs = [];
  for (const f of slotFiles) {
    const text = fs.readFileSync(f, "utf8"), hero = JSON.parse(text).hero;
    const r = await req("PUT", `/api/slots/${encodeURIComponent(hero)}/0`, { bearer, body: text, headers: { "content-type": "application/json" } });
    if (r.status !== 200) throw new Error(`밥 슬롯 ${hero} → ${r.status} ${r.text.slice(0, 120)}`);
    refs.push({ hero, n: 0 });
  }
  const st = await req("POST", "/api/frontier/start", { bearer, body: JSON.stringify({ slots: refs }), headers: { "content-type": "application/json" } });
  const s = JSON.parse(st.text);
  if (st.status !== 200) throw new Error(`밥 시작 → ${st.status} ${st.text.slice(0, 120)}`);
  const files = s.slots.map((x, i) => { const f = path.join(OUT, `bob_slot${i}.json`); fs.writeFileSync(f, JSON.stringify(x.slot)); return f; });
  const tape = path.join(OUT, "bob_tape.json");
  bz(["frontier", "--data", CONTENT, "--slots", files.join(","), "--seed", String(s.seed), "--fightbot", "smart", "--tape", tape]);
  const text = fs.readFileSync(tape, "utf8");
  const sub = await req("POST", "/api/frontier/submit", { bearer, body: JSON.stringify({ attempt: s.attempt, score: JSON.parse(text).score, tape: text }), headers: { "content-type": "application/json" } });
  note(`다른 교주 밥돌이 — 닉네임 ${nm.status} · 슬롯 ${refs.length} · 시도 ${s.attempt} · 점수 ${JSON.parse(text).score} · 제출 ${sub.status}`);
  if (sub.status !== 202) throw new Error(`밥 제출 → ${sub.status} ${sub.text.slice(0, 160)}`);
}

/**
 * 게임 계정(앨리스)으로 낮은 점수 하나 더 — 게임이 낸 점수(검증 대기)보다 낮아 「검증 안 함」(skipped · 2026-10-19 「검증은 자기 최고 후보만」).
 * 칸 하나로 시작 · bz frontier 가 싸운 테이프의 점수만 1 로(주장도 같게 — 검증기는 보지 않는다). 게임의 내 기록 화면이 「검증 안 함」 줄을 보인다.
 */
async function seedSkipped() {
  const bearer = JSON.parse((await req("GET", "/api/auth/test?sub=alice&kind=pc")).text).token;
  const slots = JSON.parse((await req("GET", "/api/slots", { bearer })).text).slots.filter((x) => x.status !== "bad");
  const st = await req("POST", "/api/frontier/start", { bearer, body: JSON.stringify({ slots: [{ id: slots[0].id }] }), headers: { "content-type": "application/json" } });
  if (st.status !== 200) throw new Error(`앨리스 낮은 점수 시작 → ${st.status} ${st.text.slice(0, 120)}`);
  const a = JSON.parse(st.text);
  const f = path.join(OUT, "alice_low_slot0.json"); fs.writeFileSync(f, JSON.stringify(a.slots[0].slot));
  const tape = path.join(OUT, "alice_low_tape.json");
  bz(["frontier", "--data", CONTENT, "--slots", f, "--seed", String(a.seed), "--fightbot", "smart", "--tape", tape]);
  const text = fs.readFileSync(tape, "utf8").replace(/"score":\d+/, '"score":1');
  const sub = await req("POST", "/api/frontier/submit", { bearer, body: JSON.stringify({ attempt: a.attempt, score: 1, tape: text }), headers: { "content-type": "application/json" } });
  note(`앨리스 낮은 점수(1) — 시도 ${a.attempt} · 제출 ${sub.status} ${sub.text.slice(0, 80)}`);
  if (sub.status !== 202 || JSON.parse(sub.text).status !== "skipped") throw new Error("앨리스 낮은 점수가 skipped 가 아니다 — " + sub.text.slice(0, 160));
}

async function main() {
  fs.rmSync(OUT, { recursive: true, force: true });
  fs.mkdirSync(STATIC, { recursive: true });
  fs.writeFileSync(path.join(STATIC, "index.html"), "<!doctype html><title>bolzena local</title>");
  fs.rmSync(CAPS, { recursive: true, force: true });
  fs.mkdirSync(CAPS, { recursive: true });
  let srv = null;
  if (!OFF) {
    fs.rmSync(STATE, { recursive: true, force: true });
    run(Q(WRANGLER), ["d1", "migrations", "apply", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`]);
    const args = ["pages", "dev", `"${STATIC}"`, "--port", String(PORT), "--ip", "127.0.0.1", "--d1", "DB=bolzena-frontier-local", "--persist-to", `"${STATE}"`, "--show-interactive-dev-session=false",
      "-b", "TEST_LOGIN=1", "-b", "ID_PEPPER=local-pepper", "-b", `VERIFY_TOKEN=${VERIFY_TOKEN}`];
    const log = fs.openSync(path.join(OUT, "dev.log"), "w");
    srv = spawn(Q(WRANGLER), args, { cwd: WEB, env: ENV, shell: true, stdio: ["ignore", log, log] });
    for (let i = 0; i < 90; i++) {
      try { const r = await fetch(`${BASE}/api/frontier/season`); if (r.status === 200) break; } catch {}
      await sleep(1000);
    }
    note("로컬 서버 " + BASE);
  }
  const gameArgs = ["-demo", "-demo-frontier", "-demo-timeout", "1500", "-save", PHONE ? "frontier_ui_phone" : "frontier_ui", "-captures", CAPS,
    "-screen-width", PHONE ? "1560" : "1600", "-screen-height", PHONE ? "720" : "900", "-screen-fullscreen", "0", "-logFile", LOG];
  if (PHONE) gameArgs.push("-phone");
  if (!OFF) gameArgs.push("-server", BASE);
  fs.rmSync(LOG, { force: true });
  const game = spawn(EXE, gameArgs, { stdio: "ignore" });
  let exit = null;
  game.on("exit", (c) => { exit = c; });
  let pos = 0, approved = false, verified = false, tapeFile = null;
  const t0 = Date.now();
  while (exit === null && Date.now() - t0 < 1700 * 1000) {
    await sleep(1000);
    if (!fs.existsSync(LOG)) continue;
    const all = fs.readFileSync(LOG, "utf8");
    const chunk = all.slice(pos); pos = all.length;
    for (const line of chunk.split(/\r?\n/)) {
      if (/\[Demo\] 단언|\[Frontier\]|\[Online\] (상태|기기 코드|닉네임)|\[Bridge\] 프론티어|\[Demo\] 프론티어|\[HeroSave\]|\[Capture\] f|봇 완주/.test(line)) console.log("  게임 | " + line.trim());
      const m = line.match(/\[Online\] 기기 코드 ([A-Z2-9]{4}-[A-Z2-9]{4})/);
      if (m && !approved) { approved = true; await sleep(2500); await approve(m[1]); }
      const t = line.match(/\[Frontier\] 테이프 남김 (.+\.json) ·/);
      if (t) tapeFile = t[1].trim();
      if (/\[Demo\] 검증을 기다림/.test(line) && !verified) {
        verified = true;
        if (tapeFile && fs.existsSync(tapeFile)) {
          const base = tapeFile.replace(/\.json$/, "");
          try { await seedOther([0, 1, 2].map((i) => `${base}_slot${i}.json`).filter((f) => fs.existsSync(f))); } catch (e) { note("밥 기록 심기 실패 — " + e.message.slice(0, 400)); }
          try { await seedSkipped(); } catch (e) { note("앨리스 낮은 점수 심기 실패 — " + e.message.slice(0, 400)); }
        }
        const vlog = path.join(OUT, "verify.log");
        const o = bz(["verify", "--data", CONTENT, "--url", BASE, "--token", VERIFY_TOKEN, "--log", vlog]);
        note("bz verify —\n" + o.trim().split(/\r?\n/).map((l) => "    " + l).join("\n"));
        if (tapeFile && fs.existsSync(tapeFile)) {
          const base = tapeFile.replace(/\.json$/, "");
          const slots = [0, 1, 2].map((i) => `${base}_slot${i}.json`).filter((f) => fs.existsSync(f));
          try {
            const o2 = bz(["frontier", "--data", CONTENT, "--verify", tapeFile, "--slots", slots.join(",")]);
            note("bz frontier --verify(게임 테이프 파일) —\n" + o2.trim().split(/\r?\n/).map((l) => "    " + l).join("\n"));
          } catch (e) { note("bz frontier --verify 실패 — " + e.message.slice(0, 400)); }
        }
      }
    }
  }
  if (exit === null) { note("게임이 제한 시간 안에 끝나지 않음 — 끈다"); try { execSync("taskkill /F /IM Bolzena.exe", { stdio: "ignore" }); } catch {} }
  note(`게임 끝 코드 ${exit}`);
  if (srv) { try { execSync(`taskkill /pid ${srv.pid} /T /F`, { stdio: "ignore" }); } catch {} }
  const shots = fs.existsSync(CAPS) ? fs.readdirSync(CAPS).filter((f) => f.endsWith(".png")).sort() : [];
  note(`캡처 ${shots.length}장 → ${CAPS}`);
  fs.writeFileSync(path.join(OUT, "result.json"), JSON.stringify({ exit, approved, verified, tapeFile, shots, notes }, null, 2));
  process.exit(exit === 0 ? 0 : 1);
}

main().catch((e) => { console.error(e); process.exit(1); });
