# Phase 03 — 클라용 socket 라이브러리 신작 (ClientNet) 완료 박제

**완료일**: 2026-05-10
**커밋**: `fb7a06d` (feat(client-net): ClientNet 라이브러리 신작 — Connector/ClientSession/Recv·SendBuffer)
**소요 시간**: 약 2시간 (세션 1회 끊김 + 재진입 포함)

---

## 5단계 보고

### 🎯 무엇을 만들었나
Unity 클라이언트가 사용할 socket 레이어 라이브러리(`Dawnholder.Client.Net.dll`)를 별도 csproj(`04_ClientNet/`)로 신작하고, 빌드 시 자동으로 Unity Plugins 폴더에 복사 + Unity 안에서 **원본 한국어 주석**까지 보이도록 검증 완료. 실제 connect/send 동작은 Phase 04에서, 이번 Phase는 *라이브러리 토대* 박는 데 집중.

### 🤔 왜 필요한가
Y2 갈래(ADR-012) — 서버/클라가 **각자의 socket 코드**를 갖는 분리 모델. 서버측은 Phase 02에서 `02_Server/Network/`에 정착됐고, 이번엔 클라측 짝꿍을 만들어 양쪽이 *같은 패턴, 다른 컨텍스트*로 굴러갈 토대를 마련. 이 토대 없이 Phase 04(첫 connect 스모크)로 가면 서버측 코드를 클라가 어떻게 잡을지 결정도 안 된 채 디버깅 폭발.

### 🛠️ 어떻게 만들었나
- **csproj 4종 옵션 셋트** (`.NET Std 2.1` + `<LangVersion>latest</LangVersion>` + `<ImplicitUsings>enable</ImplicitUsings>` + `<DebugType>embedded</DebugType>` + `<EmbedAllSources>true</EmbedAllSources>`) — Phase 01·02 함정을 미리 회피.
- **5개 .cs 파일**: `RecvBuffer`/`SendBuffer`는 서버 거의 그대로(주석만 클라 컨텍스트), `ClientSession`은 base + PacketSession 두 클래스에 **Unity main thread 침범 금지 경고 박스** 추가, `Connector`는 `Func<ClientSession>`로 타입 좁힘, `SmokeProbe`는 검증용 Marker 1개.
- **고려했지만 안 고른 대안**: ① 서버 7파일 통째 복사 → 학습 가치 0, 클라 무관 코드(Listener) 끌고 옴. ② framing만 `98_Shared/`로 추출 → ADR-012 트레이드오프 ④에서 *진짜 변경이 양쪽을 깰 때*까지 미루기로 결정.
- **신규 개념**: `<EmbedAllSources>` (PDB에 .cs 원본을 통째 임베드 → 디컴파일 대신 원본 표시), Unity main thread 제약 (socket 콜백 vs GameObject 접근).

### 🧪 테스트 결과
- `dotnet build Dawnholder.slnx`: **5개 프로젝트 경고 0 / 오류 0** (GameServer, GameServer.Tests, Network, Shared, ClientNet)
- DLL 자동 복사: `03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll` (28KB) 생성 확인
- Unity 6.4 LTS에서 `using Dawnholder.Client.Net;` 인식 ✅
- `SmokeProbe`에서 **F12 → 원본 .cs + 한국어 주석 ReadOnly 표시 ✅** (ADR-010 두 번째 인스턴스 검증)
- Plugin Inspector platform 설정 기본값 OK ✅
- `_VerifyClientNet.cs` 정리 완료

### ➡️ 다음 스텝
- **Phase 04**: 서버 Listener wire-up (`Program.cs`에서 7777 listen) + Unity 측 첫 connect 스모크 + main thread queue 첫 도입.
- 알아두면 좋을 후속: 같은 라이브러리를 `99_Tools/`의 헤드리스 봇이 그대로 재사용 — Connector의 `count` 파라미터를 살린 이유. 부하 테스트 시점이 오면 즉시 활용 가능.

