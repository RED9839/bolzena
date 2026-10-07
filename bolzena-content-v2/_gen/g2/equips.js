// G2 — 장비 = 원작 아티팩트 86(한국 서버). 이름 · 등급 · 추가 스탯은 원작 그대로(볼제나 눈금으로 환산),
// 효과는 원작 효과의 결을 키워드 계기 한 줄로. 원작에 부위가 없어 아이콘 생김새 · 효과로 무기 · 방어구 · 장신구를 나눴다.
// 원작이 「특수 효과 없음, 스탯만」 인 것은 효과 없이 스탯만(×1.2).
const L = require('./lib');
const { S, E1, EA, ER, D, AP, NC, G, T, CL, RAND, SH, HEAL, EX, ST, SP, MAKE, MOD, rule, perTurn, perFight } = L;

// 이름 → [id, 칸, 효과 규칙(없으면 null), 한 줄, 애착 { stats, effect }]
const DEF = {
  // ── 전설 · 애착 ──
  '벨리타의 지팡이': ['eq_velitastaff', '무기',
    rule('마녀 여왕의 마력', { on: 'always' }, [MOD('dealtMod', 0.2, { target: 'self' })]),
    '마녀 여왕의 지팡이. 손잡이 속에 초콜릿 한 조각이 숨어 있다.',
    { stats: { atk: 8 }, effect: [rule('비밀 창고 열쇠', { on: 'fightStart' }, [ST('당', 1)])] }],
  '에르핀의 지팡이': ['eq_erpinstaff', '무기',
    rule('다치지 않은 날', { on: 'turnStart' }, [G(15)], { conds: [{ c: 'hpMin', pct: 0.99 }] }),
    '요정 여왕의 마력 지팡이. 하나도 안 다친 날엔 마력이 넘친다.',
    { stats: { atk: 8 }, effect: [rule('외상 장부 펼치기', { on: 'turnStart' }, [ST('외상', 1)], { conds: [{ c: 'hpMin', pct: 0.99 }] })] }],
  '실라의 바람살': ['eq_sillaarrow', '무기',
    rule('바람 가르기', { on: 'play', type: '공격', every: 3 }, [EX(0.7)]),
    '바람을 타고 날아가는 화살. 연달아 쏘면 한 발이 덤으로 붙는다.',
    { stats: { atk: 8 }, effect: [rule('맞바람 한 발', { on: 'play', type: '공격', every: 3 }, [ST('정면 승부', 1, 'oneEnemy')])] }],
  '셰이디의 차원 사슬낫': ['eq_shadyscythe', '무기',
    rule('떨어지는 인형', { on: 'play', type: '공격', every: 4 }, [EX(1.5, 'randomEnemy')]),
    '차원을 가르는 사슬낫. 넷째 휘두름마다 어디선가 인형이 떨어진다.',
    { stats: { atk: 8 }, effect: [rule('짓궂은 인형', { on: 'play', type: '공격', every: 4 }, [ST('장난', 1, 'oneEnemy')])] }],
  '다야의 다이아몬드 커터': ['eq_dayacutter', '무기',
    rule('흠집 찾기', { on: 'always' }, [MOD('dealtMod', 0.15, { target: 'self' })], { conds: [{ c: 'debuffs', n: 1 }] }),
    '흠이 있는 원석만 골라 자르는 커터. 디버프가 걸린 적에게 더 잘 든다.',
    { stats: { atk: 8 }, effect: [rule('감정 접수', { on: 'break', mine: true }, [ST('감정 의뢰', 1)])] }],
  '림의 낫': ['eq_rimscythe', '무기',
    rule('거둘 때', { on: 'play', type: '공격' }, [{ k: 'ifWounded', target: 'oneEnemy' }, EX(1.2)], { limit: perTurn(1) }),
    '균형을 지키는 낫. 다 기운 목숨은 단숨에 거둔다.',
    { stats: { atk: 8 }, effect: [rule('저울추', { on: 'kill', mine: true }, [ST('낫 자국', 1, 'allEnemies')], { limit: perTurn(1) })] }],
  '네르의 엘드르 깃발': ['eq_nerflag', '무기',
    rule('행사 깃발', { on: 'play', type: '스킬', every: 3 }, [HEAL(0.6)]),
    '교단 행사 때 거는 깃발. 흔들 때마다 모두 기운이 난다.',
    { stats: { hp: 60, def: 5 }, effect: [rule('깃발 아래 기도', { on: 'play', type: '스킬', every: 3 }, [ST('기도', 1)])] }],
  '아멜리아의 E-Pad 클래식': ['eq_ameliapad', '장신구',
    rule('찌릿한 화면', { on: 'play', type: '스킬', every: 2 }, [E1('충격', 1)]),
    '시장님 비서의 업무용 패드. 결재가 밀리면 화면에서 불꽃이 튄다.',
    { stats: { hp: 40, def: 5 }, effect: [rule('결재함 동기화', { on: 'play', who: 'other' }, [ST('결재 서류', 1)], { limit: perTurn(1) })] }],
  '블랑셰의 유리 파랑새': ['eq_blanchetbird', '장신구',
    rule('무대의 정점', { on: 'ult' }, [S('사기', 1), S('잔광', 2)]),
    '유리로 빚은 파랑새. 가장 큰 무대에서만 날개를 편다.',
    { stats: { atk: 8 }, effect: [rule('두 빛깔 커튼콜', { on: 'ult' }, [ST('붉은 장미', 1), ST('푸른 장미', 1)])] }],
  '리츠의 대지 뽀개기': ['eq_reetsmash', '무기',
    rule('뒤에 숨어', { on: 'turnStart' }, [S('피해 감소', 1)], { conds: [{ c: 'hurtLast' }] }),
    '땅을 쪼개는 큰 망치. 휘두르는 쪽 뒤가 가장 안전하다.',
    { stats: { hp: 40, atk: 6 }, effect: [rule('먼저 치셨으니까', { on: 'hurt', guarded: true }, [ST('명분이 서면', 1)], { limit: perTurn(1) })] }],
  '레비의 단도': ['eq_levidagger', '무기',
    rule('버티고 베기', { on: 'lowHp', pct: 0.3 }, [S('절대 무적', 1), MOD('dealtMod', 0.3, { target: 'self', run: true })]),
    '단도라 부르기엔 너무 큰 칼. 몰리면 손에 힘이 들어간다.',
    { stats: { atk: 8 }, effect: [rule('참았던 괴력', { on: 'lowHp', pct: 0.3 }, [SP('괴력 봉인', 'all')])] }],
  '버터의 옐로카드': ['eq_butteryellow', '장신구',
    rule('강하게 경고', { on: 'play', type: '공격', minCost: 2 }, [E1('취약', 1)], { limit: perTurn(1) }),
    '주머니에서 꺼내기만 해도 분위기가 바뀌는 노란 카드.',
    { stats: { atk: 8 }, effect: [rule('경고 누적', { on: 'hurt', guarded: true }, [ST('옐로카드', 1)], { limit: perTurn(1) })] }],
  '힐데의 간호복': ['eq_hildeuniform', '방어구',
    rule('회복 확인', { on: 'unwound' }, [S('사기', 1), S('결의', 1)], { limit: perFight(2) }),
    '빳빳하게 다린 간호복. 환자가 일어나면 모두 힘이 난다.',
    { stats: { hp: 40, def: 5 }, effect: [rule('처방 기록', { on: 'unwound' }, [ST('처방전', 2)], { limit: perFight(2) })] }],
  '리스티의 게임 컨트롤러': ['eq_ristypad', '장신구',
    rule('입력 방해', { on: 'hit' }, [E1('약화', 1)], { limit: perTurn(1) }),
    '손에 붙은 듯 익숙한 컨트롤러. 맞힐 때마다 상대 손이 꼬인다.',
    { stats: { atk: 8 }, effect: [rule('예열된 손가락', { on: 'fightStart' }, [ST('콤보', 2)])] }],
  '네티의 발굴 기록서': ['eq_nettynote', '장신구',
    rule('현장 안전 수칙', { on: 'hurt', guarded: true }, [S('피해 감소', 1)], { limit: perTurn(1) }),
    '발굴 현장의 사고 기록이 빼곡하다. 덕분에 맞는 법도 안다.',
    { stats: { hp: 60, def: 5 }, effect: [rule('흔들린 지층', { on: 'hurt', guarded: true }, [ST('유물 발굴', 1)], { limit: perTurn(1) })] }],
  // ── 희귀 · 애착 ──
  '프리클의 여왕 대리 인장': ['eq_pricklesigil', '장신구',
    rule('대리 집행', { on: 'play', type: '스킬', every: 3 }, [E1('취약', 1)]),
    '여왕 대리의 인장. 찍히는 쪽은 꼼짝 못 한다.',
    { stats: { atk: 6 }, effect: [rule('함정 결재', { on: 'play', type: '스킬' }, [ST('봉인 함정', 1, 'oneEnemy')], { limit: perTurn(1) })] }],
  '루드의 운동 교본': ['eq_rudebook', '장신구',
    rule('회복도 운동', { on: 'fightStart' }, [{ k: 'healMod', v: 0.4, run: true }]),
    '「쉬는 것도 운동이다」 가 첫 장에 적힌 교본.',
    { stats: { hp: 60, def: 5 }, effect: [rule('세트 끝 스트레칭', { on: 'stackReach', id: '한 세트 더', n: 4 }, [HEAL(0.5)])] }],
  '셀리네의 미드나잇 미라주': ['eq_selinemirage', '방어구',
    rule('한밤의 신기루', { on: 'play', type: '스킬' }, [HEAL(0.4)], { limit: perTurn(1) }),
    '한밤의 무대 의상. 넘치는 기운이 신기루처럼 몸을 감싼다.',
    { stats: { hp: 60, def: 5 }, effect: [rule('넘친 기운', { on: 'overheal' }, [SH(0.5), ST('표정', 1)], { limit: perTurn(1) })] }],
  '캬롯의 사탕수수': ['eq_carrotcane', '무기',
    rule('달콤한 기운', { on: 'turnStart' }, [G(8)]),
    '텃밭에서 갓 꺾은 사탕수수. 씹으면 기운이 돈다.',
    { stats: { hp: 40, def: 5 }, effect: [rule('먼저 일군 텃밭', { on: 'fightStart' }, [ST('텃밭', 2)])] }],
  '제이드의 비싼 만년필': ['eq_jadepen', '장신구',
    rule('사치품의 필기감', { on: 'play', type: '스킬', every: 2 }, [MOD('dealtMod', 0.15, { target: 'self' })]),
    '책값을 쪼개 산 사치품. 쓸 때마다 주문이 또렷해진다.',
    { stats: { atk: 6 }, effect: [rule('책값 메모', { on: 'turnEnd' }, [ST('소장 가치', 1)], { conds: [{ c: 'held', n: 1 }] })] }],
  '코미의 베개': ['eq_komipillow', '방어구',
    rule('기절하듯 낮잠', { on: 'lowHp', pct: 0.3 }, [S('초재생', 2)]),
    '어디서든 잠들 수 있는 베개. 쓰러지기 직전에도 푹 잔다.',
    { stats: { hp: 60, def: 5 }, effect: [rule('남긴 기운 베고 자기', { on: 'turnEnd' }, [ST('낮잠', 1)], { conds: [{ c: 'apLeft', n: 1 }] })] }],
  // ── 고급 · 애착 ──
  '로네의 건틀릿': ['eq_ronegauntlet', '방어구',
    rule('외교적 방어', { on: 'fightStart' }, [S('피해 감소', 1)]),
    '외교 행사용 장갑. 속에 강철판이 들어 있다는 건 비밀.',
    { stats: { hp: 40, def: 4 }, effect: [rule('정체 숨기기', { on: 'always' }, [MOD('takenMod', -0.1, { target: 'self' })], { conds: [{ c: 'stack', id: '첩보원 모드', n: 1 }] })] }],

  // ══ 범용 64 — 카제나식 리워크(2026-10-07, _measure/장비_중립카드_리워크_지침.md §2) ══
  // 유형: [능력] 능력치만 · [시작] 전투 시작 · [조건] 조건부 발동 · [카드] 카드 유형 강화 · [축] 사도 축(넓음 · 좁음)
  // 확률 효과는 쓰지 않는다(원작 확률 → 기대값이 같은 횟수). 전설은 규칙 둘까지(늘 도는 바탕 + 축 계기).

  // ── 전설 · 범용(13) ──
  // [조건] 원작 「일반 공격 3회마다 번개 150%」
  '날씨는 맑음 카드': ['eq_sunnycard', '장신구',
    rule('맑은 날 번개', { on: 'play', type: '공격', every: 3 }, [EX(1.5, 'randomEnemy')]),
    '「맑음」 이라 적힌 카드. 그런데 왜 번개가 치는 걸까요.'],
  // [시작 + 축 좁음: 넘친 치유] 원작 「치유량 +60%」 — 넘친 치유는 실드로
  '엘다인 램프': ['eq_eldainlamp', '장신구',
    [rule('따스한 심지', { on: 'fightStart' }, [{ k: 'healMod', v: 0.4, run: true }]),
     rule('남는 불빛', { on: 'overheal', who: 'any' }, [{ k: 'shield', ratio: 1, ofEvent: 0.5 }], { limit: perTurn(1) })],
    '엘다인의 숲을 비추던 램프. 넘치는 온기는 막이 되어 남습니다.'],
  // [조건] 원작 「HP 30% 이하 → 큰 회복」
  '생명의 보석': ['eq_lifegem', '장신구',
    rule('마지막 맥박', { on: 'lowHp', pct: 0.3 }, [S('초재생', 3)]),
    '쓰러지기 직전에 뛰기 시작하는 보석.'],
  // [조건 + 축 넓음: 실드] 원작 「받는 피해 -10%」
  '말랑말랑한 조끼': ['eq_squishyvest', '방어구',
    [rule('말랑 완충', { on: 'turnStart' }, [S('피해 감소', 1)]),
     rule('눌린 자국', { on: 'shieldBreak' }, [S('결정화', 1)], { limit: perFight(3) })],
    '무엇이든 말랑하게 받아 내는 조끼. 눌린 자리는 더 단단해집니다.'],
  // [조건 + 카드: 비용 0] 원작 「일반 공격 5회마다 숨죽임 → 눈속임」
  '암살자의 비급': ['eq_assassinbook', '장신구',
    [rule('숨죽이기', { on: 'play', type: '공격', every: 4 }, [S('회피', 1)]),
     rule('그림자 찌르기', { on: 'play', type: '공격', maxCost: 0 }, [EX(0.8)], { limit: perTurn(2) })],
    '다섯 번 베면 한 번 숨는 법이 적힌 비급. 가벼운 손일수록 그림자가 짙습니다.'],
  // [카드: 비용 2 이상] 원작 「스킬 피해 +52%, 공격 속도 -30%」
  '30KG 케틀벨': ['eq_kettlebell', '무기',
    rule('한 번에 들어 올리기', { on: 'play', minCost: 2 }, [EX(0.4), S('잔광', 1)], { limit: perTurn(1) }),
    '느리지만 한 번 휘두르면 묵직합니다. 무거운 카드일수록 힘이 실립니다.'],
  // [조건] 원작 「매 초 공격 속도 +8%」
  '용광검': ['eq_furnace', '무기',
    rule('달아오르는 날', { on: 'turnStart' }, [MOD('atkMod', 0.05, { target: 'self', run: true })]),
    '싸울수록 달아오르는 검.'],
  // [조건 둘] 원작 「HP 100% 미만이면 공속 · 피해 증가」
  '녹슨 붉은 검': ['eq_rustyred', '무기',
    [rule('붉은 오기', { on: 'hurt' }, [S('사기', 1)], { limit: perFight(1) }),
     rule('벼랑 끝', { on: 'always' }, [MOD('dealtMod', 0.15, { target: 'self' })], { conds: [{ c: 'wounded' }] })],
    '한 대 맞으면 붉게 빛납니다. 알고 보니 말라붙은 석류 주스.'],
  // [축: 표식] 원작 「사거리에 따라 피해 증가」
  '장난감 망원경': ['eq_toyscope', '장신구',
    rule('멀리서 조준', { on: 'play', type: '공격' }, [E1('표식', 1)], { limit: perTurn(1) }),
    '장난감인데 멀리 있는 적일수록 잘 보입니다.'],
  // [시작 + 조건] 원작 「최대 HP +46%, 재생, 피해 +15%」
  '거대화 물약': ['eq_growpotion', '방어구',
    [rule('쑥쑥', { on: 'fightStart' }, [S('초재생', 1)]),
     rule('큰 주먹', { on: 'always' }, [MOD('dealtMod', 0.1, { target: 'self' })])],
    '마시면 몸이 커집니다. 커진 몸은 금방 아물고 주먹도 커집니다.'],
  // [조건 + 축 좁음: 추가 공격] 원작 「처치하면 피해 · 공속 증가(3회)」
  '슈슈슈슉 글러브': ['eq_glove', '무기',
    [rule('기세', { on: 'kill', mine: true }, [S('사기', 1)], { limit: perFight(3) }),
     rule('연타', { on: 'extra', who: 'any' }, [S('잔광', 1)], { limit: perFight(2) })],
    '한 놈 쓰러뜨릴 때마다 주먹이 빨라집니다(세 번까지). 덤으로 날린 주먹도 가끔 묵직합니다.'],
  // [축 넓음: 실드] 원작 「주는 보호막 +46%」
  '볼케니카 특산 과일 주스': ['eq_volcanijuice', '장신구',
    rule('과즙 코팅', { on: 'guard', kind: 'shield' }, [S('결의', 1)], { limit: perFight(3) }),
    '화산섬 과일을 짠 주스. 몸을 감싼 막이 더 단단해집니다.'],
  // [카드: 스킬] 원작 「저학년 스킬로 충전 → 다음 공격 폭발」
  '마도 공학 배터리': ['eq_battery', '장신구',
    rule('충전 완료', { on: 'play', type: '스킬', every: 3 }, [EX(2.0, 'randomEnemy')]),
    '주문을 쓸수록 차오르다 한 번에 터집니다.'],

  // ── 희귀 · 범용(16) ──
  // [조건] 원작 「HP 15% 이하 → 5초 무적」
  '도깨비 감투': ['eq_goblinhat', '방어구',
    rule('사라지기', { on: 'lowHp', pct: 0.15 }, [S('절대 무적', 1)]),
    '쓰면 모습이 사라진다는 감투. 위급할 때만 효과가 있습니다.'],
  // [축 좁음: 소모] 원작 「일반 공격 적중 시 SP 회복」
  '기원의 성배': ['eq_grail', '장신구',
    rule('차오르는 기원', { on: 'spend', who: 'any' }, [D(1)], { limit: perFight(2) }),
    '모아 둔 힘을 쏟을 때마다 성배에 기운이 고여 다음 손이 보입니다.'],
  // [시작 · 덱 조작] 원작 「스킬 피해 +16%」 — 펼쳐 둔 장
  '옥빛 마법서': ['eq_jadebook', '장신구',
    rule('펼쳐 둔 장', { on: 'fightStart' }, [{ k: 'draw', v: 1, unique: true }]),
    '전투 전에 가장 자신 있는 주문이 적힌 장을 펼쳐 둡니다.'],
  // [카드: 스킬] 원작 「스킬 피해 +16%」
  '날카로운 지팡이': ['eq_sharpstaff', '무기',
    rule('지팡이 끝 칼날', { on: 'play', type: '스킬' }, [EX(0.45)], { limit: perTurn(1) }),
    '지팡이와 검을 하나로 합친 물건. 주문을 외우는 틈에 칼날이 먼저 나갑니다.'],
  // [조건] 원작 「스킬 피해 +18%」 — 큰 기술
  '황금 왕관': ['eq_goldcrown', '장신구',
    rule('왕의 한 수', { on: 'ult' }, [S('사기', 1)]),
    '큰 기술을 쓸 때마다 왕관이 번쩍입니다.'],
  // [조건] 원작 「치명 피해 +10%」
  '전투 지침서': ['eq_combatguide', '장신구',
    rule('급소 복습', { on: 'crit' }, [D(1)], { limit: perTurn(1) }),
    '급소 그림이 가득한 지침서.'],
  // [조건] 원작 「일반 공격 피해의 178% 회복」
  '치유의 펜던트': ['eq_healpendant', '장신구',
    rule('피어나는 빛', { on: 'play', type: '공격' }, [HEAL(0.4)], { limit: perTurn(1) }),
    '칠 때마다 상처를 조금씩 메워 주는 펜던트.'],
  // [조건] 원작 「매 초 최대 HP 2% 회복」
  '축복받은 견갑': ['eq_blessedpauldron', '방어구',
    rule('굳은 축복', { on: 'turnEnd' }, [HEAL(0.3)]),
    '어깨에 얹은 축복이 쉬지 않고 상처를 돌봅니다.'],
  // [축 넓음: 반격] 원작 「6회 맞으면 분노 → 강타 + 기절」
  '참회의 메이스': ['eq_mace', '무기',
    rule('웃으며 참회', { on: 'hurt', guarded: true }, [S('반격', 1)], { limit: perTurn(1) }),
    '맞을수록 손이 매워지는 메이스.'],
  // [시작] 원작 「전투 시작 시 같은 열에 보호막」
  '긴급 보호 벨트': ['eq_safetybelt', '방어구',
    rule('비상 실드', { on: 'fightStart' }, [SH(1.5), S('실드 유지', 1)]),
    '전투가 시작되면 철컥 하고 실드가 펼쳐집니다.'],
  // [시작 · 축 넓음: 실드] 원작 「받는 보호막 +52%」
  '제사장의 향로': ['eq_censer', '장신구',
    rule('향 피우기', { on: 'fightStart' }, [S('결의', 2)]),
    '향 연기가 감싼 동안 막이 두꺼워집니다.'],
  // [축 넓음: 실드] 원작 「(스탯만)」 → 비늘이 받아 낸 만큼 되받는다
  '비늘 갑옷': ['eq_scalearmor', '방어구',
    rule('비늘 세우기', { on: 'blocked' }, [S('반격', 1)], { limit: perFight(1) }),
    '단단한 비늘을 엮은 갑옷. 다 막아 내면 비늘이 곤두섭니다.'],
  // [카드: 비용 0 · 축 좁음] 원작 「5번째 일반 공격 치명」
  '망토와 단검': ['eq_cloakdagger', '무기',
    rule('망토 속 단검', { on: 'play', maxCost: 0, who: 'any' }, [EX(0.6)], { limit: perTurn(2) }),
    '괴도 놀이 소품. 망토를 펄럭이는 틈에 단검이 먼저 나갑니다.'],
  // [조건] 원작 「HP 50% 이하 → 큰 보호막」
  '풍선 갑옷': ['eq_balloon', '방어구',
    rule('예비 풍선', { on: 'lowHp', pct: 0.5 }, [SH(2.5)]),
    '반쯤 터지면 안쪽 풍선이 부풉니다.'],
  // [축 좁음: 약화] 원작 「기절 해제 · 받는 피해 감소」
  '고무고무 건틀릿': ['eq_rubbergauntlet', '방어구',
    rule('튕겨 내기', { on: 'turnStart' }, [CL(1), S('피해 감소', 1)], { conds: [{ c: 'status', id: '약화', n: 1 }], limit: perFight(2) }),
    '몸이 굳으면 고무처럼 튕겨 풀어 줍니다.'],
  // [축 좁음: 소멸] 원작 「30회 적중마다 폭발 · 보호막 파괴」
  '폭발 머핀': ['eq_muffin', '장신구',
    rule('펑', { on: 'exhaust', who: 'any' }, [{ k: 'strip', target: 'oneEnemy' }, EX(1.0)], { limit: perFight(1) }),
    '오래 굽다 보면 터집니다. 다 쓴 카드가 사라질 때마다 펑 — 막 같은 건 소용없습니다.'],

  // ── 고급 · 범용(19) ──
  // [조건] 원작 「주변 적에게 반사 피해」
  '광기의 가면': ['eq_madmask', '방어구',
    rule('섬뜩한 웃음', { on: 'hurt', guarded: true }, [{ k: 'reflect', ratio: 0.3 }]),
    '가까이 오는 것마다 되갚아 주는 가면.'],
  // [조건] 원작 「일반 공격 피해의 182% 회복」
  '행운의 주사위': ['eq_luckydice', '장신구',
    rule('좋은 눈', { on: 'crit' }, [HEAL(0.5)], { limit: perTurn(1) }),
    '좋은 눈이 나오면 몸도 가뿐해집니다.'],
  // [시작] 원작 「전투 시작 시 같은 열에 보호막」 — 벨트(희귀)의 작은 판
  '엘프산 요술봉': ['eq_elfwand', '무기',
    rule('반짝 실드', { on: 'fightStart' }, [SH(1.0)]),
    '흔들면 반짝이는 막이 생깁니다.'],
  // [축 넓음: 실드] 원작 「받는 보호막 +28%」
  '찬란한 티아라': ['eq_tiara', '장신구',
    rule('빛나는 막', { on: 'guard', kind: 'shield' }, [S('결의', 1)], { limit: perFight(1) }),
    '머리 위에서 빛나며 막을 한 겹 더 두릅니다.'],
  // [축 넓음: 실드] 원작 「(스탯만)」 → 깨져도 남는 다이아
  '다이아몬드 팔찌': ['eq_diabracelet', '장신구',
    rule('깨지지 않는 돌', { on: 'shieldBreak' }, [S('피해 감소', 1)], { limit: perFight(1) }),
    '단단한 다이아를 박은 팔찌. 막이 깨져도 팔찌는 남습니다.'],
  // [조건] 원작 「치명 피해를 받으면 피해 감소」
  '가시 왕관': ['eq_thorncrown', '방어구',
    rule('가시의 각오', { on: 'hurt', pct: 0.1 }, [S('피해 감소', 1)], { limit: perTurn(1) }),
    '크게 맞을수록 가시가 곤두섭니다.'],
  // [축 좁음: 고통] 원작 「상태이상 피해 +35%」
  '탐욕의 반지': ['eq_greedring', '장신구',
    rule('더 아프게', { on: 'fightStart' }, [EA('고통 각인', 1)]),
    '흠집이 하나 나면 반지가 그 흠을 더 벌립니다.'],
  // [카드: 비용 2 이상] 원작 「6회 적중마다 치명」
  '강철 대검': ['eq_steelsword', '무기',
    rule('무게 싣기', { on: 'play', type: '공격', minCost: 2 }, [T(1)], { limit: perTurn(1) }),
    '날보다 무게로 베는 대검.'],
  // [조건: 첫 턴] 원작 「전투 시작 충전 → 다음 공격 +80% · 화상」
  '불타는 가지': ['eq_burnbranch', '무기',
    rule('첫 불씨', { on: 'play', type: '공격' }, [E1('그을림', 3), S('잔광', 1)], { conds: [{ c: 'firstTurn' }], limit: perFight(1) }),
    '전투가 시작되면 불이 붙는 나뭇가지.'],
  // [조건] 원작 「HP 회복량 +21%」 — 다쳤을 때 덧나지 않게
  '가죽 갑옷': ['eq_leather', '방어구',
    rule('질긴 가죽', { on: 'turnEnd' }, [HEAL(0.4)], { conds: [{ c: 'wounded' }] }),
    '상처가 덧나지 않게 감싸 주는 가죽. 많이 다쳤을 때 더 단단히 조입니다.'],
  // [카드: 비용 1 이하] 원작 「일반 공격 피해 +8%」
  '흑요석 수리검': ['eq_obsidianstar', '무기',
    rule('곁따라 던지기', { on: 'play', type: '공격', maxCost: 1, every: 3 }, [EX(0.5, 'randomEnemy')]),
    '가볍게 칠 때마다 한 자루씩 곁따라 날아갑니다.'],
  // [축 좁음: 버리기] 원작 「(스탯만) 치명 피해」 → 안개 속에서 찌른다
  '안개 비수': ['eq_mistdagger', '무기',
    rule('안개 속 한 수', { on: 'discard', who: 'any' }, [EX(0.3)], { limit: perTurn(1) }),
    '안개처럼 흐릿한 날의 비수. 손에서 흩어진 카드 뒤에서 날이 나옵니다.'],
  // [카드: 비용 1 이하] 원작 「(스탯만) 공격 속도」 → 곧은 화살
  '낡은 화살': ['eq_oldarrow', '무기',
    rule('곧게 날기', { on: 'play', type: '공격', maxCost: 1, every: 4 }, [EX(0.6)]),
    '오래됐지만 아직 곧게 납니다.'],
  // [조건] 원작 「피해 +6%」
  '여우 단검': ['eq_foxdagger', '무기',
    rule('여우 손놀림', { on: 'always' }, [MOD('dealtMod', 0.06, { target: 'self' })]),
    '여우 장식이 귀여운 단검.'],
  // [조건] 원작 「웨이브 종료 시 회복」
  '숭배용 액자': ['eq_worshipframe', '장신구',
    rule('우러러보기', { on: 'kill' }, [HEAL(0.4)], { limit: perTurn(1) }),
    '멋진 장면을 넣어 둘 액자. 한 놈 쓰러질 때마다 기운이 납니다.'],
  // [조건] 원작 「26.6% 확률 쓰라림」 → 3장마다
  '앗따검': ['eq_ouchsword', '무기',
    rule('쓰라림', { on: 'play', type: '공격', every: 3 }, [E1('균열', 2)]),
    '스치기만 해도 앗따 소리가 납니다.'],
  // [시작] 원작 「(스탯만)」 → 보기만 해도 배고프다
  '돈까스 모양 머리핀': ['eq_cutletpin', '장신구',
    rule('배꼽시계', { on: 'fightStart' }, [S('다음 턴 드로우', 1)]),
    '보기만 해도 배고파지는 머리핀. 다음 끼니 생각에 손이 빨라집니다.'],
  // [조건] 원작 「6.5% 확률 감전」 → 4장마다
  '앗땃따건': ['eq_taser', '무기',
    rule('찌릿', { on: 'play', type: '공격', every: 4 }, [E1('충격', 1)]),
    '가끔 찌릿 하고 불꽃이 튑니다.'],
  // [조건] 원작 「27% 확률 화상」 → 3장마다
  '활활 불타활': ['eq_firebow', '무기',
    rule('불화살', { on: 'play', type: '공격', every: 3 }, [E1('그을림', 2)]),
    '옆에만 둬도 후끈한 불화살 활.'],

  // ── 일반 · 범용(16) — 능력치만 12 + 작은 효과 4 ──
  '덧댄 지팡이': ['eq_patchstaff', '무기', null, '닳도록 쓴 꼬질꼬질한 지팡이.'],
  '급조한 목검': ['eq_woodsword', '무기', null, '훈련용으로 대충 깎은 목검.'],
  '알루미늄 골무': ['eq_thimble', '장신구', null, '바늘에 찔려도 끄떡없습니다.'],
  '녹슨 송곳': ['eq_awl', '무기', null, '녹슬었지만 끝은 아직 뾰족합니다.'],
  '잎사귀 로브': ['eq_leafrobe', '방어구', null, '나뭇잎을 꿰매 만든 로브.'],
  // [카드: 스킬] 원작 「스킬 뒤 다음 일반 공격 +30%」
  '보석 반지': ['eq_gemring', '장신구',
    rule('주문 뒤 한 방', { on: 'play', type: '스킬' }, [EX(0.3)], { limit: perTurn(1) }),
    '주문을 쓰고 나면 보석이 한 번 반짝입니다.'],
  // [축 넓음: 실드] 일반의 작은 효과
  '골판지 갑옷': ['eq_cardboard', '방어구',
    rule('한 겹 더', { on: 'guard', kind: 'shield' }, [SH(0.3)], { limit: perTurn(1) }),
    '택배 상자를 겹겹이 댄 갑옷. 막을 치면 상자가 한 겹 더 받쳐 줍니다.'],
  '보자기 로브': ['eq_wraprobe', '방어구', null, '보자기를 두른 로브. 생각보다 따뜻합니다.'],
  '수건 머리띠': ['eq_towel', '방어구', null, '이마에 질끈 묶으면 힘이 납니다.'],
  '고목나무 비수': ['eq_treedagger', '무기', null, '낡은 나무를 깎은 비수.'],
  '슬라임 쿠션': ['eq_slimecushion', '방어구', null, '말랑하게 받아 내는 쿠션.'],
  '오븐 장갑': ['eq_ovenmitt', '방어구', null, '뜨거운 것도 잡을 수 있는 장갑.'],
  '중량 조끼': ['eq_weightvest', '방어구', null, '무겁지만 든든한 조끼.'],
  // [시작] 원작 「치유량 +13%」
  '치유의 호롱불': ['eq_healinglamp', '장신구',
    rule('작은 불빛', { on: 'fightStart' }, [{ k: 'healMod', v: 0.15, run: true }]),
    '곁에 두면 회복이 조금 더 잘 듭니다.'],
  // [조건: 첫 턴] 원작 「전투 시작 충전 → 다음 공격 +38%」
  '묘수': ['eq_catpaw', '장신구',
    rule('고양이 기운', { on: 'play', type: '공격' }, [EX(0.5)], { conds: [{ c: 'firstTurn' }], limit: perFight(1) }),
    '고양이 발 모양 부적. 첫 공격에 기운을 싣습니다.'],
  '요정 로브': ['eq_fairyrobe', '방어구', null, '요정들이 입는 가벼운 로브.'],
};

