# Phase 07: PDL 정합 + Phase 05 코드 교체 + Unity 재시연 (책임 단위 분리/통합)

> **상태**: pending
> **마일스톤**: M1 - Foundation (정비 마지막)
> **예상 소요**: 2.5~3시간
> **담당 에이전트**: 메인 세션 + `netcode` 서브에이전트 + 사용자 (Unity 시연)
> **근거 ADR**: ADR-002 v2 (자체 PDL) + ADR-012 (Y2 분리, 본 Phase에서 *책임 단위*로 정제)

---

## 🎯 목표

Phase 05의 임시 BitConverter `PingPacket`/`PongPacket`을 **생성 코드(`C_Ping`/`S_Pong`)**로 교체. PDL 인프라가 양쪽에 wire-up되어 *살아있는 Ping/Pong 시연*이 Phase 05와 동일하게 재현됨을 입증. **책임 단위 분리/통합 결정을 코드/문서에 박제**.

**책임 단위 정합** (③ 채택):

| 책임 | 분리 vs 통합 | 위치 |
|---|---|---|
| socket 라이프사이클 (Connector/Listener/Session) | 분리 | `02_Server/Network/` + `04_ClientNet/` |
| 버퍼 관리 (SendBuffer/RecvBuffer) | 분리 | 위와 동일 |
| **패킷 데이터 클래스** (C_Ping/S_Pong) | **통합** | `98_Shared/Protocol/Generated/` |
| PacketManager (dispatch table) | 분리 | 양쪽 (단, Phase 07엔 미사용) |
| 핸들러 함수 (HandlePing 등) | 분리 | GameSession / UnityClientSession |

**본 Phase 한계**: PacketManager + PacketHandler 자동 dispatch는 **Phase 08로 분리**. 본 Phase는 GameSession/UnityClientSession이 *직접 switch*로 dispatch (Phase 05 패턴 유지).

---

## ⏪ 사전 조건

- [x] Phase 06 완료 (PacketGenerator 이주 + 잠복 버그 정정)
- [x] ③ 책임 단위 분리/통합 갈래 결정 (사용자 통찰: "패킷 정의는 환경 무관, 환경 차이는 핸들러에서")
- [ ] 헌법 + Phase 06 -DONE + ADR-012 통독
- [ ] **이번 Phase 핵심 통찰 인지**: *분리/통합은 책임마다 따로*. *전부 분리*도 *전부 통합*도 단순화. 환경 의존성 유무가 기준.

---

## 📝 작업 내용

### 1단계: PacketFormat.cs 수정 — Write() byte[] 반환 + BinaryPrimitives

**Write() 템플릿 변경**:
- Before: `SendBufferHelper.Open/Close` 의존 → 양쪽 SendBufferHelper 통합 필요했음
- After: `byte[] buffer = new byte[size]; ... return buffer;` (Phase 05 ToBytes() 패턴과 정합)
- 패킷 클래스의 *SendBuffer 의존 제거* → 패킷이 *순수 데이터*가 됨 → Shared에 통합 가능

**BinaryPrimitives 정합**:
- `BitConverter.ToInt64(...)` → `BinaryPrimitives.ReadInt64LittleEndian(...)` 명시
- `BitConverter.TryWriteBytes(...)` → `BinaryPrimitives.WriteInt64LittleEndian(...)` 명시
- 이유: BitConverter는 *호스트 endian* 따름. 게임 wire format은 *플랫폼 무관 약속*이라야.

**namespace 자리 표시자**: `using {0};` 추가 → Program.cs가 출력 시 namespace 인자 결정.

### 2단계: Program.cs 수정 — 3 출력 폴더 분리

각 파일을 *책임에 맞는 위치*로:

| 파일 | 출력 위치 | namespace |
|---|---|---|
| `GenPackets.cs` | `../../98_Shared/Protocol/Generated/` | `Shared.Protocol` |
| `ServerPacketManager.cs` | `../../02_Server/GameServer/Network/Generated/` | (Phase 08에서 결정) |
| `ClientPacketManager.cs` | `../../04_ClientNet/Generated/` | (Phase 08에서 결정) |

**옵션**: `--no-manager` 인자 추가 → Phase 07 시점엔 manager 출력 끄고 GenPackets만. PacketHandler stub 작성 부담 회피.

**또는** manager 출력 그대로 두되 PacketHandler를 *최소 stub*으로 양쪽에 작성 (Phase 08까지 미사용).

→ **결정 (코드 작업 시)**: `--no-manager` 옵션 추가가 깔끔. Phase 08에서 manager 도입 시 옵션 제거.

### 3단계: 생성기 재실행 + 출력 검증

```powershell
cd 99_Tools/PacketGenerator
dotnet run -- PDL.xml --no-manager
```

