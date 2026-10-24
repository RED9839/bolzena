# 적 리뉴얼 시범 — 수인 부락(furry). 2026-10-10.
# 원본 world/villages/furry.json 은 고치지 않는다. 결과: _gen/foes/villages/furry.json(원본 사본에 클론 아닌 적 15 + 판 조합만 바꿈).
#   python furry.py            → _gen/foes/villages/furry.json (적 + 판 조합)
#   python furry.py --foes-only <out.json>  → 적만 바꾼 사본(판 조합은 원본 그대로 — 측정에서 효과를 나눠 보려고)
# 기준: _measure/적_리뉴얼/00_기준.md · 설계 근거: _measure/적_리뉴얼/시범_수인부락.md
import json, sys, copy, os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, '..', '..', '..'))
SRC = os.path.join(ROOT, 'world', 'villages', 'furry.json')
OUT = os.path.join(ROOT, '_gen', 'foes', 'villages', 'furry.json')

def P(name, on, do, **kw):
    d = {"name": name, "on": on, "do": do}; d.update(kw); return d

FOES = {
# ── 일반 ─────────────────────────────────────────────────────────────
"furrywarriorcloserange": dict(  # 수인 광전사 — 딜러 · 타수가 독(多段 불리)
    hp=440, tough=3,
    blurb="쉬지 않고 훈련하는 어린 수인 — 맞을수록 투지가 오릅니다. 여러 번 나눠 치지 말고 큰 한 방으로.",
    intents=[
        {"t": "attack", "v": 115, "say": "주먹 휘두르기", "w": 3},
        {"t": "multi", "v": 50, "n": 2, "say": "연속 발차기", "w": 3},
        {"t": "block", "v": 55, "say": "자세 잡기", "w": 2},
    ],
    counters=[{"name": "투지", "desc": "오기", "onHit": 1, "dealt": 0.1, "max": 3}],
    passives=[P("기세 꺾임", "broken", {"t": "count", "id": "투지", "v": -3, "say": "기세가 꺾임"})],
),
"furrywarriorlongrange": dict(  # 수인 궁수 — 저격 · 시간이 가면 조준, 건드리면 흐트러짐
    hp=360, tough=3,
    blurb="활을 고른 수인 훈련생 — 턴마다 겨냥이 쌓여 관통 화살이 세지고, 맞을 때마다 겨냥이 흐트러집니다.",
    intents=[
        {"t": "back", "v": 105, "say": "관통 화살", "w": 3},
        {"t": "count", "id": "겨냥", "v": 1, "say": "숨 고르기", "w": 2},
        {"t": "block", "v": 45, "say": "엄폐", "w": 2},
    ],
    counters=[{"name": "겨냥", "desc": "급소를 노림", "onTurnStart": 1, "onHit": -1, "dealt": 0.25, "max": 3, "clearOnAttack": True}],
    passives=[],
),
"gluttonbear": dict(  # 머곰 — 벽 · 실드를 깨면 성나서 바로 후려친다
    hp=600, tough=5,
    blurb="음식에 집착하는 곰 수인 — 끌어안은 음식(실드)을 깨면 성나서 바로 후려칩니다. 깰 때는 받아 낼 실드부터.",
    intents=[
        {"t": "block", "v": 85, "say": "음식 끌어안기", "w": 3},
        {"t": "attack", "v": 110, "say": "앞발 후려치기", "w": 2},
        {"t": "multi", "v": 50, "n": 2, "say": "허겁지겁", "w": 2},
        {"t": "attack", "v": 90, "say": "곰 몸통 박치기", "w": 2},
    ],
    counters=[],
    passives=[
        P("먹이 지키기", "guardBreak", {"t": "shift", "say": "먹이를 뺏겼다!", "next": {"t": "attack", "v": 150, "say": "성난 앞발"}}),
        P("주저앉음", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "배고파 주저앉음"}),
    ],
),
"foodscavenger": dict(  # 라쿤아치 — 방해 · 사료를 뿌리고, 쓰러진 동료 몫까지 먹는다
    hp=340, tough=3,
    blurb="사료에 중독된 라쿤 수인 — 사료 한 줌을 덱에 뿌리고, 동료가 쓰러지면 그 몫을 주워 먹어 회복하고 사나워집니다.",
    intents=[
        {"t": "attack", "v": 120, "say": "앞발 할퀴기", "w": 3},
        {"t": "addCard", "id": "st_kibble", "n": 1, "to": "draw", "say": "사료 뿌리기", "max": 2, "w": 2},
        {"t": "multi", "v": 40, "n": 3, "say": "허겁지겁 물기", "w": 2},
    ],
    counters=[],
    passives=[P("주워 먹기", "allyDown", {"t": "selfHeal", "v": 90, "say": "주워 먹기"}),
              P("배부름", "allyDown", {"t": "buff", "id": "사기", "v": 1, "say": "배부르다"})],
),
"furring": dict(  # 퍼리 — 지원 · 지갑이 털리면 기절(多段 유리)
    hp=400, tough=3,
    blurb="갑자기 부자가 된 수인 — 동료를 회복하고 사기를 올립니다. 한 턴에 세 번 맞으면 지갑이 털려 기절합니다.",
    intents=[
        {"t": "heal", "v": 75, "say": "한턱 쏘기", "w": 3},
        {"t": "buff", "id": "사기", "v": 1, "say": "오늘은 내가 쏜다!", "all": True, "w": 2},
        {"t": "attack", "v": 75, "say": "지갑으로 치기", "w": 2},
    ],
    counters=[{"name": "지갑", "desc": "두둑한 지갑", "start": 3, "max": 3, "onHit": -1, "resetTurnStart": True, "stunAtZero": True}],
    passives=[],
),
"curseddoll": dict(  # 누루링 인형 — 방해 · 쓰러질 때 원한을 남긴다(유령 늪에도 나온다)
    hp=380, tough=3,
    blurb="버려진 인형에 깃든 마녀의 마법 — 소음을 덱에 넣고, 쓰러질 때 원한의 바늘 1장을 덱에 남깁니다.",
    intents=[
        {"t": "addCard", "id": "st_chatter", "n": 1, "to": "draw", "say": "소름 끼치는 소음", "max": 2, "w": 2},
        {"t": "attack", "v": 100, "say": "바늘 꽂기", "w": 3},
        {"t": "debuff", "id": "약화", "v": 1, "say": "버려진 원한", "w": 2},
    ],
    counters=[],
    passives=[P("버려진 원한", "death", {"t": "addCard", "id": "st_needle", "n": 1, "to": "draw", "say": "원한의 바늘"})],
),
"nependers": dict(  # 한입초 — 방해 · 손패를 삼킨다, 격파 · 때려서 뱉게(모나티엄 · 유령 늪 · 정령산에도 나온다)
    hp=460, tough=4,
    blurb="풀인 척 사냥하는 식물 — 카드 1장을 통째로 삼킵니다. 격파하거나 HP 15% 만큼 때리면 뱉어 냅니다.",
    intents=[
        {"t": "seize", "n": 1, "v": 15, "say": "꿀꺽 삼키기", "w": 2},
        {"t": "attack", "v": 110, "say": "덥석", "w": 3},
        {"t": "block", "v": 60, "say": "풀인 척", "w": 2},
    ],
    counters=[],
    passives=[],
),
"nururingtanker_furry": dict(  # 누루링-수인 탱커 — 벽 · 격파 전엔 털 갑옷, 격파하면 벗겨진다
    hp=650, tough=5,
    blurb="털 난 젤리 방패 — 털 갑옷으로 받는 피해가 줄어듭니다. 격파하면 갑옷이 벗겨져 오히려 취약해집니다.",
    intents=[
        {"t": "guard", "v": 50, "say": "털 뭉치 방벽", "w": 3},
        {"t": "attack", "v": 130, "say": "깔고 앉기", "w": 2},
        {"t": "attack", "v": 100, "say": "털 박치기", "w": 3},
    ],
    counters=[{"name": "털 갑옷", "desc": "부푼 털", "start": 1, "max": 1, "taken": -0.3}],
    passives=[
        P("갑옷 벗겨짐", "broken", {"t": "count", "id": "털 갑옷", "v": -1, "say": "털이 가라앉음"}),
        P("맨 젤리", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "맨 젤리"}),
        P("다시 부풂", "recover", {"t": "count", "id": "털 갑옷", "v": 1, "say": "털이 다시 부풂"}),
    ],
),
"nururingwarrior_furry": dict(  # 누루링-수인 전사 — 딜러 · 행동할수록 신난다
    hp=440, tough=3,
    blurb="당돌하게 앞장서는 젤리 — 행동할 때마다 신바람이 올라 세집니다. 격파하면 신바람이 꺼집니다.",
    intents=[
        {"t": "attack", "v": 95, "say": "젤리 펀치", "w": 3},
        {"t": "multi", "v": 40, "n": 3, "say": "신나는 연타", "w": 2},
        {"t": "block", "v": 50, "say": "통통 튀기", "w": 2},
    ],
    counters=[{"name": "신바람", "desc": "들뜬 기분", "dealt": 0.15, "max": 4}],
    passives=[P("신남", "act", {"t": "count", "id": "신바람", "v": 1, "say": "신난다!"}),
              P("김빠짐", "broken", {"t": "count", "id": "신바람", "v": -4, "say": "김이 빠짐"})],
),
"nururingarcher_furry": dict(  # 누루링-수인 마법사 — 저격 · 공격이 아닌 카드에 반응해 마탄
    hp=320, tough=3,
    blurb="이상한 음식을 먹고 마법에 눈뜬 젤리 — 공격이 아닌 카드를 3장 내면 바로 관통 마탄을 쏩니다.",
    intents=[
        {"t": "back", "v": 85, "say": "젤리 마탄", "w": 3},
        {"t": "debuff", "id": "약화", "v": 1, "say": "으르렁", "w": 2},
        {"t": "attack", "v": 70, "say": "젤리 몸통 치기", "w": 2},
    ],
    counters=[{"name": "이상한 마력", "desc": "들끓는 마력", "onCard": 1, "cardType": "!공격", "max": 3, "at": 3,
               "act": {"t": "back", "v": 55, "say": "폭주 마탄"}}],
    passives=[],
),
"nururingsupporter_furry": dict(  # 누루링-수인 서포터 — 지원 · 회복 · 쓰러지면 동료가 성낸다
    hp=340, tough=3,
    blurb="낚싯대로 동료를 이끄는 젤리 — 회복과 불굴을 두릅니다. 쓰러지면 남은 동료 전체가 사기를 얻습니다.",
    intents=[
        {"t": "heal", "v": 80, "say": "털 고르기", "w": 3},
        {"t": "buff", "id": "불굴", "v": 1, "say": "뭉치자 젤리!", "all": True, "w": 2},
        {"t": "attack", "v": 65, "say": "낚싯대 휘두르기", "w": 2},
    ],
    counters=[],
    passives=[P("길잡이를 잃다", "death", {"t": "buff", "id": "사기", "v": 1, "all": True, "say": "길잡이를 잃은 분노"})],
),
# ── 엘리트 ───────────────────────────────────────────────────────────
"furrywarriorcloserange_elite": dict(  # 만년 수련생 — 같은 사도 연타 금지 + 투지
    hp=780, tough=6,
    blurb="엘리트 · 졸업을 미룬 만년 수련생 — 같은 사도의 카드를 잇달아 내면 간파해 연격으로 받아칩니다. 맞을수록 투지도 오릅니다.",
    intents=[
        {"t": "attack", "v": 170, "say": "정권 지르기", "w": 3},
        {"t": "multi", "v": 55, "n": 3, "say": "수련 연격", "w": 2},
        {"t": "block", "v": 95, "say": "철벽 자세", "w": 2},
        {"t": "jam", "v": 1, "say": "기합", "w": 2},
        {"t": "attack", "v": 195, "say": "천 번째 지르기", "w": 2},
    ],
    counters=[{"name": "투지", "desc": "오기", "onHit": 1, "dealt": 0.06, "max": 3}],
    passives=[P("간파", "card", {"t": "multi", "v": 40, "n": 2, "say": "간파 — 받아치기"}, same=True),
              P("자세 무너짐", "broken", {"t": "count", "id": "투지", "v": -3, "say": "자세가 무너짐"})],
),
"gluttonbear_elite": dict(  # 머곰 두목 — 부하를 부르고, 부하가 쓰러지면 성낸다
    hp=650, tough=7,
    blurb="엘리트 · 머곰 무리를 거느린 두목 — 부하 머곰을 부르고, 동료가 쓰러질 때마다 사기를 얻습니다. 격파하면 체면이 구겨져 취약 3.",
    intents=[
        {"t": "summon", "id": "gluttonbear", "n": 1, "max": 1, "say": "부하 부르기", "w": 2},
        {"t": "attack", "v": 125, "say": "두목의 앞발", "w": 3},
        {"t": "multi", "v": 50, "n": 3, "say": "할퀴기", "w": 2},
        {"t": "block", "v": 90, "say": "두목의 배짱", "w": 2},
        {"t": "attackAll", "v": 50, "say": "두목의 내려찍기", "w": 2},
    ],
    counters=[],
    passives=[P("두목의 호령", "allyDown", {"t": "buff", "id": "사기", "v": 1, "say": "두목의 호령"}),
              P("체면 구김", "broken", {"t": "buff", "id": "취약", "v": 3, "say": "두목 체면 구김"})],
),
"foodscavenger_elite": dict(  # 라쿤아치 미식가 — 디버프를 뱉는다 + 안절부절
    hp=820, tough=6,
    blurb="엘리트 · 비싼 사료만 먹는 미식가 — 안절부절을 덱에 넣고(4장까지), 디버프를 받으면 뱉어 내고 회복합니다.",
    intents=[
        {"t": "attack", "v": 155, "say": "미식 평가", "w": 3},
        {"t": "addCard", "id": "st_jitters", "n": 2, "to": "draw", "say": "까다로운 눈초리", "max": 4, "w": 2},
        {"t": "multi", "v": 60, "n": 2, "say": "포크질", "w": 3},
        {"t": "block", "v": 85, "say": "냅킨 두르기", "w": 2},
        {"t": "multi", "v": 30, "n": 4, "say": "풀코스", "w": 2},
    ],
    counters=[],
    passives=[P("편식", "debuffed", {"t": "selfHeal", "v": 60, "say": "싫은 건 뱉기"}),
              P("식탁 엎어짐", "broken", {"t": "buff", "id": "취약", "v": 2, "say": "식탁이 엎어짐"})],
),
"furring_elite": dict(  # 퍼리 몰락한 큰손 — 재기 의지 + 절반에서 거대화 + 압류 딱지
    hp=620, tough=6,
    blurb="엘리트 · 투자에 실패한 큰손 — 턴마다 재기 의지가 쌓이고, HP 절반에서 거대화합니다. 카드에 압류 딱지(봉쇄)를 붙입니다. 격파하면 재기 의지가 꺾입니다.",
    intents=[
        {"t": "attack", "v": 100, "say": "큰손의 주먹", "w": 3},
        {"t": "multi", "v": 40, "n": 3, "say": "돈다발 난타", "w": 2},
        {"t": "cardDebuff", "id": "봉쇄", "n": 2, "to": "draw", "say": "압류 딱지", "w": 2},
        {"t": "block", "v": 85, "say": "금고 끌어안기", "w": 2},
    ],
    phase={"at": 0.5, "say": "거대화", "intents": [
        {"t": "attack", "v": 150, "say": "거대 큰손", "w": 3},
        {"t": "multi", "v": 45, "n": 3, "say": "분풀이", "w": 2},
        {"t": "attackAll", "v": 55, "say": "돈다발 폭탄", "w": 2},
        {"t": "block", "v": 120, "say": "거대한 금고", "w": 2},
    ]},
    counters=[{"name": "재기 의지", "desc": "이 악물기", "onTurnStart": 1, "dealt": 0.12, "max": 3}],
    passives=[P("주저앉음", "broken", {"t": "count", "id": "재기 의지", "v": -3, "say": "털썩 주저앉음"})],
),
}

