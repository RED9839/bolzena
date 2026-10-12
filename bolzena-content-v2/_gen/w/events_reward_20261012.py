# 이벤트 보상 손질 (2026-10-12 밤, 사용자 결정 넷) — _measure/이벤트_보상_손질_20261012.md
#  1. 덱 보상 위주(은총 · 교주 카드 · 신탁 · 축복 · 제거 · 복제 · 장비), HP · 골드 부스러기는 빼거나 값을 크게
#  2. 좋은 보상엔 분명한 대가, 공짜 선택지는 작게
#  3. 판정: 지금 사도 공격력은 95 · 115 · 140 뿐 — 145 이상 판정은 아무도 못 넘었고 140 이하는 사람 판 95%가 넘었다.
#     1층 공격력 판정은 140 으로 두고(거의 성공 — 성공에 대가), 2층 공격력 판정 6개는 HP 75% 판정으로. HP 판정은 1층 70% · 2층 75%.
#     화면이 성공/실패를 미리 보여 주므로 성공에도 대가(HP)를 붙이고, 실패 쪽은 「작은 보상 + 큰 대가」 로 고를 수는 있는 칸으로.
#  4. 안 고르는 보상: 지도 공개 · 상점 선물은 걷어 냄. 정신 붕괴는 엔진에서 「다음 전투 매 턴 망령의 속삭임」 으로 다시 만들고 유령 늪 세 곳에만 큰 보상의 대가로
# 원본: C:/projects/_backup/events_reward_20261012/ (손질 전) → world/events/ 에 쓴다. 다시 돌려도 같은 결과.
import json, os, sys

SRC = "C:/projects/_backup/events_reward_20261012"
DST = "C:/projects/bolzena-content-v2/world/events"
FILES = ["공용.json", "모나티엄.json", "수인마을.json", "에르피엔.json", "용족터.json", "유령늪.json", "정령산.json"]


def hp(v): return {"k": "hp", "v": v}
def gold(v): return {"k": "gold", "v": v}
def maxhp(v): return {"k": "maxHp", "v": v}
def uniq(): return {"k": "unique"}
def flash(): return {"k": "flash"}
def shinpick(): return {"k": "shinPick", "n": 1}
def shin(v): return {"k": "shin", "v": v}
def dupe(n=1): return {"k": "dupe", "n": n}
def remove(n=1): return {"k": "remove", "n": n}
def neutral(g): return {"k": "neutral", "grade": g}
def equip(g, slot=None): return {"k": "equip", "grade": g, **({"slot": slot} if slot else {})}
def curse(i): return {"k": "curse", "id": i}
def gift(i): return {"k": "gift", "id": i}
def flag(i): return {"k": "flag", "id": i}
def nxt(**kw): return {"k": "next", "next": kw}
def mind(n=1): return {"k": "mindBreak", "n": n}   # 정신 붕괴(10-12 다시 만듦) — 다음 전투 n번, 매 턴 손에 「망령의 속삭임」


# (이벤트, 선택지 번호, 지금 이름) → 바꿀 칸. judge 는 at · pass · fail · passSay 만 덮어쓴다. gamble 은 {번호: {out, say}}.
P = {}
def ch(eid, i, label, **kw): P[(eid, i)] = (label, kw)


# ── 공용 ──
ch("G04", 0, "카드끼리 붙이기", out=[flash(), remove(1), hp(-0.1)],
   say="아네트가 카드 두 장을 마주 세웁니다. \"자, 붙어!\" 이긴 카드에는 신탁이 붙고, 진 카드는 덱에서 빠집니다. 튄 불똥에 손등이 얼얼합니다.")
ch("G04", 1, "맨 위 칸에 서기", judge=dict(at=140, pass_=[uniq(), hp(-0.1)], fail=[flash(), hp(-0.18)],
   passSay="\"…흥, 허접치고는 제법이네.\" 아네트가 메달을 꺼내다 말고 도로 넣더니, 대신 제 카드 묶음에서 한 장을 던져 줍니다. 맞은 데가 아직 욱신거립니다."),
   say="\"허접.\" 아네트에게 호되게 졌습니다. 그래도 맞으면서 본 수 하나가 카드에 남았습니다.")
