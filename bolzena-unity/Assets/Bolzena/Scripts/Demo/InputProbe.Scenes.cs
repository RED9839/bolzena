using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Battle;
using Bolzena.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using CoreFight = Bolzena.Core.Battle;
using CoreSetup = Bolzena.Core.BattleSetup;
using R = Bolzena.Core.R;
using RBtn = Bolzena.RunUI.Btn;
using RFlow = Bolzena.RunUI.Flow;

namespace Bolzena.Demo
{
    // 확인 장면(-inputprobe -probescene <이름>) — 화면 하나를 정해진 차례로 띄워 캡처 · 로그를 남기고 스스로 끈다(Tools/probe.sh scene <이름>).
    //   캡처 Captures/probe/<장면>/NN_설명.png · 로그 [Probe:<장면>] 한 줄씩. 다 맞으면 종료 코드 0, 틀린 것이 있으면 3(이유는 「틀림(!)」 줄).
    //   누르기는 가상 마우스(사람과 같은 길)로 하고, 안 먹으면 직접 부른 뒤 그렇다고 남긴다(「가상 마우스가 안 먹어」).
    //   rage       광폭화 — 시험 싸움에 판 싸움과 같은 칸(-rage)을 채우고 턴만 넘겨 5 · 6턴 HUD · 적 상세 창 · 예고 피해 전/후
    //   settings   로비 → 설정 → 화면 탭 맨 아래 「플레이 기록 보내기」 토글 아래 표시(PlayRecord.NotSentWhy)
    //   enemyinfo  전투에서 적을 눌러 상세 창(다음 행동 · 상태 · 패시브 · 광폭화 줄)
    //   equip      도감 → 아티팩트 → 횟수 제한(턴당 · 전투당) 장비 상세
    //   pack       N마리 일반전(-pack 편성 배율) — 배치 · 적마다 HP · 예고 피해를 배율 없는 같은 싸움과 견줌
    //   endturn    턴 종료 확인 창(카드 남기고 턴 종료 → 창 · 취소 · 턴 종료 · 설정 끄면 안 뜸) · HUD(더미 단추 · 파티 칩 「…」 · 단축키 배지 켬/끔)
    //   enemynav   적 정보 창 ◀ ▶ — 가상 마우스로 한 바퀴 · 뒤로 하나 · 바깥 누르면 닫힘
    //   dmg        피해 숫자 꼬리표 — 자동 전투로 약점 · 치명 숫자가 뜬 순간을 찍는다(성격 원)
    //   deck       덱 보기 정렬 — 새 판(-save probe_deck)을 열어 지도에서 덱 보기 → 사도별 · 비용 · 종류 · 이름
    public partial class InputProbe
    {
        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i < a.Length - 1 ? a[i + 1] : null; }
        static string SceneName => Arg("-probescene");

        string scene;
        int sceneFail, mouseMiss;
        bool hideBoxes;   // 판정 상자 · 그림 경계 선을 끈다(배치를 맨눈으로 볼 캡처)

        void Log(string s) => Debug.Log($"[Probe:{scene}] {s}");
        void Want(bool ok, string what) { if (!ok) sceneFail++; Debug.Log($"[Probe:{scene}] 확인 {(ok ? "맞음" : "틀림(!)")} — {what}"); }

