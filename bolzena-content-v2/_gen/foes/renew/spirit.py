# 적 리뉴얼 — 정령산(spirit). 2026-10-10. 기준: _measure/적_리뉴얼/00_기준.md · 결과 문서: 마을_정령산.md
# 마을 축: 「원소 · 산 — 버티면 터진다」 — 카제나 세크레드 축(단단함 누적 · 부활 · 시간 뒤 강공)을 정령산에.
# 공용 적 나누기: 한입초 → nependers_spirit(우울 — 산지 외피) · 호바깅 → pumpkin_spirit / pumpkin_spirit_elite(냉정 — 고랭지).
from lib import P, main

FOES = {
"wisps": dict(  # 위스프 — 지원(광역) · 타수가 약: 두 번 맞으면 떨어져 기절
    hp=320, tough=3,
    blurb="순수한 에너지에 가까운 어린 정령 — 떠 있는 동안 원소를 터뜨립니다. 한 턴에 두 번 맞으면 떨어져 기절합니다.",
    intents=[
        {"t": "debuff", "id": "고통", "v": 2, "say": "스며드는 원소", "w": 2},
        {"t": "attack", "v": 100, "say": "원소 불똥", "w": 3},
        {"t": "attackAll", "v": 55, "say": "원소 터뜨리기", "w": 3},
    ],
    counters=[{"name": "부유", "desc": "떠다니는 몸", "start": 2, "max": 2, "onHit": -1, "stunAtZero": True, "resetTurnStart": True}],
),
"lupalu": dict(  # 루파루 — 딜러 · 체력 구간: 절반 아래면 병이 깨져 폭주
    hp=380, tough=3,
    blurb="물가의 덩치 큰 정령 — 땜빵한 음료병을 아슬아슬 지킵니다. HP 가 절반 아래가 되면 병이 깨져 다음 수가 난타로 바뀝니다.",
    intents=[
        {"t": "multi", "v": 45, "n": 2, "say": "음료병 휘두르기", "id": "균열", "per": 1, "w": 3},
        {"t": "back", "v": 85, "say": "물 뿜기", "w": 3},
        {"t": "attack", "v": 105, "say": "병 내려치기", "w": 2},
    ],
    passives=[P("깨진 병", "lowHp", {"t": "shift", "say": "병이 깨졌다!", "next": {"t": "multi", "v": 32, "n": 3, "say": "깨진 병 난타"}}, at=0.5)],
),
"oldtree": dict(  # 고모구지 — 벽 · 기습 + 껍질(그 턴만 단단)
    hp=480, tough=4,
    blurb="겉모습만 보고 지나치는 침입자를 기습하는 정령 — 첫 턴에 기습하고, 껍질을 굳힌 턴에는 받는 피해가 줄어듭니다.",
    open={"t": "attack", "v": 110, "say": "기습 가지"},
    intents=[
        {"t": "count", "id": "나무껍질", "v": 3, "say": "껍질 굳히기", "w": 3},
        {"t": "attackAll", "v": 60, "say": "가지 휩쓸기", "w": 2},
        {"t": "attack", "v": 120, "say": "가지 휘두르기", "w": 3},
    ],
    counters=[{"name": "나무껍질", "desc": "굳힌 껍질 — 턴이 끝나면 풀림", "taken": -0.15, "max": 3, "clearTurnEnd": True}],
    passives=[P("껍질 벗겨짐", "broken", {"t": "count", "id": "나무껍질", "v": -3, "say": "껍질이 벗겨짐"})],
),
"nururingtanker_spirit": dict(  # 누루링-정령 탱커 — 벽 · 우직하게 버틴다(강인도 회복) · 격파 덤이 크다
    hp=660, tough=5,
    blurb="말수 적고 우직한 젤리 방패 — 깎인 강인도를 묵묵히 되찾습니다. 격파하면 무너져 취약 3.",
    intents=[
        {"t": "brace", "v": 2, "say": "묵묵히 버티기", "w": 2},
        {"t": "attack", "v": 120, "say": "젤리 박치기", "w": 3},
        {"t": "guard", "v": 55, "say": "젤리 방벽", "w": 2},
        {"t": "attack", "v": 145, "say": "깔고 앉기", "w": 2},
    ],
    passives=[P("무너짐", "broken", {"t": "buff", "id": "취약", "v": 3, "say": "우직함이 무너짐"})],
),
"nururingwarrior_spirit": dict(  # 누루링-정령 전사 — 딜러 · 날카로움: 맞으면 되친다(다단 불리)
    hp=450, tough=3,
    blurb="성격이 날카로운 젤리 — 맞을 때마다 때린 사도에게 되칩니다(턴에 두 번). 큰 한 방으로 상대하세요.",
    intents=[
        {"t": "attack", "v": 100, "say": "정령 펀치", "w": 3},
        {"t": "multi", "v": 45, "n": 2, "say": "날 선 연타", "w": 2},
        {"t": "block", "v": 55, "say": "가부좌", "w": 2},
    ],
    passives=[P("날카로움", "hurt", {"t": "thorns", "v": 20, "say": "날카롭게 되침"}, limit=2)],
),
"nururingarcher_spirit": dict(  # 누루링-정령 마법사 — 방해 · 장난: 손패 비용 +1
    hp=330, tough=3,
    blurb="장난기 넘치는 원소 젤리 — 파티가 손패를 뽑으면 장난을 쳐 카드 1장 비용을 1 올립니다. 화살은 실드를 건넙니다.",
    intents=[
        {"t": "back", "v": 85, "say": "불 화살", "id": "고통", "n": 1, "w": 3},
        {"t": "back", "v": 80, "say": "번개 화살", "id": "충격", "n": 1, "w": 2},
        {"t": "attack", "v": 75, "say": "젤리 몸통 치기", "w": 2},
    ],
    passives=[P("원소 장난", "afterDraw", {"t": "handCost", "v": 1, "n": 1, "say": "원소 장난"})],
),
"nururingsupporter_spirit": dict(  # 누루링-정령 서포터 — 지원 · 느긋함: 턴마다 조금씩 회복, 끼어들지 않는다
    hp=360, tough=3, rush=0,
    blurb="느긋해서 늘 뒤처지는 젤리 — 카드 수와 상관없이 턴 끝에만 움직이고, 턴이 끝날 때마다 가장 다친 동료를 조금 회복합니다.",
    intents=[
        {"t": "heal", "v": 80, "say": "젤리 붕대", "w": 3},
        {"t": "buff", "id": "불굴", "v": 1, "say": "정령의 가호", "all": True, "w": 2},
        {"t": "attack", "v": 70, "say": "느릿느릿 톡", "w": 2},
    ],
    passives=[P("느긋한 손길", "turnEnd", {"t": "heal", "v": 35, "say": "느긋한 손길"})],
),
# ── 엘리트 ───────────────────────────────────────────────────────────
"wisps_elite": dict(  # 원소 핵 — 소환 + 타이머(과열 3 이면 대폭발)
    hp=600, tough=6,
    blurb="엘리트 · 원소가 뭉친 핵 — 위스프를 불러내고, 턴마다 과열이 올라 3 이 되면 대폭발합니다. 격파하면 과열이 식습니다.",
    intents=[
        {"t": "summon", "id": "wisps", "n": 1, "max": 1, "say": "원소 소환", "w": 2},
        {"t": "attack", "v": 150, "say": "원소 광선", "w": 3},
        {"t": "addCard", "id": "st_overheat", "n": 2, "to": "draw", "say": "원소 과열", "max": 4, "w": 2},
        {"t": "multi", "v": 50, "n": 3, "say": "원소 탄", "w": 2},
    ],
    counters=[{"name": "과열", "desc": "끓어오르는 핵", "onTurnStart": 1, "max": 3, "at": 3,
               "act": {"t": "attackAll", "v": 85, "say": "원소 대폭발", "id": "고통", "n": 2}, "mode": "next"}],
    passives=[P("핵이 식음", "broken", {"t": "count", "id": "과열", "v": -3, "say": "핵이 식음"}),
              P("원소 삼키기", "allyDown", {"t": "selfHeal", "v": 70, "say": "원소 삼키기"}, who="wisps")],
),
"lupalu_elite": dict(  # 산정 루파루 — 시간 누적(쉬지 않기) + 빙결
    hp=620, tough=6,
    blurb="엘리트 · 정상 추위에 얼지 않으려 쉬지 않는 루파루 — 턴마다 쉬지 않기가 올라 세지고, 격파하면 멈춰 얼어붙습니다. 뽑을 더미를 빙결시킵니다.",
    intents=[
        {"t": "multi", "v": 50, "n": 3, "say": "언 음료병 휘두르기", "id": "균열", "per": 1, "w": 3},
        {"t": "cardDebuff", "id": "빙결", "n": 2, "to": "draw", "say": "얼음 숨결", "w": 2},
        {"t": "back", "v": 125, "say": "얼음 물 뿜기", "w": 3},
        {"t": "attack", "v": 130, "say": "꼬리 후리기", "w": 2},
        {"t": "attack", "v": 170, "say": "빙벽 들이받기", "w": 2},
    ],
    counters=[{"name": "쉬지 않기", "desc": "얼지 않으려 계속 움직임", "onTurnStart": 1, "dealt": 0.1, "max": 4}],
    passives=[P("얼어붙음", "broken", {"t": "count", "id": "쉬지 않기", "v": -4, "say": "멈춰서 얼어붙음"})],
),
"oldtree_elite": dict(  # 열매 직전 고모구지 — 디버프 → 사기 + 절반에서 열매 터짐(국면)
    hp=760, tough=8,
    blurb="엘리트 · 열매가 맺히기 직전인 고목 — 디버프를 받으면 열매가 영급니다(사기). HP 절반에서 열매가 터져 마구 휩씁니다.",
    intents=[
        {"t": "attack", "v": 135, "say": "열매 가지", "w": 3},
        {"t": "attackAll", "v": 80, "say": "가지 난타", "w": 2},
        {"t": "block", "v": 90, "say": "껍질 두르기", "w": 2},
        {"t": "cardDebuff", "id": "침체", "n": 2, "to": "draw", "say": "끈끈한 수액", "w": 2},
    ],
    phase={"at": 0.5, "say": "열매가 터진다", "intents": [
        {"t": "attackAll", "v": 90, "say": "열매 폭발", "w": 3},
        {"t": "multi", "v": 55, "n": 3, "say": "씨앗 난사", "w": 2},
        {"t": "attack", "v": 170, "say": "뿌리째 내려치기", "w": 2},
    ]},
    passives=[P("영그는 열매", "debuffed", {"t": "buff", "id": "사기", "v": 1, "say": "영그는 열매"}),
              P("열매 떨어짐", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "열매가 떨어짐"})],
),
}

