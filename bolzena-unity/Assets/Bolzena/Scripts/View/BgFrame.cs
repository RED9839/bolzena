using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.View
{
    // 전투 배경 맞추기 — 화면을 늘 꽉 채우되(cover, 비율 그대로) 바닥 줄을 발 높이에 고정한다.
    //   배경은 원작 스크롤 배경 프리팹(prefab/scrollbg/stage)의 층을 트랜스폼 그대로 겹친 한 장(약 2047×1100, 16:9 보다 조금 세로가 길다).
    //   위쪽 깨진 띠가 없으므로 「위 쓸 수 있는 첫 줄」 은 0 이고, 표는 바닥 줄만 그림마다 다를 때 적는다.
    public static class BgFrame
    {
        // 이름 → (위 쓸 수 있는 첫 줄, 바닥 줄) — 1080 줄 기준(그림 높이에 비례해 바꾼다). 바닥 줄 = 사도 · 적 발이 닿는 그림 줄(유닛 발 높이 FootY 에 맞춘다)
        //   표에 없는 그림은 (0, 607) — 옛 맞춤의 발 자리(632/1080)를 바닥 층 기준으로 새 그림에 옮긴 값. 대부분 607±4
        static readonly Dictionary<string, (int top, int ground)> Table = new()
        {
            ["stage2_1"] = (0, 515),       // 엘리아스 숲 — 하늘 층이 높아 길(흙)이 그림 위쪽에 있다
            ["stage30_1"] = (0, 658),      // 노을 들판
            ["stage12_1"] = (0, 679),      // 호수
            ["globalstage1"] = (0, 581),   // 달밤 꽃밭
            ["stage44_1"] = (0, 617),      // 밀밭
        };
        const int Top0 = 0, Ground0 = 607;
        const float FootY = -0.5f;              // 싸움터에서 발 높이(사도 HeroPos y −0.3 ~ −0.8 의 가운데)
        const float MarginX = 0.6f, MarginY = 0.4f;   // 흔들림 · 줌 펀치에 끝이 안 보이게 남기는 몫(옛 맞춤과 같은 값)
        const float MinW = 18.4f;                // 옛 맞춤의 최소 폭

        public static string Name(Sprite s) => s == null ? null : (s.texture != null ? s.texture.name : s.name);

        // 카메라 크기에 맞춰 배경 크기 · 자리를 정한다(부모 = 싸움터, 쉴 때 싸움터 = 월드)
        public static void Fit(SpriteRenderer sr, Camera cam)
        {
            if (sr == null || sr.sprite == null || cam == null) return;
            var sp = sr.sprite;
            float ih = sp.rect.height, iw = sp.rect.width;
            var (t, g) = Table.TryGetValue(Name(sp) ?? "", out var v) ? v : (Top0, Ground0);
            float top = t * ih / 1080f, ground = g * ih / 1080f;
            float half = cam.orthographicSize, halfW = half * cam.aspect;
            // k = 그림 한 줄(픽셀)이 차지하는 월드 길이 — 폭을 덮고, 바닥 줄 위로 쓸 수 있는 부분이 화면 위끝을, 아래가 화면 아래끝을 덮는 가장 작은 값
            float k = Mathf.Max(MinW / iw, (halfW * 2 + MarginX * 2) / iw,
                                (half + MarginY - FootY) / Mathf.Max(1f, ground - top),
                                (half + MarginY + FootY) / Mathf.Max(1f, ih - ground));
            Make.Fit(sr, new Vector2(iw * k, ih * k));
            var p = sr.transform.localPosition;
            sr.transform.localPosition = new Vector3(0, FootY + (ground - ih / 2f) * k, p.z);   // 바닥 줄이 발 높이에
        }

        // -bg 이름: 시험 전투(-foes)의 배경을 고른다(배경 맞춤 점검 캡처)
        public static string Arg()
        {
            var a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, "-bg");
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }
    }
}
