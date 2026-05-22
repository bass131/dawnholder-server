# 유현 영역 재논의 자료 (2026-05-22)

> **목적**: M3.6 Phase 05 (클라 코드 전수조사) 중 *유현 영역 경계 모호 케이스* 박제. **본 자료는 변경 결정 X, 재논의 자료까지가 책임**.
>
> **재논의 시점**: M3.5 마감 후 본인 + 유현 의논 권장 (M4 진입 전).
>
> **출처**: `01_Phases/youngho/M3.6-harness-and-codebase-audit/05-client-codebase-audit-DONE.md` AC §3 + reviewer Tier 2-A 추가 발견.

---

## §1 점검 범위

CODEOWNERS 박힌 유현 단독 3 경로:
- `/03_Client/Assets/Scenes/UI.unity`
- `/03_Client/Assets/Scripts/Bootstrap/`
- `/03_Client/Assets/Scripts/UI/`

**변경 0건**. 읽기만 (UI.unity는 git log만, YAML 보지 않음 — prefab 사고 학습 정합).

---

## §2 경계 모호 케이스 (3건)

### 2-1. `HudController.UpdateHP()/UpdateGold()` 연결 미완성

**위치**: `03_Client/Assets/Scripts/UI/HudController.cs:11-13`

**증상**:
- `HudController.UpdateHP(current, max)` + `UpdateGold(amount)` *public 메서드만* 박힘
- 주석 12~13줄에 "다음 마일스톤에서 패킷 수신 핸들러가 호출"이라고 forward 약속 박힘
- 실제로 `UnityClientSession.HandleSnapshot` / `HandleHitResult` 등 클라 네트워크 측에서 `HudController.UpdateHP()` 호출하는 경로가 *현재 없음*
- 즉 `HudController`는 `Start()`에서 mock 값으로만 초기화됨

**경계 모호 사유**:
- `HudController` 자체 = **유현 영역** (UI/ 하위)
- *HP 패킷 수신 → HudController.UpdateHP() 호출 경로* = **클라 네트워크 영역** (본인 영역)
- 양쪽이 만나는 지점 = 어느 쪽이 박을지 명시 안 됨

**재논의 권장 안건**:
- M4 전투 패킷 설계 시 `S_PlayerHp` 또는 `S_HitResult`의 *본인 HP 경로* 박힘 시점에 *클라 네트워크 → HudController* 호출을 누가 박을지 결정
- 옵션 A: 본인이 `UnityClientSession`에 dispatch 박고 `HudController.UpdateHP()` 직접 호출
- 옵션 B: 본인이 `S_PlayerHp` 이벤트만 publish하고 `HudController`가 subscribe (event-driven 정합)
- 옵션 C: 별 `PlayerStateBridge` 컴포넌트 신설 (영역 분리 명시화)

**reviewer 재판단**: forward false-promise 변종 (명시적 미래 약속 박힘) — false-promise 아님. M4 전투 패킷 설계와 묶음 자연스러움 (P1 backlog).

---

### 2-2. `StageClearUI.BuildRuntime()` Combat → UI 호출 흐름

**위치**:
- `03_Client/Assets/Scripts/UI/StageClearUI.cs:128-135` (BuildRuntime 메서드)
- `03_Client/Assets/Scripts/Combat/CombatBootstrap.cs` (호출처)

**증상**:
- `StageClearUI` = **유현 영역** (UI/ 하위)
- `StageClearUI.BuildRuntime()` 정적 메서드를 `CombatBootstrap`(본인 영역)이 호출 → UI 객체 *런타임 생성*
- 의도: 씬 YAML 편집 회피 (`runtime-code-gen-scene-yaml-avoidance` 학습 정합)

**경계 모호 사유**:
- UI 객체를 *Combat 코드가 생성*하는 구조 = 영역 침범 후보
- 현재는 *씬 YAML 충돌 차단* 목적으로 의도적 박힘
- 유현 UI 씬이 커지면 StageClearUI를 UI.unity로 마이그레이션할 가치 ↑

