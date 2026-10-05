import { hero, START, O, D, DD, SH, HE, ST, STK, SPA, SP, MK, DR, AP, IFS, IFNOT, PERS, WHEN, IFC } from './lib.mjs';

const B = (name, kind) => ({ name, kind });
const Bf = (name, fx) => ({ name, fx });
const Bt = (name, tags) => ({ name, tags });

// ───────────────────────── 셰럼 ─────────────────────────
{
  const W = '원고', R = '연재 회차';
  hero('셰럼', {
    name: '셰럼', nature: '순수', race: '마녀', row: 'front', role: '탱커', star: 3, hp: 860, atk: 75, def: 76, crit: 5,
    blurb: '궁정 서기관 마녀. 파티가 맞은 장면까지 받아 적고, 원고가 차면 몰래 쓰던 소설 속 인물이 차례로 걸어 나온다.',
    keyword: {
      name: W, desc: '현실로 튀어나온 원고 — 파티가 다칠 때마다 한 쪽, 셋이면 연재 회차가 넘어간다', carrier: 'self', cap: 3,
      rules: [{ name: '탈고', when: { on: 'stackReach', id: W, n: 3 }, fx: [SPA(W), STK(R, 1)] }],
    },
    keywords: [{
      name: R, desc: '회차마다 원고 속 인물 카드가 나온다 — 셋째 회차 뒤 처음부터', carrier: 'self', cap: 3,
      rules: [
        { name: '1회: 어둠 정령', when: { on: 'stackReach', id: R, n: 1 }, fx: [MK('셰럼_t1')] },
        { name: '2회: 기사', when: { on: 'stackReach', id: R, n: 2 }, fx: [MK('셰럼_t2')] },
        { name: '3회: 공주', when: { on: 'stackReach', id: R, n: 3 }, fx: [MK('셰럼_t3'), SPA(R)] },
      ],
    }],
    passives: [{ name: '받아 적기', when: { on: 'hurt' }, limit: { per: 'turn', n: 2 }, fx: [STK(W, 1)] }],
    ult: { name: '생생한 흑역사', cost: 250, fx: [DD(0.3, 'allEnemies', { hits: 6 }), ST('약화', 2, 'allEnemies'), STK(W, 2)] },
    ...START.tank('깃펜 찌르기', '두꺼운 장부'),
    tokens: [
      { id: 't1', name: '어둠 정령', cost: 0, type: '공격', tags: ['소멸'], fx: [DD(0.6), ST('약화', 1, 'oneEnemy')], blurb: '원고 1회의 주인공. 작가보다 진지하다' },
      { id: 't2', name: '원고 속 기사', cost: 0, type: '스킬', tags: ['소멸'], fx: [SH(1.4)], blurb: '원고 2회. 방패를 들고 걸어 나온다' },
      { id: 't3', name: '원고 속 공주', cost: 0, type: '스킬', tags: ['소멸'], fx: [HE(1.0), ST('결의', 1)], blurb: '원고 3회. 해피엔딩 담당' },
    ],
    uniques: [
      { id: 'u1', name: '위치 아카이브', cost: 2, type: '스킬', sig: true, fx: [SH(2.6), STK(W, 1)],
        blurb: '마녀 왕실의 일을 다 적어 둔 서고. 펼치면 그날의 흑역사가 쏟아진다',
        oracles: [O.up('왕실 사료 전권', 1.35), O.cheap('간추린 기록', 0.75), O.plus('기밀 문서', [ST('피해 감소', 1)], 1.15), O.raw('흑역사 보관함', [SH(2.8), STK(W, 2)]), O.tag('정리된 서가', ['보존'], 1.3)],
        blesses: [B('두꺼운 표지', 'guard'), Bt('색인', ['보존']), Bf('각주', [STK(W, 1)])] },
      { id: 'u2', name: '어둠 정령의 상처', cost: 2, type: '공격', fx: [DD(0.55, 'allEnemies'), ST('약화', 1, 'allEnemies'), STK(W, 1)],
        blurb: '몰래 쓴 소설의 한 장면. 읽히는 쪽이 더 아프다',
        oracles: [O.up('완결편', 1.35), O.cheap('습작', 0.7), O.raw('2쇄 증보판', [DD(0.6, 'allEnemies'), ST('약화', 1, 'allEnemies'), STK(W, 2)]), O.plus('작가의 말', [], 1.4), O.tag('비밀 동인지', ['보존'], 1.3)],
        blesses: [B('클리셰 폭발', 'power'), Bf('삽화', [ST('약화', 1, 'allEnemies')]), B('연재 마감', 'cost')] },
      { id: 'u3', name: '미행과 잠입', cost: 1, type: '스킬', fx: [SH(1.5), WHEN('draw'), STK(W, 1)],
        blurb: '미행 · 잠입 · 받아쓰기. 들키면 맞는 장면까지 기록에 넣는다',
        oracles: [O.up('장기 잠입', 1.4), O.cheap('숨어 엿보기', 0.75), O.raw('발소리 지우기', [SH(1.6), ST('피해 감소', 1), WHEN('draw'), STK(W, 1)]), O.raw('현장 기록', [SH(1.6), STK(W, 1), WHEN('draw'), STK(W, 1)]), O.tag('검은 망토', ['개전'], 1.3)],
        blesses: [B('검은 망토', 'guard'), B('메모장', 'draw'), Bt('발끝 걸음', ['개전'])] },
      { id: 'u4', name: '서기관의 기록체', cost: 1, type: '강화', fx: [ST('결정화', 1), STK(W, 1)],
        blurb: '「~함.」 으로 끝나는 기록체. 모든 일은 사료가 된다',
        oracles: [O.raw('궁정 서기관', [ST('결정화', 2), STK(W, 1)]), O.raw('가벼운 필기', [ST('결정화', 1)], { cost: 0 }), O.raw('사료와 전례', [ST('결정화', 1), STK(W, 2), ST('결의', 1)]), O.raw('기록체', [ST('결정화', 1), STK(W, 1), ST('반격', 2)]), O.raw('정리된 서가', [ST('결정화', 1), STK(W, 2)], { tags: ['개전'] })],
        blesses: [B('함.', 'defUp'), Bt('인 듯함.', ['개전']), Bf('정중한 존댓말', [SH(0.8)])] },
    ],
  });
}

// ───────────────────────── 아야 ─────────────────────────
{
  const K = '서리';
  hero('아야', {
    name: '아야', nature: '냉정', race: '마녀', row: 'mid', role: '딜러', star: 3, hp: 720, atk: 146, def: 29, crit: 10,
    blurb: '영원살이 마녀 자매의 맏이. 탄환마다 서리를 묻혀 두고, 그 적이 무너지는 순간 눈꽃으로 피워 낸다.',
    keyword: { name: K, desc: '눈꽃 개화 — 공격으로 묻히는 서리. 묻은 적은 약해지고, 격파되면 적 전체로 터진다', carrier: 'enemy', cap: 5, per: [{ stat: 'dealt', v: -0.05 }] },
    passives: [
      { name: '서리 탄환', when: { on: 'play', type: '공격' }, fx: [STK(K, 1, 'oneEnemy')] },
      { name: '눈꽃 개화', when: { on: 'break' }, fx: [PERS(K), D(0.35, 'allEnemies'), SPA(K)] },
    ],
    ult: { name: '만개설화', cost: 150, fx: [D(0.9, 'allEnemies'), STK(K, 2, 'allEnemies'), { k: 'tough', v: 1, target: 'allEnemies' }] },
    ...START.dealer('서리 탄환', '빙창 탄환', '얼음 장막'),
    uniques: [
      { id: 'u1', name: '얼음 결정 탄', cost: 1, type: '공격', sig: true, tags: ['약점 공격'], fx: [D(1.0), STK(K, 1, 'oneEnemy')],
        blurb: '맞은 자리부터 얼어붙는 탄환',
        oracles: [O.up('단단한 결정', 1.35), O.cheap('작은 결정', 0.75), O.plus('겹눈송이', [ST('약화', 1, 'oneEnemy')], 1.1), O.raw('서리 꽃봉오리', [D(1.1), STK(K, 2, 'oneEnemy')], { tags: ['약점 공격'] }), O.tag('맏이의 조준', ['주도'], 1.25)],
        blesses: [B('날 선 서리', 'power'), B('얼어붙는 탄', 'frost'), Bf('눈송이', [STK(K, 1, 'oneEnemy')])] },
      { id: 'u2', name: '눈보라 사격', cost: 2, type: '공격', fx: [D(0.5, 'oneEnemy', { hits: 3 }), { k: 'tough', v: 1 }],
        blurb: '눈보라 속에서도 과녁은 하나',
        oracles: [O.up('한겨울 사격', 1.3), O.cheap('짧은 눈보라', 0.7), O.raw('휘몰아치는 눈', [D(0.4, 'allEnemies', { hits: 3 }), { k: 'tough', v: 1, target: 'allEnemies' }]), O.plus('얼음 박차', [ST('취약', 1, 'oneEnemy')], 1.1), O.big('끝없는 겨울', 2.0, [ST('잔광', 1)])],
        blesses: [B('세찬 바람', 'power'), B('빠른 장전', 'cost'), B('겨울 숨결', 'weakSpot')] },
      { id: 'u3', name: '동상 조준', cost: 0, type: '스킬', fx: [STK(K, 2, 'oneEnemy'), ST('약화', 1, 'oneEnemy'), DR(1)],
        blurb: '먼저 얼려 두고, 그다음에 쏜다',
        oracles: [O.raw('깊은 동상', [STK(K, 3, 'oneEnemy'), ST('약화', 1, 'oneEnemy'), DR(1)]), O.raw('얼음 눈금', [STK(K, 2, 'oneEnemy'), ST('취약', 1, 'oneEnemy'), DR(1)]), O.raw('서릿발', [STK(K, 2, 'oneEnemy'), ST('약화', 1, 'oneEnemy'), DR(2)]), O.raw('겨울 시야', [STK(K, 2, 'allEnemies'), DR(1)]), O.raw('희망의 눈꽃', [STK(K, 2, 'oneEnemy'), ST('약화', 2, 'oneEnemy'), DR(1)], { tags: ['보존'] })],
        blesses: [B('차가운 손끝', 'draw'), Bf('하얀 숨', [ST('약화', 1, 'oneEnemy')]), Bt('눈 덮인 길', ['보존'])] },
      { id: 'u4', name: '영원살이 맏이', cost: 1, type: '강화', fx: [ST('잔광', 2), { k: 'atkMod', v: 0.15, run: true, target: 'self' }],
        blurb: '눈보라 속에서 희망을 찾아낸 맏이의 각오',
        oracles: [O.raw('맏이의 각오', [ST('잔광', 3), { k: 'atkMod', v: 0.15, run: true, target: 'self' }]), O.raw('작은 다짐', [ST('잔광', 1), { k: 'atkMod', v: 0.1, run: true, target: 'self' }], { cost: 0 }), O.raw('만개의 예감', [ST('잔광', 2), { k: 'atkMod', v: 0.2, run: true, target: 'self' }]), O.raw('겨울 지킴이', [ST('잔광', 2), { k: 'atkMod', v: 0.15, run: true, target: 'self' }, ST('피해 감소', 2)]), O.raw('첫눈', [ST('잔광', 2), { k: 'atkMod', v: 0.15, run: true, target: 'self' }], { tags: ['개전'] })],
        blesses: [B('단단한 마음', 'atkUp'), Bt('첫눈', ['개전']), Bf('눈꽃 장식', [STK(K, 1, 'allEnemies')])] },
    ],
  });
}

