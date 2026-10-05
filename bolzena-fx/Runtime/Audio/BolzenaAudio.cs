using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Audio;

namespace Bolzena.Fx
{
    // 효과음 · 목소리 — 웹판 js/sfx.js · js/voice.js 의 규칙을 AudioSource 풀로 옮겼다.
    //   BolzenaAudio.Play("hit.slash")                         공용 갈래(SfxMap.SFX)
    //   BolzenaAudio.Hero("에르핀", "attack") · Enemy("curburus", "hit")   제 소리(없으면 공용 대신 갈래)
    //   BolzenaAudio.At("에르핀", "ult2", 1300)                 큰 소리(첫 마루)가 1.3초 뒤에 오도록 당겨 튼다
    //   BolzenaAudio.Action("에르핀", "ult", plan.Snd, ...)     동작 하나의 소리 — 스파인 SFX(n) 이벤트 시각에 그 칸의 파일
    //   BolzenaAudio.Land(...) · Ult(...) · Card(...)          전투 한 줄짜리
    //   BolzenaAudio.Speak("에르핀", "ultimate", "shout")       목소리(한 번에 하나, 같은 대사는 12초 안에 되풀이 안 함)
    // 규칙(웹판과 같다):
    //   크기  파일마다 g(색인 — 원작 세기 맞춤) × 갈래의 v × SfxVolume. 믹서 그룹(Sfx)에 압축기를 걸면 겹친 순간만 누른다
    //   길이  앞의 고요(lead)는 건너뛰고, 긴 꼬리는 갈래의 max 초에서 fade 초 동안 줄여 끊는다(TAIL · KIND_TAIL)
    //   겹침  모두 합쳐 LIVE_MAX 개까지(넘치면 가장 작은 · 오래된 것을 줄여 끊는다), 갈래(앞머리)마다 CAP 개까지.
    //         같은 소리가 0.5초 안에 잇달면 한 번마다 0.72배(0.45배까지), 같은 순간(±70ms)에 다른 소리가 걸렸으면 하나마다 12% 작게(맞는 소리는 빼고)
    //         같은 소리는 45ms 안에 다시 안 틀고, 갈래마다 gap ms 안에 다시 안 튼다. 높이 흔들기는 맞는 소리에만 ±2.5%
    // 소리 파일은 Resources/BolzenaAudio/(sfx|voice)/…(Tools/fx_prepare.py 가 Ogg Opus 를 WAV 로 풀어 둔다), 색인은 audio_index.json
    public class BolzenaAudio : MonoBehaviour
    {
        public static string Root = "BolzenaAudio";
        public static float SfxVolume = 0.8f, VoiceVolume = 0.9f;
        public static AudioMixerGroup SfxGroup, VoiceGroup;

        static readonly Dictionary<string, int> CAP = new Dictionary<string, int> { { "hit", 4 }, { "hurt", 3 }, { "hero", 6 }, { "monster", 3 }, { "card", 3 }, { "ui", 2 }, { "ult", 2 } };
        public const int LIVE_MAX = 8, POOL = 24;
        const float SAME_MS = 45, REPEAT_MS = 500, REPEAT_K = 0.72f, REPEAT_MIN = 0.45f, BUSY_MS = 70, BUSY_K = 0.12f, BUSY_MIN = 0.6f;
        const int NO_REPEAT_MS = 12000;

        static BolzenaAudio inst;
        readonly List<AudioSource> pool = new List<AudioSource>();
        AudioSource voice;

        class Info { public float G = 1, Lead, Onset, D; }
        static Dictionary<string, Info> sfxIdx;
        static Dictionary<string, Dictionary<string, List<string>>> voiceIdx;
        static Dictionary<string, string> heroSfx, heroVoice, art;
        static HashSet<string> monsterDirs;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static readonly Dictionary<string, Dictionary<string, List<string>>> dirCache = new Dictionary<string, Dictionary<string, List<string>>>();
        static readonly Dictionary<string, SfxMap.Slots> slotCache = new Dictionary<string, SfxMap.Slots>();

