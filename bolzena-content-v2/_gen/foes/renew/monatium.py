# 적 리뉴얼 — 모나티엄(monatium · 엘프). 2026-10-10. 기준: _measure/적_리뉴얼/00_기준.md
# 마을 축: 「군대 · 기계 · 공사」 — 대열(동료가 격파되면 메움) · 엄폐 사격(실드 분기) · 행동 카운트 드론 · 통제 신호 ·
#   스킬 카드에 신경질(엘프 전사) · 주운 장비(디버프 → 실드) · 폐수를 빨아 회복하는 한입초.
# 공용 적 나누기: 한입초 → nependers_monatium(냉정 — 엘프 폐수를 빨아들인 한입초).
import lib
from lib import P, main

FOES = {
"elfsoldiercloserange": dict(  # 돌격병 — 벽 · 대열 메우기(동료가 격파되면 방패)
    hp=530, tough=4,
    blurb="모나티엄의 징병 군인 — 방패 대열로 모두의 결의를 올리고, 동료가 격파되면 빈자리를 메우며 방패를 세웁니다.",
    intents=[
        {"t": "buff", "id": "결의", "v": 1, "say": "방패 대열", "all": True, "w": 2},
        {"t": "attack", "v": 120, "say": "돌격", "w": 3},
        {"t": "block", "v": 60, "say": "방패 세우기", "w": 2},
    ],
    passives=[P("대열 메우기", "allyBroken", {"t": "block", "v": 70, "say": "빈자리 메우기"})],
),
"elfsoldierlongrange": dict(  # 명사수 — 저격 · 엄폐 뒤 사격(실드가 있을 때만 정밀 사격)
    hp=360, tough=3,
    blurb="원거리 병과로 뽑힌 엘프 군인 — 엄폐(실드)가 남아 있으면 다음엔 한 치 어긋남 없는 관통 사격을 합니다. 엄폐를 깨 두세요.",
    intents=[
        {"t": "block", "v": 55, "say": "엄폐", "w": 3},
        {"t": "back", "v": 150, "say": "한 치 어긋남 없는 한 발", "if": {"selfBlock": True}, "w": 4},
        {"t": "back", "v": 90, "say": "의장 사격", "w": 2},
    ],
),
"drones": dict(  # 드론 S형 — 딜러 · 행동 카운트 3 + 절반에서 비상 경보
    hp=320, tough=3, rush=3,
    blurb="엘프들의 비행 감시 드론 — 카드를 3장 낼 때마다 그 자리에서 쏩니다. HP 절반 아래에선 비상 경보로 충격을 겁니다.",
    intents=[
        {"t": "attack", "v": 100, "say": "레이저 점사", "id": "충격", "n": 1, "w": 3},
        {"t": "attack", "v": 85, "say": "부딪히기", "w": 2},
        {"t": "debuff", "id": "충격", "v": 2, "say": "방전", "w": 2},
    ],
    passives=[P("비상 경보", "lowHp", {"t": "debuff", "id": "충격", "v": 1, "say": "비상 경보"}, at=0.5)],
),
"droneg": dict(  # 드론 G형 — 방해/벽 · 통제 신호 + 방어 프로토콜(강인도 회복)
    hp=580, tough=5,
    blurb="시민 통제용 보행 드론 — 통제 신호를 덱에 넣고(2장까지), 방어 프로토콜로 강인도를 되찾습니다. 동료가 쓰러지면 방어벽을 세웁니다.",
    intents=[
        {"t": "block", "v": 70, "say": "방어 프로토콜", "tough": 1, "w": 3},
        {"t": "attack", "v": 120, "say": "돌진", "w": 2},
        {"t": "addCard", "id": "st_signal", "n": 1, "to": "draw", "say": "통제 신호", "max": 2, "w": 2},
    ],
    passives=[P("방어벽", "allyDown", {"t": "guard", "v": 45, "say": "방어벽 전개"})],
),
"nururingtanker_elf": dict(  # 누루링-엘프 탱커 — 벽 · 주운 장비: 디버프를 받으면 실드
    hp=630, tough=5,
    blurb="생각보다 괜찮은 장비를 주운 젤리 — 디버프를 받을 때마다 장비로 실드를 얻습니다(턴에 한 번). 디버프보다 피해를.",
    intents=[
        {"t": "block", "v": 85, "say": "고철 방벽", "w": 3},
        {"t": "attack", "v": 120, "say": "고철 박치기", "w": 3},
        {"t": "attack", "v": 140, "say": "깔고 앉기", "w": 2},
    ],
    passives=[P("주운 장비", "debuffed", {"t": "block", "v": 60, "say": "주운 장비"})],
),
"nururingwarrior_elf": dict(  # 누루링-엘프 전사 — 딜러 · 신경질: 스킬 카드 3장마다 찌르기
    hp=440, tough=3,
    blurb="다른 누루링과 사이 나쁜 신경질쟁이 젤리 — 스킬 카드를 3장 낼 때마다 신경질이 터져 바로 관통으로 찌릅니다.",
    intents=[
        {"t": "attack", "v": 105, "say": "고철 망치", "w": 3},
        {"t": "multi", "v": 45, "n": 2, "say": "젤리 주먹", "w": 2},
        {"t": "block", "v": 45, "say": "웅크리기", "w": 2},
    ],
    counters=[{"name": "신경질", "desc": "거슬리는 스킬 카드", "onCard": 1, "cardType": "스킬", "max": 3, "at": 3, "act": {"t": "back", "v": 65, "say": "신경질 찌르기"}}],
),
"nururingarcher_elf": dict(  # 누루링-엘프 마법사 — 방해 · 기름 연기(고통 · 손상) · 격파하면 기름에 미끄러짐
    hp=320, tough=3,
    blurb="마법을 쓴다고 우기는 기름 젤리 — 고통 · 손상을 퍼뜨리고, 쓰러질 때 기름 웅덩이로 파티를 미끄럽게 합니다.",
    intents=[
        {"t": "debuff", "id": "고통", "v": 2, "say": "기름 연기", "w": 3},
        {"t": "debuff", "id": "손상", "v": 1, "say": "끈적한 기름", "w": 2},
        {"t": "back", "v": 85, "say": "기름 방울", "w": 2},
    ],
    passives=[P("기름 웅덩이", "death", {"t": "debuff", "id": "미끄러움", "v": 1, "say": "기름 웅덩이"})],
),
"nururingsupporter_elf": dict(  # 누루링-엘프 서포터 — 지원 · 사이 나쁜 동료가 쓰러지면 고소해함
    hp=340, tough=3,
    blurb="누루링들만이 아는 응급 수리 젤리 — 회복과 사기를 돌리고, 사이 나쁜 동료가 쓰러지면 오히려 신납니다(사기).",
    intents=[
        {"t": "heal", "v": 80, "say": "응급 수리", "w": 3},
        {"t": "buff", "id": "사기", "v": 1, "say": "힘내라 젤리!", "all": True, "w": 2},
        {"t": "attack", "v": 70, "say": "톡 치기", "w": 2},
    ],
    passives=[P("고소함", "allyDown", {"t": "buff", "id": "사기", "v": 1, "say": "고소함"})],
),
}

