---
owner: youngho
milestone: M4.3R
phase: 01
title: Convention 문구 정합 + 정책 결정 + 베이스라인 회귀 스냅샷
status: done
grade: 보통
domain: shared
estimated: 1~2h
---

# Phase 01: Convention 문구 정합 + 정책 결정 + 베이스라인 회귀 스냅샷

> **상태**: done (2026-05-29 — 정책 결정 + §3.3 명문화 v4 + 베이스라인 박음, 코드 변경 0)
> **마일스톤**: M4.3R
> **등급**: 보통 (문서 + 정책결정 + 베이스라인 — 코드 변경 최소, 후속 Phase 블로커 해소)
> **담당**: shared SubAgent (CODE_CONVENTION 편집) + 메인 직접 (정책 결정 사용자 확인 + 베이스라인 측정)

---

## 🎯 목표

리팩토링 코드를 한 줄도 건드리기 *전에* 두 가지를 못 박는다: (1) 네이밍 후속 Phase(06·07)를 막고 있는 **정책 결정 2건**을 사용자 확인 후 CODE_CONVENTION에 명문화, (2) "동작 보존"을 정량 증명할 **베이스라인 회귀 스냅샷**(빌드/테스트/봇 스모크 green 기록)을 박는다. 이 Phase가 끝나면 모든 후속 Phase의 선행 의존이 풀리고, 비교 기준선이 생긴다.

---

## ⏪ 사전 조건

- [x] convention-audit 워크플로우 백로그 (9건) — 이 마일스톤의 입력
- [ ] 없음 — M4.3R 첫 Phase

---

## 📝 작업 내용

### 정책 결정 (사용자 확인 의무 — 코드 변경 아님)
- [ ] **결정 1 — §3.3 서버 적용 명문화**: 현재 §3.3은 "3. Unity 클라이언트" 섹션 아래라 서버(ServerCore Network/)의 `m_` prefix가 위반인지 해석 갭. CODE_CONVENTION에 "**§3.3 네이밍 prefix는 서버 production 코드에도 동일 적용** (`m_` 헝가리안 → `_camelCase`)" 한 줄 추가. → Phase 07 선행.
- [ ] **결정 2 — SerializeField 규칙 (사용자 택1)**:
  - **옵션 A**: `[SerializeField]` 필드도 `_camelCase`로 통일 (§3.3 문자 그대로. Inspector 라벨은 자동 정리되어 표시 무해). → Phase 06에서 8파일 rename (보통)
  - **옵션 B**: "`[SerializeField]`는 bare camelCase, 순수 private은 `_camelCase`" 규칙을 §3.3에 명문화해 현 패턴 합법화. → Phase 06은 isPaused 1건만 (단순)
  - 결정 후 §3.3에 명문화. → Phase 06 선행.

### Convention 문서 반영 (shared)
- [ ] CODE_CONVENTION.md §3.3에 위 결정 2건 반영 + 변경이력 v4 한 줄
- [ ] (선택) ADR-028 또는 짧은 보충 노트에 "서버 §3.3 적용 + SerializeField 규칙" 결정 사유 박음 (헌법 우선순위상 Convention 문구로 충분하면 생략)

### 베이스라인 회귀 스냅샷 (메인 직접)
- [ ] `dotnet build Dawnholder.slnx` green 확인 (경고/오류 카운트 기록)
- [ ] `dotnet test --no-incremental` green — **통과 테스트 카운트 기록**. 기준 = M4.3 Phase 07 -DONE.md 실측 **315 통과 / 0 실패 / 4 skip** — 재실측해 같은지 확인 후 박음 (후속 회귀 비교 기준)
- [ ] 헤드리스 봇 스모크 3종 baseline 기록: `EnemyAiSmoke` / `BossStageClearSmoke` / `MapTransitionScenario` (실행 가능 여부 + 통과 기준)
- [ ] size-guard 현 경고 3파일 줄수 기록 (GameMap 665 / GameSession 700 / UnityClientSession 665 — 리팩토링 후 <600 목표)

---

## ✅ 완료 조건

