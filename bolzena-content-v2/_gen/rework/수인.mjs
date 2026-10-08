// 사도 리워크 — 수인 23명(디아나는 1단계 rework.mjs). 2단계 2026-10-07 · 3단계(카제나 자료 보강) 2026-10-08
// 틀: 고유 효과(장치 1개) + 패시브 1~2 + 고유 카드 4장(열기 · 굴리기 · 터뜨리기 · 완성형) + 신탁 5갈래(서로 다른 칸).
// 3단계: 신탁 = 수치 1 · 비용 1 이하의 얕은 갈래 + 재설계 + 강화화(D) / 서치(F) BEST 후보 + 대가(H) 카드 2장 중 1장,
//        ④ 완성형 = 장치를 세는 1코 마무리(강화 엔진은 그 카드의 D 갈래로), 개전 강화 시동 · 생성 카드 · 기본 카드 연료 · 편성 연계 · 축복 정리.
// 지침: _measure/사도_리워크_지침.md §12 · 보고: _measure/리워크_수인.md · 공용 부품: lib.mjs
// node _gen/rework/수인.mjs [사도 이름 일부]  → heroes/수인/<사도>.json 덮어쓰기(백업에서 읽음)
import { E1, EA, ER, dmg, ddef, hits, sh, heal, drain, st, stk, spendAll, per, perTag, draw, make, ap, ifStack, ifKill, ifWounded, ifAll, ifBroken, inspire, power, rule, later, O, B, card, starter, run } from './lib.mjs';

// ── 이 스크립트만의 부품 ──
const TOP = 'topEnemy', LOW = 'lowEnemy';
const pas = (name, on, fx, o = {}) => ({ name, when: { on, ...(o.when || {}) }, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: { per: o.per || 'turn', n: o.limit } } : {}), fx });
const tough = (v = 1, t) => (t ? { k: 'tough', v, target: t } : { k: 'tough', v });
const discard = v => (v === 'all' ? { k: 'discard', all: true } : { k: 'discard', v });
const ifHand = n => ({ k: 'ifHand', n });
const notStack = (id, n = 1) => ({ k: 'ifStack', id, n, not: true });
const ifRand = pct => ({ k: 'ifRandom', pct });
const gauge = v => ({ k: 'gauge', v });
const spend = (id, v) => ({ k: 'spend', id, v });
const pull = (o = {}) => ({ k: 'pull', from: 'discard', n: 1, ...o });
const each = id => ({ k: 'perStack', id, each: true });
const reach = (id, n, fx) => rule('stackReach', fx, { when: { id, n } });
const setCards = (j, cards) => { j.cards = [...j.cards.filter(c => !c.unique), ...cards]; };
const renameUlt = (h, from, to) => { h.ult.fx = h.ult.fx.map(f => ((f.k === 'stack' || f.k === 'spend') && f.id === from ? { ...f, id: to } : f)); };
// 3단계 부품 — 서치 · 생성 카드 · 기본 카드 연료
const SU = { who: 'self', unique: true };                       // 자신의 고유 카드
const srch = (o = SU, v = 1) => draw(v, o);                     // 지정 드로우(서치)
const exileAll = tag => ({ k: 'exileFrom', from: 'hand', all: true, tag });
const tfFrom = (id, from) => ({ k: 'transform', id, from, n: 1 });   // 손의 시작 카드(from) 1장 → 생성 카드(이 전투) — 기본 카드 연료
const exileBasic = { k: 'exileFrom', from: 'hand', n: 1, basic: true };           // 손의 시작 카드 1장 소멸
const drawBasic = (o = {}) => draw(1, { basic: true, ...o });                      // 시작 카드 드로우
// u5(2026-10-08) — 비용 유틸: 손의 (거르개) 카드 1장 비용 -1
const cheap = (o = { who: 'self', type: '공격' }) => ({ k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, ...o });
const ATK = { who: 'self', type: '공격' }, SKL = { who: 'self', type: '스킬' };
// 마무리(2026-10-08) 사도 몫 숫자 배율 — 고유 · 생성 카드의 기본형과 신탁 피해 · 실드 · 회복을 같은 비율로(신탁 값어치 비는 그대로)
const scaleU = (j, m) => {
  const mul = fx => { for (const x of fx || []) { if (['dmg', 'shield', 'heal'].includes(x.k) && x.ratio) x.ratio = Math.round(x.ratio * m * 100) / 100; if (x.k === 'power') for (const r of x.rules) mul(r.fx); } };
  for (const c of j.cards) if (c.unique || c.token) { mul(c.fx); for (const o of c.oracles || []) mul(o.fx); }
};
// u5 한 장만 숫자 배율(혼자 완주율이 3%p 넘게 오른 사도) — 기본형 · 신탁 모두
const scaleC = (j, id, m) => { const c = j.cards.find(x => x.id === id); const mul = fx => { for (const x of fx || []) { if (['dmg', 'shield', 'heal'].includes(x.k) && x.ratio) x.ratio = Math.round(x.ratio * m * 100) / 100; if (x.k === 'power') for (const r of x.rules) mul(r.fx); } }; mul(c.fx); for (const o of c.oracles || []) mul(o.fx); };
const token = (j, c) => { const old = j.cards.find(x => x.id === c.id); const t = { hero: j.heroes[0].id, token: true, ...c }; if (old) Object.assign(old, t); else j.cards.push(t); };

