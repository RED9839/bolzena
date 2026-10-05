# 볼제나 유니티 이식 — 트랙과 주인 (2026-10-04)

사용자: 「여러 에이전트로 나눠서 이식해봐」. 웹판 C:\projects\볼제나 는 **읽기 전용 참고**(수정 · 커밋 금지). 카드 · 이벤트 · 적 **내용은 새로 만든다**(웹 데이터 이식 안 함) — 설계 문서(docs/18 규칙 · docs/19 운영 방식 · docs/20 마을)는 기준.

## 규칙 (모든 트랙)
- 유니티 6000.6.4f1, 명령줄(batchmode)만. **한 유니티 프로젝트는 한 번에 한 프로세스** — 각 트랙은 **자기 시험 프로젝트**에서 컴파일 · 테스트 · 캡처하고, 남의 프로젝트 · 패키지 폴더는 건드리지 않는다.
- 공유는 **UPM 로컬 패키지**(`"com.bolzena.xxx": "file:../bolzena-xxx"`)로. 패키지마다 asmdef 하나 이상, 의존 방향: core ← (fx, battle, runui) ← bolzena-unity.
- 원작 에셋은 git 에 넣지 않는다(.gitignore). 에셋 복사는 각 시험 프로젝트에서 `bolzena-unity/Tools/copy_assets.py` 를 참고해 필요한 것만.
- 커밋은 각 폴더 로컬 git 만, 원격 · 푸시 금지.
- 보고는 한국어. 캡처로 직접 확인(자동 데모 + PNG).

## 트랙
| 트랙 | 주인 폴더(쓰기) | 시험 프로젝트 | 하는 일 |
|---|---|---|---|
| A 코어 | `C:\projects\bolzena-core` (com.bolzena.core) | `bolzena-core-test` | 규칙 엔진 · 데이터 틀 · 카드 글 생성기 · 봇 · 시뮬. **API 문서 `Docs/API.md` 를 먼저 내놓는다** |
| B 연출 자료 | `C:\projects\bolzena-fx` (com.bolzena.fx) | `bolzena-fx-test` | 원작 이펙트 `assets/fx/*/fx.json` → 유니티 파티클/낱장 변환기, 사도별 고학년 이펙트, 카드 모션 · DASH · 총구 · 목소리 표(웹 js/card-motion · motion-voice · fight-screen DASH), 오디오 규칙(겹침 · 꼬리 · 믹싱) |
| C 전투 화면 | `C:\projects\bolzena-unity` (본 프로젝트) | (자기 자신) | 전투 화면 완성: 툴팁 · 키워드 · 상태 칩 · 더미 보기 · 정보 창 · 입력 · 하얗게 날아가는 연출 세기 다듬기 · IBattle → core 어댑터(코어 API 나오면) · B 의 fx 패키지 붙이기 · 에셋 파이프라인(압축 · 아틀라스) |
| D 판 화면 | `C:\projects\bolzena-runui` (com.bolzena.runui) | `bolzena-runui-test` | 로비 · 마을 공개 · 파티 편성 · 지도 · 상점 · 이벤트 · 캠프 · 보상 · 장비 · 도감 · 설정 · 저장/이어하기 화면. 판 로직은 core 의 판 API(나오기 전엔 얇은 인터페이스 + 가짜) |

통합: 마지막에 C 가 bolzena-unity 의 manifest 에 core · fx · runui 를 붙여 한 게임으로 묶는다.
