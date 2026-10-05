using System.Collections.Generic;
using Spine.Unity;
using TMPro;
using UnityEngine;

namespace Bolzena
{
    // Resources 에서 꺼내 붙들어 두는 곳 — 그림 · 소리 · 재질 · 글꼴 · 스파인
    public static class Res
    {
        static readonly Dictionary<string, Object> cache = new Dictionary<string, Object>();
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        static TMP_FontAsset font, body, symbols;

        public static T Load<T>(string path) where T : Object
        {
            string key = typeof(T).Name + ":" + path;
            if (cache.TryGetValue(key, out var o)) return o as T;
            var r = Resources.Load<T>(path);
            if (r == null) Debug.LogWarning("[Res] 없음: " + path);
            cache[key] = r;
            return r;
        }

        public static Sprite Sprite(string path) => Load<Sprite>(path);
        public static Sprite UI(string name) => Load<Sprite>("UI/" + name);
        public static Sprite FxTex(string name) => Load<Sprite>("Fx/Tex/" + name);
        public static AudioClip Clip(string path) => Load<AudioClip>(path);

        public static SkeletonDataAsset Spine(string folder)
        {
            string key = "spine:" + folder;
            if (cache.TryGetValue(key, out var o)) return o as SkeletonDataAsset;
            var all = Resources.LoadAll<SkeletonDataAsset>("Spine/" + folder);
            var a = all.Length > 0 ? all[0] : null;
            if (a == null) Debug.LogWarning("[Res] 스파인 없음: " + folder);
            cache[key] = a;
            return a;
        }

        // 스프라이트 재질 — 섞기(알파 · 더하기)와 HDR 세기(블룸이 먹게)별로 하나씩
        public static Material SpriteMat(bool additive = false, float boost = 1f)
        {
            string key = (additive ? "add" : "alpha") + boost.ToString("F2");
            if (mats.TryGetValue(key, out var m)) return m;
            m = new Material(Shader.Find("Bolzena/Sprite"));
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)(additive ? UnityEngine.Rendering.BlendMode.One : UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            m.SetFloat("_Boost", boost);
            m.name = "Sprite_" + key;
            mats[key] = m;
            return m;
        }

        // 특수 셰이더 재질은 매번 새로(속성을 따로 움직인다)
        public static Material NewMat(string shader) => new Material(Shader.Find(shader));

        public static TMP_FontAsset Font
        {
            get
            {
                if (font != null) return font;
                var ttf = Resources.Load<Font>("Fonts/Jua-Regular");
                font = TMP_FontAsset.CreateFontAsset(ttf, 72, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048,
                    AtlasPopulationMode.Dynamic, true);
                font.name = "Jua SDF (runtime)";
                var sh = Shader.Find("TextMeshPro/Distance Field");
                if (sh != null) font.material.shader = sh;
                // 보조 — Jua 에 없는 기호(「」 · × → — ◆ …)는 Noto Sans KR 기호만 자른 것에서(OFL)
                var fb = Symbols;
                if (fb != null) font.fallbackFontAssetTable = new List<TMP_FontAsset> { fb };
                return font;
            }
        }

        static TMP_FontAsset Symbols
        {
            get
            {
                if (symbols != null) return symbols;
                var sym = Resources.Load<Font>("Fonts/NotoSansKR-Symbols");
                if (sym == null) return null;
                symbols = TMP_FontAsset.CreateFontAsset(sym, 72, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                symbols.name = "Noto Symbols SDF (runtime)";
                var sh = Shader.Find("TextMeshPro/Distance Field");
                if (sh != null) symbols.material.shader = sh;
                return symbols;
            }
        }

        /// <summary>본문 글꼴 — Noto Sans KR SemiBold(판 화면 톤.md 의 본문). 판 화면 패키지의 글꼴 파일을 함께 쓴다. 없으면 Jua.</summary>
        public static TMP_FontAsset Body
        {
            get
            {
                if (body != null) return body;
                var ttf = Resources.Load<Font>("RunUI/Fonts/NotoSansKR-SemiBold-sub");
                if (ttf == null) return body = Font;
                body = TMP_FontAsset.CreateFontAsset(ttf, 64, 8, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                body.name = "Noto Sans KR SemiBold SDF (battle)";
                var sh = Shader.Find("TextMeshPro/Distance Field");
                if (sh != null) body.material.shader = sh;
                var fb = Symbols;
                body.fallbackFontAssetTable = fb != null ? new List<TMP_FontAsset> { fb, Font } : new List<TMP_FontAsset> { Font };
                return body;
            }
        }
    }
}
