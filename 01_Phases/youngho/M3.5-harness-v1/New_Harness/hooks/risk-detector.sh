#!/usr/bin/env bash
# New_Harness/hooks/risk-detector.sh
# PreToolUse Bash/Edit/Write 훅 — 위험 깃발 3종 자동 검출 + 등급 상향 통보
#
# ★ 함정 (Phase 03): 등급 자동 상향이 5/20 의논 핵심 안전망.
# 본인이 깜빡 단순 등급으로 처리하려는 변경이 trust-boundary일 때 강제 인지.
#
# 깃발 3종:
#   1. trust-boundary  — 02_Server/GameSession.cs / Handlers/ / Validation*
#   2. irreversible    — git push to main / gh pr merge / Protocol.Version bump / force push
#   3. unity-asset     — 03_Client/Assets/**/*.{prefab,unity,asset,mat}
#
# 동작:
#   - 깃발 0개: 통과 (exit 0)
#   - 깃발 1+개: stderr 알림 + .claude/state/risk-flags.txt 누적 + exit 0
#   - 차단 X: 정보 제공만. 본인이 등급 인지 후 진행.
#
# 정책 참조: 00_Document/policies/grade-and-risk.md §3 + §위험 깃발 표.

set -e

# 입력 — Edit/Write는 파일 경로, Bash는 명령
TOOL_INPUT_FILE="${CLAUDE_TOOL_INPUT_FILE:-}"
TOOL_INPUT_COMMAND="${CLAUDE_TOOL_INPUT_COMMAND:-${CLAUDE_TOOL_INPUT:-}}"

# 둘 다 없으면 통과
if [ -z "$TOOL_INPUT_FILE" ] && [ -z "$TOOL_INPUT_COMMAND" ]; then
  exit 0
fi

# ─────────────────────────────────────────────
# 깃발 검출
# ─────────────────────────────────────────────
FLAGS=()

# trust-boundary: 파일 경로 grep
if [ -n "$TOOL_INPUT_FILE" ]; then
  case "$TOOL_INPUT_FILE" in
    */02_Server/GameSession.cs|*/02_Server/Handlers/*|*/02_Server/*Validation*|*/02_Server/Network/*Auth*)
      FLAGS+=("trust-boundary")
      ;;
  esac
fi

# unity-asset: 파일 경로 grep
if [ -n "$TOOL_INPUT_FILE" ]; then
  case "$TOOL_INPUT_FILE" in
    */03_Client/Assets/*.prefab|*/03_Client/Assets/*.unity|*/03_Client/Assets/*.asset|*/03_Client/Assets/*.mat)
      FLAGS+=("unity-asset")
      ;;
    */03_Client/Assets/**/*.prefab|*/03_Client/Assets/**/*.unity|*/03_Client/Assets/**/*.asset|*/03_Client/Assets/**/*.mat)
      FLAGS+=("unity-asset")
      ;;
  esac
fi

# irreversible: 명령 grep + 파일 grep
if [ -n "$TOOL_INPUT_COMMAND" ]; then
  # git push to main (브랜치 명시 또는 현재 브랜치 = main 추정)
  if [[ "$TOOL_INPUT_COMMAND" =~ git[[:space:]]+push.*[[:space:]]+main[[:space:]:] ]] || \
     [[ "$TOOL_INPUT_COMMAND" =~ git[[:space:]]+push[[:space:]]+origin[[:space:]]+main ]]; then
    FLAGS+=("irreversible")
  fi
  # force push (모든 브랜치)
  if [[ "$TOOL_INPUT_COMMAND" =~ git[[:space:]]+push.*(-f([[:space:]]|$)|--force) ]]; then
    FLAGS+=("irreversible")
  fi
  # gh pr merge
  if [[ "$TOOL_INPUT_COMMAND" =~ gh[[:space:]]+pr[[:space:]]+merge ]]; then
    FLAGS+=("irreversible")
  fi
  # git reset --hard (dangerous-cmd-guard가 차단하지만 깃발도 박음)
  if [[ "$TOOL_INPUT_COMMAND" =~ git[[:space:]]+reset[[:space:]]+.*--hard ]]; then
    FLAGS+=("irreversible")
  fi
fi

# irreversible (파일): Protocol.Version bump / DB 마이그
if [ -n "$TOOL_INPUT_FILE" ]; then
  case "$TOOL_INPUT_FILE" in
    */98_Shared/Protocol/ProtocolVersion.cs)
      FLAGS+=("irreversible")
      ;;
    */02_Server/Migrations/*.cs|*/02_Server/Migrations/*.sql)
      FLAGS+=("irreversible")
      ;;
  esac
fi

# ─────────────────────────────────────────────
# 결과 처리
# ─────────────────────────────────────────────
if [ ${#FLAGS[@]} -eq 0 ]; then
  exit 0
fi

# 중복 제거
UNIQUE_FLAGS=$(printf '%s\n' "${FLAGS[@]}" | sort -u | tr '\n' ',' | sed 's/,$//')
FLAG_COUNT=$(printf '%s\n' "${FLAGS[@]}" | sort -u | wc -l | tr -d ' ')

# 누적 기록
LOG_FILE=".claude/state/risk-flags.txt"
mkdir -p "$(dirname "$LOG_FILE")" 2>/dev/null || true
echo "$(date -Iseconds) [$UNIQUE_FLAGS] ${TOOL_INPUT_FILE:-$TOOL_INPUT_COMMAND}" >> "$LOG_FILE" 2>/dev/null || true

# ─────────────────────────────────────────────
# stderr 알림
# ─────────────────────────────────────────────
cat <<EOF >&2
⚠️ 위험 깃발 자동 검출: $UNIQUE_FLAGS ($FLAG_COUNT 종)

  대상: ${TOOL_INPUT_FILE:-$TOOL_INPUT_COMMAND}

  자동 등급 상향:
    - 깃발 1개 → 1단계 상향 (예: 보통 → 복잡)
    - 깃발 2개+ → 2단계 상향 (예: 보통 → 대규모)

  깃발 의미:
    - trust-boundary  : 헌법 #3 — 한 줄 실수가 보안 구멍
    - irreversible    : 되돌리는 비용 큼 (push/merge/migration)
    - unity-asset     : YAML 자동 머지 충돌 + prefab 백업 사고 (Phase 08 사고)

  본 작업이 *정말 단순한 변경인지* 본인이 인지 후 진행.
  양식 부담 적절히 자동 상향 — 양식 노이즈 X, 안전망 ↑.

  로그: .claude/state/risk-flags.txt
  정책: 00_Document/policies/grade-and-risk.md
EOF

exit 0
