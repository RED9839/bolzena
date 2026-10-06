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
    // 전투 카드 그림 시트(-battle -cardartsheet, 2026-10-07 「사도들 위에 글자랑 공격 · 스킬 이 글자에 안 가리게」) — 135명 사도의 대표 카드(시작 카드 첫 장)를
    //   실제 전투 카드(CardView)로 두 모습씩 찍는다: 왼쪽 = 손패 모습(효과 글 · 핀 감춤), 오른쪽 = 확대 모습(효과 글 · 태그 · 얼굴 핀).
    //   크기만 다르고 꼴은 같다(카드 안 자리는 모두 카드 단위) — 한 쪽에 24명씩. -oldcardcrop 이면 예전 자르기(frac 0.56).
    //   확대 모습의 안내선: 하늘 = 위 글 · 칩 끝(26%) · 주황 = 아래 효과 판 위 끝(61%) · 분홍 = 표의 얼굴 상자.
    public class CardArtSheet : MonoBehaviour
    {
        BattleDirector d;

        public static void Attach(BattleDirector director)
        {
            var s = director.gameObject.AddComponent<CardArtSheet>();
            s.d = director;
        }

        static string Arg(string n) { var a = Environment.GetCommandLineArgs(); int i = Array.IndexOf(a, n); return i >= 0 && i + 1 < a.Length ? a[i + 1] : null; }

        IEnumerator Start()
        {

            float t = 0;
            while (!d.WaitingInput && t < 60f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1.0f);
            var data = Look.Data;
            if (data == null) { Debug.LogError("[CardArtSheet] 데이터 없음"); Application.Quit(2); yield break; }
            // 전투 화면을 감춘다(카메라만 쓴다)
            foreach (var r in new[] { d.FieldRoot, d.ScreenRoot, d.UiRoot }) if (r != null) r.gameObject.SetActive(false);
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.enabled = false;
            string dir = Arg("-captures") ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Captures"));
            Directory.CreateDirectory(dir);
            string tag = Bolzena.RunUI.StandingSnap.OldCardCrop ? "before" : "after";
            var text = new Bolzena.Core.CardText(data);
            if (!Bolzena.RunUI.Roster.All.Any(h => h.CoreId != null)) Bolzena.RunUI.Roster.Link(data);   // 전투만 띄우면 명부가 코어와 이어지지 않았다
            var heroes = Bolzena.RunUI.Roster.All.Where(h => h.art != null && h.CoreId != null && data.Hero(h.CoreId) != null)
                .OrderBy(h => h.art, StringComparer.Ordinal).ToList();
            Debug.Log($"[CardArtSheet] 사도 {heroes.Count}명(명부 {Bolzena.RunUI.Roster.All.Count} · 데이터 사도 {data.Heroes.Count}) — {tag}");
            var cam = d.Cam;
            float camH = cam.orthographicSize * 2, camW = camH * cam.aspect;
            const int cols = 6, rows = 4, per = cols * rows;
            float lab = 0.28f, gap = 0.08f, pairGap = 0.22f;
            float s = Mathf.Min((camW - 0.4f) / (cols * (2 * CardView.W + gap) + (cols - 1) * pairGap), (camH - 0.3f) / (rows * (CardView.H + lab + 0.06f)));
            float cw = CardView.W * s, ch = CardView.H * s;
            float pairW = 2 * cw + gap * s;
            float x0 = cam.transform.position.x - camW / 2 + (camW - (cols * pairW + (cols - 1) * pairGap * s)) / 2;
            float y0 = cam.transform.position.y + camH / 2 - 0.15f;
            var oldHeroOf = CardView.HeroOf;
            for (int p = 0; p * per < heroes.Count; p++)
            {
                var root = Make.Node("cardartsheet", null);
                Make.Box("bg", root, Res.UI("white"), new Vector3(cam.transform.position.x, cam.transform.position.y, 0), new Vector2(camW + 2, camH + 2), 900, new Color(0.04f, 0.05f, 0.09f));
                var page = heroes.Skip(p * per).Take(per).ToList();
                var fakes = new List<HeroState>();
                CardView.HeroOf = i => i >= 0 && i < fakes.Count ? fakes[i] : null;
                for (int i = 0; i < page.Count; i++)
                {
                    var h = page[i];
                    var hd = data.Hero(h.CoreId);
                    var look = Look.Hero(h.CoreId);
                    fakes.Add(new HeroState { Key = look.Art, Id = h.CoreId, Name = h.ko, Tint = look.Tint, Nature = hd.Nature });
                    string id = hd.Starter.FirstOrDefault();
                    if (id == null) continue;
                    var cv = data.View(id);
                    if (cv == null) continue;
                    string type = cv.Type;
                    var info = new CardInfo
                    {
                        Id = id, Name = cv.Name, Hero = i, HeroName = h.ko, Cost = cv.Cost, TypeName = type,
                        Type = type == "공격" ? CardType.Attack : type == "강화" ? CardType.Power : type == "스킬" ? CardType.Skill : CardType.Status,
                        Text = text.Card(cv), Art = Look.CardArt(cv.Def.Hero, Bolzena.Core.GameData.BaseId(id), cv.Unique, cv.Type),
                        Tags = cv.Tags.ToList(), Unique = cv.Unique, Nature = hd.Nature,
                    };
                    int r = i / cols, c = i % cols;
                    float px = x0 + c * (pairW + pairGap * s), py = y0 - r * (ch + (lab + 0.06f) * s) - ch / 2;
                    for (int k = 0; k < 2; k++)
                    {
                        var v = CardView.Create(root, info);
                        v.ShowDesc = k == 1; v.ShowPin = k == 1;
                        v.TargetPos = new Vector3(px + cw / 2 + k * (cw + gap * s), py, 0);
                        v.TargetScale = s; v.TargetRot = 0;
                        v.SetOrder(1000 + (i * 2 + k) * 20);
                        v.Snap();
                        if (k == 1) Guides(v.transform, info.Art, 1000 + (i * 2 + k) * 20 + 9);
                    }
                    Make.Text("label", root, $"{h.ko}  <size=75%><color=#9aa3bd>{h.art}</color></size>", new Vector3(px + pairW / 2, py - ch / 2 - lab * s * 0.55f, 0), lab * s * 0.8f, 990, new Color(0.93f, 0.94f, 1f), TMPro.TextAlignmentOptions.Center, pairW);
                }
                yield return null;
                t = 0;
                while (t < 60 && Bolzena.RunUI.StandingSnap.Pending > 0) { t += Time.unscaledDeltaTime; yield return null; }
                yield return new WaitForSecondsRealtime(0.8f);
                yield return new WaitForEndOfFrame();
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                var path = Path.Combine(dir, $"battlecards_{tag}_{p + 1}.png");
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Destroy(tex);
                Debug.Log("[Capture] " + Path.GetFileName(path));
                Destroy(root.gameObject);
                yield return null;
            }
            CardView.HeroOf = oldHeroOf;
            Debug.Log("[CardArtSheet] 끝");
            yield return new WaitForSecondsRealtime(0.3f);
            Application.Quit(0);
        }

        // 확대 모습 카드에 안내선 · 얼굴 상자(카드 단위 — 그림 창 = H − 0.07 가운데)
        static void Guides(Transform card, string art, int order)
        {
            const float aw = CardView.W - 0.07f, ah = CardView.H - 0.07f;
            float Y(float frac) => ah / 2 - frac * ah;
            void Bar(Vector3 pos, Vector2 size, Color col) => Make.Box("guide", card, Res.UI("white"), pos, size, order, col);
            Bar(new Vector3(0, Y(0.26f), 0), new Vector2(aw, 0.012f), new Color(0.45f, 0.8f, 1f, 0.85f));
            Bar(new Vector3(0, Y(0.61f), 0), new Vector2(aw, 0.012f), new Color(1f, 0.6f, 0.2f, 0.85f));
            if (art == null || !art.StartsWith("st:")) return;
            var key = art.Substring(3);
            Rect r;
            bool ok = Bolzena.RunUI.StandingSnap.OldCardCrop ? Bolzena.RunUI.StandingSnap.CropRect(key, Bolzena.RunUI.StandingSnap.CardRatio, 0.56f, out r) : Bolzena.RunUI.StandingSnap.CardCropRect(key, out r);
            if (!ok || !Bolzena.RunUI.StandingFit.TryGet(key, out var f) || !f.HasHead) return;
            var fb = f.FaceBox;
            float Fy(float yy) => Y((r.yMax - yy) / r.height);
            float Fx(float xx) => -aw / 2 + (xx - r.xMin) / r.width * aw;
            var pink = new Color(1f, 0.35f, 0.75f, 0.85f);
            float l = Fx(fb.xMin), rr = Fx(fb.xMax), tp = Fy(fb.yMax), bt = Fy(fb.yMin);
            Bar(new Vector3((l + rr) / 2, tp, 0), new Vector2(rr - l, 0.01f), pink);
            Bar(new Vector3((l + rr) / 2, bt, 0), new Vector2(rr - l, 0.01f), pink);
            Bar(new Vector3(l, (tp + bt) / 2, 0), new Vector2(0.01f, tp - bt), pink);
            Bar(new Vector3(rr, (tp + bt) / 2, 0), new Vector2(0.01f, tp - bt), pink);
        }
    }
}
