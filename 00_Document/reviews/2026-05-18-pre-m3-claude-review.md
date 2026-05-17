# Tier 2 자동 리뷰 결과 — M2 ad-hoc 전체 감사 (pre-M3)

- **범위**: `range = ad-hoc-20260518-pre-m3-claude-review`
- **기준 commit**: main `aca7795` (Phase 08 마감 직후, M2 First Connection 완료)
- **점검 영역**: `02_Server/`, `98_Shared/`, `99_Tools/`, `04_ClientNet/`, `00_Document/`, 루트 `CLAUDE.md`/`CONTEXT.md`, `Dawnholder.slnx`, `global.json`
- **제외**: `03_Client/` 전체 (유현/인규 공유 영역), `.claude/` (별도 패스), `.github/`
- **실행자**: Claude reviewer agent (γ 방식의 α축, 2026-05-18 본인 호출)
- **메타**: Codex CLI 결과(β축)와 비교는 별도 문서 또는 본 보고서 부록으로 추가 예정

---

## 위반 / 주의 / 관찰 발견 목록

### [위반] 헌법 #3 (Trust Boundary) — 패킷 길이 헤더 상·하한 검증 누락

- **위치**: `02_Server/Network/Session.cs:28~30` (`PacketSession.OnRecv`)
- **증거**:
  ```csharp
  ushort dataSize = BitConverter.ToUInt16(buffer.Array!, buffer.Offset);
  if(buffer.Count < dataSize)
      break;
  // dataSize < HeaderSize(=2) 검증 없음. dataSize == 0 / 1 도 검증 없음.
  ```
- **위반 항목**: 헌법 §3 Trust Boundary + 체크리스트 축 3.4 (length-check 전 buffer 처리)
- **영향**: 악성 클라가 `dataSize=0` 또는 `dataSize=1`을 보내면 `OnRecvPacket`이 헤더만 있는(혹은 잘린) 버퍼로 호출되고 `processLen += 0/1`로 같은 위치를 계속 도는 *부분 무한 루프* 가능. 패킷 ID 디코드(`GameSession` L86~87)는 `dataSize=1`이면 1바이트 너머를 읽어 잘못된 PacketID로 dispatch (현재는 default → drop 안전). 또한 `dataSize > MaxPacketSize` 상한도 없음(이론상 65535까지 허용) — buffer 크기와 동일해 즉시 폭주는 X지만 *경계*. M3 broadcast 진입 시 첫 표적이 될 영역.
- **권장 조치**: `if (dataSize < HeaderSize || dataSize > MaxPacketSize) { Disconnect(); return processLen; }` 박기. `MaxPacketSize`는 `Shared.GameData.Constants`에 `4096` 정도로 박는 게 정석.
- **참고**: 동일 위반이 클라 측 `04_ClientNet/ClientSession.cs:50~52`에도 있으나 클라는 trusted 서버에서 받는 비대칭이라 우선순위 낮음.

### [위반] 02_Server/CLAUDE.md vs 현실 디렉토리 mismatch — stale Layout

- **위치**: `02_Server/CLAUDE.md:5~16` Layout 박스
- **증거**: 박힌 디렉토리 `Combat/ Persistence/ Handlers/`가 *현재 존재하지 않음*. `GameServer/`에 실제 있는 건 `Network/ Loop/ Maps/ Program.cs`뿐. 핸들러 dispatch는 `GameSession.OnRecvPacket`에 직접 박혀 있어 사실상 *Handlers 디렉토리 없는 상태*.
- **위반 항목**: 체크리스트 축 3 (구조 정합) — 문서가 *코드 진실*과 충돌.
- **영향**: 신규 합류자(7월 합류 팀원)가 layer CLAUDE.md만 읽고 "Handlers/에 신규 핸들러 추가하라"는 컨벤션을 따르려 하면 *디렉토리부터 만들어야* 함. 더 큰 문제는 `02_Server/CLAUDE.md`의 "새 packet handler 추가" 절차(L37~42)가 "`Handlers/`에 추가 + dispatch 테이블 등록"이라 박혀 있는데, 현실은 *직접 GameSession.OnRecvPacket switch 추가* 패턴. M3 broadcast 핸들러 추가 시 둘 중 어느 길을 따를지 모호.
- **권장 조치**: 둘 중 하나
  - **(a)** layer CLAUDE.md를 *현 상태*에 맞춰 응축(Handlers/Combat/Persistence를 "M4+ 도입 예정" 자리잡이로 명시)
  - **(b)** M3 진입 시 Handlers/ 디렉토리 + dispatch 테이블을 *먼저* 박고 GameSession.OnRecvPacket을 thin proxy로 정리. 도메인 패턴(축 5.4 명시적 상태 머신) 학습 가치 큼.
