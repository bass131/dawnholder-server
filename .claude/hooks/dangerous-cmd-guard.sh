#!/usr/bin/env bash
# .claude/hooks/dangerous-cmd-guard.sh
# PreToolUse Bash 훅 — 파괴 명령 차단 (exit 2 = block, 우회 불가)
#
# ★ 본질 봉합 (M3.6 Phase 03-B 4-1, 2026-05-22):
#   옛 bash regex word-boundary 한계 → false positive 위험 (PR #43 응급 봉합 후속).
#   새 = **Python shlex.split 토큰화 기반 매칭**.
#   - 따옴표 안 literal 텍스트(`"... --admin ..."`)는 *데이터 토큰*으로 분리 → 차단 X
#   - 실행 명령 토큰(`gh pr merge --admin`)은 *단독 토큰* → 차단 ✓
#
# Python 의존: hook-common.sh가 이미 Python 의존 (PR #43 박힘). 본 hook도 같은 의존.
# ADR-020 정합 = Hook 환경 의존성 = Git Bash + Python 3 (M3.6 Phase 03-B에서 명시 박힘).
#
# 차단 vs ask: 본 Hook은 *차단*. 정말 필요하면 외부 셸(Git Bash 직접)에서 실행.
# Claude Code 도구 호출은 명령 단위 grep으로 잡힘 → 사용자가 의도적으로 우회하면
# 그건 명시적 결정 (별 셸).
#
# 정책 참조: 00_Document/policies/grade-and-risk.md "irreversible" 깃발 + 헌법.

set -e

. "$(dirname "$0")/hook-common.sh"
parse_hook_payload

COMMAND="$TOOL_INPUT_COMMAND"

if [ -z "$COMMAND" ]; then
  exit 0
fi

