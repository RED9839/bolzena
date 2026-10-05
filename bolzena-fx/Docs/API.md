# com.bolzena.fx — API (트랙 C 가 붙일 것)

웹판(C:\projects\볼제나)의 연출 자료를 유니티로 옮긴 패키지. 어셈블리 셋:

| 어셈블리 | 들어 있는 것 | 의존 |
|---|---|---|
| `Bolzena.Fx` | 이펙트 재생기 · 고학년 차례 · 카드 모션 · 목소리 · DASH · 총구 · 타격 갈래 표 · BolzenaAudio | 없음 |
| `Bolzena.Fx.Spine` | 스파인 이벤트로 몸짓 · 때리는 순간 · 소리 칸 · 총구 본 | spine-csharp · spine-unity(Assets/Spine) |
| `Bolzena.Fx.Editor` | 원작 이펙트 일괄 변환(FxImport) · 가져오기 설정 · 믹서 | 에디터 전용 |

네임스페이스는 모두 `Bolzena.Fx`. 시범의 `Bolzena.View.Fx` 와 이름이 겹치지 않게 정적 진입점은 **`BolzenaFx`** 다.

## 0. 준비(한 번, 쓰는 프로젝트에서)

```sh
python <bolzena-fx>/Tools/fx_prepare.py --project <유니티 프로젝트> --heroes all   # 이펙트 · 구운 낱장(webp→png) · 소리(Opus→WAV) · 소리 색인
"Unity.exe" -batchmode -quit -projectPath <프로젝트> -executeMethod Bolzena.Fx.EditorTools.FxImport.ImportAll
```
- 결과는 `Assets/BolzenaFxData/`(원작 에셋 — **.gitignore 에 넣을 것**).
  - `Resources/BolzenaFx/FxLibrary.asset` 색인 · `Effects/<이름>.asset`(FxEffect) · `Baked/<이름>.asset`(FxSheet) · `FxParticle.mat`(셰이더를 빌드에 싣는 본)
  - `Resources/BolzenaAudio/sfx|voice/…wav` · `audio_index.json` · `BolzenaMixer.mixer`(Master ← Sfx[압축기] · Voice)
- `--heroes` 는 소리 · 목소리만 줄인다(이펙트는 늘 전부 — 134명 604개).

## 1. 이펙트

```csharp
using Bolzena.Fx;
BolzenaFx.UnitWorld = 0.16f;            // 원작 1단위 → 월드. 사도 SD 가 0.3 배(키 ≈1.5)인 싸움터에서 웹판과 같은 크기
BolzenaFx.DeltaTime = () => Clock.Dt;   // 전투 시계(히트스톱 · 슬로)를 쓰려면. 기본 Time.deltaTime
BolzenaFx.Rate = 1f;                    // 배속
BolzenaFx.Calm = false;                 // 움직임 줄이기 — 켜면 아무것도 안 튼다
BolzenaFx.AddBoost = 1.25f;             // 더하기 입자 HDR 배율(블룸 문턱 1.15 위만 번진다)

string[] names = FxLibrary.Get("에르핀", "ult");     // 사도 키 또는 원작 그림 이름(erpin)
bool has = FxLibrary.HasUlt("에르핀");
FxLibrary.PreloadUlt("에르핀");                       // 자산만 미리 읽기
int tex = FxLibrary.Prewarm("에르핀");                // 읽기 + 텍스처 GPU 올리기 + 셰이더/블렌드 상태 + 코드(JIT) 데우기(공용 타격 이펙트 포함) — 싸움을 열 때 파티 사도마다
BolzenaAudio.PreloadHero("에르핀");                   // 제 소리 · 고학년 목소리 · 공용 타격 소리 미리 풀기

FxRun r = BolzenaFx.Play("fx_common_hit_3_m", new FxPlayOptions {
    At = feet, Scale = 1f, Flip = false,       // At: 그 유닛의 발밑(원작 이펙트 원점)
    Until = 1.2f,                              // 이 초까지 하고 0.4초 동안 걷힌다
    Center = true,                             // 뿌리에서 멀리 짜인 대상 쪽 이펙트를 가운데로
    Top = cam.orthographicSize - 0.3f,         // 넘지 않을 위끝(월드 y)
    Track = () => muzzleWorld,                 // 매 프레임 따라간다(총구)
    MoveTo = target, MoveDur = 0.35f,          // 투사체
    Order = 300, SortingLayer = null, Parent = fieldRoot,
    Done = run => { },
});
r.Stop(0.4f); bool done = r.IsDone;
```
- 구운 낱장(FxSheet)이 그 이름에 있으면 그것, 없으면 파티클 판(FxEffect)을 튼다(웹판과 같은 순서).
- 공용 타격 이펙트 표: `MotionTables.HIT_FX["slash"|"shot"|"magic"|"blunt"|"big"|"bigBlunt"|"crit"]`,
  갈래 고르기 `MotionTables.HitKindHero(dmgType, backRow)` · `HitKindEnemy(enemyKey)`.