- **참고**: ARCHITECTURE.md L52~57도 같은 구조를 박고 있어 *동시 갱신* 필요.

### [위반] 헌법 우선순위 표에 `policies/` 빠짐

- **위치**: `CLAUDE.md:67` "충돌 시 우선순위" 표
- **증거**: `CLAUDE.md(헌법) > 00_Document/ADR/ > ARCHITECTURE.md > PRD.md` — `00_Document/policies/`가 표에 없음. 헌법 본문 L4 운영 원칙은 "정책·양식·운영 가이드는 `00_Document/policies/`로 외부화"라고 박혀 있어 *존재는 인정*하나 충돌 시 어디 끼는지 미정의.
- **위반 항목**: 헌법 vs 헌법 내부 자기 일관성. 추가 점검 후보 "헌법 vs ADR 일관성" 항목 직접 명중.
- **영향**: 합류 후 팀원이 `policies/pin-and-done.md`와 `ADR-013` 또는 헌법 본문이 *상충하는 양식*을 박으면 어느 게 이기는지 모호. 본인 1인일 땐 머릿속 해석으로 무마됐지만 M3+ 팀원 진입 시 충돌점.
- **권장 조치**: 한 줄 보강. 추천: `CLAUDE.md(헌법) > 00_Document/ADR/(결정) > 00_Document/policies/(운영) > 00_Document/ARCHITECTURE.md(구조) > 00_Document/PRD.md(요구사항)`. policies는 ADR 결정의 *운영 풀이*라 ADR 하위가 맞음.

---

### [주의] CONTEXT.md 응축 정책 임계 초과 (200줄 한도 → 현재 296줄)

- **위치**: `CONTEXT.md` 전체
- **증거**: 296줄. 본문 L8 "~200줄 넘으면 큰 마일스톤 끝날 때마다 처음부터 재작성" 박혀 있음. M2 First Connection 마감 = "큰 마일스톤 끝" 상태.
- **위반 항목**: CONTEXT.md 자기 정책 위반 + ADR-014(220줄 임계) 정신.
- **영향**: 다음 세션 시작 시 첫 로드 토큰 ~50% 증가. 응축본의 정의가 "응축"인데 누적 중. M3 진입 *직전* 시점이 재작성 적기.
- **권장 조치**: M3 진입 전(또는 직후 1회) CONTEXT.md 재작성. 옛 디테일은 `CONTEXT_History.md`로 이전. 본인 결정 사항.

### [주의] ProtocolVersion.cs 정의만 있고 핸드셰이크 미구현

- **위치**: `98_Shared/Protocol/ProtocolVersion.cs:25` (`Current = 2`)
- **증거**: `ProtocolVersion.Current`를 `02_Server/`, `04_ClientNet/`, `03_Client/Assets/Scripts/Network/` 어디서도 *호출하지 않음* (grep 결과 코드 사용처 0건, 모두 문서/주석/검사 스크립트만 hit). 헌법 §2 + ARCHITECTURE Network 섹션 + `98_Shared/CLAUDE.md` "Protocol 버전 핸드셰이크"에서 *의무*로 박혀 있는 첫 패킷 교환이 코드 단계에서 미실현.
- **위반 항목**: 체크리스트 축 1 (헌법 §2 Protocol Sacred) + 축 3 (ARCHITECTURE Network) — *문서엔 박혀 있고 코드엔 없는 비대칭*.
- **영향**: M3 진입 후 옛 클라가 새 서버에 붙으면 *silent mismatch*. 헌법이 "관대하게 처리 금지 — silent mismatch는 hard error보다 나쁘다"고 명시한 바로 그 상황. 현재는 1인 개발 + 같은 머신이라 잠복 중.
- **권장 조치**: M3 진입 전(또는 첫 Phase로) C_Handshake/S_HandshakeAck 패킷 1쌍 추가. PDL.xml 맨 아래 박기. Phase 단위로 끊으면 ~1~2시간 작업.

