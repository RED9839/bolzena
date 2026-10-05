import fs from 'fs';
const rows = fs.readFileSync(process.argv[2], 'utf8').trim().split('\n').map(l => { const [k, p] = l.split('\t'); return { k, p: parseFloat(p) }; });
const solo = {}, same = {}, diff = {};
for (const r of rows) {
  const a = r.k.split(':');
  if (a[0] === 'solo') solo[a[1]] = r.p;
  else for (const h of [a[1], a[2]]) { const m = a[3] === 'same' ? same : diff; (m[h] ||= []).push([a[1] === h ? a[2] : a[1], r.p]); }
}
const avg = l => l && l.length ? l.reduce((s, x) => s + x[1], 0) / l.length : NaN;
for (const h of Object.keys(solo)) {
  console.log(`${h}\t혼자 ${solo[h]}%\t같은 속성 짝 ${avg(same[h]).toFixed(0)}% (${(same[h]||[]).map(x=>x[0]+' '+x[1]).join(', ')})\t다른 속성 짝 평균 ${avg(diff[h]).toFixed(0)}%`);
}
