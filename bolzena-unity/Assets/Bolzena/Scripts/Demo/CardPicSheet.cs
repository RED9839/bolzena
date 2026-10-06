using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bolzena.Battle;
using Bolzena.UI;
using Bolzena.View;
using UnityEngine;

namespace Bolzena.Demo
{
    // 원작 그림 카드 전후 시트(-battle -cardpicsheet, 2026-10-07 「몇몇 카드가 카드 이미지가 너무 붕 뜬 것도 있음」) — 그림 표(runui cardart.json)의
    //   원작 그림 카드(pics) · 스킬 아이콘 카드(cards)를 실제 전투 확대 카드(CardView — 효과 글 · 핀)로 한 쪽에 24장씩(2배로 찍는다 — 카드 그림 창 ≈ 500px) 찍는다.
    //   -oldpicfit 이면 예전 자리(한 장 굽기 · 가운데 아이콘 판). -picset pics|icons(없으면 둘 다) · -cards a,b,c 로 고른다. 소리 0.
    //   안내선: 하늘 = 위 글 · 칩 끝(26%). [PicSheet] 줄 = 카드마다 장식 선 자리 · 그림 내용 자리(창 몫) — Tools/cardpic_fit_report.py 가 모은다.
    public class CardPicSheet : MonoBehaviour
    {
        BattleDirector d;

