// 시범 보고 문서 조립: 시범_머리.md + 측정 + 부록(엔진 글 전후) → _measure/사도_리워크_시범.md
// node assemble.mjs <scratch 폴더>   (scratch: solo_base.md · solo_fin3.md · sim_base_400.txt · sim_fin3_400.txt · solo_all_base.md · kw_fin3.tsv · m_base.json · m_fin.json · txt/<사도>_전/후.txt · 꼬리.md)
import fs from 'fs';
const S = process.argv[2];
const rd = f => fs.readFileSync(`${S}/${f}`, 'utf8');
const solo = f => Object.fromEntries(rd(f).split(/\r?\n/).filter(l => /^\| \S.* \| (탱커|딜러|서포터) \|/.test(l)).map(l => l.split('|').map(x => x.trim())).map(r => [r[1], { role: r[2], n: r[4], pct: parseFloat(r[5]), err: r[6] }]));
const b = solo('solo_base.md'), a = solo('solo_fin3.md'), all = solo('solo_all_base.md');
const roleAvg = {}; for (const r of rd('solo_all_base.md').split(/\r?\n/)) { const m = r.match(/^\| 역할 (\S+) \| \d+ \| ([\d.]+)%/); if (m) roleAvg[m[1]] = m[2]; }
const avgAll = rd('solo_all_base.md').match(/평균 ([\d.]+)%/)[1];
const names = ['루드', '에르핀', '비비(신성)', '멜루나', '디아나', '키샤'];
let t = '| 사도 | 역할 | 전 | 후 | 차이 | 같은 역할 평균 | 판정 |\n|---|---|---|---|---|---|---|\n';
for (const n of names) {
  const x = b[n], y = a[n]; const d = (y.pct - x.pct).toFixed(1); const ra = parseFloat(roleAvg[y.role]);
  const elder = n === '비비(신성)';
  const ok = elder ? `엘다인 — 목표 27~31 · 상한 32.4(일반 24.4 + 8) — ${y.pct > 32.4 ? '상한보다 ' + (y.pct - 32.4).toFixed(1) + '%p 위(오차 ±2 안)' : '안'}` : (Math.abs(y.pct - ra) <= 5 ? '역할 평균 ±5 안' : `역할 평균보다 ${(y.pct - ra).toFixed(1)}%p(전 ${(x.pct - ra).toFixed(1)}) — 전보다 가까워짐`);
  t += `| ${n} | ${y.role} | ${x.pct}% (${x.n}판 ${x.err}) | **${y.pct}%** (${y.n}판 ${y.err}) | ${d > 0 ? '+' : ''}${d}%p | ${ra}% | ${ok} |\n`;
}
const simLine = f => rd(f).split(/\r?\n/).slice(0, 13).join('\n');
const mb = JSON.parse(rd('m_base.json')), mf = JSON.parse(rd('m_fin.json'));
let m = '| 지표 | 전 | 후 | 목표(지침) |\n|---|---|---|---|\n';
const goal = { '2코': '24장 중 5장 안팎(20%)', '비용↓갈래 카드': '카드 3장 중 1장(8장)', '비용↑갈래 카드': '카드 2장 중 1장', '강화화 갈래 카드': '카드 3장 중 1장(8장)', '스케일링·서치 갈래 카드': '카드 3장 중 1장', '조건 갈래 카드': '카드 3장 중 1장', '얕은 갈래 중앙': '1~2' };
for (const k of Object.keys(goal)) m += `| ${k} | ${mb[k]} | ${mf[k]} | ${goal[k]} |\n`;
m += `| 태그(카드 + 신탁에 붙은 수) | ${Object.entries(mb['태그']).map(([k, v]) => k + ' ' + v).join(' · ')} | ${Object.entries(mf['태그']).map(([k, v]) => k + ' ' + v).join(' · ')} | 보존 · 소멸 · 생성 · 버리기 사도 4명 중 1명꼴 |\n`;
m += `| 생성 · 버리기 · 서치(사도 수) | 생성 1(에르핀) · 버리기 0 · 서치 0 | 생성 1(에르핀 케이크) · 버리기 1(키샤 체리색 집착) · 서치 5(루드 · 에르핀 · 비비 · 멜루나 · 키샤 — 종류 · 사도 거르개 드로우) | |\n`;
let head = fs.readFileSync(new URL('./시범_머리.md', import.meta.url), 'utf8').replace('@@METRICS@@', m);
let kw = '';
try {
  const L = rd('kw_fin3.tsv').trim().split(/\r?\n/);
  kw = '| ' + L[0].split('\t').join(' | ') + ' |\n|' + L[0].split('\t').map(() => '---').join('|') + '|\n' + L.slice(1).filter(l => ['루드', '에르핀', '비비_신성', '멜루나', '디아나', '키샤'].includes(l.split('	')[0])).map(l => '| ' + l.split('\t').join(' | ') + ' |').join('\n') + '\n';
} catch { kw = '(계측 표 없음)\n'; }
const tail = rd('꼬리.md');
let app = '\n---\n\n## 부록 — 엔진 글 전후(`bz hero <사도>` 그대로)\n';
for (const [k, n] of [['루드', '루드'], ['에르핀', '에르핀'], ['비비_신성', '비비(신성)'], ['멜루나', '멜루나'], ['디아나', '디아나'], ['키샤', '키샤']]) {
  app += `\n### ${n}\n<details><summary>전</summary>\n\n\`\`\`\n${rd(`txt/${k}_전.txt`).trim()}\n\`\`\`\n</details>\n\n<details open><summary>후</summary>\n\n\`\`\`\n${rd(`txt/${k}_후.txt`).trim()}\n\`\`\`\n</details>\n`;
}
const out = head + `\n## 3. 측정\n\n### 3-1. 혼자 완주율(\`bz solo --err 2 --min 2000\` — 그 사도 + 무작위 둘 · 판 전체)\n전체 135명 혼자 평균 **${avgAll}%**(리워크 전 · \`--err 4\` 7만 판) — 딜러 ${roleAvg['딜러']} · 서포터 ${roleAvg['서포터']} · 탱커 ${roleAvg['탱커']}% · 엘다인 20명 30.5 · 일반 115명 24.4%.\n\n${t}\n### 3-2. 전체 메타 시뮬(\`bz sim 400 --bot skilled --party role\`, 같은 씨앗 · 18,000판 · ±0.7%p)\n전:\n\`\`\`\n${simLine('sim_base_400.txt')}\n\`\`\`\n후:\n\`\`\`\n${simLine('sim_fin3_400.txt')}\n\`\`\`\n\n### 3-3. 고유 효과 계측(\`bz solo --kw\`) — 장치가 실제로 도는지\n${kw}\n` + tail + app;
fs.writeFileSync('C:/projects/bolzena-content-v2/_measure/사도_리워크_시범.md', out, 'utf8');
console.log('썼다', out.length);
