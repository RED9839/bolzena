# -*- coding: utf-8 -*-
"""고학년 원작 영상 맞춤 표 — Assets/Bolzena/Scripts/UltVideoFit.cs 의 ULT_FIT 칸(<<FIT-TABLE … FIT-TABLE>>)을 이 표로 다시 쓴다.
  python Tools/ult_fit_table.py
근거: C:/projects/bolzena-unity-tmp/ult_videos/compare_fixes.json(원작 영상 시트 ↔ 우리 시트 견줌) · 원작 스파인 이벤트(ult_motion.json) · fix_progress.md
시각 단위: 기본은 「화면 ms」(1.05배속 · 컷인 끝에서 — 우리 시트 칸 시각). nat=True 인 줄은 원작 1배속 ms(스파인 조각 시계)를 그대로 쓴다.
  hits  보이는 타격 시각(엔진 피해 합은 그대로, 보이기만 나누거나 묶음)
  move  none · dash · teleport · leap (go · land · ret · back · dest)
  fx    {장 이름 조각: (자리 caster/target/None, 트는 때 ms 또는 -1)}
  skip  건너뛸 구간 [from, to, …] · end 쉬는 동작으로 가는 때 · speed 배속(1배속 ms 로만)
"""
import os, re, sys
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
BASE = 1.05

