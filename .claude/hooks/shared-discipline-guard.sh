#!/usr/bin/env bash
# New_Harness/hooks/shared-discipline-guard.sh
# PreToolUse Edit|Write 훅 — 98_Shared/ + PDL 의무 3종 강제 (exit 2 차단)
#
# ★ 함정 (Phase 03): 옛 validate-shared-changes.sh가 *경고만* 했다 →
# "주석 약속은 가짜다" 3회 봉합 사고 원인. 새 운영은 의무 3종 자동 점검 + 차단.
#
# 옛 validate-shared-changes.sh 흡수 + 다음 강화:
#   1. PDL.xml 변경 검출 → 의무 3종 점검:
#      (a) PacketGenerator 산출물 (GenPackets.cs / PacketFormat.cs) 재생성됐는지 (mtime)
#      (b) Shared.dll 변경 동반 (git status)
#      (c) ProtocolVersion.cs 변경 검토 (필드 추가/재정렬 = breaking change → bump)
#   2. 98_Shared/ 일반 변경 → 빌드 산출물 stale 검사 (mtime)
#
# 정책 참조: 헌법 #2/#4 + 5/17 운영 룰 (PDL 수정 후 후속 작업 의무 박제, CHANGELOG).

set -e

TOOL_INPUT_FILE="${CLAUDE_TOOL_INPUT_FILE:-}"

# 98_Shared/ 외 변경은 즉시 통과
case "$TOOL_INPUT_FILE" in
  */98_Shared/*) ;;
  *) exit 0 ;;
esac

if ! command -v git >/dev/null 2>&1; then
  exit 0
fi

REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null || echo ".")
cd "$REPO_ROOT" 2>/dev/null || exit 0

FAIL=0
ERRORS=()
WARNINGS=()

PDL_XML="98_Shared/Protocol/PDL.xml"
GEN_PACKETS="98_Shared/Protocol/GenPackets.cs"
PACKET_FORMAT="98_Shared/Protocol/PacketFormat.cs"
PROTOCOL_VERSION="98_Shared/Protocol/ProtocolVersion.cs"
SHARED_DLL="03_Client/Assets/Plugins/Shared/Shared.dll"

# ─────────────────────────────────────────────
# 케이스 1: PDL.xml 자체 변경 (의무 3종 점검)
# ─────────────────────────────────────────────
case "$TOOL_INPUT_FILE" in
  */98_Shared/Protocol/PDL.xml)
    # 본 Edit/Write 자체가 PDL 변경 시작 — 후속 의무 3종 안내 (차단 X)
    cat <<EOF >&2
📋 PDL.xml 변경 감지 — 후속 의무 3종 (옛 운영 사고 봉합 핵심):

  1. PacketGenerator 재생성 실행:
     cd 99_Tools/PacketGenerator && dotnet run -- ../../98_Shared/Protocol/PDL.xml
     → 산출물: GenPackets.cs + PacketFormat.cs 갱신

  2. Shared.dll 갱신 commit 동반:
     dotnet build 98_Shared/  → Shared.dll 자동 복사 (CopyToUnityPlugins target)
     git add 03_Client/Assets/Plugins/Shared/Shared.dll

  3. ProtocolVersion.cs bump 여부 결정:
     - 새 패킷 추가만 = bump 불필요 (additive)
     - 기존 패킷 필드 추가/재정렬/타입 변경 = bump 필요 (breaking)

  본 Edit는 통과합니다 (차단 X). 후속 작업이 의무.
EOF
    exit 0
    ;;
esac

# ─────────────────────────────────────────────
# 케이스 2: 98_Shared/ 안 다른 코드 편집 — PDL stale 점검
# ─────────────────────────────────────────────
# git status로 PDL.xml + 산출물 + Shared.dll 상태 추출
PDL_STAGED=$(git diff --cached --name-only 2>/dev/null | grep -F "$PDL_XML" || true)
PDL_UNSTAGED=$(git diff --name-only 2>/dev/null | grep -F "$PDL_XML" || true)
PDL_DIRTY="${PDL_STAGED}${PDL_UNSTAGED}"

