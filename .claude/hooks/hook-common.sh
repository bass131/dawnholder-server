#!/usr/bin/env bash
# .claude/hooks/hook-common.sh
# Claude Code Hook payload 공통 파싱 헬퍼
#
# ★ 함정 (Phase 06 후 β 발견, 2026-05-21):
#   옛 hook 본문은 `$CLAUDE_TOOL_INPUT_*` 환경변수를 *추측*으로 사용했으나,
#   Claude Code 공식 명세 = PreToolUse/PostToolUse hook payload는 **stdin JSON**.
#   환경변수만 읽으면 입력이 빈 칸 → 보안 hook 전부 무력화 (가짜 약속).
#
# 본 헬퍼는 stdin JSON을 1차로 파싱하고, env vars로 fallback 박음 (양쪽 호환).
# python 의존 (jq 없는 Windows + Git Bash 환경 호환). python 없으면 env vars만 작동.
#
# 사용법 (hook 본문에서):
#   . "$(dirname "$0")/hook-common.sh"
#   parse_hook_payload
#   # → $TOOL_NAME, $TOOL_INPUT_FILE, $TOOL_INPUT_COMMAND 세팅됨
#
# Payload 스키마 (Claude Code 공식):
#   { "session_id": "...", "transcript_path": "...",
#     "tool_name": "Bash|Edit|Write|...",
#     "tool_input": { "command": "...", "file_path": "...", ... },
#     "tool_response": { ... }  # PostToolUse만 }

parse_hook_payload() {
  # stdin 읽기 — empty 또는 non-JSON 이면 빈 객체로 fallback
  local payload
  payload=$(cat 2>/dev/null || true)
  [ -z "$payload" ] && payload='{}'

  # python으로 3개 필드 한 번에 추출 (json.dumps로 quote escape 안전)
  local extracted
  extracted=$(printf '%s' "$payload" | python -c "
import sys, json
try:
    d = json.load(sys.stdin)
except Exception:
    d = {}
ti = d.get('tool_input', {}) if isinstance(d.get('tool_input'), dict) else {}
print('PARSED_TOOL_NAME=' + json.dumps(d.get('tool_name', '') or ''))
print('PARSED_TOOL_INPUT_FILE=' + json.dumps(ti.get('file_path', '') or ''))
print('PARSED_TOOL_INPUT_COMMAND=' + json.dumps(ti.get('command', '') or ''))
" 2>/dev/null || true)

  # eval 안전 — 자체 생성한 JSON-escaped 문자열만 평가
  if [ -n "$extracted" ]; then
    eval "$extracted"
  fi

  # env vars fallback (옛 추측 명세 호환 + python 부재 환경 대비)
  TOOL_NAME="${PARSED_TOOL_NAME:-${CLAUDE_TOOL_NAME:-}}"
  TOOL_INPUT_FILE="${PARSED_TOOL_INPUT_FILE:-${CLAUDE_TOOL_INPUT_FILE:-}}"
  TOOL_INPUT_COMMAND="${PARSED_TOOL_INPUT_COMMAND:-${CLAUDE_TOOL_INPUT_COMMAND:-${CLAUDE_TOOL_INPUT:-}}}"

  export TOOL_NAME TOOL_INPUT_FILE TOOL_INPUT_COMMAND
}
