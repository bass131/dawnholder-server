#!/usr/bin/env bash
# .claude/hooks/check-server-authority.sh
# client/ 수정 후 권위 위반 패턴 자동 감지

set -e

TOOL_INPUT_FILE="${CLAUDE_TOOL_INPUT_FILE:-}"
case "$TOOL_INPUT_FILE" in
  */client/Assets/Scripts/*) ;;
  *) exit 0 ;;
esac

# Prediction/ 은 predicted state 변경 허용 — 스킵
case "$TOOL_INPUT_FILE" in
  */Prediction/*) exit 0 ;;
esac

VIOLATIONS=()

# 패턴 1: 클라에서 HP/데미지 산술
if grep -nE '(currentHp|HP|Hitpoints)\s*[-+]=' "$TOOL_INPUT_FILE" 2>/dev/null; then
  VIOLATIONS+=("클라이언트에서 HP/데미지 변경 감지. 서버 전용입니다.")
fi

# 패턴 2: 인벤토리 변경
if grep -nE '(inventory|Inventory)\.(Add|Remove|Insert)\(' "$TOOL_INPUT_FILE" 2>/dev/null; then
  VIOLATIONS+=("클라이언트에서 인벤토리 변경 감지. 서버 패킷 수신 시에만 가능.")
fi

# 패턴 3: XP / Level / Currency 변경
if grep -nE '(experience|xp|level|gold|gems)\s*[-+]=' "$TOOL_INPUT_FILE" 2>/dev/null; then
  VIOLATIONS+=("클라이언트에서 통화/XP/레벨 변경 감지. 서버 전용입니다.")
fi

# 패턴 4: 게임플레이용 random
if grep -nE 'Random\.(Range|value)' "$TOOL_INPUT_FILE" 2>/dev/null | grep -viE '(particle|fx|cosmetic|sound)' ; then
  VIOLATIONS+=("클라이언트에서 random 굴림 감지. 게임플레이에 영향 주면 서버에서 굴려야 함.")
fi

if [ ${#VIOLATIONS[@]} -gt 0 ]; then
  echo "❌ $TOOL_INPUT_FILE 에서 서버 권위 원칙 위반 감지:"
  for v in "${VIOLATIONS[@]}"; do echo "   - $v"; done
  echo ""
  echo "→ 로직을 server/ + shared/Formulas로 옮기거나,"
  echo "  의도적인 cosmetic 코드라면 // AUTHORITY-OK: <이유> 주석을 추가하세요."
  # 명시적 예외 허용
  if grep -q "AUTHORITY-OK:" "$TOOL_INPUT_FILE"; then
    echo "ℹ️  AUTHORITY-OK 주석 발견, 통과시킵니다."
    exit 0
  fi
  exit 2
fi

exit 0
