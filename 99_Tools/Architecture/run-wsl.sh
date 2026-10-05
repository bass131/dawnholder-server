#!/usr/bin/env bash
set -euo pipefail
SOURCE_ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)
ACTION=${1:-help}
[[ $# == 0 ]] || shift
SELECTION=roslyn
EVIDENCE_RELATIVE=
EVIDENCE_SET=0
METADATA_STDIN=0
STATUS_WRITABLE=0
fail() {
    local status=$1 reason=$2 message=$3 repair=$4
    echo "ERROR: $message; status: $status; evidence: ${EVIDENCE_RELATIVE:-unset}" >&2
    echo "Repair: $repair" >&2
    if [[ "$STATUS_WRITABLE" == 1 ]]; then
        python3 -B "$STATUS_TOOL" record "${STATUS_ARGS[@]}" --action "$ACTION" \
            --status "$status" --reason "$reason" --message "$message" --repair "$repair" >&2 || true
    fi
    exit 1
}
trap 'fail failed preparation_failed "Preparation command failed at line $LINENO (exit $?)" "Inspect the emitted command error; preserve existing installation/cache and rerun the same action/selection/root."' ERR
case "$ACTION" in
    help|--help|-h)
        echo 'Usage: bash 99_Tools/Architecture/run-wsl.sh {path|prepare|measure|check} [--extractor roslyn|codegraph|compare] [--evidence .backups/path]'
        exit 0 ;;
    path|prepare|measure|check) ;;
    *) fail failed invalid_action "Unknown action: $ACTION" 'Use path|prepare|measure|check.' ;;
esac
while [[ $# -gt 0 ]]; do
    case "$1" in
        --extractor|--evidence)
            [[ $# -ge 2 ]] || fail failed invalid_arguments "Missing value: $1" 'Supply the extractor or repository-relative .backups/ evidence path.'
            if [[ "$1" == --extractor ]]; then SELECTION=$2; else EVIDENCE_RELATIVE=$2; EVIDENCE_SET=1; fi
            shift 2 ;;
        --metadata-stdin) METADATA_STDIN=1; shift ;;
        *) fail failed invalid_arguments "Unknown argument: $1" 'Use --extractor roslyn|codegraph|compare and --evidence .backups/path.' ;;
    esac
done
[[ $(uname -s) == Linux ]] || fail unavailable platform_unavailable 'Run in WSL Linux.' 'Use the PowerShell entry point with WSL Ubuntu.'
command -v python3 >/dev/null || fail unavailable python_unavailable 'Missing Python 3.' 'Provide an executable WSL Python 3 without changing global settings.'
STATUS_TOOL="$SOURCE_ROOT/99_Tools/Architecture/Pipeline/execution_status.py"
STATUS_ARGS=(--source "$SOURCE_ROOT" --extractor "$SELECTION")
if [[ "$EVIDENCE_SET" == 1 ]]; then STATUS_ARGS+=(--evidence "$EVIDENCE_RELATIVE"); fi
# Path performs selection/path/owner queries only: no copies, installs or completed extraction.
RUNTIME_ROOT=$(python3 -B "$STATUS_TOOL" path "${STATUS_ARGS[@]}") || exit $?
if [[ "$ACTION" == path ]]; then printf '%s\n' "$RUNTIME_ROOT"; exit 0; fi
if [[ "$EVIDENCE_SET" == 0 ]]; then EVIDENCE_RELATIVE=".backups/architecture/$SELECTION"; fi
EVIDENCE="$SOURCE_ROOT/$EVIDENCE_RELATIVE"
for tool in rsync flock sha256sum realpath; do
    command -v "$tool" >/dev/null || fail unavailable tool_unavailable "Missing: $tool" "Provide WSL $tool, then rerun the same selection; no automatic installation."
done
LOCK_ROOT="$HOME/.cache/dawnholder/locks"
[[ ! -L "$LOCK_ROOT" && $(realpath -m "$LOCK_ROOT") == "$LOCK_ROOT" ]] || fail failed linked_lock_root "Linked lock root: $LOCK_ROOT" 'Choose the owned unlinked WSL home; preserve the other workspace.'
mkdir -p -- "$LOCK_ROOT"
RUNTIME_LOCK="$LOCK_ROOT/architecture-$(basename -- "$RUNTIME_ROOT").lock"
EVIDENCE_KEY=$(printf '%s' "$EVIDENCE" | sha256sum | cut -c1-20)
EVIDENCE_LOCK="$LOCK_ROOT/architecture-evidence-$EVIDENCE_KEY.lock"
for lock in "$RUNTIME_LOCK" "$EVIDENCE_LOCK"; do
    [[ ! -L "$lock" ]] || fail failed linked_lock "Linked lock: $lock" 'Use an unlinked owned lock path; do not stop another executor.'
done
exec 8>"$RUNTIME_LOCK"
flock -n 8 || fail failed execution_locked "Another executor holds runtime lock: $RUNTIME_LOCK" 'Wait for that executor; do not stop its process.'
exec 9>"$EVIDENCE_LOCK"
flock -n 9 || fail failed execution_locked "Another executor holds evidence lock: $EVIDENCE_LOCK" 'Wait for that executor; do not stop its process.'
python3 -B "$STATUS_TOOL" claim "${STATUS_ARGS[@]}"
STATUS_WRITABLE=1
SETTINGS="$SOURCE_ROOT/99_Tools/Architecture/comparison-settings.json"
GOAL_RELATIVE=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["goalPath"])' "$SETTINGS")
MANIFEST="$SOURCE_ROOT/$GOAL_RELATIVE/input-manifest.json"
if [[ "$ACTION" != check ]]; then
    [[ -f "$MANIFEST" ]] || fail unavailable manifest_missing "Missing frozen manifest: $MANIFEST" 'Provide the unchanged frozen goal/input-manifest.json and freeze-record from the approved comparison.'
    if [[ "$METADATA_STDIN" == 1 ]]; then
        python3 -B "$STATUS_TOOL" metadata "${STATUS_ARGS[@]}"
    fi
    [[ -f "$EVIDENCE/source-git.json" ]] || fail unavailable source_metadata_missing "Missing source Git capture: $EVIDENCE/source-git.json" 'Run pwsh -File 99_Tools/Architecture/run-architecture.ps1 -Action prepare with the same -Extractor and -EvidencePath.'
    mkdir -p -- "$RUNTIME_ROOT/tool"
    EXCLUDES=(--exclude=CodeGraph/ --exclude=bin/ --exclude=obj/ --exclude=__pycache__/)
    if [[ "$SELECTION" == codegraph ]]; then EXCLUDES+=(--exclude=Roslyn/); fi
    # Delete is confined to this marker/lock checked tool copy; original inputs/install/cache remain intact.
    rsync -a --delete "${EXCLUDES[@]}" "$SOURCE_ROOT/99_Tools/Architecture/" "$RUNTIME_ROOT/tool/"
    if [[ "$SELECTION" != roslyn ]]; then
        BUNDLE_SOURCE="$SOURCE_ROOT/99_Tools/Architecture/CodeGraph/node_modules/@colbymchenry/codegraph-linux-x64"
        STATUS_WRITABLE=0
        python3 -B "$STATUS_TOOL" bundle "${STATUS_ARGS[@]}" --action "$ACTION" || exit $?
        STATUS_WRITABLE=1
        mkdir -p -- "$RUNTIME_ROOT/tool/CodeGraph" "$RUNTIME_ROOT/bundle"
        cp -- "$SOURCE_ROOT/99_Tools/Architecture/CodeGraph/syntax-context.cjs" "$RUNTIME_ROOT/tool/CodeGraph/syntax-context.cjs"
        rsync -a "$BUNDLE_SOURCE/" "$RUNTIME_ROOT/bundle/"
        chmod u+x -- "$RUNTIME_ROOT/bundle/bin/codegraph" "$RUNTIME_ROOT/bundle/node"
    fi
    STATUS_WRITABLE=0
    python3 -B "$RUNTIME_ROOT/tool/Pipeline/inputs.py" "$SOURCE_ROOT" "$RUNTIME_ROOT" "$MANIFEST" "$EVIDENCE" "$SELECTION" "$ACTION" || exit $?
    STATUS_WRITABLE=1
    cp -- "$MANIFEST" "$RUNTIME_ROOT/manifest.json"
    if [[ "$SELECTION" != codegraph ]]; then
        cp -- "$SOURCE_ROOT/global.json" "$RUNTIME_ROOT/global.json"
        cp -- "$SOURCE_ROOT/.editorconfig" "$RUNTIME_ROOT/.editorconfig"
        cp -- "$SOURCE_ROOT/99_Tools/.editorconfig" "$RUNTIME_ROOT/tool/.editorconfig"
    fi
