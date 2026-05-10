# Phase 06: PacketGenerator(자체 PDL) 이주 + 핵심 함정 정정

> **상태**: pending
> **마일스톤**: M1 - Foundation (정비 후속, M1 완료 후 인프라 정합)
> **예상 소요**: 2~3시간
> **담당 에이전트**: 메인 세션 + `netcode` 서브에이전트
> **근거 ADR**: ADR-002 v2 (자체 PDL) + ADR-011 (ServerDev 코드 부분 채택)

---

## 🎯 목표

4월 ServerDev 레포(`C:\Users\bass1\바탕 화면\ServerDev\Dawnholder_Server\PacketGenerator`)에 박혀있는 PacketGenerator 4파일을 `99_Tools/PacketGenerator/`로 이주하고, **알려진 함정 정정 + .NET 10 호환 + PDL.xml = Ping/Pong 단일 소스** 까지. 생성기 실행 → `GenPackets.cs` / `ClientPacketManager.cs` / `ServerPacketManager.cs` 3개 출력이 *컴파일 가능한 코드*로 나오는지 검증.

**본 Phase 범위 한계** (Phase 단위 1~3h 헌법 준수):
- *생성기 자체가 작동*하는 데까지. 출력 코드는 *eyeball + 격리 컴파일*만 검증.
- **Y2 양쪽 정합 + Phase 05 PingPacket/PongPacket 코드 교체 + Unity 시연**은 **Phase 07로 분리**.
- BinaryPrimitives 정합(생성 코드 endianness 정합)도 Phase 07로.

---

## ⏪ 사전 조건

- [x] Phase 05 완료 + ★ M1 Foundation 도달
- [x] ADR-002 v2 (자체 PDL 채택) / ADR-011 (ServerDev 부분 채택) 통독
- [x] 4월 PacketGenerator 위치 + 핵심 4파일 통독 완료
- [ ] **이번 Phase의 핵심 통찰 인지**: *코드 생성기는 자기도 코드*. 4월에 *손으로 짠 코드*에 박혀있는 하드코딩 버그를 *발견 + 정정*하는 작업.

---

## 📝 작업 내용

### 1단계: 핵심 4파일 복사

대상 폴더: `99_Tools/PacketGenerator/`

복사 대상:
- `Program.cs` (XML reader, 메인 로직)
- `PacketFormat.cs` (코드 생성 템플릿)
- `PDL.xml` (단일 소스 — 본 Phase에서 통째 교체)
- `PacketGenerator.csproj`

**제외**: `ICON.ico` (포트폴리오 단순화 — 부수 자산 부담 줄이기), `bin/`, `obj/`, `Debug/`, `Release/`.

### 2단계: csproj 정합 (.NET 10)

원본 csproj는 `<ApplicationIcon>ICON.ico</ApplicationIcon>` + `<Content Include="ICON.ico" />` 박혀있음. 아이콘 제외했으니 그 항목들 제거.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Dawnholder.Tools.PacketGenerator</RootNamespace>
    <AssemblyName>Dawnholder.Tools.PacketGenerator</AssemblyName>
  </PropertyGroup>
</Project>
```

### 3단계: 하드코딩 버그 정정 (PacketFormat.cs L178)

원본 버그:
```csharp
byte[] buffer = BitConverter.GetBytes((ushort)PacketID.C_Chat); // ❌ 모든 패킷이 C_Chat ID로 송신됨
```

`{0}` 자리 표시자가 빠진 채 `C_Chat`이 하드코딩됨. 정정:
```csharp
byte[] buffer = BitConverter.GetBytes((ushort)PacketID.{0});
```

이 버그는 `#else` (NET_LEGACY) 분기에만 있어 *현 시점에서 발현 안 함* (.NET 10 + .NET Std 2.1은 `!NET_LEGACY`만 사용). 그러나 임시 우회로 미루지 않고 *발견 시점에 정정*이 안전망.

### 4단계: PDL.xml 통째 교체 = Ping/Pong만

원본 PDL.xml은 4월 게임용 패킷(`S_BroadcastEnterGame`, `C_Move` 등) 박혀있음. 우리 Phase 06 시점엔 *Ping/Pong*만 있어야 함.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<PDL>
    <!-- C_Xxx = Client→Server, S_Xxx = Server→Client (Program.cs 명명 규칙) -->

    <packet name="C_Ping">
        <long name="clientTimestampMs"/>
    </packet>

    <packet name="S_Pong">
        <long name="clientTimestampMs"/>
        <long name="serverTimestampMs"/>
    </packet>