        IEnumerator SceneRun()
        {
            scene = SceneName;
            dir = Path.Combine(dir, scene);
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir, "*.png")) File.Delete(f);   // 지난 실행의 캡처를 비운다(번호가 섞이지 않게)
            Log($"시작 — 화면 {Screen.width}×{Screen.height} → {dir}");
            IEnumerator body = scene switch
            {
                "rage" => RageScene(),
                "settings" => SettingsScene(),
                "enemyinfo" => EnemyInfoScene(),
                "equip" => EquipScene(),
                "pack" => PackScene(),
                "endturn" => EndTurnScene(),
                "enemynav" => EnemyNavScene(),
                "dmg" => DmgScene(),
                "deck" => DeckScene(),
                _ => null,
            };
            if (body == null) { Debug.LogError($"[Probe:{scene}] 모르는 장면 — rage · settings · enemyinfo · equip · pack · endturn · enemynav · dmg · deck"); Application.Quit(2); yield break; }
            yield return body;
            yield return new WaitForSecondsRealtime(0.6f);   // 마지막 캡처가 파일로 써질 때까지
            Log($"끝 — {(sceneFail == 0 ? "통과" : $"틀림 {sceneFail}")}{(mouseMiss > 0 ? $" · 가상 마우스가 안 먹어 직접 부른 것 {mouseMiss}" : "")} · 캡처 {shotNo}장");
            yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit(sceneFail == 0 ? 0 : 3);
        }

        // ── 전투 장면 도우미 ──

        IEnumerator WaitBattle()
        {
            float t = 0;
            while ((BattleDirector.I == null || !BattleDirector.I.WaitingInput) && t < 150f) { t += Time.unscaledDeltaTime; yield return null; }
            if (BattleDirector.I == null || !BattleDirector.I.WaitingInput) { Want(false, "전투가 입력 대기까지 열리지 않음"); yield break; }
            yield return new WaitForSecondsRealtime(0.8f);
        }

        CoreFight FightOf(BattleDirector d) => (d.Battle as CoreBattle)?.Fight;

        // 턴 종료 단추를 가상 마우스로 누르고 다음 입력 대기(또는 싸움 끝)까지
        IEnumerator EndTurn(BattleDirector d)
        {
            int t0 = FightOf(d)?.Turn ?? 0;
            yield return Click(d.Hud.EndPos);
            float w = 0;
            while (d.WaitingInput && w < 1.5f) { w += Time.unscaledDeltaTime; yield return null; }
            if (d.WaitingInput && (FightOf(d)?.Turn ?? 0) == t0) { mouseMiss++; Log("가상 마우스가 안 먹어 턴 종료를 직접 부름"); d.RequestEnd(); }
            w = 0;
            while (!d.WaitingInput && !d.Over && w < 40f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.9f);   // 턴 시작 연출 · 예고 갱신이 가라앉게
        }

        // 적 하나를 가상 마우스로 눌러 상세 창을 연다. 열린 창의 글을 돌려준다(못 열면 null)
        string panelText;
        IEnumerator OpenEnemy(BattleDirector d, int i)
        {
            panelText = null;
            if (Modal.Open != null) Modal.Open.Close();
            yield return Frames(4);
            yield return Click(ArtBounds(d.Enemies[i]).center);
            float w = 0;
            while (Modal.Open == null && w < 1f) { w += Time.unscaledDeltaTime; yield return null; }
            if (Modal.Open == null) { mouseMiss++; Log($"가상 마우스가 안 먹어 적 {i} 상세 창을 직접 엶"); d.OpenEnemyInfo(i); }
            yield return new WaitForSecondsRealtime(0.6f);
            if (Modal.Open != null)
                panelText = string.Join(" / ", Modal.Open.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => Plain(t.text)).Where(s => s.Length > 0));
        }

        IEnumerator CloseModal()
        {
            if (Modal.Open != null) Modal.Open.Close();
            yield return new WaitForSecondsRealtime(0.3f);
        }

        static string Plain(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]*>", "").Replace("\n", " ").Trim();

        string HudRage(BattleDirector d)
        {
            var t = d.Hud.GetComponentsInChildren<TMPro.TMP_Text>(true).FirstOrDefault(x => x.name == "rage");
            return t != null ? Plain(t.text) : "(rage 글 없음)";
        }

        // 적 하나의 예고 피해 — 화면 값(Snapshot) · 규칙 값(IntentHit) · 광폭화를 뺀 같은 수의 값(RageTurn 을 잠깐 0 으로 — 읽기만, 바로 되돌린다)
        static (int shown, int? hit, int? bare) IntentOf(BattleDirector d, CoreFight b, int i)
        {
            var e = b.Enemies[i];
            int? hit = b.IntentHit(e);
            int keep = b.RageTurn;
            b.RageTurn = 0;
            int? bare = b.IntentHit(e);
            b.RageTurn = keep;
            return (d.Battle.Snapshot.Enemies[i].IntentValue, hit, bare);
        }

        // ── rage ──
        IEnumerator RageScene()
        {
            yield return WaitBattle();
            var d = BattleDirector.I;
            var b = FightOf(d);
            if (b == null || sceneFail > 0) { Want(false, "코어 전투 없음"); yield break; }
            Want(b.RageTurn > 0, $"광폭화 턴 {b.RageTurn}(일반 {R.RAGE_TURN_FIGHT}) — -rage 로 BattleSetup.Rage 를 채움");
            var prev = new Dictionary<int, int>();
            bool seenBefore = false, seenOn = false;
            while (!d.Over && b.Turn <= R.RAGE_TURN_FIGHT + 1)
            {
                int turn = b.Turn;
                string hud = HudRage(d);
                Log($"{turn}턴 — 규칙 배율 ×{b.RageNow:0.###} · HUD 「{hud}」 · 파티 HP {d.Battle.Snapshot.PartyHp}/{d.Battle.Snapshot.PartyMaxHp}");
                for (int i = 0; i < b.Enemies.Count; i++)
                {
                    if (b.Enemies[i].Dead) continue;
                    var (shown, hit, bare) = IntentOf(d, b, i);
                    var es = d.Battle.Snapshot.Enemies[i];
                    string was = prev.TryGetValue(i, out var pv) ? $"전 턴 {pv} → 이번 턴 {shown}" : $"이번 턴 {shown}";
                    Log($"{turn}턴 적 {i} {es.Name} 「{Plain(es.IntentText)}」 예고 {was} · 규칙 {hit?.ToString() ?? "-"} · 광폭화 뺀 같은 수 {bare?.ToString() ?? "-"}{(hit > 0 && bare > 0 ? $" (×{(double)hit / bare:0.###})" : "")}");
                    prev[i] = shown;
                    if (turn == R.RAGE_TURN_FIGHT + 1 && hit >= 10 && bare > 0)
                        Want(Math.Abs((double)hit / bare.Value - R.RAGE_BASE) < 0.06, $"{turn}턴 적 {i} 예고 피해 {bare} → {hit} 이 ×{R.RAGE_BASE} 언저리");
                }
                if (turn == R.RAGE_TURN_FIGHT)
                {
                    seenBefore = true;
                    Want(b.RageNow == 1 && hud.Contains("광폭화까지") && hud.Contains("1"), $"{turn}턴 HUD 「{hud}」 = 광폭화까지 1턴 · 배율 ×1");
                    Shot($"turn{turn}_hud");
                    yield return Frames(3);
                    yield return OpenEnemy(d, d.FirstAliveEnemy());
                    Log($"{turn}턴 적 상세 창 — {panelText}");
                    Want(panelText != null && panelText.Contains("1턴 뒤 광폭화"), $"{turn}턴 적 상세 창에 「1턴 뒤 광폭화」");
                    Shot($"turn{turn}_enemyinfo");
                    yield return Frames(3);
                    yield return CloseModal();
                }
                if (turn == R.RAGE_TURN_FIGHT + 1)
                {
                    seenOn = true;
                    Want(Math.Abs(b.RageNow - R.RAGE_BASE) < 1e-9 && hud.Contains($"×{R.RAGE_BASE:0.##}"), $"{turn}턴 HUD 「{hud}」 · 규칙 ×{b.RageNow:0.###} = 광폭화 ×{R.RAGE_BASE}");
                    Shot($"turn{turn}_hud");
                    yield return Frames(3);
                    yield return OpenEnemy(d, d.FirstAliveEnemy());
                    Log($"{turn}턴 적 상세 창 — {panelText}");
                    Want(panelText != null && panelText.Contains($"주는 피해 ×{R.RAGE_BASE:0.##}"), $"{turn}턴 적 상세 창에 「광폭화 — 주는 피해 ×{R.RAGE_BASE}」");
                    Shot($"turn{turn}_enemyinfo");
                    yield return Frames(3);
                    yield return CloseModal();
                    break;
                }
                yield return EndTurn(d);
            }
            Want(seenBefore && seenOn, $"5 · 6턴까지 버팀(싸움 {(d.Over ? "끝남(!)" : "이어짐")} · 지금 {b.Turn}턴)");
        }

        // ── enemyinfo ──
        IEnumerator EnemyInfoScene()
        {
            yield return WaitBattle();
            var d = BattleDirector.I;
            if (d == null || sceneFail > 0) yield break;
            for (int round = 0; round < 2 && !d.Over; round++)
            {
                for (int i = 0; i < d.Enemies.Count; i++)
                {
                    if (d.Battle.Snapshot.Enemies[i].Dead) continue;
                    var es = d.Battle.Snapshot.Enemies[i];
                    yield return OpenEnemy(d, i);
                    Want(panelText != null, $"{FightOf(d)?.Turn}턴 적 {i} {es.Name} 상세 창 열림");
                    Log($"{FightOf(d)?.Turn}턴 적 {i} {es.Name} — 예고 「{Plain(es.IntentText)}」 · 상태 {es.Chips.Count} · 패시브 {es.Passives.Count} · 창 글: {panelText}");
                    if (panelText != null && FightOf(d)?.RageTurn > 0) Want(panelText.Contains("광폭화"), $"적 {i} 상세 창에 광폭화 줄");
                    Shot($"turn{FightOf(d)?.Turn}_enemy{i}");
                    yield return Frames(3);
                    yield return CloseModal();
                }
                if (round == 0) yield return EndTurn(d);   // 한 턴 넘겨 상태 · 바뀐 예고도 본다
            }
        }

        // ── pack ──
        IEnumerator PackScene()
        {
            yield return WaitBattle();
            var d = BattleDirector.I;
            var cb = d != null ? d.Battle as CoreBattle : null;
            var b = cb?.Fight;
            if (b == null || sceneFail > 0) { Want(false, "코어 전투 없음"); yield break; }
            int n = cb.Fx.Waves[0].Count;
            double want = R.PackX(n, false) ?? 1;
            Log($"{n}마리 — 편성 배율 R.PackX({n}, 일반) = PACK_SLOT[{n}] {(R.PACK_SLOT.TryGetValue(n, out var sl) ? sl : 0):0.##} × PACK_FIGHT {R.PACK_FIGHT} = {want:0.###} · 실제 칸 {cb.Fx.FoePack?.ToString("0.###") ?? "없음(-pack 빠짐)"}");
            Want(cb.Fx.FoePack != null, "-pack 으로 BattleSetup.FoePack 을 채움");
            // 견줄 싸움 — 같은 차림 · 같은 씨앗으로 편성 배율만 뺀 것(규칙만 열어 값을 읽고 버린다)
            var fx = cb.Fx;
            var bare = CoreFight.Start(cb.Data, new CoreSetup { Party = fx.Party.ToList(), Deck = fx.Deck.ToList(), Enemies = fx.Waves[0].ToList(), Seed = fx.Seed, EnemyNature = fx.EnemyNature, Rage = fx.Rage });
            for (int i = 0; i < b.Enemies.Count && i < bare.Enemies.Count; i++)
            {
                var e = b.Enemies[i]; var e0 = bare.Enemies[i];
                var es = d.Battle.Snapshot.Enemies[i];
                int? hit = b.IntentHit(e), hit0 = bare.IntentHit(e0);
                double hx = (double)e.MaxHp / Math.Max(1, e0.MaxHp);
                Log($"적 {i} {es.Name}({e.Key}) HP {es.Hp}/{es.MaxHp} — 배율 없이 {e0.MaxHp} (×{hx:0.###}) · 예고 「{Plain(es.IntentText)}」 {es.IntentValue} — 배율 없이 {hit0?.ToString() ?? "-"}{(hit > 0 && hit0 > 0 ? $" (×{(double)hit / hit0:0.###})" : "")}");
                Want(Math.Abs(hx - want) < 0.02 + 1.0 / Math.Max(1, e0.MaxHp), $"적 {i} HP 배율 ×{hx:0.###} ≈ ×{want:0.###}");
                if (hit >= 10 && hit0 > 0 && e.Intent?.T == e0.Intent?.T) Want(Math.Abs((double)hit / hit0.Value - want) < 0.08, $"적 {i} 예고 피해 배율 ×{(double)hit / hit0.Value:0.###} ≈ ×{want:0.###}");
            }
            // 배치 — 판정 상자끼리 · 그림 경계끼리 겹친 넓이(작은 쪽의 몇 %)
            for (int i = 0; i < d.Enemies.Count; i++)
                for (int j = i + 1; j < d.Enemies.Count; j++)
                {
                    float bo = Overlap(d.EnemyBox(i), d.EnemyBox(j));
                    var ai = ArtBounds(d.Enemies[i]); var aj = ArtBounds(d.Enemies[j]);
                    float ao = Overlap(new Rect(ai.min, ai.size), new Rect(aj.min, aj.size));
                    Log($"배치 적 {i} {Fmt(d.Enemies[i].transform.position)} · 적 {j} {Fmt(d.Enemies[j].transform.position)} — 판정 상자 겹침 {bo:P0} · 그림 경계 겹침 {ao:P0}");
                    Want(ao < 0.05f, $"적 {i} · {j} 그림이 겹치지 않음({ao:P0}) — 판정 상자는 그림보다 넓게 잡아 둘러선 줄끼리 겹칠 수 있어 남기기만");
                }
            Shot($"pack{n}_boxes");
            yield return Frames(3);
            hideBoxes = true;
            yield return Frames(2);
            Shot($"pack{n}");
            yield return Frames(3);
        }

        static float Overlap(Rect a, Rect b)
        {
            float w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin), h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            if (w <= 0 || h <= 0) return 0;
            return w * h / Mathf.Max(1e-4f, Mathf.Min(a.width * a.height, b.width * b.height));
        }

        // ── 판 화면(로비) 장면 도우미 ──

        static RFlow F => RFlow.Me;
        static RBtn Hot(string k) => F != null && F.Stage.Hot.TryGetValue(k, out var b) && b != null && b.isActiveAndEnabled ? b : null;

        IEnumerator WaitScreen(string name, float timeout = 60f)
        {
            float t = 0;
            while ((F == null || F.Stage.Current != name || F.Stage.Busy) && t < timeout) { t += Time.unscaledDeltaTime; yield return null; }
            Want(F != null && F.Stage.Current == name, $"화면 {name} 열림(지금 {F?.Stage.Current ?? "-"})");
            yield return new WaitForSecondsRealtime(1.0f);
        }

        static Camera CamOf(RectTransform rt)
        {
            var canvas = rt.GetComponentInParent<Canvas>().rootCanvas;
            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
        static Vector2 UiScreen(RectTransform rt) => RectTransformUtility.WorldToScreenPoint(CamOf(rt), rt.TransformPoint(rt.rect.center));
        static bool InView(RectTransform view, Vector2 p) => RectTransformUtility.RectangleContainsScreenPoint(view, p, CamOf(view));

        // 판 화면 단추를 가상 마우스로 누른다 — done 이 1.5초 안에 참이 안 되면 단추를 직접 누른다(그렇다고 남긴다)
        IEnumerator PressHot(string key, Func<bool> done)
        {
            var b = Hot(key);
            if (b == null) { Want(false, $"단추 {key} 없음"); yield break; }
            var p = UiScreen((RectTransform)b.transform);
            Set(p, false); yield return Frames(10);
            Set(p, true); yield return Frames(4);
            Set(p, false); yield return Frames(4);
            float w = 0;
            while (!done() && w < 1.5f) { w += Time.unscaledDeltaTime; yield return null; }
            if (!done())
            {
                mouseMiss++;
                Log($"가상 마우스가 안 먹어 {key} 를 직접 누름(화면 {p.x:0},{p.y:0})");
                b = Hot(key);
                if (b != null) yield return b.Press();
                w = 0;
                while (!done() && w < 2f) { w += Time.unscaledDeltaTime; yield return null; }
            }
            Set(new Vector2(4, 4), false);   // 커서를 구석으로(올림 강조가 캡처에 남지 않게)
            yield return new WaitForSecondsRealtime(0.6f);
        }

        // 로비에 먼저 떠 있는 창(업데이트 내용 · 안내)이 있으면 닫는다
        IEnumerator ClearModals()
        {
            for (int k = 0; k < 3 && F.Stage.ModalLayer.childCount > 0; k++)
            {
                Log($"로비 위에 떠 있는 창 {F.Stage.ModalLayer.GetChild(0).name} — 닫음");
                var x = Hot("modal.x");
                if (x != null) yield return x.Press(); else break;
                yield return new WaitForSecondsRealtime(0.6f);
            }
        }

        // ── settings ──
        IEnumerator SettingsScene()
        {
            yield return WaitScreen("lobby");
            if (sceneFail > 0) yield break;
            yield return ClearModals();
            UnityEngine.UI.ScrollRect Sr() => F.Stage.ModalLayer.GetComponentInChildren<UnityEngine.UI.ScrollRect>();
            yield return PressHot("settings", () => Sr() != null);
            if (Sr() == null) { Want(false, "설정 창이 열리지 않음"); yield break; }
            TMPro.TMP_Text RecTitle() => Sr().content.GetComponentsInChildren<TMPro.TMP_Text>().FirstOrDefault(t => t.text.Contains("플레이 기록 보내기"));
            yield return PressHot("settings.tab1", () => RecTitle() != null);
            var sr = Sr();
            var title = RecTitle();
            if (title == null) { Want(false, "화면 탭에 「플레이 기록 보내기」 줄 없음"); yield break; }
            Shot("settings_tab1_top");
            yield return Frames(3);
            // 끝까지 내리기 — 가상 마우스 휠(보이는 칸 가운데). 안 내려가면 스크롤 칸을 직접 맨 아래로
            var vp = sr.viewport != null ? sr.viewport : (RectTransform)sr.transform;
            var at = UiScreen(vp);
            for (int k = 0; k < 20 && sr.verticalNormalizedPosition > 0.001f; k++)
            {
                mouse.MakeCurrent();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = at, scroll = new Vector2(0, -120) }); yield return Frames(2);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = at }); yield return Frames(2);
            }
            if (sr.verticalNormalizedPosition > 0.001f) { mouseMiss++; Log($"가상 마우스 휠이 안 먹어 스크롤을 직접 맨 아래로(지금 {sr.verticalNormalizedPosition:0.00})"); sr.verticalNormalizedPosition = 0; }
            yield return new WaitForSecondsRealtime(0.5f);
            // 토글 줄 — 이름 다음 글이 설명(SetToggle). 보이는 칸 안에 들었나
            var texts = sr.content.GetComponentsInChildren<TMPro.TMP_Text>().ToList();
            int ti = texts.IndexOf(title);
            string desc = ti >= 0 && ti + 1 < texts.Count ? Plain(texts[ti + 1].text) : null;
            var why = Bolzena.RunUI.PlayRecord.NotSentWhy;
            Log($"토글 「{Plain(title.text)}」 · 아래 표시 「{desc}」 · PlayRecord.NotSentWhy 「{why ?? "null(보내는 실행)"}」 · 켬 {Bolzena.RunUI.PlayRecord.On} · 스크롤 {sr.verticalNormalizedPosition:0.00}(내용 {sr.content.rect.height:0} · 보이는 {vp.rect.height:0})");
            Want(why != null && desc == why, "토글 아래 표시가 보내지 않는 까닭과 같음");
            var tp = UiScreen(title.rectTransform);
            Want(InView(vp, tp), "맨 아래로 내린 뒤 토글 줄이 보이는 칸 안");
            Shot("settings_tab1_bottom");
            yield return Frames(3);
            // 전투 연출 탭 — 「턴 종료 확인」 · 「단축키 표시」 토글(2026-11-01)
            yield return PressHot("settings.tab2", () => Hot("set.showkeys") != null);
            Want(Hot("set.endconfirm") != null && Hot("set.showkeys") != null, "전투 연출 탭에 「턴 종료 확인」 · 「단축키 표시」 토글");
            Shot("settings_tab2");
            yield return Frames(3);
            yield return PressHot("settings.close", () => Sr() == null);
        }

        // ── equip ──
        static readonly string[] LimitEquips = { "eq_healpendant", "eq_grail", "eq_furnace" };   // 치유의 펜던트(턴당 1회) · 기원의 성배(턴당 1회) · 용광검(전투당 3회 · 2회)

        IEnumerator EquipScene()
        {
            yield return WaitScreen("lobby");
            if (sceneFail > 0) yield break;
            yield return ClearModals();
            yield return PressHot("dex", () => F.Stage.Current == "dex");
            yield return WaitScreen("dex", 10f);
            yield return PressHot("tab:아티팩트", () => Hot("dexequip:" + LimitEquips[0]) != null);
            Shot("dex_equips");
            yield return Frames(3);
            var ids = (Arg("-equips")?.Split(',') ?? LimitEquips).ToList();
            foreach (var id in ids)
            {
                var e = F.P.Data.Equip(id);
                if (e == null) { Want(false, $"장비 {id} 없음"); continue; }
                string rule = Plain(F.P.Text.Passives(e.Effect));
                int before = F.Stage.ModalLayer.childCount;
                var b = Hot("dexequip:" + id);
                bool onScreen = false;
                if (b != null)
                {
                    var p = UiScreen((RectTransform)b.transform);
                    var sr = b.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                    onScreen = p.x > 0 && p.y > 0 && p.x < Screen.width && p.y < Screen.height && (sr == null || InView(sr.viewport != null ? sr.viewport : (RectTransform)sr.transform, p));
                }
                if (onScreen) yield return PressHot("dexequip:" + id, () => F.Stage.ModalLayer.childCount > before);
                else { Log($"{e.Name} 칸이 목록에 안 보여(스크롤 밖) 장비 상세를 직접 엶"); F.EquipDetail(id); yield return new WaitForSecondsRealtime(0.8f); }
                var shown = string.Join(" / ", F.Stage.ModalLayer.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => Plain(t.text)).Where(s => s.Contains("회)")));
                Log($"{e.Name}({id}) {e.Grade} — 규칙 글 「{rule}」 · 창에 보인 줄 「{shown}」");
                Want(shown.Contains("(턴당") || shown.Contains("(전투당"), $"{e.Name} 상세 창에 「(턴당 N회)」 · 「(전투당 N회)」");
                Shot("equip_" + id);
                yield return Frames(3);
                yield return PressHot("modal.x", () => F.Stage.ModalLayer.childCount <= before);
            }
        }

        // ── endturn ── 턴 종료 확인 창 · HUD 크기 · 단축키 배지(2026-11-01)
        Button ModalButton(string name) => Modal.Open == null ? null : Modal.Open.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
        static Vector3 BtnAt(Button b) { var ls = b.transform.lossyScale; return b.transform.position + (Vector3)(b.Offset * new Vector2(ls.x, ls.y)); }

        IEnumerator EndTurnScene()
        {
            bool keepConfirm = Bolzena.RunUI.Settings.EndTurnConfirm, keepKeys = Bolzena.RunUI.Settings.ShowKeys;
            Bolzena.RunUI.Settings.EndTurnConfirm = true;
            Bolzena.RunUI.Settings.ShowKeys = true;
            EndTurnButton.ForceAsk = true;   // 시험 실행이라 평소엔 묻지 않는다 — 이 장면만
            yield return WaitBattle();
            var d = BattleDirector.I;
            var b = FightOf(d);
            if (b == null || sceneFail > 0) { Want(false, "코어 전투 없음"); yield break; }
            // HUD — 더미 단추 · 파티 칩 · 단축키 배지
            var badges = d.Hud.GetComponentsInChildren<KeyBadge>(true).Select(k => k.name.Replace("key_", "")).ToList();
            Log($"HUD 단축키 배지 {badges.Count}개 — {string.Join(" ", badges)} · 보임 {KeyBadge.Show}");
            Want(new[] { "E", "A", "S", "D", "Q", "Tab", "Esc", "Z", "X", "C" }.All(badges.Contains), "HUD 배지 E A S D Q Tab Esc Z X C");
            var deckIcon = d.Hud.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "deck_i");
            if (deckIcon != null)
            {
                float px = Screen.height / (Camera.main.orthographicSize * 2) * 1280f / Screen.width;   // 1280 기준 화소
                var sz = deckIcon.bounds.size;
                Log($"뽑을 더미 그림 상자 1280 기준 {sz.x * px:0}×{sz.y * px:0}px — 카드 꼴은 약 55% × 82% = {sz.x * px * 0.55f:0}×{sz.y * px * 0.82f:0}(원작 단추 58×70)");
            }
            Shot("hud_keys_on");
            yield return Frames(3);
            Bolzena.RunUI.Settings.ShowKeys = false;
            yield return Frames(3);
            Want(!KeyBadge.Show && d.Hud.GetComponentsInChildren<KeyBadge>(true).All(k => !k.transform.GetChild(0).gameObject.activeSelf), "「단축키 표시」 끄면 배지가 숨음");
            Shot("hud_keys_off");
            yield return Frames(3);
            Bolzena.RunUI.Settings.ShowKeys = true;
            // 파티 칩 「…」 — 시험 칩 13개를 잠깐 넣어 넘침 모양을 본다(다음 갱신에 제 값으로 돌아간다)
            var fake = new List<StatusChip>();
            string[] ids = { "사기", "불굴", "피해 감소", "회피", "결정화", "반격", "초재생", "협공", "근면", "결의", "집중", "공명", "끈기" };
            foreach (var id in ids) fake.Add(new StatusChip { Id = id, Value = "2", Turns = 2, Kind = "buff", Text = "시험 칩" });
            d.Hud.Chips.Set(fake);
            yield return Frames(1);
            int shown = d.Hud.Chips.transform.Cast<Transform>().Count(t => t.name.StartsWith("chip_"));
            bool more = d.Hud.Chips.transform.Find("more") != null;
            Log($"파티 칩 시험 13개 — 칩 {shown}개 · 「…」 {(more ? "있음" : "없음")} · 줄 폭 {d.Hud.Chips.Width:0.00}");
            Want(shown == 10 && more, "파티 칩 10칸 + 「…」");
            Shot("hud_chips_more");
            yield return Frames(3);
            // 카드를 남기고 턴 종료 → 확인 창
            int t0 = b.Turn;
            int playable = 0; for (int i = 0; i < d.Hand.Cards.Count; i++) if (d.Battle.CanPlay(i, out _)) playable++;
            Log($"{t0}턴 — 손패 {d.Hand.Cards.Count} · 낼 수 있는 카드 {playable} · 확인 필요 {EndTurnButton.NeedAsk}");
            yield return Click(d.Hud.EndPos);
            yield return new WaitForSecondsRealtime(0.5f);
            Want(Modal.Open != null && Modal.Open.name == "Modal_endask", $"턴 종료 → 확인 창(지금 창 {(Modal.Open != null ? Modal.Open.name : "없음")})");
            if (Modal.Open != null) Log("확인 창 글 — " + string.Join(" / ", Modal.Open.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => Plain(t.text)).Where(x => x.Length > 0)));
            Shot("endask");
            yield return Frames(3);
            var no = ModalButton("no");
            if (no != null) yield return Click(BtnAt(no));
            yield return new WaitForSecondsRealtime(0.5f);
            Want(Modal.Open == null && b.Turn == t0 && d.WaitingInput, $"취소 — 창 닫힘 · 턴 그대로({b.Turn})");
            // 다시 단추 → 이번엔 「턴 종료」(E 키는 가상 입력을 PointerInput.Key 가 읽지 않아 단추로)
            yield return Click(d.Hud.EndPos);
            yield return new WaitForSecondsRealtime(0.5f);
            Shot("endask_again");
            yield return Frames(3);
            var yes = ModalButton("yes");
            if (yes != null) yield return Click(BtnAt(yes));
            else if (Modal.Open != null && Modal.Open.name == "Modal_endask") { mouseMiss++; Log("「턴 종료」 단추를 못 찾아 직접 부름"); EndTurnButton.YesFn?.Invoke(); }
            float w = 0;
            while ((!d.WaitingInput || b.Turn == t0) && !d.Over && w < 40f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.9f);
            Want(b.Turn == t0 + 1 || d.Over, $"턴 종료 — {t0} → {b.Turn}턴");
            // 설정 「턴 종료 확인」 을 끄면 창 없이 바로
            Bolzena.RunUI.Settings.EndTurnConfirm = false;
            int t1 = b.Turn;
            if (!d.Over)
            {
                yield return Click(d.Hud.EndPos);
                yield return new WaitForSecondsRealtime(0.4f);
                Want(Modal.Open == null || Modal.Open.name != "Modal_endask", "설정을 끄면 창 없이 턴 종료");
                w = 0;
                while ((!d.WaitingInput || b.Turn == t1) && !d.Over && w < 40f) { w += Time.unscaledDeltaTime; yield return null; }
                Want(b.Turn == t1 + 1 || d.Over, $"창 없이 턴 종료 — {t1} → {b.Turn}턴");
            }
            Bolzena.RunUI.Settings.EndTurnConfirm = keepConfirm;
            Bolzena.RunUI.Settings.ShowKeys = keepKeys;
            EndTurnButton.ForceAsk = false;
        }

        // ── enemynav ── 적 정보 창 ◀ ▶
        string OpenName() => Modal.Open == null ? null : Modal.Open.GetComponentsInChildren<TMPro.TMP_Text>().Select(t => Plain(t.text)).FirstOrDefault(x => x.Length > 0);

        IEnumerator EnemyNavScene()
        {
            yield return WaitBattle();
            var d = BattleDirector.I;
            if (d == null || sceneFail > 0) yield break;
            var es = d.Battle.Snapshot.Enemies;
            int n = es.Count(e => !e.Dead);
            Log($"적 {es.Count}(살아 있음 {n}) — {string.Join(" · ", es.Select(e => e.Name))}");
            yield return OpenEnemy(d, 0);
            Want(Modal.Open != null, "적 0 정보 창 열림");
            Want(ModalButton("navr") != null && ModalButton("navl") != null, "창 안에 ◀ ▶ 단추");
            Shot("enemy0");
            yield return Frames(3);
            var names = new List<string> { OpenName() };
            for (int k = 0; k < n; k++)
            {
                var navR = ModalButton("navr");
                if (navR == null) break;
                yield return Click(BtnAt(navR));
                yield return new WaitForSecondsRealtime(0.5f);
                names.Add(OpenName());
                if (k == 0) { Shot("enemy_next"); yield return Frames(3); }
            }
            Log("▶ 로 넘긴 차례 — " + string.Join(" → ", names));
            Want(names.Count == n + 1 && names[0] == names[n] && names.Take(n).Distinct().Count() == n, $"▶ {n}번이면 한 바퀴");
            var navL = ModalButton("navl");
            if (navL != null) { yield return Click(BtnAt(navL)); yield return new WaitForSecondsRealtime(0.5f); }
            Want(n > 1 && OpenName() == names[n - 1], $"◀ 로 앞 적({OpenName()})");
            // 바깥(싸움터 오른쪽 위 빈 곳)을 누르면 닫힘
            yield return Click(new Vector3(Tone.HalfW * 0.5f, Tone.HalfH * 0.55f, 0));
            yield return new WaitForSecondsRealtime(0.4f);
            Want(Modal.Open == null, "바깥을 누르면 닫힘(그대로)");
        }

        // ── dmg ── 약점 · 치명 꼬리표
        IEnumerator DmgScene()
        {
            yield return WaitBattle();
            var d = BattleDirector.I;
            if (d == null || sceneFail > 0) yield break;
            var seen = new HashSet<Transform>();
            int weak = 0, crit = 0, both = 0, nat = 0, plain = 0;
            bool shotWeak = false, shotCrit = false, shotBoth = false;
            d.Hud.OnAuto?.Invoke();   // 자동 전투 — 카드를 저절로 낸다
            float t = 0;
            while (!d.Over && t < 150f && !(shotWeak && shotCrit && shotBoth))
            {
                t += Time.unscaledDeltaTime;
                if (Bolzena.View.Vfx.Field != null)
                    foreach (Transform node in Bolzena.View.Vfx.Field)
                    {
                        if (node.name != "dmg" || !seen.Add(node)) continue;
                        var tag = node.Find("tag");
                        string tt = tag != null ? Plain(tag.GetComponent<TMPro.TMP_Text>().text) : "";
                        bool w = tt.Contains("약점"), c = tt.Contains("치명");
                        bool hasNat = node.Find("nat") != null;
                        if (hasNat) nat++;
                        if (w && c) both++; else if (w) weak++; else if (c) crit++; else plain++;
                        string why = w && c && !shotBoth ? "dmg_weak_crit" : w && !c && !shotWeak ? "dmg_weak" : c && !w && !shotCrit ? "dmg_crit" : null;
                        if (why == null) continue;
                        if (why == "dmg_weak_crit") shotBoth = true; else if (why == "dmg_weak") shotWeak = true; else shotCrit = true;
                        var num = node.Find("n");
                        Log($"{why} — 「{tt}」 숫자 {(num != null ? Plain(num.GetComponent<TMPro.TMP_Text>().text) : "-")} · 성격 원 {(hasNat ? "있음" : "없음")}");
                        StartCoroutine(ShotLater(why, 0.12f));
                    }
                yield return null;
            }
            if (!d.Over) d.Hud.OnAuto?.Invoke();
            Log($"숫자 {seen.Count}개 — 약점만 {weak} · 치명만 {crit} · 둘 다 {both} · 꼬리표 없음 {plain} · 성격 원 {nat} · {t:0}초");
            Want(weak + both > 0, "약점 꼬리표가 뜬 숫자가 있음(-nature 냉정 — 란(순수)이 약점)");
            Want(nat > 0, "사도가 친 숫자에 성격 원");
            if (crit + both == 0) Log("이 판에서는 치명이 안 나옴 — 치명 꼬리표는 확인 못 함(틀림 아님)");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        IEnumerator ShotLater(string name, float after)
        {
            yield return new WaitForSecondsRealtime(after);
            Shot(name);
        }

        // ── deck ── 덱 보기 정렬(새 판은 -save probe_deck 에 — 사람의 판 저장을 건드리지 않는다)
        IEnumerator DeckScene()
        {
            yield return WaitScreen("lobby");
            if (sceneFail > 0) yield break;
            yield return ClearModals();
            // 모험 시작 → (마을 고르기면 무작위로 출발) → 마을 → 편성 → 지도. 같은 -save 에 판이 남아 있으면 바로 지도로 이어진다
            yield return PressHot("start", () => F.Stage.Current != "lobby" && !F.Stage.Busy);
            float tw = 0;
            while (F.Stage.Current != "map" && tw < 60f)
            {
                string cur = F.Stage.Current;
                if (F.Stage.Busy) { tw += Time.unscaledDeltaTime; yield return null; continue; }
                if (cur == "vilpick" && Hot("vilpick.go") != null) yield return PressHot("vilpick.go", () => F.Stage.Current != "vilpick");
                else if (cur == "village" && Hot("go") != null) yield return PressHot("go", () => F.Stage.Current != "village");
                else if (cur == "party")
                {
                    var slot = Hot("slot0"); if (slot != null) { yield return slot.Press(); yield return new WaitForSecondsRealtime(0.6f); }
                    foreach (var k in Bolzena.RunUI.Roster.All.Where(h => h.Playable).Take(3).Select(h => h.key).ToList())
                    {
                        var hb = Hot("hero:" + k);
                        if (hb != null) { yield return hb.Press(); yield return new WaitForSecondsRealtime(0.25f); }
                    }
                    var done = Hot("list.done"); if (done != null) { yield return done.Press(); yield return new WaitForSecondsRealtime(0.6f); }
                    yield return PressHot("party.go", () => F.Stage.Current != "party");
                }
                tw += Time.unscaledDeltaTime; yield return null;
            }
            yield return WaitScreen("map", 20f);
            if (F.Stage.Current != "map") yield break;
            F.DeckView();
            yield return new WaitForSecondsRealtime(1.6f);
            string[] names = { "사도별", "비용", "종류", "이름" };
            var P = F.P;
            for (int k = 0; k < names.Length; k++)
            {
                int target = k;
                if (k > 0) yield return PressHot("deck.sort" + k, () => RFlow.DeckSort == target);
                // 첫 묶음 — 화면 카드 차례(카드 상세로 넘기는 차례와 같은 SortIds 를 거친 것)를 기대 차례와 견준다
                var g0 = Bolzena.RunUI.CardOrder.Groups(P.S.Deck, P.Data, P.S.Party).FirstOrDefault();
                if (g0 != null)
                {
                    var vs = g0.Ids.Select((id, i) => (v: P.View(id), i)).ToList();
                    var want = (k == 1 ? vs.OrderBy(x => x.v.Cost) : k == 2 ? vs.OrderBy(x => x.v.Type == "공격" ? 0 : x.v.Type == "스킬" ? 1 : x.v.Type == "강화" ? 2 : 3) : k == 3 ? vs.OrderBy(x => x.v.Name, StringComparer.Ordinal) : vs.OrderBy(x => 0)).ThenBy(x => x.i).Select(x => x.v.Name).ToList();
                    Log($"정렬 「{names[k]}」 — 첫 묶음({g0.Title}) 기대 차례(캡처와 맨눈 대조): {string.Join(" · ", want)}");
                }
                Want(RFlow.DeckSort == k, $"정렬 「{names[k]}」 골라짐");
                Shot("deck_sort" + k);
                yield return Frames(3);
            }
            yield return PressHot("deck.sort0", () => RFlow.DeckSort == 0);
            Want(RFlow.DeckSort == 0, "다시 「사도별」(기본)");
        }
    }
}
