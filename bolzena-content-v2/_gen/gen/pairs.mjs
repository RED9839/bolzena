if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
import fs from 'fs';
const dir = process.argv[2], filler = { 탱커: 'rico', 서포터: 'carrot', 딜러: 'sion' };
const H = fs.readdirSync(dir).map(f => JSON.parse(fs.readFileSync(dir + '/' + f, 'utf8')).heroes[0]);
const lines = [];
const third = roles => { for (const r of ['탱커', '서포터', '딜러']) if (!roles.includes(r)) return filler[r]; return filler['탱커']; };
for (const h of H) {
  const rest = ['탱커', '서포터', '딜러'].filter(r => r !== h.role);
  lines.push(`solo:${h.id} | ${h.id},${filler[rest[0]]},${filler[rest[1]]}`);
}
for (let i = 0; i < H.length; i++) for (let j = i + 1; j < H.length; j++) {
  const a = H[i], b = H[j];
  let t = third([a.role, b.role]);
  if (a.role === b.role) t = a.role === '탱커' ? 'carrot' : 'rico';
  lines.push(`pair:${a.id}:${b.id}:${a.nature === b.nature ? 'same' : 'diff'} | ${a.id},${b.id},${t}`);
}
fs.writeFileSync(process.argv[3], lines.join('\n'));
console.log(lines.length);