</PDL>
```

⚠️ **명명 규칙**: 생성기가 `S_`/`s_` 접두사로 *클라이언트 receiver*인지 *서버 receiver*인지 구분 (Program.cs L93). 따라서:
- `C_Ping` = 클라가 보내는 패킷 = 서버 PacketManager에 등록
- `S_Pong` = 서버가 보내는 패킷 = 클라 PacketManager에 등록

Phase 05의 `PingPacket`/`PongPacket` 클래스는 이 규칙으로 자연스럽게 `C_Ping`/`S_Pong`로 흡수됨.

### 5단계: `#if NET_LEGACY` 분기 정리 (옵션)

PacketFormat.cs는 `#if !NET_LEGACY` / `#else` 양 분기를 둘 다 출력. 현재 우리 환경(.NET 10 / .NET Std 2.1)은 `!NET_LEGACY` 분기만 사용 → `#else` 분기는 *데드 코드*.

**판단**: 본 Phase에선 *원본 그대로 둠* (분기 정리는 Phase 07에서 BinaryPrimitives 정합 시 함께). 이유:
- 제거 시 변경 폭 큼. 본 Phase는 *작동 검증 우선*.
- `#else` 분기는 컴파일 시 무시되므로 *동작에 영향 없음*.

### 6단계: 솔루션 등록

- `Dawnholder.slnx`에 새 폴더 + 프로젝트 추가:
  ```xml
  <Folder Name="/99_Tools/">
    <Project Path="99_Tools/PacketGenerator/PacketGenerator.csproj" />
  </Folder>
  ```
- `dotnet sln Dawnholder.slnx list`로 6개 프로젝트 인식 확인 (GameServer / GameServer.Tests / Network / ClientNet / Shared / **PacketGenerator**).

### 7단계: 생성기 실행 + 출력 검증

```powershell
cd 99_Tools/PacketGenerator
dotnet run -- PDL.xml
```

⚠️ **함정 가드** (Program.cs):
- 끝에 `Console.ReadKey(true)` 박혀있음 → CI/자동화 시 멈춤. 본 Phase엔 *수동 실행*이라 그대로 둠. 향후 Phase 07+에서 `--no-wait` 인자 분기 추가 가능.
- `PDL_PATH = "../PDL.xml"` 하드코딩 → 작업 디렉토리에 따라 깨짐. 인자로 명시 권장(`-- PDL.xml`).

기대 출력 (3개 .cs):
- `GenPackets.cs` — `PacketID enum` + `IPacket` interface + `C_Ping` / `S_Pong` 클래스
- `ClientPacketManager.cs` — `S_Pong` 등록(클라가 받는 것)
- `ServerPacketManager.cs` — `C_Ping` 등록(서버가 받는 것)

검증:
- [ ] 3개 파일 정상 생성
- [ ] eyeball — `C_Ping` 클래스에 `public long clientTimestampMs;` 멤버 보이는지
- [ ] eyeball — `S_Pong` 클래스에 두 long 멤버
- [ ] eyeball — `(ushort)PacketID.C_Chat` 같은 하드코딩 잔재 없는지 (3단계 정정 검증)
- [ ] **격리 컴파일**: 출력 .cs를 임시 위치에 복사 + 미니 csproj 만들어서 컴파일. (또는 `dotnet build`가 아니라 `csc`로 한 번 격리)

> 본 Phase는 *생성 코드를 양쪽 프로젝트에 wire-up*하지 않음. 격리 컴파일까지가 검증의 한계. 양쪽 wire-up은 Phase 07.

### 8단계: commit + DONE.md 박제

- `feat(tools): PacketGenerator 이주 — 4월 ServerDev → 99_Tools/, 하드코딩 버그 정정 + PDL.xml = Ping/Pong`
- `-DONE.md` 박제 + CONTEXT/History 갱신.

---

## ✅ 완료 조건

- [ ] `99_Tools/PacketGenerator/` 4파일 (Program.cs / PacketFormat.cs / PDL.xml / PacketGenerator.csproj) 이주
- [ ] csproj에 `<ApplicationIcon>` / `<Content Include="ICON.ico"/>` 제거
- [ ] PacketFormat.cs L178 하드코딩 버그 정정 (`PacketID.C_Chat` → `PacketID.{0}`)
- [ ] PDL.xml 통째 교체 — `C_Ping` / `S_Pong` 두 개만
- [ ] Dawnholder.slnx에 PacketGenerator 등록 (총 6개 프로젝트)
- [ ] `dotnet build Dawnholder.slnx` — 6개 프로젝트 경고 0 / 오류 0
- [ ] `dotnet run --project 99_Tools/PacketGenerator -- PDL.xml` 실행 → 3개 .cs 정상 생성
- [ ] 출력 파일 eyeball 검증 + 격리 컴파일 통과

---

## 🧪 테스트

**자동 테스트**: 생성기 자체엔 안 만듦 (스크립트성 도구). 향후 PacketFormat 템플릿이 복잡해지면 snapshot test 도입 가능.