ch("G04", 2, "기초부터 다지기", needGold=50, out=[gold(-50), maxhp(80)])
ch("G06", 0, "배달 일 거들기", out=[hp(-0.1), equip("고급")],
   say="상자를 다 나르고 나니 시스트가 수레 밑에서 아티팩트 하나를 꺼내 줍니다. \"사장님, 손이 빠르시네. 이건 품삯이에요.\"")
ch("G06", 3, "가짜 뿔 알아보기", out=[neutral("희귀")])
ch("G09", 0, "서랍 비우기", out=[remove(1), hp(-0.08)],
   say="버릴 카드를 골라 자루에 담았습니다. 무거운 자루를 몇 번이나 날랐더니 허리가 뻐근합니다.")
ch("G12", 1, "배양 시술 받기", out=[maxhp(-60), dupe(1)])
ch("G12", 2, "임상 시험 자원하기", judge=dict(at=70, pass_=[flash(), hp(-0.08)], fail=[gold(50), hp(-0.15)],
   passSay="\"소견: 매우 건강함. 좋은 자료가 됐어요.\" 힐데가 시약 한 방울을 카드에 떨어뜨려 줍니다. 주사 자국이 아직 따끔합니다."),
   say="\"소견: 휴식이 필요함.\" 시험은 중간에 멈췄습니다. 힐데가 사례금만 내주고 침대로 돌려보냅니다.")
ch("B1", 0, "천막 그늘에서 참관하기", out=[hp(0.2)])
ch("B1", 2, "시연 조수로 서기", judge=dict(at=70, pass_=[equip("희귀"), hp(-0.1)], fail=[gold(50), hp(-0.18)],
   passSay="실험 마법을 끝까지 버텨 냈습니다. \"조수, 끝까지 버팀. 사례 지급함.\" 머리카락 끝이 조금 그을렸습니다."),
   say="실험 마법에 먼저 쓰러졌습니다. \"조수, 중도 탈락. 사례 절반 지급함.\"")

# ── 모나티엄 ──
ch("M1", 0, "하루 일하기", out=[hp(-0.1), gold(100)])
ch("M2", 1, "발명품 시험에 응하기", gamble={1: dict(out=[hp(-0.15)], say="펑. 시제품이 터졌습니다. 다 같이 그을렸고, 시제품이 뭘 했는지는 아무도 모릅니다.")})
ch("M3", 0, "정식 통행증 내기", out=[gold(-40), neutral("고급")],
   say="「통행 허가.」 통행증과 함께 시민 안내 카드 묶음이 나옵니다. 쓸 만한 한 장이 끼어 있습니다.")
ch("M3", 3, "엘프답게 통과하기", out=[equip("희귀")], say="드론이 고개를 숙였습니다. 「시민 확인. 협력 상점 물품을 지급합니다.」")
ch("M5", 2, "작은 글씨까지 읽기", out=[remove(1), nxt(ap=-1)],
   say="눈이 빠지게 읽다가 위약금 조항을 찾아냈습니다. \"어머, 거기까지 읽으셨어요? 그 줄은 빼 드릴게요♪\" 멜루나가 웃으며 조항 한 줄을 지워 줍니다. 눈이 아파 한동안 앞이 흐릿합니다.")
ch("M4", 2, "적성 감별기에 서기", judge=dict(at=75, pass_=[uniq(), gift("gift_badge"), hp(-0.1)], fail=[flash(), curse("curse_paperwork")],
   passSay="「협동 적성 우수.」 판정서와 함께 맞춤 교육 과정 안내가 출력됐습니다. 감별기 전류가 꽤 셌습니다."),
   say="「적성 재검 필요.」 재검 서류가 한 묶음 나왔습니다. 그사이 옆 창구 교육 영상에서 한 수는 건졌습니다.")
