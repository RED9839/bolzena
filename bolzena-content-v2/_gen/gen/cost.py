# 대가형 — HP · 다음 턴 AP · 카드를 치르고 큰 한 방. 공용: 파티는 한 몸 — 누가 치르든 「HP가 50% 이하이면」 이 함께 깨어난다.
from lib import *
F = "cost"

# ── 라이카 (순수 · 딜러 · 만드는 쪽 — 제 몸에 전류를 꽂아 HP 를 크게 치른다) ──
h = "라이카"; KW = "번개 자극"
hero(F, h,
 {"name": "라이카", "nature": "순수", "race": "정령", "row": "front", "role": "딜러", "style": "대가형", "star": 3, "hp": 830, "atk": 137, "def": 40, "crit": 10,
  "blurb": "발전소 사고로 번개의 고위 정령이 된 열혈 파이터. 제 몸에 전류를 꽂을수록 주먹이 무거워진다 — 얼마나 꽂을지는 HP 가 정한다."},
 {"name": KW, "desc": "제 몸에 꽂은 전류 — 주먹 한 번에 한 칸씩 빠져나가며 무겁게 한다", "carrier": "self", "cap": 4, "consume": 1,
  "per": [{"stat": "dealt", "v": 0.2}]},
 [P("과충전", "pay", [K(KW, 1)], limit=("turn", 2), who="any"),
  P("의외로 튼튼한 몸", "always", [MOD("dealtMod", 0.2)], conds=[HPLOW()])],
 {"name": "원격 충전!!", "cost": 200, "fx": [D(1.5, "allEnemies"), ST("기절", 1, "oneEnemy"), K(KW, 2)]},
 ["찌릿 펀치", "콰지직 펀치", "정전기 가드"],
 [
  card(h, "u1", "급속 충전!", 1, "스킬", [PAY(80), K(KW, 3), ST("피해 감소", 1)], sig=True,
   blurb="공격 없이 제 몸에 감전을 건다. 220V 가 흐르는 동안은 맞아도 버틴다.",
   oracles=[
    O("220V 직결", [PAY(80), K(KW, 3), ST("피해 감소", 1)], cost=0),
    O("과전압 주의", [PAY(120), K(KW, 4), D(0.3, "randomEnemy", 5)], cost=2),
    O("전열은 맡겨", [PAY(80), K(KW, 2), SH(2.0)]),
    O("충전 완료", [PAY(100), K(KW, 4), ST("피해 감소", 1)]),
    O("번개 펀치", [PAY(80), K(KW, 3), D(0.45, "randomEnemy", 3)]),
   ],
   blesses=[BL("터보 엔진 배터리", "cost"), BL("카페 충전기", tags=["개전"]), BL("자부심의 스파크", fx=[DRAW(1)])]),
  card(h, "u2", "감전 질주", 2, "공격", [PAY(60), D(1.6, "allEnemies"), TOUGH(1, "allEnemies")],
   blurb="감전된 몸으로 내지르는 질주. 전류가 남아 있으면 두 배로 아프다.",
   oracles=[
    O("질주 본능", [PAY(60), D(2.0, "allEnemies"), TOUGH(1, "allEnemies")]),
    O("220V 풀 스로틀", [PAY(100), D(2.6, "allEnemies"), TOUGH(2, "allEnemies")], cost=3),
    O("감전 가속", [PAY(120), D(2.2, "allEnemies"), TOUGH(1, "allEnemies")]),
    O("야구장 정전", [PAY(60), D(1.7, "allEnemies"), ST("약화", 2, "allEnemies")]),
    O("무너진 놈부터", [PAY(60), D(1.8, "allEnemies"), IFB(), AP(2)]),
   ],
   blesses=[BL("효율 만땅 배터리", "power"), BL("220V 콘센트", fx=[K(KW, 1)]), BL("번쩍이는 장갑", "frost")]),
  card(h, "u3", "원투 펀치", 1, "공격", [PAY(40), D(0.65, hits=2), K(KW, 1)],
   blurb="한 칸 쓰고 한 칸 채운다. 전류가 끊기지 않게 잇는 주먹.",
   oracles=[
    O("가벼운 잽", [PAY(40), D(0.5, hits=2), K(KW, 1)], cost=0),
    O("원투 쓰리 포", [PAY(60), D(0.65, hits=4), K(KW, 2)], cost=2),
    O("감전 원투", [PAY(40), D(0.8, hits=2), K(KW, 1)]),
    O("몸으로 막기", [PAY(40), D(0.65, hits=2), B(1.5)]),
    O("충전식 연타", [PAY(80), D(0.55, hits=3), K(KW, 2)]),
   ],
   blesses=[BL("하위 정령 장갑", "power"), BL("정전기", fx=[B(1.0)]), BL("콘센트 사랑", "draw")]),
  card(h, "u4", "발전소 사고", 2, "강화", [ATK(0.15), K(KW, 2), PAY(100)],
   blurb="바람의 하위 정령을 번개로 바꾼 그날. 다시 겪으라면 또 하겠다.",
   oracles=[
    O("사고 그 이후", [ATK(0.15), K(KW, 2), PAY(100)], cost=1),
    O("고위 정령 각성", [ATK(0.2), K(KW, 4), PAY(100)], cost=3),
    O("자가 발전의 꿈", [ATK(0.2), K(KW, 2), PAY(100)]),
    O("번개가 된 날", [ATK(0.15), K(KW, 3), PAY(100)], tags=["개전"]),
    O("220V 만 받는다", [ATK(0.15), K(KW, 3), PAY(60)]),
   ],
   blesses=[BL("실라 선배처럼", "atkUp"), BL("하위 정령 시절의 장갑", fx=[ST("피해 감소", 1)]), BL("카페 콘센트", tags=["개전"])]),
 ])
