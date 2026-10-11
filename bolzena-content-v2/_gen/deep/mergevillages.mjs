// 클론만 원본 마을에 끼워 넣기 — node _gen/deep/mergevillages.mjs [대상 폴더(기본 원본 world/villages)]
// clones.mjs 는 백업 마을에서 통째로 짓는다. 그사이 적 리뉴얼 담당이 원본의 일반 적 · 마을 줄을 고치므로, 반영할 때는
// 지금 원본을 그대로 두고 클론(id 가 clone_ 로 시작) 항목만 바꾼다: 같은 id 는 덮고, 손 클론으로 바뀐 생성 클론(clone_g_*)은 같은 자리에서 갈아 끼운다.
import fs from 'fs';
const GEN = new URL('./villages/', import.meta.url);
const DST = process.argv[2] || 'C:/projects/bolzena-content-v2/world/villages';
const isClone = e => e.id.startsWith('clone_');
for (const f of fs.readdirSync(GEN).filter(x => x.endsWith('.json'))) {
  const gen = JSON.parse(fs.readFileSync(new URL(f, GEN), 'utf8'));
  const path = `${DST}/${f}`; const cur = JSON.parse(fs.readFileSync(path, 'utf8'));
  const g = new Map(gen.enemies.filter(isClone).map(e => [e.id, e]));
  const byHero = new Map(gen.enemies.filter(isClone).map(e => [e.clone, e]));
  const used = new Set(); let n = 0;
  cur.enemies = cur.enemies.map(e => {
    if (!isClone(e)) return e;
    const r = g.get(e.id) || (!g.has(e.id) && byHero.get(e.clone));
    if (!r) return e;
    used.add(r.id); if (JSON.stringify(r) !== JSON.stringify(e)) n++;
    return r;
  });
  for (const [id, e] of g) if (!used.has(id) && !cur.enemies.some(x => x.id === id)) { cur.enemies.push(e); n++; }
  const tmp = `${path}.tmp${process.pid}`;
  fs.writeFileSync(tmp, JSON.stringify(cur, null, 2) + '\n', 'utf8'); fs.renameSync(tmp, path);
  console.log(f, '클론 바뀜', n);
}
