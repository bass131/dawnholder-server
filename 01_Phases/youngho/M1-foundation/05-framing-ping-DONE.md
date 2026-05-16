# Phase 05 — framing + 첫 Ping/Pong 양방향 시연 완료 박제

**완료일**: 2026-05-10
**커밋**: `5174573` (feat(packet): framing + 첫 Ping/Pong 양방향 시연 — M1 Foundation 완료)
**소요 시간**: 약 2시간

> ★ **M1 Foundation 마일스톤 완료**. 영상 시연 가능한 첫 데모.

---

## 5단계 보고

### 🎯 무엇을 만들었나
서버↔Unity 간 첫 **양방향 패킷 왕복**. Unity가 1초마다 Ping을 던지면 서버가 즉시 Pong으로 응답하고, 클라가 RTT(왕복 시간)를 계산해 Console에 1ms 단위로 출력. TCP byte stream을 length-prefix(`[size(2)][packetId(2)][payload]`) framing으로 패킷 단위로 자르는 인프라 도입.

### 🤔 왜 필요한가
지금까지(Phase 01~04)는 *connection은 있지만 데이터 의미는 없었던 상태* — OnRecv가 raw byte 수만 로그했음. Phase 05는 **그 raw byte stream에 *의미*를 입히는 첫 단계** — 패킷 ID로 종류 식별, payload로 데이터 전달. 이게 모든 게임 통신의 토대. 그리고 Ping/Pong은 *RTT 측정*이라는 실용 가치까지 함께 제공.

### 🛠️ 어떻게 만들었나
- **Protocol 신설** (`98_Shared/Protocol/` 3파일): `PacketId` enum(범위 예약 주석) + `PingPacket`/`PongPacket` (BinaryPrimitives.*LittleEndian 명시 + ToBytes/Read 페어).
- **양쪽 PacketSession 상속 교체**: 서버 GameSession + Unity UnityClientSession 둘 다 raw `Session`/`ClientSession` → `PacketSession`으로. framing 자동화.
- **Unity Update 송신 루프**: `Time.deltaTime` 누적 + `_isConnected` 가드. 1초마다 `Ping.ToBytes()` Send.
- **고려했지만 안 고른 대안**: ① 자체 PDL을 이번에 도입 → 인프라 비용 1.5~2h 추가, Phase 단위 깨짐. Phase 06으로 분리. ② `BitConverter.GetBytes` 직접 → 호스트 endian 따라 머신마다 결과 다름. `BinaryPrimitives.*LittleEndian` 명시가 wire format의 *플랫폼 무관 약속*에 부합.
- **신규 개념**: little-endian 명시(BitConverter vs BinaryPrimitives), `Span<byte>.Slice`, PacketSession의 framing while-loop가 partial read를 자동 흡수.

### 🧪 테스트 결과
- `dotnet build Dawnholder.slnx`: **5개 프로젝트 경고 0 / 오류 0**
- Plugins/Shared/Shared.dll 14.5KB로 커짐 (Protocol 추가) — Unity 자동 인식
- Unity Console: **1초마다 `[Unity] Pong! RTT = Nms (one-way ≈ Nms)` 출력** ✅
- 서버 콘솔: **1초마다 `Ping received → Pong`** ✅
- `UnityException` / `NullReferenceException` 없음 ✅
- 1분 안정성 통과 ✅

### ➡️ 다음 스텝
- **Phase 06**: PacketGenerator(자체 PDL) 이주 + `Packets.xml` 단일 소스 + 코드 생성기 → PingPacket/PongPacket이 *생성된 코드*로 교체. 새 패킷 추가는 XML 한 줄 + 명령으로 자동화.
- **PRD.md 응축** (220 임계 초과 미해결) — Phase 06 진입 전 처리 추천.
- **M2 First Connection** 진입 가능 — 캐릭터 첫 이동(input → 패킷 → 서버 검증 → snapshot). M1과 달리 본격 *게임 로직* 단계.

