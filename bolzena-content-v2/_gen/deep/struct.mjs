// 구조 손질(10-10 마지막) — 배율(tune)이 안 닿는 곳(고유 효과 per · onMax · rules · 기본 카드)을 직접 고친다.
// lib.run 이 묶음 스크립트 · 새 패시브 뒤, 배율 앞에 부른다. 원본을 그대로 두는 사도(시저)는 apply_struct.mjs 로 원본 파일에 바로.
const kw = (j, name) => [j.heroes[0].keyword, ...(j.heroes[0].keywords || [])].find(k => k && k.name === name);
const card = (j, name) => j.cards.find(c => !c.unique && !c.token && c.name === name);
const rule = (k, name) => (k.rules || []).find(r => r.name === name);
const S = (id, v, target) => ({ k: 'stack', id, v, ...(target ? { target } : {}) });
// 멈춤 방지 상한(30회/턴) 걸림을 막는 조건 — 동료 카드마다 도는 규칙은 코스트 1 이상만(생성 0코 카드 줄은 계기가 안 된다)
const MINCOST1 = (j, ...names) => { for (const p of j.heroes[0].passives || []) if (names.includes(p.name)) p.when.minCost = 1; };
export const STRUCT = {
  '정령/미로': j => MINCOST1(j, '좌우가 뒤집힌 응원'),
  '마녀/아사나': j => MINCOST1(j, '심마체'),
  '유령/셀리네': j => MINCOST1(j, '한밤의 신기루'),
  '용족/오팔': j => MINCOST1(j, '오팔을 칭찬합니다!'),
  // ── 낮은 쪽 ──
  '정령/뮤트': j => {   // 홀로그램 1당 15% 는 너무 얇다(실측 13 피해) — 0.6, 터질 때 셋만 쓴다(봇 문제 아님: basic · skilled 봇 모두 낮음)
    const k = kw(j, '홀로그램'); k.per[0].ratio = 1.0;
    const r = rule(k, '에러 메시지'); r.fx[0] = { k: 'spend', id: '홀로그램', v: 3 };
    card(j, '데이터 스트림').fx.push(S('홀로그램', 1, 'oneEnemy'));
    card(j, '나타의 복구 코드').fx[0].ratio = 1.8;
    const w = j.heroes[0].passives.find(x => x.name === '웹트래핑'); if (w) w.fx[0].v = 2;
  },
  '용족/시스트': j => { card(j, '가짜 날개').fx.push(S('수수료', 1)); },   // 수수료가 처치 · 격파에서만 — 기본 방어에도 웃돈
  '유령/앨리스': j => { card(j, '운명 회피').fx.push(S('행운', 1)); const k = kw(j, '행운'); k.cap = 5; k.per[0].v = 0.12; card(j, '카드 날리기').fx[0].ratio = 1.25; },
  '수인/스패럿': j => { card(j, '해적 권총').fx.push(S('노획물', 1)); kw(j, '노획물').per[0].ratio = 0.2; },
  '정령/우이_기억': j => { kw(j, '되찾은 기억').per[0].v = 0.08; },
  '용족/아네트': j => { card(j, '응원 도시락').fx.push(S('관중 함성', 1)); kw(j, '관중 함성').cap = 3; },
  '정령/시저': j => { rule(kw(j, '단종 예고'), '단종').fx[0].ratio = 3.0; card(j, '간식 나눠주기').fx[0].ratio = 1.8; },
  '수인/그윈': j => { kw(j, '깃발').per = [{ stat: 'taken', v: 0.06 }]; },   // 깃발이 꽂힌 적은 받는 피해 +6%/개
  '마녀/비비_신성': j => { kw(j, '새싹').onMax.ratio = 0.6; },
  '정령/오로라': j => { const k = kw(j, '오로라 장막'); for (const r of k.rules) r.fx[1].ratio = 0.45; },
  '마녀/셰럼': j => { rule(kw(j, '회한의 영역'), '되살아나는 흑역사').fx[1].ratio = 0.5; card(j, '깃펜 찌르기').fx[0].ratio = 0.7; card(j, '두꺼운 장부').fx[0].ratio = 2.0; },
  // ── 높은 쪽 ──
  '수인/코미': j => { kw(j, '낮잠').per[0].ratio = 0.03; },
  '수인/디아나_왕년': j => { kw(j, '연격').per[0].v = 0.04; card(j, '도끼 내려찍기').fx[0].ratio = 1.7; },
  '수인/슈로': j => { kw(j, '검의 영역').per[0].v = -0.04; },
  '요정/폴랑': j => { kw(j, '머스킷 장전').onMax.ratio = 0.3; },
  '엘프/하이디': j => { kw(j, '특종감').per[0].v = 0.08; kw(j, '잠복').per[0].v = 0.03; const p = j.heroes[0].passives.find(x => x.name === '특종 공유'); if (p) p.fx[0].ratio = 0.15; card(j, '망원 풀줌 샷').fx[0].ratio = 1.6; j.heroes[0].forms.find(f => f.id === '하이디_잠입').mods.taken = -0.1; },
  '용족/실비아': j => { MINCOST1(j, '어머니의 특별 강의'); card(j, '숙녀의 자세').fx[0].ratio = 1.2; j.heroes[0].ult.fx.find(f => f.k === 'shield').ratio = 1.5; const p = j.heroes[0].passives.find(x => x.name === '어머니의 특별 강의'); if (p) p.fx.find(f => f.k === 'shield').ratio = 0.06; kw(j, '은방울').onMax.ratio = 0.25; const u = j.heroes[0].ult; u.fx = u.fx.filter(f => !(f.k === 'status' && f.id === '기절')); },   // 엘다인 고학년 · 손님 실드가 몸통 — 고학년 실드 350% → 150%, 손님 카드마다 실드 20% → 6%
  '엘프/이드': j => { card(j, '나타 격벽').fx[0].ratio = 1.3; const p = j.heroes[0].passives.find(x => x.name === '꿈에서 깨지 않은'); if (p) p.fx = p.fx.filter(f => f.id !== '피해 감소'); },
  '용족/리츠': j => { kw(j, '정당방위').per[0].v = 0.04; card(j, '강철 자세').fx[0].ratio = 1.1; card(j, '칼등 치기').fx[0].ratio = 0.9;
    const fm = j.heroes[0].forms[0]; fm.mods.dealt = 0.1; j.cards.find(c => c.id === '리츠_f1').fx[0].ratio = 0.4;
    j.heroes[0].passives.find(x => x.name === '참는 자세').fx[0].v = -0.05; },   // 파티 받는 피해 -10% 상시가 몸통 → -5%
};
// 배율(tune) 뒤 손 보정 — 정수만 있는 신탁이라 배율 · boost 가 안 닿아 본판보다 약해진 것
export const POST = {
  '마녀/마카샤': j => { const c = j.cards.find(c => c.id === '마카샤_u1'); c.oracles[3].fx[0].v = 6; },
  '마녀/벨벳': j => { const c = j.cards.find(c => c.id === '벨벳_u1'); c.oracles[3].fx[1].rules[0].fx[0].v = 3; },
  '마녀/요미': j => { const c = j.cards.find(c => c.id === '요미_u1'); c.oracles[2].fx[0].v = 5; },
};
