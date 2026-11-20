using Bolzena.View;
using UnityEngine;

namespace Bolzena.UI
{
    /// <summary>
    /// 단축키 배지 — 단추 옆 작은 키 글자(2026-11-01). 이미 있는 키만 붙인다:
    ///   전투 — 턴 종료 E · 뽑을 더미 A · 버린 더미 S · 전투 기록 D · 자동 전투 Q · 배속 Tab · 메뉴 Esc · 고학년 Z X C(BattleDirector.PollInput)
    ///   턴 종료 확인 창 — 턴 종료 Enter · 취소 Esc / 적 정보 창 — 이전 · 다음 ← →
    /// 설정 「단축키 표시」(Settings.ShowKeys, 기본 켬)를 끄거나 터치 기기(손가락으로 누름 · 폰 · 모바일)면 숨긴다 — 매 프레임 본다.
    /// </summary>
    public class KeyBadge : MonoBehaviour
    {
        public static bool Show => Bolzena.RunUI.Settings.ShowKeys && !PointerInput.Touch && !Tone.Compact && !Application.isMobilePlatform;

        GameObject body;
        bool shown = true;

        /// <summary>at 가운데에 키 배지 — h 는 높이(월드), 글이 길면(Tab · Esc) 옆으로 늘어난다.</summary>
        public static KeyBadge Add(Transform parent, Vector3 at, string key, int order, float h = 0.22f)
        {
            var t = Make.Node("key_" + key, parent, at);
            var k = t.gameObject.AddComponent<KeyBadge>();
            k.body = Make.Node("b", t).gameObject;
            var txt = Make.Text("k", k.body.transform, key, new Vector3(0, -0.006f, 0), h * 0.6f, order + 2, new Color(0.95f, 0.93f, 0.88f));
            txt.ForceMeshUpdate();
            float w = Mathf.Max(h, txt.preferredWidth + h * 0.5f);
            Make.Sliced("rim", k.body.transform, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(w + 0.035f, h + 0.035f), order, new Color(0.95f, 0.93f, 0.88f, 0.6f));
            Make.Sliced("bg", k.body.transform, Res.UI("bar_fill_9s"), Vector3.zero, new Vector2(w, h), order + 1, new Color(0.04f, 0.05f, 0.1f, 0.92f));
            k.Apply();
            return k;
        }

        void Update() => Apply();

        void Apply()
        {
            bool v = Show;
            if (v == shown) return;
            shown = v;
            body.SetActive(v);
        }
    }
}
