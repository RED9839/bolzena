// 몬스터 성격 모습 감사(2026-10-06) — 본 게임 Resources/Spine 의 skel 을 spine-core 4.1 로 실제로 읽어
//   성격 스킨(Skin_Naive …)이 있나 · 부착물이 있나(빈 스킨이 아닌가) · 성격끼리 모습이 다른가(부착물 경로 비교) · 성격 아이콘이 있나 를 본다.
//   node _gen/foes/looks.mjs <결과.json>
import fs from "node:fs";
import { SkeletonBinary, TextureAtlas, AtlasAttachmentLoader } from "file:///C:/projects/볼제나/node_modules/@esotericsoftware/spine-core/dist/index.js";
const U = "C:/projects/bolzena-unity/Assets/Bolzena/Resources/Spine/";
const IC = "C:/projects/bolzena-unity/Assets/Bolzena/Resources/Art/Monster/";
const W = "C:/projects/bolzena-content-v2/world/villages/";
const NAT = [["순수", "naive"], ["광기", "mad"], ["활발", "jolly"], ["우울", "gloomy"], ["냉정", "cool"]];
const art = JSON.parse(fs.readFileSync(W + "_그림.json", "utf8"));
function load(folder) {
  const dir = U + folder, files = fs.readdirSync(dir);
  const skel = files.find(f => f.endsWith(".skel.bytes")), atl = files.find(f => f.endsWith(".atlas.txt"));
  const atlas = new TextureAtlas(fs.readFileSync(dir + "/" + atl, "utf8"));
  for (const p of atlas.pages) p.setTexture({ getImage: () => ({ width: p.width, height: p.height }), setFilters() {}, setWraps() {}, dispose() {} });
  const d = new SkeletonBinary(new AtlasAttachmentLoader(atlas)).readSkeletonData(new Uint8Array(fs.readFileSync(dir + "/" + skel)));
  const skins = {};
  for (const s of d.skins) {
    const paths = [];
    s.attachments.forEach((m, slot) => { if (m) for (const k in m) { const a = m[k]; paths.push(slot + ":" + k + "=" + (a.path || a.name)); } });
    skins[s.name] = paths.sort();
  }
  return { skins, anims: d.animations.map(a => a.name) };
}
const out = {};
for (const [id, p] of Object.entries(art)) {
  if (!p.spine.startsWith("monsterspine/")) continue;
  const folder = p.spine.split("/").pop();
  if (out[folder]) { out[folder].ids.push(id); continue; }
  const r = { folder, ids: [id], dataSkin: p.skin, looks: {}, problems: [] };
  let sk;
  try { sk = load(folder); } catch (e) { r.problems.push("스파인 못 읽음 " + e.message); out[folder] = r; continue; }
  r.allSkins = Object.keys(sk.skins);
  const def = sk.skins["default"] || [];
  const sigs = {};
  for (const [ko, en] of NAT) {
    const want = folder === "orica" && en === "jolly" ? "Skin_Joly" : "Skin_" + en[0].toUpperCase() + en.slice(1);
    const name = r.allSkins.find(n => n.toLowerCase() === want.toLowerCase());
    const icon = fs.existsSync(IC + "icon_" + folder + en + ".png") ? "icon_" + folder + en : fs.existsSync(IC + "still_" + folder + "_" + en + ".png") ? "still_" + folder + "_" + en : null;
    const n = name ? sk.skins[name].length : 0;
    const sig = name ? sk.skins[name].join("|") : null;
    r.looks[ko] = { skin: name || null, attach: n, icon, anim: sk.anims.some(a => a.toLowerCase().endsWith("_" + en)) };
    if (sig) (sigs[sig] ||= []).push(ko);
  }
  const have = NAT.filter(([ko]) => r.looks[ko].skin && r.looks[ko].attach > 0).map(([ko]) => ko);
  r.have = have;
  r.distinct = Object.keys(sigs).length;
  r.sameGroups = Object.values(sigs).filter(g => g.length > 1);
  for (const [ko] of NAT) {
    const l = r.looks[ko];
    if (!l.skin) r.problems.push(`${ko} 스킨 없음`);
    else if (l.attach === 0) r.problems.push(`${ko} 빈 스킨(부착물 0)`);
    if (!l.icon) r.problems.push(`${ko} 아이콘 없음`);
  }
  for (const g of r.sameGroups) r.problems.push(`모습이 같은 성격: ${g.join("·")}(부착물 경로가 같다 — 색은 틴트일 수 있음)`);
  const want = p.skin && !p.skin.startsWith("Skin_") ? "Skin_" + p.skin[0].toUpperCase() + p.skin.slice(1) : p.skin;
  if (want && !r.allSkins.includes(want) && want !== "default") r.problems.push(`데이터 스킨 ${p.skin} 이 skel 에 없음`);
  out[folder] = r;
}
fs.writeFileSync(process.argv[2], JSON.stringify(out, null, 1));
for (const r of Object.values(out)) console.log(`${r.folder}\t모습 ${r.have?.length ?? 0}/5 (서로 다른 ${r.distinct})\t${r.problems.join(" · ") || "문제 없음"}`);