// ───────────────────────── 벨벳 ─────────────────────────
{
  const K = '근력';
  hero('벨벳', {
    name: '벨벳', nature: '냉정', race: '마녀', row: 'front', role: '탱커', star: 3, hp: 980, atk: 84, def: 64, crit: 5,
    blurb: '근력 강화 마법 하나로 고위 마녀가 된 근육파. 체력을 깎아 근육에 붓고, 불어난 근육으로 맞고 때린다.',
    keyword: { name: K, desc: '근력 강화 마법 — HP 를 치러 키우는 근육. 쌓일수록 세지고 덜 아프다', carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.12 }, { stat: 'taken', v: -0.04 }] },
    passives: [{ name: '근육은 배신하지 않는다', when: { on: 'stackReach', id: K, n: 5 }, limit: { per: 'fight', n: 1 }, fx: [ST('불굴', 1)] }],
    ult: { name: '마법: 원심 분리', cost: 250, fx: [DD(0.3, 'allEnemies', { hits: 5 }), ST('둔화', 1, 'allEnemies'), STK(K, 2)] },
    ...START.tank('주먹 한 방', '이두근 가드'),
    uniques: [
      { id: 'u1', name: '이두근 펌핑', cost: 1, type: '스킬', sig: true, fx: [{ k: 'payHpPct', v: 0.03 }, SH(1.9), STK(K, 1)],
        blurb: '마력 대신 근육을 부풀리는 근력 강화 마법',
        oracles: [O.up('삼두근까지', 1.35), O.cheap('가벼운 펌핑', 0.75), O.raw('부풀어 오른 등근육', [{ k: 'payHpPct', v: 0.03 }, SH(1.8), STK(K, 2)]), O.tag('펌핑 후 휴식', ['보존'], 1.25), O.tag('아침 운동', ['개전'], 1.3)],
        blesses: [B('근육 방패', 'guard'), Bf('단백질', [HE(0.6)]), B('근성', 'defUp')] },
      { id: 'u2', name: '원심 분리 펀치', cost: 1, type: '공격', tags: ['분쇄'], fx: [DD(0.85), IFS(K, 3), ST('약화', 1, 'oneEnemy')],
        blurb: '빙글 돌고 나서 꽂는 주먹. 근육이 셋이면 상대가 흔들린다',
        oracles: [O.up('풀스윙', 1.35), O.cheap('잽', 0.7), O.raw('어퍼컷', [DD(0.9), { k: 'tough', v: 1 }, IFS(K, 3), ST('약화', 1, 'oneEnemy')], { tags: ['분쇄'] }), O.big('회전 연타', 2.0), O.tag('근육 자랑', ['보존'], 1.3)],
        blesses: [B('강펀치', 'power'), B('무거운 주먹', 'frost'), B('빠른 손', 'cost')] },
      { id: 'u3', name: '프로틴 셰이크', cost: 1, type: '스킬', fx: [{ k: 'payHpPct', v: 0.03 }, ST('불굴', 1), STK(K, 1)],
        blurb: '맛은 묻지 않는다. 근육이 기억할 뿐',
        oracles: [O.raw('곱빼기 셰이크', [{ k: 'payHpPct', v: 0.03 }, ST('불굴', 1), STK(K, 2)]), O.raw('한 모금', [{ k: 'payHpPct', v: 0.03 }, ST('불굴', 1)], { cost: 0 }), O.raw('회복 셰이크', [ST('불굴', 1), STK(K, 1), HE(0.8)]), O.raw('철분 추가', [{ k: 'payHpPct', v: 0.03 }, ST('불굴', 1), STK(K, 1)], { tags: ['보존'] }), O.raw('운동 전 한 잔', [{ k: 'payHpPct', v: 0.03 }, ST('불굴', 1), STK(K, 1)], { tags: ['개전'] })],
        blesses: [B('단백질 듬뿍', 'draw'), Bt('셰이커', ['보존']), Bf('바나나', [SH(0.8)])] },
      { id: 'u4', name: '고위 마녀의 단련법', cost: 2, type: '강화', fx: [STK(K, 2), ST('반격', 3)],
        blurb: '마법도 결국 근육이다 — 벨벳의 지론',
        oracles: [O.raw('지옥 훈련', [STK(K, 3), ST('반격', 3)]), O.raw('가벼운 단련', [STK(K, 1), ST('반격', 2)], { cost: 1 }), O.raw('맞으면서 단련', [STK(K, 2), ST('반격', 5)]), O.raw('버티는 단련', [STK(K, 2), ST('반격', 3), ST('결의', 1)]), O.raw('새벽 단련', [STK(K, 2), ST('반격', 3)], { tags: ['개전'] })],
        blesses: [B('근육 마녀', 'defUp'), Bt('새벽 기상', ['개전']), Bf('근육통', [ST('피해 감소', 1)])] },
    ],
  });
}

// ───────────────────────── 프리클 ─────────────────────────
{
  const K = '봉인 함정';
  hero('프리클', {
    name: '프리클', nature: '냉정', race: '마녀', row: 'mid', role: '딜러', star: 3, hp: 640, atk: 135, def: 28, crit: 10,
    blurb: '마녀 왕궁의 참모. 적 앞에 가시 덫을 깔아 두고, 계획이 어긋나도 덫은 거두지 않는다 — 다음 공격까지 기다릴 뿐.',
    keyword: { name: K, desc: '적 앞에 깔아 둔 덫 — 그 적이 공격하기 직전 쌓인 만큼 터진다', carrier: 'enemy', cap: 4 },
    passives: [
      { name: '덫 작동', when: { on: 'foeActBefore', type: '공격' }, conds: [{ c: 'stack', id: K }], fx: [PERS(K), D(1.1), ST('둔화', 1, 'oneEnemy'), SPA(K)] },
    ],
    ult: { name: '조여오는 경비병', cost: 200, fx: [D(1.0, 'allEnemies'), STK(K, 2, 'allEnemies')] },
    ...START.dealer('가시 투척', '가시 작살', '덩굴 장벽'),
    uniques: [
      { id: 'u1', name: '가시 덫 설치', cost: 1, type: '스킬', sig: true, fx: [STK(K, 2, 'oneEnemy'), ST('둔화', 1, 'oneEnemy'), DR(1)],
        blurb: '발밑을 보지 않는 적이 잘못이다',
        oracles: [O.raw('겹겹이 덫', [STK(K, 3, 'oneEnemy'), ST('둔화', 1, 'oneEnemy'), DR(1)]), O.raw('급조한 덫', [STK(K, 2, 'oneEnemy'), DR(1)], { cost: 0 }), O.raw('덩굴 올가미', [STK(K, 2, 'oneEnemy'), ST('둔화', 2, 'oneEnemy'), DR(1)]), O.raw('덫밭', [STK(K, 1, 'allEnemies'), ST('둔화', 1, 'oneEnemy'), DR(1)]), O.raw('미리 짠 작전', [STK(K, 2, 'oneEnemy'), ST('둔화', 1, 'oneEnemy'), DR(1)], { tags: ['개전'] })],
        blesses: [B('작전 노트', 'draw'), Bf('가시 하나 더', [STK(K, 1, 'oneEnemy')]), Bt('준비된 참모', ['개전'])] },
      { id: 'u2', name: '가시 작살 연사', cost: 1, type: '공격', fx: [D(1.2), STK(K, 1, 'oneEnemy')],
        blurb: '꽂힌 가시가 그대로 덫이 된다',
        oracles: [O.up('굵은 작살', 1.35), O.cheap('가시 한 줌', 0.7), O.raw('미늘 작살', [D(1.1), STK(K, 2, 'oneEnemy')]), O.big('작살 폭풍', 1.4, [{ k: 'tough', v: 1 }]), O.plus('관통 작살', [ST('취약', 1, 'oneEnemy')], 1.1)],
        blesses: [B('날카로운 가시', 'power'), B('독 묻은 가시', 'weakSpot'), B('빠른 손놀림', 'cost')] },
      { id: 'u3', name: '덩굴 경비병', cost: 2, type: '공격', fx: [D(0.8, 'allEnemies'), STK(K, 1, 'allEnemies')],
        blurb: '조여 오는 덩굴 경비병들',
        oracles: [O.up('굵은 덩굴', 1.35), O.cheap('덩굴 한 줄기', 0.75), O.raw('얽히는 덩굴', [D(0.8, 'allEnemies'), STK(K, 2, 'allEnemies')]), O.plus('조이는 덩굴', [ST('둔화', 1, 'allEnemies')], 1.1), O.tag('잠복 경비', ['보존'], 1.3)],
        blesses: [B('억센 덩굴', 'power'), Bf('가시 울타리', [ST('약화', 1, 'allEnemies')]), B('정원 손질', 'cost')] },
      { id: 'u4', name: '참모의 수정안', cost: 1, type: '강화', fx: [{ k: 'atkMod', v: 0.15, run: true, target: 'self' }, STK(K, 1, 'allEnemies')],
        blurb: '계획이 어긋나면 그 자리에서 고쳐 쓴다',
        oracles: [O.raw('완벽한 수정안', [{ k: 'atkMod', v: 0.2, run: true, target: 'self' }, STK(K, 1, 'allEnemies')]), O.raw('메모 한 줄', [{ k: 'atkMod', v: 0.1, run: true, target: 'self' }, STK(K, 1, 'oneEnemy')], { cost: 0 }), O.raw('두 번째 계획', [{ k: 'atkMod', v: 0.15, run: true, target: 'self' }, STK(K, 2, 'allEnemies')]), O.raw('예비 계획', [{ k: 'atkMod', v: 0.15, run: true, target: 'self' }, STK(K, 1, 'allEnemies'), DR(1)]), O.raw('사전 작전', [{ k: 'atkMod', v: 0.15, run: true, target: 'self' }, STK(K, 1, 'allEnemies')], { tags: ['개전'] })],
        blesses: [B('참모의 눈', 'atkUp'), Bt('미리 짠 판', ['개전']), B('지휘봉', 'draw')] },
    ],
  });
}