NEW = {
"nependers_spirit": dict(  # 한입초(산지) — 벽 · 단단함이 쌓인다, 격파하면 갈라진다
    name="한입초", row="front", nature="우울", hp=480, tough=4,
    blurb="산지에서 단단한 외피를 키운 한입초 — 외피를 굳힐수록 받는 피해가 줄어듭니다. 격파하면 외피가 갈라집니다.",
    intents=[
        {"t": "count", "id": "단단함", "v": 2, "say": "외피 굳히기", "w": 3},
        {"t": "attack", "v": 120, "say": "덥석", "w": 3},
        {"t": "attack", "v": 100, "say": "꿀꺽", "w": 2},
    ],
    counters=[{"name": "단단함", "desc": "굳어 가는 외피", "taken": -0.08, "max": 5}],
    passives=[P("외피 갈라짐", "broken", {"t": "count", "id": "단단함", "v": -5, "say": "외피가 갈라짐"})],
),
"pumpkin_spirit": dict(  # 고랭지 호바깅 — 벽 · 껍질을 깨면 휘청(실드 분기 — 머곰과 반대로 깨는 게 이득)
    name="호바깅", row="front", nature="냉정", hp=440, tough=4,
    blurb="고지대에서 억지로 자란 호박 — 두꺼운 껍질을 두르고 굴러 덮칩니다. 껍질을 깨면 휘청여 약한 수로 바뀝니다.",
    intents=[
        {"t": "block", "v": 90, "say": "두꺼운 껍질", "w": 3},
        {"t": "attack", "v": 150, "say": "굴러 덮치기", "if": {"selfBlock": True}, "w": 3},
        {"t": "attack", "v": 105, "say": "데굴데굴", "w": 2},
    ],
    passives=[P("껍질 깨짐", "guardBreak", {"t": "shift", "say": "휘청!", "next": {"t": "attack", "v": 50, "say": "휘청이며 부딪치기"}})],
),
"pumpkin_spirit_elite": dict(  # 고랭지 호바깅 엘리트 — 부활(세크레드 축)
    name="호바깅 · 고랭지", row="front", nature="냉정", hp=820, tough=7,
    blurb="엘리트 · 맞지 않는 땅에서 버틴 호박 — 쓰러지면 2턴 뒤 40% 로 다시 자랍니다. 서리로 뽑을 더미를 침체시킵니다.",
    intents=[
        {"t": "attack", "v": 180, "say": "서리 박치기", "w": 3},
        {"t": "block", "v": 95, "say": "웅크리기", "w": 2},
        {"t": "multi", "v": 60, "n": 3, "say": "언 씨앗 난사", "w": 2},
        {"t": "cardDebuff", "id": "침체", "n": 2, "to": "draw", "say": "서리 범벅", "w": 2},
    ],
    passives=[P("다시 자람", "death", {"t": "revive", "v": 40, "n": 2, "say": "언 땅에서 다시 자람"}),
              P("껍질 쪼개짐", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "껍질이 쪼개짐"})],
),
}
ART = {"nependers_spirit": "nependers", "pumpkin_spirit": "pumpkin", "pumpkin_spirit_elite": "pumpkin_elite"}

