#!/bin/sh
# 웹(WebGL) 빌드 — bolzena.pages.dev 용. 원작 에셋을 (없는 것만) 복사하고, 프로젝트 설정 · 콘텐츠 묶음까지 만든 뒤 WebBuild/ 에 빌드,
# 그다음 web_post.py 가 25MB 넘는 파일을 조각내고 index.html(로딩 화면 · 비공식 표기)을 쓴다 → WebBuild/ 를 그대로 올린다:
# 빌드는 두 벌 — 데스크톱(DXT) 판 WebBuild/ 와 폰(ASTC) 판 WebBuildAstc/. 폰 판은 .data 만 WebBuild/Build 에 옮겨지고,
# index.html 이 브라우저가 ASTC 를 읽으면 그것을 받는다(BOLZENA_WEB_ASTC=0 이면 데스크톱 판만 — 그림 다시 가져오기를 건너뛴다).
#   npx wrangler pages deploy WebBuild --project-name bolzena
cd /c/projects/bolzena-unity
mkdir -p /c/projects/bolzena-unity-tmp
PYTHONIOENCODING=utf-8 python Tools/copy_assets.py >/dev/null || exit 1
PYTHONIOENCODING=utf-8 python Tools/copy_run_assets.py | tail -3 || exit 1
PYTHONIOENCODING=utf-8 python Tools/pad4.py | tail -1 || exit 1   # 4의 배수가 아닌 그림 채우기(웹 DXT — WebImport)
timeout -k 10 10800 "/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -quit -buildTarget WebGL -projectPath "C:\projects\bolzena-unity" \
  -executeMethod Bolzena.EditorTools.ProjectSetup.SetupAndBuildWeb -logFile "C:\projects\bolzena-unity-tmp\build_web.log"
code=$?
grep -E "error CS|Shader error|\[Build\]|\[Setup\]|\[WebTex\] [^안]|\[Bundles\]" /c/projects/bolzena-unity-tmp/build_web.log | sort -u | head -60
[ $code -eq 0 ] || exit $code
PYTHONIOENCODING=utf-8 python Tools/web_post.py
