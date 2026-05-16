# Phase 01 — 솔루션 부트스트랩 + DLL 빌드 파이프라인 완료 박제

**완료일**: 2026-05-09
**커밋**: `2411ae0` (코드) + `ed13936` (학습 일지) + `1ed6a43` (개념 일지)
**소요 시간**: 1.5~2시간 + 후속 학습 일지

> **소급 박제** — 본 파일은 Phase 02 완료 후 박제 정책이 정해지면서 (`-DONE.md` 페어 도입) **소급 작성**되었습니다. 따라서 5단계 보고는 git history + Phase 정의 작업 로그 + 노션 세션 기록을 종합해 재구성한 것이며, 작업 시점 실시간 출력 그대로는 아닙니다.

---

## 5단계 보고 (재구성)

### 🎯 무엇을 만들었나
.NET 10 솔루션 한 개 + `shared/Shared` 라이브러리(.NET Standard 2.1) + Unity 6.4 LTS 빈 클라이언트 프로젝트를 한 묶음으로 부트스트랩하고, **빌드 시 `Shared.dll`이 자동으로 `client/Assets/Plugins/Shared/`에 복사되어 Unity F12로 원본 한국어 주석까지 보이는 상태**까지 검증.

### 🤔 왜 필요한가
이후 모든 Phase가 이 토대 위에 쌓임. 특히 ADR-010(DLL + Embedded PDB로 코드 공유 강제)이 *말로만* 결정된 상태였고 — 진짜로 Unity가 인식하고 F12 디버깅이 동작하는지가 가장 큰 위험 지점이었음. 첫 Phase에서 그 위험을 먼저 만나버리는 게 목적.

### 🛠️ 어떻게 만들었나
- **`global.json`으로 .NET 10 SDK 핀** — 팀 작업 시 SDK 버전 분산 방지.
- **`shared/`는 `netstandard2.1`, `server/`는 `net10.0`** — Unity Mono/IL2CPP가 인식하는 공통 API 사양이 .NET Standard 2.1이기 때문. 서버는 최신 런타임 활용.
- **MSBuild `<Target>` post-build로 자동 복사** — 수동 복사 = 까먹음 = 동기화 지옥. csproj 한 곳에 박아 빌드 시마다 강제.
- 대안 **단일 모노 솔루션 + Unity 자체 컴파일**(즉 .cs 직접 공유)도 고려했지만 — 헌법 #4 ("복사-붙여넣기 금지")의 *물리적 강제*가 안 됨. DLL 모델은 같은 .cs를 양쪽에서 컴파일할 수 없게 만드는 것이 핵심.

### 🧪 테스트 결과
- ✅ `dotnet build` 무경고 통과
- ✅ `dotnet run --project server/GameServer` → 콘솔 출력 정상
- ✅ Unity 에디터에서 `using Shared.GameData;` IntelliSense 동작
- ✅ F12 → `Constants.ServerTickRate` 정의로 점프 → **원본 .cs 코드 + 한국어 주석 ReadOnly 표시** (디컴파일 X)
- ✅ `git check-ignore`로 `.dll`이 git에 안 들어감 검증

### ➡️ 다음 스텝
- Phase 02: ServerCore 7파일 정착 (당시 결정 = `98_Shared/Net/`로 마이그 → 후에 시나리오 변경되어 `02_Server/Network/`로 정착)
- 학습 일지 작성 (`/journal:phase`) → 후에 `ed13936`로 박제됨