W, L, O, NEP, PU = "wisps", "lupalu", "oldtree", "nependers_spirit", "pumpkin_spirit"
TK, WR, MG, SP = "nururingtanker_spirit", "nururingwarrior_spirit", "nururingarcher_spirit", "nururingsupporter_spirit"
POOLS = [
    {  # 1층 — 정령산 기슭
        "pools": [
            [[W, L], [O, W], [PU, L], [WR, W], [NEP, L]],
            [[O, W, L], [NEP, PU], [TK, MG], [W, W, L], [O, SP]],
            [[O, L, W], [TK, WR, SP], [PU, NEP, W], [L, SP, WR], [O, PU, MG]],
        ],
        "elites": [["wisps_elite", L], ["oldtree_elite", W], ["pumpkin_spirit_elite", L], ["wisps_elite", TK]],
    },
    {  # 2층 — 정령산 꼭대기
        "pools": [
            [[L, W], [O, MG], [W, PU], [WR, L], [NEP, W]],
            [[O, L, W], [PU, MG], [TK, W, MG], [NEP, W], [L, SP, WR]],
            [[O, L, MG], [PU, NEP, MG], [TK, WR, L], [O, PU, W], [L, SP, NEP]],
        ],
        "elites": [["lupalu_elite", O], ["oldtree_elite", W], ["wisps_elite", PU], ["lupalu_elite", TK]],
    },
]
DROP = ()

if __name__ == '__main__':
    main('spirit', FOES, POOLS, NEW, DROP)
