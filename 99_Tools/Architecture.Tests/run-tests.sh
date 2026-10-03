#!/usr/bin/env bash
# Runs the independent Architecture checks in the owned WSL workspace under the same owner
# marker and lock as 99_Tools/Architecture/run-wsl.sh. The checks use the Python standard
# library only: no .NET, CodeGraph, restore or network step runs here. Temporary outputs live
# in a fresh verification/ subfolder; tool/, inputs, bundle and recorded evidence are untouched.
# Without the owned workspace, `python3 -B -m unittest discover -s <this folder>` runs the same
# checks with the system temporary folder.
set -euo pipefail
fail() { echo "ERROR: $*" >&2; exit 1; }

[[ $(uname -s) == Linux ]] || fail 'Run in WSL.'
TESTS_ROOT=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
SOURCE_ROOT=$(cd -- "$TESTS_ROOT/../.." && pwd -P)
WORKSPACE_KEY=$(printf '%s' "$SOURCE_ROOT" | sha256sum | cut -c1-20)
RUNTIME_ROOT="$HOME/.cache/dawnholder/architecture/$WORKSPACE_KEY"
MARKER="$RUNTIME_ROOT/.dawnholder-source"

[[ -f "$MARKER" && ! -L "$MARKER" ]] || fail 'Owned Architecture workspace is missing.'
[[ $(cat "$MARKER") == "$SOURCE_ROOT" ]] || fail 'Foreign runtime workspace.'
mkdir -p -- "$HOME/.cache/dawnholder/locks"
exec 8>"$HOME/.cache/dawnholder/locks/architecture-$WORKSPACE_KEY.lock"
flock -n 8 || fail 'Architecture workspace is in use.'

[[ ! -L "$RUNTIME_ROOT/verification" ]] || fail 'Linked verification folder.'
mkdir -p -- "$RUNTIME_ROOT/verification"
TEMPORARY=$(mktemp -d "$RUNTIME_ROOT/verification/run-XXXXXXXX")
trap 'rm -rf -- "$TEMPORARY"' EXIT
export TMPDIR="$TEMPORARY" PYTHONDONTWRITEBYTECODE=1

python3 -B -m unittest discover \
    --start-directory "$TESTS_ROOT" \
    --top-level-directory "$TESTS_ROOT" \
    --pattern 'test_*.py' \
    "$@" 8>&-