검증:
- [ ] `98_Shared/Protocol/Generated/GenPackets.cs` 정상 생성
- [ ] eyeball: Write()가 byte[] 반환 + BinaryPrimitives 사용
- [ ] eyeball: namespace `Shared.Protocol` 정상
- [ ] 빌드 격리 컴파일: SendBufferHelper 의존 0, .NET Std 2.1만으로 컴파일

### 4단계: Phase 05 임시 코드 삭제

생성 코드가 대체하므로 수동 작성된 임시 파일 삭제:
- [ ] `98_Shared/Protocol/PingPacket.cs` 삭제
- [ ] `98_Shared/Protocol/PongPacket.cs` 삭제
- [ ] `98_Shared/Protocol/PacketId.cs` 삭제 (생성된 PacketID enum이 대체)

### 5단계: 양쪽 dispatch 정합

**서버 GameSession 변경**:
- `using Shared.Protocol;` 그대로 OK (생성 코드도 같은 namespace)
- `PacketId` → `PacketID` (대문자 — 생성기 enum 이름 규칙)
- `case PacketId.Ping:` → `case PacketID.C_Ping:`
- `PingPacket ping = new PingPacket(); ping.Read(buffer);` → `C_Ping ping = new C_Ping(); ping.Read(buffer);`
- `pong.ToBytes()` → `pong.Write()` (생성 코드 메서드명)
- 멤버명 `ClientTimestampMs` → `clientTimestampMs` (PDL.xml의 camelCase)

**Unity UnityClientSession 변경**: 같은 패턴.
- `case PacketId.Pong:` → `case PacketID.S_Pong:`
- `PongPacket` → `S_Pong`
- `pong.ToBytes()` → `pong.Write()`
- 멤버명 case 정합

**NetworkBootstrap 변경**:
- `PingPacket ping = new PingPacket { ClientTimestampMs = ... };` → `C_Ping ping = new C_Ping { clientTimestampMs = ... };`
- `ping.ToBytes()` → `ping.Write()`

### 6단계: 빌드 검증

- [ ] `dotnet build Dawnholder.slnx` — 6 프로젝트 경고 0 / 오류 0
- [ ] `Plugins/Shared/Shared.dll`이 새 GenPackets 포함 (크기 변화 확인)
- [ ] `Plugins/ClientNet/Dawnholder.Client.Net.dll` 그대로

### 7단계: Unity 시연 (사용자 손)

- [ ] 서버 `dotnet run` + Unity Play
- [ ] 1초마다 `[Unity] Pong! RTT = Nms` 로그 (Phase 05와 *동일*)
- [ ] 서버 콘솔 `Ping received → Pong` 로그
- [ ] **검증의 의미**: *생성 코드*로 동일 시연이 재현됨 → PDL 인프라가 양쪽 wire-up 정합됨을 입증.

### 8단계: 책임 단위 분리/통합 문서화 (6군데)

미래의 면접관/팀원/본인이 *왜 이렇게 분리/통합했는지* 즉시 이해 가능하도록:

1. **ADR-012 보강** — "본 결정은 socket 라이프사이클뿐 아니라 버퍼 관리(SendBuffer/RecvBuffer)도 분리. 패킷 정의는 *환경 무관*이라 통합. 책임 단위 분리/통합 표는 Phase 07 -DONE.md 참조."
2. **`98_Shared/CLAUDE.md`** — "Shared = *양쪽 동기 필수인 cross-cutting*만. Protocol(패킷) + GameData. socket 인프라는 양쪽 분리."
3. **`02_Server/Network/SendBuffer.cs` + `04_ClientNet/SendBuffer.cs` 상단 주석** — "이 코드는 반대편에 거의 동일한 코드 있음. 합치지 않은 이유: Y2 갈래(ADR-012) — 환경별 GC + 변경 내성. 패킷 정의는 통합(98_Shared/Protocol/Generated/)."
4. **PacketFormat.cs 헤더 주석** — "본 생성기 출력은 *책임 단위로 분리*. GenPackets(패킷 정의)=Shared 통합. PacketManager(dispatch)=양쪽 분리."
5. **Phase 07 -DONE.md** — 결정 흐름 박제 (책임 단위 표 + ① vs ② vs ③ 비교 + ③ 사유).
6. **학습 일지 후보 키워드** — `y2-split-by-responsibility` (또는 비슷한 키워드).

### 9단계: commit + DONE.md 박제

- `feat(packet): PDL 정합 + 책임 단위 분리/통합 + Phase 05 코드 교체`
- `-DONE.md` 박제 + CONTEXT/History 갱신.

---

## ✅ 완료 조건

