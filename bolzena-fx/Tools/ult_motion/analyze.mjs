// 고학년 몸짓 · 이동 · 소리 표 — node analyze.mjs <out.json>
import fs from "node:fs";
import {load,wear,track,refHeight,S} from "./pose.mjs";
const AS="C:/projects/볼제나/assets/";
const roster=JSON.parse(fs.readFileSync("C:/projects/bolzena-unity/Assets/Resources/RunUI/roster.json","utf8")).heroes;
// ── 유니티 FxLibrary(읽기만) ──
const fxlib={};
{ const y=fs.readFileSync("C:/projects/bolzena-unity/Assets/BolzenaFxData/Resources/BolzenaFx/FxLibrary.asset","utf8").split(/\r?\n/);
  let cur=null,list=null; const un=s=>s.startsWith('"')?JSON.parse(s):s;
  for(const l of y){ let m;
    if((m=l.match(/^  - Key: (.*)$/))){ cur={key:un(m[1].trim()),Ult:[],Attack:[],Power:[],Skill:[],Sig:[]}; fxlib[cur.key]=cur; list=null; continue; }
    if(!cur) continue;
    if(/^  [A-Za-z]/.test(l)){ cur=null; continue; }
    if((m=l.match(/^    (Ult|Attack|Power|Skill|Sig):\s*(\[\])?$/))){ list=cur[m[1]]; continue; }
    if((m=l.match(/^    - (.*)$/)) && list){ list.push(m[1].trim()); continue; }
    if(/^    \w/.test(l)) list=null;
  } }
