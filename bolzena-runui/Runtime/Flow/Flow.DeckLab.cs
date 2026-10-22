using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 덱 만들기(2026-10-20 사용자 「사도 카드 원하는 덱 만들어서 프론티어 테스트」) — 시험 전용 화면.
    //   열리는 때: 에디터 · 실행 인자 -decklab(이 PC 시험 모드 — 서버 없이) · 시험 계정(Online.Tester — 서버 users.role 'tester', 웹 · PC 어디서나).
    //   공개판(웹 빌드 포함)에도 코드는 들어가되 위 셋이 아니면 로비에 진입점이 없다(도감 「모션 확인」 과 같은 방식).
    //   ① 덱 목록(이 기기 — DeckLabStore, 여러 벌 · 이름) → ② 덱 고치기: 자리 셋마다 사도 · 카드(그 사도의 기본 · 고유 · 교주 카드 — 장수 · 복제 자유) ·
    //      카드마다 신탁 갈래 · 장비 3칸(등급 · 종류 자유) · 판 성장 → 저장. 규칙 검사(HeroSlot.Check)를 넘지 못해도 저장 · 시험은 되고 「규칙 밖」 으로 표시한다.
    //   ③ 「M.E.O.W 시험 전투」 — 서버 없이 지금 시즌 보스 · 턴으로(무작위 씨앗 또는 입력) 그대로 싸운다(IFrontierFight lab — 규칙 검사 밖 슬롯도 · 사도별 딜 측정)
    //      → 결과: 점수 · 끝 · 사도별 딜(엔진 Battle.HpMeter). 서버로 아무것도 보내지 않는다.
    //   ④ 시험 계정만 「서버 시험 도전」 — 자리 셋을 서버 「시험 칸」(/api/test/slots — 일반 칸과 따로)에 올리고 그 칸으로 프론티어 도전 → 점수를 낸다(서버가 시험 점수로 — 랭킹 · 남의 편성 · 내 순위 밖).
    public partial class Flow
    {
        /// <summary>이 PC 시험 모드 — 에디터 · 실행 인자 -decklab.</summary>
        public static bool DeckLabLocal => Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), "-decklab") >= 0;
        /// <summary>로비에 「덱 만들기」 가 보이나 — 이 PC 시험 모드 또는 시험 계정.</summary>
        public static bool DeckLabOpen => DeckLabLocal || Online.Tester;

        // ── 데모 단언 ──
        public int LabDeckRows { get; private set; }
        public int LabCatalogCards { get; private set; }
        public int LabDeckCards { get; private set; }
        public bool LabRuleOut { get; private set; }
        public string LabRuleWhy { get; private set; }
        public FrontierResult LabLast { get; private set; }
        public string LabSubmit { get; private set; }
        public TMP_InputField LabNameField { get; private set; }
        public TMP_InputField LabSeedField { get; private set; }
        public string LabDeckId => labDeck?.Id;
        /// <summary>지금 고치는 자리의 슬롯(데모 — 신탁 있는 카드 찾기).</summary>
        public HeroSlot LabSlotShown => LabSlotNow;
        public int LabSeatNow => labSeat;
        /// <summary>데모 — 성장을 한 번에 크게(규칙 밖 덱을 만들어 「규칙 밖」 표시를 찍는다).</summary>
        public void DemoLabGrow(string key, int d) { if (Demo.Active) LabGrow(key, d); }

        DeckLabStore.Deck labDeck;
        int labSeat;
        string labFilter = "all";

        // ── 로비 진입점 ──
        /// <summary>로비 왼쪽 위(이름 아래) 작은 단추 — 보일 때만(이 PC 시험 모드 · 시험 계정). 로그인 상태가 바뀌면 다시 본다.</summary>
        void LobbyDeckLab(RectTransform root)
        {
            var b = NavyPill(root, "ic_deck", "덱 만들기 · 시험", DeckLabScreen, "lobby.decklab", 236, 50);
            b.GetComponent<RectTransform>().At(0, 1, 34, ProgStore.On ? -146 - Theme.C(84, 78) - 10 : -146, 236, 50);   // 성장 켬이면 잠재력 판(Flow.Prog.cs) 아래로
            void Paint()
            {
                if (!b) { Online.Changed -= Paint; return; }
                bool on = DeckLabOpen;
                b.gameObject.SetActive(on);
                if (on) Stage.Hot["lobby.decklab"] = b; else Stage.Hot.Remove("lobby.decklab");
            }
            Online.Changed += Paint;
            Paint();
        }

        // ── ① 덱 목록 ──
        public void DeckLabScreen()
        {
            if (!DeckLabOpen) { Toast.Show("시험 계정 · 시험 모드에서만 열린다"); return; }
            Stage.SetBg("stage37_1", 0.82f);
            Stage.Show("decklab", BuildDeckLab);
        }

        void BuildDeckLab(RectTransform root)
        {
            var fs = SeasonOf(Online.Season);
            ScreenHead(root, "덱 만들기", $"시험 전용 · {(Online.Tester ? "시험 계정" : "이 PC")} · 덱은 이 기기에만", Lobby, hd =>
            {
                HeadChip(hd, Online.Tester ? "시험 계정" : "시험 모드", ChipKind.Test, "ic_spark");
            });
            var size = Stage.Size;
            float g = Theme.Gutter, barH = Theme.BtnMainH;
            var panel = FPanel(root, "decks");
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.offsetMin = new Vector2(g, g + barH + FGap); panel.offsetMax = new Vector2(-g, -FTop);
            FLabel(panel, $"내 덱 {DeckLabStore.All.Count}벌", 14, "ic_deck");
            var area = Ui.Rect("area", panel).Fill(12, 12, 12, 54);
            var content = Ui.Scroll(area, out _);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(4, 12, 2, 8), true, false);
            var list = DeckLabStore.All;
            LabDeckRows = list.Count;
            if (list.Count == 0) EmptyNote(content, "만든 덱이 없다", "「새 덱」 으로 만들 수 있다", EmptyH);
            for (int k = 0; k < list.Count; k++)
            {
                var d = list[k];
                int kk = k;
                var row = Ui.Img(content, Theme.Round, k % 2 == 0 ? Theme.NavyCell.A(0.55f) : Theme.NavyWell.A(0.5f), "deck " + k);
                float rh = Theme.C(104, 92);
                row.Pref(-1, rh);
                var rt = row.rectTransform;
                var faces = Ui.Rect("faces", rt).At(0, 0.5f, 14, 0, 3 * 66, 64);
                Ui.Row(faces, 4, TextAnchor.MiddleLeft, null, false, false);
                for (int i = 0; i < Frontier.PARTY_MAX; i++)
                {
                    var s = d.Slot(i);
                    if (s != null) W.Face(faces, Roster.OfCore(s.Hero), 62);
                    else { var e = Ui.Img(faces, Theme.S("circle"), Theme.NavyWell, "empty"); e.Pref(62, 62); }
                }
                var (ruleOut, why) = LabRules(d);
                var nm = Ui.Title(rt, d.Name + (ruleOut ? RuleOutTag : ""), Theme.FsLg, Theme.Ink, TextAlignmentOptions.BottomLeft);
                nm.rectTransform.Fill(14 + 3 * 66 + 16, rh * 0.5f, 560, 6); nm.textWrappingMode = TextWrappingModes.NoWrap; nm.overflowMode = TextOverflowModes.Ellipsis;
                var party = d.Party();
                string meta = $"{string.Join(" · ", party.Select(s => $"{Roster.OfCore(s.Hero)?.ko ?? s.Hero} {s.Cards.Count}장"))}{(party.Count == 0 ? "빈 덱" : "")} · {SlotStore.Date(d.Updated)}"
                    + (d.Test.Any(x => x > 0) ? $" · <color={Theme.GoldTag}>서버 시험 칸</color>" : "");
                var mt = Ui.Text(rt, meta, Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft);
                mt.rectTransform.Fill(14 + 3 * 66 + 16, 6, 560, rh * 0.5f + 2); mt.textWrappingMode = TextWrappingModes.NoWrap; mt.overflowMode = TextOverflowModes.Ellipsis;
                var btns = Ui.Rect("btns", rt).At(1, 0.5f, -12, 0, 550, 52);
                Ui.Row(btns, 8, TextAnchor.MiddleRight, null, false, true);
                var ed = NavyPill(btns, "ic_edit", "고치기", () => DeckLabEdit(d.Id), "edit" + k, -1, 50);
                var ft = Btn.Make(btns, "시험 전투", BtnStyle.PillGold, () => LabFightPanel(d.Id), Theme.FsMd, "fight" + k); ft.Pref(150, 50);
                Stage.Hot["decklab.edit" + k] = ed; Stage.Hot["decklab.fight" + k] = ft;
                if (party.Count == 0) ft.Interactable = false;
                if (Online.Tester)
                {
                    var sv = Btn.Make(btns, "서버 시험", BtnStyle.PillRose, () => LabServerFight(d.Id), Theme.FsMd, "server" + k); sv.Pref(140, 50);
                    if (party.Count == 0) sv.Interactable = false;
                    Stage.Hot["decklab.server" + k] = sv;
                }
                var del = Btn.Icon(btns, Theme.S("ic_trash"), () => Confirm($"「{d.Name}」 덱을 지울까?", "이 기기의 덱만 지운다 — 서버 시험 칸 · 시험 점수는 그대로", "지우기", () => { DeckLabStore.Remove(d.Id); DeckLabScreen(); }, true), 48, "del" + k);
                Stage.Hot["decklab.del" + k] = del;
                if (ruleOut) Debug.Log($"[DeckLab] 덱 「{d.Name}」 규칙 밖 — {why}");
            }
            // 아래 줄 — 안내 · 새 덱
            var bar = Ui.Rect("bar", root).Band(0, barH, g, g, g);
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var info = Ui.Text(bar, $"<color={Theme.SubTag}>시험 전투 = {Boss(fs)?.Name ?? fs.Boss} · {fs.Turns}턴 · 서버로 보내지 않음{(Online.Tester ? " · 「서버 시험」 은 랭킹 밖" : "")}</color>", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.Pref(-1, -1, 1); info.Outline(0.2f); info.enableAutoSizing = true; info.fontSizeMin = 11; info.fontSizeMax = Theme.FsSm;
            var nb = Btn.Make(bar, "새 덱", BtnStyle.PillGold, () => DeckLabEdit(null), Theme.FsXl, "decklab.new");
            nb.Pref(Theme.BtnMainW, -1);
            Stage.Hot["decklab.new"] = nb;
            Debug.Log($"[DeckLab] 덱 목록 {list.Count}벌 · {(Online.Tester ? "시험 계정" : "이 PC 시험 모드")}");
        }

        EnemyDef Boss(FrontierSeason fs) => P.Data.Enemy(fs.Boss);
        static string RuleOutTag => $"  <size=62%><color={Theme.BadTag}>규칙 밖</color></size>";

        /// <summary>덱이 규칙 밖인가 — 자리마다 HeroSlot.Check · 슬롯끼리(Frontier.WhyNot). 까닭은 앞 둘만.</summary>
        (bool, string) LabRules(DeckLabStore.Deck d)
        {
            var party = d.Party();
            if (party.Count == 0) return (false, null);
            var errs = new List<string>();
            foreach (var s in party) { var c = HeroSlot.Check(P.Data, s); errs.AddRange(c.Errors.Select(e => $"{Roster.OfCore(s.Hero)?.ko ?? s.Hero} — {e}")); }
            if (errs.Count == 0) { var w = Frontier.WhyNot(P.Data, party, SeasonOf(Online.Season)); if (w != null) errs.Add(w); }
            return (errs.Count > 0, errs.Count > 0 ? string.Join(" / ", errs.Take(2)) : null);
        }

        // ── ② 덱 고치기 ──
        /// <summary>덱 고치기(id 가 없으면 새 덱).</summary>
        public void DeckLabEdit(string id)
        {
            var d = id != null ? DeckLabStore.Get(id) : null;
            labDeck = d != null ? new DeckLabStore.Deck { Id = d.Id, Name = d.Name, Updated = d.Updated, Slots = d.Slots.ToList(), Test = d.Test.ToList() }
                : new DeckLabStore.Deck { Id = DeckLabStore.NewId(), Name = $"시험 덱 {DeckLabStore.All.Count + 1}" };
            labSeat = 0; labFilter = "all";
            LabAgain();
        }
        void LabAgain() => Stage.Show("decklab_edit", BuildLabEdit, 0.1f, true);

        HeroSlot LabSlotNow => labDeck?.Slot(labSeat);
        void LabSetSlot(HeroSlot s)
        {
            labDeck.Slots[labSeat] = s?.Json();
        }
        HeroSlot LabNewSlot(string hero)
        {
            return new HeroSlot
            {
                Hero = hero, Rules = Fingerprint.Rules(), Content = P.Data.ContentHash,
                From = new SlotFrom { Result = "clear", Floor = 2, Ended = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm'Z'", System.Globalization.CultureInfo.InvariantCulture), Party = new List<string> { hero } },
            };
        }
        /// <summary>자리마다 from.party 를 덱의 사도들로(규칙 검사 「편성에 이 사도가 없다」 가 덱 자체 때문에 뜨지 않게).</summary>
        void LabFixParty()
        {
            var heroes = Enumerable.Range(0, labDeck.Slots.Count).Select(labDeck.Slot).Where(s => s != null).Select(s => s.Hero).ToList();
            for (int i = 0; i < labDeck.Slots.Count; i++)
            {
                var s = labDeck.Slot(i);
                if (s == null) continue;
                s.From ??= new SlotFrom();
                s.From.Result = "clear"; s.From.Party = heroes.ToList();
                labDeck.Slots[i] = s.Json();
            }
        }

        Action labPaint;

        void BuildLabEdit(RectTransform root)
        {
            var size = Stage.Size;
            float g = Theme.Gutter, barH = Theme.BtnMainH, top = FTop;
            ScreenHead(root, "덱 고치기", "자리마다 사도 · 카드 · 신탁 · 아티팩트 · 성장", () => Confirm("저장하지 않고 나갈까?", "저장하지 않은 바뀜은 사라진다", "나가기", DeckLabScreen));
            // ── 자리 줄 + 이름 · 저장 ──
            float seatH = Theme.C(86, 76);
            var seats = Ui.Rect("seats", root);
            seats.anchorMin = new Vector2(0, 1); seats.anchorMax = new Vector2(1, 1); seats.pivot = new Vector2(0.5f, 1);
            seats.offsetMin = new Vector2(g, -top - seatH); seats.offsetMax = new Vector2(-g, -top);
            Ui.Row(seats, 10, TextAnchor.MiddleLeft, null, false, true);
            var seatRow = Ui.Rect("seatrow", seats); seatRow.Pref(-1, -1, 1);
            Ui.Row(seatRow, 10, TextAnchor.MiddleLeft, null, false, true);
            var nameBox = Ui.Rect("name", seats); nameBox.Pref(Theme.C(300, 260), seatH - 16);
            LabNameField = NameInput(nameBox, labDeck.Name, "덱 이름", 24);
            LabNameField.onValueChanged.AddListener(v => { if (labDeck != null) labDeck.Name = v; });
            var save = Btn.Make(seats, "저장", BtnStyle.PillGold, () => LabSave(), Theme.FsLg, "lab.save"); save.Pref(150, seatH - 16);
            Stage.Hot["lab.save"] = save;

            // ── 세 기둥 — 왼쪽 자리 판(사도 · 장비 · 성장 · 규칙) · 가운데 덱 · 오른쪽 카드 목록 ──
            float y0 = top + seatH + FGap, bottom = g + barH + FGap;
            float lw = Theme.C(330, 300), rw = Mathf.Min(Theme.C(560, 520), size.x * 0.36f);
            RectTransform Col(string name, float l, float r, float w)
            {
                var c = FPanel(root, name);
                if (w > 0) { c.anchorMin = new Vector2(l < 0 ? 1 : 0, 0); c.anchorMax = new Vector2(l < 0 ? 1 : 0, 1); c.pivot = new Vector2(l < 0 ? 1 : 0, 0.5f); c.offsetMin = new Vector2(l < 0 ? -g - w : g, bottom); c.offsetMax = new Vector2(l < 0 ? -g : g + w, -y0); }
                else { c.anchorMin = Vector2.zero; c.anchorMax = Vector2.one; c.offsetMin = new Vector2(l, bottom); c.offsetMax = new Vector2(-r, -y0); }
                return c;
            }
            var left = Col("seat", 0, 0, lw);
            var right = Col("catalog", -1, 0, rw);
            var mid = Col("deck", g + lw + FGap, g + rw + FGap, 0);

            // 오른쪽 — 카드 목록(그 사도의 기본 · 고유 · 교주 카드)
            var slot0 = LabSlotNow;
            FLabel(right, "카드 목록", 14, "ic_book");
            var chips = Ui.Rect("filter", right).Band(1, 40, FPad, FPad, -48);
            Ui.Row(chips, 6, TextAnchor.MiddleLeft, null, false, true);
            foreach (var (k, label) in new[] { ("all", "전체"), ("base", "기본"), ("unique", "고유"), ("leader", "교주") })
            {
                var fb = Btn.Make(chips, label, labFilter == k ? BtnStyle.PillGold : BtnStyle.PillDark, () => { labFilter = k; LabAgain(); }, Theme.FsSm, "f " + k);
                fb.Pref(86, 38);
                Stage.Hot["lab.filter:" + k] = fb;
            }
            var catArea = Ui.Rect("area", right).Fill(10, 10, 10, 96);
            var cat = Ui.Scroll(catArea, out _);
            var cgrid = cat.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            float ccw = Theme.C(116, 104);
            cgrid.cellSize = new Vector2(ccw, ccw * 1.4f); cgrid.spacing = new Vector2(8, 10); cgrid.padding = new RectOffset(4, 4, 4, 12);
            LabCatalogCards = 0;
            if (slot0 == null)
            {
                UnityEngine.Object.DestroyImmediate(cgrid);
                Ui.Col(cat, 8, TextAnchor.UpperCenter, null, true, false);
                Ui.Text(cat, "자리에 사도를 먼저 골라야 한다", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center).Pref(-1, 120);
            }
            else
            {
                int ci = 0;
                foreach (var (def, id) in LabCatalog(slot0.Hero))
                {
                    var cv = P.Data.View(id);
                    if (cv == null) continue;
                    var card = W.Card(cat, this, id, ccw, "cat " + def.Id, cv, slot0.Hero);
                    string cid = id;
                    var b = RowTap(card.GetComponent<UnityEngine.UI.Image>(), () => LabAddCard(cid));
                    Stage.Hot["lab.cat" + ci] = b;
                    Stage.Hot["lab.catid:" + def.Id] = b;
                    ci++;
                }
                LabCatalogCards = ci;
            }

            // 가운데 — 이 자리의 덱
            var deckLabel = FLabel(mid, "", 14, "ic_deck");
            var deckArea = Ui.Rect("area", mid).Fill(10, 10, 10, 50);
            var deckContent = Ui.Scroll(deckArea, out _);
            var dgrid = deckContent.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            float dcw = Theme.C(122, 108);
            dgrid.cellSize = new Vector2(dcw, dcw * 1.4f); dgrid.spacing = new Vector2(8, 10); dgrid.padding = new RectOffset(4, 4, 4, 12);

            // 왼쪽 — 자리 판
            var leftBody = Ui.Rect("body", left).Fill(FPad, 14, FPad, 14);

            void PaintSeats()
            {
                for (int c = seatRow.childCount - 1; c >= 0; c--) Destroy(seatRow.GetChild(c).gameObject);
                for (int i = 0; i < Frontier.PARTY_MAX; i++)
                {
                    int k = i;
                    var s = labDeck.Slot(i);
                    var hi = s != null ? Roster.OfCore(s.Hero) : null;
                    var sb = Btn.Make(seatRow, null, BtnStyle.Cell, () => { labSeat = k; LabAgain(); }, 0, "seat" + i);
                    sb.Pref(Theme.C(250, 220), seatH - 8);
                    if (i == labSeat) sb.Bg.sprite = Theme.CellOn;
                    var srt = sb.GetComponent<RectTransform>();
                    if (hi != null) W.Face(srt, hi, seatH - 26).At(0, 0.5f, 10, 0, seatH - 26, seatH - 26);
                    else { var pl = Ui.Img(srt, Theme.S("ic_plus"), Theme.Dim, "plus"); pl.preserveAspect = true; pl.rectTransform.At(0, 0.5f, 16, 0, 40, 40); }
                    bool bad = s != null && !HeroSlot.Check(P.Data, s).Ok;
                    var st = Ui.Title(srt, hi != null ? hi.ko : $"자리 {i + 1}", Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                    st.rectTransform.Fill(seatH - 4, (seatH - 8) * 0.5f + 1, 8, 6); st.textWrappingMode = TextWrappingModes.NoWrap; st.enableAutoSizing = true; st.fontSizeMin = 12; st.fontSizeMax = Theme.FsMd;
                    var ss = Ui.Text(srt, s != null ? $"카드 {s.Cards.Count}장{(bad ? $" · <color={Theme.BadTag}>규칙 밖</color>" : "")}" : "사도 고르기", Theme.FsCap, Theme.Sub, TextAlignmentOptions.MidlineLeft);
                    ss.rectTransform.Fill(seatH - 4, 8, 8, (seatH - 8) * 0.5f + 1); ss.textWrappingMode = TextWrappingModes.NoWrap;
                    Stage.Hot["lab.seat" + i] = sb;
                }
            }

            void PaintDeck()
            {
                for (int c = deckContent.childCount - 1; c >= 0; c--) Destroy(deckContent.GetChild(c).gameObject);
                var s = LabSlotNow;
                LabDeckCards = s?.Cards.Count ?? 0;
                deckLabel.text = s != null ? $"이 자리 덱 {s.Cards.Count}장" : "이 자리 덱";
                if (s == null) return;
                for (int k = 0; k < s.Cards.Count; k++)
                {
                    var c = s.Cards[k];
                    var def = P.Data.Card(c.Id);
                    if (def == null) continue;
                    var view = GameData.WithBless(P.Data.View(c.Id, c.O), c.B != null ? Battle.BlessOf(def, c.B) : null);
                    if (view == null) continue;
                    var card = W.Card(deckContent, this, c.Id, dcw, "deck " + c.Id, view, s.Hero, false, CardMark.Of(P.Data, c.Id, c.O, c.B));
                    int kk = k;
                    Stage.Hot["lab.deck" + k] = RowTap(card.GetComponent<UnityEngine.UI.Image>(), () => LabCardMenu(kk));
                }
            }

            void PaintLeft()
            {
                for (int c = leftBody.childCount - 1; c >= 0; c--) Destroy(leftBody.GetChild(c).gameObject);
                var s = LabSlotNow;
                var hi = s != null ? Roster.OfCore(s.Hero) : null;
                var head = Ui.Rect("head", leftBody).Band(1, 70, 0, 0, 0);
                if (hi != null) W.Face(head, hi, 64).At(0, 0.5f, 0, 0, 64, 64);
                var hn = Ui.Title(head, hi?.ko ?? $"자리 {labSeat + 1} — 빈 자리", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                hn.rectTransform.Fill(hi != null ? 74 : 0, 0, 0, 0); hn.textWrappingMode = TextWrappingModes.NoWrap; hn.enableAutoSizing = true; hn.fontSizeMin = 14; hn.fontSizeMax = Theme.FsLg;
                var pick = NavyPill(leftBody, "ic_refresh", s == null ? "사도 고르기" : "사도 바꾸기", LabHeroPick, "lab.hero", -1, 46);
                pick.GetComponent<RectTransform>().Band(1, 46, 0, 0, -76);
                Stage.Hot["lab.hero"] = pick;
                if (s == null) return;
                float y = 76 + 46 + 12;
                var gl = Ui.Title(leftBody, "아티팩트", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft); gl.rectTransform.Band(1, 28, 0, 0, -y); y += 30;
                float rowH = Theme.C(52, 44);
                foreach (var slotName in R.SLOTS)
                {
                    s.Gear.TryGetValue(slotName, out var gid);
                    var e = gid != null ? P.Data.Equip(gid) : null;
                    var gc = e != null ? Theme.GradeOf(e.Grade) : Theme.Dim;
                    string sn = slotName;
                    var gb = Btn.Make(leftBody, null, BtnStyle.Cell, () => LabEquipPick(sn), 0, "gear " + slotName);
                    gb.GetComponent<RectTransform>().Band(1, rowH, 0, 0, -y);
                    var grt = gb.GetComponent<RectTransform>();
                    Ui.Img(grt, Theme.Round, gc, "grade").rectTransform.At(0, 0.5f, 6, 0, 5, rowH - 14);
                    var pic = gid != null ? CardArt.Equip(gid) : null;
                    var ic = Ui.Img(grt, pic ?? W.SlotIcon(slotName), pic != null ? Color.white : gc, "icon"); ic.preserveAspect = true; ic.rectTransform.At(0, 0.5f, 18, 0, rowH - 12, rowH - 12);
                    var gt = Ui.Text(grt, e != null ? $"{e.Name} <size=80%><color=#{ColorUtility.ToHtmlStringRGB(gc)}>{e.Grade}</color></size>" : $"<color={Theme.DimTag}>{slotName} 없음</color>", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
                    gt.rectTransform.Fill(rowH + 14, 0, 8, 0); gt.textWrappingMode = TextWrappingModes.NoWrap; gt.overflowMode = TextOverflowModes.Ellipsis;
                    Stage.Hot["lab.gear:" + slotName] = gb;
                    y += rowH + 6;
                }
                y += 6;
                var grl = Ui.Title(leftBody, "모험 성장", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft); grl.rectTransform.Band(1, 28, 0, 0, -y); y += 30;
                foreach (var (key, label) in new[] { ("atk", "공격"), ("def", "방어"), ("crit", "치명") })
                {
                    int v = key == "atk" ? s.Growth.Atk : key == "def" ? s.Growth.Def : s.Growth.Crit;
                    var gr = Ui.Rect("grow " + key, leftBody).Band(1, 40, 0, 0, -y);
                    var lt = Ui.Text(gr, label, Theme.FsBody, Theme.Ink, TextAlignmentOptions.MidlineLeft); lt.rectTransform.Fill(0, 0, 160, 0);
                    var vt = Ui.Title(gr, $"{v:+0;-0;0}", Theme.FsLg, v != 0 ? Theme.Gold : Theme.Sub, TextAlignmentOptions.Center); vt.rectTransform.At(1, 0.5f, -52, 0, 70, 38);
                    string kk = key;
                    var mb = Btn.Make(gr, "−", BtnStyle.PillDark, () => LabGrow(kk, -5), Theme.FsLg, "minus"); mb.GetComponent<RectTransform>().At(1, 0.5f, -126, 0, 48, 38);
                    var pb = Btn.Make(gr, "+", BtnStyle.PillDark, () => LabGrow(kk, 5), Theme.FsLg, "plus"); pb.GetComponent<RectTransform>().At(1, 0.5f, 0, 0, 48, 38);
                    Stage.Hot[$"lab.grow.{key}-"] = mb; Stage.Hot[$"lab.grow.{key}+"] = pb;
                    y += 44;
                }
                // 규칙 검사 — 넘지 못해도 시험은 된다(「규칙 밖」 표시)
                var chk = HeroSlot.Check(P.Data, s);
                var rl = Ui.Text(leftBody, chk.Ok ? $"<color={Theme.GoodTag}>규칙 안</color> — 일반 저장 칸 규칙(HeroSlot) 통과" : $"<color={Theme.BadTag}>규칙 밖</color> — {string.Join(" / ", chk.Errors.Take(3))}", Theme.FsCap, Theme.Sub, TextAlignmentOptions.TopLeft);
                rl.rectTransform.Fill(0, 0, 0, y + 6); rl.enableAutoSizing = true; rl.fontSizeMin = 10; rl.fontSizeMax = Theme.FsCap;
            }

            labPaint = () =>
            {
                if (!seatRow) return;
                PaintSeats(); PaintDeck(); PaintLeft();
                var (ro, why) = LabRules(labDeck);
                LabRuleOut = ro; LabRuleWhy = why;
            };
            labPaint();

            // 아래 줄 — 안내 · 시험 전투
            var bar = Ui.Rect("bar", root).Band(0, barH, g, g, g);
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var info = Ui.Text(bar, $"<color={Theme.SubTag}>목록의 카드를 누르면 담고 · 덱의 카드를 누르면 신탁 · 한 장 더 · 빼기 — 장수 · 복제 자유</color>", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.Pref(-1, -1, 1); info.Outline(0.2f); info.enableAutoSizing = true; info.fontSizeMin = 11; info.fontSizeMax = Theme.FsSm;
            var fb2 = Btn.Make(bar, "저장 후 시험 전투", BtnStyle.PillGold, () => { if (LabSave()) LabFightPanel(labDeck.Id); }, Theme.FsLg, "lab.fight");
            fb2.Pref(Theme.BtnMainW + 30, -1);
            Stage.Hot["lab.fight"] = fb2;
        }

        /// <summary>그 사도의 카드 목록 — 기본 → 고유 → 교주(어디서나 같은 차례), 같은 묶음은 비용 · 이름. 덱에 넣을 id(교주 카드는 「@사도」).</summary>
        List<(CardDef def, string id)> LabCatalog(string hero)
        {
            var all = P.Data.Cards.Values;
            var mine = all.Where(c => c.Hero == hero && !c.Token && !c.IsStatusCard && !c.IsCurse).ToList();
            var bases = mine.Where(c => !c.Unique).OrderBy(c => c.Cost).ThenBy(c => c.Name, StringComparer.Ordinal);
            var uniques = mine.Where(c => c.Unique).OrderBy(c => c.Cost).ThenBy(c => c.Name, StringComparer.Ordinal);
            var leaders = all.Where(c => c.Neutral).OrderBy(c => c.Cost).ThenBy(c => c.Name, StringComparer.Ordinal);
            var list = new List<(CardDef, string)>();
            if (labFilter == "all" || labFilter == "base") list.AddRange(bases.Select(c => (c, c.Id)));
            if (labFilter == "all" || labFilter == "unique") list.AddRange(uniques.Select(c => (c, c.Id)));
            if (labFilter == "all" || labFilter == "leader") list.AddRange(leaders.Select(c => (c, GameData.WithOwner(c.Id, hero))));
            return list;
        }

        /// <summary>새로 담을 카드의 id — 처음이면 그대로, 이미 있으면 고유 카드(복제할 수 있는 것)는 복제본 「^」, 그 밖은 한 장 번호 「#n」(카드마다 신탁을 따로 고를 수 있게).</summary>
        string LabNextId(HeroSlot s, string baseId)
        {
            bool Has(string x) => s.Cards.Any(c => c.Id == x);
            if (!Has(baseId)) return baseId;
            var def = P.Data.Card(baseId);
            var v = P.Data.View(baseId);
            if (def != null && def.Unique && def.Hero != null && v != null && !v.IsOnly && !v.IsTaboo)
            {
                var cp = baseId + "^";
                while (Has(cp)) cp += "^";
                return cp;
            }
            for (int n = 2; ; n++) { var x = GameData.WithInst(baseId, n); if (!Has(x)) return x; }
        }

        void LabAddCard(string baseId)
        {
            var s = LabSlotNow;
            if (s == null) return;
            if (s.Cards.Count >= HeroSlot.MAX_CARDS) { Toast.Show($"한 자리 카드는 {HeroSlot.MAX_CARDS}장까지"); return; }
            var id = LabNextId(s, baseId);
            s.Cards.Add(new SlotCard { Id = id });
            LabSetSlot(s);
            labPaint?.Invoke();
            Toast.Show($"「{P.Data.Card(id)?.Name ?? id}」 담음 · {s.Cards.Count}장", 1.2f);
        }

        void LabGrow(string key, int d)
        {
            var s = LabSlotNow; if (s == null) return;
            int Clamp(int v) => Mathf.Clamp(v + d, 0, 999);
            if (key == "atk") s.Growth.Atk = Clamp(s.Growth.Atk); else if (key == "def") s.Growth.Def = Clamp(s.Growth.Def); else s.Growth.Crit = Clamp(s.Growth.Crit);
            LabSetSlot(s);
            labPaint?.Invoke();
        }

        /// <summary>덱의 카드 한 장 — 신탁 갈래 고르기(카드 모습으로) · 한 장 더 · 빼기.</summary>
        void LabCardMenu(int k)
        {
            var s = LabSlotNow;
            if (s == null || k < 0 || k >= s.Cards.Count) return;
            var c = s.Cards[k];
            var def = P.Data.Card(c.Id);
            if (def == null) return;
            var size = Stage.Size;
            int n = def.Complete ? 0 : def.Oracles.Count;
            float cw = Theme.C(170, 140);
            float w = Mathf.Min(size.x - 48, Mathf.Max(720, (n + 1) * (cw + 16) + 80)), hh = Mathf.Min(size.y - 24, cw * 1.4f + 250);
            var (body, close, foot) = Stage.ModalBox("labcard", w, hh, $"{def.Name} — 신탁 갈래", def.Complete ? "완성형 카드 — 신탁이 없다" : n == 0 ? "이 카드는 신탁이 없다" : "고르면 그 모습으로 바뀐다", true, null, Theme.BtnH + 24);
            var row = Ui.Rect("row", body).Fill(20, 10, 20, 10);
            var content = Ui.Scroll(row, out _, true);
            Ui.Row(content, 16, TextAnchor.UpperLeft, new RectOffset(4, 4, 4, 4), false, false);
            for (int o = 0; o <= n; o++)
            {
                int oo = o;
                var cell = Ui.Rect("o" + o, content); cell.Pref(cw, cw * 1.4f + 44);
                var view = GameData.WithBless(P.Data.View(c.Id, o), c.B != null ? Battle.BlessOf(def, c.B) : null);
                var card = W.Card(cell, this, c.Id, cw, "card", view, s.Hero, false, CardMark.Of(P.Data, c.Id, o, c.B));
                card.At(0.5f, 1, 0, 0, cw, cw * 1.4f);
                var b = RowTap(card.GetComponent<UnityEngine.UI.Image>(), () =>
                {
                    c.O = oo; LabSetSlot(s); close(); labPaint?.Invoke();
                    Toast.Show(oo == 0 ? "신탁 없음" : $"신탁 {oo} — {def.Oracles[oo - 1].Name}", 1.4f);
                });
                if (o == c.O) { var on = Ui.Img(card, Theme.Frame, Theme.Gold, "on"); on.rectTransform.Fill(-3, -3, -3, -3); }
                var lt = Ui.Title(cell, o == 0 ? "신탁 없음" : $"{o}. {def.Oracles[o - 1].Name}", Theme.FsSm, o == c.O ? Theme.Gold : Theme.Ink, TextAlignmentOptions.Center);
                lt.rectTransform.Band(0, 38, 0, 0, 0); lt.textWrappingMode = TextWrappingModes.NoWrap; lt.enableAutoSizing = true; lt.fontSizeMin = 10; lt.fontSizeMax = Theme.FsSm;
                Stage.Hot["laboracle" + o] = b;
            }
            var fr = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(fr, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var more = Btn.Make(fr, "한 장 더", BtnStyle.PillDark, () => { close(); LabAddCard(GameData.NoInst(c.Id).TrimEnd('^')); }, Theme.FsMd, "labcard.more"); more.Pref(180, -1);
            var del = Btn.Make(fr, "빼기", BtnStyle.PillRose, () => { s.Cards.RemoveAt(k); LabSetSlot(s); close(); labPaint?.Invoke(); }, Theme.FsMd, "labcard.del"); del.Pref(160, -1);
            Stage.Hot["labcard.more"] = more; Stage.Hot["labcard.del"] = del;
        }

        /// <summary>자리의 사도 고르기 — 고를 수 있는 사도 모두(다른 자리에 든 사도는 못 고름). 바꾸면 그 자리의 카드는 비운다(장비 · 성장은 둔다).</summary>
        void LabHeroPick()
        {
            var size = Stage.Size;
            float w = Mathf.Min(1400, size.x - 32), hh = size.y - 24;
            var (body, close, foot) = Stage.ModalBox("labhero", w, hh, $"자리 {labSeat + 1} — 사도 고르기", "같은 사도는 한 덱에 하나 · 바꾸면 그 자리 카드는 비운다", true);
            var others = Enumerable.Range(0, labDeck.Slots.Count).Where(i => i != labSeat).Select(labDeck.Slot).Where(s => s != null).Select(s => s.Hero).ToHashSet();
            var content = Ui.Scroll(Ui.Rect("area", body).Fill(), out _);
            var grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            float cw = Theme.C(124, 112), ch = cw + 40;
            grid.cellSize = new Vector2(cw, ch); grid.spacing = new Vector2(10, 10); grid.padding = new RectOffset(8, 8, 8, 12);
            foreach (var hi in Roster.Players.OrderBy(h => h.ko, StringComparer.Ordinal))
            {
                string key = hi.CoreId;
                if (key == null || P.Data.Hero(key) == null) continue;
                bool taken = others.Contains(key);
                var b = Btn.Make(content, null, BtnStyle.Cell, () =>
                {
                    if (taken) { Toast.Show("같은 사도는 한 덱에 하나만 넣을 수 있다"); return; }
                    var cur = LabSlotNow;
                    var ns = LabNewSlot(key);
                    if (cur != null) { ns.Gear = new Dictionary<string, string>(cur.Gear); ns.Growth = new SlotGrowth { Atk = cur.Growth.Atk, Def = cur.Growth.Def, Crit = cur.Growth.Crit }; }
                    // 처음 고른 사도는 그 사도의 기본 카드로 시작(판의 시작 덱과 같은 꼴 — 고치기 쉽게)
                    foreach (var c in P.Data.Cards.Values.Where(c => c.Hero == key && !c.Unique && !c.Token && !c.IsStatusCard && !c.IsCurse).OrderBy(c => c.Cost).ThenBy(c => c.Name, StringComparer.Ordinal))
                        ns.Cards.Add(new SlotCard { Id = LabNextId(ns, c.Id) });
                    labDeck.Slots[labSeat] = ns.Json();
                    labDeck.Test[labSeat] = 0;   // 사도가 바뀌면 서버 시험 칸도 새로(칸의 사도는 바꿀 수 없다)
                    LabFixParty();
                    close(); LabAgain();
                }, 0, "hero " + key);
                if (taken) b.Interactable = false;
                if (LabSlotNow?.Hero == key) b.Bg.sprite = Theme.CellOn;
                var rt = b.GetComponent<RectTransform>();
                W.Face(rt, hi, cw - 30).At(0.5f, 1, 0, -8, cw - 30, cw - 30);
                var nm = Ui.Title(rt, hi.ko, Theme.FsSm, taken ? Theme.Dim : Theme.Ink, TextAlignmentOptions.Center); nm.rectTransform.Band(0, 30, 4, 4, 6);
                nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 10; nm.fontSizeMax = Theme.FsSm;
                Stage.Hot["labhero:" + key] = b;
            }
        }

        /// <summary>장비 칸 고르기 — 그 칸의 모든 장비(등급 · 종류 자유) · 빼기.</summary>
        void LabEquipPick(string slotName)
        {
            var s = LabSlotNow; if (s == null) return;
            var size = Stage.Size;
            float w = Mathf.Min(1400, size.x - 32), hh = size.y - 24;
            var (body, close, foot) = Stage.ModalBox("labequip", w, hh, $"{slotName} 고르기", "등급 · 종류 자유 — 시험 전용", true, null, Theme.BtnH + 24);
            var content = Ui.Scroll(Ui.Rect("area", body).Fill(), out _);
            var grid = content.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();
            float cw = Theme.C(300, 280), ch = Theme.C(200, 180);
            grid.cellSize = new Vector2(cw, ch); grid.spacing = new Vector2(10, 10); grid.padding = new RectOffset(8, 8, 8, 12);
            s.Gear.TryGetValue(slotName, out var cur);
            int k = 0;
            foreach (var e in P.Data.Equips.Values.Where(e => e.Slot == slotName).OrderBy(e => { int i = GradeOrder(e.Grade); return i < 0 ? 99 : i; }).ThenBy(e => e.Name, StringComparer.Ordinal))
            {
                string id = e.Id;
                var card = W.Equip(content, this, id, cw, ch, "eq " + id);
                if (id == cur) { var on = Ui.Img(card, Theme.Frame, Theme.Gold, "on"); on.rectTransform.Fill(-2, -2, -2, -2); }
                var b = RowTap(card.GetComponent<UnityEngine.UI.Image>(), () => { s.Gear[slotName] = id; LabSetSlot(s); close(); labPaint?.Invoke(); });
                Stage.Hot["labeq:" + id] = b;
                Stage.Hot["labeq" + k++] = b;
            }
            var fr = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(fr, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var none = Btn.Make(fr, $"{slotName} 빼기", BtnStyle.PillDark, () => { s.Gear.Remove(slotName); LabSetSlot(s); close(); labPaint?.Invoke(); }, Theme.FsMd, "labeq.none");
            none.Pref(200, -1);
            if (cur == null) none.Interactable = false;
            Stage.Hot["labeq.none"] = none;
        }

        /// <summary>저장(이 기기) — 이름이 비면 막는다. 빈 자리는 그대로 둔다.</summary>
        bool LabSave()
        {
            if (labDeck == null) return false;
            var nm = (LabNameField != null ? LabNameField.text : labDeck.Name)?.Trim();
            if (string.IsNullOrEmpty(nm)) { Toast.Show("덱 이름을 적어야 한다"); return false; }
            labDeck.Name = nm.Length > 24 ? nm.Substring(0, 24) : nm;
            LabFixParty();
            DeckLabStore.Put(new DeckLabStore.Deck { Id = labDeck.Id, Name = labDeck.Name, Slots = labDeck.Slots.ToList(), Test = labDeck.Test.ToList() });
            var (ro, why) = LabRules(labDeck);
            LabRuleOut = ro; LabRuleWhy = why;
            Debug.Log($"[DeckLab] 저장 「{labDeck.Name}」 — [{string.Join(", ", labDeck.Party().Select(x => $"{x.Hero} {x.Cards.Count}장 · 신탁 {x.Cards.Count(c => c.O > 0)} · 장비 {x.Gear.Count} · 성장 {x.Growth.Atk}/{x.Growth.Def}/{x.Growth.Crit}"))}]{(ro ? " · 규칙 밖 — " + why : " · 규칙 안")}");
            Toast.Show(ro ? "저장했다 — 규칙 밖(시험 전용)" : "저장했다", 1.6f);
            labPaint?.Invoke();
            return true;
        }

        // ── ③ 시험 전투(이 PC — 서버 없이) ──
        /// <summary>시험 전투 창 — 지금 시즌 보스 · 턴, 씨앗(비우면 무작위).</summary>
        public void LabFightPanel(string deckId)
        {
            var d = DeckLabStore.Get(deckId);
            if (d == null || d.Party().Count == 0) { Toast.Show("사도가 있는 덱만 싸울 수 있다"); return; }
            var fs = SeasonOf(Online.Season);
            var (ro, why) = LabRules(d);
            var (body, close, foot) = Stage.ModalBox("labfight", Mathf.Min(820, Stage.Size.x - 48), Theme.C(470, 430), $"{Boss(fs)?.Name ?? fs.Boss} 시험 전투", $"{fs.Turns}턴 · 서버로 보내지 않음 · 사도별 딜 측정", true, null, Theme.BtnH + 24);
            var col = Ui.Rect("col", body).Fill(32, 10, 32, 10);
            Ui.Col(col, 10, TextAnchor.UpperLeft, null, true, false);
            var t = Ui.Title(col, d.Name + (ro ? RuleOutTag : ""), Theme.FsXl, Theme.Ink); t.Pref(-1, 44);
            var faces = Ui.Rect("faces", col); faces.Pref(-1, 74);
            Ui.Row(faces, 8, TextAnchor.MiddleLeft, null, false, false);
            foreach (var s in d.Party()) W.Face(faces, Roster.OfCore(s.Hero), 70);
            if (ro) { var rw = Ui.Text(col, "규칙 밖 — " + why, Theme.FsSm, Theme.Bad); rw.Pref(-1, 46); rw.enableAutoSizing = true; rw.fontSizeMin = 10; rw.fontSizeMax = Theme.FsSm; }
            var sl = Ui.Text(col, "씨앗 — 비우면 무작위(같은 씨앗 · 같은 수면 같은 전투)", Theme.FsSm, Theme.Sub); sl.Pref(-1, 28);
            var fb = Ui.Rect("seed", col); fb.Pref(-1, 58);
            LabSeedField = NameInput(fb, "", "무작위", 10);
            LabSeedField.contentType = TMP_InputField.ContentType.IntegerNumber;
            var fr = Ui.Rect("row", foot).Fill(40, 12, 40, 12);
            Ui.Row(fr, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var go = Btn.Make(fr, "싸우기", BtnStyle.PillGold, () =>
            {
                long seed;
                var txt = LabSeedField != null ? LabSeedField.text.Trim() : "";
                if (txt.Length == 0) seed = UnityEngine.Random.Range(1, int.MaxValue);
                else if (!long.TryParse(txt, out seed) || seed < 1 || seed > int.MaxValue) { Toast.Show($"씨앗은 1~{int.MaxValue}"); return; }
                close();
                LabStartFight(d, fs, seed, null);
            }, Theme.FsLg, "labfight.go");
            go.Pref(Theme.BtnMainW, -1);
            Stage.Hot["labfight.go"] = go;
        }

        /// <summary>서버 시험 도전의 시도(있으면 결과를 서버에 낸다).</summary>
        sealed class LabAttempt { public string Id; public List<string> Heroes; }

        void LabStartFight(DeckLabStore.Deck d, FrontierSeason fs, long seed, LabAttempt server, List<HeroSlot> slots = null)
        {
            slots ??= d.Party();
            var why = Frontier.WhyNot(P.Data, slots, fs, false);
            if (why != null) { Toast.Show("이 덱으로는 전투를 지을 수 없습니다 — " + why, 4f); Debug.LogWarning("[DeckLab] 열 수 없음 — " + why); return; }
            if (FrontierFight == null) { Toast.Show("전투 화면이 없다"); return; }
            LabLast = null; LabSubmit = null;
            var (ro, _) = LabRules(d);
            Debug.Log($"[DeckLab] 시험 전투{(server != null ? "(서버 시도 " + server.Id + ")" : "(이 PC)")} — 「{d.Name}」 · {fs.Id}/{fs.Boss}/{fs.Turns}턴 · 씨앗 {seed} · [{string.Join(", ", slots.Select(s => s.Hero))}]{(ro ? " · 규칙 밖" : "")}");
            Hitch.Mark("시험 전투 열기");
            FrontierFight.OpenFrontier(P.Data, slots, fs, seed, res => LabDone(d, fs, seed, res, server, ro), true);
        }

        void LabDone(DeckLabStore.Deck d, FrontierSeason fs, long seed, FrontierResult res, LabAttempt server, bool ruleOut)
        {
            LabLast = res;
            var meter = res.Meter ?? new Dictionary<string, long[]>();
            Debug.Log($"[DeckLab] 끝 — {res.Over} · {res.Played}턴 · 점수 {res.Score} · 사도별 딜 [{string.Join(", ", meter.OrderByDescending(kv => kv.Value[0]).Select(kv => $"{(kv.Key.Length > 0 ? kv.Key : "?")} {kv.Value[0]}(+{kv.Value[1]})"))}] · 합 {meter.Values.Sum(a => a[0])}{(res.Error != null ? " · " + res.Error : "")}");
            Stage.SetBg("stage37_1", 0.8f);
            Stage.Show("decklab_result", root => BuildLabResult(root, d, fs, seed, res, server, ruleOut));
            if (server != null && res.Tape != null) LabSubmitServer(server, res);
        }

        Action<string> labPaintSubmit;

        void BuildLabResult(RectTransform root, DeckLabStore.Deck d, FrontierSeason fs, long seed, FrontierResult res, LabAttempt server, bool ruleOut)
        {
            var boss = Boss(fs);
            int bossHp = boss != null ? (int)Math.Round(boss.Hp * fs.Hp) : 0;
            ScreenHead(root, "시험 전투 결과", $"「{d.Name}」 · 씨앗 {seed}{(server != null ? " · 서버 시험 시도" : " · 이 PC(서버로 보내지 않음)")}", DeckLabScreen, hd =>
            {
                if (!ruleOut) return;
                HeadChip(hd, "규칙 밖", ChipKind.Warn);
            });
            var size = Stage.Size;
            float g = Theme.Gutter, barH = Theme.BtnMainH;
            // 왼쪽 — 점수
            float lw = Theme.C(520, 480);
            var lp = FPanel(root, "score");
            lp.anchorMin = new Vector2(0, 0); lp.anchorMax = new Vector2(0, 1); lp.pivot = new Vector2(0, 0.5f);
            lp.offsetMin = new Vector2(g, g + barH + FGap); lp.offsetMax = new Vector2(g + lw, -FTop);
            string overKo = res.Over == "win" ? "보스 처치!" : res.Over == "lose" ? "파티가 쓰러졌다" : res.Over == Frontier.TIME ? "시간 종료" : "전투 중단";
            FLabel(lp, overKo, 14, "ic_crown");
            var sl = Ui.Text(lp, "보스가 잃은 HP — 점수", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center); sl.rectTransform.Band(1, 30, 0, 0, -60);
            var sc = Ui.Title(lp, $"{res.Score:N0}", Theme.C(Theme.FsDisplay, Theme.Fs3xl + 10), Theme.Gold, TextAlignmentOptions.Center); sc.rectTransform.Band(1, Theme.C(100, 80), 0, 0, -92); sc.Outline(0.1f);
            Tw.Pop(sc.rectTransform, 0.2f, 1.4f, 0.5f);
            var meta = Ui.Text(lp, $"{boss?.Name ?? fs.Boss} · {res.Played} / {fs.Turns}턴 · 보스 HP 의 {(bossHp > 0 ? 100.0 * res.Score / bossHp : 0):0.0}%\n턴당 {(res.Played > 0 ? res.Score / res.Played : 0):N0}{(res.Error != null ? " · " + res.Error : "")}", Theme.FsBody, Theme.Ink, TextAlignmentOptions.Center);
            meta.rectTransform.Band(1, 64, 10, 10, -Theme.C(200, 176));
            var sub = Ui.Text(lp, server == null ? $"<color={Theme.SubTag}>이 PC 시험 — 서버로 보내지 않았다</color>" : res.Tape == null ? $"<color={Theme.BadTag}>기록이 없어 낼 수 없다</color>" : "시험 점수를 내는 중…", Theme.FsSm, Theme.Ink, TextAlignmentOptions.Center);
            sub.rectTransform.Band(0, 60, 16, 16, 16);
            labPaintSubmit = s => { if (sub) sub.text = s; };
            if (LabSubmit != null && LabSubmit != "sending") labPaintSubmit(LabSubmitLine);

            // 오른쪽 — 사도별 딜(엔진 Battle.HpMeter — 측정 전용)
            var rp = FPanel(root, "meter");
            rp.anchorMin = Vector2.zero; rp.anchorMax = Vector2.one;
            rp.offsetMin = new Vector2(g + lw + FGap, g + barH + FGap); rp.offsetMax = new Vector2(-g, -FTop);
            FLabel(rp, "사도별 딜(보스가 잃은 HP)", 14, "ic_swords");
            var col = Ui.Rect("rows", rp).Fill(FPad, 16, FPad, 56);
            Ui.Col(col, 10, TextAnchor.UpperLeft, null, true, false);
            var meter = res.Meter ?? new Dictionary<string, long[]>();
            long total = Math.Max(1, meter.Values.Sum(a => a[0]));
            long top = Math.Max(1, meter.Values.Select(a => a[0]).DefaultIfEmpty(0).Max());
            var order = d.Party().Select(s => s.Hero).ToList();
            foreach (var k in meter.Keys) if (!order.Contains(k)) order.Add(k);
            int rows = 0;
            foreach (var key in order)
            {
                meter.TryGetValue(key, out var a);
                long bossD = a?[0] ?? 0, other = a?[1] ?? 0;
                if (key.Length == 0 && bossD == 0 && other == 0) continue;
                var hi = key.Length > 0 ? Roster.OfCore(key) : null;
                var row = Ui.Rect("row " + key, col); row.Pref(-1, Theme.C(84, 70));
                if (hi != null) W.Face(row, hi, Theme.C(66, 56)).At(0, 0.5f, 0, 0, Theme.C(66, 56), Theme.C(66, 56));
                float x0 = Theme.C(80, 68);
                var nm = Ui.Title(row, hi?.ko ?? "주인 모름(지속 · 반사 등)", Theme.FsMd, Theme.Ink, TextAlignmentOptions.TopLeft); nm.rectTransform.Fill(x0, Theme.C(42, 34), 260, 4); nm.textWrappingMode = TextWrappingModes.NoWrap;
                var vt = Ui.Title(row, $"{bossD:N0} <size=60%><color={Theme.SubTag}>{100.0 * bossD / total:0.0}%</color></size>", Theme.FsLg, Theme.Gold, TextAlignmentOptions.TopRight); vt.rectTransform.At(1, 1, 0, -2, 260, 34);
                var track = Ui.Img(row, Theme.Round, Theme.NavyWell, "track"); track.rectTransform.Fill(x0, 10, 0, Theme.C(48, 40));
                var fill = Ui.Img(track.rectTransform, Theme.Round, Theme.NatureOf(hi?.nature), "fill");
                fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)bossD / top), 1); fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
                var ot = Ui.Text(row, $"턴당 {(res.Played > 0 ? bossD / res.Played : 0):N0}{(other > 0 ? $" · 드론 · 소환물 {other:N0}" : "")}", Theme.FsCap, Theme.Sub, TextAlignmentOptions.BottomRight); ot.rectTransform.Fill(x0, 10, 4, Theme.C(48, 40)); ot.Outline(0.2f);
                rows++;
            }
            if (rows == 0) EmptyNote(col, "딜 기록이 없다", null, 80);
            LabMeterRows = rows;

            // 아래 — 같은 씨앗 다시 · 새 씨앗 · 덱 목록
            var bar = Ui.Rect("bar", root).Band(0, barH, g, g, g);
            Ui.Row(bar, Theme.Gap, TextAnchor.MiddleRight, null, false, true);
            var info = Ui.Text(bar, $"<color={Theme.SubTag}>딜 = 그 사도가 보스에게서 깎은 HP(막힌 몫 · 넘친 피해 빼고) — 측정 전용, 점수 · 기록과 무관</color>", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            info.Pref(-1, -1, 1); info.Outline(0.2f); info.enableAutoSizing = true; info.fontSizeMin = 11; info.fontSizeMax = Theme.FsSm;
            if (server == null)
            {
                var again = Btn.Make(bar, "같은 씨앗 다시", BtnStyle.PillDark, () => LabStartFight(d, fs, seed, null), Theme.FsMd, "labres.again"); again.Pref(220, -1);
                var fresh = Btn.Make(bar, "새 씨앗", BtnStyle.PillDark, () => LabFightPanel(d.Id), Theme.FsMd, "labres.new"); fresh.Pref(160, -1);
                Stage.Hot["labres.again"] = again; Stage.Hot["labres.new"] = fresh;
            }
            var back = Btn.Make(bar, "덱 목록", BtnStyle.PillGold, DeckLabScreen, Theme.FsLg, "labres.back"); back.Pref(Theme.BtnMainW, -1);
            Stage.Hot["labres.back"] = back;
            Stage.OnBack = DeckLabScreen;
        }
        public int LabMeterRows { get; private set; }

        // ── ④ 서버 시험 도전(시험 계정만) ──
        /// <summary>자리 셋을 서버 시험 칸에 올리고(있으면 덮어쓰기 · 없거나 사라졌으면 새로) 그 칸으로 프론티어를 연다.</summary>
        public void LabServerFight(string deckId)
        {
            var d = DeckLabStore.Get(deckId);
            if (d == null || d.Party().Count == 0) { Toast.Show("사도가 있는 덱만 도전할 수 있다"); return; }
            if (!Online.Tester) { Toast.Show("시험 계정으로 로그인해야 한다"); return; }
            var seats = Enumerable.Range(0, d.Slots.Count).Where(i => d.Slots[i] != null).ToList();
            Toast.Show("시험 칸에 올리는 중…", 1.5f);
            int at = 0;
            void Next()
            {
                if (at >= seats.Count) { LabServerStart(d, seats); return; }
                int i = seats[at];
                var hero = d.Slot(i).Hero;
                string body = d.Slots[i];
                void Post()
                {
                    Online.Call("POST", $"/api/test/slots?hero={Uri.EscapeDataString(hero)}&name={Uri.EscapeDataString(LabSlotName(d.Name))}", body, r =>
                    {
                        if (!r.Ok) { LabServerFail("시험 칸에 올리지 못했다", r); return; }
                        d.Test[i] = r.Json?["id"]?.Type == JTokenType.Integer ? (long)r.Json["id"] : 0;
                        at++; Next();
                    });
                }
                if (d.Test[i] > 0)
                    Online.Call("PUT", $"/api/test/slot/{d.Test[i]}", body, r =>
                    {
                        if (r.Ok) { at++; Next(); return; }
                        if (r.Code == 404 || r.Code == 400) { d.Test[i] = 0; Post(); return; }   // 지워졌거나 사도가 다르다 — 새로
                        LabServerFail("시험 칸을 덮어쓰지 못했다", r);
                    });
                else Post();
            }
            Next();
        }

        /// <summary>시험 칸 이름 — 서버 칸 이름 규칙(16자 · 한글 · 영문 · 숫자 · 공백 · _ - .)에 맞게 덱 이름을 줄인다.</summary>
        static string LabSlotName(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ch in name ?? "") if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '_' || ch == '-' || ch == '.') sb.Append(ch);
            var s = sb.ToString().Trim();
            if (s.Length > 16) s = s.Substring(0, 16).Trim();
            return s.Any(char.IsLetterOrDigit) ? s : "시험 덱";
        }

        void LabServerFail(string what, Online.Resp r)
        {
            string why = r.Code == 403 && r.Str("error") == "tester only" ? "시험 계정이 아니다 — 권한이 회수됐을 수 있다" : r.Code == 400 ? r.Error : Online.ErrorText(r);
            Debug.LogWarning($"[DeckLab] {what} — {r.Code} {r.Error}");
            Toast.Show($"{what} — {why}", 4f);
            if (r.Code == 403) Online.Refresh();
        }

        void LabServerStart(DeckLabStore.Deck d, List<int> seats)
        {
            DeckLabStore.Put(d);   // 받은 시험 칸 id 를 남긴다
            var body = new JObject { ["slots"] = new JArray(seats.Select(i => new JObject { ["test"] = d.Test[i] })) }.ToString(Newtonsoft.Json.Formatting.None);
            Online.Call("POST", "/api/frontier/start", body, r =>
            {
                if (!r.Ok) { LabServerFail("도전하지 못했다", r); return; }
                try
                {
                    var fs = SeasonOf(r.Json["season"] as JObject);
                    var slots = ((JArray)r.Json["slots"]).Select(x => HeroSlot.Parse(x["slot"].ToString(Newtonsoft.Json.Formatting.None))).ToList();
                    var att = new LabAttempt { Id = r.Str("attempt"), Heroes = slots.Select(s => s.Hero).ToList() };
                    Debug.Log($"[DeckLab] 서버 시험 시도 — {att.Id} · test {r.Json["test"]} · 씨앗 {r.Json["seed"]}");
                    LabStartFight(d, fs, (long)r.Json["seed"], att, slots);
                }
                catch (Exception e) { Debug.LogException(e); Toast.Show("도전 응답을 읽지 못했습니다 — " + e.Message, 3.5f); }
            });
        }

        string LabSubmitLine => LabSubmit == "test" ? $"<color={Theme.GoldTag}>시험 기록으로 냈다</color> <color={Theme.SubTag}>— 랭킹에 오르지 않고 내 기록에 「시험」 으로 남는다</color>"
            : LabSubmit == "failed" ? $"<color={Theme.BadTag}>내지 못했다</color>" : $"<color={Theme.SubTag}>낸 상태 {LabSubmit}</color>";

        void LabSubmitServer(LabAttempt att, FrontierResult res)
        {
            LabSubmit = "sending";
            var body = new JObject { ["attempt"] = att.Id, ["score"] = res.Score, ["tape"] = res.Tape }.ToString(Newtonsoft.Json.Formatting.None);
            Online.Call("POST", "/api/frontier/submit", body, r =>
            {
                bool test = r.Ok && r.Json?["test"]?.Type == JTokenType.Boolean && (bool)r.Json["test"];
                LabSubmit = r.Ok ? (test ? "test" : r.Str("status")) : "failed";
                Debug.Log($"[DeckLab] 시험 제출 → {r.Code} {r.Str("status")} · test {test} · 시도 {att.Id}{(r.Ok ? "" : " · " + r.Error)}");
                labPaintSubmit?.Invoke(LabSubmitLine + (r.Ok ? "" : " — " + Online.ErrorText(r)));
            });
        }
    }
}
