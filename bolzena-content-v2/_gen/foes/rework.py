# 카제나식 적 리워크 — 37종 전부(2026-10-07 사용자 결정 · _measure/적_리워크_지침.md).
#   틀: 컨셉 1 + 의도 3~5 + 패시브(카운터 포함) 0~1 + (엘리트) 예고 큰 수 1 · 방해 한 가지 + 격파 반응.
#   역할 표지(묶음 규칙이 본다 — build.py feats): 관통 P · 큰 수 C · 방해 D · 지원(회복 · 적 전체 버프 · 적 전체 실드) H.
#     일반은 그 가운데 하나까지(명사수 · 휠레용은 「모아 쏘는 저격」 이라 P+C), 엘리트는 C + D(+ 컨셉에 따라 P · H).
#   값은 원값(build.py 의 BAL 이 곱한다 — 피해 × 1.155 · HP × 1.1). HP 는 옛 값 그대로(아래 hp 가 있는 것만 바꿈).
#   방해 카드는 엘리트 한 가지 · 덱에 4장까지(addCard 의 max — 엔진 Battle.ShuffleOk).
import copy

def A(v, say, **k): return dict(t='attack', v=v, say=say, **k)
def B(v, say, **k): return dict(t='back', v=v, say=say, **k)
def M(v, n, say, **k): return dict(t='multi', v=v, n=n, say=say, **k)
def AA(v, say, **k): return dict(t='attackAll', v=v, say=say, **k)
def BL(v, say, **k): return dict(t='block', v=v, say=say, **k)
def HL(v, say): return dict(t='heal', v=v, say=say)
def BF(st, v, say, all=False): return dict(t='buff', id=st, v=v, say=say, **({'all': True} if all else {}))
def DB(st, v, say): return dict(t='debuff', id=st, v=v, say=say)
def AC(card, n, say, mx=4): return dict(t='addCard', id=card, n=n, to='draw', say=say, max=mx)
def CD(st, n, say): return dict(t='cardDebuff', id=st, n=n, to='draw', say=say)
def J(say): return dict(t='jam', v=1, say=say)
def SZ(say, n=1, v=20): return dict(t='seize', n=n, v=v, say=say)
def CNT(cid, v, say): return dict(t='count', id=cid, v=v, say=say)
def SUM(fid, say): return dict(t='summon', id=fid, n=1, max=1, say=say)
def CH(warn, nxt):
    n = dict(nxt); n['brk'] = True
    return {'t': 'charge', 'say': warn, 'next': n, 'brk': True}
def P(name, on, do, **k): return dict(name=name, on=on, do=do, **k)
def BRK(name, say, v=1): return P(name, 'broken', BF('취약', v, say))
def BRKC(name, say, cid, v): return P(name, 'broken', CNT(cid, -v, say))

# id → 바꿀 칸(intents · counters · passives · open · phase · rare · hp · tough · blurb). counters · passives · open · rare 는 적으면 통째로 바꾸고, None 이면 지운다.
D = {}

# ═══════════════ 요정 · 에르피엔 ═══════════════
D['fairymobcloserange'] = dict(   # 딜러
    intents=[A(90, '배고파서 물기'), A(105, '허겁지겁 달려들기'), M(40, 2, '허둥지둥 할퀴기')],
    passives=[BRKC('당 떨어짐', '당이 뚝 떨어짐', '허기', 3)],
    blurb='당이 떨어져 눈이 돌아간 요정 — 「허기」 하나당 피해가 오르고 맞을 때마다 하나씩 빠집니다(턴 시작에 셋). 격파하면 허기가 다 빠집니다. 잘게 여러 번 쳐서 허기를 뺀 뒤 크게.')
D['fairymobcloserange_elite'] = dict(   # C · D(어지럼) · H
    intents=[A(120, '크림 범벅 주먹'), M(45, 3, '잼 난사'), AC('st_dizzy', 2, '어지럼 옮기기'), HL(100, '토핑 폭식'),
             CH('토핑 몽땅 끌어안기', M(70, 4, '토핑 폭격'))],
    passives=[BRKC('토핑 쏟음', '토핑을 쏟음', '허기', 4)],
    blurb='엘리트 · 빵은 버리고 토핑만 먹는 요정 — 「허기」 넷(하나당 피해 +15%, 맞으면 하나씩 빠짐). 「어지럼」 을 뽑을 더미에 옮기고(덱에 4장까지) 토핑을 퍼먹어 회복합니다. 격파하면 토핑을 쏟아 허기가 다 빠집니다. 희귀종: 턴 시작에 손 2장 비용 +1.')
D['fairymoblongrange'] = dict(   # D(착란)
    intents=[A(70, '채소 던지기'), A(60, '콕 찌르기'), DB('약화', 1, '혀 내밀기')],
    passives=[P('깔깔 웃음', 'card', dict(t='handCost', v=-1, n=1, say='깔깔 — 손 1장 비용 -1'), type='공격'),
              P('뾰로통', 'card', dict(t='handCost', v=1, n=1, say='뾰로통 — 손 1장 비용 +1'), type='!공격'),
              BRK('울상', '울상이 됨')],
    blurb='새 채소 맛에 들뜬 요정 — 공격 카드를 보면 신이 나 손 1장 비용 -1, 다른 카드를 보면 심술로 1장 비용 +1(착란). 공격으로 몰면 덕을 봅니다. 격파하면 울상이 되어 취약 1.')
D['magicfork'] = dict(   # 딜러
    intents=[A(90, '밭을 갈듯 내리찍기'), M(40, 2, '녹슨 날로 두 번 긁기'), BL(55, '자루 곧추세우기')],
    passives=[BRKC('자루 빠짐', '자루가 빠짐', '심통', 9)],
    blurb='일할 밭을 잃고 방황하는 요정의 마법 괭이 — 공격이 아닌 카드를 낼 때마다 「심통」 이 붙고 넷이면 바로 내리찍습니다. 격파하면 자루가 빠져 심통이 다 풀립니다.')
