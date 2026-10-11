if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 보스 클론 기믹(2026-10-10 사용자 결정 — 보스가 너무 약해짐) — node _gen/deep/bossgimmick.mjs [F1,F2,DM] [출력 폴더]
// 바탕: _gen/deep/villages_pre_boss/(기믹 전 사본 — 클론을 새로 끼울 땐 mergevillages 로 이 사본에도 넣는다). 출력: 원본 world/villages(클론 항목만 바꿈).
// 고학년은 엔진(BossUlt)이 이미 보스 특수 공격으로 쓴다(2026-10-09 끊기 없음) — 여기서는 고학년을 또 넣지 않는다.
// 모든 사도 클론에 그 사도 원작 근거의 보스 전용 기믹 두 가지:
//  ① 국면 전환 — 체력 절반 아래로 처음 내려가면 그 사도 고유 효과 이름으로 「본기」: 사기 1(공격 ↑). 한 번뿐.
//  ② 그 사도 고유 기믹 — bossgimmicks_table.mjs(사도마다 원작 · 새 덱 방식에서 한 가지 + 대응 수단 한 줄).
// 세기: 엔진은 보스 칸의 「몸」(그 층 보스 줄에 적힌 클론)의 체력에 다른 사도 키트를 비율로 맞춘다(GameData.MakeClone) — 그래서 층 세기는 몸에만 곱한다.
//  F1 = 1층 몸 · F2 = 2층 몸의 체력 · 피해 배율, DM = 모든 키트 피해 배율. 대응 불가 즉사 · 긴 끊기는 넣지 않는다.
import fs from 'fs'; import path from 'path';
import { GIM } from './bossgimmicks_table.mjs';
const ROOT = 'C:/projects/bolzena-content-v2';
const SRC = `${ROOT}/_gen/deep/villages_pre_boss`;
const OUT = process.argv[3] || `${ROOT}/world/villages`;
const [F1, F2, DM] = (process.argv[2] || '1,1.25,1').split(',').map(Number);
const heroes = {};
for (const r of fs.readdirSync(`${ROOT}/heroes`)) for (const f of fs.readdirSync(`${ROOT}/heroes/${r}`)) {
  if (!f.endsWith('.json')) continue;
  const h = JSON.parse(fs.readFileSync(`${ROOT}/heroes/${r}/${f}`, 'utf8')).heroes[0]; heroes[h.id] = h;
}
const HITS = new Set(['attack', 'back', 'attackAll', 'multi', 'thorns']);
const scaleIt = (it, m) => { if (it && HITS.has(it.t) && it.v) it.v = Math.round(it.v * m); if (it?.next) scaleIt(it.next, m); for (const t of it?.then || []) scaleIt(t, m); };
let n = 0;
for (const f of fs.readdirSync(SRC).filter(x => x.endsWith('.json'))) {
  const d = JSON.parse(fs.readFileSync(path.join(SRC, f), 'utf8'));
  const cur = JSON.parse(fs.readFileSync(path.join(OUT, f), 'utf8'));
  for (const e0 of d.enemies.filter(x => x.clone)) {
    const h = heroes[e0.clone]; if (!h) continue;
    const e = JSON.parse(JSON.stringify(e0));
    const floor = (d.villages || []).flatMap(v => (v.floors || []).map((fl, i) => (fl.boss || []).includes(e.id) ? i : -1)).find(i => i >= 0);
    const F = floor === 0 ? F1 : floor >= 1 ? F2 : 1, M = F * DM;
    e.hp = Math.round(e.hp * F);
    for (const it of [e.open, ...(e.intents || []), ...(e.phase?.intents || []), ...(e.phase2?.intents || [])]) scaleIt(it, M);
    for (const c of e.counters || []) scaleIt(c.act, M);
    for (const p of e.passives || []) scaleIt(p.do, M);
    const allIt = [...(e.intents || []), ...(e.phase?.intents || [])];
    const base = Math.max(90, ...allIt.filter(i => i.t === 'attackAll').map(i => i.v));
    const kw = h.keyword?.name || h.keywords?.[0]?.name || h.passives?.[1]?.name || '본기';
    e.passives = e.passives || [];
    // ① 국면 전환 — 절반 아래에서 한 번 본기(사기 1)
    e.passives.push({ name: kw, on: 'lowHp', at: 0.5, do: { t: 'buff', id: '사기', v: 1, say: `「${kw}」 본기` } });
    // ② 그 사도 고유 기믹(bossgimmicks_table.mjs) — 원작 · 새 덱 방식에서 한 가지, 정보창 한 줄에 대응 수단
    const g = GIM[h.id]; if (!g) { console.log('기믹 없음', h.id); continue; }
    const [gn0, on, opt, spec, line] = g;
    const names = new Set(e.passives.map(p => p.name)); const gn = names.has(gn0) ? gn0 + ' (보스)' : gn0;
    const it = { t: spec.t, say: gn };
    if (spec.m !== undefined) it.v = Math.round(base * spec.m);
    if (spec.p !== undefined) it.v = Math.round(e.hp * spec.p);
    if (spec.v !== undefined) it.v = spec.v;
    if (spec.n !== undefined) it.n = spec.n;
    if (spec.id !== undefined) it.id = spec.id === '*' ? e.counters?.[0]?.name : spec.id;
    if (spec.to !== undefined) it.to = spec.to;
    if (spec.t === 'count' && !(e.counters || []).some(c => c.name === it.id)) console.log('쌓이는 수치 없음', h.id, it.id);
    e.passives.push({ name: gn, on, ...opt, do: it });
    const tag = ' 보스 기믹 — ';
    e.blurb = (e.blurb || '').split(tag)[0] + `${tag}HP 가 절반 아래로 내려가면 「${kw}」 본기로 공격이 세집니다 · ${line}.`;
    const i = cur.enemies.findIndex(x => x.id === e.id);
    if (i >= 0) { cur.enemies[i] = e; n++; }
  }
  const out = path.join(OUT, f), tmp = `${out}.tmp${process.pid}`;
  fs.writeFileSync(tmp, JSON.stringify(cur, null, 2) + '\n', 'utf8'); fs.renameSync(tmp, out);
}
console.log('보스 기믹', n, '클론 ·', `1층 몸 ×${F1} · 2층 몸 ×${F2} · 피해 ×${DM}`);
