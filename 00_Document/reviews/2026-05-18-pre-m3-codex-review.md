# Codex 리뷰 결과 — M2 ad-hoc 전체 감사 (pre-M3)

- **범위**: `02_Server/`, `98_Shared/`, `99_Tools/`, `04_ClientNet/`, `00_Document/`, 루트 `CLAUDE.md`/`CONTEXT.md`, `Dawnholder.slnx`, `global.json`
- **제외**: `03_Client/`, `.claude/`, `.github/`
- **기준**: 요청 기준은 main `aca7795`. 로컬 HEAD는 `1b96d65`였으나 `aca7795..HEAD` 차이는 제외 영역(`03_Client/`, `01_Phases/yuhyeon/`)뿐이라 본 리뷰 범위에는 영향 없음.
- **리뷰 자세**: Claude 선행 리뷰는 검증 표에만 반영. 본문은 새로 확인한 runtime/code-path 리스크만 기록.

---

## 1. Claude 발견 검증 표

> Claude 원문 heading 기준 발견은 13건입니다. 요청의 14건 수량과 맞추기 위해 Claude가 첫 위반의 참고로 적은 `04_ClientNet` 동형 length-check를 별도 1건으로 분리해 검증했습니다. 반대 없음.

| # | Claude 항목 | 판단 | 한 줄 근거 |
|---:|---|---|---|
| 1 | [위반] 서버 `PacketSession.OnRecv` 패킷 길이 상하한 검증 누락 | 동의 — 권장 조치 적절 | 단, 최소값은 `HeaderSize`(2)보다 `size + packetId` 최소 4바이트와 패킷별 payload 길이까지 보는 쪽이 안전함. |
| 2 | [위반] `02_Server/CLAUDE.md` Layout mismatch | 동의 — fix 필요 | M3 직전에는 `Handlers/Combat/Persistence = M3+ 자리잡이`로 명시하고, 실제 handler 분리는 M3 첫 Phase로 미루는 판단이 현실적. |
| 3 | [위반] 헌법 우선순위 표에 `policies/` 누락 | 동의 — 권장 조치 적절 | 운영 정책이 외부화되어 있으므로 충돌 순위가 없으면 팀원 합류 시 해석이 갈림. |
| 4 | [주의] `CONTEXT.md` 200줄 한도 초과 | 동의 — 권장 조치 적절 | M2 마감 직후가 응축 타이밍이고, M3 시작 컨텍스트 비용을 줄일 수 있음. |
| 5 | [주의] `ProtocolVersion.Current` 정의만 있고 핸드셰이크 미구현 | 동의 — 영향 적절 | 코드 검색에서 `ProtocolVersion.Current` 사용처가 없음. silent mismatch 차단 장치가 아직 코드로 실행되지 않음. |
| 6 | [주의] ServerCore dead code (`Connector`/`SendBuffer`/`JobQueue`/`PriorityQueue`) | 동의 — 유지 쪽 | `Connector`/`SendBufferHelper`는 M3+ 부하/봇/버퍼 최적화 자리로 가치가 있음. 단 `PriorityQueue`는 의도 주석 또는 격리 필요. |
| 7 | [주의] `PacketSession.OnRecv` batch 로그 스팸 | 동의 — 권장 조치 적절 | M3에서 packet batch가 늘면 콘솔 IO가 진단보다 노이즈가 됨. Serilog Trace로 내리는 게 맞음. |
| 8 | [주의/참고] `04_ClientNet.ClientSession` 동형 length-check 누락 | 동의 — 서버보다 우선순위 낮음 | trusted server 수신이라 보안 위험은 낮지만, 프로토콜 mismatch/서버 버그 시 클라 hang/crash를 만들 수 있어 같은 helper로 고치는 게 좋음. |
| 9 | [관찰] 핸들러 happy/invalid/auth 단위 테스트 누락 | 동의 — 권장 조치 적절 | 현재 테스트는 roundtrip/physics/map tick 중심이고, `GameSession` trust-boundary 동작을 직접 고정하지 않음. |
| 10 | [관찰] 패킷 `Write()`가 호출마다 65KB 할당 | 동의 — M3 2인까지 보류 가능 | naive entity x recipient broadcast면 `4 * N^2 * 65535B/s`: 2명은 약 1MB/s, 10명은 약 25MB/s, 20명은 약 100MB/s. 10명 이상 봇 테스트 전에는 고쳐야 함. |
| 11 | [관찰] `UnitTest1.cs` 빈 placeholder | 동의 — 삭제 적절 | 통과 카운트만 올리고 invariant를 보장하지 않음. |
| 12 | [관찰] PDL ↔ generated sync OK | 동의 | 현재 6개 packet은 PDL과 `GenPackets.cs`가 1:1로 맞음. |
| 13 | [관찰] ADR INDEX ↔ ADR 폴더 정합 OK | 동의 | 요청 범위에서 INDEX의 21개 ADR 파일 존재는 정합. |
| 14 | [관찰] 5축 통과 영역 | 동의 — 열거된 항목에 한정 | Server Authority/No Blocking/Y2 분리는 확인됨. 다만 rate-limit enforcement는 아래 새 위반으로 별도 기록. |

