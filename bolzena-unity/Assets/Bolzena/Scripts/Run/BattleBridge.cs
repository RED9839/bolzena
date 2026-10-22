using System;
using System.Collections.Generic;
using Bolzena.Battle;
using Bolzena.RunUI;
using UnityEngine;
using UnityEngine.SceneManagement;
using RunDemo = Bolzena.RunUI.Demo;

namespace Bolzena
{
    // 판 화면(com.bolzena.runui) ↔ 전투 화면(Battle 장면)의 다리.
    //   Flow.FightStop → Open: 코어 판이 싸움을 열고(Run.OpenFight — 쪽지는 CoreBattle 이 받는다) 판 화면을 감춘 뒤 Battle 장면으로.
    //   BattleDirector 가 끝을 보여 주면 Return: 판에 결과를 넣고(RunPort.Finish → Run.AfterFight) Run 장면으로 돌아와 판 화면을 다시 켠다.
    //   프론티어(OpenFrontier): 판 없이 저장 사도 슬롯 · 시즌 · 서버 씨앗으로 Core.Frontier.Start 한 전투를 같은 길로 연다(신탁 · 은총 없이 — 엔진이 끈다).
    //   끝(보스 처치 · 쓰러짐 · 턴 제한 TimeUp)을 보여 주면 Return: 입력 테이프를 닫아 FrontierTape.Json() 글을 그대로 들고 Run 장면으로 돌아와 결과를 넘긴다.
    public class BattleBridge : IFightScreen, IFrontierFight
    {
        public const string RunScene = "Run", BattleScene = "Battle";

        public class Pending
        {
            public RunPort Port;
            public RunPort.FightTicket Ticket;
            public CoreBattle Battle;
            public Action<RunPort.FightOutcome> Done;
            /// <summary>프론티어 전투면 시즌(판 전투는 null — Port · Ticket 이 null 이다).</summary>
            public Bolzena.Core.FrontierSeason Frontier;
            public List<Bolzena.Core.HeroSlot> FrontierSlots;
            public Action<FrontierResult> FrontierDone;
            /// <summary>시험 덱 전투(「덱 만들기」 — 규칙 검사 밖 슬롯 · 사도별 딜 측정).</summary>
            public bool Lab;
            /// <summary>배경(Ticket 이 없을 때 — 프론티어).</summary>
            public string Bg;
        }

        /// <summary>프론티어 싸움터 배경.</summary>
        public const string FrontierBg = "stage37_1";

        public void OpenFrontier(Bolzena.Core.GameData data, List<Bolzena.Core.HeroSlot> slots, Bolzena.Core.FrontierSeason season, long seed, Action<FrontierResult> done, bool lab = false, Bolzena.Core.ProgState prog = null)
        {
            var cb = CoreBattle.ForRun(data);
            var gear = new Dictionary<string, Dictionary<string, string>>();
            foreach (var s in slots) gear[s.Hero] = new Dictionary<string, string>(s.Gear);
            CoreBattle.GearOf = k => gear.TryGetValue(k, out var g) ? g : null;
            // 시험 덱(lab)은 규칙 검사 밖 슬롯도 열고(strict false) · 사도별 딜을 잰다(Battle.HpMeter — 측정 전용, Battle.Start 때 켜져 있어야 그 전투를 잰다 · 점수 · 테이프 · 지문 무관)
            Bolzena.Core.Battle b;
            Bolzena.Core.Battle.HpMeterOn = lab;
            try { b = Bolzena.Core.Frontier.Start(data, slots, season, seed, cb.CueSink, strict: !lab, prog: prog); }   // prog — 서버가 떠 둔 성장(성장 켬일 때만, 없으면 null = 지금과 같다)   // 셋업 · 입력 테이프(Record)까지 — 서버 검증기가 같은 슬롯 · 씨앗에서 똑같이 짓는다
            finally { Bolzena.Core.Battle.HpMeterOn = false; }
            cb.Attach(b);
            cb.FrontierSeason = season;
            cb.RunTally = null;
            Fight = new Pending { Battle = cb, Frontier = season, FrontierSlots = slots, FrontierDone = done, Bg = FrontierBg, Lab = lab };
            PlayRecord.FightSpeed = () => 0;   // 판 기록(전투 시간)에 섞지 않는다
            EndHold = null;                    // 보상 오버레이 없음 — 끝을 보여 주면 곧장 결과로
            Debug.Log($"[Bridge] 프론티어 열기{(lab ? "(시험 덱)" : "")} — {season.Id}/{season.Boss}/{season.Turns}턴 · 씨앗 {seed} · [{string.Join(", ", slots.ConvertAll(s => s.Hero))}] · 파티 HP {b.Pool.Hp}");
            Hitch.Mark("프론티어 열기");
            Flow.Me.StartCoroutine(Enter());
        }

