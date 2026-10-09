using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 판 진행 중 사도 한마디(2026-10-09 사용자 — 트릭컬다움) — 휴식 · 상점 · 보상 · 이벤트 도착 때 편성 사도 하나가 말풍선 한 줄.
    //   ※ 지금은 부르는 곳이 없다(2026-10-09 사용자 결정: 판 중 대사는 보스 전투 시작 · 전투 승리 두 갈래만 — 전투 쪽 BattleDirector.Say.cs).
    //     대사(hero_lines 의 rest · shop · reward · event)와 이 도구는 나중에 다시 쓸 수 있게 남긴다.
    //   대사: hero_lines.json 의 rest · shop · reward · event(전투 쪽 victory · bossStart · lowhp 는 bolzena-unity BattleDirector 가 띄운다). 목소리 없이.
    //   모양: 사도 말풍선 진입점 HeroBubble.Show(HeroBubble.cs) — 서 있는 사도 머리 위에 1.8초.
    //   입력을 막지 않는다(레이캐스트 끔) · 기다림 없음 · 같은 판에서 같은 줄 · 같은 자리 되풀이 없음(HeroLines.PickFresh · FirstAt) · 설정 「사도 말풍선」 으로 끈다.
    public partial class Flow
    {
        /// <summary>전투 장면 위에 보상을 띄울 때(배경 없이) 사도 머리의 화면 좌표 — 전투 쪽(BattleDirector)이 꽂는다. 사도 키 → 화면 픽셀(없으면 null).</summary>
        public static Func<string, Vector2?> BattleHeadOnScreen;
        /// <summary>전투 화면 학년 공책 툴팁을 고정(true) · 풀기(false) — 전투 쪽 PartyHud 가 꽂는다(시범 캡처 -demo-grade).</summary>
        public static Action<bool> BattleGradeTip;
        /// <summary>전투 끝 학년 스티커 「착」 연출을 시작한 시각(Time.unscaledTime, 없으면 -1) — 전투 쪽 PartyHud 가 적는다(시범 캡처).</summary>
        public static float BattleGradeSlapAt = -1;

        /// <summary>파티에서 한 명 — 무작위(빼고 싶은 사도 avoid).</summary>
        HeroInfo SayPick(string avoid = null)
        {
            var keys = P.S.Party.Where(k => k != avoid).ToList();
            if (keys.Count == 0) keys = P.S.Party.ToList();
            if (keys.Count == 0) return null;
            return Roster.OfCore(keys[UnityEngine.Random.Range(0, keys.Count)]);
        }

        /// <summary>
        /// 사도 한마디 — head(부모의 왼쪽 아래 기준 좌표)가 그 사도 머리 꼭대기. spot 은 이 자리 이름(이번 판에 이미 말했으면 건너뜀, null 이면 늘).
        /// 말풍선은 delay 뒤 떠서 hold 초 머물다 사라진다.
        /// </summary>
        public void HeroSay(RectTransform parent, HeroInfo h, string kind, Vector2 head, string spot, float delay = 0.5f, float hold = 1.8f)
        {
            if (!Settings.HeroTalk || h == null || parent == null || P?.Run == null) return;
            if (spot != null && !HeroLines.FirstAt(P.Run, spot)) return;
            string line = HeroLines.PickFresh(h, kind, P.Run);
            if (string.IsNullOrEmpty(line)) return;
            Debug.Log($"[HeroSay] {kind} — {h.ko}: {line}");
            HeroBubble.Show(parent, h, line, Vector2.zero, head + new Vector2(0, 8), new Vector2(0.5f, 0), hold, delay, kind);
        }

        /// <summary>배경 없이 띄운 보상(전투 장면 위) — 전투 쪽이 알려 준 머리 위치. 모르면 null.</summary>
        Vector2? BattleHead(RectTransform root, string key)
        {
            var s = BattleHeadOnScreen?.Invoke(key);
            if (s == null) return null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, s.Value, null, out var local)) return null;
            return local - root.rect.min;
        }
    }
}
