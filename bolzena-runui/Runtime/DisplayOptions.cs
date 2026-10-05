using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Bolzena.RunUI
{
    /// <summary>
    /// 화면 설정 — 창 모드 · 해상도 · 프레임 제한 · 수직동기. 판 화면 설정 창과 전투 화면 설정 창이 함께 쓴다(값은 PlayerPrefs 한 벌).
    ///   화면 모드(다른 PC 게임과 같은 세 가지): 0 창 모드 · 1 테두리 없는 창 모드(FullScreenWindow — 기본값) · 2 전체 화면(ExclusiveFullScreen — Windows 만)
    ///   테두리 없는 창 모드는 늘 모니터를 꽉 채운다 — 해상도를 고르면 그리는 크기(렌더 해상도)만 바뀐다(유니티 동작).
    ///   해상도: 0 모니터 해상도 · 1 HD 1280×720 · 2 FHD 1920×1080 · 3 WQHD 2560×1440 · 4 4K (UHD) 3840×2160
    ///   모니터보다 큰 해상도는 잠긴다(Fits). 창 모드에서는 고른 크기가 작업 영역을 넘으면 비율을 지켜 줄여서 가운데에 놓는다.
    /// 부팅 때 LoadAndApply() 를 한 번 부른다. 명령줄에 -screen-width · -screen-height · -screen-fullscreen · -window-mode · -popupwindow
    /// 가 있으면 해상도는 명령줄을 따른다(프레임 설정만 적용).
    /// 해상도를 바꿔 보고 되돌릴 수 있게: Try → (Keep | Revert). Apply 는 바로 저장한다.
    /// </summary>
    public static class DisplayOptions
    {
        public enum WindowMode { Windowed = 0, Borderless = 1, Exclusive = 2 }

        public readonly struct ModeInfo
        {
            public readonly WindowMode Mode;
            public readonly string Name;
            public readonly FullScreenMode Unity;
            public ModeInfo(WindowMode m, string name, FullScreenMode u) { Mode = m; Name = name; Unity = u; }
        }

        public readonly struct Preset
        {
            /// <summary>짧은 이름(칸 위) — 「모니터」 · 「HD」 · 「FHD」 · 「WQHD」 · 「4K (UHD)」.</summary>
            public readonly string Name;
            /// <summary>0 이면 모니터 해상도(Native).</summary>
            public readonly int Width, Height;
            public bool Native => Width == 0;
            public Preset(string name, int w, int h) { Name = name; Width = w; Height = h; }
        }

        /// <summary>화면 모드 세 가지(순서 = modeIndex). 전체 화면(독점)은 Windows 가 아니면 ModeAvailable 이 false.</summary>
        public static readonly IReadOnlyList<ModeInfo> Modes = new[]
        {
            new ModeInfo(WindowMode.Windowed, "창 모드", FullScreenMode.Windowed),
            new ModeInfo(WindowMode.Borderless, "테두리 없는 창 모드", FullScreenMode.FullScreenWindow),
            new ModeInfo(WindowMode.Exclusive, "전체 화면", FullScreenMode.ExclusiveFullScreen),
        };

        /// <summary>해상도 다섯(순서 = presetIndex). 0 은 모니터 해상도.</summary>
        public static readonly IReadOnlyList<Preset> Presets = new[]
        {
            new Preset("모니터", 0, 0),
            new Preset("HD", 1280, 720),
            new Preset("FHD", 1920, 1080),
            new Preset("WQHD", 2560, 1440),
            new Preset("4K (UHD)", 3840, 2160),
        };

        /// <summary>프레임 제한(순서 = fpsIndex). 0 은 제한 없음.</summary>
        public static readonly IReadOnlyList<int> FrameCaps = new[] { 30, 60, 120, 144, 0 };
        public static string FrameCapName(int fpsIndex) => FrameCaps[Mathf.Clamp(fpsIndex, 0, FrameCaps.Count - 1)] is var f && f > 0 ? f.ToString() : "제한 없음";

        /// <summary>화면 모드 · 해상도 · 프레임 · 수직동기 가운데 무엇이든 바뀌면(되돌릴 때도).</summary>
        public static event Action Changed;

        /// <summary>기본 화면 모드 — 테두리 없는 창 모드 · 모니터 해상도.</summary>
        public const int DefaultMode = 1, DefaultPreset = 0;

        /// <summary>화면 모드 한 줄 설명(설정 창 아래 줄).</summary>
        public static string ModeHint(int modeIndex) => modeIndex switch
        {
            0 => "고른 해상도 크기의 창 · 모니터보다 크면 맞춰 줄입니다",
            1 => "모니터를 꽉 채운 채 그리는 해상도만 바뀝니다 · 알트탭이 빠릅니다",
            _ => "모니터를 그 해상도로 바꿔 차지합니다 · 알트탭이 느립니다",
        };

        const string KMode = "bz.disp.mode", KPreset = "bz.disp.preset", KFps = "bz.disp.fps", KVsync = "bz.disp.vsync";

        // ── 지금 값 ──
        /// <summary>지금 창 모드(실제 화면에서 읽는다 — 최대화 창은 전체화면으로 본다).</summary>
        public static int ModeIndex => Screen.fullScreenMode switch
        {
            FullScreenMode.Windowed => 0,
            FullScreenMode.ExclusiveFullScreen => 2,
            _ => 1,
        };

        /// <summary>지금 해상도 칸 — 저장된 것이 있으면 그것, 없으면 지금 크기와 같은 칸, 그도 없으면 -1(명령줄 · 손으로 늘린 창).</summary>
        public static int PresetIndex
        {
            get
            {
                if (trial) return trialPreset;
                if (PlayerPrefs.HasKey(KPreset)) return Mathf.Clamp(PlayerPrefs.GetInt(KPreset), 0, Presets.Count - 1);
                var m = Monitor;
                if (Screen.width == m.x && Screen.height == m.y) return 0;
                for (int i = 1; i < Presets.Count; i++) if (Presets[i].Width == Screen.width && Presets[i].Height == Screen.height) return i;
                return -1;
            }
        }

        public static int FpsIndex => Mathf.Clamp(PlayerPrefs.GetInt(KFps, 1), 0, FrameCaps.Count - 1);
        public static bool VSync => PlayerPrefs.GetInt(KVsync, 0) == 1;

        /// <summary>모니터 해상도(창이 있는 모니터).</summary>
        public static Vector2Int Monitor
        {
            get
            {
                if (Web) return new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);   // 웹은 mainWindowDisplayInfo 가 없다(예외)
                var d = Screen.mainWindowDisplayInfo;
                if (d.width > 0 && d.height > 0) return new Vector2Int(d.width, d.height);
                var r = Screen.currentResolution;
                return new Vector2Int(r.width, r.height);
            }
        }

        /// <summary>해상도 칸의 실제 크기(모니터 칸은 모니터 크기).</summary>
        public static Vector2Int SizeOf(int presetIndex)
        {
            var p = Presets[Mathf.Clamp(presetIndex, 0, Presets.Count - 1)];
            return p.Native ? Monitor : new Vector2Int(p.Width, p.Height);
        }

        /// <summary>이 해상도를 고를 수 있나 — 모니터보다 크면 false(잠금).</summary>
        public static bool Fits(int presetIndex)
        {
            if (presetIndex < 0 || presetIndex >= Presets.Count) return false;
            var s = SizeOf(presetIndex); var m = Monitor;
            return s.x <= m.x && s.y <= m.y;
        }

        /// <summary>이 창 모드를 쓸 수 있나 — 독점 전체화면은 Windows 만.</summary>
        public static bool ModeAvailable(int modeIndex)
        {
            if (modeIndex < 0 || modeIndex >= Modes.Count) return false;
            if (Modes[modeIndex].Mode != WindowMode.Exclusive) return true;
            return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor;
        }

        /// <summary>지금 화면 한 줄 — 「1920×1080 · 전체화면」.</summary>
        public static string Describe() => Web ? $"{Screen.width}×{Screen.height} · {(Screen.fullScreen ? "전체 화면" : "브라우저 창")}"
                                               : $"{Screen.width}×{Screen.height} · {Modes[ModeIndex].Name}";

        // ── 웹(WebGL) — 창 모드 · 해상도는 브라우저가 정한다. 설정 창은 「전체 화면」 켜고 끄기만 보인다 ──
        /// <summary>웹 빌드인가 — 설정 창이 창 모드 · 해상도 칸을 숨기고 전체 화면 스위치를 낸다.</summary>
        public static bool Web => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>웹 전체 화면 켜기 · 끄기(브라우저는 누른 그 순간에만 허락한다 — 유니티가 다음 입력 때 건다).</summary>
        public static void SetWebFullScreen(bool on)
        {
            Screen.fullScreen = on;
            Run(Notify());
        }
        static IEnumerator Notify() { yield return null; yield return null; Changed?.Invoke(); }

        // ── 바꾸기 ──
        /// <summary>창 모드 · 해상도를 바로 적용하고 저장한다. 못 쓰는 모드 · 모니터보다 큰 해상도면 false(아무것도 안 바꾼다).</summary>
        public static bool Apply(int modeIndex, int presetIndex)
        {
            if (!SetScreen(modeIndex, presetIndex)) return false;
            trial = false;
            PlayerPrefs.SetInt(KMode, modeIndex);
            PlayerPrefs.SetInt(KPreset, presetIndex);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>시험 적용 — 저장하지 않고 바꾼다. 앞 상태를 기억해 두었다가 Revert 로 돌아간다(Keep 이면 저장).
        /// 설정 창은 이것을 부르고 「10초 안에 확인」 창을 띄운다 — 모니터가 못 받는 모드에서도 시간이 지나면 돌아오게.</summary>
        public static bool Try(int modeIndex, int presetIndex)
        {
            var before = new Snapshot
            {
                W = Screen.width, H = Screen.height, Mode = Screen.fullScreenMode,
                HadSaved = PlayerPrefs.HasKey(KPreset), SavedMode = PlayerPrefs.GetInt(KMode, DefaultMode), SavedPreset = PlayerPrefs.GetInt(KPreset, DefaultPreset),
            };
            if (!SetScreen(modeIndex, presetIndex)) return false;
            if (!trial) snap = before;     // 시험 중에 또 바꾸면 처음 상태로 돌아가게
            trial = true; trialMode = modeIndex; trialPreset = presetIndex;
            Changed?.Invoke();
            return true;
        }

        /// <summary>시험 중인가(Try 뒤 Keep · Revert 전).</summary>
        public static bool Pending => trial;

        /// <summary>시험 적용을 그대로 둔다(저장).</summary>
        public static void Keep()
        {
            if (!trial) return;
            trial = false;
            PlayerPrefs.SetInt(KMode, trialMode);
            PlayerPrefs.SetInt(KPreset, trialPreset);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>시험 적용 전으로 돌아간다(크기 · 창 모드 · 저장값 모두).</summary>
        public static void Revert()
        {
            if (!trial) return;
            trial = false;
            var s = snap;
            Screen.SetResolution(s.W, s.H, s.Mode);
            if (s.Mode == FullScreenMode.Windowed) Run(CenterWindow(new Vector2Int(s.W, s.H)));
            if (s.HadSaved) { PlayerPrefs.SetInt(KMode, s.SavedMode); PlayerPrefs.SetInt(KPreset, s.SavedPreset); }
            else { PlayerPrefs.DeleteKey(KMode); PlayerPrefs.DeleteKey(KPreset); }
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>프레임 제한(FrameCaps 의 칸) — 바로 적용 · 저장. 수직동기가 켜져 있으면 수직동기가 이긴다.</summary>
        public static void SetFrameCap(int fpsIndex)
        {
            PlayerPrefs.SetInt(KFps, Mathf.Clamp(fpsIndex, 0, FrameCaps.Count - 1));
            PlayerPrefs.Save();
            ApplyFrames();
            Changed?.Invoke();
        }

        /// <summary>수직동기 — 바로 적용 · 저장.</summary>
        public static void SetVSync(bool on)
        {
            PlayerPrefs.SetInt(KVsync, on ? 1 : 0);
            PlayerPrefs.Save();
            ApplyFrames();
            Changed?.Invoke();
        }

        /// <summary>부팅 때 한 번 — 저장된 창 모드 · 해상도(명령줄이 정했으면 건너뜀)와 프레임 · 수직동기를 적용한다.</summary>
        public static void LoadAndApply()
        {
            ApplyFrames();
            if (Web || CommandLineSetsScreen()) return;   // 웹은 브라우저 크기를 따른다
            // 저장이 없으면 기본(테두리 없는 창 모드 · 모니터 해상도) — 저장하지는 않는다
            int mode = Mathf.Clamp(PlayerPrefs.GetInt(KMode, DefaultMode), 0, Modes.Count - 1);
            int preset = Mathf.Clamp(PlayerPrefs.GetInt(KPreset, DefaultPreset), 0, Presets.Count - 1);
            if (!ModeAvailable(mode)) mode = 1;
            if (!Fits(preset)) preset = 0;          // 모니터를 바꿨다 — 모니터 해상도로
            var want = Target(mode, preset);
            if (Screen.width == want.x && Screen.height == want.y && Screen.fullScreenMode == Modes[mode].Unity) return;
            SetScreen(mode, preset);
        }

        // ── 속 ──
        struct Snapshot { public int W, H; public FullScreenMode Mode; public bool HadSaved; public int SavedMode, SavedPreset; }
        static Snapshot snap;
        static bool trial;
        static int trialMode, trialPreset;

        static void ApplyFrames()
        {
            if (VSync) { QualitySettings.vSyncCount = 1; Application.targetFrameRate = -1; }
            else
            {
                QualitySettings.vSyncCount = 0;
                int cap = FrameCaps[FpsIndex];
                Application.targetFrameRate = cap > 0 ? cap : -1;
            }
        }

        /// <summary>실제로 세울 크기 — 창 모드는 작업 영역 안으로(제목 줄 몫을 빼고) 비율을 지켜 줄인다.</summary>
        static Vector2Int Target(int modeIndex, int presetIndex)
        {
            var s = SizeOf(presetIndex);
            if (Modes[modeIndex].Mode != WindowMode.Windowed) return s;
            var wa = WorkArea();
            float k = Mathf.Min(1f, (wa.width - 16f) / s.x, (wa.height - 48f) / s.y);
            return k >= 1f ? s : new Vector2Int(Mathf.FloorToInt(s.x * k), Mathf.FloorToInt(s.y * k));
        }

        static RectInt WorkArea()
        {
            if (Web) { var w = Monitor; return new RectInt(0, 0, w.x, w.y); }
            var d = Screen.mainWindowDisplayInfo;
            if (d.workArea.width > 0 && d.workArea.height > 0) return d.workArea;
            var m = Monitor;
            return new RectInt(0, 0, m.x, m.y);
        }

        static bool SetScreen(int modeIndex, int presetIndex)
        {
            if (!ModeAvailable(modeIndex) || !Fits(presetIndex)) return false;
            var size = Target(modeIndex, presetIndex);
            var mode = Modes[modeIndex].Unity;
            Debug.Log($"[Display] {size.x}×{size.y} · {Modes[modeIndex].Name} ({Presets[presetIndex].Name})");
            Screen.SetResolution(size.x, size.y, mode);
            if (mode == FullScreenMode.Windowed) Run(CenterWindow(size));
            return true;
        }

        // 창은 크기가 바뀐 다음 프레임에 작업 영역 가운데로 옮긴다(넘치면 왼쪽 위에 붙인다)
        static IEnumerator CenterWindow(Vector2Int size)
        {
            yield return null;
            yield return null;
            if (Screen.fullScreenMode != FullScreenMode.Windowed) yield break;
            var d = Screen.mainWindowDisplayInfo;
            var wa = WorkArea();
            int x = wa.x + Mathf.Max(0, (wa.width - size.x) / 2);
            int y = wa.y + Mathf.Max(32, (wa.height - size.y) / 2);
            Screen.MoveMainWindowTo(d, new Vector2Int(x, y));
        }

        static bool CommandLineSetsScreen()
        {
            var a = Environment.GetCommandLineArgs();
            foreach (var s in a)
                if (s == "-screen-width" || s == "-screen-height" || s == "-screen-fullscreen" || s == "-window-mode" || s == "-popupwindow") return true;
            return false;
        }

        // 코루틴 실행기 — 숨은 오브젝트 하나(장면이 바뀌어도 남는다)
        sealed class Runner : MonoBehaviour { }
        static Runner runner;
        static void Run(IEnumerator co)
        {
            if (!Application.isPlaying) return;
            if (runner == null)
            {
                var go = new GameObject("DisplayOptions") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            runner.StartCoroutine(co);
        }
    }
}
