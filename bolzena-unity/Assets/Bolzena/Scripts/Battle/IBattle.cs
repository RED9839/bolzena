using System.Collections.Generic;

namespace Bolzena.Battle
{
    // 전투 규칙의 얇은 문. 화면은 이것만 부른다.
    // 지금은 CoreBattle(Bolzena.Core 어댑터)이 이 문 뒤에 있다.
    //
    // 부르는 쪽은 돌아온 이벤트 목록을 차례대로 연출한다. 규칙은 즉시 끝까지 계산하고(상태는 이미 바뀌어 있다),
    // 화면은 이벤트에 실린 값(HpAfter · BlockAfter …)으로 막대를 움직인다 — 그래서 연출 도중의 상태를 규칙에 묻지 않는다.
    public interface IBattle
    {
        BattleSnapshot Snapshot { get; }

        // 전투를 연다 — 첫 웨이브 · 첫 턴 · 첫 손패까지의 이벤트
        IReadOnlyList<BattleEvent> Begin();

        // 이 카드를 지금 낼 수 있는가(AP · 대상) — 못 내면 까닭
        bool CanPlay(int handIndex, out string reason);

        // 카드를 낸다. 신탁 카드면 epiphanyChoice 로 고른 것(0~)으로 바뀌어 나간다
        // branch — 두 갈래 카드(CardInfo.Choices)에서 고른 갈래 1 · 2(0 이면 없음)
        // spend — 소모량을 고르는 카드(SpendPromptOf 가 null 이 아닌 카드)에서 고른 수(0 이면 전부)
        IReadOnlyList<BattleEvent> PlayCard(int handIndex, int targetEnemy, int epiphanyChoice = -1, int branch = 0, int spend = 0);

        // 소모량을 고르는 카드(spend pick)면 후보 · 결과 미리보기. 아니면 null
        SpendPrompt SpendPromptOf(int handIndex);

        // 신탁 선택지 — 그 카드가 바뀔 수 있는 모습들(빛나는 카드가 아니면 빈 목록)
        IReadOnlyList<CardInfo> EpiphanyOptions(int handIndex);

        bool CanUlt(int hero, out string reason);
        IReadOnlyList<BattleEvent> UseUlt(int hero, int targetEnemy);
        bool UltNeedsTarget(int hero);

        // 미리보기 — 내 보면 적마다 얼마나(없으면 null 칸). 판은 안 바뀐다
        IReadOnlyList<PreviewFoe> PreviewCard(int handIndex, int targetEnemy);
        /// <summary>그 사도(파티 몇 번째)가 그 적을 치면 약점 공격인가(성격 · 공명은 늘 · 적 표식).</summary>
        bool WeakFor(int hero, int enemy);
        PreviewParty PreviewPartyOf(int handIndex);
        IReadOnlyList<PreviewFoe> PreviewUlt(int hero, int targetEnemy);

        // 턴을 넘긴다 — 손패 버림 · 적의 수 · 다음 턴 시작(또는 다음 웨이브)까지
        IReadOnlyList<BattleEvent> EndTurn();
    }
}
