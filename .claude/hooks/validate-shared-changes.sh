#!/usr/bin/env bash
# .claude/hooks/validate-shared-changes.sh
# shared/ 수정 후 양쪽 빌드 검증

set -e

TOOL_INPUT_FILE="${CLAUDE_TOOL_INPUT_FILE:-}"
case "$TOOL_INPUT_FILE" in
  */shared/*) ;;
  *) exit 0 ;;
esac

echo "🔎 shared/ 수정 감지 — 양쪽 빌드 체크 실행 중..."

# Server build
if [ -d "server" ]; then
  if ! dotnet build server/ --nologo -v quiet > /tmp/server-build.log 2>&1; then
    echo "❌ shared/ 변경 후 서버 빌드 실패. 로그: /tmp/server-build.log"
    tail -40 /tmp/server-build.log
    exit 2   # exit 2 = block, Claude에게 에러 노출
  fi
fi

# Shared library 자체 빌드 체크 (클라이언트는 Unity 에디터에서 컴파일)
if [ -d "shared" ]; then
  if ! dotnet build shared/ --nologo -v quiet > /tmp/shared-build.log 2>&1; then
    echo "❌ Shared 라이브러리 빌드 실패. 로그: /tmp/shared-build.log"
    tail -40 /tmp/shared-build.log
    exit 2
  fi
fi

# 패킷 파일이 변경됐다면 ProtocolVersion bump 여부 확인
if echo "$TOOL_INPUT_FILE" | grep -q "shared/Protocol/Packets/"; then
  if ! git diff --cached -- shared/Protocol/ProtocolVersion.cs 2>/dev/null | grep -q "^+.*Version"; then
    echo "⚠️  패킷 파일이 변경됐지만 ProtocolVersion.cs가 bump되지 않았습니다."
    echo "    breaking change(필드 재정렬, 타입 변경)라면 bump 필요."
    echo "    additive change(새 패킷, 끝에 새 key 추가)라면 무시 가능."
    # Warning only, don't block
  fi
fi

echo "✅ shared/ 양쪽 빌드 OK"
exit 0
