#!/usr/bin/env bash
# Shared SDK resolution for sync/build/test/run/bot and formatting checks.
dawnholder_sdk() {
  local root=$1 required observed code=0
  required=$(python3 - "$root/global.json" <<'PY'
import json,sys
sdk=json.load(open(sys.argv[1],encoding='utf-8'))['sdk']
if sdk.get('version')!='10.0.301' or sdk.get('rollForward')!='disable':
 raise SystemExit('global.json must pin SDK10.0.301 with rollForward:disable')
print(sdk['version'])
PY
  ) || fail 'Cannot read the required pinned SDK from global.json.'
  if [[ ${DAWNHOLDER_DOTNET+x} ]]; then
    DOTNET=$DAWNHOLDER_DOTNET
    [[ -n "$DOTNET" && "$DOTNET" == /* ]] || fail "Required SDK $required; explicit DAWNHOLDER_DOTNET must name an absolute executable: '$DOTNET'."
  elif [[ -e /home/bass1/.local/share/dawnholder/dotnet-10.0.301/dotnet ]]; then
    DOTNET=/home/bass1/.local/share/dawnholder/dotnet-10.0.301/dotnet
  else
    DOTNET=$(command -v dotnet || true)
    [[ -n "$DOTNET" ]] || DOTNET="$HOME/.dotnet/dotnet"
  fi
  [[ -f "$DOTNET" && -x "$DOTNET" ]] || fail "Required SDK $required; selected $DOTNET; executable missing or not executable."
  DOTNET=$(realpath -- "$DOTNET")
  observed=$(cd -- "$root" && "$DOTNET" --version 2>&1) || code=$?
  [[ $code == 0 && "$observed" == "$required" ]] || fail "Required SDK $required; selected $DOTNET; observed '$observed'; version command exit $code. No version fallback."
  export DOTNET_HOST_PATH="$DOTNET" DOTNET_ROOT="$(dirname -- "$DOTNET")"
  printf 'Required SDK: %s\nSelected dotnet: %s\nObserved SDK: %s\n' "$required" "$DOTNET" "$observed"
}
