// 사도 리워크 2단계 — 엘프 22명(2026-10-07) + 3단계 카제나 자료 보강(2026-10-08). 지침: _measure/사도_리워크_지침.md §12 · 공용 부품: lib.mjs
// node _gen/rework/엘프.mjs [사도 이름 일부]   → heroes/엘프/<사도>.json 덮어쓰기(백업 C:\projects\_backup\hero_rework_20261007 에서 읽음)
// 신탁 값어치 맞춤: bash _gen/rework/loop.sh 엘프.mjs boost_엘프.json "엘프/"
// 3단계 틀(카드마다 신탁 다섯): A 수치 · 비용(↓ 또는 고를 이유가 있는 ↑) · R 재설계 · D 강화화 또는 F 서치 · H 대가(손패 버리기 · 소멸 · 장치를 더 씀 · HP · 태그 빼기)
// 신탁에는 later(예약)를 쓰지 않는다 — loop.sh 의 boost 가 later.then 안 수치를 못 곱한다(2단계 오르 「드론 무리」). 예약은 기본형에만.
import { E1, EA, ER, dmg, ddef, hits, sh, heal, st, stk, spendAll, per, perTag, draw, make, ifStack, ifKill, ifWounded, ifAll, inspire, power, rule, later, O, B, card, starter, run } from './lib.mjs';

// ── 이 스크립트 몫 부품 ──
const exileAll = id => ({ k: 'exileFrom', from: 'hand', all: true, tag: id });
const exBasic = { k: 'exileFrom', from: 'hand', n: 1, basic: true };          // 손의 시작 카드 1장 소멸(기본 카드 연료)
const pullBasic = { k: 'pull', from: 'discard', basic: true };                 // 버린 더미의 시작 카드 1장을 손으로
const disc = v => ({ k: 'discard', v });
const ifTune = { k: 'ifTune' };
const onDisc = { k: 'when', on: 'discard' };
const extra = (r, t = E1) => ({ k: 'extra', ratio: r, target: t });
const tough = v => ({ k: 'tough', v });
const cleanse = v => ({ k: 'cleanse', v });
const dispel = n => ({ k: 'dispel', n });
const burn = v => ({ k: 'burn', v });
const mark = (id, v = 1, t = E1) => ({ k: 'stack', id, v, target: t });
const ifFoe = (id, o = {}) => ({ k: 'ifFoe', id, ...o });
const perDebuff = { k: 'perDebuff' };
const spendN = (id, v) => ({ k: 'spend', id, v });
const payHp = v => ({ k: 'payHpPct', v });
const srch = (type) => draw(1, { who: 'self', type });                         // 자신의 그 종류 카드 1장 드로우
const srchU = draw(1, { who: 'self', unique: true });                          // 자신의 고유 카드 1장 드로우(핵심 카드 되풀이)
const costDownHand = (type) => ({ k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'self', type });
const P = (name, when, fx, o = {}) => ({ name, when, ...(o.conds ? { conds: o.conds } : {}), ...(o.limit ? { limit: o.limit } : {}), fx });
const TL = n => ({ per: 'turn', n }), FL = n => ({ per: 'fight', n });
const setUnique = (j, cards) => { j.cards = [...j.cards.filter(c => !c.unique), ...cards]; };
const ultRef = (h, from, to) => { h.ult.fx = h.ult.fx.map(f => ((f.k === 'stack' || f.k === 'spend') && f.id === from ? { ...f, id: to } : f)); };
const reach = (id, n) => ({ when: { id, n } });
const OP = { tags: ['개전'] };                                                 // 개전 강화 시동 카드의 신탁(대가 갈래 말고는 개전을 지킨다)
const opc = (o = {}) => ({ ...o, tags: ['개전'] });
// 새 생성 카드(token) — 백업에 없으니 돌릴 때마다 넣는다
const token = (j, hero, id, name, type, fx, tags = ['소멸']) => { j.cards = j.cards.filter(c => c.id !== id); j.cards.push({ id, name, cost: 0, type, tags, fx, hero, token: true }); };
// u5(2026-10-08) — 회수 · 동료 카드 비용↓
const pullU = { k: 'pull', from: 'discard', who: 'self', unique: true };                 // 버린 더미의 자신의 고유 카드 1장을 손으로
const pullAtk = { k: 'pull', from: 'discard', who: 'self', type: '공격' };              // 버린 더미의 자신의 공격 카드 1장을 손으로
const costDownOther = { k: 'cardStatus', id: '비용', v: -1, to: 'hand', n: 1, who: 'other' };   // 손의 다른 사도 카드 1장 비용 -1

