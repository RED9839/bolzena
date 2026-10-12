// 엘다인 검사 — node check.mjs 사도키,사도키|all [--strip] [--all-text]
// 1) out/heroes 의 그 사도 JSON 을 세계와 함께 bz check(오류 · 주의 — 그 사도 줄만)
// 2) bz hero 글에서 길이(고유 · 신탁 45 · 강화 · 생성 · 변신 카드 55, 공백 뺀 글자) · 같은 글 신탁
// 3) JSON 에서 % 단위(10% — 20% 미만은 5%) · 기본 카드 한 줄
// --strip: 엔진에 아직 없는 칸을 지운 사본으로 잰다(글 · 단위 미리 보기용 — 결과는 참고만)
import fs from 'fs';
import path from 'path';
import { execSync } from 'child_process';
const run = (args) => { try { return execSync(`"${BZ}" ${args}`, { encoding: 'utf8', maxBuffer: 1 << 26 }); } catch (e) { return (e.stdout || '') + (e.stderr || ''); } };
import { fileURLToPath } from 'url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../..');
const BZ = 'C:/projects/bolzena-core/Tools~/Dev/bz.cmd';
const OUT = path.join(HERE, 'out', 'heroes');
const TMP = path.join(HERE, 'out', '_tmp');
const strip = process.argv.includes('--strip'), allText = process.argv.includes('--all-text');
const list = process.argv[2];

const files = {};
for (const r of fs.readdirSync(OUT)) for (const f of fs.readdirSync(path.join(OUT, r))) files[f.replace(/\.json$/, '')] = path.join(OUT, r, f);
const ids = list === 'all' ? Object.keys(files) : list.split(',');

const FUTURE = { when: ['by', 'token', 'once'], oracle: ['type', 'countAs'], card: ['countAs'], form: ['turnMake', 'offExpire', 'offEnd'], fx: ['hitFx', 'carry', 'set', 'hitsPer', 'hitsAdd', 'loop', 'ofShield', 'token', 'all_'] };
function stripObj(o, kind) {
  if (Array.isArray(o)) return o.map(x => stripObj(x, kind));
  if (!o || typeof o !== 'object') return o;
  if (kind === 'fx' && ['spendShield', 'perShield', 'ifForm', 'formExtend', 'noForget'].includes(o.k)) return { k: 'cue', id: `미리:${o.k}` };
  const r = {};
  for (const [k, v] of Object.entries(o)) {
    if (kind && FUTURE[kind]?.includes(k)) continue;
    const kk = k === 'when' ? 'when' : k === 'oracles' ? 'oracle' : k === 'forms' ? 'form' : k === 'fx' || k === 'then' || k === 'off' ? 'fx' : null;
    r[k] = stripObj(v, kk ?? (kind === 'fx' ? 'fx' : null));
  }
  return r;
}

