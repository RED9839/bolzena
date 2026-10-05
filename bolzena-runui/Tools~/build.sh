#!/bin/sh
# 시험 프로젝트를 꾸리고 Windows 빌드 — RunUiSetup.SetupAndBuild(코어 데이터 복사 · 장면 · 플레이어 설정 · 빌드)
P=${1:-C:/projects/bolzena-runui-test}
"/c/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -quit -projectPath "$P" \
  -executeMethod Bolzena.RunUI.EditorTools.RunUiSetup.SetupAndBuild -logFile "$P/build.log"
code=$?
grep -E "error CS|Shader error|\[Build\]|\[RunUiSetup\]" "$P/build.log" | sort -u | head -40
exit $code
