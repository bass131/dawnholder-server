# 개념: `.NET Standard 2.1` + `Embedded PDB` — 학습 일지

> **작성일**: 2026-05-09
> **등장 Phase**: Phase 01 — 솔루션 부트스트랩 + DLL 빌드 파이프라인
> **이해도 자가평가**: 둘 다 🟡 대략 이해 (메커니즘 깊이는 학습 큐)
> **블로그 글로 발전?**: 미정 (자가평가 🟡 풀고 나서)

> **묶음 이유**: 두 개념이 `shared` 라이브러리 빌드 파이프라인이라는 같은 맥락에서 등장. `.NET Standard 2.1`이 "양쪽이 같은 어셈블리를 인식할 수 있게" 하고, `Embedded PDB`가 "그 어셈블리를 디버깅도 가능하게" 만든다 — 두 개가 합쳐져야 ADR-010이 노린 "헌법 #4의 물리적 강제"가 완성됨.

---

## 🎯 한 줄 정의

### `.NET Standard 2.1`
여러 .NET 런타임(Mono, .NET Core/.NET 5+ 등) 사이의 **API 호환을 약속한 사양**으로, 우리 프로젝트에선 shared 라이브러리를 **Unity의 Mono와 .NET 10 서버 양쪽에서 인식 가능**하게 하는 **연결 다리** 역할.

### `Embedded PDB` (표면 수준)
디버깅 정보 파일(PDB)을 별도 파일로 두지 않고 **.dll 안에 통째 임베드**해서, 디버거가 .dll 하나만 봐도 원본 코드 추적이 가능한 형식.

> 💡 학습 큐: `EmbedAllSources`까지 같이 박았을 때의 의미(.cs 원본 자체를 임베드 → F12 ReadOnly 표시)는 아직 표면만 본 영역. 메커니즘은 더 학습 필요.

---

## 🌱 처음 만난 맥락

**이전 학습 이력 (DX9 기반 클라이언트 포트폴리오)**:
- 렌더러 + 엔진 코어를 DLL로 패킹해 클라에 임포팅하는 패턴 적용
- 그때 이해: **"핵심 부분을 안전하게 감싸야 한다"** 는 단순한 맥락만으로 깨우침

**이번 Phase에서의 진화**:

같은 "DLL 패킹" 패턴이지만 한 단계 진화한 형태로 만남:

