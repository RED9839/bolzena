// 싸움 하나를 사도 셋 무작위 N 벌로 — 새(content-v2) · 옛(base) 나란히: 이긴 비율 · 남은 HP · 턴
const { execFile } = require("child_process");
const fs = require("fs"), path = require("path");
const NEW = "C:/projects/bolzena-content-v2", OLD = path.join(__dirname, "base");
const N = +(process.env.N || 24);
const groups = process.argv.slice(2);   // "1:clone_rude,proteindragon_naive"
const heroes = [];
(function walk(d) { for (const f of fs.readdirSync(d)) { const p = path.join(d, f); if (fs.statSync(p).isDirectory()) walk(p); else if (f.endsWith(".json")) heroes.push(f.replace(/\.json$/, "")); } })(NEW + "/heroes");
const BZ = "C:/projects/bolzena-core/Tools~/Dev/bz.cmd";
const jobs = [], res = {};
for (const g of groups) { const [fl, foes] = g.split(":"); for (const [tag, data] of [["새", NEW], ["옛", OLD]]) for (let i = 1; i <= N; i++) {
  const pick = []; let r = i * 7919;
  while (pick.length < 3) { r = (r * 1103515245 + 12345) % 2147483648; const h = heroes[r % heroes.length]; if (!pick.includes(h)) pick.push(h); }
  jobs.push({ key: g, tag, data, fl, foes, pick, s: i });
} }
const TOTAL = jobs.length; let done = 0;
function next() {
  const j = jobs.shift(); if (!j) return;
  execFile("cmd", ["/c", BZ, "fight", j.pick.join(","), j.foes, String(j.s), "--floor", j.fl, "--data", j.data], { maxBuffer: 1e8 }, (e, out) => {
    const m = out.match(/결과 (\S+)(?: 넘음)? · (\d+)턴 · 파티 HP (\d+)\/(\d+)/);
    const R = (res[j.key] = res[j.key] || {})[j.tag] = res[j.key]?.[j.tag] || { n: 0, w: 0, hp: 0, t: 0 };
    if (m) { R.n++; if (m[1] === "win") R.w++; R.hp += +m[3] / +m[4]; R.t += +m[2]; } else console.log("?", j.key, out.slice(-300));
    if (++done === TOTAL) for (const [k, v] of Object.entries(res)) console.log(k.padEnd(60), ["새", "옛"].map(t => v[t] ? `${t} 승 ${(100 * v[t].w / v[t].n).toFixed(0)}% HP ${(100 * v[t].hp / v[t].n).toFixed(0)}% ${(v[t].t / v[t].n).toFixed(1)}턴` : "").join("  |  "));
    next();
  });
}
for (let i = 0; i < (+process.env.P || 10); i++) next();
