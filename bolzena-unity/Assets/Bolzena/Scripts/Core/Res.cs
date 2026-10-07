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

        // 무거운 것(사도 · 적 · 스탠딩 스파인, 사도 · 적 제 소리, 사도 그림)은 최근에 쓴 것만 붙든다 — 다 붙들면 싸움마다 새 적 · 새 사도가
        //   쌓여(2026-10-06 고학년 점검 135판: 객체 1,682 → 12,875) 장면을 바꿀 때마다 정리(UnloadUnusedAssets)가 길어졌다.
        //   밀려난 것은 사전에서만 뺀다 — 아직 화면에 있으면 그대로 살고, 아무도 안 쓰면 다음 장면 정리 때 풀린다. 다시 부르면 새로 읽는다.
        public const int SpineKeep = 24, HeavyKeep = 160;
        static readonly LinkedList<string> spineOrder = new LinkedList<string>(), heavyOrder = new LinkedList<string>();
        static readonly Dictionary<string, LinkedListNode<string>> order = new Dictionary<string, LinkedListNode<string>>();

        static bool Heavy(string path) => path.StartsWith("Sfx/hero/") || path.StartsWith("Sfx/monster/") || path.StartsWith("Art/") || path.StartsWith("Voice/");

        static void Touch(string key, LinkedList<string> list, int keep)
        {
            if (order.TryGetValue(key, out var n)) { if (n.List == list && n != list.Last) { list.Remove(n); list.AddLast(n); } return; }
            order[key] = list.AddLast(key);
            while (list.Count > keep)
            {
                var old = list.First.Value;
                list.RemoveFirst();
                order.Remove(old);
                cache.Remove(old);
            }
        }

        /// <summary>붙든 무거운 것의 수(스파인 · 그 밖) — 점검용.</summary>
        public static (int spine, int heavy, int all) Held => (spineOrder.Count, heavyOrder.Count, cache.Count);

        public static T Load<T>(string path) where T : Object
        {
            string key = typeof(T).Name + ":" + path;
            bool heavy = Heavy(path);
            if (cache.TryGetValue(key, out var o)) { if (heavy) Touch(key, heavyOrder, HeavyKeep); return o as T; }
            var r = Resources.Load<T>(path);
            if (r == null) Debug.LogWarning("[Res] 없음: " + path);
            cache[key] = r;
            if (heavy) Touch(key, heavyOrder, HeavyKeep);
            return r;
        }

        public static Sprite Sprite(string path) => Load<Sprite>(path);
        public static Sprite UI(string name) => Load<Sprite>("UI/" + name);
        public static Sprite FxTex(string name) => Load<Sprite>("Fx/Tex/" + name);
        public static AudioClip Clip(string path) => Load<AudioClip>(path);

        public static SkeletonDataAsset Spine(string folder)
        {
            string key = "spine:" + folder;
            if (cache.TryGetValue(key, out var o)) { Touch(key, spineOrder, SpineKeep); return o as SkeletonDataAsset; }
            // 웹 빌드는 스파인을 번들로 받는다(WebBundles — SpineSource 가 번들 먼저, 없으면 Resources)
            using var _h = Bolzena.RunUI.Hitch.Span("Res.Spine 읽기");
            var a = Bolzena.RunUI.SpineStencil.Fix(Bolzena.RunUI.SpineSource.Load(folder));   // 원작 스텐실 가리기(미로 거울 · 셰이디(역전)) → 스파인 클리핑
            if (a == null && Bolzena.RunUI.SpineSource.IsPending(folder)) { Debug.LogWarning("[Res] 스파인 아직 받는 중: " + folder); return null; }
            if (a == null) Debug.LogWarning("[Res] 스파인 없음: " + folder);
            cache[key] = a;
            Touch(key, spineOrder, SpineKeep);
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