- [ ] CODE_CONVENTION §3.3에 "서버 적용" + "SerializeField 규칙(택1 결과)" 명문화 완료
- [ ] 베이스라인 테스트 카운트 박힘 (예: "315 통과 / 0 실패 / 4 skip" — 후속 Phase 회귀 비교 기준)
- [ ] 헤드리스 봇 스모크 3종 baseline 통과 확인 기록
- [ ] size-guard 경고 3파일 줄수 기록 (리팩토링 목표값)
- [ ] 코드(.cs) 변경 0 — 문서 + 측정만 (순수 준비 Phase)

---

## 🧪 테스트

**자동**: 베이스라인 측정 자체 (`dotnet test`) — 변경 없으니 통과가 당연. 카운트를 박는 게 목적.
**수동**: 봇 스모크 3종 실행 가능 여부 확인.

---

## 📚 학습 포인트

- **리팩토링 안전망 = 회귀 테스트 베이스라인**: "동작 보존"을 주장하려면 *변경 전* 통과 카운트를 박아두고 *변경 후* 같은지 비교해야 함. 베이스라인 없는 리팩토링은 "안 깨졌겠지" 추측.
- **정책 결정을 코드보다 먼저**: 네이밍 규칙이 옵션 A/B로 갈리면 코드를 어느 방향으로 고칠지 못 정함. 결정을 선행 Phase로 떼면 후속 Worker가 헤매지 않음.

---

## ⚠️ 함정 / 주의사항

- **정책 결정 미루면 Phase 06·07 블로킹** — 이 Phase의 핵심 산출물은 코드가 아니라 "결정 + 기준선".
- **CODE_CONVENTION은 shared SubAgent 영역이지만 헌법 우선순위 문서** — 문구 추가는 신중히, refs(책 이론)와 섞지 않기 (우리 규칙만).
- **베이스라인은 반드시 깨끗한 working tree에서** — 미커밋 변경이 섞이면 비교 기준 오염.

---

## ➡️ 다음 Phase

- Phase 02 (클라 dispatch) ∥ Phase 03 (GameMap System) — R1 병렬

---

## 📋 박제 (완료 후)

- **보통 등급** — work-pin + commit message만 (-DONE.md 안 박음). 단 베이스라인 카운트는 work-pin 작업로그에 박음 (후속 비교용).

---

## 작업 로그

- 2026-05-29: 계획 수립 (`/work:plan`)
- 2026-05-29: **완료**.
  - **정책 결정 (사용자)**: SerializeField = **옵션 A**(`[SerializeField]`도 `_camelCase`, designer 예외 없음). 매개변수/지역변수 = `camelCase`(밑줄 금지). 필드 = `_camelCase`(밑줄). → §3.3 v4 명문화 + 서버 공통 적용 + `_`-prefix 매개변수를 §4 casing이 아닌 §3.3 prefix 위반으로 재분류(rank 9 = Phase 07 포함).
  - **CODE_CONVENTION §3.3 → v4** 갱신 (서버·클라 공통 / SerializeField `_camelCase` / 매개변수 밑줄 금지).
  - **베이스라인 스냅샷** (clean working tree, `feature/m4.3` 기준):
    - build: `dotnet build Dawnholder.slnx` → 경고 0 / 오류 0 ✅
    - test: `dotnet test Dawnholder.slnx --no-build` → **실패 0 / 통과 315 / skip 4 / 전체 319** (Phase 07 DONE.md 실측과 일치). skip 4 = 봇 통합 시나리오(MapTransition/M2Basic/LagSim ×2, 서버 실행 필요라 CI skip — 수동 검증용)
    - size-guard 대상 줄수: GameMap **665** / GameSession **700** / UnityClientSession **665** (리팩토링 후 <600 목표)
  - **⚠️ 도구 학습**: `dotnet test --no-incremental`은 SDK 10.0.203 + slnx에서 `MSB1001 알 수 없는 스위치`로 실패. 대안 = `dotnet build --no-incremental`(클린) → `dotnet test --no-build`(재빌드 회피). work-pin 습관 정정함.
  - Phase 06(옵션 A 확정) / Phase 07(rank 9 포함) 문서 정합 갱신.
