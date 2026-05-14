#!/usr/bin/env bash
# .claude/hooks/inject-current-pin.sh
# UserPromptSubmit 훅 — 입구 안전망 (ADR-018)
# 매 사용자 입력 직전 current-pin.txt 내용을 stdout으로 출력 →
# Claude Code가 이 출력을 사용자 prompt에 추가 컨텍스트로 주입.
# 학습 질문 끼어들어도 다음 턴에 작업 좌표 자동 복원.
#
# 확장 (2026-05-14): commit 안 된 -DONE.md 박제 검출 시 경고 주입.
# 본인이 짚은 "Phase 끝나면 commit/PR 깜빡 위험" 안전망.

set -e

PIN_FILE=".claude/state/current-pin.txt"

# ─────────────────────────────────────────────
# 섹션 1 — 작업 좌표 핀 주입 (기존)
# ─────────────────────────────────────────────

if [ -f "$PIN_FILE" ] && [ -s "$PIN_FILE" ]; then
  cat <<EOF
<work-pin source=".claude/state/current-pin.txt">
[자동 주입 — 학습 질문 끼어들어도 작업 좌표 잃지 않게 매 턴 컨텍스트 상단에 박힘. 갱신은 ADR-018 정책 참조.]

$(cat "$PIN_FILE")
</work-pin>
EOF
fi

# ─────────────────────────────────────────────
# 섹션 2 — commit 안 된 -DONE.md 박제 검출 (신규)
# ─────────────────────────────────────────────
# 본인 헌법: -DONE.md 박힌 후 commit + PR + 노션 박제 + 다음 액션이
# /session:end로 묶임. 깜빡 위험 안전망.

# Git 명령 가능한지 먼저 확인 (ADR-020 Bash PATH 함정 회피)
if command -v git >/dev/null 2>&1; then
  # -DONE.md로 끝나는 파일 중 staged/unstaged/untracked 모두 검사
  # git status --porcelain 출력 형식:
  #   M  path/to/file   (modified, staged)
  #   ?? path/to/file   (untracked)
  #    M path/to/file   (modified, unstaged)
  UNCOMMITTED_DONE=$(git status --porcelain 2>/dev/null | grep -E '\-DONE\.md$' || true)

  if [ -n "$UNCOMMITTED_DONE" ]; then
    cat <<EOF
<phase-completion-pending>
⚠️ commit 안 된 -DONE.md 박제가 있어요:

$UNCOMMITTED_DONE

본인 헌법: Phase 완료 = -DONE.md 박제 + commit + PR + 노션 + 다음 액션.
지금 /session:end 호출하면 차근차근 안내합니다. 깜빡 안전망입니다.

(작업 막지 않음 — 경고만. 다른 작업 먼저 할 거면 그냥 진행.)
</phase-completion-pending>
EOF
  fi
fi

exit 0
