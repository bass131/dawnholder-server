#!/usr/bin/env bash
set -euo pipefail
[[ $# -gt 0 ]] || { echo 'Usage: bash 99_Tools/run_bot_fresh_recheck.sh Scenario [Scenario ...]' >&2; exit 2; }
exec bash "$(dirname -- "${BASH_SOURCE[0]}")/sync-wsl.sh" bot "$@"
