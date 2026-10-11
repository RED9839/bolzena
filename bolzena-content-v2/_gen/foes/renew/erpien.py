# 적 리뉴얼 — 에르피엔(erpien · 요정). 2026-10-10. 기준: _measure/적_리뉴얼/00_기준.md
# 마을 축: 「단것 · 장난 · 빵집」 — 당(허기 — 다단이 약) ↔ 부글부글(다단이 독) 짝, 카드 종류에 반응하는 괭이 · 요정,
#   쓰러질 때 남기는 것(목매킴 고통 · 부스러기 설탕 · 산사모 가사), 실드 분기(탱탱 멜로 · 요정 전사).
# 공용 적 없음.
from lib import P, main

FOES = {
"fairymobcloserange": dict(  # 저혈당 요정 — 딜러 · 허기(다단이 약)
    hp=390, tough=3,
    blurb="당이 떨어져 눈이 돌아간 요정 — 허기 셋으로 시작해 맞을 때마다 하나씩 빠집니다. 잘게 여러 번 쳐서 허기를 뺀 뒤 크게.",
    intents=[
        {"t": "attack", "v": 90, "say": "배고파서 물기", "w": 3},
        {"t": "attack", "v": 100, "say": "허겁지겁 달려들기", "w": 2},
        {"t": "multi", "v": 40, "n": 2, "say": "허둥지둥 할퀴기", "w": 2},
    ],
    counters=[{"name": "허기", "desc": "당이 떨어져 사나움", "start": 3, "max": 3, "dealt": 0.2, "onHit": -1, "resetTurnStart": True}],
    passives=[P("당 떨어짐", "broken", {"t": "count", "id": "허기", "v": -3, "say": "당이 뚝 떨어짐"})],
),
"fairymoblongrange": dict(  # 고혈당 요정 — 방해 · 공격 카드엔 손 비용 -1, 아닌 카드엔 +1
    hp=290, tough=3,
    blurb="새 채소 맛에 들뜬 요정 — 공격 카드를 보면 신이 나 손 1장 비용 -1, 다른 카드를 보면 심술로 +1. 공격으로 몰면 덕을 봅니다.",
    intents=[
        {"t": "attack", "v": 80, "say": "채소 던지기", "w": 3},
        {"t": "debuff", "id": "약화", "v": 1, "say": "혀 내밀기", "w": 2},
        {"t": "attack", "v": 65, "say": "콕 찌르기", "w": 2},
    ],
    passives=[P("깔깔 웃음", "card", {"t": "handCost", "v": -1, "n": 1, "say": "깔깔 — 손 1장 비용 -1"}, type="공격"),
              P("뾰로통", "card", {"t": "handCost", "v": 1, "n": 1, "say": "뾰로통 — 손 1장 비용 +1"}, type="!공격")],
),
"magicfork": dict(  # 불효자손 — 딜러 · 공격 아닌 카드 4장이면 심통 폭발
    hp=330, tough=3,
    blurb="일할 밭을 잃은 요정의 마법 괭이 — 공격이 아닌 카드를 낼 때마다 심통이 붙고, 넷이면 바로 내리찍습니다. 격파하면 심통이 풀립니다.",
    intents=[
        {"t": "attack", "v": 105, "say": "밭을 갈듯 내리찍기", "w": 3},
        {"t": "multi", "v": 45, "n": 2, "say": "녹슨 날로 두 번 긁기", "w": 2},
        {"t": "block", "v": 55, "say": "자루 곧추세우기", "w": 2},
    ],
    counters=[{"name": "심통", "desc": "공격이 아닌 카드에 나는 심통", "onCard": 1, "cardType": "!공격", "max": 4, "at": 4,
               "act": {"t": "attack", "v": 120, "say": "심통 폭발 — 내리찍기"}}],
    passives=[P("자루 빠짐", "broken", {"t": "count", "id": "심통", "v": -4, "say": "자루가 빠짐"})],
),
"ginseng": dict(  # 산사모 — 지원 · 쓰러지면 숨은 척(가사) — 다른 적이 회복하면 일어선다
    hp=350, tough=3,
    blurb="세계수 양분으로 걸어 다니는 산삼 — 동료를 회복하고 비명으로 약화를 겁니다. 쓰러지면 땅속에 숨은 척하다가 동료가 회복하면 일어섭니다.",
    intents=[
        {"t": "heal", "v": 60, "say": "뿌리즙 나누기", "w": 3},
        {"t": "attack", "v": 75, "say": "쓴 즙 뿌리기", "w": 2},
        {"t": "debuff", "id": "약화", "v": 1, "say": "비명 지르기", "w": 2},
    ],
    passives=[P("땅속으로", "death", {"t": "feign", "say": "땅속으로 쏙 숨기"})],
),
"mogmaekim": dict(  # 목매킴 — 딜러 · 쓰러지면 펑(고통)
    hp=360, tough=3,
    blurb="잘못 구워진 빵 — 쓰러지면 펑 터져 파티에 고통 2 를 남깁니다. 마지막에 치우거나 고통에 대비하세요.",
    intents=[
        {"t": "attack", "v": 100, "say": "통통 튀기", "w": 3},
        {"t": "block", "v": 55, "say": "바람 넣기", "w": 2},
        {"t": "attack", "v": 110, "say": "몸통 박치기", "w": 2},
    ],
    passives=[P("펑!", "death", {"t": "debuff", "id": "고통", "v": 2, "say": "펑! 터짐"})],
),
"buseuleogi": dict(  # 부스러기 — 물량 · 쓰러지면 설탕 범벅 · 동료가 쓰러지면 성남
    hp=260, tough=3,
    blurb="잘못 구워진 컵케이크 — 떼로 나와 동료가 쓰러질 때마다 성나고, 쓰러지면 설탕 범벅 1장을 덱에 남깁니다.",
    intents=[
        {"t": "attack", "v": 75, "say": "부스러기 세례", "w": 3},
        {"t": "multi", "v": 35, "n": 2, "say": "부스럭부스럭", "w": 2},
        {"t": "attack", "v": 65, "say": "바스락 할퀴기", "w": 2},
    ],
    passives=[P("마지막 부스러기", "death", {"t": "addCard", "id": "st_sugar", "n": 1, "to": "draw", "say": "마지막 부스러기"}),
              P("같이 바스러지기", "allyDown", {"t": "buff", "id": "사기", "v": 1, "say": "부스럭 성내기"})],
),
"marshmallowtanker": dict(  # 탱탱 멜로 — 벽 · 실드가 깨지면 말랑 조각으로 동료 실드
    hp=580, tough=5,
    blurb="탱탱한 마시멜로 골렘 — 부풀 때 강인도를 1 되찾고, 제 실드가 깨지면 조각을 흩어 동료 모두에게 실드를 줍니다. 격파하면 취약 2.",
    intents=[
        {"t": "block", "v": 80, "say": "탱탱하게 부풀기", "tough": 1, "w": 3},
        {"t": "attack", "v": 100, "say": "말랑 박치기", "w": 2},
        {"t": "attack", "v": 80, "say": "말랑 깔아뭉개기", "w": 2},
    ],
    passives=[P("말랑 흩어짐", "guardBreak", {"t": "guard", "v": 35, "say": "말랑 조각 흩어짐"}),
              P("터진 마시멜로", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "속이 터짐"})],
),
"marshmallowdealer": dict(  # 말랑 멜로 — 딜러 · 부글부글(다단이 독)
    hp=360, tough=3,
    blurb="뜯어먹힐까 늘 화가 난 마시멜로 — 맞을 때마다 부글부글이 올라 세집니다(최대 3). 한 방에 크게, 격파하면 김이 빠집니다.",
    intents=[
        {"t": "attack", "v": 105, "say": "부글부글 박치기", "w": 3},
        {"t": "multi", "v": 45, "n": 2, "say": "꼬치 난타", "w": 2},
        {"t": "multi", "v": 35, "n": 3, "say": "분풀이 연타", "w": 2},
    ],
    counters=[{"name": "부글부글", "desc": "맞을수록 화가 남", "onHit": 1, "dealt": 0.1, "max": 3}],
    passives=[P("김 빠짐", "broken", {"t": "count", "id": "부글부글", "v": -3, "say": "김이 빠짐"})],
),
"marshmallowsupporter": dict(  # 쫀득 멜로 — 지원 · 회복 + 피해 감소 코팅
    hp=330, tough=3,
    blurb="쫀득한 응원 마시멜로 — 동료를 회복하고 모두에게 설탕 코팅(피해 감소)을 두릅니다. 코팅은 격파하면 벗겨집니다.",
    intents=[
        {"t": "heal", "v": 70, "say": "쫀득 붕대", "w": 3},
        {"t": "buff", "id": "피해 감소", "v": 1, "say": "설탕 코팅", "all": True, "w": 2},
        {"t": "attack", "v": 70, "say": "톡 치기", "w": 2},
    ],
    passives=[P("코팅 벗겨짐", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "코팅이 벗겨짐"})],
),
"nururingtanker_fairy": dict(  # 누루링-요정 탱커 — 벽 · 맞으면 통통 부풀어 실드(턴에 두 번)
    hp=560, tough=5,
    blurb="통통한 젤리 방패 — 맞을 때마다 통통 부풀어 실드를 얻습니다(턴에 두 번). 큰 한 방이나 격파로.",
    intents=[
        {"t": "attack", "v": 105, "say": "통통 부딪히기", "w": 3},
        {"t": "guard", "v": 45, "say": "젤리로 감싸기", "w": 2},
        {"t": "attack", "v": 130, "say": "깔고 앉기", "w": 2},
    ],
    passives=[P("통통 부풀기", "hurt", {"t": "block", "v": 35, "say": "통통 부풀기"}, limit=2)],
),
"nururingwarrior_fairy": dict(  # 누루링-요정 전사 — 딜러 · 힘자랑: 몸을 굳힌 다음엔 어퍼컷
    hp=400, tough=3,
    blurb="젤리 살이 아니라 근육이라 주장하는 젤리 — 몸을 굳혀 실드가 남아 있으면 다음엔 크게 어퍼컷을 칩니다. 실드를 깨 두세요.",
    intents=[
        {"t": "block", "v": 55, "say": "근육 자랑", "w": 3},
        {"t": "attack", "v": 145, "say": "젤리 어퍼컷", "if": {"selfBlock": True}, "w": 4},
        {"t": "attack", "v": 90, "say": "젤리 주먹", "w": 2},
    ],
),
"nururingarcher_fairy": dict(  # 누루링-요정 마법사 — 저격 · 마력 빵이 두 턴마다 구워짐
    hp=310, tough=3,
    blurb="마력 빵을 굽는 젤리 — 턴마다 빵이 익어 둘이 되면 다음 차례에 실드를 건너 뜨거운 빵을 던집니다.",
    intents=[
        {"t": "debuff", "id": "고통", "v": 2, "say": "마력 빵 부스러기", "w": 3},
        {"t": "back", "v": 75, "say": "딱딱한 빵", "w": 2},
        {"t": "attack", "v": 65, "say": "젤리 몸통 치기", "w": 2},
    ],
    counters=[{"name": "굽는 빵", "desc": "익어 가는 마력 빵", "onTurnStart": 1, "max": 2, "at": 2,
               "act": {"t": "back", "v": 110, "say": "갓 구운 빵"}, "mode": "next"}],
),
"nururingsupporter_fairy": dict(  # 누루링-요정 서포터 — 지원 · 응원 박자: 카드 4장마다 동료 사기
    hp=330, tough=3,
    blurb="응원하는 젤리 — 파티가 카드를 4장 낼 때마다 박자를 맞춰 동료 모두 사기를 얻습니다(턴에 한 번). 회복도 씁니다.",
    intents=[
        {"t": "heal", "v": 75, "say": "젤리 붕대", "w": 3},
        {"t": "attack", "v": 65, "say": "톡 치기", "w": 2},
        {"t": "block", "v": 45, "say": "응원 깃발", "w": 2},
    ],
    passives=[P("응원 박자", "card", {"t": "buff", "id": "사기", "v": 1, "all": True, "say": "힘내라 젤리!"}, every=4)],
),
# ── 엘리트 ───────────────────────────────────────────────────────────
"fairymobcloserange_elite": dict(  # 토핑 폭식 — 허기(다단이 약) + 어지럼 + 폭식 회복
    hp=620, tough=6,
    blurb="엘리트 · 빵은 버리고 토핑만 먹는 요정 — 허기 넷(맞으면 하나씩 빠짐)으로 세게 치고, 어지럼을 덱에 옮기며 토핑을 퍼먹어 회복합니다.",
    intents=[
        {"t": "attack", "v": 130, "say": "크림 범벅 주먹", "w": 3},
        {"t": "multi", "v": 45, "n": 3, "say": "잼 난사", "w": 2},
        {"t": "addCard", "id": "st_dizzy", "n": 2, "to": "draw", "say": "어지럼 옮기기", "max": 4, "w": 2},
        {"t": "heal", "v": 90, "say": "토핑 폭식", "w": 2},
    ],
    counters=[{"name": "허기", "desc": "당이 떨어져 사나움", "start": 4, "max": 4, "dealt": 0.15, "onHit": -1, "resetTurnStart": True}],
    passives=[P("토핑 쏟음", "broken", {"t": "count", "id": "허기", "v": -4, "say": "토핑을 쏟음"})],
),
"magicfork_elite": dict(  # 폭주 괭이 — 맞을 때마다 폭주 → 다섯이면 연타 · 침체
    hp=540, tough=6,
    blurb="엘리트 · 무엇이든 갈아 버리는 괭이 — 맞을 때마다 폭주가 쌓여 다섯이면 바로 세 번 찍습니다. 흙을 튀겨 뽑을 더미를 침체시킵니다. 격파하면 폭주가 풀립니다.",
    intents=[
        {"t": "attack", "v": 120, "say": "폭주 내려찍기", "w": 3},
        {"t": "multi", "v": 45, "n": 3, "say": "쉬지 않고 찍기", "w": 2},
        {"t": "cardDebuff", "id": "침체", "n": 2, "to": "draw", "say": "흙 튀기기", "w": 2},
        {"t": "attack", "v": 165, "say": "땅 뒤엎기", "w": 2},
    ],
    counters=[{"name": "폭주", "desc": "맞을수록 날뜀", "onHit": 1, "max": 5, "at": 5, "act": {"t": "multi", "v": 45, "n": 3, "say": "폭주 연타"}}],
    passives=[P("자루 꺾임", "broken", {"t": "count", "id": "폭주", "v": -5, "say": "자루가 꺾임"}),
              P("날 빠짐", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "날이 빠짐"})],
),
"ginseng_elite": dict(  # 웃자람 산사모 — 턴마다 회복(DPS 체크) + 독 뿌리 · 절반에서 비명
    hp=480, tough=6,
    blurb="엘리트 · 너무 오래 자라 양분이 넘치는 산삼 — 턴마다 스스로 회복하고, 쓴 뿌리를 심어 뽑을 더미에 독을 겁니다. 몰아쳐서 끝내세요.",
    intents=[
        {"t": "heal", "v": 80, "say": "양분 뿜기", "w": 2},
        {"t": "attack", "v": 120, "say": "쓴 즙 뿌리기", "w": 3},
        {"t": "cardDebuff", "id": "독", "n": 2, "to": "draw", "say": "쓴 뿌리 심기", "w": 2},
        {"t": "attackAll", "v": 50, "say": "쓴 즙 폭발", "id": "고통", "n": 2, "w": 2},
    ],
    passives=[P("넘치는 양분", "turnStart", {"t": "heal", "v": 35, "say": "넘치는 양분"}),
              P("양분 새어 나감", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "양분이 새어 나감"})],
),
"goldring_elite": dict(  # 새마음금고 — 공격 카드 두 장마다 뚜껑(실드) + 카드 삼키기
    hp=700, tough=7,
    blurb="엘리트 · 요정 왕국 금고를 노린 슬라임 — 공격 카드 두 장째마다 뚜껑을 닫아 실드를 얻고(턴에 두 번), 카드 1장을 금고에 삼킵니다.",
    intents=[
        {"t": "block", "v": 90, "say": "자물쇠 걸기", "w": 2},
        {"t": "multi", "v": 45, "n": 3, "say": "동전 세례", "w": 3},
        {"t": "seize", "n": 1, "v": 20, "say": "금고에 꿀꺽", "w": 2},
        {"t": "attack", "v": 140, "say": "금괴 내려찍기", "w": 3},
    ],
    passives=[P("뚜껑", "card", {"t": "block", "v": 65, "say": "뚜껑 닫기"}, type="공격", every=2, limit=2),
              P("열린 금고", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "열린 금고"})],
),
}

