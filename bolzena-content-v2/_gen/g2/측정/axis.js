const fs=require('fs'),path=require('path');const R=process.argv[2] || 'C:/projects/bolzena-content-v2/heroes';
const walk=(d,o=[])=>{for(const f of fs.readdirSync(d)){const p=path.join(d,f);fs.statSync(p).isDirectory()?walk(p,o):f.endsWith('.json')&&o.push(p)}return o};
const cnt={};let n=0;let zero=0;
for(const f of walk(R)){const j=JSON.parse(fs.readFileSync(f,'utf8'));const h=j.heroes&&j.heroes[0];if(!h)continue;n++;
 const s=new Set();const visit=o=>{if(Array.isArray(o))return o.forEach(visit);if(o&&typeof o==='object'){
  if(o.k==='status'&&o.id)s.add('st:'+o.id);
  if(['extra','make','discard','burn','shield','heal','tough','payHp','pull','draw'].includes(o.k))s.add('ev:'+o.k);
  if(o.k==='dmg'&&o.base==='def')s.add('ev:방어기반');
  for(const v of Object.values(o))visit(v);}};
 const cards=(j.cards||[]).filter(c=>c.hero===h.id);visit(cards);visit(h.passives);visit(h.ult);visit(h.keyword);
 for(const c of cards){if(c.cost===0)s.add('cost0');if((c.tags||[]).some(t=>t.startsWith('소멸')))s.add('tag:소멸');}
 for(const x of s)cnt[x]=(cnt[x]||0)+1;}
console.log('사도',n);console.log(Object.entries(cnt).sort((a,b)=>b[1]-a[1]).map(([k,v])=>k+' '+v).join(' · '));