### [주의] SendBufferHelper/SendBuffer/JobQueue/PriorityQueue/Connector — ServerCore dead code (테스트만 참조)

- **위치**: `02_Server/Network/SendBuffer.cs`, `JobQueue.cs`, `PriorityQueue.cs`, `Connector.cs`
- **증거**: `Grep SendBufferHelper`/`SendBuffer` 02_Server 안 — 자기 정의 파일 1건만 hit (사용처 0). `JobQueue` — 정의 + 테스트만 (production 사용 0). `PriorityQueue` — 정의만 (테스트조차 없음). `Connector` — 정의만 (server는 listener 측, 서버 connector는 적합 use case 0).
- **위반 항목**: 축 5.6 (미래 확장성 hook인데 호출자 0). 헌법 위반 X — 학습/박물관 코드는 ADR-011 ("ServerDev 부분 채택") 정신에 부합.
- **영향**: ServerCore.csproj 빌드 시간 약간 증가, IDE 내비게이션 노이즈. 클라쪽 04_ClientNet의 `SendBuffer.cs`/`SmokeProbe.cs`도 비슷한 자리잡이 상태일 가능성 있음 (이번 패스 미점검).
- **권장 조치**: 3갈래
  - **(a)** 그대로 두고 자리잡이로 활용 (M4 broadcast 시 SendBufferHelper 재사용 가능, ADR-011 패턴 일관)
  - **(b)** 별도 `02_Server/Network/Legacy/` 폴더로 격리하고 `<Compile Remove>`로 빌드 제외
  - **(c)** 삭제 후 `git history`로 복구 가능 표시
- **추천**: **(a) 유지** — 자리잡이 효과가 Connector에서 이미 3번째 증명(CONTEXT.md L71).

### [주의] PacketSession.OnRecv — packetCount > 1 시 매번 로그

- **위치**: `02_Server/Network/Session.cs:43~47`
- **증거**: `if (packetCount > 1) { Console.WriteLine($"...모아보내기 {packetCount} Packets"); Console.WriteLine($"Receive Socket Data Success."); }` — 두 줄 모두 매 batch 마다.
- **위반 항목**: `02_Server/CLAUDE.md` L21 "Info 레벨에서 패킷 페이로드 로깅 금지 (PII / 스팸). Trace 레벨에서만." 정신 — payload는 아니지만 *스팸 측면*에서 동일. 또한 헌법 #5 정신 (틱 hot path) — `OnRecv`는 socket worker thread지만 GameSession→EnqueueJob 경유로 결국 tick path와 close path.
- **영향**: M3 두 명 동시 접속 시 1봇 + 1 클라 = batch 자주 발생. 콘솔 스팸 가능성.
- **권장 조치**: Serilog 도입 시 Trace 레벨로 강등 + 두 번째 Console.WriteLine 통째 제거 (의미 중복). 현재 Serilog 미통합 상태(`ARCHITECTURE.md`엔 박혀 있으나 코드 0건)라 자연 후속.

---

### [관찰] 새 핸들러 양식 — happy/invalid/auth 3단 의무 누락

