if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 10% 반올림 뒤 검사 주의 손질(2026-10-10) — 값을 정해 넣는다(몇 번 돌려도 같다). node _gen/deep/finalfix.mjs <데이터 폴더>
import fs from 'fs';
const dir = process.argv[2]; if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
const card = (j, id) => j.cards.find(c => c.id === id);
const FIX = {
  '엘프/로네': j => { const o = card(j, '로네_u4').oracles[4]; o.fx[0].ratio = 0.9; o.fx[2].ratio = 0.5; },          // 소멸 신탁은 기본의 2배 넘게
  '엘프/마에스트로2호': j => { const o = card(j, '마에스트로2호_u1').oracles[4]; o.fx[0].ratio = 2.3; o.fx[2].ratio = 0.7; },
  '요정/에슈르_마도': j => { const o = card(j, '에슈르_마도_u4').oracles[2]; if (o.fx[0].k === 'dmg') o.fx = o.fx.slice(1); o.fx.find(f => f.k === 'dmg').ratio = 0.6; },   // 글 45자 — 앞 피해를 빼고 1당 피해를 올림
  '요정/캬롯': j => { const o = card(j, '캬롯_u3').oracles[2]; o.fx = o.fx.filter(f => f.k !== 'shield'); },   // 글 45자
  '수인/디아나_왕년': j => { j.heroes[0].ult.fx.find(f => f.k === 'dmg').ratio = 1.4; },                               // 변신을 든 고학년이 너무 세지 않게
};
for (const [f, fn] of Object.entries(FIX)) {
  const p = `${dir}/heroes/${f}.json`; if (!fs.existsSync(p)) continue;
  const s0 = fs.readFileSync(p, 'utf8'); const j = JSON.parse(s0); fn(j); const s1 = JSON.stringify(j, null, 2) + '\n';
  if (s1 !== s0) { fs.writeFileSync(p + '.tmp', s1); fs.renameSync(p + '.tmp', p); console.log('손질', f); }
}
