using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 이벤트 대사의 말하는 이(-demo-evby) — 선택지 `by`(동행 사도가 하는 말)가 그 사도 이름표로 붙는지.
    //   -evby H6:베루,F4:코미 처럼 「이벤트 id:사도 키」 를 준다(기본 H6:베루 · F4:코미). 파티 첫 자리를 그 사도로 바꾸고 by 가 붙은 선택지를 고른 뒤,
    //   결과 대화창을 한 줄씩 넘기며 이름표가 붙은 줄을 찍는다. 저장은 -save 로 따로(데모 판).
    public partial class Demo
    {
        IEnumerator EvBy_()
        {
            f.DialogAuto = true;
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
            var jobs = (Arg("-evby") ?? "H6:베루,F4:코미").Split(',').Select(s => s.Split(':')).Where(a => a.Length == 2).ToList();
            foreach (var job in jobs)
            {
                string id = job[0];
                var h = Roster.ByKey(job[1]);
                var ev = f.P.Data.Events.FirstOrDefault(e => e.Id == id);
                if (ev == null || h == null || !h.Playable) { Expect(false, $"{id} · {job[1]} 없음(코어에 없는 사도?)"); continue; }
                f.P.S.Party[0] = h.CoreId;
                f.DialogAuto = true;
                OpenEvent(id);
                yield return Screen_("event", 2.0f);
                var opts = f.P.Options(ev);
                int idx = opts.FindIndex(o => o.By != null && f.P.LockOf(o) == null);
                Debug.Log($"[EvBy] {id} 「{ev.Name}」 · 동행 {h.ko} · by 선택지 {idx}");
                if (idx < 0) { Expect(false, $"{id} by 선택지를 못 고름"); continue; }
                yield return Press("event.opt" + idx, 0.5f);
                f.DialogAuto = false;
                yield return Press("event.ok", 1.2f);
                var whos = new List<string>();
                bool shot = false;
                for (int i = 0; i < 30 && f.EventStep == "resultDialog"; i++)
                {
                    yield return Wait(1.4f);   // 한 글자씩 — 줄이 다 나온 뒤
                    var l = f.EventLinesNow[Mathf.Clamp(f.EventLineIdx, 0, f.EventLinesNow.Count - 1)];
                    if (l.Who != null)
                    {
                        whos.Add(l.Who);
                        Debug.Log($"[EvBy] {id} 줄 {f.EventLineIdx + 1}: {l.Who} 「{l.Text}」");
                        yield return Shot($"evby_{id}_{whos.Count}");
                        shot = true;
                    }
                    yield return Press("event.next", 0.2f);
                }
                Expect(shot, $"{id} 이름표 붙은 결과 대사가 있음");
                Expect(whos.Contains(h.ko), $"{id} 동행 사도 {h.ko} 이름표({string.Join(" · ", whos)})");
                CloseModals();
            }
            Debug.Log($"[Demo] 말하는 이 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }
    }
}
