// 계산형 — 활발: 루포(셈을 맞춰 주는 쪽) · 에슈르(마도)(꼭 맞춘 셈을 쓰는 쪽) · 타이다(장수를 덜어 몰아 쏘는 쪽)
//          우울: 림(공격 · 스킬 저울) · 실피르(언제나 둘째)
const L = require('./lib');
const { E1, EA, ER, D, DD, BL, SH, HL, ST, RH, KS, SP, DRAW, AP, NC, TOUGH, RUSH, MAKE, ATK, DEF, CRIT, PER, IFS, TUNE, CHAIN, BROKEN, card, B, write } = L;
const SPENT = n => ({ k: 'ifSpent', n });
const BAL = { k: 'ifBalanced' };
const APLEFT = n => ({ k: 'ifApLeft', n });

// ── 루포 ───────────────────────────────────────────────
{
  const h = '루포';
  const hero = {
    id: h, name: '루포', nature: '활발', race: '수인', row: 'mid', role: '딜러', style: '계산형', star: 3,
    hp: 570, atk: 135, def: 28, crit: 10,
    blurb: '사료스탕스의 책사. 남은 AP 와 코스트를 딱 맞춰 낸 수가 계획이 되고, 계획이 맞아떨어지면 한 수가 더 난다 — 동료의 셈까지 맞춰 주는 쪽.',
    keyword: {
      name: '계획', desc: '루포 님 머릿속의 완벽한 작전 — 맞아떨어질 때마다 칼끝이 급소를 찾는다',
      carrier: 'self', cap: 3, per: [{ stat: 'crit', v: 0.1 }],
    },
    passives: [
      { name: '계획대로인 것이다', when: { on: 'break' }, limit: { per: 'turn', n: 1 }, fx: [KS('계획', 1)] },
      { name: '한 수 앞', when: { on: 'play', nth: 3, who: 'any' }, conds: [{ c: 'apLeft', n: 1 }], fx: [KS('계획', 1), DRAW(1)] },
    ],
    ult: { name: '오의 여우회전!', cost: 200, fx: [D(0.2, EA, 8), ST('피해 감소', 2)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '단검 찌르기', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '단검 급소 찌르기', 2, '공격', [D(2.3)]),
    card(h, `${h}_s3`, '몸 숙이기', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '루포류 신속베기', 2, '공격', [D(0.4, E1, 3), ST('고통', 2), TUNE, KS('계획', 1)], {
      unique: true, sig: true, tags: ['신속'], blurb: '가장 뒤의 적에게 순간 이동해 세 번. 마지막 AP 로 들어가면 계획이 선다.',
      oracles: [
        ['한 번 베기', 1, ['신속'], [D(0.4, E1, 2), ST('고통', 1), TUNE, KS('계획', 1)]],
        ['루포류 비기', 3, ['신속'], [D(0.55, E1, 4), ST('고통', 3), TUNE, KS('계획', 2)]],
        ['쓰라림', null, ['신속'], [D(0.4, E1, 3), ST('고통', 3), TUNE, KS('계획', 1)]],
        ['가장 뒤의 적', null, ['신속'], [D(0.45, E1, 3), ST('고통', 2), TUNE, AP(1)]],
        ['완벽한 동선', null, ['신속'], [D(0.45, E1, 3), ST('고통', 2), TUNE, KS('계획', 1)]],
      ],
      blesses: [B('여우 꼬리', 'power'), B('지름길', 'cost'), B('쓰라린 상처', 'frost')],
    }),
    card(h, `${h}_u2`, '작전 지도 펼치기', 1, '스킬', [NC(1), DRAW(1), TUNE, KS('계획', 1)], {
      unique: true, tags: ['신속'], blurb: '다음 수의 값을 깎아 셈을 맞춘다. 누구의 카드든.',
      oracles: [
        ['메모 한 장', 0, ['신속'], [NC(1), DRAW(1)]],
        ['대작전', 2, ['신속'], [NC(2), DRAW(2), TUNE, KS('계획', 2)]],
        ['룰 조작', null, ['신속'], [NC(1), DRAW(1), TUNE, AP(1)]],
        ['보드게임 9전 9패', null, ['신속'], [NC(1), DRAW(2)]],
        ['머리가 좋아지는 약', null, ['신속', '보존'], [NC(1), DRAW(1), TUNE, KS('계획', 1)]],
      ],
      blesses: [B('붉은 펜', 'draw'), B('작전 회의', null, null, ['개전']), B('그럴듯한 계책', null, [KS('계획', 1)])],
    }),
    card(h, `${h}_u3`, '계획대로인 것이다', 1, '공격', [PER('계획'), D(0.6), SP('계획', 'all'), TUNE, AP(1)], {
      unique: true, tags: ['신속'], blurb: '세운 계획만큼 찌르고, 마지막 AP 로 마무리하면 한 수가 더 난다.',
      oracles: [
        ['작은 계획', 0, ['신속'], [PER('계획'), D(0.35), SP('계획', 'all'), TUNE, DRAW(1)]],
        ['대계', 2, ['신속'], [PER('계획'), D(1.2), SP('계획', 'all'), TUNE, AP(1)]],
        ['눈속임 단검', null, ['신속'], [D(0.8), PER('계획'), D(0.55), SP('계획', 'all')]],
        ['루포 님의 작전', null, ['신속'], [PER('계획'), D(0.75), SP('계획', 'all'), TUNE, AP(1)]],
        ['티그의 위세', null, ['신속'], [PER('계획'), D(0.6), TUNE, AP(1)]],
      ],
      blesses: [B('급소 찌르기', 'power'), B('신속 결정', 'ap'), B('계획 수정', null, [KS('계획', 1)])],
    }),
    card(h, `${h}_u4`, '책사의 두뇌', 1, '강화', [ATK(0.1), KS('계획', 1), DRAW(1)], {
      unique: true, blurb: '머리가 좋아지는 약을 먹고 작전을 짠다. 논리는 가끔 샌다.',
      oracles: [
        ['메모장', 0, null, [ATK(0.1), DRAW(1)]],
        ['완벽한 작전서', 2, null, [ATK(0.15), KS('계획', 3), DRAW(2)]],
        ['첫 역할 놀이', null, null, [ATK(0.1), KS('계획', 1), ST('사기', 1)]],
        ['사료스탕스 브레인', null, null, [ATK(0.1), KS('계획', 2), DRAW(1)]],
        ['닌닌', null, null, [ATK(0.1), KS('계획', 1), AP(1)]],
      ],
      blesses: [B('영웅 소설', null, null, ['개전']), B('루포 님', 'atkUp'), B('작전 노트', null, null, ['보존'])],
    }),
  ];
  write('calc', h, hero, cards);
}

// ── 에슈르(마도) ───────────────────────────────────────
{
  const h = '에슈르_마도';
  const hero = {
    id: h, name: '에슈르(마도)', nature: '활발', race: '요정', row: 'mid', role: '딜러', style: '계산형', star: 3,
    hp: 700, atk: 135, def: 28, crit: 10,
    blurb: '마법을 입자로 푼 빵집 요정. 이번 턴 쓴 AP 가 꼭 정해진 수에 닿는 순간 마력이 증폭된다 — 입자를 굴려 셈을 맞춘다.',
    keyword: {
      name: '마력 증폭', desc: '셈이 맞아떨어진 마법의 여운 — 이 동안은 한 대 한 대가 굵어지고, 밤이 지나면 한 겹씩 빠진다',
      carrier: 'self', cap: 3, decay: 1, per: [{ stat: 'dealt', v: 0.2 }],
    },
    passives: [
      { name: '입자 분해', when: { on: 'fightStart' }, fx: [MAKE(`${h}_bit`, 1)] },
      { name: '정점 계산', when: { on: 'play', who: 'any' }, conds: [{ c: 'spent', n: 4 }], limit: { per: 'turn', n: 1 }, fx: [KS('마력 증폭', 2), DRAW(1)] },
    ],
    ult: { name: '마도학자의 길', cost: 200, fx: [D(1.5, EA), KS('마력 증폭', 3), ATK(0.15)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '마력 구체', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '왕마력 구체', 2, '공격', [D(2.3)]),
    card(h, `${h}_s3`, '로브 두르기', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_bit`, '마력 입자', 0, '스킬', [AP(1)], { tags: ['소멸', '신속'], blurb: '마법을 쪼갠 알갱이. 한 칸 더 — 넷째 AP 를 만든다.' }),
    card(h, `${h}_u1`, '빵타지아', 2, '공격', [D(0.8, EA), ST('취약', 1, EA), SPENT(3), KS('마력 증폭', 2)], {
      unique: true, sig: true, blurb: '빵과 마법을 섞은 폭발. 셋째 AP 에 꼭 맞춰 구우면 오븐 밖으로 마력이 넘친다.',
      oracles: [
        ['작은 빵', 1, null, [D(0.5, EA), ST('취약', 1, EA), SPENT(3), KS('마력 증폭', 1)]],
        ['궁극의 마법 빵', 3, null, [D(1.4, EA), ST('취약', 2, EA), SPENT(3), KS('마력 증폭', 3)]],
        ['민트는 안 돼', null, null, [D(0.8, EA), ST('취약', 1, EA), SPENT(3), MAKE(`${h}_bit`, 2)]],
        ['빵타지아 개정판', null, null, [D(0.9, EA), ST('취약', 1, EA), SPENT(3), KS('마력 증폭', 2)]],
        ['여왕님의 빵', null, null, [D(0.8, EA), ST('취약', 2, EA), SPENT(3), KS('마력 증폭', 2)]],
      ],
      blesses: [B('갓 구운 빵', 'power'), B('반죽 생략', 'cost'), B('밀가루 폭풍', 'frost')],
    }),
    card(h, `${h}_u2`, '마력 레이저', 3, '공격', [D(3.0), SPENT(4), D(1.5, EA)], {
      unique: true, blurb: '셋으로는 모자란다 — 입자 하나를 더 쪼개 넷째 AP 까지 꼭 맞추면 레이저가 번진다.',
      oracles: [
        ['마력 광선', 2, null, [D(2.0), SPENT(3), D(1.0, EA)]],
        ['대-에슈르의 레이저', null, null, [D(3.3), SPENT(4), D(1.6, EA)]],
        ['입자 분해 광선', null, null, [D(3.0), MAKE(`${h}_bit`, 1), SPENT(4), D(1.5, EA)]],
        ['거울 형상', null, null, [D(1.7, E1, 2), SPENT(4), D(1.5, EA)]],
        ['정점의 마법', null, null, [D(3.0), SPENT(3), D(1.5, EA)]],
      ],
      blesses: [B('렌즈 연마', 'power'), B('효율 계산', 'cost'), B('초점', 'weakSpot')],
    }),
    card(h, `${h}_u3`, '억까의 시간은 끝', 1, '스킬', [MAKE(`${h}_bit`, 1), DRAW(1), SPENT(2), KS('마력 증폭', 1)], {
      unique: true, blurb: '제대로 평가받지 못한 세월은 끝. 셈부터 다시 한다.',
      oracles: [
        ['짧은 각오', 0, null, [MAKE(`${h}_bit`, 1), DRAW(1)]],
        ['밤샘 연구', 2, null, [MAKE(`${h}_bit`, 2), DRAW(2), SPENT(3), KS('마력 증폭', 2)]],
        ['새 복장', null, null, [MAKE(`${h}_bit`, 1), DRAW(1), KS('마력 증폭', 1)]],
        ['전단지 접기', null, ['보존'], [MAKE(`${h}_bit`, 1), DRAW(1), SPENT(2), KS('마력 증폭', 1)]],
        ['파트라의 민트', null, null, [MAKE(`${h}_bit`, 2), DRAW(1)]],
      ],
      blesses: [B('연구 노트', 'draw'), B('개점 준비', null, null, ['개전']), B('마력 여운', null, [KS('마력 증폭', 1)])],
    }),
    card(h, `${h}_u4`, '연구 집중', 1, '강화', [ATK(0.1), KS('마력 증폭', 1), MAKE(`${h}_bit`, 1)], {
      unique: true, blurb: '밤새 연구하며 흘러가는 시간이 헛되지 않았음을 느끼고 싶다.',
      oracles: [
        ['메모', 0, null, [ATK(0.1), DRAW(1)]],
        ['마법의 정점', 2, null, [ATK(0.15), KS('마력 증폭', 2), MAKE(`${h}_bit`, 2)]],
        ['스터디 그룹', null, null, [ATK(0.1), KS('마력 증폭', 1), ST('사기', 1)]],
        ['빵과 마법', null, null, [ATK(0.1), KS('마력 증폭', 2), DRAW(1)]],
        ['교주와 밤샘', null, null, [ATK(0.1), MAKE(`${h}_bit`, 2), DRAW(1)]],
      ],
      blesses: [B('마법사 복장', null, null, ['개전']), B('후훗', 'atkUp'), B('식지 않는 빵', null, null, ['보존'])],
    }),
  ];
  write('calc', h, hero, cards);
}

// ── 타이다 ─────────────────────────────────────────────
{
  const h = '타이다';
  const hero = {
    id: h, name: '타이다', nature: '활발', race: '엘프', row: 'back', role: '딜러', style: '계산형', star: 1,
    hp: 420, atk: 114, def: 23, crit: 10,
    blurb: '농땡이 경비원. 파티가 이번 턴 꼭 두 장 이하로 버티면 땡땡이가 쌓이고, 아껴 둔 AP 는 다음 턴으로 — 그리고 한 발에 몽땅 싣는다.',
    keyword: {
      name: '땡땡이', desc: '눈치껏 농땡이 친 시간 — 아껴 둔 힘은 한 발에 몰아 쓴다',
      carrier: 'self', cap: 4,
    },
    passives: [
      { name: '짱박힘 1인자', when: { on: 'turnEnd' }, conds: [{ c: 'playedMax', n: 2 }], fx: [KS('땡땡이', 2), ST('저장', 1)] }, { name: '딱 세 장까지', when: { on: 'play', nth: 3, who: 'any' }, fx: [KS('땡땡이', 1)] },
      
    ],
    ult: { name: '교주의 천벌 - 타이다', cost: 250, fx: [D(5.0), KS('땡땡이', 2)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '근무 중 한 발', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '정조준 사격', 2, '공격', [D(2.3)]),
    card(h, `${h}_s3`, '경비실 문 잠그기', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, 'DX - 슈터', 2, '공격', [D(1.4), PER('땡땡이'), D(0.6), SP('땡땡이', 'all')], {
      unique: true, sig: true, blurb: '한 방 쏠 때만큼은 진심. 놀던 시간이 길수록 탄이 무겁다.',
      oracles: [
        ['한 발만', 1, null, [D(0.8), PER('땡땡이'), D(0.4), SP('땡땡이', 'all')]],
        ['DX - 슈터 풀차지', 3, null, [D(2.2), PER('땡땡이'), D(1.0), SP('땡땡이', 'all')]],
        ['수당 계산', null, null, [D(1.5), PER('땡땡이'), D(0.65), SP('땡땡이', 'all')]],
        ['조준은 진심', null, null, [D(1.4), PER('땡땡이'), D(0.7), SP('땡땡이', 2)]],
        ['퇴근 직전 한 발', null, null, [D(1.7), PER('땡땡이'), D(0.6), SP('땡땡이', 'all')]],
      ],
      blesses: [B('DX 개조', 'power'), B('대충 장전', 'cost'), B('뒤통수', 'weakSpot')],
    }),
    card(h, `${h}_u2`, '일 떠넘기기', 1, '스킬', [ATK(0.15, 'oneAlly'), KS('땡땡이', 1), DRAW(1)], {
      unique: true, tags: ['증발'], blurb: '제 몫을 동료에게 넘기고 옆에서 훈수까지. 이번 턴에 안 넘기면 들킨다.',
      oracles: [
        ['대충 떠넘기기', 0, ['증발'], [KS('땡땡이', 1), DRAW(1)]],
        ['업무 통째로', 2, ['증발'], [ATK(0.3, 'oneAlly'), KS('땡땡이', 3), DRAW(2)]],
        ['훈수 두기', null, ['증발'], [ATK(0.15, 'oneAlly'), KS('땡땡이', 1), ST('협공', 1)]],
        ['숨을 자리', null, ['증발'], [BL(1.5), KS('땡땡이', 2), DRAW(1)]],
        ['동료 핑계', null, null, [ATK(0.15, 'oneAlly'), KS('땡땡이', 1), DRAW(1)]],
      ],
      blesses: [B('웹서핑', 'draw'), B('출근 도장만', null, null, ['개전']), B('기록 삭제', null, [KS('땡땡이', 1)])],
    }),
    card(h, `${h}_u3`, '야근 수당 사격', 2, '공격', [D(2.2), TUNE, KS('땡땡이', 2)], {
      unique: true, blurb: '마지막 AP 까지 딱 맞춰 쏘면 그만큼 수당이 붙는다.',
      oracles: [
        ['30분 연장', 1, null, [D(1.1), TUNE, KS('땡땡이', 1)]],
        ['철야 근무', 3, null, [D(3.4), TUNE, KS('땡땡이', 3)]],
        ['수당부터', null, null, [D(2.2), TUNE, AP(1)]],
        ['칼퇴 사격', null, null, [D(2.2), TUNE, KS('땡땡이', 2), ST('저장', 1)]],
        ['성과급 사격', null, null, [D(2.4), TUNE, KS('땡땡이', 2)]],
      ],
      blesses: [B('야근 커피', 'power'), B('시간 외 근무', 'cost'), B('수당 청구서', null, [KS('땡땡이', 1)])],
    }),
    card(h, `${h}_u4`, '유령 늪 출장', 1, '강화', [ATK(0.1), KS('땡땡이', 2), ST('저장', 1)], {
      unique: true, blurb: '출장이라 쓰고 휴가라 읽는다. 남은 힘은 다음 날로.',
      oracles: [
        ['출장 서류', 0, null, [ATK(0.1), DRAW(1)]],
        ['장기 출장', 2, null, [ATK(0.15), KS('땡땡이', 3), ST('저장', 2)]],
        ['진흙탕 구르기', null, null, [ATK(0.1), KS('땡땡이', 2), ST('피해 감소', 2)]],
        ['상관 자리 털기', null, null, [ATK(0.1), KS('땡땡이', 2), DRAW(1)]],
        ['출장 수당', null, null, [ATK(0.1), KS('땡땡이', 3), ST('저장', 1)]],
      ],
      blesses: [B('출장 가방', null, null, ['개전']), B('풋풋한 냄새', 'atkUp'), B('휴가 계획', null, null, ['보존'])],
    }),
  ];
  write('calc', h, hero, cards);
}

// ── 림 ─────────────────────────────────────────────────
{
  const h = '림';
  const hero = {
    id: h, name: '림', nature: '우울', race: '유령', row: 'front', role: '딜러', style: '계산형', star: 3,
    hp: 830, atk: 129, def: 40, crit: 10,
    blurb: '질서와 균형의 유령. 공격과 스킬을 같은 장수로 맞추면 저울이 선다 — 기울지 않은 판에서 낫이 가장 깊이 들어간다.',
    keyword: {
      name: '낫 자국', desc: '낫 「기역」에 베인 자리 — 쓰리고, 잘 아물지 않는다',
      carrier: 'enemy', cap: 5, decay: 1, per: [{ stat: 'dot', ratio: 0.2 }],
    },
    passives: [
      { name: '저울 맞추기', when: { on: 'play', who: 'any' }, conds: [{ c: 'balanced' }], limit: { per: 'turn', n: 2 }, fx: [KS('낫 자국', 1, EA)] },
      { name: '아재개그 본능', when: { on: 'play', type: '스킬', every: 2 }, fx: [RUSH(1, EA)] },
    ],
    ult: { name: '그림 하베스트', cost: 300, fx: [D(1.3, EA, 2), HL(3.6), KS('낫 자국', 3, EA)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '낫 베기', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '대낫 휘두르기', 2, '공격', [D(2.3)]),
    card(h, `${h}_s3`, '망령 장막', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '스크래치 사이드', 2, '공격', [D(0.9, EA), KS('낫 자국', 1, EA), BAL, HL(1.6)], {
      unique: true, sig: true, blurb: '어둠을 내리고 판 전체를 벤다. 저울이 맞으면 베인 만큼 제 몸이 아문다.',
      oracles: [
        ['한 획', 1, null, [D(0.55, EA), KS('낫 자국', 1, EA), BAL, HL(1.0)]],
        ['어둠 내리기', 3, null, [D(1.5, EA), KS('낫 자국', 2, EA), BAL, HL(2.4)]],
        ['쓰라린 참격', null, null, [D(0.9, EA), KS('낫 자국', 2, EA), BAL, HL(1.6)]],
        ['기역과 함께', null, null, [D(1.0, EA), KS('낫 자국', 1, EA), BAL, HL(1.8)]],
        ['질서의 낫', null, null, [D(0.9, EA), KS('낫 자국', 1, EA), BAL, D(0.6, EA)]],
      ],
      blesses: [B('낫 손질', 'power'), B('외딴 탑', 'cost'), B('셰이디 뒤처리', null, [KS('낫 자국', 1)])],
    }),
    card(h, `${h}_u2`, '날아가는 기역', 1, '공격', [D(0.4, ER, 3), BAL, KS('낫 자국', 1, EA)], {
      unique: true, blurb: '낫에게도 개그를 푼다. 친구 「기역」은 저울이 맞을 때 가장 멀리 난다.',
      oracles: [
        ['기역 던지기', 0, null, [D(0.2, ER, 3), DRAW(1)]],
        ['기역 니은', 2, null, [D(0.5, ER, 4), BAL, KS('낫 자국', 2, EA)]],
        ['되돌아오는 낫', null, ['회수'], [D(0.4, ER, 3), BAL, KS('낫 자국', 1, EA)]],
        ['기역 자 베기', null, null, [D(0.45, ER, 3), BAL, KS('낫 자국', 1, EA)]],
        ['낫의 인격', null, null, [D(0.4, ER, 3), BAL, KS('낫 자국', 1, EA), DRAW(1)]],
      ],
      blesses: [B('날 선 기역', 'power'), B('가벼운 낫', 'cost'), B('찬 바람', 'frost')],
    }),
    card(h, `${h}_u3`, '썰렁 개그', 1, '스킬', [BL(1.5), RUSH(1, EA), BAL, DRAW(1)], {
      unique: true, blurb: '말장난이 나오면 주위 기온이 실제로 떨어진다. 저울이 맞은 날은 손이 하나 더 간다.',
      oracles: [
        ['푸흡', 0, null, [RUSH(1, EA), DRAW(1)]],
        ['냉동고 개그', 2, null, [BL(3.0), RUSH(2, EA), BAL, DRAW(2)]],
        ['호박 스프 개그', null, null, [BL(1.5), HL(1.2), BAL, DRAW(1)]],
        ['얼어붙은 무대', null, null, [BL(1.5), RUSH(1, EA), ST('약화', 1, EA)]],
        ['말줄임표', null, ['보존'], [BL(1.6), RUSH(1, EA), BAL, DRAW(1)]],
      ],
      blesses: [B('다기 세트', 'guard'), B('아재개그 준비', null, null, ['개전']), B('정적', null, [RUSH(1, EA)])],
    }),
    card(h, `${h}_u4`, '균형의 저울', 1, '강화', [ATK(0.1), KS('낫 자국', 1, EA), BAL, ST('불굴', 1)], {
      unique: true, blurb: '다들 혼돈으로 기울면 홀로 반대편에 선다. 혼돈이 사라지면 질서의 자리도 없다.',
      oracles: [
        ['명상', 0, null, [ATK(0.1), DRAW(1)]],
        ['질서의 수호자', 2, null, [ATK(0.15), KS('낫 자국', 2, EA), BAL, ST('불굴', 2)]],
        ['셰이디와 같은 날', null, null, [ATK(0.1), KS('낫 자국', 1, EA), ST('사기', 1)]],
        ['미래의 친구들', null, null, [ATK(0.1), KS('낫 자국', 2, EA), BAL, ST('불굴', 1)]],
        ['진 적 없는 강자', null, null, [ATK(0.1), DEF(0.1), BAL, ST('불굴', 1)]],
      ],
      blesses: [B('차 한 잔', null, null, ['개전']), B('질서의 기운', 'atkUp'), B('저울추', null, null, ['보존'])],
    }),
  ];
  write('calc', h, hero, cards);
}

// ── 실피르 ─────────────────────────────────────────────
{
  const h = '실피르';
  const hero = {
    id: h, name: '실피르', nature: '우울', race: '용족', row: 'mid', role: '딜러', style: '계산형', star: 2,
    hp: 590, atk: 115, def: 27, crit: 10,
    blurb: '자칭 용족 2인자. 언제나 딱 두 번째 — 파티의 둘째 장에 서열이 오르고, 동료가 먼저 움직이면 두 번째로 따라 나선다.',
    keyword: {
      name: '서열', desc: '실피르가 우기는 자리 — 언제나 딱 2위까지, 그 자리에 선 단검은 한 끗 더 날카롭다',
      carrier: 'self', cap: 2, consume: 1, per: [{ stat: 'dealt', v: 0.25 }],
    },
    passives: [
      { name: '넘버 투', when: { on: 'play', nth: 2, who: 'any' }, fx: [KS('서열', 1)] },
      { name: '2인자 쟁탈전', when: { on: 'break' }, limit: { per: 'turn', n: 1 }, fx: [KS('서열', 1)] },
    ],
    ult: { name: '킹갓zi존 실피르 어택', cost: 200, fx: [D(0.4, ER, 8), KS('서열', 2)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '단검 투척', 1, '공격', [D(1.0)]),
    card(h, `${h}_s2`, '쌍단검 투척', 2, '공격', [D(2.2)]),
    card(h, `${h}_s3`, '비늘 가드', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '창공의 지배자', 2, '공격', [D(0.8, EA), RUSH(1, EA), CHAIN, KS('서열', 1)], {
      unique: true, sig: true, tags: ['신속'], blurb: '날개 바람이 적을 굼뜨게 한다. 같은 결의 동료 뒤에 서면 2위가 굳는다.',
      oracles: [
        ['날갯짓', 1, ['신속'], [D(0.5, EA), RUSH(1, EA), CHAIN, KS('서열', 1)]],
        ['창공 제패', 3, ['신속'], [D(1.4, EA), RUSH(2, EA), CHAIN, KS('서열', 2)]],
        ['호수 바람', null, ['신속'], [D(0.8, EA), ST('약화', 1, EA), CHAIN, KS('서열', 1)]],
        ['한 끗 차 비행', null, ['신속'], [D(0.9, EA), RUSH(1, EA), CHAIN, KS('서열', 1)]],
        ['웨스트 블루 제독', null, ['신속'], [D(0.8, EA), RUSH(2, EA), CHAIN, KS('서열', 1)]],
      ],
      blesses: [B('사파이어 비늘', 'power'), B('가벼운 날개', 'cost'), B('호숫물', 'frost')],
    }),
    card(h, `${h}_u2`, '2인자의 자존심', 1, '스킬', [KS('서열', 1), BL(1.5), DRAW(1)], {
      unique: true, tags: ['연계'], blurb: '동료가 먼저 움직이면 두 번째로 따라 나선다. 그 자리는 양보 못 한다.',
      oracles: [
        ['자칭 2인자', 0, ['연계'], [KS('서열', 1), DRAW(1)]],
        ['진짜 2인자', 2, ['연계'], [KS('서열', 2), BL(3.0), DRAW(2)]],
        ['청금이라 부르지 마', null, ['연계'], [KS('서열', 1), ST('약화', 1), DRAW(1)]],
        ['다야 님 곁으로', null, ['연계'], [KS('서열', 1), BL(1.8), DRAW(1)]],
        ['정정당당', null, ['연계', '보존'], [KS('서열', 1), BL(1.6), DRAW(1)]],
      ],
      blesses: [B('도전장', 'draw'), B('요란한 선언', null, null, ['개전']), B('비늘 방패', 'guard')],
    }),
    card(h, `${h}_u3`, '사파이어 단검', 2, '공격', [D(1.0, E1, 2), CHAIN, TOUGH(1)], {
      unique: true, tags: ['약점'], blurb: '실력 차이는 없다시피 한데 늘 한 끗. 그 한 끗을 단검 두 자루로 메운다.',
      oracles: [
        ['단검 한 자루', 1, ['약점'], [D(0.55, E1, 2), CHAIN, TOUGH(1)]],
        ['엑박스칼리버', 3, ['약점'], [D(1.5, E1, 2), CHAIN, TOUGH(2)]],
        ['청금석 빛', null, ['약점'], [D(1.0, E1, 2), ST('취약', 1), CHAIN, TOUGH(1)]],
        ['영롱한 날', null, ['약점'], [D(1.1, E1, 2), CHAIN, TOUGH(1)]],
        ['다이아몬드를 향해', null, ['약점'], [D(1.05, E1, 2), CHAIN, KS('서열', 1), TOUGH(1)]],
      ],
      blesses: [B('반짝이는 것', 'power'), B('빠른 손', 'cost'), B('빨간 건 싫어', null, [ST('약화', 1)])],
    }),
    card(h, `${h}_u4`, '보석이라고 외쳐도', 1, '강화', [ATK(0.1), KS('서열', 2)], {
      unique: true, blurb: '사파이어는 다이아몬드처럼 맑아질 수 없다는 열등감. 그래도 외친다.',
      oracles: [
        ['다짐', 0, null, [ATK(0.1), DRAW(1)]],
        ['보석의 자존심', 2, null, [ATK(0.15), KS('서열', 2), CRIT(0.1)]],
        ['나이아의 호수', null, null, [ATK(0.1), KS('서열', 1), HL(1.5)]],
        ['3연패 설욕', null, null, [ATK(0.1), KS('서열', 2), DRAW(1)]],
        ['사파이어의 용족', null, null, [ATK(0.1), KS('서열', 2), ST('피해 감소', 1)]],
      ],
      blesses: [B('교복', null, null, ['개전']), B('제독 모자', 'atkUp'), B('보석함', null, null, ['보존'])],
    }),
  ];
  write('calc', h, hero, cards);
}
