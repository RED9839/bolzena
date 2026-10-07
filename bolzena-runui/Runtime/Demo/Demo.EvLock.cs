using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 잠긴 칸 · 연속 이벤트 깃발(-demo-events 안에서, 2026-10-07 사용자 「조건이 안 맞는 선택지는 잠금으로 보여 주기」 · 「연속 이벤트」)
    //   ① 파티에 없는 사도 조건 선택지가 있는 이벤트 — 잠긴 칸(어둡게 · 자물쇠 · 조건 글)이 보이고 고를 수 없는지
    //   ② 깃발을 세우는 선택지를 고른 뒤, 그 깃발을 읽는 이벤트의 선택지가 바뀌는지 — 앞 · 뒤를 찍는다
    public partial class Demo
    {
        IEnumerator EvLock_()
        {
            var evs = f.P.Data.Events;
            f.P.S.Gold = Mathf.Max(f.P.S.Gold, 500); f.P.S.MindBreak = 0;
            // ① 잠긴 칸
            var party = f.P.S.Party;
            var lockEv = evs.FirstOrDefault(e => e.Options.Count(o => o.Hero != null && !o.Hero.Any(party.Contains)) >= 1 && e.Options.Count >= 3);
            Expect(lockEv != null, "잠긴 칸: 파티에 없는 사도 조건 선택지가 있는 이벤트");
            if (lockEv != null)
            {
                OpenEvent(lockEv.Id);
                yield return Screen_("event", 1.6f);
                yield return Shot($"evlock_{lockEv.Id}");
                var opts = f.P.Options(lockEv);
                int li = opts.FindIndex(o => o.Hero != null && !o.Hero.Any(party.Contains));
                string why = li >= 0 ? f.P.LockOf(opts[li]) : null;
                Debug.Log($"[Demo] 잠긴 칸 {lockEv.Id} 「{(li >= 0 ? opts[li].Label : "-")}」 — {why}");
                Expect(li >= 0 && why != null && why.Contains("파티에 있어야"), $"잠긴 칸: {lockEv.Id} 사도 조건 칸이 보이고 조건 글이 붙는다 ({why})");
                if (li >= 0)
                {
                    yield return Press("event.opt" + li, 0.4f);
                    Expect(f.P.S.Event != null && f.P.S.Event.Phase == "choose", "잠긴 칸: 눌러도 고르지 않는다");
                    yield return Shot($"evlock_{lockEv.Id}_pressed");
                }
                CloseModals();
            }
            // ② 깃발 줄기 — 깃발을 세우는(고르기만 하면 되는) 선택지 → 그 깃발을 읽는 이벤트
            var chains = evs.SelectMany(e => e.Options.Where(o => o.Gamble == null && o.Judge == null && o.Fight == null && o.Hero == null && o.Race == null && o.When == null)
                                                      .SelectMany(o => (o.Out ?? new List<Core.Outcome>()).Where(x => x.K == "flag").Select(x => (e, o, flag: x.Id))))
                            .Where(c => evs.Any(r => r != c.e && r.Options.Any(o => o.Flag == c.flag)))
                            .ToList();
            Expect(chains.Count > 0, "깃발 줄기: 깃발을 세우는 선택지와 읽는 이벤트가 있다");
            foreach (var (e, o, flag) in chains.Take(3))
            {
                var reader = evs.First(r => r != e && r.Options.Any(x => x.Flag == flag));
                f.P.S.Flags.Remove(flag);
                OpenEvent(reader.Id);
                yield return Screen_("event", 1.4f);
                yield return Shot($"evflag_{flag}_before");
                var before = f.P.Options(reader).Select(x => x.Label).ToList();
                // 앞 이벤트에서 깃발을 세우는 선택지를 고른다
                OpenEvent(e.Id);
                yield return Screen_("event", 1.0f);
                var opts = f.P.Options(e);
                int idx = opts.IndexOf(o);
                string lockWhy = idx >= 0 ? f.P.LockOf(o) : "없음";
                if (idx >= 0 && lockWhy == null)
                {
                    f.EventSnapNow();
                    var (fight, why) = f.P.Choose(idx);
                    for (int g = 0; g < 6 && f.P.S.Event?.Pending.Count > 0; g++)
                    {
                        var p = f.P.S.Event.Pending[0];
                        object v = p.K == "remove" || p.K == "dupe" ? (object)f.P.S.Deck.FirstOrDefault(id => p.K != "dupe" || f.P.DupeOk(id)) : p.K == "card" ? p.Cards?.FirstOrDefault() : p.K == "gambleChoice" ? (object)0 : null;
                        if (f.P.Resolve(v) != null) f.P.Resolve(null);
                    }
                    f.P.S.Bag.Clear();
                    while (f.P.PendingNeutral != null && f.P.S.Party.Count > 0) f.P.AssignNeutral(f.P.S.Party[0]);
                    f.EventStop();
                    yield return Screen_("event", 0.8f);
                    yield return Shot($"evflag_{flag}_set_{e.Id}");
                }
                else Debug.LogWarning($"[Demo] 깃발 {flag}: {e.Id} 「{o.Label}」 을 고를 수 없음 — {lockWhy}");
                Expect(f.P.S.Flags.Contains(flag), $"깃발 줄기: {e.Id} 「{o.Label}」 → 깃발 {flag}");
                CloseModals();
                OpenEvent(reader.Id);
                yield return Screen_("event", 1.4f);
                yield return Shot($"evflag_{flag}_after_{reader.Id}");
                var after = f.P.Options(reader).Select(x => x.Label).ToList();
                Debug.Log($"[Demo] 깃발 {flag}: {reader.Id} 선택지 전 [{string.Join(" · ", before)}] → 후 [{string.Join(" · ", after)}]");
                Expect(!before.SequenceEqual(after) && after.Any(l => reader.Options.Any(x => x.Flag == flag && x.Label == l)), $"깃발 줄기: {reader.Id} 의 선택지가 깃발로 바뀐다");
                CloseModals();
            }
        }
    }
}
