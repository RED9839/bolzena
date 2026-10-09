using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 플레이 기록(RunRecord)용 전투 녹화 — 턴마다 낸 카드 · 받은 피해 · 막은 양 · 턴 끝 파티 HP · 고학년 · 적의 행동.
    /// 판이 싸움을 열 때(Run.OpenFight) 붙인다. 봇이 수를 읽으려고 복사한 판(Battle.Clone)에는 안 따라간다.
    /// 카드 표기: 맨 id(인스턴스 꼬리 「#n」 뗌) + 신탁이면 「/o번호」, 덱에 없던 카드(전투 중 생긴 것)는 앞에 「+」.
    /// </summary>
    public sealed class BattleTape
    {
        /// <summary>싸움을 열 때의 덱(맨 id) — 생긴 카드 가리기.</summary>
        public HashSet<string> Deck = new();
        public List<TurnRec> Turns = new();
        /// <summary>전투 중 고른 신탁 · 은총(내는 순간 — Battle.ApplyEpiphany).</summary>
        public List<EpiRec> Epis = new();

        public BattleTape() { }
        public BattleTape(IEnumerable<string> deck) { foreach (var id in deck) Deck.Add(GameData.NoInst(id)); }

        TurnRec Cur(Battle b)
        {
            var t = Turns.Count > 0 ? Turns[Turns.Count - 1] : null;
            if (t == null || t.T != b.Turn) { t = new TurnRec { T = b.Turn }; Turns.Add(t); }
            return t;
        }

        /// <summary>카드 한 장을 냈다.</summary>
        public void Card(Battle b, string cardId)
        {
            var k = GameData.NoInst(cardId);
            int n = b.Flash.TryGetValue(cardId, out var f) ? f : b.Flash.TryGetValue(k, out var f2) ? f2 : 0;
            Cur(b).C.Add((Deck.Contains(k) ? "" : "+") + k + (n > 0 ? "/o" + n : ""));
        }

        public void Ult(Battle b) => Cur(b).U++;

        /// <summary>적이 제 차례 · 즉시 행동으로 한 수(패시브 빼고) — 그 수로 파티가 받은 피해(막은 몫 포함).</summary>
        public void Foe(Battle b, Unit e, Intent it, int dealt)
        {
            if (e == null || it == null) return;
            (Cur(b).F ??= new List<FoeActRec>()).Add(new FoeActRec { E = e.Key, A = it.T, D = dealt });
        }

        public void Epi(Battle b, string cardId, Glow g, int choice)
        {
            if (g == null || choice < 0 || choice >= g.Count) return;
            Epis.Add(new EpiRec
            {
                Card = GameData.NoInst(cardId), Kind = g.Kind, Hero = g.Hero,
                Offer = g.Kind == "card" ? g.Picks.Select(p => p.N.ToString()).ToList() : g.Options.ToList(),
                Pick = g.Kind == "card" ? g.Picks[choice].N.ToString() : g.Options[choice],
            });
        }

        /// <summary>턴을 닫는다 — 이번 판(내 턴 + 적의 차례)에 받은 피해 · 막은 양 · 지금 파티 HP. 다음 턴 시작 · 전투 끝에 부른다(여러 번 불러도 된다).</summary>
        public void TurnEnd(Battle b)
        {
            if (b.Turn <= 0) return;
            var t = Cur(b);
            t.D = System.Math.Max(0, b.TakenNow - b.GuardedNow);
            t.B = b.GuardedNow;
            t.Hp = System.Math.Max(0, b.Pool.Hp);
        }
    }

    /// <summary>한 턴 — T 턴 번호 · C 낸 카드(차례대로) · D 받은 피해(HP) · B 막은 양 · Hp 턴 끝 파티 HP · U 고학년 쓴 수 · F 적의 수.</summary>
    public sealed class TurnRec
    {
        public int T;
        public List<string> C = new();
        public int D, B, Hp, U;
        public List<FoeActRec> F;
    }

    /// <summary>적의 수 하나 — E 적 id · A 수 갈래(attack · ult …) · D 파티가 받은 피해(막은 몫 포함).</summary>
    public sealed class FoeActRec
    {
        public string E, A;
        public int D;
    }

    /// <summary>전투 중 고른 빛 — Kind card(신탁 — Offer · Pick 은 신탁 번호) · hero(은총 — 카드 id).</summary>
    public sealed class EpiRec
    {
        public string Card, Kind, Hero, Pick;
        public List<string> Offer;
    }
}
