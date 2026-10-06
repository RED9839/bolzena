using UnityEngine;
using UnityEngine.InputSystem;

namespace Bolzena.UI
{
    // 손가락(마우스 · 터치) — 화면 좌표를 월드로. 자동 데모는 Simulated 를 켜고 값을 직접 넣는다.
    //   Down/Up/Held — 왼쪽 단추 또는 첫 손가락, RightDown — 오른쪽 단추(취소 · 정보), Touch — 마지막 입력이 터치였나(올려 보기가 없다)
    //   LongPress — 누른 채 0.45초 움직이지 않으면 한 번(터치의 정보 보기)
    public static class PointerInput
    {
        public static bool Simulated;
        public static Vector2 SimPos;
        public static bool SimHeld, SimRight, SimTouch;   // SimTouch — 자동 데모가 터치처럼(올려 두기 없음)
        static bool prevHeld, prevRight;
        static int frame = -1;
        static bool down, up, held, rightDown, longPress, longFired;
        static Vector2 pos, downPos;
        static float downAt;
        public static bool Touch { get; private set; }
        public static bool Moved { get; private set; }       // 누른 뒤 멀리 움직였나(끌기)

        static void Tick()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            bool h, r = false;
            var cam = Camera.main;
            if (Simulated)
            {
                pos = SimPos;
                h = SimHeld;
                r = SimRight;
                Touch = SimTouch;
            }
            else
            {
                h = false;
                var ts = Touchscreen.current;
                if (ts != null && ts.primaryTouch.press.isPressed && cam != null)
                {
                    var sp = ts.primaryTouch.position.ReadValue();
                    pos = cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10));
                    h = true;
                    Touch = true;
                }
                else
                {
                    var m = Mouse.current;
                    if (m != null && cam != null)
                    {
                        var sp = m.position.ReadValue();
                        var wp = (Vector2)cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10));
                        if (m.leftButton.isPressed || m.rightButton.isPressed || (wp - pos).sqrMagnitude > 0.0004f) Touch = false;
                        if (!Touch || prevHeld) pos = wp;
                        h = m.leftButton.isPressed;
                        r = m.rightButton.isPressed;
                    }
                }
            }
            down = h && !prevHeld;
            up = !h && prevHeld;
            rightDown = r && !prevRight;
            held = h;
            if (down) { downPos = pos; downAt = Time.unscaledTime; Moved = false; longFired = false; }
            if (held && (pos - downPos).magnitude > MoveSlop(cam)) Moved = true;
            longPress = false;
            if (held && !Moved && !longFired && Time.unscaledTime - downAt > 0.45f) { longPress = true; longFired = true; }
            prevHeld = h;
            prevRight = r;
        }

        // 끌기로 치는 거리(월드) — 마우스 0.18. 터치는 손가락 떨림이 있어 화면 2.5mm(dpi 를 모르면 짧은 변의 2.2%) 와 0.18 중 큰 것.
        //   폰은 화면 1 단위가 10여 px 이라 0.18 이면 가만히 누른 손가락도 「끌기」가 되어 길게 누르기 확대가 열리지 않았다
        static float MoveSlop(Camera cam)
        {
            if (!Touch || cam == null || Screen.height <= 0) return 0.18f;
            float px = Screen.dpi > 0 ? Screen.dpi * 0.1f : Mathf.Min(Screen.width, Screen.height) * 0.022f;
            return Mathf.Max(0.18f, px * cam.orthographicSize * 2f / Screen.height);
        }

        public static Vector2 Pos { get { Tick(); return pos; } }
        public static bool Down { get { Tick(); return down; } }
        public static bool Up { get { Tick(); return up; } }
        public static bool Held { get { Tick(); return held; } }
        public static bool RightDown { get { Tick(); return rightDown; } }
        public static bool LongPress { get { Tick(); return longPress; } }
        public static bool LongFired { get { Tick(); return longFired; } }
        // 짧게 눌렀다 뗌(끌지 않고, 길게 누르기도 아님) — 「탭」
        public static bool Tap { get { Tick(); return up && !Moved && !longFired; } }

        public static bool Key(Key k) => !Simulated && Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame;
    }
}
