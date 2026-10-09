using System;
using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Bolzena.RunUI
{
    /// <summary>
    /// 클립보드 — PC 는 GUIUtility.systemCopyBuffer, 웹(WebGL)은 브라우저 클립보드(Plugins/WebGL/BzClipboard.jslib).
    /// 웹 복사는 비동기라 결과를 콜백으로 준다: 「copied」 · 「retry」(다음 터치 · 클릭 때 한 번 더 시도 — 그때 결과가 다시 온다) · 「failed」.
    /// 웹 붙여넣기는 브라우저가 막을 수 있다(빈 글이 오면 화면의 입력 칸에 붙여 넣게 한다).
    /// </summary>
    public static class WebClipboard
    {
        static Action<string> onCopy;
        static Action<string> onPaste;

        public static void Copy(string text, Action<string> done)
        {
            text ??= "";
#if UNITY_WEBGL && !UNITY_EDITOR
            Host.Ensure();
            onCopy = done;
            BzClip_Copy(text);
#else
            GUIUtility.systemCopyBuffer = text;
            done?.Invoke(GUIUtility.systemCopyBuffer == text && text.Length > 0 ? "copied" : "failed");
#endif
        }

        /// <summary>클립보드 글을 읽는다 — 못 읽으면 빈 글.</summary>
        public static void Paste(Action<string> done)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            Host.Ensure();
            onPaste = done;
            BzClip_Paste();
#else
            done?.Invoke(GUIUtility.systemCopyBuffer ?? "");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BzClip_Copy(string text);
        [DllImport("__Internal")] static extern void BzClip_Paste();

        /// <summary>자바스크립트가 SendMessage 로 부르는 받는 쪽(오브젝트 이름 「BzClipboard」).</summary>
        sealed class Host : MonoBehaviour
        {
            static Host me;
            public static void Ensure()
            {
                if (me != null) return;
                var go = new GameObject("BzClipboard");
                DontDestroyOnLoad(go);
                me = go.AddComponent<Host>();
            }
            public void OnCopied(string r) => onCopy?.Invoke(r == "1" ? "copied" : r == "retry" ? "retry" : "failed");
            public void OnPasted(string t) { var f = onPaste; onPaste = null; f?.Invoke(t ?? ""); }
        }
#endif
    }
}
