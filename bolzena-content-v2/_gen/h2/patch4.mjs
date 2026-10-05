import { load, save, setCard, hero } from './lib.mjs';
const ed=(k,f)=>{const j=load(k);f(j,hero(j));save(k,j);};
ed('쵸피',(j)=>setCard(j,'쵸피_u3',{fx:[{k:'status',id:'잔광',v:2},{k:'stack',id:'수련',v:2},{k:'status',id:'불굴',v:1}]}));
ed('티그',(j)=>setCard(j,'티그_u4',{fx:[{k:'status',id:'불굴',v:1},{k:'atkMod',v:0.2,run:true,target:'self'},{k:'stack',id:'장작 패기',v:2}]}));
