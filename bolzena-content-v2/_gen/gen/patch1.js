const fs=require('fs');
function P(f,pairs){let s=fs.readFileSync(f,'utf8');for(const[a,b]of pairs){if(!s.includes(a))throw new Error(f+': '+a);s=s.split(a).join(b);}fs.writeFileSync(f,s);}
P('cold.js',[
 ["seq: ['공격', '스킬'], who: 'any' }, fx: [KS('접시', 1)] }","seq: ['공격', '스킬'], who: 'any' }, fx: [KS('접시', 1), RH(1)] }"],
 ["seq: ['스킬', '공격'], who: 'any' }, fx: [KS('접시', 1)] }","seq: ['스킬', '공격'], who: 'any' }, fx: [KS('접시', 1), RH(1)] }"],
 ["fx: [PERR, D(0.35, EA), KS('전류', 1, EA)] }","fx: [PERR, D(0.4, EA), KS('전류', 1, EA)] }"],
]);
P('lively.js',[
 ["{ name: '누가 허접이래!', when: { on: 'rhythm', n: 4 }, fx: [AP(1)] }","{ name: '누가 허접이래!', when: { on: 'play', type: '공격' }, conds: [{ c: 'status', id: '리듬', n: 2 }], fx: [D(0.6)] }"],
 ["{ name: '단백질 보충', when: { on: 'play', repeat: true }, fx: [KS('세트', 2)] }","{ name: '단백질 보충', when: { on: 'play' }, fx: [KS('세트', 1)] }"],
 ["{ name: '마법 유기물 탐지', when: { on: 'play', nth: 4, who: 'any' }, fx: [RH(1), DRAW(1)] }","{ name: '마법 유기물 탐지', when: { on: 'play', every: 2 }, fx: [RH(1)] }"],
 ["넷째 장에 소포가 떨어지고, 다섯째 장에 한 바퀴 더 돈다.","셋째 장에 소포가 떨어지고, 넷째 장에 한 바퀴 더 돈다."],
 ["{ name: '과속 배달', when: { on: 'play', nth: 4, who: 'any' }","{ name: '과속 배달', when: { on: 'play', nth: 3, who: 'any' }"],
 ["{ name: '한 바퀴 더', when: { on: 'play', nth: 5, who: 'any' }","{ name: '한 바퀴 더', when: { on: 'play', nth: 4, who: 'any' }"],
 ["fx: [SP('구독자', 3)] }]","fx: [SP('구독자', 2)] }]"],
 ["{ name: '떡상', when: { on: 'rhythm', n: 4 }, fx: [KS('구독자', 2)] }","{ name: '떡상', when: { on: 'rhythm', n: 3 }, fx: [KS('구독자', 2)] }"],
]);
P('gloomy.js',[
 ["fx: [SP('수련', 'all')] }]","fx: [SP('수련', 2)] }]"],
 ["{ name: '스승님 보세요', when: { on: 'rhythm', n: 3 }, fx: [KS('수련', 1)] }","{ name: '스승님 보세요', when: { on: 'play', type: '공격' }, conds: [{ c: 'status', id: '리듬', n: 2 }], fx: [KS('수련', 1)] }"],
 ["{ name: '천재 해커의 등장', when: { on: 'ult' }, fx: [D(0.6, ER, 3), ST('기절', 1, E1)] }","{ name: '핫키 연타', when: { on: 'rhythm', n: 3 }, fx: [AP(1)] }"],
 ["fx: [DD(1.0), TOUGH(2), RH(1)] }","fx: [DD(1.0), TOUGH(2), RH(2)] }"],
 ["{ name: '펜스 너머의 너', when: { on: 'play', type: '공격', who: 'any', every: 2 }, fx: [KS('열기', 1)] }","{ name: '펜스 너머의 너', when: { on: 'play', type: '공격', who: 'any', every: 2 }, fx: [KS('열기', 1), RH(1)] }"],
]);
P('calc.js',[
 ["{ name: '마도 각성', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [KS('마력 증폭', 1), MAKE(`${h}_bit`, 1)] }","{ name: '정점 계산', when: { on: 'play', who: 'any' }, conds: [{ c: 'spent', n: 4 }], limit: { per: 'turn', n: 1 }, fx: [KS('마력 증폭', 2), DRAW(1)] }"],
 ["'마력 입자', 0, '스킬', [NC(1)], { tags: ['소멸', '신속'], blurb: '마법을 쪼갠 알갱이. 다음 수의 값을 한 칸 깎아 셈을 맞춘다.' }","'마력 입자', 0, '스킬', [AP(1)], { tags: ['소멸', '신속'], blurb: '마법을 쪼갠 알갱이. 한 칸 더 — 넷째 AP 를 만든다.' }"],
 ["{ name: '짱박힘 1인자', when: { on: 'turnEnd' }, conds: [{ c: 'playedMax', n: 2 }], fx: [KS('땡땡이', 2)] }","{ name: '짱박힘 1인자', when: { on: 'turnEnd' }, conds: [{ c: 'playedMax', n: 2 }], fx: [KS('땡땡이', 2), ST('저장', 1)] }, { name: '딱 세 장까지', when: { on: 'play', nth: 3, who: 'any' }, fx: [KS('땡땡이', 1)] }"],
 ["{ name: '성과급부터', when: { on: 'kill', mine: true }, limit: { per: 'turn', n: 1 }, fx: [DRAW(1)] },",""],
 ["{ name: '저울 맞추기', when: { on: 'play' }","{ name: '저울 맞추기', when: { on: 'play', who: 'any' }"],
]);
