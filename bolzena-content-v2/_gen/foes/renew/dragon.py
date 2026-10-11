# 적 리뉴얼 — 용족 동굴(dragon · 불길과 물길의 터). 2026-10-10. 기준: _measure/적_리뉴얼/00_기준.md
# 마을 축: 「힘 자랑 · 동굴 수호」 — 공격 카드 / 공격 아닌 카드에 따로 반응하는 짝(길어용 ↔ 누루링 마법사),
#   타수로 벗기는 껍질(동석), 다시 그려 넣는 크레용(지원할레용 소환), 쓰러질 때 한 발(휠레용).
# 공용 적 없음(용족 적은 이 파일에만).
from lib import P, main

FOES = {
"hatchling": dict(  # 목도룡 — 저격 · 시간(급소 노림)
    hp=360, tough=3,
    blurb="용족에서 밀려나 짐승이 됐다는 괴물 — 턴마다 급소 노림이 쌓여 뒷줄 일격이 세지고, 치면 비워집니다. 격파하면 목도리가 접힙니다.",
    intents=[
        {"t": "back", "v": 80, "say": "목도리 불꽃", "w": 3},
        {"t": "debuff", "id": "취약", "v": 1, "say": "목도리 쫙 펴기", "w": 2},
        {"t": "attack", "v": 70, "say": "꼬리 할퀴기", "w": 2},
    ],
    counters=[{"name": "급소 노림", "desc": "목도리를 펴고 급소를 노림", "onTurnStart": 1, "max": 3, "dealt": 0.2, "clearOnAttack": True}],
    passives=[P("목도리 접힘", "broken", {"t": "count", "id": "급소 노림", "v": -3, "say": "접힌 목도리"})],
),
"imoogi": dict(  # 길어용 — 딜러 · 공격 아닌 카드 3장이면 틈을 노려 꿰뚫기
    hp=420, tough=3,
    blurb="완전한 모습을 갖추지 못한 아룡 — 공격이 아닌 카드를 4장 내면 그 틈에 몸을 곧게 뻗어 꿰뚫습니다.",
    intents=[
        {"t": "attack", "v": 95, "say": "몸통 감기", "w": 3},
        {"t": "back", "v": 90, "say": "꿰뚫는 돌진", "w": 2},
        {"t": "block", "v": 60, "say": "똬리 틀기", "w": 2},
    ],
    counters=[{"name": "틈 노리기", "desc": "빈틈을 엿봄", "onCard": 1, "cardType": "!공격", "max": 4, "at": 4,
               "act": {"t": "back", "v": 45, "say": "틈새 꿰뚫기"}}],
),
"proteindragon": dict(  # 근육인데용 — 지원(자기 회복) · 재활: 턴 끝마다 회복 → 몰아치기
    hp=480, tough=4,
    blurb="힘을 좇아 상체만 키운 용 — 턴이 끝날 때마다 재활로 스스로 회복합니다. 나눠 치지 말고 한 턴에 몰아치세요.",
    intents=[
        {"t": "block", "v": 70, "say": "재활 스트레칭", "w": 2},
        {"t": "attack", "v": 115, "say": "아령 펀치", "w": 3},
        {"t": "heal", "v": 65, "say": "프로틴 셰이크", "w": 2},
    ],
    passives=[P("재활", "turnEnd", {"t": "selfHeal", "v": 45, "say": "재활"})],
),
"golem": dict(  # 동석 — 벽 · 불순물 껍질(타수로 벗김)
    hp=500, tough=4,
    blurb="불순물 섞인 보석을 뭉친 골렘 — 껍질 둘이 남은 동안 받는 피해가 1 입니다. 잘게 여러 번 쳐서 벗기거나 격파하세요.",
    intents=[
        {"t": "attack", "v": 115, "say": "보석 주먹", "w": 3},
        {"t": "count", "id": "불순물 껍질", "v": 2, "say": "불순물 덧씌우기", "w": 2},
        {"t": "attack", "v": 90, "say": "보석 내려찍기", "w": 2},
    ],
    counters=[{"name": "불순물 껍질", "desc": "탁한 껍질", "start": 2, "max": 2, "flat": 1, "onHit": -1}],
    passives=[P("껍질 깨짐", "broken", {"t": "count", "id": "불순물 껍질", "v": -2, "say": "깨진 껍질"})],
),
"crayonwarrior": dict(  # 칠레용 — 딜러 · 행동 카운트 3(카드 세 장이면 돌진)
    hp=350, tough=3, rush=3,
    blurb="영광스럽게 돌진해 꼬다리만 남기를 바라는 크레용 전사 — 카드를 3장 낼 때마다 그 자리에서 돌진합니다.",
    intents=[
        {"t": "attack", "v": 115, "say": "크레용 돌진", "w": 3},
        {"t": "multi", "v": 45, "n": 2, "say": "덧칠 연타", "w": 2},
        {"t": "attack", "v": 90, "say": "꼬다리 찌르기", "w": 2},
    ],
),
"crayontanker": dict(  # 막을레용 — 벽 · 방패 들 때 강인도 회복 + 밀치기(AP)
    hp=600, tough=5,
    blurb="비대한 몸으로 앞을 막는 크레용 전사 — 손바닥 방패를 들 때마다 강인도를 되찾고, 배로 밀쳐 다음 턴 AP 를 1 깎습니다. 격파하면 취약 2.",
    intents=[
        {"t": "block", "v": 70, "say": "손바닥 방패", "tough": 1, "w": 3},
        {"t": "jam", "v": 1, "say": "배로 밀치기", "w": 2},
        {"t": "attack", "v": 95, "say": "방패 밀기", "w": 2},
    ],
    passives=[P("상처는 훈장", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "진짜 아픔"})],
),
"crayonarcher": dict(  # 휠레용 — 저격 · 쓰러질 때 마지막 한 발
    hp=320, tough=3,
    blurb="긴 허리로 멀리서 쏘는 크레용 전사 — 실드를 건너 뒷줄을 노리고, 쓰러질 때도 마지막 한 발을 쏩니다.",
    intents=[
        {"t": "back", "v": 95, "say": "허리 휘어 쏘기", "w": 3},
        {"t": "attack", "v": 75, "say": "크레용 화살", "w": 2},
        {"t": "block", "v": 45, "say": "허리 숙이기", "w": 2},
    ],
    passives=[P("마지막 한 발", "death", {"t": "back", "v": 50, "say": "마지막 한 발"})],
),
"crayonwizard": dict(  # 지원할레용 — 지원/소환 · 동료가 쓰러지면 칠레용을 다시 그린다
    hp=350, tough=3,
    blurb="크레용 부족의 장로 마법사 — 동료를 회복하고 사기를 올립니다. 동료가 쓰러지면 그 자리에 칠레용을 그려 넣습니다(하나까지).",
    intents=[
        {"t": "heal", "v": 75, "say": "덧칠 치료", "w": 3},
        {"t": "buff", "id": "사기", "v": 1, "say": "전쟁 놀이 응원", "all": True, "w": 2},
        {"t": "attack", "v": 65, "say": "지팡이 톡", "w": 2},
    ],
    passives=[P("다시 그리기", "allyDown", {"t": "summon", "id": "crayonwarrior", "n": 1, "max": 1, "say": "칠레용 다시 그리기"})],
),
"nururingtanker_dragon": dict(  # 누루링-용족 탱커 — 벽 · 뚝심(턴마다 단단해짐, 격파하면 0)
    hp=700, tough=5,
    blurb="뚝심 있게 동료를 지키는 젤리 방패 — 턴마다 뚝심이 쌓여 받는 피해가 줄어듭니다. 격파하면 뚝심이 꺾입니다.",
    intents=[
        {"t": "guard", "v": 50, "say": "젤리 방벽", "w": 3},
        {"t": "attack", "v": 115, "say": "젤리 박치기", "w": 3},
        {"t": "attack", "v": 135, "say": "깔고 앉기", "w": 2},
    ],
    counters=[{"name": "뚝심", "desc": "견고한 자세", "onTurnStart": 1, "taken": -0.1, "max": 3}],
    passives=[P("뚝심 꺾임", "broken", {"t": "count", "id": "뚝심", "v": -3, "say": "뚝심이 꺾임"})],
),
"nururingwarrior_dragon": dict(  # 누루링-용족 전사 — 딜러 · 투기: 격파에서 일어서면 성난다 → 맞을수록
    hp=480, tough=3,
    blurb="투기가 남다른 젤리 — 맞을 때마다 투기가 올라 세집니다(최대 3). 격파하면 투기가 빠집니다.",
    intents=[
        {"t": "attack", "v": 95, "say": "용 젤리 펀치", "w": 3},
        {"t": "multi", "v": 45, "n": 2, "say": "젤리 연타", "w": 2},
        {"t": "block", "v": 50, "say": "버티기", "w": 2},
    ],
    counters=[{"name": "투기", "desc": "싸울수록 끓는 피", "onHit": 1, "dealt": 0.1, "max": 3}],
    passives=[P("투기 빠짐", "broken", {"t": "count", "id": "투기", "v": -3, "say": "투기가 빠짐"})],
),
"nururingarcher_dragon": dict(  # 누루링-용족 마법사 — 저격 · 공격 카드 4장이면 맞받아 돌팔매(길어용과 짝)
    hp=340, tough=3,
    blurb="돌을 던지는 젤리 — 파티가 공격 카드를 5장 내면 그 자리에서 돌을 맞받아 던집니다. 자갈로 손상을 겁니다.",
    intents=[
        {"t": "back", "v": 85, "say": "돌 던지기", "w": 3},
        {"t": "debuff", "id": "손상", "v": 1, "say": "자갈 세례", "w": 2},
        {"t": "attack", "v": 70, "say": "젤리 몸통 치기", "w": 2},
    ],
    counters=[{"name": "돌팔매", "desc": "주워 든 돌", "onCard": 1, "cardType": "공격", "max": 5, "at": 5,
               "act": {"t": "back", "v": 45, "say": "맞받아 돌팔매"}}],
),
"nururingsupporter_dragon": dict(  # 누루링-용족 서포터 — 지원 · 전투 시작에 동료 전체 실드
    hp=360, tough=3,
    blurb="응원하는 젤리 — 전투가 시작되면 동료 전체에 비늘 실드를 두르고, 회복과 결의를 씁니다.",
    intents=[
        {"t": "heal", "v": 75, "say": "젤리 붕대", "w": 3},
        {"t": "buff", "id": "결의", "v": 1, "say": "용의 비늘처럼!", "all": True, "w": 2},
        {"t": "attack", "v": 65, "say": "톡 치기", "w": 2},
    ],
    passives=[P("젤리 비늘", "fightStart", {"t": "guard", "v": 45, "say": "젤리 비늘 두르기"})],
),
# ── 엘리트 ───────────────────────────────────────────────────────────
"hatchling_elite": dict(  # 유파 창시자 — 제자 소환 · 제자가 쓰러지면 성냄 + 불티
    hp=620, tough=6,
    blurb="엘리트 · 새 유파를 세우려는 목도룡 — 제자를 불러 모으고(하나까지), 제자가 쓰러지면 모두 사기를 얻습니다. 불티를 덱에 흩뿌립니다.",
    intents=[
        {"t": "summon", "id": "hatchling_pupil", "n": 1, "max": 1, "say": "제자 모집", "w": 2},
        {"t": "back", "v": 125, "say": "창시자의 불", "w": 3},
        {"t": "multi", "v": 45, "n": 3, "say": "유파 연격", "w": 2},
        {"t": "addCard", "id": "st_ember", "n": 2, "to": "draw", "say": "불씨 흩뿌리기", "max": 4, "w": 2},
        {"t": "attackAll", "v": 55, "say": "창시자의 화염", "w": 2},
    ],
    passives=[P("제자의 원수", "allyDown", {"t": "buff", "id": "사기", "v": 1, "say": "제자의 원수", "all": True}, who="hatchling_pupil"),
              P("체면 구김", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "제자 앞에서 망신"})],
),
"hatchling_pupil": dict(  # 제자 — 창시자가 쓰러지면 흩어짐 · 두리번(주인을 찾음)
    hp=330, tough=3, tied=True,
    blurb="창시자가 불러 모은 목도룡 제자 — 창시자가 쓰러지면 흩어집니다. 턴마다 두리번이 올라 셋이면 다음 차례에 뒷줄을 크게 노립니다.",
    intents=[
        {"t": "back", "v": 90, "say": "뒷줄 쪼기", "w": 2},
        {"t": "attack", "v": 70, "say": "꼬리 치기", "w": 3},
        {"t": "block", "v": 50, "say": "날개로 감싸기", "w": 2},
    ],
    counters=[{"name": "두리번", "desc": "주인을 찾는 눈길", "onTurnStart": 1, "max": 3, "at": 3, "act": {"t": "back", "v": 140, "say": "주인님인 줄 알았어!"}, "mode": "next"}],
),
"imoogi_elite": dict(  # 궁극의 준비 — 실드 분기: 실드가 있을 때만 궁극의 일격
    hp=860, tough=7,
    blurb="엘리트 · 진짜 용족이 되려 궁극의 준비 상태가 된 아룡 — 두꺼운 준비 실드가 남아 있을 때만 궁극의 일격을 씁니다. 실드를 깨 두세요.",
    open={"t": "block", "v": 200, "say": "궁극의 준비"},
    intents=[
        {"t": "attack", "v": 155, "say": "준비 운동", "w": 3},
        {"t": "attackAll", "v": 95, "say": "궁극의 일격", "if": {"selfBlock": True}, "w": 4},
        {"t": "block", "v": 110, "say": "다시 준비", "w": 2},
        {"t": "cardDebuff", "id": "봉쇄", "n": 1, "to": "draw", "say": "똬리로 묶기", "w": 2},
        {"t": "multi", "v": 60, "n": 3, "say": "몸풀기 연타", "w": 2},
    ],
    passives=[P("준비 무너짐", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "준비가 무너짐"})],
),
"proteindragon_elite": dict(  # 조교 — 같은 사도 연속이면 자세 불량 + 얼차려
    hp=680, tough=6,
    blurb="엘리트 · 주변을 거칠게 훈련시키는 조교 — 같은 사도의 카드를 잇달아 내면 「자세 불량!」 으로 바로 칩니다. 얼차려로 다음 턴 AP 를 깎습니다.",
    intents=[
        {"t": "buff", "id": "사기", "v": 1, "say": "하나 더!", "all": True, "w": 2},
        {"t": "attack", "v": 155, "say": "조교의 주먹", "w": 3},
        {"t": "block", "v": 85, "say": "시범 자세", "w": 2},
        {"t": "jam", "v": 1, "say": "얼차려", "w": 2},
        {"t": "attack", "v": 185, "say": "한계 돌파 펀치", "w": 2},
    ],
    passives=[P("자세 불량", "card", {"t": "attack", "v": 90, "say": "자세 불량!"}, same=True),
              P("조교 체면", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "조교 체면 구김"})],
),
"golem_elite": dict(  # 수호 골렘 — 수호 결(시간) + 광택(강인도 회복) · 격파 덤 큼
    hp=1000, tough=8,
    blurb="엘리트 · 다야가 직접 만든 수호 골렘 — 턴마다 수호 결이 쌓여 단단해지고, 광택을 낼 때 강인도를 2 되찾습니다. 격파하면 결이 깨지며 취약 3.",
    intents=[
        {"t": "attack", "v": 180, "say": "수호 주먹", "w": 3},
        {"t": "block", "v": 110, "say": "보석 광택 내기", "tough": 2, "w": 2},
        {"t": "addCard", "id": "st_rubble", "n": 1, "to": "draw", "say": "돌무더기 쏟기", "max": 3, "w": 2},
        {"t": "attackAll", "v": 80, "say": "대지 붕괴", "w": 2},
    ],
    counters=[{"name": "수호 결", "desc": "쌓여 가는 결정의 결", "onTurnStart": 1, "taken": -0.08, "max": 4}],
    passives=[P("결 깨짐", "broken", {"t": "count", "id": "수호 결", "v": -4, "say": "결 깨짐"}),
              P("드러난 핵", "broken", {"t": "buff", "id": "취약", "v": 3, "say": "드러난 핵"})],
),
}

