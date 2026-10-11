if (!process.env.BZ_OLD_GEN) { console.error("[중단] 옛 생성기입니다(2026-10-11 엘다인 리뉴얼). heroes/** 는 _gen/eldain 이 씁니다. 꼭 돌려야 하면 BZ_OLD_GEN=1 — 엘다인 파일을 덮어쓰니 주의."); process.exit(1); }
// 시범 사도의 보스 클론 — 새 엔진(시범 설계)에 맞춰 다시 짓고 _gen/deep/villages/ 에 마을 파일 통째로 쓴다(원본 world/villages 는 안 고친다).
//   node _gen/deep/clones.mjs
// 생성 클론(clone_g_*)은 손 클론(clone_<영문>)으로 바꾼다 — gen_clones.py 는 손 클론이 있는 사도를 건너뛰므로 다시 돌려도 덮이지 않는다.
// 전제: 클론 고학년은 엔진 BossUlt(다른 담당이 「예고 · 끊기」 를 없애는 중 — 바로 쓰고 위력 50% 안팎). 그래서 클론의 「끊을 거리」 는 쌓이는 수치 + 격파로 둔다.
// 체력 · 강인도 · 줄(row)은 원래 클론 그대로, 수 값은 원래 생성 키트(그 사도 overrides 의 k · s 로 맞춘 값) 언저리.
import fs from 'fs';
const SRC = 'C:/projects/_backup/heroes_villages_before_deep_20261009/villages';   // 원본 반영 뒤에는 백업에서 읽는다
const OUT = 'C:/projects/bolzena-content-v2/_gen/deep/villages';
const load = f => JSON.parse(fs.readFileSync(`${SRC}/${f}.json`, 'utf8'));
const files = { erpien: load('erpien'), furry: load('furry'), monatium: load('monatium'), ghost: load('ghost'), spirit: load('spirit') };
const find = (d, id) => d.enemies.find(e => e.id === id);
function swap(d, oldId, e) {
  const i = d.enemies.findIndex(x => x.id === oldId);
  if (i < 0) throw new Error('없는 클론 ' + oldId);
  const o = d.enemies[i];
  d.enemies[i] = { ...e, hp: o.hp, row: o.row, nature: o.nature, clone: o.clone, tough: o.tough, boss: true, pick: 'shuffle' };
  return { before: o, after: d.enemies[i] };
}
const log = [];

// ── 버터 — 맞을 때마다 옐로카드, 넷이면 다음 차례 앞치마 매그넘(그 뒤 0), 파티의 스킬도 참는다 · 격파하면 흩어짐
{
  const d = files.furry;
  log.push(['버터', swap(d, 'clone_g_버터', {
    id: 'clone_butter', name: '버터 (클론)',
    blurb: '2층 보스 · 버터(클론) — 맞을수록 참는 리트리버. 맞거나 파티가 스킬을 낼 때마다 「옐로카드」 가 쌓이고(1개당 주는 피해 +5%), 넷이면 다음 차례에 「에이프런 매그넘」 으로 전체를 쏘고 0 으로 돌아갑니다. 격파하면 옐로카드가 흩어집니다 — 넷 차기 전에 격파할지, 터뜨리게 두고 버틸지.',
    intents: [
      { t: 'multi', v: 46, n: 4, say: '버터 플라이!', w: 3 },
      { t: 'back', v: 190, say: '짱돌 던지기', w: 3 },
      { t: 'attackAll', v: 80, say: '왕짱돌 던지기', w: 2 },
      { t: 'block', v: 130, say: '꾹 참기', w: 2 },
      { t: 'count', id: '옐로카드', v: 2, say: '레드카드', w: 2 },
    ],
    phase: { at: 0.5, say: '「참을 만큼 참았다!」', intents: [
      { t: 'multi', v: 66, n: 3, id: '취약', per: 1, say: '개껌 발사', w: 3 },
      { t: 'back', v: 215, say: '짱돌 던지기', w: 3 },
      { t: 'multi', v: 52, n: 4, say: '버터 플라이!', w: 2 },
      { t: 'count', id: '옐로카드', v: 2, say: '레드카드', w: 2 },
    ] },
    counters: [{ name: '옐로카드', desc: '꾹 참는 부당한 대우', onHit: 1, dealt: 0.05, max: 4, at: 4, act: { t: 'attackAll', v: 120, say: '에이프런 매그넘' }, mode: 'next' }],
    passives: [
      { name: '부탁은 거절 못 해', on: 'card', type: '스킬', do: { t: 'count', id: '옐로카드', v: 1, say: '부탁은 거절 못 해' } },
      { name: '흩어진 옐로카드', on: 'broken', do: { t: 'count', id: '옐로카드', v: -4, say: '흩어진 옐로카드' } },
    ],
  })]);
}

// ── 아멜리아 — 턴마다 결재 서류, 넷이면 다음 차례 감봉 통보(관통 + 약화), 행동할 때마다 파티 충격(감전) · 그냥 승인으로 동료 회복
{
  const d = files.monatium;
  log.push(['아멜리아', swap(d, 'clone_g_아멜리아', {
    id: 'clone_amelia', name: '아멜리아 (클론)',
    blurb: '2층 보스 · 아멜리아(클론) — 일을 쌓아 두지 않는 비서. 턴마다 「결재 서류」 가 쌓이고(1개당 주는 피해 +4%), 넷이면 다음 차례에 「감봉 통보」(관통 + 약화 2)로 한 번에 처리합니다. 행동할 때마다 파티에 충격 1 — 위성 폭격은 충격이 쌓인 파티를 노립니다. 격파하면 서류가 흩어집니다.',
    intents: [
      { t: 'attackAll', v: 66, id: '충격', n: 1, say: '새틀라이트 신호탄', w: 3 },
      { t: 'multi', v: 32, n: 3, say: '레이저 포인트 샷', w: 3 },
      { t: 'heal', v: 120, say: '그냥 승인', w: 2 },
      { t: 'count', id: '결재 서류', v: 1, say: '모나티엄 행정 대행', w: 2 },
    ],
    phase: { at: 0.5, say: '「야근입니다.」', intents: [
      { t: 'attackAll', v: 78, id: '충격', n: 1, say: '새틀라이트 신호탄', w: 3 },
      { t: 'multi', v: 38, n: 3, say: '레이저 포인트 샷', w: 3 },
      { t: 'count', id: '결재 서류', v: 2, say: '모나티엄 행정 대행', w: 2 },
      { t: 'debuff', id: '약화', v: 2, say: '감봉 예고', w: 2 },
    ] },
    counters: [{ name: '결재 서류', desc: '넘치는 일손으로 올라오는 서류', onTurnStart: 1, dealt: 0.04, max: 4, at: 4, act: { t: 'back', v: 150, id: '약화', n: 2, say: '감봉 통보' }, mode: 'next' }],
    passives: [
      { name: '초고속 썬더 레이저', on: 'act', do: { t: 'debuff', id: '충격', v: 1, say: '초고속 썬더 레이저' } },
      { name: '흩어진 결재 서류', on: 'broken', do: { t: 'count', id: '결재 서류', v: -4, say: '흩어진 결재 서류' } },
    ],
  })]);
}