fi
if [[ "$ACTION" == prepare ]]; then
    python3 -B "$STATUS_TOOL" record "${STATUS_ARGS[@]}" --action prepare --status completed \
        --reason inputs_prepared --message "Prepared selected inputs: $RUNTIME_ROOT; extraction has not run."
    printf 'Prepared: %s\n' "$RUNTIME_ROOT"
    exit 0
fi
RUNNER_ARGS=("$ACTION" --source "$SOURCE_ROOT" --runtime "$RUNTIME_ROOT" --evidence "$EVIDENCE" --extractor "$SELECTION")
if [[ "$SELECTION" != codegraph ]]; then
    STATE=$(mktemp -d "$RUNTIME_ROOT/.dotnet-state-XXXXXXXX")
    export DOTNET_CLI_HOME="$STATE/cli-home" NUGET_PACKAGES="$STATE/nuget/packages" NUGET_HTTP_CACHE_PATH="$STATE/nuget/http" NUGET_PLUGINS_CACHE_PATH="$STATE/nuget/plugins" NUGET_SCRATCH="$STATE/nuget/scratch"
    export DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0 DOTNET_GENERATE_ASPNET_CERTIFICATE=false DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 DOTNET_NOLOGO=1
    DOTNET_RELATIVE=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["dotnetRelativePath"])' "$SETTINGS")
    DOTNET="$HOME/$DOTNET_RELATIVE"
    [[ -x "$DOTNET" ]] || fail unavailable sdk_unavailable "Pinned SDK host missing: $DOTNET" 'Provide global.json SDK 10.0.301 at comparison-settings.json dotnetRelativePath; preserve existing SDK/global settings.'
    export DOTNET_HOST_PATH="$DOTNET" DOTNET_ROOT="$(dirname -- "$DOTNET")"
    RUNNER_ARGS+=(--dotnet "$DOTNET")
fi
if [[ "$SELECTION" != roslyn ]]; then
    export DO_NOT_TRACK=1 CODEGRAPH_TELEMETRY=0 CODEGRAPH_NO_UPDATE_CHECK=1 CODEGRAPH_NO_DAEMON=1
fi
cd -- "$RUNTIME_ROOT"
STATUS_WRITABLE=0
# The runner verifies/reuses FDs 8/9; its child commands close them through the original helper.
python3 -B "$SOURCE_ROOT/99_Tools/Architecture/Pipeline/runner.py" "${RUNNER_ARGS[@]}" || exit $?
