using System.Collections;
using System.Collections.Generic;
using System.IO;
using Bolzena.Battle;
using Bolzena.UI;
using UnityEngine;

namespace Bolzena.Demo
{
    // 변신 자세 점검(-formprobe) — 디아나(왕년) 따위 변신 사도가 변신한 뒤 다른 공격 카드를 써도 자세(꼬리 동작)가 유지되는지.
    //   -battle -party 디아나_왕년 -foes … -deck … -formprobe -captures <폴더> — 가짜 손가락(PointerInput.Simulated)만 쓴다. 진짜 마우스 아님.
    //   모든 동작 바뀜을 [FormProbe] 로 적는다(시각 · 동작 이름 · 꼬리 · 사도 위치)
    public class FormProbe : MonoBehaviour
    {
        BattleDirector d;
        Capture cap;
        int shotNo;
        static readonly bool Fast = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-formfast") >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-formprobe") < 0) return;
            var go = new GameObject("FormProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<FormProbe>();
        }

        static string Arg(string name)
        {
            var a = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return null;
        }

        IEnumerator Start()
        {
            AudioListener.volume = 0f;
            float t = 0;
            while ((BattleDirector.I == null || !BattleDirector.I.Ready) && t < 120f) { t += Time.unscaledDeltaTime; yield return null; }
            d = BattleDirector.I;
            if (d == null) { Debug.Log("[FormProbe] 전투 없음"); Application.Quit(4); yield break; }
            cap = gameObject.AddComponent<Capture>();
            cap.Dir = Arg("-captures") ?? "C:\\projects\\bolzena-unity-tmp\\diana_form";
            Directory.CreateDirectory(cap.Dir);
            PointerInput.Simulated = true;
            PointerInput.SimPos = new Vector2(0, -6);
            d.DemoEpiphanyPick = 0; d.DemoSpendPick = 0;
            StartCoroutine(Watch());
            yield return Run();
            Debug.Log("[FormProbe] 끝");
            yield return Wait(0.5f);
            Application.Quit(0);
        }

        // 동작이 바뀔 때마다 적는다
        IEnumerator Watch()
        {
            string last = null;
            while (true)
            {
                var u = d.Heroes.Count > 0 ? d.Heroes[0] : null;
                if (u != null && u.Sa != null)
                {
                    var cur = u.Sa.AnimationState.GetCurrent(0);
                    string n = cur != null ? cur.Animation.Name + (cur.Loop ? "(loop)" : "") : "-";
                    if (n != last)
                    {
                        Debug.Log($"[FormProbe] t={Time.unscaledTime:F2} 동작 {n} · 꼬리 {(u.FormTail ?? "없음")} · 자리 {(u.Feet != u.Home ? "밖" : "집")}");
                        last = n;
                    }
                }
                yield return null;
            }
        }

        static IEnumerator Wait(float sec) { float t = 0; while (t < sec) { yield return null; t += Time.unscaledDeltaTime; } }

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

        IEnumerator MoveTo(Vector2 at, float dur = 0.25f)
        {
            Vector2 from = PointerInput.SimPos; float t = 0;
            while (t < dur) { PointerInput.SimPos = Vector2.Lerp(from, at, Ease.InOutCubic(t / dur)); yield return null; t += Time.unscaledDeltaTime; }
            PointerInput.SimPos = at;
        }

        IEnumerator Click(Vector2 at, float hover = 0.3f)
        {
            yield return MoveTo(at); yield return Wait(hover);
            PointerInput.SimHeld = true; yield return null; yield return null;
            PointerInput.SimHeld = false; yield return null; yield return null;
        }

        void Shot(string name) { shotNo++; cap.Still(shotNo.ToString("D2") + "_" + name); }

        int Find(string baseId)
        {
            for (int i = 0; i < d.Hand.Cards.Count; i++)
                if (Bolzena.Core.GameData.BaseId(d.Hand.Cards[i].Info.Id) == baseId && d.Battle.CanPlay(i, out _)) return i;
            return -1;
        }

        // 카드 한 장을 끌어 내고, 그 몸짓 도중(mid 초)과 끝난 뒤(after 초)에 캡처한다
        IEnumerator Play(string baseId, string tag, float mid = 0.5f, float after = 2.2f)
        {
            yield return WaitInput();
            int i = Find(baseId);
            if (i < 0) { Debug.Log($"[FormProbe] 손에 없음/못 냄 {baseId} · 손 {string.Join(",", d.Hand.Cards.ConvertAll(c => c.Info.Id))} AP?"); yield break; }
            var info = d.Hand.Cards[i].Info;
            Debug.Log($"[FormProbe] t={Time.unscaledTime:F2} 카드 {info.Id} ({info.Name}) 비용 {info.Cost} 몸짓 {info.Motion}");
            int target = d.FirstAliveEnemy();
            Vector3 to = info.Target == TargetKind.Enemy ? d.FieldRoot.TransformPoint(d.Enemies[target].Center) : new Vector3(0.3f, 0.6f, 0);
            if (!Fast) StartCoroutine(ShotLater(tag + "_mid", mid + 0.9f));
            yield return d.Hand.DemoDrag(i, to, 0.4f, 0.5f, 0.3f, null);
            yield return WaitTurnDone();
            if (Fast) { Shot(tag + "_now"); yield break; }
            yield return Wait(after);
            Shot(tag + "_after");
            yield return Wait(0.3f);
        }

        IEnumerator ShotLater(string name, float sec) { yield return Wait(sec); Shot(name); }

        IEnumerator EndTurn()
        {
            yield return WaitInput();
            yield return Click(d.Hud.EndPos);
            yield return WaitTurnDone();
            yield return Wait(1.0f);
        }

        IEnumerator Run()
        {
            yield return WaitInput();
            yield return Wait(1.0f);
            Shot("start");
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-formseq") >= 0) cap.BeginSeq("form", 6);   // 동작 줄 — 접촉 시트용
            // -formcard <카드 id> -formhit <카드 id>: 아무 변신 · 태세 사도나 — 태세 카드 → 공격 카드 → 턴 끝 → 다음 턴 시작(2026-10-11 엘다인 태세 점검)
            // -formult: 고학년으로 들기(네르 · 죠안 — 원작도 고학년 쓰면 모습이 바뀐다). -wavegauge 로 게이지를 채워 둔다
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-formult") >= 0)
            {
                yield return WaitInput();
                Debug.Log($"[FormProbe] t={Time.unscaledTime:F2} 고학년");
                d.RequestUlt(0);
                yield return Wait(1.2f); Shot("ult_mid");
                yield return WaitTurnDone();
                yield return Wait(2.0f); Shot("ult_after");
                var h2 = Arg("-formhit");
                if (h2 != null) yield return Play(h2, "form_hit");
                yield return EndTurn();
                Shot("turn2_start");
                yield break;
            }
            var fc = Arg("-formcard");
            if (fc != null)
            {
                yield return Play(fc, "form_on");
                var hit = Arg("-formhit");
                if (hit != null) yield return Play(hit, "form_hit");
                yield return EndTurn();
                Shot("turn2_start");
                yield return Wait(1.0f);
                Shot("final");
                yield break;
            }
            // 1턴: 변신(u2 부수기) → 도끼 내려찍기(s2, 변신 중에도 도끼 카드)
            yield return Play("디아나_왕년_u2", "u2_form");
            yield return Play("디아나_왕년_s2", "s2_axe");
            yield return Play("디아나_왕년_s1", "s1_fist");
            if (Fast) { yield return Wait(3f); Shot("fast_t1_end"); }
            yield return EndTurn();
            // 2턴: 바위 던지기(u4) · 맨주먹 · 찢기(u1, 변신을 푼다)
            Shot("turn2_start");
            yield return Play("디아나_왕년_u4", "u4_rock");
            yield return Play("디아나_왕년_s1", "s1_fist2");
            if (Fast) { yield return Wait(3f); Shot("fast_t2_end"); }
            yield return Play("디아나_왕년_u1", "u1_end");
            yield return Wait(1.0f);
            Shot("final");
        }
    }
}
