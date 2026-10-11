// 정석 인연 파티 손질(2026-10-10) — 약한 파티의 하이퍼캐리가 공급 자원을 실제로 받아 쓰게(동료 몫에서만 크게 도는 계기)
const st = (id, v, target) => (target ? { k: 'stack', id, v, target } : { k: 'stack', id, v });
export const FB_PARTY = {
  // P15 생성 소비(에슈르 빵 · 네르): 동료가 만든 카드마다 케이크 냄새 — 배부름 1 · 무작위 적 추가 공격
  '요정/에르핀': (j, H) => { H.rule(j, { name: '케이크 냄새', when: { on: 'make', who: 'other' }, fx: [st('배부름', 1), { k: 'extra', ratio: 1.2, target: 'randomEnemy' }] }); },
  // P14 소멸 소비(마요(멋짐) · 죠안): 누구의 카드든 소멸하면 혼돈 1 · 뒤처리 한 대
  '유령/림_혼돈': (j, H) => { H.rule(j, { name: '뒤처리 정리', when: { on: 'exhaust', who: 'other' }, fx: [st('혼돈', 1), { k: 'extra', ratio: 1.3, target: 'randomEnemy' }] }); },
  // P05 사기 · 고학년 공급(나이아 · 가비아): 동료가 고학년을 쓰면 수압 2 · 동료 공격마다 물줄기
  '요정/샤샤': (j, H) => {
    H.rule(j, { name: '물보라 응원', when: { on: 'ult', who: 'any' }, fx: [st('텀블러 수압', 2)] });
    H.rule(j, { name: '같이 쏘기', when: { on: 'play', who: 'other', type: '스킬' }, fx: [{ k: 'extra', ratio: 0.35, target: 'oneEnemy' }] });
  },
  // P02 생성 · AP 공급(스패럿 노획물 · 그윈 지도): 동료가 만든 카드마다 수족열증 · 추가 공격
  '수인/델리아': (j, H) => { H.rule(j, { name: '선장님 몫', when: { on: 'make', who: 'other' }, fx: [st('수족열증', 1, 'randomEnemy'), { k: 'extra', ratio: 0.8, target: 'randomEnemy' }] }); },
  // P12 AP · 사기 공급(페스타 · 죠안): 동료의 스킬마다 스텝 맞춰 한 대
  '정령/아르코': (j, H) => { H.rule(j, { name: '댄스 쇼다운', when: { on: 'play', who: 'other', type: '스킬' }, fx: [{ k: 'extra', ratio: 0.2, target: 'oneEnemy' }] }); },
  // 세기(사도 135명 측정): 뮤트 −15.5 — 서포터인데 버팀 몫이 없다(HP 500). 홀로그램 그물이 파티를 가린다
  '정령/뮤트': (j, H) => { H.rule(j, { name: '홀로그램 방벽', when: { on: 'turnStart' }, fx: [{ k: 'shield', ratio: 0.45 }] }); },
  // P07(74%)은 벨벳 쪽 계기가 아니라 탱커 둘 + 서포터 편성 자체가 세다(같은 꼴 무작위 편성 81%) — 탱커 전반 세기(밸런스 담당 몫)라 손대지 않는다
};