D['magicfork_elite'] = dict(   # C · D(침체)
    intents=[A(110, '폭주 내려찍기'), M(45, 3, '쉬지 않고 찍기'), CD('침체', 2, '흙 튀기기'), A(130, '땅 뒤엎기'),
             CH('자루 높이 치켜들기', A(290, '밭 갈아엎기'))],
    passives=[BRKC('자루 꺾임', '자루가 꺾임', '폭주', 9), BRK('날 빠짐', '날이 빠짐', 2)],
    blurb='엘리트 · 근처의 무엇이든 갈아 버리는 괭이 — 맞을 때마다 「폭주」 가 쌓여 다섯이면 바로 세 번 찍습니다. 흙을 튀겨 뽑을 더미 2장을 침체시킵니다(비용 +1). 격파하면 폭주가 다 풀리고 취약 2. 희귀종: 턴 시작에 손 2장 비용 +1.')
D['ginseng'] = dict(   # H
    intents=[HL(60, '뿌리즙 나누기'), A(60, '쓴 즙 뿌리기'), DB('약화', 1, '비명 지르기')],
    passives=[P('땅속으로', 'death', dict(t='feign', say='땅속으로 쏙 숨기')), BRK('뿌리 뽑힘', '뿌리가 뽑힘')],
    blurb='세계수 양분을 먹고 걸어 다니는 산삼 — 동료를 회복시키고 비명으로 약화를 겁니다. 쓰러지면 땅속으로 숨은 척하다가(가사) 다른 적이 회복을 쓰면 일어섭니다 — 치유사부터.')
D['ginseng_elite'] = dict(   # H · C · D(독)
    intents=[HL(90, '양분 뿜기'), A(100, '쓴 즙 뿌리기'), CD('독', 2, '쓴 뿌리 심기'), DB('고통', 2, '번지는 쓴맛'),
             CH('양분 끌어올리기', AA(110, '쓴 즙 폭발', id='고통', n=2))],
    passives=[P('넘치는 양분', 'turnStart', HL(35, '넘치는 양분')), BRK('양분 새어 나감', '양분이 새어 나감', 2)],
    blurb='엘리트 · 너무 오래 자라 양분이 넘치는 산삼 — 턴마다 회복하고, 쓴 뿌리를 심어 뽑을 더미 2장에 독을 겁니다. 격파하면 양분이 새어 나가 취약 2. 희귀종: 턴 시작에 손 2장에 독.')
D['mogmaekim'] = dict(   # 딜러(물량)
    intents=[A(90, '통통 튀기'), BL(55, '바람 넣기'), A(105, '몸통 박치기')],
    passives=[P('펑!', 'death', DB('고통', 2, '펑! 터짐')), BRK('바람 빠짐', '바람이 빠짐')],
    blurb='잘못 구워진 빵 — 쓰러지면 펑 터져 파티에 고통 2 를 남깁니다. 격파하면 바람이 빠져 취약 1. 고통에 대비하고 처치하는 편이 좋습니다.')
D['buseuleogi'] = dict(   # 딜러(물량)
    intents=[A(70, '부스러기 세례'), A(60, '바스락 할퀴기'), M(30, 2, '부스럭부스럭')],
    blurb='잘못 구워진 컵케이크 — 쓰러지면 「설탕 범벅」 을 뽑을 더미에 남깁니다. 동료가 쓰러지면 성납니다. 떼로 나오니 한꺼번에 치우는 편이 좋습니다.')
D['goldring_elite'] = dict(   # C · D(빼앗기)
    intents=[BL(95, '자물쇠 걸기'), M(45, 3, '동전 세례'), SZ('금고에 꿀꺽'), A(140, '금괴 내려찍기'),
             CH('금고 문 활짝 열기', M(65, 4, '금화 폭포'))],
    blurb='엘리트 · 요정 왕국 금고를 노린 슬라임 — HP 가 많고, 공격 카드 두 장째마다 뚜껑을 닫아 실드를 얻습니다(턴당 2). 카드 1장을 금고에 삼키고, 격파 · 처치하거나 HP 20% 만큼 치면 돌려줍니다. 격파하면 열려 취약 2. 희귀종: 전투 시작 결정화 3.')
D['marshmallowtanker'] = dict(   # 벽 · C
    intents=[BL(80, '탱탱하게 부풀기', tough=1), A(90, '말랑 박치기'), CH('크게 부풀기', A(165, '말랑 깔아뭉개기'))],
    blurb='탱탱한 마시멜로 골렘 — 제 실드가 깨지면 말랑 조각을 흩어 적 전체에 실드를 줍니다. 탱탱하게 부풀 때 제 강인도를 1 되찾습니다. 격파하면 속이 터져 취약 2.')
D['marshmallowdealer'] = dict(   # 딜러
    intents=[M(35, 3, '분풀이 연타'), A(100, '부글부글 박치기'), M(45, 2, '꼬치 난타')],
    passives=[BRKC('김 빠짐', '김이 빠짐', '부글부글', 5)],
    blurb='뜯어먹힐까 늘 화가 나 있는 마시멜로 — 맞을 때마다 「부글부글」 이 올라 주는 피해가 커집니다(최대 5). 격파하면 김이 빠져 다 풀립니다. 한 방에 크게.')
D['marshmallowsupporter'] = dict(   # H
    intents=[HL(70, '쫀득 붕대'), BF('피해 감소', 1, '설탕 코팅', all=True), A(55, '톡 치기')],
    passives=[BRK('코팅 벗겨짐', '코팅이 벗겨짐')],
    blurb='쫀득한 응원 마시멜로 — 동료를 회복시키고 적 전체에 피해 감소를 두릅니다. 격파하면 코팅이 벗겨져 취약 1. 먼저 끊는 편이 좋습니다.')

# ═══════════════ 수인 · 수인 부락 ═══════════════
D['furrywarriorcloserange'] = dict(   # 딜러
    intents=[A(90, '주먹 휘두르기'), BL(55, '자세 잡기'), A(70, '발차기')],
    passives=[BRKC('기세 꺾임', '기세가 꺾임', '투지', 3)],
    blurb='훈련에 매진하는 어린 수인 — 맞을 때마다 「투지」 가 붙어 주는 피해가 오릅니다(최대 3). 격파하면 기세가 꺾여 투지가 다 빠집니다.')