| | 이전 (DX9) | 이번 (.NET) |
|---|---|---|
| 안전성 (핵심 코드 보호) | ✓ | ✓ |
| 호환 사양 (.NET Standard 2.1) | — | ✓ |
| 디버깅 풍부함 (Embedded PDB + EmbedAllSources) | — | ✓ |
| 거버넌스 강제 (헌법 #4를 빌드 시스템으로) | — | ✓ |

→ 같은 도구(DLL 패킹)가 환경(C++ → .NET)에 따라 **한 csproj 두세 줄에 4가지 가치를 다 박을 수 있는 형태**로 진화. 이게 면접에서 "DLL 패킹 경험 있어요?" 질문 받을 때 강한 답.

---

## 🤔 비유로 이해하기

### `.NET Standard 2.1` — "법/규율"
- **비유 핵심**: 모두가 따르기로 한 약속. 입법자 = MS/.NET Foundation.
- **비유의 한계**: 법은 어길 수 있고 도덕 의지에 맡기지만, .NET Standard는 어기면 **컴파일 자체가 안 됨** = 빌드 타임에 시스템이 자동 강제. **법보다 더 강한 형태**.

### `Embedded PDB` — "설계도 도면" (의도: .dll 안에 내부 구조까지 박힘)
- **비유 핵심**: 외부에서 봐도 내부 구조가 다 보이는 형태.
- **비유의 한계**: 설계도는 "사전 청사진"이고 PDB는 "사후 디버그 정보"라 방향이 약간 다름. 더 정확한 비유는 **"조립 설명서 + 부품 이름표가 같이 포장된 IKEA 가구"** 또는 **"X-ray가 같이 붙은 도자기"**. 단 `EmbedAllSources`로 .cs 원본까지 들어가는 부분에선 "설계도"도 부분적으로 맞음.

---

## 🔬 정확한 설명

### `.NET Standard 2.1`

#### 표면 수준
"여러 .NET 런타임이 공통으로 지원할 API 집합의 사양." 한 줄 정의에서 표현된 그대로.

#### 한 단계 깊이 — 🔴 학습 큐
> "컴파일러가 어떻게 .NET Standard 2.1 호환을 자동 검증하는가?"의 메커니즘은 아직 모름. 다음 학습으로:
> - 컴파일러가 `<TargetFramework>netstandard2.1</TargetFramework>` 보고 무엇을 다르게 하는가
> - 해당 사양에 없는 API를 쓰려고 하면 어떻게 빌드 에러가 발생하는가
> - .NET Standard reference assembly 메커니즘

### `Embedded PDB`

#### 표면 수준
".dll 안에 디버그 정보를 통째 임베드. 디버거가 .dll 하나만 봐도 원본 코드 추적 가능."

#### 한 단계 깊이 — 🔴 학습 큐
> "PDB가 .dll에 어떻게 들어가는가? IDE는 그걸 어떻게 인식해서 F12 점프하는가?"의 메커니즘은 아직 모름. 다음 학습으로:
> - PE 파일 형식의 디버그 디렉터리
> - PDB 안의 IL ↔ .cs 줄 매핑 구조
> - `EmbedAllSources`가 추가로 어떤 섹션에 .cs 원본을 박는가
> - IDE/디버거의 PDB 로드 절차

---

## 💻 우리 프로젝트에서의 실제

### `shared/Shared.csproj` — 두 개념이 같이 박힘

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.1</TargetFramework>     <!-- ① -->
  <Nullable>enable</Nullable>
  <LangVersion>latest</LangVersion>
  <DebugType>embedded</DebugType>                        <!-- ② -->
  <EmbedAllSources>true</EmbedAllSources>                <!-- ③ -->
</PropertyGroup>

<Target Name="CopyToUnityPlugins" AfterTargets="Build">
  <ItemGroup>
    <_Outputs Include="$(TargetDir)$(TargetName).dll" />
  </ItemGroup>
  <MakeDir Directories="$(MSBuildThisFileDirectory)..\client\Assets\Plugins\Shared\" />
  <Copy SourceFiles="@(_Outputs)"
        DestinationFolder="$(MSBuildThisFileDirectory)..\client\Assets\Plugins\Shared\"
        SkipUnchangedFiles="true" />
</Target>
```

- **①** `.NET Standard 2.1` 타겟 — Unity의 Mono 런타임이 인식 가능
- **②** `embedded` — .pdb를 별도 파일로 두지 않고 .dll 안에 임베드
- **③** `EmbedAllSources` — .cs 원본까지 .dll 안에 임베드 (F12 시 ReadOnly로 표시 가능)

**왜 두 개를 같이 박았나**: ①만 있으면 Unity가 dll을 인식은 하지만 디버깅 경험이 디컴파일된 코드 수준. ②③까지 같이 박아야 클라이언트 입장에서 "원본 .cs를 받지 않아도 .cs와 같은 디버깅 경험" 가능. 즉 헌법 #4(클라가 shared 코드 수정 금지)를 위해 .cs를 클라에 안 주면서도, 디버깅 경험은 손해 보지 않음.

---

## ⚠️ 흔한 오해 / 함정

### 본인이 이번에 보인 헷갈림
- **".NET Framework 2.1"이라는 미존재 조합** — `.NET Framework`(옛 Windows 전용 런타임)와 `.NET Standard`(호환 사양)를 혼동. Framework 2.1은 존재하지 않는 버전 조합.
- **"프로젝트 사이의 호환"** — 실제론 "런타임/변종 사이의 호환". 프로젝트(csproj 단위)는 이 약속을 따르는 사용자.
- **"런타임 흐름 추적"** — 실제론 "디버깅 시 원본 코드 추적". PDB는 런타임 동작과 무관 (런타임 자체는 PDB 없어도 잘 돌아감).

### 일반 학습자가 헷갈릴 만한 것
- "`.NET Standard` = 런타임?" → ❌ 사양/약속이지 실행되는 게 아님.
- "PDB는 별도 파일이어야 한다?" → ❌ embedded 가능.
- "Embedded PDB는 보안 강화 도구?" → 부분적. 본질은 "디버깅 풍부함 + 배포 단순성". 보안(클라가 shared 수정 못 함)은 부수 효과.

---

## 🎤 면접 시뮬레이션

**Q1: "왜 `shared`를 `.NET Standard 2.1`로 빌드해요? `.NET 10`이 더 최신이잖아요?"**
A: Unity의 Mono 런타임은 .NET Standard 2.1까지 인식한다. 서버는 .NET 10 최신을 쓰지만, 양쪽이 공통으로 인식 가능한 .NET Standard 2.1을 shared의 타겟으로 잡으면 한 어셈블리로 양쪽이 같은 코드를 공유할 수 있다.

**Q2: "PDB를 임베드하면 뭐가 좋아요? 별도 파일과 비교해서요."**
A: 🔴 아직 답 못 함. 학습 큐 — "PDB의 메커니즘 + Embedded vs Portable PDB의 trade-off"는 추후 학습.
   현재 표면 인지: "별도 파일로 두면 .dll과 .pdb를 따로 배포해야 하지만, embedded면 .dll 하나만 배포하면 됨" 정도까지만.

**Q3: "embedded PDB + EmbedAllSources를 같이 박으면 뭐가 가능해지나요?"**
A: 클라이언트가 .dll 한 개만 받아도, IDE에서 F12로 점프 시 원본 .cs 코드(주석 포함)를 ReadOnly로 볼 수 있다. 즉 "원본 .cs 파일을 클라에 안 줘도 디버깅은 풍부하게" 가능. 우리 프로젝트에선 이게 헌법 #4(공유 코드 규율 — 클라가 shared 수정 금지)를 빌드 시스템으로 자동 강제하는 메커니즘.

---

## 🔗 더 깊이 가고 싶다면

**다음에 볼 자료**:
- [.NET Standard 공식 문서](https://learn.microsoft.com/dotnet/standard/net-standard) — reference assembly 메커니즘 설명
- Microsoft Docs의 "Diagnosing with PDBs" 섹션
- PE 파일 형식 — Mads Torgersen 또는 Sasha Goldshtein 글

**연관된 다른 개념**:
- MSBuild Target / Item / Property 시스템 (이번 csproj의 `<Target>` 블록)
- Unity의 Mono 런타임 vs IL2CPP
- .NET Compatibility Pack
- Reference Assembly (.NET Standard 메커니즘의 핵심)
- Source Link (Embedded PDB의 사촌 — GitHub 호스팅 .cs를 디버깅 시 가져옴)

---

## 📝 블로그 글 발전 메모

자가평가 🟡인 상태에선 글로 발전 보류. 학습 큐 한 단계 더 풀고 나서 다시 검토.

발전 시 추가할 자료:
- [ ] 다이어그램: shared.dll이 .NET 10 서버와 Unity Mono 양쪽에 인식되는 구조
- [ ] csproj 두 줄로 헌법을 빌드 시스템에 박기 — 디자인 패턴 글로 발전 가능
- [ ] DX9 시절 DLL 패킹 vs .NET 시절 DLL 패킹의 진화 — 이전 경험 활용 글
- [ ] 제목 후보: "csproj 두 줄로 클라이언트가 코드를 못 만지게 만들기"

---

## 작성 메모

- ✅ 한 줄 정의 (둘 다)
- ✅ 처음 만난 맥락 (DX9 → .NET 진화 형태로 박음)
- ✅ 비유 + 한계 (둘 다)
- ✅ 표면 수준 설명 (둘 다)
- 🔴 한 단계 깊이 (둘 다 학습 큐)
- ✅ 흔한 오해 (본인이 인터뷰에서 보인 헷갈림 박음)
- ✅ 우리 코드 사용 (shared/Shared.csproj)
- ⚠️ 면접 시뮬레이션 Q2 미답 — 학습 큐
- ✅ 자가평가 🟡 둘 다