        // 프론티어 끝 — 테이프를 닫아 글로(FrontierTape.Json() 그대로 — 다시 직렬화하지 않는다) Run 장면으로 돌아가 결과를 넘긴다
        static void ReturnFrontier(Pending p, bool quit)
        {
            Clock.Reset();
            var res = new FrontierResult();
            var fb = p.Battle?.Fight;
            try
            {
                if (fb == null) res.Error = "전투가 없다";
                else
                {
                    res.Over = Bolzena.Core.Frontier.Over(fb, p.Frontier);
                    res.Score = Bolzena.Core.Frontier.Score(fb);
                    res.Played = Math.Min(fb.Turn, p.Frontier.Turns);
                    if (p.Lab && fb.HpMeter != null)
                    {
                        res.Meter = new Dictionary<string, long[]>();
                        foreach (var kv in fb.HpMeter) res.Meter[kv.Key] = (long[])kv.Value.Clone();
                    }
                    if (quit || res.Over == null) res.Error = "전투를 끝까지 하지 않아 낼 수 없다";
                    else
                    {
                        var tape = Bolzena.Core.Frontier.Tape(fb, p.Frontier, p.FrontierSlots);
                        res.Tape = tape.Json();
                        res.Score = tape.Score; res.Over = tape.Over; res.Played = tape.Played;
                    }
                }
            }
            catch (Exception e) { Debug.LogException(e); res.Error = "기록을 닫지 못했습니다 — " + e.Message; res.Tape = null; }
            Debug.Log($"[Bridge] 프론티어 끝 — {res.Over ?? "-"} · {res.Played}턴 · 점수 {res.Score} · 테이프 {(res.Tape != null ? $"{res.Tape.Length / 1024f:0.0}KB" : "없음")}");
            void Loaded(Scene s, LoadSceneMode m)
            {
                SceneManager.sceneLoaded -= Loaded;
                Fight = null; finished = false;
                PlayRecord.FightSpeed = null;
                Flow.Me.Stage.Canvas.enabled = true;
                p.FrontierDone?.Invoke(res);
            }
            SceneManager.sceneLoaded += Loaded;
            SceneManager.LoadScene(RunScene);
        }

        /// <summary>끝 자세(싸움터를 둔 채 HUD 를 걷은 모습) 위에서 붙들 손 — 판 화면의 「배경 없이 보상 열기」 가 생기면 여기에 그것을 꽂는다
        /// (보상을 띄우고 「떠나기」 를 누를 때까지 기다리는 코루틴). null 이면 곧바로 판으로 돌아간다.</summary>
        public static Func<bool, System.Collections.IEnumerator> EndHold;

        /// <summary>지금 열린 싸움(없으면 전투 시범).</summary>
        public static Pending Fight { get; private set; }

