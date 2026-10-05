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

        /// <summary>
        /// 그림 감사(-artaudit) — 적 데이터 전부를 돌며 스파인 · 스킨 · 아이콘이 실제로 있는지 본다. 없는 것을 「[ArtAudit]」 줄로 남긴다(고치는 건 콘텐츠 쪽).
        /// </summary>
        public static void Audit()
        {
            int n = 0, bad = 0;
            foreach (var kv in Data.Enemies)
            {
                n++;
                var id = kv.Key;
                var def = kv.Value;
                var (spine, skin) = Enemy(id, def.Boss);
                string want = def.Art?.Spine;
                if (want == null && ArtTable().TryGetValue(id, out var row)) want = row.spine;
                string problem = null;
                var d = Res.Spine(spine)?.GetSkeletonData(true);
                if (d == null) problem = $"스파인 없음 {spine}";
                else if (want != null && !want.Contains("ingame/") && spine != want.Substring(want.LastIndexOf('/') + 1)) problem = $"그림 없음 {want} → 대역 {spine}";
                else if (!string.IsNullOrEmpty(skin))
                {
                    bool has = false;
                    foreach (var k in d.Skins) if (string.Equals(k.Name, skin, System.StringComparison.OrdinalIgnoreCase)) has = true;
                    if (!has)
                    {
                        var names = new List<string>();
                        foreach (var k in d.Skins) names.Add(k.Name);
                        problem = "스킨 없음 " + spine + "/" + skin + " (있는 것: " + string.Join(", ", names) + ")";
                    }
                }
                var ic = EnemyIcon(id);
                bool icon = !string.IsNullOrEmpty(ic) && Res.Sprite("Art/Monster/" + ic) != null;
                if (problem != null || !icon)
                {
                    bad++;
                    string tag = def.Name + (def.Boss ? " · 보스" : "") + (def.Clone != null ? " · 클론 " + def.Clone : "");
                    string iconNote = icon ? "" : " · 아이콘 없음 " + (ic.Length > 0 ? ic : "(표에 없음)");
                    Debug.Log("[ArtAudit] " + id + " (" + tag + ") — " + (problem ?? "그림 있음") + iconNote);
                }
            }
            Debug.Log($"[ArtAudit] 끝 — 적 {n} · 문제 {bad}");
        }

        // 성격 ↔ 원작 스킨 꼬리(Skin_Naive …) — _ref/적스파인_목록.md
        static readonly Dictionary<string, string> NatEn = new Dictionary<string, string> { ["순수"] = "naive", ["광기"] = "mad", ["활발"] = "jolly", ["우울"] = "gloomy", ["냉정"] = "cool" };

        static bool SkinIn(Spine.SkeletonData d, string skin)
        {
            foreach (var k in d.Skins) if (string.Equals(k.Name, skin, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // 판 속성으로 스킨을 바꾸지 않는 몬스터 — 누루링(스킨이 종족) · 크레용 기사 · nt00(성격이 폴더마다)
        static bool FixedSkin(string spine) => spine != null && (spine.StartsWith("nururing") || spine.StartsWith("crayonknight") || spine.StartsWith("nt00"));

        /// <summary>
        /// 그 성격(판의 적 속성으로 통일된 것)으로 그린 모습 — 몬스터 스파인에 그 성격 스킨(Skin_Cool …)이 실제로 있을 때만 바꾼다.
        /// 없으면 원래 짝(_그림.json) 그대로 + 경고. 클론(사도 그림) · 대역 · 스킨이 성격이 아닌 몬스터는 그대로.
        /// </summary>
        public static (string spine, string skin) EnemyAs(string id, bool boss, string nature)
        {
            var b = Enemy(id, boss);
            var def = Data?.Enemy(id);
            if (def == null || nature == null || def.Clone != null || standIn.Contains(id) || FixedSkin(b.spine)) return b;
            if (!NatEn.TryGetValue(nature, out var en)) return b;
            string want = b.spine == "orica" && en == "jolly" ? "Skin_Joly" : "Skin_" + char.ToUpperInvariant(en[0]) + en.Substring(1);
            if (string.Equals(b.skin, want, System.StringComparison.OrdinalIgnoreCase)) return b;
            var d = Res.Spine(b.spine)?.GetSkeletonData(true);
            if (d != null && SkinIn(d, want)) return (b.spine, want);
            Debug.LogWarning($"[Look] {id}: {b.spine} 에 {nature}({want}) 스킨이 없다 — 원래 모습 {b.skin ?? "기본"}");
            return b;
        }

        /// <summary>그 성격의 정지 아이콘 — icon_<몬스터><성격> 이 있으면 그것, 없으면 원래 아이콘.</summary>
        public static string EnemyIconAs(string id, string nature)
        {
            var def = Data?.Enemy(id);
            var baseIcon = EnemyIcon(id);
            if (def == null || nature == null || def.Clone != null || !NatEn.TryGetValue(nature, out var en)) return baseIcon;
            var spine = Enemy(id, def.Boss).spine;
            if (FixedSkin(spine) || standIn.Contains(id)) return baseIcon;
            var ic = "icon_" + spine + en;
            return Res.Sprite("Art/Monster/" + ic) != null ? ic : baseIcon;
        }

        /// <summary>적의 정지 아이콘 이름(원작 monster/icon_*.png → Resources/Art/Monster) — 적 데이터 Art.Icon, 없으면 그림 표. 없으면 "".</summary>
        public static string EnemyIcon(string id)
        {
            var def = Data?.Enemy(id);
            var ic = def?.Art?.Icon;
            if (ic == null && id != null && ArtTable().TryGetValue(id, out var row)) ic = row.icon;
            return ic ?? "";
        }

        public static (string spine, string skin) Enemy(string id, bool boss)
        {
            if (id != null && enemies.TryGetValue(id, out var e)) return e;
            if (id != null && resolved.TryGetValue(id, out e)) return e;
            string spine = null, skin = null;
            var def = Data?.Enemy(id);
            string sp = def?.Art?.Spine, sk = def?.Art?.Skin;
            if (sp == null && id != null && ArtTable().TryGetValue(id, out var row)) { sp = row.spine; sk = row.skin; }
            if (sp == null && def?.Clone != null) sp = "spine/ingame/" + def.Clone;   // 빌린 몸 클론 — 그 사도 그림
            if (sp != null)
            {
                // monsterspine/<이름> — 원작 적 스파인 · spine/ingame/<사도키> — 사도의 분신(사도 SD)
                var name = sp.Substring(sp.LastIndexOf('/') + 1);
                spine = sp.Contains("ingame/") ? Hero(name).Art : name;
                skin = SkinName(sk);
            }
            if (!HasSpine(spine))
            {
                if (spine != null) Debug.Log($"[Look] 적 그림 없음 {id}({spine}) — 아이콘이 있으면 아이콘, 없으면 대역");
                if (id != null) standIn.Add(id);
                (spine, skin) = boss ? ("curburus", "Skin_Mad") : Stand(id);
            }
            e = (spine, skin);
            if (id != null) resolved[id] = e;
            return e;
        }

        static readonly HashSet<string> standIn = new HashSet<string>();
        /// <summary>제 스파인이 없어 대역이 선 적인가 — 화면은 원작 아이콘이 있으면 대역 대신 아이콘을 세운다(확실한 그림만).</summary>
        public static bool IsStandIn(string id) { if (id != null && !resolved.ContainsKey(id)) Enemy(id, Data?.Enemy(id)?.Boss ?? false); return id != null && standIn.Contains(id); }

        // 대역의 옷 — Normal 이 있으면 그것(옷 입히기 규칙), 기본 스킨이 빈 몸(wisps 처럼 Normal · default 가 없다)이면 그 스파인의 첫 스킨(이름순)
        static string StandSkin(string folder)
        {
            var d = Res.Spine(folder)?.GetSkeletonData(true);
            if (d == null) return null;
            Spine.Skin best = null;
            foreach (var k in d.Skins)
            {
                if (k == d.DefaultSkin) continue;
                if (string.Equals(k.Name, "normal", System.StringComparison.OrdinalIgnoreCase)) return k.Name;
                if (best == null || string.CompareOrdinal(k.Name, best.Name) < 0) best = k;
            }
            int baseN = d.DefaultSkin != null ? d.DefaultSkin.Attachments.Count : 0;
            return best != null && baseN < best.Attachments.Count ? best.Name : null;
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
                if (HasSpine(s)) return (s, StandSkin(s));
            }
            return ("fairymobcloserange", "Skin_Mad");
        }

        // 카드 그림(임시 규칙 — runui Docs/카드그림.md, 판 화면 W.Card 와 같은 것):
        //   시작 카드 = "st:<그림 키>" 사도 스탠딩 상반신 · 고유 · 생성 카드 = "pc:<그림>" 고른 원작 그림(runui cardart.json 의 pics),
        //   없으면 "ic:<아이콘>" 카드 그림 표의 스킬 아이콘
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
            var pic = Bolzena.RunUI.CardArt.PicOf(cardId);
            if (pic != null && Bolzena.RunUI.CardArt.Pic(pic) != null) return "pc:" + pic;
            if (!unique)
            {
                var tok = Bolzena.RunUI.CardArt.IconOf(cardId);   // 생성 카드에 고른 원작 스킬 아이콘(시작 카드는 표에 없다)
                return tok != null && Bolzena.RunUI.CardArt.Icon(tok) != null ? "ic:" + tok : "st:" + art;
            }
            var mapped = Bolzena.RunUI.CardArt.IconOf(cardId);
            if (mapped != null) return "ic:" + mapped;
            int h = 0;
            foreach (var ch in cardId ?? "") h = h * 31 + ch;
            h = Mathf.Abs(h);
            return "ic:" + (type == "공격" ? "icon_admissionskill_" + art : "aside_skill_" + art + "_" + (1 + h % 3));
        }
    }
}
