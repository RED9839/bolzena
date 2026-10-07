using System.Linq;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 캠프 — 웹판 ui.js campScreen. 모닥불 둘레에 파티(SD 전투 스파인), 오른쪽에 쉬기 · 수련 가운데 하나, 그 아래 장비 · (휴식+상점이면) 골디의 좌판.
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
            W.StatusBar(root, this, true, false, false, withShop ? "휴식 · 골디의 좌판" : "휴식", $"{P.Floor.Name} — 모닥불 곁에서 한숨 돌립니다 · 쉬기 · 수련 가운데 하나를 꼭 고릅니다");
            // 휴식은 쉬기 · 수련 가운데 하나를 반드시 고른다(2026-10-06 사용자) — 고르기 전에는 출발 단추가 없고 뒤로 키도 듣지 않는다(Stage.OnBack 없음).
            //   수련할 카드가 없거나(고유 · 교주 카드에 붙일 신탁이 없음) 정신 붕괴면 쉬기만 고를 수 있다. 상점(좌판)은 고르기와 상관없이 드나든다.
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
            // 파티 — 전투 화면과 같은 SD 전투 스파인(SceneHero: 같은 배율 · 쉬는 동작 · 이벤트 장면과 같은 몸 키) · 모닥불 둘레 구도는 그대로(2026-10-06 사용자: 미니미 → SD)
            //   불 왼쪽 둘은 오른쪽(불)을, 오른쪽 하나는 왼쪽을 본다(좌우 뒤집기). 뒤에 선 사도(가운데 위)를 먼저 그려 앞 사도에 가려지게.
            float body = Stage.Size.y * 0.24f;
            Vector2[] spots = { new Vector2(-340, 262), new Vector2(-170, 390), new Vector2(300, 262) };
            var party = P.S.Party.Take(spots.Length).ToList();
            foreach (int i in Enumerable.Range(0, party.Count).OrderByDescending(x => spots[x].y))
            {
                var h = Roster.OfCore(party[i]);
                var spot = Ui.Rect("hero" + i, scene).At(0.5f, 0, spots[i].x, spots[i].y, 10, 10);
                SceneHero.Make(spot, h, body, spots[i].x < 0, i * 0.4f);
                Tw.Pop(spot, 0.2f + i * 0.1f, 0.5f, 0.4f);
            }
            // 골디(상점 주인)는 상점 화면(좌판 단추를 누른 뒤 · Flow.Shop)에서만 선다 — 휴식 화면에는 파티만(2026-10-06 사용자)
            // 가운데 파티 HP 막대는 뺐다(2026-10-06 사용자) — 왼쪽 위 머리 띠와 같은 정보. 쉬기의 회복은 머리 띠 막대가 차오르며 보인다(HudHp)

            // 아래 — 고르기(가로 카드). 쉬기 · 수련 가운데 하나만, 장비 · 좌판은 선택을 쓰지 않는다
            var bottom = Ui.Rect("bottom", root);
            bottom.anchorMin = new Vector2(0, 0); bottom.anchorMax = new Vector2(1, 0); bottom.pivot = new Vector2(0.5f, 0);
            bottom.offsetMin = new Vector2(Theme.Gutter, Theme.Gutter); bottom.offsetMax = new Vector2(-320, Theme.Gutter + optH);
            Ui.Row(bottom, Theme.Gap, TextAnchor.LowerLeft, null, false, false);
            int heal = P.CampHeal;
            var rest = W.Option(bottom, "쉬기", used == "rest" ? "<color=#6EE0A0>푹 쉬었습니다</color>" : heal > 0 ? $"파티 HP 를 최대의 30% 채웁니다  <color=#6EE0A0>+{heal} HP</color>" : "HP 가 가득합니다 — 쉬기만 하고 지나갑니다", () =>
            {
                int before = P.S.PartyHp;
                var why = P.CampRest();
                if (why != null) { Toast.Show(why); return; }
                Sfx.Play("heal");
                int after = P.S.PartyHp, max = P.S.PartyMaxHp;
                Tw.Run(root, 0.6f, k => HudHp?.Invoke(Mathf.RoundToInt(Mathf.Lerp(before, after, k)), max), Tw.OutCubic);
                Toast.Show($"푹 쉬었습니다 — HP {before:N0} → {P.S.PartyHp:N0}");
                P.Save("camp", kind);
                Tw.After(0.7f, () => { if (root) BuildCamp(root, kind, withShop); });
            }, optH, Theme.S("ic_moon"), "rest");
            rest.Pref(300, -1, 1);
            rest.Interactable = used == "";   // HP 가 가득해도 고를 수 있다 — 수련할 카드가 없으면 쉬기가 유일한 길
            rest.Why = "이번 휴식에서는 이미 골랐습니다";
            if (used == "rest") W.Select(rest, true);
            Stage.Hot["camp.rest"] = rest;

            var train = P.S.Camp?.Train;
            var tcard = train != null ? P.Data.Card(train.CardId) : null;
            bool mind = P.Run.MindBroken;
            string noTrain = mind ? "정신 붕괴 — 수련할 수 없습니다 · 쉬기만 고를 수 있습니다" : "신탁을 붙일 고유 카드가 없습니다 — 쉬기만 고를 수 있습니다";
            var tr = W.Option(bottom, "수련", used == "train" ? "<color=#6EE0A0>손을 익혔습니다</color>" : tcard != null && !mind ? $"「{tcard.Name}」 에 신탁 하나를 붙입니다 — 셋 가운데 고릅니다" : noTrain, () => TrainPick(root, kind, withShop), optH, Theme.S("ic_spark"), "train");
            tr.Pref(300, -1, 1);
            tr.Interactable = used == "" && tcard != null && !mind;
            tr.Why = used != "" ? "이번 휴식에서는 이미 골랐습니다" : noTrain;
            if (used == "train") W.Select(tr, true);
            Stage.Hot["camp.trainopen"] = tr;

            // 장비 칸은 없앴다(2026-10-06 사용자) — 낀 장비는 머리 띠의 장비 단추 · 덱 보기의 장비 탭에서 본다
            if (withShop)
            {
                var shop = W.Option(bottom, "골디의 좌판", "「어서 오세요, 고객님!」 카드 · 장비 · 카드 제거 — 들러도 휴식 선택은 그대로", () => Shop(kind), optH, Theme.S("ic_bag"), "shop");
                shop.Pref(320, -1, 1);
                Stage.Hot["camp.shop"] = shop;
            }
            int c = 0;
            foreach (Transform t in bottom) Tw.Pop((RectTransform)t, 0.25f + c++ * 0.07f, 0.9f, 0.35f);

            if (used != "")
            {
                var leave = Btn.Make(root, "출발", BtnStyle.PillGold, LeaveCamp, Theme.FsLg);
                leave.GetComponent<RectTransform>().At(1, 0, -Theme.Gutter, Theme.Gutter + (optH - 66) / 2, 272, 66);
                Stage.Hot["camp.leave"] = leave;
                Tw.Pop(leave.GetComponent<RectTransform>(), 0.1f, 0.8f, 0.35f);
            }
            else
            {   // 출발 자리 — 고르기 전에는 안내만
                var hint = Ui.Text(root, "쉬기 · 수련 가운데\n하나를 고르세요", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center);
                hint.rectTransform.At(1, 0, -Theme.Gutter, Theme.Gutter + (optH - 66) / 2, 272, 66);
            }
            if (used == "") Stage.Hot.Remove("camp.leave");   // 다시 세울 때 옛 출발 단추가 남지 않게
        }

        /// <summary>수련 — 신탁 고르기 연출(OracleReveal). 취소 없음 · 반드시 한 장(2026-10-06 사용자).</summary>
        void TrainPick(RectTransform root, string kind, bool withShop)
        {
            var train = P.S.Camp?.Train;
            var tcard = train != null ? P.Data.Card(train.CardId) : null;
            if (tcard == null || OracleReveal.IsOpen) return;
            var opts = P.Run.FlashOptions(train);
            OracleReveal.Show(this, new RevealOpts
            {
                Title = "수련!", Sub = $"<color=#ffd76a>{tcard.Name}</color>에 신탁 — 카드를 눌러 하나를 고르세요",
                BaseCardId = train.CardId, Picks = RevealPick.Of(P.Data, train.CardId, opts), Cancel = false, Hot = "camp.train",
                Try = i => P.CampTrain(opts[i].N),
                Done = i =>
                {
                    Toast.Show($"「{tcard.Name}」 — 신탁 {opts[i].N}" + (opts[i].Blessed ? " · 축복" : ""));
                    P.Save("camp", kind);
                    if (root) BuildCamp(root, kind, withShop);
                },
            });
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
