# 적 리뉴얼 — 유령 늪(ghost). 2026-10-10. 기준: _measure/적_리뉴얼/00_기준.md
# 마을 축: 「장난 · 팬심 · 늪」 — 행동 카운트로 튀어나오는 장난(이불령), 처치 순서(극성팬 · 죽은 척 탱커 + 서포터),
#   실드를 깨는 때가 반대인 짝(호바깅: 턴 끝 전에 깨라 ↔ 햇팽이: 깨면 숨는다), 맞으면 덥석(늪 한입초), 바늘 저주(인형).
# 공용 적 나누기: 한입초 → nependers_ghost(광기 — 늪지대의 예민한 한입초) · 누루링 인형 → curseddoll_ghost.
import lib
from lib import P, main

FOES = {
"blanketghost": dict(  # 이불령 — 저격 · 행동 카운트 3(카드 3장마다 튀어나와 뒷줄 찌르기)
    hp=340, tough=3, rush=3,
    blurb="모습을 갖추지 못한 어린 유령 — 카드를 3장 낼 때마다 이불 속에서 튀어나와 실드를 건너 찌릅니다.",
    intents=[
        {"t": "back", "v": 95, "say": "이불 속에서 찌르기", "w": 3},
        {"t": "debuff", "id": "미끄러움", "v": 1, "say": "이불 잡아당기기", "w": 2},
        {"t": "attack", "v": 80, "say": "이불 휘감기", "w": 2},
    ],
),
"shadyfollowercloserange": dict(  # 극성팬 — 딜러 · 동료가 쓰러지면 극성(사기)
    hp=450, tough=3,
    blurb="셰이디 팬클럽의 극성 유령 — 응원봉 연타마다 균열을 새기고, 동료가 쓰러질 때마다 더 극성이 됩니다(사기).",
    intents=[
        {"t": "multi", "v": 45, "n": 3, "say": "응원봉 연타", "id": "균열", "per": 1, "w": 3},
        {"t": "attack", "v": 120, "say": "찌르기", "w": 2},
        {"t": "block", "v": 45, "say": "응원봉 방패", "w": 2},
    ],
    passives=[P("더 극성", "allyDown", {"t": "buff", "id": "사기", "v": 1, "say": "셰이디님을 위해!"})],
),
"shadyfollowerlongrange": dict(  # 극성팬 원거리 — 저격 · 열광이 쌓였다 쏟는다(시간)
    hp=350, tough=3,
    blurb="아공간을 넘나드는 극성팬 — 턴마다 열광이 쌓여 순간이동 찌르기가 세지고, 찌르면 비워집니다.",
    intents=[
        {"t": "back", "v": 105, "say": "순간이동 찌르기", "id": "균열", "n": 2, "w": 3},
        {"t": "attack", "v": 75, "say": "응원봉", "w": 2},
        {"t": "block", "v": 45, "say": "아공간 숨기", "w": 2},
    ],
    counters=[{"name": "열광", "desc": "셰이디를 향한 열광", "onTurnStart": 1, "max": 3, "dealt": 0.2, "clearOnAttack": True}],
    passives=[P("열광 식음", "broken", {"t": "count", "id": "열광", "v": -3, "say": "열광이 식음"})],
),
"pumpkin": dict(  # 호바깅 — 벽 · 턴 끝에 실드가 남아 있으면 사기(턴 안에 깨라)
    hp=420, tough=4,
    blurb="유령들에게 장난당하며 자란 호박 — 턴이 끝날 때 제 실드가 남아 있으면 사기를 얻습니다. 그 턴 안에 껍질을 깨세요.",
    intents=[
        {"t": "block", "v": 75, "say": "두꺼운 껍질", "w": 3},
        {"t": "attack", "v": 115, "say": "데굴데굴", "w": 2},
        {"t": "buff", "id": "결의", "v": 1, "say": "단단한 호박 껍질", "all": True, "w": 2},
    ],
    passives=[P("여무는 껍질", "turnEnd", {"t": "buff", "id": "사기", "v": 1, "say": "여무는 껍질", "if": {"selfBlock": True}})],
),
"hatsnail": dict(  # 햇팽이 — 방해 · 실드가 깨지면 모자 속으로(그 턴 단단) · 쪽지
    hp=450, tough=4,
    blurb="마녀 모자를 집 삼은 달팽이 — 실드가 깨지면 그 턴엔 모자 속에 숨어 덜 아픕니다. 모자 속 쪽지를 덱에 흘립니다.",
    intents=[
        {"t": "block", "v": 75, "say": "모자 속으로 숨기", "w": 3},
        {"t": "attack", "v": 110, "say": "끈적한 몸통박치기", "w": 2},
        {"t": "addCard", "id": "st_note", "n": 1, "to": "draw", "say": "모자 속 쪽지 흘리기", "max": 2, "w": 2},
    ],
    counters=[{"name": "모자 껍질", "desc": "모자 속에 숨은 몸", "taken": -0.2, "max": 2, "clearTurnEnd": True}],
    passives=[P("모자 깊이", "guardBreak", {"t": "count", "id": "모자 껍질", "v": 2, "say": "모자 깊이 숨기"})],
),
"nururingtanker_ghost": dict(  # 누루링-유령 탱커 — 벽 · 죽은 척(가사) — 서포터 회복에 일어선다
    hp=700, tough=5,
    blurb="비웃는 젤리 방패 — 쓰러지면 죽은 척하다가 동료가 회복을 쓰면 일어섭니다. 회복하는 동료부터 치우세요.",
    intents=[
        {"t": "block", "v": 90, "say": "젤리 방벽", "w": 3},
        {"t": "attack", "v": 120, "say": "히죽 박치기", "w": 3},
        {"t": "attack", "v": 145, "say": "깔고 앉기", "w": 2},
    ],
    passives=[P("죽은 척", "death", {"t": "feign", "say": "죽은 척"})],
),
"nururingwarrior_ghost": dict(  # 누루링-유령 전사 — 딜러 · 깜짝 젤리(첫 턴 기습)
    hp=480, tough=3,
    blurb="장난이 심한 젤리 — 첫 턴에 깜짝 튀어나와 크게 칩니다. 그 뒤로는 흐물흐물.",
    open={"t": "attack", "v": 140, "say": "깜짝 젤리!"},
    intents=[
        {"t": "attack", "v": 105, "say": "관통 장난", "w": 3},
        {"t": "multi", "v": 40, "n": 2, "say": "젤리 튕기기", "w": 2},
        {"t": "block", "v": 45, "say": "흐물흐물", "w": 2},
    ],
),
"nururingarcher_ghost": dict(  # 누루링-유령 마법사 — 방해 · 장난 주문: 손 1장 봉쇄
    hp=350, tough=3,
    blurb="장난 주문 젤리 — 파티가 손패를 뽑으면 장난 주문으로 1장을 봉쇄합니다. 미끄러움 · 약화를 겁니다.",
    intents=[
        {"t": "debuff", "id": "미끄러움", "v": 1, "say": "미끌미끌 주문", "w": 2},
        {"t": "debuff", "id": "약화", "v": 1, "say": "흐물 주문", "w": 2},
        {"t": "back", "v": 95, "say": "젤리 화살", "w": 3},
    ],
    passives=[P("장난 주문", "afterDraw", {"t": "cardDebuff", "id": "봉쇄", "n": 1, "to": "hand", "say": "장난 주문"})],
),
"nururingsupporter_ghost": dict(  # 누루링-유령 서포터 — 지원 · 회복이 죽은 척한 동료를 일으킨다
    hp=380, tough=3,
    blurb="동료를 서포트하는 젤리 — 회복으로 죽은 척한 동료를 일으킵니다. 디버프를 받으면 그 틈에 다친 동료를 회복합니다(턴에 한 번).",
    intents=[
        {"t": "heal", "v": 80, "say": "젤리 붕대", "w": 3},
        {"t": "buff", "id": "사기", "v": 1, "say": "힘내라 젤리!", "all": True, "w": 2},
        {"t": "attack", "v": 70, "say": "톡 치기", "w": 2},
    ],
    passives=[P("장난 붕대", "debuffed", {"t": "heal", "v": 45, "say": "장난 붕대"})],
),
}

