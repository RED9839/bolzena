using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 이벤트 대화창 흐름(2026-10-06 사용자: 「처음 서술 · 대사를 밑으로 내린 다음 서술 · 대사를 다 넘기고 선택 창을 띄우고, 다시 서술 창으로」)
    //   dialog(scene 글을 한 줄씩) → choose(선택지 카드 · 맨 끝 서술 한 줄을 작게) → resultDialog(결과 say 를 한 줄씩) → result(받은 것 · 떠나기)
    //   서술(따옴표 밖)은 이름표 없이, 대사(따옴표 안)는 이름표(말하는 이 = npc · 장면 대상 · 글에 이름이 나오는 파티 사도)와 함께.
    //   한 글자씩 · 누르면 그 줄이 다 나오고 · 다시 누르면 다음 줄. 「건너뛰기」는 남은 줄을 넘긴다. Esc · 뒤로는 쓰지 않는다(이벤트는 골라야 한다).
    public partial class Flow
    {
        public sealed class EvLine { public string Who; public string Text; }

        /// <summary>지금 이벤트 단계 — dialog · choose · resultDialog · result. 데모가 본다.</summary>
        public string EventStep { get; private set; }
        /// <summary>지금 대화창 줄들 · 보이는 줄 번호(데모).</summary>
        public List<EvLine> EventLinesNow { get; private set; } = new List<EvLine>();
        public int EventLineIdx { get; private set; }
        /// <summary>데모 — 대화창을 바로 넘긴다(대화창을 보지 않는 단언들이 쓴다).</summary>
        public bool DialogAuto;

        string evStepKey;
        Dictionary<string, Vector2> evHeads = new Dictionary<string, Vector2>();

        /// <summary>글 → 대화창 줄. 서술은 문장마다 한 줄, 대사(따옴표 안)는 한 덩어리 한 줄. 따옴표 짝이 안 맞으면 통째로 서술.</summary>
        public static List<EvLine> EventLines(string text, string speaker, IEnumerable<string> partyNames = null, IList<string> by = null)
        {
            var outs = new List<EvLine>();
            if (string.IsNullOrWhiteSpace(text)) return outs;
            var (_, _, ok) = SplitSay(text);
            void Narr(string chunk)
            {
                foreach (var sen in Regex.Split(chunk.Trim(), @"(?<=[.!?…。])\s+"))
                    if (!string.IsNullOrWhiteSpace(sen)) outs.Add(new EvLine { Text = sen.Trim() });
            }
            if (!ok) { Narr(text); return outs; }
            var cur = new System.Text.StringBuilder(); bool inQ = false; int qi = 0;
            foreach (char ch in text)
            {
                bool open = ch == '“' || (ch == '"' && !inQ), close = ch == '”' || (ch == '"' && inQ);
                if (!inQ && open) { Narr(cur.ToString()); cur.Clear(); inQ = true; continue; }
                if (inQ && close)
                {
                    inQ = false;
                    var sp = cur.ToString().Trim(); cur.Clear();
                    if (sp.Length == 0) continue;
                    // 말하는 이 — 데이터의 by(대사 순서대로 · 동행 사도 · 「경비대」 따위)가 먼저. 없으면 장면 대상, 그것도 없으면 이 대사 바로 앞 서술에 이름이 나오는 파티 사도
                    string who = by != null && qi < by.Count && !string.IsNullOrEmpty(by[qi]) ? by[qi] : speaker;
                    qi++;
                    if (who == null && partyNames != null)
                    {
                        string before = outs.Count > 0 && outs[outs.Count - 1].Who == null ? outs[outs.Count - 1].Text : "";
                        who = partyNames.FirstOrDefault(n => before.Contains(n)) ?? partyNames.FirstOrDefault(n => text.Contains(n));
                    }
                    outs.Add(new EvLine { Who = who, Text = who != null ? sp : $"\"{sp}\"" });
                    continue;
                }
                cur.Append(ch);
            }
            Narr(cur.ToString());
            return outs;
        }

        /// <summary>장면을 그린 뒤 — 단계에 따라 대화창 · 선택지 · 결과.</summary>
        void EventStage(RectTransform root, RectTransform scene, EventDef evd, EventState E, int picked, float floorY, string speaker, bool special)
        {
            string key = E.Key + "|" + E.Id + "|" + E.Phase;
            if (key != evStepKey) { evStepKey = key; EventStep = E.Phase == "choose" ? "dialog" : "resultDialog"; EventLineIdx = 0; }
            string text = E.Phase == "choose" ? evd.Scene : E.Say;
            var party = P.S.Party.Select(k => Roster.OfCore(k)?.ko).Where(n => n != null).ToList();
            // 결과 대사의 말하는 이(엔진이 선택지 by 를 풀어 둔 것) — 사도 이름은 화면 이름(로스터 ko)으로
            var by = E.Phase == "choose" ? null : E.SayBy?.Select(n => n == null ? null : Roster.All.FirstOrDefault(h => h.ko == n || h.key == n || h.ko.Replace(" ", "") == n.Replace(" ", ""))?.ko ?? n).ToList();
            EventLinesNow = EventLines(text, speaker, party, by);
            if (EventStep == "dialog" && (EventLinesNow.Count == 0 || DialogAuto)) EventStep = "choose";
            if (EventStep == "resultDialog" && (EventLinesNow.Count == 0 || DialogAuto)) EventStep = "result";

            EventTitle(root, evd);
            if (EventStep == "dialog" || EventStep == "resultDialog")
            {
                EventDialog(root, evd);
                return;
            }
            if (EventStep == "choose")
            {
                float top = EventChoices(root, scene, evd, picked, floorY, 0, 0, false);
                // 맥락 — scene 의 마지막 서술 한 줄을 선택지 위에 작게(선택지 왼쪽 시작선)
                var sceneLines = EventLines(evd.Scene, speaker, party);
                var last = sceneLines.LastOrDefault(l => l.Who == null) ?? sceneLines.LastOrDefault();
                if (last != null)
                {
                    var size = Stage.Size;
                    var (x0, rowW, _) = ChoiceRow0(evd, false);
                    var ctx = Ui.Text(root, last.Who != null ? $"<color={Theme.GoldTag}>{last.Who}</color>  {last.Text}" : last.Text, Theme.FsBody, Theme.Sub, TextAlignmentOptions.BottomLeft, false, "context");
                    ctx.textWrappingMode = TextWrappingModes.Normal; ctx.overflowMode = TextOverflowModes.Overflow; ctx.Outline(0.2f);
                    float cw = Mathf.Max(rowW, Mathf.Min(size.x - x0 - Theme.Gutter, Theme.C(1200, 700)));
                    float ch = ctx.GetPreferredValues(ctx.text, cw - EvTextInset, 0).y;
                    var crt = ctx.rectTransform; crt.anchorMin = crt.anchorMax = crt.pivot = Vector2.zero;
                    crt.sizeDelta = new Vector2(cw - EvTextInset, ch); crt.anchoredPosition = new Vector2(x0 + EvTextInset, top + Theme.C(56, 40));
                    // 선택지가 두 줄이 되면 이 글이 파티 SD 발치에 겹친다(M4 · 2026-10-06 캡처) — 글 뒤에 옅은 밤색 판을 깔아 그림 위에서도 읽히게
                    float tw = Mathf.Min(cw - EvTextInset, ctx.GetPreferredValues(ctx.text, cw - EvTextInset, 0).x);
                    var plate = Ui.Img(root, Theme.Round, Theme.Night.A(0.55f), "contextplate"); plate.type = Image.Type.Sliced;
                    var prt = plate.rectTransform; prt.anchorMin = prt.anchorMax = prt.pivot = Vector2.zero;
                    prt.sizeDelta = new Vector2(tw + 28, ch + 10); prt.anchoredPosition = crt.anchoredPosition - new Vector2(14, 5);
                    plate.raycastTarget = false;
                    plate.transform.SetSiblingIndex(ctx.transform.GetSiblingIndex());
                    Tw.FadeIn(plate, 0.3f, 0.1f);
                    Tw.FadeIn(ctx, 0.3f, 0.1f);
                }
                return;
            }
            EventResult(root, evd, special ? Stage.Size.y * 0.42f : floorY, false);
        }

        /// <summary>왼쪽 위 이벤트 아이콘 원 · 이름(지문 띠 없이).</summary>
        void EventTitle(RectTransform root, EventDef ev)
        {
            float iconS = Theme.C(110, 76), y = -Theme.C(108, 70);
            var disc = Ui.Img(root, Theme.S("circle"), Theme.NavyPanel, "evicon"); disc.rectTransform.At(0, 1, Theme.Gutter, y, iconS, iconS);
            var ring = Ui.Img(disc.transform, Theme.S("ring"), Theme.Gold.A(0.85f), "ring"); ring.rectTransform.Fill();
            bool fight = ev.Options.Any(o => o.Fight != null);
            var ic = Ui.Img(disc.transform, Theme.S(ev.Npc != null ? "ic_heart" : fight ? "ic_swords" : "ic_question"), Theme.Gold, "ic"); ic.rectTransform.Fill(iconS * 0.26f, iconS * 0.26f, iconS * 0.26f, iconS * 0.26f); ic.preserveAspect = true;
            var name = Ui.Title(root, ev.Name, Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft, "evname");
            name.rectTransform.At(0, 1, Theme.Gutter + iconS + 16, y - iconS / 2 + 22, Mathf.Min(Theme.C(700, 420), Stage.Size.x * 0.4f), 44);
            name.textWrappingMode = TextWrappingModes.Normal; name.overflowMode = TextOverflowModes.Overflow; name.Outline(0.25f);
        }

        /// <summary>아래 대화창 — 지금 줄(EventLineIdx). 누르면 그 줄을 다 보이고, 다시 누르면 다음 줄 · 끝이면 다음 단계.</summary>
        void EventDialog(RectTransform root, EventDef evd)
        {
            var size = Stage.Size;
            var lines = EventLinesNow;
            float bottom = Theme.C(30, 12), tagH = Theme.C(46, 32);
            float boxW = Mathf.Min(size.x - Theme.Gutter * 2, Theme.C(2000, 4000));
            float textW = boxW - EvTextInset * 2 - Theme.C(60, 44);
            // 높이 — 모든 줄 가운데 가장 긴 줄에 맞춘다(줄을 넘겨도 상자가 출렁이지 않게)
            var probe = Ui.Text(root, "", Theme.FsLg, Theme.Ink, TextAlignmentOptions.TopLeft);
            probe.textWrappingMode = TextWrappingModes.Normal;
            float need = lines.Select(l => probe.GetPreferredValues(l.Text, textW, 0).y).DefaultIfEmpty(40).Max();
            Destroy(probe.gameObject);
            float boxH = Mathf.Max(Theme.C(190, 104), need + Theme.C(64, 40));
            float boxX = (size.x - boxW) / 2;

            var box = Btn.Make(root, null, BtnStyle.Ghost, null, 0, "evdialog");
            box.Bg.sprite = Theme.Round; box.Bg.type = Image.Type.Sliced; box.SetColor(new Color(0.04f, 0.05f, 0.11f, 0.86f));
            var brt = box.GetComponent<RectTransform>(); brt.name = "evdialog";
            brt.anchorMin = brt.anchorMax = brt.pivot = Vector2.zero; brt.sizeDelta = new Vector2(boxW, boxH); brt.anchoredPosition = new Vector2(boxX, bottom);
            var rim = Ui.Img(brt, Theme.Frame, Theme.Gold.A(0.55f), "rim"); rim.rectTransform.Fill();

            // 이름표 — 상자 위 왼쪽(대사 줄만)
            var tag = Ui.Img(brt, Theme.S("pill_dark", 46), Color.white.A(0.95f), "nametag");
            tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = tag.rectTransform.pivot = new Vector2(0, 1);
            var tagT = Ui.Title(tag.rectTransform, "", Theme.FsMd, Theme.Gold, TextAlignmentOptions.Center, "name");
            tagT.rectTransform.Fill(18, 0, 18, 0); tagT.textWrappingMode = TextWrappingModes.NoWrap; tagT.overflowMode = TextOverflowModes.Overflow;

            var t = Ui.Text(brt, "", Theme.FsLg, Theme.Ink, TextAlignmentOptions.TopLeft, false, "line");
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 4;
            var trt = t.rectTransform; trt.anchorMin = new Vector2(0, 0); trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = new Vector2(EvTextInset, Theme.C(20, 12)); trt.offsetMax = new Vector2(-EvTextInset - Theme.C(60, 44), -Theme.C(30, 20));
            var ty = t.gameObject.AddComponent<TypeOn>();

            // 다음 표시(▼) · 줄 수
            var nextIc = Ui.Img(brt, Theme.S("ic_play"), Theme.Gold, "next"); nextIc.preserveAspect = true;
            nextIc.rectTransform.At(1, 0, -EvTextInset - 6, Theme.C(22, 14), Theme.C(30, 22), Theme.C(30, 22)); nextIc.rectTransform.localEulerAngles = new Vector3(0, 0, -90);
            Tw.Breathe(nextIc.transform, 0.12f, 0.9f);
            var count = Ui.Text(brt, "", Theme.FsCap, Theme.Dim, TextAlignmentOptions.TopRight, false, "count");
            count.rectTransform.At(1, 1, -EvTextInset, -12, 120, 24);

            // 말하는 이 표시 — 장면의 그 머리 위 작은 금빛 ▼.
            //   폰(좁고 긴 가로 · 작은 해상도): 캔버스가 1280×720 기준이라 24 단위면 화면에서 10px 남짓이고, 회전 축이 아래 끝이라 반쯤 머리에 묻혔다
            //   → 가운데 축으로 돌리고(머리 위로 온전히), 폰에서는 PC 와 같은 34 단위(캔버스 비율로 화면에서 더 크다) + 그림자 테로 머리 · 모자 색 위에서도 보이게
            float mS = Theme.C(34, 34);
            var mark = Ui.Img(root, Theme.S("ic_play"), Theme.Gold, "speaker"); mark.preserveAspect = true;
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = Vector2.zero; mark.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            mark.rectTransform.sizeDelta = new Vector2(mS, mS); mark.rectTransform.localEulerAngles = new Vector3(0, 0, -90);
            var mEdge = mark.gameObject.AddComponent<Shadow>(); mEdge.effectColor = new Color(0, 0, 0, 0.75f); mEdge.effectDistance = new Vector2(1.5f, -1.5f);
            Tw.Breathe(mark.transform, 0.15f, 0.8f);

            void Show()
            {
                var l = lines[Mathf.Clamp(EventLineIdx, 0, lines.Count - 1)];
                bool speaking = l.Who != null;
                tag.gameObject.SetActive(speaking);
                if (speaking)
                {
                    tagT.text = l.Who;
                    float tw = tagT.GetPreferredValues(l.Who, 0, 0).x + 40;
                    tag.rectTransform.sizeDelta = new Vector2(Mathf.Max(Theme.C(140, 96), tw), tagH);
                    tag.rectTransform.anchoredPosition = new Vector2(EvTextInset - 6, tagH * 0.62f);
                }
                t.text = l.Text; t.color = speaking ? Color.white : Theme.Ink;
                ty.Restart();
                count.text = $"{EventLineIdx + 1} / {lines.Count}";
                nextIc.enabled = false;
                bool marked = speaking && evHeads.TryGetValue(l.Who, out var hp);
                mark.gameObject.SetActive(marked);
                if (marked) { evHeads.TryGetValue(l.Who, out var h2); mark.rectTransform.anchoredPosition = new Vector2(h2.x, Mathf.Min(h2.y + 4 + mS / 2, size.y - Theme.C(150, 90))); }
            }
            ty.Done = () => { if (nextIc) nextIc.enabled = true; };
            void Advance()
            {
                if (!ty.IsDone) { ty.Finish(); return; }
                EventLineIdx++;
                if (EventLineIdx < lines.Count) { Show(); return; }
                EndDialog(root);
            }
            box.OnClick = Advance;
            Stage.Hot["event.next"] = box;

            // 건너뛰기 — 남은 줄을 넘긴다(상자 위 오른쪽)
            var skip = Btn.Make(root, "건너뛰기", BtnStyle.PillDark, () => EndDialog(root), Theme.FsSm, "evskip");
            var srt = skip.GetComponent<RectTransform>(); srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0, 0);
            srt.sizeDelta = new Vector2(Theme.C(170, 120), Theme.C(46, 34)); srt.anchoredPosition = new Vector2(boxX + boxW - srt.sizeDelta.x, bottom + boxH + 10);
            Stage.Hot["event.skip"] = skip;

            Show();
            Tw.Rise(brt, 0.05f, 24, 0.3f);
        }

        void EndDialog(RectTransform root)
        {
            if (EventStep == "dialog") EventStep = "choose";
            else if (EventStep == "resultDialog") EventStep = "result";
            BuildEvent(root, -1);
        }
    }
}