        class Live { public AudioSource Src; public float Vol, Pitch = 1; public double Start, End, CutAt = -1; public float Fade; public double At; public string Cat; }
        readonly List<Live> all = new List<Live>();
        readonly Dictionary<string, double> lastAt = new Dictionary<string, double>();
        readonly Dictionary<string, string> lastPath = new Dictionary<string, string>();
        readonly Dictionary<string, List<double>> recent = new Dictionary<string, List<double>>();
        public static int Dropped;
        public static readonly List<string> Trace = new List<string>();   // 시험용 — 최근에 건 소리
        string lastVoice; double lastVoiceAt = -1e9;

        static double Now => AudioSettings.dspTime * 1000.0;

        // ── 준비 ──
        public static bool Ready => Ensure() != null && sfxIdx != null;

        static BolzenaAudio Ensure()
        {
            if (inst != null) return inst;
            // 믹서가 있으면(FxImport 가 Resources/BolzenaAudio/BolzenaMixer 로 만든다) 그 그룹으로
            if (SfxGroup == null)
            {
                var mixer = Resources.Load<AudioMixer>(Root + "/BolzenaMixer");
                if (mixer != null)
                {
                    var s1 = mixer.FindMatchingGroups("Sfx"); if (s1.Length > 0) SfxGroup = s1[0];
                    var v1 = mixer.FindMatchingGroups("Voice"); if (v1.Length > 0 && VoiceGroup == null) VoiceGroup = v1[0];
                }
            }
            var go = new GameObject("BolzenaAudio");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<BolzenaAudio>();
            for (int i = 0; i < POOL; i++)
            {
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false; s.spatialBlend = 0; s.outputAudioMixerGroup = SfxGroup;
                inst.pool.Add(s);
            }
            inst.voice = go.AddComponent<AudioSource>();
            inst.voice.playOnAwake = false; inst.voice.spatialBlend = 0; inst.voice.outputAudioMixerGroup = VoiceGroup;
            LoadIndex();
            return inst;
        }

        // 믹서를 쓰면 — 효과음 · 목소리 그룹(효과음 쪽에 압축기)
        public static void Configure(AudioMixerGroup sfx, AudioMixerGroup voiceGroup)
        {
            SfxGroup = sfx; VoiceGroup = voiceGroup;
            if (inst == null) return;
            foreach (var s in inst.pool) s.outputAudioMixerGroup = sfx;
            inst.voice.outputAudioMixerGroup = voiceGroup;
        }

        static void LoadIndex()
        {
            if (sfxIdx != null) return;
            var ta = Resources.Load<TextAsset>(Root + "/audio_index");
            if (ta == null) { Debug.LogWarning("[BolzenaAudio] 색인 없음 — Tools/fx_prepare.py 를 먼저"); return; }
            var j = (Dictionary<string, object>)MiniJson.Parse(ta.text);
            sfxIdx = new Dictionary<string, Info>();
            foreach (var kv in (Dictionary<string, object>)j["sfx"])
            {
                var d = (Dictionary<string, object>)kv.Value;
                float F(string k) => d.TryGetValue(k, out var v) && v is double x ? (float)x : 0;
                sfxIdx[kv.Key] = new Info { G = F("g") > 0 ? F("g") : 1, Lead = F("lead"), Onset = F("onset"), D = F("d") };
            }
            voiceIdx = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (var kv in (Dictionary<string, object>)j["voice"])
            {
                var cats = new Dictionary<string, List<string>>();
                foreach (var c in (Dictionary<string, object>)kv.Value) cats[c.Key] = ((List<object>)c.Value).Cast<string>().ToList();
                // 웹판 voiceSet — touch · dutchrubend 를 둘로 가른다
                List<string> Only(string cat, string re) => cats.TryGetValue(cat, out var l) ? l.Where(f => Regex.IsMatch(f, re)).ToList() : new List<string>();
                cats["cheek"] = Only("touch", @"touch1(?:_\d+)?$");
                cats["pat"] = Only("touch", @"touch2(?:_\d+)?$");
                cats["smashHit"] = Only("dutchrubend", @"dutchrubend1(?:_\d+)?$");
                cats["smashLine"] = Only("dutchrubend", @"dutchrubend2(?:_\d+)?$");
                voiceIdx[kv.Key] = cats;
            }
            Dictionary<string, string> Map(string k) => j.TryGetValue(k, out var o) && o is Dictionary<string, object> d ? d.ToDictionary(x => x.Key, x => (string)x.Value) : new Dictionary<string, string>();
            heroSfx = Map("heroSfx"); heroVoice = Map("heroVoice"); art = Map("art");
            monsterDirs = new HashSet<string>(sfxIdx.Keys.Where(k => k.StartsWith("monster/")).Select(k => k.Split('/')[1]));
        }

