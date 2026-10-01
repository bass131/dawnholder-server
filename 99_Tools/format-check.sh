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
for tool in python3 realpath flock sha256sum rsync; do command -v "$tool" >/dev/null || fail "Missing tool: $tool"; done
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
run() {
  local name=$1; shift
  local code=0
  printf 'cwd=%q\n' "$PWD" > "$EVIDENCE/$name.command.txt"
  printf '%q ' "$DOTNET" "$@" >> "$EVIDENCE/$name.command.txt"
  printf '\n' >> "$EVIDENCE/$name.command.txt"
  "$DOTNET" "$@" > "$EVIDENCE/$name.log" 2>&1 || code=$?
  printf 'exit=%s\n' "$code" >> "$EVIDENCE/$name.command.txt"
  [[ $code == 0 ]] || { cat "$EVIDENCE/$name.log" >&2; fail "$name exited $code; evidence $EVIDENCE"; }
}
run formatter-version format --version
run product-restore restore Dawnholder.slnx
run tool-restore restore 99_Tools/Formatting/Formatting.csproj
run tool-build build 99_Tools/Formatting/Formatting.csproj --no-restore --nologo
for configuration in Debug Release; do
  run "shared-$configuration" build 98_Shared/Shared.csproj --configuration "$configuration" --no-restore --nologo
  run "clientnet-$configuration" build 04_ClientNet/Dawnholder.Client.Net.csproj --configuration "$configuration" --no-restore --nologo
done
if [[ -f "$ROOT/99_Tools/Formatting.Tests/Formatting.Tests.csproj" ]]; then
  run tests-restore restore 99_Tools/Formatting.Tests/Formatting.Tests.csproj
fi
CLI="$ROOT/99_Tools/Formatting/bin/Debug/net10.0/Formatting.dll"
GIT_ARGS=()
if [[ -z "$MANIFEST" ]]; then
  git rev-parse --show-toplevel >/dev/null || fail 'No Git checkout: provide the Windows source manifest.'
  MANIFEST="$EVIDENCE/input-manifest.json"
  run manifest "$CLI" manifest --root "$ROOT" --dotnet "$DOTNET" --out "$MANIFEST"
  GIT_ARGS=(--git)
else
  [[ -f "$MANIFEST" ]] || fail "Missing manifest: $MANIFEST"
fi
run validate "$CLI" validate --root "$ROOT" --dotnet "$DOTNET" --manifest "$MANIFEST" "${GIT_ARGS[@]}"
# No stub is accepted while independent test ownership is pending.
[[ -f "$ROOT/99_Tools/Formatting.Tests/Formatting.Tests.csproj" ]] || fail 'Independent Formatting.Tests project is missing; tests and the complete check have not run.'
run tests-restore restore 99_Tools/Formatting.Tests/Formatting.Tests.csproj
run tests-build build 99_Tools/Formatting.Tests/Formatting.Tests.csproj --no-restore --nologo
for entry in 'product:Dawnholder.slnx' 'tool:99_Tools/Formatting/Formatting.csproj' 'tests:99_Tools/Formatting.Tests/Formatting.Tests.csproj'; do
  name=${entry%%:*}; project=${entry#*:}
  run "$name-format" format whitespace "$project" --no-restore --verify-no-changes --exclude 98_Shared/Protocol/Generated/GenPackets.cs --report "$EVIDENCE/$name-format-report.json" --verbosity diagnostic
  run "$name-report" "$CLI" check-report --root "$ROOT" --dotnet "$DOTNET" --manifest "$MANIFEST" --report "$EVIDENCE/$name-format-report.json" --zero
done
# Apply only in an owned snapshot, then compare every source using the manifest's actual parse options.
SNAPSHOT="$STATE/formatted"
mkdir -- "$SNAPSHOT"
for tree in 02_Server 04_ClientNet 98_Shared 99_Tools; do
  rsync -a --exclude='bin/' --exclude='obj/' --exclude='secrets/' --exclude='.env*' --exclude='*.log' --exclude='appsettings.Local.json' --exclude='appsettings.Development.json' "$ROOT/$tree/" "$SNAPSHOT/$tree/"
done
for input in Dawnholder.slnx global.json Directory.Build.props .editorconfig .gitattributes .github/workflows/dotnet-tests.yml Directory.Build.targets NuGet.config nuget.config packages.lock.json; do
  if [[ -f "$ROOT/$input" ]]; then mkdir -p -- "$SNAPSHOT/$(dirname -- "$input")"; cp -- "$ROOT/$input" "$SNAPSHOT/$input"; fi
done
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
cd -- "$ROOT"
run preservation "$CLI" compare --root "$ROOT" --after "$SNAPSHOT" --dotnet "$DOTNET" --manifest "$MANIFEST" --out "$EVIDENCE/preservation.json" "${GIT_ARGS[@]}"
run tests test 99_Tools/Formatting.Tests/Formatting.Tests.csproj --no-build --logger 'console;verbosity=normal'
run final-validate "$CLI" validate --root "$ROOT" --dotnet "$DOTNET" --manifest "$MANIFEST" "${GIT_ARGS[@]}"
printf 'Formatting checks passed; checkout and source hashes: %s; evidence: %s\n' "$MANIFEST" "$EVIDENCE"
