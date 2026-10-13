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
        /// <summary>(엘다인 ▶E26) formExtend 로 늘린 턴의 합 — 전체 길이 상한(max)을 잰다.</summary>
        public int Ext;
        /// <summary>(엘다인 4차 ▶E76) 이 변신에 든 뒤 그 사도가 준 피해 — 새로 들 때 0.</summary>
        public int Dealt;
        public FormRt Copy() => (FormRt)MemberwiseClone();
    }

    /// <summary>
    /// 화면이 읽는 변신 — 누가(Hero) · 무엇으로(Id · Name) · 남은 턴(Left, 0 = 전투 끝까지) · 모습 키(Skin 스파인 스킨 · Anim 애니 접두어).
    /// </summary>
    public sealed class FormView
    {
        public string Hero, Id, Name, Skin, Anim;
        public int Left;
        /// <summary>(엘다인 4차) 태세인가(변신 대신 「태세」 로 보인다) · 이번 턴 보존 중인가(formKeep).</summary>
        public bool Stance, Kept;
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
            return fd == null ? null : new FormView { Hero = heroKey, Id = fd.Id, Name = fd.Name, Skin = fd.Skin, Anim = fd.Anim, Left = fd.Turns > 0 ? fr.Left : 0, Stance = fd.Stance, Kept = FormKept(heroKey) };
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
            var key = (v.Def.Id + "|" + id + "|" + v.Type, v.FlashN, fd.Id);   // 종류 — 전투 중 축복으로 공격 카드가 되면 다시 만든다
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
                if (b.Hits > 1) { var src = fx; fx = src.Select((f, i) => i > 0 && FxK.Pers.Contains(src[i - 1].K) ? f : MoreHits(f, b.Hits)).ToList(); }   // (2026-10-14 클로에 탑승) n연타 — 「1개당」 피해는 이미 여러 대라 그대로
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
        /// <summary>피해 조각의 친 횟수 × n — 고정 횟수 피해만(일의 값 · 지속 피해 · X · 겹 비례 횟수 · 1개당(per 뒤) 피해는 그대로).</summary>
        static Fx MoreHits(Fx f, int n)
        {
            if (f.K != FxK.Dmg || f.OfEvent > 0 || f.OfForm > 0 || f.OfShield || f.Dot || f.XHits || f.HitsPer != null) return f;
            var c = f.Copy(); c.Hits = f.HitsOr1 * n; if (c.KnownHits > 0) c.KnownHits *= n; return c;
        }

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
                var v = Blessed(id, Data.View(id, Flash.TryGetValue(id, out var n) ? n : 0, Party.Count > 0 ? Party[0].Key : null));
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
                case FxK.NoToggle: ctx.NoToggle = true; return true;   // 이번엔 짝 태세로 안 바뀜(우로스)
                case FxK.FormKeep:   // n턴간 태세 보존(엘다인 4차 — 린 「흑운 심법」) — 그동안 풀리는 계기 · 시간으로 안 풀린다
                    if (owner != null && owner.Side == Side.Party) { Counts["formKeep|" + owner.Key] = Turn + f.TurnsOr1; Say($"{owner.Name} — {f.TurnsOr1}턴간 {(FormDefOf(owner.Key)?.Name ?? "태세")} 보존"); StatusCue(owner, "태세 보존", true); }
                    return true;
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
                    cur.Left = fd.Turns; cur.Count = 0; cur.Ext = 0;
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
            // (엘다인 ▶E6) 드는 순간 변신 카드 · 계기 formOn(새로 들 때만 — 지속만 처음으로는 아니다)
            if (fd.OnEnter != null && fd.OnEnter.Count > 0 && Over == null) RunPassive(owner, fd.OnEnter, new FxCtx { Owner = owner, TargetIdx = AliveEnemies().FirstOrDefault()?.Idx ?? 0 }, $"{owner.Name} · 「{fd.Name}」 듦");   // 드는 순간(엘다인 3차)
            if (owner.Key == TraceHero) { TraceAdd("formIn"); TraceAdd("formInTurn:" + Turn); Counts["formAt|" + owner.Key] = Turn; }
            FormMakeCards(owner, fd);
            if (Over == null) Emit("formOn", new EmitInfo { Hero = owner.Key, Id = fd.Id, Who = owner });
        }

        /// <summary>변신을 푼다 — why: time(지속이 다함) · until(풀리는 계기) · card(효과 formEnd) · switch(다른 변신). off 효과가 돈다.</summary>
        public void EndForm(Unit owner, string why)
        {
            if (owner == null || !Forms.ContainsKey(owner.Key)) return;
            var fd = FormDefOf(owner.Key);
            var changed = FormHand(owner.Key, fd);
            Forms.Remove(owner.Key);
            if (owner.Key == TraceHero) { TraceAdd("formOff:" + why); if (Counts.TryGetValue("formAt|" + owner.Key, out var fa)) TraceAdd("formTurns", Turn - fa + 1); }
            string name = fd?.Name ?? "변신";
            Say($"{owner.Name} — 「{name}」 풀림");
            Cue("formOff", owner, new Cue { Hero = owner.Key, Id = fd?.Id, Name = name, Label = why });
            foreach (var id in changed) CardCue(id, "hand", "hand", "form");
            if (fd != null && fd.Off != null && fd.Off.Count > 0 && Over == null && !owner.Dead)
                RunPassive(owner, fd.Off, new FxCtx { Owner = owner, TargetIdx = AliveEnemies().FirstOrDefault()?.Idx ?? 0 }, $"{owner.Name} · 「{name}」 풀림");
            // (엘다인 ▶E22) 끝의 갈래 — 시간이 다 되면(반동) · 스스로 끝내면(보상)
            var tail = fd == null ? null : why == "time" ? fd.OffExpire : why == "card" ? fd.OffEnd : null;
            if (tail != null && tail.Count > 0 && Over == null && !owner.Dead)
                RunPassive(owner, tail, new FxCtx { Owner = owner, TargetIdx = AliveEnemies().FirstOrDefault()?.Idx ?? 0 }, $"{owner.Name} · 「{name}」 {(why == "time" ? "시간이 다 됨" : "스스로 끝냄")}");
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
                if (FormKept(key)) continue;   // 보존 중엔 시간이 줄지 않는다
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
                if (FormKept(key)) continue;   // 보존 중엔 풀리는 계기가 안 든다(엘다인 4차 태세)
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

        /// <summary>(우로스 선 · 악) 그 사도가 자기 카드를 낸 뒤 — 지금 태세에 toggle 이 있으면 짝 태세로(그 카드가 noToggle · 변신 · 해제를 했으면 안 바뀜).</summary>
        void FormToggle(Unit owner, FxCtx ctx, string formBefore)
        {
            if (owner == null || ctx.NoToggle || Over != null || !Forms.TryGetValue(owner.Key, out var fr) || fr.Id != formBefore) return;
            var fd = FormDefOf(owner.Key);
            if (fd?.Toggle == null) return;
            var to = Data.Hero(owner.Key)?.Forms?.FirstOrDefault(x => x.Id == fd.Toggle);
            if (to == null) return;
            Forms.Remove(owner.Key);   // 풀림 효과 · 계기 없이 갈아 든다(번갈아 드는 태세)
            Forms[owner.Key] = new FormRt { Id = to.Id, Left = to.Turns };
            Say($"{owner.Name} — 「{fd.Name}」 → 「{to.Name}」");
            Cue("formOn", owner, new Cue { Hero = owner.Key, Id = to.Id, Name = to.Name, Label = to.Skin, T = to.Anim, V = to.Turns });
            foreach (var id in FormHand(owner.Key, to)) CardCue(id, "hand", "hand", "form");
            if (Over == null) Emit("formOn", new EmitInfo { Hero = owner.Key, Id = to.Id, Who = owner });
            // 한 바퀴(시작 태세로 돌아옴 — 우로스 허물 → 본성 → 허물) — 계기 formLap
            if (Over == null && to.Id == Data.Hero(owner.Key)?.StartForm) Emit("formLap", new EmitInfo { Hero = owner.Key, Id = to.Id, Who = owner });
        }
        /// <summary>전투 시작 태세(heroes startForm).</summary>
        void StartForms()
        {
            foreach (var u in AliveParty().ToList())
            {
                var sf = Data.Hero(u.Key)?.StartForm;
                if (sf != null && !Forms.ContainsKey(u.Key) && Over == null) EnterForm(u, sf);
            }
        }

        /// <summary>그 사도의 태세(변신)가 보존 중인가(formKeep — 걸린 턴부터 turns 턴, 다음 내 턴 시작 전까지).</summary>
        public bool FormKept(string heroKey) => heroKey != null && Counts.Count > 0 && Counts.TryGetValue("formKeep|" + heroKey, out var t) && t > Turn;
        /// <summary>그 카드가 「그 변신 · 태세 중에만 사용 가능」 인데 지금 아니면 그 이름(되면 null).</summary>
        public string NeedFormWhy(CardView c)
        {
            var need = c?.NeedForm;
            if (need == null || c.Hero == null) return null;
            if (Forms.TryGetValue(c.Hero, out var fr) && (need == "*" || fr.Id == need)) return null;
            return need == "*" ? "변신" : Data.Hero(c.Hero)?.Forms?.FirstOrDefault(x => x.Id == need)?.Name ?? need;
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
