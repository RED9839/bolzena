using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 이벤트 은총(2026-10-06 사용자: 「원하는 사도 3명 중에 클릭하게 하고, 그 사도의 남은 고유 카드를 랜덤으로」)
    //   core: 결과의 unique → Pending「grace」(N 장) → 사도 키로 ResolvePending → 그 사도의 아직 없는 고유 카드에서 무작위 N 장.
    //   창: 파티 셋 칸(SD · 이름 · 「남은 고유 카드 N장」). 남은 것이 없는 사도는 흐리게 · 누를 수 없다. 「받지 않기」.
    //   얻은 카드는 CardGain(GainCards) 연출로 가운데에 크게 보인 뒤 덱으로.
    public partial class Flow
    {
        /// <summary>은총으로 얻은 카드를 가운데에 크게 보여 주는 시간(초) — 그 뒤 저절로 덱으로(누르면 바로). 전투 은총(손패로)과 같은 몫.</summary>
        public const float GraceHold = 0.9f;

        void GracePick(RectTransform root, Pending p)
        {
            var party = P.S.Party.Take(3).ToList();
            float H = Mathf.Min(Theme.C(620, 560), Stage.Size.y - 24);
            float W0 = Mathf.Min(Theme.C(1180, 1100), Stage.Size.x - 32);
            string sub = p.N > 1 ? $"고른 사도의 아직 없는 고유 카드 가운데 무작위 {p.N}장을 얻습니다" : "고른 사도의 아직 없는 고유 카드 가운데 무작위 한 장을 얻습니다";
            var (body, close, _) = Stage.ModalBox("grace", W0, H, "은총 — 누구의 고유 카드를 받을까요?", sub, false, null, 0, false);

            void Done(string key)
            {
                var cs = CardSnap();
                var why = P.Resolve(key);
                if (why != null) { Toast.Show(why); return; }
                close();
                // 은총 — 고르는 것은 사도까지. 얻은 카드는 「은총!」 으로 크게 0.9초 보인 뒤 저절로 덱으로(누르면 바로 · 2026-10-07 사용자)
                GainCards(NewCards(cs), () => { P.Save("event"); BuildEvent(root, -1); }, "은총!", key != null ? $"{Roster.OfCore(key).ko}의 고유 카드 — 덱에 넣습니다" : null, GraceHold);
            }

            float footH = Theme.C(64, 54), gap = 16;
            float bodyW = W0 - 40, bodyH = H - 86 - 12 - 18 - footH - 8;
            float colW = (bodyW - gap * (party.Count - 1)) / Mathf.Max(1, party.Count);
            var cols = Ui.Rect("heroes", body).Fill(0, footH + 8, 0, 0);
            for (int i = 0; i < party.Count; i++)
            {
                var key = party[i];
                var hero = Roster.OfCore(key);
                int left = P.Run.UniquesLeft(key).Count;
                bool ok = left > 0;
                var nat = Theme.NatureCardOf(P.Data.Hero(key)?.Nature ?? hero?.nature);
                var b = Btn.Make(cols, null, BtnStyle.Cell, ok ? () => Done(key) : (Action)null, 0, "grace " + key);
                var rt = b.GetComponent<RectTransform>();
                rt.At(0, 0.5f, i * (colW + gap), 0, colW, bodyH);
                Ui.Img(rt, Theme.Frame, nat.A(ok ? 0.85f : 0.3f), "rim").rectTransform.Fill();

                // 아래에서부터 자리 — 단추 · 남은 수 · 이름, 남는 높이가 SD 칸(폰에서도 겹치지 않게)
                float goH = Theme.C(52, 42), cntY = 12 + goH + 6, cntH = Theme.C(30, 24), nameY = cntY + cntH, nameH = Theme.C(40, 30);
                // 사도 SD — 칸 위쪽 바닥선에 선다(오른쪽을 본다)
                float sdH = Mathf.Max(40, bodyH - nameY - nameH - 8 - 8 - 30);
                var well = Ui.Img(rt, Theme.Round, Color.Lerp(Theme.NavyWell, nat, ok ? 0.3f : 0.08f), "well"); well.pixelsPerUnitMultiplier = 2f;
                well.rectTransform.Band(1, sdH + 30, 8, 8, -8);
                well.gameObject.AddComponent<RectMask2D>();
                if (hero != null)
                {
                    var spot = Ui.Rect("spot", well.rectTransform).At(0.5f, 0, 0, 16, 10, 10);
                    var g = SceneHero.Make(spot, hero, sdH * 0.86f, true, i * 0.4f);
                    if (g != null && !ok) g.color = new Color(0.45f, 0.45f, 0.5f, 0.7f);
                }

                var name = Ui.Title(rt, hero?.ko ?? key, Theme.FsLg, ok ? Theme.Ink : Theme.Dim, TextAlignmentOptions.Center, "name");
                name.rectTransform.Band(0, nameH, 10, 10, nameY);
                name.textWrappingMode = TextWrappingModes.Normal; name.overflowMode = TextOverflowModes.Overflow;
                name.enableAutoSizing = true; name.fontSizeMin = Theme.FsBody; name.fontSizeMax = Theme.FsLg;
                var cnt = Ui.Text(rt, ok ? $"남은 고유 카드 <color={Theme.GoldTag}>{left}장</color>" : "남은 고유 카드 없음", Theme.FsBody, ok ? Theme.Sub : Theme.Dim, TextAlignmentOptions.Center, false, "left");
                cnt.rectTransform.Band(0, cntH, 10, 10, cntY);
                cnt.textWrappingMode = TextWrappingModes.Normal; cnt.overflowMode = TextOverflowModes.Overflow;

                var go = Btn.Make(rt, ok ? "이 사도로" : "받을 카드 없음", ok ? BtnStyle.PillGold : BtnStyle.PillDark, ok ? () => Done(key) : (Action)null, Theme.FsMd, "gracego");
                go.GetComponent<RectTransform>().Band(0, goH, 16, 16, 12);
                if (!ok) { go.Interactable = false; b.Interactable = false; }
                if (ok) Stage.Hot["grace:" + key] = go;
                Tw.Rise(rt, 0.06f * i, 22, 0.35f, Vector2.up);
            }
            var skip = Btn.Make(body, "받지 않기", BtnStyle.PillDark, () => Done(null), Theme.FsMd, "graceskip");
            skip.GetComponent<RectTransform>().At(0.5f, 0, 0, 4, Theme.C(260, 220), footH - 12);
            Stage.Hot["grace.skip"] = skip;
        }
    }
}
