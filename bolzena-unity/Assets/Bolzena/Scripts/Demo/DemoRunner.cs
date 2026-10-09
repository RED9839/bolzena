using System.Collections;
using System.Collections.Generic;
using System.IO;
using Bolzena.Battle;
using Bolzena.UI;
using UnityEngine;

namespace Bolzena.Demo
{
    // 자동 데모(-demo) — 가짜 손가락으로 전투 한 판(코어 규칙 · 샘플 사도 셋)을 저절로 진행하며 정해진 순간마다 캡처하고 끝낸다.
    //   1턴: 카드 툴팁 → 끌어서 타격 → 눌러 고르기 + 미리보기 → 적의 수 툴팁 · 상태 칩 → 적 · 사도 정보 창 → 더미 보기 → 일시정지 → 배속 → 남은 카드 → 턴 종료
    //   다음 턴들: 신탁 카드(창 · 변신) → 남은 카드 …
    //   보스: 등장 띠 → 사도마다 고학년(고르기 미리보기 → 확정) → 보스 격파 · 승리
    // 한 프레임 = 1/30초로 고정(Time.captureDeltaTime) — 느린 캡처가 연출 시간을 흐트러뜨리지 않게.
    public class DemoRunner : MonoBehaviour
    {
        BattleDirector d;
        Capture cap;
        readonly HashSet<string> taken = new HashSet<string>();
        int shotNo;
        bool perf;
        readonly List<float> allMs = new List<float>(), ultMs = new List<float>();

        void Update()
        {
            if (!perf || started <= 0) return;
            float ms = Time.unscaledDeltaTime * 1000f;
            allMs.Add(ms);
            if (d.InUlt) ultMs.Add(ms);
            if (ms > 25f) Debug.Log($"[Spike] {ms:F1}ms t={Time.unscaledTime - started:F1} 고학년={d.InUlt} 마지막={lastMoment}");
        }

        static string Stat(List<float> l)
        {
            if (l.Count == 0) return "없음";
            var s = new List<float>(l);
            s.Sort();
            float avg = 0; foreach (var x in s) avg += x; avg /= s.Count;
            int n1 = Mathf.Max(1, s.Count / 100);
            float low = 0; for (int i = s.Count - n1; i < s.Count; i++) low += s[i]; low /= n1;
            int over33 = 0, over16 = 0; foreach (var x in s) { if (x > 33.4f) over33++; if (x > 16.8f) over16++; }
            return $"프레임 {s.Count} · 평균 {avg:F1}ms · 1% low {low:F1}ms · 최악 {s[s.Count - 1]:F1}ms · >16.7ms {over16} · >33ms {over33}";
        }
        float started;

        public static void Attach(BattleDirector director)
        {
            var go = new GameObject("Demo");
            var r = go.AddComponent<DemoRunner>();
            r.d = director;
        }

        void Start()
        {
            perf = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-perf") >= 0;
            if (Has("-mute")) AudioListener.volume = 0f;   // 캡처 · 점검은 음소거(설정값은 그대로)
            cap = gameObject.AddComponent<Capture>();
            cap.Off = perf;
            if (!perf) Time.captureDeltaTime = 1f / 30f;   // 재기 모드는 실제 시계로
            cap.Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            string arg = Arg("-captures");
            if (arg != null) cap.Dir = arg;
            Debug.Log("[Demo] captures → " + cap.Dir);
            PointerInput.Simulated = true;
            PointerInput.SimPos = new Vector2(0, -6);
            d.DemoEpiphanyPick = 0;
            d.DemoSpendPick = 0;   // 소모량 · 버리기 고르기 창 — 사람 손을 기다리다 데모가 멈췄다(2026-10-09 「Card 0 → -1」 160초)
            d.Moment += OnMoment;
            UltCutin.OnStage += s => { lastMoment = s; Shot(s, 0); };
            EpiphanyWindow.OnStage += s =>
            {
                Shot(s, 1);
                if (s == "epiphany_window") StartCoroutine(PickEpiphany());
                if (s == "epiphany_pick" && Has("-pickframes")) StartCoroutine(PickFrames());
            };
            Banners.OnStage += s => Shot(s, 0);
            StartCoroutine(Run());
            // -shotevery 초 — 정해진 순간 말고도 게임 시간 그 초마다 한 장(every_NNN) — 소개용 장면 고르기(적 힘 모으기 예고 · 고학년 한창 등)
            if (float.TryParse(Arg("-shotevery"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var every) && every > 0) StartCoroutine(Every(every));
        }

        IEnumerator Every(float sec)
        {
            int n = 0;
            while (true)
            {
                yield return new WaitForSeconds(sec);
                cap.Still("every_" + (n++).ToString("D3") + (d.InUlt ? "_ult" : ""));
            }
        }

        static string Arg(string name)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }

        string lastMoment = "";

        static UI.Button ModalButton(string name) => Modal.Open == null ? null : System.Array.Find(Modal.Open.GetComponentsInChildren<UI.Button>(), b => b.name == name);

        // 칸 단추 둘레의 글이 이 이름인가(설정 창 칩 — 글은 단추의 형제)
        static bool Near(UI.Button b, string label)
        {
            foreach (var t in b.transform.parent.GetComponentsInChildren<TMPro.TextMeshPro>())
                if (t.text == label && Vector2.Distance(t.transform.position, b.transform.position) < 0.3f) return true;
            return false;
        }

        void OnMoment(string m)
        {
            lastMoment = m;
            switch (m)
            {
                case "hit": Shot("hit", 1); break;
                case "crit": Shot("crit", 1); break;
                case "break": Shot("break", 3); break;
                case "boss_break": Shot("boss_break", 3); break;
                case "enemy_attack": Shot("enemy_attack", 1); break;
                case "ult_hit": Shot("ult_hit", 1); break;
                case "boss_kill": Shot("boss_kill", 2); Shot("boss_kill_after", 14); break;
                case "victory": Shot("victory", 20); break;
                case "battle_end": Shot("battle_end", 10); break;
                case "boss_banner_start": cap.BeginSeq("boss_entry"); break;
                case "boss_in": cap.EndSeq(); break;
                default:
                    if (m.StartsWith("ult_impact_")) { Shot(m, 2); Shot(m + "_after", 9); }
                    break;
            }
        }

        // 같은 이름은 처음 한 번만. frames 프레임 뒤에 찍는다(파편이 퍼진 뒤를 보게)
        void Shot(string name, int frames)
        {
            if (!taken.Add(name)) return;
            StartCoroutine(ShotCo(name, frames));
        }

        IEnumerator ShotCo(string name, int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
            shotNo++;
            cap.Still(shotNo.ToString("D2") + "_" + name);
        }

        IEnumerator WaitInput()
        {
            float t = 0;
            while (!d.WaitingInput && !d.Over && t < 60f) { yield return null; t += Time.unscaledDeltaTime; }
            yield return null;
        }

        IEnumerator WaitTurnDone()
        {
            float t = 0;
            while (d.WaitingInput && t < 3f) { yield return null; t += Time.unscaledDeltaTime; }
            yield return WaitInput();
        }

        static IEnumerator Wait(float sec)
        {
            float t = 0;
            while (t < sec) { yield return null; t += Time.unscaledDeltaTime; }
        }

