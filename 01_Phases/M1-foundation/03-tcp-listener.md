# Phase 03: 클라용 socket 라이브러리 신작 (ClientNet, .NET Std 2.1)

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 2~3시간
> **담당 에이전트**: 메인 세션 + `netcode` 서브에이전트
> **재작성**: 2026-05-10 (Y2 갈래 확정 후 통째 재작성. 옛 plan = "TCP 리스너 + Session"은 폐기 — 서버 Listener는 Phase 04로 이동)
> **근거 ADR**: ADR-012 (Y2 분리 모델) + ADR-010 (DLL + Embedded PDB 패턴 재사용)

---

## 🎯 목표

ServerCore와는 **별개로** Unity 클라이언트용 socket 라이브러리를 새 csproj로 신작한다. 산출 DLL을 `03_Client/Assets/Plugins/ClientNet/`에 자동 복사 + Unity F12로 원본 코드(한국어 주석)까지 보이는 상태까지 검증.

**완료 조건은 "빌드 + Unity 인식 + 클래스 형 점검까지"** — 실제 서버에 connect해서 패킷 주고받는 스모크는 **Phase 04**에서. 이번 Phase는 *라이브러리 토대*만.

**왜 이 범위로 잘랐나**:
- 한 Phase = 1~3시간 (헌법 권고). "라이브러리 신작 + 서버 wire-up + 연결 스모크"를 한 Phase에 넣으면 4~5시간 + 디버깅 폭발 위험.
- 라이브러리가 *Unity 안에서 인식*되는지가 ADR-010 패턴 두 번째 검증 (`Shared.dll` 다음). 이걸 먼저 건강하게 박아두면 Phase 04에서 connect 디버깅에만 집중 가능.

---

## ⏪ 사전 조건

- [x] Phase 01 완료 (DLL 파이프라인 검증 — Shared.dll이 Unity Plugins/에 자동 복사 + F12 동작)
- [x] Phase 02 완료 (서버측 ServerCore 7파일 정착, `02_Server/Network/`)
- [x] ADR-012 박힘 (Y2 결정)
- [ ] 헌법(`CLAUDE.md`) + ADR-001/002/010/012 + `02_Server/Network/Session.cs` 통독
- [ ] **이번 Phase의 핵심 통찰 인지**: 클라는 connect만 하므로 `Listener.cs`는 안 옮김. `Connector` + `Session` + 버퍼만.

---

## 📝 작업 내용

### 1단계: 새 csproj 생성