**재논의 권장 안건**:
- M4 진입 시점에 *StageClearUI를 UI.unity로 마이그레이션할지* 결정
- 마이그레이션 시: 유현이 UI.unity에서 prefab 박음 + 본인은 `Find` 또는 `[SerializeField]` 참조로 단순 활성화
- 유지 시: 현재 구조 + 영역 침범 *의도적 명시* 주석 박음 (CombatBootstrap에)

---

### 2-3. `StageClearUI.BuildRuntime()` Reflection IL2CPP risk

**위치**: `03_Client/Assets/Scripts/UI/StageClearUI.cs:128-135`

**증상**:
- `[SerializeField] private` 필드를 런타임 코드 생성 시점에 `System.Reflection.GetField().SetValue()`로 주입
- Unity Mono(개발 빌드)에서는 동작
- **IL2CPP(배포 빌드)에서는 code stripping이 private 필드 reflection을 제거할 수 있음** → 배포 빌드 시 NullReferenceException 위험

**경계 모호 사유**:
- StageClearUI = **유현 영역** (UI/ 하위)
- Reflection 호출 = `BuildRuntime` 내부 (UI 코드)
- 배포 빌드 봉합책 = `link.xml` (Plugins/ 또는 03_Client/Assets/) 또는 `[Preserve]` attribute — 둘 다 *영역 경계 명확하지 않음*

**재논의 권장 안건**:
- M5+ 배포 빌드 시점 (캡스톤 발표 후 단계)에 봉합
- 봉합 옵션:
  - A: `[SerializeField] private` → `public` 전환 (Reflection 제거)
  - B: `[Preserve]` attribute 박음 (UnityEngine.Scripting.PreserveAttribute)
  - C: `link.xml`에 `<Type fullname="StageClearUI" preserve="all"/>` 박음 (별 파일, 영역 무관)
- 옵션 B가 가장 영역 친화적 — 유현이 StageClearUI에 attribute 박는 식

**우선순위**: P2 (별 시점 backlog, 배포 빌드 단계까지 유효).

---

## §3 CODEOWNERS 정합 확인 (참고)

PR #17 (2026-05-17 박힘) 정합:
- `/03_Client/Assets/Scenes/UI.unity` — `@jungyoohyun0105` 단독 ✅
- `/03_Client/Assets/Scripts/Bootstrap/` — `@jungyoohyun0105` 단독 ✅
- `/03_Client/Assets/Scripts/UI/` — `@jungyoohyun0105` 단독 ✅

본 자료에 박힌 3 케이스 모두 *유현 단독 권한 영역*이라 본인 영역에서 *변경 X*. 재논의는 본인 + 유현 둘 다 합의 필요.

---

## §4 unity-bridge 자문 결과 (참고, 유현 영역 X)

unity-bridge 발견은 *유현 영역 아님* (Prefabs/Characters/ = 본인 + 유현 협업이지만 prefab 자체는 무주공산). 별 시점 M4 진입 전 정리 권장. 본 자료는 유현 영역만 다룸.

---

## §5 재논의 권장 cadence

- **M3.6 마감 직후 (Phase 06 후)**: 의논 1회차 — 본 자료 3 케이스 전체 훑음
- **M4 진입 시점**: 의논 2회차 — 케이스 2-1 (HudController) 봉합 옵션 결정
- **M5+ 배포 빌드 단계**: 의논 3회차 — 케이스 2-3 (Reflection IL2CPP) 봉합

---

## §6 본 자료 위치

- 본 파일: `01_Phases/youngho/M3.6-harness-and-codebase-audit/_yuhyeon-area-review-2026-05-22.md`
- 출처: `05-client-codebase-audit-DONE.md` AC §3 + reviewer Tier 2-A 추가 발견
- 인용 시점: M3.6 Phase 06 종합 보고에서 1회 인용

---

## ➡️ 다음 액션

- 본 자료 박은 직후 = **변경 X, 의논 대기**
- 본인 + 유현 의논 시점 = 별 시점 (Phase 06 후 또는 M4 진입 직전)
- 의논 결과 박힌 후 = 본 파일 갱신 (옵션 결정 박음) 또는 새 ADR 신설
