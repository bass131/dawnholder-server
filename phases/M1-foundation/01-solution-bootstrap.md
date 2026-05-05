# Phase 01: 솔루션 부트스트랩 + Hello World

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 1~1.5시간
> **담당 에이전트**: 메인 세션 (서브에이전트 없이)

---

## 🎯 목표

.NET 솔루션을 만들고, `dotnet run`으로 서버가 켜져서 콘솔에
"Hello, Dawnholder!" 한 줄을 찍는 상태까지.

**왜 이 작은 작업이 첫 Phase냐**: 환경이 진짜 동작하는지 검증하는 게
가장 먼저예요. 여기서 막히면 (예: .NET 버전 문제, 경로 문제) 다음
Phase 다 무의미해져요.

---

## ⏪ 사전 조건

- [ ] .NET 8 SDK 설치 완료 (`dotnet --version` 시 8.x.x 표시)
- [ ] Git 저장소 초기화 (`git init`)
- [ ] IDE 준비 (VSCode + C# Dev Kit, 또는 JetBrains Rider)

---

## 📝 작업 내용

- [ ] 루트에 솔루션 파일 생성: `dotnet new sln -n Dawnholder`
- [ ] `server/` 안에 GameServer 프로젝트 생성:
      `dotnet new console -n GameServer -o server/GameServer`
- [ ] `server/` 안에 테스트 프로젝트 생성:
      `dotnet new xunit -n GameServer.Tests -o server/GameServer.Tests`
- [ ] 솔루션에 두 프로젝트 추가:
      `dotnet sln add server/GameServer/GameServer.csproj`
      `dotnet sln add server/GameServer.Tests/GameServer.Tests.csproj`
- [ ] 테스트 프로젝트가 GameServer를 참조하도록:
      `dotnet add server/GameServer.Tests reference server/GameServer`
- [ ] `Program.cs`를 다음으로 교체:
      ```csharp
      Console.WriteLine("Hello, Dawnholder!");
      Console.WriteLine($"Server starting at {DateTime.UtcNow:o}");
      Console.WriteLine("Press Enter to exit.");
      Console.ReadLine();
      ```
- [ ] `.gitignore` 추가 (`dotnet new gitignore`)
- [ ] 첫 커밋: `git commit -m "chore: bootstrap solution with empty server"`

---

## ✅ 완료 조건

- [ ] `dotnet build` 가 경고/에러 없이 통과
- [ ] `dotnet run --project server/GameServer` 시 "Hello, Dawnholder!" 출력
- [ ] `dotnet test` 가 0 tests passed로 깔끔하게 통과
- [ ] `git status`가 clean (모든 변경 커밋됨)

---

## 🧪 테스트

**자동 테스트:**
- 아직 없음. xunit 프로젝트는 빈 상태로 검증만.

**수동 테스트:**
- 터미널에서 `dotnet run --project server/GameServer` 실행
- 출력 확인 후 Enter로 종료
- 터미널에서 `dotnet test` 실행하여 테스트 인프라 동작 확인

---

## 📚 학습 포인트

이번 Phase에서 처음 만나는 개념들:

1. **`.sln` (Solution) vs `.csproj` (Project) 차이**
   - csproj는 컴파일 단위 하나. sln은 여러 csproj를 묶는 메타파일.
   - IDE는 sln을 열고, 빌드 시스템은 각 csproj를 처리.

2. **`dotnet new` 템플릿 시스템**
   - `dotnet new console` = 콘솔 앱. `dotnet new xunit` = 테스트 프로젝트.
   - 다양한 템플릿이 있고 (`dotnet new --list`로 확인), 우리는 일단 두 개만.

3. **프로젝트 참조 (Project Reference)**
   - 테스트 프로젝트가 본체를 참조해야 본체의 클래스를 테스트 가능.
   - 상호 참조(circular reference)는 금지. 의존성은 한 방향.

4. **`.gitignore`의 `bin/`, `obj/`**
   - .NET이 빌드 시 만드는 임시 폴더. 절대 커밋하지 않음.
   - `dotnet new gitignore`가 알아서 추가.

---

## ⚠️ 함정 / 주의사항

- **솔루션 파일 위치**: 루트에 한 개만. 하위 폴더에 .sln 만들지 말 것.
- **Windows에서 경로**: 경로에 한글이나 공백이 있으면 가끔 dotnet CLI가
  꼬임. 영어/언더스코어만 쓰는 경로 추천.
- **닷넷 버전 mismatch**: `global.json` 안 만들었으면 시스템에 깔린
  최신 .NET을 씀. 8.x인지 한 번 더 확인.

---

## ➡️ 다음 Phase

**Phase 02: shared 라이브러리 + 클라/서버 참조**
- shared/ 폴더에 .NET Standard 2.1 라이브러리 만들고
- 서버에서 그 라이브러리의 상수를 참조해서 로그에 찍어보기
- (장차 Unity 클라도 같은 라이브러리를 참조할 예정)

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모를 여기 누적.
