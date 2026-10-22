// 아야 — 딜러 · 냉정 · 마녀. 설계: _measure/아야_약점사냥꾼_리뉴얼안.md (10-20 리워크 2판 「눈꽃 퓨어딜러」 — 사용자 10-19 밤)
// 엔진: 적에게 「눈꽃」(아야 전용 표식 — weakOwner: 걸린 적은 아야의 타격만 약점, 동료는 아님 · 저절로 안 짐)을 피우고 →
//   눈꽃이 핀 적을 아야가 치면 약점(피해 ×1.25 · 강인도 많이) · 「만년설」 +1(약점 공격 피해 1개당 +8%) →
//   「얼음 결정 깨기」 가 눈꽃 1을 꺾으며 한 대 더 → 격파 → 「빙설화도」: 격파 상태면 만년설을 모두 써서 1개당 큰 한 방 → 다시 피우기.
//   걷어 낸 것(4차 → 리워크): 키워드 「서리」 · 「약점 노출」 · 「냉정 약점」(동료도 덕 보는 장치) · 강인도 선 · 보스 덤 · 「약점 공격」 태그.
//   사용자 수정 ①: 「눈꽃의 날개짓」 은 전투 시작 눈꽃 대신 드로우 1(설계안 갈림길 D-나) · ②: 그만큼 딜 살짝 상향(프론티어 딜러 평균 +5% 안팎 · 혼자 ≤ 78%).
//   사용자 10-19 밤 2차: 「눈꽃의 날개짓」(전투 시작 드로우 1)을 패시브째 없앰 — 패시브는 「만년설의 현자님!」 하나(검사기는 둘까지만 본다).
//     프론티어 딜러 평균 +5.0 → +5.3%(빙설화도는 신탁 I 로 재 그대로) · 혼자 77.0 → 70.3% 라 빙설화도 보정은 안 함.
//   격파 덤 「격파」 는 네르(빡침) 소유 — 빙설화도 한 장의 격파 덤만 나눠 쓴다(설계안 갈림길 A-가). 자동 피해 0. 4차 판 백업: scratchpad rw20/bak
import { hero, dmg, stack, spend, draw, status } from '../lib.mjs';

const H = '아야', M = '만년설', F = '눈꽃';
const tough = (v, target) => ({ k: 'tough', v, ...(target ? { target } : {}) });
const bloom = (v = 1, target = 'oneEnemy') => stack(F, v, { target });            // 눈꽃 피우기
const weak = (v = 1, target = 'oneEnemy') => status('약화', v, { target });         // 원작 싸락나비 — 걸어 주기만(란 정산)
const marked = { k: 'ifStack', id: F, n: 1 };                                       // 고른 적에게 눈꽃이 있으면
const hunt = (r) => [marked, dmg(r), spend(F, 1)];                                  // 눈꽃 1을 꺾으며 한 대 더(소모가 뒤라 마지막 송이도 두 대 모두 약점)
const shatter = (per) => [{ k: 'ifFoe', id: 'broken' }, spend(M, 'all'), { k: 'perEvent' }, dmg(per)];   // 격파 상태면 만년설 모두 써서 1개당

