using System.Collections;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using TMPro;
using UnityEngine;

namespace Bolzena
{
    // 은총 — 빛나는 기본 카드를 내면 그 사도의 고유 카드 하나가 손에 들어온다(core ApplyEpiphany "hero").
    //   고르기가 없으니 신탁 창(EpiphanyWindow)을 열지 않는다(2026-10-07 제보: 은총이 「신탁!」 창으로 떠서 카드를 한 번 더 눌러야 들어왔다).
    //   얻은 카드를 화면 가운데 위에 크게(손패 길게 누르기 확대 크기) 0.9초 보여 준 뒤 그대로 손패 제자리로 날아 들어간다. 누르면 바로 넘어간다.
    //   손이 가득하면(core: 버림 더미로) 크게 보인 뒤 버림 더미 쪽으로 날아간다.
    public partial class BattleDirector
    {
        /// <summary>은총 연출 단계(grace_show · grace_fly · grace_done) — 점검 캡처가 본다.</summary>
        public static System.Action<string> OnGraceStage;
        string graceFrom;          // 은총 빛이 선 카드(낸 카드)의 이름 — 안내 글

        static void GraceStage(string s, string card)
        {
            Debug.Log($"[Grace] {s} 「{card}」 t={Time.unscaledTime:F2}");
            OnGraceStage?.Invoke(s);
        }

        IEnumerator GraceIn(BattleEvent e)
        {
            const int O = 800;
            bool toHand = e.Text == "grace";
            var info = e.Card;
            if (toHand) info.Cost = 0;   // 은총 카드는 그 턴 비용 0(core FreeTurn) — Draw 이벤트의 카드 정보는 손 자리 없이 만든 것이라 원래 비용이 실려 온다
            var parent = Hand.transform.parent;
            var root = Make.Node("Grace", parent);
            bool calm = Bolzena.RunUI.Settings.ReduceMotion || LowSpecFx.On;

            // 자리 — 손패 확대 자리(ZoomAt)에서, 위에 제목 · 안내 두 줄이 들어가게 조금 줄인다
            var zoom = Hand.ZoomAt(out float s);
            float bottom = zoom.y - CardView.H * s / 2;
            float top = Tone.HalfH - 1.75f * Tone.K;
            float s2 = Mathf.Max(s * 0.7f, Mathf.Min(s, (top - bottom) / CardView.H));
            var at = new Vector3(zoom.x, bottom + CardView.H * s2 / 2, 0);

            var dim = Make.Box("dim", root, Res.UI("white"), Vector3.zero, new Vector2(60, 24), O, new Color(0.03f, 0.02f, 0.06f, 0));
            string who = string.IsNullOrEmpty(info.HeroName) ? "사도" : info.HeroName;
            var title = Make.Text("title", root, "은총!", new Vector3(zoom.x, Tone.HalfH - 0.62f * Tone.K, 0), 0.7f, O + 40, new Color(1f, 0.95f, 0.75f));
            title.colorGradient = new VertexGradient(Color.white, Color.white, new Color(1f, 0.78f, 0.3f), new Color(1f, 0.78f, 0.3f));
            Make.Outline(title, 0.2f, new Color(0.35f, 0.15f, 0));
            string what = toHand ? $"{who}의 고유 카드를 손에 얻습니다 · 이번 턴 비용 0" : $"{who}의 고유 카드 — 손이 가득해 버림 더미로";
            string line = string.IsNullOrEmpty(graceFrom) ? what : $"<color=#ffd76a>「{graceFrom}」</color>의 은총 — {what}";
            var sub = Make.Text("sub", root, line, new Vector3(zoom.x, Tone.HalfH - 1.28f * Tone.K, 0), 0.22f, O + 40, new Color(1f, 0.92f, 0.8f));
            Make.Outline(sub, 0.25f, Color.black);
            sub.ForceMeshUpdate();
            // 안내 띠 — 뒤의 적 머리 위 묶음(예고 · 체력)과 겹쳐도 글이 읽히게(신탁 창의 띠와 같은 것)
            var subBg = Make.Box("subbg", root, Res.UI("band"), sub.transform.localPosition, new Vector2(sub.preferredWidth + 1.6f, 0.5f), O + 39, new Color(1, 1, 1, 0));
            title.alpha = 0; sub.alpha = 0;

            // 얻은 카드 — 빛 속에서 가운데 위로 크게
            var big = CardView.Create(parent, info);
            big.ShowPin = false;
            big.transform.localPosition = at;
            big.transform.localScale = Vector3.one * s2 * 0.55f;
            big.Follow = 16f;
            big.TargetPos = at;
            big.TargetRot = 0;
            big.TargetScale = s2;
            big.SetOrder(O + 20);
            big.SetFlash(calm ? 0.4f : 0.65f);
            Sfx.Play("epiphany", 0.5f, 1.2f);
            Vfx.Glow(at, 2.6f * s2, new Color(1f, 0.85f, 0.45f, calm ? 0.35f : 0.6f), 0.3f, calm ? 1.2f : 2f, null, O + 10, root);
            GraceStage("grace_show", info.Name);
            yield return Clock.Tween(0.25f, t =>
            {
                Make.Alpha(dim, 0.55f * t);
                title.alpha = t; sub.alpha = t; Make.Alpha(subBg, t);
                if (big) big.SetFlash((calm ? 0.4f : 0.65f) * (1 - t));
            }, true);

            // 보여 주기 — 누르면(마우스 · 터치 · 엔터 · 스페이스) 바로 넘어간다
            float waited = 0, show = calm ? 0.6f : 0.85f;
            while (waited < show)
            {
                if (PointerInput.Down || PointerInput.Key(UnityEngine.InputSystem.Key.Enter) || PointerInput.Key(UnityEngine.InputSystem.Key.Space)) break;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            // 손패로 — 손패 카드를 그 자리 · 크기에서 시작시켜 제자리로 날린다(Layout 이 TargetPos 를 준다)
            GraceStage(toHand ? "grace_fly_hand" : "grace_fly_discard", info.Name);
            Clock.Run(Clock.Tween(0.2f, t => { if (dim) Make.Alpha(dim, 0.55f * (1 - t)); if (subBg) Make.Alpha(subBg, 1 - t); if (title) title.alpha = 1 - t; if (sub) sub.alpha = 1 - t; }, true));
            if (toHand)
            {
                var hv = Hand.Add(info, false);
                hv.transform.localPosition = Hand.transform.InverseTransformPoint(parent.TransformPoint(big.transform.localPosition));
                hv.transform.localScale = big.transform.localScale;
                hv.Follow = 12f;
                Hand.Layout();
                Destroy(big.gameObject);
                Sfx.Play("card_draw", 0.4f);
                yield return Clock.WaitU(0.3f);
            }
            else
            {
                big.Follow = 10f;
                big.TargetPos = parent.InverseTransformPoint(Hand.transform.TransformPoint(Hand.DiscardPos));
                big.TargetScale = 0.25f;
                Clock.Run(Clock.Tween(0.3f, t => { if (big) big.SetAlpha(1 - t * t); }, true));
                yield return Clock.WaitU(0.32f);
                if (big) Destroy(big.gameObject);
            }
            Destroy(root.gameObject);
            graceFrom = null;
            RefreshHud();
            GraceStage("grace_done", info.Name);
        }
    }
}
