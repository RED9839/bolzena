using System.Collections;
using System.IO;
using UnityEngine;

namespace Bolzena.Demo
{
    // 화면 캡처 — 한 장(PNG, 원래 크기)과 연속(JPG, 800×450 — GIF 재료)
    public class Capture : MonoBehaviour
    {
        public string Dir;
        public bool Off;                       // 재기 모드 — 저장이 그 자체로 멈칫이라 끈다
        string seq;
        int seqN, every = 2;
        RenderTexture small;
        Texture2D readback;

        // -supersize N — 정지 캡처를 N 배 해상도로 다시 그려 찍는다(창보다 큰 4K · WQHD 의 글 · 그림 선명도 확인)
        public static readonly int SuperSize = ParseSuper();
        static int ParseSuper()
        {
            var a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, "-supersize");
            return i >= 0 && i < a.Length - 1 && int.TryParse(a[i + 1], out var n) ? Mathf.Clamp(n, 1, 4) : 1;
        }

        public void Still(string name) { if (!Off) StartCoroutine(StillCo(name)); }

        IEnumerator StillCo(string name)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture(SuperSize);
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path.Combine(Dir, name + ".png"), tex.EncodeToPNG());
            Destroy(tex);
            Debug.Log("[Capture] " + name);
        }

        public void BeginSeq(string name, int everyFrames = 2)
        {
            if (Off) return;
            seq = name;
            seqN = 0;
            every = everyFrames;
            Directory.CreateDirectory(Path.Combine(Dir, "frames", name));
        }

        public void EndSeq() => seq = null;

        void LateUpdate()
        {
            if (seq != null && Time.frameCount % every == 0) StartCoroutine(FrameCo(seq, seqN++));
        }

        IEnumerator FrameCo(string name, int n)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (small == null) small = new RenderTexture(800, 450, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            if (readback == null) readback = new Texture2D(800, 450, TextureFormat.RGB24, false);
            Graphics.Blit(tex, small);
            var prev = RenderTexture.active;
            RenderTexture.active = small;
            readback.ReadPixels(new Rect(0, 0, 800, 450), 0, 0);
            readback.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(Dir, "frames", name, n.ToString("D4") + ".jpg"), readback.EncodeToJPG(88));
            Destroy(tex);
        }
    }
}