def _blurb(s):
    def f(e): e['blurb'] = s
    return f
def _elite_fan(e):   # 친위대장 — 동료 격파 대신 동료가 쓰러지면(대가형 피하기)
    e['blurb'] = "엘리트 · 제 팬클럽을 꾸린 극성팬 — 칠 때마다 충격을 남기고 극성팬 편지를 덱에 넣습니다. 동료가 쓰러지면 결집해 사기를 얻습니다."
    for p in e['passives']:
        if p['on'] == 'allyBroken': p['on'] = 'allyDown'
lib.PATCH.update({
    "blanketghost_elite": _blurb("엘리트 · 오래 살아 분위기 파괴에 도가 튼 이불령 — 안절부절을 덱에 심고(4장까지), 쓰러질 때 정신 붕괴 1 을 남깁니다. 짧게 끝내세요."),
    "shadyfollowercloserange_elite": _elite_fan,
    "pumpkin_elite": _blurb("엘리트 · 서늘한 음지에서 자란 불쌍한 호박 — 눈물로 뽑을 더미를 침체시키고, 쓰러지면 2턴 뒤 40% 로 다시 자랍니다."),
    "hatsnail_elite": _blurb("엘리트 · 마녀의 의식을 흉내 내는 햇팽이 — 쪽지를 둘씩 흘리고(4장까지), 디버프를 받으면 모자를 눌러써 실드를 얻습니다. 디버프보다 피해를."),
})

