#!/usr/bin/env bash
set -euo pipefail

fail() {
    printf 'management backend: %s\n' "$*" >&2
    exit 2
}

init_config() {
    local source_repository git_entry git_directory
    if [[ -e "$config" || -L "$config" ]]; then
        printf 'management backend: configuration already exists: %s\n' "$config"
        return 0
    fi
    if [[ -d "$repo_root/.git" ]]; then
        source_repository=$repo_root
    elif [[ -f "$repo_root/.git" ]]; then
        IFS= read -r git_entry < "$repo_root/.git" || [[ -n "$git_entry" ]]
        git_entry=${git_entry%$'\r'}
        if [[ "$git_entry" == 'gitdir: '* ]]; then
            git_directory=${git_entry#gitdir: }
            if [[ "$git_directory" =~ ^[A-Za-z]:[/\\] ]]; then
                git_directory=$(wslpath -u "$git_directory") || return 1
            fi
            if [[ "$git_directory" =~ ^(/.+)/\.git/worktrees/([^/]+)$ \
                && "${BASH_REMATCH[2]}" != . && "${BASH_REMATCH[2]}" != .. ]]; then
                source_repository=${BASH_REMATCH[1]}
            fi
        fi
    fi
    if [[ -z "${source_repository:-}" ]]; then
        printf 'management backend: warning: cannot determine release source from %s/.git; configuration not created\n' "$repo_root" >&2
        return 1
    fi

    # JSON escaping and exclusive 0600 creation also cover paths with quotes and concurrent initializers.
    python3 - "$config" "$source_repository" <<'PY'
import json
import os
import sys

path, source = sys.argv[1:]
try:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, "w", encoding="utf-8") as output:
        json.dump({"release": {"sourceRepository": source}}, output)
        output.write("\n")
except FileExistsError:
    print(f"management backend: configuration already exists: {path}")
except OSError as error:
    print(f"management backend: warning: cannot create configuration {path}: {error}", file=sys.stderr)
    sys.exit(1)
else:
    print(f"management backend: created configuration: {path}")
PY
}

action=${1:-}
[[ "$action" == build || "$action" == test || "$action" == run || "$action" == init-config ]] \
    || fail 'usage: backend-wsl.sh build|test|run|init-config [arguments]'
shift
repo_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)
config="$HOME/.config/dawnholder/management/backend.json"
# Initialization must work even in a minimal checkout, before the copy lock, rsync or SDK selection.
if [[ "$action" == init-config ]]; then
    [[ $# -eq 0 ]] || fail 'usage: backend-wsl.sh init-config'
    init_config || exit 2
    exit 0
elif [[ "$action" == run && $# -eq 0 && ! -e "$config" && ! -L "$config" ]]; then
    init_config || true
fi

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

"$DOTNET" build "$backend" -c Release --artifacts-path "$artifacts" --disable-build-servers
if [[ $# -eq 0 && -f "$config" ]]; then
    set -- --config "$config"
fi
# Foreground exec preserves signals and stdin ownership for the window process which launched us.
exec "$DOTNET" "$artifacts/bin/ManagementBackend/release/Dawnholder.Management.Backend.dll" "$@"
