if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 1~2성 기본 카드도 한 줄 수치뿐(2026-10-11 사용자 — 카제나 기본 카드에는 덧효과가 없다). 수치는 그대로, 덧효과 · 태그만 뺀다.
// 수치가 없던 기본 카드(약화 · 드로우 같은)는 카제나 기본처럼 실드 100%(회복이 기본인 서포터는 회복 100%).
// node _gen/deep/basics12.mjs <데이터 폴더>   (몇 번 돌려도 같다)
import fs from 'fs';
const dir = process.argv[2]; if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
let n = 0;
for (const r of fs.readdirSync(`${dir}/heroes`)) for (const f of fs.readdirSync(`${dir}/heroes/${r}`)) {
  if (!f.endsWith('.json')) continue;
  const p = `${dir}/heroes/${r}/${f}`; const s0 = fs.readFileSync(p, 'utf8'); const j = JSON.parse(s0); const h = j.heroes[0];
  if (h.star >= 3 || h.id === '마리') continue;
  for (const c of j.cards.filter(c => c.hero === h.id && !c.unique && !c.token && /_s\d+$/.test(c.id))) {
    const main = c.fx.find(q => ['dmg', 'shield', 'heal'].includes(q.k) && typeof q.ratio === 'number');
    if (main) c.fx = [main];
    else { c.type = '스킬'; c.name = '방어 자세'; c.fx = [h.role === '서포터' && c.fx.some(q => q.k === 'heal') ? { k: 'heal', ratio: 1.0 } : { k: 'shield', ratio: 1.0 }]; }
    delete c.tags;
  }
  const s1 = JSON.stringify(j, null, 2) + '\n'; if (s1 !== s0) { fs.writeFileSync(p + '.tmp', s1); fs.renameSync(p + '.tmp', p); n++; }
}
console.log('1~2성 기본 카드 한 줄로', n, '파일');