D['furrywarriorcloserange_elite'] = dict(   # C · D(AP)
    intents=[A(130, '정권 지르기'), M(45, 3, '수련 연격'), BL(95, '철벽 자세'), J('기합 — 숨 막히는 압박'),
             CH('정신 집중', A(300, '천 번째 지르기'))],
    passives=[P('간파', 'card', M(40, 3, '간파 — 연격'), same=True), BRK('자세 무너짐', '자세가 무너짐', 2)],
    blurb='엘리트 · 졸업을 미룬 만년 수련생 — 같은 사도의 카드를 잇달아 내면 바로 연격. 기합으로 다음 턴 AP 를 1 깎습니다. 격파하면 자세가 무너져 취약 2. 사도를 번갈아 내는 편이 좋습니다. 희귀종: 받는 강인도 피해 -20%.')
D['furrywarriorlongrange'] = dict(   # P — 치명타 주의: 겨냥이 쏘면 비워짐
    intents=[B(90, '관통 화살'), A(60, '활대'), BL(45, '엄폐')],
    counters=[{'name': '겨냥', 'desc': '숨을 죽이고 급소를 겨눔', 'onTurnStart': 1, 'dealt': 0.2, 'max': 3, 'clearOnAttack': True}],
    passives=[BRKC('시위 끊김', '시위가 끊김', '겨냥', 3)],
    blurb='활을 고른 수인 훈련생 — 실드를 건너 뒷줄을 꿰뚫습니다. 턴마다 「겨냥」 이 올라(하나당 피해 +20%, 최대 3) 쏘면 비워집니다. 격파하면 시위가 끊겨 다 빠집니다.')
D['gluttonbear'] = dict(   # 벽 · C
    intents=[A(100, '앞발 후려치기'), BL(70, '음식 끌어안기'), M(45, 2, '허겁지겁'), CH('크게 숨 들이쉬기', A(175, '곰 몸통 박치기'))],
    passives=[BRKC('주저앉음', '배고파 주저앉음', '허기', 2)],
    blurb='듬직한 먹보 곰 수인 — 「허기」 하나당 피해가 오르고 맞으면 빠집니다(턴 시작에 둘). 격파하면 주저앉아 허기가 다 빠집니다.')
D['gluttonbear_elite'] = dict(   # C · D(울부짖음)
    intents=[A(100, '두목의 앞발'), M(40, 3, '할퀴기'), BL(90, '두목의 배짱'), AC('st_howl', 2, '부하 부르는 울음'),
             CH('두목의 포효', AA(110, '두목의 내려찍기'))],
    counters=None,
    passives=[P('두목의 호령', 'allyDown', BF('사기', 1, '두목의 호령', all=True)), BRK('체면 구김', '두목 체면 구김', 2)],
    blurb='엘리트 · 다른 머곰들을 모아 세력을 꾸린 두목 — 동료가 쓰러질 때마다 적 전체 사기. 부하를 부르는 울음(뽑으면 손 1장을 떨굼)을 뽑을 더미에 넣습니다(덱에 4장까지). 격파하면 체면이 구겨져 취약 2. 두목부터. 희귀종: 전투 시작 결정화 3.')
D['foodscavenger'] = dict(   # D(사료)
    intents=[AC('st_kibble', 1, '사료 뿌리기', mx=2), A(90, '앞발 할퀴기'), A(70, '물어뜯기')],
    passives=[BRK('봉지 터짐', '사료 봉지가 터짐')],
    blurb='사료에 중독된 라쿤 수인 — 턴 끝에 손에 있으면 약화를 거는 「사료 한 줌」 을 뽑을 더미에 뿌립니다(덱에 2장까지). 격파하면 취약 1.')
D['foodscavenger_elite'] = dict(   # C · D(안절부절)
    intents=[A(110, '미식 평가'), AC('st_jitters', 2, '까다로운 눈초리'), M(45, 2, '포크질'), BL(85, '냅킨 두르기'),
             CH('식탁 차리기', M(60, 4, '풀코스 포크질'))],
    passives=[P('편식', 'debuffed', dict(t='selfHeal', v=50, say='싫은 건 뱉기')), BRK('식탁 엎어짐', '식탁이 엎어짐', 2)],
    blurb='엘리트 · 비싼 사료만 골라 먹는 미식가 — 「안절부절」 을 뽑을 더미에 넣고(덱에 4장까지) 디버프를 받으면 뱉어 회복합니다. 격파하면 식탁이 엎어져 취약 2. 희귀종: 덱에 든 안절부절 수만큼 타격 +1.')
D['furring'] = dict(   # H
    intents=[HL(75, '한턱 쏘기'), BF('사기', 1, '오늘은 내가 쏜다!', all=True), A(60, '지갑으로 치기')],
    passives=[BRK('빈 지갑', '지갑이 텅 빔')],
    blurb='갑자기 재산이 불어난 수인 — 동료를 회복시키고 적 전체 사기를 올립니다. 격파하면 지갑이 비어 취약 1. 먼저 끊는 편이 좋습니다.')
D['furring_elite'] = dict(   # C · D(빼앗기)
    intents=[A(95, '큰손의 주먹'), M(40, 3, '돈다발 난타'), SZ('빚 대신 압류'), BL(85, '금고 끌어안기'),
             CH('돈다발 쌓기', AA(110, '돈다발 폭탄'))],
    phase={'at': 0.5, 'say': '거대화', 'intents': [BL(190, '거대화'), A(140, '거대 큰손'), M(45, 3, '분풀이'), SZ('빚 대신 압류'),
                                                   CH('돈다발 쌓기', AA(110, '돈다발 폭탄'))]},
    passives=[BRKC('주저앉음', '털썩 주저앉음', '재기 의지', 3)],
    blurb='엘리트 · 잇따른 투자 실패로 밑바닥에 떨어진 큰손 — 턴마다 「재기 의지」 가 올라 피해가 커지고(최대 3), 절반에서 거대화합니다. 빚 대신 카드 1장을 압류하고, 격파 · 처치하거나 HP 20% 만큼 치면 돌려줍니다. 격파하면 주저앉아 재기 의지가 다 빠집니다. 희귀종: 턴 시작에 손 2장 비용 +1.')