ch("M6", 0, "방패 대열에 서기", judge=dict(at=75, pass_=[uniq(), hp(-0.1)], fail=[flash(), hp(-0.18)],
   passSay="\"버텼슴다! 합격임다!\" 알레트가 기합을 넣습니다. 방패를 받친 팔이 저릿합니다."),
   say="대열이 무너졌습니다. \"괜찮슴다! 무너지는 법도 훈련임다!\" 실컷 깔리고 나서야 대열 감각 하나가 손에 남았습니다.")
ch("M9", 0, "펀칭머신 기록 겨루기", judge=dict(by="hp-party", at=75, pass_=[equip("전설", "무기"), hp(-0.1)], fail=[equip("고급", "무기"), hp(-0.15)],
   passSay="\"오, 이건 진압반 입단 기록이다!\" 칸나가 맨 위 칸 경품을 넘겨줍니다. 주먹이 얼얼합니다."),
   say="기계가 시큰둥한 숫자를 띄웁니다. 칸나가 웃으며 참가상을 챙겨 줍니다. 손목이 시큰합니다.")

# ── 수인 마을 ──
ch("F1", 0, "꿀단지 들고 튀기", judge=dict(at=140, pass_=[uniq(), gift("gift_honey"), hp(-0.12)], fail=[gift("gift_honey"), hp(-0.2)],
   passSay="머곰이 깨기 전에 숲 밖으로 빠져나왔습니다. 단지 바닥에 누가 적어 둔 비법 쪽지가 붙어 있습니다. 나뭇가지에 긁힌 자국이 쓰라립니다."),
   say="머곰이 눈을 떴습니다. 단지는 끝까지 안고 왔지만, 앞발에 몇 번이나 얻어맞았습니다.")
ch("F1", 1, "죽은 척하기", gamble={0: dict(out=[hp(0.3)]), 1: dict(out=[hp(-0.12)])})
ch("F3", 0, "사료 받아먹기", out=[hp(0.3), nxt(weak=1)],
   say="고소합니다. 배가 든든해지더니 이내 머리가 핑 돕니다. 사료에 하얀 가루가 섞여 있었습니다.")
ch("F3", 1, "팻말 부수고 지나가기", judge=dict(at=140, pass_=[flash(), hp(-0.08), flag("furry_sas_sign")], fail=[gold(40), hp(-0.15), flag("furry_sas_sign")],
   passSay="팻말이 두 동강 났습니다. \"베니 팻말…!\" 곰 수인이 울상이 되어 숲 안쪽으로 물러납니다. 팻말 뒤에 붙어 있던 부적 한 장이 손에 남았습니다."),
   say="팻말은 생각보다 단단했습니다. 곰 수인에게 떠밀려 나뒹구는 사이 팻말 밑 모금함이 굴러왔습니다.")
ch("F8", 1, "편지 몇 통 뜯어 보기", gamble={0: dict(out=[neutral("희귀"), gold(40)], say="편지 사이에 용돈과 교주 카드 한 장이 끼어 있었습니다. 받는 놈이 임자라고들 합니다.")})
ch("F8", 2, "묻어 둔 간식 찾아 주기", gamble={0: dict(out=[hp(0.3)]), 1: dict(out=[hp(0.15), curse("curse_spore")])})
ch("G08", 1, "분신 둘 만들기", out=[dupe(2), hp(-0.15), maxhp(-60)],
   say="분신이 셋, 넷… 누가 진짜인지 헷갈립니다. 기운이 분신 쪽으로 빠져나가 몸이 한동안 돌아오지 않습니다.")
ch("F5", 0, "티그와 겨루기", judge=dict(by="hp-party", at=75, pass_=[uniq(), gift("gift_scroll"), hp(-0.12)], fail=[flash(), hp(-0.2)],
   passSay="\"…쳇, 오늘은 봐준 거다!\" 티그가 비전서 사본을 던지고 갑니다. 칼등에 맞은 어깨가 아직 얼얼합니다."),
   say="\"허접!\" 티그의 칼등이 먼저 닿았습니다. 실컷 구른 끝에 칼끝 한 수는 눈에 남았습니다.")