FITS = {
    # ── 길이 · 꼬리(원작 7초 안 → 원작 길이 그대로, 길이는 PlanCut 규칙이 맡는다) + 박자 ──
    "니콜": dict(nat=True, hits=[2133], move=("dash", 1600, 2100, 3000, 350, "middle"), note="로켓 돌진 → 폭발 1타(1_3 첫 칸) → 반동으로 제자리"),
    "림_혼돈": dict(nat=True, hits=[2100, 4667], move=("teleport", 1150, 1900, 5500, 0, "behind"), note="포털로 사라져 대상 뒤 위에서 큰 베기 → 넘어가 뒤에 선 채 둘째 베기"),
    "베니_베니": dict(nat=True, hits=[2500], move=("leap", 1000, 2300, 3500, 300, "front"), note="높이 뛰어 대상 위에서 꿀단지 내려찍기(41%)"),
    "아멜리아_R41": dict(nat=True, hits=[3933], fx={"explosion": ("target", -1)}, note="포 소환 · 장판 · 레이저 뒤 빔 폭발(이벤트 3933)"),
    "빅우드": dict(nat=True, move=("none",), fx={"heart": ("caster", 4100)}, note="제자리 · 바닥 기운 뒤 한참 지나 하트 회복(이벤트 4100)"),
    "스패럿": dict(hits=[1500, 1950, 2400, 2850, 3300, 3750, 4200, 4650, 5250], note="배 돌격 뒤 3초 동안 포격 연타 · 마지막 큰 폭발 따로"),
    "바롱": dict(nat=True, hits=[4033], add=["fx_barong_ultimate_thunder_1", "fx_barong_ultimate_burn_lightning_1"], note="휘두르기 끝난 뒤 분홍 번개 폭발(이벤트 4033)"),
    "에피카": dict(nat=True, hits=[1067, 3400, 5600, 7000], note="전차 포격 → 해적선 → 기사 → 착지 번쩍(단계마다 한 박자)"),
    "아르코": dict(nat=True, hits=[1766, 2266, 2733, 3133, 3633, 4566, 4966, 5500], note="착지 뒤 펀치 8타 약 0.45초 간격(1_2 이벤트)"),
    "스피키_메이드": dict(nat=True, hits=[2467, 3134, 3801, 4468, 5135], skip=[5602, 8937], note="총 겨눔(2.1초) 뒤 순간이동 사격 5번(원작) — 우리 고리 10번 중 뒤 5번은 건너뜀"),
    "리뉴아": dict(nat=True, hits=[1133, 2975, 4392, 6375, 7509, 9208, 11050, 13317], end=14167, speed=1.2,
                 note="원작 약 11초 유지(예외) — 타격을 원작 비율(8·21·31·45·53·65·78·94%)로 고르게"),
    "아사나": dict(nat=True, hits=[2333, 3333, 4333, 5333, 6333, 7933], move=("dash", 7200, 7800, 9300, 300, "front"),
                 note="연꽃 속 제자리에서 1초 간격 5타 → 마지막 직전에만 뛰어들어 큰 폭발"),
    # ── 타격 박자 · 타수 · 이동(높음 · 중간) ──
    "그윈": dict(move=("none",), hits=[1333, 1713, 1928, 2127, 2292, 2491, 2706, 2788, 2871, 3450], note="10타를 0.4~0.6초 간격으로 넓히고 마지막 큰 휘두르기는 따로"),
    "다야": dict(hits=[1750], fx={"ground_ready": ("caster", -1), "sneeze_ground": ("caster", 1450)}, note="날아오른 뒤 하얀 폭발에 타격 · 재채기 구름은 발밑"),
    "로네_시장": dict(hits=[1618, 2018, 2418, 2818], note="군중 타격 간격 0.4초"),
    "리스티": dict(hits=[1400, 1637, 1875, 2112, 2350, 2587, 2825, 3062, 3300, 3800], note="블록이 펼쳐진 뒤 연타 · 마지막 큰 블록은 0.5초 떨어져"),
    "마고": dict(hits=[2250], fx={"target": ("target", 1250)}, note="표식 먼저 · 1초 뒤 양이 내려찍음"),
    "메죵": dict(hits=[1200]),
    "베루": dict(hits=[1100]),
    "타이다": dict(hits=[1030]),
    "레이지": dict(hits=[720]),
    "파트라": dict(hits=[1000], move=("none",), note="제자리 · 천벌 낙뢰 1타"),
    "아멜리아": dict(hits=[1500, 1675, 1850, 2025, 2200, 2375, 2550, 2725, 2900], note="충전 뒤 곧 쏨 · 파란 레이저와 주황 빔 1.4초"),
    "알레트": dict(move=("none",), fx={"stomp": ("caster", -1), "trace": ("caster", -1)}, note="제자리 보호막"),
    "에스피": dict(hits=[1400, 1480], note="사라졌다 대상 위에 나타나자마자 유령 폭발(스파인 숨는 창 733~1366 끝)"),
    "코미": dict(hits=[3450], move=("dash", 3000, 3330, 4300, 300, "front"), note="제자리에서 캔 먹고 뛰어올라 내려앉으며 타격"),
    "폴랑": dict(hits=[1650], note="병사 · 대포가 오고 겨눈 뒤 발사"),
    "헤일리_멀쩡": dict(move=("none",), hits=[1350, 1700, 2050, 2400, 2750], fx={"buff": ("caster", 3000)}, note="전함 · 병사 내려와 겨눈 뒤 사격 5타 · 버프는 끝에"),
    "네르": dict(hits=[2780], move=("none",), fx={"ultimate_03": ("caster", 800), "ultimate02": ("caster", -1)}, note="제자리에서 황금 나무가 다 자란 뒤 1타"),
    "델리아": dict(hits=[1330], move=("none",), note="펭귄 소환 뒤 펭귄이 돌진 · 델리아는 제자리"),
    "리츠": dict(hits=[403, 1040, 1850], move=("dash", 1600, 1800, 2850, 300, "front"), note="1~2타 제자리 땅 칼날 · 3타만 뛰어듦"),
    "마에스트로2호": dict(hits=[1450], move=("none",), note="제자리 충전 뒤 충격파"),
    "모모": dict(hits=[1150, 1400, 1650, 1920], move=("teleport", 500, 900, 2950, 0, "behind"), fx={"linelightning": ("target", -1), "linetotarget": ("target", -1)},
               note="사라져 적 너머로 · 세로 번개 4타 · 2.9초에 돌아옴"),
    "바리에": dict(fx={"energy": ("caster", 100), "book": ("caster", 2500), "_buff": ("caster", 3500)}, note="충전 → 책 → 버프 단계"),
    "슈로": dict(hits=[1880, 2500, 3010], move=("dash", 1700, 1880, 3260, 300, "front"), note="제자리 바람 준비 뒤 1타에 뛰어듦 · 3타 직후 복귀"),
    "실피르": dict(hits=[1200, 1330], note="날아올랐다 내려와 얼음 광선(숫자 1~2번)"),
    "엘레나": dict(hits=[730, 1020, 1310, 1600, 1890, 2200], fx={"drone_spawn": ("target", -1)}, note="드론 빛기둥은 적 위 · 연타 빨리 · 폭발로 끝"),
    "잉클": dict(hits=[1650], note="글씨 쓰기 뒤 자기 번쩍 → 적 분홍 폭발"),
    "칸나": dict(hits=[1850], fx={"fire_1": ("caster", 1056)}, note="바주카 발사(총구) 뒤 약 0.8초에 양 폭발"),
    "큐이": dict(fx={"heal": ("caster", 700)}, note="회복 빛 0.33"),
    "카렌": dict(fx={"heal": ("caster", 400)}, note="회복 고리 0.2"),
    "티그_영웅": dict(hits=[2250], move=("teleport", 2050, 2150, 3000, 0, "front"), note="섬광 직후 베어 들어감"),
    "피라": dict(hits=[632, 931, 1236, 1570, 1868, 2167, 2486, 3300, 3650, 4000, 4350, 4700], note="1막 7타 · 2막(큰 고양이) 5타"),
    "네티": dict(move=("none",), fx={"neti_ultimate_1": ("caster", 1278)}, note="빛구슬은 사도 머리 위에서"),
    "디아나_왕년": dict(hits=[2400], move=("dash", 1450, 1730, 4354, 300, "front"), note="불꽃 모으고 일찍 돌진 · 0.8초 버티다 폭발"),
    "레테": dict(move=("none",), note="제자리 빛줄기"),
    "림": dict(hits=[1650, 2440], note="뛰어들며 큰 베기 · 0.8초 뒤 X자 베기"),
    "셀리네": dict(hits=[1250], move=("none",), note="제자리 리본 춤 뒤 번쩍"),
    "스노키": dict(hits=[1100], fx={"slash_3": ("caster", 500)}, move=("leap", 280, 1000, 3300, 300, "front"), note="뛰어올라 금빛 고리에 머물다 내리꽂음"),
    "시온더다크불릿": dict(hits=[1480], note="푸른 고리 모으기 뒤 베기"),
    "아라그니아": dict(hits=[1100, 1600, 2100], fx={"laser": ("target", -1)}, note="소용돌이 뒤 첫 충격 · 물기둥은 적 자리"),
    "아이시아": dict(hits=[250, 2900], fx={"jump": ("caster", -1), "explosion": ("target", 2350)}, note="번쩍 직후 작은 구슬 · 큰 폭발은 둘째 타격"),
    "에슈르": dict(hits=[1833, 1931, 2650], fx={"proj_2": ("target", -1)}, note="첫 폭발 1초 뒤 작은 폭발 한 번 더"),
    "오르": dict(fx={"orr_ultimate_1": ("caster", 950)}, note="돔은 사도를 덮고 일찍"),
    "캐시": dict(nat=True, hits=[4400], move=("dash", 2900, 3150, 5600, 300, "front"), fx={"kathy_ultimate_1": ("target", 3333)}, note="내려앉은 뒤 기둥 · 큰 폭발 1타 · 붉은 원이 남는 동안 머묾"),
    "키디언": dict(hits=[1333, 2330], move=("teleport", 681, 1014, 2760, 0, "front"), note="찌르기 두 번(1초 간격) 뒤 내려섬"),
    "하이디": dict(hits=[1950, 2050, 2150, 2250, 2350, 2500], move=("dash", 700, 1500, 2600, 300, "front"), fx={"news": ("target", 1900)}, note="굴러 들어가 큰 번쩍과 함께 몰아침 · 돌아옴"),
    "네르_빡침": dict(nat=True, hits=[3455], move=("leap", 3150, 3400, 3650, 0, "front"), note="수정 장막 뒤 도약 타격 · 0.2초 만에 복귀"),
    "리코타": dict(hits=[2250, 2431, 2653, 2889], move=("none",), fx={"slash_1": ("target", -1)}, note="제자리 긴 베기 · 이른 베기는 첫 타격에"),
    "마요": dict(hits=[900, 1600, 1800, 2050, 2300, 2550, 2800, 3000], note="단발 28% → 구름 다단이 끝 직전까지"),
    "버터": dict(hits=[2300], note="새총 당기기 1초 뒤 발사"),
    "벨벳": dict(nat=True, move=("dash", 383, 673, 3200, 300, "front"), note="마지막 타격 직후 복귀"),
    "슈팡": dict(hits=[1350], move=("dash", 1150, 1350, 2300, 300, "front"), note="노란 돌진이 닿는 때"),
    "시스트": dict(fx={"sist_ultimate": ("caster", -1)}, note="지폐비 · 오라는 시스트 자신에게"),
    "아네트": dict(hits=[819, 935, 1052, 1168, 1285, 1401, 1517, 1634, 1750, 1867, 1983, 2100], note="다단은 65% 지점까지"),
    "아야": dict(hits=[1200, 1725, 2250, 2775, 3300], note="연꽃 두 번에 걸쳐 약 0.5초 간격"),
    "칸타": dict(hits=[1350, 3000, 3380], move=("dash", 1050, 1300, 1750, 300, "front"), note="대시 베기 뒤 곧 복귀 · 폭발은 집에서"),
    "클로에": dict(hits=[1632, 2050, 2470, 2890, 3310, 3730, 4100], note="토끼들이 0.4초 간격으로"),
    "비비_신성": dict(fx={"boll": ("target", 4500)}, note="레이저 뒤 약 1초에 적 자리 빛기둥(boll)"),
    "실라": dict(move=("dash", 1500, 1700, 2000, 200, "front"), note="짧게 달려들어 휘두르고 곧 돌아옴"),
    "에르핀_왕도": dict(fx={"crash_1": ("caster", 800)}, note="첫 빛기둥은 사도 자리 · 도약은 길이를 되살려 원작 비율(1.9초)"),
    "바나": dict(fx={"happy": ("caster", 4600), "hit_1": ("caster", -1)}, note="불꽃은 모루 위 · 완성 버프(happy)는 끝에"),
    "피코라": dict(fx={"ultimate_up": ("caster", -1)}, note="하트 방울은 사도를 감쌈"),
    "로네": dict(move=("none",), note="제자리 깃발"),
    "비비": dict(hits=[1140], note="공중에 떴다 내리꽂기"),
    "셰럼": dict(hits=[2650, 3350, 4150], move=("none",), note="제자리 그리기 · 큰 타격 3번 0.7~0.8초 간격"),
    "시저": dict(hits=[975, 1525]),
    "에슈르_마도": dict(hits=[1980], note="마법진 준비 뒤 불덩이 1타"),
    "쥬비": dict(hits=[820, 885, 950, 1015, 1080, 1145, 1210], note="꿀 폭발 뒤 0.4초 안 연타"),
    "캬롯": dict(hits=[3800, 3865, 3930, 3995, 4060, 4125, 4190, 4255, 4320, 4385, 4450, 4515], note="빛기둥이 내려온 뒤 큰 폭발"),
    "포셔": dict(hits=[2150], note="휘두른 뒤 분홍 폭발"),
    "나이아": dict(hits=[2350], note="파도가 들어와 0.8초 뒤 침"),
    "다야_퓨어샤인": dict(hits=[2250, 2500, 2750, 2986, 3236, 3458], fx={"explosion_2": ("target", 3500)}, note="뜨고 모은 뒤 연타 · 마지막 큰 폭발"),
    "레비": dict(hits=[1550], move=("dash", 1250, 1500, 3181, 300, "front")),
    "로니": dict(hits=[1050, 1400, 1750, 2100, 2450, 2900]),
    "리온": dict(hits=[688, 1190, 2667]),
    "마리": dict(hits=[2300]),
    "멜루나": dict(hits=[2300]),
    "벨라": dict(hits=[1240]),
    "셰이디_역전": dict(hits=[1800, 2800]),
    "스피키": dict(fx={"raady": ("caster", -1), "ready": ("caster", -1), "circle": ("caster", -1), "ground": ("caster", -1), "pumpkin": ("target", 3100)}),
    "실비아": dict(move=("none",)),
    "우로스": dict(hits=[1250, 2500]),
    "코미_수영복": dict(nat=True, hits=[300, 2000, 2667, 3333, 4000, 4667], move=("none",), fx={"waterfall": ("target", 100)}, note="물기둥이 시작하자마자 침 · 코미는 제자리에서 뛰어오름"),
    "티그": dict(nat=True, hits=[840], move=("leap", 420, 800, 3167, 300, "front"), fx={"aura": ("caster", 1800)}, note="뛰어 베기 먼저 · 호랑이 오라는 착지 뒤"),
    # ── 판단 어려움 6명(원작 mp4 를 0.1초 칸으로 뽑아 봄) ──
    "아일라": dict(nat=True, hits=[2433, 3400], note="영상: 2.3초 베기는 화상만 · 피해는 4.1초 운석 폭발 → 베기 · 운석 두 박자(운석 장은 없음)"),
    "롤렛": dict(nat=True, hits=[2333], fx={"box": ("target", 1300)}, note="영상: 상자 등장 1.3초 → 걸어가 3.6초 폭발 피해 → 상자 먼저 · 폭발 타격은 둘째 이벤트"),
    "사리": dict(hits=[604], note="영상: 하늘 번개 한 번(0.4초) 피해 1회 → 둘째 표시 타격 뺌"),
    "디아나": dict(hits=[1403, 1473, 1543, 1613, 1683, 1753, 1823, 1893, 1963, 2033, 2103], fx={"diana_ultimate_3": ("caster", -1), "ground_1": ("caster", -1)},
                 note="영상: 번쩍 직후 0.5초 안에 다단 · 초록 초승달은 디아나 앞에서 쓸고 감(첫 타격 1433 정권은 2026-10-06 사용자 결정대로 둠)"),

}