D['curseddoll'] = dict(   # D(소음)
    intents=[AC('st_chatter', 2, '소름 끼치는 소음', mx=4), A(75, '바늘 꽂기'), DB('약화', 1, '버려진 원한')],
    passives=[P('다시 꿰매기', 'lowHp', HL(90, '다시 꿰매기'), at=0.4), BRK('실밥 터짐', '실밥이 터짐')],
    blurb='마녀의 뒤틀린 마법으로 움직이는 버려진 인형 — 「왁자지껄」 소음을 둘씩 뽑을 더미에 넣고(덱에 4장까지) 약화를 겁니다. HP 가 40% 아래면 한 번 스스로 꿰맵니다. 격파하면 실밥이 터져 취약 1.')
D['nependers'] = dict(   # 벽 · C
    intents=[CNT('단단함', 1, '외피 굳히기'), A(90, '덥석'), CH('입 크게 벌리기', A(185, '꿀꺽'))],
    passives=[BRKC('외피 갈라짐', '외피가 갈라짐', '단단함', 5)],
    blurb='힘없는 풀인 척 사냥하는 식물 — 「단단함」 이 쌓여 받는 피해가 줄고, 입을 크게 벌린 다음 턴 통째로 삼킵니다. 입을 벌린 한 입은 격파로 끊기고, 격파하면 외피가 갈라져 단단함이 다 빠집니다.')

# ═══════════════ 유령 · 유령 늪 ═══════════════
D['blanketghost'] = dict(   # P
    intents=[B(70, '이불 속에서 찌르기'), DB('미끄러움', 1, '이불 잡아당기기'), A(60, '이불 휘감기')],
    passives=[BRK('이불 벗겨짐', '이불이 벗겨짐')],
    blurb='모습을 갖추지 못한 어린 유령 — 이불 속에서 실드를 건너 찌르고, 「미끄러움」 을 겁니다. 격파하면 이불이 벗겨져 취약 1.')
D['blanketghost_elite'] = dict(   # P · C · D(안절부절)
    intents=[B(90, '악몽 찌르기'), AC('st_jitters', 2, '악몽 심기'), M(35, 2, '가위눌림'), DB('약화', 1, '식은땀'),
             CH('악몽 불러오기', AA(110, '가위눌림 악몽', id='약화', n=1))],
    passives=[P('깨지 않는 악몽', 'death', DB('정신 붕괴', 1, '깨지 않는 악몽')), BRK('잠 깸', '번쩍 잠이 깸', 2)],
    blurb='엘리트 · 오래 살아 분위기 파괴에 도가 튼 이불령 — 「안절부절」 을 뽑을 더미에 심습니다(덱에 4장까지). 격파하면 잠이 깨어 취약 2, 쓰러질 때 정신 붕괴 1. 희귀종: 덱의 안절부절 수만큼 타격 +1 — 짧게 끝내는 편이 좋습니다.')
D['shadyfollowercloserange'] = dict(   # 딜러
    intents=[M(30, 3, '응원봉 연타', id='균열', per=1), A(90, '찌르기'), BL(45, '응원봉 방패')],
    passives=[BRK('응원봉 부러짐', '응원봉이 부러짐')],
    blurb='셰이디 팬클럽의 극성 유령 — 응원봉 연타마다 균열을 새깁니다. 격파하면 응원봉이 부러져 취약 1.')
D['shadyfollowercloserange_elite'] = dict(   # C · D(편지)
    intents=[M(40, 3, '친위대 찌르기', id='충격', per=1), A(130, '친위대장 일격'), AC('st_fanletter', 2, '극성팬 편지'), BL(85, '친위대 방패'),
             CH('친위대 집결', M(55, 4, '친위대 총공격', id='충격'))],
    passives=[P('친위대 결집', 'allyBroken', BF('사기', 1, '친위대 결집')), BRK('대장 체면', '대장 체면 구김', 2)],
    blurb='엘리트 · 셰이디를 따라 하며 제 팬클럽을 꾸리는 극성팬 — 칠 때마다 충격을 남기고 「극성팬 편지」 를 뽑을 더미에 넣습니다(덱에 4장까지). 동료가 격파되면 사기. 격파하면 체면이 구겨져 취약 2. 희귀종: 행동할 때 파티에 취약 2.')
D['shadyfollowerlongrange'] = dict(   # P
    intents=[B(90, '순간이동 찌르기', id='균열', n=2), A(60, '응원봉'), B(70, '뒤에서 찌르기')],
    passives=[BRK('아공간 닫힘', '아공간이 닫힘')],
    blurb='아공간을 넘나드는 극성팬 — 뒷줄로 순간이동해 실드를 건너 찌르고 균열을 새깁니다. 카드 세 장이면 움직이니 서두르는 편이 좋습니다. 격파하면 취약 1.')
D['pumpkin'] = dict(   # H
    intents=[BF('결의', 1, '단단한 호박 껍질', all=True), BL(70, '두꺼운 껍질'), A(90, '데굴데굴')],
    blurb='유령 늪에서 유독 잘 자라는 호박 — 적 전체 결의를 올리고, 턴이 끝날 때 제 실드가 남아 있으면 사기. 실드를 깨고 넘기는 편이 좋습니다.')
D['pumpkin_elite'] = dict(   # C · D(침체)
    intents=[A(130, '서러운 박치기'), BL(95, '웅크리기'), M(45, 3, '호박씨 난사'), CD('침체', 2, '눈물 범벅'),
             CH('서러움 꾹꾹 담기', AA(130, '서러움 폭발'))],
    passives=[P('늪에 묻힘', 'death', dict(t='revive', v=40, n=2, say='늪 아래에서 다시 자람')), BRK('서러움 터짐', '서러움이 터짐', 2)],
    blurb='엘리트 · 서늘한 음지에서 자라 유령들도 안 괴롭히는 호박 — 눈물로 뽑을 더미 2장을 침체시킵니다(비용 +1). 격파하면 서러움이 터져 취약 2. 쓰러지면 재 속에서 2턴 뒤 40% 로 돌아옵니다. 희귀종: 전투 시작 결정화 3.')