ch("F5", 1, "구경꾼 내기에 끼기", gamble={0: dict(out=[gold(120)])})
ch("F6", 0, "같이 장작 패기", out=[hp(-0.1), gold(120)])
ch("F6", 2, "쵸피와 겨루기", judge=dict(by="hp-party", at=75, pass_=[dupe(1), hp(-0.1)], fail=[gold(60), hp(-0.18)],
   passSay="쵸피가 울먹이며 도끼를 내려놓습니다. \"다음엔 꼭 이길 거예요!\" 이긴 쪽이 스승이라더니, 쵸피가 방금 그 수를 몇 번이고 따라 해 보이는 사이 손에 완전히 익었습니다."),
   say="쵸피의 도끼질이 더 빨랐습니다. \"괘, 괜찮으세요?\" 쵸피가 미안해하며 모아 둔 품삯을 나눠 줍니다.")
ch("F7", 0, "고장 난 기계 팔기", out=[gold(130), nxt(ap=-1)])
ch("F9", 1, "어떤 수인인지 묻기", out=[uniq(), shinpick(), curse("curse_grudge")],
   say="마루가 조용해집니다. 디아나가 웃으며 군밤을 까 주더니, 옛 비급 한 장을 슬쩍 밀어 줍니다. 웃는 얼굴이 조금 무섭습니다. 그 뒤로 마을 어른들 눈길이 따갑습니다.")

# ── 에르피엔 ──
ch("E3", 1, "사탕수수 얻어먹기", out=[gold(-30), hp(0.25)])
ch("E3", 2, "거대 사탕수수 베기", judge=dict(at=140, pass_=[uniq(), hp(-0.08), flag("erpien_cane")], fail=[gold(40), hp(-0.15), flag("erpien_cane")]),
   say="사탕수수가 휘청이다 도로 튕겨 왔습니다. 캬롯이 깔깔 웃으며 부러진 토막을 들려 보냅니다. \"그래도 이건 가져가. 왕도 빵집에서 값 쳐 줄걸.\"")
ch("E4", 1, "케이크 나눠 받기", out=[gold(-20), hp(0.25)])
ch("E8", 2, "공정하게 재기", judge=dict(at=70, pass_=[shinpick(), hp(-0.08)], fail=[gold(40), hp(-0.12)],
   passSay="무승부였습니다. 둘 다 내년을 기약하며, 밭 두 개를 다 재 준 심판에게 밭의 축복을 하나씩 얹어 줍니다. 허리가 뻐근합니다."),
   say="밭을 재다 지쳐 쓰러졌습니다. 둘이 서로 자기 밭이 컸다며 참가비만 돌려줍니다.")
ch("E9", 1, "괜찮다고 다독이기", needGold=30, out=[gold(-30), shinpick()])
ch("E1", 2, "수강 신청하기", out=[gold(-80), uniq()])
ch("E1", 3, "전단지 돌려 주기", out=[gold(150), curse("curse_chores")])
ch("E2", 0, "서류 정리 거들기", out=[hp(0.3), nxt(ap=-1)])
ch("E5", 0, "경비대와 힘 겨루기", judge=dict(by="hp-party", at=75, pass_=[equip("희귀", "방어구"), hp(-0.1)], fail=[gold(60), hp(-0.18)],
   passSay="창이 비켜섰습니다. \"…통과! 반란군은 아닌 걸로!\" 폴랑이 제 미숙함을 짚으며 경비대 방패 하나를 건넵니다. 창대에 맞은 정강이가 욱신거립니다."),
   say="창끝에 밀려 엉덩방아를 찧었습니다. \"반란군은 아닌 걸로!\" 폴랑이 미안해하며 통행 사례를 쥐여 줍니다.")
