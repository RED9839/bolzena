# 볼제나 — 유니티판

웹판 「볼제나」(C:\projects\볼제나)를 유니티로 옮기는 프로젝트. 지금은 **전투 한 판 시범**(연출 · 화면 · 스파인 · 입력)이다.
전투 규칙은 `IBattle` 뒤의 임시 구현(`PilotBattle`)이고, 본판에서는 코어 패키지(`com.bolzena.core`) 어댑터로 바꾼다.

- Unity 6000.6.4f1 · URP(Universal 렌더러, 후처리 Bloom 등) · Linear 색 공간
- spine-unity **4.1**(`Assets/Spine`, 4.1 브랜치) — Unity 6.6 에서 없어진 API 몇 줄을 막았다(아래)
- 장면 · 설정은 전부 코드로 만든다(에디터 화면을 열 필요 없음)

## 처음 받았을 때

원작 에셋은 저장소에 없다. 볼제나 저장소의 `assets/` 에서 필요한 것만 복사 · 변환한다.

```sh
python Tools/copy_assets.py   # 스파인(PMA → 곧은 알파) · 효과음/목소리(Ogg Opus → WAV) · 배경 · 이펙트 · 카드 그림
python Tools/make_ui.py       # 카드 틀 · 구슬 · 아이콘 같은 화면 그림(코드로 그림)
"/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -projectPath . \
  -executeMethod Bolzena.EditorTools.ProjectSetup.ImportTmp      # TMP 필수 리소스(한 번, -quit 없이)
```

필요: Python 3 + Pillow · numpy · soundfile.

## 빌드 · 자동 데모

```sh
./Tools/build.sh      # ProjectSetup.SetupAndBuild — 씬 · 재질 · 플레이어 설정을 다시 만들고 Build/Bolzena.exe
./Tools/demo.sh       # Build/Bolzena.exe -demo → Captures/*.png (장면별) · Captures/frames/<이름>/*.jpg
python Tools/make_gifs.py   # frames → Captures/gif_*.gif
```

`-demo` 는 가짜 손가락으로 한 판을 진행한다(카드 → 치명 → 격파 → 적의 수 → 신탁 → 보스 등장 → 고학년 → 보스 격파 → 승리).
한 프레임을 1/30초로 고정해(Time.captureDeltaTime) 캡처가 느려도 연출 시간은 그대로다.

## 폴더

```
Assets/Bolzena/
  Scripts/Battle/     IBattle(규칙 문) · BattleTypes(이벤트 · 상태) · PilotBattle/PilotData(시범용 임시 규칙 · 자료)
  Scripts/Core/       Clock(히트스톱 · 슬로 · 트윈) · Res(자원) · Sfx(효과음 · 목소리)
  Scripts/View/       UnitView(SD 스파인 · 번쩍임 · 잔상 · 타격 시각) · Fx(낱장 · 파티클 · 숫자) · FieldRig(흔들림 · 줌) · PostFx · EnemyHud
  Scripts/UI/         HandView/CardView/TargetArrow(손패) · PartyHud · UltCutin(고학년 컷인) · EpiphanyWindow(신탁) · Banners · ScreenFx
  Scripts/Demo/       DemoRunner(-demo) · Capture
  Scripts/BattleDirector*.cs   이벤트를 받아 연출을 차례대로 거는 감독
  Shaders/            Sprite(HDR · 채우기) · FocusLines(집중선) · Rays(빛줄기)
  Editor/             ProjectSetup(설정 · 씬 · 빌드) · BolzenaImport(그림 가져오기 규칙) · SpineDump
  Resources/          Fonts(Jua, OFL) · UI(코드로 그린 것) · [원작 에셋 — gitignore]
Tools/                copy_assets.py · make_ui.py · make_gifs.py · build.sh · demo.sh
```

## 규칙 ↔ 화면

`IBattle` 의 `Begin / PlayCard / UseUlt / EndTurn` 은 규칙을 끝까지 계산하고 **이벤트 목록**을 돌려준다
(`Act` → `Damage`(Hit 번째 · Crit · HpAfter) → `Toughness` → `Break` → `Death` …). `BattleDirector` 는 `Act` 하나와 뒤따르는 결과를
한 몸짓으로 묶고, 스파인 이벤트로 짐작한 때리는 순간(웹판 strikeOf 와 같은 규칙)에 맞춰 차례마다 펼친다.
코어 패키지로 바꿀 때는 그 결과를 이 이벤트로 옮기는 어댑터 하나면 된다.

## spine-unity 4.1 을 Unity 6.6 에 맞춘 곳

- `GetInstanceID()` / `InstanceIDToObject` (Unity 6.6 에서 오류) → `GetHashCode()` · 참조 비교로
- `DragAndDrop.AddDropHandler` · `hierarchyWindowItemOnGUI` 등록 줄을 막음(에디터 끌어 놓기 · 계층 아이콘만 빠진다)
- 원작 그림은 PMA 인데 Linear 색 공간에서는 색이 바래서, 복사할 때 곧은 알파로 풀고 재질을 Straight Alpha 로 쓴다
