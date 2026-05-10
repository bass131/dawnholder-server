# Phase 05: Length-prefixed framing + 첫 Ping/Pong

> **상태**: pending
> **마일스톤**: M1 - Foundation (이 Phase 끝나면 **M1 완료**)
> **예상 소요**: 2~3시간
> **담당 에이전트**: 메인 세션 + `netcode` 서브에이전트 + 사용자 (Unity 시연)
> **근거 ADR**: ADR-002 (자체 PDL — 단, 본 Phase는 *임시 BitConverter*. PDL 이주는 Phase 06)

---

## 🎯 목표

서버↔Unity 간 **첫 양방향 패킷 왕복**(Ping → Pong). Unity가 1초마다 Ping을 던지고 서버가 즉시 Pong으로 응답, 클라가 RTT(Round-trip Time) 계산해 Console에 출력.

이번 Phase 끝나면 **M1 Foundation 마일스톤 완료** — 살아있는 양방향 통신 + 영상 시연 가능.

**왜 BitConverter 직접인가**: 직렬화 인프라(자체 PDL)는 ADR-002 채택안. 그러나 4월 외부 코드라 이주 비용 1.5~2h 추가. Phase 단위 1~3h 권고 준수 + raw 직렬화 한 번 짜보는 학습 가치 → **본 Phase는 BitConverter 임시. Phase 06에서 PDL 이주 + Ping/Pong을 PDL로 교체**.

---

## ⏪ 사전 조건

- [x] Phase 04 완료 (서버 Listener wire-up + Unity main thread queue + 양쪽 connect 시연)
- [x] ADR-002 통독 (자체 PDL 채택, 본 Phase는 임시 우회)
- [ ] 헌법 + Phase 04 -DONE.md 통독
- [ ] **이번 Phase의 핵심 통찰 인지**: TCP byte stream의 패킷 경계 모호성. 한 번 OnRecv에 Ping이 0.5/1/1.5/N개 섞여올 수 있음. → Length-prefix(`[size(2)][packetId(2)][payload]`) framing이 표준 해결책.

---

## 📝 작업 내용

### 1단계: PacketId enum (98_Shared/Protocol/)

- [ ] 새 폴더: `98_Shared/Protocol/`
- [ ] 새 파일: `98_Shared/Protocol/PacketId.cs`
  ```csharp
  namespace Shared.Protocol;

  /// <summary>
  /// 모든 패킷의 stable한 숫자 ID.
  ///
  /// **헌법 #2 (Protocol is Sacred)**:
  /// - 은퇴한 ID는 절대 재사용 금지.
  /// - 범위 예약: 1~999=System, 1000~1999=Auth, 2000~2999=Movement, ...
  /// - 추후 Protocol.Version bump 규칙 추가 예정.
  ///
  /// Phase 05는 시스템 패킷(1~999) 첫 두 개만.
  /// </summary>
  public enum PacketId : ushort
  {
      None = 0,

      // 1~999: System
      Ping = 1,
      Pong = 2,

      // 향후:
      // 1000~1999: Auth
      // 2000~2999: Movement
      // ...
  }
  ```

### 2단계: PingPacket / PongPacket 정의 (Read/Write 직접)

Rookiss 패턴 + Phase 06 PDL 생성기가 *자동 생성할 모양*과 정합. 각 패킷이 자기 직렬화 책임.

- [ ] 새 파일: `98_Shared/Protocol/PingPacket.cs`
  ```csharp
  using System;
  using System.Buffers.Binary;

  namespace Shared.Protocol;

  /// <summary>
  /// 클라 → 서버. 클라 timestamp만 실어 보냄 (RTT 측정용).
  ///
  /// 와이어 포맷: [size(2)][packetId(2)][clientTimestampMs(8)] = 12 bytes
  /// </summary>
  public class PingPacket
  {
      public const ushort Id = (ushort)PacketId.Ping;
      public const int PacketSize = 2 + 2 + 8; // 12 bytes

      public long ClientTimestampMs;

      /// <summary>버퍼에서 읽어 PingPacket 채움. buffer는 [size][id] 포함 전체.</summary>
      public void Read(ArraySegment<byte> buffer)
      {
          ReadOnlySpan<byte> span = buffer.AsSpan();
          // span[0..2] = size, span[2..4] = id (이미 dispatch에서 검증)
          ClientTimestampMs = BinaryPrimitives.ReadInt64LittleEndian(span.Slice(4, 8));
      }

      /// <summary>SendBufferHelper에서 받은 segment에 [size][id][payload]를 쓴다.</summary>
      public ArraySegment<byte> Write()
      {
          ArraySegment<byte> open = ServerCore.Net.SendBufferHelper.Open(PacketSize);
          // ... 또는 직접 byte[] 할당. ClientNet/ServerCore의 SendBufferHelper 통일 필요 (1단계 결정).
          // (실제 구현은 코드 작업 시 양쪽 SendBufferHelper 시그니처 보고 결정)
          throw new NotImplementedException();
      }
  }
  ```

  ⚠️ **결정 보류**: `Write`의 SendBuffer 사용은 *서버측 SendBufferHelper(ServerCore)* 와 *클라측 SendBufferHelper(ClientNet)* 가 똑같이 동작하지만 namespace가 다름. 양쪽이 같은 `Shared.Protocol`을 참조하므로 *Shared가 양쪽 SendBufferHelper 중 하나*에 의존하면 안 됨 (순환/비대칭). 해결안 후보:
  - **(a) Shared에 Write의 *byte[]만* 반환하는 helper 두기** — `ToBytes()` 형태. 송신측이 Send에 그 byte[]를 넘김. SendBuffer는 양쪽이 자기 라이브러리 안에서 사용.
  - **(b) 각 패킷 클래스를 양쪽에 두 벌** — Y2 갈래 패턴 그대로. 단점은 Shared의 의미 약화.
  - **(c) Read/Write를 정적 헬퍼로 분리** — Shared.Protocol.Serializer가 ToBytes/FromBytes만. SendBuffer 통합은 양쪽에서 각자.

  → 본 Phase 코드 작업 시 **(a) 채택 후 진행**. Send 시 byte[] 카피 1회 비용은 Phase 06 PDL에서 SendBuffer 직통으로 해결 예정.