// ── 네르 — 턴마다 꾸벅 기도, 셋이면 바로 세계수의 계시(적 전체 사기 2) · 파티가 카드 셋을 낼 때마다 기도 1 · 행동하면 피해 감소(무적권)
{
  const d = files.erpien;
  log.push(['네르', swap(d, 'clone_g_네르', {
    id: 'clone_ner', name: '네르 (클론)',
    blurb: '2층 보스 · 네르(클론) — 졸다 깨면 계시를 내리는 사제장. 턴마다, 그리고 파티가 카드를 3장 낼 때마다 「꾸벅 기도」 가 쌓이고, 셋이면 곧바로 「세계수의 계시」 로 적 전체에 사기 2. 행동한 뒤에는 피해 감소 1(무적권). 계시 전에 격파하면 기도가 흩어집니다 — 카드를 몰아 낼지, 나눠 낼지.',
    intents: [
      { t: 'back', v: 100, say: '뒷골목 오함마', w: 3 },
      { t: 'attackAll', v: 60, say: '도끼 올려치기', w: 3 },
      { t: 'heal', v: 110, say: '달콤한 간식', w: 2 },
      { t: 'count', id: '꾸벅 기도', v: 1, say: '여왕님 앞은 못 지나가요', w: 2 },
    ],
    phase: { at: 0.5, say: '「엘드르의 축복…!」', intents: [
      { t: 'back', v: 120, say: '뒷골목 오함마', w: 3 },
      { t: 'multi', v: 33, n: 3, say: '쏟아지는 졸음', w: 3 },
      { t: 'heal', v: 135, say: '달콤한 간식', w: 2 },
      { t: 'count', id: '꾸벅 기도', v: 2, say: '꿈속 계시', w: 2 },
    ] },
    counters: [{ name: '꾸벅 기도', desc: '졸다가 들키면 우기는 기도', onTurnStart: 1, max: 3, at: 3, act: { t: 'buff', id: '사기', v: 2, all: true, say: '세계수의 계시' }, mode: 'now' }],
    passives: [
      { name: '사제장의 무적권', on: 'act', do: { t: 'buff', id: '피해 감소', v: 1, say: '사제장의 무적권' } },
      { name: '여왕님 앞은 못 지나가요', on: 'card', every: 3, limit: 0, do: { t: 'count', id: '꾸벅 기도', v: 1, say: '여왕님 앞은 못 지나가요' } },
      { name: '흩어진 꾸벅 기도', on: 'broken', do: { t: 'count', id: '꾸벅 기도', v: -3, say: '흩어진 꾸벅 기도' } },
    ],
  })]);
}

// ── 에르핀(손 클론 clone_erpin) — 외상 장부 · 간식 소환 · 판 셋은 그대로, 「배고픔」 을 새 엔진 「배부름」 으로:
//    턴마다 케이크를 먹어 배부름 +1(1개당 주는 피해 +5%) → 다섯이면 다음 차례 「산 뽑아 던지기」 한 방 뒤 0 · 맞으면 하나씩 토한다(배가 비면 덜 세다)
{
  const d = files.erpien; const e = find(d, 'clone_erpin'); const before = JSON.parse(JSON.stringify(e));
  e.blurb = '2층 보스 · 에르핀(클론) — 무전취식 여왕. 파티가 카드를 낼 때마다 「외상 장부」 가 차 여덟이면 다음 턴 AP 를 1 깎습니다. 턴마다 케이크를 먹어 「배부름」 이 오르고(1개당 주는 피해 +5%) 다섯이면 다음 차례 「산 뽑아 던지기」 한 방 — 때리면 하나씩 토해 냅니다. 간식(탱탱 멜로 · 강인도 없음)이 쓰러지면 먹어 치워 크게 회복합니다.';
  const ren = { '산을 뽑아 던지기': '산 뽑아 던지기', '케이크 난사': '마력탄 폭주', '케이크로 배 채우기': '친구 몰래 케이크' };
  for (const it of [...e.intents, ...(e.phase?.intents || []), ...(e.phase2?.intents || [])]) if (ren[it.say]) it.say = ren[it.say];
  const ci = e.intents.findIndex(it => it.say === '친구 몰래 케이크');
  if (ci >= 0) e.intents[ci] = { t: 'count', id: '배부름', v: 2, say: '친구 몰래 케이크', w: 3 };
  e.counters = e.counters.filter(c => c.name !== '배고픔');
  e.counters.push({ name: '배부름', desc: '케이크로 채운 여왕의 배', onTurnStart: 1, onHit: -1, dealt: 0.05, max: 5, at: 5, act: { t: 'back', v: 270, say: '산 뽑아 던지기' }, mode: 'next' });
  log.push(['에르핀', { before, after: e }]);
}