# 마을 세기 맞춤(측정 2026-10-10): 용족 동굴은 1층 보스(클론)가 무거워 일반 적을 덜어 준다 — 일반 적의 피해 × DMG_N · HP × HP_N.
DMG_N, HP_N = 1.0, 1.0   # 03:08 클론 루드(1층 보스)가 가벼워져 임시 완화(0.88 · 0.92)를 되돌림
def _scale(f):
    f['hp'] = int(round(f['hp'] * HP_N / 10.0)) * 10
    for it in f.get('intents', []) + ([f['open']] if f.get('open') else []):
        if it['t'] in ('attack', 'back', 'multi', 'attackAll') and 'v' in it: it['v'] = int(round(it['v'] * DMG_N / 5.0)) * 5
for k, f in FOES.items():
    if not k.endswith('_elite'): _scale(f)

H, I, PR, G = "hatchling", "imoogi", "proteindragon", "golem"
CW, CT, CA, CZ = "crayonwarrior", "crayontanker", "crayonarcher", "crayonwizard"
TK, WR, MG, SP = "nururingtanker_dragon", "nururingwarrior_dragon", "nururingarcher_dragon", "nururingsupporter_dragon"
POOLS = [
    {  # 1층 — 용족 동굴 어귀
        "pools": [
            [[I, PR], [G, CW], [CW, PR], [WR, H], [I, SP]],
            [[PR, H], [G, I], [TK, MG], [CT, CA], [I, CZ]],
            [[I, PR, CT], [TK, WR, SP], [CT, CW, CZ], [G, H, SP], [PR, I, MG]],
        ],
        "elites": [["imoogi_elite", H], ["hatchling_elite", CW], ["proteindragon_elite", I], ["imoogi_elite", SP]],
    },
    {  # 2층 — 용족 동굴
        "pools": [
            [[G, PR], [CW, CA], [H, G], [WR, I], [CT, H]],
            [[G, H], [CT, CW], [TK, I, SP], [PR, MG], [G, CZ]],
            [[G, PR, I], [CT, CW, CZ], [TK, WR, MG], [G, H, PR], [I, CZ, WR]],
        ],
        "elites": [["golem_elite", PR], ["proteindragon_elite", H], ["hatchling_elite", TK], ["golem_elite", CZ]],
    },
]

if __name__ == '__main__':
    main('dragon', FOES, POOLS)
