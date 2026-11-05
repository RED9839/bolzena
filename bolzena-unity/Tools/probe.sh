#!/bin/sh
# 사람 입력 재현 — 가상 마우스(Input System)로 카드를 끌어 낸다. 진짜 커서는 건드리지 않는다. 결과: [Probe] 로그 · Captures/probe/
#   ./Tools/probe.sh battle [추가 인자…]   전투만(-battle). 예: ./Tools/probe.sh battle -oldbox -party 란,에르핀_왕도,비비 -foes goldring_elite,marshmallowsupporter
#   ./Tools/probe.sh run                   판 화면은 데모가 넘기고(-demo -humanfight) 첫 싸움을 가상 마우스로
# 확인 장면 — 화면 하나를 정해진 차례로 띄워 캡처 · 로그를 남기고 스스로 끈다(InputProbe.Scenes.cs). 빌드 한 번 뒤 명령 하나로.
#   ./Tools/probe.sh scene rage            광폭화 — 시험 싸움에 판 싸움과 같은 광폭화 칸(-rage)을 채우고 적 HP ×30(-wavehp), 턴만 넘겨
#                                          5턴 「광폭화까지 1턴」 · 6턴 「광폭화 ×1.1」 HUD · 적 상세 창 캡처, 예고 피해 전 턴 → 이번 턴 · 광폭화 뺀 같은 수 값 로그
#   ./Tools/probe.sh scene settings        로비 → 설정 → 화면 탭을 맨 아래까지 → 「플레이 기록 보내기」 토글 아래 표시(PlayRecord.NotSentWhy)
#   ./Tools/probe.sh scene enemyinfo       전투에서 적마다 눌러 상세 창(다음 행동 · 상태 · 패시브 · 광폭화 줄) — 1턴 · 2턴
#   ./Tools/probe.sh scene equip           도감 → 아티팩트 → 치유의 펜던트 · 기원의 성배 · 용광검 상세(「(턴당 1회)」 · 「(전투당 N회)」). -equips a,b 로 바꾼다
#   ./Tools/probe.sh scene pack [1~5]      N마리 일반전(기본 5, -pack 편성 배율) — 배치 캡처(상자 선 있음 · 없음) · 적마다 HP · 예고 피해를 배율 없는 같은 싸움과 견줌
#   ./Tools/probe.sh scene endturn         턴 종료 확인 창(카드 남기고 턴 종료 · 취소 · 다시 보지 않기) · HUD 더미 단추 · 파티 칩 「…」 · 단축키 배지 켬/끔
#   ./Tools/probe.sh scene enemynav        적 정보 창 ◀ ▶ — 적 셋을 한 바퀴 · 뒤로 · 바깥 누르면 닫힘
#   ./Tools/probe.sh scene dmg             피해 숫자 꼬리표 — 자동 전투로 「약점」 · 「치명」 숫자 순간(-nature 냉정 — 란이 약점)
#   ./Tools/probe.sh scene deck            덱 보기 정렬 — 새 판(-save probe_deck, 사람의 판 저장과 따로)을 열어 사도별 · 비용 · 종류 · 이름
#   ./Tools/probe.sh scene all             위 아홉을 차례로(pack 은 5마리) — 끝에 장면마다 종료 코드
#   장면 뒤에 인자를 더 붙이면 그대로 넘기고 기본값을 이긴다(예: ./Tools/probe.sh scene enemyinfo -foes goldring_elite).
#   캡처 Captures/probe/<장면>/NN_설명.png(돌릴 때마다 비움) · 로그 [Probe:<장면>] · 전체 로그 bolzena-unity-tmp/probe_scene_<장면>.log
#   종료 코드 0 = 확인 다 맞음 · 3 = 틀린 확인 있음(「틀림(!)」 줄) · 2 = 모르는 장면 · 5 = 240초 · 124 = 300초 끊김. 소리는 0(-mute).
# 프로브는 240초 · 싸움 하나 끝에 스스로 꺼진다. 그래도 남으면 여기서 끊고 지운다.
cd /c/projects/bolzena-unity
mode=${1:-battle}; shift

