// 잠재력 미리 보기 표(2026-10-22 사용자 「성장이 꺼진 공개판에서도 사도 상세 잠재력 탭을 보기 전용으로」) — 화면만 읽는 사본을 짓는다.
//   node "Tools~/make_pot_preview.mjs" [staging 성장 표] [서버 lib/prog.js]
//   → Runtime/Resources/RunUI/pot_preview.json = { note, potential, heroes(성장 표 그대로), cost(서버 POT_COST — 칸 → 단계별 [하급, 중급, 상급]) }
// 콘텐츠(content-v2)에 넣지 않는다 — 콘텐츠 · 규칙 지문(공개판 검증)이 그대로이고 엔진 판정 · 서버와 상관없다(ProgStore.ViewTable 이 화면에서만 읽는다).
// 켜는 날(콘텐츠에 성장 표가 들어가면) 콘텐츠 표가 우선이다. 성장 표나 서버 비용을 고치면 이 스크립트를 다시 돌린다.
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const HERE = path.dirname(fileURLToPath(import.meta.url));
const TABLE = process.argv[2] || "C:\\projects\\_backup\\prog_staging\\카제나판.json";
const SERVER = process.argv[3] || "C:\\projects\\bolzena-unity\\Web\\lib\\prog.js";
const OUT = path.join(HERE, "..", "Runtime", "Resources", "RunUI", "pot_preview.json");

const prog = JSON.parse(fs.readFileSync(TABLE, "utf8")).prog;
const L = await import(pathToFileURL(SERVER).href);
const ids = prog.potential.map((n) => n.id);
const cost = Object.fromEntries(ids.map((id) => [id, L.POT_COST[id]]));
if (ids.some((id) => !Array.isArray(cost[id]))) throw new Error("서버 POT_COST 에 없는 칸이 있다");
const out = { note: "화면 미리 보기 전용(성장 꺼짐 · 로그인 전) — Tools~/make_pot_preview.mjs 가 staging 성장 표 · 서버 POT_COST 로 짓는다. 엔진 · 서버 · 지문과 상관없다.", potential: prog.potential, heroes: prog.heroes, cost };
fs.writeFileSync(OUT, JSON.stringify(out, null, 1) + "\n");
console.log(`잠재력 미리 보기 ${ids.length}칸 · 사도 ${Object.keys(prog.heroes).length}명 → ${OUT}`);
