---
summary: M3 Phase 02 완료 — 헌법 #2 "Protocol is Sacred" 가짜 약속 1번째 봉합. PacketGenerator bool/string 결함 동반 fix(blocker) + PDL C_Handshake/S_HandshakeResult 신설 + GameSession first-packet 강제 패턴 + 04_ClientNet(Unity wrapper)/헤드레스 봇 양쪽 handshake 전송 흐름 + HandshakeHandlerTests 3건(happy/mismatch/non-handshake 첫 패킷) 신설. γ 방식 4회차 = Codex β 직접 검증 후 옵션 B 채택 (CompleteHandshakeAndEnter 캡슐화 + duplicate handshake 정책 + Unity SendIntent gate + PacketRoundTripTests 11건 추가). dotnet test 151/0/1 green (Phase 01 132 → +19). 잔존 알려진 결함 4건은 응급 데모 영향 작아 본 마감 전 별도 Phase 봉합. 다음 = M3 Phase 03 핸들러 layer 분리.
phase: 02-protocol-version-handshake
work-id: phase02-protocol-version-handshake
status: done
completed_at: 2026-05-18
commit: TBD
---

# Phase 02 — ProtocolVersion 핸드셰이크 완료 박제

**작업 시간**: ~90분 (응급 모드 예상 1.5h 정합)

## TL;DR

헌법 #2 "Protocol is Sacred"의 *가짜 약속 1번째* 봉합 — `98_Shared/CLAUDE.md`에 "핸드셰이크 코드 미구현" 박혀있던 자리에 application-layer handshake 박음. 클라가 첫 패킷으로 `C_Handshake { clientVersion }` 전송, 서버 `GameSession`이 first-packet 강제 패턴으로 다른 패킷 거절 + Disconnect, version mismatch도 즉시 Disconnect (헌법 #3 정합 — timeout 안 기다림). **PacketGenerator bool/string 결함은 본 Phase의 blocker라 동반 fix** (S_HandshakeResult가 두 타입의 첫 실수요자). dotnet test 135/0/1 (Phase 01 baseline 132 → +3 HandshakeHandlerTests, 회귀 0).

## 5단계 보고

- **무엇을 만들었나** —
  - **PacketGenerator 도구 fix (blocker 봉합)**: `Program.cs` switch에 `bool` 별도 분기 신설(BinaryPrimitives에 LittleEndian 변종 없음 → byte 패턴 차용), `ToMemberType`의 dead `Boolean` 케이스 제거. `PacketFormat.cs`: `ReadBoolFormat`/`WriteBoolFormat` 신설(0/1 매핑), `ReadStringFormat`/`WriteStringFormat` 정정(옛 `Segment.Array` 미정의 + NET_LEGACY 분기 깨짐 → `BinaryPrimitives.TryWriteUInt16LittleEndian` + `Encoding.Unicode.GetByteCount/GetBytes(ReadOnlySpan<char>, Span<byte>)` 패턴, netstandard2.1 지원)
  - **PDL.xml**: `C_Handshake { ushort clientVersion }` + `S_HandshakeResult { bool ok, ushort serverVersion, string reason }` 신설 (PacketID 7, 8 자동 부여 — 은퇴 재사용 금지 룰 정합)
  - **`98_Shared/Protocol/Generated/GenPackets.cs`**: 재생성 (bool/string 정상 wire format, 회귀 안전망 확보)
  - **`02_Server/GameServer/Network/GameSession.cs`**:
    - `bool _handshakeCompleted` 필드 신설 (first-packet 강제 게이트)
    - `OnConnected`: AddPlayer 흐름 제거 → 로그만 (handshake 대기)
    - `OnRecvPacket`: `_handshakeCompleted==false`이면 → C_Handshake만 dispatch, 다른 거 거절+Disconnect
    - `HandleHandshake`: 검증(== 비교) → ok 시 `S_HandshakeResult(ok=true)` + `EnterGameWorld()` / mismatch 시 `S_HandshakeResult(ok=false, reason)` + 즉시 Disconnect
    - `EnterGameWorld()`: 옛 OnConnected의 AddPlayer 흐름 통째 이동 (protected — 테스트 mock 접근)
    - `MarkHandshakeCompletedForTest()`: protected helper (lifecycle/rate-limit 테스트가 handshake 단계 우회)
  - **`04_ClientNet`(Unity wrapper, `03_Client/Assets/Scripts/Network/UnityClientSession.cs`)**: OnConnected에서 `C_Handshake { clientVersion = ProtocolVersion.Current }` Send + OnRecvPacket dispatch에 `S_HandshakeResult` 추가(ok 로그 / fail Disconnect)
  - **`99_Tools/headless-bot/Scenarios/M2BasicMovement.cs`**: connectedEv 후 C_Handshake 전송 → handshakeResultEv 대기 → ok이면 enterMapEv 대기 진행 (옛 흐름 호환 — connect→entermap 사이에 handshake 한 단계 박힘)
  - **`02_Server/GameServer.Tests/Network/HandshakeHandlerTests.cs`** 신설 (3건):
    - `Happy_MatchingVersion_AcksAndEntersGame`
    - `Mismatch_HigherVersion_RejectsAndDisconnects`
    - `NonHandshakeFirstPacket_Rejected_NoEntry`
  - **기존 테스트 적응**: `GameSessionLifecycleTests` + `GameSessionRateLimitTests`의 `TestGameSession.OnConnected` override에 `MarkHandshakeCompletedForTest() + EnterGameWorld()` (handshake 우회 = 옛 흐름 재현). race/rate 검증 본질엔 영향 X
  - **문서 정정**: `98_Shared/CLAUDE.md:19` "M3 Phase 02 처리 예정" → "M3 Phase 02 봉합 완료", `ProtocolVersion.cs` 주석 "**다음 단계**" → "**핸드셰이크 봉합 (M3 Phase 02 완료)**"
