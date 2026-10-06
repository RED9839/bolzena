using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 이벤트 — 카제나 이벤트 배치를 따른다(배치만 — 그림 · 글 · 아이콘은 우리 것). 판 화면 톤(Docs/톤.md) · 판 HUD 그대로.
    //   화면 전체가 장면: 왼쪽에 파티 셋(전투 화면과 같은 SD 전투 스파인), 오른쪽에 등장 사도 · NPC(전투 스파인 → 스탠딩, 없으면 배경만)
    //   위 가로 띠: 지문 두 줄(가운데 · 한 글자씩, 누르면 다 보임) · 왼쪽 끝 이벤트 아이콘 원 + 이벤트 이름
    //   아래: 선택지 카드를 가로로(4개 넘으면 두 줄) — 윗줄 「~하기」, 아랫줄 결과 칩(아이콘 + 수치, 손해는 붉게), 조건이 모자라면 흐리고 까닭을 붉게.
    //     대화로 이어지는 선택지는 위 왼쪽 말풍선 · 아래 가운데 꼬리 점. 고른 카드를 한 번 더 누르면 결정. 「떠난다」는 오른쪽 아래 둥근 단추
    //   결과: 같은 장면, 띠 지문이 바뀌고 아래에 「받은 것」(보상 화면 꼴 유리 줄 · 얻은 카드 · 기록) · 오른쪽 아래 「떠나기」
    //     받은 것은 고르기 전 상태(골드 · HP · 덱)와 비교해 센다 — 고를 것(카드 빼기 · 복제 · 카드 · 신탁 · 축복 · 도박)을 마친 뒤까지 쌓인다.
    //   고를 것 창 · 싸움으로 이어지는 흐름은 예전과 같다(PendingPick · FightStop). 휴식 담당의 공용 연출 부품이 오면 PendingPick 을 그것으로 바꾼다.
    public partial class Flow
    {
        static readonly Dictionary<string, string> NpcSpine = new Dictionary<string, string>
        {
            ["시스트"] = "st_sist", ["앨리스"] = "st_alice", ["쥬비"] = "st_jubee", ["골디"] = "st_goldy", ["에르핀"] = "st_erpin",
        };

        // 고르기 전 모습 — 결과 화면이 「받은 것」을 센다
        class EvSnap { public string Key; public int Gold, Hp, MaxHp; public List<string> Deck; }
        EvSnap evSnap;
        /// <summary>고르기 직전 상태를 적어 둔다 — 결과 칸이 얻은 · 잃은 것을 셈한다. 시범이 core 로 바로 고를 때도 부른다.</summary>
        public void EventSnapNow() { var E = P.S.Event; evSnap = new EvSnap { Key = E?.Key + "|" + E?.Id, Gold = P.S.Gold, Hp = P.S.PartyHp, MaxHp = P.S.PartyMaxHp, Deck = P.S.Deck.ToList() }; removeTotal = 0; }

        void Event()
        {
            var f = P.Floor;
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue("event", out var bg) ? bg : "stage2_1", 0.4f);
            Stage.Show("event", root =>
            {
                BuildEvent(root, -1);
                // 창 크기가 바뀌면 지금 단계(대화 줄 · 선택 · 결과 — 상태는 필드에)로 다시 세운다. 고르기 창 · 카드 얻기 연출 중이면 끝난 뒤에
                Stage.WhenResized(root, () => { if (Stage.ModalLayer.childCount > 0 || CardGain.IsOpen || Stage.Current != "event") return Stage.Current != "event"; BuildEvent(root, -1); return true; });
            }, 1.2f);
        }

        void BuildEvent(RectTransform root, int picked)
        {
            Ui.Clear(root);
            var E = P.S.Event;
            if (E == null) { MapStep(); return; }
            if (E.Id == null && E.Choices.Count > 1) { EventFork(root, E); return; }
            var evd = P.Data.Event(E.Id);
            if (evd == null) { P.LeaveEvent(); MapStep(); return; }
            var size = Stage.Size;
            W.StatusBar(root, this, true, true, false, null, null);
            var style = EventStyleOf(evd.Id);
            if (style != null) { BuildStandingEvent(root, evd, E, picked, style); return; }

            // ── 장면 — 화면 전체. 왼쪽에 파티 미니미, 오른쪽에 등장 사도 · NPC(없으면 배경만) ──
            var scene = Ui.Rect("scene", root).Fill();
            scene.SetSiblingIndex(1);   // 머리 띠 밑
            var low = Ui.Img(scene, Theme.S("fade_down"), Theme.Night.A(0.45f), "low");   // 아래 선택지 · 결과가 읽히게만 옅게(결과 화면도 장면이 밝게 보인다)
            low.rectTransform.Band(0, size.y * 0.42f);
            float floorY = size.y * Theme.C(0.33f, 0.36f);   // 장면의 바닥선(아래 선택지 띠 위)
            var shadow = Ui.Img(scene, Theme.S("soft"), Color.black.A(0.4f), "ground"); shadow.rectTransform.At(0.5f, 0, 0, floorY - 30, size.x * 0.8f, 90);
            int pi = 0, pn = P.S.Party.Count;
            // 말할 수 있는 자리 — 이름 → (발 x, 머리 꼭대기 y) 장면(=화면) 좌표. 대사 말풍선이 그 머리 위에 선다
            var heads = new Dictionary<string, Vector2>();
            foreach (var k in P.S.Party)
            {
                var h = Roster.OfCore(k);
                if (h == null) continue;
                // 전투 화면과 같은 SD 전투 스파인 · 오른쪽을 본다 · 편성 순서 맨 앞이 오른쪽(적 쪽)
                float mh = size.y * 0.24f;
                var spot = Ui.Rect("party" + pi, scene).At(0, 0, Theme.Gutter + mh * 1.05f + (pn - 1 - pi) * mh * 0.9f, floorY + (pi % 2) * 8, 10, 10);   // 날개 · 큰 소품이 왼쪽 끝에 잘리지 않게 들여 세운다
                var psg = SceneHero.Make(spot, h, mh, true, pi * 0.4f);   // SD 전투 스파인(없으면 웹 렌더 · 얼굴) + 발밑 그림자 — 휴식과 같은 공용 부품
                // 머리 꼭대기 — 실제 메시 꼭대기(키 · 모자 · 날개가 사도마다 달라 짐작 높이로는 ▼ 가 머리에 묻힌다). 메시가 없으면 짐작
                float ph = floorY + (pi % 2) * 8 + mh * 1.05f;
                if (MeshTopY(psg, root) is float pt) ph = pt;
                if (h.ko != null) heads[h.ko] = new Vector2(Theme.Gutter + mh * 1.05f + (pn - 1 - pi) * mh * 0.9f + 5, ph);
                Tw.Pop(spot, 0.15f + pi * 0.07f, 0.7f, 0.35f);
                pi++;
            }
            string npc = evd.Npc;
            var hero = npc != null ? Roster.All.FirstOrDefault(h => h.ko == npc) : null;
            // 데이터에 대상이 없는 이벤트 — 표(event_style.json 의 target)에 적힌 사도
            if (hero == null && EventRaw(evd.Id) is Dictionary<string, object> raw && raw.TryGetValue("target", out var tg) && tg is string tk) hero = Roster.ByKey(tk);
            float sh = size.y - floorY - 110, sw = Mathf.Min(size.x * 0.34f, sh * 1.1f);
            var standArea = Ui.Rect("npc", scene); standArea.anchorMin = standArea.anchorMax = standArea.pivot = Vector2.zero;
            standArea.sizeDelta = new Vector2(sw, sh); standArea.anchoredPosition = new Vector2(size.x * 0.66f - sw / 2, floorY);
            Spine.Unity.SkeletonGraphic sg = null;
            Vector2? targetHead = null; bool targetFoe = false;
            // 오른쪽 대상 — 전투 스파인(왼쪽을 본다)이 있으면 그것, 없으면 스탠딩
            if (hero != null)
            {
                var bspot = Ui.Rect("battle", standArea).At(0.5f, 0, 0, 0, 10, 10);
                sg = SceneHero.Make(bspot, hero, size.y * 0.24f, false);   // 파티와 같은 크기 · 바닥선 · 파티 쪽(왼쪽)을 본다
                if (sg == null) Destroy(bspot.gameObject);
                else targetHead = new Vector2(size.x * 0.66f, floorY + size.y * 0.24f * 1.05f);   // SD 가 없으면 아래 스탠딩(움직이는) · NPC 스파인 차례 — 정지 대체는 버린다
            }
            if (sg == null && hero?.art != null) sg = SpineUi.Standing(standArea, hero.art, sw, sh, StandMode.Full, 0, 0);
            if (sg == null && npc != null && NpcSpine.TryGetValue(npc, out var key))
            {
                var spot = Ui.Rect("spot", standArea).At(0.5f, 0, 0, 0, 10, 10);
                sg = SpineUi.Make(spot, key, null, sh * 0.92f, "Idle_1", "Idle");
            }
            // 대상이 없는 이벤트 — 표(event_style.json 의 foe)에 적힌 몬스터(지문에 나오는 그 적 · 확실한 것만). 원작 적 SD 는 왼쪽(파티 쪽)을 본다.
            //   모습은 그 판 적 속성(Skin_<성격>) — 전투와 같다(적 리워크 2026-10-06: 몬스터에 성격 갈래가 없다). 판 속성을 모를 때만 표의 foeSkin
            if (sg == null && hero == null && npc == null && EventRaw(evd.Id) is Dictionary<string, object> fr && fr.TryGetValue("foe", out var fo) && fo is string foe)
            {
                var fspot = Ui.Rect("foe", standArea).At(0.5f, 0, 0, 0, 10, 10);
                string runNat = P.S?.EnemyNature ?? FoeNature, fskin = null;
                foreach (var (ko, en) in NatEn) if (ko == runNat) fskin = "Skin_" + char.ToUpperInvariant(en[0]) + en.Substring(1);
                if (fskin == null && fr.TryGetValue("foeSkin", out var fsk)) fskin = fsk as string;
                sg = SpineUi.Make(fspot, foe, fskin, size.y * 0.24f, "Idle", "idle", "Idle_1");
                targetFoe = sg != null;
            }
            if (sg != null) { SpineUi.ClampInto(sg, standArea, Stage.Root, 0.7f); Tw.FadeIn(sg, 0.5f, 0.05f); }
            else if (hero != null)
            {
                var full = CardArt.Standing(hero.art);
                if (full != null) { var im = Ui.Img(standArea, full, Color.white, "still"); im.preserveAspect = true; im.rectTransform.At(0.5f, 0, 0, 0, sw, sh); }
            }
            // 대상 이름표는 두지 않는다(2026-10-06 사용자: 조우하면 누구인지 알려 준다)

            // SD · 스탠딩 · NPC 스파인 대상 — 메시 꼭대기(ClampInto 뒤). 메시가 없으면 SD 는 짐작 높이, 스탠딩은 칸 높이의 0.9
            if (sg != null && !targetFoe)
            {
                if (MeshTopY(sg, root) is float top) targetHead = new Vector2(size.x * 0.66f, Mathf.Min(top, floorY + sh));
                else if (targetHead == null) targetHead = new Vector2(size.x * 0.66f, floorY + sh * 0.9f);
            }

            // 그림 — 대상이 있어야 하는 이벤트(npc · 표의 target · foe)인데 움직이는 그림 없이 정지 그림 · 빈 자리로 떨어졌는지(데모 단언)
            bool wantTarget = hero != null || npc != null || (EventRaw(evd.Id) is Dictionary<string, object> fr2 && fr2.ContainsKey("foe"));
            LastTargetArt = sg != null ? (targetFoe ? "foe" : "spine") : wantTarget ? (hero != null && CardArt.Standing(hero.art) != null ? "still" : "none") : "nobody";

            // ── 대화창 흐름(2026-10-06 사용자: 비주얼 노벨식) — 서술 · 대사 줄 → 선택 → 결과 줄 → 받은 것 · 떠나기 ──
            evHeads = new Dictionary<string, Vector2>(heads);
            string tName = targetHead != null && !targetFoe ? (npc ?? hero?.ko) : null;
            if (tName != null) evHeads[tName] = targetHead.Value;
            EventStage(root, scene, evd, E, picked, floorY, tName, false);
        }

        /// <summary>마지막 장면 대상 그림 — spine(움직이는 SD · 스탠딩 · NPC) · foe · still(정지 그림 대체) · none(대상이 있어야 하는데 그림 없음) · nobody(대상 없는 이벤트). 데모가 본다.</summary>
        public string LastTargetArt { get; private set; }

        /// <summary>스파인 메시 꼭대기의 높이(root 아래 끝에서) — 말하는 이 ▼ 를 그 머리 위에 세운다. 메시가 아직 없으면 null.</summary>
        static float? MeshTopY(Spine.Unity.SkeletonGraphic g, RectTransform root)
        {
            if (g == null) return null;
            g.UpdateMesh();
            var mesh = g.GetLastMesh();
            if (mesh == null || mesh.vertexCount == 0) return null;
            var top = root.InverseTransformPoint(g.rectTransform.TransformPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.max.y, 0)));
            return top.y - root.rect.yMin;
        }

        /// <summary>
        /// 글을 서술(따옴표 밖)과 대사(큰따옴표 "…" · “…” 안)로 나눈다 — 특별 이벤트가 서술은 지문 띠, 대사는 NPC 말풍선에.
        /// 따옴표 짝이 안 맞으면 통째로 서술(ok = false).
        /// </summary>
        public static (string narr, string speech, bool ok) SplitSay(string text)
        {
            if (string.IsNullOrEmpty(text)) return ("", "", true);
            var narr = new System.Text.StringBuilder(); var speech = new List<string>();
            var cur = new System.Text.StringBuilder(); bool inQ = false;
            foreach (char ch in text)
            {
                bool open = ch == '“' || (ch == '"' && !inQ), close = ch == '”' || (ch == '"' && inQ);
                if (!inQ && open) { inQ = true; cur.Clear(); continue; }
                if (inQ && close) { inQ = false; var sp = cur.ToString().Trim(); if (sp.Length > 0) speech.Add(sp); narr.Append(' '); continue; }
                if (inQ) cur.Append(ch); else narr.Append(ch);
            }
            if (inQ) return (text, "", false);
            string n = System.Text.RegularExpressions.Regex.Replace(narr.ToString(), @"\s{2,}", " ").Trim();
            return (n, string.Join(" ", speech), true);
        }

        // ── 선택지 — 아래에 가로로 나란한 반투명 카드(윗줄 이름 · 아랫줄 결과 칩) · 「떠난다」는 오른쪽 아래 둥근 단추 ──
        //   카드 높이는 글에 맞춰 늘어난다(이름 줄바꿈 · 칩 줄바꿈 — 말줄임 없음). 한 줄의 카드는 가장 높은 카드에 맞춘다.
        /// <summary>이벤트 글의 왼쪽 안쪽 여백 — 선택지 이름 · 결과 칸 머리 · 받은 것 줄이 모두 상자 왼쪽에서 이만큼 들어가 시작한다.</summary>
        public const float EvTextInset = 24;
        /// <summary>선택지 묶음 첫 줄의 왼쪽 끝 x · 너비(아래 왼쪽 기준) — 결과 칸이 같은 세로줄에서 시작하게(2026-10-06 사용자: 결과 글만 오른쪽 기준).</summary>
        public float LastEvLeft { get; private set; }
        (float x0, float rowW, float availW) ChoiceRow0(EventDef ev, bool column)
        {
            var size = Stage.Size;
            var opts = P.Options(ev);
            int nc = opts.Count(o => !o.Leave);
            float roundS = Theme.C(116, 96);
            float availW = size.x - Theme.Gutter * 2 - roundS - 30;
            int perRow = column ? 1 : nc <= 3 ? Mathf.Max(1, nc) : Mathf.CeilToInt(nc / 2f);
            float gap = 22, cw = column ? Mathf.Min(Theme.C(600, 520), size.x * 0.4f) : Mathf.Min(Theme.C(520, 440), (availW - gap * (perRow - 1)) / perRow);
            int inRow = Mathf.Max(1, Mathf.Min(perRow, nc));
            float rowW = inRow * cw + (inRow - 1) * gap;
            float x0 = column ? size.x * 0.56f : Theme.Gutter + (availW - rowW) / 2;
            return (x0, rowW, availW);
        }

        float EventChoices(RectTransform root, RectTransform scene, EventDef ev, int picked, float floorY, float sw, float sh, bool column = false)
        {
            LastEvLeft = ChoiceRow0(ev, column).x0;
            var size = Stage.Size;
            var opts = P.Options(ev);
            var cards = Enumerable.Range(0, opts.Count).Where(i => !opts[i].Leave).ToList();
            int leaveIdx = opts.FindIndex(o => o.Leave);
            float roundS = Theme.C(116, 96);
            float availW = size.x - Theme.Gutter * 2 - roundS - 30;
            int perRow = column ? 1 : cards.Count <= 3 ? Mathf.Max(1, cards.Count) : Mathf.CeilToInt(cards.Count / 2f);
            int rows = Mathf.CeilToInt(cards.Count / (float)perRow);
            float gap = 22, cw = column ? Mathf.Min(Theme.C(600, 520), size.x * 0.4f) : Mathf.Min(Theme.C(520, 440), (availW - gap * (perRow - 1)) / perRow);
            float minH = Theme.C(104, 90);
            float bottom = Theme.C(34, 20);
            var built = new List<(RectTransform rt, int row, int col, float h)>();
            for (int ci = 0; ci < cards.Count; ci++)
            {
                int idx = cards[ci];
                var o = opts[idx];
                var lockWhy = P.LockOf(o);
                bool on = picked == idx;
                var b = Btn.Make(root, null, BtnStyle.Ghost, () =>
                {
                    if (lockWhy != null) return;
                    if (picked == idx) { DecideEvent(root, picked); return; }
                    BuildEvent(root, idx);
                }, 0, "opt" + idx);
                b.Bg.sprite = Theme.Round; b.Bg.type = Image.Type.Sliced; b.SetColor(new Color(0.05f, 0.06f, 0.12f, on ? 0.9f : 0.78f));
                var rt = b.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero; rt.sizeDelta = new Vector2(cw, minH);
                var rim = Ui.Img(rt, Theme.Frame, on ? Theme.Gold : Color.white.A(0.22f), "rim"); rim.rectTransform.Fill();
                var tail = Ui.Img(rt, Theme.S("circle"), on ? Theme.Gold : Color.white.A(0.5f), "tail"); tail.rectTransform.At(0.5f, 0, 0, -4, 8, 8);
                if (!string.IsNullOrEmpty(o.Say))
                {
                    var bub = Ui.Img(rt, Theme.S("circle"), Theme.NavyPanel, "say"); bub.rectTransform.At(0, 1, -10, 14, 34, 34);
                    var bi = Ui.Img(bub.transform, Theme.S("ic_talk") ?? Theme.S("ic_info"), Theme.Gold, "ic"); bi.rectTransform.Fill(8, 8, 8, 8); bi.preserveAspect = true;   // 작은 금빛 「i」 는 「!」 로 읽혔다 — 말풍선(ic_talk)
                }
                float faceW = o.Hero != null ? 44 : 0, inner = cw - 24 - 18 - faceW;
                // 이름 — 줄바꿈(말줄임 없음)
                var nm = Ui.Title(rt, o.Label, Theme.FsLg, lockWhy != null ? Theme.Dim : Color.white, TextAlignmentOptions.TopLeft, "label");
                nm.textWrappingMode = TextWrappingModes.Normal; nm.overflowMode = TextOverflowModes.Overflow;
                float labelH = nm.GetPreferredValues(nm.text, inner, 0).y;
                nm.rectTransform.anchorMin = nm.rectTransform.anchorMax = nm.rectTransform.pivot = new Vector2(0, 1);
                nm.rectTransform.sizeDelta = new Vector2(inner, labelH); nm.rectTransform.anchoredPosition = new Vector2(EvTextInset, -14);
                // 결과 칩 — 흐르는 줄(넘치면 다음 줄로)
                var chips = Ui.Rect("chips", rt); chips.anchorMin = chips.anchorMax = chips.pivot = new Vector2(0, 1);
                chips.anchoredPosition = new Vector2(EvTextInset, -14 - labelH - 8);
                float chipsW = cw - 24 - 14, chipsH;
                if (lockWhy != null)
                {
                    var lk = Ui.Img(chips, Theme.S("ic_lock"), Theme.Bad, "lock"); lk.rectTransform.At(0, 1, 0, -2, 18, 18); lk.preserveAspect = true;
                    var lt = Ui.Text(chips, lockWhy, Theme.FsSm, Theme.Bad, TextAlignmentOptions.TopLeft, false, "why");
                    lt.textWrappingMode = TextWrappingModes.Normal; lt.overflowMode = TextOverflowModes.Overflow;
                    float lh = lt.GetPreferredValues(lt.text, chipsW - 24, 0).y;
                    lt.rectTransform.At(0, 1, 24, 0, chipsW - 24, lh);
                    chipsH = Mathf.Max(22, lh);
                    b.Interactable = false; b.Why = lockWhy;
                    b.Group().alpha = 0.6f;
                }
                else chipsH = OptionChips(chips, o, chipsW);
                chips.sizeDelta = new Vector2(chipsW, chipsH);
                float h = Mathf.Max(minH, 14 + labelH + 8 + chipsH + 18);
                if (o.Hero != null) { var face = W.Face(rt, Roster.OfCore(o.Hero[0]), 36); face.At(1, 1, -12, -10, 36, 36); }
                if (on)
                {
                    var hint = Ui.Text(rt, "한 번 더 누르면 결정합니다", Theme.FsCap, Theme.Gold, TextAlignmentOptions.Center, false, "hint"); hint.rectTransform.At(0.5f, 1, 0, 26, cw, 22); hint.Outline(0.3f);
                    hint.overflowMode = TextOverflowModes.Overflow;
                    Tw.Breathe(rt, 0.02f, 1.2f);
                    Stage.Hot["event.ok"] = b;
                }
                Stage.Hot["event.opt" + idx] = b;
                built.Add((rt, ci / perRow, ci % perRow, h));
            }
            // 줄마다 가장 높은 카드에 맞추고, 아래에서부터 쌓는다(세로 목록이면 화면 가운데쯤)
            var rowH = Enumerable.Range(0, rows).Select(r => built.Where(x => x.row == r).Select(x => x.h).DefaultIfEmpty(minH).Max()).ToList();
            float total = rowH.Sum() + 18 * (rows - 1);
            if (column) bottom = Mathf.Max(bottom, (size.y - 120 - total) / 2f);
            foreach (var (rt, r, c, _) in built)
            {
                int inRow = built.Count(x => x.row == r);
                float rowW = inRow * cw + (inRow - 1) * gap;
                float x0 = column ? size.x * 0.56f : Theme.Gutter + (availW - rowW) / 2;
                float y = bottom; for (int k = r + 1; k < rows; k++) y += rowH[k] + 18;
                rt.sizeDelta = new Vector2(cw, rowH[r]);
                rt.anchoredPosition = new Vector2(x0 + c * (cw + gap), y);
                Tw.Rise(rt, 0.15f + (r * perRow + c) * 0.06f, 24, 0.3f);
            }
            // 오른쪽 아래 둥근 단추 — 「떠난다」
            if (leaveIdx >= 0)
            {
                var lo = opts[leaveIdx];
                var lb = Btn.Icon(root, Theme.S("ic_back"), () => DecideEvent(root, leaveIdx), roundS, "leaveround");
                lb.GetComponent<RectTransform>().At(1, 0, -Theme.Gutter, bottom + 18, roundS, roundS);
                var ll = Ui.Title(root, lo.Label ?? "떠나기", Theme.FsSm, Theme.Ink, TextAlignmentOptions.Top, "leavelabel"); ll.rectTransform.At(1, 0, -Theme.Gutter - roundS / 2 + 80, bottom - 30, 160, 44);
                ll.textWrappingMode = TextWrappingModes.Normal; ll.overflowMode = TextOverflowModes.Overflow; ll.Outline(0.3f);
                Stage.Hot["event.leaveopt"] = lb;
            }
            return bottom + total;   // 선택지 묶음 위 끝(아래 기준)
        }

        // ── 특별 이벤트(Resources/RunUI/event_style.json — 겨우살이 축복 …): 파티 장면 없이 NPC 스탠딩 하나 + 말풍선 + 선택지(오른쪽 세로) ──
        static Dictionary<string, Dictionary<string, object>> eventStyles;
        static Dictionary<string, object> EventStyleOf(string id)
        {
            if (eventStyles == null)
            {
                eventStyles = new Dictionary<string, Dictionary<string, object>>();
                var ta = Resources.Load<TextAsset>("RunUI/event_style");
                if (ta != null && FitJson.Parse(ta.text) is Dictionary<string, object> root)
                    foreach (var kv in root) if (!kv.Key.StartsWith("_") && kv.Value is Dictionary<string, object> o) eventStyles[kv.Key] = o;
            }
            return id != null && eventStyles.TryGetValue(id, out var s) && (s.TryGetValue("style", out var st) ? st as string : null) == "standing" ? s : null;
        }

        static Dictionary<string, object> EventRaw(string id)
        {
            EventStyleOf(null);
            return id != null && eventStyles.TryGetValue(id, out var s) ? s : null;
        }

        void BuildStandingEvent(RectTransform root, EventDef evd, EventState E, int picked, Dictionary<string, object> style)
        {
            var size = Stage.Size;
            string S(string k) => style.TryGetValue(k, out var v) ? v as string : null;
            string npc = S("npc") ?? evd.Npc;
            // 축복 빛 — 스탠딩 뒤 부드러운 금빛 · 반짝임 몇 개
            var scene = Ui.Rect("scene", root).Fill(); scene.SetSiblingIndex(1);
            var veil = Ui.Img(scene, Theme.White, Theme.Night.A(0.35f), "veil"); veil.rectTransform.Fill();
            float cx = size.x * 0.5f;   // 가로 가운데(2026-10-06 사용자: 겨우살이를 중앙으로)
            var halo = Ui.Img(scene, Theme.S("soft"), Theme.Gold.A(0.28f), "halo"); halo.rectTransform.At(0, 0, cx - size.y * 0.5f, size.y * 0.08f, size.y, size.y);
            Tw.Breathe(halo.transform, 0.06f, 2.4f);
            for (int i = 0; i < 7; i++)
            {
                var sp = Ui.Img(scene, Theme.S("ic_spark"), Theme.Gold.A(0.8f), "spark");
                float a = i * 0.9f, r = size.y * (0.22f + 0.05f * (i % 3));
                sp.rectTransform.At(0, 0, cx + Mathf.Cos(a) * r, size.y * 0.5f + Mathf.Sin(a) * r * 0.8f, 22 + (i % 3) * 8, 22 + (i % 3) * 8);
                Tw.Breathe(sp.transform, 0.25f, 1.2f + i * 0.17f, i * 0.3f);
            }
            // 스탠딩 — 로비와 같은 규칙(화면 높이 안). 아래 대화창 · 선택지 위에서 머리 띠 아래까지(가리지 않게)
            float floorY = size.y * Theme.C(0.30f, 0.34f), topY = size.y - Theme.C(96, 62);
            float ah = topY - floorY, aw = Mathf.Min(size.x * 0.5f, ah * 1.1f);
            var area = Ui.Rect("stand", scene); area.anchorMin = area.anchorMax = area.pivot = Vector2.zero;
            area.sizeDelta = new Vector2(aw, ah); area.anchoredPosition = new Vector2(cx - aw / 2, floorY);
            Spine.Unity.SkeletonGraphic sg = null;
            var hero = S("hero") != null ? Roster.ByKey(S("hero")) : npc != null ? Roster.All.FirstOrDefault(h => h.ko == npc) : null;
            if (hero?.art != null) sg = SpineUi.Standing(area, hero.art, aw, ah * 0.97f, StandMode.Full, 0, 0);
            if (sg == null && S("spine") != null) { var spot = Ui.Rect("spot", area).At(0.5f, 0, 0, 0, 10, 10); sg = SpineUi.Make(spot, S("spine"), null, ah * 0.95f, "Idle_1", "Idle"); }
            LastTargetArt = sg != null ? "spine" : "none";
            if (sg != null)
            {
                SpineUi.ClampInto(sg, area, Stage.Root, ah / Mathf.Max(1, size.y)); Tw.FadeIn(sg, 0.6f, 0.05f);
                // 고른 뒤(결과) — 기뻐하는 동작 한 번(있으면) 뒤 다시 쉬기
                if (E.Phase != "choose")
                {
                    var happy = SpineUi.PickAnim(sg.Skeleton.Data, "Happy_1", "Happy", "Smile_1");
                    if (happy != null && happy.StartsWith("Happy") || happy == "Smile_1") { sg.AnimationState.SetAnimation(0, happy, false); sg.AnimationState.AddAnimation(0, SpineUi.PickAnim(sg.Skeleton.Data, "Idle_1", "Idle"), true, 0); }
                }
            }
            else
            {
                // 스탠딩이 없는 NPC(겨우살이) — 빛 문양
                var disc = Ui.Img(scene, Theme.S("circle"), Theme.NavyPanel.A(0.9f), "emblem"); disc.rectTransform.At(0, 0, cx - ah * 0.3f, floorY + ah * 0.2f, ah * 0.6f, ah * 0.6f);
                var ring = Ui.Img(disc.transform, Theme.S("ring"), Theme.Gold, "ring"); ring.rectTransform.Fill();
                var gl = Ui.Img(disc.transform, Theme.S(S("glow") ?? "ic_spark"), Theme.Gold, "glyph"); gl.rectTransform.Fill(size.y * 0.09f, size.y * 0.09f, size.y * 0.09f, size.y * 0.09f); gl.preserveAspect = true;
                Tw.Breathe(disc.transform, 0.05f, 2f);
            }
            evHeads = new Dictionary<string, Vector2>();
            if (npc != null) evHeads[npc] = new Vector2(cx, topY);
            EventStage(root, scene, evd, E, picked, floorY, npc, true);
        }

        void DecideEvent(RectTransform root, int picked)
        {
            if (picked < 0) return;
            var E = P.S.Event;
            evSnap = new EvSnap { Key = E?.Key + "|" + E?.Id, Gold = P.S.Gold, Hp = P.S.PartyHp, MaxHp = P.S.PartyMaxHp, Deck = P.S.Deck.ToList() };
            removeTotal = 0;
            var cs = CardSnap();
            var (fight, why) = P.Choose(picked);
            if (why != null) { Toast.Show(why); return; }
            GainCards(NewCards(cs), () =>   // 얻은 카드는 가운데에 크게 → (주인 없는 카드면) 사도 고르기 — 공용 CardGain
            {
                P.Save("event");
                if (fight) { FightStop(); return; }
                BuildEvent(root, -1);
            });
        }

        // 「지도 공개」 — 두 이야기 가운데 하나
        void EventFork(RectTransform root, EventState E)
        {
            W.StatusBar(root, this, true, true, false, "갈림길", "두 이야기 가운데 하나를 고릅니다");
            var row = Ui.Rect("pick", root).At(0.5f, 0.5f, 0, -20, Mathf.Min(1100, Stage.Size.x - 60), Theme.C(340, 300));
            Ui.Row(row, 24, TextAnchor.MiddleCenter, null, true, true);
            int i = 0;
            foreach (var id in E.Choices)
            {
                var ev = P.Data.Event(id);
                var b = Btn.Make(row, null, BtnStyle.Glass, () => { P.PickEvent(id); P.Save("event"); BuildEvent((RectTransform)root, -1); }, 0, id);
                var rt = b.GetComponent<RectTransform>();
                var ic = Ui.Img(rt, Theme.S("ic_question"), Theme.Gold, "ic"); ic.rectTransform.At(0.5f, 1, 0, -26, 44, 44); ic.preserveAspect = true;
                var t = Ui.Title(rt, ev?.Name ?? id, Theme.FsXl, Theme.Ink, TextAlignmentOptions.Top); t.rectTransform.Fill(24, 20, 24, 84);
                var s = Ui.Text(rt, ev?.Scene ?? "", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Top); s.rectTransform.Fill(28, 24, 28, 132);
                s.enableAutoSizing = true; s.fontSizeMin = 11; s.fontSizeMax = Theme.FsSm;
                Stage.Hot["event.fork" + i++] = b;
                Tw.Pop(rt, 0.1f * i, 0.9f, 0.35f);
            }
        }

        /// <summary>
        /// 선택지의 결과를 아이콘 + 수치 칩으로 흐르게 놓는다(넘치면 다음 줄 — 말줄임 없음). 도박은 「확률 · 칩」, 판정은 「판정 · 성공/실패 칩」, 싸움은 「전투 · 이기면 칩」.
        /// 돌려줌: 쓴 높이.
        /// </summary>
        float OptionChips(RectTransform box, EventOption o, float width)
        {
            float x = 0, y = 0, lineH = 28, gapX = 6, gapY = 6;
            void Put(RectTransform rt, float w)
            {
                if (x > 0 && x + w > width) { x = 0; y += lineH + gapY; }
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(Mathf.Min(w, width), lineH); rt.anchoredPosition = new Vector2(x, -y);
                x += Mathf.Min(w, width) + gapX;
            }
            void Lead(string text, Color c)
            {
                if (string.IsNullOrEmpty(text)) return;
                var t = Ui.Title(box, text, Theme.FsSm, c, TextAlignmentOptions.MidlineLeft, "lead");
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                Put(t.rectTransform, t.GetPreferredValues(text).x + 2);
            }
            void Chips(IEnumerable<Outcome> outs) { foreach (var x2 in outs ?? Enumerable.Empty<Outcome>()) { var (rt, w) = OutChip(box, x2); Put(rt, w); } }
            if (o.Leave) { Lead(P.OutOf(o).Count == 0 ? "아무 대가 없이 떠납니다" : "", Theme.Sub); Chips(P.OutOf(o)); }
            else if (o.Fight != null) { Lead($"전투 — {o.Fight.Name ?? "적"} · 이기면", Theme.Bad); Chips(o.Fight.Win); }
            else if (o.Judge != null)
            {
                var j = P.JudgeOf(o);
                Lead(j.Hp ? $"판정 HP {j.Value}%/{j.Need}%" : $"판정 {Roster.OfCore(j.Who)?.ko} 공격력 {j.Value}/{j.Need}", j.Pass ? Theme.Good : Theme.Bad);
                Chips(j.Pass ? o.Judge.Pass : o.Judge.Fail);
            }
            else if (o.Gamble != null)
            {
                if (o.Hidden) Lead("결과는 알 수 없습니다", Theme.Sub);
                else foreach (var g in o.Gamble) { Lead($"{Mathf.RoundToInt((float)g.P * 100)}%", Theme.Sky); Chips(g.Out); }
            }
            else
            {
                var outs = P.OutOf(o);
                if (outs.Count == 0) Lead("아무 대가도 없이", Theme.Sub); else Chips(outs);
            }
            return x == 0 && y == 0 ? lineH : y + lineH;
        }

        /// <summary>결과 하나 → 아이콘 · 수치 칩(손해는 붉게). 돌려줌: 칩과 그 폭.</summary>
        (RectTransform rt, float w) OutChip(RectTransform parent, Outcome x)
        {
            var (icon, text, bad) = OutLook(x);
            var bg = Ui.Img(parent, Theme.Pill, (bad ? Theme.Bad : Theme.NavyWell).A(bad ? 0.22f : 0.85f), "chip " + x.K);
            var ic = Ui.Img(bg.transform, icon, bad ? Theme.Bad : x.K == "gold" ? Color.white : Theme.Gold, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 0.5f, 8, 0, 18, 18);
            var t = Ui.Text(bg.transform, text, Theme.FsSm - 1, bad ? Theme.Bad : Theme.Ink, TextAlignmentOptions.MidlineLeft, false, "t");
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow; t.rectTransform.Fill(30, 0, 10, 0);
            float w = t.GetPreferredValues(text).x + 42;
            bg.Pref(w, 28);
            return (bg.rectTransform, w);
        }

        /// <summary>글 속 등급 낱말에 등급 색(Theme.GradeOf) — 「무기 (전설)」.</summary>
        static string GradeTint(string text, string grade) => string.IsNullOrEmpty(grade) || text == null ? text : text.Replace(grade, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.GradeOf(grade))}>{grade}</color>");

        (Sprite icon, string text, bool bad) OutLook(Outcome x)
        {
            string text = P.Text.Outcome(x);
            switch (x.K)
            {
                case "gold": return (Theme.Icon("gold") ?? Theme.S("ic_bag"), text.Replace("골드 ", ""), x.V < 0);
                case "hp": return (Theme.S("ic_heart"), text, x.V < 0);
                case "maxHp": return (Theme.S("ic_heart"), text, x.V < 0);
                case "remove": return (Theme.S("ic_trash"), text, false);
                case "dupe": return (Theme.S("ic_plus"), text, false);
                case "unique": case "gift": return (Theme.S("ic_deck"), text, false);
                case "equip": case "shopGift": return (W.SlotIcon(x.Slot), GradeTint(text, x.Grade), false);
                case "neutral": return (Theme.S("ic_deck"), GradeTint(text, x.Grade), false);
                case "flash": case "rewardFlash": case "shin": case "shinNow": case "shinPick": return (Theme.S("ic_spark"), text, false);
                case "noShin": return (Theme.S("ic_spark"), text, true);
                case "curse": case "mindBreak": return (Theme.S("ic_skull"), text, true);
                case "scout": return (Theme.S("ic_flag"), text, false);
                case "next": return (Theme.S("ic_swords"), text, x.Next != null && (x.Next.Weak > 0 || x.Next.HpCut > 0));
                default: return (Theme.S("ic_play"), text, false);
            }
        }

        // ── 결과 — 같은 장면. 위 띠 지문이 바뀌고, 아래에 받은 것(보상 화면 꼴 유리 줄 · 얻은 카드) · 오른쪽 아래 「떠나기」 ──
        void EventResult(RectTransform root, EventDef ev, float floorY, bool column = false)
        {
            var size = Stage.Size;
            var E = P.S.Event;
            float roundS = Theme.C(116, 96), bottom = Theme.C(30, 16);
            float listW = Mathf.Min(size.x - Theme.Gutter * 2 - roundS - 40, Theme.C(1500, 1400)), listH = Mathf.Min(floorY - bottom - 20, Theme.C(280, 230));
            var (cx0, crowW, cavail) = ChoiceRow0(ev, column);
            float boxX = cx0;
            float rightEdge = Theme.Gutter + cavail;
            listW = Mathf.Min(rightEdge - boxX, Mathf.Max(crowW, Mathf.Min(listW, rightEdge - boxX)));
            LastEvLeft = boxX;
            var panel = Ui.Img(root, Theme.Round, new Color(0.05f, 0.06f, 0.12f, 0.82f), "gains");
            panel.rectTransform.At(0, 0, Theme.Gutter, bottom, listW, listH);
            var prim = Ui.Img(panel.rectTransform, Theme.Frame, Theme.Gold.A(0.6f), "rim"); prim.rectTransform.Fill();
            var head = Ui.Title(panel.rectTransform, $"「{E.Label}」  <size=70%><color={Theme.SubTag}>받은 것</color></size>", Theme.FsLg, Theme.Gold, TextAlignmentOptions.MidlineLeft, "label");
            head.rectTransform.Band(1, 40, EvTextInset, 22, -8); head.textWrappingMode = TextWrappingModes.NoWrap; head.overflowMode = TextOverflowModes.Overflow;
            head.enableAutoSizing = true; head.fontSizeMin = Theme.FsBody; head.fontSizeMax = Theme.FsLg;
            var area = Ui.Rect("area", panel.rectTransform).Fill(EvTextInset, 12, 18, 52);   // 왼쪽 여백 = 선택지 이름과 같은 값
            var content = Ui.Scroll(area, out _);
            Ui.Col(content, 8, TextAnchor.UpperLeft, new RectOffset(0, 6, 0, 4), true, false);
            // 받은 것 — 유리 줄(보상 화면처럼 오른쪽에 동그란 아이콘)을 두 줄씩 나란히
            float rowH = Theme.C(62, 56), colW = Mathf.Min(listW - 36 - 6, Mathf.Min(420, Mathf.Max(Theme.C(260, 200), (listW - 60) / 3)));   // 좁은 칸(특별 이벤트 · 폰)에서도 글이 들어갈 너비
            int cols = Mathf.Max(1, (int)((listW - 40) / (colW + 12)));
            RectTransform rowsHost = null;
            int n = 0;
            void Row(Sprite icon, Color tint, string title, string subText, bool bad = false)
            {
                if (n % cols == 0)
                {
                    rowsHost = Ui.Rect("rows", content); rowsHost.Pref(-1, rowH);
                    Ui.Row(rowsHost, 12, TextAnchor.MiddleLeft, null, false, true);
                }
                var g = Ui.Img(rowsHost, Theme.Glass, Color.white.A(0.92f), "gain"); g.Pref(colW, rowH);
                var rt = g.rectTransform;
                var t = Ui.Title(rt, title + (subText != null ? $"\n<size=70%><color={Theme.SubTag}>{subText}</color></size>" : ""), Theme.FsMd, bad ? Theme.Bad : Theme.Ink, TextAlignmentOptions.MidlineLeft);
                t.rectTransform.Fill(10 + 48 + 12, 4, 14, 4);   // 왼쪽 정렬 — 아이콘이 왼쪽, 글은 그 오른쪽에서(다른 이벤트 글과 같은 쪽) t.lineSpacing = -6; t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
                t.enableAutoSizing = true; t.fontSizeMin = Theme.FsSm; t.fontSizeMax = Theme.FsMd;
                var disc = Ui.Img(rt, Theme.S("circle"), Theme.NavyWell.A(0.9f), "disc"); disc.rectTransform.At(0, 0.5f, 10, 0, 48, 48);
                var ic = Ui.Img(disc.transform, icon, tint, "ic"); ic.rectTransform.Fill(11, 11, 11, 11); ic.preserveAspect = true;
                Tw.Pop(rt, 0.15f + n * 0.06f, 0.85f, 0.3f);   // 줄 배치 안이라 자리를 옮기는 연출(Rise)은 쓰지 않는다
                n++;
            }
            // 카드 · 장비는 글 한 줄 — 얻은 카드 그림은 화면 가운데 크게 띄우기 연출이 보여 준다(2026-10-06 사용자: 「카드 이미지 없애고 글자만」).
            //   카드 이름은 밑줄 낱말(CardTerms) — 누르면(PC 는 올리면) 작은 카드 판. 글이 길면 줄바꿈하고 줄 높이를 글에 맞춘다.
            float lineW = listW - 36 - 6, lineIc = Theme.C(30, 26);
            void Line(Sprite icon, Color tint, string text, string cardId = null, string cardName = null, bool bad = false)
            {
                var row = Ui.Rect("gainline", content);
                var t = Ui.Text(row, "", Theme.FsBody, bad ? Theme.Bad : Theme.Ink, TextAlignmentOptions.MidlineLeft, false, "text");
                t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
                List<CardTerms.Term> terms = null;
                if (cardId != null && !string.IsNullOrEmpty(cardName))
                    terms = new List<CardTerms.Term> { new CardTerms.Term { Name = cardName, CardId = cardId, Kind = "카드", Bad = bad } };
                if (terms != null) { CardTerms.Prepare(t); t.text = CardTerms.Mark(text, terms); TermPop.Attach(this, t, terms); }
                else t.text = text;
                float th = t.GetPreferredValues(t.text, lineW - lineIc - 12, 0).y;
                float h = Mathf.Max(lineIc + 6, th + 6);
                row.Pref(-1, h);
                t.rectTransform.Fill(lineIc + 12, 0, 0, 0);
                if (icon != null) { var ic = Ui.Img(row, icon, tint, "ic"); ic.preserveAspect = true; ic.rectTransform.At(0, 1, 0, -3, lineIc, lineIc); }   // 첫 줄 높이에 맞춘다
                Tw.FadeIn(t, 0.25f, 0.2f + n * 0.05f);
                n++;
            }
            string CardLine(string id, string tail)
            {
                var v = P.View(id); var c = P.Data.Card(id);
                string name = v?.Name ?? c?.Name ?? id;
                var bits = new List<string>();
                if (c?.Grade != null) bits.Add($"<color=#{ColorUtility.ToHtmlStringRGB(Theme.GradeOf(c.Grade))}>{c.Grade}</color>");
                if (P.S.Flash.TryGetValue(id, out var fl) && fl > 0) bits.Add($"신탁 {fl}");
                if (P.S.Shin.ContainsKey(id)) bits.Add("축복");
                var own = Core.GameData.OwnerOf(id); if (own != null) bits.Add($"{Roster.OfCore(own)?.ko ?? own} 덱");
                return $"「{name}」" + (bits.Count > 0 ? $" <size=80%><color={Theme.SubTag}>({string.Join(" · ", bits)})</color></size>" : "") + tail;
            }
            var snap = evSnap != null && evSnap.Key == E.Key + "|" + E.Id ? evSnap : null;
            var gotCards = new List<string>();
            if (snap != null)
            {
                int dg = P.S.Gold - snap.Gold;
                if (dg != 0) Row(Theme.Icon("gold") ?? Theme.S("ic_bag"), Color.white, $"골드 <color={(dg > 0 ? Theme.GoldTag : Theme.BadTag)}>{(dg > 0 ? "+" : "")}{dg}</color>", $"지금 {P.S.Gold}", dg < 0);
                int dm = P.S.PartyMaxHp - snap.MaxHp;
                if (dm != 0) Row(Theme.S("ic_heart"), dm > 0 ? Theme.Good : Theme.Bad, $"최대 HP {(dm > 0 ? "+" : "")}{dm}", null, dm < 0);
                int dh = P.S.PartyHp - snap.Hp;
                if (dh != 0) Row(Theme.S("ic_heart"), dh > 0 ? Theme.Good : Theme.Bad, $"파티 HP {(dh > 0 ? "+" : "")}{dh}", $"{P.S.PartyHp} / {P.S.PartyMaxHp}", dh < 0);
                var before = snap.Deck.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                var after = P.S.Deck.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
                foreach (var kv in after) for (int i = 0; i < kv.Value - (before.TryGetValue(kv.Key, out var b) ? b : 0); i++) gotCards.Add(kv.Key);
                foreach (var kv in before)
                {
                    int lost = kv.Value - (after.TryGetValue(kv.Key, out var a) ? a : 0);
                    for (int i = 0; i < lost; i++) Line(Theme.S("ic_trash"), Theme.Sub, $"카드 {CardLine(kv.Key, " — 덱에서 빠짐")}", kv.Key, P.View(kv.Key)?.Name);
                }
                foreach (var id in gotCards)
                {
                    var v = P.View(id);
                    bool curse = v != null && (v.IsCurse || v.IsStatus);
                    Line(Theme.S(curse ? "ic_skull" : "ic_deck"), curse ? Theme.Bad : Theme.Gold, $"{(curse ? "골칫거리" : "카드")} {CardLine(id, " — 덱에")}", id, v?.Name, curse);
                }
            }
            // 받은 장비(아직 가방에 — 이 화면 뒤에 끼기 · 팔기를 묻는다) — 등급 색
            foreach (var eid in P.S.Bag)
            {
                var e = P.Data.Equip(eid);
                var gc = Theme.GradeOf(e?.Grade);
                Line(CardArt.Equip(eid) ?? W.SlotIcon(e?.Slot), CardArt.Equip(eid) != null ? Color.white : gc, $"장비 「{e?.Name ?? eid}」 <size=80%><color={Theme.SubTag}>(<color=#{ColorUtility.ToHtmlStringRGB(gc)}>{e?.Grade}</color> · {e?.Slot ?? "장비"})</color></size> — 가방에");
            }
            // 기록 — 위 줄에 없는 것(신탁 · 축복 · 다음 전투 …)까지
            var logs = E.Log.ToList();
            if (logs.Count == 0 && n == 0) logs.Add("아무 일도 일어나지 않았습니다");
            if (logs.Count > 0)
            {
                var t = Ui.Text(content, string.Join("   ", logs.Select(l => "· " + l)), Theme.FsSm, Theme.Sub, TextAlignmentOptions.TopLeft, false, "log");
                t.textWrappingMode = TextWrappingModes.Normal; t.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            // 상자 높이 — 내용(줄 · 기록 · 카드 실제 높이)에 맞춘다. 장면 바닥선을 넘어 위 띠 밑까지는 늘고, 그보다 많으면 칸 안에서 스크롤
            // 가로는 떠나기 단추 자리(오른쪽 roundS + 40)를 빼고 잡았으니 겹치지 않는다
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            float need = content.rect.height + 52 + 14;
            float maxH = Mathf.Max(listH, size.y - Theme.C(300, 250) - bottom);   // 위 띠(지문 · 이벤트 이름) 밑까지
            float boxH = Mathf.Clamp(need, Theme.C(120, 104), maxH);
            // 왼쪽 끝 = 선택지 묶음 첫 줄의 왼쪽 끝(같은 세로줄). 너비는 선택지 줄 너비 이상 · 떠나기 단추 앞까지
            panel.rectTransform.At(0, 0, boxX, bottom, listW, boxH);
            Tw.Rise(panel.rectTransform, 0.05f, 30, 0.35f);
            var p = E.Pending.FirstOrDefault();
            if (p != null)
            {
                // 신탁 — 휴식 담당의 공용 신탁 연출(OracleReveal). 그 밖의 고를 것은 PendingPick 창
                if (p.K == "flash" && p.Offer != null)
                {
                    var opts = P.Run.FlashOptions(p.Offer);
                    void Fin(object v) { var cs = CardSnap(); var why = P.Resolve(v); if (why != null) { Toast.Show(why); return; } GainCards(NewCards(cs), () => { P.Save("event"); BuildEvent(root, -1); }); }
                    OracleReveal.Show(this, new RevealOpts
                    {
                        BaseCardId = p.Offer.CardId,
                        Picks = RevealPick.Of(P.Data, p.Offer.CardId, opts),
                        Hot = "pending",
                        Cancel = true,
                        Cancelled = () => Fin(null),
                        Done = i => Fin(opts[i].N),
                    });
                    return;
                }
                if (p.K == "grace") { GracePick(root, p); return; }   // 은총 — 사도 고르기 → 그 사도의 남은 고유 카드(Flow.EventGrace)
                PendingPick(root, p);
                return;
            }
            if (P.S.Bag.Count > 0) { Settle(() => BuildEvent(root, -1)); return; }
            var lb = Btn.Icon(root, Theme.S("ic_play"), () => { evSnap = null; P.LeaveEvent(); MapStep(); }, roundS, "leave");
            lb.GetComponent<RectTransform>().At(1, 0, -Theme.Gutter, bottom + 18, roundS, roundS);
            lb.Bg.color = Theme.Gold; lb.SetColor(Theme.Gold);
            var ic2 = lb.transform.Find("icon")?.GetComponent<Image>(); if (ic2 != null) ic2.color = Theme.Brown;
            var ll = Ui.Title(root, "떠나기", Theme.FsMd, Theme.Gold, TextAlignmentOptions.Center, "leavelabel"); ll.rectTransform.At(1, 0, -Theme.Gutter - roundS / 2 + 70, bottom - 10, 140, 26); ll.Outline(0.3f);
            Stage.Hot["event.leave"] = lb;
            Tw.Breathe(lb.transform, 0.04f, 1.2f);
        }

        /// <summary>이벤트 카드 제거 머리 — 몇 장 가운데 몇 번째인지(제거 n 은 한 장씩 차례로 고른다 · 무작위 제거 없음 · 취소 없음 — 선택지의 대가).</summary>
        string RemoveTitle(Pending p)
        {
            var pend = P.S.Event?.Pending ?? new List<Pending>();
            int rest = pend.TakeWhile(x => x.K == "remove").Count();
            int total = Mathf.Max(rest, removeTotal);
            removeTotal = total;
            string what = p.Basic ? "시작 카드" : "카드";
            return total > 1 ? $"덱에서 뺄 {what}를 고릅니다 ({total - rest + 1}/{total})" : $"덱에서 뺄 {what}를 고릅니다";
        }
        int removeTotal;

        void PendingPick(RectTransform root, Pending p)
        {
            if (p.K != "remove") removeTotal = 0;
            string title = p.K switch
            {
                "remove" => RemoveTitle(p),
                "dupe" => "복제할 고유 카드를 고릅니다",
                "card" => (p.Label ?? "카드") + " — 하나를 고릅니다",
                "flash" => $"「{P.Data.Card(p.Offer?.CardId)?.Name}」 — 신탁 하나를 고릅니다",
                "shinPick" => "축복을 얹을 카드를 고릅니다",
                "gambleChoice" => "하나를 고릅니다",
                _ => "고릅니다",
            };
            float mw = Mathf.Min(Theme.Compact ? 1240 : Mathf.Max(1240, 8 * (170 + 12) + 80), Stage.Size.x - 40);   // 넓은 화면은 카드 고르기 한 줄 6~8칸
            var (right, closeM, _) = Stage.ModalBox("pending", mw, Mathf.Min(680, Stage.Size.y - 24), title, "이벤트에서 얻은 것 — 하나를 고릅니다", false, null, 0, false);
            Ui.Col(right, Theme.Gap, TextAnchor.UpperLeft, new RectOffset(4, 4, 0, 4));
            var area = Ui.Rect("pick", right); area.Pref(-1, 200, -1, 1);
            var content = Ui.Scroll(area, out _);
            void Done(object v)
            {
                var cs = CardSnap();
                var why = P.Resolve(v);
                if (why != null) { Toast.Show(why); return; }
                closeM();
                GainCards(NewCards(cs), () => { P.Save("event"); BuildEvent(root, -1); });
            }
            if (p.K == "flash" && p.Offer != null)
            {
                Destroy(content.GetComponentInParent<ScrollRect>());
                var opts = P.Run.FlashOptions(p.Offer);
                OracleRow(content, p.Offer.CardId, opts, Theme.C(210, 170), idx => Done(opts[idx].N), "pending");
                content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one; content.offsetMin = content.offsetMax = Vector2.zero;
                Destroy(content.GetComponent<ContentSizeFitter>());
            }
            else if (p.K == "gambleChoice")
            {
                Ui.Col(content, 10, TextAnchor.UpperLeft, new RectOffset(4, 4, 4, 4));
                for (int i = 0; i < p.Options.Count; i++)
                {
                    int idx = i;
                    var b = Btn.Make(content, null, BtnStyle.Glass, () => Done(idx), 0, "g" + i);
                    b.Pref(-1, 64);
                    var row = Ui.Rect("chips", b.transform).Fill(20, 10, 20, 10);
                    Ui.Row(row, 6, TextAnchor.MiddleLeft, null, false, false);
                    foreach (var x in p.Options[i]) OutChip(row, x);   // 줄 배치가 칩 폭(Pref)을 쓴다
                    Stage.Hot["pending" + i] = b;
                }
            }
            else
            {
                List<string> ids = p.K switch
                {
                    "remove" => P.S.Deck.Where(id => (!p.Basic || P.Run.IsBasic(id)) && !P.View(id).IsTaboo).ToList(),   // 같은 카드도 한 장마다 한 칸 · 고를 수 있는 것만(시작 카드만 · 금기 제외)
                    "dupe" => P.S.Deck.Where(P.DupeOk).ToList(),
                    "card" => p.Cards ?? new List<string>(),
                    "shinPick" => P.ShinAble(p.Kind),
                    _ => new List<string>(),
                };
                CardGroups(content, ids, 170, GridCols(mw - 80, 170, 12, Theme.Compact ? 3 : 6, 8), (c, id, i) =>   // 상점 제거 고르기와 같은 카드(170) — 칸 수는 창 너비에 맞춘다(폰 창이 좁아 6칸이 넘쳤다)
                {
                    var b = c.gameObject.AddComponent<Btn>();
                    b.OnClick = () => Done(id);
                    Stage.Hot["pending" + i] = b;
                });
            }
            if (p.K != "remove" && p.K != "dupe" && p.K != "gambleChoice")
            {
                var skip = Btn.Make(right, "받지 않기", BtnStyle.PillDark, () => Done(null), Theme.FsMd);
                skip.Pref(-1, 52);
                Stage.Hot["pending.skip"] = skip;
            }
        }

        /// <summary>선택지 종류 → 동그라미 아이콘(싸움 · 도박 · 판정 · 사도 · 그냥).</summary>
        static string OptIcon(EventOption o) => o.Fight != null ? "ic_swords" : o.Gamble != null ? "ic_question" : o.Judge != null ? "ic_shield" : o.Hero != null ? "ic_heart" : "ic_play";
    }

    /// <summary>글을 한 글자씩 보인다(초당 cps 자) — Finish() 로 바로 다 보이고, 다 보이면 Done.</summary>
    public class TypeOn : MonoBehaviour
    {
        public float Cps = 42;
        public Action Done;
        TextMeshProUGUI t; float shown; bool done;
        void Start() { t = GetComponent<TextMeshProUGUI>(); t.maxVisibleCharacters = 0; if (Settings.ReduceMotion) Finish(); }
        void Update()
        {
            if (done || t == null) return;
            shown += Time.unscaledDeltaTime * Cps;
            t.maxVisibleCharacters = (int)shown;
            if (shown >= t.textInfo.characterCount && t.textInfo.characterCount > 0) Finish();
        }
        /// <summary>새 글로 처음부터 한 글자씩(대화창 다음 줄).</summary>
        public void Restart() { if (t == null) t = GetComponent<TextMeshProUGUI>(); shown = 0; done = false; if (t != null) t.maxVisibleCharacters = 0; if (Settings.ReduceMotion) Finish(); }
        public bool IsDone => done;
        public void Finish()
        {
            if (done) return;
            done = true;
            if (t == null) t = GetComponent<TextMeshProUGUI>();
            if (t != null) t.maxVisibleCharacters = 99999;
            Done?.Invoke();
        }
    }
}