# 마을 세기 맞춤(03:13 엔진 측정 2026-10-10): ×1.00 49.9% · ×1.04 44.7% · ×1.12 31.9% → 치는 수 × 1.07 · HP × 1.02.
from lib import scale
import os
scale(FOES, dmg=float(os.environ.get("FOE_DMG", "1.07")), hp=float(os.environ.get("FOE_HP", "1.02")))

F1, F2, MF, GS, MK, BS = "fairymobcloserange", "fairymoblongrange", "magicfork", "ginseng", "mogmaekim", "buseuleogi"
MT, MD, MS = "marshmallowtanker", "marshmallowdealer", "marshmallowsupporter"
TK, WR, MG, SP = "nururingtanker_fairy", "nururingwarrior_fairy", "nururingarcher_fairy", "nururingsupporter_fairy"
POOLS = [
    {  # 1층 — 세계수 숲
        "pools": [
            [[F1, MF], [F2, GS], [BS, MK], [WR, F2], [MF, BS]],
            [[F1, F2], [GS, MF], [TK, MG], [MK, F1], [MF, SP]],
            [[F1, GS, F2], [TK, WR, SP], [MF, MK, F2], [BS, BS, F1], [GS, WR, MF]],
        ],
        "elites": [["magicfork_elite", F2], ["ginseng_elite", MF], ["fairymobcloserange_elite", MG], ["magicfork_elite", SP]],
    },
    {  # 2층 — 요정 왕도
        "pools": [
            [[MT, MD], [F1, BS], [MK, F2], [MD, MS], [WR, BS]],
            [[MT, F2], [MK, BS, F1], [TK, MD], [F1, MS, MK], [MT, BS]],
            [[MT, MD, MS], [F1, F2, MK], [TK, WR, MS], [BS, MK, MD], [MT, F1, MG]],
        ],
        "elites": [["goldring_elite", MD], ["fairymobcloserange_elite", MG], ["goldring_elite", F1], ["ginseng_elite", TK]],
    },
]

if __name__ == '__main__':
    main('erpien', FOES, POOLS)
