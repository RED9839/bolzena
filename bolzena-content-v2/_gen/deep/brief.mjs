// 설계 전 한 장 — node _gen/deep/brief.mjs <사도 파일 이름(마리 · 에르핀_왕도 …)> …
// 백업(리뉴얼 전) 데이터의 기본 카드 꼴 · 고유 효과 · 패시브 · 고학년 · 장비 애착, 그림 저학년 칸, 갈래 세분화 문서의 그 사도 절, 나무위키 요약(스킬 · 어사이드)을 한 번에.
import fs from 'fs'; import path from 'path';
const BACK = 'C:/projects/_backup/heroes_villages_before_deep_20261009/heroes';
const LOW = JSON.parse(fs.readFileSync('C:/projects/bolzena-runui/Tools~/cardart_low.json', 'utf8')).cards;
const SEG = 'C:/projects/bolzena-content-v2/_measure/갈래_세분화';
const NAMU = 'C:/projects/볼제나/.omc/research/namu';
const segs = fs.readdirSync(SEG).filter(f => /^[A-H]_.*\.md$/.test(f)).map(f => fs.readFileSync(path.join(SEG, f), 'utf8'));
const fx = l => (l || []).map(f => f.k + (f.id ? ':' + f.id : '') + (f.ratio ? ' ' + f.ratio : '') + (f.v !== undefined ? ' ' + f.v : '') + (f.target ? '@' + f.target : '') + (f.k === 'power' ? '{' + f.rules.map(r => r.when.on + '→' + fx(r.fx)).join(';') + '}' : '')).join(', ');
for (const key of process.argv.slice(2)) {
  const file = fs.readdirSync(BACK).map(d => path.join(BACK, d, key + '.json')).find(fs.existsSync);
  const j = JSON.parse(fs.readFileSync(file, 'utf8')); const h = j.heroes[0];
  console.log(`\n######## ${h.name} (${key}) — ${h.role} · ${h.nature} · ${h.race} · ${h.style} · ${h.star}성 · HP ${h.hp} 공 ${h.atk} 방 ${h.def}`);
  console.log('파일', path.relative(BACK, file).replace(/\\/g, '/'), '· 시작', h.starter.join(' '));
  for (const c of j.cards.filter(c => !c.unique)) console.log(`  ${c.token ? '토큰' : '기본'} ${c.id} 「${c.name}」 ${c.cost} ${c.type} ${(c.tags || []).join('/')} — ${fx(c.fx)}`);
  for (const c of j.cards.filter(c => c.unique)) console.log(`  고유 ${c.id} 「${c.name}」 ${c.cost} ${c.type} ${(c.tags || []).join('/')} — ${fx(c.fx)}${LOW[c.id] ? '   ★저학년 칸(' + LOW[c.id] + ')' : ''}`);
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) console.log(`  키워드 「${k.name}」 ${k.carrier} cap ${k.cap ?? '-'} ${JSON.stringify(k.per || '')} ${k.onMax ? 'onMax ' + JSON.stringify(k.onMax) : ''} ${(k.rules || []).map(r => r.when.on + '→' + fx(r.fx)).join(' ; ')}`);
  for (const p of h.passives || []) console.log(`  패시브 「${p.name}」 ${p.when.on}${p.when.type ? ':' + p.when.type : ''}${p.when.who ? ':' + p.when.who : ''} → ${fx(p.fx)}`);
  for (const f of h.forms || []) console.log(`  변신 「${f.name}」 ${f.id} turns ${f.turns} cards ${JSON.stringify(f.cards || {})}`);
  console.log(`  고학년 「${h.ult.name}」 ${h.ult.cost} — ${fx(h.ult.fx)}`);
  for (const e of j.equips || []) console.log(`  장비 ${e.id} 애착 ${JSON.stringify(e.affinityEffect || [])}`);
  // 갈래 세분화 문서
  const nm = h.name;
  for (const t of segs) {
    const i = t.indexOf(`### ${nm} — `); if (i < 0) continue;
    const e = t.indexOf('\n### ', i + 5); const sec = t.slice(i, e < 0 ? undefined : e);
    console.log(sec.split('\n').filter(l => /원작 근거|^\s+- |한 줄|고유 4장|^\s+\d\. |패시브 2개|같은 갈래 안/.test(l)).slice(0, 22).join('\n'));
    break;
  }
  // 나무위키 스킬
  const nf = [key, key.replace(/_/g, '')].map(k => path.join(NAMU, k + '.brief.md')).find(fs.existsSync);
  if (nf) { const b = fs.readFileSync(nf, 'utf8'); const s = b.slice(b.indexOf('## 스킬'), b.indexOf('## 개요') > 0 ? b.indexOf('## 개요') : undefined); console.log(s.split('\n').filter(l => l.trim() && !/^(최대|물리|마법|피해|SP|HP|재사용|\(최고|증가|감소)/.test(l.trim())).slice(0, 45).join('\n')); }
}
