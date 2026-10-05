using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Bolzena.View
{
    // 후처리 — 블룸(늘) · 색수차 · 렌즈 왜곡 · 채도/노출 펀치(타격 · 격파 · 고학년)
    public class PostFx : MonoBehaviour
    {
        public static PostFx I;
        Bloom bloom;
        ChromaticAberration chroma;
        LensDistortion lens;
        ColorAdjustments color;
        Vignette vignette;
        float chromaK, lensK, satK, expK, bloomK;
        const float BloomBase = 0.6f;
        const float BloomKickMax = 0.35f;     // 펀치로 더하는 블룸의 끝 — 넘으면 화면이 하얗게 날아가 대상이 안 보인다

        public static PostFx Create()
        {
            var go = new GameObject("PostFx");
            var v = go.AddComponent<Volume>();
            v.isGlobal = true;
            v.priority = 10;
            var p = ScriptableObject.CreateInstance<VolumeProfile>();
            v.sharedProfile = p;
            var me = go.AddComponent<PostFx>();
            me.bloom = p.Add<Bloom>(true);
            me.bloom.threshold.Override(1.15f);
            me.bloom.intensity.Override(BloomBase);
            me.bloom.scatter.Override(0.55f);
            me.bloom.highQualityFiltering.Override(true);
            var tm = p.Add<Tonemapping>(true);
            tm.mode.Override(TonemappingMode.None);
            me.vignette = p.Add<Vignette>(true);
            me.vignette.intensity.Override(0.24f);
            me.vignette.smoothness.Override(0.45f);
            me.chroma = p.Add<ChromaticAberration>(true);
            me.chroma.intensity.Override(0f);
            me.lens = p.Add<LensDistortion>(true);
            me.lens.intensity.Override(0f);
            me.lens.scale.Override(1f);
            me.color = p.Add<ColorAdjustments>(true);
            me.color.saturation.Override(0f);
            me.color.postExposure.Override(0f);
            I = me;
            return me;
        }

        // 한 번 확 — 시간이 지나면 저절로 0 으로
        public static void Kick(float chroma = 0f, float lens = 0f, float sat = 0f, float exposure = 0f, float bloom = 0f)
        {
            if (I == null) return;
            I.chromaK = Mathf.Max(I.chromaK, chroma);
            if (Mathf.Abs(lens) > Mathf.Abs(I.lensK)) I.lensK = lens;
            if (Mathf.Abs(sat) > Mathf.Abs(I.satK)) I.satK = sat;
            if (Mathf.Abs(exposure) > Mathf.Abs(I.expK)) I.expK = exposure;
            I.bloomK = Mathf.Min(BloomKickMax, Mathf.Max(I.bloomK, bloom));
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float k = 1 - Mathf.Exp(-dt * 7f);
            chromaK = Mathf.Lerp(chromaK, 0, k);
            lensK = Mathf.Lerp(lensK, 0, k * 1.3f);
            satK = Mathf.Lerp(satK, 0, k * 0.8f);
            expK = Mathf.Lerp(expK, 0, k * 1.5f);
            bloomK = Mathf.Lerp(bloomK, 0, k);
            chroma.intensity.value = Mathf.Clamp01(chromaK);
            lens.intensity.value = Mathf.Clamp(lensK, -1, 1);
            color.saturation.value = Mathf.Clamp(satK, -100, 100);
            color.postExposure.value = expK;
            bloom.intensity.value = BloomBase + bloomK;
        }
    }
}
