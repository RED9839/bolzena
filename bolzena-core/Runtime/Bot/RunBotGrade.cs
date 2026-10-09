using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    // 학년 진급 보상 고르기(봇) — 후보를 이벤트 결과(Outcome)로 옮겨 이벤트와 같은 값어치(OpsValue)로 재고, 가장 큰 것을 받는다.
    //   신탁은 그 카드의 가장 센 신탁, 빼기는 가장 약한 카드(WorstCard), 장비 · 교주 카드는 싸움 뒤 정리(ManageGear · SettleNeutrals)가 맡는다.
    public sealed partial class RunBot
    {
        static List<Outcome> AsOutcomes(GradeChoice c) => c.Kind switch
        {
            "gold" => new List<Outcome> { new Outcome { K = "gold", V = c.V } },
            "heal" => new List<Outcome> { new Outcome { K = "hp", V = c.V } },
            "gauge" => new List<Outcome> { new Outcome { K = "next", Next = new NextFight { Gauge = (int)c.V } } },
            "flash" => new List<Outcome> { new Outcome { K = "flash" } },
            "grace" => new List<Outcome> { new Outcome { K = "unique", N = 1 } },
            "remove" => new List<Outcome> { new Outcome { K = "remove" } },
            "equip" => new List<Outcome> { new Outcome { K = "equip", Grade = c.Grade } },
            "neutral" => new List<Outcome> { new Outcome { K = "neutral" } },
            "prep" => Enumerable.Range(0, Grades.PREP_FIGHTS).Select(_ => new Outcome { K = "next", Next = new NextFight { Hand = (int)c.V } }).ToList(),
            "mock" => Enumerable.Range(0, Grades.MOCK_FIGHTS).Select(_ => new Outcome { K = "next", Next = new NextFight { Buff = new Dictionary<string, int> { ["사기"] = (int)c.V } } }).ToList(),
            _ => new List<Outcome>(),
        };

        /// <summary>쌓인 진급 보상을 차례로 고른다.</summary>
        void TakeGrades(Run run)
        {
            int guard = 0;
            while (run.GradeOfferNow != null && guard++ < 10)
            {
                var off = run.GradeOfferNow;
                int best = Enumerable.Range(0, off.Choices.Count).OrderByDescending(i => OpsValue(run, AsOutcomes(off.Choices[i]))).First();
                var t = run.TakeGrade(best);
                if (t == null) break;
                if (t.Flash != null && t.Flash.Picks.Count > 0) run.TakeFlash(t.Flash.CardId, t.Flash.Picks.OrderByDescending(n => CardEff(t.Flash.CardId, n)).First());
                if (t.Remove) { var w = WorstCard(run); if (w != null) run.GradeRemove(w); }
            }
            SettleNeutrals(run);
        }
    }
}
