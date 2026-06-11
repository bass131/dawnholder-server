---
owner: youngho
phase: 05
status: done
grade: 복잡 (마일스톤 마감이므로 보고 양식 = 대규모: `_milestone-DONE.md` + 5단계 보고 MD/HTML)
summary: Roslyn 멤버정렬 전체 스윕(경고 0) + 클래스 책임 헤더 보강 + ENTRY_POINTS.md 실제 작성 + 전체 회귀 + 마일스톤 마감
---

# Phase 05: 컨벤션 강제 스윕 + 진입점맵 작성 + 전체 회귀 + 마감

> **상태**: pending
> **마일스톤**: M4.10
> **등급**: 복잡 (마일스톤 마감이므로 보고 양식 = 대규모: `_milestone-DONE.md` + 5단계 보고 MD/HTML)
> **담당**: 메인 세션 + qa Worker
> **의존**: Phase 01~04 **전부** (스윕은 모든 코드가 박힌 뒤, 진입점 맵은 통합된 구조 위에서)

---

## 🎯 목표

Phase 01이 *선언*한 컨벤션 v6를 **강제 스윕으로 비로소 적용**한다. 멤버정렬 Roslyn 룰을 전체 코드에 적용해 **경고 0**까지 정리하고, 주요 클래스에 1줄 책임 헤더(§6.5)를 보강하며, Phase 01에서 골격만 만든 `ENTRY_POINTS.md`를 **실제 룩업표로 채운다**(전투·이동·스킬·맵이동 + **동기화 항목** — M4.11 대비). 그리고 전체 회귀(dotnet test + 봇 전 시나리오 + Unity 콘솔 0에러)로 거동 불변을 증명한 뒤 마일스톤을 마감한다. 이 Phase가 끝나면 **컨벤션이 "강제된" 상태가 되고, 다음 마일스톤(M4.11 동기화)의 디버깅 자산(진입점 맵)이 손에 들어온다.**

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 — 컨벤션 v6 + `.editorconfig` 멤버정렬 룰 + ENTRY_POINTS.md 골격
- [ ] Phase 02 완료 — 매직넘버 단일화 + HitEffect enum (스윕 대상 코드 확정)
- [ ] Phase 03 완료 — HandleEnemyDeath 통합 (GameMap 구조 확정)
- [ ] Phase 04 완료 — roster 통합 + 헬퍼 (GameMap/CombatSystem/SkillSystem 구조 확정)
- [ ] 02~04 전부 머지 — 스윕은 통합된 최종 구조 위에서 한 번에(중복 스윕 방지)

---

## 📝 작업 내용

> 멤버정렬 스윕 → 책임 헤더 보강 → 진입점 맵 작성 → 전체 회귀 → 마감.

**멤버정렬 Roslyn 전체 스윕 (경고 0)**:
- [ ] `dotnet build`로 SA1201/SA1202 경고 목록 수집
- [ ] 각 파일의 멤버를 §7.1 순서(상수→static필드→인스턴스필드→프로퍼티→생성자→public메서드→private메서드→중첩타입)로 재배치 — IDE/`dotnet format` 자동 정렬 활용
- [ ] **경고 0까지** — 스윕 diff는 크지만 동작은 불변(멤버 *순서*만 바뀜, 로직 0)

**클래스 1줄 책임 헤더 보강 (§6.5)**:
- [ ] 주요 public 클래스 상단에 책임 1줄 헤더 — GameMap(모범, 이미 있음) 외에 CombatSystem/SkillSystem/DeferredDamageSystem/GameSession/MapMigration 등 핵심 클래스
- [ ] §6.2 금지 주석(자명 재진술·역사 박제)과 구분 — "이 클래스가 *무엇을 책임지는가*"만

**`ENTRY_POINTS.md` 실제 작성 (00_Document/conventions/)**:
- [ ] "증상 → 시작 파일·함수" 룩업표 채우기. 최소 카테고리:
  - **전투** — 예: "데미지가 안 들어감 → `CombatSystem.ProcessAttack` → 박스 스캔 → S_HitResult"
  - **이동** — 예: "캐릭터가 안 움직임 → `LocalPlayerMovement`(클라 prediction) ↔ `GameMap.Tick` 물리 루프(서버 권위)"
  - **스킬** — 예: "스킬이 안 나감 → `LocalPlayerInput.TrySendSkill`(클라 게이트) → C_SkillUseHandler → `SkillSystem.ProcessSkill`"
  - **맵이동** — 예: "맵 넘어가면 적이 안 보임 → `MapMigration.Execute` → `GameMap.SendInitialRosterTo`"
  - **동기화** (M4.11 대비) — 예: "원격이 천천히 따라옴 → RemoteEntity 보간 타임소스 / 버퍼" / "내 캐릭터가 튕김(rubber-band) → reconcile 경로 / force-adopt 게이트"
- [ ] 각 시스템 파일 상단 흐름 1줄 헤더(§7.2)가 스윕 후에도 정합한지 확인

