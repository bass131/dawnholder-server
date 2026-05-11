#!/usr/bin/env bash
# .claude/hooks/validate-phase-gate.sh
# Phase Post-flight 게이트 — -DONE.md 형식/필수필드 검증
# 누락 시 exit 2 로 차단 → Claude가 채워서 재시도하도록 강제

set -e

TOOL_INPUT_FILE="${CLAUDE_TOOL_INPUT_FILE:-}"

# *-DONE.md 만 대상
case "$TOOL_INPUT_FILE" in
  *-DONE.md) ;;
  *) exit 0 ;;
esac

if [ ! -f "$TOOL_INPUT_FILE" ]; then
  exit 0
fi

FAIL=0
ERRORS=()

# 1. YAML frontmatter 존재 확인
if ! head -1 "$TOOL_INPUT_FILE" | grep -q '^---$'; then
  ERRORS+=("YAML frontmatter 누락. 파일 첫 줄이 '---' 이어야 함.")
  FAIL=1
fi

# 2. frontmatter 안 필수 필드: summary, phase, status
FM=$(awk '/^---$/{c++; next} c==1{print} c==2{exit}' "$TOOL_INPUT_FILE")

for FIELD in summary phase status; do
  VAL=$(echo "$FM" | awk -F': ' -v f="$FIELD" '$1==f {sub(/^[^:]*: /,""); print}')
  if [ -z "$VAL" ]; then
    ERRORS+=("frontmatter 필수 필드 '$FIELD' 비어있거나 누락.")
    FAIL=1
  fi
done

# 3. 필수 섹션 5개 존재 확인
for SECTION in "## TL;DR" "## 5단계 보고" "## AC 검증 결과" "## 결정 흐름" "## 학습 일지 후보 키워드"; do
  if ! grep -qF "$SECTION" "$TOOL_INPUT_FILE"; then
    ERRORS+=("필수 섹션 '$SECTION' 누락.")
    FAIL=1
  fi
done

# 4. 5단계 보고 5개 항목 모두 등장
for ITEM in "무엇을 만들었나" "왜 필요한가" "어떻게 만들었나" "테스트 결과" "다음 스텝"; do
  if ! grep -qF "$ITEM" "$TOOL_INPUT_FILE"; then
    ERRORS+=("5단계 보고 항목 '$ITEM' 누락.")
    FAIL=1
  fi
done

# 5. AC 검증 결과 섹션이 비어있지 않은지 (다음 H2 전까지 최소 한 줄 내용)
AC_BODY=$(awk '/^## AC 검증 결과$/{flag=1; next} /^## /{flag=0} flag' "$TOOL_INPUT_FILE" | grep -v '^[[:space:]]*$' || true)
if [ -z "$AC_BODY" ]; then
  ERRORS+=("'## AC 검증 결과' 섹션 본문 비어있음. 실제 검증 명령 + 출력 박을 것.")
  FAIL=1
fi

if [ "$FAIL" -eq 1 ]; then
  echo "❌ Phase Post-flight 게이트 실패: $TOOL_INPUT_FILE"
  echo ""
  for E in "${ERRORS[@]}"; do
    echo "  - $E"
  done
  echo ""
  echo "📋 템플릿: .claude/templates/done-md-template.md 참조"
  echo "    Phase 완료 박제는 사실/검증 결과를 빠짐없이 박아야 합니다."
  exit 2
fi

echo "✅ Phase Post-flight 게이트 통과: $(basename "$TOOL_INPUT_FILE")"
exit 0