---

## 2. Codex 추가 발견

### [위반] Trust Boundary — rate-limit가 기록만 하고 모든 intent를 tick queue에 계속 넣음

- **위치**: `02_Server/GameServer/Network/GameSession.cs:130~145`, `02_Server/GameServer/Network/GameSession.cs:165~172`, `02_Server/GameServer/Maps/GameMap.cs:47~51`
- **증거**:
  ```csharp
  if (_intentCountInWindow > IntentRateLimitPerSecond && !_rateLimitLoggedThisWindow)
  {
      Console.WriteLine(...);
      _rateLimitLoggedThisWindow = true;
      // 그래도 처리 진행
  }
  ...
  map.EnqueueJob(() => { ... entity.PendingInputX = capturedInputX; ... });
  ```
  ```csharp
  while (_pendingJobs.TryDequeue(out Action? job))
      job();
  ```
- **영향**: 헌법 #3의 rate-limit가 실제 차단이 아니라 로그뿐임. 악성 클라가 `C_MoveIntent`를 초당 수천 개 보내면 패킷마다 closure `Action`이 생성되고, tick thread가 한 tick 안에서 큐를 끝까지 drain함. M3 broadcast가 붙으면 snapshot 송신 전 tick p99를 먼저 밀 수 있음.
- **권장 조치**: 임계 초과 시 해당 intent drop 또는 disconnect. 추가로 `GameMap.Tick`에서 tick당 job 처리 상한을 두거나, 이동 입력은 큐에 Action을 N개 쌓지 말고 세션별 latest input 슬롯으로 coalesce. 테스트 invariant: “rate 초과 후에는 map job 수/적용 횟수가 증가하지 않는다.”

### [위반] 연결 직후 disconnect race로 ghost player가 남을 수 있음

- **위치**: `02_Server/GameServer/Network/GameSession.cs:41~62`, `02_Server/GameServer/Network/GameSession.cs:70~78`, `02_Server/GameServer/Maps/GameMap.cs:27~36`, `02_Server/GameServer/Maps/GameMap.cs:74~88`
- **증거**:
  ```csharp
  map.EnqueueJob(() =>
  {
      PlayerEntity entity = map.AddPlayer(self, spawnPos);
      self._entityId = entity.EntityId;
      self.Send(pkt.Write());
  });
  ```
  ```csharp
  if (_entityId < 0) return;
  map.EnqueueJob(() => map.RemovePlayer(eid));
  ```