- **위치**: `02_Server/GameServer/Network/GameSession.cs` (현 핸들러 2개: HandlePing, HandleMoveIntent)
- **증거**: `PacketRoundTripTests.cs`에 라운드트립 6개 (각 PacketID 1개씩) 박혀 있고 `InputBitsTests`/`PhysicsTests`/`MoveIntentTests`로 도메인 단위 테스트는 풍부. 그러나 *핸들러 직접 테스트* (GameSession.HandleMoveIntent에 invalid bits 패킷을 던지면 cheat log + entity Pending 0) 형태는 없음. 통합 테스트(M2BasicMovementIntegrationTests)가 happy path만 cover.
- **위반 항목**: 체크리스트 축 4.2 (새 핸들러 invalid 거부 테스트) — *현 핸들러 수 = 2개*. M3에서 핸들러가 늘면 빠르게 빚이 됨.
- **영향**: invalid input cheat 로그 패턴이 *회귀로 깨져도* 단위 테스트가 못 잡음. PacketRoundTrip + InputBits로 부분 cover 되지만 GameSession 단위는 빈 칸.
- **권장 조치**: M3 진입 시 핸들러 layer 추출(위 두 번째 위반 해결 묶음) + GameSession은 thin proxy → 핸들러 단위 xUnit 테스트 박을 수 있는 형태로 진화. Phase 단위 1개로 큰 작업.

### [관찰] 패킷 Write() — 매 호출 `new byte[ushort.MaxValue]` (65KB) 할당

- **위치**: `98_Shared/Protocol/Generated/GenPackets.cs:67, 127, 196, 259, 319, 408` (모든 Write)
- **증거**: PDL.xml로부터 자동 생성. 모든 `Write()`가 `byte[] segment = new byte[ushort.MaxValue]` → ArraySegment slice. M2 SnapshotTickInterval=5(=250ms)에서 본인 1명일 땐 초당 4번 × 64KB = 256KB/s alloc. M3 두 명이면 8/s × 64KB = 512KB/s. M8 100명 도달 시 ~25MB/s GC 압박.
- **위반 항목**: 체크리스트 축 5.2 (hot path 알로케이션 — 패킷 처리 경로). 헌법 #5 위반 아님 (틱 자체는 spin 기반 ✓).
- **영향**: 단기(M3)는 문제 X. PRD tick p99 < 10ms 기준 55배 마진 박혀있어 즉시 압박 X. M5+ broadcast scale-up 시 첫 GC hotspot 후보.
- **권장 조치**: 지금은 **무시 OK**. ADR-002의 PDL 생성기는 *템플릿만 바꾸면 양쪽 동시 진화* 가능 — PDL.xml에 박힌 모든 Write가 한 번에 SendBufferHelper(이미 박혀있는 dead code!) 또는 `ArrayPool<byte>` 패턴으로 갈아엎힘. M5+ 부하 테스트에서 실측 후 결정. 자리잡이 보존 결정의 *이런* 이유 때문에 SendBufferHelper 삭제 X 권장(앞 항목과 연결).

### [관찰] UnitTest1.cs — 빈 placeholder 테스트 (의미 0)

- **위치**: `02_Server/GameServer.Tests/UnitTest1.cs`
- **증거**: `[Fact] public void Test1() { }` — body 비어있음. dotnet new xunit 기본 산출물 그대로.
- **위반 항목**: 체크리스트 축 4.8 (테스트 이름이 의도를 표현 못 함).
- **영향**: 110/111 통과 카운트에서 1건이 이거. 의미 없는 통과 수 1 = 정직성에 흠. CI 시그널 약화.
- **권장 조치**: 통째 삭제 1줄 작업. CHANGELOG 박을 가치 없음.

### [관찰] PDL ↔ 생성 .cs sync 정합 — OK 확인

- **위치**: `99_Tools/PacketGenerator/PDL.xml` ↔ `98_Shared/Protocol/Generated/GenPackets.cs`
- **증거**: PDL의 6개 packet (C_Ping/S_Pong/S_EnterMap/S_LeaveMap/C_MoveIntent/S_Snapshot) ↔ GenPackets.cs의 6개 클래스 + PacketID enum 6개 모두 1:1 대응. 필드 순서/타입 일치. v2 변경(byte input 비트필드, vx/vy 추가)이 양쪽 정합.
- **결론**: 정합. *재생성 필요 흔적 0*.

### [관찰] ADR INDEX ↔ 실제 ADR 폴더 정합 — OK 확인

