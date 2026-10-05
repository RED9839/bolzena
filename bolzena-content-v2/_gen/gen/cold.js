// 박자형 · 냉정 — 리코타(만듦) · 아멜리아(씀)
const L = require('./lib');
const { E1, EA, D, DD, BL, SH, HL, ST, RH, KS, SP, DRAW, AP, NC, TOUGH, RUSH, ATK, DEF, LINK, PREV, IFR, PERR, PER, card, B, write } = L;

// ── 리코타 ─────────────────────────────────────────────
{
  const h = '리코타';
  const hero = {
    id: h, name: '리코타', nature: '냉정', race: '요정', row: 'front', role: '탱커', style: '박자형', star: 3,
    hp: 960, atk: 90, def: 67, crit: 5,
    blurb: '요정 왕궁 셰프. 전채 다음엔 메인, 메인 다음엔 디저트 — 공격과 스킬을 번갈아 낼 때마다 접시가 나가고 식탁에 박자가 깔린다.',
    keyword: {
      name: '접시', desc: '손님 앞에 나간 접시 — 코스가 이어지는 동안 셰프는 꿈쩍도 하지 않지만, 식으면 한 접시씩 물린다',
      carrier: 'self', cap: 3, decay: 1, per: [{ stat: 'def', v: 0.1 }],
    },
    passives: [
      { name: '웍질', when: { on: 'play', seq: ['공격', '스킬'], who: 'any' }, fx: [KS('접시', 1), RH(1)] },
      { name: '웍질', when: { on: 'play', seq: ['스킬', '공격'], who: 'any' }, fx: [KS('접시', 1), RH(1)] },
      { name: '다음 접시 나갑니다', when: { on: 'rhythm', n: 3 }, fx: [SH(1.5)] },
    ],
    ult: { name: '준비만전', cost: 200, fx: [DD(2.5), TOUGH(2), SH(2.0)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s2`],
  };
  const cards = [
    card(h, `${h}_s1`, '주방칼 썰기', 1, '공격', [D(1.0)]),
    card(h, `${h}_s2`, '냄비 뚜껑', 1, '스킬', [BL(2.0)]),
    card(h, `${h}_u1`, '리코타 풀코스', 2, '스킬', [SH(3.0), L.PER('접시'), SH(0.6), PREV('공격'), RH(2)], {
      unique: true, sig: true, blurb: '전채 다음에 나온 메인은 식탁 전체의 박자를 맞춘다.',
      oracles: [
        ['전채 먼저', 1, null, [SH(2.0), PER('접시'), SH(0.5), PREV('공격'), RH(2)]],
        ['메인 디시', 3, null, [SH(5.0), PER('접시'), SH(1.0), PREV('공격'), RH(3)]],
        ['퐁듀 분수', null, null, [SH(3.0), PER('접시'), SH(0.6), AP(1)]],
        ['코스 요리', null, null, [SH(3.2), PER('접시'), SH(0.7), RH(2)]],
        ['파인애플 피자 소각', null, null, [DD(1.3, EA), PER('접시'), DD(0.3, EA), PREV('공격'), RH(2)]],
      ],
      blesses: [B('우주식량 금지', 'guard'), B('접시 데우기', null, null, ['보존']), B('찐빵 본뜨기', null, [RH(1)])],
    }),
    card(h, `${h}_u2`, '다 조려버리겠습니다', 1, '공격', [DD(0.7), PREV('스킬'), KS('접시', 1), RH(1)], {
      unique: true, tags: ['분쇄'], blurb: '메인 다음의 웍질은 다음 코스의 전채가 된다.',
      oracles: [
        ['살짝 조리기', 0, ['분쇄'], [DD(0.4), DRAW(1), PREV('스킬'), KS('접시', 1)]],
        ['센 불', 2, ['분쇄'], [DD(1.7), PREV('스킬'), KS('접시', 2), RH(2)]],
        ['볶음 한 바퀴', null, ['분쇄'], [DD(0.6, EA), PREV('스킬'), KS('접시', 1), RH(1)]],
        ['냄비째로', null, ['분쇄'], [DD(0.9), ST('약화', 1), PREV('스킬'), KS('접시', 1)]],
        ['다 조렸습니다', null, ['분쇄'], [DD(0.7), PER('접시'), DD(0.35), PREV('스킬'), RH(1)]],
      ],
      blesses: [B('웍 돌리기', 'power'), B('화력 조절', null, [RH(1)]), B('조림 간 보기', 'draw')],
    }),
    card(h, `${h}_u3`, '눈치 없는 서빙', 1, '스킬', [BL(2.5), PREV('공격'), DRAW(1), RH(1)], {
      unique: true, blurb: '손님은 겁에 질려도 시선은 셰프에게 꽂히고, 주방은 그새 다음 박자를 잡는다.',
      oracles: [
        ['낯선 메뉴판', null, null, [BL(3.0), PREV('공격'), DRAW(1), RH(1)]],
        ['주문 확인합니다', 2, null, [BL(4.6), ST('피해 감소', 2), PREV('공격'), RH(2)]],
        ['기괴한 데코', null, null, [BL(2.5), ST('약화', 1, EA), PREV('공격'), DRAW(1)]],
        ['손님 맞이', null, ['보존'], [BL(2.8), PREV('공격'), DRAW(1), RH(1)]],
        ['서빙 동선', null, null, [BL(2.5), ST('실드 유지', 1), PREV('공격'), DRAW(1)]],
      ],
      blesses: [B('앞치마', 'guard'), B('메뉴 추천', null, null, ['개전']), B('냅킨', null, [ST('피해 감소', 1)])],
    }),
    card(h, `${h}_u4`, '요리를 무시하지 마십시오', 1, '강화', [DEF(0.1), ST('반격', 1), PREV('스킬'), RH(2)], {
      unique: true, blurb: '제 요리를 깔본 자는 식칼 맛을 먼저 본다.',
      oracles: [
        ['식칼 맛', null, null, [DEF(0.1), ST('반격', 2), PREV('스킬'), RH(2)]],
        ['셰프의 자존심', 2, null, [DEF(0.15), ST('반격', 2), KS('접시', 3)]],
        ['레스토랑 대출금', null, null, [DEF(0.1), KS('접시', 2), ST('반격', 1)]],
        ['악역 영애', 0, null, [DEF(0.1), DRAW(1)]],
        ['파인애플 피자 금지', null, null, [DEF(0.1), ST('면역', 1), ST('반격', 2)]],
      ],
      blesses: [B('수석 졸업', null, null, ['개전']), B('합니다체', 'defUp'), B('다시 굽기', null, [RH(1)])],
    }),
  ];
  write('rhythm', h, hero, cards);
}

// ── 아멜리아 ───────────────────────────────────────────
{
  const h = '아멜리아';
  const hero = {
    id: h, name: '아멜리아', nature: '냉정', race: '엘프', row: 'back', role: '서포터', style: '박자형', star: 3,
    hp: 520, atk: 86, def: 58, crit: 5,
    blurb: '자타공인 최고의 비서. 결재가 박자에 맞춰 올라온 날, 셋째 서류에 레이저가 꽂힌다 — 리듬은 읽기만 하고 남겨 둔다.',
    keyword: {
      name: '전류', desc: '위성 레이저가 남긴 전류 — 흐르는 동안 몸이 굳어 무엇을 맞든 더 아프다',
      carrier: 'enemy', cap: 3, decay: 1, per: [{ stat: 'taken', v: 0.1 }],
    },
    passives: [
      { name: '초고속 썬더 레이저', when: { on: 'play', nth: 3, who: 'any' }, fx: [PERR, D(0.4, EA), KS('전류', 1, EA)] },
      { name: '지원 요청의 건', when: { on: 'rhythm', n: 3 }, fx: [DRAW(1)] },
    ],
    ult: { name: '초전도 레이저 캐논', cost: 200, fx: [D(0.2, EA, 8), RH(3), ST('기절', 1, E1)] },
    starter: [`${h}_s1`, `${h}_s1`, `${h}_s2`, `${h}_s2`],
  };
  const cards = [
    card(h, `${h}_s1`, '레이저 포인트 샷', 1, '공격', [DD(0.6)]),
    card(h, `${h}_s2`, '응급 키트', 1, '스킬', [HL(2.1)]),
    card(h, `${h}_u1`, '새틀라이트 전술폭격', 2, '공격', [D(0.6, EA), PERR, D(0.45, EA)], {
      unique: true, sig: true, blurb: '결재가 박자에 맞춰 올라올수록 폭격이 촘촘해진다.',
      oracles: [
        ['좌표 확인 완료', null, ['개전'], [D(0.7, EA), PERR, D(0.5, EA)]],
        ['새틀라이트 오버히트', 3, null, [D(0.3, EA, 4), PERR, D(0.65, EA)]],
        ['지원 요청의 건', null, null, [PERR, D(0.55, EA), ST('협공', 1)]],
        ['과부하 전류', null, null, [PERR, D(0.65, EA), KS('전류', 2, EA)]],
        ['결재 등급: 그냥', null, null, [D(0.55, EA), PERR, D(0.45, EA), ST('약화', 1, EA)]],
      ],
      blesses: [B('최고의 비서', 'power'), B('야근 없는 폭격', 'cost'), B('엘레나땅 마크17', null, [RH(2)])],
    }),
    card(h, `${h}_u2`, '감봉 통보', 2, '공격', [D(2.2), IFR(3), ST('기절', 1, E1)], {
      unique: true, blurb: '서류가 제때 올라온 날이면 통보서와 함께 레이저가 꽂혀 굳어 버린다.',
      oracles: [
        ['사전 통보', null, ['개전'], [D(2.5), IFR(3), ST('기절', 1, E1)]],
        ['전원 감봉', 3, null, [D(3.4), ST('약화', 2), IFR(3), ST('기절', 1, E1)]],
        ['남은 예산 이월', null, null, [D(2.2), ST('저장', 1), IFR(3), ST('기절', 1, E1)]],
        ['부서 전체 징계', null, null, [D(2.2), KS('전류', 2), IFR(3), ST('기절', 1, E1)]],
        ['입 모양 읽기', null, null, [D(2.4), RUSH(2), IFR(3), ST('기절', 1, E1)]],
      ],
      blesses: [B('격식체 막말', 'power'), B('빠른 결재', 'cost'), B('싸늘한 시선', null, [ST('약화', 1)])],
    }),
    card(h, `${h}_u3`, '모나티엄 행정 대행', 1, '스킬', [DRAW(1), PREV('공격'), RH(1)], {
      unique: true, tags: ['연계'], blurb: '동료가 칼을 휘두른 바로 다음이 결재 타이밍이다.',
      oracles: [
        ['밀린 결재', null, ['연계', '보존'], [DRAW(2), PREV('공격'), RH(1)]],
        ['지원 요청 접수', 0, ['연계'], [DRAW(1), PREV('공격'), RH(1)]],
        ['시장님 일정 관리', null, ['연계'], [DRAW(1), ATK(0.15, 'oneAlly'), PREV('공격'), RH(1)]],
        ['감시 보고서', null, ['연계'], [DRAW(1), KS('전류', 1, EA), PREV('공격'), RH(1)]],
        ['휴가지에서도 일함', null, ['연계'], [DRAW(1), ST('협공', 1), PREV('공격'), RH(1)]],
      ],
      blesses: [B('출근 일등', null, null, ['개전']), B('서류 정리', 'draw'), B('직위로 누르기', null, [ST('약화', 1)])],
    }),
    card(h, `${h}_u4`, '감시카메라 32대', 1, '공격', [D(0.8, EA), PREV('스킬'), KS('전류', 1, EA)], {
      unique: true, blurb: '보고가 끝난 다음 컷에서 셔터가 내려간다.',
      oracles: [
        ['녹화 중', null, ['보존'], [D(0.9, EA), PREV('스킬'), KS('전류', 1, EA)]],
        ['전 채널 출력', 0, null, [D(0.3, EA), DRAW(1), PREV('스킬'), KS('전류', 1, EA)]],
        ['교주에게만 너그럽게', null, null, [D(0.8, EA), ATK(0.15, 'oneAlly'), PREV('스킬'), KS('전류', 1, EA)]],
        ['브로마이드 수호', null, null, [D(0.8, EA), PREV('스킬'), KS('전류', 3, EA)]],
        ['왕자님의 은총', null, null, [ATK(0.1), D(0.8, EA), PREV('스킬'), KS('전류', 1, EA)], true],
      ],
      blesses: [B('사각지대 없음', 'power'), B('항시 대기', null, null, ['보존']), B('산타 경보', null, [RUSH(1, EA)])],
    }),
  ];
  write('rhythm', h, hero, cards);
}