## 2. 고학년 한 벌

```csharp
ActPlan plan = SpineMotion.PlanUlt(skeletonData, "에르핀");   // 갈래(앨리스 불·번개·바람)는 무작위 — way 로 고정 가능
// plan.Anim + plan.Chain 을 차례로 튼다(DASH 고리 횟수까지 펼쳐져 있다)
// plan.S.At 때리는 순간(ms) · plan.S.End 여러 번 때리는 창 끝 · plan.S.Marks 그 사이 이벤트 · plan.S.Total
// plan.Dash (에르핀 · 에르핀_왕도) — Go/Land/Hit/Home(ms) · Cfg.Reach(본) · Cfg.Hop · Cfg.BackMs
// plan.Snd — 스파인 SFX(n) 이벤트 [{N 칸, T ms}] · plan.Pick — 갈래 i/n
int lag = UltFx.LagMs("에르핀", plan.Pick);        // 투사체가 날아가는 몫
int hitAt = plan.S.At + lag + 60;                    // 웹판과 같은 타격 시각(숫자 · 체력 · 피격)
BolzenaFx.PlayUlt(this, "에르핀", new UltOptions {
    From = casterFeet,
    To = targetFeet,      // 단일 대상: 그 대상 발밑 · 전체 공격: 적 무리의 정중앙 · 달리면 부딪치는 자리
    ToWidth = 0f,         // 전체 공격이면 적 무리의 가로 폭(월드) — 대상 쪽 이펙트를 그 폭만큼 벌린다(입자 자리 최대 2배, 구운 낱장 가로 최대 1.3배)
    Impact = plan.S.At, End = plan.S.End, Marks = plan.S.Marks.Select(x => (float)x).ToList(),
    Pick = plan.Pick, Dash = plan.Dash != null,
    Muzzle = SpineMotion.Muzzle(skeletonAnimation, "에르핀"),   // 총구 본(아멜리아 Weapon_main7 끝 · 이름에 muzzle/barrel)
    Top = cam.orthographicSize - 0.3f, Order = 300,
});
// SD 이벤트를 모르면 Impact 를 비우고 UltFx.ImpactMs(heroKey) 를 타격 시각으로(이펙트도 없으면 MotionTables.ULT_HIT)
List<UltPart> parts = UltFx.Plan("에르핀", plan.Pick);   // 어느 이펙트가 시전자/대상/투사체 · 준비/본 · 다시 틀기 · 총구인지(살펴보기)
```

## 성능 기준(시험 프로젝트에서 잼 — 보고 참고)
- 입자 상한 `FxRun.Cap = 1500`(웹판 600), 겹침 상한 `FxRun.OverdrawBudget = 12`(한 프레임에 그리는 큰 입자 — 화면 2% 넘는 것 — 의 넓이×알파 합이 화면 12장 넘으면 그 프레임에서 뺀다. 0 = 끄기). `FxRun.Culled` 로 센다.
- 가비지 없음: 입자는 되쓰고(풀), 메시는 하나를 매 프레임 다시 채운다. `FxRun.CpuMsThisFrame` = 이 프레임에 이펙트가 쓴 CPU.
- 첫 재생 스파이크는 `FxLibrary.Prewarm` + `BolzenaAudio.PreloadHero` 로 없앤다(안 부르면 첫 고학년에 최악 40~100ms 프레임이 났다).
- 이펙트마다 MeshRenderer 하나로 그린다(카메라 무관) — 정렬은 `Order`(sortingOrder) · `SortingLayer`.

## 3. 카드 모션 · 목소리

