using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Bolzena.View
{
    // 이펙트 공장 — 구운 타격 낱장(원작 fx-baked), 빛 · 고리 · 파편 · 불꽃 파티클, 피해 숫자, 글 터뜨리기, 화면 번쩍임.
    // Field 는 싸움터 마디(흔들림을 같이 탄다), Screen 은 화면 마디(안 흔들린다).
    public static class Vfx
    {
        public static Transform Field, Screen;

        [System.Serializable] class SheetInfo { public string name; public float fps; public int frames, w, h, cols; public float ax, ay, ppu; }
        [System.Serializable] class SheetList { public SheetInfo[] sheets; }
        static Dictionary<string, SheetInfo> infos;
        static readonly Dictionary<string, Sprite[]> sheets = new Dictionary<string, Sprite[]>();
        static readonly Dictionary<string, Material> texMats = new Dictionary<string, Material>();

        // 원작 단위 → 우리 화면 단위(사도 SD 를 0.3 배로 세웠다)
        public const float Unit = 0.3f;

        // ── 낱장 ──
        static Sprite[] Baked(string name, out float fps)
        {
            fps = 30;
            if (infos == null)
            {
                infos = new Dictionary<string, SheetInfo>();
                var ta = Resources.Load<TextAsset>("Fx/sheets");
                if (ta != null) foreach (var s in JsonUtility.FromJson<SheetList>(ta.text).sheets) infos[s.name] = s;
            }
            if (!infos.TryGetValue(name, out var info)) return null;
            fps = info.fps;
            if (sheets.TryGetValue(name, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>("Fx/Sheets/" + name);
            if (tex == null) return null;
            var list = new Sprite[info.frames];
            for (int i = 0; i < info.frames; i++)
            {
                int c = i % info.cols, r = i / info.cols;
                var rect = new Rect(c * info.w, tex.height - (r + 1) * info.h, info.w, info.h);
                list[i] = Sprite.Create(tex, rect, new Vector2(info.ax / info.w, 1 - info.ay / info.h), info.ppu, 0, SpriteMeshType.FullRect);
            }
            sheets[name] = list;
            return list;
        }

        // 원작 공용 텍스처의 칸 나눈 낱장(FX_Tig_Slash08 처럼 한 장에 여러 칸)
        public static Sprite[] Grid(string tex, int cols, int rows, float ppu = 100f)
        {
            string key = tex + cols + "x" + rows;
            if (sheets.TryGetValue(key, out var cached)) return cached;
            var sp = Res.FxTex(tex);
            if (sp == null) return null;
            var t = sp.texture;
            int w = t.width / cols, h = t.height / rows;
            var list = new Sprite[cols * rows];
            for (int i = 0; i < list.Length; i++)
            {
                int c = i % cols, r = i / cols;
                list[i] = Sprite.Create(t, new Rect(c * w, t.height - (r + 1) * h, w, h), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
            }
            sheets[key] = list;
            return list;
        }

        public static SheetAnim Sheet(string name, Vector3 pos, float scale, int order, float rot = 0, bool flipX = false, float speed = 1f,
                                      bool additive = false, float boost = 1f, Color? tint = null, Transform parent = null)
        {
            var frames = Baked(name, out var fps);
            if (frames == null) return null;
            return Play(frames, fps * speed, pos, scale * Unit, order, rot, flipX, additive, boost, tint, parent);
        }

        public static SheetAnim Play(Sprite[] frames, float fps, Vector3 pos, float scale, int order, float rot = 0, bool flipX = false,
                                     bool additive = false, float boost = 1f, Color? tint = null, Transform parent = null)
        {
            if (frames == null) return null;
            var sr = Make.Sprite("sheet", parent ? parent : Field, frames[0], pos, order, tint, Res.SpriteMat(additive, boost));
            sr.flipX = flipX;
            sr.transform.localScale = Vector3.one * scale;
            sr.transform.localRotation = Quaternion.Euler(0, 0, rot);
            var a = sr.gameObject.AddComponent<SheetAnim>();
            a.Frames = frames;
            a.Fps = fps;
            return a;
        }

        // 낱장 · 공용 텍스처를 미리 — 첫 타격 · 첫 고학년에서 그림을 처음 올리며 멈칫하지 않게
        public static void Preload()
        {
            Baked("fx_common_hit_1_m", out _);
            if (infos != null) foreach (var n in infos.Keys) Baked(n, out _);
            foreach (var t in Resources.LoadAll<Sprite>("Fx/Tex")) { }
        }

        // ── 빛 한 덩이(커졌다 사라진다) ──
        public static SpriteRenderer Glow(Vector3 pos, float size, Color c, float dur, float boost = 2f, string tex = null, int order = 160,
                                          Transform parent = null, float grow = 1.6f, float rot = 0)
        {
            var sp = tex == null ? Res.UI("soft") : Res.FxTex(tex);
            var sr = Make.Sprite("glow", parent ? parent : Field, sp, pos, order, c, Res.SpriteMat(true, boost));
            sr.transform.localRotation = Quaternion.Euler(0, 0, rot);
            float baseScale = size / sp.bounds.size.x;
            Clock.Run(Clock.Tween(dur, t =>
            {
                if (sr == null) return;
                float s = baseScale * Mathf.Lerp(1, grow, Ease.OutCubic(t));
                sr.transform.localScale = new Vector3(s, s, 1);
                Make.Alpha(sr, c.a * (1 - Ease.InCubic(t)));
                if (t >= 1) Object.Destroy(sr.gameObject);
            }));
            return sr;
        }

        // 고리 충격파 — from → to 크기로 퍼지며 사라진다
        public static SpriteRenderer Ring(Vector3 pos, float from, float to, float dur, Color c, float boost = 2.5f, string tex = "FX_IN_Ring_ShockWave_01",
                                          int order = 170, Transform parent = null, float squash = 1f, bool unscaled = false)
        {
            var sp = Res.FxTex(tex);
            var sr = Make.Sprite("ring", parent ? parent : Field, sp, pos, order, c, Res.SpriteMat(true, boost));
            float bs = 1f / sp.bounds.size.x;
            Clock.Run(Clock.Tween(dur, t =>
            {
                if (sr == null) return;
                float s = Mathf.Lerp(from, to, Ease.OutQuint(t)) * bs;
                sr.transform.localScale = new Vector3(s, s * squash, 1);
                Make.Alpha(sr, c.a * (1 - Ease.InCubic(t)));
                if (t >= 1) Object.Destroy(sr.gameObject);
            }, unscaled));
            return sr;
        }

        // ── 파티클 ──
        public static Material TexMat(Texture tex, bool additive, float boost)
        {
            string key = tex.name + (additive ? "+" : "a") + boost.ToString("F1");
            if (texMats.TryGetValue(key, out var m)) return m;
            m = new Material(Res.SpriteMat(additive, boost));
            m.mainTexture = tex;
            texMats[key] = m;
            return m;
        }

        public class BurstOpt
        {
            public string Tex = "FX_IN_Spark";
            public bool UiTex;
            public int Count = 12;
            public Vector2 Speed = new Vector2(4, 10);
            public Vector2 Life = new Vector2(0.25f, 0.5f);
            public Vector2 Size = new Vector2(0.1f, 0.25f);
            public Color C0 = Color.white, C1 = Color.white;
            public float Gravity;
            public bool Stretch;
            public float StretchK = 0.05f;
            public int Order = 175;
            public bool Additive = true;
            public float Boost = 2f;
            public float Angle;            // 퍼지는 방향(도)
            public float Spread = 360f;
            public float Drag = 0f;
            public bool Spin;
            public float Radius;           // 처음 자리 흩뿌림
            public float ShrinkTo = 0.1f;
            public Transform Parent;
        }

        public static ParticleSystem Burst(Vector3 pos, BurstOpt o)
        {
            int count = LowSpecFx.Count(o.Count);   // 저사양 모드면 절반(BurstOpt 는 되쓰일 수 있어 고치지 않는다)
            var go = new GameObject("burst");
            go.SetActive(false);
            go.transform.SetParent(o.Parent ? o.Parent : Field, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = o.Life.y;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0;
            main.maxParticles = Mathf.Max(1, count);
            main.gravityModifier = o.Gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, o.ShrinkTo));
            if (o.Drag > 0)
            {
                var lv = ps.limitVelocityOverLifetime;
                lv.enabled = true;
                lv.limit = 1000f;
                lv.drag = o.Drag;
                lv.multiplyDragByParticleSize = false;
                lv.multiplyDragByParticleVelocity = true;
            }
            if (o.Spin)
            {
                var rl = ps.rotationOverLifetime;
                rl.enabled = true;
                rl.z = new ParticleSystem.MinMaxCurve(-8f, 8f);
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            var sp = o.UiTex ? Res.UI(o.Tex) : Res.FxTex(o.Tex);
            r.sharedMaterial = TexMat(sp != null ? sp.texture : Texture2D.whiteTexture, o.Additive, o.Boost);
            r.renderMode = o.Stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            if (o.Stretch) { r.velocityScale = o.StretchK; r.lengthScale = 1.2f; }
            r.sortingOrder = o.Order;
            go.SetActive(true);
            ps.Play();
            for (int i = 0; i < count; i++)
            {
                float a = (o.Angle + Random.Range(-o.Spread / 2, o.Spread / 2)) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                var ep = new ParticleSystem.EmitParams
                {
                    position = (Vector3)(Random.insideUnitCircle * o.Radius),
                    applyShapeToPosition = false,
                    velocity = dir * Random.Range(o.Speed.x, o.Speed.y),
                    startLifetime = Random.Range(o.Life.x, o.Life.y),
                    startSize = Random.Range(o.Size.x, o.Size.y),
                    startColor = Color.Lerp(o.C0, o.C1, Random.value),
                    rotation = o.Stretch ? 0 : Random.Range(0f, 360f),
                };
                ps.Emit(ep, 1);
            }
            Object.Destroy(go, o.Life.y + 1.5f);
            return ps;
        }

        // ── 피해 숫자 ──
        public static void Number(Vector3 pos, int value, bool crit, int hitIndex, Color? tint = null, bool hurt = false, int blocked = 0, int order = 460)
        {
            var root = Make.Node("dmg", Field, pos + new Vector3(Random.Range(-0.25f, 0.25f), hitIndex * 0.32f, 0));
            float size = crit ? 0.85f : 0.62f;
            if (hurt) size = 0.6f;
            string s = value.ToString("N0");
            if (hurt && value == 0 && blocked > 0) { s = "<size=60%>막음</size> " + blocked; blocked = 0; tint = new Color(0.6f, 0.85f, 1f); }
            var t = Make.Text("n", root, s, Vector3.zero, size, order + hitIndex, Color.white);
            t.fontStyle = FontStyles.Normal;
            t.characterSpacing = -4;
            Make.Shadow(t, new Color(0, 0, 0, 0.55f), 0.35f, 0.7f, -0.9f);     // 밝은 이펙트 위에서도 숫자가 떠 보이게
            if (crit)
            {
                t.colorGradient = new VertexGradient(new Color(1f, 1f, 0.75f), new Color(1f, 1f, 0.75f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.55f, 0.1f));
                Make.Outline(t, 0.22f, new Color(0.35f, 0.05f, 0.02f));
                Make.Glow(t, Color.white, 1.12f);
                if (hitIndex == 0)
                {
                var label = Make.Text("crit", root, "CRITICAL", new Vector3(0, size * 0.85f, 0), 0.26f, order + 1 + hitIndex, new Color(1f, 0.9f, 0.5f));
                Make.Outline(label, 0.3f, new Color(0.3f, 0.05f, 0f));
                label.characterSpacing = 6;
                }
            }
            else if (hurt)
            {
                t.color = tint ?? new Color(1f, 0.42f, 0.4f);
                Make.Outline(t, 0.24f, new Color(0.2f, 0f, 0.02f));
            }
            else
            {
                t.color = tint ?? Color.white;
                Make.Outline(t, 0.24f, new Color(0.08f, 0.06f, 0.12f));
            }
            if (blocked > 0)
            {
                var b = Make.Text("blk", root, "<size=70%>막음</size> " + blocked, new Vector3(0, -size * 0.75f, 0), 0.32f, order - 1, new Color(0.6f, 0.85f, 1f));
                Make.Outline(b, 0.25f, new Color(0, 0.05f, 0.15f));
            }
            var p = root.gameObject.AddComponent<Popup>();
            p.Vel = new Vector2(Random.Range(-0.7f, 0.7f) * (crit ? 0.6f : 1f), crit ? 2.6f : 3.4f);
            p.Gravity = crit ? -4f : -8f;
            p.Life = crit ? 1.25f : 0.95f;
            p.Pop = crit ? 2.1f : 1.6f;
        }

        // 글 터뜨리기 — 「격파!」 · 「막음」 · 「취약」
        public static TextMeshPro Word(Vector3 pos, string text, float size, Color face, Color outline, float life = 0.9f, float boost = 1f,
                                       Transform parent = null, int order = 470, float rise = 0.8f)
        {
            var root = Make.Node("word", parent ? parent : Field, pos);
            var t = Make.Text("w", root, text, Vector3.zero, size, order, face);
            Make.Outline(t, 0.25f, outline);
            if (boost > 1) Make.Glow(t, face, boost);
            var p = root.gameObject.AddComponent<Popup>();
            p.Vel = new Vector2(0, rise);
            p.Gravity = 0;
            p.Life = life;
            p.Pop = 1.8f;
            return t;
        }

        // ── 화면 번쩍 ──
        public static void Flash(Color c, float dur, float boost = 1f, int order = 295)
        {
            var sr = Make.Box("flash", Screen, Res.UI("white"), Vector3.zero, new Vector2(20, 12), order, c, Res.SpriteMat(true, boost));
            Clock.Run(Clock.Tween(dur, t =>
            {
                if (sr == null) return;
                Make.Alpha(sr, c.a * (1 - Ease.OutCubic(t)));
                if (t >= 1) Object.Destroy(sr.gameObject);
            }, true));
        }
    }

    // 튀어 오르는 글 — 처음에 크게 떴다가(Pop) 제 크기로, 포물선으로 날다 흐려진다
    public class Popup : MonoBehaviour
    {
        public Vector2 Vel;
        public float Gravity = -8f, Life = 1f, Pop = 1.8f;
        float t;
        TextMeshPro[] texts;

        void Start() { texts = GetComponentsInChildren<TextMeshPro>(); }

        void Update()
        {
            float dt = Time.deltaTime;
            t += dt;
            float k = t / Life;
            if (k >= 1) { Destroy(gameObject); return; }
            float s = t < 0.12f ? Mathf.Lerp(Pop, 1f, Ease.OutBack(t / 0.12f, 2.5f)) : 1f;
            transform.localScale = new Vector3(s, s, 1);
            // 처음 0.12초는 제자리에서 튄다(멈칫과 함께 읽히게), 그 뒤 날아간다
            if (t > 0.1f)
            {
                Vel.y += Gravity * dt;
                transform.localPosition += (Vector3)(Vel * dt);
            }
            float a = k < 0.65f ? 1 : 1 - (k - 0.65f) / 0.35f;
            foreach (var x in texts) if (x) x.alpha = a;
        }
    }
}
