장비 · 교주 카드 리워크 측정 도구 (2026-10-07 · 둘째 손질 10-08) — _measure/장비_중립카드_리워크_시범.md §5 · §7

엔진 소스를 그대로 묶어 빌드한다(bz 바이너리와 달리 지금 엔진 소스가 들어간다). 엔진 경로는 RT 속성으로 준다:
  "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Data\DotNetSdk\dotnet.exe" build -c Release -o out -p:RT=C:/projects/bolzena-core/Runtime
  (다른 담당이 엔진을 고치는 중이면 Runtime 을 임시 폴더에 복사해 그 사본으로 빌드 — 전 · 후를 같은 바이너리로 잰다)

tracker: out\Tracker.exe <데이터 폴더> <바퀴> <씨앗 0,1,2,3> [full] [적HP배율] [적피해배율]
  숙련 봇 · 역할 편성(bz sim --bot skilled --party role 과 같은 판)으로 씨앗마다 완주율, 합, 끝 덱 교주 카드 평균 장수.
  full 이면 장비마다 「낀 판 · 낀 판 완주%」, 교주 카드마다 「끝 덱에 든 판 · 든 판 완주%」 (오래 산 판일수록 많이 끼므로 완주%는 비교용).
kit: out\Kit.exe check <데이터 폴더>          — 지금 엔진 소스의 검사기(오류 · 주의 전부)
     out\Kit.exe text <데이터 폴더> [id,…]    — 장비 글 + 봇 값(CardValue.GearWorth) · 교주 카드 기본/신탁/축복 글 + 코스트당 값
axis.js: node axis.js [heroes 폴더] — 사도마다 내는 상태 · 일(실드 · 드로우 · 0코 · 생성 …)을 센다(장비 축 비율 정할 때).

전 · 후는 콘텐츠를 임시 폴더에 복사해 같은 바탕에서 잰다(_ 로 시작하는 이 폴더는 bz 가 안 읽는다).
