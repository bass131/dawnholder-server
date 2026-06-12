#!/usr/bin/env bash
# 봇 전 시나리오 회귀 러너 (WSL2, ADR-029) — fresh 서버 기동 후 16 시나리오 연속 실행.
# 사용: wsl -d Ubuntu -- bash -c "tr -d '\r' < /mnt/c/Dev/ClaudeDev/99_Tools/run_bot_regression.sh > /tmp/botreg.sh && bash /tmp/botreg.sh"
# (Git Bash → wsl 중첩 따옴표 변수 전개 깨짐 함정 회피 = 파일 실행. tr은 CRLF 방어.)
# 연속 실행 한계: HpSync/BossFight는 보스 상태 누적으로 실패 가능 — fresh 서버 단독 재검 인정 (M4.10 전례).
set -u

DOTNET=/home/bass1/.dotnet/dotnet
SERVER=/home/bass1/dawnholder-poc/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll
BOT=/home/bass1/dawnholder-poc/99_Tools/headless-bot/bin/Debug/net10.0/Dawnholder.Tools.HeadlessBot.dll

# 자기매치 회피 bracket 패턴
pkill -f 'GameServer\.[d]ll' 2>/dev/null
sleep 1

tail -f /dev/null | "$DOTNET" "$SERVER" > /tmp/gs_bot_regression.log 2>&1 &
for i in $(seq 1 12); do
  sleep 1
  ss -tln 2>/dev/null | grep -q 7777 && break
done
if ss -tln 2>/dev/null | grep -q 7777; then
  echo "SERVER_READY"
else
  echo "SERVER_FAIL"
  exit 1
fi

for s in MultiRosterSmoke EmergencyCombatSmoke BossStageClearSmoke BossFightSmoke HpSyncSmoke RemoteAttackSmoke WhiffSwingSmoke RangedHitSmoke FreezeSmoke ThunderboltAoeSmoke RangedWhiffSmoke DashSmoke TeleportSmoke EnemyAiSmoke MapTransition M2BasicMovement; do
  echo "=== $s ==="
  "$DOTNET" "$BOT" --scenario "$s" --host 127.0.0.1 --port 7777 2>&1 | grep -E 'success=|desync|FAIL|Exception' | head -4
done

echo "ALL_DONE (서버는 살아있음 — 후속 fresh 재검/수동 테스트용. 종료: pkill -f 'GameServer\.[d]ll')"
