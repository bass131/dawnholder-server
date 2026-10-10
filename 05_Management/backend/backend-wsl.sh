#!/usr/bin/env bash
set -euo pipefail

fail() {
    printf 'management backend: %s\n' "$*" >&2
    exit 2
}

action=${1:-}
[[ "$action" == build || "$action" == test || "$action" == run ]] || fail 'usage: backend-wsl.sh build|test|run [arguments]'
shift
repo_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)
cache="$HOME/.cache/dawnholder/management"
snapshot="$cache/backend-src"
mkdir -p -- "$cache"
exec 9<>"$cache/backend-src.lock"
flock -n 9 || fail 'another backend build, test or run owns the WSL copy'
mkdir -p -- "$snapshot"
if [[ -f "$snapshot/.source-owner" ]]; then
    [[ $(cat -- "$snapshot/.source-owner") == "$repo_root" ]] || fail 'the WSL copy belongs to another checkout'
elif [[ -n $(find "$snapshot" -mindepth 1 -maxdepth 1 -print -quit) ]]; then
    fail 'refusing to overwrite an unowned WSL copy'
fi
printf '%s\n' "$repo_root" > "$snapshot/.source-owner"

mkdir -p -- "$snapshot/05_Management/backend" "$snapshot/cli-home" "$snapshot/tmp" \
    "$snapshot/nuget/packages" "$snapshot/nuget/http-cache" "$snapshot/nuget/plugins-cache" "$snapshot/nuget/scratch"
# The only deleting sync is bounded by this fixed, ownership-checked destination.
rsync -a --delete --exclude bin --exclude obj \
    "$repo_root/05_Management/backend/" "$snapshot/05_Management/backend/"
cp -- "$repo_root/global.json" "$repo_root/Directory.Build.props" "$repo_root/.editorconfig" "$snapshot/"

# Isolate all SDK side effects before the very first version query.
export DOTNET_CLI_HOME="$snapshot/cli-home"
export NUGET_PACKAGES="$snapshot/nuget/packages"
export NUGET_HTTP_CACHE_PATH="$snapshot/nuget/http-cache"
export NUGET_PLUGINS_CACHE_PATH="$snapshot/nuget/plugins-cache"
export NUGET_SCRATCH="$snapshot/nuget/scratch"
export TMPDIR="$snapshot/tmp"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_ADD_GLOBAL_TOOLS_TO_PATH=0
export DOTNET_NOLOGO=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0
export MSBUILDDISABLENODEREUSE=1
export UseSharedCompilation=false
source "$repo_root/99_Tools/Formatting/sdk.sh"
dawnholder_sdk "$snapshot"
cd -- "$snapshot"
backend="$snapshot/05_Management/backend/ManagementBackend/ManagementBackend.csproj"
tests="$snapshot/05_Management/backend/ManagementBackend.Tests/ManagementBackend.Tests.csproj"
artifacts="$snapshot/artifacts"
if [[ "$action" == test ]]; then
    exec "$DOTNET" test "$tests" --artifacts-path "$artifacts" --disable-build-servers "$@"
elif [[ "$action" == build ]]; then
    exec "$DOTNET" build "$backend" --artifacts-path "$artifacts" --disable-build-servers "$@"
fi

"$DOTNET" build "$backend" --artifacts-path "$artifacts" --disable-build-servers
if [[ $# -eq 0 && -f "$HOME/.config/dawnholder/management/backend.json" ]]; then
    set -- --config "$HOME/.config/dawnholder/management/backend.json"
fi
# Foreground exec preserves signals and stdin ownership for the window process which launched us.
exec "$DOTNET" "$artifacts/bin/ManagementBackend/debug/Dawnholder.Management.Backend.dll" "$@"
