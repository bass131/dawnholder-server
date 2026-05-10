---
description: 새 패킷을 양쪽 wiring까지 한 번에 추가
argument-hint: <C2S|S2C> <PacketName> <reason>
---

"새 패킷 추가" 워크플로우 시작. 사용자 요청:
**$ARGUMENTS**

`netcode` 서브에이전트에게 위임하세요. 브리프:

1. 적절한 숫자 범위에서 다음 빈 PacketId 선택 (98_Shared/CLAUDE.md 참조).
   방향은 첫 번째 인자.
2. `98_Shared/Protocol/Packets/<Name>.cs`에 `[MessagePackObject]` +
   `[Key(N)]` 인덱스로 패킷 struct 정의. 필드 목록은 컨텍스트로 명확하지
   않으면 메인 세션에 물어볼 것.
3. `98_Shared/Protocol/PacketId.cs`에 새 ID 등록.
4. 서버 측: `02_Server/GameServer/Handlers/`에 stub 핸들러 추가
   (로깅 후 return). dispatch 테이블에 wire 연결.
5. 클라이언트 측: `03_Client/Assets/Scripts/Network/`에 send helper(C2S용)
   또는 receive handler(S2C용) 추가.
6. 양쪽 빌드 확인. 기존 패킷에 대한 breaking change가 아니면
   ProtocolVersion bump 금지.

netcode 작업 후, 메인 세션은 핸들러 본문을 어느 에이전트가 채울지
사용자에게 물어봅니다 (gameplay / persistence / etc).
