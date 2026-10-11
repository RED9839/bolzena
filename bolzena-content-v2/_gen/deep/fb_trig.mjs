// 계기 넓히기(2026-10-10 사용자 보충) — 쌓는 계기를 「자신이 공격 카드를 내면」 한 곳에서 여러 갈래로 나눈다.
//  · 계기는 who any(나 포함)가 기본 — 다른 사도가 그 조건을 자주 · 싸게 만들어 주면(생산) 소비 사도가 크게 산다(파츠 구조).
//  · 소비 사도의 카드는 그 조건을 드물게 · 약하게 스스로 만든다(1~2장) — 혼자서도 돌지만 생산 사도와 짝이면 확실히 세다.
//  · 계기 이름(카제나): 감응(뽑힘) · 안식(버려짐) · 소각(소멸) · 연계(다른 아군이 냄) · 천상(비용 2 이상) · 연속(앞 카드와 성격 같음) · 붕괴(격파) · 파괴(처치).
const W = (on, x = {}) => ({ on, who: 'any', ...x });
const setWhen = (H, j, name, when, v) => H.P(j, name, r => { r.when = when; if (v != null) for (const f of r.fx) if (f.k === 'stack') f.v = v; });
export const FB_TRIG = {
  '마녀/롤렛': (j, H) => setWhen(H, j, '사라진 카드 마술', W('exhaust')),            // 소각 — 사라진 카드가 갈채로
  '마녀/포셔': (j, H) => setWhen(H, j, '약값은 칼같이', W('debuff'), 1),              // 디버프를 걸면 — 부작용 청구
  '마녀/프리클': (j, H) => setWhen(H, j, '문지기 소환', W('hit', { weak: true })),     // 약점으로 치면
  '수인/리온': (j, H) => setWhen(H, j, '판결봉', W('break'), 2),                      // 붕괴 — 격파가 곧 유죄 판결
  '용족/네티': (j, H) => setWhen(H, j, '드릴 굴착', W('kill'), 2),                    // 파괴 — 쓰러진 적에게서 발굴
  '수인/티그_영웅': (j, H) => setWhen(H, j, '두 번 베기', W('play', { chain: true })), // 연속 — 같은 성격이 이어지면
  '유령/시온더다크불릿': (j, H) => setWhen(H, j, '큭큭, 봉인 해제', W('play', { minCost: 2 })),   // 천상 — 큰 카드가 봉인을 푼다
  '정령/나이아': (j, H) => setWhen(H, j, '퓨퓨~', W('overheal')),                      // 넘친 회복이 물탱크로
  '엘프/리스티': (j, H) => setWhen(H, j, '글러브 해킹', W('drawn', { type: '공격' })),  // 감응(공격) — 뽑히는 공격 카드마다 개인정보
  '정령/잉클': (j, H) => setWhen(H, j, '획 긋기', W('discard')),                       // 안식 — 버린 종이에 먹빛
  '수인/에피카': (j, H) => setWhen(H, j, '에피콘 극단', W('play', { sameHero: true })), // 같은 사도의 카드가 잇달면
  '요정/캬롯': (j, H) => setWhen(H, j, '꼼꼼한 정원사', W('shuffle'), 2),              // 더미를 섞으면 새 씨앗
  '엘프/엘레나': (j, H) => setWhen(H, j, '드론 정비', W('shieldBreak'), 2),            // 파티 실드가 깨지면 드론이 나선다
  '정령/라이카': (j, H) => setWhen(H, j, '220V 콘센트', W('play', { maxCost: 0 })),     // 비용 0 카드가 나가면 과충전
  // 몰린 계기 덜기 — 파괴(처치) 17 · 연계 21 에서 다른 갈래로
  '용족/제이드': (j, H) => setWhen(H, j, '지식 독점', W('break')),                    // 붕괴 — 격파한 적의 지식
  '요정/에슈르_마도': (j, H) => H.P(j, '노력의 결실', r => { r.when = W('debuff'); r.limit = { per: 'turn', n: 2 }; }),   // 디버프를 걸면 드로우(턴당 2회)
  '정령/블랑셰': (j, H) => setWhen(H, j, '푸른 장미 정원', W('exhaust')),             // 소각 — 지는 장미
  '정령/니콜': (j, H) => setWhen(H, j, '깜짝 출연', W('play', { chain: true })),       // 연속 — 같은 성격이 이어지면 테이크
  '마녀/셰럼': (j, H) => setWhen(H, j, '아군관찰일지', W('drawn', { type: '스킬' })),   // 감응(스킬) — 관찰 일지
  '마녀/아야': (j, H) => setWhen(H, j, '시련 속의 눈꽃', W('debuff')),                // 디버프를 걸면 서리
};
/** who other → any(나 포함) — 연계(play other)는 그 뜻이 「다른 아군」 이라 둔다. */
export function trigAny(j) {
  const h = j.heroes[0];
  for (const r of [...(h.passives || []), ...[h.keyword, ...(h.keywords || [])].filter(Boolean).flatMap(k => k.rules || [])])
    if (r.when && r.when.who === 'other' && ['make', 'exhaust', 'drawn', 'hit', 'discard', 'spend', 'extra', 'crit'].includes(r.when.on)) r.when.who = 'any';
}
