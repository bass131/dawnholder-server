#!/usr/bin/env bash
# New_Harness/hooks/dangerous-cmd-guard.sh
# PreToolUse Bash 훅 — 파괴 명령 차단 (exit 2 = block, 우회 불가)
#
# 차단 사유: 작업물 유실 + 헌법 절대 원칙 보호. settings.json의 `permissions.deny`
# 룰은 정확 매처 기반이고 글로브 우회 가능 → PreToolUse Hook 추가 안전망.
#
# 차단 vs ask: 본 Hook은 *차단*. 정말 필요하면 외부 셸(Git Bash 직접)에서 실행.
# Claude Code 도구 호출은 명령 단위 grep으로 잡힘 → 사용자가 의도적으로 우회하면
# 그건 명시적 결정 (별 셸).
#
# 환경변수: PreToolUse Bash의 입력 — Claude Code 공식 명세 의존. 추정 변수 후보 검사.
#
# 정책 참조: 00_Document/policies/grade-and-risk.md "irreversible" 깃발 + 헌법.

set -e

# 입력 명령 추출 — 환경변수 또는 arg ($1) 양쪽 호환
COMMAND="${CLAUDE_TOOL_INPUT_COMMAND:-${CLAUDE_TOOL_INPUT:-${1:-}}}"

if [ -z "$COMMAND" ]; then
  # 입력 없으면 그냥 통과 (잘못된 호출 가정)
  exit 0
fi

# ─────────────────────────────────────────────
# 차단 패턴 (정규식 — bash =~ 호환)
# ─────────────────────────────────────────────
BLOCKED=""
REASON=""

# 1. rm -rf / rm -fr — 재귀 강제 삭제
if [[ "$COMMAND" =~ rm[[:space:]]+-([rR][fF]|[fF][rR]) ]]; then
  BLOCKED="rm -rf (재귀 강제 삭제)"
  REASON="작업물 유실 위험. 특정 파일만 지우려면 'rm <파일>' 또는 'git checkout -- <파일>' 사용."
fi

# 2. git reset --hard — 워킹 디렉토리 + index 둘 다 파괴
if [[ "$COMMAND" =~ git[[:space:]]+reset[[:space:]]+.*--hard ]]; then
  BLOCKED="git reset --hard"
  REASON="워킹 디렉토리 + index 동시 파괴 = uncommitted 변경 영구 손실. 'git stash' 또는 'git checkout -- <특정 파일>'로 부분 정리."
fi

# 3. git checkout --force / -f — branch 전환 시 변경 무시 파괴
if [[ "$COMMAND" =~ git[[:space:]]+checkout[[:space:]]+.*(-f|--force) ]]; then
  BLOCKED="git checkout --force"
  REASON="다른 브랜치로 전환하면서 현재 변경 파괴. 안전한 대안: 'git stash → git checkout <branch>'."
fi

# 4. git push --force / -f — 원격 history 덮어쓰기 (협업 사고)
if [[ "$COMMAND" =~ git[[:space:]]+push[[:space:]]+.*(-f([[:space:]]|$)|--force) ]]; then
  BLOCKED="git push --force"
  REASON="원격 history 덮어쓰기 = 다른 팀원 작업 유실 가능. 'git push --force-with-lease' 사용 또는 main 외 브랜치만."
fi

# 5. git clean -fd / -fdx — untracked 파일 강제 삭제
if [[ "$COMMAND" =~ git[[:space:]]+clean[[:space:]]+.*(-fd|-fdx|-fx) ]]; then
  BLOCKED="git clean -fd*"
  REASON="untracked 파일 영구 삭제 (.env 같은 본인 자산 유실 위험). 'git clean -n'으로 미리보기 후 수동 처리."
fi

# 6. main 브랜치 force push (별도 강한 메시지)
if [[ "$COMMAND" =~ git[[:space:]]+push[[:space:]]+.*main.*(-f|--force) ]] || \
   [[ "$COMMAND" =~ git[[:space:]]+push[[:space:]]+--force.*main ]]; then
  BLOCKED="git push --force to main"
  REASON="main 브랜치 history 파괴 — 협업 전원 영향. 절대 금지. PR 흐름 사용."
fi

# 7. gh pr merge --admin (CODEOWNERS 우회)
if [[ "$COMMAND" =~ gh[[:space:]]+pr[[:space:]]+merge.*--admin ]]; then
  BLOCKED="gh pr merge --admin"
  REASON="CODEOWNERS 승인 우회 = 영역 분리 정책 위반. 정상 PR 흐름으로 머지."
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
