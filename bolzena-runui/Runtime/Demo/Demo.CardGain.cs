using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 카드 얻기 연출 점검(-demo-cardgain, 2026-10-06 「카드를 얻었을 때는 화면 중앙에」 · 이벤트 먼저)
    //   이벤트에서 골칫거리 · 선물 · 교주 카드(주인 없는 카드)를 주는 선택지를 골라 → 가운데 표시 → 덱 아이콘으로 · 덱 수 증가 → 사도 고르기 → 결과
    public partial class Demo
    {
        IEnumerator CardGain_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return PickParty(0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.0f);
            f.P.S.Gold = Mathf.Max(f.P.S.Gold, 400);
            int f0 = fails;
            // 카드를 곧장 주는(고르기 없이) 선택지 — 주인 없는 카드(curse · gift · neutral)
            string[] gives = { "curse", "gift", "neutral" };
            var all = f.P.Data.Events
                .SelectMany(e => e.Options.Select((o, i) => (e, o, i)))
                .Where(x => (x.o.Gamble == null || x.o.Gamble.Count == 0) && x.o.Fight == null && (x.o.Hero == null || x.o.Hero.Count == 0)
                    && (x.o.Out ?? new List<Core.Outcome>()).Any(c => gives.Contains(c.K))
                    && !x.o.Out.Any(c => new[] { "remove", "dupe", "card", "flash", "shinPick", "mindBreak", "gold" }.Contains(c.K)))
                .ToList();
            // 주인을 고르는 카드(선물 · 교주) 하나 + 저주 하나(사도 고르기 없이 덱으로)
            var cand = all.Where(x => x.o.Out.Any(c => c.K != "curse" && gives.Contains(c.K))).Take(1)
                .Concat(all.Where(x => x.o.Out.Any(c => c.K == "curse") && !x.o.Out.Any(c => c.K == "gift" || c.K == "neutral")).Take(1)).ToList();
            Debug.Log($"[Demo] 카드 주는 선택지 {all.Count}개 · 시험 {string.Join(", ", cand.Select(x => x.e.Id + "#" + x.i))}");
            if (cand.Count == 0) { Debug.LogWarning("[Demo] 카드를 주는 이벤트 선택지가 없습니다"); }
            int shot = 0;
            foreach (var (e, o, i) in cand)
            {
                string key = $"{f.P.S.Floor}:{(f.P.S.Map?.At ?? f.P.S.Node.ToString())}";   // Run.EnterEvent 와 같은 열쇠 — 다르면 새로 굴린다
                f.P.S.Event = new Core.EventState { Key = key, Choices = new List<string> { e.Id }, Id = e.Id };
                f.EventStop();
                yield return Screen_("event", 1.2f);
                int deck0 = f.P.S.Deck.Count, wait0 = f.P.S.NeutralWait.Count;
                var hot = f.Stage.Hot.Keys.Where(k => k.StartsWith("event.opt")).OrderBy(k => k, System.StringComparer.Ordinal).ToList();
                string pk = f.Stage.Hot.ContainsKey("event.opt" + i) ? "event.opt" + i : null;
                Debug.Log($"[Demo] 이벤트 {e.Id} 「{e.Name}」 선택지 {i} → {pk}");
                if (pk == null) continue;
                bool curse = o.Out.Any(c => c.K == "curse") && !o.Out.Any(c => c.K == "gift" || c.K == "neutral");
                yield return Press(pk, 0.4f);
                if (Hot("event.ok") != null) yield return Press("event.ok", 0.1f);
                yield return Until(() => CardGain.Phase == "show", 6);
                yield return Wait(0.2f);
                Expect(CardGain.IsOpen && CardGain.LastCount >= 1, $"이벤트 {e.Id} — 얻은 카드 {CardGain.LastCount}장을 가운데에");
                yield return Shot("cardgain_event" + shot);
                var deckTxt = Hot("deck") != null ? Hot("deck").GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).FirstOrDefault(x => int.TryParse(x.text, out _)) : null;
                int shown0 = deckTxt != null && int.TryParse(deckTxt.text, out var d0) ? d0 : -1;
                yield return Press("gain.ok", 0.1f);
                yield return Until(() => !CardGain.IsOpen, 4);
                int shown1 = deckTxt != null && int.TryParse(deckTxt.text, out var d1) ? d1 : -1;
                Expect(shown1 > shown0, $"덱으로 날아가며 덱 수가 오른다({shown0} → {shown1})");
                yield return Wait(0.5f);
                bool owner = f.Stage.ModalLayer.GetComponentsInChildren<Transform>(true).Any(t => t.name == "modal ownerpick");
                Expect(f.P.S.NeutralWait.Count == 0 || owner, $"주인 없는 카드 — 사도 고르기 창(기다리는 줄 {f.P.S.NeutralWait.Count})");
                if (curse) Expect(!owner && f.P.S.NeutralWait.Count == 0, "저주 — 사도 고르기 없이 덱으로");
                else Expect(owner, "선물 · 교주 카드 — 가운데 표시 뒤 사도 고르기");
                if (owner) { yield return Shot("cardgain_owner" + shot); yield return PickFirstOwner(); }
                yield return Until(() => f.P.S.NeutralWait.Count == 0, 6);
                yield return Wait(1.0f);
                Expect(f.P.S.Deck.Count + f.P.S.NeutralWait.Count > deck0 + wait0 - 1 && f.P.S.NeutralWait.Count == 0, $"덱에 들어감 — 덱 {deck0} → {f.P.S.Deck.Count}");
                yield return Shot("cardgain_result" + shot);
                shot++;
                f.P.S.Event = null;
            }
            Debug.Log($"[Demo] 카드 얻기 단언 실패 {fails - f0}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        IEnumerator PickFirstOwner()
        {
            var k = f.Stage.Hot.Keys.Where(x => x.StartsWith("owner")).OrderBy(x => x, System.StringComparer.Ordinal).ToList();
            Debug.Log("[Demo] 주인 고르기 단추 " + string.Join(", ", k));
            var pick = k.FirstOrDefault(x => x.StartsWith("owner:"));
            if (pick != null) yield return Press(pick, 1.0f);
        }
    }
}
