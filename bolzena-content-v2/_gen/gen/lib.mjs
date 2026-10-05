// 마을 데이터 생성 도우미 — 수 · 패시브 · 적 · 상태 카드를 짧게 쓴다.
export const A = (v, say, o = {}) => ({ t: "attack", v, say, ...o });
export const BK = (v, say, o = {}) => ({ t: "back", v, say, ...o });
export const AL = (v, say, o = {}) => ({ t: "attackAll", v, say, ...o });
export const MU = (v, n, say, o = {}) => ({ t: "multi", v, n, say, ...o });
export const BL = (v, say, o = {}) => ({ t: "block", v, say, ...o });
export const GU = (v, say, o = {}) => ({ t: "guard", v, say, ...o });
export const HE = (v, say, o = {}) => ({ t: "heal", v, say, ...o });
export const BF = (id, v, say, o = {}) => ({ t: "buff", id, v, say, ...o });
export const DB = (id, v, say, o = {}) => ({ t: "debuff", id, v, say, ...o });
export const JM = (v, say, o = {}) => ({ t: "jam", v, say, ...o });
export const AC = (id, n, to, say, o = {}) => ({ t: "addCard", id, n, to, say, ...o });
export const CH = (say, next, o = {}) => ({ t: "charge", say, next, ...o });
export const P = (name, on, d, o = {}) => ({ name, on, ...o, do: d });

const NAT = { naive: "순수", mad: "광기", cool: "냉정", gloomy: "우울", jolly: "활발" };

/** 적 하나. 그림 키(art)는 따로 모은다 — 엔진 EnemyDef 에 칸이 없어서(NEEDS.md). */
export function E(id, name, hp, row, nat, blurb, intents, o = {}, o2 = {}) {
  o = { ...o, ...o2 };
  const e = { id, name, hp, row };
  if (nat) e.nature = NAT[nat] ?? nat;
  if (o.weak) e.weak = o.weak;
  if (o.tough) e.tough = o.tough;
  if (o.boss) e.boss = true;
  e.pick = o.pick ?? (o.boss ? "cycle" : "shuffle");
  e.blurb = blurb;
  if (o.open) e.open = o.open;
  e.intents = intents;
  if (o.phase) e.phase = o.phase;
  if (o.phase2) e.phase2 = o.phase2;
  if (o.passives) e.passives = o.passives;
  const art = o.art ?? artOf(id);
  return { e, art };
}

const NR_ICON = { tanker: "tanker", warrior: "dealer", archer: "wizard", supporter: "supporter" };
const NR_SKIN = { fairy: "Skin_Fairy", elf: "Skin_Elf", witch: "Skin_Witch", furry: "Skin_Furry", spirit: "Skin_Spirit", ghost: "Skin_Ghost", dragon: "Skin_Dragon" };

/** id 꼴에서 그림 키를 읽는다 — `<그림>_<성격>` · `nururing<역할>_<종족>`. */
export function artOf(id) {
  let m = id.match(/^nururing(tanker|warrior|archer|supporter)_(\w+)$/);
  if (m) return { spine: `monsterspine/nururing${m[1]}`, skin: NR_SKIN[m[2]], icon: `icon_${m[2]}curseddoll${NR_ICON[m[1]]}` };
  m = id.match(/^([a-z0-9]+)_(naive|mad|cool|gloomy|jolly)$/);
  if (m) return { spine: `monsterspine/${m[1]}`, skin: m[2], icon: `icon_${m[1]}${m[2]}` };
  throw new Error("그림 키를 모른다: " + id);
}

export const clone = (en, ko, scale) => ({ spine: `spine/ingame/${ko}`, skin: "default", icon: `icon_monster${en}`, ...(scale ? { scale } : {}) });

/** 상태 카드 */
export function ST(id, name, cost, tags, blurb, fx) {
  const c = { id, name, cost, type: "상태", tags, blurb };
  if (fx) c.fx = fx;
  return c;
}
export const onDraw = (...fx) => [{ k: "when", on: "draw" }, ...fx];
export const onHandEnd = (...fx) => [{ k: "when", on: "handEnd" }, ...fx];
export const partySt = (id, v) => ({ k: "status", id, v, target: "party" });

export function bundle(village, list, cards) {
  const enemies = list.map(x => x.e);
  const art = Object.fromEntries(list.map(x => [x.e.id, x.art]));
  // 쓰지 않는 적 · 없는 적을 잡는다
  const used = new Set();
  for (const f of village.floors) {
    for (const t of f.pools) for (const p of t) p.forEach(id => used.add(id));
    for (const p of f.elites) p.forEach(id => used.add(id));
    f.boss.forEach(id => used.add(id));
  }
  const ids = new Set(enemies.map(e => e.id));
  for (const id of used) if (!ids.has(id)) throw new Error(`${village.id}: 없는 적 ${id}`);
  for (const id of ids) if (!used.has(id)) throw new Error(`${village.id}: 안 쓰는 적 ${id}`);
  const data = { villages: [village], enemies }; if (cards?.length) data.cards = cards;
  return { data, art };
}

const HIT = new Set(["attack", "back", "attackAll", "multi", "thorns"]);
const r5 = x => Math.max(5, Math.round(x / 5) * 5);
function scaleIntent(it, dm) {
  if (!it) return;
  if (HIT.has(it.t)) it.v = r5(it.v * dm);
  scaleIntent(it.next, dm);
}
/** 체력 · 치는 수를 한꺼번에 깎는다(층 눈금 맞추기). */
export function tune(list, ids, hm, dm) {
  for (const x of list) {
    if (!ids.includes(x.e.id)) continue;
    const e = x.e; e.hp = Math.round(e.hp * hm / 10) * 10;
    for (const it of [e.open, ...e.intents, ...(e.phase?.intents ?? []), ...(e.phase2?.intents ?? []), ...(e.passives ?? []).map(p => p.do)]) scaleIntent(it, dm);
  }
}
