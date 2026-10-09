using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 사도를 고르고 살피는 화면 셋 — 다른 판 화면과 같은 어두운 남색 판 + 금 테(Docs/톤.md). 카제나 「팀 편성」 · 「전투원 목록」 · 「상세 정보」 의 배치만 따른다.
    //   편성(PartyScreen): 큰 세로 사도 카드 셋(스탠딩 · 이어진 수치 판 — 왼쪽부터 자리 1 · 2 · 3) · 아래 왼쪽 시작 덱 평균 비용 · 오른쪽 이번 층 판 · 「모험 시작」
    //   사도 목록(HeroList): 왼쪽 성격 탭 · 카드 격자(스탠딩 상반신 · 파티 = 금 테 + 번호 · 보고 있는 사도 = 하늘 테) · 아래 「상세 정보」 · 「편성」
    //   사도 상세(HeroDetail): 왼쪽 초상 줄 · 세로 메뉴(능력치 · 카드 · 고유 효과) · 큰 스탠딩 + 기울어진 색 판 · 수치 표
    // 그림: 스탠딩(CardArt.Standing · Upper) — 없으면 SD 전투 스파인 · 초상으로 대신.
    public partial class Flow
    {
        // ── 판 조각 ──
        /// <summary>바탕 — 맨 뒤 남색(불투명이면 밑 화면이 비치지 않는다) · 가운데 남색 빛 · 아래 그늘 · 가는 금빛 사선.</summary>
        static void RosterBg(RectTransform root, float alpha = 1f)
        {
            var bg = Ui.Img(root, Theme.White, Theme.Night.A(alpha), "rosterbg", true); bg.rectTransform.Fill();
            var halo = Ui.Img(root, Theme.S("soft"), Theme.NavyCell.A(0.7f), "halo"); halo.rectTransform.At(0.55f, 0.6f, 0, 0, 1900, 1200);
            var low = Ui.Img(root, Theme.S("fade_down"), Theme.Night.A(0.8f), "low"); low.rectTransform.Band(0, 300);
            for (int i = 0; i < 5; i++)
            {
                var ln = Ui.Img(root, Theme.White, Theme.Edge.A(0.05f), "deco");
                ln.rectTransform.At(0.5f, 0.5f, -700 + i * 360, 0, 1, 1400);
                ln.rectTransform.localRotation = Quaternion.Euler(0, 0, -28);
            }
        }

        /// <summary>큰 판(남색 + 얇은 금 테 — panel.png).</summary>
        static RectTransform NavyBox(Transform parent, string name = "panel", float alpha = 0.96f)
            => Ui.Img(parent, Theme.Panel, Color.white.A(alpha), name, true).rectTransform;

        TextMeshProUGUI RosterHead(RectTransform root, string title, Action back, bool info = true)
        {
            var bk = Btn.Icon(root, Theme.S("ic_back"), back, 56, "back");
            bk.GetComponent<RectTransform>().At(0, 1, Theme.Gutter, -18, 56, 56);
            Stage.Hot["back"] = bk;
            var t = Ui.Title(root, title, Theme.Fs2xl - 4, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            t.rectTransform.At(0, 1, Theme.Gutter + 72, -16, 600, 60);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.Outline(0.2f);
            if (info)
            {
                var ii = Ui.Img(root, Theme.S("ic_info"), Theme.Dim, "info"); ii.preserveAspect = true;
                ii.rectTransform.At(0, 1, Theme.Gutter + 72 + t.preferredWidth + 10, -34, 24, 24);
            }
            return t;
        }

        /// <summary>남색 알약 단추(금 테) — 아이콘 + 글.</summary>
        Btn NavyPill(Transform parent, string icon, string label, Action go, string name, float w = -1, float h = 52, Color? text = null)
        {
            var b = Btn.Make(parent, null, BtnStyle.PillDark, go, 0, name);
            var rt = b.GetComponent<RectTransform>();
            Ui.Row(rt, 8, TextAnchor.MiddleCenter, new RectOffset(icon != null ? 16 : 22, 22, 6, 6), false, false);
            if (icon != null) { var ic = Ui.Img(rt, Theme.S(icon), Theme.Gold, "ic"); ic.preserveAspect = true; ic.Pref(24, 24); }
            var t = Ui.Title(rt, label, Theme.FsBody, text ?? Theme.Ink); t.textWrappingMode = TextWrappingModes.NoWrap;
            b.Label = t;
            b.Pref(w, h);
            return b;
        }

        static Color NatureCol(HeroInfo h) => Theme.NatureCardOf(h?.nature);

        Sprite Icon(string key) => Theme.Icon(key);

        /// <summary>사도 그림을 rt 에 채운다 — 스탠딩(위에서 frac 만큼, rt 비율로 자름) → 없으면 미니미 SD(spine) → 초상.</summary>
        void HeroArt(RectTransform rt, HeroInfo h, float w, float hgt, float frac, float spineH, float phase = 0, Color? tint = null, bool live = false, bool lazyLive = false, float tallest = 0)
        {
            // 사도 목록 상반신 — 정지 그림. 표가 있는 사도는 스크롤 창에 들어올 때 스탠딩을 표대로 구워 넣는다(LiveStanding · StandingSnap)
            if (lazyLive && h?.art != null)
            {
                bool fit = StandingFit.Has(h.art);
                // 표 · 스파인이 없을 때만 쓰는 웹판 렌더 자르기 — 표가 있는 사도는 굽기에 실패할 때만 그 자리에서(LiveStanding.WebFallback).
                //   예전엔 목록을 열 때 135명의 큰 정지 스탠딩(RunArt/Standing)을 한꺼번에 읽었다
                var web = fit ? null : CardArt.Upper(h.art, w / hgt, frac, false);
                if (!fit && web == null && h.Icon != null) { var ic = Ui.Img(rt, h.Icon, tint ?? Color.white, "icon"); ic.rectTransform.At(0.5f, 1, 0, 0, w * 1.08f, w * 1.08f); ic.preserveAspect = true; return; }
                var still = Ui.Img(rt, fit ? null : web, fit ? Color.clear : tint ?? Color.white, "standing"); still.rectTransform.Fill();
                if (!fit) return;
                var ls = rt.gameObject.AddComponent<LiveStanding>();
                ls.Art = h.art; ls.W = w; ls.H = hgt; ls.Frac = frac;
                ls.Tint = tint ?? Color.white; ls.Still = still; ls.Fallback = web;
                ls.List = true; ls.WebFallback = true;   // 목록 카드 자르기 — 머리 전체 + 어깨 · 가슴께, 얼굴이 위 1/3 께(StandingSnap.ListRect)
                return;
            }
            // 편성 큰 카드(셋) — 스탠딩 스파인을 실제로, 다리 기준 크기(무릎께에서 잘리게 발은 창 아래) · 머리 뼈는 창 위 2할 아래로
            if (live && h?.art != null)
            {
                float leg = hgt / frac / 2.96f;   // 키/다리 중앙 2.96 — 보통 키가 창 높이/frac 이 되게
                var sg = SpineUi.Standing(rt, h.art, w, hgt, StandMode.Knee, tallest);   // 표(standing_fit.json · 안 B) — 셋 모두 같은 배율 · 바닥선(기준 = 셋 가운데 가장 큰 사도)
                if (sg != null) { sg.AnimationState.Update(phase); if (tint != null) sg.color = tint.Value; return; }
            }
            var up = h?.art != null ? CardArt.Upper(h.art, w / hgt, frac) : null;
            if (up != null)
            {
                var im = Ui.Img(rt, up, tint ?? Color.white, "standing"); im.rectTransform.Fill();
                return;
            }
            var spot = Ui.Rect("hero", rt).At(0.5f, 0, 0, hgt * 0.06f, 10, 10);
            var g = spineH > 0 && h != null ? SpineUi.Battle(spot, h, spineH * 0.8f, true) : null;   // 스탠딩 · 상반신이 없을 때 — SD 전투 스파인(고정 배율), 미니미는 없앴다
            if (g != null) { g.gameObject.AddComponent<SceneHero.FixedScale>(); g.AnimationState.Update(phase); if (tint != null) g.color = tint.Value; return; }
            if (h?.Icon != null) { var face = Ui.Img(rt, h.Icon, tint ?? Color.white, "icon"); face.rectTransform.At(0.5f, 1, 0, 0, w * 1.08f, w * 1.08f); face.preserveAspect = true; }
        }

        // ═════════════════════════════ 편성 ═════════════════════════════
        void BuildPartyLight(RectTransform root, PartyState st)
        {
            Ui.Clear(root);
            RosterBg(root, 0.8f);
            PartySlotsNow = st.Slots;
            int count = st.Slots.Count(s => s != null);
            RosterHead(root, "팀 편성", () => VillageReveal(st.Village, () => Party(st.Village)));
            var size = Stage.Size;
            float side = Theme.C(440, 400);
            float top = 96, bottom = Theme.C(176, 150);   // 아래: 시작 덱 · 성격 줄 + 보드 요약 줄
            float leftW = size.x - side - Theme.Gutter * 3;
            float plateH = Theme.C(78, 66);
            float cardH = size.y - top - bottom - plateH - 10;
            float gap = Theme.C(22, 14);
            float cw = Mathf.Min((leftW - gap * 2) / 3f, cardH * 0.74f);
            float x0 = Theme.Gutter + (leftW - (cw * 3 + gap * 2)) / 2f;
            var slotRts = new RectTransform[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = Ui.Rect("slot" + i, root).At(0, 1, x0 + i * (cw + gap), -top, cw, cardH + plateH + 6);
                PartyCard(slot, st, i, cw, cardH, plateH, root);
                slotRts[i] = slot;
                if (!st.Quiet) Tw.Rise(slot, 0.05f + i * 0.07f, 30, 0.45f);
            }
            // 끌어서 자리 바꾸기 — 사도가 있는 칸만 끌 수 있다(빈 칸은 놓을 곳)
            for (int i = 0; i < 3; i++)
            {
                if (st.Slots[i] == null || !Stage.Hot.TryGetValue("slot" + i, out var cb) || cb == null) continue;
                var dg = cb.gameObject.AddComponent<PartySlotDrag>();
                dg.Slot = slotRts[i]; dg.Slots = slotRts; dg.Root = root; dg.Index = i;
                dg.OnSwap = (a, b) => SwapPartySlots(st, a, b, root);
            }
            st.Quiet = false;
            // 위 오른쪽 — 무작위 편성 · 프리셋 · 최근 편성
            var tools = Ui.Rect("partytools", root).At(1, 1, -Theme.Gutter, -20, 520, 52);
            Ui.Row(tools, 10, TextAnchor.MiddleRight, null, false, true);
            Stage.Hot["party.random"] = NavyPill(tools, "ic_refresh", "무작위 편성", () => PartyRandom(st, root), "random", 170);
            Stage.Hot["party.presets"] = NavyPill(tools, "ic_copy", "프리셋 · 최근 편성", () => PartyPresets(st, root), "presets", 230);
            var dragHint = Ui.Text(root, count > 0 ? "칸을 끌어 자리를 바꿀 수 있습니다" : "칸을 눌러 사도를 고릅니다", Theme.FsSm, Theme.Dim, TextAlignmentOptions.MidlineLeft);
            dragHint.rectTransform.At(0, 1, Theme.Gutter + 72 + 250, -34, 460, 24); dragHint.textWrappingMode = TextWrappingModes.NoWrap;

            // 아래 왼쪽 — 위 줄: 시작 덱(누르면 목록) · 성격 점 / 아래 줄: 크레파스 보드 요약 · 학년 안내(추천 편성 · 자리 안내는 없앴다 — 사용자 2026-10-06)
            float rowH = Theme.C(58, 52), boardH = Theme.C(52, 46), low = Theme.C(24, 18);
            PartyBoardRow(Ui.Rect("boardrow", root).At(0, 0, Theme.Gutter, low, leftW, boardH), leftW, boardH);
            var row = Ui.Rect("tools", root).At(0, 0, Theme.Gutter, low + boardH + 12, leftW, rowH);
            Ui.Row(row, 12, TextAnchor.MiddleLeft, null, false, true);
            var picked = st.Slots.Where(k => k != null).Select(Roster.ByKey).Where(h => h != null && h.Playable).ToList();
            var starter = picked.SelectMany(h => P.Data.Hero(h.CoreId)?.Starter ?? new List<string>()).Select(id => P.Data.Card(id)).Where(c => c != null).ToList();
            var costed = starter.Where(c => !c.X).ToList();
            var deckBtn = Btn.Make(row, null, BtnStyle.PillDark, () => PartyDeck(st), 0, "deck"); deckBtn.Pref(Theme.C(400, 360), rowH);
            deckBtn.Why = "사도를 먼저 편성하세요"; deckBtn.Interactable = starter.Count > 0;
            var deckChip = deckBtn.GetComponent<RectTransform>();
            var dic = Ui.Img(deckChip, Theme.S("ic_deck"), Theme.Gold, "ic"); dic.rectTransform.At(0, 0.5f, 18, 0, 26, 26); dic.preserveAspect = true;
            var dtt = Ui.Title(deckChip, $"<size=70%><color={Theme.SubTag}>시작 덱 {starter.Count}장 · 평균 비용</color></size>  <color={Theme.GoldTag}>{(costed.Count > 0 ? costed.Average(c => c.Cost).ToString("0.0") : "—")}</color>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            dtt.rectTransform.Fill(54, 0, 40, 0); dtt.textWrappingMode = TextWrappingModes.NoWrap;
            var dar = Ui.Title(deckChip, "›", Theme.FsXl, Theme.Gold, TextAlignmentOptions.MidlineRight); dar.rectTransform.Fill(0, 0, 16, 0);
            Stage.Hot["party.deck"] = deckBtn;
            foreach (var g in picked.GroupBy(h => h.nature))
            {
                var nc = Ui.Img(row, Theme.S("pill_dark", 46), Color.white.A(0.94f), "nat"); nc.Pref(96, rowH);
                var ni = Ui.Img(nc.transform, Icon("성격_" + g.Key), Color.white, "ic"); ni.rectTransform.At(0, 0.5f, 14, 0, 28, 28); ni.preserveAspect = true;
                var nt = Ui.Title(nc.transform, "×" + g.Count(), Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft); nt.rectTransform.Fill(50, 0, 6, 0);
            }

            // 오른쪽 — 이번 층
            var right = NavyBox(root, "floor");
            right.At(1, 1, -Theme.Gutter, -top + 4, side, size.y - top - Theme.C(104, 92) - 2);   // 오른쪽 판은 예전 높이 그대로(아래 보드 줄은 왼쪽 몫)
            FloorPanelLight(right, st.Village, side);
            Tw.Rise(right, 0.1f, 30, 0.45f, Vector2.right);

            // 「모험 시작」 — 주 동작(금 알약), 왼쪽 동그라미 아이콘
            var go = Btn.Make(root, null, BtnStyle.PillGold, () =>
            {
                var party = st.Slots.Select(k => Roster.ByKey(k).CoreId).Reverse().ToList();   // 화면 왼쪽(후열) → 오른쪽(전열). 코어 파티 목록은 전열(맨 오른쪽)이 첫째
                PartyStore.SetRecent(st.Slots);   // 「최근 편성」
                RunPort.ClearSave();
                P.NewRun(party, st.Village, FoeNature != null ? FoeSeed : DateTime.Now.Ticks & 0x7fffffff, FoeNature);
                MapStep();
            }, 0, "go");
            var grt = go.GetComponent<RectTransform>(); grt.At(1, 0, -Theme.Gutter, Theme.C(20, 14), side, Theme.C(76, 66));
            var disc = Ui.Img(grt, Theme.S("circle"), Theme.Brown.A(0.16f), "disc"); disc.rectTransform.At(0, 0.5f, 14, 0, 50, 50);
            var dring = Ui.Img(disc.transform, Theme.S("ring"), Theme.Brown.A(0.5f), "ring"); dring.rectTransform.Fill();
            var dp = Ui.Img(disc.transform, Theme.S("ic_play"), Theme.Brown, "ic"); dp.rectTransform.Fill(15, 14, 13, 14); dp.preserveAspect = true;
            var gl = Ui.Title(grt, count < 3 ? $"사도 {3 - count}명 더" : "모험 시작", Theme.FsXl, Theme.Brown, TextAlignmentOptions.MidlineRight);
            gl.rectTransform.Fill(80, 4, 34, 4);
            go.Label = gl;
            go.Interactable = count == 3;
            go.Why = "사도 셋을 고르세요";
            Stage.Hot["party.go"] = go;
            if (count == 3) Tw.Breathe(go.transform, 0.015f, 1.4f);
        }

        // 큰 세로 사도 카드(스탠딩) + 이어진 수치 판
        void PartyCard(RectTransform slot, PartyState st, int i, float w, float h, float plateH, RectTransform root)
        {
            var key = st.Slots[i];
            var hero = key != null ? Roster.ByKey(key) : null;
            var b = Btn.Make(slot, null, BtnStyle.Ghost, () => HeroList(st, i, () => BuildPartyLight(root, st)), 0, "card" + i);
            b.Bg.sprite = Theme.Round;
            b.NoScale = true;   // 올려도 커지지 않는다 — 커지면 아래 판(이름 · 「눌러서 사도 목록에서 고릅니다」)과 겹친다(사용자 2026-10-07). 밝은 테두리(Hl)만
            var rt = b.GetComponent<RectTransform>(); rt.At(0, 1, 0, 0, w, h);
            Stage.Hot["slot" + i] = b;
            void HoverRim() { var hl = Ui.Img(rt, Theme.Frame, Theme.Gold.A(0), "hl"); hl.rectTransform.Fill(-2, -2, -2, -2); hl.raycastTarget = false; b.Hl = hl; }
            if (hero == null)
            {
                b.SetColor(Theme.NavyCell.A(0.85f));
                var rim0 = Ui.Img(rt, Theme.Frame, Theme.Edge.A(0.45f), "rim"); rim0.rectTransform.Fill();
                var plus = Ui.Img(rt, Theme.S("circle"), Theme.Gold, "plus"); plus.rectTransform.At(0.5f, 0.5f, 0, 30, 76, 76);
                var pi = Ui.Img(plus.transform, Theme.S("ic_plus"), Theme.Brown, "ic"); pi.rectTransform.Fill(20, 20, 20, 20);
                Tw.Breathe(plus.transform, 0.05f, 1.5f, i * 0.3f);
                var t0 = Ui.Title(rt, "사도 넣기", Theme.FsLg, Theme.Ink, TextAlignmentOptions.Center); t0.rectTransform.At(0.5f, 0.5f, 0, -36, 300, 34);
                var r0 = Ui.Title(rt, RowNames[i], Theme.Fs2xl, Theme.Dim, TextAlignmentOptions.BottomLeft); r0.rectTransform.At(0, 0, 18, 12, 200, 56);
                var plate0 = NavyBox(slot, "plate", 0.75f); plate0.At(0, 1, 0, -(h + 6), w, plateH);
                var pt0 = Ui.Text(plate0, "눌러서 사도 목록에서 고릅니다", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center); pt0.rectTransform.Fill(10, 0, 10, 0);
                HoverRim();
                return;
            }
            var nc = NatureCol(hero);
            b.SetColor(Color.Lerp(Theme.NavyWell, nc, 0.3f));
            var glow = Ui.Img(rt, Theme.S("fade_top"), nc.A(0.5f), "glow"); glow.rectTransform.Fill(2, 2, 2, 2);
            var halo = Ui.Img(rt, Theme.S("soft"), Color.Lerp(nc, Color.white, 0.4f).A(0.28f), "halo"); halo.rectTransform.At(0.5f, 0.5f, 0, 40, w * 1.3f, w * 1.3f);
            var mask = Ui.Img(rt, Theme.Round, Color.white, "mask"); mask.rectTransform.Fill(3, 3, 3, 3); mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            // 스탠딩 — 무릎께까지(카드가 키가 크다)
            HeroArt(mask.rectTransform, hero, w - 6, h - 6, 0.72f, Mathf.Min(h * 0.46f, 300), i * 0.37f, null, true, false, StandingFit.Tallest(st.Slots.Where(x => x != null).Select(x => Roster.ByKey(x)?.art)));
            var shade = Ui.Img(rt, Theme.S("fade_down"), Color.black.A(0.72f), "shade"); shade.rectTransform.Band(0, h * 0.32f, 3, 3, 3);
            var shadeT = Ui.Img(rt, Theme.S("fade_top"), Color.black.A(0.35f), "shadeT"); shadeT.rectTransform.Band(1, 120, 3, 3, -3);
            // 왼쪽 위 세로 아이콘 열 — 역할 · 성격
            var col = Ui.Rect("icons", rt).At(0, 1, 12, -12, 34, 200);
            Ui.Col(col, 6, TextAnchor.UpperLeft, null, false, false);
            foreach (var k in new[] { "역할_" + hero.role, "성격_" + hero.nature })
            {
                var sp = Icon(k); if (sp == null) continue;
                var ic = Ui.Img(col, sp, Color.white, k); ic.Pref(32, 32); ic.preserveAspect = true;
            }
            // 오른쪽 위 돋보기
            var zoom = Btn.Icon(rt, Theme.S("ic_zoom"), () => HeroDetail(key, st.Slots.Where(x => x != null).ToList(), () => BuildPartyLight(root, st)), 44, "zoom");
            zoom.GetComponent<RectTransform>().At(1, 1, -10, -10, 44, 44);
            Stage.Hot["zoom" + i] = zoom;
            // 고유 효과 · 패시브 · 고학년 — 누르면 판 묶음(수치가 다 든 글 · 길면 스크롤)
            if (hero.Playable)
            {
                var tc = TraitsChip(rt, hero.CoreId, "고유 효과", "traits" + i);
                var trt = tc.GetComponent<RectTransform>(); trt.anchorMin = trt.anchorMax = trt.pivot = new Vector2(1, 1); trt.anchoredPosition = new Vector2(-62, -15);
                Stage.Hot["party.traits" + i] = tc;
            }
            // 왼쪽 아래 자리 번호(편성 순서) · 오른쪽 아래 장비 칸(빈 칸)
            var rowT = Ui.Title(rt, RowNames[i], Theme.Fs2xl, Color.white, TextAlignmentOptions.BottomLeft);
            rowT.rectTransform.At(0, 0, 16, 10, 160, 56); rowT.Outline(0.22f);
            for (int s = 0; s < RunPort.Slots.Length; s++)
            {
                var box = Ui.Img(rt, Theme.Round, Color.black.A(0.45f), "gear" + s); box.rectTransform.At(1, 0, -12 - (2 - s) * 40, 14, 34, 34);
                var gi = Ui.Img(box.transform, W.SlotIcon(RunPort.Slots[s]), Color.white.A(0.4f), "ic"); gi.rectTransform.Fill(8, 8, 8, 8); gi.preserveAspect = true;
            }
            var rim = Ui.Img(rt, Theme.Frame, nc, "rim"); rim.rectTransform.Fill();
            HoverRim();
            // 이어진 수치 판 — 이름 · 핵심 수치
            var plate = NavyBox(slot, "plate"); plate.At(0, 1, 0, -(h + 6), w, plateH);
            var stripe = Ui.Img(plate, Theme.Round, nc, "stripe"); stripe.rectTransform.At(0, 0.5f, 10, 0, 5, plateH - 24);
            var d = P.Data.Hero(hero.CoreId);
            var nm = Ui.Title(plate, $"{hero.ko}  <size=70%><color={Theme.GoldTag}>{new string('★', Mathf.Clamp(hero.star, 1, 5))}</color></size>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.TopLeft); nm.rectTransform.Fill(24, plateH * 0.45f, PartyStore.Cleared(hero.key) > 0 ? 84 : 10, 9);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 14; nm.fontSizeMax = Theme.FsLg;
            ClearBadge(plate, hero.key, -10, -10);
            var stt = Ui.Text(plate, $"HP <b>{d?.Hp ?? hero.hp:N0}</b>   공격 <b>{d?.Atk ?? hero.atk}</b>   방어 <b>{d?.Def ?? hero.def}</b>", Theme.FsSm, Theme.Sub, TextAlignmentOptions.BottomLeft);
            stt.rectTransform.Fill(24, 9, 10, plateH * 0.5f); stt.textWrappingMode = TextWrappingModes.NoWrap; stt.enableAutoSizing = true; stt.fontSizeMin = 11; stt.fontSizeMax = Theme.FsSm;
        }

        void FloorPanelLight(RectTransform panel, string villageId, float w)
        {
            var v = P.Village(villageId);
            var f = v.Floors[0];
            var body = Ui.Rect("body", panel).Fill(24, 18, 24, 20);
            Ui.Col(body, 8, TextAnchor.UpperLeft, null, true, false);
            var chip = Ui.Text(body, $"{v.Name} · {v.Race} 마을", Theme.FsSm, Theme.Sub); chip.Pref(-1, 22);
            var t = Ui.Title(body, $"1층 · {f.Name}", Theme.FsXl + 2, Theme.Gold); t.Pref(-1, 44);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 18; t.fontSizeMax = Theme.FsXl + 2;
            void Band(string label, string val, string icon)
            {
                var b = Ui.Img(body, Theme.Round, Theme.NavyWell.A(0.7f), "band"); b.Pref(-1, 40);
                if (icon != null) { var ic = Ui.Img(b.transform, Theme.S(icon), Theme.Gold, "ic"); ic.rectTransform.At(0, 0.5f, 12, 0, 22, 22); ic.preserveAspect = true; }
                var l = Ui.Title(b.transform, label, Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft); l.rectTransform.Fill(icon != null ? 44 : 14, 0, 0, 0);
                if (val != null) { var r = Ui.Text(b.transform, val, Theme.FsSm, Theme.Sub, TextAlignmentOptions.MidlineRight); r.rectTransform.Fill(0, 0, 14, 0); r.textWrappingMode = TextWrappingModes.NoWrap; }
            }
            Band("층의 모습", f.Sub, "ic_flag");
            if (!string.IsNullOrEmpty(FoeNature))
            {
                // 이번 판 적 속성 + 층 보스 초상 — 약점에 맞춰 파티를 짜도록
                FoeNatureBand(body, FoeNature);
                BossCardsRow(body, v, FoeNature, w);
            }
            else
            {
                Band("보스", null, "ic_crown");
                var bossRow = Ui.Rect("boss", body); bossRow.Pref(-1, 78);
                Ui.Row(bossRow, 10, TextAnchor.MiddleLeft, new RectOffset(4, 0, 0, 0), false, false);
                foreach (var id in f.Boss.Distinct()) FoeCell(bossRow, id, "보스");
            }
            // 나오는 적 — 층마다 가로 줄(보스 · 엘리트 · 일반 순, 그 판 적 속성 그림). 누르면 적 도감 상세(창이라 편성은 그대로 남는다)
            int total = v.Floors.Select((fl, fi) => FloorFoes(fl, RunBossLine(v, fi))).SelectMany(x => x).Select(x => x.id).Distinct().Count();
            Band("나오는 적", total + "종 · 누르면 도감", "ic_skull");
            for (int fi = 0; fi < Mathf.Min(2, v.Floors.Count); fi++) FoeStrip(body, v, fi);
            var line = Ui.Text(body, v.Line ?? "", Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft); line.Pref(-1, Theme.C(40, 0));
            line.enableAutoSizing = true; line.fontSizeMin = 11; line.fontSizeMax = Theme.FsSm;
            if (Theme.Compact) line.gameObject.SetActive(false);
        }

        /// <summary>그 층 보스 줄 — 이번 모험이 고른 것(마을 공개 때 Run.PickBosses — 판 속성의 사도 클론), 없으면 층 데이터 그대로.</summary>
        List<string> RunBossLine(Core.VillageDef v, int floor)
            => FoeBosses != null && floor < FoeBosses.Count && FoeBosses[floor] != null && FoeBosses[floor].Count > 0 ? FoeBosses[floor] : v.Floors[floor].Boss;

        /// <summary>한 층의 적(보스 → 엘리트 → 일반, 같은 적은 한 번). boss 를 주면 보스 줄은 그것(이번 모험의 클론).</summary>
        static List<(string id, string grade)> FloorFoes(Core.FloorDef fl, List<string> boss = null)
        {
            var o = new List<(string, string)>();
            var seen = new HashSet<string>();
            foreach (var id in boss ?? fl.Boss) if (seen.Add(id)) o.Add((id, "보스"));
            foreach (var id in fl.Elites.SelectMany(x => x)) if (seen.Add(id)) o.Add((id, "엘리트"));
            foreach (var id in fl.Pools.SelectMany(p => p).SelectMany(x => x)) if (seen.Add(id)) o.Add((id, "일반"));
            return o;
        }

        /// <summary>층 한 줄 — 왼쪽 「1층」 표 · 가로 스크롤 적 칸.</summary>
        void FoeStrip(RectTransform body, Core.VillageDef v, int floor)
        {
            var fl = v.Floors[floor];
            float cellW = Theme.C(70, 54), cellH = Theme.C(86, 62);
            var row = Ui.Rect("floor" + floor, body); row.Pref(-1, cellH + 6);
            var tag = Ui.Title(row, $"{floor + 1}층", Theme.FsSm, Theme.Gold, TextAlignmentOptions.Center, "tag"); tag.rectTransform.At(0, 0.5f, 0, 6, 34, 30);
            var area = Ui.Rect("strip", row).Fill(38, 0, 0, 0);
            var content = Ui.Scroll(area, out var sr, true);
            Ui.Row(content, 6, TextAnchor.MiddleLeft, new RectOffset(2, 8, 0, 0), false, false);
            foreach (var (id, grade) in FloorFoes(fl, RunBossLine(v, floor))) FoeCell(content, id, grade, cellW, cellH, v.Id, floor);
        }

        /// <summary>적 칸 — 그 판 적 속성 그림(클론은 자기 성격) · 등급 테(보스 금 · 엘리트 보라 · 일반 회색) · 등급 표식 · 이름. 누르면 적 도감 상세.</summary>
        void FoeCell(RectTransform parent, string id, string grade, float cw = 70, float chh = 86, string village = null, int floor = 0)
        {
            var e = P.Data.Enemy(id);
            bool clone = e?.Clone != null;
            string nat = clone ? e.Nature ?? FoeNature : RunPort.NatureIn(e, FoeNature);   // 이번 모험은 모든 적이 판의 적 속성 — 클론은 자기 성격(= 그 속성으로 골라진 사도)
            var gc = FoeGradeColor(grade);
            var b = Btn.Make(parent, null, BtnStyle.Ghost, () => { if (e == null) return; if (clone && village != null) RunCloneDetail(e, village, floor); else EnemyDetail(id); }, 0, "foe " + id);
            b.Bg.color = Color.clear; b.SetColor(Color.clear);
            b.Pref(cw, chh);
            var cell = b.GetComponent<RectTransform>();
            float ds = cw - 18;   // 그림을 조금 줄여 아래 이름 칸을 두 줄로(긴 몬스터 이름이 말줄임 없이 다 읽히게)
            var disc = Ui.Img(cell, Theme.S("circle"), Color.Lerp(Theme.NavyWell, Theme.NatureCardOf(nat), 0.3f), "disc"); disc.rectTransform.At(0.5f, 1, 0, -2, ds, ds);
            disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var cloneHero = clone ? BossHero(e) : null;   // 클론 — 이번 모험의 그 사도 얼굴(원래 보스 몸 그림이 아니라)
            var face = cloneHero != null ? CardArt.Upper(cloneHero.art, 1f, 0.34f) : null;
            var art = face ?? FoeArtFor(e, nat);
            if (face != null) { var im = Ui.Img(disc.transform, face, Color.white, "art"); im.rectTransform.Fill(); }
            else if (art != null) { var im = Ui.Img(disc.transform, art, Color.white, "art"); im.rectTransform.Fill(-2, -2, -2, -2); im.preserveAspect = true; }
            else if (FoeSilhouette(e) is Sprite sil) { var im = Ui.Img(disc.transform, sil, new Color(0.04f, 0.05f, 0.1f, 0.85f), "silhouette"); im.rectTransform.Fill(-2, -2, -2, -2); im.preserveAspect = true; }   // 그림 없는 적 — 같은 몬스터 실루엣
            else { var ic = Ui.Img(disc.transform, Theme.S(grade == "보스" ? "ic_crown" : "ic_skull"), Color.white.A(0.9f), "ic"); ic.rectTransform.Fill(ds * 0.24f, ds * 0.24f, ds * 0.24f, ds * 0.24f); ic.preserveAspect = true; }
            var ring = Ui.Img(cell, Theme.S("ring"), gc, "ring"); ring.rectTransform.At(0.5f, 1, 0, -2, ds, ds);
            // 등급 표식(위 왼쪽) · 성격(아래 오른쪽)
            var gb = Ui.Img(cell, Theme.Pill, gc, "grade"); gb.rectTransform.At(0, 1, 0, 0, 26, 16);
            var gt = Ui.Title(gb.transform, grade == "보스" ? "보스" : grade == "엘리트" ? "엘" : "일", 10, Theme.Brown, TextAlignmentOptions.Center); gt.rectTransform.Fill();
            var ni = Icon("성격_" + nat);
            if (ni != null) { var nb = Ui.Img(cell, ni, Color.white, "nat"); nb.rectTransform.At(1, 1, 0, -(ds - 18), 20, 20); nb.preserveAspect = true; }
            // 이름 — 「셰이디아 극성팬 · 메가폰」 같은 두 마디 이름은 마디마다 한 줄, 그 밖에는 칸 너비에서 줄바꿈 · 자동 크기(말줄임 없음)
            string label = cloneHero != null ? cloneHero.ko + " 클론" : (e?.Name ?? id).Replace(" · ", "\n");
            var nm = Ui.Text(cell, label, Theme.FsCap - 2, Theme.Ink, TextAlignmentOptions.Top); nm.rectTransform.At(0.5f, 0, 0, 0, cw, chh - ds - 2);
            nm.textWrappingMode = TextWrappingModes.Normal; nm.overflowMode = TextOverflowModes.Overflow; nm.lineSpacing = -14;
            nm.enableAutoSizing = true; nm.fontSizeMin = 7; nm.fontSizeMax = Theme.FsCap - 3;
            Stage.Hot["partyfoe:" + id] = b;
        }

        /// <summary>이번 모험의 보스 클론 → 적 도감의 그 클론 상세(이 마을 · 이 층). 색인에 없으면 적 상세로.</summary>
        void RunCloneDetail(Core.EnemyDef e, string village, int floor)
        {
            var r = CloneIndex().FirstOrDefault(x => x.Hero == e.Clone && x.Village == village);
            if (r == null) { EnemyDetail(e.Id); return; }
            CloneDetail(r, r.Floors.Contains(floor) ? floor : r.Floors.Max);
        }
        // ═════════════════════════════ 사도 목록 ═════════════════════════════
        // 목록 상태 — 다시 그려도(파티 넣기 · 상세에서 돌아오기) 필터 · 정렬 · 고른 사도 · 스크롤 자리를 그대로 둔다.
        class ListState { public string Nature; public string Sort = "성급"; public bool Quick = true; public string Focus; public float ScrollY; public bool Drawn; }
        static readonly string[] Sorts = { "성급", "이름", "역할", "종족" };

        /// <summary>정렬 — 성급(높은 별 먼저) · 이름 · 역할 · 종족, 같으면 가나다순.</summary>
        static IEnumerable<HeroInfo> SortHeroes(IEnumerable<HeroInfo> list, string sort) => sort switch
        {
            "이름" => list.OrderBy(h => h.ko, StringComparer.Ordinal),
            "역할" => list.OrderBy(h => h.role, StringComparer.Ordinal).ThenBy(h => h.ko, StringComparer.Ordinal),
            "종족" => list.OrderBy(h => h.race, StringComparer.Ordinal).ThenBy(h => h.ko, StringComparer.Ordinal),
            _ => list.OrderByDescending(h => h.star).ThenBy(h => h.ko, StringComparer.Ordinal),
        };

        /// <summary>사도 목록 — st 가 있으면 편성(target 칸에 넣기 · 빼기), 없으면 도감(보기만). done 은 닫을 때.</summary>
        void HeroList(PartyState st, int target, Action done, ListState ls = null, RectTransform host = null)
        {
            ls ??= new ListState { Focus = st != null && target >= 0 ? st.Slots[target] : null };
            var size = Stage.Size;
            // host 가 있으면 그 화면에 그린다(도감), 없으면 창 층에 덮는다(편성)
            var layer = host != null ? host : Ui.Rect("modal herolist", Stage.ModalLayer).Fill();
            if (host != null) Ui.Clear(host);
            RosterBg(layer, host != null ? 0.9f : 1f);
            void Close() { if (host == null && layer) Destroy(layer.gameObject); done?.Invoke(); }
            void Rebuild() { if (host == null && layer) Destroy(layer.gameObject); HeroList(st, target, done, ls, host); }
            void Refilter() { ls.ScrollY = 0; Rebuild(); }
            bool dex = st == null;
            RosterHead(layer, "사도 목록", Close);

            // 위 오른쪽 — 빠른 편성 · 정렬 · (도감) 교주 카드 · 장비
            var tr = Ui.Rect("tools", layer).At(1, 1, -Theme.Gutter, -20, 1240, 52);
            Ui.Row(tr, 10, TextAnchor.MiddleRight, null, false, true);
            if (!dex)
            {
                var q = Btn.Make(tr, null, BtnStyle.PillDark, () => { ls.Quick = !ls.Quick; Rebuild(); }, 0, "quick");
                Ui.Row(q.GetComponent<RectTransform>(), 8, TextAnchor.MiddleCenter, new RectOffset(18, 12, 6, 6), false, false);
                var qt = Ui.Title(q.transform, "빠른 편성", Theme.FsBody, Theme.Ink); qt.textWrappingMode = TextWrappingModes.NoWrap;
                var pill = Ui.Img(q.transform, Theme.Pill, ls.Quick ? Theme.Gold : Theme.NavyWell, "sw"); pill.Pref(48, 26);
                var knob = Ui.Img(pill.transform, Theme.S("circle"), ls.Quick ? Theme.Brown : Theme.Dim, "knob"); knob.rectTransform.At(ls.Quick ? 1 : 0, 0.5f, ls.Quick ? -3 : 3, 0, 20, 20);
                q.Pref(-1, 52);
                Stage.Hot["list.quick"] = q;
            }
            var sb = NavyPill(tr, "ic_refresh", $"<color={Theme.SubTag}>정렬</color>  {ls.Sort}", () => { ls.Sort = Sorts[(Array.IndexOf(Sorts, ls.Sort) + 1) % Sorts.Length]; Refilter(); }, "sort", 168);
            Stage.Hot["list.sort"] = sb;
            if (dex)
            {
                DexTabs(tr, host, "사도", done);   // 도감 탭 — 같은 화면 틀(Flow.Dex.cs)
            }

            // 왼쪽 세로 성격 탭 — ALL + 다섯(덱 보기 탭 줄과 같은 꼴: 고른 탭은 금빛 바탕 + 왼쪽 금 막대)
            float railW = 76;
            var rail = Ui.Rect("rail", layer); rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(0, 1); rail.pivot = new Vector2(0, 1);
            rail.sizeDelta = new Vector2(railW, -110); rail.anchoredPosition = new Vector2(Theme.Gutter, -96);
            Ui.Col(rail, 10, TextAnchor.UpperCenter, null, false, false);
            void Tab(string nat, string label)
            {
                bool on = ls.Nature == nat;
                var b = Btn.Make(rail, null, BtnStyle.Ghost, () => { ls.Nature = nat; Refilter(); }, 0, "tab " + label);
                b.Bg.sprite = Theme.Round; b.SetColor(on ? Theme.Gold.A(0.16f) : Theme.NavyCell.A(0.6f));
                b.Pref(railW, 62);
                if (nat == null) { var t = Ui.Title(b.transform, "ALL", Theme.FsLg, on ? Theme.Gold : Theme.Sub, TextAlignmentOptions.Center); t.rectTransform.Fill(); }
                else { var ic = Ui.Img(b.transform, Icon("성격_" + nat), on ? Color.white : Color.white.A(0.5f), "ic"); ic.rectTransform.Fill(14, 12, 14, 12); ic.preserveAspect = true; }
                if (on)
                {
                    var br = Ui.Img(b.transform, Theme.Frame, Theme.Gold, "on"); br.rectTransform.Fill();
                    var bar = Ui.Img(b.transform, Theme.Round, Theme.Gold, "bar"); bar.rectTransform.At(0, 0.5f, -10, 0, 4, 36);
                }
                Stage.Hot["nat:" + label] = b;
            }
            Tab(null, "ALL");
            foreach (var n in new[] { "순수", "광기", "냉정", "우울", "활발" }) Tab(n, n);

            // 카드 격자
            float footH = Theme.C(84, 72);
            var area = Ui.Rect("grid", layer); area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Theme.Gutter + railW + 16, footH + 8); area.offsetMax = new Vector2(-Theme.Gutter, -92);
            var content = Ui.Scroll(area, out var sr);
            ScrollBar(area, sr);
            float k = Theme.C(1f, 0.86f);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(150 * k, 214 * k); grid.spacing = new Vector2(14, 14); grid.padding = new RectOffset(6, 18, 8, 16);
            grid.childAlignment = TextAnchor.UpperLeft;
            var list = SortHeroes(Roster.All.Where(h => ls.Nature == null || h.nature == ls.Nature), ls.Sort);
            if (!dex) list = list.OrderByDescending(h => h.Playable);   // 고를 수 있는 사도 먼저(안정 정렬 — 정렬 순서는 그대로)
            var shown = list.ToList();
            var shownKeys = shown.Select(x => x.key).ToList();
            bool anim = !ls.Drawn;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            // 칸 자리(빈 RectTransform — 이름 「hero <키>」 는 검색이 읽는다)를 먼저 다 놓고, 카드는 보이는 줄부터 — 보이는 줄 + 한 줄은 지금,
            //   나머지는 다음 프레임들에 FillPerFrame 장씩(2026-10-07 「사도 목록이 무겁다」 — 135칸을 한 프레임에 다 세우면 웹에서 100ms 넘게 멈췄다.
            //   편성 목록은 누를 때마다 다시 그린다). 격자 자리 · 스크롤 높이는 처음부터 그대로라 스크롤 되돌리기 · 검색이 그대로 된다
            var cells = new RectTransform[shown.Count];
            for (int i = 0; i < shown.Count; i++) cells[i] = Ui.Rect("hero " + shown[i].key, content);
            // 스크롤 자리 — 다시 그려도 보던 자리 그대로(첫 누름이 엉뚱한 칸에 가지 않게)
            if (ls.ScrollY > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                float maxY = Mathf.Max(0, content.rect.height - ((RectTransform)sr.viewport).rect.height);
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Min(ls.ScrollY, maxY));
                sr.StopMovement();
            }
            {
                var vp = (RectTransform)sr.viewport;
                float cellW = grid.cellSize.x + grid.spacing.x, cellH = grid.cellSize.y + grid.spacing.y;
                int cols = Mathf.Max(1, Mathf.FloorToInt((vp.rect.width - grid.padding.horizontal + grid.spacing.x) / cellW));
                float y0 = Mathf.Max(0, content.anchoredPosition.y) - grid.padding.top;
                int r0 = Mathf.Max(0, Mathf.FloorToInt(y0 / cellH)), r1 = Mathf.Max(r0 + 1, Mathf.CeilToInt((y0 + Mathf.Max(vp.rect.height, cellH * 3)) / cellH));
                int Dist(int i) { int r = i / cols; return r < r0 ? (r0 - r) * 2 + 1 : r > r1 ? (r - r1) * 2 : 0; }
                var later = new List<int>();
                for (int i = 0; i < shown.Count; i++)
                {
                    if (Dist(i) == 0) ListCard(cells[i], shown[i], st, ls, target, k, i, anim, Rebuild, shownKeys);
                    else later.Add(i);
                }
                later.Sort((a, b) => Dist(a) != Dist(b) ? Dist(a).CompareTo(Dist(b)) : a.CompareTo(b));
                if (later.Count > 0) StartCoroutine(FillCards(content, later.Select(i => (Action)(() => { if (cells[i]) ListCard(cells[i], shown[i], st, ls, target, k, i, false, Rebuild, shownKeys); })).ToList()));
            }
            if (Demo.Active) Debug.Log($"[ListPerf] 사도 목록 칸 {shown.Count} 만들기 {sw.Elapsed.TotalMilliseconds:F0}ms");
            ls.Drawn = true;
            sr.onValueChanged.AddListener(_ => { if (content) ls.ScrollY = content.anchoredPosition.y; });

            // 아래 — 남색 띠 · 가는 금 선 · 고른 사도 · 「완료」 · 「상세 정보」 · 「편성」
            var fbg = Ui.Img(layer, Theme.White, Theme.NavyPanel.A(0.96f), "footbg", true); fbg.rectTransform.Band(0, footH, 0, 0, 0);
            var line = Ui.Img(layer, Theme.White, Theme.Edge.A(0.35f), "rule"); line.rectTransform.Band(0, 1, 0, 0, footH);
            var foot = Ui.Rect("foot", layer).Band(0, footH, Theme.Gutter, Theme.Gutter, 0);
            var fh = ls.Focus != null ? Roster.ByKey(ls.Focus) : null;
            var info = Ui.Text(foot, fh != null ? $"<b>{fh.ko}</b>  <color={Theme.SubTag}>{fh.nature} · {fh.race} · {fh.role}</color>" : (dex ? $"{shown.Count}명 · 누르면 상세 정보" : $"{shown.Count}명 · 파티 <color={Theme.GoldTag}>{st.Slots.Count(x => x != null)}</color> / 3"), Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.rectTransform.Fill(0, 0, dex ? 280 : 720, 0); info.textWrappingMode = TextWrappingModes.NoWrap; info.overflowMode = TextOverflowModes.Ellipsis;
            var detail = Btn.Make(foot, null, BtnStyle.PillDark, () => { if (fh != null) HeroDetail(fh.key, shownKeys, null); }, 0, "detail");
            var drt = detail.GetComponent<RectTransform>(); drt.At(1, 0.5f, dex ? 0 : -274, 0, 250, 58);
            var dzi = Ui.Img(drt, Theme.S("ic_zoom"), Theme.Gold, "ic"); dzi.rectTransform.At(0, 0.5f, 22, 0, 24, 24); dzi.preserveAspect = true;
            var dl = Ui.Title(drt, "상세 정보", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineRight); dl.rectTransform.Fill(50, 0, 26, 0);
            detail.Interactable = fh != null; detail.Why = "사도를 먼저 고르세요";
            Stage.Hot["list.detail"] = detail;
            // 도감 — 검색 칸(이름 · 초성 · 괄호 이름, 도감마다 따로 기억)
            if (dex) DexSearch(tr, "사도", content, area, n => n.StartsWith("hero ") && Roster.ByKey(n.Substring(5)) is HeroInfo hi0 ? new[] { hi0.ko, hi0.key } : null,
                fh == null ? info : null, n => $"{n}명 · 누르면 상세 정보");
            if (!dex)
            {
                bool inParty = fh != null && st.Slots.Contains(fh.key);
                var pick = Btn.Make(foot, null, inParty ? BtnStyle.PillRose : BtnStyle.PillDark, () =>
                {
                    if (fh == null) return;
                    TogglePartyHero(st, fh, target);
                    Rebuild();
                }, 0, "pick");
                var prt = pick.GetComponent<RectTransform>(); prt.At(1, 0.5f, 0, 0, 260, 58);
                var pl = Ui.Title(prt, fh == null ? "편성" : inParty ? "빼기" : "편성", Theme.FsLg, Theme.Ink, TextAlignmentOptions.Center); pl.rectTransform.Fill();
                pick.Interactable = fh != null && (fh.Playable || inParty); pick.Why = fh == null ? "사도를 먼저 고르세요" : $"{fh.ko} — 아직 판에 데려갈 수 없습니다(코어 데이터 준비 중)";
                Stage.Hot["list.pick"] = pick;
                var done2 = Btn.Make(foot, null, BtnStyle.PillGold, Close, 0, "done");
                var d2 = done2.GetComponent<RectTransform>(); d2.At(1, 0.5f, -548, 0, 150, 58);
                var dck = Ui.Img(d2, Theme.S("ic_check"), Theme.Brown, "ic"); dck.rectTransform.At(0, 0.5f, 20, 0, 22, 22); dck.preserveAspect = true;
                var dtl = Ui.Title(d2, "완료", Theme.FsLg, Theme.Brown, TextAlignmentOptions.MidlineRight); dtl.rectTransform.Fill(46, 0, 24, 0);
                Stage.Hot["list.done"] = done2;
            }
        }

        /// <summary>파티에 넣기 · 빼기 — target 칸이 비었으면 그 칸에, 아니면 앞쪽 빈 칸.</summary>
        void TogglePartyHero(PartyState st, HeroInfo h, int target)
        {
            int at = Array.IndexOf(st.Slots, h.key);
            if (at >= 0) { st.Slots[at] = null; return; }
            if (!h.Playable) { Toast.Show($"{h.ko} — 아직 판에 데려갈 수 없습니다(코어 데이터 준비 중)"); return; }
            int slot = target >= 0 && st.Slots[target] == null ? target : Array.IndexOf(st.Slots, null);
            if (slot < 0 && target >= 0) slot = target;   // 칸을 눌러 열었으면 그 칸을 바꾼다
            if (slot >= 0)
            {
                st.Slots[slot] = h.key;
                HeroVoice.Speak(h);   // 칸에 넣을 때 그 사도의 목소리 한 번(앞 소리는 끊긴다)
                Toast.Show($"<color={Theme.GoldTag}>{h.ko}</color>  {HeroLines.Pick(h, "party")}");   // 그 사도 말투로 한마디(hero_lines.json)
            }
            else Toast.Show("세 칸이 다 찼습니다 — 넣은 사도를 눌러 빼세요");
        }

        /// <summary>목록 카드 나머지를 프레임마다 FillPerFrame 장씩(HeroList — 보이는 줄은 이미 세웠다). 목록이 닫히면(content 없음) 멈춘다.</summary>
        const int FillPerFrame = 12;
        System.Collections.IEnumerator FillCards(RectTransform content, List<Action> jobs)
        {
            int i = 0;
            while (i < jobs.Count)
            {
                yield return null;
                if (!content) yield break;
                for (int n = 0; n < FillPerFrame && i < jobs.Count; n++) jobs[i++]();
            }
        }

        void ListCard(RectTransform content, HeroInfo h, PartyState st, ListState ls, int target, float k, int idx, bool anim, Action rebuild, List<string> shownKeys)
        {
            bool dex = st == null;
            int at = dex ? -1 : Array.IndexOf(st.Slots, h.key);
            bool locked = !dex && !h.Playable;
            var nc = NatureCol(h);
            var b = Btn.Make(content, null, BtnStyle.Ghost, () =>
            {
                ls.Focus = h.key;
                // 도감 — 한 번 누르면 바로 상세(목록은 고른 사도 · 스크롤 그대로 뒤에 남는다)
                if (dex) { rebuild(); HeroDetail(h.key, shownKeys, null); return; }
                if (ls.Quick && !locked) TogglePartyHero(st, h, target);
                else if (locked) Toast.Show($"{h.ko} — 아직 판에 데려갈 수 없습니다(코어 데이터 준비 중)");
                rebuild();
            }, 0, "hero " + h.key);
            b.Bg.sprite = Theme.Round; b.SetColor(locked ? Theme.NavyCell : Color.Lerp(Theme.NavyWell, nc, 0.32f));
            var rt = b.GetComponent<RectTransform>();
            rt.Fill();   // 격자 칸 자리(HeroList 가 먼저 놓은 빈 칸)를 채운다
            var glow = Ui.Img(rt, Theme.S("fade_top"), (locked ? Theme.Dim : nc).A(0.45f), "glow"); glow.rectTransform.Fill(2, 2, 2, 2);
            var mask = Ui.Img(rt, Theme.Round, Color.white, "mask"); mask.rectTransform.Fill(2, 2, 2, 2); mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            // 스탠딩 상반신(없으면 초상)
            HeroArt(mask.rectTransform, h, 150 * k - 4, 214 * k - 4, 0.5f, 0, (idx * 0.29f) % 2f, locked ? new Color(0.5f, 0.5f, 0.56f) : (Color?)null, false, true);
            var shade = Ui.Img(rt, Theme.S("fade_down"), Color.black.A(0.8f), "shade"); shade.rectTransform.Band(0, 86 * k, 2, 2, 2);
            var col = Ui.Rect("icons", rt).At(0, 1, 6, -6, 24 * k, 120 * k);
            Ui.Col(col, 3, TextAnchor.UpperLeft, null, false, false);
            foreach (var key in new[] { "역할_" + h.role, "성격_" + h.nature })   // 종족 아이콘은 싣지 않는다(사용자 요청)
            {
                var sp = Icon(key); if (sp == null) continue;
                var ic = Ui.Img(col, sp, locked ? Color.white.A(0.6f) : Color.white, key); ic.Pref(24 * k, 24 * k); ic.preserveAspect = true;
            }
            var stars = Ui.Title(rt, $"<size=70%>★</size>{h.star}", Theme.FsXl, Theme.Gold, TextAlignmentOptions.BottomLeft);
            stars.rectTransform.At(0, 0, 8, 28 * k, 80, 34 * k); stars.Outline(0.25f);
            var nm = Ui.Title(rt, h.ko, Theme.FsBody, Color.white, TextAlignmentOptions.BottomRight);
            nm.rectTransform.Band(0, 26 * k, 6, 8, 6 * k);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 11; nm.fontSizeMax = Theme.FsBody; nm.Outline(0.25f);
            var rim = Ui.Img(rt, Theme.Frame, (locked ? Theme.Dim : nc).A(0.85f), "rim"); rim.rectTransform.Fill();
            if (locked) { var lk = Ui.Img(rt, Theme.S("ic_lock"), Color.white.A(0.8f), "lock"); lk.rectTransform.At(1, 1, -8, -8, 22 * k, 22 * k); lk.preserveAspect = true; }
            if (!dex && !locked) ClearBadge(rt, h.key, -6, at >= 0 ? -6 - 38 * k : -6, 0.92f * k);
            if (at >= 0)
            {
                var veil = Ui.Img(rt, Theme.Round, Theme.Gold.A(0.12f), "veil"); veil.rectTransform.Fill();
                var fr = Ui.Img(rt, Theme.S("frame_thick", 24), Theme.Gold, "party"); fr.rectTransform.Fill(-2, -2, -2, -2);
                var no = Ui.Img(rt, Theme.S("circle"), Theme.Gold, "no"); no.rectTransform.At(1, 1, -6, -6, 32 * k, 32 * k);
                var nt = Ui.Title(no.transform, (at + 1).ToString(), Theme.FsLg, Theme.Brown, TextAlignmentOptions.Center); nt.rectTransform.Fill();
            }
            else if (ls.Focus == h.key) { var fr = Ui.Img(rt, Theme.S("frame_thick", 24), Theme.Sky, "focus"); fr.rectTransform.Fill(-2, -2, -2, -2); }
            if (h.Playable || dex) Stage.Hot["hero:" + h.key] = b;
            if (anim && idx < 36) Tw.Pop(rt, 0.01f * idx, 0.88f, 0.28f);
        }

        // ═════════════════════════════ 사도 상세 ═════════════════════════════
        /// <summary>사도 상세 — keys 는 왼쪽 초상 줄(위아래로 넘김), 없으면 그 사도 하나. back 이 없으면 닫기만.</summary>
        public void HeroDetail(string key, List<string> keys, Action back, string tab = "능력치")
        {
            if (tab == "고유 효과") tab = "카드";   // 고유 효과 탭은 카드 탭 오른쪽 판으로 옮겼다
            keys = keys != null && keys.Count > 0 ? keys : new List<string> { key };
            var layer = Ui.Rect("modal herodetail", Stage.ModalLayer).Fill();
            RosterBg(layer);
            var h = Roster.ByKey(key);
            var d = h?.CoreId != null ? P.Data.Hero(h.CoreId) : null;
            void Close() { if (layer) Destroy(layer.gameObject); back?.Invoke(); }
            void Go(string k2, string t2) { if (layer) Destroy(layer.gameObject); HeroDetail(k2, keys, back, t2); }
            // 창 크기가 바뀌면 같은 사도 · 탭으로 다시 세운다(스탠딩 · 칸 너비가 Stage.Size 고정 단위) — 겹친 창 순서는 그대로
            Stage.WhenResized(layer, () =>
            {
                if (!layer) return;
                int at = layer.GetSiblingIndex();
                Go(key, tab);
                var ml = Stage.ModalLayer;
                if (ml.childCount > 0) ml.GetChild(ml.childCount - 1).SetSiblingIndex(Mathf.Min(at, ml.childCount - 1));
            });
            RosterHead(layer, "상세 정보", Close);
            Stage.Hot["detail.close"] = Stage.Hot["back"];

            // 맨 왼쪽 — 초상 줄(보고 있는 사도가 보이게 스크롤)
            float listW = 84;
            var rail = Ui.Rect("faces", layer); rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(0, 1); rail.pivot = new Vector2(0, 1);
            rail.sizeDelta = new Vector2(listW, -110); rail.anchoredPosition = new Vector2(Theme.Gutter - 4, -96);
            var rc = Ui.Scroll(rail, out var rsr);
            Ui.Col(rc, 10, TextAnchor.UpperCenter, new RectOffset(4, 4, 6, 6), false, false);
            int fi = 0, cur = 0;
            foreach (var k2 in keys)
            {
                var hh = Roster.ByKey(k2); if (hh == null) continue;
                var b = Btn.Make(rc, null, BtnStyle.Ghost, () => Go(k2, tab), 0, "face " + k2);
                b.Bg.sprite = Theme.Round; b.SetColor(Color.Lerp(Theme.NavyWell, NatureCol(hh), 0.35f));
                b.Pref(70, 70);
                // 기본 스탠딩의 머리 · 어깨(없으면 초상) — 줄에 보이는 칸만 차례로 굽는다(LiveStanding). 예전엔 열 때 135명을 한 프레임에
                //   구워(CardArt.Upper) 웹 힙이 2GB 를 넘어 멈췄다(2026-10-06 「처음 사도 상세에 들어가면 오류」 — abort("OOM"))
                var clip = Ui.Img(b.transform, Theme.Round, Color.white, "clip"); clip.rectTransform.Fill(3, 3, 3, 3); clip.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                bool snap = hh.art != null && StandingFit.Has(hh.art);   // 구울 수 없으면(스파인 없음) 초상이 그대로 남는다
                var face = snap ? null : CardArt.Upper(hh.art, 1f, 0.34f, false);   // 표 · 스파인이 없는 사도만 웹판 렌더 자르기(가벼운 그림 자르기)
                var im = Ui.Img(clip.rectTransform, face ?? hh.Icon, Color.white, "ic");
                if (face != null) im.rectTransform.Fill(); else { im.rectTransform.Fill(-1, -3, -1, 1); im.preserveAspect = true; }
                if (snap)
                {
                    var still = Ui.Img(clip.rectTransform, null, Color.clear, "snap"); still.rectTransform.Fill();
                    var ls = clip.gameObject.AddComponent<LiveStanding>();
                    ls.Art = hh.art; ls.W = 64; ls.H = 64; ls.Frac = 0.34f; ls.Still = still; ls.Hide = im.gameObject;
                }
                var r0 = Ui.Img(b.transform, Theme.Frame, NatureCol(hh).A(0.7f), "rim"); r0.rectTransform.Fill();
                if (k2 == key) { cur = fi; var br = Ui.Img(b.transform, Theme.S("frame_thick", 24), Theme.Gold, "on"); br.rectTransform.Fill(-4, -4, -4, -4); }
                Stage.Hot["detail.face" + fi++] = b;
            }
            if (cur > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rc);
                float viewH = ((RectTransform)rsr.viewport).rect.height;
                float y = 6 + cur * 80 - viewH / 2 + 35;
                rc.anchoredPosition = new Vector2(0, Mathf.Clamp(y, 0, Mathf.Max(0, rc.rect.height - viewH)));
            }

            // 세로 메뉴 — 능력치 · 카드(고유 효과 탭은 없앴다 — 고학년 · 고유 효과 · 패시브는 카드 탭 오른쪽 판)
            float menuX = Theme.Gutter + listW + 14, menuW = 150;
            var em = Ui.Img(layer, Theme.S("ic_spark"), Theme.Gold.A(0.7f), "emblem"); em.rectTransform.At(0, 1, menuX + menuW / 2 - 14, -108, 28, 28);
            int ti = 0;
            foreach (var t in new[] { "능력치", "카드" })
            {
                bool on = t == tab;
                var b = Btn.Make(layer, null, on ? BtnStyle.PillDark : BtnStyle.Ghost, () => Go(key, t), 0, "tab " + t);
                if (!on) { b.Bg.sprite = Theme.Pill; b.SetColor(Color.white.A(0f)); }
                b.GetComponent<RectTransform>().At(0, 1, menuX, -150 - ti * 64, menuW, 52);
                var tl = Ui.Title(b.transform, t, Theme.FsLg, on ? Theme.Gold : Theme.Sub, TextAlignmentOptions.Center); tl.rectTransform.Fill();
                if (on) { var bar = Ui.Img(b.transform, Theme.Round, Theme.Gold, "bar"); bar.rectTransform.At(0, 0.5f, -8, 0, 4, 30); }
                Stage.Hot["detail.tab:" + t] = b;
                ti++;
            }

            var stage = Ui.Rect("stage", layer); stage.anchorMin = Vector2.zero; stage.anchorMax = Vector2.one;
            stage.offsetMin = new Vector2(menuX + menuW + 20, Theme.Gutter); stage.offsetMax = new Vector2(-Theme.Gutter, -92);
            if (h == null) return;
            if (tab == "카드") DetailCards(stage, h, d);
            else DetailStats(stage, h, d);
        }

        void DetailStats(RectTransform stage, HeroInfo h, Core.HeroDef d)
        {
            var nc = NatureCol(h);
            float panelW = Theme.C(520, 480);
            float stageH = Stage.Size.y - 92 - Theme.Gutter;
            // 가운데 — 기울어진 색 판 + 큰 스탠딩(없으면 SD)
            var fig = Ui.Rect("figure", stage); fig.anchorMin = new Vector2(0, 0); fig.anchorMax = new Vector2(1, 1); fig.offsetMin = Vector2.zero; fig.offsetMax = new Vector2(-(panelW + 20), 0);
            var plate2 = Ui.Img(fig, Theme.Round, Theme.NavyCell.A(0.9f), "plate2"); plate2.rectTransform.At(0.5f, 0.5f, 70, -10, 300, Mathf.Min(560, stageH - 80));
            plate2.rectTransform.localRotation = Quaternion.Euler(0, 0, -9);
            var plate = Ui.Img(fig, Theme.Round, nc.A(0.55f), "plate"); plate.rectTransform.At(0.5f, 0.5f, 0, 0, 380, Mathf.Min(620, stageH - 40));
            plate.rectTransform.localRotation = Quaternion.Euler(0, 0, -9);
            var pr = Ui.Img(plate.rectTransform, Theme.Frame, Color.Lerp(nc, Color.white, 0.3f).A(0.8f), "rim"); pr.rectTransform.Fill();
            var glow = Ui.Img(fig, Theme.S("soft"), Color.Lerp(nc, Color.white, 0.5f).A(0.35f), "glow"); glow.rectTransform.At(0.5f, 0.5f, 0, 40, 620, 620);
            // 원작 스탠딩 스파인을 실제로(Idle_1 · Normal) — 다리 기준 크기 · 골반이 가운데 · 발은 바닥선(StandingFit). 없으면 정지 그림
            float figW0 = Mathf.Max(420, Stage.Size.x - (Theme.Gutter + 84 + 14 + 150 + 20) - Theme.Gutter - panelW - 20);   // 가운데 칸 너비(상세 메뉴 · 오른쪽 판을 뺀 것)
            var live = SpineUi.Standing(fig, h.art, figW0, stageH, StandMode.Full, 0, 4, "detail");   // 기준 키 = 표 95%(가장 큰 사도 쪽) — 작은 사도는 작게 · 떠 있는 사도는 얼굴을 기준 얼굴 높이에(standing_center_fix.json float · _faceRef)
            if (live != null)
            {
                var sh = Ui.Img(fig, Theme.S("fade_down"), Theme.Night.A(0.75f), "foot"); sh.rectTransform.Band(0, 90, -40, -40, -6);
                Tw.Rise(live.rectTransform, 0.04f, 24, 0.45f);
            }
            var full = live == null ? CardArt.Standing(h.art) : null;
            if (live != null) { }
            else if (full != null)
            {
                float figH = stageH + 10, figW = figH * full.rect.width / full.rect.height;
                var im = Ui.Img(fig, full, Color.white, "standing"); im.rectTransform.At(0.5f, 0, 0, -6, figW, figH); im.preserveAspect = true;
                var sh = Ui.Img(fig, Theme.S("fade_down"), Theme.Night.A(0.75f), "foot"); sh.rectTransform.Band(0, 90, -40, -40, -6);
                Tw.Rise(im.rectTransform, 0.04f, 24, 0.45f);
            }
            else
            {
                var spot = Ui.Rect("hero", fig).At(0.5f, 0.5f, 0, -Theme.C(170, 140), 10, 10);
                var g = SceneHero.Make(spot, h, Theme.C(330, 280) * 0.8f, true, 0, true);   // 스탠딩이 없는 사도 — SD 전투 스파인(고정 배율), 미니미는 없앴다
                if (g == null && spot.childCount <= 1) { var im = Ui.Img(spot, h.Icon, Color.white, "icon"); im.rectTransform.At(0.5f, 0, 0, 0, 360, 360); im.preserveAspect = true; }
                Tw.Pop(spot, 0.05f, 0.8f, 0.4f);
            }

            // 오른쪽 판 — 아이콘 줄 · 이름 · 별 · 수치 표
            var panel = NavyBox(stage, "stats"); panel.anchorMin = new Vector2(1, 0); panel.anchorMax = new Vector2(1, 1); panel.pivot = new Vector2(1, 0.5f);
            panel.sizeDelta = new Vector2(panelW, 0); panel.anchoredPosition = Vector2.zero;
            var body = Ui.Rect("body", panel).Fill(26, 20, 26, 20);
            Ui.Col(body, 8, TextAnchor.UpperLeft, null, true, false);
            var icons = Ui.Rect("icons", body); icons.Pref(-1, 34);
            Ui.Row(icons, 8, TextAnchor.MiddleLeft, null, false, false);
            foreach (var k in new[] { "역할_" + h.role, "성격_" + h.nature, "종족_" + h.race })
            {
                var sp = Icon(k); if (sp == null) continue;
                var ic = Ui.Img(icons, sp, Color.white, k); ic.Pref(32, 32); ic.preserveAspect = true;
            }
            var nm = Ui.Title(body, h.ko, Theme.Fs3xl - 10, Theme.Ink); nm.Pref(-1, 56);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 22; nm.fontSizeMax = Theme.Fs3xl - 10; nm.Outline(0.15f);
            var starRow = Ui.Rect("stars", body); starRow.Pref(-1, 26);
            Ui.Row(starRow, 2, TextAnchor.MiddleLeft, null, false, false);
            for (int i = 0; i < 5; i++) { var s = Ui.Img(starRow, Icon(i < h.star ? "별_켜짐" : "별_꺼짐"), Color.white, "star"); s.Pref(24, 24); s.preserveAspect = true; }
            var sub = Ui.Text(body, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(h.nature))}>{h.nature}</color> · {h.race} · {h.role}", Theme.FsBody, Theme.Sub); sub.Pref(-1, 28);
            W.Section(body, "능력치", d == null ? "코어 데이터 준비 중 — 원작 표의 값" : null, 36);
            (string, string)[] rows =
            {
                ("공격력", (d?.Atk ?? h.atk).ToString()),
                ("방어력", (d?.Def ?? h.def).ToString()),
                ("HP", (d?.Hp ?? h.hp).ToString("N0")),
                ("치명 확률", (d?.Crit ?? h.crit) + "%"),
            };
            for (int i = 0; i < rows.Length; i++)
            {
                var r = Ui.Img(body, Theme.Round, i % 2 == 0 ? Theme.NavyWell.A(0.7f) : Color.white.A(0f), "row"); r.Pref(-1, 40);
                var l = Ui.Text(r.transform, rows[i].Item1, Theme.FsBody, Theme.Sub, TextAlignmentOptions.MidlineLeft); l.rectTransform.Fill(16, 0, 0, 0);
                var v = Ui.Title(r.transform, rows[i].Item2, Theme.FsLg, Theme.Gold, TextAlignmentOptions.MidlineRight); v.rectTransform.Fill(0, 0, 16, 0);
            }
            var note = Ui.Img(body, Theme.Glass, Color.white, "grow"); note.Pref(-1, 56);
            var nt = Ui.Text(note.transform, "모험 안에서 강해지는 길 — 캠프 수련 · 아티팩트 · 은총(고유 카드)", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center); nt.rectTransform.Fill(12, 0, 12, 0);
            // 이야기(옛 고유 효과 탭에서 옮김) — 남는 높이에 다 담기게 글자를 줄인다
            if (!string.IsNullOrEmpty(h.blurb))
            {
                W.Section(body, "이야기", null, 34);
                var bl = Ui.Text(body, h.blurb, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft); bl.Pref(-1, 40, -1, 1);
                bl.enableAutoSizing = true; bl.fontSizeMin = 10; bl.fontSizeMax = Theme.FsSm; bl.overflowMode = TextOverflowModes.Ellipsis;
            }
            Tw.Rise(panel, 0.08f, 30, 0.4f, Vector2.right);
        }

        void DetailCards(RectTransform stage, HeroInfo h, Core.HeroDef d)
        {
            if (d == null)
            {
                var t = Ui.Text(stage, "코어 데이터에 아직 없는 사도라 카드가 없습니다 — 도감으로만 봅니다.", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center); t.rectTransform.Fill();
                return;
            }
            float sideW = Theme.C(250, 220);
            // 카드 크기 — PC 는 두 줄(시작 · 고유)이 한 화면에 들게 높이로, 폰은 한 줄 셋으로 줄여 크게(스크롤). 넓은 화면(21:9)은 폭이 남으면 카드를 키우고,
            // 그래도 남는 자리는 오른쪽 판(고학년 · 고유 효과)을 카드 바로 옆에 붙여 빈 곳이 생기지 않게 한다
            int cols = Theme.Compact ? 3 : 4;
            float stageW = Stage.Size.x - (Theme.Gutter + 84 + 14 + 150 + 20) - Theme.Gutter;
            float availH = Stage.Size.y - 92 - Theme.Gutter - 2 * 44 - 30;
            float availW = stageW - sideW - 16 - 18;
            float byW = (availW - 12 * (cols - 1) - 20) / cols;
            float cw = Theme.Compact ? byW : Mathf.Min(Mathf.Clamp(availH / 2f / 1.4f, 140, 240), byW), ch = cw * 1.4f;
            float gridW = cols * cw + (cols - 1) * 12 + 18 + 18;
            var left = Ui.Rect("cards", stage); left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(0, 1); left.pivot = new Vector2(0, 0.5f);
            left.sizeDelta = new Vector2(gridW, 0); left.anchoredPosition = Vector2.zero;
            var content = Ui.Scroll(left, out var sr);
            ScrollBar(left, sr);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(4, 14, 0, 10), true, false);
            // 카드 상세의 이전 · 다음 — 시작 카드 → 고유 카드 순 그대로
            var graceIds = P.Data.UniquesOf(d.Id).Where(u => !d.Starter.Contains(u)).ToList();   // 시동 카드(시작 카드 가운데 고유 카드)는 시작 카드 줄에만 — 은총으로 얻는 것만
            var allCards = CardOrder.Sort(d.Starter, P.Data, new[] { d.Id }).Concat(CardOrder.Sort(graceIds, P.Data, new[] { d.Id })).ToList();
            void Row(string title, string sub, List<string> ids)
            {
                var s = W.Section(content, title, sub, 40);
                var ii = Ui.Img(s.transform.parent, Theme.S("ic_info"), Theme.Dim, "i"); ii.rectTransform.At(0, 0.5f, 14 + s.preferredWidth + 8, 0, 20, 20); ii.preserveAspect = true;
                var holder = Ui.Rect("row " + title, content); holder.Pref(-1, Mathf.CeilToInt(ids.Count / (float)cols) * (ch + 12));
                // 사도 안에서도 순서는 CardOrder(기본 → 고유 · 같은 카드 나란히)
                var sorted = CardOrder.Sort(ids, P.Data, new[] { d.Id });
                for (int i = 0; i < sorted.Count; i++)
                {
                    var id = sorted[i];
                    var c = W.Card(holder, this, id, cw, "card", P.Data.View(id)); c.At(0, 1, (i % cols) * (cw + 12), -(i / cols) * (ch + 12), cw, ch);
                    var b = c.gameObject.AddComponent<Btn>(); int at = allCards.IndexOf(id); b.OnClick = () => CardZoomPlain(id, allCards, at);
                    Stage.Hot["detail.card" + title + i] = b;
                    Tw.Pop(c, 0.03f * i, 0.85f, 0.3f);
                }
            }
            Row("시작 카드", $"{d.Starter.Count}장 · 모험을 시작할 때 덱에", d.Starter);
            Row("고유 카드", $"{graceIds.Count}장 · 은총으로 얻습니다 · 신탁이 나올 수 있습니다", graceIds);

            // 오른쪽 — 고학년 → 고유 효과 → 패시브(2026-10 사용자: 「고유 효과 탭을 없애고 카드 목록 옆에, 자세히 없이 다 보이게」)
            var side = Ui.Rect("side", stage); side.anchorMin = new Vector2(0, 0); side.anchorMax = new Vector2(0, 1); side.pivot = new Vector2(0, 0.5f);
            float sideW2 = Mathf.Clamp(stageW - gridW - 16, sideW, Theme.C(460, 300));   // 넓은 화면이면 판도 넓게
            side.sizeDelta = new Vector2(sideW2, 0); side.anchoredPosition = new Vector2(Mathf.Min(gridW + 16, stageW - sideW2), 0);   // 카드 옆에 붙인다
            TraitsPanel(side, h, d);
        }

        /// <summary>
        /// 고학년 · 고유 효과 · 패시브 한 판(사도 상세 카드 탭) — 「자세히」 없이 처음부터 끝까지 다 보인다(넘치면 판 안에서 스크롤).
        ///   고학년: 볼따구 아이콘 · 이름(굵게) · 「게이지 N%」 · 효과 / 고유 효과: 키워드 이름 · 부제(작은 글) · 줄 목록(그 키워드를 쓰는 패시브까지)
        ///   패시브: 이름 · 줄 목록(키워드를 안 쓰는 것만). 줄마다 「계기 → 결과 (턴당 N회)」 — core CardText.Traits · Docs/설명글.md.
        ///   글 속 키워드 · 생성 카드는 밑줄 · 설명 판(TermPop).
        /// </summary>
        void TraitsPanel(RectTransform host, HeroInfo h, Core.HeroDef d)
        {
            var panel = NavyBox(host, "traits"); panel.Fill();
            var area = Ui.Rect("area", panel).Fill(18, 14, 10, 14);
            var content = Ui.Scroll(area, out var sr);
            Ui.Col(content, 6, TextAnchor.UpperLeft, new RectOffset(2, 12, 2, 8), true, false);
            int ti = 0;
            void Head(string t)
            {
                if (ti > 0) { var gap = Ui.Rect("gap", content); gap.Pref(-1, 6); var rule = Ui.Img(content, Theme.White, Theme.Line, "rule"); rule.Pref(-1, 1); }
                var hd = Ui.Title(content, t, Theme.FsMd, Theme.Sub, TextAlignmentOptions.MidlineLeft, "head " + t); hd.Pref(-1, 30);
                ti++;
            }
            TextMeshProUGUI Body(string text, float size, Color c, bool terms = true)
            {
                var t = Ui.Text(content, "", size, c, TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow; t.lineSpacing = 2;
                if (terms) TermPop.MarkAndAttach(t, text, CardTerms.OfText(P.Data, P.Text, text, d.Id), Stage.ToastLayer, (p, id, w) => W.Card(p, this, id, w, "termcard"));
                else t.text = text;
                if (string.IsNullOrEmpty(t.text)) t.text = text;
                return t;
            }
            if (d.Ult != null)
            {
                Head("고학년");
                var row = Ui.Rect("ult", content); row.Pref(-1, 64);
                var disc = Ui.Img(row, Theme.S("circle"), Color.Lerp(Theme.NavyWell, NatureCol(h), 0.35f), "disc"); disc.rectTransform.At(0, 0.5f, 0, 0, 60, 60);
                disc.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var pic = CardArt.Icon("icon_graduateskill_" + h.art) ?? CardArt.Icon("ultimate_icon_common3") ?? h.Icon;   // 원작 고학년 「볼따구」 얼굴(copy_assets.py)
                if (pic != null) { var im = Ui.Img(disc.transform, pic, Color.white, "pic"); im.rectTransform.Fill(-3, -8, -3, 0); im.preserveAspect = true; }
                var nm = Ui.Title(row, d.Ult.Name, Theme.FsLg, Theme.Gold, TextAlignmentOptions.TopLeft, "ult name"); nm.rectTransform.Fill(72, 30, 0, 2);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 13; nm.fontSizeMax = Theme.FsLg;
                var ga = Ui.Text(row, $"게이지 {d.Ult.Cost}%", Theme.FsSm, Theme.Sub, TextAlignmentOptions.BottomLeft, false, "gauge"); ga.rectTransform.Fill(72, 6, 0, 34);
                Body(P.Text.Fx(d.Ult.Fx), Theme.FsSm, Theme.Ink);
            }
            // 고유 효과 · 패시브 — core CardText.Traits(세 칸 따로 — 패시브 칸은 passives 전부, 없으면 「없음」 · 줄마다 「계기 → 결과」)
            var traits = P.Text.Traits(d);
            foreach (var kind in new[] { "고유 효과", "패시브" })
            {
                var part = traits.Where(x => x.Kind == kind).ToList();
                if (part.Count == 0) continue;
                Head(kind);
                foreach (var t in part)
                {
                    var kn = Ui.Title(content, t.Name, Theme.FsMd, kind == "고유 효과" ? Theme.Gold : Theme.Ink, TextAlignmentOptions.TopLeft, "kw " + t.Name);
                    kn.textWrappingMode = TextWrappingModes.Normal; kn.Pref(-1, 26);
                    if (!string.IsNullOrEmpty(t.Sub)) Body(t.Sub, Theme.FsCap + 1, Theme.Sub, false);
                    Body(t.Body, Theme.FsSm, Theme.Ink);
                }
            }
            if (ti == 0) Body($"<color={Theme.SubTag}>고학년 · 고유 효과 · 패시브가 없는 사도입니다.</color>", Theme.FsSm, Theme.Sub, false);
        }    }
}
