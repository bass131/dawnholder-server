# Phase 04: 길이 프리픽스 프레이밍 + 첫 ping/pong

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 2~3시간
> **담당 에이전트**: `netcode`

---

## 🎯 목표

TCP 스트림을 "패킷" 단위로 자르는 framing을 구현하고, 첫 패킷 왕복
(C2S_Ping → S2C_Pong)을 동작시킨다. 검증을 위해 `tools/cli-client/`에
간단한 .NET 콘솔 클라이언트도 같이 만든다.

**이 Phase가 끝나면 M1 완료**. 첫 마일스톤의 마무리.

**왜 length-prefixed framing인가**: TCP는 "스트림"이지 "메시지"가 아님.
"안녕"과 "하세요" 두 번 보내도 받는 쪽엔 "안녕하세요"로 이어붙어 옴.
어디서 끊어 읽을지 약속이 필요 → 가장 단순하고 일반적인 게
**[길이 4바이트][본문 N바이트]** 형식.

---

## ⏪ 사전 조건

- [x] Phase 03 완료 (TCP 리스너 + Session)
- [x] MessagePack 패키지 추가 결정 (이미 ADR-002에서 채택)

---

## 📝 작업 내용

### MessagePack 패키지 추가
- [ ] `dotnet add shared package MessagePack`
- [ ] `dotnet add server/GameServer package MessagePack`
- [ ] (CLI 클라용도 추후 추가)

### Protocol 기반 정의
- [ ] `shared/Protocol/PacketId.cs`:
      ```csharp
      namespace Dawnholder.Shared.Protocol;

      public enum PacketId : ushort
      {
          // 1~999: System
          C2S_Ping = 1,
          S2C_Pong = 2,
          // 향후 추가:
          // 1000~1999: Auth
          // 2000~2999: Movement
          // ...
      }
      ```
- [ ] `shared/Protocol/Packets/C2S_Ping.cs`:
      ```csharp
      using MessagePack;

      namespace Dawnholder.Shared.Protocol.Packets;

      [MessagePackObject]
      public class C2S_Ping
      {
          [Key(0)] public long ClientTimestampMs { get; set; }
      }
      ```
- [ ] `shared/Protocol/Packets/S2C_Pong.cs`:
      ```csharp
      using MessagePack;

      namespace Dawnholder.Shared.Protocol.Packets;

      [MessagePackObject]
      public class S2C_Pong
      {
          [Key(0)] public long ClientTimestampMs { get; set; }  // echo
          [Key(1)] public long ServerTimestampMs { get; set; }
      }
      ```

### Framing (서버: `server/GameServer/Network/Framing.cs`)

Framing은 두 가지 책임:
1. **읽기**: 스트림에서 [4바이트 길이][본문] 한 프레임 추출
2. **쓰기**: 본문을 [4바이트 길이][본문]로 감싸서 스트림에 씀

- [ ] `static async Task<byte[]?> ReadFrameAsync(NetworkStream s, CancellationToken ct)`
  - 정확히 4바이트 읽음 (못 읽으면 null = 연결 종료)
  - 길이를 BinaryPrimitives.ReadInt32BigEndian으로 파싱
  - 길이가 0 이하 또는 MaxPacketSize 초과면 예외 (불량 연결)
  - 정확히 그 길이만큼 본문 읽음
  - 본문 byte[] 반환
- [ ] `static async Task WriteFrameAsync(NetworkStream s, byte[] body, CancellationToken ct)`
  - 4바이트 길이 헤더 + body 한 번에 씀

### Session.RunAsync 채우기
이전 Phase에서 비워둔 read 루프를 채움.

- [ ] 무한 루프:
  - `body = await Framing.ReadFrameAsync(stream, ct)`
  - body == null이면 break (정상 종료)
  - body 첫 2바이트 = PacketId, 나머지 = 페이로드
  - PacketId에 따라 dispatcher 호출
- [ ] try/catch로 모든 예외를 잡고 로깅 후 disconnect
- [ ] 마지막 활동 시각 갱신

### Dispatcher (`server/GameServer/Handlers/PacketDispatcher.cs`)
- [ ] `Dictionary<PacketId, Func<Session, byte[], Task>>` 형태의 핸들러 맵
- [ ] `RegisterHandlers()` 메서드에서 모든 핸들러 등록
- [ ] `DispatchAsync(Session, PacketId, byte[] payload)` — 적절한 핸들러 호출

### Ping 핸들러 (`server/GameServer/Handlers/PingHandler.cs`)
- [ ] `payload`를 `MessagePackSerializer.Deserialize<C2S_Ping>(payload)`
- [ ] `S2C_Pong` 만들어서 `ClientTimestampMs` 그대로, `ServerTimestampMs`에 현재 시각
- [ ] 직렬화 + framing + 전송 (`session.SendAsync(...)`)
- [ ] 핸들러는 ASYNC METHOD. await 사용 시 스레드 양보 정상.

### Session.SendAsync (편의 메서드)
- [ ] `Task SendAsync<T>(PacketId id, T packet, CancellationToken ct)`
- [ ] PacketId(2바이트) + MessagePack 직렬화된 packet 합쳐서
- [ ] WriteFrameAsync로 전송
- [ ] 동시 쓰기 보호: `SemaphoreSlim` 1개 사용 (한 번에 한 패킷만 씀)

### CLI 클라이언트 (`tools/cli-client/`)
- [ ] `dotnet new console -n CliClient -o tools/cli-client`
- [ ] `dotnet sln add tools/cli-client/CliClient.csproj`
- [ ] `dotnet add tools/cli-client reference shared/Shared.csproj`
- [ ] `dotnet add tools/cli-client package MessagePack`
- [ ] `Program.cs`:
  - TcpClient로 localhost:7777 접속
  - C2S_Ping 패킷 만들어서 (ClientTimestampMs = 현재) 전송
  - S2C_Pong 응답 받아서 RTT(왕복 시간) 계산해 출력
  - 1초마다 반복, Ctrl+C로 종료

