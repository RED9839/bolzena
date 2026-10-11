// 용족 동굴 1층 보스 클론(루드) 다시 짓기 — node _gen/deep/dragonfix.mjs [대상 villages 폴더]
// 2026-10-10: 적 리뉴얼 담당 보고 — 이 클론이 판의 20% 를 끝냈다(다른 마을 1층 보스 5% 안팎). 묶음 3 의 루드(같은 동작 되풀이 · 렙)에 맞춰
// 수 이름 · 쌓이는 수치를 새로 하고, 세기를 다른 1층 보스 손 클론(마리 · 캬롯) 언저리로 내린다. dragon.json 의 다른 적은 건드리지 않는다.
import fs from 'fs';
const DST = process.argv[2] || 'C:/projects/bolzena-content-v2/world/villages';
const path = `${DST}/dragon.json`;
const d = JSON.parse(fs.readFileSync(path, 'utf8'));
const i = d.enemies.findIndex(e => e.id === 'clone_rude');
if (i < 0) throw new Error('clone_rude 없음');
const o = d.enemies[i];
d.enemies[i] = {
  id: 'clone_rude', name: o.name, hp: 3400, row: o.row, nature: o.nature, clone: o.clone, tough: 9, boss: true, pick: 'shuffle',
  blurb: '1층 보스 · 루드(클론) — 무엇이든 운동으로 바꾸는 용족 서열 2위. 같은 동작을 되풀이할 때마다 「렙」 이 쌓여(1개당 받는 피해 −5%) 넷이면 다음 차례 「임팩트 프레스」 로 전체를 칩니다. 맞으면 단백질 보충으로 실드(턴에 두 번). 격파하면 세트가 끊깁니다.',
  intents: [
    { t: 'attack', v: 160, say: '맨손 스쿼트 킥', w: 3 },
    { t: 'block', v: 130, say: '코어 버티기', w: 2 },
    { t: 'multi', v: 52, n: 3, say: '한 세트 더!', w: 3 },
    { t: 'count', id: '렙', v: 1, say: '미숫가루 프로틴', w: 2 },
  ],
  phase: { at: 0.5, say: '「마지막 한 번 더!」', intents: [
    { t: 'attack', v: 190, say: '실피르보다 세게', w: 3 },
    { t: 'multi', v: 56, n: 3, say: '한 세트 더!', w: 3 },
    { t: 'block', v: 130, say: '다야 님을 지켜라', w: 2 },
    { t: 'count', id: '렙', v: 1, say: '미숫가루 프로틴', w: 2 },
  ] },
  counters: [{ name: '렙', desc: '같은 동작을 되풀이한 횟수', onTurnStart: 1, taken: -0.05, max: 4, at: 4, act: { t: 'attackAll', v: 140, say: '임팩트 프레스', id: '약화', n: 1 }, mode: 'next' }],
  passives: [
    { name: '단백질 보충', on: 'hurt', do: { t: 'block', v: 35, say: '단백질 보충' }, limit: 2 },
    { name: '끊긴 세트', on: 'broken', do: { t: 'count', id: '렙', v: -4, say: '끊긴 세트' } },
  ],
};
const tmp = `${path}.tmp${process.pid}`;
fs.writeFileSync(tmp, JSON.stringify(d, null, 2) + '\n', 'utf8'); fs.renameSync(tmp, path);
console.log('clone_rude 다시 지음 →', path);
