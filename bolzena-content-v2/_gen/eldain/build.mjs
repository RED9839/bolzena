// 엘다인 20명 빌드 — node build.mjs [--apply 사도키,사도키|all] [--only 사도키,…]
// src/<사도키>.mjs(손으로 쓴 설계) → out/heroes/<종족>/<사도키>.json(늘 씀, _ 폴더라 엔진은 안 읽음).
// --apply 면 그 사도만 콘텐츠 heroes/ 에 옮긴다(엔진 기능이 아직 없는 사도는 옮기지 않는다 — 읽기 실패로 다른 작업을 막지 않게).
// 같은 원본이면 늘 같은 결과(키 순서 · 들여쓰기 고정).
import fs from 'fs';
import path from 'path';
import { fileURLToPath, pathToFileURL } from 'url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../..');
const SRC = path.join(HERE, 'src');
const OUT = path.join(HERE, 'out', 'heroes');
const arg = (k) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1] : null; };
const only = arg('--only')?.split(',');
const apply = arg('--apply');

const files = fs.readdirSync(SRC).filter(f => f.endsWith('.mjs')).sort();

// ── 신탁 맞춤(4차 덧붙임 10-11 — 사용자: 세기 맞춤으로 기본형을 올리면 신탁도 함께) ──
// 1) 같은 꼴 신탁(조각 종류 · 대상이 기본형과 같은 차례): 피해 · 실드 · 회복 비율이 기본형보다 낮으면 기본형 값으로 올리고,
//    아무 칸도 기본형보다 크지 않으면(「수치만」) 가장 큰 비율 칸을 ×1.3 — 기본형보다 확실히 세게.
// 2) oracle_tune.json { 카드 id: { 신탁 번호: 배율 } } — bz check 「기본보다 낫지 않다」 를 읽어 tune_oracles.py 가 쓴다. 그 신탁의 피해 · 실드 · 회복 비율에 곱한다.
const POW = new Set(['dmg', 'shield', 'heal']);
const grid = (v) => { const a = Math.abs(v); const g = a >= 0.2 ? 0.1 : 0.05; return Math.sign(v) * Math.round(Math.ceil(a / g - 1e-9) * g * 100) / 100; };
const shape = (fx) => (fx || []).map(f => `${f.k}|${f.target || ''}|${f.id || ''}`).join(',');
const TUNE = fs.existsSync(path.join(HERE, 'oracle_tune.json')) ? JSON.parse(fs.readFileSync(path.join(HERE, 'oracle_tune.json'), 'utf8')) : {};
function syncOracles(c) {
  if (!c.oracles || !c.unique) return;
  c.oracles.forEach((o, i) => {
    if (o.power || !o.fx) return;
    if ((o.cost ?? c.cost) === c.cost && (o.type ?? c.type) === c.type && shape(o.fx) === shape(c.fx)) {
      let up = false, best = -1, bestV = 0;
      o.fx.forEach((f, j) => {
        const b = c.fx[j];
        if (!POW.has(f.k) || typeof f.ratio !== 'number' || f.ofEvent || f.ofShield || f.ofForm) return;
        if (f.ratio < b.ratio) f.ratio = b.ratio;
        if (f.ratio > b.ratio) up = true;
        if (f.ratio > bestV) { bestV = f.ratio; best = j; }
      });
      const others = JSON.stringify(o.fx.map(f => ({ ...f, ratio: 0 }))) !== JSON.stringify(c.fx.map(f => ({ ...f, ratio: 0 })));
      if (!up && !others && best >= 0) o.fx[best].ratio = grid(o.fx[best].ratio * 1.3);
    }
    const t = TUNE[c.id]?.[String(i + 1)];
    if (t && t !== 1) {
      const walk = (fx) => (fx || []).forEach(f => { if (POW.has(f.k) && typeof f.ratio === 'number' && !f.ofEvent && !f.ofShield && !f.ofForm) { f.ratio = grid(f.ratio * t); if (typeof f.plus === 'number') f.plus = grid(f.plus * t); } if (f.then) walk(f.then); if (f.hitFx) walk(f.hitFx); });
      walk(o.fx);
    }
  });
}

const built = [];
for (const f of files) {
  const id = f.replace(/\.mjs$/, '');
  if (only && !only.includes(id)) continue;
  const mod = await import(pathToFileURL(path.join(SRC, f)).href);
  const data = mod.default;
  const h = data.heroes[0];
  for (const c of data.cards) syncOracles(c);
  // 폴더는 지금 콘텐츠의 자리 그대로(미스틱 둘은 마녀 폴더) — 없으면 종족 이름
  const HD = path.join(ROOT, 'heroes');
  const dir = fs.readdirSync(HD).find(d => fs.existsSync(path.join(HD, d, `${h.id}.json`))) ?? h.race;
  const rel = path.join(dir, `${h.id}.json`);
  const txt = JSON.stringify(data, null, 2) + '\n';
  fs.mkdirSync(path.join(OUT, dir), { recursive: true });
  fs.writeFileSync(path.join(OUT, rel), txt);
  built.push({ id: h.id, rel, txt });
}
console.log(`빌드 ${built.length}명 → ${OUT}`);
if (apply) {
  const ids = apply === 'all' ? built.map(b => b.id) : apply.split(',');
  for (const b of built.filter(b => ids.includes(b.id))) {
    fs.writeFileSync(path.join(ROOT, 'heroes', b.rel), b.txt);
    console.log(`  옮김 heroes/${b.rel.replace(/\\/g, '/')}`);
  }
}
