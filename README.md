# 볼제나 (볼 제로 나이트메어)

트릭컬 리바이브 사도들로 하는 카제나식 덱빌딩 로그라이크 — **비공식 팬 게임 · 비영리**.

- 플레이: https://bolzena.pages.dev (유니티 WebGL)
- 예전 웹(JS)판: [`web-legacy`](../../tree/web-legacy) 브랜치 · `web-final` 태그

> 트릭컬 리바이브의 그림 · 음성 · 설정의 저작권은 EPID Games 에 있습니다. 이 저장소와 게임은 공식과 무관하며, 권리자의 요청이 있으면 즉시 내립니다.
> 원작 에셋(스파인 · 그림 · 소리)은 이 저장소에 들어 있지 않습니다.

## 구성

| 폴더 | 내용 |
|---|---|
| `bolzena-unity/` | 유니티 6 본 프로젝트(URP · spine-unity 4.1) — 전투 화면 · 통합 · 빌드 |
| `bolzena-core/` | 규칙 엔진 UPM 패키지(UnityEngine 없는 순수 C#) — 키워드 · 전투 · 판 · 봇 · 시뮬 · 검사기, `Tools~/Dev` 콘솔 |
| `bolzena-runui/` | 판 화면 UPM 패키지 — 로비 · 마을 · 편성 · 지도 · 상점 · 이벤트 · 보상 · 도감 · 설정 |
| `bolzena-fx/` | 연출 자료 UPM 패키지 — 이펙트 변환 · 모션 표 · 소리 규칙 |
| `bolzena-content-v2/` | 게임 데이터(JSON) — 사도 135 · 적 · 마을 6 · 이벤트 · 장비(아티팩트) · 교주 카드(스펠 카드) |

패키지는 `bolzena-unity/Packages/manifest.json` 에서 `file:../../bolzena-*` 로 붙어 있으니 이 폴더 구조 그대로 받아야 열립니다.

## 빌드

1. 유니티 6000.6.4f1 (Windows · WebGL 모듈).
2. 원작 에셋은 각자 준비해야 합니다 — `bolzena-unity/Tools/copy_assets.py` · `copy_run_assets.py` 가 로컬 에셋 폴더에서 필요한 것만 프로젝트로 복사합니다(경로는 스크립트 안).
3. 데이터 검사: `bolzena-core/Tools~/Dev/bz.cmd check --data <bolzena-content-v2 경로>`
4. PC: `bolzena-unity/Tools/build.sh` · 웹: `bolzena-unity/Tools/build_web.sh`(25MB 넘는 파일은 조각으로 나눔).