def ms(v, nat):
    return int(round(v if nat else v * BASE))


def cs(hero, f):
    nat = f.get("nat", False)
    parts = []
    if "hits" in f: parts.append("Hits = H(" + ", ".join(str(ms(x, nat)) for x in f["hits"]) + ")")
    if "move" in f:
        m = f["move"]
        if m[0] == "none": parts.append('Move = "none"')
        else:
            t, go, land, ret, back, dest = m
            parts.append(f'Move = "{t}", Go = {ms(go, nat)}, Land = {ms(land, nat)}, Ret = {ms(ret, nat)}, Back = {back}, Dest = "{dest}"')
    if "fx" in f:
        items = []
        for k, (at, t) in f["fx"].items():
            items.append(f'("{k}", {("\"" + at + "\"") if at else "null"}, {ms(t, nat) if t >= 0 else -1})')
        parts.append("Fx = F(" + ", ".join(items) + ")")
    if "skip" in f: parts.append("Skip = H(" + ", ".join(str(x) for x in f["skip"]) + ")")
    if "end" in f: parts.append(f"End = {f['end']}")
    if "add" in f: parts.append("Add = new[] { " + ", ".join('"' + x + '"' for x in f["add"]) + " }")
    if "speed" in f: parts.append(f"Speed = {f['speed']}f")
    if "note" in f: parts.append(f'Note = "{f["note"]}"')
    return f'            ["{hero}"] = new UltFit {{ {", ".join(parts)} }},'


def main():
    path = os.path.join(os.path.dirname(__file__), "..", "Assets", "Bolzena", "Scripts", "UltVideoFit.cs")
    s = open(path, encoding="utf-8").read()
    a = s.index("// <<FIT-TABLE")
    a = s.index("\n", a) + 1
    b = s.index("            // FIT-TABLE>>")
    body = "\n".join(cs(h, f) for h, f in FITS.items()) + "\n"
    s = s[:a] + body + s[b:]
    open(path, "w", encoding="utf-8", newline="\n").write(s)
    print(f"맞춤 {len(FITS)}명 → {os.path.normpath(path)}")


if __name__ == "__main__":
    main()
