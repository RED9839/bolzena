using System;
using System.Collections.Generic;
using System.Linq;

namespace Bolzena.Core
{
    /// <summary>변신 중인 사도 하나의 상태(저장된다) — 변신 id · 남은 턴(0 = 전투 끝까지) · until every 의 셈.</summary>
    public sealed class FormRt
    {
        public string Id;
        public int Left;
        public int Count;
        public FormRt Copy() => (FormRt)MemberwiseClone();
    }

    /// <summary>
    /// 화면이 읽는 변신 — 누가(Hero) · 무엇으로(Id · Name) · 남은 턴(Left, 0 = 전투 끝까지) · 모습 키(Skin 스파인 스킨 · Anim 애니 접두어).
    /// </summary>
    public sealed class FormView
    {
        public string Hero, Id, Name, Skin, Anim;
        public int Left;
        public bool UntilFightEnd => Left <= 0;
    }

    /// <summary>
    /// 변신(2026-10-05 여섯째) — 효과 form 으로 사도가 변신 상태가 된다. 개인 층(그 사도에게만): 능력치(mods) · 카드 모습(cards · bonus) · 패시브(passives · replace).
    /// 같은 변신을 다시 쓰면 지속을 처음으로(겹치지 않는다) — 원작 트릭컬의 변신(빡침 · 꿈결 형상 · 백수공권)이 다시 쓰면 시간이 새로 도는 꼴이고,
    /// 겹치면 능력치 · 카드 덤이 곱으로 불어나 값어치를 셀 수 없다. 다른 변신이면 앞의 것을 푼다(off 가 돈다).
    /// 쪽지: formOn(Hero · Id · Name · Label = 모습 키 Skin · T = Anim · V = 턴, 0 = 전투 끝까지) · formOff(Hero · Id · Name · Label = 까닭 time · until · card · switch).
    /// 손의 카드 모습이 바뀌면 card 쪽지(hand → hand, Label "form").
    /// </summary>
    public sealed partial class Battle
    {
        /// <summary>사도 키 → 변신 상태(변신 중인 사도만).</summary>
        public Dictionary<string, FormRt> Forms = new();
        Dictionary<(string, int, string), CardView> formViews = new();
        Dictionary<string, List<RuleRt>> formRules = new();

        // ── 화면 API ──────────────────────────────────────────────────
        /// <summary>그 사도가 변신 중인가.</summary>
        public bool InForm(string heroKey) => heroKey != null && Forms.ContainsKey(heroKey);

        /// <summary>그 사도의 변신(아니면 null).</summary>
        public FormView FormOf(string heroKey)
        {
            if (heroKey == null || !Forms.TryGetValue(heroKey, out var fr)) return null;
            var fd = FormDefOf(heroKey);
            return fd == null ? null : new FormView { Hero = heroKey, Id = fd.Id, Name = fd.Name, Skin = fd.Skin, Anim = fd.Anim, Left = fd.Turns > 0 ? fr.Left : 0 };
        }

        /// <summary>그 사도의 모습 키(스파인 스킨) — 변신 중이 아니면 null(기본 모습).</summary>
        public string FormSkin(string heroKey) => FormOf(heroKey)?.Skin;

        /// <summary>그 사도가 지금 든 변신의 정의(아니면 null).</summary>
        public FormDef FormDefOf(string heroKey)
        {
            if (heroKey == null || !Forms.TryGetValue(heroKey, out var fr)) return null;
            return Data.Hero(heroKey)?.Forms?.FirstOrDefault(f => f.Id == fr.Id);
        }

        // ── 카드 모습 ─────────────────────────────────────────────────
        /// <summary>변신 중인 사도의 카드면 변신판 모습(카드 바꾸기 · 덤), 아니면 그대로.</summary>
        CardView FormCard(string id, CardView v)
        {
            if (v?.Hero == null || !Forms.TryGetValue(v.Hero, out var fr)) return v;
            var fd = FormDefOf(v.Hero);
            if (fd == null) return v;
            var key = (v.Def.Id + "|" + id, v.FlashN, fd.Id);
            if (formViews.TryGetValue(key, out var hit)) return hit;
            var o = v;
            if (fd.Cards != null && fd.Cards.TryGetValue(GameData.BaseId(id), out var into) && Data.Card(into) is CardDef nd)
            {   // 카드 바꾸기 — 고른 신탁 번호는 변신판에 그 신탁이 있으면 잇는다
                int n = v.FlashN > 0 && v.FlashN <= nd.Oracles.Count ? v.FlashN : 0;
                o = new CardView(id, nd, n > 0 ? nd.Oracles[n - 1] : null, n);
                o.Target = Data.TargetOf(o.Fx);
            }
            foreach (var b in fd.Bonus ?? new List<FormBonus>())
            {
                if (b.Card != null && GameData.BaseId(id) != b.Card) continue;
                if (b.Type != null && o.Type != b.Type) continue;
                if (b.Unique && !v.Unique) continue;
                if (b.Tag != null && !o.HasTag(b.Tag)) continue;
                var fx = o.Fx.Select(f => b.Ratio > 0 && (f.K == FxK.Dmg || f.K == FxK.Extra) && f.OfEvent <= 0 ? Scaled(f, b.Ratio) : f).ToList();
                // 덤 효과는 조건 없이 돈다 — 첫 조건 · 때 붙은 마디(when) 앞에 끼운다(뒤에 붙이면 앞 조건에 걸린다)
                int at = fx.FindIndex(f => FxK.Conditions.Contains(f.K));
                fx.InsertRange(at < 0 ? fx.Count : at, b.Fx ?? new List<Fx>());
                var tags = o.Tags.ToList();
                foreach (var t in b.Tags ?? new List<string>()) if (!tags.Contains(t)) tags.Add(t);
                o = new CardView(o, tags, fx);
                o.Target = Data.TargetOf(o.Fx);
            }
            formViews[key] = o;
            return o;
        }

