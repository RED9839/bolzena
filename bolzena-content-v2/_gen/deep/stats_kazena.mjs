if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 사도 스탯 = 역할 × 등급 표(2026-10-10 사용자 결정 — 카제나식, role_stats.json). 같은 역할 · 성급이면 hp · atk · def · crit 가 같다.
// 사도별 세기는 고유 카드 수치로(tune.json → lib tune · retune.mjs). node _gen/deep/stats_kazena.mjs <데이터 폴더>   (몇 번 돌려도 같다)
import fs from 'fs';
const dir = process.argv[2]; if (!dir) { console.error('데이터 폴더를 꼭 준다(원본에 쓸 땐 C:/projects/bolzena-content-v2 를 직접) — 인자 없이 원본을 고치지 않게'); process.exit(1); }
const RS = JSON.parse(fs.readFileSync(new URL('./role_stats.json', import.meta.url), 'utf8'));
const sum = {};
for (const r of fs.readdirSync(`${dir}/heroes`)) for (const f of fs.readdirSync(`${dir}/heroes/${r}`)) {
  if (!f.endsWith('.json')) continue;
  const p = `${dir}/heroes/${r}/${f}`; const j = JSON.parse(fs.readFileSync(p, 'utf8')); const h = j.heroes[0];
  const nv = RS[h.role][String(Math.min(3, Math.max(1, h.star)))];
  const s = (sum[`${h.role} ${h.star}성`] ||= 0); sum[`${h.role} ${h.star}성`] = s + 1;
  if (h.hp !== nv.hp || h.atk !== nv.atk || h.def !== nv.def || h.crit !== nv.crit) { Object.assign(h, { hp: nv.hp, atk: nv.atk, def: nv.def, crit: nv.crit }); fs.writeFileSync(p + '.tmp', JSON.stringify(j, null, 2) + '\n'); fs.renameSync(p + '.tmp', p); }
}
console.log('역할 × 등급 스탯 —', Object.entries(sum).sort().map(([k, n]) => `${k} ${n}명`).join(' · '));
