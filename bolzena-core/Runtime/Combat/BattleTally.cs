using System.Collections.Generic;

namespace Bolzena.Core
{
    /// <summary>
    /// 전투 기록 한 줄 — 한 사도가 이 싸움(또는 판 전체)에서 낸 몫(2026-10-11, 데미지 지표).
    /// Dealt — 적에게 들어간 피해: 실제로 깎인 HP(넘친 피해는 빼고) + 적의 방어 · 실드를 벗긴 양.
    /// Shield — 파티에 얹은 방어 · 실드(얻은 그대로 — 나중에 쓰였는지는 보지 않는다).
    /// Heal — 파티 HP 를 실제로 채운 양(최대 HP 를 넘친 회복은 빼고).
    /// </summary>
    public sealed class HeroTally
    {
        public int Dealt, Shield, Heal;
        public HeroTally Copy() => (HeroTally)MemberwiseClone();
        public void Add(HeroTally o) { if (o == null) return; Dealt += o.Dealt; Shield += o.Shield; Heal += o.Heal; }
    }

    public sealed partial class Battle
    {
        /// <summary>
        /// 사도별 전투 기록 — 사도 키 → 몫. 피해 · 실드 · 회복을 낸 사도(카드 주인 · 상태를 건 사도 · 키워드 주인)의 몫으로 센다.
        /// 주인을 모르는 몫(교주 카드 · 장비 · 사건)은 어느 사도에게도 넣지 않는다. 저장 · 복사에 함께 간다.
        /// </summary>
        public Dictionary<string, HeroTally> Tally = new();

        /// <summary>몫을 받을 사도 — 파티의 사도 하나만(파티 몸 · 적 · 없음은 null).</summary>
        HeroTally TallyOf(Unit by)
        {
            if (by == null || by.Side != Side.Party || by.BodyRef == null || by.Key == null) return null;
            if (!Tally.TryGetValue(by.Key, out var t)) Tally[by.Key] = t = new HeroTally();
            return t;
        }

        void TallyDealt(Unit by, int v) { if (v > 0 && TallyOf(by) is HeroTally t) t.Dealt += v; }
        void TallyShield(Unit by, int v) { if (v > 0 && TallyOf(by) is HeroTally t) t.Shield += v; }
        void TallyHeal(Unit by, int v) { if (v > 0 && TallyOf(by) is HeroTally t) t.Heal += v; }

        /// <summary>
        /// 그 상태를 건 사도 — 지속 피해(고통 · 균열)는 마지막에 건 사도(Body.Giver), 그 밖은 건 쪽 기록(Src)에서 겹이 가장 많은 사도.
        /// 상태가 줄거나 사라지기 전에 불러야 한다(사라지면 기록도 지워진다). 봇이 뜬 판은 Src 가 비어 있어 null 일 수 있다 — 기록만 비고 판은 같다.
        /// </summary>
        Unit TallyBy(Unit holder, string id)
        {
            if (holder == null) return null;
            if ((id == "고통" || id == "균열") && holder.Body.Giver.TryGetValue(id, out var gi))
                foreach (var h in Party) if (h.Idx == gi) return h;
            if (!SrcBag(holder, id).TryGetValue(id, out var d)) return null;
            string best = null; int most = 0;
            foreach (var kv in d)
                if (kv.Value > most && kv.Key.StartsWith("hero:", System.StringComparison.Ordinal)) { best = kv.Key.Substring(5); most = kv.Value; }
            return HeroUnit(best);
        }

        /// <summary>복사 · 저장용 — 속까지 베낀다.</summary>
        internal static Dictionary<string, HeroTally> CopyTally(Dictionary<string, HeroTally> d)
        {
            var o = new Dictionary<string, HeroTally>(d.Count);
            foreach (var kv in d) o.Add(kv.Key, kv.Value.Copy());
            return o;
        }
    }
}
