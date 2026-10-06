using System;
using System.Collections.Generic;
using System.Linq;
using Bolzena.Core;

namespace Bolzena.Battle
{
    // 낱말 풀이 — 카드 태그 · 상태 · 화면 낱말(웹판 docs/18 §4 · §5 · fight-screen ST_HELP 의 뜻 그대로, 수치는 코어 R.STATUS_V).
    // 코어에 풀이 표 API 가 생기면 그쪽을 부른다(보고에 적었다).
    public static class Terms
    {
        static int P(double x) => (int)Math.Round(x * 100);
        static double SV(string id) => R.SV(id);

        public static readonly Dictionary<string, string> Tag = new Dictionary<string, string>
        {
            ["개전"] = "첫 손패에 포함",
            ["보존"] = "턴 끝에 버려지지 않음",
            ["소멸"] = "내면 이 전투에서 빠짐(「소멸 N」은 N번 내면)",
            ["증발"] = "턴 끝에 손에 있으면 소멸",
            ["종극"] = "내면 바로 턴 종료",
            ["주도"] = "턴 시작에 이 카드를 먼저 내면 비용 -1",
            ["유일"] = "덱에 한 장만",
            ["연계"] = "손에 있을 때 다른 사도의 카드를 내면 AP 없이 저절로 나감",
            ["천상"] = "손에 있을 때 비용 2 이상 카드를 내면 저절로 나감",
            ["신속"] = "적의 즉시 행동 셈을 늘리지 않음",
            ["분쇄"] = "방어 · 실드가 남은 적에게 피해 +20%",
            ["약점"] = "성격과 상관없이 약점 공격(강인도 +1 · 피해 +10%)",
            ["연결"] = "직접 내면 손의 다른 연결 카드를 모두 버림",
            ["개막"] = "전투 시작 시 AP를 치르고 저절로 나감",
            ["금기"] = "신탁 · 복제 · 제거 불가",
            ["봉인"] = "처음 내면 효과 없이 봉인만 풀림 · 그다음부터 효과 발동",
            ["회수"] = "내면 버린 더미 대신 손으로 돌아옴(「회수 N」은 N번까지)",
            ["연쇄"] = "다음 턴 시작에 같은 효과 한 번 더",
            ["사용 불가"] = "낼 수 없음",
            ["강화"] = "내면 전투 내내 남는 효과 · 카드는 이 전투에서 빠짐",
        };

        public static readonly Dictionary<string, Func<string>> Status = new Dictionary<string, Func<string>>
        {
            ["취약"] = () => $"받는 피해 +{P(SV("취약"))}% · 맞을 때마다 −1",
            ["약화"] = () => $"주는 피해 -{P(SV("약화"))}% · 칠 때마다 −1",
            ["손상"] = () => $"얻는 방어 · 실드 -{P(SV("손상"))}% · 얻을 때마다 −1",
            ["피해 감소"] = () => $"받는 피해 -{P(SV("피해 감소"))}% · 맞을 때마다 −1",
            ["반격"] = () => $"적에게 맞으면 방어 기반 피해 {P(SV("반격"))}%로 반격 · 그때 −1",
            ["표식"] = () => $"공격 카드에 맞으면 덤 타격 {P(SV("표식"))}% · 강인도 1 · 그때 −1",
            ["잔불"] = () => $"격파된 이 적을 치면 피해 +{P(SV("잔불"))}%씩 · 그때 모두 사라짐",
            ["잔광"] = () => $"공격 카드 강인도 +1 · 격파된 적에게 피해 +{P(SV("잔광"))}%",
            ["면역"] = () => "디버프 1개를 막음 · 막을 때마다 −1",
            ["실드 유지"] = () => $"턴이 바뀔 때 방어의 {P(SV("실드 유지"))}%가 남음",
            ["저장"] = () => "턴이 끝날 때 남은 AP를 다음 턴으로 넘김",
            ["협공"] = () => $"아군이 공격 카드를 내면 다른 아군이 공격력 {P(SV("협공"))}%로 추가 공격",
            ["고동"] = () => $"턴 끝에 적 전체에 고정 피해(겹마다 {P(SV("고동"))}%)",
            ["그을림"] = () => $"즉시 행동 셈이 오를 때마다 지속 피해 {P(SV("그을림"))}%",
            ["충격"] = () => $"공격 카드의 대상이 되면 고정 피해 {P(SV("충격"))}%",
            ["충격파"] = () => $"카드에 맞으면 그 적을 뺀 적 전체에 고정 피해 {P(SV("충격파"))}%",
            ["사기"] = () => $"카드 피해 계수 +{P(SV("사기"))}%p(겹마다, 전투 내내)",
            ["불굴"] = () => $"받는 피해 -{P(SV("불굴"))}%(겹마다, 최대 -{P(SV("불굴Cap"))}%)",
            ["결의"] = () => $"얻는 방어 · 실드 +{SV("결의")}(겹마다)",
            ["결정화"] = () => $"턴 끝에 방어력 {P(SV("결정화"))}% 실드(겹마다)",
            ["고통"] = () => $"턴 끝에 지속 피해(겹의 {P(SV("고통"))}%) — 그 뒤 절반",
            ["균열"] = () => $"턴 끝에 지속 피해(겹마다 {P(SV("균열"))}%) — 그 뒤 절반",
            ["리듬"] = () => "이번 턴 박자 — 「잇기:」 · 「앞이 …:」 가 서면 +1 · 턴이 끝나면 사라짐",
            ["기절"] = () => "다음 차례를 쉼",
        };

