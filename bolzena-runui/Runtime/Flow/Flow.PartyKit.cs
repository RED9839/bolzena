using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 팀 편성 편의(2026-10-08): 칸을 끌어 자리 바꾸기 · 프리셋 셋 + 최근 편성 · 무작위 편성 · 완주 기록 · 시작 덱 목록 · 크레파스 보드 요약 · 학년 안내.
    //   화면 틀은 Flow.Roster.cs(BuildPartyLight)가 세우고, 여기는 조각만 둔다. 저장은 Data/PartyStore.cs(PlayerPrefs).
    public partial class Flow
    {
        /// <summary>편성 칸 이름표 — 화면 왼쪽부터 후열 · 중열 · 전열(전투에서 사도는 오른쪽의 적을 보고 서므로 왼쪽이 뒤). 이름표뿐, 규칙은 없다.
        /// 전투는 파티 목록의 첫 사도가 맨 오른쪽(전열)에 서므로(BattleDirector.HeroPos) 모험을 열 때 화면 순서를 뒤집어 넘긴다.</summary>
        public static readonly string[] RowNames = { "후열", "중열", "전열" };
        /// <summary>파티 목록(코어) 순서 번호 → 열 이름(0 = 전열).</summary>
        public static string RowOfParty(int partyIdx) => RowNames[Mathf.Clamp(2 - partyIdx, 0, 2)];

        /// <summary>지금 편성 화면의 자리(자리 1 · 2 · 3 의 사도 key — 시험 · 점검이 읽는다).</summary>
        public string[] PartySlotsNow;

        // ═════════════ 끌어서 자리 바꾸기 ═════════════
        /// <summary>
        /// 편성 칸(큰 카드 + 수치 판 한 덩이)을 끌어 다른 칸 위에 놓으면 둘의 자리가 바뀐다(빈 칸이면 옮겨 간다). 마우스 · 터치 모두 EventSystem 끌기 하나로 된다 —
        /// 짧게 눌러 떼면 끌기가 아니라 눌림이라 칸 누르기(사도 목록 열기)는 그대로. 놓은 자리가 제자리거나 칸 밖이면 부드럽게 돌아간다.
        /// </summary>
        public sealed class PartySlotDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public RectTransform Slot, Root;
            public RectTransform[] Slots;
            public int Index;
            /// <summary>(from, to) — 자리 바꿈을 해 달라(화면을 다시 세운다).</summary>
            public Action<int, int> OnSwap;
            public bool Dragging { get; private set; }
            Vector2 home, p0; float homeX;
            int over = -1; RectTransform mark; CanvasGroup cg;

            Vector2 Local(Vector2 screen) { RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, screen, null, out var lp); return lp; }
            static float CenterX(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return (c[0].x + c[2].x) * 0.5f; }

            public void OnBeginDrag(PointerEventData e)
            {
                Dragging = true;
                home = Slot.anchoredPosition; p0 = Local(e.position);
                homeX = CenterX(Slot);
                Slot.SetAsLastSibling();
                cg = Slot.Group(); cg.alpha = 0.93f; cg.blocksRaycasts = false;
                Slot.localScale = Vector3.one * 1.03f;
                var b = GetComponent<Btn>(); if (b != null) b.Hover(false);
            }

            public void OnDrag(PointerEventData e)
            {
                if (!Dragging) return;
                Slot.anchoredPosition = home + (Local(e.position) - p0);
                // 놓일 칸 — 손(포인터) x 에 가장 가까운 칸 가운데(끄는 칸은 처음 자리 기준)
                int best = Index; float bd = Mathf.Abs(e.position.x - homeX);
                for (int j = 0; j < Slots.Length; j++)
                {
                    if (j == Index) continue;
                    float d = Mathf.Abs(e.position.x - CenterX(Slots[j]));
                    if (d < bd) { bd = d; best = j; }
                }
                SetOver(best == Index ? -1 : best);
            }

            void SetOver(int j)
            {
                if (j == over) return;
                over = j;
                if (mark) Destroy(mark.gameObject);
                mark = null;
                if (j < 0) return;
                var fr = Ui.Img(Slots[j], Theme.S("frame_thick", 24), Theme.Gold, "droptarget");
                fr.raycastTarget = false; fr.rectTransform.Fill(-6, -6, -6, -6);
                var veil = Ui.Img(fr.transform, Theme.Round, Theme.Gold.A(0.12f), "veil"); veil.raycastTarget = false; veil.rectTransform.Fill(6, 6, 6, 6);
                mark = fr.rectTransform;
            }

            public void OnEndDrag(PointerEventData e)
            {
                if (!Dragging) return;
                Dragging = false;
                int to = over;
                SetOver(-1);
                if (cg) { cg.alpha = 1; cg.blocksRaycasts = true; }
                Slot.localScale = Vector3.one;
                if (to >= 0 && to != Index) { OnSwap?.Invoke(Index, to); return; }
                var from = Slot.anchoredPosition; var slot = Slot; var h = home;
                Tw.Run(slot, 0.2f, k => { if (slot) slot.anchoredPosition = Vector2.LerpUnclamped(from, h, k); }, Tw.OutCubic);
            }
        }

        /// <summary>자리 바꿈 — 둘을 맞바꾸고(빈 칸이면 옮김) 같은 모양으로 다시 세운다(올라오는 효과는 건너뛴다).</summary>
        void SwapPartySlots(PartyState st, int a, int b, RectTransform root)
        {
            (st.Slots[a], st.Slots[b]) = (st.Slots[b], st.Slots[a]);
            Debug.Log($"[Party] 자리 바꿈 {a + 1} ↔ {b + 1} → {string.Join(" · ", st.Slots.Select(k => k ?? "-"))}");
            st.Quiet = true;
            BuildPartyLight(root, st);
        }

        // ═════════════ 완주 기록 ═════════════
        /// <summary>「완주 N」 작은 크림 알약 — N 이 0 이면 만들지 않는다. 오른쪽 위 기준 (x, y) 자리.</summary>
        static void ClearBadge(RectTransform parent, string heroKey, float x, float y, float scale = 1f)
        {
            int n = PartyStore.Cleared(heroKey);
            if (n <= 0) return;
            float w = (n >= 10 ? 74 : 66) * scale, h = 24 * scale;
            var rim = Ui.Img(parent, Theme.S("pill", 46), GradeBrown, "clears");
            rim.raycastTarget = false;
            rim.rectTransform.At(1, 1, x, y, w, h);
            var body = Ui.Img(rim.transform, Theme.S("pill", 46), GradeCream, "body"); body.raycastTarget = false; body.rectTransform.Fill(2, 2, 2, 2);
            var t = Ui.Title(rim.transform, $"완주 {n}", Mathf.Max(11, Theme.FsCap - 1) * scale, GradeBrown, TextAlignmentOptions.Center);
            t.raycastTarget = false;
            t.rectTransform.Fill(); t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
        }

        // ═════════════ 무작위 편성 ═════════════
        void PartyRandom(PartyState st, RectTransform root)
        {
            var pool = Roster.All.Where(h => h.Playable).ToList();
            if (pool.Count < 3) { Toast.Show("고를 수 있는 사도가 셋보다 적습니다"); return; }
            var pick = new List<HeroInfo>();
            while (pick.Count < 3) { var h = pool[UnityEngine.Random.Range(0, pool.Count)]; if (!pick.Contains(h)) pick.Add(h); }
            for (int i = 0; i < 3; i++) st.Slots[i] = pick[i].key;
            Debug.Log($"[Party] 무작위 편성 → {string.Join(" · ", st.Slots)}");
            HeroVoice.Speak(pick[0]);   // 셋이 한꺼번에 들어오니 첫 자리 사도 하나만
            st.Quiet = false;
            BuildPartyLight(root, st);
        }

        // ═════════════ 프리셋 · 최근 편성 ═════════════
        static string NamesOf(string[] keys) => keys.All(k => k == null) ? null
            : string.Join(" · ", keys.Select(k => k == null ? "—" : Roster.ByKey(k)?.ko ?? k));

        void PartyPresets(PartyState st, RectTransform root)
        {
            var (body, close, _) = Stage.ModalBox("presets", 1000, 700, "편성 불러오기 · 저장", "프리셋 다섯 — 왼쪽 성격 단추로 약점 속성용 표를 붙입니다(이번 적의 약점과 같으면 강조) · 이 기기에 남습니다");
            var list = Ui.Rect("rows", body).Fill(0, 0, 0, 0);
            Ui.Col(list, 8, TextAnchor.UpperCenter, null, true, false);

            void Load(string[] keys, string what)
            {
                if (keys.All(k => k == null)) { Toast.Show("비어 있습니다"); return; }
                for (int i = 0; i < 3; i++) st.Slots[i] = keys[i];
                Debug.Log($"[Party] {what} 불러옴 → {string.Join(" · ", st.Slots.Select(k => k ?? "-"))}");
                st.Quiet = false;
                close();
                BuildPartyLight(root, st);
                Toast.Show($"{what} 을 불러왔습니다");
            }

            var weak = string.IsNullOrEmpty(FoeNature) ? new List<string>() : RunPort.WeakTo(FoeNature);
            void Row(string label, string[] keys, bool preset, int n)
            {
                string tag = preset ? PartyStore.Tag(n) : null;
                bool hit = tag != null && weak.Contains(tag);   // 이번 모험 적의 약점 — 바로 고르게 강조
                var row = Ui.Img(list, Theme.Round, hit ? Color.Lerp(Theme.NavyWell, Theme.Gold, 0.22f) : Theme.NavyWell.A(0.75f), "row " + label);
                row.Pref(-1, Theme.C(84, 76));
                var rim = Ui.Img(row.transform, hit ? Theme.S("frame_thick", 24) : Theme.Frame, hit ? Theme.Gold : Theme.Edge.A(0.35f), "rim"); rim.rectTransform.Fill(); rim.raycastTarget = false;
                float bh = Theme.C(48, 46);
                float nameL = 24;
                if (preset)
                {
                    // 성격 단추 — 누를 때마다 없음 → 순수 → 광기 → 냉정 → 우울 → 활발. 아이콘이 이름을 대신하고, 없으면 번호
                    var tb = Btn.Make(row.transform, null, BtnStyle.Ghost, () =>
                    {
                        var order = new[] { null, "순수", "광기", "냉정", "우울", "활발" };
                        string nxt = order[(Array.IndexOf(order, tag) + 1) % order.Length];
                        PartyStore.SetTag(n, nxt);
                        Rebuild();
                    }, 0, "preset.tag" + n);
                    tb.Bg.sprite = Theme.S("circle"); tb.SetColor(Theme.NavyCell.A(0.95f));
                    tb.GetComponent<RectTransform>().At(0, 0.5f, 18, 0, 58, 58);
                    if (tag != null) { var ic = Ui.Img(tb.transform, Icon("성격_" + tag), Color.white, "ic"); ic.rectTransform.Fill(9, 9, 9, 9); ic.preserveAspect = true; ic.raycastTarget = false; }
                    else { var num = Ui.Title(tb.transform, n.ToString(), Theme.FsXl, Theme.Gold, TextAlignmentOptions.Center); num.rectTransform.Fill(); num.raycastTarget = false; }
                    var ring = Ui.Img(tb.transform, Theme.S("ring"), tag != null ? Theme.NatureCardOf(tag) : Theme.Edge.A(0.7f), "ring"); ring.rectTransform.Fill(); ring.raycastTarget = false;
                    Stage.Hot["preset.tag" + n] = tb;
                    nameL = 90;
                }
                else
                {
                    var lab = Ui.Title(row.transform, label, Theme.FsLg, Theme.Gold, TextAlignmentOptions.MidlineLeft);
                    lab.rectTransform.At(0, 0.5f, 22, 0, 150, 40); lab.textWrappingMode = TextWrappingModes.NoWrap;
                    nameL = 180;
                }
                var names = NamesOf(keys);
                var nt = Ui.Text(row.transform, (hit ? $"<color={Theme.GoldTag}>약점  </color>" : "") + (names ?? "비어 있음"), Theme.FsMd, names != null ? Theme.Ink : Theme.Dim, TextAlignmentOptions.MidlineLeft);
                nt.rectTransform.Fill(nameL, 0, preset ? 350 : 190, 0); nt.textWrappingMode = TextWrappingModes.NoWrap;
                nt.enableAutoSizing = true; nt.fontSizeMin = 12; nt.fontSizeMax = Theme.FsMd; nt.overflowMode = TextOverflowModes.Ellipsis;
                string loadKey = preset ? "preset.load" + n : "preset.recent";
                var ld = Btn.Make(row.transform, null, BtnStyle.PillDark, () => Load(keys, preset ? (tag != null ? tag + " 프리셋 " + n : "프리셋 " + n) : label), 0, loadKey);
                ld.GetComponent<RectTransform>().At(1, 0.5f, -16, 0, 150, bh);
                var lt = Ui.Title(ld.transform, "불러오기", Theme.FsBody, Theme.Ink, TextAlignmentOptions.Center); lt.rectTransform.Fill(); lt.textWrappingMode = TextWrappingModes.NoWrap;
                ld.Interactable = names != null; ld.Why = "저장된 편성이 없습니다";
                Stage.Hot[loadKey] = ld;
                if (!preset) return;
                var sv = Btn.Make(row.transform, null, BtnStyle.PillGold, () =>
                {
                    var cur = st.Slots.ToArray();
                    if (cur.All(k => k == null)) { Toast.Show("먼저 사도를 편성하세요"); return; }
                    PartyStore.SetPreset(n, cur);
                    Debug.Log($"[Party] 프리셋 {n} 저장 → {string.Join(" · ", cur.Select(k => k ?? "-"))}");
                    Toast.Show($"프리셋 {n} 에 저장했습니다");
                    Rebuild();
                }, 0, "preset.save" + n);
                sv.GetComponent<RectTransform>().At(1, 0.5f, -176, 0, 150, bh);
                var stt = Ui.Title(sv.transform, keys.Any(k => k != null) ? "덮어쓰기" : "저장", Theme.FsBody, Theme.Brown, TextAlignmentOptions.Center); stt.rectTransform.Fill(); stt.textWrappingMode = TextWrappingModes.NoWrap;
                Stage.Hot["preset.save" + n] = sv;
            }

            void Rebuild()
            {
                Ui.Clear(list);
                Row("최근 편성", PartyStore.Recent, false, 0);
                for (int n = 1; n <= PartyStore.PresetCount; n++) Row("프리셋 " + n, PartyStore.Preset(n), true, n);
            }
            Rebuild();
        }

        // ═════════════ 시작 덱 목록 ═════════════
        /// <summary>고른 사도의 시작 카드 전부 — 사도마다 한 줄(기본 + 시동). 시동 표식 · 누르면 카드 상세(같은 목록의 이전 · 다음).</summary>
        void PartyDeck(PartyState st)
        {
            var heroes = st.Slots.Where(k => k != null).Select(Roster.ByKey).Where(h => h != null && h.Playable).ToList();
            var decks = heroes.Select(h => (h, d: P.Data.Hero(h.CoreId))).Where(x => x.d != null)
                .Select(x => (x.h, x.d, ids: CardOrder.Sort(x.d.Starter, P.Data, new[] { x.d.Id }))).ToList();
            if (decks.Count == 0) { Toast.Show("사도를 먼저 편성하세요"); return; }
            int total = decks.Sum(x => x.ids.Count), opener = decks.Sum(x => x.ids.Count(id => P.Data.IsOpener(id)));
            float winH = Stage.Size.y - 24;
            var (body, close, _) = Stage.ModalBox("startdeck", 1500, winH, $"시작 덱 {total}장", $"사도마다 기본 카드 + 시동 카드({opener}장) — 카드를 누르면 상세");
            float rowGap = 12, labelW = Theme.C(170, 140);
            float availH = winH - 86 - 12 - 18;   // ModalBox 몸통 높이(머리 + 부제 · 여백 뺀)
            int maxCards = Mathf.Max(4, decks.Max(x => x.ids.Count));
            float availW = Mathf.Min(1500, Stage.Size.x - 32) - 40 - labelW - 12;
            float cw = Mathf.Min(200, (availH - rowGap * (decks.Count - 1) - 8) / decks.Count / 1.4f, (availW - 12 * (maxCards - 1)) / maxCards);
            float ch = cw * 1.4f;
            var all = decks.SelectMany(x => x.ids).ToList();
            int flat = 0;
            for (int r = 0; r < decks.Count; r++)
            {
                var (h, d, ids) = decks[r];
                var row = Ui.Rect("deckrow " + h.key, body).At(0, 1, 0, -r * (ch + rowGap), labelW + 12 + maxCards * (cw + 12), ch);
                var tag = Ui.Img(row, Theme.Round, Color.Lerp(Theme.NavyWell, NatureCol(h), 0.3f), "tag");
                tag.rectTransform.At(0, 0.5f, 0, 0, labelW, ch);
                var nm = Ui.Title(tag.transform, h.ko, Theme.FsLg, Theme.Ink, TextAlignmentOptions.Center);
                nm.rectTransform.At(0.5f, 0.5f, 0, 18, labelW - 12, 40); nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 14; nm.fontSizeMax = Theme.FsLg;
                var sub = Ui.Text(tag.transform, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(h.nature))}>{h.nature}</color> · {h.role}\n{ids.Count}장", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
                sub.rectTransform.At(0.5f, 0.5f, 0, -26, labelW - 12, 48);
                for (int i = 0; i < ids.Count; i++)
                {
                    var id = ids[i];
                    var c = W.Card(row, this, id, cw); c.At(0, 0.5f, labelW + 12 + i * (cw + 12), 0, cw, ch);
                    int at = all.IndexOf(id), idx = flat++;
                    var b = c.gameObject.AddComponent<Btn>(); b.OnClick = () => CardZoom(id, all, at);
                    Stage.Hot["deck.card" + idx] = b;
                    if (P.Data.IsOpener(id))
                    {
                        var tg = Ui.Img(c, Theme.Pill, new Color(0.35f, 0.2f, 0.02f, 0.95f), "ignition"); tg.rectTransform.At(1, 0, -6, 70 * cw / 200f, 46 * cw / 200f + 14, 22);
                        var tt = Ui.Title(tg.transform, "시동", 13, new Color(1f, 0.88f, 0.5f), TextAlignmentOptions.Center); tt.rectTransform.Fill();
                    }
                    Tw.Pop(c, 0.02f * idx, 0.88f, 0.28f);
                }
            }
        }

        // ═════════════ 크레파스 보드 요약 · 학년 ═════════════
        /// <summary>보드가 모험 시작에 주는 것을 한 줄 글로 — 능력치(공격 · 체력 · 방어 · 치명) · 시작 골드 · 학점 · 신탁 확률 따위. 칠한 칸이 없으면 null.</summary>
        static string BoardSummary(out int painted, out int cells)
        {
            var T = CrayonStore.Table; var Sv = CrayonStore.Save; var pk = CrayonStore.Perks;
            cells = T.Cells.Where(c => !c.Blank).Sum(c => c.Levels);
            painted = T.Cells.Where(c => !c.Blank).Sum(c => Math.Min(c.Levels, Sv.LevelOf(c.Id)));
            double V(string k) => pk.TryGetValue(k, out var v) ? v : 0;
            var stat = new List<string>();
            if (V("atk") > 0) stat.Add($"공격 +{V("atk") * 100:0.#}%");
            if (V("hp") > 0) stat.Add($"체력 +{V("hp") * 100:0.#}%");
            if (V("def") > 0) stat.Add($"방어 +{V("def") * 100:0.#}%");
            if (V("crit") > 0) stat.Add($"치명 +{V("crit"):0.#}%p");
            if (V("critDmg") > 0) stat.Add($"치명 피해 +{V("critDmg") * 100:0.#}%");
            var perk = new List<string>();
            if (V("gold") > 0) perk.Add($"시작 골드 +{V("gold"):0}");
            if (V("credits") > 0) perk.Add($"시작 학점 +{V("credits"):0}");
            if (V("oracleChance") > 0) perk.Add($"신탁 확률 +{V("oracleChance") * 100:0.#}%p");
            if (V("oraclePick") > 0) perk.Add($"신탁 후보 +{V("oraclePick"):0}");
            if (V("removeCost") > 0) perk.Add($"빼기 -{V("removeCost"):0}골드");
            if (V("eliteUp") > 0) perk.Add($"엘리트 장비 상향 {V("eliteUp") * 100:0}%");
            if (stat.Count == 0 && perk.Count == 0) return null;
            string sep = "   <color=" + Theme.GoldTag + ">|</color>   ";
            return string.Join(" · ", stat) + (stat.Count > 0 && perk.Count > 0 ? sep : "")
                + (perk.Count > 0 ? "<color=" + Theme.GoldTag + ">" + string.Join(" · ", perk) + "</color>" : "");
        }

        /// <summary>시작 학년 글 — 학점 보너스가 있으면 그만큼 쌓인 학년(Core.Grades.Of).</summary>
        static string StartGradeText()
        {
            var pk = CrayonStore.Perks;
            int credits = pk.TryGetValue("credits", out var c) ? (int)c : 0;
            int g = Core.Grades.Of(credits);
            return credits > 0 ? $"{Core.Grades.Name(g)}으로 시작 · 학점 {credits}" : $"{Core.Grades.Name(g)}으로 시작";
        }

        /// <summary>편성 아래 줄 — 왼쪽 보드 요약(누르면 보드 화면) · 오른쪽 학년 알약. w = 줄 너비.</summary>
        void PartyBoardRow(RectTransform parent, float w, float h)
        {
            var summary = BoardSummary(out int painted, out int cells);
            string gradeText = StartGradeText();
            float gradeW = Theme.C(250, 230);
            var board = Btn.Make(parent, null, BtnStyle.PillDark, CrayonScreen, 0, "board");
            var brt = board.GetComponent<RectTransform>();
            brt.At(0, 0, 0, 0, w - gradeW - 10, h);
            var ic = PastelIcon(brt, 1, h - 14, "ic"); ic.At(0, 0.5f, 16, 0, h - 14, h - 14);
            var lab = Ui.Title(brt, "교주 보드", Theme.FsMd, Theme.Gold, TextAlignmentOptions.MidlineLeft);
            lab.rectTransform.At(0, 0.5f, h + 6, 0, 110, h); lab.textWrappingMode = TextWrappingModes.NoWrap;
            var tx = Ui.Text(brt, summary ?? $"<color={Theme.SubTag}>아직 칠한 칸이 없습니다 — 크레파스를 칠하면 모험 시작에 혜택</color>", Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            tx.rectTransform.Fill(h + 122, 0, 150, 0); tx.textWrappingMode = TextWrappingModes.NoWrap;
            tx.enableAutoSizing = true; tx.fontSizeMin = 11; tx.fontSizeMax = Theme.FsSm; tx.overflowMode = TextOverflowModes.Ellipsis;
            var go = Ui.Title(brt, $"<color={Theme.SubTag}>{painted}/{cells}</color>  보드 ›", Theme.FsSm, Theme.Gold, TextAlignmentOptions.MidlineRight);
            go.rectTransform.Fill(0, 0, 18, 0); go.textWrappingMode = TextWrappingModes.NoWrap;
            Stage.Hot["party.board"] = board;

            // 학년 안내 — 크림 알약(갈색 테), 작게
            var rim = Ui.Img(parent, Theme.S("pill", 46), GradeBrown, "grade");
            rim.rectTransform.At(1, 0, 0, 0, gradeW, h);
            var inner = Ui.Img(rim.transform, Theme.S("pill", 46), GradeCream, "body"); inner.rectTransform.Fill(3, 3, 3, 3);
            var gt = Ui.Title(rim.transform, gradeText, Theme.FsSm, GradeBrown, TextAlignmentOptions.Center);
            gt.rectTransform.Fill(10, 0, 10, 0); gt.textWrappingMode = TextWrappingModes.NoWrap;
            gt.enableAutoSizing = true; gt.fontSizeMin = 11; gt.fontSizeMax = Theme.FsSm;
        }
    }
}
