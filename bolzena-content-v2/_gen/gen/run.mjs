import fs from "fs";
const OUT = "C:/projects/bolzena-content/world/villages";
fs.mkdirSync(OUT, { recursive: true });
const names = process.argv.slice(2);
const artAll = {};
for (const n of names) {
  const m = (await import(`./${n}.mjs`)).default;
  fs.writeFileSync(`${OUT}/${n}.json`, JSON.stringify(m.data, null, 2) + "\n");
  Object.assign(artAll, m.art);
  console.log(n, "적", m.data.enemies.length);
}
const artPath = `${OUT}/_그림.json`;
let prev = {}; try { prev = JSON.parse(fs.readFileSync(artPath, "utf8")); } catch {}
fs.writeFileSync(artPath, JSON.stringify({ ...prev, ...artAll }, null, 1) + "\n");
