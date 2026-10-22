// 덱 만들기 화면 시험(2026-10-20 사용자 「사도 카드 원하는 덱 만들어서 프론티어 테스트」) — PC 빌드(Build/Bolzena.exe)를 두 번 띄운다.
//   cd C:/projects/bolzena-unity/Web && node test/decklab-ui.mjs [--phone] [--only offline|online]
// ① 이 PC 시험 모드(-demo -demo-decklab -decklab, 서버 없이) — 로비 진입점 · 덱 만들기(사도 · 카드 · 신탁 · 장비 · 성장 · 규칙 밖) · 저장 · M.E.O.W 시험 전투 · 결과(사도별 딜).
// ② 시험 계정(-demo -demo-decklab-online -server 로컬, -decklab 없이 — 공개판과 같은 조건) — 로컬 서버(wrangler pages dev + 로컬 D1 · TEST_LOGIN=1)에
//    일반 계정 「평범교주」(lab_plain) · 시험 계정 후보 「시험관」(lab_tester)을 만들고, 시험관에게 `bz admin role 시험관 tester --yes`(이 로컬 서버 대상 --url)로 역할을 준 뒤
//    게임이: 일반 계정엔 진입점 없음 → 시험 계정엔 보임 → 서버 시험 도전(시험 칸 저장 · 도전 · 제출) → 내 기록 「시험」. 끝나면 이 스크립트가 서버 쪽(랭킹에 없음 · 시험 점수 · 운영 기록)을 확인한다.
// 캡처: C:/projects/bolzena-unity-tmp/decklab_ui(--phone 이면 decklab_ui_phone)/offline · online. 결과: test/.out-decklab/result.json. 게임 · 서버는 끝나면 끈다. 공개 서버는 부르지 않는다.
import { spawn, spawnSync, execSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import os from "node:os";
import { fileURLToPath } from "node:url";

const WEB = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const ROOT = path.resolve(WEB, "..");
const OUT = path.join(WEB, "test", ".out-decklab"), STATE = path.join(WEB, "test", ".state-decklab"), STATIC = path.join(OUT, "static");
const BZ = process.env.BZ || "C:\\projects\\bolzena-core\\Tools~\\Dev\\bz.cmd";
const EXE = process.env.EXE || path.join(ROOT, "Build", "Bolzena.exe");
const PHONE = process.argv.includes("--phone");
const ONLY = (() => { const i = process.argv.indexOf("--only"); return i >= 0 ? process.argv[i + 1] : null; })();
const CAPS = process.env.CAPS || "C:\\projects\\bolzena-unity-tmp\\decklab_ui" + (PHONE ? "_phone" : "");
const PORT = Number(process.env.PORT || 8797), BASE = `http://127.0.0.1:${PORT}`;
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
const Q = (cmd) => (fs.existsSync(cmd) ? `"${cmd}"` : cmd);
const ENV = { ...process.env, WRANGLER_SEND_METRICS: "false", CI: "1" };
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const rows = []; let failed = 0;
function expect(name, ok, detail = "") { rows.push({ name, ok: !!ok, detail }); if (!ok) failed++; console.log(`${ok ? "통과" : "실패"} — ${name}${detail ? " · " + detail : ""}`); }

function run(cmd, args) {
  const r = spawnSync(cmd, args, { cwd: WEB, env: ENV, encoding: "utf8", shell: true });
  if (r.status !== 0) throw new Error(`${cmd} ${args.join(" ")} → ${r.status}\n${r.stdout}\n${r.stderr}`);
  return r.stdout;
}
async function req(method, p, { body, bearer } = {}) {
  const h = {};
  if (bearer) h.authorization = "Bearer " + bearer;
  if (body !== undefined) h["content-type"] = "application/json";
  const r = await fetch(BASE + p, { method, headers: h, body: body === undefined ? undefined : JSON.stringify(body) });
  const text = await r.text();
  let data = null; try { data = JSON.parse(text); } catch { data = text; }
  return { status: r.status, data };
}

/** 게임 한 번 — 끝 코드 · 로그의 단언 줄. */
async function game(name, extra) {
  const caps = path.join(CAPS, name), log = path.join(OUT, `player_${name}.log`);
  fs.rmSync(caps, { recursive: true, force: true }); fs.mkdirSync(caps, { recursive: true });
  const args = ["-demo", ...extra, "-demo-timeout", "1500", "-save", `decklab_${name}${PHONE ? "_phone" : ""}`, "-captures", caps,
    "-screen-width", PHONE ? "1560" : "1600", "-screen-height", PHONE ? "720" : "900", "-screen-fullscreen", "0", "-logFile", log];
  if (PHONE) args.push("-phone");
  console.log(`게임 ${name} — ${args.join(" ")}`);
  const p = spawn(EXE, args, { stdio: "ignore" });
  let exit = null; p.on("exit", (c) => { exit = c; });
  let pos = 0; const t0 = Date.now();
  while (exit === null && Date.now() - t0 < 1700 * 1000) {
    await sleep(1000);
    if (!fs.existsSync(log)) continue;
    const all = fs.readFileSync(log, "utf8"); const chunk = all.slice(pos); pos = all.length;
    for (const line of chunk.split(/\r?\n/)) if (/\[Demo\] 단언|\[DeckLab\]|\[Online\] 상태|\[Bridge\] 프론티어|\[Capture\]/.test(line)) console.log("  게임 | " + line.trim());
  }
  if (exit === null) { console.log("게임이 제한 시간 안에 끝나지 않음 — 끈다"); try { execSync(`taskkill /pid ${p.pid} /T /F`, { stdio: "ignore" }); } catch {} }
  const text = fs.existsSync(log) ? fs.readFileSync(log, "utf8") : "";
  const bad = text.split(/\r?\n/).filter((l) => /\[Demo\] 단언 실패/.test(l));
  const ok = text.split(/\r?\n/).filter((l) => /\[Demo\] 단언 OK/.test(l)).length;
  const shots = fs.readdirSync(caps).filter((f) => f.endsWith(".png")).sort();
  expect(`게임 ${name} — 끝 코드 0 · 단언 실패 없음(OK ${ok})`, exit === 0 && bad.length === 0, bad.map((l) => l.replace(/^.*단언 실패 — /, "")).join(" / ") || `끝 코드 ${exit}`);
  expect(`게임 ${name} — 캡처`, shots.length > 0, `${shots.length}장 → ${caps}`);
  return { exit, shots };
}

async function main() {
  fs.rmSync(OUT, { recursive: true, force: true });
  fs.mkdirSync(STATIC, { recursive: true });
  fs.writeFileSync(path.join(STATIC, "index.html"), "<!doctype html><title>bolzena local</title>");
  if (ONLY !== "online") await game("offline", ["-demo-decklab", "-decklab"]);
  if (ONLY !== "offline") {
    fs.rmSync(STATE, { recursive: true, force: true });
    run(Q(WRANGLER), ["d1", "migrations", "apply", "DB", "--local", "-c", "test/wrangler.test.toml", "--persist-to", `"${STATE}"`]);
    const args = ["pages", "dev", `"${STATIC}"`, "--port", String(PORT), "--ip", "127.0.0.1", "--d1", "DB=bolzena-frontier-local", "--persist-to", `"${STATE}"`, "--show-interactive-dev-session=false",
      "-b", "TEST_LOGIN=1", "-b", "ID_PEPPER=local-pepper", "-b", `ADMIN_TOKEN=${ADMIN_TOKEN}`];
    const srv = spawn(Q(WRANGLER), args, { cwd: WEB, env: ENV, shell: true, stdio: ["ignore", fs.openSync(path.join(OUT, "dev.log"), "w"), fs.openSync(path.join(OUT, "dev.log"), "a")] });
    try {
      for (let i = 0; i < 90; i++) { try { const r = await fetch(`${BASE}/api/frontier/season`); if (r.status === 200) break; } catch {} await sleep(1000); }
      // 계정 둘 — 일반 「평범교주」 · 시험 계정 후보 「시험관」(가입까지)
      const plain = (await req("GET", "/api/auth/test?sub=lab_plain&name=" + encodeURIComponent("평범교주") + "&kind=pc")).data;
      const tester = (await req("GET", "/api/auth/test?sub=lab_tester&name=" + encodeURIComponent("시험관") + "&kind=pc")).data;
      expect("로컬 서버 — 계정 둘(평범교주 · 시험관)", !!plain?.token && !!tester?.token);
      // 역할 부여 — bz admin role(이 로컬 서버만 --url)
      const r = spawnSync(Q(BZ), ["admin", "role", "시험관", "tester", "--reason", "\"덱 만들기 화면 시험\"", "--yes", "--url", BASE, "--token", ADMIN_TOKEN], { cwd: WEB, env: ENV, encoding: "utf8", shell: true });
      console.log((r.stdout || "") + (r.stderr || ""));
      expect("bz admin role 시험관 tester --yes(로컬 서버) → 부여", r.status === 0 && /tester/.test(r.stdout));
      expect("/api/me — 시험관 role tester", (await req("GET", "/api/me", { bearer: tester.token })).data?.user?.role === "tester");
      await game("online", ["-demo-decklab-online", "-server", BASE]);
      // 서버 쪽 확인 — 시험 점수는 랭킹 밖 · 시험 기록 · 운영 기록
      const rk = await req("GET", "/api/frontier/ranking");
      expect("랭킹에 시험관 없음", rk.status === 200 && !rk.data.rows.some((x) => x.name === "시험관"), JSON.stringify(rk.data.rows?.map((x) => x.name)));
      const me = await req("GET", "/api/frontier/me", { bearer: tester.token });
      expect("시험관 — 시험 점수 1 · 최고 · 순위 없음", me.data.best === null && me.data.rank === null && me.data.scores.filter((x) => x.test).length === 1, JSON.stringify({ best: me.data.best, rank: me.data.rank, scores: me.data.scores?.map((x) => `${x.status}/${x.test}/${x.claimed}`) }));
      const ts = await req("GET", "/api/test/slots", { bearer: tester.token });
      expect("시험관 — 서버 시험 칸(게임이 올린 것)", ts.status === 200 && ts.data.count >= 1, `${ts.data.count}칸`);
      const pl = await req("GET", "/api/test/slots", { bearer: plain.token });
      expect("평범교주 — 시험 칸 주소 403", pl.status === 403);
      const lg = await req("GET", "/api/admin/log?action=role", { bearer: ADMIN_TOKEN });
      expect("운영 기록 — role 부여 한 줄", lg.status === 200 && lg.data.rows.length === 1 && lg.data.rows[0].detail.to === "tester", JSON.stringify(lg.data.rows?.map((l) => `${l.action} ${l.name} ${JSON.stringify(l.detail)} ${l.reason}`)));
    } finally { try { execSync(`taskkill /pid ${srv.pid} /T /F`, { stdio: "ignore" }); } catch {} }
  }
  fs.writeFileSync(path.join(OUT, "result.json"), JSON.stringify(rows, null, 2));
  console.log(`\n${rows.length - failed}/${rows.length} 통과${failed ? ` · 실패 ${failed}` : ""} → ${path.join(OUT, "result.json")} · 캡처 ${CAPS}`);
  process.exit(failed ? 1 : 0);
}

main().catch((e) => { console.error(e); process.exit(2); });
