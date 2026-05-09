# Phase 02: ServerCore 정착 (서버측만)

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 2~3시간
> **담당 에이전트**: 메인 세션 + `netcode` 서브에이전트
> **재작성**: 2026-05-09 (옛 plan은 Phase 01에 흡수되어 폐기)

---

## 🎯 목표

ServerDev 4월 코드의 ServerCore 7파일(`Connector` / `JobQueue` / `Listener` / `PriorityQueue` / `RecvBuffer` / `SendBuffer` / `Session`)을 **`02_Server/Network/`** 로 정착. **.NET 10 그대로 유지** — 마이그 X — 다만 `<ImplicitUsings>enable</ImplicitUsings>` 추가 + nullable annotation 13곳 청소.

**이 Phase가 중요한 이유**: 본 프로젝트의 첫 *실제 네트워크 코드*가 들어오는 단계. 이후 모든 packet handler, framing, session 관리가 이 위에 쌓여요. 단 **클라측 socket 전략(X/Y 갈래)은 Phase 03 시작 시점에 결정** — 이번 Phase는 *서버측만* 다룹니다.

**왜 옛 plan을 폐기했나**: 옛 plan은 "ServerCore를 `98_Shared/Net/`에 .NET Standard 2.1로 마이그"였는데, 마이그 함정 실측 결과 *가능*하긴 하나 *현업 표준 + 학습 가치*는 분리 모델(Y)이 우세. 일단 서버측만 정착시킨 뒤 클라측 전략은 별도 결정.

---

## ⏪ 사전 조건

- [x] Phase 01 완료 (솔루션 부트스트랩 + DLL 파이프라인 검증)
- [x] 폴더 prefix 정렬 + 경로 정합성 갱신 (commit `071680e`)
- [x] ServerCore 마이그 함정 실측 (2026-05-09) — nullable 13개 외 함정 없음
- [ ] 헌법(`CLAUDE.md`) + `ADR-001/002/010/011` 통독
- [ ] 5대 원칙 + 시나리오 B 인지

---

## 📝 작업 내용

### 1단계: 새 프로젝트 생성