        // 화면 낱말 — 카드 글에 자주 나오는 것
        public static readonly Dictionary<string, string> Words = new Dictionary<string, string>
        {
            ["방어"] = "턴이 바뀌면 사라지는 막 · 피해를 먼저 받음",
            ["실드"] = "턴이 바뀌어도 남는 막 · 피해를 먼저 받음",
            ["강인도"] = "0이 되면 격파 · 격파한 쪽 AP +1 · 약점 성격 사도로 치면 많이 깎임(비용 1당 1, 약점이 아니면 1/3) · 저절로 차지 않음(회복 스킬이 있는 적만 되찾음)",
            ["격파"] = "강인도 0 · 그 적은 다음 차례를 쉬고, 격파한 쪽 AP +1 · 다음 내 턴 시작에 강인도가 가득 참",
            ["즉시 행동"] = "적의 수가 예고된 뒤 카드를 그 장수만큼 내면 당겨서 바로 행동",
            ["방어 기반"] = "방어력 210% + 공격력 30% 를 바탕으로 한 피해",
            ["고정 피해"] = "상태 · 상성 · 증감을 타지 않는 피해",
            ["치명"] = "피해 ×1.5",
            ["드로우"] = "뽑을 더미에서 카드를 뽑음",
            ["파괴"] = "「파괴:」 — 대상이 이 카드로 쓰러졌으면",
            ["조율"] = "「조율:」 — 이 카드의 비용이 낼 때 남은 AP와 같으면",
            ["연속"] = "「연속:」 — 바로 앞 카드가 같은 성격이면",
            ["잇기"] = "「잇기:」 — 바로 앞 카드가 같은 사도의 카드면(리듬 +1)",
            ["영감"] = "「영감:」 — 카드 · 패시브로 뽑혔을 때",
            ["안식"] = "「안식:」 — 카드 효과로 버려질 때",
            ["전환"] = "카드 주인의 모드 고유 효과를 뒤집음",
        };

        /// <summary>
        /// 상태 · 태그 · 낱말 풀이 — 엔진 표(core CardText.TIPS — 수치는 Rules.cs 에서 읽고 지속 · 제한까지 다 든다, Docs/설명글.md)가 먼저,
        /// 엔진 표에 없는 것만 아래 옛 표(리듬 · 화면 낱말 따위).
        /// </summary>
        public static string Tip(string id) => id != null ? CardText.Tip(id) : null;
        public static string StatusText(string id) => Tip(id) ?? (Status.TryGetValue(id, out var f) ? f() : null);
        public static string TagText(string id) => Tip(id == Bolzena.Core.Tag.Weak ? Bolzena.Core.Tag.WeakHit : id) ?? (Tag.TryGetValue(id, out var t) ? t : null);
        public static string WordText(string id) => Tip(id) ?? (Words.TryGetValue(id, out var t) ? t : null);

        // 카드 글 · 태그에 실제로 나오는 낱말만 풀이로(풀이 속 낱말까지 끌어오지 않는다)
        public static List<Term> ForCard(string text, IEnumerable<string> tags, Func<string, (string name, string text)?> keyword)
        {
            var res = new List<Term>();
            var seen = new HashSet<string>();
            void Add(string w, string t, string kind) { if (t != null && seen.Add(w)) res.Add(new Term(w, t, kind)); }
            foreach (var raw in tags)
            {
                var (id, _) = Bolzena.Core.Tag.Parse(raw);
                var t = TagText(id);
                if (t != null) Add(id, t, "tag");
            }
            string plain = text ?? "";
            // 사도 키워드 「X」
            int i = 0;
            while ((i = plain.IndexOf('「', i)) >= 0)
            {
                int j = plain.IndexOf('」', i + 1);
                if (j < 0) break;
                var w = plain.Substring(i + 1, j - i - 1);
                var k = keyword(w);
                if (k != null) Add(k.Value.name, k.Value.text, "kw");
                i = j + 1;
            }
            // 엔진 상태(취약 · 사기 …) — 이름이 둘 이상 글자인 것만(글 속 우연한 한 글자 맞춤을 막는다)
            foreach (var id in R.ALL_ST.Concat(R.CARD_ST).Concat(Status.Keys).Distinct()) if (id.Length >= 2 && plain.Contains(id)) Add(id, StatusText(id), "status");
            foreach (var kv in Words) if (plain.Contains(kv.Key)) Add(kv.Key, WordText(kv.Key), "kw");
            return res;
        }
    }
}