**수동 테스트**:
1. 위 7단계 실행 → 3개 .cs 생성
2. PDL.xml에 *임시* 패킷 한 개 추가 → 다시 실행 → 출력에 새 패킷 클래스 반영되는지 (생성기 동작 데모)
3. 추가했던 임시 패킷은 다시 제거 후 재실행 → 깨끗한 상태로 commit

---

## 📚 학습 포인트

### 1. 코드 생성기 패턴
- **단일 소스 (PDL.xml)** → 다중 출력 (server/client/shared 코드).
- 대안: T4 (Visual Studio 의존) / Roslyn Source Generator (컴파일 시 자동 실행) / 직접 string format (이번 안). 직접 string format은 *디버깅 단순*하지만 템플릿이 거대해지면 가독성 ↓.
- ASP.NET의 `dotnet ef migrations add`도 같은 패턴 (모델 → SQL 스크립트 생성).

### 2. 자기 코드의 버그 발견 의식
- 4월 본인이 작성한 코드의 하드코딩 버그(L178)를 *5월 본인*이 발견 + 정정. *이주는 점검 기회*.
- 학습 일지 후보: "임시 우회 → 미루기" 패턴이 어떻게 잠복 버그가 되는가.

### 3. 명명 규칙으로 책임 분리 (`C_` / `S_` 접두사)
- 생성기가 *접두사로 클라/서버 dispatch table을 자동 분리*. 코드 한 줄 수정 없이 새 패킷 추가 시 적절한 manager에 자동 등록.
- 단점: 접두사 강제 → 명명 자유도 ↓. 그러나 *프로토콜은 일관성이 더 중요*.

### 4. 임시 코드 → 생성 코드 교체의 점진성
- Phase 05 PingPacket/PongPacket이 *임시 BitConverter*. Phase 06엔 *생성기 자체 작동*만 검증. Phase 07에서 *교체*. 한 번에 안 갈아엎고 *각 단계가 독립 검증 가능*하게 분리.

---

## ⚠️ 함정 / 주의사항

- **`Console.ReadKey` 자동화 차단**: Program.cs 끝에 사용자 입력 대기. 수동 실행은 OK, CI엔 부적합. Phase 07+에서 `--no-wait` 인자 분기 추가 검토.
- **PDL_PATH 작업 디렉토리 의존**: 인자 없으면 `../PDL.xml` 가정. `dotnet run --project 99_Tools/PacketGenerator -- PDL.xml`처럼 *명시 인자* 권장.
- **출력 파일 위치**: Program.cs L46-48이 `File.WriteAllText("GenPackets.cs", ...)` 등으로 *현재 작업 디렉토리에 출력*. 즉 `dotnet run` 호출 위치에 출력됨. Phase 07에서 정합 시 `99_Tools/PacketGenerator/output/` 등 명시적 경로로 변경 검토.
- **NET_LEGACY 매크로**: 정의된 곳 없으니 항상 false. `#if !NET_LEGACY` 분기만 사용됨. 그러나 본 Phase에서 정리 X (Phase 07 BinaryPrimitives 정합 시 함께).
- **하드코딩 정정의 *왜 이 위치만 정정*인가**: PacketFormat.cs를 grep해보니 `C_Chat`이 L178 한 곳만. 정정 후 다른 잔재 없는지 grep 재확인.
- **생성기 출력의 `using ServerCore;`**: 생성 코드가 `SendBufferHelper` / `PacketSession`을 ServerCore namespace에서 가져옴. 양쪽(서버 + 클라) 정합은 Phase 07. 본 Phase에선 *격리 컴파일* 불가능할 수 있음 — 격리 시점에 ServerCore stub 필요. 필요 시 Phase 06 검증은 *eyeball만으로 충분*하다고 인정 후 Phase 07 진입.

---

## ➡️ 다음 Phase

**Phase 07: 생성 코드 양쪽 정합 + Phase 05 코드 교체 + 시연**
- Y2 정합 결정: ① Shared에 SendBufferHelper 두기(코드 중복 0) ② 생성기가 양쪽에 별도 GenPackets.cs 출력 ③ 다른 방식
- BinaryPrimitives 정합 (PacketFormat.cs 템플릿 수정)
- Phase 05 `PingPacket.cs` / `PongPacket.cs` (수동 작성) 삭제 → 생성 코드 사용
- Phase 05 GameSession / UnityClientSession이 새 `C_Ping` / `S_Pong` 인지하도록 dispatch 정합
- Unity Play → 1초마다 RTT 로그 (Phase 05 시연 그대로 재현, 단 *생성 코드*로)

> Phase 07 후 새 패킷 추가는 `/new-packet <C2S|S2C> <name>` 슬래시 커맨드로 자동화 가능 (이미 정의됨).

---

## 작업 로그

> Phase 진행하면서 발견된 이슈, 결정, 메모 누적.
> 끝나면 `06-pdl-migration-DONE.md`로 박제.
