using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 패치노트 — Resources/RunUI/patchnotes.txt 를 읽는다(쓰는 법: Docs/패치노트.md).
    //   맨 위 판의 버전이 게임 버전이다 — 로비 표시 · 플레이 기록(PlayRecord.Ver) · 빌드의 bundleVersion(Editor/BuildStamp.cs)이 모두 이것 하나를 따른다.
    //   「업데이트 내용」 창(Flow.PatchNotes.cs)은 본 버전(PlayerPrefs bz.patch.seen)과 지금 버전이 다를 때 로비에서 한 번 뜬다.
    public static class PatchNotes
    {
        public sealed class Section { public string Name; public readonly List<string> Items = new List<string>(); }

        public sealed class Entry
        {
            public string Version, Title, Date;
            public readonly List<string> Intro = new List<string>();
            public readonly List<Section> Sections = new List<Section>();
        }

        const string KSeen = "bz.patch.seen";
        static List<Entry> all;

        /// <summary>판들 — 새 판이 앞. 파일이 없으면 빈 목록.</summary>
        public static List<Entry> All => all ??= Parse(Resources.Load<TextAsset>("RunUI/patchnotes")?.text);

        /// <summary>지금 버전(맨 위 판) — 파일이 없으면 Application.version.</summary>
        public static string Version => All.Count > 0 ? All[0].Version : Application.version;

        /// <summary>이 버전의 창을 아직 안 봤는가.</summary>
        public static bool Unseen => All.Count > 0 && PlayerPrefs.GetString(KSeen, "") != Version;

        public static void MarkSeen() { PlayerPrefs.SetString(KSeen, Version); PlayerPrefs.Save(); }

        /// <summary>이번 실행이 이 기기의 첫 실행인가(플레이 기록 안내를 아직 안 본 채로 켜졌다) — 켜질 때 한 번 잰다(로비의 안내 토스트보다 먼저).</summary>
        public static bool FirstRun { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => FirstRun = !PlayRecord.Noticed;

        /// <summary>글 → 판들. 「@ 버전 | 제목 | 날짜」 로 판을 열고, 「## 분류」 · 「- 항목」 · 그 밖의 글(소개)을 그 판에 붙인다. # 줄 · 빈 줄은 건너뛴다.</summary>
        public static List<Entry> Parse(string text)
        {
            var list = new List<Entry>();
            if (string.IsNullOrEmpty(text)) return list;
            Entry e = null;
            Section s = null;
            foreach (var raw in text.Replace("\r", "").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("##"))
                {
                    if (e == null) continue;
                    s = new Section { Name = line.Substring(2).Trim() };
                    e.Sections.Add(s);
                }
                else if (line.StartsWith("#")) continue;
                else if (line.StartsWith("@"))
                {
                    var p = line.Substring(1).Split('|');
                    string At(int i) => i < p.Length ? p[i].Trim() : "";
                    e = new Entry { Version = At(0), Title = At(1), Date = At(2) };
                    if (e.Title.Length == 0) e.Title = e.Version;
                    s = null;
                    list.Add(e);
                }
                else if (e == null) continue;
                else if (line.StartsWith("- ") || line == "-")
                {
                    if (s == null) { s = new Section { Name = "" }; e.Sections.Add(s); }
                    s.Items.Add(line.Substring(1).Trim());
                }
                else e.Intro.Add(line);
            }
            return list;
        }
    }
}
