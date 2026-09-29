#!/usr/bin/env bash
# One fresh Debug server per scenario; nonzero exit if any scenario fails.
set -euo pipefail
exec bash "$(dirname -- "${BASH_SOURCE[0]}")/sync-wsl.sh" bot \
  MultiRosterSmoke EmergencyCombatSmoke BossStageClearSmoke BossFightSmoke \
  HpSyncSmoke RemoteAttackSmoke WhiffSwingSmoke RangedHitSmoke FreezeSmoke \
  ThunderboltAoeSmoke RangedWhiffSmoke DashSmoke TeleportSmoke EnemyAiSmoke \
  MapTransition M2BasicMovement
