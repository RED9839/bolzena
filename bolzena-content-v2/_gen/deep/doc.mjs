// 묶음 문서 만들기 — node _gen/deep/doc.mjs <묶음(b0 …)> <데이터 폴더> <전 solo.md> <후 solo.md> [제목] > 묶음_N.md 의 사도 절
// 묶음 스크립트의 주석(// ── 사도 ── · // 엔진: · U 줄 「// 역할 — 근거」 · O 줄 「// 방향」)과 bz hero 글, solo 표를 모은다.
import fs from 'fs'; import { execSync } from 'child_process';
const [key, data, soloA, soloB, title] = process.argv.slice(2);
const BZ = 'C:/projects/bolzena-core/Tools~/Dev/bz.cmd';
const src = fs.readFileSync(new URL(`./${key}.mjs`, import.meta.url), 'utf8');
const files = [...src.matchAll(/\['([^'/]+)\/([^']+)',\s*(\w+)\]/g)].map(m => ({ race: m[1], id: m[2], fn: m[3] }));
const solo = f => { const o = {}; if (!f || !fs.existsSync(f)) return o; for (const m of fs.readFileSync(f, 'utf8').matchAll(/^\| ([^|]+) \| (딜러|서포터|탱커) \| ([^|]+) \| (\d+) \| ([\d.]+)% \| ±([\d.]+) \|$/gm)) o[m[1].trim()] = +m[5]; return o; };
const SA = solo(soloA), SB = solo(soloB);
const len = s => s.replace(/\s+/g, '').length;
const out = [];
for (const h of files) {
  const i = src.indexOf(`function ${h.fn}(`);
  const head = src.slice(src.lastIndexOf('// ──', i), i);
  const body = src.slice(i, i + src.slice(i).search(/\r?\n\}\r?\n/));
  const sheet = execSync(`"${BZ}" --data "${data}" hero ${h.id}`, { encoding: 'utf8' });
  const name = sheet.split(/\r?\n/)[0].split(' — ')[0];
  const roleLine = sheet.split(/\r?\n/)[0];
  const t = (head.match(/\/\/ ── (.*?) ──/) || [])[1] || name;
  const engine = (head.match(/\/\/ 엔진: (.*)/) || [])[1] || '';
  out.push(`## ${name}\n`);
  out.push(`- **${roleLine.split(' — ')[1] || ''}** · ${t.split(' — ')[1] || ''}`);
  out.push(`- **엔진 한 줄:** ${engine}`);
  // 기본 · 고유 카드 글(bz hero)
  const lines = sheet.split(/\r?\n/);
  const cardText = (kind, nm) => { const k = lines.findIndex(l => l.startsWith(`  ${kind} 「${nm}」`)); if (k < 0) return ''; let s = lines[k].replace(/^  \S+ 「[^」]+」 /, ''); for (let q = k + 1; q < lines.length && lines[q].trim() && !/^  \S/.test(lines[q]) && !/^(패시브|고유 효과|변신|고학년|HP) /.test(lines[q]); q++) s += ' ' + lines[q].trim(); return s; };
  const basics = [...new Set(lines.filter(l => l.startsWith('  시작 ')).map(l => l.match(/「([^」]+)」/)[1]))];
  out.push(`\n**기본 카드**(시작 덱)\n\n| 카드 | 글 | 글자 |\n|---|---|---|`);
  for (const b of basics) { const s = cardText('시작', b); out.push(`| ${b} | ${s} | ${len(s.replace(/^\[\w+\] /, ''))} |`); }
  // 고유 4장 + 신탁 방향(주석)
  out.push(`\n**고유 4장 · 신탁 방향**\n\n| 칸 | 카드 | 기본 글 | ① | ② | ③ | ④ | ⑤ |\n|---|---|---|---|---|---|---|---|`);
  for (const m of body.matchAll(/U\(`\$\{H\}_(u\d)`, '([^']+)',[^\n]*\/\/ ([^\n]*)\n((?:\s+O\([^\n]*\n){5})/g)) {
    const dirs = [...m[4].matchAll(/\/\/ ([^\n]*)/g)].map(x => x[1].trim());
    const [role, why] = m[3].split(' — ');
    out.push(`| ${role.trim()} | ${m[2]}${why ? `<br><sub>${why.trim()}</sub>` : ''} | ${cardText('고유', m[2]).replace(/\|/g, '/')} | ${dirs.join(' | ')} |`);
  }
  // 패시브 · 고유 효과 · 변신
  const pas = lines.filter(l => /^(패시브|고유 효과|변신) 「/.test(l)).map(l => { const k = lines.indexOf(l); let s = l; for (let q = k + 1; q < lines.length && /^    /.test(lines[q]); q++) s += ' ' + lines[q].trim(); return s; });
  out.push(`\n**고유 효과 · 패시브 · 변신**\n`); const seen = new Set(); for (const p of pas) { const k = p.slice(0, 2) + p.match(/「[^」]+」/)[0]; if (seen.has(k)) continue; seen.add(k); out.push(`- ${p}`); }
  const ult = lines.find(l => l.startsWith('고학년'));
  if (ult) out.push(`- ${ult} ${lines[lines.indexOf(ult) + 1].trim()}`);
  const team = (body.match(/\/\/ 팀: (.*)/) || [])[1];
  if (team) out.push(`- **팀 고리:** ${team}`);
  const orig = (body.match(/\/\/ 원작: (.*)/) || [])[1];
  if (orig) out.push(`- **원작 근거:** ${orig}`);
  const a = SA[name], b = SB[name];
  out.push(`\n**측정:** 혼자 완주율 ${a ?? '?'}% → **${b ?? '?'}%**\n`);
}
console.log((title ? `# ${title}\n\n` : '') + out.join('\n'));
