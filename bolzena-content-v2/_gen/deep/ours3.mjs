import fs from 'fs'; import path from 'path';
const ROOT=process.argv[2]||'C:/projects/bolzena-content-v2/heroes';const only=process.argv[3];
const FILL=new Set(['draw','gauge','discard','payHp','payHpPct','nextAp','burn']);
const tg=fx=>fx.filter(f=>f.k==='dmg').map(f=>f.target||'oneEnemy').join();
let tot=0,dirc=0;const per=[];
for(const g of fs.readdirSync(ROOT))for(const f of fs.readdirSync(path.join(ROOT,g))){if(only&&!only.split(',').includes(f.replace('.json','')))continue;const j=JSON.parse(fs.readFileSync(path.join(ROOT,g,f),'utf8'));if(j.heroes[0].star!==3)continue;
 let hd=0;for(const c of j.cards.filter(c=>c.unique)){let cd=0;const bk=new Set(c.fx.map(x=>x.k));for(const o of (c.oracles||[])){tot++;const ok=new Set(o.fx.map(x=>x.k));
  const added=[...ok].filter(k=>!bk.has(k)&&!FILL.has(k)),removed=[...bk].filter(k=>!ok.has(k)&&!FILL.has(k));
  const typeCh=o.power||(o.type&&o.type!==c.type);const tgCh=tg(o.fx)!==tg(c.fx);
  const tagCh=JSON.stringify((o.tags||c.tags||[]).filter(t=>t!=='소멸'))!==JSON.stringify((c.tags||[]).filter(t=>t!=='소멸'));
  if(typeCh||added.length||removed.length||tgCh)cd++;}
  per.push(cd);dirc+=cd;hd+=cd;}}
const d=[0,0,0,0,0,0];per.forEach(x=>d[x]++);
console.log('신탁',tot,'방향 바꿈(강화화 · 새 효과 종류 · 빠진 종류 · 대상 바뀜)',dirc,(100*dirc/tot).toFixed(0)+'%','카드당 분포 0~5',d.join(' '));
// 같은 글 신탁(2026-10-10 빌드 피드백) — 같은 카드 안에서 fx · cost · power · type 이 JSON 으로 똑같은 신탁 둘
const dup=[];
for(const g of fs.readdirSync(ROOT))for(const f of fs.readdirSync(path.join(ROOT,g))){if(only&&!only.split(',').includes(f.replace('.json','')))continue;const j=JSON.parse(fs.readFileSync(path.join(ROOT,g,f),'utf8'));if(j.heroes[0].star<3&&j.heroes[0].id!=='마리')continue;
 for(const c of j.cards.filter(c=>c.unique)){const s=(c.oracles||[]).map(o=>JSON.stringify([o.fx,o.cost??null,!!o.power,o.type??null]));s.forEach((x,i)=>{const k=s.indexOf(x);if(k<i)dup.push(`${c.id} 신탁${k+1}=${i+1}`)});}}
console.log('같은 글 신탁',dup.length+(dup.length?' — '+dup.join(' · '):''));
