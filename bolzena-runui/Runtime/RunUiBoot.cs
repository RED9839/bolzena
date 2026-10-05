using UnityEngine;

namespace Bolzena.RunUI
{
    // 장면에 하나 — 판 화면을 띄운다. -demo 면 자동 데모(Demo.cs).
    public class RunUiBoot : MonoBehaviour
    {
        void Awake()
        {
            DisplayOptions.LoadAndApply();   // 저장된 창 모드 · 해상도 · 프레임(명령줄 -screen-* 가 있으면 해상도는 명령줄을 따른다)
            bool demo = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-demo") >= 0;
            if (demo) Demo.Active = true;
            var f = Flow.Boot();
            if (demo) Demo.Attach(f);
        }
    }
}
