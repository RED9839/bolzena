// 마을 파일의 적을 새 설계로 다시 쓴다 — 마을 구조(층 · 칸 · 그림)는 그대로
const fs = require("fs"), path = require("path");
const W = "C:/projects/bolzena-content-v2/world/villages";
const ORIG = path.join(__dirname, "orig");   // 처음 복사본(옛 것) — 바탕 칸을 여기서 읽는다
const files = { erpien: "v_erpien", monatium: "v_monatium", furry: "v_furry", ghost: "v_ghost", spirit: "v_spirit", dragon: "v_dragon" };
const only = process.argv.slice(2);
const ORDER = ["id", "name", "hp", "row", "nature", "weak", "tough", "toughTaken", "boss", "pick", "rush", "soul", "tied", "blurb", "open", "intents", "phase", "phase2", "counters", "passives", "rare"];
for (const [f, mod] of Object.entries(files)) {
  if (only.length && !only.includes(f)) continue;
  if (!fs.existsSync(path.join(__dirname, mod + ".js"))) { console.log("건너뜀", f); continue; }
  const design = require("./" + mod);
  const d = JSON.parse(fs.readFileSync(path.join(ORIG, f + ".json"), "utf8"));
  const out = [];
  for (const e of d.enemies) {
    const des = design[e.id];
    if (!des) { console.log("설계 없음:", f, e.id); continue; }
    const base = { id: e.id, name: e.name, hp: e.hp, row: e.row, nature: e.nature, weak: e.weak, tough: e.tough, boss: e.boss };
    const m = Object.assign({}, base, des);
    const o = {};
    for (const k of ORDER) if (m[k] !== undefined && m[k] !== null) o[k] = m[k];
    for (const k of Object.keys(m)) if (!(k in o) && m[k] !== undefined) { console.log("모르는 칸", e.id, k); o[k] = m[k]; }
    out.push(o);
  }
  for (const id of Object.keys(design)) if (!d.enemies.find(e => e.id === id)) console.log("마을에 없는 설계:", f, id);
  d.enemies = out;
  fs.writeFileSync(path.join(W, f + ".json"), JSON.stringify(d, null, 2) + "\n");
  console.log("씀", f, out.length);
}