        static Fx Scaled(Fx f, double r) { var c = f.Copy(); c.Ratio = Math.Round(f.Ratio * r, 4); return c; }

        /// <summary>봇의 카드 값 캐시 키 — 그 카드의 주인이 든 변신(없으면 "").</summary>
        public string FormKeyOf(string cardId)
        {
            if (Forms.Count == 0) return "";
            var h = Data.View(cardId, 0, Party.Count > 0 ? Party[0].Key : null)?.Hero;
            return h != null && Forms.TryGetValue(h, out var fr) ? fr.Id : "";
        }

        /// <summary>손에서 변신으로 모습이 바뀌는 카드(그 사도의 것).</summary>
        List<string> FormHand(string heroKey, FormDef fd)
        {
            var o = new List<string>();
            if (fd == null) return o;
            foreach (var id in Hand.Distinct())
            {
                var v = Data.View(id, Flash.TryGetValue(id, out var n) ? n : 0, Party.Count > 0 ? Party[0].Key : null);
                if (v?.Hero != heroKey) continue;
                var b = GameData.BaseId(id);
                bool hit = (fd.Cards != null && fd.Cards.ContainsKey(b)) || (fd.Bonus ?? new List<FormBonus>()).Any(x => (x.Card == null || x.Card == b) && (x.Type == null || x.Type == v.Type) && (!x.Unique || v.Unique));
                if (hit) o.Add(id);
            }
            return o;
        }

        // ── 들어가기 · 풀기 ───────────────────────────────────────────
        bool FxForm(Fx f, FxCtx ctx)
        {
            var owner = ctx.Owner;
            switch (f.K)
            {
                case FxK.Form: if (owner != null && owner.Side == Side.Party && owner.BodyRef != null) EnterForm(owner, f.Id); return true;
                case FxK.FormEnd: if (owner != null && owner.Side == Side.Party) EndForm(owner, "card"); return true;
            }
            return false;
        }

        /// <summary>변신한다 — 같은 변신이면 지속을 처음으로, 다른 변신이면 앞의 것을 풀고 새로.</summary>
        public void EnterForm(Unit owner, string formId)
        {
            var fd = Data.Hero(owner.Key)?.Forms?.FirstOrDefault(x => x.Id == formId);
            if (fd == null) { Say($"{owner.Name}: 변신 「{formId}」 이 없다"); return; }
            if (Forms.TryGetValue(owner.Key, out var cur))
            {
                if (cur.Id == formId)
                {
                    cur.Left = fd.Turns; cur.Count = 0;
                    Say($"{owner.Name} — 「{fd.Name}」 지속이 처음으로{(fd.Turns > 0 ? $"({fd.Turns}턴)" : "")}");
                    Cue("formOn", owner, new Cue { Hero = owner.Key, Id = fd.Id, Name = fd.Name, Label = fd.Skin, T = fd.Anim, V = fd.Turns });
                    return;
                }
                EndForm(owner, "switch");
                if (Over != null) return;
            }
            Forms[owner.Key] = new FormRt { Id = fd.Id, Left = fd.Turns };
            Say($"{owner.Name} — 변신 「{fd.Name}」{(fd.Turns > 0 ? $" ({fd.Turns}턴)" : " (전투 끝까지)")}");
            Cue("formOn", owner, new Cue { Hero = owner.Key, Id = fd.Id, Name = fd.Name, Label = fd.Skin, T = fd.Anim, V = fd.Turns });
            foreach (var id in FormHand(owner.Key, fd)) CardCue(id, "hand", "hand", "form");
        }