// ───────────────────────── 피코라 ─────────────────────────
{
  const K = '스티커';
  hero('피코라', {
    name: '피코라', nature: '냉정', race: '마녀', row: 'back', role: '서포터', star: 3, hp: 580, atk: 75, def: 58, crit: 5,
    blurb: '스티커 만들기가 낙인 패셔니스타 마녀. 반짝 · 하트 · 별 · 해골 넷을 다 모으면 한정판 세트가 된다.',
    keyword: {
      name: K, desc: '한정 스티커 수집 — 카드를 낼 때마다 한 장, 넷이면 한정판 세트', carrier: 'self', cap: 4,
      rules: [{ name: '한정판 세트', when: { on: 'stackReach', id: K, n: 4 }, fx: [ST('사기', 1), ST('피해 감소', 2), SPA(K)] }],
    },
    passives: [{ name: '스티커 붙이기', when: { on: 'play' }, fx: [STK(K, 1)] }],
    ult: { name: '너도 될 수 있다 패션피플', cost: 200, fx: [SH(3.0), STK(K, 2), ST('피해 감소', 1)] },
    ...START.support('요술봉 톡', '반짝 파우더'),
    uniques: [
      { id: 'u1', name: '반짝이 스티커', cost: 1, type: '스킬', sig: true, fx: [SH(1.6), STK(K, 1)],
        blurb: '붙이면 반짝, 떼면 아쉬운 첫 장',
        oracles: [O.up('홀로그램 반짝이', 1.4), O.cheap('작은 반짝이', 0.7), O.plus('반짝이 코팅', [ST('결의', 1)], 1.0), O.raw('두 장 겹쳐 붙이기', [SH(1.7), STK(K, 2)]), O.tag('마음에 든 장', ['보존'], 1.3)],
        blesses: [B('반짝 코팅', 'guard'), B('새 도안', 'draw'), Bt('스티커북', ['보존'])] },
      { id: 'u2', name: '해골 스티커', cost: 1, type: '공격', fx: [DD(0.65), ST('취약', 1, 'oneEnemy')],
        blurb: '귀여운 해골은 생각보다 아프다',
        oracles: [O.up('큼직한 해골', 1.4), O.cheap('미니 해골', 0.7), O.raw('해골 도배', [DD(0.5, 'allEnemies'), ST('취약', 1, 'allEnemies')]), O.plus('해골 한 쌍', [STK(K, 1)], 1.1), O.raw('오싹 해골', [DD(0.7), ST('취약', 2, 'oneEnemy')])],
        blesses: [B('반짝이는 해골', 'power'), B('찐득한 풀', 'frost'), Bf('해골 하나 더', [STK(K, 1)])] },
      { id: 'u3', name: '별 스티커', cost: 0, type: '스킬', fx: [STK(K, 1), DR(1)],
        blurb: '손이 빨라야 별을 모은다',
        oracles: [O.raw('별 한 묶음', [STK(K, 2), DR(1)]), O.raw('유성 스티커', [STK(K, 1), DR(2)]), O.raw('별빛 파우더', [STK(K, 1), DR(1), HE(0.6)]), O.raw('별자리 세트', [STK(K, 1), DR(1), ST('결의', 1)]), O.raw('행운의 별', [STK(K, 1), DR(1)], { tags: ['보존'] })],
        blesses: [B('반짝 별', 'draw'), Bf('하트 하나', [HE(0.4)]), Bt('포켓 앨범', ['보존'])] },
      { id: 'u4', name: '한정판 앨범', cost: 1, type: '강화', fx: [ST('결의', 1), STK(K, 2)],
        blurb: '아직 빈칸이 있다는 게 수집의 즐거움',
        oracles: [O.raw('완성된 앨범', [ST('결의', 2), STK(K, 2)]), O.raw('얇은 앨범', [ST('결의', 1), STK(K, 1)], { cost: 0 }), O.raw('두 권째 앨범', [ST('결의', 1), STK(K, 3)]), O.raw('패션 화보', [ST('결의', 1), STK(K, 2), ST('사기', 1)], { cost: 2 }), O.raw('첫 페이지', [ST('결의', 1), STK(K, 2)], { tags: ['개전'] })],
        blesses: [B('패셔니스타', 'defUp'), Bt('첫 장', ['개전']), Bf('사은품 스티커', [STK(K, 1)])] },
    ],
  });
}

// ───────────────────────── 롤렛 ─────────────────────────
{
  const K = '바꿔치기';
  hero('롤렛', {
    name: '롤렛', nature: '광기', race: '마녀', row: 'back', role: '딜러', star: 3, hp: 550, atk: 145, def: 25, crit: 10,
    blurb: '판을 슬쩍 굴리는 트릭스터 마녀. 손에서 한 장을 흘려 보내는 순간, 다음 마술봉이 훨씬 세게 들어간다.',
    keyword: { name: K, desc: '자신의 카드가 버려지면 준비되는 트릭 — 다음 공격 카드 한 장에 실린다', carrier: 'self', cap: 1, consumeAll: true, per: [{ stat: 'dealt', v: 0.6 }] },
    passives: [{ name: '소매 속 트릭', when: { on: 'discard' }, fx: [STK(K, 1)] }],
    ult: { name: '관객을 사로잡는 트릭스터', cost: 250, fx: [D(1.6, 'allEnemies'), ST('기절', 1, 'randomEnemy'), STK(K, 1)] },
    ...START.dealer('마술봉', '마술봉 대공연', '트릭 망토'),
    uniques: [
      { id: 'u1', name: '비둘기 부활 마술', cost: 0, type: '스킬', sig: true, fx: [{ k: 'discard', v: 1 }, DR(2)],
        blurb: '사라진 비둘기는 언제나 엉뚱한 데서 돌아온다',
        oracles: [O.raw('비둘기 떼', [{ k: 'discard', v: 1 }, DR(3)]), O.raw('모자 속 비둘기', [{ k: 'discard', v: 1 }, DR(2), ST('공명', 1)]), O.raw('두 번 접은 카드', [{ k: 'discard', v: 2 }, DR(3)]), O.raw('박수 유도', [{ k: 'discard', v: 1 }, DR(2), ST('사기', 1)], { cost: 1 }), O.raw('다시 부활', [{ k: 'discard', v: 1 }, DR(2)], { tags: ['보존'] })],
        blesses: [B('하얀 비둘기', 'draw'), Bt('비밀 주머니', ['보존']), Bf('갈채', [ST('공명', 1)])] },
      { id: 'u2', name: '마술봉 난타', cost: 2, type: '공격', fx: [D(1.0, 'oneEnemy', { hits: 3 })],
        blurb: '관객이 숨을 고르기 전에 끝낸다',
        oracles: [O.up('앙코르 난타', 1.3), O.cheap('짧은 난타', 0.7), O.plus('눈속임 난타', [{ k: 'discard', v: 1 }, DR(1)], 1.0), O.plus('피날레', [ST('취약', 1, 'oneEnemy')], 1.1), O.raw('흩뿌리는 난타', [D(0.5, 'randomEnemy', { hits: 6 })])],
        blesses: [B('스포트라이트', 'power'), B('빠른 손', 'cost'), B('관객 호응', 'ap')] },
      { id: 'u3', name: '트릭 카드', cost: 1, type: '공격', fx: [D(1.1), WHEN('discard'), D(1.1, 'randomEnemy'), DR(1)],
        blurb: '버린 줄 알았던 카드가 날아와 꽂힌다',
        oracles: [O.up('날 선 트릭 카드', 1.3), O.raw('가벼운 트릭', [D(0.75), WHEN('discard'), D(0.9, 'randomEnemy'), DR(1)], { cost: 0 }), O.raw('비장의 카드', [D(1.1), WHEN('discard'), D(1.4, 'randomEnemy'), DR(1)]), O.raw('카드 부채', [D(0.6, 'allEnemies'), WHEN('discard'), D(0.6, 'allEnemies'), DR(1)]), O.raw('조커', [D(1.0), ST('취약', 1, 'oneEnemy'), WHEN('discard'), D(1.2, 'randomEnemy')])],
        blesses: [B('금박 카드', 'power'), B('마킹 카드', 'weakSpot'), B('트릭 손목', 'draw')] },
      { id: 'u4', name: '관객의 갈채', cost: 1, type: '강화', fx: [ST('공명', 3), STK(K, 1)],
        blurb: '박수가 클수록 트릭은 대담해진다',
        oracles: [O.raw('기립 박수', [ST('공명', 5), STK(K, 1)]), O.raw('작은 박수', [ST('공명', 2)], { cost: 0 }), O.raw('환호성', [ST('공명', 3), STK(K, 1), ST('사기', 1)]), O.raw('앙코르 요청', [ST('공명', 3), STK(K, 1), DR(2)]), O.raw('개막 박수', [ST('공명', 3), STK(K, 1)], { tags: ['개전'] })],
        blesses: [B('무대 매너', 'atkUp'), Bt('커튼 오픈', ['개전']), Bf('휘파람', [DR(1)])] },
    ],
  });
}

