# Phase 02: shared 라이브러리 + 클라/서버 참조

> **상태**: pending
> **마일스톤**: M1 - Foundation
> **예상 소요**: 1~2시간
> **담당 에이전트**: 메인 세션

---

## 🎯 목표

`shared/` 폴더에 .NET Standard 2.1 라이브러리를 만들고, 거기에 정의된
상수를 서버가 참조해서 콘솔에 출력. 모노레포 구조의 첫 검증.

**이 Phase가 중요한 이유**: 우리 헌법(CLAUDE.md)의 핵심 중 하나가
"shared/는 클라/서버 양쪽이 같은 어셈블리를 참조한다"인데, 이게
물리적으로 가능한지 일찍 검증해야 해요. .NET Standard 2.1은 Unity
2022 LTS와 호환되는 가장 안전한 선택이에요.

---

## ⏪ 사전 조건

- [x] Phase 01 완료 (솔루션 부트스트랩)

---

## 📝 작업 내용

- [ ] shared 라이브러리 프로젝트 생성:
      `dotnet new classlib -n Shared -o shared -f netstandard2.1`
- [ ] 솔루션에 추가:
      `dotnet sln add shared/Shared.csproj`
- [ ] 서버가 shared를 참조:
      `dotnet add server/GameServer reference shared/Shared.csproj`
- [ ] 테스트 프로젝트도 shared를 참조:
      `dotnet add server/GameServer.Tests reference shared/Shared.csproj`
- [ ] 기본 클래스 `Class1.cs` 삭제
- [ ] `shared/GameData/Constants.cs` 생성:
      ```csharp
      namespace Dawnholder.Shared.GameData;

      public static class Constants
      {
          /// <summary>서버 시뮬레이션 틱 레이트 (Hz)</summary>
          public const int ServerTickRate = 20;

          /// <summary>한 틱 길이 (밀리초)</summary>
          public const int ServerTickMs = 1000 / ServerTickRate;

          /// <summary>TCP 리스닝 포트 (개발 환경)</summary>
          public const int DefaultPort = 7777;

          /// <summary>최대 패킷 크기 (바이트). 이보다 크면 거부.</summary>
          public const int MaxPacketSize = 64 * 1024;
      }
      ```
- [ ] `shared/Protocol/ProtocolVersion.cs` 생성:
      ```csharp
      namespace Dawnholder.Shared.Protocol;

      public static class ProtocolVersion
      {
          /// <summary>
          /// 프로토콜 버전. 패킷 모양이 깨지는 변경 시 +1.
          /// 클라/서버 핸드셰이크에서 mismatch면 즉시 끊음.
          /// </summary>
          public const int Current = 1;
      }
      ```
- [ ] 서버 `Program.cs`를 업데이트해서 상수 출력:
      ```csharp
      using Dawnholder.Shared.GameData;
      using Dawnholder.Shared.Protocol;

      Console.WriteLine("Hello, Dawnholder!");
      Console.WriteLine($"Protocol version: {ProtocolVersion.Current}");
      Console.WriteLine($"Tick rate: {Constants.ServerTickRate} Hz ({Constants.ServerTickMs}ms/tick)");
      Console.WriteLine($"Will listen on port {Constants.DefaultPort}");
      Console.WriteLine("Press Enter to exit.");
      Console.ReadLine();
      ```
- [ ] 첫 단위 테스트 작성 (`server/GameServer.Tests/SmokeTests.cs`):
      ```csharp
      using Dawnholder.Shared.GameData;
      using Xunit;

      namespace GameServer.Tests;

      public class SmokeTests
      {
          [Fact]
          public void TickRate_Is20Hz()
          {
              // tick rate 변경은 ADR-004 위반. 이 테스트가 깨지면 ADR 갱신부터.
              Assert.Equal(20, Constants.ServerTickRate);
          }

          [Fact]
          public void TickMs_DerivesFromTickRate()
          {
              Assert.Equal(50, Constants.ServerTickMs);
          }
      }
      ```
- [ ] 커밋: `git commit -m "feat(shared): add Constants and ProtocolVersion"`

---

## ✅ 완료 조건

- [ ] `dotnet build` 통과
- [ ] `dotnet run --project server/GameServer` 실행 시 다음 4줄 출력:
      ```
      Hello, Dawnholder!
      Protocol version: 1
      Tick rate: 20 Hz (50ms/tick)
      Will listen on port 7777
      ```
- [ ] `dotnet test` 통과 (2 tests passed)

---

## 🧪 테스트

**자동 테스트:**
- `SmokeTests.TickRate_Is20Hz` — ADR-004 회귀 방어
- `SmokeTests.TickMs_DerivesFromTickRate` — 산술 검증

**수동 테스트:**
- `dotnet run`으로 서버 출력에서 shared의 상수가 잘 읽히는지 눈으로 확인

---

## 📚 학습 포인트

이번 Phase에서 처음 만나는 개념들:

1. **.NET Standard vs .NET (Core/8)**
   - .NET 8은 최신 런타임. .NET Standard 2.1은 "여러 런타임이 공통으로
     지원하는 API 표준". Unity 6.4 LTS가 .NET 10을 직접 지원 안 해서
     shared만 .NET Standard 2.1로 빌드. 서버는 .NET 8 그대로.
   - 즉 한 어셈블리(shared.dll)가 .NET 8 서버와 Unity Mono 양쪽에서
     로드되는 구조.

2. **네임스페이스 컨벤션**
   - `Dawnholder.Shared.GameData` 처럼 `회사/프로젝트.모듈.하위`.
   - 폴더 구조와 네임스페이스를 일치시키는 게 표준.

3. **`public const` vs `public static readonly`**
   - `const`는 컴파일 시 박힘. 호출자에 박제됨. 값 바꾸면 호출자도
     재컴파일 필요. 우리 상수는 자주 안 바뀌니 const로 OK.
   - `static readonly`는 런타임에 결정되는 값에 씀.

4. **xunit `[Fact]` vs `[Theory]`**
   - `[Fact]` = 인자 없는 테스트 메서드 1개.
   - `[Theory]` = 데이터 여러 개로 같은 로직 반복 테스트.

5. **테스트가 ADR 회귀 방어가 되는 패턴**
   - `TickRate_Is20Hz` 같은 테스트는 "성능 테스트"가 아니라
     "결정 회귀 방어"예요. 누가 무심코 30으로 바꾸면 테스트가
     "ADR-004 봐라"라고 멈춰주는 역할.

---

## ⚠️ 함정 / 주의사항

- **`netstandard2.1` 명시 필수**. 빠뜨리면 디폴트(.NET 8)로 만들어져서
  나중에 Unity가 못 읽어요.
- **using 문 누락**: shared 참조 추가 안 하고 `using Dawnholder.Shared...`
  쓰면 IDE는 빨간 줄, 빌드 시 에러. 참조부터 추가.
- **상수 너무 많이 넣지 말기**: 지금은 4개로 충분. 나중에 필요할 때
  추가. 미리 다 박아두면 안 쓰는 상수가 쌓여요.

---

## ➡️ 다음 Phase

**Phase 03: TCP 리스너 + Session 객체**
- 진짜 네트워크 코드 시작
- 담당 에이전트: `netcode`
- 패킷 framing은 아직 안 함 (Phase 04에서)
- 일단 "TCP 연결을 받고 Session으로 묶어 관리"까지

---

## 작업 로그