D['hatsnail'] = dict(   # 벽 · D(쪽지)
    intents=[BL(80, '모자 속으로 숨기'), A(80, '끈적한 몸통박치기'), AC('st_note', 1, '모자 속 쪽지 흘리기', mx=2)],
    blurb='마녀 모자를 집 삼은 달팽이 — 「모자 속 쪽지」 를 뽑을 더미에 흘리고(덱에 2장까지), 실드가 깨지면 모자 속으로 숨어 이번 턴 받는 피해가 줄어듭니다. 실드는 턴 끝에 깨는 편이 좋습니다.')
D['hatsnail_elite'] = dict(   # C · D(쪽지)
    intents=[AC('st_note', 2, '주문 쪽지 흘리기'), A(120, '모자챙 휘두르기'), M(40, 3, '점액 방울'), BL(80, '모자 눌러쓰기'),
             CH('의식 주문 외우기', AA(125, '마녀의 대주문'))],
    passives=[P('마녀 모자', 'debuffed', BL(60, '마녀 모자 눌러쓰기')), BRK('의식 깨짐', '의식이 깨짐', 2)],
    blurb='엘리트 · 마녀의 의식이 옮겨진 듯 구는 햇팽이 — 「모자 속 쪽지」 를 둘씩 흘립니다(덱에 4장까지). 디버프를 받으면 모자를 눌러써 실드. 격파하면 의식이 깨져 취약 2. 디버프보다 피해를. 희귀종: 받는 강인도 피해 -20%.')

# ═══════════════ 엘프 · 모나티엄 ═══════════════
D['elfsoldiercloserange'] = dict(   # H(대열)
    intents=[BF('결의', 1, '방패 대열', all=True), A(90, '돌격'), BL(60, '방패 세우기')],
    passives=[P('대열 메우기', 'allyBroken', BL(70, '빈자리 메우기')), BRK('대열 붕괴', '대열이 무너짐')],
    blurb='모나티엄의 징병 군인 — 적 전체 결의를 올리고, 동료가 격파되면 그 빈자리를 메우며 방패를 세웁니다. 격파하면 대열이 무너져 취약 1.')
D['elfsoldiercloserange_elite'] = dict(   # C · D(자재) · H(결의)
    intents=[A(120, '현장 지휘봉'), BL(80, '바리케이드 증설', tough=1), M(50, 3, '작업 지시'), AC('st_rubble', 1, '공사 자재 쌓기', mx=2),
             CH('철거 장비 돌리기', A(290, '철거 망치'))],
    passives=[P('공정 검사', 'turnEnd', dict(BF('결의', 1, '바리케이드를 두껍게', all=True), **{'if': {'partyBlock': True}})), BRK('공정 중단', '공정이 멈춤', 2)],
    blurb='엘리트 · 「노동반」 공병의 현장 반장 — 턴이 끝날 때 파티에 실드가 남아 있으면 바리케이드가 두꺼워집니다(적 전체 결의). 바리케이드를 늘릴 때 제 강인도를 1 되찾고, 공사 자재 「돌무더기」 를 뽑을 더미에 쌓습니다(덱에 2장까지). 격파하면 공정이 멈춰 취약 2. 희귀종: 행동할 때 파티에 약화 2.')
D['elfsoldierlongrange'] = dict(   # P · C(모아 쏘는 저격)
    intents=[B(80, '의장 사격'), CH('숨 고르기', B(190, '한 치 어긋남 없는 한 발')), BL(45, '엄폐')],
    passives=[BRK('조준 흐트러짐', '조준이 흐트러짐')],
    blurb='원거리 병과로 뽑힌 엘프 군인 — 한 치 어긋남 없이 힘을 모아 관통 한 발. 모으는 동안 격파하면 끊기고 취약 1.')
D['elfsoldierlongrange_elite'] = dict(   # P · C · D(봉쇄)
    intents=[B(130, '조준 사격', rush=0), CD('봉쇄', 2, '조준 고정'), CH('숨 고르기', B(220, '경계조장의 한 발')), M(40, 3, '견제 사격')],
    passives=[P('경계 태세', 'allyBroken', BF('사기', 1, '경계 태세')), BRK('조준경 깨짐', '조준경이 깨짐', 2)],
    blurb='엘리트 · 작업하는 노동반 곁을 지키는 경계조장 — 뽑을 더미 2장을 「봉쇄」 로 묶고, 큰 한 발은 카드로 당겨지지 않습니다. 동료가 격파되면 경계 태세(사기). 격파하면 조준경이 깨져 취약 2. 희귀종: 행동할 때 파티에 취약 2.')
D['drones'] = dict(   # 딜러(rush 3)
    intents=[A(70, '레이저 점사', id='충격', n=1), A(60, '부딪히기'), DB('충격', 2, '방전')],
    passives=[P('비상 경보', 'lowHp', DB('충격', 1, '비상 경보'), at=0.5), BRK('추락', '감전되어 추락')],
    blurb='엘프들의 비행 감시 드론 — 방전으로 충격을 겁니다. 카드 세 장이면 움직이니 서두르는 편이 좋습니다. 격파하면 감전되어 추락해 취약 1.')
D['droneg'] = dict(   # 벽 · D(신호)
    intents=[BL(70, '방어 프로토콜', tough=1), A(90, '돌진'), AC('st_signal', 1, '통제 신호', mx=2)],
    passives=[P('방어벽', 'allyDown', dict(t='guard', v=50, say='방어벽 전개')), BRK('회로 과부하', '회로 과부하')],
    blurb='시민 통제용 보행 드론 — 「통제 신호」 를 뽑을 더미에 넣고(덱에 2장까지) 동료가 쓰러지면 방어벽을 세웁니다. 방어 프로토콜을 펼칠 때 제 강인도를 1 되찾습니다. 격파하면 회로 과부하로 취약 1.')