// ───────────────────────── 벨리타 ─────────────────────────
{
  const K = '당';
  hero('벨리타', {
    name: '벨리타', nature: '광기', race: '마녀', row: 'back', role: '딜러', star: 3, hp: 550, atk: 145, def: 25, crit: 10,
    blurb: '마녀 여왕. 비밀 창고의 초콜릿을 몰래 꺼내 먹으며 힘을 내지만, 당이 차오르면 손이 떨린다.',
    keyword: {
      name: K, desc: '비밀 간식 창고 — 몰래 먹은 초콜릿. 오를수록 세지고, 셋이면 당쇼크', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: 0.1 }],
      rules: [{ name: '당쇼크', when: { on: 'stackReach', id: K, n: 3 }, fx: [ST('사기', 1), { k: 'later', n: 1, then: [{ k: 'cardStatus', id: '봉쇄', v: 1, to: 'hand', n: 2 }] }, SPA(K)] }],
    },
    passives: [{ name: '몰래 초콜릿', when: { on: 'turnStart' }, fx: [MK('벨리타_t1')] }],
    equips: [{ id: 'eq_velitastaff', name: '벨리타의 지팡이', grade: '전설', slot: '무기', stats: { atk: 26, crit: 4 },
      effect: [{ name: '여왕의 위엄', when: { on: 'ult' }, fx: [{ k: 'dealtMod', v: 0.25, target: 'self', turns: 2 }] }],
      affinity: '벨리타', affinityStats: { hp: 40, atk: 10 },
      affinityEffect: [{ name: '비밀 창고 열쇠', when: { on: 'fightStart' }, fx: [MK('벨리타_t1')] }],
      blurb: '마녀 여왕의 지팡이. 손잡이 속에 초콜릿 한 조각이 숨어 있다.' }],
    ult: { name: '크림슨 레인', cost: 200, fx: [D(0.15, 'allEnemies', { hits: 10 }), MK('벨리타_t1')] },
    ...START.dealer('마녀의 지팡이', '여왕의 마력 폭발', '뿌리의 수호'),
    tokens: [{ id: 't1', name: '몰래 초콜릿', cost: 0, type: '스킬', tags: ['증발'], fx: [AP(1), STK(K, 1)], blurb: '여왕 체면에 대놓고 먹을 수는 없다' }],
    uniques: [
      { id: 'u1', name: '크림슨 스파이크', cost: 1, type: '공격', sig: true, fx: [D(1.15), IFS(K, 2), DR(1)],
        blurb: '당이 오르면 마력이 날카로워진다',
        oracles: [O.up('핏빛 스파이크', 1.35), O.cheap('작은 가시', 0.75), O.raw('달콤한 가시', [D(1.2), IFS(K, 1), DR(1)]), O.plus('여왕의 일격', [ST('취약', 1, 'oneEnemy')], 1.1), O.tag('여왕의 비밀', ['보존'], 1.3)],
        blesses: [B('붉은 마력', 'power'), B('설탕 코팅', 'weakSpot'), B('간식 타임', 'draw')] },
      { id: 'u2', name: '당 충전 마법', cost: 1, type: '공격', fx: [D(0.7), PERS(K), D(0.35)],
        blurb: '당 한 칸에 마력 한 칸',
        oracles: [O.up('과충전', 1.35), O.cheap('살짝 충전', 0.7), O.raw('당 폭발', [D(0.6, 'allEnemies'), PERS(K), D(0.3, 'allEnemies')], { cost: 2 }), O.plus('당 스파크', [ST('약화', 1, 'oneEnemy')], 1.1), O.tag('간식 비축', ['보존'], 1.3)],
        blesses: [B('고당도', 'power'), B('빠른 소화', 'cost'), Bf('사탕 한 알', [STK(K, 1)])] },
      { id: 'u3', name: '핏빛 소나기', cost: 2, type: '공격', fx: [D(0.3, 'randomEnemy', { hits: 6 })],
        blurb: '여왕의 짜증이 비처럼 쏟아진다',
        oracles: [O.up('폭우', 1.3), O.cheap('소나기 한 줄기', 0.7), O.raw('광역 소나기', [D(0.65, 'allEnemies', { hits: 2 })]), O.plus('당 떨어진 짜증', [STK(K, 1)], 1.1), O.big('여왕의 폭풍우', 1.75, [ST('약화', 1, 'allEnemies')])],
        blesses: [B('붉은 비', 'power'), B('비구름', 'cost'), B('천둥', 'frost')] },
      { id: 'u4', name: '여왕의 간식 시간', cost: 1, type: '강화', fx: [ST('사기', 1), STK(K, 1)],
        blurb: '아무도 보지 않을 때가 간식 시간이다',
        oracles: [O.raw('성대한 간식', [ST('사기', 2), STK(K, 1)], { cost: 2 }), O.raw('한 입만', [ST('사기', 1)], { cost: 0 }), O.raw('초콜릿 비축', [ST('사기', 1), STK(K, 1), MK('벨리타_t1')]), O.raw('티타임', [ST('사기', 1), STK(K, 1), DR(1)]), O.raw('아침 간식', [ST('사기', 1), STK(K, 1)], { tags: ['개전'] })],
        blesses: [B('여왕의 위엄', 'atkUp'), Bt('새벽 간식', ['개전']), Bf('여분 초콜릿', [MK('벨리타_t1')])] },
    ],
  });
}

// ───────────────────────── 레비(졸업) ─────────────────────────
{
  const K = '퇴근 도장';
  hero('레비_졸업', {
    name: '레비(졸업)', nature: '활발', race: '마녀', row: 'front', role: '탱커', star: 3, hp: 1000, atk: 75, def: 76, crit: 5,
    blurb: '인턴을 마치고 정식 사원이 된 레비. 소원은 오직 정시 퇴근 — 일을 빨리 끝낼수록 남는 시간이 방패가 된다.',
    keyword: { name: K, desc: '정시 퇴근 — 종극 카드로 찍고, 턴이 끝날 때 남은 AP 만큼 실드로 바꾼다', carrier: 'self', cap: 1 },
    passives: [{ name: '정시 퇴근', when: { on: 'turnEnd' }, conds: [{ c: 'stack', id: K }], fx: [{ k: 'perApLeft' }, SH(0.7), SPA(K)] }],
    ult: { name: '열정 넘치는 신입', cost: 200, fx: [DD(0.2, 'allEnemies', { hits: 5 }), ST('둔화', 2, 'allEnemies'), ST('실드 유지', 1)] },
    ...START.tank('빗자루 후리기', '사원증 가드'),
    uniques: [
      { id: 'u1', name: '칼퇴 선언', cost: 1, type: '스킬', sig: true, tags: ['종극'], fx: [SH(2.4), STK(K, 1)],
        blurb: '「오늘 일은 여기까지입니다!」',
        oracles: [O.up('당당한 칼퇴', 1.35), O.cheap('조기 퇴근', 0.75), O.plus('퇴근길 경계', [ST('반격', 2)], 1.0), O.plus('내일 할 일 정리', [ST('다음 턴 드로우', 1)], 1.0), O.raw('퇴근 준비 완료', [SH(2.6), STK(K, 1), ST('실드 유지', 1)], { tags: ['종극'] })],
        blesses: [B('사원증 방패', 'guard'), Bf('퇴근 도시락', [HE(0.6)]), B('정시 알람', 'cost')] },
      { id: 'u2', name: '업무 마무리', cost: 1, type: '공격', fx: [DD(0.75), ST('실드 유지', 1)],
        blurb: '깔끔하게 끝내야 내일이 편하다',
        oracles: [O.up('완벽한 마무리', 1.35), O.cheap('대충 마무리', 0.75), O.plus('결재 완료', [ST('약화', 1, 'oneEnemy')], 1.1), O.raw('서류 폭탄', [DD(0.55, 'allEnemies'), ST('실드 유지', 1)]), O.tag('업무 일지', ['보존'], 1.3)],
        blesses: [B('빗자루 스윙', 'power'), B('서류 뭉치', 'frost'), Bf('메모', [DR(1)])] },
      { id: 'u3', name: '반차 신청', cost: 0, type: '스킬', tags: ['종극'], fx: [SH(1.0), STK(K, 1)],
        blurb: '오후는 내 시간이다',
        oracles: [O.up('연차 신청', 1.5), O.plus('반차 + 커피', [ST('결의', 1)], 1.0), O.plus('미리 결재', [DR(1)], 1.0), O.raw('재택 근무', [SH(1.1), STK(K, 1), HE(0.6)], { tags: ['종극'] }), O.raw('주 4일제', [SH(1.0), STK(K, 1), ST('다음 턴 드로우', 1)], { tags: ['종극'] })],
        blesses: [B('휴가 계획', 'guard'), Bf('낮잠', [HE(0.5)]), Bf('퇴근 버스', [ST('결의', 1)])] },
      { id: 'u4', name: '정규직 계약서', cost: 1, type: '강화', fx: [ST('결정화', 1), ST('결의', 1)],
        blurb: '드디어 인턴 딱지를 뗐다',
        oracles: [O.raw('연봉 협상', [ST('결정화', 2), ST('결의', 1)]), O.raw('수습 계약', [ST('결정화', 1)], { cost: 0 }), O.raw('복지 포인트', [ST('결정화', 1), ST('결의', 1), HE(0.8)]), O.raw('팀장 승진', [ST('결정화', 1), ST('결의', 2)]), O.raw('입사 첫날', [ST('결정화', 1), ST('결의', 1)], { tags: ['개전'] })],
        blesses: [B('정규직의 여유', 'defUp'), Bt('출근 도장', ['개전']), Bf('사내 식당', [HE(0.6)])] },
    ],
  });
}

