#!/usr/bin/env bash
# New_Harness/hooks/circuit-breaker.sh
# PostToolUse 훅 — 같은 도구 N회 반복 호출 시 사용자에게 알림 (Stop 아님)
#
# 동기: AI 무한 재시도 = 토큰/시간 낭비 + 잘못된 가정 누적. 임계 도달 시
# *사용자가 판단*하도록 알림. 차단 X — false positive 위험이 차단 비용보다 큼.
#
# ★ 함정 (Phase 03 §함정): 정당한 반복 차단 금지.
#   - Bash 도구 제외 (테스트 fuzz 1000회, batch 명령 정상)
#   - 임계는 등급별 차등 (단순 = 5회, 보통 = 10회, 복잡 = 15회, 대규모 = 20회)
#   - 윈도우 = 최근 5분
#
# 정책 참조: 00_Document/policies/grade-and-risk.md (등급별 처리 패턴).

set -e

# stdin JSON payload 파싱 — $TOOL_NAME 세팅
. "$(dirname "$0")/hook-common.sh"
parse_hook_payload

# 입력 없으면 통과
[ -z "$TOOL_NAME" ] && exit 0

# ─────────────────────────────────────────────
# Bash 도구 제외 (정당한 반복 보호)
# ─────────────────────────────────────────────
case "$TOOL_NAME" in
  Bash) exit 0 ;;
esac

# ─────────────────────────────────────────────
# 로그 파일 + 윈도우 설정
# ─────────────────────────────────────────────
LOG_FILE=".claude/state/circuit-breaker.log"
mkdir -p "$(dirname "$LOG_FILE")" 2>/dev/null || true

# 현재 unix timestamp
NOW=$(date +%s)
WINDOW_SEC=300  # 최근 5분

# 라인 형식: <unix-ts> <tool>
echo "$NOW $TOOL_NAME" >> "$LOG_FILE"

# ─────────────────────────────────────────────
# 임계 결정 — work-pin "등급:" 라인에서 추출
# ─────────────────────────────────────────────
PIN_FILE=".claude/state/current-pin.txt"
GRADE=""
if [ -f "$PIN_FILE" ]; then
  # work-pin 본문에서 "등급: <값>" 또는 "grade: <값>" 패턴 grep
  GRADE=$(grep -E '^(등급|grade):' "$PIN_FILE" 2>/dev/null | head -1 | awk -F': ' '{print $2}' | awk '{print $1}' || true)
fi

case "$GRADE" in
  단순)   THRESHOLD=5 ;;
  보통)   THRESHOLD=10 ;;
  복잡)   THRESHOLD=15 ;;
  대규모) THRESHOLD=20 ;;
  *)      THRESHOLD=10 ;;  # 등급 불명 = 보통 기본값
esac

# ─────────────────────────────────────────────
# 최근 5분 윈도우 카운트
# ─────────────────────────────────────────────
SINCE=$((NOW - WINDOW_SEC))

# 윈도우 안 같은 도구 호출 카운트
COUNT=$(awk -v since="$SINCE" -v tool="$TOOL_NAME" '$1 >= since && $2 == tool' "$LOG_FILE" 2>/dev/null | wc -l | tr -d ' ')

# ─────────────────────────────────────────────
# 로그 가지치기 (윈도우 밖 라인 제거 — IO 부담 ↓)
# ─────────────────────────────────────────────
if [ -f "$LOG_FILE" ]; then
  LINES=$(wc -l < "$LOG_FILE" 2>/dev/null | tr -d ' ')
  if [ "$LINES" -gt 500 ]; then
    awk -v since="$SINCE" '$1 >= since' "$LOG_FILE" > "${LOG_FILE}.tmp" 2>/dev/null && mv "${LOG_FILE}.tmp" "$LOG_FILE"
  fi
fi

# ─────────────────────────────────────────────
# 임계 도달 시 알림 (Stop 아님 — 사용자 판단)
# ─────────────────────────────────────────────
if [ "$COUNT" -ge "$THRESHOLD" ]; then
  cat <<EOF >&2
⚠️ Circuit breaker — $TOOL_NAME 도구 반복 호출 임계 도달

  최근 5분 호출: $COUNT 회 (임계: $THRESHOLD, 등급: ${GRADE:-불명})

  AI 무한 재시도일 가능성:
    - 같은 파일 반복 Edit → 변경이 누적 X (이전 변경 인지 못함)?
    - 같은 검색 반복 → 결과 해석 막힘?
    - 잘못된 가정 누적?

  *작업 막지 않음* — 알림만. 본인이 판단:
    - 정당한 반복(반복 검증·batch)이면 무시
    - AI 막힌 거 같으면 멈추고 다른 접근 (다른 SubAgent / 사용자 개입)

  로그: .claude/state/circuit-breaker.log
EOF
fi

exit 0
