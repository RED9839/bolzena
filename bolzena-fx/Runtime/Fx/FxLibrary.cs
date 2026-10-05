using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.Fx
{
    // 원작 이펙트 찾기 — FxImport 가 만든 것을 Resources/<Root>/ 에서 읽는다.
    //   FxLibrary.Get("에르핀", "ult")      → 그 사도의 고학년 이펙트 이름들(없으면 빈 배열)
    //   FxLibrary.Effect("fx_erpin_ultimate_ground_1") → 파티클 판(FxEffect) · FxLibrary.Sheet(이름) → 구운 낱장(FxSheet)
    // 본문은 처음 부를 때 읽어 붙들어 둔다(텍스처도 그때). 색인만 가볍게 늘 들고 있다.
    public static class FxLibrary
    {
        public static string Root = "BolzenaFx";
        static FxLibraryAsset index;
        static Dictionary<string, FxLibraryAsset.Hero> heroes;
        static Dictionary<string, FxLibraryAsset.Hero> heroesByArt;
        static Dictionary<string, FxLibraryAsset.Entry> effects;
        static readonly Dictionary<string, FxEffect> fxCache = new Dictionary<string, FxEffect>();
        static readonly Dictionary<string, FxSheet> sheetCache = new Dictionary<string, FxSheet>();
        static readonly string[] Empty = new string[0];

        public static bool Ready => Load();

        public static bool Load()
        {
            if (index != null) return true;
            index = Resources.Load<FxLibraryAsset>(Root + "/FxLibrary");
            if (index == null) return false;
            heroes = new Dictionary<string, FxLibraryAsset.Hero>();
            heroesByArt = new Dictionary<string, FxLibraryAsset.Hero>();
            effects = new Dictionary<string, FxLibraryAsset.Entry>();
            foreach (var h in index.Heroes) { heroes[h.Key] = h; if (!string.IsNullOrEmpty(h.Art)) heroesByArt[h.Art] = h; }
            foreach (var e in index.Effects) effects[e.Name] = e;
            return true;
        }

        public static IEnumerable<FxLibraryAsset.Hero> Heroes { get { if (Load()) foreach (var h in index.Heroes) yield return h; } }
        public static IEnumerable<FxLibraryAsset.Entry> Entries { get { if (Load()) foreach (var e in index.Effects) yield return e; } }

        // 사도 키(에르핀) 또는 원작 그림 이름(erpin)
        public static FxLibraryAsset.Hero HeroOf(string key)
        {
            if (!Load() || string.IsNullOrEmpty(key)) return null;
            if (heroes.TryGetValue(key, out var h)) return h;
            return heroesByArt.TryGetValue(key.ToLowerInvariant(), out h) ? h : null;
        }

        // kind: "ult" 고학년(궁극기가 없는 사도는 스킬로 메운 것) · "attack" 평타 · "power" 센 공격(Attack2) · "skill" 스킬 · "sig" 시그니처
        public static string[] Get(string heroKey, string kind = "ult")
        {
            var h = HeroOf(heroKey);
            if (h == null) return Empty;
            var l = ListOf(h, kind);
            return l == null ? Empty : l.ToArray();
        }

        static List<string> ListOf(FxLibraryAsset.Hero h, string kind)
        {
            switch (kind)
            {
                case "ult": return h.Ult;
                case "attack": return h.Attack;
                case "power": return h.Power;
                case "skill": return h.Skill;
                case "sig": return h.Sig;
                default: return null;
            }
        }

        public static bool Has(string heroKey, string kind) { var h = HeroOf(heroKey); var l = h != null ? ListOf(h, kind) : null; return l != null && l.Count > 0; }

        // 공용 갈래의 후보들(FxRules.COMMON 이 고른다) — 없으면 빈 배열
        public static string[] Common(string kind)
        {
            if (!Load() || kind == null) return Empty;
            foreach (var g in index.Common) if (g.Kind == kind) return g.Names.ToArray();
            return Empty;
        }

        public static bool HasUlt(string heroKey) { var h = HeroOf(heroKey); return h != null && h.Ult.Count > 0; }

        public static FxLibraryAsset.Entry Info(string name)
        {
            if (!Load() || name == null) return null;
            return effects.TryGetValue(name.ToLowerInvariant(), out var e) ? e : (effects.TryGetValue(name, out e) ? e : null);
        }

        public static float Duration(string name)
        {
            var e = Info(name);
            return e != null && e.Dur > 0 ? e.Dur : 1f;
        }

        public static FxEffect Effect(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (fxCache.TryGetValue(name, out var fx)) return fx;
            fx = Resources.Load<FxEffect>(Root + "/Effects/" + name);
            fxCache[name] = fx;
            return fx;
        }

        public static FxSheet Sheet(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var key = name.ToLowerInvariant();
            if (sheetCache.TryGetValue(key, out var s)) return s;
            s = Resources.Load<FxSheet>(Root + "/Baked/" + key);
            sheetCache[key] = s;
            return s;
        }

        // 그 이름을 틀 수 있는가(구운 낱장 또는 파티클 판)
        public static bool Has(string name) => Sheet(name) != null || Effect(name) != null;

        // 미리 읽어 둔다 — 컷인이 도는 동안 · 싸움을 열 때
        public static void Preload(IEnumerable<string> names) { foreach (var n in names) { if (Sheet(n) == null) Effect(n); } }
        public static void PreloadUlt(string heroKey) => Preload(Get(heroKey));
        // 읽기 + 텍스처 올리기 + 셰이더 준비까지(BolzenaFx.Prewarm) — 첫 재생에 멈칫하지 않게. 공용 타격 이펙트도 같이
        public static int Prewarm(string heroKey, bool hits = true, bool cards = false)
        {
            var names = new List<string>(Get(heroKey));
            if (cards) foreach (var k in new[] { "attack", "power", "skill", "sig" }) names.AddRange(Get(heroKey, k));
            if (hits) foreach (var l in MotionTables.HIT_FX.Values) names.AddRange(l);
            int n = BolzenaFx.Prewarm(names);
            // 코드도 데운다(Mono JIT) — 화면 밖에서 한 번 틀고 바로 걷는다. 고학년 차례 셈도 한 번
            UltFx.Plan(heroKey);
            foreach (var x in names)
            {
                var r = BolzenaFx.Play(x, new FxPlayOptions { At = new UnityEngine.Vector3(1e5f, 1e5f, 0), Until = 0.05f });
                if (r != null) break;
            }
            return n;
        }
    }
}
