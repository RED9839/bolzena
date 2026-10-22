// 성장 화면 시험(2026-10-21 밤 잠재력만) — 서버 없이 PC 빌드로 한 바퀴(-demo -demo-prog). 서버 응답은 run-local.mjs 가 로컬 성장 켬 서버에서 남긴 진짜 응답(test/.out/prog-samples — view · earn · pot_view · potential).
//   cd C:/projects/bolzena-unity/Web && node test/prog-ui.mjs [--off] [--phone]
// 스위치 켬(기본): 켠 사본 C:/projects/bolzena-unity-tmp/Build_prog_on 을 Build 로 다시 채우고 성장 표(staging)를 얹어 띄운다 —
//   로비 잠재력 판 → 도전 사다리(단계 고르기) → 판 → 판 끝 잠재력 재료 → 도감 사도 상세(잠재력 요약 · 「잠재력」 단추) → 「잠재력」 탭(10칸 · 칸 고르기 · 열기 · 잠긴 칸) → 카드 상세 실제 수치.
//   어디서나 없앤 성장(레벨 · 진급 · 교주 레벨 · 던전 · 왕사탕 · 모카롱 · 교본 · 골드)의 글 · 단추가 없는지 단언한다.
// --off: 지금 빌드(Build — 공개판 콘텐츠 · 성장 표 없음) 그대로 — 단계 · 잠재력이 하나도 안 보이는지(스위치 끔 = 지금 게임).
// 캡처: C:/projects/bolzena-unity-tmp/prog_ui(_phone · _off). 시험 실행은 음소거(-demo).
import { spawn, spawnSync } from "node:child_process";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const WEB = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const ROOT = path.resolve(WEB, "..");
const OFF = process.argv.includes("--off"), PHONE = process.argv.includes("--phone");
const TMP = "C:\\projects\\bolzena-unity-tmp";
const BUILD = path.join(ROOT, "Build"), ON_BUILD = path.join(TMP, "Build_prog_on");
const PROG_TABLE = process.env.PROG_TABLE || "C:\\projects\\_backup\\prog_staging\\카제나판.json";
const SAMPLES = process.env.SAMPLES || path.join(WEB, "test", ".out", "prog-samples");
const CAPS = path.join(TMP, "prog_ui" + (OFF ? "_off" : "") + (PHONE ? "_phone" : ""));
const LOG = path.join(TMP, "prog_player" + (OFF ? "_off" : "") + (PHONE ? "_phone" : "") + ".log");

let exe = path.join(BUILD, "Bolzena.exe");
if (!OFF) {
  for (const n of ["view", "earn", "pot_view", "potential"]) if (!fs.existsSync(path.join(SAMPLES, n + ".json"))) { console.log(`견본 ${n}.json 이 없다 — 먼저 node test/run-local.mjs`); process.exit(2); }
  // 켠 사본 — 빌드를 통째로 다시 베끼고(원본 Build 는 그대로) 성장 표를 얹는다
  fs.rmSync(ON_BUILD, { recursive: true, force: true });
  fs.cpSync(BUILD, ON_BUILD, { recursive: true });
  exe = path.join(ON_BUILD, "Bolzena.exe");
  const coreData = path.join(ON_BUILD, "Bolzena_Data", "StreamingAssets", "CoreData");
  fs.mkdirSync(path.join(coreData, "world", "성장"), { recursive: true });
  fs.copyFileSync(PROG_TABLE, path.join(coreData, "world", "성장", "카제나판.json"));
}
fs.rmSync(CAPS, { recursive: true, force: true }); fs.mkdirSync(CAPS, { recursive: true });
const args = ["-demo", "-demo-prog", "-demo-timeout", "900", "-save", "prog_ui", "-captures", CAPS, "-prog-samples", SAMPLES,
  "-screen-width", PHONE ? "1560" : "1600", "-screen-height", PHONE ? "720" : "900", "-screen-fullscreen", "0", "-logFile", LOG];
if (PHONE) args.push("-phone");
console.log(`게임 ${exe}${OFF ? "(스위치 끔)" : "(켠 사본)"} → 캡처 ${CAPS}`);
const code = await new Promise((ok) => { const g = spawn(exe, args, { stdio: "ignore" }); g.on("exit", (c) => ok(c)); });
spawnSync("taskkill", ["/F", "/IM", "Bolzena.exe"], { stdio: "ignore" });
const log = fs.existsSync(LOG) ? fs.readFileSync(LOG, "utf8") : "";
for (const l of log.split(/\r?\n/).filter((l) => /\[Demo\] 단언|\[Demo\] 성장|Exception/.test(l))) console.log("  " + l.trim());
console.log(`게임 끝 코드 ${code}`);
const shots = fs.readdirSync(CAPS).filter((f) => f.endsWith(".png"));
console.log(`캡처 ${shots.length}장 — ${shots.join(" · ")}`);
process.exit(code !== 0 ? 1 : 0);