ch("E5", 1, "통행료 내기", out=[gold(-50), hp(0.25)])
ch("E5", 2, "소방대장 이름 대기", out=[hp(0.15), flash(), nxt(rush=2, hand=1)],
   say="\"아, 동굴 제단 물 퍼 주신 분들! 샤샤 대장이 몇 번이나 말했습니다.\" 경비대가 통행료 대신 순찰 시간표와 지름길을 알려 줍니다. 샤샤 대장이 맡겨 뒀다는 세계수 잎도 건넵니다 — 카드에 대자 빛이 스밉니다.")
ch("E11", 0, "짐 하나 맡기기", out=[remove(1)], say="\"이건 받아 줌. 돌려주진 않음.\" 마요가 짐을 진열장 뒤로 치웁니다.")
ch("E12", 1, "한판 돌리기", gamble={0: dict(out=[gold(150)])})

# ── 용족 터 ──
ch("D1", 1, "둥지 고쳐 주기", out=[hp(-0.08), maxhp(80)])
ch("D7", 0, "짐꾼으로 쓰기", out=[gold(-30), hp(0.25)],
   say="피라가 짐을 번쩍 들고 앞장섭니다. 품삯을 받으며 진짜 금이 맞는지 세 번이나 깨물어 봅니다. 짐을 덜고 나니 숨이 트입니다.")
ch("D9", 0, "살짝 쳐 보기", judge=dict(at=70, pass_=[equip("희귀", "방어구"), hp(-0.1)], fail=[gold(50), hp(-0.2)],
   passSay="\"덤벼 봐!\" 리츠가 폭발했다가 금세 정신을 차리고, 무승부라며 사과하고 갑옷 조각을 건넵니다. 폭발에 휩쓸린 자리가 쓰라립니다."),
   say="리츠가 폭발했습니다. 정신을 차린 리츠가 연신 사과하며 치료비를 쥐여 줍니다.")
ch("D9", 1, "갑옷 구경하기", needGold=40, out=[gold(-40), maxhp(80)])
ch("D2", 0, "헬스로 겨루기", judge=dict(by="hp-party", at=75, pass_=[gift("gift_dumbbell"), maxhp(80), hp(-0.1)], fail=[maxhp(40), hp(-0.2)],
   passSay="\"좋은 근육이다!\" 루드가 아령을 건넵니다. 온몸이 쑤시지만 그만큼 단단해졌습니다."),
   say="\"근손실이다!\" 루드가 고개를 젓습니다. 온몸이 쑤시는데, 그래도 조금은 단단해졌습니다.")
ch("D4", 4, "수장의 손님으로 가기", out=[equip("희귀"), gift("gift_diamond")],
   say="\"다이아 브레…츄! …흠, 손님 대접을 하마.\" 다야가 재채기를 수습하고 직접 안내하더니, 진열장 하나를 손님 몫으로 열어 줍니다.")
ch("D6", 0, "실피르와 몸풀기 대련하기", judge=dict(by="hp-party", at=75, pass_=[equip("희귀", "무기"), shinpick(), hp(-0.1)], fail=[flash(), hp(-0.2)],
   passSay="몸이 제대로 풀린 실피르가 한 칸 끼어들었습니다. 들뜬 얼굴로 단검 하나를 던져 줍니다. \"정정당당하게 딴 자리다!\" 몸풀기치고는 꽤 아팠습니다."),
   say="몸풀기라더니 실피르는 진심이었습니다. 한참 얻어맞고 나서야 서열전 칼놀림 하나가 눈에 남았습니다.")
ch("D11", 1, "결투 받기", judge=dict(at=75, pass_=[uniq(), shinpick(), hp(-0.12)], fail=[flash(), hp(-0.2)],
   passSay="\"…훌륭하옵니다.\" 실비아가 장갑을 다시 끼며 예를 갖춥니다. 찻잔이 쏟아질 만큼 거친 결투였습니다."),
   say="\"아직이옵니다.\" 실비아의 장갑이 먼저 닿았습니다. 쓰러진 채로 본 마지막 일격이 눈에 남았습니다.")

