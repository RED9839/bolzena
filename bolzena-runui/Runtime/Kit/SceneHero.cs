using Spine.Unity;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 장면용 사도 하나 — 전투 화면과 같은 SD 전투 스파인(SpineUi.Battle: 같은 배율 · 바닥선 · 쉬는 동작 Idle)을 발이 spot 의 (0.5, 0) 에 닿게 세운다.
    /// 전투 스파인이 없는 시험 프로젝트는 웹 렌더(정지 스탠딩), 그것도 없으면 얼굴. 발밑 그림자를 깐다.
    /// bodyPx — 전투와 맞추려면 Stage.Size.y × 0.24(이벤트 장면과 같은 값). faceRight = 오른쪽을 본다(왼쪽을 보게 하려면 false — 좌우 뒤집기).
    /// 휴식(캠프) · 이벤트 장면이 같이 쓴다(2026-10-06 사용자: 「휴식 미니미를 SD 로」).
    /// </summary>
    public static class SceneHero
    {
        /// <summary>표식 — 전투와 같은 고정 배율로 세운 SD. SpineUi.ClampInto 가 이것은 메시 경계로 줄이지 않는다(투명 슬롯 · 펫 · 무기 이펙트가 경계를 부풀린다).</summary>
        public sealed class FixedScale : MonoBehaviour { }

        public static SkeletonGraphic Make(RectTransform spot, HeroInfo h, float bodyPx, bool faceRight = true, float phase = 0, bool shadow = true)
        {
            if (spot == null || h == null) return null;
            var g = SpineUi.Battle(spot, h, bodyPx, faceRight);
            if (g != null) { g.AnimationState.Update(phase); g.gameObject.AddComponent<FixedScale>(); }
            else
            {
                var full = CardArt.Standing(h.art);
                if (full != null)
                {
                    var im = Ui.Img(spot, full, Color.white, "still"); im.preserveAspect = true;
                    im.rectTransform.At(0.5f, 0, 0, 0, bodyPx * 0.95f, bodyPx * 1.15f);
                    if (!faceRight) im.rectTransform.localScale = new Vector3(-1, 1, 1);
                }
                else { var fc = W.Face(spot, h, bodyPx * 0.5f); fc.At(0.5f, 0, 0, 0, bodyPx * 0.5f, bodyPx * 0.5f); }
            }
            if (shadow)
            {
                var shd = Ui.Img(spot, Theme.S("soft"), Color.black.A(0.45f), "shadow");
                shd.rectTransform.At(0.5f, 0.5f, 0, 0, bodyPx * 0.7f, bodyPx * 0.14f);
                shd.transform.SetAsFirstSibling();
            }
            return g;
        }
    }
}
