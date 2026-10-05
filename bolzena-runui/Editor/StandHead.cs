using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Spine;
using UnityEngine;

namespace Bolzena.RunUI.EditorTools
{
    // 스탠딩 머리 상자 재기(2026-10 사용자: 「허리 굽은 스탠딩 얼굴이 반 잘림」) — 원작 스탠딩 135명(standing_fit.json 의 그림 키)을
    //   spine-csharp 로 그 자리에서 읽어(텍스처 없이 · 스킨 Normal · Idle_1 첫 프레임 · 배율 1 = 표와 같은 원본 단위) 머리 뼈와 「머리 둘레 상자」를 잰다.
    //   머리 둘레 상자 = 머리 뼈 아래에 달린 슬롯(머리 · 머리카락 · 얼굴 · 귀 · 뿔)의 꼭짓점 가운데 머리 높이 언저리의 것 — 장신구 · 모자 · 이펙트 · 감정 표시 ·
    //   소품 낱말이 든 슬롯은 뺀다(Docs/스탠딩맞춤.md 의 몸 고르기 낱말). 긴 뒷머리는 머리 뼈 아래로 머리 반지름의 0.8 배까지만 센다.
    //   결과: Resources/RunUI/standing_head.json { "<그림 키>": { headX, headY, l, b, r, t, cx, flag } } · 「숙인 · 기운 사도」 목록 Tools~/standing_head_report.txt
    //   Unity.exe -batchmode -quit -projectPath <시험 프로젝트> -executeMethod Bolzena.RunUI.EditorTools.StandHead.Run
    public static class StandHead
    {
        const string Src = @"C:\projects\볼제나\assets\standing";
        static readonly HashSet<string> NotHead = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "wing","wings","tail","tails","weapon","sword","gun","rifle","pistol","staff","spear","lance","wand","rod","shield","axe","hammer","scythe","blade","knife","dagger","bow","arrow",
            "ac","acc","accessory","hat","cap","crown","tiara","halo","ribbon","earring","necklace","pendant",
            "prop","item","book","bag","card","doll","mic","speaker","umbrella","parasol","flag","balloon","cup","bottle","candy","cane","broom","lamp","lantern","basket","box","plate","tray","bell","mirror","drone","pet","fan","chain",
            "flower","petal","leaf","leaves",
            "effect","fx","eff","vfx","shadow","commonshadow","glitter","star","starline","sparkle","twinkle","glow","light","aura","particle","smoke","spark","lightning","fire","flame","water","ice","snow","bubble","cloud","dust",
            "emo","emotion","sweat","tear","heart","note","mark","icon","text",
            "antenna","ahoge","hairpin","pin","clip","band","headband","headset","goggle","goggles","glasses","veil","mask","helmet","hood",
        };
        static readonly string[] Heads = { "Head", "Head_Root", "Face", "Face_Root" };

        class NoTex : TextureLoader { public void Load(AtlasPage page, string path) { } public void Unload(object texture) { } }

        static IEnumerable<string> Words(string name)
        {
            name = Regex.Replace(name ?? "", @"\[[^\]]*\]", " ");
            name = Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
            foreach (var w in Regex.Split(name, @"[^A-Za-z]+")) if (w.Length > 0) yield return w.ToLowerInvariant();
        }

