// 시범 사도 카드 글 길이 — node textlen.mjs <데이터 폴더> 사도키,사도키 [--all]
// bz hero 출력에서 고유 카드 · 신탁 · 변신판 글을 모아 공백 뺀 글자 수를 잰다. 기준(00_기준.md §3-7): 고유 · 신탁 45 · 강화(강화 카드 · 강화화 신탁 · 변신판) 55
import { execSync } from 'child_process';
const [data, list] = process.argv.slice(2); const all = process.argv.includes('--all');
const BZ = 'C:/projects/bolzena-core/Tools~/Dev/bz.cmd';
const len = s => s.replace(/\s+/g, '').length;
let over = 0; const L = [];
for (const h of list.split(',')) {
  const out = execSync(`"${BZ}" --data "${data}" hero ${h}`, { encoding: 'utf8' }).split(/\r?\n/);
  const items = []; let cur = null;
  for (const l of out) {
    let m;
    if ((m = l.match(/^  (고유|변신판) 「([^」]+)」 \[\w+\] (\S+) · (.*)$/))) { cur = { h, n: `${m[2]}`, t: m[1] === '변신판' ? '변신판' : m[3], s: m[4].replace(/ \(← .*$/, '') }; items.push(cur); }
    else if ((m = l.match(/^  시작 「([^」]+)」 \[\w+\] (.*)$/))) { if (!items.some(x => x.n === m[1] && x.t === '기본')) { cur = { h, n: m[1], t: '기본', s: m[2] }; items.push(cur); cur.o = true; } else cur = null; }
    else if ((m = l.match(/^    신탁(\d) 「[^」]*」 (.*)$/))) { cur = { h, n: `${items.filter(x => x.t !== '변신판').length ? '' : ''}신탁${m[1]}`, t: '신탁', s: m[2] }; cur.card = items.filter(x => !x.o && x.t !== '변신판').length; items.push(cur); cur.o = true; }
    else if (/^    (축복|패시브)|^  (시작|─)|^\S/.test(l)) cur = null;
    else if (cur && l.trim()) cur.s += ' ' + l.trim();
  }
  const uniqNames = new Set(items.filter(x => x.t !== '기본').map(x => x.n));
  for (const it of items.filter(x => !(x.t === '기본' && uniqNames.has(x.n)))) {
    const power = it.t === '강화' || it.t === '변신판' || /강화 카드\./.test(it.s);
    const cap = it.t === '기본' ? 30 : power ? 55 : 45, n = len(it.s.replace(/^강화 카드\.\s*/, ''));
    L.push(n); if (n > cap) over++;
    if (all || n > cap) console.log(`${n > cap ? '넘침' : '    '} ${h} ${it.n} ${n}/${cap} ${it.s}`);
  }
}
L.sort((a, b) => a - b);
console.log(`글 ${L.length}개 · 중앙 ${L[Math.floor(L.length / 2)]} · 90% ${L[Math.floor(L.length * 0.9)]} · 최대 ${L[L.length - 1]} · 넘침 ${over}`);
