using System.Collections;
using System.IO;
using Spine.Unity;
using UnityEngine;

namespace Bolzena.Demo
{
    // 스파인 한 장 찍기(실행 파일) — -spineshot <표.tsv>. 줄마다 「폴더 스킨 동작 초 출력.png [뒤집기 0/1]」(탭).
    // 투명 배경 1024² PNG(미리 곱한 알파)로 찍고 끝낸다. 에디터 batchmode 는 URP 카메라가 그리지 않아 실행 파일에서 찍는다.
    // 성격 아이콘 만들기 · 성격 모습(틴트) 비교 · 새 몬스터 공격 몸짓 보기에 쓴다.
    public class SpineShotRun : MonoBehaviour
    {
        const int PX = 1024;
        const int LAYER = 31;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var a = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(a, "-spineshot");
            if (i < 0 || i >= a.Length - 1) return;
            var go = new GameObject("SpineShotRun");
            DontDestroyOnLoad(go);
            go.AddComponent<SpineShotRun>().jobs = a[i + 1];
        }

        string jobs;

        IEnumerator Start()
        {
            AudioListener.volume = 0f;
            var camGo = new GameObject("shotCam");
            DontDestroyOnLoad(camGo);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.cullingMask = 1 << LAYER;
            cam.depth = 100;
            cam.transform.position = new Vector3(1000, 0, -10);
            var rt = new RenderTexture(PX, PX, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            var tex = new Texture2D(PX, PX, TextureFormat.RGBA32, false);
            int done = 0;
            foreach (var raw in File.ReadAllLines(jobs))
            {
                if (string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                var q = raw.Split('\t');
                string folder = q[0], skin = q[1], anim = q[2], outp = q[4];
                float t = float.Parse(q[3], System.Globalization.CultureInfo.InvariantCulture);
                bool flip = q.Length > 5 && q[5] == "1";
                var asset = Resources.Load<SkeletonDataAsset>("Spine/" + folder + "/" + folder + "_SkeletonData");
                if (asset == null) { foreach (var x in Resources.LoadAll<SkeletonDataAsset>("Spine/" + folder)) { asset = x; break; } }
                if (asset == null) { Debug.LogWarning("[SpineShot] 스파인 없음 " + folder); continue; }
                var sa = SkeletonAnimation.NewSkeletonAnimationGameObject(asset);
                sa.gameObject.layer = LAYER;
                sa.transform.position = new Vector3(1000, 0, 0);
                var sd = asset.GetSkeletonData(true);
                if (skin != "-" && sd.FindSkin(skin) != null) { sa.Skeleton.SetSkin(skin); sa.Skeleton.SetSlotsToSetupPose(); }
                else if (skin != "-") Debug.LogWarning($"[SpineShot] {folder} 에 스킨 {skin} 없음");
                if (anim != "-" && sd.FindAnimation(anim) != null) sa.AnimationState.SetAnimation(0, anim, false).TrackTime = t;
                else if (anim != "-") Debug.LogWarning($"[SpineShot] {folder} 에 동작 {anim} 없음");
                sa.timeScale = 0;
                sa.Skeleton.ScaleX = flip ? -1 : 1;
                sa.AnimationState.Apply(sa.Skeleton);
                sa.Skeleton.UpdateWorldTransform();
                float[] buf = null;
                sa.Skeleton.GetBounds(out float bx, out float by, out float bw, out float bh, ref buf);
                float s = sa.transform.lossyScale.x;
                cam.orthographicSize = Mathf.Max(bw, bh) * s * 0.55f;
                cam.transform.position = new Vector3(1000 + (bx + bw / 2) * s * (flip ? 1 : 1), (by + bh / 2) * s, -10);
                yield return null;
                yield return new WaitForEndOfFrame();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, PX, PX), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                Directory.CreateDirectory(Path.GetDirectoryName(outp));
                File.WriteAllBytes(outp, tex.EncodeToPNG());
                Destroy(sa.gameObject);
                done++;
            }
            Debug.Log($"[SpineShot] {done}장");
            Application.Quit(0);
        }
    }
}