let bad = 0;
const pct = [];
for (const id of ids) {
  let file = files[id];
  if (!file) { console.log(`없음 ${id}`); bad++; continue; }
  const j = JSON.parse(fs.readFileSync(file, 'utf8'));
  // % 단위
  const walk = (o, at, cap) => {
    if (Array.isArray(o)) return o.forEach((x, i) => walk(x, `${at}[${i}]`, cap));
    if (!o || typeof o !== 'object') return;
    for (const [k, v] of Object.entries(o)) {
      if (o.stat === 'ultCost' || o.k === 'ultCostMod') continue;   // 고학년 게이지 증감 25% 칸(▶E78 — 엔진 검사기도 뺌)
      if (typeof v === 'number' && ['ratio', 'ofEvent'].includes(k) || (typeof v === 'number' && k === 'v' && Math.abs(v) < 1 && v !== 0)) {
        const p = Math.round(Math.abs(v) * 1000) / 10; // %
        const ok = p >= 20 ? Math.abs(p / 10 - Math.round(p / 10)) < 1e-6 : Math.abs(p / 5 - Math.round(p / 5)) < 1e-6;
        // 고유 효과 1개당 10% 미만은 1% 단위도 — 최대 겹을 채운 값이 10% 단위면(엔진 검사기 CapGrid · 10-12 밤 비비 수은 중독 8% × 5)
        const capOk = k === 'v' && at.includes('.per[') && cap >= 2 && p < 10 && Number.isInteger(p) && Number.isInteger(p * cap / 10);
        if (!ok && !capOk) pct.push(`${id} ${at}.${k}=${v}`);
      }
      walk(v, `${at}.${k}`, o.cap ?? cap);
    }
  };
  walk(j, id);
  if (strip) {
    fs.mkdirSync(TMP, { recursive: true });
    const s = stripObj(j, null);
    for (const c of s.cards) { delete c.countAs; for (const o of c.oracles || []) { delete o.type; delete o.countAs; } }
    for (const h of s.heroes) for (const f of h.forms || []) { delete f.turnMake; delete f.offExpire; delete f.offEnd; }
    file = path.join(TMP, `${id}.json`); fs.writeFileSync(file, JSON.stringify(s, null, 1));
  }
  const data = `${path.join(ROOT, 'world')};${file}`;
  const out = run(`check --data "${data}" --hero ${id}`);
  const lines = out.split(/\r?\n/);
  const head = lines[0];
  const errs = lines.filter(l => /^(오류|주의)/.test(l) && l.includes(id));
  if (/읽기 실패/.test(head)) { console.log(`■ ${id} 읽기 실패\n  ${lines.slice(1, 6).join('\n  ')}`); bad++; continue; }
  console.log(`■ ${id} — ${head}`);
  for (const l of errs) console.log('  ' + l);
  // 글 길이
  const hout = run(`hero ${id} --data "${data}"`);
  const items = []; let cur = null; let card = null;
  for (const l of hout.split(/\r?\n/)) {
    let m;
    if ((m = l.match(/^  (고유|시작|생성|변신판) 「([^」]+)」 \[[^\]]*\] (.*)$/))) { card = m[2]; cur = { n: m[2], t: m[1], s: m[3] }; items.push(cur); }
    else if ((m = l.match(/^  ─ 「([^」]+)」/))) { card = m[1]; cur = null; }
    else if ((m = l.match(/^    신탁(\d) 「[^」]*」 (.*)$/))) { cur = { n: `${card} 신탁${m[1]}`, t: '신탁', card, s: m[2] }; items.push(cur); }
    else if ((m = l.match(/^    축복 「[^」]*」 (.*)$/))) { cur = { n: `${card} 축복`, t: '축복', card, s: m[1] }; items.push(cur); }
    else if (/^  /.test(l)) cur = null;
    else if (cur && l.trim()) cur.s += ' ' + l.trim();
  }
  const len = s => s.replace(/\s+/g, '').length;
  let over = 0;
  for (const it of items) {
    if (it.t === '시작' || it.t === '축복') continue;
    const powerish = /강화 카드\.|^강화 ·|^\S+ · 강화/.test(it.s) || it.t === '생성' || it.t === '변신판';
    const body = it.s.replace(/^(공격|스킬|강화) · /, '').replace(/^강화 카드\.\s*/, '');
    const cap = powerish ? 55 : 45, n = len(body);
    if (n > cap) over++;
    if (allText || n > cap) console.log(`  ${n > cap ? '넘침' : '    '} ${it.n} ${n}/${cap} ${body}`);
  }
  // 같은 글 신탁(비용만 다른 것 포함 — 「비용 N.」 을 떼고 비교)
  const byCard = {};
  for (const it of items.filter(x => x.t === '신탁')) {
    const k = it.s.replace(/비용 \d\.\s*/, '').trim();
    (byCard[it.card] ??= {})[k] = (byCard[it.card][k] || 0) + 1;
  }
  let dup = 0;
  for (const [c, m] of Object.entries(byCard)) for (const [k, v] of Object.entries(m)) if (v > 1) { dup++; console.log(`  같은 글 신탁 ${c}: ${k}`); }
  console.log(`  글 넘침 ${over} · 같은 글 신탁 ${dup}`);
  if (over || dup || errs.some(l => l.startsWith('오류'))) bad++;
}
if (pct.length) { console.log(`% 단위 어긋남 ${pct.length}`); for (const p of pct) console.log('  ' + p); }
console.log(bad ? `문제 있는 사도 ${bad}` : '모두 통과');
