using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 축복 연출 캡처(-demo-bless, 2026-10-07) — 휴식 수련에서 축복이 얹힌 신탁을 고르는 장면.
    //   수련 후보 둘째에 축복을 얹고(점검 — 실제는 R.ORACLE_BLESS 확률) 그것을 고른다: 고르기 창 → 고른 순간부터 0.1초마다(camp_pick_NN) → 끝난 뒤 →
    //   덱 보기 · 카드 크게(축복 표식). -oldbless 를 같이 주면 예전 연출로 같은 장면. 저장은 -save 로 따로 둔다(사용자 이어하기를 건드리지 않게).
    public partial class Demo
    {
        IEnumerator Bless_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.0f);
            var S = f.P.S; var run = f.P.Run; var d = f.P.Data;
            foreach (var u in d.UniquesOf(S.Party[0]).Take(2)) if (!S.Deck.Contains(u)) S.Deck.Add(u);
            f.Camp("camp");
            yield return Screen_("camp", 1.0f);
            var tr = S.Camp?.Train;
            if (tr == null || tr.Picks.Count < 2) { Debug.LogError("[Demo] 수련 후보가 없습니다"); Application.Quit(5); yield break; }
            var c = d.Card(tr.CardId);
            var ks = c.Blesses.Count > 0 ? new List<string> { "own" } : Core.Run.DivineKindsFor(run.ViewOf(tr.CardId));
            string kind = ks.Count > 0 ? ks[0] : "draw";
            tr.Shins = Enumerable.Range(0, tr.Picks.Count).Select(i => i == 1 ? kind : null).ToList();
            Debug.Log($"[Demo] 수련 「{c.Name}」 둘째 후보에 축복 {kind}{(BlessFx.Old ? " (예전 연출)" : "")}");
            yield return Press("camp.trainopen", 0.1f);
            yield return Until(() => OracleReveal.Phase == "choose", 5); yield return Wait(0.35f);
            yield return Shot("bless_camp_choose");
            var frames = StartCoroutine(Frames("camp_pick", 2.6f));
            yield return Press("camp.train1", 0.05f);
            yield return frames;
            yield return Until(() => !OracleReveal.IsOpen, 8); yield return Wait(0.8f);
            yield return Shot("bless_camp_after");
            Expect(S.Shin.ContainsKey(tr.CardId), $"수련으로 축복이 얹혔다 — {tr.CardId}");
            f.DeckView();
            yield return Wait(1.6f);
            yield return Shot("bless_deck");
            CloseModals();
            f.CardZoom(tr.CardId);
            yield return Wait(1.0f);
            yield return Shot("bless_zoom");
            CloseModals();
            Debug.Log($"[Demo] 축복 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        // 화면 시각 0.1초마다 한 장 — 메모리에 모았다가 끝에 저장(찍는 동안 멈칫이 없게). 이름: 이름_NN_밀리초
        IEnumerator Frames(string name, float sec)
        {
            var shots = new List<(Texture2D tex, float at)>();
            float t0 = Time.unscaledTime, next = 0;
            while (Time.unscaledTime - t0 < sec)
            {
                yield return new WaitForEndOfFrame();
                float e = Time.unscaledTime - t0;
                if (e >= next) { shots.Add((ScreenCapture.CaptureScreenshotAsTexture(), e)); next += 0.1f; }
                yield return null;
            }
            for (int i = 0; i < shots.Count; i++)
            {
                File.WriteAllBytes(Path.Combine(dir, $"{name}_{i:D2}_{Mathf.RoundToInt(shots[i].at * 1000):D4}ms.png"), shots[i].tex.EncodeToPNG());
                Destroy(shots[i].tex);
            }
            Debug.Log($"[Capture] {name} {shots.Count}장");
        }
    }
}
