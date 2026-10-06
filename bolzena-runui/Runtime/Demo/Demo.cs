using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 자동 데모(-demo) — 가짜 손가락으로 판 한 바퀴를 지나며 화면마다 캡처한다(Captures/NN_이름.png, -phone 이면 _phone 꼬리).
    //   로비 → 마을 공개 → 편성(빈 칸 · 셋) → 지도 → 싸움 → 보상 · 장비 → 이벤트 → 캠프 → 상점(사기 · 제거) → … → 층 보스 → 복제
    //   → 2층 → 마지막 보스 → 끝(완주) → 도감 · 설정 · 덱 → 새 판에서 지기 → 끝(패배)
    public partial class Demo : MonoBehaviour
    {
        public static bool Active;
        public static Demo Me { get; private set; }
        /// <summary>지금 싸움을 어떻게 받을지 — win(이기게) · lose(지게). 전투 화면(FightScreen)이 열 때 읽는다.</summary>
        public static string FightMode = "win";
        Flow f;
        string dir, tail = "";
        int no;
        readonly HashSet<string> seen = new HashSet<string>();

        public static void Attach(Flow flow)
        {
            Active = true;
            var d = flow.gameObject.AddComponent<Demo>();
            d.f = flow;
            Me = d;
        }

        /// <summary>다른 화면(전투)이 같은 번호 줄로 찍는다 — 같은 이름은 한 번만.</summary>
        public void Snap(string name) => StartCoroutine(Shot(name));

        void Start()
        {
            dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            if (Has("-phone")) tail = "_phone";
            Directory.CreateDirectory(dir);
            Debug.Log($"[Demo] captures → {dir} ({Screen.width}×{Screen.height})");
            RunPort.ClearSave();
            PlayerPrefs.DeleteKey("bz.calm");
            // 감시 — 예외가 나거나 시간이 넘으면 오류 코드로 스스로 꺼진다(먹통으로 남지 않게)
            Application.logMessageReceived += (msg, stack, type) =>
            {
                if (type == LogType.Exception) Bail(2, "예외 — " + msg);
            };
            bool shortRun = Has("-demo-roster"), quick = Has("-demo-quick");
            float limit = float.TryParse(Arg("-demo-timeout"), out var lim) ? lim : shortRun ? 150f : quick ? 240f : 420f;
            f.DialogAuto = !Has("-demo-dialog");   // 시범은 이벤트 대화창을 바로 넘긴다(대화창을 보는 묶음 · -demo-dialog 만 줄마다)
            StartCoroutine(AutoOwner());
            if (Has("-demo-loop")) { limit = float.TryParse(Arg("-demo-timeout"), out var ll) ? ll : 3600f; }
            StartCoroutine(Watchdog(limit));
            StartCoroutine(Has("-demo-clonesize") ? CloneSize_() : Has("-demo-cardgain") ? CardGain_() : Has("-demo-sdsize") ? SdSize_() : Has("-demo-marks") ? Marks_() : Has("-demo-partyfoe") ? PartyFoe_() : Has("-demo-events") ? Events_() : Has("-demo-loop") ? Loop() : Has("-demo-lobby") ? Lobby_() : Has("-demo-traits") ? Traits_() : Has("-demo-search") ? Search_() : Has("-demo-sheet") ? Sheet_() : quick ? Quick() : shortRun ? Roster_() : Run());
        }

        // 이벤트(-demo-events) — 꼴이 다른 이벤트 여섯(선택지 적음 · 많음 · 카드 고르기 · 신탁 · 전투 · 도박/판정)의 처음 화면 · 고른 뒤 결과를 찍고,
        //   그다음 데이터의 이벤트 전부를 한 번씩 열어 첫 선택지까지 골라 본다(예외가 나면 Bail 이 멈춘다)
        IEnumerator Events_()
        {
            f.DialogAuto = true;   // 대화창은 아래 「대화창 흐름」 묶음에서만 줄마다 본다
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return Press("party.auto", 0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.0f);
            f.P.S.Gold = Mathf.Max(f.P.S.Gold, 300);
            var evs = f.P.Data.Events;
            bool Has(Core.EventDef e, System.Func<Core.EventOption, bool> p) => e.Options.Any(p);
            bool Out(Core.EventOption o, params string[] ks) => (o.Out ?? new List<Core.Outcome>()).Any(x => ks.Contains(x.K)) || (o.Gamble ?? new List<Core.Gamble>()).Any(g => g.Out.Any(x => ks.Contains(x.K)));
            var pick = new List<(string tag, Core.EventDef ev, System.Func<Core.EventOption, bool> opt)>();
            void Add(string tag, Core.EventDef ev, System.Func<Core.EventOption, bool> opt) { if (ev != null && pick.All(p => p.ev != ev)) pick.Add((tag, ev, opt)); }
            Add("few", evs.Where(e => e.Options.Count <= 2).FirstOrDefault(), o => o.Hero == null);
            Add("many", evs.OrderByDescending(e => e.Options.Count).FirstOrDefault(), o => o.Hero == null);
            bool Free(Core.EventDef e) => pick.All(p => p.ev != e);
            Add("special", evs.FirstOrDefault(e => e.Id == "G07"), o => o.Hero == null);
            Add("special2", evs.FirstOrDefault(e => e.Id == "G12"), o => o.Hero == null);
            Add("cardpick", evs.FirstOrDefault(e => Free(e) && Has(e, o => o.Hero == null && Out(o, "remove", "unique", "dupe"))), o => o.Hero == null && Out(o, "remove", "unique", "dupe"));
            Add("oracle", evs.FirstOrDefault(e => Free(e) && Has(e, o => o.Hero == null && Out(o, "flash"))), o => o.Hero == null && Out(o, "flash"));
            Add("fight", evs.FirstOrDefault(e => Free(e) && Has(e, o => o.Fight != null && o.Hero == null)), o => o.Fight != null);
            Add("equip", evs.FirstOrDefault(e => Free(e) && Has(e, o => o.Hero == null && o.Fight == null && Out(o, "equip"))), o => o.Hero == null && o.Fight == null && Out(o, "equip"));
            Add("gamble", evs.FirstOrDefault(e => Free(e) && Has(e, o => (o.Gamble != null || o.Judge != null) && o.Hero == null)), o => o.Gamble != null || o.Judge != null);
            // 카드를 주는 이벤트 · 여러 개를 주는 이벤트 — 결과 칸이 받은 것을 자르지 않는지
            Add("gaincard", evs.FirstOrDefault(e => Free(e) && Has(e, o => o.Hero == null && o.Fight == null && o.Gamble == null && o.Judge == null && Out(o, "gift", "curse"))), o => o.Hero == null && o.Fight == null && Out(o, "gift", "curse"));
            int OutN(Core.EventOption o) => (o.Out ?? new List<Core.Outcome>()).Count(x => x.K != "none");
            var multi = evs.Where(e => Free(e)).SelectMany(e => e.Options.Where(o => o.Hero == null && o.Fight == null && o.Gamble == null && o.Judge == null).Select(o => (e, o))).OrderByDescending(x => OutN(x.o)).FirstOrDefault();
            if (multi.e != null) { var mo = multi.o; Add("multi", multi.e, o => o == mo); }
            int n = 0;
            foreach (var (tag, ev, optOf) in pick)
            {
                n++;
                OpenEvent(ev.Id);
                yield return Screen_("event", 2.4f);
                yield return Shot($"ev{n}_{tag}");
                float choiceLeft = f.LastEvLeft;
                var opts = f.P.Options(ev);
                int idx = opts.FindIndex(o => optOf(o) && f.P.LockOf(o) == null);
                if (idx < 0) idx = opts.FindIndex(o => f.P.LockOf(o) == null && !o.Leave);
                Debug.Log($"[Demo] 이벤트 {tag}: {ev.Id} 「{ev.Name}」 선택지 {opts.Count} · 고름 {idx} 「{(idx >= 0 ? opts[idx].Label : "-")}」");
                if (idx < 0) continue;
                yield return Press("event.opt" + idx, 0.5f);
                yield return Shot($"ev{n}_{tag}_picked");
                yield return Press("event.ok", 2.0f);
                yield return Shot($"ev{n}_{tag}_result");
                Fit($"{ev.Id} {tag}", choiceLeft);
                for (int g = 0; g < 4 && Hot("pending0") != null && f.Stage.ModalLayer.Find("modal pending") != null; g++) { yield return Press("pending0", 1.2f); }
                if (f.Stage.Current == "event") { yield return Wait(0.6f); yield return Shot($"ev{n}_{tag}_after"); }
                CloseModals();
            }
            // 전부 한 번씩 — 열고, 첫 고를 수 있는 선택지(떠나기 아닌 것)를 고르고, 고를 것은 첫 것으로
            int ok = 0, cut = 0;
            bool firstFitDone = false;
            // 말줄임 검사 — 보이는 글 가운데 잘린(… · 넘쳐 잘림) 것이 있으면 단언 실패
            void Trunc(string tag)
            {
                foreach (var ty in f.Stage.Root.GetComponentsInChildren<TypeOn>()) ty.Finish();
                foreach (var t in f.Stage.Root.GetComponentsInChildren<TMPro.TextMeshProUGUI>())
                {
                    if (!t.isActiveAndEnabled || string.IsNullOrEmpty(t.text)) continue;
                    t.ForceMeshUpdate();
                    if (t.isTextTruncated || (t.overflowMode == TMPro.TextOverflowModes.Ellipsis && t.isTextOverflowing)) { cut++; Debug.LogWarning($"[Demo] 말줄임 {tag} — 「{t.text}」 ({t.name})"); }
                }
            }
            foreach (var ev in evs)
            {
                OpenEvent(ev.Id);
                yield return Screen_("event", 0.3f);
                Trunc(ev.Id + " 고르기");
                ArtCheck(ev);
                // -demo-evshots: 이벤트마다 한 장(그림 · 사도 방향 · 글 잘림을 눈으로) — 그림이 다 떠오른 뒤
                if (System.Environment.GetCommandLineArgs().Contains("-demo-evshots")) { yield return Wait(0.9f); yield return Shot($"all_{ev.Id}"); Debug.Log($"[Demo] 이벤트 그림 {ev.Id} 「{ev.Name}」 — {f.LastTargetArt} (npc {ev.Npc ?? "-"})"); }
                float choiceLeft2 = f.LastEvLeft;
                var opts = f.P.Options(ev);
                int idx = opts.FindIndex(o => f.P.LockOf(o) == null && !o.Leave);
                if (idx >= 0)
                {
                    f.EventSnapNow();
                    var (fight, why) = f.P.Choose(idx);
                    if (!fight && why == null)
                    {
                        for (int g = 0; g < 6 && f.P.S.Event?.Pending.Count > 0; g++)
                        {
                            var p = f.P.S.Event.Pending[0];
                            object v = p.K == "remove" || p.K == "dupe" ? (object)f.P.S.Deck.FirstOrDefault(id => p.K != "dupe" || f.P.DupeOk(id)) : p.K == "card" ? p.Cards?.FirstOrDefault() : p.K == "gambleChoice" ? (object)0 : null;
                            if (f.P.Resolve(v) != null) f.P.Resolve(null);
                        }
                        f.P.S.Bag.Clear();
                        while (f.P.PendingNeutral != null && f.P.S.Party.Count > 0) f.P.AssignNeutral(f.P.S.Party[0]);
                        f.EventStop();
                        yield return Screen_("event", 0.3f);
                        Trunc(ev.Id + " 결과");
                        Fit(ev.Id + " 결과", choiceLeft2);
                        CloseModals();
                    }
                }
                ok++;
            }
            Debug.Log($"[Demo] 이벤트 전부 {ok}/{evs.Count} 열고 골라 봄");
            f.P.S.Gold = Mathf.Max(f.P.S.Gold, 500); f.P.S.PartyHp = f.P.S.PartyMaxHp; f.P.S.MindBreak = 0;   // 뒤 단언이 골드 · HP · 정신 붕괴(앞 이벤트가 건 것) 잠금에 걸리지 않게
            // 결과 칸 글 줄(카드 · 장비 한 줄) — 가운데 띄우기 연출 없이 결과 칸만 찍는다(카드를 주는 · 여러 개를 주는 이벤트)
            foreach (var (tag, ev, optOf) in pick.Where(x => x.tag == "gaincard" || x.tag == "multi").ToList())
            {
                OpenEvent(ev.Id);
                yield return Screen_("event", 0.3f);
                var opts = f.P.Options(ev);
                int idx = opts.FindIndex(o => optOf(o) && f.P.LockOf(o) == null);
                if (idx < 0) continue;
                f.EventSnapNow();
                var (fight, why) = f.P.Choose(idx);
                if (fight || why != null) continue;
                for (int g = 0; g < 6 && f.P.S.Event?.Pending.Count > 0; g++) { var p = f.P.S.Event.Pending[0]; if (f.P.Resolve(p.K == "remove" ? (object)f.P.S.Deck.FirstOrDefault() : null) != null) f.P.Resolve(null); }
                f.P.S.Bag.Clear();
                while (f.P.PendingNeutral != null && f.P.S.Party.Count > 0) f.P.AssignNeutral(f.P.S.Party[0]);
                f.EventStop();
                yield return Screen_("event", 1.2f);
                Fit(ev.Id + " 글 줄");
                yield return Shot($"lines_{tag}");
                CloseModals();
            }
            Expect(cut == 0, $"이벤트 글 말줄임 {cut}");
            Expect(artBad == 0 && artSeen > 0, $"대상 그림 — 대상이 있는 이벤트 {artSeen} 가운데 정지 그림 · 빈 자리로 떨어진 것 {artBad}");
            Expect(fitBad == 0 && fitSeen > 0, $"결과 칸 — 받은 것이 칸 밖으로 잘린 것 {fitBad} (본 결과 칸 {fitSeen} · 스크롤 {fitScroll})");
            Expect(alignBad == 0 && fitSeen > 0, $"결과 칸 — 글 왼쪽 끝 = 칸 왼쪽 + {Flow.EvTextInset} · 칸 왼쪽 = 선택지 왼쪽 (어긋남 {alignBad})");
            _ = firstFitDone;

            // 대화창 흐름 — 서술 · 대사 줄(이름표) → 선택 → 결과 줄 → 받은 것 · 떠나기(2026-10-06 사용자: 비주얼 노벨식)
            f.DialogAuto = false;
            {
                bool HasSpeech(string t) => Flow.SplitSay(t ?? "").speech.Length > 0;
                var ids = new List<string> { "G07", "G12" };
                var spE = evs.Where(e => HasSpeech(e.Scene) && !ids.Contains(e.Id)).Select(e => e.Id).Take(2).ToList(); ids.AddRange(spE);
                var narE = evs.FirstOrDefault(e => !HasSpeech(e.Scene) && !ids.Contains(e.Id)); if (narE != null) ids.Add(narE.Id);
                var resE = evs.FirstOrDefault(e => !ids.Contains(e.Id) && e.Options.Any(o => o.Hero == null && o.Fight == null && o.Gamble == null && o.Judge == null && HasSpeech(o.Say))); if (resE != null) ids.Add(resE.Id);
                int dlgBad = 0, dlgLines = 0, dlgTags = 0;
                void Frame(string tag)
                {
                    var box = f.Stage.Root.GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name == "evdialog");
                    if (box == null) { dlgBad++; Debug.LogWarning($"[Demo] 대화창 없음 {tag}"); return; }
                    Canvas.ForceUpdateCanvases();
                    var c = new Vector3[4]; box.GetWorldCorners(c); var br = Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
                    f.Stage.Root.GetWorldCorners(c); var sr = Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
                    if (br.xMin < sr.xMin - 1 || br.xMax > sr.xMax + 1 || br.yMin < sr.yMin - 1 || br.yMax > sr.yMax + 1) { dlgBad++; Debug.LogWarning($"[Demo] 대화창 화면 밖 {tag}"); }
                    var line = box.GetComponentsInChildren<TMPro.TextMeshProUGUI>().FirstOrDefault(t => t.name == "line");
                    var cur = f.EventLinesNow[Mathf.Clamp(f.EventLineIdx, 0, f.EventLinesNow.Count - 1)];
                    if (line == null || line.text != cur.Text || line.alignment != TMPro.TextAlignmentOptions.TopLeft) { dlgBad++; Debug.LogWarning($"[Demo] 대화창 글 {tag} 「{line?.text}」 ≠ 「{cur.Text}」"); }
                    var tagGo = box.Find("nametag");
                    bool tagOn = tagGo != null && tagGo.gameObject.activeSelf;
                    if (tagOn != (cur.Who != null) || (tagOn && tagGo.GetComponentInChildren<TMPro.TextMeshProUGUI>().text != cur.Who)) { dlgBad++; Debug.LogWarning($"[Demo] 이름표 {tag} — {cur.Who ?? "서술"}"); }
                    if (cur.Who != null) dlgTags++;
                    // 말하는 이 ▼ — 파티 사도가 말하면 그 머리 위 표시가 켜져 있고 화면 안 · 대화창 위에 있어야 한다
                    var party = f.P.S.Party.Select(k => Roster.OfCore(k)?.ko).Where(n => n != null).ToList();
                    var npcNow = f.P.S.Event?.Id != null ? f.P.Data.Event(f.P.S.Event.Id)?.Npc : null;
                    if (cur.Who != null && (party.Contains(cur.Who) || cur.Who == npcNow))
                    {
                        var mk = f.Stage.Root.GetComponentsInChildren<RectTransform>().FirstOrDefault(r => r.name == "speaker");
                        if (mk == null) { dlgBad++; Debug.LogWarning($"[Demo] 화자 ▼ 없음 {tag} — {cur.Who}"); }
                        else
                        {
                            mk.GetWorldCorners(c);   // ▼ 는 -90° 돌린 것이라 모서리 순서가 바뀐다 — 네 모서리의 최소 · 최대로
                            var mr = Rect.MinMaxRect(c.Min(v => v.x), c.Min(v => v.y), c.Max(v => v.x), c.Max(v => v.y));
                            bool inside = mr.xMin >= sr.xMin - 1 && mr.xMax <= sr.xMax + 1 && mr.yMin >= sr.yMin - 1 && mr.yMax <= sr.yMax + 1;
                            bool above = mr.yMin >= br.yMax - 1;
                            bool big = mr.height >= 12f;   // 화면 픽셀 — 작은 폰 창(844×390)에서도 눈에 띄는 크기
                            if (!inside || !above || !big || !mk.gameObject.activeInHierarchy) { dlgBad++; Debug.LogWarning($"[Demo] 화자 ▼ 안 보임 {tag} — {cur.Who} ▼{mr} 화면{sr} 대화창{br}"); }
                            else Debug.Log($"[Demo] 화자 ▼ {tag} — {cur.Who} ▼({mr.center.x:0},{mr.yMin:0})");
                        }
                    }
                    if (cur.Who == null && line != null && line.text.Contains("\"") && f.EventLinesNow.Any(l => l.Who != null)) { dlgBad++; Debug.LogWarning($"[Demo] 서술 줄에 따옴표 {tag}"); }
                    dlgLines++;
                    Trunc(tag);
                }
                foreach (var id in ids)
                {
                    var ev = f.P.Data.Event(id); if (ev == null) continue;
                    OpenEvent(id);
                    yield return Screen_("event", 1.0f);
                    if (f.EventStep != "dialog") { dlgBad++; Debug.LogWarning($"[Demo] {id} 처음 단계 {f.EventStep}"); continue; }
                    bool shotSay = false;
                    int nl = f.EventLinesNow.Count;
                    for (int i = 0; i < nl && f.EventStep == "dialog"; i++)
                    {
                        yield return Wait(0.2f);
                        var ty = f.Stage.Root.GetComponentsInChildren<TypeOn>().FirstOrDefault(x => x.name == "line");
                        if (ty != null && !ty.IsDone) { ty.Finish(); yield return Wait(0.1f); }   // 한 글자씩을 끝내고(누르기와 다투지 않게)
                        Frame($"{id} 줄 {i + 1}");
                        if (i == 0) yield return Shot($"dlg_{id}_line1");
                        if (!shotSay && f.EventLineIdx < f.EventLinesNow.Count && f.EventLinesNow[f.EventLineIdx].Who != null) { shotSay = true; yield return Shot($"dlg_{id}_say"); }
                        yield return Press("event.next", 0.35f);
                    }
                    yield return Wait(0.8f);
                    Expect(f.EventStep == "choose" && f.Stage.Hot.Keys.Any(k => k.StartsWith("event.opt") && Hot(k) != null), $"대화창 {id}: 줄 {nl} 넘김 → 선택 창");
                    yield return Shot($"dlg_{id}_choose");
                    var opts = f.P.Options(ev);
                    int oi = id == resE?.Id ? opts.FindIndex(o => f.P.LockOf(o) == null && HasSpeech(o.Say) && o.Fight == null) : -1;
                    if (oi < 0) oi = opts.FindIndex(o => f.P.LockOf(o) == null && !o.Leave && o.Fight == null && o.Gamble == null && o.Judge == null);
                    if (oi < 0) { CloseModals(); continue; }
                    yield return Press("event.opt" + oi, 0.4f);
                    yield return Press("event.ok", 1.0f);
                    for (float w = 0; w < 10f && CardGain.IsOpen; w += 0.2f) yield return Wait(0.2f);
                    yield return Wait(0.6f);
                    if (f.EventStep == "resultDialog")
                    {
                        int rl = f.EventLinesNow.Count;
                        for (int i = 0; i < rl && f.EventStep == "resultDialog"; i++)
                        {
                            yield return Wait(0.2f);
                            var ty = f.Stage.Root.GetComponentsInChildren<TypeOn>().FirstOrDefault(x => x.name == "line");
                            if (ty != null && !ty.IsDone) { ty.Finish(); yield return Wait(0.1f); }   // 한 글자씩을 끝내고(누르기와 다투지 않게)
                            Frame($"{id} 결과 줄 {i + 1}");
                            if (i == 0) yield return Shot($"dlg_{id}_resultline");
                            yield return Press("event.next", 0.35f);
                        }
                        yield return Wait(0.8f);
                    }
                    bool pendingUp = f.Stage.ModalLayer.Find("modal pending") != null || f.Stage.ModalLayer.Find("modal grace") != null || OracleReveal.IsOpen;
                    Expect(f.EventStep == "result" && (Hot("event.leave") != null || pendingUp || f.P.S.Bag.Count > 0), $"대화창 {id}: 결과 줄 → 받은 것 · 떠나기 (단계 {f.EventStep})");
                    yield return Shot($"dlg_{id}_result");
                    CloseModals();
                }
                Expect(dlgBad == 0 && dlgLines > 0 && dlgTags > 0, $"대화창 — 본 줄 {dlgLines} (이름표 줄 {dlgTags}) · 화면 밖 · 글 · 이름표 어긋남 {dlgBad}");
            }
            f.DialogAuto = true;

            // 이벤트 은총 — 실제 화면으로: 선택지 → 결과 → 사도 고르기 창 → 그 사도의 남은 고유 카드 → 가운데 띄우기
            {
                bool Plain(Core.EventOption o) => o.Hero == null && o.Fight == null && o.Gamble == null && o.Judge == null;
                var gev = evs.FirstOrDefault(e => e.Options.Any(o => Plain(o) && Out(o, "unique") && !Out(o, "remove", "dupe", "flash")));
                if (gev == null) Expect(false, "은총: unique 를 주는 이벤트가 없다");
                else
                {
                    OpenEvent(gev.Id);
                    yield return Screen_("event", 1.6f);
                    var gopts = f.P.Options(gev);
                    int gi = gopts.FindIndex(o => Plain(o) && Out(o, "unique") && f.P.LockOf(o) == null);
                    Expect(gi >= 0, $"은총: {gev.Id} 의 은총 선택지를 고를 수 있다 {string.Join(" · ", gopts.Select(o => f.P.LockOf(o)).Where(x => x != null))}");
                    if (gi >= 0)
                    {
                        yield return Press("event.opt" + gi, 0.5f);
                        yield return Press("event.ok", 2.0f);
                        yield return Shot("grace_pick");
                        Trunc("은총 창");
                        var heroes = f.P.Run.GraceHeroes();
                        Expect(f.Stage.ModalLayer.Find("modal grace") != null && heroes.Count > 0, $"은총: 사도 고르기 창이 뜬다 (고를 수 있는 사도 {heroes.Count})");
                        var key = heroes.LastOrDefault();
                        var left = f.P.Run.UniquesLeft(key);
                        int before = f.P.S.Deck.Count;
                        yield return Press("grace:" + key, 1.0f);
                        for (float w = 0; w < 3f && CardGain.Phase != "show"; w += 0.1f) yield return Wait(0.1f);
                        yield return Shot("grace_gain");
                        for (float w = 0; w < 10f && CardGain.IsOpen; w += 0.2f) yield return Wait(0.2f);
                        var got = f.P.S.Deck.Skip(before).ToList();
                        Expect(got.Count > 0 && got.All(id => left.Contains(id) && f.P.Data.Card(id)?.Hero == key), $"은총: {key} 의 남은 고유 카드를 얻음 — {string.Join(", ", got)}");
                        yield return Wait(0.6f);
                        yield return Shot("grace_result");
                    }
                    CloseModals();
                }
                // 이벤트 카드 제거 — 무작위가 아니라 고르는 창, 고른 카드가 빠진다(분신에게 잡일 맡기기: 제거 2 + 저주)
                var rev = evs.FirstOrDefault(e => e.Options.Any(o => Plain(o) && (o.Out ?? new List<Core.Outcome>()).Any(x => x.K == "remove" && x.N >= 2)))
                          ?? evs.FirstOrDefault(e => e.Options.Any(o => Plain(o) && Out(o, "remove")));
                if (rev != null)
                {
                    OpenEvent(rev.Id);
                    yield return Screen_("event", 1.6f);
                    var ropts = f.P.Options(rev);
                    int ri = ropts.FindIndex(o => Plain(o) && Out(o, "remove") && f.P.LockOf(o) == null);
                    Expect(ri >= 0, $"제거: {rev.Id} 의 제거 선택지를 고를 수 있다 {string.Join(" · ", ropts.Select(o => f.P.LockOf(o)).Where(x => x != null))}");
                    if (ri >= 0)
                    {
                        yield return Press("event.opt" + ri, 0.5f);
                        yield return Press("event.ok", 2.0f);
                        int picks = 0;
                        // 얻은 저주가 먼저 가운데에 뜬다(CardGain) — 그 연출이 끝나고 제거 창이 뜰 때까지 기다린다
                        for (float w = 0; w < 12f && f.Stage.ModalLayer.Find("modal pending") == null; w += 0.2f) yield return Wait(0.2f);
                        for (int g = 0; g < 4 && f.Stage.ModalLayer.Find("modal pending") != null && f.P.S.Event?.Pending.FirstOrDefault()?.K == "remove"; g++)
                        {
                            yield return Wait(1.4f);   // 창 · 카드 등장 연출이 끝난 뒤에 찍는다
                            yield return Shot($"remove_pick{g + 1}");
                            var keyHot = FirstHot("pending");
                            // 칸 순서 = 덱 고르기 목록 순서 — 그 칸의 카드 id 는 덱에서 한 장 줄어야 한다
                            var deck0 = f.P.S.Deck.ToList();
                            yield return Press(keyHot, 1.4f);
                            for (float w = 0; w < 8f && CardGain.IsOpen; w += 0.2f) yield return Wait(0.2f);
                            var gone = deck0.GroupBy(x => x).Where(x => x.Count() > f.P.S.Deck.Count(y => y == x.Key)).Select(x => x.Key).ToList();
                            Expect(gone.Count == 1 && f.P.S.Deck.Count == deck0.Count - 1, $"제거 {g + 1}: 고른 카드가 빠짐 — {string.Join(", ", gone)}");
                            picks++;
                        }
                        Expect(picks >= 1, $"제거: 고르는 창으로 {picks}장 뺐다 · 「{ropts[ri].Label}」");
                        yield return Wait(1.0f);
                        yield return Shot("remove_result");
                        Fit(rev.Id + " 제거 결과");
                    }
                    CloseModals();
                }
            }

            // 선물 카드는 주인 고르기 창 → 덱 id 「id@사도」 → 그 사도 묶음. 저주는 창 없이 주인 없는 id 로 덱의 「저주」 묶음(2026-10-06 사용자)
            var party3 = f.P.S.Party.ToList();
            var curse = f.P.Data.Cards.Values.Where(c => c.Hero == null && c.IsCurse && !c.Token).Select(c => c.Id).OrderBy(x => x, System.StringComparer.Ordinal).FirstOrDefault();
            var gift = f.P.Data.Cards.Values.Where(c => c.Hero == null && c.Gift && !c.Token).Select(c => c.Id).OrderBy(x => x, System.StringComparer.Ordinal).FirstOrDefault();
            if (party3.Count > 1)
            {
                f.Map();
                yield return Screen_("map", 1.0f);
                holdOwner = true;   // 시범의 자동 주인 고르기를 멈춘다 — 이 단언은 창을 직접 누른다
                foreach (var (cid, tag) in new[] { (curse, "curse"), (gift, "gift") })
                {
                    if (cid == null) { Debug.Log($"[Demo] 주인 고르기 {tag}: 카드 없음"); continue; }
                    f.P.S.Deck.RemoveAll(x => Core.GameData.BaseId(x) == cid);
                    if (tag == "curse")
                    {
                        var got = f.P.Run.GainCard(cid);
                        Expect(got == cid && f.P.PendingNeutral == null, $"저주: 고르기 창 없이 주인 없는 id 로 덱에 ({got})");
                        var cg = CardOrder.Groups(f.P.S.Deck, f.P.Data, party3).FirstOrDefault(g => g.Ids.Contains(cid));
                        Expect(cg != null && cg.Kind == CardOrder.Kind.Curse, $"저주: 덱 목록 「저주」 묶음 ({cg?.Kind})");
                        continue;
                    }
                    Expect(f.P.Run.GainCard(cid) == null && f.P.PendingNeutral == cid, $"주인 고르기 {tag}: 얻으면 주인을 기다린다 ({cid})");
                    bool fin = false;
                    f.PickOwners(() => fin = true);
                    yield return Wait(1.2f);
                    yield return Shot($"owner_{tag}");
                    Trunc("주인 " + tag);
                    yield return Press("owner:" + party3[1], 1.2f);
                    var owned = f.P.S.Deck.FirstOrDefault(x => Core.GameData.BaseId(x) == cid);
                    Expect(fin && Core.GameData.OwnerOf(owned) == party3[1], $"주인 고르기 {tag}: 덱 id {owned} · 주인 {Core.GameData.OwnerOf(owned)} · 끝 {fin}");
                    var order = CardOrder.Sort(f.P.S.Deck, f.P.Data, party3);
                    var grp = order.Where(x => (f.P.Data.Card(x)?.Hero ?? Core.GameData.OwnerOf(x)) == party3[1]).ToList();
                    Expect(grp.Count > 0 && grp.IndexOf(owned) >= 0, $"주인 고르기 {tag}: 덱 정렬 — {party3[1]} 묶음 {grp.Count}장에 든다");
                }
                if (Hot("deck") != null)
                {
                    yield return Press("deck", 1.2f);
                    yield return Shot("deck_groups");
                    var sr = f.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                    if (sr != null) { sr.verticalNormalizedPosition = 0.5f; yield return Wait(0.4f); yield return Shot("deck_groups_mid"); }
                    CloseModals();
                }
                holdOwner = false;
            }
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        // 결과 칸 단언 — 칸(gains) 안의 받은 것(줄 · 기록 · 카드)이 스크롤 내용 안에 다 들어 있고, 내용이 보이는 칸보다 크면 스크롤이 켜져 있고,
        // 칸이 화면 안 · 떠나기 단추와 겹치지 않는지
        int fitBad, fitSeen, fitScroll, alignBad;
        int artSeen, artBad;

        // 대상 그림 — 대상이 있어야 하는 이벤트가 움직이는 그림(SD · 스탠딩 · NPC · 적 스파인) 없이 정지 그림 · 빈 자리로 떨어지면 오류
        void ArtCheck(Core.EventDef ev)
        {
            var a = f.LastTargetArt;
            if (a == null || a == "nobody") return;
            artSeen++;
            if (a == "still" || a == "none") { artBad++; Debug.LogWarning($"[Demo] 대상 그림 없음 {ev.Id} 「{ev.Name}」 — {a} (npc {ev.Npc ?? "-"})"); }
        }

        void Fit(string tag, float expLeft = float.NaN)
        {
            Canvas.ForceUpdateCanvases();
            var panel = f.Stage.Root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(r => r.name == "gains" && r.gameObject.activeInHierarchy);
            if (panel == null) return;
            fitSeen++;
            Rect W(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y); }
            bool Inside(Rect a, Rect box, float tol = 2f) => a.xMin >= box.xMin - tol && a.xMax <= box.xMax + tol && a.yMin >= box.yMin - tol && a.yMax <= box.yMax + tol;
            var bad = new List<string>();
            var sr = panel.GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            var content = sr != null ? sr.content : null;
            var view = sr != null ? sr.viewport ?? (RectTransform)sr.transform : null;
            if (content == null) { bad.Add("스크롤 내용 없음"); }
            else
            {
                if (view == null || view.name != "view") view = content.parent as RectTransform;
                var cr = W(content); var vr = W(view); var pr = W(panel);
                foreach (var r in content.GetComponentsInChildren<RectTransform>())
                {
                    if (r == content) continue;
                    if (!(r.name.StartsWith("gain") || r.name.StartsWith("got ") || r.name == "log" || r.name == "rows" || r.name == "cards")) continue;
                    if (!Inside(W(r), cr)) bad.Add($"{r.name} 내용 밖");
                }
                bool scroll = content.rect.height > view.rect.height + 2;
                if (scroll) { fitScroll++; if (!sr.vertical || !sr.enabled) bad.Add("넘치는데 스크롤 꺼짐"); }
                if (!Inside(vr, pr)) bad.Add("보이는 칸이 칸 밖");
                var sc = W(f.Stage.Root);
                if (!Inside(pr, sc)) bad.Add("칸이 화면 밖");
                var lv = Hot("event.leave");
                if (lv != null && W((RectTransform)lv.transform).Overlaps(pr)) bad.Add("떠나기 단추와 겹침");
            }
            // 왼쪽 정렬 — 칸 머리 · 받은 것 줄 · 기록 글의 왼쪽 끝이 칸 왼쪽 + EvTextInset, 칸 왼쪽 = 선택지 묶음 왼쪽(같은 세로줄)
            float Lx(RectTransform r) { var c = new Vector3[4]; r.GetWorldCorners(c); return panel.InverseTransformPoint(c[0]).x - panel.rect.xMin; }
            var heads = panel.GetComponentsInChildren<RectTransform>().Where(r => r.name == "label" && r.parent == panel).ToList();
            var starts = heads.Concat(content != null ? content.Cast<Transform>().OfType<RectTransform>().Where(r => r.gameObject.activeSelf) : Enumerable.Empty<RectTransform>()).ToList();
            foreach (var r in starts)
            {
                // 줄 묶음(rows)은 첫 칸, 글 줄(gainline)은 줄 자체, 기록(log)은 글
                var probe = r.name == "rows" && r.childCount > 0 ? (RectTransform)r.GetChild(0) : r;
                float lx = Lx(probe);
                if (Mathf.Abs(lx - Flow.EvTextInset) > 2.5f) { alignBad++; bad.Add($"{r.name} 왼쪽 {lx:0.#} ≠ {Flow.EvTextInset}"); }
                var tmp = probe.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null && (tmp.alignment == TMPro.TextAlignmentOptions.MidlineRight || tmp.alignment == TMPro.TextAlignmentOptions.Right || tmp.alignment == TMPro.TextAlignmentOptions.TopRight)) { alignBad++; bad.Add($"{r.name} 글 오른쪽 정렬"); }
            }
            if (!float.IsNaN(expLeft) && Mathf.Abs(panel.anchoredPosition.x - expLeft) > 1.5f) { alignBad++; bad.Add($"칸 왼쪽 {panel.anchoredPosition.x:0} ≠ 선택지 왼쪽 {expLeft:0}"); }
            if (bad.Count > 0) { fitBad++; Debug.LogWarning($"[Demo] 결과 칸 잘림 {tag} — {string.Join(" · ", bad.Distinct().Take(6))}"); }
        }

        // 편성 「나오는 적」(-demo-partyfoe) — 적 칸 그림 · 누르기 → 적 도감 상세 → 닫기 → 편성 상태(고른 사도 · 화면) 그대로인지 단언
        IEnumerator PartyFoe_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return Press("party.auto", 0.8f);
            yield return Wait(0.6f);
            yield return Shot("party_foes");
            string Slots() => string.Join(",", f.Stage.ScreenLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Where(t => t.transform.parent != null && t.transform.parent.name == "plate").Select(t => t.text));
            var before = Slots();
            var key = f.Stage.Hot.Keys.Where(k => k.StartsWith("partyfoe:") && f.Stage.Hot[k] != null).OrderBy(k => k, StringComparer.Ordinal).Skip(2).FirstOrDefault();
            Expect(key != null, "적 칸이 있다 — " + (key ?? "없음"));
            if (key != null)
            {
                yield return Press(key, 1.0f);
                Expect(f.Stage.ModalLayer.childCount > 0, "누르면 적 도감 상세가 열린다");
                yield return Shot("party_foe_detail");
                yield return Press("zoom.close", 0.6f);
                Expect(f.Stage.Current == "party" && f.Stage.ModalLayer.childCount == 0, "닫으면 편성 화면");
                Expect(Slots() == before && before.Length > 0, "편성 그대로(고른 사도 셋)");
                yield return Shot("party_foes_back");
            }
            Debug.Log($"[Demo] 편성 적 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        // 카드 표식(-demo-marks) — 신탁 · 축복 · 복제가 섞인 덱을 만들어 덱 보기 · 카드 크게 · 보스 뒤 복제 창을 찍는다
        //   첫 사도: 신탁 + 축복 → 복제 · 둘째: 축복만 + 맨 복제 · 셋째: 신탁만 → 복제 뒤 원본 신탁을 바꿈(복제본은 그대로)
        IEnumerator Marks_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return Press("party.auto", 0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.0f);
            var run = f.P.Run; var S = f.P.S; var d = f.P.Data;
            string full = null, fullCopy = null, third = null, thirdCopy = null;
            for (int i = 0; i < S.Party.Count && i < 3; i++)
            {
                var u = d.UniquesOf(S.Party[i]).Where(id => d.Card(id).Oracles.Count == 5 && !run.ViewOf(id).IsOnly && !run.ViewOf(id).IsTaboo).ToList();
                if (u.Count < 2) continue;
                foreach (var id in u) S.Deck.Add(id);   // 고유 카드 전부(덱 30장 넘게 — 칸 크기 · 스크롤 확인)
                string Bless(string id) { var c = d.Card(id); if (c.Blesses.Count > 0) return "own"; var ks = Core.Run.DivineKindsFor(run.ViewOf(id)); return ks.Count > 0 ? ks[0] : "draw"; }
                if (i == 0) { S.Flash[u[0]] = 2; S.Shin[u[0]] = Bless(u[0]); full = u[0]; fullCopy = run.AddCopy(u[0]); S.Flash[u[1]] = 4; }
                if (i == 1) { S.Shin[u[0]] = Bless(u[0]); run.AddCopy(u[1]); }
                if (i == 2) { S.Flash[u[0]] = 1; third = u[0]; thirdCopy = run.AddCopy(u[0]); S.Flash[u[0]] = 3; }
            }
            S.Deck.AddRange(S.Deck.Where(run.IsBasic).Take(8).ToList());   // 같은 기본 카드가 여러 장 — 한 장마다 한 칸
            Expect(S.Deck.Count >= 30, $"덱 {S.Deck.Count}장");
            Expect(fullCopy != null && run.MarkOf(fullCopy).Copy && run.MarkOf(fullCopy).Oracle != null && run.MarkOf(fullCopy).Blessed, $"복제본이 신탁 · 축복을 베꼈다 — {fullCopy}");
            if (third != null) Expect(run.MarkOf(thirdCopy).Oracle != run.MarkOf(third).Oracle, "원본 신탁을 바꿔도 복제본은 그대로");
            Expect(!run.FlashTargets().Any(Core.GameData.IsCopy) && !run.ShinAble(null).Any(Core.GameData.IsCopy), "복제본은 신탁 · 축복 후보가 아니다");
            f.DeckView();
            yield return Wait(1.6f);
            yield return Shot("marks_deck");
            var sr = f.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            if (sr != null) { sr.verticalNormalizedPosition = 0; yield return Wait(0.5f); yield return Shot("marks_deck_end"); }
            CloseModals();
            if (fullCopy != null)
            {
                f.CardZoom(fullCopy);
                yield return Wait(1.0f);
                yield return Shot("marks_zoom_copy");
                CloseModals();
                f.CardZoom(full);
                yield return Wait(1.0f);
                yield return Shot("marks_zoom_orig");
                CloseModals();
            }
            // 보스 뒤 복제 — 1층 보스 칸에서 보상을 다 챙긴 뒤(AfterReward)
            S.Floor = 0; S.Node = 3; S.CopyOffer = null;
            var offer = run.BossCopyOffer();
            Expect(offer.Count == Mathf.Min(3, offer.Count) && offer.Count > 0 && offer.All(id => d.Card(id).Unique && !Core.GameData.IsCopy(id)), $"보스 복제 후보 {offer.Count}장 — {string.Join(", ", offer)}");
            int before = S.Deck.Count;
            f.AfterReward();
            yield return Screen_("bosscopy", 1.4f);
            yield return Shot("marks_bosscopy");
            yield return Press("copy0", 0.4f);
            yield return Shot("marks_bosscopy_pick");
            yield return Press("copy.go", 1.0f);
            Expect(S.Deck.Count == before + 1 && S.Deck.Any(Core.GameData.IsCopy), "복제본이 한 장 더");
            Debug.Log($"[Demo] 표식 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        void OpenEvent(string id)
        {
            CloseModals();
            f.P.S.Event = new Core.EventState { Key = $"{f.P.S.Floor}:{(f.P.S.Map?.At ?? f.P.S.Node.ToString())}", Choices = new List<string> { id }, Id = id };   // 지금 칸의 열쇠 — EnterEvent 가 다시 굴리지 않게
            f.P.S.PartyHp = f.P.S.PartyMaxHp;
            f.EventStop();
        }

        void CloseModals()
        {
            for (int i = f.Stage.ModalLayer.childCount - 1; i >= 0; i--) Destroy(f.Stage.ModalLayer.GetChild(i).gameObject);
        }

        // 로비(-demo-lobby) — 해상도별 로비 캡처. -lobby-expect <키>: 앞 실행에서 저장한 메인 사도가 유지되는지 단언.
        //   -lobby-swap: 고르기 창 · 키 차이가 큰 넷(리코타 · 키디언 · 쥬비 · 티그(영웅))으로 바꿔 찍고, 없는 사도면 에르핀인지 단언(마지막에 티그(영웅) 저장)
        IEnumerator Lobby_()
        {
            yield return Wait(0.5f);
            var expect = Arg("-lobby-expect");
            if (expect != null) Expect(Flow.LobbyHeroInfo()?.key == expect, $"다시 실행해도 메인 사도 유지 — 저장 「{Settings.LobbyHero}」 · 로비 「{Flow.LobbyHeroInfo()?.key}」 · 기대 「{expect}」");
            f.Lobby();
            yield return Screen_("lobby", 1.2f);
            yield return Shot("lobby");
            if (Has("-lobby-swap"))
            {
                var saved = Settings.LobbyHero;
                Settings.LobbyHero = "없는사도";
                Expect(Flow.LobbyHeroInfo()?.key == "에르핀", $"없는 사도 → {Flow.LobbyHeroInfo()?.key}");
                Settings.LobbyHero = saved;
                bool first = true;
                foreach (var k in new[] { "리코타", "키디언", "쥬비", "티그_영웅" })
                {
                    yield return Press("lobby.swap", 1.0f);
                    if (first) { yield return Shot("lobby_picker"); first = false; }
                    var fld = f.DexSearchField;
                    if (fld != null) { fld.text = k == "티그_영웅" ? "ㅌㄱ" : k.Substring(0, 2); yield return Wait(0.4f); }
                    yield return Press("lobbyhero:" + k, 1.2f);
                    Expect(Settings.LobbyHero == k && Flow.LobbyHeroInfo()?.key == k, $"바꾸기 → 저장 「{Settings.LobbyHero}」");
                    yield return Shot("lobby_" + k);
                }
            }
            Debug.Log($"[Demo] 로비 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        // 사도 상세 카드 탭 오른쪽 판(-demo-traits) — 고학년 → 고유 효과 → 패시브(자세히 없이). 다섯 사도 · 능력치 탭 하나
        //   사도마다 카드 글 속 낱말 설명 판도 연다(고유 효과 · 엔진 키워드) — 「자세히」 없이 수치가 다 든 글(core Docs/설명글.md)
        IEnumerator Traits_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            var want = new[] { "그윈", "밍스", "레비", "리코타" };
            var keys = want.Select(k => Roster.ByKey(k)?.key).Where(k => k != null).ToList();
            Debug.Log("[Demo] 상세 사도: " + string.Join(", ", keys));
            for (int i = 0; i < keys.Count; i++)
            {
                f.HeroDetail(keys[i], keys, null, "카드");
                yield return Wait(1.4f);
                yield return Shot("traits_" + keys[i]);
                int over = f.Stage.ModalLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Count(t => t.isTextOverflowing);
                Debug.Log($"[Demo] {keys[i]} 넘친 글 {over}");
                if (TermPop.DemoOpen(false, true)) { yield return Wait(0.5f); yield return Shot("term_hero_" + keys[i]); Debug.Log($"[Demo] {keys[i]} 낱말 판 스크롤 {TermPop.OpenScrolls}"); }
                if (i == 0 && TermPop.DemoOpen(false)) { yield return Wait(0.5f); yield return Shot("term_keyword_" + keys[i]); }
                TermPop.Close(); yield return Wait(0.2f);
                if (i == 0) { f.PopTerms(f.Stage.ModalLayer, f.HeroTraitTerms(Roster.ByKey(keys[i])?.CoreId)); yield return Wait(0.6f); yield return Shot("traits_pop_" + keys[i]); Debug.Log($"[Demo] 판 묶음 스크롤 {TermPop.OpenScrolls}"); TermPop.Close(); yield return Wait(0.2f); }
                if (i == 0) { yield return Press("detail.tab:능력치", 1.0f); yield return Shot("stats_" + keys[i]); }
                yield return Press("detail.close", 0.6f);
            }
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        // 도감 검색(-demo-search) — 초성 · 부분 이름 · 섞어 쓰기 · 괄호 이름 · 결과 없음 · 도감마다 따로 기억을 캡처하고 단언한다
        int fails;
        void Expect(bool ok, string what) { Debug.Log($"[Demo] 단언 {(ok ? "OK" : "실패")} — {what}"); if (!ok) fails++; }

        IEnumerator Until(System.Func<bool> ok, float timeout)
        {
            float t = 0;
            while (!ok() && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            if (t >= timeout) Debug.LogWarning("[Demo] 기다리다 지침");
        }

        /// <summary>뒤로 키(Esc)를 가상 키보드로 한 번 — 진짜 입력 장치 없이 Stage 의 키 읽기를 그대로 거친다.</summary>
        IEnumerator PressEsc()
        {
            var kb = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("DemoBackKey");
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.Escape));
            yield return null; yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            yield return null; yield return null;
            UnityEngine.InputSystem.InputSystem.RemoveDevice(kb);
            // 창에 포커스가 없으면 입력 시스템이 키보드를 끈다(나란히 띄운 폰 창) — 그때는 같은 길(Stage.Back)을 바로 부른다
            if (!Application.isFocused) { Debug.Log("[Demo] 창 포커스 없음 — 뒤로 키 대신 Stage.Back()"); f.Stage.Back(); }
        }

        /// <summary>상점에서 장비 사기 → 곧장 장착 창 → 끼우기 → 상점으로. 골드 · 매진 · 가방 · 낀 장비를 단언한다(2026-10-06 사용자).</summary>
        IEnumerator ShopGear()
        {
            int f0 = fails;
            Expect(Hot("gear") == null, "상점 — 머리 띠 장비 단추 없음");
            var items = f.P.S.Shop.Items;
            int idx = items.FindIndex(x => x.Kind == "equip" && !x.Sold);
            if (idx < 0) { Debug.LogWarning("[Demo] 상점에 장비가 없습니다 — 장비 단언 건너뜀"); yield break; }
            var it = items[idx];
            if (f.P.S.Gold < it.Price || Hot("shop.item" + idx) == null || !Hot("shop.item" + idx).Interactable)
            {   // 시범용 — 살 수 있게 골드를 채우고 상점을 다시 세운다(단추가 꺼진 채 남지 않게)
                if (f.P.S.Gold < it.Price) f.P.S.Gold = it.Price + 50;
                yield return Press("shop.back"); yield return Screen_("camp", 0.6f);
                yield return Press("camp.shop"); yield return Screen_("shop", 1.0f);
            }
            int g0 = f.P.S.Gold;
            string hero = f.P.S.Party[0];
            var e = f.P.Data.Equip(it.Id);
            yield return Press("shop.item" + idx, 0.2f);
            yield return Until(() => Hot("gear.ok") != null && f.Stage.ModalLayer.childCount > 0, 4); yield return Wait(0.7f);
            Expect(f.P.S.Gold == g0 - it.Price && it.Sold && f.P.Bought(it.Id), $"장비 사기 — 장착 창 전에 골드 {g0}→{f.P.S.Gold}(값 {it.Price}) · 매진 {it.Sold}");
            Expect(Hot("gear.ok") != null, "장비 사기 — 곧장 장착 창");
            Expect(Hot("gear:sell") == null, "산 장비 — 팔기 단추 없음");
            yield return Shot("gear_shop");
            string want = e?.Affinity != null && f.P.S.Party.Contains(e.Affinity) ? e.Affinity : hero;
            if (Hot("gear.ok") == null) { Debug.Log($"[Demo] 상점 장비 단언 실패 {fails - f0}"); yield break; }
            Expect(e?.Affinity == null || !f.P.S.Party.Contains(e.Affinity) || Hot("gear.ok").Interactable, "애착 장비 — 그 사도가 미리 골라져 있다");
            if (!Hot("gear.ok").Interactable) yield return Press("gear:1", 0.4f);
            yield return Press("gear.ok", 0.2f);
            yield return Until(() => f.P.S.Bag.Count == 0 && f.Stage.ModalLayer.childCount == 0, 4); yield return Wait(1.2f);
            var worn = f.P.GearOf(want);
            Expect(worn != null && worn.ContainsValue(it.Id) && f.P.S.Bag.Count == 0 && f.Stage.Current == "shop", $"끼우기 → 상점으로 · {want} 칸에 「{e?.Name}」 · 가방 {f.P.S.Bag.Count} · 화면 {f.Stage.Current}");
            Expect(f.P.S.Gold == g0 - it.Price, $"끼운 뒤 골드 그대로 {f.P.S.Gold}");
            yield return Shot("shop_after_gear");
            Debug.Log($"[Demo] 상점 장비 단언 실패 {fails - f0}");
        }

        /// <summary>휴식은 쉬기 · 수련 가운데 하나를 반드시 — 출발 · 취소 단추가 없고 뒤로 키도 듣지 않는다. 수련은 신탁 연출(등장 · 고른 순간 · 다시 태어남).
        /// 시범용으로 첫 사도의 고유 카드 둘을 덱에(하나는 수련, 하나는 보상의 빛나는 카드).</summary>
        IEnumerator CampMust()
        {
            int f0 = fails;
            foreach (var u in f.P.Data.UniquesOf(f.P.S.Party[0]).Take(2)) if (!f.P.S.Deck.Contains(u)) f.P.S.Deck.Add(u);
            f.Camp("camp");
            yield return Screen_("camp", 1.0f);
            yield return Shot("camp_first");
            Expect(Hot("camp.leave") == null, "휴식 — 고르기 전에는 출발 단추가 없다");
            int b0 = f.Stage.Backs;
            yield return PressEsc(); yield return Wait(0.4f);
            Expect(f.Stage.Backs > b0 && f.Stage.Current == "camp" && f.P.CampChoice == "" && f.Stage.ModalLayer.childCount == 0, $"휴식 — 뒤로 키를 눌러도 그대로(키 {f.Stage.Backs - b0}번 · 화면 {f.Stage.Current} · 선택 「{f.P.CampChoice}」)");
            if (Hot("camp.trainopen") == null || !Hot("camp.trainopen").Interactable) { Debug.LogWarning("[Demo] 수련할 카드가 없습니다 — 연출 캡처 건너뜀"); yield break; }
            yield return Press("camp.trainopen", 0.1f);
            yield return Until(() => OracleReveal.Phase == "choose", 5); yield return Wait(0.35f);
            yield return Shot("camp_train");
            b0 = f.Stage.Backs;
            yield return PressEsc(); yield return Wait(0.4f);
            Expect(f.Stage.Backs > b0 && OracleReveal.Phase == "choose" && Hot("camp.train.close") == null && f.P.CampChoice == "", "수련 고르기 — 뒤로 키를 눌러도 창이 그대로 · 닫기 단추 없음");
            // 덱 보기 — 신탁 창 위에 열리고, 닫으면 같은 신탁 창(같은 후보 · 고르기 그대로)
            var names0 = string.Join("|", f.Stage.ModalLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(t => t.transform.parent && t.transform.parent.name == "label").Select(t => t.text));
            int d0 = OracleReveal.DeckOpens;
            yield return Press("camp.train.deck", 0.9f);
            bool deckUp = f.Stage.ModalLayer.Cast<Transform>().Any(t => t.name == "modal deck");
            Expect(OracleReveal.DeckOpens == d0 + 1 && deckUp && OracleReveal.Phase == "choose", "수련 고르기 — 덱 보기가 신탁 창 위에 열린다");
            yield return Shot("camp_train_deck");
            yield return PressEsc(); yield return Wait(0.5f);
            deckUp = f.Stage.ModalLayer.Cast<Transform>().Any(t => t.name == "modal deck");
            var names1 = string.Join("|", f.Stage.ModalLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Where(t => t.transform.parent && t.transform.parent.name == "label").Select(t => t.text));
            Expect(!deckUp && OracleReveal.Phase == "choose" && names0 == names1 && f.P.CampChoice == "", $"덱 보기를 닫으면 같은 신탁 창 그대로({names1})");
            yield return Press("camp.train1", 0.05f);
            yield return Until(() => OracleReveal.Phase == "burn", 3); yield return Wait(0.2f);
            yield return Shot("camp_train_pick");
            yield return Until(() => OracleReveal.Phase == "reborn", 3); yield return Wait(0.3f);
            yield return Shot("camp_train_reborn");
            yield return Until(() => !OracleReveal.IsOpen, 6); yield return Wait(0.8f);
            Expect(f.P.CampChoice == "train" && Hot("camp.leave") != null, "수련을 고르면 출발 단추가 선다");
            yield return Shot("camp_trained");
            Debug.Log($"[Demo] 휴식 단언 실패 {fails - f0}");
        }
        IEnumerator Type(string q)
        {
            var fld = f.DexSearchField;
            if (fld == null) { Expect(false, "검색 칸 없음"); yield break; }
            fld.text = q;
            yield return Wait(0.5f);
        }
        IEnumerator Search_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Shot("lobby_menu");   // 메뉴 이름 「도감」
            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.2f);
            yield return Shot("search_empty");
            var all = f.DexSearchShown.Count;
            yield return Type("ㅌㄱ");
            yield return Shot("search_chosung");
            Expect(f.DexSearchShown.Contains("티그") && f.DexSearchShown.All(n => HangulSearch.Match(n, "ㅌㄱ")), $"초성 「ㅌㄱ」 → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("티ㄱ");
            Expect(f.DexSearchShown.Contains("티그"), $"섞어 쓰기 「티ㄱ」 → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("왕년");
            yield return Shot("search_partial");
            Expect(f.DexSearchShown.Count >= 1 && f.DexSearchShown.All(n => n.Contains("왕년")), $"부분 이름 「왕년」 → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("ㄷㅇㄴ");
            Expect(f.DexSearchShown.Any(n => n.StartsWith("디아나")), $"초성 「ㄷㅇㄴ」(괄호 이름) → {string.Join(", ", f.DexSearchShown)}");
            yield return Type("ㅋㅋㅋ없는사도");
            yield return Shot("search_none");
            var nm = f.Stage.ScreenLayer.GetComponentsInChildren<TMPro.TextMeshProUGUI>(false).Any(t => t.text == "찾는 것이 없습니다");
            Expect(f.DexSearchShown.Count == 0 && nm, "결과 없음 → 「찾는 것이 없습니다」");
            yield return Type("ㄷㅇㄴ");
            // 도감을 바꾸면 그 도감의 검색어(비어 있음), 돌아오면 사도 검색어 그대로
            yield return Press("tab:장비", 1.0f);
            Expect(f.DexSearchField != null && f.DexSearchField.text == "", "장비 도감 검색어는 따로(빈 칸)");
            var eqName = f.DexSearchShown.FirstOrDefault();
            if (eqName != null && eqName.Length >= 2)
            {
                var part = eqName.Substring(eqName.Length - 2);
                yield return Type(part);
                yield return Shot("search_equip");
                Expect(f.DexSearchShown.Contains(eqName), $"장비 부분 이름 「{part}」 → {string.Join(", ", f.DexSearchShown)}");
            }
            yield return Press("tab:교주 카드", 1.0f);
            var cName = f.DexSearchShown.FirstOrDefault();
            if (cName != null) { var q = new string(cName.Take(2).Select(HangulSearch.Initial).ToArray()); yield return Type(q); Expect(f.DexSearchShown.Contains(cName), $"교주 카드 초성 「{q}」 → {string.Join(", ", f.DexSearchShown)}"); yield return Shot("search_cards"); }
            yield return Press("tab:적", 1.0f);
            var fName = f.DexSearchShown.FirstOrDefault();
            if (fName != null) { yield return Type(fName.Substring(0, 1)); Expect(f.DexSearchShown.Contains(fName), $"적 「{fName.Substring(0, 1)}」 → {f.DexSearchShown.Count}종"); yield return Shot("search_foes"); }
            yield return Press("tab:사도", 1.0f);
            Expect(f.DexSearchField != null && f.DexSearchField.text == "ㄷㅇㄴ", $"사도 도감으로 돌아오면 검색어 그대로 — 「{f.DexSearchField?.text}」");
            yield return Shot("search_back");
            if (Hot("dex.search.clear") != null) { yield return Press("dex.search.clear", 0.5f); Expect(f.DexSearchShown.Count == all, $"지우기(×) → {f.DexSearchShown.Count}/{all}명"); }
            Debug.Log($"[Demo] 검색 단언 실패 {fails}");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(fails == 0 ? 0 : 4);
        }

        // 스탠딩 맞춤 시트만(-demo-sheet) — 숙인 · 기운 사도의 전신 · 무릎께 · 카드 그림 · 얼굴 칸을 한 장으로(-nohead 와 같이 돌리면 고치기 전)
        IEnumerator Sheet_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            if (Has("-sheet-compare")) { CompareSheet(); yield return Wait(1.5f); yield return Shot("height_compare"); }
            else { LiveSheet(); yield return Wait(1.5f); yield return Shot("standing_sheet_bent"); }
            Debug.Log("[Demo] 끝");
            yield return Wait(0.3f);
            Application.Quit(0);
        }

        // 짧은 데모(-demo-roster) — 사도 화면 셋 · 도감 버그 확인 · 카드(상세 · 덱 · 보상 · 신탁)만 보고 끝낸다
        //   버그 확인: 도감에서 사도 A 를 한 번 눌러 상세 → 닫기 → 사도 B 를 한 번 눌러 바로 B 상세가 떠야 한다 · 스크롤 자리는 그대로
        IEnumerator Roster_()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Shot("lobby");   // 로비 스탠딩 가장자리(흰 테두리) 확인
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(3.0f);
            yield return Shot("village_settled");   // 층 · 적 속성이 다 펼쳐진 뒤
            yield return Press("go");
            yield return Screen_("party", 1.0f);
            yield return Shot("party_empty");
            var keys = Roster.All.Where(h => h.Playable).Select(h => h.key).Take(3).ToList();
            yield return Press("slot0", 1.0f);
            yield return Shot("herolist");
            foreach (var k in keys) yield return Press("hero:" + k, 0.4f);
            yield return Wait(0.4f);
            yield return Shot("herolist_picked");
            yield return Press("list.done", 0.8f);
            yield return Wait(0.5f);
            yield return Shot("party_full");
            // 편성 큰 카드의 「고유 효과」 — 판 묶음(고학년 · 고유 효과 · 패시브, 수치가 다 든 글)
            if (Hot("party.traits0") != null)
            {
                yield return Press("party.traits0", 0.6f);
                yield return Shot("party_traits");
                TermPop.Close(); yield return Wait(0.2f);
            }
            yield return Press("zoom0", 1.0f);
            yield return Shot("hero_detail");
            yield return Press("detail.tab:카드", 1.0f);
            yield return Shot("hero_detail_cards");
            // 카드 글 속 낱말 설명 판(TermPop) — 키워드 하나 · 사도 고유 효과 · 생성 카드 하나(있으면)
            if (TermPop.DemoOpen(false)) { yield return Wait(0.5f); yield return Shot("termpop_keyword"); }
            if (TermPop.DemoOpen(false, true)) { yield return Wait(0.5f); yield return Shot("termpop_hero"); }
            if (TermPop.DemoOpen(true)) { yield return Wait(0.5f); yield return Shot("termpop_card"); }
            TermPop.Close();
            // 다른 사도(생성 카드가 있는 사도 — 시온)의 카드 탭에서 생성 카드 판
            var gen = keys.Select(Roster.ByKey).FirstOrDefault(h => h?.CoreId != null && f.P.Data.UniquesOf(h.CoreId).Concat(f.P.Data.Hero(h.CoreId).Starter)
                .Any(id => f.P.Data.Card(id)?.Fx.Any(x => x.K == Core.FxK.Make || x.K == Core.FxK.Transform) == true || f.P.Data.Card(id)?.Evolve != null || f.P.Data.Card(id)?.BondCard != null));
            yield return Press("detail.close", 0.6f);
            if (gen != null)
            {
                f.HeroDetail(gen.key, null, null, "카드");
                yield return Wait(1.2f);
                yield return Shot("hero_detail_cards_gen");
                if (TermPop.DemoOpen(true)) { yield return Wait(0.6f); yield return Shot("termpop_card_gen"); }
                TermPop.Close();
                var cz = FirstHot("detail.card고유 카드");
                if (Hot(cz) != null) { yield return Press(cz, 1.0f); yield return Shot("card_zoom_terms"); yield return Press("zoom.close", 0.5f); }
                yield return Press("detail.close", 0.6f);
            }

            // 도감 — 버그 확인(가비아 → 닫기 → 그윈, 한 번씩)
            f.Dex(f.Lobby);
            yield return Screen_("dex", 1.0f);
            var sr = f.Stage.ScreenLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            string a = Roster.All.Any(h => h.key == "가비아") ? "가비아" : Roster.All[5].key;
            string b = Roster.All.Any(h => h.key == "그윈") ? "그윈" : Roster.All[6].key;
            // A 가 보이게 내려 둔다(스크롤이 돌아가면 목록이 「초기화」된 것처럼 보인다)
            if (sr != null && Hot("hero:" + a) != null)
            {
                var ca = (RectTransform)Hot("hero:" + a).transform;
                sr.content.anchoredPosition = new Vector2(0, Mathf.Max(0, -ca.anchoredPosition.y - 120));
                yield return Wait(0.3f);
            }
            float y0 = sr != null ? sr.content.anchoredPosition.y : 0;
            yield return Shot("dex_list");
            yield return Press("hero:" + a, 1.0f);
            Check(a, "첫 누름");
            yield return Shot("dex_detail_a");
            yield return Press("detail.close", 0.6f);
            sr = f.Stage.ScreenLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            float y1 = sr != null ? sr.content.anchoredPosition.y : 0;
            Debug.Log($"[Demo] 확인 — 상세에서 돌아온 스크롤 {y0:F0} → {y1:F0} {(Mathf.Abs(y0 - y1) < 2 ? "그대로" : "달라짐(버그)")}");
            yield return Shot("dex_back");
            yield return Press("hero:" + b, 1.0f);
            Check(b, "돌아와 다음 사도 첫 누름");
            yield return Shot("dex_detail_b");
            yield return Press("detail.tab:카드", 1.0f);
            yield return Shot("dex_detail_b_cards");
            yield return Press("detail.close", 0.6f);

            // 도감 탭 — 교주 카드 · 장비(같은 화면 틀) · 상세
            yield return Press("tab:교주 카드", 1.0f);
            yield return Shot("dex_cards");
            yield return Press(FirstHot("dexcard:"), 1.0f);
            yield return Shot("dex_card_zoom");
            yield return Press("zoom.close", 0.5f);
            yield return Press("tab:장비", 1.0f);
            yield return Shot("dex_equips");
            yield return Press("filter:무기", 0.8f);
            yield return Press("filter:전설", 0.8f);
            yield return Shot("dex_equips_filter");
            yield return Press(FirstHot("dexequip:"), 1.0f);
            yield return Shot("dex_equip_detail");
            yield return Press("zoom.close", 0.5f);
            // 적 도감 — 전체 · 마을 하나 + 보스 · 상세
            yield return Press("tab:적", 1.0f);
            yield return Shot("dex_foes");
            yield return Press(FirstHot("dexfoe:"), 1.0f);
            yield return Shot("dex_foe_detail");
            yield return Press("zoom.close", 0.5f);
            yield return Press("filter:보스", 0.8f);
            yield return Shot("dex_foes_boss");
            yield return Press(FirstHot("dexfoe:"), 1.0f);
            yield return Shot("dex_foe_boss_detail");
            yield return Press("zoom.close", 0.5f);
            yield return Press("filter:소환물", 0.8f);
            yield return Press(FirstHot("dexfoe:"), 1.0f);
            yield return Shot("dex_foe_summon_detail");
            yield return Press("zoom.close", 0.5f);
            yield return Press("tab:사도", 1.0f);
            yield return Shot("dex_back_heroes");
            if (tail == "") { StandingSheet(); yield return Wait(1.2f); yield return Shot("standing_sheet"); Destroy(sheet); }
            // 스탠딩 스파인 맞춤(standing_fit.json) — 같은 몸 키 px 로 전신 · 상반신을 나란히(바닥선 · 몸 크기 비 확인)
            LiveSheet(); yield return Wait(1.5f); yield return Shot("standing_live"); Destroy(sheet);

            // 판 하나 — 지도 · 덱 보기 · 카드 크게 · 싸움 · 보상(신탁 카드)
            f.Lobby();
            yield return Screen_("lobby", 0.8f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(1.4f);
            yield return Press("go");
            yield return Screen_("party", 0.8f);
            yield return Press("party.auto", 0.6f);
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            yield return Press("deck", 1.2f);
            yield return Shot("deck");
            if (Hot("deck.card0") != null) { yield return Press("deck.card0", 0.9f); yield return Shot("card_zoom"); yield return Press("zoom.close", 0.4f); }
            yield return Press("deck.close", 0.6f);
            yield return OwnerPick();
            // 상점 · 장비 고르기(장비 · 교주 카드 그림) — 캠프 상점을 바로 연다
            yield return CampMust();
            f.Camp("campshop");
            yield return Screen_("camp", 1.0f);
            yield return Shot("camp_shop");   // 휴식+상점 — 좌판 단추를 누르기 전(골디 없음) · 출발 단추 없음
            yield return Press("camp.shop");
            yield return Screen_("shop", 1.2f);
            yield return Shot("shop");
            yield return ShopGear();
            if (f.FightScreen != null) { Debug.Log("[Demo] 끝(전투는 -battle 시범이 찍는다)"); yield return Wait(0.4f); Application.Quit(0); yield break; }
            yield return Press("shop.back");
            yield return Screen_("camp", 0.8f);
            Expect(f.P.CampChoice == "" && Hot("camp.leave") == null, "휴식+상점 — 좌판을 드나들어도 쉬기 · 수련은 아직 골라야 한다");
            yield return Press("camp.rest", 1.2f);
            Expect(Hot("camp.leave") != null, "쉬기를 고르면 출발 단추가 선다");
            yield return Press("camp.leave");
            yield return Screen_("map", 1.2f);
            var reach = f.P.Reachable();
            var map = f.P.Map();
            var fight = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.First();
            var uq = f.P.Run.FlashTargets().FirstOrDefault(x => f.P.Data.Card(x)?.Unique == true);   // 보상의 신탁 고르기(연출 · 번호 없이 이름만)를 보이려고 고유 카드 하나를 빛나게 — 시범용
            if (uq != null) f.P.S.ForceGlow = new Dictionary<string, Core.Glow> { [uq] = new Core.Glow { Kind = "card", Picks = f.P.Run.RollOracles(uq) } };
            yield return Press("node:" + fight, 1.2f);
            yield return GoFight("win");
            f.P.S.ForceGlow = null;
            Debug.Log("[Demo] 끝");
            yield return Wait(0.4f);
            Application.Quit(0);
        }

        // 교주 카드 주인 고르기 — core API 전이라 가짜 카드(피해 · 방어 배율)로 창만 찍는다. 그 뒤 진짜 교주 카드 한 장을 주인 셋 · 없음으로 나란히(틀 색 · 핀)
        IEnumerator OwnerPick()
        {
            var party = f.P.S.Party.ToList();
            var def = new Core.CardDef { Id = "demo_leader", Name = "교주의 일격", Type = "공격", Cost = 1, Grade = "고급" };
            def.Fx.Add(new Core.Fx { K = Core.FxK.Dmg, Ratio = 0.7, Hits = 2 });
            def.Fx.Add(new Core.Fx { K = Core.FxK.Block, Ratio = 0.8 });
            var fv = new Core.CardView("demo_leader", def, null, 0);
            string got = null;
            holdOwner = true;
            f.OwnerWindow("demo_leader", k => got = k, fv);
            yield return Wait(1.2f);
            yield return Shot("owner_pick");
            if (Hot("owner.traits:" + party[0]) != null) { yield return Press("owner.traits:" + party[0], 0.6f); yield return Shot("owner_traits"); TermPop.Close(); yield return Wait(0.3f); }
            if (party.Count > 1) yield return Press("owner:" + party[1], 0.25f);
            yield return Shot("owner_picked");
            yield return Wait(1.0f);
            Debug.Log($"[Demo] 확인 — 교주 카드 주인 고르기(창): {(got == party.ElementAtOrDefault(1) ? "골랐음 " + got : "안 골라짐(버그)")}");

            var leader = f.P.Data.Cards.Values.Where(c => c.Neutral).Select(c => c.Id).OrderBy(x => x, StringComparer.Ordinal).FirstOrDefault();
            // 진짜 흐름(core API) — 교주 카드를 얻으면 PendingNeutral → 고르기 창 → AssignNeutral → 덱에 「id@사도」
            if (leader != null && party.Count > 2)
            {
                f.P.Run.GainCard(leader);
                bool fin = false;
                f.PickOwners(() => fin = true);
                yield return Wait(1.2f);
                yield return Shot("owner_real");
                yield return Press("owner:" + party[2], 1.2f);
                var owned = f.P.S.Deck.FirstOrDefault(x => Core.GameData.BaseId(x) == leader);
                Debug.Log($"[Demo] 확인 — 교주 카드 주인(core): 덱 id {owned} · 주인 {Core.GameData.OwnerOf(owned)} · 기다림 {f.P.PendingNeutral ?? "없음"} · 끝 {fin}");
                yield return Press("deck", 1.2f);
                var sr = f.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                if (sr != null) { sr.verticalNormalizedPosition = 0; yield return Wait(0.4f); }
                yield return Shot("deck_leader_owner");
                yield return Press("deck.close", 0.6f);
            }
            holdOwner = false;
            if (leader != null)
            {
                var (panel, close) = f.Stage.Modal("ownertint", 1100, 470);
                var row = Ui.Rect("row", panel).Fill(20, 20, 20, 20);
                Ui.Row(row, 24, TextAnchor.MiddleCenter, null, false, false);
                foreach (var k in party.Take(3).Prepend(null))
                {
                    var col = Ui.Rect("col", row); col.Pref(220, 420);
                    var c = W.Card(col, f, Core.GameData.WithOwner(leader, k), 220, "card"); c.At(0.5f, 1, 0, 0, 220, 308);
                    var t = Ui.Text(col, k == null ? "주인 없음(금빛 중립)" : Roster.OfCore(k).ko + " 덱", Theme.FsBody, Theme.Sub, TMPro.TextAlignmentOptions.Center);
                    t.rectTransform.At(0.5f, 0, 0, 40, 220, 30);
                }
                yield return Wait(0.8f);
                yield return Shot("owner_tint");
                close();
                yield return Wait(0.4f);
            }
        }

        // 교주 카드 주인 고르기 창이 뜨면 첫 사도로 저절로(긴 데모가 멈추지 않게) — OwnerPick 이 찍는 동안은 쉰다
        bool holdOwner;
        IEnumerator AutoOwner()
        {
            while (true)
            {
                yield return Wait(0.6f);
                if (holdOwner || f.P?.S == null || f.Stage.ModalLayer.Find("modal ownerpick") == null) continue;
                var k = f.P.S.Party.FirstOrDefault(x => Hot("owner:" + x) != null);
                if (k != null) yield return Press("owner:" + k, 0.8f);
            }
        }

        string FirstHot(string prefix) => f.Stage.Hot.Where(kv => kv.Key.StartsWith(prefix) && kv.Value != null).Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal).FirstOrDefault() ?? prefix;

        // 135명 상반신 모아 보기(카드 자르기와 같은 규칙 — CardArt.Upper 0.7 비율 · 위 5.6할) — 머리 자리 판정을 눈으로 확인
        GameObject sheet;
        void StandingSheet()
        {
            var layer = Ui.Rect("modal standingsheet", f.Stage.ModalLayer).Fill();
            sheet = layer.gameObject;
            Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
            var size = f.Stage.Size;
            int cols = 20;
            int rows = Mathf.CeilToInt(Roster.All.Count / (float)cols);
            float ch = Mathf.Floor(Mathf.Min((size.x - 20) / cols / 0.7f, (size.y - 10) / rows - 12)), cw = Mathf.Floor(ch * 0.7f);
            int i = 0;
            foreach (var h in Roster.All.OrderBy(x => x.art, StringComparer.Ordinal))
            {
                float x = 10 + (i % cols) * cw, y = -6 - (i / cols) * (ch + 12);
                var sp = CardArt.Upper(h.art, 0.7f, 0.56f);
                var im = Ui.Img(layer, sp ?? h.Icon, sp != null ? Color.white : Color.white.A(0.5f), h.key);
                im.rectTransform.At(0, 1, x + 1, y, cw - 2, ch);
                var t = Ui.Text(layer, h.ko, 10, sp != null ? Theme.Ink : Theme.Bad, TMPro.TextAlignmentOptions.Top);
                t.rectTransform.At(0, 1, x, y - ch, cw, 12); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 6; t.fontSizeMax = 10;
                i++;
            }
        }

        // 키 비교 판(-sheet-compare) — 묶음마다 [티그(영웅) | 원판 | 다른 판], 같은 배율 · 같은 바닥선, 머리 꼭대기 가는 선(금 = 티그 기준)
        void CompareSheet()
        {
            var layer = Ui.Rect("modal comparesheet", f.Stage.ModalLayer).Fill();
            sheet = layer.gameObject;
            Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
            const string tig = "tighero";
            var groups = new[]
            {
                new[] { "vivi", "vividivine" }, new[] { "daya", "dayapureshine" }, new[] { "erpin", "erpinroyale" },
                new[] { "ed", "edrehab" }, new[] { "diana", "dianayester" }, new[] { "ui", "uimemory" },
                new[] { "amelia", "ameliar41" }, new[] { "ner", "nerrage" }, new[] { "lethe", "rollett" },
            };
            var all = groups.SelectMany(g => g).Append(tig).ToList();
            float tall = StandingFit.Tallest(all);
            var size = f.Stage.Size;
            int rows = 3, perRow = 3;
            float top = 56, rowH = (size.y - top - 10) / rows, labelH = 46, figH = rowH - labelH - 8;
            float groupW = (size.x - 40) / perRow, cw = (groupW - 30) / 3;
            var head = Ui.Title(layer, "키 비교 — 묶음마다 티그(영웅) · 원판 · 다른 판 (손보정 · 짝 적용, 같은 배율 · 같은 바닥선) · 금빛 선 = 티그(영웅) 머리 꼭대기, 흰 선 = 그 사도 머리 꼭대기", Theme.FsMd, Theme.Gold, TMPro.TextAlignmentOptions.MidlineLeft);
            head.rectTransform.At(0, 1, 24, -10, size.x - 48, 36);
            StandingFit.TryGet(tig, out var tf);
            var log = new System.Text.StringBuilder("[Demo] 키 비교(티그(영웅) 몸 " + tf.Body.ToString("0") + "):");
            for (int gi = 0; gi < groups.Length; gi++)
            {
                int r = gi / perRow, c = gi % perRow;
                float gx = 20 + c * groupW, gy = -(top + r * rowH);
                var box = Ui.Img(layer, Theme.Round, Theme.NavyCell.A(0.45f), "group"); box.rectTransform.At(0, 1, gx + 4, gy, groupW - 8, rowH - 6);
                var members = new[] { tig }.Concat(groups[gi]).ToArray();
                float floorY = gy - figH;   // 칸 아래(바닥선)
                float tigTop = 0;
                for (int i = 0; i < members.Length; i++)
                {
                    var a = members[i];
                    float x = gx + 15 + i * cw;
                    var slot = Ui.Rect("fig " + a, layer).At(0, 1, x, gy, cw, figH);
                    SpineUi.Standing(slot, a, cw, figH, StandMode.Full, tall);
                    StandingFit.TryGet(a, out var fit);
                    if (StandingFit.Solve(a, new Rect(0, 0, cw, figH), StandMode.Full, tall, 0, out var k, out var o))
                    {
                        float yTop = o.y + fit.TopY * k;   // 칸 아래에서 머리 꼭대기까지
                        if (i == 0) tigTop = yTop;
                        var ln = Ui.Img(layer, Theme.White, (i == 0 ? Theme.Gold : Color.white).A(i == 0 ? 0.9f : 0.75f), "headline");
                        if (i == 0) ln.rectTransform.At(0, 1, gx + 10, floorY + tigTop, groupW - 20, 2);
                        else ln.rectTransform.At(0, 1, x + 6, floorY + yTop, cw - 12, 2);
                    }
                    string ko = Roster.All.FirstOrDefault(h => h.art == a)?.ko ?? fit.Hero ?? a;
                    float ratio = fit.Body / Mathf.Max(1, tf.Body);
                    var t = Ui.Text(layer, $"<b>{ko}</b>\n<size=80%><color={Theme.SubTag}>배율 ×{fit.Scale:0.00} · 몸 {fit.Body:0} · 티그 대비 {ratio * 100:0}%</color></size>", Theme.FsSm, i == 0 ? Theme.Gold : Theme.Ink, TMPro.TextAlignmentOptions.Top);
                    t.rectTransform.At(0, 1, x - 6, floorY - 4, cw + 12, labelH); t.textWrappingMode = TMPro.TextWrappingModes.NoWrap; t.enableAutoSizing = true; t.fontSizeMin = 9; t.fontSizeMax = Theme.FsSm;
                    if (i > 0) log.Append($" {ko} {fit.Body:0}({ratio * 100:0}%)");
                }
                var fl = Ui.Img(layer, Theme.White, Theme.Gold.A(0.6f), "floor"); fl.rectTransform.At(0, 1, gx + 10, floorY, groupW - 20, 2);
            }
            // 이름 글은 맨 위로(바위 · 탈것이 바닥선 아래로 내려와도 글을 덮지 않게)
            foreach (var tx in layer.GetComponentsInChildren<TMPro.TextMeshProUGUI>()) tx.transform.SetAsLastSibling();
            Debug.Log(log.ToString());
        }

        void LiveSheet()
        {
            var layer = Ui.Rect("modal livesheet", f.Stage.ModalLayer).Fill();
            sheet = layer.gameObject;
            Ui.Img(layer, Theme.White, Theme.Night, "bg", true).rectTransform.Fill();
            // 숙인 · 기운 사도(얼굴 잘림 제보) + 키 짝(네르 · 네르_빡침)을 먼저 — 줄: 전신 · 편성 무릎께 · 카드 그림 · 얼굴 칸
            //   -sheet-heights: 키 짝(원판 · 다른 판 나란히) + 키 손보정 사도
            //   -sheet-center: 중심 다시 잡기(벨라 · 클로에 · 쥬비 + 같은 문제 사도) — 칸 가운데 세로선
            bool center = Has("-sheet-center");
            var want = Has("-sheet-heights")
                ? new[] { "vivi", "vividivine", "daya", "dayapureshine", "erpin", "erpinroyale", "ed", "edrehab", "diana", "dianayester", "ui", "uimemory", "amelia", "ameliar41", "ner", "nerrage", "lethe", "rollett" }
                : center ? new[] { "vela", "chloe", "jubee", "marie", "snorky", "canta", "naia", "sist", "erpin", "tighero" }
                : new[] { "selline", "diana", "dianayester", "epica", "ran", "naia", "rude", "ed", "sist", "ner", "nerrage" };
            var arts = want.Concat(Roster.All.Select(h => h.art)).Where(a => a != null && SpineUi.Data("st_" + a) != null).Distinct().Take(Has("-sheet-heights") || center ? want.Length : 12).ToList();
            var size = f.Stage.Size;
            float cw = Mathf.Min(center ? 168 : 220, (size.x - 40) / Mathf.Max(1, arts.Count)), fullH = size.y * 0.3f, kneeH = (cw - 10) * 1.4f, cardH = (cw - 16) / 0.71f, faceS = Mathf.Min(70, cw - 30);
            float tall = StandingFit.Tallest(arts);
            var floor = Ui.Img(layer, Theme.White, Theme.Gold.A(0.6f), "floor"); floor.rectTransform.At(0, 1, 20, -(14 + fullH), size.x - 40, 2);
            for (int i = 0; i < arts.Count; i++)
            {
                var a = arts[i];
                float x = 20 + i * cw, y = -14;
                var full = Ui.Rect("full " + a, layer).At(0, 1, x, y, cw, fullH);
                SpineUi.Standing(full, a, cw, fullH, StandMode.Full, tall);
                y -= fullH + 6;
                var knee = Ui.Img(layer, Theme.Round, Theme.NavyCell, "knee " + a); knee.rectTransform.At(0, 1, x + 5, y, cw - 10, kneeH);
                knee.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
                SpineUi.Standing(knee.rectTransform, a, cw - 10, kneeH, StandMode.Knee, tall);
                y -= kneeH + 6;
                var card = Ui.Img(layer, CardArt.Upper(a, 0.71f, 0.56f), Color.white, "card " + a); card.rectTransform.At(0, 1, x + 8, y, cw - 16, cardH);
                y -= cardH + 6;
                var face = Ui.Img(layer, Theme.S("circle"), Theme.NavyCell, "face " + a); face.rectTransform.At(0, 1, x + (cw - faceS) / 2, y, faceS, faceS);
                face.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = true;
                var fi = Ui.Img(face.rectTransform, CardArt.Upper(a, 1f, 0.34f), Color.white, "pic"); fi.rectTransform.Fill();
                y -= faceS + 2;
                StandingFit.TryGet(a, out var fit);
                var t = Ui.Text(layer, $"{a}{(fit.Bent ? " ·숙임" : "")}\n×{fit.Scale:0.00} 몸 {fit.Body:0}", 11, fit.Bent ? Theme.Gold : Theme.Ink, TMPro.TextAlignmentOptions.Top);
                t.rectTransform.At(0, 1, x, y, cw, 30);
                if (center)
                {
                    // 칸 가운데 세로선(전신 · 무릎께 · 카드 · 얼굴) — 몸 · 얼굴이 선에 걸쳐야 한다
                    var vl = Ui.Img(layer, Theme.White, Theme.Sky.A(0.55f), "centerline"); vl.rectTransform.At(0, 1, x + cw / 2 - 1, -14, 2, -y - 14);
                }
            }
        }

        void Check(string key, string what)
        {
            var open = f.Stage.ModalLayer.Cast<Transform>().LastOrDefault(t => t.name == "modal herodetail");
            var name = open != null ? open.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Select(t => t.text).FirstOrDefault(t => t == Roster.ByKey(key)?.ko) : null;
            Debug.Log($"[Demo] 확인 — {what}: {key} 상세 {(open != null && name != null ? "열림" : "안 열림(버그)")}");
        }

        bool bailing;
        void Bail(int code, string why)
        {
            if (bailing) return;
            bailing = true;
            Debug.LogError($"[Demo] 멈춤({code}) — {why}");
            StartCoroutine(BailCo(code));
        }
        IEnumerator BailCo(int code)
        {
            yield return Shot("zz_bail_" + code, false);
            if (Looping) RunPort.ClearSave();
            Application.Quit(code);
        }
        IEnumerator Watchdog(float limit)
        {
            yield return new WaitForSecondsRealtime(limit);
            Bail(3, $"{limit:F0}초가 넘었습니다 (지금 화면 {f.Stage.Current})");
        }

        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); for (int i = 0; i < a.Length - 1; i++) if (a[i] == n) return a[i + 1]; return null; }
        static bool Has(string n) => Environment.GetCommandLineArgs().Contains(n);

        IEnumerator Shot(string name, bool once = true)
        {
            if (Looping && !name.StartsWith("zz_bail")) yield break;   // 연속 싸움 시험은 캡처 없이(재는 것이 흐트러지지 않게)
            if (once && !seen.Add(name)) yield break;
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture(int.TryParse(Arg("-supersize"), out var ss) && ss > 1 ? ss : 1);   // -supersize 2 = 1920×1080 창을 3840×2160 으로(모니터보다 큰 4K 확인)
            var path = Path.Combine(dir, $"{++no:D2}_{name}{tail}.png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
            Debug.Log("[Capture] " + Path.GetFileName(path));
        }

        IEnumerator Wait(float s) { yield return new WaitForSecondsRealtime(s); }

        IEnumerator Screen_(string name, float settle = 1.1f, float timeout = 25)
        {
            float t = 0;
            while ((f.Stage.Current != name || f.Stage.Busy) && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            if (t >= timeout) Debug.LogWarning($"[Demo] 화면 {name} 을 기다리다 지침 (지금 {f.Stage.Current})");
            yield return Wait(settle);
        }

        Btn Hot(string key) => f.Stage.Hot.TryGetValue(key, out var b) && b != null ? b : null;

        IEnumerator Press(string key, float after = 0.5f)
        {
            var b = Hot(key);
            if (b == null) { Debug.LogWarning("[Demo] 버튼 없음: " + key); yield break; }
            Debug.Log("[Demo] 누름 " + key);
            yield return b.Press();
            yield return Wait(after);
        }

        // 짧은 판(-demo-quick) — 새 규칙이 판 처음부터 싸움까지 이어지는지만: 로비 → 마을 공개(판 속성 · 두 보스) → 편성(추천) → 판 열기
        //   (미리 보인 판 속성 · 보스 줄 = 실제 판의 것인지 대 본다) → 지도 → 싸움 둘(전투 화면이 붙으면 그쪽 봇) → 지도로 돌아오면 끝
        IEnumerator Quick()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.0f);
            yield return Press("start");
            yield return Screen_("village", 0.4f);
            yield return Wait(3.0f);
            yield return Shot("quick_village");
            string natShown = f.FoeNature;
            var bossShown = f.FoeBosses?.Select(l => string.Join(",", l)).ToList();
            yield return Press("go");
            yield return Screen_("party", 1.0f);
            yield return Press("party.auto", 0.6f);
            yield return Shot("quick_party");
            yield return Press("party.go");
            yield return Screen_("map", 1.4f);
            yield return Shot("quick_map");
            var run = f.P.Run;
            var bossReal = run.Bosses.Select(l => string.Join(",", l)).ToList();
            bool same = natShown == run.EnemyNature && bossShown != null && bossShown.SequenceEqual(bossReal);
            Debug.Log($"[Demo] 판 속성 화면 {natShown} · 실제 {run.EnemyNature} · 보스 화면 [{string.Join(" | ", bossShown ?? new List<string>())}] · 실제 [{string.Join(" | ", bossReal)}] · 클론 사도 [{string.Join(", ", run.BossHeroes.Select(x => x ?? "-"))}] → {(same ? "일치" : "어긋남")}");
            if (!same) { Bail(4, "마을 공개 화면의 판 속성 · 보스가 실제 판과 다르다"); yield break; }
            int fights = 0;
            for (int step = 0; step < 6 && fights < 2; step++)
            {
                float t = 0;
                while (t < 25 && (f.Stage.Busy || f.Stage.Current != "map")) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Stage.Current != "map") break;
                var reach = f.P.Reachable();
                var map = f.P.Map();
                var pick = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "fight") ?? reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == "elite") ?? reach.First();
                var type = Core.Run.NodeById(map, pick).Type;
                yield return Press("node:" + pick, 0.2f);
                yield return Wait(1f);
                switch (type)
                {
                    case "fight": case "elite": case "boss": yield return GoFight("win", type); fights++; break;
                    case "event": yield return GoEvent(); break;
                    case "camp": case "campshop": yield return GoCamp(type); break;
                }
                Debug.Log($"[Demo] {type} 지남 — 화면 {f.Stage.Current}");
            }
            yield return Wait(1.0f);
            yield return Shot("quick_after");
            Debug.Log($"[Demo] 끝(짧은 판 — 싸움 {fights}번)");
            yield return Wait(0.4f);
            Application.Quit(fights > 0 ? 0 : 5);
        }

        /// <summary>연속 싸움 시험(-demo-loop N) 중 — 판 흐름(지도 → 전투 → 보상 → 지도)으로 싸움 N 번. 캡처는 찍지 않는다.</summary>
        public static bool Looping { get; private set; }
        /// <summary>연속 싸움 시험에서 지금까지 들어간 싸움 수.</summary>
        public static int LoopFights { get; private set; }

        // 연속 싸움(-demo-loop N, 기본 30) — 전투에 들고 나기를 거듭해 무엇이 쌓이는지 잰다(전투 쪽 EnterLeak 이 돌아올 때마다 센다).
        //   로비 → 마을 → 편성(추천) → 지도에서 싸움 칸을 먼저 고른다(없으면 엘리트 · 보스 · 그 밖). 판이 끝나면 로비에서 새 판.
        IEnumerator Loop()
        {
            Looping = true;
            int want = int.TryParse(Arg("-demo-loop"), out var n) ? n : 30;
            int runs = 0;
            while (LoopFights < want && runs++ < 30)
            {
                yield return Wait(0.5f);
                f.Lobby();
                yield return Screen_("lobby", 0.8f);
                yield return Press("start");
                yield return Screen_("village", 0.4f);
                yield return Wait(2.5f);
                yield return Press("go");
                yield return Screen_("party", 0.8f);
                yield return Press("party.auto", 0.5f);
                yield return Press("party.go");
                for (int step = 0; step < 60 && LoopFights < want; step++)
                {
                    float t = 0;
                    while (t < 25 && (f.Stage.Busy || (f.Stage.Current != "map" && !f.Stage.Current.StartsWith("end")))) { t += Time.unscaledDeltaTime; yield return null; }
                    if (f.Stage.Current != "map") break;
                    yield return Wait(1.0f);
                    var reach = f.P.Reachable();
                    var map = f.P.Map();
                    string Of(string ty) => reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == ty);
                    var pick = Of("fight") ?? Of("elite") ?? Of("boss") ?? reach.First();
                    var type = Core.Run.NodeById(map, pick).Type;
                    yield return Press("node:" + pick, 0.2f);
                    yield return Wait(1f);
                    switch (type)
                    {
                        case "fight": case "elite": case "boss":
                            LoopFights++;
                            Debug.Log($"[Loop] 싸움 {LoopFights}/{want} — {type}");
                            yield return GoFight("win", type);
                            break;
                        case "event": yield return GoEvent(); break;
                        case "camp": case "campshop": yield return GoCamp(type); break;
                    }
                    if (f.Stage.Current != null && f.Stage.Current.StartsWith("end")) break;
                }
                if (f.Stage.Current != null && f.Stage.Current.StartsWith("end")) { yield return Wait(1f); }
            }
            // 마지막 싸움에서 돌아와 지도가 서고 센 뒤에 끝낸다
            float w = 0;
            while (w < 30 && (f.Stage.Busy || f.Stage.Current != "map")) { w += Time.unscaledDeltaTime; yield return null; }
            yield return Wait(4f);
            Debug.Log($"[Demo] 끝(연속 싸움 {LoopFights}번 · 판 {runs})");
            RunPort.ClearSave();   // 남긴 판을 지운다 — 다음 판 데모가 이 판으로 이어져 막히지 않게
            PlayerPrefs.Save();
            Application.Quit(0);
        }

        IEnumerator Run()
        {
            yield return Wait(0.5f);
            f.Lobby();
            yield return Screen_("lobby", 1.6f);
            yield return Shot("lobby");
            yield return Press("hero", 0.6f);
            yield return Shot("lobby_touch");
            // 버튼 상태 — 메뉴 하나에 올림(금 테두리)을 켜 둔 채 찍는다
            var hv = Hot("dex");
            if (hv != null) { hv.Hover(true); yield return Wait(0.45f); yield return Shot("lobby_hover"); hv.Hover(false); yield return Wait(0.2f); }
            yield return Press("start");

            yield return Screen_("village", 0.4f);
            yield return Shot("village_roll");
            yield return Wait(1.6f);
            yield return Shot("village");
            yield return Press("go");

            yield return Screen_("party", 1.2f);
            yield return Shot("party_empty");
            var keys = Roster.All.Where(h => h.Playable).Select(h => h.key).Take(3).ToList();
            // 칸을 누르면 사도 목록(빠른 편성) → 셋 넣기 → 완료
            yield return Press("slot0", 1.0f);
            yield return Shot("herolist");
            foreach (var k in keys) yield return Press("hero:" + k, 0.5f);
            yield return Wait(0.5f);
            yield return Shot("herolist_picked");
            yield return Press("list.done", 0.8f);
            yield return Wait(0.6f);
            yield return Shot("party_full");
            yield return Press("zoom0", 1.0f);
            yield return Shot("hero_detail");
            yield return Press("detail.tab:카드", 1.0f);
            yield return Shot("hero_detail_cards");
            yield return Press("detail.close", 0.8f);
            yield return Press("party.go");

            yield return Travel(true);
            yield return Screen_("end_clear", 2.5f);
            yield return Shot("end_clear");

            // 로비 · 도감 · 설정
            yield return Press("end.lobby");
            yield return Screen_("lobby", 1.2f);
            yield return Press("dex");
            yield return Screen_("dex", 1.2f);
            yield return Shot("dex_list");
            yield return Press("hero:" + keys[0], 0.8f);
            yield return Press("list.detail", 1.0f);
            yield return Shot("dex_hero");
            yield return Press("detail.close", 0.8f);
            yield return Press("tab:교주 카드");
            yield return Wait(1.2f);
            yield return Shot("dex_cards");
            yield return Press("tab:장비");
            yield return Wait(1.2f);
            yield return Shot("dex_equips");
            yield return Press("back");
            yield return Screen_("lobby", 1f);
            yield return Press("settings", 0.8f);
            yield return Shot("settings");
            if (!Has("-no-display-try"))
            {
                // 해상도 바꿔 보기 → 「이 화면으로 둘까요?」 → 되돌리기(데모는 창 크기를 남기지 않는다)
                int pick = DisplayOptions.PresetIndex == 1 ? 2 : 1;
                if (DisplayOptions.Fits(pick))
                {
                    yield return Press("disp.res" + pick, 1.4f);
                    yield return Shot("settings_keep");
                    yield return Press("disp.revert", 1.4f);
                    yield return Shot("settings_reverted");
                }
            }
            yield return Press("settings.close");

            // 새 판에서 지기 · 이어하기
            yield return Press("start");
            yield return Screen_("village", 2f);
            yield return Press("go");
            yield return Screen_("party", 1f);
            yield return Press("slot0", 0.9f);
            foreach (var k in keys.AsEnumerable().Reverse()) yield return Press("hero:" + k, 0.3f);
            yield return Press("list.done", 0.8f);
            yield return Press("party.go");
            yield return Screen_("map", 1.6f);
            yield return Press("settings", 0.6f);
            yield return Press("settings.lobby");
            yield return Screen_("lobby", 1.2f);
            yield return Shot("lobby_resume");
            yield return Press("start");
            yield return Screen_("map", 1.4f);
            yield return Shot("map_resumed");
            var mp = f.P.Map();
            var reachNow = f.P.Reachable();
            var any = reachNow.FirstOrDefault(id => Core.Run.NodeById(mp, id)?.Type == "fight") ?? reachNow.FirstOrDefault();
            yield return Press("node:" + any, 1.5f);
            yield return GoFight("lose");
            yield return Screen_("end_lose", 2.2f);
            yield return Shot("end_lose");
            Debug.Log("[Demo] 끝");
            yield return Wait(0.5f);
            Application.Quit(0);
        }

        // 한 판을 끝까지 — 지도마다 아직 못 본 칸 종류를 먼저 고른다
        IEnumerator Travel(bool win)
        {
            var wanted = new List<string> { "fight", "event", "campshop", "camp", "elite" };
            int guard = 0;
            while (guard++ < 60)
            {
                float t = 0;
                while (t < 25 && (f.Stage.Busy || (f.Stage.Current != "map" && !f.Stage.Current.StartsWith("end")))) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Stage.Current != "map") break;
                yield return Wait(1.5f);
                yield return Shot(f.P.S.Floor == 0 ? "map" : "map_floor2");
                if (f.P.S.Floor == 1 && !seen.Contains("deck") && Hot("deck") != null)
                {
                    // 덱 보기 — 사도별 묶음(기본 → 고유) · 맨 끝 교주 카드 · 카드 크게
                    yield return Press("deck", 1.2f);
                    yield return Shot("deck");
                    if (Hot("deck.card0") != null) { yield return Press("deck.card0", 0.9f); yield return Shot("card_zoom"); yield return Press("zoom.close", 0.4f); }
                    yield return Press("deck.close", 0.6f);
                }
                var reach = f.P.Reachable();
                var map = f.P.Map();
                string pick = null;
                foreach (var w in wanted)
                {
                    pick = reach.FirstOrDefault(id => Core.Run.NodeById(map, id)?.Type == w);
                    if (pick != null) { wanted.Remove(w); break; }
                }
                pick ??= reach.OrderBy(id => Core.Run.NodeById(map, id)?.Type == "campshop" ? 0 : Core.Run.NodeById(map, id)?.Type == "event" ? 2 : 1).First();
                var type = Core.Run.NodeById(map, pick).Type;
                if (!seen.Contains("map_walk"))
                {
                    var b = Hot("node:" + pick);
                    StartCoroutine(b.Press());
                    yield return Wait(0.62f);
                    yield return Shot("map_walk");
                }
                else yield return Press("node:" + pick, 0.2f);
                yield return Wait(1f);
                switch (type)
                {
                    case "fight": case "elite": case "boss": yield return GoFight(win ? "win" : "lose", type); break;
                    case "event": yield return GoEvent(); break;
                    case "camp": case "campshop": yield return GoCamp(type); break;
                }
                if (f.Stage.Current != null && f.Stage.Current.StartsWith("end")) break;
            }
        }

        IEnumerator GoFight(string mode, string type = "fight")
        {
            if (f.FightScreen != null)
            {
                // 전투 화면이 붙었다 — 그쪽 봇이 싸운다(캡처도 그쪽이 Snap 으로). 판 화면으로 돌아올 때까지 기다린다
                FightMode = mode;
                float t = 0;
                while (!f.Fighting && t < 15) { t += Time.unscaledDeltaTime; yield return null; }
                if (!f.Fighting) { Debug.LogWarning("[Demo] 싸움이 열리지 않았습니다 (지금 화면 " + f.Stage.Current + ")"); yield break; }
                t = 0;
                while (f.Fighting && t < 400) { t += Time.unscaledDeltaTime; yield return null; }
                if (f.Fighting) { Bail(3, "전투가 400초 안에 끝나지 않았습니다"); yield break; }
                FightMode = "win";
                yield return Wait(0.6f);
                if (mode == "lose") yield break;
                if (f.P.S.Event != null && f.P.S.Event.Phase != "choose") yield break;
            }
            else
            {
            yield return Screen_("fight", 1.2f);
            yield return Shot(type == "boss" ? (f.P.S.Floor == 0 ? "fight_boss" : "fight_boss2") : type == "elite" ? "fight_elite" : "fight");
            if (mode == "lose") { yield return Press("fight.lose", 0.6f); yield return Press("confirm.yes", 0.4f); }
            else yield return Press("fight.win", 0.1f);
            yield return Wait(0.45f);
            yield return Shot(mode == "lose" ? "banner_lose" : "banner_win");
            if (mode == "lose") yield break;
            if (f.P.S.Event != null && f.P.S.Event.Phase != "choose") yield break;   // 이벤트 싸움 — 결과로 돌아간다
            }
            yield return Screen_("reward", 0.75f);
            if (f.Stage.Current != "reward") yield break;
            // 골드는 저절로 — 화면이 서면 이미 받았고 줄은 「받음」, 누를 칸이 아니다
            var rw = f.P.S.Reward; int gOpen = f.P.S.Gold;
            yield return Shot("reward_goldfly");
            Expect(rw == null || (rw.GoldTaken && Hot("reward.gold") != null && !Hot("reward.gold").Interactable), $"보상 — 골드 {rw?.Gold} 저절로 받음 · 누를 칸 아님");
            yield return Wait(1.0f);
            yield return Shot("reward");
            if (Hot("reward.equip") != null && Hot("reward.equip").Interactable)
            {
                yield return Press("reward.equip", 0.8f);
                yield return Gear("gear_reward");
            }
            for (int i = 0; i < 4 && Hot("reward.glow") != null; i++)
            {
                yield return Shot("reward_glow");
                yield return Press("reward.glow", 0.9f);
                yield return Until(() => OracleReveal.Phase == "choose", 5); yield return Wait(0.3f);
                yield return Shot("reward_glow_pick");
                yield return Press("glow.opt0", 0.1f);
                yield return Until(() => OracleReveal.Phase == "reborn", 4); yield return Wait(0.25f);
                yield return Shot("reward_glow_reborn");
                yield return Until(() => !OracleReveal.IsOpen, 6);
                yield return Until(() => CardGain.Phase == "show" || !CardGain.IsOpen, 3);
                if (CardGain.IsOpen) { yield return Shot("cardgain_reward"); yield return Press("gain.ok", 0.1f); }
                yield return Until(() => !CardGain.IsOpen && f.Stage.ModalLayer.childCount == 0, 6); yield return Wait(0.5f);
            }
            yield return Shot("reward_taken");
            Expect(f.P.S.Gold >= gOpen && (rw == null || rw.GoldTaken), $"보상 — 다른 보상을 고른 뒤에도 골드 그대로 한 번({gOpen} → {f.P.S.Gold})");
            int gLeave = f.P.S.Gold;
            bool boss = f.P.IsBoss;
            yield return Press("reward.next", 0.5f);
            if (Hot("confirm.yes") != null) yield return Press("confirm.yes", 0.4f);
            Expect(f.P.S.Gold == gLeave, $"보상 — 떠날 때 골드를 또 받지 않는다({gLeave} → {f.P.S.Gold})");
            if (boss && !f.P.IsLastFloor)
            {
                yield return Screen_("bosscopy", 1.2f);
                if (f.Stage.Current == "bosscopy")
                {
                    yield return Shot("bosscopy");
                    yield return Press("copy0", 0.6f);
                    yield return Shot("bosscopy_pick");
                    yield return Press("copy.go", 0.5f);
                }
            }
        }

        IEnumerator Gear(string shot)
        {
            yield return Wait(0.6f);
            yield return Shot(shot);
            yield return Press("gear:1", 0.4f);
            yield return Shot(shot + "_pick");
            var cmp = Hot("gear.cmp");
            if (cmp != null && !seen.Contains("gear_compare"))
            {
                yield return cmp.LongPress();
                yield return Wait(0.6f);
                yield return Shot("gear_compare");
                yield return Press("compare.close", 0.4f);
            }
            yield return Press("gear.ok", 0.8f);
        }

        IEnumerator GoEvent()
        {
            yield return Wait(1.2f);
            if (f.Stage.Current != "event") yield break;
            yield return Screen_("event", 1.2f);
            yield return Shot("event");
            // 잠기지 않은 첫 선택지(떠나기가 아닌 것)
            int pick = 0;
            for (int i = 0; i < 6; i++) { var b = Hot("event.opt" + i); if (b != null && b.Interactable) { pick = i; break; } }
            yield return Press("event.opt" + pick, 0.6f);
            yield return Shot("event_pick");
            yield return Press("event.ok", 0.8f);
            if (f.Stage.Current == "fight" || f.Fighting) { yield return GoFight("win"); yield return Screen_("event", 1.2f); }
            for (int i = 0; i < 4; i++)
            {
                yield return Wait(0.5f);
                if (f.Stage.ModalLayer.childCount > 0 && Hot("gear.ok") != null) { yield return Gear("gear_event"); continue; }
                if (Hot("pending0") != null) { yield return Shot("event_pending"); yield return Press("pending0", 0.8f); continue; }
                break;
            }
            yield return Wait(0.6f);
            yield return Shot("event_result");
            yield return Press("event.leave", 0.4f);
        }

        IEnumerator GoCamp(string kind)
        {
            yield return Screen_("camp", 1.4f);
            yield return Shot(kind == "campshop" ? "camp_shop" : "camp");
            bool canTrain = Hot("camp.trainopen") != null && Hot("camp.trainopen").Interactable;
            bool canRest = Hot("camp.rest") != null && Hot("camp.rest").Interactable;
            if (canTrain && (seen.Contains("camp_rest") || !canRest))
            {
                yield return Press("camp.trainopen", 0.9f);
                yield return Until(() => OracleReveal.Phase == "choose", 5); yield return Wait(0.3f);
                yield return Shot("camp_train");
                yield return Press("camp.train0", 0.1f);
                yield return Until(() => !OracleReveal.IsOpen, 6); yield return Wait(0.6f);
            }
            else if (canRest) { yield return Press("camp.rest", 1.2f); yield return Shot("camp_rest"); }
            Expect(Hot("camp.gear") == null && Hot("gear") == null, "휴식 — 장비 칸 · 장비 단추 없음");
            if (kind == "campshop")
            {
                yield return Press("camp.shop");
                yield return Screen_("shop", 1.4f);
                if (!seen.Contains("shop"))
                {
                    yield return Shot("shop");
                    // 살 수 있는 것 하나(장비면 끼기 창)
                    for (int i = 0; i < 8; i++)
                    {
                        var b = Hot("shop.item" + i);
                        if (b == null || !b.Interactable) continue;
                        yield return Press("shop.item" + i, 0.9f);
                        if (Hot("gear.ok") != null && f.Stage.ModalLayer.childCount > 0) yield return Gear("gear_shop");
                        break;
                    }
                    yield return Wait(0.8f);
                    yield return Shot("shop_bought");
                    if (Hot("shop.remove") != null && Hot("shop.remove").Interactable)
                    {
                        yield return Press("shop.remove", 0.9f);
                        yield return Shot("shop_remove");
                        yield return Press("remove0", 0.5f);
                        yield return Press("confirm.yes", 0.9f);
                    }
                }
                yield return Press("shop.back");
                yield return Screen_("camp", 1f);
            }
            yield return Press("camp.leave");
        }
    }
}
