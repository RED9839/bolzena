using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    // 사도별 대사(2026-10-08 사용자: 「에르핀이 존댓말할 일이 없는데」) — 로비 · 만지기 반응 · 편성 한마디를 사도마다 원작 말투로.
    //   표: Resources/RunUI/hero_lines.json — 사도 키마다 reg(말 단계) · lobby · cheek · tickle · angry · knock(꿀밤) · pat · party.
    //     reg = casual(반말) · royal(왕 말투, 반말 계열) · polite(존댓말) · special(하오체 · 「~사와요」 · 로봇 따위 특이 말투).
    //   우리가 쓴 2차 창작 대사다(원작 대사 원문이 아니다). 검사: Tools~/hero_lines_check.py
    //   표에 없는 사도 · 빈 칸은 공통 문구 — 존댓말 / 반말 두 벌 가운데 그 사도의 reg 로 고른다(reg 도 없으면 존댓말).
    public static class HeroLines
    {
        [Serializable]
        class Entry
        {
            public string key, reg, note;
            public string[] lobby, cheek, tickle, angry, knock, pat, party;
        }

        [Serializable] class FileT { public List<Entry> heroes = new List<Entry>(); }

        static Dictionary<string, Entry> map;

        static Dictionary<string, Entry> Map
        {
            get
            {
                if (map != null) return map;
                map = new Dictionary<string, Entry>();
                var ta = Resources.Load<TextAsset>("RunUI/hero_lines");
                if (ta == null) { Debug.LogWarning("[HeroLines] hero_lines.json 없음 — 공통 문구만 씁니다"); return map; }
                var f = JsonUtility.FromJson<FileT>(ta.text);
                if (f?.heroes != null) foreach (var e in f.heroes) if (!string.IsNullOrEmpty(e.key)) map[e.key] = e;
                return map;
            }
        }

        // 공통 문구 — 존댓말 / 반말. 호칭 없이(사도마다 교주를 부르는 말이 달라서)
        static readonly Dictionary<string, string[]> Polite = new Dictionary<string, string[]>
        {
            ["lobby"] = new[] { "오늘도 모험 가는 거죠?", "준비는 다 됐어요. 언제든지요!", "카드는 잘 챙기셨어요?" },
            ["cheek"] = new[] { "으앗, 볼이 늘어나요!", "히잉… 볼은 왜 당겨요?" },
            ["tickle"] = new[] { "아하하! 가, 간지러워요!", "푸핫! 그만, 그만이요!" },
            ["angry"] = new[] { "그만해요! 진짜 화낼 거예요!", "흥, 이제 안 놀아 줄 거예요!" },
            ["knock"] = new[] { "아야! 머리는 때리지 마세요!", "으앗, 갑자기 꿀밤이에요?" },
            ["pat"] = new[] { "헤헤… 기분 좋아요." },
            ["party"] = new[] { "같이 가요. 맡겨 주세요!" },
        };
        static readonly Dictionary<string, string[]> Casual = new Dictionary<string, string[]>
        {
            ["lobby"] = new[] { "오늘도 모험 가는 거지?", "준비는 다 됐어. 언제든지!", "카드는 잘 챙겼어?" },   // 문체:허용 — 사도 대사(반말)
            ["cheek"] = new[] { "으앗, 볼 늘어나!", "히잉… 볼은 왜 당기는 건데!" },   // 문체:허용 — 사도 대사(반말)
            ["tickle"] = new[] { "아하하! 가, 간지러워!", "푸핫! 그만, 그만!" },   // 문체:허용 — 사도 대사(반말)
            ["angry"] = new[] { "그만해! 진짜 화낸다!", "흥, 이제 안 놀아 줄 거야!" },   // 문체:허용 — 사도 대사(반말)
            ["knock"] = new[] { "아야! 머리 때리지 마!", "으앗, 갑자기 꿀밤이야?" },   // 문체:허용 — 사도 대사(반말)
            ["pat"] = new[] { "헤헤… 기분 좋다." },   // 문체:허용 — 사도 대사(반말)
            ["party"] = new[] { "같이 가자. 나만 믿어!" },   // 문체:허용 — 사도 대사(반말)
        };

        /// <summary>그 사도의 말 단계(casual · royal · polite · special). 표에 없으면 null.</summary>
        public static string Reg(HeroInfo h) => h != null && Map.TryGetValue(h.key, out var e) ? e.reg : null;

        /// <summary>반말 계열(casual · royal)인가 — 공통 문구를 고를 때.</summary>
        public static bool IsCasual(HeroInfo h) { var r = Reg(h); return r == "casual" || r == "royal"; }

        /// <summary>표에 그 사도 대사가 있나.</summary>
        public static bool Has(HeroInfo h) => h != null && Map.ContainsKey(h.key);

        /// <summary>그 상황(lobby · cheek · tickle · angry · knock · pat · party)의 대사 — 비면 말 단계에 맞는 공통 문구.</summary>
        public static string[] Of(HeroInfo h, string kind)
        {
            string[] a = null;
            if (h != null && Map.TryGetValue(h.key, out var e))
                a = kind switch { "lobby" => e.lobby, "cheek" => e.cheek, "tickle" => e.tickle, "angry" => e.angry, "knock" => e.knock, "pat" => e.pat, "party" => e.party, _ => null };
            if (a != null && a.Length > 0) return a;
            var set = IsCasual(h) ? Casual : Polite;
            return set.TryGetValue(kind, out var f) ? f : Polite["lobby"];
        }

        /// <summary>그 상황에서 한 줄(무작위).</summary>
        public static string Pick(HeroInfo h, string kind)
        {
            var a = Of(h, kind);
            return a[UnityEngine.Random.Range(0, a.Length)];
        }
    }
}