        static AudioClip Clip(string path)
        {
            if (clips.TryGetValue(path, out var c)) return c;
            c = Resources.Load<AudioClip>(Root + "/sfx/" + path);
            clips[path] = c;
            return c;
        }

        public static void Preload(IEnumerable<string> paths) { if (Ensure() != null) foreach (var p in paths) { var c = Clip(p); if (c != null) c.LoadAudioData(); } }

        // 그 사도의 제 소리 · 고학년 목소리를 미리 읽어 둔다(첫 재생에 디스크 읽기 · 풀기가 끼지 않게). 읽은 수를 돌려준다
        public static int PreloadHero(string heroKey)
        {
            Ensure();
            if (sfxIdx == null) return 0;
            int n = 0;
            var d = HeroDir(heroKey);
            if (d != null) foreach (var k in sfxIdx.Keys) if (k.StartsWith("hero/" + d + "/")) { var c = Clip(k); if (c != null) { c.LoadAudioData(); n++; } }
            foreach (var kv in SfxMap.SFX) if (kv.Key.StartsWith("hit") || kv.Key.StartsWith("ult")) foreach (var f in kv.Value.F) { var c = Clip(f); if (c != null) { c.LoadAudioData(); n++; } }
            var vd = VoiceDir(heroKey);
            if (vd != null && voiceIdx.TryGetValue(vd, out var set) && set.TryGetValue("ultimate", out var ul))
                foreach (var f in ul) { var c = Resources.Load<AudioClip>(Root + "/voice/" + f); if (c != null) { c.LoadAudioData(); n++; } }
            return n;
        }

        // ── 트는 곳 ──
        public class Opts
        {
            public float V = 1, Pan, DelayMs;
            public double? HitAt;            // 이 시각(Now ms)에 첫 마루가 오도록 당겨 시작한다
            public bool Pitch;               // 높이 흔들기(맞는 소리만)
            public int Gap = 50;
            public string Tag;
            public float Max, Fade = 0.15f;  // 꼬리 — Max 초를 넘으면 Fade 초 동안 줄여 끊는다(0 이면 끝까지)
            public Opts With(Action<Opts> f) { var o = (Opts)MemberwiseClone(); f(o); return o; }
        }

        float Crowd(string tag, double t, bool soft)
        {
            if (!recent.TryGetValue(tag, out var r)) recent[tag] = r = new List<double>();
            r.RemoveAll(x => Math.Abs(t - x) >= REPEAT_MS);
            float rep = Mathf.Max(REPEAT_MIN, Mathf.Pow(REPEAT_K, r.Count));
            r.Add(t);
            if (r.Count > 6) r.RemoveAt(0);
            if (!soft) return rep;
            int n = 0;
            foreach (var x in all) if (Math.Abs(x.At - t) < BUSY_MS) n++;
            return rep * Mathf.Max(BUSY_MIN, 1 - BUSY_K * n);
        }

        void CutOff(Live x)
        {
            double dsp = AudioSettings.dspTime;
            if (x.Start > dsp) { x.Src.Stop(); x.End = dsp; return; }
            x.CutAt = dsp;
            x.End = Math.Min(x.End, dsp + 0.035);
            x.Src.SetScheduledEndTime(x.End);
        }

