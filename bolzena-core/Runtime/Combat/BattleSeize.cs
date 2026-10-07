using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 적의 손패 흡수(수 seize — 2026-10-07 적 리워크): 손의 무작위 카드 n 장(상태 · 저주 카드 빼고)을 그 적이 빼앗아 쥔다.
    /// 손이 비었으면(적의 차례 — 손은 턴 끝에 버렸다) 뽑을 더미 맨 위 카드(다음 턴에 뽑을 것)를 빼앗는다.
    /// 그 적을 격파하거나 · 쓰러뜨리거나 · 빼앗은 뒤 그 적에게 최대 HP 의 v%(없으면 R.SEIZE_PCT) 피해를 주면 모두 손으로 돌아온다(손이 가득하면 버린 더미).
    /// 쥔 장수는 그 적의 상태 칸 R.SEIZED 에 보인다. 카드 쪽지: 손 → "seize"(빼앗김) · "seize" → 손(되찾음).
    /// </summary>
    public sealed partial class Battle
    {
        /// <summary>그 적이 쥔 카드(화면 칩 풀이 · 시험).</summary>
        public IReadOnlyList<string> SeizedOf(Unit e) => e?.Seized ?? new List<string>();
        /// <summary>되찾기까지 남은 피해(0 이면 쥔 것 없음).</summary>
        public int SeizeLeft(Unit e) => e == null || e.Seized.Count == 0 ? 0 : Math.Max(0, e.SeizeNeed - e.SeizeDmg);

        /// <summary>시험 · 도구 — 적이 이 수를 지금 바로 한다(패시브처럼 — 차례 · 즉시 행동과 상관없이).</summary>
        public void FoeDo(Unit e, Intent it) { if (e != null && !e.Dead && it != null) ActEnemy(e, it, true); }

        void SeizeCards(Unit e, Intent it, string say)
        {
            // 손에서 무작위 — 손이 비었으면(적의 차례엔 손을 이미 버렸다) 뽑을 더미 맨 위(다음 턴에 뽑을 카드)부터
            bool Ok(string id) { var c = CardOf(id); return c != null && !c.IsStatus && !c.IsCurse; }
            var got = new List<string>();
            for (int k = 0; k < Math.Max(1, it.N); k++)
            {
                var hand = Hand.Where(Ok).ToList();
                string id = null, from = "hand";
                if (hand.Count > 0) { id = hand[Rng.Int(hand.Count)]; Hand.Remove(id); }
                else
                {
                    if (Draw.Count == 0 && Discard.Count > 0) { Draw = Shuffle(Rng, Discard.ToList()); Discard.Clear(); CardCue(null, "discard", "draw", "shuffle"); }
                    for (int i = Draw.Count - 1; i >= 0; i--) if (Ok(Draw[i])) { id = Draw[i]; Draw.RemoveAt(i); from = "draw"; break; }
                }
                if (id == null) break;
                e.Seized.Add(id); got.Add(id);
                CardCue(id, from, "seize", "seize");
            }
            if (got.Count == 0) { Say($"{e.Name}: {say} — 빼앗을 카드가 없다"); return; }
            int pct = it.V > 0 ? it.V : R.SEIZE_PCT;
            e.SeizeDmg = 0; e.SeizeNeed = Math.Max(1, Num.Round(e.MaxHp * pct / 100.0));
            SetStRaw(e, R.SEIZED, e.Seized.Count);
            StatusCue(e, $"{R.SEIZED} {e.Seized.Count}", true);
            Say($"{e.Name}: {say} (손패 {string.Join(" · ", got.Select(x => "「" + CardOf(x)?.Name + "」"))} 빼앗김 — 피해 {e.SeizeNeed} 로 되찾음)");
        }

        /// <summary>빼앗은 적이 파티에게 맞았다 — 쌓인 피해가 되찾는 값에 닿으면 돌려준다.</summary>
        void SeizeHurt(Unit e, int d)
        {
            if (e == null || e.Seized.Count == 0 || d <= 0) return;
            e.SeizeDmg += d;
            if (e.SeizeDmg >= e.SeizeNeed) SeizeBack(e, "피해");
        }

        /// <summary>쥔 카드를 모두 손으로(격파 · 처치 · 피해).</summary>
        void SeizeBack(Unit e, string why)
        {
            if (e == null || e.Seized.Count == 0) return;
            var back = e.Seized.ToList(); e.Seized.Clear(); e.SeizeDmg = 0; e.SeizeNeed = 0;
            SetStRaw(e, R.SEIZED, 0);
            foreach (var id in back)
            {
                if (Hand.Count < R.HAND_MAX) { Hand.Add(id); CardCue(id, "seize", "hand", "seizeBack"); }
                else { Discard.Add(id); CardCue(id, "seize", "discard", "seizeBack"); }
            }
            Say($"{e.Name}: {why} — 빼앗긴 카드 {back.Count}장을 되찾았다");
        }
    }
}
