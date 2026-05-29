#!/usr/bin/env bash
# .claude/hooks/convention-size-guard.sh
# PostToolUse Edit|Write 훅 — 핵심 파일 줄 수 임계 경고 (Code Convention §2.3 / ADR-028)
#
# God class 비대화 *조기 경고*. 차단 X (exit 0) — 거친 신호일 뿐 (정확한 God class 판정은
# reviewer 축 6 + 사람). "또 커지고 있다"를 자동으로 알려주는 보조 장치.
#
# 임계 = 600줄 (CODE_CONVENTION §2.3: "600줄+ 단일 클래스 = 거의 확실히 God class").
# 현재 GameMap/GameSession/UnityClientSession은 이미 초과 → 리팩토링 전까지 경고 뜸 (의도된 신호, 부록 A).

set -e

# stdin JSON payload 파싱 — $TOOL_INPUT_FILE 세팅 (phase-gate-validator.sh 정합)
. "$(dirname "$0")/hook-common.sh"
parse_hook_payload

# 감시 대상 핵심 파일만 (부록 A 리팩토링 대상)
case "$TOOL_INPUT_FILE" in
  */GameMap.cs|*/GameSession.cs|*/UnityClientSession.cs) ;;
  *) exit 0 ;;
esac

if [ ! -f "$TOOL_INPUT_FILE" ]; then
  exit 0
fi

THRESHOLD=600
LINES=$(wc -l < "$TOOL_INPUT_FILE" | tr -d ' ')

if [ "$LINES" -gt "$THRESHOLD" ]; then
  echo "⚠️ Code Convention §2.3 경고: $(basename "$TOOL_INPUT_FILE") = ${LINES}줄 (임계 ${THRESHOLD})." >&2
  echo "   God class 의심 — CODE_CONVENTION §2.2(2+ 도메인이면 컨테이너+System 분리) 점검 권장." >&2
  echo "   (차단 아님 — 조기 경고. 정확한 판정은 reviewer 축 6. ADR-028)" >&2
fi

exit 0