        IEnumerator MoveTo(Vector2 at, float dur = 0.25f)
        {
            Vector2 from = PointerInput.SimPos;
            float t = 0;
            while (t < dur) { PointerInput.SimPos = Vector2.Lerp(from, at, Ease.InOutCubic(t / dur)); yield return null; t += Time.unscaledDeltaTime; }
            PointerInput.SimPos = at;
        }

        IEnumerator Click(Vector2 at, float hover = 0.3f, bool right = false)
        {
            yield return MoveTo(at);
            yield return Wait(hover);
            if (right) PointerInput.SimRight = true; else PointerInput.SimHeld = true;
            yield return null;
            yield return null;
            PointerInput.SimRight = false;
            PointerInput.SimHeld = false;
            yield return null;
            yield return null;
        }

        Vector3 EnemyCenter(int i) => d.FieldRoot.TransformPoint(d.Enemies[i].Center);
        Vector3 HeroCenter(int i) => d.FieldRoot.TransformPoint(d.Heroes[i].Center);

        int FirstAttack()
        {
            for (int i = 0; i < d.Hand.Cards.Count; i++)
                if (d.Battle.CanPlay(i, out _) && d.Hand.Cards[i].Info.Target == TargetKind.Enemy && !d.Hand.Cards[i].Info.Epiphany) return i;
            return -1;
        }

        // 카드 한 장 — 끌어서 놓는다(대상 카드는 화살표로 적을 짚고)
        IEnumerator Drag(int i, int target, string seq = null, string hoverShot = null, string aimShot = null)
        {
            yield return WaitInput();
            if (i < 0 || i >= d.Hand.Cards.Count || !d.Battle.CanPlay(i, out _)) yield break;
            var info = d.Hand.Cards[i].Info;
            if (seq != null) cap.BeginSeq(seq);
            if (target < 0 || target >= d.Enemies.Count || d.Battle.Snapshot.Enemies[target].Dead) target = d.FirstAliveEnemy();
            Vector3 to = info.Target == TargetKind.Enemy ? EnemyCenter(target) : info.Target == TargetKind.Ally ? d.Hand.AllyDrop(info) : new Vector3(0.3f, 0.6f, 0);
            if (hoverShot != null) StartCoroutine(ShotLater(hoverShot, 0.45f));
            yield return d.Hand.DemoDrag(i, to, 0.4f, 0.5f, 0.3f, () => { if (aimShot != null) Shot(aimShot, 2); });
            yield return WaitTurnDone();
            if (seq != null) { yield return Wait(0.3f); cap.EndSeq(); }
        }

        // 카드 한 장 — 눌러서 고르고(미리보기 캡처) 대상을 눌러 낸다
        IEnumerator Tap(int i, int target, string previewShot = null)
        {
            yield return WaitInput();
            if (i < 0 || i >= d.Hand.Cards.Count || !d.Battle.CanPlay(i, out _)) yield break;
            var info = d.Hand.Cards[i].Info;
            yield return Click(d.Hand.Cards[i].transform.position + new Vector3(0, -0.4f, 0), 0.2f);
            yield return Wait(0.35f);
            if (target < 0 || target >= d.Enemies.Count || d.Battle.Snapshot.Enemies[target].Dead) target = d.FirstAliveEnemy();
            Vector2 at = info.Target == TargetKind.Enemy ? (Vector2)EnemyCenter(target) : new Vector2(0.3f, 0.8f);
            yield return MoveTo(at, 0.4f);
            yield return Wait(0.45f);
            if (previewShot != null) { Shot(previewShot, 0); yield return Wait(0.1f); }
            yield return Click(at, 0.05f);
            yield return WaitTurnDone();
        }

        IEnumerator ShotLater(string name, float sec)
        {
            yield return Wait(sec);
            Shot(name, 0);
        }

        IEnumerator EndTurn(string seq = null)
        {
            yield return WaitInput();
            if (seq != null) cap.BeginSeq(seq);
            yield return Click(d.Hud.EndPos);
            yield return WaitTurnDone();
            if (seq != null) cap.EndSeq();
        }

        // 고학년 — 얼굴을 눌러 고르고(미리보기), 적을 눌러 쓴다
        IEnumerator Ult(int hero, string seq = null, string previewShot = null)
        {
            yield return WaitInput();
            if (!d.Battle.CanUlt(hero, out var why)) { Debug.Log("[Demo] 고학년 못 씀 " + hero + " — " + why); yield break; }
            if (seq != null) cap.BeginSeq(seq);
            yield return Click(d.Hud.Ults[hero].transform.position, 0.4f);
            int t = d.FirstAliveEnemy();
            yield return MoveTo(EnemyCenter(t), 0.4f);
            yield return Wait(0.4f);
            if (previewShot != null) { Shot(previewShot, 0); yield return Wait(0.1f); }
            yield return Click(EnemyCenter(t), 0.05f);
            yield return WaitTurnDone();
            if (seq != null) { yield return Wait(0.2f); cap.EndSeq(); }
        }

        IEnumerator PickEpiphany()
        {
            yield return Wait(0.3f);
            var at = new Vector2(-3.5f, 0.1f);
            yield return MoveTo(at, 0.4f);
            yield return Wait(0.5f);
            Shot("epiphany_hover", 0);
            yield return Wait(0.3f);
            if (Has("-blessshots"))
            {   // 길게 누르면 카드 상세 — 놓아도 고르지 않아야 한다
                PointerInput.SimHeld = true;
                yield return Wait(0.7f);
                Shot("epiphany_longpress", 0);
                PointerInput.SimHeld = false;
                yield return Wait(0.6f);
                Shot("epiphany_after_longpress", 0);
                yield return Wait(0.3f);
            }
            PointerInput.SimHeld = true;
            yield return null;
            yield return null;
            PointerInput.SimHeld = false;
        }

        // 남은 카드 — 낼 수 있는 공격 카드를 첫 적에게, 없으면 스킬, 그것도 없으면 끝
        IEnumerator Auto(int maxCards = 6)
        {
            for (int n = 0; n < maxCards; n++)
            {
                yield return WaitInput();
                if (d.Over) yield break;
                int pick = -1;
                for (int i = 0; i < d.Hand.Cards.Count; i++)
                    if (d.Battle.CanPlay(i, out _) && d.Hand.Cards[i].Info.Type == CardType.Attack) { pick = i; break; }
                if (pick < 0)
                    for (int i = 0; i < d.Hand.Cards.Count; i++)
                        if (d.Battle.CanPlay(i, out _)) { pick = i; break; }
                if (pick < 0) yield break;
                yield return Drag(pick, d.FirstAliveEnemy());
            }
        }

        bool aoeUlt;
        bool powerShot;

        // 강화 카드를 내고 파티 버프 줄의 강화 칩(이름 · 겹 · 반짝임)과 그 툴팁(남는 효과 글)을 찍는다
        IEnumerator PowerCard()
        {
            int i = d.Hand.Cards.FindIndex(c => c.Info.Type == CardType.Power);
            if (i < 0 || !d.Battle.CanPlay(i, out _)) yield break;
            string name = d.Hand.Cards[i].Info.Name;
            yield return Drag(i, d.FirstAliveEnemy());
            yield return WaitInput();
            var row = d.Hud.Chips;
            var chip = d.Battle.Snapshot.PartyChips.Find(c => c.Kind == "power");
            if (chip == null) { Debug.Log("[Demo] 강화 칩 없음 — " + name); powerShot = true; yield break; }
            Shot("power_chip", 0);
            var at = row != null ? row.PosOf(chip.Id) : null;
            if (at.HasValue) { yield return MoveTo(at.Value, 0.3f); yield return Wait(0.8f); Shot("power_chip_tip", 0); }
            Debug.Log("[Demo] 강화 칩 — " + chip.Id + " " + chip.Value);
            powerShot = true;
        }
        bool BossWave => d.Battle.Snapshot.Enemies.Exists(e => e.Boss);

