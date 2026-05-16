# Phase 02 — ServerCore 정착(서버측만) 완료 박제

**완료일**: 2026-05-09
**커밋**: `c2ea772`
**소요 시간**: 약 2~3시간 (마이그 함정 실측 포함)

> **소급 박제** — 본 파일은 Phase 03 진입 직전 박제 정책(`-DONE.md` 페어)이 정해지면서 **소급 작성**되었습니다. 5단계 보고는 commit 메시지 + Phase 02 작업 로그 + 노션 세션 기록을 종합한 재구성입니다.

---

## 5단계 보고 (재구성)

### 🎯 무엇을 만들었나
ServerDev 4월 코드의 ServerCore 7파일(`Connector`, `JobQueue`, `Listener`, `PriorityQueue`, `RecvBuffer`, `SendBuffer`, `Session`)을 새 csproj `02_Server/Network/Dawnholder.Server.Network.csproj`로 정착. **.NET 10 그대로 유지**, namespace를 `Dawnholder.Server.Network`로 통일, nullable annotation 21곳을 청소.

### 🤔 왜 필요한가
이 7파일이 본 프로젝트의 **첫 실제 네트워크 코드**. 이후 모든 packet handler / framing / session 관리가 이 위에 쌓임. 서버측 토대를 *현 위치*에 정착시켜야 다음 Phase에서 클라이언트 socket 전략(갈래 X/Y2)을 *별개*로 결정할 수 있음.

### 🛠️ 어떻게 만들었나
- **`02_Server/Network/`로 정착, `98_Shared/`로 안 옮김** — 옛 plan은 `98_Shared/Net/`로 마이그(.NET Std 2.1)였으나, 마이그 함정 실측(nullable 13곳뿐) 결과 *가능*하나 *현업 표준 + 학습 가치*는 분리 모델 우세 판단. 서버측만 이번 Phase로 축소.
- **`<ImplicitUsings>enable</ImplicitUsings>`** — 실측 함정. 빠뜨리면 `ThreadLocal<>` 같은 흔한 타입조차 못 찾음 (.NET 10이 더 까다로움).
- **nullable 청소 패턴**: `T?` (정직: null 가능) vs `null!` (단언: 여기는 null 아님 — 컴파일러 그만 추적해) vs `default` (struct 반환). 의미 보고 골라 적용.
- **`JobQueueTests` 2개 추가** — actor 패턴의 핵심 invariant를 테스트로 박제: ① 빈 큐에 Push 시 즉시 실행 ② Flush 안에서 재진입 Push가 새 Flush 안 일으킴 (`m_Flush` 플래그).

### 🧪 테스트 결과
- ✅ `dotnet build Dawnholder.slnx` — **경고 0, 오류 0**
- ✅ `dotnet test` — 3개 통과 (smoke 1 + JobQueue 2)
- ✅ `dotnet run --project 02_Server/GameServer` — Phase 01 출력 정상 (Listener wire-up은 아직 안 함 — Phase 04 예정)
- ✅ namespace grep 검증 — `namespace ServerCore` 잔존 0곳

### ➡️ 다음 스텝
- **Phase 03**: Unity 클라 socket 전략 결정 (갈래 X 공유 DLL vs 갈래 Y2 분리 클라 라이브러리). 결정 직후 새 ADR + Phase 03 파일 재작성.
- 학습 일지(`/journal:phase`)는 본인 페이스에 따라 미루거나 진행.

---

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **마이그 갈래(`98_Shared/Net/`로 .NET Std 2.1) vs 정착 갈래(`02_Server/Network/`에 .NET 10 유지)** → 정착. 이유: 마이그 함정은 가벼움(nullable 13개)이지만 *현업 표준 + socket 자체 학습*은 분리 모델 우세. 클라측은 별도 라이브러리로 신작 예정.
- **`02_Server/` 자체는 .NET 10 vs .NET Std 2.1 일치** → 일치 안 함. 서버는 최신 런타임 활용.
- **nullable `null!` vs `T?` 선택** → 의미 보고 분기. `m_listenSocket`처럼 생성자 안에서 즉시 초기화될 거면 `null!` (의도 단언). `Pop()`처럼 *진짜* 빌 수 있으면 `T?` (정직).
- **`Server/Program.cs`의 `while(true) { Flush(); }` busy-loop 가져옴 vs 안 가져옴** → 안 가져옴. 헌법 #5 ("틱 루프 블로킹 금지") 정신 위반. Phase 04 wire-up 시 정정 예정.
- **`Session.cs`의 `unsafe ToBytes` 처리** → 그대로 둠 + `[Obsolete]` 마킹은 *향후*. 이번 Phase 범위 아님.

---

## 막혔던 지점

1. **`ImplicitUsings` 빠뜨려서 `ThreadLocal<>` 못 찾음**
   - 증상: ServerCore 복사 + 빌드 시 `ThreadLocal<T>` 등 흔한 타입에서 컴파일 에러.
   - 원인: 새 csproj가 `<ImplicitUsings>` 비활성. 기본 namespace 자동 임포트 안 됨.
   - 해결: csproj에 `<ImplicitUsings>enable</ImplicitUsings>` 한 줄. 실측에서 첫 번째로 만난 함정.

2. **nullable 청소가 실측보다 8개 더 나옴**
   - 사전 실측: 13개. 실제 청소: 21개.
   - 원인: 사전 실측은 .NET Standard 2.1 컴파일러로, 실제 정착은 .NET 10 컴파일러로. **.NET 10이 nullable 흐름 추적이 더 엄격**.
   - 패턴: `as` 결과를 `T?`로, `object? sender` 시그니처, struct 반환 시 `default`.
   - 학습: 컴파일러 버전이 nullable 추적의 *엄격도*에 영향. SDK 차이로 경고 수가 달라질 수 있음.

3. **namespace 일괄 변경 누락 가능성**
   - IDE 일괄 치환 후에도 grep으로 마지막 검증 필요. `grep -rn "namespace ServerCore" 02_Server/Network/` empty 확인. 안 그러면 한 파일 미루어진 것 빌드 시 충돌.

---

## 학습 일지 후보 키워드

- **`ImplicitUsings`의 정체** — .NET SDK 6+ 기능, 어떤 namespace가 자동 임포트되나
- **Nullable Reference Types (NRT)** — `null!` vs `T?` 의미 차이, 컴파일러 버전이 추적 엄격도에 미치는 영향
- **SocketAsyncEventArgs (SAEA) 패턴** — `Pending` 반환값 + `Completed` 이벤트, GC 부담 0인 비동기 socket
- **JobQueue = actor 미니 구현** — Push 멀티스레드 / Flush 단일스레드 보장, `m_Flush` 플래그의 invariant
- **ProjectReference vs PackageReference** — 같은 솔루션 내 csproj 참조 vs NuGet 패키지

---

## 후속 박제

- 노션 협업 히스토리 DB: [Phase 02 세션 로그](https://www.notion.so/35b76ceccb78816b85abde9b91217218)
- Phase 학습 일지: **미작성** (본인 페이스 따라 추후 `/journal:phase`)
