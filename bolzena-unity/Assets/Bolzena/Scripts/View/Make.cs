using TMPro;
using UnityEngine;

namespace Bolzena.View
{
    // 장면 조각을 코드로 만드는 손 — 스프라이트 · 글 · 빈 마디
    public static class Make
    {
        public static Transform Node(string name, Transform parent, Vector3 pos = default)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        public static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Vector3 pos, int order, Color? color = null, Material mat = null)
        {
            var t = Node(name, parent, pos);
            var sr = t.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            sr.sharedMaterial = mat != null ? mat : Res.SpriteMat();
            if (color.HasValue) sr.color = color.Value;
            return sr;
        }

        // 크기를 월드 단위로 맞춘 스프라이트(가로 · 세로)
        public static SpriteRenderer Box(string name, Transform parent, Sprite sprite, Vector3 pos, Vector2 size, int order, Color? color = null, Material mat = null)
        {
            var sr = Sprite(name, parent, sprite, pos, order, color, mat);
            Fit(sr, size);
            return sr;
        }

        public static void Fit(SpriteRenderer sr, Vector2 size)
        {
            if (sr.sprite == null) return;
            if (sr.drawMode != SpriteDrawMode.Simple) { sr.size = size; return; }
            var b = sr.sprite.bounds.size;
            sr.transform.localScale = new Vector3(size.x / b.x, size.y / b.y, 1);
        }

        // 9칸 스프라이트(가장자리 그대로 늘리기)
        public static SpriteRenderer Sliced(string name, Transform parent, Sprite sprite, Vector3 pos, Vector2 size, int order, Color? color = null, Material mat = null)
        {
            var sr = Sprite(name, parent, sprite, pos, order, color, mat);
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = size;
            return sr;
        }

        // 글 — size 는 대략 월드 단위 글자 높이
        public static TextMeshPro Text(string name, Transform parent, string text, Vector3 pos, float size, int order, Color? color = null,
                                       TextAlignmentOptions align = TextAlignmentOptions.Center, float width = 10f)
        {
            var t = Node(name, parent, pos);
            var tmp = t.gameObject.AddComponent<TextMeshPro>();
            tmp.font = Res.Font;
            Bolzena.RunUI.CardTerms.Prepare(tmp);   // 글 속 카드 아이콘(<sprite>) — 모든 싸움터 글에 걸어 둔다(갈래 창 · 확대 · 더미 · 툴팁). 애셋이 없으면 CardTerms 가 「▣」로 쓴다
            tmp.text = text;
            tmp.fontSize = size * 10f;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.rectTransform.sizeDelta = new Vector2(width, size * 2f);
            tmp.sortingOrder = order;
            tmp.color = color ?? Color.white;
            tmp.richText = true;
            return tmp;
        }

        public static void Outline(TextMeshPro t, float width, Color c)
        {
            // TMP 의 outlineWidth 세터는 글마다 재질을 복제한다 — 꾸밈별 재질 하나를 나눠 쓴다
            Derive(t, $"ol{width}{(Color32)c}", m =>
            {
                m.EnableKeyword("OUTLINE_ON");
                m.SetFloat(ShaderUtilities.ID_OutlineWidth, width);
                m.SetColor(ShaderUtilities.ID_OutlineColor, c);
            });
        }

        // 글 재질은 꾸밈마다 하나만 만들어 나눠 쓴다 — 글마다 fontMaterial 을 복제하면 피해 숫자가 쏟아질 때 멈칫했다
        static readonly System.Collections.Generic.Dictionary<string, Material> textMats = new System.Collections.Generic.Dictionary<string, Material>();

        static Material Derive(TextMeshPro t, string key, System.Action<Material> setup)
        {
            var src = t.fontSharedMaterial;
            key = src.GetHashCode() + "|" + key;
            if (!textMats.TryGetValue(key, out var m))
            {
                m = new Material(src);
                setup(m);
                textMats[key] = m;
            }
            t.fontSharedMaterial = m;
            return m;
        }

        // 아래 그림자(밑깔림)
        public static void Shadow(TextMeshPro t, Color c, float soft = 0.2f, float ox = 0.6f, float oy = -0.6f)
        {
            Derive(t, $"sh{c}{soft}{ox}{oy}", m =>
            {
                m.EnableKeyword("UNDERLAY_ON");
                m.SetColor(ShaderUtilities.ID_UnderlayColor, c);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, ox);
                m.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, oy);
                m.SetFloat(ShaderUtilities.ID_UnderlaySoftness, soft);
            });
        }

        // 글자 면을 HDR 로 — 블룸이 먹는다
        public static void Glow(TextMeshPro t, Color face, float boost)
        {
            Derive(t, $"gl{face}{boost}", m => m.SetColor(ShaderUtilities.ID_FaceColor, face * boost));
        }

        // 화면 크기 사각형(uv 0~1) — 집중선 · 빛줄기 셰이더용
        public static MeshRenderer Quad(string name, Transform parent, Vector3 pos, Vector2 size, Material mat, int order)
        {
            var t = Node(name, parent, pos);
            var mf = t.gameObject.AddComponent<MeshFilter>();
            var mr = t.gameObject.AddComponent<MeshRenderer>();
            var m = new Mesh();
            float w = size.x / 2, h = size.y / 2;
            m.vertices = new[] { new Vector3(-w, -h), new Vector3(w, -h), new Vector3(-w, h), new Vector3(w, h) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            m.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            m.RecalculateBounds();
            m.name = "quad " + name;
            mf.sharedMesh = m;
            Own(t.gameObject, m);                   // 조각과 함께 지운다(전투마다 새로 만든 메시가 남지 않게)
            mr.sharedMaterial = mat;
            mr.sortingOrder = order;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return mr;
        }

        /// <summary>코드로 만든 자산(재질 · 메시 …)을 이 오브젝트가 지워질 때 같이 지운다.</summary>
        public static T Own<T>(GameObject go, T asset) where T : Object
        {
            if (go == null || asset == null) return asset;
            var o = go.GetComponent<Owned>();
            if (o == null) o = go.AddComponent<Owned>();
            o.List.Add(asset);
            return asset;
        }

        public static void Alpha(SpriteRenderer sr, float a)
        {
            var c = sr.color;
            c.a = a;
            sr.color = c;
        }

        public static void Alpha(TextMeshPro t, float a) => t.alpha = a;
    }

    // 코드로 만든 자산의 주인 — 오브젝트가 지워질 때 같이 지운다(Make.Own)
    public class Owned : MonoBehaviour
    {
        public readonly System.Collections.Generic.List<Object> List = new System.Collections.Generic.List<Object>();
        void OnDestroy() { foreach (var o in List) if (o != null) Destroy(o); List.Clear(); }
    }
}
