if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 3성 「깊이」 시범(2026-10-09) — 원본 heroes 에서 읽어 시범 사도를 다시 짓고 _gen/deep/heroes/ 에 쓴다(원본은 안 고친다).
//   node _gen/deep/build.mjs [사도 일부]
// 기준: _measure/깊이_기준/00_기준.md(고유 4장 = 시동 · 연료 · 결정타 · 원작 자유, 신탁 다섯 = 방향, 패시브 둘, 팀 고리 하나, 글 40자).
// 카드 id 는 남는 카드끼리 원본 그대로 둔다(그림 짝) — 빠진 카드는 지우고, 새 자리는 원본 id 하나를 이어 쓴다(시범 문서에 적음).
import fs from 'fs'; import path from 'path';
const SRC = process.env.DEEPSRC || 'C:/projects/_backup/heroes_villages_before_deep_20261009/heroes';   // 원본 반영(10-09 23:32) 뒤에는 백업에서 읽는다
const OUT = process.env.DEEPOUT || 'C:/projects/bolzena-content-v2/_gen/deep/heroes';
const TUNE = fs.existsSync(new URL('./tune.json', import.meta.url)) ? JSON.parse(fs.readFileSync(new URL('./tune.json', import.meta.url), 'utf8')) : {};

// ── 조각 ──
const E1 = 'oneEnemy', EA = 'allEnemies', ER = 'randomEnemy', TOP = 'topEnemy';
const dmg = (r, t = E1, o = {}) => ({ k: 'dmg', ratio: r, target: t, ...o });
const ddef = (r, t = E1, o = {}) => ({ k: 'dmg', ratio: r, base: 'def', target: t, ...o });
const hits = (n, r, t = ER) => ({ k: 'dmg', ratio: r, target: t, hits: n });
const sh = r => ({ k: 'shield', ratio: r });
const heal = r => ({ k: 'heal', ratio: r });
const st = (id, v, t) => (t ? { k: 'status', id, v, target: t } : { k: 'status', id, v });
const stk = (id, v, t) => (t ? { k: 'stack', id, v, target: t } : { k: 'stack', id, v });
const spendAll = id => ({ k: 'spend', id, all: true });
const spendN = (id, v) => ({ k: 'spend', id, v });
const per = (id, o = {}) => ({ k: 'perStack', id, ...o });
const perTag = id => ({ k: 'perTag', id });
const draw = (v, o = {}) => ({ k: 'draw', v, ...o });
const make = (id, v = 1, o = {}) => ({ k: 'make', id, v, ...o });
const ifStack = (id, n, o = {}) => ({ k: 'ifStack', id, n, ...o });
const notStack = (id, n = 1) => ({ k: 'ifStack', id, n, not: true });
const empower = (ratio, who) => (who ? { k: 'empower', ratio, who } : { k: 'empower', ratio });
const form = id => ({ k: 'form', id });
const formEnd = { k: 'formEnd' };
const tough = (v, t) => (t ? { k: 'tough', v, target: t } : { k: 'tough', v });
const power = (...rules) => ({ k: 'power', rules });
const rule = (on, fx, o = {}) => ({ when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), fx });
const pw = (on, fx, o = {}) => power(rule(on, fx, o));
const pas = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), fx });
const O = (fx, o = {}) => ({ ...(o.cost !== undefined ? { cost: o.cost } : {}), ...(o.tags ? { tags: o.tags } : {}), ...(o.power ? { power: true } : {}), fx });
const B = x => (typeof x === 'string' ? { kind: x } : Array.isArray(x) ? { fx: x } : x);
// 고유 카드 — oracles 다섯(O), blesses 셋(시동은 null → 공용 축복 풀)
function U(id, name, cost, type, fx, oracles, blesses, tags) {
  if (oracles.length !== 5) throw new Error(`${id}: 신탁 다섯이 아니다`);
  const c = { id, name, hero: id.replace(/_u\d+$/, ''), unique: true, cost, type, ...(tags && tags.length ? { tags } : {}), fx,
    oracles: oracles.map((o, i) => ({ name: `신탁 ${i + 1}`, ...(o.tags === undefined && tags ? { tags } : {}), ...o })) };
  if (blesses) c.blesses = blesses.map((b, i) => ({ name: `축복 ${i + 1}`, ...B(b) }));
  return c;
}
const tok = (id, name, hero, type, fx, o = {}) => ({ id, name, hero, token: true, cost: o.cost ?? 0, type, ...(o.tags ? { tags: o.tags } : {}), fx, ...(o.blurb ? { blurb: o.blurb } : {}) });
// 세기 맞춤 — 피해 · 실드 · 회복 · 추가 공격 배율만(변신판 · 생성 카드 포함)
function tune(j, m) {
  const R2 = x => Math.round(x * 100) / 100;
  const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal', 'extra'].includes(f.k) && f.ratio && !f.ofEvent) f.ratio = R2(f.ratio * m); if (f.k === 'power') for (const r of f.rules) mul(r.fx); if (f.then) mul(f.then); } };
  for (const c of j.cards) if (c.unique || c.token) { mul(c.fx); for (const o of c.oracles || []) mul(o.fx); for (const b of c.blesses || []) mul(b.fx); }
  const h = j.heroes[0];
  for (const k of [h.keyword, ...(h.keywords || [])].filter(Boolean)) for (const r of k.rules || []) mul(r.fx);
  for (const r of h.passives || []) mul(r.fx);
  for (const f of h.forms || []) for (const r of f.passives || []) mul(r.fx);
}
const keepCards = (j, ids) => { j.cards = j.cards.filter(c => ids.includes(c.id)); };
// 신탁 값어치 보정(loop.mjs 가 boost.json 을 채운다) — 그 신탁의 피해 · 실드 · 회복 · 추가 공격 · 다음 카드 강화 배율만
const BOOST = fs.existsSync(new URL('./boost.json', import.meta.url)) ? JSON.parse(fs.readFileSync(new URL('./boost.json', import.meta.url), 'utf8')) : {};
function boost(j) {
  const R2 = x => Math.round(x * 100) / 100;
  const mul = (fx, b) => { for (const f of fx || []) { if (['dmg', 'shield', 'heal', 'extra', 'empower'].includes(f.k) && f.ratio && !f.ofEvent) f.ratio = R2(f.ratio * b); if (f.k === 'power') for (const r of f.rules) mul(r.fx, b); if (f.then) mul(f.then, b); } };
  for (const c of j.cards) (c.oracles || []).forEach((o, i) => { const b = BOOST[`${c.id}|${i + 1}`]; if (b) mul(o.fx, b); });
  for (const c of j.cards) { const b = BOOST[`${c.id}|0`] || (c.token ? BOOST[`FORM:${c.name}`] : 0); if (b) mul(c.fx, b); }   // 변신판 · 생성 카드 본문
}

