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
            d.Moment += OnMoment;
            UltCutin.OnStage += s => { lastMoment = s; Shot(s, 0); };
            EpiphanyWindow.OnStage += s =>
            {
                Shot(s, 1);
                if (s == "epiphany_window") StartCoroutine(PickEpiphany());
            };
            Banners.OnStage += s => Shot(s, 0);
            StartCoroutine(Run());
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
            Vector3 to = info.Target == TargetKind.Enemy ? EnemyCenter(target) : new Vector3(0.3f, 0.6f, 0);
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
        bool BossWave => d.Battle.Snapshot.Enemies.Exists(e => e.Boss);

        IEnumerator Run()
        {
            started = Time.unscaledTime;
            StartCoroutine(Watchdog());
            yield return WaitInput();
            yield return Wait(0.3f);
            Shot("battle_start", 0);

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
                var fake = new CardInfo { Id = ci.Id + "_t", Name = ci.Name, Hero = ci.Hero, Cost = ci.Cost, Type = ci.Type, TypeName = ci.TypeName, Text = ci.Text, Art = ci.Art, Nature = ci.Nature, Terms = ci.Terms, Tags = ci.Tags, Choices = new List<string> { "베어 가르기", "막아 서기" } };
                int got = -1;
                StartCoroutine(UI.ChoiceWindow.Run(d.UiRoot, fake, b => got = b, 2));
                yield return Wait(0.6f);
                Shot("choice_window", 0);
                while (got < 0) yield return null;
                Debug.Log("[Demo] 두 갈래 고름 " + got);
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

        IEnumerator Watchdog()
        {
            while (Time.unscaledTime - started < 280f) yield return null;
            Debug.LogError("[Demo] 진행 시간 초과 — 끝낸다(코드 4)");
            Application.Quit(4);
        }
    }
}
