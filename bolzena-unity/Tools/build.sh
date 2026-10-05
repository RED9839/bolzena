#!/bin/sh
# 빌드 — 원작 에셋을 (없는 것만) 복사하고, 프로젝트 설정(콘텐츠 데이터 · 씬 · 재질 · 플레이어)까지 코드로 다시 만든 뒤 Windows 빌드
cd /c/projects/bolzena-unity
mkdir -p /c/projects/bolzena-unity-tmp
PYTHONIOENCODING=utf-8 python Tools/copy_assets.py >/dev/null || exit 1
PYTHONIOENCODING=utf-8 python Tools/copy_run_assets.py | tail -3 || exit 1
timeout -k 10 3000 "/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -quit -projectPath "C:\projects\bolzena-unity" \
  -executeMethod Bolzena.EditorTools.ProjectSetup.SetupAndBuild -logFile "C:\projects\bolzena-unity-tmp\build.log"
code=$?
grep -E "error CS|Shader error|\[Build\]|\[Setup\]" /c/projects/bolzena-unity-tmp/build.log | sort -u | head -30
exit $code