// ════════════════════════════════════════════════════════════════════
// 1. 디아나(왕년) — 두 얼굴형 · 딜러(엘다인 — 클론 없음)
//    엔진: 도끼로 백수공권을 모아 → 「도끼 부수기」로 털며 맨주먹 → 맨주먹 주먹질(동료 공격 포함)로 연격 → 「도끼 되찾기」(찢기의 변신판)로 연격을 광역으로 쏟고 도끼로
// ════════════════════════════════════════════════════════════════════
function diana(j) {
  const H = '디아나_왕년', h = j.heroes[0], K = '백수공권', C = '연격', F = '디아나_왕년_맨주먹';
  h.keyword = { name: K, desc: '도끼를 쥐고 모으는 기세', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.05 }] };
  h.keywords = [{ name: C, desc: '맨주먹으로 잇는 주먹질', carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.06 }] }];
  h.passives = [
    pas('단순하게', 'play', [{ k: 'ifHand', n: 1 }, stk(K, 1)], { when: { type: '공격' } }),
    pas('휴식이 필요해', 'lowHp', [stk(K, 4), st('피해 감소', 2)], { when: { pct: 0.3 } }),
  ];
  h.forms = [{
    id: F, name: '맨주먹', desc: '도끼를 부순 맨주먹 — 주먹질이 이어질수록 세지고, 잃은 HP를 조금씩 되찾음', turns: 4,
    cards: { [`${H}_s1`]: `${H}_f1`, [`${H}_u1`]: `${H}_f2` },
    passives: [
      { name: '주먹이 이어진다', when: { on: 'play', type: '공격' }, fx: [stk(C, 1)] },
      { name: '주먹이 이어진다', when: { on: 'play', type: '공격', who: 'other' }, fx: [stk(C, 1)] },
      { name: '되찾는 숨', when: { on: 'turnEnd' }, fx: [heal(0.15)] },
    ],
    off: [stk(K, 2)], skin: null, anim: 'Idle_Change',
  }];
  h.ult = { name: '부수기', cost: 250, fx: [dmg(1.6, EA), form(F)] };
  h.starter = [`${H}_s1`, `${H}_u3`, `${H}_s2`, `${H}_s3`];
  const base = j.cards.filter(c => [`${H}_s1`, `${H}_s2`, `${H}_s3`].includes(c.id));
  const f1 = tok(`${H}_f1`, '맨주먹 연타', H, '공격', [dmg(0.6, E1, { hits: 2 })], { cost: 1, blurb: '도끼 없이 주먹으로 — 맨주먹 전성기의 기본 공격' });
  const f2 = tok(`${H}_f2`, '도끼 되찾기', H, '공격', [per(C, { n: 1 }), dmg(0.55, EA), spendAll(C), formEnd], { cost: 1, blurb: '쌓아 둔 주먹질을 땅에 쏟고 다시 도끼를 쥔다 — 맨주먹 전성기의 「찢기」' });
  const B3 = [stk(K, 1)];
  const u3 = U(`${H}_u3`, '거르지 않는 수련', 1, '강화', [stk(K, 1), pw('turnStart', [stk(K, 1)])], [
    O([pw('turnStart', [stk(K, 1), stk(C, 1)])], { tags: ['개전'] }),                         // 두 얼굴 다 굴림
    O([pw('discard', [stk(K, 1)])], { cost: 0, tags: ['개전'] }),                           // 버리기 덱(찢기 ④와)
    O([stk(K, 2), pw('hurt', [stk(K, 1)], { when: { guarded: true } })], { tags: ['개전'] }), // 탱커와 — 맞으며 배운다
    O([stk(K, 2), pw('turnStart', [stk(K, 1)])], { tags: ['개전'] }),                         // 수치
    O([power(rule('turnStart', [stk(K, 1)]), rule('spend', [draw(2)], { when: { id: K } }))], { tags: ['개전'] }), // 부수면 드로우
  ], null, ['개전']);
  const u1 = U(`${H}_u1`, '찢기', 1, '공격', [dmg(0.7, EA), st('균열', 2, EA), stk(K, 1)], [
    O([dmg(0.95, EA), st('균열', 2, EA), stk(K, 1)]),                                         // 수치
    O([dmg(0.5, EA), st('균열', 2, EA), pw('turnStart', [st('균열', 1, EA)])], { power: true }), // 균열 운영
    O([dmg(1.5, E1), tough(1), stk(K, 1)]),                                                   // 한 놈 · 격파
    O([{ k: 'discard', all: true }, { k: 'perDiscarded' }, dmg(0.9, EA), stk(K, 1)]),        // 손 털기(단순하게)
    O([dmg(1.3, EA), st('균열', 2, EA), form(F)], { cost: 2 }),                              // 찢으며 바로 맨주먹
  ], [B('power'), B('draw'), B(B3)]);
  const u2 = U(`${H}_u2`, '도끼 부수기', 1, '공격', [per(K), dmg(0.55, E1), spendAll(K), form(F)], [
    O([per(K), dmg(0.75, E1), spendAll(K), form(F)]),                               // 수치
    O([per(K), dmg(0.4, EA), spendAll(K), form(F)]),                                // 광역
    O([per(K), dmg(0.55, E1), tough(1), form(F)]),                                  // 털지 않고 들어감(백수공권 유지)
    O([per(K), dmg(0.5, E1), form(F), pw('turnStart', [stk(C, 1)])], { power: true }), // 매 턴 연격
    O([form(F), stk(C, 2)], { cost: 0 }),                                                     // 빠른 변신
  ], [B('power'), B('cost'), B(B3)]);
  const u4 = U(`${H}_u4`, '바위 던지기', 2, '공격', [dmg(1.0, EA), st('기절', 1, TOP)], [
    O([dmg(1.3, EA), st('기절', 1, TOP)]),                                                    // 수치
    O([dmg(1.1, E1), st('기절', 1, E1)], { cost: 1 }),                                        // 한 놈 · 1코
    O([per(C, { n: 1 }), dmg(0.3, EA), st('기절', 1, TOP)]),                                  // 맨주먹으로 던지기(연격 비례)
    O([dmg(1.15, EA), pw('hurt', [dmg(0.6, ER)], { when: { guarded: true } })], { power: true }), // 맞으면 던진다(원작 강화 공격)
    O([dmg(1.0, EA), st('둔화', 1, EA), stk(K, 2)]),                                          // 도끼 쪽 연료
  ], [B('power'), B({ tags: ['보존'] }), B(B3)]);
  j.cards = [...base, f1, f2, u3, u1, u2, u4];
}

