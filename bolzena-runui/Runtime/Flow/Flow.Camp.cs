using System.Linq;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 캠프 — 웹판 ui.js campScreen. 모닥불 둘레에 파티(미니미), 오른쪽에 쉬기 · 수련 가운데 하나, 그 아래 장비 · (휴식+상점이면) 골디의 좌판.
    // 웹판보다: 모닥불이 일렁이고 불티가 오르며, 쉬면 파티 HP 막대가 차오른다.
    public partial class Flow
    {
        void CampScreen(string kind)
        {
            bool withShop = kind == "campshop";
            var f = P.Floor;
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue("fight", out var bg) ? bg : "stage3_2", 0.85f);
            Stage.Show("camp", root => BuildCamp(root, kind, withShop), 1.2f);
        }

        void BuildCamp(RectTransform root, string kind, bool withShop)
        {
            Ui.Clear(root);
            string used = P.CampChoice;
            W.StatusBar(root, this, true, true, false, withShop ? "휴식 · 골디의 좌판" : "휴식", $"{P.Floor.Name} — 모닥불 곁에서 한숨 돌립니다 · 쉬기 · 수련 가운데 하나");
            float optH = Theme.C(150, 132);

            // 가운데 — 모닥불과 파티
            var scene = Ui.Rect("fire", root).Fill();
            scene.SetSiblingIndex(1);   // 머리 띠(HUD) 밑에
            var warm = Ui.Img(scene, Theme.S("soft"), new Color(1f, 0.55f, 0.2f, 0.55f), "warm");
            warm.rectTransform.At(0.5f, 0, 0, 40, 1300, 760);
            Tw.Pulse(warm, 0.38f, 0.6f, 0.35f);
            var fire = Ui.Img(scene, Theme.S("ic_fire"), new Color(1f, 0.75f, 0.35f), "flame");
            fire.rectTransform.At(0.5f, 0, 0, 300, 110, 110);
            Tw.Breathe(fire.transform, 0.08f, 0.45f);
            var core = Ui.Img(scene, Theme.S("soft"), new Color(1f, 0.9f, 0.5f, 0.8f), "core");
            core.rectTransform.At(0.5f, 0, 0, 300, 200, 160);
            Tw.Pulse(core, 0.5f, 0.9f, 0.27f);
            StartCoroutine(Embers(scene, 320));
            Vector2[] spots = { new Vector2(-280, 270), new Vector2(-150, 400), new Vector2(240, 270) };
            int i = 0;
            foreach (var k in P.S.Party)
            {
                var h = Roster.OfCore(k);
                var spot = Ui.Rect("hero" + i, scene).At(0.5f, 0, spots[i].x, spots[i].y, 10, 10);
                var g = SpineUi.Make(spot, "minimi", h.MiniSkin, 160, "Idle", "idle");
                if (g == null) { var face = W.Face(spot, h, 100); face.At(0.5f, 0, 0, 0, 100, 100); }
                else g.AnimationState.Update(i * 0.5f);
                if (spots[i].x > 0 && g != null) g.transform.localScale = new Vector3(-g.transform.localScale.x, g.transform.localScale.y, 1);
                Tw.Pop(spot, 0.2f + i * 0.1f, 0.5f, 0.4f);
                i++;
            }
            if (withShop)
            {
                var gs = Ui.Rect("goldy", scene).At(0.5f, 0, 520, Theme.C(230, 196), 10, 10);
                var gg = SpineUi.Make(gs, "st_goldy", null, Theme.C(420, 350), "Idle_1", "Idle");
                if (gg != null) Tw.FadeIn(gg, 0.5f, 0.2f);
            }
            var hpBox = Ui.Img(scene, Theme.S("pill_dark", 46), Color.white, "hp");
            hpBox.rectTransform.At(0.5f, 0, 0, optH + 44, 440, 46);
            var hl = Ui.Title(hpBox.transform, "파티 HP", Theme.FsSm, Theme.Sub); hl.rectTransform.At(0, 0.5f, 22, 0, 80, 30);
            var (hpRt, setHp) = W.HpBar(hpBox.transform, 320, 22);
            hpRt.At(1, 0.5f, -14, 0, 320, 22);
            setHp(P.S.PartyHp, P.S.PartyMaxHp);

            // 아래 — 고르기(가로 카드). 쉬기 · 수련 가운데 하나만, 장비 · 좌판은 선택을 쓰지 않는다
            var bottom = Ui.Rect("bottom", root);
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(0.5f, 0);
            bottom.offsetMin = new Vector2(Theme.Gutter, Theme.Gutter); bottom.offsetMax = new Vector2(-320, Theme.Gutter + optH);
            Ui.Row(bottom, Theme.Gap, TextAnchor.LowerLeft, null, false, false);
            int heal = P.CampHeal;
            var rest = W.Option(bottom, "쉬기", used == "rest" ? "<color=#6EE0A0>푹 쉬었습니다</color>" : heal > 0 ? $"파티 HP 를 최대의 30% 채웁니다  <color=#6EE0A0>+{heal} HP</color>" : "지금은 찰 HP 가 없습니다", () =>
            {
                int before = P.S.PartyHp;
                var why = P.CampRest();
                if (why != null) { Toast.Show(why); return; }
                Sfx.Play("heal");
                setHp(P.S.PartyHp, P.S.PartyMaxHp);
                Toast.Show($"푹 쉬었습니다 — HP {before:N0} → {P.S.PartyHp:N0}");
                P.Save("camp", kind);
                Tw.After(0.7f, () => { if (root) BuildCamp(root, kind, withShop); });
            }, optH, Theme.S("ic_moon"), "rest");
            rest.Pref(300, -1, 1);
            rest.Interactable = used == "" && heal > 0;
            rest.Why = used != "" ? "이번 휴식에서는 이미 골랐습니다" : "지금은 찰 HP 가 없습니다";
            if (used == "rest") W.Select(rest, true);
            Stage.Hot["camp.rest"] = rest;

            var train = P.S.Camp?.Train;
            var tcard = train != null ? P.Data.Card(train.CardId) : null;
            var tr = W.Option(bottom, "수련", used == "train" ? "<color=#6EE0A0>손을 익혔습니다</color>" : tcard != null ? $"「{tcard.Name}」 에 신탁 하나를 붙입니다 — 셋 가운데 고릅니다" : "신탁을 붙일 고유 카드가 없습니다", () => TrainPick(root, kind, withShop), optH, Theme.S("ic_spark"), "train");
            tr.Pref(300, -1, 1);
            tr.Interactable = used == "" && tcard != null;
            tr.Why = used != "" ? "이번 휴식에서는 이미 골랐습니다" : "신탁을 붙일 고유 카드가 없습니다";
            if (used == "train") W.Select(tr, true);
            Stage.Hot["camp.trainopen"] = tr;

            var gear = W.Option(bottom, "장비", "낀 장비 보기 — 선택을 쓰지 않습니다", GearView, optH, Theme.S("ic_sword"), "gear");
            gear.Pref(260, -1, 1);
            Stage.Hot["camp.gear"] = gear;
            if (withShop)
            {
                var shop = W.Option(bottom, "골디의 좌판", "「어서 오세요, 고객님!」 카드 · 장비 · 카드 제거 — 들러도 휴식 선택은 그대로", () => Shop(kind), optH, Theme.S("ic_bag"), "shop");
                shop.Pref(320, -1, 1);
                Stage.Hot["camp.shop"] = shop;
            }
            int c = 0;
            foreach (Transform t in bottom) Tw.Pop((RectTransform)t, 0.25f + c++ * 0.07f, 0.9f, 0.35f);

            var leave = Btn.Make(root, "길을 떠납니다", BtnStyle.PillGold, LeaveCamp, Theme.FsLg);
            leave.GetComponent<RectTransform>().At(1, 0, -Theme.Gutter, Theme.Gutter + (optH - 66) / 2, 272, 66);
            Stage.Hot["camp.leave"] = leave;
        }

        void TrainPick(RectTransform root, string kind, bool withShop)
        {
            var train = P.S.Camp?.Train;
            var tcard = train != null ? P.Data.Card(train.CardId) : null;
            if (tcard == null) return;
            var (row, close, foot) = Stage.ModalBox("train", 1180, 580, $"수련 — 「{tcard.Name}」 에 붙일 신탁", "셋 가운데 하나 · 고르면 이번 휴식의 선택을 씁니다", true, null, 76);
            Ui.Row(row, Theme.Gap + 6, TextAnchor.MiddleCenter, new RectOffset(8, 8, 6, 6), false, true);
            var cardHold = Ui.Rect("base", row); cardHold.Pref(200, -1);
            var bc = W.Card(cardHold, this, train.CardId, 200); bc.At(0.5f, 0.5f, 0, 0, 200, 280);
            int j = 0;
            foreach (var n in train.Picks)
            {
                var o = tcard.Oracles[n - 1];
                var ob = W.Option(row, o.Name, P.Text.Oracle(tcard, o), () =>
                {
                    var why = P.CampTrain(n);
                    if (why != null) { Toast.Show(why); return; }
                    close();
                    Toast.Show($"「{tcard.Name}」 — 신탁 「{o.Name}」");
                    P.Save("camp", kind);
                    BuildCamp(root, kind, withShop);
                }, 300, Theme.S("ic_spark"), "oracle" + n);
                ob.Pref(270, -1, 0);
                Stage.Hot["camp.train" + j++] = ob;
                Tw.Pop(ob.GetComponent<RectTransform>(), 0.1f + j * 0.08f, 0.85f, 0.35f);
            }
            var x = Btn.Make(foot, "그만둡니다", BtnStyle.PillDark, close, Theme.FsMd);
            x.GetComponent<RectTransform>().At(1, 0.5f, -8, 0, 220, 52);
        }

        Image CampCard(RectTransform parent, Sprite icon, string title, string desc, string badge, bool can, bool done, System.Action go)
        {
            Image bg;
            if (go != null)
            {
                var b = Btn.Make(parent, null, done ? BtnStyle.Cell : BtnStyle.Dark, go, 0, title);
                b.Interactable = can;
                if (!can) b.Why = done ? "이미 골랐습니다" : desc;
                bg = b.Bg;
            }
            else bg = Ui.Img(parent, done ? Theme.CellOn : Theme.S("btn_dark", 26), Color.white, title, true);
            bg.Pref(-1, 132);
            var disc = Ui.Img(bg.transform, Theme.S("circle"), new Color(0.1f, 0.08f, 0.15f), "disc");
            disc.rectTransform.At(0, 1, 22, -24, 64, 64);
            var ring = Ui.Img(disc.transform, Theme.S("ring"), Theme.Gold.A(0.8f), "ring"); ring.rectTransform.Fill();
            var ic = Ui.Img(disc.transform, icon, Theme.Gold, "ic"); ic.rectTransform.Fill(16, 16, 16, 16);
            var t = Ui.Title(bg.transform, title, 32, Theme.Ink, TextAlignmentOptions.TopLeft); t.rectTransform.At(0, 1, 104, -18, 400, 40);
            var d = Ui.Text(bg.transform, desc, 17, Theme.Sub, TextAlignmentOptions.TopLeft); d.rectTransform.At(0, 1, 106, -62, 410, 26);
            if (badge != null)
            {
                var (bb, bt) = Ui.Chip(bg.transform, null, badge, 34, done ? new Color(0.2f, 0.45f, 0.3f, 0.95f) : new Color(0.25f, 0.2f, 0.1f, 0.95f), null, 18);
                bb.rectTransform.At(1, 1, -18, -20, 200, 34);
                bt.color = done ? Theme.Good : Theme.Gold;
                var fit = bb.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            return bg;
        }

        System.Collections.IEnumerator Embers(RectTransform scene, float y0 = 170)
        {
            while (scene != null)
            {
                if (!Settings.ReduceMotion)
                {
                    var e = Ui.Img(scene, Theme.S("soft"), new Color(1f, 0.7f, 0.3f, 0.9f), "ember");
                    var rt = e.rectTransform;
                    float s = Random.Range(6f, 14f);
                    rt.At(0.5f, 0, Random.Range(-40f, 40f), y0, s, s);
                    var start = rt.anchoredPosition;
                    float drift = Random.Range(-60f, 60f), rise = Random.Range(220f, 420f);
                    Tw.Run(rt, Random.Range(1.4f, 2.4f), k =>
                    {
                        if (!rt) return;
                        rt.anchoredPosition = start + new Vector2(drift * k + Mathf.Sin(k * 9) * 8, rise * k);
                        e.color = new Color(1f, 0.7f, 0.3f, 0.9f * (1 - k));
                    }, Tw.Linear, 0, () => { if (rt) Destroy(rt.gameObject); });
                }
                yield return new WaitForSecondsRealtime(0.12f);
            }
        }
    }
}
