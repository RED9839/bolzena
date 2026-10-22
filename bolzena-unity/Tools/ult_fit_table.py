# -*- coding: utf-8 -*-
"""고학년 원작 영상 맞춤 표 — Assets/Bolzena/Scripts/UltVideoFit.cs 의 ULT_FIT 칸(<<FIT-TABLE … FIT-TABLE>>)을 이 표로 다시 쓴다.
  python Tools/ult_fit_table.py
근거: C:/projects/bolzena-unity-tmp/ult_videos/compare_fixes.json(원작 영상 시트 ↔ 우리 시트 견줌) · 원작 스파인 이벤트(ult_motion.json) · fix_progress.md
시각 단위: 기본은 「화면 ms」(1.05배속 · 컷인 끝에서 — 우리 시트 칸 시각). nat=True 인 줄은 원작 1배속 ms(스파인 조각 시계)를 그대로 쓴다.
  hits  보이는 타격 시각(엔진 피해 합은 그대로, 보이기만 나누거나 묶음)
  move  none · dash · teleport · leap (go · land · ret · back · dest)
  fx    {장 이름 조각: (자리 caster/target/None, 트는 때 ms 또는 -1)}
  skip  건너뛸 구간 [from, to, …] · end 쉬는 동작으로 가는 때 · speed 배속(1배속 ms 로만)
  raw   C# 원문 그대로(앞 주석 줄 포함) — 위 필드로 못 내는 맞춤(Pick · Chain · FS · FM · Cap · Drop · Rep · RetHidden …). 2026-10-18 표와 맞춤(그 전엔 표를 손으로 고쳐 이 도구가 뒤처졌다)
"""
import os, re, sys
sys.stdout.reconfigure(encoding="utf-8", errors="replace")
BASE = 1.05

