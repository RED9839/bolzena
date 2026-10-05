// ult.json(자세한 것) → bolzena-fx/Runtime/Motion/ult_motion.json(쓰는 것) + 문제 표(md 조각)
import fs from "node:fs";
const src=JSON.parse(fs.readFileSync("ult.json","utf8")).heroes;
const OUT="C:/projects/bolzena-fx/Runtime/Motion/ult_motion.json";
const FORM_MAIN=["죠안","네르_빡침","디아나_왕년"];
const heroes={}, problems=[], formCands=[];
const kindCode={go:"go",back:"back","go?":"go?","back?":"back?",start:"start",fx:"fx",mark:"mark",sound:"sfx",voice:"voice",next:"next"};
for(const [key,R] of Object.entries(src)){
  if(R.error){ problems.push({key,ko:R.ko,kinds:["읽기 실패"],detail:R.error}); continue; }
  const pieces={};
  for(const [n,p] of Object.entries(R.pieces)) pieces[n]={ms:p.ms,next:p.next,fresh:p.fresh,ev:p.events.map(e=>[e.n,e.t,e.s,kindCode[e.k]||e.k])};
  const ways=R.plans.map(P=>{
    const mv={type:P.move,why:P.why,selfTravel:P.selfTravel,approach:P.approach};
    if(P.move!=="none"){ Object.assign(mv,{goMs:P.goMs,landMs:P.landMs,hitMs:P.hitMs,returnMs:Math.max(P.returnMs,P.landMs),backMs:P.backMs}); if(P.homeMs!=null) mv.homeMs=P.homeMs; }
    const s=P.sound;
    return {names:P.names,motionMs:P.motionMs,totalMs:P.totalMs,strike:P.strike,move:mv,pose:P.pose,
      sound:{mode:s.mode,slotNs:s.slotNs,plays:s.plays.map(x=>({at:x.at,file:x.file,ms:x.ms??null,note:x.note})),sfxEndMs:s.sfxEndMs,sfxDiffMs:s.sfxDiffMs,
        voice:{events:s.voiceEvents.map(v=>[v.n,v.T]),lenMs:s.voiceLenMs,unityStartMs:s.voiceUnityStartMs,unityEndMs:s.voiceUnityEndMs,unityDiffMs:s.voiceUnityDiffMs,atEventEndMs:s.voiceAtEventEndMs,atEventDiffMs:s.voiceAtEventDiffMs}},
      unity:{names:P.unity.names,motionMs:P.unity.motionMs,moves:P.unity.moves,travel:P.unity.travel,why:P.unity.why}};
  });
  const H={ko:R.ko,art:R.art,row:R.row,role:R.role,ult:R.ult,bodyH:R.bodyH,fallback:R.fallback,melee:R.melee,
    ways,unityWays:R.unityWays,missing:R.missing,wayNotes:R.wayNotes,orphan:R.orphan,related:R.related,pieces,
    sfx:{dir:R.sound.sfxDir,files:R.sound.sfx,fallbackSkill:R.sound.sfxFallback},
    voice:{dir:R.sound.voiceDir,files:R.sound.voice,fallbackShout:R.sound.voiceFallback,lines:R.sound.voiceFull,takes:R.sound.voiceFrag,pick:"유니티 Sfx.Voice — 이 중 무작위 하나(ultimate*, 없으면 shout*)"},
    fx:R.fx};
  // 변신
  if(R.form){
    const f=R.form, entersByUlt=f.look.ultEndVsForm!=null&&f.look.ultEndVsForm<=8;
    const form={skin:"Normal",idle:f.idle,attack:f.attack,attack2:f.attack2,skill:f.skill,hit:f.hit,move:f.move,die:f.die,victory:f.victory,spawn:f.spawn,
      ult:f.ultInForm,enter:entersByUlt?f.enter:null,exit:null,tail:f.tail,
      note:(entersByUlt?`고학년(${f.enter}) 끝 자세가 변신 모습(${f.idle} 첫 자세와 붙은 그림 ${f.look.ultEndVsForm}곳만 다름) — 고학년이 곧 들어가는 애니`:"고학년 끝 자세는 본모습 — 고학년으로 들어가지 않는다")+
        ". 스킨은 따로 없고(모두 Normal) 변신 동작들이 슬롯 attachment 를 바꿔 모습을 그린다 — 그러니 변신 중엔 언제나 _"+f.tail+" 동작만 튼다(본 동작을 틀면 본모습으로 돌아온다). 원작에 나오는 애니(되돌아가기)는 없다 — 끝낼 땐 본 Idle 로 바로 바꾼다(섞기 0.2초)",
      look:f.look};
    if(FORM_MAIN.includes(key)) H.form=form;
    else formCands.push({key,ko:R.ko,tail:f.tail,entersByUlt,idle:f.idle,note:form.note.split(".")[0]});
  }
  H.ultEndLookDiff=R.ultEndLookDiff??null;
  // 문제
  const kinds=[], det=[];
  const mm=R.missing.filter(m=>m.kind!=="섞임(다른 갈래 · 되돌이 조각까지 이어 틂)"||true);
  if(mm.length){ kinds.push("조각"); det.push(mm.map(m=>`${m.kind}: ${[...new Set(m.pieces)].join(", ")}`).join(" / ")); }
  const W0=ways[0];
  if(ways.some(w=>w.move.type!=="none"&&!w.unity.moves)){ kinds.push("이동(제자리)"); det.push(`${W0.move.type} — ${W0.move.why}; 유니티: ${W0.unity.why}`); }
  else if(ways.some(w=>w.move.type!=="none"&&w.move.type!=="dash"&&w.unity.moves)){ kinds.push("이동 방식"); det.push(`${W0.move.type}(가기 ${W0.move.goMs} · 닿기 ${W0.move.landMs}) 인데 유니티는 시작 0.16초에 달려감`); }
  if(ways.every(w=>w.move.type==="none")&&W0.unity.moves){ kinds.push("괜히 달림"); det.push(`원작 근거 없음(${W0.move.why}); 유니티: ${W0.unity.why}`); }
  const sOver=ways.filter(w=>w.sound.sfxDiffMs>300), vOver=ways.filter(w=>w.sound.voice.unityDiffMs>300);
  if(sOver.length){ kinds.push("소리>몸짓"); det.push(`효과음이 몸짓보다 ${Math.max(...sOver.map(w=>w.sound.sfxDiffMs))}ms 길다(${sOver[0].sound.mode})`); }
  if(vOver.length){ kinds.push("목소리>몸짓"); det.push(`목소리(컷인에서 시작)가 몸짓 끝보다 ${Math.max(...vOver.map(w=>w.sound.voice.unityDiffMs))}ms 늦게 끝남`); }
  H.problems=kinds;
  if(kinds.length) problems.push({key,ko:R.ko,row:R.row,kinds,detail:det.join(" · "),motionMs:W0.motionMs,unityMotionMs:W0.unity.motionMs,totalMs:W0.totalMs});
  heroes[key]=H;
}
const fallbackSnd=Object.entries(heroes).filter(([k,h])=>h.ways[0].sound.mode==="fallback").map(([k])=>k);
const out={_meta:{
  made:"2026-10-05", tool:"유니티 밖(node · spine-core 4.1.56) — 원작 assets/spine/ingame · sfx/hero · voice 를 읽기만. 소리는 ogg 헤더로 길이만 쟀다(재생 안 함)",
  units:"ms · 원작 1배속(유니티 고학년은 1.05배속 — 에드 1.35 — 이니 몸짓 · 효과음 시각은 ÷1.05, 목소리는 배속 없음)",
  rules:"Docs/고학년모션표.md",
  unityLayout:{heroFeetX:{front:-1.55,mid:-3.2,back:-4.85},enemyX:3.2,reach:1.9,fieldPerSkel:0.003,cutinLeadMs:1510},
  moveTypes:{none:"제자리(원거리 · 몸짓이 스스로 닿음)",dash:"달려가 적 앞에서 때리고 돌아옴",teleport:"몸이 사라진 사이 적 앞으로 옮기고, 돌아오기 표시(또는 끝)에 제자리로",leap:"뛰어올라 화면 밖/공중에 있는 사이 옮김"},
  eventKinds:{start:"1000003 스킬 · 1000004 고학년 시작 표시",go:"1000002 원작 '가기'(대상 앞으로 옮김)",back:"1000008 원작 '돌아오기'","go?":"1000013(가기로 봄)","back?":"1000014(돌아오기로 봄)",fx:"이 사도 이펙트 Event(≥1000100)",sfx:"SFX n — 효과음 칸 n",voice:"Voice n — 대사 조각 n",next:"NextAni — 원작이 다음에 트는 조각"},
  counts:{heroes:Object.keys(heroes).length,problems:problems.length,fallbackSound:fallbackSnd.length}},
  problems, formCandidates:formCands, fallbackSound:fallbackSnd, heroes};
fs.writeFileSync(OUT,JSON.stringify(out,null,1));
console.log("wrote",OUT,(fs.statSync(OUT).size/1024|0)+"KB","problems",problems.length);
const by={}; for(const p of problems) for(const k of p.kinds) by[k]=(by[k]||0)+1; console.log(by);
fs.writeFileSync("problems.json",JSON.stringify(problems,null,1));
console.log("form cands",JSON.stringify(formCands));
console.log("fallbackSnd",fallbackSnd.length);