```csharp
var card = new MotionCard { Id = "에르핀_u1", Type = "공격", Signature = false, Target = null,
    Fx = { new MotionFx { K = "dmg", Ratio = 2.2f, Hits = 1, Target = "oneEnemy" } } };
MotionPick m = CardMotion.Pick(card, role: "딜러", key: "에르핀", has: n => data.FindAnimation(n) != null);
// m.Anim(SD 동작 · null 이면 제자리) · m.Group(attack/power/skill/null — 사도 소리 갈래) · m.Tier · m.CutMs · m.From
ActPlan p = SpineMotion.PlanCard(data, "에르핀", "딜러", card);   // 위 + 스킬 조각 잇기 · 때리는 순간 · 소리 칸
string[] cats = MotionVoice.CatsFor("Ultimate1_1");              // ultimate, shout, anger
List<List<string>> ways = SpineMotion.WaysOf(data, "Ultimate1");  // 조각 갈래
StrikeInfo s = SpineMotion.StrikeOf(data, names);                  // 웹판 strikeOf
Vector3? w = SpineMotion.BoneWorld(skeletonAnimation, "Point_Ult1", tip: false);
```
표 그대로: `CardMotion.LOOK · LOOK_BY_HERO · POWER_NOT_ATTACK · CARD_MOTION · LIGHT_ONE_CUT · HEAVY · AOE_HEAVY · MULTI · BUFF_CUT · SPAWN_FROM`,
`MotionTables.DASH · MUZZLE · MUZZLE_RE · ULT_HIT · HIT_FX`, `MotionVoice.TABLE`.

## 4. 소리 — BolzenaAudio

```csharp
BolzenaAudio.SfxVolume = 0.8f; BolzenaAudio.VoiceVolume = 0.9f;
BolzenaAudio.Configure(sfxGroup, voiceGroup);        // 선택 — 없으면 Resources/BolzenaAudio/BolzenaMixer 를 스스로 찾는다
BolzenaAudio.Play("hit.slash");                       // 공용 갈래(SfxMap.SFX)
BolzenaAudio.Hero("에르핀", "attack");                 // 제 소리 — 없으면 FALLBACK 공용 갈래
BolzenaAudio.Enemy("curburus", "hit");
BolzenaAudio.At("에르핀", "ult2", 1300);               // 큰 소리(첫 마루)가 1.3초 뒤에 오게 당겨 튼다
BolzenaAudio.Action("에르핀", plan.Group, plan.Snd, cast: plan.Group, impact: new[] { "ult2", "ultBoom" }, impactMs: plan.S.At);
BolzenaAudio.Card(card, "에르핀", motion: true);       // 카드 날아가는 소리(동작이 없으면 종류 소리까지)
BolzenaAudio.Land(new BolzenaAudio.HitInfo { K = "hurt", Side = "enemy", V = 33, Crit = true }, hero: "에르핀", ult: true, heavy: false, group: "skill", heroMagic: true);
BolzenaAudio.Ult("에르핀", "cutin");
BolzenaAudio.StopPending(); BolzenaAudio.StopAll();
AudioSource a = BolzenaAudio.Speak("에르핀", "ultimate", "shout");   // 목소리 — 하나씩, 같은 대사 12초 안에 되풀이 안 함
BolzenaAudio.SpeakFor("에르핀", "Ultimate1_1"); BolzenaAudio.StopVoice();
```
규칙(웹판 sfx.js 그대로): 파일별 세기 맞춤 g × 갈래 v · 앞의 고요 건너뛰기 · 꼬리 max 초에서 fade 초 동안 줄여 끊기(TAIL · KIND_TAIL) ·
모두 합쳐 8개(넘치면 가장 작은 · 오래된 것을 끊음) · 갈래별 CAP · 같은 소리 0.5초 안 반복은 0.72배씩(0.45 까지) ·
같은 순간(±70ms) 겹침은 12%씩(0.6 까지, 맞는 소리 제외) · 같은 파일 45ms · 갈래 gap · 맞는 소리만 높이 ±2.5%.
스파인 SFX 칸 ↔ 파일: `SfxMap.SlotsIn · SlotMap`(웹판과 같은 줄 세우기).

## 5. 시험

`C:\projects\bolzena-fx-test` — `Tools/build.sh`(변환+빌드) · `Tools/demo.sh`(15명 고학년 자동 재생 · 캡처) ·
`Tools/web_capture.py`(웹판 같은 고학년) · `Tools/compare.py` · `Tools/montage.py` · `Tools/make_gifs.py`.
