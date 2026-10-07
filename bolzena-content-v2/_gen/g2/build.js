// G2 빌드 — node build.js
// 쓰는 것: world/equips/{무기,방어구,장신구}.json · world/neutral/교주카드.json · 애착 사도 파일의 "equips" 부분만 ·
//          _ref/그림짝.json(장비 · 교주 카드 id → 원작 아이콘 번호, 화면 몫) · _ref/장비_칸나눔.md(칸 나눔 표)
// 상태카드.json · 저주와선물.json 은 건드리지 않는다.
const fs = require('fs'), path = require('path');
const L = require('./lib');
const EQ = require('./equips').build();
const NC = require('./cards').build();
const ROOT = L.ROOT;

// ── 세계 장비 · 교주 카드 ──
for (const slot of ['무기', '방어구', '장신구'])
  L.writeJson(`${ROOT}/world/equips/${slot}.json`, { equips: EQ.world.filter(e => e.slot === slot) });
L.writeJson(`${ROOT}/world/neutral/교주카드.json`, { cards: NC.cards });

// --world: 세계 장비 · 교주 카드 · _ref 만 쓰고 사도 파일(애착 장비)은 건드리지 않는다(2026-10-07 — 사도 담당이 애착 장비를 고치는 동안)
const WORLD_ONLY = process.argv.includes('--world');
// ── 사도 파일 — 최상위 "equips" 만 갈아 끼운다(다른 글자는 그대로) ──
function walk(d, o = []) { for (const f of fs.readdirSync(d)) { const p = path.join(d, f); if (fs.statSync(p).isDirectory()) walk(p, o); else if (f.endsWith('.json')) o.push(p); } return o; }
// 최상위 키 "equips" 의 [키 시작, 값 끝) 범위와 그 앞 쉼표 위치
function findTopKey(t, key) {
  let depth = 0, i = 0;
  while (i < t.length) {
    const ch = t[i];
    if (ch === '"') {
      let j = i + 1; while (t[j] !== '"') { if (t[j] === '\\') j++; j++; }
      const s = t.slice(i + 1, j);
      if (depth === 1 && s === key) {
        let k = j + 1; while (/\s/.test(t[k])) k++;
        if (t[k] === ':') {
          k++; while (/\s/.test(t[k])) k++;
          // 값 끝
          let d = 0, m = k;
          for (; m < t.length; m++) {
            const c = t[m];
            if (c === '"') { m++; while (t[m] !== '"') { if (t[m] === '\\') m++; m++; } continue; }
            if (c === '[' || c === '{') d++;
            else if (c === ']' || c === '}') { d--; if (d === 0) { m++; break; } }
          }
          // 앞 쉼표
          let p = i - 1; while (/\s/.test(t[p])) p--;
          return { keyStart: i, valStart: k, valEnd: m, commaAt: t[p] === ',' ? p : -1 };
        }
      }
      i = j + 1; continue;
    }
    if (ch === '{' || ch === '[') depth++;
    else if (ch === '}' || ch === ']') depth--;
    i++;
  }
  return null;
}
const indentJson = v => JSON.stringify(L.clean(v), null, 2).replace(/\n/g, '\n  ');
const heroFiles = WORLD_ONLY ? [] : walk(`${ROOT}/heroes`);
const touched = [], removed = [];
const owners = new Set(Object.keys(EQ.affinity));
for (const f of heroFiles) {
  let t = fs.readFileSync(f, 'utf8');
  const j = JSON.parse(t);
  const h = j.heroes && j.heroes[0];
  if (!h) continue;
  const want = owners.has(h.id) ? EQ.affinity[h.id] : null;
  const at = findTopKey(t, 'equips');
  if (at) removed.push(...JSON.parse(t.slice(at.valStart, at.valEnd)).map(e => `${e.id}(${h.id})`));
  if (!want && !at) continue;
  if (at && want) t = t.slice(0, at.valStart) + indentJson(want) + t.slice(at.valEnd);
  else if (at && !want) t = t.slice(0, at.commaAt >= 0 ? at.commaAt : at.keyStart) + t.slice(at.valEnd);
  else {
    const end = t.lastIndexOf('}');
    let p = end - 1; while (/\s/.test(t[p])) p--;
    t = t.slice(0, p + 1) + ',\n  "equips": ' + indentJson(want) + t.slice(p + 1);
  }
  JSON.parse(t); // 깨지지 않았나
  // 다른 부분은 그대로인지
  const a = JSON.parse(fs.readFileSync(f, 'utf8')), b = JSON.parse(t);
  delete a.equips; delete b.equips;
  if (JSON.stringify(a) !== JSON.stringify(b)) throw new Error('equips 밖이 바뀜 ' + f);
  fs.writeFileSync(f, t, 'utf8');
  touched.push(`${h.id}: ${want ? want.map(e => e.id).join(',') : '(애착 없음 — 지움)'}`);
  owners.delete(h.id);
}
if (owners.size && !WORLD_ONLY) throw new Error('사도 파일을 못 찾은 애착 주인: ' + [...owners].join(','));

