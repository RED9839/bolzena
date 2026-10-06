using System;
using System.Collections;
using System.IO;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;

namespace Bolzena.Demo
{
    // 카드 글 가독성 점검(-battle -cardtextshot, 2026-10-07 「찢기의 『손패가 없으면』 이 헷갈린다 · 줄 띄워 가독성」) — 대표 카드들을
    //   전투 카드 확대(CardZoom — 카드 + 오른쪽 낱말 판)로 한 장씩 찍는다. 줄 나눈 효과 글 · 글 칸 높이 · 고유 효과 · 변신 낱말 판을 본다.
    //   -cards a,b,c 로 카드 id 를 고를 수 있다(없으면 아래 대표 22장). 찍고 나면 스스로 끝난다(소리는 -demo 가 끈다).
    public class CardTextShot : MonoBehaviour
    {
        BattleDirector d;

        public static readonly string[] Default =
        {
            "디아나_왕년_u1", "디아나_왕년_u4", "디아나_왕년_u3", "디아나_왕년_f1", "네르_빡침_f1", "포셔_u2", "로네_u2", "피코라_u4", "버터_u1", "에르핀_u1", "멜루나_u1",
            "시저_u1", "란_u1", "루포_u4", "비비_신성_u1", "다야_퓨어샤인_u1", "제이드_u3", "로네_u3", "캬롯_sprout", "네르_빡침_u1", "라이카_f1", "죠안_f1",
        };

        public static void Attach(BattleDirector director)
        {
            var s = director.gameObject.AddComponent<CardTextShot>();
            s.d = director;
        }

        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        IEnumerator Start()
        {
            AudioListener.volume = 0f;   // 점검은 음소거
            float t = 0;
            while (!d.WaitingInput && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.0f);
            var core = d.Battle as CoreBattle;
            if (core == null) { Debug.LogError("[CardTextShot] 코어 전투가 아님"); Application.Quit(2); yield break; }
            string dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            Directory.CreateDirectory(dir);
            // 손패가 매 프레임 CardZoom.Hide() 를 부른다(올린 카드가 없으면) — 찍는 동안 손패를 멈춘다
            HandView hand = null;
            foreach (var hv in FindObjectsByType<HandView>(FindObjectsSortMode.None)) { hv.enabled = false; hand = hv; }
            var ids = Arg("-cards") is string list ? list.Split(',') : Default;
            int n = 0;
            foreach (var id in ids)
            {
                CardInfo info = null;
                try { info = core.InfoOf(id); } catch (Exception e) { Debug.LogWarning($"[CardTextShot] {id} — {e.Message}"); }
                if (info == null || info.Name == "?") { Debug.LogWarning($"[CardTextShot] 없는 카드 {id}"); continue; }
                AudioListener.volume = 0f;
                // 실제 손패 꾹 누르기와 같은 자리 · 크기(HandView.ZoomAt — 화면 가운데 위) · 같은 부모
                if (hand != null) { var at = hand.ZoomAt(out float zs); CardZoom.Show(hand.transform.parent, info, at, zs); }
                else CardZoom.Show(d.UiRoot, info, Vector3.zero);
                yield return new WaitForSecondsRealtime(0.5f);
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                string name = $"battle_{++n:00}_{id}.png";
                File.WriteAllBytes(Path.Combine(dir, name), tex.EncodeToPNG());
                Destroy(tex);
                Debug.Log($"[CardTextShot] {name} — 판 {CardZoom.PanelCount} · {info.Text.Replace("\n", " ⏎ ")}");
                CardZoom.Hide();
                yield return null;
            }
            Debug.Log($"[CardTextShot] 끝 — {n}장");
            Application.Quit(0);
        }
    }
}
