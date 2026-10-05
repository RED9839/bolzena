# com.bolzena.fx — 볼제나 연출 자료 (트랙 B)

웹판 볼제나가 사도마다 쓰는 원작 연출 자료를 유니티에서 쓰게 옮긴 UPM 로컬 패키지. API 는 [Docs/API.md](Docs/API.md).

- **원작 이펙트** — 웹판 `assets/fx/*/fx.json`(원작 ParticleSystem 을 2D 로 떨군 값) → `FxEffect` 자산(에디터 일괄 변환),
  웹판 `js/fx-burst.js` 의 시뮬레이션을 C# 으로 그대로 옮긴 재생기 `FxRun`(메시 하나 · 텍스처마다 서브메시 · 미리 곱한 알파 · HDR 색).
  구운 낱장(`assets/fx-baked`, 18개)은 `FxSheet` — 웹판 TUNE(scale · dx · dy · reach · fadeFrom · post)까지.
- **고학년 차례** — `UltFx`(웹판 ultPlan · playUltFx): 이름 낱말로 시전자/대상/투사체 · 준비/본 · 레이저 다시 틀기 · 총구.
- **모션 표** — `CardMotion`(card-motion.js) · `MotionVoice`(motion-voice.js) · `MotionTables`(DASH · MUZZLE · HIT_FX · 타격 갈래),
  `SpineMotion`(waysOf · strikeOf · planAct · boneScreen).
- **소리** — `BolzenaAudio`(sfx.js · voice.js 규칙, AudioSource 풀 · 믹서 Sfx[압축기]/Voice) · `SfxMap`(sfx-map.js).

원작 에셋은 이 패키지에 없다 — `Tools/fx_prepare.py` 가 쓰는 프로젝트의 `Assets/BolzenaFxData`(gitignore)로 옮기고,
`Bolzena.Fx.EditorTools.FxImport.ImportAll` 이 자산을 만든다. 필요: Python 3 · Pillow · numpy · soundfile.

## 웹판과 다른 점
- 색이 HDR 그대로라 블룸이 먹는다(웹판 캔버스는 1 에서 잘렸다). 더하기 입자는 `BolzenaFx.AddBoost`(1.25)만큼 더 밝게.
- 화면 입자 상한 2000(웹판 600).
- 구운 낱장 칸의 위아래(와 늘인 레이저 끝)를 22% 걷어 네모 테두리가 덜 보인다.
- Linear 색 공간 — 꼭짓점 색은 감마→선형으로 바꿔 넘긴다(1 넘는 HDR 몫은 그대로).
- 입체 메시 이미터(654개)는 웹판처럼 뺀다. 셰이더 그래프(디졸브 · 노이즈 · UV 흐름)는 원작 추출에서 이미 빠져 구운 그림(_lum · _toon · _dslv)으로 근사.
