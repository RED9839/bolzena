// 계기별 생산 · 소비 사도 표 — node _gen/deep/trig_table.mjs <데이터 폴더>   (3성 + 마리 · 쵸피)
// 소비 = 그 계기로 도는 고유 능력 규칙을 가진 사도. 생산 = 카드(기본 · 고유 · 생성)로 그 계기를 만드는 사도.
import fs from 'fs';
const DIR = process.argv[2];
const walk = (fx, cb) => (fx || []).forEach(f => { cb(f); walk(f.then, cb); for (const r of f.rules || []) walk(r.fx, cb); });
const CAT = w => {
  if (!w) return null; const o = w.on;
  if (o === 'drawn') return '감응(뽑힘)'; if (o === 'discard') return '안식(버려짐)'; if (o === 'exhaust') return '소각(소멸)';
  if (o === 'break') return '붕괴(격파)'; if (o === 'kill' || o === 'huntDown') return '파괴(처치)';
  if (o === 'play') { if (w.chain) return '연속'; if (w.sameHero) return '같은 사도 잇달아'; if ((w.minCost || 0) >= 2) return '천상(비용 2+)'; if (w.maxCost === 0) return '비용 0 카드'; if (w.token) return '생성 카드를 냄'; if (w.who === 'other') return '연계(다른 아군)'; return w.type === '공격' ? '공격 카드를 냄' : '카드를 냄'; }
  if (o === 'make') return '생성'; if (o === 'debuff') return '디버프'; if (o === 'overheal') return '넘친 회복'; if (o === 'shieldBreak') return '실드 깨짐';
  if (o === 'shuffle') return '섞기'; if (o === 'pulled') return '끌어옴'; if (o === 'hit') return w.weak ? '약점으로 침' : '적을 침';
  if (o === 'hurt' || o === 'blocked' || o === 'guardSum') return '맞음 · 막음'; if (o === 'spend') return '소모(쓰기)';
  if (['turnStart', 'turnEnd', 'fightStart', 'always', 'lowHp'].includes(o)) return null;
  return o;
};
const PROD = { '감응(뽑힘)': q => q.k === 'draw', '안식(버려짐)': q => q.k === 'discard', '소각(소멸)': q => q.k === 'burn' || q.k === 'exileFrom', '생성': q => q.k === 'make',
  '디버프': q => q.k === 'status' && /Enem|oneEnemy|random|top|low/.test(q.target || ''), '넘친 회복': q => q.k === 'heal', '실드 깨짐': q => q.k === 'shield',
  '끌어옴': q => q.k === 'pull', '붕괴(격파)': q => q.k === 'tough', '비용 0 카드': null, '천상(비용 2+)': null };
const cons = {}, prod = {};
for (const r of fs.readdirSync(`${DIR}/heroes`)) for (const f of fs.readdirSync(`${DIR}/heroes/${r}`)) {
  const j = JSON.parse(fs.readFileSync(`${DIR}/heroes/${r}/${f}`, 'utf8')); const h = j.heroes[0]; if (h.star < 3 && h.id !== '마리' && h.id !== '쵸피') continue;
  for (const x of [...(h.passives || []), ...[h.keyword, ...(h.keywords || [])].filter(Boolean).flatMap(k => k.rules || [])]) { const c = CAT(x.when); if (c) (cons[c] ||= new Set()).add(h.id); }
  const mine = j.cards.filter(c => c.hero === h.id);
  for (const [c, test] of Object.entries(PROD)) {
    let n = 0;
    for (const cd of mine) { if (c === '소각(소멸)' && (cd.tags || []).some(t => t.startsWith('소멸'))) n++; if (c === '비용 0 카드' && cd.cost === 0) n++; if (c === '천상(비용 2+)' && cd.cost >= 2) n++; if (test) walk(cd.fx, q => { if (test(q)) n++; }); }
    if (n >= 2) (prod[c] ||= new Set()).add(h.id);
  }
}
const keys = [...new Set([...Object.keys(cons)])].sort((a, b) => cons[b].size - cons[a].size);
console.log('| 계기 | 소비 사도 수 | 소비 사도 | 생산 사도 수(그 계기를 카드 2장 이상으로) |'); console.log('|---|---|---|---|');
for (const k of keys) console.log(`| ${k} | ${cons[k].size} | ${[...cons[k]].join(' · ')} | ${prod[k] ? prod[k].size : '— (누구나)'} |`);