// 등급마다 능력치 값 평균(공격 1 = HP 6 = 방어 1.5 = 치명 2) — 옛 장비 69 의 평균(일반 12 · 고급 16 · 희귀 22 · 전설 28)에 맞춘다
const GRADE_VALUE = { 일반: 12, 고급: 16, 희귀: 22, 전설: 28 };
// 2026-10-07 리워크 시범부터 배율을 고정 — 장비 하나에 효과를 달거나 빼도(스탯만 ×1.2 가 바뀌어) 같은 등급 다른 장비의 능력치가 흔들리지 않게.
// 값은 리워크 전(장비 64 + 애착 22) 계산값. 다시 계산하려면 null 로.
const FREEZE_SCALE = { 전설: 0.9791840133222315, 희귀: 1.2659110723626852, 고급: 1.2379110251450676, 일반: 1.265934065934066 };

function build() {
  const { artifacts } = L.readRef();
  const raw = {};
  for (const a of artifacts) { const d = DEF[a.name]; if (!d) continue; (raw[a.grade] = raw[a.grade] || []).push(L.statValue(L.convertStats(a.stat, !d[2]))); }
  const calc = Object.fromEntries(Object.entries(raw).map(([g, xs]) => [g, GRADE_VALUE[g] / (xs.reduce((p, q) => p + q, 0) / xs.length)]));
  const scale = FREEZE_SCALE || calc;
  const world = [], affinity = {}, rows = [];
  const seen = new Set();
  for (const a of artifacts) {
    const d = DEF[a.name];
    if (!d) throw new Error('정의 없음: ' + a.name);
    const [id, slot, eff, blurb, aff] = d;
    if (seen.has(id)) throw new Error('id 겹침 ' + id); seen.add(id);
    const e = { id, name: a.name, grade: a.grade, slot, stats: L.convertStats(a.stat, !eff, scale[a.grade]) };
    if (eff) e.effect = Array.isArray(eff) ? eff : [eff];
    if (a.owner) {
      if (!aff) throw new Error('애착 정의 없음 ' + a.name);
      e.affinity = a.owner; e.affinityStats = aff.stats; e.affinityEffect = aff.effect;
      (affinity[a.owner] = affinity[a.owner] || []).push(e);
    } else world.push(e);
    e.blurb = blurb;
    rows.push({ id, name: a.name, grade: a.grade, slot, owner: a.owner, icon: a.icon, stats: e.stats, effect: eff ? [].concat(eff).map(r => r.name).join(' · ') : '(스탯만)' });
  }
  for (const k of Object.keys(DEF)) if (!artifacts.some(a => a.name === k)) throw new Error('원작 표에 없는 이름: ' + k);
  return { world, affinity, rows, scale };
}
module.exports = { build };
