using System.Collections.Generic;
using System.IO;
using Bolzena.Core;
using UnityEngine;

namespace Bolzena.Battle
{
    // 규칙 id → 화면의 모습(스파인 · 소리 폴더 · 색 · 맞는 결 · 근접인가). 코어 데이터에는 그림 칸이 없어서 화면 쪽이 갖는다.
    // 트랙 B(com.bolzena.fx)가 사도별 표를 내면 그쪽으로 옮긴다.
    //   사도: 손으로 적은 표(시범 여섯) → 판 화면 사도 표(runui roster.json — 웹판 키 → 원작 그림 키) → id 그대로
    //   적:   손으로 적은 표 → EnemyDef.Art → 콘텐츠의 적 그림 표(world/villages/_그림.json — 로더가 건너뛰는 파일이라 화면이 직접 읽는다)
    //         → 그림이 없으면 비슷한 것으로 대신 선다
    public static class Look
    {
        public class HeroLook
        {
            public string Art;          // Resources/Spine · Sfx/hero · Voice · Art 의 폴더 이름(원작 키)
            public Color Tint;
            public HitKind Hit;
            public bool Melee;
        }

        /// <summary>지금 전투의 데이터(CoreBattle 이 넣는다) — 적 그림(EnemyDef.Art)을 본다.</summary>
        public static GameData Data;

        static readonly Dictionary<string, HeroLook> heroes = new Dictionary<string, HeroLook>
        {
            ["rico"] = new HeroLook { Art = "ricota", Tint = new Color(1f, 0.72f, 0.42f), Hit = HitKind.Blunt, Melee = true },
            ["carrot"] = new HeroLook { Art = "kyarot", Tint = new Color(0.55f, 0.95f, 0.5f), Hit = HitKind.Magic, Melee = false },
            ["sion"] = new HeroLook { Art = "xxionx", Tint = new Color(0.72f, 0.55f, 1f), Hit = HitKind.Slash, Melee = false },
            ["tig"] = new HeroLook { Art = "tig", Tint = new Color(1f, 0.69f, 0.35f), Hit = HitKind.Slash, Melee = true },
            ["leets"] = new HeroLook { Art = "leets", Tint = new Color(0.95f, 0.3f, 0.35f), Hit = HitKind.Blunt, Melee = true },
            ["ed"] = new HeroLook { Art = "ed", Tint = new Color(0.55f, 0.6f, 1f), Hit = HitKind.Magic, Melee = false },
        };
        static readonly Dictionary<string, HeroLook> made = new Dictionary<string, HeroLook>();

        // 성격 색(테두리 · 빛에만 쓴다)
        static readonly Dictionary<string, Color> natureTint = new Dictionary<string, Color>
        {
            ["순수"] = new Color(0.55f, 0.95f, 0.55f), ["냉정"] = new Color(0.55f, 0.75f, 1f), ["광기"] = new Color(0.95f, 0.4f, 0.45f),
            ["활발"] = new Color(1f, 0.8f, 0.4f), ["우울"] = new Color(0.75f, 0.55f, 1f),
        };

        public static HeroLook Hero(string id)
        {
            if (id == null) return new HeroLook { Art = null, Tint = Color.white, Hit = HitKind.Slash };
            if (heroes.TryGetValue(id, out var h)) return h;
            if (made.TryGetValue(id, out h)) return h;
            var info = Bolzena.RunUI.Roster.ByKey(id);
            string art = !string.IsNullOrEmpty(info?.art) ? info.art : id;
            // 같은 그림의 시범 표가 있으면 그 색 · 결 그대로(콘텐츠의 리코타 = 시범의 rico)
            foreach (var x in heroes.Values) if (x.Art == art) { made[id] = x; return x; }
            var hd = Data?.Hero(id);
            string nature = hd?.Nature ?? info?.nature;
            string row = hd?.Row ?? info?.row;
            h = new HeroLook
            {
                Art = art,
                Tint = nature != null && natureTint.TryGetValue(nature, out var c) ? c : Color.white,
                Melee = row == "front",
                Hit = row == "front" ? HitKind.Blunt : row == "back" ? HitKind.Magic : HitKind.Slash,
            };
            made[id] = h;
            return h;
        }

        public static bool MeleeArt(string art)
        {
            foreach (var h in heroes.Values) if (h.Art == art) return h.Melee;
            foreach (var h in made.Values) if (h.Art == art) return h.Melee;
            return false;
        }

        public static Color TintArt(string art)
        {
            foreach (var h in heroes.Values) if (h.Art == art) return h.Tint;
            foreach (var h in made.Values) if (h.Art == art) return h.Tint;
            return Color.white;
        }

        // 적 — (스파인 폴더, 스킨). 그림이 없는 적은 비슷한 것으로 대신 선다
        static readonly Dictionary<string, (string spine, string skin)> enemies = new Dictionary<string, (string, string)>
        {
            ["fairy_close"] = ("fairymobcloserange", "Skin_Mad"),
            ["fairy_long"] = ("fairymoblongrange", "Skin_Cool"),
            ["curburus"] = ("curburus", "Skin_Mad"),
        };
        static readonly Dictionary<string, (string spine, string skin)> resolved = new Dictionary<string, (string, string)>();