// ════════════════════════════════════════════════════════════════════
// 2. 에르핀 — 손 만들기형 · 딜러
//    엔진: 케이크를 손에 만들어 두고 — 먹으면 배부름(결계 부수기의 연료), 안 먹으면 마력탄 폭주의 탄알. 배가 비었을 땐 예민해 다음 카드가 세고, 배부름을 쓰면 남은 케이크를 동료에게 나눈다
// ════════════════════════════════════════════════════════════════════
function erpin(j) {
  const H = '에르핀', h = j.heroes[0], K = '배부름', CK = '에르핀_cake', BT = '에르핀_bite';
  h.keyword = { name: K, desc: '케이크로 채운 여왕의 배', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.05 }] };
  delete h.keywords;
  h.passives = [
    pas('달달한 게 최고야!', 'play', [make(CK, 1)], { when: { type: '공격' } }),
    pas('순수 케이크 공격!!!', 'spend', [make(BT, 1, { owner: 'other' })], { when: { id: K } }),
  ];
  h.starter = [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`];
  const keep = j.cards.filter(c => [`${H}_s1`, `${H}_s2`, `${H}_s3`, CK].includes(c.id));
  const bite = tok(BT, '한 입 케이크', H, '스킬', [heal(0.3), empower(0.3)], { tags: ['소멸'], blurb: '여왕이 먹다 남긴 한 입 — 받은 동료의 다음 카드가 힘을 낸다' });
  const u2 = U(`${H}_u2`, '무한의 케이크', 1, '스킬', [make(CK, 2), draw(1)], [
    O([make(CK, 3), draw(1)]),                                                     // 수치
    O([make(CK, 1), draw(1), pw('turnStart', [make(CK, 1)])], { power: true }),            // 매 턴 케이크
    O([make(BT, 2, { owner: 'other' }), draw(2)]),                                 // 동료 몫
    O([stk(K, 2), draw(1)], { cost: 0 }),                                          // 바로 먹기(결계 부수기 덱)
    O([make(CK, 4, { to: 'draw' })]),                                   // 굶은 채 버티기
  ], null);
  const u5 = U(`${H}_u5`, '배고픈 여왕', 0, '스킬', [make(CK, 1), draw(1), notStack(K), empower(0.4)], [
    O([make(CK, 2), draw(1), notStack(K), empower(0.5)]),                          // 수치
    O([make(CK, 1), draw(1), pw('turnStart', [empower(0.5)], { conds: [{ c: 'stack', id: K, n: 1, not: true }] })], { power: true }), // 굶주린 눈빛(매 턴)
    O([stk(K, 2), draw(2), heal(0.6)]),                                                       // 배부른 낮잠
    O([make(BT, 2, { owner: 'other' }), notStack(K), empower(0.4)]),      // 동료와 나눠 먹기
    O([make(CK, 2), notStack(K), empower(0.3)]),                                   // 탄알 장전
  ], [B('draw'), B('ap'), B([make(CK, 1)])]);
  const u1 = U(`${H}_u1`, '마력탄 폭주', 1, '공격', [perTag(CK), dmg(0.8, ER), { k: 'exileFrom', from: 'hand', all: true, tag: CK }], [
    O([perTag(CK), dmg(1.0, ER), { k: 'exileFrom', from: 'hand', all: true, tag: CK }]), // 수치
    O([perTag(CK), dmg(0.55, EA), { k: 'exileFrom', from: 'hand', all: true, tag: CK }]),                                 // 케이크는 남긴다
    O([perTag(CK), dmg(0.95, E1), { k: 'exileFrom', from: 'hand', all: true, tag: CK }]), // 한 놈에게 다
    O([hits(3, 0.6), make(CK, 1), pw('make', [dmg(0.6, ER)])], { power: true }),               // 만들 때마다 쏜다
    O([perTag(CK), dmg(0.8, E1), { k: 'exileFrom', from: 'hand', all: true, tag: CK }, stk(K, 2)]), // 쏘면서 먹기
  ], [B('power'), B('draw'), B([make(CK, 1)])]);
  const u4 = U(`${H}_u4`, '맨주먹 결계 부수기', 2, '공격', [dmg(2.13, E1), per(K), dmg(0.53, E1), spendAll(K)], [
    O([dmg(2.8, E1), per(K), dmg(0.7, E1), spendAll(K)]),                          // 수치
    O([dmg(1.6, EA), per(K), dmg(0.4, EA), spendAll(K)]),                          // 광역
    O([dmg(1.8, E1), per(K), dmg(0.45, E1)]),                                      // 배는 그대로
    O([per(K, { n: 1 }), dmg(0.75, E1), spendAll(K)], { cost: 1 }),                // 한입 펀치(1코)
    O([per(K, { n: 1 }), dmg(0.95, E1), spendAll(K), st('피해 감소', 2)]),          // 무적 돌격(원작 고학년)
  ], [B('power'), B('weakSpot'), B([make(CK, 1)])], ['분쇄']);
  j.cards = [...keep, bite, u2, u5, u1, u4];
}

// ════════════════════════════════════════════════════════════════════
// 3. 버터 — 쌓고 고르기 · 딜러
//    엔진: 파티가 맞거나 동료가 부탁(스킬)할 때마다 옐로카드를 참고 → 넷이면 「레드카드」로 2턴 웃음기 없는 버터(짱돌이 총으로) → 풀리면 0(방전)
// ════════════════════════════════════════════════════════════════════
function butter(j) {
  const H = '버터', h = j.heroes[0], K = '옐로카드', F = '버터_퇴장';
  h.keyword = { name: K, desc: '꾹 참는 부당한 대우', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.08 }], onMax: { make: `${H}_red`, consume: true } };
  h.passives = [
    pas('참다 참다', 'hurt', [stk(K, 1)], { when: { guarded: true } }),
    pas('심부름 목록', 'play', [stk(K, 1)], { when: { type: '스킬', who: 'other' } }),
  ];
  h.forms = [{
    id: F, name: '참을 만큼 참았다', desc: '웃음기를 거둔 버터 — 짱돌 대신 앞치마 속 총', turns: 2,
    cards: { [`${H}_s1`]: `${H}_f1` },
    bonus: [{ type: '공격', ratio: 1.25 }],
    off: [{ k: 'nextCheaper', v: 1 }], skin: null, anim: null,
  }];
  h.ult = { name: '새쭁! 쓔튜라이크!', cost: 250, fx: [dmg(2.3, EA), st('기절', 1), stk(K, 2)] };
  h.starter = [`${H}_s1`, `${H}_u2`, `${H}_s2`, `${H}_s3`];
  const keep = j.cards.filter(c => [`${H}_s1`, `${H}_s2`, `${H}_s3`].includes(c.id));
  const red = tok(`${H}_red`, '레드카드', H, '공격', [dmg(0.6, E1, { hits: 2 }), form(F)], { tags: ['소멸'], blurb: '참을 만큼 참았다 — 꺼내 들면 2턴 동안 웃음기 없는 버터' });
  const f1 = tok(`${H}_f1`, '앞치마 총', H, '공격', [dmg(0.75, E1, { hits: 2 }), st('취약', 1, E1)], { cost: 1, blurb: '참고 참던 골든 리트리버의 총 — 참을 만큼 참았다의 기본 공격' });
  const B3 = [stk(K, 1)];
  const u2 = U(`${H}_u2`, '부탁은 거절 안 해', 1, '스킬', [sh(1.0), stk(K, 2), draw(1)], [
    O([sh(1.35), stk(K, 2), draw(1)]),                                             // 수치
    O([sh(0.8), stk(K, 1), pw('play', [sh(0.5), stk(K, 1)], { when: { type: '스킬', who: 'other' } })], { power: true }), // 부탁마다 막아 줌(팀)
    O([sh(1.7), st('피해 감소', 1), stk(K, 1)]),                                    // 대신 맞기(버팀)
    O([stk(K, 2), draw(1)], { cost: 0 }),                                          // 빨리 참기
    O([draw(2, { who: 'other' }), stk(K, 2), sh(0.8)]),                                     // 동료 부탁 먼저(동료 카드 드로우)
  ], null);
  const u1 = U(`${H}_u1`, '버터 플라이!', 1, '공격', [hits(4, 0.35), stk(K, 1)], [
    O([hits(4, 0.46), stk(K, 1)]),                                                 // 수치
    O([per(K, { n: 1 }), dmg(0.65, ER)]),                                           // 학년마다 타수(옐로카드 수만큼)
    O([dmg(0.36, E1, { hits: 4 }), stk(K, 1)]),                                    // 한 놈만
    O([hits(3, 0.3), pw('hurt', [dmg(0.3, ER)], { when: { guarded: true } })], { power: true }), // 맞으면 새총(애착)
    O([hits(2, 0.38), stk(K, 2)], { cost: 0 }),                                    // 꼬리 쫓기
  ], [B('power'), B('weakSpot'), B(B3)]);
  const u4 = U(`${H}_u4`, '에이프런 매그넘', 2, '공격', [dmg(1.7, E1), per(K), dmg(0.4, E1), spendAll(K)], [
    O([dmg(2.2, E1), per(K), dmg(0.5, E1), spendAll(K)]),                          // 수치
    O([dmg(1.2, EA), per(K), dmg(0.28, EA), spendAll(K)]),                         // 총알 세례(광역)
    O([dmg(1.5, E1), per(K), dmg(0.35, E1)]),                                      // 겨누기만(옐로카드는 둔다)
    O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)], { cost: 1 }),                 // 빠른 뽑기(1코)
    O([per(K, { n: 1 }), dmg(0.55, E1), spendAll(K), st('기절', 1, E1)]),          // 퇴장 명령(기절)
  ], [B('power'), B('ap'), B(B3)]);
  const u3 = U(`${H}_u3`, '구덩이 파기', 1, '공격', [dmg(0.75, E1), per(K), dmg(0.22, E1)], [
    O([dmg(1.0, E1), per(K), dmg(0.3, E1)]),                                       // 수치
    O([dmg(1.0, E1), { k: 'when', on: 'handEnd' }, stk(K, 2)]),                    // 묻어 둔 사료(쥐고 넘기면)
    O([dmg(0.55, EA), per(K), dmg(0.16, EA)]),                                     // 넓게 판 구덩이
    O([dmg(0.9, E1), st('약화', 2, E1), stk(K, 1)]),                                                     // 사료 꺼내 먹기(회복)
    O([dmg(0.75, E1), pw('turnEnd', [stk(K, 1), dmg(0.3, ER)])], { power: true }),                // 매일 판다
  ], [B('power'), B('frost'), B(B3)], ['보존']);
  j.cards = [...keep, red, f1, u2, u1, u4, u3];
}

// ════════════════════════════════════════════════════════════════════
// 4. 시온 더 다크불릿 — 표적형 · 딜러(엘다인 — 클론 없음)
//    엔진: 공격마다 마탄(검은 탄창)을 재우고 → 다섯 발이면 다음 카드가 세진다 → 마.탄.의.사.수가 장탄 수만큼 좌표 찍은 적을 연사. 동료가 좌표 적을 치면 장전을 돕는다
// ════════════════════════════════════════════════════════════════════
function sion(j) {
  const H = '시온더다크불릿', h = j.heroes[0], K = '검은 탄창', M = '좌표', T = `${H}_t1`;
  h.keyword = { name: K, desc: '어둠의 힘으로 장전한 마탄', carrier: 'self', cap: 5, per: [{ stat: 'atk', v: 0.03 }], onMax: { empower: 'next', ratio: 0.5 } };
  h.passives = [
    pas('큭큭, 봉인 해제', 'play', [stk(K, 1)], { when: { type: '공격' } }),
    pas('화합', 'hit', [{ k: 'ifHunted' }, stk(K, 1)], { when: { who: 'other' } }),
  ];
  h.starter = [`${H}_s1`, `${H}_u3`, `${H}_s3`, `${H}_s2`];
  const keep = j.cards.filter(c => [`${H}_s1`, `${H}_s2`, `${H}_s3`, T].includes(c.id));
  const B3 = [stk(K, 1)];
  const u3 = U(`${H}_u3`, '좌표 잡기', 0, '스킬', [stk(M, 1, E1), stk(K, 1), draw(1)], [
    O([stk(M, 2, E1), stk(K, 2), draw(1)]),                                        // 수치
    O([stk(M, 1, E1), draw(1), pw('huntDown', [make(T, 1)])], { power: true }),             // 찍은 적이 쓰러지면 마탄
    O([stk(M, 1, 'farEnemy'), stk(K, 1), draw(2)]),                                                  // 가장 먼 적(원작 저격 대상 — 엔진 farEnemy 10-09)
    O([stk(K, 3), draw(1)]),                                                       // 좌표 없이 장전만
    O([stk(M, 1, E1), st('취약', 2, E1), draw(1)]),                              // 동료를 위한 좌표(취약)
  ], null);
  const u2 = U(`${H}_u2`, '디 엑시트로', 1, '공격', [dmg(0.7, E1), stk(K, 1), { k: 'ifHunted' }, stk(K, 1)], [
    O([dmg(0.95, E1), stk(K, 1), { k: 'ifHunted' }, stk(K, 1)]),                   // 수치
    O([dmg(0.5, EA), stk(K, 2)]),                                                  // 흩뿌리기
    O([dmg(0.6, E1), make(T, 1)]),                                                 // 마탄 한 발
    O([dmg(0.8, E1), pw('turnStart', [stk(K, 1)])], { power: true }),              // 매 턴 장전
    O([dmg(1.6, E1), stk(K, 2), tough(1)], { cost: 2 }),       // 관통 저격
  ], [B('power'), B('weakSpot'), B(B3)]);
  const u1 = U(`${H}_u1`, '마.탄.의.사.수', 1, '공격', [per(K, { n: 1 }), dmg(0.45, E1), spendAll(K)], [
    O([per(K, { n: 1 }), dmg(0.6, E1), spendAll(K)]),                              // 수치
    O([per(K, { n: 1 }), dmg(0.58, ER), spendAll(K)]),                             // 흩뿌리기(무작위 적)
    O([per(K, { n: 1 }), dmg(0.42, E1), spendAll(K), stk(M, 1, E1)]),                            // 두 발 남기기
    O([per(K, { n: 1 }), dmg(0.4, E1), spendAll(K), { k: 'ifKill' }, stk(K, 3)]),  // 처치하면 재장전
    O([per(K, { n: 1 }), dmg(0.72, E1), spendAll(K), st('취약', 2, E1)], { cost: 2 }), // 종말 직전
  ], [B('power'), B('cost'), B(B3)]);
  const u5 = U(`${H}_u5`, '원 플러스 원', 1, '스킬', [make(T, 2), sh(0.3)], [
    O([make(T, 3), sh(0.3)]),                                                     // 수치
    O([make(T, 2), pw('play', [stk(K, 1), dmg(0.35, ER)], { when: { type: '공격', maxCost: 0 } })], { power: true }), // 진혼의 탄환(0코 공격마다 장전)
    O([make(T, 2), make(T, 1, { owner: 'other' })]),                               // 동생 몫(동료가 쏘는 마탄)
    O([make(T, 1), draw(1)], { cost: 0 }),                                         // 유통기한 임박
    O([make(T, 4), { k: 'nextAp', v: -1 }]),                                       // 폐기 처분(다음 턴 AP)
  ], [B('draw'), B('cost'), B(B3)]);
  j.cards = [...keep, u3, u2, u1, u5];
}

// ════════════════════════════════════════════════════════════════════
// 5. 아멜리아 — 회복형 · 딜러
//    엔진: 파티 HP 를 70% 넘게 채울 때마다 결재 서류가 올라오고 → 감봉 통보로 한 번에 처리(충격 걸린 적은 약화) · 위성 폭격으로 충격을 깐다. 그냥 승인으로 동료 카드를 싸게
// ════════════════════════════════════════════════════════════════════
function amelia(j) {
  const H = '아멜리아', h = j.heroes[0], K = '결재 서류';
  h.starter = [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u3`];
  const keep = j.cards.filter(c => [`${H}_s1`, `${H}_s2`].includes(c.id));
  const B3 = [stk(K, 1)];
  const u3 = U(`${H}_u3`, '모나티엄 행정 대행', 0, '스킬', [heal(0.59), stk(K, 1), draw(1)], [
    O([heal(0.9), stk(K, 1), draw(1)]),                                            // 수치
    O([stk(K, 1), draw(1), pw('overheal', [dmg(1, E1, { ofEvent: 1 })], { when: { pct: 0.8 } })], { power: true }), // 감시카메라 32대
    O([stk(K, 2), empower(0.4), draw(1)]),                               // 야근
    O([heal(0.7), draw(2, { who: 'other' }), stk(K, 1)]),                                     // 업무 분담(동료 카드)
    O([stk(K, 1), draw(1), st('충격', 2, E1)]),                                    // 감시 보고
  ], null);
  const u1 = U(`${H}_u1`, '새틀라이트 전술폭격', 1, '공격', [dmg(0.8, EA), st('충격', 1, EA), stk(K, 1)], [
    O([dmg(1.05, EA), st('충격', 1, EA), stk(K, 1)]),                              // 수치
    O([{ k: 'later', n: 1, then: [dmg(1.3, EA), st('충격', 1, EA)] }, stk(K, 1)]),  // 신호탄 먼저(1턴 뒤 폭격)
    O([dmg(0.27, EA, { hits: 4 }), st('충격', 1, EA)]),                             // Mk.2 4연속
    O([dmg(0.9, EA), st('충격', 1, EA), pw('overheal', [dmg(0.6, EA)], { when: { pct: 0.8 } })], { power: true }), // 넘친 회복이 폭격으로
    O([dmg(1.4, E1), st('충격', 2, E1)]),                                          // 정밀 폭격
  ], [B('power'), B('draw'), B(B3)]);
  const u2 = U(`${H}_u2`, '감봉 통보', 1, '공격', [per(K, { n: 1 }), dmg(0.55, E1), spendAll(K), { k: 'ifFoe', id: '충격' }, st('약화', 2, E1)], [
    O([per(K, { n: 1 }), dmg(0.75, E1), spendAll(K), { k: 'ifFoe', id: '충격' }, st('약화', 2, E1)]), // 수치
    O([per(K, { n: 1 }), dmg(0.38, EA), spendAll(K)]),                              // 전 직원 감봉
    O([per(K, { n: 1 }), dmg(0.5, E1), st('약화', 1, E1)]),                        // 경고만(서류는 둔다)
    O([per(K, { n: 1 }), sh(0.45), spendAll(K)], { cost: 0 }),                     // 결재 반려(서류로 막기)
    O([per(K, { n: 1 }), dmg(0.42, E1), spendAll(K), pw('spend', [draw(1)], { when: { id: K } })], { power: true }), // 결재할 때마다 드로우
  ], [B('power'), B('ap'), B(B3)]);
  const u5 = U(`${H}_u5`, '그냥 승인', 0, '스킬', [heal(0.52), { k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'other' }], [
    O([heal(0.8), { k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'other' }]),           // 수치
    O([heal(0.6), { k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 2, who: 'other' }, draw(2)], { cost: 1 }), // 일괄 승인
    O([stk(K, 1), { k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'other' }]),          // 서명만
    O([{ k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'other' }, pw('play', [st('충격', 1, E1)], { when: { type: '공격', who: 'other' } })], { power: true }), // 감시 체계
    O([heal(0.45), { k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'other' }, draw(1)], { tags: ['신속'] }), // 즉결
  ], [B('draw'), B('heal'), B(B3)]);
  j.cards = [...keep, u3, u1, u2, u5];
}

