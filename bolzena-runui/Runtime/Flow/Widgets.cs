using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 여러 화면이 같이 쓰는 조각 — 머리 띠 · HP 막대 · 초상 · 카드 · 장비 칸 · 제목 · 말풍선.
    public static class W
    {
        // ── 초상(둥근 틀) ──
        public static RectTransform Face(Transform parent, HeroInfo h, float size, bool ring = true, string name = "face")
        {
            var holder = Ui.Rect(name, parent);
            holder.sizeDelta = new Vector2(size, size);
            holder.Pref(size, size);
            var bg = Ui.Img(holder, Theme.S("circle"), Theme.NavyCell, "disc", true);
            bg.rectTransform.Fill();
            bg.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var ic = h?.Icon;
            if (ic != null)
            {
                var im = Ui.Img(bg.rectTransform, ic, Color.white, "icon");
                im.rectTransform.Fill(-size * 0.04f, -size * 0.1f, -size * 0.04f, size * 0.02f);
                im.preserveAspect = true;
            }
            else
            {
                var t = Ui.Title(bg.rectTransform, h != null && !string.IsNullOrEmpty(h.ko) ? h.ko.Substring(0, 1) : "?", size * 0.45f, Theme.Ink, TextAlignmentOptions.Center);
                t.rectTransform.Fill();
            }
            if (ring)
            {
                var r = Ui.Img(holder, Theme.S("ring"), h != null ? Theme.NatureOf(h.nature) : Theme.Gold, "ring");
                r.rectTransform.Fill(-2, -2, -2, -2);
            }
            return holder;
        }

        // ── HP 막대 ──
        public static (RectTransform root, Action<int, int> set) HpBar(Transform parent, float w, float h, Color? fill = null)
        {
            var bg = Ui.Img(parent, Theme.Pill, Theme.NavyWell.A(0.9f), "hpbar");
            var rt = bg.rectTransform;
            rt.sizeDelta = new Vector2(w, h);
            bg.Pref(w, h);
            var f = Ui.Img(rt, Theme.Pill, fill ?? Theme.Good, "fill");
            var frt = f.rectTransform;
            frt.anchorMin = new Vector2(0, 0); frt.anchorMax = new Vector2(1, 1); frt.pivot = new Vector2(0, 0.5f);
            frt.offsetMin = new Vector2(2, 2); frt.offsetMax = new Vector2(-2, -2);
            var t = Ui.Title(rt, "", h * 0.62f, Color.white, TextAlignmentOptions.Center);
            t.rectTransform.Fill();
            t.Outline(0.25f);
            float shown = -1;
            Action<int, int> set = (hp, max) =>
            {
                float k = max > 0 ? Mathf.Clamp01((float)hp / max) : 0;
                float from = shown < 0 ? k : shown;
                shown = k;
                Tw.Run(frt, 0.6f, x =>
                {
                    if (!frt) return;
                    float v = Mathf.Lerp(from, k, x);
                    frt.anchorMax = new Vector2(Mathf.Max(0.02f, v), 1);
                });
                f.color = k > 0.5f ? (fill ?? Theme.Good) : k > 0.25f ? Theme.Gold : Theme.Bad;
                t.text = $"{hp:N0} / {max:N0}";
            };
            return (rt, set);
        }

        // ── 화면 머리(제목 · 부제) — 왼쪽 위. 금빛 별 + 큰 제목 + 회색 부제 ──
        public static RectTransform Head(RectTransform root, string title, string sub, float left = 28)
        {
            var box = Ui.Rect("head", root);
            box.At(0, 1, left, -20, 900, 84);
            var star = Ui.Img(box, Theme.S("ic_spark"), Theme.Gold, "spark");
            star.rectTransform.At(0, 1, 0, -8, 30, 30);
            var t = Ui.Title(box, title, Theme.Fs2xl, Theme.Ink, TextAlignmentOptions.TopLeft);
            t.rectTransform.Band(1, 50, 40);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.Outline(0.2f);
            if (sub != null)
            {
                var s = Ui.Text(box, sub, Theme.FsBody, Theme.Sub, TextAlignmentOptions.TopLeft);
                s.rectTransform.Band(1, 28, 4, 0, -52);
                s.textWrappingMode = TextWrappingModes.NoWrap;
                s.Outline(0.15f);
            }
            Tw.Rise(box, 0.05f, 18, 0.4f, Vector2.left);
            return box;
        }

        // ── 판 머리(HUD) — 카제나 결: 왼쪽 위에 파티(초상 셋 + HP 막대) 알약, 가운데 지금 화면의 이름, 오른쪽 위에 골드 · 덱 · 장비 · 메뉴 ──
        //   title/sub 를 주면 가운데에 이름을 띄운다(지도 · 상점 · 캠프 · 이벤트 · 보상이 같은 자리에 이름을 둔다)
        public static RectTransform StatusBar(RectTransform root, Flow f, bool deck = true, bool gear = true, bool menu = true, string title = null, string sub = null)
        {
            var P = f.P;
            Toast.Hud = root; Toast.HudTitle = null;
            var band = Ui.Img(root, Theme.S("fade_top"), new Color(1, 1, 1, 0.82f), "hudband");
            band.rectTransform.Band(1, 170);
            band.transform.SetAsFirstSibling();

            // 왼쪽 — 파티 알약: 초상 셋 · 파티 HP 숫자 · 막대
            var left = Ui.Img(root, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.92f), "hud.left", true).rectTransform;
            left.At(0, 1, Theme.Gutter - 6, -14, 424, 62);
            var faces = Ui.Rect("faces", left).At(0, 0.5f, 8, 0, 164, 50);
            Ui.Row(faces, 5, TextAnchor.MiddleLeft, null, false, false);
            foreach (var k in P.S.Party)
            {
                var face = Face(faces, Roster.OfCore(k), 48);
                var hb = face.gameObject.AddComponent<Btn>();
                hb.OnClick = () => f.HeroSheet(k);
            }
            var lab = Ui.Text(left, "파티 HP", Theme.FsCap, Theme.Sub, TextAlignmentOptions.MidlineLeft);
            lab.rectTransform.At(0, 1, 176, -7, 90, 22);
            var num = Ui.Title(left, $"{P.S.PartyHp:N0}<color={Theme.DimTag}> / {P.S.PartyMaxHp:N0}</color>", Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineRight);
            num.rectTransform.At(1, 1, -22, -6, 190, 24);
            num.textWrappingMode = TextWrappingModes.NoWrap; num.overflowMode = TextOverflowModes.Overflow;
            var (hpRt, setHp) = HpBar(left, 226, 10);
            hpRt.At(0, 0, 176, 14, 226, 10);
            foreach (var t in hpRt.GetComponentsInChildren<TextMeshProUGUI>()) t.gameObject.SetActive(false);
            setHp(P.S.PartyHp, P.S.PartyMaxHp);
            Tw.Rise(left, 0.05f, 16, 0.4f, Vector2.left);

            // 가운데 — 화면 이름
            if (title != null)
            {
                var midOuter = Ui.Rect("hud.title", root).At(0.5f, 1, 0, -12, 660, 70);
                // 안쪽 한 겹 — 등장 연출(Rise)은 바깥 알파, 알림이 떠 있는 동안 숨기기는 안쪽 알파(서로 덮어쓰지 않게)
                var mid = Ui.Rect("inner", midOuter).Fill();
                Toast.HudTitle = mid;
                if (Toast.Showing) mid.Group().alpha = 0;   // 알림이 뜬 채 머리 띠를 다시 세웠다(상점에서 사고 난 뒤 따위)
                var tt = Ui.Title(mid, title, Theme.FsXl, Theme.Ink, TextAlignmentOptions.Top);
                tt.rectTransform.Band(1, 38, 0, 0, 0);
                tt.textWrappingMode = TextWrappingModes.NoWrap; tt.overflowMode = TextOverflowModes.Ellipsis;
                tt.Outline(0.2f);
                if (sub != null)
                {
                    var st = Ui.Text(mid, sub, Theme.FsSm, Theme.Sub, TextAlignmentOptions.Top);
                    st.rectTransform.Band(1, 24, 0, 0, -40);
                    st.textWrappingMode = TextWrappingModes.NoWrap; st.overflowMode = TextOverflowModes.Ellipsis;
                    st.Outline(0.2f);
                }
                Tw.Rise(midOuter, 0.08f, 14, 0.4f, Vector2.up);
            }

            // 오른쪽 — 골드 · 덱 · 장비 · 메뉴
            var bar = Ui.Rect("status", root);
            bar.At(1, 1, -(Theme.Gutter - 6), -15, 640, 60);
            Ui.Row(bar, 8, TextAnchor.MiddleRight, null, false, false);
            var (gb, gt) = HudChip(bar, Theme.Icon("gold"), Ui.Gold(P.S.Gold), Theme.Gold);
            gt.name = "gold";
            if (deck) f.Stage.Hot["deck"] = HudChipBtn(bar, Theme.S("ic_deck"), P.S.Deck.Count.ToString(), f.DeckView);
            if (gear) f.Stage.Hot["gear"] = Btn.Icon(bar, Theme.S("ic_sword"), f.GearView, 54, "gear");
            if (menu) f.Stage.Hot["settings"] = Btn.Icon(bar, Theme.S("ic_menu"), () => f.SettingsPanel(true), 54, "menu");
            Tw.Rise(bar, 0.1f, 16, 0.4f, Vector2.up);
            return bar;
        }

        /// <summary>머리 띠의 알약 — 아이콘 + 숫자(골드).</summary>
        static (Image bg, TextMeshProUGUI text) HudChip(Transform parent, Sprite icon, string text, Color c)
        {
            var bg = Ui.Img(parent, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.92f), "chip", true);
            Ui.Row(bg.rectTransform, 8, TextAnchor.MiddleCenter, new RectOffset(12, 20, 6, 6), false, false);
            var ic = Ui.Img(bg.transform, icon, Color.white, "icon"); ic.preserveAspect = true; ic.Pref(32, 32);
            var t = Ui.Title(bg.transform, text, Theme.FsLg, c, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            bg.Pref(-1, 54);
            return (bg, t);
        }

        static Btn HudChipBtn(Transform parent, Sprite icon, string text, Action go)
        {
            var b = Btn.Make(parent, null, BtnStyle.PillDark, go, 0, "deckchip");
            Ui.Row(b.GetComponent<RectTransform>(), 8, TextAnchor.MiddleCenter, new RectOffset(14, 20, 6, 6), false, false);
            var ic = Ui.Img(b.transform, icon, Theme.Ink, "icon"); ic.preserveAspect = true; ic.Pref(26, 26);
            var t = Ui.Title(b.transform, text, Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            b.Pref(-1, 54);
            return b;
        }

        /// <summary>아래쪽 선택지 카드(카제나 이벤트 · 휴식의 가로 카드) — 아이콘 · 제목 · 가는 선 · 설명. 고르면 금빛 테두리(Select).</summary>
        public static Btn Option(Transform parent, string title, string desc, Action go, float h = 150, Sprite icon = null, string name = "option")
        {
            var b = Btn.Make(parent, null, BtnStyle.Glass, go, 0, name);
            b.Pref(-1, h, 1);
            var rt = b.GetComponent<RectTransform>();
            float x0 = 22;
            if (icon != null)
            {
                var disc = Ui.Img(rt, Theme.S("circle"), Theme.NavyWell.A(0.95f), "disc"); disc.rectTransform.At(0, 1, 16, -14, 48, 48);
                var ring = Ui.Img(disc.transform, Theme.S("ring"), Theme.Edge.A(0.9f), "ring"); ring.rectTransform.Fill();
                var ic = Ui.Img(disc.transform, icon, Theme.Gold, "ic"); ic.rectTransform.Fill(12, 12, 12, 12); ic.preserveAspect = true;
                x0 = 76;
            }
            var t = Ui.Title(rt, title, Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.Band(1, 48, x0, 14, -14);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.enableAutoSizing = true; t.fontSizeMin = 16; t.fontSizeMax = Theme.FsLg;
            var line = Ui.Img(rt, Theme.White, Theme.Line, "rule"); line.rectTransform.Band(1, 1, 16, 16, -72);
            var d = Ui.Text(rt, desc, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft);
            d.rectTransform.Fill(20, 12, 16, 82);
            d.enableAutoSizing = true; d.fontSizeMin = 12; d.fontSizeMax = Theme.FsSm;
            d.lineSpacing = 2;
            return b;
        }

        public static void Select(Btn b, bool on)
        {
            if (b == null || b.Bg == null) return;
            b.Bg.sprite = on ? Theme.GlassOn : Theme.Glass;
        }

        /// <summary>장면 위 가운데 이야기 글 — 머리 띠 아래 유리판 한 장(글 길이만큼 높이).</summary>
        public static TextMeshProUGUI Narration(RectTransform root, string text, float y = -120, float w = 1000)
        {
            var bg = Ui.Img(root, Theme.Glass, new Color(1, 1, 1, 0.92f), "narr_bg");
            var t = Ui.Text(bg.transform, text, Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
            float th = Mathf.Max(30, t.GetPreferredValues(text, w - 64, 0).y);
            bg.rectTransform.At(0.5f, 1, 0, y, w, th + 32);
            t.rectTransform.Fill(32, 14, 32, 14);
            t.overflowMode = TextOverflowModes.Ellipsis;
            Tw.FadeIn(bg, 0.5f, 0.2f);
            return t;
        }

        // ── 칸 제목(왼쪽 금빛 막대) ──
        public static TextMeshProUGUI Section(Transform parent, string title, string sub = null, float h = 40)
        {
            var row = Ui.Rect("sec " + title, parent);
            row.Pref(-1, h);
            var bar = Ui.Img(row, Theme.Round, Theme.Gold, "bar");
            bar.rectTransform.At(0, 0.5f, 0, 0, 4, h * 0.6f);
            var t = Ui.Title(row, title + (sub != null ? $"  <size=68%><color={Theme.SubTag}>{sub}</color></size>" : ""), Theme.FsLg, Theme.Ink);
            t.rectTransform.Fill(14, 0, 0, 0);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            return t;
        }

        // ── 말풍선(종이) ──
        public static TextMeshProUGUI Bubble(Transform parent, string who, string line, float w, out RectTransform rt)
        {
            var bg = Ui.Img(parent, Theme.Paper, Color.white, "bubble");
            rt = bg.rectTransform;
            Ui.Col(rt, 6, TextAnchor.UpperLeft, new RectOffset(26, 26, 18, 20));
            var fit = bg.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            rt.sizeDelta = new Vector2(w, 100);
            if (who != null)
            {
                var name = Ui.Title(rt, who, Theme.FsBody, Theme.Hex("C2573F"));
                name.Pref(-1, 24);
            }
            var t = Ui.Text(rt, line, Theme.FsMd, Theme.Hex("2B2230"));
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        // ── 카드(손패 밖 모든 카드 — 덱 · 보상 · 상점 · 상세 · 카드 크게) ── 200×280 기준. 전투 카드(bolzena-unity CardView)와 같은 꼴:
        //   그림이 카드 전체(둥근 창) · 위 왼쪽 큰 비용 숫자 + 밑줄 · 이름 · 종류 알약(아이콘 + 「기본 공격」 따위) · 위 오른쪽 고유 표
        //   아래: 장식 선(가운데 마름모) · 금빛 키워드 태그 「[ 유일 / 소멸 ]」 · 효과 글 — 그림 위 어둠 그라데이션에 얹는다
        //   왼쪽 가장자리 띠 · 테 = 카드 주인 사도의 성격 색(Theme.NatureCard) · 교주 카드 금 · 상태 · 저주 어두운 보라. 종류는 아이콘 · 글로만.
        //   그림(CardArt — Docs/카드그림.md): 시작 카드 = 사도 스탠딩 상반신 · 고유 카드 = 스킬 아이콘(흐린 확대 바탕 + 가운데 선명) · 교주 · 상태 = 종류 무늬
        public static Sprite TypeIcon(string type) => Theme.S(type == "공격" ? "ic_swords" : type == "강화" ? "ic_spark" : type == "상태" || type == "저주" ? "ic_skull" : "ic_moon");

        /// <summary>종류 글 색 — 공격 붉게 · 스킬 푸르게 · 강화 보라 · 상태 잿빛(전투 CardView.TypeColor 와 같은 값).</summary>
        public static Color TypeColor(string type) =>
            type == "공격" ? new Color(1f, 0.55f, 0.58f) : type == "강화" ? new Color(0.8f, 0.68f, 1f)
            : type == "상태" || type == "저주" ? new Color(0.7f, 0.7f, 0.78f) : new Color(0.55f, 0.8f, 1f);

        public static Color CardTint(Flow f, CardView v)
        {
            if (v == null) return Theme.LeaderCard;
            if (v.IsStatus || v.IsCurse) return Theme.StatusCard;
            if (v.Hero == null) return Theme.LeaderCard;
            var hd = f.P.Data.Hero(v.Hero);
            return Theme.NatureCardOf(hd?.Nature ?? Roster.OfCore(v.Hero)?.nature);
        }

        /// <summary>view 를 주면 그 모습(신탁을 얹은 후보 따위)으로 그린다.</summary>
        public static RectTransform Card(Transform parent, Flow f, string id, float w = 200, string name = "card", CardView view = null)
        {
            var P = f.P;
            var v = view ?? P.View(id);
            float k = w / 200f, h = 280 * k;
            string type = v?.Type ?? "스킬";
            bool status = v != null && (v.IsStatus || v.IsCurse);
            Color tint = CardTint(f, v);
            var hero = v?.Hero != null ? Roster.OfCore(v.Hero) : null;
            var bg = Ui.Img(parent, Theme.Round, Theme.NavyWell, name, true);
            bg.pixelsPerUnitMultiplier = 1.7f;   // 모서리 둥글기 ≈ 12
            var rt = bg.rectTransform;
            rt.sizeDelta = new Vector2(w, h);
            bg.Pref(w, h);

            // ── 그림 창(테 안쪽 전체 · 둥글게 오린다) ──
            var win = Ui.Img(rt, Theme.Round, Color.white, "art");
            win.pixelsPerUnitMultiplier = 1.9f;
            win.rectTransform.Fill(2 * k, 2 * k, 2 * k, 2 * k);
            win.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var wr = win.rectTransform;
            var kind = CardArt.Of(id, hero?.art, v != null && v.Unique);
            if (kind == CardArt.Kind.Standing)
            {
                var im = Ui.Img(wr, CardArt.Upper(hero.art, (w - 4 * k) / (h - 4 * k), 0.56f), Color.white, "pic");
                im.rectTransform.Fill();
            }
            else if (kind == CardArt.Kind.Icon)
            {
                var icName = CardArt.IconOf(id);
                var blur = Ui.Img(wr, CardArt.Blur(icName), new Color(0.6f, 0.6f, 0.64f), "blur"); blur.rectTransform.Fill();
                var halo = Ui.Img(wr, Theme.S("soft"), tint.A(0.35f), "halo"); halo.rectTransform.At(0.5f, 0.5f, 0, 22 * k, 230 * k, 230 * k);
                var sh = Ui.Img(wr, Theme.S("shadow", 40), Color.black.A(0.6f), "shadow"); sh.rectTransform.At(0.5f, 0.5f, 0, 14 * k, 150 * k, 150 * k);
                var plate = Ui.Img(wr, Theme.Round, Color.Lerp(tint, Color.white, 0.25f), "plate"); plate.pixelsPerUnitMultiplier = 2.4f;
                plate.rectTransform.At(0.5f, 0.5f, 0, 22 * k, 134 * k, 134 * k);
                var icm = Ui.Img(plate.rectTransform, Theme.Round, Color.white, "clip"); icm.pixelsPerUnitMultiplier = 2.6f;
                icm.rectTransform.Fill(3 * k, 3 * k, 3 * k, 3 * k); icm.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var ic = Ui.Img(icm.rectTransform, CardArt.Icon(icName), Color.white, "icon"); ic.rectTransform.Fill();
            }
            else if (hero?.Icon != null)
            {
                // 그림이 없는 사도(스탠딩 · 아이콘 미복사) — SD 초상으로 대신
                var wash = Ui.Img(wr, Theme.White, Color.Lerp(Theme.NavyWell, tint, 0.3f), "wash"); wash.rectTransform.Fill();
                var im = Ui.Img(wr, hero.Icon, Color.white, "pic"); im.rectTransform.At(0.5f, 0.5f, 0, 20 * k, 230 * k, 230 * k); im.preserveAspect = true;
            }
            else
            {
                // 교주 · 상태 카드 — 종류 무늬
                var wash = Ui.Img(wr, Theme.White, Color.Lerp(Theme.NavyWell, tint, 0.22f), "wash"); wash.rectTransform.Fill();
                var glow = Ui.Img(wr, Theme.S("soft"), tint.A(0.4f), "halo"); glow.rectTransform.At(0.5f, 0.5f, 0, 26 * k, 260 * k, 260 * k);
                var g = Ui.Img(wr, status ? Theme.S("ic_skull") : v != null && v.Hero == null ? Theme.S("ic_crown") : TypeIcon(type), Color.Lerp(tint, Color.white, 0.2f).A(0.75f), "glyph");
                g.rectTransform.At(0.5f, 0.5f, 0, 26 * k, 86 * k, 86 * k); g.preserveAspect = true;
            }

            // ── 아래 글 자리 재기(효과 글 길이만큼 어둠을 올린다) ──
            string full = v != null ? P.Text.Card(v) : "";
            bool hasTags = v != null && v.Tags != null && v.Tags.Count > 0;
            if (hasTags)
            {
                var tagHead = string.Join(" ", v.Tags.Select(t => t + "."));
                if (full.StartsWith(tagHead)) full = full.Substring(tagHead.Length).TrimStart();
                else full = full.Replace(tagHead, "").Trim();
            }
            float descW = w - 22 * k;
            var desc = Ui.Text(rt, full, 13.5f * k, Theme.Ink, TextAlignmentOptions.Bottom, false, "desc");
            desc.lineSpacing = -2;
            float need = string.IsNullOrEmpty(full) ? 0 : desc.GetPreferredValues(full, descW, 0).y;
            float descH = Mathf.Clamp(need + 2 * k, 20 * k, 112 * k);
            float descB = 12 * k;
            float tagY = descB + descH + 3 * k;              // 태그 줄 아래 끝
            float decoY = tagY + (hasTags ? 20 * k : 2 * k) + 5 * k;

            var shadeT = Ui.Img(wr, Theme.S("fade_top"), new Color(0.01f, 0.02f, 0.06f, 0.9f), "shadeT"); shadeT.rectTransform.Band(1, 96 * k);
            var shadeB = Ui.Img(wr, Theme.S("fade_down"), new Color(0.01f, 0.02f, 0.06f, 0.96f), "shadeB"); shadeB.rectTransform.Band(0, Mathf.Min(h, decoY + 92 * k));
            var shadeB2 = Ui.Img(wr, Theme.S("fade_down"), new Color(0.01f, 0.02f, 0.06f, 0.75f), "shadeB2"); shadeB2.rectTransform.Band(0, decoY + 34 * k);   // 글 밑은 한 겹 더(그림 위 글이 읽히게)
            if (need > 0)
            {
                // 효과 글 뒤 어둠 판 — 밝은 그림(흰 옷 따위) 위에서도 글이 읽히게(전투 CardView 의 descbg 와 같은 것)
                var plate = Ui.Img(wr, Theme.Round, new Color(0.01f, 0.02f, 0.06f, 0.6f), "descbg"); plate.pixelsPerUnitMultiplier = 2.2f;
                plate.rectTransform.At(0.5f, 0, 0, descB - 6 * k, w - 12 * k, descH + (hasTags ? 21 * k : 0) + 10 * k);
            }
            var band = Ui.Img(wr, Theme.White, tint, "band"); band.rectTransform.Column(0, 5 * k);

            // ── 테(성격 색) ──
            var frame = Ui.Img(rt, Theme.S("frame", 24), tint, "frame"); frame.rectTransform.Fill();

            // ── 위: 큰 비용 · 밑줄 · 이름 · 종류 알약 ──
            var ct = Ui.Title(rt, v == null ? "?" : v.X ? "X" : status && v.Cost <= 0 ? "-" : v.Cost.ToString(), 44 * k, Color.white, TextAlignmentOptions.Center, "cost");
            ct.rectTransform.At(0, 1, 9 * k, -2 * k, 36 * k, 50 * k);
            ct.Outline(0.24f);
            var cl = Ui.Img(rt, Theme.White, Color.Lerp(tint, Color.white, 0.45f), "costline"); cl.rectTransform.At(0, 1, 15 * k, -50 * k, 24 * k, 2.2f * k);
            var nm = Ui.Title(rt, v?.Name ?? id, 19 * k, Color.white, TextAlignmentOptions.MidlineLeft, "name");
            nm.rectTransform.At(0, 1, 47 * k, -8 * k, w - 47 * k - (v != null && v.Unique ? 28 : 8) * k, 25 * k);
            nm.textWrappingMode = TextWrappingModes.NoWrap;
            nm.enableAutoSizing = true; nm.fontSizeMin = 11 * k; nm.fontSizeMax = 19 * k;
            nm.Outline(0.26f);
            string typeText = status ? type : hero == null ? "교주 · " + type : v != null && !v.Unique ? "기본 " + type : type;
            var pill = Ui.Img(rt, Theme.Pill, new Color(0.01f, 0.02f, 0.06f, 0.62f), "typepill");
            var tic = Ui.Img(pill.rectTransform, TypeIcon(type), TypeColor(type), "typeicon"); tic.rectTransform.At(0, 0.5f, 7 * k, 0, 13 * k, 13 * k); tic.preserveAspect = true;
            var ty = Ui.Text(pill.rectTransform, typeText, 13 * k, TypeColor(type), TextAlignmentOptions.MidlineLeft, false, "type");
            ty.textWrappingMode = TextWrappingModes.NoWrap; ty.overflowMode = TextOverflowModes.Overflow;
            float tyW = Mathf.Min(w - 60 * k, ty.GetPreferredValues(typeText).x + 1);
            ty.rectTransform.Fill(22 * k, 0, 6 * k, 0);
            pill.rectTransform.At(0, 1, 45 * k, -34 * k, tyW + 31 * k, 20 * k);
            if (v != null && v.Unique)
            {
                var star = Ui.Img(rt, Theme.S("ic_spark"), Theme.Gold, "unique");
                star.rectTransform.At(1, 1, -8 * k, -9 * k, 20 * k, 20 * k);
            }

            // ── 아래: 장식 선(가운데 마름모) · 태그 · 효과 글 ──
            var deco = Ui.Img(rt, Theme.White, new Color(1f, 0.88f, 0.6f, 0.6f), "deco"); deco.rectTransform.At(0.5f, 0, 0, decoY, w * 0.56f, 1.4f * k);
            var dot = Ui.Img(rt, Theme.White, new Color(1f, 0.9f, 0.65f), "decodot");
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 0); dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dot.rectTransform.sizeDelta = new Vector2(6 * k, 6 * k); dot.rectTransform.anchoredPosition = new Vector2(0, decoY + 0.7f * k);
            dot.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            if (hasTags)
            {
                var tg = Ui.Title(rt, "[ " + string.Join(" / ", v.Tags) + " ]", 13.5f * k, Theme.Gold, TextAlignmentOptions.Center, "tags");
                tg.rectTransform.At(0.5f, 0, 0, tagY, w - 16 * k, 18 * k);
                tg.textWrappingMode = TextWrappingModes.NoWrap; tg.enableAutoSizing = true; tg.fontSizeMin = 9 * k; tg.fontSizeMax = 13.5f * k;
                tg.Outline(0.2f);
            }
            desc.transform.SetAsLastSibling();
            desc.rectTransform.At(0.5f, 0, 0, descB, descW, descH);
            desc.enableAutoSizing = need > 112 * k; desc.fontSizeMin = 8.5f * k; desc.fontSizeMax = 13.5f * k;
            desc.Outline(0.2f);
            // 신탁 표시 — 장식 선 바로 위
            if (v != null && v.Oracle != null)
            {
                var (ob, otx) = Ui.Chip(rt, Theme.S("ic_spark"), v.Oracle.Name, 22 * k, new Color(0.35f, 0.25f, 0.05f, 0.95f), Theme.Gold, 12.5f * k);
                ob.rectTransform.At(0.5f, 0, 0, decoY + 8 * k, 150 * k, 22 * k);
                UnityEngine.Object.Destroy(ob.GetComponent<HorizontalLayoutGroup>());
                otx.rectTransform.Fill(22 * k, 0, 6 * k, 0);
                otx.alignment = TextAlignmentOptions.Center;
                ob.transform.GetChild(0).GetComponent<RectTransform>().At(0, 0.5f, 6 * k, 0, 14 * k, 14 * k);
            }
            return rt;
        }

        public static Sprite SlotIcon(string slot) => Theme.S(slot == "무기" ? "ic_sword" : slot == "방어구" ? "ic_shield" : "ic_ring_slot");

        public static string StatLine(Stats s)
        {
            var a = new List<string>();
            if (s == null) return "";
            if (s.Hp != 0) a.Add($"HP {(s.Hp > 0 ? "+" : "")}{s.Hp}");
            if (s.Atk != 0) a.Add($"공격력 {(s.Atk > 0 ? "+" : "")}{s.Atk}");
            if (s.Def != 0) a.Add($"방어력 {(s.Def > 0 ? "+" : "")}{s.Def}");
            if (s.Crit != 0) a.Add($"치명 {(s.Crit > 0 ? "+" : "")}{s.Crit}%");
            return string.Join(" · ", a);
        }

        /// <summary>장비 한 장(가로) — 아이콘 · 칸 · 이름 · 등급 · 수치 · 효과.</summary>
        public static RectTransform Equip(Transform parent, Flow f, string id, float w, float h, string name = "equip")
        {
            var e = f.P.Data.Equip(id);
            var gc = Theme.GradeOf(e?.Grade);
            var bg = Ui.Img(parent, Theme.Cell, Color.white, name, true);
            var rt = bg.rectTransform;
            rt.sizeDelta = new Vector2(w, h);
            bg.Pref(w, h);
            Ui.Img(rt, Theme.Round, gc, "grade").rectTransform.Band(1, 4, 16, 16, -1);
            var icb = Ui.Img(rt, Theme.Round, gc.A(0.18f), "iconbg");
            icb.rectTransform.At(0, 1, 14, -14, 56, 56);
            var pic = CardArt.Equip(id);   // 원작 아이콘(장비 · 교주 카드 그림 표) — 없으면 칸 무늬
            var ic = Ui.Img(icb.rectTransform, pic ?? SlotIcon(e?.Slot), pic != null ? Color.white : gc, "icon");
            ic.rectTransform.Fill(pic != null ? 2 : 10, pic != null ? 2 : 10, pic != null ? 2 : 10, pic != null ? 2 : 10);
            ic.preserveAspect = true;
            var nm = Ui.Title(rt, e?.Name ?? id, 22, Theme.Ink);
            nm.rectTransform.At(0, 1, 82, -14, w - 96, 28);
            nm.textWrappingMode = TextWrappingModes.NoWrap;
            var sub = Ui.Text(rt, $"{e?.Slot} · <color=#{ColorUtility.ToHtmlStringRGB(gc)}>{e?.Grade}</color>" + (e?.Affinity != null ? $" · 애착 {Roster.OfCore(e.Affinity).ko}" : ""), 15, Theme.Sub);
            sub.rectTransform.At(0, 1, 82, -44, w - 96, 22);
            var st = Ui.Text(rt, StatLine(e?.Stats), 17, Theme.Good);
            st.rectTransform.At(0, 1, 16, -80, w - 32, 24);
            string eff = e != null && e.Effect.Count > 0 ? f.P.Text.Passives(e.Effect) : (e?.Blurb ?? "");
            var ef = Ui.Text(rt, eff, 15, Theme.Ink, TextAlignmentOptions.TopLeft);
            ef.rectTransform.Fill(16, 10, 16, 108);
            ef.enableAutoSizing = true; ef.fontSizeMin = 11; ef.fontSizeMax = 15;
            return rt;
        }

        // ── 빈 칸 메우기 ──
        public static RectTransform Spacer(Transform parent, float w = -1, float h = -1, float flex = 1)
        {
            var s = Ui.Rect("spacer", parent);
            s.Pref(w, h, flex, flex);
            return s;
        }
    }
}
