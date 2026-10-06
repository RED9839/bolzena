using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Bolzena.RunUI
{
    /// <summary>
    /// 글자 입력 칸(TMP_InputField)에 한글이 입력되게 하는 공용 부품 — 칸을 만든 뒤 WebInput.Attach(칸) 한 줄.
    ///   PC(에디터 포함): ImeInput 을 걸어 Input System 환경에서도 IME 가 켜지고 조합 중 글자가 보이게 한다.
    ///   웹(WebGL): 유니티는 브라우저 IME 를 못 받는다 → 칸 위에 투명한 HTML input 을 겹쳐(Plugins/WebGL/BzWebInput.jslib) 브라우저가 한글 조합 · 초성 · 붙여넣기 · 폰 화면 자판을 처리하게 하고,
    ///   값이 바뀔 때마다(조합 중 포함) 칸의 text 로 넘긴다. 칸의 보이는 모습은 TMP 가 그대로 그린다.
    /// 웹이 아니면 웹 부분은 컴파일되지 않는다(UNITY_WEBGL 이고 에디터가 아닐 때만).
    /// </summary>
    public static class WebInput
    {
        /// <summary>칸에 한글 입력을 붙인다 — 여러 번 불러도 한 번만 붙는다.</summary>
        public static void Attach(TMP_InputField field)
        {
            if (field == null) return;
            ImeInput.Ensure();
#if UNITY_WEBGL && !UNITY_EDITOR
            field.shouldHideSoftKeyboard = true;   // 유니티의 TouchScreenKeyboard 를 열지 않는다(HTML input 이 자판을 띄운다)
            if (field.GetComponent<Bridge>() == null) field.gameObject.AddComponent<Bridge>();
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        static class Native
        {
            [DllImport("__Internal")] public static extern void BzWI_Reg(int id, float x, float y, float w, float h, float fs);
            [DllImport("__Internal")] public static extern void BzWI_Unreg(int id);
            [DllImport("__Internal")] public static extern void BzWI_Show(int id, string text, int maxLen, int caret);
            [DllImport("__Internal")] public static extern void BzWI_SetValue(int id, string text);
            [DllImport("__Internal")] public static extern void BzWI_Hide();
            [DllImport("__Internal")] public static extern int BzWI_IsOpen();
        }

        /// <summary>자바스크립트가 SendMessage 로 부르는 받는 쪽(오브젝트 이름 「BzWebInput」).</summary>
        sealed class Host : MonoBehaviour
        {
            static Host me;
            public static readonly Dictionary<int, Bridge> All = new Dictionary<int, Bridge>();

            public static void Ensure()
            {
                if (me != null) return;
                var go = new GameObject("BzWebInput");
                DontDestroyOnLoad(go);
                me = go.AddComponent<Host>();
            }

            // id, 커서, 값 — 세 조각을 \u0001 로 나눈 글
            public void OnValue(string msg)
            {
                var p = msg.Split('\u0001');
                if (p.Length < 3 || !int.TryParse(p[0], out var id) || !All.TryGetValue(id, out var b)) return;
                int.TryParse(p[1], out var caret);
                b.FromBrowser(p[2], caret);
            }

            // 브라우저 쪽에서 입력이 끝났다(엔터 · 바깥 누름 · 초점 잃음)
            public void OnEnd(string msg)
            {
                if (int.TryParse(msg, out var id) && All.TryGetValue(id, out var b)) b.EndFromBrowser();
            }

            // 탭 순간 자바스크립트가 먼저 초점을 걸었다 — 실제로 이 칸이 선택됐는지 두 프레임 뒤 확인하고 아니면(위에 다른 창이 눌렸다 등) 닫는다
            public void OnArmed(string msg)
            {
                if (int.TryParse(msg, out var id) && All.TryGetValue(id, out var b)) StartCoroutine(Verify(b));
            }

            IEnumerator Verify(Bridge b)
            {
                yield return null;
                yield return null;
                if (b != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != b.gameObject) Native.BzWI_Hide();
            }
        }

        /// <summary>칸마다 붙는 부품 — 칸의 화면 자리를 알리고, 선택되면 HTML input 을 연다.</summary>
        sealed class Bridge : MonoBehaviour, ISelectHandler, IDeselectHandler
        {
            static int counter;
            TMP_InputField field;
            int id;
            Vector4 sent = new Vector4(-1, -1, -1, -1);
            float sentFs;
            string fromBrowser;
            bool open;

            void Awake()
            {
                field = GetComponent<TMP_InputField>();
                id = ++counter;
                Host.Ensure();
            }

            void OnEnable()
            {
                Host.All[id] = this;
                field.onValueChanged.AddListener(OnChanged);
            }

            void OnDisable()
            {
                field.onValueChanged.RemoveListener(OnChanged);
                Host.All.Remove(id);
                Native.BzWI_Unreg(id);
                sent = new Vector4(-1, -1, -1, -1);
                if (open) EndLocal();
            }

            void Update()
            {
                var vp = field.textViewport != null ? field.textViewport : (RectTransform)field.transform;
                var cv = vp.GetComponentInParent<Canvas>();
                if (cv == null || Screen.width < 1 || Screen.height < 1) return;
                var cam = cv.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : cv.rootCanvas.worldCamera;
                var c = new Vector3[4];
                vp.GetWorldCorners(c);
                var a = RectTransformUtility.WorldToScreenPoint(cam, c[0]);   // 왼쪽 아래
                var b = RectTransformUtility.WorldToScreenPoint(cam, c[2]);   // 오른쪽 위
                float sw = Screen.width, sh = Screen.height;
                var r = new Vector4(a.x / sw, 1f - b.y / sh, (b.x - a.x) / sw, (b.y - a.y) / sh);
                float fs = (field.textComponent != null ? field.textComponent.fontSize : 24f) * cv.rootCanvas.scaleFactor / sh;
                if ((r - sent).sqrMagnitude < 1e-10f && Mathf.Abs(fs - sentFs) < 1e-6f) return;
                sent = r; sentFs = fs;
                Native.BzWI_Reg(id, r.x, r.y, r.z, r.w, fs);
            }

            public void OnSelect(BaseEventData e)
            {
                if (field.readOnly) return;
                open = true;
                WebGLInput.captureAllKeyboardInput = false;   // 켜 두면 유니티가 키를 가로채 HTML input 에 글이 안 써진다
                fromBrowser = null;
                Native.BzWI_Show(id, field.text ?? "", field.characterLimit, field.stringPosition);
            }

            public void OnDeselect(BaseEventData e)
            {
                if (!open) return;
                Native.BzWI_Hide();
                EndLocal();
            }

            void EndLocal()
            {
                open = false;
                WebGLInput.captureAllKeyboardInput = true;
            }

            // 코드가 칸 글을 바꿨다(지우기 단추 등) — 브라우저 쪽 값도 맞춘다
            void OnChanged(string v)
            {
                if (open && v != fromBrowser) Native.BzWI_SetValue(id, v ?? "");
            }

            public void FromBrowser(string v, int caret)
            {
                fromBrowser = v;
                if (field.text != v) field.text = v;
                field.stringPosition = Mathf.Clamp(caret, 0, v.Length);
            }

            public void EndFromBrowser()
            {
                if (!open) return;
                EndLocal();
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }
#endif
    }
}