- [ ] PacketFormat.cs 템플릿: Write() byte[] 반환 + BinaryPrimitives 정합 + namespace 자리 표시자
- [ ] Program.cs: 3 출력 폴더 분리 + `--no-manager` 옵션
- [ ] 생성기 재실행 → `98_Shared/Protocol/Generated/GenPackets.cs` 생성
- [ ] Phase 05 임시 코드 (PingPacket/PongPacket/PacketId) 삭제
- [ ] 서버 GameSession + Unity UnityClientSession + NetworkBootstrap 모두 *생성 코드* 사용 (직접 switch dispatch)
- [ ] 6 프로젝트 빌드 경고 0 / 오류 0
- [ ] Unity 시연 → 1초마다 RTT 로그 (Phase 05 재현)
- [ ] 문서화 6군데 박제

---

## 🧪 테스트

**자동 테스트**: 추가 안 함 (생성 코드 자체엔 단위 테스트 가치 낮음, 시연이 본질).

**수동 테스트** (Phase 05와 동일):
1. 서버 실행 → "Listening on 0.0.0.0:7777"
2. Unity Play → "Pong! RTT = Nms" 1초마다
3. 1분 안정성

**의미적 추가 검증**:
- `grep -r "PingPacket\|PongPacket\|PacketId\b" --include="*.cs"` → 잔재 0건 (Phase 05 임시 코드 완전 제거)

---

## 📚 학습 포인트

### 1. 책임 단위 분리/통합
*전부 분리* 또는 *전부 통합*이 아닌, **각 책임이 환경에 의존하는지** 기준으로 따로 결정. *시니어 사고*. 본 프로젝트의 정합:
- 환경 의존(socket/buffer/handler) → 분리
- 환경 무관(패킷 wire format) → 통합
- 양쪽 다른 데이터(dispatch table) → 분리

### 2. 단일 소스 + 자동 동기화
PDL.xml 한 곳만 바꾸면 양쪽 패킷 자동 동기. 헌법 #2(Protocol is Sacred) + #4(복사-붙여넣기 금지) 둘 다 *물리적으로 강제*. 면접 답변: "동기화 비용은 자동화로 0".

### 3. 임시 코드의 점진적 교체
Phase 05 BitConverter 임시 → Phase 07 생성 코드. 임시 코드의 신호 검증(*교체 시 다른 시스템 파급 0*) 통과. 임시 코드 패턴 학습.

### 4. Generated 폴더 컨벤션
*수동 작성 코드* vs *자동 생성 코드*를 폴더로 분리(`Generated/`). 미래 사람이 *어디를 직접 수정해야 하는지* 즉시 파악. ASP.NET / EF Core / Protobuf 모두 같은 패턴.

### 5. 의존 역전의 실천
패킷 클래스가 SendBufferHelper에 의존했던 게 *Shared에 두지 못한 진짜 이유*. byte[] 반환으로 *의존 역전* → Shared 통합 가능. SOLID의 D 실전 사례.

---

## ⚠️ 함정 / 주의사항

- **camelCase vs PascalCase**: 생성 코드 멤버명은 PDL.xml 정의 그대로(camelCase). Phase 05 코드의 PascalCase와 다름 → 호출자 정합 필요. 검색·치환 시 case-sensitive.
- **`Write()` vs `ToBytes()`**: 생성 코드 메서드명이 `Write()` (PacketFormat.cs 템플릿). Phase 05의 `ToBytes()`와 다름. 호출자 정합.
- **PacketHandler 의존**: 생성된 ServerPacketManager.cs가 `PacketHandler.X_Handler` 참조 → 컴파일 오류 가능. 본 Phase는 `--no-manager` 채택해 manager 출력 끔. Phase 08에서 manager + PacketHandler 도입.
- **Plugins 자동 복사 후 Unity Refresh**: Shared.dll 갱신되면 Ctrl+R 또는 에디터 재시작.
- **Generated 폴더 .gitignore**: 생성 .cs는 *commit 필수* — Unity가 .dll만 보므로 Shared.dll에 컴파일되어 들어가야 인식. .gitignore X.
- **`Console.ReadKey` stdin redirect**: Phase 06 함정 그대로. 옵션 인자 처리 시 `--no-wait`도 같이 추가 검토 (CI 가능하게).
- **빌드 후 Unity Plugin 캐시**: 가끔 Unity가 옛 .dll 잡음. Refresh 안 되면 에디터 재시작.

---

## ➡️ 다음 Phase 후보

**Phase 08 (옵션)**: PacketManager + PacketHandler 자동 dispatch 도입.
- 새 패킷 추가 시 PDL.xml 한 줄 + 핸들러 메서드만 작성하면 자동 등록
- 슬래시 커맨드 `/work:new-packet`이 그 흐름 자동화
- 미루고 M2 진입해도 OK (수동 switch dispatch가 짧으면 비용 작음)

**M2 First Connection** 진입 — 캐릭터 첫 이동 + Move/Snapshot 패킷.
- prediction + reconciliation 첫 도입
- 6월 캡스톤 1 옵션 B 도달

→ Phase 07 후 사용자 결정.

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모 누적.
> 끝나면 `07-pdl-integration-DONE.md`로 박제.