// ───────────────────────── 마카샤 ─────────────────────────
{
  const K = '예언';
  hero('마카샤', {
    name: '마카샤', nature: '활발', race: '마녀', row: 'mid', role: '서포터', star: 3, hp: 570, atk: 86, def: 60, crit: 5,
    blurb: '수다쟁이 책략관 마녀. 흘린 말 속에 적의 결말이 이미 적혀 있고, 맞아떨어진 예언은 다음 결말의 재료가 된다.',
    keyword: { name: K, desc: '줄줄 새는 예언 — 모아 두면 「적혀 있던 결말」이 몽땅 꺼내 쓴다', carrier: 'self', cap: 5 },
    passives: [{ name: '수다 속 결말', when: { on: 'stackReach', id: K, n: 3 }, limit: { per: 'turn', n: 1 }, fx: [DR(1)] }],
    ult: { name: '마녀의 움직이는 집', cost: 200, fx: [DD(1.2, 'allEnemies'), ST('취약', 2, 'allEnemies'), STK(K, 2)] },
    ...START.support('절굿공이 톡', '안개 약초차'),
    uniques: [
      { id: 'u1', name: '운명의 시계', cost: 1, type: '스킬', sig: true, fx: [{ k: 'afterCards', n: 3, then: [DD(1.4, 'allEnemies'), ST('취약', 1, 'allEnemies'), STK(K, 1)] }, DR(1)],
        blurb: '시곗바늘 세 칸 뒤에 결말이 온다',
        oracles: [
          O.raw('또렷한 예언', [{ k: 'afterCards', n: 3, then: [DD(1.3, 'allEnemies'), ST('취약', 1, 'allEnemies'), STK(K, 1)] }, DR(1)]),
          O.raw('성급한 예언', [{ k: 'afterCards', n: 2, then: [DD(0.8, 'allEnemies'), ST('취약', 1, 'allEnemies'), STK(K, 1)] }], { cost: 0 }),
          O.raw('겹예언', [{ k: 'afterCards', n: 3, then: [DD(1.0, 'allEnemies'), ST('취약', 2, 'allEnemies'), STK(K, 2)] }, DR(1)]),
          O.raw('예언과 처방', [{ k: 'afterCards', n: 3, then: [DD(1.0, 'allEnemies'), ST('취약', 1, 'allEnemies'), STK(K, 1)] }, DR(1), HE(0.8)]),
          O.raw('이미 본 결말', [{ k: 'afterCards', n: 3, then: [DD(1.15, 'allEnemies'), ST('취약', 1, 'allEnemies'), STK(K, 1)] }, DR(1)], { tags: ['개전'] }),
        ],
        blesses: [B('수정 구슬', 'draw'), Bt('예지몽', ['개전']), Bf('덤으로 한마디', [ST('약화', 1, 'oneEnemy')])] },
      { id: 'u2', name: '적혀 있던 결말', cost: 1, type: '공격', fx: [DD(0.6), PERS(K), DD(0.3, 'allEnemies'), SPA(K)],
        blurb: '「그러니까 말했잖아~」',
        oracles: [O.up('결말 낭독', 1.35), O.cheap('짧은 결말', 0.7), O.raw('남겨 둔 결말', [DD(0.6), PERS(K), DD(0.3, 'allEnemies'), SP(K, 1)]), O.plus('비극의 결말', [], 1.25), O.tag('책략관의 각본', ['보존'], 1.3)],
        blesses: [B('명대사', 'power'), B('복선', 'weakSpot'), Bf('에필로그', [DR(1)])] },
      { id: 'u3', name: '수다 한 바가지', cost: 0, type: '스킬', fx: [{ k: 'afterCards', n: 2, then: [ST('취약', 1, 'allEnemies'), STK(K, 1)] }, HE(0.7)],
        blurb: '흘린 말 속에 힌트가 섞여 있다',
        oracles: [O.raw('수다 두 바가지', [{ k: 'afterCards', n: 2, then: [ST('취약', 1, 'allEnemies'), STK(K, 2)] }, HE(0.8)]), O.raw('귓속말', [{ k: 'afterCards', n: 2, then: [ST('취약', 1, 'allEnemies'), STK(K, 1)] }, HE(0.7), DR(1)]), O.raw('잔소리', [{ k: 'afterCards', n: 2, then: [ST('취약', 1, 'allEnemies'), ST('약화', 1, 'allEnemies'), STK(K, 1)] }, HE(0.7)]), O.raw('약초 수다', [{ k: 'afterCards', n: 2, then: [ST('취약', 1, 'allEnemies'), STK(K, 1)] }, HE(1.2)]), O.raw('수다 비축', [{ k: 'afterCards', n: 2, then: [ST('취약', 1, 'allEnemies'), STK(K, 1)] }, HE(0.8)], { tags: ['보존'] })],
        blesses: [B('약초 향', 'heal'), Bt('말주머니', ['보존']), B('입담', 'draw')] },
      { id: 'u4', name: '책략관의 탁상', cost: 1, type: '강화', fx: [STK(K, 2), ST('사기', 1)],
        blurb: '모든 수가 다 보이는 탁상',
        oracles: [O.raw('완벽한 책략', [STK(K, 3), ST('사기', 1)]), O.raw('작은 탁상', [STK(K, 2)], { cost: 0 }), O.raw('책략과 약초', [STK(K, 2), ST('사기', 1), HE(0.8)]), O.raw('수 읽기', [STK(K, 2), ST('사기', 1), DR(1)]), O.raw('미리 펼친 지도', [STK(K, 2), ST('사기', 1)], { tags: ['개전'] })],
        blesses: [B('책략가', 'defUp'), Bt('먼저 펼친 판', ['개전']), Bf('찻잔', [HE(0.5)])] },
    ],
  });
}

// ───────────────────────── 스노키 ─────────────────────────
{
  const K = '두유';
  hero('스노키', {
    name: '스노키', nature: '우울', race: '마녀', row: 'front', role: '탱커', star: 3, hp: 920, atk: 101, def: 67, crit: 5,
    blurb: '두유 유통 조직의 전 두목, 지금은 경호원 겸 오른팔. 동료가 주먹을 내밀 때마다 두유 한 병씩 건넨다.',
    keyword: {
      name: K, desc: '두유 배달 — 다른 아군이 공격할 때마다 한 병, 한 턴에 세 병이면 반격', carrier: 'self', cap: 3, endClear: true,
      rules: [{ name: '세 병째', when: { on: 'stackReach', id: K, n: 3 }, fx: [ST('반격', 1)] }],
    },
    passives: [{ name: '두유 배달', when: { on: 'play', who: 'other', type: '공격' }, limit: { per: 'turn', n: 3 }, fx: [SH(0.4), STK(K, 1)] }],
    ult: { name: '구역 점거', cost: 300, fx: [DD(1.6, 'allEnemies'), ST('둔화', 2, 'allEnemies'), ST('반격', 2)] },
    ...START.tank('절도 있는 발차기', '경호 자세'),
    uniques: [
      { id: 'u1', name: '경호원 출동', cost: 1, type: '스킬', sig: true, fx: [SH(1.9), ST('반격', 1)],
        blurb: '보스 앞에는 언제나 스노키가 선다',
        oracles: [O.up('철통 경호', 1.35), O.cheap('몸으로 막기', 0.75), O.plus('두목의 위엄', [ST('반격', 1)], 1.0), O.plus('경호 교대', [STK(K, 1)], 1.1), O.tag('대기 중인 경호원', ['보존'], 1.3)],
        blesses: [B('방탄 코트', 'guard'), Bf('선글라스', [ST('피해 감소', 1)]), B('무전기', 'draw')] },
      { id: 'u2', name: '두유 상자 던지기', cost: 1, type: '공격', fx: [DD(0.75), STK(K, 1)],
        blurb: '한 상자에 스물네 병. 맞으면 아프다',
        oracles: [O.up('대형 상자', 1.35), O.cheap('한 병 던지기', 0.7), O.raw('상자째 투척', [DD(0.55, 'allEnemies'), STK(K, 1)]), O.plus('냉장 두유', [ST('약화', 1, 'oneEnemy')], 1.1), O.big('트럭째 배달', 1.8, [STK(K, 1)])],
        blesses: [B('꽉 찬 상자', 'power'), B('차가운 두유', 'frost'), B('빠른 배달', 'cost')] },
      { id: 'u3', name: '오른팔의 신호', cost: 0, type: '스킬', fx: [ST('협공', 1), STK(K, 1)],
        blurb: '눈짓 한 번이면 조직이 움직인다',
        oracles: [O.raw('합동 작전', [ST('협공', 2), STK(K, 1)]), O.raw('신호와 엄호', [ST('협공', 1), STK(K, 1), SH(0.8)]), O.raw('작전 지시', [ST('협공', 1), STK(K, 1), DR(1)]), O.raw('두 병 건네기', [ST('협공', 1), STK(K, 2)]), O.raw('잠복 신호', [ST('협공', 1), STK(K, 1)], { tags: ['보존'] })],
        blesses: [Bf('수신호', [DR(1)]), Bt('비밀 암호', ['보존']), Bf('두유 한 병', [SH(0.5)])] },
      { id: 'u4', name: '불법 유통망', cost: 1, type: '강화', fx: [ST('결정화', 1), ST('반격', 2)],
        blurb: '은퇴했어도 연락망은 살아 있다',
        oracles: [O.raw('전국 유통망', [ST('결정화', 2), ST('반격', 2)]), O.raw('동네 유통', [ST('결정화', 1)], { cost: 0 }), O.raw('경호 계약', [ST('결정화', 1), ST('반격', 4)]), O.raw('유통 장부', [ST('결정화', 1), ST('반격', 2), DR(1)]), O.raw('새벽 배송', [ST('결정화', 1), ST('반격', 2)], { tags: ['개전'] })],
        blesses: [B('전 두목', 'defUp'), Bt('새벽 출근', ['개전']), Bf('비상 두유', [SH(0.6)])] },
    ],
  });
}