export default hero({
  id: H, name: '아야', nature: '냉정', race: '마녀', role: '딜러', star: 3,
  blurb: '만년설의 현자, 영원살이 맏이. 적에게 자신만 아는 눈꽃을 피워 틈을 열고, 그 틈을 하나씩 베어 내다 얼음이 깨지는 순간 빙설화도를 내리칩니다.',
  keyword: { name: M, desc: '눈꽃을 벨 때마다 쌓이는 현자의 눈 — 약점 공격이 더 깊어짐', carrier: 'self', cap: 5, per: [{ stat: 'weak', v: 0.08 }] },
  // 보조 고유 효과 — 아야 전용 적 표식(엔진 weakOwner, 10-20)
  keywords: [{ name: F, desc: '아야가 피운 눈꽃 — 아야만 아는 틈', carrier: 'enemy', cap: 5, weakOwner: true, per: [{ stat: 'taken', v: 0.04, from: 'owner' }] }],   // 10-22 아야 딜 보강(퓨어 딜러 — 표식): 1당 아야에게 받는 피해 +4%
  passives: [
    { name: '만년설의 현자님!', when: { on: 'hit', weak: true }, fx: [stack(M, 1)] },   // 원작 팀 버프 → 아야만: 약점으로 칠 때마다 만년설 1
  ],
  ult: { name: '만개설화', cost: 200, fx: [dmg(1.6, 'allEnemies'), bloom(3, 'allEnemies'), tough(1, 'allEnemies'), { k: 'ifFoe', id: 'alone' }, dmg(2.5, 'allEnemies')] },   // 동상(냉정 약점) 대신 눈꽃을 크게 · 하나뿐이면 250% 더 그대로
}, {
  basicNames: ['서리 베기', '서리 베기', '은빛 양산'], deck: 'light',
  uniques: [
    { // 시동 — 싸락나비: 치고 눈꽃 · 약화
      name: '싸락나비', cost: 1, type: '공격',
      fx: [dmg(0.4, 'oneEnemy', { hits: 2 }), bloom(2), weak(1)],
      oracles: [
        { fx: [dmg(0.7, 'oneEnemy', { hits: 2 }), bloom(2), weak(1)] },
        { fx: [dmg(0.8, 'randomEnemy', { hits: 2, hitFx: [bloom(1)] })] },
        { fx: [dmg(0.5, 'oneEnemy', { hits: 2 }), bloom(1, 'allEnemies')] },
        { fx: [dmg(0.5, 'oneEnemy', { hits: 2 }), bloom(3)] },
        { fx: [dmg(0.4, 'oneEnemy', { hits: 2 }), bloom(2), stack(M, 1)] },
      ],
      blesses: ['power', 'draw', [bloom(1)]],
    },
    { // 원작 「차분해질 시간이야」 — 동상 대신 눈꽃 · 행동 늦춤
      name: '차분해질 시간이야', cost: 0, type: '스킬',
      fx: [bloom(1), { k: 'rushDown', v: 1, target: 'oneEnemy' }, stack(M, 1)],
      oracles: [
        { fx: [bloom(2), { k: 'rushDown', v: 1, target: 'oneEnemy' }, stack(M, 1)] },
        { fx: [bloom(1, 'allEnemies'), stack(M, 1)] },
        { fx: [bloom(1), stack(M, 1), draw(1)] },
        { fx: [{ k: 'rushDown', v: 1, target: 'allEnemies' }, bloom(1), stack(M, 1)] },
        { fx: [bloom(1), stack(M, 2)] },
      ],
      blesses: ['draw', 'cost', [bloom(1)]],
    },
    { // 간파 — 눈꽃 · 디버프(눈꽃도 든다)가 걸린 적이면 강인도도
      name: '간파', cost: 0, type: '공격',
      fx: [dmg(0.5), bloom(1), { k: 'ifDebuffs', n: 1 }, tough(1)],
      oracles: [
        { fx: [dmg(1), bloom(1), { k: 'ifDebuffs', n: 1 }, tough(1)] },
        { fx: [dmg(0.8), bloom(2)] },
        { fx: [dmg(0.7, 'allEnemies'), bloom(1, 'allEnemies'), tough(1, 'allEnemies')] },
        { fx: [dmg(0.8), bloom(1), { k: 'ifDebuffs', n: 1 }, tough(2)] },
        { fx: [dmg(0.8), bloom(1), draw(1)] },
      ],
      blesses: ['power', 'draw', [bloom(1)]],
    },
    { // 사냥 — 얼음 결정 깨기: 눈꽃을 꺾으며 베기(원작 발도 2회)
      name: '얼음 결정 깨기', cost: 1, type: '공격',
      fx: [dmg(0.8), ...hunt(0.8)],
      oracles: [
        { fx: [dmg(1.1), ...hunt(1.0)] },
        { fx: [dmg(0.6), marked, spend(F, 'all'), { k: 'perEvent' }, dmg(0.6)] },
        { cost: 0, fx: [dmg(0.5), ...hunt(0.5)] },
        { fx: [dmg(0.5, 'allEnemies'), tough(1, 'allEnemies'), ...hunt(0.6)] },
        { fx: [dmg(0.7), ...hunt(0.7), stack(M, 1)] },
      ],
      blesses: ['power', 'cost', [bloom(1)]],
    },
    { // 결정타 — 빙설화도: 격파 순간 큰 한 방(2코 · 보존)
      name: '빙설화도', cost: 2, type: '공격', tags: ['보존'],
      fx: [dmg(2.2), tough(1), ...shatter(3.3)],   // 사용자 수정 ②: 1개당 300 → 330%(시작 눈꽃이 빠진 몫 — 프론티어 딜러 평균 +1 → +5%, 혼자 76.4 → 77.0%)
      oracles: [
        { tags: ['보존'], fx: [dmg(3.0), tough(1), ...shatter(0.7)] },
        { tags: ['보존'], fx: [dmg(2.4), tough(2), ...shatter(0.5)] },
        { tags: ['보존'], fx: [dmg(1.8, 'allEnemies'), ...shatter(0.3)] },
        { tags: ['보존'], fx: [dmg(2.0), tough(1), marked, dmg(1.5)] },
        { cost: 1, tags: ['보존'], fx: [dmg(1.2), ...shatter(1.5)] },
      ],
      blesses: ['power', 'cost', [bloom(1)]],
    },
  ],
});