- [ ] 새 파일: `98_Shared/Protocol/PongPacket.cs` — 동일 패턴, payload 16 bytes (clientTs 8 + serverTs 8)

### 3단계: 서버 GameSession을 PacketSession 상속으로 교체

- [ ] `02_Server/GameServer/Network/GameSession.cs`:
  - `Session` → `PacketSession` 상속
  - `OnRecv` (raw)는 더 이상 override X. PacketSession이 framing 처리.
  - `OnRecvPacket(ArraySegment<byte> buffer)` 구현:
    - 첫 4바이트(size+id) 파싱
    - PacketId가 Ping이면 PongPacket 만들어서 `Send(pong.ToBytes())`
    - 알 수 없는 ID면 로그 + drop (헌법: untrusted input은 검증)

### 4단계: Unity 클라 UnityClientSession을 PacketSession 상속으로 교체

- [ ] `03_Client/Assets/Scripts/Network/UnityClientSession.cs`:
  - `ClientSession` → `PacketSession` 상속
  - `OnRecvPacket` 구현 → PacketId가 Pong이면 PongPacket 파싱 후 RTT 계산:
    ```csharp
    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    long rtt = now - pong.ClientTimestampMs;
    MainThreadDispatcher.Enqueue(() => Debug.Log($"[Unity] Pong! RTT = {rtt}ms"));
    ```

### 5단계: Unity Ping 자동 송신

- [ ] `NetworkBootstrap.cs` 확장 또는 새 컴포넌트 `PingSender.cs`:
  - 인스펙터 노출: `pingIntervalSeconds = 1.0f`
  - `Update()` 안에서 누적 시간이 interval을 넘으면 Ping 송신:
    ```csharp
    void Update()
    {
        if (_session == null) return;
        _accumSec += Time.deltaTime;
        if (_accumSec < pingIntervalSeconds) return;
        _accumSec = 0;

        var ping = new PingPacket
        {
            ClientTimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        _session.Send(new ArraySegment<byte>(ping.ToBytes()));
    }
    ```

### 6단계: 빌드 + 시연 (사용자)

- [ ] `dotnet build Dawnholder.slnx` — 5개 프로젝트 경고 0 / 오류 0
- [ ] 서버 실행 → Unity Play
- [ ] **기대 결과**:
  - Unity Console: 1초마다 `[Unity] Pong! RTT = <N>ms`
  - 서버 콘솔: `[GameSession] Ping received` 같은 로그 (선택)
  - RTT 값이 합리적 (loopback 시 1~5ms, LAN 5~20ms)
- [ ] 1분간 안정성 — 메모리 안 늘고 RTT 안정

### 7단계: 커밋

- [ ] `feat(packet): framing + 첫 Ping/Pong 왕복 (BitConverter 임시 — Phase 06 PDL 이주 예정)`

---

## ✅ 완료 조건

- [ ] `dotnet build` 5개 프로젝트 경고 0 / 오류 0
- [ ] `98_Shared/Protocol/`에 PacketId + PingPacket + PongPacket
- [ ] 서버 GameSession이 PacketSession 상속, Ping 받으면 Pong 응답
- [ ] Unity UnityClientSession이 PacketSession 상속, Pong 받으면 RTT 계산
- [ ] Unity NetworkBootstrap (또는 PingSender)이 1초마다 Ping 자동 송신
- [ ] **End-to-end**: Unity Console에 1초마다 RTT 로그
- [ ] 1분 시연 안정 (메모리·RTT 둘 다)

---

## 🧪 테스트