// ── ogg 길이(헤더만 읽는다 — 재생 안 함) ──
function oggDur(p){
  const b=fs.readFileSync(p); const o=27+b[26];
  let i=b.length-27; while(i>0 && !(b[i]===0x4f&&b[i+1]===0x67&&b[i+2]===0x67&&b[i+3]===0x53)) i--;
  const g=Number(b.readBigInt64LE(i+6));
  if(b.toString("latin1",o,o+8)==="OpusHead"){ const pre=b.readUInt16LE(o+10); return Math.max(0,(g-pre)/48000); }
  if(b.toString("latin1",o+1,o+7)==="vorbis"){ const r=b.readUInt32LE(o+12); return g/r; }
  return 0;
}
const ms=x=>Math.round(x*1000);
function find(d,n){ if(!n) return null; return d.findAnimation(n) || d.animations.find(a=>a.name.toLowerCase()===n.toLowerCase()) || null; }
function evsOf(d,n){ const a=find(d,n); if(!a) return []; const r=[]; for(const tl of a.timelines) if(tl instanceof S.EventTimeline) for(const e of tl.events) r.push({name:e.data.name,t:e.time,s:e.stringValue??"",i:e.intValue}); return r.sort((x,y)=>x.t-y.t); }
const has=(d,n)=>!!find(d,n), dur=(d,n)=>{const a=find(d,n);return a?a.duration:0;};
const FRESH=/(^|,)100000[34](,|$)/;
const fresh=(d,n)=>evsOf(d,n).some(e=>e.t<0.15&&((e.name==="Voice"&&e.s==="1")||FRESH.test(e.s)));
function partsOf(d,base){ const p=[]; for(let k=1;k<30;k++){ const n=has(d,`${base}_${k}`)?`${base}_${k}`:has(d,`${base}_${k}_Loop`)?`${base}_${k}_Loop`:null; if(!n) break; p.push(find(d,n).name);} return p; }
// 유니티 SpineMotion.WaysOf 그대로
function waysUnity(d,base){ const ways=[]; for(const n of partsOf(d,base)) if(!ways.length||fresh(d,n)) ways.push([n]); else ways[ways.length-1].push(n); return ways; }
const nextAni=(d,n)=>{ const e=evsOf(d,n).filter(x=>x.name==="NextAni"); return e.length? e[e.length-1].s : undefined; };
const evSig=(d,n)=>evsOf(d,n).filter(e=>e.name!=="NextAni").map(e=>e.t.toFixed(2)).join(",");
const twin=(d,a,b)=>Math.abs(dur(d,a)-dur(d,b))<0.034 && evSig(d,a)===evSig(d,b) && evSig(d,a)!=="";
// 원작 기준 갈래 — NextAni 를 먼저 따르고, 없으면 웹판 규칙(다음 번호 조각 · 목소리/표시로 새로 시작하면 다른 갈래).
// 같은 길이 · 같은 이벤트 시각의 다음 번호 조각은 이어지는 것이 아니라 바꿔 끼우는 갈래(피라 · 스노키)
function waysOrig(d,base){
  const parts=partsOf(d,base), idx=n=>parts.indexOf(n), notes=[];
  const used=new Set(), ways=[];
  const walk=(head)=>{
    const w=[], seen=new Set(); let cur=head;
    while(cur){
      w.push(cur); seen.add(cur); used.add(cur);
      const na=nextAni(d,cur);
      if(na!==undefined){
        if(na==="End") break;
        const tgt = na==="" ? parts[idx(cur)+1] : (find(d,na)||{}).name;
        if(!tgt){ notes.push(`${cur} NextAni→${na||"(다음)"} 대상 없음 — 여기서 끝`); break; }
        if(seen.has(tgt)){ notes.push(`${cur} NextAni→${tgt} 되돌이(반복)`); break; }
        cur=tgt; continue;
      }
      let k=idx(cur)+1, nx=null;
      while(k<parts.length){
        const c=parts[k];
        if(fresh(d,c)) break;
        const na2=nextAni(d,c);
        if(na2 && seen.has((find(d,na2)||{}).name)){ notes.push(`${c} → ${na2} 되돌이 조각(대상이 여럿일 때 되풀이하는 갈래로 보임) — 한 번 하는 계획에선 건너뜀`); used.add(c); k++; continue; }
        if(twin(d,c,cur)){ notes.push(`${c} = ${cur} 와 같은 길이 · 같은 이벤트 시각 — 바꿔 끼우는 갈래`); break; }
        nx=c; break;
      }
      cur=nx;
    }
    return w;
  };
  for(const p of parts){
    if(used.has(p)) continue;
    if(ways.length && !fresh(d,p)){
      const w0=ways[0]; const tw=w0.find(x=>twin(d,x,p));
      if(tw){ ways.push([...w0.slice(0,w0.indexOf(tw)),...walk(p)]); continue; }
      continue;
    }
    ways.push(walk(p));
  }
  const orphan=parts.filter(p=>!ways.some(w=>w.includes(p)));
  return {ways,notes,orphan};
}
// 때리는 순간 — SpineMotion.StrikeOf 그대로
function strikeOf(d,names,fixAt){
  let off=0,from=0; const evs=[];
  for(const n of names){ for(const e of evsOf(d,n)) evs.push({...e,t:off+e.t}); off+=dur(d,n); if(/_loop$/i.test(n)) from=off; }
  const own=e=>e.name==="Event"&&e.s.split(",").some(x=>+x>=1000100);
  const late=evs.filter(e=>e.t>=from-1e-3);
  const hit=late.find(e=>e.name==="SFX"&&(+e.s>=2||from>0))||late.find(own);
  const at=fixAt!=null?fixAt:ms(hit?hit.t:dur(d,names[0])*0.45);
  const marks=[...new Set(late.filter(e=>(own(e)||e.name==="SFX")&&e.t*1000>=at-1).map(e=>ms(e.t)))];
  const end=Math.min(marks.length?marks[marks.length-1]:at,at+2600);
  return {at,end,marks:marks.filter(m=>m<=end),src:fixAt!=null?"move":hit?(hit.name==="SFX"?"SFX"+hit.s:"Event"):"45%"};
}
// 웹판 · 유니티 DASH 표
const DASH={ "에르핀":{go:["Ultimate1_1",0.85],hit:["Ultimate1_3",0],loop:{"Ultimate1_2_Loop":2},back:560,move:"dash"},
  "에르핀_왕도":{go:["Ultimate1_1",1.96],land:["Ultimate1_2",0.05],hit:["Ultimate1_2",0.93],home:[["Ultimate1_2",2.78],["Ultimate1_3",0.5]],back:0,move:"leap"} };
