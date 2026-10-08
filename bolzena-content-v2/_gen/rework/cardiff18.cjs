// 118명 카드 그림 짝 목록 — 백업(heroes_before_118) 과 지금 데이터를 견준다
const fs=require("fs"),path=require("path");
const A="C:/projects/_backup/heroes_before_118_20261008",B="C:/projects/bolzena-content-v2/heroes";
const TRIAL=new Set("리코타 티그 디아나_왕년 죠안 캬롯 마카샤 벨벳 샤샤 이프리트 니콜 쵸피 레비_졸업 쥬비 모모 힐데 큐이 코미".split(" "));
const out={opener:[],renamed:[],typecost:[],added:[],removed:[],fxonly:[],kw:[],forms:[]};
const sum={};
for(const r of fs.readdirSync(B))for(const f of fs.readdirSync(path.join(B,r))){const id=f.replace(".json","");if(TRIAL.has(id))continue;
const a=JSON.parse(fs.readFileSync(path.join(A,r,f),"utf8")),b=JSON.parse(fs.readFileSync(path.join(B,r,f),"utf8"));
const ha=a.heroes[0],hb=b.heroes[0],nm=hb.name;
const ua=ha.starter.filter(x=>a.cards.find(c=>c.id===x&&c.unique)),ub=hb.starter.filter(x=>b.cards.find(c=>c.id===x&&c.unique));
if(JSON.stringify(ua)!==JSON.stringify(ub))out.opener.push(`${nm}: ${ua.join(",")||"없음"} → ${ub.join(",")||"없음"}`);
const ca=Object.fromEntries(a.cards.map(c=>[c.id,c])),cb=Object.fromEntries(b.cards.map(c=>[c.id,c]));
for(const [cid,c] of Object.entries(cb)){const o=ca[cid];
 if(!o){out.added.push(`${cid} 「${c.name}」(${c.type} · ${c.cost}${c.token?" · 토큰":""})`);continue;}
 if(o.name!==c.name)out.renamed.push(`${cid}(${o.name} → ${c.name}${o.type!==c.type?`, ${o.type} → ${c.type}`:""}${o.cost!==c.cost?`, 비용 ${o.cost} → ${c.cost}`:""})`);
 else if(o.type!==c.type||o.cost!==c.cost)out.typecost.push(`${cid}(${o.type!==c.type?`${o.type} → ${c.type}`:""}${o.type!==c.type&&o.cost!==c.cost?", ":""}${o.cost!==c.cost?`비용 ${o.cost} → ${c.cost}`:""})`);
 else if(JSON.stringify([o.fx,o.tags])!==JSON.stringify([c.fx,c.tags]))out.fxonly.push(cid);}
for(const cid of Object.keys(ca))if(!cb[cid])out.removed.push(`${cid} 「${ca[cid].name}」`);
const kn=h=>[h.keyword,...(h.keywords||[])].filter(Boolean).map(k=>k.name);
const ka=kn(ha),kb=kn(hb);if(JSON.stringify(ka)!==JSON.stringify(kb))out.kw.push(`${nm}: ${ka.join("·")} → ${kb.join("·")}`);
const fa=(ha.forms||[]).map(x=>x.id),fb=(hb.forms||[]).map(x=>x.id);const nf=fb.filter(x=>!fa.includes(x));if(nf.length)out.forms.push(`${nm}: ${nf.join(",")}`);
}
const L=(t,a)=>`- **${t} ${a.length}**: ${a.join(" · ")||"없음"}`;
console.log([L("시동 칸 바꿈",out.opener),L("이름(· 종류 · 비용)이 바뀐 카드",out.renamed),L("이름은 같고 종류 · 비용이 바뀐 카드",out.typecost),L("새 카드(그림 없음)",out.added),L("빠진 카드",out.removed),L("새 변신",out.forms),L("고유 효과 이름 바뀜",out.kw),L("이름 · 종류 · 비용 같고 효과만 바뀐 카드(그림 짝 그대로)",out.fxonly)].join("\n"));
