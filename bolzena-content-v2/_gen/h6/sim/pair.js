const fs=require('fs'),p=require('path'),{execFile}=require('child_process');
const BZ='C:/projects/bolzena-core/Tools~/Dev/bz.cmd', DATA='C:/projects/bolzena-content-v2';
const ids=[];(function w(d){for(const f of fs.readdirSync(d)){const q=p.join(d,f);if(fs.statSync(q).isDirectory())w(q);else if(q.endsWith('.json')){const j=JSON.parse(fs.readFileSync(q));for(const h of j.heroes||[])ids.push(h.id);}}})(DATA+'/heroes');
const pairs=[['비비','실비아'],['다야','키디언'],['오팔','아라그니아'],['제이드','시스트'],['아라그니아','오팔'],['실비아','비비'],['네티','피라'],['리츠','루드'],['시스트','다야'],['다야_퓨어샤인','다야'],['아네트','리츠'],['피라','네티'],['루드','실피르'],['키디언','다야'],['실피르','루드']];
const N=30, jobs=[];
pairs.forEach(([a,b],pi)=>{for(let s=0;s<N;s++){let t;let k=(s*37+pi*11)%ids.length;do{t=ids[k];k=(k+1)%ids.length;}while(t===a||t===b);jobs.push({a,b,t,s});}});
const res={};let i=0,run=0;
function next(){if(i>=jobs.length){if(run===0)done();return;}const j=jobs[i++];run++;
 execFile('cmd',['/c',BZ,'run',`${j.a},${j.b},${j.t}`,String(j.s+1),'--data',DATA],{maxBuffer:1e7},(e,out)=>{const k=j.a+'+'+j.b;res[k]=res[k]||[0,0];res[k][1]++;if(/\n완주/.test(out))res[k][0]++;run--;next();});}
for(let c=0;c<8;c++)next();
function done(){for(const [k,[w,n]] of Object.entries(res))console.log(k, (100*w/n).toFixed(1)+'%', `(${w}/${n})`);}
