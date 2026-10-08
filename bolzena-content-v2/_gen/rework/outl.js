const fs=require("fs");const t=fs.readFileSync(process.argv[2],"utf8");
const E=["아야","시온 더 다크불릿","에피카","비비","클로에","이드","우이","우이(기억)","리뉴아","실비아","비비(신성)","에르핀(왕도)","란","디아나(왕년)","벨라","죠안","우로스","티그(영웅)","네르(빡침)","요미"];
const rows=t.split(/\n/).filter(l=>/^\| [^|]+ \| (탱커|딜러|서포터) /.test(l)).map(l=>l.split("|").map(s=>s.trim())).map(r=>({n:r[1],role:r[2],p:parseFloat(r[5]),e:E.includes(r[1])}));
const avg=a=>a.reduce((x,y)=>x+y,0)/a.length;const ra={};for(const r of ["탱커","딜러","서포터"])ra[r]=avg(rows.filter(x=>x.role===r).map(x=>x.p));
const norm=avg(rows.filter(x=>!x.e).map(x=>x.p)),eld=avg(rows.filter(x=>x.e).map(x=>x.p));
console.log("역할 평균",JSON.stringify(Object.fromEntries(Object.entries(ra).map(([k,v])=>[k,v.toFixed(1)]))),"일반",norm.toFixed(1),"엘다인",eld.toFixed(1),"상한",(norm+8).toFixed(1));
const out=rows.filter(r=>r.e?(r.p>norm+8||r.p<ra[r.role]-5):Math.abs(r.p-ra[r.role])>5);console.log("바깥",out.length);for(const r of out)console.log(`${r.n}(${r.role}${r.e?"·엘다인":""}) ${r.p} — 역할 평균 ${(r.p-ra[r.role]).toFixed(1)}${r.e&&r.p>norm+8?" · 엘다인 상한 넘음":""}`);
const sd=Math.sqrt(avg(rows.map(r=>(r.p-avg(rows.map(x=>x.p)))**2)));const s=rows.map(r=>r.p).sort((a,b)=>a-b);console.log("분포 최저",s[0],"p10",s[13],"중앙",s[67],"p90",s[121],"최고",s[134],"표준편차",sd.toFixed(1));
