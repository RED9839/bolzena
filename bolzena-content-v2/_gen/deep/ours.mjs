if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
import fs from 'fs'; import path from 'path';
const DATA=process.argv[2]||'C:/projects/bolzena-content-v2';const ROOT=path.join(DATA,'heroes');
const CARDS=process.argv[3]||'cards_now.txt';
// 카드 글
const txt={};let cur=null;
for(const l of fs.readFileSync(CARDS,'utf8').split(/\r?\n/)){const m=l.match(/^(\S+) \[(\w+)\] (\S+) 「([^」]*)」 (.*)$/);
 if(m){cur=m[1];txt[cur]=m[5].replace(/\s+\(코스트당 값 [\d.]+\)$/,'');}else if(cur&&l.trim())txt[cur]+=' '+l.replace(/\s+\(코스트당 값 [\d.]+\)$/,'');}
const len=s=>(s||'').replace(/\s+/g,'').length;
// solo
const solo={};const sm=fs.readFileSync('C:/projects/bolzena-content-v2/_measure/갈래_세분화/06_118/solo_세계맞춤.md','utf8');
for(const m of sm.matchAll(/^\| ([^|]+) \| (딜러|서포터|탱커) \| ([^|]+) \| (\d+) \| ([\d.]+)% \| ±([\d.]+) \|$/gm))solo[m[1].trim()]=+m[5];
const sig=fx=>JSON.stringify((fx||[]).map(f=>[f.k,f.id||'',f.target||'',f.hits||'',f.base||'',f.k==='power'?JSON.stringify((f.rules||[]).map(r=>[r.when,(r.fx||[]).map(x=>x.k+(x.id||''))])):'']));
const kinds=fx=>new Set((fx||[]).map(f=>f.k));
const TEAM=/"who":"(any|other)"|"target":"(oneAlly|otherAllies|strongestAlly|allAllies)"|"owner":"other"|"k":"feed"|"k":"castOther"|"marked"|"hero:/;
const rows=[];
for(const d of fs.readdirSync(ROOT))for(const f of fs.readdirSync(path.join(ROOT,d))){
 const j=JSON.parse(fs.readFileSync(path.join(ROOT,d,f),'utf8'));const h=j.heroes[0];if(h.star!==3)continue;
 const uniq=j.cards.filter(c=>c.unique);let orc=0,shallow=0,numOnly=0,pw=0;const lens=[];
 for(const c of uniq){lens.push(len(txt[c.id]));for(const o of c.oracles||[]){orc++;const same=sig(o.fx)===sig(c.fx);const bk=kinds(c.fx),ok=kinds(o.fx);
   const diff=[...ok].filter(k=>!bk.has(k)).length+[...bk].filter(k=>!ok.has(k)).length;
   if(o.power)pw++;if(same&&!o.power)numOnly++;if(same||(!o.power&&diff===0))shallow++;}}
 const kws=[h.keyword,...(h.keywords||[])].filter(Boolean);
 const pasRules=(h.passives||[]).length;const pasNames=new Set((h.passives||[]).map(p=>p.name)).size;
 const kwRules=kws.reduce((a,k)=>a+(k.rules||[]).length,0);
 const fxN=JSON.stringify([h.passives,kws.map(k=>k.rules)]).match(/"k":/g)?.length||0;
 const team=TEAM.test(JSON.stringify([h.passives,kws,uniq.map(c=>c.fx)]));
 const ks=new Set();for(const c of uniq)for(const x of JSON.stringify(c.fx).matchAll(/"k":"(\w+)"/g))ks.add(x[1]);for(const p of h.passives||[])ks.add('on:'+p.when.on);
 for(const k of kws){ks.add('c:'+k.carrier);for(const z of ['reserve','hunt','guard','mode','wrap','onMax','per','decay','consume'])if(k[z])ks.add('kw:'+z);}
 rows.push({file:d+'/'+f.replace('.json',''),name:h.name,role:h.role,style:h.style,uniq:uniq.length,orc,shallow,numOnly,pw,lens,maxLen:Math.max(...lens),avgLen:lens.reduce((a,b)=>a+b,0)/lens.length,pasRules,pasNames,kwRules,fxN,team,solo:solo[h.name],ks:[...ks],forms:(h.forms||[]).length,tokens:j.cards.filter(c=>c.token).length});
}
// 같은 갈래 비슷함
for(const r of rows){let best=0,who='';for(const o of rows){if(o===r||o.style!==r.style)continue;const A=new Set(r.ks),B=new Set(o.ks);const i=[...A].filter(x=>B.has(x)).length;const u=new Set([...A,...B]).size;const J=i/u;if(J>best){best=J;who=o.name;}}r.sim=best;r.simWho=who;}
fs.writeFileSync('ours.json',JSON.stringify(rows,null,1));
const avg=a=>a.reduce((x,y)=>x+y,0)/a.length,med=a=>{a=a.slice().sort((x,y)=>x-y);return a[Math.floor(a.length/2)]};
const L=rows.flatMap(r=>r.lens);const O=rows.reduce((a,r)=>a+r.orc,0);
console.log('사도',rows.length,'고유카드',L.length,'신탁',O);
console.log('글 길이 중앙',med(L),'평균',avg(L).toFixed(1),'90%',L.slice().sort((a,b)=>a-b)[Math.floor(L.length*.9)],'최대',Math.max(...L));
console.log('얕은 신탁',rows.reduce((a,r)=>a+r.shallow,0),'수치만',rows.reduce((a,r)=>a+r.numOnly,0),'강화화',rows.reduce((a,r)=>a+r.pw,0));
const sh=[];for(const r of rows){} 
console.log('고유카드 수 분포',JSON.stringify(rows.reduce((o,r)=>(o[r.uniq]=(o[r.uniq]||0)+1,o),{})));
console.log('패시브 규칙 수 분포',JSON.stringify(rows.reduce((o,r)=>(o[r.pasRules]=(o[r.pasRules]||0)+1,o),{})),'패시브+키워드 규칙 fx 조각 중앙',med(rows.map(r=>r.fxN)),'평균',avg(rows.map(r=>r.fxN)).toFixed(1));
console.log('팀 고리 있는 사도',rows.filter(r=>r.team).length);
const st={};for(const r of rows)(st[r.style]??=[]).push(r);
for(const [s,a] of Object.entries(st).sort((x,y)=>y[1].length-x[1].length))console.log(s,a.length,'유사도 평균',avg(a.map(r=>r.sim)).toFixed(2),'완주 범위',Math.min(...a.map(r=>r.solo||99)),'~',Math.max(...a.map(r=>r.solo||0)),'|',a.map(r=>r.name).join(' '));
const S=rows.map(r=>r.solo).filter(x=>x!=null);console.log('혼자 완주 평균',avg(S).toFixed(1),'표준편차',Math.sqrt(avg(S.map(x=>(x-avg(S))**2))).toFixed(1),'범위',Math.min(...S),Math.max(...S));