NEW = {
"nependers_ghost": dict(  # 늪 한입초 — 딜러 · 예민함: 맞으면 덥석 되문다(턴에 한 번)
    name="한입초", row="front", nature="광기", hp=450, tough=4,
    blurb="늪지대에서 자란 예민한 한입초 — 맞으면 그 자리에서 덥석 되뭅니다(턴에 한 번). 한 턴에 몰아 끝내세요.",
    intents=[
        {"t": "attack", "v": 110, "say": "덥석", "w": 3},
        {"t": "block", "v": 55, "say": "풀인 척", "w": 2},
        {"t": "attack", "v": 95, "say": "꿀꺽", "w": 2},
    ],
    passives=[P("예민함", "hurt", {"t": "attack", "v": 55, "say": "예민하게 덥석"})],
),
"curseddoll_ghost": dict(  # 저주 인형 — 방해 · 행동할 때마다 파티 균열
    name="누루링 인형", row="back", nature="광기", hp=380, tough=3,
    blurb="버려져 악만 남은 인형 — 행동할 때마다 바늘로 파티에 균열 1 을 새기고, 소음을 덱에 넣습니다.",
    intents=[
        {"t": "addCard", "id": "st_chatter", "n": 1, "to": "draw", "say": "소름 끼치는 소음", "max": 2, "w": 2},
        {"t": "attack", "v": 95, "say": "바늘 꽂기", "w": 3},
        {"t": "debuff", "id": "약화", "v": 1, "say": "버려진 원한", "w": 2},
    ],
    passives=[P("원한의 바늘", "act", {"t": "debuff", "id": "균열", "v": 1, "say": "원한의 바늘"})],
),
}
# 마을 세기 맞춤(03:13 엔진 측정 2026-10-10): ×1.00 35.3% · ×0.93 42.3% → 일반 적 · 새 적의 치는 수 × 0.96.
import os
from lib import scale
_d = float(os.environ.get("FOE_DMG", "0.96"))
scale({k: v for k, v in FOES.items()}, dmg=_d); scale(NEW, dmg=_d)
ART = {"nependers_ghost": "nependers", "curseddoll_ghost": "curseddoll"}

B, F1, F2, PU, HS, NEP, DOLL = "blanketghost", "shadyfollowercloserange", "shadyfollowerlongrange", "pumpkin", "hatsnail", "nependers_ghost", "curseddoll_ghost"
TK, WR, MG, SP = "nururingtanker_ghost", "nururingwarrior_ghost", "nururingarcher_ghost", "nururingsupporter_ghost"
POOLS = [
    {  # 1층 — 유령 늪
        "pools": [
            [[PU, B], [NEP, HS], [B, DOLL], [WR, PU], [HS, B]],
            [[PU, HS], [B, MG, PU], [TK, MG], [NEP, PU], [NEP, SP]],
            [[PU, HS, B], [TK, WR, SP], [NEP, DOLL, PU], [B, SP, TK], [HS, MG, WR]],
        ],
        "elites": [["pumpkin_elite", B], ["hatsnail_elite", MG], ["blanketghost_elite", TK], ["pumpkin_elite", SP]],
    },
    {  # 2층 — 셰이디의 아공간
        "pools": [
            [[F1, F2], [B, HS], [F1, PU], [DOLL, F2], [WR, B]],
            [[F1, F2, DOLL], [HS, F1], [TK, F1, MG], [PU, NEP], [F1, B, SP]],
            [[F1, F2, SP], [HS, F1, MG], [TK, WR, F2], [PU, B, F1], [NEP, F1, SP]],
        ],
        "elites": [["shadyfollowercloserange_elite", F2], ["blanketghost_elite", F1], ["hatsnail_elite", SP], ["shadyfollowercloserange_elite", PU]],
    },
]
DROP = ()

if __name__ == '__main__':
    main('ghost', FOES, POOLS, NEW, DROP)
