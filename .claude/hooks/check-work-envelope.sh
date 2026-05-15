#!/usr/bin/env bash
# .claude/hooks/check-work-envelope.sh
# Stop 훅 — 출구 안전망 (ADR-018)
# Edit/Write/MultiEdit 도구 호출 + 봉투 마커/헤더/WORK-ID 누락을 grep으로 감지.
# 누락 시 stderr 경고만 (exit 0) — AI가 다음 응답에서 보강. 차단 X.
#
# 검사 원칙 (Codex R2): LLM self-check 금지 — 망각하는 자에게 검사 위임 X.
# 기계적 grep만으로 판단.

# stdin으로 hook JSON payload 받음
INPUT=$(cat 2>/dev/null || echo "")

# transcript_path 추출 (sed 기반, jq 의존성 회피)
TRANSCRIPT_PATH=$(printf '%s' "$INPUT" | sed -n 's/.*"transcript_path"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')

[ -n "$TRANSCRIPT_PATH" ] || exit 0
[ -f "$TRANSCRIPT_PATH" ] || exit 0

# 마지막 user 메시지 이후의 줄들 = 직전 assistant 응답
# tac 대신 awk로 처리 (Windows Git Bash 호환성)
LAST_TURN=$(awk '
  /"role"[[:space:]]*:[[:space:]]*"user"/ { lines = ""; next }
  { lines = lines $0 "\n" }
  END { printf "%s", lines }
' "$TRANSCRIPT_PATH" 2>/dev/null)

[ -n "$LAST_TURN" ] || exit 0

# Edit/Write/MultiEdit 도구 사용 흔적
HAS_TOOL=$(printf '%s' "$LAST_TURN" | grep -cE '"name"[[:space:]]*:[[:space:]]*"(Edit|Write|MultiEdit)"' || true)

# 도구 사용 안 했으면 봉투 검사 안 함 (대화/질문/검증만 응답)
[ "$HAS_TOOL" -gt 0 ] || exit 0

# 봉투 항목 검사
MISSING=""
printf '%s' "$LAST_TURN" | grep -q 'work-envelope' || MISSING="$MISSING 마커(work-envelope)"
printf '%s' "$LAST_TURN" | grep -q '변경:' || MISSING="$MISSING 변경:"
printf '%s' "$LAST_TURN" | grep -q '검증:' || MISSING="$MISSING 검증:"
printf '%s' "$LAST_TURN" | grep -q '남은 것:' || MISSING="$MISSING 남은-것:"
printf '%s' "$LAST_TURN" | grep -q '학습 포인트:' || MISSING="$MISSING 학습-포인트:"
# WORK-ID는 봉투 마커 안에 `work-envelope: <id>` 형태로 포함됨
printf '%s' "$LAST_TURN" | grep -qE 'work-envelope:[[:space:]]*[^[:space:]]+' || MISSING="$MISSING WORK-ID"

if [ -n "$MISSING" ]; then
  cat >&2 <<EOF
⚠️ work-envelope 누락 감지 (Edit/Write/MultiEdit 도구 사용 응답에 봉투 없음)
누락 항목:$MISSING
참조: 00_Document/policies/reporting-format.md 2번 절 / ADR-018
→ 다음 응답에서 보강 권장 (차단 아님).
EOF
fi

exit 0