FITS = {
    # raw = 손으로 다듬은 C# 줄 그대로(앞 주석 줄까지) — 생성식(cs)이 못 내는 필드(Pick · Chain · FS · FM · Cap · Drop · Rep · RetHidden …)를 쓴 사도. 표를 고칠 때는 이 원문을 고친다(10-18 표와 맞춤)
    "니콜": dict(nat=True, hits=[2133], move=("dash", 1600, 2100, 3000, 350, "middle"), note="로켓 돌진 → 폭발 1타(1_3 첫 칸) → 반동으로 제자리"),
    "림_혼돈": dict(nat=True, hits=[2100, 4667], move=("teleport", 1150, 1900, 5500, 0, "behind"), note="포털로 사라져 대상 뒤 위에서 큰 베기 → 넘어가 뒤에 선 채 둘째 베기"),
    "베니_베니": dict(nat=True, hits=[2500], move=("leap", 1000, 2300, 3500, 300, "front"), note="높이 뛰어 대상 위에서 꿀단지 내려찍기(41%)"),
    "아멜리아_R41": dict(nat=True, hits=[3933], fx={"explosion": ("target", -1)}, note="포 소환 · 장판 · 레이저 뒤 빔 폭발(이벤트 3933)"),
    "빅우드": dict(nat=True, move=("none",), fx={"heart": ("caster", 4100)}, note="제자리 · 바닥 기운 뒤 한참 지나 하트 회복(이벤트 4100)"),
    "스패럿": dict(hits=[1500, 1950, 2400, 2850, 3300, 3750, 4200, 4650, 5250], note="배 돌격 뒤 3초 동안 포격 연타 · 마지막 큰 폭발 따로"),
    "바롱": dict(nat=True, hits=[4033], add=["fx_barong_ultimate_thunder_1", "fx_barong_ultimate_burn_lightning_1"], note="휘두르기 끝난 뒤 분홍 번개 폭발(이벤트 4033)"),
    "요미": dict(raw=r'''            ["요미"] = new UltFit { Fx = FS(("ultimate_3", null, 1100, 0.6f), ("ultimate_2", null, 1100, 1f)), Hits = H(2300, 2700, 3100, 3500, 3900, 4300),   // (3차) 달빛은 1.1초 요미 뒤에 생겨 쓸고 나감(UltFx.YOMI_SWEEP) · 타격 6번
                Note = "2026-10-14 원작 대조: 시전자 쪽 큰 흰 초승달이 원작보다 크고 오래 — 0.6배",
                Cap = new Dictionary<string, float> { ["ultimate_1"] = 3.0f, ["ultimate_3"] = 3.3f, ["ultimate_2"] = 3.3f, ["ultimate_proj_1"] = 3.0f } },   // (10-17 원작 대조) 반복 장(달빛 · 보라 막) — 원작은 4.6초에 걷힌다, 우리 고학년이 길어 화면 끝까지 덮었다'''),
    "이드": dict(raw=r'''            // (3차) 키 「ground_1」이 endground_1 에도 걸려 endground 줄이 안 먹었다 · 구(ultimate_ground)는 이드 위
            ["이드"] = new UltFit { Move = "none", Fx = F(("bodycast_1", "caster", 367), ("endground", "target!", 3500), ("ultimate_ground_1", null, 1867), ("ultimate_ground", "caster!", 1500)), Hits = H(2233, 2733, 3233, 3733), Cap = new Dictionary<string, float> { ["ultimate_ground"] = 1.8f }, Note = "(10-17 원작 대조) 반복 바닥 장(보라 타원 · 비눗방울 구) — 원작은 3.6초 뒤 없다" },'''),
    "에피카": dict(raw=r'''            // 에피카 — Ultimate1_2 = 자동차(MadMax) · 1_3 = 해적선(Pirate) · 1_1 = 유령 말(Gandalf) + 끝 착지(스파인 첨부 표본 2026-10-15). 갈래가 아니라
            //   원작은 1_2 앞 2.3초 → 1_3 의 0.9~3.1초 → 1_1 의 6.6~10초를 잇는다(합 7.9초 = 원작 영상). 이은 시계: 1_2 0~10 · 1_3 10~20 · 1_1 20~30초
            //   (10-16 사용자) 셋을 잇지 말고 고학년마다 하나만 무작위로 — 조각마다 제 시계(ms), 고르지 않은 단계 장은 뺀다. 예전 잇기 값: 1_2 앞 2.3초 · 1_3 0.9~3.1초 · 1_1 6.6~10초
            ["에피카"] = new UltFit
            {
                Pick = new[]
                {
                    // (fix_Y 3) 갈래마다 스파인 맞음 이벤트(1006411, 200ms 간격)대로 — 투사체 · 소환수 던지기를 맞음마다(Rep · PetHits), 이벤트 없는 가운데는 Skip
                    // (10-18 원작 대조) 맞음은 1006411 대로 867~2467 200ms 9번(전엔 4번 · 값이 이벤트와 달랐다) · 사막 바닥 원 · 불기둥 · 미니카는 시전자 쪽(키 「ultimate_desert」 하나가 다 잡아 300ms 에 적 쪽이던 것 — 구체 키 먼저)
                    new UltFit { Anim = "Ultimate1_2", PetSkin = "Normal2", PetAnim = "Attack1_2", PetHits = H(867, 1067, 1267, 1467, 1667, 1867, 2067, 2267, 2467), Hits = H(867, 1067, 1267, 1467, 1667, 1867, 2067, 2267, 2467), Skip = H(2600, 9000),
                                 Rep = new[] { "ultimate_proj_firebomb" },
                                 // (10-18 검증_1) 붉은 바닥 원(desert_ground_2)은 입자 수명 0.6초 한 번 터짐 — 장 길이 · Cap 이 자른 것이 아니다. 원작은 400ms 박자 이벤트(1006421: 867 · 2000 · 2400 · 2800 …)마다 다시 튼다
                                 //   (세 갈래 모두 박자 이벤트가 하나씩 — 1006419 · 1006421 · 1006428 — 그 갈래 ground_2 로 본다). 자르기(Skip 2600~) 안의 2000 · 2400 만
                                 Fx = FM(("ultimate_desert_fire", "caster!", 867, 1f, null), ("ultimate_desert_ground_2", "caster!", 867, 1f, new[] { 2000, 2400 }), ("ultimate_desert_ground", "caster!", 867, 1f, null), ("ultimate_desert_minicar", "caster!", 1267, 1f, null),
                                         ("ultimate_desert", null, 300, 1f, null), ("ultimate_guitar", "caster", 900, 1f, null), ("ultimate_jump", null, 9100, 1f, null), ("ultimate_buff_top", null, 9200, 1f, null)),
                                 Drop = new[] { "ultimate_sea", "ultimate_proj_plunger", "ultimate_battlefield", "ultimate_proj_arrow", "ultimate_horn", "ultimate_violin" }, Note = "자동차(MadMax) — 사막 · 기타 · 화염병 매 맞음" },
                    new UltFit { Anim = "Ultimate1_3", PetSkin = "Normal3", PetAnim = "Attack1_3", PetHits = H(1500, 1630, 1800, 2270, 2500, 2800, 3100, 3400), Hits = H(1500, 1630, 1800, 2270, 2500, 2800, 3100, 3400), Skip = H(3600, 9000),
                                 Rep = new[] { "ultimate_proj_plunger" },
                                 Fx = FS(("ultimate_sea_ground", "caster!", 1000, 1f), ("ultimate_sea", null, 1000, 1f), ("ultimate_violin", "caster", 1000, 1f), ("ultimate_jump", null, 9100, 1f), ("ultimate_buff_top", null, 9200, 1f)),   // (10-18) 바닥 장(sea_ground)은 시전자 쪽 — 자동차 갈래와 같은 꼴, 시각은 그대로
                                 Drop = new[] { "ultimate_desert", "ultimate_proj_firebomb", "ultimate_battlefield", "ultimate_proj_arrow", "ultimate_guitar", "ultimate_horn" }, Note = "해적선(Pirate) — 바다 · 바이올린 · 뚫어뻥 매 맞음" },
                    new UltFit { Anim = "Ultimate1_1", PetSkin = "Normal1", PetAnim = "Attack1_1", PetHits = H(1070, 1270, 1670, 1940, 2070, 2270, 2570, 2770, 2870), Hits = H(1070, 1270, 1670, 1940, 2070, 2270, 2570, 2770, 2870, 9200), Skip = H(2900, 8700),
                                 Rep = new[] { "ultimate_proj_arrow" },
                                 Fx = FS(("ultimate_battlefield_ground", "caster!", 800, 1f), ("ultimate_battlefield", null, 800, 1f), ("ultimate_horn", "caster", 900, 1f), ("ultimate_jump", null, 9100, 1f), ("ultimate_buff_top", null, 9200, 1f)),   // (10-18) 바닥 장(battlefield_ground)은 시전자 쪽 — 자동차 갈래와 같은 꼴, 시각은 그대로
                                 Drop = new[] { "ultimate_desert", "ultimate_proj_firebomb", "ultimate_sea", "ultimate_proj_plunger", "ultimate_guitar", "ultimate_violin" }, Note = "유령 말(Gandalf) — 맞음 9번 + 착지(Skip 2.9~8.7 은 영상이 건너뛰는 가운데)" },
                },
                Note = "자동차 · 해적선 · 유령 말 중 하나(무작위)",
            },'''),
    "아르코": dict(nat=True, hits=[1766, 2266, 2733, 3133, 3633, 4566, 4966, 5500], note="착지 뒤 펀치 8타 약 0.45초 간격(1_2 이벤트)"),
    "스피키_메이드": dict(nat=True, hits=[2467, 3134, 3801, 4468, 5135], skip=[5602, 8937], note="총 겨눔(2.1초) 뒤 순간이동 사격 5번(원작) — 우리 고리 10번 중 뒤 5번은 건너뜀"),
    "리뉴아": dict(raw=r'''            ["리뉴아"] = new UltFit { Cap = new Dictionary<string, float> { ["ultimate_ground_"] = 4.2f },   // (10-16) 바닥 장 1~3 모두(Patten · Uline 다각형 이미터를 같이 품는다) — 4.6초부터 다각형 조각 단계 — 원작은 그때 클로즈업이 덮는다, 우리는 클로즈업을 건너뛰어 회색 다각형 바닥이 드러났다
                Fx = FS(("ultimate_loop_1", "caster!", -1, 1f), ("ultimate_ground_", "caster", -1, 1.8f)), Hits = H(3333, 4233, 11633, 11967, 12467, 12967, 13467, 13967), Skip = H(4600, 11300), End = 14400, Speed = 1.2f, Note = "2026-10-13 사용자: 시전 준비를 줄인다 — 시계 장판 뒤 이벤트 없이 떠 있는 6.7초를 건너뛰고 연타(이벤트 11633~13967)로 · 11.8→약 6.4초" },'''),
    "아사나": dict(nat=True, hits=[2333, 3333, 4333, 5333, 6333, 7933], move=("dash", 7200, 7800, 9300, 300, "front"),
                 note="연꽃 속 제자리에서 1초 간격 5타 → 마지막 직전에만 뛰어들어 큰 폭발"),
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
    "티그_영웅": dict(raw=r'''            ["티그_영웅"] = new UltFit { Fx = FS(("ultimate_slash_1", "target!", 2200, 2.5f), ("ultimate_cros_1", "caster", 1200, 1f)), Hits = H(2362), Move = "teleport", Go = 2152, Land = 2258, Ret = 3150, Back = 0, Dest = "front", Note = "섬광 직후 베어 들어감" },'''),
    "피라": dict(hits=[632, 931, 1236, 1570, 1868, 2167, 2486, 3300, 3650, 4000, 4350, 4700], note="1막 7타 · 2막(큰 고양이) 5타"),
    "네티": dict(move=("none",), fx={"neti_ultimate_1": ("caster", 1278)}, note="빛구슬은 사도 머리 위에서"),
    "디아나_왕년": dict(raw=r'''            ["디아나_왕년"] = new UltFit { Cap = new Dictionary<string, float> { ["ultimate_ground_1"] = 1.5f },   // (10-16 원작 대조) 원작은 회색 바람 고리가 2.6~2.8초에 잠깐 — 대지 장을 고학년 끝까지 두면 3~4초까지 남았다
                Fx = FS(("ultimate_cast_1", "caster", -1, 2f), ("ultimate_kick_1", "caster", -1, 2f), ("ultimate_ground_1", "target!", -1, 1f), ("ultimate_hit_1", null, -1, 2.5f), ("ultimate_scranim_1", null, 3050, 1f)),   // scranim = 붉은 화면 막 — 원작 2.8~3.0초에 덮고 3.2초에 흩어진다(전엔 0.4초 늦었다)
                Hits = H(2933), Move = "dash", Go = 1800, Land = 2900, Ret = 4533, Back = 300, Dest = "front", Note = "(10-18 사용자 2차 「도끼 끌며 흩뿌리다 터져야 · 몸짓 끊김」) 끌기(스파인 1.8~2.9초) 동안 끌며 다가가 2933 내리찍기에 폭발 · 몸짓 끝(4533)까지 다 하고 복귀(End 없앰) · 대지 파임은 적 자리 고정(10-16). 예전: Hits 2520 · Go 1522 · Land 1816 · Ret 3000 · End 3400" },'''),
    "레테": dict(move=("none",), note="제자리 빛줄기"),
    "림": dict(hits=[1650, 2440], note="뛰어들며 큰 베기 · 0.8초 뒤 X자 베기"),
    "셀리네": dict(hits=[1250], move=("none",), note="제자리 리본 춤 뒤 번쩍"),
    "스노키": dict(hits=[1100], fx={"slash_3": ("caster", 500)}, move=("leap", 280, 1000, 3300, 300, "front"), note="뛰어올라 금빛 고리에 머물다 내리꽂음"),
    "시온더다크불릿": dict(raw=r'''            ["시온더다크불릿"] = new UltFit { Hits = H(2433), Fx = FS(("ultimate_dance_1", "caster", 467, 1f), ("ultimate_dance_2", "caster", 1000, 1f), ("ultimate_dance_3", "caster", 1500, 1f), ("ultimate_cast_1", "caster", 467, 1f),
                    ("ultimate_explosion_1", "floor", 2433, 1f), ("ultimate_explosion", "target!", 2433, 1f), ("ultimate_fire_1", "target!", 2433, 1f), ("ultimate_ground_1", "floor", 2433, 1f), ("ultimate_smoke_1", null, 2433, 1f)), Note = "(10-18 사용자 「시온 발 밑이랑 맞춰줘」 — 바닥 불꽃 벽 explosion_1 · ground_1 은 가로 적 · 높이 시온 발(floor), 구체 키 먼저) · (10-17 사용자 「포즈마다 기 모으다 마지막 불길 임팩트로 피해」 · 원작 이벤트 467 · 1000 · 1500 · 2433) · 푸른 고리 모으기 뒤 베기 · 2026-10-14 원작 대조: dance(파란 리본 · 불꽃)는 시전자를 감는다" },'''),
    "아라그니아": dict(hits=[1100, 1600, 2100], fx={"laser": ("target", -1)}, note="소용돌이 뒤 첫 충격 · 물기둥은 적 자리"),
    "아이시아": dict(hits=[250, 2900], fx={"jump": ("caster", -1), "explosion": ("target", 2350)}, note="번쩍 직후 작은 구슬 · 큰 폭발은 둘째 타격"),
    "에슈르": dict(hits=[1833, 1931, 2650], fx={"proj_2": ("target", -1)}, note="첫 폭발 1초 뒤 작은 폭발 한 번 더"),
    "오르": dict(fx={"orr_ultimate_1": ("caster", 950)}, note="돔은 사도를 덮고 일찍"),
    "캐시": dict(nat=True, hits=[4400], move=("dash", 2900, 3150, 5600, 300, "front"), fx={"kathy_ultimate_1": ("target", 3333)}, note="내려앉은 뒤 기둥 · 큰 폭발 1타 · 붉은 원이 남는 동안 머묾"),
    "키디언": dict(hits=[1333, 2330], move=("teleport", 681, 1014, 2760, 0, "front"), note="찌르기 두 번(1초 간격) 뒤 내려섬"),
    "하이디": dict(hits=[1950, 2050, 2150, 2250, 2350, 2500], move=("dash", 700, 1500, 2600, 300, "front"), fx={"news": ("target", 1900)}, note="굴러 들어가 큰 번쩍과 함께 몰아침 · 돌아옴"),
    "네르_빡침": dict(raw=r'''            ["네르_빡침"] = new UltFit { Fx = FS(("ultimate_charge_2", null, 1900, 1f), ("ultimate_1", null, 2967, 1f)), Hits = H(2967),   /* (10-15) 장 시각 = 스파인 이벤트 1900(수정 깨짐 프리즘) · 2967(타격) — 전엔 셋 다 0초에 떠 0.2초에 무지개 판 */ Move = "none", Speed = 1.35f,   /* (10-17 사용자 「네르 빡침 다가감」) 후열 원거리 — 도약 없앰 */ Note = "수정 장막 뒤 도약 타격 · 0.2초 만에 복귀 · 2026-10-14 배속 1.35(몸짓 4.67초 → 원작 영상 3.37초, 타격 3.3 → 2.5초 — 1.05배속이라 슬로모션처럼 보였다)" },'''),
    "리코타": dict(hits=[2250, 2431, 2653, 2889], move=("none",), fx={"slash_1": ("target", -1)}, note="제자리 긴 베기 · 이른 베기는 첫 타격에"),
    "마요": dict(hits=[900, 1600, 1800, 2050, 2300, 2550, 2800, 3000], note="단발 28% → 구름 다단이 끝 직전까지"),
    "버터": dict(hits=[2300], note="새총 당기기 1초 뒤 발사"),
    "벨벳": dict(nat=True, move=("dash", 383, 673, 3200, 300, "front"), note="마지막 타격 직후 복귀"),
    "슈팡": dict(hits=[1350], move=("dash", 1150, 1350, 2300, 300, "front"), note="노란 돌진이 닿는 때"),
    "시스트": dict(fx={"sist_ultimate": ("caster", -1)}, note="지폐비 · 오라는 시스트 자신에게"),
    "아네트": dict(hits=[819, 935, 1052, 1168, 1285, 1401, 1517, 1634, 1750, 1867, 1983, 2100], note="다단은 65% 지점까지"),
    "아야": dict(raw=r'''            // (10-18 3차 「씨앗이 떨어지지 않는다」) 씨앗 장은 입자 속도 곡선(y × 50 — 수명 0.648 까지 0, 1.0 에 −0.287)으로 수명 0.91초부터 떨어진다. 원작 고학년.mp4 1.7 → 1.95초에 머리 위에서 몸으로 떨어지고 2.0초 터짐(떨어지기 시작 ~ 터짐 ≈ 몸짓 0.33초).
            //   1000 에 틀면 떨어지기 시작(1910)이 터짐(1900)과 겹쳐 떠 있다가 터지기만 했다 → 1900 − 910 − 330 = 660 에 틀어 터지기 전에 떨어지게
            ["아야"] = new UltFit { Hits = H(1900), Fx = FS(("ultimate_seed_1", "target!", 660, 1f), ("ultimate_explosion_1", null, 1900, 1f)), Note = "(10-17 사용자 「적마다 얼음 씨앗 → 만개 한 번」 · 원작 이벤트 1000 · 1900) · 연꽃 두 번에 걸쳐 약 0.5초 간격 · (10-18 3차) 씨앗 660" },'''),
    "칸타": dict(hits=[1350, 3000, 3380], move=("dash", 1050, 1300, 1750, 300, "front"), note="대시 베기 뒤 곧 복귀 · 폭발은 집에서"),
    "클로에": dict(raw=r'''            ["클로에"] = new UltFit { Anim = "Skill2_1", Move = "none",   // (fix_Y 4) 서서 웃는 Skill2_1(Acts 의 Anim 덮어쓰기) — 전엔 세바스티안 타기(Skill1_1)
                Fx = FM(("ultimate_proj_1", null, 1300, 1f, new[] { 1500, 1700 }), ("ultimate_hit_1", "target!", 2100, 1f, null)),
                Hits = H(2133, 2600, 3267, 3767, 4133, 4767), Note = "제자리 웃음 — 곰 셋이 왼쪽에서 달려와 적 발밑으로(1.3~2.1초) · 맞음 6번(10-17 정밀 분석)" },'''),
    "비비_신성": dict(nat=True, hits=[4400, 5300], fx={"full_default": ("caster", 1900), "boll": ("target", 5150), "full_end": (None, 5900)}, end=7300,
                  note="2026-10-14 원작 영상(×1.2 = 스파인 ms): 시전 1.4초 → 화면 위로 날아올라 사라짐(1.45~2.4) → 금빛 고리(1.9) → 큰 스탠딩(2.6~5.8 · ULT_STANDING) 손에서 레이저(3.95) · 맞음(4.4) → 빛기둥(5.15~5.75) → 소용돌이(5.9) → 내려와 제자리(6.4~7.2) · 건너뛰기 없앰(날아오름을 살림) · 끝 대기만 7.3초에서"),
    "실라": dict(move=("dash", 1500, 1700, 2000, 200, "front"), note="짧게 달려들어 휘두르고 곧 돌아옴"),
    "에르핀_왕도": dict(raw=r'''            // (fix_Y 2) 기 모음(charge_1 무지개 번쩍)은 원작 0.0~0.7 에 없다 — 빼고 금빛 기둥(cast_1)을 이벤트 567 에. 가운데 「가까이 찍기」는 원작 영상이 압축(Skip)
            //   Skip 길이: fix_Y 는 1배속 가정으로 1.08초(3750~4830) — 배속표(1.31) 아래에선 2933→5366(2.43초)이 영상 1.27초가 되려면 0.77초라 3750~4520
            ["에르핀_왕도"] = new UltFit { Drop = new[] { "ultimate_charge_1" },
                Fx = F(("cast_1", "caster", 567), ("jump_1", "caster", 1933), ("drop_1", "target!", 2800), ("crash_1", "target!", 2933), ("crash_aura_1", "target!", 2933),
                       ("shockwave_1", "target!", 2933), ("explosion_1", "target!", 5400)),   // 주먹 찍기 = 폭발 자리 · 거의 동시, 반동 복귀는 그 뒤(원작 스파인: 1.93 도약 → 2.93 내려찍기 → 5.43 폭발)
                Hits = H(2933, 3200, 3467, 3733, 4000, 4267, 4533, 4733, 5433),   // (10-18 사용자 2차 「8회 범위 피해 동안 주먹 계속, 마지막 터지며 날아감」) 원작 1_2 주먹 8번(1012323 ×7 + 1012324) + 1_3 폭발(1012348) — 전 H(5366) 한 번 · Skip 3750~4520 이 주먹 구간을 잘랐다
                Move = "leap", Go = 2000, Land = 2033, Ret = 5366, Back = 350, RetHidden = true, RetFly = true, Dest = "on", End = 7800, Note = "(10-18 사용자 2차) Skip 없앰 · 숨은 틈 끝(5366)에 폭발과 함께 제자리로 날려 감(RetFly — 몸짓 1_3 그대로). 주먹마다 먼지(drop_1 짝)는 모름이라 안 넣음 · (10-18 사용자 「공중 순간이동 · 터짐과 날아감 어긋남」) 원작 숨은 틈(pose.hidden 2000~2033 · 4833~5366)에 옮긴다 — 2016 적 위로, 4850 제자리로(폭발과 함께 제자리에서 굴러 나옴) · (10-17 원작 영상) 날아올라 적 위에 내리찍고 반동으로 돌아온다 · 첫 빛기둥은 사도 자리 · 둘째 빛기둥(jump_1)도 뛰어오르는 자리(대상 쪽이면 화면 오른쪽 밖) · 꼬리를 원작 6.21초(영상 한 벌)에 맞춰 6500에서 끊음(1.05배속 → 6.19초)" },'''),
    "바나": dict(fx={"happy": ("caster", 4600), "hit_1": ("caster", -1)}, note="불꽃은 모루 위 · 완성 버프(happy)는 끝에"),
    "피코라": dict(fx={"ultimate_up": ("caster", -1)}, note="하트 방울은 사도를 감쌈"),
    "로네": dict(move=("none",), note="제자리 깃발"),
    "비비": dict(raw=r'''            ["비비"] = new UltFit { Hits = H(1167, 2067), Move = "teleport", Go = 1800, Land = 2000, Ret = 2600, Back = 300, Dest = "front",   // (fix_Y 7) 0.4초 순간이동 → 제자리에서 날아올라 창을 던지고 1.9초에 내려와 밟음
                Fx = F(("fire_1", "caster", 150), ("jump_1", "caster", 450), ("jump_screw", "caster", 900), ("proj_1", null, 830), ("jump_2", "target!", 1167), ("stomp", "target!", 2067)),
                Note = "제자리에서 날아올라 창(830 발사 → 1167 맞음) → 장판 1167~ → 1.9초(스파인) 내려와 밟음" },'''),
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
    "벨라": dict(raw=r'''            ["벨라"] = new UltFit { Hits = H(1000),   // (fix_Y 5-2) hit_a 는 적 발밑(아군 판 끔) · cast_2 는 시전자 + 아군(2133) · end 보호막은 3500
                Fx = F(("ultimate1_lantern", "caster", 800), ("ultimate1_hit_a", "target!", 1000), ("ultimate1_hit_e", "target!", 1100), ("ultimate1_cast_1", "caster", 1700),
                       ("ultimate1_proj_a", "caster", 1800), ("ultimate1_cast_2", "ally", 2133), ("ultimate1_end", "caster", 3500)),
                Note = "원작 1.0 적 발밑 폭발 → 1.1~1.6 기둥 → 1.7 십자 섬광 → 2.3 아군 섬광 → 3.4 시전자 보호막" },'''),
    "셰이디_역전": dict(hits=[1800, 2800]),
    "스피키": dict(fx={"raady": ("caster", -1), "ready": ("caster", -1), "circle": ("caster", -1), "ground": ("caster", -1), "pumpkin": ("target", 3100)}),
    "실비아": dict(raw=r'''            ["실비아"] = new UltFit { Hits = H(1400, 1500), Move = "none", Fx = F(("ultimate_statue", "target!", 600), ("ultimate_hit", "target!", 1350)),   // (10-17 정밀 분석) 분수는 적 무리 가운데 고정 · 맞음 2번
                Note = "2026-10-13 석상이 떨어져 닿는 때(1.25~1.4초)에 둘째 타격 — 원작 영상 피해 1.37초" },'''),
    "우로스": dict(raw=r'''            ["우로스"] = new UltFit { Hits = H(1933, 3100), Fx = F(("dash_jolly", "caster", 1400), ("slash_jolly", "target", 1867), ("fire_jolly", "target", 3050), ("snake_jolly", "target!", 3050), ("hit_jolly", "target", 3100)), Note = "2026-10-14 한 바퀴: 1.5초 앞으로 사라져 적 무리 뒤에 나타남(1.8) → 왼쪽으로 베며 무리를 가로지름(1.93 타격) → 왼끝까지 갔다 제자리(3.0) → 적 자리 불기둥 · 금빛 용(3.1 타격) · 무리 뒤 자리는 ULT_LOOP" },'''),
    "코미_수영복": dict(nat=True, hits=[300, 2000, 2667, 3333, 4000, 4667], move=("none",), fx={"waterfall": ("target", 100)}, note="물기둥이 시작하자마자 침 · 코미는 제자리에서 뛰어오름"),
    "죠안": dict(raw=r'''            ["죠안"] = new UltFit { Fx = F(("explosion_2", "target!", 633), ("wing_1", "caster", 2133), ("explosion_3", "target!", 2233), ("ground_1", "target!", 2233)), Hits = H(2233), Cap = new Dictionary<string, float> { ["explosion_2"] = 1.4f }, Note = "(3차: 적 사슬은 변신(2.13초) 때 딱 걷힌다 — 633 + 1.4초) (10-17 2차 사용자) 사슬은 상대에게 생겨(explosion_2 적) 변신하며 조이며 터진다 · 예전: 변신 전 사슬이 몸을 감았다가(explosion_2 633) 날개 변신(2133)하며 풀리면서 피해(2233)" },'''),
    "란": dict(raw=r'''            // (fix_Y 1) 원작은 1_1 한 번 뒤 1_2 세 번(1_2 시작 = 3300 · 5467 · 7634) · 늑대 표식은 skill_ready_1 장 · 달 · X 장은 원점이 화면 가운데(midx)
            ["란"] = new UltFit { Move = "none", Chain = new[] { "Ultimate1_1", "Ultimate1_2", "Ultimate1_2", "Ultimate1_2" }, Speed = 1.32f,   // Speed 는 원작 그대로가 아닐 때만 — 원작 그대로는 배속표(ORIG_SPEED 1.31)
                Hits = H(1500, 1767, 2033, 3733, 4000, 4267, 5900, 6167, 6434, 8067, 8334, 8601),   // 1_1 이벤트 1500 · 1767 · 2033 · 1_2 이벤트 433 · 700 · 967
                Add = new[] { "fx_ran_skill_ready_1_default" },
                Fx = FM(("ultimate_cast_2", null, 33, 1f, new[] { 3333, 5500, 7667 }),   // 금빛 섬광 구(1_1 · 1_2 마다)
                        ("ultimate_cast_4", "midx", 233, 1f, null), ("ultimate_cast_5", "midx", 233, 1f, null),   // 달 · X(장 안 지연 2.0 → 스파인 2.28초)
                        ("ultimate_slash_1", "target!", 1400, 1f, null),   // 이벤트 1467(영상 1.0~1.1) — 세로 섬광 · 고리 베기 여러 겹
                        ("ultimate_slash_2", "target!", 3700, 1f, new[] { 5867, 8034 }),   // 1_2 이벤트 400/433 — 사선 · Z · X 고리
                        ("skill_ready_1", "body", 2300, 1f, new[] { 4533, 6700, 8867 })),   // 늑대 표식 — 이벤트 1009123(1_1 2300 · 1_2 1233)
                Note = "원작: 1_1 한 번 뒤 1_2 세 번(영상 7.5초 = 스파인 9.8초 ÷ 1.32)" },'''),
    "우이_기억": dict(raw=r'''            ["우이_기억"] = new UltFit { Fx = F(("garden_1", "target!", -1), ("garden_end", "target!", -1), ("garden_start", "caster", -1), ("step_1", "caster", -1)), Hits = H(3200, 4000, 4800), Note = "(10-17 사용자 「상대에게 웅덩이 생기고 피해」) · 2026-10-14 원작 대조: 연못 · 물 돔은 시전자(아군) 쪽 — 적 발밑 연못은 원작에 없다" },'''),
    "우이": dict(raw=r'''            ["우이"] = new UltFit { Hits = H(1833, 2233, 2633), Move = "none", Fx = F(("flower_1", "caster!", 200)), Note = "2026-10-13 타격이 0ms(몸짓 전)에 들던 것 — 원작 영상 분홍 꽃 폭발(1.8~2.0) · 버섯 연기(2.3)에" },'''),
    "티그": dict(nat=True, hits=[840], move=("leap", 420, 800, 3167, 300, "front"), fx={"aura": ("caster", 1800)}, note="뛰어 베기 먼저 · 호랑이 오라는 착지 뒤"),
    "아일라": dict(nat=True, hits=[2433, 3400], note="영상: 2.3초 베기는 화상만 · 피해는 4.1초 운석 폭발 → 베기 · 운석 두 박자(운석 장은 없음)"),
    "롤렛": dict(nat=True, hits=[2333], fx={"box": ("target", 1300)}, note="영상: 상자 등장 1.3초 → 걸어가 3.6초 폭발 피해 → 상자 먼저 · 폭발 타격은 둘째 이벤트"),
    "사리": dict(hits=[604], note="영상: 하늘 번개 한 번(0.4초) 피해 1회 → 둘째 표시 타격 뺌"),
    "디아나": dict(hits=[1403, 1473, 1543, 1613, 1683, 1753, 1823, 1893, 1963, 2033, 2103], fx={"diana_ultimate_3": ("caster", -1), "ground_1": ("caster", -1)},
                 note="영상: 번쩍 직후 0.5초 안에 다단 · 초록 초승달은 디아나 앞에서 쓸고 감(첫 타격 1433 정권은 2026-10-06 사용자 결정대로 둠)"),
}


def ms(v, nat):
    return int(round(v if nat else v * BASE))


def cs(hero, f):
    if "raw" in f: return f["raw"]   # 손으로 다듬은 C# 줄(앞 주석 줄까지) 그대로
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
