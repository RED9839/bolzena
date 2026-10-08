// 사도 리워크 — 마녀 폴더 15명. 2단계(2026-10-07) 틀 위에 3단계(2026-10-08 카제나 자료 보강 — 지침 §12)를 덧입힘.
// 비비(신성)은 시범(rework.mjs)에서 끝냄 — 여기서 건드리지 않는다.
// 지침: _measure/사도_리워크_지침.md(§12) · 공용 부품: lib.mjs · 보고: _measure/리워크_마녀.md
// node _gen/rework/마녀.mjs [사도 일부 이름]   → heroes/마녀/<사도>.json 덮어쓰기(백업에서 읽음)
//
// 3단계에서 카드마다 지킨 것(§12-1): 얕은 갈래(숫자 · 비용 · 태그만) 둘까지 — 보통 「수치」 + 「비용↓」 · 재설계 1~2 ·
// D 강화화(power) 또는 F 서치(거르개 드로우 · 회수)를 BEST 후보로 · 카드 둘에 하나꼴 대가(태그 빼기 · 소멸 · 버리기 · 장치를 더 씀).
// ④ 완성형은 원작이 상시 효과인 셋(아야 · 요미 엘다인 한 단계 · 프리클 어사이드)만 강화 카드로 두고, 나머지는 「장치를 세는 1코 마무리」 —
// 옛 강화 엔진은 그 카드의 D 갈래로 옮겼다. 축복은 카드마다 [기존 효과 강화 · 유틸 · 장치] 결로(§12-5).
import { E1, EA, ER, dmg, ddef, hits, sh, heal, st, stk, spendAll, per, perTag, draw, make, ifStack, ifKill, ifWounded, ifBroken, inspire, power, rule, O, B, card, starter, run } from './lib.mjs';

// ── 이 스크립트만 쓰는 조각 ──
const dH = (n, r, t = E1) => ({ k: 'dmg', ratio: r, target: t, hits: n });               // 한 대상 여러 번
const ddH = (n, r, t = E1) => ({ k: 'dmg', ratio: r, base: 'def', target: t, hits: n }); // 방어 기반 여러 번
const atkRun = v => ({ k: 'atkMod', v, run: true, target: 'self' });
const spendN = (id, v) => ({ k: 'spend', id, v });
const disc = v => ({ k: 'discard', v });
const gauge = v => ({ k: 'gauge', v });
const pull = (n = 1, o = {}) => ({ k: 'pull', from: 'discard', n, ...o });
const cleanse = v => ({ k: 'cleanse', v });
const onDiscard = { k: 'when', on: 'discard' };
const P = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: o.limit } : {}), fx });
const perTurn = n => ({ per: 'turn', n }), perFight = n => ({ per: 'fight', n });
const reach = (id, n) => ({ when: { id, n } });
const swap = (j, cards) => { j.cards = [...j.cards.filter(c => !c.unique), ...cards]; };
const refKw = (fx, from, to) => fx.map(f => ((f.k === 'stack' || f.k === 'spend') && f.id === from ? { ...f, id: to } : f));
// 3단계 조각
const mine = (n = 1, o = {}) => draw(n, { who: 'self', unique: true, ...o });                       // 자신의 고유 카드 서치
const exileAll = tag => ({ k: 'exileFrom', from: 'hand', all: true, tag });                         // 손의 그 생성 카드 전부 소멸
const exileBasic = n => ({ k: 'exileFrom', from: 'hand', n, basic: true });                         // 손의 기본 카드 소멸(기본 카드 연료)
const cheaper = (o = {}) => ({ k: 'costMod', v: -1, turns: 1, n: 1, who: 'self', unique: true, ...o }); // 손의 자신의 고유 카드 1장 비용 -1(이번 턴)
const token = (j, c) => { j.cards = j.cards.filter(x => x.id !== c.id); j.cards.push({ hero: j.heroes[0].id, token: true, ...c }); };

