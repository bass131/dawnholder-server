#!/usr/bin/env bash
# The Python boundary owns input snapshots, bounded processes and all results.
set -euo pipefail
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
export PYTHONDONTWRITEBYTECODE=1
exec python3 -B "$script_dir/check-module-boundaries.py" "$@"
