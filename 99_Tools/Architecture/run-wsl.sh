#!/usr/bin/env bash
set -euo pipefail
fail() { echo "ERROR: $*" >&2; exit 1; }
SOURCE_ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)
ACTION=${1:-help}
case "$ACTION" in
  help|--help|-h) echo 'Usage: bash 99_Tools/Architecture/run-wsl.sh {path|prepare|measure|check}'; exit 0 ;;
  path|prepare|measure|check) ;;
  *) fail "Unknown action: $ACTION" ;;
esac
[[ $(uname -s) == Linux ]] || fail 'Run in WSL.'
WORKSPACE_KEY=$(printf '%s' "$SOURCE_ROOT" | sha256sum | cut -c1-20)
RUNTIME_ROOT="$HOME/.cache/dawnholder/architecture/$WORKSPACE_KEY"
SETTINGS="$SOURCE_ROOT/99_Tools/Architecture/comparison-settings.json"
EVIDENCE_RELATIVE=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["evidencePath"])' "$SETTINGS")
GOAL_RELATIVE=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["goalPath"])' "$SETTINGS")
EVIDENCE="$SOURCE_ROOT/$EVIDENCE_RELATIVE"
MANIFEST="$SOURCE_ROOT/$GOAL_RELATIVE/input-manifest.json"
if [[ "$ACTION" == path ]]; then printf '%s\n' "$RUNTIME_ROOT"; exit 0; fi
for tool in python3 rsync flock git sha256sum; do command -v "$tool" >/dev/null || fail "Missing: $tool"; done
mkdir -p -- "$HOME/.cache/dawnholder/locks"
exec 8>"$HOME/.cache/dawnholder/locks/architecture-$WORKSPACE_KEY.lock"
flock -n 8 || fail 'Architecture workspace is in use.'
[[ ! -L "$RUNTIME_ROOT" ]] || fail 'Linked runtime root.'
[[ $(realpath -m "$RUNTIME_ROOT") == "$RUNTIME_ROOT" ]] || fail 'Linked runtime ancestor.'
if [[ -e "$RUNTIME_ROOT" ]]; then
  [[ -f "$RUNTIME_ROOT/.dawnholder-source" && $(cat "$RUNTIME_ROOT/.dawnholder-source") == "$SOURCE_ROOT" ]] || fail 'Foreign runtime workspace.'
  [[ ! -L "$RUNTIME_ROOT/.dawnholder-source" ]] || fail 'Linked owner marker.'
fi
mkdir -p -- "$RUNTIME_ROOT" "$EVIDENCE"
for directory in tool bundle roslyn-input codegraph-input; do
  [[ ! -L "$RUNTIME_ROOT/$directory" ]] || fail "Linked workspace directory: $directory"
done
printf '%s\n' "$SOURCE_ROOT" > "$RUNTIME_ROOT/.dawnholder-source"
mkdir -p -- "$RUNTIME_ROOT/tool" "$RUNTIME_ROOT/bundle" "$RUNTIME_ROOT/roslyn-input" "$RUNTIME_ROOT/codegraph-input"
# Delete is limited to the marker-checked, owned tool directory; excluded build
# and dependency folders are retained. Product input roots are never deleted.
rsync -a --delete --exclude=CodeGraph/ --exclude=bin/ --exclude=obj/ --exclude=__pycache__/ "$SOURCE_ROOT/99_Tools/Architecture/" "$RUNTIME_ROOT/tool/"
mkdir -p -- "$RUNTIME_ROOT/tool/CodeGraph"
cp -- "$SOURCE_ROOT/99_Tools/Architecture/CodeGraph/syntax-context.cjs" "$RUNTIME_ROOT/tool/CodeGraph/syntax-context.cjs"
rsync -a "$SOURCE_ROOT/99_Tools/Architecture/CodeGraph/node_modules/@colbymchenry/codegraph-linux-x64/" "$RUNTIME_ROOT/bundle/"
chmod u+x -- "$RUNTIME_ROOT/bundle/bin/codegraph" "$RUNTIME_ROOT/bundle/node"
python3 "$RUNTIME_ROOT/tool/Pipeline/inputs.py" "$SOURCE_ROOT" "$RUNTIME_ROOT" "$MANIFEST" "$EVIDENCE"
cp -- "$MANIFEST" "$RUNTIME_ROOT/manifest.json"
cp -- "$SOURCE_ROOT/global.json" "$RUNTIME_ROOT/global.json"
cp -- "$SOURCE_ROOT/.editorconfig" "$RUNTIME_ROOT/.editorconfig"
cp -- "$SOURCE_ROOT/99_Tools/.editorconfig" "$RUNTIME_ROOT/tool/.editorconfig"
[[ "$ACTION" != prepare ]] || { printf 'Prepared: %s\n' "$RUNTIME_ROOT"; exit 0; }
# A fresh invocation owns all .NET state, including the first SDK probe.
STATE=$(mktemp -d "$RUNTIME_ROOT/.dotnet-state-XXXXXXXX")
export DOTNET_CLI_HOME="$STATE/cli-home" NUGET_PACKAGES="$STATE/nuget/packages" NUGET_HTTP_CACHE_PATH="$STATE/nuget/http" NUGET_PLUGINS_CACHE_PATH="$STATE/nuget/plugins" NUGET_SCRATCH="$STATE/nuget/scratch"
export DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0 DOTNET_GENERATE_ASPNET_CERTIFICATE=false DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 DOTNET_NOLOGO=1
export DO_NOT_TRACK=1 CODEGRAPH_TELEMETRY=0 CODEGRAPH_NO_UPDATE_CHECK=1 CODEGRAPH_NO_DAEMON=1
DOTNET_RELATIVE=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["dotnetRelativePath"])' "$SETTINGS")
DOTNET="$HOME/$DOTNET_RELATIVE"
[[ -x "$DOTNET" ]] || fail 'Pinned SDK host missing.'
export DOTNET_HOST_PATH="$DOTNET" DOTNET_ROOT="$(dirname -- "$DOTNET")"
cd -- "$RUNTIME_ROOT"
python3 "$RUNTIME_ROOT/tool/Pipeline/runner.py" "$ACTION" --source "$SOURCE_ROOT" --runtime "$RUNTIME_ROOT" --evidence "$EVIDENCE" --dotnet "$DOTNET" 8>&-