// ════════════════════════════════════════════════════════════════════
// 6. 벨라 — 버티형 · 탱커(엘다인 — 클론 없음)
//    엔진: 막아 낸 만큼 존속(불꽃)을 모아 → 서툰 챙김으로 파티를 치유하거나 불씨를 동료 다음 카드에 → 영속(끈기)이 남아 있는 동안 파티 피해 +, 끈기로 버티면 불꽃이 다시 타오른다
// ════════════════════════════════════════════════════════════════════
function bella(j) {
  const H = '벨라', h = j.heroes[0], K = '존속';
  h.passives = [
    pas('다시금 타오르는 존재', 'guardSum', [stk(K, 1, undefined)].map(f => ({ ...f, ofEvent: 0.02 }))),
    pas('우린 이미 완전한 존재들', 'always', [{ k: 'dealtMod', v: 0.15, target: 'allAllies', run: true }], { conds: [{ c: 'status', id: '끈기', n: 1 }] }),
  ];
  h.starter = [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`];
  const keep = j.cards.filter(c => [`${H}_s1`, `${H}_s2`].includes(c.id));
  const B3 = [stk(K, 1)];
  const u1 = U(`${H}_u1`, '경계선상의 유령', 1, '공격', [ddef(0.24, EA), sh(0.72), stk(K, 1)], [
    O([ddef(0.32, EA), sh(0.95), stk(K, 1)]),                                      // 수치
    O([sh(0.5), pw('turnStart', [sh(0.4), stk(K, 1)])], { power: true }),          // 불꽃길(매 턴)
    O([ddef(0.42, EA), st('약화', 1, EA), stk(K, 1)]),                             // 끌어오기 대신 약화
    O([sh(0.7), empower(0.3, 'any'), stk(K, 1)]),                                  // 동료에게 불씨
    O([ddef(0.4, EA), sh(1.4), st('반격', 2)], { cost: 2 }),                           // 큰 불꽃길
  ], null);
  const u4 = U(`${H}_u4`, '도깨비불', 1, '강화', [sh(0.48), pw('hurt', [ddef(0.18, EA), stk(K, 1)], { when: { guarded: true } })], [
    O([sh(0.7), pw('hurt', [ddef(0.24, EA), stk(K, 1)], { when: { guarded: true } })]), // 수치
    O([sh(0.5), st('실드 유지', 1), pw('hurt', [sh(0.5), stk(K, 1)], { when: { guarded: true } })]),          // 회복 불꽃
    O([st('반격', 2), pw('turnStart', [st('반격', 1)])]),                          // 반격 불꽃
    O([sh(0.9), pw('hurt', [empower(0.3, 'any'), stk(K, 1)], { when: { guarded: true } })]), // 나눠 준 불꽃(동료 다음 카드)
    O([pw('blocked', [ddef(0.3, EA)])], { cost: 0 }),                              // 다 막으면 번진다
  ], [B('guard'), B('draw'), B(B3)]);
  const u3 = U(`${H}_u3`, '서툰 챙김', 1, '스킬', [per(K, { n: 1 }), heal(0.3), spendAll(K), st('피해 감소', 1)], [
    O([per(K, { n: 1 }), heal(0.42), spendAll(K), st('피해 감소', 1)]),            // 수치
    O([spendAll(K), empower(0.6, 'any'), heal(0.3)]),                              // 불씨 건네기(동료)
    O([per(K, { n: 1 }), sh(0.45), spendAll(K), st('실드 유지', 1)]),              // 존재의 실드
    O([{ k: 'later', n: 1, then: [heal(1.2), sh(0.8)] }, stk(K, 2)]),                               // 조금만 나누기
    O([per(K, { n: 1 }), ddef(0.25, EA), spendAll(K)]),                            // 불꽃 되돌려주기(공격)
  ], [B('heal'), B('cost'), B(B3)]);
  const u2 = U(`${H}_u2`, '영속', 1, '강화', [st('끈기', 1), pw('endure', [heal(1.0), stk(K, 3)])], [
    O([st('끈기', 1), pw('endure', [heal(1.4), stk(K, 3), sh(1.0)])]),                       // 수치
    O([st('끈기', 1), pw('endure', [st('기절', 1, TOP), stk(K, 3)])]),             // 변이(가장 센 적 기절)
    O([st('끈기', 1), stk(K, 3), pw('turnStart', [stk(K, 1), sh(0.6)], { conds: [{ c: 'status', id: '끈기', n: 1 }] })]), // 존재의 증명(끈기 있는 동안 매 턴)
    O([st('끈기', 1), pw('endure', [empower(1.0, 'any'), stk(K, 3)])]),            // 작은 불꽃들(동료 한 방)
    O([st('끈기', 1), pw('endure', [heal(0.8)])], { cost: 0 }),                    // 가벼운 영속
  ], [B('guard'), B('draw'), B(B3)]);
  j.cards = [...keep, u1, u4, u3, u2];
}

// ════════════════════════════════════════════════════════════════════
// 7. 네르 — 거드는 형 · 서포터
//    엔진: 졸며 꾸벅 기도를 모으고(동료 카드도 기도가 된다) → 세계수의 계시로 이번 턴 아군 피해를 올리고 → 그 턴에 동료 카드를 몰아 낸다. 다 차면 꿈속 계시
// ════════════════════════════════════════════════════════════════════
function ner(j) {
  const H = '네르', h = j.heroes[0], K = '꾸벅 기도', DR = '네르_dream';
  h.starter = [`${H}_s1`, `${H}_s1`, `${H}_s2`, `${H}_u1`];
  const keep = j.cards.filter(c => [`${H}_s1`, `${H}_s2`, DR].includes(c.id));
  const B3 = [stk(K, 1)];
  const all = 'allAllies';
  const u1 = U(`${H}_u1`, '꿈으로 올리는 기도', 0, '스킬', [stk(K, 1), draw(1)], [
    O([stk(K, 2), draw(1)]),                                                       // 수치
    O([stk(K, 1), draw(1), pw('keepAp', [stk(K, 1), sh(0.3)])], { power: true }),                               // 졸음 수행(AP 남기면 기도)
    O([stk(K, 1), draw(1, { who: 'other' }), empower(0.2, 'any')]),                                     // 대신 기도(동료 카드)
    O([make(DR, 1), stk(K, 1)]),                                                 // 바로 계시
    O([stk(K, 1), sh(1.3)]),                                                       // 잠꼬대(막기)
  ], null, ['신속']);
  const u4 = U(`${H}_u4`, '여왕님 앞은 못 지나가요', 1, '강화', [st('피해 감소', 1), pw('play', [stk(K, 1)], { when: { who: 'other' } })], [
    O([pw('play', [stk(K, 1)], { when: { who: 'other' } })], { cost: 0 }),         // 수치(비용)
    O([st('피해 감소', 1), sh(0.8), pw('play', [sh(0.5), stk(K, 1)], { when: { type: '공격', who: 'other' } })]), // 근위(동료 공격마다 실드)
    O([st('피해 감소', 3), pw('hurt', [stk(K, 1), sh(0.4)], { when: { guarded: true } })]),  // 대신 맞기
    O([st('사기', 1), pw('play', [stk(K, 1)], { when: { who: 'other' } })]),        // 세계수 결계(사기)
    O([st('피해 감소', 1), pw('turnStart', [stk(K, 1), draw(1), sh(0.2)])]),                                    // 섭정 업무(매 턴 기도 · 드로우)
  ], [B('cost'), B('draw'), B(B3)]);
  const u3 = U(`${H}_u3`, '세계수의 계시', 1, '스킬', [{ k: 'dealtMod', v: 0.25, target: all }, ifStack(K, 2), { k: 'dealtMod', v: 0.25, target: all }, spendAll(K)], [
    O([{ k: 'dealtMod', v: 0.33, target: all }, ifStack(K, 2), { k: 'dealtMod', v: 0.33, target: all }, spendAll(K)]), // 수치
    O([{ k: 'perPlayed' }, sh(0.5), ifStack(K, 2), st('사기', 2), spendAll(K)]),   // 뒷수습(낸 카드 수만큼 실드)
    O([{ k: 'dealtMod', v: 0.4, target: all }], { cost: 0 }),       // 짧고 굵게
    O([{ k: 'dealtMod', v: 0.2, target: all }, pw('turnStart', [{ k: 'dealtMod', v: 0.15, target: all }, stk(K, 1)])], { power: true }), // 워리어즈 생츄어리(기도 둘이면 매 턴)
    O([{ k: 'critMod', v: 0.3, target: all }, ifStack(K, 2), { k: 'critMod', v: 0.3, target: all }, spendAll(K)]), // 치명의 축복
  ], [B('cost'), B('draw'), B(B3)]);
  const u2 = U(`${H}_u2`, '뒷골목 오함마', 1, '공격', [ddef(1.04, E1), ifStack(K, 2), ddef(0.59, E1)], [
    O([ddef(1.35, E1), ifStack(K, 2), ddef(0.75, E1)]),                            // 수치
    O([ddef(1.2, E1), stk(K, 1), { k: 'ifKill' }, make(DR, 1)]),                              // 사탕 뜯기(처치하면 계시)
    O([ddef(0.7, EA), stk(K, 1)]),                                                 // 세계수의 도끼(광역)
    O([ddef(0.8, E1), stk(K, 2)]),                                                 // 깨우는 망치(기도)
    O([ddef(1.0, E1), stk(K, 1), pw('play', [ddef(0.4, E1)], { when: { type: '스킬' } })], { power: true }), // 건달 시절(스킬마다 망치)
  ], [B('power'), B('weakSpot'), B(B3)], ['분쇄']);
  j.cards = [...keep, u1, u4, u3, u2];
}

const JOBS = [['수인/디아나_왕년', diana], ['요정/에르핀', erpin], ['수인/버터', butter], ['유령/시온더다크불릿', sion], ['엘프/아멜리아', amelia], ['유령/벨라', bella], ['요정/네르', ner]];
const only = process.argv[2];
for (const [f, fn] of JOBS) {
  if (only && !f.includes(only)) continue;
  const j = JSON.parse(fs.readFileSync(`${SRC}/${f}.json`, 'utf8'));
  fn(j);
  boost(j);
  if (TUNE[f]) tune(j, TUNE[f]);
  const out = `${OUT}/${f}.json`;
  fs.mkdirSync(path.dirname(out), { recursive: true });
  const tmp = `${out}.tmp${process.pid}`;
  fs.writeFileSync(tmp, JSON.stringify(j, null, 2) + '\n', 'utf8');
  fs.renameSync(tmp, out);
  console.log('썼다', f);
}
