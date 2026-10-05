import fs from "node:fs";
import { SkeletonBinary, TextureAtlas, AtlasAttachmentLoader } from "file:///C:/projects/볼제나/node_modules/@esotericsoftware/spine-core/dist/index.js";
for (const sp of process.argv.slice(2)) {
  const dir="C:/projects/볼제나/assets/monsterspine/"+sp, files=fs.readdirSync(dir);
  const atlas=new TextureAtlas(fs.readFileSync(dir+"/"+files.find(f=>f.endsWith(".atlas")),"utf8"));
  for (const p of atlas.pages) p.setTexture({getImage:()=>({width:p.width,height:p.height}),setFilters(){},setWraps(){},dispose(){}});
  const d=new SkeletonBinary(new AtlasAttachmentLoader(atlas)).readSkeletonData(new Uint8Array(fs.readFileSync(dir+"/"+files.find(f=>f.endsWith(".skel")))));
  console.log("==",sp, "anims:", d.animations.map(a=>a.name).join(","));
  for (const s of d.skins){ const paths=new Set(); for (const e of s.getAttachments()) paths.add(e.attachment.path||e.attachment.name); const arr=[...paths]; console.log(" ",s.name, arr.length, arr.slice(0,8).join(" ")); }
}
