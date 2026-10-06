const fs=require('fs'),path=require('path');
const ROOT='C:/projects/bolzena-content-v2';
function walk(d,o=[]){for(const f of fs.readdirSync(d)){const p=path.join(d,f);if(fs.statSync(p).isDirectory())walk(p,o);else if(f.endsWith('.json'))o.push(p);}return o;}
const want=process.argv.slice(2);
for(const f of walk(ROOT+'/heroes')){const j=JSON.parse(fs.readFileSync(f,'utf8'));const h=j.heroes[0];
 if(!want.includes(h.id)&&!want.includes(h.name))continue;
 console.log('=== ',path.relative(ROOT,f),h.id,h.name,h.role,h.nature,'hp',h.hp,'atk',h.atk,'def',h.def);
 for(const k of [h.keyword,...(h.keywords||[])].filter(Boolean))console.log(' KW',JSON.stringify({name:k.name,desc:k.desc,carrier:k.carrier,cap:k.cap,per:k.per,guard:k.guard}));
 for(const p of h.passives||[])console.log(' P',JSON.stringify(p));
 if(j.equips)console.log(' EQ',JSON.stringify(j.equips));
}