        void Fire(string cat, IList<string> list, Opts o)
        {
            if (list == null || list.Count == 0 || SfxVolume <= 0 || sfxIdx == null) return;
            double t = o.HitAt ?? Now + Math.Max(0, o.DelayMs);
            if (o.Tag != null && lastAt.TryGetValue(o.Tag, out var lt) && Math.Abs(t - lt) < o.Gap) return;
            string prev = o.Tag != null && lastPath.TryGetValue(o.Tag, out var pp) ? pp : null;
            var cand = list.Count > 1 && prev != null ? list.Where(p => p != prev).ToList() : list.ToList();
            if (cand.Count == 0) cand = list.ToList();
            var path = cand[UnityEngine.Random.Range(0, cand.Count)];
            if (lastAt.TryGetValue(path, out var lp) && Math.Abs(t - lp) < SAME_MS) return;
            if (o.Tag != null) { lastAt[o.Tag] = t; lastPath[o.Tag] = path; }
            lastAt[path] = t;
            float busy = Crowd(o.Tag ?? path, t, !o.Pitch);
            var clip = Clip(path);
            if (clip == null) return;
            var info = sfxIdx.TryGetValue(path, out var inf) ? inf : new Info { D = clip.length };
            float vol = Mathf.Clamp(o.V * info.G * busy * (o.Pitch ? 0.94f + UnityEngine.Random.value * 0.06f : 1), 0, 2);
            double dsp = AudioSettings.dspTime;
            all.RemoveAll(x => x.End <= dsp);
            if (all.Count >= LIVE_MAX)
            {
                var w = all[0];
                foreach (var x in all) if (x.Vol < w.Vol || (x.Vol == w.Vol && x.Start < w.Start)) w = x;
                if (vol < w.Vol) { Dropped++; return; }
                CutOff(w); all.Remove(w); Dropped++;
            }
            int cap = CAP.TryGetValue(cat, out var c) ? c : 3;
            var q = all.Where(x => x.Cat == cat).OrderBy(x => x.Start).ToList();
            while (q.Count >= cap) { var old = q[0]; q.RemoveAt(0); CutOff(old); all.Remove(old); }
            var src = FreeSource();
            if (src == null) { Dropped++; return; }
            float rate = o.Pitch ? 1 + (UnityEngine.Random.value - 0.5f) * SfxMap.JITTER * 2 : 1;
            float lead = info.Lead;
            float on = o.HitAt.HasValue ? Mathf.Max(0, info.Onset - lead) / rate : 0;
            double wait = o.HitAt.HasValue ? o.HitAt.Value - Now - on * 1000 : o.DelayMs;
            double start = dsp + Math.Max(0, wait) / 1000.0 + 0.005;
            float full = (clip.length - lead) / rate, len = o.Max > 0 ? Mathf.Min(full, o.Max) : full;
            src.clip = clip;
            src.pitch = rate;
            src.panStereo = Mathf.Clamp(o.Pan, -1, 1);
            src.volume = Mathf.Min(1, vol * SfxVolume);
            src.timeSamples = Mathf.Clamp((int)(lead * clip.frequency), 0, Math.Max(0, clip.samples - 1));
            src.PlayScheduled(start);
            var e = new Live { Src = src, Vol = vol, Pitch = rate, Start = start, End = start + len, At = t, Cat = cat, Fade = len < full ? Mathf.Min(o.Fade, len * 0.5f) : 0 };
            if (len < full) src.SetScheduledEndTime(e.End);
            all.Add(e);
            Trace.Add($"{path} v{vol:0.00} len{len:0.00} lead{lead * 1000:0}ms on{on * 1000:0}ms live{all.Count}");
            if (Trace.Count > 60) Trace.RemoveAt(0);
        }

        AudioSource FreeSource()
        {
            double dsp = AudioSettings.dspTime;
            foreach (var s in pool) if (!s.isPlaying && !all.Any(x => x.Src == s && x.End > dsp)) return s;
            return null;
        }

        // 꼬리 줄이기 · 끊기 — 프레임마다 크기 곡선을 맞춘다
        void Update()
        {
            double dsp = AudioSettings.dspTime;
            foreach (var x in all)
            {
                if (dsp < x.Start || dsp > x.End) continue;
                float k = 1;
                if (x.CutAt >= 0) k = Mathf.Clamp01((float)((x.End - dsp) / Math.Max(1e-3, x.End - x.CutAt)));
                else if (x.Fade > 0 && dsp > x.End - x.Fade) k = Mathf.Clamp01((float)((x.End - dsp) / x.Fade));
                x.Src.volume = Mathf.Min(1, x.Vol * SfxVolume) * k;
            }
            all.RemoveAll(x => x.End <= dsp - 0.05);
        }

