if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 고유 효과 이름 → 원작 어사이드 이름(2026-10-11 사용자 결정, _measure/어사이드_이름.json).
// 사도의 첫 고유 효과(keyword.name)가 표의 now 와 같고 aside 가 있으면, 그 사도 파일 · 마을(클론 키트)의 같은 문자열 값을 모두 바꾼다(fx id · 규칙 · 클론).
// 건너뜀: aside 없음 · aside 에 다른 사도 이름이 든 것 · 다른 고유 효과 · 엔진 상태 · 키워드 이름과 같은 것. node _gen/deep/rename_kw.mjs <데이터 폴더> [더 바꿀 마을 폴더 …]   (몇 번 돌려도 같다)
import fs from 'fs';
const [dir, ...extra] = process.argv.slice(2); if (!dir) { console.error('데이터 폴더를 꼭 준다'); process.exit(1); }
const T = JSON.parse(fs.readFileSync(new URL('../../_measure/어사이드_이름.json', import.meta.url), 'utf8')).heroes;
const files = []; for (const r of fs.readdirSync(`${dir}/heroes`)) for (const f of fs.readdirSync(`${dir}/heroes/${r}`)) if (f.endsWith('.json')) files.push(`${dir}/heroes/${r}/${f}`);
const H = files.map(p => ({ p, j: JSON.parse(fs.readFileSync(p, 'utf8')) }));
const base = id => id.split('_')[0];
const names = H.map(x => ({ id: x.j.heroes[0].id, name: x.j.heroes[0].name }));
// 엔진 상태 · 키워드 이름(Rules.cs STATUS_V · CardText TIPS 의 ["이름"])
const eng = new Set(); for (const p of ['C:/projects/bolzena-core/Runtime/Rules/Rules.cs', 'C:/projects/bolzena-core/Runtime/Text/CardText.cs']) for (const m of fs.readFileSync(p, 'utf8').matchAll(/\["([^"\]]{1,20})"\]/g)) eng.add(m[1]);
const kwNames = new Set(H.flatMap(x => [x.j.heroes[0].keyword, ...(x.j.heroes[0].keywords || [])].filter(Boolean).map(k => k.name)));
const map = {}, skip = [];
for (const x of H) {
  const h = x.j.heroes[0], t = T[h.id]; if (!t || !t.aside || !h.keyword) continue;
  const old = h.keyword.name; if (old === t.aside) continue;   // 이미 바뀜
  if (old !== t.now) { skip.push(`${h.id}: 지금 이름 「${old}」 ≠ 표 「${t.now}」`); continue; }
  const other = names.find(n => base(n.id) !== base(h.id) && n.name && t.aside.includes(n.name.replace(/\(.*\)/, '').trim()));
  if (other) { skip.push(`${h.id}: 「${t.aside}」 — 다른 사도 이름(${other.name}) 유지`); continue; }
  if (kwNames.has(t.aside) || eng.has(t.aside) || Object.values(map).includes(t.aside)) { skip.push(`${h.id}: 「${t.aside}」 — 이미 있는 이름과 겹쳐 유지`); continue; }
  map[old] = t.aside;
}
// 같은 옛 이름을 둘 이상이 쓰면(다른 사도의 같은 이름) 그 사도 파일에서만 바꾼다
const owner = {}; for (const x of H) { const k = x.j.heroes[0].keyword; if (k && map[k.name]) owner[x.p] = k.name; }
const rep = (o, from, to) => { if (Array.isArray(o)) return o.map(v => rep(v, from, to)); if (o && typeof o === 'object') { for (const k of Object.keys(o)) o[k] = rep(o[k], from, to); return o; } return o === from ? to : o; };
let nf = 0;
for (const x of H) { const old = owner[x.p]; if (!old) continue; rep(x.j, old, map[old]); fs.writeFileSync(x.p + '.tmp', JSON.stringify(x.j, null, 2) + '\n'); fs.renameSync(x.p + '.tmp', x.p); nf++; }
// 마을(클론 키트) — 그 이름을 쓰는 곳 모두
const vdirs = [`${dir}/world/villages`, ...extra];
for (const vd of vdirs) if (fs.existsSync(vd)) for (const f of fs.readdirSync(vd)) {
  if (!f.endsWith('.json')) continue; const p = `${vd}/${f}`; const s0 = fs.readFileSync(p, 'utf8'); let j = JSON.parse(s0);
  for (const [o, n] of Object.entries(map)) j = rep(j, o, n);
  const s1 = JSON.stringify(j, null, 2) + '\n'; if (s1 !== s0) { fs.writeFileSync(p + '.tmp', s1); fs.renameSync(p + '.tmp', p); }
}
fs.writeFileSync(new URL('./_rename_map.json', import.meta.url), JSON.stringify({ map, skip }, null, 1));
console.log(`고유 효과 이름 바꿈 ${Object.keys(map).length} · 사도 파일 ${nf} · 건너뜀 ${skip.length}`);