const expand=(key,w)=>{ const dc=DASH[key]; if(!dc||!dc.loop) return w; return w.flatMap(n=>Array(dc.loop[n]||1).fill(n)); };
const whenIn=(d,names,p)=>{ let off=0; for(const n of names){ if(n===p[0]) return ms(off+p[1]); off+=dur(d,n);} return null; };
// ── 효과음 칸(유니티 SfxMap.SlotsIn · SlotMap 그대로) ──
const SLOT_GROUP=/^ultimate/, SLOT_HIT=/hit|heal/, EARLY=/^(cast|casting|ready|charge|start|normal|spawn|swing|jump|magiccircle|holypower|upgrade|mark|crew|crowd)$/, LATE=/^(explosion|laserexplosion|finalexplosion|end|revival)$/;
function slotKey(tail){ const head=tail.match(SLOT_GROUP)[0]; const r=tail.slice(head.length).replace(/^[_-]+/,""); if(!r) return [0,0,0,0,0];
  let m=r.match(/^0*(\d+)(?:-(\d+))?$/); if(m) return [0,1,0,+m[1],m[2]?+m[2]:0];
  m=r.match(/^([a-z]+)_?0*(\d*)/); const w=m?m[1]:r; return [0,2,EARLY.test(w)?0:LATE.test(w)?2:1,m&&m[2]?+m[2]:0,0]; }
function slotsIn(files){ const list=[],hit=[];
  for(const {f,t} of files){ if(!SLOT_GROUP.test(t)) continue; (SLOT_HIT.test(t)?hit:list).push({k:slotKey(t),t,f}); }
  const cmp=(a,b)=>{ for(let i=0;i<5;i++) if(a.k[i]!==b.k[i]) return a.k[i]-b.k[i]; return a.t<b.t?-1:1; };
  list.sort(cmp); hit.sort(cmp); return {list:list.map(x=>x.f),hit:hit.map(x=>x.f)}; }
function slotMap(sl,ns){ const u=[...new Set(ns)].sort((a,b)=>a-b), L=sl.list.length; if(!L||!u.length) return null; const m={};
  if(u.length===L){ u.forEach((n,i)=>m[n]=sl.list[i]); return m; }
  if(u.length===L+1&&sl.hit.length){ u.forEach((n,i)=>m[n]=i<L?sl.list[i]:"hit"); return m; } return null; }