- **위치**: `00_Document/ADR/INDEX.md` ↔ `00_Document/ADR/{tech-stack,gameplay,harness}/`
- **증거**: INDEX의 21개 ADR (001~021)이 실제 파일과 1:1 대응. 폐기/누락 0건. ADR-019 (reviewer agent) 박혀 있고 본 감사가 그 결과물 행위.

### [관찰] 5축 통과 영역 (잘 된 부분)

- **헌법 #1 Server Authority**: `GameSession.HandleMoveIntent`가 클라에서 좌표를 받지 않고 *의도만* 받음. `GameMap.Tick`이 권위 `Physics.Step`만 적용. 클라가 보낸 좌표를 echo하는 코드 0건. ✓
- **헌법 #2 Protocol Sacred**: `[MessagePackObject]` 사용 X(자체 PDL), PDL.xml 주석에 "은퇴한 ID 재사용 금지"+"맨 아래에만 추가" 박혀 있음. PacketID 1~6 가용. ✓
- **헌법 #4 Shared Discipline**: `Shared.csproj` `<EmbedAllSources>true` + `<DebugType>embedded</DebugType>` 박혀 있어 ADR-010 물리적 강제. csproj 빌드 후 03_Client/Plugins로 자동 복사. ✓
- **헌법 #5 No Blocking**: `TickScheduler.RunLoop`이 `SpinWait.SpinUntil` 사용. `await`/`Thread.Sleep`/`Task.Run`/동기 DB 호출 *틱 안에서* 0건. 주석에 "헌법 #5 정신 부합" 명시. ✓
- **ADR-012 Y2 socket 분리**: `Dawnholder.Server.Network.csproj`와 `Dawnholder.Client.Net.csproj` 분리. 98_Shared에 socket 코드 0건 (Connector/Listener는 02_Server/Network/와 04_ClientNet/ 양쪽 분리, 패킷 정의만 통합). ✓
- **ADR-004 20 TPS**: `Constants.ServerTickRate = 20` 단일 출처. 하드코딩 우회 0건. ✓
- **테스트 커버리지 4.5 (PDL 라운드트립)**: 6 packet × 라운드트립 6개 박혀 있음 (`PacketRoundTripTests.cs`). 헌법 권고 영역. ✓
- **Phase 마감 박제 (ADR-013)**: `01_Phases/youngho/M2-first-connection/`에 8개 `-DONE.md` 박힘. ✓

---

## 학습 포인트

### Protocol versioning은 *코드*까지 박혀야 사는 약속

`ProtocolVersion.cs:25` 한 줄 `const ushort Current = 2`만 박고 *호출하지 않은* 상태가 가장 위험합니다. 문서엔 "v1→v2 bump"가 박혀 있어 사람은 "버전 관리되고 있다"고 느끼지만, 코드는 *그 약속을 실행하지 않음*. 합류한 팀원이 `ProtocolVersion.Current = 3`으로 바꿔도 *아무 일도 일어나지 않는* 상태입니다. 헌법 §2가 "관대하게 처리 금지 — silent mismatch는 hard error보다 나쁘다"고 박은 이유가 정확히 이것 — 약속만 박힌 시스템은 *약속 위반이 silent하게 통과*함. M3 진입 *전*에 핸드셰이크 박는 것을 강력 추천. 비용은 Phase 1개 (~1~2시간), 효과는 *옛 클라 자동 차단 + 헌법 §2 문서/코드 정합 회복*. "코드가 박혀야 약속이 진짜다" — 이게 production hardening의 첫 발걸음.

### Dead-code는 두 종류 — 박물관 코드 vs 자리잡이 코드