        // -enginedemo: 소모량 고르기 창(강화 없이 · 강화가 걸린 채) · 강화 표시가 붙은 손패 · 파티 칩 · 소모되면 사라지나
        int HandIdx(string id) => d.Hand.Cards.FindIndex(c => c.Info.Id == id || c.Info.Id.StartsWith(id + "@"));
        IEnumerator EngineShots()
        {
            int windows = 0;
            SpendWindow.OnStage = _ => { windows++; StartCoroutine(ShotLater(windows == 1 ? "spend_window" : "spend_window_boost", 0.5f)); };
            d.DemoSpendPick = 1;   // 절반
            yield return Wait(0.3f);
            Shot("engine_hand0", 0);
            Debug.Log("[Engine] 손 " + string.Join(", ", d.Hand.Cards.ConvertAll(c => c.Info.Id + (c.Info.EmpowerMul > 0 ? "(강화)" : ""))));
            yield return Drag(HandIdx(EngineDemo.Stack), d.FirstAliveEnemy());
            var pr = d.Battle.SpendPromptOf(HandIdx(EngineDemo.SpendA));
            Debug.Log("[Engine] 소모 후보 — " + (pr == null ? "없음" : $"{pr.Kw} 가진 {pr.Have} · {pr.PerUnit} · " + string.Join(" / ", pr.Options.ConvertAll(o => $"{o.N}:{o.Name}:{o.Result}:{o.Boosted}"))));
            yield return Drag(HandIdx(EngineDemo.SpendA), d.FirstAliveEnemy());
            Debug.Log("[Engine] 소모 뒤 후보 — " + (d.Battle.SpendPromptOf(HandIdx(EngineDemo.SpendB))?.Have.ToString() ?? "없음"));
            yield return Drag(HandIdx(EngineDemo.Empower), d.FirstAliveEnemy());
            yield return Wait(0.6f);
            Shot("empower_hand", 0);
            Debug.Log("[Engine] 강화 뒤 손 " + string.Join(", ", d.Hand.Cards.ConvertAll(c => c.Info.Id + (c.Info.EmpowerMul > 0 ? $"(강화 ×{c.Info.EmpowerMul})" : ""))) + " · 칩 " + string.Join(", ", d.Battle.Snapshot.PartyChips.ConvertAll(c => c.Id + " " + c.Value)));
            int hi = HandIdx(EngineDemo.SpendB);
            if (hi >= 0)
            {   // 강화된 카드 위에 올려 두기 — 본문 수치
                yield return MoveTo(d.Hand.Cards[hi].transform.position + new Vector3(0, -0.3f, 0), 0.3f);
                yield return Wait(0.8f);
                Shot("empower_hover", 0);
            }
            var chip = d.Battle.Snapshot.PartyChips.Find(c => c.Kind == "empower");
            var at = chip != null && d.Hud.Chips != null ? d.Hud.Chips.PosOf(chip.Id) : null;
            if (at.HasValue) { yield return MoveTo(at.Value, 0.3f); yield return Wait(0.8f); Shot("empower_chip_tip", 0); }
            yield return MoveTo(new Vector2(0, -6), 0.2f);
            d.DemoSpendPick = 0;   // 1개 — 강화된 값을 보인 뒤
            yield return Drag(HandIdx(EngineDemo.SpendB), d.FirstAliveEnemy());
            yield return Wait(0.6f);
            Shot("empower_gone", 0);
            Debug.Log("[Engine] 소모 뒤 손 " + string.Join(", ", d.Hand.Cards.ConvertAll(c => c.Info.Id + (c.Info.EmpowerMul > 0 ? "(강화)" : ""))) + " · 칩 " + d.Battle.Snapshot.PartyChips.Count + " · 창 " + windows);
            yield return Wait(0.5f);
            Debug.Log("[Demo] -enginedemo 끝");
            Application.Quit(0);
        }

        // -holdzoom: 손패 길게 누르기 확대 — 누르기 전 · 누르는 중(마우스) · 키워드가 가장 많은 카드(터치) · 뗀 뒤 · 누른 채 끌면 확대가 접히나
        IEnumerator HoldZoomShots()
        {
            var cards = d.Hand.Cards;
            if (cards.Count == 0) yield break;
            int many = 0;
            for (int i = 1; i < cards.Count; i++) if ((cards[i].Info.Terms?.Count ?? 0) > (cards[many].Info.Terms?.Count ?? 0)) many = i;
            Shot("hold_before", 0);
            yield return Wait(0.2f);
            var passes = new[] { (i: 0, touch: false, name: "hold_zoom"), (i: many, touch: true, name: "hold_zoom_many") };
            foreach (var ps in passes)
            {
                if (ps.i >= cards.Count) continue;
                PointerInput.SimTouch = ps.touch;
                yield return MoveTo(cards[ps.i].transform.position + new Vector3(0, -0.3f, 0), 0.2f);
                PointerInput.SimHeld = true;
                yield return Wait(0.6f);
                Debug.Log($"[HoldZoom] {ps.name} — 카드 {ps.i} 「{cards[ps.i].Info.Name}」 낱말 {cards[ps.i].Info.Terms?.Count ?? 0}개 · 확대 {(d.Hand.HoldZooming && CardZoom.Shown ? "켜짐" : "꺼짐")}");
                Shot(ps.name, 0);
                yield return Wait(0.2f);
                PointerInput.SimHeld = false;
                yield return Wait(0.4f);
                Debug.Log($"[HoldZoom] {ps.name} 뗀 뒤 — 고른 카드 {(d.Hand.HasSelection ? "있음(틀림)" : "없음")} · 길게 누르기 확대 {(d.Hand.HoldZooming ? "남음(틀림)" : "접힘")}");
                Shot(ps.name + "_released", 0);
            }
            // 누른 채 끌기 — 확대가 접히고 카드가 손가락을 따라오나(손 쪽으로 돌려 놓고 떼서 내지 않는다)
            PointerInput.SimTouch = false;
            Vector2 at = cards[0].transform.position + new Vector3(0, -0.3f, 0);
            yield return MoveTo(at, 0.2f);
            PointerInput.SimHeld = true;
            yield return Wait(0.5f);
            yield return MoveTo(at + new Vector2(1.2f, 0.6f), 0.25f);
            yield return Wait(0.1f);
            Debug.Log($"[HoldZoom] 누른 채 끌기 — 확대 {(CardZoom.Shown ? "남음(틀림)" : "접힘")}");
            Shot("hold_then_drag", 0);
            yield return MoveTo(at, 0.2f);
            PointerInput.SimHeld = false;
            yield return Wait(0.4f);
        }

        IEnumerator Run()
        {
            started = Time.unscaledTime;
            StartCoroutine(Watchdog());
            yield return WaitInput();
            yield return Wait(0.3f);
            Shot("battle_start", 0);
            // -powershot: 첫 손에 강화 카드가 있으면 먼저 내고 강화 칩 · 툴팁을 찍는다(없으면 다음 턴들에서)
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-powershot") >= 0) yield return PowerCard();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-toughshots") >= 0) { yield return ToughShots(); yield break; }
            if (Has("-breakkillshots")) { yield return BreakKillShots(); yield break; }
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-holdzoom") >= 0) { yield return HoldZoomShots(); yield break; }
            if (Has("-blessshots")) { yield return BlessShots(); yield break; }
            if (EngineDemo.On) { yield return EngineShots(); yield break; }