### 단위 테스트
- [ ] `FramingTests.RoundTrip` — 메모리 스트림에 write 후 read해서 같은 바이트 나오는지
- [ ] `FramingTests.RejectsOversizedFrame` — MaxPacketSize 초과 시 예외
- [ ] `FramingTests.HandlesPartialRead` — TCP는 partial read 가능. 시뮬레이션해도 정확히 동작하는지

---

## ✅ 완료 조건

- [ ] `dotnet build` + `dotnet test` 통과
- [ ] **End-to-end 시나리오**:
  1. `dotnet run --project server/GameServer` (서버 켜기)
  2. 다른 터미널: `dotnet run --project tools/cli-client`
  3. 클라이언트가 1초마다 ping 보내고 RTT 출력
  4. 서버 로그에 ping 받음/pong 보냄 흔적
  5. 동시 5개 클라이언트 연결, 각각 정상 RTT
  6. 클라 또는 서버 Ctrl+C 시 깔끔한 종료
- [ ] 1분간 ping/pong 반복해도 메모리 안정 (계속 증가 안 함)

---

## 🧪 테스트

**자동 테스트:**
- `FramingTests.RoundTrip`
- `FramingTests.RejectsOversizedFrame`
- `FramingTests.HandlesPartialRead`
- `FramingTests.Read_Returns_Null_On_Disconnect`
- `PacketDispatcherTests.DispatchesToRegisteredHandler`
- `PacketDispatcherTests.UnknownPacketId_Logs_And_Continues`

**수동 테스트:**
- 위 End-to-end 시나리오
- 추가: 잘못된 데이터 보내기 (예: 길이 -1) → 서버가 깨끗하게 disconnect

---

## 📚 학습 포인트

### 1. TCP는 스트림이지 메시지가 아니다
- "Hello"와 "World" 두 번 send해도 받는 쪽엔 "HelloWorld"로 이어 옴.
- Framing은 메시지 경계를 약속하는 방식 (length-prefix, delimiter 등).

### 2. Length-Prefixed Framing의 장단점
- **장점**: 단순, 빠름, 바이너리 안전. 게임에서 표준.
- **단점**: 이미 buffer에 들어온 데이터를 못 미리 봄 (peek 불가).
- 대안 (지금 안 쓸 것): newline-delimited (텍스트), HTTP-style headers,
  fixed-size frames.

### 3. Big-endian vs Little-endian
- 네트워크 표준은 Big-endian (network byte order).
- 인텔/AMD CPU는 Little-endian. 항상 변환 필요.
- `BinaryPrimitives.ReadInt32BigEndian` 사용 (직접 비트 시프트보다 안전).

### 4. Partial Read 처리
- `stream.ReadAsync(buffer, 0, 4)`가 4바이트 다 안 줄 수 있음 (예: 2바이트만).
- 루프로 정확한 양 채울 때까지 반복해야 함. **MMO 코드 가장 흔한 버그**.
- 우리 ReadFrameAsync는 이걸 내부에서 처리해야 함.

### 5. MessagePack의 [Key(N)] 명시 모드
- 자동 매핑(Contractless)은 클래스 변경에 취약.
- [Key(0)], [Key(1)] 명시는 호환성 안전. 필드 추가는 OK, 재정렬은 금지.

### 6. SemaphoreSlim과 동시 쓰기 방지
- 두 스레드가 같은 NetworkStream에 동시에 쓰면 패킷이 섞일 수 있음.
- SemaphoreSlim(1, 1) = 한 번에 한 명만 통과하는 mutex.
- 읽기는 한 곳(Session.RunAsync)에서만 하니 보호 불필요.

### 7. Fire-and-forget의 위험
- `_ = SendPongAsync(...)` 같은 패턴은 예외를 삼킬 수 있음.
- 우리 패턴: 핸들러 안에서 await로 처리. 호출 체인 상부에서 catch.

---

## ⚠️ 함정 / 주의사항

- **Partial read 안 처리**: 위에서 언급. 가장 흔한 신입 버그.
- **PacketId enum 캐스팅 시**: 클라가 보낸 정수가 enum에 없는 값일 수 있음.
  반드시 `Enum.IsDefined` 체크.
- **MessagePack 직렬화 예외**: 잘못된 바이트는 예외 던짐. 핸들러 try/catch.
- **CLI 클라이언트 NetworkStream 닫기**: `using` 쓰거나 finally에서 정리.
- **로그 폭주**: ping/pong은 초당 수 회. Info 레벨로 찍으면 로그 폭발.
  Debug 레벨 또는 sampling.
- **동시 SendAsync**: 같은 세션에 여러 핸들러가 동시에 쓸 수 있음.
  SemaphoreSlim으로 직렬화.

---

## ➡️ 다음 마일스톤

**M2 - First Connection**: Unity 클라이언트 등장!
- Unity 프로젝트 생성, shared/Shared.dll 참조
- Unity에서 같은 ping/pong 동작 확인 (CLI 클라 로직을 Unity로 포팅)
- 그 후 캐릭터 첫 이동 (input → 패킷 → 서버 검증 → snapshot)

여기까지 완료되면 우리 백엔드의 **첫 진짜 데모**가 가능해요. CLI든
Unity든 클라가 서버에 연결돼서 패킷을 주고받는 모습을 영상으로 찍을
수 있어요.

---

## 작업 로그
