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
        public override uint GetVersion() => 1;   // 규칙이 바뀌면 올린다

        void OnPreprocessAudio()
        {
            var p = assetPath.Replace("\\", "/");
            if (!p.Contains("/Resources/Voice/")) return;
            var ai = (AudioImporter)assetImporter;
            var w = ai.defaultSampleSettings;
            w.loadType = AudioClipLoadType.CompressedInMemory;
            ai.SetOverrideSampleSettings("WebGL", w);
        }
    }
}