- **왜 필요한가** —
  - 헌법 #2의 첫 *가짜 약속* — "프로토콜은 신성하다"고 박혀있는데 *버전 핸드셰이크 코드는 미구현*인 상태가 7 Phase째 잔존. Phase 11 M2.5에서 봉합한 "약속은 가짜다" 두 번째 증명(rate-limit) 다음의 **첫 번째 케이스**가 본 Phase
  - 응급 데모(5/20 교수 중간 면담)에서 두 명 접속 시 클라/서버 버전 불일치가 *silent하게* 게임 진행 후 desync로 표출되면 디버깅 곡선 폭발. handshake가 진입 *전에* hard error로 차단
  - PacketGenerator bool/string 결함 = `99_Tools/CLAUDE.md` "M2.5 후 또는 새 패킷 추가 직전 fix 대상" 박혀있던 룰. S_HandshakeResult가 두 타입의 첫 실수요자 → *지금* 봉합 의무 (분리 시 의존성 헝클어짐)
- **어떻게 만들었나** —
  - **first-packet 강제 패턴 선택지**: (A) `_handshakeCompleted` 플래그 + OnRecvPacket 게이트 vs (B) OnRecv 자체에서 첫 호출 격리 vs (C) 별도 state machine. → **(A)** 채택. 이유: 응급 모드 minimum + GameSession 도메인 안에 박혀 OnRecv(PacketSession base) 영향 X + race 안전 (OnRecvPacket은 단일 socket 워커 스레드 직렬 호출)
  - **버전 비교 방식**: (A) `==` 비교 vs (B) compatibility 표(`{2: {1, 2}, ...}`). → **(A)** 채택 (응급 모드). 호환 가능 minor version 거절은 *false positive*지만 응급 데모 환경에선 양쪽 동일 빌드라 영향 X. 본 마감 전 별도 Phase에서 호환표 + 옛 클라 안내 메시지
  - **mismatch 후 S_HandshakeResult Send 후 Disconnect 순서**: ok=false 회신을 *먼저* 보내고 Disconnect (TCP는 보낸 데이터의 in-flight를 close 시점에 flush). 클라가 reason을 읽고 사용자에게 메시지 가능. timeout 안 기다림 = 헌법 #3 정합
  - **테스트 mock 패턴**: 기존 lifecycle/rate-limit는 *handshake 이후*의 race/rate 검증이라 본질이 변경된 게 아님. `TestGameSession.OnConnected`에서 `MarkHandshakeCompletedForTest()+EnterGameWorld()` 호출로 *옛 흐름 그대로 재현* — 정확한 mock 분리 (handshake 검증은 신설 HandshakeHandlerTests가 단독)
  - **PacketGenerator bool/string fix 범위**: (A) blocker만 최소 fix vs (B) 결함 클래스 통째 정리(NET_LEGACY 분기 제거 등). → **(B) 부분 채택** — string은 NET_LEGACY 측이 *깨진 채* 잔존 + 본 프로젝트는 NET_LEGACY define 안 쓰니 dead code → 통째 제거. bool은 별도 분기 신설. List 등 다른 NET_LEGACY 분기는 안 건드림 (scope creep 차단)
