#!/usr/bin/env bash
# 연속 실행 FAIL 시나리오 fresh 서버 단독 재검 (시나리오마다 서버 재기동).
# 사용: wsl -d Ubuntu -- bash -c "tr -d '\r' < /mnt/c/Dev/ClaudeDev/99_Tools/run_bot_fresh_recheck.sh > /tmp/botfresh.sh && bash /tmp/botfresh.sh BossFightSmoke HpSyncSmoke"
set -u

DOTNET=/home/bass1/.dotnet/dotnet
SERVER=/home/bass1/dawnholder-poc/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll
BOT=/home/bass1/dawnholder-poc/99_Tools/headless-bot/bin/Debug/net10.0/Dawnholder.Tools.HeadlessBot.dll

for s in "$@"; do
  pkill -f 'GameServer\.[d]ll' 2>/dev/null
  sleep 1
  tail -f /dev/null | "$DOTNET" "$SERVER" > "/tmp/gs_fresh_$s.log" 2>&1 &
  for i in $(seq 1 12); do
    sleep 1
    ss -tln 2>/dev/null | grep -q 7777 && break
  done
  if ! ss -tln 2>/dev/null | grep -q 7777; then
    echo "=== $s === SERVER_FAIL"
    continue
  fi
  echo "=== $s (fresh) ==="
  "$DOTNET" "$BOT" --scenario "$s" --host 127.0.0.1 --port 7777 2>&1 | grep -E 'success=|desync|FAIL|Exception' | head -4
done

echo "ALL_DONE"
