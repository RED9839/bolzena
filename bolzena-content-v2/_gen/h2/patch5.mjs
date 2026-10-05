import { load, save, setCard, hero, scale } from './lib.mjs';
const ed=(k,f)=>{const j=load(k);f(j,hero(j));save(k,j);};
const card=(j,id)=>j.cards.find(c=>c.id===id);
const up=(j,id,k)=>{const c=card(j,id); setCard(j,id,{fx:scale(c.fx,k)});};
ed('에피카',(j,h)=>{ setCard(j,'에피카_u3',{fx:[{k:'stack',id:'에피콘',v:1},{k:'draw',v:1},{k:'shield',ratio:1.0}]}); h.ult.fx[2].v=2; });
ed('루포',(j,h)=>{ up(j,'루포_u1',1.1); up(j,'루포_u3',1.1); up(j,'루포_u4',1.1); h.passives[0].fx[1].v=2; });
ed('바나',(j,h)=>{ h.passives[0].fx[0].v=1; setCard(j,'바나_u4',{fx:[{k:'cardStatus',id:'탐구심',v:2,to:'draw',n:3},{k:'status',id:'사기',v:1},{k:'draw',v:1}]}); up(j,'바나_u2',1.1); });
