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
        }
    }
}
