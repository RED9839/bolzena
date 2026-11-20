using System.Collections.Generic;
using Bolzena.Battle;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 사도별 연출 값 표(2026-11-10, 11주차) — 고학년 · 탄 연출에서 사도 id 로 찾는 표를 BattleDirector.Acts.cs 에서 그대로 옮겼다.
    //   카드 몸짓 표는 BattleDirector.Cards.cs, 동물 · 떨어뜨리기는 BattleDirector.Pets.cs 에 있다. 사도를 늘리거나 손볼 때 여기만 본다.
    //   값은 옮기기 전과 같다(ADR-002 · 고학년 점검 전/후 비교).
    public partial class BattleDirector
    {
        // 화면 밖 막기 — 고학년 몸짓 동안 몸(메시 경계)의 왼끝이 화면 왼끝 + 여백보다 밖이면 그만큼 몸(Art)을 오른쪽으로 당긴다(부드럽게).
        //   끝나면 0.25초에 제자리로. 몸짓 자체 · 이동(UltTravelCo — 몸 전체 transform)은 그대로 두고 Art 만 민다
        // ── 고학년 가운데의 큰 클로즈업(2026-10-14 사용자 「몸이 화면 밖으로 사라지고 화면에 스탠딩이 생기는 연출」 → 「고학년 전용 모션 스탠딩」) ──
        //   비비(신성) 원작 영상(×1.2 = 스파인 초): 1.75~2.4 몸이 화면 위로 날아가 사라짐 → 2.6 화면이 더 어두워지고 클로즈업이 오른쪽 아래에서 휘돌아 들어와 왼쪽에 섬
        //   → 3.95 왼손을 적 쪽으로 뻗고 레이저 → 4.4 · 5.3 타격 · 5.15 빛기둥 → 5.8 클로즈업이 위로 날아 나감 → 6.4~7.2 몸이 내려와 제자리(스파인 몸짓 그대로 — 막지 않는다).
        //   그림은 원작 고학년 전용 스파인(spine/ingame/effect/vividivineult · Highlight_1 4.133초 · 2026-10-14 기기에서 뽑음) — 들어옴 · 섬 · 나감이 모두 그 몸짓 안에 있어 우리는 틀기만 한다.
        //   Start: 몸짓을 시작하는 스파인 초. 원작 영상 프레임에 뼈 실루엣을 겹쳐 맞춤(몸짓 0.8초 = 영상 2.54초, 3.85초 = 4.87초 → 사이 2.1초).
        //   자리 · 크기: 영상(440×428, 높이 = 화면 높이, 왼끝 = 화면 왼끝)에서 스파인 원점이 (324, 252)px, 스파인 1 단위 = 0.12px.
        //   스파인 이벤트(Event · SFX · Voice)는 정의만 있고 Highlight_1 에 찍힌 것이 없다 — 소리 · 타격 시각은 지금 계획(UltVideoFit) 그대로.
        //   원작 레이저는 UI 전용 이펙트 프리팹(fx_ui_vividivine_ultimate_attack_*)이라 아직 없어 SD 고학년 레이저 장(laser_one)으로 대신 —
        //   쏘는 자리는 이 스파인의 Point_Ult 뼈(원작 이펙트가 붙는 자리 · 몸 뒤 왼쪽 아래 — 영상의 빛줄기도 거기서 나온다). SD 쪽 레이저는 계획에서 뺀다(Drop)
        class UltStanding { public string Spine, Drop, Fx; public float Start, Laser, Out; public float OX = 324f / 428f, OY = 252f / 428f, Unit = 0.12f / 428f; }

        static readonly Dictionary<string, UltStanding> ULT_STANDING = new Dictionary<string, UltStanding>
        {
            ["비비_신성"] = new UltStanding { Spine = "vividivineult", Start = 2.1f, Laser = 3.4f, Out = 5.8f, Drop = "full_laser", Fx = "fx_vividivine_ultimate_full_laser_one_jolly_default" },
        };

        static readonly HashSet<string> KEEP_ON_SCREEN = new HashSet<string> { "버터", "셰이디_역전" };   // 원작 영상 비교: 몸짓 루트 이동이 화면 왼쪽 밖으로(우로스는 ULT_LOOP 가 같이 맡는다)

        //   비비(신성)는 뺐다(2026-10-14) — 원작도 1.45~2.4초에 화면 위로 날아올라 나간 뒤 숨는다(퇴장). 내리면 화면 안 허공에 멈췄다 사라졌다
        static readonly HashSet<string> ULT_KEEP_TOP = new HashSet<string> { "에르핀_왕도" };   // 2026-10-13 고학년 캡처: 뛰어오른 몸이 화면 위 가장자리에 잘림

        // ── 적 무리 한 바퀴(2026-10-14 사용자 「우로스 — 적을 한 바퀴 돌고 불기둥 솟는 거」) ──
        //   우로스 Ultimate1_1 은 몸짓 자체가 원을 그린다(스파인 루트, 키 H 단위): 1.5초 앞 +3.1H 로 사라짐(숨김 1.5~1.8) → 1.8초 +1.8H 에 나타나
        //   왼쪽으로 베며 지나감(1.93 타격) → 2.1~2.9초 −3H(왼끝) → 3.0~3.1초 제자리 → 3.1초 불기둥. 원작은 적이 바로 앞(약 1H)이라 +1.8H 가 적 뒤였는데
        //   우리 판은 적이 멀어(뒷줄) 적 앞 허공만 베고 지나갔다. → 숨은 동안(From) 몸을 「살아 있는 적 무리 오른끝 뒤」로 옮겨 두고(나타나는 자리 = 무리 뒤),
        //   Hold 부터 Back 까지 그 몫을 0 으로 풀어 왼쪽으로 베는 몸이 무리를 가로질러 제자리 쪽으로 돌아오게 한다. 시각은 스파인 조각 시계(TrackTime, 1배속 ms)
        //   Gap: 나타나는 몸 왼끝이 무리 오른끝에서 떨어지는 몫(H). 화면 양 끝 밖으로는 나가지 않게 같이 막는다(KeepOnScreen 과 같은 셈)
        class UltLoop { public int From, Hold, Back; public float Gap; }

        static readonly Dictionary<string, UltLoop> ULT_LOOP = new Dictionary<string, UltLoop>
        {
            ["우로스"] = new UltLoop { From = 1500, Hold = 1900, Back = 2100, Gap = 0.3f },
        };

        // 변신 · 태세가 시간이 다 돼 풀릴 때(풀림 까닭 time) 트는 몸짓 — 사도 id → 스파인 조각
        static readonly Dictionary<string, string> FORM_EXPIRE_MOTION = new Dictionary<string, string>
        {
            ["클로에"] = "Attack2_1",   // 2026-10-18 탑승 리뉴얼 — 내리며 두 팔 내려찍기(원작 강화 공격 · 맞음 1433), 적 전체 80%(offExpire)
        };

        // 손으로 정한 건너뛰기(초, 1배속) — 시전 되풀이가 이벤트로 이어져 빈 구간으로 안 잡히는 사도. 리뉴아: 1.2~11.2초 시전 고리(첫 타격 11.6초 → 1.6초)
        static readonly Dictionary<string, List<(float from, float to)>> CUT_FIX = new Dictionary<string, List<(float, float)>>
        {
            // (리뉴아 1.2~11.2초 건너뛰기는 원작 가운데 타격까지 지워서 뺐다 — 사용자 원칙: 앞 준비 · 뒤 꼬리만)
        };

        // 쏘는 한 대의 맞는 장을 손으로 정한 사도(2026-10-13) — 평타에 「_hit」 장이 없거나 맞지 않는 것: (장, 크기)
        //   이드: attackhit 는 연기가 위로 크게 솟는 판(화면 위까지) → 작게 · 우이(기억): 맞음 장이 없어 공용 타격 → 물방울 폭발(attack1_explosion)
        static readonly Dictionary<string, (string fx, float scale)> SHOT_HIT = new Dictionary<string, (string fx, float scale)>
        {
            ["이드"] = ("fx_ed_attackhit_1", 0.5f),
            ["우이_기억"] = ("fx_uimemory_attack1_explosion_1_default", 0.6f),
            ["우이"] = ("fx_uieru_waterhit_1", 0.75f),
        };

        // 위에서 내리치는 사도(2026-10-13) — 지팡이 끝이 머리 위라 탄이 적 머리 위에서 세로로 꽂혀 바닥 아래까지 갔다(네르(빡침) 벼락).
        //   적 몸 위(dy)에서 몸 가운데 조금 위로 짧게 떨어뜨리고, 판을 돌리지 않고 작게 — 판 아래 끝이 발밑을 넘지 않게
        static readonly Dictionary<string, (float dy, float scale)> SHOT_DROP = new Dictionary<string, (float dy, float scale)>
        {
            ["네르_빡침"] = (2.2f, 0.55f),
        };

        // 사도 원작 평타 이펙트에서 — 날아갈 작은 것(proj · bullet, 꼬리 빼고) · 맞는 장(hit). SHOT_HIT 가 있으면 그 장
        // 추가 공격 쏘는 장을 사도별로(빈 글 = 쏘는 장 없이 몸 앞 빛) — 「proj」 첫 장이 강공용인 사도(디아나(왕년) 던지는 바위가 공중에 떠 보였다, 2026-10-14)
        static readonly Dictionary<string, string> SHOT_PROJ = new Dictionary<string, string> { ["디아나_왕년"] = "", ["시온더다크불릿"] = "", ["우로스"] = "", ["죠안"] = "" };   // 우로스(근접) · 죠안(표식 → 터짐)은 평타에 나는 탄이 없다(원작 대조 10-15)

        // 조준선을 띄우고 사라질 때 맞히는 사도 — (조준선 장, 기다림 초). 탄이 날지 않는다(시온, 2026-10-15 사용자 · 원작 평타 영상 1.2 → 1.55초)
        // 광선으로 맞히는 사도 — 연계도 쏘는 자리에서 적 쪽으로 돌린 레이저 판(이드 평타 attack_1_1, 원작 대조 10-15)
        static readonly Dictionary<string, string> SHOT_BEAM = new Dictionary<string, string> { ["이드"] = "fx_ed_attack_1_1" };

        static readonly Dictionary<string, (string fx, float wait)> SHOT_MARK = new Dictionary<string, (string fx, float wait)> { ["시온더다크불릿"] = ("fx_xxionx_crosshair_1", 0.35f), ["죠안"] = ("fx_joanne_attack1_1_ready_1", 0.4f) };

        // ── 사도 이름으로 갈리던 분기(11주차에 Acts.cs 본문에서 옮김) ──
        // 란 — 고학년은 늘 첫 갈래(Ultimate1_1, 달 · 몰이 4타). 1_2 는 처치 뒤 다시 쓸 때만(엔진 ifKill), 타격은 적마다 차례로.
        static readonly HashSet<string> ULT_REUSE_ON_KILL = new HashSet<string> { "란" };
        static int? UltFirstBranch(string hid) => hid != null && ULT_REUSE_ON_KILL.Contains(hid) ? 0 : (int?)null;
        static bool UltReuseOnKill(string hid) => hid != null && ULT_REUSE_ON_KILL.Contains(hid);

        // 티그(영웅) 강공 — 가까운 적이면 검기(proj) 없이 cast_1 올려 베기로 맞힌다(10-18 4차). TigNear 는 몸짓마다 정한다.
        static readonly HashSet<string> NEAR_UPPERCUT = new HashSet<string> { "티그_영웅" };
        bool NearUpperCut(string hid, Motion m) => hid != null && NEAR_UPPERCUT.Contains(hid) && m == Motion.Attack2 && TigNear;
    }
}
