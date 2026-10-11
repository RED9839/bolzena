using System.Collections;
using System.IO;
using Bolzena.Battle;
using Bolzena.UI;
using UnityEngine;

namespace Bolzena.Demo
{
    // 전투 기록 점검(-meterprobe) — 위 오른쪽 「전투 기록」 단추를 눌러 표를 열고, 카드를 내고 턴을 넘기며 값이 오르는지 찍은 뒤
    //   자동 전투로 끝까지 가서 끝 자세의 요약을 찍는다. 가짜 손가락(PointerInput.Simulated)만 쓴다 — 진짜 마우스 아님. 소리 0.
    //   -battle -party … -foes … -meterprobe [-captures <폴더>] (이기게 하려면 -wavehp 0.5 따위)
    //   모든 단계의 표 값을 [MeterProbe] 로 적는다
    public class MeterProbe : MonoBehaviour
    {
        BattleDirector d;
        Capture cap;
        int shotNo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-meterprobe") < 0) return;
            var go = new GameObject("MeterProbe");
            DontDestroyOnLoad(go);
            go.AddComponent<MeterProbe>();
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
            if (d == null) { Debug.Log("[MeterProbe] 전투 없음"); Application.Quit(4); yield break; }
            cap = gameObject.AddComponent<Capture>();
            cap.Dir = Arg("-captures") ?? "C:\\projects\\bolzena-unity-tmp\\meter";
            Directory.CreateDirectory(cap.Dir);
            PointerInput.Simulated = true;
            PointerInput.SimPos = new Vector2(0, -6);
            d.DemoEpiphanyPick = 0; d.DemoSpendPick = 0; d.DemoBranchPick = 0;
            yield return Run();
            Debug.Log("[MeterProbe] 끝");
            yield return Wait(0.5f);
            Application.Quit(0);
        }

        static IEnumerator Wait(float sec) { float t = 0; while (t < sec) { yield return null; t += Time.unscaledDeltaTime; } }

        IEnumerator WaitInput(float limit = 60f)
        {
            float t = 0;
            while (!d.WaitingInput && !d.Over && t < limit) { yield return null; t += Time.unscaledDeltaTime; }
            yield return null;
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

        void Log(string when)
        {
            var s = d.Battle.Snapshot;
            var line = string.Join(" · ", s.Meter.ConvertAll(r => $"{r.Name} 피해 {r.Dealt} 실드 {r.Shield} 회복 {r.Heal}"));
            Debug.Log($"[MeterProbe] {when} — {line}{(s.MeterRun != null ? " (판 탭 있음)" : "")} · 표 {(d.Meter != null && d.Meter.gameObject.activeSelf ? "열림" : "닫힘")}");
        }

        // 낼 수 있는 카드 하나를 끌어 낸다(공격이면 첫 적에게) — 없으면 false
        IEnumerator PlayAny(System.Action<bool> done)
        {
            yield return WaitInput();
            int i = -1;
            for (int k = 0; k < d.Hand.Cards.Count; k++) { var c = d.Hand.Cards[k].Info; if (d.Battle.CanPlay(k, out _) && !c.Epiphany && c.Choices == null) { i = k; break; } }
            if (i < 0 || d.Over) { done(false); yield break; }
            var info = d.Hand.Cards[i].Info;
            int target = d.FirstAliveEnemy();
            Vector3 to = info.Target == TargetKind.Enemy && target >= 0 ? d.FieldRoot.TransformPoint(d.Enemies[target].Center) : new Vector3(0.3f, 0.6f, 0);
            Debug.Log($"[MeterProbe] 카드 {info.Id} ({info.Name})");
            yield return d.Hand.DemoDrag(i, to, 0.4f, 0.5f, 0.3f, null);
            float t = 0;
            while (d.WaitingInput && t < 3f) { yield return null; t += Time.unscaledDeltaTime; }
            yield return WaitInput();
            done(true);
        }

        IEnumerator Run()
        {
            yield return WaitInput();
            yield return Wait(1.0f);
            Shot("start");
            // ① 단추로 표 열기 — 아직 아무도 안 쳤다
            yield return Click(d.Hud.MeterPos);
            yield return Wait(0.6f);
            Log("열자마자");
            Shot("meter_open");
            // ② 카드 셋 — 표가 행동마다 오른다
            for (int n = 0; n < 3; n++)
            {
                bool ok = false;
                yield return PlayAny(v => ok = v);
                if (!ok) break;
                yield return Wait(1.2f);
                Log($"카드 {n + 1}");
            }
            Shot("after_cards");
            // ③ 턴 넘기기 — 적 차례 · 턴 끝 상태(지속 피해 · 결정화 실드)
            yield return WaitInput();
            if (!d.Over)
            {
                yield return Click(d.Hud.EndPos);
                float t = 0;
                while (d.WaitingInput && t < 3f) { yield return null; t += Time.unscaledDeltaTime; }
                yield return WaitInput();
                yield return Wait(1.0f);
                Log("2턴");
                Shot("turn2");
            }
            // ④ 단추로 닫았다가 다시 열기(D 키와 같은 길)
            yield return Click(d.Hud.MeterPos); yield return Wait(0.4f);
            Log("닫음");
            yield return Click(d.Hud.MeterPos); yield return Wait(0.4f);
            Log("다시 엶");
            // ⑤ 자동 전투로 끝까지 — 끝 자세의 요약
            d.Auto = true; d.Hud.SetAuto(true);
            float tt = 0;
            bool mid = false;
            while (!d.Over && tt < 200f)
            {
                yield return null; tt += Time.unscaledDeltaTime;
                if (!mid && tt > 12f) { mid = true; Log("자동 전투 중"); Shot("auto_mid"); }
            }
            Log(d.Battle.Snapshot.Won ? "승리" : "끝(이기지 못함)");
            tt = 0;
            while (d.MeterSummary == null && tt < 15f) { yield return null; tt += Time.unscaledDeltaTime; }
            yield return Wait(0.7f);
            Shot(d.MeterSummary != null ? "summary" : "end_no_summary");
            if (d.MeterSummary != null && d.Battle.Snapshot.MeterRun != null)
            {
                d.MeterSummary.SetTab(1);
                yield return Wait(0.3f);
                Shot("summary_run");
            }
        }
    }
}