# 판 조합 — 묶음마다 방해(라쿤 · 한입초 · 인형) 하나 · 저격(궁수 · 마법사) 하나 · 지원(퍼리 · 서포터) 하나까지.
# 라쿤(동료가 쓰러지면 먹음)과 서포터(쓰러지면 동료 사기)는 한 묶음에 두지 않는다. 2층 엘리트는 엘리트 + 하나(두목은 부하를 부른다).
A, B = "furrywarriorcloserange", "furrywarriorlongrange"
BEAR, RAC, FUR, DOLL, NEP = "gluttonbear", "foodscavenger", "furring", "curseddoll", "nependers"
TK, WR, MG, SP = "nururingtanker_furry", "nururingwarrior_furry", "nururingarcher_furry", "nururingsupporter_furry"
POOLS = [
    {  # 1층 — 수련생 숲
        "pools": [
            [[A, B], [BEAR, MG], [RAC, WR], [TK, A], [NEP, B]],                                  # 약 — 둘 · 시너지 없음
            [[FUR, A], [BEAR, B], [NEP, WR], [DOLL, A], [TK, MG]],                               # 중 — 둘 · 누구부터 하나
            [[BEAR, A, FUR], [TK, WR, SP], [RAC, BEAR, MG], [DOLL, A, B], [NEP, TK, B]],         # 강 — 셋 · 시너지 하나
        ],
        "elites": [[A + "_elite", B], [BEAR + "_elite", WR], [RAC + "_elite", MG], [A + "_elite", SP]],
    },
    {  # 2층 — 털 부락
        "pools": [
            [[RAC, FUR], [A, DOLL], [BEAR, B], [NEP, A], [WR, FUR]],
            [[RAC, FUR, A], [BEAR, DOLL, B], [TK, MG], [NEP, FUR, A], [BEAR, SP]],
            [[BEAR, RAC, FUR], [A, B, DOLL], [TK, WR, FUR], [NEP, BEAR, MG], [BEAR, A, MG]],
        ],
        "elites": [[FUR + "_elite", A], [BEAR + "_elite", B], [RAC + "_elite", A], [FUR + "_elite", B]],
    },
]

def build(foes_only=False):
    d = json.load(open(SRC, encoding='utf-8'))
    for e in d['enemies']:
        if 'clone' in e or e['id'] not in FOES: continue
        f = FOES[e['id']]
        keep = {k: e[k] for k in ('id', 'name', 'row', 'nature', 'weak') if k in e}
        for k in list(e.keys()): del e[k]
        e.update(keep)
        e['hp'] = f['hp']; e['tough'] = f['tough']; e['pick'] = 'shuffle'; e['blurb'] = f['blurb']; e['intents'] = f['intents']
        if f.get('phase'): e['phase'] = f['phase']
        if f.get('counters'): e['counters'] = f['counters']
        if f.get('passives'): e['passives'] = f['passives']
    if not foes_only:
        for fl, p in zip(d['villages'][0]['floors'], POOLS):
            fl['pools'] = p['pools']; fl['elites'] = p['elites']
    return d

if __name__ == '__main__':
    if len(sys.argv) > 2 and sys.argv[1] == '--foes-only':
        out = sys.argv[2]; d = build(True)
    else:
        out = OUT; d = build(False)
    import lib; lib.balance(d)   # 밸런스 손질(balance.json, 2026-10-10)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    json.dump(d, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('→', out)