        public void Open(RunPort port, Action<RunPort.FightOutcome> done)
        {
            // 자동 데모는 싸움을 줄인다 — 적 체력 · 피해를 낮춰 한 판이 몇 분 안에 끝나게(지기 데모는 적 피해를 크게)
            double hpx = 1, dmgx = 1;
            if (RunDemo.Active)
            {
                bool lose = RunDemo.FightMode == "lose";
                hpx = lose ? 1 : port.IsBoss ? 0.3 : 0.35;
                dmgx = lose ? 80 : 0.5;
            }
            var cb = CoreBattle.ForRun(port.Data);
            CoreBattle.GearOf = k => { try { return port.GearOf(k); } catch (Exception) { return null; } };
            var t = port.OpenFight(hpx, dmgx, cb.CueSink);
            cb.Attach(t.B);
            // 판 전체 전투 기록의 앞몫 — 이 싸움 전까지의 합을 베껴 둔다(싸움이 끝나면 AfterFight 가 판에 더하므로 열 때 값으로 고정)
            cb.RunTally = new Dictionary<string, Bolzena.Core.HeroTally>();
            if (port.S?.Tally != null) foreach (var kv in port.S.Tally) cb.RunTally[kv.Key] = kv.Value.Copy();
            Fight = new Pending { Port = port, Ticket = t, Battle = cb, Done = done };
            PlayRecord.FightSpeed = () => Fight != null && !finished && !Clock.Paused ? Clock.Speed : 0;   // 플레이 기록 — 전투 시간 · 2배속 몫
            // 이기면 판 화면의 「배경 없이 보상 열기」(Flow.RewardOverlay)를 전투 장면 위에 띄운다 — 판 자동 데모는 예전 길(보상 화면)로
            EndHold = Overlay;   // runui RewardOverlay 가 시작 때 Fighting 을 끄므로 판 자동 데모도 이 길로
            Debug.Log($"[Bridge] 싸움 열기 — {string.Join(", ", t.Foes)} (배경 {t.Bg ?? "-"}, 이벤트 {t.Event})");
            Demo.EnterLeak.MarkOpen();
            Hitch.Mark("싸움 열기");
            Flow.Me.StartCoroutine(Enter());
        }

        /// <summary>암전 길이(초) — 판 화면을 검게 덮는 동안 남은 정리를 하고 전투 장면을 읽는다.</summary>
        public const float BlackoutSec = 0.18f;

        // 전투로 — ① 판 화면을 검게 덮고 ② 덮인 동안 지난 화면의 찌꺼기를 치운다(GC · UnloadUnusedAssets, 비동기)
        //   ③ 전투 장면을 비동기로 읽는다(그 장면 읽기에 딸린 정리도 검은 화면 뒤에서) ④ 전투가 서면(제 암전 ScreenFx 가 검게 시작한다)
        //   판 화면 캔버스를 끄고 덮개를 걷는다. 예전에는 캔버스를 끄자마자 같은 프레임에 장면을 통째로 읽어, 지도가 멈춘 채 굳어 보였다
        static System.Collections.IEnumerator Enter()
        {
            var stage = Flow.Me.Stage;
            yield return stage.Blackout(BlackoutSec);
            // 로딩 화면(2026-10-07) — 전투 쪽(BattleDirector.Boot)이 스파인 · 이펙트 · 셰이더를 여러 프레임에 나눠 준비하는 동안 덮고 진행을 보인다
            LoadingScreen.Show("전투 준비 중");
            LoadingScreen.Progress(0.03f, "정리하는 중");
            yield return null;                                   // 로딩 화면이 한 번 그려진 뒤에
            using (Hitch.Span("GC.Collect")) GC.Collect();
            yield return Resources.UnloadUnusedAssets();
            LoadingScreen.Progress(0.08f, "전투 장면 읽는 중");
            yield return WebBundles.WaitAll();                   // 웹 — 전투 스파인 번들을 덜 받았으면 여기서 기다린다(로딩 화면)
            void Loaded(Scene s, LoadSceneMode m)
            {
                if (s.name != BattleScene) return;
                SceneManager.sceneLoaded -= Loaded;
                if (Flow.Me == null) return;
                Flow.Me.Stage.Canvas.enabled = false;
                Flow.Me.Stage.ClearFade();
            }
            SceneManager.sceneLoaded += Loaded;
            var op = SceneManager.LoadSceneAsync(BattleScene);
            while (op != null && !op.isDone) { LoadingScreen.Progress(0.08f + 0.07f * op.progress); yield return null; }
        }

