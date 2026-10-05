// 마을마다 봇 판을 돌려 어디서 쓰러지는지 · 싸움마다 잃은 HP 를 모은다
const { execFile } = require("child_process");
const fs = require("fs"), path = require("path");
const DATA = process.argv[2] || "C:/projects/bolzena-content-v2";
const N = +(process.argv[3] || 40);
const villages = (process.argv[4] || "erpien,monatium,furry,ghost,spirit,dragon").split(",");
const heroes = [];
(function walk(d) { for (const f of fs.readdirSync(d)) { const p = path.join(d, f); if (fs.statSync(p).isDirectory()) walk(p); else if (f.endsWith(".json")) heroes.push(f.replace(/\.json$/, "")); } })("C:/projects/bolzena-content-v2/heroes");
const BZ = "C:/projects/bolzena-core/Tools~/Dev/bz.cmd";
let seed = 1;
const jobs = [];
for (const v of villages) for (let i = 0; i < N; i++) {
  const s = seed++; const pick = []; let r = s * 7919;
  while (pick.length < 3) { r = (r * 1103515245 + 12345) % 2147483648; const h = heroes[r % heroes.length]; if (!pick.includes(h)) pick.push(h); }
  jobs.push({ v, s, pick });
}
const res = {}; const TOTAL = jobs.length;
let run = 0, done = 0;
function next() {
  if (!jobs.length) return;
  const j = jobs.shift(); run++;
  execFile("cmd", ["/c", BZ, "run", j.pick.join(","), String(j.s), "--village", j.v, "--data", DATA], { maxBuffer: 1e7 }, (err, out) => {
    const R = res[j.v] = res[j.v] || { n: 0, win: 0, die: {}, loss: {} };
    R.n++;
    const lines = out.split(/\r?\n/);
    let prev = null;
    for (const l of lines) {
      const m = l.match(/^\s+(\d)층 (싸움|엘리트|보스) (.*) · 파티 HP (\d+)\/(\d+)/);
      if (m) {
        const key = m[1] + "층 " + m[2] + " " + m[3];
        const hp = +m[4], max = +m[5];
        if (prev != null) { const L = R.loss[key] = R.loss[key] || [0, 0]; L[0] += Math.max(0, (prev - hp) / max); L[1]++; }
        prev = hp;
      }
      const d = l.match(/^(\d)층 (\w+) 에서 쓰러짐/);
      if (d) { const k = d[1] + "층 " + d[2]; R.die[k] = (R.die[k] || 0) + 1; }
      if (/완주|클리어|승리/.test(l) && !/쓰러짐/.test(l)) R.win += 0;
    }
    if (!lines.some(l => /쓰러짐/.test(l))) R.win++;
    done++; next();
    if (done === TOTAL) report();
  });
}
function report() {
  for (const [v, R] of Object.entries(res)) {
    console.log(`== ${v} 완주 ${(100 * R.win / R.n).toFixed(0)}% (${R.n}) 쓰러짐: ${Object.entries(R.die).sort((a, b) => b[1] - a[1]).map(([k, c]) => k + " " + c).join(" · ")}`);
    const L = Object.entries(R.loss).filter(([k, x]) => x[1] >= 2).map(([k, x]) => [k, x[0] / x[1], x[1]]).sort((a, b) => b[1] - a[1]).slice(0, 8);
    for (const [k, a, c] of L) console.log(`   ${(a * 100).toFixed(0)}%  ×${c}  ${k}`);
  }
}
for (let i = 0; i < 8; i++) next();
