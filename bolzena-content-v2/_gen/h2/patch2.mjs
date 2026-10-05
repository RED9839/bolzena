import { load, save, setCard, hero } from './lib.mjs';
const D=(ratio,target='oneEnemy',hits)=>({k:'dmg',ratio,target,...(hits?{hits}:{})});
const S=(ratio)=>({k:'shield',ratio}); const ST=(id,v,target)=>({k:'status',id,v,...(target?{target}:{})});
const K=(id,v,target)=>({k:'stack',id,v,...(target?{target}:{})}); const SP=(id,v)=>v==='all'?{k:'spend',id,all:true}:{k:'spend',id,v};
const IFS=(id,n)=>({k:'ifStack',id,...(n?{n}:{})}); const PER=(id)=>({k:'perStack',id}); const DRAW=(v)=>({k:'draw',v});
const TOUGH=(v,target)=>({k:'tough',v,...(target?{target}:{})}); const CS=(id,v,to,n)=>({k:'cardStatus',id,v,to,n});
const ed=(k,f)=>{const j=load(k);f(j,hero(j));save(k,j);};

// 티그 — wipe 빼기(이번 턴 티그 공격 카드 수로), 계수 올림
ed('티그',(j,h)=>{ delete h.keyword.wipe; h.keyword.desc='「장작 패기 반복」 — 이번 턴 티그가 공격 카드를 낼 때마다 쌓이는 손맛(턴이 끝나면 사라진다). 셋이 되면 AP 1 을 돌려받는다.';
  setCard(j,'티그_u1',{fx:[D(1.5,'allEnemies'),IFS('장작 패기',1),D(1.0,'allEnemies')]});
  setCard(j,'티그_u2',{fx:[D(0.6,'oneEnemy',2),IFS('장작 패기',1),D(0.6)]});
  setCard(j,'티그_u3',{fx:[D(2.2),IFS('장작 패기',1),D(1.1),TOUGH(0.5)]}); });
// 바나 — 호두까기 1코, 단조 둘에 사기
ed('바나',(j,h)=>{ h.keyword.rules[0].when.n=2; h.keyword.desc=h.keyword.desc.replace('셋이 차면','둘이 차면');
  setCard(j,'바나_u1',{cost:1,fx:[CS('탐구심',1,'hand',0),S(1.6),K('단조',1)]});
  setCard(j,'바나_u3',{fx:[S(1.2),CS('탐구심',1,'hand',2),DRAW(1)]});
  setCard(j,'바나_u2',{fx:[D(2.0),ST('취약',1),K('단조',1)]}); });
// 우로스 — 전투 시작 순환 1, 버린 더미 문턱 4, 계수
ed('우로스',(j,h)=>{ h.keyword.rules=[{name:'허물',when:{on:'fightStart'},fx:[K('순환',1)]}];
  setCard(j,'우로스_u1',{fx:[{k:'discard',v:2},DRAW(3),{k:'ifPile',from:'discard',n:4},{k:'ap',v:1}]});
  setCard(j,'우로스_u2',{fx:[D(1.2),{k:'ifPile',from:'discard',n:4},D(0.8)]});
  setCard(j,'우로스_u4',{fx:[D(1.2,'allEnemies'),PER('순환'),D(0.5,'allEnemies')]}); });
// 유미미 — 턴 끝마다 +1, 안 냈으면 +1 더
ed('유미미',(j,h)=>{ h.passives=[{name:'늘어지기',when:{on:'turnEnd'},fx:[K('나른함',1)]},{name:'늘어지기',when:{on:'turnEnd'},conds:[{c:'ownNone'}],fx:[K('나른함',1)]}];
  h.keyword.desc='「늘어지기」 — 턴 끝마다 하나, 유미미 카드를 하나도 안 낸 턴이면 하나 더 쌓이는 나른함. 1개당 자신의 피해가 오르고, 공격 카드 한 장에 모두 쏟는다.';
  setCard(j,'유미미_u4',{fx:[D(1.9),{k:'ifWounded',target:'oneEnemy'},D(1.9)]}); });
// 루포 — 계획이면 강인도 더, 계수
ed('루포',(j)=>{ setCard(j,'루포_u1',{fx:[D(0.75,'oneEnemy',3),TOUGH(0.5),IFS('계획'),TOUGH(1)]});
  setCard(j,'루포_u3',{fx:[D(1.1),TOUGH(0.5)]});
  setCard(j,'루포_u4',{fx:[D(2.2),{k:'ifFoe',id:'broken'},D(1.3)]}); });
// 쵸피 — 계수
ed('쵸피',(j)=>{ setCard(j,'쵸피_u1',{fx:[D(1.15),TOUGH(0.5)]}); setCard(j,'쵸피_u2',{fx:[D(2.2),{k:'ifBreak'},K('수련',1)]}); setCard(j,'쵸피_u4',{fx:[S(1.5),ST('반격',1),K('수련',1)]}); });
// 코미 — 너무 튐: 낮잠 실드 120 → 90, 사료 낮잠 3 → 2
ed('코미',(j,h)=>{ h.passives[0].fx[1].ratio=0.9; setCard(j,'코미_u4',{fx:[K('낮잠',2),ST('피해 감소',2),{k:'heal',ratio:1.0}]}); });