// ════════════════════════════════════════════════════════════════════
// 1. 레이지 — 딜러 · 냉정 · 1성. 부서마다 불려 다니는 만능 해결사: 종류마다 「출동 기록」, 넷이면 밤샘 끝의 레이저
// 3단계: 시동 = 개전 강화(리그 오브 엘프 — 매 턴 기록 + 시작 카드 서치: 단순 반복 작업 = 기본 카드 연료) · ④ 용접 야근 = 기록을 세는 1코 마무리(엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function lazy(j) {
  const H = '레이지', K = '출동 기록';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '부서마다 불려 다닌 기록', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.07 }],
    rules: [{ name: '밤샘 완료', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(0.8, EA), draw(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('만능 해결사', { on: 'play', type: '공격' }, [stk(K, 1), extra(0.7)], { limit: TL(1) }),
    P('만능 해결사', { on: 'play', type: '스킬' }, [stk(K, 1), sh(0.6)], { limit: TL(1) }),
    P('만능 해결사', { on: 'play', type: '강화' }, [stk(K, 1)], { limit: TL(1) }),
    P('커피 수혈', { on: 'fightStart' }, [stk(K, 2)]),
  ];
  const cards = [
    // u1 터뜨리기 — 원작 저학년 XG - 레이저(광역) · 기록 1개당 · 전부 소모
    card(H, 1, 'XG - 레이저', 1, '공격', [dmg(0.85, EA), per(K), dmg(0.22, EA), spendAll(K)], [
      O('출력 최대', [dmg(0.95, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      O('풀차지 레이저', [dmg(1.5, EA), per(K), dmg(0.35, EA), tough(1)], { cost: 2 }),
      O('한 놈만 조준', [dmg(1.4), ifStack(K, 3), dmg(0.9)]),
      O('레이저 상시 가동', [dmg(0.7, EA), per(K), dmg(0.2, EA), power(rule('stackReach', [dmg(0.45, EA)], reach(K, 4)))], { power: true }),
      O('마감 몰아치기', [disc(1), dmg(1.0, EA), per(K), dmg(0.26, EA)]),
    ], [B('고출력 렌즈', 'power'), B('작업 순서표', 'ap'), B('작업 일지', [stk(K, 1)])]),
    // u2 굴리기 — 유능해서 또 불려 감
    card(H, 2, '유능한 것도 문제예요', 1, '공격', [dmg(1.3), stk(K, 1)], [
      O('일 잘하는 죄', [dmg(1.5), stk(K, 1)]),
      O('대충 해도 됨', [dmg(0.85), stk(K, 1)], { cost: 0 }),
      O('또 불려 감', [dmg(1.1), ifStack(K, 3), dmg(0.75)]),
      O('겸사겸사', [dmg(1.05), stk(K, 1), srchU]),
      O('휴일 출근', [dmg(1.0), ifStack(K, 2), spendN(K, 2), dmg(1.1)]),
    ], [B('정밀 작업', 'weakSpot'), B('손에 익은 공구', 'cost'), B('부서 호출', [stk(K, 1)])]),
    // u3 열기(시동 — 개전 강화) — 리그 오브 엘프 승률 9할: 매 턴 기록 + 손에 익은 시작 카드부터
    card(H, 3, '리그 오브 엘프 90.1%', 0, '강화', [stk(K, 1), draw(1, { basic: true }), power(rule('turnStart', [stk(K, 1)]))], [
      O('승률 9할', [stk(K, 2), draw(1, { basic: true }), power(rule('turnStart', [stk(K, 1)]))], OP),
      O('버그 제보', [sh(0.6), draw(2), power(rule('turnStart', [stk(K, 1)]))], OP),
      O('밤샘 공략', [dmg(0.6, EA), power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [dmg(0.4, EA)], reach(K, 4)))], OP),
      O('공략 검색', [stk(K, 2), srchU, power(rule('turnStart', [stk(K, 1)]))], OP),
      O('쉬는 시간 반납', [stk(K, 3), sh(0.6), power(rule('turnStart', [stk(K, 1)]))]),
    ], [B('서버실 차폐', [sh(0.4)]), B('게이밍 의자', 'draw'), B('자유시간 사수', 'cost')], OP),
    // u4 완성형 — 용접 야근: 쥐고 있는 기록을 센다(쓰지 않음) — 1코 마무리
    card(H, 4, '용접 야근', 1, '공격', [dmg(0.8), per(K), dmg(0.3)], [
      O('철야 용접', [dmg(0.85), per(K), dmg(0.3)]),
      O('짧은 야근', [dmg(0.5), per(K), dmg(0.18)], { cost: 0 }),
      O('광역 용접', [dmg(0.55, EA), stk(K, 1), ifStack(K, 4), dmg(0.4, EA)]),
      O('용접 야근 상시', [dmg(0.6), per(K), dmg(0.2), power(rule('turnStart', [stk(K, 1)]), rule('stackReach', [dmg(0.35, EA)], reach(K, 4)))], { power: true }),
      O('밤샘에 설렘', [payHp(0.05), dmg(1.0), per(K), dmg(0.35)]),
    ], [B('달군 용접봉', 'frost'), B('용접 고글', 'draw'), B('야근 기록', [stk(K, 1)])]),
    // u5 유틸(서치) — 꼬인 선을 못 본 척 못 해 정리하다 붙잡힌다: 차폐 + 기록 + 고유 카드 서치
    card(H, 5, '꼬인 선 정리', 1, '스킬', [sh(0.9), stk(K, 1), srchU], [
      O('신탁 1', [sh(1.2), stk(K, 1), srchU]),
      O('신탁 2', [sh(0.7), stk(K, 1), srchU], { cost: 0 }),
      O('신탁 3', [costDownHand('공격'), sh(0.9), stk(K, 2)]),                                     // 재설계 — 서치 대신 공격 카드 비용↓
      O('신탁 4', [sh(0.7), pullU, srchU]),                                                   // F 회수 + 서치 — BEST
      O('신탁 5', [disc(1), sh(1.3), stk(K, 3)]),                                           // H 손패 버리기
    ], [B('축복 1', 'defUp'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '레이지_u3');
}

// ════════════════════════════════════════════════════════════════════
// 2. 로네 — 탱커 · 순수. 갑옷 속에 숨긴 「진심」 — 쥐고 있으면 덜 아프고(외교관), 건틀릿에 실으면 진심 펀치(첩보원)
// ════════════════════════════════════════════════════════════════════
function rohne(j) {
  const H = '로네', K = '로네의 진심';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '갑옷 속에 숨겨 둔 진심', carrier: 'self', cap: 3, per: [{ stat: 'taken', v: -0.04 }] };
  delete h.keywords;
  h.passives = [
    P('갑옷 속 진심', { on: 'hurt', guarded: true }, [stk(K, 1)], { limit: TL(2) }),
    P('항복 협상', { on: 'fightStart' }, [st('피해 감소', 1)]),
  ];
  const cards = [
    // u1 굴리기(방어) — 원작 저학년 어리바리 블랙옵스
    card(H, 1, '어리바리 블랙옵스', 1, '스킬', [sh(1.4), stk(K, 1)], [
      O('엘프 최고 스파이', [sh(1.8), stk(K, 1)]),
      O('완벽한 위장', [sh(2.5), st('피해 감소', 2), stk(K, 1)], { cost: 2 }),
      O('가장 약한 아군 곁', [sh(1.4), st('피해 감소', 1), ifWounded, sh(0.9)]),
      O('첩보 수첩', [sh(1.25), stk(K, 1), srch('공격')]),
      O('서류 파쇄', [disc(1), sh(2.0), stk(K, 2)]),
    ], [B('판금 덧대기', 'guard'), B('외교 대사 직함', 'cost'), B('흐린 존재감', [stk(K, 1)])]),
    // u2 열기 — 거짓말을 하면 더듬는다(약화 담당 · 시동 카드)
    card(H, 2, '더듬는 거짓말', 1, '스킬', [st('약화', 2, E1), stk(K, 1), draw(1)], [
      O('더… 더듬지 않았어오', [st('약화', 3, E1), stk(K, 1), draw(1)]),
      O('작은 거짓말', [st('약화', 2, E1), stk(K, 1)], { cost: 0 }),
      O('습관성 거짓말', [st('약화', 2, E1), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('거짓말 탄로', [st('약화', 2, E1), per(K), sh(0.75)]),
      O('거짓말 들통', [disc(1), st('약화', 2, EA), draw(2)]),
    ], [B('식은땀', 'frost'), B('일단 할 수 있다고', 'draw'), B('거짓말 한 겹', [stk(K, 1)])]),
    // u3 버티기 — 돈까스(결의 · 탱커의 세기형 버프 하나)
    card(H, 3, '소스 듬뿍 경양식', 1, '스킬', [heal(0.8), st('결의', 1), stk(K, 1)], [
      O('곱빼기 소스', [heal(1.1), st('결의', 1), stk(K, 1)]),
      O('돈까스 한 조각', [heal(0.6), st('결의', 1)], { cost: 0 }),
      O('감봉 전 회식', [heal(0.7), st('결의', 2), ifWounded, heal(0.8)]),
      O('단골 가게 검색', [heal(0.9), st('결의', 1), srchU]),
      O('보너스로 사 먹기', [st('결의', 2), per(K), heal(0.45), spendAll(K)]),
    ], [B('엘프 렌즈 시즌 7', 'heal'), B('포장 주문', { tags: ['보존'] }), B('든든한 한 끼', 'defUp')]),
    // u4 터뜨리기 — 원작 애착 강화 평타: 진심을 다한 공격(넉백 · 기절)
    card(H, 4, '로네의 건틀릿', 1, '공격', [ddef(0.6), per(K), ddef(0.3), spendAll(K)], [
      O('진심을 다한 공격', [ddef(0.8), per(K), ddef(0.38), spendAll(K)]),
      O('넉백', [ddef(1.2), per(K), ddef(0.5), st('약화', 1, E1)], { cost: 2 }),
      O('진심이 가득하면', [ddef(0.75), ifStack(K, 3), st('기절', 1, E1)]),
      O('진심의 건틀릿', [ddef(0.6), per(K), ddef(0.3), power(rule('stackReach', [ddef(0.5)], reach(K, 3)))], { power: true }),
      O('마지막 진심', [ddef(1.3), per(K), ddef(0.6), spendAll(K)], { tags: ['소멸'] }),
    ], [B('강철판', 'power'), B('돈까스 힘', 'ap'), B('갑옷 기름칠', [sh(0.5)])]),
    // u5 굴리기(공격) — 겨울의 스파이(산타 갑옷 돌격): 진심 + 방어 기반 피해
    card(H, 5, '징글벨 돌격', 1, '공격', [ddef(0.75), stk(K, 1)], [
      O('신탁 1', [ddef(0.98), stk(K, 1)]),
      O('신탁 2', [ddef(0.6), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [sh(1.0), stk(K, 1), ifStack(K, 3), ddef(0.5)]),                                // 재설계 — 막고 진심이 차면 친다
      O('신탁 4', [ddef(0.6), stk(K, 1), power(rule('hurt', [ddef(0.35)], { when: { guarded: true }, limit: 1 }))], { power: true }),   // D — 막아 낼 때마다 되받아침 · BEST
      O('신탁 5', [per(K), ddef(0.45), spendAll(K)]),                                             // H 진심을 다 쏟는다
    ], [B('축복 1', 'power'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '로네_u2');
  // 애착 장비(고급) — 범용 몫: 턴 시작 실드 50%(싸움당 1코 카드 0.5장꼴), 애착 몫: 전투 시작에 진심 1
  for (const e of j.equips || []) {
    e.effect = [P('외교적 방어', { on: 'turnStart' }, [sh(0.5)])];
    e.affinityEffect = [P('정체 숨기기', { on: 'fightStart' }, [stk(K, 1)])];
  }
}

// ════════════════════════════════════════════════════════════════════
// 3. 로네(시장) — 딜러 · 우울. 돈까스 도시락을 돌린다 — 먹으면 지지율(셋이면 시청 앞 축포), 쥐고 있으면 배달 드론의 포탄
// 3단계: 시동 = 개전 강화 「돈까스 생각」(u2 → u3 — 도시락이 비워질 때마다 지지자 돌진이 첫 턴부터)
// ════════════════════════════════════════════════════════════════════
function rohneMayor(j) {
  const H = '로네_시장', K = '지지율', T = '로네_시장_t1';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '도시락으로 모으는 민심', carrier: 'self', cap: 3,
    rules: [{ name: '시청 앞 축포', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), { k: 'dmg', ratio: 0.4, hits: 3, target: EA }] }],
  };
  delete h.keywords;
  h.passives = [
    P('돈까스 배달', { on: 'play' }, [make(T, 1)], { limit: TL(2) }),
    P('바삭한 꿈', { on: 'fightStart' }, [make(T, 1)]),
  ];
  Object.assign(j.cards.find(c => c.id === T), { cost: 0, type: '스킬', tags: ['소멸'], fx: [heal(0.5), stk(K, 1)] });
  const x4 = r => ({ k: 'dmg', ratio: r, hits: 4, target: EA });
  const cards = [
    // u1 터뜨리기 — 원작 저학년: 배달 드론 4회 광역 · 손의 도시락 1장당
    card(H, 1, '돈까스 시장, 로네!', 1, '공격', [x4(0.2), perTag(T), dmg(0.3, EA), exileAll(T)], [
      O('특대 배달', [x4(0.25), perTag(T), dmg(0.36, EA), exileAll(T)]),
      O('시민 대축제', [x4(0.36), perTag(T), dmg(0.45, EA), make(T, 1)], { cost: 2 }),
      O('도시락 배달 드론', [hits(4, 0.36, E1), make(T, 1)]),
      O('배달 경로 검색', [x4(0.2), perTag(T), dmg(0.26, EA), srch('스킬')]),
      O('급한 배달', [disc(1), x4(0.26), perTag(T), dmg(0.36, EA)]),
    ], [B('바삭한 튀김옷', 'power'), B('배달 앱', 'draw'), B('덤 도시락', [make(T, 1)])]),
    // u2 굴리기 — 배달 주문(도시락 둘)
    card(H, 2, '배달 주문', 1, '스킬', [make(T, 2), draw(1)], [
      O('곱빼기 주문', [make(T, 3), draw(1)]),
      O('단골 할인', [make(T, 2)], { cost: 0 }),
      O('정기 배달', [make(T, 1), draw(1), power(rule('turnStart', [make(T, 1)]))], { power: true }),
      O('시장실 회식', [make(T, 2), stk(K, 1), heal(1.0)]),
      O('도시락 몰아 받기', [disc(1), make(T, 3), draw(1)]),
    ], [B('뜨거운 소스', 'defUp'), B('시장 할인', 'cost'), B('도시락 하나 더', [make(T, 1)])]),
    // u3 열기(시동 — 개전 강화) — 돈까스 생각: 도시락이 비워질(소멸할) 때마다 지지자 돌진
    card(H, 3, '돈까스 생각', 1, '강화', [stk(K, 1), power(rule('exhaust', [dmg(0.35, ER)], { limit: 3 }))], [
      O('돈까스 생각뿐', [stk(K, 1), power(rule('exhaust', [dmg(0.48, ER)], { limit: 3 }))], OP),
      O('한 입 생각', [power(rule('exhaust', [dmg(0.35, ER)], { limit: 3 }))], opc({ cost: 0 })),
      O('도시락 공장', [make(T, 1), power(rule('exhaust', [dmg(0.3, ER)], { limit: 3 }), rule('turnStart', [make(T, 1)]))], OP),
      O('지지자 명단', [stk(K, 1), srchU, power(rule('exhaust', [dmg(0.35, ER)], { limit: 3 }))], OP),
      O('퇴근길 돈까스', [stk(K, 2), make(T, 1), power(rule('exhaust', [dmg(0.4, ER)], { limit: 3 }))]),
    ], [B('돈까스 상징 마크', 'atkUp'), B('공약 이행', 'cost'), B('지지자 함성', [stk(K, 1)])], OP),
    // u4 마무리 — 돈까스 모독엔 말이 짧아진다: 지지율 전부를 한 방에(2코 — 비싼 한 방)
    card(H, 4, '돈까스 모독은 용서 못 해', 2, '공격', [dmg(2.0), per(K), dmg(0.5), spendAll(K)], [
      O('말이 짧아짐', [dmg(2.5), per(K), dmg(0.6), spendAll(K)]),
      O('참을 인 세 번', [dmg(1.6), per(K), dmg(0.4), spendAll(K)], { cost: 1 }),
      O('도시락 투척', [dmg(2.1), perTag(T), dmg(0.6), exileAll(T)]),
      O('돈까스 모독 금지령', [dmg(2.0), per(K), dmg(0.5), power(rule('stackReach', [dmg(1.0)], reach(K, 3)))], { power: true }),
      O('시장직을 걸고', [dmg(4.0), per(K), dmg(0.8), make(T, 2)], { tags: ['소멸'] }),
    ], [B('공기 커틀릿', 'weakSpot'), B('빠른 배달', 'ap'), B('짧아진 말투', 'power')]),
    // u5 유틸(비용) — 업무 효율 131% 상승: 지지율 + 공격 카드 비용↓
    card(H, 5, '업무 효율 131%', 0, '스킬', [stk(K, 1), costDownHand('공격')], [
      O('신탁 1', [stk(K, 2), costDownHand('공격')]),
      O('신탁 2', [stk(K, 1), costDownHand('공격')], { tags: ['보존'] }),
      O('신탁 3', [make(T, 2)]),                                                                    // 재설계 — 지지율 대신 도시락
      O('신탁 4', [stk(K, 1), srch('공격'), costDownHand('공격')]),                               // F 공격 서치 + 비용↓ — BEST
      O('신탁 5', [spendAll(K), make(T, 3), draw(1)]),                                              // H 지지율을 다 털어 도시락으로
    ], [B('축복 1', 'draw'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '로네_시장_u3');
}

// ════════════════════════════════════════════════════════════════════
// 4. 리뉴아 — 딜러 · 광기 · 엘다인. 규격에 딱 맞는 1초: 카드마다 「초침」, 넷이면 정각의 레이저. 남은 AP 를 딱 맞추면(조율) 한 번 더 울린다
// ④ 오천 년 뒤의 편지는 강화 카드 그대로(엘다인 한 단계 — 정각마다 미사일)
// ════════════════════════════════════════════════════════════════════
function renewa(j) {
  const H = '리뉴아', K = '초침';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '규격에 맞춰 도는 초침', carrier: 'self', cap: 4,
    rules: [{ name: '정각', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), dmg(0.6, EA), { k: 'dealtMod', v: 0.15, target: 'self' }] }],
  };
  delete h.keywords;
  h.passives = [
    P('째깍', { on: 'play' }, [stk(K, 1)], { limit: TL(3) }),
    // 원작 엘다인 패시브 「시공간, 저 너머로!」 — 크게 맞으면 미사일(전투당 1회)
    P('시공간, 저 너머로!', { on: 'hurt', pct: 0.08 }, [dmg(1.0, EA), st('피해 감소', 1)], { limit: FL(1) }),
  ];
  const x5 = r => ({ k: 'dmg', ratio: r, hits: 5, target: EA });
  const cards = [
    // u1 굴리기 — 원작 저학년 시공의 메아리(5회 포격 · 버프 해제)
    card(H, 1, '시공의 메아리', 1, '공격', [x5(0.16), dispel(1), stk(K, 1)], [
      O('다섯 번의 메아리', [x5(0.21), dispel(1), stk(K, 1)]),
      O('무한 회귀', [x5(0.3), dispel(2), stk(K, 2)], { cost: 2 }),
      O('딱 맞는 메아리', [x5(0.16), stk(K, 1), ifTune, x5(0.12)]),
      O('시간선 추적', [x5(0.17), stk(K, 1), srchU]),
      O('메아리 과부하', [disc(1), x5(0.24), stk(K, 2)]),
    ], [B('마도 공학 포탄', 'power'), B('계산 끝', 'draw'), B('메아리 한 번 더', [stk(K, 1)])]),
    // u2 열기 — 남은 AP 가 0 일 때 내면 초침이 더 돈다(시동 카드)
    card(H, 2, '규격에 딱 맞는 1초', 0, '스킬', [stk(K, 1), draw(1), ifTune, stk(K, 2)], [
      O('1초의 오차도 없이', [stk(K, 2), draw(1), ifTune, stk(K, 2)]),
      O('째깍째깍', [stk(K, 1), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('시간선 검색', [stk(K, 1), srch('공격'), draw(1)]),
      O('정각 맞추기', [stk(K, 1), ifStack(K, 3), dmg(0.6, EA)]),
      O('오차 수정', [disc(1), stk(K, 2), draw(2)]),
    ], [B('비밀 기지', { tags: ['보존'] }), B('호버보드', 'ap'), B('바코드 스캔', [stk(K, 1)])]),
    // u3 굴리기 — 수많은 시간선을 건너온 한 발(조율이면 한 번 더)
    card(H, 3, '수많은 시간선', 1, '공격', [dmg(1.0), stk(K, 1), ifTune, dmg(0.7)], [
      O('겹친 시간선', [dmg(1.3), stk(K, 1), ifTune, dmg(0.9)]),
      O('짧은 시간선', [dmg(0.75), stk(K, 1), ifTune, dmg(0.55)], { cost: 0 }),
      O('실패한 시간선', [dmg(1.0), stk(K, 1), ifStack(K, 3), dmg(0.8)]),
      O('평행 세계 탐색', [dmg(1.05), stk(K, 1), srchU]),
      O('시간선 정리', [disc(1), dmg(1.5), stk(K, 2)]),
    ], [B('실드 파괴', 'weakSpot'), B('평행 이동', 'cost'), B('마도 공학 에너지', 'atkUp')]),
    // u4 완성형(엘다인 한 단계 더 — 강화 카드 그대로) — 정각마다 미사일 투하
    card(H, 4, '오천 년 뒤의 편지', 1, '강화', [stk(K, 2), power(rule('stackReach', [hits(3, 0.35, ER)], reach(K, 4)))], [
      O('긴 편지', [stk(K, 2), power(rule('stackReach', [hits(3, 0.48, ER)], reach(K, 4)))]),
      O('짧은 편지', [power(rule('stackReach', [hits(3, 0.35, ER)], reach(K, 4)))], { cost: 0 }),
      O('답장 요청', [dmg(0.6), draw(1), power(rule('stackReach', [hits(3, 0.3, ER)], reach(K, 4)))]),
      O('편지 배달 추적', [stk(K, 1), srch('공격'), power(rule('stackReach', [hits(3, 0.35, ER)], reach(K, 4)))]),
      O('시간 여행 비용', [payHp(0.05), stk(K, 3), power(rule('stackReach', [hits(3, 0.45, ER)], reach(K, 4)))]),
    ], [B('엘레나의 서명', 'defUp'), B('시공 우편', 'cost'), B('멈춘 시계', [stk(K, 1)])]),
    // u5 유틸(회수) — 즐거웠던 기억 떠올리기: 실드 + 초침 + 버린 고유 카드 회수
    card(H, 5, '즐거웠던 기억', 1, '스킬', [sh(0.85), stk(K, 1), pullU], [
      O('신탁 1', [sh(1.1), stk(K, 1), pullU]),
      O('신탁 2', [sh(0.85), pullU, ifTune, stk(K, 3)]),                               // E 조율 조건
      O('신탁 3', [dmg(0.6, EA), stk(K, 1), draw(1)]),                                             // 재설계 — 기억 대신 포격
      O('신탁 4', [sh(0.85), pullU, srchU]),                                             // F 회수 + 서치 — BEST
      O('신탁 5', [payHp(0.05), sh(1.2), stk(K, 3)]),                                       // H HP
    ], [B('축복 1', 'defUp'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '리뉴아_u2');
}

// ════════════════════════════════════════════════════════════════════
// 5. 리스티 — 딜러 · 우울. 공격마다 적의 「개인정보」를 모아 셋이면 신상 털기(확정 치명) — 테크노맨시로 미리 털지, 다 모을지
// 3단계: 생성 카드 「빈 캔」(다 마신 캔이 남기는 0코 투척 — 개인정보 1) · ④ 글러브는 강화 그대로(원작 강화 평타 = 곰인형 AI 상시)
// ════════════════════════════════════════════════════════════════════
function risty(j) {
  const H = '리스티', K = '개인정보', C = '리스티_can';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '해킹으로 긁어모은 적의 신상', carrier: 'enemy', cap: 3,
    rules: [{ name: '신상 털기', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), dmg(0.45), tough(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('글러브 해킹', { on: 'play', type: '공격', minCost: 1 }, [mark(K, 1)], { limit: TL(1) }),
    P('천재 해커의 등장', { on: 'spend', id: K }, [{ k: 'gauge', v: 5 }], { limit: TL(1) }),
  ];
  h.ult.fx = h.ult.fx.map(f => (f.k === 'stack' ? mark(K, 2) : f));
  token(j, H, C, '빈 캔', '공격', [dmg(0.2)]);
  const cards = [
    // u1 터뜨리기 — 원작 저학년 테크노맨시(최대 3번 다시 찾아 친다) · 3코 → 2코
    card(H, 1, '테크노맨시', 2, '공격', [hits(3, 0.45, E1), per(K), dmg(0.4), spendAll(K)], [
      O('마지막 타격 강화', [hits(3, 0.62, E1), per(K), dmg(0.55), spendAll(K)]),
      O('라이트 해킹', [hits(3, 0.4, E1), per(K), dmg(0.35)], { cost: 1 }),
      O('캔 던지기 해킹', [hits(3, 0.56, E1), perTag(C), dmg(0.45), exileAll(C)]),
      O('상주 해킹 프로그램', [hits(3, 0.5, E1), per(K), dmg(0.4), power(rule('stackReach', [hits(2, 0.35, E1)], reach(K, 3)))], { power: true }),
      O('서버 다운', [hits(3, 1.15, E1), per(K), dmg(1.0), spendAll(K)], { tags: ['소멸'] }),
    ], [B('복셀 블록', 'power'), B('독타 페퍼', 'ap'), B('로그 수집', [mark(K, 1)])]),
    // u2 열기 — 못 깨는 게임은 없다(시동 카드)
    card(H, 2, '훗, 못 깨는 게임은 없지', 0, '스킬', [mark(K, 1), draw(1)], [
      O('공략 완료', [mark(K, 2), draw(1)]),
      O('무한 콤보', [draw(1), power(rule('turnStart', [mark(K, 1, 'topEnemy')]))], { power: true }),
      O('공략집 검색', [mark(K, 1), srch('공격'), draw(1)]),
      O('버그 악용', [mark(K, 1), ifStack(K, 2), dmg(0.6)]),
      O('밤샘 게임', [disc(1), mark(K, 2), draw(2)]),
    ], [B('세이브 파일', { tags: ['보존'] }), B('골판지 왕관', [sh(0.4)]), B('다크넷 계정', [mark(K, 1)])]),
    // u3 굴리기 — 다 마신 캔(원작 기본 공격) — 빈 캔을 남긴다
    card(H, 3, '다 마신 캔', 1, '공격', [dmg(0.75), mark(K, 1), make(C, 1)], [
      O('독타 페퍼 캔', [dmg(1.1), mark(K, 1), make(C, 1)]),
      O('찌그러뜨린 캔', [dmg(0.55), mark(K, 1), make(C, 1)], { cost: 0 }),
      O('캔 탑 쌓기', [dmg(0.7), perTag(C), dmg(0.3), make(C, 1)]),
      O('냉장고 뒤지기', [dmg(1.1), make(C, 1), srchU]),
      O('분리수거', [disc(1), dmg(1.1), make(C, 2)]),
    ], [B('찌그러진 캔', 'frost'), B('한 캔 더', 'draw'), B('캔 하나 더', [make(C, 1)])]),
    // u4 완성형(강화 그대로) — 곰인형 AI 글러브: 신상 털기가 더 아프다
    card(H, 4, '글러브', 1, '강화', [mark(K, 1), power(rule('stackReach', [dmg(0.35)], reach(K, 3)))], [
      O('글러브 풀가동', [mark(K, 1), power(rule('stackReach', [dmg(0.75)], reach(K, 3)))]),
      O('글러브 대기 모드', [power(rule('stackReach', [dmg(0.45)], reach(K, 3)))], { cost: 0 }),
      O('양심의 목소리', [sh(0.6), draw(1), power(rule('stackReach', [dmg(0.5), sh(0.5)], reach(K, 3)))]),
      O('현실 개변 장치', [mark(K, 1), srch('공격'), power(rule('stackReach', [dmg(0.5)], reach(K, 3)))]),
      O('곰인형 혹사', [payHp(0.05), mark(K, 2), power(rule('stackReach', [dmg(0.7)], reach(K, 3)))]),
    ], [B('말동무', 'atkUp'), B('호숫가 집', 'cost'), B('방구석 바리케이드', [sh(0.4)])]),
    // u5 유틸(생성) — 독타 페퍼 한 박스: 빈 캔 둘 + 드로우
    card(H, 5, '독타 페퍼 한 박스', 0, '스킬', [make(C, 2), draw(1)], [
      O('신탁 1', [make(C, 3), draw(1)]),
      O('신탁 2', [make(C, 2), draw(1), mark(K, 1)], { tags: ['보존'] }),
      O('신탁 3', [make(C, 2), mark(K, 1), draw(1, { who: 'other' })]),                            // 재설계 — 협동 게임(동료 카드)
      O('신탁 4', [make(C, 1), draw(1), power(rule('turnStart', [make(C, 1)]))], { power: true }), // D 매 턴 캔 — BEST
      O('신탁 5', [disc(1), make(C, 3), mark(K, 1)]),                                              // H 손패 버리기
    ], [B('축복 1', 'draw'), B('축복 2', 'cost'), B('축복 3', [mark(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '리스티_u2');
  // 애착 장비(전설) — 범용 몫: 칠 때 약화 1(턴당 1회) + 턴 시작 실드 40%, 애착 몫: 전투 시작에 가장 센 적의 개인정보 1
  for (const e of j.equips || []) {
    e.effect = [P('입력 방해', { on: 'hit' }, [st('약화', 1, E1)], { limit: TL(1) }), P('손에 붙은 패드', { on: 'turnStart' }, [sh(0.4)])];
    e.affinityEffect = [P('예열된 손가락', { on: 'fightStart' }, [mark(K, 1, 'topEnemy')])];
  }
}

// ════════════════════════════════════════════════════════════════════
// 6. 마에스트로 2호 — 탱커 · 광기. 무엇이든 삼켜 「삼킨 연료」로 — 다섯이면 방화벽, 그 전에 진격 방해 모드로 태울지
// 3단계: 시동 = 개전 강화(A3쨩 정비 — 매 턴 연료) · 「무엇이든 삼키기」 갈래가 손의 시작 카드를 삼킨다(기본 카드 연료) · ④ 카메라 담당 = 연료를 세는 1코 마무리(실드)
// ════════════════════════════════════════════════════════════════════
function maestro(j) {
  const H = '마에스트로2호', K = '삼킨 연료';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '무엇이든 삼켜 채운 연료', carrier: 'self', cap: 5,
    rules: [{ name: '방화벽 가동', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), st('피해 감소', 2), st('약화', 1, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    P('무엇이든 연료로', { on: 'guard', kind: 'shield' }, [stk(K, 1)], { limit: TL(3) }),
    P('자가 회복 기능', { on: 'fightStart' }, [stk(K, 1), sh(0.6)]),
  ];
  ultRef(h, '배터리', K);
  const cards = [
    // u1 굴리기 — 원작 저학년 로보틱 매트릭스(자신 + 모든 아군 보호막)
    card(H, 1, '로보틱 매트릭스', 1, '스킬', [sh(1.35), stk(K, 1)], [
      O('매트릭스 강화', [sh(1.75), stk(K, 1)]),
      O('전 아군 실드', [sh(2.5), stk(K, 2), st('결정화', 1)], { cost: 2 }),
      O('반사 장갑', [sh(1.0), st('반격', 1)]),
      O('정비 매뉴얼', [sh(1.2), stk(K, 1), srchU]),
      O('연료 직결 장갑', [sh(1.2), ifStack(K, 3), spendN(K, 3), sh(1.3)]),
    ], [B('장갑판', 'guard'), B('수은음료', 'draw'), B('주유구 개방', [stk(K, 1)])]),
    // u2 열기(시동 — 개전 강화) — A3쨩 정비: 매 턴 연료
    card(H, 2, 'A3쨩 정비', 1, '강화', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('특대 A3 배터리', [stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], OP),
      O('저전력 충전', [power(rule('turnStart', [stk(K, 1)]))], opc({ cost: 0 })),
      O('자가 정비 루틴', [sh(0.6), draw(1), power(rule('guard', [stk(K, 1)], { when: { kind: 'shield' }, limit: 1 }))], OP),
      O('정비 매뉴얼 검색', [stk(K, 1), srchU, power(rule('turnStart', [stk(K, 1)]))], OP),
      O('무엇이든 삼키기', [exBasic, stk(K, 3), power(rule('turnStart', [stk(K, 1)]))]),
    ], [B('블루 스크린', [sh(0.4)]), B('저전력 모드', 'cost'), B('예비 배터리', [stk(K, 1)])], OP),
    // u3 터뜨리기 — 진격 방해 모드(연료 전부)
    card(H, 3, '진격 방해 모드', 1, '공격', [ddef(0.55), per(K), ddef(0.2), spendAll(K)], [
      O('풀파워 방해', [ddef(0.72), per(K), ddef(0.26), spendAll(K)]),
      O('쇼크 웨이브 예열', [ddef(1.15), per(K), ddef(0.38), st('둔화', 1, E1)], { cost: 2 }),
      O('으음… 과연', [ddef(0.8), ifStack(K, 3), st('둔화', 1, E1), ddef(0.4)]),
      O('방해 모드 상시', [ddef(0.55), per(K), ddef(0.2), power(rule('stackReach', [ddef(0.5, EA)], reach(K, 5)))], { power: true }),
      O('연료 폭주', [disc(1), ddef(0.75), per(K), ddef(0.26)]),
    ], [B('기계 팔', 'power'), B('소음 발생기', 'frost'), B('남은 연료', 'ap')]),
    // u4 완성형 — 카메라 담당: 쥐고 있는 연료만큼 실드(쓰지 않음) — 1코 마무리
    card(H, 4, '카메라 담당', 1, '스킬', [sh(0.8), per(K), sh(0.2)], [
      O('고화질 녹화', [sh(1.0), per(K), sh(0.25)]),
      O('셀카봉', [sh(0.55), per(K), sh(0.14)], { cost: 0 }),
      O('녹화본 재생', [sh(0.6), stk(K, 2), draw(1)]),
      O('카메라 상시 녹화', [sh(0.6), power(rule('turnEnd', [per(K), sh(0.15)]))], { power: true }),
      O('녹화 테이프 소진', [per(K), sh(0.38), spendAll(K), draw(1)]),
    ], [B('자기 성찰', 'defUp'), B('자동 녹화', { tags: ['보존'] }), B('연료 보충', [stk(K, 1)])]),
    // u5 굴리기(공격) — 척살 모드 전환 경고(실행되진 않음): 연료 + 방어 기반 피해
    card(H, 5, '척살 모드 전환', 1, '공격', [ddef(0.7), stk(K, 1)], [
      O('신탁 1', [ddef(0.9), stk(K, 1)]),
      O('신탁 2', [ddef(0.55), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [ddef(0.7), ifKill, stk(K, 3)]),                                                  // 재설계 — 처치하면 무지개불(연료 셋)
      O('신탁 4', [ddef(0.6), stk(K, 1), power(rule('guard', [ddef(0.3, ER)], { when: { kind: 'shield' }, limit: 1 }))], { power: true }),   // D — 실드를 얻을 때마다 반격 · BEST
      O('신탁 5', [payHp(0.05), ddef(1.0), stk(K, 2)]),                                           // H 미숫가루(HP)
    ], [B('축복 1', 'weakSpot'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '마에스트로2호_u2');
}

// ════════════════════════════════════════════════════════════════════
// 7. 아멜리아 — 서포터(축: AP) · 냉정. 아군이 일할 때마다 「결재 서류」 — 넷이면 일괄 결재(AP), 그 전에 감봉 통보로 다 쓸지. 감전(충격) 담당
// ④ 감시카메라 32대는 강화 그대로(원작 상시 감시)
// ════════════════════════════════════════════════════════════════════
function amelia(j) {
  const H = '아멜리아', K = '결재 서류';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '아군이 일할 때 올라오는 서류', carrier: 'self', cap: 4,
    rules: [{ name: '일괄 결재', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), { k: 'ap', v: 1 }] }],
  };
  delete h.keywords;
  h.passives = [
    P('서류 접수', { on: 'play', who: 'other' }, [stk(K, 1)], { limit: TL(3) }),
    P('초고속 썬더 레이저', { on: 'play', type: '공격' }, [st('충격', 1, E1)], { limit: TL(1) }),
  ];
  const camera = (v = 1) => rule('play', [st('충격', v, E1)], { when: { who: 'other', type: '공격' }, limit: 1 });
  const cards = [
    // u1 굴리기 — 원작 저학년 새틀라이트 전술폭격(광역 + 감전)
    card(H, 1, '새틀라이트 전술폭격', 1, '공격', [dmg(0.85, EA), st('충격', 1, EA)], [
      O('Mk.2 폭격', [dmg(0.95, EA), st('충격', 1, EA)]),
      O('새틀라이트 오버히트', [{ k: 'dmg', ratio: 0.35, hits: 4, target: EA }, st('충격', 2, EA), stk(K, 1)], { cost: 2 }),
      O('결재된 폭격', [dmg(0.75, EA), ifStack(K, 2), dmg(0.45, EA)]),
      O('좌표 요청', [dmg(0.72, EA), st('충격', 1, EA), draw(1, { who: 'other' })]),
      O('서류 1장당 폭격', [per(K), dmg(0.28, EA), st('충격', 1, EA), spendAll(K)]),
    ], [B('레이저 출력', 'power'), B('엘레나 사진', 'ap'), B('결재 도장', [stk(K, 1)])]),
    // u2 터뜨리기 — 감봉 통보(서류 전부)
    card(H, 2, '감봉 통보', 1, '공격', [dmg(0.6), per(K), dmg(0.3), spendAll(K)], [
      O('일괄 감봉', [dmg(0.8), per(K), dmg(0.36), spendAll(K)]),
      O('구두 경고', [dmg(0.5), per(K), dmg(0.22), spendAll(K)], { cost: 0 }),
      O('감전된 직원', [dmg(1.1), st('약화', 1, E1), ifFoe('충격'), dmg(0.8)]),
      O('상시 감봉 제도', [dmg(0.6), per(K), dmg(0.3), power(rule('stackReach', [dmg(0.6)], reach(K, 4)))], { power: true }),
      O('징계 위원회', [disc(1), dmg(0.8), per(K), dmg(0.38)]),
    ], [B('찌릿한 서명', 'weakSpot'), B('야근 금지', 'draw'), B('감봉 서식', 'frost')]),
    // u3 열기 — 모나티엄 행정 대행(시동 카드)
    card(H, 3, '모나티엄 행정 대행', 0, '스킬', [stk(K, 2), draw(1, { who: 'other' })], [
      O('일괄 처리', [stk(K, 3), draw(1, { who: 'other' })]),
      O('상시 대행', [stk(K, 1), draw(1, { who: 'other' }), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('업무 대행', [stk(K, 2), { k: 'castOther' }]),
      O('결재 우선순위', [stk(K, 3), draw(1, { who: 'other', type: '공격' })], { tags: ['보존'] }),
      O('시장님 일정 우선', [disc(1), stk(K, 3), draw(2, { who: 'other' })]),
    ], [B('관리자 직함', { tags: ['보존'] }), B('일기 작성', 'ap'), B('복지 탄원서 반려', [stk(K, 1)])]),
    // u4 완성형(강화 그대로) — 감시카메라 32대: 아군이 공격할 때마다 그 적에게 충격
    card(H, 4, '감시카메라 32대', 1, '강화', [stk(K, 1), power(camera())], [
      O('64대 증설', [stk(K, 1), power(camera(2))]),
      O('몰래카메라', [power(camera())], { cost: 0 }),
      O('녹화본 판독', [st('충격', 1, EA), draw(1), power(camera())]),
      O('녹화본 열람', [stk(K, 1), draw(1, { who: 'other' }), power(camera())]),
      O('야근 감시', [payHp(0.05), stk(K, 2), power(camera(2))]),
    ], [B('부동산 카메라', 'cost'), B('채널 고정', 'defUp'), B('서류 더미', [stk(K, 1)])]),
    // u5 유틸(비용) — 승인 등급은 전부 「그냥」: 서류 + 동료 카드 비용↓
    card(H, 5, '그냥 승인', 0, '스킬', [stk(K, 1), costDownOther], [
      O('신탁 1', [stk(K, 2), costDownOther]),
      O('신탁 2', [stk(K, 1), costDownOther], { tags: ['보존'] }),
      O('신탁 3', [st('충격', 1, EA), draw(1, { who: 'other' })]),                                  // 재설계 — 입 모양 읽기(감전 + 동료 카드)
      O('신탁 4', [stk(K, 1), costDownOther, power(rule('stackReach', [costDownOther], reach(K, 4)))], { power: true }),   // D 일괄 결재마다 비용↓ — BEST
      O('신탁 5', [disc(1), stk(K, 2), costDownOther]),                                            // H 손패 버리기
    ], [B('축복 1', 'draw'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '아멜리아_u3');
  // 애착 장비(전설) — 범용 몫: 스킬마다 충격 1(턴당 1회) + 전투 시작 적 전체 충격 1, 애착 몫: 전투 시작 결재 서류 2
  for (const e of j.equips || []) {
    e.effect = [P('찌릿한 화면', { on: 'play', type: '스킬' }, [st('충격', 1, E1)], { limit: TL(1) }), P('화면 켜기', { on: 'fightStart' }, [st('충격', 1, EA)])];
    e.affinityEffect = [P('결재함 동기화', { on: 'fightStart' }, [stk(K, 2)])];
  }
}

// ════════════════════════════════════════════════════════════════════
// 8. 아멜리아(R41) — 서포터(축: 회복) · 우울. 카드마다 「시제품」, 시제품을 셋 써 보면(시험 가동) 완성품 — 시제품을 전자포에 몰아 넣을지
// 3단계: 시동 = 개전 강화 「영광인 줄 알거라」(u2 → u3 — 매 턴 시제품)
// ════════════════════════════════════════════════════════════════════
function ameliaR41(j) {
  const H = '아멜리아_R41', K = '시험 가동', T1 = '아멜리아_R41_t1', T2 = '아멜리아_R41_t2';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '시제품을 쏴 본 횟수', carrier: 'self', cap: 3,
    rules: [{ name: '완성', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), make(T2, 1), heal(0.5)] }],
  };
  delete h.keywords;
  h.passives = [
    P('이 몸의 시제품', { on: 'play' }, [make(T1, 1)], { limit: TL(2) }),
    P('고출력 실드', { on: 'overheal' }, [{ k: 'shield', ratio: 1, ofEvent: 0.5 }], { limit: TL(1) }),
  ];
  Object.assign(j.cards.find(c => c.id === T1), { cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(1.5), stk(K, 1), { k: 'ifRandom', pct: 0.25 }, { k: 'payHpPct', v: 0.03 }] });
  Object.assign(j.cards.find(c => c.id === T2), { cost: 0, type: '공격', tags: ['소멸'], fx: [dmg(1.8), heal(0.8)] });
  const cards = [
    // u1 굴리기 — 원작 저학년 엘렌-A 오버차지(치료 증폭 드론 — 넘친 회복은 실드로)
    card(H, 1, '엘렌-A 오버차지', 1, '스킬', [heal(1.2), stk(K, 1)], [
      O('고출력 드론', [heal(1.4), stk(K, 1)]),
      O('치료 증폭 드론 둘', [heal(2.0), stk(K, 2), st('결정화', 1)], { cost: 2 }),
      O('공격형 드론', [dmg(1.05), stk(K, 1)]),
      O('설계 수정', [heal(1.0), stk(K, 1), srchU]),
      O('시험 데이터 소모', [heal(1.0), ifStack(K, 2), spendN(K, 2), heal(1.1)]),
    ], [B('엘렌-A', 'heal'), B('커피 심부름', 'draw'), B('시험 기록', [stk(K, 1)])]),
    // u2 굴리기 — 나노 수복 드론: 시제품 둘
    card(H, 2, '나노 수복 드론', 1, '스킬', [make(T1, 2), draw(1)], [
      O('양산 라인', [make(T1, 3), draw(1)]),
      O('시제품 하나', [make(T1, 2)], { cost: 0 }),
      O('자동 생산', [make(T1, 1), draw(1), power(rule('turnStart', [make(T1, 1)]))], { power: true }),
      O('수복 드론 편대', [make(T1, 2), heal(1.1), stk(K, 1)]),
      O('불량품 폐기', [disc(1), make(T1, 3), draw(1)]),
    ], [B('홀터넥 갑옷', 'defUp'), B('제국의 공방', 'cost'), B('예비 시제품', [make(T1, 1)])]),
    // u3 열기(시동 — 개전 강화) — 영광인 줄 알거라: 매 턴 시제품(결정화 — 세기형 버프 하나)
    card(H, 3, '영광인 줄 알거라', 1, '강화', [st('결정화', 1), power(rule('turnStart', [make(T1, 1)]))], [
      O('세계 최고의 엘프', [st('결정화', 1), power(rule('turnStart', [make(T1, 1), stk(K, 1)]))], OP),
      O('이 몸의 명령', [power(rule('turnStart', [make(T1, 1)]))], opc({ cost: 0 })),
      O('완성품 직행', [make(T2, 1), heal(0.5), power(rule('turnStart', [make(T1, 1)]))], OP),
      O('설계도 검색', [st('결정화', 1), srch('공격'), power(rule('turnStart', [make(T1, 1)]))], OP),
      O('나르시시스트 듀얼', [st('결정화', 1), make(T1, 2), power(rule('turnStart', [make(T1, 1)]))]),
    ], [B('어제보다 완벽', [heal(0.4)]), B('제국 예산', 'ap'), B('통제 가능한 것', 'defUp')], OP),
    // u4 터뜨리기 — 소형 전자포: 손의 시제품을 몰아 쏜다
    card(H, 4, '소형 전자포', 1, '공격', [dmg(0.9), perTag(T1), dmg(0.35), exileAll(T1)], [
      O('고출력 전자포', [dmg(1.15), perTag(T1), dmg(0.45), exileAll(T1)]),
      O('초고출력 전자포', [dmg(0.95, EA), perTag(T1), dmg(0.35, EA), exileAll(T1)], { cost: 2 }),
      O('감전이면 기절', [dmg(1.2), perTag(T1), dmg(0.2), ifFoe('충격'), st('기절', 1, E1)]),
      O('전자포 자동 장전', [dmg(0.9), perTag(T1), dmg(0.3), power(rule('make', [dmg(0.3, ER)], { limit: 2 }))], { power: true }),
      O('과열 사격', [disc(1), dmg(1.1), perTag(T1), dmg(0.45)]),
    ], [B('전자포 냉각', 'power'), B('나노 기계', 'ap'), B('시제품 하나 더', [make(T1, 1)])]),
    // u5 굴리기(공격) — 거대 드론이 장비를 사출: 시험 가동 + 시제품
    card(H, 5, '장비 사출', 1, '공격', [dmg(0.8), stk(K, 1), make(T1, 1)], [
      O('신탁 1', [dmg(1.05), stk(K, 1), make(T1, 1)]),
      O('신탁 2', [dmg(0.6), stk(K, 1), make(T1, 1)], { cost: 0 }),
      O('신탁 3', [dmg(0.55, EA), make(T1, 2)]),                                       // 재설계 — 광역 사출
      O('신탁 4', [dmg(1.05), make(T1, 1), srchU]),                                                  // F 서치 — BEST
      O('신탁 5', [payHp(0.05), dmg(1.5), stk(K, 2)]),                                             // H HP · 생성 빼기
    ], [B('축복 1', 'power'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '아멜리아_R41_u3');
}

// ════════════════════════════════════════════════════════════════════
// 9. 아이시아 — 딜러 · 냉정. 악덕 CEO 행세가 번번이 「선행 장부」에 적힌다 — 다섯이면 기부천사, 그 전에 해고야!로 다 털지
// 3단계: 생성 카드 「신상 냉장고」(신제품 시연이 남기는 0코 광역 투척 — 장부 1) · ④ 정복, 멜루나! = 장부를 세는 1코 마무리(엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function eisia(j) {
  const H = '아이시아', K = '선행 장부', F = '아이시아_fridge';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '본의 아니게 쌓인 선행 기록', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1 }],
    rules: [{ name: '기부천사 아이시아', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), heal(0.9), sh(1.0)] }],
  };
  delete h.keywords;
  h.passives = [
    P('의도치 않은 선행', { on: 'play' }, [stk(K, 1)], { limit: TL(3) }),
    P('냉혹한 경영', { on: 'kill', mine: true }, [stk(K, 2)], { limit: TL(1) }),
  ];
  token(j, H, F, '신상 냉장고', '공격', [dmg(0.35, EA), stk(K, 1)]);
  const x4 = r => ({ k: 'dmg', ratio: r, hits: 4, target: EA });
  const cards = [
    // u1 굴리기 — 원작 저학년 신제품 시연(냉장고 4회 광역) — 신상 냉장고를 남긴다
    card(H, 1, '신제품 시연', 1, '공격', [x4(0.2), make(F, 1)], [
      O('대형 냉장고', [x4(0.25), make(F, 1)]),
      O('아이스박스 폭발', [x4(0.3), make(F, 2), st('기절', 1, E1)], { cost: 2 }),
      O('재고 처리', [x4(0.2), perTag(F), dmg(0.25, EA), exileAll(F)]),
      O('신제품 카탈로그', [x4(0.18), make(F, 1), srchU]),
      O('반품 처리', [disc(1), x4(0.27), make(F, 2)]),
    ], [B('액체 질소', 'power'), B('우주식량', 'draw'), B('신상 하나 더', [make(F, 1)])]),
    // u2 열기 — 내가 누군지 몰라?(시동 카드)
    card(H, 2, '내가 누군지 몰라?', 0, '스킬', [stk(K, 2), draw(1)], [
      O('회장님 등장', [stk(K, 3), draw(1)]),
      O('주주 총회', [stk(K, 1), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('인사 기록 검색', [stk(K, 2), srch('공격'), draw(1)]),
      O('신제품 발표', [make(F, 2), ifStack(K, 3), draw(1)]),
      O('갑질 회의', [disc(1), stk(K, 3), draw(2)]),
    ], [B('유급 휴가', { tags: ['보존'] }), B('법무팀 방패', [sh(0.4)]), B('장부 한 줄', [stk(K, 1)])]),
    // u3 터뜨리기 — 해고야!(장부 전부)
    card(H, 3, '해고야!', 1, '공격', [dmg(0.8), per(K), dmg(0.2), spendAll(K)], [
      O('즉시 해고', [dmg(1.0), per(K), dmg(0.26), spendAll(K)]),
      O('구두 해고', [dmg(0.6), per(K), dmg(0.16), spendAll(K)], { cost: 0 }),
      O('냉동 창고행', [dmg(0.8), perTag(F), dmg(0.3), exileAll(F)]),
      O('상시 구조 조정', [dmg(0.8), per(K), dmg(0.2), power(rule('stackReach', [dmg(0.8)], reach(K, 5)))], { power: true }),
      O('전원 해고', [dmg(1.8), per(K), dmg(0.4), draw(1)], { tags: ['소멸'] }),
    ], [B('해고 통지서', 'weakSpot'), B('퇴직금 정산', 'ap'), B('찬바람', 'frost')]),
    // u4 완성형 — 정복, 멜루나!: 쥐고 있는 장부를 센다(쓰지 않음) — 1코 마무리
    card(H, 4, '정복, 멜루나!', 1, '공격', [dmg(0.7), per(K), dmg(0.2)], [
      O('131전 끝의 승리', [dmg(0.9), per(K), dmg(0.25)]),
      O('승리 선언', [dmg(0.45), per(K), dmg(0.14)], { cost: 0 }),
      O('멜론 혐오', [dmg(1.0), ifKill, stk(K, 2)]),
      O('정복 계획서', [stk(K, 2), power(rule('turnEnd', [per(K), dmg(0.12, ER)]))], { power: true }),
      O('정복 자금 탕진', [dmg(0.8), per(K), dmg(0.3), spendAll(K)]),
    ], [B('프로스트 노바', 'atkUp'), B('경영 효율화', 'cost'), B('선행 기록', [stk(K, 1)])]),
    // u5 유틸(서치 · 생성) — 아이스크림 수레로 시작: 실드 + 냉장고 + 공격 서치
    card(H, 5, '아이스크림 수레', 1, '스킬', [sh(0.8), make(F, 1), srch('공격')], [
      O('신탁 1', [sh(1.05), make(F, 1), srch('공격')]),
      O('신탁 2', [sh(0.65), make(F, 1), srch('공격')], { cost: 0 }),
      O('신탁 3', [sh(1.65), make(F, 1), costDownHand('공격')]),                            // 재설계 — 서치 대신 비용↓
      O('신탁 4', [sh(0.6), make(F, 1), power(rule('turnStart', [make(F, 1)]))], { power: true }), // D 매 턴 냉장고 — BEST
      O('신탁 5', [disc(1), sh(1.1), make(F, 2)]),                                   // H 손패 버리기
    ], [B('축복 1', 'defUp'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '아이시아_u2');
}

// ════════════════════════════════════════════════════════════════════
// 10. 알레트 — 탱커 · 순수 · 1성. 「반장님」(지정 아군) 앞을 막고, 반장님이 움직일 때마다 「명령 수행」 — 셋이면 반격, 그 전에 방패로 밀어붙여 다 쓸지
// 3단계: ④ 자율 훈련 = 명령을 세는 1코 마무리(실드 — 엔진은 D 갈래) · 「기본기 훈련」 갈래가 버린 시작 카드를 다시 손으로(기본 카드 연료)
// ════════════════════════════════════════════════════════════════════
function allet(j) {
  const H = '알레트', K = '명령 수행', M = '반장님';
  const h = j.heroes[0];
  h.keyword = { name: M, desc: '알레트가 따르는 아군 표시', carrier: 'hero', cap: 1, hunt: true };
  h.keywords = [{
    name: K, desc: '반장님 명령으로 쌓는 임무', carrier: 'self', cap: 3,
    rules: [{ name: '명령 완수', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), st('반격', 2), sh(1.0)] }],
  }];
  h.passives = [
    P('보고드립니다!', { on: 'fightStart' }, [mark(M, 1, 'hero:칸나')]),
    P('반장님 명령이라면', { on: 'play', marked: M }, [sh(0.5), stk(K, 1)], { limit: TL(3) }),
  ];
  const cards = [
    // u1 굴리기 — 원작 저학년 삽치기(기절) · 2코 → 1코
    card(H, 1, '삽치기', 1, '공격', [ddef(0.7), stk(K, 1)], [
      O('힘찬 삽질', [ddef(0.9), stk(K, 1)]),
      O('삽 끝으로', [ddef(0.48), stk(K, 1)], { cost: 0 }),
      O('방패로 막고 삽', [sh(1.3), stk(K, 1)]),
      O('삽 찾아오기', [ddef(0.6), stk(K, 1), srch('스킬')]),
      O('삽자루 부러뜨리기', [disc(1), ddef(1.0), stk(K, 2)]),
    ], [B('군용 삽', 'power'), B('몰래 배달', 'cost'), B('방패 받침', [sh(0.4)])]),
    // u2 열기 — 막내 하사 돌격: 반장님 지정 + 실드(시동 카드)
    card(H, 2, '막내 하사 돌격', 1, '스킬', [mark(M, 1, 'oneAlly'), sh(0.8), stk(K, 1)], [
      O('경례!', [mark(M, 1, 'oneAlly'), sh(1.1), stk(K, 1)]),
      O('충성 맹세', [mark(M, 1, 'oneAlly'), sh(0.8), power(rule('turnStart', [stk(K, 1), sh(0.4)]))], { power: true }),
      O('진압반 소집', [mark(M, 1, 'oneAlly'), sh(0.9), draw(2, { who: 'other' })]),
      O('반장님 호위', [mark(M, 1, 'oneAlly'), heal(0.6), st('반격', 1)]),
      O('군장 정리', [disc(1), mark(M, 1, 'oneAlly'), sh(2.2)]),
    ], [B('흐트러짐 없는 자세', 'guard'), B('제 이름은 알레트', 'draw'), B('경례 대기', [stk(K, 1)])]),
    // u3 터뜨리기 — 방패로 밀어붙이기(명령 전부)
    card(H, 3, '방패로 밀어붙이기', 1, '공격', [ddef(0.5), per(K), ddef(0.3), spendAll(K)], [
      O('전력 돌파', [ddef(0.65), per(K), ddef(0.38), spendAll(K)]),
      O('방패 툭', [ddef(0.35), per(K), ddef(0.2), spendAll(K)], { cost: 0 }),
      O('진압 대형', [ddef(0.8, EA), stk(K, 1)]),
      O('방패 벽', [ddef(0.5), per(K), ddef(0.25), power(rule('stackReach', [ddef(0.6)], reach(K, 3)))], { power: true }),
      O('진압 돌격', [disc(1), ddef(0.7), per(K), ddef(0.4)]),
    ], [B('큰 방패', 'weakSpot'), B('기상 나팔', 'ap'), B('밀어붙인 자리', [stk(K, 1)])]),
    // u4 완성형 — 자율 훈련: 쥐고 있는 명령만큼 실드(쓰지 않음) — 1코 마무리
    card(H, 4, '자율 훈련', 1, '스킬', [sh(0.7), per(K), sh(0.3)], [
      O('야간 자율 훈련', [sh(0.9), per(K), sh(0.38)]),
      O('짧은 훈련', [sh(0.5), per(K), sh(0.2)], { cost: 0 }),
      O('기본기 훈련', [pullBasic, sh(0.6), stk(K, 1)]),
      O('자율 훈련 일과', [st('결의', 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('훈련 일지 제출', [per(K), sh(0.42), spendAll(K), st('결의', 1)]),
    ], [B('군기', 'defUp'), B('훈련 교본', { tags: ['보존'] }), B('훈련 일지', [stk(K, 1)])]),
    // u5 유틸(드로우) — 반장님 몫 주스를 몰래 빼돌려 우주식량을 말아 먹는다: 실드 + 명령 + 드로우
    card(H, 5, '반장님 주스 빼돌리기', 1, '스킬', [sh(1.0), stk(K, 1), draw(1)], [
      O('신탁 1', [sh(1.3), stk(K, 1), draw(1)]),
      O('신탁 2', [sh(0.8), stk(K, 1), draw(1)], { cost: 0 }),
      O('신탁 3', [mark(M, 1, 'oneAlly'), sh(1.2), st('반격', 1)]),                           // 재설계 — 반장님을 옮기고 반격
      O('신탁 4', [sh(0.9), stk(K, 1), pullU]),                                               // F 회수 — BEST
      O('신탁 5', [disc(1), sh(1.2), stk(K, 3)]),                                                   // H 손패 버리기
    ], [B('축복 1', 'draw'), B('축복 2', 'ap'), B('축복 3', [mark(M, 1, 'oneAlly'), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '알레트_u2');
}

// ════════════════════════════════════════════════════════════════════
// 11. 엘레나 — 딜러 · 냉정. 카드마다 「냥이 드론」 출격 — 띄워 두면 턴 끝마다 쏘고, 자폭 기능으로 한꺼번에 터뜨릴 수도
// 3단계: 시동 = 개전 강화(냥이 드론 출격 — 매 턴 드론) · ④ 엘프답게! = 드론을 세는 1코 마무리
// ════════════════════════════════════════════════════════════════════
function elena(j) {
  const H = '엘레나', K = '냥이 드론';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '자폭 기능이 달린 고양이 드론', carrier: 'self', cap: 3, per: [{ stat: 'dot', ratio: 0.6 }] };
  delete h.keywords;
  h.passives = [
    P('드론 출격', { on: 'play' }, [stk(K, 1)], { limit: TL(3) }),
    P('엘프의 자존심', { on: 'fightStart' }, [stk(K, 1)]),
  ];
  ultRef(h, '드론', K);
  const cards = [
    // u1 굴리기 — 원작 저학년 전술 드론 MK-2(펄스파 광역 + 감전) · 2코 → 1코
    card(H, 1, '전술 드론 MK-2', 1, '공격', [dmg(0.9, EA), stk(K, 1)], [
      O('펄스파 증폭', [dmg(1.0, EA), stk(K, 1)]),
      O('코드 기능 개선', [dmg(1.35, EA), stk(K, 2), draw(1)], { cost: 2 }),
      O('드론 편대', [dmg(0.68, EA), per(K), dmg(0.16, EA)]),
      O('아멜리아 호출', [dmg(0.72, EA), stk(K, 1), draw(1, { who: 'other' })]),
      O('과부하 펄스', [disc(1), dmg(1.15, EA), stk(K, 1)]),
    ], [B('펄스 출력', 'power'), B('커피 수혈', 'draw'), B('예비 드론', [stk(K, 1)])]),
    // u2 열기(시동 — 개전 강화) — 냥이 드론 출격: 매 턴 드론
    card(H, 2, '냥이 드론 출격', 1, '강화', [stk(K, 1), power(rule('turnStart', [stk(K, 1)]))], [
      O('냥이 두 마리', [stk(K, 2), power(rule('turnStart', [stk(K, 1)]))], OP),
      O('즉석 출격', [power(rule('turnStart', [stk(K, 1)]))], opc({ cost: 0 })),
      O('출격 신호탄', [dmg(0.5, EA), power(rule('turnStart', [stk(K, 1)]))], OP),
      O('발명품 서랍', [stk(K, 1), srch('공격'), power(rule('turnStart', [stk(K, 1)]))], OP),
      O('쉬는 날 출격', [stk(K, 2), draw(1), power(rule('turnStart', [stk(K, 1)]))]),
    ], [B('작업대 방패', [sh(0.4)]), B('랩코트 주머니', 'cost'), B('드론 정비', [stk(K, 1)])], OP),
    // u3 터뜨리기 — 쓸데없는 자폭 기능(드론 전부) · 3코 → 1코
    card(H, 3, '쓸데없는 자폭 기능', 1, '공격', [dmg(0.6, EA), per(K), dmg(0.35, EA), spendAll(K)], [
      O('대형 자폭', [dmg(0.66, EA), per(K), dmg(0.38, EA), spendAll(K)]),
      O('소형 자폭', [dmg(0.38, EA), per(K), dmg(0.22, EA), spendAll(K)], { cost: 0 }),
      O('자폭 취소', [dmg(0.45, EA), per(K), dmg(0.25, EA), draw(1)]),
      O('자폭 프로토콜', [dmg(0.6, EA), per(K), dmg(0.3, EA), power(rule('stackReach', [per(K), dmg(0.25, EA), spendAll(K)], reach(K, 3)))], { power: true }),
      O('연쇄 자폭', [dmg(1.2, EA), per(K), dmg(0.6, EA), draw(1)], { tags: ['소멸'] }),
    ], [B('버터 바른 토스트', 'weakSpot'), B('연구동 두 채', 'ap'), B('영구동력', 'frost')]),
    // u4 완성형 — 엘프답게!: 띄워 둔 드론을 센다(쓰지 않음) — 1코 마무리
    card(H, 4, '엘프답게!', 1, '공격', [dmg(0.6), per(K), dmg(0.3)], [
      O('엘프의 긍지', [dmg(0.75), per(K), dmg(0.38)]),
      O('즉석 기술명', [dmg(0.4), per(K), dmg(0.2)], { cost: 0 }),
      O('60년 막내의 공구함', [dmg(0.65), stk(K, 1), srch('스킬')]),
      O('시장 연설', [dmg(0.6), per(K), dmg(0.3), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('3초 이상은 못 참아', [dmg(0.6), per(K), dmg(0.4, EA), spendAll(K)]),
    ], [B('기계마법 공학', 'atkUp'), B('지름길', 'cost'), B('드론 한 대 더', [stk(K, 1)])]),
    // u5 유틸(서치) — 아메리카노 주말농장 에디션(물 한 샷 · 나머지 에스프레소): 실드 + 드론 + 공격 서치
    card(H, 5, '아메리카노 주말농장 에디션', 1, '스킬', [sh(0.75), stk(K, 1), srch('공격')], [
      O('신탁 1', [sh(1.0), stk(K, 1), srch('공격')]),
      O('신탁 2', [sh(0.6), stk(K, 1), srch('공격')], { cost: 0 }),
      O('신탁 3', [costDownHand('공격'), sh(1.0), stk(K, 2)]),                                     // 재설계 — 버그는 버그로(비용↓)
      O('신탁 4', [sh(0.75), stk(K, 1), pullAtk]),                                            // F 버린 공격 회수 — BEST
      O('신탁 5', [payHp(0.05), sh(0.9), stk(K, 3)]),                                              // H HP · 서치 빼기
    ], [B('축복 1', 'defUp'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '엘레나_u2');
}

// ════════════════════════════════════════════════════════════════════
// 12. 오르 — 서포터(축: 실드) · 우울. 어떤 카드든 소멸하면 「고물 부품」 — 셋이면 발명품, 소멸이 잦을수록 공방이 돈다
// 3단계: 시작 카드를 고물로(「고물상 순회」 · 「고물 재조립」 — 기본 카드 연료) · ④ 필요는 발명의 어머니 = 부품을 세는 1코 마무리(실드 — 엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function orr(j) {
  const H = '오르', K = '고물 부품', T = '오르_t1';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '사라진 카드에서 주운 부품', carrier: 'self', cap: 6,
    rules: [{ name: '발명품 조립', when: { on: 'stackReach', id: K, n: 3 }, fx: [{ k: 'spend', id: K, v: 3 }, make(T, 1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('고물 줍기', { on: 'exhaust', who: 'any' }, [stk(K, 1)], { limit: TL(3) }),
    P('응원의 녹음기', { on: 'fightStart' }, [stk(K, 3)]),
  ];
  ultRef(h, '자재', K);
  Object.assign(j.cards.find(c => c.id === T), { cost: 0, type: '스킬', tags: ['소멸'], choices: ['휴대용 돔', '2인용 로켓'], fx: [{ k: 'ifChoice', n: 1 }, sh(1.4), { k: 'ifChoice', n: 2 }, dmg(1.6)] });
  const cards = [
    // u1 굴리기 — 원작 저학년 강제 기상 장치(소음 = 약화 담당 · 시간이 다하면 폭발 — 예약은 기본형에만)
    card(H, 1, '강제 기상 장치', 1, '스킬', [st('약화', 2, E1), later(1, [dmg(0.8, ER)])], [
      O('강제 기상 2호', [st('약화', 2, E1), dmg(0.95, ER)]),
      O('알람 시계', [st('약화', 2, E1)], { cost: 0 }),
      O('부품 장착 기상기', [st('약화', 2, E1), ifStack(K, 3), dmg(1.15, ER)]),
      O('설계 메모', [st('약화', 2, E1), dmg(0.5, ER), srchU]),
      O('부품 재활용', [st('약화', 2, E1), spendN(K, 2), make(T, 1)]),
    ], [B('집중 방해', 'frost'), B('늘어난 메리야스', 'cost'), B('나사 하나', [stk(K, 1)])]),
    // u2 열기 — 2인용 로켓: 발명품 하나(시동 카드)
    card(H, 2, '2인용 로켓', 1, '스킬', [make(T, 1), draw(1)], [
      O('로켓 두 대', [make(T, 2), draw(1)]),
      O('로켓 정비소', [make(T, 1), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('설계도 뒤적이기', [make(T, 1), draw(2, { who: 'other' })]),
      O('고물상 순회', [exBasic, stk(K, 2), make(T, 1)]),
      O('리어카 과적', [disc(1), make(T, 2), draw(1)]),
    ], [B('비행선 빵주', 'draw'), B('지원 물자', 'ap'), B('부품 한 봉지', [stk(K, 1)])]),
    // u3 버티기 — 이동식 자기장(원작 고학년 자기장)
    card(H, 3, '이동식 자기장', 1, '스킬', [sh(1.3), stk(K, 1)], [
      O('자기장 확장', [sh(1.7), stk(K, 1)]),
      O('휴대용 돔 전개', [sh(2.5), stk(K, 2), st('반격', 1)], { cost: 2 }),
      O('부품이 넉넉하면', [sh(1.2), ifStack(K, 4), sh(0.8)]),
      O('자기장 설계도', [sh(1.15), stk(K, 1), srch('스킬')]),
      O('부품 태우기', [burn(1), sh(1.5), stk(K, 3)]),
    ], [B('기계팔', 'guard'), B('작업복', 'defUp'), B('응원 녹음', [heal(0.4)])]),
    // u4 완성형 — 필요는 발명의 어머니: 쥐고 있는 부품만큼 실드(쓰지 않음) — 1코 마무리
    card(H, 4, '필요는 발명의 어머니', 1, '스킬', [sh(0.6), per(K), sh(0.2)], [
      O('밤샘 발명', [sh(0.75), per(K), sh(0.25)]),
      O('작은 발명', [sh(0.4), per(K), sh(0.14)], { cost: 0 }),
      O('고물 재조립', [exBasic, stk(K, 2), sh(0.8)]),
      O('발명 공방', [make(T, 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('부품 몽땅 투입', [per(K), sh(0.35), spendAll(K), make(T, 1)]),
    ], [B('오르나르도 엘빈치', 'guard'), B('자재 창고', 'cost'), B('부품 상자', [stk(K, 1)])]),
    // u5 굴리기(공격) — 화나면 팔을 번쩍 들고 크앙: 부품 + 피해(스킬만 넷이라 공격 하나)
    card(H, 5, '기계팔 크앙', 1, '공격', [dmg(0.85), stk(K, 1)], [
      O('신탁 1', [dmg(1.1), stk(K, 1)]),
      O('신탁 2', [dmg(0.65), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [st('약화', 1, E1), sh(1.4), stk(K, 1)]),                                        // 재설계 — 헛소리 차단 마스크(막기)
      O('신탁 4', [dmg(0.7), stk(K, 1), srchU]),                                                    // F 서치 — BEST
      O('신탁 5', [exBasic, dmg(1.1), stk(K, 2)]),                                                  // H 시작 카드를 고물로(기본 카드 연료)
    ], [B('축복 1', 'power'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '오르_u2');
}

// ════════════════════════════════════════════════════════════════════
// 13. 이드 — 탱커 · 냉정 · 엘다인. 맞을 때마다 나타가 「나타 경보」를 세고, 다섯이면 꿈의 파동 — 엘다인 한 단계: 흐릿한 경계(쓰러짐 1회 무효)
// 3단계: ④ 착한 마음씨 이드 = 경보를 세는 1코 마무리(실드 — 결정화 엔진은 D 갈래로). 2단계 혼자 35.2%(엘다인 상한 위) → 결정화 상시를 갈래로 빼서 낮춤
// ════════════════════════════════════════════════════════════════════
function ed(j) {
  const H = '이드', K = '나타 경보';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '나타가 세는 꿈속의 충격', carrier: 'self', cap: 5,
    rules: [{ name: '꿈의 파동', when: { on: 'stackReach', id: K, n: 5 }, fx: [spendAll(K), ddef(0.4, EA), st('약화', 1, EA)] }],
  };
  delete h.keywords;
  h.passives = [
    P('함께 꾸는 꿈', { on: 'hurt', guarded: true }, [stk(K, 1)], { limit: TL(2) }),
    // 원작 엘다인 패시브 「꿈에서 깨지 않았다」 — 웨이브 시작 무적(전투 시작 한 번)
    P('깨지 않은 꿈', { on: 'fightStart' }, [st('피해 감소', 1), stk(K, 1)]),
  ];
  const cards = [
    // u1 굴리기 — 나타에게 먼저(실드)
    card(H, 1, '나타에게 먼저', 1, '스킬', [sh(1.4), stk(K, 1)], [
      O('전뇌 통신', [sh(1.8), stk(K, 1)]),
      O('나타의 대답', [sh(0.95), stk(K, 1)], { cost: 0 }),
      O('꿈 이야기', [sh(1.3), ifStack(K, 3), draw(1)]),
      O('나타의 기록', [sh(1.25), stk(K, 1), srchU]),
      O('리소스 정리', [disc(1), sh(1.8), stk(K, 2)]),
    ], [B('생명유지장치', 'guard'), B('LLEL-S', 'draw'), B('먼저 건 말', [stk(K, 1)])]),
    // u2 열기 — 전기로 지져 줘(반격 + 경보 — 시동 카드) · 2코 → 1코
    card(H, 2, '전기로 지져 줘', 1, '스킬', [st('반격', 1), stk(K, 1), draw(1)], [
      O('기절할 때까지', [st('반격', 2), stk(K, 1), draw(1)]),
      O('나타의 거부', [st('반격', 1), draw(1), power(rule('turnStart', [stk(K, 1), sh(0.3)]))], { power: true }),
      O('잠든 척', [st('반격', 1), stk(K, 1), draw(2, { who: 'self', type: '스킬' })]),
      O('찌릿한 잠꼬대', [st('반격', 1), stk(K, 2), sh(0.6)]),
      O('꿈속의 전류', [disc(1), st('반격', 2), draw(2)]),
    ], [B('감전 반사', [sh(0.4)]), B('겨울잠 직전', 'cost'), B('전뇌 신호', [stk(K, 1)])]),
    // u3 엘다인 한 단계 — 원작 저학년 흐릿한 경계(쓰러질 일격 무효 = 끈기) · 3코 → 2코
    card(H, 3, '흐릿한 경계', 2, '스킬', [sh(2.1), st('끈기', 1)], [
      O('세이빙', [sh(2.6), st('끈기', 1)]),
      O('얕은 경계', [sh(1.5), st('끈기', 1)], { cost: 1 }),
      O('꿈과 현실 사이', [ddef(0.9, EA), st('끈기', 1), stk(K, 2)]),
      O('경보 연동 탐색', [sh(1.9), st('끈기', 1), srchU]),
      O('경보 소모 방벽', [per(K), sh(0.95), spendAll(K), st('끈기', 1)]),
    ], [B('나타의 격벽', 'guard'), B('외롭지 않은 꿈', { tags: ['보존'] }), B('동료의 온기', 'defUp')]),
    // u4 완성형 — 착한 마음씨 이드: 쥐고 있는 경보만큼 실드(쓰지 않음) — 1코 마무리
    card(H, 4, '착한 마음씨 이드', 1, '스킬', [sh(0.7), per(K), sh(0.2)], [
      O('함께라면', [sh(0.85), per(K), sh(0.25)]),
      O('겨울잠 이불', [sh(0.45), per(K), sh(0.14)], { cost: 0 }),
      O('언니 생각', [sh(0.6), stk(K, 1), draw(1)]),
      O('눈 감은 방패', [st('결정화', 1), power(rule('stackReach', [sh(1.2)], reach(K, 5)))], { power: true }),
      O('이드 더 이터널 불릿', [per(K), ddef(0.35), spendAll(K)]),
    ], [B('포근한 격벽', 'defUp'), B('잠결의 다짐', 'ap'), B('잠결 경보', [stk(K, 1)])]),
    // u5 굴리기(공격) — 화나면 나타로 들이받는다: 경보 + 방어 기반 피해(스킬만 넷이라 공격 하나)
    card(H, 5, '나타로 들이받기', 1, '공격', [ddef(0.75), stk(K, 1)], [
      O('신탁 1', [ddef(0.98), stk(K, 1)]),
      O('신탁 2', [ddef(0.6), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [ddef(0.5, EA), st('약화', 1, E1)]),                                             // 재설계 — 커피 컵 걷어차기(광역)
      O('신탁 4', [ddef(0.6), stk(K, 1), power(rule('hurt', [ddef(0.3)], { when: { guarded: true }, limit: 1 }))], { power: true }),   // D 막을 때마다 들이받음 — BEST
      O('신탁 5', [per(K), ddef(0.4), spendAll(K), sh(0.6)]),                                      // H 경보를 다 쓴다
    ], [B('축복 1', 'power'), B('축복 2', 'cost'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '이드_u2');
}

// ════════════════════════════════════════════════════════════════════
// 14. 이드(재활) — 탱커 · 순수(원작 방식 고학년 「잠든 꿈」 유지). 맞을 때 쌓인 「악몽」을 정면으로 마주해(소모) 실드로
// ════════════════════════════════════════════════════════════════════
function edRehab(j) {
  const H = '이드_재활', K = '악몽';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '깨면 잊는 밤의 악몽', carrier: 'self', cap: 5, per: [{ stat: 'guard', v: 0.1 }] };
  // 「잠든 꿈」 · 「꿈에 쌓인 아픔」(원작 방식 고학년)은 그대로 둔다
  h.passives = [
    P('악몽 마주하기', { on: 'hurt' }, [stk(K, 1)], { limit: TL(3) }),
    P('정면으로 마주하기', { on: 'play', type: '스킬' }, [per(K), sh(0.6), spendAll(K), st('둔화', 1, EA)], { conds: [{ c: 'stack', id: K, n: 2 }], limit: TL(1) }),
  ];
  const cards = [
    // u1 굴리기 — 원작 저학년 뚜렷한 경계(광역 + 꿈의 흔적 회복)
    card(H, 1, '뚜렷한 경계', 1, '공격', [ddef(0.55, EA), heal(0.6), stk(K, 1)], [
      O('선명한 경계', [ddef(0.62, EA), heal(0.65), stk(K, 1)]),
      O('환장의 짝꿍', [ddef(0.8, EA), heal(0.8), st('기절', 1, E1)], { cost: 2 }),
      O('악몽의 경계', [ddef(0.45, EA), heal(0.5), ifStack(K, 3), ddef(0.3, EA)]),
      O('경계 기록', [ddef(0.55, EA), heal(0.55), srchU]),
      O('꿈의 흔적', [disc(1), ddef(0.75, EA), heal(0.75)]),
    ], [B('꿈의 경계', 'power'), B('보조기', 'draw'), B('꿈속 중얼거림', [stk(K, 1)])]),
    // u2 굴리기 — 원작 강화 평타 꿈의 파장(SP 감소 = 둔화)
    card(H, 2, '꿈의 파장', 1, '공격', [ddef(0.7), st('둔화', 1, E1)], [
      O('선명한 파장', [ddef(0.92), st('둔화', 1, E1)]),
      O('작은 파장', [ddef(0.48), st('둔화', 1, E1)], { cost: 0 }),
      O('악몽 공명', [ddef(0.75), ifStack(K, 3), ddef(0.5)]),
      O('파장 상시 발산', [ddef(0.5), power(rule('hurt', [stk(K, 1)], { limit: 1 }))], { power: true }),
      O('악몽 털어내기', [per(K), ddef(0.25), spendAll(K), ddef(0.5)]),
    ], [B('흐트러진 박자', 'frost'), B('산책길', 'cost'), B('한 걸음 더', [stk(K, 1)])]),
    // u3 열기 — 잠든 동안: 악몽 + 실드 + 드로우(시동 카드) · 2코 → 1코
    card(H, 3, '잠든 동안', 1, '스킬', [stk(K, 2), sh(1.0), draw(1)], [
      O('깊은 잠', [stk(K, 3), sh(0.95), draw(1)]),
      O('산책 겸 재활', [sh(0.8), draw(1), power(rule('turnStart', [stk(K, 1), sh(0.4)]))], { power: true }),
      O('뮤트에게 들려줄 이야기', [stk(K, 2), sh(0.8), draw(2, { who: 'self', type: '스킬' })]),
      O('꿈 일기', [heal(1.0), stk(K, 2), sh(1.3)]),
      O('잠꼬대', [disc(1), stk(K, 3), draw(3)]),
    ], [B('꿈틀이 쿠션', 'cost'), B('자매들의 선물', { tags: ['보존'] }), B('뮤트의 손길', [heal(0.4)])]),
    // u4 터뜨리기 — 소중한 순간: 악몽을 마주해 다 쓰고 실드 + 적 전체 둔화
    card(H, 4, '소중한 순간', 1, '스킬', [per(K), sh(0.35), spendAll(K), st('둔화', 1, EA)], [
      O('엘생 네컷', [per(K), sh(0.45), spendAll(K), st('둔화', 1, EA)]),
      O('찰칵', [per(K), sh(0.26), spendAll(K)], { cost: 0 }),
      O('스스로 마주하기', [sh(0.8), st('둔화', 1, EA), power(rule('turnEnd', [per(K), sh(0.15)]))], { power: true }),
      O('악몽 간직', [per(K), sh(0.3), st('둔화', 1, EA), draw(1)]),
      O('모두의 보조기', [disc(1), per(K), sh(0.5), heal(0.7)]),
    ], [B('사진 부스', 'guard'), B('네컷 한 장 더', 'ap'), B('악몽 한 조각', [stk(K, 1)])]),
    // u5 유틸(동료 카드) — 혼자 걷기 힘들면 같이 걸어 달라고: 실드 + 악몽 + 동료 카드
    card(H, 5, '같이 걷는 산책', 1, '스킬', [sh(0.9), stk(K, 1), draw(1, { who: 'other' })], [
      O('신탁 1', [sh(1.2), stk(K, 1), draw(1, { who: 'other' })]),
      O('신탁 2', [sh(0.7), stk(K, 1), draw(1, { who: 'other' })], { cost: 0 }),
      O('신탁 3', [heal(1.2), sh(0.8), stk(K, 1)]),                                                          // 재설계 — 뮤트의 꿀밤 치료
      O('신탁 4', [sh(0.9), stk(K, 1), pullU]),                                            // F 회수 — BEST
      O('신탁 5', [disc(1), sh(1.0), stk(K, 3)]),                                                   // H 손패 버리기
    ], [B('축복 1', 'guard'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '이드_재활_u3');
}

// ════════════════════════════════════════════════════════════════════
// 15. 칸나 — 딜러 · 활발. 큰 한 방(비용 2 이상 공격) · 아군의 격파마다 「반려 도장」 — 셋이면 휴가 신청서 반려(고학년 게이지)
// 3단계: 2코 시동(큰 거 한 방 = 비싼 한 방이 축) — ①의 여는 일은 0코 서치 「명 받았습니다!」 · 생성 카드 「특수 포탄」 · ④ = 손의 포탄을 세는 1코 마무리(휴가 신청서 엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function canna(j) {
  const H = '칸나', K = '반려 도장', S = '칸나_shell';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '휴가 신청서마다 찍히는 반려 도장', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.12 }],
    rules: [{ name: '휴가 신청서 반려', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), { k: 'gauge', v: 25 }, draw(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('즉시 출동', { on: 'play', type: '공격', minCost: 2 }, [stk(K, 1)]),
    P('양자폭탄 결재', { on: 'break' }, [stk(K, 1)], { limit: TL(1) }),
  ];
  token(j, H, S, '특수 포탄', '공격', [dmg(0.75), stk(K, 1)]);
  const cards = [
    // u1 큰 한 방(2코 시동) — 원작 저학년 큰 거 한 방(특수 포탄) · 3코 → 2코
    card(H, 1, '큰 거 한 방', 2, '공격', [dmg(2.2), make(S, 1)], [
      O('대구경 포탄', [dmg(2.75), make(S, 1)]),
      O('작은 포탄', [dmg(1.5), make(S, 1)], { cost: 1 }),
      O('격파 확인 사격', [dmg(2.3), ifFoe('broken'), dmg(1.2)]),
      O('탄약고 확인', [dmg(1.85), make(S, 1), srchU]),
      O('도장째 발사', [dmg(1.8), per(K), dmg(0.45), spendAll(K)]),
    ], [B('고성능 포신', 'power'), B('출동 대기', 'ap'), B('포탄 하나 더', [make(S, 1)])]),
    // u2 열기(0코 서치 — 시동이 2코라 여는 일은 이 카드) — 명 받았습니다!: 도장 + 공격 카드 서치
    card(H, 2, '명 받았습니다!', 0, '스킬', [stk(K, 1), srch('공격')], [
      O('복창!', [stk(K, 2), srch('공격')]),
      O('상명하복', [draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('출동 준비', [make(S, 1), costDownHand('공격')]),
      O('규칙대로', [stk(K, 1), srch('공격'), inspire, stk(K, 2)]),
      O('휴가 반납', [disc(1), stk(K, 2), draw(2, { who: 'self', type: '공격' })]),
    ], [B('반묶음 포니테일', 'draw'), B('레몬차 거절', { tags: ['보존'] }), B('도장 쾅', [stk(K, 1)])]),
    // u3 굴리기 — 오락실 패왕(펀칭머신) · 2코 → 1코
    card(H, 3, '오락실 패왕', 1, '공격', [dmg(1.3), stk(K, 1)], [
      O('펀칭머신 신기록', [dmg(1.7), stk(K, 1)]),
      O('로데오 완주', [dmg(2.3), stk(K, 1), tough(1)], { cost: 2 }),
      O('격파 콤보', [dmg(1.1), ifFoe('broken'), dmg(0.9)]),
      O('다음 판 대기', [dmg(1.0), stk(K, 1), srch('공격')]),
      O('동전 탕진', [disc(1), dmg(1.5), stk(K, 1)]),
    ], [B('펀치 글러브', 'weakSpot'), B('한 판 더', 'cost'), B('오락실 동전', 'frost')]),
    // u4 완성형 — 포탄 일제 사격: 손의 특수 포탄을 센다 — 1코 마무리
    card(H, 4, '포탄 일제 사격', 1, '공격', [dmg(0.75), perTag(S), dmg(0.55), exileAll(S)], [
      O('포탄 전부 장전', [dmg(0.9), perTag(S), dmg(0.65), exileAll(S)]),
      O('한 발만', [dmg(0.4), perTag(S), dmg(0.3), exileAll(S)], { cost: 0 }),
      O('도장 찍고 발사', [dmg(1.0), per(K), dmg(0.45)]),
      O('휴가 신청서', [dmg(0.6), perTag(S), dmg(0.45), power(rule('ult', [st('피해 감소', 2), draw(1)]))], { power: true }),
      O('탄약 낭비', [disc(1), dmg(0.8), perTag(S), dmg(0.55)]),
    ], [B('충격 포탄', 'power'), B('휴가 계획표', 'draw'), B('결재 도장 쾅', [stk(K, 1)])]),
    // u5 유틸(회수) — 실수엔 「시정하겠슴다」: 도장 + 버린 공격 카드 회수(큰 한 방을 다시)
    card(H, 5, '시정 보고', 0, '스킬', [stk(K, 1), pullAtk], [
      O('신탁 1', [stk(K, 2), pullAtk]),
      O('신탁 2', [stk(K, 1), pullAtk, make(S, 1)]),
      O('신탁 3', [make(S, 1), draw(1, { who: 'other' })]),                                         // 재설계 — 부하 식단 관리(동료 카드 + 포탄)
      O('신탁 4', [stk(K, 1), pullAtk, power(rule('stackReach', [pullAtk], reach(K, 3)))], { power: true }),   // D 반려마다 회수 — BEST
      O('신탁 5', [disc(1), stk(K, 2), pullAtk]),                                                   // H 손패 버리기
    ], [B('축복 1', 'cost'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '칸나_u1');
}

// ════════════════════════════════════════════════════════════════════
// 16. 캐시 — 딜러 · 순수. 겁먹을 때마다 「히익 전류」가 찬다 — 셋이면 과충전, 그 전에 바삭 지짐이로 다 지질지
// 3단계: 동료 연계(채우기) — 동료가 공격하면 놀라서 전류(턴당 1) · ④ 301일의 동굴 = 전류를 세는 1코 마무리(엔진은 D 갈래). 2단계 28.5%(딜러 +5.4)
// ════════════════════════════════════════════════════════════════════
function kathy(j) {
  const H = '캐시', K = '히익 전류';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '겁먹을 때마다 차오르는 전기', carrier: 'self', cap: 3,
    rules: [{ name: '과충전', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), st('약화', 1, ER)] }],
  };
  delete h.keywords;
  h.passives = [
    P('히익!', { on: 'play', who: 'other', type: '공격' }, [stk(K, 1)], { limit: TL(1) }),
    P('유령 잡는 엘프', { on: 'kill', mine: true }, [stk(K, 1)], { limit: FL(2) }),
  ];
  ultRef(h, '충전', K);
  const cards = [
    // u1 터뜨리기 — 원작 저학년 바삭 지짐이(연쇄 테이저) · 2코 → 1코
    card(H, 1, '바삭 지짐이', 1, '공격', [dmg(0.55), per(K), dmg(0.24), spendAll(K)], [
      O('풀파워 지짐이', [dmg(0.87), per(K), dmg(0.37), spendAll(K)]),
      O('찌릿', [dmg(0.48), per(K), dmg(0.22), spendAll(K)], { cost: 0 }),
      O('정전기 충전', [dmg(0.7), stk(K, 2), draw(1)]),
      O('상시 테이저', [dmg(0.7), per(K), dmg(0.3), power(rule('stackReach', [dmg(0.6)], reach(K, 3)))], { power: true }),
      O('바삭바삭바삭', [disc(1), dmg(0.9), per(K), dmg(0.38)]),
    ], [B('테이저 건', 'power'), B('정전기', 'ap'), B('찌릿 충전', [stk(K, 1)])]),
    // u2 열기 — 더플백 은신(회피 + 전류 — 시동 카드)
    card(H, 2, '더플백 은신', 1, '스킬', [sh(0.75), stk(K, 1), draw(1)], [
      O('가방 깊숙이', [sh(0.95), stk(K, 2), draw(1)]),
      O('노숙 동굴', [sh(0.75), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('무전 호출', [sh(0.75), stk(K, 1), draw(2, { who: 'self', type: '공격' })]),
      O('깜짝 반격', [sh(0.75), ifStack(K, 2), dmg(0.95)]),
      O('누, 누구야!', [disc(1), st('회피', 1), draw(2)]),
    ], [B('해골 가면', 'draw'), B('하얀 분칠', 'cost'), B('긴 귀 숨기기', [sh(0.4)])]),
    // u3 굴리기 — 불가사의한 현상 · 2코 → 1코
    card(H, 3, '불가사의한 현상', 1, '공격', [dmg(0.78), stk(K, 1)], [
      O('공포의 유령', [dmg(1.26), stk(K, 1)]),
      O('작은 현상', [dmg(0.65), stk(K, 1)], { cost: 0 }),
      O('비명의 메아리', [dmg(0.78), per(K), dmg(0.24)]),
      O('유령 수사 수첩', [dmg(0.85), stk(K, 1), srchU]),
      O('심령 사진 소각', [dmg(2.4), stk(K, 1), st('약화', 1, E1)], { tags: ['소멸'] }),
    ], [B('으스스한 기운', 'weakSpot'), B('비명 한 번', 'draw'), B('겁먹은 손', 'frost')]),
    // u4 완성형 — 301일의 동굴: 쥐고 있는 전류를 센다(쓰지 않음) — 1코 마무리
    card(H, 4, '301일의 동굴', 1, '공격', [dmg(0.45), per(K), dmg(0.18)], [
      O('동굴 생활 달인', [dmg(0.75), per(K), dmg(0.38)]),
      O('동굴 입구', [dmg(0.4), per(K), dmg(0.2)], { cost: 0 }),
      O('동굴 메아리', [dmg(0.7, EA), stk(K, 1)]),
      O('동굴 생활', [dmg(0.6), per(K), dmg(0.3), power(rule('hurt', [stk(K, 1)], { limit: 1 }))], { power: true }),
      O('동굴 탈출', [per(K), dmg(0.35), spendAll(K), dmg(0.6)]),
    ], [B('내면의 초자아', 'atkUp'), B('숨기 명수', 'cost'), B('겁 많은 책임감', [stk(K, 1)])]),
    // u5 유틸(서치) — 다크넷에서 찾은 「무서워 보이는 꿀팁」: 실드 + 전류 + 공격 서치
    card(H, 5, '무서워 보이는 꿀팁', 1, '스킬', [sh(0.8), stk(K, 1), srch('공격')], [
      O('신탁 1', [sh(1.05), stk(K, 1), srch('공격')]),
      O('신탁 2', [sh(0.65), stk(K, 1), srch('공격')], { cost: 0 }),
      O('신탁 3', [sh(1.5), stk(K, 1), costDownOther]),                                         // 재설계 — 이멀전씨!(동료 카드 비용↓)
      O('신탁 4', [sh(0.85), stk(K, 1), power(rule('play', [stk(K, 1)], { when: { who: 'other', type: '공격' }, limit: 1 }))], { power: true }),   // D — BEST
      O('신탁 5', [disc(1), sh(1.4), stk(K, 3)]),                                    // H 손패 버리기
    ], [B('축복 1', 'frost'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '캐시_u2');
}

// ════════════════════════════════════════════════════════════════════
// 17. 타이다 — 딜러 · 활발 · 1성. 일을 떠넘기고(버리기) 「땡땡이」를 모아 DX - 슈터 한 방에 쏟는다
// 3단계: 생성 카드 「밀린 업무」(떠넘기면 땡땡이 2 — 안식) · 「근무 일지」 갈래가 시작 카드를 찾아온다(기본 카드 연료) · ④ 유령 늪 출장 = 땡땡이를 세는 1코 마무리(엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function taida(j) {
  const H = '타이다', K = '땡땡이', C = '타이다_chore';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '일 대신 아껴 둔 힘', carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.15 }] };
  delete h.keywords;
  h.passives = [
    P('떠넘긴 일', { on: 'discard' }, [stk(K, 1), { k: 'nextCheaper', v: 1 }], { limit: TL(2) }),
    P('짱박힐 시간', { on: 'turnStart' }, [stk(K, 1)]),
  ];
  token(j, H, C, '밀린 업무', '스킬', [draw(1), onDisc, stk(K, 2)]);
  const cards = [
    // u1 터뜨리기 — 원작 저학년 DX - 슈터(강력한 탄환) · 땡땡이 전부
    card(H, 1, 'DX - 슈터', 2, '공격', [dmg(2.3), per(K), dmg(0.45), spendAll(K)], [
      O('정조준', [dmg(2.6), per(K), dmg(0.5), spendAll(K)]),
      O('퇴근 직전 한 발', [dmg(1.55), per(K), dmg(0.3), spendAll(K)], { cost: 1 }),
      O('야근 수당 확정', [dmg(2.3), per(K), dmg(0.45), ifKill, stk(K, 3)]),
      O('상시 저격 대기', [dmg(2.0), per(K), dmg(0.4), power(rule('stackReach', [dmg(0.9)], reach(K, 4)))], { power: true }),
      O('관통탄', [disc(1), dmg(2.6), per(K), dmg(0.5)]),
    ], [B('DX 탄창', 'power'), B('퇴근 시간', 'ap'), B('한숨 돌리기', [stk(K, 1)])]),
    // u2 열기 — 일 떠넘기기(버리고 뽑기 + 밀린 업무 — 시동 카드)
    card(H, 2, '일 떠넘기기', 1, '스킬', [disc(1), draw(2), make(C, 1)], [
      O('몽땅 떠넘기기', [disc(2), draw(3), make(C, 1)]),
      O('상습 농땡이', [draw(2), stk(K, 2), power(rule('turnEnd', [stk(K, 1)]))], { power: true }),
      O('동료에게 훈수', [disc(1), draw(3, { who: 'other' }), make(C, 1)]),
      O('업무 분배', [make(C, 2), stk(K, 1)]),
      O('사표 쓰기', [draw(3), make(C, 2), stk(K, 2)], { tags: ['소멸'] }),
    ], [B('방문 기록 삭제', 'draw'), B('훈수 두기', 'cost'), B('밀린 업무 하나 더', [make(C, 1)])]),
    // u3 굴리기 — 야근 수당 사격: 버려지면 땡땡이 둘(안식) · 2코 → 1코
    card(H, 3, '야근 수당 사격', 1, '공격', [dmg(1.3), onDisc, stk(K, 2)], [
      O('수당 두 배', [dmg(1.5), onDisc, stk(K, 2)]),
      O('근무 중 슬쩍', [dmg(0.75), onDisc, stk(K, 2)], { cost: 0 }),
      O('상관 눈 피하기', [dmg(1.0), stk(K, 1), ifStack(K, 3), dmg(0.6)]),
      O('근무 일지', [dmg(1.05), draw(1, { basic: true }), onDisc, stk(K, 2)]),
      O('난사', [disc(1), dmg(1.6), onDisc, stk(K, 2)]),
    ], [B('수당 계산기', 'weakSpot'), B('비상벨', 'frost'), B('땡땡이 한 번', [stk(K, 1)])]),
    // u4 완성형 — 유령 늪 출장: 쥐고 있는 땡땡이를 센다(쓰지 않음) — 1코 마무리
    card(H, 4, '유령 늪 출장', 1, '공격', [dmg(0.75), per(K), dmg(0.35)], [
      O('출장비 청구', [dmg(0.75), per(K), dmg(0.38)]),
      O('당일치기 출장', [dmg(0.4), per(K), dmg(0.2)], { cost: 0 }),
      O('출장 보고서', [disc(1), draw(3), stk(K, 3)]),
      O('늪지 출장 상주', [dmg(0.6), per(K), dmg(0.3), power(rule('discard', [dmg(0.45, ER)], { limit: 2 }))], { power: true }),
      O('땡땡이 출장', [dmg(0.6), per(K), dmg(0.3), make(C, 1)]),
    ], [B('출장 가방', 'atkUp'), B('법인 카드', 'cost'), B('늪지 장화', 'draw')]),
    // u5 유틸(동료 카드 + 버리기) — 상관 서랍을 슬쩍: 동료 카드 하나 가져와 하나 떠넘긴다
    card(H, 5, '상관 서랍 털이', 0, '스킬', [draw(1, { who: 'other' }), disc(1)], [
      O('신탁 1', [draw(2, { who: 'other' }), disc(1)]),
      O('신탁 2', [sh(0.8), stk(K, 1)]),                                                            // 재설계 — 진흙탕 구르기
      O('신탁 3', [draw(1, { who: 'other' }), disc(1), ifStack(K, 3), draw(1)]),                   // E 땡땡이 조건
      O('신탁 4', [srchU, disc(1), make(C, 1)]),                                                    // F 고유 서치 + 밀린 업무 — BEST
      O('신탁 5', [payHp(0.05), draw(2), disc(2)]),                                                // H HP
    ], [B('축복 1', 'ap'), B('축복 2', [sh(0.4)]), B('축복 3', [stk(K, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '타이다_u2');
}

// ════════════════════════════════════════════════════════════════════
// 18. 페스타 — 서포터(축: 버퍼) · 우울. 파티가 공격 · 스킬을 번갈아 낼 때마다 「반항」 — 넷이면 소음 공해, 그 전에 락 앤 피스!로 다 쏟을지. 협공 담당
// ④ 갓 오브 뮤직!은 강화 그대로(원작 어사이드 「역조공 이벤트!」 — 모든 아군 피해↑ 상시)
// ════════════════════════════════════════════════════════════════════
function festa(j) {
  const H = '페스타', K = '반항';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '공격과 스킬을 뒤섞는 엇박자', carrier: 'self', cap: 4,
    rules: [{ name: '소음 공해', when: { on: 'stackReach', id: K, n: 4 }, fx: [spendAll(K), st('약화', 1, EA), dispel(1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('나락도 락', { on: 'play', who: 'any', seq: ['공격', '스킬'] }, [stk(K, 1)]),
    P('나락도 락', { on: 'play', who: 'any', seq: ['스킬', '공격'] }, [stk(K, 1)]),
    P('역조공 이벤트!', { on: 'fightStart' }, [stk(K, 1)]),
  ];
  const god = (...more) => power(rule('stackReach', [st('협공', 1), ...more], reach(K, 4)));
  const cards = [
    // u1 터뜨리기 — 원작 저학년 락 앤 피스!(소음 + 버프 해제) · 3코 → 1코
    card(H, 1, '락 앤 피스!', 1, '공격', [dmg(0.5, EA), per(K), dmg(0.2, EA), spendAll(K)], [
      O('앰프 최대', [dmg(0.65, EA), per(K), dmg(0.25, EA), spendAll(K)]),
      O('앙코르 없는 공연', [dmg(0.95, EA), per(K), dmg(0.38, EA), st('협공', 1)], { cost: 2 }),
      O('기세 꺾기', [dmg(0.5, EA), dispel(1), ifStack(K, 3), dmg(0.4, EA)]),
      O('세트리스트', [dmg(0.5, EA), per(K), dmg(0.2, EA), draw(1, { who: 'other' })]),
      O('기타 부수기', [dmg(1.1, EA), per(K), dmg(0.45, EA), spendAll(K)], { tags: ['소멸'] }),
    ], [B('한정판 앰프', 'power'), B('기타 케이스', 'draw'), B('엇박자', [stk(K, 1)])]),
    // u2 버티기 — 노숙 락커(회복) · 2코 → 1코
    card(H, 2, '노숙 락커', 1, '스킬', [heal(0.95), stk(K, 1)], [
      O('역 앞 노숙', [heal(1.25), stk(K, 1)]),
      O('깡통 한 모금', [heal(0.65), stk(K, 1)], { cost: 0 }),
      O('깡통 모금', [heal(0.9), st('협공', 1)]),
      O('배달 끼니', [heal(0.9), stk(K, 1), draw(1, { who: 'other' })]),
      O('부자인데 궁핍', [disc(1), heal(1.3), stk(K, 2)]),
    ], [B('브랜디', 'heal'), B('침낭', 'cost'), B('자유의 노래', [stk(K, 1)])]),
    // u3 열기 — 버스킹: 반항 + 협공(시동 카드)
    card(H, 3, '버스킹', 1, '스킬', [stk(K, 2), st('협공', 1)], [
      O('길거리 대공연', [stk(K, 2), st('협공', 2)]),
      O('매일 버스킹', [stk(K, 2), st('협공', 1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('관객 호응', [stk(K, 2), st('협공', 1), draw(1, { who: 'other' })]),
      O('거리 공연 수익', [heal(1.2), st('협공', 1), ifStack(K, 3), draw(1)]),
      O('흥이 오르면', [disc(1), stk(K, 3), st('협공', 2)]),
    ], [B('즉흥 연주', 'cost'), B('앰프 연결', 'ap'), B('떼창 유도', [st('협공', 1)])]),
    // u4 완성형(강화 그대로) — 갓 오브 뮤직!: 소음 공해마다 모두 함께(협공)
    card(H, 4, '갓 오브 뮤직!', 1, '강화', [stk(K, 1), god()], [
      O('한정판 기타', [stk(K, 1), god(dmg(0.35, EA))]),
      O('무대 뒤 조율', [god()], { cost: 0 }),
      O('역조공 이벤트', [heal(0.4), draw(1), god()]),
      O('앨범 재발매', [stk(K, 2), draw(1, { who: 'other' }), god()]),
      O('목 쉬도록', [payHp(0.05), stk(K, 3), god(dmg(0.2, EA))]),
    ], [B('스포트라이트', 'atkUp'), B('저작권료', 'draw'), B('떼창', [stk(K, 1)])]),
    // u5 굴리기(공격) — 일부러 기괴하게 연주: 반항 + 광역
    card(H, 5, '기괴한 연주', 1, '공격', [dmg(0.7, EA), stk(K, 1)], [
      O('신탁 1', [dmg(0.88, EA), stk(K, 1)]),
      O('신탁 2', [dmg(1.25, EA), stk(K, 2), st('협공', 1)], { cost: 2 }),                         // 비용↑ = 협공 + 반항 2
      O('신탁 3', [st('협공', 1), sh(1.1)]),                                                       // 재설계 — 심사 거부(막기)
      O('신탁 4', [dmg(0.55, EA), stk(K, 1), power(rule('stackReach', [dmg(0.3, EA)], reach(K, 4)))], { power: true }),   // D — BEST
      O('신탁 5', [disc(1), dmg(0.95, EA), stk(K, 2)]),                                            // H 손패 버리기
    ], [B('축복 1', 'power'), B('축복 2', 'ap'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '페스타_u3');
}

// ════════════════════════════════════════════════════════════════════
// 19. 하이디 — 딜러 · 광기(원작 방식 고학년 「초 집중 취재」 유지). 「특종감」으로 찍은 적에게 셔터를 몰아치고, 상자 속 「잠복」으로 한 컷을 노린다
// 3단계: 동료 연계(채우기) — 동료가 특종감을 치면 잠복(턴당 2) · ④ 셀카 한 장 = 잠복을 세는 1코 마무리(엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function heidi(j) {
  const H = '하이디', K = '특종감', L = '잠복', T = '하이디_t1';
  const h = j.heroes[0];
  // 고유 효과 셋(특종감 · 잠복 · 초 집중 취재)은 이름 · 꼴 그대로 — 초 집중 취재(원작 방식)의 규칙이 잠복을 쌓는다
  h.passives = [
    P('목표 포착!', { on: 'fightStart' }, [mark(K, 1, 'topEnemy')]),
    P('목표 포착!', { on: 'hit', who: 'other' }, [ifStack(K, 1), stk(L, 1)], { limit: TL(1) }),
    P('특종 셀카', { on: 'huntDown', id: K }, [make(T, 1), mark(K, 1, 'topEnemy')]),
  ];
  Object.assign(j.cards.find(c => c.id === T), { cost: 0, type: '스킬', tags: ['소멸'], fx: [draw(1), stk(L, 2)] });
  const cards = [
    // u1 열기 — 원작 저학년 잠입 취재 중!(상자 속 잠복 · 눈속임) · 2코 → 1코 (시동 카드)
    card(H, 1, '잠입 취재 중!', 1, '스킬', [stk(L, 2), st('회피', 1)], [
      O('상자 속 잠입', [stk(L, 3), st('회피', 1)]),
      O('장기 잠입', [stk(L, 1), st('회피', 1), power(rule('turnStart', [stk(L, 1)]))], { power: true }),
      O('제보 검색', [stk(L, 2), st('회피', 1), srch('공격')]),
      O('위장 취재', [mark(K, 1, 'topEnemy'), make(T, 2)]),
      O('상자째 돌진', [disc(1), stk(L, 3), st('회피', 1)]),
    ], [B('프리 기자 패스', 'draw'), B('골판지 상자', 'cost'), B('숨죽이기', [stk(L, 1)])]),
    // u2 굴리기 — 눈가림 플래시(약화)
    card(H, 2, '눈가림 플래시', 1, '공격', [dmg(1.0), st('약화', 1, E1)], [
      O('풀 플래시', [dmg(1.3), st('약화', 1, E1)]),
      O('찰칵', [dmg(0.7), st('약화', 1, E1)], { cost: 0 }),
      O('특종의 순간', [dmg(1.0), ifStack(K, 1), dmg(0.65)]),
      O('사진첩 뒤지기', [dmg(0.9), st('약화', 1, E1), srchU]),
      O('플래시 과열', [disc(1), dmg(1.35), st('약화', 1, E1)]),
    ], [B('대형 플래시', 'power'), B('셔터 연사', 'ap'), B('눈부심', 'frost')]),
    // u3 굴리기 — 특종감 지정(옮겨 찍고 친다) · 2코 → 1코
    card(H, 3, '특종감 지정', 1, '공격', [mark(K, 1), dmg(0.9)], [
      O('1면 특종', [mark(K, 1), dmg(1.35)]),
      O('속보', [mark(K, 1), dmg(2.2), stk(L, 1)], { cost: 2 }),
      O('이미 특종감이면', [dmg(1.0), ifStack(K, 1), dmg(0.8)]),
      O('취재 수첩', [mark(K, 1), dmg(0.9), srchU]),
      O('독점 보도', [disc(1), mark(K, 1), dmg(1.35)]),
    ], [B('망원 렌즈', 'weakSpot'), B('자극적인 제목', 'draw'), B('잠복 준비', [stk(L, 1)])]),
    // u4 완성형 — 셀카 한 장: 쌓아 둔 잠복을 센다 — 1코 마무리
    card(H, 4, '셀카 한 장', 1, '공격', [dmg(0.5), per(L), dmg(0.16)], [
      O('셀카 각도 완벽', [dmg(0.75), per(L), dmg(0.25)]),
      O('대충 한 장', [dmg(0.4), per(L), dmg(0.14)], { cost: 0 }),
      O('아핫!', [make(T, 1), mark(K, 1), dmg(0.5)]),
      O('셀카 연재', [make(T, 1), power(rule('turnStart', [stk(L, 1)]))], { power: true }),
      O('블로그 리뷰', [disc(1), dmg(0.9), per(L), dmg(0.3)]),
    ], [B('셀카봉', 'atkUp'), B('식당 평점', 'cost'), B('한 장 더', [make(T, 1)])]),
    // u5 유틸(서치) — 단서 몇 개로 거대한 음모론을 완성: 실드 + 잠복 + 공격 서치
    card(H, 5, '음모론 완성', 1, '스킬', [sh(0.7), stk(L, 2), srch('공격')], [
      O('신탁 1', [sh(0.9), stk(L, 3), srch('공격')]),
      O('신탁 2', [sh(0.55), stk(L, 2), srch('공격')], { cost: 0 }),
      O('신탁 3', [make(T, 1), mark(K, 1, 'topEnemy'), sh(0.7)]),                                            // 재설계 — 종군기자 시절(특종감 옮겨 찍기)
      O('신탁 4', [sh(0.8), stk(L, 2), pullAtk]),                                             // F 버린 공격 회수 — BEST
      O('신탁 5', [disc(1), sh(0.8), stk(L, 4)]),                                                   // H 손패 버리기
    ], [B('축복 1', 'defUp'), B('축복 2', 'ap'), B('축복 3', [stk(L, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '하이디_u1');
}

// ════════════════════════════════════════════════════════════════════
// 20. 헤일리 — 딜러 · 순수. 턴마다 적 하나를 「외계인」으로 착각해 그 적을 치면 한 발 더 — 채찍의 쓰라림(고통 담당)
// 3단계: 동료 연계(채우기) — 동료가 외계인을 치면 고통(턴당 2) · ④ 엘프군 최전선은 강화 그대로(원작 강화 평타 「채찍 정비」 상시)
// ════════════════════════════════════════════════════════════════════
function haley(j) {
  const H = '헤일리', K = '외계인';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '외계인이라 믿는 적 표시', carrier: 'enemy', cap: 1, hunt: true };
  delete h.keywords;
  h.passives = [
    P('착각 순찰', { on: 'turnStart' }, [mark(K, 1, ER)]),
    P('외계인 격퇴', { on: 'hit' }, [ifStack(K, 1), extra(0.7)], { limit: TL(3) }),
    P('외계인 격퇴', { on: 'hit', who: 'other' }, [ifStack(K, 1), st('고통', 1, E1)], { limit: TL(2) }),
  ];
  const x3 = r => ({ k: 'dmg', ratio: r, hits: 3, target: EA });
  const front = (v = 1) => power(rule('hit', [st('고통', v, E1)], { limit: 2 }));
  const cards = [
    // u1 굴리기 — 원작 저학년 논 그라타(채찍 3번 + 쓰라림) · 2코 → 1코
    card(H, 1, '논 그라타', 1, '공격', [x3(0.25), st('고통', 2, EA)], [
      O('채찍 세 번', [x3(0.32), st('고통', 2, EA)]),
      O('쓰라림 폭발', [x3(0.43), st('고통', 3, EA), draw(1)], { cost: 2 }),
      O('디버프 감별', [x3(0.25), perDebuff, dmg(0.2)]),
      O('채찍 정비 교범', [x3(0.22), st('고통', 2, EA), srchU]),
      O('채찍 집중', [disc(1), hits(3, 0.45, E1), st('고통', 3, E1)]),
    ], [B('가죽 채찍', 'power'), B('작전 지도', 'draw'), B('채찍 정비', [st('고통', 1, EA)])]),
    // u2 굴리기 — 플랜 A: 정면 돌파(외계인을 옮겨 찍는다)
    card(H, 2, '플랜 A: 정면 돌파', 1, '공격', [dmg(1.2), mark(K, 1)], [
      O('돌격 앞으로', [dmg(1.4), mark(K, 1)]),
      O('척후', [dmg(0.75), mark(K, 1)], { cost: 0 }),
      O('외계인 확인 사살', [dmg(1.0), ifStack(K, 1), dmg(0.75)]),
      O('동맹국 지원', [dmg(0.95), mark(K, 1), draw(1, { who: 'other' })]),
      O('플랜 B', [disc(1), dmg(1.6), mark(K, 1)]),
    ], [B('장교의 기백', 'weakSpot'), B('군사 기밀', 'cost'), B('외계인 발견', [mark(K, 1)])]),
    // u3 열기 — 우주 전함 헤일리(실드 + 외계인 지정 — 시동 카드)
    card(H, 3, '우주 전함 헤일리', 1, '스킬', [sh(0.8), mark(K, 1, 'topEnemy'), draw(1)], [
      O('전함 출항', [sh(1.1), mark(K, 1, 'topEnemy'), draw(1)]),
      O('상시 순찰', [mark(K, 1, 'topEnemy'), draw(1), power(rule('turnStart', [sh(0.6)]))], { power: true }),
      O('작전 선포', [sh(0.8), mark(K, 1, 'topEnemy'), draw(2, { who: 'self', type: '공격' })]),
      O('함포 사격', [dmg(0.6, EA), mark(K, 1, 'topEnemy'), st('고통', 1, EA)]),
      O('경례 대기', [disc(1), sh(1.2), draw(2)]),
    ], [B('함장석', 'guard'), B('제복', 'ap'), B('전함 레이더', [mark(K, 1, 'topEnemy')])]),
    // u4 완성형(강화 그대로) — 엘프군 최전선: 칠 때마다 그 적에게 고통
    card(H, 4, '엘프군 최전선', 1, '강화', [mark(K, 1, 'topEnemy'), front()], [
      O('최전선 사수', [mark(K, 1, 'topEnemy'), front(2)]),
      O('순찰 출발', [front()], { cost: 0 }),
      O('재판에 회부', [draw(1), dmg(0.6), front()]),
      O('외계인 격퇴 작전', [mark(K, 1, 'topEnemy'), srch('공격'), front()]),
      O('전군 돌격', [payHp(0.05), mark(K, 1, 'topEnemy'), front(2)]),
    ], [B('훈장', 'atkUp'), B('군사 용어', 'cost'), B('쓰라린 기억', 'frost')]),
    // u5 유틸(동료 카드) — 주변에 멋대로 배역을 나눠 준다: 외계인 지정 + 동료 공격 카드
    card(H, 5, '배역 나눠 주기', 0, '스킬', [mark(K, 1, 'topEnemy'), draw(1, { who: 'other', type: '공격' })], [
      O('신탁 1', [mark(K, 1, 'topEnemy'), draw(2, { who: 'other', type: '공격' })]),
      O('신탁 2', [mark(K, 1, 'topEnemy'), draw(1, { who: 'other', type: '공격' }), sh(0.5)], { tags: ['보존'] }),
      O('신탁 3', [st('고통', 3, E1), sh(0.8)]),                                                   // 재설계 — 방범 순찰
      O('신탁 4', [mark(K, 1, 'topEnemy'), srch('공격'), draw(1, { who: 'other', type: '공격' })]),   // F 자신 + 동료 공격 — BEST
      O('신탁 5', [payHp(0.05), mark(K, 1, 'topEnemy'), draw(2, { who: 'other' })]),              // H HP
    ], [B('축복 1', 'ap'), B('축복 2', 'guard'), B('축복 3', [st('고통', 1, E1)])]),
  ];
  setUnique(j, cards);
  starter(j, '헤일리_u3');
}

// ════════════════════════════════════════════════════════════════════
// 21. 헤일리(멀쩡) — 탱커 · 광기. 「극복의 훈장」 — 쥐고 있으면 덜 아프고, 장교 검법에 실어 다 쓸 수도. 맞서 막아 낸 공격마다 하나
// 3단계: ④ 극복의 기록 = 훈장을 세는 1코 마무리(실드 — 엔진은 D 갈래)
// ════════════════════════════════════════════════════════════════════
function haleySane(j) {
  const H = '헤일리_멀쩡', K = '극복의 훈장';
  const h = j.heroes[0];
  h.keyword = { name: K, desc: '마주 본 과거의 훈장', carrier: 'self', cap: 4, per: [{ stat: 'taken', v: -0.05 }] };
  delete h.keywords;
  h.passives = [
    P('잊을 수 없는 기억', { on: 'fightStart' }, [stk(K, 2)]),
    P('다 막아 낸 전열', { on: 'blocked' }, [stk(K, 1)], { limit: TL(1) }),
  ];
  const cards = [
    // u1 굴리기 — 원작 저학년 나아가는 결의(지면 강타 + 자신 회복 + 훈장) · 2코 → 1코
    card(H, 1, '나아가는 결의', 1, '공격', [ddef(0.45, EA), heal(0.5), stk(K, 1)], [
      O('지면 강타', [ddef(0.6, EA), heal(0.6), stk(K, 1)]),
      O('지면 붕괴', [ddef(0.9, EA), heal(0.9), stk(K, 2)], { cost: 2 }),
      O('훈장의 무게', [ddef(0.45, EA), heal(0.5), ifStack(K, 3), ddef(0.3, EA)]),
      O('전열 정비', [ddef(0.45, EA), heal(0.55), srchU]),
      O('단독 강타', [disc(1), ddef(0.85, EA), heal(0.65)]),
    ], [B('지팡이', 'power'), B('되찾은 언변', 'draw'), B('훈장 하나', [stk(K, 1)])]),
    // u2 터뜨리기 — 엘피니아 장교 검법(훈장 전부)
    card(H, 2, '엘피니아 장교 검법', 1, '공격', [ddef(0.5), per(K), ddef(0.2), spendAll(K)], [
      O('정예 검법', [ddef(0.65), per(K), ddef(0.26), spendAll(K)]),
      O('검집 치기', [ddef(0.35), per(K), ddef(0.14), spendAll(K)], { cost: 0 }),
      O('방어 검세', [ddef(0.6), sh(0.8), stk(K, 1)]),
      O('장교 검술 수련', [ddef(0.5), per(K), ddef(0.2), power(rule('blocked', [ddef(0.4)], { limit: 1 }))], { power: true }),
      O('장교의 결단', [disc(1), ddef(0.7), per(K), ddef(0.27)]),
    ], [B('장교 검', 'weakSpot'), B('검법 교범', 'cost'), B('검 끝 방어', [sh(0.5)])]),
    // u3 열기 — 이번엔 진짜 전장(실드 + 훈장 + 드로우 — 시동 카드) · 3코 → 1코
    card(H, 3, '이번엔 진짜 전장', 1, '스킬', [sh(0.8), stk(K, 1), draw(1)], [
      O('전술 재정비', [sh(1.0), stk(K, 2), draw(1)]),
      O('함장의 지휘', [sh(0.8), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('작전 지도 펼치기', [sh(0.8), stk(K, 1), draw(2, { who: 'self', type: '공격' })]),
      O('과거 마주하기', [cleanse(1), stk(K, 2), draw(1)]),
      O('흑역사 모니터', [disc(1), sh(1.1), draw(2)]),
    ], [B('빵주 함교', 'guard'), B('하게체 명령', 'ap'), B('훈장 수여', [stk(K, 1)])]),
    // u4 완성형 — 극복의 기록: 쥐고 있는 훈장만큼 실드(쓰지 않음) — 1코 마무리
    card(H, 4, '극복의 기록', 1, '스킬', [sh(0.7), per(K), sh(0.22)], [
      O('헤일리 다큐멘터리', [sh(0.85), per(K), sh(0.28)]),
      O('짧은 기록', [sh(0.45), per(K), sh(0.15)], { cost: 0 }),
      O('찬란한 한때', [cleanse(1), heal(0.6), stk(K, 1)]),
      O('기록 보관소', [st('결의', 1), power(rule('turnEnd', [per(K), sh(0.15)]))], { power: true }),
      O('기록 공개', [per(K), sh(0.38), spendAll(K), heal(0.5)]),
    ], [B('낯선 과거', 'defUp'), B('느긋한 산책', 'cost'), B('민트초코 사절', [stk(K, 1)])]),
    // u5 굴리기(0코) — 필요하면 무릎을 꿇는 교섭: 실드 + 훈장
    card(H, 5, '무릎 꿇는 교섭', 0, '스킬', [sh(0.75), stk(K, 1)], [
      O('신탁 1', [sh(0.98), stk(K, 1)]),
      O('신탁 2', [sh(0.75), stk(K, 1)], { tags: ['보존'] }),
      O('신탁 3', [ddef(0.55), stk(K, 1)]),                                                         // 재설계 — 기대를 배신하는 검(막기 ↔ 치기)
      O('신탁 4', [sh(0.6), stk(K, 1), srchU]),                                                     // F 서치 — BEST
      O('신탁 5', [per(K), heal(0.35), spendAll(K), draw(1)]),                                     // H 훈장을 다 쓴다
    ], [B('축복 1', 'guard'), B('축복 2', 'draw'), B('축복 3', [stk(K, 1), sh(0.3)])]),
  ];
  setUnique(j, cards);
  starter(j, '헤일리_멀쩡_u3');
}

// ════════════════════════════════════════════════════════════════════
// 22. 힐데 — 서포터(축: 드로우) · 우울. 카드를 낼 때마다 「진료 차트」 — 셋이면 회진(회복 + 진료 소견서), 그 전에 피톤치드 파동으로 차트째 크게 처방할지. 사기 담당(부상일 때)
// 3단계: 생성 카드 「진료 소견서」(0코 드로우 — 회진 · 정밀 회진이 남긴다)
// ════════════════════════════════════════════════════════════════════
function hilde(j) {
  const H = '힐데', K = '진료 차트', N = '힐데_note';
  const h = j.heroes[0];
  h.keyword = {
    name: K, desc: '진찰할 때마다 채우는 차트', carrier: 'self', cap: 3,
    rules: [{ name: '회진', when: { on: 'stackReach', id: K, n: 3 }, fx: [spendAll(K), heal(1.0), make(N, 1)] }],
  };
  delete h.keywords;
  h.passives = [
    P('진찰', { on: 'play' }, [stk(K, 1)], { limit: TL(3) }),
    P('더 센 병이 돌아야', { on: 'play', type: '스킬' }, [st('사기', 1)], { conds: [{ c: 'wounded' }], limit: FL(3) }),
  ];
  token(j, H, N, '진료 소견서', '스킬', [draw(1), heal(0.3)]);
  const cards = [
    // u1 터뜨리기 — 원작 저학년 피톤치드 파동(전원 회복) · 차트 전부 · 2코 → 1코
    card(H, 1, '피톤치드 파동', 1, '스킬', [heal(0.7), per(K), heal(0.25), spendAll(K)], [
      O('짙은 피톤치드', [heal(0.9), per(K), heal(0.32), spendAll(K)]),
      O('숲의 파동', [heal(1.45), per(K), heal(0.45), make(N, 1)], { cost: 2 }),
      O('무차별 처방', [dmg(0.5, EA), per(K), dmg(0.15, EA), spendAll(K)]),
      O('처방 기록 검색', [heal(0.62), per(K), heal(0.22), srchU]),
      O('매 초 회복', [disc(1), heal(1.0), per(K), heal(0.33)]),
    ], [B('온천의 효능', 'heal'), B('간이 처방', 'cost'), B('차트 한 장', [stk(K, 1)])]),
    // u2 열기 — 정밀 회진(차트 + 회복 + 드로우 — 시동 카드) · 2코 → 1코
    card(H, 2, '정밀 회진', 1, '스킬', [stk(K, 1), heal(0.6), draw(1)], [
      O('꼼꼼한 회진', [stk(K, 2), heal(0.7), draw(1)]),
      O('정기 검진', [heal(0.6), draw(1), power(rule('turnStart', [stk(K, 1)]))], { power: true }),
      O('진료 기록 검색', [stk(K, 1), heal(0.5), draw(2, { who: 'self', type: '스킬' })]),
      O('소견서 작성', [make(N, 2), stk(K, 1)]),
      O('진짜로 아픈 환자', [disc(1), heal(0.9), draw(2)]),
    ], [B('청진기', 'heal'), B('파티 게임', 'draw'), B('소견서 한 장', [make(N, 1)])]),
    // u3 완성형 — 의료인 보호법: 회진마다 실드(원작 어사이드 — 강화 공격에 보호막)
    card(H, 3, '의료인 보호법', 1, '강화', [sh(0.8), power(rule('stackReach', [sh(1.0)], reach(K, 3)))], [
      O('보호법 개정', [sh(1.0), power(rule('stackReach', [sh(1.35)], reach(K, 3)))]),
      O('보호법 초안', [power(rule('stackReach', [sh(1.0)], reach(K, 3)))], { cost: 0 }),
      O('닥터 호칭 복원', [make(N, 1), draw(1), power(rule('stackReach', [sh(1.0)], reach(K, 3)))]),
      O('판례 검색', [sh(0.7), srch('스킬'), power(rule('stackReach', [sh(1.0)], reach(K, 3)))]),
      O('야간 진료', [payHp(0.05), sh(1.0), power(rule('stackReach', [sh(1.3)], reach(K, 3)))]),
    ], [B('리뉴아의 창조주', 'defUp'), B('진료 시간 엄수', 'ap'), B('소독약', [heal(0.4)])]),
    // u4 굴리기 — 주사기총 난사(원작 기본 공격)
    card(H, 4, '주사기총 난사', 1, '공격', [hits(3, 0.42, ER), stk(K, 1)], [
      O('대량 처방', [hits(4, 0.36, ER), stk(K, 1)]),
      O('주사 한 대', [hits(2, 0.38, ER), stk(K, 1)], { cost: 0 }),
      O('처방 1장당', [hits(3, 0.36, ER), per(K), dmg(0.18, ER)]),
      O('상시 처방', [hits(3, 0.42, ER), stk(K, 1), power(rule('stackReach', [hits(2, 0.3, ER)], reach(K, 3)))], { power: true }),
      O('과잉진료', [disc(1), hits(4, 0.42, ER), stk(K, 1)]),
    ], [B('주사기총', 'power'), B('마취 주사', 'frost'), B('차트 기록', [stk(K, 1)])]),
    // u5 굴리기(공격) — 판이 기운 대국에 뒤집을 수를 둘: 차트 + 피해
    card(H, 5, '판을 뒤집는 수', 1, '공격', [dmg(0.85), stk(K, 1)], [
      O('신탁 1', [dmg(1.1), stk(K, 1)]),
      O('신탁 2', [dmg(0.65), stk(K, 1)], { cost: 0 }),
      O('신탁 3', [make(N, 1), draw(1, { who: 'other' })]),                                         // 재설계 — 지휘관과 전략 회의
      O('신탁 4', [dmg(0.7), stk(K, 1), power(rule('stackReach', [dmg(0.4, ER)], reach(K, 3)))], { power: true }),   // D 회진마다 한 수 — BEST
      O('신탁 5', [disc(1), dmg(1.1), stk(K, 2)]),                                                  // H 손패 버리기
    ], [B('축복 1', 'power'), B('축복 2', 'cost'), B('축복 3', [make(N, 1)])]),
  ];
  setUnique(j, cards);
  starter(j, '힐데_u2');
  // 애착 장비(전설) — 범용 몫: 부상에서 벗어나면 결의 1(전투당 2회) + HP 40% 이하면 회복(한 번), 애착 몫: 부상에서 벗어나면 진료 차트 2
  for (const e of j.equips || []) {
    e.effect = [P('회복 확인', { on: 'unwound' }, [st('결의', 1)], { limit: FL(2) }), P('응급 처치', { on: 'lowHp', pct: 0.4 }, [heal(1.0)])];
    e.affinityEffect = [P('처방 기록', { on: 'unwound' }, [stk(K, 2)], { limit: FL(2) })];
  }
}

// 마무리(2026-10-08) — 기준 밖 사도의 고유 · 생성 카드 피해 · 실드 · 회복 배율(혼자 완주율을 역할 평균 안으로)
function scaleM(j, m) {
  const mul = fx => { for (const f of fx || []) { if (['dmg', 'shield', 'heal'].includes(f.k) && f.ratio) f.ratio = Math.round(f.ratio * m * 100) / 100; if (f.k === 'power') for (const r of f.rules) mul(r.fx); } };
  for (const c of j.cards) { if (!c.unique && !c.token) continue; mul(c.fx); for (const o of c.oracles || []) mul(o.fx); for (const b of c.blesses || []) mul(b.fx); }
}
const SM = (fn, m) => j => { fn(j); scaleM(j, m); };
run([
  ['엘프/레이지', SM(lazy, 1.2)], ['엘프/로네', rohne], ['엘프/로네_시장', rohneMayor], ['엘프/리뉴아', renewa], ['엘프/리스티', risty],
  ['엘프/마에스트로2호', maestro], ['엘프/아멜리아', amelia], ['엘프/아멜리아_R41', ameliaR41], ['엘프/아이시아', eisia], ['엘프/알레트', allet],
  ['엘프/엘레나', elena], ['엘프/오르', orr], ['엘프/이드', ed], ['엘프/이드_재활', edRehab], ['엘프/칸나', canna],
  ['엘프/캐시', kathy], ['엘프/타이다', taida], ['엘프/페스타', festa], ['엘프/하이디', heidi], ['엘프/헤일리', haley],
  ['엘프/헤일리_멀쩡', haleySane], ['엘프/힐데', hilde],
], new URL('./boost_엘프.json', import.meta.url));