        // ── 공용 갈래 ──
        public static void Play(string kind, Opts opts = null)
        {
            var me = Ensure();
            if (sfxIdx == null || kind == null) return;
            opts = opts ?? new Opts();
            var k = SfxMap.ALIAS.TryGetValue(kind, out var al) ? al : kind;
            if (!SfxMap.SFX.TryGetValue(k, out var e)) return;
            var cat = k.Split('.')[0];
            var tl = SfxMap.TAIL.TryGetValue(k, out var t1) ? t1 : SfxMap.TAIL.TryGetValue(cat, out var t2) ? t2 : new[] { 0f, 0.15f };
            var o = opts.With(x =>
            {
                x.Gap = e.Gap; x.Pitch = e.Jitter; x.Max = e.Max >= 0 ? e.Max : tl[0]; x.Fade = e.Fade >= 0 ? e.Fade : tl[1];
                x.V = e.V * opts.V; x.Tag = k;
            });
            me.Fire(cat, e.F, o);
        }

        static string HeroDir(string key)
        {
            if (key == null || heroSfx == null) return null;
            if (heroSfx.TryGetValue(key, out var d)) return d;
            return art != null && art.TryGetValue(key, out d) ? d : null;
        }

        static Dictionary<string, List<string>> KindsOf(string top, string dir, Dictionary<string, Regex> kinds)
        {
            var id = top + "/" + dir;
            if (!dirCache.TryGetValue(id, out var r)) dirCache[id] = r = dir != null ? SfxMap.KindsIn(sfxIdx.Keys, top, dir, kinds) : new Dictionary<string, List<string>>();
            return r;
        }

        static void Own(string top, string who, string kind, Dictionary<string, Regex> kinds, Opts opts)
        {
            var me = Ensure();
            if (sfxIdx == null) return;
            opts = opts ?? new Opts();
            var d = top == "hero" ? HeroDir(who) : SfxMap.MonsterDir(who, monsterDirs);
            List<string> list = null;
            if (d != null) KindsOf(top, d, kinds).TryGetValue(kind, out list);
            var tl = SfxMap.KIND_TAIL.TryGetValue(kind, out var t) ? t : new[] { 0f, 0.15f };
            if (list != null && list.Count > 0)
            {
                var pitch = Regex.IsMatch(kind, "Hit$|^hit$");
                me.Fire(top, list, opts.With(x =>
                {
                    x.Pitch = pitch; x.Max = tl[0]; x.Fade = tl[1];
                    x.V = (SfxMap.KIND_V.TryGetValue(kind, out var kv) ? kv : 1) * opts.V; x.Tag = $"{top}:{d}:{kind}";
                }));
                return;
            }
            if (SfxMap.FALLBACK.TryGetValue(kind, out var fb)) Play(fb, opts);
        }

        public static void Hero(string key, string kind, Opts opts = null) => Own("hero", key, kind, SfxMap.HERO_KINDS, opts);
        public static void Enemy(string key, string kind, Opts opts = null) => Own("monster", key, kind, SfxMap.ENEMY_KINDS, opts);
        public static void At(string key, string kind, float ms, Opts opts = null) => Own("hero", key, kind, SfxMap.HERO_KINDS, (opts ?? new Opts()).With(x => x.HitAt = Now + Math.Max(0, ms)));

        public static bool HasKind(string key, string kind)
        {
            Ensure();
            var d = sfxIdx != null ? HeroDir(key) : null;
            return d != null && KindsOf("hero", d, SfxMap.HERO_KINDS).TryGetValue(kind, out var l) && l.Count > 0;
        }

        static SfxMap.Slots SlotsOf(string key, string group)
        {
            var d = HeroDir(key);
            if (d == null || sfxIdx == null) return new SfxMap.Slots();
            var id = d + "|" + group;
            if (!slotCache.TryGetValue(id, out var s)) slotCache[id] = s = SfxMap.SlotsIn(sfxIdx.Keys, d, group);
            return s;
        }

