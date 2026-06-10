#!/usr/bin/env bash
set -e

DOTNET=/home/bass1/.dotnet/dotnet
SERVER_DLL=/mnt/c/Dev/ClaudeDev/02_Server/GameServer/bin/Release/net10.0/GameServer.dll
BOT_DLL=/mnt/c/Dev/ClaudeDev/99_Tools/headless-bot/bin/Release/net10.0/Dawnholder.Tools.HeadlessBot.dll

echo "=== DashSmoke regression run ==="

# stdin 유지 파이프로 서버 기동
tail -f /dev/null | "$DOTNET" "$SERVER_DLL" > /tmp/gs_dash_smoke.log 2>&1 &
SERVER_PID=$!
echo "server PID=$SERVER_PID"

# 포트 7777 open 대기 (최대 12초)
for i in $(seq 1 12); do
  sleep 1
  if ss -tlnp 2>/dev/null | grep -q 7777; then
    echo "port 7777 ready (${i}s)"
    break
  fi
  echo "  waiting ${i}s..."
done

if ! ss -tlnp 2>/dev/null | grep -q 7777; then
  echo "ERROR: server did not open port 7777"
  kill $SERVER_PID 2>/dev/null
  exit 1
fi

# 봇 실행
echo "--- bot output ---"
"$DOTNET" "$BOT_DLL" --scenario DashSmoke --host 127.0.0.1 --port 7777
BOT_EXIT=$?
echo "--- bot exit=$BOT_EXIT ---"

# 서버 정리
kill $SERVER_PID 2>/dev/null
wait $SERVER_PID 2>/dev/null || true
echo "server killed"

echo "=== server log (last 25 lines) ==="
tail -25 /tmp/gs_dash_smoke.log

exit $BOT_EXIT
