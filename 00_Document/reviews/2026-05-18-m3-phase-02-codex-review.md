# M3 Phase 02 — Codex Review

> 작성 방식: 외부 `codex exec --sandbox danger-full-access` 실행은 데이터 유출 위험으로 차단되어, 현재 Codex 세션이 실제 파일을 직접 읽고 검증한 결과다. `2026-05-18-m3-phase-02-codex-raw-output.txt`는 참고만 했고, 아래 판단은 코드/테스트 재확인 기준이다.

## 검증 범위

- 대상: Phase 02 ProtocolVersion handshake 변경분
- 주요 파일: `GameSession.cs`, `UnityClientSession.cs`, `M2BasicMovement.cs`, `PacketGenerator`, `PDL.xml`, `GenPackets.cs`, `HandshakeHandlerTests.cs`
- 실행 확인:
  - `dotnet test Dawnholder.slnx --nologo` → **135 passed / 0 failed / 1 skipped**
  - `dotnet test Dawnholder.slnx --nologo --no-build --filter "FullyQualifiedName~HandshakeHandlerTests|FullyQualifiedName~PacketRoundTrip"` → **54 passed**
  - `git diff --check` → generated `GenPackets.cs` trailing whitespace 경고 8건

## 발견 사항

### 1. HIGH — mismatch 응답 전송 후 즉시 `Disconnect()`는 reason 전달을 보장하지 않음

- 위치: `02_Server/GameServer/Network/GameSession.cs:207-208`
- 근거: mismatch에서 `Send(fail.Write())` 직후 `Disconnect()`를 호출한다. 그런데 서버 `Session.Send()`는 큐에 넣고 비동기 `SendAsync`를 시작할 뿐이며, `Disconnect()`는 곧바로 `Shutdown/Close`와 `Clear()`를 수행한다.
- 관련 근거: `02_Server/Network/Session.cs:99-104`, `02_Server/Network/Session.cs:151`, `02_Server/Network/Session.cs:167-178`, `02_Server/Network/Session.cs:195-208`
- 영향: fail-closed 자체는 맞지만, `98_Shared/CLAUDE.md`의 "명확한 에러 코드로 거부" 약속은 실 TCP 클라이언트에서 깨질 수 있다. 현재 단위 테스트는 `Send()` override로 패킷 캡처만 하므로 실제 flush를 검증하지 않는다.
- 권장: `DisconnectAfterFlush` 또는 send-completion 후 close 패턴을 추가하고, mismatch 클라이언트가 `S_HandshakeResult(ok=false, reason)`을 실제 수신하는 통합 테스트를 박기.

### 2. MEDIUM — Unity client outbound gate가 없어 첫 패킷 순서가 thread timing에 의존할 수 있음

- 위치: `03_Client/Assets/Scripts/Network/UnityClientSession.cs:36`, `03_Client/Assets/Scripts/Network/UnityClientSession.cs:45`, `03_Client/Assets/Scripts/Network/UnityClientSession.cs:59-68`, `03_Client/Assets/Scripts/Input/LocalPlayerController.cs:89`, `03_Client/Assets/Scripts/Network/NetworkBootstrap.cs:45-50`, `04_ClientNet/Connector.cs:77`
- 근거: `UnityClientSession` 생성자에서 `Instance`가 먼저 세팅되고, `Connector`는 `session.Start()` 후 `OnConnected()`를 호출한다. `NetworkBootstrap`도 session factory 안에서 `_isConnected = true`를 먼저 박는다. 아주 짧지만 main thread `Update()`가 그 사이 `SendIntent()`를 호출할 수 있는 창이 있다.
- 영향: 의도는 첫 패킷 `C_Handshake`지만, 실 Unity 클라에서는 타이밍 의존이 남는다. 데모 중 흔한 실패는 아니어도, 헌법 #2의 "first packet" 약속을 코드로 완전히 닫지는 못한다.
- 권장: `UnityClientSession.HandshakeCompleted`를 두고 `SendIntent()`/일반 gameplay send는 완료 전 drop 또는 queue. 더 단순하게는 handshake OK 전 `Instance` 공개를 지연해도 된다.

### 3. MEDIUM — malformed `C_Handshake` payload 테스트가 빠져 있음

- 위치: `02_Server/GameServer/Network/GameSession.cs:190-193`, `98_Shared/Protocol/Generated/GenPackets.cs:460-509`
- 근거: 정상 frame size지만 body가 부족한 `C_Handshake`는 generated `Read()`에서 예외가 난다. 실제 socket 경로에서는 Phase 09의 `OnRecvCompleted` catch가 disconnect할 가능성이 높지만, 이번 Phase 테스트는 `OnRecvPacket()` 직접 호출이라 이 경로를 고정하지 않는다.
- 권장: `PacketSession` 경유 테스트로 `[size=4][id=C_Handshake]` 또는 `[size=5][id=C_Handshake][1 byte]`가 disconnect되고 player가 생성되지 않는지 추가.

### 4. MEDIUM — bool/string wire format 직접 round-trip 테스트가 없음