        // 동작 하나의 소리 — evs: 그 동작의 스파인 SFX 이벤트(칸 번호 · 지금부터 ms). 칸 수가 파일 수와 맞으면 칸마다 그 파일을 그 시각에
        // (맞는 소리 칸은 건너뛴다 — 맞는 순간에 Land 가 낸다). 안 맞으면 cast 갈래를 지금, impact 갈래를 첫 마루가 impactMs 에 오도록.
        // 무엇으로 틀었는지 돌려준다("events" · "fallback")
        public static string Action(string heroKey, string group, IList<SoundEvent> evs, string cast = null, string[] impact = null, float impactMs = 0, float v = 1)
        {
            var me = Ensure();
            if (sfxIdx == null || heroKey == null) return null;
            var m = evs != null && evs.Count > 0 && SfxMap.SLOT_GROUPS.ContainsKey(group) ? SfxMap.SlotMap(SlotsOf(heroKey, group), evs.Select(e => e.N)) : null;
            if (m != null)
            {
                var tl = SfxMap.KIND_TAIL.TryGetValue(group, out var t) ? t : new[] { 0f, 0.15f };
                float kv = SfxMap.KIND_V.TryGetValue(group, out var k) ? k : 1;
                foreach (var e in evs)
                    if (m.TryGetValue(e.N, out var p) && p != "hit")
                        me.Fire("hero", new[] { p }, new Opts { V = v * kv, DelayMs = e.T, Max = tl[0], Fade = tl[1], Tag = "slot:" + p + ":" + e.T });
                return "events";
            }
            if (cast != null) Hero(heroKey, cast, new Opts { V = v });
            if (impact != null && impact.Length > 0)
            {
                var kk = impact.FirstOrDefault(x => HasKind(heroKey, x)) ?? impact[0];
                At(heroKey, kk, impactMs, new Opts { V = v });
            }
            return "fallback";
        }

        // ── 전투 한 줄짜리 ──
        // 카드 종류 소리의 이름 — 스킬은 하는 일로 가른다(회복 · 방어 · 그 밖)
        public static string CardKey(MotionCard c)
        {
            if (c == null) return "card.스킬";
            if (c.Type != "스킬") return "card." + c.Type;
            bool Has(params string[] k) => c.Fx != null && c.Fx.Any(f => k.Contains(f.K));
            if (Has("dmg")) return "card.스킬";
            if (Has("heal", "healMod")) return "card.회복";
            if (Has("block", "shield", "invuln", "blockAll", "blockAlly", "takenMod", "defMod")) return "card.방어";
            if (Has("cleanse")) return "card.회복";
            return "card.스킬";
        }

        // 카드를 냈다 — 날아가는 소리 + (동작이 없으면) 종류 소리
        public static void Card(MotionCard c, string heroKey, bool motion = false)
        {
            if (c == null) return;
            Play("card.play");
            if (motion) return;
            if (c.Type == "공격" && heroKey != null) Hero(heroKey, "attack", new Opts { V = 0.8f });
            else Play(CardKey(c));
        }

        static readonly HashSet<string> BAD = new HashSet<string> { "취약", "약화", "감전", "중독", "기절", "침묵", "봉인", "출혈", "화상", "수은", "도발" };

        // 맞는 순간 — k: hurt · heal · block · shield · status · die. side: "party" · "enemy"
        public struct HitInfo { public string K, Side, Id; public int V; public bool Crit; public bool? Up; }

        public static void Land(HitInfo h, string hero = null, string enemy = null, bool ult = false, bool heavy = false, string group = null, bool heroMagic = false)
        {
            if (Ensure() == null || sfxIdx == null) return;
            var opts = new Opts { Pan = h.Side == "party" ? -0.25f : h.Side == "enemy" ? 0.25f : 0 };
            switch (h.K)
            {
                case "die": Play(h.Side == "enemy" ? "death.enemy" : "death.hero", opts); return;
                case "heal": Play("heal", opts); return;
                case "block": case "shield": Play("block.gain", opts); return;
                case "status":
                    if (h.Id == "기절") { Play("status.stun", opts); return; }
                    if (h.Id == "화상") { Play("status.burn", opts); return; }
                    Play(h.Up == false ? "debuff" : (h.Up == true || !BAD.Contains(h.Id ?? "")) ? "buff" : "debuff", opts);
                    return;
            }
            if (h.K != "hurt") return;
            if (h.V == 0) { Play("block.hit", opts); return; }
            if (enemy != null) Enemy(enemy, "hit", opts);
            else if (hero != null)
            {
                string type = heroMagic ? "hit.magic" : "hit.slash";
                string mine = ult ? "ultHit" : new[] { group == "power" ? "powerHit" : null, group == "skill" ? "skillHit" : null, "attackHit" }.FirstOrDefault(k => k != null && HasKind(hero, k));
                if (mine != null && HasKind(hero, mine)) Hero(hero, mine, opts);
                else Play(ult && !HasKind(hero, "ult2") && !HasKind(hero, "ultBoom") ? "ult.impact" : type, opts);
            }
            else Play("hit.small", opts);
            if (h.Crit) Play("hit.crit", opts.With(x => x.DelayMs = 20));
            if (heavy && !ult) Play("hit.heavy", opts.With(x => { x.DelayMs = 30; x.V = 0.7f; }));
        }