`SendBufferHelper`/`Connector`/`PriorityQueue`/`JobQueue`가 현재 ServerCore에서 0회 호출되지만, `ADR-011`(ServerDev 부분 채택) 정신으로 *자리잡이*로 박혀 있어요. 박물관 코드(과거 잔재, 이제 안 쓰는 패턴)와 자리잡이 코드(미래 호출자가 들어올 자리)는 *겉모습이 같음* — 둘 다 빌드는 되는데 호출자가 0개. 구별 기준은 *주석에 박힌 사용 의도*입니다. `Connector.cs:21~25`처럼 "헤드리스 봇 재사용 의도"가 박혀 있고 실제 3번째 증명까지 했으면 자리잡이. 주석 없이 비어있으면 박물관. M3 진입 시 한 번씩 훑어보고 자리잡이 표시(주석 1줄) 박는 습관이 유용합니다. "왜 안 지웠나"의 답이 *주석으로 살아있어야* 6개월 후 본인도 헷갈리지 않아요.

---

## 카테고리별 카운트

| 영역 | 위반 | 주의 | 관찰 |
|------|------|------|------|
| 02_Server | 1 (Session.cs length-check) | 2 (CLAUDE.md stale Layout + OnRecv 로그 스팸) | 2 (UnitTest1 빈 placeholder + 핸들러 단위 테스트 없음) |
| 98_Shared | 0 | 1 (ProtocolVersion 미호출) | 2 (Write() 65KB alloc + PDL↔Generated 정합 OK) |
| 04_ClientNet | 0* | 0 | 0 |
| 99_Tools | 0 | 0 | 0 |
| 00_Document | 1 (헌법 우선순위 표 policies 누락) | 1 (CONTEXT.md 296줄) | 2 (ADR INDEX 정합 + 5축 통과) |
| ServerCore 자리잡이 | - | 1 (dead code 자리잡이) | - |
| **합계** | **3** | **5** | **6** |

\* `04_ClientNet`의 동형 length-check 위반은 trusted server 수신 비대칭으로 우선순위 낮음 분류 (별도 패스에서 다룰 가치 있음).

---

## 권장 액션 (우선순위)

### 🔴 우선 fix — M3 진입 전 박는 게 효과 큼
1. **위반 1** (Session.cs length-check) — 30분. M3 broadcast가 첫 표적 영역이라 *지금이 적기*.
2. **위반 3** (CLAUDE.md policies 우선순위) — 1줄 보강. 합류 팀원 첫 혼란 차단.

### 🟡 M3 첫 Phase로 묶기 (자연 합류 후보)
3. **주의 2** (ProtocolVersion 핸드셰이크 미구현) — C_Handshake/S_HandshakeAck 한 쌍, Phase 1개 (~1~2시간).
4. **위반 2** (02_Server/CLAUDE.md Handlers 디렉토리 mismatch) — M3에서 핸들러 layer 분리 묶음. 헌법 #1 *코드 시연*도 같이.

### 🟢 M3 진입 직전 정리 작업
5. **주의 1** (CONTEXT.md 296→200줄 응축) — M2 마감 시점이 응축 적기.
6. **관찰** (UnitTest1 빈 placeholder 삭제) — 1줄.

### 📝 보류 OK — M5+ 부하 측정 후 결정
- **관찰** (Write 65KB alloc) — PRD 기준 55배 마진 박혀 있어 즉시 X.
- **주의 3** (dead code 자리잡이) — 학습 가치 보존이 더 큼. ADR-011 정신.
- **주의 4** (OnRecv 로그 스팸) — Serilog 도입 시 자연 해결.

---

## 메타

- 이번 감사는 ad-hoc 전체 패스 — Tier 2 자동 리뷰가 PR 단위라면 본 패스는 *누적 상태 점검*.
- ADR-019 후속으로 "Phase 마감 시 자동 ad-hoc 감사" 슬래시 커맨드 후보(`/work:audit`?) 가능성 발견 — 별도 ADR로 박는 것은 본인 결정 영역.
- 클라이언트 공유 영역(`03_Client/`)과 하네스(`.claude/`)는 *별도 패스* (요청한 대로 제외).
- 본 보고서는 *추측 0*. 모든 위반/주의/관찰은 파일:줄 + 인용으로 박힘.
- Codex CLI 결과(β축)는 별도 문서 `2026-05-18-pre-m3-codex-review.md`로 박은 후 본 보고서에 비교 부록 추가 예정.
