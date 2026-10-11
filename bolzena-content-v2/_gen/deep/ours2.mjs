import fs from 'fs'; import path from 'path';
const ROOT='C:/projects/bolzena-content-v2/heroes';
const sig=fx=>JSON.stringify((fx||[]).map(f=>[f.k,f.id||'',f.target||'',f.hits||'',f.base||'',f.k==='power'?JSON.stringify((f.rules||[]).map(r=>[r.when,(r.fx||[]).map(x=>x.k+(x.id||''))])):'']));
const d=[0,0,0,0,0,0],dn=[0,0,0,0,0,0];let cards=0,dir3=0;
for(const g of fs.readdirSync(ROOT))for(const f of fs.readdirSync(path.join(ROOT,g))){const j=JSON.parse(fs.readFileSync(path.join(ROOT,g,f),'utf8'));if(j.heroes[0].star!==3)continue;
 for(const c of j.cards.filter(c=>c.unique)){cards++;let n=0,s=0;const bk=new Set(c.fx.map(x=>x.k));for(const o of c.oracles){const same=sig(o.fx)===sig(c.fx);const ok=new Set(o.fx.map(x=>x.k));const diff=[...ok].filter(k=>!bk.has(k)).length+[...bk].filter(k=>!ok.has(k)).length;if(same&&!o.power)n++;if(same||(!o.power&&diff===0))s++;}
 d[s]++;dn[n]++;if(5-s>=3&&n<=1)dir3++;}}
console.log('카드',cards,'얕은 분포',d.join(' '),'수치만 분포',dn.join(' '),'체크리스트③ 통과 카드',dir3);