// ── 묶음 1(2026-10-10) ──
// 생성 키트 → 손 클론: 원래 수의 꼴 · 값(overrides 로 맞춘 세기)은 두고 이름(say) · 쌓이는 수치 · 패시브를 새 엔진으로
function regen(d, oldId, newId, o) {
  const e = JSON.parse(JSON.stringify(find(d, oldId)));
  const ren = (list, says) => list.forEach((it, i) => { if (says[i]) { if (typeof says[i] === 'string') it.say = says[i]; else Object.assign(it, says[i]); } });
  ren(e.intents, o.say || []); ren(e.phase.intents, o.psay || []);
  return swap(d, oldId, { ...e, id: newId, blurb: o.blurb, phase: { ...e.phase, say: o.phaseSay || e.phase.say }, counters: o.counters, passives: o.passives });
}
const rename = (e, map) => { for (const it of [...e.intents, ...(e.phase?.intents || []), ...(e.phase2?.intents || [])]) if (map[it.say]) it.say = map[it.say]; };
{
  // 마리(2성 — 1층 보스 후보): 턴마다 · 파티가 공격할 때마다 묻은 폭탄, 여섯이면 다음 차례 기폭(전체)
  log.push(['마리', regen(files.erpien, 'clone_g_마리', 'clone_mari', {
    blurb: '1층 보스 · 마리(클론) — 일단 묻고 보는 황금손. 턴마다, 그리고 파티가 공격 카드를 낼 때마다 「묻은 폭탄」 이 늘고(1개당 주는 피해 +4%), 여섯이면 다음 차례 「기폭 스위치」 로 전체를 날립니다. 격파하면 묻은 폭탄이 흩어집니다 — 빨리 몰아칠수록 폭탄도 빨리 묻힙니다.',
    say: ['폭탄 배달 왔어용~', '숨겨 둔 한 방', '빨간 막대기 묻기', '방폭 헬멧', { t: 'count', id: '묻은 폭탄', v: 1, say: '빨간 막대기 묻기' }],
    psay: ['폭죽 던지기', '숨겨 둔 한 방', '폭탄 배달 왔어용~', { t: 'count', id: '묻은 폭탄', v: 2, say: '빨간 막대기 묻기' }],
    phaseSay: '「발파 합니다!」',
    counters: [{ name: '묻은 폭탄', desc: '곳곳에 묻어 둔 빨간 막대기', onTurnStart: 1, dealt: 0.04, max: 6, at: 6, act: { t: 'attackAll', v: 95, say: '기폭 스위치' }, mode: 'next' }],
    passives: [
      { name: '강화 폭탄', on: 'card', type: '공격', do: { t: 'count', id: '묻은 폭탄', v: 1, say: '강화 폭탄' } },
      { name: '흩어진 폭탄', on: 'broken', do: { t: 'count', id: '묻은 폭탄', v: -6, say: '흩어진 폭탄' } },
    ],
  })]);
  // 오르: 턴마다 응원, 셋이면 다음 차례 2인용 로켓(전체) · 가장 센 사도에게 강제 기상(약화)
  log.push(['오르', regen(files.monatium, 'clone_g_오르', 'clone_or', {
    blurb: '2층 보스 · 오르(클론) — 한발 늦은 발명가. 턴마다 「응원」 이 쌓여(1개당 주는 피해 +5%) 셋이면 다음 차례 「2인용 로켓」 으로 전체를 칩니다. 「강제 기상 장치」 로 파티를 약하게 하고, 격파하면 응원이 끊깁니다.',
    say: ['기계팔 크앙', '휴대용 돔', { t: 'debuff', id: '약화', v: 2, say: '강제 기상 장치' }, '렌치 휘두르기', { t: 'buff', id: '사기', v: 1, say: '응원의 녹음기' }],
    psay: ['기계팔 크앙', '렌치 휘두르기', '휴대용 돔', '한발 늦은 발명'],
    phaseSay: '「휴대용 돔 전개!」',
    counters: [{ name: '응원', desc: '녹음기에 담긴 응원', onTurnStart: 1, dealt: 0.05, max: 3, at: 3, act: { t: 'attackAll', v: 123, say: '2인용 로켓' }, mode: 'next' }],
    passives: [
      { name: '포기 없는 발명', on: 'lowHp', at: 0.5, do: { t: 'count', id: '응원', v: 2, say: '포기 없는 발명' } },
      { name: '끊긴 녹음', on: 'broken', do: { t: 'count', id: '응원', v: -3, say: '끊긴 녹음' } },
    ],
  })]);
  // 시저: 턴마다 단종 예고, 셋이면 다음 차례 「싹둑싹둑」(전체 + 약화) · 한정판의 맛(큰 회복)
  log.push(['시저', regen(files.spirit, 'clone_g_시저', 'clone_caesar', {
    blurb: '2층 보스 · 시저(클론) — 자칭 최고의 빌런. 턴마다 「단종 예고」 가 쌓여 셋이면 다음 차례 「싹둑싹둑」 — 파티 전체에 약화를 남기는 한 방입니다. 「한정판의 맛」 으로 크게 회복하고, 격파하면 예고가 취소됩니다.',
    say: ['보이는 실', '한정판의 맛', { t: 'debuff', id: '취약', v: 2, say: '싹둑' }, '싹둑싹둑', { t: 'buff', id: '사기', v: 1, say: '빌런의 품격' }],
    psay: ['빌런의 품격', '싹둑싹둑', '한정판의 맛', '싹둑'],
    phaseSay: '「가지치기 시간이다!」',
    counters: [{ name: '단종 예고', desc: '두 차례 뒤 단종되는 한정판', onTurnStart: 1, dealt: 0.03, max: 3, at: 3, act: { t: 'attackAll', v: 82, id: '약화', n: 2, say: '싹둑싹둑' }, mode: 'next' }],
    passives: [
      { name: '의외로 빌런이 이기는 전개', on: 'lowHp', at: 0.3, do: { t: 'selfHeal', v: 300, say: '의외로 빌런이 이기는 전개' } },
      { name: '취소된 단종', on: 'broken', do: { t: 'count', id: '단종 예고', v: -3, say: '취소된 단종' } },
    ],
  })]);
  // 스피키(메이드): 파티가 공격할 때마다 잡동사니, 넷이면 다음 차례 터보 흡입(연타)
  log.push(['스피키(메이드)', regen(files.ghost, 'clone_g_스피키_메이드', 'clone_spikymaid', {
    blurb: '2층 보스 · 스피키(메이드)(클론) — 크레페를 흉내 내는 청소 메이드. 파티가 공격 카드를 낼 때마다 「잡동사니」 를 빨아들여(1개당 주는 피해 +4%) 넷이면 다음 차례 「터보 흡입」 으로 몰아 뱉습니다. 격파하면 청소기가 멈춥니다.',
    say: ['먼지털이 휘두르기', '터보 흡입', '깨끗하면 할 일이 없어!', '두 명의 크레페', { say: '먼지판 만들기' }],
    psay: ['먼지털이 대회전', '터보 흡입', '깨끗하면 할 일이 없어!', '먼지판 만들기'],
    counters: [{ name: '잡동사니', desc: '고장 난 청소기에 빨려 든 것들', onCard: 1, cardType: '공격', dealt: 0.04, max: 4, at: 4, act: { t: 'multi', v: 45, n: 4, say: '터보 흡입' }, mode: 'next' }],
    passives: [
      { name: '자동 청소 모드에요오!', on: 'turnStart', do: { t: 'block', v: 60, say: '자동 청소 모드에요오!' } },
      { name: '멈춘 청소기', on: 'broken', do: { t: 'count', id: '잡동사니', v: -4, say: '멈춘 청소기' } },
    ],
  })]);
  // 잉클: 파티가 공격할 때마다 먹빛(받는 피해 감소), 넷이면 다음 차례 마무리 펜 터치
  log.push(['잉클', regen(files.spirit, 'clone_g_잉클', 'clone_inkle', {
    blurb: '2층 보스 · 잉클(클론) — 버려진 원고에서 태어난 잉크의 정령. 파티가 공격 카드를 낼 때마다 「먹빛」 이 번져(1개당 받는 피해 −4%) 넷이면 다음 차례 「마무리 펜 터치」 로 전체를 칩니다. 격파하면 먹빛이 씻겨 나갑니다.',
    say: ['마무리 펜 터치', '먹물 세례', '붓끝 찌르기', '다시 쓰는 이야기', { say: '고쳐 쓴 원고' }],
    psay: ['큰붓 내려긋기', '먹물 세례', '붓끝 찌르기', '고쳐 쓴 원고'],
    counters: [{ name: '먹빛', desc: '획마다 스며드는 먹물 얼룩', onCard: 1, cardType: '공격', taken: -0.04, max: 4, at: 4, act: { t: 'attackAll', v: 99, say: '마무리 펜 터치' }, mode: 'next' }],
    passives: [
      { name: '세상에 한 획', on: 'turnStart', do: { t: 'debuff', id: '약화', v: 1, say: '세상에 한 획' } },
      { name: '씻긴 먹빛', on: 'broken', do: { t: 'count', id: '먹빛', v: -4, say: '씻긴 먹빛' } },
    ],
  })]);
  // 손 클론(엘레나 · 칸나 · 캬롯 · 셰이디): 수 이름만 새 카드 이름으로
  rename(find(files.monatium, 'clone_canna'), { '연사': '큰 거 한 방', '명포수 저격': '포격 요청' });
  rename(find(files.erpien, 'clone_carrot'), { '사탕수수 회초리': '사탕수수 몽둥이' });
  rename(find(files.ghost, 'clone_shady'), { '깜짝 장난': '장난 노트', '아공간에 AP 가두기': '달력 한 장 넘기기' });
  rename(find(files.monatium, 'clone_elena'), { '시장 레이저': '전술 드론 MK-2' });
}

