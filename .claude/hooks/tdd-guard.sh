#!/usr/bin/env bash
# New_Harness/hooks/tdd-guard.sh
# PreToolUse Edit|Write 훅 — TDD 강제 영역에서 대응 테스트 파일 부재 시 *경고만*
#
# 차단 아님 (exit 0) — 학부생 학습 호흡 유지. 다만 stderr에 안내 출력 +
# .claude/state/tdd-guard-log.txt에 누적 기록 (어느 파일이 테스트 없이 박혔는지 추적).
#
# 정책 참조: 00_Document/policies/grade-and-risk.md trust-boundary 깃발 + reporting-format.md.
#
# TDD 강제 영역 (Phase 03 결정 — 함정 §"TDD 강제 영역 결정" 해소):
#   - 02_Server/Handlers/        : 신뢰 경계 검증 코드 (헌법 #3)
#   - 02_Server/GameSession.cs   : 세션 lifecycle + first-packet 강제
#   - 98_Shared/Protocol/Packets/: PDL 자동 생성 코드 (변경 = breaking change 위험)
#   - 98_Shared/GameData/        : 공식·상수 (재현 가능한 테스트 의무)
#
# 영역 외 코드는 경고 X (학부생 학습 호흡).

set -e

# stdin JSON payload 파싱 — $TOOL_INPUT_FILE 세팅
. "$(dirname "$0")/hook-common.sh"
parse_hook_payload

# 입력 없으면 통과
[ -z "$TOOL_INPUT_FILE" ] && exit 0

# ─────────────────────────────────────────────
# TDD 강제 영역 매칭
# ─────────────────────────────────────────────
IN_TDD_ZONE=0
case "$TOOL_INPUT_FILE" in
  */02_Server/Handlers/*.cs)        IN_TDD_ZONE=1; ZONE="02_Server/Handlers/" ;;
  */02_Server/GameSession.cs)       IN_TDD_ZONE=1; ZONE="02_Server/GameSession.cs" ;;
  */98_Shared/Protocol/Packets/*.cs)IN_TDD_ZONE=1; ZONE="98_Shared/Protocol/Packets/" ;;
  */98_Shared/GameData/*.cs)        IN_TDD_ZONE=1; ZONE="98_Shared/GameData/" ;;
  *) exit 0 ;;
esac

# 테스트 파일이 아닌 *생산 코드*만 대상 (테스트 파일 자체 변경은 점검 X)
case "$TOOL_INPUT_FILE" in
  *Tests.cs|*Test.cs|*.Tests/*) exit 0 ;;
esac

# ─────────────────────────────────────────────
# 대응 테스트 파일 존재 점검
# ─────────────────────────────────────────────
# 추정 매핑:
#   02_Server/Handlers/PingHandler.cs → 02_Server.Tests/Handlers/PingHandlerTests.cs
#   02_Server/GameSession.cs → 02_Server.Tests/SessionTests.cs (또는 Lifecycle*.cs)
#   98_Shared/Protocol/Packets/S_Snapshot.cs → 02_Server.Tests/PacketRoundTripTests.cs (통합 회귀)
#
# 정확한 매핑은 실측 (M4 진입 후 재조정). 일단 *동명 +Tests 파일* 또는
# *해당 도메인 Tests 폴더 안 어떤 파일* 존재 여부 둘 다 점검.

BASE_NAME=$(basename "$TOOL_INPUT_FILE" .cs)
EXPECTED_TEST_NAMES=("${BASE_NAME}Tests.cs" "${BASE_NAME}Test.cs")

TEST_FOUND=0
if command -v git >/dev/null 2>&1; then
  REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null || echo ".")
else
  REPO_ROOT="."
fi

for TEST_NAME in "${EXPECTED_TEST_NAMES[@]}"; do
  if find "$REPO_ROOT/02_Server.Tests" -name "$TEST_NAME" 2>/dev/null | grep -q .; then
    TEST_FOUND=1
    break
  fi
done

# 02_Server/GameSession.cs는 특수 — Lifecycle/Broadcast/Handshake 등 분산 테스트 OK
if [ "$ZONE" = "02_Server/GameSession.cs" ]; then
  if find "$REPO_ROOT/02_Server.Tests" -name "*Lifecycle*.cs" -o -name "*Session*.cs" -o -name "*Broadcast*.cs" -o -name "*Handshake*.cs" 2>/dev/null | grep -q .; then
    TEST_FOUND=1
  fi
fi

# 98_Shared/Protocol/Packets/ → PacketRoundTripTests 또는 도메인 핸들러 테스트로 커버
if [ "$ZONE" = "98_Shared/Protocol/Packets/" ]; then
  if find "$REPO_ROOT/02_Server.Tests" -name "PacketRoundTrip*.cs" -o -name "*Handler*Tests.cs" 2>/dev/null | grep -q .; then
    TEST_FOUND=1
  fi
fi

# ─────────────────────────────────────────────
# 결과 출력 + 로깅
# ─────────────────────────────────────────────
if [ "$TEST_FOUND" -eq 0 ]; then
  cat <<EOF >&2
⚠️ TDD 영역 변경 (테스트 부재): $TOOL_INPUT_FILE
  영역: $ZONE
  기대 테스트: ${EXPECTED_TEST_NAMES[*]} (또는 도메인 통합 테스트)

  새 코드 박기 전에 테스트 먼저 박는 게 정신입니다 (TDD).
  Server Authority + Trust Boundary 같은 핵심 영역은 회귀 안전망 의무.

  경고만 — 작업 막지 않습니다. /work:plan 단계에서 테스트 페어 박을지 결정.
EOF

  # 누적 기록
  LOG_FILE=".claude/state/tdd-guard-log.txt"
  mkdir -p "$(dirname "$LOG_FILE")" 2>/dev/null || true
  echo "$(date -Iseconds) $TOOL_INPUT_FILE [$ZONE]" >> "$LOG_FILE" 2>/dev/null || true
fi

exit 0
