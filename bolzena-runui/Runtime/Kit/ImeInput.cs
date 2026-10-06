using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Bolzena.RunUI
{
    /// <summary>
    /// PC 한글(IME) 입력 — 이 프로젝트는 입력을 Input System 만 쓴다(activeInputHandler = 1).
    /// TMP_InputField 는 IME 를 켜고(imeCompositionMode) 조합 중 글자를 읽을 때(compositionString) EventSystem 입력 모듈의 BaseInput 을 거치는데,
    /// Input System UI 모듈의 기본 BaseInput 은 옛 Input 클래스를 부르거나 빈 글을 돌려줘서 IME 가 꺼진 채였다 → 한/영 키로 한글로 바꿔도 글자가 안 들어갔다.
    /// 이 BaseInput 이 그 셋을 Input System 의 Keyboard(SetIMEEnabled · onIMECompositionChange · SetIMECursorPosition)로 이어 준다.
    /// 쓰기: InputModule.inputOverride 로 한 번 건다 — Ensure().
    /// </summary>
    public sealed class ImeInput : BaseInput
    {
        string composition = "";
        IMECompositionMode mode = IMECompositionMode.Auto;
        Vector2 cursor;
        Keyboard hooked;

        /// <summary>지금 EventSystem 의 입력 모듈에 IME 입력을 건다(이미 걸려 있으면 아무 일 없다).</summary>
        public static void Ensure()
        {
            var m = Object.FindAnyObjectByType<BaseInputModule>();
            if (m == null || m.inputOverride != null) return;
            m.inputOverride = m.gameObject.AddComponent<ImeInput>();
        }

        protected override void OnEnable() { base.OnEnable(); Hook(); }

        protected override void OnDisable()
        {
            if (hooked != null) hooked.onIMECompositionChange -= OnComposition;
            hooked = null;
            composition = "";
            base.OnDisable();
        }

        void Update() { if (hooked != Keyboard.current) Hook(); }   // 키보드가 나중에 꽂혀도 이어지게

        void Hook()
        {
            var k = Keyboard.current;
            if (hooked != null) hooked.onIMECompositionChange -= OnComposition;
            hooked = k;
            if (k != null) { k.onIMECompositionChange += OnComposition; ApplyMode(); }
        }

        void OnComposition(IMECompositionString s) { composition = s.ToString() ?? ""; }

        void ApplyMode()
        {
#if !(UNITY_WEBGL && !UNITY_EDITOR)
            var k = Keyboard.current;
            if (k != null) k.SetIMEEnabled(mode == IMECompositionMode.On);
#endif
        }

        public override string compositionString => composition;

        public override IMECompositionMode imeCompositionMode
        {
            get => mode;
            set { mode = value; if (mode != IMECompositionMode.On) composition = ""; ApplyMode(); }
        }

        public override Vector2 compositionCursorPos
        {
            get => cursor;
            set
            {
                cursor = value;
#if !(UNITY_WEBGL && !UNITY_EDITOR)
                var k = Keyboard.current;
                if (k != null) k.SetIMECursorPosition(value);
#endif
            }
        }
    }
}
