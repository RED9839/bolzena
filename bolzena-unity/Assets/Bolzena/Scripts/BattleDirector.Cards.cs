using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using Spine;
using UnityEngine;
using Motion = Bolzena.Battle.Motion;

namespace Bolzena
{
    // 카드 몸짓(평타 Attack1 · 강공 Attack2 · 저학년 Skill1)을 원작 영상에 맞춘다(2026-10-06 카드 몸짓 대조 135명 · card_sheets/_review_1~4).
    //   · 몸짓을 「마지막 타격 + 0.32초」에서 끊지 않고 애니 끝까지 튼다(속도 1.3배 그대로). 길면 상한까지 앞 준비 · 빈 구간 · 꼬리부터 줄인다
    //   · 이어지는 조각(_Loop · _End · _2 …)을 고학년처럼 잇는다(표 CARD_CHAIN)
    //   · 원작이 자기 · 아군에게 거는 몸짓은 제자리에서, 이펙트는 시전자 쪽에(표 CARD_SELF) · 원작이 제자리 · 돌진인 몸짓(CARD_STAY · CARD_DASH)
    //   · 때리는 순간은 스파인 타격 이벤트(0ms 시전 표시는 빼고), 원작 타수대로 보이기만 나눈다(엔진 피해 합은 그대로)
    public partial class BattleDirector
    {
        // 카드 몸짓 길이 상한(화면 초, 1.3배속 뒤). 넘으면 앞 준비 · 빈 구간 · 꼬리부터 줄인다
        public const float CARD_MAX_A1 = 1.5f, CARD_MAX_A2 = 1.8f, CARD_MAX_S1 = 2.5f;
        const float CARD_SPEED_MAX = 1.5f;     // 줄여도 상한을 0.4초 넘게 넘으면 이 배속까지만 빠르게
        const int CARD_HITS_MAX = 5;           // 원작 타수대로 나누어 보일 때 많아야
        const float CARD_TAIL_KEEP = 0.45f;    // 마지막 타격 뒤 남기는 몫(1배속 초) — 회수 동작
        const float CARD_PRE_KEEP = 0.35f;     // 첫 타격 앞 남기는 몫(1배속 초) — 휘두르기

        // 점검(-cardsheet) — 마지막 카드 몸짓
        public string CardAnimLog = "";
        public int CardNaturalMs, CardShownMs, CardHitsShown;
        public float CardSpeed = 1.3f;
        public string CardMoveLog = "";

        static string MKey(string hid, Motion m) => hid + "|" + m;

