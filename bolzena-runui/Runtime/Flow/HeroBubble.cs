using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 사도 말풍선 — 사도가 말하는 모든 곳의 단 하나의 진입점(2026-10-09): 로비(메인 사도 · 만지기) · 진급 · 졸업 · 판 진행(전투 승리 · 보스 시작 따위).
    //   부르는 쪽은 위치(anchor · pos · pivot — Ui.At 과 같은 뜻) · 대상 사도 · 글 · 시간만 넘긴다. 모양은 이 파일 안에만 둔다.
    //   모양(2026-10-09 사용자 결정 — D1 「원작 말풍선」, 「채팅 치듯 나온다」 를 고침):
    //     · 흰 둥근 몸통 + 두꺼운 갈색 테(원작 Common_TalkBubble_1 을 구운 RunArt/Ui/bubble_body — Tools~/bubble_art.py, 9칸)
    //     · 말하는 사도 쪽을 가리키는 꼬리(원작 Common_TalkBubbleTail_1 — bubble_tail 은 몸통 뒤, bubble_tail_fill 은 몸통 위에 덮어 이음매 테를 지운다)
    //       판 진행(anchor 0 · pos = 머리 꼭대기)은 꼬리 끝이 머리에 닿는다 — 풍선을 화면 안으로 밀면 꼬리 자리만 옮긴다. 로비는 오른쪽 아래(서 있는 사도), 진급 · 졸업은 왼쪽 아래.
    //     · 이름은 성격 색 리본(위 왼쪽, 살짝 기울게) — 동그란 얼굴은 없다(메신저처럼 보이던 까닭)
    //     · 글은 한 번에 통째로 「톡」(0.85 → 1.06 → 1, 0.25초) — 한 글자씩 찍지 않는다. 움직임 줄이기면 커지기 없이 나타나기만, 저사양이면 그림자를 뺀다
    //   kind 별로 크기만 다르다: lobby(폭 ≤ 360) · promote/graduate(≤ 620, 글 조금 크게) · 그 밖(판 진행, ≤ 360 · 화면 폭 40%).
    //   NPC(골디 상점 주인)는 사도가 아니라 W.Bubble 을 그대로 쓴다.
    public sealed class HeroBubble
    {
        public RectTransform Rect { get; private set; }
        TextMeshProUGUI text;
        HeroInfo hero;
        string kind;
        RectTransform parent, body, tail, tailFill;
        Vector2 anchor, pos, pivot;
        float maxW, fs;

        public bool Alive => Rect != null;

        // 원작 그림 치수(Tools~/bubble_art.py 와 짝) — 몸통 9칸 테두리 · 화면 배율, 꼬리 판(232×176)의 끝점 · 윗변
        const float BodyBorder = 120, BodyPpu = 4f;
        const float TailW = 48, TailCanvasW = 232, TailCanvasH = 176, TailTipX = 40, TailTipY = 152, TailTopY = 24;
        const float PadX = 22, PadTop = 20, PadBottom = 14, MinH = 60, TailInset = 6;
        static float TailScale => TailW / TailCanvasW;
        /// <summary>몸통 아래로 보이는 꼬리 길이(끝점 ~ 몸통 바닥).</summary>
        static float TailDrop => (TailTipY - TailTopY) * TailScale - TailInset;

        bool Run => kind != "lobby" && kind != "promote" && kind != "graduate";

        /// <summary>
        /// 말풍선을 띄운다. anchor · pos · pivot 은 부모 안 자리(Ui.At 과 같은 뜻 — pivot 이 pos 에 온다). 판 진행(anchor 0 · pivot 아래 가운데)은 pos 가 꼬리 끝(머리 꼭대기).
        /// hold &gt; 0 이면 그 초만큼 머물다 사라지고, 0 이면 남는다. kind 는 상황 이름(lobby · promote · graduate · victory · bossStart …) — 크기 고르기와 시범 찾기(이름 「herosay:갈래」)에 쓴다.
        /// </summary>
        public static HeroBubble Show(RectTransform parent, HeroInfo h, string line, Vector2 anchor, Vector2 pos, Vector2 pivot, float hold = 0, float delay = 0, string kind = null)
        {
            var b = new HeroBubble { hero = h, kind = kind ?? "-", parent = parent, anchor = anchor, pos = pos, pivot = pivot };
            b.Build(line, hold, delay);
            return b;
        }

        /// <summary>글만 바꾼다(로비 톡 · 만지기 반응) — 크기를 다시 맞추고 「톡」.</summary>
        public void SetLine(string line, float pop = 0.3f)
        {
            if (!Rect || !text) return;
            text.text = line;
            Layout();
            Pop(0, Mathf.Clamp(pop, 0.2f, 0.3f));
        }

        public void Close() { if (Rect) Object.Destroy(Rect.gameObject); Rect = null; }

        // ── 만들기 ──
        void Build(string line, float hold, float delay)
        {
            bool grade = kind == "promote" || kind == "graduate";
            maxW = kind == "lobby" ? 360 : grade ? 620 : Mathf.Min(360, Mathf.Max(240, parent.rect.width * 0.4f));
            fs = grade ? Theme.FsMd + 2 : Theme.FsMd;   // Ui.Text 가 글자 크게 배율을 곱한다
            var holder = Ui.Rect(Run ? "herosay:" + kind : kind == "lobby" ? "bubble" : "bubble:" + kind, parent);
            Rect = holder;
            holder.anchorMin = holder.anchorMax = anchor; holder.pivot = pivot;

            var bodySpr = Theme.Art("Ui/bubble_body", BodyBorder);
            var tailSpr = Theme.Art("Ui/bubble_tail");
            var fillSpr = Theme.Art("Ui/bubble_tail_fill");
            if (bodySpr == null) Debug.LogWarning("[HeroBubble] 원작 말풍선 그림 없음(RunArt/Ui/bubble_body — Tools~/copy_assets.py) — 둥근 판으로 대신");

            // 꼬리(테) → 몸통(그림자 · 종이) → 꼬리 속(이음매 덮기) 순서
            if (tailSpr != null) tail = TailImg(holder, tailSpr, "tail");
            body = Ui.Rect("body", holder);
            body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(0, TailDrop); body.offsetMax = Vector2.zero;
            if (!DisplayOptions.LowSpec)
            {
                var sh = Ui.Img(body, bodySpr ?? Theme.S("round", 20), new Color(0, 0, 0, 0.28f), "shadow");
                sh.pixelsPerUnitMultiplier = bodySpr != null ? BodyPpu : 1;
                sh.rectTransform.Fill(0, -5, 0, 5);
            }
            if (bodySpr != null)
            {
                var bi = Ui.Img(body, bodySpr, Color.white, "paper"); bi.pixelsPerUnitMultiplier = BodyPpu; bi.rectTransform.Fill();
            }
            else
            {
                var rim = Ui.Img(body, Theme.S("round", 20), Theme.BubbleRim, "rim"); rim.rectTransform.Fill(-4, -4, -4, -4);
                var bi = Ui.Img(body, Theme.S("round", 20), Color.white, "paper"); bi.rectTransform.Fill();
            }
            if (fillSpr != null) tailFill = TailImg(holder, fillSpr, "tailfill");

            text = Ui.Text(body, line, fs, Theme.BubbleInk, TextAlignmentOptions.MidlineLeft);
            text.rectTransform.Fill(PadX, PadBottom, PadX, PadTop);
            text.overflowMode = TextOverflowModes.Overflow;
            Ribbon(body);
            Layout();

            foreach (var g in holder.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;
            var cg = holder.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false; cg.interactable = false; cg.alpha = 0;
            if (Settings.ReduceMotion) hold *= 4;   // Tw 가 움직임 줄이기에서 지연을 1/4 로 줄인다 — 읽을 시간은 그대로 둔다
            Pop(delay, 0.25f, () =>
            {
                if (hold <= 0 || !holder) return;
                Tw.Run(holder, 0.25f, k => { if (cg) cg.alpha = 1 - k; }, Tw.Linear, hold, () => { if (holder) Object.Destroy(holder.gameObject); });
            });
        }

        RectTransform TailImg(RectTransform holder, Sprite s, string name)
        {
            var im = Ui.Img(holder, s, Color.white, name);
            var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(TailTipX / TailCanvasW, 1 - TailTipY / TailCanvasH);   // 꼬리 끝점
            rt.sizeDelta = new Vector2(TailCanvasW, TailCanvasH) * TailScale;
            return rt;
        }

        /// <summary>이름 리본 — 성격 색 알약 · 갈색 테 · 흰 글(갈색 외곽선), 몸통 위 왼쪽에 살짝 기울게 걸친다.</summary>
        void Ribbon(RectTransform b)
        {
            string nm = hero?.ko ?? "";
            if (string.IsNullOrEmpty(nm)) return;
            var col = Theme.NatureCardOf(hero?.nature);
            float rfs = Theme.FsMd - 2, rh = 30 * Settings.TextScale;
            var rim = Ui.Img(b, Theme.S("pill", 46), Theme.BubbleRim, "ribbon");
            var fill = Ui.Img(rim.transform, Theme.S("pill", 46), col, "fill"); fill.rectTransform.Fill(3, 3, 3, 3);
            var t = Ui.Title(rim.transform, nm, rfs, Color.white, TextAlignmentOptions.Center, "name");
            t.textWrappingMode = TextWrappingModes.NoWrap; t.overflowMode = TextOverflowModes.Overflow;
            t.Outline(0.24f, Theme.BubbleRim);
            float rw = t.GetPreferredValues(nm, 600, rh).x + 26;
            var rr = rim.rectTransform;
            rr.anchorMin = rr.anchorMax = new Vector2(0, 1);
            rr.pivot = new Vector2(0, 0.5f);
            rr.anchoredPosition = new Vector2(12, 2);
            rr.sizeDelta = new Vector2(rw, rh);
            rr.localRotation = Quaternion.Euler(0, 0, 4);
            t.rectTransform.Fill(8, 0, 8, 1);
        }

        /// <summary>글에 맞춰 크기 · 자리 · 꼬리를 다시 잡는다.</summary>
        void Layout()
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            float pref = text.GetPreferredValues(text.text, 4000, 200).x;
            var rb = body.Find("ribbon") as RectTransform;
            float ribbonW = rb ? rb.sizeDelta.x + 24 : 0;
            float innerMax = maxW - PadX * 2;
            float innerW = Mathf.Clamp(Mathf.Max(pref, ribbonW - PadX), 80, innerMax);
            if (pref > innerMax) text.textWrappingMode = TextWrappingModes.Normal;
            float th = text.GetPreferredValues(text.text, innerW, 0).y;
            float w = innerW + PadX * 2, h = Mathf.Max(MinH, th + PadTop + PadBottom);
            float total = h + TailDrop;
            Rect.sizeDelta = new Vector2(w, total);

            float tipX;
            if (Run && anchor == Vector2.zero)
            {
                // 머리 위 — 풍선은 화면 안으로 밀고(머리 띠 HUD 밑), 꼬리 끝은 머리에 그대로
                float x = Mathf.Clamp(pos.x, w * pivot.x + 12, parent.rect.width - w * (1 - pivot.x) - 12);
                float y = Mathf.Min(pos.y, parent.rect.height - total - 120);
                Rect.anchoredPosition = new Vector2(x, y);
                tipX = Mathf.Clamp(pos.x - (x - w * pivot.x), 26, w - 26);
            }
            else
            {
                Rect.anchoredPosition = pos;
                tipX = kind == "lobby" ? w - 54 : 58;
            }
            bool flip = kind == "lobby" || (Run && tipX > w * 0.5f);   // 꼬리 윗변이 몸통 안에 들게 — 오른쪽 쪽이면 뒤집어 왼쪽으로 기댄다
            foreach (var t in new[] { tail, tailFill })
            {
                if (!t) continue;
                t.anchoredPosition = new Vector2(tipX, 0);
                t.localScale = new Vector3(flip ? -1 : 1, 1, 1);
            }
        }

        /// <summary>「톡」 — 0.85 → 1.06 → 1(0.25초), 글은 통째로. 움직임 줄이기면 크기 그대로 나타나기만.</summary>
        void Pop(float delay, float dur, System.Action done = null)
        {
            var rt = Rect; var cg = rt.GetComponent<CanvasGroup>();
            bool calm = Settings.ReduceMotion;
            bool top = false;   // 뜨는 순간 맨 위로 — 화면이 말풍선 뒤에 그린 칸에 가리지 않게
            Tw.Run(rt, dur, k =>
            {
                if (!rt) return;
                if (k > 0 && !top) { top = true; rt.SetAsLastSibling(); }
                if (cg) cg.alpha = Mathf.Clamp01(k * 3f);
                float s = calm ? 1 : k < 0.6f ? Mathf.Lerp(0.85f, 1.06f, k / 0.6f) : Mathf.Lerp(1.06f, 1f, (k - 0.6f) / 0.4f);
                rt.localScale = new Vector3(s, s, 1);
            }, Tw.Linear, delay, done);
        }
    }
}