// ── 묶음 2(2026-10-10) — 두 얼굴형 · 버티형 ──
{
  // 코미: 턴이 끝날 때마다 낮잠(1개당 받는 피해 −4%) — 넷이면 다음 차례 엘프산 특제 사료(전체). 자는 동안 단단하니 격파로 깨운다
  log.push(['코미', regen(files.furry, 'clone_g_코미', 'clone_komi', {
    blurb: '2층 보스 · 코미(클론) — 일하면 지는 고양이. 턴이 끝날 때마다 「낮잠」 이 쌓여(1개당 받는 피해 −4%) 넷이면 다음 차례 「엘프산 특제 사료」 로 거대해져 전체를 칩니다. 격파하면 낮잠에서 깹니다 — 자는 동안 단단해지니 일찍 깨울수록 좋습니다.',
    say: ['냥펀치', '푹신푹신 타임', '코미버터의 낮잠시간', '엘프산 특제 사료', '냥펀치'],
    psay: ['거대 냥펀치', '푹신푹신 타임', '엘프산 특제 사료', '엘프산 사료 한 그릇'],
    counters: [{ name: '낮잠', desc: '일하지 않고 모아 둔 낮잠', onTurnEnd: 1, taken: -0.04, max: 4, at: 4, act: { t: 'attackAll', v: 136, say: '엘프산 특제 사료' }, mode: 'next' }],
    passives: [
      { name: '쿨쿨', on: 'turnEnd', do: { t: 'count', id: '낮잠', v: 1, say: '쿨쿨' } },
      { name: '낮잠의 미학', on: 'act', do: { t: 'block', v: 57, say: '낮잠의 미학' } },
      { name: '깨어난 코미', on: 'broken', do: { t: 'count', id: '낮잠', v: -4, say: '깨어난 코미' } },
    ],
  })]);
  // 이드(재활): 맞을 때마다 악몽 — 여섯이면 다음 차례 꿈의 공간 붕괴(전체). 파티가 스킬을 내면 실드
  log.push(['이드(재활)', regen(files.monatium, 'clone_g_이드_재활', 'clone_ide_rehab', {
    blurb: '2층 보스 · 이드(재활)(클론) — 꿈속에서 맞은 아픔을 모으는 엘프. 맞을 때마다 「악몽」 이 쌓여(1개당 주는 피해 +3%) 여섯이면 다음 차례 「꿈의 공간 붕괴」 로 전체를 칩니다. 파티가 스킬을 내면 실드를 두르고, 격파하면 악몽이 흩어집니다 — 잘게 여러 번 때릴수록 빨리 무너집니다.',
    say: ['뚜렷한 경계', '잠든 동안', '같이 걷는 산책', '꿈의 공간 붕괴', '꿈결 두드리기'],
    psay: ['뚜렷한 경계', '잠든 동안', '꿈의 공간 붕괴', '한 걸음씩'],
    counters: [{ name: '악몽', desc: '꿈속에서 맞아 둔 아픔', onHit: 1, dealt: 0.03, max: 6, at: 6, act: { t: 'attackAll', v: 108, say: '꿈의 공간 붕괴' }, mode: 'next' }],
    passives: [
      { name: '악몽 마주하기', on: 'hurt', do: { t: 'count', id: '악몽', v: 1, say: '악몽 마주하기' } },
      { name: '환상의 짝꿍', on: 'card', type: '스킬', do: { t: 'block', v: 77, say: '환상의 짝꿍' } },
      { name: '흩어진 악몽', on: 'broken', do: { t: 'count', id: '악몽', v: -6, say: '흩어진 악몽' } },
    ],
  })]);
  // 하이디: 턴마다 · 맞을 때마다 잠복(1개당 주는 피해 +5%) — 넷이면 다음 차례 셔터 연사(전체)
  log.push(['하이디', regen(files.monatium, 'clone_g_하이디', 'clone_heidi', {
    blurb: '2층 보스 · 하이디(클론) — 상자에 숨은 특종 기자. 턴마다, 그리고 맞을 때마다 「잠복」 이 쌓여(1개당 주는 피해 +5%) 넷이면 다음 차례 「셔터 연사」 로 전체를 찍습니다. 격파하면 상자에서 끌려 나와 잠복이 흩어집니다.',
    say: ['플래시 연사', '특종감 지정', '셀카 한 장', '잠입 취재 중!', { t: 'count', id: '잠복', v: 1, say: '삼각대 가드' }],
    psay: ['줌 인 샷', '특종감 지정', '셀카 한 장', { t: 'count', id: '잠복', v: 2, say: '잠입 취재 중!' }],
    phaseSay: '「현장 중계 중!」',
    counters: [{ name: '잠복', desc: '숨어서 모은 셔터', onTurnStart: 1, dealt: 0.05, max: 4, at: 4, act: { t: 'attackAll', v: 123, say: '셔터 연사' }, mode: 'next' }],
    passives: [
      { name: '목표 포착!', on: 'fightStart', do: { t: 'count', id: '잠복', v: 1, say: '목표 포착!' } },
      { name: '목표 포착!', on: 'hurt', do: { t: 'count', id: '잠복', v: 1, say: '목표 포착!' } },
      { name: '들킨 잠복', on: 'broken', do: { t: 'count', id: '잠복', v: -4, say: '들킨 잠복' } },
    ],
  })]);
  // 리온: 파티가 공격할 때마다 유죄 — 넷이면 다음 차례 도장 낙하(전체)
  log.push(['리온', regen(files.furry, 'clone_g_리온', 'clone_lion', {
    blurb: '2층 보스 · 리온(클론) — 판결봉을 든 자경단 히어로. 파티가 공격 카드를 낼 때마다 「유죄」 가 쌓여(1개당 주는 피해 +4%) 넷이면 다음 차례 「도장 낙하」 로 전체를 칩니다. 격파하면 판결이 뒤집혀 유죄가 흩어집니다.',
    say: ['판결봉 휘두르기', '유죄 선언', '라이온 시그널', '도장 낙하', '이의 있음!'],
    psay: ['판결봉 휘두르기', '유죄 선언', '도장 낙하', '라이온 시그널'],
    phaseSay: '「즉결심판!」',
    counters: [{ name: '유죄', desc: '리온이 내린 판결', onCard: 1, cardType: '공격', dealt: 0.04, max: 4, at: 4, act: { t: 'attackAll', v: 134, say: '도장 낙하' }, mode: 'next' }],
    passives: [
      { name: '판결봉', on: 'card', type: '공격', do: { t: 'count', id: '유죄', v: 1, say: '판결봉' } },
      { name: '히어로 꿈나무', on: 'act', do: { t: 'block', v: 60, say: '히어로 꿈나무' } },
      { name: '뒤집힌 판결', on: 'broken', do: { t: 'count', id: '유죄', v: -4, say: '뒤집힌 판결' } },
    ],
  })]);
  // 헤일리(멀쩡): 턴마다 훈장(1개당 받는 피해 −4%) — 넷이면 다음 차례 전함 호출(전체)
  log.push(['헤일리(멀쩡)', regen(files.monatium, 'clone_g_헤일리_멀쩡', 'clone_hailey_ok', {
    blurb: '2층 보스 · 헤일리(멀쩡)(클론) — 과거를 극복한 전열 장교. 턴마다, 그리고 행동할 때마다 「극복의 훈장」 이 쌓여(1개당 받는 피해 −4%) 넷이면 다음 차례 「전함 호출」 로 전체를 칩니다. 격파하면 훈장이 흩어집니다.',
    say: ['나아가는 결의', '방진', '극복의 기록', '전함 호출', '군홧발'],
    psay: ['나아가는 결의', '방진', '전함 호출', '무릎 꿇는 교섭'],
    phaseSay: '「전함 호출!」',
    counters: [{ name: '극복의 훈장', desc: '극복해 온 날의 훈장', onTurnStart: 1, taken: -0.04, max: 4, at: 4, act: { t: 'attackAll', v: 117, say: '전함 호출' }, mode: 'next' }],
    passives: [
      { name: '다 막아 낸 전열', on: 'act', do: { t: 'count', id: '극복의 훈장', v: 1, say: '다 막아 낸 전열' } },
      { name: '잊을 수 없는 기억', on: 'fightStart', do: { t: 'count', id: '극복의 훈장', v: 2, say: '잊을 수 없는 기억' } },
      { name: '흩어진 훈장', on: 'broken', do: { t: 'count', id: '극복의 훈장', v: -4, say: '흩어진 훈장' } },
    ],
  })]);
  // 셀리네: 턴마다 · 해로운 효과를 받을 때마다 표정 — 다섯이면 다음 차례 화난 얼굴 갤러리(전체)
  log.push(['셀리네', regen(files.ghost, 'clone_g_셀리네', 'clone_seline', {
    blurb: '2층 보스 · 셀리네(클론) — 화난 얼굴을 모으는 유령 엘튜버. 턴마다, 그리고 디버프를 받을 때마다 「표정」 이 쌓여(1개당 주는 피해 +3%) 다섯이면 다음 차례 「화난 얼굴 갤러리」 로 전체를 칩니다. 격파하면 표정이 흩어집니다 — 약 올릴수록 빨리 찹니다.',
    say: ['온 마음을 다해', '팬서비스 가드', '사근사근 반말', '화난 얼굴 갤러리', '하트 탄'],
    psay: ['온 마음을 다해', '팬서비스 가드', '조회수 폭발 셀카', '어그로 썸네일'],
    phaseSay: '「방금 그 표정 좋았어!」',
    counters: [{ name: '표정', desc: '모아 둔 화난 얼굴', onTurnStart: 1, dealt: 0.03, max: 5, at: 5, act: { t: 'attackAll', v: 129, say: '화난 얼굴 갤러리' }, mode: 'next' }],
    passives: [
      { name: '방금 그 표정 좋았어', on: 'debuffed', do: { t: 'count', id: '표정', v: 1, say: '방금 그 표정 좋았어' } },
      { name: '세 번째 도발', on: 'act', do: { t: 'block', v: 60, say: '세 번째 도발' } },
      { name: '흩어진 표정', on: 'broken', do: { t: 'count', id: '표정', v: -5, say: '흩어진 표정' } },
    ],
  })]);
}

