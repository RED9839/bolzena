// 박자형 · 활발 — 루드 · 레비(졸업) · 슈팡(만듦) · 아르코(만들며 씀) · 티그 · 카렌(씀)
const L = require('./lib');
const { E1, EA, ER, D, DD, BL, SH, HL, ST, RH, KS, SP, SPR, DRAW, AP, NC, TOUGH, RUSH, STRIP, ATK, DEF, CRIT, LINK, PREV, IFR, PERR, PER, IFS, CHAIN, card, B, write } = L;
const REP = { k: 'ifRepeat' };

// ── 티그 ───────────────────────────────────────────────
{
  const h = '티그';
  const hero = {
    id: h, name: '티그', nature: '활발', race: '수인', row: 'front', role: '딜러', style: '박자형', star: 3,
    hp: 820, atk: 115, def: 40, crit: 10,
    blurb: '백호 검성. 제 칼을 끊기지 않고 이어 벨수록 칼끝이 깊어지고, 파티가 깔아 둔 박자를 한 번에 쓸어 담는다.',
    keyword: {
      name: '연격', desc: '쌍검이 이어 붙는 박자 — 이어 벤 만큼 칼끝이 깊어지지만, 남의 손이 끼면 처음부터',
      carrier: 'self', cap: 3, decayAll: true, wipe: true, per: [{ stat: 'dealt', v: 0.15 }],
    },
    passives: [
      { name: '검성의 박자', when: { on: 'play', type: '공격' }, fx: [KS('연격', 1)] },
      { name: '누가 허접이래!', when: { on: 'play', type: '공격' }, conds: [{ c: 'status', id: '리듬', n: 2 }], fx: [D(0.6)] },
    ],
    ult: { name: '오버드라이브', cost: 150, fx: [D(1.0, EA), ATK(0.15), RH(3)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '쌍검 베기', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '쌍검 십자베기', 2, '공격', [D(2.3)]),
    card(h, `${h}_s3`, '받아쳐!', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '소닉 블레이드', 3, '공격', [D(1.4, EA), PERR, D(0.5, EA), SPR('all')], {
      unique: true, sig: true, tags: ['신속'], blurb: '눈 깜짝할 새 파고들어 베고 돌아온다. 박자가 쌓인 만큼 칼이 따라 들어간다.',
      oracles: [
        ['바람을 가르는 검', 2, ['신속'], [D(1.0, EA), PERR, D(0.5, EA), SPR('all')]],
        ['쇳덩이째 베기', null, ['신속'], [D(1.9, EA), PERR, D(0.55, EA), SPR('all')]],
        ['촌장 후보의 등', null, ['신속'], [D(1.5, EA), PERR, D(0.5, EA), ST('피해 감소', 2)]],
        ['세 번째 칼', null, ['신속'], [D(1.4, EA), PERR, D(0.75, EA), SPR('all')]],
        ['허접들아 덤벼', null, ['신속'], [D(1.6, EA), PERR, D(0.55, EA)]],
      ],
      blesses: [B('원래 자리로', 'power'), B('유랑 호랑 검성', 'cost'), B('시비 걸고 싸워 보기', null, [ST('약화', 1, EA)])],
    }),
    card(h, `${h}_u2`, '쌍검 휘두르기', 1, '공격', [D(0.6, E1, 2), LINK, D(0.7)], {
      unique: true, blurb: '빠르게 두 번, 둘째가 더 깊다. 박자가 끊기지 않았으면 셋째 칼이 따라 들어간다.',
      oracles: [
        ['가볍게 툭', null, ['보존'], [D(0.65, E1, 2), LINK, D(0.75)]],
        ['네 갈래 쌍검', 2, null, [D(0.7, E1, 4), LINK, D(1.0)]],
        ['되받아치기', null, null, [D(0.6, E1, 2), BL(1.5), LINK, D(0.7)]],
        ['박자 당기기', null, null, [D(0.7, E1, 2), IFR(3), DRAW(2)]],
        ['두 번째가 진짜', null, null, [D(0.75, E1, 2), LINK, D(0.6), TOUGH(1)]],
      ],
      blesses: [B('기척 읽기', null, [ST('약화', 1)]), B('활발한 체육 특기생', 'power'), B('검으로 사귄 친구', null, null, ['개전'])],
    }),
    card(h, `${h}_u3`, '장작 패기', 2, '공격', [D(1.0), PER('연격'), D(0.45), PERR, D(0.35)], {
      unique: true, blurb: '걸음마보다 먼저 배운 것. 모인 박자를 한 번에 내려치면 버티던 자세째 쪼개진다.',
      oracles: [
        ['잔가지 패기', 1, null, [D(0.6), PER('연격'), D(0.4)]],
        ['통나무째로', 3, null, [D(1.9), PER('연격'), D(0.7), PERR, D(0.55)]],
        ['잔디 뜯어 먹기', null, null, [D(1.4), PER('연격'), D(0.55), ST('불굴', 1)]],
        ['장작 삼천 개', null, null, [D(1.3), PERR, D(0.6), TOUGH(2)]],
        ['한 바퀴 휘두르기', null, null, [D(1.2), PER('연격'), D(0.5), PERR, D(0.3, EA)]],
      ],
      blesses: [B('걸음마보다 장작', null, [BL(1.5)]), B('촌장의 교육', 'power'), B('할망구 몰래', 'atkUp')],
    }),
    card(h, `${h}_u4`, '백호 비전서', 2, '강화', [ATK(0.15), RH(2), DRAW(1)], {
      unique: true, blurb: '말만은 고풍스럽다. 그래도 펼치는 순간 몸이 박자를 기억한다.',
      oracles: [
        ['비전서 첫 장', null, ['개전'], [ATK(0.15), RH(3), DRAW(1)]],
        ['비전서 끝 장', 3, null, [ATK(0.2), CRIT(0.1), RH(3)]],
        ['사료스탕스 결성', null, null, [ATK(0.15), RH(2), ST('사기', 1)]],
        ['기척과 비전', null, null, [ATK(0.15), RH(2), DRAW(2)]],
        ['참스승의 가르침', null, null, [ATK(0.15), CRIT(0.1), RH(2)]],
      ],
      blesses: [B('사료는 모욕이다', 'atkUp'), B('디아나의 수제자', 'cost'), B('무사 차림의 아침', null, null, ['개전'])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 루드 ───────────────────────────────────────────────
{
  const h = '루드';
  const hero = {
    id: h, name: '루드', nature: '활발', race: '용족', row: 'front', role: '탱커', style: '박자형', star: 3,
    hp: 950, atk: 101, def: 67, crit: 5,
    blurb: '용족 2인자이자 헬창. 같은 동작을 되풀이해 한 세트를 채우고, 세트가 끊기는 순간 그때까지 맞춘 박자를 파티에 넘겨준다.',
    keyword: {
      name: '세트', desc: '쉬지 않고 이어 가는 반복 — 이어 갈수록 근육이 달아오르고, 남이 끼어들어 끊기면 맞춘 박자가 파티에 남는다',
      carrier: 'self', cap: 3, wipe: true, per: [{ stat: 'def', v: 0.1 }],
      rules: [{ name: '세트 끝', when: { on: 'stackGone', id: '세트' }, fx: [RH(2)] }],
    },
    passives: [
      { name: '단백질 보충', when: { on: 'play' }, fx: [KS('세트', 1)] },
      { name: '크로스핏 선발대', when: { on: 'rhythm', n: 3 }, fx: [SH(2.0)] },
    ],
    ult: { name: '임팩트 프레스', cost: 300, fx: [DD(0.3, EA, 5), ST('기절', 1, E1), RH(3)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s2`],
  };
  const cards = [
    card(h, `${h}_s1`, '맨손 스쿼트 킥', 1, '공격', [D(1.0)]),
    card(h, `${h}_s2`, '코어 버티기', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '한 세트 더!', 2, '공격', [DD(0.8, EA), ST('약화', 1, EA), LINK, RH(2)], {
      unique: true, sig: true, blurb: '쩌렁쩌렁한 기합에 적의 주먹이 풀린다. 세트 한가운데서 지르면 온 파티의 심장이 같이 뛴다.',
      oracles: [
        ['가벼운 세트', 1, null, [DD(0.55, EA), ST('약화', 1, EA), LINK, RH(1)]],
        ['무한 세트', null, ['회수'], [DD(0.9, EA), ST('약화', 1, EA), LINK, RH(2)]],
        ['기합 소리', null, null, [DD(0.85, EA), ST('약화', 2, EA), LINK, RH(3)]],
        ['루드 짐 개장', 3, null, [DD(1.5, EA), ST('약화', 2, EA), LINK, RH(3)]],
        ['다야 님을 위하여', null, null, [DD(0.8, EA), SH(2.0), LINK, RH(2)]],
      ],
      blesses: [B('루비 아령', 'power'), B('헬씨 레드', 'cost'), B('소음 공해', null, [RUSH(1, EA)])],
    }),
    card(h, `${h}_u2`, '미숫가루 프로틴', 1, '스킬', [BL(2.0), REP, KS('세트', 1), DRAW(1)], {
      unique: true, tags: ['회수'], blurb: '세트 사이 30초, 단백질의 골든타임. 한 잔 더 — 같은 동작을 그대로 한 번 더.',
      oracles: [
        ['한 스쿱', 0, ['회수'], [BL(1.0), REP, KS('세트', 1), DRAW(1)]],
        ['벌크업', 2, ['회수'], [BL(4.0), REP, KS('세트', 2), DRAW(2)]],
        ['식물에게도 프로틴', null, ['회수'], [BL(2.0), REP, KS('세트', 1), ATK(0.15, 'oneAlly')]],
        ['유산소는 적', null, ['회수 2'], [BL(2.2), REP, KS('세트', 1), DRAW(1)]],
        ['루드의 운동 교본', null, null, [DEF(0.1), KS('세트', 2), DRAW(1)], true],
      ],
      blesses: [B('쉐이커', 'guard'), B('탄수화물 금지', null, null, ['보존']), B('단백질 바', null, [RH(1)])],
    }),
    card(h, `${h}_u3`, '다야 님을 지켜라', 2, '스킬', [SH(3.0), PERR, SH(0.5), ST('피해 감소', 1)], {
      unique: true, blurb: '수장님 앞을 근육으로 막아선다. 세트 사이 박자가 빠를수록 벽이 두꺼워진다.',
      oracles: [
        ['한발 앞으로', 1, null, [SH(1.8), PERR, SH(0.4)]],
        ['용족 2인자의 의무', 3, null, [SH(5.0), PERR, SH(0.8), ST('피해 감소', 2)]],
        ['약한 용족들을 위해', null, null, [SH(3.0), PERR, SH(0.5), ST('사기', 1)]],
        ['근육 방패', null, null, [SH(3.6), PERR, SH(0.7), ST('피해 감소', 1)]],
        ['근손실 방지', null, null, [SH(3.0), PERR, SH(0.5), ST('피해 감소', 3)]],
      ],
      blesses: [B('수장 경호', 'cost'), B('루비 이두근', 'guard'), B('패배는 인정', null, [ST('면역', 1)])],
    }),
    card(h, `${h}_u4`, '실피르보다 세게', 2, '공격', [DD(1.3), PER('세트'), DD(0.4), REP, TOUGH(1)], {
      unique: true, tags: ['회수'], blurb: '혼자 덤벼 오는 악우를 노는 셈 치고 받아 주던 주먹. 한 번 더 — 마지막 한 개가 무겁다.',
      oracles: [
        ['잽', 1, ['회수'], [DD(0.8), PER('세트'), DD(0.35), REP, TOUGH(1)]],
        ['재도전', 3, ['회수'], [DD(2.3), PER('세트'), DD(0.6), REP, TOUGH(2)]],
        ['노는 셈 치고', null, ['회수'], [DD(1.3), PER('세트'), DD(0.4), SH(1.5)]],
        ['마지막 한 개', null, ['회수'], [DD(1.5), PER('세트'), DD(0.6), REP, TOUGH(1)]],
        ['악우의 주먹', null, ['회수', '분쇄'], [DD(1.7), PER('세트'), DD(0.45), REP, TOUGH(1)]],
      ],
      blesses: [B('정면 승부', 'power'), B('하체 운동', 'cost'), B('날개는 장식', null, [ST('약화', 2)])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 아르코 ─────────────────────────────────────────────
{
  const h = '아르코';
  const hero = {
    id: h, name: '아르코', nature: '활발', race: '정령', row: 'mid', role: '딜러', style: '박자형', star: 3,
    hp: 720, atk: 135, def: 28, crit: 10,
    blurb: '적포도 정령 스트리트 댄서. 킥 · 스텝 · 킥 — 공격과 스킬을 엇박으로 엮으면 몸에 그루브가 오르고, 그 그루브를 저항의 몸짓으로 쏟는다.',
    keyword: {
      name: '그루브', desc: '엇박에 올라탄 몸 — 스텝을 밟을수록 춤이 거칠어지고, 밤이 지나면 식는다',
      carrier: 'self', cap: 4, decayAll: true, per: [{ stat: 'dealt', v: 0.1 }],
    },
    passives: [
      { name: '음파 두 번', when: { on: 'play', seq: ['공격', '스킬', '공격'], who: 'any' }, fx: [D(0.5, E1, 2), KS('그루브', 1)] },
      { name: '리듬에 몸을 맡겨', when: { on: 'rhythm', n: 4 }, fx: [KS('그루브', 2)] },
    ],
    ult: { name: '퍼플 쇼츠', cost: 250, fx: [ST('피해 감소', 2), D(0.45, E1, 8), KS('그루브', 2)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '스텝 킥', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '토네이도 킥', 2, '공격', [D(2.4)]),
    card(h, `${h}_s3`, '백스핀 회피', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '정정당당 댄스배틀', 1, '공격', [D(0.35, EA, 2), PREV('스킬'), KS('그루브', 1)], {
      unique: true, sig: true, tags: ['분쇄'], blurb: '첫 박에 방패를 깨고 둘째 박에 들어간다. 스텝 뒤의 킥은 더 신난다.',
      oracles: [
        ['첫 스텝', 0, ['분쇄'], [D(0.25, EA), DRAW(1), PREV('스킬'), KS('그루브', 1)]],
        ['댄스 크루 총출동', 2, ['분쇄'], [D(0.4, EA, 3), PREV('스킬'), KS('그루브', 2), RH(1)]],
        ['청적전쟁', null, ['분쇄'], [D(0.35, EA, 2), ST('약화', 1, EA), PREV('스킬'), KS('그루브', 1)]],
        ['댄스 어쌔신', null, ['분쇄'], [D(0.45, EA, 2), PREV('스킬'), KS('그루브', 1)]],
        ['프리스타일', null, ['분쇄'], [D(0.35, EA, 2), PREV('스킬'), KS('그루브', 1), RH(1)]],
      ],
      blesses: [B('페스타의 신곡', 'power'), B('청포도 사절', 'frost'), B('맨발 스텝', 'draw')],
    }),
    card(h, `${h}_u2`, '브레이크 스핀', 1, '스킬', [BL(1.5), PREV('공격'), KS('그루브', 1), DRAW(1)], {
      unique: true, blurb: '킥 다음엔 바닥을 쓸며 돈다 — 엇박이 몸에 붙는 순간.',
      oracles: [
        ['윈드밀', null, null, [BL(2.0), PREV('공격'), KS('그루브', 1), DRAW(1)]],
        ['헤드스핀', 2, null, [BL(3.5), PREV('공격'), KS('그루브', 2), DRAW(2)]],
        ['프리즈', null, null, [BL(1.5), ST('피해 감소', 1), PREV('공격'), KS('그루브', 1)]],
        ['백스텝', 0, null, [DRAW(1), PREV('공격'), KS('그루브', 1)]],
        ['업록', null, ['보존'], [BL(1.8), PREV('공격'), KS('그루브', 1), DRAW(1)]],
      ],
      blesses: [B('스니커즈', 'guard'), B('거리 공연', null, null, ['개전']), B('흥겨운 파동', null, [RH(1)])],
    }),
    card(h, `${h}_u3`, '저항의 몸짓', 2, '공격', [D(1.0), PER('그루브'), D(0.5), SP('그루브', 'all')], {
      unique: true, blurb: '여덟 박을 기다리지 않는다. 오른 만큼 지금 쏟아 낸다.',
      oracles: [
        ['작은 저항', 1, null, [D(0.6), PER('그루브'), D(0.35), SP('그루브', 'all')]],
        ['거리 점령', 3, null, [D(1.3, EA), PER('그루브'), D(0.5, EA), SP('그루브', 'all')]],
        ['비폭력 예술', null, null, [D(1.0), PER('그루브'), D(0.5), ST('약화', 2)]],
        ['끝까지 춤춘다', null, null, [D(1.2), PER('그루브'), D(0.5), SP('그루브', 2)]],
        ['자유의 외침', null, null, [D(1.0), PER('그루브'), D(0.4, EA), SP('그루브', 'all')]],
      ],
      blesses: [B('적포도가 근본', 'power'), B('맨몸 공연', 'cost'), B('청포도는 못 비벼', null, [ST('약화', 1)])],
    }),
    card(h, `${h}_u4`, '우상의 신곡', 1, '강화', [ATK(0.1), KS('그루브', 2), RH(1)], {
      unique: true, blurb: '페스타의 새 노래가 나오면 몸부터 나간다.',
      oracles: [
        ['페스타 첫 공연', null, ['개전'], [ATK(0.1), KS('그루브', 2), RH(1)]],
        ['정령 대연회', 2, null, [ATK(0.15), KS('그루브', 4), RH(2)]],
        ['크루 결성', null, null, [ATK(0.1), KS('그루브', 2), ST('사기', 1)]],
        ['밤샘 연습', 0, null, [ATK(0.1), DRAW(1)]],
        ['샤인그레이프', null, null, [ATK(0.1), KS('그루브', 2), DRAW(1)]],
      ],
      blesses: [B('이어폰', null, null, ['개전']), B('과일 주스', 'atkUp'), B('스피커', null, [RH(1)])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 레비(졸업) ─────────────────────────────────────────
{
  const h = '레비_졸업';
  const hero = {
    id: h, name: '레비(졸업)', nature: '활발', race: '마녀', row: 'front', role: '탱커', style: '박자형', star: 3,
    hp: 1000, atk: 75, def: 76, crit: 5,
    blurb: '노력천재. 매일 같은 양을 꾸준히 — 턴마다 서너 장을 빠짐없이 내면 근속이 쌓이고, 하루라도 손을 놓으면 처음부터다.',
    keyword: {
      name: '근속', desc: '하루도 빠짐없이 출근한 날들 — 쌓일수록 일 처리가 단단해지지만, 하루 쉬면 처음부터 다시',
      carrier: 'self', cap: 5, per: [{ stat: 'def', v: 0.1 }],
      rules: [
        { name: '출근 도장', when: { on: 'turnEnd' }, conds: [{ c: 'playedMin', n: 3 }, { c: 'playedMax', n: 4 }], fx: [KS('근속', 1)] },
        { name: '지각', when: { on: 'turnEnd' }, conds: [{ c: 'playedMax', n: 2 }], fx: [SP('근속', 'all')] },
      ],
    },
    passives: [
      { name: '마법 유기물 탐지', when: { on: 'play', every: 2 }, fx: [RH(1)] },
      { name: '변하지 않는 사이', when: { on: 'lowHp', pct: 0.3 }, fx: [HL(3.0)] },
    ],
    ult: { name: '열정 넘치는 신입', cost: 200, fx: [DD(0.2, EA, 5), RUSH(2, EA), KS('근속', 1)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s2`],
  };
  const cards = [
    card(h, `${h}_s1`, '빗자루 후리기', 1, '공격', [DD(0.7)]),
    card(h, `${h}_s2`, '사원증 가드', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '패기 가득한 인재', 2, '공격', [DD(0.3, EA, 3), ST('약화', 1, EA), PREV('스킬'), RH(1)], {
      unique: true, sig: true, blurb: '각성 드링크를 들이켜고 세 번 터진다. 서류 정리 다음 박자에 맞추면 더 잘 터진다.',
      oracles: [
        ['신입 패기', 1, null, [DD(0.2, EA, 3), ST('약화', 1, EA), PREV('스킬'), RH(1)]],
        ['각성 드링크', 3, null, [ST('면역', 1), DD(0.45, EA, 3), ST('약화', 2, EA)]],
        ['야근 폭발', null, null, [DD(0.3, EA, 3), ST('약화', 1, EA), PREV('스킬'), RH(3)]],
        ['논문 발표', null, null, [DD(0.35, EA, 3), ST('약화', 1, EA), PREV('스킬'), KS('근속', 1)]],
        ['정식 마녀', null, null, [ST('면역', 1), DD(0.35, EA, 3), ST('약화', 1, EA)]],
      ],
      blesses: [B('당근 튀김', 'power'), B('칼퇴', 'cost'), B('인사평가', 'frost')],
    }),
    card(h, `${h}_u2`, '롤모델 찾기', 0, '스킬', [DRAW(1), LINK, RH(1)], {
      unique: true, blurb: '할 일 하나를 더 끼워 넣는다 — 오늘 몫의 장수를 채우는 습관.',
      oracles: [
        ['선배 따라 하기', null, null, [DRAW(1), LINK, RH(2)]],
        ['프리클 님 수제자', null, null, [DRAW(1), ATK(0.15, 'oneAlly'), LINK, RH(1)]],
        ['지침서 무료 배포', null, null, [DRAW(2)]],
        ['견습 시절 가게', null, ['보존'], [DRAW(1), LINK, RH(2)]],
        ['바리에와 나란히', null, null, [DRAW(1), ST('협공', 1)]],
      ],
      blesses: [B('아침 회의', null, null, ['개전']), B('업무 보고', 'draw'), B('사원증 목걸이', null, [RH(1)])],
    }),
    card(h, `${h}_u3`, '서류 뭉치 휘두르기', 1, '공격', [DD(0.5), PER('근속'), DD(0.15), LINK, RH(1)], {
      unique: true, blurb: '보고서 한 장에 관찰 일지 다섯 장. 일한 날이 쌓인 만큼 뭉치가 두껍다.',
      oracles: [
        ['한 장씩', 0, null, [DD(0.25), PER('근속'), DD(0.08), DRAW(1)]],
        ['관찰 일지 다섯 장', 2, null, [DD(1.1), PER('근속'), DD(0.3), LINK, RH(2)]],
        ['보고서 반려', null, null, [DD(0.55), PER('근속'), DD(0.15), ST('약화', 1)]],
        ['일할수록 일이 는다', null, null, [DD(0.5), PER('근속'), DD(0.2), LINK, KS('근속', 1)]],
        ['결재 도장', null, ['분쇄'], [DD(0.6), PER('근속'), DD(0.15), LINK, RH(1)]],
      ],
      blesses: [B('논문 인용', 'power'), B('야근 수당', 'cost'), B('스테이플러', null, [RH(1)])],
    }),
    card(h, `${h}_u4`, '눈물로 쓴 가이드북', 2, '강화', [DEF(0.15), KS('근속', 2), ST('결의', 1)], {
      unique: true, blurb: '뒤따라올 견습들을 위해 공짜로 푸는 지침서. 쓰는 동안 저도 단단해진다.',
      oracles: [
        ['초판', 1, null, [DEF(0.1), KS('근속', 1), ST('결의', 1)]],
        ['개정 증보판', 3, null, [DEF(0.2), KS('근속', 3), ST('결의', 2)]],
        ['무료 배포', null, null, [DEF(0.15), KS('근속', 2), ST('사기', 1)]],
        ['칼퇴 계획서', null, null, [DEF(0.15), KS('근속', 2), DRAW(2)]],
        ['노력천재', null, null, [DEF(0.15), KS('근속', 4), ST('결의', 1)]],
      ],
      blesses: [B('도서관 논문 전부', null, null, ['개전']), B('검은 티셔츠', 'defUp'), B('퇴근 가방', null, null, ['보존'])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 슈팡 ───────────────────────────────────────────────
{
  const h = '슈팡';
  const hero = {
    id: h, name: '슈팡', nature: '활발', race: '요정', row: 'back', role: '서포터', style: '박자형', star: 3,
    hp: 630, atk: 75, def: 58, crit: 5,
    blurb: '과속 배달부. 한 턴에 많이 낼수록 빨라진다 — 셋째 장에 소포가 떨어지고, 넷째 장에 한 바퀴 더 돈다.',
    keyword: {
      name: '우편물', desc: '슈팡이 흘리고 간 소포 — 쌓인 만큼 뜯어 보는 쪽이 임자다',
      carrier: 'self', cap: 5,
    },
    passives: [
      { name: '과속 배달', when: { on: 'play', nth: 3, who: 'any' }, fx: [KS('우편물', 2), RH(1)] },
      { name: '한 바퀴 더', when: { on: 'play', nth: 4, who: 'any' }, fx: [AP(1)] },
    ],
    ult: { name: '슈팡 배송', cost: 250, fx: [D(1.5, EA), ST('약화', 2, EA), ST('피해 감소', 2)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s2`],
  };
  const cards = [
    card(h, `${h}_s1`, '소포 투척', 1, '공격', [D(1.0)]),
    card(h, `${h}_s2`, '구급 소포', 1, '스킬', [HL(2.1)]),
    card(h, `${h}_u1`, '무책임 배달부', 1, '스킬', [HL(1.6), KS('우편물', 1), PREV('공격'), RH(1)], {
      unique: true, sig: true, tags: ['신속'], blurb: '두 번 왕복하며 흘린 소포가 누군가의 상처를 덮는다.',
      oracles: [
        ['대충 던짐', 0, ['신속'], [HL(0.7), DRAW(1), PREV('공격'), RH(1)]],
        ['두 번 왕복', 2, ['신속'], [HL(3.6), KS('우편물', 2), PREV('공격'), RH(2)]],
        ['흘린 소포', null, ['신속'], [HL(1.7), KS('우편물', 2), PREV('공격'), RH(1)]],
        ['문 앞 배송', null, ['신속', '보존'], [HL(1.8), KS('우편물', 1), PREV('공격'), RH(1)]],
        ['야, 타!', null, ['신속'], [HL(1.6), KS('우편물', 1), DRAW(1)]],
      ],
      blesses: [B('훈장', 'heal'), B('첫 배송', null, null, ['개전']), B('슈파볼트', null, [RH(1)])],
    }),
    card(h, `${h}_u2`, '슈팡은⋯ 달리고 싶다!', 0, '스킬', [DRAW(1), PREV('스킬'), RH(1)], {
      unique: true, tags: ['신속'], blurb: '일이 없어도 달린다. 장수가 하나 늘면 그만큼 빨라진다.',
      oracles: [
        ['풀 스로틀', null, ['신속', '보존'], [DRAW(1), PREV('스킬'), RH(1)]],
        ['질주 본능', null, ['신속'], [DRAW(2), PREV('스킬'), RH(1)]],
        ['가속 마법 도구', null, ['신속'], [DRAW(1), NC(1)]],
        ['배달 동선', null, ['신속'], [DRAW(1), KS('우편물', 1), PREV('스킬'), RH(1)]],
        ['엘리아스는 질렸어', null, ['신속'], [DRAW(1), PREV('스킬'), RH(2)]],
      ],
      blesses: [B('지름길', 'draw'), B('정비 완료', null, null, ['보존']), B('경적', null, [RH(1)])],
    }),
    card(h, `${h}_u3`, '케이크 위 코너링', 2, '공격', [DD(0.7, EA), PER('우편물'), DD(0.25, EA), SP('우편물', 'all')], {
      unique: true, tags: ['신속'], blurb: '케이크째 들이받고 코너를 돈다. 흘린 소포가 많을수록 사고가 크다.',
      oracles: [
        ['급커브', 1, ['신속'], [DD(0.45, EA), PER('우편물'), DD(0.15, EA), SP('우편물', 'all')]],
        ['슈파볼트 폭주', 3, ['신속'], [DD(1.2, EA), PER('우편물'), DD(0.35, EA), SP('우편물', 'all')]],
        ['박살 난 케이크', null, ['신속'], [DD(0.75, EA), PER('우편물'), DD(0.25, EA), ST('약화', 1, EA)]],
        ['배달 완료', null, ['신속'], [DD(0.8, EA), PER('우편물'), HL(0.6), SP('우편물', 'all')]],
        ['경찰 추격전', null, ['신속'], [DD(0.8, EA), PER('우편물'), DD(0.25, EA), SP('우편물', 2)]],
      ],
      blesses: [B('으하하', 'power'), B('과속 단속 무시', 'cost'), B('초콜릿 범벅', 'frost')],
    }),
    card(h, `${h}_u4`, '손가락 인사', 1, '강화', [DEF(0.1), KS('우편물', 2), RH(1)], {
      unique: true, tags: ['신속'], blurb: '뒤로 날리는 쌍손가락. 어디에도 매이지 않겠다는 선언이다.',
      oracles: [
        ['쌍손가락', 0, ['신속'], [DEF(0.1), DRAW(1)]],
        ['폭주족 대장', 2, ['신속'], [DEF(0.15), KS('우편물', 4), RH(2)]],
        ['경비대 상사 출신', null, ['신속'], [DEF(0.1), KS('우편물', 2), ST('결의', 1)]],
        ['드럼 비트', null, ['신속'], [DEF(0.1), KS('우편물', 2), DRAW(1)]],
        ['⋯슈팡', null, ['신속'], [DEF(0.1), KS('우편물', 3), RH(2)]],
      ],
      blesses: [B('배달 조끼', null, null, ['개전']), B('스판 운동복', 'defUp'), B('배송 조회', null, null, ['보존'])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 카렌 ───────────────────────────────────────────────
{
  const h = '카렌';
  const hero = {
    id: h, name: '카렌', nature: '활발', race: '요정', row: 'back', role: '딜러', style: '박자형', star: 1,
    hp: 440, atk: 93, def: 23, crit: 10,
    blurb: '몰락한 엘튜버. 매 턴 한 번은 공격으로 업로드해야 구독자가 붙는다 — 하루 빠지면 반 토막, 박자가 오른 날엔 떡상.',
    keyword: {
      name: '구독자', desc: '카렌 채널의 구독자 — 늘수록 방송에 힘이 실리지만, 업로드를 하루 거르면 우르르 빠진다',
      carrier: 'self', cap: 6, per: [{ stat: 'dealt', v: 0.1 }],
      rules: [{ name: '업로드 공백', when: { on: 'turnEnd' }, conds: [{ c: 'ownNone' }], fx: [SP('구독자', 2)] }],
    },
    passives: [
      { name: '오늘의 업로드', when: { on: 'play', type: '공격' }, limit: { per: 'turn', n: 1 }, fx: [KS('구독자', 1)] },
      { name: '떡상', when: { on: 'rhythm', n: 3 }, fx: [KS('구독자', 2)] },
    ],
    ult: { name: '교주의 축복 - 카렌', cost: 200, fx: [HL(6.0), ST('사기', 1), KS('구독자', 3)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '당근 던지기', 1, '공격', [D(1.0)]),
    card(h, `${h}_s2`, '왕당근 던지기', 2, '공격', [D(2.2)]),
    card(h, `${h}_s3`, '셀카봉 가드', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '당근 치유', 2, '스킬', [HL(2.4), PERR, HL(0.5), KS('구독자', 1)], {
      unique: true, sig: true, blurb: '생방송 중의 치유 마법. 채팅이 빨라질수록 화면 너머까지 낫는다.',
      oracles: [
        ['당근 한 입', 1, null, [HL(1.5), PERR, HL(0.3)]],
        ['특 플러스 당근', 3, null, [HL(4.2), PERR, HL(0.7), KS('구독자', 2)]],
        ['당근 케이크', null, null, [HL(2.4), PERR, HL(0.5), ST('사기', 1)]],
        ['언니 몰래 드레싱', null, null, [HL(2.8), PERR, HL(0.6), KS('구독자', 1)]],
        ['100캐럿 당근', null, null, [HL(2.4), PERR, HL(0.5), DRAW(1)]],
      ],
      blesses: [B('구독 감사', 'heal'), B('짧은 영상', 'cost'), B('알림 설정', null, [KS('구독자', 1)])],
    }),
    card(h, `${h}_u2`, '근본 생식 챌린지', 1, '스킬', [KS('구독자', 2), DRAW(1), PREV('공격'), RH(1)], {
      unique: true, blurb: '생당근을 씹는 척. 공격 영상 바로 다음에 올려야 조회수가 붙는다.',
      oracles: [
        ['생당근 공포증', 0, null, [KS('구독자', 1), DRAW(1)]],
        ['24시간 생방송', 2, null, [KS('구독자', 4), DRAW(2), PREV('공격'), RH(2)]],
        ['썸네일 낚시', null, null, [KS('구독자', 2), DRAW(1), PREV('공격'), RH(2)]],
        ['합방', null, null, [KS('구독자', 2), ST('협공', 1), PREV('공격'), RH(1)]],
        ['드레싱 해명 방송', null, ['보존'], [KS('구독자', 2), DRAW(1), PREV('공격'), RH(1)]],
      ],
      blesses: [B('편집자', 'draw'), B('예약 업로드', null, null, ['개전']), B('실시간 채팅', null, [RH(1)])],
    }),
    card(h, `${h}_u3`, '어그로 댓글 추적 방송', 2, '공격', [D(1.6), PERR, D(0.4), KS('구독자', 1)], {
      unique: true, blurb: '악플 하나에 발끈해 범인을 찾아 나선다. 시청자가 몰릴수록 추적이 매섭다.',
      oracles: [
        ['댓글 하나', 1, null, [D(0.9), PERR, D(0.3)]],
        ['범인 찾았다', 3, null, [D(2.4), PERR, D(0.6), KS('구독자', 2)]],
        ['레비였다고?', null, null, [D(1.6), PERR, D(0.4), ST('약화', 2)]],
        ['사과의 당근 튀김', null, null, [D(1.6), PERR, D(0.45), HL(1.0)]],
        ['다크넷 역추적', null, null, [D(1.8), PERR, D(0.45), KS('구독자', 1)]],
      ],
      blesses: [B('고정 댓글', 'power'), B('신고 누적', 'weakSpot'), B('조회수 폭발', null, [KS('구독자', 1)])],
    }),
    card(h, `${h}_u4`, '엘튜브 생방송 마법쇼', 1, '강화', [ATK(0.1), KS('구독자', 2), RH(1)], {
      unique: true, blurb: '방송이 곧 힘이다. 카메라가 켜지면 마법도 커진다.',
      oracles: [
        ['첫 방송', 0, null, [ATK(0.1), DRAW(1)]],
        ['100만 구독 기념', 2, null, [ATK(0.15), KS('구독자', 4), RH(2)]],
        ['몰락 관종', null, null, [ATK(0.1), KS('구독자', 3), RH(1)]],
        ['실시간 후원', null, null, [ATK(0.1), KS('구독자', 2), AP(1)]],
        ['당근 케이크 먹방', null, null, [ATK(0.1), KS('구독자', 2), HL(1.2)]],
      ],
      blesses: [B('썸네일', null, null, ['개전']), B('링라이트', 'atkUp'), B('다시 보기', null, null, ['보존'])],
    }),
  ];
  write('rhythm', h, hero, cards);
}