// ════════════════════════════════════════════════════════════════════
// 1. 레비 — 딜러 · 우울. 포션으로 눌러 둔 「괴력」 — 쉬면(AP 를 남기면) · 남이 일하면 차오르고, 장도 한 방에 몽땅 싣는다.
//    3단계: 「비싼 한 방」 사도 — 장도 뽑기(2코)를 시동으로, 0코 농땡이가 장도를 찾아오고 값을 깎는다(§12-2). 연계 채우기(동료 공격 → 괴력).
// ════════════════════════════════════════════════════════════════════
function levi(j) {
  const H = '레비', K = '괴력';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '포션으로 눌러 둔 진짜 힘', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }],
    rules: [{ name: '참을 수 없는 힘', when: { on: 'stackOver', id: K }, fx: [{ k: 'perEvent' }, dmg(0.5, ER)] }],
  };
  delete h.keywords;
  h.passives = [
    P('몰래 쉬는 시간', 'turnEnd', [stk(K, 1)], { conds: [{ c: 'apLeft', n: 1 }] }),
    P('남이 일하는 사이', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: perTurn(1) }),
  ];
  h.ult.fx = refKw(h.ult.fx, '괴력 봉인', K);
  const nimble = (a, b, k = 1) => [dH(2, a), dmg(b), stk(K, k)];
  const thesis = (v, kk, r) => [atkRun(v), stk(K, kk), power(rule('spend', [dmg(r, ER)], { when: { id: K } }))];
  const blade = (b, p, t = E1) => [dmg(b, t), per(K), dmg(p, t), spendAll(K)];
  swap(j, [
    // u1 열기 — 농땡이(0코): 괴력을 채우고, 장도를 찾아와 값을 깎는다
    card(H, 1, '농땡이', 0, '스킬', [stk(K, 2), draw(1)], [
      O('연차를 몰라', [stk(K, 3), draw(1)]),
      O('사장님 몰래', [mine(2, { type: '공격' }), cheaper({ type: '공격' }), stk(K, 3)]),
      O('숨어서 쉬는 시간', [stk(K, 2), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('피로회복제 원샷', [disc(1), stk(K, 3), draw(2)]),
      O('일 떠넘기기', [exileBasic(1), stk(K, 4)]),
    ], [B('말 걸면 일단 경계', 'atkUp'), B('월급통장', 'draw'), B('새벽 배달', [stk(K, 1)])]),
    // u2 굴리기 — 원작 저학년 님블 컷(그림 짝 = 저학년 아이콘)
    card(H, 2, '님블 컷', 1, '공격', nimble(0.4, 0.65), [
      O('잔상이 보일 만큼', nimble(0.5, 0.8)),
      O('단도라 부르기엔 큰 칼', nimble(0.3, 0.45), { cost: 0 }),
      O('깊은 마지막 타격', [dH(2, 0.4), stk(K, 1), ifStack(K, 3), dmg(1.0)]),
      O('대충 네 번', [dH(2, 0.42), per(K), dmg(0.3)]),
      O('땡땡이 칼질', [disc(1), dH(3, 0.45), stk(K, 2)]),
    ], [B('대충 휘두르기', 'power'), B('노점 계산대', 'ap'), B('잔상', [stk(K, 1)])]),
    // u3 강화 — 쓰다 만 논문: 괴력을 쏟을 때마다 남은 힘이 튄다(사도당 강화 기본형 1장)
    card(H, 3, '쓰다 만 논문', 1, '강화', thesis(0.15, 2, 0.5), [
      O('밤샘 초고', thesis(0.15, 2, 0.75)),
      O('한 줄만 쓰기', thesis(0.12, 1, 0.4), { cost: 0 }),
      O('셰럼의 조언', [atkRun(0.2), mine(1, { type: '공격' }), power(rule('spend', [dmg(0.8, ER)], { when: { id: K } }))]),
      O('반려된 초고', [disc(1), atkRun(0.25), power(rule('spend', [dmg(0.75, ER)], { when: { id: K } }))]),
      O('출근 전 초고', [stk(K, 3), power(rule('spend', [dmg(0.95, ER)], { when: { id: K } }), rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
    ], [B('수당 없는 야근', 'defUp'), B('극장 매표소', { tags: ['개전'] }), B('논문 반려', 'cost')]),
    // u4 터뜨리기 · 시동(2코) — 장도 뽑기(괴력 1개당 · 전부 소모 — 그림 짝 = 고학년 아이콘)
    card(H, 4, '장도 뽑기', 2, '공격', blade(1.6, 0.36), [
      O('이중 계약', blade(2.4, 0.55), { tags: ['분쇄'] }),
      O('반쯤 뽑기', blade(1.2, 0.45), { cost: 1, tags: ['분쇄'] }),
      O('괴력 해방', [dmg(5.0), per(K), dmg(1.0)], { tags: ['분쇄', '소멸'] }),
      O('교도소의 환대', [dmg(2.0), per(K), dmg(0.45), ifBroken, dmg(1.5)], { tags: ['분쇄'] }),
      O('레비드 더 섀도우', [dmg(1.7, EA), per(K), dmg(0.4, EA), pull(1, { who: 'self', type: '스킬' })], { tags: ['분쇄'] }),
    ], [B('비장의 장도', 'weakSpot'), B('UFC 알바', 'ap'), B('휴가를 모름', [stk(K, 2)])], { tags: ['분쇄'] }),
  ]);
  // u5 굴리기(스킬) — 평균치 포션: 포셔의 약화 포션으로 힘을 누르는 동안 실드 · 괴력(공격 셋 · 강화 하나라 스킬 쪽)
  j.cards.push(card(H, 5, '평균치 포션', 1, '스킬', [sh(1.1), stk(K, 2)], [
    O('신탁 1', [sh(1.45), stk(K, 2)]),
    O('신탁 2', [sh(1.1), stk(K, 2)], { tags: ['보존'] }),
    O('신탁 3', [sh(1.0), stk(K, 2), mine(1, { type: '공격' })]),
    O('신탁 4', [sh(1.0), stk(K, 1), power(rule('spend', [sh(0.6)], { when: { id: K } }))], { power: true }),
    O('신탁 5', [disc(1), sh(1.15), stk(K, 3)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]));
  starter(j, '레비_u4');
  for (const e of j.equips || []) e.affinityEffect = [P('참았던 괴력', 'lowHp', [stk(K, 5)], { when: { pct: 0.3 } })];
}

// ════════════════════════════════════════════════════════════════════
// 2. 레비(졸업) — 탱커 · 활발(원작 방식 고학년 「어수선한 영역」 유지). 롤모델을 따라 차오르는 「신입의 패기」 — 넷이면 각성 드링크.
//    3단계: ④ 정규직 계약서를 「패기를 세는 1코 공격」 으로, 옛 강화 엔진(결의 · 각성마다 광역)은 그 카드의 D 갈래로.
// ════════════════════════════════════════════════════════════════════
function leviGrad(j) {
  const H = '레비_졸업', K = '신입의 패기', ZONE = '어수선한 영역';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '정규직 첫날의 넘치는 의욕', carrier: 'self', cap: 4,
    rules: [{ name: '각성 드링크', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), heal(0.35)] }],
  };
  h.keywords = h.keywords.filter(k => k.name === ZONE);
  const keep = h.passives.find(p => p.name === '변하지 않는 사이');
  h.passives = [P('롤모델 따라 하기', 'play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: perTurn(1) }), keep];
  const blast = (r, w = 1, k = 1) => [ddH(3, r, EA), st('약화', w, EA), stk(K, k)];
  const home = (b, p, t = [spendAll(K)]) => [sh(b), per(K), sh(p), ...t];
  const deal = (b, p) => [ddef(b), per(K), ddef(p)];
  swap(j, [
    // u1 열기 — 출근 도장(시동 카드)
    card(H, 1, '출근 도장', 1, '스킬', [stk(K, 2), sh(0.7)], [
      O('당당한 출근', [stk(K, 3), sh(1.1)]),
      O('첫 출근', [stk(K, 2), sh(0.9)], { tags: ['개전'] }),
      O('매일 아침 출근 도장', [stk(K, 1), sh(0.8), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('선배 따라 하기', [stk(K, 3), draw(2, { who: 'other', type: '공격' })]),
      O('미리 결재', [stk(K, 2), sh(0.9), ifWounded, heal(0.9)]),
    ], [B('사원증', 'guard'), B('커피 한 잔', 'draw'), B('출근 카드', [stk(K, 1)])]),
    // u2 굴리기 — 원작 저학년 「패기 가득한 인재」(약화 담당 · 그림 짝 = 저학년 아이콘)
    card(H, 2, '패기 가득한 인재', 1, '공격', blast(0.13), [
      O('신입의 기합', blast(0.21)),
      O('작은 패기', blast(0.12), { cost: 0 }),
      O('서류 뭉치', [ddH(3, 0.14, EA), st('약화', 1, EA), per(K), sh(0.45)]),
      O('각성 드링크 원샷', [ddH(3, 0.16, EA), st('약화', 1, EA), ifStack(K, 3), heal(0.95)]),
      O('패기 과잉', [ddH(3, 0.3, EA), st('약화', 1, EA), spendN(K, 2)]),
    ], [B('신입의 열정', 'power'), B('서류 정리', 'ap'), B('면역 드링크', [cleanse(1)])]),
    // u3 터뜨리기 — 정시 퇴근(패기 1개당 실드 · 전부 소모 — 각성 드링크와 맞바꿈)
    card(H, 3, '정시 퇴근', 1, '스킬', home(0.8, 0.35), [
      O('칼퇴', home(1.0, 0.45)),
      O('반차 신청', home(0.6, 0.3), { cost: 0 }),
      O('주 4일제', [sh(1.2), power(rule('turnEnd', [per(K), sh(0.35)]))], { power: true }),
      O('야근 각오', home(0.95, 0.4, [])),
      O('퇴근길 서류 폭탄', [ddef(0.3, EA), per(K), ddef(0.15, EA), spendAll(K)]),
    ], [B('퇴근 준비 완료', 'guard'), B('칼같은 시계', 'cost'), B('퇴근길 간식', [stk(K, 1)])]),
    // u4 완성형 — 정규직 계약서(패기 1개당 · 패기는 남김 — 1코 마무리). 옛 강화 엔진은 「정규직」 갈래로
    card(H, 4, '정규직 계약서', 1, '공격', deal(0.38, 0.16), [
      O('연봉 협상', deal(0.6, 0.26)),
      O('수습 계약', deal(0.35, 0.15), { cost: 0 }),
      O('정규직', [st('결의', 1), stk(K, 1), power(rule('stackReach', [ddef(0.45, EA)], reach(K, 4)))], { power: true }),
      O('팀장 승진', [ifStack(K, 3), ddef(1.2), stk(K, 1)]),
      O('계약 해지', [ddef(0.55), per(K), ddef(0.3), spendAll(K)]),
    ], [B('정규직의 무게', 'weakSpot'), B('명함', 'draw'), B('사내 복지', [st('결의', 1)])]),
  ]);
  // u5 유틸(0코 스킬) — 견습 지침서: 뒤따라올 견습들에게 공짜로 푸는 지침서 — 다른 아군 카드 · 패기
  j.cards.push(card(H, 5, '견습 지침서', 0, '스킬', [stk(K, 1), sh(0.5), draw(1, { who: 'other' })], [
    O('신탁 1', [stk(K, 2), sh(0.65), draw(1, { who: 'other' })]),
    O('신탁 2', [stk(K, 1), sh(0.5), draw(2, { who: 'other', type: '공격' })]),
    O('신탁 3', [sh(0.4), draw(1, { who: 'other' }), power(rule('play', [stk(K, 1)], { when: { who: 'other', type: '스킬' }, limit: 1 }))], { power: true }),
    O('신탁 4', [stk(K, 1), sh(0.5), ifStack(K, 3), draw(2)]),
    O('신탁 5', [spendN(K, 2), sh(1.2), draw(2, { who: 'other' })]),
  ], [B('축복 1', 'defUp'), B('축복 2', { tags: ['보존'] }), B('축복 3', [stk(K, 1)])]));
  starter(j, '레비_졸업_u1');
}

// ════════════════════════════════════════════════════════════════════
// 3. 롤렛 — 딜러 · 광기. 버려진 카드가 트릭이 된다 — 「갈채」 다섯이면 피날레.
//    3단계: 생성 카드 「하얀 비둘기」(버려지면 갈채) · 연계 채우기(동료의 무대에도 박수) · ④ 관객의 갈채를 갈채를 세는 1코 공격으로.
// ════════════════════════════════════════════════════════════════════
function rollett(j) {
  const H = '롤렛', K = '갈채', DOVE = '롤렛_dove';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '봉봉한 트릭에 쏟아지는 박수', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [{ name: '피날레', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), dmg(1.2, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    P('소매 속 트릭', 'discard', [stk(K, 1)], { limit: perTurn(3) }),
    P('관객의 환호', 'play', [stk(K, 1)], { when: { who: 'other' }, limit: perTurn(2) }),
  ];
  h.ult.fx = refKw(h.ult.fx, '바꿔치기', K);
  token(j, { id: DOVE, name: '하얀 비둘기', cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(0.35, ER), onDiscard, stk(K, 2)], blurb: '모자에서 나와 관객 머리 위를 한 바퀴. 사라질 때 박수가 더 큽니다' });
  const show = (r, k1, k2, t = EA) => [dmg(r, t), stk(K, k1), onDiscard, stk(K, k2)];
  const trick = (b, p, t = E1) => [dmg(b, t), per(K), dmg(p, t), spendAll(K)];
  const clap = (b, p) => [dmg(b), per(K), dmg(p, ER)];
  swap(j, [
    // u1 열기 — 비둘기 부활 마술(시동 카드): 한 장 사라지고 두 장 나타난다
    card(H, 1, '비둘기 부활 마술', 0, '스킬', [disc(1), draw(2)], [
      O('비둘기 떼', [disc(2), draw(3)]),
      O('박수 유도', [disc(1), draw(3)], { tags: ['보존'] }),
      O('다시 부활', [disc(1), draw(2), power(rule('turnStart', [stk(K, 2)]))], { power: true }),
      O('두 번 접은 카드', [disc(1), draw(2, { who: 'self' }), stk(K, 1)]),
      O('모자 속 비둘기', [disc(1), make(DOVE, 2)]),
    ], [B('모나미', 'atkUp'), B('소매 속 카드', 'draw'), B('비둘기 모자', [make(DOVE, 1)])]),
    // u2 굴리기 — 원작 저학년 「갈채를 먹는 엔터테이너」(그림 짝 = 저학년 아이콘)
    card(H, 2, '갈채를 먹는 엔터테이너', 1, '공격', show(0.9, 1, 2), [
      O('앙코르 무대', show(1.15, 1, 2)),
      O('울랄라', show(0.7, 1, 2), { cost: 0 }),
      O('호기심 상자', [dmg(1.1, EA), stk(K, 1), draw(2, { who: 'self', type: '공격' })]),
      O('비둘기 쇼', [dmg(0.9, EA), make(DOVE, 1)]),
      O('앙코르 강요', [dmg(1.6, EA), spendN(K, 2)]),
    ], [B('스포트라이트', 'power'), B('봉봉하게', 'ap'), B('박수 부대', [stk(K, 1)])]),
    // u3 터뜨리기 — 트릭 카드(갈채 1개당 · 전부 소모 — 피날레와 맞바꿈)
    card(H, 3, '트릭 카드', 1, '공격', trick(0.8, 0.35), [
      O('날 선 트릭 카드', trick(1.0, 0.42)),
      O('가벼운 트릭', trick(0.5, 0.25), { cost: 0 }),
      O('조커', [dmg(1.3), stk(K, 1), onDiscard, stk(K, 3)]),
      O('카드 부채', [dmg(0.6, EA), perTag(DOVE), dmg(0.4, EA), exileAll(DOVE)]),
      O('비장의 카드', [dmg(1.7), per(K), dmg(0.7)], { tags: ['소멸'] }),
    ], [B('트럼프 한 벌', 'power'), B('숨긴 카드', 'cost'), B('파흐돈', 'weakSpot')]),
    // u4 완성형 — 관객의 갈채(갈채 1개당 · 갈채는 남김 — 1코 마무리). 옛 강화 엔진은 「환호성」 갈래로
    card(H, 4, '관객의 갈채', 1, '공격', clap(0.6, 0.22), [
      O('기립 박수', clap(0.78, 0.28)),
      O('작은 박수', clap(0.45, 0.17), { cost: 0 }),
      O('환호성', [stk(K, 3), power(rule('discard', [dmg(0.5, ER)], { limit: 3 }))], { power: true }),
      O('비둘기 커튼콜', [dmg(0.7), perTag(DOVE), dmg(0.45, ER)]),
      O('마지막 박수', [dmg(0.8), per(K), dmg(0.32, ER), spendAll(K)]),
    ], [B('최고의 엔터테인먼트', 'weakSpot'), B('지팡이 돌리기', 'draw'), B('앙코르', [stk(K, 1)])]),
  ]);
  // u5 굴리기(스킬) — 투명 의자: 다리와 지팡이만으로 버티는 폼 — 실드 · 갈채(공격 셋이라 스킬 쪽)
  j.cards.push(card(H, 5, '투명 의자', 1, '스킬', [sh(1.1), stk(K, 2)], [
    O('신탁 1', [sh(1.45), stk(K, 2)]),
    O('신탁 2', [sh(1.0), stk(K, 1), onDiscard, stk(K, 3)]),
    O('신탁 3', [sh(0.8), stk(K, 1), make(DOVE, 1)]),
    O('신탁 4', [sh(1.2), stk(K, 1), power(rule('discard', [sh(0.6)], { limit: 2 }))], { power: true }),
    O('신탁 5', [disc(2), sh(1.3), draw(2)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'cost'), B('축복 3', [make(DOVE, 1)])]));
  starter(j, '롤렛_u1');
}

// ════════════════════════════════════════════════════════════════════
// 4. 마카샤 — 서포터(축: 버퍼) · 활발. 적에게 「운명의 시계」를 감아 두면 적의 차례마다 한 칸씩 돌고, 다 닳으면 적혀 있던 결말이 떨어진다.
//    3단계: ④ 책략관의 탁상을 「시계를 세는 1코 공격」 으로(시계는 남김), 옛 엔진(사기 · 결말마다 취약 + 다음 예언)은 D 갈래로.
// ════════════════════════════════════════════════════════════════════
function makasha(j) {
  const H = '마카샤', K = '운명의 시계';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '세 칸 뒤에 결말이 오는 시곗바늘', carrier: 'enemy', cap: 3, decay: 1,
    rules: [{ name: '적혀 있던 결말', when: { on: 'stackGone', id: K }, fx: [ddef(1.6), st('취약', 1, E1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('예언 떠벌리기', 'play', [spendN(K, 1)], { when: { type: '공격' }, limit: perTurn(2) }),
    P('이미 본 결말', 'fightStart', [stk(K, 3, EA)]),
  ];
  h.ult.fx = refKw(h.ult.fx, '예언', K);
  const clock = (v, extra = [], t = E1) => [stk(K, v, t), ...extra];
  const ending = (b, p, t = [spendAll(K)]) => [ddef(b), per(K), ddef(p), ...t];
  const chatter = (r, v, extra = [draw(1)]) => [ddef(r), spendN(K, v), ...extra];
  swap(j, [
    // u1 열기 — 언젠가 일어날 일(시동 카드 · 원작 저학년 · 그림 짝 = 저학년 아이콘)
    card(H, 1, '언젠가 일어날 일', 1, '스킬', clock(3, [draw(1)]), [
      O('또렷한 예언', clock(3, [draw(2)])),
      O('성급한 예언', clock(2, [draw(1)]), { cost: 0 }),
      O('다음 날의 쪽지', [stk(K, 3, E1), draw(1), power(rule('turnStart', [stk(K, 1, 'topEnemy')]))], { power: true }),
      O('예언과 처방', [stk(K, 3, E1), pull(2, { who: 'other' })]),
      O('겹예언', [stk(K, 2, EA), ddef(0.35, EA), draw(1)]),
    ], [B('브로치 우물거리기', { tags: ['보존'] }), B('절굿공이', 'draw'), B('이-히힛', [stk(K, 1, E1)])]),
    // u2 터뜨리기 — 적혀 있던 결말(남은 시계 1칸당 · 전부 소모하면 결말이 바로 떨어진다)
    card(H, 2, '적혀 있던 결말', 1, '공격', ending(0.55, 0.3), [
      O('결말 낭독', ending(0.7, 0.4)),
      O('그러니까 말했잖아~', ending(0.5, 0.3), { tags: ['약점 공격'] }),
      O('마녀 탐정 바바', [ddef(0.55), per(K), ddef(0.3), mine(1)]),
      O('정해진 결말 비틀기', [ddef(0.8), ifStack(K, 1), ddef(1.0), spendAll(K)]),
      O('남겨 둔 결말', ending(0.7, 0.3, [])),
    ], [B('뱀잡이 대장', 'power'), B('지팡이 내려치기', 'ap'), B('예언 쪽지', [stk(K, 1, E1)])]),
    // u3 굴리기 — 생각이 줄줄(시곗바늘 한 칸 앞당김 + 드로우)
    card(H, 3, '생각이 줄줄', 0, '공격', chatter(0.35, 1), [
      O('수다 두 바가지', chatter(0.45, 2)),
      O('귓속말', chatter(0.45, 1, [draw(1, { who: 'other' })])),
      O('딴생각 흘리기', [ddef(0.75), draw(1), power(rule('turnStart', [spendN(K, 1)]))], { power: true }),
      O('말 끊기 싫음', [ddef(0.35), spendN(K, 1), inspire, draw(2)]),
      O('수다 폭주', [disc(1), ddef(0.9), spendN(K, 2)]),
    ], [B('긴긴-팔다리', 'draw'), B('미숫가루 한 잔', [heal(0.3)]), B('노을 보며 멍', 'weakSpot')]),
    // u4 완성형 — 책략관의 탁상(남은 시계 1칸당 · 시계는 남김 — 1코 마무리). 옛 강화 엔진은 「수 읽기」 갈래로
    card(H, 4, '책략관의 탁상', 1, '공격', ending(0.6, 0.25, []), [
      O('완벽한 책략', ending(0.75, 0.32, [])),
      O('발자국 남기기', ending(0.45, 0.2, []), { cost: 0 }),
      O('수 읽기', [st('사기', 1), power(rule('stackGone', [st('취약', 1, E1), stk(K, 2, 'topEnemy')], { when: { id: K } }))], { power: true }),
      O('미리 펼친 지도', [stk(K, 3, EA), draw(2, { who: 'other' })]),
      O('이-히힛 결말', ending(0.8, 0.35)),
    ], [B('궁극의 절구통', 'frost'), B('쓴 커피', 'ap'), B('바바의 발자국', [st('사기', 1)])]),
  ]);
  // u5 굴리기(스킬) — 닭다리 집 바바: 살아 있는 집이 막아서는 사이 시곗바늘을 감는다(공격 셋이라 스킬 쪽)
  j.cards.push(card(H, 5, '닭다리 집 바바', 1, '스킬', [sh(1.0), stk(K, 2, E1)], [
    O('신탁 1', [sh(1.3), stk(K, 2, E1)]),
    O('신탁 2', [sh(1.1), stk(K, 2, EA)]),
    O('신탁 3', [sh(0.7), stk(K, 2, E1), pull(1, { who: 'other' })]),
    O('신탁 4', [sh(1.1), stk(K, 1, E1), power(rule('stackGone', [sh(1.0)], { when: { id: K } }))], { power: true }),
    O('신탁 5', [disc(1), sh(1.0), stk(K, 3, E1)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1, E1)])]));
  starter(j, '마카샤_u1');
}

// ════════════════════════════════════════════════════════════════════
// 5. 바리에 — 서포터(축: 드로우) · 우울. 북카트를 끌며 책을 나른다 — 「북카트」 셋이면 서가 정리(헌 책 2권 + 빌린 책).
//    3단계: 생성 카드 「헌 책」(다른 아군 카드 드로우) · 기본 카드 회수 · ④ 서가 정리를 「손의 헌 책을 세는 1코 스킬」 로.
// ════════════════════════════════════════════════════════════════════
function barie(j) {
  const H = '바리에', K = '북카트', BOOK = '빌린 책', OLD = '바리에_book';
  const h = j.heroes[0];
  const book = { ...h.keywords.find(k => k.name === BOOK), per: [{ stat: 'dealt', v: 0.15 }] };
  h.keyword = {
    name: K, desc: '고서를 가득 실은 사서의 수레', carrier: 'self', cap: 3,
    rules: [{ name: '서가 정리', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), make(OLD, 2), stk(BOOK, 2, 'strongestAlly')] }],
  };
  h.keywords = [book];
  h.passives = [
    P('북카트 순회', 'play', [stk(K, 1)], { limit: perTurn(3) }),
    P('당일 반납', 'stackGone', [draw(1), sh(0.5)], { when: { id: BOOK, decay: true }, limit: perTurn(1) }),
  ];
  token(j, { id: OLD, name: '헌 책', cost: 0, type: '스킬', tags: ['소멸'], fx: [draw(1, { who: 'other' }), heal(0.4)], blurb: '분리수거장에서 건진 고서. 펼치면 누군가의 비법이 적혀 있습니다' });
  const lend = (v, extra) => [stk(BOOK, v, 'oneAlly'), ...extra];
  const cart = (s, extra = [stk(BOOK, 1, 'strongestAlly')]) => [sh(s), stk(K, 1), ...extra];
  const dun = (b, p, t = E1, tail = [spendAll(K)]) => [ddef(b, t), per(K), ddef(p, t), ...tail];
  const shelf = (b, p) => [sh(b), perTag(OLD), sh(p), stk(BOOK, 1, 'strongestAlly')];
  swap(j, [
    // u1 열기 — 도서 대출(시동 카드 · 아군 1명에게 빌린 책 + 다른 아군 카드)
    card(H, 1, '도서 대출', 0, '스킬', lend(2, [draw(1, { who: 'other' })]), [
      O('두 권 대출', lend(3, [draw(1, { who: 'other' })])),
      O('추천 도서', lend(3, [draw(2, { who: 'other', type: '공격' })])),
      O('연장 대출', [stk(BOOK, 1, 'oneAlly'), draw(1, { who: 'other' }), power(rule('turnStart', [stk(BOOK, 1, 'strongestAlly')]))], { power: true }),
      O('예약 도서', lend(2, [make(OLD, 1)])),
      O('반납 독촉', [pull(1, { basic: true }), stk(BOOK, 3, 'oneAlly')]),
    ], [B('책 모자', { tags: ['보존'] }), B('대출 카드', 'draw'), B('헌 책 한 권', [make(OLD, 1)])]),
    // u2 굴리기 — 원작 저학년 「책을 정리해주세요오」(그림 짝 = 저학년 아이콘)
    card(H, 2, '책을 정리해주세요오', 1, '스킬', cart(1.6), [
      O('고서 카트', cart(1.6)),
      O('책 한 칸', cart(0.75), { cost: 0 }),
      O('분리수거장의 헌 책', [sh(1.8), stk(K, 1), pull(1, { who: 'other' })]),
      O('북카트 들이받기', [ddef(0.35), stk(K, 1), make(OLD, 1)]),
      O('정숙 경고', [disc(1), sh(2.1), stk(K, 2)]),
    ], [B('제자리에', 'guard'), B('단것부터', 'draw'), B('정숙 표지판', [stk(K, 1)])]),
    // u3 터뜨리기 — 연체자 독촉(괴력의 사서 · 북카트 1칸당 · 전부 소모 — 서가 정리와 맞바꿈)
    card(H, 3, '연체자 독촉', 1, '공격', dun(0.72, 0.3), [
      O('몽둥이 독촉', dun(0.65, 0.25)),
      O('쉿', dun(0.4, 0.18), { cost: 0 }),
      O('연체 고지서', dun(0.75, 0.32, E1, [])),
      O('잉크 폭탄', [...dun(0.85, 0.34, E1, []), ifKill, pull(1)]),
      O('헌 책 압수', [ddef(0.35), per(K), ddef(0.13), make(OLD, 1)]),
    ], [B('고서 카트 들이받기', 'power'), B('연체료', 'ap'), B('정숙해주세요오', [st('약화', 1, E1)])]),
    // u4 완성형 — 사서의 서가 정리(손의 헌 책 1권당 실드 + 빌린 책 — 1코 마무리). 옛 강화 엔진은 「신착 도서」 갈래로
    card(H, 4, '사서의 서가 정리', 1, '스킬', shelf(0.7, 0.35), [
      O('대청소', shelf(0.9, 0.45)),
      O('분류 번호', shelf(0.5, 0.25), { cost: 0 }),
      O('신착 도서', [st('사기', 1), power(rule('turnStart', [stk(BOOK, 1, 'strongestAlly')]))], { power: true }),
      O('개관 준비', [pull(1, { who: 'other' }), make(OLD, 1)]),
      O('벨리타가 인정한 학위', [sh(1.05), perTag(OLD), sh(0.55), spendAll(K)]),
    ], [B('셰럼 몰래', 'guard'), B('만년 막내', { tags: ['개전'] }), B('헌 책 사랑', [stk(K, 1)])]),
  ]);
  // u5 굴리기(공격) — 책 아령: 마녀들이 아령으로 쓰던 그 책으로(스킬 셋이라 공격 쪽)
  j.cards.push(card(H, 5, '책 아령', 1, '공격', [ddef(0.8), stk(K, 1)], [
    O('신탁 1', [ddef(1.05), stk(K, 1)]),
    O('신탁 2', [ddef(0.55, EA), stk(K, 1)]),
    O('신탁 3', [ddef(0.6), make(OLD, 1)]),
    O('신탁 4', [ddef(0.6), stk(K, 1), power(rule('stackReach', [ddef(0.5, EA)], reach(K, 3)))], { power: true }),
    O('신탁 5', [disc(1), ddef(0.8), stk(K, 2)]),
  ], [B('축복 1', 'weakSpot'), B('축복 2', 'cost'), B('축복 3', [make(OLD, 1)])]));
  starter(j, '바리에_u1');
}

// ════════════════════════════════════════════════════════════════════
// 6. 벨리타 — 딜러 · 광기. 몰래 초콜릿 — 먹으면 「당 수치」가 올라 당쇼크(고학년 게이지), 쥐고 있으면 디멘션이 커진다.
//    3단계: ④ 여왕의 간식 시간을 「당을 세는 1코 공격」 으로, 옛 엔진(당쇼크마다 광역)은 D 갈래로.
// ════════════════════════════════════════════════════════════════════
function belita(j) {
  const H = '벨리타', K = '당 수치', CHOC = '벨리타_t1';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '몰래 먹은 초콜릿만큼 오르는 여왕의 당', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [{ name: '당쇼크', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), gauge(40)] }],
  };
  delete h.keywords;
  h.passives = [
    P('몰래 초콜릿', 'turnStart', [make(CHOC, 1)]),
    P('여왕의 심판', 'ult', [dmg(0.6, EA)]),
  ];
  const choc = j.cards.find(c => c.id === CHOC);
  Object.assign(choc, { cost: 0, type: '스킬', tags: ['소멸'], fx: [stk(K, 1), draw(1)] });
  const dim = (b, p, t = EA) => [dmg(b, t), perTag(CHOC), dmg(p, t)];
  const rain = (n, r, k = 1) => [hits(n, r), make(CHOC, k)];
  const tea = (b, p) => [dmg(b), per(K), dmg(p, ER)];
  swap(j, [
    // u1 터뜨리기 — 원작 저학년 디멘션 오브 위치(손의 초콜릿 1장당 · 그림 짝 = 저학년 아이콘)
    card(H, 1, '디멘션 오브 위치', 1, '공격', dim(0.7, 0.2), [
      O('블랙홀 오브 위치', dim(0.9, 0.25)),
      O('작은 차원', dim(0.55, 0.18), { cost: 0 }),
      O('차원 문 열기', [dmg(0.75, EA), perTag(CHOC), dmg(0.25, EA), mine(1, { type: '스킬' })]),
      O('당 충전 차원', [dmg(0.8, EA), per(K), dmg(0.2, EA)]),
      O('간식 털어 넣기', [dmg(0.75, EA), perTag(CHOC), dmg(0.32, EA), exileAll(CHOC)]),
    ], [B('여왕의 마력', 'power'), B('후으', 'ap'), B('뿌리의 가시', 'weakSpot')]),
    // u2 열기 — 비밀 간식 창고(시동 카드)
    card(H, 2, '비밀 간식 창고', 0, '스킬', [make(CHOC, 2)], [
      O('창고 대방출', [make(CHOC, 3)]),
      O('몽유병 창고털이', [make(CHOC, 2), power(rule('turnStart', [make(CHOC, 1)]))], { power: true }),
      O('시스트 암거래', [make(CHOC, 2), draw(1, { who: 'self', type: '공격' }), stk(K, 2)]),
      O('건강식 먹는 척', [make(CHOC, 2), inspire, make(CHOC, 2)]),
      O('창고 정리', [disc(1), make(CHOC, 3)]),
    ], [B('목각 인형', { tags: ['보존'] }), B('서마터펀 주문', 'draw'), B('로켓 목걸이', [stk(K, 1)])]),
    // u3 굴리기 — 핏빛 소나기(무작위 다섯 번 + 초콜릿)
    card(H, 3, '핏빛 소나기', 1, '공격', rain(5, 0.25), [
      O('크림슨 소나기', rain(5, 0.33)),
      O('보슬비', rain(3, 0.25), { cost: 0 }),
      O('당 떨어진 짜증', [hits(5, 0.3), ifStack(K, 3), hits(4, 0.3)]),
      O('집중 호우', [dH(4, 0.36), stk(K, 2)], { tags: ['약점 공격'] }),
      O('엘리아스의 수호자들', [disc(1), hits(6, 0.3), make(CHOC, 2)]),
    ], [B('핏빛 마력', 'power'), B('굵은 빗방울', 'ap'), B('초콜릿 한 입', [make(CHOC, 1)])]),
    // u4 완성형 — 여왕의 간식 시간(당 1개당 · 당은 남김 — 1코 마무리). 옛 강화 엔진은 「티타임」 갈래로
    card(H, 4, '여왕의 간식 시간', 1, '공격', tea(0.6, 0.22), [
      O('성대한 간식', tea(0.78, 0.28)),
      O('한 입만', tea(0.45, 0.17), { cost: 0 }),
      O('티타임', [make(CHOC, 1), power(rule('stackReach', [dmg(0.85, EA)], reach(K, 5)))], { power: true }),
      O('초콜릿 폭식', [perTag(CHOC), dmg(0.4, ER), exileAll(CHOC), stk(K, 2)]),
      O('당쇼크 직전', [dmg(0.85), per(K), dmg(0.32, ER), spendAll(K)]),
    ], [B('뿌리의 수호자', 'weakSpot'), B('민트초코 질색', 'draw'), B('비밀 창고', [make(CHOC, 1)])]),
  ]);
  // u5 유틸(0코 스킬) — 엘프깡 한 봉지: 당을 올리며 공격 고유 카드를 찾아온다(공격 셋이라 스킬 쪽)
  j.cards.push(card(H, 5, '엘프깡 한 봉지', 0, '스킬', [sh(0.5), stk(K, 1), mine(1, { type: '공격' })], [
    O('신탁 1', [sh(0.7), stk(K, 2), mine(1, { type: '공격' })]),
    O('신탁 2', [sh(0.5), stk(K, 1), mine(1, { type: '공격' })], { tags: ['보존'] }),
    O('신탁 3', [make(CHOC, 1), sh(0.4), mine(1, { type: '공격' })]),
    O('신탁 4', [sh(0.5), stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
    O('신탁 5', [perTag(CHOC), sh(0.3), exileAll(CHOC), stk(K, 3)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]));
  starter(j, '벨리타_u2');
  for (const e of j.equips || []) e.affinityEffect = [P('비밀 창고 열쇠', 'fightStart', [make(CHOC, 1)])];
}

// ════════════════════════════════════════════════════════════════════
// 7. 벨벳 — 탱커 · 냉정. 맞을수록 「근력 강화」 마법이 불어난다 — 쥐고 버틸까(받는 피해↓), 원심 분리 펀치로 쏟을까.
//    3단계: 시동 = 개전 강화(이두근 펌핑 — 매 턴 근력) · 기본 카드 연료(기본기 단련) · ④ 단련법을 「근력 다섯 문턱의 1코 공격」 으로.
// ════════════════════════════════════════════════════════════════════
function velvet(j) {
  const H = '벨벳', K = '근력 강화';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '마법으로 부풀린 힘의 마녀의 근육', carrier: 'self', cap: 5, per: [{ stat: 'taken', v: -0.04 }] };
  delete h.keywords;
  h.passives = [
    P('스스로 지키는 힘', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: perTurn(2) }),
    P('배신하지 않는 근육', 'stackReach', [st('불굴', 1)], { when: { id: K, n: 5 }, limit: perFight(1) }),
  ];
  h.ult.fx = refKw(h.ult.fx, '근력', K);
  const pump = (k, s, extra = []) => [stk(K, k), sh(s), ...extra, power(rule('turnStart', [stk(K, 1)]))];
  const dare = (s, c = 1) => [sh(s), st('반격', c), stk(K, 1)];
  const punch = (b, p, t = E1, tail = [spendAll(K)]) => [ddef(b, t), per(K), ddef(p, t), ...tail];
  const drill = (b, p) => [ddef(b), ifStack(K, 5), ddef(p)];
  swap(j, [
    // u1 열기 · 시동 — 이두근 펌핑(개전 강화: 첫 턴에 켜 두면 매 턴 근력)
    card(H, 1, '이두근 펌핑', 1, '강화', pump(1, 0.6), [
      O('삼두근까지', pump(2, 0.8), { tags: ['개전'] }),
      O('근육교 집회', pump(2, 0.9)),
      O('아침 운동', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('기본기 단련', [stk(K, 1), draw(2, { basic: true, type: '공격' }), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('근육 시범', [ddef(0.8), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
    ], [B('정복 계획', 'defUp'), B('흠~', 'cost'), B('생딸기', [stk(K, 1)])], { tags: ['개전'] }),
    // u2 굴리기 — 원작 저학년 「그냥 다 덤벼!」(그림 짝 = 저학년 아이콘)
    card(H, 2, '그냥 다 덤벼!', 1, '스킬', dare(1.0), [
      O('왕좌에 앉은 벨벳', dare(1.4)),
      O('캬하', dare(0.75), { cost: 0 }),
      O('마녀 여왕 벨벳', [sh(0.8), st('반격', 1), power(rule('turnStart', [st('반격', 1)]))], { power: true }),
      O('나를 지키는 근육', [sh(1.2), st('반격', 1), per(K), sh(0.3)]),
      O('민심을 잡아라', [sh(1.5), st('반격', 2), spendN(K, 2)]),
    ], [B('도끼 방패', 'guard'), B('엿 날리기', 'draw'), B('아이쒸', [stk(K, 1)])]),
    // u3 터뜨리기 — 원심 분리 펀치(근력 1개당 · 전부 소모)
    card(H, 3, '원심 분리 펀치', 1, '공격', punch(0.45, 0.2), [
      O('풀스윙', punch(0.6, 0.25), { tags: ['분쇄'] }),
      O('어퍼컷', punch(0.45, 0.2), { tags: ['분쇄', '약점 공격'] }),
      O('회전 가속', [ddef(0.45), per(K), ddef(0.2), mine(1)], { tags: ['분쇄'] }),
      O('근육 자랑', punch(0.55, 0.22, E1, []), { tags: ['분쇄'] }),
      O('배신은 빛보다 빠르게', [per(K), ddef(0.4), spendAll(K), ifKill, stk(K, 3)]),
    ], [B('도끼 마법', 'power'), B('끝에 욕심', 'ap'), B('뚝배기 수집', 'weakSpot')], { tags: ['분쇄'] }),
    // u4 완성형 — 고위 마녀의 단련법(근력이 다섯이면 한 방 더 — 1코 마무리). 옛 강화 엔진은 「맞으면서 단련」 갈래로
    card(H, 4, '고위 마녀의 단련법', 1, '공격', drill(0.6, 1.1), [
      O('지옥 훈련', drill(0.75, 1.4)),
      O('가벼운 단련', drill(0.45, 0.85), { cost: 0 }),
      O('맞으면서 단련', [stk(K, 2), power(rule('hurt', [ddef(0.75)], { when: { guarded: true }, limit: 2 }))], { power: true }),
      O('버티는 단련', [sh(2.2), per(K), sh(0.45)]),
      O('새벽 단련', [ddef(0.8), ifStack(K, 5), ddef(1.6), spendAll(K)]),
    ], [B('근력 강화 마법', 'weakSpot'), B('코코넛 솔잎 볶음', 'draw'), B('근육 펌프', [stk(K, 1)])]),
  ]);
  // u5 유틸(0코 스킬) — 솔즙 무료 나눔: 요정 정복용이던 솔즙 — 실드 · 근력 · 드로우(공격 둘 · 스킬 하나 · 강화 하나)
  j.cards.push(card(H, 5, '솔즙 무료 나눔', 0, '스킬', [sh(0.6), stk(K, 1)], [
    O('신탁 1', [sh(0.8), stk(K, 1)]),
    O('신탁 2', [sh(0.5), stk(K, 1), mine(1, { type: '공격' })]),
    O('신탁 3', [heal(0.8), stk(K, 1)]),
    O('신탁 4', [sh(0.4), stk(K, 1), power(rule('turnStart', [sh(0.4)]))], { power: true }),
    O('신탁 5', [exileBasic(1), sh(0.5), stk(K, 3)]),
  ], [B('축복 1', 'heal'), B('축복 2', 'defUp'), B('축복 3', [ifStack(K, 3), draw(1)])]));
  starter(j, '벨벳_u1');
}

// ════════════════════════════════════════════════════════════════════
// 8. 셰럼 — 탱커 · 순수(원작 방식 고학년 「회한의 영역」 유지). 맞을 때마다 받아 적는 「원고지」 — 넷이면 탈고(소설 카드).
//    3단계: ④ 서기관의 기록체를 「원고지를 세는 1코 공격」 으로, 옛 엔진(결정화 · 탈고마다 원고 속 기사)은 D 갈래로.
// ════════════════════════════════════════════════════════════════════
function sherum(j) {
  const H = '셰럼', K = '원고지', ZONE = '회한의 영역', T1 = '셰럼_t1', T2 = '셰럼_t2', T3 = '셰럼_t3';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '받아 적은 흑역사가 쌓이는 원고', carrier: 'self', cap: 4,
    rules: [{ name: '탈고', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), make(T1, 1)] }],
  };
  h.keywords = h.keywords.filter(k => k.name === ZONE);
  h.passives = [
    P('받아 적기', 'hurt', [stk(K, 1)], { limit: perTurn(2) }),
    P('아군관찰일지', 'fightStart', [stk(K, 2)]),
  ];
  h.ult.fx = refKw(h.ult.fx, '원고', K);
  const tk = id => j.cards.find(c => c.id === id);
  Object.assign(tk(T1), { name: '어둠 정령의 상처', cost: 0, type: '공격', tags: ['소멸'], fx: [ddef(0.5, EA), st('약화', 1, EA)], blurb: '저자명 벨라도나 S. 읽히는 쪽이 더 아픕니다' });
  Object.assign(tk(T2), { cost: 0, type: '스킬', tags: ['소멸'], fx: [sh(1.0)] });
  Object.assign(tk(T3), { cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.8), st('결의', 1)] });
  const archive = (s, k, extra = []) => [sh(s), stk(K, k), ...extra];
  const curtain = (s, c = 2) => [sh(s), st('피해 감소', c), stk(K, 1)];
  const read = (b, p, t = EA, tail = [spendAll(K)]) => [ddef(b, t), per(K), ddef(p, t), ...tail];
  const record = (b, p) => [ddef(b), per(K), ddef(p)];
  swap(j, [
    // u1 열기 — 위치 아카이브(시동 카드)
    card(H, 1, '위치 아카이브', 1, '스킬', archive(0.8, 2), [
      O('왕실 사료 전권', archive(1.0, 3)),
      O('정리된 서가', archive(0.8, 2), { tags: ['보존'] }),
      O('나무 윗치', [stk(K, 1), sh(0.8), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('기밀 문서', archive(1.0, 2, [draw(1, { who: 'self', type: '공격' })])),
      O('흑역사 보관함', [stk(K, 2), make(T3, 1)]),
    ], [B('깃펜', 'guard'), B('검은 망토', { tags: ['보존'] }), B('적지 말라면 그 말까지', [stk(K, 1)])]),
    // u2 굴리기 — 원작 저학년 전승의 커튼(피해 감소 담당 · 그림 짝 = 저학년 아이콘)
    card(H, 2, '전승의 커튼', 1, '스킬', curtain(1.0), [
      O('일곱 겹 커튼', curtain(1.3)),
      O('얇은 커튼', curtain(0.7, 1), { cost: 0 }),
      O('엘리아스실록', [sh(1.2), st('피해 감소', 2), draw(1, { who: 'other' })]),
      O('원고 속 기사', [sh(0.7), st('피해 감소', 1), make(T2, 1)]),
      O('전승 낭독', [sh(1.8), st('피해 감소', 2), spendN(K, 2)]),
    ], [B('두꺼운 커튼', 'guard'), B('사관의 길', 'draw'), B('사견 한 줄', [stk(K, 1)])]),
    // u3 터뜨리기 — 흑역사 낭독(원고지 1장당 광역 · 전부 소모 — 탈고와 맞바꿈)
    card(H, 3, '흑역사 낭독', 1, '공격', read(0.3, 0.15), [
      O('완결편 낭독', read(0.4, 0.2)),
      O('작가의 말', read(0.25, 0.12), { cost: 0 }),
      O('생생한 묘사', [ddef(0.4, EA), power(rule('turnEnd', [per(K), ddef(0.12, EA)]))], { power: true }),
      O('습작 한 편', read(0.38, 0.15, EA, [])),
      O('2쇄 증보판', [...read(0.3, 0.15, EA, []), ifKill, make(T1, 1)]),
    ], [B('처참한 필력', 'power'), B('깃펜 물기', 'ap'), B('교정한 원고', [make(T2, 1)])]),
    // u4 완성형 — 서기관의 기록체(원고지 1장당 · 원고지는 남김 — 1코 마무리). 옛 강화 엔진은 「사료와 전례」 갈래로
    card(H, 4, '서기관의 기록체', 1, '공격', record(0.6, 0.22), [
      O('궁정 서기관', record(0.78, 0.28)),
      O('가벼운 필기', record(0.45, 0.17), { cost: 0 }),
      O('사료와 전례', [st('결정화', 1), stk(K, 2), power(rule('stackReach', [make(T2, 1)], reach(K, 4)))], { power: true }),
      O('후대에 남기는 의미', [make(T3, 1), stk(K, 2)]),
      O('「~함.」', [ddef(0.85), per(K), ddef(0.3), spendAll(K)]),
    ], [B('서기관의 기록법', 'weakSpot'), B('검은 잉크', 'draw'), B('원고 마감', [st('결정화', 1)])]),
  ]);
  // u5 유틸(0코 공격) — 미행 기록: 정보 수집을 위한 미행 — 작은 피해 · 원고지 · 드로우
  j.cards.push(card(H, 5, '미행 기록', 0, '공격', [ddef(0.35), stk(K, 1)], [
    O('신탁 1', [ddef(0.45), stk(K, 1)]),
    O('신탁 2', [ddef(0.3), stk(K, 1), mine(1, { type: '스킬' })]),
    O('신탁 3', [ddef(0.27, EA), stk(K, 1)]),
    O('신탁 4', [ddef(0.25), stk(K, 1), power(rule('hurt', [ddef(0.2, ER)], { limit: 2 }))], { power: true }),
    O('신탁 5', [disc(1), ddef(0.35), stk(K, 3)]),
  ], [B('축복 1', 'frost'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]));
  starter(j, '셰럼_u1');
}

// ════════════════════════════════════════════════════════════════════
// 9. 스노키 — 탱커 · 우울. 동료가 칠 때마다 「밀두유」 한 병 — 셋째 병이면 실드 + 적 전체 취약.
//    3단계: 시동 = 개전 강화(경호원 출동 — 매 턴 밀두유) · ④ 불법 유통망을 「밀두유를 세는 1코 공격」 으로.
// ════════════════════════════════════════════════════════════════════
function snorky(j) {
  const H = '스노키', K = '밀두유';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '갈아 만든 콩물파의 비밀 상품', carrier: 'self', cap: 3,
    rules: [{ name: '세 병째', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), sh(0.6), st('취약', 1, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    P('두유 배달', 'play', [sh(0.2), stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: perTurn(2) }),
    P('의리의 대명사', 'shieldBreak', [st('취약', 1, E1)], { limit: perTurn(1) }),
  ];
  const guard = (k, s) => [stk(K, k), sh(s), power(rule('turnStart', [stk(K, 1)]))];
  const soy = (s, extra = []) => [sh(s), stk(K, 1), ...extra];
  const box = (b, p, t = EA, tail = [spendAll(K)]) => [ddef(b, t), per(K), ddef(p, t), ...tail];
  const net = (b, p) => [ddef(b), per(K), ddef(p)];
  swap(j, [
    // u1 열기 · 시동 — 경호원 출동(개전 강화: 첫 턴에 켜 두면 매 턴 밀두유)
    card(H, 1, '경호원 출동', 1, '강화', guard(1, 0.6), [
      O('철통 경호', guard(2, 0.8), { tags: ['개전'] }),
      O('돈을 모시는 오른팔', guard(2, 0.9)),
      O('대기 중인 경호원', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('경호 교대', [stk(K, 2), draw(1, { who: 'other', type: '공격' }), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('구역 순찰', [ddef(0.6, EA), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
    ], [B('부하 모집', 'defUp'), B('문워크', 'cost'), B('콩 한 줌', [stk(K, 1)])], { tags: ['개전'] }),
    // u2 굴리기 — 원작 저학년 불법 두유(실드 · 그림 짝 = 저학년 아이콘)
    card(H, 2, '불법 두유', 1, '스킬', soy(1.2), [
      O('냉장 두유', soy(1.5)),
      O('한 병 건네기', soy(0.9), { cost: 0 }),
      O('두유 영업', [sh(1.35), draw(1, { who: 'other', type: '공격' })]),
      O('갈아 만든 콩물', [heal(1.5), stk(K, 1)]),
      O('재고 처분', [sh(2.1), spendN(K, 1)]),
    ], [B('두유 상자 방패', 'guard'), B('의리', 'draw'), B('미숫가루', [heal(0.3)])]),
    // u3 터뜨리기 — 두유 상자 던지기(밀두유 1병당 광역 · 전부 소모 — 세 병째와 맞바꿈)
    card(H, 3, '두유 상자 던지기', 1, '공격', box(0.35, 0.15), [
      O('대형 상자', box(0.45, 0.2)),
      O('날아 차기', box(0.35, 0.15), { tags: ['약점 공격'] }),
      O('스물네 병', [ddef(0.45, EA), power(rule('stackReach', [ddef(0.6, EA)], reach(K, 3)))], { power: true }),
      O('유통 장부', [ddef(0.35, EA), per(K), ddef(0.15, EA), mine(1)]),
      O('상자째 투척', box(0.75, 0.3, EA, []), { tags: ['소멸'] }),
    ], [B('발차기 각도', 'power'), B('콩 금단 현상', 'ap'), B('밀두유 한 상자', 'frost')]),
    // u4 완성형 — 불법 유통망(밀두유 1병당 · 두유는 남김 — 1코 마무리). 옛 강화 엔진은 「삼각회」 갈래로
    card(H, 4, '불법 유통망', 1, '공격', net(0.6, 0.3), [
      O('전국 유통망', net(0.72, 0.35)),
      O('동네 유통', net(0.45, 0.22), { cost: 0 }),
      O('삼각회', [st('반격', 1), stk(K, 1), power(rule('play', [ddef(1.0, EA)], { when: { type: '공격', every: 3 } }))], { power: true }),
      O('경호 계약', [sh(1.6), per(K), sh(0.55)]),
      O('새벽 배송', [ddef(0.8), per(K), ddef(0.4), spendAll(K)]),
    ], [B('내 꿈은 빅보스', 'weakSpot'), B('금테 장신구', 'draw'), B('두목 자리', [stk(K, 1)])]),
  ]);
  // u5 유틸(0코 스킬) — 명절 선물 세트: 조직 시절 명절마다 돌리던 건강식 — 실드 · 밀두유 · 동료 공격 카드
  j.cards.push(card(H, 5, '명절 선물 세트', 0, '스킬', [sh(0.4), stk(K, 1), draw(1, { who: 'other', type: '공격' })], [
    O('신탁 1', [sh(0.6), stk(K, 1), draw(1, { who: 'other', type: '공격' })]),
    O('신탁 2', [sh(0.4), stk(K, 1), draw(1, { who: 'other', type: '공격' })], { tags: ['개전'] }),
    O('신탁 3', [heal(0.6), stk(K, 1), pull(1, { who: 'other' })]),
    O('신탁 4', [sh(0.6), stk(K, 1), power(rule('stackReach', [sh(0.9), draw(1, { who: 'other' })], reach(K, 3)))], { power: true }),
    O('신탁 5', [spendN(K, 2), sh(0.8), draw(2, { who: 'other', type: '공격' })]),
  ], [B('축복 1', 'heal'), B('축복 2', 'atkUp'), B('축복 3', [stk(K, 1)])]));
  starter(j, '스노키_u1');
}

// ════════════════════════════════════════════════════════════════════
// 10. 아사나 — 탱커 · 우울. 마력이 잘 도는 몸 — 카드마다 「마력 호흡」, 셋째 호흡에 세 번째 분출(방어 기반 피해 + 실드).
//    3단계: 시동 = 개전 강화(볼-요가 시간 — 매 턴 호흡) · ④ 밀착 감시를 「호흡을 세는 1코 회복」 으로, 옛 엔진(사기 · 분출마다 회복)은 D 갈래로.
// ════════════════════════════════════════════════════════════════════
function asana(j) {
  const H = '아사나', K = '마력 호흡';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '들숨 넷, 날숨 넷 — 심마체의 호흡', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.08, who: 'allies' }],
    rules: [{ name: '세 번째 분출', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), ddef(0.8), sh(0.5)] }],
  };
  delete h.keywords;
  h.passives = [
    P('심마체', 'play', [stk(K, 1)], { limit: perTurn(2) }),
    P('건강한 몸에 건강한 정신', 'turnStart', [heal(1.0)], { conds: [{ c: 'hp', pct: 0.5 }], limit: perFight(1) }),
  ];
  h.ult.fx = refKw(h.ult.fx, '명상', K);
  const yoga = (s, k) => [sh(s), stk(K, k), power(rule('turnStart', [stk(K, 1)]))];
  const burst = (s, r, extra = [stk(K, 1)]) => [sh(s), ddef(r), ...extra];
  const calm = (b, p, t = E1, tail = [spendAll(K)]) => [ddef(b, t), per(K), ddef(p, t), ...tail];
  const watch = (b, p) => [heal(b), per(K), heal(p)];
  swap(j, [
    // u1 열기 · 시동 — 볼-요가 시간(개전 강화: 첫 턴에 켜 두면 매 턴 호흡)
    card(H, 1, '볼-요가 시간', 1, '강화', yoga(0.7, 1), [
      O('깊은 들숨', yoga(0.9, 2), { tags: ['개전'] }),
      O('요가 매트', yoga(1.0, 2)),
      O('아침 요가', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('단체 요가', [sh(0.8), draw(2, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('붙잡고 시키기', [ddef(0.8), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
    ], [B('엘튜브 방송', 'defUp'), B('스트레칭', 'cost'), B('호흡 교정', [stk(K, 1)])], { tags: ['개전'] }),
    // u2 굴리기 — 원작 저학년 마력 분출(그림 짝 = 저학년 아이콘)
    card(H, 2, '마력 분출', 1, '공격', burst(0.5, 0.55), [
      O('강한 분출', burst(0.6, 0.7)),
      O('가벼운 분출', burst(0.4, 0.4), { cost: 0 }),
      O('마력 탐지', [ddef(0.75), stk(K, 1), mine(1)]),
      O('세 번째 분출', [sh(0.5), ddef(0.55), ifStack(K, 2), ddef(0.6)]),
      O('과호흡', [sh(0.9), ddef(0.95), spendN(K, 2)]),
    ], [B('마력 집중', 'power'), B('들숨', 'draw'), B('불가능 없는 몸', 'frost')]),
    // u3 터뜨리기 — 고요한 일격(호흡 1개당 · 전부 소모 — 세 번째 분출과 맞바꿈)
    card(H, 3, '고요한 일격', 1, '공격', calm(0.45, 0.25), [
      O('천천히, 깊게', calm(0.56, 0.28)),
      O('균형의 일격', calm(0.45, 0.25), { tags: ['분쇄'] }),
      O('명상 자세', [ddef(1.0), power(rule('stackReach', [sh(0.6), ddef(0.35)], reach(K, 3)))], { power: true }),
      O('기다린 일격', calm(0.55, 0.27, E1, [])),
      O('긴 날숨', [sh(1.2), per(K), sh(0.45), spendAll(K)]),
    ], [B('명상을 깨는 동작', 'power'), B('맑은 정신', 'ap'), B('허리 삐끗', [stk(K, 1)])]),
    // u4 완성형 — 24시간 밀착 감시(호흡 1개당 파티 회복 · 호흡은 남김 — 1코 마무리). 옛 강화 엔진은 「평정심」 갈래로
    card(H, 4, '24시간 밀착 감시', 1, '스킬', watch(0.8, 0.3), [
      O('요가 교실', watch(1.0, 0.4)),
      O('혼자 요가', watch(0.6, 0.24), { cost: 0 }),
      O('평정심', [st('사기', 1), power(rule('stackReach', [heal(0.45)], reach(K, 3)))], { power: true }),
      O('굳건한 자세', [ddef(0.5), per(K), ddef(0.2)]),
      O('심마체 이론', [heal(1.2), per(K), heal(0.5), spendAll(K)]),
    ], [B('건강 잔소리', 'heal'), B('불주사 협박', 'draw'), B('요가복', 'guard')]),
  ]);
  // u5 유틸(0코 스킬) — 자세 교정: 구부정하면 그 자리에서 교정 — 실드 · 호흡 · 고유 카드 비용 -1(공격 둘이라 스킬 쪽)
  j.cards.push(card(H, 5, '자세 교정', 0, '스킬', [sh(0.6), stk(K, 1), cheaper()], [
    O('신탁 1', [sh(0.7), stk(K, 2), cheaper()]),
    O('신탁 2', [sh(0.5), stk(K, 1), mine(1)]),
    O('신탁 3', [heal(0.5), stk(K, 1), draw(1, { who: 'other' })]),
    O('신탁 4', [sh(0.4), stk(K, 1), power(rule('turnStart', [sh(0.5)]))], { power: true }),
    O('신탁 5', [disc(1), sh(0.7), stk(K, 2)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]));
  starter(j, '아사나_u1');
}

// ════════════════════════════════════════════════════════════════════
// 11. 아야 — 딜러 · 냉정 · 엘다인. 맞힌 적에게 「서리」 — 쌓인 적은 더 아프고, 다섯이면 눈꽃 개화(광역).
//    3단계: ④ 만년설의 현자는 엘다인 한 단계(완성형 강화)라 그대로 강화 카드. 수치는 올리지 않고 갈래 구조만.
// ════════════════════════════════════════════════════════════════════
function aya(j) {
  const H = '아야', K = '서리';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '맞을수록 깊이 박히는 만년설의 눈꽃', carrier: 'enemy', cap: 5, per: [{ stat: 'taken', v: 0.04 }, { stat: 'dealt', v: -0.03 }],
    rules: [
      { name: '눈꽃 개화', when: { on: 'stackReach', id: K, n: 5 }, limit: perTurn(1), fx: [dmg(0.5, EA)] },
      { name: '얼어 터지는 서리', when: { on: 'stackOver', id: K }, limit: perTurn(3), fx: [{ k: 'perEvent' }, dmg(0.3, E1)] },
    ],
  };
  delete h.keywords;
  h.passives = [
    P('싸락나비', 'play', [stk(K, 1, E1)], { when: { type: '공격' } }),
    P('희망의 눈꽃', 'lowHp', [cleanse(1), st('피해 감소', 2)], { when: { pct: 0.3 }, limit: perFight(1) }),
  ];
  const flit = (n, r, v = 1) => [hits(n, r), stk(K, v, EA)];
  const iaido = (b, p, t = E1, tail = [spendAll(K)]) => [dmg(b, t), per(K), dmg(p, t), ...tail];
  const aim = (v, extra = [draw(1)]) => [stk(K, v, E1), ...extra];
  const rules = (r) => power(rule('turnStart', [stk(K, 1, EA)]), rule('stackReach', [dmg(r, EA)], reach(K, 5)));
  const sage = r => [stk(K, 1, EA), rules(r)];
  swap(j, [
    // u1 굴리기 — 원작 저학년 싸락나비(그림 짝 = 저학년 아이콘)
    card(H, 1, '싸락나비', 1, '공격', flit(3, 0.38), [
      O('겹눈송이', flit(3, 0.5)),
      O('작은 나비', flit(2, 0.4), { cost: 0 }),
      O('나비 길잡이', [hits(3, 0.5), mine(1)]),
      O('서리 꽃봉오리', [hits(3, 0.38), per(K), dmg(0.16, ER)]),
      O('눈보라 나비', [disc(1), hits(4, 0.4), stk(K, 2, EA)]),
    ], [B('얼음 날개', 'power'), B('냉차 한 모금', 'ap'), B('눈꽃 가루', [stk(K, 1, E1)])]),
    // u2 터뜨리기 — 빙설화도 발도(서리 1개당 · 전부 소모 — 개화와 맞바꿈)
    card(H, 2, '빙설화도 발도', 1, '공격', iaido(0.8, 0.3), [
      O('한겨울 발도', iaido(1.0, 0.38)),
      O('휘몰아치는 눈', iaido(0.55, 0.2, EA)),
      O('양산 발도', [...iaido(0.8, 0.3, E1, []), ifKill, stk(K, 3, 'topEnemy')]),
      O('얼음 박차', iaido(0.85, 0.28, E1, [])),
      O('끝없는 겨울', [dmg(1.9), per(K), dmg(0.7)], { tags: ['분쇄', '소멸'] }),
    ], [B('빙설화도', 'weakSpot'), B('맑은 칼날', 'draw'), B('눈사람', [stk(K, 1, E1)])]),
    // u3 열기 — 동상 조준(시동 카드 · 0코)
    card(H, 3, '동상 조준', 0, '스킬', aim(2), [
      O('깊은 동상', aim(3)),
      O('겨울 시야', aim(2, [draw(1), draw(1, { who: 'self', type: '공격' })])),
      O('첫눈', [stk(K, 1, E1), draw(1), power(rule('turnStart', [stk(K, 1, 'topEnemy')]))], { power: true }),
      O('얼음 눈금', [dmg(0.7), stk(K, 2, E1)]),
      O('눈 감고 조준', [disc(1), stk(K, 3, E1), draw(2)]),
    ], [B('레몬차', { tags: ['보존'] }), B('하늘이 탁 트인 곳', 'draw'), B('눈꽃 한 송이', 'atkUp')]),
    // u4 완성형(엘다인 한 단계) — 만년설의 현자: 매 턴 적 전체 서리 · 개화마다 눈보라
    card(H, 4, '만년설의 현자', 1, '강화', sage(0.33), [
      O('만개의 예감', sage(0.55)),
      O('작은 다짐', sage(0.35), { cost: 0 }),
      O('동생들 생각', [mine(1), stk(K, 1, EA), rules(0.45)]),
      O('희망의 눈꽃', [dmg(0.45, EA), rules(0.4)]),
      O('맏이의 각오', [disc(1), stk(K, 2, EA), rules(0.45)]),
    ], [B('새하얀 눈송이', 'atkUp'), B('눈보라 권능', { tags: ['개전'] }), B('겨울 지킴이', 'cost')]),
  ]);
  // u5 유틸(0코 스킬) — 마당 쓸기: 쓸어 낸 눈발이 적 전체에 서리로 — 실드 · 드로우(엘다인 — 수치는 일반과 같게)
  j.cards.push(card(H, 5, '마당 쓸기', 0, '스킬', [stk(K, 1, EA), sh(0.4), draw(1)], [
    O('신탁 1', [stk(K, 2, EA), sh(0.3), draw(1)]),
    O('신탁 2', [stk(K, 1, EA), sh(0.6), mine(1, { type: '공격' })]),
    O('신탁 3', [stk(K, 2, E1), sh(0.3), ifBroken, draw(2)]),
    O('신탁 4', [sh(0.5), draw(1), power(rule('turnEnd', [stk(K, 1, EA)]))], { power: true }),
    O('신탁 5', [disc(1), stk(K, 3, EA), sh(0.6)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1, E1)])]));
  starter(j, '아야_u3');
}

// ════════════════════════════════════════════════════════════════════
// 12. 요미 — 딜러 · 우울 · 엘다인(원작 방식 고학년 「구름 걷는 달빛」 유지). 매 턴 차오르는 「달바라기」 — 넷이면 보름달(광역 + 약화).
//    3단계: ④ 홀로 섬긴 사제는 엘다인 한 단계(완성형 강화) 그대로. 수치는 올리지 않고 갈래 구조만.
// ════════════════════════════════════════════════════════════════════
function yomi(j) {
  const H = '요미', K = '달바라기', MOON = '구름 걷는 달빛';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '달을 향해 피어나는 꽃밭', carrier: 'self', cap: 4, per: [{ stat: 'taken', v: -0.03, who: 'allies' }, { stat: 'dealt', v: 0.05 }],
    rules: [{ name: '보름달', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(0.8, EA), st('약화', 1, EA)] }],
  };
  h.keywords = h.keywords.filter(k => k.name === MOON);
  h.passives = [
    P('달의 순환', 'turnStart', [stk(K, 1)]),
    P('부디 저를 기억해 주세요', 'lowHp', [heal(1.0), st('피해 감소', 1)], { when: { pct: 0.3 }, limit: perFight(1) }),
  ];
  const field = (r, k = 1, t = EA) => [dmg(r, t), stk(K, k)];
  const star = (b, p, t = E1, tail = [spendAll(K)]) => [dmg(b, t), per(K), dmg(p, t), ...tail];
  const pray = (k, extra = [draw(1)]) => [stk(K, k), ...extra];
  const moonRule = r => power(rule('stackReach', [dmg(r, EA), gauge(15)], reach(K, 4)));
  const priest = r => [stk(K, 1), moonRule(r)];
  swap(j, [
    // u1 굴리기 — 원작 저학년 달바라기(그림 짝 = 저학년 아이콘)
    card(H, 1, '달빛 꽃밭', 1, '공격', field(0.75), [
      O('은빛 꽃밭', field(0.95)),
      O('달 아래 대기', field(0.75), { tags: ['보존'] }),
      O('달빛 길잡이', [dmg(1.0, EA), mine(1)]),
      O('희미한 달빛', [dmg(0.75, EA), stk(K, 1), ifStack(K, 3), st('약화', 1, EA)]),
      O('달무리', [dmg(1.1, EA), spendN(K, 2)]),
    ], [B('달님의 빛', 'power'), B('시조 한 수', 'ap'), B('달바라기 씨앗', [stk(K, 1)])]),
    // u2 터뜨리기 — 별빛 사격(달바라기 1개당 · 전부 소모 — 원작 별빛 강화 공격)
    card(H, 2, '별빛 사격', 1, '공격', star(0.6, 0.3), [
      O('칠흑의 별빛', star(0.8, 0.38)),
      O('흩어지는 별', star(0.45, 0.2, EA)),
      O('별자리 읽기', [dmg(0.72), per(K), dmg(0.3), draw(1, { who: 'self', type: '스킬' })]),
      O('기다린 밤', star(0.7, 0.28, E1, [])),
      O('별똥별 부르기', [dmg(1.8), per(K), dmg(0.6)], { cost: 2, tags: ['종극'] }),
    ], [B('배고픈 검은 별', 'weakSpot'), B('달떡', 'draw'), B('반짝이는 불빛', [stk(K, 1)])]),
    // u3 열기 — 달을 섬기는 기도(시동 카드 · 0코)
    card(H, 3, '달을 섬기는 기도', 0, '스킬', pray(1), [
      O('간절한 기도', pray(2)),
      O('교주님을 위해서라면', pray(1, [draw(1, { who: 'other' }), heal(0.4)])),
      O('소망을 머금은 꽃', [stk(K, 1), draw(1), power(rule('turnEnd', [stk(K, 1)]))], { power: true }),
      O('묵상', [heal(1.1), stk(K, 1)]),
      O('밤샘 기도', [disc(1), stk(K, 2), draw(2)]),
    ], [B('슴슴한 국물', 'heal'), B('긴 소매', 'draw'), B('은은히 빛나는', 'atkUp')]),
    // u4 완성형(엘다인 한 단계) — 홀로 섬긴 사제: 보름달마다 광역 + 고학년 게이지
    card(H, 4, '홀로 섬긴 사제', 1, '강화', priest(0.5), [
      O('만월의 서약', priest(0.7)),
      O('첫 기도', priest(0.5), { tags: ['개전'] }),
      O('갸륵한 정성으로', [draw(2, { who: 'other' }), moonRule(0.5)]),
      O('작은 서약', [moonRule(0.45)], { cost: 0 }),
      O('달의 가호', [disc(1), stk(K, 2), moonRule(0.5)]),
    ], [B('달의 사제', 'atkUp'), B('먼지 털어 주기', { tags: ['개전'] }), B('구름 걷기', 'cost')]),
  ]);
  // u5 굴리기(스킬) — 달밤의 춤: 보름달 아래 춤추는 취미 — 실드 · 달바라기(엘다인 — 수치는 일반과 같게)
  j.cards.push(card(H, 5, '달밤의 춤', 1, '스킬', [sh(1.0), stk(K, 2)], [
    O('신탁 1', [sh(1.3), stk(K, 2)]),
    O('신탁 2', [sh(0.45), stk(K, 1)], { cost: 0 }),
    O('신탁 3', [sh(1.05), stk(K, 1), mine(1, { type: '공격' })]),
    O('신탁 4', [sh(0.8), stk(K, 1), power(rule('turnEnd', [per(K), sh(0.2)]))], { power: true }),
    O('신탁 5', [disc(1), sh(1.4), stk(K, 2)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'defUp'), B('축복 3', [stk(K, 1)])]));
  starter(j, '요미_u3');
}

// ════════════════════════════════════════════════════════════════════
// 13. 포셔 — 서포터(축: 회복) · 우울. 무엇을 먹이든 남는 「부작용」 — 두고 볼까, 해독제 값으로 회복을 받을까.
//    3단계: ④ 약장수의 비법서를 「부작용을 세는 1코 공격」 으로, 옛 엔진(스킬마다 부작용)은 D 갈래로.
// ════════════════════════════════════════════════════════════════════
function posher(j) {
  const H = '포셔', K = '부작용', BITTER = '포셔_t1';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '일부러 남겨 둔 포셔 약의 하자', carrier: 'enemy', cap: 5, decay: 1, per: [{ stat: 'dealt', v: -0.04 }] };
  delete h.keywords;
  h.passives = [
    P('약값은 칼같이', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: perTurn(2) }),
    P('해독제 값은 따로', 'stackGone', [heal(0.4)], { when: { id: K, decay: true }, limit: perTurn(1) }),
  ];
  h.ult.fx = refKw(h.ult.fx, '약효', K);
  const potion = (v, r, extra = []) => [stk(K, v, E1), heal(r), ...extra];
  const vial = (r, v = 2, t = E1) => [ddH(2, r, t), stk(K, v, t)];
  const weak = (w, p, tail = [spendAll(K)]) => [st('약화', w, E1), per(K), heal(p), ...tail];
  const book = (b, p) => [ddef(b), per(K), ddef(p)];
  swap(j, [
    // u1 열기 — 무슨 포션 줄까?(시동 카드 · 원작 저학년 · 그림 짝 = 저학년 아이콘)
    card(H, 1, '무슨 포션 줄까?', 1, '스킬', potion(2, 0.7), [
      O('초록 포션', potion(2, 1.0)),
      O('신약 견본', potion(2, 0.95, [draw(1, { who: 'other' })])),
      O('빨간 포션', [stk(K, 2, E1), ddef(0.6)]),
      O('노란 포션', [stk(K, 2, E1), st('기절', 1, E1), ddef(0.6)], { cost: 2 }),
      O('입에 쓴 약', [stk(K, 2, E1), heal(0.5), make(BITTER, 1)]),
    ], [B('약초 달이기', 'heal'), B('장부 대충', 'draw'), B('임상 시험', [stk(K, 1, E1)])]),
    // u2 굴리기 — 중독 포션 두 병(원작 강화 평타 · 두 번 + 부작용 2)
    card(H, 2, '중독 포션 두 병', 1, '공격', vial(0.33), [
      O('독한 두 병', vial(0.42)),
      O('흩뿌린 포션', vial(0.27, 2, EA)),
      O('약병 하나 더', [ddH(2, 0.58), draw(1, { who: 'self', type: '스킬' })]),
      O('부식 물약', [...vial(0.33), ifBroken, ddef(0.6)]),
      O('네 병 한꺼번에', [disc(1), ddH(3, 0.33), stk(K, 3, E1)]),
    ], [B('대폭발 물약', 'power'), B('약병 하나 더 챙기기', 'ap'), B('감기약이었음', [stk(K, 1, E1)])]),
    // u3 터뜨리기 — 흐물 약화 포션(약화 담당 · 부작용 1개당 회복 · 전부 소모 = 해독제 값)
    card(H, 3, '흐물 약화 포션', 1, '스킬', weak(2, 0.25), [
      O('흐물흐물 포션', weak(2, 0.33)),
      O('묽은 포션', weak(1, 0.25), { cost: 0 }),
      O('해독제 장사', [st('약화', 2, E1), power(rule('turnStart', [heal(0.75)]))], { power: true }),
      O('나른 포션', [st('약화', 3, E1), per(K), heal(0.2)]),
      O('비상 포션', [...weak(2, 0.25, []), ifWounded, heal(0.8)]),
    ], [B('레비용 포션', 'heal'), B('맛은 안중에 없음', 'cost'), B('첨가제', [st('약화', 1, E1)])]),
    // u4 완성형 — 약장수의 비법서(부작용 1개당 · 부작용은 남김 — 1코 마무리). 옛 강화 엔진은 「임상 기록」 갈래로
    card(H, 4, '약장수의 비법서', 1, '공격', book(0.45, 0.2), [
      O('비법서 완본', book(0.6, 0.26)),
      O('비법 메모', book(0.35, 0.15), { cost: 0 }),
      O('임상 기록', [stk(K, 3, E1), power(rule('play', [stk(K, 1, E1)], { when: { type: '스킬' }, limit: 2 }))], { power: true }),
      O('포션 장인 포셔', [heal(1.45), per(K), heal(0.5)]),
      O('개업 준비', [ddef(0.7), per(K), ddef(0.32), spendAll(K)]),
    ], [B('예상 못 한 결과', 'weakSpot'), B('약초 도감', 'draw'), B('하얘진 피부', { tags: ['개전'] })]),
  ]);
  // u5 유틸(0코 스킬) — 약초 감별: 속이려 드는 상대가 무서워하는 감별 — 회복 · 부작용 · 드로우
  j.cards.push(card(H, 5, '약초 감별', 0, '스킬', [heal(0.32), stk(K, 1, E1), draw(1)], [
    O('신탁 1', [heal(0.44), stk(K, 2, E1), draw(1)]),
    O('신탁 2', [heal(0.36), stk(K, 2, E1), mine(1)]),
    O('신탁 3', [stk(K, 3, EA), heal(0.32)]),
    O('신탁 4', [heal(0.36), stk(K, 1, E1), power(rule('turnStart', [stk(K, 1, 'topEnemy')]))], { power: true }),
    O('신탁 5', [per(K), heal(0.2), spendAll(K), draw(2)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'defUp'), B('축복 3', [stk(K, 1, E1)])]));
  starter(j, '포셔_u1');
}

// ════════════════════════════════════════════════════════════════════
// 14. 프리클 — 딜러 · 냉정(원작 방식 고학년 「가시 덩굴」 유지). 공격마다 돋는 「가시 촉수」(턴 끝마다 찌름).
//    3단계: ④ 마녀의 힘은 대단했다!는 원작 어사이드(촉수 상시 강화)라 강화 카드 그대로. 갈래 구조만.
// ════════════════════════════════════════════════════════════════════
function prickle(j) {
  const H = '프리클', K = '가시 촉수', VINE = '가시 덩굴';
  const h = j.heroes[0];
  const tent = h.keywords.find(k => k.name === K);
  h.keyword = { ...tent, desc: '덩굴에서 돋아나 턴 끝마다 찌르는 촉수', cap: 4, per: [{ stat: 'dot', ratio: 0.6 }] };
  h.keywords = h.keywords.filter(k => k.name === VINE);
  h.passives = [
    P('문지기 소환', 'play', [stk(K, 1)], { when: { type: '공격' }, limit: perTurn(3) }),
    P('계획은 그 자리에서', 'stackOver', [draw(1)], { when: { id: K }, limit: perTurn(1) }),
  ];
  h.ult.fx = refKw(h.ult.fx, '봉인 함정', K);
  const trap = (k, extra = [draw(1)]) => [stk(K, k), ...extra];
  const harpoon = (n, r, k = 1) => [hits(n, r), stk(K, k)];
  const gate = (b, p, t = EA, tail = [spendAll(K)]) => [dmg(b, t), per(K), dmg(p, t), ...tail];
  const stab = r => power(rule('turnEnd', [per(K), dmg(r, ER)]));
  const power2 = (r, extra = [stk(K, 1)]) => [...extra, stab(r)];
  swap(j, [
    // u1 열기 — 가시 덫 설치(시동 카드)
    card(H, 1, '가시 덫 설치', 1, '스킬', trap(3), [
      O('겹겹이 덫', trap(4)),
      O('급조한 덫', trap(2), { cost: 0 }),
      O('덫밭', [stk(K, 2), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('미리 짠 작전', trap(3, [draw(1, { who: 'self', type: '공격' }), sh(0.8)])),
      O('덩굴 올가미', [disc(1), stk(K, 4), sh(0.8)]),
    ], [B('여왕 대리', { tags: ['보존'] }), B('가시 반지', 'draw'), B('꽃꽂이', [stk(K, 1)])]),
    // u2 굴리기 — 가시 작살 연사(원작 강화 평타 · 무작위 세 번 + 촉수)
    card(H, 2, '가시 작살 연사', 1, '공격', harpoon(3, 0.4, 2), [
      O('굵은 작살', harpoon(3, 0.5, 2)),
      O('가시 한 줌', harpoon(2, 0.4), { cost: 0 }),
      O('제자 부르기', [hits(3, 0.62), draw(1, { who: 'other' })]),
      O('관통 작살', [hits(3, 0.4), ifStack(K, 3), hits(2, 0.5)]),
      O('작살 폭풍', [disc(1), hits(4, 0.42), stk(K, 2)]),
    ], [B('날 선 가시', 'power'), B('튀김 열두 봉지', 'ap'), B('찔린 자리', [stk(K, 1)])]),
    // u3 터뜨리기 — 원작 저학년 따끔한 문지기(촉수를 모두 거둬 터뜨림 · 그림 짝 = 저학년 아이콘)
    card(H, 3, '따끔한 문지기', 1, '공격', gate(0.6, 0.3), [
      O('조이는 문지기', gate(0.75, 0.38)),
      O('한 놈만 조이기', gate(1.0, 0.45, E1)),
      O('나의 애제자 피코라', [...gate(0.72, 0.36, EA, []), draw(1, { who: 'other' })]),
      O('잠복 경비', gate(0.7, 0.28, EA, [])),
      O('엄한 스승', gate(1.5, 0.7, EA, []), { tags: ['소멸'] }),
    ], [B('멸망의 전조', 'power'), B('미숫가루', 'cost'), B('발저림', 'frost')]),
    // u4 완성형(원작 어사이드 — 촉수 상시 강화) — 마녀의 힘은 대단했다!(촉수가 턴 끝마다 한 번 더 찌른다)
    card(H, 4, '마녀의 힘은 대단했다!', 1, '강화', power2(0.18), [
      O('완벽한 수정안', power2(0.25)),
      O('두 번째 계획', power2(0.18, [stk(K, 2)])),
      O('사전 작전', [draw(1, { who: 'self', type: '공격' }), stab(0.3)]),
      O('메모 한 줄', [stab(0.15)], { cost: 0 }),
      O('예비 계획', [disc(1), stk(K, 2), stab(0.2)]),
    ], [B('이인자의 위엄', 'atkUp'), B('거창한 등장', { tags: ['개전'] }), B('제자 몰래 도와주기', 'draw')]),
  ]);
  // u5 굴리기(스킬) — 가시 울타리: 실드 · 촉수(공격 둘 · 강화 하나라 스킬 쪽)
  j.cards.push(card(H, 5, '가시 울타리', 1, '스킬', [sh(1.0), stk(K, 2)], [
    O('신탁 1', [sh(1.3), stk(K, 2)]),
    O('신탁 2', [sh(1.0), stk(K, 2)], { tags: ['보존'] }),
    O('신탁 3', [sh(0.9), stk(K, 1), draw(1, { who: 'other' })]),
    O('신탁 4', [sh(0.9), stk(K, 1), power(rule('turnEnd', [per(K), sh(0.18)]))], { power: true }),
    O('신탁 5', [disc(1), sh(1.2), stk(K, 3)]),
  ], [B('축복 1', 'guard'), B('축복 2', 'defUp'), B('축복 3', [stk(K, 1)])]));
  starter(j, '프리클_u1');
  for (const e of j.equips || []) e.affinityEffect = [P('함정 결재', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: perTurn(1) })];
}

// ════════════════════════════════════════════════════════════════════
// 15. 피코라 — 서포터(축: 회복) · 냉정. 카드마다 「스티커」 한 장 — 넷이면 한정판 세트(회복 + 피해 감소).
//    3단계: 생성 카드 「덤 스티커」 · 기본 카드 연료(기본 코디 — 손의 기본 카드 값↓) · ④ 쇼핑왕을 「스티커를 세는 1코 회복」 으로.
// ════════════════════════════════════════════════════════════════════
function picora(j) {
  const H = '피코라', K = '스티커', EXTRA = '피코라_sticker';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '지상에서 모은 반짝이는 한정 스티커', carrier: 'self', cap: 4,
    rules: [
      { name: '첫 세트', when: { on: 'stackReach', id: K, n: 4 }, limit: perFight(1), fx: [st('사기', 1)] },
      { name: '한정판 세트', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), heal(0.8), st('피해 감소', 2)] },
    ],
  };
  delete h.keywords;
  h.passives = [
    P('스티커 붙이기', 'play', [stk(K, 1)], { limit: perTurn(3) }),
    P('사고 나면 일단 도망', 'lowHp', [st('피해 감소', 2), draw(1)], { when: { pct: 0.3 }, limit: perFight(1) }),
  ];
  token(j, { id: EXTRA, name: '덤 스티커', cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.3), stk(K, 1)], blurb: '「스티커는 덤!」 한 장 더 붙이면 기분도 한 장 더' });
  const shine = (r, k, extra = []) => [heal(r), stk(K, k), ...extra];
  const ltd = (r, c = 1, extra = [stk(K, 1)]) => [heal(r), st('피해 감소', c), ...extra];
  const skull = (b, p, t = E1, tail = [spendAll(K)]) => [ddef(b, t), per(K), ddef(p, t), ...tail];
  const album = (b, p) => [heal(b), per(K), heal(p)];
  swap(j, [
    // u1 열기 — 반짝이 스티커(시동 카드)
    card(H, 1, '반짝이 스티커', 1, '스킬', shine(0.7, 2), [
      O('홀로그램 반짝이', shine(0.9, 3)),
      O('마음에 든 장', shine(0.7, 2), { tags: ['보존'] }),
      O('반짝이 코팅', [heal(0.5), stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('두 장 겹쳐 붙이기', shine(0.85, 2, [draw(1, { who: 'other', type: '공격' })])),
      O('반짝 별빛 마녀', [heal(0.45), make(EXTRA, 1)]),
    ], [B('캔디콘 뿔', 'heal'), B('패션 잡지', 'draw'), B('작은 반짝이', [make(EXTRA, 1)])]),
    // u2 굴리기 — 원작 저학년 한정 스티커(그림 짝 = 저학년 아이콘)
    card(H, 2, '한정 스티커', 1, '스킬', ltd(0.9), [
      O('초한정 스티커', ltd(1.15)),
      O('미니 스티커', ltd(0.65, 1), { cost: 0 }),
      O('피코라 스티커는 덤!', [heal(0.8), st('피해 감소', 1), make(EXTRA, 1)]),
      O('천재 영재 마녀', [heal(1.1), st('피해 감소', 1), per(K), heal(0.3)]),
      O('기본 코디', [heal(1.75), st('피해 감소', 1), { k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, basic: true }]),
    ], [B('지상의 밝은 빛', 'heal'), B('쇼핑 봉투', 'ap'), B('트윙클 스윗 리본', [stk(K, 1)])]),
    // u3 터뜨리기 — 해골 스티커 도배(스티커 1장당 · 전부 소모 — 한정판 세트와 맞바꿈)
    card(H, 3, '해골 스티커 도배', 1, '공격', skull(0.4, 0.2), [
      O('큼직한 해골', skull(0.55, 0.25)),
      O('오싹 해골 도배', skull(0.28, 0.14, EA)),
      O('귀여운 해골', [ddef(0.55), power(rule('stackReach', [ddef(0.5, EA)], reach(K, 4)))], { power: true }),
      O('해골 한 쌍', skull(0.5, 0.2, E1, [])),
      O('사고 치고 도망', [ddef(0.4), per(K), ddef(0.2), draw(1, { who: 'other' })]),
    ], [B('찐득한 풀', 'power'), B('울면서 인정', 'cost'), B('반짝 주문', 'frost')]),
    // u4 완성형 — 쇼핑왕 피코라(스티커 1장당 파티 회복 · 스티커는 남김 — 1코 마무리). 옛 강화 엔진은 「첫 페이지」 갈래로
    card(H, 4, '쇼핑왕 피코라', 1, '스킬', album(0.7, 0.28), [
      O('완성된 앨범', album(0.9, 0.36)),
      O('얇은 앨범', album(0.55, 0.22), { cost: 0 }),
      O('첫 페이지', [st('사기', 1), power(rule('stackReach', [heal(0.45)], reach(K, 4)))], { power: true }),
      O('덤 스티커 앨범', [heal(0.5), perTag(EXTRA), heal(0.45), exileAll(EXTRA)]),
      O('패션 화보', [heal(1.05), per(K), heal(0.4), spendAll(K)]),
    ], [B('솔잎죽 두 그릇', 'heal'), B('지상 쇼핑', 'draw'), B('언젠가 옷차림 전문가', [stk(K, 1)])]),
  ]);
  // u5 굴리기(공격) — 반짝 구속 마법: 최고위 마녀들이 빠진 자리에서 혼자 버틴 구속 마법(스킬 셋이라 공격 쪽)
  j.cards.push(card(H, 5, '반짝 구속 마법', 1, '공격', [ddef(0.75), stk(K, 2)], [
    O('신탁 1', [ddef(1.0), stk(K, 2)]),
    O('신탁 2', [ddef(0.6, EA), stk(K, 2)]),
    O('신탁 3', [ddef(0.85), make(EXTRA, 1)]),
    O('신탁 4', [ddef(0.85), stk(K, 1), power(rule('play', [heal(0.5)], { when: { type: '공격' }, limit: 1 }))], { power: true }),
    O('신탁 5', [disc(1), ddef(1.1), stk(K, 2)]),
  ], [B('축복 1', 'weakSpot'), B('축복 2', 'guard'), B('축복 3', [make(EXTRA, 1)])]));
  starter(j, '피코라_u1');
}

// 마무리(2026-10-08) — 기준 밖 사도의 고유 · 생성 카드 피해 · 실드 · 회복 배율(혼자 완주율을 역할 평균 안으로)
function scale(j, m) {
  const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal'].includes(f.k) && f.ratio) f.ratio = Math.round(f.ratio * m * 100) / 100; if (f.k === 'power') for (const r of f.rules) mul(r.fx); } };
  for (const c of j.cards) { if (!c.unique && !c.token) continue; mul(c.fx); for (const o of c.oracles || []) mul(o.fx); for (const b of c.blesses || []) mul(b.fx); }
}
const S = (fn, m) => j => { fn(j); scale(j, m); };

run([
  ['마녀/레비', S(levi, 0.88)], ['마녀/레비_졸업', S(leviGrad, 0.88)], ['마녀/롤렛', rollett], ['마녀/마카샤', makasha], ['마녀/바리에', barie],
  ['마녀/벨리타', S(belita, 1.45)], ['마녀/벨벳', velvet], ['마녀/셰럼', sherum], ['마녀/스노키', S(snorky, 0.8)], ['마녀/아사나', asana],
  ['마녀/아야', S(aya, 0.87)], ['마녀/요미', yomi], ['마녀/포셔', posher], ['마녀/프리클', S(prickle, 1.33)], ['마녀/피코라', picora],
], new URL('./boost_마녀.json', import.meta.url));