            // ── 1턴 — 화면 둘러보기 ──
            // 카드 올려 두기 → 풀이 툴팁
            if (d.Hand.Cards.Count > 1)
            {
                yield return MoveTo(d.Hand.Cards[1].transform.position + new Vector3(0, -0.3f, 0), 0.3f);
                yield return Wait(0.9f);
                Shot("tooltip_card", 0);
                yield return Wait(0.2f);
            }
            // 적의 수 툴팁(즉시 행동 셈)
            if (d.EnemyHuds.Count > 0)
            {
                yield return MoveTo(d.EnemyHuds[0].IntentPos, 0.35f);
                yield return Wait(0.6f);
                Shot("tooltip_intent", 0);
            }
            // -choicetest: 두 갈래 고르기 창(콘텐츠에 아직 두 갈래 카드가 없어 손패 첫 카드에 갈래 이름을 달아 띄운다)
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-choicetest") >= 0 && d.Hand.Cards.Count > 0)
            {
                var ci = d.Hand.Cards[0].Info;
                var real = Arg("-choicecard") is string cid ? (d.Battle as CoreBattle)?.InfoOf(cid) : null;   // -choicecard 포셔_u1 — 그 카드의 진짜 갈래 창(글 속 <sprite> 아이콘 보기)
                var fake = real != null && real.Choices != null && real.Choices.Count > 0 ? real : new CardInfo { Id = ci.Id + "_t", Name = ci.Name, Hero = ci.Hero, Cost = ci.Cost, Type = ci.Type, TypeName = ci.TypeName, Text = ci.Text, Art = ci.Art, Nature = ci.Nature, Terms = ci.Terms, Tags = ci.Tags, Choices = new List<string> { "베어 가르기", "막아 서기" } };
                int got = -1;
                StartCoroutine(UI.ChoiceWindow.Run(d.UiRoot, fake, b => got = b, 2));
                yield return Wait(0.6f);
                Shot("choice_window", 0);
                while (got < 0) yield return null;
                Debug.Log("[Demo] 두 갈래 고름 " + got);
                if (real != null) { Debug.Log("[Demo] -choicecard 끝"); Application.Quit(0); yield break; }
            }
            // 고학년 원 툴팁 · 적 이름 줄(올림)
            if (d.Hud.Ults.Count > 0)
            {
                yield return MoveTo(d.Hud.Ults[0].transform.position, 0.3f);
                yield return Wait(0.6f);
                Shot("tooltip_ult", 0);
            }
            if (d.Enemies.Count > 0)
            {
                yield return MoveTo(EnemyCenter(0), 0.3f);
                yield return Wait(0.5f);
                Shot("enemy_hover", 0);
            }
            // 전체 공격 고학년 — 두 적의 가운데로(첫 웨이브에 한 번)
            if (d.Battle.CanUlt(0, out _)) { aoeUlt = true; yield return Ult(0, "ult_aoe", "preview_ult_aoe"); }
            // 끌어서 타격
            yield return Drag(FirstAttack(), 0, "hit", "card_hover", "target_arrow");
            // 눌러 고르기 → 적 위 미리보기 → 확정
            yield return Tap(FirstAttack(), 1, "preview_card");
            // 터치 — 첫 탭 고르기, 적 첫 탭 겨누기(미리보기), 같은 적 두 번째 탭 확정
            {
                yield return WaitInput();
                int ti = FirstAttack();
                if (ti >= 0)
                {
                    PointerInput.SimTouch = true;
                    yield return Click(d.Hand.Cards[ti].transform.position + new Vector3(0, -0.4f, 0), 0.1f);
                    yield return Wait(0.3f);
                    int e = d.FirstAliveEnemy();
                    yield return Click(EnemyCenter(e), 0.1f);
                    yield return Wait(0.5f);
                    Shot("touch_aim_preview", 0);
                    yield return Click(EnemyCenter(e), 0.2f);
                    PointerInput.SimTouch = false;
                    yield return WaitTurnDone();
                }
            }
            // 상태 칩 툴팁(걸린 것이 있으면)
            yield return WaitInput();
            for (int i = 0; i < d.EnemyHuds.Count; i++)
                if (d.EnemyHuds[i] != null && d.EnemyHuds[i].HasChips && !d.Battle.Snapshot.Enemies[i].Dead)
                {
                    yield return MoveTo(d.EnemyHuds[i].ChipPos, 0.3f);
                    yield return Wait(0.6f);
                    Shot("tooltip_chip", 0);
                    break;
                }
            // 정보 창 — 적(오른쪽 클릭) · 사도(오른쪽 클릭)
            yield return Click(EnemyCenter(d.FirstAliveEnemy()), 0.2f, true);
            yield return Wait(0.5f);
            Shot("info_enemy", 0);
            yield return Wait(0.2f);
            yield return Click(new Vector2(0, -4.3f), 0.1f, true);
            // 사도(오른쪽 클릭) — 전체 화면 파티 창의 전투원 탭 → 돋보기(고학년 · 패시브 펼침)
            yield return Click(HeroCenter(0), 0.2f, true);
            yield return Wait(0.5f);
            Shot("info_hero", 0);
            var zoomBtn = ModalButton("zoom");
            if (zoomBtn != null) { yield return Click(zoomBtn.transform.position, 0.2f); yield return Wait(0.3f); Shot("info_hero_open", 0); }
            yield return Click(new Vector2(0, -4.3f), 0.1f, true);
            // 파티 HP 를 눌러 — 효과 탭이 먼저 → 카드 탭 → 카드 누르기(확대)
            yield return Click(d.Hud.PartyPos, 0.2f);
            yield return Wait(0.5f);
            Shot("party_effects", 0);
            var tab2 = ModalButton("tab2");
            if (tab2 != null)
            {
                yield return Click(tab2.transform.position, 0.2f);
                yield return Wait(0.4f);
                Shot("party_deck", 0);
                var deck = Modal.Open != null ? Modal.Open.GetComponentsInChildren<CardView>() : new CardView[0];
                if (deck.Length > 2) { yield return Click(deck[2].transform.position, 0.2f); yield return Wait(0.4f); Shot("party_card_zoom", 0); yield return Click(new Vector2(6, 3.5f), 0.1f); }
                // 생성 카드가 있는 카드(「케이크」 1장 생성 …) — 확대 옆에 그 카드를 작은 카드로
                var mk = System.Array.Find(deck, c => c.Info != null && c.Info.Terms != null && c.Info.Terms.Exists(t => t.Kind == "card"));
                if (mk != null) { yield return Wait(0.3f); var md = Modal.Open; if (md != null) md.gameObject.SetActive(false); CardZoom.Show(d.UiRoot, mk.Info, new Vector3(-3f, 0.2f, 0)); yield return Wait(0.5f); Shot("party_card_zoom_make", 0); yield return Wait(0.2f); CardZoom.Hide(); if (md != null) md.gameObject.SetActive(true); }
                var pv = FindAnyObjectByType<PartyUi>();   // 맨 끝 교주 카드 묶음(주인 사도 빛깔 · 핀)
                if (pv != null) { yield return Wait(0.3f); pv.ScrollToEnd(); yield return Wait(0.5f); Shot("party_deck_end", 0); yield return Wait(0.3f); }
            }
            yield return Click(new Vector2(0, -4.3f), 0.1f, true);
            // 왼쪽 위 초상을 눌러 — 같은 창의 전투원 탭(그 사도에 금 테)
            yield return Click(d.Hud.PortraitRect(1).center, 0.2f);
            yield return Wait(0.5f);
            Shot("info_hero_portrait", 0);
            yield return Click(new Vector2(0, -4.3f), 0.1f, true);
            // 더미 보기 — 뽑을 더미(누르기) → 카드 누르기(확대 + 키워드 판) → 닫기, 버린 더미
            yield return Click(d.Hud.PilePos(0), 0.2f);
            yield return Wait(0.5f);
            Shot("pile_draw", 0);
            var pc = Modal.Open != null ? Modal.Open.GetComponentsInChildren<CardView>() : new CardView[0];
            if (pc.Length > 2) { yield return Click(pc[2].transform.position, 0.2f); yield return Wait(0.5f); Shot("pile_tooltip", 0); yield return Click(new Vector2(6, 3.5f), 0.1f); }
            yield return Click(new Vector2(0, -4.4f), 0.1f, true);
            yield return Click(d.Hud.PilePos(1), 0.2f);
            yield return Wait(0.5f);
            Shot("pile_discard", 0);
            yield return Click(new Vector2(0, -4.4f), 0.1f, true);
            // 일시정지 · 배속
            yield return Click(d.Hud.PausePos, 0.2f);
            yield return Wait(0.4f);
            Shot("pause", 0);
            // -trydisplay: 설정 창의 해상도 칸(HD)을 눌러 시험 적용 → 「유지할까요?」 → 되돌리기(진짜 창 크기가 바뀐다 — 따로 돌린다)
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-trydisplay") >= 0 && Modal.Open != null)
            {
                var hd = System.Array.Find(Modal.Open.GetComponentsInChildren<UI.Button>(), b => b.transform.parent == Modal.Open.Content && b.GetComponentInChildren<TMPro.TextMeshPro>() == null && Near(b, "HD"));
                if (hd != null)
                {
                    yield return Click(hd.transform.position, 0.2f);
                    yield return Wait(1.2f);
                    Shot("pause_try", 0);
                    var rv = System.Array.Find(Modal.Open.GetComponentsInChildren<UI.Button>(), b => b.name == "revert");
                    if (rv != null) { yield return Click(rv.transform.position, 0.2f); yield return Wait(1.2f); Shot("pause_reverted", 0); }
                }
                else Debug.Log("[Demo] HD 칸을 못 찾음");
            }
            yield return Click(new Vector2(0, -4.4f), 0.1f, true);
            yield return Click(d.Hud.SpeedPos, 0.2f);
            yield return Wait(0.3f);
            Shot("speed_2x", 0);
            yield return Click(d.Hud.SpeedPos, 0.2f);
            yield return Auto(4);
            // 자동 전투 토글 — 켜서 모습만 찍고 끈다
            d.Hud.OnAuto?.Invoke();
            yield return Wait(0.1f);
            Shot("auto_on", 0);
            yield return Wait(0.2f);
            d.Hud.OnAuto?.Invoke();
            yield return EndTurn("enemy_turn");

