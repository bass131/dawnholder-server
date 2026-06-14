#!/usr/bin/env bash
# New_Harness/hooks/phase-gate-validator.sh
# PostToolUse Edit|Write 훅 — `-DONE.md` 박제 형식/필수필드 검증
#
# 누락 시 exit 2 로 차단 → Claude가 채워서 재시도하도록 강제.
#
# 옛 이름: validate-phase-gate.sh (옛 운영 ADR-015 그대로).
# 새 v1 정합 (Phase 03 산출물):
#   1) frontmatter에 `grade:` 필수 추가 (단순/보통/복잡/대규모, 복잡|대규모만 valid)
#   2) frontmatter에 `owner:` 필수 추가 (사람별 namespace 정합)
#   3) 5단계 보고 섹션은 *복잡 이상* 의무 (ADR-031; 단순/보통은 -DONE.md 자체 박지 않음)
#   4) MD/HTML 이중 박음 = *복잡 이상* 의무 (ADR-031 — 옛 대규모 한정에서 복잡으로 하향)
#
# 정책 참조: 00_Document/policies/pin-and-done.md + grade-and-risk.md

set -e

# stdin JSON payload 파싱 — $TOOL_INPUT_FILE 세팅
. "$(dirname "$0")/hook-common.sh"
parse_hook_payload

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
WARNINGS=()

# ─────────────────────────────────────────────
# 1. YAML frontmatter 존재 확인
# ─────────────────────────────────────────────
if ! head -1 "$TOOL_INPUT_FILE" | grep -q '^---$'; then
  ERRORS+=("YAML frontmatter 누락. 파일 첫 줄이 '---' 이어야 함.")
  FAIL=1
fi

# frontmatter 본문 추출
FM=$(awk '/^---$/{c++; next} c==1{print} c==2{exit}' "$TOOL_INPUT_FILE")

# ─────────────────────────────────────────────
# 2. 필수 필드 점검 — 옛 3 + 신규 2 = 총 5
# ─────────────────────────────────────────────
for FIELD in summary phase status grade owner; do
  VAL=$(echo "$FM" | awk -F': ' -v f="$FIELD" '$1==f {sub(/^[^:]*: /,""); print}')
  if [ -z "$VAL" ]; then
    ERRORS+=("frontmatter 필수 필드 '$FIELD' 비어있거나 누락.")
    FAIL=1
  fi
done

# ─────────────────────────────────────────────
# 3. grade 값 valid 점검
# ─────────────────────────────────────────────
GRADE=$(echo "$FM" | awk -F': ' '$1=="grade" {sub(/^[^:]*: /,""); print}' | tr -d '"' | tr -d "'")
case "$GRADE" in
  복잡|대규모) ;;
  단순|보통)
    ERRORS+=("grade='$GRADE'은 -DONE.md 박지 않는 등급입니다. 단순/보통은 work-pin + commit message로 충분 (grade-and-risk.md 표 5 참조). -DONE.md 삭제 또는 등급 재판정.")
    FAIL=1
    ;;
  "")
    # 이미 필수 필드 점검에서 잡힘 — 중복 메시지 회피
    ;;
  *)
    ERRORS+=("grade='$GRADE'는 valid 값 아님. 단순/보통/복잡/대규모 중 하나여야 함.")
    FAIL=1
    ;;
esac

# ─────────────────────────────────────────────
# 4. 항상 의무 섹션 4개
# ─────────────────────────────────────────────
for SECTION in "## TL;DR" "## AC 검증 결과" "## 결정 흐름" "## 학습 일지 후보 키워드"; do
  if ! grep -qF "$SECTION" "$TOOL_INPUT_FILE"; then
    ERRORS+=("필수 섹션 '$SECTION' 누락.")
    FAIL=1
  fi
done

# ─────────────────────────────────────────────
# 5. 복잡 이상 의무: 5단계 보고 (ADR-031 — 옛 대규모 한정 → 복잡 이상)
# ─────────────────────────────────────────────
if [ "$GRADE" = "복잡" ] || [ "$GRADE" = "대규모" ]; then
  if ! grep -qF "## 5단계 보고" "$TOOL_INPUT_FILE"; then
    ERRORS+=("복잡 이상 등급은 '## 5단계 보고' 섹션 의무 (캡스톤 평가 자산).")
    FAIL=1
  else
    # 5단계 보고 항목 5개 모두 등장
    for ITEM in "무엇을 만들었나" "왜 필요한가" "어떻게 만들었나" "테스트 결과" "다음 스텝"; do
      if ! grep -qF "$ITEM" "$TOOL_INPUT_FILE"; then
        ERRORS+=("5단계 보고 항목 '$ITEM' 누락.")
        FAIL=1
      fi
    done
  fi
fi

# ─────────────────────────────────────────────
# 6. AC 검증 결과 섹션 본문 비어있지 않음
# ─────────────────────────────────────────────
AC_BODY=$(awk '/^## AC 검증 결과$/{flag=1; next} /^## /{flag=0} flag' "$TOOL_INPUT_FILE" | grep -v '^[[:space:]]*$' || true)
if [ -z "$AC_BODY" ]; then
  ERRORS+=("'## AC 검증 결과' 섹션 본문 비어있음. 실제 검증 명령 + 출력 박을 것.")
  FAIL=1
fi

# ─────────────────────────────────────────────
# 7. (복잡 이상) MD/HTML 이중 박음 의무 (ADR-031 — 옛 대규모 한정 → 복잡 이상)
# ─────────────────────────────────────────────
# 옛 = WARNINGS (권장) → 대규모 ERRORS → ADR-031로 복잡 이상 ERRORS.
# 옛 자산(이전 -DONE.md)은 Edit 안 하면 자연 회피 (PostToolUse Edit/Write 시점만 발동).
# 복잡 이상 -DONE.md = HTML 페어 박지 않으면 차단 (HTML 먼저 박은 후 MD 박는 순서).
if [ "$GRADE" = "복잡" ] || [ "$GRADE" = "대규모" ]; then
  HTML_SIBLING="${TOOL_INPUT_FILE%.md}.html"
  if [ ! -f "$HTML_SIBLING" ]; then
    ERRORS+=("복잡 이상 등급은 MD + HTML 이중 박음 의무 (캡스톤 평가 자산). 대응 .html 파일 없음: $HTML_SIBLING")
    FAIL=1
  fi
fi

# ─────────────────────────────────────────────
# 출력
# ─────────────────────────────────────────────
if [ "$FAIL" -eq 1 ]; then
  echo "❌ Phase 게이트 실패: $TOOL_INPUT_FILE"
  echo ""
  for E in "${ERRORS[@]}"; do
    echo "  - $E"
  done
  echo ""
  echo "📋 템플릿: .claude/templates/done-md-template.md 참조 (옛) 또는 새 New_Harness 템플릿(Phase 05 산출물)."
  echo "    Phase 완료 박제는 사실/검증 결과를 빠짐없이 박아야 합니다."
  exit 2
fi

if [ "${#WARNINGS[@]}" -gt 0 ]; then
  echo "⚠️ Phase 게이트 통과 (경고 있음): $(basename "$TOOL_INPUT_FILE")"
  for W in "${WARNINGS[@]}"; do
    echo "  - $W"
  done
else
  echo "✅ Phase 게이트 통과: $(basename "$TOOL_INPUT_FILE")"
fi
exit 0
