using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Bolzena.Core.Tests
{
    /// <summary>
    /// 입력 테이프 · 다시 돌리기(프론티어 1단계 — BattleInput.cs). 봇 판의 전투마다 테이프를 JSON 으로 썼다 읽어 처음부터 다시 돌려 턴 지문 · 끝 지문 · 결과가 같은가,
    /// 화면 · 봇 미리보기를 수마다 끼워도 같은가, 규칙 · 콘텐츠 지문, 그리고 .NET 8(bz replay --out)이 남긴 테이프를 유니티(Mono)에서 다시 돌려 같은가.
    /// </summary>
    public class ReplayTests
    {
        static readonly List<string> PARTY = new() { "rico", "carrot", "sion" };
        /// <summary>콘텐츠 폴더 — 환경 변수 BZ_CONTENT(교차 테이프를 뜬 콘텐츠 사본, 예: 성장 표를 넣은 scratchpad 사본), 없으면 bolzena-content-v2.</summary>
        static readonly string CONTENT = System.Environment.GetEnvironmentVariable("BZ_CONTENT") is string bc && bc.Length > 0 ? bc : "C:/projects/bolzena-content-v2";

        /// <summary>봇 판 하나 — 판이 연 전투들(테이프가 달린 채).</summary>
        static List<Battle> Fights(GameData d, List<string> party, long seed, bool skilled = false)
        {
            var got = new List<Battle>();
            new RunBot(d).RunFull(party, seed, new SimOpts { Skilled = skilled, OnFight = (b, run) => got.Add(b) });
            return got;
        }

        static void AllSame(GameData d, List<Battle> fights, bool poke, string what)
        {
            var bots = poke ? new Bots(d) : null;
            Assert.Greater(fights.Count, 0, what);
            for (int k = 0; k < fights.Count; k++)
            {
                var b = fights[k];
                Assert.IsNotNull(b.Input, $"{what} 전투 {k} — 판이 테이프를 단다");
                var tape = BattleInput.Parse(b.Input.Json());   // 파일로 썼다 읽은 것과 같게
                var r = BattleInput.Replay(d, tape, poke ? (Action<Battle>)(s => BattleInput.Poke(s, bots)) : null);
                Assert.IsTrue(r.Same, $"{what} 전투 {k} — {r.Why}");
                bool cut = tape.Over == "lose" && r.Battle.Over == null;   // 봇 40턴 끊김은 엔진 밖의 lose
                if (!cut) Assert.AreEqual(tape.Over, r.Battle.Over, $"{what} 전투 {k} 결과");
                Assert.AreEqual(b.Turn, r.Battle.Turn, $"{what} 전투 {k} 턴");
                Assert.AreEqual(Math.Max(0, b.Pool.Hp), Math.Max(0, r.Battle.Pool.Hp), $"{what} 전투 {k} 파티 HP");
                Assert.AreEqual(tape.Digests.Count, r.Battle.Input.Digests.Count);
            }
        }

        [Test] public void 봇_판의_전투마다_테이프로_다시_돌리면_같다()
        {
            var d = K.Sample();
            for (long seed = 1; seed <= 4; seed++) AllSame(d, Fights(d, PARTY, seed), false, $"씨앗 {seed}");
            AllSame(d, Fights(d, PARTY, 5, skilled: true), false, "숙련 씨앗 5");
        }

        [Test] public void 미리보기를_수마다_끼워도_같다()
        {
            var d = K.Sample();
            for (long seed = 1; seed <= 3; seed++) AllSame(d, Fights(d, PARTY, seed), true, $"씨앗 {seed}");
        }

        [Test] public void 콘텐츠_봇_판도_미리보기를_끼워_같다()
        {
            if (!Directory.Exists(CONTENT)) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(CONTENT);
            bool tf = R.TOUGH_FIXED; R.TOUGH_FIXED = true;   // 시험 도구(K)가 끈 것을 판 기본으로
            try
            {
                foreach (var j in PartyPick.Jobs(d, 1, 0, PartyPick.Random).Take(4))
                    AllSame(d, Fights(d, j.party, j.seed), true, $"{string.Join(",", j.party)} 씨앗 {j.seed}");
            }
            finally { R.TOUGH_FIXED = tf; }
        }

        [Test] public void 테이프를_한_칸_바꾸면_어긋남을_잡는다()
        {
            var d = K.Sample();
            var b = Fights(d, PARTY, 2).First(x => x.Input.Ops.Count(o => o.K == InputOp.PLAY) >= 3 && x.Input.Digests.Count >= 3);
            var json = b.Input.Json();
            // 카드 한 장을 빼면 — 다음 수의 손 자리 또는 턴 지문이 어긋난다
            var t1 = BattleInput.Parse(json);
            t1.Ops.RemoveAt(t1.Ops.FindIndex(o => o.K == InputOp.PLAY));
            Assert.IsFalse(BattleInput.Replay(d, t1).Same, "수 하나를 빼면 다르다");
            // 지문을 하나 바꾸면 — 그 번호에서 어긋난다
            var t2 = BattleInput.Parse(json);
            t2.Digests[1] = "0000000000000000";
            var r2 = BattleInput.Replay(d, t2);
            Assert.IsFalse(r2.Same); Assert.AreEqual(1, r2.Turn);
            // 수마다 앞 지문(Fine)으로 적으면 한 수 단위로 잡는다
            BattleInput.Fine = true;
            try
            {
                var bf = Fights(d, PARTY, 2).First(x => x.Input.Ops.Count(o => o.K == InputOp.PLAY) >= 3);
                Assert.IsTrue(bf.Input.Ops.All(o => o.Pre != null));
                Assert.IsTrue(BattleInput.Replay(d, BattleInput.Parse(bf.Input.Json())).Same);
            }
            finally { BattleInput.Fine = false; }
        }

        [Test] public void 셋업은_떠_둔_그대로_되살아난다()
        {
            var d = K.Sample();
            foreach (var b in Fights(d, PARTY, 3))
            {
                var tape = BattleInput.Parse(b.Input.Json());
                var again = BattleInput.Of(tape.Setup());
                Assert.IsTrue(JToken.DeepEquals(tape.SetupJson, again.SetupJson), "셋업 → JSON → 셋업 → JSON 이 같다(필드 누락 · 기본값 어긋남 없음)");
            }
            // 판이 연 뒤에 판 상태(신탁 표)를 고쳐도 테이프의 셋업은 그대로
            var run = Run.New(d, PARTY, 9);
            var (fb, _) = run.OpenFight();
            var before = fb.Input.SetupJson.ToString();
            run.S.Flash["아무_카드"] = 2;
            Assert.AreEqual(before, fb.Input.SetupJson.ToString());
        }

        [Test] public void 복사본은_진짜_판과_테이프를_나눠_쓰지_않는다()
        {
            var d = K.Sample();
            var b = Fights(d, PARTY, 1)[0];
            Assert.IsNotNull(b.Input);
            Assert.IsNull(b.Clone().Input, "봇 · 미리보기 복사본은 적지 않는다");
            int n = b.Input.Ops.Count;
            b.PreviewCard(0, 0);
            Assert.AreEqual(n, b.Input.Ops.Count, "미리보기는 테이프에 안 남는다");
        }

        [Test] public void 복사본의_적이_카드를_빼앗아도_진짜_적은_그대로()
        {
            var e = new Unit { Side = Side.Enemy, Key = "x" };
            e.Seized.Add("a");
            var c = e.Clone();
            c.Seized.Add("b");
            CollectionAssert.AreEqual(new[] { "a" }, e.Seized, "2026-10-18 — 복사본이 빼앗은 카드가 진짜 판의 적 목록에 쌓였다");
        }

        [Test] public void 규칙_지문은_손잡이와_표의_속을_본다()
        {
            string r0 = Fingerprint.Rules();
            Assert.AreEqual(r0, Fingerprint.Rules(), "같은 값이면 같다");
            int fn = R.FLOOR_NODES;
            try { R.FLOOR_NODES = fn + 1; Assert.AreNotEqual(r0, Fingerprint.Rules(), "바꿀 수 있는 손잡이"); }
            finally { R.FLOOR_NODES = fn; }
            var k = R.ROLE_GUARD.Keys.First(); double v = R.ROLE_GUARD[k];
            try { R.ROLE_GUARD[k] = v + 0.5; Assert.AreNotEqual(r0, Fingerprint.Rules(), "readonly 표의 속"); }
            finally { R.ROLE_GUARD[k] = v; }
            bool on = BossUlt.On;
            try { BossUlt.On = !on; Assert.AreNotEqual(r0, Fingerprint.Rules(), "보스 클론 고학년 손잡이"); }
            finally { BossUlt.On = on; }
            Assert.AreEqual(r0, Fingerprint.Rules(), "되돌리면 같다");
            R.SMax("약화"); R.SMax("새_상태_이름");   // 메모 캐시(MAX_KEYS)는 지문에 안 든다
            Assert.AreEqual(r0, Fingerprint.Rules());
        }

        [Test] public void 콘텐츠_지문은_줄바꿈과_상관없고_글이_바뀌면_바뀐다()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bz_hash_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string f = Path.Combine(dir, "all.json");
                string body = "{ enemies:[{id:'m', name:'졸개', hp:100, intents:[{t:'attack', v:10}]}] }";
                File.WriteAllText(f, body.Replace(" e", "\n e"));
                var h1 = GameData.FromFolder(dir).ContentHash;
                File.WriteAllText(f, body.Replace(" e", "\r\n e"));
                Assert.AreEqual(h1, GameData.FromFolder(dir).ContentHash, "CRLF · LF 는 같다(git 설정이 달라도)");
                File.WriteAllText(f, body.Replace("hp:100", "hp:101"));
                Assert.AreNotEqual(h1, GameData.FromFolder(dir).ContentHash);
                Assert.IsNull(GameData.FromFolder(dir).Add(null, null, null, null, null, null).ContentHash, "덧씌운 콘텐츠는 지문 없음");
            }
            finally { Directory.Delete(dir, true); }
        }

        /// <summary>
        /// .NET 8 ↔ 유니티(Mono) 교차 — bz replay --data bolzena-content-v2 --out (아래 폴더) 가 남긴 테이프를 여기서 다시 돌려 지문이 같은가.
        /// 폴더: 환경 변수 BZ_TAPES, 없으면 bolzena-core-test/replay-tapes. 없으면 건너뛴다.
        /// </summary>
        [Test] public void 닷넷이_남긴_테이프를_유니티에서_다시_돌려도_같다()
        {
            string dir = Environment.GetEnvironmentVariable("BZ_TAPES");
            if (string.IsNullOrEmpty(dir)) dir = new[] { "replay-tapes", "C:/projects/bolzena-core-test/replay-tapes" }.FirstOrDefault(Directory.Exists);
            if (dir == null || !Directory.Exists(dir)) Assert.Ignore("교차 테이프 폴더가 없다(bz replay --out)");
            if (!Directory.Exists(CONTENT)) Assert.Ignore("bolzena-content-v2 가 없다");
            var d = GameData.FromFolder(CONTENT);
            var files = Directory.GetFiles(dir, "*.json").OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (files.Count == 0) Assert.Ignore("테이프가 없다");
            bool tf = R.TOUGH_FIXED; R.TOUGH_FIXED = true;
            try
            {
                var bad = new List<string>();
                int same = 0;
                foreach (var f in files)
                {
                    var tape = BattleInput.Parse(File.ReadAllText(f));
                    Assert.AreEqual(tape.Rules, Fingerprint.Rules(), "규칙 지문 — 같은 규칙 값(두 런타임의 지문 셈도 같다)\n" + Fingerprint.RulesText());
                    Assert.AreEqual(tape.Content, d.ContentHash, "콘텐츠 지문 — 같은 콘텐츠로 남긴 테이프");
                    var r = BattleInput.Replay(d, tape);
                    bool cut = tape.Over == "lose" && r.Battle.Over == null;
                    if (r.Same && (cut || r.Battle.Over == tape.Over)) same++; else bad.Add($"{Path.GetFileName(f)} — {r.Why ?? $"결과 {tape.Over} → {r.Battle.Over}"}");
                }
                Assert.AreEqual(files.Count, same, $"테이프 {files.Count} 가운데 다름 {bad.Count}\n" + string.Join("\n", bad.Take(20)));
                UnityEngine.Debug.Log($"[ReplayTests] .NET 8 테이프 {files.Count}개를 유니티에서 다시 돌림 — 모두 같음");
            }
            finally { R.TOUGH_FIXED = tf; }
        }
    }
}