        public static void Run()
        {
            var fitTa = File.ReadAllText(Path.Combine(PackageRoot(), "Runtime/Resources/RunUI/standing_fit.json"));
            var fit = (Dictionary<string, object>)FitJson.Parse(fitTa);
            var outJ = new StringBuilder("{\n \"_meta\": {\"note\": \"스탠딩 머리 상자(원본 단위 · Normal · Idle_1 첫 프레임). headX/headY = 머리 뼈, l/b/r/t = 머리 둘레 상자(머리 · 머리카락 · 얼굴 슬롯, 장신구 · 이펙트 뺌), fl/fb/fr/ft = 얼굴 상자(옆머리 뺌), cx = 얼굴 상자 가운데. flag = 숙임 · 기울임(얼굴이 옛 자르기 밖). Editor/StandHead.cs\"}");
            var report = new StringBuilder("그림 키\t사도\t머리-몸 가로(몸 키 비)\t몸 꼭대기-머리 상자 위(몸 키 비)\t옛 카드 자르기 머리 상자 담김\t옛 얼굴 칸 담김\t까닭\n");
            int n = 0, flagged = 0;
            foreach (var kv in fit)
            {
                if (kv.Key.StartsWith("_") || !(kv.Value is Dictionary<string, object> f)) continue;
                string art = kv.Key;
                var dir = Path.Combine(Src, art);
                var skel = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.skel").FirstOrDefault() : null;
                var atl = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.atlas").FirstOrDefault() : null;
                if (skel == null || atl == null) { Debug.LogWarning("[StandHead] 없음 " + art); continue; }
                try
                {
                    Atlas atlas;
                    using (var rd = new StreamReader(atl)) atlas = new Atlas(rd, dir, new NoTex());
                    var sd = new SkeletonBinary(new AtlasAttachmentLoader(atlas)) { Scale = 1 }.ReadSkeletonData(skel);
                    var sk = new Skeleton(sd);
                    var normal = sd.Skins.FirstOrDefault(s => string.Equals(s.Name, "Normal", StringComparison.OrdinalIgnoreCase)) ?? sd.DefaultSkin;
                    if (normal != null) sk.SetSkin(normal);
                    sk.SetToSetupPose();
                    var anim = sd.Animations.FirstOrDefault(a => a.Name == "Idle_1") ?? sd.Animations.FirstOrDefault(a => a.Name.StartsWith("Idle"));
                    anim?.Apply(sk, 0, 0, false, null, 1, MixBlend.Setup, MixDirection.In);
                    sk.UpdateWorldTransform();
                    // 머리 뼈
                    Bone head = null;
                    foreach (var hn in Heads)
                    {
                        head = sk.Bones.FirstOrDefault(b => Regex.Replace(b.Data.Name, @"^S\d+_", "").Equals(hn, StringComparison.OrdinalIgnoreCase));
                        if (head != null) break;
                    }
                    float fx(string k) => o2f(f, k);
                    float bodyH = fx("topY") - fx("footY");
                    if (head == null) { Debug.LogWarning("[StandHead] 머리 뼈 없음 " + art); continue; }
                    bool Under(Bone b) { for (var x = b; x != null; x = x.Parent) if (x == head) return true; return false; }
                    // 머리 아래 슬롯의 꼭짓점
                    var pts = new List<Vector2>();
                    float[] buf = new float[2048];
                    foreach (var slot in sk.DrawOrder)
                    {
                        var at = slot.Attachment;
                        if (at == null || !slot.Bone.Active || slot.A <= 0.01f || !Under(slot.Bone)) continue;
                        if (Words(slot.Data.Name).Concat(Words(at.Name)).Any(w => NotHead.Contains(w))) continue;
                        // 슬롯의 뼈 사슬(머리 아래)에도 빼는 낱말이 있으면 뺀다(모자 뼈 밑 장식 따위)
                        bool bad = false;
                        for (var x = slot.Bone; x != null && x != head; x = x.Parent) if (Words(x.Data.Name).Any(w => NotHead.Contains(w))) { bad = true; break; }
                        if (bad) continue;
                        int cnt = 0;
                        if (at is RegionAttachment ra) { ra.ComputeWorldVertices(slot, buf, 0, 2); cnt = 4; }
                        else if (at is MeshAttachment ma) { if (ma.WorldVerticesLength > buf.Length) buf = new float[ma.WorldVerticesLength]; ma.ComputeWorldVertices(slot, 0, ma.WorldVerticesLength, buf, 0, 2); cnt = ma.WorldVerticesLength / 2; }
                        for (int i = 0; i < cnt; i++) pts.Add(new Vector2(buf[i * 2], buf[i * 2 + 1]));
                    }
                    float hx = head.WorldX, hy = head.WorldY;
                    if (pts.Count == 0) pts.Add(new Vector2(hx, hy));
                    float top = pts.Max(p => p.y);
                    float R = Mathf.Max(40, Mathf.Min(top, fx("topY") > hy ? fx("topY") : top) - hy);   // 머리 반지름 — 몸 꼭대기(표의 topY, 뿔 · 귀 · 바보털 뺀 픽셀 판정)까지. 큰 뿔 · 사슴뿔이 부풀리지 않게
                    var ball = pts.Where(p => p.y >= hy - 0.8f * R && Mathf.Abs(p.x - hx) <= 2.2f * R).ToList();
                    if (ball.Count == 0) ball = pts;
                    float l = ball.Min(p => p.x), r = ball.Max(p => p.x), b = ball.Min(p => p.y), t = ball.Max(p => p.y);
                    // 얼굴 상자 — 머리 뼈 위쪽 · 머리 뼈에서 반지름 안의 꼭짓점(옆으로 늘어진 머리카락은 뺀다), 아래는 턱 언저리(머리 뼈 − 0.45R)
                    var face = pts.Where(p => p.y >= hy && Mathf.Abs(p.x - hx) <= 0.95f * R).ToList();
                    if (face.Count == 0) face = ball;
                    // 얼굴 상자 — 이등신 머리는 머리카락이 넓어 머리 상자 전체는 카드 창보다 넓다. 그래서 「얼굴」만: 가운데 = 머리 뼈와 머리 덩어리 가운데의 중간,
                    //   폭 ±0.55R · 위 = 머리 뼈 + 0.85R · 아래 = 머리 뼈 − 0.35R(턱 언저리)
                    float cx = ((face.Min(p => p.x) + face.Max(p => p.x)) / 2 + hx) / 2;
                    float fl = cx - 0.55f * R, fr = cx + 0.55f * R, ft = Mathf.Min(t, hy + 0.85f * R), fb = hy - 0.35f * R;
                    // 판정 — 머리 가운데가 몸 가운데선에서 멀다 · 머리 위가 몸 꼭대기보다 많이 아래 · 옛 자르기가 머리 상자를 못 담는다
                    float dx = Mathf.Abs(cx - fx("centerX")) / bodyH, dy = (fx("topY") - t) / bodyH;
                    float cardCov = Cover(OldCrop(f, 196f / 276f, 0.56f), fl, fb, fr, ft), faceCov = Cover(OldCrop(f, 1f, 0.34f), fl, fb, fr, ft);
                    var why = new List<string>();
                    if (dx > 0.10f) why.Add("머리가 옆으로");
                    if (dy > 0.06f) why.Add("머리가 몸 꼭대기보다 아래");
                    if (cardCov < 0.8f) why.Add("카드 자르기에서 머리 잘림");
                    if (faceCov < 0.7f) why.Add("얼굴 칸에서 머리 잘림");
                    bool flag = why.Count > 0;
                    if (flag) flagged++;
                    string hero = f.TryGetValue("hero", out var hv) ? hv as string : "";
                    report.Append($"{art}\t{hero}\t{dx:0.00}\t{dy:0.00}\t{cardCov:0.00}\t{faceCov:0.00}\t{(flag ? string.Join(" · ", why) : "-")}\n");
                    outJ.Append($",\n \"{art}\": {{\"hero\": \"{hero}\", \"headX\": {N(hx)}, \"headY\": {N(hy)}, \"l\": {N(l)}, \"b\": {N(b)}, \"r\": {N(r)}, \"t\": {N(t)}, \"cx\": {N(cx)}, \"fl\": {N(fl)}, \"fb\": {N(fb)}, \"fr\": {N(fr)}, \"ft\": {N(ft)}, \"flag\": {(flag ? "true" : "false")}}}");
                    n++;
                }
                catch (Exception e) { Debug.LogWarning($"[StandHead] {art} 실패 — {e.Message}"); }
            }
            outJ.Append("\n}\n");
            File.WriteAllText(Path.Combine(PackageRoot(), "Runtime/Resources/RunUI/standing_head.json"), outJ.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(PackageRoot(), "Tools~/standing_head_report.txt"), report.ToString(), new UTF8Encoding(false));
            Debug.Log($"[StandHead] {n}명 · 숙임/기울임 {flagged}명");
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(0);
        }

