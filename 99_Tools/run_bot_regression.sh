#!/usr/bin/env bash
# 봇 전 시나리오 회귀 러너 (WSL2, ADR-029) — 시나리오당 fresh 서버로 결정론적 격리 실행.
# 사용: wsl -d Ubuntu -- bash -c "tr -d '\r' < /mnt/c/Dev/ClaudeDev/99_Tools/run_bot_regression.sh > /tmp/botreg.sh && bash /tmp/botreg.sh"
# (Git Bash → wsl 중첩 따옴표 변수 전개 깨짐 함정 회피 = 파일 실행. tr은 CRLF 방어.)
#
# **시나리오당 fresh 서버** (M7.6 봇수정): 옛 단일-서버 연속실행은 보스 상태 누적으로
#   BossFight/HpSync/Freeze/Thunderbolt가 비결정적 실패 → 시나리오마다 서버 재기동으로 격리.
# **BossRoom 시나리오 게이트**: standalone은 C_CheatCommand(#if DEBUG)로 20킬 게이트 충족
#   (xUnit seedBossGate의 socket 등가물). DEBUG 빌드 전제.
set -u

DOTNET=/home/bass1/.dotnet/dotnet
SERVER=/home/bass1/dawnholder-poc/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll
BOT=/home/bass1/dawnholder-poc/99_Tools/headless-bot/bin/Debug/net10.0/Dawnholder.Tools.HeadlessBot.dll

SCENARIOS="MultiRosterSmoke EmergencyCombatSmoke BossStageClearSmoke BossFightSmoke HpSyncSmoke RemoteAttackSmoke WhiffSwingSmoke RangedHitSmoke FreezeSmoke ThunderboltAoeSmoke RangedWhiffSmoke DashSmoke TeleportSmoke EnemyAiSmoke MapTransition M2BasicMovement"

PASS=0
FAIL=0
FAILED_LIST=""

for s in $SCENARIOS; do
  # 자기매치 회피 bracket 패턴
  pkill -f 'GameServer\.[d]ll' 2>/dev/null
  sleep 1
  tail -f /dev/null | "$DOTNET" "$SERVER" > /tmp/gs_bot_regression.log 2>&1 &
  for i in $(seq 1 12); do
    sleep 1
    ss -tln 2>/dev/null | grep -q 7777 && break
  done
  if ! ss -tln 2>/dev/null | grep -q 7777; then
    echo "=== $s : SERVER_FAIL ==="
    FAIL=$((FAIL+1)); FAILED_LIST="$FAILED_LIST $s(server)"
    continue
  fi

  echo "=== $s ==="
  OUT=$("$DOTNET" "$BOT" --scenario "$s" --host 127.0.0.1 --port 7777 2>&1)
  echo "$OUT" | grep -E 'success=|reason:|desync' | head -4
  if echo "$OUT" | grep -q 'success=True'; then
    PASS=$((PASS+1))
  else
    FAIL=$((FAIL+1)); FAILED_LIST="$FAILED_LIST $s"
  fi
done

pkill -f 'GameServer\.[d]ll' 2>/dev/null
echo ""
echo "########## REGRESSION SUMMARY: PASS=$PASS FAIL=$FAIL ##########"
[ -n "$FAILED_LIST" ] && echo "FAILED:$FAILED_LIST"
echo "ALL_DONE"