- **테스트 결과** —
  - `dotnet test Dawnholder.slnx --nologo`: **135 통과 / 0 실패 / 1 skip / 46s** (`Hundred_runs_all_succeed` LongRunning skip 유지)
  - Phase 01 baseline 132 → **+3 HandshakeHandlerTests** (happy/mismatch/non-handshake 첫 패킷). 회귀 0
  - `dotnet build Dawnholder.slnx`: 경고 0 / 오류 0 (모든 영향 영역 컴파일 정합)
- **다음 스텝** — M3 Phase 03 — 핸들러 layer 분리 + `02_Server/CLAUDE.md` 정합. 현재 `GameSession.OnRecvPacket`에 switch dispatch가 직접 박혀있어 `02_Server/CLAUDE.md` Layout 표의 `Handlers/` 디렉토리 약속과 불일치 (또 하나의 *가짜 약속*). Phase 03이 그 봉합

## AC 검증 결과

```bash
# 1. PDL 변경 의무 3종 — regen + build + commit (commit은 본 박제 후)
$ dotnet run --project 99_Tools/PacketGenerator -- PDL.xml --no-wait
   [GEN] GenPackets.cs → 98_Shared/Protocol/Generated/
   [GEN] --no-manager: PacketManager 출력 skip (Phase 08+에서 manager 도입 예정)
   [GEN] Packet Generate Success.
$ dotnet build c:/Dev/ClaudeDev/Dawnholder.slnx -nologo -v minimal | tail -5
   빌드했습니다.
       경고 0개
       오류 0개

# 2. 핸들러 단위 테스트 3건 (Phase 02 핵심)
$ dotnet test c:/Dev/ClaudeDev/Dawnholder.slnx -nologo --no-build --filter "FullyQualifiedName~HandshakeHandlerTests"
   Happy_MatchingVersion_AcksAndEntersGame              [통과]
   Mismatch_HigherVersion_RejectsAndDisconnects         [통과]
   NonHandshakeFirstPacket_Rejected_NoEntry             [통과]

# 3. 전체 회귀 (Phase 01 baseline 132 → +3 HandshakeHandlerTests)
$ dotnet test c:/Dev/ClaudeDev/Dawnholder.slnx -nologo --no-build | tail -3
   통과!  - 실패: 0, 통과: 135, 건너뜀: 1, 전체: 136, 기간: 46s - GameServer.Tests.dll

# 4. 헌법 #2 봉합 — 옛 stale 문구 정정
$ grep -n "M3 Phase 02 봉합 완료" 98_Shared/CLAUDE.md
   19:│   └── ProtocolVersion.cs  ... — 핸드셰이크 코드 M3 Phase 02 봉합 완료 (C_Handshake/S_HandshakeResult + first-packet 강제)
```

## 결정 흐름

- **PacketGenerator bool/string fix를 Phase 02에 묶음 vs 별도 Phase**: → **묶음 채택**. 이유: 본 Phase가 fix의 직접 trigger(S_HandshakeResult가 두 타입 첫 실수요자) + 분리 시 의존성 헝클어짐(별도 Phase = PDL 추가 못 함 = Phase 02 자체가 막힘) + 같은 commit에 *Protocol 봉합 + 도구 결함 fix*가 헌법 #2 정신 같은 가치
- **first-packet 강제 위치**: GameSession 도메인 vs base PacketSession. → **GameSession 도메인** 채택. 이유: base PacketSession은 *모든 세션 공통* 영역인데 handshake는 *게임 도메인* 책임. 다른 미래 도메인(예: chat server)이 같은 base 쓰면서 handshake 안 박을 수도 있음 → 분리 유지
- **lifecycle/rate-limit 테스트 적응 방식**: (A) handshake 패킷을 ctor에서 직접 OnRecvPacket 주입 vs (B) protected helper로 우회. → **(B)** 채택. 이유: (A)는 test가 *내부 wire format에 직접 의존*해서 PDL 변경 시 깨짐. (B)는 *handshake 우회 의도*를 명시적으로 표현 + race/rate 검증 본질에 집중
- **응급 모드 vs 본 마감 호환표**: → **응급 모드 == 비교 채택**. 호환 가능 minor version 호환표는 본 마감 시 별도 Phase. 5/20 데모 환경은 양쪽 동일 빌드라 false positive 영향 X