// ───────────────────────── 아사나 ─────────────────────────
{
  const K = '명상';
  hero('아사나', {
    name: '아사나', nature: '우울', race: '마녀', row: 'front', role: '탱커', star: 3, hp: 1000, atk: 90, def: 67, crit: 5,
    blurb: '볼-요가 전도사. 스킬로 숨을 고르면 명상에 들어 파티가 덜 다치고, 명상을 깨는 첫 일격은 두 배로 무겁다.',
    keyword: { name: K, desc: '명상 자세 — 고유 스킬로 들어가 덜 아프고, 다음 공격 카드에 힘을 싣고 풀린다', carrier: 'self', cap: 1, consumeAll: true, per: [{ stat: 'taken', v: -0.05 }, { stat: 'dealt', v: 0.6 }] },
    ult: { name: '명상 시간', cost: 250, fx: [DD(0.25, 'allEnemies', { hits: 6 }), HE(2.0), STK(K, 1)] },
    ...START.tank('요가 킥', '나무 자세'),
    uniques: [
      { id: 'u1', name: '볼-요가 시간', cost: 1, type: '스킬', sig: true, fx: [SH(1.6), HE(0.7), STK(K, 1)],
        blurb: '숨 들이쉬고… 볼을 부풀리고… 내쉬고',
        oracles: [O.up('깊은 호흡', 1.35), O.cheap('가벼운 스트레칭', 0.75), O.raw('단체 요가', [SH(1.6), ST('결의', 1), STK(K, 1)]), O.raw('명상 음악', [SH(1.7), ST('피해 감소', 1), STK(K, 1)]), O.tag('요가 매트', ['보존'], 1.3)],
        blesses: [B('고요한 마음', 'guard'), B('요가 블록', 'heal'), Bt('매트 깔기', ['개전'])] },
      { id: 'u2', name: '고요한 일격', cost: 2, type: '공격', fx: [DD(1.2), ST('약화', 1, 'oneEnemy')],
        blurb: '명상을 깨는 단 한 번의 동작',
        oracles: [O.up('천천히, 깊게', 1.3), O.cheap('짧은 동작', 0.7), O.plus('균형의 일격', [{ k: 'tough', v: 1 }], 1.05), O.raw('전신 동작', [DD(0.9, 'allEnemies'), ST('약화', 1, 'allEnemies')]), O.tag('기다린 일격', ['보존'], 1.3)],
        blesses: [B('단전의 힘', 'power'), B('흐름', 'frost'), B('간결한 동작', 'cost')] },
      { id: 'u3', name: '호흡 고르기', cost: 0, type: '스킬', fx: [ST('피해 감소', 1), DR(1), STK(K, 1)],
        blurb: '들숨 넷, 날숨 넷',
        oracles: [O.raw('긴 호흡', [ST('피해 감소', 2), DR(1), STK(K, 1)]), O.raw('복식 호흡', [SH(0.8), DR(1), STK(K, 1)]), O.raw('맑은 정신', [ST('피해 감소', 1), DR(2), STK(K, 1)]), O.raw('명상 준비', [ST('결의', 1), DR(1), STK(K, 1)]), O.raw('잠깐 쉬기', [ST('피해 감소', 1), DR(1), STK(K, 1)], { tags: ['보존'] })],
        blesses: [B('평온', 'draw'), Bf('따뜻한 차', [HE(0.4)]), Bt('조용한 방', ['보존'])] },
      { id: 'u4', name: '볼-요가 전도', cost: 1, type: '강화', fx: [ST('피해 감소', 2), ST('결의', 1)],
        blurb: '함께하면 모두가 말랑해진다',
        oracles: [O.raw('요가 교실', [ST('불굴', 1), ST('결의', 2)]), O.raw('혼자 요가', [ST('불굴', 1)], { cost: 0 }), O.raw('평정심', [ST('불굴', 1), ST('결의', 1), HE(0.8)]), O.raw('굳건한 자세', [ST('불굴', 2), ST('결의', 1)], { cost: 2 }), O.raw('아침 요가', [ST('불굴', 1), ST('결의', 1)], { tags: ['개전'] })],
        blesses: [B('유연함', 'defUp'), Bt('새벽 수련', ['개전']), Bf('요가 볼', [SH(0.6)])] },
    ],
  });
}

// ───────────────────────── 포셔 ─────────────────────────
{
  const K = '약효';
  const C = ['보통', '진하게'];
  const bitter = MK('포셔_t1', 1, 'draw');
  hero('포셔', {
    name: '포셔', nature: '우울', race: '마녀', row: 'back', role: '서포터', star: 3, hp: 580, atk: 75, def: 58, crit: 5,
    blurb: '맛은 무시, 효과는 확실한 약장수 마녀. 진하게 달인 약은 잘 듣지만 쓴맛이 덱에 남는다.',
    keyword: { name: K, desc: '잘 듣는 약일수록 — 카드를 만들 때마다 쌓여 턴이 끝날 때 파티를 회복한다', carrier: 'self', cap: 5, per: [{ stat: 'hot', ratio: 0.15 }] },
    passives: [{ name: '약효 남기기', when: { on: 'make' }, fx: [STK(K, 1)] }],
    ult: { name: '감자 고구마!', cost: 150, fx: [ST('기절', 1, 'oneEnemy'), ST('기절', 1, 'randomEnemy'), STK(K, 1)] },
    ...START.support('약병 투척', '초록 물약'),
    tokens: [{ id: 't1', name: '쓴맛', cost: 0, type: '스킬', tags: ['사용 불가', '증발'], fx: [], blurb: '혀가 기억하는 맛. 손에 들고 있으면 아무것도 못 한다' }],
    uniques: [
      { id: 'u1', name: '만병통치 물약', cost: 1, type: '스킬', sig: true, choices: C, fx: [IFC(1), HE(1.4), IFC(2), HE(2.1), bitter],
        blurb: '진하게 달일수록 잘 듣는다. 맛은 책임지지 않는다',
        oracles: [
          O.raw('특제 물약', [IFC(1), HE(1.8), IFC(2), HE(2.7), bitter]),
          O.raw('희석 물약', [IFC(1), HE(1.0), IFC(2), HE(1.5), bitter], { cost: 0 }),
          O.raw('보약', [IFC(1), HE(1.5), IFC(2), HE(2.3), bitter], { tags: ['축복'] }),
          O.raw('쓴 약이 몸에 좋다', [IFC(1), HE(1.4), IFC(2), HE(2.8), bitter]),
          O.raw('상비약', [IFC(1), HE(1.6), IFC(2), HE(2.4), bitter], { tags: ['보존'] }),
        ],
        blesses: [B('약초 한 줌', 'heal'), Bt('약 상자', ['보존']), B('약사의 손', 'draw')] },
      { id: 'u2', name: '폭발 물약 투척', cost: 1, type: '공격', choices: C, fx: [IFC(1), DD(0.55, 'allEnemies'), IFC(2), DD(0.85, 'allEnemies'), bitter],
        blurb: '원래는 감기약이었다',
        oracles: [
          O.raw('대폭발 물약', [IFC(1), DD(0.7, 'allEnemies'), IFC(2), DD(1.05, 'allEnemies'), bitter]),
          O.raw('작은 약병', [IFC(1), DD(0.4, 'allEnemies'), IFC(2), DD(0.6, 'allEnemies'), bitter], { cost: 0 }),
          O.raw('부식 물약', [IFC(1), DD(0.65, 'allEnemies'), IFC(2), DD(1.0, 'allEnemies'), bitter], { tags: ['분쇄'] }),
          O.raw('끈적 물약', [IFC(1), DD(0.6, 'allEnemies'), IFC(2), DD(0.95, 'allEnemies'), bitter], { tags: ['약점 공격'] }),
          O.raw('한 방 물약', [IFC(1), DD(1.1), IFC(2), DD(1.7), bitter]),
        ],
        blesses: [B('독한 약', 'power'), B('흩날리는 가루', 'frost'), B('빠른 투척', 'cost')] },
      { id: 'u3', name: '흐물 약화 포션', cost: 1, type: '스킬', choices: C, fx: [IFC(1), ST('약화', 1, 'allEnemies'), IFC(2), ST('약화', 2, 'allEnemies'), bitter],
        blurb: '마시면 힘이 쭉 빠진다 — 적에게 먹이면 된다',
        oracles: [
          O.raw('흐물흐물 포션', [IFC(1), ST('약화', 2, 'allEnemies'), IFC(2), ST('약화', 3, 'allEnemies'), bitter]),
          O.raw('묽은 포션', [IFC(1), ST('약화', 1, 'oneEnemy'), IFC(2), ST('약화', 2, 'oneEnemy'), bitter], { cost: 0 }),
          O.raw('나른 포션', [IFC(1), ST('약화', 1, 'allEnemies'), IFC(2), ST('약화', 2, 'allEnemies'), bitter], { tags: ['보존'] }),
          O.raw('쓰디쓴 포션', [IFC(1), ST('취약', 1, 'allEnemies'), IFC(2), ST('취약', 2, 'allEnemies'), bitter]),
          O.raw('비상 포션', [IFC(1), ST('약화', 1, 'allEnemies'), IFC(2), ST('약화', 2, 'allEnemies'), bitter], { tags: ['개전'] }),
        ],
        blesses: [Bf('수면 가루', [ST('둔화', 1, 'oneEnemy')]), B('약효 강화', 'draw'), Bt('약병 선반', ['보존'])] },
      { id: 'u4', name: '약장수의 비법서', cost: 1, type: '강화', fx: [STK(K, 2), ST('면역', 1)],
        blurb: '부작용 목록이 효능 목록보다 길다',
        oracles: [O.raw('비법서 완본', [STK(K, 3), ST('면역', 1)]), O.raw('비법 메모', [STK(K, 2)], { cost: 0 }), O.raw('임상 기록', [STK(K, 2), ST('면역', 2)]), O.raw('약초 도감', [STK(K, 2), ST('면역', 1), HE(0.8)]), O.raw('개업 준비', [STK(K, 2), ST('면역', 1)], { tags: ['개전'] })],
        blesses: [B('명의', 'defUp'), Bt('간판', ['개전']), Bf('감초', [HE(0.5)])] },
    ],
  });
}