// ════════════════════════════════════════════════════════════════════
// 1. 그윈 — 서포터(버퍼) · 냉정. 먼저 찜한 적에게 「깃발」 — 파티의 약점 공격 표적, 셋이면 얼어붙음(기절)
//    ④ 「신나는 모험이다!」 는 원작 어사이드가 상시 영역 효과라 강화 카드로 둔다.
// ════════════════════════════════════════════════════════════════════
function gwin(j) {
  const H = '그윈', K = '깃발';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '먼저 찜한 적에게 꽂는 탐험 깃발', carrier: 'enemy', cap: 3, weakens: true,
    rules: [{ name: '동상', when: { on: 'stackReach', id: K, n: 3 }, limit: { per: 'fight', n: 1 }, fx: [st('기절', 1, E1), spendAll(K)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('먼저 찜한 쪽이 임자', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 1 }),
  ];
  setCards(j, [
    // u1 열기 — 저학년 「스노우포그」: 안개 속 깃발 · 약화(광역 1 — 담당)
    card(H, 1, '스노우포그', 1, '스킬', [stk(K, 1, EA), st('약화', 1, EA)], [
      O('짙은 눈안개', [stk(K, 1, EA), st('약화', 1, EA), st('고통', 3, EA)]),
      O('옅은 안개', [stk(K, 1, EA), st('고통', 2, EA)], { cost: 0 }),
      O('깃발 행렬', [stk(K, 2, E1), srch({ who: 'self', type: '공격' })]),                    // 재설계 · 서치(BEST)
      O('눈 속 피난처', [discard(1), stk(K, 1, EA), sh(1.3)]),                                  // 대가
      O('서늘한 바람', [stk(K, 1, EA), st('약화', 1, EA), inspire, stk(K, 1, EA)]),
    ], [B('탐험가의 감', 'draw'), B('빵주 일등항해사', 'ap'), B('추위를 모르는 펭귄', [sh(0.4)])]),
    // u2 굴리기 — 강평 「가방 크게 휘두르기」: 광역 방어 기반 + 깃발 + 막기
    card(H, 2, '얼음 가방', 1, '공격', [ddef(0.5, EA), stk(K, 1, E1), sh(0.6)], [
      O('꽉 찬 가방', [ddef(0.65, EA), stk(K, 1, E1), sh(0.6)]),
      O('가방째 던지기', [ddef(1.3, EA), stk(K, 1, EA), tough(1, EA)], { cost: 2 }),          // 비용↑ — 광역 깃발 · 격파
      O('지도에 표시', [ddef(0.5, EA), stk(K, 1, E1), ifStack(K, 2), ddef(0.45, EA)]),
      O('앵무 대가리!', [ddef(1.1, E1), stk(K, 1, E1), sh(0.6)]),
      O('짐 비우기', [discard(1), ddef(0.75, EA), stk(K, 2, E1)]),                              // 대가
    ], [B('묵직한 얼음', 'power'), B('먼 곳까지 휘두르기', { tags: ['개전'] }), B('빙판 위 미끄럼', 'frost')]),
    // u3 완성형(강화 — 원작 상시) — 어사이드 「신나는 모험이다!」(영역 위 아군 스킬 피해↑ = 사기) · 매 턴 먼저 찜
    card(H, 3, '신나는 모험이다!', 1, '강화', [st('사기', 1), power(rule('turnStart', [stk(K, 1, TOP)]))], [
      O('출항 준비', [st('사기', 1), power(rule('turnStart', [stk(K, 2, TOP)]))]),
      O('배낭 하나', [st('사기', 1), power(rule('turnStart', [stk(K, 1, TOP)]))], { cost: 0 }),
      O('정복 기념사진', [stk(K, 2, EA), srch({ who: 'self', type: '공격' }, 2), power(rule('turnStart', [stk(K, 1, TOP)]))], { tags: ['개전'] }),
      O('모험 일지', [st('사기', 1), draw(1), power(rule('stackReach', [sh(1.2)], { when: { id: K, n: 3 } }))]),
      O('남이 가꾼 길은 싫어', [st('사기', 2), stk(K, 1, EA)]),                                  // 엔진을 버리고 사기 둘
    ], [B('레전드 나침반', 'atkUp'), B('탐험 준비 끝', 'draw'), B('출발 깃발', [stk(K, 1, E1)])]),
    // u4 터뜨리기 — 정복 인증샷: 그 적의 깃발을 거둬 피해 · 실드(셋까지 모아 얼릴까, 지금 찍을까)
    card(H, 4, '깃발 꽂고 인증샷', 1, '스킬', [per(K), dmg(0.4), per(K), sh(0.4), spendAll(K)], [
      O('모두 다 나오게', [per(K), dmg(0.5), per(K), sh(0.5), spendAll(K)]),
      O('극지 탐험 기록', [per(K), dmg(1.0), spendAll(K), draw(2)], { cost: 2 }),              // 비용↑ — 드로우 둘
      O('정복 앨범', [per(K), dmg(0.5), power(rule('hit', [sh(0.35)], { when: { weak: true, who: 'any' }, limit: 2 }))], { power: true }),  // 강화화(BEST)
      O('먼저 찜한 적', [per(K), dmg(0.4), per(K), sh(0.4), ifStack(K, 2), st('기절', 1, E1)]),
      O('인증샷 공유', [per(K), dmg(0.4), per(K), sh(0.4), draw(1, { who: 'other' })]),
    ], [B('동생을 등 뒤로', 'guard'), B('극지 귀가길', 'cost'), B('한 장 더 찰칵', [stk(K, 1, E1)])]),
    // u5 유틸(0코 서치) — 어사이드 「전설의 나침반」: 원하는 것이 있는 쪽을 가리킨다
    card(H, 5, '전설의 나침반', 0, '스킬', [stk(K, 1, E1), srch()], [
      O('신탁 1', [stk(K, 2, E1), srch()]),
      O('신탁 2', [stk(K, 1, E1), srch(SU, 2)]),                                       // 서치 둘
      O('신탁 3', [stk(K, 1, E1), srch(), power(rule('stackReach', [draw(1)], { when: { id: K, n: 3 } }))], { power: true }),   // 강화화(BEST)
      O('신탁 4', [discard(1), stk(K, 2, EA), draw(1)]),                                                // 대가 · 재설계 — 광역 깃발
      O('신탁 5', [stk(K, 1, E1), srch(), ifStack(K, 2), draw(1)]),
    ], [B('축복 1', 'ap'), B('축복 2', { tags: ['보존'] }), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  starter(j, '그윈_u1');
}

// ════════════════════════════════════════════════════════════════════
// 2. 델리아 — 딜러 · 순수(연계: 동료의 공격이 「수족열증」을 채운다). 화나면 수족열증 — 지속 피해 · 델리아에게 받는 피해↑, 이글루 배치기로 터뜨림
// ════════════════════════════════════════════════════════════════════
function delia(j) {
  const H = '델리아', K = '수족열증';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '발끝부터 화끈거리는 열', carrier: 'enemy', cap: 3, per: [{ stat: 'dot', ratio: 0.1 }, { stat: 'taken', v: 0.06, from: 'owner' }] };
  delete h.keywords;
  h.passives = [
    pas('펭귄 탐험대', 'play', [stk(K, 1, E1)], { when: { type: '공격', who: 'other' }, limit: 1 }),   // 연계 — 동료가 공격하면 그 적에게 열
    pas('이글루 도망', 'lowHp', [{ k: 'cleanse', v: 1 }, st('피해 감소', 1)], { when: { pct: 0.5 } }),
  ];
  renameUlt(h, '미끄럼', K);
  setCards(j, [
    // u1 열기 — 저학년 「델리아 화났어!」: 점프 범위 + 수족열증
    card(H, 1, '델리아 화났어!', 1, '공격', [dmg(0.42, EA), stk(K, 1, EA)], [
      O('진짜 화났어!', [dmg(0.65, EA), stk(K, 1, EA)]),
      O('삐짐', [dmg(0.4, EA), stk(K, 1, EA)], { cost: 0 }),
      O('겁쟁이 대장', [stk(K, 1, EA), sh(1.5), ifWounded, heal(1.0)]),                         // 재설계 — 겁이 나서 막기
      O('날아차기', [dmg(1.2), stk(K, 2, E1), ifStack(K, 3), dmg(0.6)]),
      O('분노 폭발', [discard(1), dmg(0.8, EA), stk(K, 2, EA)]),                                // 대가
    ], [B('빽 소리', 'power'), B('차가운 눈초리', 'weakSpot'), B('씩씩거림', [stk(K, 1, E1)])]),
    // u2 버팀 — 강평: 민초 아이스크림 먹고 회복
    card(H, 2, '가방 속 민초', 1, '스킬', [heal(1.2), draw(1)], [
      O('민초 두 봉지', [heal(1.6), draw(1)]),
      O('한 입만', [heal(0.9), draw(1)], { cost: 0 }),
      O('민초 도시락', [heal(0.8), draw(1), power(rule('turnStart', [heal(0.5)]))], { power: true }),   // 강화화
      O('민초 던지기', [dmg(0.8), stk(K, 1, E1), draw(1)]),                                     // 재설계 — 먹지 않고 던짐
      O('가방 뒤적이기', [heal(1.0), pull(SU)]),                                                 // 서치
    ], [B('민트 향', 'heal'), B('탐험대장 배지', 'ap'), B('입가의 민초', [stk(K, 1, E1)])]),
    // u3 터뜨리기 — 어사이드 「이글루 배치기」: 수족열증 1개당 한 대 더 · 전부 소모(2코 하나)
    card(H, 3, '이글루 배치기', 2, '공격', [dmg(1.5), per(K), dmg(0.35), spendAll(K)], [
      O('대왕 배치기', [dmg(1.9), per(K), dmg(0.45), spendAll(K)]),
      O('작은 이글루', [dmg(1.1), per(K), dmg(0.32), spendAll(K)], { cost: 1 }),
      O('반피 이글루', [dmg(1.6), per(K), dmg(0.4), ifWounded, sh(1.6)]),
      O('뒤뚱 회오리', [dmg(1.1, EA), each(K), dmg(0.3, EA)]),
      O('이글루 한 채', [dmg(3.6), per(K), dmg(0.9), tough(1)], { tags: ['소멸'] }),            // 대가 — 소멸 한 방
    ], [B('통통한 배', 'power'), B('도움닫기', 'ap'), B('열 오른 배', [stk(K, 1, E1)])]),
    // u4 완성형(1코 마무리) — 어사이드 「일어나! 탐험 가야지!」: 적 전체에 열 · 적마다 제 열만큼(쓰지 않음)
    card(H, 4, '일어나! 탐험 가야지!', 1, '공격', [stk(K, 1, EA), each(K), dmg(0.18, EA)], [
      O('눈썰매 질주', [stk(K, 1, EA), each(K), dmg(0.23, EA)]),
      O('탐험대 출발', [st('사기', 1), power(rule('turnEnd', [each(K), dmg(0.15, EA)]))], { power: true }),   // 강화화(BEST) — 옛 엔진 자리
      O('베스트 프렌드', [dmg(0.5), per(K), dmg(0.45), ifKill, draw(2)]),                                    // 재설계 — 한 명에게 몰기
      O('설원의 아침', [discard(1), stk(K, 1, EA), each(K), dmg(0.3, EA)]),                      // 대가
      O('탐험 장비', [stk(K, 1, EA), each(K), dmg(0.18, EA), srch()]),                           // 서치
    ], [B('펭귄 탐험대 깃발', 'atkUp'), B('첫 미끄럼', { tags: ['개전'] }), B('탐험대 구호', 'draw')]),
    // u5 굴리기(스킬) — 탐험대장 배지: 펭귄 탐험대를 불러 모아 열을 퍼뜨리고 막는다
    card(H, 5, '탐험대 소집', 1, '스킬', [stk(K, 1, EA), sh(0.9), draw(1)], [
      O('신탁 1', [stk(K, 1, EA), sh(1.2), draw(1)]),
      O('신탁 2', [stk(K, 1, EA), each(K), dmg(0.15, EA), sh(0.6)]),                      // 재설계 — 막기 대신 펭귄
      O('신탁 3', [stk(K, 1, EA), sh(0.9), srch(ATK, 2)]),                                     // 서치(BEST)
      O('신탁 4', [discard(1), stk(K, 3, EA), sh(1.4)]),                                      // 대가
      O('신탁 5', [stk(K, 2, EA), sh(1.0), ifStack(K, 3), heal(1.0)]),
    ], [B('축복 1', 'guard'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1, EA)])]),
  ]);
  scaleU(j, 0.82);
  starter(j, '델리아_u1');
}

// ════════════════════════════════════════════════════════════════════
// 3. 디아나(왕년) — 딜러 · 냉정 · 엘다인. 빈손으로 모으는 「백수공권」 — 넷이면 바위 던지기
//    3단계: 「거르지 않는 수련」 을 개전 강화 시동으로, 「맨주먹 한 방」 을 백수공권을 세는 1코 마무리로.
// ════════════════════════════════════════════════════════════════════
function dianaOld(j) {
  const H = '디아나_왕년', K = '백수공권';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '도끼 없이 맨주먹으로 모으는 기세', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [{ name: '바위 던지기', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(0.7, EA), tough(1, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('단순하게', 'play', [ifHand(1), stk(K, 1)], { when: { type: '공격' } }),
    pas('휴식이 필요해', 'lowHp', [stk(K, 4), st('피해 감소', 2)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // u1 저학년 「찢는다」: 땅의 균열(담당) — 변신 덤이 「맨주먹 전성기」를 푼다(원작 그대로)
    card(H, 1, '찢기', 1, '공격', [dmg(0.7, EA), st('균열', 2, EA)], [
      O('갈가리 찢기', [dmg(0.9, EA), st('균열', 2, EA)]),
      O('땅째로 찢기', [dmg(1.6, EA), st('균열', 3, EA), tough(1, EA)], { cost: 2 }),          // 비용↑ — 격파
      O('맨손 찢기', [stk(K, 2), dmg(0.8, EA), ifHand(1), dmg(0.6, EA)]),                                     // 재설계 — 균열 대신 기세
      O('한 놈만 찢기', [dmg(1.5), st('균열', 3, E1)]),
      O('귀찮은 건 빼고', [discard(1), dmg(0.9, EA), st('균열', 2, EA)]),                        // 대가
    ], [B('억센 손톱', 'power'), B('약한 데부터', 'weakSpot'), B('한창때 근육', 'atkUp')]),
    // u2 굴리기(큰 한 방) — 손을 다 털고 내려친다(2코 하나)
    card(H, 2, '복잡한 건 질색', 2, '공격', [discard('all'), dmg(2.4), stk(K, 2)], [
      O('정말 질색', [discard('all'), dmg(2.9), stk(K, 2)]),
      O('대충 한 방', [discard('all'), dmg(1.8), stk(K, 2)], { cost: 1 }),
      O('도끼 없이 끝장', [discard('all'), dmg(4.2), tough(2)], { cost: 3 }),                    // 비용↑ — 격파 둘
      O('주먹만 남기기', [discard('all'), dmg(2.6), srch({ who: 'self', type: '공격' })]),        // 서치
      O('휩쓸기', [discard('all'), dmg(1.8, EA), tough(1, EA)]),
    ], [B('통째로 부수기', 'power'), B('얼얼한 주먹', 'frost'), B('생각 비우기', 'ap')]),
    // u3 시동(개전 강화) — 「지금 크지 않으면 얕보인다」 수련은 거르지 않음
    card(H, 3, '거르지 않는 수련', 1, '강화', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('다시 붙은 근육', [stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('맨손 체조', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('새벽 단련', [srch({ who: 'self', type: '공격' }, 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),  // 재설계 · 서치
      O('전성기의 하루', [stk(K, 2), draw(1), power(rule('turnStart', [stk(K, 1)]))]),           // 대가 — 개전을 뗌
      O('한창때 감각', [stk(K, 2), ifHand(1), dmg(0.6, EA), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
    ], [B('몸이 기억함', 'draw'), B('나른한 오후', 'atkUp'), B('매듭 빵', [stk(K, 1)])], { tags: ['개전'] }),
    // u4 완성형(1코 마무리) — 맨주먹 한 방: 백수공권 1개당 한 대(쓰지 않음) · 빈손이면 기세
    card(H, 4, '맨주먹 한 방', 1, '공격', [dmg(0.6), per(K), dmg(0.2), ifHand(1), stk(K, 1)], [
      O('묵직한 한 방', [dmg(0.75), per(K), dmg(0.25), ifHand(1), stk(K, 1)]),
      O('잽', [dmg(0.45), per(K), dmg(0.15), ifHand(1), stk(K, 1)], { cost: 0 }),
      O('끝내기 주먹', [dmg(0.6), per(K), dmg(0.45), spendAll(K)]),                              // 대가 · 재설계 — 다 쏟기
      O('맨주먹의 나날', [dmg(0.6), stk(K, 1), power(rule('play', [per(K), dmg(0.15)], { when: { type: '공격' }, limit: 1 }))], { power: true }),  // 강화화(BEST)
      O('빈손의 기세', [dmg(0.6), per(K), dmg(0.2), ifHand(0), srch({ who: 'self', type: '공격' })]),
    ], [B('주먹에 힘', 'power'), B('멍 자국', [stk(K, 1)]), B('숨 고르기', 'draw')]),
    // u5 굴리기(스킬) — 도끼 없이: 손을 비우며 기세를 모으고 버틴다
    card(H, 5, '도끼 내려놓기', 1, '스킬', [discard(1), stk(K, 2), sh(1.0)], [
      O('신탁 1', [discard(1), stk(K, 2), sh(1.4)]),
      O('신탁 2', [discard(1), stk(K, 1), sh(0.8)], { cost: 0 }),
      O('신탁 3', [stk(K, 2), sh(0.9), power(rule('turnEnd', [ifHand(1), stk(K, 1)]))], { power: true }),   // 강화화(BEST) — 빈손으로 턴을 넘기면 기세
      O('신탁 4', [discard('all'), stk(K, 3), sh(1.3)]),                                  // 대가 — 손을 다 턴다
      O('신탁 5', [stk(K, 2), sh(0.9), srch(ATK)]),                                         // 서치 · 재설계 — 버리지 않고 찾는다
    ], [B('축복 1', 'guard'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '디아나_왕년_u3');
}

// ════════════════════════════════════════════════════════════════════
// 4. 란 — 딜러 · 순수 · 엘다인. 「늑대 표식」(지속 피해 · 적 피해량 감소)을 넓게 묻히고 사랑니로 거둔다
//    ④ 「날선 늑대의 발톱」 은 원작 어사이드가 상시 효과라 강화 카드로 둔다.
// ════════════════════════════════════════════════════════════════════
function ran(j) {
  const H = '란', K = '늑대 표식';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '무리 사냥의 표적에 남는 이빨 자국', carrier: 'enemy', cap: 3, per: [{ stat: 'dot', ratio: 0.15 }, { stat: 'dealt', v: -0.05 }] };
  delete h.keywords;
  h.passives = [
    pas('허공 가르기', 'play', [dmg(0.5, EA), stk(K, 1, EA)], { when: { type: '공격', every: 3 } }),
    pas('란의 공포', 'kill', [gauge(20), stk(K, 2, LOW)], { limit: 1 }),
  ];
  setCards(j, [
    // u1 열기 — 저학년 「아랑의 여유」: 칼바람 광역 + 늑대 표식
    card(H, 1, '아랑의 여유', 1, '공격', [dmg(0.8, EA), stk(K, 1, EA)], [
      O('칼바람', [dmg(0.95, EA), stk(K, 1, EA)]),
      O('한숨 돌리고', [dmg(0.55, EA), stk(K, 1, EA)], { cost: 0 }),
      O('여유 부리기', [dmg(1.5, EA), stk(K, 2, EA), draw(1)], { cost: 2 }),                    // 비용↑ — 표식 둘 · 드로우
      O('벗의 기억', [each(K), dmg(0.35, EA), draw(1)]),                                       // 재설계 — 묻히지 않고 센다
      O('무리 사냥', [dmg(0.8, EA), stk(K, 1, EA), srch({ who: 'self', type: '공격' })]),       // 서치
    ], [B('날 선 송곳니', 'power'), B('흐트러진 틈', 'weakSpot'), B('란과 벗님들', 'atkUp')]),
    // u2 터뜨리기 — 요도 「사랑니」 발도: 표식 1개당 한 칼 · 전부 거둠
    card(H, 2, '사랑니 발도술', 1, '공격', [dmg(0.9), per(K), dmg(0.4), spendAll(K)], [
      O('깊은 발도', [dmg(1.1), per(K), dmg(0.45), spendAll(K)]),
      O('칼끝 겨누기', [dmg(0.6), per(K), dmg(0.3), spendAll(K)], { cost: 0 }),
      O('발도 후 납도', [dmg(1.0), per(K), dmg(0.35)], { tags: ['보존'] }),
      O('횡베기', [each(K), dmg(0.35, EA), stk(K, 1, EA), draw(1)]),                                     // 재설계 — 거두지 않고 다시 묻힘
      O('피 냄새', [per(K), dmg(0.5), spendAll(K), srch({ who: 'self', type: '스킬' })]),        // 서치(BEST) — 투구꽃 독을 다시
    ], [B('벼린 칼날', 'power'), B('빠른 손', 'draw'), B('남은 이빨 자국', [stk(K, 1, E1)])]),
    // u3 굴리기 — 투구꽃 독초(남을 재우는 독): 표식 + 고통(담당)
    card(H, 3, '투구꽃 독', 1, '스킬', [stk(K, 2, E1), st('고통', 3, E1), draw(1)], [
      O('짙은 독', [stk(K, 2, E1), st('고통', 5, E1), draw(1)]),
      O('독 한 방울', [stk(K, 1, E1), st('고통', 3, E1), draw(1)], { cost: 0 }),
      O('약초 주머니', [heal(1.2), draw(2), stk(K, 1, E1)]),                                                   // 재설계 — 독초 대신 약초
      O('잠재우는 독', [stk(K, 2, E1), st('고통', 4, E1), power(rule('turnStart', [stk(K, 1, LOW)]))], { power: true }),  // 강화화
      O('독 바른 칼', [discard(1), dmg(1.2), stk(K, 3, E1)]),                          // 대가
    ], [B('얇게 바르기', 'cost'), B('번지는 독', [st('고통', 2, E1)]), B('독초 면역', 'draw')]),
    // u4 완성형(강화 — 원작 상시) — 어사이드 「늑대 야수의 발톱」: 턴 끝마다 적마다 제 표식만큼 할퀸다
    card(H, 4, '날선 늑대의 발톱', 1, '강화', [stk(K, 1, EA), power(rule('turnEnd', [each(K), dmg(0.2, EA)]))], [
      O('사냥 본능', [stk(K, 1, EA), power(rule('turnEnd', [each(K), dmg(0.28, EA)]))]),
      O('발톱 세우기', [stk(K, 1, EA), power(rule('turnEnd', [each(K), dmg(0.2, EA)]))], { cost: 0 }),
      O('으르렁', [srch(), power(rule('turnEnd', [each(K), dmg(0.2, EA)]))], { tags: ['개전'] }),   // 재설계 · 서치
      O('늑대 무리', [stk(K, 1, EA), gauge(15), power(rule('turnEnd', [each(K), dmg(0.2, EA)]), rule('kill', [stk(K, 2, LOW)]))]),
      O('깊은 상처', [stk(K, 1, EA), draw(1), power(rule('turnEnd', [each(K), dmg(0.2, EA)]))]),
    ], [B('날카로운 발톱', 'atkUp'), B('사냥의 숨', 'draw'), B('무리의 표적', [stk(K, 1, EA)])]),
    // u5 유틸(0코) — 어사이드 「행복한 아기 늑대」: 근심 없는 기억이 무리를 깨운다
    card(H, 5, '아기 늑대의 기억', 0, '스킬', [stk(K, 1, EA), draw(1)], [
      O('신탁 1', [stk(K, 1, EA), draw(1), sh(0.6)]),
      O('신탁 2', [stk(K, 1, EA), draw(2)]),
      O('신탁 3', [stk(K, 2, EA), srch(ATK)]),                                                 // 서치(BEST) — 사랑니를 찾는다
      O('신탁 4', [discard(1), stk(K, 2, EA), st('고통', 2, EA)]),                             // 대가
      O('신탁 5', [stk(K, 1, EA), draw(1), ifStack(K, 2), heal(0.7)]),
    ], [B('축복 1', 'ap'), B('축복 2', { tags: ['보존'] }), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  starter(j, '란_u1');
}

// ════════════════════════════════════════════════════════════════════
// 5. 루포 — 딜러 · 활발. 「계획」 넷이면 계획대로(드로우 둘). 생성 카드 「작전 쪽지」 — 써서 계획을 쌓을지, 쥐고 단검에 실을지
// ════════════════════════════════════════════════════════════════════
function rufo(j) {
  const H = '루포', K = '계획', M = '루포_memo';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '루포 님의 치밀한 작전', carrier: 'self', cap: 4,
    rules: [{ name: '계획대로', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), draw(2)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('치밀한 계획', 'break', [stk(K, 1)], { when: { mine: true } }),
    pas('삼총사의 대모험', 'fightStart', [make(M, 1)]),
  ];
  token(j, { id: M, name: '작전 쪽지', cost: 0, type: '스킬', tags: ['소멸'], fx: [stk(K, 1), draw(1)], blurb: '루포 님이 몰래 적어 둔 다음 수입니다.' });
  const SP = ['신속'];
  setCards(j, [
    // u1 굴리기 — 저학년 「루포류 신속베기」: 세 번 베고 쓰라림(고통 — 담당)
    card(H, 1, '루포류 신속베기', 1, '공격', [hits(3, 0.38, E1), st('고통', 2, E1), stk(K, 1)], [
      O('루포류 쾌속베기', [hits(3, 0.48, E1), st('고통', 2, E1), stk(K, 1)], { tags: SP }),
      O('한 박자 베기', [hits(2, 0.4, E1), st('고통', 2, E1), stk(K, 1)], { cost: 0, tags: SP }),
      O('오의 신속베기', [hits(5, 0.5, E1), st('고통', 3, E1), make(M, 2)], { cost: 2, tags: SP }),   // 비용↑ — 쪽지 둘
      O('계획대로 베기', [hits(3, 0.38, E1), st('고통', 2, E1), { k: 'ifTune' }, stk(K, 2)], { tags: SP }),
      O('뒤통수', [hits(3, 0.42, E1), st('고통', 2, E1), draw(1)], { tags: ['약점 공격'] }),       // 대가 — 신속을 떼고 약점 · 드로우
    ], [B('바람 가르기', 'power'), B('계획된 급소', 'weakSpot'), B('발 빠른 여우', 'draw')], { tags: SP }),
    // u2 열기 — 작전 지도: 계획 + 쪽지 + 드로우
    card(H, 2, '작전 지도 펼치기', 1, '스킬', [stk(K, 1), make(M, 1), draw(1)], [
      O('세밀한 지도', [stk(K, 2), make(M, 1), draw(1)]),
      O('쪽지 작전', [make(M, 1), draw(1)], { cost: 0 }),
      O('비밀 작전', [stk(K, 2), make(M, 1), pull({ who: 'self', type: '공격' })]),               // 서치
      O('보드게임 룰 조작', [make(M, 1), draw(1), inspire, stk(K, 3)]),
      O('쪽지 몽땅 쓰기', [discard(1), stk(K, 2), make(M, 2)]),                                   // 대가 · 재설계
    ], [B('접힌 귀퉁이', 'draw'), B('작전 개시', { tags: ['개전'] }), B('머리 좋아지는 약', [stk(K, 1)])]),
    // u3 완성형(1코 마무리) — 강평 눈속임 단검: 손의 쪽지 1장당 한 번 더(쪽지는 남는다)
    card(H, 3, '눈속임 단검', 1, '공격', [dmg(0.9), perTag(M), dmg(0.45), sh(0.5)], [
      O('깊이 찌르기', [dmg(1.1), perTag(M), dmg(0.56), sh(0.5)], { tags: SP }),
      O('잔꾀', [dmg(0.5), perTag(M), dmg(0.25), sh(0.3)], { cost: 0, tags: SP }),
      O('계획된 눈속임', [dmg(0.8), per(K), dmg(0.3), st('회피', 1)]),                            // 재설계 — 쪽지 대신 계획을 센다
      O('잔꾀 노트', [dmg(1.0), make(M, 2), power(rule('make', [stk(K, 1)], { limit: 2 }))], { power: true }),   // 강화화(BEST) — 쪽지가 생길 때마다 계획
      O('쪽지 불태우기', [dmg(2.2), perTag(M), dmg(1.0), exileAll(M)], { tags: ['소멸'] }),       // 대가
    ], [B('날 선 단검', 'power'), B('허 찌르기', 'weakSpot'), B('한 수 앞', 'ap')]),
    // u4 터뜨리기 — 오의 여우 꼬리: 계획 1개당 한 대 · 전부(2코 하나)
    card(H, 4, '오의 여우 꼬리', 2, '공격', [dmg(1.6), per(K), dmg(0.55), spendAll(K)], [
      O('오의 구미호 꼬리', [dmg(1.9), per(K), dmg(0.65), spendAll(K)]),
      O('꼬리 휘두르기', [dmg(1.1), per(K), dmg(0.4), spendAll(K)], { cost: 1 }),
      O('구미호의 계략', [dmg(1.0), stk(K, 2), power(reach(K, 4, [dmg(0.9)]))], { cost: 1, power: true }),           // 강화화
      O('오의 여우불', [dmg(4.2), perTag(M), dmg(1.2), exileAll(M)], { tags: ['소멸'] }),          // 대가 · 재설계 — 쪽지를 태워 여우불
      O('최고의 전략가', [dmg(1.6), per(K), dmg(0.55), srch()]),                                  // 서치
    ], [B('매서운 꼬리', 'power'), B('틈 노리는 꼬리', 'weakSpot'), B('비상 쪽지', [make(M, 1)])]),
    // u5 유틸(0코 비용) — 보드게임 룰은 루포 님 마음대로: 쪽지 한 장 · 다음 공격을 싸게
    card(H, 5, '여우의 꼼수', 0, '스킬', [make(M, 1), cheap(ATK)], [
      O('신탁 1', [stk(K, 1), make(M, 1), cheap(ATK)]),
      O('신탁 2', [make(M, 1), cheap(ATK), inspire, stk(K, 2)]),
      O('신탁 3', [stk(K, 2), make(M, 1), power(rule('stackReach', [cheap(ATK), draw(1)], { when: { id: K, n: 4 } }))], { power: true }),   // 강화화 — 계획대로마다 공격 하나를 싸게
      O('신탁 4', [stk(K, 1), make(M, 1), srch(ATK, 2)]),                                         // 서치(BEST)
      O('신탁 5', [discard(2), draw(3), stk(K, 2)]),                                               // 대가 · 재설계
    ], [B('축복 1', 'ap'), B('축복 2', { tags: ['보존'] }), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.5);
  starter(j, '루포_u2');
}

// ════════════════════════════════════════════════════════════════════
// 6. 리온 — 탱커 · 우울. 판결로 막는다 — 「유죄」 겹마다 그 적의 주먹이 무뎌지고, 망치로 도장을 찍어 거둔다
//    ④ 「라이온 시그널」 은 탱커의 결의(담당) · 맞을 때마다 판결이라 강화 카드로 둔다.
// ════════════════════════════════════════════════════════════════════
function lion(j) {
  const H = '리온', K = '유죄';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '빌런에게 내리는 판결', carrier: 'enemy', cap: 3, per: [{ stat: 'dealt', v: -0.07 }] };
  delete h.keywords;
  h.passives = [
    pas('판결봉', 'play', [stk(K, 1, E1)], { when: { type: '공격' }, limit: 2 }),
    pas('히어로의 정의봉', 'fightStart', [st('피해 감소', 1)]),
  ];
  setCards(j, [
    // u1 열기 — 저학년 「유죄 선언」: 앞의 적들을 꾸짖고 막는다
    card(H, 1, '유죄 선언', 1, '스킬', [stk(K, 1, EA), sh(1.2), draw(1)], [
      O('중형 선고', [stk(K, 2, EA), sh(1.5), draw(1)]),
      O('약식 판결', [stk(K, 1, EA), sh(0.8), draw(1)], { cost: 0 }),
      O('즉결 재판', [stk(K, 3, EA), sh(2.6), tough(1, EA)], { cost: 2 }),                       // 비용↑ — 유죄 셋 · 격파
      O('한 명만 유죄', [stk(K, 2, E1), sh(1.2), pull(SU)]),                                      // 서치 · 재설계
      O('집행유예', [discard(1), stk(K, 2, EA), sh(1.6)]),                                        // 대가
    ], [B('히어로 망토', 'guard'), B('증거 서류', 'draw'), B('법정의 위엄', [stk(K, 1, E1)])]),
    // u2 굴리기 — 쓸데없는 수수께끼(히어로명 스핑크스)
    card(H, 2, '수수께끼 하나 더', 1, '스킬', [sh(1.3), draw(1), stk(K, 1, E1)], [
      O('어려운 수수께끼', [sh(1.7), draw(1), stk(K, 1, E1)]),
      O('쉬운 수수께끼', [sh(0.8), draw(1), stk(K, 1, E1)], { cost: 0 }),
      O('정답은 너!', [sh(1.6), draw(1), inspire, stk(K, 2, E1)]),                                        // 재설계 — 영감
      O('끝없는 수수께끼', [sh(1.0), draw(1), power(rule('turnStart', [sh(0.4), stk(K, 1, TOP)]))], { power: true }),  // 강화화(BEST)
      O('힌트 쪽지', [sh(1.5), stk(K, 1, E1), pull()]),
    ], [B('갈기 방패', 'guard'), B('힌트 하나', 'draw'), B('번뜩이는 정답', 'ap')]),
    // u3 터뜨리기 — 어사이드 「심판의 망치」(도장): 유죄를 찍어 거둔다 — 쥐면 방어, 찍으면 피해
    card(H, 3, '심판의 망치', 1, '공격', [ddef(0.6), per(K), ddef(0.3), spendAll(K)], [
      O('무거운 망치', [ddef(0.75), per(K), ddef(0.35), spendAll(K)]),
      O('판결봉 톡', [ddef(0.4), per(K), ddef(0.22), spendAll(K)], { cost: 0 }),
      O('최후의 판결', [{ k: 'strip' }, ddef(1.2), per(K), ddef(0.6)], { cost: 2 }),
      O('법정 흔들기', [ddef(0.6), ifStack(K, 3), per(K), ddef(0.45), spendAll(K)]),
      O('집단 처벌', [discard(1), ddef(0.55, EA), each(K), ddef(0.25, EA)]),                      // 대가
    ], [B('정의의 무게', 'power'), B('죄목 확인', [stk(K, 1, E1)]), B('단단한 갈기', [sh(0.5)])]),
    // u4 완성형(강화) — 라이온 시그널: 결의(담당 세기 버프) · 맞을 때마다 때린 적에게 유죄
    card(H, 4, '라이온 시그널', 1, '강화', [st('결의', 1), power(rule('hurt', [stk(K, 1, E1)], { limit: 2 }))], [
      O('정의의 시그널', [st('결의', 1), power(rule('hurt', [stk(K, 1, E1), sh(0.3)], { limit: 2 }))]),
      O('작은 시그널', [st('결의', 1), power(rule('hurt', [stk(K, 1, E1)], { limit: 2 }))], { cost: 0 }),
      O('히어로 등장', [st('결의', 1), sh(0.8), power(rule('hurt', [stk(K, 1, E1)], { limit: 2 }))], { tags: ['개전'] }),
      O('사자의 포효', [stk(K, 1, EA), draw(1), power(rule('hurt', [stk(K, 1, E1)], { limit: 2 }), reach(K, 3, [st('반격', 1)]))]),  // 재설계 — 결의 대신 판결
      O('사건 파일', [st('결의', 1), srch({ who: 'self', type: '공격' }), power(rule('hurt', [stk(K, 1, E1)], { limit: 2 }))]),
    ], [B('신호탄', 'draw'), B('히어로 자세', 'defUp'), B('히어로 꿈나무', [sh(0.5)])]),
    // u5 굴리기(공격) — 히어로 펀치: 방어 기반 한 대 · 유죄 · 막기
    card(H, 5, '히어로 펀치', 1, '공격', [ddef(0.75), stk(K, 1, E1), sh(0.7)], [
      O('신탁 1', [ddef(0.95), stk(K, 1, E1), sh(0.9)]),
      O('신탁 2', [ddef(0.5), stk(K, 1, E1), sh(0.5)], { cost: 0 }),
      O('신탁 3', [ddef(0.75), stk(K, 1, E1), ifStack(K, 3), ddef(0.6)]),
      O('신탁 4', [ddef(0.75), stk(K, 1, E1), srch(SKL)]),                                       // 서치(BEST)
      O('신탁 5', [ddef(0.8, EA), stk(K, 1, EA)]),                                               // 재설계 — 광역
    ], [B('축복 1', 'weakSpot'), B('축복 2', 'power'), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  scaleC(j, '리온_u5', 0.8);
  starter(j, '리온_u1');
}

// ════════════════════════════════════════════════════════════════════
// 7. 마고 — 서포터(회복) · 순수. 「목장 친구」가 곁에 있는 동안 턴마다 낫고, 아이들을 건드리면 다 같이 들이받는다
//    3단계: 생성 카드 「사료 한 줌」(둘이면 「고급 사료」) · ④ 「아기 동물 돌보기」 를 친구를 세는 1코 마무리로.
// ════════════════════════════════════════════════════════════════════
function mago(j) {
  const H = '마고', K = '목장 친구', F = '마고_feed', F2 = '마고_feed2';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '곁에 모여드는 작은 동물들', carrier: 'self', cap: 4, per: [{ stat: 'hot', ratio: 0.25 }] };
  delete h.keywords;
  h.passives = [
    pas('비스트 로드', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 2 }),
    pas('아이들은 건드리지 마', 'hurt', [per(K), ddef(0.2, E1)], { when: { guarded: true }, limit: 1 }),
  ];
  token(j, { id: F, name: '사료 한 줌', cost: 0, type: '스킬', tags: ['소멸'], evolve: { n: 2, into: F2 }, fx: [stk(K, 1), heal(0.25)], blurb: '형체를 알 수 없지만 동물들은 아주 좋아합니다.' });
  token(j, { id: F2, name: '고급 사료', cost: 0, type: '스킬', tags: ['소멸'], fx: [stk(K, 2), heal(0.5), draw(1)], blurb: '두 줌을 꾹꾹 뭉친 특제 사료입니다.' });
  setCards(j, [
    // u1 굴리기 — 저학년 「마고 마구 회복해」: 전체 지속 회복(초재생)
    card(H, 1, '마고 마구 회복해', 1, '스킬', [heal(0.8), stk(K, 1), st('초재생', 1)], [
      O('마구마구 회복', [heal(1.0), stk(K, 2), st('초재생', 1)]),
      O('조금만 회복', [heal(0.5), stk(K, 1), st('초재생', 1)], { cost: 0 }),
      O('양털 담요', [heal(1.4), make(F, 2), st('초재생', 2)], { cost: 2 }),                     // 비용↑ — 사료 둘
      O('다 함께 모여', [heal(0.8), stk(K, 1), ifStack(K, 3), st('초재생', 2)]),
      O('풀 한 다발', [discard(1), heal(1.0), make(F, 2)]),                                      // 대가 · 재설계
    ], [B('포근한 양털', 'heal'), B('튼튼한 뿔', 'defUp'), B('친구 하나 더', [stk(K, 1)])]),
    // u2 열기 — 사료(형체 없는 가공식품)를 한 줌씩 나눠 둔다
    card(H, 2, '사료가 제일 맛있어', 1, '스킬', [make(F, 2), draw(1)], [
      O('고급 사료', [make(F, 3), draw(1)]),
      O('사료 한 줌', [make(F, 1), draw(1)], { cost: 0 }),
      O('사료 나눠 주기', [make(F, 2), stk(K, 1), draw(2, { who: 'other' })]),                   // 서치
      O('든든한 배', [make(F, 2), draw(1), sh(0.6)], { tags: ['보존'] }),
      O('우주식량', [stk(K, 3), draw(1), inspire, make(F, 2)]),                                            // 재설계 — 사료 없이 영감
    ], [B('여물통', 'draw'), B('사료 봉지', [make(F, 1)]), B('가공식품 사랑', 'ap')]),
    // u3 터뜨리기 — 아이들 일엔 목소리가 낮아진다: 친구들을 다 데리고 들이받기(HOT 와 맞바꿈)
    card(H, 3, '내 목장에서 나가', 1, '공격', [ddef(0.5), per(K), ddef(0.25), spendAll(K)], [
      O('당장 나가!', [ddef(0.65), per(K), ddef(0.3), spendAll(K)]),
      O('저리 가', [ddef(0.35), per(K), ddef(0.18), spendAll(K)], { cost: 0 }),
      O('목장 총출동', [ddef(0.9, EA), perTag(F), ddef(0.3, EA), exileAll(F)], { cost: 2 }),    // 비용↑ · 재설계 — 사료로 꾀어 광역
      O('목장 순찰', [ddef(0.7), stk(K, 1), power(rule('turnEnd', [per(K), ddef(0.12, ER)]))], { power: true }),   // 강화화
      O('뿔 울타리', [ddef(0.5), per(K), sh(0.5), stk(K, 1)]),
    ], [B('힘껏 들이받기', 'power'), B('발굽 자국', 'frost'), B('앞장서기', [sh(0.4)])]),
    // u4 완성형(1코 마무리) — 결의(담당 세기 버프) · 친구 1개당 회복(쓰지 않음)
    card(H, 4, '아기 동물 돌보기', 1, '스킬', [st('결의', 1), per(K), heal(0.2)], [
      O('정성껏 돌보기', [st('결의', 1), per(K), heal(0.26)]),
      O('목장 일과', [st('결의', 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),     // 강화화(BEST) — 옛 엔진 자리
      O('다 함께 낮잠', [per(K), sh(0.3), make(F, 1)]),                                          // 재설계
      O('총출동 준비', [st('결의', 1), per(K), heal(0.35), spendAll(K)]),                        // 대가 — 다 쓰는 쪽
      O('우유병', [st('결의', 1), per(K), heal(0.2), srch({ who: 'self', type: '스킬' })]),      // 서치
    ], [B('작은 손길', 'heal'), B('가축 목걸이', 'draw'), B('뿔 세우기', [stk(K, 1)])]),
    // u5 굴리기(공격) — 양몰이: 들이받으며 친구를 모으고 사료를 챙긴다
    card(H, 5, '양몰이 돌진', 1, '공격', [ddef(0.6), stk(K, 1), make(F, 1)], [
      O('신탁 1', [ddef(0.8), stk(K, 1), make(F, 1)]),
      O('신탁 2', [ddef(0.6, EA), stk(K, 1), make(F, 1)]),                                             // 재설계 — 광역
      O('신탁 3', [ddef(0.6), make(F, 1), srch(SKL, 2)]),                                           // 서치(BEST)
      O('신탁 4', [ddef(0.6), stk(K, 1), power(rule('turnStart', [make(F, 1)]))], { power: true }),   // 강화화
      O('신탁 5', [stk(K, 2), perTag(F), ddef(0.7), exileAll(F)]),                       // 대가 — 사료를 다 쓴다
    ], [B('축복 1', 'weakSpot'), B('축복 2', 'power'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '마고_u2');
}

// ════════════════════════════════════════════════════════════════════
// 8. 모모 — 딜러 · 활발. 「분신」이 대신 맞고 따라 치다가, 안아준닷으로 한꺼번에 터진다(충격 — 담당)
//    3단계: 생성 카드 「번개 수리검」 · ④ 「카게닌자의 극의」 를 손의 수리검을 세는 1코 마무리로.
// ════════════════════════════════════════════════════════════════════
function momo(j) {
  const H = '모모', K = '분신', S = '모모_shuriken';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '따라 던지고 대신 맞는 람쥐 분신', carrier: 'self', cap: 2, guard: true, cut: 0.3 };
  delete h.keywords;
  h.passives = [
    pas('분신술', 'play', [per(K), dmg(0.2, ER)], { when: { type: '공격' }, limit: 1 }),
    pas('통나무 바꿔치기', 'fightStart', [stk(K, 1)]),
  ];
  token(j, { id: S, name: '번개 수리검', cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(0.3), st('충격', 1, E1)], blurb: '람쥐썬더를 머금은 작은 수리검입니다.' });
  setCards(j, [
    // u1 터뜨리기 — 저학년 「두배로 안아준닷」: 분신 자폭 광역 + 감전(충격)
    card(H, 1, '두배로 안아준닷', 1, '공격', [per(K), dmg(0.3, EA), spendAll(K), st('충격', 1, EA)], [
      O('세배로 안아준닷', [per(K), dmg(0.38, EA), spendAll(K), st('충격', 1, EA)]),
      O('살짝 안아준닷', [per(K), dmg(0.22, EA), spendAll(K), st('충격', 1, EA)], { cost: 0 }),
      O('찌릿 포옹', [per(K), dmg(0.55, EA), spendAll(K), perTag(S), dmg(0.3, EA)], { cost: 2 }),   // 비용↑ · 재설계 — 수리검까지
      O('다시 안아준닷', [per(K), dmg(0.3, EA), spendAll(K), srch()]),                            // 서치
      O('꽃꿀 포옹', [st('충격', 1, EA), ifStack(K, 2), per(K), dmg(0.35, EA), spendAll(K)]),
    ], [B('힘껏 포옹', 'power'), B('빈틈 포옹', 'weakSpot'), B('신난 람쥐', [stk(K, 1)])]),
    // u2 굴리기 — 평타 전기 수리검 두 번
    card(H, 2, '전기 수리검', 1, '공격', [hits(2, 0.5, E1), st('충격', 1, E1), stk(K, 1)], [
      O('고압 수리검', [hits(2, 0.62, E1), st('충격', 1, E1), stk(K, 1)]),
      O('정전기 수리검', [hits(2, 0.32, E1), st('충격', 1, E1), stk(K, 1)], { cost: 0 }),
      O('번개 흩뿌리기', [hits(4, 0.5), st('충격', 2, EA), make(S, 2)], { cost: 2 }),            // 비용↑ · 재설계 — 수리검 둘
      O('번개로 콩 볶기', [hits(2, 0.5, E1), st('충격', 1, E1), inspire, stk(K, 2)]),
      O('람쥐썬더', [hits(2, 0.45, E1), power(rule('turnStart', [make(S, 1)]))], { power: true }),   // 강화화(BEST)
    ], [B('찌릿찌릿', 'power'), B('감전', 'frost'), B('손목 스냅', 'draw')]),
    // u3 열기 — 환영술: 분신 + 수리검
    card(H, 3, '환영술', 1, '스킬', [stk(K, 1), make(S, 1), draw(1)], [
      O('다중 환영술', [stk(K, 2), make(S, 1), draw(1)]),
      O('잔상', [stk(K, 1), make(S, 1)], { cost: 0 }),
      O('연막탄', [stk(K, 3), draw(1), st('회피', 1)]),
      O('통나무 진법', [stk(K, 1), make(S, 1), srch({ who: 'self', type: '공격' }, 2)]),           // 서치
      O('그림자 수련', [discard(1), stk(K, 2), make(S, 2)]),                                      // 대가 · 재설계
    ], [B('손가락 인', 'draw'), B('두루마리', 'ap'), B('통나무 하나 더', [stk(K, 1)])]),
    // u4 완성형(1코 마무리) — 카게닌자의 극의: 손의 수리검 1장당 한 번 더(수리검은 남는다)
    card(H, 4, '카게닌자의 극의', 1, '공격', [dmg(0.6), perTag(S), dmg(0.3), stk(K, 1)], [
      O('그림자 분신술', [dmg(0.75), perTag(S), dmg(0.35), stk(K, 1)]),
      O('카게의 비전', [dmg(0.8), stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),        // 강화화(BEST) — 옛 엔진 자리
      O('수리검 비', [discard(1), dmg(0.8), perTag(S), dmg(0.45)]),                                // 대가 · 재설계
      O('닌자 두루마리', [dmg(0.6), perTag(S), dmg(0.3), srch({ who: 'self', type: '스킬' })]),   // 서치
      O('연기 구슬', [dmg(0.4), perTag(S), dmg(0.2), stk(K, 1)], { cost: 0 }),
    ], [B('닌자 호흡', 'atkUp'), B('비전서 한 장', 'draw'), B('수리검 한 자루', [make(S, 1)])]),
    // u5 굴리기(스킬) — 나뭇잎 은신: 분신을 남기고 숨어 수리검을 챙긴다
    card(H, 5, '나뭇잎 은신', 1, '스킬', [stk(K, 1), sh(0.9), make(S, 1)], [
      O('신탁 1', [stk(K, 1), sh(1.2), make(S, 1)]),
      O('신탁 2', [stk(K, 1), sh(0.6)], { cost: 0 }),
      O('신탁 3', [stk(K, 2), sh(0.9), srch(ATK, 2)]),                                          // 서치(BEST)
      O('신탁 4', [discard(1), stk(K, 2), make(S, 2)]),                                             // 대가
      O('신탁 5', [make(S, 2), sh(0.6)]),                                                      // 재설계
    ], [B('축복 1', 'guard'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '모모_u3');
}

// ════════════════════════════════════════════════════════════════════
// 9. 밍스 — 딜러 · 활발 · 1성(연계: 동료의 공격이 「졸업 미룸」을 채운다). 겹마다 파티가 세지고, 졸업 시험으로 다 쏟는다
//    3단계: 「오늘은 자습」 을 개전 강화 시동으로, 「요령은 없고 시범만」 을 미룸을 세는 1코 마무리(기본 카드를 다시 꺼내 가르침)로.
// ════════════════════════════════════════════════════════════════════
function mynx(j) {
  const H = '밍스', K = '졸업 미룸';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '제자를 붙잡아 두는 핑계', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.1, who: 'allies' }],
    rules: [{ name: '졸업은 아직', when: { on: 'stackReach', id: K, n: 3 }, limit: { per: 'fight', n: 2 }, fx: [spendAll(K), st('사기', 1)] }] };
  delete h.keywords;
  h.passives = [
    pas('시범 수업', 'play', [stk(K, 1)], { when: { type: '공격', who: 'other' }, limit: 1 }),
    pas('개학', 'fightStart', [stk(K, 1)]),
  ];
  setCards(j, [
    // u1 굴리기 — 저학년 「캬오오~」: 고함 범위 + 약화(담당)
    card(H, 1, '캬오오~', 1, '공격', [dmg(0.75, EA), st('약화', 1, EA), stk(K, 1)], [
      O('캬오오오오~!', [dmg(0.9, EA), st('약화', 1, EA), stk(K, 1)]),
      O('캬오', [dmg(0.5, EA), st('약화', 1, EA), stk(K, 1)], { cost: 0 }),
      O('교실 울림', [dmg(1.3, EA), st('약화', 2, EA), drawBasic()], { cost: 2 }),                // 비용↑ — 시작 카드 다시
      O('꾸중 한 마디', [discard(1), dmg(1.8), st('약화', 2, E1)]),                              // 대가 · 재설계
      O('쉬는 시간 끝!', [dmg(0.75, EA), st('약화', 1, EA), ifStack(K, 2), dmg(0.4, EA)]),
    ], [B('목청 높이기', 'power'), B('쩌렁쩌렁', 'frost'), B('선생님의 위엄', [stk(K, 1)])]),
    // u2 완성형(1코 마무리) — 요령 없이 시범부터: 미룸 1개당 한 대(쓰지 않음) · 시작 카드 1장을 다시 손에
    card(H, 2, '요령은 없고 시범만', 1, '공격', [dmg(0.8), per(K), dmg(0.25), drawBasic()], [
      O('확실한 시범', [dmg(1.0), per(K), dmg(0.3), drawBasic()]),
      O('대충 시범', [dmg(0.55), per(K), dmg(0.18), drawBasic()], { cost: 0 }),
      O('매일 같은 수업', [dmg(0.8), drawBasic(), power(rule('turnEnd', [per(K), dmg(0.2, ER)]))], { power: true }),   // 강화화(BEST)
      O('졸업장 수여', [dmg(0.8), per(K), dmg(0.35), spendAll(K)]),                              // 대가 · 재설계
      O('전원 따라 해', [dmg(0.8), stk(K, 1), draw(2, { who: 'other' })]),                       // 서치 · 재설계
    ], [B('발톱 힘주기', 'power'), B('교과서 펼치기', 'draw'), B('출석부', [stk(K, 1)])]),
    // u3 터뜨리기 — 결국 졸업: 미룬 만큼 한 번에(그 대신 버프가 사라진다)
    card(H, 3, '졸업 시험', 1, '공격', [per(K), dmg(0.45, EA), spendAll(K), draw(1)], [
      O('깐깐한 졸업 시험', [per(K), dmg(0.55, EA), spendAll(K), draw(1)]),
      O('쪽지 시험', [per(K), dmg(0.3, EA), spendAll(K), draw(1)], { cost: 0 }),
      O('졸업식', [per(K), dmg(0.8, EA), spendAll(K), st('사기', 1)], { cost: 2 }),              // 비용↑ — 사기
      O('졸업은 또 미룸', [per(K), dmg(0.4, EA), draw(1), stk(K, 1)]),
      O('졸업 앨범', [per(K), dmg(0.45, EA), spendAll(K), pull(SU)]),                            // 서치
    ], [B('졸업장', 'power'), B('열쇠 숨기기', 'draw'), B('종소리', 'ap')]),
    // u4 시동(개전 강화) — 오늘은 자습: 협공(동행) · 매 턴 졸업 미룸
    card(H, 4, '오늘은 자습', 1, '강화', [st('협공', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('모범생 칭찬', [st('협공', 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('쉬는 시간', [st('협공', 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('조회 시간', [stk(K, 2), draw(2, { basic: true }), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),   // 재설계
      O('숙제 검사', [st('협공', 1), stk(K, 2), power(rule('turnStart', [stk(K, 1)]))]),         // 대가 — 개전을 뗌
      O('자습 과제', [st('협공', 1), draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
    ], [B('캔 사료 상자', 'atkUp'), B('숲속 별채', 'draw'), B('사료 선물', [stk(K, 1)])], { tags: ['개전'] }),
    // u5 유틸(기본 카드 연료) — 보충 수업: 미룸 · 시작 카드를 다시 꺼내 가르친다
    card(H, 5, '보충 수업', 1, '스킬', [stk(K, 2), drawBasic(), sh(0.7)], [
      O('신탁 1', [stk(K, 2), drawBasic(), sh(1.0)]),
      O('신탁 2', [stk(K, 1), drawBasic()], { cost: 0 }),
      O('신탁 3', [stk(K, 2), srch(SU, 2), sh(0.6)]),                                               // 서치(BEST)
      O('신탁 4', [stk(K, 2), drawBasic(), power(rule('play', [stk(K, 1)], { when: { type: '스킬', who: 'other' }, limit: 1 }))], { power: true }),   // 강화화
      O('신탁 5', [discard(1), stk(K, 3), draw(2, { basic: true })]),                                             // 대가
    ], [B('축복 1', 'ap'), B('축복 2', { tags: ['보존'] }), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '밍스_u4');
}

// ════════════════════════════════════════════════════════════════════
// 10. 바나 — 서포터(실드) · 활발 · 2성. 「단조」 겹마다 주는 실드↑, 둘이면 명검 완성(사기)
//     3단계: 기본 카드를 벼려 생성 카드 「벼린 검」으로(기본 카드 연료) · ④ 를 단조와 손의 검을 세는 1코 마무리로.
// ════════════════════════════════════════════════════════════════════
function bana(j) {
  const H = '바나', K = '단조', BL = '바나_blade';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '두드려 벼리는 영웅의 검', carrier: 'self', cap: 3, per: [{ stat: 'guard', v: 0.1 }],
    rules: [{ name: '명검 완성', when: { on: 'stackReach', id: K, n: 2 }, limit: { per: 'fight', n: 4 }, fx: [spendAll(K), st('사기', 1), draw(1)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('풀무질', 'play', [stk(K, 1)], { when: { type: '스킬' }, limit: 2 }),
    pas('정령 일꾼', 'fightStart', [stk(K, 1), sh(1.5)]),
  ];
  token(j, { id: BL, name: '벼린 검', cost: 0, type: '스킬', tags: ['소멸'], fx: [sh(0.8), stk(K, 1)], blurb: '흔한 쇠붙이도 바나의 망치를 거치면 영웅의 검이 됩니다.' });
  setCards(j, [
    // u1 굴리기 — 저학년 「호두까기 장인」: 보호막
    card(H, 1, '호두까기 장인', 1, '스킬', [sh(1.5), stk(K, 1)], [
      O('장인의 손놀림', [sh(1.9), stk(K, 1)]),
      O('호두 한 알', [sh(0.9), stk(K, 1)], { cost: 0 }),
      O('명장의 작업대', [sh(2.8), stk(K, 2), draw(1)], { cost: 2 }),                            // 비용↑ — 드로우
      O('호두 껍질 방패', [sh(1.2), make(BL, 1)]),                                                // 재설계 — 검 한 자루
      O('호두까기 공방', [sh(1.2), stk(K, 1), power(rule('turnStart', [sh(0.4)]))], { power: true }),          // 강화화(BEST)
    ], [B('두꺼운 앞치마', 'guard'), B('해바라기씨', 'draw'), B('풀무 바람', [stk(K, 1)])]),
    // u2 평타 망치 던지기: 범위 + 모든 방어력 감소(취약 — 담당)
    card(H, 2, '모루 위의 망치', 1, '공격', [dmg(0.75, EA), st('취약', 1, EA), stk(K, 1)], [
      O('힘찬 망치질', [dmg(0.95, EA), st('취약', 1, EA), stk(K, 1)]),
      O('톡톡 망치질', [dmg(0.5, EA), st('취약', 1, EA), stk(K, 1)], { cost: 0 }),
      O('전설의 검 두드리기', [dmg(1.3, EA), st('취약', 2, EA), make(BL, 2)], { cost: 2 }),       // 비용↑ · 재설계 — 검 둘
      O('담금질', [discard(1), dmg(1.8), st('취약', 2, E1)]),                                     // 대가 · 재설계
      O('불꽃 튀기기', [dmg(0.75, EA), st('취약', 1, EA), srch({ who: 'self', type: '스킬' })]),   // 서치
    ], [B('시뻘건 쇳물', 'power'), B('금 간 곳', 'weakSpot'), B('손에 익은 망치', [stk(K, 1)])]),
    // u3 열기 — 정령 일꾼 호령: 기본 카드 1장을 벼린 검으로(기본 카드 연료)
    card(H, 3, '정령 일꾼들아!', 1, '스킬', [stk(K, 1), tfFrom(BL, '바나_s1'), draw(1)], [
      O('일꾼 총동원', [stk(K, 2), tfFrom(BL, '바나_s1'), draw(1)]),
      O('한 명만 와 줘', [stk(K, 1), tfFrom(BL, '바나_s1')], { cost: 0 }),
      O('불씨 정령', [stk(K, 1), tfFrom(BL, '바나_s1'), srch({ who: 'self', type: '스킬' }, 2)]),               // 서치
      O('정령 응원단', [tfFrom(BL, '바나_s1'), draw(1), inspire, stk(K, 2)]),
      O('작업장 정리', [discard(1), stk(K, 2), sh(1.8)]),                                          // 대가 · 재설계
    ], [B('정령 방패', 'guard'), B('일당 지급', 'ap'), B('일거리 하나 더', 'draw')]),
    // u4 완성형(1코 마무리) — 이야기책에 남을 검: 단조 1개당 · 손의 벼린 검 1장당 실드(쓰지 않음)
    card(H, 4, '이야기책에 남을 검', 1, '스킬', [per(K), sh(0.55), perTag(BL), sh(0.3)], [
      O('전설의 대장장이', [per(K), sh(0.68), perTag(BL), sh(0.38)]),
      O('아침 화로', [draw(1), sh(0.8), power(rule('turnStart', [stk(K, 1)]))], { power: true }),            // 강화화(BEST) — 옛 엔진 자리
      O('영웅에게 바치는 검', [per(K), sh(0.6), spendAll(K), exileAll(BL)]),                       // 대가 · 재설계
      O('설계도', [per(K), sh(0.45), srch()]),                                                     // 서치
      O('풀무 한 번', [per(K), sh(0.38), perTag(BL), sh(0.2)], { cost: 0 }),
    ], [B('달군 쇠', 'draw'), B('첫 담금질', 'defUp'), B('한 자루 더', [make(BL, 1)])]),
    // u5 굴리기(공격) — 시험 베기: 갓 벼린 검을 휘둘러 보고 단조를 잇는다
    card(H, 5, '시험 베기', 1, '공격', [dmg(0.9), stk(K, 1), sh(0.5)], [
      O('신탁 1', [dmg(1.15), stk(K, 1), sh(0.6)]),
      O('신탁 2', [dmg(0.55), stk(K, 1), sh(0.3)], { cost: 0 }),
      O('신탁 3', [dmg(0.9), stk(K, 1), srch(SKL)]),                                             // 서치(BEST)
      O('신탁 4', [dmg(0.8), power(rule('turnStart', [make(BL, 1)]))], { power: true }),       // 강화화
      O('신탁 5', [dmg(0.9), perTag(BL), dmg(0.5), exileAll(BL)]),                       // 대가 — 손의 검을 태운다
    ], [B('축복 1', 'power'), B('축복 2', 'frost'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.3);
  starter(j, '바나_u3');
}

// ════════════════════════════════════════════════════════════════════
// 11. 버터 — 딜러 · 활발(연계: 동료의 부탁(스킬)이 「옐로카드」를 채운다). 파티가 맞을수록 옐로카드 — 넷이면 레드카드(광역)
//     3단계: ④ = 「구덩이 파기」(보존 · 옐로카드를 세는 1코 마무리).
// ════════════════════════════════════════════════════════════════════
function butter(j) {
  const H = '버터', K = '옐로카드';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '꾹 참는 부당한 대우', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.08 }],
    rules: [{ name: '레드카드', when: { on: 'stackReach', id: K, n: 4 }, limit: { per: 'fight', n: 2 }, fx: [spendAll(K), dmg(1.4, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('참다 참다', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 3 }),
    pas('심부름 목록', 'play', [stk(K, 1)], { when: { type: '스킬', who: 'other' }, limit: 1 }),     // 연계 — 동료의 부탁
  ];
  setCards(j, [
    // u1 굴리기 — 저학년 「버터 플라이!」: 튕기는 탄환
    card(H, 1, '버터 플라이!', 1, '공격', [hits(4, 0.35), stk(K, 1)], [
      O('버터 플라이 킥!', [hits(4, 0.42), stk(K, 1)]),
      O('살짝 플라이', [hits(2, 0.35), stk(K, 1)], { cost: 0 }),
      O('다이빙 헤딩', [hits(6, 0.38), stk(K, 2), tough(1)], { cost: 2 }),                        // 비용↑ — 격파
      O('경고 누적', [hits(4, 0.3), ifStack(K, 2), dmg(0.6, ER)]),
      O('버터 회오리', [hits(4, 0.3), stk(K, 1), srch({ who: 'self', type: '공격' })]),          // 서치
    ], [B('한껏 날아오르기', 'power'), B('옐로 받은 녀석', 'weakSpot'), B('신나는 꼬리', [stk(K, 1)])]),
    // u2 열기 — 부탁은 거절 못 함: 대신 막고 참는다
    card(H, 2, '부탁은 거절 안 해', 1, '스킬', [sh(1.0), stk(K, 2), draw(1)], [
      O('뭐든 맡겨 줘', [sh(1.3), stk(K, 2), draw(1)]),
      O('잠깐만 도와줄게', [sh(0.6), stk(K, 1), draw(1)], { cost: 0 }),
      O('대신 맞아 줄게', [sh(1.0), stk(K, 2), st('반격', 1)]),
      O('심부름 갔다 올게', [stk(K, 2), pull({ who: 'self', type: '공격' }), sh(1.2)]),                    // 서치 · 재설계
      O('착한 강아지', [discard(1), sh(1.6), stk(K, 3)]),                                         // 대가
    ], [B('두 발로 버티기', 'guard'), B('부탁 쪽지', 'draw'), B('든든한 등', [stk(K, 1)])]),
    // u3 완성형(1코 마무리 · 보존) — 구덩이에 간식 묻기: 쥐고 있을수록(옐로카드가 쌓일수록) 세진다
    card(H, 3, '구덩이 파기', 1, '공격', [dmg(0.7), per(K), dmg(0.2)], [
      O('깊은 구덩이', [dmg(0.85), per(K), dmg(0.24)], { tags: ['보존'] }),
      O('얕은 구덩이', [dmg(0.45), per(K), dmg(0.12)], { cost: 0, tags: ['보존'] }),
      O('간식 묻어 두기', [dmg(0.7), per(K), dmg(0.15), { k: 'when', on: 'handEnd' }, stk(K, 1)], { tags: ['보존'] }),   // 재설계
      O('구덩이 밭', [dmg(0.55, EA), per(K), dmg(0.15, EA), draw(1)]),                            // 대가 — 보존을 뗌
      O('땅파기 습관', [dmg(0.75), power(rule('turnStart', [stk(K, 1)]))], { power: true, tags: ['보존'] }),   // 강화화(BEST)
    ], [B('삽질 한 번 더', 'power'), B('흙투성이', 'frost'), B('신나게 파기', [stk(K, 1)])], { tags: ['보존'] }),
    // u4 터뜨리기 — 에이프런 매그넘: 옐로카드 1장당 한 발 · 전부(2코 하나)
    card(H, 4, '에이프런 매그넘', 2, '공격', [dmg(1.6), per(K), dmg(0.35), spendAll(K)], [
      O('풀파워 매그넘', [dmg(1.9), per(K), dmg(0.42), spendAll(K)]),
      O('에이프런 슛', [dmg(1.1), per(K), dmg(0.25), spendAll(K)], { cost: 1 }),
      O('레드카드 각오', [dmg(0.9), stk(K, 2), power(reach(K, 4, [dmg(0.8)]))], { cost: 1, power: true }),            // 강화화
      O('레드카드 매그넘', [dmg(3.4), per(K), dmg(0.8), tough(2)], { tags: ['소멸'] }),             // 대가 · 재설계
      O('앞치마 펄럭', [dmg(1.6), per(K), dmg(0.35), srch()]),                                     // 서치
    ], [B('앞치마 꽉 매기', 'power'), B('골문 구석', 'weakSpot'), B('짧은 도움닫기', 'ap')]),
    // u5 유틸(회수) — 공 물어 오기: 던진 고유 카드를 버린 더미에서 물어 온다
    card(H, 5, '공 물어 오기', 1, '스킬', [stk(K, 1), sh(0.9), pull(SU)], [
      O('신탁 1', [stk(K, 2), sh(1.1), pull(SU)]),
      O('신탁 2', [stk(K, 1), sh(0.5), pull(SU)], { cost: 0 }),
      O('신탁 3', [stk(K, 2), sh(1.0), power(rule('stackReach', [pull(SU)], { when: { id: K, n: 4 } }))], { power: true }),   // 강화화 — 레드카드마다 회수
      O('신탁 4', [stk(K, 2), sh(0.6), srch(ATK, 2)]),                                                 // 서치(BEST) · 재설계
      O('신탁 5', [discard(1), stk(K, 3), sh(1.3)]),                                               // 대가
    ], [B('축복 1', 'draw'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '버터_u2');
  // 애착 장비(전설 — 싸움당 1코 카드 1.5~2.5장): 범용 몫은 맞을 때 새총 반격, 애착 몫은 옐로카드
  for (const e of j.equips || []) {
    e.effect = [pas('분노의 새총', 'hurt', [dmg(0.6, ER)], { limit: 2 })];
    e.affinityEffect = [pas('경고 누적', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 1 })];
  }
}

// ════════════════════════════════════════════════════════════════════
// 12. 베니 — 딜러 · 활발 · 2성. 먹으면 「포만감」(공격마다 하나씩 꺼짐), 배고프면 도끼가 더 무겁다
//     3단계: 생성 카드 「꿀 절인 생선」 — 먹어서 포만감을 채울지, 쥐고 ④ 에 실을지.
// ════════════════════════════════════════════════════════════════════
function beni(j) {
  const H = '베니', K = '포만감', FI = '베니_fish';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '꿀 절인 생선으로 채운 배', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.15 }], consume: 1 };
  delete h.keywords;
  h.passives = [
    pas('먹고 나면 힘', 'kill', [stk(K, 1), heal(0.4)], { limit: 1 }),
    pas('나를 따르라 곰', 'fightStart', [st('피해 감소', 1), make(FI, 1)]),
  ];
  token(j, { id: FI, name: '꿀 절인 생선', cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.3), stk(K, 1)], blurb: '배고픈 곰이 아껴 두는 비상식량입니다.' });
  setCards(j, [
    // u1 열기 — 저학년 「생선 꿀~꺽!」: 하나는 먹고 하나는 챙긴다
    card(H, 1, '생선 꿀~꺽!', 1, '스킬', [heal(0.6), stk(K, 1), make(FI, 1)], [
      O('두 마리째', [heal(0.8), stk(K, 1), make(FI, 2)]),
      O('한 입 간식', [heal(0.4), stk(K, 1), make(FI, 1)], { cost: 0 }),
      O('배 터지게 먹기', [heal(1.8), stk(K, 3), draw(2)], { cost: 2 }),                          // 비용↑ — 포만감 가득 · 드로우
      O('사료 나눠 먹기', [heal(0.6), make(FI, 1), draw(2, { who: 'other' })]),                   // 서치
      O('아침 생선', [make(FI, 2), inspire, stk(K, 1)], { tags: ['개전'] }),                      // 재설계
    ], [B('기름 오른 생선', 'heal'), B('생선 냄새', 'draw'), B('입가심', [stk(K, 1)])]),
    // u2 굴리기 — 힘으로 하는 공사: 배부르면 한 번 더
    card(H, 2, '힘으로 하는 공사', 1, '공격', [dmg(1.1), ifStack(K, 1), dmg(0.4)], [
      O('곡괭이질', [dmg(1.35), ifStack(K, 1), dmg(0.45)]),
      O('벽돌 나르기', [dmg(0.7), ifStack(K, 1), dmg(0.3)], { cost: 0 }),
      O('강제 철거', [dmg(2.0), ifStack(K, 1), dmg(0.8), tough(1)], { cost: 2 }),                 // 비용↑ — 격파
      O('빈속 공사', [dmg(1.2), notStack(K), dmg(0.9), srch({ who: 'self', type: '스킬' })]),       // 서치
      O('공사장 새참', [dmg(1.0), make(FI, 1)]),                                                  // 재설계
    ], [B('굵은 팔뚝', 'power'), B('망치 자국', 'weakSpot'), B('현장 체력', [sh(0.4)])]),
    // u3 완성형(1코 마무리) — 어사이드 「사료스탕스 뽀너스」: 손의 생선 1장당 한 번 더(먹지 않고 쥔 만큼)
    card(H, 3, '사료스탕스 뽀너스', 1, '공격', [dmg(0.8), perTag(FI), dmg(0.35)], [
      O('곱빼기', [dmg(1.0), perTag(FI), dmg(0.42)]),
      O('뽀너스 정기 배송', [stk(K, 1), power(rule('turnStart', [make(FI, 1)]))], { power: true }),   // 강화화(BEST) — 옛 엔진 자리
      O('생선 한입에', [discard(1), dmg(0.8), perTag(FI), dmg(0.45)]),                             // 대가
      O('든든한 밥심', [per(K), dmg(0.45), heal(0.8)]),                                            // 재설계 — 생선 대신 포만감
      O('사료 축제', [dmg(0.8), perTag(FI), dmg(0.35), srch()]),                                   // 서치
    ], [B('뽀너스 쿠폰', 'power'), B('두툼한 뱃살', 'weakSpot'), B('생선 한 마리 더', [make(FI, 1)])]),
    // u4 터뜨리기 — 배고픈 곰의 도끼: 포만감이 없으면 더 무겁고, 처치하면 먹는다(2코 하나)
    card(H, 4, '배고픈 곰의 도끼', 2, '공격', [dmg(2.0), notStack(K), dmg(1.0), ifKill, stk(K, 2)], [
      O('통나무 쪼개기', [dmg(2.4), notStack(K), dmg(1.1), ifKill, stk(K, 2)]),
      O('손도끼', [dmg(1.3), notStack(K), dmg(0.6), ifKill, stk(K, 2)], { cost: 1 }),
      O('굶주린 도끼', [spendAll(K), dmg(2.6), ifKill, make(FI, 2)]),                              // 대가 · 재설계 — 배를 비우고
      O('곰의 사냥 본능', [dmg(1.4), power(rule('kill', [make(FI, 1)], { limit: 1 }))], { cost: 1, power: true }),   // 강화화
      O('한 입 거리', [dmg(2.0), notStack(K), dmg(1.0), srch()]),                                  // 서치
    ], [B('날 선 도끼날', 'power'), B('배고픈 눈빛', 'weakSpot'), B('나눠 먹는 생선', [heal(0.4)])]),
    // u5 굴리기(스킬) — 어사이드 「나를 따르라 곰」: 앞장서서 막고 생선을 챙긴다
    card(H, 5, '앞장서는 곰', 1, '스킬', [sh(1.0), stk(K, 1), make(FI, 1)], [
      O('신탁 1', [sh(1.3), stk(K, 1), make(FI, 1)]),
      O('신탁 2', [dmg(0.8), stk(K, 1), make(FI, 1)]),                                             // 재설계 — 막기 대신 들이받기
      O('신탁 3', [sh(1.0), make(FI, 1), srch(ATK, 2)]),                                            // 서치(BEST)
      O('신탁 4', [discard(1), sh(1.3), make(FI, 2)]),                                           // 대가
      O('신탁 5', [sh(1.2), make(FI, 2), ifStack(K, 2), st('피해 감소', 2)]),
    ], [B('축복 1', 'guard'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '베니_u1');
}

// ════════════════════════════════════════════════════════════════════
// 13. 베니(베니) — 딜러 · 냉정(원작 방식 고학년 유지). 「꿀범벅」을 바르고, 강타로 거둘지 고학년(넷 이상 기절)까지 둘지
//     3단계: 생성 카드 「꿀단지 폭탄」(꿀 주먹이 만든다) · ④ = 「꿀 던지기」(꿀범벅을 세는 1코 마무리).
// ════════════════════════════════════════════════════════════════════
function beniBeni(j) {
  const H = '베니_베니', K = '꿀범벅', JR = '베니_베니_jar';
  const h = j.heroes[0];
  const share = h.keyword.rules.find(r => r.name === '꿀 나눔');   // 고학년 회복(원작 방식) — 그대로
  h.keyword = { name: K, desc: '끈적하게 들러붙는 꿀', carrier: 'enemy', cap: 6, per: [{ stat: 'dealt', v: -0.05 }], rules: [share] };
  delete h.keywords;
  h.passives = [
    pas('꿀 주먹', 'play', [make(JR, 1)], { when: { every: 4 } }),
  ];
  token(j, { id: JR, name: '꿀단지 폭탄', cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(0.2, EA), stk(K, 1, EA)], blurb: '던지면 깨져서 사방에 꿀이 튑니다.' });
  setCards(j, [
    // u1 열기 — 저학년 「허니밤」: 튕기며 꿀 웅덩이 + 마지막 큰 한 방
    card(H, 1, '허니밤', 1, '공격', [dmg(0.5, EA), stk(K, 1, EA), dmg(0.5)], [
      O('꿀 폭탄', [dmg(0.6, EA), stk(K, 1, EA), dmg(0.7)]),
      O('작은 꿀단지', [dmg(0.35, EA), stk(K, 1, EA), dmg(0.35)], { cost: 0 }),
      O('꿀비', [dmg(1.0, EA), stk(K, 2, EA), st('약화', 1, EA)], { cost: 2 }),                     // 비용↑ — 약화
      O('꿀 굳히기', [dmg(0.5, EA), stk(K, 1, EA), ifStack(K, 4), st('기절', 1, E1)]),
      O('끈적 급소', [dmg(0.5, EA), stk(K, 1, EA), srch()]),                                         // 서치
    ], [B('진한 꿀', 'power'), B('꿀 냄새', 'weakSpot'), B('덧바르기', [stk(K, 1, E1)])]),
    // u2 굴리기 — 꿀 웅덩이: 광역 꿀범벅 + 약화(담당)
    card(H, 2, '꿀 웅덩이', 1, '스킬', [stk(K, 2, EA), st('약화', 1, EA), draw(1)], [
      O('꿀 장마', [stk(K, 3, EA), st('약화', 1, EA), draw(1)]),
      O('꿀 한 방울', [stk(K, 1, EA), st('약화', 1, EA), draw(1)], { cost: 0 }),
      O('거대 꿀 웅덩이', [stk(K, 3, EA), st('약화', 1, EA), power(rule('turnStart', [stk(K, 1, EA)]))], { power: true }),   // 강화화(BEST)
      O('꿀 수확', [dmg(0.6, EA), stk(K, 2, EA)]),                                                  // 재설계
      O('끈끈이 바닥', [discard(1), stk(K, 4, EA), st('약화', 2, EA)]),                              // 대가
    ], [B('손가락 핥기', 'draw'), B('한 바가지', [make(JR, 1)]), B('느릿느릿 꿀', 'ap')]),
    // u3 완성형(1코 마무리) — 평타 꿀 던지기: 꿀 하나 바르고 꿀범벅 1개당 한 대(쓰지 않음)
    card(H, 3, '꿀 던지기', 1, '공격', [dmg(0.6), stk(K, 1, E1), per(K), dmg(0.15)], [
      O('꿀단지 투척', [dmg(0.75), stk(K, 1, E1), per(K), dmg(0.18)]),
      O('꿀 튕기기', [dmg(0.4), stk(K, 1, E1), per(K), dmg(0.1)], { cost: 0 }),
      O('두 번 바르기', [stk(K, 2, E1), ifStack(K, 4), st('기절', 1, E1)]),                         // 재설계 — 고학년 몫을 미리
      O('꿀벌 친구들', [dmg(0.6), stk(K, 1, E1), power(rule('turnEnd', [each(K), dmg(0.08, EA)]))], { power: true }),   // 강화화
      O('단지째 비우기', [discard(1), dmg(0.8), per(K), dmg(0.25)]),                                 // 대가
    ], [B('묵직한 단지', 'power'), B('끈적한 자국', 'frost'), B('한 숟갈 더', [stk(K, 1, E1)])]),
    // u4 터뜨리기 — 꿀단지 강타: 꿀범벅 1개당 한 대 · 전부(고학년 몫과 맞바꿈)
    card(H, 4, '꿀단지 강타', 2, '공격', [dmg(1.6), per(K), dmg(0.35), spendAll(K)], [
      O('꿀단지 박살', [dmg(1.9), per(K), dmg(0.42), spendAll(K)]),
      O('꿀통 내리치기', [dmg(1.1), per(K), dmg(0.25), spendAll(K)], { cost: 1 }),
      O('꿀 해일', [dmg(2.0, EA), each(K), dmg(0.4, EA)], { cost: 3 }),                             // 비용↑ — 광역 · 꿀은 남김
      O('꿀에 파묻기', [dmg(1.6), per(K), dmg(0.35), srch()]),                                       // 서치 · 재설계
      O('단지 깨뜨리기', [dmg(3.2), per(K), dmg(0.7), tough(2)], { tags: ['소멸'] }),                // 대가 · 재설계
    ], [B('꽉 찬 단지', 'power'), B('꿀 냄새 추적', 'weakSpot'), B('베니는 멋있어', 'ap')]),
    // u5 유틸(0코 생성) — 비상 꿀단지: 던질 단지를 챙기고 한 장 뽑는다
    card(H, 5, '비상 꿀단지', 0, '스킬', [make(JR, 1), draw(1)], [
      O('신탁 1', [make(JR, 2), draw(1)]),
      O('신탁 2', [make(JR, 1), draw(1), ifStack(K, 3), make(JR, 1)]),
      O('신탁 3', [make(JR, 1), srch(SU, 2)]),                                                     // 서치(BEST)
      O('신탁 4', [make(JR, 1), power(rule('turnStart', [make(JR, 1)]))], { power: true }),       // 강화화
      O('신탁 5', [discard(1), make(JR, 3)]),                                                // 대가
    ], [B('축복 1', { tags: ['보존'] }), B('축복 2', 'draw'), B('축복 3', [stk(K, 1, E1)])]),
  ]);
  starter(j, '베니_베니_u1');
}

// ════════════════════════════════════════════════════════════════════
// 14. 슈로 — 서포터(버퍼) · 활발. 「검의 영역」(파티 — 덜 다치고 더 세게 · 턴 끝 검기)을 긋고, 세 자루로 거둔다
//     3단계: 「독 없는 투구꽃」 을 개전 강화 시동으로, 「통성명」 을 영역을 세는 1코 마무리로.
// ════════════════════════════════════════════════════════════════════
function shuro(j) {
  const H = '슈로', K = '검의 영역';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '검 끝으로 그어 세운 둘레', carrier: 'ally', cap: 3, decay: 1,
    per: [{ stat: 'taken', v: -0.06 }, { stat: 'dealt', v: 0.06 }],
    rules: [{ name: '강한 상대에게 강함', when: { on: 'turnEnd' }, conds: [{ c: 'stack', id: K, n: 1 }], fx: [per(K), ddef(0.2, ER)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('물러났다 전진', 'hurt', [stk(K, 1)], { when: { guarded: true }, limit: 2 }),
    pas('검으로 사귄 친구', 'fightStart', [stk(K, 1)]),
  ];
  setCards(j, [
    // u1 열기 — 저학년 「조율」: 검의 영역
    card(H, 1, '검 조율', 1, '스킬', [stk(K, 2), sh(0.8), draw(1)], [
      O('숫돌질', [stk(K, 3), sh(0.8), draw(1)]),
      O('칼끝 맞추기', [stk(K, 1), sh(0.5), draw(1)], { cost: 0 }),
      O('세 자루 조율', [stk(K, 3), sh(2.0), st('협공', 1)], { cost: 2 }),                          // 비용↑ — 협공
      O('매일의 조율', [stk(K, 2), sh(0.8), power(rule('turnStart', [sh(0.5), stk(K, 1)]))], { power: true }),  // 강화화(BEST)
      O('맞춤 조언', [stk(K, 2), sh(0.8), pull({ who: 'other' })]),                                  // 서치
    ], [B('가지런한 칼집', 'guard'), B('익숙한 손놀림', 'draw'), B('한 뼘 넓은 둘레', [stk(K, 1)])]),
    // u2 완성형(1코 마무리) — 통성명: 인사하고 칼을 맞댄다 — 영역 1개당 실드(쓰지 않음)
    card(H, 2, '통성명', 1, '공격', [dmg(0.7), per(K), sh(0.4)], [
      O('정중한 인사', [dmg(0.85), per(K), sh(0.5)]),
      O('이름만 대기', [dmg(0.45), per(K), sh(0.25)], { cost: 0 }),
      O('칼을 맞대면 친구', [stk(K, 1), draw(1), ifStack(K, 2), sh(1.6)]),                                    // 재설계
      O('결투 신청서', [dmg(0.8), stk(K, 1), power(rule('turnEnd', [per(K), sh(0.2)]))], { power: true }),       // 강화화
      O('수인의 예법', [discard(1), dmg(0.6), per(K), sh(0.55)]),                                    // 대가
    ], [B('또렷한 목소리', 'power'), B('예의 바른 방패', 'guard'), B('상대 살피기', 'draw')]),
    // u3 터뜨리기 — 장검 · 단도 · 한손검: 영역을 거두며 검을 쏟는다
    card(H, 3, '장검 · 단도 · 한손검', 1, '공격', [hits(3, 0.3, E1), per(K), ddef(0.25, EA), spendAll(K)], [
      O('세 자루 연격', [hits(3, 0.36, E1), per(K), ddef(0.3, EA), spendAll(K)]),
      O('단도만', [hits(2, 0.3, E1), per(K), ddef(0.15, EA), spendAll(K)], { cost: 0 }),
      O('한 자루 더', [hits(4, 0.45, E1), per(K), ddef(0.45, EA), stk(K, 1)], { cost: 2 }),          // 비용↑ · 재설계 — 거두지 않고 넓힘
      O('지키는 영역', [hits(4, 0.3, E1), per(K), ddef(0.25, EA)]),
      O('결투의 검', [per(K), ddef(0.3, EA), spendAll(K), srch({ who: 'self', type: '스킬' })]),       // 서치
    ], [B('손에 익은 검', 'power'), B('틈을 노리는 단도', 'weakSpot'), B('검 바꿔 쥐기', 'ap')]),
    // u4 시동(개전 강화) — 어사이드 「독 없는 투구꽃」: 결의(담당 세기 버프) · 매 턴 영역
    card(H, 4, '독 없는 투구꽃', 1, '강화', [st('결의', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('활짝 핀 투구꽃', [st('결의', 1), power(rule('turnStart', [stk(K, 2)]))], { tags: ['개전'] }),
      O('가지 하나', [st('결의', 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('꽃 피는 결투장', [stk(K, 2), srch(SU, 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),   // 재설계 · 서치
      O('맞댄 칼날', [st('결의', 1), sh(0.6), power(rule('turnStart', [stk(K, 1)]), rule('blocked', [st('반격', 1)], { limit: 1 }))], { tags: ['개전'] }),
      O('우리를 기억해', [st('결의', 1), draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))]),   // 대가 — 개전을 뗌
    ], [B('단단한 꽃대', 'defUp'), B('투구꽃 향기', 'draw'), B('꽃잎 둘레', [stk(K, 1)])], { tags: ['개전'] }),
    // u5 유틸(비용) — 어사이드 「우리를 기억해」: 옛 벗들의 검술을 동료에게 — 동료 카드 하나를 싸게
    card(H, 5, '옛 벗의 검', 1, '스킬', [stk(K, 1), cheap({ who: 'other' }), draw(1)], [
      O('신탁 1', [stk(K, 2), cheap({ who: 'other' }), draw(1)]),
      O('신탁 2', [cheap({ who: 'other' }), draw(1)], { cost: 0 }),
      O('신탁 3', [stk(K, 1), cheap({ who: 'other' }), draw(2, { who: 'other' })]),           // 서치(BEST)
      O('신탁 4', [stk(K, 1), draw(1), power(rule('play', [stk(K, 1)], { when: { type: '공격', who: 'other' }, limit: 1 }))], { power: true }),   // 강화화
      O('신탁 5', [discard(1), stk(K, 2), sh(1.2)]),                                     // 대가 · 재설계
    ], [B('축복 1', 'ap'), B('축복 2', { tags: ['보존'] }), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '슈로_u4');
}

// ════════════════════════════════════════════════════════════════════
// 15. 스패럿 — 서포터(AP) · 순수. 털고 나눈다 — 「전리품」을 먹어 「노획물」(넷이면 통 큰 분배 · AP) 또는 쥐고 쏜다
//     3단계: 통 큰 분배를 AP 로(서포터 AP 축) · ④ = 「해적의 언어」(손의 전리품을 세는 1코 마무리 · 약화 담당).
// ════════════════════════════════════════════════════════════════════
function sparrot(j) {
  const H = '스패럿', K = '노획물', T = '스패럿_t1';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '배에 쌓아 두고 나눠 먹는 노획물', carrier: 'self', cap: 4, per: [{ stat: 'hot', ratio: 0.12 }],
    rules: [{ name: '통 큰 분배', when: { on: 'stackReach', id: K, n: 4 }, limit: { per: 'fight', n: 1 }, fx: [spendAll(K), ap(1)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('해적의 약탈', 'kill', [make(T, 1)], { when: { mine: true }, limit: 1 }),
    pas('해적의 약탈', 'foeShieldBreak', [make(T, 1)], { when: { mine: true }, limit: 1 }),
    pas('진정한 해적의 길', 'fightStart', [make(T, 1), gauge(20)]),
  ];
  const tok = j.cards.find(c => c.id === T);
  Object.assign(tok, { cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.5), stk(K, 1), draw(1)] });
  const BR = ['분쇄'];
  setCards(j, [
    // u1 열기 — 저학년 「오늘은 저녀석이다!」: HP 가장 높은 적을 털고 전리품
    card(H, 1, '오늘은 저녀석이다!', 1, '공격', [dmg(0.8, TOP), make(T, 1)], [
      O('저녀석 확정!', [dmg(1.0, TOP), make(T, 1)], { tags: BR }),
      O('손가락 지목', [dmg(0.5, TOP), make(T, 1)], { cost: 0, tags: BR }),
      O('집중 포화 신호', [hits(4, 0.5, TOP), make(T, 2), st('표식', 1, TOP)], { cost: 2, tags: BR }),   // 비용↑ — 표식
      O('해적의 낙인', [dmg(0.6, TOP), stk(K, 2), draw(1)], { tags: BR }),                              // 재설계
      O('전리품 찜', [dmg(0.8, TOP), make(T, 1), srch({ who: 'self', type: '공격' })], { tags: BR }),   // 서치
    ], [B('겨눈 총구', 'power'), B('깃털의 후예', 'weakSpot'), B('금화 한 닢', 'ap')], { tags: BR }),
    // u2 완성형(1코 마무리) — 강평 「해적의 언어」: 손의 전리품 1장당 한 마디 더 · 소음(약화 2 — 담당)
    card(H, 2, '해적의 언어', 1, '공격', [dmg(0.4), perTag(T), dmg(0.2), st('약화', 2, E1)], [
      O('거친 욕설', [dmg(0.5), perTag(T), dmg(0.36), st('약화', 2, E1)], { tags: BR }),
      O('짧은 욕', [dmg(0.25), perTag(T), dmg(0.2), st('약화', 2, E1)], { cost: 0, tags: BR }),
      O('뱃노래', [dmg(0.5, EA), st('약화', 1, EA), heal(0.5)], { tags: BR }),                       // 재설계
      O('알아듣게 협박', [dmg(0.6), st('약화', 2, E1), srch()], { tags: BR }),                        // 서치
      O('해적식 흥정', [discard(1), make(T, 2), st('약화', 2, E1)]),                                  // 대가 — 분쇄를 뗌
    ], [B('목청 높이기', 'power'), B('전리품 자랑', [make(T, 1)]), B('해적 사전', 'draw')], { tags: BR }),
    // u3 굴리기(생성) — 약탈한 전리품
    card(H, 3, '약탈한 전리품', 1, '스킬', [make(T, 2)], [
      O('대약탈', [make(T, 3)]),
      O('주머니 털기', [make(T, 1), draw(1)], { cost: 0 }),
      O('보물 지도', [make(T, 1), power(rule('turnStart', [make(T, 1)]))], { power: true }),          // 강화화(BEST)
      O('한몫씩 나누기', [make(T, 2), draw(1, { who: 'other' })]),                                    // 서치
      O('노획물 정리', [stk(K, 3), heal(0.9), draw(2)]),                                                       // 재설계
    ], [B('쌍안경', 'draw'), B('챙긴 금붙이', 'ap'), B('찬란한 금은보화', [stk(K, 1)])]),
    // u4 터뜨리기 — 플라즈마 포 일제사격: 손의 전리품 1장당 포탄 · 전리품 모두 장전(소멸)
    card(H, 4, '플라즈마 포 일제사격', 2, '공격', [dmg(1.0, EA), perTag(T), dmg(0.4, EA), exileAll(T)], [
      O('과충전 포격', [dmg(1.2, EA), perTag(T), dmg(0.45, EA), exileAll(T)], { tags: BR }),
      O('소형 플라즈마', [dmg(0.7, EA), perTag(T), dmg(0.25, EA), exileAll(T)], { cost: 1, tags: BR }),
      O('주포 전탄 발사', [dmg(2.0, EA), perTag(T), dmg(0.7, EA), tough(1, EA)], { cost: 3, tags: BR }),   // 비용↑ · 재설계 — 전리품은 남김 · 격파
      O('예비 포탄', [dmg(1.0, EA), perTag(T), dmg(0.5, EA), srch()], { tags: BR }),                    // 서치
      O('방패째 박살', [{ k: 'strip', target: EA }, dmg(1.0, EA), perTag(T), dmg(0.4, EA)], { tags: BR }),
    ], [B('출력 최대', 'power'), B('조준경 보정', 'weakSpot'), B('냉각 완료', 'ap')], { tags: BR }),
    // u5 굴리기(스킬) — 노획물 장부: 쌓아 둔 몫을 적어 두고 한 입씩 나눈다
    card(H, 5, '노획물 장부', 1, '스킬', [stk(K, 2), heal(0.7), draw(1)], [
      O('신탁 1', [stk(K, 3), heal(0.9), draw(1)]),
      O('신탁 2', [stk(K, 1), heal(0.5), draw(1)], { cost: 0 }),
      O('신탁 3', [stk(K, 2), heal(0.7), srch(ATK, 2)]),                                            // 서치(BEST)
      O('신탁 4', [stk(K, 2), draw(1), ifStack(K, 3), heal(1.5)]),
      O('신탁 5', [discard(1), make(T, 2), heal(0.6)]),                                          // 대가 · 재설계 — 적지 않고 털어 온다
    ], [B('축복 1', { tags: ['보존'] }), B('축복 2', [heal(0.3)]), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 0.8);
  starter(j, '스패럿_u1');
}

// ════════════════════════════════════════════════════════════════════
// 16. 에피카 — 딜러 · 활발 · 엘다인(원작 방식 고학년 「연주 중」 유지). 「에피콘」이 대신 맞고 따라 치다가, 용감한 돌격으로 쏟는다
//     ④ 「만족스러운 연주」 는 원작(고학년 뒤 연주 상태)이 상시 효과라 강화 카드로 둔다.
// ════════════════════════════════════════════════════════════════════
function epica(j) {
  const H = '에피카', K = '에피콘';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '따라 치고 대신 맞는 분홍 극단원', carrier: 'self', cap: 3, guard: true, cut: 0.4 };
  // h.keywords(「연주 중」 — 고학년 원작 장치)는 그대로
  h.passives = [
    pas('에피콘 극단', 'play', [per(K), dmg(0.15, ER)], { when: { type: '공격' }, limit: 2 }),
    pas('일생일대의 버티기', 'lowHp', [st('끈기', 1), stk(K, 2)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // u1 저학년 「극적인 연출」: 주변 아군 공속↑ = 협공
    card(H, 1, '극적인 연출', 1, '스킬', [stk(K, 1), st('협공', 1)], [
      O('앙코르', [stk(K, 2), st('협공', 1)]),
      O('짧은 막간극', [stk(K, 1), st('협공', 1)], { cost: 0 }),
      O('대공연', [stk(K, 3), st('협공', 2), draw(1)], { cost: 2 }),                                 // 비용↑ — 드로우
      O('개막 인사', [stk(K, 3), sh(0.8), srch({ who: 'self', type: '공격' })], { tags: ['개전'] }),            // 재설계 · 서치
      O('관객 모두 기립', [stk(K, 1), st('협공', 1), draw(1, { who: 'other' })]),
    ], [B('박수갈채', 'draw'), B('대본 넘기기', 'ap'), B('엑스트라 한 명', [stk(K, 1)])]),
    // u2 터뜨리기 — 강평 「용감한 에피콘」: 에피콘 떼 돌격 광역 · 전부
    card(H, 2, '용감한 에피콘', 1, '공격', [dmg(0.6, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      O('돌격 대열', [dmg(0.75, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      O('에피콘 하나', [dmg(0.4, EA), per(K), dmg(0.13, EA), spendAll(K)], { cost: 0 }),
      O('클라이맥스', [dmg(1.1, EA), per(K), dmg(0.35, EA), tough(1, EA)], { cost: 2 }),              // 비용↑ · 재설계 — 격파 · 극단은 남김
      O('대열 보충', [dmg(0.75, EA), per(K), dmg(0.23, EA)]),
      O('에피콘 떼 습격', [per(K), dmg(0.3, EA), spendAll(K), srch()]),                               // 서치(BEST)
    ], [B('용기 한 스푼', 'power'), B('에피콘의 눈치', 'weakSpot'), B('무대 체질', 'ap')]),
    // u3 열기 — 어사이드 「에피칸」(똑같은 옷을 입힌 제자)
    card(H, 3, '에피칸', 1, '스킬', [stk(K, 1), draw(1), sh(0.8)], [
      O('에피칸 둘', [stk(K, 2), draw(1), sh(0.8)]),
      O('에피칸 신호', [stk(K, 1), draw(1), sh(0.4)], { cost: 0 }),
      O('상설 무대', [stk(K, 1), sh(1.0), power(rule('turnStart', [stk(K, 1)]))], { power: true }),             // 강화화 — 엘다인 한 단계
      O('즉흥 등장', [stk(K, 1), draw(1), inspire, stk(K, 2)]),
      O('분장실 숨기', [discard(1), stk(K, 3), sh(1.2)]),                                              // 대가 · 재설계
    ], [B('두꺼운 무대막', 'guard'), B('대기실 쪽지', 'draw'), B('조수 에피콘', [stk(K, 1)])]),
    // u4 완성형(강화 — 원작 상시) — 「만족스러운 연주」: 사기 · 공격할 때마다 에피콘
    card(H, 4, '만족스러운 연주', 1, '강화', [st('사기', 1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))], [
      O('앙코르 공연', [st('사기', 1), stk(K, 2), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))]),
      O('소극장', [st('사기', 1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))], { cost: 0 }),
      O('첫 공연', [stk(K, 3), draw(1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))], { tags: ['개전'] }),   // 재설계
      O('커튼콜', [st('사기', 1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }), rule('ult', [stk(K, 2)]))]),
      O('다음 악장', [st('사기', 1), srch(), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))]),
    ], [B('흥겨운 박자', 'atkUp'), B('객석의 에피콘', [stk(K, 1)]), B('켈틱 하프', 'draw')]),
    // u5 굴리기(공격) — 어사이드 「아군에게 바칩니다」: 에피콘을 불러 무대를 바친다
    card(H, 5, '바치는 무대', 1, '공격', [dmg(1.0), stk(K, 1)], [
      O('신탁 1', [dmg(1.25), stk(K, 1)]),
      O('신탁 2', [dmg(0.6), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [dmg(0.9), stk(K, 1), srch(SKL)]),                                           // 서치(BEST)
      O('신탁 4', [dmg(0.75, EA), stk(K, 1)]),                                            // 재설계 — 광역
      O('신탁 5', [dmg(0.6), per(K), dmg(0.3), spendAll(K)]),                                  // 대가 — 극단을 다 쓴다
    ], [B('축복 1', 'power'), B('축복 2', 'weakSpot'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleC(j, '에피카_u5', 0.85);
  starter(j, '에피카_u3');
}

// ════════════════════════════════════════════════════════════════════
// 17. 우로스 — 서포터(드로우) · 공명 · 엘다인. 「지배의 영역」(파티 공격력↑ · 적의 차례마다 좁아짐) — 넘치면 비수, 거대한 뱀으로 거둔다
//     3단계: 「세계수의 보물 파괴」 를 개전 강화 시동으로, 「마음이 비수가 되어」 를 영역을 세는 1코 마무리로.
// ════════════════════════════════════════════════════════════════════
function uros(j) {
  const H = '우로스', K = '지배의 영역';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '가장 강한 자의 힘을 나누는 영역', carrier: 'self', cap: 3, decay: 1, per: [{ stat: 'atk', v: 0.1, who: 'allies' }],
    rules: [
      { name: '영역 전개', when: { on: 'fightStart' }, fx: [stk(K, 1)] },
      { name: '내 마음의 등대', when: { on: 'stackOver', id: K }, limit: { per: 'turn', n: 1 }, fx: [hits(2, 0.3), st('취약', 1, ER)] },
    ],
  };
  delete h.keywords;
  h.passives = [
    pas('순환', 'shuffle', [stk(K, 2), draw(1)], { limit: 3, per: 'fight' }),
    pas('변치 않는 것', 'lowHp', [{ k: 'cleanse', v: 1 }, st('피해 감소', 2)], { when: { pct: 0.3 } }),
  ];
  setCards(j, [
    // u1 열기 — 저학년 「순환」(우로보로스): 버리고 뽑고 영역
    card(H, 1, '순환의 고리', 1, '스킬', [discard(2), draw(3), stk(K, 1)], [
      O('넓은 고리', [discard(2), draw(3), stk(K, 2)]),
      O('짧은 고리', [discard(1), draw(2), stk(K, 1)], { cost: 0 }),
      O('되감는 고리', [discard(2), draw(3), pull()]),                                               // 서치
      O('끝없는 순환', [discard(2), draw(3), power(rule('discard', [stk(K, 1), sh(0.3)], { limit: 2 }))], { power: true }),   // 강화화(BEST)
      O('허물 벗기', [discard(2), draw(3), ifStack(K, 2), st('협공', 1)]),                            // 재설계
    ], [B('한 바퀴 더', 'draw'), B('비워 낸 손', 'ap'), B('백사의 혀', [stk(K, 1)])]),
    // u2 완성형(1코 마무리) — 어사이드 「내 마음의 등대」: 비수 둘 · 영역 1개당 한 자루 더(쓰지 않음) · 취약(담당)
    card(H, 2, '마음이 비수가 되어', 1, '공격', [hits(2, 0.3), per(K), dmg(0.3, ER), st('취약', 1, EA)], [
      O('깊이 꽂힌 비수', [hits(2, 0.38), per(K), dmg(0.38, ER), st('취약', 1, EA)]),
      O('작은 비수', [hits(1, 0.3), per(K), dmg(0.3, ER), st('취약', 1, EA)], { cost: 0 }),
      O('걸어갈 길', [stk(K, 1), hits(4, 0.3), ifStack(K, 2), hits(2, 0.3)]),                                      // 재설계
      O('등대의 불빛', [hits(2, 0.3), st('취약', 1, EA), power(rule('turnEnd', [per(K), dmg(0.2, ER)]))], { power: true }),   // 강화화
      O('한 놈만 노리는 비수', [spendAll(K), hits(4, 0.4, E1), st('취약', 2, E1)]),                  // 대가 — 영역을 다 씀
    ], [B('벼린 마음', 'power'), B('상처를 아는 칼', 'frost'), B('되돌아온 비수', [stk(K, 1)])]),
    // u3 시동(개전 강화) — 세계수의 보물 파괴: 협공 · 매 턴 영역
    card(H, 3, '세계수의 보물 파괴', 1, '강화', [st('협공', 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('보물 상자 박살', [st('협공', 1), power(rule('turnStart', [stk(K, 2)]))], { tags: ['개전'] }),
      O('작은 똬리', [st('협공', 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('허물 벗는 뱀', [stk(K, 2), srch(SU, 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),   // 재설계 · 서치
      O('세계수의 숨결', [st('협공', 1), draw(1), power(rule('turnStart', [stk(K, 1)]), rule('shuffle', [draw(1)]))], { tags: ['개전'] }),
      O('또 한 바퀴', [st('협공', 1), stk(K, 2), power(rule('turnStart', [stk(K, 1)]))]),             // 대가 — 개전을 뗌
    ], [B('파괴의 손맛', 'atkUp'), B('보물 조각', 'draw'), B('단숨에 부수기', [stk(K, 1)])], { tags: ['개전'] }),
    // u4 터뜨리기 — 강평 「거대한 뱀」: 영역을 거둬 광역(2코 하나)
    card(H, 4, '거대한 뱀', 2, '공격', [dmg(1.0, EA), per(K), dmg(0.3, EA), spendAll(K)], [
      O('몸을 감는 뱀', [dmg(1.2, EA), per(K), dmg(0.35, EA), spendAll(K)]),
      O('새끼 뱀', [dmg(0.65, EA), per(K), dmg(0.2, EA), spendAll(K)], { cost: 1 }),
      O('세계를 휘감는 뱀', [dmg(1.5, EA), per(K), dmg(0.45, EA), st('취약', 2, EA)], { cost: 3 }),   // 비용↑ · 재설계 — 영역은 남김
      O('꼬리 무는 뱀', [dmg(1.0, EA), per(K), dmg(0.3, EA), srch()]),                                // 서치 · 재설계
      O('잠든 뱀', [dmg(1.1, EA), per(K), dmg(0.33, EA)]),
    ], [B('거대한 독니', 'power'), B('비늘 긁힘', 'weakSpot'), B('압도', 'ap')]),
    // u5 유틸(0코 거르기) — 똬리 틀기: 한 장 뽑고 한 장 버린다(순환)
    card(H, 5, '똬리 틀기', 0, '스킬', [stk(K, 1), draw(1), discard(1)], [
      O('신탁 1', [stk(K, 2), draw(1), discard(1)]),
      O('신탁 2', [stk(K, 1), draw(2), discard(1)]),
      O('신탁 3', [stk(K, 1), srch(SU, 2), discard(1)]),                                        // 서치(BEST)
      O('신탁 4', [discard(2), stk(K, 3), sh(0.6)]),                                             // 대가 · 재설계
      O('신탁 5', [stk(K, 1), draw(1), ifStack(K, 3), draw(1)]),
    ], [B('축복 1', 'draw'), B('축복 2', { tags: ['보존'] }), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '우로스_u3');
}

// ════════════════════════════════════════════════════════════════════
// 18. 유미미 — 딜러 · 광기 · 1성(연계: 동료의 스킬은 「나른함」을 채우고, 동료의 공격(소란)은 깬다).
//     늘어져 겨누는 나른함 — 다음 공격 한 장에 몽땅, 약한 놈은 마무리(④ 1코).
// ════════════════════════════════════════════════════════════════════
function yumimi(j) {
  const H = '유미미', K = '나른함';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '늘어진 채 겨누는 한 발', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.3 }], consumeAll: true,
    rules: [
      { name: '한적한 숲', when: { on: 'fightStart' }, fx: [stk(K, 2)] },
      { name: '한적함', when: { on: 'play', type: '스킬', who: 'other' }, limit: { per: 'turn', n: 1 }, fx: [stk(K, 1)] },   // 채우기 — 동료가 대신 일해 줌
    ] };
  delete h.keywords;
  h.passives = [
    pas('늘어지기', 'turnEnd', [stk(K, 1)]),
    pas('약하면 설치지 마', 'kill', [ap(1), draw(1)], { limit: 1 }),
  ];
  setCards(j, [
    // u1 굴리기 — 저학년 「발싸! 아뵤~」: 가장 먼(HP 가장 높은) 적에게 강화 화살
    card(H, 1, '발싸! 아뵤~', 1, '공격', [dmg(1.3, TOP), ifStack(K, 3), tough(1, TOP)], [
      O('힘껏 발싸!', [dmg(1.6, TOP), ifStack(K, 3), tough(1, TOP)]),
      O('대충 발싸', [dmg(0.85, TOP), ifStack(K, 3), tough(1, TOP)], { cost: 0 }),
      O('천벌 예행연습', [dmg(2.6, TOP), ifStack(K, 3), tough(2, TOP), draw(1)], { cost: 2 }),         // 비용↑ — 드로우
      O('화살 줍기', [dmg(1.2, TOP), srch({ who: 'self', type: '스킬' })]),                            // 재설계 · 서치 — 늘어질 카드
      O('늘어지게 조준', [dmg(1.3, TOP), ifStack(K, 2), dmg(0.6, TOP)]),
    ], [B('팽팽한 시위', 'power'), B('급소 관찰', 'weakSpot'), B('하품 한 번', 'draw')]),
    // u2 굴리기 — 뿔갈이 화살촉: 쏘고 다시 늘어진다
    card(H, 2, '유령도 꿰뚫는 화살', 1, '공격', [dmg(1.0), stk(K, 1)], [
      O('유령 관통', [dmg(1.25), stk(K, 1)]),
      O('툭 쏘기', [dmg(0.6), stk(K, 1)], { cost: 0 }),
      O('뿔갈이 습관', [dmg(0.85), power(rule('turnStart', [stk(K, 1)]))], { power: true }),             // 강화화(BEST)
      O('잠 깨는 한 발', [discard(1), dmg(1.2), draw(1)]),                                             // 대가 · 재설계
      O('하품 섞인 한 발', [dmg(1.0), stk(K, 1), inspire, stk(K, 1)]),
    ], [B('날 선 화살촉', 'power'), B('으스스한 화살', 'weakSpot'), B('애벌레 간식', [stk(K, 1)])]),
    // u3 열기 — 수풀에 늘어지기(보존)
    card(H, 3, '수풀에 늘어지기', 1, '스킬', [stk(K, 2), sh(1.0)], [
      O('푹 늘어지기', [stk(K, 2), sh(1.4)], { tags: ['보존'] }),
      O('잠깐 눕기', [stk(K, 1), sh(0.6)], { cost: 0, tags: ['보존'] }),
      O('뒹굴며 줍기', [stk(K, 2), sh(0.8), srch({ who: 'self', type: '공격' })], { tags: ['보존'] }),   // 서치
      O('본격 낮잠', [stk(K, 2), sh(0.6), power(rule('turnEnd', [sh(0.6)]))], { power: true, tags: ['보존'] }),   // 강화화
      O('수풀 위장', [stk(K, 3), draw(1)]),                                                             // 대가 · 재설계 — 보존을 뗌
    ], [B('푹신한 풀밭', 'guard'), B('꿈결에 한 장', 'draw'), B('느긋한 마음', [stk(K, 1)])], { tags: ['보존'] }),
    // u4 완성형(1코 마무리) — 약하면 설치지 마: 부상인 적에게 한 번 더 · 처치하면 AP
    card(H, 4, '약하면 설치지 마', 1, '공격', [dmg(1.2), ifWounded_(E1), dmg(1.0), ifKill, ap(1)], [
      O('건방진 놈 혼내기', [dmg(1.45), ifWounded_(E1), dmg(1.2), ifKill, ap(1)]),
      O('끝장내기', [dmg(2.2), ifWounded_(E1), dmg(1.6), ifKill, draw(2)], { cost: 2 }),                 // 비용↑ — 드로우 둘
      O('제일 약한 놈 골라', [dmg(1.2), ifWounded_(E1), dmg(1.0), ifKill, ap(1)], { tags: ['약점 공격'] }),
      O('나른한 일격', [dmg(1.2), ifStack(K, 3), dmg(0.9), ifKill, ap(1)]),                             // 재설계
      O('다음 화살', [srch({ who: 'self', type: '공격' }), dmg(1.2), ifWounded_(E1), dmg(1.0)]),         // 서치
    ], [B('깔보는 눈빛', 'power'), B('약한 곳 찌르기', 'weakSpot'), B('잘난 척', 'ap')]),
    // u5 유틸(0코 비용) — 귀찮아서 대충: 늘어지면서 다음 화살을 싸게
    card(H, 5, '귀찮아서 대충', 0, '스킬', [stk(K, 1), cheap(ATK)], [
      O('신탁 1', [stk(K, 2), cheap(ATK)]),
      O('신탁 2', [stk(K, 1), cheap(ATK), sh(0.8)]),
      O('신탁 3', [stk(K, 1), srch(ATK), cheap(ATK)]),                                         // 서치(BEST)
      O('신탁 4', [cheap(ATK), power(rule('play', [stk(K, 1)], { when: { type: '스킬' }, limit: 1 }))], { power: true }),   // 강화화
      O('신탁 5', [discard(1), cheap(ATK), draw(2)]),                                     // 대가 — 나른함 대신 손
    ], [B('축복 1', { tags: ['보존'] }), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '유미미_u3');
}
function ifWounded_(t) { return { k: 'ifWounded', target: t }; }

// ════════════════════════════════════════════════════════════════════
// 19. 쵸피 — 딜러 · 우울 · 1성. 하루도 거르지 않는 「수련」 — 넷이면 판 내내 강해지고(성장), 그 전에 도끼로 쏟을지
//     3단계: 기본 카드를 생성 카드 「쪼갠 장작」으로 패고(기본 카드 연료) · ④ 「티그 님처럼」 을 수련을 세는 1코 마무리로.
// ════════════════════════════════════════════════════════════════════
function chopi(j) {
  const H = '쵸피', K = '수련', LG = '쵸피_log';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '하루도 거르지 않는 장작 패기', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [{ name: '수련 완성', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), { k: 'growRun', id: 'atk', v: 3 }, dmg(1.2, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('하루도 거르지 않는 수련', 'turnStart', [stk(K, 1)]),
    pas('강한 사람은 모두 스승', 'break', [stk(K, 2), { k: 'growRun', id: 'atk', v: 1 }], { when: { mine: true }, limit: 2, per: 'fight' }),
  ];
  token(j, { id: LG, name: '쪼갠 장작', cost: 0, type: '스킬', tags: ['소멸'], fx: [stk(K, 1), draw(1)], blurb: '오늘 몫의 장작입니다. 내일 몫도 팰 겁니다.' });
  const WK = ['약점 공격'];
  setCards(j, [
    // u1 굴리기 — 저학년 「퍄오오~」: 1성 최고 계수 고함 범위
    card(H, 1, '퍄오오~', 1, '공격', [dmg(0.85, EA), stk(K, 1)], [
      O('퍄아아아~!', [dmg(1.05, EA), stk(K, 1)]),
      O('작은 기합', [dmg(0.55, EA), stk(K, 1)], { cost: 0 }),
      O('온몸 기합', [dmg(1.6, EA), stk(K, 2), tough(1, EA)], { cost: 2 }),                            // 비용↑ — 격파
      O('매일 기합', [dmg(0.6, EA), power(rule('turnStart', [make(LG, 1)]))], { power: true }),          // 강화화(BEST)
      O('장작 기합', [dmg(0.7, EA), make(LG, 1), draw(1)]),                                            // 재설계
    ], [B('힘찬 도끼', 'power'), B('쩍 갈라진 틈', 'weakSpot'), B('기합 충전', [stk(K, 1)])]),
    // u2 터뜨리기 — 결대로 쪼개기: 수련 1개당 한 번 더 · 전부(2코 하나)
    card(H, 2, '결대로 쪼개기', 2, '공격', [dmg(1.6), per(K), dmg(0.2), spendAll(K)], [
      O('통나무 두 동강', [dmg(1.9), per(K), dmg(0.25), spendAll(K)], { tags: WK }),
      O('잔가지 쪼개기', [dmg(1.0), per(K), dmg(0.14), spendAll(K)], { cost: 1, tags: WK }),
      O('결 따라 수련', [dmg(1.7), per(K), dmg(0.22), stk(K, 1)], { tags: WK }),                       // 재설계 — 쓰지 않고 쌓음
      O('단숨에 장작더미', [dmg(2.4), per(K), dmg(0.3), make(LG, 2)], { cost: 3, tags: WK }),           // 비용↑ — 장작 둘
      O('나무꾼의 감', [dmg(1.6), per(K), dmg(0.2), srch()], { tags: WK }),                              // 서치
    ], [B('도끼날 갈기', 'power'), B('나뭇결 읽기', 'weakSpot'), B('쪼갠 장작 줍기', 'draw')], { tags: WK }),
    // u3 완성형(1코 마무리) — 티그 님처럼: 수련 1개당 한 번 더(쓰지 않음)
    card(H, 3, '티그 님처럼', 1, '공격', [dmg(1.0), per(K), dmg(0.18)], [
      O('백 번 휘두르기', [dmg(1.25), per(K), dmg(0.22)]),
      O('사부님 일과', [dmg(0.9), stk(K, 1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))], { power: true }),   // 강화화(BEST) — 옛 엔진 자리
      O('사부님 흉내', [perTag(LG), dmg(0.55), stk(K, 2)]),                                              // 재설계 — 손의 장작을 센다
      O('목검 수련', [discard(1), dmg(0.8), per(K), dmg(0.2)]),                                         // 대가
      O('아침 수련', [dmg(0.65), per(K), dmg(0.12)], { cost: 0 }),
    ], [B('티그 님 흉내', 'power'), B('따라 하기', 'draw'), B('장작 한 짐', [make(LG, 1)])]),
    // u4 열기 — 심부름길 호신술: 막고 수련 · 시작 카드 1장을 장작으로(기본 카드 연료)
    card(H, 4, '심부름길 호신술', 1, '스킬', [sh(0.9), stk(K, 1), tfFrom(LG, '쵸피_s1')], [
      O('단단한 호신술', [sh(1.2), stk(K, 1), tfFrom(LG, '쵸피_s1')]),
      O('몸 피하기', [sh(0.5), stk(K, 1), tfFrom(LG, '쵸피_s1')], { cost: 0 }),
      O('수련 겸 심부름', [stk(K, 1), ifStack(K, 3), st('반격', 2)]),                                   // 재설계
      O('심부름 쪽지', [sh(0.9), stk(K, 2), drawBasic()]),                              // 서치(시작 공격 카드)
      O('장바구니 막기', [discard(1), sh(1.4), stk(K, 3)]),                                              // 대가
    ], [B('단단한 자세', 'guard'), B('지는 건 싫어', 'defUp'), B('한 걸음 수련', [stk(K, 1)])]),
    // u5 굴리기(스킬) — 장작 지게: 장작을 지고 나르며 수련 · 막기
    card(H, 5, '장작 지게', 1, '스킬', [make(LG, 1), stk(K, 1), sh(0.8)], [
      O('신탁 1', [make(LG, 1), stk(K, 1), sh(1.1)]),
      O('신탁 2', [make(LG, 1), sh(0.5)], { cost: 0 }),
      O('신탁 3', [stk(K, 2), sh(0.8), srch(ATK, 2)]),                                            // 서치(BEST)
      O('신탁 4', [perTag(LG), sh(1.0), exileAll(LG), stk(K, 2)]),                             // 대가 · 재설계 — 손의 장작을 태운다
      O('신탁 5', [make(LG, 1), stk(K, 2), ifStack(K, 3), heal(1.0)]),
    ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 1.35);
  starter(j, '쵸피_u4');
}

// ════════════════════════════════════════════════════════════════════
// 20. 코미 — 탱커 · 우울(변신 「거대 코미」 유지). 틈만 나면 「낮잠」 — 든 낮잠은 턴 끝 회복 · 적의 주먹 앞 실드
//     3단계: 「엘프산 사료 한 그릇」 을 개전 강화 시동으로, 「일하기 싫으면 안 해도 돼」 를 낮잠을 세는 1코 마무리(시작 카드 1장을 안 함 = 소멸)로.
// ════════════════════════════════════════════════════════════════════
function komi(j) {
  const H = '코미', K = '낮잠';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '틈만 나면 자 두는 낮잠', carrier: 'self', cap: 3, per: [{ stat: 'hot', ratio: 0.05 }] };
  delete h.keywords;
  h.passives = [
    pas('쿨쿨', 'turnEnd', [stk(K, 1)], { conds: [{ c: 'apLeft', n: 2 }] }),
    pas('낮잠의 미학', 'foeActBefore', [spend(K, 1), sh(0.3)], { when: { type: '공격' }, conds: [{ c: 'stack', id: K, n: 1 }], limit: 1 }),
  ];
  setCards(j, [
    // u1 터뜨리기 — 저학년 「푹신푹신 타임」: 낮잠 1개당 실드 · 불굴(담당 세기 버프)
    card(H, 1, '푹신푹신 타임', 1, '스킬', [per(K), sh(0.5), st('불굴', 1), spendAll(K)], [
      O('푹신푹신 이불', [per(K), sh(0.7), st('불굴', 1), spendAll(K)]),
      O('쪽잠 타임', [per(K), sh(0.35), st('불굴', 1), spendAll(K)], { cost: 0 }),
      O('겨울잠', [per(K), sh(0.8), st('불굴', 2), heal(1.0)], { cost: 2 }),                            // 비용↑ · 재설계 — 회복 · 낮잠은 남김
      O('이불 속 뒹굴기', [sh(1.4), st('불굴', 1), stk(K, 1)]),
      O('개운한 기상', [st('불굴', 1), ifStack(K, 3), per(K), sh(0.75), spendAll(K)]),
    ], [B('두툼한 이불', 'guard'), B('뒹굴 자리', 'draw'), B('포근한 털', [st('면역', 1)])]),
    // u2 굴리기 — 강평 베개(확률 기절) · 거대 코미 변신 중 ×1.5
    card(H, 2, '베개 강타', 1, '공격', [ddef(0.7), stk(K, 1), ifRand(0.2), st('기절', 1, E1)], [
      O('솜 꽉 찬 베개', [ddef(0.9), stk(K, 1), ifRand(0.2), st('기절', 1, E1)]),
      O('베개 톡', [ddef(0.45), stk(K, 1), ifRand(0.2), st('기절', 1, E1)], { cost: 0 }),
      O('베개 싸움', [discard(1), ddef(0.8, EA), stk(K, 1)]),                                           // 대가 · 재설계
      O('졸린 베개질', [ddef(0.7), stk(K, 1), ifStack(K, 3), st('기절', 1, E1)]),
      O('베개 베고 한숨', [ddef(0.8), stk(K, 2), srch({ who: 'self', type: '스킬' })]),                  // 서치
    ], [B('묵직한 베개', 'power'), B('솜뭉치 범벅', 'frost'), B('하품', [stk(K, 1)])]),
    // u3 완성형(1코 마무리 · 보존) — 일하기 싫으면 안 해도 돼: 낮잠 1개당 실드(쓰지 않음) · 손의 시작 카드 1장을 안 함(소멸)
    card(H, 3, '일하기 싫으면 안 해도 돼', 1, '스킬', [per(K), sh(0.3), exileBasic], [
      O('오늘은 쉬는 날', [per(K), sh(0.4), exileBasic], { tags: ['보존'] }),
      O('잠깐 쉬기', [per(K), sh(0.2), exileBasic], { cost: 0, tags: ['보존'] }),
      O('다들 쉬어도 돼', [sh(0.9), stk(K, 2), draw(2, { who: 'other' })], { tags: ['보존'] }),            // 재설계 · 서치
      O('낮잠 시간표', [st('저장', 1), power(rule('turnStart', [per(K), sh(0.12)]))], { power: true, tags: ['보존'] }),   // 강화화(BEST)
      O('오늘 일 끝', [per(K), sh(0.45), spendAll(K), st('저장', 1)]),                                  // 대가 — 보존을 떼고 다 씀
    ], [B('폭신한 쿠션', 'guard'), B('느긋한 손길', 'draw'), B('종이 상자', [stk(K, 1)])], { tags: ['보존'] }),
    // u4 시동(개전 강화) — 엘프산 사료 한 그릇: 매 턴 낮잠
    card(H, 4, '엘프산 사료 한 그릇', 1, '강화', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('두 그릇째', [stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('사료 한 알', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], { cost: 0, tags: ['개전'] }),
      O('확 돌아가는 머리', [draw(2, { basic: true }), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),   // 재설계
      O('배부른 오후', [stk(K, 1), sh(0.6), power(rule('turnStart', [stk(K, 1)]))], { tags: ['개전'] }),
      O('포근한 방석', [stk(K, 2), sh(0.8), power(rule('turnStart', [stk(K, 1)]))]),                    // 대가 — 개전을 뗌
    ], [B('배부른 코미', 'defUp'), B('그릇 핥기', 'draw'), B('식후 낮잠', [stk(K, 1)])], { tags: ['개전'] }),
    // u5 굴리기(공격) — 잠꼬대 펀치: 자면서 휘두르고 낮잠 · 막기
    card(H, 5, '잠꼬대 펀치', 1, '공격', [ddef(0.8), stk(K, 1), sh(0.5)], [
      O('신탁 1', [ddef(1.0), stk(K, 1), sh(0.6)]),
      O('신탁 2', [ddef(0.5), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [ddef(0.8), stk(K, 1), srch(SKL)]),                                            // 서치(BEST)
      O('신탁 4', [ddef(0.75, EA), stk(K, 1)]),                                               // 재설계 — 광역
      O('신탁 5', [ddef(0.6), per(K), ddef(0.4), spendAll(K)]),                                // 대가 — 낮잠을 다 쓴다
    ], [B('축복 1', 'power'), B('축복 2', 'weakSpot'), B('축복 3', [stk(K, 1)])]),
  ]);
  scaleU(j, 0.75);
  starter(j, '코미_u4');
  // 애착 장비(희귀 — 싸움당 1코 카드 1~1.5장): 범용 몫은 위기 한 번 회복 · 실드, 애착 몫은 낮잠 둘로 시작
  for (const e of j.equips || []) {
    e.effect = [pas('기절하듯 낮잠', 'lowHp', [st('초재생', 2), sh(1.0)], { when: { pct: 0.3 } })];
    e.affinityEffect = [pas('베개 끌어안기', 'fightStart', [stk(K, 2)])];
  }
}

// ════════════════════════════════════════════════════════════════════
// 21. 코미(수영복) — 서포터(회복) · 냉정. 매 턴 들어오는 「주스 재고」 — 스킬마다 한 잔씩 팔지(회복), 특제 주스로 몰아 낼지
//     3단계: 생성 카드 「코미 주스」(재고가 넘치면 병에 담음) · ④ 「조기 매진!」 은 원작 어사이드가 상시 효과라 강화 카드로 둔다.
// ════════════════════════════════════════════════════════════════════
function komiSwim(j) {
  const H = '코미_수영복', K = '주스 재고', JU = '코미_수영복_juice';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '수상한 재료로 만든 코미 주스', carrier: 'self', cap: 4,
    rules: [{ name: '입고', when: { on: 'fightStart' }, fx: [stk(K, 2)] }, { name: '입고', when: { on: 'turnStart' }, fx: [stk(K, 1)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('조기 매진', 'play', [spend(K, 1), heal(0.7)], { when: { type: '스킬' }, conds: [{ c: 'stack', id: K, n: 1 }], limit: 2 }),
    pas('장사 수완', 'stackOver', [make(JU, 1)], { when: { id: K }, limit: 1 }),
  ];
  token(j, { id: JU, name: '코미 주스', cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.4), draw(1)], blurb: '맛은 묻지 마세요. 효과는 확실합니다.' });
  setCards(j, [
    // u1 굴리기 — 저학년 「물놀이 좋은거 아니야?」: 회복 · 스킬 피해량↑(사기 — 담당 세기 버프)
    card(H, 1, '물놀이 좋은거 아니야?', 1, '스킬', [heal(1.0), st('사기', 1)], [
      O('풀장 개장', [heal(1.2), st('사기', 1)]),
      O('튜브 띄우기', [heal(0.5), st('사기', 1)], { cost: 0 }),
      O('워터파크 통째로', [heal(1.8), st('사기', 2), make(JU, 2)], { cost: 2 }),                       // 비용↑ — 주스 둘
      O('물보라 튀기기', [heal(0.9), st('사기', 1), ifStack(K, 3), heal(0.8)]),
      O('매점 대목', [stk(K, 2), make(JU, 1), srch({ who: 'self', type: '스킬' })]),                    // 재설계 · 서치
    ], [B('시원한 물', 'heal'), B('입장권 할인', 'cost'), B('튼튼한 튜브', [sh(0.5)])]),
    // u2 터뜨리기 — 어사이드 「걸작 탄생」 특제 코미 주스: 재고 1개당 회복 · 받는 피해↓
    card(H, 2, '특제 코미 주스', 1, '스킬', [per(K), heal(0.45), spendAll(K), st('피해 감소', 2)], [
      O('진한 특제 주스', [per(K), heal(0.6), spendAll(K), st('피해 감소', 2)]),
      O('맛보기 주스', [per(K), heal(0.3), spendAll(K), st('피해 감소', 1)], { cost: 0 }),
      O('원액 그대로', [heal(1.4), st('피해 감소', 3)]),                                                // 재설계 — 재고를 쓰지 않음
      O('주스 가판대', [per(K), heal(0.3), power(rule('turnStart', [make(JU, 1)]))], { power: true }),   // 강화화(BEST)
      O('에너지 주스', [per(K), heal(0.45), spendAll(K), draw(2)]),
    ], [B('얼음 동동', 'heal'), B('빨대 꽂기', 'draw'), B('단골 손님', [stk(K, 1)])]),
    // u3 열기 — 평타 주스 판매: 적 피해 + 아군 회복
    card(H, 3, '주스 한 병 투척', 1, '공격', [dmg(0.9), heal(0.6), stk(K, 1)], [
      O('꽉 찬 병 투척', [dmg(1.1), heal(0.7), stk(K, 1)]),
      O('빨대 튕기기', [dmg(0.55), heal(0.4), stk(K, 1)], { cost: 0 }),
      O('한 박스 투척', [hits(4, 0.5), heal(1.0), make(JU, 2)], { cost: 2 }),                           // 비용↑ · 재설계 — 주스 둘
      O('주스 폭탄', [discard(1), dmg(0.75, EA), heal(0.6)]),                                          // 대가 · 재설계
      O('투척 판매', [dmg(1.0), heal(0.6), srch({ who: 'self', type: '스킬' })]),                       // 서치
    ], [B('얼음 넣은 병', 'power'), B('끈적한 과즙', 'frost'), B('다음 병', [stk(K, 1)])]),
    // u4 완성형(강화 — 원작 상시) — 어사이드 「조기 매진! 코사장 성공 신화」: 스킬마다 작은 회복
    card(H, 4, '조기 매진!', 1, '강화', [stk(K, 2), power(rule('play', [heal(0.3)], { when: { type: '스킬' }, limit: 1 }))], [
      O('완판 행진', [stk(K, 2), power(rule('play', [heal(0.45)], { when: { type: '스킬' }, limit: 1 }))]),
      O('한정 수량', [stk(K, 2), power(rule('play', [heal(0.3)], { when: { type: '스킬' }, limit: 1 }))], { cost: 0 }),
      O('해변 대목', [make(JU, 2), power(rule('play', [heal(0.3)], { when: { type: '스킬' }, limit: 1 }))], { tags: ['개전'] }),   // 재설계
      O('코사장 성공 신화', [stk(K, 2), srch(), power(rule('play', [heal(0.3)], { when: { type: '스킬' }, limit: 1 }))]),
      O('단골 확보', [st('사기', 1), power(rule('play', [heal(0.3)], { when: { type: '스킬' }, limit: 1 }))]),
    ], [B('주문서 뭉치', 'draw'), B('차양 모자', 'defUp'), B('첫 손님', [stk(K, 1)])]),
    // u5 굴리기(공격) — 물총 세례: 뿌리면서 재고 · 손님 회복
    card(H, 5, '물총 세례', 1, '공격', [hits(3, 0.35), stk(K, 1), heal(0.4)], [
      O('신탁 1', [hits(3, 0.45), stk(K, 1), heal(0.5)]),
      O('신탁 2', [hits(2, 0.3), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [hits(3, 0.35), heal(0.4), srch(SKL)]),                                        // 서치(BEST)
      O('신탁 4', [hits(3, 0.35), stk(K, 1), power(rule('play', [stk(K, 1)], { when: { type: '공격' }, limit: 1 }))], { power: true }),   // 강화화
      O('신탁 5', [per(K), hits(2, 0.3), spendAll(K), heal(0.6)]),                              // 대가 — 재고를 다 쓴다
    ], [B('축복 1', 'frost'), B('축복 2', 'weakSpot'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '코미_수영복_u3');
}

// ════════════════════════════════════════════════════════════════════
// 22. 티그 — 딜러 · 활발. 이번 턴 휘두를수록 「장작 패기」 — 셋이면 내려치고 휘두르기(광역 · AP)
//     3단계: ④ 「백호 비전서」 를 장작 패기를 세는 1코 마무리로(시작 공격 카드를 다시 꺼내 이어 감 — 기본 카드 연료).
// ════════════════════════════════════════════════════════════════════
function tig(j) {
  const H = '티그', K = '장작 패기';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '이번 턴 휘두를수록 붙는 손맛', carrier: 'self', cap: 3, endClear: true, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [{ name: '내려치고 휘두르기', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), dmg(0.8, EA), ap(1)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('장작 패기 반복', 'play', [stk(K, 1)], { when: { type: '공격' } }),
    pas('허접 처리', 'kill', [stk(K, 1)], { limit: 1 }),
  ];
  const SP = ['신속'];
  setCards(j, [
    // u1 굴리기 — 저학년 「소닉 블레이드」: 전진 범위(신속)
    card(H, 1, '소닉 블레이드', 1, '공격', [dmg(0.8, EA), ifStack(K, 2), dmg(0.4, EA)], [
      O('음속 돌파', [dmg(0.95, EA), ifStack(K, 2), dmg(0.45, EA)], { tags: SP }),
      O('짧은 칼바람', [dmg(0.5, EA), ifStack(K, 2), dmg(0.25, EA)], { cost: 0, tags: SP }),
      O('바람 가르는 칼', [dmg(1.6, EA), ifStack(K, 2), dmg(0.8, EA), tough(1, EA)], { cost: 2, tags: SP }),   // 비용↑ — 격파
      O('장작 쌓아 두기', [dmg(0.8, EA), stk(K, 2), drawBasic()]),                                   // 대가 · 재설계 — 신속을 떼고 시작 공격 카드
      O('백호의 바람', [dmg(0.9, EA), power(reach(K, 3, [dmg(0.6, EA)]))], { power: true, tags: SP }),   // 강화화
    ], [B('백호의 눈', 'power'), B('몸이 먼저', 'draw'), B('틈새 가르기', 'weakSpot')], { tags: SP }),
    // u2 열기 — 평타 쌍검 2연타
    card(H, 2, '쌍검 휘두르기', 1, '공격', [hits(2, 0.55, E1), stk(K, 1)], [
      O('쌍검 난무', [hits(2, 0.68, E1), stk(K, 1)]),
      O('한 손 베기', [hits(2, 0.35, E1), stk(K, 1)], { cost: 0 }),
      O('쌍검 폭풍', [hits(5, 0.5, E1), stk(K, 2), drawBasic()], { cost: 2 }),          // 비용↑ — 시작 공격 카드
      O('장작 쌓기', [hits(2, 0.55, E1), stk(K, 1), srch({ who: 'self', type: '공격' })]),              // 서치
      O('쌍검 회전', [discard(1), dmg(0.45, EA, { hits: 2 })]),                                        // 대가 · 재설계
    ], [B('손목 스냅', 'power'), B('엇갈린 상처', 'weakSpot'), B('검집 돌리기', [stk(K, 1)])]),
    // u3 터뜨리기 — 장작 패기를 다 쏟는 내려치기
    card(H, 3, '통나무 두 동강', 1, '공격', [dmg(0.9), per(K), dmg(0.35), spendAll(K)], [
      O('통나무 패기', [dmg(1.1), per(K), dmg(0.42), spendAll(K)]),
      O('손도끼 패기', [dmg(0.55), per(K), dmg(0.22), spendAll(K)], { cost: 0 }),
      O('통째로 두 동강', [dmg(1.6), per(K), dmg(0.6), tough(1)], { cost: 2 }),                         // 비용↑ · 재설계 — 격파
      O('장작 산더미', [dmg(1.05), per(K), dmg(0.42)]),
      O('장작더미 무너뜨리기', [dmg(0.5), per(K), dmg(0.4), srch()]),                                // 서치
    ], [B('도끼 같은 쌍검', 'power'), B('갈라진 틈', 'weakSpot'), B('팔 걷어붙이기', 'ap')]),
    // u4 완성형(1코 마무리) — 백호 비전서: 장작 패기 1개당 한 번 더(쓰지 않음) · 시작 공격 카드 1장 드로우
    card(H, 4, '백호 비전서', 1, '공격', [dmg(0.6), per(K), dmg(0.3), drawBasic()], [
      O('백호의 일격', [dmg(0.75), per(K), dmg(0.36), drawBasic()]),
      O('비전서 첫 장', [dmg(0.8), { k: 'atkMod', v: 0.1, run: true, target: 'self' }, power(rule('turnStart', [stk(K, 1)]))], { power: true }),   // 강화화(BEST) — 옛 엔진 자리
      O('비전서 속독', [draw(2, { basic: true }), stk(K, 1), ap(1)]),                                         // 재설계
      O('도끼 손질', [dmg(0.6), per(K), dmg(0.4), spendAll(K)]),                                        // 대가
      O('백호의 기백', [dmg(0.4), per(K), dmg(0.2), drawBasic()], { cost: 0 }),
    ], [B('다음 장 넘기기', 'draw'), B('참스승 디아나', 'atkUp'), B('차기 촌장', [stk(K, 1)])]),
    // u5 유틸(0코 서치) — 백호 자세: 손맛을 붙이고 공격 카드를 찾는다
    card(H, 5, '백호 자세', 0, '스킬', [stk(K, 1), srch(ATK)], [
      O('신탁 1', [stk(K, 2), srch(ATK)]),
      O('신탁 2', [stk(K, 1), srch(ATK, 2)]),                                                   // 서치 둘
      O('신탁 3', [stk(K, 1), srch(ATK), cheap(ATK)]),                                                      // 서치 · 재설계(BEST) — 찾아서 싸게
      O('신탁 4', [discard(1), stk(K, 2), draw(1)]),                                             // 대가
      O('신탁 5', [srch(ATK), inspire, stk(K, 2)]),
    ], [B('축복 1', { tags: ['보존'] }), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '티그_u2');
}

// ════════════════════════════════════════════════════════════════════
// 23. 티그(영웅) — 딜러 · 광기 · 엘다인(원작 방식 고학년 유지). 「영웅심」 셋이면 험난한 영웅의 길(광역 · 회복)
//     3단계: ④ 「영웅의 길」 을 영웅심을 세는 1코 마무리로(옛 매 턴 엔진은 D 갈래로).
// ════════════════════════════════════════════════════════════════════
function tigHero(j) {
  const H = '티그_영웅', K = '영웅심';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '영웅의 길에서 솟는 용기', carrier: 'self', cap: 3,
    rules: [{ name: '험난한 영웅의 길', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), dmg(0.4, EA), heal(0.3)] }],
  };
  delete h.keywords;
  h.passives = [
    pas('무용담', 'break', [stk(K, 1)], { when: { mine: true } }),
    pas('그 스승에 그 제자', 'ult', [stk(K, 1)]),
  ];
  const WK = ['약점 공격'];
  setCards(j, [
    // u1 굴리기 — 저학년 「사슴류 베기」: 돌진 · 복귀 두 번 광역
    card(H, 1, '사슴류 베기', 1, '공격', [dmg(0.4, EA, { hits: 2 }), stk(K, 1)], [
      O('뿔 꺾기 베기', [dmg(0.55, EA, { hits: 2 }), stk(K, 1)]),
      O('한 칼만', [dmg(0.3, EA, { hits: 2 }), stk(K, 1)], { cost: 0 }),
      O('삼단 베기', [dmg(0.55, EA, { hits: 3 }), stk(K, 2), tough(1, EA)], { cost: 2 }),                // 비용↑ — 격파
      O('무용담 베기', [dmg(0.45, EA, { hits: 2 }), stk(K, 1), ifStack(K, 2), heal(1.0)]),
      O('사슴 뿔 휘돌리기', [discard(1), hits(2, 0.9, E1), stk(K, 1)]),                                // 대가
    ], [B('날 선 롱소드', 'power'), B('사냥꾼의 눈', 'weakSpot'), B('발걸음 고르기', 'draw')]),
    // u2 열기 — 중열 「훈련의 성과」
    card(H, 2, '훈련의 성과', 1, '공격', [dmg(0.8), stk(K, 1), tough(1)], [
      O('매일의 성과', [dmg(1.0), stk(K, 1), tough(1)]),
      O('기초 훈련', [dmg(0.55), stk(K, 1), tough(1)], { cost: 0 }),
      O('필살기 연습', [dmg(0.9), stk(K, 1), drawBasic()]),                             // 서치(시작 공격 카드)
      O('대련 상대', [dmg(1.0), stk(K, 1), draw(1, { who: 'other' })]),                                // 서치
      O('영웅담 한 줄', [dmg(1.0), stk(K, 1), inspire, stk(K, 2)]),
    ], [B('반복 숙달', 'power'), B('목검 자국', 'frost'), B('몸에 밴 동작', [stk(K, 1)])]),
    // u3 터뜨리기 — 어사이드 「영웅의 검」: 영웅심 1개당 한 칼 · 전부(2코 하나)
    card(H, 3, '영웅의 검', 2, '공격', [dmg(1.8), per(K), dmg(0.35), spendAll(K)], [
      O('전설의 일격', [dmg(2.1), per(K), dmg(0.42), spendAll(K)], { tags: WK }),
      O('뽑아 든 단검', [dmg(1.2), per(K), dmg(0.22), spendAll(K)], { cost: 1, tags: WK }),
      O('결착의 검', [dmg(2.9), per(K), dmg(0.5), heal(0.8)], { cost: 3, tags: WK }),                   // 비용↑ · 재설계 — 회복
      O('무용담의 검', [dmg(1.8), per(K), dmg(0.35), srch()], { tags: WK }),                             // 서치
      O('영웅의 검술', [dmg(1.0), stk(K, 1), power(reach(K, 3, [dmg(0.5, EA)]))], { cost: 1, power: true }),                    // 강화화 · 대가(약점 공격을 뗌)
    ], [B('영웅의 각오', 'power'), B('이름값', 'ap'), B('흔들린 적을 노려', 'weakSpot')], { tags: WK }),
    // u4 완성형(1코 마무리) — 영웅의 길: 영웅심 1개당 한 칼(쓰지 않음) · 고학년을 당김
    card(H, 4, '영웅의 길', 1, '공격', [dmg(0.6), per(K), dmg(0.3), gauge(10)], [
      O('전해질 무용담', [dmg(0.75), per(K), dmg(0.36), gauge(10)]),
      O('첫걸음', [dmg(0.8), gauge(30), power(rule('turnStart', [stk(K, 1)]))], { power: true }),                   // 강화화(BEST) — 옛 엔진 자리
      O('각성 직전', [gauge(30), dmg(1.0), srch()]),                                                               // 재설계 · 서치
      O('영웅의 이름', [dmg(0.6), per(K), dmg(0.4), spendAll(K)]),                                       // 대가
      O('첫 무용담', [dmg(0.4), per(K), dmg(0.2), gauge(10)], { cost: 0 }),
    ], [B('길잡이', 'draw'), B('영웅의 기개', 'atkUp'), B('친구의 목걸이', [stk(K, 1)])]),
    // u5 굴리기(스킬) — 영웅의 다짐: 영웅심 · 막기 · 한 장
    card(H, 5, '영웅의 다짐', 1, '스킬', [stk(K, 1), sh(1.0), draw(1)], [
      O('신탁 1', [stk(K, 1), sh(1.3), draw(1)]),
      O('신탁 2', [stk(K, 1), sh(0.5)], { cost: 0 }),
      O('신탁 3', [stk(K, 2), sh(0.9), srch(ATK, 2)]),                                          // 서치(BEST)
      O('신탁 4', [stk(K, 2), sh(1.0), ifStack(K, 2), heal(1.2)]),
      O('신탁 5', [discard(1), stk(K, 3), sh(1.4)]),                                          // 대가
    ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ]);
  starter(j, '티그_영웅_u2');
}

run([
  ['수인/그윈', gwin], ['수인/델리아', delia], ['수인/디아나_왕년', dianaOld], ['수인/란', ran], ['수인/루포', rufo],
  ['수인/리온', lion], ['수인/마고', mago], ['수인/모모', momo], ['수인/밍스', mynx], ['수인/바나', bana],
  ['수인/버터', butter], ['수인/베니', beni], ['수인/베니_베니', beniBeni], ['수인/슈로', shuro], ['수인/스패럿', sparrot],
  ['수인/에피카', epica], ['수인/우로스', uros], ['수인/유미미', yumimi], ['수인/쵸피', chopi], ['수인/코미', komi],
  ['수인/코미_수영복', komiSwim], ['수인/티그', tig], ['수인/티그_영웅', tigHero],
], new URL('./boost_수인.json', import.meta.url));
