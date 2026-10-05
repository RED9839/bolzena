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
    public class BattleBridge : IFightScreen
    {
        public const string RunScene = "Run", BattleScene = "Battle";

        public class Pending
        {
            public RunPort Port;
            public RunPort.FightTicket Ticket;
            public CoreBattle Battle;
            public Action<RunPort.FightOutcome> Done;
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
            Fight = new Pending { Port = port, Ticket = t, Battle = cb, Done = done };
            // 이기면 판 화면의 「배경 없이 보상 열기」(Flow.RewardOverlay)를 전투 장면 위에 띄운다 — 판 자동 데모는 예전 길(보상 화면)로
            EndHold = Overlay;   // runui RewardOverlay 가 시작 때 Fighting 을 끄므로 판 자동 데모도 이 길로
            Debug.Log($"[Bridge] 싸움 열기 — {string.Join(", ", t.Foes)} (배경 {t.Bg ?? "-"}, 이벤트 {t.Event})");
            Flow.Me.Stage.Canvas.enabled = false;
            SceneManager.LoadScene(BattleScene);
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

        public static void Return()
        {
            var p = Fight;
            if (p == null || finished) return;
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