// ───────────────────────── 레비 ─────────────────────────
{
  const K = '괴력 봉인';
  hero('레비', {
    name: '레비', nature: '우울', race: '마녀', row: 'mid', role: '딜러', star: 2, hp: 590, atk: 125, def: 27, crit: 10,
    blurb: '약화 포션으로 괴력을 눌러 두고 사는 인턴 마녀. 스킬로 봉인을 하나씩 풀다 보면, 다 풀린 단도가 적 전체를 벤다.',
    keyword: { name: K, desc: '눌러 둔 괴력 — 스킬 카드로 봉인을 풀고, 다 풀리면 고유 공격 카드가 적 전체로 번진다(그 뒤 1 다시)', carrier: 'self', cap: 3, per: [{ stat: 'dealt', v: -0.05 }] },
    passives: [
      { name: '포션 복용', when: { on: 'fightStart' }, fx: [STK(K, 2)] },
      { name: '봉인 풀기', when: { on: 'play', type: '스킬' }, fx: [SP(K, 1)] },
    ],
    equips: [{ id: 'eq_levidagger', name: '레비의 단도', grade: '전설', slot: '무기', stats: { atk: 24, crit: 5 },
      effect: [{ name: '버티고 베기', when: { on: 'lowHp', pct: 0.3 }, fx: [ST('피해 감소', 3), { k: 'dealtMod', v: 0.3, target: 'self', run: true }] }],
      affinity: '레비', affinityStats: { hp: 40, atk: 10 },
      affinityEffect: [{ name: '참았던 괴력', when: { on: 'lowHp', pct: 0.3 }, fx: [SPA('괴력 봉인')] }],
      blurb: '단도라 부르기엔 너무 큰 칼. 몰리면 봉인이 저절로 풀린다.' }],
    ult: { name: '레비드 더 레드', cost: 250, fx: [D(2.2, 'allEnemies'), SPA(K)] },
    ...START.dealer('단도 찌르기', '단도 깊숙이 찌르기', '앞치마 방어'),
    uniques: [
      { id: 'u1', name: '포션 깜빡', cost: 0, type: '스킬', sig: true, fx: [SP(K, 1), DR(1)],
        blurb: '아침 약을 깜빡했다. 큰일이다 — 적에게',
        oracles: [O.raw('포션 쏟기', [SP(K, 2), DR(1)]), O.raw('포션 바꿔치기', [SP(K, 1), DR(2)]), O.raw('해독제', [SP(K, 1), DR(1), ST('사기', 1)], { cost: 1 }), O.raw('빈 병', [SP(K, 1), DR(1), SH(0.8)]), O.raw('주머니 속 빈 병', [SP(K, 1), DR(1)], { tags: ['보존'] })],
        blesses: [B('깜빡깜빡', 'draw'), Bt('앞치마 주머니', ['보존']), Bf('기지개', [SH(0.5)])] },
      { id: 'u2', name: '단도 난무', cost: 1, type: '공격', fx: [D(1.3), IFNOT(K), D(1.0, 'allEnemies'), STK(K, 1)],
        blurb: '봉인이 풀린 손은 멈추지 않는다',
        oracles: [O.up('피의 난무', 1.3), O.cheap('단도 한 번', 0.75), O.plus('빈틈 찌르기', [], 1.2), O.tag('붉은 궤적', ['약점 공격'], 1.15), O.tag('숨긴 단도', ['보존'], 1.25)],
        blesses: [B('붉은 칼날', 'power'), B('급소', 'weakSpot'), B('가벼운 단도', 'cost')] },
      { id: 'u3', name: '인턴의 각성', cost: 2, type: '공격', fx: [D(2.4), IFNOT(K), D(1.7, 'allEnemies'), STK(K, 1)],
        blurb: '참고 참다가 터진 인턴은 무섭다',
        oracles: [O.up('완전 각성', 1.3), O.cheap('반쯤 각성', 0.7), O.tag('붉은 폭주', ['분쇄'], 1.2), O.plus('각성의 여파', [], 1.2), O.tag('참았던 한 방', ['보존'], 1.25)],
        blesses: [B('괴력', 'power'), B('분노', 'frost'), B('조급함', 'cost')] },
      { id: 'u4', name: '괴력 조절 훈련', cost: 1, type: '강화', fx: [SP(K, 2), ST('사기', 1)],
        blurb: '조절만 하면 이 힘도 쓸모가 있다',
        oracles: [O.raw('완전 조절', [SPA(K), ST('사기', 1)]), O.raw('가벼운 훈련', [SP(K, 1), DR(1)], { cost: 0 }), O.raw('훈련과 다짐', [SP(K, 2), ST('사기', 1), DR(1)]), O.raw('집중 훈련', [SP(K, 2), ST('사기', 1), ST('잔광', 2)]), O.raw('출근 전 훈련', [SP(K, 2), ST('사기', 1)], { tags: ['개전'] })],
        blesses: [B('붉은 머리', 'atkUp'), Bt('이른 출근', ['개전']), Bf('심호흡', [DR(1)])] },
    ],
  });
}

// ───────────────────────── 바리에 ─────────────────────────
{
  const K = '대출 도장';
  hero('바리에', {
    name: '바리에', nature: '우울', race: '마녀', row: 'back', role: '서포터', star: 2, hp: 460, atk: 64, def: 55, crit: 5,
    blurb: '「정숙해주세요오」 도서관 사서 마녀. 버린 더미에서 쓸 만한 책을 당일 대출해 손에 돌려 주고, 도장이 차면 서가를 정리한다.',
    keyword: {
      name: K, desc: '당일 반납 — 버린 더미에서 카드를 대출할 때마다 찍고, 셋이면 서가 정리', carrier: 'self', cap: 3,
      rules: [{ name: '서가 정리', when: { on: 'stackReach', id: K, n: 3 }, fx: [ST('다음 턴 드로우', 2), HE(0.8), SPA(K)] }],
    },
    ult: { name: '당일 반납해주세요오', cost: 200, fx: [{ k: 'pull', from: 'discard', n: 2 }, STK(K, 2), { k: 'atkMod', v: 0.15, run: true, target: 'oneAlly' }] },
    ...START.support('잉크 발사', '열람실 휴식'),
    uniques: [
      { id: 'u1', name: '도서 대출', cost: 0, type: '스킬', sig: true, fx: [{ k: 'pull', from: 'discard', n: 1 }, STK(K, 1)],
        blurb: '대출 기한은 오늘까지예요오',
        oracles: [O.raw('두 권 대출', [{ k: 'pull', from: 'discard', n: 2 }, STK(K, 1)]), O.raw('대출과 추천', [{ k: 'pull', from: 'discard', n: 1 }, STK(K, 1), DR(1)]), O.raw('연장 대출', [{ k: 'pull', from: 'discard', n: 1 }, STK(K, 2)]), O.raw('조용한 대출', [{ k: 'pull', from: 'discard', n: 1 }, STK(K, 1), HE(0.6)]), O.raw('예약 도서', [{ k: 'pull', from: 'discard', n: 1 }, STK(K, 1)], { tags: ['보존'] })],
        blesses: [B('추천 도서', 'draw'), Bt('예약 선반', ['보존']), Bf('책갈피', [HE(0.4)])] },
      { id: 'u2', name: '잉크 폭탄', cost: 1, type: '공격', fx: [DD(0.85), ST('약화', 1, 'oneEnemy')],
        blurb: '도서관에서 떠드는 자에게',
        oracles: [O.up('먹물 폭탄', 1.4), O.cheap('잉크 한 방울', 0.7), O.raw('잉크 범벅', [DD(0.55, 'allEnemies'), ST('약화', 1, 'allEnemies')]), O.plus('지워지지 않는 잉크', [ST('취약', 1, 'oneEnemy')], 1.1), O.plus('대출 연체 고지서', [STK(K, 1)], 1.1)],
        blesses: [B('진한 잉크', 'power'), B('번지는 잉크', 'frost'), B('만년필', 'cost')] },
      { id: 'u3', name: '정숙해주세요오', cost: 1, type: '스킬', fx: [ST('약화', 1, 'allEnemies'), HE(0.9), STK(K, 1)],
        blurb: '사서의 한마디에 열람실이 얼어붙는다',
        oracles: [O.raw('엄중 경고', [ST('약화', 2, 'allEnemies'), HE(0.9), STK(K, 1)]), O.raw('쉿', [ST('약화', 1, 'allEnemies'), HE(0.6)], { cost: 0 }), O.raw('열람실 휴식', [ST('약화', 1, 'allEnemies'), HE(1.4), STK(K, 1)]), O.raw('퇴실 조치', [ST('약화', 1, 'allEnemies'), ST('둔화', 1, 'allEnemies'), STK(K, 1)]), O.raw('정숙 표지판', [ST('약화', 1, 'allEnemies'), HE(1.0), STK(K, 1)], { tags: ['보존'] })],
        blesses: [B('고요', 'heal'), Bf('눈빛', [ST('둔화', 1, 'oneEnemy')]), Bt('표지판', ['보존'])] },
      { id: 'u4', name: '사서의 서가 정리', cost: 1, type: '강화', fx: [ST('결의', 1), STK(K, 2)],
        blurb: '모든 책은 제자리에',
        oracles: [O.raw('대청소', [ST('결의', 2), STK(K, 2)]), O.raw('책 한 칸', [ST('결의', 1), STK(K, 1)], { cost: 0 }), O.raw('분류 번호', [ST('결의', 1), STK(K, 2), DR(1)]), O.raw('신착 도서', [ST('결의', 1), STK(K, 2), ST('다음 턴 드로우', 1)]), O.raw('개관 준비', [ST('결의', 1), STK(K, 2)], { tags: ['개전'] })],
        blesses: [B('꼼꼼함', 'defUp'), Bt('개관', ['개전']), Bf('반납함', [DR(1)])] },
    ],
  });
}

