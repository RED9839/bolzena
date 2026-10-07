using Spine.Unity;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 스파인 데이터 찾기 — 기본은 Resources("Spine/<폴더>"). 본 게임 웹 빌드는 스파인을 첫 로딩에서 빼 번들로 받으므로(bolzena-unity WebBundles)
    //   번들 층이 Find · Has · Pending 을 꽂는다. 꽂지 않으면(PC · 에디터 · 시험 프로젝트) 예전과 똑같이 Resources 만 본다.
    public static class SpineSource
    {
        /// <summary>번들에서 찾기 — 받아 둔 번들에 있으면 그것, 없으면 null(그다음 Resources).</summary>
        public static System.Func<string, SkeletonDataAsset> Find;
        /// <summary>번들 목록에 그 폴더가 있는가(받기 전이어도).</summary>
        public static System.Func<string, bool> Has;
        /// <summary>번들 목록에는 있지만 아직 받는 중인가 — 이때 받은 null 은 붙들어 두지 않는다(다음에 다시 찾는다).</summary>
        public static System.Func<string, bool> Pending;

        public static SkeletonDataAsset Load(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return null;
            using var _h = Hitch.Span("스파인 자산 읽기");
            var a = Find?.Invoke(folder);
            if (a != null) return a;
            var all = Resources.LoadAll<SkeletonDataAsset>("Spine/" + folder);
            return all.Length > 0 ? all[0] : null;
        }

        /// <summary>그 폴더의 스파인이 있는가 — 번들 목록이면 받기 전이어도 true(불러오지 않고 답한다).</summary>
        public static bool Exists(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return false;
            if (Has != null && Has(folder)) return true;
            return Resources.LoadAll<SkeletonDataAsset>("Spine/" + folder).Length > 0;
        }

        public static bool IsPending(string folder) => Pending != null && !string.IsNullOrEmpty(folder) && Pending(folder);
    }
}
