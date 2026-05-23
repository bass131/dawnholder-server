#!/usr/bin/env bash
# .claude/hooks/reviewer-auto-trigger.sh
# PostToolUse Edit|Write 훅 — Tier 2-A reviewer 자동 호출 권유 (Hard, M3.6 Phase 03-B 4-5)
#
# ★ ADR-019 본문 약속 풀세트 봉합:
#   옛 = Soft (메인 세션 판단) — 까먹으면 검증 0 위험 (정유현 5/16 합류 → 5/22 = 1주 임박)
#   새 = Hard hook (조건 충족 시 명확한 알림 + 누적 로그) — 까먹기 차단 안전망
#   단 Hook 자체가 SubAgent 호출 권한 X → *알림 + 누적*까지. 호출은 메인 세션 책임.
#
# 트리거 조건 (subagent-routing.md §4-1 정합):
#   - 무조건 호출: 98_Shared/ 변경 / 02_Server/Handlers/ 변경 / Protocol 변경
#   - 무조건 스킵: 테스트 파일만 / -DONE.md / 옛 자산
#
# 정책 참조: 00_Document/policies/review-tiering.md + ADR-019 + subagent-routing.md §4-1

set -e

. "$(dirname "$0")/hook-common.sh"
parse_hook_payload

[ -z "$TOOL_INPUT_FILE" ] && exit 0

# ─────────────────────────────────────────────
# 무조건 스킵: 테스트 파일 + 메타 자산
# ─────────────────────────────────────────────
case "$TOOL_INPUT_FILE" in
  *Tests.cs|*Test.cs|*.Tests/*|*Tests/*) exit 0 ;;
  *-DONE.md|*-DONE.html) exit 0 ;;
  */CHANGELOG.md) exit 0 ;;
esac

# ─────────────────────────────────────────────
# 무조건 호출: 98_Shared/ + Handlers/ + Protocol
# ─────────────────────────────────────────────
TRIGGER=""
case "$TOOL_INPUT_FILE" in
  */98_Shared/Protocol/PDL.xml|*/98_Shared/Protocol/*.cs)
    TRIGGER="Protocol 변경 (헌법 #2 Protocol is Sacred)"
    ;;
  */98_Shared/*)
    TRIGGER="98_Shared 변경 (헌법 #4 Shared Code Discipline)"
    ;;
  */02_Server/*/Handlers/*Handler.cs|*/02_Server/Handlers/*Handler.cs)
    TRIGGER="02_Server/Handlers/ 변경 (신뢰 경계, 헌법 #3)"
    ;;
  */02_Server/*/GameSession.cs|*/02_Server/GameSession.cs)
    TRIGGER="GameSession.cs 변경 (lifecycle + first-packet 게이트)"
    ;;
esac

# 트리거 X면 통과 (≥10줄 조건부는 메인 세션이 판단 — Hook은 무조건 호출만 강제)
[ -z "$TRIGGER" ] && exit 0

# ─────────────────────────────────────────────
# 누적 + 알림
# ─────────────────────────────────────────────
LOG_FILE=".claude/state/reviewer-pending.txt"
mkdir -p "$(dirname "$LOG_FILE")" 2>/dev/null || true
echo "$(date -Iseconds) [$TRIGGER] $TOOL_INPUT_FILE" >> "$LOG_FILE" 2>/dev/null || true

cat <<EOF >&2
🔔 reviewer SubAgent 자동 호출 권유 (Tier 2-A 자동 리뷰, ADR-019 Hard)

  대상: $TOOL_INPUT_FILE
  트리거: $TRIGGER

  메인 세션 책임:
    1. 본 작업 묶음 마감 시 reviewer SubAgent 호출 (Agent tool, subagent_type=reviewer)
    2. 입력 = range / files / diff_summary 3종 박음
    3. reviewer 결과 = -DONE.md "AC 검증 결과" 섹션에 박음

  *작업 막지 않음* — 알림 + 누적만. 호출 망각 차단 안전망.

  스킵 사유 있으면 commit message 또는 work-pin에 한 줄 박음
  (예: "리뷰 스킵 사유: rename만, subagent-routing.md §4-1 정합").

  로그: .claude/state/reviewer-pending.txt
  정책: 00_Document/policies/review-tiering.md + ADR-019
EOF

exit 0