---

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **직렬화 갈래 C 채택** — A(PDL 한 번에) / B(BitConverter, Phase 06 미정) / **C(BitConverter 임시 + Phase 06에서 PDL 이주)** 중 C. 사유: Phase 단위 1~3시간 헌법 권고 + raw 직렬화 한 번 짜는 학습 가치 + 교체 비용 낮음(필드 2개씩, 다른 시스템 파급 0).
- **`BinaryPrimitives.*LittleEndian` vs `BitConverter`** — `BitConverter`는 호스트 endian 따름(x86은 little, ARM 모드 따라 big 가능). 게임 wire format은 *플랫폼 무관 약속*이라 명시적 LE 사용. 코드 리뷰 시 의도 명확.
- **PacketId `None = 0` 추가** — enum 기본값이 0이고 `(PacketId)0`이 의미 있는 PacketId로 매칭되면 안 됨. None을 명시해서 unset 상태와 구분.
- **PacketSession의 OnRecvPacket이 `[size][id][payload]` 통째 전달** — id를 별도 파라미터로 빼지 않은 이유: PacketSession이 *framing*만 담당, *해석*은 GameSession/UnityClientSession이 함. 책임 분리.
- **Send 시 byte[] 카피 1회** — Shared.Protocol의 `ToBytes()`가 새 byte[]를 반환 → 송신측이 SendBuffer로 넘김. 카피 비용은 Phase 06 PDL이 SendBuffer 직통으로 해결 예정. 본 Phase는 단순함 우선.
- **server `OnSend 20 bytes` 로그 vs client `OnSend 12 bytes`** — 패킷 크기 차이가 콘솔로 즉시 보임. 검증 차원에서 의외로 유용.
- **`_isConnected` 가드 + `_session != null` 둘 다 체크** — 단순 null 체크만으로는 race 조건 가능. Connector가 비동기라 _session 할당 전에 Update가 먼저 돌 수 있음. 두 플래그 안전망.
- **Phase 06을 Phase 05 -DONE에서 *명시*** — "Phase 06에서 PDL 이주" 약속이 commit/주석/-DONE 세 곳에 박힘. 다음 세션이 잊지 못함.

---

## 막혔던 지점

이번 Phase는 큰 막힘 없음. 시연 1회 통과.

소소한 발견:
- **PongPacket.cs namespace 오타** (`Shared.Parser` → `Shared.Protocol`) — 작성 직후 즉시 발견 + 정정. 코드 리뷰 없이 즉발견 가능했던 이유: 같은 파일 안에서 `Shared.Protocol.PacketId.Pong`로 fully-qualify 박은 게 *namespace 다른 신호*였음. 자기 위반이 자기 신호.
- **`BinaryPrimitives` 사용을 양쪽에 통일** — 처음 GameSession.cs에서 비트 시프트로 직접 박았으나(`buffer[2] | (buffer[3] << 8)`), PingPacket의 패턴과 일관성 위해 즉시 BinaryPrimitives.ReadUInt16LittleEndian으로 정정. 코드 리뷰 시 *의도 명확*.

---

## 학습 일지 후보 키워드

`/journal:concept <키워드>` 로 펼칠 만한 것들:

- **endianness-and-wire-format** — host endian vs network byte order, BitConverter의 함정, BinaryPrimitives.*LittleEndian/BigEndian의 의미. TCP/IP는 big-endian(network byte order)인데 게임은 왜 little-endian을 쓰나(인텔/ARM 호환성).
- **length-prefix-framing** — TCP byte stream의 의미와 메시지 경계 모호성. length-prefix vs delimiter vs fixed-size의 trade-off. 게임에서 length-prefix가 표준인 이유.
- **partial-read-handling** — RecvBuffer 링버퍼의 역할. `ReadAsync(buffer, 0, 4)`가 4바이트 다 못 줄 수 있는 이유. PacketSession의 while-loop가 partial을 어떻게 흡수하는지.
- **packet-id-range-reservation** — 1~999=System, 1000~=Auth 식 사전 약속의 의미. 충돌 방지 + 새 카테고리 추가 시 영역 분리. 헌법 #2(Protocol is Sacred)와 직결.
- **rtt-vs-one-way-latency** — RTT/2 ≈ 한쪽 latency 추정의 한계 (비대칭 경로, jitter). 게임에서 RTT가 lag compensation 기준 단위인 이유. 30/100/200ms 임계.
- **packetsession-framing-internals** — Phase 03에서 짠 PacketSession의 OnRecv while-loop 알고리즘. dataSize 헤더 파싱 → 본문 검증 → OnRecvPacket 호출 → 커서 전진. RecvBuffer의 read/write 분리 커서가 어떻게 partial을 흡수하는지.

---

## 메모 (다음 세션을 위한)

- **M1 Foundation 마일스톤 완료**. M2(First Connection — 캐릭터 첫 이동)로 진입 가능. 단 그 전에:
  - **Phase 06: PacketGenerator 이주** — Phase 05에서 박은 임시 BitConverter를 PDL 자동 생성 코드로 교체. 인프라 1.5~2h.
  - **PRD.md 응축** — 229줄(220 초과). 응축 또는 누적 외부화. 30분 정도.
- 4월 PacketGenerator 코드는 외부 백업 또는 ServerDev 레포에 있을 것. Phase 06 진입 시점에 사용자에게 코드 위치 확인 + 이주 plan.
- Phase 06 후 새 패킷 추가는 `/work:new-packet <C2S|S2C> <name>` 슬래시 커맨드로 자동화 가능 (이미 `.claude/commands/work:new-packet.md`에 정의됨).
- 시연 영상 촬영 권장 시점 = M1 완료 직후(지금) 또는 M2 끝나고 캐릭터 이동까지 잡히면 더 임팩트. 캡스톤 1 발표(6월) 자료로 활용.
- 노션 세션 로그 박을 가치 있음 — `/session:log` 권유.
