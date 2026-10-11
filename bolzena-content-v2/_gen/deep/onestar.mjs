if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 1~2성 사도 손질(2026-10-10 — 정석 인연 파티) — 원본 heroes 파일에 바로(몇 번 돌려도 같다: 같은 이름 규칙을 바꾼다)
// node _gen/deep/onestar.mjs <데이터 폴더>
import fs from 'fs';
const dir = process.argv[2]; if (!dir) { console.error('데이터 폴더를 꼭 준다(원본에 쓸 땐 C:/projects/bolzena-content-v2 를 직접) — 인자 없이 원본을 고치지 않게'); process.exit(1); }
const PATCH = {
  // P10 쵸피(격파 소비 — 스승 둘): 「새로운 모험」(카드가 자라면 드로우)을 「스승의 가르침」 으로 — 동료가 적을 치면 수련, 격파되면 장작 한 대
  '수인/쵸피': h => ({ cards: j => { const c = j.cards.find(c => c.id === '쵸피_u2'); const i = c.fx.findIndex(f => f.k === 'perStack'); if (i >= 0) c.fx[i + 1].ratio = 0.5; const o4 = c.oracles[3].fx; o4[0].ratio = 3.9; o4[2].ratio = 0.6; },   // 신탁 4(처치 배움) — 기본보다 1.15배 넘게   // 쓰기는 카드로 — 결대로 쪼개기 수련 1개당 39 → 50%
    drop: ['새로운 모험', '넘치는 수련'], add: [{ name: '스승의 가르침', when: { on: 'play', who: 'other', type: '공격' }, fx: [{ k: 'stack', id: '수련', v: 1 }] }] }),
};
for (const [k, fn] of Object.entries(PATCH)) {
  const p = `${dir}/heroes/${k}.json`; const j = JSON.parse(fs.readFileSync(p, 'utf8')); const h = j.heroes[0]; const r = fn(h);
  h.passives = (h.passives || []).filter(x => !r.drop.includes(x.name) && !r.add.some(a => a.name === x.name)); h.passives.push(...r.add);
  if (r.cards) r.cards(j);
  fs.writeFileSync(p + '.tmp', JSON.stringify(j, null, 2) + '\n'); fs.renameSync(p + '.tmp', p); console.log('손질', k);
}
