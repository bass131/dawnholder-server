---
owner: youngho
milestone: M4.1
phase: 04
title: Build Artifact Hygiene (P0-5 — Shared.dll/ProjectSettings dirty 봉합, hash 비교)
status: pending
grade: 보통
risk: low
estimated: 1~2h
domain: harness+infra
---

# Phase 04: Build Artifact Hygiene (P0-5)

> **상태**: pending
> **마일스톤**: M4.1
> **등급**: 보통 (1 도메인 harness / ~30~50줄 / 가역적)
> **담당**: 메인 직접 (영호) — build script 영역, SubAgent 위임보다 직접이 정합
> **사용자 보정 2 (2026-05-23)**: **`.gitignore` 옵션 A 박지 X**. 옛 Shared.dll 미commit 사고 학습과 충돌 위험. 기본 추천 = **hash 비교 / SkipUnchangedFiles / deterministic copy** 봉합. 목표 = "진짜 Shared 변경이면 dirty OK, 같은 소스 재빌드면 dirty X".

---

## 🎯 목표

**P0-5 (dotnet build/test가 Unity Shared.dll dirty 만드는 빌드 산출물) 봉합**. 매 빌드마다 `Shared.dll` + `ProjectSettings.asset` 부산물 자동 누적 = git diff 노이즈 + commit 시 의도 X 박힘 위험.

본 Phase가 끝나면 = (a) `dotnet build`/`test` 후 5회 연속 `git status -s | grep -E "(Shared.dll|ProjectSettings)"` 빈 출력, (b) Shared.cs *실제 변경* 시 dirty OK 회귀 검증, (c) 옛 Shared.dll 미commit 사고 학습 정신 보존 (배포 자산은 그대로 git 추적).

---

## ⏪ 사전 조건

- [x] Phase 03 (ClientNet Trust Boundary Symmetry) 마감 — 잦은 빌드 영역
- [x] 옛 학습 인지 — CHANGELOG `2026-05-17` Shared.dll 미commit 사고 (정유현 pull 사고 트라우마)
- [x] `Shared.csproj` PostBuild target (`CopyToUnityPlugins`) 현재 박힌 위치 확인

---

## 📝 작업 내용

### 1단계: 진단 + 원인 파악

- [ ] `dotnet build Dawnholder.slnx` 1회 실행 → `git status` 박힘 확인 (재현)
- [ ] `Shared.csproj` PostBuild target 코드 점검 — `<Copy SourceFiles="$(TargetPath)" DestinationFiles="$(UnityPluginsPath)\Shared.dll" />` 같은 분기 박힌 위치
- [ ] 매 빌드마다 `Shared.dll` 타임스탬프 갱신 → Unity 측 .meta 갱신 → ProjectSettings 변경 박힘 가능성 점검
- [ ] **결과 박음**: 본 1단계 산출물 = 원인 파악 메모 (PostBuild target 코드 vs MSBuild incremental 정신)

### 2단계: 봉합 가닥 결정 (사용자 보정 2 정합)

- [ ] **옵션 A** (사용자 비추천): `Shared.dll` `.gitignore` 박음 → 옛 미commit 사고 학습과 충돌. **박지 X**.
- [ ] **옵션 B** (사용자 권장): MSBuild `SkipUnchangedFiles="true"` 박음 — Copy task 자체에 `SkipUnchangedFiles` 옵션 박으면 source/dest 타임스탬프 비교 후 동일하면 skip. 단 *.dll 자체 재생성이 매번 박힘* 시 본 옵션도 한계.
- [ ] **옵션 C** (사용자 권장 + B 보강): hash 비교 PostBuild — 새 `Shared.dll` 빌드 후 *기존 Unity Plugins/Shared.dll와 hash 비교* → 동일하면 copy skip, 다르면 copy + Unity .meta 갱신 트리거.
- [ ] **결정 가닥** = 옵션 C 권장 (deterministic copy + 실제 변경만 dirty). MSBuild 안에 PowerShell 또는 .NET tool 호출 박음.

### 3단계: deterministic build 봉합

- [ ] `Shared.csproj` 옵션 추가:
  - `<Deterministic>true</Deterministic>` (이미 박힘 가능성 ↑, 점검 후 박음)
  - `<PathMap>` 설정 — 절대 경로 → 상대 경로 변환 (빌드 머신 종속 hash 차이 회피)
- [ ] PostBuild target에 hash 비교 분기 박음:
  ```xml
  <Target Name="CopyToUnityPluginsIfChanged" AfterTargets="Build">
    <PropertyGroup>
      <UnityPluginPath>$(MSBuildProjectDirectory)\..\03_Client\Assets\Plugins\Shared\Shared.dll</UnityPluginPath>
    </PropertyGroup>
    <Copy SourceFiles="$(TargetPath)"
          DestinationFiles="$(UnityPluginPath)"
          SkipUnchangedFiles="true"
          OverwriteReadOnlyFiles="false" />
  </Target>
  ```
  (또는 hash 비교 task PowerShell exec 분기)

### 4단계: ProjectSettings.asset 부수 진단

- [ ] git status에 `ProjectSettings.asset` modified 자주 박히는 원인 = Unity 측 cloud 라인 자동 갱신 (이미 hook 봉합 박힘) 또는 빌드 시점 Unity Editor 자동 변경
- [ ] 본 Phase scope 박정 = ProjectSettings 봉합은 옛 hook 정신 그대로 (Phase 04는 *Shared.dll 중심*, ProjectSettings는 별 시점 또는 본 Phase 마지막에 점검만)

### 5단계: 검증

