using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 이벤트 — 웹판 ui.js eventScreen. 왼쪽에 NPC(스탠딩 스파인) · 장면 말풍선, 오른쪽에 선택지(결과 · 못 고르는 까닭 · 판정 미리보기).
    // 고르고 「이것으로 합니다」 → 결과(말 · 기록) → 고를 것(카드 빼기 · 복제 · 카드 · 신탁 · 축복 · 도박 고르기)을 하나씩 → 떠납니다.
    // 싸움이 걸리면 싸움 칸으로 넘어갔다가 이기면 결과로 돌아온다.
    public partial class Flow
    {
        static readonly Dictionary<string, string> NpcSpine = new Dictionary<string, string>
        {
            ["시스트"] = "st_sist", ["앨리스"] = "st_alice", ["쥬비"] = "st_jubee", ["골디"] = "st_goldy", ["에르핀"] = "st_erpin",
        };

        void Event()
        {
            var f = P.Floor;
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue("event", out var bg) ? bg : "stage2_1", 0.45f);
            Stage.Show("event", root => BuildEvent(root, -1), 1.2f);
        }

        void BuildEvent(RectTransform root, int picked)
        {
            Ui.Clear(root);
            var E = P.S.Event;
            if (E == null) { MapStep(); return; }
            // 「지도 공개」 — 둘 가운데 하나를 고른다
            if (E.Id == null && E.Choices.Count > 1)
            {
                W.Head(root, "갈림길", "두 이야기 가운데 하나를 고릅니다");
                var row = Ui.Rect("pick", root).At(0.5f, 0.5f, 0, 0, 1000, 300);
                Ui.Row(row, 24, TextAnchor.MiddleCenter, null, true, true);
                foreach (var id in E.Choices)
                {
                    var ev = P.Data.Event(id);
                    var b = Btn.Make(row, null, BtnStyle.Cell, () => { P.PickEvent(id); P.Save("event"); BuildEvent(root, -1); }, 0, id);
                    var t = Ui.Title(b.transform, ev?.Name ?? id, 32, Theme.Ink, TextAlignmentOptions.Top); t.rectTransform.Fill(20, 20, 20, 30);
                    var s = Ui.Text(b.transform, ev?.Scene ?? "", 17, Theme.Sub, TextAlignmentOptions.Bottom); s.rectTransform.Fill(24, 24, 24, 90);
                }
                return;
            }
            var evd = P.Data.Event(E.Id);
            if (evd == null) { P.LeaveEvent(); MapStep(); return; }

            W.StatusBar(root, this, true, true, false, evd.Name, $"이벤트 · {P.Floor.Name}" + (npc0(evd) != null ? $" · {npc0(evd)}" : ""));
            float optH = Theme.C(150, 132);

            // 장면 — 가운데에 NPC(스탠딩), 왼쪽에 파티(미니미). 카제나 이벤트처럼 인물이 배경 속에 선다
            string npc = evd.Npc;
            var spineKey = npc != null && NpcSpine.TryGetValue(npc, out var sk) ? sk : null;
            var hero = npc != null ? Roster.All.FirstOrDefault(h => h.ko == npc) : null;
            var scene = Ui.Rect("scene", root).Fill();
            scene.SetSiblingIndex(1);   // 머리 띠(HUD) 밑에 — 장면의 위 그늘이 HUD 를 덮지 않게
            var topShade = Ui.Img(scene, Theme.S("fade_top"), new Color(1, 1, 1, 0.95f), "topshade");
            topShade.rectTransform.Band(1, 300);
            var shade = Ui.Img(scene, Theme.S("soft"), new Color(0, 0, 0, 0.5f), "shade");
            shade.rectTransform.At(0.5f, 0, 0, 170, 1500, 220);
            var stand = Ui.Rect("npc", scene).At(0.5f, 0, 300, 175, 10, 10);
            var sg = spineKey != null ? SpineUi.Make(stand, spineKey, null, 600, "Idle_1", "Idle") : null;
            if (sg == null && hero?.Icon != null)
            {
                var im = Ui.Img(stand, hero.Icon, Color.white, "still"); im.rectTransform.At(0.5f, 0, 0, 0, 300, 300); im.preserveAspect = true;
            }
            if (sg != null) Tw.FadeIn(sg, 0.6f, 0.1f);
            int pi = 0;
            foreach (var k in P.S.Party)
            {
                var h = Roster.OfCore(k);
                var spot = Ui.Rect("mini" + pi, scene).At(0.5f, 0, -420 + pi * 120, 190 + (pi % 2) * 14, 10, 10);
                var g = SpineUi.Make(spot, "minimi", h.MiniSkin, 150, "Idle", "idle");
                if (g == null) { var fc = W.Face(spot, h, 90); fc.At(0.5f, 0, 0, 0, 90, 90); }
                else g.AnimationState.Update(pi * 0.4f);
                Tw.Pop(spot, 0.2f + pi * 0.08f, 0.6f, 0.4f);
                pi++;
            }
            if (npc != null)
            {
                var plate = Ui.Img(scene, Theme.S("pill_dark", 46), Color.white, "plate");
                plate.rectTransform.At(0.5f, 0, 300, 190, 220, 44);
                var pn = Ui.Title(plate.transform, $"{npc}  <size=70%><color=#A7B1CC>{(hero != null ? hero.race : "")}</color></size>", 20, Theme.Ink, TextAlignmentOptions.Center); pn.rectTransform.Fill();
            }

            // 위 — 이야기(머리 띠 아래 유리판)
            string sceneLine = E.Phase == "choose" ? evd.Scene : (E.Say ?? evd.Scene);
            W.Narration(root, (npc != null && E.Phase != "choose" ? $"<color={Theme.GoldTag}>{npc}</color>  " : "") + sceneLine, -104, Theme.C(980, 1000));

            // 아래 — 선택지(가로 카드) 또는 결과
            var bottom = Ui.Rect("bottom", root);
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(0.5f, 0);
            bottom.offsetMin = new Vector2(Theme.Gutter, Theme.Gutter); bottom.offsetMax = new Vector2(-Theme.Gutter, Theme.Gutter + optH);

            if (E.Phase == "choose") EventChoices(root, bottom, evd, picked);
            else EventResult(root, bottom, evd);
        }

        void EventChoices(RectTransform root, RectTransform bottom, EventDef ev, int picked)
        {
            var opts = P.Options(ev);
            var row = Ui.Rect("opts", bottom).Fill(0, 0, 118, 0);
            Ui.Row(row, Theme.Gap, TextAnchor.LowerCenter, null, false, false);
            float optH = Theme.C(150, 132);
            int i = 0;
            foreach (var o in opts)
            {
                int idx = i++;
                var lockWhy = P.LockOf(o);
                var outs = P.OutOf(o);
                string desc;
                if (o.Gamble != null) desc = string.Join(" / ", o.Gamble.Select(g => $"{Mathf.RoundToInt((float)g.P * 100)}% {P.Text.Outcomes(g.Out)}"));
                else if (o.Fight != null) desc = $"싸움 — {o.Fight.Name ?? "적"} · 이기면 {P.Text.Outcomes(o.Fight.Win)}";
                else if (o.Judge != null)
                {
                    var j = P.JudgeOf(o);
                    desc = (j.Hp ? $"[판정] 파티 HP {j.Value}% (≥{j.Need}%)" : $"[판정] {Roster.OfCore(j.Who).ko} 공격력 {j.Value} (≥{j.Need})") + $" — {(j.Pass ? "<color=#6EE0A0>성공</color>" : "<color=#FF7A86>실패</color>")}: {P.Text.Outcomes(j.Pass ? o.Judge.Pass : o.Judge.Fail)}";
                }
                else desc = outs.Count == 0 ? "아무 대가도 없이" : P.Text.Outcomes(outs);
                if (o.Hero != null) desc = $"<color=#FF9AC8>[{string.Join(" · ", o.Hero.Select(k => Roster.OfCore(k).ko))}]</color> " + desc;
                if (lockWhy != null) desc = $"<color=#FF7A86>{lockWhy}</color>";
                var b = W.Option(row, o.Label, desc, () => { if (lockWhy == null) BuildEvent(root, idx); }, optH, Theme.S(OptIcon(o)), "opt" + idx);
                b.Pref(330, -1, 1);
                if (picked == idx) W.Select(b, true);
                if (o.Hero != null)
                {
                    var face = W.Face(b.transform, Roster.OfCore(o.Hero[0]), 34);
                    face.At(1, 1, -10, -10, 34, 34);
                }
                if (lockWhy != null) { b.Interactable = false; b.Why = lockWhy; }
                Stage.Hot["event.opt" + idx] = b;
                Tw.Pop(b.GetComponent<RectTransform>(), 0.3f + idx * 0.07f, 0.9f, 0.35f);
            }
            // 오른쪽 — 정하기(둥근 금빛)
            var ok = Btn.Make(bottom, null, BtnStyle.PillGold, () =>
            {
                var (fight, why) = P.Choose(picked);
                if (why != null) { Toast.Show(why); return; }
                P.Save("event");
                if (fight) { FightStop(); return; }
                BuildEvent(root, -1);
            }, 0, "ok");
            ok.GetComponent<RectTransform>().At(1, 0, -6, optH - 96, 96, 96);
            ok.Bg.sprite = Theme.S("circle");
            ok.Bg.type = UnityEngine.UI.Image.Type.Simple;
            ok.SetColor(Theme.Gold);
            var ck = Ui.Img(ok.transform, Theme.S("ic_check"), Theme.Brown, "ck"); ck.rectTransform.Fill(22, 22, 22, 22); ck.preserveAspect = true;
            var okl = Ui.Title(ok.transform, "정합니다", Theme.FsSm, Theme.Ink, TextAlignmentOptions.Center); okl.rectTransform.At(0.5f, 0, 0, -26, 120, 22); okl.Outline(0.28f);
            ok.Interactable = picked >= 0;
            ok.Why = "선택지를 먼저 고르세요";
            if (picked >= 0) Tw.Breathe(ok.transform, 0.05f, 1.1f);
            Stage.Hot["event.ok"] = ok;
        }

        void EventResult(RectTransform root, RectTransform bottom, EventDef ev)
        {
            var E = P.S.Event;
            var box = Ui.Img(bottom, Theme.Glass, Color.white, "log");
            box.rectTransform.Fill(0, 0, 310, 0);
            Ui.Col(box.rectTransform, 4, TextAnchor.UpperLeft, new RectOffset(24, 24, 14, 12));
            Ui.Title(box.transform, $"「{E.Label}」", Theme.FsLg, Theme.Gold).Pref(-1, 32);
            foreach (var line in E.Log.DefaultIfEmpty("아무 일도 일어나지 않았습니다"))
                Ui.Text(box.transform, "· " + line, Theme.FsBody, Theme.Ink).Pref(-1, 26);
            Tw.Pop(box.rectTransform, 0.2f, 0.95f, 0.35f);
            var p = E.Pending.FirstOrDefault();
            if (p != null) { PendingPick(root, p); return; }
            if (P.S.Bag.Count > 0) { Settle(() => BuildEvent(root, -1)); return; }
            var leave = Btn.Make(bottom, "떠납니다", BtnStyle.PillGold, () => { P.LeaveEvent(); MapStep(); }, Theme.FsLg);
            leave.GetComponent<RectTransform>().At(1, 0.5f, 0, 0, 280, 66);
            Stage.Hot["event.leave"] = leave;
            Tw.Pop(leave.GetComponent<RectTransform>(), 0.4f, 0.9f, 0.35f);
        }

        void PendingPick(RectTransform root, Pending p)
        {
            string title = p.K switch
            {
                "remove" => "덱에서 뺄 카드를 고르세요",
                "dupe" => "복제할 고유 카드를 고르세요",
                "card" => (p.Label ?? "카드") + " — 하나를 고르세요",
                "flash" => $"「{P.Data.Card(p.Offer?.CardId)?.Name}」 — 신탁 하나를 고르세요",
                "shinPick" => "축복을 얹을 카드를 고르세요",
                "gambleChoice" => "하나를 고르세요",
                _ => "고르세요",
            };
            var (right, closeM, _) = Stage.ModalBox("pending", 1240, 680, title, "이벤트에서 얻은 것 — 하나를 고릅니다", false, null, 0, false);
            Ui.Col(right, Theme.Gap, TextAnchor.UpperLeft, new RectOffset(4, 4, 0, 4));
            var area = Ui.Rect("pick", right); area.Pref(-1, 200, -1, 1);
            var content = Ui.Scroll(area, out _);
            void Done(object v)
            {
                var why = P.Resolve(v);
                if (why != null) { Toast.Show(why); return; }
                closeM();
                P.Save("event");
                BuildEvent(root, -1);
            }
            if (p.K == "flash" && p.Offer != null)
            {
                // 신탁 — 코어가 고른 후보 셋(+ 축복, Run.FlashOptions)을 신탁을 얹은 카드 모습으로(보상의 빛났던 카드와 같은 줄)
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
                    var b = Btn.Make(content, P.Text.Outcomes(p.Options[i]), BtnStyle.Cell, () => Done(idx), Theme.FsMd, "g" + i);
                    b.Pref(-1, 64);
                    Stage.Hot["pending" + i] = b;
                }
            }
            else
            {
                List<string> ids = p.K switch
                {
                    "remove" => P.S.Deck.Distinct().ToList(),
                    "dupe" => P.S.Deck.Distinct().Where(P.DupeOk).ToList(),
                    "card" => p.Cards ?? new List<string>(),
                    "shinPick" => P.ShinAble(p.Kind),
                    _ => new List<string>(),
                };
                // 사도별 묶음(기본 → 고유) · 상태 · 저주 · 교주 카드 순 — CardOrder
                CardGroups(content, ids, 170, 6, false, (c, id, i) =>
                {
                    var b = c.gameObject.AddComponent<Btn>();
                    b.OnClick = () => Done(id);
                    Stage.Hot["pending" + i] = b;
                });
            }
            if (p.K != "remove" && p.K != "dupe" && p.K != "gambleChoice")
            {
                var skip = Btn.Make(right, "받지 않습니다", BtnStyle.PillDark, () => Done(null), Theme.FsMd);
                skip.Pref(-1, 52);
                Stage.Hot["pending.skip"] = skip;
            }
        }

        static string npc0(EventDef ev) => ev?.Npc;

        /// <summary>선택지 종류 → 동그라미 아이콘(싸움 · 도박 · 판정 · 그냥).</summary>
        static string OptIcon(EventOption o) => o.Fight != null ? "ic_swords" : o.Gamble != null ? "ic_question" : o.Judge != null ? "ic_shield" : o.Hero != null ? "ic_heart" : "ic_play";
    }
}
