using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 파티 편성 · 사도 도감 — 웹판 js/party-screen.js. 위에 세 줄(후열 · 중열 · 전열) 칸, 아래에 135명 그리드(필터),
    // 오른쪽에 이번 층(마을 · 보스 · 나오는 적)과 고른 사도의 정보. 도감은 같은 그리드에 교주 카드 · 장비 탭이 붙는다.
    // 코어 데이터에 있는 사도만 데려갈 수 있다(나머지는 「준비 중」 — 도감으로만).
    public partial class Flow
    {
        class PartyState
        {
            public string Village;
            public string[] Slots = new string[3];       // 0 후열 · 1 중열 · 2 전열 — 사도 key(표)
            public string Nature, Role;
            public bool PlayableOnly;
            public string Focus;
            public string Tab = "사도";
            public bool Dex;
        }

        static readonly string[] RowKey = { "back", "mid", "front" };
        static readonly string[] RowKo = { "후열", "중열", "전열" };

        public void Party(string village)
        {
            var st = new PartyState { Village = village, PlayableOnly = false };
            var v = P.Village(village);
            Stage.SetBg(v.Floors[0].Bg != null && v.Floors[0].Bg.TryGetValue("fight", out var bg) ? bg : "stage3_2", 0.5f);
            Stage.Show("party", root => BuildPartyLight(root, st));   // 밝은 판(Flow.Roster.cs)
        }

        // 도감은 Flow.Dex.cs(사도 · 교주 카드 · 장비를 한 화면 틀에서). 아래 BuildParty 의 도감 갈래는 옛 화면 — 이제 쓰지 않는다.

        void BuildParty(RectTransform root, PartyState st, Action back = null)
        {
            Ui.Clear(root);
            int count = st.Slots.Count(s => s != null);
            // ── 머리 ──
            var head = Ui.Rect("head", root).At(0, 1, Theme.Gutter, -18, 1000, 64);
            Ui.Row(head, 14, TextAnchor.MiddleLeft, null, false, false);
            var bk = Btn.Icon(head, Theme.S("ic_back"), () => { if (st.Dex) (back ?? Lobby)(); else VillageReveal(st.Village, () => Party(st.Village)); }, 54);
            Stage.Hot["back"] = bk;
            var t = Ui.Title(head, st.Dex ? "사도 도감" : "팀 편성", Theme.Fs2xl - 4, Theme.Ink);
            t.Outline(0.2f); t.textWrappingMode = TextWrappingModes.NoWrap; t.Pref(t.preferredWidth + 12, 60);
            if (!st.Dex)
            {
                var (cb, ct) = Ui.Chip(head, null, $"{count} / 3", 42, count == 3 ? Theme.Gold : Theme.NavyCell, null, Theme.FsMd);
                ct.color = count == 3 ? Theme.Brown : Theme.Gold;
                var hint = Ui.Text(head, count == 3 ? "준비됐습니다 — 오른쪽 아래에서 떠납니다" : "아래 목록에서 사도를 눌러 넣습니다 · 칸을 누르면 뺍니다", Theme.FsSm, Theme.Sub);
                hint.textWrappingMode = TextWrappingModes.NoWrap; hint.Pref(560, 40); hint.Outline(0.2f);
            }
            else
            {
                foreach (var tab in new[] { "사도", "교주 카드", "장비" })
                {
                    var tb = Btn.Make(head, tab, st.Tab == tab ? BtnStyle.PillGold : BtnStyle.PillDark, () => { st.Tab = tab; BuildParty(root, st, back); }, Theme.FsMd);
                    tb.Pref(150, 48);
                    Stage.Hot["tab:" + tab] = tb;
                }
            }

            float side = Theme.C(460, 420);
            float stripH = Theme.C(190, 150), filterH = Theme.C(44, 40);
            // ── 왼쪽 ──
            var left = Ui.Rect("left", root);
            left.anchorMin = new Vector2(0, 0); left.anchorMax = new Vector2(1, 1);
            left.offsetMin = new Vector2(Theme.Gutter, Theme.Gutter); left.offsetMax = new Vector2(-(side + Theme.Gutter + 16), -94);

            float gridTop = 0;
            if (!st.Dex)
            {
                var slots = Ui.Rect("slots", left);
                slots.anchorMin = new Vector2(0, 0); slots.anchorMax = new Vector2(1, 1);
                slots.offsetMin = new Vector2(0, stripH + filterH + 22); slots.offsetMax = Vector2.zero;
                Ui.Row(slots, Theme.Gap + 4, TextAnchor.MiddleCenter, null, true, true);
                for (int i = 0; i < 3; i++) Slot(slots, st, i, root, back);

                // 아래 — 필터 + 가로 목록(카제나 편성처럼 칸 아래에서 고른다)
                var fr = Ui.Rect("filters", left).Band(0, filterH, 0, 0, stripH + 10);
                Ui.Row(fr, 8, TextAnchor.MiddleLeft, null, false, true);
                Filter(fr, "전체", st.Nature == null && st.Role == null, () => { st.Nature = null; st.Role = null; BuildParty(root, st, back); });
                foreach (var n in new[] { "순수", "광기", "냉정", "우울", "활발" })
                    Filter(fr, n, st.Nature == n, () => { st.Nature = st.Nature == n ? null : n; BuildParty(root, st, back); }, Theme.NatureCardOf(n), Theme.Icon("성격_" + n));
                foreach (var r in new[] { "탱커", "딜러", "서포터" })
                    Filter(fr, r, st.Role == r, () => { st.Role = st.Role == r ? null : r; BuildParty(root, st, back); }, null, Theme.Icon("역할_" + r));
                Stage.Hot["filter.playable"] = Filter(fr, "고를 수 있는 사도", st.PlayableOnly, () => { st.PlayableOnly = !st.PlayableOnly; BuildParty(root, st, back); }, Theme.Good);
                var strip = Ui.Rect("strip", left).Band(0, stripH, 0, 0, 0);
                var sbg = Ui.Img(strip, Theme.Panel, Color.white, "bg"); sbg.rectTransform.Fill();
                var scont = Ui.Scroll(strip, out var ssr, true);
                int sp = (int)((stripH - Theme.C(150, 124)) / 2);
                Ui.Row(scont, 10, TextAnchor.MiddleLeft, new RectOffset(14, 14, sp, sp), false, false);
                var shown = Roster.All.Where(h => (st.Nature == null || h.nature == st.Nature) && (st.Role == null || h.role == st.Role) && (!st.PlayableOnly || h.Playable))
                    .OrderByDescending(h => h.Playable).ThenBy(h => h.ko, StringComparer.Ordinal).ToList();
                int gi = 0;
                foreach (var h in shown) GridHero(scont, st, h, root, back, gi++);
            }

            if (st.Dex && st.Tab == "사도")
            {
                // 필터
                var fr = Ui.Rect("filters", left).Band(1, filterH, 0, 0, -gridTop);
                Ui.Row(fr, 8, TextAnchor.MiddleLeft, null, false, true);
                Filter(fr, "전체", st.Nature == null && st.Role == null, () => { st.Nature = null; st.Role = null; BuildParty(root, st, back); });
                foreach (var n in new[] { "순수", "광기", "냉정", "우울", "활발" })
                    Filter(fr, n, st.Nature == n, () => { st.Nature = st.Nature == n ? null : n; BuildParty(root, st, back); }, Theme.NatureCardOf(n), Theme.Icon("성격_" + n));
                foreach (var r in new[] { "탱커", "딜러", "서포터" })
                    Filter(fr, r, st.Role == r, () => { st.Role = st.Role == r ? null : r; BuildParty(root, st, back); }, null, Theme.Icon("역할_" + r));
                var po = Filter(fr, "고를 수 있는 사도", st.PlayableOnly, () => { st.PlayableOnly = !st.PlayableOnly; BuildParty(root, st, back); }, Theme.Good);
                Stage.Hot["filter.playable"] = po;

                var area = Ui.Rect("grid", left);
                area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
                area.offsetMin = Vector2.zero; area.offsetMax = new Vector2(0, -(gridTop + filterH + 12));
                var areaBg = Ui.Img(area, Theme.Panel, new Color(1, 1, 1, 0.75f), "bg");
                areaBg.rectTransform.Fill();
                var content = Ui.Scroll(area, out var sr);
                var grid = content.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(Theme.C(118, 96), Theme.C(150, 124));
                grid.spacing = new Vector2(10, 10);
                grid.padding = new RectOffset(16, 16, 16, 16);
                grid.childAlignment = TextAnchor.UpperCenter;
                var list = Roster.All.Where(h => (st.Nature == null || h.nature == st.Nature) && (st.Role == null || h.role == st.Role) && (!st.PlayableOnly || h.Playable))
                    .OrderByDescending(h => h.Playable).ThenBy(h => h.ko, StringComparer.Ordinal).ToList();
                int i = 0;
                foreach (var h in list) GridHero(content, st, h, root, back, i++);
                if (list.Count == 0) Ui.Text(content, "맞는 사도가 없습니다", 22, Theme.Sub, TextAlignmentOptions.Center);
            }
            else if (st.Tab == "교주 카드")
            {
                var area = Ui.Rect("cards", left).Fill(0, 0, 0, 0);
                var areaBg = Ui.Img(area, Theme.Panel, new Color(1, 1, 1, 0.75f), "bg");
                areaBg.rectTransform.Fill();
                var content = Ui.Scroll(area, out _);
                var grid = content.gameObject.AddComponent<GridLayoutGroup>();
                float cw = Theme.C(200, 170);
                grid.cellSize = new Vector2(cw, cw * 1.4f); grid.spacing = new Vector2(14, 14); grid.padding = new RectOffset(18, 18, 18, 18); grid.childAlignment = TextAnchor.UpperCenter;
                int i = 0;
                foreach (var id in P.Data.NeutralIds()) { var c = W.Card(content, this, id, cw); Tw.Pop(c, 0.02f * i++, 0.85f, 0.3f); }
                foreach (var hero in P.Data.Heroes.Values)
                    foreach (var id in P.Data.UniquesOf(hero.Id)) { var c = W.Card(content, this, id, cw); Tw.Pop(c, 0.02f * i++, 0.85f, 0.3f); }
            }
            else if (st.Dex)
            {
                var area = Ui.Rect("equips", left).Fill(0, 0, 0, 0);
                var areaBg = Ui.Img(area, Theme.Panel, new Color(1, 1, 1, 0.75f), "bg");
                areaBg.rectTransform.Fill();
                var content = Ui.Scroll(area, out _);
                var grid = content.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(330, 200); grid.spacing = new Vector2(14, 14); grid.padding = new RectOffset(18, 18, 18, 18); grid.childAlignment = TextAnchor.UpperCenter;
                int i = 0;
                foreach (var e in P.Data.Equips.Values.OrderBy(e => GradeRank(e.Grade)).ThenBy(e => e.Slot))
                { var c = W.Equip(content, this, e.Id, 330, 200); Tw.Pop(c, 0.03f * i++, 0.85f, 0.3f); }
            }

            // ── 오른쪽 ──
            var right = Ui.Rect("side", root).Column(1, side, st.Dex ? Theme.Gutter : Theme.C(120, 100), 18, -Theme.Gutter);
            Ui.Col(right, Theme.Gap, TextAnchor.UpperLeft, null, true, false);
            if (!st.Dex) FloorInfo(right, st.Village);
            HeroInfoPanel(right, st);
            Tw.Rise(right, 0.1f, 30, 0.45f, Vector2.right);

            if (!st.Dex)
            {
                var go = Btn.Make(root, count < 3 ? $"사도 {3 - count}명 더" : "이 파티로 출발", BtnStyle.PillGold, () =>
                {
                    var party = st.Slots.Select(k => Roster.ByKey(k).CoreId).ToList();
                    var rows = Enumerable.Range(0, 3).ToDictionary(i => party[i], i => RowKey[i]);
                    RunPort.ClearSave();
                    P.NewRun(party, st.Village, DateTime.Now.Ticks & 0x7fffffff);
                    foreach (var kv in rows) P.S.Rows[kv.Key] = kv.Value;
                    MapStep();
                }, Theme.FsXl - 2, "go");
                go.GetComponent<RectTransform>().At(1, 0, -Theme.Gutter, Theme.Gutter, side, Theme.C(78, 64));
                go.Interactable = count == 3;
                go.Why = "사도 셋을 고르세요";
                Stage.Hot["party.go"] = go;
                if (count == 3) Tw.Breathe(go.transform, 0.02f, 1.4f);
            }
        }

        static int GradeRank(string g) => g == "일반" ? 0 : g == "고급" ? 1 : g == "희귀" ? 2 : 3;

        Btn Filter(Transform parent, string label, bool on, Action go, Color? tone = null, Sprite icon = null)
        {
            var b = Btn.Make(parent, null, on ? BtnStyle.Gold : BtnStyle.Dark, go, 0, "filter " + label);
            var row = Ui.Row(b.GetComponent<RectTransform>(), 6, TextAnchor.MiddleCenter, new RectOffset(14, 14, 4, 4), false, false);
            if (icon != null) { var ic = Ui.Img(b.transform, icon, Color.white, "icon"); ic.preserveAspect = true; ic.Pref(24, 24); }
            else if (tone.HasValue) { var d = Ui.Img(b.transform, Theme.S("circle"), tone.Value, "dot"); d.Pref(12, 12); }
            var t = Ui.Title(b.transform, label, Theme.FsBody, on ? Theme.Brown : Theme.Ink);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            b.Pref(-1, Theme.C(44, 40));
            return b;
        }

        void Slot(RectTransform parent, PartyState st, int i, RectTransform root, Action back)
        {
            var key = st.Slots[i];
            var h = key != null ? Roster.ByKey(key) : null;
            var b = Btn.Make(parent, null, h != null ? BtnStyle.Glass : BtnStyle.Glass, () =>
            {
                if (h != null) { st.Slots[i] = null; st.Focus = key; }
                BuildParty(root, st, back);
            }, 0, "slot" + i);
            var rt = b.GetComponent<RectTransform>();
            if (h == null) b.Bg.sprite = Theme.GlassDim;
            var lab = Ui.Title(rt, RowKo[i], Theme.FsMd, Theme.Gold, TextAlignmentOptions.Center);
            lab.rectTransform.Band(1, 34, 0, 0, -10);
            if (h == null)
            {
                var plus = Ui.Img(rt, Theme.S("circle"), Theme.Gold, "plus");
                plus.rectTransform.At(0.5f, 0.5f, 0, 10, 64, 64);
                var pi = Ui.Img(plus.rectTransform, Theme.S("ic_plus"), Theme.Brown, "icon");
                pi.rectTransform.Fill(15, 15, 15, 15);
                Tw.Breathe(plus.transform, 0.05f, 1.5f, i * 0.3f);
                var tt = Ui.Title(rt, "사도 넣기", Theme.FsLg, Theme.Ink, TextAlignmentOptions.Center);
                tt.rectTransform.At(0.5f, 0.5f, 0, -50, 300, 32);
                var tt2 = Ui.Text(rt, "아래 목록에서 고르세요", Theme.FsCap + 1, Theme.Sub, TextAlignmentOptions.Center);
                tt2.rectTransform.At(0.5f, 0.5f, 0, -80, 300, 24);
                return;
            }
            float base_ = Theme.C(104, 80), mh = Theme.C(230, 170);
            var glow = Ui.Img(rt, Theme.S("soft"), Theme.NatureOf(h.nature).A(0.4f), "glow");
            glow.rectTransform.At(0.5f, 0, 0, base_ - 30, 320, mh + 70);
            var floor = Ui.Img(rt, Theme.S("soft"), new Color(0, 0, 0, 0.5f), "floor");
            floor.rectTransform.At(0.5f, 0, 0, base_ - 18, 220, 50);
            // 편성 칸 — SD 전투 스파인(칸 고정 몸 키 mh × 0.8 · 사도마다 경계로 맞추지 않음). 미니미는 없앴다(2026-10-06)
            var spot = Ui.Rect("hero", rt).At(0.5f, 0, 0, base_, 10, 10);
            SceneHero.Make(spot, h, mh * 0.8f, true, 0, false);
            Tw.Pop(spot, 0, 0.6f, 0.4f);
            var shade = Ui.Img(rt, Theme.S("fade_card"), Color.white, "shade"); shade.rectTransform.Fill(3, 3, 3, 3);
            shade.transform.SetSiblingIndex(1);
            var nm = Ui.Title(rt, h.ko, Theme.FsXl - 4, Theme.Ink, TextAlignmentOptions.Center);
            nm.rectTransform.Band(0, 34, 6, 6, Theme.C(42, 34));
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 16; nm.fontSizeMax = Theme.FsXl - 4;
            nm.Outline(0.2f);
            var sub = Ui.Text(rt, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(h.nature))}>{h.nature}</color> · {h.role} · {h.race}", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
            sub.rectTransform.Band(0, 24, 6, 6, Theme.C(16, 10));
            var x = Ui.Img(rt, Theme.S("ic_x"), Theme.Sub, "x");
            x.rectTransform.At(1, 1, -14, -14, 22, 22);
            var ri = Theme.Icon("역할_" + h.role);
            if (ri != null) { var r = Ui.Img(rt, ri, Color.white, "role"); r.rectTransform.At(0, 1, 12, -12, 30, 30); r.preserveAspect = true; }
            var own = P.Data.Hero(h.CoreId)?.Row ?? h.row;
            if (own != RowKey[i])
            {
                var warn = Ui.Text(rt, $"본래 {(own == "front" ? "전열" : own == "mid" ? "중열" : "후열")}", Theme.FsCap, Theme.Gold, TextAlignmentOptions.TopRight);
                warn.rectTransform.At(1, 1, -44, -14, 120, 20);
            }
        }

        void GridHero(RectTransform content, PartyState st, HeroInfo h, RectTransform root, Action back, int idx)
        {
            bool picked = st.Slots.Contains(h.key);
            var b = Btn.Make(content, null, picked ? BtnStyle.Cell : BtnStyle.Cell, () =>
            {
                st.Focus = h.key;
                if (!st.Dex && h.Playable)
                {
                    int at = Array.IndexOf(st.Slots, h.key);
                    if (at >= 0) st.Slots[at] = null;
                    else
                    {
                        // 본래 줄의 빈 칸 → 아무 빈 칸
                        int want = Array.IndexOf(RowKey, P.Data.Hero(h.CoreId)?.Row ?? h.row);
                        int slot = want >= 0 && st.Slots[want] == null ? want : Array.IndexOf(st.Slots, null);
                        if (slot >= 0) st.Slots[slot] = h.key;
                        else Toast.Show("세 칸이 다 찼습니다 — 위 칸을 눌러 빼세요");
                    }
                }
                else if (!st.Dex && !h.Playable) Toast.Show($"{h.ko} — 아직 판에 데려갈 수 없습니다(코어 데이터 준비 중)");
                BuildParty(root, st, back);
            }, 0, "hero " + h.key);
            float k = Theme.C(1f, 96f / 118f);
            if (picked) b.Bg.sprite = Theme.CellOn;
            b.Pref(118 * k, 150 * k);
            var rt = b.GetComponent<RectTransform>();
            var pic = Ui.Img(rt, Theme.Round, Theme.NavyWell, "pic");
            pic.rectTransform.At(0.5f, 1, 0, -8 * k, 102 * k, 102 * k);
            pic.gameObject.AddComponent<RectMask2D>();
            var face = CardArt.Upper(h.art, 1f, 0.34f);   // 기본 스탠딩의 머리 · 어깨(없으면 초상)
            if (face != null || h.Icon != null)
            {
                var im = Ui.Img(pic.rectTransform, face ?? h.Icon, Color.white, "icon");
                if (face != null) im.rectTransform.Fill();
                else { im.rectTransform.Fill(-4, -10, -4, 0); im.preserveAspect = true; }
                if (!h.Playable && !st.Dex) im.color = new Color(0.55f, 0.55f, 0.6f);
            }
            var tone = Ui.Img(rt, Theme.Round, Theme.NatureOf(h.nature), "tone");
            tone.rectTransform.At(0.5f, 1, 0, -106 * k, 102 * k, 3);
            var nm = Ui.Title(rt, h.ko, Theme.FsBody, h.Playable || st.Dex ? Theme.Ink : Theme.Dim, TextAlignmentOptions.Center);
            nm.rectTransform.Band(0, 32 * k, 4, 4, 4 * k);
            nm.textWrappingMode = TextWrappingModes.NoWrap;
            nm.enableAutoSizing = true; nm.fontSizeMin = 12; nm.fontSizeMax = Theme.FsBody;
            var ri = Theme.Icon("역할_" + h.role);
            if (ri != null) { var r = Ui.Img(rt, ri, Color.white, "role"); r.rectTransform.At(0, 1, 6, -6, 24 * k, 24 * k); r.preserveAspect = true; }
            if (h.Playable && !st.Dex)
            {
                var ok = Ui.Img(rt, Theme.Pill, picked ? Theme.Gold : new Color(0.25f, 0.6f, 0.4f, 0.95f), "ok");
                ok.rectTransform.At(1, 1, -6, -6, picked ? 30 : 44, 22);
                var okt = Ui.Title(ok.rectTransform, picked ? "✓" : "출전", Theme.FsCap - 1, picked ? Theme.Brown : Color.white, TextAlignmentOptions.Center);
                okt.rectTransform.Fill();
                if (picked) { okt.text = ""; var ck = Ui.Img(ok.rectTransform, Theme.S("ic_check"), Theme.Brown, "ck"); ck.rectTransform.Fill(6, 2, 6, 2); ck.preserveAspect = true; }
            }
            else if (!h.Playable && !st.Dex)
            {
                var lk = Ui.Img(rt, Theme.S("ic_lock"), new Color(1, 1, 1, 0.6f), "lock");
                lk.rectTransform.At(1, 1, -8, -8, 20, 20);
            }
            if (st.Focus == h.key && !picked) { var f = Ui.Img(rt, Theme.Frame, Theme.Sky.A(0.9f), "focus"); f.rectTransform.Fill(); }
            if (h.Playable) Stage.Hot["hero:" + h.key] = b;
            if (idx < 40) Tw.Pop(rt, 0.012f * idx, 0.85f, 0.3f);
        }

        void FloorInfo(RectTransform parent, string villageId)
        {
            var v = P.Village(villageId);
            var f = v.Floors[0];
            var box = Ui.Panel(parent, Theme.Panel, null, "floor");
            Ui.Col(box.rectTransform, 4, TextAnchor.UpperLeft, new RectOffset(22, 22, 14, 16));
            var chip = Ui.Text(box.transform, $"{v.Name} · 첫 층", Theme.FsSm, Theme.Gold);
            chip.Pref(-1, 22);
            var t = Ui.Title(box.transform, $"1층 · {f.Name}", Theme.FsXl, Theme.Ink);
            t.Pref(-1, 40);
            var s = Ui.Text(box.transform, f.Sub ?? "", Theme.FsSm, Theme.Sub);
            s.Pref(-1, 24);
            var boss = string.Join(" · ", f.Boss.Select(id => P.Data.Enemy(id)?.Name ?? id).Distinct());
            var bt = Ui.Text(box.transform, $"<color=#FF8C7A>보스</color>  {boss}", Theme.FsBody, Theme.Ink);
            bt.Pref(-1, 28);
            var foes = f.Pools.SelectMany(p => p).SelectMany(x => x).Concat(f.Elites.SelectMany(x => x)).Distinct()
                .Where(id => !f.Boss.Contains(id)).Select(id => P.Data.Enemy(id)?.Name ?? id).Distinct();
            var ft = Ui.Text(box.transform, $"<color={Theme.SubTag}>나오는 적</color>  " + string.Join(" · ", foes), Theme.FsSm, Theme.Ink);
            ft.Pref(-1, Theme.C(50, 44));
        }

        void HeroInfoPanel(RectTransform parent, PartyState st)
        {
            var h = st.Focus != null ? Roster.ByKey(st.Focus) : null;
            var box = Ui.Panel(parent, Theme.Panel, null, "info");
            box.Pref(-1, -1, -1, 1);
            Ui.Col(box.rectTransform, 8, TextAnchor.UpperLeft, new RectOffset(22, 22, 16, 16));
            if (st.Tab != "사도")
            {
                Ui.Title(box.transform, st.Tab == "장비" ? "장비 도감" : "교주 카드", 30, Theme.Ink).Pref(-1, 40);
                Ui.Text(box.transform, st.Tab == "장비"
                    ? $"{P.Data.Equips.Count}개 — 사도 하나에 무기 · 방어구 · 장신구 한 칸씩. 애착 장비는 그 사도가 끼면 더 셉니다. 얻으면 곧장 끼거나 팝니다."
                    : "교주님이 직접 쓰는 카드와 사도 고유 카드. 교주 카드는 상점에서, 고유 카드는 싸우며 은총으로 얻습니다.", 18, Theme.Sub).Pref(-1, 120);
                return;
            }
            if (h == null)
            {
                Ui.Title(box.transform, st.Dex ? "사도를 고르면 능력이 여기 뜹니다" : "파티 성격 · 시작 덱", 24, Theme.Ink).Pref(-1, 34);
                if (!st.Dex)
                {
                    var picked = st.Slots.Where(k => k != null).Select(Roster.ByKey).ToList();
                    var nat = picked.Count == 0 ? "사도를 고르면 셋의 성격이 여기 뜹니다." : string.Join(" · ", picked.GroupBy(x => x.nature).Select(g => $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(g.Key))}>{g.Key}</color> {g.Count()}"));
                    Ui.Text(box.transform, nat, 19, Theme.Ink).Pref(-1, 30);
                    var deck = picked.Where(x => x.Playable).SelectMany(x => P.Data.Hero(x.CoreId).Starter).Select(id => P.Data.Card(id)?.Name ?? id);
                    Ui.Text(box.transform, picked.Count == 0 ? "사도마다 시작 카드 넉 장. 고유 카드는 싸우며 은총으로 얻습니다." : "시작 덱  " + string.Join(" · ", deck), 17, Theme.Sub).Pref(-1, 80);
                }
                return;
            }
            var top = Ui.Rect("top", box.transform);
            float fz = Theme.C(88, 72);
            top.Pref(-1, fz + 6);
            var face = W.Face(top, h, fz);
            face.At(0, 0.5f, 0, 0, fz, fz);
            var nm = Ui.Title(top, h.ko, Theme.FsXl, Theme.Ink);
            nm.rectTransform.At(0, 1, fz + 18, -4, 320, 42);
            nm.enableAutoSizing = true; nm.fontSizeMin = 18; nm.fontSizeMax = Theme.FsXl;
            nm.textWrappingMode = TextWrappingModes.NoWrap;
            string stars = new string('★', Mathf.Clamp(h.star, 1, 3));
            var sub = Ui.Text(top, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(h.nature))}>{h.nature}</color> · {h.race} · {h.role} · {h.RowKo}  <color={Theme.GoldTag}>{stars}</color>", Theme.FsSm, Theme.Sub);
            sub.rectTransform.At(0, 1, fz + 20, -48, 330, 26);
            sub.textWrappingMode = TextWrappingModes.NoWrap;
            var blurb = Ui.Text(box.transform, h.blurb ?? "", Theme.FsSm - 1, Theme.Sub, TextAlignmentOptions.TopLeft);
            blurb.Pref(-1, Theme.C(84, 60));
            blurb.enableAutoSizing = true; blurb.fontSizeMin = 11; blurb.fontSizeMax = Theme.FsSm - 1;
            blurb.overflowMode = TextOverflowModes.Ellipsis;
            if (h.Playable)
            {
                var d = P.Data.Hero(h.CoreId);
                string body = "";
                // 좁은 칸이라 이름만 — 글(수치 전부)은 아래 알약을 누르면 판으로(「자세히」 따로 없음 · core CardText.Traits)
                foreach (var g in P.Text.Traits(d).GroupBy(x => x.Kind))
                    body += $"<color=#F2CF7A>{g.Key}</color>  {string.Join(" · ", g.Select(x => $"「{x.Name}」"))}\n";
                body += $"<color=#F2CF7A>시작 카드</color>  {string.Join(" · ", d.Starter.Select(id => P.Data.Card(id)?.Name ?? id))}\n";
                body += $"<color=#F2CF7A>고유 카드</color>  {string.Join(" · ", P.Data.UniquesOf(d.Id).Select(id => P.Data.Card(id)?.Name ?? id))}";
                var bt = Ui.Text(box.transform, body, Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft);
                bt.Pref(-1, 120, -1, 1);
                bt.enableAutoSizing = true; bt.fontSizeMin = 11; bt.fontSizeMax = Theme.FsSm;
                // 고학년 · 고유 효과 · 패시브 판(수치가 다 든 글 · 길면 판 묶음이 스크롤)
                var mr = Ui.Rect("more", box.transform); mr.Pref(-1, 34);
                Ui.Row(mr, 0, TextAnchor.MiddleLeft, null, false, false);
                var tc = TraitsChip(mr, d.Id, "고학년 · 고유 효과 · 패시브");
                Stage.Hot["info.traits"] = tc;
            }
            else
            {
                var bt = Ui.Text(box.transform, $"<color=#F2CF7A>키워드</color>  {h.keyword}\n<color=#F2CF7A>고학년</color>  {h.ult}\n\n<color=#A7B1CC>코어 데이터에 아직 없는 사도입니다 — 도감으로만 봅니다.</color>", 17, Theme.Ink);
                bt.Pref(-1, 160);
            }
        }
    }
}
