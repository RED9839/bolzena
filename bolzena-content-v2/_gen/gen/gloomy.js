// 박자형 · 우울 — 아사나 · 키샤(만듦) · 쵸피 · 리스티(씀)
const L = require('./lib');
const { E1, EA, ER, D, DD, BL, SH, HL, ST, RH, KS, SP, SPR, DRAW, AP, NC, TOUGH, RUSH, STRIP, ATK, DEF, CRIT, LINK, PREV, IFR, PERR, PER, IFS, CHAIN, card, B, write } = L;

// ── 쵸피 ───────────────────────────────────────────────
{
  const h = '쵸피';
  const hero = {
    id: h, name: '쵸피', nature: '우울', race: '수인', row: 'mid', role: '딜러', style: '박자형', star: 1,
    hp: 520, atk: 115, def: 25, crit: 10,
    blurb: '티그 님을 동경하는 꼬마 수련생. 같은 동작을 끊지 않고 이어 가며 수련을 쌓는다 — 하루라도 거르면 처음부터, 그래서 매일 한다.',
    keyword: {
      name: '수련', desc: '티그 님처럼 되려고 매일 쌓는 수련 — 이어 갈수록 손도끼가 손에 붙지만, 하루 거르면 처음부터',
      carrier: 'self', cap: 5, per: [{ stat: 'dealt', v: 0.1 }],
      rules: [{ name: '거른 날', when: { on: 'turnEnd' }, conds: [{ c: 'ownNone' }], fx: [SP('수련', 2)] }],
    },
    passives: [
      { name: '도끼 한 방', when: { on: 'break', mine: true }, limit: { per: 'turn', n: 1 }, fx: [KS('수련', 1)] },
      { name: '스승님 보세요', when: { on: 'play', type: '공격' }, conds: [{ c: 'status', id: '리듬', n: 2 }], fx: [KS('수련', 1)] },
    ],
    ult: { name: '교주의 천벌 - 쵸피', cost: 250, fx: [D(4.0), TOUGH(2), KS('수련', 3)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '손도끼 휘두르기', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '온 힘 도끼질', 2, '공격', [D(2.3)]),
    card(h, `${h}_s3`, '통나무 방패', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '퍄오오~', 1, '공격', [D(0.6, EA), PERR, D(0.2, EA), LINK, KS('수련', 1)], {
      unique: true, sig: true, blurb: '작은 몸에서 나오는 큰 소리. 박자가 오른 날엔 온 마을이 울린다.',
      oracles: [
        ['작은 기합', null, ['보존'], [D(0.7, EA), PERR, D(0.2, EA), LINK, KS('수련', 1)]],
        ['온 마을이 울리게', 2, null, [D(1.3, EA), PERR, D(0.4, EA), LINK, KS('수련', 2)]],
        ['스승님 보세요', null, null, [D(0.6, EA), PERR, D(0.2, EA), LINK, ST('사기', 1)]],
        ['기합도 수련', null, null, [D(0.6, EA), PERR, D(0.25, EA), LINK, KS('수련', 2)]],
        ['고구마 아니에요!', null, null, [D(0.8, EA), PERR, D(0.25, EA), LINK, KS('수련', 1)]],
      ],
      blesses: [B('또박또박 해요체', 'power'), B('새벽 수련', null, null, ['개전']), B('기운 빠지는 소리', null, [RUSH(1, EA)])],
    }),
    card(h, `${h}_u2`, '결대로 쪼개기', 2, '공격', [L.STRIP(), D(2.0), PER('수련'), D(0.2)], {
      unique: true, blurb: '통나무든 방패든 결대로 쪼갠다. 쌓은 수련만큼 결이 잘 보인다.',
      oracles: [
        ['잔가지 쪼개기', 1, null, [L.STRIP(), D(1.2), PER('수련'), D(0.12)]],
        ['통나무 산더미', 3, null, [L.STRIP(), D(3.4), PER('수련'), D(0.3)]],
        ['티그 님 앞에서', null, null, [D(2.2), PER('수련'), D(0.2), ST('사기', 1)]],
        ['결이 보인다', null, null, [L.STRIP(), D(2.1), PER('수련'), D(0.3)]],
        ['던진 도끼 한 자루', null, null, [L.STRIP(), D(2.4), TOUGH(2)]],
      ],
      blesses: [B('장작 패기 특기', 'power'), B('손에 익은 도끼', 'cost'), B('한참 못 일어남', null, [ST('약화', 2)])],
    }),
    card(h, `${h}_u3`, '티그 님처럼', 1, '강화', [ATK(0.1), KS('수련', 2)], {
      unique: true, blurb: '멋진 전사는 매일 조금씩 자란다.',
      oracles: [
        ['첫 장작', null, ['개전'], [ATK(0.1), KS('수련', 3)]],
        ['최강의 전사', 2, null, [ATK(0.15), CRIT(0.15), KS('수련', 3)]],
        ['강한 사람은 다 스승', null, null, [ATK(0.1), KS('수련', 2), ATK(0.15, 'oneAlly')]],
        ['훈련법 찾기', null, null, [ATK(0.1), KS('수련', 3), DRAW(1)]],
        ['우로스 님 친구', null, null, [ATK(0.1), DEF(0.1), KS('수련', 2)]],
      ],
      blesses: [B('다음엔 꼭 이길 거예요', 'atkUp'), B('도끼 손질', null, null, ['보존']), B('캠프파이어', 'draw')],
    }),
    card(h, `${h}_u4`, '심부름길 호신술', 1, '스킬', [BL(2.0), LINK, KS('수련', 1), DRAW(1)], {
      unique: true, blurb: '습격당해도 몸부터 지킨다. 손에 익은 순서대로 이어 가면 그것도 수련이다.',
      oracles: [
        ['가볍게 막기', null, ['보존'], [BL(2.4), LINK, KS('수련', 1), DRAW(1)]],
        ['심부름 열 개', 2, null, [BL(3.6), LINK, KS('수련', 3), DRAW(2)]],
        ['선배들 따라 하기', null, null, [BL(2.0), ST('협공', 1), LINK, KS('수련', 1)]],
        ['오늘의 수련 일지', null, null, [BL(2.0), LINK, KS('수련', 2), DRAW(1)]],
        ['지는 건 싫어!', null, null, [BL(2.8), LINK, KS('수련', 1), DRAW(1)]],
      ],
      blesses: [B('통나무 방패', 'guard'), B('심부름 바구니', null, null, ['개전']), B('무서운 줄 모름', null, [ST('약화', 1)])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 리스티 ─────────────────────────────────────────────
{
  const h = '리스티';
  const hero = {
    id: h, name: '리스티', nature: '우울', race: '엘프', row: 'back', role: '딜러', style: '박자형', star: 3,
    hp: 550, atk: 135, def: 25, crit: 10,
    blurb: '방구석 프로게이머. 끊기지 않는 입력을 한 턴에 몰아 넣는다 — 콤보는 턴을 넘기지 못하니, 이번 턴에 다 쏟는다.',
    keyword: {
      name: '콤보', desc: '끊기지 않고 이어 붙인 입력 — 길어질수록 급소가 보이지만, 판이 넘어가면 처음부터',
      carrier: 'self', cap: 5, decayAll: true, per: [{ stat: 'dealt', v: 0.1 }],
    },
    passives: [
      { name: '개인정보 수집', when: { on: 'play', type: '공격' }, fx: [KS('콤보', 1)] },
      { name: '핫키 연타', when: { on: 'rhythm', n: 3 }, fx: [AP(1)] },
    ],
    ult: { name: '복셀 글리치', cost: 150, fx: [ST('기절', 1, E1), KS('콤보', 2), RH(2)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s3`],
  };
  const cards = [
    card(h, `${h}_s1`, '키보드 연타', 1, '공격', [D(1.1)]),
    card(h, `${h}_s2`, '키보드 샷건', 2, '공격', [D(2.4)]),
    card(h, `${h}_s3`, '방구석 바리케이드', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '테크노맨시', 2, '공격', [D(0.4, ER, 3), PER('콤보'), D(0.25), PERR, D(0.25)], {
      unique: true, sig: true, blurb: '안 쓰러지면 다른 적을 찾아 다시 친다. 이어 붙인 입력만큼 마지막 한 방이 굵다.',
      oracles: [
        ['핫키', 1, null, [D(0.3, ER, 3), PER('콤보'), D(0.15)]],
        ['풀 콤보', 3, null, [D(0.5, ER, 3), PER('콤보'), D(0.4), PERR, D(0.35)]],
        ['리그 오브 엘프', null, null, [D(0.4, ER, 3), PER('콤보'), D(0.3), ST('약화', 1)]],
        ['현실 개변', null, null, [D(0.4, ER, 3), PER('콤보'), D(0.3), PERR, D(0.3)]],
        ['오늘의 POTG', null, null, [D(0.5, ER, 3), PER('콤보'), D(0.35)]],
      ],
      blesses: [B('후후', 'power'), B('매크로', 'cost'), B('랙 유발', 'frost')],
    }),
    card(h, `${h}_u2`, '훗, 못 깨는 게임은 없지', 1, '스킬', [DRAW(2), KS('콤보', 1), LINK, AP(1)], {
      unique: true, blurb: '패와 AP 를 채워 긴 턴을 연다. 입력이 끊기지 않았으면 한 판 더.',
      oracles: [
        ['퀵 세이브', 0, null, [DRAW(1), KS('콤보', 1)]],
        ['밤샘 공략', 2, null, [DRAW(3), KS('콤보', 2), LINK, AP(2)]],
        ['독타 페퍼', null, null, [DRAW(2), KS('콤보', 2), LINK, AP(1)]],
        ['리스티의 슈퍼 세이브', null, null, [DRAW(2), KS('콤보', 1), AP(1)]],
        ['공략집', null, ['보존'], [DRAW(2), KS('콤보', 1), LINK, AP(1)]],
      ],
      blesses: [B('컨트롤러', 'draw'), B('로그인 보상', null, null, ['개전']), B('콤보 연습', null, [KS('콤보', 1)])],
    }),
    card(h, `${h}_u3`, '다 마신 캔', 1, '공격', [D(1.0), CHAIN, D(0.7)], {
      unique: true, blurb: '같은 우울 박자에 이어 던지면 한 캔 더 날아간다.',
      oracles: [
        ['빈 캔', 0, null, [D(0.45), CHAIN, D(0.35)]],
        ['캔 피라미드', 2, null, [D(2.0), CHAIN, D(1.5)]],
        ['연속 투척', null, null, [D(0.55, E1, 2), CHAIN, D(0.8)]],
        ['과자 부스러기', null, null, [D(1.0), DRAW(1), CHAIN, D(0.7)]],
        ['리필', null, ['회수'], [D(1.0), CHAIN, D(0.8)]],
      ],
      blesses: [B('독타 페퍼', 'power'), B('원 핸드', 'cost'), B('바닥에 굴러다님', null, [ST('약화', 1)])],
    }),
    card(h, `${h}_u4`, '글러브', 1, '스킬', [DRAW(1), KS('콤보', 1), PREV('공격'), RH(1)], {
      unique: true, tags: ['연계'], blurb: '곰인형 속 AI. 동료의 입력에 묻어 손을 굴린다.',
      oracles: [
        ['양심 회로', null, ['연계', '보존'], [DRAW(1), KS('콤보', 1), PREV('공격'), RH(1)]],
        ['현실 개변 장치', 0, ['연계'], [DRAW(1), PREV('공격'), RH(1)]],
        ['말동무', null, ['연계'], [DRAW(2), KS('콤보', 1)]],
        ['해킹 보조', null, ['연계'], [DRAW(1), KS('콤보', 2), PREV('공격'), RH(1)]],
        ['리그 오브 엘프의 최강자', null, null, [ATK(0.15), KS('콤보', 2), DRAW(1)], true],
      ],
      blesses: [B('곰인형 솜', 'draw'), B('기지에서 들고 나옴', null, null, ['개전']), B('글러브의 잔소리', null, [KS('콤보', 1)])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 아사나 ─────────────────────────────────────────────
{
  const h = '아사나';
  const hero = {
    id: h, name: '아사나', nature: '우울', race: '마녀', row: 'front', role: '탱커', style: '박자형', star: 3,
    hp: 1000, atk: 90, def: 67, crit: 5,
    blurb: '볼-요가 전도사. 자세 → 분출 → 자세, 정해진 순서를 따라가면 마력이 터지고, 파티를 붙잡아 억지로라도 자세를 잡게 한다.',
    keyword: {
      name: '볼-요가', desc: '아사나가 붙잡고 시키는 자세 — 억지로라도 자세를 잡은 동료는 덜 아프고, 밤이 지나면 한 자세씩 풀린다',
      carrier: 'ally', cap: 4, decay: 1, per: [{ stat: 'taken', v: -0.05 }],
    },
    passives: [
      { name: '세 번째 분출', when: { on: 'play', seq: ['스킬', '공격', '스킬'], who: 'any' }, fx: [DD(1.0), TOUGH(2), RH(2)] },
      { name: '건강한 몸에 건강한 정신', when: { on: 'lowHp', pct: 0.5 }, fx: [HL(2.5)] },
    ],
    ult: { name: '명상 시간', cost: 250, fx: [DD(0.25, EA, 6), HL(3.0), KS('볼-요가', 2)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s2`],
  };
  const cards = [
    card(h, `${h}_s1`, '요가 킥', 1, '공격', [D(1.0)]),
    card(h, `${h}_s2`, '나무 자세', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '마력 분출', 2, '공격', [BL(2.0), DD(1.0), PREV('스킬'), KS('볼-요가', 1)], {
      unique: true, sig: true, blurb: '자세를 잡은 다음 숨을 내뱉듯 마력을 쏜다.',
      oracles: [
        ['짧은 분출', 1, null, [BL(1.2), DD(0.6), PREV('스킬'), KS('볼-요가', 1)]],
        ['마지막 분출', 3, null, [BL(3.0), DD(1.6), PREV('스킬'), ST('기절', 1, E1)]],
        ['심마체', null, null, [BL(2.0), DD(1.1), PREV('스킬'), KS('볼-요가', 2)]],
        ['기합으로 넘기기', null, null, [BL(2.4), DD(1.2), PREV('스킬'), RH(1)]],
        ['엘리아스 체형', null, ['분쇄'], [BL(2.0), DD(1.2), PREV('스킬'), KS('볼-요가', 1)]],
      ],
      blesses: [B('호흡 맞추기', 'power'), B('까치발', 'guard'), B('교정 들어갑니다', null, [KS('볼-요가', 1)])],
    }),
    card(h, `${h}_u2`, '볼-요가', 1, '스킬', [KS('볼-요가', 2), BL(1.5), PREV('공격'), RH(1)], {
      unique: true, blurb: '권하는 게 아니라 붙잡고 시킨다. 킥 다음의 자세가 제일 깊다.',
      oracles: [
        ['아침 스트레칭', 0, null, [KS('볼-요가', 1), DRAW(1)]],
        ['단체 수업', 2, null, [KS('볼-요가', 3), BL(3.5), PREV('공격'), RH(2)]],
        ['붙잡고 시키기', null, null, [KS('볼-요가', 2), BL(1.5), PREV('공격'), ST('약화', 1, EA)]],
        ['엘튜브 요가 방송', null, null, [KS('볼-요가', 2), BL(1.5), DRAW(1)]],
        ['까치발 자세', null, ['보존'], [KS('볼-요가', 2), BL(1.7), PREV('공격'), RH(1)]],
      ],
      blesses: [B('요가 매트', 'guard'), B('새벽 수련', null, null, ['개전']), B('들숨 날숨', null, [RH(1)])],
    }),
    card(h, `${h}_u3`, '명상용 진자', 1, '공격', [DD(0.35, E1, 2), PREV('스킬'), DRAW(1)], {
      unique: true, tags: ['약점'], blurb: '흔들리는 진자를 따라 숨을 고르면, 다음 자세가 손에 잡힌다.',
      oracles: [
        ['진자 하나', 0, ['약점'], [DD(0.3), PREV('스킬'), DRAW(1)]],
        ['쌍진자', 2, ['약점'], [DD(0.4, E1, 4), PREV('스킬'), RH(2)]],
        ['흔들림 교정', null, ['약점'], [DD(0.35, E1, 2), ST('약화', 1), PREV('스킬'), DRAW(1)]],
        ['호흡 맞추기', null, ['약점'], [DD(0.4, E1, 2), PREV('스킬'), RH(1)]],
        ['구부정 금지', null, ['약점'], [DD(0.4, E1, 2), PREV('스킬'), DRAW(1)]],
      ],
      blesses: [B('진자 추', 'power'), B('작은 진자', 'cost'), B('최면', 'frost')],
    }),
    card(h, `${h}_u4`, '불가능은 없다', 2, '강화', [DEF(0.15), KS('볼-요가', 2), ST('불굴', 1)], {
      unique: true, blurb: '안 되는 자세도 기합으로 넘긴다. 허리를 삐끗해도.',
      oracles: [
        ['작은 다짐', 1, null, [DEF(0.1), KS('볼-요가', 1), ST('불굴', 1)]],
        ['학위를 내려놓고', 3, null, [DEF(0.2), KS('볼-요가', 3), ST('불굴', 2)]],
        ['벨리타 님의 인정', null, null, [DEF(0.15), KS('볼-요가', 2), ST('사기', 1)]],
        ['허리를 삐끗해도', null, null, [DEF(0.15), KS('볼-요가', 2), HL(2.0)]],
        ['심마체 이론', null, null, [DEF(0.15), KS('볼-요가', 3), ST('불굴', 1)]],
      ],
      blesses: [B('건강식', 'defUp'), B('요가복', null, null, ['개전']), B('보존식', null, null, ['보존'])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 키샤 ───────────────────────────────────────────────
{
  const h = '키샤';
  const hero = {
    id: h, name: '키샤', nature: '우울', race: '유령', row: 'mid', role: '서포터', style: '박자형', star: 3,
    hp: 570, atk: 86, def: 60, crit: 5,
    blurb: '지하 아이돌. 이번 턴 셋리스트의 넷째 곡에서 함성이 터진다 — 동료가 공격할수록 객석이 달아오르고, 그 열기는 무대 전체에 번진다.',
    keyword: {
      name: '열기', desc: '키샤가 무대에서 끌어올린 객석의 열기 — 달아오른 만큼 무대 위 모두가 세게 친다',
      carrier: 'self', cap: 4, per: [{ stat: 'dealt', v: 0.05, who: 'allies' }],
    },
    passives: [
      { name: '넷째 곡', when: { on: 'play', nth: 4, who: 'any' }, fx: [ST('협공', 1), KS('열기', 1)] },
      { name: '펜스 너머의 너', when: { on: 'play', type: '공격', who: 'any', every: 2 }, fx: [KS('열기', 1), RH(1)] },
    ],
    ult: { name: '고! 고! 러브 스테이지!', cost: 200, fx: [D(1.5, EA), ST('약화', 1, EA), KS('열기', 3)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s2`],
  };
  const cards = [
    card(h, `${h}_s1`, '진심 하트', 1, '공격', [D(1.0)]),
    card(h, `${h}_s2`, '팬레터 낭독', 1, '스킬', [HL(2.1)]),
    card(h, `${h}_u1`, '궁극의 멜로디', 1, '스킬', [ATK(0.15, 'oneAlly'), PER('열기'), HL(0.4), PREV('공격'), RH(1)], {
      unique: true, sig: true, blurb: '가장 센 팬 한 명에게 부르는 노래. 객석이 뜨거울수록 상처가 아문다.',
      oracles: [
        ['짧은 멜로디', 0, null, [DRAW(1), PREV('공격'), RH(1)]],
        ['풀 셋리스트', 2, null, [ATK(0.3, 'oneAlly'), PER('열기'), HL(0.6), PREV('공격'), RH(2)]],
        ['체리 레드 무대', null, null, [ATK(0.15, 'oneAlly'), PER('열기'), HL(0.5), ST('사기', 1)]],
        ['팬 서비스', null, null, [ATK(0.15, 'oneAlly'), ST('협공', 1), PREV('공격'), RH(1)]],
        ['언더돌의 진심', null, ['보존'], [ATK(0.15, 'oneAlly'), PER('열기'), HL(0.45), PREV('공격'), RH(1)]],
      ],
      blesses: [B('팬 한 명 한 명', 'heal'), B('오프닝 무대', null, null, ['개전']), B('박수 박자', null, [RH(1)])],
    }),
    card(h, `${h}_u2`, '체리 레드 조명봉', 1, '스킬', [KS('열기', 2), DRAW(1), PREV('공격'), RH(1)], {
      unique: true, blurb: '객석의 붉은 조명봉이 박자에 맞춰 흔들린다.',
      oracles: [
        ['조명봉 하나', 0, null, [KS('열기', 1), DRAW(1)]],
        ['객석 전체', 2, null, [KS('열기', 4), DRAW(2), PREV('공격'), RH(2)]],
        ['붉은 길바닥', null, null, [KS('열기', 2), DRAW(1), ST('약화', 1, EA)]],
        ['팬 이름 부르기', null, null, [KS('열기', 2), ST('협공', 1), PREV('공격'), RH(1)]],
        ['응원 조명', null, ['보존'], [KS('열기', 2), DRAW(1), PREV('공격'), RH(1)]],
      ],
      blesses: [B('무대 소품', 'draw'), B('의상 쇼핑', null, null, ['개전']), B('떼창 연습', null, [KS('열기', 1)])],
    }),
    card(h, `${h}_u3`, '하트 파동', 1, '공격', [D(0.3, E1, 3), PREV('스킬'), RH(1)], {
      unique: true, tags: ['연계'], blurb: '동료가 움직이면 무대 뒤에서 하트가 날아간다.',
      oracles: [
        ['작은 하트', null, ['연계', '보존'], [D(0.35, E1, 3), PREV('스킬'), RH(1)]],
        ['하트 폭격', 2, ['연계'], [D(0.4, E1, 5), PREV('스킬'), RH(2)]],
        ['도취', null, ['연계'], [D(0.3, E1, 3), ST('약화', 1), PREV('스킬'), RH(1)]],
        ['팬 한 명 한 명', null, ['연계'], [D(0.35, E1, 3), PREV('스킬'), RH(1), KS('열기', 1)]],
        ['입만 맞추기', 0, ['연계'], [D(0.2, E1, 3), DRAW(1)]],
      ],
      blesses: [B('윙크', 'power'), B('하트 손', 'frost'), B('함성 유도', null, [KS('열기', 1)])],
    }),
    card(h, `${h}_u4`, '떼창 지진', 2, '공격', [D(0.9, EA), PER('열기'), D(0.25, EA), SP('열기', 'all')], {
      unique: true, blurb: '지하 공연장이 흔들린다. 달아오른 열기를 한꺼번에 쏟아 낸다.',
      oracles: [
        ['작은 떼창', 1, null, [D(0.55, EA), PER('열기'), D(0.15, EA), SP('열기', 'all')]],
        ['지하 공연장 붕괴', 3, null, [D(1.5, EA), PER('열기'), D(0.35, EA), SP('열기', 'all')]],
        ['앙코르', null, null, [D(1.0, EA), PER('열기'), D(0.25, EA)]],
        ['함성', null, null, [D(1.1, EA), PER('열기'), D(0.3, EA), SP('열기', 'all')]],
        ['킹짱 언더돌의 꿈', null, null, [DEF(0.15), KS('열기', 3), ST('사기', 1)], true],
      ],
      blesses: [B('마이크 하울링', 'power'), B('소속사 없음', 'cost'), B('바닥 울림', null, [RUSH(1, EA)])],
    }),
  ];
  write('rhythm', h, hero, cards);
}