D['droneg_elite'] = dict(   # C · D(AP)
    intents=[A(120, '경비봉'), BL(95, '경비 장갑'), M(45, 3, '테이저 연사', id='충격', per=1), J('테이저 마비'),
             CH('경보 최고 단계', AA(110, '전체 진압 사격', id='충격', n=2))],
    passives=[BRKC('경보 해제', '경보 해제', '경보 단계', 3)],
    blurb='엘리트 · 관리가 중요한 시설에 상주하는 경비 드론 — 턴마다 「경보 단계」 가 올라 셋이면 다음 차례에 전체 경보(충격 1). 테이저로 다음 턴 AP 를 1 깎습니다. 격파하면 경보가 해제되어 단계가 0 이 됩니다. 희귀종: 전투 시작 결정화 3.')

# ═══════════════ 정령 · 정령산 ═══════════════
D['wisps'] = dict(   # 딜러(광역)
    intents=[DB('고통', 2, '스며드는 원소'), A(70, '원소 불똥'), AA(35, '원소 터뜨리기')],
    blurb='순수한 에너지에 가까운 어린 정령 — 「부유」 둘: 한 턴에 두 번 맞으면 떨어져 기절합니다. 스며드는 원소로 고통 2, 터뜨려 파티 전체를 칩니다.')
D['wisps_elite'] = dict(   # C · D(과열)
    intents=[SUM('wisps', '원소 소환'), A(100, '원소 광선'), AC('st_overheat', 2, '원소 과열'), M(35, 3, '원소 탄'),
             CH('원소 응축', AA(120, '원소 대폭발', id='고통', n=2))],
    passives=[P('원소 삼키기', 'allyDown', dict(t='selfHeal', v=70, say='원소 삼키기'), who='wisps'), BRK('핵 균열', '핵에 금이 감', 2)],
    blurb='엘리트 · 원소가 뭉친 핵 — 위스프를 불러내고(최대 1), 위스프가 쓰러지면 그 원소를 삼켜 회복합니다. 「과열 경고」 를 뽑을 더미에 넣습니다(덱에 4장까지). 격파하면 핵에 금이 가 취약 2. 희귀종: 받는 강인도 피해 -20%.')
D['lupalu'] = dict(   # P
    intents=[M(40, 2, '음료병 휘두르기', id='균열', per=1), B(80, '물 뿜기'), A(70, '병 내려치기')],
    passives=[BRK('병 깨짐', '음료병이 깨짐')],
    blurb='물가에 사는 덩치 큰 정령 — 음료병을 휘두를 때마다 균열을 새기고, 물을 뿜어 실드를 건너 칩니다. 격파하면 병이 깨져 취약 1.')
D['lupalu_elite'] = dict(   # P · C · D(빙결)
    intents=[M(45, 3, '언 음료병 휘두르기', id='균열', per=1), CD('빙결', 2, '얼음 숨결'), B(120, '얼음 물 뿜기'), A(110, '꼬리 후리기'),
             CH('냉기 모으기', M(65, 4, '얼음 병 난타', id='균열'))],
    blurb='엘리트 · 정령산 정상 추위에서 얼지 않으려 쉬지 않는 루파루 — 턴마다 「쉬지 않기」 가 올라 피해가 커지고(최대 4), 격파하면 멈춰 얼어붙어 다 빠집니다. 뽑을 더미 2장을 빙결시킵니다.')
D['oldtree'] = dict(   # 딜러(광역)
    open=A(100, '기습 가지'),
    intents=[CNT('나무껍질', 2, '껍질 굳히기'), AA(60, '가지 휩쓸기'), A(90, '가지 휘두르기')],
    passives=[BRKC('껍질 벗겨짐', '껍질이 벗겨짐', '나무껍질', 3)],
    blurb='겉모습만 보고 지나치는 침입자를 기습하는 영악한 정령 — 첫 턴에 기습하고, 가지로 파티 전체를 휩쓸며, 「나무껍질」 을 두르면 받는 피해가 줄어듭니다(턴 끝에 풀림, 격파하면 벗겨짐).')
D['oldtree_elite'] = dict(   # C · D(침체)
    intents=[A(110, '열매 가지'), AA(75, '가지 난타'), BL(85, '껍질 두르기'), CD('침체', 2, '끈끈한 수액'),
             CH('뿌리 깊이 박기', AA(140, '뿌리째 휩쓸기'))],
    passives=[P('영그는 열매', 'debuffed', BF('사기', 1, '영그는 열매')), BRK('열매 떨어짐', '열매가 떨어짐', 2)],
    blurb='엘리트 · 열매가 맺히기 직전이라 다가오는 모두를 마구 치는 고목 — 디버프를 받으면 사기(턴당 1). 끈끈한 수액으로 뽑을 더미 2장을 침체시킵니다(비용 +1). 격파하면 열매가 떨어져 취약 2. 희귀종: 턴 시작에 손 2장 비용 +1.')

# ═══════════════ 용족 · 용족 동굴 ═══════════════
D['hatchling'] = dict(   # P — 치명타 주의: 급소 노림이 쏘면 비워짐
    hp=340,
    intents=[B(60, '뒷줄 할퀴기'), B(85, '목도리 불꽃'), DB('취약', 1, '목도리 쫙 펴기')],
    counters=[{'name': '급소 노림', 'desc': '목도리를 펴고 급소를 노림', 'onTurnStart': 1, 'max': 3, 'dealt': 0.2, 'clearOnAttack': True}],
    passives=[BRKC('목도리 접힘', '접힌 목도리', '급소 노림', 3)],
    blurb='용족에서 밀려나 짐승처럼 변했다는 소문의 괴물 — 실드를 건너 뒷줄 급소만 노립니다. 턴마다 「급소 노림」 이 올라(하나당 피해 +20%, 최대 3) 공격하면 비워지고, 격파하면 목도리가 접혀 다 빠집니다.')