// ───────────────────────── 요미(미스틱) ─────────────────────────
{
  const K = '달의 위상';
  hero('요미', {
    name: '요미', nature: '우울', race: '미스틱', row: 'mid', role: '딜러', star: 3, hp: 720, atk: 146, def: 29, crit: 10,
    blurb: '누구도 모시지 않는 달을 홀로 섬겨 온 사제. 턴마다 달이 그믐 → 초승 → 반달 → 보름으로 차오르고, 스킬로 기도하면 한 칸 앞당긴다.',
    keyword: {
      name: K, desc: '턴마다 한 칸 도는 달 — 1 그믐(고유 카드 AP +1) · 2~3 초승 · 반달(드로우 1) · 4 보름(피해 +60%)', carrier: 'self', cap: 4, wrap: true,
      rules: [{ name: '보름달', when: { on: 'stackReach', id: K, n: 4 }, fx: [HE(0.6)] }],
    },
    passives: [
      { name: '달의 순환', when: { on: 'turnStart' }, fx: [STK(K, 1)] },
      { name: '만월의 사제', when: { on: 'always' }, conds: [{ c: 'stack', id: K, n: 4 }], fx: [{ k: 'dealtMod', v: 0.6, target: 'self' }] },
    ],
    ult: { name: '지극정성의 마중', cost: 200, fx: [D(1.0, 'allEnemies'), ST('둔화', 1, 'allEnemies'), AP(1)] },
    ...START.dealer('달빛 탄', '만월 탄', '사제의 베일'),
    uniques: [
      { id: 'u1', name: '달빛 사격', cost: 1, type: '공격', sig: true, fx: [D(1.25), IFS(K, 1, 1), AP(1), IFS(K, 2, 3), DR(1)],
        blurb: '달이 어떤 얼굴이든 사제의 손은 흔들리지 않는다',
        oracles: [O.up('은빛 사격', 1.35), O.cheap('희미한 달빛', 0.7), O.raw('달무리', [D(0.8, 'allEnemies'), IFS(K, 1, 1), AP(1), IFS(K, 2, 3), DR(1)], { cost: 2 }), O.tag('달빛 표적', ['약점 공격'], 1.15), O.tag('달 아래 대기', ['보존'], 1.3)],
        blesses: [B('은빛 탄', 'power'), B('달그림자', 'weakSpot'), B('밤하늘', 'draw')] },
      { id: 'u2', name: '그믐의 화살', cost: 2, type: '공격', fx: [D(0.8, 'oneEnemy', { hits: 3 }), IFS(K, 1, 1), AP(1)],
        blurb: '달이 숨은 밤의 화살은 보이지 않는다',
        oracles: [O.up('칠흑의 화살', 1.3), O.cheap('그림자 화살', 0.7), O.raw('흩어지는 별', [D(0.45, 'allEnemies', { hits: 3 }), IFS(K, 1, 1), AP(1)]), O.plus('밤의 장막', [], 1.25), O.tag('기다린 밤', ['보존'], 1.25)],
        blesses: [B('어둠', 'power'), B('달 없는 밤', 'cost'), B('서늘한 바람', 'frost')] },
      { id: 'u3', name: '달을 섬기는 기도', cost: 0, type: '스킬', fx: [STK(K, 1), DR(1)],
        blurb: '아무도 모시지 않는 달에게, 오늘도 홀로',
        oracles: [O.raw('간절한 기도', [STK(K, 2), DR(1)]), O.raw('밤샘 기도', [STK(K, 1), DR(2)]), O.raw('달빛 축복', [STK(K, 1), DR(1), HE(0.8)]), O.raw('사제의 베일', [STK(K, 1), DR(1), SH(1.2)]), O.raw('묵상', [STK(K, 1), DR(1)], { tags: ['보존'] })],
        blesses: [B('은방울', 'draw'), Bf('달빛 한 줄기', [HE(0.4)]), Bt('제단', ['보존'])] },
      { id: 'u4', name: '홀로 섬긴 사제', cost: 1, type: '강화', fx: [STK(K, 1), { k: 'atkMod', v: 0.15, run: true, target: 'self' }],
        blurb: '모시는 이 없어도 달은 차오른다',
        oracles: [O.raw('만월의 서약', [STK(K, 1), { k: 'atkMod', v: 0.2, run: true, target: 'self' }]), O.raw('작은 서약', [{ k: 'atkMod', v: 0.1, run: true, target: 'self' }], { cost: 0 }), O.raw('사제의 인내', [STK(K, 1), { k: 'atkMod', v: 0.15, run: true, target: 'self' }, DR(1)]), O.raw('달의 가호', [STK(K, 1), { k: 'atkMod', v: 0.15, run: true, target: 'self' }, ST('피해 감소', 2)]), O.raw('첫 기도', [STK(K, 1), { k: 'atkMod', v: 0.15, run: true, target: 'self' }], { tags: ['개전'] })],
        blesses: [B('사제의 각오', 'atkUp'), Bt('새벽 예배', ['개전']), Bf('촛불', [DR(1)])] },
    ],
  });
}

// ───────────────────────── 비비(신성) ─────────────────────────
{
  const K = '다정한 세상';
  hero('비비_신성', {
    name: '비비(신성)', nature: '공명', race: '미스틱', row: 'mid', role: '딜러', star: 3, hp: 720, atk: 135, def: 29, crit: 10,
    blurb: '새 세계수가 된 비비. 동료가 함께 움직일수록 빛이 짙어지고, 셋이 다 손을 내민 턴에는 그 빛이 파티의 사기가 된다.',
    keyword: { name: K, desc: '이번 턴 다른 아군이 카드를 낼 때마다 쌓이는 온기 — 자신의 피해가 오른다', carrier: 'self', cap: 3, endClear: true, per: [{ stat: 'dealt', v: 0.2 }] },
    passives: [{ name: '함께 걷는 세상', when: { on: 'play', who: 'other' }, limit: { per: 'turn', n: 3 }, fx: [STK(K, 1)] }],
    ult: { name: '모든 이를 굽어살피리', cost: 300, fx: [D(2.5, 'allEnemies'), ST('피해 감소', 3), ST('사기', 1)] },
    ...START.dealer('빛구슬', '빛구슬 세례', '세계수 잎사귀'),
    uniques: [
      { id: 'u1', name: '셋의 몫', cost: 1, type: '공격', sig: true, fx: [D(1.15), { k: 'ifAllHeroes' }, ST('사기', 1)],
        blurb: '혼자 쥔 빛보다 셋이 나눈 빛이 밝다',
        oracles: [O.up('넷의 몫', 1.35), O.cheap('작은 몫', 0.75), O.raw('나눈 빛', [D(0.8, 'allEnemies'), { k: 'ifAllHeroes' }, ST('사기', 1)], { cost: 2 }), O.plus('다정한 몫', [DR(1)], 1.0), O.tag('남겨 둔 몫', ['보존'], 1.3)],
        blesses: [B('따뜻한 빛', 'power'), B('잎사귀 바람', 'draw'), Bf('새싹', [HE(0.4)])] },
      { id: 'u2', name: '모두의 빛', cost: 2, type: '공격', fx: [D(1.0, 'allEnemies'), { k: 'ifAllHeroes' }, AP(1)],
        blurb: '세계수의 빛은 누구도 빠뜨리지 않는다',
        oracles: [O.up('찬란한 빛', 1.3), O.cheap('작은 빛', 0.7), O.plus('빛의 장막', [ST('피해 감소', 1)], 1.05), O.raw('정화의 빛', [D(1.0, 'allEnemies'), { k: 'cleanse', v: 1 }, { k: 'ifAllHeroes' }, AP(1)]), O.tag('기다리는 빛', ['보존'], 1.25)],
        blesses: [B('세계수의 가지', 'power'), B('빛 가루', 'cost'), Bf('잎맥', [HE(0.5)])] },
      { id: 'u3', name: '손잡기', cost: 0, type: '스킬', fx: [STK(K, 1), DR(1)],
        blurb: '헤어지는 건 싫으니까',
        oracles: [O.raw('꼭 잡은 손', [STK(K, 2), DR(1)]), O.raw('둘러앉기', [STK(K, 1), DR(2)]), O.raw('포옹', [STK(K, 1), DR(1), HE(0.8)]), O.raw('지켜 주기', [STK(K, 1), DR(1), SH(1.2)]), O.raw('놓지 않는 손', [STK(K, 1), DR(1)], { tags: ['보존'] })],
        blesses: [B('온기', 'draw'), Bf('토닥토닥', [HE(0.4)]), Bt('약속', ['보존'])] },
      { id: 'u4', name: '새 세계수의 서약', cost: 1, type: '강화', fx: [ST('협공', 2), STK(K, 1)],
        blurb: '모두가 함께 움직이는 세상을 위하여',
        oracles: [O.raw('영원한 서약', [ST('협공', 3), STK(K, 1)]), O.raw('작은 서약', [ST('협공', 1)], { cost: 0 }), O.raw('함께하는 서약', [ST('협공', 2), STK(K, 1), ST('사기', 1)]), O.raw('지키는 서약', [ST('협공', 2), STK(K, 1), ST('피해 감소', 2)]), O.raw('첫 서약', [ST('협공', 2), STK(K, 1)], { tags: ['개전'] })],
        blesses: [B('세계수의 뿌리', 'atkUp'), Bt('아침 햇살', ['개전']), Bf('새잎', [DR(1)])] },
    ],
  });
}

console.log('ok');