---

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **`global.json` 추가** vs 무시 → 추가. 이유: `dotnet --list-sdks`로 보면 머신에 여러 SDK 깔려있을 수 있음. SDK 버전 일관성은 일찍 박을수록 좋음.
- **shared 타겟 = `netstandard2.1`** vs `net10.0` → `netstandard2.1` (강제). 이유: ADR-010 + Unity 호환. `net10.0`로 만들면 Unity가 인식 못 함.
- **post-build 복사 = MSBuild Target** vs npm 스타일 외부 스크립트 → MSBuild Target. 이유: csproj 안에 박혀있어야 빌드와 분리 안 됨, 누구나 `dotnet build`만 하면 동작.
- **PDB 모드 = `embedded` + `EmbedAllSources=true`** → 실측에서 `.pdb` 파일이 *별도로 안 생김*을 확인. 모든 게 `.dll`에 통째 들어감 → 복사 대상도 `.dll` 하나로 충분.

---

## 막혔던 지점

1. **CS8400 — file-scoped namespace** (가장 큰 발견)
   - 증상: `dotnet run` 시 `error CS8400: '파일 범위 네임스페이스' 기능은 C# 8.0에서 사용할 수 없습니다`.
   - 원인: `.NET Standard 2.1`의 default C# 언어 버전은 **8.0**. `namespace Foo;` 형식은 C# 10+.
   - 해결: `shared/Shared.csproj`에 `<LangVersion>latest</LangVersion>` 한 줄 추가.
   - **핵심 통찰**: ".NET Standard 2.1 = API 호환 사양"이지 "C# 컴파일러 사양"이 아님. 컴파일러는 .NET 10 SDK가 제공 → 최신 C# 문법으로 .NET Standard 2.1 라이브러리 빌드 가능.

2. **Embedded PDB → 별도 `.pdb` 파일 부재**
   - `embedded` + `EmbedAllSources=true`이면 디버그 정보 + 원본 .cs 전부가 `.dll` 안. `.pdb` 자체가 안 생성됨.
   - Phase 본문에 처음 적었던 ".dll + .pdb 복사"는 부정확 — `.dll` 하나로 충분. csproj `<Target>`이 `.pdb`도 명시했지만 missing 시 Copy가 silently skip해 실해 없음.

3. **`.gitignore` 깊이 패턴 함정**
   - 1차 셋업의 `.gitignore`는 `client/Assets/Plugins/*.dll` (한 단계만 매칭).
   - 실제 경로는 `Plugins/Shared/Shared.dll` (한 단계 깊이) → 매칭 실패 → 빌드 산출물이 git에 들어갈 뻔.
   - 수정: `client/Assets/Plugins/**/*.dll` (재귀 매칭).
   - **검증 도구**: `git check-ignore -v <path>`로 어떤 패턴이 매칭하는지 확인.

4. **Unity Hub의 `client/` 폴더 점유**
   - Unity Hub는 빈 폴더에만 새 프로젝트 생성. 기존 `client/CLAUDE.md` 등이 있으면 거부 또는 덮어쓰기.
   - 처리: `client/CLAUDE.md` 임시 root 백업 → `client/` 통째 삭제 → Unity 신규 프로젝트 생성 → CLAUDE.md 복원 → `dotnet build` 재실행으로 Shared.dll 자동 복사.

5. **`.slnx` 형식 등장** (함정 아닌 발견)
   - .NET 10 SDK가 만든 솔루션은 옛 `.sln`이 아니라 XML 기반 `.slnx`. 호환 정상 동작. `dotnet sln add/list`도 그대로.

---

## 학습 일지 후보 키워드

- `.NET Standard 2.1` (이미 `1ed6a43`로 일지 박힘)
- `Embedded PDB + EmbedAllSources` (이미 일지 박힘)
- `MSBuild Target` (post-build 자동화 패턴)
- `Unity Plugins/ 폴더 규약` (어떻게 .dll이 .cs와 동등하게 인식되나)
- `global.json` + SDK 버전 핀 전략

---

## 후속 박제

- Phase 학습 일지: `00_Document/learning-journal/M1-foundation/01-solution-bootstrap.md` (commit `ed13936`)
- 개념 일지: `.NET Standard 2.1 + Embedded PDB` (commit `1ed6a43`)
- 노션 협업 히스토리 DB에도 STAR 박제됨