// ── 그림짝 · 칸 나눔 표 ──
L.writeJson(`${ROOT}/_ref/그림짝.json`, {
  _note: '장비 · 교주 카드 id → 원작 아이콘 번호(C:/projects/볼제나/assets/gear · assets/spell 의 webp). 데이터 틀(equip · card)에 그림 칸이 없어 화면이 이 짝으로 붙인다. 이름 없는 아이콘(스펠 10 · 아티팩트 20)은 쓰지 않는다.',
  equip: Object.fromEntries(EQ.rows.map(r => [r.id, r.icon])),
  neutral: Object.fromEntries(NC.rows.map(r => [r.id, r.icon])),
});
const st = s => Object.entries(s).map(([k, v]) => `${{ hp: 'HP', atk: '공격', def: '방어', crit: '치명' }[k]} ${v}`).join(' · ');
let md = '# 장비 칸 나눔 — 원작 아티팩트 86(한국 서버)\n\n_gen/g2/build.js 가 만든다. 원작에는 부위가 없어 아이콘 생김새(날붙이 · 지팡이 · 활 · 총 = 무기, 옷 · 갑옷 · 모자 · 장갑 = 방어구, 반지 · 책 · 소품 = 장신구)와 효과(공격 · 방어 · 그 밖)로 나눴다.\n\n';
for (const slot of ['무기', '방어구', '장신구']) {
  const rs = EQ.rows.filter(r => r.slot === slot);
  md += `## ${slot} (${rs.length})\n\n| 등급 | 이름 | id | 애착 | 능력치 | 효과 | 아이콘 |\n|---|---|---|---|---|---|---|\n`;
  for (const g of ['전설', '희귀', '고급', '일반']) for (const r of rs.filter(r => r.grade === g))
    md += `| ${r.grade} | ${r.name} | ${r.id} | ${r.owner || ''} | ${st(r.stats)} | ${r.effect} | ${r.icon} |\n`;
  md += '\n';
}
md += '## 등급 × 칸\n\n| | 무기 | 방어구 | 장신구 |\n|---|---|---|---|\n';
for (const g of ['전설', '희귀', '고급', '일반']) md += `| ${g} | ${['무기', '방어구', '장신구'].map(s => EQ.rows.filter(r => r.grade === g && r.slot === s).length).join(' | ')} |\n`;
fs.writeFileSync(`${ROOT}/_ref/장비_칸나눔.md`, md, 'utf8');

console.log('세계 장비', EQ.world.length, '애착', Object.values(EQ.affinity).flat().length, '교주 카드', NC.cards.length);
console.log('사도 파일:', touched.join(' | '));
console.log('지운 옛 애착:', removed.join(', '));
