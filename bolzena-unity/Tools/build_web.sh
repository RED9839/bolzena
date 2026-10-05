#!/bin/sh
# 웹(WebGL) 빌드 — bolzena.pages.dev 용. 원작 에셋을 (없는 것만) 복사하고, 프로젝트 설정 · 콘텐츠 묶음까지 만든 뒤 WebBuild/ 에 빌드,
# 그다음 web_post.py 가 25MB 넘는 파일을 조각내고 index.html(로딩 화면 · 비공식 표기)을 쓴다 → WebBuild/ 를 그대로 올린다:
#   npx wrangler pages deploy WebBuild --project-name bolzena
cd /c/projects/bolzena-unity
mkdir -p /c/projects/bolzena-unity-tmp
PYTHONIOENCODING=utf-8 python Tools/copy_assets.py >/dev/null || exit 1
PYTHONIOENCODING=utf-8 python Tools/copy_run_assets.py | tail -3 || exit 1
timeout -k 10 7200 "/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -quit -buildTarget WebGL -projectPath "C:\projects\bolzena-unity" \
  -executeMethod Bolzena.EditorTools.ProjectSetup.SetupAndBuildWeb -logFile "C:\projects\bolzena-unity-tmp\build_web.log"
code=$?
grep -E "error CS|Shader error|\[Build\]|\[Setup\]" /c/projects/bolzena-unity-tmp/build_web.log | sort -u | head -30
[ $code -eq 0 ] || exit $code
PYTHONIOENCODING=utf-8 python Tools/web_post.py
