using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 끝 화면 — 이긴 판 · 진 판. 카제나 「세이브 데이터 수집 완료」 꼴(Docs/참고_카제나_UI.md §결과 화면, 2026-10-21):
    //   위에 큰 제목 · 어디서 끝났는지 · 골드 · 덱 · HP 한 줄, 가운데 사도 셋의 세로 카드(스탠딩 + 이름 + 그 사도의 판 요약),
    //   아래 — 성장 보상(성장 켬일 때만, 아래 띠 위 왼쪽), 아래 띠 오른쪽 「전투 기록」(지나온 전투 창) · 넓은 금 「로비로」.
    //   기록 보내기 · 보상 · 저장은 Flow.End(판 끝)가 이미 했다 — 여기는 보이기만.
    public partial class Flow
    {
        void EndScreen(string kind)
        {
            bool win = kind == "clear";
            var f = P.Floor;
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue(win ? "boss" : "fight", out var bg) ? bg : "stage3_3", win ? 0.4f : 0.8f);
            Stage.Show("end_" + kind, root =>
            {
                var glow = Ui.Img(root, Theme.S("soft"), (win ? Theme.Gold : Theme.Bad).A(0.35f), "glow");
                glow.rectTransform.At(0.5f, 1, 0, 40, 1300, 520);
                Tw.Pulse(glow, 0.2f, 0.45f, 2.2f);
                var t = Ui.Title(root, win ? "모험 완주!" : "쓰러졌다", Theme.C(Theme.FsDisplay, 64), Color.white, TextAlignmentOptions.Center);
                t.name = "end.title";
                t.rectTransform.At(0.5f, 1, 0, Theme.C(-22, -8), 1200, Theme.C(100, 78));
                var c = win ? Theme.Gold : Theme.Bad;
                t.colorGradient = new VertexGradient(Color.white, Color.white, c, c);
                t.Outline(0.12f, new Color(0.15f, 0.05f, 0, 1));
                Tw.Pop(t.rectTransform, 0.1f, 1.6f, 0.6f);
                var v = P.VillageDef;
                var s = Ui.Text(root, (win ? $"{v.Name} 두 층을 모두 넘었다 — {string.Join(" → ", v.Floors.Select(x => x.Name))}"
                    : $"{v.Name} {P.S.Floor + 1}층 {f.Name} · {(P.IsBoss ? "보스" : $"{P.S.Step}번째 칸")}에서 멈췄다") + TierTag(P.S.Tier), Theme.FsMd, Theme.Ink, TextAlignmentOptions.Center);
                s.rectTransform.At(0.5f, 1, 0, Theme.C(-122, -86), 1200, 32);
                s.Outline(0.2f);
                var nums = Ui.Text(root, $"<color={Theme.GoldTag}>골드</color> {P.S.Gold:N0}  ·  <color={Theme.GoldTag}>덱</color> {P.S.Deck.Count}장  ·  <color={Theme.GoldTag}>HP</color> {P.S.PartyHp:N0}/{P.S.PartyMaxHp:N0}  ·  <color={Theme.GoldTag}>전투</color> {P.S.Hist.Count}번", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
                nums.rectTransform.At(0.5f, 1, 0, Theme.C(-156, -116), 1200, 28);
                nums.Outline(0.2f);

                // 아래 띠 — 오른쪽 「전투 기록」 · 「로비로」
                float footY = Theme.C(26, 14), footH = Theme.BtnMainH, goW = Theme.C(380, 340);
                var foot = Ui.Rect("endfoot", root).Band(0, footH, Theme.Gutter, Theme.Gutter, footY);
                var go = Btn.Make(foot, "로비로", BtnStyle.PillGold, Lobby, Theme.FsLg);
                go.GetComponent<RectTransform>().At(1, 0.5f, 0, 0, goW, footH);
                Stage.Hot["end.lobby"] = go;
                Tw.Rise(go.GetComponent<RectTransform>(), 0.6f, 20);
                var hist = NavyPill(foot, "ic_swords", $"전투 기록 <color={Theme.SubTag}>{P.S.Hist.Count}</color>", EndHist, "hist", Theme.C(230, 210), 52);
                hist.GetComponent<RectTransform>().At(1, 0.5f, -goW - 14, 0, Theme.C(230, 210), 52);
                Stage.Hot["end.hist"] = hist;
                bool prog = EndProg(root, win);   // 성장 보상(성장 켬일 때만 — Flow.Prog.cs) — 아래 띠 위 왼쪽

                // 가운데 — 사도 셋의 세로 카드(화면 왼쪽부터 자리 1 · 2 · 3 — 편성 화면과 같은 차례)
                float top = Theme.C(196, 146), bottom = footY + footH + 16 + (prog ? Theme.C(92, 84) + 12 : 0);
                float ch = Stage.Size.y - top - bottom;
                float gap = Theme.C(30, 22);
                float cw = Mathf.Min(Theme.C(340, 320), ch * 0.62f, (Stage.Size.x - Theme.Gutter * 2 - gap * 2) / 3f);
                var keys = P.S.Party.AsEnumerable().Reverse().ToList();
                var stats = EndStats(keys);
                int best = stats.Played.Values.DefaultIfEmpty(0).Max();
                if (stats.Played.Values.Count(x => x == best) > 1) best = 0;   // 왕관은 혼자 가장 많이 낸 사도에게만(같으면 없음)
                float tallest = StandingFit.Tallest(keys.Select(k => Roster.OfCore(k)?.art));
                for (int i = 0; i < keys.Count; i++)
                {
                    var slot = Ui.Rect("endhero " + keys[i], root).At(0.5f, 1, (i - (keys.Count - 1) / 2f) * (cw + gap), -top, cw, ch);
                    EndHeroCard(slot, keys[i], cw, ch, i, tallest, win, stats, best > 0 && stats.Played.TryGetValue(keys[i], out var pl) && pl == best);
                    Tw.Rise(slot, 0.25f + 0.08f * i, 30, 0.45f);
                }
                if (win) StartCoroutine(Confetti(root));
            }, 1.6f);
        }

        /// <summary>사도별 판 요약 — 낸 카드(전투 녹화가 있을 때만) · 덱 장수 · 고유 카드 장수.</summary>
        class EndStat { public bool HasLog; public Dictionary<string, int> Played = new(), Deck = new(), Uniq = new(); }

        EndStat EndStats(List<string> keys)
        {
            var st = new EndStat();
            string Owner(string id) => Core.GameData.OwnerOf(id) ?? P.Data.Card(Core.GameData.BaseId(id))?.Hero;
            foreach (var k in keys) { st.Played[k] = 0; st.Deck[k] = 0; st.Uniq[k] = 0; }
            foreach (var r in P.S.Hist.Where(r => r.Log != null && r.Log.Count > 0))
            {
                st.HasLog = true;
                foreach (var id in r.Log.Where(x => x.C != null).SelectMany(x => x.C)) { var o = Owner(id); if (o != null && st.Played.ContainsKey(o)) st.Played[o]++; }
            }
            if (!st.HasLog) st.Played.Clear();
            foreach (var id in P.S.Deck)
            {
                var o = Owner(id); if (o == null || !st.Deck.ContainsKey(o)) continue;
                st.Deck[o]++;
                if (P.Data.Card(Core.GameData.BaseId(id))?.Unique == true) st.Uniq[o]++;
            }
            return st;
        }

        /// <summary>판 끝 사도 카드 — 위 스탠딩(편성 큰 카드와 같은 무릎 구도), 아래 이름 · 성격 · 역할 · 판 요약 · (완주) 사도 저장. top = 이번 판 카드를 가장 많이 낸 사도(왕관).</summary>
        void EndHeroCard(RectTransform slot, string key, float w, float h, int i, float tallest, bool win, EndStat st, bool top)
        {
            var hero = Roster.OfCore(key);
            var nc = NatureCol(hero);
            NavyBox(slot, "panel").Fill();
            float infoH = Theme.C(168, 146), artH = h - infoH;
            var glow = Ui.Img(slot, Theme.S("fade_top"), nc.A(0.4f), "glow"); glow.rectTransform.Band(1, artH, 3, 3, -3);
            var mask = Ui.Img(slot, Theme.Round, Color.white, "mask"); mask.rectTransform.Band(1, artH - 3, 3, 3, -3);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            if (hero != null) HeroArt(mask.rectTransform, hero, w - 6, artH - 3, 0.72f, Mathf.Min(artH * 0.46f, 300), i * 0.37f, null, true, false, tallest);
            var shade = Ui.Img(slot, Theme.S("fade_down"), Theme.Night.A(0.85f), "shade"); shade.rectTransform.Band(1, 70, 3, 3, -(artH - 70));
            var rule = Ui.Img(slot, Theme.White, nc.A(0.7f), "rule"); rule.rectTransform.Band(1, 2, 3, 3, -artH);
            if (top) { var cr = Ui.Img(slot, Theme.S("ic_crown"), Theme.Gold, "crown"); cr.preserveAspect = true; cr.rectTransform.At(0, 1, 12, -12, 34, 34); }

            var info = Ui.Rect("info", slot).Band(0, infoH - 12, 16, 14, 8);
            bool save = win && SlotStore.Wait != null && SlotStore.Wait.Slots.ContainsKey(key);   // 완주 — 사도 저장(프론티어, Flow.HeroSave.cs)
            var nm = Ui.Title(info, hero?.ko ?? key, Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineLeft);
            nm.rectTransform.At(0, 1, 0, 0, w - 30 - (save ? 128 : 0), 38);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 14; nm.fontSizeMax = Theme.FsLg;
            if (save) { var sr = Ui.Rect("saverow", info).At(1, 1, 0, 1, 124, 38); EndSave(sr, key); }
            if (hero != null)
            {
                var sub = Ui.Text(info, $"<color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(hero.nature))}>{hero.nature}</color> · {hero.role}", Theme.FsCap + 1, Theme.Sub, TextAlignmentOptions.MidlineLeft);
                sub.rectTransform.At(0, 1, 0, -38, w - 30, 22);
            }
            var gear = P.GearOf(key).Values.Select(id => P.Data.Equip(id)?.Name).Where(x => x != null).ToList();
            var lines = new List<string>();
            if (st.HasLog) lines.Add($"쓴 카드 <color={Theme.GoldTag}>{st.Played[key]}</color>장");
            lines.Add($"덱 <color={Theme.GoldTag}>{st.Deck[key]}</color>장 · 고유 <color={Theme.GoldTag}>{st.Uniq[key]}</color>장");
            lines.Add(gear.Count > 0 ? $"아티팩트 <color={Theme.GoldTag}>{gear.Count}</color> — {string.Join(" · ", gear)}" : "아티팩트 없음");
            var body = Ui.Text(info, string.Join("\n", lines), Theme.FsSm, Theme.Ink, TextAlignmentOptions.TopLeft);
            body.rectTransform.Fill(0, 0, 0, 64);
            body.textWrappingMode = TextWrappingModes.NoWrap; body.overflowMode = TextOverflowModes.Ellipsis;
            body.enableAutoSizing = true; body.fontSizeMin = 11; body.fontSizeMax = Theme.FsSm; body.lineSpacing = 6;
        }

        /// <summary>지나온 전투 창(판 끝 「전투 기록」) — 층 · 종류 · 적 · 턴 · HP · 승패, 처음부터 끝까지.</summary>
        void EndHist()
        {
            var hist = P.S.Hist;
            var (body, _, _) = Stage.ModalBox("endhist", Mathf.Min(1180, Stage.Size.x - 48), Mathf.Min(Theme.C(720, 640), Stage.Size.y - 24), "전투 기록", $"지나온 전투 {hist.Count}번");
            var area = Ui.Rect("area", body).Fill(28, 18, 18, 20);
            var content = Ui.Scroll(area, out var sr);
            ScrollBar(area, sr);
            Ui.Col(content, 4, TextAnchor.UpperLeft, new RectOffset(0, 14, 0, 8), true, false);
            foreach (var r in hist)
            {
                string kindKo = r.Kind == "boss" ? "보스" : r.Kind == "elite" ? "엘리트" : r.Kind == "event" ? "이벤트" : "일반";
                var line = Ui.Text(content, $"<color=#F2CF7A>{r.Floor}층</color>  {kindKo} · {string.Join(" · ", r.Foes.Select(id => P.Data.Enemy(id)?.Name ?? id).Distinct())}  <color=#A7B1CC>{r.Turns}턴 · HP {r.HpBefore}→{r.HpAfter}</color>  {(r.Result == "win" ? "<color=#6EE0A0>승리</color>" : "<color=#FF7A86>패배</color>")}", Theme.FsSm, Theme.Ink);
                line.Pref(-1, Theme.C(34, 30));
                line.textWrappingMode = TextWrappingModes.NoWrap; line.overflowMode = TextOverflowModes.Ellipsis;
            }
            if (hist.Count == 0) EmptyNote(content, "전투가 없었다", null, EmptyH);
        }

        System.Collections.IEnumerator Confetti(RectTransform root)
        {
            var cols = new[] { Theme.Gold, Theme.Sky, Theme.Good, new Color(1f, 0.6f, 0.8f), Color.white };
            for (int n = 0; n < 90 && root; n++)
            {
                var p = Ui.Img(root, Theme.Round, cols[n % cols.Length], "c");
                var rt = p.rectTransform;
                rt.At(0.5f, 1, Random.Range(-800f, 800f), 20, Random.Range(8, 14), Random.Range(14, 22));
                var start = rt.anchoredPosition;
                float fall = Random.Range(700f, 1000f), sway = Random.Range(-80f, 80f), spin = Random.Range(-500f, 500f);
                Tw.Run(rt, Random.Range(2.2f, 3.6f), k =>
                {
                    if (!rt) return;
                    rt.anchoredPosition = start + new Vector2(sway * Mathf.Sin(k * 6), -fall * k);
                    rt.localRotation = Quaternion.Euler(0, 0, spin * k);
                    p.color = p.color.A(1 - k * k);
                }, Tw.Linear, 0, () => { if (rt) Destroy(rt.gameObject); });
                yield return new WaitForSecondsRealtime(0.025f);
            }
        }
    }
}
