using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 사도 저장(프론티어) — 완주 판 끝 화면의 「저장」(새로 저장 · 기존 칸 덮어쓰기) · 사도 저장함(로비 · 도감 · 프론티어에서 — 사도 거르기 · 칸 목록 · 고른 칸의 카드 · 장비 · 성장이 바로).
    //   사도마다 10칸까지(2026-10-19 — SlotStore.PerHero). 빈 칸을 그려 두지 않는다: 있는 칸만 · 끝에 「새로 저장 n/10」 하나(SlotNewRow) — 판 끝 저장 창 · 저장함 · 저장 데이터 창이 같은 모양.
    //   10칸이 차면 「새로 저장」 대신 「덮어쓸 칸을 고르거나 지우세요」 — 칸마다 지우기 단추. 서버도 11번째를 409 hero slots full 로 거절.
    //   새로 저장 = POST /api/slots?hero=(&name=) → 서버가 id 를 준다 · 덮어쓰기 PUT /api/slot/:id · 이름 PATCH · 지우기 DELETE(서버 문서 §사도 슬롯).
    //   슬롯 글은 엔진 HeroSlot.Of 로 판 끝에 뜬 것 그대로(SlotStore.HoldClear). 서버는 모양만, 규칙 검사는 검증기(pending → ok · bad).
    //   로그인 전 · 서버 없음이면 기다림에 둔 채로 안내하고, 로그인한 뒤 저장함의 「기다리는 사도」 에서 마저 저장한다. 기기 사본은 SlotStore.
    public partial class Flow
    {
        /// <summary>슬롯 한 줄 요약 — 카드 수 · 신탁 · 축복 · 복제 · 장비 · 성장.</summary>
        public static string SlotLine(HeroSlot s)
        {
            if (s == null) return "읽을 수 없는 저장 칸";
            int o = s.Cards.Count(c => c.O > 0), b = s.Cards.Count(c => c.B != null), cp = s.Cards.Count(c => GameData.IsCopy(c.Id));
            var a = new List<string> { $"카드 {s.Cards.Count}장" };
            if (o > 0) a.Add($"신탁 {o}");
            if (b > 0) a.Add($"축복 {b}");
            if (cp > 0) a.Add($"복제 {cp}");
            a.Add($"아티팩트 {s.Gear.Count}");
            var g = s.Growth;
            if (g != null && (g.Atk != 0 || g.Def != 0 || g.Crit != 0)) a.Add($"성장 공{g.Atk:+0;-0} 방{g.Def:+0;-0} 치{g.Crit:+0;-0}");
            return string.Join(" · ", a);
        }

        // ── 서버 ──
        /// <summary>서버의 내 슬롯 목록으로 기기 사본을 맞춘다(로그인했을 때만). done(서버에서 받았나).</summary>
        public void SyncSlots(Action<bool> done)
        {
            if (!Online.Ready) { done?.Invoke(false); return; }
            Online.Call("GET", "/api/slots", null, r =>
            {
                if (r.Ok && r.Json?["slots"] is JArray rows)
                {
                    if (r.Json["max"]?["perHero"]?.Type == JTokenType.Integer) SlotStore.PerHero = (int)r.Json["max"]["perHero"];
                    SlotStore.SyncFrom(rows); done?.Invoke(true);
                }
                else done?.Invoke(false);
            });
        }

        /// <summary>칸 요청의 실패 처리 — 닉네임 · 로그인 창, 그 밖은 알림. </summary>
        void SlotFail(Online.Resp r, string what)
        {
            if (r.Code == 403 && r.Json?["needsName"] != null) { NamePanel(null); return; }
            if (r.Code == 401) { AccountPanel("로그인하면 저장할 수 있다"); return; }
            string why = r.Code == 409 && r.Error == "hero slots full" ? $"이 사도는 {r.Json?["max"] ?? SlotStore.PerHero}칸이 다 찼다 — 덮어쓸 칸을 고를 수 있다"
                : r.Code == 409 && r.Json?["max"] != null ? $"저장 칸이 계정 한도({r.Json["max"]:N0}칸)에 닿았다 — 안 쓰는 칸을 지울 수 있다"
                : r.Code == 404 ? "서버에 없는 칸이다 — 보관함을 다시 열 수 있다"
                : r.Code == 400 ? (r.Str("why") ?? r.Error ?? "서버가 받지 않았다") : Online.ErrorText(r);
            Toast.Show($"{what}하지 못했다 — {why}", 3.5f);
        }

        /// <summary>새로 저장 — 서버가 id 를 준다. 되면 기기 사본(검증 대기) · 기다림 표시.</summary>
        void SaveNew(string hero, string json, string name, Action<SlotStore.Entry> done)
        {
            name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
            string q = "/api/slots?hero=" + Uri.EscapeDataString(hero) + (name != null ? "&name=" + Uri.EscapeDataString(name) : "");
            Online.Call("POST", q, json, r =>
            {
                if (!r.Ok || r.Json == null) { SlotFail(r, "저장"); done?.Invoke(null); return; }
                var e = SlotStore.FromRow(r.Json, json);
                SlotStore.Put(e);
                SlotStore.MarkSaved(hero, e.Id);
                Debug.Log($"[HeroSave] {hero} 새로 저장 → id {e.Id} · 차례 {e.N} · 이름 {e.Name ?? "-"} · {e.Status}");
                Toast.Show($"「{e.Label}」 저장 · 검증 대기", 2.6f);
                done?.Invoke(e);
            });
        }

        /// <summary>칸 덮어쓰기(같은 id · 이름 그대로) — 상태는 다시 검증 대기.</summary>
        void Overwrite(SlotStore.Entry old, string json, Action<SlotStore.Entry> done)
        {
            Online.Call("PUT", $"/api/slot/{old.Id}", json, r =>
            {
                if (!r.Ok || r.Json == null) { SlotFail(r, "덮어쓰기"); done?.Invoke(null); return; }
                var e = SlotStore.FromRow(r.Json, json);
                SlotStore.Put(e);
                SlotStore.MarkSaved(e.Hero, e.Id);
                Debug.Log($"[HeroSave] {e.Hero} 덮어쓰기 → id {e.Id} · 차례 {e.N} · {e.Status}");
                Toast.Show($"「{e.Label}」 덮어씀 · 검증 대기", 2.6f);
                done?.Invoke(e);
            });
        }

        void DeleteSlot(SlotStore.Entry e, Action done)
        {
            Online.Call("DELETE", $"/api/slot/{e.Id}", null, r =>
            {
                if (r.Ok || r.Code == 404) { SlotStore.Remove(e.Id); Toast.Show($"「{e.Label}」 을 지웠다"); }
                else Toast.Show("지우지 못했다 — " + Online.ErrorText(r), 3f);
                done?.Invoke();
            });
        }

        // ── 칸 카드(저장 고르기 · 저장함 · 저장 데이터 창이 같이) ──
        /// <summary>
        /// 칸 카드 — 이름(없으면 「〇〇 저장 n」) · 날짜 · 상태 칩 · 이 칸 최고 점수(크게) · 장착 중. face 면 왼쪽에 사도 얼굴(여러 사도가 섞인 목록).
        /// 카제나 세이브 데이터 칸처럼 점수가 주인공 — 요약 글은 넣지 않는다(보기 창에서).
        /// </summary>
        Btn SlotCard(Transform parent, SlotStore.Entry e, float h, bool face, bool on, bool equipped, Action click, string name, Action del = null)
        {
            var b = Btn.Make(parent, null, BtnStyle.Cell, click, 0, name);
            b.Pref(-1, h);
            if (on) b.Bg.sprite = Theme.CellOn;
            var rt = b.GetComponent<RectTransform>();
            var hero = Roster.OfCore(e.Hero);
            var nc = hero != null ? Theme.NatureOf(hero.nature) : Theme.Sub;
            var tone = Ui.Img(rt, Theme.Round, nc.A(0.9f), "tone"); tone.rectTransform.At(0, 0.5f, 6, 0, 4, h - 28);
            float x = 18;
            if (face) { float fs = Mathf.Min(64, h - 40); var f = W.Face(rt, hero, fs); f.At(0, 1, 18, -14, fs, fs); x = 18 + fs + 14; }
            var nt = Ui.Title(rt, e.Label, Theme.FsMd, Theme.Ink, TextAlignmentOptions.TopLeft);
            nt.rectTransform.Band(1, 28, x, 112, -12); nt.textWrappingMode = TextWrappingModes.NoWrap; nt.overflowMode = TextOverflowModes.Ellipsis;
            var dt = Ui.Text(rt, SlotStore.Date(e.Updated), Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft);
            dt.rectTransform.Band(1, 22, x, 112, -42);
            var (sb, st) = Ui.Chip(rt, null, SlotStore.StatusKo(e.Status), 28, SlotStore.StatusColor(e.Status).A(0.22f), null, Theme.FsCap);
            st.color = SlotStore.StatusColor(e.Status);
            sb.rectTransform.At(1, 1, -12, -12, 92, 28);
            if (equipped)
            {
                var (eb, et) = Ui.Chip(rt, null, "장착 중", 28, Theme.Gold, null, Theme.FsCap);
                et.color = Theme.Brown; eb.rectTransform.At(0, 0, x, 14, 84, 28);
            }
            var bv = SlotBest(e);
            var sc = Ui.Title(rt, bv != null ? $"{bv.Value:N0}<size=55%><color={Theme.SubTag}> 점</color></size>" : $"<size=60%><color={Theme.DimTag}>기록 없음</color></size>",
                Theme.FsXl, Theme.Gold, TextAlignmentOptions.BottomRight);
            sc.rectTransform.Band(0, 40, x, del != null ? 64 : 16, 8); sc.textWrappingMode = TextWrappingModes.NoWrap;
            if (del != null)
            {
                // 칸 지우기(10칸이 찼을 때 자리를 비우는 길) — 칸을 누르는 것(덮어쓰기 · 고르기)과 따로
                var db = Btn.Make(rt, null, BtnStyle.PillRose, del, 0, "del");
                db.GetComponent<RectTransform>().At(1, 0, -12, 10, 44, 36);
                var di = Ui.Img(db.GetComponent<RectTransform>(), Theme.S("ic_trash"), Color.white, "ic"); di.preserveAspect = true; di.rectTransform.Fill(11, 7, 11, 7);
                Stage.Hot[name + ".del"] = db;
            }
            return b;
        }

        /// <summary>
        /// 칸 목록 끝 한 줄 — 빈 칸을 그려 두지 않고 이것 하나(2026-10-19). 한도 아래면 「＋ 새로 저장 n/10」(onNew 가 없으면 칸 수만 흐리게),
        /// 한도에 닿으면 「10칸이 다 찼습니다 — 덮어쓸 칸을 고르거나 지우세요」. 판 끝 저장 창 · 저장함 · 저장 데이터 창이 같이 쓴다.
        /// </summary>
        Btn SlotNewRow(Transform parent, string hero, float h, Action onNew, string name)
        {
            int n = SlotStore.CountOf(hero), max = SlotStore.PerHero;
            bool full = n >= max, live = !full && onNew != null;
            var b = Btn.Make(parent, null, BtnStyle.Cell, () =>
            {
                if (full) { Toast.Show($"{max}칸이 다 찼다 — 덮어쓰거나 지울 수 있다", 3f); return; }
                if (onNew == null) { Toast.Show("완주한 모험의 끝 화면에서 새로 저장할 수 있다", 2.6f); return; }
                onNew();
            }, 0, name);
            b.Pref(-1, h);
            var rt = b.GetComponent<RectTransform>();
            b.Bg.sprite = Theme.Frame;
            b.SetColor(live ? Theme.Gold.A(0.9f) : Theme.Sub.A(0.35f));
            var fill = Ui.Img(rt, Theme.Round, live ? Theme.Gold.A(0.10f) : Theme.NavyWell.A(0.35f), "fill"); fill.rectTransform.Fill(3, 3, 3, 3);
            var row = Ui.Rect("row", rt).Fill(18, 0, 130, 0);
            Ui.Row(row, 12, TextAnchor.MiddleLeft, null, false, false);
            if (!full)
            {
                var ic = Ui.Img(row, Theme.S("ic_plus"), live ? Theme.Gold : Theme.Dim, "ic"); ic.preserveAspect = true; ic.Pref(26, 26);
                var t = Ui.Title(row, live ? "새로 저장" : "저장 칸", Theme.FsLg, live ? Theme.Gold : Theme.Sub); t.textWrappingMode = TextWrappingModes.NoWrap; t.Pref(t.preferredWidth + 4, h - 8);
            }
            else
            {
                var t = Ui.Text(row, $"{max}칸이 다 찼다 <color={Theme.DimTag}>— 덮어쓰거나 지울 수 있다</color>", Theme.FsSm, Theme.Sub);
                t.Pref(-1, h - 8, 1); t.enableAutoSizing = true; t.fontSizeMin = 11; t.fontSizeMax = Theme.FsSm;
            }
            var c = Ui.Title(rt, $"{n}<color={Theme.DimTag}> / {max}</color>", Theme.FsLg, full ? Theme.Bad : live ? Theme.Gold : Theme.Sub, TextAlignmentOptions.MidlineRight);
            c.rectTransform.At(1, 0.5f, -18, 0, 110, h - 8); c.textWrappingMode = TextWrappingModes.NoWrap;
            Stage.Hot[name] = b;
            return b;
        }

        /// <summary>최신순 ⇄ 점수순 단추(누르면 바꾸고 changed).</summary>
        Btn SortToggle(Transform parent, Action changed, string name, float w = 170, float h = 48)
        {
            var b = Btn.Make(parent, null, BtnStyle.PillDark, () => { SlotStore.Order = SlotStore.Order == SlotStore.Sort.Recent ? SlotStore.Sort.Score : SlotStore.Sort.Recent; changed(); }, 0, name);
            b.Pref(w, h);
            var rt = b.GetComponent<RectTransform>();
            var row = Ui.Rect("row", rt).Fill();
            Ui.Row(row, 10, TextAnchor.MiddleCenter, null, false, false);
            var t = Ui.Title(row, SlotStore.OrderKo(SlotStore.Order), Theme.FsSm, Theme.Ink); t.Pref(t.preferredWidth + 4, h - 8);
            var ic = Ui.Img(row, Theme.S("ic_sort"), Theme.Sub, "ic"); ic.preserveAspect = true; ic.Pref(22, 22);
            Stage.Hot[name] = b;
            return b;
        }

        // ── 판 끝 화면 ──
        /// <summary>완주 끝 화면의 파티 줄 오른쪽 — 「저장」 단추(서버가 없으면 「준비 중」).</summary>
        void EndSave(RectTransform row, string hero)
        {
            var w = SlotStore.Wait;
            if (w == null || !w.Slots.ContainsKey(hero)) return;
            float bw = 92, bh = 40;
            if (!Online.Up)
            {
                var t = Ui.Text(row, "저장 · 준비 중", Theme.FsCap, Theme.Dim, TextAlignmentOptions.MidlineRight);
                t.rectTransform.At(1, 0.5f, 0, 0, 120, 30);
                return;
            }
            bool saved = w.SavedId.ContainsKey(hero);
            Btn b = null;
            b = Btn.Make(row, saved ? "저장됨" : "저장", saved ? BtnStyle.PillDark : BtnStyle.PillGold, () =>
                NeedAccount("로그인하면 이 모험의 사도를 저장할 수 있다", () =>
                    SavePick(hero, w.Slots[hero], e => { if (b) { b.SetLabel("저장됨"); } })), Theme.FsSm, "save " + hero);
            b.GetComponent<RectTransform>().At(1, 0.5f, 0, 0, bw + (saved ? 8 : 0), bh);
            Stage.Hot["end.save:" + hero] = b;
        }

        /// <summary>지금 열린 저장 창의 이름 칸(자동 데모가 글을 넣는다).</summary>
        public TMP_InputField SaveNameField { get; private set; }
        /// <summary>마지막 저장 창의 덮어쓰기 목록 칸 수(데모 단언).</summary>
        public int SavePickRows { get; private set; }

        /// <summary>저장 창 — 왼쪽 「이번 판」 + 이름(선택), 오른쪽 그 사도의 칸 목록(누르면 덮어쓰기 · 칸마다 지우기) + 끝에 「새로 저장 n/10」(10칸이면 「다 찼습니다」).</summary>
        public void SavePick(string hero, string json, Action<SlotStore.Entry> saved)
        {
            SyncSlots(_ => SavePickOpen(hero, json, saved));
        }

        void SavePickOpen(string hero, string json, Action<SlotStore.Entry> saved)
        {
            var h = Roster.OfCore(hero);
            HeroSlot slot = null;
            try { slot = HeroSlot.Parse(json); } catch (Exception) { }
            var size = Stage.Size;
            float w = Mathf.Min(1180, size.x - 48), hh = Mathf.Min(660, size.y - 24);
            var mine = SlotStore.Sorted(SlotStore.Of(hero), SlotStore.Order, SlotBest);
            bool full = !SlotStore.CanAdd(hero);
            var (body, close, _) = Stage.ModalBox("savepick", w, hh, $"{h?.ko ?? hero} 저장", full ? "덮어쓸 칸을 고를 수 있다" : "새로 저장하거나 덮어쓸 수 있다", true, null, 0);
            int next = mine.Count == 0 ? 1 : mine.Max(x => x.N) + 2;
            void Again() { close(); SavePickOpen(hero, json, saved); }

            // 왼쪽 — 이번 판(그림 · 요약) · 새 칸 이름
            float lw = Mathf.Min(420, w * 0.38f);
            var left = Ui.Panel(Ui.Rect("leftcol", body).Column(0, lw, 0, 0, 0), Theme.Panel, null, "new").rectTransform;
            left.Fill();
            var nc = h != null ? Theme.NatureOf(h.nature) : Theme.Sub;
            float picH = Mathf.Max(120, Mathf.Min(260, hh - 340));
            var win = Ui.Img(left, Theme.Round, Color.Lerp(Theme.NavyWell, nc, 0.25f), "pic").rectTransform; win.Band(1, picH, 10, 10, -10);
            win.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var up = h?.art != null ? CardArt.Upper(h.art, (lw - 20) / picH, 0.42f) : null;
            if (up != null) Ui.Img(win, up, Color.white, "art").rectTransform.Fill();
            else { var face = W.Face(win, h, 96); face.At(0.5f, 0.6f, 0, 0, 96, 96); }
            var shade = Ui.Img(win, Theme.S("fade_card"), Color.white, "shade"); shade.rectTransform.Fill();
            var nt = Ui.Title(win, h?.ko ?? hero, Theme.FsXl, Color.white, TextAlignmentOptions.BottomLeft); nt.rectTransform.Band(0, 40, 18, 18, 10); nt.Outline(0.25f);
            var (tb, tt) = Ui.Chip(win, null, "이번 모험", 30, Theme.Gold, null, Theme.FsCap); tt.color = Theme.Brown; tb.rectTransform.At(0, 1, 12, -12, 84, 30);
            var sl = Ui.Text(left, SlotLine(slot), Theme.FsSm, Theme.Ink, TextAlignmentOptions.Center); sl.rectTransform.Band(1, 44, 20, 20, -(picH + 18));
            sl.enableAutoSizing = true; sl.fontSizeMin = 11; sl.fontSizeMax = Theme.FsSm;
            SaveNameField = null;
            if (!full)
            {
                var nl = Ui.Text(left, "새 칸 이름 <color=" + Theme.DimTag + ">(선택 · 16자)</color>", Theme.FsSm, Theme.Ink, TextAlignmentOptions.BottomLeft); nl.rectTransform.Band(0, 26, 24, 24, 92);
                var box = Ui.Rect("field", left).Band(0, 56, 22, 22, 28);
                SaveNameField = NameInput(box, "", $"{h?.ko ?? hero} 저장 {next}", SlotStore.NAME_MAX);
            }
            else
            {
                var ft = Ui.Text(left, $"{SlotStore.PerHero}칸이 다 찼다\n<size=85%><color={Theme.SubTag}>덮어쓰거나 지울 수 있다</color></size>", Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
                ft.rectTransform.Band(0, 100, 20, 20, 24); ft.enableAutoSizing = true; ft.fontSizeMin = 12; ft.fontSizeMax = Theme.FsMd;
            }

            // 오른쪽 — 칸 목록 + 끝에 「새로 저장 n/10」
            var right = Ui.Rect("over", body);
            right.anchorMin = new Vector2(0, 0); right.anchorMax = new Vector2(1, 1);
            right.offsetMin = new Vector2(lw + 20, 0); right.offsetMax = Vector2.zero;
            var rh = Ui.Rect("head", right).Band(1, 48, 0, 0, 0);
            var rt = Ui.Title(rh, $"저장 칸  <size=80%><color={(full ? Theme.BadTag : Theme.SubTag)}>{SlotStore.CountKo(hero)}</color></size>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft); rt.rectTransform.Fill(4, 0, 190, 0);
            if (mine.Count > 1) SortToggle(rh, Again, "savepick.sort").GetComponent<RectTransform>().At(1, 0.5f, 0, 0, 170, 44);
            var area = Ui.Rect("list", right).Fill(0, 0, 0, 58);
            SavePickRows = mine.Count;
            var content = Ui.Scroll(area, out _);
            Ui.Col(content, 10, TextAnchor.UpperLeft, new RectOffset(0, 12, 0, 8), true, false);
            float rowH = Theme.C(96, 88);
            for (int i = 0; i < mine.Count; i++)
            {
                var e = mine[i];
                Action del = Online.Ready && e.OnServer
                    ? () => Confirm($"「{e.Label}」 을 지울까?", "저장한 사도가 사라진다 — 낸 점수는 그대로", "지우기", () => DeleteSlot(e, Again), true)
                    : (Action)null;
                var cb = SlotCard(content, e, rowH, false, false, false, () =>
                    Confirm($"「{e.Label}」 을 덮어쓸까?", $"지금 든 {h?.ko ?? hero}({SlotStore.StatusKo(e.Status)} · {SlotStore.Date(e.Updated)})는 사라지고 이번 모험의 사도가 들어간다 — 이름은 그대로", "덮어쓰기",
                        () => Overwrite(e, json, ne => { if (ne == null) return; close(); saved?.Invoke(ne); }), true), "over" + i, del);
                Stage.Hot["savepick.over" + i] = cb;
                if (Stage.Hot.TryGetValue("over" + i + ".del", out var dl)) Stage.Hot["savepick.del" + i] = dl;
            }
            Btn nb = null;
            nb = SlotNewRow(content, hero, rowH, () =>
            {
                if (!nb.Interactable) return;
                nb.Interactable = false;
                SaveNew(hero, json, SaveNameField ? SaveNameField.text : null, e =>
                {
                    if (nb) nb.Interactable = true;
                    if (e == null) { if (!SlotStore.CanAdd(hero)) Again(); return; }
                    close(); saved?.Invoke(e);
                });
            }, "savepick.newrow");
            Stage.Hot.Remove("savepick.new");
            if (!full) Stage.Hot["savepick.new"] = nb;
        }

        // ── 칸 이름 ──
        /// <summary>지금 열린 칸 이름 칸 · 마지막 오류(데모).</summary>
        public TMP_InputField SlotNameField { get; private set; }
        public string SlotNameError { get; private set; }

        /// <summary>칸 이름 바꾸기 — 비우면 「〇〇 저장 n」 으로. 되면 done(바뀐 칸).</summary>
        public void SlotNamePanel(SlotStore.Entry e, Action<SlotStore.Entry> done)
        {
            var size = Stage.Size;
            float w = Mathf.Min(680, size.x - 48), h = Mathf.Min(380, size.y - 24);
            var hero = Roster.OfCore(e.Hero);
            var (body, close, foot) = Stage.ModalBox("slotname", w, h, "칸 이름", $"16자 · 한글 · 영문 · 숫자 · 공백 · _ - . · 비우면 「{hero?.ko ?? e.Hero} 저장 {e.N + 1}」", true, null, Theme.BtnH + 24);
            var box = Ui.Rect("field", body).Band(1, 60, 50, 50, -24);
            var field = NameInput(box, e.Name ?? "", $"{hero?.ko ?? e.Hero} 저장 {e.N + 1}", SlotStore.NAME_MAX);
            SlotNameField = field; SlotNameError = null;
            var err = Ui.Text(body, "", Theme.FsSm, Theme.Bad, TextAlignmentOptions.Center); err.rectTransform.Band(1, 48, 10, 10, -96);
            var row = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(row, Theme.Gap, TextAnchor.MiddleCenter, null, true, true);
            Btn.Make(row, "취소", BtnStyle.PillDark, close, Theme.FsMd, "sname.no");
            Btn ok = null;
            ok = Btn.Make(row, "저장", BtnStyle.PillGold, () =>
            {
                if (!ok.Interactable) return;
                ok.Interactable = false;
                var body2 = new JObject { ["name"] = field.text ?? "" }.ToString(Newtonsoft.Json.Formatting.None);
                Online.Call("PATCH", $"/api/slot/{e.Id}", body2, r =>
                {
                    if (ok) ok.Interactable = true;
                    if (r.Ok && r.Json != null)
                    {
                        var ne = SlotStore.FromRow(r.Json, e.Json); ne.Best = e.Best;
                        SlotStore.Put(ne);
                        Debug.Log($"[HeroSave] 칸 {e.Id} 이름 → {ne.Name ?? "(없음)"}");
                        close(); done?.Invoke(ne);
                        return;
                    }
                    SlotNameError = r.Code == 400 ? r.Str("why") ?? "쓸 수 없는 이름이다" : Online.ErrorText(r);
                    if (err) err.text = SlotNameError;
                });
            }, Theme.FsMd, "sname.ok");
            Stage.Hot["sname.ok"] = ok;
        }

        // ── 사도 저장함 ──
        string boxHero;
        long boxSel;
        /// <summary>저장함에 보인 칸 수 · 고른 칸의 카드 수 · 고른 칸 id(데모 단언).</summary>
        public int SlotBoxCards { get; private set; }
        public int SlotBoxDetailCards { get; private set; }
        public long SlotBoxSel => boxSel;

        /// <summary>
        /// 사도 저장함 — 로비 메뉴 · 도감 사도 상세(「저장한 덱」 — hero 로 거른 채) · 프론티어 입구에서 연다.
        /// 왼쪽 사도 거르기(칸 수 n/10) · 가운데 칸 목록(+ 끝에 「새로 저장 n/10」) · 오른쪽에 고른 칸의 카드 덱 · 장비 3칸 · 성장 · 신탁이 바로.
        /// 로그인했으면 계정 칸(서버 목록으로 맞춤), 아니면 이 기기 사본.
        /// </summary>
        public void SlotBox(Action back, string hero = null)
        {
            if (hero != null) { boxHero = hero; boxSel = 0; }
            Stage.SetBg("stage1_1", 0.75f);
            void Build(bool synced)
            {
                if (boxHero != null && !SlotStore.Copies.Any(x => x.Hero == boxHero) && hero == null) boxHero = null;
                Stage.Show("slotbox", root => BuildSlotBox(root, back, synced));
            }
            if (Online.Ready) SyncSlots(Build); else Build(false);
        }
        void SlotBoxAgain(Action back, bool synced) => Stage.Show("slotbox", root => BuildSlotBox(root, back, synced), 0.1f, true);

        void BuildSlotBox(RectTransform root, Action back, bool synced)
        {
            var size = Stage.Size;
            float g = Theme.Gutter;
            var all = SlotStore.Copies;
            string hint = synced ? "계정 칸 · 사도마다 " + SlotStore.PerHero + "칸" : Online.Up ? "로그인 전 · 이 기기 사본" : "서버에 닿지 않음 · 이 기기 사본";
            ScreenHead(root, "보관함", hint, back, hd =>
            {
                HeadChip(hd, $"{all.Count}칸");
            });
            var sortB = SortToggle(root, () => SlotBoxAgain(back, synced), "box.sort", 180, 50);
            sortB.GetComponent<RectTransform>().At(1, 1, HeadToolsX, -25, 180, 50);   // 머리 오른쪽 묶음(홈 · 설정) 왼쪽

            float top = FTop;
            // 기다리는 사도(마지막 완주 판 — 아직 저장하지 않은) — 한 줄 띠
            var wt = SlotStore.Wait;
            if (wt != null && wt.Party.Count > 0 && !wt.AllSaved)
            {
                float wh = Theme.C(76, 68);
                var band = FPanel(root, "waiting");
                band.Band(1, wh, g, g, -top);
                var tt = Ui.Title(band, "저장을 기다리는 사도", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft); tt.rectTransform.At(0, 0.5f, FPad, 0, 260, 40);
                var row = Ui.Rect("row", band).Fill(FPad + 260, 8, 150, 8);
                Ui.Row(row, 20, TextAnchor.MiddleLeft, null, false, true);
                foreach (var hk in wt.Party)
                {
                    string k = hk;
                    var cell = Ui.Rect(k, row); cell.Pref(270, -1);
                    var face = W.Face(cell, Roster.OfCore(k), 48); face.At(0, 0.5f, 0, 0, 48, 48);
                    var nm = Ui.Title(cell, Roster.OfCore(k)?.ko ?? k, Theme.FsMd, Theme.Ink); nm.rectTransform.At(0, 0.5f, 58, 0, 110, 30);
                    nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                    bool saved = wt.SavedId.ContainsKey(k);
                    var sb = Btn.Make(cell, saved ? "저장됨" : Online.Up ? "저장" : "준비 중", saved || !Online.Up ? BtnStyle.PillDark : BtnStyle.PillGold, () =>
                    {
                        if (!Online.Up) { NeedAccount(null, null); return; }
                        NeedAccount("로그인하면 저장할 수 있다", () => SavePick(k, wt.Slots[k], _ => SlotBox(back)));
                    }, Theme.FsSm, "wait " + k);
                    sb.GetComponent<RectTransform>().At(1, 0.5f, 0, 0, 92, 40);
                    Stage.Hot["waiting:" + k] = sb;
                }
                var dropB = Btn.Make(band, "버리기", BtnStyle.PillDark, () => Confirm("기다리는 사도를 버릴까?", "저장하지 않은 사도는 사라진다 — 저장한 칸은 그대로", "버리기", () => { SlotStore.DropWaiting(); SlotBox(back); }, true), Theme.FsSm, "waiting.drop");
                dropB.GetComponent<RectTransform>().At(1, 0.5f, -FPad, 0, 110, 40);
                top += wh + FGap;
            }

            SlotBoxDetailCards = 0;
            if (all.Count == 0)
            {
                var em = FPanel(root, "empty");
                em.At(0.5f, 0.5f, 0, -40, Mathf.Min(900, size.x - g * 2), 200);
                EmptyNote(em, "저장한 사도가 없다", "모험을 완주하면 저장할 수 있다");
                SlotBoxCards = 0;
                return;
            }

            // 기둥 너비 — 사도 거르기 · 칸 목록 · 정보 판 · 카드(나머지)
            float fw = Theme.C(220, 200), lw = Theme.C(330, 300), iw = Theme.C(400, 380);
            RectTransform Col(string name, float x, float wd)
            {
                var r = Ui.Rect(name, root);
                r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 0.5f);
                r.offsetMin = new Vector2(x, g); r.offsetMax = new Vector2(x + wd, -top);
                return r;
            }

            // 왼쪽 — 사도 거르기(칸 수 n/10)
            var left = FPanel(Col("heroes", g, fw), "heroespanel"); left.Fill();
            var lc = Ui.Scroll(Ui.Rect("area", left).Fill(10, 10, 10, 10), out _);
            Ui.Col(lc, 6, TextAnchor.UpperLeft, new RectOffset(0, 4, 0, 6), true, false);
            void Filter(string hk, string label, string count, HeroInfo hero, bool over)
            {
                bool on = boxHero == hk;
                var b = Btn.Make(lc, null, BtnStyle.Cell, () => { boxHero = hk; boxSel = 0; SlotBoxAgain(back, synced); }, 0, "f " + (hk ?? "all"));
                b.Pref(-1, Theme.C(56, 52));
                b.Bg.sprite = on ? Theme.CellOn : Theme.Cell;
                if (!on) b.SetColor(Color.white.A(0.55f));
                var rt = b.GetComponent<RectTransform>();
                float x = 14;
                if (hero != null) { var f = W.Face(rt, hero, 38); f.At(0, 0.5f, 8, 0, 38, 38); x = 54; }
                var nm = Ui.Title(rt, label, Theme.FsBody, on ? Theme.Gold : Theme.Ink, TextAlignmentOptions.MidlineLeft); nm.rectTransform.Fill(x, 0, 56, 0);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                var c = Ui.Text(rt, count, Theme.FsCap, over ? Theme.Bad : Theme.Sub, TextAlignmentOptions.MidlineRight); c.rectTransform.At(1, 0.5f, -10, 0, 52, 30);
                Stage.Hot["box.f:" + (hk ?? "all")] = b;
            }
            Filter(null, "전체", all.Count.ToString(), null, false);
            foreach (var hk in SlotStore.Heroes) Filter(hk, Roster.OfCore(hk)?.ko ?? hk, SlotStore.CountKo(hk), Roster.OfCore(hk), !SlotStore.CanAdd(hk));

            // 가운데 — 칸 목록(+ 사도를 골랐으면 끝에 「새로 저장 n/10」)
            var shown = SlotStore.Sorted(all.Where(x => boxHero == null || x.Hero == boxHero), SlotStore.Order, SlotBest);
            SlotBoxCards = shown.Count;
            var sel = shown.FirstOrDefault(x => x.Id == boxSel) ?? shown.FirstOrDefault();
            boxSel = sel?.Id ?? 0;
            var mid = Col("list", g + fw + FGap, lw);
            var content = Ui.Scroll(mid, out _);
            Ui.Col(content, 10, TextAnchor.UpperLeft, new RectOffset(0, 10, 0, 10), true, false);
            float rowH = Theme.C(104, 92);
            for (int i = 0; i < shown.Count; i++)
            {
                var e = shown[i];
                var b = SlotCard(content, e, rowH, boxHero == null, e.Id == boxSel, false, () => { boxSel = e.Id; SlotBoxAgain(back, synced); }, "card" + i);
                Stage.Hot["box.card" + i] = b;
                Stage.Hot["box.card:" + e.Id] = b;
            }
            if (boxHero != null)
            {
                string hk = boxHero;
                bool waiting = wt != null && wt.Slots.ContainsKey(hk) && !wt.SavedId.ContainsKey(hk) && Online.Up;
                SlotNewRow(content, hk, Theme.C(76, 68), waiting ? () => NeedAccount("로그인하면 저장할 수 있다", () => SavePick(hk, wt.Slots[hk], _ => SlotBox(back))) : (Action)null, "box.new");
            }

            // 오른쪽 — 고른 칸(정보 판 · 카드 그리드)
            float ix = g + fw + FGap + lw + FGap;
            var info = FPanel(Col("infocol", ix, iw), "info"); info.Fill();
            var right = Col("cards", ix + iw + FGap, size.x - (ix + iw + FGap) - g);
            if (sel == null) { if (boxHero != null) EmptyNote(info, $"{Roster.OfCore(boxHero)?.ko ?? boxHero} 저장 칸이 없다", "모험을 완주하면 저장할 수 있다"); else EmptyNote(info, "칸을 고를 수 있다"); return; }
            var bv = SlotBest(sel);
            SaveDataBody(info, right, iw, sel.Hero, sel.Slot, $"{SlotStore.Date(sel.Updated)} · <color=#{ColorUtility.ToHtmlStringRGB(SlotStore.StatusColor(sel.Status))}>{SlotStore.StatusKo(sel.Status)}</color>",
                bv, bv != null ? (BestFromServer ? "이 칸 최고 · " + BestStatusKo(sel.BestStatus) : "이 칸 최고 · 이 기기") : "이 칸으로 낸 점수 없음", sel.Status == "bad" ? "문제 — " + (sel.Why ?? "규칙 검사 거절") : null, true, sel.Label);
            SlotBoxDetailCards = sel.Slot?.Cards.Count ?? 0;
            bool manage = Online.Ready && sel.OnServer;
            var del = Btn.Make(info, null, BtnStyle.PillRose, () => Confirm($"「{sel.Label}」 을 지울까?", "저장한 사도가 사라진다 — 낸 점수는 그대로", "지우기",
                () => DeleteSlot(sel, () => { boxSel = 0; SlotBox(back); }), true), 0, "del");
            del.GetComponent<RectTransform>().At(1, 1, -16, -16, 52, 44);
            var dic = Ui.Img(del.GetComponent<RectTransform>(), Theme.S("ic_trash"), Color.white, "ic"); dic.preserveAspect = true; dic.rectTransform.Fill(12, 8, 12, 8);
            var nb = Btn.Icon(info, Theme.S("ic_edit"), () => SlotNamePanel(sel, ne => { boxSel = ne.Id; SlotBox(back); }), 44);
            nb.GetComponent<RectTransform>().At(1, 1, -78, -16, 44, 44);
            if (!manage) { del.Interactable = false; nb.Interactable = false; }
            Stage.Hot["box.del"] = del; Stage.Hot["box.name"] = nb;
            Debug.Log($"[HeroSave] 저장함 — {(synced ? "계정" : "기기 사본")} {all.Count}칸 · 거름 {boxHero ?? "전체"} {shown.Count}칸 · 고른 칸 {sel.Id}「{sel.Label}」 카드 {SlotBoxDetailCards}장");
        }

        /// <summary>
        /// 칸 보기 — 저장 데이터 창과 같은 판(왼쪽 정보 · 장비 · 요약, 오른쪽 카드). changed 를 주면 저장함에서 연 것 — 아래에 「이름」 · 「지우기」.
        /// </summary>
        public void SlotView(SlotStore.Entry e, Action changed = null)
        {
            var s = e.Slot;
            var h = Roster.OfCore(e.Hero);
            var size = Stage.Size;
            float w = Mathf.Min(1440, size.x - 32), hh = size.y - 24;
            bool manage = changed != null && Online.Ready && e.OnServer;
            var (body, close, foot) = Stage.ModalBox("slotview", w, hh, e.Label, $"{h?.ko ?? e.Hero} · 저장 {SlotStore.Date(e.Updated)}", true, null, manage ? Theme.BtnH + 24 : 0);
            float iw = Mathf.Min(440, w * 0.32f);
            var info = Ui.Panel(Ui.Rect("infocol", body).Column(0, iw, 0, 0, 0), Theme.Panel, null, "info").rectTransform;
            info.Fill();
            var right = Ui.Rect("cards", body); right.anchorMin = new Vector2(0, 0); right.anchorMax = new Vector2(1, 1); right.offsetMin = new Vector2(iw + 18, 0); right.offsetMax = Vector2.zero;
            var bv = SlotBest(e);
            SaveDataBody(info, right, iw, e.Hero, s, $"<color=#{ColorUtility.ToHtmlStringRGB(SlotStore.StatusColor(e.Status))}>{SlotStore.StatusKo(e.Status)}</color> · 저장 {SlotStore.Date(e.Updated)}",
                bv, bv != null ? "이 칸 최고 점수" : "이 칸으로 낸 점수가 없다", e.Status == "bad" ? "문제 — " + (e.Why ?? "규칙 검사 거절") : null, true, e.Label);
            Stage.Hot["slotview.close"] = Stage.Hot.TryGetValue("modal.x", out var x) ? x : null;
            if (!manage) return;
            var row = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(row, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var nb = Btn.Make(row, "이름 바꾸기", BtnStyle.PillDark, () => SlotNamePanel(e, ne => { close(); changed(); }), Theme.FsMd, "sview.name"); nb.Pref(200, -1);
            var db = Btn.Make(row, "지우기", BtnStyle.PillRose, () => Confirm($"「{e.Label}」 을 지울까?", "저장한 사도가 사라진다 — 낸 점수는 그대로", "지우기", () => DeleteSlot(e, () => { close(); changed(); }), true), Theme.FsMd, "sview.del");
            db.Pref(160, -1);
            Stage.Hot["sview.name"] = nb; Stage.Hot["sview.del"] = db;
        }
    }
}