        // ═════ 편 키(뼈대로 잰 키) 점검 — 탈것 · 의자 · 큰 소품을 키에 넣었거나, 숙이거나 앉아 키가 줄어든 사도 찾기 ═════
        //   편 키 = 발 뼈 → (다리 사슬) → 두 사슬이 만나는 뼈 → (몸통 사슬) → 머리 뼈 의 관절 사이 거리 합(굽혀도 펴진 길이) + 머리 꼭대기(표 topY − headY).
        //   뿌리(root)를 지나는 사슬 · 발 뼈가 없는 사도는 「뼈로 못 잼」. 결과 Tools~/standing_height_check.txt
        //   Unity.exe -batchmode -quit -projectPath <시험 프로젝트> -executeMethod Bolzena.RunUI.EditorTools.StandHead.Heights
        static readonly string[] FootNames = { "Foot_L", "Foot_R", "Foot_L_T", "Foot_R_T", "Ankle_L", "Ankle_R", "Leg_L_3", "Leg_R_3", "Leg_L_2", "Leg_R_2", "Calf_L", "Calf_R" };

        public static void Heights()
        {
            var fit = (Dictionary<string, object>)FitJson.Parse(File.ReadAllText(Path.Combine(PackageRoot(), "Runtime/Resources/RunUI/standing_fit.json")));
            var rows = new List<(string art, string hero, float table, float straight, float pose, string how)>();
            foreach (var kv in fit)
            {
                if (kv.Key.StartsWith("_") || !(kv.Value is Dictionary<string, object> f)) continue;
                string art = kv.Key, hero = f.TryGetValue("hero", out var hv) ? hv as string : "";
                float table = o2f(f, "topY") - o2f(f, "footY");
                try
                {
                    var sk = Load(art);
                    if (sk == null) continue;
                    string Bare(Bone b) => Regex.Replace(b.Data.Name, @"^S\d+_", "");
                    Bone head = null;
                    foreach (var hn in Heads) { head = sk.Bones.FirstOrDefault(b => Bare(b).Equals(hn, StringComparison.OrdinalIgnoreCase)); if (head != null) break; }
                    float best = 0; string how = "뼈 없음";
                    if (head != null)
                    {
                        var headPath = new List<Bone>(); for (var x = head; x != null; x = x.Parent) headPath.Add(x);
                        foreach (var fn in FootNames)
                        {
                            var foot = sk.Bones.FirstOrDefault(b => Bare(b).Equals(fn, StringComparison.OrdinalIgnoreCase));
                            if (foot == null) continue;
                            var footPath = new List<Bone>(); for (var x = foot; x != null; x = x.Parent) footPath.Add(x);
                            var common = footPath.FirstOrDefault(headPath.Contains);
                            if (common == null || common.Parent == null || !Regex.IsMatch(Bare(common), "(?i)pel|plev|hip|spine|body|waist|chest")) continue;   // 골반 · 몸통 뼈에서 만나는 사슬만 믿는다(뿌리 · 조종 뼈 · 머리 장식 뼈 제외)
                            float Chain(List<Bone> path)
                            {
                                float s = 0;
                                for (int i = 0; i + 1 < path.Count && path[i] != common; i++)
                                    s += Vector2.Distance(new Vector2(path[i].WorldX, path[i].WorldY), new Vector2(path[i + 1].WorldX, path[i + 1].WorldY));
                                return s;
                            }
                            float len = Chain(footPath) + Chain(headPath) + Mathf.Max(0, o2f(f, "topY") - o2f(f, "headY")) + Mathf.Max(0, foot.WorldY - o2f(f, "footY"));
                            if (len > best) { best = len; how = Bare(foot) + "→" + Bare(common); }
                        }
                    }
                    // 자세: 머리 뼈 높이 − 바닥 ÷ 편 키(서 있으면 1 에 가깝다)
                    float pose = best > 0 && head != null ? (head.WorldY - o2f(f, "footY") + (o2f(f, "topY") - o2f(f, "headY"))) / best : 0;
                    rows.Add((art, hero, table, best, pose, how));
                }
                catch (Exception e) { Debug.LogWarning($"[StandHead] {art} 편 키 실패 — {e.Message}"); }
            }
            var ok = rows.Where(r => r.straight > 0).Select(r => r.table / r.straight).OrderBy(x => x).ToList();
            float med = ok.Count > 0 ? ok[ok.Count / 2] : 1;
            var sb = new StringBuilder($"그림 키\t사도\t표 몸 키\t편 키\t편 키×중앙비(={med:0.000})\t차이\t자세(서면 1)\t뼈 사슬\n");
            foreach (var r in rows.OrderBy(r => r.straight > 0 ? -Mathf.Abs(r.straight * med / r.table - 1) : 0))
            {
                float est = r.straight * med;
                sb.Append($"{r.art}\t{r.hero}\t{r.table:0}\t{r.straight:0}\t{est:0}\t{(r.straight > 0 ? (est / r.table - 1) * 100 : 0):+0.0;-0.0}%\t{r.pose:0.00}\t{r.how}\n");
            }
            File.WriteAllText(Path.Combine(PackageRoot(), "Tools~/standing_height_check.txt"), sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[StandHead] 편 키 {rows.Count}명 · 뼈로 잰 {ok.Count}명 · 중앙비 {med:0.000}");
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(0);
        }

        static Skeleton Load(string art)
        {
            var dir = Path.Combine(Src, art);
            var skel = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.skel").FirstOrDefault() : null;
            var atl = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.atlas").FirstOrDefault() : null;
            if (skel == null || atl == null) return null;
            Atlas atlas;
            using (var rd = new StreamReader(atl)) atlas = new Atlas(rd, dir, new NoTex());
            var sd = new SkeletonBinary(new AtlasAttachmentLoader(atlas)) { Scale = 1 }.ReadSkeletonData(skel);
            var sk = new Skeleton(sd);
            var normal = sd.Skins.FirstOrDefault(s => string.Equals(s.Name, "Normal", StringComparison.OrdinalIgnoreCase)) ?? sd.DefaultSkin;
            if (normal != null) sk.SetSkin(normal);
            sk.SetToSetupPose();
            var anim = sd.Animations.FirstOrDefault(a => a.Name == "Idle_1") ?? sd.Animations.FirstOrDefault(a => a.Name.StartsWith("Idle"));
            anim?.Apply(sk, 0, 0, false, null, 1, MixBlend.Setup, MixDirection.In);
            sk.UpdateWorldTransform();
            return sk;
        }

        // 옛 자르기(머리 상자 쓰기 전 StandingSnap) — 비교용
        static Rect OldCrop(Dictionary<string, object> f, float ratio, float frac)
        {
            float topY = o2f(f, "topY"), headY = o2f(f, "headY"), hairTop = o2f(f, "hairTop"), cxB = o2f(f, "centerX");
            float h, top;
            if (frac < 0.45f) { float head = Mathf.Clamp(topY - headY, 120, 320); h = head * (frac / 0.34f) * 1.55f; top = topY + h * 0.05f; }
            else { h = frac * 705f; top = topY + h * 0.06f; }
            top = Mathf.Max(top, hairTop + h * 0.03f);
            if (frac >= 0.45f) top = Mathf.Min(top, Mathf.Max(topY + h * 0.02f, headY + h * 0.62f));
            float w = h * ratio;
            return new Rect(cxB - w / 2, top - h, w, h);
        }

        static float Cover(Rect c, float l, float b, float r, float t)
        {
            float ix = Mathf.Max(0, Mathf.Min(r, c.xMax) - Mathf.Max(l, c.xMin)), iy = Mathf.Max(0, Mathf.Min(t, c.yMax) - Mathf.Max(b, c.yMin));
            float a = Mathf.Max(1, (r - l) * (t - b));
            return ix * iy / a;
        }

        static float o2f(Dictionary<string, object> o, string k) => o.TryGetValue(k, out var v) && v is double d ? (float)d : 0f;
        static string N(float v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        static string PackageRoot()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.bolzena.runui");
            return info != null ? info.resolvedPath : @"C:\projects\bolzena-runui";
        }
    }
}