            // ── 다음 턴들 — 신탁 카드가 나오면 낸다, 보스 웨이브까지 ──
            int guard = 0;
            bool epi = false;
            while (!BossWave && !d.Over && guard++ < 12)
            {
                yield return WaitInput();
                // 전체 공격 고학년 — 두 적의 가운데로(첫 웨이브에 한 번)
                if (!aoeUlt && d.Battle.CanUlt(0, out _) && d.Battle.Snapshot.Enemies.FindAll(e => !e.Dead).Count > 1)
                {
                    aoeUlt = true;
                    yield return Ult(0, "ult_aoe", "preview_ult_aoe");
                    continue;
                }
                yield return StatusInfo();
                if (!powerShot) { yield return PowerCard(); if (powerShot) continue; }
                if (!epi)
                {
                    int g = d.Hand.Cards.FindIndex(c => c.Info.Epiphany);
                    if (g >= 0 && d.Battle.CanPlay(g, out _))
                    {
                        yield return Wait(0.4f);
                        Shot("epiphany_in_hand", 0);
                        yield return Drag(g, d.FirstAliveEnemy(), "epiphany");
                        epi = true;
                        continue;
                    }
                }
                yield return Auto(1);
                yield return WaitInput();
                if (!BossWave && !AnyPlayable()) yield return EndTurn();
            }

