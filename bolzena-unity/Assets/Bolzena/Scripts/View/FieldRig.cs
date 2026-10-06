using UnityEngine;

namespace Bolzena.View
{
    // 싸움터 카메라 — 카메라는 그대로 두고 싸움터 마디(배경 · 유닛 · 이펙트)를 움직인다. 손패 · 화면 UI 는 흔들리지 않는다.
    //   흔들림(trauma²) · 줌 펀치(맞은 자리를 중심으로 확 당겼다 풀기) · 기울기 · 연출용 줌/이동(고학년 카메라)
    // 멈칫(히트스톱) 동안에도 흔들려야 하니 시간은 Clock.Now(멈춤과 무관)로 잰다.
    public class FieldRig : MonoBehaviour
    {
        public static FieldRig I;
        float trauma, roll;
        float punch, punchVel;
        Vector2 punchAt;
        public float Zoom = 1f;              // 연출용 줌(고학년)
        public Vector2 Center;               // 연출용 카메라 중심(싸움터 좌표)
        float seed;

        void Awake()
        {
            I = this;
            seed = Random.value * 100f;
        }

        public static void Shake(float amount, float rollDeg = 0f)
        {
            if (I == null) return;
            if (Bolzena.RunUI.Settings.NoShake) return;                       // 설정 「화면 흔들림 끄기」
            if (Bolzena.RunUI.Settings.ReduceMotion) { amount *= 0.35f; rollDeg *= 0.35f; }   // 움직임 줄이기 — 약하게
            I.trauma = Mathf.Min(1.2f, I.trauma + amount);
            I.roll += rollDeg * (Random.value < 0.5f ? -1 : 1);
        }

        // 줌 펀치 — at(싸움터 좌표)을 중심으로 k 만큼(0.05 = 5%) 당긴다
        public static void Punch(Vector2 at, float k)
        {
            if (I == null) return;
            I.punchAt = at;
            I.punch = Mathf.Max(I.punch, k);
            I.punchVel = 0;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            float t = Clock.Now * 28f;
            float s = trauma * trauma;
            Vector2 shake = new Vector2(Mathf.PerlinNoise(seed, t) - 0.5f, Mathf.PerlinNoise(seed + 7.3f, t) - 0.5f) * 2f * s * 0.42f;
            float rot = roll * s + (Mathf.PerlinNoise(seed + 3.1f, t) - 0.5f) * 2f * s * 1.2f;
            trauma = Mathf.Max(0, trauma - dt * 1.9f);
            roll = Mathf.Lerp(roll, 0, 1 - Mathf.Exp(-dt * 6f));

            // 펀치는 용수철처럼 0 으로 돌아온다(살짝 넘친다). 1/120초 조각으로 나눠 센다 — 한 프레임이 길면(≥ 67ms: 3440×1440 캡처 · 느린 PC)
            //   한 번에 적분하면 용수철이 발산해 싸움터가 뒤집히고 끝없이 커졌다(2026-10-07)
            for (float left = Mathf.Min(dt, 0.25f); left > 1e-5f; left -= 1f / 120f)
            {
                float h = Mathf.Min(left, 1f / 120f);
                float acc = -punch * 900f - punchVel * 38f;
                punchVel += acc * h;
                punch += punchVel * h;
            }

            float z = Zoom * (1 + punch);
            // 펀치 중심이 화면에서 그 자리에 머물도록 중심을 보정한다
            Vector2 c = Center;
            if (Mathf.Abs(punch) > 1e-4f) c = punchAt - (punchAt - Center) * (Zoom / z);
            transform.localScale = new Vector3(z, z, 1);
            transform.localPosition = new Vector3(-c.x * z + shake.x, -c.y * z + shake.y, 0);
            transform.localRotation = Quaternion.Euler(0, 0, rot);
        }

        // 싸움터 좌표 → 월드(화면) 좌표
        public Vector3 ToWorld(Vector3 field) => transform.TransformPoint(field);
    }
}