---

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **csproj 옵션 5종 설계** → Phase 01(`<LangVersion>latest</LangVersion>` 누락 시 CS8400) + Phase 02(`<ImplicitUsings>` 없으면 `ThreadLocal<>` 못 찾음) 함정을 모두 미리 회피한 형태로 한 번에 박음. 두 번째 인스턴스라 정착감 있음.
- **`Connector.cs`의 `count` 파라미터 보존** vs 클라 단순화 → 보존 채택. 같은 라이브러리를 `99_Tools/` 헤드리스 봇이 재사용할 거라 N개 가짜 클라 시나리오가 자연스럽게 들어옴. 미래 비용을 지금 0원에 사두는 형태.
- **`Func<Session>` → `Func<ClientSession>`** 타입 좁힘 → 컴파일 타임에 "ClientSession 상속해야 함"을 강제. 학습 가치(타입 시스템 활용) + 안전성 둘 다.
- **Unity main thread 경고를 ClientSession.cs 상단에 박스 주석** → Phase 04에서 main thread queue 도입할 때 자리 표시. 코드를 처음 보는 미래의 자기/팀원이 즉시 인지하도록.
- **`98_Shared/`로 framing 추출 보류** → ADR-012 트레이드오프 ④. *진짜로 양쪽을 깰 때까지* 분리 신작 우선. 추상화 비용을 *진짜 필요*가 드러난 후로 미룸.
- **`-DONE.md` 페어 정책 두 번째 인스턴스** → 5단계 보고 후 즉시 박제. Phase 01·02 소급 작성과 달리 *처음부터 정상 흐름*으로 박힌 첫 케이스.

---

## 막혔던 지점

이번 Phase는 큰 막힘 없음. 세션이 한 번 끊겼지만 `CONTEXT.md` + Phase 03 파일 통독으로 5분 안에 이어붙임 (재진입 비용 = 박제의 가치 입증).

소소한 발견:
- **`.gitignore` 추가 작업 불필요**: Phase 01 시점에 `Plugins/**/*.dll` 같은 *재귀 패턴*으로 박아둔 덕분에 ClientNet 하위도 자동 매칭. 선견지명의 이자.
- **`Plugins/ClientNet.meta`**: Unity가 폴더 단위 .meta를 자동 생성. 이건 git에 들어가야 함 (Shared.meta와 동일 패턴) — `.gitignore`는 *.dll.meta / *.pdb.meta만 무시하므로 폴더 .meta는 정상 커밋.

---

## 학습 일지 후보 키워드

`/journal-concept <키워드>` 로 펼칠 만한 것들:

- **Embedded PDB & EmbedAllSources** — 디컴파일 대신 원본 표시가 가능한 메커니즘. 일반 .pdb vs portable .pdb vs embedded .pdb 차이. F12가 "메타데이터에서" 가는 게 아니라 *임베드된 원본*으로 가는 동작 원리.
- **Unity main thread 제약** — `UnitySynchronizationContext`, main thread queue 패턴, .NET 스레드풀 워커 vs Unity의 update loop. Phase 04에서 본격 도입 전 개념 선제 학습 가치 큼.
- **`SocketAsyncEventArgs` 패턴** — 왜 콜백 기반? `async/await`(Task 기반) 대비 GC 부담/풀링/지연 특성 차이. 게임 서버에서 SAEA 선호되는 이유.
- **TCP byte stream의 패킷 경계 모호성** — `OnRecv` 안의 while-loop가 "절반 도착 / 1.5개 도착" 케이스를 모두 흡수하는 구조. 헤더 prefix 패턴이 *왜* 표준인지.
- **클라이언트 socket vs 서버 socket의 비대칭** — Listener 부재, 단일 connection의 함의, 객체 풀링 우선순위 차이. Phase 03 학습 포인트 #1.
- **Y2 갈래의 트레이드오프 회고** — 같은 코드 두 벌의 *진짜 비용*을 두 번 짜고 나서 체감. ADR-012 추가 메모로 가능.

---

## 메모 (다음 세션을 위한)

- Phase 04 진입 시 Phase 04 파일(`04-framing-and-pingpong.md`)이 **outdated**. Phase 03 파일에 명시된 대로 "Listener wire-up + 첫 connect 스모크"로 재작성 필요. 첫 framing 코드는 Phase 05로 이동.
- `99_Tools/` 헤드리스 봇은 아직 미존재. Phase 04~05 어딘가에서 ClientNet을 첫 소비자로 쓰는 봇 한 개를 만들어두면 부하 테스트 시점에 즉시 가동 가능.
- `CONTEXT.md`의 "현재 멈춤 지점"을 Phase 03 완료 + Phase 04 진입 전으로 갱신 필요 (다음 세션 시작 시).
