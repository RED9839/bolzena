using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 마을 공개 — 웹판 ui.js villageScreen(docs/20-마을.md §0). 모험을 시작하면 마을 하나가 무작위로 정해져 보인다.
    // 웹판보다: 마을 이름이 슬롯처럼 돌다가 멈추고, 두 층이 차례로 펼쳐진다.
    public partial class Flow
    {
        public void VillageReveal(string villageId, Action onGo)
        {
            var v = P.Village(villageId);
            var f0 = v.Floors[0];
            Stage.SetBg(f0.Bg != null && f0.Bg.TryGetValue("fight", out var bg) ? bg : "stage3_2", 0.55f);
            Stage.Show("village", root =>
            {
                var panel = Ui.Panel(root, Theme.Panel, null, "card").rectTransform;
                bool hasNat = !string.IsNullOrEmpty(FoeNature);
                // 적 속성이 있으면 오른쪽에 「이번 판 적 속성 + 층 보스 초상」 칸을 붙인다(이걸 보고 약점에 맞춰 파티를 짠다)
                panel.At(0.5f, 0.5f, 0, 0, hasNat ? 680 + SideW : 680, hasNat ? 600 : 560);
                FitScale.Fit(panel, Stage.Size, hasNat ? 680 + SideW : 680, hasNat ? 600 : 560, 0.67f);   // 화면 비례(PC 높이 900 에서 보이는 몫) — 보스 클론 초상도 함께 줄어든다
                Ui.Shadow(panel, 30, -14, 0.7f);
                Tw.Pop(panel, 0.05f, 0.85f, 0.5f);
                var main = Ui.Rect("main", panel).Fill(0, 0, hasNat ? SideW : 0, 0);

                var kick = Ui.Title(main, "이번 모험의 마을", Theme.FsMd, Theme.Gold, TextAlignmentOptions.Center);
                kick.rectTransform.Band(1, 30, 0, 0, -30);
                var name = Ui.Title(main, v.Name, 72, Theme.Ink, TextAlignmentOptions.Center);
                name.rectTransform.Band(1, 90, 0, 0, -62);
                name.Outline(0.12f);
                var race = Ui.Chip(main, null, v.Race ?? "", 34, Theme.NavyCell, null, Theme.FsSm);
                race.bg.rectTransform.At(0.5f, 1, 0, -160, 200, 36);
                var fit = race.bg.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
                fit.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                var line = Ui.Text(main, v.Line ?? "", Theme.FsMd, Theme.Sub, TextAlignmentOptions.Center);
                line.rectTransform.Band(1, 60, 40, 40, -206);

                var floors = Ui.Rect("floors", main).Band(1, 120, 40, 40, -276);
                Ui.Col(floors, 10, TextAnchor.UpperCenter, null, true, false);
                for (int i = 0; i < v.Floors.Count; i++)
                {
                    var fl = v.Floors[i];
                    var row = Ui.Img(floors, Theme.Cell, Color.white, "floor" + i);
                    row.Pref(-1, 54);
                    var t = Ui.Title(row.transform, $"<color={Theme.GoldTag}>{i + 1}층</color>    {fl.Name}   <size=68%><color={Theme.SubTag}>{fl.Sub}</color></size>", Theme.FsLg, Theme.Ink);
                    t.rectTransform.Fill(22, 0, 10, 0);
                    t.textWrappingMode = TextWrappingModes.NoWrap;
                    Tw.Pop(row.rectTransform, 1.1f + i * 0.18f, 0.8f, 0.4f);
                }
                if (hasNat) FoeSide(panel, v, FoeNature, 1.4f);
                var note = Ui.Text(main, $"1-1 부터 {v.Floors.Count}-10 까지 이 마을의 적만 나옵니다. {v.Floors.Count}층 보스를 이기면 모험 완주입니다.", Theme.FsSm, Theme.Sub, TextAlignmentOptions.Center);
                note.rectTransform.Band(0, 30, 30, 30, 112);
                var row2 = Ui.Rect("buttons", main).Band(0, 66, 40, 40, 34);
                Ui.Row(row2, 16, TextAnchor.MiddleCenter, null, false, true);
                var back = Btn.Make(row2, "로비로", BtnStyle.PillDark, Lobby, Theme.FsMd);
                back.Pref(200, Theme.BtnMainH);
                var go = Btn.Make(row2, "파티 편성", BtnStyle.PillGold, onGo, Theme.FsLg);
                go.Pref(380, Theme.BtnMainH);
                Stage.Hot["go"] = go;

                // 이름 굴리기 — 다른 마을 이름(없으면 층 이름)을 돌리다가 멈춘다
                var pool = P.Villages.Select(x => x.Name).Concat(v.Floors.Select(x => x.Name)).Distinct().ToList();
                StartCoroutine(Roll(name, pool, v.Name));
            }, 1.6f);
        }

        static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

        const float SideW = 400;

        /// <summary>
        /// 그 층의 보스 id — 판의 적 속성에 맞춰 core 가 고른 보스 줄(Run.PickBosses — 같은 씨앗으로 판을 열면 run.Bosses 와 같다).
        /// </summary>
        string BossAt(Bolzena.Core.VillageDef v, int floor)
        {
            var line = FoeBosses != null && floor < FoeBosses.Count ? FoeBosses[floor] : floor < v.Floors.Count ? v.Floors[floor].Boss : null;
            if (line == null || line.Count == 0) return null;
            return line.FirstOrDefault(id => P.Data.Enemy(id)?.Clone != null) ?? line[0];   // 그 층 보스 줄의 사도 클론(없으면 첫 적)
        }

        static string WeakLine(string nat)
        {
            var weak = RunPort.WeakTo(nat);
            return weak.Count == 0 ? "" : $"<color={Theme.SubTag}>약점</color>  <color={Hex(Theme.NatureCardOf(weak[0]))}>{string.Join(" · ", weak)}</color>";
        }

        /// <summary>판의 적 속성 한 줄 표시 이름 — 마을 공개 · 편성 · 지도 머리에서 같은 말로 이어진다.</summary>
        public const string FoeNatureLabel = "이번 모험의 적 속성";

        /// <summary>적 속성 굴리기(RollNature)가 멈추기까지 걸리는 때 — 약점 · 보스처럼 속성을 알려 주는 것은 그 뒤에 보인다(먼저 보이면 굴리기 전에 답이 새어 나간다).</summary>
        static float RollTime()
        {
            if (Settings.ReduceMotion) return 0;
            float dt = 0.06f, el = 0;
            while (el < 0.7f) { el += dt; dt *= 1.2f; }
            return el;
        }

        // 마을 공개 오른쪽 — 「이번 모험의 적 속성」 큰 아이콘 · 이름(슬롯처럼 돌다 멈춘다) → 멈춘 뒤에 「모든 적이 ○○ 속성입니다」 · 약점(보조) · 1층 / 2층 보스 클론 초상
        void FoeSide(RectTransform panel, Bolzena.Core.VillageDef v, string nat, float delay)
        {
            var col = Ui.Rect("foeside", panel).Column(1, SideW, 0, 0, 0);
            var sep = Ui.Img(col, null, Color.white.A(0.1f), "sep"); sep.rectTransform.Column(0, 2, 28, 28, 0);
            var kick = Ui.Title(col, FoeNatureLabel, Theme.FsMd, Theme.Gold, TextAlignmentOptions.Center);
            kick.rectTransform.Band(1, 30, 0, 0, -30);
            var head = Ui.Rect("head", col).At(0.5f, 1, 0, -68, 260, 76);
            var glow = Ui.Img(head, Theme.S("soft"), Theme.NatureCardOf(nat).A(0), "glow"); glow.rectTransform.At(0, 0.5f, 38, 0, 170, 170);
            var ic = Ui.Img(head, Icon("성격_" + nat), Color.white, "ic"); ic.rectTransform.At(0, 0.5f, 38, 0, 68, 68); ic.preserveAspect = true;
            var nm = Ui.Title(head, nat, 60, Theme.NatureCardOf(nat), TextAlignmentOptions.MidlineLeft); nm.rectTransform.Fill(106, 0, 0, 0);
            nm.Outline(0.12f); nm.textWrappingMode = TextWrappingModes.NoWrap;
            float done = delay + RollTime();   // 속성이 멈춘 때 — 이 뒤로만 속성을 드러내는 것(설명 · 약점 · 보스)을 띄운다
            var nc = Theme.NatureCardOf(nat);
            var say = Ui.Title(col, $"모든 적과 두 보스가 <color={Hex(nc)}>{nat}</color> 속성입니다", Theme.FsMd + 2, Theme.Ink, TextAlignmentOptions.Center);
            say.rectTransform.Band(1, 32, 12, 12, -148); say.textWrappingMode = TextWrappingModes.NoWrap; say.enableAutoSizing = true; say.fontSizeMin = 14; say.fontSizeMax = Theme.FsMd + 2;
            var weak = RunPort.WeakTo(nat);
            var wk = Ui.Text(col, weak.Count == 0 ? "" : $"<color={Theme.SubTag}>약점</color>  <color={Hex(Theme.NatureCardOf(weak[0]))}>{string.Join(" · ", weak)}</color> <color={Theme.SubTag}>— {nat}에 강한 성격으로 치면 강인도가 크게 깎입니다(공명은 늘)</color>",
                Theme.FsSm, Theme.Ink, TextAlignmentOptions.Top);
            wk.rectTransform.Band(1, 46, 18, 18, -184);
            Tw.Pop(say.rectTransform, done, 0.9f, 0.35f);
            Tw.Pop(wk.rectTransform, done + 0.15f, 0.95f, 0.35f);
            int n = Mathf.Min(2, v.Floors.Count);
            float cw = 158, ch = 236, gap = 18;
            for (int i = 0; i < n; i++)
            {
                var id = BossAt(v, i);
                if (id == null) continue;
                var card = BossCard(col, id, $"{i + 1}층 보스", nat, cw, ch);
                card.At(0.5f, 1, (i - (n - 1) / 2f) * (cw + gap), -246, cw, ch);
                Tw.Pop(card, done + 0.3f + i * 0.15f, 0.8f, 0.4f);
            }
            Tw.Pop(head, delay, 0.85f, 0.4f);
            StartCoroutine(RollNature(nm, ic, glow, nat, delay));
        }

        /// <summary>보스 초상 칸 — 사도 클론이면 그 사도 상반신, 아니면 왕관. 오른쪽 아래에 성격 아이콘, 밑에 「N층 보스」 · 이름.</summary>
        RectTransform BossCard(Transform parent, string id, string label, string nat, float w, float h)
        {
            var e = P.Data.Enemy(id);
            nat = e?.Nature ?? nat;   // 클론 보스는 제 성격(= 판의 적 속성으로 골라진 사도)
            var nc = Theme.NatureCardOf(nat);
            var card = Ui.Img(parent, Theme.Cell, Color.white, "boss " + id).rectTransform;
            card.sizeDelta = new Vector2(w, h);
            card.Pref(w, h);
            float bottom = 52;
            var win = Ui.Img(card, Theme.Round, Color.Lerp(Theme.NavyWell, nc, 0.4f), "win").rectTransform;
            win.Fill(6, bottom, 6, 6);
            var mask = Ui.Rect("mask", win).Fill(3, 3, 3, 3);
            mask.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var hero = BossHero(e);
            var face = hero != null ? CardArt.Upper(hero.art, (w - 18) / (h - bottom - 18), 0.5f) : null;
            if (face != null) { var f = Ui.Img(mask, face, Color.white, "face"); f.rectTransform.Fill(); }
            else { var cr = Ui.Img(mask, Theme.S("ic_crown"), Theme.Gold.A(0.9f), "crown"); cr.rectTransform.At(0.5f, 0.5f, 0, 0, 56, 56); cr.preserveAspect = true; }
            var nb = Ui.Img(card, Theme.S("circle"), Theme.NavyWell, "natbg"); nb.rectTransform.At(1, 0, -8, bottom - 6, 46, 46);
            var nr = Ui.Img(nb.transform, Theme.S("ring"), nc, "ring"); nr.rectTransform.Fill();
            var ni = Ui.Img(nb.transform, Icon("성격_" + nat), Color.white, "ic"); ni.rectTransform.Fill(6, 6, 6, 6); ni.preserveAspect = true;
            var lb = Ui.Title(card, label, Theme.FsCap, Theme.Gold, TextAlignmentOptions.MidlineLeft); lb.rectTransform.At(0, 0, 12, 30, w - 20, 20);
            var nm = Ui.Title(card, e?.Name ?? id, Theme.FsSm, Theme.Ink, TextAlignmentOptions.MidlineLeft); nm.rectTransform.At(0, 0, 12, 8, w - 20, 24);
            nm.textWrappingMode = TextWrappingModes.NoWrap; nm.enableAutoSizing = true; nm.fontSizeMin = 10; nm.fontSizeMax = Theme.FsSm;
            return card;
        }

        /// <summary>사도 클론 보스의 사도 — core EnemyDef.Clone(사도 키 · 빌린 몸 클론도). 없으면 이름 「○○(클론)」 · 스파인 폴더로.</summary>
        static HeroInfo BossHero(Bolzena.Core.EnemyDef e)
        {
            if (e == null) return null;
            if (e.Clone != null) { var c = Roster.All.FirstOrDefault(x => x.CoreId == e.Clone || x.key == e.Clone); if (c != null) return c; }
            string nm = (e.Name ?? "").Replace("(클론)", "").Trim();
            var h = Roster.All.FirstOrDefault(x => x.ko == nm);
            if (h == null && e.Art != null && e.Art.Spine != null)
            {
                var sp = e.Art.Spine.Replace('\\', '/');
                sp = sp.Substring(sp.LastIndexOf('/') + 1);
                h = Roster.All.FirstOrDefault(x => x.ko == sp || x.key == sp || x.art == sp);
            }
            return h;
        }

        // 적 속성 굴리기 — 다섯 성격을 돌다가 멈추고, 멈춘 성격 빛이 한 번 번진다
        IEnumerator RollNature(TextMeshProUGUI t, UnityEngine.UI.Image ic, UnityEngine.UI.Image glow, string final, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            var pool = Bolzena.Core.R.FOE_NATURES;
            float dt = 0.06f, el = 0, total = Settings.ReduceMotion ? 0 : 0.7f;
            int i = System.Array.IndexOf(pool, final) + 1;
            while (el < total && t != null)
            {
                var n = pool[i++ % pool.Length];
                t.text = n; t.color = Theme.NatureCardOf(n);
                if (ic != null) ic.sprite = Icon("성격_" + n);
                yield return new WaitForSecondsRealtime(dt);
                el += dt;
                dt *= 1.2f;
            }
            if (t == null) yield break;
            t.text = final; t.color = Theme.NatureCardOf(final);
            ic.sprite = Icon("성격_" + final);
            var rt = ic.rectTransform;
            Tw.Run(rt, 0.4f, k => { if (rt) rt.localScale = Vector3.one * Mathf.LerpUnclamped(1.5f, 1, k); }, Tw.OutBack);
            var c = Theme.NatureCardOf(final);
            Tw.Run(glow, 0.9f, k => { if (glow) glow.color = c.A(0.75f * (1 - k)); }, Tw.Linear);
        }

        /// <summary>편성 오른쪽 판 — 「이번 모험의 적 속성」 띠(성격 아이콘 · 이름 · 약점은 작게).</summary>
        void FoeNatureBand(RectTransform body, string nat)
        {
            if (string.IsNullOrEmpty(nat)) return;
            var nc = Theme.NatureCardOf(nat);
            var b = Ui.Img(body, Theme.Round, Color.Lerp(Theme.NavyWell, nc, 0.22f).A(0.9f), "foenature"); b.Pref(-1, 44);
            var ic = Ui.Img(b.transform, Icon("성격_" + nat), Color.white, "ic"); ic.rectTransform.At(0, 0.5f, 10, 0, 30, 30); ic.preserveAspect = true;
            var l = Ui.Title(b.transform, FoeNatureLabel, Theme.FsMd, Theme.Ink, TextAlignmentOptions.MidlineLeft); l.rectTransform.Fill(48, 0, 0, 0);
            var r = Ui.Title(b.transform, $"<color={Hex(nc)}>{nat}</color>  <size=78%>{WeakLine(nat)}</size>", Theme.FsLg, Theme.Ink, TextAlignmentOptions.MidlineRight);
            r.rectTransform.Fill(0, 0, 14, 0); r.textWrappingMode = TextWrappingModes.NoWrap;
        }

        /// <summary>편성 오른쪽 판 — 층 보스 초상 줄(1층 · 2층).</summary>
        void BossCardsRow(RectTransform body, Bolzena.Core.VillageDef v, string nat, float w)
        {
            int n = Mathf.Min(2, v.Floors.Count);
            float h = Theme.C(196, 124), cw = Mathf.Min(150, (w - 48 - 12 * (n - 1)) / n);
            var row = Ui.Rect("bosses", body); row.Pref(-1, h);
            Ui.Row(row, 12, TextAnchor.MiddleCenter, null, false, false);
            for (int i = 0; i < n; i++)
            {
                var id = BossAt(v, i);
                if (id != null) BossCard(row, id, $"{i + 1}층 보스", nat, cw, h);
            }
        }

        IEnumerator Roll(TextMeshProUGUI t, System.Collections.Generic.List<string> pool, string final)
        {
            float dt = 0.05f;
            int i = 0;
            float total = Settings.ReduceMotion ? 0.1f : 0.9f, el = 0;
            while (el < total && t != null)
            {
                t.text = pool[i++ % pool.Count];
                t.color = Theme.Sub;
                yield return new WaitForSecondsRealtime(dt);
                el += dt;
                dt *= 1.18f;
            }
            if (t == null) yield break;
            t.text = final;
            t.color = Theme.Ink;
            var rt = t.rectTransform;
            Tw.Run(rt, 0.4f, k => { if (rt) rt.localScale = Vector3.one * Mathf.LerpUnclamped(1.35f, 1, k); }, Tw.OutBack);
            var glow = Ui.Img(rt.parent, Theme.S("soft"), new Color(1f, 0.85f, 0.45f, 0), "flash");
            glow.rectTransform.At(0.5f, 1, 0, -60, 520, 160);
            glow.transform.SetSiblingIndex(0);
            Tw.Run(glow, 0.9f, k => { if (glow) glow.color = new Color(1f, 0.85f, 0.45f, 0.7f * (1 - k)); }, Tw.Linear);
            Sfx.Play("reveal");
        }
    }
}