        public static void Attach(BattleDirector director)
        {
            var s = director.gameObject.AddComponent<CardPicSheet>();
            s.d = director;
        }

        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        IEnumerator Start()
        {
            AudioListener.volume = 0f;
            float t = 0;
            while (!d.WaitingInput && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.0f);
            AudioListener.volume = 0f;
            var data = Look.Data;
            if (data == null) { Debug.LogError("[PicSheet] 데이터 없음"); Application.Quit(2); yield break; }
            foreach (var r in new[] { d.FieldRoot, d.ScreenRoot, d.UiRoot }) if (r != null) r.gameObject.SetActive(false);
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.enabled = false;
            string dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            Directory.CreateDirectory(dir);
            string tag = Bolzena.RunUI.CardArt.OldFit ? "before" : "after";
            var text = new Bolzena.Core.CardText(data);
            if (!Bolzena.RunUI.Roster.All.Any(h => h.CoreId != null)) Bolzena.RunUI.Roster.Link(data);
            string set = Arg("-picset");
            var ids = new List<(string id, string set)>();
            if (Arg("-cards") is string list) ids.AddRange(list.Split(',').Select(x => (x, "pick")));
            else
            {
                if (set != "icons") ids.AddRange(Bolzena.RunUI.CardArt.PicCards().OrderBy(x => x, StringComparer.Ordinal).Select(x => (x, "pics")));
                if (set != "pics") ids.AddRange(Bolzena.RunUI.CardArt.IconCards().OrderBy(x => x, StringComparer.Ordinal).Select(x => (x, "icons")));
            }
            var cards = new List<(string id, string set, Bolzena.Core.CardView cv)>();
            foreach (var (id, s) in ids)
            {
                Bolzena.Core.CardView cv = null;
                try { cv = data.View(id); } catch { }
                if (cv == null || cv.Def?.Hero == null || data.Hero(cv.Def.Hero) == null) { Debug.Log($"[PicSheet] 건너뜀 {id}"); continue; }
                cards.Add((id, s, cv));
            }
            Debug.Log($"[PicSheet] {tag} 카드 {cards.Count}장(표 {ids.Count})");
            var cam = d.Cam;
            float camH = cam.orthographicSize * 2, camW = camH * cam.aspect;
            const int cols = 8, rows = 3, per = cols * rows;
            float lab = 0.36f, gap = 0.12f;
            float sc = Mathf.Min((camW - 0.3f) / (cols * (CardView.W + gap)), (camH - 0.2f) / (rows * (CardView.H + lab + 0.05f)));
            float cw = CardView.W * sc, ch = CardView.H * sc;
            float x0 = cam.transform.position.x - camW / 2 + (camW - cols * (cw + gap * sc)) / 2 + gap * sc / 2;
            float y0 = cam.transform.position.y + camH / 2 - 0.1f;
            var oldHeroOf = CardView.HeroOf;
            var log = new System.Text.StringBuilder();
            for (int p = 0; p * per < cards.Count; p++)
            {
                var root = Make.Node("picsheet", null);
                Make.Box("bg", root, Res.UI("white"), new Vector3(cam.transform.position.x, cam.transform.position.y, 0), new Vector2(camW + 2, camH + 2), 900, new Color(0.04f, 0.05f, 0.09f));
                var page = cards.Skip(p * per).Take(per).ToList();
                var fakes = new List<HeroState>();
                CardView.HeroOf = i => i >= 0 && i < fakes.Count ? fakes[i] : null;
                var views = new List<(CardView v, string id, string set, string art)>();
                for (int i = 0; i < page.Count; i++)
                {
                    var (id, s, cv) = page[i];
                    var hd = data.Hero(cv.Def.Hero);
                    var look = Look.Hero(cv.Def.Hero);
                    fakes.Add(new HeroState { Key = look.Art, Id = cv.Def.Hero, Name = hd.Name, Tint = look.Tint, Nature = hd.Nature });
                    string type = cv.Type;
                    var info = new CardInfo
                    {
                        Id = id, Name = cv.Name, Hero = i, HeroName = hd.Name, Cost = cv.Cost, TypeName = type,
                        Type = type == "공격" ? CardType.Attack : type == "강화" ? CardType.Power : type == "스킬" ? CardType.Skill : CardType.Status,
                        Text = text.Card(cv), Art = Look.CardArt(cv.Def.Hero, Bolzena.Core.GameData.BaseId(id), cv.Unique, cv.Type),
                        Tags = cv.Tags.ToList(), Unique = cv.Unique, Nature = hd.Nature,
                    };
                    int r = i / cols, c = i % cols;
                    float px = x0 + c * (cw + gap * sc) + cw / 2, py = y0 - r * (ch + (lab + 0.05f) * sc) - ch / 2;
                    var v = CardView.Create(root, info);
                    v.ShowDesc = true; v.ShowPin = true;
                    v.TargetPos = new Vector3(px, py, 0);
                    v.TargetScale = sc; v.TargetRot = 0;
                    v.SetOrder(1000 + i * 20);
                    v.Snap();
                    const float aw = CardView.W - 0.07f, ah = CardView.H - 0.07f;
                    Make.Box("guide", v.transform, Res.UI("white"), new Vector3(0, ah / 2 - 0.26f * ah, 0), new Vector2(aw, 0.012f), 1000 + i * 20 + 9, new Color(0.45f, 0.8f, 1f, 0.85f));
                    Make.Text("label", root, $"{cv.Name}  <size=70%><color=#9aa3bd>{id}</color></size>", new Vector3(px, py - ch / 2 - lab * sc * 0.5f, 0), lab * sc * 0.62f, 990, new Color(0.93f, 0.94f, 1f), TMPro.TextAlignmentOptions.Center, cw + gap * sc);
                    views.Add((v, id, s, info.Art));
                }
                yield return null;
                yield return new WaitForSecondsRealtime(0.6f);
                foreach (var (v, id, s, art) in views)
                {
                    var r = v.ArtRect;
                    log.Append($"[PicSheet] battle\t{tag}\t{s}\t{id}\t{art}\t{v.ArtBottom:0.000}\t{r.x:0.000}\t{r.y:0.000}\t{r.width:0.000}\t{r.height:0.000}\t{(v.ArtCapped ? 1 : 0)}\n");
                }
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture(2);
                var path = Path.Combine(dir, $"picsheet_battle_{tag}_{p + 1:00}.jpg");
                File.WriteAllBytes(path, tex.EncodeToJPG(90));
                Destroy(tex);
                Debug.Log("[Capture] " + Path.GetFileName(path));
                Destroy(root.gameObject);
                yield return null;
            }
            Debug.Log(log.ToString());
            CardView.HeroOf = oldHeroOf;
            Debug.Log("[PicSheet] 끝");
            yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit(0);
        }
    }
}
