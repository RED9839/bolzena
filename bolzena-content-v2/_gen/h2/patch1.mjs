import fs from 'fs';
const P='C:/projects/bolzena-content-v2/heroes/수인/';
const ed=(k,f)=>{const p=P+k+'.json';const j=JSON.parse(fs.readFileSync(p,'utf8'));f(j);fs.writeFileSync(p,JSON.stringify(j,null,2)+'\n');};
const card=(j,id)=>j.cards.find(c=>c.id===id);
ed('에피카',j=>{card(j,'에피카_u1').oracles[0]={name:'클라이맥스',fx:[{k:'stack',id:'에피콘',v:3},{k:'status',id:'협공',v:2},{k:'status',id:'사기',v:1}]};});
ed('코미_수영복',j=>{const c=card(j,'코미_수영복_u4');c.oracles[0]={name:'완판 행진',fx:[{k:'stack',id:'매출',v:2},{k:'stack',id:'주스 재고',v:2},{k:'status',id:'사기',v:2}]};c.oracles[3]={name:'단골 손님',fx:[{k:'stack',id:'매출',v:2},{k:'status',id:'사기',v:2},{k:'draw',v:1}]};});
