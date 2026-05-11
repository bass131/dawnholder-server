#!/usr/bin/env bash
# .claude/hooks/inject-current-pin.sh
# UserPromptSubmit 훅 — 입구 안전망 (ADR-018)
# 매 사용자 입력 직전 current-pin.txt 내용을 stdout으로 출력 →
# Claude Code가 이 출력을 사용자 prompt에 추가 컨텍스트로 주입.
# 학습 질문 끼어들어도 다음 턴에 작업 좌표 자동 복원.

set -e

PIN_FILE=".claude/state/current-pin.txt"

# 핀 파일 없거나 비어있으면 silent exit (정상)
[ -f "$PIN_FILE" ] || exit 0
[ -s "$PIN_FILE" ] || exit 0

# 핀 내용을 마커로 감싸서 출력 — AI가 "이건 자동 주입된 작업 좌표"임을 인지
cat <<EOF
<work-pin source=".claude/state/current-pin.txt">
[자동 주입 — 학습 질문 끼어들어도 작업 좌표 잃지 않게 매 턴 컨텍스트 상단에 박힘. 갱신은 ADR-018 정책 참조.]

$(cat "$PIN_FILE")
</work-pin>
EOF

exit 0