        class ArtRow { public string spine, skin, icon; }
        static Dictionary<string, ArtRow> artTable;

        static Dictionary<string, ArtRow> ArtTable()
        {
            if (artTable != null) return artTable;
            artTable = new Dictionary<string, ArtRow>();
            try
            {
                var p = Path.Combine(Bolzena.RunUI.RunPort.CoreDataDir, "world", "villages", "_그림.json");
                if (File.Exists(p))
                {
                    var o = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(p));
                    foreach (var kv in o) artTable[kv.Key] = kv.Value.ToObject<ArtRow>();
                }
                else Debug.LogWarning("[Look] 적 그림 표 없음: " + p);
            }
            catch (System.Exception e) { Debug.LogWarning("[Look] 적 그림 표를 못 읽었습니다: " + e.Message); }
            return artTable;
        }

        static bool HasSpine(string folder) => !string.IsNullOrEmpty(folder) && Resources.LoadAll<Spine.Unity.SkeletonDataAsset>("Spine/" + folder).Length > 0;

        // 원작 스킨 이름 — 표의 mad · cool 은 Skin_Mad · Skin_Cool, default 는 기본(스킨 없음)
        static string SkinName(string s)
        {
            if (string.IsNullOrEmpty(s) || s == "default") return null;
            if (s.StartsWith("Skin_")) return s;
            return "Skin_" + char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        public static (string spine, string skin) Enemy(string id, bool boss)
        {
            if (id != null && enemies.TryGetValue(id, out var e)) return e;
            if (id != null && resolved.TryGetValue(id, out e)) return e;
            string spine = null, skin = null;
            var def = Data?.Enemy(id);
            string sp = def?.Art?.Spine, sk = def?.Art?.Skin;
            if (sp == null && id != null && ArtTable().TryGetValue(id, out var row)) { sp = row.spine; sk = row.skin; }
            if (sp != null)
            {
                // monsterspine/<이름> — 원작 적 스파인 · spine/ingame/<사도키> — 사도의 분신(사도 SD)
                var name = sp.Substring(sp.LastIndexOf('/') + 1);
                spine = sp.Contains("ingame/") ? Hero(name).Art : name;
                skin = SkinName(sk);
            }
            if (!HasSpine(spine))
            {
                if (spine != null) Debug.Log($"[Look] 적 그림 없음 {id}({spine}) — 대역이 섭니다");
                (spine, skin) = boss ? ("curburus", "Skin_Mad") : Stand(id);
            }
            e = (spine, skin);
            if (id != null) resolved[id] = e;
            return e;
        }

        // 그림이 없는 적 — 이름(갈래)마다 늘 같은 대역
        static readonly string[] standIns = { "fairymobcloserange", "fairymoblongrange", "wisps", "ginseng", "magicfork", "lupalu" };
        static (string, string) Stand(string id)
        {
            string kind = id ?? "";
            int us = kind.IndexOf('_');
            if (us > 0) kind = kind.Substring(0, us);
            int h = 0;
            foreach (var ch in kind) h = h * 31 + ch;
            for (int k = 0; k < standIns.Length; k++)
            {
                var s = standIns[(Mathf.Abs(h) + k) % standIns.Length];
                if (HasSpine(s)) return (s, null);
            }
            return ("fairymobcloserange", "Skin_Mad");
        }

        // 카드 그림(임시 규칙 — runui Docs/카드그림.md, 판 화면 W.Card 와 같은 것):
        //   시작 카드 = "st:<그림 키>" 사도 스탠딩 상반신 · 고유 카드 = "ic:<아이콘>" 카드 그림 표(runui cardart.json)의 스킬 아이콘
        //   교주 · 상태 · 저주 · 선물 = "ic:<아이콘>" 장비 · 교주 카드 그림 표(itemart.json) — 없으면 null. 표에 없는 고유 카드는 사도 아이콘을 카드 id 로 돌려 쓴다
        public static string CardArt(string heroId, string cardId, bool unique, string type)
        {
            if (heroId == null)
            {
                // 교주 · 상태 · 저주 · 선물 — runui 장비 · 교주 카드 그림 표(itemart.json), 없으면 그림 없음
                var item = Bolzena.RunUI.CardArt.IconOf(cardId);
                return item != null ? "ic:" + item : null;
            }
            var art = Hero(heroId).Art;
            if (!unique) return "st:" + art;
            var mapped = Bolzena.RunUI.CardArt.IconOf(cardId);
            if (mapped != null) return "ic:" + mapped;
            int h = 0;
            foreach (var ch in cardId ?? "") h = h * 31 + ch;
            h = Mathf.Abs(h);
            return "ic:" + (type == "공격" ? "icon_admissionskill_" + art : "aside_skill_" + art + "_" + (1 + h % 3));
        }
    }
}