**자동 테스트** (선택, 시간 남으면):
- `PingPacketTests.RoundTrip` — Write → Read 했을 때 같은 ClientTimestampMs
- `PongPacketTests.RoundTrip` — 동일

**수동 테스트** (필수):
- 위 6단계 시연
- 서버 안 켠 상태로 Unity Play → ConnectionRefused 로그, 오류 폭발 X
- Unity Stop → 양쪽 OnDisconnected 로그 (Phase 04 패턴 그대로)

---

## 📚 학습 포인트

### 1. TCP byte stream의 패킷 경계 모호성
- "Hello"와 "World" 두 번 send해도 받는 쪽엔 "HelloWorld"로 이어 옴.
- Length-prefix는 *각 메시지 앞에 길이를 박아* 경계를 약속하는 가장 단순한 방법.
- 대안: delimiter (텍스트 프로토콜), HTTP-style header, fixed-size frame.

### 2. BinaryPrimitives.ReadInt64LittleEndian
- `BitConverter`는 *호스트 endian* 따름 → x86/x64는 little-endian이지만 *machine마다 다를 수 있음* (ARM big-endian 모드).
- `BinaryPrimitives.ReadXxxLittleEndian`/`BigEndian`은 *명시적*. 게임 wire format은 *플랫폼 무관 약속* 필요.
- 본 Phase는 little-endian 채택 (.NET 환경 다수 — 단, *명시*가 핵심).

### 3. PacketSession의 framing 알고리즘
- `OnRecv` 안 while 루프: 헤더(2byte) 모이면 size 파싱 → size만큼 모이면 한 패킷으로 잘라 `OnRecvPacket` 호출 → 반복.
- *partial read* 자동 흡수 (RecvBuffer가 다음 OnRecv까지 보존).
- 이 패턴이 Phase 03에서 짠 `PacketSession` 본문. Phase 05엔 *상속만* 함.

### 4. Ping/Pong과 RTT 측정 의미
- RTT = 클라 → 서버 → 클라 왕복 시간.
- 게임에서 RTT는 *지연 보상(lag compensation)*의 기본 단위. RTT/2 ≈ 한쪽 latency.
- 30ms 이하 = LAN/같은 머신, 30~100ms = 같은 국가, 100~200ms = 대륙간.
- MMORPG는 200ms 이상이면 전투가 어색해짐 → 서버 위치 중요.

### 5. Phase 06으로 미루는 PDL (자기관찰)
- 임시 BitConverter는 *교체 비용 낮음* (Ping/Pong 필드 2개씩, ~10줄).
- PDL 이주 시 PingPacket/PongPacket이 *생성된 코드*로 교체됨. PacketId enum은 그대로.
- "임시 코드"가 진짜 임시인 신호: *교체 시 다른 시스템에 파급 0*. Ping/Pong이 그 조건 충족.

---

## ⚠️ 함정 / 주의사항

- **headers 모자라서 deadlock**: PacketSession의 OnRecv는 size 헤더 2byte가 모일 때까지 기다림. 단 1byte도 흐르지 않으면 영원히 대기. *현실에선* 클라가 1byte씩 보낼 일 거의 없지만, 부하 테스트용 fuzz 시 발견 가능. Phase 05엔 신경 X.
- **Endian 미스매치**: 양쪽이 *같은* endian으로 read/write해야. BinaryPrimitives.ReadInt64**LittleEndian** 명시.
- **PongPacket Send 시 SendBuffer 동시성**: PacketSession의 Send는 내부 `m_lock` 보호. 같은 세션에 두 곳에서 동시 Send해도 안전.
- **Unity Update의 1초 정확도**: `Time.deltaTime` 누적은 *대략* 1초. 정확한 1초 필요하면 `InvokeRepeating` — 본 Phase엔 정확도 무관.
- **클라 ClientTimestampMs 시계 점프**: 본인 머신 시각 변경 시 RTT 음수 가능. 본 Phase는 1대 시연이라 무시.
- **Plugins 캐시**: ClientNet.dll 변경됐는데 Unity가 옛 .dll 잡을 때 → Refresh(Ctrl+R) 또는 에디터 재시작.

---

## ➡️ 다음 Phase

**M1 Foundation 완료 직후 → Phase 06: PacketGenerator(자체 PDL) 이주**
- 4월에 작성한 외부 PDL 시스템을 `99_Tools/PacketGenerator/`로 이주
- `98_Shared/Protocol/Packets.xml` (PDL 단일 소스) 정의
- 코드 생성기 실행 → PingPacket/PongPacket이 *생성된 코드*로 교체
- 새 패킷 추가 시 XML 한 줄 + 생성 명령 → 양쪽에 자동 반영

> Phase 06 후 새 패킷 추가는 `/new-packet <C2S|S2C> <name>` 슬래시 커맨드로 자동화 가능.

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모 누적.
> 끝나면 `05-framing-ping-DONE.md`로 박제.