- [ ] `dotnet build Dawnholder.slnx` 1회 → `git status` clean (Shared.dll 미박힘 확인)
- [ ] `dotnet build Dawnholder.slnx` 4회 연속 → `git status` 5회 연속 clean (회귀 검증)
- [ ] **Shared 실제 변경 회귀**: `98_Shared/GameData/Constants.cs` 한 줄 추가 → `dotnet build` → `git status`에 `Shared.dll` modified 박힘 확인 (정상 동작)
- [ ] 변경 되돌리기 → `dotnet build` → `git status` clean (회귀 안전)
- [ ] `dotnet test` 같은 검증 5회 연속

### 6단계: 문서 정합

- [ ] `02_Server/CLAUDE.md` 또는 `98_Shared/CLAUDE.md`에 "Shared.dll 부산물 정신" 한 문단 박음 — "Shared 실제 변경 시 dirty OK, 같은 소스 재빌드 시 dirty X. PostBuild target SkipUnchangedFiles 정신."

---

## ✅ 완료 조건

- [ ] `Shared.csproj` PostBuild target `SkipUnchangedFiles="true"` 또는 hash 비교 분기 박힘
- [ ] `dotnet build Dawnholder.slnx` 5회 연속 후 `git status -s | grep -E "(Shared.dll|ProjectSettings.asset)" | grep -v cloud` = 빈 출력 (clean)
- [ ] `dotnet test` 5회 연속 후 같은 점검 통과
- [ ] Shared 실제 변경 회귀 = dirty 박힘 → 되돌리기 → clean 회귀 통과
- [ ] `98_Shared/CLAUDE.md` 또는 `02_Server/CLAUDE.md` 본 정신 한 문단 박음
- [ ] 본 Phase 보통 등급 = -DONE.md 없음, work-pin + commit message 충분

---

## 🧪 테스트

**자동**:
- `dotnet build` + `dotnet test` 5회 연속 후 git status clean 검증 (수동 스크립트 또는 본인 직접)
- 회귀 안전 = Shared.cs 한 줄 변경 → 빌드 → dirty 박힘 → 되돌리기 → 빌드 → clean

**수동**:
- 본인 머신에서 5회 연속 빌드 + git status 박힘
- 정유현 머신 또는 별 머신에서 회귀 확인 (다음 pull 시점에 본인 알아채는 가닥)

---

## 📚 학습 포인트

- **deterministic build 정신** — `<Deterministic>true</Deterministic>` + `<PathMap>` = 같은 소스 + 다른 머신 빌드 = 같은 hash 보장. CI/CD 영역 + reproducible build 정합 (한국 게임 회사 백엔드 어필).
- **MSBuild incremental build** — `SkipUnchangedFiles="true"` = MSBuild가 source/dest 타임스탬프 비교 후 동일하면 skip. 빌드 캐시 정신 정합.
- **hash 비교 vs 타임스탬프 trade-off** — 타임스탬프 = 빠름, 단 같은 소스 재컴파일 시 timestamp 갱신 가능 (특히 .dll). hash = 정확, 단 hash 계산 비용 ↑. 본 Phase = `SkipUnchangedFiles` 우선 (옵션 B), 실패 시 hash 비교 fallback (옵션 C).
- **옛 사고 학습 정합 (보정 2 정합)** — `.gitignore` 옵션 A = 옛 Shared.dll 미commit 사고 (정유현 pull 사고 트라우마)와 충돌 가능성. 본 Phase 정신 = "*같은 소스 재빌드만* dirty X, *진짜 변경*은 dirty OK". 옛 트라우마와 새 봉합이 *충돌 X 정합*.

---

## ⚠️ 함정 / 주의사항

- **`.gitignore` 옵션 함정 (사용자 보정 2)** — 옛 Shared.dll 미commit 사고 학습 정신 잃음 위험. `.gitignore` 박지 X 의무. 본 Phase 사후 검토 시 본 함정 발견 시 즉시 되돌림.
- **PathMap 설정 누락 함정** — `<PathMap>` 없으면 본인 머신 (`C:\Dev\ClaudeDev\`) vs CI 머신 (예: `/home/runner/work/...`) 절대 경로 hash에 박힘 → 같은 소스 다른 hash 생성. CI 환경 영역 (M5+) 대비 박음.
- **PostBuild target 캐시 함정** — MSBuild가 PostBuild target 자체를 incremental 안 박을 가능성. `BeforeTargets="..."` 박는 게 정합한지 점검 의무.
- **ProjectSettings.asset 부수 사고 함정** — 본 Phase scope = Shared.dll 중심. ProjectSettings는 옛 hook 정신 그대로 (cloud 라인 자동 unstage). 본 Phase에서 ProjectSettings 봉합 시도 시 scope creep.

---

## ➡️ 다음 Phase

- **Phase 05 (Formulas.cs + PlayerStats 진짜 반영)** — P0-3 + P1. P0 베이스 풀세트 봉합 (Phase 02·03·04) 후 정밀도 진입.

---

## 📋 박제 (완료 후)

- 보통 등급 = -DONE.md 없음, work-pin + commit message 충분
- 단, 옵션 C (hash 비교) 박힘 시 별 학습 가치 ↑ = 작은 별 박제 권유 (`98_Shared/CLAUDE.md` 또는 `learning-journal/` 후보)

---

## 작업 로그

- 2026-05-23: Phase 정의 박힘 (M4.1 재구성 옵션 A' GO 시점). 사용자 보정 2 (`.gitignore` 옵션 A 박지 X, hash 비교 권장) 흡수.