- [ ] 폴더 생성: `04_ClientNet/`
- [ ] csproj 작성: `04_ClientNet/Dawnholder.Client.Net.csproj`
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>netstandard2.1</TargetFramework>     <!-- Unity 호환 (ADR-010) -->
      <LangVersion>latest</LangVersion>                      <!-- file-scoped namespace 등 (Phase 01 함정) -->
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>                <!-- ThreadLocal<> 회피 (Phase 02 함정) -->
      <DebugType>embedded</DebugType>                        <!-- ADR-010: PDB 임베드 -->
      <EmbedAllSources>true</EmbedAllSources>                <!-- F12에서 원본 .cs + 한국어 주석 -->
      <RootNamespace>Dawnholder.Client.Net</RootNamespace>
      <AssemblyName>Dawnholder.Client.Net</AssemblyName>
    </PropertyGroup>

    <!-- 빌드 후 03_Client/Assets/Plugins/ClientNet/ 으로 자동 복사 -->
    <Target Name="CopyToUnityPlugins" AfterTargets="Build">
      <ItemGroup>
        <_ClientNetOutput Include="$(TargetDir)$(TargetName).dll" />
      </ItemGroup>
      <MakeDir Directories="$(MSBuildThisFileDirectory)..\03_Client\Assets\Plugins\ClientNet\" />
      <Copy SourceFiles="@(_ClientNetOutput)"
            DestinationFolder="$(MSBuildThisFileDirectory)..\03_Client\Assets\Plugins\ClientNet\"
            SkipUnchangedFiles="true" />
    </Target>
  </Project>
  ```
- [ ] 함정 가드: `.NET Std 2.1`이라도 `<LangVersion>latest</LangVersion>` + `<ImplicitUsings>` 둘 다 켜기 (Phase 01·02 실측됨).

### 2단계: 솔루션 등록

- [ ] `Dawnholder.slnx`의 `<Folder Name="/04_ClientNet/">` 신설:
  ```xml
  <Folder Name="/04_ClientNet/">
    <Project Path="04_ClientNet/Dawnholder.Client.Net.csproj" />
  </Folder>
  ```
- [ ] `dotnet sln Dawnholder.slnx list`로 4개 프로젝트(GameServer, GameServer.Tests, Network, ClientNet) 모두 보이는지 확인.

### 3단계: 클라용 socket 코드 신작

ServerCore 7파일 중 **클라에 필요한 것만 골라 새로 작성**. 그대로 복사 X — 클라 컨텍스트(단일 connection, Unity main thread)에 맞게 손봄.

#### 3-1. `04_ClientNet/RecvBuffer.cs`
- [ ] 서버측 `02_Server/Network/RecvBuffer.cs`와 거의 동일 (링버퍼 패턴). 그대로 가져오되:
  - namespace = `Dawnholder.Client.Net`
  - **목적 주석 추가** ("클라는 단일 connection, 그래도 부분 수신 처리 위해 동일 패턴 사용")

#### 3-2. `04_ClientNet/SendBuffer.cs`
- [ ] 마찬가지로 서버측과 거의 동일. namespace만 변경.

#### 3-3. `04_ClientNet/ClientSession.cs`
- [ ] 서버측 `Session.cs`의 base `Session` 클래스 + `PacketSession` 패턴을 *클라 버전*으로 재작성.
- [ ] **중요한 차이점**:
  - 클라는 단일 connection이라 `m_PendingList`/큐 패턴은 동일하게 유지하되 — Unity main thread 침범 금지가 추가 제약. `OnConnected`/`OnRecv` 콜백이 socket 콜백 스레드에서 호출됨 → **Unity API 직접 호출 금지** (Phase 04에서 Unity 측 wrapper로 main thread marshalling 처리 예정).
  - 주석에 "Unity main thread 외 스레드에서 호출 — 직접 GameObject 접근 금지" 명시.
- [ ] 핵심 메서드: `Start(Socket)`, `Send(ArraySegment<byte>)`, `Disconnect()`, abstract `OnConnected`/`OnDisconnected`/`OnRecv`/`OnSend`.

#### 3-4. `04_ClientNet/Connector.cs`
- [ ] 서버측 `Connector.cs` 거의 그대로 (서버 connector도 클라 → 서버 conn 시뮬용이라 사실상 클라 패턴).
- [ ] namespace 변경. `Func<ClientSession>` 으로 타입 좁힘.

#### 3-5. `04_ClientNet/SmokeProbe.cs` (검증용 빈 셸)
- [ ] Unity 측에서 typeof(SmokeProbe) 만 봐도 Plugin이 잡히는지 확인할 수 있는 작은 클래스:
  ```csharp
  namespace Dawnholder.Client.Net;

  /// <summary>
  /// 이 클래스가 Unity에서 보이고 한국어 주석이 F12에서 보이면 Phase 03 검증 통과.
  /// 본 사용은 Phase 04에서 ClientSession + Connector를 직접 씁니다.
  /// </summary>
  public static class SmokeProbe
  {
      /// <summary>Unity F12 시 이 한국어 문장이 그대로 보여야 함 (ADR-010 동작 확인).</summary>
      public const string Marker = "ClientNet 라이브러리가 Unity에서 정상 인식됨";
  }
  ```

### 4단계: 빌드 검증

- [ ] `dotnet build Dawnholder.slnx`
  - 목표: **경고 0, 오류 0**.
  - nullable 경고 나오면 청소 (Phase 02 패턴: `T?` 정직 / `null!` 단언 / `default` struct 선택).
- [ ] 빌드 후 `03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll` 생성 확인.

### 5단계: Unity 인식 검증

- [ ] Unity 6.4 LTS 에디터 열기 → `Assets > Refresh` (Ctrl+R).
- [ ] Unity 에디터에서 임시 스크립트 `03_Client/Assets/_VerifyClientNet.cs` 생성:
  ```csharp
  using UnityEngine;
  using Dawnholder.Client.Net;

  public class _VerifyClientNet : MonoBehaviour
  {
      void Start() => Debug.Log(SmokeProbe.Marker);
  }
  ```
- [ ] `using Dawnholder.Client.Net;`에 IntelliSense 동작 확인.
- [ ] `SmokeProbe` 위에서 **F12** → 원본 .cs 코드 + 한국어 주석 ReadOnly로 표시되는지 확인 (ADR-010 두 번째 검증 통과).
- [ ] (확인 완료 후 `_VerifyClientNet.cs`는 삭제)

### 6단계: .gitignore 갱신

- [ ] `.gitignore`에 ClientNet 산출물 패턴 추가 (Phase 01의 Shared 패턴을 따라):
  ```
  03_Client/Assets/Plugins/ClientNet/*.dll
  03_Client/Assets/Plugins/ClientNet/*.pdb
  03_Client/Assets/Plugins/ClientNet/*.dll.meta
  03_Client/Assets/Plugins/ClientNet/*.pdb.meta
  ```
- [ ] `git check-ignore -v 03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll`로 매칭 확인 (Phase 01 함정 #3 재발 방지).

### 7단계: 커밋

- [ ] `feat(client-net): ClientNet 라이브러리 신작 — Connector/ClientSession/RecvBuffer/SendBuffer (.NET Std 2.1, Unity 자동 복사)`

---

## ✅ 완료 조건

- [ ] `dotnet build Dawnholder.slnx` — **경고 0, 오류 0** (4개 프로젝트 모두)
- [ ] `04_ClientNet/`에 csproj + 5개 .cs 파일 (Connector, ClientSession, RecvBuffer, SendBuffer, SmokeProbe)
- [ ] 솔루션 IDE에서 4개 프로젝트 깨끗이 로드
- [ ] `03_Client/Assets/Plugins/ClientNet/Dawnholder.Client.Net.dll` 자동 생성
- [ ] Unity 에디터에서 `using Dawnholder.Client.Net;` IntelliSense 동작
- [ ] Unity F12 시 `SmokeProbe.Marker`의 한국어 주석 ReadOnly로 표시 (디컴파일 X)
- [ ] `git check-ignore`로 .dll이 git 무시 패턴에 매칭

---

## 🧪 테스트

**자동 테스트**: 이번 Phase는 **신설 안 함** (라이브러리 인터페이스 형만 잡고 동작은 Phase 04에서 검증).
- 굳이 만들고 싶다면 `RecvBuffer`의 링버퍼 invariant 테스트 정도 가능. 다만 서버측 `RecvBuffer`와 동일 코드라 중복 위험.

**수동 테스트**:
1. `dotnet build Dawnholder.slnx` → 4개 프로젝트 빌드 통과
2. Unity 에디터에서 위 5단계 IntelliSense + F12 검증
3. `Dawnholder.Client.Net.dll`을 Unity Plugin Inspector에서 클릭 → platform 설정 확인 (기본값 OK여야 함)

---

## 📚 학습 포인트

이번 Phase에서 새로 만나거나 깊어지는 개념들:

1. **클라이언트 socket vs 서버 socket의 비대칭**
   - 서버: Listener로 *수동적 accept* (수많은 connection). 객체 풀링 + GC 부담 최소화 핵심.
   - 클라: Connector로 *능동적 connect* (단 하나의 connection). Listener 자체가 없음.
   - 같은 SocketAsyncEventArgs 패턴이지만 *복잡도와 최적화 우선순위*가 다름.

2. **Unity main thread 제약**
   - Socket 콜백은 .NET 스레드풀의 워커 스레드에서 호출. Unity의 `GameObject`/`Transform`/`Rigidbody2D` 등은 **main thread에서만** 접근 가능.
   - 잘못 접근 시: `UnityException: get_isActiveAndEnabled can only be called from the main thread`.
   - 해결 패턴(Phase 04에서 본격 도입): main thread queue 또는 `UnitySynchronizationContext` 활용.

3. **ADR-010 패턴의 두 번째 인스턴스화**
   - Phase 01에서 `Shared.dll` 한 번 검증. 이번에 `Dawnholder.Client.Net.dll`로 **재사용** — 패턴이 "한 번 만들면 끝"이 아니라 *서비스로 사용 가능*함을 확인.
   - 향후 새 라이브러리 추가 시(`Dawnholder.Client.Audio` 등) 같은 패턴 그대로 복붙 가능.

4. **Y2 갈래의 실전 모양**
   - 같은 `RecvBuffer`/`SendBuffer` 코드를 양쪽에 두 벌. *코드 중복*이라는 단점이 *현업 표준 + 변경 내성*과 맞바꿔진 것.
   - "그러면 framing 부분만 떼서 `98_Shared/`로 옮기면 안 되나?" — 가능 (ADR-012의 트레이드오프 ④ 참조). 단 이번 Phase는 *분리 신작*을 우선 박고, 공유 추출은 *진짜로 framing 변경이 양쪽을 깰 때* 결정.

5. **csproj `<RootNamespace>` + `<AssemblyName>` 명시 의미**
   - 명시 안 하면 폴더명/파일명에서 추론 → `04_ClientNet`이라는 폴더명에 숫자 prefix가 있어 추론이 어색해짐.
   - 명시하면 외부에서 보는 어셈블리 이름이 깨끗 (`Dawnholder.Client.Net.dll`).

---

## ⚠️ 함정 / 주의사항

- **`<TargetFramework>net10.0</TargetFramework>`로 만들면 Unity 인식 X** — `netstandard2.1` 강제 (ADR-010, Phase 01 함정).
- **`<LangVersion>latest</LangVersion>` 누락 시 `namespace Foo;` 컴파일 안 됨** (Phase 01 CS8400 함정 재발).
- **`<ImplicitUsings>enable</ImplicitUsings>` 누락 시 `ThreadLocal<>` 등 흔한 타입 못 찾음** (Phase 02 실측 함정 재발).
- **`.gitignore` 깊이 패턴 함정** — `Plugins/ClientNet/` 한 단계 더 깊으니 `Plugins/ClientNet/*.dll` 명시 또는 `Plugins/**/*.dll` 재귀 패턴 사용 (Phase 01 함정 #3 재발 방지). `git check-ignore`로 검증.
- **Unity .meta 파일** — DLL이 Plugins/에 처음 들어갈 때 Unity가 .meta 자동 생성. 이 .meta는 *플랫폼 설정*을 담고 있어서 git에 들어갈 수 있으나 우리 패턴은 `.gitignore`에 .meta도 무시 → 각자 머신에서 처음 한 번 Unity가 자동 생성.
- **Unity가 .dll 인식 못 함**: `Assets > Refresh`. 그래도 안 되면 Unity 에디터 재시작. 그래도 안 되면 .dll의 Plugin Inspector에서 platforms 체크.
- **서버 코드를 그대로 복사하지 말 것** — 똑같이 짜더라도 *클라 컨텍스트 주석*을 추가하는 게 학습 가치. ("같은 코드를 두 번 짜는 동안 차이를 인지" 가 Y2의 학습 임팩트).

---

## ➡️ 다음 Phase

**Phase 04: 서버 Listener wire-up + 클라 첫 연결 스모크**
- 서버 `Program.cs`에 `Listener` 인스턴스화 + 포트 7777 listen (현재는 ServerCore 코드만 있고 main에서 안 부름)
- Unity 측 `MonoBehaviour` 하나 만들어서 ClientNet의 `Connector` 호출 → 서버에 connect → 양쪽 로그 확인
- Unity main thread marshalling 첫 도입 (`Update()`에서 main thread queue drain)
- 첫 framing 코드는 **Phase 05**로 — 이번엔 raw bytes 송수신 한 줄만 시연

> 옛 Phase 04 파일(`04-framing-and-pingpong.md`)은 outdated. Phase 04 시작 시점에 재작성 (Listener wire-up + connect 스모크 기준).

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모를 여기 누적.
> Phase 끝나면 이 내용을 `03-tcp-listener-DONE.md`로 박제 (헌법: `-DONE.md` 페어 규칙).