D['hatchling_elite'] = dict(   # P · C · D(불티) · 소환
    hp=620, open=None,
    intents=[SUM('hatchling_pupil', '제자 모집'), B(110, '창시자의 불'), M(45, 3, '유파 연격'), AC('st_ember', 2, '불씨 흩뿌리기'),
             CH('불씨 모으기', AA(130, '창시자의 화염'))],
    passives=[P('제자의 원수', 'allyDown', BF('사기', 1, '제자의 원수', all=True), who='hatchling_pupil'), BRK('체면 구김', '제자 앞에서 망신', 2)],
    blurb='엘리트 · 새 용족 유파를 세우려는 목도룡 — 제자를 불러 모으고(최대 1), 제자가 쓰러지면 적 전체 사기. 스승을 쓰러뜨리면 제자도 흩어집니다. 「불티」 를 뽑을 더미에 흩뿌립니다(덱에 4장까지 · 손에 남기면 그을림). 격파하면 제자 앞에서 망신을 당해 취약 2. 희귀종: 받는 강인도 피해 -20%.')
D['imoogi'] = dict(   # P
    intents=[B(105, '꿰뚫는 돌진'), A(80, '몸통 감기'), BL(60, '똬리 틀기')],
    passives=[BRK('똬리 풀림', '똬리가 풀림')],
    blurb='완전한 모습을 갖추지 못한 아룡 — 몸을 곧게 뻗어 실드를 건너 꿰뚫습니다. 격파하면 똬리가 풀려 취약 1.')
D['imoogi_elite'] = dict(   # C · D(봉쇄)
    intents=[A(120, '준비 운동'), CH('힘 끌어모으기', AA(150, '궁극의 일격')), BL(105, '다시 준비'), CD('봉쇄', 1, '똬리로 묶기'), M(50, 3, '몸풀기 연타')],
    passives=[P('만반의 준비', 'fightStart', BF('실드 보존', 1, '만반의 준비')), BRK('준비 무너짐', '준비가 무너짐', 2)],
    blurb='엘리트 · 진짜 용족으로 인정받으려 「궁극의 준비 상태」 가 된 아룡 — 첫 턴 두꺼운 실드를 「실드 보존」 으로 넘기고, 똬리로 뽑을 더미 1장을 봉쇄합니다. 모은 힘을 터뜨립니다. 격파하면 준비가 무너져 취약 2. 희귀종: 전투 시작 결정화 3.')
D['proteindragon'] = dict(   # H
    intents=[BL(70, '재활 스트레칭'), A(100, '재활 펀치'), HL(70, '프로틴 셰이크')],
    passives=[P('재활', 'turnEnd', dict(t='selfHeal', v=45, say='재활')), BRK('근육 경련', '근육 경련')],
    blurb='힘을 좇아 몸만 키운 뒤틀린 용 — 턴 끝마다 스스로 회복하고, 프로틴 셰이크로 동료도 회복시킵니다. 격파하면 근육 경련으로 취약 1. 느린 지속 피해보다 몰아 치기.')
D['proteindragon_elite'] = dict(   # H · C · D(AP)
    intents=[BF('사기', 1, '하나 더!', all=True), A(130, '조교의 주먹'), BL(85, '시범 자세'), J('얼차려'),
             CH('마지막 한 세트 준비', A(280, '한계 돌파 펀치'))],
    passives=[P('자세 불량', 'card', A(100, '자세 불량!'), same=True), BRK('조교 체면', '조교 체면 구김', 2)],
    blurb='엘리트 · 주변을 거칠게 훈련시키는 조교 근육인데용 — 적 전체 사기를 올리고 얼차려로 다음 턴 AP 를 1 깎습니다. 같은 사도의 카드를 잇달아 내면 「자세 불량!」 으로 바로 칩니다. 격파하면 체면이 구겨져 취약 2. 희귀종: 전투 시작 결정화 3.')
D['golem'] = dict(   # 벽 · C
    hp=480,
    intents=[A(100, '보석 주먹'), CNT('불순물 껍질', 2, '불순물 덧씌우기'), A(85, '무거운 발걸음', rush=0), CH('보석 주먹 치켜들기', A(175, '보석 내려찍기'))],
    passives=[BRKC('껍질 깨짐', '깨진 껍질', '불순물 껍질', 2)],
    blurb='용족이 동굴을 지키려 불순물 섞인 보석을 뭉쳐 만든 골렘 — 「불순물 껍질」 둘이 남은 동안 받는 피해가 1입니다(맞으면 하나씩, 격파하면 다 깨짐). 잘게 여러 번 쳐서 벗기거나 격파한 뒤 크게.')
D['golem_elite'] = dict(   # C · D(돌무더기)
    hp=880, tough=8,
    intents=[A(140, '수호 주먹'), BL(120, '보석 광택 내기', tough=2), AC('st_rubble', 2, '돌무더기 쏟기'), A(160, '대지 강타'),
             CH('대지 끌어올리기', AA(155, '대지 붕괴'))],
    passives=[P('결 깨짐', 'broken', CNT('수호 결', -5, '결 깨짐')), P('드러난 핵', 'broken', BF('취약', 3, '드러난 핵'))],
    blurb='엘리트 · 다야가 직접 만든 수호 골렘 — 강인도 8. 턴마다 「수호 결」 이 쌓여 받는 피해가 줄고(최대 5), 「보석 광택 내기」 로 실드를 두르며 강인도를 2 되찾습니다. 「돌무더기」 2장을 뽑을 더미에 쏟습니다(비용 2 · 내면 사라짐 · 덱에 4장까지). 격파하면 결이 깨지며 취약 3. 희귀종: 전투 시작 결정화 3.')
D['crayonwarrior'] = dict(   # 딜러(rush 3)
    intents=[A(110, '크레용 돌진'), M(40, 3, '덧칠 연타'), A(80, '꼬다리 찌르기')],
    passives=[P('영광의 꼬다리', 'allyDown', BF('사기', 1, '영광의 꼬다리')), BRK('심 부러짐', '크레용 심이 부러짐')],
    blurb='크레용 차원의 전사 — 카드 세 장이면 돌진해 칩니다. 동료가 쓰러지면 사기, 격파하면 심이 부러져 취약 1. 서두르는 편이 좋습니다.')
