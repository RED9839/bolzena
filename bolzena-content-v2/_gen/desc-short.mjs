// 사도 고유 효과(키워드) 설명 글 줄이기 — desc-short.tsv(사도키 · 키워드 이름 · 새 설명)를 사도 JSON 에 덮어쓴다.
// 종족별 생성기(h4-정령 · h6 · h7 · ghost …)를 다시 돌린 뒤에도 이것을 한 번 더 돌리면 짧은 글이 유지된다.
//   node _gen/desc-short.mjs            사도 JSON 에 적용
//   node _gen/desc-short.mjs --gen      생성기 소스의 옛 글도 같은 글로 바꿔 둔다(찾은 것만, 못 찾은 것은 출력)
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const GEN = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.dirname(GEN);
const rows = fs.readFileSync(path.join(GEN, 'desc-short.tsv'), 'utf8').split(/\r?\n/).filter(Boolean).map(l => l.split('\t'));
const table = new Map(rows.map(([id, name, desc]) => [`${id}|${name}`, desc]));

function walk(d, ext, o = []) {
  for (const f of fs.readdirSync(d)) {
    const p = path.join(d, f);
    if (fs.statSync(p).isDirectory()) walk(p, ext, o); else if (ext.some(e => f.endsWith(e))) o.push(p);
  }
  return o;
}

const olds = [];   // [옛 글, 새 글]
let changed = 0, same = 0;
const seen = new Set();
for (const f of walk(path.join(ROOT, 'heroes'), ['.json'])) {
  let text = fs.readFileSync(f, 'utf8');
  const h = JSON.parse(text).heroes[0];
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) {
    const key = `${h.id}|${k.name}`;
    const desc = table.get(key);
    if (desc == null) { console.log('표에 없음', key); continue; }
    seen.add(key);
    if (k.desc === desc) { same++; continue; }
    const from = JSON.stringify(k.desc), to = JSON.stringify(desc);
    if (text.split(from).length !== 2) { console.log('글을 한 곳에서 못 찾음', key); continue; }
    text = text.replace(from, () => to);
    olds.push([k.desc, desc]);
    changed++;
  }
  fs.writeFileSync(f, text, 'utf8');
}
for (const key of table.keys()) if (!seen.has(key)) console.log('사도 파일에 없음', key);
console.log(`사도 JSON — 바꿈 ${changed} · 이미 같음 ${same}`);

if (process.argv.includes('--gen')) {
  const srcs = walk(GEN, ['.js', '.mjs']).filter(f => !f.endsWith('desc-short.mjs'));
  let hit = 0;
  const miss = [];
  for (const [o, n] of olds) {
    let found = false;
    for (const f of srcs) {
      const t = fs.readFileSync(f, 'utf8');
      if (!t.includes(o)) continue;
      fs.writeFileSync(f, t.split(o).join(n), 'utf8');
      found = true;
    }
    if (found) hit++; else miss.push(o);
  }
  console.log(`생성기 소스 — 바꿈 ${hit} · 못 찾음 ${miss.length}(이 글은 생성기를 다시 돌리면 옛 글로 돌아간다 — 이 스크립트를 다시 돌릴 것)`);
  for (const m of miss) console.log('  못 찾음:', m);
}
