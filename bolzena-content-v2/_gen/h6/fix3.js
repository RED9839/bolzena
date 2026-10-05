const fs=require('fs');
const R=(f,a,b)=>{let s=fs.readFileSync(f,'utf8');if(!s.includes(a)){console.log('MISS',f,a);return;}fs.writeFileSync(f,s.replace(a,b));};
R('h_sist.js',"    per: [{ stat: 'dealt', v: 0.05 }],\n  },\n  passives: [\n    { name: '장사 수완', when: { on: 'kill' }, fx: [stk(KW, 2)] },\n    { name: '다음 손님', when: { on: 'break' }, fx: [stk(KW, 2)] },\n  ],",
"    per: [{ stat: 'dealt', v: 0.05 }],\n    rules: [\n      { name: '장사 수완', when: { on: 'kill' }, fx: [stk(KW, 2)] },\n      { name: '다음 손님', when: { on: 'break' }, fx: [stk(KW, 2)] },\n    ],\n  },\n  passives: [\n    { name: '개업 자금', when: { on: 'fightStart' }, fx: [stk(KW, 2)] },\n  ],");
R('h_pira.js',"{ name: '도금', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 1 }, fx: gild(1) }","{ name: '도금', when: { on: 'play', type: '스킬' }, limit: { per: 'turn', n: 2 }, fx: gild(1) }");
R('h_rude.js',"fx: [sh(0.4)] }","fx: [sh(0.3)] }");
R('h_rude.js',"fx: [sh(0.8)] }","fx: [sh(0.6)] }");
R('h_rude.js',"fx: [sh(1.2), st('결의', 1)] }","fx: [sh(0.9), st('결의', 1)] }");
R('h_aragnia.js',"L.sh(0.5, { fixed: true })","L.sh(0.4, { fixed: true })");