- **영향**: accept 직후 클라가 tick 전에 끊기면 `OnDisconnected`는 `_entityId == -1`이라 remove job을 넣지 않음. 이후 queued add job이 실행되면 닫힌 세션을 owner로 가진 player가 맵에 남고, snapshot tick마다 `p.Owner.Send(...)` 대상이 됨. M3에서는 다른 플레이어에게 ghost entity로 보일 수 있음.
- **권장 조치**: disconnect도 항상 map job으로 넣고 map이 `GameSession` 기준으로 cleanup하게 만들기. 또는 `_closing` 플래그를 두어 add job 실행 시 이미 끊긴 세션이면 AddPlayer/Send를 skip. 테스트 invariant: “connect job 처리 전 disconnect가 와도 `Players`가 0으로 남는다.”

### [위반] malformed short payload가 decode 예외 후 세션을 half-open 상태로 남김

- **위치**: `02_Server/GameServer/Network/GameSession.cs:84~103`, `98_Shared/Protocol/Generated/GenPackets.cs:292~309`, `02_Server/Network/Session.cs:259~262`
- **증거**:
  ```csharp
  this.input = (byte)s[count];
  count += sizeof(byte);
  this.clientTick = BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(count, s.Length - count));
  ```
  ```csharp
  catch (Exception ex)
  {
      Console.WriteLine($"OnRecvCompleted Failed : {ex}");
  }
  ```
- **영향**: 예를 들어 `[size=4][id=C_MoveIntent]`처럼 packet id까지만 있는 frame은 `PacketSession`을 통과한 뒤 `C_MoveIntent.Read`에서 예외가 남. `OnRecvCompleted`는 예외를 로그만 찍고 disconnect도, `RegisterRecv()`도 하지 않아 세션이 열린 채 더 이상 수신하지 않는 상태가 될 수 있음.
- **권장 조치**: decode 예외는 fail-closed로 `Disconnect()` 처리. 더 좋은 쪽은 generated `Read`가 `bool TryRead(...)` 또는 길이 검증을 수행하고, `PacketSession`/dispatch 단계에서 packet id별 최소 frame size를 확인하는 것. 테스트 invariant: “truncated known packet은 disconnect되고 recv loop가 멈춰서 리소스를 잡지 않는다.”

### [주의] PacketGenerator 기본 실행이 현재 프로젝트와 맞지 않는 manager 파일을 생성함

- **위치**: `99_Tools/PacketGenerator/Program.cs:20~22`, `99_Tools/PacketGenerator/Program.cs:65~75`, `99_Tools/PacketGenerator/PacketFormat.cs:11~18`, `99_Tools/PacketGenerator/PacketFormat.cs:91~94`
- **증거**:
  ```csharp
  bool noManager = false;
  ...
  if (!noManager)
      File.WriteAllText(... "ServerPacketManager.cs" ...)
  ```
  ```csharp
  using ServerCore;
  ...
  m_Handler.Add((ushort)PacketID.{0}, PacketHandler.{0}Handler);
  ```
- **영향**: 현재 repo에는 `ServerCore` namespace/`PacketHandler` 타입/Generated manager 파일이 없음. `--no-manager` 없이 generator를 실행하면 SDK default glob에 잡히는 compile-breaking 파일을 `02_Server/GameServer/Network/Generated/`와 `04_ClientNet/Generated/`에 만들 수 있음.
- **권장 조치**: manager 도입 전까지 기본값을 `noManager = true`로 바꾸거나 옵션을 `--with-manager`로 뒤집기. manager를 살릴 거면 현재 namespace와 handler 구조에 맞춘 뒤 generator smoke test에서 “기본 실행 후 solution build”를 고정.

### [주의] PDL schema/type validation이 약해 typo는 조용히 누락되고 일부 타입은 broken code를 냄

- **위치**: `99_Tools/PacketGenerator/Program.cs:148~180`, `99_Tools/PacketGenerator/Program.cs:182~188`, `99_Tools/PacketGenerator/Program.cs:201~202`, `99_Tools/PacketGenerator/Program.cs:245~250`, `99_Tools/PacketGenerator/PacketFormat.cs:361~373`
- **증거**:
  ```csharp
  string memberType = _r.Name.ToLower();
  ...
  default:
      break;
  ```
  ```csharp
  case "bool":
      return "Boolean"; // BinaryPrimitives에 ReadBooleanLittleEndian 없음
  ```
  ```csharp
  Encoding.Unicode.GetBytes(..., Segment.Array, Segment.Offset + ...)
  ```