// ── 묶음 3(2026-10-10) — 박자형 · 격파형 ──
{
  // 리코타: 파티가 카드를 낼 때마다 코스(1개당 주는 피해 +3%) — 여섯이면 다음 차례 웍질(전체)
  log.push(['리코타', regen(files.erpien, 'clone_g_리코타', 'clone_ricotta', {
    blurb: '2층 보스 · 리코타(클론) — 코스 순서를 고집하는 셰프. 파티가 카드를 낼 때마다 「코스」 가 이어져(1개당 주는 피해 +3%) 여섯이면 다음 차례 「웍질」 로 전체를 칩니다. 카드를 낼 때마다 칼질도 한 번 — 적게 내고 세게 칠수록 코스가 늦게 찹니다. 격파하면 코스가 흩어집니다.',
    say: ['다 조려버리겠습니다', '냄비 뚜껑', '리코타 풀코스', '웍질', '주방칼 썰기'],
    psay: ['다 조려버리겠습니다', '아뮤즈 부쉬', '웍질', '똑같이 본뜬 요리'],
    phaseSay: '「준비만전!」',
    counters: [{ name: '코스', desc: '가벼운 접시에서 무거운 접시로', onCard: 1, dealt: 0.03, max: 6, at: 6, act: { t: 'attackAll', v: 60, say: '웍질' }, mode: 'next' }],
    passives: [
      { name: '코스 진행', on: 'card', do: { t: 'count', id: '코스', v: 1, say: '코스 진행' } },
      { name: '주방칼 썰기', on: 'card', do: { t: 'attack', v: 41, say: '주방칼 썰기' } },
      { name: '흩어진 코스', on: 'broken', do: { t: 'count', id: '코스', v: -6, say: '흩어진 코스' } },
    ],
  })]);
  // 샤샤: 파티가 공격할 때마다 텀블러 수압 — 넷이면 다음 차례 텀블러 폭주(전체)
  log.push(['샤샤', regen(files.erpien, 'clone_g_샤샤', 'clone_shasha', {
    blurb: '2층 보스 · 샤샤(클론) — 수압을 조절할 생각이 없는 텀블러. 파티가 공격 카드를 낼 때마다 「텀블러 수압」 이 차올라(1개당 주는 피해 +4%) 넷이면 다음 차례 「텀블러 폭주」 로 전체를 칩니다. 격파하면 물이 쏟아져 수압이 빠집니다.',
    say: ['텀블러 투척!', '물줄기 발사', '부수적인 피해입니다', '텀블러 달래기', '텀블러 방패'],
    psay: ['텀블러 투척!', '물줄기 발사', '부수적인 피해입니다', '텀블러 방패'],
    phaseSay: '「물대포 폭격!!」',
    counters: [{ name: '텀블러 수압', desc: '조절할 생각이 없는 텀블러 수압', onCard: 1, cardType: '공격', dealt: 0.04, max: 4, at: 4, act: { t: 'attackAll', v: 89, say: '텀블러 폭주' }, mode: 'next' }],
    passives: [
      { name: '대용량 텀블러', on: 'fightStart', do: { t: 'count', id: '텀블러 수압', v: 1, say: '대용량 텀블러' } },
      { name: '대용량 텀블러', on: 'card', type: '공격', do: { t: 'count', id: '텀블러 수압', v: 1, say: '대용량 텀블러' } },
      { name: '쏟아진 물', on: 'broken', do: { t: 'count', id: '텀블러 수압', v: -4, say: '쏟아진 물' } },
    ],
  })]);
  // 슈팡: 파티가 카드를 낼 때마다 배달 속도 — 여섯이면 다음 차례 슈팡 배송(전체)
  log.push(['슈팡', regen(files.erpien, 'clone_g_슈팡', 'clone_shupang', {
    blurb: '2층 보스 · 슈팡(클론) — 느린 세상을 못 참는 배달부. 파티가 카드를 낼 때마다 「배달 속도」 가 올라(1개당 주는 피해 +3%) 여섯이면 다음 차례 「슈팡 배송」 으로 전체를 칩니다. 몰아 낼수록 빨리 찹니다 — 격파하면 속도가 꺾입니다.',
    say: ['케이크 위 코너링', '무책임 배달부', '슈팡은⋯ 달리고 싶다!', '케이크 위 코너링', '야, 타!'],
    psay: ['소포 투척', '택배 상자', '무책임 배달부', '케이크 위 코너링'],
    phaseSay: '「슈팡 배송!」',
    counters: [{ name: '배달 속도', desc: '멈출 수 없는 배달 속도', onCard: 1, dealt: 0.03, max: 6, at: 6, act: { t: 'attackAll', v: 58, say: '슈팡 배송' }, mode: 'next' }],
    passives: [
      { name: '과속 배달', on: 'card', do: { t: 'count', id: '배달 속도', v: 1, say: '과속 배달' } },
      { name: '배달의 요정', on: 'fightStart', do: { t: 'buff', id: '사기', v: 1, say: '배달의 요정' } },
      { name: '꺾인 속도', on: 'broken', do: { t: 'count', id: '배달 속도', v: -6, say: '꺾인 속도' } },
    ],
  })]);
  // 리스티: 파티가 공격할 때마다 · 행동할 때마다 개인정보 — 셋이면 다음 차례 오늘의 POTG(전체)
  log.push(['리스티', regen(files.monatium, 'clone_g_리스티', 'clone_risty', {
    blurb: '2층 보스 · 리스티(클론) — 리그 오브 엘프 최고 티어 해커. 파티가 공격 카드를 낼 때마다, 그리고 행동할 때마다 「개인정보」 를 긁어(1개당 주는 피해 +3%) 셋이면 다음 차례 「오늘의 POTG」 로 전체를 칩니다. 격파하면 긁은 정보가 날아갑니다.',
    say: ['테크노맨시', '훗, 못 깨는 게임은 없지', '다 마신 캔', '개인정보 수집', '빈 캔'],
    psay: ['키보드 연타', '훗, 못 깨는 게임은 없지', '다 마신 캔', '빈 캔'],
    phaseSay: '「복셀 글리치!」',
    counters: [{ name: '개인정보', desc: '글러브가 긁어 온 정보', onCard: 1, cardType: '공격', dealt: 0.03, max: 3, at: 3, act: { t: 'attackAll', v: 117, say: '오늘의 POTG' }, mode: 'next' }],
    passives: [
      { name: '글러브 해킹', on: 'card', type: '공격', do: { t: 'count', id: '개인정보', v: 1, say: '글러브 해킹' } },
      { name: '천재 해커의 등장', on: 'act', do: { t: 'count', id: '개인정보', v: 1, say: '천재 해커의 등장' } },
      { name: '날아간 정보', on: 'broken', do: { t: 'count', id: '개인정보', v: -3, say: '날아간 정보' } },
    ],
  })]);
  // 캐시: 맞을 때마다 히익 전류 — 넷이면 다음 차례 풀파워 테이저(전체) · 파티가 공격할 때마다 과충전
  log.push(['캐시', regen(files.monatium, 'clone_g_캐시', 'clone_cash', {
    blurb: '2층 보스 · 캐시(클론) — 겁나서 먼저 지지는 정찰병. 맞을 때마다 「히익 전류」 가 차(1개당 주는 피해 +3%) 넷이면 다음 차례 「풀파워 테이저」 로 전체를 칩니다. 파티가 공격 카드를 낼 때마다 과충전 테이저로 되받습니다. 격파하면 전류가 흩어집니다.',
    say: ['바삭 지짐이', '불가사의한 현상', '301일의 동굴', '더플백 은신', '숨기'],
    psay: ['테이저 찌르기', '불가사의한 현상', '301일의 동굴', '더플백 은신'],
    phaseSay: '「점프 스케어!」',
    counters: [{ name: '히익 전류', desc: '겁나서 먼저 지지는 전류', onHit: 1, dealt: 0.03, max: 4, at: 4, act: { t: 'attackAll', v: 74, say: '풀파워 테이저' }, mode: 'next' }],
    passives: [
      { name: '히익!', on: 'hurt', do: { t: 'count', id: '히익 전류', v: 1, say: '히익!' } },
      { name: '과충전 테이저', on: 'card', type: '공격', do: { t: 'attack', v: 51, say: '과충전 테이저' } },
      { name: '흩어진 전류', on: 'broken', do: { t: 'count', id: '히익 전류', v: -4, say: '흩어진 전류' } },
    ],
  })]);
  // 아르코: 파티가 카드를 낼 때마다 스텝 — 여섯이면 다음 차례 정정당당 댄스배틀(전체)
  log.push(['아르코', regen(files.spirit, 'clone_g_아르코', 'clone_arco', {
    blurb: '2층 보스 · 아르코(클론) — 99시간 논스탑 스트리트 댄서. 파티가 카드를 낼 때마다 「스텝」 을 밟아(1개당 주는 피해 +3%) 여섯이면 다음 차례 「정정당당 댄스배틀」 로 전체를 칩니다. 공격 카드에는 음파로 맞받습니다. 격파하면 스텝이 꼬입니다.',
    say: ['정정당당 댄스배틀', '토네이도 스핀', '스텝 킥', '프리즈', '적포도 스트리트'],
    psay: ['토네이도 킥', '토네이도 스핀', '스텝 킥', '적포도 스트리트'],
    phaseSay: '「퍼플 쇼츠!」',
    counters: [{ name: '스텝', desc: '엇박으로 이어 밟은 스텝', onCard: 1, dealt: 0.03, max: 6, at: 6, act: { t: 'attackAll', v: 81, say: '정정당당 댄스배틀' }, mode: 'next' }],
    passives: [
      { name: '리듬에 몸을 맡겨', on: 'card', do: { t: 'count', id: '스텝', v: 1, say: '리듬에 몸을 맡겨' } },
      { name: '음파 스텝', on: 'card', type: '공격', do: { t: 'attack', v: 56, say: '음파 스텝' } },
      { name: '꼬인 스텝', on: 'broken', do: { t: 'count', id: '스텝', v: -6, say: '꼬인 스텝' } },
    ],
  })]);
  // 손 클론 티그: 수 이름만 새 카드로
  rename(find(files.furry, 'clone_tig'), { '백호 일섬': '소닉 블레이드', '검 세우기': '받아쳐!', '장작 쪼개기!': '내려치고 휘두르기', '쌍검 난무': '쌍검 휘두르기', '검성의 호흡': '백호 비전서', '백호 참!': '오버드라이브' });
}