# ── 유령 늪 ──
ch("G02", 0, "카드 손질 돕기", out=[maxhp(50)])
ch("H1", 2, "웃는 호박 걷어차기", fight_win=[gift("gift_pumpkin"), equip("고급")])
ch("H1", 3, "늪의 지름길 타기", out=[nxt(rush=3, foeVuln=1), neutral("고급")],
   say="안개 사이로 유령만 아는 길이 열립니다. 길가 무덤가에서 반짝이는 것을 하나 주웠습니다.")
ch("H2", 1, "이불 확 걷기", gamble={2: dict(out=[hp(-0.15), curse("curse_haunt")], say="잠을 깬 이불령이 화가 나 등에 달라붙었습니다. 떼어 낼 수가 없습니다.")})
# 정신 붕괴는 유령 늪에서만 — 큰 덱 보상의 대가(사용자 10-12: 「유령 늪 효과로 다시 만든다」)
ch("H2", 2, "꿈 엿보기", out=[flash(), shinpick(), mind(1)],
   say="이불 귀퉁이를 살짝 들추자 이불령의 꿈이 새어 나옵니다. 꿈속 장면 하나는 카드에 스며들고, 다른 하나는 축복이 되어 내려앉습니다. 대신 꿈속 목소리가 한동안 귓가를 떠나지 않습니다.")
ch("H8", 3, "카드 보여 주기", out=[uniq(), flash(), mind(1)],
   say="\"이건 뭐야? 왜 이렇게 생겼어?\" 벨라가 카드 한 장을 뚫어지게 봅니다. 그 안에 잠든 힘 하나가 깨어나고, 벨라가 제 카드 한 장까지 내밉니다. 대신 질문 공세가 다음 전투까지 귓가에 맴돕니다.")
ch("H5", 3, "문 닫는 걸 돕기", out=[remove(2), mind(1)],
   say="문을 밀어 닫다가 덱 두 장이 문틈으로 빨려 들어갔습니다. 림이 \"악당은 고맙다는 말 안 합니다.\" 하면서도 고개를 꾸벅 숙입니다. 닫힌 문 너머에서 누군가 계속 속삭입니다.")
ch("H7", 0, "눈 둘 곳 찾아 헤매기", out=[gold(50)])
ch("H5", 0, "문에 손 넣기", gamble={2: dict(out=[hp(-0.15), curse("curse_haunt")], say="무언가가 손을 잡아당겼습니다. 겨우 빠져나왔지만, 무언가가 같이 따라 나왔습니다.")})
ch("H6", 0, "집들이 선물 하기", out=[gold(-70), hp(0.3)])
ch("H8", 0, "질문에 다 답해 주기", judge=dict(at=75, pass_=[flash(), shinpick(), hp(-0.1)], fail=[gold(80), hp(-0.15)],
   passSay="백 번째 질문까지 답했습니다. 벨라가 히힛 웃으며 고맙다고 카드 두 장에 제 불꽃을 나눠 줍니다. 머리가 지끈거립니다."),
   say="쉰 번째 질문에서 머리가 하얘졌습니다. \"히힛, 재밌었어!\" 벨라가 모아 둔 동전을 다 쏟아 줍니다.")
ch("H8", 1, "용돈 쥐여 주기", out=[gold(-60), hp(0.3)])

# ── 정령산 ──
ch("S1", 0, "불길 막아서기", judge=dict(at=70, pass_=[uniq(), gift("gift_drizzle"), hp(-0.1)], fail=[gift("gift_drizzle"), hp(-0.18)],
   passSay="불길이 한풀 꺾였습니다. 이프리트가 씩씩대다가 탄 고구마와 함께 불꽃 비법 한 장을 내밉니다. 앞머리가 조금 그을렸습니다."),
   say="불길에 밀려 물러났습니다. 공장 엘프들이 고맙다며 물방울 병 하나를 쥐여 줍니다. 화상이 쓰라립니다.")
