using System;
using System.IO;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 시험이 쓰는 폴더 찾기 — PC 마다 다른 드라이브 절대 경로를 쓰지 않는다(2026-11-03, 10주차).
    /// 지금 폴더에서 위로 올라가며 이름이 맞는 폴더를 찾는다. 유니티(지금 폴더 = bolzena-unity) · dotnet test(bin 아래) · 새 clone 모두 같은 코드로 찾는다.
    /// 환경 변수가 있으면 그것이 먼저다.
    /// </summary>
    public static class TestPaths
    {
        /// <summary>start 에서 위로 올라가며 rel(예: 「bolzena-content-v2」)이 있는 첫 폴더. 없으면 null.</summary>
        public static string FindUp(string rel, string start = null)
        {
            var d = new DirectoryInfo(start ?? Directory.GetCurrentDirectory());
            for (; d != null; d = d.Parent)
            {
                string p = Path.Combine(d.FullName, rel);
                if (Directory.Exists(p)) return p;
            }
            return null;
        }

        static string Env(string name) => Environment.GetEnvironmentVariable(name) is string v && v.Length > 0 ? v : null;

        /// <summary>콘텐츠 폴더 — BZ_CONTENT, 없으면 위로 올라가며 bolzena-content-v2. 없으면 null.</summary>
        public static string Content => Env("BZ_CONTENT") is string e && Directory.Exists(e) ? e : FindUp("bolzena-content-v2");

        /// <summary>콘텐츠 폴더. 없으면 시험을 건너뛴다.</summary>
        public static string ContentOrIgnore()
        {
            string dir = Content;
            if (dir == null || !Directory.Exists(dir)) Assert.Ignore("bolzena-content-v2 가 없다(BZ_CONTENT 로 줄 수 있다)");
            return dir;
        }

        /// <summary>
        /// 저장소 밖 시험 자료 — 환경 변수 env, 없으면 위로 올라가며 rel. 없으면 null(부르는 쪽이 건너뛴다).
        /// 예: 교차 테이프(bolzena-core-test/…) · 빠진 사도 백업(_backup/…). CI 에는 없어서 그 시험은 건너뜀으로 센다.
        /// </summary>
        public static string Outside(string env, string rel) => Env(env) ?? FindUp(rel);

        /// <summary>시험 출력 — 유니티 콘솔 대신 NUnit 출력(유니티 · dotnet test 둘 다 결과에 남는다).</summary>
        public static void Log(string msg) => TestContext.Progress.WriteLine(msg);
    }
}