**전체 회귀 (qa)**:
- [ ] `dotnet test` 전체 green
- [ ] 헤드리스 봇 **전 시나리오** — RangedHitSmoke/ThunderboltAoeSmoke/DashSmoke/TeleportSmoke/BossStageClear 등 마일스톤 전 결과와 동일(거동 불변 증명)
- [ ] Unity 콘솔 **error CS 0** — Shared.dll 재빌드 후

**마일스톤 마감**:
- [ ] `_milestone-DONE.md` 작성 (대규모 → 5단계 보고 MD + HTML 이중 박음)
- [ ] CHANGELOG[M] 갱신 (`.claude/CHANGELOG.md`)
- [ ] 세션 마감 권유

---

## ✅ 완료 조건 (정량)

- [ ] **Roslyn 멤버정렬 경고 0** — `dotnet build` SA1201/SA1202 경고 0건
- [ ] **ENTRY_POINTS.md 채워짐** — 최소 5 카테고리(전투/이동/스킬/맵이동/동기화) 각 1+ 룩업 행, 동기화 항목 포함
- [ ] **전체 dotnet test + 봇 green** — 마일스톤 전 시나리오 결과와 동일(거동 불변)
- [ ] **Unity 콘솔 error CS 0**
- [ ] **DONE 박제** — `_milestone-DONE.md` + `.html`(5단계 보고) + CHANGELOG[M]

---

## 🧪 테스트

**자동**:
- `dotnet build` — SA1201/SA1202 경고 0
- `dotnet test` 전체 — 마일스톤 누적 변경(02~04) 후 회귀 0
- 헤드리스 봇 전 시나리오 — PASS, 추출/스윕 전과 동일 결과

**수동**:
- 2클라 Play 1회전 — 4스킬(근접/Thunderbolt/Dash/Teleport) + 맵이동 + 보스 StageClear가 마일스톤 전과 동일하게 동작(거동 불변 눈으로 확인)
- ENTRY_POINTS.md를 들고 "증상 → 시작 파일"이 실제 코드와 맞는지 1회 추적 검증

---

## 📚 학습 포인트

- **컨벤션은 "선언"이 아니라 "강제 스윕"으로 비로소 적용된다 (ADR-028 정신)**: Phase 01이 "멤버는 이 순서"라고 *선언*해도, 기존 코드가 그 순서가 아니면 컨벤션은 종이일 뿐이다. 전체 스윕으로 경고 0을 만들어야 비로소 "이제부터 어기면 빌드가 경고한다"가 성립한다. 헌법 §5("선언 ≠ 강제")의 실천 — 선언과 강제 사이엔 반드시 *스윕*이라는 한 번의 적용이 필요하다.
- **진입점 맵이 다음 마일스톤(M4.11 동기화)의 디버깅 자산이 됨**: 동기화 버그는 증상(rubber-band, 슬라이드, 느린 추종)에서 원인(reconcile, 보간 타임소스, force-adopt 게이트)까지 거리가 멀다. ENTRY_POINTS.md에 "증상 → 시작 파일"을 미리 적어두면, M4.11에서 버그를 만났을 때 백지 탐색 없이 바로 출발점을 안다. 진입점 맵은 *미래의 나*를 위한 투자다.

---

## ⚠️ 함정 / 주의사항

- **멤버정렬 스윕이 diff를 크게 내지만 동작은 불변** — 수백 줄 diff가 나도 그건 멤버 *순서*만 바뀐 것. 로직이 한 줄도 안 바뀌었음을 회귀 테스트(dotnet test + 봇)로 *반드시* 확인. diff 크기에 겁먹지 말되, 회귀 green 없이는 마감 금지.
- **스윕 중 실수로 로직을 건드리지 말 것** — 자동 정렬 도구(`dotnet format`)가 멤버를 옮기다 의존 순서(예: static 초기화 순서)를 깨뜨릴 수 있다. 스윕 후 빌드 + 테스트로 검증.
- **진입점 맵을 빈약하게 두면 비상 가독성 가치 0** — "전투 → CombatSystem.cs" 한 줄만 적으면 룩업표의 의미가 없다. *증상*에서 출발해 *함수*까지, 흐름 요약까지 적어야 비상 디버깅에서 쓸 수 있다. 특히 동기화 항목은 M4.11이 바로 쓸 것이라 충실히.
- **마감 게이트** — `phase-gate-validator.sh`가 frontmatter + 등급별 의무 섹션을 검사. 대규모 마일스톤은 5단계 보고 MD/HTML 이중 박음 의무.

---

## ➡️ 다음 Phase

- 마일스톤 마감 → M4.11(동기화 동작 검증, ENTRY_POINTS.md 활용) 권유.

---

## 📋 박제 (완료 후 -DONE.md)

- 복잡 등급(Phase) → `-DONE.md` 박음. 단 **마일스톤 마감**이므로 `_milestone-DONE.md`(대규모) + 5단계 보고 MD/HTML 이중 박음 동반.

---

## 작업 로그

- 2026-06-11: 계획 작성 (컨벤션 v6 강제 스윕 + ENTRY_POINTS.md 본문 작성 + 전체 회귀 + 마감. 01~04 전부 머지 후 한 번에 스윕)