            // ── 보스 — 사도마다 고학년 ──
            yield return WaitInput();
            yield return Wait(0.5f);
            Shot("boss_field", 0);
            {
                int b = d.Battle.Snapshot.Enemies.FindIndex(e => e.Boss && !e.Dead);
                if (b >= 0)
                {
                    yield return Click(EnemyCenter(b), 0.2f, true);
                    yield return Wait(0.6f);
                    Shot("info_boss", 0);
                    yield return Click(new Vector2(0, -4.3f), 0.1f, true);
                }
            }
            var s = d.Battle.Snapshot;
            var order = new List<int>();
            for (int h = s.Heroes.Count - 1; h >= 0; h--) order.Add(h);
            // 보스 — 먼저 두 턴은 카드로(강인도를 깎아 격파), 그다음 고학년
            for (int turn = 0; turn < 2 && !d.Over; turn++)
            {
                for (int n = 0; n < 6 && !d.Over; n++)
                {
                    yield return WaitInput();
                    if (d.Over) break;
                    if (!epi) { int g = d.Hand.Cards.FindIndex(c => c.Info.Epiphany); if (g >= 0 && d.Battle.CanPlay(g, out _)) { Shot("epiphany_in_hand", 0); yield return Drag(g, 0, "epiphany"); epi = true; continue; } }
                    if (!AnyPlayable()) break;
                    yield return Auto(1);
                }
                if (!d.Over) yield return EndTurn();
                yield return WaitInput();
                if (!d.Over) yield return StatusInfo(1);     // 보스 — 패시브로 쌓인 상태(결의 …)
            }
            bool first = true;
            for (int n = 0; n < 24 && !d.Over; n++)
            {
                yield return WaitInput();
                if (d.Over) break;
                int ready = order.Find(h => d.Battle.CanUlt(h, out _));
                if (order.Exists(h => d.Battle.CanUlt(h, out _)))
                {
                    order.Remove(ready);
                    yield return Ult(ready, "ult_" + d.Battle.Snapshot.Heroes[ready].Key, first ? "preview_ult" : null);
                    first = false;
                    continue;
                }
                if (AnyPlayable()) { yield return Auto(1); continue; }
                yield return EndTurn();
            }
            yield return Wait(6.5f);
            if (perf) { Debug.Log("[Perf] 전체 " + Stat(allMs)); Debug.Log("[Perf] 고학년 " + Stat(ultMs)); }
            Debug.Log("[Demo] 끝 — " + shotNo + "장");
            Application.Quit();
        }

        // -breakkillshots: ① 격파하며 처치 — 강인도 칸이 깎여 0 · 「격파!」 · AP +1 이 쓰러지기 전에 보이는지(0.1초마다 bk_kill_NN)
        //   ② 격파한 적이 그 차례를 쉬는지 — 불효자손(-foes …magicfork)이면 격파 뒤 스킬을 내 심통을 채워도 못 움직이고, 적 차례에도 쉰다(bk_foeturn_NN) · 정보 창
        IEnumerator BreakKillShots()
        {
            var cb = d.Battle as CoreBattle;
            if (cb == null) { Application.Quit(); yield break; }
            yield return WaitInput();
            var es = d.Battle.Snapshot.Enemies;
            int B = es.FindIndex(x => !x.Dead && x.Name.Contains("불효자손"));
            int A = -1;
            for (int i = 0; i < es.Count && A < 0; i++) if (i != B && !es[i].Dead && es[i].ToughMaxV > 0) A = i;
            if (A >= 0)
            {
                int pa = AttackBy(true, A); if (pa < 0) pa = AttackBy(false, A);
                cb.AuditSetFoe(A, 1, 0.01);
                d.RefreshAll();
                yield return Wait(0.5f);
                Shot("bk_before", 0);
                StartCoroutine(Burst("bk_kill", 0.12f, 45));
                yield return Tap(pa, A);
                yield return Wait(0.3f);
                Shot("bk_after", 0);
            }
            if (B < 0) B = d.FirstAliveEnemy();
            if (B >= 0 && !d.Over)
            {
                int pb = AttackBy(true, B); if (pb < 0) pb = AttackBy(false, B);
                cb.AuditSetFoe(B, 5000, 0.01);
                d.RefreshAll();
                yield return Wait(0.3f);
                yield return Tap(pb, B);
                yield return Wait(0.9f);
                Shot("bk_broken", 0);
                // 공격이 아닌 카드 — 불효자손 심통(4 면 즉시 내리찍기)을 채운다. 격파 중이면 다음 차례로 미뤄져야 한다
                for (int k = 0; k < 5 && !d.Over; k++)
                {
                    yield return WaitInput();
                    int s = -1;
                    for (int i = 0; i < d.Hand.Cards.Count && s < 0; i++) if (d.Battle.CanPlay(i, out _) && d.Hand.Cards[i].Info.Type != CardType.Attack) s = i;
                    if (s < 0) break;
                    yield return Tap(s, B);
                }
                yield return Wait(0.5f);
                Shot("bk_after_skills", 0);
                yield return InfoShot(B, "bk_info_broken");
                yield return MoveTo(new Vector2(0, -6), 0.2f);
                StartCoroutine(Burst("bk_foeturn", 0.25f, 24));
                yield return EndTurn();
                yield return Wait(0.6f);
                Shot("bk_next_turn", 0);
                yield return InfoShot(B, "bk_info_next");
            }
            yield return Wait(0.5f);
            Debug.Log("[Demo] 끝 — " + shotNo + "장");
            Application.Quit();
        }

        IEnumerator Burst(string name, float step, int n)
        {
            for (int k = 0; k < n; k++) { cap.Still(name + "_" + k.ToString("D2")); yield return Wait(step); }
        }

        // -toughshots: 강인도 막대만 짧게 — 약점 아닌 카드 겨눔(1/3 예고) · 부분 칸 · 약점 카드 겨눔(아이콘 빛) · 상세 창 · 격파 · 격파 상태 · 일어남
        int AttackBy(bool weak, int target)
        {
            var e = d.Battle.Snapshot.Enemies[target];
            for (int i = 0; i < d.Hand.Cards.Count; i++)
            {
                var c = d.Hand.Cards[i].Info;
                if (!d.Battle.CanPlay(i, out _) || c.Target != TargetKind.Enemy || c.Type != CardType.Attack || c.Epiphany) continue;
                bool w = c.Nature != null && (c.Nature == "공명" || e.Weak.Contains(c.Nature));
                if (w == weak) return i;
            }
            return -1;
        }

        IEnumerator InfoShot(int target, string name)
        {
            yield return WaitInput();
            yield return Click(EnemyCenter(target), 0.2f, true);
            yield return Wait(0.6f);
            Shot(name, 0);
            yield return Wait(0.2f);
            if (Modal.Open != null) Modal.Open.Close();   // 바깥 누르기는 턴 끝 단추에 닿을 수 있어 바로 닫는다
            yield return Wait(0.4f);
        }

        // 겨눌 적 — 살아 있고 격파 안 된 적 가운데 HP 가 가장 많은 적(격파 전에 쓰러지지 않게)
        int ToughTarget()
        {
            var es = d.Battle.Snapshot.Enemies;
            int best = -1;
            for (int i = 0; i < es.Count; i++) if (!es[i].Dead && (best < 0 || es[i].Hp > es[best].Hp)) best = i;
            return best < 0 ? 0 : best;
        }

        IEnumerator ToughShots()
        {
            int T = ToughTarget();
            int n = 0;
            bool partial = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-toughweakfirst") >= 0, weakAim = false;   // 약점 먼저(격파까지 빨리)
            for (int turn = 0; turn < 10 && !d.Over; turn++)
            {
                for (int k = 0; k < 8 && !d.Over; k++)
                {
                    yield return WaitInput();
                    var e = d.Battle.Snapshot.Enemies[T];
                    if (e.Broken) break;
                    if (e.Dead) { T = ToughTarget(); e = d.Battle.Snapshot.Enemies[T]; if (e.Dead) break; }
                    int pick = !partial ? AttackBy(false, T) : -1;
                    if (pick < 0) pick = AttackBy(true, T);
                    if (pick < 0) pick = AttackBy(false, T);
                    if (pick < 0) break;
                    bool w = AttackBy(true, T) == pick;
                    string pv = !partial && !w ? "t_aim_third" : !weakAim && w ? "t_aim_weak" : null;
                    if (!w) partial = true; else weakAim = true;
                    n++;
                    Debug.Log($"[Demo] 강인도 {d.Battle.Snapshot.Enemies[T].ToughV:0.00}/{d.Battle.Snapshot.Enemies[T].ToughMaxV:0.00} ← {d.Hand.Cards[pick].Info.Name}({d.Hand.Cards[pick].Info.Nature}) 약점={w}");
                    yield return Tap(pick, T, pv);
                    yield return Wait(0.15f);
                    Shot("t_after" + n, 0);
                    var e2 = d.Battle.Snapshot.Enemies[T];
                    Debug.Log($"[Demo] → 강인도 {e2.ToughV:0.00} 격파={e2.Broken}");
                    if (n == 1 && !e2.Broken) yield return InfoShot(T, "t_info_partial");
                    if (e2.Broken) break;
                }
                if (d.Over) break;
                if (d.Battle.Snapshot.Enemies[T].Broken)
                {
                    yield return Wait(0.9f);
                    Shot("t_broken", 0);
                    yield return InfoShot(T, "t_info_broken");
                    yield return MoveTo(new Vector2(0, -6), 0.2f);
                    yield return EndTurn();
                    yield return Wait(0.5f);
                    Shot("t_recover", 0);
                    yield return Wait(0.8f);
                    Shot("t_recover_full", 0);
                    break;
                }
                yield return EndTurn();
            }
            yield return Wait(0.5f);
            Debug.Log("[Demo] 끝 — " + shotNo + "장");
            Application.Quit();
        }

        // 상태가 여럿 걸린 적의 정보 창 — 처음 한 번(칩 2개 이상)
        bool statusShot;
        IEnumerator StatusInfo(int need = 2)
        {
            if (statusShot) yield break;
            var es = d.Battle.Snapshot.Enemies;
            int best = -1;
            for (int i = 0; i < es.Count; i++) if (!es[i].Dead && es[i].Chips.Count >= need && (best < 0 || es[i].Chips.Count > es[best].Chips.Count)) best = i;
            if (best < 0) yield break;
            statusShot = true;
            yield return Click(EnemyCenter(best), 0.2f, true);
            yield return Wait(0.6f);
            Shot("info_enemy_status", 0);
            yield return Click(new Vector2(0, -4.3f), 0.1f, true);
            yield return WaitInput();
        }

        bool AnyPlayable()
        {
            for (int i = 0; i < d.Hand.Cards.Count; i++) if (d.Battle.CanPlay(i, out _)) return true;
            return false;
        }

        static bool Has(string a) => System.Array.IndexOf(System.Environment.GetCommandLineArgs(), a) >= 0;

        // -pickframes — 신탁을 고른 순간부터 2.2초 동안 화면 시각 0.1초마다 한 장(pick_NN_밀리초) — 메모리에 모았다가 끝에 저장(찍는 동안 멈칫이 없게)
        IEnumerator PickFrames()
        {
            var shots = new List<(Texture2D tex, float at)>();
            float t0 = Time.unscaledTime, next = 0;
            while (Time.unscaledTime - t0 < 2.2f)
            {
                yield return new WaitForEndOfFrame();
                float e = Time.unscaledTime - t0;
                if (e >= next) { shots.Add((ScreenCapture.CaptureScreenshotAsTexture(), e)); next += 0.1f; }
                yield return null;
            }
            Directory.CreateDirectory(cap.Dir);
            for (int i = 0; i < shots.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(cap.Dir, $"pick_{i:D2}_{Mathf.RoundToInt(shots[i].at * 1000):D4}ms.png"), shots[i].tex.EncodeToPNG());
                Destroy(shots[i].tex);
            }
            Debug.Log($"[PickFrame] {shots.Count}장");
        }

        // -blessshots(축복 연출 캡처 · -forcebless 와 함께) — 빛나는 카드를 내어 신탁 창(축복 얹힌 첫 선택지)을 고르고,
        // 축복 받은 카드가 손에 다시 들어오면 찍는다(손패 · 올려 둔 모습). 그 밖의 시범은 하지 않는다.
        IEnumerator BlessShots()
        {
            yield return Wait(0.5f);
            // 강화 카드는 내면 판에 남아 손에 돌아오지 않는다 — 공격 · 스킬 가운데 빛나는 카드
            System.Predicate<CardView> ok = c => c.Info.Epiphany && c.Info.Type != CardType.Power;
            int g = d.Hand.Cards.FindIndex(ok);
            for (int k = 0; g < 0 && k < 5 && !d.Over; k++) { yield return EndTurn(); yield return WaitInput(); yield return Wait(0.6f); g = d.Hand.Cards.FindIndex(ok); }
            if (g < 0) { Debug.LogError("[Demo] 빛나는 카드가 손에 안 들어옴"); Application.Quit(5); yield break; }
            Shot("epiphany_in_hand", 0);
            yield return Wait(0.1f);
            yield return Drag(g, d.FirstAliveEnemy());
            yield return Wait(0.25f);
            Shot("after_pick_fly", 0);
            yield return Wait(0.75f);
            Shot("after_pick_field", 0);
            int b = -1;
            for (int k = 0; k < 8 && !d.Over; k++)
            {
                yield return WaitInput();
                yield return Wait(0.8f);
                b = d.Hand.Cards.FindIndex(c => !string.IsNullOrEmpty(c.Info.MarkBless));
                if (b >= 0) break;
                yield return EndTurn();
            }
            if (b < 0) { Debug.LogError("[Demo] 축복 카드가 손에 다시 안 들어옴"); Application.Quit(6); yield break; }
            Debug.Log($"[Demo] 축복 카드 손에 — {d.Hand.Cards[b].Info.Name} · {d.Hand.Cards[b].Info.MarkBless}");
            Shot("bless_in_hand", 0);
            yield return Wait(0.2f);
            yield return MoveTo(d.Hand.Cards[b].transform.position + new Vector3(0, -0.3f, 0), 0.3f);
            yield return Wait(1.0f);
            Shot("bless_in_hand_hover", 0);
            yield return Wait(0.3f);
            // 길게 눌러 확대(손가락) — 확대 카드 · 낱말 판에서 축복 표식 · 이름을 본다
            PointerInput.SimTouch = true;
            yield return MoveTo(d.Hand.Cards[b].transform.position + new Vector3(0, -0.3f, 0), 0.2f);
            PointerInput.SimHeld = true;
            yield return Wait(0.7f);
            Shot("bless_in_hand_zoom", 0);
            yield return Wait(0.2f);
            PointerInput.SimHeld = false;
            PointerInput.SimTouch = false;
            yield return Wait(0.5f);
            // 신탁만 · 축복까지 · 아무것도 없는 카드 나란히
            {
                var src = d.Hand.Cards[b].Info;
                var mc = typeof(CardInfo).GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var plain = (CardInfo)mc.Invoke(src, null); plain.MarkBless = null; plain.MarkBlessText = null; plain.EpiphanyLabel = null; plain.CostDown = false;
                var ora = (CardInfo)mc.Invoke(src, null); ora.MarkBless = null; ora.MarkBlessText = null; ora.EpiphanyLabel = "신탁 1";
                var views = new System.Collections.Generic.List<CardView>();
                int k = 0;
                foreach (var inf in new[] { plain, ora, src })
                {
                    var cv2 = CardView.Create(d.UiRoot, inf);
                    cv2.ShowDesc = true; cv2.Follow = 40f;
                    cv2.TargetPos = new Vector3((k - 1) * 3.3f, 0.3f, 0); cv2.TargetScale = 1.5f; cv2.SetOrder(1700 + k * 10); cv2.Snap();
                    views.Add(cv2); k++;
                }
                yield return Wait(0.5f);
                Shot("marks_compare", 0);
                yield return Wait(0.3f);
                foreach (var v in views) Object.Destroy(v.gameObject);
            }
            // 겨우살이 표정 후보(Happy_1~4 · Normal · Idle_1) 나란히
            {
                var nd = Res.Spine("st_noone");
                var made = new System.Collections.Generic.List<GameObject>();
                string[] an = { "Happy_1", "Happy_2", "Happy_3", "Happy_4", "Normal", "Idle_1" };
                for (int q = 0; q < an.Length; q++)
                {
                    if (nd == null || nd.GetSkeletonData(true).FindAnimation(an[q]) == null) continue;
                    var sa = Spine.Unity.SkeletonAnimation.NewSkeletonAnimationGameObject(nd);
                    sa.transform.SetParent(d.UiRoot, false);
                    sa.AnimationState.SetAnimation(0, an[q], true); sa.Update(0.3f); sa.LateUpdate();
                    var mr = sa.GetComponent<MeshRenderer>(); mr.sortingOrder = 1700;
                    float k = 3.2f / Mathf.Max(0.1f, mr.bounds.size.y);
                    sa.transform.localScale = Vector3.one * k;
                    sa.transform.localPosition = new Vector3((q - 2.5f) * 2.7f - mr.bounds.center.x * k, -3f - (mr.bounds.min.y - sa.transform.position.y) * k, 0);
                    made.Add(sa.gameObject);
                }
                var back = Bolzena.View.Make.Box("nbg", d.UiRoot, Res.UI("white"), Vector3.zero, new Vector2(44, 16), 1690, new Color(0.1f, 0.1f, 0.12f, 1));
                yield return Wait(0.4f);
                Shot("noone_faces", 0);
                yield return Wait(0.3f);
                foreach (var g0 in made) Object.Destroy(g0);
                Object.Destroy(back.gameObject);
            }
            // 축복 카드를 쓰고 버린 더미 창에서도 축복이 남는지
            string bid = d.Hand.Cards[b].Info.Id; var bInfo = d.Hand.Cards[b].Info;
            if (d.Battle.CanPlay(b, out _))
            {
                yield return Drag(b, d.FirstAliveEnemy());
                yield return Wait(1.2f);
                d.OpenPile(1);
                yield return Wait(0.6f);
                Shot("pile_discard", 0);
                var pcv = Modal.Open != null ? Modal.Open.GetComponentsInChildren<CardView>() : new CardView[0];
                if (pcv.Length > 0) { yield return Click(pcv[0].transform.position, 0.2f); yield return Wait(0.7f); Shot("pile_card_click", 0); yield return Wait(0.2f); }
                Modal.Open?.Close();
                yield return Wait(0.3f);
            }
            // 두 번째 신탁 창 — 손의 카드 하나에 빛을 얹어 진짜로 한 번 더 열어 잔상이 없는지 본다
            {
                var cb = d.Battle as CoreBattle;
                bool lit = false;
                for (int k = 0; k < 4 && !lit; k++)
                {
                    if (cb != null && cb.DebugGlow()) { yield return EndTurn(); yield return WaitInput(); yield return Wait(0.6f); }
                    else break;
                    lit = d.Hand.Cards.Exists(c => c.Info.Epiphany && c.Info.Type != CardType.Power);
                }
                int g2 = d.Hand.Cards.FindIndex(c => c.Info.Epiphany && c.Info.Type != CardType.Power);
                Debug.Log("[Demo] 두 번째 신탁 카드 " + g2 + (g2 >= 0 ? " " + d.Hand.Cards[g2].Info.Name + " 낼수있음=" + d.Battle.CanPlay(g2, out _) : ""));
                if (g2 >= 0 && d.Battle.CanPlay(g2, out _))
                {
                    yield return Drag(g2, d.FirstAliveEnemy());
                    yield return Wait(1.3f);
                    Shot("second_window", 0);
                    yield return Wait(2.5f);
                    Shot("second_after", 0);
                }
            }
            // 비용이 오른 카드 — 적 방해를 흉내 내 한 장 비용을 올리고 깜빡임 · ▲ 를 찍는다
            if (d.Battle is CoreBattle cbc && d.Hand.Cards.Count > 0)
            {
                cbc.DebugRaiseCost(0);
                yield return Wait(0.25f);
                Shot("cost_up_flash", 0);
                yield return Wait(1.0f);
                d.Hand.Cards[0].Hovered = true;
                Shot("cost_up_hand", 0);
                yield return Wait(0.3f);
                CardZoom.Show(d.UiRoot, d.Hand.Cards[0].Info, new Vector3(0, 0.3f, 0), 1.7f);
                yield return Wait(0.6f);
                Shot("cost_up_zoom", 0);
                CardZoom.Hide();
            }
            // 고르는 버리기 · 소멸 카드가 손에 있으면 고르기 창을 띄워 본다
            {
                var cbd = d.Battle as CoreBattle;
                for (int t = 0; t < 6 && cbd != null && !d.Over; t++)
                {
                    int di = -1;
                    for (int q = 0; q < d.Hand.Cards.Count; q++) { if (cbd.DiscardNeed(q, out _) > 0 && d.Battle.CanPlay(q, out _)) { di = q; break; } }
                    if (di >= 0)
                    {
                        Debug.Log("[Demo] 버리기 고르기 카드 " + d.Hand.Cards[di].Info.Name);
                        d.DemoSpendPick = 0;
                        DiscardWindow.OnStage = st => StartCoroutine(ShotLater("discard_window", 0.4f));
                        yield return Drag(di, d.FirstAliveEnemy());
                        yield return Wait(3.0f);
                        break;
                    }
                    yield return EndTurn(); yield return WaitInput(); yield return Wait(0.5f);
                }
            }
            // 툴팁 — 올리기만 해서는 안 뜨고 눌러야 뜬다
            {
                TipZone zone = null;
                Debug.Log("[Demo] 툴팁 구역: " + string.Join(",", Tooltip.Zones.FindAll(q => q != null && q.isActiveAndEnabled).ConvertAll(q => q.name)));
                foreach (var z in Tooltip.Zones) if (z != null && z.isActiveAndEnabled && z.Priority < 100 && z.name != "hpzone" && !z.name.StartsWith("portrait") && z.Text != null && !string.IsNullOrEmpty(z.Text())) { zone = z; break; }
                if (zone != null)
                {
                    Vector2 zp = zone.Center;
                    yield return Click(new Vector2(0, 2.5f), 0.1f); yield return Wait(0.3f);   // 남은 눌림 닫기
                    yield return MoveTo(zp, 0.3f);
                    yield return Wait(1.0f);
                    Debug.Log("[Demo] 툴팁 올림만: " + (Tooltip.AnyShown ? "떴다(실패)" : "안 뜸(통과)"));
                    Shot("tip_hover_only", 0);
                    yield return Click(zp, 0.1f);
                    yield return Wait(0.4f);
                    Debug.Log("[Demo] 툴팁 누름: " + (Tooltip.AnyShown ? "떴다(통과)" : "안 뜸(실패)"));
                    Shot("tip_clicked", 0);
                    yield return Click(new Vector2(0, 1.5f), 0.1f);
                }
                else Debug.Log("[Demo] 툴팁 구역 없음");
            }
            // 일시정지 메뉴 — 일반 / 저사양 / 나가기 확인
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) { Bolzena.RunUI.DisplayOptions.SetLowSpec(true); yield return Wait(0.5f); }
                yield return Wait(0.6f);
                Shot(pass == 0 ? "scene_normal" : "scene_lowspec", 0);
                yield return Wait(0.3f);
                d.OpenPause();
                yield return Wait(0.6f);
                Shot(pass == 0 ? "pause_normal" : "pause_lowspec", 0);
                yield return Wait(0.2f);
                var pu = Modal.Open != null ? Modal.Open.GetComponent<PauseUi>() : null;
                if (pu != null) { pu.DemoAskExit(); yield return Wait(0.5f); Shot(pass == 0 ? "pause_confirm_normal" : "pause_confirm_lowspec", 0); yield return Wait(0.2f); }
                Modal.Open?.Close();
                yield return Wait(0.3f);
            }
            for (int pass = 0; pass < 2; pass++)
            {
                Bolzena.RunUI.DisplayOptions.SetLowSpec(pass == 1);
                yield return Wait(0.4f);
                d.OpenHeroInfo(1);
                yield return Wait(0.6f);
                Shot(pass == 0 ? "party_normal" : "party_lowspec", 0);
                yield return Wait(0.2f);
                Modal.Open?.Close();
                yield return Wait(0.3f);
            }
            Bolzena.RunUI.DisplayOptions.SetLowSpec(false);
            Debug.Log("[Demo] 끝");
            Application.Quit(0);
        }

        IEnumerator Watchdog()
        {
            while (Time.unscaledTime - started < 280f) yield return null;
            Debug.LogError("[Demo] 진행 시간 초과 — 끝낸다(코드 4)");
            Application.Quit(4);
        }
    }
}
