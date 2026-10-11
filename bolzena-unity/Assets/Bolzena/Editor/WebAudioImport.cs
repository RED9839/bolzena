using UnityEditor;
using UnityEngine;

namespace Bolzena.EditorTools
{
    // 웹(WebGL) 목소리 — BolzenaImport 가 정한 기본(DecompressOnLoad) 위에 WebGL 덮어쓰기만 얹는다(PC · 폰은 그대로).
    //   DecompressOnLoad 는 목소리 1,379개를 받자마자 전부 풀어 메모리에 올리고, iOS 무음 모드에서는 소리가 안 난다(유니티 Web 오디오 문서).
    //   CompressedInMemory 는 압축된 채로 두고 재생할 때 푼다 — 조금 늦게 나도 되는 목소리만(FxImport 의 voice 와 같은 규칙).
    //   효과음(Sfx/hero)은 고학년 타격 시점에 맞아야 해서 그대로 둔다.
    //   BolzenaImport 의 GetVersion 을 올리면 그림까지 전부 다시 가져오므로 소리만 다루는 클래스로 뗐다.
    public class WebAudioImport : AssetPostprocessor
    {
        public override int GetPostprocessOrder() => 1000;
        public override uint GetVersion() => 3;   // 규칙이 바뀌면 올린다(3 = 아래 품질 낮춤 — 2 는 덮어쓰기만 걸어 웹 결과가 그대로였다)

        // 2026-10-09(perf_20261009): 웹은 소리를 AAC(mp4)로 넣는데, 품질 0.6(BolzenaImport 기본)이 모노 166kbps 로 나와 목소리 · 사도 효과음이
        //   .data 의 1/4(받는 크기 약 93MB)이었다. 웹만 낮춘다(PC · 폰 앱은 그대로) — 목소리 품질 0.3 · 22.05kHz, 사도 효과음 품질 0.4 · 44.1kHz(타격 고음 그대로).
        const float VoiceQuality = 0.3f, HeroSfxQuality = 0.4f;

        void OnPreprocessAudio()
        {
            var p = WebImport.Original(assetPath.Replace("\\", "/"));   // 웹 빌드 동안 옮겨 둔 목소리(WebBundleBuild.StageVoice)도 원래 자리 규칙대로
            bool voice = p.Contains("/Resources/Voice/"), heroSfx = p.Contains("/Resources/Sfx/hero/");
            if (!voice && !heroSfx) return;
            var ai = (AudioImporter)assetImporter;
            var w = ai.defaultSampleSettings;
            if (voice) w.loadType = AudioClipLoadType.CompressedInMemory;   // 효과음은 타격 시점 때문에 DecompressOnLoad 그대로
            w.quality = voice ? VoiceQuality : HeroSfxQuality;
            // 웹 AAC 비트레이트는 품질만으로는 덜 준다(1.0 → 200kbps · 0.6 → 166kbps) — 목소리는 22.05kHz 로(말소리는 8kHz 아래가 거의 전부)
            if (voice) { w.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate; w.sampleRateOverride = 22050; }
            ai.SetOverrideSampleSettings("WebGL", w);
            // WebGL 덮어쓰기만으로는 웹 AAC 결과가 바뀌지 않았다(perf_20261009 1차 빌드: 166kbps 그대로) — 웹으로 가져올 때는 기본값 자체를 웹 값으로.
            //   가져오기 결과는 플랫폼마다 따로 쌓이므로 PC · 폰 앱 결과는 그대로다(.meta 에도 저장하지 않는다)
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL) ai.defaultSampleSettings = w;
        }
    }
}
