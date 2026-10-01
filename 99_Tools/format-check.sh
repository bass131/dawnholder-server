#!/usr/bin/env bash
set -euo pipefail
fail() { echo "ERROR: $*" >&2; exit 1; }
ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd -P)
MANIFEST=''
EVIDENCE=''
while [[ $# -gt 0 ]]; do
  case "$1" in
    --manifest|--evidence) [[ $# -ge 2 ]] || fail "Missing value: $1"; if [[ $1 == --manifest ]]; then MANIFEST=$2; else EVIDENCE=$2; fi; shift 2 ;;
    *) fail "Unknown argument: $1" ;;
  esac
done
for tool in python3 realpath flock sha256sum cmp; do command -v "$tool" >/dev/null || fail "Missing tool: $tool"; done
STATE_BASE="${DAWNHOLDER_FORMAT_STATE_BASE:-$HOME/.cache/dawnholder/format-check}"
[[ "$STATE_BASE" == /* && ! -L "$STATE_BASE" ]] || fail 'Format state base must be an absolute real directory.'
mkdir -p -- "$STATE_BASE"
STATE=$(mktemp -d "$STATE_BASE/run-XXXXXXXX")
printf '%s\n' "$ROOT" > "$STATE/.dawnholder-format-owner"
exec 8>"$STATE/.format.lock"
flock -n 8 || fail 'Format state is already locked.'
[[ -n "$EVIDENCE" ]] || EVIDENCE="$STATE/evidence"
EVIDENCE=$(realpath -m -- "$EVIDENCE")
mkdir -p -- "$EVIDENCE"
[[ -z $(find "$EVIDENCE" -mindepth 1 -maxdepth 1 -print -quit) ]] || fail "Evidence directory is not empty: $EVIDENCE"
printf '%s\n' "$ROOT" > "$EVIDENCE/.dawnholder-format-evidence-owner"
export DOTNET_CLI_HOME="$STATE/cli-home" NUGET_PACKAGES="$STATE/nuget/packages" NUGET_HTTP_CACHE_PATH="$STATE/nuget/http" NUGET_PLUGINS_CACHE_PATH="$STATE/nuget/plugins" NUGET_SCRATCH="$STATE/nuget/scratch"
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1
source "$ROOT/99_Tools/Formatting/sdk.sh"
dawnholder_sdk "$ROOT" > "$EVIDENCE/sdk.txt"
cat "$EVIDENCE/sdk.txt"
cd -- "$ROOT"
GIT_ARGS=()
if [[ -z "$MANIFEST" ]]; then
  git rev-parse --show-toplevel >/dev/null || fail 'No Git checkout: provide the Windows source manifest.'
  export GIT_OPTIONAL_LOCKS=0
  git rev-parse HEAD > "$EVIDENCE/checkout-before.txt"
  git status --porcelain=v1 -z --untracked-files=all > "$EVIDENCE/status-before.z"
  GIT_ARGS=(--git)
else
  [[ -f "$MANIFEST" && ! -L "$MANIFEST" ]] || fail "Missing or linked manifest: $MANIFEST"
fi
record_plugins() {
  local path
  for path in 03_Client/Assets/Plugins/Shared/Shared.dll 03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll; do
    if [[ -f "$ROOT/$path" ]]; then sha256sum -- "$ROOT/$path"; fi
  done
}
record_plugins > "$EVIDENCE/plugins-before.sha256"
run() {
  local name=$1; shift
  local code=0
  printf 'cwd=%q\n' "$PWD" > "$EVIDENCE/$name.command.txt"
  printf '%q ' "$DOTNET" "$@" >> "$EVIDENCE/$name.command.txt"
  printf '\n' >> "$EVIDENCE/$name.command.txt"
  "$DOTNET" "$@" 8>&- > "$EVIDENCE/$name.log" 2>&1 || code=$?
  printf 'exit=%s\n' "$code" >> "$EVIDENCE/$name.command.txt"
  [[ $code == 0 ]] || { cat "$EVIDENCE/$name.log" >&2; fail "$name exited $code; evidence $EVIDENCE"; }
}
run formatter-version format --version
# Bootstrap only the independent tool at the source. Product builds run in the owned snapshot.
run tool-restore restore 99_Tools/Formatting/Formatting.csproj
run tool-build build 99_Tools/Formatting/Formatting.csproj --no-restore --nologo
CLI="$ROOT/99_Tools/Formatting/bin/Debug/net10.0/Formatting.dll"
SOURCE="$STATE/source"
mkdir -- "$SOURCE"
SNAPSHOT_ARGS=()
if [[ -n "$MANIFEST" ]]; then SNAPSHOT_ARGS=(--manifest "$MANIFEST"); fi
run source-copy "$CLI" snapshot --root "$ROOT" --after "$SOURCE" --dotnet "$DOTNET" --out "$EVIDENCE/source-copy.json" "${SNAPSHOT_ARGS[@]}"
cd -- "$SOURCE"
run product-restore restore Dawnholder.slnx
run source-tool-restore restore 99_Tools/Formatting/Formatting.csproj
for configuration in Debug Release; do
  run "shared-$configuration" build 98_Shared/Shared.csproj --configuration "$configuration" --no-restore --nologo
  run "clientnet-$configuration" build 04_ClientNet/Dawnholder.Client.Net.csproj --configuration "$configuration" --no-restore --nologo
done
if [[ -f "$SOURCE/99_Tools/Formatting.Tests/Formatting.Tests.csproj" ]]; then
  run tests-restore restore 99_Tools/Formatting.Tests/Formatting.Tests.csproj
fi
if [[ -z "$MANIFEST" ]]; then
  MANIFEST="$EVIDENCE/input-manifest.json"
  run manifest "$CLI" manifest --root "$SOURCE" --git-root "$ROOT" --dotnet "$DOTNET" --out "$MANIFEST"
  git -C "$ROOT" rev-parse HEAD > "$EVIDENCE/checkout-at-manifest.txt"
  git -C "$ROOT" status --porcelain=v1 -z --untracked-files=all > "$EVIDENCE/status-at-manifest.z"
  cmp -- "$EVIDENCE/checkout-before.txt" "$EVIDENCE/checkout-at-manifest.txt" || fail 'Checkout changed during setup.'
  cmp -- "$EVIDENCE/status-before.z" "$EVIDENCE/status-at-manifest.z" || fail 'Git status changed during setup.'
fi
run validate "$CLI" validate --root "$SOURCE" --dotnet "$DOTNET" --manifest "$MANIFEST"
run original-validate "$CLI" validate --root "$ROOT" --dotnet "$DOTNET" --manifest "$MANIFEST" --files-only "${GIT_ARGS[@]}"
record_plugins > "$EVIDENCE/plugins-after-setup.sha256"
cmp -- "$EVIDENCE/plugins-before.sha256" "$EVIDENCE/plugins-after-setup.sha256" || fail 'Source plug-in DLLs changed during setup.'
# No stub is accepted while independent test ownership is pending.
[[ -f "$SOURCE/99_Tools/Formatting.Tests/Formatting.Tests.csproj" ]] || fail 'Independent Formatting.Tests project is missing; tests and the complete check have not run.'
run tests-build build 99_Tools/Formatting.Tests/Formatting.Tests.csproj --no-restore --nologo
for entry in 'product:Dawnholder.slnx' 'tool:99_Tools/Formatting/Formatting.csproj' 'tests:99_Tools/Formatting.Tests/Formatting.Tests.csproj'; do
  name=${entry%%:*}; project=${entry#*:}
  run "$name-format" format whitespace "$project" --no-restore --verify-no-changes --exclude 98_Shared/Protocol/Generated/GenPackets.cs --report "$EVIDENCE/$name-format-report.json" --verbosity diagnostic
  run "$name-report" "$CLI" check-report --root "$SOURCE" --dotnet "$DOTNET" --manifest "$MANIFEST" --report "$EVIDENCE/$name-format-report.json" --zero
done
# Apply only in an owned snapshot, then compare every source using the manifest's actual parse options.
SNAPSHOT="$STATE/formatted"
mkdir -- "$SNAPSHOT"
run snapshot-copy "$CLI" snapshot --root "$SOURCE" --after "$SNAPSHOT" --dotnet "$DOTNET" --manifest "$MANIFEST" --out "$EVIDENCE/snapshot-copy.json"
cd -- "$SNAPSHOT"
run snapshot-product-restore restore Dawnholder.slnx
run snapshot-tool-restore restore 99_Tools/Formatting/Formatting.csproj
run snapshot-tests-restore restore 99_Tools/Formatting.Tests/Formatting.Tests.csproj
for configuration in Debug Release; do
  run "snapshot-shared-$configuration" build 98_Shared/Shared.csproj --configuration "$configuration" --no-restore --nologo
  run "snapshot-clientnet-$configuration" build 04_ClientNet/Dawnholder.Client.Net.csproj --configuration "$configuration" --no-restore --nologo
done
for entry in 'product:Dawnholder.slnx' 'tool:99_Tools/Formatting/Formatting.csproj' 'tests:99_Tools/Formatting.Tests/Formatting.Tests.csproj'; do
  name=${entry%%:*}; project=${entry#*:}
  run "snapshot-$name-apply" format whitespace "$project" --no-restore --exclude 98_Shared/Protocol/Generated/GenPackets.cs --report "$EVIDENCE/snapshot-$name-report.json" --verbosity diagnostic
done
cd -- "$SOURCE"
run preservation "$CLI" compare --root "$SOURCE" --after "$SNAPSHOT" --dotnet "$DOTNET" --manifest "$MANIFEST" --out "$EVIDENCE/preservation.json"
run tests test 99_Tools/Formatting.Tests/Formatting.Tests.csproj --no-build --logger 'console;verbosity=normal'
run final-validate "$CLI" validate --root "$ROOT" --dotnet "$DOTNET" --manifest "$MANIFEST" --files-only "${GIT_ARGS[@]}"
record_plugins > "$EVIDENCE/plugins-after.sha256"
cmp -- "$EVIDENCE/plugins-before.sha256" "$EVIDENCE/plugins-after.sha256" || fail 'Source plug-in DLLs changed during checks.'
printf 'Formatting checks passed; checkout and source hashes: %s; evidence: %s\n' "$MANIFEST" "$EVIDENCE"
