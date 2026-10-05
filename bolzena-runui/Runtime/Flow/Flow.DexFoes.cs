using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 적 도감 — 사도 · 교주 카드 · 장비와 같은 화면 틀(왼쪽 필터 탭 · 격자 · 위 오른쪽 정렬과 도감 탭 · 아래 남색 띠).
    //   필터 = 마을(마을 파일 6) + 등급(일반 · 엘리트 · 보스 · 소환물). 정렬 = 마을 → 등급(보스 먼저) → 가나다(이름 · 강인도로 바꿀 수 있다).
    //   칸 = 원작 정지 아이콘(Art/Monster — 사도 클론은 그 사도 상반신, 그림이 없으면 대역 글리프 + 이름) · 이름 · 등급 띠 · 강인도 칸.
    //   상세 = 큰 그림 · 마을 · 층 · 등급 · HP · 강인도 · 기본 성격(판마다 통일 규칙 안내) · 행동 · 패시브 · 쌓이는 수치 · 희귀종 · 소환물 · 이야기.
    //   빌린 몸 클론(clone~사도~몸)은 데이터에 따로 없으므로 늘어놓지 않고, 원래 클론의 상세에 「그 성격 판이면 ○○ 클론」 으로 안내한다.
    public partial class Flow
    {
        class FoeListState { public string Village, Grade, Focus; public string Sort = "마을"; public float ScrollY; public bool Drawn; }
        FoeListState dexFoes;
        static readonly string[] FoeGrades = { "보스", "엘리트", "일반", "소환물" };

        /// <summary>적 한 줄의 자리 — 처음 나오는 마을 · 층 · 등급(소환물은 세운 적의 마을).</summary>
        class FoeSpot { public EnemyDef Def; public string Village, VillageName; public int VillageIdx = 99, Floor = -1; public string Grade = "일반"; public string Summoner; public bool NoTough; }
        List<FoeSpot> foeIndex;

        static Color FoeGradeColor(string g) => g switch
        {
            "보스" => new Color(1f, 0.36f, 0.42f),
            "엘리트" => new Color(0.65f, 0.49f, 0.94f),
            "소환물" => new Color(0.31f, 0.82f, 0.88f),
            _ => new Color(0.62f, 0.66f, 0.76f),
        };
        static int FoeGradeOrder(string g) => Array.IndexOf(FoeGrades, g);

        static IEnumerable<Intent> AllIntents(EnemyDef e)
        {
            if (e.Open != null) yield return e.Open;
            foreach (var i in e.Intents) yield return i;
            if (e.Phase != null) foreach (var i in e.Phase.Intents) yield return i;
            if (e.Phase2 != null) foreach (var i in e.Phase2.Intents) yield return i;
            foreach (var p in e.Passives) if (p.Do != null) yield return p.Do;
            foreach (var c in e.Counters) if (c.Act != null) yield return c.Act;
        }

        static IEnumerable<Intent> Chain(Intent i)
        {
            for (var x = i; x != null; x = x.Next) yield return x;
        }

        List<FoeSpot> FoeIndex()
        {
            if (foeIndex != null) return foeIndex;
            var d = P.Data;
            var spots = new Dictionary<string, FoeSpot>();
            var vills = d.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();
            for (int vi = 0; vi < vills.Count; vi++)
            {
                var v = d.Villages[vills[vi]];
                for (int f = 0; f < v.Floors.Count; f++)
                {
                    var fl = v.Floors[f];
                    void Put(string id, string grade)
                    {
                        if (id == null || spots.ContainsKey(id) || d.Enemy(id) == null) return;
                        spots[id] = new FoeSpot { Def = d.Enemy(id), Village = vills[vi], VillageName = v.Name, VillageIdx = vi, Floor = f, Grade = grade };
                    }
                    foreach (var id in fl.Boss) Put(id, "보스");
                    foreach (var line in fl.Elites) foreach (var id in line) Put(id, "엘리트");
                    foreach (var pool in fl.Pools) foreach (var line in pool) foreach (var id in line) Put(id, "일반");
                }
            }
            // 소환물 — 마을 줄에 없고 누가 세우는 적(세운 적의 마을 · 층)
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (var s in spots.Values.ToList())
                    foreach (var it in AllIntents(s.Def).SelectMany(Chain))
                        if (it.T == "summon" && it.Id != null && !spots.ContainsKey(it.Id) && d.Enemy(it.Id) != null)
                        {
                            spots[it.Id] = new FoeSpot { Def = d.Enemy(it.Id), Village = s.Village, VillageName = s.VillageName, VillageIdx = s.VillageIdx, Floor = s.Floor, Grade = "소환물", Summoner = s.Def.Name, NoTough = it.NoTough };
                            grew = true;
                        }
            }
            // 어디에도 없는 적(데이터에만) — 마을 없음
            foreach (var kv in d.Enemies)
                if (!spots.ContainsKey(kv.Key)) spots[kv.Key] = new FoeSpot { Def = kv.Value, VillageName = "기타", Grade = kv.Value.Boss ? "보스" : "일반" };
            return foeIndex = spots.Values.ToList();
        }

        /// <summary>강인도 칸 수 — 적 데이터(tough), 없으면 등급 기본(R.TOUGH). 강인도 없는 소환물은 0.</summary>
        static double FoeTough(FoeSpot s)
        {
            if (s.NoTough) return 0;
            if (s.Def.Tough > 0) return s.Def.Tough;
            return s.Grade == "보스" || s.Def.Boss ? R.TOUGH.Boss : s.Grade == "엘리트" ? R.TOUGH.Elite : R.TOUGH.Fight;
        }

        static string FoeNum(double v)
        {
            foreach (int dn in new[] { 1, 2, 3, 4, 5, 6 })
            {
                double k = Math.Round(v * dn);
                if (Math.Abs(v * dn - k) > 0.02) continue;
                int ki = (int)k, w = ki / dn, r = ki % dn;
                return r == 0 ? w.ToString() : (w > 0 ? w + " " : "") + r + "/" + dn;
            }
            return v.ToString("0.0");
        }

        /// <summary>적 그림 — 사도 클론은 그 사도 상반신, 아니면 원작 정지 아이콘. 없으면 null(대역 글리프).</summary>
        Sprite FoeArt(EnemyDef e, float ratio)
        {
            if (e == null) return null;
            if (e.Clone != null)
            {
                var h = Roster.All.FirstOrDefault(x => x.CoreId == e.Clone || x.key == e.Clone);
                var up = h != null ? CardArt.Upper(h.art, ratio, 0.55f) : null;
                if (up != null) return up;
            }
            var ic = e.Art?.Icon;
            if (ic == null) FoeIcons().TryGetValue(e.Id, out ic);
            return string.IsNullOrEmpty(ic) ? null : Resources.Load<Sprite>("Art/Monster/" + ic);
        }

        static readonly (string ko, string en)[] NatEn = { ("순수", "naive"), ("광기", "mad"), ("활발", "jolly"), ("우울", "gloomy"), ("냉정", "cool") };

        /// <summary>같은 몬스터의 성격별 아이콘 — 이 적 아이콘 이름(icon_<몬스터><성격>)에서 몬스터를 떼어 다섯 성격을 찾는다(있는 것만).</summary>
        List<(string nature, Sprite sp)> NatureLooks(EnemyDef e)
        {
            var o = new List<(string, Sprite)>();
            var ic = e.Art?.Icon;
            if (ic == null) FoeIcons().TryGetValue(e.Id, out ic);
            if (string.IsNullOrEmpty(ic) || !ic.StartsWith("icon_")) return o;
            string mon = null;
            foreach (var (_, en) in NatEn) if (ic.EndsWith(en)) { mon = ic.Substring(5, ic.Length - 5 - en.Length); break; }
            if (mon == null) return o;
            foreach (var (ko, en) in NatEn)
            {
                var sp = Resources.Load<Sprite>("Art/Monster/icon_" + mon + en);
                if (sp != null) o.Add((ko, sp));
            }
            return o;
        }

        // 적 그림 표(콘텐츠 world/villages/_그림.json) 의 icon — 적 데이터에 Art 가 없을 때(전투 Look.EnemyIcon 과 같은 표)
        static Dictionary<string, string> foeIcons;
        static Dictionary<string, string> FoeIcons()
        {
            if (foeIcons != null) return foeIcons;
            foeIcons = new Dictionary<string, string>();
            try
            {
                var p = System.IO.Path.Combine(RunPort.CoreDataDir, "world", "villages", "_그림.json");
                if (System.IO.File.Exists(p))
                    foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(p), @"""([^""]+)""\s*:\s*\{[^{}]*?""icon""\s*:\s*""([^""]+)"""))
                        foeIcons[m.Groups[1].Value] = m.Groups[2].Value;
            }
            catch (Exception ex) { Debug.LogWarning("[Dex] 적 그림 표를 못 읽었습니다: " + ex.Message); }
            return foeIcons;
        }

        void DexFoes(RectTransform host, Action back, FoeListState ls)
        {
            Ui.Clear(host);
            RosterBg(host, 0.9f);
            void Rebuild() => DexFoes(host, back, ls);
            void Refilter() { ls.ScrollY = 0; Rebuild(); }
            RosterHead(host, "적 도감", back);

            var tr = Ui.Rect("tools", host).At(1, 1, -Theme.Gutter, -20, 1240, 52);
            Ui.Row(tr, 10, TextAnchor.MiddleRight, null, false, true);
            var sorts = new[] { "마을", "이름", "강인도" };
            if (Array.IndexOf(sorts, ls.Sort) < 0) ls.Sort = "마을";
            var sb = NavyPill(tr, "ic_refresh", $"<color={Theme.SubTag}>정렬</color>  {ls.Sort}", () => { ls.Sort = sorts[(Array.IndexOf(sorts, ls.Sort) + 1) % sorts.Length]; Refilter(); }, "sort", 168);
            Stage.Hot["list.sort"] = sb;
            DexTabs(tr, host, "적", back);

            // 왼쪽 세로 필터 — 마을 6 + 등급
            float railW = Theme.C(116, 104);
            var rail = Ui.Rect("rail", host); rail.anchorMin = new Vector2(0, 0); rail.anchorMax = new Vector2(0, 1); rail.pivot = new Vector2(0, 1);
            rail.sizeDelta = new Vector2(railW, -110); rail.anchoredPosition = new Vector2(Theme.Gutter, -96);
            Ui.Col(rail, Theme.C(6, 3), TextAnchor.UpperCenter, null, false, false);
            float tabH = Theme.C(46, 34);
            void Tab(string label, bool on, Action go, Color? dot = null, string hot = null)
            {
                var b = Btn.Make(rail, null, BtnStyle.Ghost, go, 0, "tab " + label);
                b.Bg.sprite = Theme.Round; b.SetColor(on ? Theme.Gold.A(0.16f) : Theme.NavyCell.A(0.6f));
                b.Pref(railW, tabH);
                var t = Ui.Title(b.transform, label, label == "ALL" ? Theme.FsMd : Theme.FsSm, on ? Theme.Gold : dot != null ? Color.Lerp(dot.Value, Color.white, 0.35f) : Theme.Sub, TextAlignmentOptions.Center);
                t.rectTransform.Fill(8, 0, 8, 0);
                t.textWrappingMode = TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = label == "ALL" ? Theme.FsMd : Theme.FsSm;
                if (dot != null) { var dd = Ui.Img(b.transform, Theme.S("circle"), dot.Value, "dot"); dd.rectTransform.At(1, 1, -5, -5, 8, 8); }
                if (on)
                {
                    var br = Ui.Img(b.transform, Theme.Frame, Theme.Gold, "on"); br.rectTransform.Fill();
                    var bar = Ui.Img(b.transform, Theme.Round, Theme.Gold, "bar"); bar.rectTransform.At(0, 0.5f, -10, 0, 4, tabH * 0.55f);
                }
                Stage.Hot[hot ?? "filter:" + label] = b;
            }
            var idx0 = FoeIndex();
            Tab("ALL", ls.Village == null, () => { ls.Village = null; Refilter(); });
            foreach (var v in P.Data.Villages.Keys.OrderBy(x => x, StringComparer.Ordinal))
            {
                var vv = v;
                Tab(P.Data.Villages[v].Name, ls.Village == vv, () => { ls.Village = vv; Refilter(); });
            }
            var gap = Ui.Img(rail, Theme.White, Theme.Edge.A(0.35f), "gap"); gap.Pref(railW - 20, 1);
            foreach (var g in FoeGrades)
            {
                var gg = g;
                Tab(g, ls.Grade == gg, () => { ls.Grade = ls.Grade == gg ? null : gg; Refilter(); }, FoeGradeColor(g));
            }

            // 격자
            float footH = Theme.C(84, 72);
            var area = Ui.Rect("grid", host); area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(Theme.Gutter + railW + 16, footH + 8); area.offsetMax = new Vector2(-Theme.Gutter, -92);
            var content = Ui.Scroll(area, out var sr);
            ScrollBar(area, sr);
            float k = Theme.C(1f, 0.92f);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            float cw = 158 * k;
            grid.cellSize = new Vector2(cw, cw * 1.3f);
            grid.spacing = new Vector2(14, 14); grid.padding = new RectOffset(6, 18, 8, 16); grid.childAlignment = TextAnchor.UpperLeft;

            IEnumerable<FoeSpot> list = idx0.Where(s => (ls.Village == null || s.Village == ls.Village) && (ls.Grade == null || s.Grade == ls.Grade));
            list = ls.Sort switch
            {
                "이름" => list.OrderBy(s => s.Def.Name, StringComparer.Ordinal),
                "강인도" => list.OrderByDescending(s => FoeTough(s)).ThenBy(s => s.Def.Name, StringComparer.Ordinal),
                _ => list.OrderBy(s => s.VillageIdx).ThenBy(s => FoeGradeOrder(s.Grade)).ThenBy(s => s.Def.Name, StringComparer.Ordinal),
            };
            var shown = list.ToList();
            string focusName = null, focusSub = null;
            bool anim = !ls.Drawn;
            for (int i = 0; i < shown.Count; i++)
            {
                var s = shown[i];
                FoeTile(content, s, ls, cw, i, anim, Rebuild);
                if (ls.Focus == s.Def.Id) { focusName = s.Def.Name; focusSub = $"{s.VillageName}{(s.Floor >= 0 ? $" {s.Floor + 1}층" : "")} · {s.Grade}"; }
            }
            if (shown.Count == 0) { var none = Ui.Text(content, "맞는 적이 없습니다", Theme.FsLg, Theme.Sub, TextAlignmentOptions.Center); none.Pref(400, 80); }
            ls.Drawn = true;
            if (ls.ScrollY > 0)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                float maxY = Mathf.Max(0, content.rect.height - ((RectTransform)sr.viewport).rect.height);
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Min(ls.ScrollY, maxY));
                sr.StopMovement();
            }
            sr.onValueChanged.AddListener(_ => { if (content) ls.ScrollY = content.anchoredPosition.y; });

            // 아래 띠
            var fbg = Ui.Img(host, Theme.White, Theme.NavyPanel.A(0.96f), "footbg", true); fbg.rectTransform.Band(0, footH, 0, 0, 0);
            var line = Ui.Img(host, Theme.White, Theme.Edge.A(0.35f), "rule"); line.rectTransform.Band(0, 1, 0, 0, footH);
            var foot = Ui.Rect("foot", host).Band(0, footH, Theme.Gutter, Theme.Gutter, 0);
            var info = Ui.Text(foot, focusName != null ? $"<b>{focusName}</b>  <color={Theme.SubTag}>{focusSub}</color>" : $"적 {shown.Count}종 · 누르면 상세 정보  <color={Theme.SubTag}>— 판마다 모든 적의 성격은 판의 적 속성 하나로 맞춰진다</color>", Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.rectTransform.Fill(0, 0, 280, 0); info.textWrappingMode = TextWrappingModes.NoWrap; info.overflowMode = TextOverflowModes.Ellipsis;
            var detail = Btn.Make(foot, null, BtnStyle.PillDark, () => { if (ls.Focus != null) EnemyDetail(ls.Focus); }, 0, "detail");
            var drt = detail.GetComponent<RectTransform>(); drt.At(1, 0.5f, 0, 0, 250, 58);
            var dzi = Ui.Img(drt, Theme.S("ic_zoom"), Theme.Gold, "ic"); dzi.rectTransform.At(0, 0.5f, 22, 0, 24, 24); dzi.preserveAspect = true;
            var dl = Ui.Title(drt, "상세 정보", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineRight); dl.rectTransform.Fill(50, 0, 26, 0);
            detail.Interactable = focusName != null; detail.Why = "적을 먼저 고르세요";
            Stage.Hot["list.detail"] = detail;
            // 검색 칸 — 이름 · 초성(도감마다 따로 기억)
            DexSearch(tr, "적", content, area, n => n.StartsWith("foe ") && P.Data.Enemy(n.Substring(4)) is Core.EnemyDef e ? new[] { e.Name } : null,
                info, n => $"적 {n}종 · 누르면 상세 정보");
        }

        /// <summary>강인도 칸 — n 칸(소수 끝 칸은 그만큼), 없으면 「강인도 없음」 글.</summary>
        static void ToughPips(RectTransform parent, double tough, float w, float h, float y, bool left = false)
        {
            if (tough <= 0)
            {
                var t = Ui.Text(parent, "강인도 없음", Theme.FsCap - 1, Theme.Dim, TextAlignmentOptions.Center); t.rectTransform.Band(0, h + 8, 6, 6, y - 4);
                return;
            }
            int n = (int)Math.Ceiling(tough - 1e-6);
            float gap = n > 8 ? 2 : 3, cw = (w - gap * (n - 1)) / n;
            var row = Ui.Rect("tough", parent).At(left ? 0 : 0.5f, 0, left ? 6 : 0, y, w, h);
            var back = Ui.Img(row, Theme.White, new Color(0.01f, 0.01f, 0.03f, 0.9f), "back"); back.rectTransform.Fill(-2, -2, -2, -2);
            for (int i = 0; i < n; i++)
            {
                float cap = (float)Math.Min(1, tough - i);
                var c = Ui.Img(row, Theme.White, new Color(0.84f, 0.76f, 1f), "pip" + i);
                c.rectTransform.anchorMin = c.rectTransform.anchorMax = c.rectTransform.pivot = new Vector2(0, 0.5f);
                c.rectTransform.anchoredPosition = new Vector2(i * (cw + gap), 0);
                c.rectTransform.sizeDelta = new Vector2(cw * cap, h);
            }
        }

        void FoeTile(RectTransform content, FoeSpot s, FoeListState ls, float w, int idx, bool anim, Action rebuild)
        {
            var e = s.Def;
            var gc = FoeGradeColor(s.Grade);
            var b = Btn.Make(content, null, BtnStyle.Ghost, () => { ls.Focus = e.Id; rebuild(); EnemyDetail(e.Id); }, 0, "foe " + e.Id);
            b.Bg.sprite = Theme.Round; b.SetColor(Color.Lerp(Theme.NavyWell, gc, 0.16f));
            var rt = b.GetComponent<RectTransform>();
            float h = w * 1.3f;
            var win = Ui.Rect("win", rt).Fill(4, h * 0.30f, 4, 4);
            win.gameObject.AddComponent<RectMask2D>();
            var glow = Ui.Img(win, Theme.S("soft"), gc.A(0.3f), "glow"); glow.rectTransform.At(0.5f, 0.5f, 0, 0, w * 1.1f, w);
            var pic = FoeArt(e, (w - 8) / (h * 0.7f - 4));
            if (pic != null)
            {
                var ic = Ui.Img(win, pic, Color.white, "art");
                if (e.Clone != null) ic.rectTransform.Fill();
                else { ic.preserveAspect = true; ic.rectTransform.At(0.5f, 0.5f, 0, -2, w * 0.86f, w * 0.86f); }
            }
            else
            {
                // 그림 없는 적 — 대역 글리프 + 이름
                var g = Ui.Img(win, Theme.S(s.Grade == "보스" ? "ic_crown" : "ic_skull"), gc.A(0.8f), "glyph"); g.preserveAspect = true; g.rectTransform.At(0.5f, 0.5f, 0, 10, w * 0.36f, w * 0.36f);
                var gn = Ui.Text(win, "그림 없음", Theme.FsCap - 1, Theme.Dim, TextAlignmentOptions.Center); gn.rectTransform.Band(0, 20, 4, 4, 8);
            }
            // 위 — 성격 아이콘(기본 성격) · 등급 알약
            var nsp = e.Nature != null ? Icon("성격_" + e.Nature) : null;
            if (nsp != null) { var ni = Ui.Img(rt, nsp, Color.white, "nat"); ni.rectTransform.At(0, 1, 6, -6, 26, 26); ni.preserveAspect = true; }
            var gp = Ui.Img(rt, Theme.Pill, gc.A(0.92f), "grade"); gp.rectTransform.At(1, 1, -6, -6, s.Grade.Length > 2 ? 58 : 46, 20);
            var gt = Ui.Title(gp.transform, s.Grade, Theme.FsCap - 1, Theme.Brown, TextAlignmentOptions.Center); gt.rectTransform.Fill();
            // 아래 — 이름 · 강인도 칸 · 등급 띠
            var nm = Ui.Title(rt, e.Name, Theme.FsSm, Theme.Ink, TextAlignmentOptions.Center); nm.rectTransform.Band(0, h * 0.14f, 6, 6, h * 0.13f);
            nm.enableAutoSizing = true; nm.fontSizeMin = 10; nm.fontSizeMax = Theme.FsSm; nm.textWrappingMode = TextWrappingModes.NoWrap; nm.Outline(0.2f);
            ToughPips(rt, FoeTough(s), w * 0.7f, Theme.C(8, 7), h * 0.07f);
            var band = Ui.Img(rt, Theme.Round, gc, "band"); band.rectTransform.Band(0, 3, 16, 16, 3);
            var rim = Ui.Img(rt, Theme.Frame, gc.A(0.7f), "rim"); rim.rectTransform.Fill();
            if (ls.Focus == e.Id) { var fr = Ui.Img(rt, Theme.S("frame_thick", 24), Theme.Sky, "focus"); fr.rectTransform.Fill(-3, -3, -3, -3); }
            Stage.Hot["dexfoe:" + idx.ToString("D3")] = b;
            if (anim && idx < 30) Tw.Pop(rt, 0.012f * idx, 0.88f, 0.28f);
        }

        /// <summary>적 상세 — 왼쪽 큰 그림(등급 빛) · 오른쪽 자리 · 수치 · 성격 · 행동 · 패시브 · 쌓이는 수치 · 희귀종 · 소환물 · 이야기.</summary>
        public void EnemyDetail(string id)
        {
            var s = FoeIndex().FirstOrDefault(x => x.Def.Id == id);
            if (s == null) return;
            var e = s.Def;
            var gc = FoeGradeColor(s.Grade);
            string where = $"{s.VillageName}{(s.Floor >= 0 ? $" · {s.Floor + 1}층" : "")} · {s.Grade}";
            var (body, close, _) = Stage.ModalBox("foezoom", 1120, Theme.C(640, 660), e.Name, where);
            float aw = Theme.C(340, 300);
            var well = Ui.Img(body, Theme.Round, Color.Lerp(Theme.NavyWell, gc, 0.2f), "well"); well.rectTransform.At(0, 0.5f, 10, 0, aw, aw * 1.25f);
            var mask = Ui.Rect("mask", well.rectTransform).Fill(4, 4, 4, 4); mask.gameObject.AddComponent<RectMask2D>();
            var glow = Ui.Img(mask, Theme.S("soft"), gc.A(0.45f), "glow"); glow.rectTransform.At(0.5f, 0.55f, 0, 0, aw * 1.2f, aw * 1.2f);
            var pic = FoeArt(e, (aw - 8) / (aw * 1.25f - 8));
            if (pic != null)
            {
                var ic = Ui.Img(mask, pic, Color.white, "art");
                if (e.Clone != null) ic.rectTransform.Fill();
                else { ic.preserveAspect = true; ic.rectTransform.At(0.5f, 0.56f, 0, 0, aw * 0.86f, aw * 0.86f); }
            }
            else
            {
                var g = Ui.Img(mask, Theme.S(s.Grade == "보스" ? "ic_crown" : "ic_skull"), gc.A(0.8f), "glyph"); g.preserveAspect = true; g.rectTransform.At(0.5f, 0.56f, 0, 0, aw * 0.4f, aw * 0.4f);
                var gn = Ui.Text(mask, "그림 없음", Theme.FsSm, Theme.Dim, TextAlignmentOptions.Center); gn.rectTransform.At(0.5f, 0.3f, 0, 0, aw, 30);
            }
            var gp = Ui.Img(well.transform, Theme.Pill, gc, "grade"); gp.rectTransform.At(0.5f, 0, 0, 16, 150, 34);
            var gt = Ui.Title(gp.transform, s.Grade, Theme.FsMd, Theme.Brown, TextAlignmentOptions.Center); gt.rectTransform.Fill();
            var rim = Ui.Img(well.transform, Theme.Frame, gc, "rim"); rim.rectTransform.Fill();

            var right = Ui.Rect("right", body).Fill(aw + 40, 0, 6, 0);
            var content = Ui.Scroll(right, out _);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(4, 10, 4, 10), true, false);
            void Line(string text, float size, Color c)
            {
                var t = Ui.Text(content, text, size, c, TextAlignmentOptions.TopLeft);
                t.textWrappingMode = TextWrappingModes.Normal;
                t.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            string Hx(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
            double tough = FoeTough(s);
            W.Section(content, "수치", null, 36);
            Line($"HP <color={Theme.GoldTag}>{e.Hp:N0}</color>    강인도 " + (tough > 0 ? $"<color=#D6C2FF>{FoeNum(tough)}</color> <size=80%><color={Theme.SubTag}>칸{(e.ToughTaken > 0 && e.ToughTaken != 1 ? $" · 받는 강인도 피해 ×{e.ToughTaken:0.##}" : "")}</color></size>" : $"<color={Theme.SubTag}>없음 — 격파되지 않는다(소환물)</color>"), Theme.FsLg, Theme.Ink);
            if (tough > 0) { var pr = Ui.Rect("pips", content); pr.Pref(-1, 16); ToughPips(pr, tough, Mathf.Min(420, 34 * (float)Math.Ceiling(tough)), 12, 2, true); }
            // 성격 — 기본 성격 + 판마다 통일 규칙
            W.Section(content, "성격", null, 36);
            var weak = e.Nature != null ? R.WeakTo(e.Nature) : new List<string>();
            var nrow = Ui.Rect("nat", content); nrow.Pref(-1, 40);
            var nsp = e.Nature != null ? Icon("성격_" + e.Nature) : null;
            if (nsp != null) { var nic = Ui.Img(nrow, nsp, Color.white, "ic"); nic.rectTransform.At(0, 0.5f, 4, 0, 34, 34); nic.preserveAspect = true; }
            var nt = Ui.Title(nrow, $"<color={Hx(Theme.NatureCardOf(e.Nature))}>{e.Nature ?? "없음"}</color>" + (weak.Count > 0 ? $"   <size=78%><color={Theme.SubTag}>약점</color> <color={Hx(Theme.NatureCardOf(weak[0]))}>{string.Join(" · ", weak)}</color></size>" : ""), Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            nt.rectTransform.Fill(48, 0, 0, 0);
            if (e.Clone != null)
            {
                var owner = Roster.All.FirstOrDefault(x => x.CoreId == e.Clone || x.key == e.Clone);
                Line($"<color={Theme.SubTag}>사도 클론({owner?.ko ?? e.Clone}) — 원래 성격 그대로. 판의 적 속성이 다르면 이 자리에는 그 성격의 사도 클론이 선다.</color>", Theme.FsSm, Theme.Sub);
                if (s.Village != null)
                {
                    var alts = new List<string>();
                    foreach (var n in Bolzena.Core.Run.NaturesFor(P.Data, s.Village))
                    {
                        var c = Bolzena.Core.Run.CloneCandidates(P.Data, s.Village, n).Select(k => Roster.All.FirstOrDefault(x => x.CoreId == k)?.ko ?? k).Distinct().Take(4).ToList();
                        if (c.Count > 0) alts.Add($"<color={Hx(Theme.NatureCardOf(n))}>{n}</color> 판이면 {string.Join(" · ", c)} 가운데 클론");
                    }
                    if (alts.Count > 0) Line(string.Join("\n", alts), Theme.FsSm, Theme.Ink);
                }
            }
            else Line($"<color={Theme.SubTag}>기본 성격 — 판에서는 모든 적(보스 포함)의 성격이 그 판의 적 속성 하나로 맞춰진다.</color>", Theme.FsSm, Theme.Sub);
            // 성격별 모습 — 같은 몬스터의 다섯 성격 그림(원작 icon_<몬스터><성격>, 있는 것만). 판 속성이 바뀌면 그 성격 모습으로 나온다
            if (e.Clone == null)
            {
                var looks = NatureLooks(e);
                if (looks.Count > 1)
                {
                    W.Section(content, "성격별 모습", "판 속성에 따라", 36);
                    var lr = Ui.Rect("looks", content); lr.Pref(-1, 118);
                    Ui.Row(lr, 10, TextAnchor.MiddleLeft, new RectOffset(4, 0, 0, 0), false, false);
                    foreach (var (n, sp) in looks)
                    {
                        var cell = Ui.Img(lr, Theme.Round, Color.Lerp(Theme.NavyWell, Theme.NatureCardOf(n), 0.25f), "look " + n); cell.Pref(96, 112);
                        var im = Ui.Img(cell.transform, sp, Color.white, "art"); im.preserveAspect = true; im.rectTransform.At(0.5f, 1, 0, -4, 84, 80);
                        var nsp2 = Icon("성격_" + n);
                        if (nsp2 != null) { var ni2 = Ui.Img(cell.transform, nsp2, Color.white, "nat"); ni2.rectTransform.At(0, 0, 6, 6, 22, 22); ni2.preserveAspect = true; }
                        var tl = Ui.Title(cell.transform, n, Theme.FsCap, Theme.NatureCardOf(n), TextAlignmentOptions.Midline); tl.rectTransform.At(0.5f, 0, 10, 6, 70, 22);
                        if (n == e.Nature) { var fr = Ui.Img(cell.transform, Theme.Frame, Theme.Gold, "on"); fr.rectTransform.Fill(); }
                    }
                }
            }
            // 행동 — 한 수에 한 줄: 의도 아이콘 + core 글(수치 금빛) + 한 줄 풀이(W.IntentRow)
            var acts = new List<(Intent it, string head)>();
            if (e.Open != null) acts.Add((e.Open, "첫 턴"));
            foreach (var it in e.Intents) acts.Add((it, null));
            if (e.Phase != null) foreach (var it in e.Phase.Intents) acts.Add((it, $"체력 {e.Phase.At * 100:0}% 아래"));
            if (e.Phase2 != null) foreach (var it in e.Phase2.Intents) acts.Add((it, $"체력 {e.Phase2.At * 100:0}% 아래"));
            if (acts.Count > 0)
            {
                W.Section(content, "행동", e.Pick == "shuffle" ? "무작위" : "차례대로", 36);
                foreach (var (it, head) in acts) W.IntentRow(content, this, it, head);
            }
            // 패시브 · 쌓이는 수치 · 희귀종 · 영혼 공유 · 소환 연결
            var more = P.Text.Enemy(e);
            if (!string.IsNullOrEmpty(more)) { W.Section(content, "특성", "패시브 · 쌓이는 수치 · 희귀종", 36); Line(more, Theme.FsBody, Theme.Ink); }
            // 소환물
            var sums = AllIntents(e).SelectMany(Chain).Where(x => x.T == "summon" && x.Id != null).GroupBy(x => x.Id).Select(g => g.First()).ToList();
            if (sums.Count > 0)
            {
                W.Section(content, "세우는 적", "소환물", 36);
                Line(string.Join("\n", sums.Select(x => $"· {P.Data.Enemy(x.Id)?.Name ?? x.Id}" + (x.NoTough ? $"  <color={Theme.SubTag}>강인도 없음</color>" : ""))), Theme.FsBody, Theme.Ink);
            }
            if (s.Summoner != null) Line($"<color={Theme.SubTag}>소환물 — {s.Summoner} 이(가) 세운다{(s.NoTough ? " · 강인도 없음(격파되지 않는다)" : "")}</color>", Theme.FsSm, Theme.Sub);
            if (!string.IsNullOrEmpty(e.Blurb)) { W.Section(content, "이야기", null, 36); Line($"<color={Theme.SubTag}>{e.Blurb}</color>", Theme.FsSm, Theme.Sub); }
            Stage.Hot["zoom.close"] = Stage.Hot["modal.x"];
        }
    }
}