def _blurb(s):
    def f(e): e['blurb'] = s
    return f
lib.PATCH.update({
    "elfsoldiercloserange_elite": _blurb("엘리트 · 노동반 공병의 현장 반장 — 턴이 끝날 때 파티에 실드가 남아 있으면 바리케이드가 두꺼워집니다(모두 결의). 돌무더기를 덱에 쌓습니다."),
    "elfsoldierlongrange_elite": _blurb("엘리트 · 노동반 곁을 지키는 경계조장 — 뽑을 더미를 봉쇄로 묶고, 큰 한 발은 카드로 당겨지지 않습니다. 동료가 격파되면 경계 태세(사기)."),
    "droneg_elite": _blurb("엘리트 · 중요 시설에 상주하는 경비 드론 — 턴마다 경보 단계가 올라 셋이면 다음 차례에 전체 경보. 테이저로 AP 를 깎습니다. 격파하면 경보가 해제됩니다."),
})

NEW = {
"nependers_monatium": dict(  # 폐수 한입초 — 벽/방해 · 턴 끝 폐수 흡수(회복) · 폐수 얼룩
    name="한입초", row="front", nature="냉정", hp=470, tough=4,
    blurb="엘프의 폐수를 빨아들여 뒤틀린 한입초 — 턴이 끝날 때마다 폐수를 빨아 회복하고, 폐수 얼룩을 덱에 뱉습니다. 몰아쳐서 끝내세요.",
    intents=[
        {"t": "attack", "v": 110, "say": "덥석", "w": 3},
        {"t": "addCard", "id": "st_wastewater", "n": 1, "to": "draw", "say": "폐수 뱉기", "max": 2, "w": 2},
        {"t": "attack", "v": 95, "say": "꿀꺽", "w": 2},
    ],
    passives=[P("폐수 흡수", "turnEnd", {"t": "selfHeal", "v": 40, "say": "폐수 흡수"})],
),
}
ART = {"nependers_monatium": "nependers"}

C, L, S, G, NEP = "elfsoldiercloserange", "elfsoldierlongrange", "drones", "droneg", "nependers_monatium"
TK, WR, MG, SP = "nururingtanker_elf", "nururingwarrior_elf", "nururingarcher_elf", "nururingsupporter_elf"
POOLS = [
    {  # 1층 — 모나티엄 외곽
        "pools": [
            [[C, L], [S, NEP], [G, L], [WR, S], [C, NEP]],
            [[G, S], [C, NEP, S], [TK, MG], [C, L, S], [G, SP]],
            [[C, G, L], [TK, WR, SP], [S, MG, NEP], [C, MG, S], [G, L, TK]],
        ],
        "elites": [["droneg_elite", S], ["elfsoldiercloserange_elite", C], ["elfsoldierlongrange_elite", C], ["droneg_elite", SP]],
    },
    {  # 2층 — 모나티엄 도심
        "pools": [
            [[C, S], [G, L], [L, TK], [WR, C], [S, G]],
            [[C, L, S], [G, NEP], [TK, L, SP], [C, G], [S, SP, L]],
            [[C, L, S], [G, S, MG], [TK, WR, C], [C, G, NEP], [L, SP, G]],
        ],
        "elites": [["elfsoldiercloserange_elite", TK], ["droneg_elite", C], ["elfsoldierlongrange_elite", C], ["elfsoldiercloserange_elite", SP]],
    },
]
DROP = ()

if __name__ == '__main__':
    main('monatium', FOES, POOLS, NEW, DROP)