## Codex β 직접 검증 (γ 방식 4회차) 후속 봉합

**검증 산출물**: [`00_Document/reviews/2026-05-18-m3-phase-02-codex-review.md`](../../../00_Document/reviews/2026-05-18-m3-phase-02-codex-review.md) (Codex 본 세션에서 코드 직접 + dotnet test 재실측, **7건 발견**)

**옵션 B 채택 (균형)** — 응급 모드 + 5/20 데모 영향 작은 HIGH/LOW 4건은 *본 마감 전 별도 Phase*, *데모 안정성 + Phase 03 진입 부드러움 + 회귀 안전망* 4건 즉시 봉합:

### 즉시 봉합 4건

1. **Codex 인사이트 — `CompleteHandshakeAndEnter()` 캡슐화** ([`GameSession.cs`](../../../02_Server/GameServer/Network/GameSession.cs))
   - 옛: HandleHandshake가 `_handshakeCompleted=true; Send(ok); EnterGameWorld()` 직접 박음
   - 새: 위 3행 묶음을 protected 메서드 캡슐화 → Phase 03 핸들러 layer 분리 시 외부 핸들러 클래스가 *세션 내부 state 직접 침범 X*. handler = decode + 검증 + (mismatch면 Send fail+Disconnect / OK면 session.CompleteHandshakeAndEnter() 호출).
   - lifecycle/rate-limit `TestGameSession.OnConnected`도 본 메서드 직접 호출 = handshake mock 의도 더 명시적. 옛 `MarkHandshakeCompletedForTest` 제거

