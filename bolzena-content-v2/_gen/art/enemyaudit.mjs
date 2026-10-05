// 적 그림 감사 — 그림 짝(_그림.json)마다 skel · atlas · png · 스킨(spine-core 4.1 로 실제로 읽음) · 아이콘 · 유니티 Resources 를 본다. node _gen/art/enemyaudit.mjs <결과.json>
import fs from "node:fs";
import { SkeletonBinary, TextureAtlas, AtlasAttachmentLoader } from "file:///C:/projects/볼제나/node_modules/@esotericsoftware/spine-core/dist/index.js";
const A = "C:/projects/볼제나/assets/", W = "C:/projects/bolzena-content-v2/world/villages/";
const U = "C:/projects/bolzena-unity/Assets/Bolzena/Resources/Spine/";
const P = JSON.parse(fs.readFileSync(W+"_그림.json","utf8"));
const cache = {};
function load(sp){
  if (cache[sp]) return cache[sp];
  const dir = A+sp, r = {ok:false, err:null, skins:[], pngMissing:[]};
  try {
    const files = fs.readdirSync(dir);
    const skel = files.find(f=>f.endsWith(".skel")), atl = files.find(f=>f.endsWith(".atlas"));
    if(!skel||!atl) throw new Error("skel/atlas 없음 "+files.join(","));
    const atlas = new TextureAtlas(fs.readFileSync(dir+"/"+atl,"utf8"));
    for (const p of atlas.pages){ if(!fs.existsSync(dir+"/"+p.name)) r.pngMissing.push(p.name); p.setTexture({getImage:()=>({width:p.width,height:p.height}),setFilters(){},setWraps(){},dispose(){}}); }
    const d = new SkeletonBinary(new AtlasAttachmentLoader(atlas)).readSkeletonData(new Uint8Array(fs.readFileSync(dir+"/"+skel)));
    r.skins = d.skins.map(s=>s.name); r.anims = d.animations.map(a=>a.name); r.ok = r.pngMissing.length===0; r.ver=d.version;
  } catch(e){ r.err = String(e.message||e); }
  return cache[sp]=r;
}
const skinName = s => (!s||s==="default") ? "default" : s.startsWith("Skin_") ? s : "Skin_"+s[0].toUpperCase()+s.slice(1);
const out = [];
for (const f of ["erpien","monatium","furry","ghost","spirit","dragon"]){
  const d = JSON.parse(fs.readFileSync(W+f+".json","utf8"));
  for (const e of d.enemies){
    const p = P[e.id]; const row = {v:f,id:e.id,name:e.name,boss:!!e.boss,nature:e.nature||"",hasArt:!!p};
    if (p){
      row.spine=p.spine; row.skin=p.skin; row.icon=p.icon;
      const r = load(p.spine); row.spineOk=r.ok; row.spineErr=r.err||(r.pngMissing.length?"png 없음 "+r.pngMissing:null);
      row.skinOk = r.skins.includes(skinName(p.skin)); row.skins=r.skins;
      row.iconOk = !!p.icon && fs.existsSync(A+"monster/"+p.icon+".png");
      const folder = p.spine.split("/").pop();
      row.unity = p.spine.includes("ingame/") ? "사도" : fs.existsSync(U+folder);
    }
    out.push(row);
  }
}
fs.writeFileSync(process.argv[2], JSON.stringify(out,null,1));
for (const r of out) if(!r.hasArt||!r.spineOk||!r.skinOk||!r.iconOk) console.log("문제", r.v, r.id, r.spine, r.skin, r.spineErr, "skin", r.skinOk, r.skins?.join("/"), "icon", r.iconOk, r.icon);
console.log("유니티 없음:", [...new Set(out.filter(r=>r.unity===false).map(r=>r.spine))].join(" "));
