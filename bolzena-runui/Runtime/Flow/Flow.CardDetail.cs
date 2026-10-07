using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 카드 상세(2026-10-07 사용자 — 카제나 「카드 상세」 배치) · 신탁 미리보기(카제나 「번뜩임」 창).
    //   카드 상세: 화면을 덮는 어두운 창 하나 — 위 띠(주인 얼굴 · 종류 아이콘 · 카드 분류) · 가운데 큰 카드 · 왼쪽 만들어지는 카드 ·
    //   오른쪽 낱말 상자 세로(신탁이 있는 카드는 맨 아래 「신탁」 상자) · 양옆 화살표(같은 목록의 이전 · 다음) ·
    //   아래 띠(가운데 「닫기」 · 오른쪽 아래 눈(누르는 동안 그림만) · 「신탁」 단추).
    //   덱 보기 · 도감 · 사도 상세 카드 목록 · 상점 · 보스 복제(꾹 누르기)가 같이 쓴다.
    //   축복은 여기서 보여 주지 않는다(인게임에서 알아 가는 재미 — 2026-10-07 사용자). 이미 받은 축복(카드 표식 · 「얹힌 것」 상자)만 보인다.
    //   신탁 미리보기: 제목 「신탁」 · 금빛 장식 선 · 안내 글 · 그 카드의 신탁 다섯을 얹은 카드 다섯 장 가로(좁으면 두 줄). Esc · × · 바깥 누르기로 닫는다.
    public partial class Flow
    {
        /// <summary>카드 상세 — list 를 주면 양옆 화살표로 그 목록의 이전 · 다음 카드로 넘긴다(index 가 없으면 id 로 찾는다).</summary>
        public void CardZoom(string id, IList<string> list = null, int index = -1) => CardDetail(id, list, index, true);

        void CardDetail(string id, IList<string> list, int index, bool fadeIn)
        {
            var size = Stage.Size;
            var v = P.View(id);
            var def = P.Data.Card(id);
            if (list != null && list.Count < 2) list = null;
            if (list != null && (index < 0 || index >= list.Count || list[index] != id)) index = list.IndexOf(id);
            if (index < 0) list = null;
            bool hasOracle = def != null && def.Oracles.Count > 0;

            var layer = Ui.Rect("modal cardzoom", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, Theme.Night, "dim", true); dim.rectTransform.Fill();   // 불투명(선형 색 공간에서는 조금만 비쳐도 밑 화면 글이 읽힌다 — 덱 보기와 같은 까닭)
            var halo = Ui.Img(layer, Theme.S("soft"), Theme.NatureCardOf(null).A(0), "halo");
            var lg = layer.Group();
            if (fadeIn) { lg.alpha = 0; Tw.Run(layer, 0.2f, k => { if (lg) lg.alpha = k; }); }
            bool closed = false;
            Action close = () =>
            {
                if (closed) return; closed = true;
                var g = lg; Tw.Run(layer, 0.15f, k => { if (g) g.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); });
            };
            layer.gameObject.AddComponent<BackClose>().Close = close;
            { var db = dim.gameObject.AddComponent<Btn>(); db.Bg = null; db.OnClick = close; }   // 바깥(빈 곳) 누르기

            // 눈 단추를 누르는 동안 숨길 것은 모두 ui 아래에, 그림은 art 에
            var ui = Ui.Rect("ui", layer).Fill();
            var uig = ui.Group();
            var art = Ui.Rect("art", layer).Fill(); art.gameObject.SetActive(false);

            float topH = Theme.C(78, 66), botH = Theme.C(100, 86), arrowW = list != null ? Theme.C(88, 80) : 24;
            float midH = size.y - topH - botH - 16;
            float cw = Mathf.Min(Theme.C(400, 340), (midH - 8) / 1.4f), ch = cw * 1.4f;
            float midY = (botH - topH) / 2;   // 가운데 칸의 가운데(화면 가운데에서)
            halo.rectTransform.At(0.5f, 0.5f, 0, midY, cw * 2.4f, cw * 2.4f);
            halo.color = W.CardTint(this, v, null).A(0.22f);

            // ── 위 띠 — 주인 얼굴 · 종류 아이콘 · 분류 글 · (목록이면) 몇째 ──
            var top = Ui.Rect("top", ui).Band(1, topH, Theme.Gutter + 8, Theme.Gutter + 8, 0);
            float x = 0;
            string ownerKey = v?.Def.Hero ?? v?.Owner;
            if (ownerKey != null && Roster.OfCore(ownerKey) != null)
            {
                float fs = topH - 22;
                var face = W.Face(top, Roster.OfCore(ownerKey), fs, true, "owner"); face.At(0, 0.5f, 0, 0, fs, fs);
                x += fs + 14;
            }
            string type = v?.Type ?? def?.Type ?? "스킬";
            var tic = Ui.Img(top, W.TypeIcon(type), W.TypeColor(type), "typeicon"); tic.rectTransform.At(0, 0.5f, x, 0, 30, 30); tic.preserveAspect = true;
            x += 40;
            var cat = Ui.Title(top, CardClass(v, def) + $"  <size=70%><color={Theme.SubTag}>{W.TypeLabel(type)}</color></size>", Theme.FsXl, Theme.Ink, TextAlignmentOptions.MidlineLeft, "class");
            cat.rectTransform.At(0, 0.5f, x, 0, 700, topH); cat.textWrappingMode = TextWrappingModes.NoWrap;
            if (list != null)
            {
                var cnt = Ui.Title(top, $"{index + 1} <color={Theme.SubTag}>/ {list.Count}</color>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineRight, "count");
                cnt.rectTransform.At(1, 0.5f, 0, 0, 200, topH);
            }
            var rule = Ui.Img(ui, Theme.White, Theme.Line, "rule"); rule.rectTransform.Band(1, 1, Theme.Gutter, Theme.Gutter, -topH);

            // ── 가운데 큰 카드 ──
            var card = W.Card(ui, this, id, cw, "zoom"); card.At(0.5f, 0.5f, 0, midY, cw, ch);
            if (fadeIn) Tw.Pop(card, 0.02f, 0.9f, 0.3f);
            float side = (size.x - cw) / 2 - arrowW - 26;   // 카드 한쪽 옆에 남는 폭
            float sideTop = size.y / 2 - topH - 12, sideBot = -size.y / 2 + botH + 8;   // 가운데 칸 위 · 아래(화면 가운데 기준)

            // 낱말 — 키워드 · 상태 · 고유 효과 · 생성 카드(카드 글의 밑줄 낱말과 같은 판)
            var terms = v != null ? CardTerms.Of(P.Data, P.Text, v, P.Text.Card(v)) : new List<CardTerms.Term>();
            var made = terms.Where(t => t.IsCard).ToList();
            float gw = Mathf.Min(Theme.C(170, 150), side - 10);
            bool leftCards = made.Count > 0 && gw >= 110;
            var boxes = leftCards ? terms.Where(t => !t.IsCard).ToList() : terms.Select(t => !t.IsCard ? t : CardBoxTerm(t)).ToList();

            // ── 카드 왼쪽 — 만들어지는 카드(작게) ──
            if (leftCards)
            {
                var la = Ui.Rect("made", ui);
                la.anchorMin = la.anchorMax = new Vector2(0.5f, 0.5f); la.pivot = new Vector2(1, 0.5f);
                la.sizeDelta = new Vector2(gw + 16, sideTop - sideBot); la.anchoredPosition = new Vector2(-cw / 2 - 26, (sideTop + sideBot) / 2);
                var lc = Ui.Scroll(la, out _);
                Ui.Col(lc, 14, TextAnchor.UpperRight, new RectOffset(4, 4, 4, 8), false, false);
                // 가운데 맞춤 — 몇 장 안 되면 카드 높이 가운데에 모은다
                float one = 30 + gw * 1.4f, need = made.Count * one + (made.Count - 1) * 14;
                if (need < sideTop - sideBot) lc.GetComponent<VerticalLayoutGroup>().padding.top = (int)Mathf.Max(4, (sideTop - sideBot - need) / 2);
                foreach (var t in made)
                {
                    var hold = Ui.Rect("made " + t.Name, lc); hold.Pref(gw, one);
                    var cap = Ui.Text(hold, $"<color={Theme.SubTag}>{t.Rel ?? "만들어지는 카드"}</color>", Theme.FsCap, Theme.Sub, TextAlignmentOptions.BottomLeft);
                    cap.rectTransform.Band(1, 26, 2, 0, 0); cap.textWrappingMode = TextWrappingModes.NoWrap;
                    var mc = W.Card(hold, this, t.CardId, gw, "termcard"); mc.At(0, 1, 0, -30, gw, gw * 1.4f);
                    var mb = mc.gameObject.AddComponent<Btn>(); var cid = t.CardId; mb.OnClick = () => CardZoom(cid);
                }
            }

            // ── 카드 오른쪽 — 낱말 상자 세로 · 얹힌 것 · 맨 아래 신탁 ──
            float bw = Mathf.Min(Theme.C(430, 380), side);
            var ra = Ui.Rect("terms", ui);
            ra.anchorMin = ra.anchorMax = new Vector2(0.5f, 0.5f); ra.pivot = new Vector2(0, 0.5f);
            ra.sizeDelta = new Vector2(bw + 12, sideTop - sideBot); ra.anchoredPosition = new Vector2(cw / 2 + 26, (sideTop + sideBot) / 2);
            var rc = Ui.Scroll(ra, out _);
            Ui.Col(rc, 10, TextAnchor.UpperLeft, new RectOffset(0, 8, 4, 8), true, false);
            void Box(CardTerms.Term t)
            {
                var holder = Ui.Rect("term " + t.Name, rc);
                var bx = TermPop.Box(holder, t, bw);
                holder.Pref(-1, bx.sizeDelta.y);
            }
            foreach (var t in boxes) Box(t);
            var mark = P.Mark(id);
            if (mark.Any)
            {
                var got = new List<string>();
                if (mark.Oracle != null) got.Add($"<color={Theme.GoldTag}>신탁</color> 이번 모험에서 신탁으로 바뀐 카드입니다 — 비용 아래 금빛 별 마크가 그 표시입니다.");   // 신탁 이름은 보이지 않는다(2026-10-07)
                if (mark.Blessed) got.Add($"<color=#D9C7FF>축복</color> {mark.BlessText} — 비용 아래 연보라 날개 마크와 본문 맨 아래 줄이 그 표시입니다.");   // 받은 축복만(받을 수 있는 축복 목록은 보이지 않는다)
                if (mark.Copy) got.Add($"<color=#C7DBFF>{Core.CardMark.COPY_LINE}</color> — 원본이 나중에 받는 신탁 · 축복은 따라오지 않습니다.");
                Box(new CardTerms.Term { Name = "이 카드에 얹힌 것", Kind = mark.Copy ? "복제할 때 모습 그대로" : null, Body = string.Join("\n", got) });
            }
            if (hasOracle)
                Box(new CardTerms.Term { Name = "신탁", Hero = true, Kind = "오른쪽 아래 「신탁」 단추",
                    Body = "이 카드에는 신탁이 나올 수 있습니다. 신탁이 나오면 카드의 비용 · 효과가 바뀝니다." });
            if (boxes.Count == 0 && !mark.Any && !hasOracle)
            {
                var none = Ui.Text(rc, "설명할 낱말이 없는 카드입니다.", Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft); none.Pref(-1, 40);
            }

            // ── 양옆 화살표 — 같은 목록의 이전 · 다음 ──
            if (list != null)
            {
                void Go(int d)
                {
                    int ni = (index + d + list.Count) % list.Count;
                    if (layer) Destroy(layer.gameObject);
                    CardDetail(list[ni], list, ni, false);
                }
                float ab = Theme.C(64, 60);
                var prev = Btn.Icon(ui, Theme.S("ic_left"), () => Go(-1), ab, "prev"); prev.GetComponent<RectTransform>().At(0, 0.5f, Theme.Gutter, midY, ab, ab);
                var next = Btn.Icon(ui, Theme.S("ic_left"), () => Go(1), ab, "next"); next.GetComponent<RectTransform>().At(1, 0.5f, -Theme.Gutter, midY, ab, ab);
                next.transform.Find("icon").localRotation = Quaternion.Euler(0, 0, 180);
                Stage.Hot["zoom.prev"] = prev; Stage.Hot["zoom.next"] = next;
            }
            else { Stage.Hot.Remove("zoom.prev"); Stage.Hot.Remove("zoom.next"); }

            // ── 아래 띠 — 가운데 「닫기」 · 오른쪽 아래 눈 · 신탁 ──
            var bot = Ui.Rect("bottom", ui).Band(0, botH, Theme.Gutter, Theme.Gutter, 0);
            var brule = Ui.Img(ui, Theme.White, Theme.Line, "rule2"); brule.rectTransform.Band(0, 1, Theme.Gutter, Theme.Gutter, botH);
            var cb = Btn.Make(bot, "닫기", BtnStyle.PillDark, close, Theme.FsLg, "close");
            cb.GetComponent<RectTransform>().At(0.5f, 0.5f, 0, 0, Theme.C(300, 280), Theme.C(62, 58));
            Stage.Hot["zoom.close"] = cb; Stage.Hot["modal.x"] = cb;
            float rx = 0;
            if (hasOracle)
            {
                var ob = Btn.Make(bot, null, BtnStyle.PillGold, () => OraclePreview(def.Id), 0, "oracle");
                var ort = ob.GetComponent<RectTransform>(); ort.At(1, 0.5f, 0, 0, Theme.C(190, 176), Theme.C(62, 58));
                var oi = Ui.Img(ort, Theme.S("ic_spark"), Theme.Brown, "ic"); oi.rectTransform.At(0, 0.5f, 22, 0, 28, 28); oi.preserveAspect = true;
                var ol = Ui.Title(ort, "신탁", Theme.FsLg, Theme.Brown, TextAlignmentOptions.Center); ol.rectTransform.Fill(50, 0, 18, 0);
                Stage.Hot["zoom.oracle"] = ob;
                rx += Theme.C(190, 176) + 16;
            }
            else Stage.Hot.Remove("zoom.oracle");
            var sprite = ArtOf(id, v);
            {
                float es = Theme.C(58, 56);
                var eye = Btn.Icon(bot, Theme.S("ic_eye"), null, es, "eye"); eye.GetComponent<RectTransform>().At(1, 0.5f, -rx, 0, es, es);
                Image pic = null;
                eye.gameObject.AddComponent<HoldEye>().OnHold = on =>
                {
                    if (!layer) return;
                    uig.alpha = on ? 0 : 1;
                    art.gameObject.SetActive(on);
                    if (!on || pic != null) return;
                    if (sprite != null)
                    {
                        pic = Ui.Img(art, sprite, Color.white, "pic"); pic.preserveAspect = true;
                        // 화면에 맞추되 원본의 2.5배는 넘기지 않는다(작은 아이콘 그림이 뭉개지지 않게)
                        float sx = sprite.rect.width, sy = sprite.rect.height;
                        float fit = Mathf.Min((size.x - Theme.Gutter * 2) / sx, (size.y - 24) / sy, 2.5f * size.y / Mathf.Max(1, Screen.height));   // 캔버스 단위 = 화면 픽셀 × size.y / Screen.height
                        pic.rectTransform.At(0.5f, 0.5f, 0, 0, sx * fit, sy * fit);
                    }
                    else
                    {   // 그림이 없는 카드 — 카드를 크게만
                        float bh = size.y - 40, bw2 = bh / 1.4f;
                        var big = W.Card(art, this, id, bw2, "termcard"); big.At(0.5f, 0.5f, 0, 0, bw2, bh);
                        pic = big.GetComponent<Image>();
                    }
                };
                Stage.Hot["zoom.eye"] = eye;
            }
        }

        /// <summary>카드 분류 — 고유 카드 · 기본 카드 · 교주 카드(등급) · 상태 · 저주.</summary>
        static string CardClass(Core.CardView v, Core.CardDef def)
        {
            if (v == null) return def?.Name ?? "카드";
            if (v.IsCurse) return "저주 카드";
            if (v.IsStatus) return "상태 카드";
            if (v.Unique) return "고유 카드";
            if (v.Def.Hero != null) return "기본 카드";
            return string.IsNullOrEmpty(v.Def.Grade) ? "교주 카드" : $"교주 카드 · {v.Def.Grade}";
        }

        /// <summary>왼쪽에 둘 자리가 없을 때 — 생성 카드 낱말을 글 상자로(종류 · 비용 · 효과).</summary>
        CardTerms.Term CardBoxTerm(CardTerms.Term t0)
        {
            var cv = P.View(t0.CardId);
            return new CardTerms.Term { Name = t0.Name, CardId = t0.CardId, Rel = t0.Rel, Kind = t0.Rel != null ? "카드 · " + t0.Rel : "만들어지는 카드",
                Body = cv != null ? $"<color={Theme.SubTag}>{W.TypeLabel(cv.Type)} · 비용 {(cv.X ? "X" : cv.Cost.ToString())}</color>  {P.Text.Card(cv)}" : null };
        }

        /// <summary>눈 단추가 보여 줄 그림 — 카드 그림 창과 같은 것의 원본(스탠딩 전신 · 장면 · 사물 · 아이콘). 없으면 null.</summary>
        static Sprite ArtOf(string id, Core.CardView v)
        {
            var hero = v?.Def.Hero != null ? Roster.OfCore(v.Def.Hero) : null;
            switch (CardArt.Of(id, hero?.art, v != null && v.Unique))
            {
                case CardArt.Kind.Standing: return CardArt.Standing(hero.art) ?? CardArt.Card(hero.art);
                case CardArt.Kind.Pic: { var pn = CardArt.PicOf(id); return CardArt.Obj(pn, out var op) ? op.Sprite : CardArt.Pic(pn); }
                case CardArt.Kind.Icon: return CardArt.Icon(CardArt.IconOf(id));
                default: return hero?.Icon;
            }
        }

        /// <summary>누르는 동안만 켜지는 단추(눈 — 그림만 보기).</summary>
        sealed class HoldEye : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            public Action<bool> OnHold;
            bool on;
            public void OnPointerDown(PointerEventData e) { if (!on) { on = true; OnHold?.Invoke(true); } }
            public void OnPointerUp(PointerEventData e) { if (on) { on = false; OnHold?.Invoke(false); } }
            void OnDisable() { if (on) { on = false; OnHold?.Invoke(false); } }
            /// <summary>자동 데모 — 누른 채로 두기 · 떼기.</summary>
            public void Set(bool v) { if (v) OnPointerDown(null); else OnPointerUp(null); }
        }

        /// <summary>자동 데모 — 카드 상세의 눈 단추를 누른 채로 두거나 뗀다.</summary>
        public void DemoEye(bool held)
        {
            if (Stage.Hot.TryGetValue("zoom.eye", out var b) && b != null) b.GetComponent<HoldEye>()?.Set(held);
        }

        /// <summary>
        /// 신탁 미리보기(카제나 「번뜩임」 창) — 제목 · 금빛 장식 선 · 안내 글, 그 카드의 신탁 다섯을 얹은 모습을 가로로.
        /// 축복은 넣지 않는다. 화면이 좁으면 카드를 줄이다가 두 줄(3 + 2)로.
        /// </summary>
        public void OraclePreview(string cardId)
        {
            var def = P.Data.Card(cardId);
            if (def == null || def.Oracles.Count == 0) return;
            var size = Stage.Size;
            var layer = Ui.Rect("modal oracles", Stage.ModalLayer).Fill();
            var dim = Ui.Img(layer, Theme.White, new Color(0.01f, 0.015f, 0.04f, 0.97f), "dim", true); dim.rectTransform.Fill();
            var lg = layer.Group(); lg.alpha = 0;
            Tw.Run(layer, 0.2f, k => { if (lg) lg.alpha = k; });
            bool closed = false;
            Action close = () =>
            {
                if (closed) return; closed = true;
                var g = lg; Tw.Run(layer, 0.15f, k => { if (g) g.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); });
            };
            layer.gameObject.AddComponent<BackClose>().Close = close;
            { var db = dim.gameObject.AddComponent<Btn>(); db.Bg = null; db.OnClick = close; }

            float headY = Theme.C(34, 22);
            var title = Ui.Title(layer, "신탁", Theme.Fs2xl, Theme.Gold, TextAlignmentOptions.Center, "title");
            title.rectTransform.At(0.5f, 1, 0, -headY, 600, 56); title.Outline(0.2f);
            // 금빛 장식 선 — 가운데 마름모, 양쪽으로 옅어지는 선
            float ly = -headY - 62;
            foreach (var sgn in new[] { -1, 1 })
            {
                var ln = Ui.Img(layer, Theme.White, Theme.Gold.A(0.7f), "deco"); ln.rectTransform.At(0.5f, 1, sgn * 130, ly, 220, 1.6f);
                var tip = Ui.Img(layer, Theme.White, Theme.Gold.A(0.35f), "deco2"); tip.rectTransform.At(0.5f, 1, sgn * 290, ly, 100, 1.2f);
            }
            var dot = Ui.Img(layer, Theme.White, Theme.Gold, "decodot");
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0.5f, 1); dot.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            dot.rectTransform.sizeDelta = new Vector2(10, 10); dot.rectTransform.anchoredPosition = new Vector2(0, ly - 0.8f);
            dot.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var guide = Ui.Text(layer, $"이 카드에는 아래의 신탁이 나올 수 있습니다.", Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center, false, "guide");
            guide.rectTransform.At(0.5f, 1, 0, ly - 14, 900, 34); guide.Outline(0.2f);
            var x = Btn.Icon(layer, Theme.S("ic_x"), close, 56, "close"); x.GetComponent<RectTransform>().At(1, 1, -Theme.Gutter, -18, 56, 56);
            Stage.Hot["oracles.close"] = x;

            // 카드 자리 — 한 줄에 다섯이 들면 한 줄, 너무 작아지면 두 줄
            int n = def.Oracles.Count;
            float topUsed = -(ly - 14 - 34) + 24, gap = Theme.C(26, 22), labelH = 0;
            float availW = size.x - Theme.Gutter * 2 - 40, availH = size.y - topUsed - Theme.C(36, 24);
            float cwOne = Mathf.Min(Theme.C(290, 300), (availW - gap * (n - 1)) / n, (availH - labelH) / 1.4f);
            int rows = 1, perRow = n;
            if (cwOne < 180 && n > 3)
            {
                rows = 2; perRow = Mathf.CeilToInt(n / 2f);
                cwOne = Mathf.Min(Theme.C(290, 300), (availW - gap * (perRow - 1)) / perRow, (availH - gap) / 2 / 1.4f);
            }
            float cw = cwOne, ch = cw * 1.4f;
            float blockH = rows * ch + (rows - 1) * gap;
            float y0 = -topUsed - Mathf.Max(0, (availH - blockH) / 2);   // 남는 높이 가운데
            for (int i = 0; i < n; i++)
            {
                int r = i / perRow, c = i % perRow;
                int inRow = r == rows - 1 ? n - perRow * (rows - 1) : perRow;
                float rowW = inRow * cw + (inRow - 1) * gap;
                var view = P.Data.View(def.Id, i + 1);
                var card = W.Card(layer, this, def.Id, cw, "oracle" + (i + 1), view, noFace: true);
                card.At(0.5f, 1, -rowW / 2 + cw / 2 + c * (cw + gap), y0 - r * (ch + gap), cw, ch);
                Tw.Pop(card, 0.06f + i * 0.06f, 0.85f, 0.32f);
            }
        }
    }
}
