# Phase 01: 솔루션 부트스트랩 + DLL 빌드 파이프라인

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 1.5~2시간 (Unity DLL 인식이 첫 시도에 안 되면 +α)
> **담당 에이전트**: 메인 세션 (서브에이전트 없이)

---

## 🎯 목표

.NET 10 솔루션 + `shared` 빈 라이브러리(.NET Standard 2.1) + 비어있는 Unity 6.4 LTS 프로젝트를 만들고, **shared.dll이 자동으로 Unity Plugins/에 복사되어 IntelliSense + F12 원본 코드 표시까지 동작**하는 상태를 만든다.

ServerCore/PacketGenerator/DummyClient 같은 **ServerDev 코드 이주는 Phase 02~04에서**. 이번 Phase는 토대 검증 전용.

**왜 이게 첫 Phase냐**: CONTEXT.md에 본인이 박은 "가장 위험한 부분 — shared의 .NET Standard 2.1 빌드가 Unity Plugins/에서 정상 인식되는지. 첫 시도에 안 될 가능성 큼"을 **가장 일찍 만나기 위함**. ADR-010(DLL + Embedded PDB)이 진짜 동작하는지 검증 안 되면 그 위에 쌓을 게 의미 없음.

---

## ⏪ 사전 조건

- [ ] .NET 10 SDK 설치 (`dotnet --version` → 10.x.x)
- [ ] Unity 6.4 LTS 설치 (Unity Hub에서)
- [ ] Git 저장소 초기화 (이미 됨)
- [ ] IDE: VSCode + C# Dev Kit 또는 JetBrains Rider
- [ ] (Phase 02부터 사용) ServerDev 폴더 접근 가능: `C:\Users\bass1\바탕 화면\ServerDev\Dawnholder_Server\`

---

## 📝 작업 내용

### A. .NET 솔루션 부트스트랩

- [ ] 루트에 `global.json` 생성 — .NET 10 SDK 고정
      ```json
      { "sdk": { "version": "10.0.0", "rollForward": "latestFeature" } }
      ```
- [ ] 솔루션 생성: `dotnet new sln -n Dawnholder`
- [ ] 서버 콘솔 프로젝트:
      `dotnet new console -n GameServer -o server/GameServer -f net10.0`
- [ ] **shared 라이브러리 (.NET Standard 2.1)**:
      `dotnet new classlib -n Shared -o shared/Shared -f netstandard2.1`
- [ ] 테스트 프로젝트:
      `dotnet new xunit -n GameServer.Tests -o server/GameServer.Tests -f net10.0`
- [ ] 솔루션에 세 프로젝트 추가
- [ ] 참조 연결:
      - `dotnet add server/GameServer reference shared/Shared`
      - `dotnet add server/GameServer.Tests reference server/GameServer`

### B. shared 라이브러리 PDB 임베드 + 자동 복사 설정 (ADR-010 핵심)

- [ ] `shared/Shared/Shared.csproj` 보강:
      ```xml
      <PropertyGroup>
        <TargetFramework>netstandard2.1</TargetFramework>
        <DebugType>embedded</DebugType>
        <EmbedAllSources>true</EmbedAllSources>
      </PropertyGroup>
      ```
- [ ] `shared/Shared/Shared.csproj`에 post-build target 추가 — 빌드 산출물(.dll + .pdb)을 `client/Assets/Plugins/Shared/`로 복사:
      ```xml
      <Target Name="CopyToUnityPlugins" AfterTargets="Build">
        <ItemGroup>
          <_Outputs Include="$(TargetDir)$(TargetName).dll;$(TargetDir)$(TargetName).pdb" />
        </ItemGroup>
        <MakeDir Directories="$(MSBuildThisFileDirectory)..\..\client\Assets\Plugins\Shared\" />
        <Copy SourceFiles="@(_Outputs)"
              DestinationFolder="$(MSBuildThisFileDirectory)..\..\client\Assets\Plugins\Shared\"
              SkipUnchangedFiles="true" />
      </Target>
      ```

### C. 더미 코드로 검증 경로 만들기

- [ ] `shared/Shared/GameConstants.cs` — 공유 상수 한 개 + 한국어 주석
      ```csharp
      namespace Shared;

      /// <summary>게임 전역 상수. 이 주석이 Unity F12 시 그대로 보여야 한다.</summary>
      public static class GameConstants
      {
          /// <summary>서버 틱 레이트 (TPS). ADR-004 참조.</summary>
          public const int ServerTickRate = 20;
      }
      ```
- [ ] `server/GameServer/Program.cs` — shared 상수 사용 확인
      ```csharp
      using Shared;

      Console.WriteLine("Hello, Dawnholder!");
      Console.WriteLine($"Server tick rate (from shared): {GameConstants.ServerTickRate} TPS");
      Console.WriteLine($"Server starting at {DateTime.UtcNow:o}");
      Console.WriteLine("Press Enter to exit.");
      Console.ReadLine();
      ```

### D. Unity 빈 프로젝트 + DLL 인식 검증

- [ ] Unity Hub로 `client/` 안에 **2D Core 템플릿** 빈 프로젝트 생성 (Unity 6.4 LTS)
      - 프로젝트 이름: `Dawnholder`, 위치: `C:\Users\bass1\바탕 화면\ClaudeDev\client\`
- [ ] `dotnet build` 실행 → `client/Assets/Plugins/Shared/Shared.dll` + `Shared.pdb` 자동 생성 확인
- [ ] Unity 에디터 열어서 새로고침(Assets > Refresh, Ctrl+R)
- [ ] Unity 에디터에서 새 C# 스크립트 `Assets/TestPluginAccess.cs` 생성:
      ```csharp
      using UnityEngine;
      using Shared;

      public class TestPluginAccess : MonoBehaviour
      {
          void Start() => Debug.Log($"Tick rate: {GameConstants.ServerTickRate}");
      }
      ```
- [ ] `using Shared;` 줄에서 IntelliSense가 `Shared` 인식하는지 확인
- [ ] `GameConstants` 위에서 F12(Go to Definition) → **원본 .cs 파일이 한국어 주석 포함해 ReadOnly로 열리는지 확인**
- [ ] (확인 끝나면 `TestPluginAccess.cs`는 삭제해도 됨 — 검증용)

### E. .gitignore + 첫 커밋

- [ ] `.gitignore`에 다음 항목 들어있는지 확인 (이미 1차 셋업에서 만들어졌을 가능성):
      ```
      bin/
      obj/
      # Unity
      client/Library/
      client/Temp/
      client/Logs/
      client/UserSettings/
      client/Builds/
      # shared 빌드 산출물 (Unity가 인식하지만 git에는 안 들어감)
      client/Assets/Plugins/Shared/*.dll
      client/Assets/Plugins/Shared/*.pdb
      client/Assets/Plugins/Shared/*.dll.meta
      client/Assets/Plugins/Shared/*.pdb.meta
      ```
- [ ] 커밋: `chore(M1-01): bootstrap solution + shared DLL pipeline`

---

## ✅ 완료 조건

- [ ] `dotnet --version` → 10.x.x
- [ ] `dotnet build` 경고/에러 없이 통과 (한 번에 server + shared + tests 다 빌드)
- [ ] `dotnet run --project server/GameServer` → "Hello, Dawnholder!" + "Server tick rate: 20 TPS" 출력
- [ ] `dotnet test` 0 tests passed (테스트 인프라만 검증)
- [ ] Unity 에디터에서 `using Shared;` IntelliSense 동작
- [ ] F12로 `GameConstants` 정의 점프 시 **원본 .cs 코드 + 한국어 주석** 표시 (디컴파일된 형태 X)
- [ ] `git status` clean
- [ ] `client/Assets/Plugins/Shared/*.dll`이 git에 안 들어감 (`.gitignore`로)

---

## 🧪 테스트

**자동**: 없음 (xunit 인프라만 동작 검증)

**수동**:
1. 터미널에서 `dotnet build` → 산출물이 `client/Assets/Plugins/Shared/`에 떨어지는지
2. 터미널에서 `dotnet run --project server/GameServer` → 콘솔 출력 확인
3. Unity 에디터에서 IntelliSense + F12 동작 확인 (위 D 단계)
4. shared/Shared/GameConstants.cs의 주석 한 글자 바꾸고 → `dotnet build` → Unity 새로고침 → F12로 변경 반영 확인 (라이브 사이클)

---

## 📚 학습 포인트

이번 Phase에서 처음 만나는 개념들:

1. **.NET Standard 2.1 vs net10.0** — Unity 6.4 LTS의 Mono/IL2CPP가 인식하는 공통 API 사양이 .NET Standard 2.1. 그래서 shared/는 반드시 netstandard2.1로 빌드, server/는 net10.0이어도 OK.
2. **DLL + Embedded PDB** — `EmbedAllSources=true`로 .pdb 안에 .cs 원본까지 통째로 임베드. F12 시 IDE가 임베드된 소스를 ReadOnly로 표시 → C++의 헤더+구현 분리 모델보다 풍부 (모든 함수 바디 보임).
3. **MSBuild Target** — `<Target Name="..." AfterTargets="Build">` 으로 빌드 후 자동 작업. post-build event의 csproj 표준 방식.
4. **Unity Plugins/ 폴더 규약** — `Assets/Plugins/` 안의 .dll은 자동으로 컴파일 대상에 포함. 일반 .cs와 동등하게 IntelliSense + 디버깅 동작.
5. **`global.json`** — 머신에 여러 .NET SDK가 깔려있을 때 이 프로젝트가 쓸 SDK 버전 고정. 팀 작업 일관성.

---

## ⚠️ 함정 / 주의사항

- **.NET 10 SDK 미설치 시**: `global.json`만 있으면 fallback 안 됨. `dotnet --list-sdks`로 확인. 없으면 https://dotnet.microsoft.com/download/dotnet/10.0
- **shared csproj 타겟 실수**: `net10.0`로 만들면 Unity 인식 안 됨. **반드시 `netstandard2.1`**.
- **PDB 종류**: `<DebugType>portable</DebugType>` (기본값)면 .pdb가 .dll 옆에 별도 파일로 생기지만 임베디드 소스는 안 들어감. 반드시 `embedded` + `EmbedAllSources=true`.
- **Unity가 .dll 인식 못 함**: 보통 `Assets > Refresh` 또는 Unity 에디터 재시작으로 해결. 그래도 안 되면 .dll의 Plugin Inspector에서 platforms 체크.
- **한글/공백 경로**: `바탕 화면` 경로 자체에 한글이 있어서 가끔 dotnet/MSBuild가 꼬임. 안 되면 영문 경로(`C:\dev\ClaudeDev` 등)로 옮기는 안도 고려.
- **F12에서 디컴파일된 코드만 보임**: PDB가 .dll에 임베드 안 됐거나, IDE가 PDB 못 찾음. embedded 모드 + .pdb 파일 자체도 같이 복사되었는지 확인.

---

## ➡️ 다음 Phase

**Phase 02: ServerCore 이주 (Listener, Session, Buffer, JobQueue)**
- ServerDev `ServerCore`의 코드를 `shared/Net/`로 이주
- 클래스별로 csproj 마이그레이션 + 빌드 검증
- 한 클래스씩 이주하며 헌법/ADR 준수 확인

(Phase 02~04 파일도 시나리오 B 기준으로 갱신 필요. Phase 01 끝나는 시점에 한 번에 갱신.)

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모를 여기 누적.

### 2026-05-09 진행 중 발견

1. **CS8400 — file-scoped namespace** (가장 큰 학습 포인트)
   - 증상: `dotnet run` 시 `error CS8400: '파일 범위 네임스페이스' 기능은 C# 8.0에서 사용할 수 없습니다`.
   - 원인: `.NET Standard 2.1`의 default C# 언어 버전은 **8.0**. `namespace Foo;` 형식은 C# 10+.
   - 해결: `shared/Shared.csproj`에 `<LangVersion>latest</LangVersion>` 추가.
   - **핵심 개념**: ".NET Standard 2.1 = API 호환 사양"이지 "C# 컴파일러 사양"이 아니다. 컴파일러는 .NET 10 SDK가 제공 → 최신 C# 문법으로 .NET Standard 2.1 라이브러리 빌드 가능.

2. **Embedded PDB → 별도 `.pdb` 파일 없음**
   - `<DebugType>embedded</DebugType>` + `<EmbedAllSources>true</EmbedAllSources>`이면 디버그 정보 + 원본 .cs 전부가 `.dll` 안에 통째로 들어감 → `.pdb` 파일 자체가 생성되지 않음.
   - 본 phase 본문에 적었던 ".dll + .pdb 복사"는 부정확 — **`.dll` 하나만 복사**.
   - csproj `<Target>` 블록도 `.dll`만 포함하면 충분. `.pdb`도 명시했지만 missing 시 Copy가 silently skip하므로 실해(實害) 없음.

3. **`.gitignore` 깊이 패턴 함정**
   - 1차 셋업 `.gitignore`엔 `client/Assets/Plugins/*.dll` (한 단계만 매칭).
   - 우리는 `Plugins/Shared/Shared.dll`(한 단계 깊이)로 두므로 매칭 안 됨 → 빌드 산출물이 git에 들어갈 뻔.
   - 수정: `client/Assets/Plugins/**/*.dll` (재귀 매칭).
   - **검증 도구**: `git check-ignore -v <path>`로 어떤 패턴이 매칭하는지 확인.

4. **Unity Hub의 client/ 폴더 점유**
   - Unity Hub는 빈 폴더에만 새 프로젝트 생성. 기존 `client/CLAUDE.md` 등이 있으면 거부 또는 덮어쓰기 위험.
   - 처리: `client/CLAUDE.md` 임시 root로 백업 → `client/` 통째 삭제 → Unity 신규 프로젝트 생성 → CLAUDE.md 복원 → `dotnet build` 재실행으로 Shared.dll 자동 복사.

5. **`.slnx` 형식 등장**
   - .NET 10 SDK가 만든 솔루션은 옛 `.sln`이 아니라 XML 기반 `.slnx`. 호환 정상 동작. `dotnet sln add/list`도 그대로.

### 2026-05-09 검증 결과

- ✅ `dotnet build` 무경고 통과
- ✅ `dotnet run` → "Hello, Dawnholder! / Server tick rate: 20 TPS / 50ms 간격" 출력
- ✅ 자동 복사 동작 (`client/Assets/Plugins/Shared/Shared.dll` 생성, 10KB)
- ✅ Unity 6.4 LTS 에디터에서 `using Shared.GameData;` IntelliSense 동작
- ✅ `Constants` 호버 시 한국어 주석 툴팁 표시
- ✅ F12 → 원본 .cs 코드(한국어 주석 포함) ReadOnly 표시 → **ADR-010 동작 검증**
- ✅ `.gitignore` 패턴 정정 + `git check-ignore`로 검증