        /// <summary>변신을 푼다 — why: time(지속이 다함) · until(풀리는 계기) · card(효과 formEnd) · switch(다른 변신). off 효과가 돈다.</summary>
        public void EndForm(Unit owner, string why)
        {
            if (owner == null || !Forms.ContainsKey(owner.Key)) return;
            var fd = FormDefOf(owner.Key);
            var changed = FormHand(owner.Key, fd);
            Forms.Remove(owner.Key);
            string name = fd?.Name ?? "변신";
            Say($"{owner.Name} — 「{name}」 풀림");
            Cue("formOff", owner, new Cue { Hero = owner.Key, Id = fd?.Id, Name = name, Label = why });
            foreach (var id in changed) CardCue(id, "hand", "hand", "form");
            if (fd != null && fd.Off != null && fd.Off.Count > 0 && Over == null && !owner.Dead)
                RunPassive(owner, fd.Off, new FxCtx { Owner = owner, TargetIdx = AliveEnemies().FirstOrDefault()?.Idx ?? 0 }, $"{owner.Name} · 「{name}」 풀림");
        }

        /// <summary>내 턴 시작 — 턴으로 재는 변신의 남은 턴을 1 줄이고 다하면 푼다.</summary>
        /// <summary>고유 효과가 최대(cap)에 닿았다 — onMax 의 변신(이미 그 변신 중이면 아무 일 없다) · 카드 만들기 · 다음 카드 강화. consume 이면 겹을 다 쓴다.</summary>
        void FormOnMax(KwRt kw, int before, int after)
        {
            var om = kw.Def.OnMax;
            int cap = kw.Def.Cap ?? 0;
            if (cap <= 0 || before >= cap || after < cap || kw.Carrier != "self") return;
            var owner = HeroUnit(kw.Owner);
            if (owner == null || owner.Dead) return;
            if (om.Form != null && Forms.TryGetValue(owner.Key, out var cur) && cur.Id == om.Form) return;
            Say($"{owner.Name} — 「{kw.Id}」 최대!");
            if (om.Consume) { AddStack(owner.Key, kw.Id, -StackOf(owner.Key, kw.Id)); KwGone(kw.Id, kw.Owner, owner, false); if (Over != null) return; }
            // 1단계(2026-10-08) — 다 차면 저절로 터지는 대신 시그니처 카드를 손에 만들기 · 다음 카드 강화(고르는 맛을 남긴다)
            if (om.Make != null) { var (prev, seq0) = (Acting, ActSeq); Acting = owner.Key; ActSeq = ++SeqN; try { Make(om.Make, Math.Max(1, om.N), owner); } finally { Acting = prev; ActSeq = seq0; } return; }
            if (om.Empower != null) { EmpowerAdd(om.Empower == "any" ? "*" : owner.Key, om.Ratio); return; }
            if (om.Form != null) EnterForm(owner, om.Form);
        }

        void FormTick()
        {
            if (Forms.Count == 0) return;
            foreach (var key in Forms.Keys.ToList())
            {
                if (Over != null) return;
                if (!Forms.TryGetValue(key, out var fr)) continue;
                var fd = FormDefOf(key);
                if (fd == null) { Forms.Remove(key); continue; }
                if (fd.Turns <= 0) continue;
                fr.Left--;
                if (fr.Left <= 0) EndForm(HeroUnit(key), "time");
            }
        }

        /// <summary>일이 났다 — 풀리는 계기(until)가 맞는 변신을 푼다.</summary>
        void FormUntil(string ev, EmitInfo info)
        {
            foreach (var key in Forms.Keys.ToList())
            {
                if (Over != null) return;
                if (!Forms.TryGetValue(key, out var fr)) continue;
                var w = FormDefOf(key)?.Until;
                var owner = HeroUnit(key);
                if (w == null || owner == null || owner.Dead) continue;
                if (!Matches(owner, w, ev, info, null)) continue;
                if (w.Every > 1 && ++fr.Count % w.Every != 0) continue;
                EndForm(owner, "until");
            }
        }

        /// <summary>변신이 덧붙이는 패시브(규칙) — 변신마다 한 번 만들어 둔다.</summary>
        List<RuleRt> FormRules(FormDef fd)
        {
            if (fd.Passives == null || fd.Passives.Count == 0) return null;
            if (!formRules.TryGetValue(fd.Id, out var l)) formRules[fd.Id] = l = fd.Passives.Select(r => new RuleRt { R = r, Form = fd.Id }).ToList();
            return l;
        }

        /// <summary>변신의 능력치 증감(StatMod 가 더한다).</summary>
        double FormMod(Unit u, string stat)
        {
            if (Forms.Count == 0 || u == null || u.Side != Side.Party || u.BodyRef == null || !Forms.ContainsKey(u.Key)) return 0;
            var fd = FormDefOf(u.Key);
            return fd?.Mods != null && fd.Mods.TryGetValue(stat, out var v) ? v : 0;
        }
    }
}
