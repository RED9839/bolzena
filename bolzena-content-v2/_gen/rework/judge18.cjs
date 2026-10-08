const fs=require("fs");const md=fs.readFileSync(process.argv[2],"utf8");
const EL=new Set("아야 시온 더 다크불릿 에피카 비비 클로에 이드 우이 우이(기억) 리뉴아 실비아 비비(신성) 에르핀(왕도) 란 디아나(왕년) 벨라 죠안 우로스 티그(영웅) 네르(빡침) 요미".split(" ").concat(["시온 더 다크불릿"]));
const TRIAL=new Set("리코타 티그 디아나(왕년) 죠안 캬롯 마카샤 벨벳 샤샤 이프리트 니콜 쵸피 레비(졸업) 쥬비 모모 힐데 큐이 코미".split(" "));
const rows=[...md.matchAll(/^\| ([^|]+) \| (딜러|서포터|탱커) \| ([^|]+) \| (\d+) \| ([\d.]+)% \| ±([\d.]+) \|$/gm)].map(m=>({n:m[1].trim(),role:m[2],race:m[3].trim(),N:+m[4],w:+m[5],e:+m[6]}));
const avg=a=>a.reduce((x,y)=>x+y,0)/a.length;
const gen=rows.filter(r=>!EL.has(r.n));const ra={};for(const role of ["딜러","서포터","탱커"])ra[role]=avg(gen.filter(r=>r.role===role).map(r=>r.w));
const ga=avg(gen.map(r=>r.w)),ea=avg(rows.filter(r=>EL.has(r.n)).map(r=>r.w));
console.log(`사도 ${rows.length} · 일반 ${gen.length} 평균 ${ga.toFixed(1)} · 엘다인 평균 ${ea.toFixed(1)} · 일반 역할 평균 딜러 ${ra.딜러.toFixed(1)} 서포터 ${ra.서포터.toFixed(1)} 탱커 ${ra.탱커.toFixed(1)}`);
const out=[];for(const r of rows){let d,ok;if(EL.has(r.n)){ok=r.w>=ga-0&&r.w<=ga+8;d=`엘다인 ${ga.toFixed(1)}~${(ga+8).toFixed(1)}`;}else{ok=Math.abs(r.w-ra[r.role])<=5;d=`${r.role} ${ra[r.role].toFixed(1)}±5`;}
if(!ok)out.push(`${r.n}${TRIAL.has(r.n)?"(시범)":""} ${r.w}% ±${r.e} [${d}] 차 ${(r.w-(EL.has(r.n)?ga:ra[r.role])).toFixed(1)}`);}
console.log("기준 밖",out.length);console.log(out.join("\n"));
if(process.argv[3])fs.writeFileSync(process.argv[3],out.map(x=>x.split(" ")[0].replace("(시범)","")).join("\n"));
