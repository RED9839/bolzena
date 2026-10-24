using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 성장 화면(성장 켬일 때만, Data/ProgStore.cs — 2026-10-21 밤 「판 밖 성장 = 잠재력만」): 도전 사다리(난이도 단계 고르기) · 로비 잠재력 판 · 판 끝 잠재력 재료 ·
    //   사도 상세 잠재력 요약 · 「잠재력」 탭 · 카드 상세 실제 수치. 레벨 · 진급 · 던전 화면은 없앴다.
    //   스위치가 꺼져 있으면(콘텐츠에 성장 표 없음) 여기 함수는 하나도 불리지 않는다 — 지금 화면 그대로.
    //   외부 게임 이름은 화면 글에 쓰지 않는다.
    public partial class Flow
    {
        static string N0(int v) => v.ToString("N0");

        /// <summary>막대(바탕 + 채움) — frac 0~1.</summary>
        static RectTransform ProgBar(Transform parent, float frac, Color fill, string name = "bar")
        {
            var bg = Ui.Img(parent, Theme.Round, Theme.NavyWell, name);
            var f = Ui.Img(bg.transform, Theme.Round, fill, "fill");
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1);
            f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
            return bg.rectTransform;
        }

        // ── 도전 사다리(난이도 단계) 고르기 줄 — 마을 고르기 화면의 오른쪽 상세 판 안(2026-10-22 사용자: 따로 고르는 화면을 없애고 마을을 고르면서) ──
        //   열린 단계(최고 완주 + 1)까지 고를 수 있고, 잠긴 단계는 자물쇠 + 「앞 단계 완주」. 고른 단계의 적 배율 · 재료 배율 · 완주 한 판 마시멜로 칩.
        //   고른 단계는 RunTier 에 바로 적고 마지막으로 고른 단계를 기억한다(설정 PlayerPrefs bz.tier.last). 성장 꺼짐이면 부르지 않는다(지금 공개판 그대로).
        static string TierLastKey => "bz.tier.last" + (Array.IndexOf(Environment.GetCommandLineArgs(), "-save") is int si && si >= 0 && si < Environment.GetCommandLineArgs().Length - 1 ? "." + Environment.GetCommandLineArgs()[si + 1] : "");
        /// <summary>모험을 열 때의 기본 단계 — 마지막으로 고른 단계(열린 단계 안으로 자름), 처음이면 열린 가장 높은 단계.</summary>
        static int TierDefault() { int last = PlayerPrefs.GetInt(TierLastKey, 0); return last > 0 ? Mathf.Clamp(last, 1, ProgStore.OpenTier) : ProgStore.DefaultTier; }

        /// <summary>단계 고르기 줄을 host 에 그린다(host 크기 w × h). compact = 폰 — 「− n단계 +」 한 줄. 높이 PC 112 · 폰 116.</summary>
        void TierRow(RectTransform host, float w, bool compact)
        {
            int open = ProgStore.OpenTier;
            if (RunTier < 1 || RunTier > open) RunTier = TierDefault();
            var head = Ui.Title(host, "", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft, "tierhead"); head.textWrappingMode = TextWrappingModes.NoWrap;
            head.enableAutoSizing = true; head.fontSizeMin = 11; head.fontSizeMax = Theme.FsSm;
            var cells = new List<(int t, Image rim)>();
            RectTransform chipHost = null;
            TextMeshProUGUI cap = null;
            void Paint()
            {
                int t = RunTier;
                head.text = $"<color={Theme.GoldTag}>난이도 {t}단계</color>  적 세기 {ProgStore.FoeX(t)}" + (compact ? "" : $"  <color={Theme.SubTag}>최고 완주 {ProgStore.BestTier}</color>");
                foreach (var c in cells) if (c.rim) c.rim.enabled = c.t == t;
            }
            if (!compact)
            {
                head.rectTransform.At(0, 1, 0, 0, w, 24);
                float cw = (w - 8 * 4) / 9f, ch = 36;
                for (int t = 1; t <= Tiers.MAX; t++)
                {
                    int tt = t; bool locked = t > open;
                    var b = Btn.Make(host, null, BtnStyle.Dark, () => { RunTier = tt; PlayerPrefs.SetInt(TierLastKey, tt); PlayerPrefs.Save(); Paint(); PaintChips(); }, 0, "tier " + t);
                    b.GetComponent<RectTransform>().At(0, 1, (t - 1) * (cw + 4), -28, cw, ch);
                    var nt = Ui.Title(b.transform, t.ToString(), Theme.FsMd, locked ? Theme.Dim : Theme.Ink, TextAlignmentOptions.Center); nt.rectTransform.Fill();
                    if (locked) { LockBadge(b.GetComponent<RectTransform>(), 16, 1, 0, -2, 2); b.Interactable = false; b.Why = $"{t - 1}단계를 완주하면 열린다(앞 단계 완주)"; }
                    var rim = Ui.Img(b.transform, Theme.S("frame_thick", 24), Theme.Gold, "on"); rim.rectTransform.Fill(-3, -3, -3, -3); rim.raycastTarget = false;
                    cells.Add((t, rim));
                    Stage.Hot["tier." + t] = b;
                }
                chipHost = Ui.Rect("tierchips", host).At(0, 1, 0, -72, 3 * (40 + 6), 40);
                cap = Ui.Text(host, "", Theme.FsCap, Theme.Sub, TextAlignmentOptions.MidlineLeft, false, "tiercap"); cap.rectTransform.At(0, 1, 3 * (40 + 6) + 6, -72, w - 3 * (40 + 6) - 6, 40);
            }
            else
            {
                head.rectTransform.At(0, 1, 0, 0, w, 22);
                float bw = 44;
                var minus = Btn.Make(host, "-", BtnStyle.PillDark, () => { if (RunTier > 1) { RunTier--; PlayerPrefs.SetInt(TierLastKey, RunTier); Paint(); PaintChips(); } }, Theme.FsLg, "tier.minus");
                minus.GetComponent<RectTransform>().At(0, 1, 0, -26, bw, 36);
                var plus = Btn.Make(host, "+", BtnStyle.PillDark, () => { if (RunTier < open) { RunTier++; PlayerPrefs.SetInt(TierLastKey, RunTier); Paint(); PaintChips(); } }, Theme.FsLg, "tier.plus");
                plus.GetComponent<RectTransform>().At(0, 1, w - bw, -26, bw, 36);
                Stage.Hot["tier.minus"] = minus; Stage.Hot["tier.plus"] = plus;
                chipHost = Ui.Rect("tierchips", host).At(0, 1, bw + 8, -26, w - 2 * bw - 16, 36);
                cap = Ui.Text(host, "", Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft, false, "tiercap"); cap.rectTransform.At(0, 1, 0, -66, w, 44);
                cap.textWrappingMode = TextWrappingModes.Normal;
            }
            cap.enableAutoSizing = true; cap.fontSizeMin = 9; cap.fontSizeMax = Theme.FsCap;
            void PaintChips()
            {
                foreach (Transform c in chipHost) Destroy(c.gameObject);
                int t = RunTier;
                MatChips(chipHost, ProgStore.MM.Select(m => (m.key, ProgStore.WinMat(t, m.key[2] - '0'))), compact ? 36 : 40).Fill();
                cap.text = "완주 한 판 마시멜로 · 쓰러져도 약 1/4"
                    + (open < Tiers.MAX ? $"\n<color={Theme.DimTag}>{open + 1}단계 — {open}단계 완주하면 열린다</color>" : "")
                    + $"\n<color={Theme.DimTag}>1~2단계 = 기본 세기 · {Tiers.MAX}단계 = 잠재력을 다 연 기준</color>";
            }
            Paint(); PaintChips();
        }

        /// <summary>판 머리 · 끝 화면에 붙일 단계 글(단계 없으면 빈 글).</summary>
        string TierTag(int tier) => tier > 0 ? $"  <size=72%><color={Theme.GoldTag}>난이도 {tier}단계</color></size>" : "";

        // ── 로비 — 잠재력 · 재료 · 최고 완주 단계 ──
        void LobbyProg(RectTransform root)
        {
            if (!ProgStore.On) return;
            float w = Theme.C(560, 520), h = Theme.C(84, 78);
            var box = NavyBox(root, "prog"); box.At(0, 1, 34, -146, w, h);
            var ic = Ui.Img(box, Theme.S("ic_spark"), Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 1, 16, -14, 30, 30);
            var t1 = Ui.Title(box, "", Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft); t1.rectTransform.At(0, 1, 54, -10, 190, 36); t1.textWrappingMode = TextWrappingModes.NoWrap;
            var barHost = Ui.Rect("barhost", box).At(0, 1, 230, -20, w - 230 - 140, 14);
            var tr = Ui.Text(box, "", Theme.FsCap, Theme.Sub, TextAlignmentOptions.MidlineRight); tr.rectTransform.At(1, 1, -16, -10, 130, 36); tr.textWrappingMode = TextWrappingModes.NoWrap;
            var t2 = Ui.Text(box, "", Theme.FsSm, Theme.Sub, TextAlignmentOptions.MidlineLeft); t2.rectTransform.Band(0, 34, 18, 14, 6); t2.textWrappingMode = TextWrappingModes.NoWrap; t2.overflowMode = TextOverflowModes.Ellipsis;
            t2.enableAutoSizing = true; t2.fontSizeMin = 11; t2.fontSizeMax = Theme.FsSm;
            var chipHost = Ui.Rect("chips", box).At(0, 0, 16, 6, w - 32, 34);
            void Paint()
            {
                if (!box) { ProgStore.Changed -= Paint; Online.Changed -= Paint; return; }
                foreach (Transform c in barHost) Destroy(c.gameObject);
                foreach (Transform c in chipHost) Destroy(c.gameObject);
                tr.text = $"최고 완주 <color={Theme.GoldTag}>{ProgStore.BestTier}</color>단계";
                if (ProgStore.Have)
                {
                    // 사도 20명의 잠재력 단계 합 / 최대 — 막대
                    int lv = 0, max = 0;
                    foreach (var k in P.Data.Heroes.Keys) { var p = ProgStore.PotProgress(k); lv += p.lv; max += p.max; }
                    t1.text = $"<color={Theme.SubTag}>잠재력</color> {N0(lv)}<size=70%><color={Theme.DimTag}>/{N0(max)}</color></size>";
                    ProgBar(barHost, max > 0 ? lv / (float)max : 0, Theme.Gold).Fill();
                    t2.text = "";
                    WalletChips(chipHost, 30).Fill();   // 마시멜로 하급 · 중급 · 상급 — 그림 + 가진 수
                }
                else
                {
                    t1.text = $"<color={Theme.SubTag}>잠재력</color>";
                    ProgBar(barHost, 0, Theme.Gold).Fill();
                    t2.text = Online.Ready ? "계정 성장을 불러오는 중…" : "로그인하면 모험이 끝날 때마다 마시멜로를 받는다";
                }
            }
            ProgStore.Changed += Paint;
            Online.Changed += Paint;
            Paint();
            Tw.Rise(box, 0.2f, 20, 0.4f, Vector2.left);
        }

        // ── 판 끝 — 잠재력 재료 ──
        /// <summary>잠재력 재료 판 — 끝 화면 아래 띠 바로 위 왼쪽. 판을 세웠으면 true(끝 화면이 사도 카드를 그만큼 줄인다).</summary>
        bool EndProg(RectTransform root, bool win)
        {
            if (!ProgStore.On || P.S == null || P.S.Tier <= 0) return false;
            float w = Mathf.Min(760, Stage.Size.x - Theme.Gutter * 2), h = Theme.C(92, 84);
            var box = NavyBox(root, "progreward");
            box.At(0, 0, Theme.Gutter, Theme.C(26, 14) + Theme.BtnMainH + 12, w, h);
            var t = Ui.Text(box, "", Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft); t.rectTransform.Fill(18, 6, 14, 8);
            t.enableAutoSizing = true; t.fontSizeMin = 11; t.fontSizeMax = Theme.FsSm; t.textWrappingMode = TextWrappingModes.Normal;
            float cs = h - 40;
            var chipHost = Ui.Rect("chips", box).At(0, 0, 18, 8, 3 * (cs + 6), cs);
            var side = Ui.Text(box, "", Theme.FsSm, Theme.Ink, TextAlignmentOptions.BottomLeft, false, "side"); side.rectTransform.At(0, 0, 18 + 3 * (cs + 6) + 10, 8, w - 3 * (cs + 6) - 40, cs);
            side.textWrappingMode = TextWrappingModes.Normal; side.enableAutoSizing = true; side.fontSizeMin = 10; side.fontSizeMax = Theme.FsSm;
            int tier = P.S.Tier;
            bool sounded = false;
            void Paint()
            {
                if (!box) { ProgStore.EarnDone -= Paint; return; }
                var e = ProgStore.LastEarn;
                foreach (Transform c in chipHost) Destroy(c.gameObject);
                side.text = "";
                string head = $"<color={Theme.GoldTag}>잠재력 재료 — 마시멜로</color> · {tier}단계";
                if (ProgStore.EarnPending) { t.text = head + "\n받는 중…"; return; }
                if (e == null)
                {
                    t.text = head + "\n" + (ProgStore.EarnError ?? (Online.Ready ? "이 모험은 재료 보상이 없다" : "로그인하면 이 모험으로 잠재력 재료를 받는다"))
                        + (win && !ProgStore.Have ? $" · 최고 완주 {ProgStore.BestTier}단계(이 기기)" : "");
                    return;
                }
                if (e["dup"]?.Type == JTokenType.Boolean && (bool)e["dup"]) { t.text = head + "\n이미 받은 모험이다"; return; }
                var rw = e["reward"]; var items = rw?["give"]?["items"] as JObject;
                double prog = (double?)rw?["progress"] ?? 1;
                var capped = rw?["capped"] as JArray;
                t.text = head + (prog < 1 ? $" · 진행 {Mathf.RoundToInt((float)prog * 100)}%" : "") + (capped != null && capped.Count > 0 ? $" · <color={Theme.DimTag}>주간 상한</color>" : "");
                var got = ProgStore.MM.Select(m => (m.key, ProgStore.IntOf(items, m.key))).Where(x => x.Item2 > 0).ToList();
                if (got.Count > 0) MatChips(chipHost, got, cs).Fill();
                else side.text = $"<color={Theme.DimTag}>받은 마시멜로가 없다</color>";
                side.text += (prog < 1 ? $"<color={Theme.SubTag}>완주하지 못해 {Mathf.RoundToInt((float)prog * 100)}%만</color>" : "")
                    + (e["opened"]?.Type == JTokenType.Integer ? (prog < 1 ? "\n" : "") + $"<color={Theme.GoldTag}>{(int)e["opened"]}단계가 열렸다</color>" : "");
                if (got.Count > 0 && !sounded) { sounded = true; Sfx.Play("marshmallow", 0.8f); }
            }
            ProgStore.EarnDone += Paint;
            Paint();
            Tw.Pop(box, 1.0f, 0.6f, 0.4f);
            return true;
        }

        // ── 사도 상세 오른쪽 판 — 이름 아래 잠재력 요약 한 줄 + 얇은 막대(레벨 줄이 있던 자리) ──
        void DetailPotLine(RectTransform body, Core.HeroDef d)
        {
            var (open, lv, max) = ProgStore.PotProgress(d.Id);
            var row = Ui.Rect("potline", body); row.Pref(-1, Theme.C(34, 30));
            var lt = Ui.Title(row, $"<color={Theme.SubTag}>잠재력</color> {lv}<color={Theme.DimTag}>/{max}</color>", Theme.FsXl, Theme.Ink, TextAlignmentOptions.MidlineLeft, "pottext"); lt.rectTransform.Fill();
            lt.textWrappingMode = TextWrappingModes.NoWrap;
            string right = !ProgStore.Have ? (Online.Ready ? "불러오는 중…" : "로그인하면 잠재력을 연다") : lv >= max && max > 0 ? "모두 열었다" : $"열린 칸 {open}/{ProgStore.Table?.Potential.Count ?? 0}";
            var et = Ui.Text(row, right, Theme.FsCap, Theme.Sub, TextAlignmentOptions.MidlineRight, false, "potsub");
            et.rectTransform.Fill(); et.textWrappingMode = TextWrappingModes.NoWrap;
            var br = Ui.Rect("potbar", body); br.Pref(-1, 6);
            ProgBar(br, max > 0 ? lv / (float)max : 0, lv >= max && max > 0 ? Theme.Gold : Theme.Sky).Fill();
        }

        // ── 사도 상세 오른쪽 판 능력치 표 아래 — 잠재력이 지금 주는 덤 두세 줄(빈 자리를 채운다 — 연 칸이 없으면 안내 한 줄) ──
        void DetailPotMods(RectTransform body, Core.HeroDef d)
        {
            var m = ProgStore.ModsOf(d.Id);
            var lines = new List<string>();
            if (m != null)
            {
                if (m.Basic != 0 || m.Neutral != 0 || m.Unique != 0) lines.Add($"카드 피해 · 실드 · 치유 — 기본 <color={Theme.GoldTag}>+{Pct(m.Basic)}</color> · 교주 <color={Theme.GoldTag}>+{Pct(m.Neutral)}</color> · 고유 <color={Theme.GoldTag}>+{Pct(m.Unique)}</color>");
                if (m.Stat != null) foreach (var kv in m.Stat.Where(x => x.Value != 0)) lines.Add(StatKo(kv.Key, kv.Value));
                if (m.Cards != null && m.Cards.Count > 0) lines.Add($"사도 칸 카드 덤 <color={Theme.GoldTag}>{m.Cards.Count}</color>");
                if (m.Shin > 0) lines.Add($"신탁 축복 확률 ×{1 + m.Shin:0.##}");
            }
            string txt = lines.Count > 0 ? string.Join("\n", lines)
                : !ProgStore.Have ? (Online.Ready ? "계정 성장을 불러오는 중…" : "로그인하면 모험 끝에 받는 재료로 잠재력 칸을 연다")
                : "아직 연 잠재력 칸이 없다 — 아래 「잠재력」 에서 연다";
            var t = Ui.Text(body, $"<color={Theme.SubTag}>잠재력 덤</color>\n" + txt, Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft, false, "potmods");
            t.Pref(-1, Theme.C(26, 22) * (1 + Math.Max(1, Math.Min(4, lines.Count))) + 6);
            t.textWrappingMode = TextWrappingModes.Normal; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = Theme.FsSm;
        }

        // ── 사도 상세 오른쪽 판 맨 아래 — 넓은 금 주 단추 하나 「잠재력」(그 탭으로) ──
        void DetailPotGo(RectTransform body, Action openPot)
        {
            var main = Btn.Make(body, null, BtnStyle.PillGold, openPot, 0, "main");
            main.Pref(-1, Theme.BtnMainH);
            var mic = Ui.Img(main.transform, Theme.S("ic_spark"), Theme.Brown, "ic"); mic.preserveAspect = true; mic.rectTransform.At(0, 0.5f, 22, 0, 26, 26);
            var mt = Ui.Title(main.transform, "잠재력", Theme.FsLg, Theme.Brown, TextAlignmentOptions.Center); mt.rectTransform.Fill(40, 0, 16, 0);
            main.Label = mt;
            main.Interactable = openPot != null;
            Stage.Hot["detail.main"] = main; Stage.Hot["detail.pot"] = main;
        }

        /// <summary>재료 칸(카제나 재료 칸) — 정사각 아이콘 + 아래 「가진 수/필요 수」(모자라면 가진 수 빨강). 글은 Paint 로 다시 쓴다.</summary>
        static TextMeshProUGUI MatCell(Transform parent, string key, float sz)
        {
            var cell = Ui.Img(parent, Theme.Round, Theme.NavyWell, "mat " + key); cell.Pref(sz, sz + 26);
            var rt = cell.rectTransform;
            var well = Ui.Img(rt, Theme.Round, Theme.NavyCell, "well"); well.rectTransform.Fill(3, 26, 3, 3);
            well.color = Color.Lerp(Theme.NavyCell, ProgStore.GradeColor(key), 0.5f);   // 등급색 바탕(하급 하늘 · 중급 금 · 상급 분홍)
            var art = Theme.Art("Currency/" + key);
            if (art != null) { var ic = Ui.Img(well.rectTransform, art, Color.white, "ic"); ic.preserveAspect = true; ic.rectTransform.Fill(4, 4, 4, 4); }
            else { var ic = Ui.Img(well.rectTransform, Theme.S("ic_bag"), Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.Fill(12, 12, 12, 12); }
            var rim = Ui.Img(rt, Theme.Frame, Theme.Edge.A(0.45f), "rim"); rim.rectTransform.Fill();
            var t = Ui.Text(rt, "", Theme.FsCap, Theme.Ink, TextAlignmentOptions.Center); t.rectTransform.Band(0, 26, 2, 2, 0);
            t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 9; t.fontSizeMax = Theme.FsCap;
            return t;
        }
        static string HaveNeed(int have, int need) => $"<color={(have < need ? Theme.BadTag : Theme.GoodTag)}>{N0(have)}</color><color={Theme.SubTag}>/{N0(need)}</color>";

        // ── 사도 상세 「잠재력」 탭 — 카제나 잠재력 화면(성장_설계_카제나판.md §11) ──
        //   왼쪽 아래 01 에서 오른쪽 위 07 로 오르는 지그재그 판(줄기 01~07 + 갈래 03-1 · 04-1 · 05-1 · 05-2). 뒤에 사도 스탠딩을 흐리게.
        //   04-1 「치명 감각」(10-22 — 사도 공통 치명 확률)은 04 에서 왼쪽 위 빈자리로 갈라진다(05-1 과 같은 높이, 왼쪽 끝).
        //   1단계짜리 특수 칸 = 큰 원(후광 고리), 여러 단계 칸 = 작은 원 + 아래 알약 「a/b」. 안 연 칸 = 흐린 반투명 원, 연 칸 = 짙은 원 + 금 테,
        //   지금 열 수 있는 칸 = 금 고리, 고른 칸 = 금 후광 + 위 작은 금 ▼. 칸 사이 선(연 길은 금).
        //   여는 조건 · 값은 서버가 판단(lib/prog.js potCheck) — 화면은 서버 줄(potential)을 그대로 보인다. 효과 글은 콘텐츠 성장 표(potential · heroes)로 짓는다.
        static string potPick = "01";
        /// <summary>칸 자리 — 판 안 (가로 0~1 왼쪽에서, 세로 0~1 아래에서). 표에 없는 칸은 오른쪽 아래부터 차례로.</summary>
        static readonly Dictionary<string, Vector2> POT_XY = new()
        {
            ["01"] = new Vector2(0.06f, 0.12f), ["02"] = new Vector2(0.30f, 0.04f), ["03"] = new Vector2(0.24f, 0.37f), ["03-1"] = new Vector2(0.50f, 0.28f),
            ["04"] = new Vector2(0.14f, 0.64f), ["04-1"] = new Vector2(0.03f, 0.93f), ["05"] = new Vector2(0.42f, 0.62f), ["05-1"] = new Vector2(0.30f, 0.93f), ["05-2"] = new Vector2(0.78f, 0.40f),
            ["06"] = new Vector2(0.66f, 0.68f), ["07"] = new Vector2(0.80f, 0.94f),
        };
        /// <summary>큰 원 = 1단계짜리 특수 칸(카제나 01 · 03-1 · 05-1 · 07), 작은 원 = 여러 단계 칸.</summary>
        static bool PotBig(ProgNodeT n) => n.Max <= 1;
        /// <summary>칸 그림 — 종류마다(카드 강화 = 카드 + ▲, 능력치 = 검, 축복 = 축복, 사도 칸 = 별빛, 시작 = 깃발).</summary>
        static string PotIcon(ProgNodeT n) => n.Kind switch { "start" => "ic_flag", "card" => "ic_deck", "skill" => "ic_deck", "stat" => n.Of == "hero" ? "ic_spark" : "ic_sword", "shin" => "ic_bless", "hero" => "ic_crown", _ => "ic_spark" };
        /// <summary>오른쪽 판 머리 — 칸 종류 이름.</summary>
        static string PotKindHead(ProgNodeT n) => n.Kind switch { "start" => "시작 보너스", "card" => "카드 강화", "skill" => "카드 강화", "stat" => n.Of == "hero" ? "고유 능력치" : "능력치 숙련", "shin" => "신탁 축복", "hero" => "고유 능력", _ => "잠재력" };

        static string PotKindKo(string of) => of == "basic" ? "기본" : of == "neutral" ? "교주" : of == "unique" ? "고유" : of;
        static string Pct(double v) => (v * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
        static string StatKo(string k, double v) => k switch
        {
            "crit" => $"치명 확률 +{v:0.#}%p", "critDmg" => $"치명 피해 +{Pct(v)}", "atk%" => $"공격력 +{Pct(v)}", "hp%" => $"HP +{Pct(v)}", "def%" => $"방어력 +{Pct(v)}",
            "weak" => $"약점 공격 피해 +{Pct(v)}", "dealt" => $"주는 피해 +{Pct(v)}", "guard" => $"실드 +{Pct(v)}", "heal" => $"치유 +{Pct(v)}", _ => $"{k} +{v}",
        };

        /// <summary>칸 이름 — 사도 칸(05 · 06 · 03-1 · 05-1 · 07)은 그 사도의 이름(「치명 숙련」 …).</summary>
        static string PotName(ProgTable t, ProgNodeT n, string hero) => (n.Kind == "hero" || n.Of == "hero") && t.HeroNode(hero, n.Id) is ProgHeroNodeT hn && !string.IsNullOrEmpty(hn.Name) ? hn.Name : n.Name;

        /// <summary>잠재력 칸 하나의 효과 글 — 단계 lv(0 이면 null). hd · now 는 문턱형이 지금 넘었는지 보이는 데 쓴다.</summary>
        static string PotEffect(ProgTable t, ProgNodeT n, int lv, Core.HeroDef hd, ProgSum now)
        {
            if (lv <= 0) return null;
            string role = Prog.RoleStat(hd.Role) == "atk" ? "공격" : "방어";
            var hn = t.HeroNode(hd.Id, n.Id);
            switch (n.Kind)
            {
                case "start": return $"모험 시작 덱에 이 사도 고유 카드 <color={Theme.GoldTag}>{lv}장</color> 더(무작위)";
                case "card": return $"{PotKindKo(n.Of)} 카드 피해 · 실드 · 치유 <color={Theme.GoldTag}>+{Pct(n.V * lv)}</color>";
                case "stat":
                    if (n.Of == "hero") return hn == null ? n.Desc : string.Join(" · ", hn.Stat.Select(kv => StatKo(kv.Key, kv.Value * lv))) + $" <color={Theme.SubTag}>(단계마다 {string.Join(" · ", hn.Stat.Select(kv => StatKo(kv.Key, kv.Value)))})</color>";
                    if (n.Of == "crit") return $"치명 확률 <color={Theme.GoldTag}>+{n.V * lv:0.#}%p</color> <color={Theme.SubTag}>(단계마다 +{n.V:0.#}%p · 기본 {hd.Crit}%)</color>";   // 04-1 치명 감각(사도 공통)
                    return n.Of == "role" ? $"주 능력치({role}) <color={Theme.GoldTag}>+{Pct(n.V * lv)}</color>" : n.Of == "hp" ? $"HP <color={Theme.GoldTag}>+{Pct(n.V * lv)}</color>" : $"{n.Of} +{Pct(n.V * lv)}";
                case "shin": return $"이 사도 카드의 신탁 후보에 축복이 얹힐 확률 ×{1 + n.V * lv:0.##} <color={Theme.SubTag}>({Pct(R.ORACLE_BLESS)} → {Pct(R.ORACLE_BLESS * (1 + n.V * lv))})</color>";
                case "hero":
                    {
                        if (hn == null) return n.Desc;
                        return hn.Desc;   // 능력치 문턱 덤은 10-22 없앴다 — 늘 드는 앞부분만
                    }
                case "skill":
                    {
                        string to = string.Join(" · ", n.To.Select(x => x == "basic" ? "기본 카드" : x == "unique" ? "고유 카드" : x == "neutral" ? "교주 카드" : x == "guard" ? "실드" : x == "heal" ? "치유" : x));
                        return $"{to} <color={Theme.GoldTag}>+{Pct(n.V * lv)}</color>";
                    }
            }
            return n.Desc;
        }

        // ── 마시멜로 그림 칩(2026-10-22 사용자 「보상은 글로만 쓰지 말고 재화 그림으로」 — 카제나처럼 등급색 바탕 네모 아이콘 + 수) ──
        /// <summary>칩 하나 — 등급색 바탕 둥근 네모 + 원작 그림 + 오른쪽 아래 「×n」(n 이 0 보다 작으면 수 없이).</summary>
        static RectTransform MatChip(Transform parent, string key, int n, float sz)
        {
            var bg = Ui.Img(parent, Theme.Round, Color.Lerp(Theme.NavyCell, ProgStore.GradeColor(key), 0.55f), "chip " + key); bg.Pref(sz, sz);
            var rt = bg.rectTransform;
            var art = Theme.Art("Currency/" + key);
            if (art != null) { var ic = Ui.Img(rt, art, Color.white, "ic"); ic.preserveAspect = true; ic.raycastTarget = false; ic.rectTransform.Fill(sz * 0.06f, sz * 0.12f, sz * 0.06f, sz * 0.02f); }
            var rim = Ui.Img(rt, Theme.Frame, ProgStore.GradeColor(key).A(0.9f), "rim"); rim.rectTransform.Fill(); rim.raycastTarget = false;
            if (n >= 0)
            {
                var t = Ui.Title(rt, "×" + N0(n), Mathf.Max(12, sz * 0.36f), Color.white, TextAlignmentOptions.BottomRight); t.rectTransform.Fill(2, 1, 4, 0);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.Outline(0.3f); t.raycastTarget = false;
            }
            return rt;
        }
        /// <summary>칩 줄 — (열쇠, 수) 차례대로 왼쪽부터.</summary>
        static RectTransform MatChips(Transform parent, IEnumerable<(string key, int n)> items, float sz)
        {
            var row = Ui.Rect("matchips", parent);
            Ui.Row(row, 6, TextAnchor.MiddleLeft, null, false, false);
            foreach (var (k, n) in items) MatChip(row, k, n, sz);
            return row;
        }
        /// <summary>지갑 칩 줄 — 마시멜로 셋을 「그림 + 가진 수(옆 글)」로(수가 커도 넘치지 않게 칩 밖 오른쪽에 쓴다).</summary>
        static RectTransform WalletChips(Transform parent, float sz)
        {
            var row = Ui.Rect("wallet", parent);
            Ui.Row(row, 14, TextAnchor.MiddleLeft, null, false, false);
            foreach (var m in ProgStore.MM)
            {
                var cell = Ui.Rect("w " + m.key, row);
                MatChip(cell, m.key, -1, sz).At(0, 0.5f, 0, 0, sz, sz);
                var t = Ui.Title(cell, N0(ProgStore.Wallet(m.key)), Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft); t.rectTransform.At(0, 0.5f, sz + 6, 0, 90, sz);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.Outline(0.15f);
                cell.Pref(sz + 6 + t.preferredWidth + 2, sz);
            }
            return row;
        }

        void DetailPotential(RectTransform stage, HeroInfo h, Core.HeroDef d, Action redraw)
        {
            var t = ProgStore.ViewTable;   // 켬 = 콘텐츠 성장 표, 꺼짐 = 보기 전용 미리 보기 사본(ProgStore)
            bool growOn = ProgStore.On;
            if (t == null || d == null) return;
            if (t.Node(potPick) == null) potPick = t.Potential[0].Id;
            float panelW = DetailPanelW;
            var pot = ProgStore.PotOf(d.Id);
            var now = ProgStore.SumOf(d.Id);
            var mods = ProgStore.ModsOf(d.Id);
            bool have = ProgStore.Have;
            int LvOf(string id) => pot.TryGetValue(id, out var l0) ? l0 : 0;
            ProgNodeT PrevOf(ProgNodeT n) { int ix = t.Potential.IndexOf(n); return n.After != null ? t.Node(n.After) : ix > 0 ? t.Potential[ix - 1] : null; }

            // 왼쪽 — 판(뒤에 흐린 스탠딩)
            var left = Ui.Rect("potboard", stage); left.anchorMin = Vector2.zero; left.anchorMax = Vector2.one; left.offsetMin = new Vector2(20, Theme.Gutter); left.offsetMax = new Vector2(-(panelW + 12), -8);
            float lw = Stage.Size.x - DetailSideW - panelW - 32, lh = Stage.Size.y - DTop - Theme.Gutter - 8;
            var full = CardArt.Standing(h.art);
            if (full != null)
            {
                float asp = full.rect.width / full.rect.height, fh = Mathf.Min(lh * 1.05f, lw * 0.62f / asp), fw = fh * asp;   // 판 오른쪽 6할 안에(카제나: 스탠딩이 오른쪽 가운데에 흐리게)
                var sil = Ui.Img(left, full, Color.white.A(0.11f), "ghost"); sil.preserveAspect = true; sil.raycastTarget = false;
                sil.rectTransform.At(1, 0, 0, -lh * 0.04f, fw, fh);
            }
            var head = Ui.Title(left, $"<color={Theme.SubTag}>{h.ko}</color>  잠재력", Theme.FsXl, Theme.Ink, TextAlignmentOptions.TopLeft); head.rectTransform.Band(1, 44, 6, 6, -4); head.Outline(0.12f);
            int opened = t.Potential.Count(n => LvOf(n.Id) > 0);
            var sub = Ui.Text(left, $"열린 칸 {opened}/{t.Potential.Count}" + (mods != null ? $" · 카드 기본 +{Pct(mods.Basic)} · 교주 +{Pct(mods.Neutral)} · 고유 +{Pct(mods.Unique)}" : ""), Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft);
            sub.rectTransform.Band(1, 26, 8, 6, -46); sub.textWrappingMode = TextWrappingModes.NoWrap; sub.enableAutoSizing = true; sub.fontSizeMin = 11; sub.fontSizeMax = Theme.FsSm;
            // 오른쪽 위 지갑 칩 — 마시멜로 하급 · 중급 · 상급(그림 + 가진 수)
            if (have) WalletChips(left, 34).At(1, 1, 0, -2, lw * 0.6f, 34);
            var board = Ui.Rect("board", left); board.anchorMin = Vector2.zero; board.anchorMax = Vector2.one; board.offsetMin = Vector2.zero; board.offsetMax = new Vector2(0, -80);
            float bw = lw, bh = lh - 80;
            float big = Mathf.Floor(Mathf.Clamp(Mathf.Min(bw * 0.14f, bh * 0.17f), 64, 112)), small = Mathf.Floor(big * 0.66f);
            float padX = big * 0.75f + 10, padB = small * 0.5f + 34, padT = big * 0.5f + 30;
            var pos = new Dictionary<string, Vector2>();
            int extra = 0;
            foreach (var n in t.Potential)
            {
                var f = POT_XY.TryGetValue(n.Id, out var p) ? p : new Vector2(0.92f, 0.1f + 0.2f * extra++);
                pos[n.Id] = new Vector2(padX + f.x * (bw - padX * 2), padB + f.y * (bh - padB - padT));   // 판 왼쪽 아래 기준
            }
            float Sz(ProgNodeT n) => PotBig(n) ? big : small;
            RectTransform Place(RectTransform rt, Vector2 at, Vector2 size) { rt.anchorMin = rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f); rt.anchoredPosition = at; rt.sizeDelta = size; return rt; }
            // 선 — 앞 칸 → 이 칸(둘 다 열었으면 금)
            foreach (var n in t.Potential)
            {
                var pv = PrevOf(n);
                if (pv == null) continue;
                Vector2 a = pos[pv.Id], b = pos[n.Id], dir = (b - a).normalized;
                a += dir * (Sz(pv) / 2 + 6); b -= dir * (Sz(n) / 2 + 6);
                bool lit = LvOf(n.Id) > 0 && LvOf(pv.Id) > 0;
                var ln = Ui.Img(board, Theme.S("white"), lit ? Theme.Gold.A(0.85f) : Theme.Ink.A(0.22f), "line " + n.Id); ln.raycastTarget = false;
                var lr = ln.rectTransform; lr.anchorMin = lr.anchorMax = Vector2.zero; lr.pivot = new Vector2(0, 0.5f);
                lr.anchoredPosition = a; lr.sizeDelta = new Vector2((b - a).magnitude, lit ? 4 : 2);
                lr.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }
            int i = 0;
            foreach (var n in t.Potential)
            {
                var sv = ProgStore.PotNode(d.Id, n.Id);
                int lv = LvOf(n.Id);
                var pv = PrevOf(n);
                bool locked = sv != null ? (bool?)sv["locked"] ?? false : ProgStore.BestTier < n.Tier || (pv != null && LvOf(pv.Id) < 1);   // 여는 조건 = 사다리 단계 완주 + 앞 칸(10-22)
                bool open = lv > 0, max = lv >= n.Max, on = n.Id == potPick, ready = !max && !locked && sv != null && ((bool?)sv["ok"] ?? false);
                float sz = Sz(n);
                string id = n.Id;
                var at = pos[n.Id];
                if (on) { var glow = Ui.Img(board, Theme.S("soft"), Theme.Gold.A(0.55f), "glow " + id); glow.raycastTarget = false; Place(glow.rectTransform, at, Vector2.one * sz * 1.9f); }
                if (PotBig(n)) { var halo = Ui.Img(board, Theme.S("ring"), open ? Theme.Gold.A(0.45f) : Theme.Ink.A(0.16f), "halo " + id); halo.raycastTarget = false; Place(halo.rectTransform, at, Vector2.one * sz * 1.28f); }
                var bg = Ui.Img(board, Theme.S("circle"), Color.white, "pot " + id, true);
                var rt = Place(bg.rectTransform, at, new Vector2(sz, sz));
                var b = bg.gameObject.AddComponent<Btn>();
                b.Bg = bg; b.OnClick = () => { potPick = id; redraw?.Invoke(); };
                b.SetColor(open ? Theme.NavyWell.A(0.97f) : locked ? Color.white.A(0.05f) : Color.white.A(0.11f));
                var ring = Ui.Img(rt, Theme.S("ring"), open ? Theme.Gold : ready ? Theme.Gold.A(0.95f) : locked ? Theme.Ink.A(0.2f) : Theme.Ink.A(0.5f), "ring"); ring.rectTransform.Fill(); ring.raycastTarget = false;
                var hlr = Ui.Img(rt, Theme.S("ring"), Theme.Gold.A(0), "hl"); hlr.rectTransform.Fill(); hlr.raycastTarget = false; b.Hl = hlr;
                var ic = Ui.Img(rt, Theme.S(PotIcon(n)), open ? Theme.Ink : locked ? Theme.Dim.A(0.55f) : Theme.Sub.A(0.85f), "ic"); ic.preserveAspect = true; ic.raycastTarget = false;
                ic.rectTransform.Fill(sz * 0.27f, sz * 0.27f, sz * 0.27f, sz * 0.27f);
                if (n.Kind == "card" || n.Kind == "skill") { var upIc = Ui.Img(rt, Theme.S("ic_up"), open ? Theme.Gold : Theme.Sub.A(0.7f), "up"); upIc.preserveAspect = true; upIc.raycastTarget = false; upIc.rectTransform.At(0.5f, 0.5f, sz * 0.17f, -sz * 0.13f, sz * 0.24f, sz * 0.24f); }
                // 칸 번호 — 원 왼쪽 위(카제나 작은 번호)
                var idt = Ui.Text(board, id, Theme.FsCap, open ? Theme.Gold : Theme.Sub.A(0.8f), TextAlignmentOptions.BottomRight); idt.raycastTarget = false; idt.textWrappingMode = TextWrappingModes.NoWrap;
                var ir = idt.rectTransform; ir.anchorMin = ir.anchorMax = Vector2.zero; ir.pivot = new Vector2(1, 0); ir.anchoredPosition = at + new Vector2(-sz * 0.36f, sz * 0.36f); ir.sizeDelta = new Vector2(48, 20);
                // 작은 원 — 아래 알약 「a/b」
                if (!PotBig(n))
                {
                    var pill = Ui.Img(board, Theme.Pill, open ? Theme.NavyWell : Theme.NavyCell.A(0.92f), "lvpill " + id); pill.raycastTarget = false;
                    Place(pill.rectTransform, at + new Vector2(0, -sz / 2 - 4), new Vector2(Mathf.Max(64, sz * 0.95f), 24));
                    var pr = Ui.Img(pill.rectTransform, Theme.FramePill, (open ? Theme.Gold : Theme.Edge).A(open ? 0.8f : 0.35f), "rim"); pr.rectTransform.Fill(); pr.raycastTarget = false;
                    var pt = Ui.Text(pill.rectTransform, $"{lv}<color={Theme.SubTag}>/{n.Max}</color>", Theme.FsCap, max ? Theme.Gold : locked ? Theme.Dim : Theme.Ink, TextAlignmentOptions.Center); pt.rectTransform.Fill(); pt.textWrappingMode = TextWrappingModes.NoWrap;
                }
                if (locked) LockBadge(rt, sz * 0.28f, 1, 1, 0, 0);
                if (on)
                {   // 고른 칸 — 위 작은 금 ▼
                    var tri = Ui.Img(board, Theme.S("tri"), Theme.Gold, "pick " + id); tri.raycastTarget = false;
                    Place(tri.rectTransform, at + new Vector2(0, sz / 2 + (PotBig(n) ? sz * 0.2f : 0) + 14), new Vector2(16, 16)); tri.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
                }
                Stage.Hot["pot." + n.Id] = b;
                Tw.Pop(rt, 0.02f * i, 0.9f, 0.25f);
                i++;
            }

            // 오른쪽 — 고른 칸 판(카제나: 머리 종류 → 큰 원 + Lv → 이름 띠 → 효과 → 재료 칸 → 단추)
            var pn = t.Node(potPick);
            var svP = ProgStore.PotNode(d.Id, pn.Id);
            int plv = LvOf(pn.Id);
            var panel = DetailPanel(stage, "potpanel");
            var body = Ui.Rect("body", panel).Fill(24, 18, 22, 18);
            Ui.Col(body, Theme.C(8, 5), TextAnchor.UpperCenter, null, true, false);
            var kh = Ui.Title(body, PotKindHead(pn), Theme.FsLg, Theme.Ink, TextAlignmentOptions.Center); kh.Pref(-1, Theme.C(40, 34));
            var hr = Ui.Img(body, Theme.White, Theme.Edge.A(0.3f), "hr"); hr.Pref(-1, 2);
            // 큰 원 + Lv
            float isz = Theme.C(96, 76);
            var icRow = Ui.Rect("icon", body); icRow.Pref(-1, isz + 12);
            var disc = Ui.Img(icRow, Theme.S("circle"), plv > 0 ? Theme.NavyWell : Color.white.A(0.1f), "disc"); disc.rectTransform.At(0.5f, 0.5f, 0, 6, isz, isz);
            var drim = Ui.Img(disc.rectTransform, Theme.S("ring"), plv > 0 ? Theme.Gold : Theme.Ink.A(0.5f), "rim"); drim.rectTransform.Fill();
            var dic = Ui.Img(disc.rectTransform, Theme.S(PotIcon(pn)), plv > 0 ? Theme.Ink : Theme.Sub, "ic"); dic.preserveAspect = true; dic.rectTransform.Fill(isz * 0.26f, isz * 0.26f, isz * 0.26f, isz * 0.26f);
            var lvp = Ui.Img(icRow, Theme.Pill, Theme.NavyCell, "lvpill"); lvp.rectTransform.At(0.5f, 0, 0, 0, 92, 24);
            var lvpt = Ui.Text(lvp.rectTransform, pn.Max > 1 ? $"{plv}<color={Theme.SubTag}>/{pn.Max}</color>" : plv > 0 ? "열림" : "닫힘", Theme.FsCap, Theme.Ink, TextAlignmentOptions.Center); lvpt.rectTransform.Fill(); lvpt.textWrappingMode = TextWrappingModes.NoWrap;
            // 이름 띠 — 안 열면 짙은 띠 + 자물쇠, 열면 금 띠
            var band = Ui.Img(body, Theme.Round, plv > 0 ? Theme.Gold.A(0.9f) : Theme.NavyWell, "nameband"); band.Pref(-1, Theme.C(34, 30));
            if (plv == 0) { var lk = Ui.Img(band.rectTransform, Theme.S("ic_lock"), Theme.Sub, "lock"); lk.preserveAspect = true; lk.rectTransform.At(0, 0.5f, 10, 0, 18, 18); }
            var bt = Ui.Title(band.rectTransform, $"{pn.Id}  {PotName(t, pn, d.Id)}", Theme.FsMd, plv > 0 ? Theme.Brown : Theme.Sub, TextAlignmentOptions.Center); bt.rectTransform.Fill(30, 0, 30, 0);
            bt.textWrappingMode = TextWrappingModes.NoWrap; bt.enableAutoSizing = true; bt.fontSizeMin = 12; bt.fontSizeMax = Theme.FsMd;
            // 효과 — 지금(안 열었으면 1단계 효과) · 다음 단계
            string eff = PotEffect(t, pn, Math.Max(1, plv), d, now);
            if (plv > 0 && plv < pn.Max) eff += $"\n<color={Theme.SubTag}>다음 단계 —</color> {PotEffect(t, pn, plv + 1, d, now)}";
            var e0 = Ui.Text(body, eff, Theme.FsBody, Theme.Ink, TextAlignmentOptions.TopLeft); e0.Pref(-1, Theme.C(120, 92));
            e0.enableAutoSizing = true; e0.fontSizeMin = 11; e0.fontSizeMax = Theme.FsBody;
            string state = plv >= pn.Max ? "최대 단계" : plv > 0 ? $"단계 {plv}/{pn.Max}" : "열지 않은 칸";
            string cond = string.Join(" · ", new[] { pn.Tier > 0 ? $"난이도 단계 {pn.Tier} 완주" : null, PrevOf(pn) is ProgNodeT pp ? $"앞 칸 {pp.Id}" : null }.Where(x => x != null));
            var st = Ui.Text(body, state + (cond.Length > 0 ? $" · 여는 조건 — {cond}" : " · 처음부터 열 수 있다"), Theme.FsCap, Theme.Dim, TextAlignmentOptions.Center); st.Pref(-1, 22);
            st.textWrappingMode = TextWrappingModes.NoWrap; st.enableAutoSizing = true; st.fontSizeMin = 10; st.fontSizeMax = Theme.FsCap;
            var fill = Ui.Rect("fill", body); fill.Pref(-1, 0, -1, 1);
            // 재료 칸(다음 단계 값 — 서버 next)
            var nx = svP?["next"] as JObject;
            if (nx == null && plv < pn.Max)
            {   // 성장 꺼짐 · 로그인 전 — 보기 전용: 다음 단계 값을 미리 보기 사본(서버 POT_COST 와 같은 값)으로
                var pc = ProgStore.PreviewCost(pn.Id, plv + 1);
                if (pc != null) { var items0 = new JObject(); foreach (var kv in pc) items0[kv.Key] = kv.Value; nx = new JObject { ["items"] = items0 }; }
            }
            if (plv < pn.Max && nx != null)
            {
                var mats = Ui.Rect("mats", body); float cs = Theme.C(78, 68); mats.Pref(-1, cs + 26);
                Ui.Row(mats, 8, TextAnchor.MiddleLeft, null, false, false);
                if (nx["items"] is JObject it) foreach (var kv in it) { var mt = MatCell(mats, kv.Key, cs); mt.text = HaveNeed(ProgStore.Wallet(kv.Key), (int)kv.Value); }
            }

            // 단추 — 될 때 금 「칸 열기 · 단계 올리기」, 안 될 때 흐린 회색 + 까닭
            bool can = have && svP != null && ((bool?)svP["ok"] ?? false) && plv < pn.Max;
            string pid = pn.Id;
            string label = can ? (plv == 0 ? "칸 열기" : "단계 올리기")
                : plv >= pn.Max ? "최대 단계" : ProgStore.BestTier < pn.Tier ? $"난이도 단계 {Ko.J(pn.Tier.ToString(), "을를")} 완주하면 열린다" : !growOn ? "성장이 열리면 키울 수 있다" : !have ? (Online.Ready ? "계정 성장을 불러오는 중…" : "로그인하면 키울 수 있다") : svP?["why"]?.Type == JTokenType.String ? (string)svP["why"] : "열 수 없다";
            var up = Btn.Make(body, null, BtnStyle.PillGold, () => ProgStore.Potential(d.Id, pid, r =>
            {
                if (r != null && r.Ok) Toast.Show($"{h.ko} 잠재력 {pid} — 단계 {r.Json["lv"]?[1]}");
                else Toast.Show(r?.Str("why") ?? Online.ErrorText(r), 3f);
                redraw?.Invoke();
            }), 0, "potup");
            up.Pref(-1, Theme.BtnMainH);
            var ut = Ui.Title(up.transform, label, Theme.FsLg, Theme.Brown, TextAlignmentOptions.Center); ut.rectTransform.Fill(16, 0, 16, 0);
            ut.textWrappingMode = TextWrappingModes.NoWrap; ut.enableAutoSizing = true; ut.fontSizeMin = 12; ut.fontSizeMax = Theme.FsLg;
            up.Label = ut;
            up.Interactable = can; up.Why = label;
            Stage.Hot["pot.up"] = up;
            Tw.Rise(panel, 0.08f, 30, 0.4f, Vector2.right);
        }

        // ── 카드 상세 — 실제 수치(지금 능력치로 셈한 피해 · 실드 · 회복) ──
        /// <summary>카드의 피해 · 방어 · 실드 · 회복 조각을 주인 사도의 지금 능력치로 셈한 값(성장 켬일 때만 — 카드 면 % 는 그대로라 실제 값을 따로 보인다). 셀 것이 없으면 null.</summary>
        /// <summary>사도 칸 덤으로 덧붙은 조각(피해 · 실드 · 회복 말고 — 고유 효과 쌓기 · 카드 만들기 · 게이지 …)을 짧은 글로.</summary>
        IEnumerable<string> HeroModLines(Core.CardView before, Core.CardView after)
        {
            foreach (var f in after.Fx)
                if (!before.Fx.Contains(f) && f.K != FxK.Dmg && f.K != FxK.Shield && f.K != FxK.Heal && f.K != FxK.Block)
                    yield return $"<color={Theme.GoldTag}>+</color> " + (f.K == FxK.Stack ? $"「{f.Id}」 {f.V}" : f.K == FxK.Make ? $"「{P.Data.Card(f.Id)?.Name ?? f.Id}」 {Math.Max(1, f.V)}장 만들기" : f.K == FxK.Gauge ? $"고학년 게이지 +{f.V}" : f.K == FxK.Draw ? $"카드 {f.V}장 뽑기" : f.K);
        }

        CardTerms.Term RealTerm(Core.CardView v)
        {
            if (!ProgStore.On || v == null) return null;
            string owner = v.Owner ?? v.Def?.Hero;
            var hd = owner != null ? P.Data.Hero(owner) : null;
            if (hd == null) return null;
            int atk, def; string where;
            ProgMods mods;
            bool inRun = P.Has && RunPort.HasSave && P.S.Done == null && P.S.Party.Contains(owner);   // 끝난 판(저장을 지움)은 판 밖으로
            if (inRun)
            {
                var g = P.Run.GearStats().TryGetValue(owner, out var gs) ? gs : new Stats();
                mods = P.S.Prog != null && P.S.Prog.TryGetValue(owner, out var m) ? m : null;
                var pm = mods?.Base ?? new Stats();
                atk = hd.Atk + pm.Atk + g.Atk; def = Num.Round(hd.Def + pm.Def + g.Def * R.GUARD_EXTRA_K);
                where = "이번 모험(잠재력 · 아티팩트 포함)";
            }
            else
            {
                var s = ProgStore.SumOf(owner);
                mods = ProgStore.ModsOf(owner);
                atk = s?.Atk ?? hd.Atk; def = s?.Def ?? hd.Def;
                where = $"지금 잠재력({ProgStore.PotProgress(owner).lv}단계)";
            }
            // 잠재력 사도 칸(03-1 · 05-1 · 07) 카드 덤 — 전투가 얹는 모습(Prog.CardWith)과 같은 모습으로 셈한다
            var vm = mods?.Cards != null ? Prog.CardWith(P.Data, v, GameData.BaseId(v.Id), mods.Cards) : v;
            bool heroMod = !ReferenceEquals(vm, v);
            var v0 = v;
            v = vm;
            // 카드군 덤(잠재력 02 · 03 · 04) — 엔진이 그 카드를 낸 사도의 몫으로 피해 · 실드 · 치유에 곱한다(ctx.Scale)
            double cb = ProgStore.CardBonus(mods, v.Def, v.Def?.Hero == null && v.Owner != null);
            double cx = 1 + cb;
            var lines = new List<string>();
            foreach (var f in v.Fx ?? new List<Fx>())
            {
                if (f.Ratio <= 0 || f.OfEvent > 0) continue;
                int hits = Math.Max(1, f.Hits);
                switch (f.K)
                {
                    case FxK.Dmg:
                        if (f.Fixed) break;
                        int one = R.FinalDamage(f.Base == "def" ? R.DefDmgStat(atk, def) : atk, f.Ratio * cx);
                        lines.Add($"피해 {Num.Round(f.Ratio * 100)}% → <color={Theme.GoldTag}>{N0(one)}</color>{(hits > 1 ? $" × {hits}번" : "")}");
                        break;
                    case FxK.Block: case FxK.Shield: case FxK.Heal:
                        int g = f.Fixed ? Math.Max(1, Num.Round(def * f.Ratio * cx)) : Math.Max(1, Num.Round(def * f.Ratio * cx * R.RoleGuard(hd.Role)));
                        lines.Add($"{(f.K == FxK.Heal ? "회복" : f.K == FxK.Block ? "방어" : "실드")} {Num.Round(f.Ratio * 100)}% → <color={Theme.GoldTag}>{N0(g)}</color>");
                        break;
                }
            }
            if (lines.Count == 0) return null;
            return new CardTerms.Term
            {
                Name = "실제 수치", Hero = true, Kind = $"{Roster.OfCore(owner)?.ko ?? owner} · {where}",
                Body = $"공격력 {N0(atk)} · 방어력 {N0(def)} 기준{(cb > 0 ? $" · <color={Theme.GoldTag}>잠재력 카드 +{Num.Round(cb * 100)}%</color>" : "")}{(heroMod ? $" · <color={Theme.GoldTag}>잠재력 사도 칸 덤</color>" : "")}(전투 중 증감 · 치명 · 상성 빼고)\n" + string.Join("\n", lines)
                    + (heroMod ? "\n" + string.Join("\n", HeroModLines(v0, v)) : ""),
            };
        }
    }
}
