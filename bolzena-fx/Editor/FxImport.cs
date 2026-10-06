using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bolzena.Fx.EditorTools
{
    // 원작 이펙트 일괄 변환 — Tools/fx_prepare.py 가 옮겨 둔 Assets/BolzenaFxData/Src 를 읽어
    //   Resources/BolzenaFx/Effects/<이름>.asset (FxEffect — 파티클 판) · Baked/<이름>.asset (FxSheet — 구운 낱장) ·
    //   FxLibrary.asset (사도 키 → 고학년 이펙트 이름들) · FxParticle.mat (셰이더가 빌드에 실리게)
    // 를 만든다. 그림 · 소리 가져오기 설정(sRGB · 압축 · 최대 크기 · 소리 압축)도 여기서 맞춘다. 다시 돌려도 같은 자산을 덮어 쓴다(GUID 유지).
    //   Unity.exe -batchmode -quit -projectPath <경로> -executeMethod Bolzena.Fx.EditorTools.FxImport.ImportAll
    public static class FxImport
    {
        public const string Data = "Assets/BolzenaFxData";
        static string Src => Data + "/Src";
        static string Out => Data + "/Resources/" + FxLibrary.Root;

        public static int Converted, Failed, Approx, Skipped3d, Baked;
        static string texDir = "fx";
        public static readonly List<string> Problems = new List<string>();

        [MenuItem("Bolzena/Fx — 원작 이펙트 변환")]
        public static void ImportAll()
        {
            Converted = Failed = Approx = Skipped3d = Baked = 0;
            Problems.Clear();
            AssetDatabase.Refresh();
            TextureSettings();
            AudioSettings();
            AudioMixer();
            Directory.CreateDirectory(Out + "/Effects");
            Directory.CreateDirectory(Out + "/Baked");
            BaseMaterial();
            var lib = LoadOrCreate<FxLibraryAsset>(Out + "/FxLibrary.asset");
            lib.Heroes.Clear();
            lib.Effects.Clear();
            var bakedNames = ImportBaked();
            var idx = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(Src + "/fx/index.json"));
            var heroes = (Dictionary<string, object>)idx["heroes"];
            var effects = (Dictionary<string, object>)idx["effects"];
            var perHero = new Dictionary<string, Dictionary<string, object>>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var kv in effects)
                {
                    var name = kv.Key;
                    var info = (Dictionary<string, object>)kv.Value;
                    var art = (string)info["hero"];
                    if (!perHero.TryGetValue(art, out var all))
                    {
                        var p = Src + "/fx/" + art + "/fx.json";
                        all = File.Exists(p) ? (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(p)) : new Dictionary<string, object>();
                        perHero[art] = all;
                    }
                    var entry = new FxLibraryAsset.Entry { Name = name, Hero = art, Dur = F(info, "dur", 1), Emitters = (int)F(info, "n", 0), Baked = bakedNames.Contains(name.ToLowerInvariant()) };
                    if (!all.TryGetValue(name, out var body)) { Failed++; Problems.Add(name + ": fx.json 에 없음"); lib.Effects.Add(entry); continue; }
                    try { MakeEffect(name, art, (Dictionary<string, object>)body); Converted++; }
                    catch (Exception ex) { Failed++; Problems.Add(name + ": " + ex.Message); }
                    lib.Effects.Add(entry);
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
            foreach (var kv in heroes)
            {
                var h = (Dictionary<string, object>)kv.Value;
                var he = new FxLibraryAsset.Hero { Key = kv.Key, Art = (string)h["name"], From = h.TryGetValue("from", out var f) ? (string)f : "" };
                foreach (var n in (List<object>)h["ult"]) he.Ult.Add((string)n);
                lib.Heroes.Add(he);
            }
            ImportMore(lib);
            // 고학년 몸짓 표(읽기만) — 런타임이 Resources 로 읽게 옮긴다
            var um = Path.GetFullPath("Packages/com.bolzena.fx/Runtime/Motion/ult_motion.json");
            if (File.Exists(um)) { File.Copy(um, Out + "/ult_motion.json", true); AssetDatabase.ImportAsset(Out + "/ult_motion.json"); Debug.Log("[FxImport] 고학년 몸짓 표 ult_motion.json"); }
            var uh = Path.GetFullPath("Packages/com.bolzena.fx/Runtime/Motion/ult_hits.json");   // 원작 고학년 타수 · 타격 시각
            if (File.Exists(uh)) { File.Copy(uh, Out + "/ult_hits.json", true); AssetDatabase.ImportAsset(Out + "/ult_hits.json"); Debug.Log("[FxImport] 고학년 타수 표 ult_hits.json"); }
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            Debug.Log($"[FxImport] 사도 {lib.Heroes.Count}명 · 이펙트 {Converted}개 변환 · 실패 {Failed} · 근사(이미터) {Approx} · 입체 메시로 뺀 이미터 {Skipped3d} · 구운 낱장 {Baked}");
            foreach (var p in Problems.Take(40)) Debug.Log("[FxImport] ! " + p);
        }

        // ── 카드 · 공용 이펙트(Tools/fx_extract_more.py → Src/fx2) ── 고학년 색인(Src/fx)에 갈래를 더한다
        static void ImportMore(FxLibraryAsset lib)
        {
            lib.Common.Clear();
            var ip = Src + "/fx2/index.json";
            if (!File.Exists(ip)) { Debug.Log("[FxImport] 카드 · 공용 이펙트 없음(Src/fx2) — Tools/fx_extract_more.py"); return; }
            var idx = (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(ip));
            var effects = (Dictionary<string, object>)idx["effects"];
            var have = new HashSet<string>(lib.Effects.Select(e => e.Name));
            var perOwner = new Dictionary<string, Dictionary<string, object>>();
            int n0 = Converted;
            texDir = "fx2";
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var kv in effects)
                {
                    var name = kv.Key;
                    if (have.Contains(name)) continue;          // 고학년 쪽에 이미 있다(궁극기가 없어 스킬로 메운 사도)
                    var info = (Dictionary<string, object>)kv.Value;
                    var owner = (string)info["hero"];
                    if (!perOwner.TryGetValue(owner, out var all))
                    {
                        var p = Src + "/fx2/" + owner + "/fx.json";
                        all = File.Exists(p) ? (Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(p)) : new Dictionary<string, object>();
                        perOwner[owner] = all;
                    }
                    var entry = new FxLibraryAsset.Entry { Name = name, Hero = owner, Dur = F(info, "dur", 1), Emitters = (int)F(info, "n", 0), Baked = false };
                    if (!all.TryGetValue(name, out var body)) { Failed++; Problems.Add(name + ": fx2 fx.json 에 없음"); continue; }
                    try { MakeEffect(name, owner, (Dictionary<string, object>)body); Converted++; lib.Effects.Add(entry); have.Add(name); }
                    catch (Exception ex) { Failed++; Problems.Add(name + ": " + ex.Message); }
                }
            }
            finally { AssetDatabase.StopAssetEditing(); texDir = "fx"; }
            var heroes = (Dictionary<string, object>)idx["heroes"];
            var byKey = lib.Heroes.ToDictionary(h => h.Key);
            List<string> L(Dictionary<string, object> h, string k) => h.TryGetValue(k, out var o) && o is List<object> l ? l.Select(x => (string)x).Where(have.Contains).ToList() : new List<string>();
            foreach (var kv in heroes)
            {
                var h = (Dictionary<string, object>)kv.Value;
                if (!byKey.TryGetValue(kv.Key, out var he))
                {
                    he = new FxLibraryAsset.Hero { Key = kv.Key, Art = (string)h["name"], From = "" };
                    lib.Heroes.Add(he); byKey[kv.Key] = he;
                }
                he.Attack = L(h, "attack"); he.Power = L(h, "power"); he.Skill = L(h, "skill"); he.Sig = L(h, "sig");
                if (he.Ult.Count == 0) he.Ult.AddRange(L(h, "ult"));   // 고학년 추출에 없던 사도(우이 — 원작 uieru)
            }
            if (idx.TryGetValue("common", out var co) && co is Dictionary<string, object> cd)
                foreach (var kv in cd)
                    lib.Common.Add(new FxLibraryAsset.Group { Kind = kv.Key, Names = ((List<object>)kv.Value).Select(x => (string)x).Where(have.Contains).ToList() });
            Debug.Log($"[FxImport] 카드 · 공용 이펙트 {Converted - n0}개 변환 · 공용 갈래 {lib.Common.Count}");
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static void BaseMaterial()
        {
            var path = Out + "/FxParticle.mat";
            var sh = Shader.Find(BolzenaFx.ShaderName);
            if (sh == null) { Debug.LogError("[FxImport] 셰이더 없음 " + BolzenaFx.ShaderName); return; }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); }
            else m.shader = sh;
            EditorUtility.SetDirty(m);
        }

        // ── 가져오기 설정 ──
        static void TextureSettings()
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Src });
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) continue;
                    bool baked = path.Contains("/baked/");
                    int max = baked ? 8192 : 1024;
                    if (ti.textureType == TextureImporterType.Default && ti.sRGBTexture && ti.alphaIsTransparency && ti.wrapMode == TextureWrapMode.Clamp
                        && ti.maxTextureSize == max && ti.npotScale == TextureImporterNPOTScale.None && ti.mipmapEnabled) continue;
                    ti.textureType = TextureImporterType.Default;
                    ti.sRGBTexture = true;
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.filterMode = FilterMode.Bilinear;
                    ti.mipmapEnabled = true;
                    ti.npotScale = TextureImporterNPOTScale.None;
                    ti.maxTextureSize = max;
                    ti.textureCompression = TextureImporterCompression.CompressedHQ;
                    ti.isReadable = false;
                    ti.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }

        static void AudioSettings()
        {
            var root = Data + "/Resources/BolzenaAudio";
            if (!Directory.Exists(root)) return;
            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { root });
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g);
                    if (!(AssetImporter.GetAtPath(path) is AudioImporter ai)) continue;
                    bool voice = path.Contains("/voice/");
                    var s = ai.defaultSampleSettings;
                    var want = voice ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                    if (s.loadType == want && s.compressionFormat == AudioCompressionFormat.Vorbis) continue;
                    s.loadType = want;
                    s.compressionFormat = AudioCompressionFormat.Vorbis;
                    s.quality = 0.7f;
                    ai.defaultSampleSettings = s;
                    ai.forceToMono = false;
                    ai.loadInBackground = voice;
                    ai.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }

        // 믹서 — Master ← Sfx(압축기 — 여럿이 겹친 순간만 누른다, 웹판 sfx.js 의 DynamicsCompressor 와 같은 값) · Voice.
        // 유니티는 믹서를 만드는 공개 API 가 없어 에디터 내부(AudioMixerController)를 반사로 부른다. 안 되면 건너뛴다(BolzenaAudio 는 믹서 없이도 돈다)
        static void AudioMixer()
        {
            var dir = Data + "/Resources/BolzenaAudio";
            if (!Directory.Exists(dir)) return;
            var path = dir + "/BolzenaMixer.mixer";
            if (File.Exists(path)) return;
            try
            {
                var asm = typeof(UnityEditor.Editor).Assembly;
                var tC = asm.GetType("UnityEditor.Audio.AudioMixerController");
                var tG = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");
                var tE = asm.GetType("UnityEditor.Audio.AudioMixerEffectController");
                const System.Reflection.BindingFlags BF = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static;
                var ctrl = tC.GetMethod("CreateMixerControllerAtPath", BF).Invoke(null, new object[] { path });
                var master = tC.GetProperty("masterGroup", BF).GetValue(ctrl);
                object Group(string name)
                {
                    var g = tC.GetMethod("CreateNewGroup", BF).Invoke(ctrl, new object[] { name, false });
                    tC.GetMethod("AddChildToParent", BF).Invoke(ctrl, new object[] { g, master });
                    return g;
                }
                var sfx = Group("Sfx");
                Group("Voice");
                try
                {
                    var eff = Activator.CreateInstance(tE, new object[] { "Compressor" });
                    tE.GetMethod("PreallocateGUIDs", BF)?.Invoke(eff, null);
                    var effects = (Array)tG.GetProperty("effects", BF).GetValue(sfx);
                    tG.GetMethod("InsertEffect", BF).Invoke(sfx, new object[] { eff, effects.Length });
                    AssetDatabase.AddObjectToAsset((UnityEngine.Object)eff, (UnityEngine.Object)ctrl);
                    var snap = tC.GetProperty("TargetSnapshot", BF)?.GetValue(ctrl) ?? tC.GetProperty("startSnapshot", BF)?.GetValue(ctrl);
                    var setVal = tE.GetMethod("SetValueForParameter", BF);
                    if (snap != null && setVal != null)
                    {
                        void P(string n, float v) { try { setVal.Invoke(eff, new object[] { ctrl, snap, n, v }); } catch { } }
                        P("Threshold", -12f); P("Attack", 4f); P("Release", 200f); P("Make up gain", 0f);
                    }
                }
                catch (Exception ex) { Debug.LogWarning("[FxImport] 믹서 압축기 못 붙임: " + ex.Message); }
                EditorUtility.SetDirty((UnityEngine.Object)ctrl);
                AssetDatabase.SaveAssets();
                Debug.Log("[FxImport] 믹서 만듦 " + path);
            }
            catch (Exception ex) { Debug.LogWarning("[FxImport] 믹서 못 만듦(건너뜀): " + (ex.InnerException ?? ex).Message); }
        }

        // ── 구운 낱장 ──
        static HashSet<string> ImportBaked()
        {
            var names = new HashSet<string>();
            var ip = Src + "/baked/index.json";
            if (!File.Exists(ip)) return names;
            var idx = (Dictionary<string, object>)((Dictionary<string, object>)MiniJson.Parse(File.ReadAllText(ip)))["effects"];
            foreach (var kv in idx)
            {
                var e = (Dictionary<string, object>)kv.Value;
                var s = LoadOrCreate<FxSheet>(Out + "/Baked/" + kv.Key + ".asset");
                s.Fps = F(e, "fps", 30);
                s.Frames = (int)F(e, "frames", 1);
                s.W = (int)F(e, "w", 1); s.H = (int)F(e, "h", 1); s.Cols = (int)F(e, "cols", 1);
                var pages = (List<object>)e["pages"];
                s.Pages = new Texture2D[pages.Count];
                s.PageFrames = new int[pages.Count];
                bool ok = true;
                for (int i = 0; i < pages.Count; i++)
                {
                    var p = (Dictionary<string, object>)pages[i];
                    var file = Path.ChangeExtension((string)p["file"], ".png");
                    s.Pages[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(Src + "/baked/" + file);
                    s.PageFrames[i] = (int)F(p, "frames", 0);
                    if (s.Pages[i] == null) { ok = false; Problems.Add(kv.Key + ": 구운 쪽 없음 " + file); }
                }
                var an = (List<object>)e["anchor"];
                s.Anchor = new Vector2((float)(double)an[0], (float)(double)an[1]);
                s.Ppu = F(e, "ppu", 16);
                s.Cx = F(e, "cx", 0);
                s.Dur = F(e, "dur", 1);
                s.Scale = F(e, "scale", 1);
                s.Dx = F(e, "dx", 0); s.Dy = F(e, "dy", 0);
                s.Reach = e.TryGetValue("reach", out var r) && r is bool rb && rb;
                s.FadeFrom = e.ContainsKey("fadeFrom") ? F(e, "fadeFrom", -1) : -1;
                EditorUtility.SetDirty(s);
                if (ok) { names.Add(kv.Key); Baked++; }
            }
            return names;
        }

        // ── 파티클 판 ──
        static void MakeEffect(string name, string art, Dictionary<string, object> body)
        {
            var fx = LoadOrCreate<FxEffect>(Out + "/Effects/" + name + ".asset");
            fx.Hero = art;
            fx.Dur = F(body, "dur", 1);
            fx.Em = new List<FxEmitter>();
            foreach (var o in (List<object>)body["em"])
            {
                var d = (Dictionary<string, object>)o;
                var e = Emitter(d);
                if (e.M3d) Skipped3d++;
                if (e.Tex == null) { Problems.Add(name + ": 텍스처 없음 " + e.TexPath); }
                // 근사 — 셰이더 그래프(디졸브 · 노이즈 · UV 흐름)는 원작 추출에서 이미 빠졌고, 남은 근사는 구운 그림(_lum · _toon · _dslv)
                if (e.TexPath != null && (e.TexPath.Contains("_lum") || e.TexPath.Contains("_toon") || e.TexPath.Contains("_dslv"))) Approx++;
                fx.Em.Add(e);
            }
            EditorUtility.SetDirty(fx);
        }

        static FxEmitter Emitter(Dictionary<string, object> d)
        {
            var e = new FxEmitter();
            e.N = S(d, "n");
            e.TexPath = S(d, "tex");
            if (e.TexPath != null) e.Tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Src + "/" + texDir + "/" + e.TexPath);
            e.Add = F(d, "add", 0) != 0;
            e.Tint = C4(d, "tint", Color.white);
            e.Dur = F(d, "dur", 0);
            e.Loop = F(d, "loop", 0) != 0;
            e.Delay = V2(d, "delay"); e.Life = V2(d, "life"); e.Speed = V2(d, "speed"); e.Size = V2(d, "size");
            e.Rot = V2(d, "rot"); e.Spin = V2(d, "spin");
            e.Sy = Or1(F(d, "sy", 1));
            if (d.TryGetValue("col", out var cl) && cl is List<object> cll && cll.Count > 0)
                e.Col = cll.Select(x => ToColor((List<object>)x)).ToArray();
            e.ColLerp = F(d, "colLerp", 0) != 0;
            e.Grav = F(d, "grav", 0);
            e.Rate = F(d, "rate", 0);
            if (d.TryGetValue("bursts", out var bs) && bs is List<object> bl)
                e.Bursts = bl.Select(x => { var l = Nums(x); return new Vector4(At(l, 0), At(l, 1), l.Count > 2 ? l[2] : 1, l.Count > 3 ? l[3] : 0.01f); }).ToArray();
            else e.Bursts = new Vector4[0];
            e.Max = (int)F(d, "max", 1000);
            if (e.Max <= 0) e.Max = 1000;
            if (d.TryGetValue("shape", out var so) && so is Dictionary<string, object> sh)
            {
                e.HasShape = true;
                e.Shape = new FxShape
                {
                    K = Kind(S(sh, "k")), R = F(sh, "r", 0), T = sh.ContainsKey("t") && sh["t"] != null ? F(sh, "t", 1) : 1,
                    Arc = Or(F(sh, "arc", 360), 360), A = F(sh, "a", 0), Rnd = F(sh, "rnd", 0), Sph = F(sh, "sph", 0),
                };
            }
            e.Pos = V2(d, "pos");
            e.M = Arr(d, "m", 6) ?? new float[] { 1, 0, 0, 0, 1, 0 };
            e.Mt = Arr(d, "mt", 6); e.HasMt = e.Mt != null;
            if (d.ContainsKey("sc")) { e.HasSc = true; e.Sc = V2(d, "sc"); }
            e.SizeOL = Keys2(d, "sizeOL");
            e.SizeK = Or1(F(d, "sizeK", 1));
            if (d.TryGetValue("colOL", out var co) && co is List<object> col)
                e.ColOL = col.Select(x => { var l = Nums(x); return new Vector4(At(l, 0), At(l, 1), At(l, 2), At(l, 3)); }).ToArray();
            e.AlphaOL = Keys2(d, "alphaOL");
            if (d.ContainsKey("vel")) { e.HasVel = true; e.Vel = V3(d, "vel"); e.VelW = F(d, "velW", 0) != 0; }
            e.Radial = F(d, "radial", 0);
            e.SpdMul = Or1(F(d, "spdMul", 1));
            if (d.ContainsKey("force")) { e.HasForce = true; e.Force = V3(d, "force"); e.ForceW = F(d, "forceW", 0) != 0; }
            if (d.ContainsKey("limit") && d["limit"] != null) { e.HasLimit = true; e.Limit = F(d, "limit", 0); e.Damp = F(d, "damp", 0); }
            e.Drag = F(d, "drag", 0);
            if (d.TryGetValue("sheet", out var sho) && sho is Dictionary<string, object> s)
            {
                e.HasSheet = true;
                e.Sheet = new FxSheetAnim
                {
                    Tx = Math.Max(1, (int)F(s, "tx", 1)), Ty = Math.Max(1, (int)F(s, "ty", 1)), Row = (int)F(s, "row", 0),
                    HasF = s.ContainsKey("f"), F = V2(s, "f"), Fk = Keys2(s, "fk"), HasSf = s.ContainsKey("sf"), Sf = V2(s, "sf"),
                    Cyc = Or1(F(s, "cyc", 1)), Fps = F(s, "fps", 0),
                };
            }
            e.Asp = Or1(F(d, "asp", 1));
            if (d.ContainsKey("stretch")) { e.HasStretch = true; e.Stretch = V2(d, "stretch"); }
            e.Flat = F(d, "flat", 0) != 0;
            var q = Arr(d, "quad", 4);
            if (q != null) { e.HasQuad = true; e.Quad = new Vector4(q[0], q[1], q[2], q[3]); }
            if (d.ContainsKey("pivot")) { e.HasPivot = true; e.Pivot = V2(d, "pivot"); }
            if (d.ContainsKey("ext")) { e.HasExt = true; e.Ext = V2(d, "ext"); }
            e.M3d = F(d, "m3d", 0) != 0;
            e.FlipU = F(d, "flipU", 0);
            return e;
        }

        static FxShapeKind Kind(string k)
        {
            switch (k)
            {
                case "sphere": return FxShapeKind.Sphere;
                case "hemi": return FxShapeKind.Hemi;
                case "cone": return FxShapeKind.Cone;
                case "circle": return FxShapeKind.Circle;
                case "donut": return FxShapeKind.Donut;
                case "edge": return FxShapeKind.Edge;
                case "box": return FxShapeKind.Box;
                case "rect": return FxShapeKind.Rect;
            }
            return FxShapeKind.None;
        }

        // ── 값 읽기 ── 웹판의 `x || 1` 처럼 0 을 1 로 읽는 것은 Or1
        static float Or1(float v) => v == 0 ? 1 : v;
        static float Or(float v, float d) => v == 0 ? d : v;
        static float At(List<float> l, int i) => i < l.Count ? l[i] : 0;
        static string S(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) ? v as string : null;
        static float F(Dictionary<string, object> d, string k, float def)
        {
            if (!d.TryGetValue(k, out var v) || v == null) return def;
            if (v is double x) return (float)x;
            if (v is bool b) return b ? 1 : 0;
            return def;
        }
        static List<float> Nums(object o) => o is List<object> l ? l.Select(x => x is double v ? (float)v : 0f).ToList() : new List<float>();
        static Vector2 V2(Dictionary<string, object> d, string k)
        {
            if (!d.TryGetValue(k, out var o) || !(o is List<object>)) return Vector2.zero;
            var l = Nums(o);
            return new Vector2(At(l, 0), l.Count > 1 ? l[1] : At(l, 0));
        }
        static Vector3 V3(Dictionary<string, object> d, string k) { var l = d.TryGetValue(k, out var o) ? Nums(o) : new List<float>(); return new Vector3(At(l, 0), At(l, 1), At(l, 2)); }
        static float[] Arr(Dictionary<string, object> d, string k, int n)
        {
            if (!d.TryGetValue(k, out var o) || !(o is List<object>)) return null;
            var l = Nums(o);
            var a = new float[n];
            for (int i = 0; i < n; i++) a[i] = At(l, i);
            return a;
        }
        static Vector2[] Keys2(Dictionary<string, object> d, string k)
        {
            if (!d.TryGetValue(k, out var o) || !(o is List<object> l) || l.Count == 0) return null;
            return l.Select(x => { var v = Nums(x); return new Vector2(At(v, 0), At(v, 1)); }).ToArray();
        }
        static Color ToColor(List<object> l) { var v = Nums(l); return new Color(At(v, 0), At(v, 1), At(v, 2), v.Count > 3 ? v[3] : 1); }
        static Color C4(Dictionary<string, object> d, string k, Color def) => d.TryGetValue(k, out var o) && o is List<object> l ? ToColor(l) : def;
    }
}