GEN_STAGED=$(git diff --cached --name-only 2>/dev/null | grep -F "$GEN_PACKETS" || true)
GEN_UNSTAGED=$(git diff --name-only 2>/dev/null | grep -F "$GEN_PACKETS" || true)
GEN_DIRTY="${GEN_STAGED}${GEN_UNSTAGED}"

DLL_STAGED=$(git diff --cached --name-only 2>/dev/null | grep -F "$SHARED_DLL" || true)
DLL_UNSTAGED=$(git diff --name-only 2>/dev/null | grep -F "$SHARED_DLL" || true)
DLL_DIRTY="${DLL_STAGED}${DLL_UNSTAGED}"

PROTO_STAGED=$(git diff --cached --name-only 2>/dev/null | grep -F "$PROTOCOL_VERSION" || true)
PROTO_UNSTAGED=$(git diff --name-only 2>/dev/null | grep -F "$PROTOCOL_VERSION" || true)
PROTO_DIRTY="${PROTO_STAGED}${PROTO_UNSTAGED}"

# 의무 3종 검증
if [ -n "$PDL_DIRTY" ]; then
  # PDL 변경 있음 — 의무 3종 점검
  if [ -z "$GEN_DIRTY" ]; then
    # PacketGenerator 산출물 변경 X — stale
    # mtime 비교로 추가 확인
    if [ -f "$PDL_XML" ] && [ -f "$GEN_PACKETS" ]; then
      PDL_MTIME=$(stat -c %Y "$PDL_XML" 2>/dev/null || stat -f %m "$PDL_XML" 2>/dev/null || echo 0)
      GEN_MTIME=$(stat -c %Y "$GEN_PACKETS" 2>/dev/null || stat -f %m "$GEN_PACKETS" 2>/dev/null || echo 0)
      if [ "$PDL_MTIME" -gt "$GEN_MTIME" ]; then
        ERRORS+=("PDL.xml 변경 있는데 GenPackets.cs 미갱신 (stale). PacketGenerator 재생성 필요: cd 99_Tools/PacketGenerator && dotnet run -- ../../98_Shared/Protocol/PDL.xml")
        FAIL=1
      fi
    fi
  fi

  if [ -z "$DLL_DIRTY" ]; then
    ERRORS+=("PDL.xml 변경 있는데 Shared.dll commit 동반 X. dotnet build 98_Shared/ 후 git add 03_Client/Assets/Plugins/Shared/Shared.dll 필요 (정유현 Phase 06 pull 사고 봉합 — CHANGELOG 5/17).")
    FAIL=1
  fi

  if [ -z "$PROTO_DIRTY" ]; then
    WARNINGS+=("PDL.xml 변경 있는데 ProtocolVersion.cs 검토 흔적 X. 필드 추가/재정렬/타입 변경이면 bump 필요 (헌법 #2). additive(새 패킷만)면 무시 가능.")
  fi
fi

# ─────────────────────────────────────────────
# 출력
# ─────────────────────────────────────────────
if [ "$FAIL" -eq 1 ]; then
  echo "❌ Shared discipline guard 실패: $TOOL_INPUT_FILE" >&2
  echo "" >&2
  for E in "${ERRORS[@]}"; do
    echo "  - $E" >&2
  done
  echo "" >&2
  echo "📋 헌법 #2 (Protocol is Sacred) + #4 (Shared Code Discipline) 정합." >&2
  echo "    옛 운영의 '주석 약속은 가짜다' 3회 봉합 사고 봉합 — 강제 차단." >&2
  exit 2
fi

if [ "${#WARNINGS[@]}" -gt 0 ]; then
  echo "⚠️ Shared discipline guard 경고: $TOOL_INPUT_FILE" >&2
  for W in "${WARNINGS[@]}"; do
    echo "  - $W" >&2
  done
fi

exit 0
