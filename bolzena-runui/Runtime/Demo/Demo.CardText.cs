using System.Collections;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 카드 글 가독성 점검(-demo-cardtext, 2026-10-07 「찢기의 『손패가 없으면』 이 헷갈린다 · 줄 띄워 가독성 · 카드 상세 옆에 낱말 설명 ·
    //   성전 모드 · 맨주먹 전성기가 뭔지 안 나온다」) — 대표 카드를 판 화면 카드(W.Card, 폭 230)로 놓고 카드 옆 낱말 판(CardSide)을 띄워 한 장씩,
    //   카드 크게 창(CardZoom — 오른쪽 맨 위 낱말 판) 셋, 사도 칸 판(변신 칸) 둘을 찍는다. -cards a,b,c 로 카드를 고른다.
    public partial class Demo
    {
        static readonly string[] CardTextIds =
        {
            "디아나_왕년_u1", "디아나_왕년_u4", "디아나_왕년_u3", "디아나_왕년_f1", "네르_빡침_f1", "포셔_u2", "로네_u2", "피코라_u4", "버터_u1", "에르핀_u1", "멜루나_u1",
            "시저_u1", "란_u1", "루포_u4", "비비_신성_u1", "다야_퓨어샤인_u1", "제이드_u3", "로네_u3", "캬롯_sprout", "네르_빡침_u1", "라이카_f1", "죠안_f1",
        };

        IEnumerator CardText_()
        {
            AudioListener.volume = 0f;   // 점검은 음소거(설정 값은 건드리지 않는다)
            yield return Wait(0.5f);
            f.Lobby();
            yield return Wait(1.0f);
            AudioListener.volume = 0f;
            var ids = Arg("-cards") is string list ? list.Split(',') : CardTextIds;
            var size = f.Stage.Size;
            int n = 0;
            foreach (var id in ids)
            {
                if (f.P.Data.Card(id) == null) { Debug.LogWarning($"[Demo] 없는 카드 {id}"); continue; }
                AudioListener.volume = 0f;
                var layer = Ui.Rect("modal cardtext", f.Stage.ModalLayer).Fill();
                Ui.Img(layer, Theme.White, Theme.Night.A(0.94f), "bg", true).rectTransform.Fill();
                float w = 230;
                var card = W.Card(layer, f, id, w, "card " + id);
                card.At(0.5f, 0.5f, -size.x * 0.18f, 0, w, w * 1.4f);
                yield return Wait(0.3f);
                var side = card.GetComponent<CardSide>();
                if (side != null) side.Show(); else Debug.LogWarning($"[Demo] 낱말 판 없음 {id}");
                yield return Wait(0.5f);
                yield return Shot($"cardtext_{++n:00}_{id}");
                Debug.Log($"[Demo] 카드 글 {id} — 낱말 {side?.Terms?.Count ?? 0}: {string.Join(" · ", side?.Terms?.Select(t => t.Name) ?? Enumerable.Empty<string>())}");
                TermPop.Close();
                Object.Destroy(layer.gameObject);
                yield return null;
            }
            // 카드 크게 창 — 오른쪽 맨 위 낱말 판(변신 카드 · 조건 둘 · 갈래)
            foreach (var id in new[] { "디아나_왕년_u1", "디아나_왕년_f1", "포셔_u2" })
            {
                f.CardZoom(id);
                yield return Wait(1.0f);
                yield return Shot($"cardtext_zoom_{id}");
                CloseModals();
                yield return Wait(0.2f);
            }
            // 사도 칸 판 — 고학년 · 고유 효과 · 변신 · 패시브(성전 모드 · 맨주먹 전성기가 무엇인지)
            foreach (var hid in new[] { "네르_빡침", "디아나_왕년" })
            {
                f.PopTerms(f.Stage.ModalLayer, f.HeroTraitTerms(hid));
                yield return Wait(0.6f);
                yield return Shot($"cardtext_traits_{hid}");
                TermPop.Close();
                yield return Wait(0.2f);
            }
            Debug.Log($"[Demo] 카드 글 점검 끝 — {n}장");
            Application.Quit(0);
        }
    }
}
