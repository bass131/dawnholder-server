#!/usr/bin/env bash
# WSL runtime entry point. Unity plug-in DLLs are still built on Windows.
set -euo pipefail

fail() { echo "ERROR: $*" >&2; exit 1; }
SOURCE_ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)
ACTION=${1:-help}
case "$ACTION" in
  help|--help|-h)
    echo 'Usage: bash 99_Tools/sync-wsl.sh {path|sync|build|test|run|bot [Scenario ...]}'
    echo 'Overrides: DAWNHOLDER_WSL_ROOT (dedicated Linux directory), DAWNHOLDER_DOTNET (executable).'
    exit 0 ;;
  path|sync|build|test|run|bot) shift ;;
  *) fail "Unknown action: $ACTION" ;;
esac
[[ $(uname -s) == Linux ]] || fail 'Run this helper inside WSL/Linux.'
for tool in realpath sha256sum rsync flock ss timeout python3; do
  command -v "$tool" >/dev/null || fail "Required tool missing: $tool"
done
WORKSPACE_KEY=$(printf '%s' "$SOURCE_ROOT" | sha256sum | cut -c1-20)
CACHE_ROOT="$HOME/.cache/dawnholder"
RUNTIME_ROOT=$(realpath -m -- "${DAWNHOLDER_WSL_ROOT:-$CACHE_ROOT/workspaces/$WORKSPACE_KEY}")
USER_ROOT=$(realpath -- "$HOME")
case "$RUNTIME_ROOT" in
  /|/home|/root|/tmp|/var|/usr|/mnt|/mnt/*|"$USER_ROOT"|"$SOURCE_ROOT"|"$SOURCE_ROOT"/*)
    fail "Unsafe runtime destination: $RUNTIME_ROOT" ;;
esac
[[ "$SOURCE_ROOT/" != "$RUNTIME_ROOT/"* && "$USER_ROOT/" != "$RUNTIME_ROOT/"* ]] || fail 'Runtime destination may not contain the source or home directory.'
[[ -f "$SOURCE_ROOT/Dawnholder.slnx" ]] || fail 'Source repository not found.'
if [[ "$ACTION" == path ]]; then printf '%s\n' "$RUNTIME_ROOT"; exit 0; fi
mkdir -p -- "$CACHE_ROOT/locks"
DEST_KEY=$(printf '%s' "$RUNTIME_ROOT" | sha256sum | cut -c1-20)
exec 8>"$CACHE_ROOT/locks/workspace-$DEST_KEY.lock"
flock -n 8 || fail "Another task owns runtime workspace: $RUNTIME_ROOT"
OWNER_FILE="$RUNTIME_ROOT/.dawnholder-source"
if [[ -e "$RUNTIME_ROOT" ]]; then
  [[ -d "$RUNTIME_ROOT" && ! -L "$RUNTIME_ROOT" ]] || fail 'Runtime must be a real directory.'
  if [[ -f "$OWNER_FILE" ]]; then
    [[ $(cat -- "$OWNER_FILE") == "$SOURCE_ROOT" ]] || fail 'Runtime belongs to another source workspace.'
  else
    [[ -z $(find "$RUNTIME_ROOT" -mindepth 1 -maxdepth 1 -print -quit) ]] || fail 'Refusing to claim a nonempty runtime directory without an owner marker.'
  fi
fi
mkdir -p -- "$RUNTIME_ROOT"
printf '%s\n' "$SOURCE_ROOT" > "$OWNER_FILE"

# --delete is confined to these four source subdirectories, never the workspace root.
for tree in 02_Server 04_ClientNet 98_Shared 99_Tools; do
  [[ -d "$SOURCE_ROOT/$tree" && ! -L "$RUNTIME_ROOT/$tree" ]] || fail "Invalid sync directory: $tree"
  mkdir -p -- "$RUNTIME_ROOT/$tree"
  rsync -a --delete --exclude='bin/' --exclude='obj/' --exclude='logs/' \
    --exclude='*.log' --exclude='.env*' --exclude='secrets/' \
    --exclude='appsettings.Local.json' --exclude='appsettings.Development.json' \
    --exclude='out/' --exclude='soundfont/' \
    "$SOURCE_ROOT/$tree/" "$RUNTIME_ROOT/$tree/"
done
for config in Dawnholder.slnx global.json Directory.Build.props .editorconfig .gitattributes .github/workflows/dotnet-tests.yml; do
  [[ -f "$SOURCE_ROOT/$config" ]] || fail "Missing root build input: $config"
  [[ ! -L "$RUNTIME_ROOT/$config" ]] || fail "Linked root build input: $config"
  if [[ "$config" == .github/* ]]; then
    [[ ! -L "$RUNTIME_ROOT/.github" && ! -L "$RUNTIME_ROOT/.github/workflows" ]] || fail 'Linked CI configuration directory.'
    mkdir -p -- "$RUNTIME_ROOT/.github/workflows"
  fi
  cp -- "$SOURCE_ROOT/$config" "$RUNTIME_ROOT/$config"
  [[ $(sha256sum "$SOURCE_ROOT/$config" | cut -d' ' -f1) == $(sha256sum "$RUNTIME_ROOT/$config" | cut -d' ' -f1) ]] || fail "Root input hash mismatch: $config"
done
for config in Directory.Build.targets NuGet.config nuget.config packages.lock.json; do
  if [[ -f "$SOURCE_ROOT/$config" ]]; then
    [[ ! -L "$RUNTIME_ROOT/$config" ]] || fail "Linked root build input: $config"
    cp -- "$SOURCE_ROOT/$config" "$RUNTIME_ROOT/$config"
    [[ $(sha256sum "$SOURCE_ROOT/$config" | cut -d' ' -f1) == $(sha256sum "$RUNTIME_ROOT/$config" | cut -d' ' -f1) ]] || fail "Root input hash mismatch: $config"
  fi
done
MANIFEST=''
if [[ -n ${DAWNHOLDER_FORMAT_MANIFEST:-} ]]; then
  [[ -f "$DAWNHOLDER_FORMAT_MANIFEST" && ! -L "$DAWNHOLDER_FORMAT_MANIFEST" ]] || fail 'Source format manifest missing or linked.'
  MANIFEST="$RUNTIME_ROOT/.dawnholder-format-manifest.json"
  [[ ! -L "$MANIFEST" ]] || fail 'Linked destination manifest.'
  cp -- "$DAWNHOLDER_FORMAT_MANIFEST" "$MANIFEST"
  [[ $(sha256sum "$DAWNHOLDER_FORMAT_MANIFEST" | cut -d' ' -f1) == $(sha256sum "$MANIFEST" | cut -d' ' -f1) ]] || fail 'Format manifest copy hash mismatch.'
fi
echo "Runtime workspace: $RUNTIME_ROOT"
[[ "$ACTION" != sync || -n "$MANIFEST" ]] || exit 0
STATE=$(mktemp -d "$RUNTIME_ROOT/.dotnet-state-XXXXXXXX")
printf '%s\n' "$SOURCE_ROOT" > "$STATE/.dawnholder-source"
export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$STATE/cli-home}" NUGET_PACKAGES="${NUGET_PACKAGES:-$STATE/nuget/packages}" NUGET_HTTP_CACHE_PATH="${NUGET_HTTP_CACHE_PATH:-$STATE/nuget/http}" NUGET_PLUGINS_CACHE_PATH="${NUGET_PLUGINS_CACHE_PATH:-$STATE/nuget/plugins}" NUGET_SCRATCH="${NUGET_SCRATCH:-$STATE/nuget/scratch}"
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1
source "$RUNTIME_ROOT/99_Tools/Formatting/sdk.sh"
dawnholder_sdk "$RUNTIME_ROOT"
cd -- "$RUNTIME_ROOT"
if [[ -n "$MANIFEST" ]]; then
  "$DOTNET" restore 99_Tools/Formatting/Formatting.csproj --nologo 8>&-
  "$DOTNET" build 99_Tools/Formatting/Formatting.csproj --no-restore --nologo 8>&-
  "$DOTNET" 99_Tools/Formatting/bin/Debug/net10.0/Formatting.dll sync-inputs --root "$SOURCE_ROOT" --after "$RUNTIME_ROOT" --dotnet "$DOTNET" --manifest "$MANIFEST" 8>&-
fi
[[ "$ACTION" != sync ]] || exit 0
# The parent owns the workspace lock; compiler servers must not inherit it.
"$DOTNET" restore Dawnholder.slnx --nologo 8>&-
"$DOTNET" build Dawnholder.slnx --configuration Debug --no-restore --nologo 8>&-
case "$ACTION" in
  build) exit 0 ;;
  test) "$DOTNET" test Dawnholder.slnx --configuration Debug --no-build --nologo "$@" 8>&-; exit 0 ;;
esac

# GameServer currently has a fixed 7777 endpoint. Different workspaces cannot run it concurrently.
exec 9>"$CACHE_ROOT/locks/server-7777.lock"
flock -n 9 || fail 'Port 7777 is owned by another Dawnholder runtime task. Stop it before starting this task.'
port_busy() { [[ -n $(ss -H -ltn 'sport = :7777') ]]; }
port_busy && fail 'Port 7777 already has a listener. This helper will not stop another process.'
SERVER="$RUNTIME_ROOT/02_Server/GameServer/bin/Debug/net10.0/GameServer.dll"
BOT="$RUNTIME_ROOT/99_Tools/headless-bot/bin/Debug/net10.0/Dawnholder.Tools.HeadlessBot.dll"
LOG_ROOT=$(mktemp -d "$RUNTIME_ROOT/runtime-logs-XXXXXXXX")
SERVER_PID=''
SERVER_INPUT="$LOG_ROOT/server.stdin"
mkfifo "$SERVER_INPUT"
exec 7<>"$SERVER_INPUT"
stop_server() {
  if [[ -n "$SERVER_PID" ]]; then
    kill "$SERVER_PID" 2>/dev/null || true
    wait "$SERVER_PID" 2>/dev/null || true
    SERVER_PID=''
  fi
}
trap stop_server EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
start_server() {
  local log_file=$1
  port_busy && fail 'Port 7777 became occupied; refusing to start or kill any other server.'
  "$DOTNET" "$SERVER" <&7 >"$log_file" 2>&1 &
  SERVER_PID=$!
  for ((attempt=0; attempt<120; attempt++)); do
    kill -0 "$SERVER_PID" 2>/dev/null || { cat "$log_file"; return 1; }
    if ss -H -ltnp 'sport = :7777' | grep -Fq "pid=$SERVER_PID,"; then return 0; fi
    sleep 0.1
  done
  echo 'Server did not listen on 7777 within 12 seconds.' >&2
  cat "$log_file"
  return 1
}
echo "Runtime logs: $LOG_ROOT"
if [[ "$ACTION" == run ]]; then
  start_server "$LOG_ROOT/server.log"
  echo "GameServer running on :7777 (PID $SERVER_PID). Stop with Ctrl+C."
  wait "$SERVER_PID"
else
  [[ $# -gt 0 ]] || set -- DashSmoke
  PASS=0
  FAIL=0
  for scenario in "$@"; do
    [[ "$scenario" =~ ^[A-Za-z][A-Za-z0-9_]*$ ]] || fail "Invalid scenario name: $scenario"
    echo "=== $scenario (fresh server) ==="
    if start_server "$LOG_ROOT/$scenario-server.log"; then
      if timeout 120 "$DOTNET" "$BOT" --scenario "$scenario" --host 127.0.0.1 --port 7777 >"$LOG_ROOT/$scenario-bot.log" 2>&1 \
          && grep -q 'success=True' "$LOG_ROOT/$scenario-bot.log"; then
        PASS=$((PASS + 1))
      else
        FAIL=$((FAIL + 1))
      fi
      cat "$LOG_ROOT/$scenario-bot.log"
    else
      FAIL=$((FAIL + 1))
    fi
    stop_server
  done
  echo "REGRESSION SUMMARY: PASS=$PASS FAIL=$FAIL"
  [[ "$FAIL" -eq 0 ]]
fi
