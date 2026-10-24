using Bolzena.Battle;
using Bolzena.Core;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena.UI
{
    // 프론티어 전투 머리(위 가운데) — 시즌 · 남은 턴(Frontier.TurnsLeft) · 누적 점수(Frontier.Score = 피해 + 방어) · 아래 줄 「피해 n + 방어 m = 점수 · 성격 상성 없음」(2026-10-24).
    //   판 전투에는 없다(BattleDirector.Build 가 BattleBridge.Fight.Frontier 일 때만 세운다). 값은 매 프레임 코어 전투에서 읽는다.
    public class FrontierHud : MonoBehaviour
    {
        const int O = 566;
        IBattle battle;
        FrontierSeason season;
        TextMeshPro turns, score, split;
        Transform node;
        int lastScore = -1, lastLeft = -1;
        float pop;

        public static FrontierHud Create(Transform parent, IBattle battle, FrontierSeason season)
        {
            var root = Make.Node("FrontierHud", parent);
            var h = root.gameObject.AddComponent<FrontierHud>();
            h.battle = battle; h.season = season;
            h.Build();
            return h;
        }

        void Build()
        {
            node = Make.Node("in", transform);
            Make.Sliced("bg", node, Res.UI("pill_dark_9s"), Vector3.zero, new Vector2(4.9f, 0.62f), O, new Color(1, 1, 1, 0.92f));
            var tag = Make.Text("tag", node, season.Id == Frontier.PRESEASON.Id ? "프리시즌" : season.Id, new Vector3(-1.86f, 0.0f, 0), 0.17f, O + 2, new Color(0.95f, 0.81f, 0.48f));
            Make.Outline(tag, 0.2f, Tone.Outline);
            turns = Make.Text("turns", node, "", new Vector3(-0.62f, 0.0f, 0), 0.2f, O + 2, Color.white);
            Make.Outline(turns, 0.22f, Tone.Outline);
            score = Make.Text("score", node, "", new Vector3(1.12f, 0.0f, 0), 0.24f, O + 2, new Color(1f, 0.9f, 0.6f));
            Make.Outline(score, 0.22f, Tone.Outline);
            split = Make.Text("nature", node, "성격 상성 없음", new Vector3(0, -0.52f, 0), 0.16f, O + 2, new Color(0.78f, 0.84f, 0.95f));
            Make.Outline(split, 0.4f, Tone.Outline);
            Place();
            Paint();
        }

        void Place()
        {
            float k = Tone.K;
            transform.localPosition = new Vector3(0, Tone.HalfH - 0.5f * k, 0);
            transform.localScale = new Vector3(k, k, 1);
        }

        void Paint()
        {
            var b = (battle as CoreBattle)?.Fight;
            if (b == null) return;
            int left = Frontier.TurnsLeft(b, season), sc = Frontier.Score(b);
            if (left != lastLeft) { lastLeft = left; turns.text = $"남은 턴 <color=#F2CF7A>{left}</color> / {season.Turns}"; }
            if (sc != lastScore)
            {
                if (lastScore >= 0 && sc > lastScore) pop = 1;
                lastScore = sc; score.text = $"점수 {sc:N0}";
                split.text = $"피해 {Frontier.Dealt(b):N0} + 방어 <color=#F2CF7A>{Frontier.Ward(b):N0}</color> = {sc:N0}   ·   성격 상성 없음";
            }
        }

        void LateUpdate()
        {
            Place();
            Paint();
            if (pop > 0) { pop = Mathf.Max(0, pop - Time.unscaledDeltaTime * 4); score.transform.localScale = Vector3.one * (1 + 0.18f * pop); }
        }
    }
}