- 위치: `99_Tools/PacketGenerator/PacketFormat.cs:365-390`, `98_Shared/Protocol/Generated/GenPackets.cs:515-587`, `02_Server/GameServer.Tests/PacketRoundTripTests.cs`
- 근거: `HandshakeHandlerTests`가 `S_HandshakeResult`를 간접으로 읽기는 하지만, `PacketRoundTripTests`에 `C_Handshake` / `S_HandshakeResult` 직접 테스트가 없다. 특히 string은 첫 실수요자라 empty/ASCII/Unicode를 고정하는 편이 좋다.
- 권장: `S_HandshakeResult_RoundTrip_PreservesBoolVersionReason`, `S_HandshakeResult_RoundTrip_HandlesEmptyAndUnicodeReason`, `C_Handshake_RoundTrip_PreservesClientVersion` 추가.

### 5. LOW — handshake 이후 재-handshake가 protocol violation으로 명시되지 않음

- 위치: `02_Server/GameServer/Network/GameSession.cs:156-182`
- 근거: `_handshakeCompleted=true` 후 `C_Handshake`를 다시 보내면 switch에 case가 없어 unknown packet으로 drop된다.
- 영향: 보안 구멍은 아니지만 "Protocol is Sacred" 관점에서는 protocol violation을 명확히 로그/Disconnect하는 편이 진단 가치가 높다.
- 권장: duplicate handshake 테스트를 추가하고, 최소한 `[Trust] duplicate handshake` 로그 또는 disconnect 정책을 정하기.

### 6. LOW — generated 파일 trailing whitespace

- 위치: `98_Shared/Protocol/Generated/GenPackets.cs:480`, `503`, `537`, `541`, `547`, `570`, `574`, `581`
- 근거: `git diff --check` 경고.
- 영향: 동작 영향은 없지만 generated code라 계속 재발한다. 포맷 훅/CI가 `diff --check`를 쓰면 커밋 전 잡힌다.
- 권장: PacketGenerator 템플릿의 빈 줄 탭을 정리하거나 generated 파일은 trailing whitespace 검사에서 제외.

### 7. LOW — PDL unknown type silent skip은 여전히 남음

- 위치: `99_Tools/PacketGenerator/Program.cs:210`
- 근거: bool/string은 고쳤지만, unknown XML element는 여전히 `default: break`로 조용히 누락된다.
- 영향: Phase 02 직접 위반은 아니다. 다만 M3 Phase 03 이후 패킷이 계속 늘면 `<unit>` 같은 오타가 성공처럼 보일 수 있다.
- 권장: 새 패킷이 더 늘기 전 unknown type은 `InvalidDataException`으로 fail-fast.

## 가장 큰 리스크

1. **Reject reason delivery** — mismatch 응답을 보내고 즉시 닫는 현재 구현은 실제 클라가 reason을 받을지 보장하지 않는다. 단위 테스트 green과 실 TCP 동작 사이에 틈이 있다.
2. **Unity outbound ordering** — headless bot은 handshake 결과를 기다리지만 Unity 쪽은 handshake 완료 전 송신 게이트가 없다. 데모 안정성 기준으로 작지만 날카로운 race다.

## 완료 조건 검증

| 완료 조건 | 판단 | 근거 |
|---|---|---|
| 버전 일치 시 정상 진입 | PASS | `Happy_MatchingVersion_AcksAndEntersGame` + 전체 테스트 통과. `EnterGameWorld()`가 tick 후 player 생성. |
| 버전 mismatch 시 즉시 disconnect | CONCERN | 단위 테스트상 disconnect 호출은 PASS. 단, `S_HandshakeResult` reason이 실제 네트워크에서 도착하는지는 미검증. |
| 핸들러 단위 테스트 3건 통과 | PASS | 전체 테스트 135/0/1, 필터 테스트 54/0/0 통과. |
| PDL 변경 의무 3종 | PASS with NOTE | PDL/Generated/Shared.dll 변경 존재 + build/test 통과. commit은 리뷰 후 단계라 현재 uncommitted 상태. |

## Phase 03 진입 영향

Phase 03에서 `Handlers/`로 분리할 때 `HandleHandshake`를 그대로 외부 클래스로 옮기면 `_handshakeCompleted`와 `EnterGameWorld()` 접근 경계가 바로 걸린다. 추천은 핸들러가 필드를 직접 만지지 않게 `GameSession.CompleteHandshakeAndEnter(ushort clientVersion)` 같은 세션 메서드 하나로 캡슐화하는 것이다. 그러면 handler layer는 "패킷 decode + 세션 메서드 호출"만 맡고, lifecycle state는 `GameSession` 안에 남는다.

## 권장 수정 우선순위

1. `DisconnectAfterFlush` 또는 mismatch 통합 테스트 중 하나는 Phase 02 후속으로 바로 처리.
2. Unity `SendIntent()` handshake gate 추가.
3. `PacketRoundTripTests`에 handshake/bool/string 직접 라운드트립 추가.
4. malformed handshake frame 테스트 추가.
5. duplicate handshake 정책 명시.

## 결론

Phase 02의 핵심 목표인 "ProtocolVersion 약속을 실제 코드로 봉합"은 대부분 달성됐다. 다만 현 상태를 그대로 Phase 완료로 닫으면, mismatch reason 전달과 Unity first-packet 보장이라는 두 지점이 데모 직전 디버깅 리스크로 남는다. 둘 다 범위가 작으니 Phase 03 진입 전 또는 Phase 03 초반에 같이 봉합하는 편이 낫다.