D['crayontanker'] = dict(   # 벽 · D(AP)
    intents=[BL(70, '손바닥 방패', tough=1), J('배로 밀치기'), A(70, '방패 밀기')],
    blurb='비대한 몸으로 눈길을 끄는 크레용 전사 — 턴마다 첫 대는 「간지럼」 으로 피해 1. 손바닥 방패를 들 때 제 강인도를 1 되찾고, 밀쳐 내 다음 턴 AP 를 깎습니다. 가벼운 카드로 간지럼을 벗긴 뒤 크게, 격파하면 취약 2.')
D['crayonarcher'] = dict(   # P · C(모아 쏘는 저격)
    intents=[B(90, '허리 휘어 쏘기'), CH('허리 끝까지 젖히기', B(190, '한 발 더 쏘아 맞히기')), A(60, '크레용 화살')],
    passives=[BRK('허리 삐끗', '허리를 삐끗함')],
    blurb='긴 허리로 멀리서 쏘는 크레용 전사 — 실드를 건너 뒷줄을 노리고, 허리를 끝까지 젖힌 큰 한 발은 격파로 끊깁니다. 격파하면 허리를 삐끗해 취약 1.')
D['crayonwizard'] = dict(   # H
    intents=[BF('사기', 1, '전쟁 놀이 응원', all=True), HL(80, '덧칠 치료'), DB('약화', 1, '낙서 주문'), A(50, '지팡이 톡')],
    passives=[BRK('지팡이 놓침', '지팡이를 놓침')],
    blurb='크레용 부족의 장로 마법사 — 적 전체 사기를 올리고 동료를 회복시키며 약화를 겁니다. 격파하면 지팡이를 놓쳐 취약 1. 먼저 끊는 편이 좋습니다.')

# 이름이 바뀌지 않은 옛 패시브 · 카운터를 둘 몬스터(위에 passives 를 안 적은 것) — 옛 것 그대로에 격파 반응만 더한다
KEEP_PASSIVES_ADD = {
    'goldring_elite': None, 'marshmallowtanker': None, 'buseuleogi': None, 'pumpkin': BRK('껍질 쪼개짐', '호박 껍질이 쪼개짐'),
    'hatsnail': BRK('모자 벗겨짐', '모자가 벗겨짐'), 'wisps': None, 'lupalu_elite': None, 'crayontanker': None,
}

# ── 누루링 수액(마을 종족 스킨 24) — 역할 넷을 틀로 맞춘다: 탱커 = 벽(적 전체 실드 → 제 실드) · 전사 = C · 마법사 = P · 서포터 = H ──
DISRUPT = {'addCard', 'cardDebuff', 'handCost', 'jam', 'seize', 'reshuffle', 'autoPlay'}
NURU_BLURB = {
    'nururingarcher_dragon': '돌을 던지는 젤리 — 실드를 건너 돌을 던지고 자갈로 손상을 겁니다. 격파하면 취약 1.',
    'nururingarcher_fairy': '마력 빵을 던지는 젤리 — 실드를 건너 빵을 던지고 고통을 뿌립니다. 격파하면 취약 1.',
    'nururingarcher_furry': '울부짖는 젤리 — 실드를 건너 화살을 쏘고 으르렁대 약화를 겁니다. 격파하면 취약 1.',
    'nururingarcher_spirit': '원소 젤리 — 불(고통) · 번개(충격) 화살로 실드를 건너 칩니다. 격파하면 취약 1.',
}
NURU_ADD = {   # 방해를 뺀 자리를 채우는 수(마법사)
    'nururingarcher_dragon': DB('손상', 1, '자갈 세례'), 'nururingarcher_fairy': DB('고통', 2, '마력 빵 부스러기'),
    'nururingarcher_furry': DB('약화', 1, '으르렁'), 'nururingarcher_spirit': B(80, '번개 화살', id='충격', n=1),
}

def nururing(k, e):
    role = k.split('_')[0]
    ins = e['intents']
    if role == 'nururingtanker':
        for i in ins:
            if i['t'] == 'guard': i['t'] = 'block'; i['v'] = int(i['v'] * 1.5)
        ins[:] = [i for i in ins if not (i['t'] == 'buff' and i.get('all'))]
    elif role == 'nururingwarrior':
        for i in ins:
            if i['t'] == 'back': i['t'] = 'attack'
    elif role == 'nururingarcher':
        ins[:] = [i for i in ins if i['t'] not in DISRUPT]
        add = NURU_ADD.get(k)
        if add and all(i.get('say') != add['say'] for i in ins): ins.append(copy.deepcopy(add))
        if k in NURU_BLURB: e['blurb'] = NURU_BLURB[k]
        if len(ins) < 3: ins.append(A(60, '젤리 몸통 치기'))
    elif role == 'nururingsupporter':
        ins[:] = [i for i in ins if i['t'] not in ('back', 'charge') and i['t'] not in DISRUPT]
    ps = e.setdefault('passives', [])
    if not any(p['on'] == 'broken' for p in ps) and role != 'nururingwarrior':
        ps.append(BRK('물러짐', '젤리가 물러짐'))
        if '격파하면' not in (e.get('blurb') or ''): e['blurb'] = (e.get('blurb') or '').rstrip() + ' 격파하면 물러져 취약 1.'

def apply(E):
    """build.py 가 부른다 — 위 설계를 E 에 덮는다(값은 원값)."""
    for k, d in D.items():
        e = E[k]
        for f, v in d.items():
            if f in ('open', 'counters', 'passives', 'phase', 'rare') and v is None: e.pop(f, None)
            else: e[f] = copy.deepcopy(v)
    for k, extra in KEEP_PASSIVES_ADD.items():
        if extra is not None and not any(p['on'] == 'broken' for p in E[k].get('passives', [])):
            E[k].setdefault('passives', []).append(copy.deepcopy(extra))
            E[k]['blurb'] = E[k]['blurb'].rstrip() + f" 격파하면 취약 {extra['do']['v']}."
    for k, e in E.items():
        if k.startswith('nururing'): nururing(k, e)
    return E

DESIGNED = set(D)
