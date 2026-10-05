// G2 공용 — 원작 표 읽기 · 효과 조각 줄임말
const fs = require('fs');
const ROOT = 'C:/projects/bolzena-content-v2';
const REF = ROOT + '/_ref/원작_스펠_아티팩트.md';

// 원작 표(스펠 43 · 아티팩트 86)를 읽는다 — 이 표에 없는 것은 만들지 않는다.
function readRef() {
  const lines = fs.readFileSync(REF, 'utf8').split('\n');
  const spells = [], artifacts = [];
  for (const l of lines) {
    const c = l.split('|').map(s => s.trim());
    if (/SpellCardIcon_\d+\.webp/.test(l)) {
      spells.push({ name: c[1], grade: c[2], coin: +c[3], stat: c[4], orig: c[5], icon: c[6].replace('.webp', '') });
    } else if (/ArtifactIcon_\d+\.webp/.test(l)) {
      const name = c[1].replace(/\s*\[한국 전용\]/, '');
      const kind = c[3];
      const m = kind.match(/애착\((.+)\)/);
      artifacts.push({ name, grade: c[2], owner: m ? m[1] : null, coin: +c[4], stat: c[5], orig: c[6], icon: c[7].replace('.webp', ''), korea: /한국 전용/.test(c[1]) });
    }
  }
  return { spells, artifacts };
}

// 원작 추가 스탯(Lv.1 %) → 볼제나 능력치. 비율(공격 · HP · 방어 · 치명)은 원작 그대로, 크기는 등급마다 옛 장비 평균에 맞춘다(equips.js GRADE_VALUE).
function convertStats(str, statOnly, scale = 1) {
  const p = {};
  for (const m of str.matchAll(/([가-힣A-Za-z ]+?)\s*\+([\d.]+)%/g)) p[m[1].trim()] = +m[2];
  let atk = 0, hp = 0, def = 0, crit = 0;
  const pa = p['물리 공격력'] || 0, ma = p['마법 공격력'] || 0;
  atk += 0.9 * Math.max(pa, ma);
  atk += 0.7 * (p['공격 속도'] || 0);
  hp += 5 * (p['HP'] || 0);
  const pd = p['물리 방어력'] || 0, md = p['마법 방어력'] || 0;
  def += (pd && md) ? 0.65 * Math.max(pd, md) : 0.45 * Math.max(pd, md);
  crit += 0.4 * (p['치명타'] || 0) + 0.25 * (p['치명 피해'] || 0);
  const r1 = p['치명타 저항'] || 0, r2 = p['치명 피해 저항'] || 0;
  def += 0.25 * (r1 + r2); hp += 2 * (r1 + r2);
  const k = (statOnly ? 1.2 : 1) * scale;
  const o = {};
  // 한 칸이 튀지 않게 — 옛 장비의 가장 큰 값 언저리(HP 170 · 공격 32 · 방어 16 · 치명 10)에서 자른다
  const CAP = { hp: 170, atk: 32, def: 16, crit: 10 };
  const put = (key, v) => { v = Math.min(CAP[key], Math.round(v * k)); if (v > 0) o[key] = v; };
  put('hp', hp); put('atk', atk); put('def', def); put('crit', crit);
  return o;
}

// ── 효과 조각 ──
const S = (id, v, target) => target ? { k: 'status', id, v, target } : { k: 'status', id, v };
const E1 = (id, v) => S(id, v, 'oneEnemy');
const EA = (id, v) => S(id, v, 'allEnemies');
const ER = (id, v) => S(id, v, 'randomEnemy');
const D = v => ({ k: 'draw', v });
const AP = v => ({ k: 'ap', v });
const NC = v => ({ k: 'nextCheaper', v });
const G = v => ({ k: 'gauge', v });
const T = (v, target) => target ? { k: 'tough', v, target } : { k: 'tough', v };
const CL = v => ({ k: 'cleanse', v });
const RAND = pct => ({ k: 'ifRandom', pct });
const PAY = v => ({ k: 'payHp', v });
const SH = ratio => ({ k: 'shield', ratio });
const HEAL = ratio => ({ k: 'heal', ratio });
const EX = (ratio, target) => ({ k: 'extra', ratio, target: target || 'oneEnemy' });
const ST = (id, v, target) => target ? { k: 'stack', id, v, target } : { k: 'stack', id, v };
const SP = (id, v) => v === 'all' ? { k: 'spend', id, all: true } : { k: 'spend', id, v };
const MAKE = (id, v) => ({ k: 'make', id, v });
const MOD = (k, v, opt = {}) => Object.assign({ k, v }, opt);

const rule = (name, when, fx, opt = {}) => {
  const r = { name, when };
  if (opt.conds) r.conds = opt.conds;
  if (opt.limit) r.limit = opt.limit;
  r.fx = fx;
  return r;
};
const perTurn = n => ({ per: 'turn', n });
const perFight = n => ({ per: 'fight', n });

const clean = o => JSON.parse(JSON.stringify(o));
const writeJson = (file, obj) => fs.writeFileSync(file, JSON.stringify(clean(obj), null, 2) + '\n', 'utf8');

const statValue = s => (s.atk || 0) + (s.hp || 0) / 6 + (s.def || 0) * 1.5 + (s.crit || 0) * 2;
module.exports = { statValue, ROOT, readRef, convertStats, S, E1, EA, ER, D, AP, NC, G, T, CL, RAND, PAY, SH, HEAL, EX, ST, SP, MAKE, MOD, rule, perTurn, perFight, clean, writeJson };
