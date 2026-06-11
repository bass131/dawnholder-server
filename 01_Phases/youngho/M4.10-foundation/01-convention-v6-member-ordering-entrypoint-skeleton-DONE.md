---
owner: youngho
phase: 01
status: done
grade: 복잡
summary: CODE_CONVENTION v5→v6(DRY·책임헤더·멤버정렬·진입점 4보강) + 부록A 실측갱신(GameMap 졸업) + StyleCop.Analyzers 도입(SA1201/1202 warning) + ENTRY_POINTS.md 골격. 코드 0, 문서·빌드설정만.
completed: 2026-06-11
---

# Phase 01 완료 — 컨벤션 v6 확정 + StyleCop 멤버정렬 + 진입점맵 골격

> M4.10(코드 기반 정비)의 선행 Phase. 02~05가 올라탈 "측정 기준"을 박았다. 코드(.cs) 변경 0 — 문서 + 빌드설정만.

---

## TL;DR

`CODE_CONVENTION.md`를 **v5 → v6**으로 올리고(측정 가능한 4도구 추가), 멤버 정렬을 **StyleCop.Analyzers로 빌드 경고 강제**하고, `ENTRY_POINTS.md` 골격을 만들었다.

**산출물 (코드 .cs 0건)**:
- `CODE_CONVENTION.md` v6 — §2.5 DRY / §6.5 클래스 책임헤더 / §7.1 멤버정렬 / §7.2 진입점 4보강 + 부록 A 실측 갱신(GameMap 졸업, ClientPacketHandlers 909 미실행, 중복 7건).
- `ENTRY_POINTS.md` 신설(골격) — 5 카테고리 표 헤더, 본문은 Phase 05.
- `INDEX.md` — ENTRY_POINTS 링크.
- 루트 `Directory.Build.props` — StyleCop.Analyzers 1.2.0-beta.556.
- 루트 `.editorconfig` — StyleCop 8카테고리 `none` + SA1201/SA1202만 `warning` + SA0001 `none`.
- `03_Client/Directory.Build.props` — 빈 차단막(Unity 격리).

---

## AC 검증 결과

| 완료조건 | 검증 | 결과 |
|---|---|---|
| CODE_CONVENTION v6 4섹션 + 변경이력 v6 | §2.5/§6.5/§7.1/§7.2 + 이력표 v6 행 존재 | ✅ |
| 부록 A 실측 정합 | GameMap 졸업(436·6System), ClientPacketHandlers 909 미실행, 중복 7건 편입 | ✅ |
| `.editorconfig` 멤버정렬이 빌드 경고로 작동(에러 X) | 아래 빌드 로그 | ✅ |
| ENTRY_POINTS.md 골격 + INDEX 링크 | 5 카테고리 + 트리 링크 | ✅ |
| 코드 변경 0 | `git status --porcelain \| grep '\.cs$'` → 0건 | ✅ |

**빌드 검증** (`~/.dotnet/dotnet build Dawnholder.slnx`, WSL2):
```
Build succeeded.
    188 Warning(s)
    0 Error(s)
SA 경고 종류별:  212 warning SA1201  /  164 warning SA1202   (raw = 본문+요약 중복, 실 ~188)
SA0001 = 0,  다른 StyleCop 룰 = 0  (카테고리 격리 작동)
```
- StyleCop.Analyzers 1.2.0-beta.556 복원 성공(.NET 10).
- Phase 01은 경고가 "뜨는지"만 확인 — **0으로 만드는 스윕은 Phase 05**.
- 빌드가 `Shared.dll`+`Dawnholder.Client.Net.dll`을 재컴파일 → Plugins drift → 소스 무변경이라 `git checkout` 복원(memory 패턴).

---

## 결정 흐름

1. **StyleCop 도입은 plan 범위 밖 → 사용자 GO 받고 확장.**
   plan은 "코드 0, .md + .editorconfig만"이라 했으나 실측 결과 **StyleCop 미설치 + .editorconfig 0개**. SA12xx 룰은 패키지가 있어야 존재 → `.editorconfig`만으론 "빌드 경고로 작동"(완료조건) 불가. 패키지 도입 = `Directory.Build.props`(빌드설정) 변경. 사용자에게 trade-off 의논 → **"StyleCop 도입" 선택**. `.props` 2개가 산출물에 추가됨(`.cs`는 여전히 0).

2. **Unity 격리** — 루트 props가 Unity 자동생성 csproj로 전파되면 빌드/IDE 깨짐(Unity는 NuGet PackageReference 비호환). `03_Client/Directory.Build.props` 빈 차단막으로 가로챔(MSBuild는 가장 가까운 props 하나만 import) = .NET 표준 격리 패턴.

3. **plan 파일 경로가 틀렸다 (Phase 02~04 주의)** — GameMap: plan `World/` → 실제 `02_Server/GameServer/Maps/`. Session/Handlers: plan `04_ClientNet/` → 실제 `03_Client/Assets/Scripts/Network/`. 줄 수(436/213/909)는 plan 정확, 경로만 정정.

4. **GameMap 책임헤더는 plan 예시문과 표현 다름** — 실제 헤더는 `// ARCHITECTURE "Map = Actor"...` 인용형. v6 §6.5는 실제 헤더를 모범으로 인용하되 "형식 기준 = 비자명 책임 선언"으로 정의(실제 헤더 수정은 코드 변경 = 범위 밖).

---

## 학습 일지 후보 키워드

- **StyleCop.Analyzers .editorconfig 카테고리 격리**: `dotnet_analyzer_diagnostic.category-StyleCop.CSharp.*Rules.severity = none`으로 8 카테고리 끄고 SA1201/1202만 `warning`. more-specific 룰이 category none을 이김. SA0001(특수룰)은 카테고리로 안 꺼져 개별 `none` 필요.
- **Directory.Build.props Unity 격리 차단막**: 빈 props를 하위(03_Client)에 두면 MSBuild가 루트 props 전파를 차단(가장 가까운 것 하나만 import). Unity는 NuGet 비호환이라 필수.
- **plan 경로는 실측 검증 먼저**: plan-auditor GO 받은 plan도 파일 경로가 stale일 수 있음(World/ vs Maps/). 줄 수는 맞아도 경로는 틀림 — 착수 전 `git ls-files` 실측.
- **StyleCop 1.2.0-beta.556이 .NET 10에서 작동** — 마지막 stable(1.1.118)이 아니라 beta가 최신 SDK 지원.