# ─────────────────────────────────────────────
# Python shlex.split로 토큰화 (본질 봉합)
# ─────────────────────────────────────────────
# tr -d '\r' — Windows CRLF 함정 봉합 (Git Bash + Python text mode = stdout \r\n).
# mapfile은 \n만 split → 각 토큰 끝 \r 잔재 → "rm\r" ≠ "rm" 비교 실패. tr로 제거.
mapfile -t TOKENS < <(printf '%s' "$COMMAND" | python -c '
import sys, shlex
try:
    for tok in shlex.split(sys.stdin.read()):
        print(tok)
except ValueError:
    pass  # quote 미스매치 등 = 빈 토큰 (안전 fallback)
' 2>/dev/null | tr -d '\r')

# 토큰 0개면 (parse 실패 또는 빈 명령) → 안전한 fallback = 통과
# (parse 깨진 명령은 보통 의도된 위험 명령 X)
if [ ${#TOKENS[@]} -eq 0 ]; then
  exit 0
fi

CMD="${TOKENS[0]}"
SUB1="${TOKENS[1]:-}"
SUB2="${TOKENS[2]:-}"

# 헬퍼: 토큰 배열에 특정 토큰이 *단독*으로 있나 검사
has_token() {
  local target="$1"; shift
  local t
  for t in "$@"; do
    if [ "$t" = "$target" ]; then return 0; fi
  done
  return 1
}

# ─────────────────────────────────────────────
# 차단 패턴 (토큰 기반)
# ─────────────────────────────────────────────
BLOCKED=""
REASON=""

# 1. rm -rf / -fr / -Rf / -fR (재귀 강제 삭제)
if [ "$CMD" = "rm" ]; then
  if has_token "-rf" "${TOKENS[@]}" || has_token "-fr" "${TOKENS[@]}" || \
     has_token "-Rf" "${TOKENS[@]}" || has_token "-fR" "${TOKENS[@]}" || \
     has_token "-rfd" "${TOKENS[@]}" || has_token "-rfv" "${TOKENS[@]}"; then
    BLOCKED="rm -rf (재귀 강제 삭제)"
    REASON="작업물 유실 위험. 특정 파일만 지우려면 'rm <파일>' 또는 'git checkout -- <파일>' 사용."
  fi
fi

# 2. git reset --hard (워킹 디렉토리 + index 둘 다 파괴)
if [ "$CMD" = "git" ] && [ "$SUB1" = "reset" ]; then
  if has_token "--hard" "${TOKENS[@]}"; then
    BLOCKED="git reset --hard"
    REASON="워킹 디렉토리 + index 동시 파괴 = uncommitted 변경 영구 손실. 'git stash' 또는 'git checkout -- <특정 파일>'로 부분 정리."
  fi
fi

# 3. git checkout --force / -f (브랜치 전환 시 변경 파괴)
if [ "$CMD" = "git" ] && [ "$SUB1" = "checkout" ]; then
  if has_token "--force" "${TOKENS[@]}" || has_token "-f" "${TOKENS[@]}"; then
    BLOCKED="git checkout --force"
    REASON="다른 브랜치로 전환하면서 현재 변경 파괴. 안전한 대안: 'git stash → git checkout <branch>'."
  fi
fi

# 4. git push --force / -f (원격 history 덮어쓰기)
if [ "$CMD" = "git" ] && [ "$SUB1" = "push" ]; then
  if has_token "--force" "${TOKENS[@]}" || has_token "-f" "${TOKENS[@]}"; then
    BLOCKED="git push --force"
    REASON="원격 history 덮어쓰기 = 다른 팀원 작업 유실 가능. 'git push --force-with-lease' 사용 또는 main 외 브랜치만."
  fi
fi

# 5. git clean -fd / -fdx / -fx (untracked 영구 삭제)
if [ "$CMD" = "git" ] && [ "$SUB1" = "clean" ]; then
  if has_token "-fd" "${TOKENS[@]}" || has_token "-fdx" "${TOKENS[@]}" || \
     has_token "-fx" "${TOKENS[@]}" || has_token "-dfx" "${TOKENS[@]}"; then
    BLOCKED="git clean -fd*"
    REASON="untracked 파일 영구 삭제 (.env 같은 본인 자산 유실 위험). 'git clean -n'으로 미리보기 후 수동 처리."
  fi
fi

# 6. gh pr merge --admin (CODEOWNERS 우회 예외 경로)
# policies/pr-and-merge-gate.md §4-A: 사유 박힘 + 사용자 명시 GO 후 진행.
# Hook 본문은 *최후 안전망* — settings.json permissions.ask가 1차 게이트.
# 사유 명시 환경변수 $CLAUDE_ADMIN_BYPASS_REASON 있으면 통과.
if [ "$CMD" = "gh" ] && [ "$SUB1" = "pr" ] && [ "$SUB2" = "merge" ]; then
  if has_token "--admin" "${TOKENS[@]}"; then
    if [ -z "${CLAUDE_ADMIN_BYPASS_REASON:-}" ]; then
      BLOCKED="gh pr merge --admin (사유 미박힘)"
      REASON="admin bypass = 예외 경로. 사유를 \$CLAUDE_ADMIN_BYPASS_REASON 환경변수에 박은 후 재호출. 정책: 00_Document/policies/pr-and-merge-gate.md §4-A."
    fi
    # 사유 박힘 = settings.json ask 매처가 1차 게이트, hook은 통과
  fi
fi

# ─────────────────────────────────────────────
# 출력
# ─────────────────────────────────────────────
if [ -n "$BLOCKED" ]; then
  cat <<EOF >&2
❌ 파괴 명령 차단: $BLOCKED

  명령: $COMMAND

  사유: $REASON

  정말 필요하면 외부 셸(Git Bash 직접)에서 실행 — 그건 명시적 본인 결정.
  Claude Code 도구 호출에서는 PreToolUse Hook이 차단합니다.

  정책: 00_Document/policies/grade-and-risk.md "irreversible" 깃발 + 헌법 절대 원칙.
EOF
  exit 2
fi

exit 0
