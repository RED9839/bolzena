using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 싸움 칸 — 전투 화면(트랙 C)이 들어올 자리. 지금은 「전투 결과 받기」: 코어의 봇이 실제로 한 판을 싸우고(Run.OpenFight → Bots)
    // 이긴 것으로 · 진 것으로 받을 수도 있다. 그 뒤 보상(골드 · 장비 · 빛났던 카드) → 보스면 고유 카드 복제 → 층 넘기기.
    public partial class Flow
    {
        void FightStub()
        {
            bool ev = P.S.EventFight != null;
            var node = P.Here;
            string kind = ev ? "이벤트 전투" : P.IsBoss ? "보스" : P.S.Elite ? "엘리트" : "일반 전투";
            var f = P.Floor;
            string bgKey = P.IsBoss && !ev ? "boss" : "fight";
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue(bgKey, out var bg) ? bg : "stage3_2", 0.5f);
            var foes = ev ? P.S.EventFight.Enemies : P.Run.CurrentEnemies();
            Stage.Show("fight", root =>
            {
                W.StatusBar(root, this, true, true, false, $"{(node != null ? P.StageName(node) + " · " : "")}{kind}", ev ? P.S.EventFight.Name : $"{P.VillageDef.Name} · {f.Name}");

                // 맞설 적
                var foeBox = Ui.Rect("foes", root).At(0.5f, 0.5f, 220, 60, 820, 420);
                Ui.Row(foeBox, 18, TextAnchor.MiddleCenter, null, false, false);
                int i = 0;
                foreach (var id in foes)
                {
                    var e = P.Data.Enemy(id);
                    var card = Ui.Panel(foeBox, P.IsBoss && !ev ? Theme.S("cell_on", 20) : Theme.Cell, null, "foe " + id);
                    card.Pref(250, 380);
                    var crt = card.rectTransform;
                    var nm = Ui.Title(crt, e?.Name ?? id, 28, e != null && e.Boss ? Theme.Bad : Theme.Ink, TextAlignmentOptions.Center);
                    nm.rectTransform.Band(1, 40, 10, 10, -16);
                    var sub = Ui.Text(crt, $"HP {e?.Hp:N0} · {(e?.Row == "back" ? "후열" : "전열")} · <color=#{ColorUtility.ToHtmlStringRGB(Theme.NatureOf(e?.Nature))}>{e?.Nature}</color>", 16, Theme.Sub, TextAlignmentOptions.Center);
                    sub.rectTransform.Band(1, 24, 10, 10, -58);
                    var glyph = Ui.Img(crt, Theme.S(e != null && e.Boss ? "ic_crown" : "ic_skull"), (e != null && e.Boss ? Theme.Bad : Theme.Sub).A(0.5f), "glyph");
                    glyph.rectTransform.At(0.5f, 1, 0, -96, 90, 90);
                    glyph.preserveAspect = true;
                    string intents = e == null ? "" : string.Join("\n", e.Intents.Take(4).Select(it => "· " + P.Text.Intent(it)));
                    var bl = Ui.Text(crt, (e?.Blurb ?? "") + "\n<color=#F2CF7A>" + intents + "</color>", 14, new Color(0.86f, 0.85f, 0.93f), TextAlignmentOptions.TopLeft);
                    bl.rectTransform.Fill(16, 14, 16, 200);
                    bl.enableAutoSizing = true; bl.fontSizeMin = 10; bl.fontSizeMax = 14;
                    Tw.Pop(crt, 0.2f + i++ * 0.12f, 0.7f, 0.45f);
                }

                // 파티
                var partyBox = Ui.Panel(root, Theme.Panel, null, "party").rectTransform;
                partyBox.At(0, 0.5f, 40, 60, 360, 420);
                Ui.Col(partyBox, 12, TextAnchor.UpperLeft, new RectOffset(22, 22, 20, 20));
                Ui.Title(partyBox, "파티", 28, Theme.Ink).Pref(-1, 36);
                foreach (var k in P.S.Party)
                {
                    var h = Roster.OfCore(k);
                    var row = Ui.Rect("p " + k, partyBox); row.Pref(-1, 76);
                    var face = W.Face(row, h, 68); face.At(0, 0.5f, 0, 0, 68, 68);
                    var t = Ui.Title(row, h.ko, 24, Theme.Ink); t.rectTransform.At(0, 1, 84, -6, 220, 32);
                    var d = P.Data.Hero(k);
                    var s = Ui.Text(row, $"자리 {P.S.Party.IndexOf(k) + 1} · 공격 {d?.Atk} · 방어 {d?.Def}", 15, Theme.Sub);
                    s.rectTransform.At(0, 1, 86, -40, 240, 24);
                }
                var (hpRt, setHp) = W.HpBar(partyBox, 300, 28);
                hpRt.Pref(-1, 28);
                setHp(P.S.PartyHp, P.S.PartyMaxHp);
                Tw.Rise(partyBox, 0.15f, 30, 0.45f, Vector2.left);

                // 결과 받기
                var note = Ui.Text(root, "전투 화면(트랙 C)이 들어올 자리입니다 — 지금은 결과만 받습니다. 「봇 전투」는 코어 규칙으로 실제 전투를 한 번 돌립니다.", 17, Theme.Sub, TextAlignmentOptions.Center);
                note.rectTransform.Band(0, 30, 200, 200, 140);
                note.Outline(0.2f);
                var bar = Ui.Rect("actions", root).Band(0, 80, 300, 300, 40);
                Ui.Row(bar, 18, TextAnchor.MiddleCenter, null, false, true);
                void Go(string mode)
                {
                    var o = P.AutoFight(mode);
                    Banner(o.Won ? "승리!" : "패배", o.Won ? Theme.Gold : Theme.Bad, () => FightDone(o));
                }
                var win = Btn.Make(bar, "전투 결과 받기 — 이김", BtnStyle.PillGold, () => Go("win"), 26); win.Pref(380, 80);
                var bot = Btn.Make(bar, "봇 전투", BtnStyle.PillDark, () => Go("bot"), 24); bot.Pref(260, 80);
                var lose = Btn.Make(bar, "짐", BtnStyle.PillDark, () => Confirm("진 것으로 받을까요?", "모험이 끝납니다.", "진 것으로", () => Go("lose"), true), 24); lose.Pref(140, 80);
                Stage.Hot["fight.win"] = win; Stage.Hot["fight.bot"] = bot; Stage.Hot["fight.lose"] = lose;
                Tw.Rise(bar, 0.4f, 30, 0.4f);
            });
        }

        /// <summary>가운데를 가로지르는 띠 — 「승리!」 · 「패배」 · 「모험 완주」.</summary>
        void Banner(string text, Color c, Action then)
        {
            var layer = Ui.Rect("banner", Stage.ModalLayer).Fill();
            var block = Ui.Img(layer, Theme.White, new Color(0, 0, 0, 0), "block", true); block.rectTransform.Fill();
            var band = Ui.Img(layer, Theme.White, new Color(0.03f, 0.02f, 0.06f, 0.85f), "band");
            band.rectTransform.Band(0.5f, 0, 0, 0, 0);
            band.rectTransform.anchorMin = new Vector2(0, 0.5f); band.rectTransform.anchorMax = new Vector2(1, 0.5f); band.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var glow = Ui.Img(layer, Theme.S("soft"), c.A(0), "glow"); glow.rectTransform.At(0.5f, 0.5f, 0, 0, 900, 300);
            var t = Ui.Title(layer, text, 96, Color.white, TextAlignmentOptions.Center);
            t.rectTransform.At(0.5f, 0.5f, 0, 0, 1200, 140);
            t.colorGradient = new VertexGradient(Color.white, Color.white, c, c);
            t.Outline(0.12f, new Color(0.15f, 0.05f, 0, 1));
            t.characterSpacing = 30;
            var brt = band.rectTransform;
            Tw.Run(brt, 0.3f, k => { if (brt) brt.sizeDelta = new Vector2(0, 170 * k); });
            Tw.Run(t, 0.5f, k => { if (t) { t.characterSpacing = Mathf.Lerp(60, 8, k); t.alpha = k; } });
            Tw.Run(glow, 0.8f, k => { if (glow) glow.color = c.A(0.6f * Mathf.Sin(k * Mathf.PI)); }, Tw.Linear);
            Sfx.Play(text == "패배" ? "lose" : "win");
            Tw.After(Settings.ReduceMotion ? 0.4f : 1.3f, () =>
            {
                var g = layer.Group();
                Tw.Run(layer, 0.25f, k => { if (g) g.alpha = 1 - k; }, Tw.Linear, 0, () => { if (layer) Destroy(layer.gameObject); then(); });
            });
        }

        // ── 보상 — 골드 · 장비 · 빛났던 카드 ──
        // 카제나 전투 끝 보상의 배치만: 싸움터를 그대로 배경으로(큰 판 · 어두운 막 없이) 사도 셋이 서 있고, 왼쪽 가운데 「전투 끝」 칩,
        // 오른쪽에 보상 줄(반투명 알약 — 이름 · 아이콘 · ✓)을 점선 세로줄이 잇는다. 누르면 받는다(장비 → 장비 고르기, 골드 → 바로 ✓ + 골드 숫자가 오른다,
        // 빛났던 카드 → 카드 고르기 창). 오른쪽 아래 「떠나기」 — 안 받은 것이 있으면 한 번 묻는다.
        void Reward(RunPort.FightOutcome o)
        {
            var loot = o.Loot ?? P.S.Reward;
            var glows = new Dictionary<string, Glow>(o.Glows);
            var f = P.Floor;
            string bgKey = P.IsBoss && !o.Event ? "boss" : "fight";
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue(bgKey, out var bg) ? bg : "stage3_2", 0.25f);
            Stage.Show("reward", root => BuildReward(root, o, loot, glows, false), 1.2f);
        }

        /// <summary>
        /// 배경 없이 보상 열기 — 전투 화면이 이긴 뒤 전투 장면(싸움터 · 서 있는 사도)을 그대로 둔 채 부른다.
        /// 판 화면의 배경 층을 숨기고(Stage.Bare) 어둡게 내리는 전환 없이 보상 줄 · 머리 띠 · 떠나기만 그 위에 그린다.
        /// 「떠나기」 를 누르면 leave 를 먼저 부르고(전투 장면을 닫을 때) 배경 층을 되살린 뒤 판이 이어진다(AfterReward — 지도 · 보스 몫).
        /// 진 싸움 · 이벤트 싸움이면 보상 없이 FightDone 과 같은 길로 간다.
        /// </summary>
        public void RewardOverlay(RunPort.FightOutcome o, Action leave = null)
        {
            Fighting = false;   // 전투 화면이 done 대신 이것을 불러도 「싸우는 중」 이 남지 않게
            if (!o.Won || o.Event) { leave?.Invoke(); FightDone(o); return; }
            var loot = o.Loot ?? P.S.Reward;
            var glows = new Dictionary<string, Glow>(o.Glows);
            Stage.Bare = true;
            bareLeave = leave;
            Stage.Show("reward", root => BuildReward(root, o, loot, glows, true), 1.2f, true);
        }
        Action bareLeave;

        void BuildReward(RectTransform root, RunPort.FightOutcome o, RewardState loot, Dictionary<string, Glow> glows, bool bare)
        {
            Ui.Clear(root);
            W.StatusBar(root, this, true, true, true, null, null);
            // 골드는 저절로(2026-10-06 사용자) — 화면이 서면 곧장 받고 「+N 골드」 가 머리 띠 골드로 날아가며 숫자가 오른다.
            //   엔진 TakeGold 는 GoldTaken 으로 한 번만 — 다시 그려도 · 떠날 때 또 불러도 두 번 받지 않는다. 엘리트 · 보스 보상도 이 화면(이벤트 싸움은 보상 화면이 없다).
            int goldBefore = P.S.Gold;
            bool goldNow = loot != null && !loot.GoldTaken;
            if (goldNow) { P.TakeGold(); var gt = root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => x.name == "gold"); if (gt) gt.text = Ui.Gold(goldBefore); }

            // 싸움터 — 판 화면만 있을 때는 사도 셋을 세워 둔다(본 게임에서는 전투 장면의 사도가 그대로 서 있다)
            if (!bare)
            {
                var scene = Ui.Rect("scene", root).Fill();
                int pi = 0;
                foreach (var k in P.S.Party)
                {
                    var h = Roster.OfCore(k);
                    // 전투와 같은 SD 전투 스파인 · 같은 고정 배율(휴식 · 이벤트와 같은 몸 키) — 미니미는 없앴다(2026-10-06)
                    float body = Stage.Size.y * 0.24f;
                    var spot = Ui.Rect("hero" + pi, scene).At(0.5f, 0, -560 + (P.S.Party.Count - 1 - pi) * body * 0.9f, Theme.C(200, 150) + (pi % 2) * 14, 10, 10);
                    SceneHero.Make(spot, h, body, true, pi * 0.4f);
                    Tw.Pop(spot, 0.1f + pi * 0.08f, 0.7f, 0.4f);
                    pi++;
                }
            }

            // 왼쪽 가운데 「전투 끝」 칩
            var chip = Ui.Img(root, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.9f), "battleEnd").rectTransform;
            chip.At(0, 0.5f, Theme.Gutter + 8, 20, 150, 64);
            var ring = Ui.Img(chip, Theme.FramePill, Theme.Gold.A(0.8f), "ring"); ring.rectTransform.Fill();
            var ct = Ui.Title(chip, $"<size=62%><color={Theme.SubTag}>{o.Turns}턴 · 승리</color></size>\n전투 끝", Theme.FsLg, Theme.Gold, TextAlignmentOptions.Center);
            ct.rectTransform.Fill(8, 4, 8, 4); ct.lineSpacing = -18;
            Tw.Rise(chip, 0.1f, 20, 0.4f, Vector2.left);

            // 오른쪽 보상 줄
            float rowH = Theme.C(78, 70), gap = Theme.C(14, 10), listW = 380;
            var list = Ui.Rect("rewards", root).At(1, 1, -Theme.Gutter, -118, listW, 600);
            var items = new List<(string label, bool taken)>();
            int n = 0;
            float y = 0;
            void Line(string name, string title, string sub, Sprite icon, Color iconTint, bool taken, Action take, string hot)
            {
                var b = Btn.Make(list, null, BtnStyle.Glass, take, 0, name);
                var rt = b.GetComponent<RectTransform>(); rt.At(1, 1, -34, -y, listW - 34, rowH);
                b.Bg.color = new Color(1, 1, 1, taken ? 0.55f : 0.9f); b.SetColor(b.Bg.color);
                var t = Ui.Title(rt, title + (sub != null ? $"\n<size=70%><color={Theme.SubTag}>{sub}</color></size>" : ""), Theme.FsLg, taken ? Theme.Sub : Theme.Ink, TextAlignmentOptions.MidlineRight);
                t.rectTransform.Fill(16, 4, 82, 4); t.lineSpacing = -6;
                t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Ellipsis;
                var disc = Ui.Img(rt, Theme.S("circle"), Theme.NavyWell.A(0.9f), "disc"); disc.rectTransform.At(1, 0.5f, -14, 0, 56, 56);
                var ic = Ui.Img(disc.transform, icon, iconTint, "ic"); ic.rectTransform.Fill(12, 12, 12, 12); ic.preserveAspect = true;
                // ✓ 칸 — 점선 줄 위의 마디
                var node = Ui.Img(list, Theme.S("circle"), taken ? Theme.Good : Theme.NavyWell, "node"); node.rectTransform.At(1, 1, -6, -y - rowH / 2 + 12, 24, 24);
                var nring = Ui.Img(node.transform, Theme.S("ring"), taken ? Theme.Good : Theme.Edge, "ring"); nring.rectTransform.Fill();
                if (taken) { var ck = Ui.Img(node.transform, Theme.S("ic_check"), Theme.Night, "ck"); ck.rectTransform.Fill(5, 5, 5, 5); ck.preserveAspect = true; }
                b.Interactable = !taken;
                if (taken) b.Why = "받았습니다";
                Stage.Hot[hot] = b;
                Tw.Rise(rt, 0.15f + n * 0.07f, 30, 0.4f, Vector2.right);
                items.Add((title, taken));
                y += rowH + gap;
                n++;
            }
            if (loot != null)
            {
                Line("gold", $"골드 <color={Theme.GoldTag}>+{loot.Gold}</color>", "받음", Theme.Icon("gold"), Color.white, true, null, "reward.gold");
                if (goldNow) GoldFly(root, list, loot.Gold, goldBefore, P.S.Gold);
            }
            if (loot?.Equip != null)
                foreach (var id in loot.Equip)
                {
                    var e = P.Data.Equip(id);
                    var gc = Theme.GradeOf(e?.Grade);
                    bool taken = loot.EquipTaken != null;
                    Line("equip", "장비", $"<color=#{ColorUtility.ToHtmlStringRGB(gc)}>{e?.Grade}</color> {e?.Name}", CardArt.Equip(id) ?? W.SlotIcon(e?.Slot), CardArt.Equip(id) != null ? Color.white : gc, taken, () =>
                    {
                        var why = P.TakeEquip(id);
                        if (why != null) { Toast.Show(why); return; }
                        Settle(() => { if (root) BuildReward(root, o, loot, glows, bare); });
                    }, "reward.equip");
                }
            foreach (var kv in glows.ToList())
            {
                var g = kv.Value;
                var baseCard = P.Data.Card(kv.Key);
                string what = g.Kind == "hero" ? $"은총 · {Roster.OfCore(g.Hero).ko}" : $"신탁 · 「{baseCard?.Name}」";
                Line("glow", "빛났던 카드", what, Theme.S("ic_spark"), Theme.Gold, false, () => GlowPick(kv.Key, g, () => { glows.Remove(kv.Key); if (root) BuildReward(root, o, loot, glows, bare); }), "reward.glow");
            }
            if (n == 0) { var none = Ui.Text(list, "챙길 것이 없습니다", Theme.FsMd, Theme.Sub, TextAlignmentOptions.MidlineRight); none.rectTransform.At(1, 1, -34, 0, listW, 40); none.Outline(0.2f); }
            // 점선 세로줄 — 줄들을 오른쪽 끝에서 잇는다
            if (n > 0)
            {
                float total = y - gap;
                for (float d = rowH / 2; d < total + 70; d += 12)
                {
                    var dot = Ui.Img(list, Theme.S("circle"), Color.white.A(0.55f), "dot");
                    dot.rectTransform.At(1, 1, -16, -d, 3, 3);
                    dot.transform.SetAsFirstSibling();
                }
            }

            // 떠나기 — 안 받은 장비 · 빛났던 카드가 있으면 묻는다(골드는 저절로 챙긴다)
            bool left = false;
            void Leave()
            {
                if (left) return;
                left = true;
                if (loot != null && !loot.GoldTaken) P.TakeGold();
                if (bare) { var l = bareLeave; bareLeave = null; l?.Invoke(); Stage.Bare = false; }
                AfterReward();
            }
            var next = Btn.Make(root, null, BtnStyle.PillGold, () =>
            {
                var missed = new List<string>();
                if (loot?.Equip != null && loot.Equip.Count > 0 && loot.EquipTaken == null) missed.Add("장비");
                if (glows.Count > 0) missed.Add("빛났던 카드");
                if (missed.Count == 0) { Leave(); return; }
                Confirm("받지 않고 떠날까요?", $"{string.Join(" · ", missed)} — 떠나면 사라집니다." + (loot != null && !loot.GoldTaken ? " 골드는 챙깁니다." : ""), "떠나기", Leave, true);
            }, 0, "next");
            var nrt = next.GetComponent<RectTransform>(); nrt.At(1, 0, -Theme.Gutter, Theme.Gutter, 300, 70);
            var play = Ui.Img(nrt, Theme.S("circle"), Theme.Brown.A(0.18f), "disc"); play.rectTransform.At(0, 0.5f, 12, 0, 46, 46);
            var pic = Ui.Img(play.transform, Theme.S("ic_play"), Theme.Brown, "ic"); pic.rectTransform.Fill(13, 12, 11, 12); pic.preserveAspect = true;
            var nl = Ui.Title(nrt, P.IsBoss ? "보스 몫으로" : "떠나기", Theme.FsLg + 2, Theme.Brown, TextAlignmentOptions.MidlineRight); nl.rectTransform.Fill(70, 4, 28, 4);
            next.Label = nl;
            Tw.Rise(nrt, 0.3f, 20, 0.4f);
            Stage.Hot["reward.next"] = next;
        }

        /// <summary>「+N 골드」 가 보상 줄 골드 칸에서 떠올라 머리 띠 골드로 날아가고, 닿으면 숫자가 before → after 로 오른다(동전 몇 닢이 따라간다).</summary>
        void GoldFly(RectTransform root, RectTransform list, int amount, int before, int after)
        {
            var target = root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => x.name == "gold");
            var layer = Stage.ToastLayer;
            var c = new Vector3[4];
            list.GetWorldCorners(c);
            Vector3 from = new Vector3(Mathf.Lerp(c[0].x, c[2].x, 0.55f), c[1].y - Theme.C(39, 35) * list.lossyScale.y, 0);
            Vector3 to = target != null ? target.transform.position : new Vector3(Screen.width - 120, Screen.height - 40, 0);
            var tag = Ui.Title(layer, $"+{amount} 골드", Theme.FsXl, Theme.Gold, TextAlignmentOptions.Center, "goldfly");
            tag.Outline(0.25f, new Color(0.3f, 0.15f, 0));
            tag.rectTransform.sizeDelta = new Vector2(320, 60);
            tag.transform.position = from;
            tag.alpha = 0;
            var trt = tag.rectTransform;
            float rise = 60 * layer.lossyScale.y;
            Tw.Run(trt, 0.35f, k => { if (!trt) return; tag.alpha = k; trt.position = from + new Vector3(0, rise * Tw.OutCubic(k), 0); trt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.15f, Tw.OutBack(k)); }, Tw.Linear, 0.25f);
            Tw.Run(trt, 0.5f, k =>
            {
                if (!trt) return;
                var a = from + new Vector3(0, rise, 0);
                var mid = (a + to) / 2 + new Vector3(0, 80 * layer.lossyScale.y, 0);
                trt.position = Vector3.Lerp(Vector3.Lerp(a, mid, k), Vector3.Lerp(mid, to, k), k);
                trt.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.5f, k);
                tag.alpha = 1 - k * k * 0.6f;
            }, Tw.InOut, 0.85f, () =>
            {
                if (trt) Destroy(trt.gameObject);
                Sfx.Play("coin");
                CountGold(root, before, after);
                if (target) { var tr = target.transform; Tw.Run(tr, 0.3f, k => { if (tr) tr.localScale = Vector3.one * (1 + 0.25f * Mathf.Sin(k * Mathf.PI)); }); }
            });
            for (int i = 0; i < 6; i++)
            {
                var coin = Ui.Img(layer, Theme.Icon("gold"), Color.white, "coin");
                var rt = coin.rectTransform; rt.sizeDelta = new Vector2(26, 26); rt.position = from;
                var jitter = new Vector3(UnityEngine.Random.Range(-40f, 40f), UnityEngine.Random.Range(-10f, 30f), 0) * layer.lossyScale.y;
                Tw.Run(rt, 0.55f, k =>
                {
                    if (!rt) return;
                    var a = from + jitter;
                    rt.position = Vector3.Lerp(a, to, Tw.InOut(k)) + new Vector3(0, Mathf.Sin(k * Mathf.PI) * 90 * layer.lossyScale.y, 0);
                    coin.color = new Color(1, 1, 1, k < 0.85f ? 1 : (1 - k) / 0.15f);
                }, Tw.Linear, 0.55f + i * 0.05f, () => { if (rt) Destroy(rt.gameObject); });
            }
        }

        /// <summary>머리 띠 골드 숫자를 before → after 로 올린다.</summary>
        void CountGold(RectTransform root, int before, int after)
        {
            var t = root.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault(x => x.name == "gold");
            if (t == null) return;
            Tw.Run(t, 0.5f, k => { if (t) t.text = Ui.Gold(Mathf.RoundToInt(Mathf.Lerp(before, after, k))); });
        }

        /// <summary>빛났던 카드 고르기 — 신탁 · 은총 고르기 연출(OracleReveal, 2026-10-06). 고르면 받고 picked, 닫으면 그대로(보상 줄에 남는다).
        /// 신탁은 그 카드의 후보 셋(+ 축복)을 신탁을 얹은 모습으로, 은총은 받을 고유 카드를. 번호는 붙이지 않는다.</summary>
        void GlowPick(string cardId, Glow g, Action picked)
        {
            if (OracleReveal.IsOpen) return;
            List<string> gcs = null;
            var baseCard = P.Data.Card(cardId);
            bool card = g.Kind == "card";
            var opts = card ? Core.OracleOption.Of(P.Data, cardId, g.Picks) : null;
            List<RevealPick> picks;
            List<int> order;
            if (card) { picks = RevealPick.Of(P.Data, cardId, opts); order = Enumerable.Range(0, picks.Count).ToList(); }
            else
            {   // 은총 선택지도 기본 → 고유 순(CardOrder) — 받을 때는 원래 자리(idx)로
                var sorted = CardOrder.Sort(g.Options.Take(g.Count), P.Data, P.S.Party);
                order = Enumerable.Range(0, g.Count).OrderBy(i => sorted.IndexOf(g.Options[i])).ToList();
                picks = order.Select(i => new RevealPick { CardId = g.Options[i], Label = P.Data.Card(g.Options[i])?.Name }).ToList();
            }
            string who = card ? $"<color=#ffd76a>{baseCard?.Name}</color>에 신탁" : $"<color=#ffd76a>{Roster.OfCore(g.Hero).ko}</color>의 은총";
            OracleReveal.Show(this, new RevealOpts
            {
                Title = card ? "신탁!" : "은총!", Awake = card ? "신탁" : "은총",
                Sub = $"{who} — 빛났지만 내지 않은 카드 · 하나를 고르세요(닫고 떠나면 사라집니다)",
                BaseCardId = cardId, Picks = picks, Cancel = true, Hot = "glow.opt",
                Try = k => { gcs = CardSnap(); return P.ClaimGlow(cardId, g, order[k]); },
                Done = k =>
                {
                    var o = card ? opts[order[k]] : null;
                    Toast.Show($"「{picks[k].Label}」 — 받았습니다" + (o != null && o.Blessed ? $" · 축복 「{o.BlessName}」" : ""));
                    GainCards(gcs != null ? NewCards(gcs) : null, picked);   // 은총 고유 카드 — 다시 태어난 뒤 가운데에서 덱으로(신탁은 새 카드가 없어 그대로)
                },
            });
            Stage.Hot["glow.close"] = Stage.Hot.TryGetValue("glow.opt.close", out var x) ? x : null;
        }

        void CoinBurst(RectTransform at)
        {
            var layer = Stage.ToastLayer;
            var corners = new Vector3[4];
            at.GetWorldCorners(corners);
            var c = (corners[0] + corners[2]) / 2;
            for (int i = 0; i < 10; i++)
            {
                var coin = Ui.Img(layer, Theme.Icon("gold"), Color.white, "coin");
                coin.rectTransform.sizeDelta = new Vector2(30, 30);
                coin.rectTransform.position = c;
                var rt = coin.rectTransform;
                var dir = new Vector2(Mathf.Cos(i * 0.63f), Mathf.Sin(i * 0.63f) + 1.2f) * (60 + i * 7);
                var start = rt.anchoredPosition;
                Tw.Run(rt, 0.6f, k =>
                {
                    if (!rt) return;
                    rt.anchoredPosition = start + dir * k + new Vector2(0, -260 * k * k);
                    coin.color = new Color(1, 1, 1, 1 - k * k);
                }, Tw.Linear, i * 0.02f, () => { if (rt) Destroy(rt.gameObject); });
            }
        }

        // ── 보스 보상 — 가진 고유 카드 셋 가운데 하나를 복제 ──
        void BossCopyPick(List<string> ids, Action<string> pick)
        {
            ids = CardOrder.Sort(ids, P.Data, P.S.Party);
            Stage.Show("bosscopy", root =>
            {
                W.StatusBar(root, this, true, true, false, "층 보스의 몫", "고유 카드 하나를 복제합니다 — 지금 신탁 · 축복 그대로, 그 뒤로는 받을 수 없습니다");
                float cw = Theme.C(280, 236);
                var row = Ui.Rect("cards", root).At(0.5f, 0.5f, 0, Theme.C(-10, -6), 1000, cw * 1.4f + 30);
                Ui.Row(row, 30, TextAnchor.MiddleCenter, null, false, false);
                int i = 0;
                string chosen = null;
                Btn go = null;
                var frames = new List<Image>();
                foreach (var id in ids)
                {
                    var holder = Ui.Rect("c" + i, row); holder.Pref(cw, cw * 1.4f + 20);
                    var card = W.Card(holder, this, id, cw);
                    card.At(0.5f, 0.5f, 0, 0, cw, cw * 1.4f);
                    var frame = Ui.Img(holder, Theme.S("frame_thick", 24), Theme.Gold.A(0), "pick");   // 속이 빈 금 테 — 고른 카드를 가리지 않는다
                    frame.rectTransform.Fill(-8, -8, -8, -8);
                    frames.Add(frame);
                    var b = card.gameObject.AddComponent<Btn>();
                    var me = frame;
                    b.OnClick = () =>
                    {
                        chosen = id;
                        foreach (var fr in frames) fr.color = Theme.Gold.A(fr == me ? 1 : 0);
                        go.Interactable = true;
                        go.SetLabel($"「{P.Data.Card(id)?.Name}」 복제");
                    };
                    Stage.Hot["copy" + i] = b;
                    Tw.Pop(holder, 0.2f + i * 0.12f, 0.6f, 0.5f);
                    i++;
                }
                var hint = Ui.Text(root, "하나를 고르세요", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center);
                hint.rectTransform.At(0.5f, 0, 0, Theme.Gutter + 18, 600, 30); hint.Outline(0.2f);
                go = Btn.Make(root, "카드를 고르세요", BtnStyle.PillGold, () => pick(chosen), Theme.FsLg);
                go.GetComponent<RectTransform>().At(1, 0, -Theme.Gutter, Theme.Gutter, 320, 66);
                go.Interactable = false;
                go.Why = "카드를 하나 고르세요";
                Stage.Hot["copy.go"] = go;
                var skip = Btn.Make(root, "받지 않기", BtnStyle.PillDark, () => pick(null), Theme.FsMd);
                skip.GetComponent<RectTransform>().At(0, 0, Theme.Gutter, Theme.Gutter, 240, 66);
            });
        }
    }
}