        // 이어 트는 조각 — 사도|몸짓 → 조각들. 「a/b」는 그중 하나(같은 칸 번호끼리 짝 — 마요(멋짐) Start2 면 End2).
        //   스켈레톤 이벤트로 가렸다(card_fix/anims.json): 새 몸짓 표시(1000003)나 같은 짜임의 다른 판(이드 Attack1_1~4 · 앨리스 Skill1_1~3)은 갈래라 잇지 않는다
        static readonly Dictionary<string, string[]> CARD_CHAIN = new Dictionary<string, string[]>
        {
            [MKey("에르핀_왕도", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_1_Loop", "Skill1_1_End" },
            [MKey("마요_멋짐", Motion.Skill1)] = new[] { "Skill1_1_Start1/Skill1_1_Start2", "Skill1_1_Loop", "Skill1_1_End1/Skill1_1_End2" },
            [MKey("나이아", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2", "Skill1_3", "Skill1_4" },
            [MKey("바롱", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3" },
            [MKey("사리", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2" },
            [MKey("셰이디", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2" },
            [MKey("루포", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2", "Skill1_3", "Skill1_4", "Skill1_5", "Skill1_6", "Skill1_7" },
            [MKey("슈팡", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3", "Skill1_4_Loop", "Skill1_5" },
            [MKey("시온더다크불릿", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_1_Loop", "Skill1_2" },
            [MKey("스노키", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2/Skill1_3" },
            [MKey("아사나", Motion.Attack2)] = new[] { "Attack2_1", "Attack2_2/Attack2_3" },
            [MKey("아사나", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2/Skill1_3" },
            [MKey("키디언", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3", "Skill1_4" },
            [MKey("바리에", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2_Loop", "Skill1_3", "Skill1_4" },
            [MKey("로네_시장", Motion.Skill1)] = new[] { "Skill1_1", "Skill1_2" },
            // 앨리스 — 1_1 불꽃 · 1_2 번개 · 1_3 회오리는 잇는 조각이 아니라 갈래(같은 짜임). 원작은 쓸 때마다 다른 판이 나와 하나를 무작위로(재검토 3조)
            // 티그(영웅) 강공 — 원작 영상 5.6초 · 6타 이상인데 Attack2_1 은 2.8초뿐. Attack2_2 를 이어 튼다(재검토 4조 짐작 · 상한 1.8초로 줄인다)
            [MKey("티그_영웅", Motion.Attack2)] = new[] { "Attack2_1", "Attack2_2" },
            [MKey("앨리스", Motion.Skill1)] = new[] { "Skill1_1/Skill1_2/Skill1_3" },
            // 네티 — 조각 셋(Start · Loop · End)을 원작이 한 판으로 묶은 _Full 이 있다(전엔 _End 만 600ms)
            [MKey("네티", Motion.Attack1)] = new[] { "Attack1_1_Full" },
            [MKey("네티", Motion.Attack2)] = new[] { "Attack2_1_Full" },
            // 하이디 강공 — 원작은 상자 변신 모습의 연타(Attack1_1_Change). Attack2_1 이 없다고 평타를 틀던 것
            [MKey("하이디", Motion.Attack2)] = new[] { "Attack1_1_Change" },
        };

        // 원작이 자기 · 아군에게 거는 몸짓(강화 · 회복 · 실드 · 보호막) — 제자리에서, 몸짓 이펙트는 시전자 쪽에. 피해 숫자 · 맞음만 적에게
        static readonly HashSet<string> CARD_SELF = new HashSet<string>
        {
            // 저학년(대조 1~4조)
            MKey("다야_퓨어샤인", Motion.Skill1), MKey("라이카", Motion.Skill1), MKey("리코타", Motion.Skill1), MKey("빅우드", Motion.Skill1),
            MKey("셰럼", Motion.Skill1), MKey("아네트", Motion.Skill1), MKey("아일라", Motion.Skill1), MKey("오팔", Motion.Skill1),
            MKey("우이_기억", Motion.Skill1), MKey("코미_수영복", Motion.Skill1), MKey("스피키", Motion.Skill1), MKey("벨벳", Motion.Skill1),
            MKey("바리에", Motion.Skill1), MKey("스노키", Motion.Skill1), MKey("아라그니아", Motion.Skill1), MKey("레테", Motion.Skill1),
            MKey("큐이", Motion.Skill1), MKey("쵸피", Motion.Skill1),
            MKey("로네", Motion.Skill1), MKey("오로라", Motion.Skill1), MKey("이드", Motion.Skill1), MKey("우로스", Motion.Skill1),
            MKey("힐데", Motion.Skill1), MKey("캬롯", Motion.Skill1), MKey("포셔", Motion.Skill1),
            MKey("네르", Motion.Skill1), MKey("바나", Motion.Skill1), MKey("베니", Motion.Skill1), MKey("아멜리아_R41", Motion.Skill1),
            MKey("코미", Motion.Skill1), MKey("폴랑", Motion.Skill1), MKey("마고", Motion.Skill1),
            // 강공
            MKey("아일라", Motion.Attack2), MKey("우이_기억", Motion.Attack2), MKey("시저", Motion.Attack2), MKey("뮤트", Motion.Attack2),
            MKey("헤일리", Motion.Attack2), MKey("힐데", Motion.Attack2), MKey("캬롯", Motion.Attack2), MKey("제이드", Motion.Attack2),
            MKey("아멜리아_R41", Motion.Attack2), MKey("에르핀", Motion.Attack2), MKey("슈팡", Motion.Attack2),
            MKey("키샤", Motion.Attack2),   // 폴라로이드 액자 연출은 키샤 앞(적 쪽에 찍혀 화면 오른쪽을 덮던 것)
            MKey("키샤", Motion.Skill1), MKey("레비_졸업", Motion.Skill1), MKey("레비_졸업", Motion.Attack2), MKey("페스타", Motion.Skill1),   // 키샤 무대 원판 · 레비(졸업) 착지 폭발은 시전자 자리(1차 재촬영 확인)
        };

        // 원작이 제자리에서 하는 몸짓(근접 그림이라도 달려가지 않는다 — 이펙트는 그대로 적에게)
        static readonly HashSet<string> CARD_STAY = new HashSet<string>
        {
            MKey("리코타", Motion.Attack1), MKey("리코타", Motion.Attack2), MKey("라이카", Motion.Attack2), MKey("레이지", Motion.Skill1),
            MKey("리온", Motion.Skill1), MKey("벨라", Motion.Skill1), MKey("리츠", Motion.Attack1), MKey("리츠", Motion.Attack2), MKey("리츠", Motion.Skill1),
            MKey("슈로", Motion.Skill1), MKey("레비_졸업", Motion.Attack2),
        };

        // 원작이 적 앞까지 가는 몸짓(근접 그림이 아니어도) — 몸짓이 스스로 멀리 옮기면(순간이동) 따로 가지 않는다
        static readonly HashSet<string> CARD_DASH = new HashSet<string>
        {
            MKey("뮤트", Motion.Skill1), MKey("슈팡", Motion.Skill1), MKey("다야", Motion.Skill1), MKey("헤일리_멀쩡", Motion.Attack1), MKey("스노키", Motion.Attack2),
        };

        // 이펙트 갈래를 고를 이름(FxRules.CardKinds) — 트는 조각 이름과 다를 때. 하이디 강공: 몸짓은 Attack1_1_Change 지만 이펙트는 원작 강공(attack2 사격)
        static readonly Dictionary<string, string> CARD_FX_ANIM = new Dictionary<string, string>
        {
            [MKey("하이디", Motion.Attack2)] = "Attack2_1",
        };
        // 원작 영상에서 읽은 타격 시각(ms, 1배속 · 몸짓 시작에서) — 스파인 타격 이벤트가 원작 박자와 다를 때만.
        //   죠안 평타: 애니에 Event 가 없고 SFX 2 가 1300ms 뿐이라 탄이 1초 넘게 떠 있다가 맞았다 — 원작은 0.3초에 첫 발, 1.3초에 둘째
        static readonly Dictionary<string, int[]> CARD_HITS = new Dictionary<string, int[]>
        {
            [MKey("죠안", Motion.Attack1)] = new[] { 300, 1300 },
            // 재검토(2026-10-06 2차): 첫 이벤트가 이펙트 시작 표시라 너무 일찍 맞던 것 · 나눈 타수가 원작보다 많던 것
            [MKey("요미", Motion.Skill1)] = new[] { 1700 },                 // 300 은 달 소환 표시 — 원작 61% 지점
            [MKey("사리", Motion.Skill1)] = new[] { 1967 },                 // 원작 1타(80%) — 연기 뒤 Skill1_2 의 찌르기(1567 + 400)
            [MKey("델리아", Motion.Skill1)] = new[] { 667, 1800 },          // 원작 2타
        };
        static List<float> CardHitsFix(string hid, Motion m, List<float> marks)
        {
            if (!CARD_HITS.TryGetValue(MKey(hid, m), out var ms)) return marks;
            var r = new List<float>(); foreach (var x in ms) r.Add(x / 1000f); return r;
        }

        // 투사체 비행(0.33초)을 타격에 그대로 더하는 사도 — 이 사도들은 스파인 이벤트가 쏘는 때라 빼면 원작보다 일찍 맞았다(재검토 4조: 시온 평타 1.6 → 1.04, 칸나 0.6 → 0.39, 에피카 54% → 29%)
        static readonly HashSet<string> CARD_LAG_KEEP = new HashSet<string> { "시온더다크불릿", "칸나", "에피카" };
        public static bool CardLagKeep(string hid) => CARD_LAG_KEEP.Contains(hid);

        static string CardFxAnim(string hid, Motion m, string first) => CARD_FX_ANIM.TryGetValue(MKey(hid, m), out var a) ? a : first;

        public static bool CardSelf(string hid, Motion m) => CARD_SELF.Contains(MKey(hid, m));

        // 이 카드 몸짓이 이어 틀 조각들(첫 조각 = 기존 AnimFor). 표에 없으면 그 한 조각
        static List<string> CardChain(UnitView u, string hid, Motion m, string first)
        {
            var res = new List<string>();
            if (u == null || !u.SpineArt || u.SkelData == null || u.FormAttack != null) { res.Add(first); return res; }
            if (!CARD_CHAIN.TryGetValue(MKey(hid, m), out var spec)) { res.Add(first); return res; }
            int pick = -1;
            foreach (var slot in spec)
            {
                var alts = slot.Split('/');
                string n;
                if (alts.Length > 1)
                {
                    if (pick < 0) pick = Random.Range(0, alts.Length);
                    n = alts[Mathf.Min(pick, alts.Length - 1)];
                }
                else n = alts[0];
                if (u.Has(n)) res.Add(n);
            }
            if (res.Count == 0) res.Add(first);
            return res;
        }

        // 조각들의 때리는 순간(1배속 초, 이어 튼 전체 시각) — UnitView.Strikes 와 같은 규칙을 조각 전체에.
        //   0ms(조각 첫머리 0.05초 안) 시전 표시 이벤트는 뒤에 다른 타격 이벤트가 있으면 첫 타격으로 잡지 않는다(이드(재활) 저학년 · 슈로 강공 · 아멜리아(R41) 강공 · 키디언 저학년).
        //   여러 번 때리는 창은 4초까지, 0.2초 안에 붙은 것은 하나로(이펙트 표시 + 맞는 소리 짝)
        static List<float> CardStrikes(UnitView u, List<string> names, out float total)
        {
            total = 0;
            var res = new List<float>();
            var sd = u.SkelData;
            var evs = new List<(float t, Spine.Event e)>();
            foreach (var n in names)
            {
                var a = sd.FindAnimation(u.F(n));
                if (a == null) continue;
                foreach (var tl in a.Timelines) if (tl is EventTimeline et) foreach (var ev in et.Events) evs.Add((total + ev.Time, ev));
                total += a.Duration;
            }
            evs.Sort((x, y) => x.t.CompareTo(y.t));
            bool Own(Spine.Event e)
            {
                if (e.Data.Name != "Event" && e.Data.Name != "Event_2") return false;
                foreach (var s in (e.String ?? "").Split(','))
                    if (int.TryParse(s, out var v) && v >= 1000100 && v != 1000013 && v != 1000014) return true;
                return false;
            }
            bool Sfx2(Spine.Event e) => e.Data.Name == "SFX" && int.TryParse(e.String, out var v) && v >= 2;
            bool Late((float t, Spine.Event e) x) => x.t >= 0.05f;
            int iS = evs.FindIndex(x => Sfx2(x.e) && Late(x)), iO = evs.FindIndex(x => Own(x.e) && Late(x));
            if (iS < 0 && iO < 0) { iS = evs.FindIndex(x => Sfx2(x.e)); iO = evs.FindIndex(x => Own(x.e)); }
            float at;
            if (iS >= 0 && iO >= 0 && Mathf.Abs(evs[iS].t - evs[iO].t) <= 0.4f) at = Mathf.Max(evs[iS].t, evs[iO].t);   // 이펙트 표시가 주먹보다 먼저 오는 일(디아나)
            else if (iS >= 0) at = evs[iS].t;
            else if (iO >= 0) at = evs[iO].t;
            else at = (names.Count > 0 ? u.Duration(u.F(names[0])) : total) * 0.45f;
            res.Add(at);
            foreach (var (t, e) in evs)
            {
                if (t <= at + 0.001f || t > at + 4f) continue;
                if (!(Own(e) || Sfx2(e))) continue;
                if (t - res[res.Count - 1] < 0.2f) continue;
                res.Add(t);
            }
            return res;
        }

        // 카드 몸짓 줄이기 — 상한(화면 초 cap)을 넘으면 꼬리(마지막 타격 + 0.45초 뒤) → 앞 준비(첫 타격 0.35초 전까지, 조각마다) → 가운데 빈 구간(0.8초 넘는 사이)
        //   → 그래도 남으면 꼬리를 마지막 타격 + 0.2초까지. cutSkips · cutEnd(고학년과 같은 칸)에 적고 Rm 으로 시각을 옮긴다. 돌려주는 것: 그래도 넘는 몫(1배속 초)
        float CardCut(UnitView u, List<string> names, List<float> hits, float total, float cap, float speed)
        {
            float need = total - cap * speed;
            if (need <= 0.05f || hits.Count == 0) return 0;
            var offs = new List<float>(); float o = 0;
            var evAll = new List<float>();
            foreach (var n in names)
            {
                offs.Add(o);
                var a = u.SkelData.FindAnimation(u.F(n));
                if (a == null) continue;
                foreach (var tl in a.Timelines) if (tl is EventTimeline et) foreach (var ev in et.Events) evAll.Add(o + ev.Time);
                o += a.Duration;
            }
            offs.Add(o);
            int PieceOf(float t) { int p = 0; for (int i = 0; i + 1 < offs.Count; i++) if (t >= offs[i]) p = i; return p; }
            float first = hits[0], last = hits[hits.Count - 1];
            // 1) · 2) 꼬리와 앞 준비를 남은 몫에 비례해 같이 줄인다 — 꼬리부터 다 덜면 타격이 몸짓 끝 쪽(75~85%)으로 밀렸다(재검토 1조 강공).
            //   앞 준비는 첫 타격이 든 조각까지, 앞 조각은 거의 통째로(끝 0.03초 남김 — 다음 조각으로 넘어가게), 그 조각은 첫 타격 0.35초 전까지
            float tailRoom = Mathf.Max(0, total - (last + CARD_TAIL_KEEP));
            int pf = PieceOf(first);
            var fr = new List<(float a, float b)>(); float frontRoom = 0;
            for (int p = 0; p <= pf; p++)
            {
                float a = offs[p] + (p == 0 ? 0.1f : 0.02f);
                float b = p < pf ? offs[p + 1] - 0.03f : first - CARD_PRE_KEEP;
                if (b - a < 0.1f) continue;
                fr.Add((a, b)); frontRoom += b - a;
            }
            float room = tailRoom + frontRoom;
            float wantTail = room > 0 ? Mathf.Min(tailRoom, need * tailRoom / room) : 0;
            float wantFront = Mathf.Min(frontRoom, need - wantTail);
            if (wantTail > 0.05f) { cutEnd = total - wantTail; need -= wantTail; }
            for (int i = 0; i < fr.Count && wantFront > 0.05f; i++)
            {
                float c = Mathf.Min(wantFront, fr[i].b - fr[i].a);
                cutSkips.Add((fr[i].b - c, fr[i].b)); need -= c; wantFront -= c;
            }
            // 3) 가운데 빈 구간 — 첫 ~ 마지막 타격 사이 이벤트 없는 0.8초 넘는 틈(같은 조각 안), 앞뒤 0.25초 남김
            if (need > 0.05f)
            {
                var pts = new List<float>(hits);
                foreach (var t in evAll) if (t > first && t < last) pts.Add(t);
                for (int i = 0; i + 1 < offs.Count; i++) if (offs[i] > first && offs[i] < last) pts.Add(offs[i]);
                pts.Sort();
                for (int i = 0; i + 1 < pts.Count && need > 0.05f; i++)
                {
                    float a = pts[i] + 0.25f, b = pts[i + 1] - 0.25f;
                    if (b - a < 0.3f || PieceOf(a) != PieceOf(b)) continue;
                    float c = Mathf.Min(need, b - a);
                    cutSkips.Add((a, a + c)); need -= c;
                }
            }
            // 4) 꼬리를 더
            if (need > 0.05f)
            {
                float end0 = cutEnd < float.MaxValue ? cutEnd : total;
                float c = Mathf.Min(need, Mathf.Max(0, end0 - (last + 0.2f)));
                if (c > 0.02f) { cutEnd = end0 - c; need -= c; }
            }
            cutSkips.Sort((x, y) => x.from.CompareTo(y.from));
            return Mathf.Max(0, need);
        }

        static float CardCap(Motion m) => m == Motion.Skill1 ? CARD_MAX_S1 : m == Motion.Attack2 ? CARD_MAX_A2 : CARD_MAX_A1;

        // 카드 몸짓 이동 — 근접 그림이면 달려가되 원작이 제자리 · 자기형이면 안 간다. 원작이 돌진이면(표) 간다(몸짓이 스스로 멀리 옮기면 빼고)
        bool CardDashes(UnitView u, string key, string hid, Motion m, List<string> chain, int targetCount)
        {
            if (targetCount == 0) { CardMoveLog = "대상 없음"; return false; }
            string k = MKey(hid, m);
            if (CARD_SELF.Contains(k)) { CardMoveLog = "자기형 제자리"; return false; }
            if (CARD_STAY.Contains(k)) { CardMoveLog = "원작 제자리"; return false; }
            if (CARD_DASH.Contains(k))
            {
                float tr = u.Travel(chain);
                CardMoveLog = tr < SELF_TRAVEL ? "원작 돌진" : $"원작 돌진 · 몸짓이 스스로 {tr:F1}";
                return tr < SELF_TRAVEL;
            }
            bool melee = Look.MeleeArt(key);
            CardMoveLog = melee ? "근접" : "제자리";
            return melee;
        }

        // 저학년 시작 섬광 — 원작은 저학년마다 0.0~0.3초에 시전자 몸에서 흰 · 푸른 구슬이 터지고(별빛 번쩍) 고리가 퍼진다(힐데 · 가비아 영상).
        //   원작 공용 장을 _fxraw · MuMu 추출본에서 못 찾아(이름 skill_start · cast · common 다 봤다) 볼제나 공용 낱장으로 짓는다
        void CardOrb(UnitView u)
        {
            var c = u.Center;
            Vfx.Glow(c, 2.6f, new Color(0.85f, 0.95f, 1f, 0.95f), 0.32f, 3f, null, 340, null, 1.25f);
            Vfx.Glow(c, 1.3f, new Color(1f, 1f, 1f, 1f), 0.22f, 3.5f, null, 341, null, 1.1f);
            Vfx.Ring(c, 1.2f, 3.4f, 0.4f, new Color(0.55f, 0.85f, 1f, 0.9f), 3f, "FX_IN_Ring_ShockWave_01", 339);
            Vfx.Burst(c, new Vfx.BurstOpt { Tex = "FX_UI_star_02", Count = 6, Speed = new Vector2(0.5f, 1.5f), Angle = 90, Spread = 360, Life = new Vector2(0.2f, 0.35f), Size = new Vector2(0.25f, 0.5f), C0 = new Color(0.9f, 0.97f, 1f), Order = 342, Boost = 3f, ShrinkTo = 0 });
        }

        // 보이기만 나눈 타격 가운데 피해 조각이 없는 박자(피해 1 처럼 작은 값) — 맞는 몸 반응 · 원작 공용 맞음만
        void CardGhostHit(List<int> targets, HitKind kind)
        {
            foreach (var ti in targets)
            {
                if (ti >= Enemies.Count || Enemies[ti] == null || Dead(ti)) continue;
                var t = Enemies[ti];
                t.Flash(new Color(1f, 0.97f, 0.9f), 0.08f, 0.22f);
                t.Knock(1f, 0.12f, 0.2f);
                if (HitSheets) Bolzena.Fx.BolzenaFx.Hit(t.Fx, FxHitKind(kind));
            }
            FieldRig.Shake(0.15f, 0.5f);
        }
    }
}