- **영향**: PDL에 `<unit name="x"/>` 같은 오타가 들어가도 generator가 성공하고 필드는 조용히 사라짐. `bool`은 `BinaryPrimitives.ReadBooleanLittleEndian` 형태로, `string`은 존재하지 않는 `Segment` 변수로 생성되어 컴파일이 깨짐. 현재 PDL은 해당 타입을 쓰지 않아 즉시 영향은 없지만, 새 패킷 추가 때 protocol drift나 build break가 늦게 발견됨.
- **권장 조치**: unknown type은 즉시 throw. `packet/member/list` 이름은 C# identifier regex와 `C_`/`S_` prefix로 검증. 현재 지원하지 않는 타입(`bool`, `string`, `list`, `double`)은 구현 전까지 명시적으로 reject하고 fixture PDL별 generator test를 추가.

### [관찰] 현재 테스트가 잡지 못하는 핵심 invariant

- **위치**: `02_Server/GameServer.Tests/PacketRoundTripTests.cs:281~357`, `02_Server/GameServer.Tests/MoveIntentTests.cs:132~145`, `02_Server/GameServer.Tests/Integration/M2BasicMovementIntegrationTests.cs:57~131`
- **증거**: packet 테스트는 정상 frame roundtrip 중심이고, map 테스트는 `EnqueueJob_MarshalsToTickThread` happy path 중심이며, 통합 테스트도 정상 봇 이동 성공/p99 중심임.
- **미포착 invariant**:
  - malformed frame: `size < 4`, known packet의 truncated payload, oversized frame은 disconnect되어야 함.
  - lifecycle race: enter-map add job 전 disconnect가 와도 player가 남지 않아야 함.
  - rate-limit: 임계 초과 intent는 tick job/physics 적용으로 이어지면 안 됨.
  - broadcast 준비: 한 세션 disconnect가 같은 tick의 다른 세션 snapshot 송신을 막으면 안 됨.
- **권장 조치**: M3 첫 테스트 묶음은 handler 단위보다 먼저 위 invariant를 박는 편이 효과적. 특히 malformed frame/lifecycle race는 현재 코드가 실제로 실패하는 경로임.

---

## 3. 우선순위 재평가

Claude의 큰 분류에는 동의하지만, M3 broadcast 직전이라면 나는 문서 정리보다 **수신 trust-boundary를 fail-closed로 묶는 작업**을 1순위로 둔다. 구체적으로 server length min/max, packet id별 payload length, decode 예외 시 disconnect, rate-limit 초과 intent drop/coalesce를 한 Phase로 묶는 게 좋다. 2순위는 **connect/disconnect lifecycle race 제거**다. 두 명 같은 맵에 들어가는 순간 ghost player와 닫힌 세션 대상 send가 바로 사용자 눈에 보일 수 있다. `Write()` 65KB alloc은 2인 M3에서는 보류 가능하지만, naive entity x recipient broadcast 기준 10명 봇 테스트 전에 반드시 ArrayPool/SendBuffer 계열로 바꿔야 한다.

**추천 1위**: Trust-boundary fail-closed 묶음 (`Session` length/payload validation + decode exception disconnect + rate-limit drop/coalesce).

**추천 2위**: Session lifecycle cleanup (`OnConnected` queued add와 `OnDisconnected` cleanup의 ordering race 제거).

---

## 4. 메타

- **점검 소요 시간**: 약 55분.
- **모델/버전**: Codex (GPT-5 계열, API 세션).
- **수행 방식**: 정적 코드 읽기 + `rg`/`git` 기반 검색. 코드 수정 없음. 테스트/빌드는 실행하지 않음.
- **한계**: `03_Client/`, `.claude/`, `.github/`는 요청대로 제외. runtime packet fuzzing, 실제 2-client broadcast 부하 측정, Unity main-thread wrapper 검증은 수행하지 않음.