2. **Codex #5 — duplicate handshake protocol violation** ([`GameSession.cs:OnRecvPacket`](../../../02_Server/GameServer/Network/GameSession.cs), [`HandshakeHandlerTests.cs`](../../../02_Server/GameServer.Tests/Network/HandshakeHandlerTests.cs))
   - `_handshakeCompleted=true` 후 C_Handshake 재수신 = silent unknown drop이 아닌 *명시적 protocol violation* (헌법 #2 정합)
   - switch에 `case PacketID.C_Handshake:` 추가 → [Trust] 로그 + Disconnect
   - `DuplicateHandshake_AfterCompleted_RejectsAsProtocolViolation` 4번째 보너스 테스트

3. **Codex #2 — Unity SendIntent handshake gate** ([`UnityClientSession.cs`](../../../03_Client/Assets/Scripts/Network/UnityClientSession.cs))
   - `HandshakeOk` 프로퍼티 신설 (main thread `HandleHandshakeResult` 안에서 박힘 → 같은 thread `SendIntent` visibility 보장)
   - `SendIntent` 진입부에서 `if (!HandshakeOk) return;` (drop)
   - race window (socket worker가 C_Handshake 자동 Send 박은 직후, main thread Update의 LocalPlayerController.SendIntent가 먼저 호출되는 짧은 창) 차단

4. **Codex #4 — PacketRoundTripTests 직접 회귀 안전망** ([`PacketRoundTripTests.cs`](../../../02_Server/GameServer.Tests/PacketRoundTripTests.cs))
   - `C_Handshake_RoundTrip_PreservesClientVersion` + `C_Handshake_RoundTrip_HandlesUshortEdgeValues` (5건) + `C_Handshake_Write_ProducesCorrectSizeAndPacketId`
   - `S_HandshakeResult_RoundTrip_PreservesAllFields_OkTrue` + `_OkFalse`
   - `S_HandshakeResult_RoundTrip_HandlesReasonStringEdgeValues` (5건: empty / ASCII / 긴 ASCII / CJK Unicode / emoji surrogate pair)
   - `S_HandshakeResult_Write_ProducesCorrectPacketId` (PacketID 8)
   - **총 11건** — PacketGenerator bool/string fix(M3 Phase 02 핵심 도구 봉합) 회귀 안전망 + UTF-16 LE wire format 정합 검증

### 잔존 알려진 결함 (응급 데모 영향 작아 본 마감 전 별도 Phase 봉합)

| Codex # | 위험 | 발견 | 봉합 시기 |
|--------|------|------|----------|
| #1 | **HIGH** | mismatch `Send → Disconnect` race — TCP flush 보장 X (응급은 양쪽 동일 빌드라 mismatch 발생 X) | 본 마감 전 `DisconnectAfterFlush` 패턴 + mismatch 통합 테스트 |
| #3 | MEDIUM | malformed C_Handshake body 테스트 누락 (Phase 09 OnRecvCompleted catch가 잡지만 명시 X) | 별도 Phase / 새 패킷 추가 직전 |
| #6 | LOW | GenPackets.cs trailing whitespace 8건 | 생성기 템플릿 정리 시 묶음 |
| #7 | LOW | PDL unknown type silent skip 잔존 (`<unit>` 같은 오타가 성공처럼 보임) | M3 Phase 03~04 패킷 추가 시점 직전 |

### Codex 후속 봉합 검증 결과

- `dotnet test Dawnholder.slnx --nologo`: **151 통과 / 0 실패 / 1 skip / 46s**
- baseline 진화: Phase 01 132 → Phase 02 closing 135 → **+ Codex 후속 봉합 151** (= 신규 16건: roundtrip 11 + duplicate handshake 1 + 기존 HandshakeHandlerTests 3 + 기타 변경 1, 회귀 0)
- `dotnet build`: 경고 0 / 오류 0

## 학습 일지 후보 키워드

- `application-layer-handshake` — TCP byte stream 위에 *application 레벨* version 약속 박는 패턴. TCP는 byte 순서만 보장, 의미는 application 책임. 한국 게임 회사 면접 단골 → ★★★ 후보 (`/journal:concept`)
- `first-packet-isolation-pattern` — handshake가 *첫 패킷이어야* isolation 보장. 다른 패킷 받기 전 검증 → 권한 미부여 상태에서 서버 리소스 박지 않음 = trust boundary 강화. 학습 가치 ★★
- `tool-defect-blocker-coupling` — PacketGenerator bool/string 결함이 *본 Phase의 직접 blocker* → 분리 시 의존성 헝클어짐. 도구 결함과 사용 시점의 결합. *언제 fix해야 하는가*의 학습 가치 ★★
- `false-promise-second-instance` — "주석으로 박힌 약속은 가짜다" 두 번째 봉합 (rate-limit fail-closed가 첫 번째, M2.5 Phase 09). 헌법 #2가 4 Phase째 가짜였던 사실 + 응급 데모 직전에야 봉합 → *문서/주석은 코드로 박혀야 진짜*라는 패턴의 ★★★ 증명 후보
- `protected-helper-test-mock` — `CompleteHandshakeAndEnter()` protected helper로 테스트 mock 박는 패턴. internal/reflection 우회 안 하고도 *handshake 우회 의도*를 명시적으로 표현. C# 접근자의 *테스트 친화* 활용 ★
- `gamma-fourth-instance-codex-direct-verification` — γ 방식 4회차 = Rule of Three 통과 후 정착 단계 실측. Codex β가 *코드 직접 + dotnet test 재실측* 검증으로 HIGH 1건 + MEDIUM 3건 + LOW 3건 짚음. *Phase 03 진입 캡슐화 인사이트* (handler 분리 시 lifecycle state 침범 차단)는 *코드 못 보면 짚기 어려운* 깊이 → AI 페어 검증의 *코드 직접 접근 가치* 학습 ★★★ 후보 (`/journal:concept`)
- `option-b-balance-emergency-mode` — 7건 발견 중 *데모 영향 작은 HIGH 1 + LOW 3*은 응급 모드 trade-off로 박제만, *안정성 + 회귀 안전망* 4건 즉시 봉합. 응급 + 본 마감 *시간 분배 의사결정* 학습 가치 ★★
