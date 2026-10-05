using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bolzena.RunUI
{
    // 지도 — 웹판 ui.js mapScreen(docs/10-지도.md). 한 층은 출발(N-0)에서 보스(N-10)까지 열 줄, 줄마다 갈래가 1~4.
    // 왼쪽 → 오른쪽으로 나아가고, 파티(미니미 셋)가 지금 칸에 선다. 갈 수 있는 칸은 숨쉬며 빛나고, 못 닿는 칸은 흐리다.
    // 웹판보다: 칸이 줄마다 차례로 튀어나오고, 길이 그어지고, 누르면 미니미가 걸어가서 들어간다.
    public partial class Flow
    {
        const float ColW = 196, MapPad = 150;
        bool walking;

        public void Map()
        {
            var f = P.Floor;
            Stage.SetBg(f.Bg != null && f.Bg.TryGetValue("fight", out var bg) ? bg : "stage3_2", 0.42f);
            Stage.Show("map", BuildMap, 1.4f);
        }

        void BuildMap(RectTransform root)
        {
            walking = false;
            var m = P.Map();
            var v = P.VillageDef;
            var here2 = P.Here;
            W.StatusBar(root, this, true, true, true, $"{P.S.Floor + 1}층 · {P.Floor.Name}",
                $"{v.Name} · {(here2 != null ? P.StageName(here2) : P.S.Floor + 1 + "-0")} / {P.S.Floor + 1}-10 · 빛나는 칸을 눌러 나아갑니다");

            // 지도 칸 — 머리 띠(위 96) 와 범례(아래 64) 사이. 높이는 캔버스에 맞춘다(폰 720 · PC 900)
            float top = 104, bottom = 64;
            var area = Ui.Rect("map", root);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(0, bottom); area.offsetMax = new Vector2(0, -top);
            var content = Ui.Scroll(area, out var sr, true);
            Destroy(content.GetComponent<ContentSizeFitter>());
            int rows = m.Rows.Count;
            // 넓은 화면(21:9 · 32:9)에서 조금만 넘치면 칸 사이를 좁혀 한 화면에 다 담는다 — 끝의 보스가 잘린 채 멈춰 보이지 않게
            float fitW = (Stage.Size.x - MapPad * 2) / Mathf.Max(1, rows - 1);
            float colW = fitW >= ColW * 0.82f ? Mathf.Min(ColW, fitW) : ColW;
            float width = MapPad * 2 + (rows - 1) * colW;
            content.sizeDelta = new Vector2(width, 0);
            var lines = Ui.Rect("lines", content).Fill();
            var nodes = Ui.Rect("nodes", content).Fill();
            var party = Ui.Rect("party", content).Fill();

            var reach = new HashSet<string>(P.Reachable());
            var ahead = P.Ahead();
            var here = m.At;
            var seen = new HashSet<string>(m.Seen);
            var pos = new Dictionary<string, Vector2>();
            float h = Mathf.Max(360, Stage.Size.y - top - bottom);   // 내용 높이
            float padY = Theme.C(72, 62);
            foreach (var row in m.Rows)
                foreach (var n in row)
                {
                    float y = row.Count == 1 ? 0.5f : (float)n.X;
                    pos[n.Id] = new Vector2(MapPad + n.Row * colW, -(padY + y * (h - padY * 2 - 20)));
                }

            // 길
            foreach (var row in m.Rows)
                foreach (var n in row)
                    foreach (var nx in n.Next)
                    {
                        if (!pos.ContainsKey(nx)) continue;
                        bool live = n.Id == here && reach.Contains(nx);
                        bool walked = seen.Contains(n.Id) && seen.Contains(nx);
                        bool faded = !ahead.Contains(nx) && !walked && !live;
                        Line(lines, pos[n.Id], pos[nx], live ? Theme.Gold : walked ? Theme.Gold.A(0.8f) : new Color(1, 1, 1, faded ? 0.1f : 0.38f),
                            live ? 4 : walked ? 3 : 2, live, n.Row * 0.06f + 0.25f);
                    }

            // 칸
            foreach (var row in m.Rows)
                foreach (var n in row)
                {
                    bool canGo = reach.Contains(n.Id);
                    bool done = seen.Contains(n.Id) && n.Id != here;
                    bool dim = !canGo && !ahead.Contains(n.Id) && !seen.Contains(n.Id);
                    MapNodeView(nodes, n, pos[n.Id], canGo, done, dim, n.Id == here, n.Row * 0.06f + (canGo ? 0.35f : 0.1f));
                }

            // 파티 — 지금 칸에 미니미 셋
            var at = here != null && pos.ContainsKey(here) ? pos[here] : pos[m.Rows[0][0].Id];
            var troop = Ui.Rect("troop", party);
            troop.anchorMin = troop.anchorMax = new Vector2(0, 1);
            troop.pivot = new Vector2(0.5f, 0);
            troop.sizeDelta = new Vector2(200, 120);
            troop.anchoredPosition = at + new Vector2(0, 14);
            var minis = new List<SkeletonGraphic>();
            int i = 0;
            foreach (var k in P.S.Party)
            {
                var hero = Roster.OfCore(k);
                var slot = Ui.Rect("mini" + i, troop).At(0.5f, 0, (i - 1) * 46, (i == 1 ? 8 : 0), 10, 10);
                var g = SpineUi.Make(slot, "minimi", hero.MiniSkin, 92, "Idle", "idle", "Stand");
                if (g != null) { minis.Add(g); g.AnimationState.Update(i * 0.37f); }
                else
                {
                    var face = W.Face(slot, hero, 58);
                    face.At(0.5f, 0, 0, 0, 58, 58);
                }
                i++;
            }
            Tw.Pop(troop, 0.2f, 0.5f, 0.5f);

            // 지금 자리로 스크롤 — 지금 칸이 화면 왼쪽 1/3 에
            StartCoroutine(ScrollTo(sr, content, at.x));

            // 칸 누르기
            foreach (var row in m.Rows)
                foreach (var n in row)
                {
                    if (!reach.Contains(n.Id)) continue;
                    var b = nodes.Find(n.Id)?.GetComponent<Btn>();
                    if (b == null) continue;
                    var node = n;
                    b.OnClick = () =>
                    {
                        if (walking) return;
                        StartCoroutine(Walk(troop, minis, pos[node.Id] + new Vector2(0, 14), () =>
                        {
                            var entered = P.Enter(node.Id);
                            if (entered == null) { Toast.Show("갈 수 없는 칸입니다"); walking = false; return; }
                            EnterNode(entered);
                        }));
                    };
                    Stage.Hot["node:" + n.Type] = Stage.Hot.ContainsKey("node:" + n.Type) ? Stage.Hot["node:" + n.Type] : b;
                    Stage.Hot["node:" + n.Id] = b;
                }

            // 아래 — 범례(유리 알약 하나에 칸 종류). 지도 위에는 글을 얹지 않는다(카제나처럼)
            var legend = Ui.Img(root, Theme.S("pill_dark", 46), new Color(1, 1, 1, 0.9f), "legend");
            legend.rectTransform.At(0.5f, 0, 0, 14, 10, 42);
            Ui.Row(legend.rectTransform, 10, TextAnchor.MiddleCenter, new RectOffset(22, 24, 4, 4), false, false);
            var lfit = legend.gameObject.AddComponent<ContentSizeFitter>(); lfit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var (t, tile) in new[] { ("일반", "tile_fight"), ("엘리트", "tile_elite"), ("이벤트", "tile_event"), ("휴식", "tile_camp"), ("휴식+상점", "tile_shop"), ("보스", "tile_boss") })
            {
                var g = Ui.Img(legend.transform, Theme.S(tile), Color.white, "g"); g.Pref(34, 20); g.preserveAspect = true;
                var lt = Ui.Text(legend.transform, t, Theme.FsSm, Theme.Sub); lt.textWrappingMode = TextWrappingModes.NoWrap; lt.overflowMode = TextOverflowModes.Overflow;
                lt.Pref(lt.preferredWidth + 8, 30);
            }
            Tw.Rise(legend.rectTransform, 0.3f, 14, 0.4f);
        }

        IEnumerator ScrollTo(ScrollRect sr, RectTransform content, float x)
        {
            yield return null;
            var view = sr.viewport.rect.width;
            float max = Mathf.Max(1, content.rect.width - view);
            float target = Mathf.Clamp01((x - view * 0.3f) / max);
            float from = Mathf.Clamp01(target - 0.15f);
            sr.horizontalNormalizedPosition = from;
            float t = 0;
            while (t < 0.8f && sr) { t += Time.unscaledDeltaTime; sr.horizontalNormalizedPosition = Mathf.Lerp(from, target, Tw.OutCubic(t / 0.8f)); yield return null; }
        }

        void Line(RectTransform parent, Vector2 a, Vector2 b, Color c, float w, bool dashed, float delay)
        {
            var d = b - a;
            float len = d.magnitude - 84;
            var mid = a + d * 0.5f;
            if (dashed)
            {
                int n = Mathf.Max(2, (int)(len / 16));
                var holder = Ui.Rect("dash", parent);
                holder.anchorMin = holder.anchorMax = holder.pivot = new Vector2(0, 1);
                holder.sizeDelta = Vector2.zero;
                holder.anchoredPosition = Vector2.zero;
                for (int i = 0; i <= n; i++)
                {
                    var p = a + d.normalized * 42 + d.normalized * (len * i / n);
                    var dot = Ui.Img(holder, Theme.S("dot"), c, "dot");
                    dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(0, 1);
                    dot.rectTransform.sizeDelta = new Vector2(w + 3, w + 3);
                    dot.rectTransform.anchoredPosition = p;
                    Tw.Pulse(dot, 0.25f, 1f, 0.9f, -i * 0.09f);
                }
                Tw.FadeIn(holder, 0.4f, delay);
                return;
            }
            var im = Ui.Img(parent, Theme.White, c, "line");
            var rt = im.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(len, w);
            rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            // 그어지기
            var full = len;
            rt.sizeDelta = new Vector2(0, w);
            Tw.Run(rt, 0.35f, t => { if (rt) rt.sizeDelta = new Vector2(full * t, w); }, Tw.OutCubic, delay);
        }

        static string TileOf(string type) => type switch
        {
            "fight" => "tile_fight", "elite" => "tile_elite", "boss" => "tile_boss", "event" => "tile_event",
            "camp" => "tile_camp", "campshop" => "tile_shop", _ => "tile_start",
        };
        static string GlyphOf(string type) => type switch
        {
            "fight" => "ic_swords", "elite" => "ic_skull", "boss" => "ic_crown", "event" => "ic_question",
            "camp" => "ic_fire", "campshop" => "ic_bag", _ => "ic_flag",
        };

        // 칸 하나 — 납작한 마름모 타일(종류 색) + 흰 글리프. 갈 수 있으면 금빛 테가 숨쉬고 이름 · 적이 밑에 붙는다.
        //   지나온 칸 = 잿빛 + 체크 · 닿지 않는 칸 = 흐리게 · 지금 칸 = 흰 테
        void MapNodeView(RectTransform parent, MapNode n, Vector2 p, bool canGo, bool done, bool dim, bool here, float delay)
        {
            bool boss = n.Type == "boss", start = n.Type != "boss" && GlyphOf(n.Type) == "ic_flag";
            float w = boss ? 156 : start ? 88 : 112, th = w * 0.6f;
            var holder = Ui.Rect(n.Id, parent);
            holder.anchorMin = holder.anchorMax = new Vector2(0, 1);
            holder.anchoredPosition = p;
            holder.sizeDelta = new Vector2(w, th);
            var sh = Ui.Img(holder, Theme.S("soft"), new Color(0, 0, 0, dim ? 0.2f : 0.45f), "shadow");
            sh.rectTransform.At(0.5f, 0.5f, 0, -th * 0.32f, w * 1.1f, th * 0.7f);
            if (canGo || here)
            {
                var glow = Ui.Img(holder, Theme.S("tile_glow"), here && !canGo ? Color.white.A(0.75f) : Theme.Gold, "glow");
                glow.rectTransform.Fill(-14, -10, -14, -10);
                if (canGo)
                {
                    Tw.Pulse(glow, 0.35f, 1f, 1.1f);
                    var halo = Ui.Img(holder, Theme.S("soft"), Theme.Gold.A(0.22f), "halo");
                    halo.rectTransform.Fill(-40, -40, -40, -40);
                    halo.transform.SetAsFirstSibling();
                }
            }
            var tint = dim ? new Color(1, 1, 1, 0.35f) : !canGo && !done && !here ? new Color(0.78f, 0.78f, 0.84f, 0.9f) : Color.white;
            var gem = Ui.Img(holder, Theme.S(done ? "tile_done" : TileOf(n.Type)), tint, "gem", canGo);
            gem.rectTransform.Fill();
            float gs = th * (boss ? 0.62f : 0.6f);
            var gl = Ui.Img(gem.rectTransform, Theme.S(done ? "ic_check" : GlyphOf(n.Type)), dim ? new Color(1, 1, 1, 0.35f) : Color.white, "glyph");
            gl.rectTransform.At(0.5f, 0.5f, 0, 1, gs, gs);
            gl.preserveAspect = true;
            if (canGo || boss || here)
            {
                var label = here ? "지금" : RunPort.KindKo(n.Type);
                var lt = Ui.Title(holder, label, boss ? Theme.FsMd : Theme.FsSm, canGo ? Theme.Gold : boss ? Theme.Hex("FFB3BB") : Theme.Ink, TextAlignmentOptions.Center);
                lt.rectTransform.At(0.5f, 0, 0, -24, 180, 24);
                lt.Outline(0.28f);
                lt.textWrappingMode = TextWrappingModes.NoWrap;
            }
            if (canGo)
            {
                // 반응은 holder 에(타일 + 빛이 같이 커지게)
                var hb = holder.gameObject.AddComponent<Btn>();
                hb.Bg = gem;
                hb.SetColor(Color.white);
                Tw.Breathe(gem.transform, 0.04f, 1.3f, n.Col * 0.4f);
                // 나오는 적 미리보기
                var foes = P.FoesAt(n);
                if (foes.Count > 0)
                {
                    var tip = Ui.Text(holder, string.Join(" · ", foes.Select(id => P.Data.Enemy(id)?.Name ?? id).Distinct()), Theme.FsCap, Theme.Ink, TextAlignmentOptions.Center);
                    tip.rectTransform.At(0.5f, 0, 0, -46, 230, 22);
                    tip.Outline(0.3f);
                    tip.textWrappingMode = TextWrappingModes.NoWrap;
                }
            }
            Tw.Pop(holder, delay, 0.3f, 0.45f);
        }

        IEnumerator Walk(RectTransform troop, List<SkeletonGraphic> minis, Vector2 to, System.Action arrive)
        {
            walking = true;
            foreach (var g in minis) SpineUi.Play(g, true, "Jump1", "Walk", "Run");
            var from = troop.anchoredPosition;
            float t = 0, dur = Settings.ReduceMotion ? 0.15f : Mathf.Clamp((to - from).magnitude / 420f, 0.45f, 0.9f);
            if (to.x < from.x) foreach (var g in minis) g.transform.localScale = new Vector3(-Mathf.Abs(g.transform.localScale.x), g.transform.localScale.y, 1);
            Sfx.Play("step");
            while (t < dur && troop)
            {
                t += Time.unscaledDeltaTime;
                float k = Tw.InOut(Mathf.Clamp01(t / dur));
                troop.anchoredPosition = Vector2.Lerp(from, to, k) + new Vector2(0, Mathf.Abs(Mathf.Sin(k * Mathf.PI * 3)) * 8);
                yield return null;
            }
            foreach (var g in minis) if (g) SpineUi.Play(g, true, "Idle", "idle", "Stand");
            yield return new WaitForSecondsRealtime(0.15f);
            arrive();
        }
    }
}