// ── 묶음 4(2026-10-10) — 거드는 형 · 아껴 두기형: 쌓이는 수치 · 패시브는 생성 키트(이미 그 사도 엔진)를 두고, 없어진 카드 이름만 새 카드로 ──
{
  const keep = (d, oldId, newId, map, o = {}) => {
    const e = find(d, oldId); const m = s => (s || '').replace(/「([^」]+)」/g, (x, n) => map[n] ? `「${map[n]}」` : x);
    const r = regen(d, oldId, newId, { blurb: m(e.blurb), counters: e.counters, passives: e.passives, phaseSay: o.phaseSay });
    const c = find(d, newId); rename(c, map);
    for (const k of c.counters || []) if (map[k.act?.say]) k.act.say = map[k.act.say];
    for (const p of c.passives || []) if (map[p.do?.say]) p.do.say = map[p.do.say];
    return r;
  };
  log.push(['슈로', keep(files.furry, 'clone_g_슈로', 'clone_shuro', { '옛 벗의 검': '종이 한 장 차이' })]);
  log.push(['로네(시장)', keep(files.monatium, 'clone_g_로네_시장', 'clone_rone_mayor', { '돈까스 생각': '업무 효율 131%' })]);
  log.push(['폴랑', keep(files.erpien, 'clone_g_폴랑', 'clone_polang', { '포위 심문': '120도 포위' })]);
  log.push(['스키아', keep(files.erpien, 'clone_g_스키아', 'clone_skia', { '몸짓 대화': '…(끄덕)', '벼락 내리기': '깃털 날리기' })]);
  log.push(['키샤', keep(files.ghost, 'clone_g_키샤', 'clone_kisha', { '립싱크 무대': '팬레터 낭독' })]);
  log.push(['미로', keep(files.spirit, 'clone_g_미로', 'clone_miro', { '거울 반사': '앞으로 넘어지기', '거울 조각': '앞으로 넘어지기' })]);
  log.push(['오로라', keep(files.spirit, 'clone_g_오로라', 'clone_aurora', { '빛 조각 흩뿌리기': '오로라 띠', '꺼지지 않는 불꽃': '플라즈마 불꽃', '빛 조각': '오로라 띠' })]);
}