ch("S2", 0, "조용히 길 묻기", out=[gold(-30), hp(0.25)],
   say="\"…우웅… 저쪽…\" 가비아가 손가락으로 길을 짚어 주고, 쉬어 갈 바위 틈까지 알려 줍니다. 큰 소리를 내지 않은 것이 다행입니다.")
ch("S3", 1, "같이 놀아 주기", judge=dict(at=70, pass_=[uniq(), hp(-0.08)], fail=[gold(40), hp(-0.15)],
   passSay="\"오, 재밌다! 또 와!\" 나이아가 호수 바닥에서 건진 것을 내줍니다. 물총을 너무 맞아 콧속이 얼얼합니다."),
   say="먼저 지쳤습니다. \"벌써 끝이야?\" 나이아가 시시하다며 호숫가에서 주운 동전을 던져 줍니다.")
ch("S8", 1, "대련 상대 되어 주기", judge=dict(at=140, pass_=[equip("희귀", "방어구"), hp(-0.1)], fail=[gold(40), hp(-0.18)],
   passSay="\"…좋다… 아프다… 고맙다…\" 빅우드가 껍질 한 조각을 떼어 줍니다. 이쪽도 꽤 아픕니다."),
   say="\"…아프냐… 미안하다…\" 빅우드의 팔 한 번에 나가떨어졌습니다. 빅우드가 송진 덩이와 동전을 미안한 듯 내밉니다.")
ch("S4", 0, "네 번 절하기", out=[hp(-0.1), flash(), shin(0.3)])
ch("S4", 2, "어지러운 돌 바로 놓기", out=[gold(-60), equip("전설", "장신구")])
ch("S5", 4, "당번표 정리해 주기", judge=dict(at=75, pass_=[remove(1), neutral("희귀"), hp(-0.08)], fail=[neutral("고급"), hp(-0.15)]),
   say="당번표는 끝내 완성되지 않았습니다. 정령들 사이에서 이리저리 치이다가, 우이가 미안하다며 카드 한 장을 쥐여 줍니다.")
ch("S6", 2, "번개 수련 받기", out=[hp(-0.15), dupe(1)],
   say="찌릿. 머리카락이 곤두섰지만 손놀림은 두 배로 빨라졌습니다. 손에 익은 수 하나가 두 번 새겨졌습니다.")
ch("S9", 0, "상대역 맡기", judge=dict(at=75, pass_=[shinpick(), flash(), hp(-0.08)], fail=[flash(), hp(-0.18)]),
   say="대사를 다 잊어버렸습니다. 블랑셰가 웃으며 대본 귀퉁이를 접어 줍니다. 무대 위에서 넘어진 무릎이 쓰라립니다.")


def main():
    done = set()
    for f in FILES:
        d = json.load(open(os.path.join(SRC, f), encoding="utf8"))
        for e in d["events"]:
            for i, o in enumerate(e["options"]):
                key = (e["id"], i)
                if key not in P: continue
                label, kw = P[key]
                assert o["label"] == label, (key, o["label"], label)
                done.add(key)
                for k, v in kw.items():
                    if k == "judge":
                        j = o["judge"]
                        if "by" in v: j["by"] = v["by"]
                        if "at" in v: j["at"] = v["at"]
                        if "pass_" in v: j["pass"] = v["pass_"]
                        if "fail" in v: j["fail"] = v["fail"]
                        if "passSay" in v: j["passSay"] = v["passSay"]
                    elif k == "gamble":
                        for gi, g in v.items():
                            o["gamble"][gi].update(g)
                    elif k == "fight_win":
                        o["fight"]["win"] = v
                    else:
                        o[k] = v
        with open(os.path.join(DST, f), "w", encoding="utf8", newline="\n") as w:
            w.write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    miss = set(P) - done
    assert not miss, miss
    print("바꾼 선택지", len(done))


if __name__ == "__main__":
    main()
