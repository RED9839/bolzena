// 마을 파일의 적을 새 설계로 다시 쓴다 — 마을 구조(층 · 칸 · 그림)는 그대로
const fs = require("fs"), path = require("path");
const W = "C:/projects/bolzena-content-v2/world/villages";
const ORIG = path.join(__dirname, "orig");   // 처음 복사본(옛 것) — 바탕 칸을 여기서 읽는다
const files = { erpien: "v_erpien", monatium: "v_monatium", furry: "v_furry", ghost: "v_ghost", spirit: "v_spirit", dragon: "v_dragon" };
const only = process.argv.slice(2);
// 그림이 확실하지 않은 적(감사 _ref/적그림_감사.md 의 △ · ×)은 뺀다 — 옛 복사본에 남아 있어도 되살리지 않는다.
// 빠진 적 id → 그 자리(풀 · 엘리트 · 보스 줄 · 소환)를 채울 ○ 적 id
const DROP = {
  elfsoldiercloserange_mad: "elfsoldiercloserange_cool_elite",   // 엘프 돌격병-광기는 원작 도감 · 아이콘 없음 → 노동반(냉정) 스킨의 현장 반장으로
};
// 새로 넣는 적 — 바탕 칸은 from 의 것에 덮어쓰기, 설계는 설계 파일에 id 가 없으면 from 의 것
const ADD = {
  monatium: [{ from: "elfsoldiercloserange_mad", id: "elfsoldiercloserange_cool_elite", name: "엘프 돌격병 · 현장 반장", nature: "냉정" }],
};
const swapIds = (x) => Array.isArray(x) ? x.map(swapIds) : x && typeof x === "object" ? Object.fromEntries(Object.entries(x).map(([k, v]) => [k, swapIds(v)])) : (typeof x === "string" && DROP[x]) ? DROP[x] : x;
const ORDER = ["id", "name", "hp", "row", "nature", "weak", "tough", "toughTaken", "boss", "pick", "rush", "soul", "tied", "blurb", "open", "intents", "phase", "phase2", "counters", "passives", "rare"];
for (const [f, mod] of Object.entries(files)) {
  if (only.length && !only.includes(f)) continue;
  if (!fs.existsSync(path.join(__dirname, mod + ".js"))) { console.log("건너뜀", f); continue; }
  const design = require("./" + mod);
  const d = JSON.parse(fs.readFileSync(path.join(ORIG, f + ".json"), "utf8"));
  const out = [];
  const adds = (ADD[f] || []).map(a => ({ at: a.from, e: Object.assign({}, d.enemies.find(e => e.id === a.from), { id: a.id, name: a.name, nature: a.nature }), from: a.from }));
  const list = [];
  for (const e of d.enemies) { for (const a of adds) if (a.at === e.id) list.push(a.e); if (!DROP[e.id]) list.push(e); }
  for (const e of list) {
    const add = adds.find(a => a.e === e);
    const des = design[e.id] || (add && design[add.from]);
    if (!des) { console.log("설계 없음:", f, e.id); continue; }
    const base = { id: e.id, name: e.name, hp: e.hp, row: e.row, nature: e.nature, weak: e.weak, tough: e.tough, boss: e.boss };
    const m = Object.assign({}, base, des);
    const o = {};
    for (const k of ORDER) if (m[k] !== undefined && m[k] !== null) o[k] = m[k];
    for (const k of Object.keys(m)) if (!(k in o) && m[k] !== undefined) { console.log("모르는 칸", e.id, k); o[k] = m[k]; }
    out.push(o);
  }
  for (const id of Object.keys(design)) if (!list.find(e => e.id === id) && !DROP[id]) console.log("마을에 없는 설계:", f, id);
  d.enemies = swapIds(out);
  d.villages = swapIds(d.villages);
  fs.writeFileSync(path.join(W, f + ".json"), JSON.stringify(d, null, 2) + "\n");
  console.log("씀", f, out.length);
}