        // 끝 자세 위에 보상 — 결과를 넣고(Finish) 판 화면 캔버스를 켜 보상 줄을 띄운 뒤, 「떠나기」 가 눌리면 Run 장면으로(판은 Flow 가 이어 간다)
        static System.Collections.IEnumerator Overlay(bool won)
        {
            var p = Fight;
            if (p == null || Flow.Me == null) yield break;
            Clock.Reset();
            var o = p.Port.Finish(p.Ticket);
            finished = true;
            Debug.Log($"[Bridge] 싸움 끝(오버레이) — {(o.Won ? "승리" : "패배")} · {o.Turns}턴");
            bool left = false;
            Flow.Me.Stage.Canvas.enabled = true;
            OnOverlay?.Invoke();
            Flow.Me.RewardOverlay(o, () => left = true);
            while (!left) yield return null;
            Fight = null;
            finished = false;
            SceneManager.LoadScene(RunScene);
        }
        static bool finished;
        /// <summary>보상 오버레이가 막 떴다(전투 HUD 를 걷을 때 · 데모 캡처).</summary>
        public static Action OnOverlay;

        /// <summary>일시정지 메뉴의 「메인 화면으로」 — 싸움을 끝내지 않고(저장은 지도 · 캠프에서 마지막으로 한 그대로) 판 장면으로 돌아가 로비를 연다. 이어하기는 그 저장에서.</summary>
        public static void LeaveToLobby()
        {
            if (Fight == null || Flow.Me == null) { Debug.Log("[Bridge] 판이 없어(전투 시범) 메인 화면으로 못 나감"); return; }
            if (Fight.Frontier != null) { var fp = Fight; ReturnFrontier(fp, true); return; }   // 프론티어 — 그만둠(시도는 쓴 것으로 센다 · 낼 수 없다)
            PlayRecord.Send(Fight.Port, "quit");   // 플레이 기록 — 메인으로(판은 이어할 수 있다)
            Clock.Reset();
            void Loaded(Scene s, LoadSceneMode m)
            {
                SceneManager.sceneLoaded -= Loaded;
                Fight = null; finished = false;
                Flow.Me.Stage.Canvas.enabled = true;
                Flow.Me.Lobby();
            }
            SceneManager.sceneLoaded += Loaded;
            SceneManager.LoadScene(RunScene);
        }

        public static void Return()
        {
            var p = Fight;
            if (p == null || finished) return;
            if (p.Frontier != null) { finished = true; ReturnFrontier(p, false); return; }
            Clock.Reset();
            var o = p.Port.Finish(p.Ticket);
            Debug.Log($"[Bridge] 싸움 끝 — {(o.Won ? "승리" : "패배")} · {o.Turns}턴 · 파티 HP {o.HpBefore} → {o.HpAfter}");
            void Loaded(Scene s, LoadSceneMode m)
            {
                SceneManager.sceneLoaded -= Loaded;
                Fight = null;
                Flow.Me.Stage.Canvas.enabled = true;
                p.Done(o);
            }
            SceneManager.sceneLoaded += Loaded;
            SceneManager.LoadScene(RunScene);
        }

        // 층 배경 — 판 화면 그림(RunArt/Bg, runui copy_assets.py)을 싸움터 스프라이트로
        static readonly Dictionary<string, Sprite> bgs = new Dictionary<string, Sprite>();
        public static Sprite BgSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (bgs.TryGetValue(name, out var sp)) return sp;
            var tex = Resources.Load<Texture2D>("RunArt/Bg/" + name);
            sp = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
            if (sp == null) Debug.LogWarning("[Bridge] 배경 없음: " + name);
            bgs[name] = sp;
            return sp;
        }
    }
}
