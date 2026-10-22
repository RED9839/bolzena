using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bolzena.Core
{
    /// <summary>
    /// 진행 코드(설정 → 「진행 코드」) — 이 기기의 편성 기억(편성 프리셋 · 최근 편성 · 완주 기록 따위, 화면이 정한 이름 → 글)을 다른 기기로 옮긴다.
    /// 꼴: 「BZP1-(base64 JSON)-(체크섬 8자리)」. JSON = { v 판, x 덧붙임 }. 엔진은 덧붙임의 뜻을 모른다(화면 저장 — runui PartyStore).
    /// 판 1~3 은 교주 보드(크레파스) 진행도 함께 담았다(h · e · l · r). 교주 보드는 2026-10-21 사용자 결정으로 없앴다 —
    /// 옛 코드를 읽으면 보드 몫은 버리고 덧붙임만 받는다(보드만 담긴 판 1 코드는 받을 것이 없다).
    /// </summary>
    public static class ProgressCode
    {
        const string HEAD = "BZP1";
        /// <summary>코드 판 — 4: 덧붙임만(2026-10-21 교주 보드 없앰). 1: 보드만 · 2: 보드 + 덧붙임 · 3: 보드 판 2 + 덧붙임(옛).</summary>
        public const int VER = 4;

        /// <summary>진행 코드를 짓는다(덧붙임이 비어도 올바른 코드).</summary>
        public static string Export(Dictionary<string, string> extra)
        {
            var o = new JObject
            {
                ["v"] = VER,
                ["x"] = JObject.FromObject(extra ?? new Dictionary<string, string>()),
            };
            var body = Convert.ToBase64String(Encoding.UTF8.GetBytes(o.ToString(Formatting.None)));
            return $"{HEAD}-{body}-{Checksum(body):x8}";
        }

        /// <summary>
        /// 진행 코드를 읽는다 — 덧붙임 · 까닭. 틀리면(머리 · 체크섬 · 꼴 · 모르는 판 · 덧붙임 없는 옛 코드) null 과 까닭.
        /// </summary>
        public static (Dictionary<string, string> extra, string why) Import(string code)
        {
            code = (code ?? "").Trim();
            var parts = code.Split('-');
            if (parts.Length != 3 || parts[0] != HEAD) return (null, "진행 코드가 아니다");
            if (!uint.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out var sum) || sum != Checksum(parts[1])) return (null, "코드가 손상됐다(체크섬이 맞지 않음)");
            try
            {
                var j = JObject.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])));
                int ver = j["v"] != null ? (int)j["v"] : 1;
                if (ver < 1 || ver > VER) return (null, $"이 게임보다 새 코드다(판 {ver})");
                if (ver < 2) return (null, "교주 보드만 담긴 옛 코드다 — 교주 보드는 없어져 받을 것이 없다");
                return (j["x"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>(), null);
            }
            catch (Exception) { return (null, "코드를 읽을 수 없다"); }
        }

        /// <summary>FNV-1a 32비트 — 진행 코드 체크섬.</summary>
        public static uint Checksum(string s)
        {
            uint h = 2166136261;
            foreach (var ch in s) { h ^= ch; h = unchecked(h * 16777619); }
            return h;
        }
    }
}
