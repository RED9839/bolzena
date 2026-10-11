using Bolzena.RunUI;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena
{
    // 전투 중 사도 한마디(2026-10-09 사용자 — 트릭컬다움) — 보스 전투 시작 · 전투 승리 직후(승리 목소리와 같은 맨 앞 사도) 두 갈래만.
    //   승리는 엘리트 · 보스면 늘, 일반 전투는 25%. 위기(lowhp)는 대사만 표에 두고 부르지 않는다(사용자 결정 — SayLow 는 나중에 쓸 때를 위해 남김).
    //   대사: runui hero_lines.json(bossStart · victory) · 말풍선: 사도 말풍선 진입점(runui HeroBubble.Show)을 덮개 캔버스에 띄운다.
    //   기다림 없음 — 띄우고 곧장 흐름을 잇는다(입력을 막지 않는다). 판에서 연 싸움만(전투 시범은 판이 없어 말하지 않는다) · 설정 「사도 말풍선」.
    public partial class BattleDirector
    {
        RectTransform sayRoot;
        bool saidStart, saidLow;
        UnitView victoryHero;   // 승리 목소리를 낸 사도(무작위) — 승리 한마디도 같은 사도
        bool spawnVoiced, victoryVoiced;   // 그 목소리가 실제로 나왔으면 같은 사도의 말풍선은 생략(목소리 없는 사도 · 목소리 끔일 때만 말풍선)

        /// <summary>말풍선 덮개 — 화면 크기 캔버스(판 화면과 같은 기준 해상도), 레이캐스터 없음.</summary>
        RectTransform SayRoot()
        {
            if (sayRoot) return sayRoot;
            var go = new GameObject("HeroSay", typeof(RectTransform));
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 40;
            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = Theme.Compact ? new Vector2(1280, 720) : new Vector2(1600, 900);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            sc.referencePixelsPerUnit = 100;
            sayRoot = (RectTransform)go.transform;
            return sayRoot;
        }

        /// <summary>사도 머리 위 화면 픽셀(판 화면이 배경 없이 보상을 띄울 때도 쓴다).</summary>
        Vector2? HeadScreen(UnitView u)
        {
            var cam = Cam ? Cam : Camera.main;
            if (!u || !cam) return null;
            var p = cam.WorldToScreenPoint(u.Top + new Vector3(0, 0.15f, 0));
            return new Vector2(p.x, p.y);
        }

        /// <summary>그 사도가 그 갈래 한 줄 — 머리 위에 1.7초.</summary>
        HeroBubble HeroSay(UnitView u, string kind, float hold = 1.7f, string shape = null)
        {
            var port = BattleBridge.Fight?.Port;
            if (!Settings.HeroTalk || !u || port?.Run == null) return null;
            var h = Roster.OfCore(CoreIdOf(u));
            var line = HeroLines.PickFresh(h, kind, port.Run);
            if (string.IsNullOrEmpty(line) || !(HeadScreen(u) is Vector2 s)) return null;
            var root = SayRoot();
            Canvas.ForceUpdateCanvases();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, s, null, out var local)) return null;
            Debug.Log($"[HeroSay] {kind} — {h.ko}: {line}");
            return HeroBubble.Show(root, h, line, Vector2.zero, local - root.rect.min + new Vector2(0, 8), new Vector2(0.5f, 0), hold, 0.1f, shape ?? kind);   // 사도 말풍선 진입점(runui HeroBubble.cs)
        }

        // ── 공백 한마디(2026-10-09 사용자 결정) — 말풍선 글은 이것만 띄운다(HeroBubble.AllKinds 꺼짐).
        //   내 턴에 입력 없이 IdleSecs 초가 지나면 편성 사도 한 명이 머리 위에 한 줄(IdleShow 초) — 그 뒤 공백이 이어지면 15~20초 간격, 한 싸움 최대 IdleMax 번.
        //   입력(카드 집기 · 커서 움직임 · 누르기) · 창이 열림이 생기면 곧바로 닫는다. 내 턴이 아닐 때(IdleTick 을 입력 대기 루프에서만 부른다) · 설정 「사도 말풍선」 끔이면 없다.
        //   줄: 파티 HP 30% 아래 lowhp · 보스전 bossStart · 그 밖 idle(hero_lines.json). 목소리는 없이 글만.
        public const float IdleSecs = 8f, IdleShow = 2.5f, IdleGapMin = 15f, IdleGapMax = 20f;
        public const int IdleMax = 3;
        float idleT, idleNeed = IdleSecs;
        Vector2 idlePos;
        UnitView idleLastHero;
        /// <summary>점검 — 이번 싸움에서 공백 한마디를 띄운 횟수 · 지금 떠 있는 말풍선.</summary>
        public int IdleCount { get; private set; }
        public HeroBubble IdleShown { get; private set; }

        void IdleClose() { if (IdleShown != null) IdleShown.Close(); IdleShown = null; }

        /// <summary>내 턴 입력 대기 루프에서 프레임마다.</summary>
        void IdleTick()
        {
            var pos = PointerInput.Pos;
            bool input = PointerInput.Down || PointerInput.Held || PointerInput.RightDown || (pos - idlePos).sqrMagnitude > 0.0004f || Hand.HasSelection || Modal.Open != null;
            idlePos = pos;
            if (input) { idleT = 0; IdleClose(); return; }
            if (IdleShown != null && !IdleShown.Alive) IdleShown = null;   // 시간이 다 돼 스스로 사라졌다
            if (IdleShown != null) return;
            if (Over || IdleCount >= IdleMax || !Settings.HeroTalk) return;
            idleT += Time.unscaledDeltaTime;
            if (idleT < idleNeed) return;
            idleT = 0;
            var port = BattleBridge.Fight?.Port;
            if (port?.Run == null) return;
            var alive = Heroes.FindAll(x => x && x != idleLastHero);
            if (alive.Count == 0) alive = Heroes.FindAll(x => x);
            if (alive.Count == 0) return;
            var u = alive[Random.Range(0, alive.Count)];
            int max = Battle?.Snapshot?.PartyMaxHp ?? 0, hp = Battle?.Snapshot?.PartyHp ?? max;
            string kind = max > 0 && hp < max * 0.3f ? "lowhp" : port.IsBoss ? "bossStart" : "idle";
            var b = HeroSay(u, kind, IdleShow, "idle");
            if (b == null) return;
            IdleShown = b; idleLastHero = u; IdleCount++;
            idleNeed = Random.Range(IdleGapMin, IdleGapMax);
        }

        /// <summary>판 화면이 이 장면 위에 보상을 띄울 때 사도 머리 위치를 물을 수 있게.</summary>
        void SayHook()
        {
            saidStart = saidLow = false; spawnVoiced = victoryVoiced = false;
            idleT = 0; idleNeed = IdleSecs; IdleCount = 0; IdleShown = null; idleLastHero = null;
            Flow.BattleHeadOnScreen = key => this ? HeadScreen(Heroes.Find(x => x && CoreIdOf(x) == key)) : null;
        }

        /// <summary>유닛의 규칙 쪽 id(판의 파티 키) — 유닛 이름은 스파인 폴더 이름이라 스냅샷의 같은 자리에서 읽는다.</summary>
        string CoreIdOf(UnitView u)
        {
            int i = Heroes.IndexOf(u);
            var hs = Battle?.Snapshot?.Heroes;
            return hs != null && i >= 0 && i < hs.Count ? hs[i].Id : u.name.Replace("hero_", "");
        }

        UnitView AnyHero()
        {
            var alive = Heroes.FindAll(x => x);
            return alive.Count > 0 ? alive[Random.Range(0, alive.Count)] : null;
        }

        /// <summary>보스 싸움(층 보스 · 클론 보스)의 등장 띠 뒤에 한 번. 엘리트는 말하지 않는다.</summary>
        void SayStart(bool bossWave)
        {
            if (saidStart || !bossWave) return;
            saidStart = true;
            var u = AnyHero();
            if (spawnVoiced && u == Heroes[0]) return;   // 등장 목소리를 낸 그 사도
            HeroSay(u, "bossStart");
        }

        /// <summary>승리 직후 — 승리 목소리를 낸 사도(편성 셋 중 무작위). 엘리트 · 보스는 늘, 일반 전투는 25%.</summary>
        void SayVictory()
        {
            var port = BattleBridge.Fight?.Port;
            if (port?.Run == null || Heroes.Count == 0) return;
            bool big = port.IsBoss || (port.Run.S.Elite && port.Run.S.EventFight == null);
            if (!big && Random.value >= 0.25f) return;
            if (victoryVoiced) return;   // 승리 목소리가 나왔다 — 글은 생략
            HeroSay(victoryHero ?? Heroes[0], "victory");
        }

        /// <summary>파티 HP 가 최대의 30% 아래로 처음 떨어졌을 때 한 번 — 지금은 부르지 않는다(2026-10-09 사용자 결정).</summary>
        void SayLow(int hpAfter)
        {
            if (saidLow || Over || hpAfter <= 0) return;
            int max = Battle?.Snapshot?.PartyMaxHp ?? 0;
            if (max <= 0 || hpAfter >= max * 0.3f) return;
            saidLow = true;
            HeroSay(AnyHero(), "lowhp");
        }
    }
}
