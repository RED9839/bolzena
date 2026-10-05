using System.Collections;
using Bolzena.Battle;
using Bolzena.UI;
using UnityEngine;
using RunDemo = Bolzena.RunUI.Demo;

namespace Bolzena.Demo
{
    // 한 판 자동 데모의 전투 손가락 — 판에서 넘어온 싸움(BattleBridge)을 가짜 손가락으로 끝까지 싸운다.
    //   고학년이 차면 쓰고(얼굴 → 적), 아니면 낼 수 있는 카드(공격 먼저 · 가장 약한 적에게), 없으면 턴 종료.
    //   캡처는 판 화면 데모(RunUI.Demo.Snap)와 같은 번호 줄로 — 싸움마다 시작 · 첫 타격 · 고학년 · 보스 등장 · 끝.
    //   지기 데모(FightMode lose)는 카드를 내지 않고 턴을 넘긴다.
    public class RunFightBot : MonoBehaviour
    {
        static int fightNo;
        BattleDirector d;
        string label;
        bool lose;

        public static void Attach(BattleDirector director)
        {
            var go = new GameObject("RunFightBot");
            var b = go.AddComponent<RunFightBot>();
            b.d = director;
        }

        void Start()
        {
            fightNo++;
            var t = BattleBridge.Fight?.Ticket;
            bool boss = t != null && t.B.Enemies.Exists(e => e.Boss);
            lose = RunDemo.FightMode == "lose";
            label = $"fight{fightNo}" + (boss ? "_boss" : t != null && t.Event ? "_event" : "");
            PointerInput.Simulated = true;
            PointerInput.SimPos = new Vector2(0, -6);
            Clock.Speed = 2f;                     // 데모는 2배속
            d.Hud.SetSpeed(Clock.Speed);
            d.DemoEpiphanyPick = 0;
            d.Moment += OnMoment;
            UltCutin.OnStage += OnUlt;
            Banners.OnStage += OnBanner;
            StartCoroutine(Run());
            StartCoroutine(Guard());
        }

        void OnDestroy()
        {
            // 정적 이벤트에서 떼어 낸다 — 다음 싸움에서 사라진 봇을 부르지 않게
            UltCutin.OnStage -= OnUlt;
            Banners.OnStage -= OnBanner;
            PointerInput.Simulated = false;
        }

        void Snap(string what) { if (RunDemo.Me != null) RunDemo.Me.Snap(label + "_" + what); }

        void OnMoment(string m)
        {
            if (m == "hit" || m == "boss_in" || m == "victory") StartCoroutine(Later(m, m == "hit" ? 1 : 4));
        }
        void OnUlt(string s) { if (s == "ult2_slide") Snap("ult"); }
        void OnBanner(string s) { if (s == "boss_entry") Snap("boss_entry"); }

        IEnumerator Later(string what, int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
            Snap(what);
        }

        // 싸움 하나가 너무 길면(봇이 막혔다) 오류로 끝낸다 — 지킴이(DemoGuard)가 예외를 보고 데모를 끈다
        IEnumerator Guard()
        {
            float t = 0;
            while (t < 300f) { t += Time.unscaledDeltaTime; yield return null; }
            throw new System.TimeoutException("[Demo] 싸움 하나가 300초를 넘었습니다 — " + label);
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

        IEnumerator MoveTo(Vector2 at, float dur = 0.2f)
        {
            Vector2 from = PointerInput.SimPos;
            float t = 0;
            while (t < dur) { PointerInput.SimPos = Vector2.Lerp(from, at, Ease.InOutCubic(t / dur)); yield return null; t += Time.unscaledDeltaTime; }
            PointerInput.SimPos = at;
        }

        IEnumerator Click(Vector2 at, float hover = 0.15f)
        {
            yield return MoveTo(at);
            yield return Wait(hover);
            PointerInput.SimHeld = true;
            yield return null;
            yield return null;
            PointerInput.SimHeld = false;
            yield return null;
            yield return null;
        }

        int Weakest()
        {
            var s = d.Battle.Snapshot;
            int best = -1;
            for (int i = 0; i < s.Enemies.Count && i < d.Enemies.Count; i++)
                if (!s.Enemies[i].Dead && (best < 0 || s.Enemies[i].Hp < s.Enemies[best].Hp)) best = i;
            return best >= 0 ? best : d.FirstAliveEnemy();
        }

        Vector3 EnemyCenter(int i) => d.FieldRoot.TransformPoint(d.Enemies[Mathf.Clamp(i, 0, d.Enemies.Count - 1)].Center);

        int PickCard()
        {
            int pick = -1;
            for (int i = 0; i < d.Hand.Cards.Count; i++)
                if (d.Battle.CanPlay(i, out _) && d.Hand.Cards[i].Info.Type == CardType.Attack) { pick = i; break; }
            if (pick < 0)
                for (int i = 0; i < d.Hand.Cards.Count; i++)
                    if (d.Battle.CanPlay(i, out _)) { pick = i; break; }
            return pick;
        }

        IEnumerator Run()
        {
            yield return WaitInput();
            yield return Wait(0.3f);
            Snap("start");
            int acts = 0;
            while (!d.Over && acts++ < 200)
            {
                yield return WaitInput();
                if (d.Over) break;
                if (lose) { yield return Click(d.Hud.EndPos); yield return WaitTurnDone(); continue; }
                // 고학년
                int ult = -1;
                for (int h = 0; h < d.Hud.Ults.Count; h++) if (d.Battle.CanUlt(h, out _)) { ult = h; break; }
                if (ult >= 0)
                {
                    yield return Click(d.Hud.Ults[ult].transform.position, 0.2f);
                    int t = Weakest();
                    yield return MoveTo(EnemyCenter(t), 0.25f);
                    yield return Wait(0.2f);
                    yield return Click(EnemyCenter(t), 0.05f);
                    yield return WaitTurnDone();
                    continue;
                }
                int pick = PickCard();
                if (pick >= 0)
                {
                    var info = d.Hand.Cards[pick].Info;
                    int t = Weakest();
                    Vector3 to = info.Target == TargetKind.Enemy ? EnemyCenter(t) : new Vector3(0.3f, 0.6f, 0);
                    yield return d.Hand.DemoDrag(pick, to, 0.15f, 0.3f, 0.15f);
                    yield return WaitTurnDone();
                    continue;
                }
                yield return Click(d.Hud.EndPos);
                yield return WaitTurnDone();
            }
            if (!d.Over) Debug.LogWarning("[Demo] 봇이 행동 200번 안에 싸움을 끝내지 못했습니다 — " + label);
            Debug.Log($"[Demo] {label} 끝 — {(d.Battle.Snapshot.Won ? "승리" : "패배")} · 행동 {acts}");
            yield return Wait(lose ? 0.6f : 0.2f);
            if (lose) Snap("defeat");
        }
    }
}
