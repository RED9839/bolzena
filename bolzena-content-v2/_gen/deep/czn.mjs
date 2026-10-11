import fs from 'fs'; import path from 'path';
const D='C:/projects/bolzena-content-v2/_measure/카제나_수집';
const SHALLOW=new Set(['수치만','수치','비용↓','비용↑','타수↑']);
const rows=[];let allLen=[],branches=0,shallowB=0,numOnly=0,powerB=0,typeB=0,redesign=0;const perCardShallow=[];const out=[];
for(const f of fs.readdirSync(D).filter(f=>f.endsWith('.md'))){
  const t=fs.readFileSync(path.join(D,f),'utf8');const name=f.replace('.md','');
  const dev=(t.match(/^\| 장치 \| (.*) \|$/m)||[])[1]||'';
  const cardRows=[...t.matchAll(/^\| (\*\*시동\*\*\(시작 덱\)|고유 [234]|완성형\(번뜩임 없음\)) \| ([^|]*) \| ([^|]*) \| ([^|]*) \| ([^|]*) \| ([^|]*) \|$/gm)];
  const lens=cardRows.map(m=>m[6].replace(/\s+/g,'').length);allLen.push(...lens);
  const roles=[...t.matchAll(/^\| (시동|고유[234]|완성형) \| ([^|]*) \| ([^|]*) \| /gm)].map(m=>m[3].trim());
  // 번뜩임
  const secs=t.split(/^### /m).slice(1).filter(s=>/^(시동|고유 [234]) — /.test(s));
  let hs=0,hb=0,hn=0,hp=0;
  for(const s of secs){let cs=0;
    for(const m of s.matchAll(/^\| ([^|]+ (I|II|III|IV|V)) \| ([^|]*) \| ([^|]*) \| ([^|]*) \| ([^|]*) \| ([^|]*) \|$/gm)){
      const cls=m[7].split('·').map(x=>x.trim().replace(/\(.*\)/,'')).filter(Boolean);
      branches++;hb++;const sh=cls.length>0&&cls.every(c=>SHALLOW.has(c)||c.startsWith('태그'));
      if(sh){shallowB++;cs++;hs++;} if(cls.includes('수치만')){numOnly++;hn++;}
      if(cls.some(c=>c.startsWith('강화화'))){powerB++;hp++;} if(cls.includes('유형 변경'))typeB++; if(cls.includes('재설계'))redesign++;}
    perCardShallow.push(cs);}
  const party=(t.match(/^- 파티 조합: (.*)$/m)||[])[1]||'';
  const teamDev=/다른 전투원|아군|파티원/.test(dev+cardRows.map(m=>m[6]).join(' '));
  out.push({name,dev:dev.replace(/\*\*/g,'').slice(0,90),roles:roles.join('/'),len:lens,hs,hb,hn,hp,teamDev,party:party.slice(0,60)});
}
const med=a=>{a=a.slice().sort((x,y)=>x-y);return a[Math.floor(a.length/2)]};const avg=a=>a.reduce((x,y)=>x+y,0)/a.length;
console.log('캐릭터',out.length,'카드',allLen.length,'글 길이(공백 뺀 글자) 중앙',med(allLen),'평균',avg(allLen).toFixed(1),'90%',allLen.slice().sort((a,b)=>a-b)[Math.floor(allLen.length*0.9)],'최대',Math.max(...allLen));
console.log('갈래',branches,'얕은',shallowB,(100*shallowB/branches).toFixed(0)+'%','수치만',numOnly,(100*numOnly/branches).toFixed(0)+'%','강화화',powerB,'유형변경',typeB,'재설계',redesign);
const d=[0,0,0,0,0,0];perCardShallow.forEach(x=>d[Math.min(5,x)]++);console.log('카드당 얕은 갈래 분포',d.join(' '));
console.log('팀 연결(장치나 카드 글에 아군/다른 전투원)',out.filter(o=>o.teamDev).length);
const rc={};out.forEach(o=>o.roles.split('/').forEach(r=>rc[r]=(rc[r]||0)+1));console.log('역할',JSON.stringify(rc));
fs.writeFileSync('czn.json',JSON.stringify(out,null,1));
for(const o of out)console.log([o.name,o.roles,o.len.join(','),`얕${o.hs}/${o.hb} 수${o.hn} 강${o.hp}`,o.teamDev?'팀':'',o.dev.slice(0,70)].join(' | '));