        // 고학년 — 컷인(원작 「사도 부르기」 + 시전 소리) · 터지는 순간
        public static void Ult(string heroKey, string phase = "cutin")
        {
            if (phase == "cutin") { Play("ult.cutin"); Hero(heroKey, "ult", new Opts { DelayMs = 250 }); }
            else Hero(heroKey, "ult2");
        }

        public static void StopAll()
        {
            if (inst == null) return;
            foreach (var s in inst.pool) s.Stop();
            inst.all.Clear();
        }

        // 걸어 두고 아직 안 울린 것만 걷는다 — 몸짓을 걷을 때 늦게 터지는 소리가 남지 않게
        public static void StopPending()
        {
            if (inst == null) return;
            double dsp = AudioSettings.dspTime;
            foreach (var x in inst.all.Where(x => x.Start > dsp).ToList()) { x.Src.Stop(); inst.all.Remove(x); }
        }

        public static int LiveCount { get { if (inst == null) return 0; double d = AudioSettings.dspTime; return inst.all.Count(x => x.End > d && x.Start <= d); } }

        // ── 목소리 (웹판 js/voice.js) ──
        public static string VoiceDir(string key)
        {
            Ensure();
            if (heroVoice != null && key != null && heroVoice.TryGetValue(key, out var d)) return d;
            return art != null && key != null && art.TryGetValue(key, out d) && voiceIdx.ContainsKey(d) ? d : null;
        }

        // 갈래 목록에서 앞에서부터 있는 것 하나. 방금 튼 것은 피하고, 하나뿐이면 12초 안에는 안 튼다(repeat 면 상관없이)
        public static AudioSource Speak(string key, string[] cats, bool repeat = false)
        {
            var me = Ensure();
            if (cats == null || cats.Length == 0 || VoiceVolume <= 0 || voiceIdx == null) return null;
            var dir = VoiceDir(key);
            if (dir == null || !voiceIdx.TryGetValue(dir, out var set)) return null;
            List<string> pool = null;
            foreach (var c in cats) if (set.TryGetValue(c, out var l) && l.Count > 0) { pool = l; break; }
            if (pool == null) return null;
            double now = Time.realtimeSinceStartupAsDouble * 1000;
            if (!repeat)
            {
                if (pool.Count > 1) pool = pool.Where(f => f != me.lastVoice).ToList();
                else if (pool[0] == me.lastVoice && now - me.lastVoiceAt < NO_REPEAT_MS) return null;
            }
            var f0 = pool[UnityEngine.Random.Range(0, pool.Count)];
            var clip = Resources.Load<AudioClip>(Root + "/voice/" + f0);
            if (clip == null) return null;
            me.voice.Stop();
            me.voice.clip = clip;
            me.voice.volume = VoiceVolume;
            me.voice.Play();
            me.lastVoice = f0; me.lastVoiceAt = now;
            return me.voice;
        }

        public static AudioSource Speak(string key, params string[] cats) => Speak(key, cats, false);
        // 동작 이름에 맞는 목소리(MotionVoice)
        public static AudioSource SpeakFor(string key, string anim) => Speak(key, MotionVoice.CatsFor(anim), false);
        public static void StopVoice() { if (inst != null) inst.voice.Stop(); }
        public static bool VoicePlaying => inst != null && inst.voice.isPlaying;
    }
}
