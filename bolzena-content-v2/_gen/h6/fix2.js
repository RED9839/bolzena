const fs=require('fs');
const R=(f,a,b)=>{let s=fs.readFileSync(f,'utf8');if(!s.includes(a)){console.log('MISS',f,a);return;}fs.writeFileSync(f,s.replace(a,b));};
R('h_sist.js',"carrier: 'self', cap: 6,\n  },","carrier: 'self', cap: 6,\n    per: [{ stat: 'dealt', v: 0.05 }],\n  },");
R('h_sist.js',"desc: '장부에 적힌 금화. 적을 격파 · 처치하면 쌓이고, 「금화로만」 파는 카드의 비용이 된다'","desc: '장부에 적힌 금화 — 격파 · 처치로 쌓이고, 「금화로만」 파는 카드의 값이 된다'");
R('h_pira.js',"fx: [d(1.6), stk(KW, 1)] }","fx: [d(1.8), stk(KW, 1)] }");
R('h_pira.js',"fx: [sh(2.4), stk(KW, 1)] }","fx: [sh(2.7), stk(KW, 1)] }");
R('h_jade.js',"{ name: '독서광', when: { on: 'turnEnd' }, conds: [{ c: 'ownNone' }], fx: [stk(KW, 1)] }","{ name: '독서광', when: { on: 'turnEnd' }, fx: [stk(KW, 1)] }");
