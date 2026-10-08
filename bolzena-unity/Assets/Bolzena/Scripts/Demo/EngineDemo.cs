using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Battle;
using Bolzena.Core;
using CoreFx = Bolzena.Core.Fx;

namespace Bolzena.Demo
{
    // -enginedemo — 소모량 고르기 창 · 다음 카드 강화 표시를 보려고 시험 카드를 데이터에 임시로 넣는다(메모리에서만 — 규칙 · 데이터 파일은 그대로).
    //   첫 손 다섯 장: 쌓기(고유 효과 6겹) · 소모 ×2(spend pick → perEvent → 피해 60%) · 강화(이 사도의 다음 카드 +50%) · 다른 사도의 시작 카드 한 장.
    //   콘텐츠에 spend pick · empower 를 쓰는 카드가 아직 없어서 만든 길이다. 실제 카드가 생기면 필요 없다.
    public static class EngineDemo
    {
        public const string Stack = "demo_stack", SpendA = "demo_spend_a", SpendB = "demo_spend_b", Empower = "demo_empower";
        public static bool On => Array.IndexOf(Environment.GetCommandLineArgs(), "-enginedemo") >= 0;

        public static void Install()
        {
            if (!On) return;
            CoreBattle.Fixture.Override = Build;
        }

        static CoreBattle.Fixture Build(GameData d)
        {
            var f = CoreBattle.Fixture.FromArgs(d) ?? CoreBattle.Fixture.Pilot(d);
            // 겹이 저절로 터지지 않는(stackReach · stackOver 규칙이 없는) 자기 주머니 고유 효과, 최대 6 이상 — 쌓아 둔 것을 카드로 골라 쓰게
            bool Calm(HeroDef h) => h.Keyword != null && (h.Keyword.Carrier ?? "self") == "self" && !h.Keyword.Mode && !h.Keyword.Hunt && (h.Keyword.Cap ?? 0) >= 6
                && !h.Keyword.Decays && !h.Keyword.EndClear && h.Keyword.EndDecay == 0
                && !h.Keyword.Rules.Any(r => r.When != null && (r.When.On == "stackReach" || r.When.On == "stackOver"));
            string h0 = f.Party.FirstOrDefault(h => d.Hero(h) != null && Calm(d.Hero(h)))
                ?? d.Heroes.Values.Where(Calm).OrderBy(h => h.Keyword.Rules.Count).ThenBy(h => h.Id, StringComparer.Ordinal).Select(h => h.Id).FirstOrDefault();
            if (h0 == null) { UnityEngine.Debug.LogWarning("[EngineDemo] 알맞은 고유 효과가 없어 시험 카드를 못 넣음"); return f; }
            if (!f.Party.Contains(h0)) f.Party = new List<string> { f.Party[0], h0, f.Party[1] };   // 가운데 자리에(첫 자리는 적의 공격을 받는다)
            string kw = d.Hero(h0).Keyword.Name;
            string other = f.Party.FirstOrDefault(h => h != h0);
            void Add(string id, string name, int cost, string type, params CoreFx[] fx)
            {
                d.Cards[id] = new CardDef { Id = id, Name = name, Hero = h0, Cost = cost, Type = type, Fx = fx.ToList() };
            }
            Add(Stack, "시험 쌓기", 0, "스킬", new CoreFx { K = FxK.Stack, Id = kw, V = 6 });
            foreach (var (id, nm) in new[] { (SpendA, "시험 소모 가"), (SpendB, "시험 소모 나") })
                Add(id, nm, 1, "공격", new CoreFx { K = FxK.Spend, Id = kw, Pick = true }, new CoreFx { K = FxK.PerEvent }, new CoreFx { K = FxK.Dmg, Ratio = 0.6, Target = "oneEnemy" });
            Add(Empower, "시험 강화", 0, "스킬", new CoreFx { K = FxK.Empower, Ratio = 0.5 });
            f.Deck = new List<string> { Stack, SpendA, Empower, SpendB };
            var otherStart = other != null ? d.Hero(other)?.Starter?.FirstOrDefault() : null;
            if (otherStart != null) f.Deck.Add(otherStart);
            f.Glow.Clear();
            f.WaveHp.Clear(); f.WaveHp.Add(4.0);
            UnityEngine.Debug.Log($"[EngineDemo] 시험 카드 — 사도 {h0} · 고유 효과 「{kw}」 · 손 {string.Join(", ", f.Deck)}");
            return f;
        }
    }
}