// ── 묶음 5(2026-10-10) — 쌓고 고르기 · 대가형: 생성 키트를 손 클론으로(쌓이는 수치 · 패시브는 이미 그 사도 엔진), 없어진 카드 이름만 새 카드로 ──
{
  const keep5 = (d, oldId, newId, map) => {
    const e = find(d, oldId); const m = s => (s || '').replace(/「([^」]+)」/g, (x, n) => map[n] ? `「${map[n]}」` : x);
    const r = regen(d, oldId, newId, { blurb: m(e.blurb), counters: e.counters, passives: e.passives });
    const c = find(d, newId); rename(c, map);
    for (const k of c.counters || []) if (map[k.act?.say]) k.act.say = map[k.act.say];
    return r;
  };
  log.push(['베니(베니)', keep5(files.furry, 'clone_g_베니_베니', 'clone_beni_beni', { '꿀 던지기': '앞발 내려치기' })]);
  log.push(['델리아', keep5(files.furry, 'clone_g_델리아', 'clone_delia', {})]);
  log.push(['셰이디(역전)', keep5(files.ghost, 'clone_g_셰이디_역전', 'clone_shady_rev', { '파수꾼의 낫질': '유령 인형 샌드백' })]);
  log.push(['나이아', keep5(files.spirit, 'clone_g_나이아', 'clone_naia', { '물대포': '물총 연사', '인사 대신 물총': '물방울' })]);
  log.push(['블랑셰', keep5(files.spirit, 'clone_g_블랑셰', 'clone_blanche', {})]);
  log.push(['아일라', keep5(files.spirit, 'clone_g_아일라', 'clone_ayla', { '초폭망 직전': '휴가 중 잠적' })]);
  log.push(['라이카', keep5(files.spirit, 'clone_g_라이카', 'clone_laika', { '220V 스트레이트': '풀 스윙' })]);
}

// ── 묶음 6(2026-10-10) — 표적형 · 손 만들기형: 생성 키트를 손 클론으로, 없어진 카드 이름만 새 카드로 ──
{
  const keep6 = (d, oldId, newId, map) => {
    const e = find(d, oldId); const m = s => (s || '').replace(/「([^」]+)」/g, (x, n) => map[n] ? `「${map[n]}」` : x);
    const r = regen(d, oldId, newId, { blurb: m(e.blurb), counters: e.counters, passives: e.passives });
    const c = find(d, newId); rename(c, map);
    for (const k of c.counters || []) if (map[k.act?.say]) k.act.say = map[k.act.say];
    return r;
  };
  log.push(['로니', keep6(files.erpien, 'clone_g_로니', 'clone_roni', { '불침번': '석양의 결투' })]);
  log.push(['에슈르', keep6(files.erpien, 'clone_g_에슈르', 'clone_eshur', { '화염 주문': '궁극의 그리모어' })]);
  log.push(['그윈', keep6(files.furry, 'clone_g_그윈', 'clone_gwin', {})]);
  log.push(['스패럿', keep6(files.furry, 'clone_g_스패럿', 'clone_sparrot', { '노획물 장부': '약탈한 전리품' })]);
  log.push(['헤일리', keep6(files.monatium, 'clone_g_헤일리', 'clone_hailey', {})]);
}

