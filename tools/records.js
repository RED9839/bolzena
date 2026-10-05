// 판 기록 모으기 — 게임이 2층 보스(판의 끝) 앞에서 내려받게 한 「볼제나-기록-*.json」 을 읽어 밸런스를 본다(js/run.js recordOf).
// 기록 v 2 = 마을 판(두 층 · village — 2026-10-04), v 1 = 옛 판(세 층 + 우로스). 둘을 따로 센다
//
//   node tools/records.js              내려받기 폴더(~/Downloads)에서 찾는다
//   node tools/records.js 폴더 …       그 폴더들(또는 파일)에서
//
// 한 판에 두 파일이 생긴다(「2층 보스 전」 · 「2층 보스 승리/패배」 — 옛 판은 「우로스 …」). 같은 판(seed · 판 꼴)이면 결과가 든 쪽만 센다.
import fs from "fs";
import path from "path";
import os from "os";

const args = process.argv.slice(2);
const roots = args.length ? args : [path.join(os.homedir(), "Downloads")];
const files = [];
for (const r of roots) {
  if (!fs.existsSync(r)) continue;
  if (fs.statSync(r).isFile()) { files.push(r); continue; }
  for (const f of fs.readdirSync(r)) if (/^볼제나-기록-.*\.json$/.test(f)) files.push(path.join(r, f));
}
const bySeed = new Map();
for (const f of files) {
  let rec;
  try { rec = JSON.parse(fs.readFileSync(f, "utf8")); } catch { continue; }
  if (!rec || rec.kind !== "bolzena-record") continue;
  const key = `${rec.v || 1}:${rec.seed}`;
  const old = bySeed.get(key);
  const done = (x) => /승리|패배/.test(x.stage);
  if (!old || (done(rec) && !done(old)) || (done(rec) === done(old) && rec.at > old.at)) bySeed.set(key, rec);
}
const recs = [...bySeed.values()].sort((a, b) => a.at.localeCompare(b.at));
if (!recs.length) { console.log(`기록 파일이 없습니다 — ${roots.join(", ")}`); process.exit(0); }

const pct = (a, b) => (b ? Math.round((a / b) * 100) : 0) + "%";
const avg = (xs) => (xs.length ? (xs.reduce((a, b) => a + b, 0) / xs.length).toFixed(1) : "-");
console.log(`판 ${recs.length}개 (파일 ${files.length}개)\n`);

const kinds = {};
const VKO = { worldtree: "세계수", monatium: "모나티엄" };
const lastOf = (r) => (r.v >= 2 ? "2층 보스" : "우로스");        // 판의 끝 — 마을 판은 2층 보스, 옛 판은 우로스
const byEnd = {};                                                 // { "세계수 2층 보스": [이김, 끝남] }
for (const r of recs) {
  const fin = r.v >= 2 ? r.fights.filter((f) => f.kind === "boss" && f.floor === 2).pop() : r.fights.find((f) => f.kind === "final");
  const res = /승리/.test(r.stage) ? "승리" : /패배/.test(r.stage) ? "패배" : "결과 없음";
  const where = r.v >= 2 ? `${VKO[r.village] || r.village || "?"} ${lastOf(r)}` : lastOf(r);
  if (res !== "결과 없음") { const e = (byEnd[where] = byEnd[where] || [0, 0]); e[1]++; if (res === "승리") e[0]++; }
  console.log(`■ ${new Date(r.at).toLocaleString("ko-KR", { hour12: false }).slice(0, -3)} · ${r.party.map((p) => p.ko).join(" · ")} · ${where} ${res}${fin ? ` (${fin.turns}턴)` : ""}`);
  console.log(`  덱 ${r.deck.reduce((a, d) => a + d.n, 0)}장 · 신탁 ${r.deck.filter((d) => d.flash).length} · 골드 ${r.gold} · 파티 HP ${r.partyHp}/${r.partyMaxHp}`);
  for (const f of r.fights) {
    const k = (kinds[f.kind] = kinds[f.kind] || { n: 0, turns: [], loss: [], lose: 0 });
    k.n++; k.turns.push(f.turns); k.loss.push(f.hp[2] ? ((f.hp[0] - f.hp[1]) / f.hp[2]) * 100 : 0);
    if (f.result !== "win") k.lose++;
  }
}
console.log("");
for (const [where, [won, ended]] of Object.entries(byEnd)) console.log(`${where} ${won}/${ended} 승리 (${pct(won, ended)})`);
console.log("");
console.log("싸움 갈래별 — 판 수 · 평균 턴 · 평균 파티 HP 손실(최대의 %) · 패배");
const KO = { fight: "일반", elite: "엘리트", event: "이벤트", boss: "층 보스", final: "우로스" };
for (const [k, v] of Object.entries(kinds)) console.log(`  ${(KO[k] || k).padEnd(5)} ${String(v.n).padStart(4)}  ${avg(v.turns).padStart(5)}턴  ${avg(v.loss).padStart(5)}%  ${v.lose}`);
