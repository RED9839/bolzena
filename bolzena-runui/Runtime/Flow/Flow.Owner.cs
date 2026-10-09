using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 교주 카드 주인 고르기 — 교주 카드를 얻을 때(상점 · 보상 · 이벤트) 파티 셋 가운데 누구 덱에 넣을지 고르는 창.
    //   왼쪽: 얻은 카드(금빛 중립) · 오른쪽: 사도 셋(스탠딩 상반신 · 성격 색 판 · 공격력/방어력 · 이 카드 위력 미리보기 · 「덱에 넣기」)
    //   위력 = 그 사도의 지금 공격력 · 방어력(기본 + 장비)으로 카드 효과의 배율을 푼 값. 셋 가운데 가장 큰 값은 초록.
    //   고르면 그 사도 성격 색으로 틀을 바꾼 카드가 한 번 튀고 닫힌다.
    //   core: 카드를 얻은 뒤 Run.PendingNeutral 이 있으면 이 창 → Run.AssignNeutral(사도) — 줄이 빌 때까지 하나씩(PickOwners).
    public partial class Flow
    {
        /// <summary>주인을 기다리는 교주 카드(PendingNeutral)가 빌 때까지 하나씩 고르게 하고 done.</summary>
        public void PickOwners(Action done)
        {
            var id = P.PendingNeutral;
            if (id == null || P.S.Party.Count == 0) { done?.Invoke(); return; }
            OwnerWindow(id, key =>
            {
                var why = P.AssignNeutral(key);
                if (why != null) Toast.Show(why);
                else Toast.Show($"「{P.Data.Card(id)?.Name}」 — {Roster.OfCore(key)?.ko} 덱에 넣었습니다");
                PickOwners(done);
            });
        }

        /// <summary>주인 고르기 창의 한 줄 — 교주 카드는 위력, 선물(· 상태) 카드는 덱 묶음만 정한다(전투 효과는 그대로). 저주는 이 창에 오지 않는다(덱의 「저주」 묶음).</summary>
        string OwnerSub(string cardId, string name)
        {
            var c = P.Data.Card(cardId);
            if (c == null || c.Neutral) return $"교주 카드 「{name}」 — 넣은 사도의 공격력 · 방어력으로 위력이 정해지고, 카드 틀이 그 사도 성격 색이 됩니다";
            string kind = c.IsStatusCard ? "상태 카드" : "선물 카드";
            return $"{kind} 「{name}」 — 고른 사도의 카드 묶음 맨 끝에 들어갑니다(효과는 누구에게 넣어도 같습니다)";
        }

        /// <summary>교주 카드(id)를 넣을 사도를 고르는 창 — 화면만(덱에 넣기는 chosen 이). view 를 주면 그 모습(시범의 가짜 카드), 안 주면 사도마다 core 의 그 사도 카드 모습.</summary>
        public void OwnerWindow(string cardId, Action<string> chosen, Core.CardView view = null, IList<string> party = null)
        {
            Action<string> done = chosen;
            var v = view ?? P.View(cardId);
            var keys = (party ?? P.S?.Party ?? new List<string>()).Take(3).ToList();
            if (v == null || keys.Count == 0) { done?.Invoke(keys.FirstOrDefault()); return; }
            float H = Theme.C(680, 640);
            var (body, close, _) = Stage.ModalBox("ownerpick", 1240, H, "누구 덱에 넣겠습니까?", OwnerSub(cardId, v.Name), false, null, 0, false);

            // 왼쪽 — 얻은 카드
            float cardW = Theme.C(220, 196);
            var left = Ui.Rect("got", body); left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0, 1); left.pivot = new Vector2(0, 0.5f);
            left.sizeDelta = new Vector2(cardW + 24, 0); left.anchoredPosition = Vector2.zero;
            var cardHost = Ui.Rect("cardhost", left).At(0.5f, 0.5f, 0, 18, cardW, cardW * 1.4f);
            var card = W.Card(cardHost, this, cardId, cardW, "card", v);
            bool picked = false;
            card.At(0.5f, 0.5f, 0, 0, cardW, cardW * 1.4f);
            var cap = Ui.Text(left, "얻은 카드", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
            cap.rectTransform.At(0.5f, 0.5f, 0, 18 + cardW * 0.7f + 22, cardW, 24);
            var arrow = Ui.Img(body, Theme.S("ic_play"), Theme.Gold.A(0.8f), "arrow");
            arrow.rectTransform.At(0, 0.5f, cardW + 30, 18, 22, 22); arrow.preserveAspect = true;

            // 위력 — 사도마다 줄(이름 · 값 · 몇 번)
            var rows = keys.Select(k => { var (a, d) = P.AtkDef(k); return (k, a, d, lines: PowerLines(view ?? P.ViewAs(cardId, k) ?? v, a, d)); }).ToList();
            var best = new Dictionary<string, int>();
            foreach (var r in rows) foreach (var l in r.lines) best[l.label] = Mathf.Max(best.TryGetValue(l.label, out var b) ? b : 0, l.total);

            // 오른쪽 — 사도 셋
            var right = Ui.Rect("heroes", body).Fill(cardW + 62, 0, 0, 0);
            // 칸 자리는 손으로 — 몸통 크기(창은 캔버스에 맞춰 줄어든다)에서 셋으로 나눈다(배치 묶음은 등장 연출과 다툰다)
            float bodyW = Mathf.Min(1240, Stage.Size.x - 32) - 40, bodyH = Mathf.Min(H, Stage.Size.y - 24) - 86 - 12 - 18;
            float gap = 16, colW = (bodyW - cardW - 62 - gap * (rows.Count - 1)) / rows.Count;
            int i = 0;
            float tallest = StandingFit.Tallest(keys.Select(k => Roster.OfCore(k)?.art));   // 셋 같은 배율 · 바닥선(안 B — 작은 사도는 작게)
            foreach (var r in rows)
            {
                var key = r.k;
                var hero = Roster.OfCore(key);
                var nat = Theme.NatureCardOf(P.Data.Hero(key)?.Nature ?? hero?.nature);
                var b = Btn.Make(right, null, BtnStyle.Cell, null, 0, "owner " + key);
                var rt = b.GetComponent<RectTransform>();
                rt.At(0, 0.5f, i * (colW + gap), 0, colW, bodyH);
                Ui.Img(rt, Theme.Frame, nat.A(0.85f), "rim").rectTransform.Fill();

                // 스탠딩 상반신 — 성격 색이 위에서 번진다
                float picH = Theme.C(276, 214);
                var well = Ui.Img(rt, Theme.Round, Color.Lerp(Theme.NavyWell, nat, 0.32f), "well"); well.pixelsPerUnitMultiplier = 2f;
                well.rectTransform.Band(1, picH, 8, 8, -8);
                well.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var glow = Ui.Img(well.rectTransform, Theme.S("fade_top"), nat.A(0.55f), "glow"); glow.rectTransform.Band(1, picH * 0.7f);
                // 스탠딩 스파인을 실제로 — 다리 기준, 상반신이 창에(발은 창 아래 · 머리 뼈는 위 2할 아래)
                float wellW = colW - 16;   // 창 너비(Band 8 · 8 안쪽)
                var liveSt = hero != null ? SpineUi.Standing(well.rectTransform, hero.art, wellW, picH, StandMode.Upper, tallest) : null;
                var sp = liveSt == null && hero != null ? CardArt.Upper(hero.art, 1.2f, 0.5f) : null;
                if (liveSt != null) { }
                else if (sp != null) { var im = Ui.Img(well.rectTransform, sp, Color.white, "pic"); im.preserveAspect = true; im.rectTransform.Fill(0, 0, 0, 6); }
                else { var f = W.Face(well.rectTransform, hero, picH * 0.7f); f.At(0.5f, 0.5f, 0, 0, picH * 0.7f, picH * 0.7f); }
                var shade = Ui.Img(well.rectTransform, Theme.S("fade_down"), new Color(0.01f, 0.02f, 0.06f, 0.97f), "shade"); shade.rectTransform.Band(0, picH * 0.58f);
                var nm = Ui.Title(well.rectTransform, hero?.ko ?? key, Theme.FsXl, Color.white, TextAlignmentOptions.BottomLeft);
                nm.rectTransform.Band(0, 40, 16, 12, 30); nm.Outline(0.3f); nm.textWrappingMode = TextWrappingModes.NoWrap;
                nm.enableAutoSizing = true; nm.fontSizeMin = Theme.FsMd; nm.fontSizeMax = Theme.FsXl;
                var sub = Ui.Text(well.rectTransform, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(hero?.nature))}>{hero?.nature}</color> · {hero?.role}", Theme.FsSm, Theme.Sub, TextAlignmentOptions.BottomLeft);
                sub.rectTransform.Band(0, 24, 16, 12, 8); sub.Outline(0.3f);
                var bar = Ui.Img(well.rectTransform, Theme.White, nat, "band"); bar.rectTransform.Band(1, 4);
                // 이 사도의 고유 효과 · 패시브 · 고학년 판(수치가 다 든 글 · 길면 스크롤). 창 위 오른쪽(칸 단추와 따로 눌린다)
                if (P.Data.Hero(key) != null)
                {
                    var tc = TraitsChip(rt, key, "고유 효과", "traits");
                    var trt = tc.GetComponent<RectTransform>(); trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(1, 1); trt.anchoredPosition = new Vector2(-16, -16);
                    Stage.Hot["owner.traits:" + key] = tc;
                }

                // 공격력 · 방어력
                float y = -8 - picH - 12;
                var st = Ui.Rect("stats", rt).Band(1, 34, 14, 14, y);
                Ui.Row(st, 8, TextAnchor.MiddleCenter, null, true, false);
                StatChip(st, "ic_swords", "공격력", r.a, new Color(1f, 0.55f, 0.58f));
                StatChip(st, "ic_shield", "방어력", r.d, new Color(0.55f, 0.8f, 1f));
                y -= 34 + 14;

                // 이 카드 위력
                var lab = Ui.Text(rt, "이 카드 위력", Theme.FsCap, Theme.Sub, TextAlignmentOptions.MidlineLeft);
                lab.rectTransform.Band(1, 20, 18, 14, y);
                var rule = Ui.Img(rt, Theme.White, Theme.Line, "rule"); rule.rectTransform.Band(1, 1, 112, 18, y - 10);
                y -= 26;
                if (r.lines.Count == 0)
                {
                    var none = Ui.Text(rt, "공격력 · 방어력과 상관없는 카드 — 누구에게 넣어도 같습니다", Theme.FsSm, Theme.Dim, TextAlignmentOptions.TopLeft);
                    none.rectTransform.Band(1, 50, 18, 14, y);
                }
                foreach (var l in r.lines.Take(3))
                {
                    bool top = rows.Count > 1 && l.total > 0 && l.total >= best[l.label] && rows.Count(o => o.lines.Any(x => x.label == l.label && x.total == l.total)) < rows.Count;
                    var row = Ui.Rect("pow " + l.label, rt).Band(1, 40, 18, 14, y);
                    var ic = Ui.Img(row, Theme.S(l.icon), l.color, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 0.5f, 0, 0, 22, 22);
                    var lt = Ui.Text(row, l.label, Theme.FsBody, Theme.Sub, TextAlignmentOptions.MidlineLeft); lt.rectTransform.Fill(30, 0, 0, 0);
                    var num = Ui.Title(row, l.text, Theme.FsXl, top ? Theme.Good : Theme.Gold, TextAlignmentOptions.MidlineRight);
                    num.rectTransform.Fill(80, 0, 0, 0); num.textWrappingMode = TextWrappingModes.NoWrap; num.Outline(0.15f);
                    y -= 40;
                }

                // 고르기 단추(칸 전체를 눌러도 같다)
                var go = Btn.Make(rt, null, BtnStyle.PillGold, null, 0, "ownergo");
                go.GetComponent<RectTransform>().Band(0, 54, 16, 16, 14);
                var gl = Ui.Title(go.transform, "이 사도 덱에 넣기", Theme.FsMd, Theme.Brown, TextAlignmentOptions.Center); gl.rectTransform.Fill();
                void Choose()
                {
                    // 고른 사도 성격 색으로 카드를 다시 그려 한 번 튀게 하고 닫는다 — 두 번 눌러도(칸 · 단추) 한 번만, 창이 먼저 닫혔으면 넣기만
                    if (picked) return; picked = true;
                    if (cardHost == null) { done?.Invoke(key); return; }   // 창이 이미 닫혔다(닫기를 또 부르면 없는 판을 만진다) — 넣기만
                    if (card != null) UnityEngine.Object.Destroy(card.gameObject);
                    var nc = W.Card(cardHost, this, cardId, cardW, "card", v, key); nc.At(0.5f, 0.5f, 0, 0, cardW, cardW * 1.4f);
                    card = nc;
                    Tw.Pop(nc, 0, 0.85f, 0.3f);
                    W.Select(b, true);
                    Tw.Run(rt, 0.55f, _ => { }, Tw.Linear, 0, () => { close(); done?.Invoke(key); });
                }
                b.OnClick = Choose; go.OnClick = Choose;
                Stage.Hot["owner:" + key] = go;
                Tw.Rise(rt, 0.06f * i++, 22, 0.35f, Vector2.up);
            }
        }

        static void StatChip(RectTransform row, string icon, string label, int value, Color c)
        {
            var bg = Ui.Img(row, Theme.Pill, Theme.NavyWell.A(0.85f), "chip " + label);
            bg.Pref(-1, 32, 1);
            var ic = Ui.Img(bg.transform, Theme.S(icon), c, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 0.5f, 12, 0, 18, 18);
            var t = Ui.Text(bg.transform, $"<color={Theme.SubTag}>{label}</color>  <b>{value}</b>", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.Fill(36, 0, 8, 0); t.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>카드 위력 미리보기 — 피해 · 방어 · 실드 · 회복의 배율을 공격력 · 방어력으로 푼 값(겹 · 조건 덤은 빼고).</summary>
        public static List<(string label, string text, int total, string icon, Color color)> PowerLines(Core.CardView v, int atk, int def)
        {
            var o = new List<(string, string, int, string, Color)>();
            var sum = new Dictionary<string, (int per, int hits, bool x, string icon, Color c)>();
            void Walk(List<Core.Fx> fx)
            {
                if (fx == null) return;
                foreach (var f in fx)
                {
                    if (f == null) continue;
                    if (f.Ratio > 0 && f.OfEvent <= 0)
                    {
                        string k = null; int baseV = 0; string ic = "ic_spark"; Color c = Theme.Ink;
                        if (f.K == Core.FxK.Dmg) { k = f.Base == "def" ? "방어 기반 피해" : f.Dot ? "지속 피해" : "피해"; baseV = f.Base == "def" ? def : atk; ic = "ic_swords"; c = new Color(1f, 0.55f, 0.58f); }
                        else if (f.K == Core.FxK.Block) { k = "방어"; baseV = def; ic = "ic_shield"; c = new Color(0.55f, 0.8f, 1f); }
                        else if (f.K == Core.FxK.Shield) { k = "파티 실드"; baseV = def; ic = "ic_shield"; c = new Color(0.55f, 0.8f, 1f); }
                        else if (f.K == Core.FxK.Heal) { k = "회복"; baseV = def; ic = "ic_heart"; c = Theme.Good; }
                        if (k != null)
                        {
                            int per = Mathf.RoundToInt((float)(baseV * f.Ratio));
                            int hits = f.K == Core.FxK.Dmg ? f.HitsOr1 : 1;
                            if (sum.TryGetValue(k, out var s) && s.hits == hits && !f.XHits) sum[k] = (s.per + per, hits, s.x, ic, c);
                            else if (!sum.ContainsKey(k)) sum[k] = (per, hits, f.K == Core.FxK.Dmg && f.XHits, ic, c);
                        }
                    }
                    Walk(f.Then);
                }
            }
            Walk(v?.Fx);
            foreach (var kv in sum)
            {
                var s = kv.Value;
                string text = s.x ? $"{s.per} × X" : s.hits > 1 ? $"{s.per} × {s.hits}" : s.per.ToString();
                o.Add((kv.Key, text, s.per * Mathf.Max(1, s.hits), s.icon, s.c));
            }
            return o;
        }
    }
}