# 확인 장면 하나 — 장면 이름 · 추가 인자
scene() {
  s=$1; shift
  PARTY="-party 란,아야,시온더다크불릿"
  case "$s" in
    rage)      ARGS="-battle $PARTY -foes fairymobcloserange,fairymoblongrange -rage -wavehp 30" ;;
    enemyinfo) ARGS="-battle $PARTY -foes goldring_elite,marshmallowsupporter -rage" ;;
    pack)
      n=5
      case "$1" in [1-5]) n=$1; shift ;; esac
      FOES=$(echo "fairymobcloserange,fairymoblongrange,magicfork,buseuleogi,ginseng" | cut -d, -f1-$n)
      ARGS="-battle $PARTY -foes $FOES -pack -rage" ;;
    settings|equip) ARGS="" ;;
    endturn)   ARGS="-battle $PARTY -foes fairymobcloserange,fairymoblongrange -wavehp 20" ;;
    enemynav)  ARGS="-battle $PARTY -foes fairymobcloserange,magicfork,fairymoblongrange" ;;
    dmg)       ARGS="-battle $PARTY -foes fairymobcloserange,fairymoblongrange -nature 냉정 -wavehp 20" ;;
    deck)      ARGS="-save probe_deck" ;;
    *) echo "모르는 장면: $s (rage · settings · enemyinfo · equip · pack · endturn · enemynav · dmg · deck · all)"; return 2 ;;
  esac
  # 뒤에 붙인 인자를 앞에 둔다 — 게임은 같은 인자의 첫 것을 읽는다(Array.IndexOf)
  timeout -k 10 300 ./Build/Bolzena.exe "$@" $ARGS -mute -inputprobe -probescene "$s" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "C:\projects\bolzena-unity-tmp\probe_scene_$s.log" >/dev/null 2>&1   # 실행 파일이 표준 출력에 쏟는 메모리 설정 줄은 버린다(로그 파일에 다 있다)
  code=$?
  # 남은 것만 끊는다 — 이 장면으로 띄운 것(-probescene)만. 같은 빌드로 도는 다른 데모 · 점검은 건드리지 않는다
  powershell -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='Bolzena.exe'\" | Where-Object { \$_.CommandLine -match '-probescene' } | ForEach-Object { Stop-Process -Id \$_.ProcessId -Force; 'left probe Bolzena.exe killed' }"
  grep -E "\[Probe:$s\]|Exception" "/c/projects/bolzena-unity-tmp/probe_scene_$s.log" | head -120
  echo "scene $s exit=$code"
  return $code
}

if [ "$mode" = "scene" ]; then
  which=${1:-all}; [ $# -gt 0 ] && shift
  if [ "$which" = "all" ]; then
    sum=""; bad=0
    for s in rage settings enemyinfo equip pack endturn enemynav dmg deck; do
      scene "$s" "$@"; c=$?
      sum="$sum $s=$c"; [ $c -ne 0 ] && bad=1
    done
    echo "장면별 종료 코드:$sum"
    exit $bad
  fi
  scene "$which" "$@"
  exit $?
fi

name=${PROBE_NAME:-$mode}
if [ "$mode" = "run" ]; then ARGS="-demo -humanfight -demo-timeout 400"; else ARGS="-battle"; fi
timeout -k 10 300 ./Build/Bolzena.exe $ARGS -inputprobe "$@" -screen-width 1600 -screen-height 900 -screen-fullscreen 0 -logFile "C:\projects\bolzena-unity-tmp\probe_$name.log"
echo "exit=$?"
taskkill //F //IM Bolzena.exe >/dev/null 2>&1 && echo "남은 Bolzena.exe 를 지움"
grep -E "\[Probe\]|\[Battle\]|\[Hand\]|\[Bridge\]|Exception" "/c/projects/bolzena-unity-tmp/probe_$name.log" | head -80
