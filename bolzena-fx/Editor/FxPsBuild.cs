using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Bolzena.Fx.EditorTools
{
    // 원작 ParticleSystem 그대로 다시 조립(2026-10-16 사용자 「4번 바꿔 봐」 — 우리 시뮬레이터 대신 유니티 기본 파티클).
    //   추출기(FX_PSRAW=1)가 넘긴 원작 직렬화 값(ps · rd)을 SerializedObject 로 새 ParticleSystem 에 그대로 쓴다. 이름이 원작 직렬화 이름과 같아 거의 통째로 맞는다.
    //   이미터마다 자식 하나(세계 변환 xf 를 그대로), 메시 입자는 원작 메시(rmesh) 에셋, 재질은 셰이더 그래프 재현(e.Sg) 아니면 단순 입자(Bolzena/FxSimple).
    //   프리팹은 Resources/<Root>/Ps/<이름>.prefab — FxEffect.Ps 가 가리킨다
    public static class FxPsBuild
    {
        public static int Built, Failed;
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<(Texture, bool), Material> simple = new Dictionary<(Texture, bool), Material>();

        static WrapMode CurveWrap(float v) => v == 1f ? WrapMode.Loop : v == 0f ? WrapMode.PingPong : WrapMode.ClampForever;
        public static void Reset() { Built = Failed = 0; meshes.Clear(); simple.Clear(); }

        public static GameObject Build(string outDir, string name, List<object> ems, List<FxEmitter> parsed)
        {
            if (ems == null || ems.Count == 0 || !(ems[0] is Dictionary<string, object> e0) || !e0.ContainsKey("ps")) return null;
            var root = new GameObject(name);
            try
            {
                var byId = new Dictionary<long, ParticleSystem>();
                var subs = new List<(ParticleSystem ps, List<object> list)>();
                for (int i = 0; i < ems.Count; i++)
                {
                    var d = (Dictionary<string, object>)ems[i];
                    if (!d.ContainsKey("ps")) continue;
                    var fe = i < parsed.Count ? parsed[i] : null;
                    var go = new GameObject(S(d, "n") ?? ("em" + i));
                    go.transform.SetParent(root.transform, false);
                    ApplyXf(go.transform, d.TryGetValue("xf", out var xo) ? xo as Dictionary<string, object> : null);
                    var ps = go.AddComponent<ParticleSystem>();
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var so = new SerializedObject(ps);
                    var psd = (Dictionary<string, object>)d["ps"];
                    foreach (var kv in psd) Set(so.FindProperty(kv.Key), kv.Value);
                    // 원작에서 꺼진(그래서 빠진) 모듈은 끈다 — 새 ParticleSystem 은 Shape 등이 켜진 채로 생긴다
                    var it = so.GetIterator(); it.NextVisible(true);
                    var mods = new List<string>();
                    do { if (it.name.EndsWith("Module")) mods.Add(it.name); } while (it.NextVisible(false));
                    foreach (var m in mods)
                        if (!psd.ContainsKey(m)) { var en = so.FindProperty(m + ".enabled"); if (en != null) en.boolValue = false; }
                    // 스프라이트 칸(원작 아틀라스 스프라이트) → 우리 그림은 한 장(또는 가로 띠) — 격자로
                    var uvMode = so.FindProperty("UVModule.mode");
                    if (uvMode != null && uvMode.intValue == 1)
                    {
                        int tx = 1;
                        if (d.TryGetValue("sheet", out var sho) && sho is Dictionary<string, object> sh) tx = (int)F(sh, "tx", 1);
                        if (tx <= 1) so.FindProperty("UVModule.enabled").boolValue = false;
                        else { uvMode.intValue = 0; so.FindProperty("UVModule.tilesX").intValue = tx; so.FindProperty("UVModule.tilesY").intValue = 1; }
                    }
                    var lp = so.FindProperty("looping"); var pa = so.FindProperty("playOnAwake");
                    if (pa != null) pa.boolValue = false;
                    // 크기 방식 — 원작 Local · Shape 는 부모 크기를 안 받아 우리 뿌리 배율(UnitWorld)이 안 먹는다(3.3배로 커졌다). Hierarchy 로 바꾸고 같은 크기가 되게
                    //   Local: 자기 크기만 = 원작 세계 크기(xf.s, 원작 뿌리는 1배) · Shape: 입자는 크기를 안 받고 모양만 → 크기를 모양 쪽으로 옮긴다
                    var scm = so.FindProperty("scalingMode");
                    if (scm != null && scm.intValue == 2)
                    {
                        var shs = so.FindProperty("ShapeModule.m_Scale");
                        var ls = go.transform.localScale;
                        if (shs != null) shs.vector3Value = Vector3.Scale(shs.vector3Value, ls);
                        go.transform.localScale = Vector3.one;
                    }
                    if (scm != null) scm.intValue = 0;
                    // 고리 버퍼(PauseUntilReplaced) — 수명이 다한 입자가 그 자리에 멈춰 남는다. 원작은 게임이 장을 정해진 때에 거둬 안 보이지만 우리는 끝을 입자 수로 알아서 영원히 남았다(티그 스킬 파란 판). 반복 안 하는 것은 끈다
                    var rb = so.FindProperty("ringBufferMode");
                    if (rb != null && lp != null && !lp.boolValue) rb.intValue = 0;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var rd = go.GetComponent<ParticleSystemRenderer>();
                    var rso = new SerializedObject(rd);
                    if (d.TryGetValue("rd", out var rdo) && rdo is Dictionary<string, object> rdd)
                        foreach (var kv in rdd)
                        {
                            if (kv.Key == "m_SortingLayerID" || kv.Key == "m_SortingLayer" || kv.Key == "m_RenderingLayerMask") continue;
                            Set(rso.FindProperty(kv.Key), kv.Value);
                        }
                    // 스트림 목록에 Custom1 · 2(34 · 38)가 있으면 사용자 스트림을 켠다 — 원작 값은 꺼짐으로 적혀 있지만 그대로 두면 셰이더에 Custom 이 안 들어가
                    //   밝기 배율(Custom1.w) · 디졸브가 0 이 되어 장이 흐리거나 안 보였다(10-17 우로스 금빛 뱀). (시험 — BOLZENA_PS_STREAMS=0 이면 원작 값 그대로)
                    var vsp = rso.FindProperty("m_VertexStreams"); var ucs = rso.FindProperty("m_UseCustomVertexStreams");
                    if (ucs != null && vsp != null && System.Environment.GetEnvironmentVariable("BOLZENA_PS_STREAMS") != "0")
                    {
                        bool custom = false;
                        if (vsp.isArray) for (int vi = 0; vi < vsp.arraySize; vi++) { int sv = vsp.GetArrayElementAtIndex(vi).intValue; if (sv >= 34 && sv <= 41) custom = true; }
                        if (custom) ucs.boolValue = true;
                    }
                    rso.ApplyModifiedPropertiesWithoutUndo();
                    // 원작 정렬 레이어(순서 값) — 2 이하(바닥 균열 · 바닥 빛)는 캐릭터 뒤, 3 이상은 앞. 레이어 이름은 우리 프로젝트에 없어 순서 값만
                    //   그리는 순서에 −1000 으로 표시해 둔다 → FxRun.InitPs 가 사도 뒤로(10-17 사용자 「임팩트가 사도 뒤로 그려져야 하는데 앞으로」)
                    if (d.TryGetValue("rd", out var rdo2) && rdo2 is Dictionary<string, object> rdd2 && rdd2.TryGetValue("m_SortingLayer", out var slv) && Convert.ToInt32(slv) <= 2)
                        rd.sortingOrder -= 1000;
                    rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rd.receiveShadows = false;
                    if (d.TryGetValue("rmesh", out var mo) && mo is Dictionary<string, object> md) rd.mesh = MeshOf(outDir, md);
                    rd.sharedMaterial = fe != null && fe.Sg != null ? fe.Sg : SimpleMat(outDir, fe);
                    if (d.TryGetValue("ps_id", out var pid)) byId[Convert.ToInt64(pid)] = ps;
                    if (d.TryGetValue("ps_sub", out var sbo) && sbo is List<object> sl && sl.Count > 0) subs.Add((ps, sl));
                }
                // 하위 이미터 — 원작 참조(path_id)를 같은 프리팹의 이미터로
                foreach (var (ps, list) in subs)
                {
                    var sm = ps.subEmitters;
                    sm.enabled = true;
                    while (sm.subEmittersCount > 0) sm.RemoveSubEmitter(0);
                    foreach (var o in list)
                    {
                        var a = (List<object>)o;
                        long id = Convert.ToInt64(a[0]);
                        if (!byId.TryGetValue(id, out var child) || child == ps) continue;
                        sm.AddSubEmitter(child, (ParticleSystemSubEmitterType)Convert.ToInt32(a[1]), (ParticleSystemSubEmitterProperties)Convert.ToInt32(a[2]), Convert.ToSingle(a[3]));
                    }
                }
                Directory.CreateDirectory(outDir + "/Ps");
                var path = outDir + "/Ps/" + name + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Built++;
                return prefab;
            }
            catch (Exception ex)
            {
                Failed++;
                Debug.LogWarning("[FxPsBuild] " + name + ": " + ex.Message);
                return null;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        // 세계 변환 — 행렬(크기 포함, 행 우선)을 위치 · 돌림 · 크기로
        static void ApplyXf(Transform t, Dictionary<string, object> xf)
        {
            if (xf == null) return;
            var p = Nums(xf, "p"); var M = Nums(xf, "M");
            if (p.Count >= 3) t.localPosition = new Vector3(p[0], p[1], p[2]);
            if (M.Count == 9)
            {
                var c0 = new Vector3(M[0], M[3], M[6]); var c1 = new Vector3(M[1], M[4], M[7]); var c2 = new Vector3(M[2], M[5], M[8]);
                float s0 = c0.magnitude, s1 = c1.magnitude, s2 = c2.magnitude;
                if (Vector3.Dot(Vector3.Cross(c0, c1), c2) < 0) s0 = -s0;
                // 원작은 부모 z 크기 0 으로 입체 메시를 납작하게 누르기도 한다(비비(신성) 레이저 4줄기) — 그때도 회전은 나머지 두 축으로 살린다(전엔 회전을 버려 빛줄기가 옆으로 누웠다, 10-16)
                if (s2 <= 1e-6f && s0 != 0 && s1 > 1e-6f) { c2 = Vector3.Cross(c0, c1).normalized * Mathf.Sign(s0); s2 = 1e-6f; t.localRotation = Quaternion.LookRotation(c2, c1 / s1); }
                else if (s1 > 1e-6f && s2 > 1e-6f) t.localRotation = Quaternion.LookRotation(c2 / s2, c1 / s1);
                t.localScale = new Vector3(s0 == 0 ? 1e-4f : s0, s1 == 0 ? 1e-4f : s1, s2 == 0 ? 1e-4f : s2);
            }
        }

        static Mesh MeshOf(string outDir, Dictionary<string, object> md)
        {
            var v = Nums(md, "v"); var u = Nums(md, "u"); var tr = Nums(md, "t");
            var key = Hash128.Compute(string.Join(",", v.Take(30)) + "|" + v.Count + "|" + tr.Count).ToString();
            if (meshes.TryGetValue(key, out var got)) return got;
            Directory.CreateDirectory(outDir + "/PsMesh");
            var path = outDir + "/PsMesh/" + key + ".asset";
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m == null)
            {
                m = new Mesh();
                int n = v.Count / 3;
                var vs = new Vector3[n]; var us = new Vector2[n];
                for (int i = 0; i < n; i++) { vs[i] = new Vector3(v[3 * i], v[3 * i + 1], v[3 * i + 2]); if (2 * i + 1 < u.Count) us[i] = new Vector2(u[2 * i], u[2 * i + 1]); }
                m.vertices = vs; m.uv = us;
                m.triangles = tr.Select(x => Mathf.Clamp((int)x, 0, n - 1)).ToArray();
                m.RecalculateNormals(); m.RecalculateBounds();
                AssetDatabase.CreateAsset(m, path);
            }
            meshes[key] = m;
            return m;
        }

        static Material SimpleMat(string outDir, FxEmitter fe)
        {
            var tex = fe != null ? fe.Tex : null;
            bool add = fe != null && fe.Add;
            if (simple.TryGetValue((tex, add), out var got)) return got;
            var sh = Shader.Find("Bolzena/FxSimple");
            Directory.CreateDirectory(outDir + "/PsMat");
            var path = outDir + "/PsMat/" + (tex ? tex.name : "none") + (add ? "_add" : "_ab") + "_" + Hash128.Compute(tex ? AssetDatabase.GetAssetPath(tex) : "none") + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
            m.mainTexture = tex;
            m.SetFloat("_Add", add ? 1 : 0);
            EditorUtility.SetDirty(m);
            simple[(tex, add)] = m;
            return m;
        }

        // ── 직렬화 값 쓰기 ──
        static readonly PropertyInfo gradProp = typeof(SerializedProperty).GetProperty("gradientValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        static void Set(SerializedProperty p, object v)
        {
            if (p == null || v == null) return;
            try
            {
                switch (p.propertyType)
                {
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                    case SerializedPropertyType.Enum:
                    case SerializedPropertyType.Character:
                        if (IsNum(v)) p.intValue = Convert.ToInt32(v); return;
                    case SerializedPropertyType.Boolean:
                        p.boolValue = v is bool b ? b : IsNum(v) && Convert.ToDouble(v) != 0; return;
                    case SerializedPropertyType.Float:
                        if (IsNum(v)) p.floatValue = Convert.ToSingle(v); return;
                    case SerializedPropertyType.Color:
                        if (v is Dictionary<string, object> c) p.colorValue = new Color(F(c, "r", 1), F(c, "g", 1), F(c, "b", 1), F(c, "a", 1)); return;
                    case SerializedPropertyType.Vector2:
                        if (v is Dictionary<string, object> v2) p.vector2Value = new Vector2(F(v2, "x", 0), F(v2, "y", 0)); return;
                    case SerializedPropertyType.Vector3:
                        if (v is Dictionary<string, object> v3) p.vector3Value = new Vector3(F(v3, "x", 0), F(v3, "y", 0), F(v3, "z", 0)); return;
                    case SerializedPropertyType.Vector4:
                        if (v is Dictionary<string, object> v4) p.vector4Value = new Vector4(F(v4, "x", 0), F(v4, "y", 0), F(v4, "z", 0), F(v4, "w", 0)); return;
                    case SerializedPropertyType.Quaternion:
                        if (v is Dictionary<string, object> q) p.quaternionValue = new Quaternion(F(q, "x", 0), F(q, "y", 0), F(q, "z", 0), F(q, "w", 1)); return;
                    case SerializedPropertyType.AnimationCurve:
                        if (v is Dictionary<string, object> ac) p.animationCurveValue = Curve(ac); return;
                    case SerializedPropertyType.Gradient:
                        if (v is Dictionary<string, object> gd && gradProp != null) gradProp.SetValue(p, Grad(gd)); return;
                    case SerializedPropertyType.ObjectReference:
                    case SerializedPropertyType.ExposedReference:
                    case SerializedPropertyType.ManagedReference:
                        return;
                }
                if (p.isArray && p.propertyType != SerializedPropertyType.String)
                {
                    if (!(v is List<object> l)) return;
                    p.arraySize = l.Count;
                    for (int i = 0; i < l.Count; i++) Set(p.GetArrayElementAtIndex(i), l[i]);
                    return;
                }
                if (v is Dictionary<string, object> dd)
                    foreach (var kv in dd) Set(p.FindPropertyRelative(kv.Key), kv.Value);
            }
            catch (Exception) { }
        }

        static AnimationCurve Curve(Dictionary<string, object> d)
        {
            var c = new AnimationCurve();
            if (d.TryGetValue("m_Curve", out var ko) && ko is List<object> keys)
                foreach (var k in keys.OfType<Dictionary<string, object>>())
                {
                    var kf = new Keyframe(F(k, "time", 0), F(k, "value", 0), F(k, "inSlope", 0), F(k, "outSlope", 0), F(k, "inWeight", 0.333f), F(k, "outWeight", 0.333f));
                    kf.weightedMode = (WeightedMode)(int)F(k, "weightedMode", 0);
                    c.AddKey(kf);
                }
            // 원작 직렬화 값(m_Pre/PostInfinity)은 0 = PingPong · 1 = Loop · 2 = 끝 고정 — 유니티 WrapMode 정수와 다르다. 전엔 2 를 그대로 (WrapMode)2 = Loop 로 넣어
            //   마지막 키가 1 앞인 곡선(크기 · 속도 · 디졸브)이 수명 끝까지 되풀이됐다(10-17 재검토: 실비아 분수가 커지며 떠오름 · 우로스 뱀이 화면 밖으로 · 리뉴아 레이저 굵은 막대)
            c.preWrapMode = CurveWrap(F(d, "m_PreInfinity", 2));
            c.postWrapMode = CurveWrap(F(d, "m_PostInfinity", 2));
            return c;
        }

        static Gradient Grad(Dictionary<string, object> d)
        {
            int nc = Mathf.Clamp((int)F(d, "m_NumColorKeys", 2), 1, 8), na = Mathf.Clamp((int)F(d, "m_NumAlphaKeys", 2), 1, 8);
            var ck = new GradientColorKey[nc]; var ak = new GradientAlphaKey[na];
            for (int i = 0; i < Math.Max(nc, na); i++)
            {
                var col = d.TryGetValue("key" + i, out var co) && co is Dictionary<string, object> cd ? new Color(F(cd, "r", 1), F(cd, "g", 1), F(cd, "b", 1), F(cd, "a", 1)) : Color.white;
                if (i < nc) ck[i] = new GradientColorKey(col, F(d, "ctime" + i, 0) / 65535f);
                if (i < na) ak[i] = new GradientAlphaKey(col.a, F(d, "atime" + i, 0) / 65535f);
            }
            var g = new Gradient();
            g.SetKeys(ck, ak);
            g.mode = (GradientMode)(int)F(d, "m_Mode", 0);
            return g;
        }

        static bool IsNum(object v) => v is double || v is long || v is int || v is float;
        static float F(Dictionary<string, object> d, string k, float def) => d.TryGetValue(k, out var v) && IsNum(v) ? Convert.ToSingle(v) : def;
        static string S(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var v) ? v as string : null;
        static List<float> Nums(Dictionary<string, object> d, string k) => d.TryGetValue(k, out var o) && o is List<object> l ? l.Select(x => IsNum(x) ? Convert.ToSingle(x) : 0f).ToList() : new List<float>();
    }
}