// ── 묶음 7(2026-10-10) — 회복형 · 돈형 · 소환형: 생성 키트를 손 클론으로, 없어진 카드 이름만 새 카드로 ──
{
  const keep7 = (d, oldId, newId, map) => {
    const e = find(d, oldId); const m = s => (s || '').replace(/「([^」]+)」/g, (x, n) => map[n] ? `「${map[n]}」` : x);
    const r = regen(d, oldId, newId, { blurb: m(e.blurb), counters: e.counters, passives: e.passives });
    const c = find(d, newId); rename(c, map);
    for (const k of c.counters || []) if (map[k.act?.say]) k.act.say = map[k.act.say];
    return r;
  };
  log.push(['디아나', keep7(files.furry, 'clone_g_디아나', 'clone_diana', { '은퇴한 무인의 수련': '기공 주문' })]);
  log.push(['모모', keep7(files.furry, 'clone_g_모모', 'clone_momo', { '전기 수리검': '카게닌자의 극의' })]);
  log.push(['코미(수영복)', keep7(files.furry, 'clone_g_코미_수영복', 'clone_komi_swim', { '물총 세례': '주스 한 병 투척' })]);
  log.push(['아이시아', keep7(files.monatium, 'clone_g_아이시아', 'clone_icea', { '아이스크림 수레': '내가 누군지 몰라?' })]);
  log.push(['힐데', keep7(files.monatium, 'clone_g_힐데', 'clone_hilde', { '주사기총 난사': '판을 뒤집는 수' })]);
  log.push(['바롱', keep7(files.ghost, 'clone_g_바롱', 'clone_barong', { '사람 사이 갈라놓기': '뒷담까기 인형' })]);
  log.push(['멜루나', keep7(files.spirit, 'clone_g_멜루나', 'clone_meluna', { '광합성': '장기 투자' })]);
}

// ── 묶음 8(2026-10-10) — 마지막 묶음: 생성 키트를 손 클론으로, 없어진 카드 이름만 새 카드로(레비_졸업 · 비비_신성 · 우이_기억은 클론 없음, 다야는 dragon 손 클론 그대로) ──
{
  const keep8 = (d, oldId, newId, map) => {
    const e = find(d, oldId); const m = s => (s || '').replace(/「([^」]+)」/g, (x, n) => map[n] ? `「${map[n]}」` : x);
    const r = regen(d, oldId, newId, { blurb: m(e.blurb), counters: e.counters, passives: e.passives });
    const c = find(d, newId); rename(c, map);
    for (const k of c.counters || []) if (map[k.act?.say]) k.act.say = map[k.act.say];
    for (const p of c.passives || []) if (map[p.do?.say]) p.do.say = map[p.do.say];
    return r;
  };
  log.push(['에슈르(마도)', keep8(files.erpien, 'clone_g_에슈르_마도', 'clone_eshur_mado', {})]);
  log.push(['루포', keep8(files.furry, 'clone_g_루포', 'clone_lupo', {})]);
  log.push(['림', keep8(files.ghost, 'clone_g_림', 'clone_rim', {})]);
  log.push(['앨리스', keep8(files.ghost, 'clone_g_앨리스', 'clone_alice', {})]);
  log.push(['림(혼돈)', keep8(files.ghost, 'clone_g_림_혼돈', 'clone_rim_chaos', {})]);
  log.push(['뮤트', keep8(files.spirit, 'clone_g_뮤트', 'clone_mute', {})]);
  log.push(['아멜리아(R41)', keep8(files.monatium, 'clone_g_아멜리아_R41', 'clone_r41', { '나노 수복 드론': '응급 수리 드론' })]);
  log.push(['니콜', keep8(files.spirit, 'clone_g_니콜', 'clone_nicole', {})]);
}

// ── 강인도(2026-10-10 사용자 규칙) — 엘리트 · 보스 최소 5, 탱커 · 버티기 클론은 높게, 날쌘 딜러 클론은 5 근처 ──
const TOUGH = {
  clone_butter: 7, clone_amelia: 5, clone_ner: 7, clone_erpin: 6, clone_mari: 5, clone_or: 7, clone_caesar: 7, clone_spikymaid: 5, clone_inkle: 6,
  clone_canna: 5, clone_carrot: 7, clone_shady: 5, clone_elena: 5,
  clone_komi: 9, clone_ide_rehab: 10, clone_heidi: 5, clone_lion: 9, clone_hailey_ok: 9, clone_seline: 9,
  clone_ricotta: 9, clone_shasha: 5, clone_shupang: 6, clone_risty: 5, clone_cash: 5, clone_arco: 5, clone_tig: 6,
  clone_shuro: 7, clone_rone_mayor: 5, clone_polang: 7, clone_skia: 5, clone_kisha: 7, clone_miro: 9, clone_aurora: 9,
  clone_beni_beni: 5, clone_delia: 6, clone_shady_rev: 7, clone_naia: 7, clone_blanche: 5, clone_ayla: 7, clone_laika: 6,
  clone_roni: 5, clone_eshur: 5, clone_gwin: 7, clone_sparrot: 7, clone_hailey: 5,
  clone_diana: 7, clone_momo: 5, clone_komi_swim: 7, clone_icea: 5, clone_hilde: 7, clone_barong: 6, clone_meluna: 7,
  clone_eshur_mado: 5, clone_lupo: 5, clone_rim: 6, clone_alice: 5, clone_rim_chaos: 6, clone_mute: 6, clone_r41: 7, clone_nicole: 5,
};
for (const d of Object.values(files)) for (const e of d.enemies) {
  if (TOUGH[e.id] !== undefined) e.tough = TOUGH[e.id];
  // 사용자 규칙(10-10): 엘리트 · 보스 · 클론 최소 5 — 검사기도 5 로 바뀜(10-10 03시)
  const MIN_BOSS = 5;
  if (e.boss && (e.tough ?? 0) < MIN_BOSS) e.tough = MIN_BOSS;
  if (e.elite && (e.tough ?? 0) < 5) e.tough = 5;
}

// 문체(10-10): 이름 · 지문이 「~다」 · 합니다체로 끝나지 않게
const RENAME = { '참다 터짐': '참고 참은 화', '보고드립니다!': '반장님께 보고!' };
for (const d of Object.values(files)) for (const e of d.enemies) if (e.id.startsWith('clone_')) for (const p of e.passives || []) {
  if (RENAME[p.name]) p.name = RENAME[p.name]; if (p.do && RENAME[p.do.say]) p.do.say = RENAME[p.do.say];
}
fs.mkdirSync(OUT, { recursive: true });
for (const [f, d] of Object.entries(files)) {
  const out = `${OUT}/${f}.json`, tmp = `${out}.tmp${process.pid}`;
  fs.writeFileSync(tmp, JSON.stringify(d, null, 2) + '\n', 'utf8'); fs.renameSync(tmp, out);
}
fs.writeFileSync(`${OUT}/../clones_diff.json`, JSON.stringify(Object.fromEntries(log.map(([h, x]) => [h, x])), null, 1));
console.log('썼다', Object.keys(files).join(' · '), '·', log.map(x => x[0]).join(' '));