- [ ] 폴더 생성: `02_Server/Network/`
- [ ] csproj 작성: `02_Server/Network/Dawnholder.Server.Network.csproj`
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <LangVersion>latest</LangVersion>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>   <!-- 함정 회피, 실측됨 -->
      <AllowUnsafeBlocks>true</AllowUnsafeBlocks>  <!-- Session.cs 일부 -->
    </PropertyGroup>
  </Project>
  ```

### 2단계: ServerCore 7파일 복사

- [ ] 원본: `C:\Users\bass1\바탕 화면\ServerDev\Dawnholder_Server\ServerCore\`
- [ ] 대상: `02_Server/Network/`
- [ ] 파일: `Connector.cs`, `JobQueue.cs`, `Listener.cs`, `PriorityQueue.cs`, `RecvBuffer.cs`, `SendBuffer.cs`, `Session.cs`

### 3단계: namespace 일괄 변경

- [ ] 모든 파일 `namespace ServerCore` → `namespace Dawnholder.Server.Network`
- [ ] 우리 헌법 컨벤션(`Dawnholder.{영역}.{모듈}`)과 일치
- [ ] grep으로 누락 검증: `grep -rn "namespace ServerCore" 02_Server/Network/` 결과 empty 확인

### 4단계: nullable annotation 청소 (13곳, 실측됨)

| 경고 | 의미 | 위치 | 청소 방향 |
|------|------|------|----------|
| CS8603 (4곳) | 가능한 null 반환 | JobQueue.Pop, SendBuffer 2곳, PriorityQueue.Pop, SendBuffer.cs L11 | 리턴 타입을 `T?`로 |
| CS8625 (4곳) | null 리터럴을 non-nullable에 할당 | Listener L61, Connector L46, Session L159·L216 | 매개변수/필드 nullable로 |
| CS8618 (3곳) | non-nullable 필드 초기화 누락 | Connector.m_SessionFactory, Listener.m_SessionFactory, Session.m_Socket | 두 갈래: ① nullable 선언(`Func<Session>?`) ② `null!`로 강제 초기화 후 첫 사용 시 throw — 의미 보고 결정 |
| CS8602 (1곳) | null 가능 참조 역참조 | Listener.cs L57 | null check 추가 |
| CS8600 (1곳) | null 가능 값을 non-nullable로 | Connector.cs L37 | 결과 변수를 nullable로 |

→ 학습 가치: 13개를 *왜 그렇게* 청소했는지가 곧 nullable 학습.

### 5단계: GameServer.csproj가 Network 참조

- [ ] `02_Server/GameServer/GameServer.csproj`에 추가:
  ```xml
  <ItemGroup>
    <ProjectReference Include="..\Network\Dawnholder.Server.Network.csproj" />
    <ProjectReference Include="..\..\98_Shared\Shared.csproj" />  <!-- 기존 유지 -->
  </ItemGroup>
  ```

### 6단계: 솔루션에 새 csproj 등록

- [ ] `Dawnholder.slnx` 갱신:
  ```xml
  <Folder Name="/02_Server/">
    <Project Path="02_Server/GameServer.Tests/GameServer.Tests.csproj" />
    <Project Path="02_Server/GameServer/GameServer.csproj" />
    <Project Path="02_Server/Network/Dawnholder.Server.Network.csproj" />  <!-- 신규 -->
  </Folder>
  ```

### 7단계: 빌드 검증

- [ ] `dotnet build Dawnholder.slnx`
- [ ] **목표: 경고 0, 오류 0**. nullable warning이 남아있다면 4단계 미완.

### 8단계: 단위 테스트 (smoke)

- [ ] `02_Server/GameServer.Tests/JobQueueTests.cs` 작성:
  ```csharp
  using Dawnholder.Server.Network;
  using Xunit;

  public class JobQueueTests
  {
      [Fact]
      public void Push_ExecutesActionImmediately_WhenQueueEmpty()
      {
          var queue = new JobQueue();
          int counter = 0;
          queue.Push(() => counter++);
          Assert.Equal(1, counter);
      }

      [Fact]
      public void Push_DoesNotReentrantFlush_WhenAlreadyFlushing()
      {
          // 첫 Push의 Flush 안에서 두 번째 Push가 들어와도
          // 두 번째 Push의 호출자가 Flush를 다시 시작하지 않음 (m_Flush 플래그)
          var queue = new JobQueue();
          int counter = 0;
          queue.Push(() => {
              queue.Push(() => counter++);  // 재진입 시도
          });
          Assert.Equal(1, counter);  // 두 작업 모두 첫 Flush에서 실행됨
      }
  }
  ```

### 9단계: 커밋

- [ ] `feat(network): ServerDev ServerCore 7파일을 02_Server/Network로 정착 (.NET 10 유지, nullable 청소)`

---

## ✅ 완료 조건

- [ ] `dotnet build Dawnholder.slnx` — **경고 0, 오류 0**
- [ ] `dotnet test` — JobQueueTests 2개 통과
- [ ] `02_Server/Network/`에 7파일 + csproj 존재
- [ ] `GameServer.csproj`가 Network ProjectReference
- [ ] 모든 namespace `Dawnholder.Server.Network`로 통일 (grep 검증)
- [ ] 솔루션이 IDE에서 깨끗이 로드 (3개 프로젝트 표시)

---

## 🧪 테스트

**자동 테스트** (이번 Phase 추가):
- `JobQueueTests.Push_ExecutesActionImmediately_WhenQueueEmpty`
- `JobQueueTests.Push_DoesNotReentrantFlush_WhenAlreadyFlushing`

**수동 테스트**:
- `dotnet run --project 02_Server/GameServer` — Phase 01 출력 그대로 나오면 OK (이번 Phase에선 Listener wire-up 안 함, Phase 04 예정)

---

## 📚 학습 포인트

이번 Phase에서 처음 만나는/깊어지는 개념들:

1. **SocketAsyncEventArgs (SAEA) 패턴**
   - .NET의 가장 빠른 비동기 socket API. **콜백 기반**(`Completed` 이벤트).
   - `Task<int> ReceiveAsync(...)` 같은 직관적 API보다 학습 곡선 높지만 **GC 부담 0**(SAEA 객체를 풀링)이라 게임 서버 표준.
   - Session.cs에서 `SocketAsyncEventArgs` 풀 + `Pending` 반환값 + `Completed` 이벤트 패턴을 본문에서 직접 보세요.

2. **JobQueue — actor 모델 미니 구현**
   - 헌법 5번째 원칙("틱 루프 블로킹 금지") + `02_Server/CLAUDE.md` ("맵은 단일 스레드, 맵 안에서는 lock 없음")의 토대.
   - **Push는 멀티스레드, Flush는 단일 스레드 보장**. 첫 Push가 Flush 책임을 가짐 (`m_Flush` 플래그).
   - 식당 비유: 카운터에 누구나 주문지 던질 수 있지만, 주방엔 한 번에 한 명만 들어가서 다 처리하고 나옴.

3. **ImplicitUsings의 정체**
   - .NET SDK 6+ 기능. 자동으로 일부 namespace 임포트.
   - 비활성화하면 `ThreadLocal<>` 같은 흔한 타입도 명시적 `using System.Threading;` 필요 (실측에서 첫 에러).
   - `<ImplicitUsings>enable</ImplicitUsings>` 한 줄로 켜짐.

4. **Nullable Reference Types (NRT)**
   - C# 8부터 도입. `string?` vs `string`이 *의미*를 가짐.
   - `<Nullable>enable</Nullable>`이면 컴파일러가 null 흐름을 추적해 잠재적 NullReferenceException을 *컴파일 타임에* 발견.
   - 우리가 청소한 13곳이 정확히 이 추적의 결과.

5. **ProjectReference vs PackageReference**
   - `ProjectReference`: 같은 솔루션 내 csproj. 소스 변경 시 함께 빌드.
   - `PackageReference`: NuGet 패키지. 미리 빌드된 .dll.
   - 우리는 `GameServer` → `Network`로 ProjectReference (한 빌드).

---

## ⚠️ 함정 / 주의사항

- **`ImplicitUsings` 빠뜨리면 `ThreadLocal<>` 못 찾음** — 실측 확인된 함정. csproj 한 줄 추가로 해결.
- **nullable 청소 시 의미 살펴 결정**: `Pop()`이 *진짜* null 반환할 수 있으면 `T?`. 만약 호출자가 null을 못 쓴다면 `null!`은 거짓말 — 호출자 쪽에서 null check 추가가 옳음.
- **namespace 변경은 IDE 일괄 치환** 활용 (대소문자 정확). 마지막 `grep` 검증으로 누락 잡기.
- **`Session.cs`의 `unsafe ToBytes`는 `[Obsolete]` 마킹된 코드** — 그대로 두되 향후 제거 후보. 학습 일지 메모거리.
- **`Server/Program.cs`(4월)의 `while(true) { Flush(); }` busy-loop은 헌법 #5 정신 위반** — 이번 Phase에선 그 코드 가져오지 않음 (Phase 04에서 wire-up 시 정정).

---

## ➡️ 다음 Phase

**Phase 03: Unity 클라 socket 전략 결정 + 클라 측 시작**
- 갈래 X(`98_Shared/Net/`로 ServerCore 마이그 + DLL 공유) vs 갈래 Y(자체 작성, 현업 표준)
- 이번 Phase에서 서버측 코드를 만지며 일정 감각이 잡힌 뒤 결정
- 결정되면 새 ADR로 박기

> 옛 Phase 03(`03-tcp-listener.md`) / Phase 04(`04-framing-and-pingpong.md`)는 1차 셋업 시점 본이라 outdated. **Phase 03 시작 시점에 재작성** 필요.

---

## 작업 로그

**2026-05-09**: ServerCore 7파일 정착 완료

- `02_Server/Network/Dawnholder.Server.Network.csproj` 신설 (.NET 10, `ImplicitUsings`/`Nullable`/`AllowUnsafeBlocks` 모두 enable)
- 7파일 namespace 일괄 변경: `ServerCore` → `Dawnholder.Server.Network`
- nullable 청소 **21곳** (실측 13 + .NET 10이 추가 추적한 7 + 그 후 1)
  - 패턴: `T?` 리턴 / `null!` 단언(`m_listenSocket!`) / `as` 결과를 `T?`로 받기 / 시그니처 `object? sender` / struct 반환 시 `default`
  - 학습: `null!` ("여기는 null 아님 — 컴파일러야 그만 추적해") vs `T?` ("정직: null 가능") 의미 구분
- `GameServer.csproj` + `GameServer.Tests.csproj`가 Network ProjectReference
- `Dawnholder.slnx`에 등록
- `JobQueueTests` 2개 작성 + 통과 (총 3 tests passed: smoke 1 + JobQueue 2)
- `dotnet build Dawnholder.slnx`: **경고 0, 오류 0**

**검증된 함정**:
- `ImplicitUsings` 빠뜨리면 `ThreadLocal<>`도 못 찾음 — csproj 한 줄로 회피 (실측됨)
- .NET 10 컴파일러가 .NET Standard 2.1보다 nullable 추적이 더 엄격 (실측 13 + 추가 8)

**다음 Phase**:
- Phase 03 시작 시 클라측 socket 전략 결정 (X / Y 갈래)
