using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Bolzena.RunUI.EditorTools
{
    /// <summary>
    /// 빌드 시각 도장 — 빌드할 때마다 Assets/Resources/RunUI/build_stamp.txt 에 UTC 시각(분)을 적는다.
    /// 플레이 기록(PlayRecord)이 「어느 빌드의 판인가」 로 함께 보낸다. Assets/Resources 는 저장소에 넣지 않는 폴더라 git 이 시끄럽지 않다.
    /// </summary>
    sealed class BuildStamp : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            const string path = "Assets/Resources/RunUI/build_stamp.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm'Z'"));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            // 게임 버전 = 패치노트 맨 위 판(Runtime/Resources/RunUI/patchnotes.txt) — 플레이어 설정 bundleVersion 도 그것으로 맞춘다
            //   (런타임 PatchNotes.All 은 한 번 읽고 쥐고 있어서, 에디터에서 글만 고친 경우를 위해 여기서는 새로 읽는다)
            var notes = Bolzena.RunUI.PatchNotes.Parse(UnityEngine.Resources.Load<UnityEngine.TextAsset>("RunUI/patchnotes")?.text);
            var ver = notes.Count > 0 ? notes[0].Version : null;
            if (!string.IsNullOrEmpty(ver) && PlayerSettings.bundleVersion != ver)
            {
                UnityEngine.Debug.Log($"[Build] 버전 {PlayerSettings.bundleVersion} → {ver}(패치노트)");
                PlayerSettings.bundleVersion = ver;
            }
        }
    }
}
