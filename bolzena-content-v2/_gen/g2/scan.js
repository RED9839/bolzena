const fs=require('fs'),path=require('path');
const ROOT='C:/projects/bolzena-content-v2';
function walk(d,o=[]){for(const f of fs.readdirSync(d)){const p=path.join(d,f);if(fs.statSync(p).isDirectory())walk(p,o);else if(f.endsWith('.json'))o.push(p);}return o;}
for(const f of walk(ROOT+'/heroes')){const j=JSON.parse(fs.readFileSync(f,'utf8'));
 const h=j.heroes&&j.heroes[0];
 if(j.equips)console.log(path.relative(ROOT,f), h&&h.name, JSON.stringify(j.equips.map(e=>[e.id,e.name,e.affinity,e.slot,e.grade])));
}
