if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 마지막 전체 세기 맞춤 — node _gen/deep/retune.mjs <solo.md> [적용]
// 같은 측정 안의 일반 역할 평균 ±5(엘다인: 일반 평균 ~ +8) 밖인 리뉴얼 사도(_gen/deep/heroes)만 tune.json 배율을 (목표/지금)^0.7 로(0.7~1.45배 한 번에).
// 목표는 범위 안쪽(역할 평균 쪽으로 반걸음). 「적용」 을 주면 tune.json 을 고쳐 쓴다.
import fs from 'fs';
const HERE = new URL('.', import.meta.url);
const md = fs.readFileSync(process.argv[2], 'utf8');
const EL = new Set(['아야', '시온더다크불릿', '에피카', '비비', '클로에', '이드', '우이', '우이_기억', '리뉴아', '실비아', '비비_신성', '에르핀_왕도', '란', '디아나_왕년', '벨라', '죠안', '우로스', '티그_영웅', '네르_빡침', '요미']);
const idOf = n => n.replace(/ /g, '').replace(/\((.+)\)$/, '_$1');
const rows = [...md.matchAll(/^\| ([^|]+) \| (딜러|서포터|탱커) \| ([^|]+) \| (\d+) \| ([\d.]+)% \| ±([\d.]+) \|$/gm)].map(m => ({ id: idOf(m[1].trim()), role: m[2], race: m[3].trim(), w: +m[5] }));
const avg = a => a.reduce((x, y) => x + y, 0) / a.length;
const gen = rows.filter(r => !EL.has(r.id)), ga = avg(gen.map(r => r.w));
const ra = Object.fromEntries(['딜러', '서포터', '탱커'].map(k => [k, avg(gen.filter(r => r.role === k).map(r => r.w))]));
const mine = new Set(); for (const k of ['요정/에르핀', '요정/캬롯', '유령/셰이디', '정령/시저']) mine.add(k);   // 원본 그대로 두는 넷도 스탯으로는 맞춘다
for (const r of fs.readdirSync(new URL('./heroes/', HERE))) for (const f of fs.readdirSync(new URL(`./heroes/${r}/`, HERE))) mine.add(`${r}/${f.replace('.json', '')}`);
// 2026-10-10 카제나식: 스탯은 역할 × 등급 표 하나 — 사도별 세기는 고유 카드 · 완성형 · 고학년 배율(tune.json), 한 바퀴에 한 칸(±10%)
const TF = new URL('./tune.json', HERE), T = JSON.parse(fs.readFileSync(TF, 'utf8'));
const out = [];
for (const r of rows) {
  const key = [...mine].find(k => k.split('/')[1] === r.id); if (!key) continue;
  const [lo, hi] = EL.has(r.id) ? [ga, ga + 8] : [ra[r.role] - 5, ra[r.role] + 5];
  if (r.w >= lo && r.w <= hi) continue;
  const target = r.w < lo ? lo + 2 : hi - 2;
  const f = Math.min(1.1, Math.max(0.9, Math.pow(target / Math.max(3, r.w), 0.7)));
  const old = T[key] ?? 1, nu = Math.round(old * f * 100) / 100;
  out.push(`${key} ${r.w}% → 목표 ${target.toFixed(1)} · ${old} → ${nu}`);
  T[key] = nu;
}
console.log(`일반 평균 ${ga.toFixed(1)} · 딜러 ${ra.딜러.toFixed(1)} 서포터 ${ra.서포터.toFixed(1)} 탱커 ${ra.탱커.toFixed(1)} · 고칠 사도 ${out.length}`);
console.log(out.join('\n'));
if (process.argv[3] === '적용') fs.writeFileSync(TF, JSON.stringify(T));
