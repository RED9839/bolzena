using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 장비 — 웹판 ui.js settleGear · gearPanel. 장비를 얻으면 곧장 「누구에게 낄까요 / 팔까요」(넣어 둘 가방은 없다).
    public partial class Flow
    {
        // 장비 고르기 — 화면 하나를 덮는 창(카제나 장비 고르기의 배치만: 왼쪽 받은 장비 카드 → 가지 선 → 오른쪽 사도 셋 줄 · 아래 띠).
        //   위: 머리 띠(파티 HP · 초상 · 「장비」 · 골드) · 그 아래 오른쪽 「낄 사도를 고르세요」
        //   왼쪽: 받은 장비 — 그림 칸(등급색 바탕 + 칸 아이콘 + 등급 별) · 종류 아이콘 · 이름(등급색) · 수치 띠 · 효과 글(수치 금빛) · 돋보기
        //   오른쪽: 사도 줄 셋 — 초상 · 역할/줄 · 무기 · 방어구 · 장신구 칸(받은 장비와 같은 칸은 금 테). 찬 칸을 길게 누르면 지금 것 ↔ 새 것
        //   아래: 덱 정보(장수 · 고유 카드 평균 비용) · 팔기(+골드 — 산 장비는 없음) · 확인
        // 넣어 둘 가방이 없어서 「뒤로」 는 없다 — 끼거나 팔아야 넘어간다.
        void GearDialog(string equipId, Action done)
        {
            var e = P.Data.Equip(equipId);
            bool bought = P.Bought(equipId);
            P.Save("gear");
            var gc = Theme.GradeOf(e?.Grade);
            var size = Stage.Size;

            var layer = Ui.Rect("modal gear", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, Theme.Night, "dim", true); dim.rectTransform.Fill();   // 불투명 — 선형 색 공간이라 5% 만 비쳐도 밑 화면 글이 25% 밝기로 보였다
            var glow = Ui.Img(layer, Theme.S("soft"), gc.A(0.10f), "glow"); glow.rectTransform.At(0.25f, 0.5f, 0, 0, 1100, 900);
            var lg = layer.Group(); lg.alpha = 0;
            Tw.Run(layer, 0.25f, k => { if (lg) lg.alpha = k; });
            var hud0 = Toast.Hud; var title0 = Toast.HudTitle;
            W.StatusBar(layer, this, false, false, false, "장비", null);
            // 제목 아래 얇은 장식 선(가운데 금 마름모)
            var orn = Ui.Rect("ornament", layer).At(0.5f, 1, 0, -60, 560, 12);
            Ui.Img(orn, Theme.White, Theme.Edge.A(0.35f), "l").rectTransform.Band(0.5f, 1, 0, 0, 0);
            var obar = Ui.Img(orn, Theme.Round, Theme.Gold.A(0.8f), "bar"); obar.rectTransform.At(0.5f, 0.5f, 0, 0, 120, 3);
            var dia = Ui.Img(orn, Theme.White, Theme.Gold, "dia"); dia.rectTransform.At(0.5f, 0.5f, 0, 0, 8, 8); dia.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var rule = Ui.Img(layer, Theme.White, Theme.Line, "rule"); rule.rectTransform.Band(1, 1, Theme.Gutter, Theme.Gutter, -92);
            var guide = Ui.Title(layer, bought ? "낄 사도를 고르세요" : "낄 사도를 고르거나 파세요", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineRight);
            guide.rectTransform.At(1, 1, -Theme.Gutter - 8, -100, 600, 34);
            guide.Outline(0.2f);

            bool closed = false;
            void Close(Action after)
            {
                if (closed) return;
                closed = true;
                Toast.Hud = hud0; Toast.HudTitle = title0;
                Tw.Run(layer, 0.18f, k => { if (lg) lg.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); after?.Invoke(); });
            }

            // ── 가운데 무대 ── 폭은 캔버스에 맞추되 1500 을 넘지 않는다(21:9 에서도 가운데 한 덩어리)
            float footH = Theme.C(96, 84);
            float rowH = Theme.C(150, 128), rowGap = Theme.C(16, 10), rowW = 560;
            float H = rowH * 3 + rowGap * 2;
            float Wd = Mathf.Min(1500, size.x - Theme.Gutter * 2);
            float cardW = Mathf.Min(620, Wd - rowW - 150), cardH = Theme.C(270, 232);
            // 머리(위 ~140)와 아래 띠 사이 가운데
            float midY = (footH - 140) / 2f;
            var stage = Ui.Rect("stage", layer).At(0.5f, 0.5f, 0, midY, Wd, H);

            // 받은 장비 카드
            float hintH = 44, groupH = cardH + 14 + hintH;
            float cardTop = (H - groupH) / 2;
            var lines = Ui.Rect("branches", stage).Fill();
            var card = Ui.Img(stage, Theme.Cell, Color.white, "item", true).rectTransform;
            card.At(0, 1, 0, -cardTop, cardW, cardH);
            Ui.Shadow(card, 26, -10, 0.6f);
            var rim = Ui.Img(card, Theme.Frame, gc.A(0.75f), "rim"); rim.rectTransform.Fill();
            float artW = Mathf.Round(cardH * 0.66f);
            var art = Ui.Img(card, Theme.Round, Color.Lerp(gc, Theme.NavyWell, 0.6f), "art"); art.rectTransform.At(0, 0.5f, 10, 0, artW, cardH - 20);
            var artHi = Ui.Img(art.transform, Theme.S("fade_top"), gc.A(0.5f), "hi"); artHi.rectTransform.Fill();
            var deco = Ui.Img(art.transform, Theme.S("tile_glow"), Color.white.A(0.14f), "deco"); deco.rectTransform.At(0.5f, 0.56f, 0, 0, artW * 0.92f, artW * 0.55f);
            var epic = CardArt.Equip(e?.Id);   // 원작 아이콘(없으면 칸 무늬)
            var big = Ui.Img(art.transform, epic ?? W.SlotIcon(e?.Slot), Color.white, "icon"); big.rectTransform.At(0.5f, 0.56f, 0, 0, artW * (epic != null ? 0.78f : 0.48f), artW * (epic != null ? 0.78f : 0.48f)); big.preserveAspect = true;
            var stars = Ui.Rect("stars", art.rectTransform).Band(0, 26, 6, 6, 10);
            Ui.Row(stars, 2, TextAnchor.MiddleCenter, null, false, false);
            int gr = GradeStars(e?.Grade);
            for (int i = 0; i < 4; i++) { var st = Ui.Img(stars, Theme.S("ic_spark"), i < gr ? Theme.Gold : Color.white.A(0.22f), "star"); st.Pref(20, 20); st.preserveAspect = true; }

            float tx = artW + 30;
            var kind = Ui.Img(card, Theme.Round, gc.A(0.22f), "kind"); kind.rectTransform.At(0, 1, tx, -20, 40, 40);
            var kic = Ui.Img(kind.transform, W.SlotIcon(e?.Slot), gc, "ic"); kic.rectTransform.Fill(8, 8, 8, 8); kic.preserveAspect = true;
            var nm = Ui.Title(card, e?.Name ?? equipId, Theme.FsLg + 2, gc, TextAlignmentOptions.MidlineLeft);
            nm.rectTransform.At(0, 1, tx + 50, -20, cardW - tx - 120, 40);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = Theme.FsMd; nm.fontSizeMax = Theme.FsLg + 2;
            var zoom = Btn.Icon(card, Theme.S("ic_zoom"), () => EquipDetail(equipId), 44, "zoom");
            zoom.GetComponent<RectTransform>().At(1, 1, -14, -18, 44, 44);
            Stage.Hot["gear.zoom"] = zoom;
            // 수치 띠 — 한 줄에 하나(이름 왼쪽 · 값 오른쪽)
            float y = -72;
            foreach (var (label, val) in StatPairs(e?.Stats))
            {
                var band = Ui.Img(card, Theme.Round, Color.white.A(0.07f), "stat"); band.rectTransform.At(0, 1, tx, y, cardW - tx - 20, 32);
                var l = Ui.Text(band.transform, label, Theme.FsSm + 1, Theme.Sub, TextAlignmentOptions.MidlineLeft); l.rectTransform.Fill(12, 0, 0, 0);
                var v = Ui.Title(band.transform, val, Theme.FsMd, Theme.Good, TextAlignmentOptions.MidlineRight); v.rectTransform.Fill(0, 0, 12, 0);
                y -= 36;
            }
            string eff = e != null && e.Effect.Count > 0 ? P.Text.Passives(e.Effect) : (e?.Blurb ?? "");
            if (e?.Affinity != null)
                eff += $"\n<color=#FF9AC8>애착 {Roster.OfCore(e.Affinity).ko}</color>" + (e.AffinityStats != null ? " — " + W.StatLine(e.AffinityStats) : "")
                     + (e.AffinityEffect.Count > 0 ? " · " + P.Text.Passives(e.AffinityEffect) : "");
            var ef = Ui.Text(card, Highlight(eff), Theme.FsBody, Theme.Ink, TextAlignmentOptions.TopLeft);
            ef.rectTransform.Fill(tx + 2, 16, 20, -y + 6);
            ef.enableAutoSizing = true; ef.fontSizeMin = 12; ef.fontSizeMax = Theme.FsBody; ef.lineSpacing = 4;
            Tw.Rise(card, 0.05f, 30, 0.45f, Vector2.left);

            var hint = Ui.Img(stage, Theme.Glass, new Color(1, 1, 1, 0.8f), "hint"); hint.rectTransform.At(0, 1, 10, -(cardTop + cardH + 14), cardW - 20, hintH);
            var ht = Ui.Text(hint.transform, "낀 장비 칸을 길게 누르면 지금 것과 새 것을 견줍니다", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center); ht.rectTransform.Fill(12, 0, 12, 0);

            // 가지 선 — 카드 오른쪽 가운데 → 줄기 → 사도 줄마다
            float rowX = Wd - rowW, trunk = cardW + (rowX - cardW) * 0.55f, cy = cardTop + cardH / 2;
            Color lc = Theme.Edge.A(0.6f);
            void Seg(float x0, float y0, float x1, float y1)
            {
                var im = Ui.Img(lines, Theme.White, lc, "seg");
                var r = im.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
                r.anchoredPosition = new Vector2(Mathf.Min(x0, x1) - 1, -Mathf.Min(y0, y1) + 1);
                r.sizeDelta = new Vector2(Mathf.Abs(x1 - x0) + 2, Mathf.Abs(y1 - y0) + 2);
            }
            void Dot(float x, float yy, float s)
            {
                var d = Ui.Img(lines, Theme.S("circle"), Theme.Gold, "dot"); d.rectTransform.At(0, 1, x - s / 2, -yy + s / 2, s, s);
            }
            Seg(cardW, cy, trunk, cy);
            Dot(cardW + 2, cy, 10);
            Seg(trunk, Mathf.Min(rowH / 2, cy), trunk, Mathf.Max(H - rowH / 2, cy));
            var lg2 = lines.Group(); lg2.alpha = 0; Tw.Run(lines, 0.5f, k => { if (lg2) lg2.alpha = k; }, Tw.OutCubic, 0.25f);

            // ── 사도 줄 셋 ──
            string pick = null;
            Btn confirm = null;
            var rows = new System.Collections.Generic.List<(string key, Btn b, TextMeshProUGUI name)>();
            void Select(string k)
            {
                pick = k;
                foreach (var (key, b, n) in rows) { b.Bg.sprite = key == k ? Theme.CellOn : Theme.Cell; if (n) n.color = key == k ? Theme.Gold : Theme.Ink; }
                confirm.Interactable = true;
                confirm.SetLabel($"{Roster.OfCore(k).ko} 에게 낍니다");
            }
            int ri = 0;
            foreach (var k in P.S.Party)
            {
                var h = Roster.OfCore(k);
                float ry = ri * (rowH + rowGap);
                Seg(trunk, ry + rowH / 2, rowX, ry + rowH / 2);
                Dot(rowX - 3, ry + rowH / 2, 9);
                var b = Btn.Make(stage, null, BtnStyle.Cell, () => Select(k), 0, "hero " + k);
                var rt = b.GetComponent<RectTransform>(); rt.At(0, 1, rowX, -ry, rowW, rowH);
                // 초상(세로 칸 · 아래 어둡게) + 역할 · 줄
                float pw = Mathf.Round(rowH * 0.82f);
                var well = Ui.Img(rt, Theme.Round, Theme.NavyWell, "well"); well.rectTransform.At(0, 0.5f, 10, 0, pw, rowH - 16);
                well.gameObject.AddComponent<RectMask2D>();
                var ic = h?.Icon;
                if (ic != null) { var im = Ui.Img(well.transform, ic, Color.white, "portrait"); im.rectTransform.At(0.5f, 1, 0, 8, pw * 1.15f, pw * 1.15f); im.preserveAspect = true; }
                var shade = Ui.Img(well.transform, Theme.S("fade_down"), Color.black.A(0.8f), "shade"); shade.rectTransform.Band(0, (rowH - 16) * 0.5f);
                var tag = Ui.Title(well.transform, $"<size=68%><color={Theme.SubTag}>{h.role}</color></size>\n{h.nature}", Theme.FsMd, Theme.Ink, TextAlignmentOptions.BottomLeft);
                tag.rectTransform.Fill(8, 6, 4, 0); tag.lineSpacing = -12; tag.Outline(0.2f);
                float sx = pw + 26;
                var hn = Ui.Title(rt, h.ko, Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                hn.rectTransform.At(0, 1, sx, -10, 220, 32);
                hn.textWrappingMode = TextWrappingModes.NoWrap; hn.enableAutoSizing = true; hn.fontSizeMin = 14; hn.fontSizeMax = Theme.FsLg;
                // 바뀌는 수치(지금 낀 것 → 새 것)
                var cur = P.GearOf(k).TryGetValue(e.Slot, out var c) ? c : null;
                var now = P.StatsOf(equipId, k);
                var old = cur != null ? P.StatsOf(cur, k) : new Core.Stats();
                var diff = new Core.Stats { Hp = now.Hp - old.Hp, Atk = now.Atk - old.Atk, Def = now.Def - old.Def, Crit = now.Crit - old.Crit };
                var dl = W.StatLine(diff);
                var dt = Ui.Text(rt, (dl.Length > 0 ? dl : $"<color={Theme.DimTag}>수치 그대로</color>") + (e.Affinity == k ? "  <color=#FF9AC8>애착!</color>" : "")
                    + (cur != null ? $"\n<size=86%><color=#FF8C7A>지금 것은 팔림 +{P.SellPrice(cur)}</color></size>" : ""), Theme.FsSm, Theme.Good, TextAlignmentOptions.TopRight);
                dt.rectTransform.At(1, 1, -14, -12, rowW - sx - 190, 46);
                dt.enableAutoSizing = true; dt.fontSizeMin = 11; dt.fontSizeMax = Theme.FsSm;
                // 장비 칸 셋
                float box = Mathf.Min(86, rowH - 60);
                for (int si = 0; si < RunPort.Slots.Length; si++)
                {
                    var slot = RunPort.Slots[si];
                    var id = P.GearOf(k).TryGetValue(slot, out var x) ? x : null;
                    var se = id != null ? P.Data.Equip(id) : null;
                    var sc = se != null ? Theme.GradeOf(se.Grade) : Theme.Dim;
                    var sb = Btn.Make(rt, null, BtnStyle.Cell, () => Select(k), 0, "slot " + slot);
                    var srt = sb.GetComponent<RectTransform>(); srt.At(0, 0, sx + si * (box + 10), 12, box, box);
                    sb.SetColor(se != null ? Color.Lerp(sc, Theme.NavyWell, 0.45f) : new Color(0.55f, 0.6f, 0.75f, 0.55f));
                    var spic = se != null ? CardArt.Equip(se.Id) : null;
                    var sic = Ui.Img(srt, spic ?? W.SlotIcon(slot), se != null ? Color.white : Theme.Dim.A(0.75f), "ic");
                    float sp = spic != null ? box * 0.08f : box * 0.26f;
                    sic.rectTransform.Fill(sp, sp, sp, sp); sic.preserveAspect = true;
                    if (se != null)
                    {
                        var srow = Ui.Rect("stars", srt).Band(0, 14, 4, 4, 5);
                        Ui.Row(srow, 1, TextAnchor.MiddleCenter, null, false, false);
                        for (int q = 0; q < GradeStars(se.Grade); q++) { var st = Ui.Img(srow, Theme.S("ic_spark"), Theme.Gold, "s"); st.Pref(11, 11); st.preserveAspect = true; }
                        string cid = id, ck = k;
                        sb.OnHold = () => GearCompare(ck, cid, equipId);
                        if (slot == e.Slot) Stage.Hot["gear.cmp"] = sb;
                    }
                    if (slot == e?.Slot)
                    {
                        var fr = Ui.Img(srt, Theme.S("frame_thick", 24), Theme.Gold, "same"); fr.rectTransform.Fill(-3, -3, -3, -3);
                    }
                }
                Tw.Rise(rt, 0.12f + 0.07f * ri, 30, 0.45f, Vector2.right);
                rows.Add((k, b, hn));
                Stage.Hot["gear:" + rows.Count] = b;
                ri++;
            }

            // ── 아래 띠 ──
            var foot = Ui.Rect("foot", layer).Band(0, footH, Theme.Gutter, Theme.Gutter, 0);
            var fl = Ui.Img(layer, Theme.White, Theme.Line, "rule2"); fl.rectTransform.Band(0, 1, Theme.Gutter, Theme.Gutter, footH);
            var deckChip = Ui.Img(foot, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.92f), "deck");
            deckChip.rectTransform.At(0, 0.5f, 0, 0, 440, 58);
            var dic = Ui.Img(deckChip.transform, Theme.S("circle"), Theme.NavyWell, "disc"); dic.rectTransform.At(0, 0.5f, 6, 0, 48, 48);
            var dii = Ui.Img(dic.transform, Theme.S("ic_deck"), Theme.Ink, "ic"); dii.rectTransform.Fill(13, 13, 13, 13); dii.preserveAspect = true;
            var (cnt, avg) = DeckInfo();
            var dtx = Ui.Text(deckChip.transform, $"<color={Theme.SubTag}>덱</color> <color={Theme.GoldTag}>{cnt}</color>장    <color={Theme.SubTag}>고유 카드 평균 비용</color>  <color={Theme.GoldTag}>{(avg < 0 ? "—" : avg.ToString("0.0"))}</color>",
                Theme.FsBody, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            dtx.rectTransform.Fill(68, 0, 16, 0); dtx.textWrappingMode = TextWrappingModes.NoWrap;

            confirm = Btn.Make(foot, "낄 사도를 고르세요", BtnStyle.PillGold, () =>
            {
                string why = P.Equip(pick, equipId, true);
                if (why != null) { Toast.Show(why); return; }
                Sfx.Play("equip");
                var who = Roster.OfCore(pick).ko;
                Close(() => { Toast.Show($"{who} — 「{e.Name}」 을 꼈습니다"); done(); });
            }, Theme.FsLg);
            confirm.GetComponent<RectTransform>().At(1, 0.5f, 0, 0, 340, 64);
            var ck2 = Ui.Img(confirm.transform, Theme.S("ic_check"), Theme.Brown, "check"); ck2.rectTransform.At(0, 0.5f, 24, 0, 26, 26); ck2.preserveAspect = true;
            confirm.Label.rectTransform.Fill(56, 4, 20, 4);
            confirm.Label.enableAutoSizing = true; confirm.Label.fontSizeMin = Theme.FsSm; confirm.Label.fontSizeMax = Theme.FsLg;
            confirm.Interactable = false;
            confirm.Why = "낄 사도를 먼저 고르세요";
            Stage.Hot["gear.ok"] = confirm;
            // 애착 장비 — 그 사도가 파티에 있으면 미리 골라 둔다(누르면 바로 그 사도에게 · 다른 사도로 바꿀 수도 있음)
            if (e?.Affinity != null && rows.Exists(r => r.key == e.Affinity)) Select(e.Affinity);
            if (!bought)
            {
                int price = P.SellPrice(equipId);
                var sellB = Btn.Make(foot, null, BtnStyle.PillRose, () => Confirm($"「{e.Name}」 을 팔까요?", $"아무에게도 끼지 않고 {price} 골드를 받습니다.", "팔기", () =>
                {
                    string why = P.Sell(equipId);
                    if (why != null) { Toast.Show(why); return; }
                    Sfx.Play("coin");
                    Close(() => { Toast.Show($"「{e.Name}」 — 팔았습니다 (+{price} 골드)"); done(); });
                }, true), 0, "sell");
                var srt = sellB.GetComponent<RectTransform>(); srt.At(1, 0.5f, -356, 0, 260, 64);
                var ti = Ui.Img(srt, Theme.S("ic_trash"), Color.white, "ic"); ti.rectTransform.At(0, 0.5f, 24, 0, 26, 26); ti.preserveAspect = true;
                var tt = Ui.Title(srt, "팔기", Theme.FsLg, Color.white, TextAlignmentOptions.MidlineLeft); tt.rectTransform.At(0, 0.5f, 60, 0, 70, 40);
                var pc = Ui.Img(srt, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.6f), "price"); pc.rectTransform.At(1, 0.5f, -10, 0, 118, 44);
                var coin = Ui.Img(pc.transform, Theme.Icon("gold"), Color.white, "coin"); coin.rectTransform.At(0, 0.5f, 10, 0, 28, 28); coin.preserveAspect = true;
                var pt = Ui.Title(pc.transform, "+" + price, Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineRight); pt.rectTransform.Fill(40, 0, 14, 0);
                Stage.Hot["gear:sell"] = sellB;
            }
            else
            {
                var note = Ui.Text(foot, "산 장비는 팔 수 없습니다", Theme.FsSm, Theme.Dim, TextAlignmentOptions.MidlineRight);
                note.rectTransform.At(1, 0.5f, -356, 0, 260, 40);
            }
        }

        static int GradeStars(string g) => GradeRank(g) + 1;   // 일반 1 · 고급 2 · 희귀 3 · 전설 4

        static System.Collections.Generic.IEnumerable<(string, string)> StatPairs(Core.Stats s)
        {
            if (s == null) yield break;
            if (s.Hp != 0) yield return ("HP", (s.Hp > 0 ? "+" : "") + s.Hp);
            if (s.Atk != 0) yield return ("공격력", (s.Atk > 0 ? "+" : "") + s.Atk);
            if (s.Def != 0) yield return ("방어력", (s.Def > 0 ? "+" : "") + s.Def);
            if (s.Crit != 0) yield return ("치명", (s.Crit > 0 ? "+" : "") + s.Crit + "%");
        }

        /// <summary>효과 글의 수치를 금빛으로(태그 속 숫자는 그대로).</summary>
        static string Highlight(string t) => string.IsNullOrEmpty(t) ? t
            : System.Text.RegularExpressions.Regex.Replace(t, @"(?<![#\w=])([+\-]?\d+(?:\.\d+)?%?)(?![^<]*>)", $"<color={Theme.GoldTag}>$1</color>");

        /// <summary>덱 장수와 고유 카드(CardDef.Unique) 평균 비용 — 고유 카드가 없으면 -1.</summary>
        (int count, float avg) DeckInfo()
        {
            var cs = P.S.Deck.Select(id => P.Data.Card(id)).Where(c => c != null && c.Unique && !c.X).ToList();
            return (P.S.Deck.Count, cs.Count > 0 ? (float)cs.Average(c => c.Cost) : -1);
        }

        // 장비 자세히(돋보기)는 도감과 같은 EquipDetail(Flow.Dex.cs) — 그림 · 등급 · 능력치 · 효과 · 애착

        /// <summary>지금 낀 것 ↔ 새 것(낀 장비 칸을 길게 눌렀을 때).</summary>
        void GearCompare(string hero, string curId, string newId)
        {
            var (body, _, _) = Stage.ModalBox("compare", 900, 470, $"{Roster.OfCore(hero).ko} — 견주기", "왼쪽이 지금 낀 것 · 오른쪽이 새 것(끼면 지금 것은 팔립니다)");
            var la = Ui.Text(body, "지금", Theme.FsSm, Theme.Sub); la.rectTransform.At(0, 1, 14, 0, 200, 22);
            var lb = Ui.Text(body, "새 것", Theme.FsSm, Theme.Gold, TextAlignmentOptions.TopRight); lb.rectTransform.At(1, 1, -14, 0, 200, 22);
            var a = W.Equip(body, this, curId, 360, 290, "cur"); a.At(0, 1, 10, -28, 360, 290);
            var b = W.Equip(body, this, newId, 360, 290, "new"); b.At(1, 1, -10, -28, 360, 290);
            var arrow = Ui.Title(body, "→", Theme.Fs2xl, Theme.Gold, TextAlignmentOptions.Center); arrow.rectTransform.At(0.5f, 1, 0, -150, 80, 60);
            Stage.Hot["compare.close"] = Stage.Hot["modal.x"];
        }

        /// <summary>낀 장비 보기(지도 · 캠프의 「장비」).</summary>
        public void GearView()
        {
            var (row, close, _) = Stage.ModalBox("gearview", 1100, 500, "낀 장비", "사도 하나에 무기 · 방어구 · 장신구 한 칸씩 · 애착 장비는 그 사도가 끼면 더 셉니다");
            Ui.Row(row, Theme.Gap, TextAnchor.UpperCenter, null, true, true);
            foreach (var k in P.S.Party)
            {
                var h = Roster.OfCore(k);
                var col = Ui.Img(row, Theme.Cell, Color.white, k);
                Ui.Col(col.rectTransform, 8, TextAnchor.UpperLeft, new RectOffset(16, 16, 14, 14));
                var top = Ui.Rect("top", col.transform); top.Pref(-1, 60);
                var f = W.Face(top, h, 54); f.At(0, 0.5f, 0, 0, 54, 54);
                var nm = Ui.Title(top, h.ko, Theme.FsLg, Theme.Ink); nm.rectTransform.At(0, 0.5f, 66, 0, 230, 32);
                nm.enableAutoSizing = true; nm.fontSizeMin = 15; nm.fontSizeMax = Theme.FsLg; nm.textWrappingMode = TextWrappingModes.NoWrap;
                foreach (var slot in RunPort.Slots)
                {
                    var id = P.GearOf(k).TryGetValue(slot, out var x) ? x : null;
                    var e = id != null ? P.Data.Equip(id) : null;
                    var line = Ui.Rect(slot, col.transform); line.Pref(-1, Theme.C(64, 58));
                    var ib = Ui.Img(line, Theme.Round, (e != null ? Theme.GradeOf(e.Grade) : Theme.Dim).A(0.2f), "ib"); ib.rectTransform.At(0, 0.5f, 0, 0, 48, 48);
                    var gpic = e != null ? CardArt.Equip(e.Id) : null;
                    var ic = Ui.Img(ib.transform, gpic ?? W.SlotIcon(slot), gpic != null ? Color.white : e != null ? Theme.GradeOf(e.Grade) : Theme.Dim, "ic"); ic.rectTransform.Fill(gpic != null ? 2 : 10, gpic != null ? 2 : 10, gpic != null ? 2 : 10, gpic != null ? 2 : 10); ic.preserveAspect = true;
                    var lt = Ui.Text(line, e != null ? $"{e.Name}\n<size=82%><color={Theme.GoodTag}>{W.StatLine(P.StatsOf(id, k))}</color></size>" : $"<color={Theme.DimTag}>{slot} — 비어 있음</color>", Theme.FsSm, Theme.Ink);
                    lt.rectTransform.Fill(62, 0, 0, 0);
                }
            }
            Stage.Hot["gearview.close"] = Stage.Hot["modal.x"];
        }
    }
}
