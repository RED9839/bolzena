using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 프론티어 기록 보기(2026-10-18 사용자 「프론티어 기록 확인할 수 있게」 — 내 기록 + 남의 편성 · 덱).
    //   내 기록(입구 「기록 보기」) — GET /api/frontier/history: 도전마다 한 줄(날 · 편성 사도 · 점수 · 끝 · 검증 상태 · 거절 까닭), 20줄씩 「더 보기」.
    //   줄을 누르면 「그때의 편성」 — GET /api/frontier/attempt/:id: 시작 때 서버가 떠 둔 사도 셋의 슬롯(카드 · 장비 · 성장).
    //   랭킹 줄(입구 랭킹 칸 · 랭킹판)을 누르면 그 사람의 편성 — 서버는 검증된 시도만 준다(그 밖은 404). 사도 카드를 누르면 저장 데이터 창과 같은 판(읽기 전용 — 장착 · 지우기 없음).
    public partial class Flow
    {
        readonly List<JObject> fHist = new List<JObject>();
        bool fHistMore;
        int fHistPage;

        /// <summary>내 기록 화면에 보인 줄 수 · 마지막으로 연 「그때의 편성」 (데모 단언).</summary>
        public int HistoryRows { get; private set; }
        /// <summary>내 기록 줄의 상태(차례대로 — 데모가 검증된 줄을 고르고 「검증 안 함」 을 확인한다).</summary>
        public List<string> HistoryStatus => fHist?.Select(x => (string)x["status"]).ToList() ?? new List<string>();
        public JObject AttemptShown { get; private set; }
        /// <summary>마지막으로 연 읽기 전용 저장 데이터 창의 사도 · 카드 수(데모 단언).</summary>
        public string SlotReadHero { get; private set; }
        public int SlotReadCards { get; private set; }

        /// <summary>시험 시도인가(서버 test — 시험 계정이 연 것, 랭킹 · 남의 편성 · 내 순위 밖).</summary>
        static bool IsTest(JToken x) => x?["test"]?.Type == JTokenType.Boolean && (bool)x["test"];
        static string AttemptStatusKo(string st) => st == "verified" ? "검증됨" : st == "rejected" ? "거절됨" : st == "pending" ? "검증 전" : st == "skipped" ? "검증 안 함" : st == "open" ? "진행 중" : st == "expired" ? "내지 않음" : st ?? "";
        static Color AttemptStatusColor(string st) => st == "verified" ? Theme.Good : st == "rejected" ? Theme.Bad : Theme.Sub;   // 검증 전은 회색(랭킹의 「검증 전」 표시와 같은 색)
        static string OverKo(string o) => o == "win" ? "보스 처치" : o == "lose" ? "파티 쓰러짐" : o == Frontier.TIME ? "시간 종료" : null;
        static long? Long(JToken t) => t != null && t.Type == JTokenType.Integer ? (long)t : (long?)null;

        /// <summary>줄 그림을 누를 수 있게 — 밝아짐 · 금빛 테두리(줄 배치에서 빠진다). 랭킹 줄 · 기록 줄.</summary>
        Btn RowTap(UnityEngine.UI.Image img, Action go)
        {
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Btn>();
            b.Bg = img; b.SetColor(img.color); b.OnClick = go; b.NoScale = true;
            b.Hl = Ui.Img(img.transform, Theme.Frame, Theme.Gold.A(0), "hl");
            b.Hl.rectTransform.Fill();
            b.Hl.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
            return b;
        }

        // ── 내 기록 ──
        /// <summary>내 도전 기록(로그인 필요) — 최근 순 20줄, 「더 보기」 로 다음 쪽.</summary>
        public void HistoryScreen(Action back)
        {
            Stage.SetBg("stage37_1", 0.78f);
            fHist.Clear(); fHistMore = false; fHistPage = 0;
            Online.Call("GET", "/api/frontier/history", null, r =>
            {
                if (!r.Ok) { Toast.Show("기록을 받지 못했다 — " + Online.ErrorText(r), 3f); if (Stage.Current != "frontier_history") return; }
                TakeHistory(r);
                Stage.Show("frontier_history", root => BuildHistory(root, back));
            });
        }

        void TakeHistory(Online.Resp r)
        {
            if (!r.Ok || r.Json == null) return;
            fHist.AddRange((r.Json["rows"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>());
            fHistMore = r.Json["more"]?.Type == JTokenType.Boolean && (bool)r.Json["more"];
            fHistPage = (int?)r.Json["page"] ?? fHistPage;
        }

        void BuildHistory(RectTransform root, Action back)
        {
            ScreenHead(root, "내 기록", "줄을 누르면 그때의 편성이 열린다", back);

            var size = Stage.Size;
            float g = Theme.Gutter, w = Mathf.Min(1320, size.x - g * 2), top = FTop, sumH = Theme.C(100, 88);
            // 위 — 요약 띠(검증된 최고 · 상태별 수)
            var sum = FPanel(root, "summary");
            sum.At(0.5f, 1, 0, -top, w, sumH);
            Tw.Rise(sum, 0.04f, 20, 0.35f);
            var verified = fHist.Where(x => (string)x["status"] == "verified" && Long(x["score"]) != null).ToList();
            long? best = verified.Count > 0 ? verified.Max(x => Long(x["score"]).Value) : (long?)null;
            FLabel(sum, "검증된 최고", 12);
            var bv = Ui.Title(sum, best != null ? $"{best.Value:N0}<size=55%><color={Theme.SubTag}> 점</color></size>" : $"<color={Theme.SubTag}>—</color>", Theme.Fs2xl, Theme.Gold, TextAlignmentOptions.BottomLeft);
            bv.rectTransform.At(0, 0, FPad, 6, 360, sumH - 44); bv.Outline(0.12f); bv.textWrappingMode = TextWrappingModes.NoWrap;
            var chips = Ui.Rect("chips", sum).Fill(400, 0, FPad, 0);
            Ui.Row(chips, 10, TextAnchor.MiddleRight, null, false, false);
            void Count(string label, int n, Color c)
            {
                if (n <= 0) return;
                var (cb, ct) = Ui.Chip(chips, null, $"{label}  <b>{n}</b>", 40, c.A(0.22f), null, Theme.FsMd);
                ct.color = c;
            }
            Count(fHistMore ? "최근 도전" : "도전", fHist.Count, Theme.Ink);
            Count("검증됨", verified.Count, Theme.Good);
            Count("검증 전", fHist.Count(x => (string)x["status"] == "pending"), Theme.Sub);
            Count("거절됨", fHist.Count(x => (string)x["status"] == "rejected"), Theme.Bad);
            Count("검증 안 함", fHist.Count(x => (string)x["status"] == "skipped"), Theme.Sub);
            Count("내지 않음", fHist.Count(x => (string)x["status"] == "expired" || (string)x["status"] == "open"), Theme.Sub);

            // 아래 — 기록 줄(카드)
            var list = Ui.Rect("list", root);
            list.anchorMin = new Vector2(0.5f, 0); list.anchorMax = new Vector2(0.5f, 1); list.pivot = new Vector2(0.5f, 0.5f);
            list.offsetMin = new Vector2(-w / 2, g); list.offsetMax = new Vector2(w / 2, -(top + sumH + 14 + 34));
            // 열 머리 — 줄(HistRow)과 같은 자리
            var hdr = Ui.Rect("hdr", root).At(0.5f, 1, 0, -(top + sumH + 14), w, 30);
            void H(string s, float x, float wd, TextAlignmentOptions al) { var t = Ui.Text(hdr, s, Theme.FsCap, Theme.Sub, al); t.rectTransform.At(0, 0.5f, x, 0, wd, 28); t.Outline(0.25f); }
            if (fHist.Count > 0) { H("날짜", 24, 100, TextAlignmentOptions.MidlineLeft); H("편성", 128, 196, TextAlignmentOptions.MidlineLeft); H("점수", 336, 210, TextAlignmentOptions.MidlineRight); H("상태", 572, 118, TextAlignmentOptions.Midline); H("결과", 712, 300, TextAlignmentOptions.MidlineLeft); }
            Tw.Rise(list, 0.1f, 24, 0.4f);
            var content = Ui.Scroll(list, out _);
            Ui.Col(content, 10, TextAnchor.UpperLeft, new RectOffset(0, 8, 0, 10), true, false);
            HistoryRows = 0;
            if (fHist.Count == 0)
            {
                var empty = FPanel(content, "empty"); empty.Pref(-1, 200);
                EmptyNote(empty, "도전한 기록이 없다", "「진입」 으로 도전하면 쌓인다");
            }
            for (int i = 0; i < fHist.Count; i++)
            {
                var x = fHist[i];
                var img = Ui.Img(content, Theme.Round, Theme.NavyCell.A(0.82f), "row " + i);
                img.rectTransform.Pref(-1, Theme.C(84, 76));
                HistRow(img.rectTransform, x);
                string aid = (string)x["attempt"];
                Stage.Hot["hist.row" + i] = RowTap(img, () => AttemptScreen(aid, () => HistoryScreen(back)));
                HistoryRows++;
            }
            if (fHistMore)
            {
                var more = Btn.Make(content, "더 보기", BtnStyle.PillDark, () =>
                {
                    Online.Call("GET", $"/api/frontier/history?page={fHistPage + 1}", null, r =>
                    {
                        if (!r.Ok) { Toast.Show("더 받지 못했다 — " + Online.ErrorText(r), 3f); return; }
                        TakeHistory(r);
                        Stage.Show("frontier_history", rt => BuildHistory(rt, back), 0.1f, true);
                    });
                }, Theme.FsMd, "hist.more");
                more.Pref(-1, 56);
                Stage.Hot["hist.more"] = more;
            }
        }

        /// <summary>기록 한 줄(카드) — 날 · 편성 얼굴 · 점수(크게) · 상태 칩 · 끝과 까닭 · 오른쪽 화살.</summary>
        void HistRow(RectTransform line, JObject x)
        {
            string st = (string)x["status"];
            long at = Long(x["at"]) ?? 0;
            var local = DateTimeOffset.FromUnixTimeSeconds(at).ToLocalTime();
            var day = Ui.Title(line, at > 0 ? local.ToString("MM-dd") : "-", Theme.FsLg, Theme.Ink, TextAlignmentOptions.BottomLeft); day.rectTransform.At(0, 0.5f, 24, 12, 100, 34);
            var hm = Ui.Text(line, at > 0 ? local.ToString("HH:mm") : "", Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft); hm.rectTransform.At(0, 0.5f, 24, -16, 100, 26);
            var faces = Ui.Rect("faces", line).At(0, 0.5f, 128, 0, 196, 60);
            Ui.Row(faces, 6, TextAnchor.MiddleLeft, null, false, false);
            foreach (var h in (x["heroes"] as JArray) ?? new JArray()) W.Face(faces, Roster.OfCore((string)h), 58);
            int? score = (int?)Long(x["score"]), claimed = (int?)Long(x["claimed"]);
            string sc = score != null ? $"{score.Value:N0}" : claimed != null ? $"<color={Theme.SubTag}>{claimed.Value:N0}</color>" : $"<color={Theme.DimTag}>—</color>";
            var stx = Ui.Title(line, sc, Theme.Fs2xl - 8, score != null ? Theme.Gold : Theme.Ink, TextAlignmentOptions.MidlineRight);
            stx.rectTransform.At(0, 0.5f, 336, 0, 210, 60); stx.Outline(0.1f); stx.textWrappingMode = TextWrappingModes.NoWrap;
            bool test = IsTest(x);   // 시험 시도(시험 계정 — 서버 migrations/0009) — 랭킹 밖
            var (cb, ct) = Ui.Chip(line, null, test ? "시험" : AttemptStatusKo(st), 36, (test ? Theme.Hex("FF9A7A") : AttemptStatusColor(st)).A(0.25f), null, Theme.FsSm);
            ct.color = test ? Theme.Hex("FF9A7A") : AttemptStatusColor(st); cb.rectTransform.At(0, 0.5f, 572, 0, 118, 36);
            var over = OverKo((string)x["over"]);
            string l1 = over != null ? $"{over} · {Long(x["played"])}턴" : st == "open" ? "도전 중" : st == "expired" ? "내지 않고 지남" : "";
            string l2 = test ? $"<color={Theme.SubTag}>시험 기록 — 랭킹 · 남의 편성 · 내 순위에 오르지 않음</color>" : st == "rejected" ? $"<color={Theme.BadTag}>{(string)x["why"]}</color>" : st == "pending" ? $"<color={Theme.SubTag}>검증기가 다시 돌려 보는 중</color>" : st == "skipped" ? $"<color={Theme.SubTag}>내 최고보다 낮아 검증 안 함</color>" : "";
            var dt = Ui.Text(line, l2.Length > 0 ? $"{l1}\n<size=85%>{l2}</size>" : l1, Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            dt.rectTransform.Fill(712, 6, 64, 6); dt.enableAutoSizing = true; dt.fontSizeMin = 11; dt.fontSizeMax = Theme.FsMd;
            var arrow = Ui.Title(line, "›", Theme.Fs2xl, Theme.Sub, TextAlignmentOptions.Center); arrow.rectTransform.At(1, 0.5f, -20, 2, 40, 60);
        }
        // ── 그때의 편성 ──
        /// <summary>시도 하나의 편성 — 내 것이면 상태와 관계없이, 남의 것이면 검증된 시도만(검증 전은 서버가 줄만 — slots null · 거절 · 그 밖은 404).</summary>
        public void AttemptScreen(string id, Action back)
        {
            if (string.IsNullOrEmpty(id)) return;
            Online.Call("GET", "/api/frontier/attempt/" + Uri.EscapeDataString(id), null, r =>
            {
                if (!r.Ok || r.Json == null)
                {
                    Toast.Show(r.Code == 404 ? "볼 수 없는 기록이다 — 지워졌거나 검증 전이다" : "기록을 받지 못했다 — " + Online.ErrorText(r), 3f);
                    return;
                }
                AttemptShown = r.Json;
                if (!Mine(r.Json) && r.Json["slots"]?.Type != JTokenType.Array)
                {
                    Toast.Show("검증 전 기록이다 — 편성은 검증 뒤 공개된다", 3f);
                    return;
                }
                Debug.Log($"[Frontier] 편성 보기 — 시도 {id} · {(r.Json["mine"]?.Type == JTokenType.Boolean && (bool)r.Json["mine"] ? "내 것" : "남 " + r.Json["name"])} · {r.Json["status"]} · 점수 {r.Json["score"]} · [{HeroNames(r.Json["heroes"] as JArray)}]");
                Stage.SetBg("stage37_1", 0.8f);
                Stage.Show("frontier_attempt", root => BuildAttempt(root, r.Json, back));
            });
        }

        static bool Mine(JObject j) => j["mine"]?.Type == JTokenType.Boolean && (bool)j["mine"];

        static List<(string hero, HeroSlot slot)> AttemptSlots(JObject j)
        {
            var list = new List<(string, HeroSlot)>();
            foreach (var so in (j["slots"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                HeroSlot s = null;
                try { if (so["slot"] is JObject o) s = HeroSlot.Parse(o.ToString(Newtonsoft.Json.Formatting.None)); } catch (Exception e) { Debug.LogWarning("[Frontier] 편성 슬롯을 읽지 못함 — " + e.Message); }
                list.Add(((string)so["hero"] ?? s?.Hero, s));
            }
            return list;
        }

        void BuildAttempt(RectTransform root, JObject j, Action back)
        {
            bool mine = Mine(j);
            string name = (string)j["name"] ?? "", st = (string)j["status"];
            ScreenHead(root, mine ? "그때의 편성" : $"{name}의 편성", "사도를 누르면 카드 · 아티팩트", back, hd =>
            {
                if (mine) return;
                HeadChip(hd, "읽기 전용", ChipKind.Sky);
            });

            var size = Stage.Size;
            float top = FTop, g = Theme.Gutter, infoH = Theme.C(100, 88), bottom = g;
            // 위 띠 — 점수 · 상태 · 끝 · 날
            var band = FPanel(root, "info");
            band.Band(1, infoH, g, g, -top);
            Tw.Rise(band, 0.04f, 20, 0.35f);
            int? score = (int?)Long(j["score"]), claimed = (int?)Long(j["claimed"]);
            var brow = Ui.Rect("row", band).Fill(FPad, 6, FPad, 6);
            Ui.Row(brow, 24, TextAnchor.MiddleLeft, null, false, false);
            var sc = Ui.Title(brow, score != null ? $"{score.Value:N0}<size=50%><color={Theme.SubTag}> 점</color></size>" : claimed != null ? $"<color={Theme.SubTag}>{claimed.Value:N0}</color><size=50%><color={Theme.SubTag}> 점</color></size>" : $"<color={Theme.DimTag}>—</color>",
                Theme.FsDisplay - 22, score != null ? Theme.Gold : Theme.Ink, TextAlignmentOptions.MidlineLeft);
            sc.Outline(0.12f); sc.textWrappingMode = TextWrappingModes.NoWrap; sc.Pref(sc.preferredWidth + 8, infoH - 12);
            bool test = IsTest(j);
            var (cb, ct) = Ui.Chip(brow, null, test ? "시험 · 랭킹 밖" : AttemptStatusKo(st), 42, (test ? Theme.Hex("FF9A7A") : AttemptStatusColor(st)).A(0.25f), null, Theme.FsMd);
            ct.color = test ? Theme.Hex("FF9A7A") : AttemptStatusColor(st);
            var over = OverKo((string)j["over"]);
            var season = Online.Season != null && (string)Online.Season["id"] == (string)j["season"] ? SeasonLabel(Online.Season) : (string)j["season"];
            string l1 = $"{(over != null ? $"{over} · {Long(j["played"])}턴" : st == "open" ? "도전 중" : "내지 않음")}  <color={Theme.SubTag}>· {season} · {SlotStore.Date(Long(j["at"]) ?? 0)}</color>";
            string l2 = mine && st == "rejected" ? $"\n<size=85%><color={Theme.BadTag}>거절됨 — {(string)j["why"]}</color></size>" : "";
            var meta = Ui.Text(brow, l1 + l2, Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            meta.Pref(-1, infoH - 12, 1); meta.enableAutoSizing = true; meta.fontSizeMin = 11; meta.fontSizeMax = Theme.FsMd;

            // 사도 카드 셋
            var slots = AttemptSlots(j);
            float availH = size.y - top - infoH - 18 - bottom, availW = size.x - g * 2, gap = 30;
            float sumH = 64, cardH = availH - sumH - 10;
            float cw = Mathf.Min((availW - gap * 2) / 3f, cardH * 0.74f);
            var cols = Ui.Rect("cols", root);
            cols.anchorMin = new Vector2(0, 0); cols.anchorMax = new Vector2(1, 1);
            cols.offsetMin = new Vector2(g, bottom); cols.offsetMax = new Vector2(-g, -(top + infoH + 18));
            for (int i = 0; i < slots.Count; i++)
            {
                int k = i;
                var (hero, s) = slots[i];
                var h = Roster.OfCore(hero);
                var nc = h != null ? Theme.NatureOf(h.nature) : Theme.Sub;
                var col = Ui.Rect("seat" + i, cols).At(0.5f, 0.5f, (i - (slots.Count - 1) / 2f) * (cw + gap), 0, cw, availH);
                Tw.Rise(col, 0.06f + i * 0.06f, 24, 0.4f);
                var hb = Btn.Make(col, null, BtnStyle.Cell, () => SlotRead(j, k), 0, "seat" + i);
                var hrt = hb.GetComponent<RectTransform>(); hrt.At(0.5f, 1, 0, 0, cw, cardH);
                Stage.Hot["fatt.hero" + i] = hb;
                var glow = Ui.Img(hrt, Theme.S("soft"), nc.A(0.45f), "glow"); glow.rectTransform.At(0.5f, 0.55f, 0, 0, cw * 1.1f, cardH * 0.8f);
                var win = Ui.Rect("win", hrt).Fill(5, 5, 5, 5); win.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                var art = h?.art != null ? CardArt.Upper(h.art, (cw - 10) / (cardH - 10), 0.74f) : null;
                if (art != null) Ui.Img(win, art, Color.white, "art").rectTransform.Fill();
                else if (h?.Icon != null) { var im = Ui.Img(win, h.Icon, Color.white, "icon"); im.preserveAspect = true; im.rectTransform.Fill(20, 80, 20, 20); }
                var shade = Ui.Img(hrt, Theme.S("fade_card"), Color.white, "shade"); shade.rectTransform.Fill(5, 5, 5, 5);
                var edge = Ui.Img(hrt, Theme.Round, nc, "tone"); edge.rectTransform.At(0, 0.5f, 5, 0, 5, cardH - 30);
                var ni = Theme.Icon("성격_" + h?.nature); var ri = Theme.Icon("역할_" + h?.role);
                if (ni != null) { var a = Ui.Img(hrt, ni, Color.white, "nat"); a.preserveAspect = true; a.rectTransform.At(0, 1, 18, -16, 46, 46); }
                if (ri != null) { var a = Ui.Img(hrt, ri, Color.white, "role"); a.preserveAspect = true; a.rectTransform.At(0, 1, 18, -68, 46, 46); }
                var zi = Ui.Img(hrt, Theme.S("ic_zoom"), Color.white.A(0.9f), "zoom"); zi.preserveAspect = true; zi.rectTransform.At(1, 1, -18, -18, 40, 40);
                var nm = Ui.Title(hrt, h?.ko ?? hero, Theme.Fs2xl - 6, Color.white, TextAlignmentOptions.BottomLeft);
                nm.rectTransform.Band(0, 50, 20, 20, 44); nm.Outline(0.25f); nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 18; nm.fontSizeMax = Theme.Fs2xl - 6;
                var sub = Ui.Text(hrt, $"{new string('★', Mathf.Clamp(h?.star ?? 0, 0, 3))}  <color=#{ColorUtility.ToHtmlStringRGB(nc)}>{h?.nature}</color> · {h?.role}", Theme.FsSm, Theme.Ink, TextAlignmentOptions.BottomLeft);
                sub.rectTransform.Band(0, 28, 22, 20, 16); sub.Outline(0.2f);
                // 아래 — 슬롯 요약
                var sm = Ui.Img(col, Theme.Round, Color.Lerp(Theme.NavyWell, nc, 0.18f), "sum").rectTransform;
                sm.At(0.5f, 0, 0, 0, cw, sumH);
                var smt = Ui.Text(sm, SlotLine(s), Theme.FsSm, Theme.Ink, TextAlignmentOptions.Center);
                smt.rectTransform.Fill(14, 6, 14, 6); smt.enableAutoSizing = true; smt.fontSizeMin = 10; smt.fontSizeMax = Theme.FsSm;
            }
            if (slots.Count == 0) Ui.Text(cols, "편성을 읽지 못했다", Theme.FsLg, Theme.Bad, TextAlignmentOptions.Center).rectTransform.Fill();
        }

        /// <summary>
        /// 읽기 전용 저장 데이터 창 — 저장 데이터 창(FrontierSaveData)과 같은 판(왼쪽 사도 셋 · 가운데 장비 · 요약 · 오른쪽 카드).
        /// 장착 · 지우기 단추가 없다. 남의 것은 뜬 판의 씨앗을 보이지 않는다(서버도 주지 않는다).
        /// </summary>
        public void SlotRead(JObject j, int i)
        {
            var slots = AttemptSlots(j);
            if (i < 0 || i >= slots.Count) return;
            bool mine = Mine(j);
            string name = (string)j["name"] ?? "", st = (string)j["status"];
            var (hero, s) = slots[i];
            var h = Roster.OfCore(hero);
            SlotReadHero = hero; SlotReadCards = s?.Cards.Count ?? 0;
            var size = Stage.Size;
            float w = Mathf.Min(1540, size.x - 32), hh = size.y - 24;
            var (body, close, _) = Stage.ModalBox("slotread", w, hh, $"{(mine ? "그때의 편성" : name + "의 편성")} — {h?.ko ?? hero}", "읽기 전용 · 도전 때 그대로", true, null, 0);
            void Reopen(int k) { close(); SlotRead(j, k); }
            float lw = 250, iw = Mathf.Min(440, w * 0.3f);
            RectTransform Col(string n, float x, float wd)
            {
                var r = Ui.Rect(n, body);
                r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 0.5f);
                r.offsetMin = new Vector2(x, 0); r.offsetMax = new Vector2(x + wd, 0);
                return r;
            }
            // 왼쪽 — 이 편성의 사도 셋
            var list = Col("list", 0, lw);
            Ui.Col(list, 12, TextAnchor.UpperLeft, new RectOffset(0, 0, 4, 0), true, false);
            for (int k = 0; k < slots.Count; k++)
            {
                int kk = k;
                var hk = Roster.OfCore(slots[k].hero);
                var b = Btn.Make(list, null, BtnStyle.Cell, () => Reopen(kk), 0, "hero" + k);
                b.Pref(-1, 128);
                if (k == i) b.Bg.sprite = Theme.CellOn;
                var rt = b.GetComponent<RectTransform>();
                var fc = W.Face(rt, hk, 56); fc.At(0, 1, 14, -14, 56, 56);
                var tt = Ui.Title(rt, hk?.ko ?? slots[k].hero, Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft); tt.rectTransform.At(0, 1, 80, -20, lw - 96, 44);
                tt.textWrappingMode = TextWrappingModes.NoWrap; tt.enableAutoSizing = true; tt.fontSizeMin = 14; tt.fontSizeMax = Theme.FsLg;
                var sl = Ui.Text(rt, SlotLine(slots[k].slot), Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft); sl.rectTransform.Fill(14, 10, 14, 76);
                sl.enableAutoSizing = true; sl.fontSizeMin = 9; sl.fontSizeMax = Theme.FsCap;
                Stage.Hot["sread.hero" + k] = b;
            }
            var lh = Ui.Text(list, mine ? "칸을 덮어써도 이 편성은 그대로" : "참고용 — 장착할 수 없다", Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft); lh.Pref(-1, 40);
            // 가운데 · 오른쪽 — 저장 데이터 창과 같은 판
            var info = Ui.Panel(Col("infocol", lw + 18, iw), Theme.Panel, null, "info").rectTransform;
            info.Fill();
            var right = Col("cards", lw + 18 + iw + 18, w - (lw + 18 + iw + 18) - 56);
            int? score = (int?)Long(j["score"]), claimed = (int?)Long(j["claimed"]);
            int? shown = score ?? claimed;
            string bandLabel = score != null ? (mine ? "이 편성으로 낸 점수 · 검증됨" : "이 편성의 검증된 점수") : claimed != null ? $"이 편성으로 낸 점수 · {AttemptStatusKo(st)}" : "이 도전으로 낸 점수가 없다";
            string dateLine = $"{(mine ? "내 도전" : name)} · {SlotStore.Date(Long(j["at"]) ?? 0)} · <color=#{ColorUtility.ToHtmlStringRGB(AttemptStatusColor(st))}>{AttemptStatusKo(st)}</color>";
            SaveDataBody(info, right, iw, hero, s, dateLine, shown, bandLabel, mine && st == "rejected" ? "거절됨 — " + (string)j["why"] : null, mine);
            Debug.Log($"[Frontier] 저장 데이터(읽기 전용) — {hero} · 카드 {SlotReadCards}장 · {(mine ? "내 것" : name)}");
        }
    }
}