// ── 붙은 그림(변신 판정) ──
function attachAt(d,anim,t){
  const sk=new S.Skeleton(d); const w=wear(d); if(w) sk.setSkin(w); sk.setSlotsToSetupPose(); sk.setToSetupPose();
  const a=find(d,anim); if(a) a.apply(sk,t,t,false,null,1,S.MixBlend.setup,S.MixDirection.mixIn);
  const s=new Set(); for(const sl of sk.slots){ const at=sl.getAttachment(); if(at && sl.color.a>0.05) s.add(sl.data.name+"="+at.name); } return s;
}
const diffN=(a,b)=>{ let n=0; for(const x of a) if(!b.has(x)) n++; for(const x of b) if(!a.has(x)) n++; return n; };
// ── 이동 판정 낱말 ──
const RANGED=/(proj|bullet|missile|shot|shoot|arrow|muzzle|laser|lazer|beam|throw|crosshair|cannon|gun|rain|meteor|summon|drone|tornado|lightning)/;
const MELEE=/(slash|stab|punch|kick|smash|swing|claw|bite|_cut|stomp|rush|dash|crash|uppercut|blade|sword|spintop|pierce|hammer|jump|_down)/;
const GAP={front:4.75,mid:6.4,back:8.05}, REACH=1.9;   // 유니티 싸움터: 사도 발 x -1.55 · -3.2 · -4.85, 적 하나 3.2 — 닿는 자리는 적 앞 ≈1.9
const CUTIN_LEAD=1510;   // 유니티: 컷인 시작에 목소리, 컷인 1.35초 + 닫힘 0.16초 뒤 몸짓 시작
const out={_meta:{}, heroes:{}};
let cnt=0;
for(const h of roster){
  const key=h.key, art=h.art; const R={ko:h.ko,art,row:h.row,role:h.role,ult:h.ult};
  let d; try{ d=load(key);}catch(e){ R.error=String(e); out.heroes[key]=R; continue; }
  const skin=wear(d); const ref=refHeight(d,skin); const H=ref.pt||ref.bbox; R.bodyH=Math.round(H);
  const hasUlt=has(d,"Ultimate1_1");
  let orig, uni, fallback=null;
  if(hasUlt){ orig=waysOrig(d,"Ultimate1"); uni=waysUnity(d,"Ultimate1"); }
  else{
    uni=[[has(d,"Skill1_1")?"Skill1_1":"Attack1_1"]];
    const st=d.animations.find(a=>/^Ultimate1_1_Start$/i.test(a.name));
    if(st){ const end=d.animations.find(a=>/^Ultimate1_1_End$/i.test(a.name));
      orig={ways:[[st.name,...(end?[end.name]:[])]],notes:["Ultimate1_1 이 없고 Ultimate1_1_Start · _M0ve · _Id1e · _End 로 나뉨 — 유니티는 Skill1_1 로 떨어진다. Start → End 를 잇고 M0ve · Id1e 는 머무는 고리"],
        orphan:d.animations.filter(a=>/^Ultimate1_1_(M0ve|Id1e)$/i.test(a.name)).map(a=>a.name)}; fallback="Ultimate1_1_Start"; }
    else { orig=waysOrig(d,"Skill1"); fallback="Skill1"; if(!orig.ways.length) orig={ways:[["Attack1_1"]],notes:[],orphan:[]}; }
  }
  R.fallback=fallback;
  R.ways=orig.ways.map(w=>expand(key,w)); R.wayNotes=orig.notes; R.orphan=orig.orphan;
  R.unityWays=uni.map(w=>expand(key,w));
  const allNames=[...new Set([...R.ways.flat(),...R.orphan,...R.unityWays.flat()])];
  R.related=d.animations.map(a=>a.name).filter(n=>/ultimate|^AS\d_/i.test(n)&&!allNames.includes(n)).map(n=>({name:n,ms:ms(dur(d,n))}));
  R.pieces={};
  for(const nm of allNames){
    const seen=new Set();
    const evs=evsOf(d,nm).map(e=>{ const k=e.name==="SFX"?"sound":e.name==="Voice"?"voice":e.name==="NextAni"?"next":
      /(^|,)1000002(,|$)/.test(e.s)?"go":/(^|,)1000008(,|$)/.test(e.s)?"back":/(^|,)1000013(,|$)/.test(e.s)?"go?":/(^|,)1000014(,|$)/.test(e.s)?"back?":
      /(^|,)100000[34](,|$)/.test(e.s)?"start":e.s.split(",").some(x=>+x>=1000100)?"fx":"mark";
      return {n:e.name,t:ms(e.t),s:e.s,k}; }).filter(e=>{ const q=e.n+e.t+e.s; if(seen.has(q)) return false; seen.add(q); return true; });
    R.pieces[nm]={ms:ms(dur(d,nm)),next:nextAni(d,nm)??null,fresh:fresh(d,nm),events:evs};
  }
  // 빠진 조각 · 토막 갈래 · 섞인 갈래
  const miss=[];
  R.ways.forEach((w,i)=>{
    const u=R.unityWays.find(x=>x.join()===w.join())||R.unityWays.find(x=>x[0]===w[0]);
    if(!u){ miss.push({way:i,kind:"갈래 없음",pieces:[...new Set(w)]}); return; }
    const lack=[...new Set(w)].filter(p=>!u.includes(p)); if(lack.length) miss.push({way:i,kind:"빠짐",pieces:lack});
    const extra=[...new Set(u)].filter(p=>!w.includes(p)); if(extra.length) miss.push({way:i,kind:"섞임(다른 갈래 · 되돌이 조각까지 이어 틂)",pieces:extra});
  });
  R.unityWays.forEach((u,j)=>{ if(!R.ways.some(w=>w[0]===u[0])) miss.push({unityWay:j,kind:"토막 갈래(중간 조각만 트는 갈래 — 무작위로 걸리면 몸짓이 반쪽)",pieces:u}); });
  R.missing=miss;
  // 근거
  const fx=fxlib[key]||{Ult:[],Attack:[]};
  const ultW=fx.Ult.map(x=>x.replace(/^fx_[a-z0-9]+_/,"")).join(" ");
  const shotBone=!!d.findBone("Point_Attack1_Shot");
  const atkRanged=fx.Attack.length? fx.Attack.some(x=>/(proj|bullet|missile|shot|arrow|muzzle|laser|lazer|beam|throw|crosshair)/.test(x)) : null;
  const ultRanged=RANGED.test(ultW), ultMelee=MELEE.test(ultW);
  const front=h.row==="front";
  R.fx={ult:fx.Ult,attackMelee:atkRanged===null?null:!atkRanged,shotBone};
  const mN=fx.Ult.filter(x=>MELEE.test(x)).length, rN=fx.Ult.filter(x=>RANGED.test(x)).length;
  const melee = !shotBone && ((front && rN<=mN) || (mN>0 && mN>rN));
  const meleeWhy=[front?"앞줄":null, mN?`근접 이펙트 ${mN}개(${fx.Ult.filter(x=>MELEE.test(x)).map(x=>x.replace(/^fx_[a-z0-9]+_/,"")).slice(0,3).join(",")})`:null].filter(Boolean).join(" · ");
  const notMeleeWhy= shotBone?"총구 본(Point_Attack1_Shot)": rN?`원거리 이펙트 ${rN}개(${fx.Ult.filter(x=>RANGED.test(x)).map(x=>x.replace(/^fx_[a-z0-9]+_/,"")).slice(0,3).join(",")})`+(mN?` > 근접 ${mN}`:""):"근접 근거 없음(앞줄 아님 · 근접 이펙트 없음)";
  R.melee=melee;
  const tr={}; for(const nm of allNames) tr[nm]=track(d,nm,skin,30);
  const n0=ref.n;
  R.plans=[];
  R.ways.forEach((w,wi)=>{
    const P={way:wi,names:w};
    const total=w.reduce((a,n)=>a+ms(dur(d,n)),0); P.motionMs=total;
    let off=0; const F=[];
    for(const nm of w){ for(const f of tr[nm].out) F.push({...f,T:off+ms(f.t),p:nm}); off+=ms(dur(d,nm)); }
    let fwd=0,back=0,up=0,jump=0,prev=null,hs=null; const hidden=[];
    for(const f of F){ fwd=Math.max(fwd,-f.bx/H); back=Math.max(back,f.bx/H); up=Math.max(up,f.by/H);
      if(prev&&prev.p===f.p) jump=Math.max(jump,Math.abs(f.bx-prev.bx)/H);
      const hid=f.n<Math.max(3,n0*0.2); if(hid&&hs==null) hs=f.T; if(!hid&&hs!=null){ hidden.push([hs,f.T]); hs=null; } prev=f; }
    if(hs!=null) hidden.push([hs,F[F.length-1].T]);
    P.pose={fwdH:+fwd.toFixed(2),backH:+back.toFixed(2),upH:+up.toFixed(2),jumpH:+jump.toFixed(2),endH:+(-F[F.length-1].bx/H).toFixed(2),hidden};
    let o2=0; const evs=[]; for(const nm of w){ for(const e of evsOf(d,nm)) evs.push({...e,T:o2+ms(e.t),p:nm}); o2+=ms(dur(d,nm)); }
    const goEv=evs.find(e=>/(^|,)1000002(,|$)/.test(e.s)), backEv=[...evs].reverse().find(e=>/(^|,)1000008(,|$)/.test(e.s));
    const go13=evs.find(e=>/(^|,)1000013(,|$)/.test(e.s)), back14=[...evs].reverse().find(e=>/(^|,)1000014(,|$)/.test(e.s)&&go13&&e.T>go13.T);
    const dc=DASH[key];
    let move="none", why=[], go=null, hitAt=null, ret=null, backMs=0, land=null;
    const S0=strikeOf(d,w);
    const longHidden=hidden.filter(([a,b])=>a>0&&a<total-40&&b-a>=200);
    const selfField=+(fwd*H*0.003).toFixed(2);
    const approach=+Math.max(0,GAP[h.row]-REACH-selfField).toFixed(2);
    P.selfTravel=selfField; P.approach=approach;
    if(dc){
      move=dc.move; why.push("웹판 DASH 표(손으로 맞춘 것)");
      go=whenIn(d,w,dc.go); hitAt=whenIn(d,w,dc.hit); land=dc.land?whenIn(d,w,dc.land):hitAt;
      ret=dc.home?whenIn(d,w,dc.home[0]):total; backMs=dc.back; if(dc.home) P.homeMs=whenIn(d,w,dc.home[1]);
    } else if(goEv){
      const vanish=hidden.find(([a,b])=>a>=goEv.T-150&&a<=goEv.T+400&&b-a>=100);
      const airborne=F.some(f=>f.T>=goEv.T-150&&f.T<=goEv.T+500&&f.by/H>0.6);
      move= vanish?"teleport":airborne?"leap":"dash";
      why.push(`원작 표시 1000002(가기) ${goEv.p}@${ms(goEv.t)}`+(backEv?` · 1000008(돌아오기) ${backEv.p}@${ms(backEv.t)}`:""));
      if(vanish) why.push(`몸이 ${vanish[0]}~${vanish[1]}ms 사라짐`); else if(airborne) why.push("뛰어오름 ≥0.6키");
      go=goEv.T; land=vanish?vanish[1]:airborne?(F.find(f=>f.T>goEv.T+100&&f.by/H<0.2)||{T:goEv.T+300}).T:Math.min(S0.at,goEv.T+250);
      hitAt=Math.max(land,S0.at); ret=backEv?backEv.T:total; backMs=backEv?0:300;
      if(hitAt>ret){ hitAt=land; why.push("돌아오기 표시가 타격보다 일러 닿는 때를 타격으로"); }
      if(move==="dash"&&land-go<150){ move="teleport"; why.push("옮길 틈 없음 → 순간이동"); }
    } else if(go13 && melee){
      move="dash"; why.push(`원작 표시 1000013 ${go13.p}@${ms(go13.t)}(가기로 봄)`+(back14?` · 1000014 @${back14.T}(돌아오기로 봄)`:""));
      go=go13.T; hitAt=S0.at; land=Math.min(hitAt,go+250); ret=back14?back14.T:total; backMs=back14?0:300;
    } else if(melee && approach>0.8){
      const air=F.find(f=>f.by/H>1.5);
      if(air){ move="leap"; why.push(meleeWhy+` · 화면 위로 ${up.toFixed(1)}키 뛰어오름`); go=air.T; const dn=F.find(f=>f.T>air.T&&f.by/H<0.3); land=dn?dn.T:Math.max(air.T+200,S0.at); hitAt=Math.max(land,S0.at); ret=total; backMs=300; }
      else if(longHidden.length){ move="teleport"; why.push(meleeWhy+" · 몸이 사라지는 창 "+JSON.stringify(longHidden)); go=longHidden[0][0]; land=longHidden[0][1]; hitAt=Math.max(land,S0.at); ret=longHidden.length>1?longHidden[longHidden.length-1][0]:total; backMs=longHidden.length>1?0:300; }
      else { move="dash"; why.push(meleeWhy); hitAt=S0.at>=250?S0.at:(evs.filter(e=>(e.name==="SFX"||(e.name==="Event"&&e.s.split(",").some(x=>+x>=1000100)))&&e.T>=250).map(e=>e.T)[0] ?? Math.round(total*0.45)); if(S0.at<250) why.push(`때리는 순간 ${S0.at}ms 는 시작 표시라 ${hitAt}ms 로`); go=Math.max(0,hitAt-350); land=Math.max(go+120,hitAt-60); ret=total; backMs=300; }
    } else why.push(melee?`${meleeWhy} — 그러나 몸짓이 스스로 ${selfField} 나가 닿음(남은 거리 ${approach})`:notMeleeWhy);
    P.move=move; P.why=why.join(" · ");
    if(move!=="none") Object.assign(P,{goMs:go,landMs:land,hitMs:hitAt,returnMs:ret,backMs});
    P.strike=dc?strikeOf(d,w,hitAt):S0;
    P.totalMs=total+(move!=="none"?backMs:0);
    // 유니티 지금(같은 첫 조각의 유니티 갈래) — UltMover 그대로: 앞줄 또는 평타 근접(총구 본 없음) · Travel < 2.5
    const uw=R.unityWays.find(x=>x[0]===w[0])||R.unityWays[0];
    const uTravel=Math.max(0,...[...new Set(uw)].map(nm=>Math.max(0,...(tr[nm]||track(d,nm,skin,30)).out.map(f=>-f.bx))))*0.003;
    const uMelee=front||(atkRanged===false&&!shotBone);
    P.unity={names:uw,motionMs:uw.reduce((a,n)=>a+ms(dur(d,n)),0),travel:+uTravel.toFixed(2),moves:uMelee&&uTravel<2.5,
      why:(front?"앞줄":atkRanged===false&&!shotBone?"평타 이펙트 근접":atkRanged===true||shotBone?"평타 원거리":"평타 이펙트 모름")+` · 몸짓 이동 ${uTravel.toFixed(2)}`+(uMelee&&uTravel>=2.5?" (스스로 간다고 보고 안 달림)":"")};
    R.plans.push(P);
  });
  // ── 소리 ──
  const sd=AS+"sfx/hero/"+art, vd=AS+"voice/"+art;
  const sf=fs.existsSync(sd)?fs.readdirSync(sd).filter(f=>f.endsWith(".ogg")):[];
  let ultF=sf.filter(f=>f.includes("_ultimate")); const sfxFallback=!ultF.length;
  if(!ultF.length) ultF=sf.filter(f=>f.includes("_spskill")||f.includes("_skill"));
  const len={}; for(const f of ultF) len[f]=ms(oggDur(sd+"/"+f));
  const vf=fs.existsSync(vd)?fs.readdirSync(vd).filter(f=>f.endsWith(".ogg")):[];
  let vpick=vf.filter(f=>f.startsWith("ultimate")); const voiceFallback=!vpick.length; if(!vpick.length) vpick=vf.filter(f=>f.startsWith("shout"));
  const vlen={}; for(const f of vpick) vlen[f]=ms(oggDur(vd+"/"+f));
  const full=vpick.filter(f=>!/-\d+\.ogg$/.test(f)), frag=vpick.filter(f=>/-\d+\.ogg$/.test(f));
  R.sound={sfxDir:"assets/sfx/hero/"+art,sfx:ultF.map(f=>({file:f,ms:len[f]})),sfxFallback,
    voiceDir:"assets/voice/"+art,voice:vpick.map(f=>({file:f,ms:vlen[f]})),voiceFallback,voiceFull:full.length,voiceFrag:frag.length};
  // 유니티 이름(art_ 로 바로잡은) 꼬리로 칸을 세운다
  const sl=slotsIn(ultF.map(f=>({f,t:f.slice(f.indexOf("_")+1)})));
  R.plans.forEach(P=>{
    let off=0; const sn=[], vo=[];
    for(const nm of P.names){ for(const e of evsOf(d,nm)){ if(e.name==="SFX"&&+e.s>0) sn.push({n:+e.s,T:off+ms(e.t)}); if(e.name==="Voice") vo.push({n:e.s,T:off+ms(e.t)}); } off+=ms(dur(d,nm)); }
    const m=slotMap(sl,sn.map(x=>x.n)); const plays=[];
    if(m){ for(const e of sn){ const f=m[e.n]; if(f==="hit") plays.push({slot:e.n,at:e.T,file:null,note:"맞는 소리 칸 — 타격 때 Impact 가 낸다"}); else if(f) plays.push({slot:e.n,at:e.T,file:f,ms:len[f],end:e.T+len[f]}); } }
    else {
      const cast=ultF.filter(f=>!f.includes("hit")).sort()[0]; if(cast) plays.push({slot:0,at:0,file:cast,ms:len[cast],end:len[cast],note:"칸이 안 맞아 시전 소리 하나(0ms)"});
      const hitf=ultF.filter(f=>f.includes("hit")).sort()[0]; if(hitf) plays.push({slot:-1,at:P.strike.at,file:hitf,ms:len[hitf],end:P.strike.at+len[hitf],note:"맞는 소리(타격 때)"});
    }
    const sEnd=Math.max(0,...plays.map(p=>p.end||0));
    const vl=vpick.map(f=>vlen[f]); const vMax=vl.length?Math.max(...vl):0, vMin=vl.length?Math.min(...vl):0;
    P.sound={mode:m?"events":"fallback",slotFiles:sl.list,hitFiles:sl.hit,slotNs:[...new Set(sn.map(x=>x.n))].sort((a,b)=>a-b),plays,
      sfxEndMs:sEnd||null,sfxDiffMs:sEnd?sEnd-P.motionMs:null,
      voiceEvents:vo,voiceLenMs:[vMin,vMax],voiceUnityStartMs:-CUTIN_LEAD,voiceUnityEndMs:vMax?vMax-CUTIN_LEAD:null,voiceUnityDiffMs:vMax?vMax-CUTIN_LEAD-P.motionMs:null,
      voiceAtEventEndMs:vMax&&vo.length?vo[0].T+vMax:null, voiceAtEventDiffMs:vMax&&vo.length?vo[0].T+vMax-P.motionMs:null};
  });
  if(hasUlt){ const sI=attachAt(d,has(d,"Idle")?"Idle":"Idle_1",0); const last=R.ways[0][R.ways[0].length-1]; R.ultEndLookDiff=diffN(attachAt(d,last,dur(d,last)),sI); }
  // ── 변신 ── Idle_<꼬리> 가 있는 꼬리(숫자 아닌) = 변신 모습 동작 묶음
  const tails=new Set(); for(const a of d.animations){ const m=a.name.match(/^Idle_(.+)$/); if(m&&!/^\d+$/.test(m[1])&&!/EasterEgg/i.test(m[1])) tails.add(m[1]); }
  if(tails.size){
    const t=[...tails][0]; const A=n=>has(d,n)?find(d,n).name:null;
    const idleN=A("Idle")||A("Idle_1"), idleF=A("Idle_"+t);
    const sIdle=attachAt(d,idleN,0), sForm=attachAt(d,idleF,0);
    const u=hasUlt?"Ultimate1_1":null; const uEnd=u?attachAt(d,u,dur(d,u)):null, uMid=u?attachAt(d,u,dur(d,u)*0.5):null;
    const uf=A("Ultimate1_1_"+t);
    R.form={tail:t,skin:"Normal(스킨은 그대로 — 그림 바꿈은 동작이 슬롯 attachment 로 한다)",
      idle:idleF,attack:A("Attack1_1_"+t),attack2:A("Attack2_1_"+t),skill:A("Skill1_1_"+t),hit:A("Groggy_"+t)||A("Hit_"+t),move:A("Move_"+t),die:A("Die_"+t),victory:A("Victory_"+t),spawn:A("Spawn_"+t),
      enter:u, ultInForm:uf, exit:null,
      look:{idleVsForm:diffN(sIdle,sForm), ultEndVsIdle:uEnd?diffN(uEnd,sIdle):null, ultEndVsForm:uEnd?diffN(uEnd,sForm):null, ultMidVsForm:uMid?diffN(uMid,sForm):null,
        formOnly:[...sForm].filter(x=>!sIdle.has(x)).slice(0,12), baseOnly:[...sIdle].filter(x=>!sForm.has(x)).slice(0,12)}};
  }
  out.heroes[key]=R; cnt++;
}
fs.writeFileSync(process.argv[2]||"ult.json",JSON.stringify(out,null,1));
console.log("done",cnt);
