---
owner: youngho
milestone: M4.1
phase: 01
title: Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본
status: pending
grade: 보통
risk: low
estimated: 1~2h
domain: qa+cross
---

# Phase 01: Codex β 크로스 리뷰 + M3 응급 하드코딩 추가 발본

> **상태**: pending
> **마일스톤**: M4.1
> **등급**: 보통 (2 도메인 cross / qa+cross / 비가역 X / 보고서 박제 위주)
> **담당**: 메인 직접 (영호) + Codex β 외부 (`/cross-review` 슬래시 정신)

---

## 🎯 목표

**M4.1 본격 진입 전 M3 응급 코드 전체에 *예상 못한 하드코딩*이 추가로 박혀있는지 외부 시각으로 검증** + 발견 시 처리 결정 (즉시 봉합 / M4.2-M4.3 이관 / 별 시점). M4.1 plan 갱신 필요성도 함께 판정.

본 Phase가 끝나면 = (a) `00_Document/reviews/2026-MM-DD-pre-m4-cross-review-codex.md` 박힘, (b) 발견 항목별 처리 결정 명시, (c) M4.1 Phase 02·03 plan 조정 결정 박힘.

---

## ⏪ 사전 조건

- [x] M3 마일스톤 완전 마감 (PR #38~#40 머지)
- [x] M3.5 `/cross-review` 슬래시 박힘 (Codex β 호출 패턴 정합)
- [x] M3.6 코드 전수조사 결과 (`01_Phases/youngho/M3.6-harness-and-codebase-audit/04~05-DONE.md`) 박힘 — 본 Phase는 *M3 응급 흔적 중심*으로 좁힘 (M3.6은 헌법/ADR 정합 중심)
- [x] ARCHITECTURE.md "M4 사전 과제" 8건 인지

---

## 📝 작업 내용

### 1단계: Claude α (메인) 자체 점검 + 발견 박제

- [ ] `02_Server/GameServer/Combat/` 전수조사 (CombatConstants / EnemyEntity / EnemyKind) — magic number / 고정 위치 / AI 없음 흔적 박제
- [ ] `02_Server/GameServer/Maps/GameMap.cs` 전수조사 — `NormalEnemySpawnX=10` / `BossSpawnX=30` / `NormalEnemyMaxHp=30` / `BossMaxHp=100` 등 const 박힘 위치 + 단일 맵 3-zone trick 흔적
- [ ] `02_Server/GameServer/Handlers/AttackHandler.cs` — decode-only 정합 확인 + magic 검증 흔적
- [ ] `03_Client/Assets/Scripts/Combat/` (RemoteEnemy / EnemyRegistry / CombatBootstrap / ZoneVisualizer) — 클라 placeholder 스프라이트 / zone 좌표 클라 박힘 / hardcoded color 등 발견
- [ ] `03_Client/Assets/Scripts/UI/` (StageClearUI / MainMenuController) — 더미 라벨 / TODO 텍스트 발견
- [ ] `03_Client/Assets/Scripts/Network/NetworkBootstrap.cs` — 서버 IP 박힘 (`127.0.0.1` placeholder?) / 포트 박힘 (7777?) 발견
- [ ] 발견 항목을 `00_Document/reviews/2026-05-22-pre-m4-cross-review-claude.md` 임시 박음 (Codex 입력용)

### 2단계: Codex β 외부 호출 (`/cross-review` 슬래시 정신)

- [ ] `/cross-review` 슬래시 호출 (또는 본인이 Codex CLI 직접 호출) — input = "M3 응급 코드 전수조사. 본 마감(11/19)엔 정밀화 필요한 하드코딩·magic number·placeholder·TODO 색출. ARCHITECTURE.md M4 사전 과제 8건은 본인 이미 인지 — 그 *외* 발견 중심. **추가 자문: M4.1 Phase 03 precision hitbox = AABB vs capsule trade-off 의견** (응급 우선 AABB 추천 박혀있음, capsule은 점프 정합 영역 — Codex 시각 자문)"
- [ ] Codex β 발견 항목 박제 → `00_Document/reviews/2026-05-22-pre-m4-cross-review-codex.md`
- [ ] α 점검 vs β 점검 교집합 / 차집합 분류

### 3단계: 발견 항목별 처리 결정

- [ ] 각 발견 항목을 4 분류 중 하나로 박음. **박는 위치 = Codex 보고서 마지막 섹션 표** (work-pin은 30~40줄 압축이라 4 분류 박기 부담, -DONE.md 없는 보통 등급이라 보고서 본문이 정합):
  - **즉시 봉합** (M4.1 안에서 처리 — Phase 02·03에 흡수 또는 Phase 0X 추가)
  - **M4.2 이관** (맵 분리·cheat-flag·Serilog 영역)
  - **M4.3 이관** (AI·jump mispredict·PvP 영역)
  - **별 시점** (M5+ 또는 캡스톤 후, 본 마일스톤과 영역 다름)
- [ ] **"발견 1건"의 정의 = 1 magic number 또는 1 hardcoded 위치 (영역 단위 X)**. 예: `NormalEnemySpawnX=10` + `BossSpawnX=30` + `NormalEnemyMaxHp=30` + `BossMaxHp=100` = **4건** (1 영역으로 묶지 않음). 임계값 작동의 정량 정합 보장.
- [ ] 발견 양 평가:
  - 0~3건 = M4.1 plan 변경 없음 (Phase 02·03 그대로 진행)
  - 4~7건 = M4.1 Phase 0X 추가 검토 (옵션 A: 본 마일스톤에 흡수 / 옵션 B: M4.3로 분산)
  - 8건+ = M4.1 plan 재구성 필요 (별 마일스톤 M4.0 신설 검토)

### 4단계: plan 갱신 결정 (사용자 확인 게이트)

- [ ] 발견 양 + 처리 결정 사용자에게 보고 (학부생 멘토링 톤)
- [ ] M4.1 plan 변경 필요 시 사용자 명시 GO 후 갱신 (옵션 C 게이트 정신 — 본인 인지)
- [ ] CHANGELOG entry 박을지 결정 (보고서 자체는 [L] / plan 갱신 시 [M])

---

## ✅ 완료 조건

- [ ] `00_Document/reviews/2026-05-22-pre-m4-cross-review-codex.md` 박힘 (Codex β 발견 항목)
- [ ] `00_Document/reviews/2026-05-22-pre-m4-cross-review-claude.md` 박힘 (Claude α 점검 — Codex 입력 자료 + α/β 비교)
- [ ] 발견 항목별 처리 결정 (4 분류) 표 박힘 — **Codex 보고서 마지막 섹션에 박음** (위치 확정)
- [ ] M4.1 Phase 02·03 plan 갱신 결정 박힘 (그대로 / 일부 조정 / 재구성)
- [ ] **등급 자동 상향 트리거**: 발견 양 8건+ 또는 즉시 봉합 결정 항목 있으면 *복잡 자동 상향* → -DONE.md 박음 + **사용자 명시 GO 게이트** 후 plan 갱신 (plan 재구성 = irreversible 직전 단계, 옵션 C 게이트 정신)
- [ ] 트리거 없으면 (0~3건 + 즉시 봉합 X) = 본 Phase 보통 등급 유지, -DONE.md 없음, work-pin + commit 보고 충분

---

## 🧪 테스트

**자동**:
- 본 Phase는 *조사 + 의논* 위주 = 코드 변경 없음 = 자동 테스트 X
- 예외: 만약 즉시 봉합 결정 항목이 있어 코드 변경 시 → dotnet test green 의무

**수동**:
- 보고서 박힘 검증 (파일 존재 + 발견 항목 4 분류 명시)
- 사용자 명시 GO (plan 갱신 필요 시)

---

## 📚 학습 포인트

- **외부 시각 cross review의 가치** — α(Claude) 단독 점검은 자기 confirmation bias 위험. β(Codex)로 외부 시각 1회 거치면 *놓친 패턴* 발견 확률 ↑. M3 Phase 02 Codex 발견 7건 + M3.6 외부 리뷰 4건 학습 정합.
- **사전 점검의 정합 (M3 Phase 01 pre-flight smoke 두 번째 실측)** — 본격 진입 *전* 조사로 사고 비용 ↓. 학부생 백지 단독 결정 위험 완화.
- **발견 양 정량 분류 → 처리 결정** — 0~3건 / 4~7건 / 8건+ 임계값으로 plan 변경 결정 = 옛 *추측 결정* → 새 *정량 게이트* 패턴.
- **`/cross-review` 슬래시 정착 단계** — γ 방식 (3회차 = Rule of Three 통과) 후 슬래시화 박혔음. 본 Phase = 슬래시 첫 운영 실측 단계.

---

## ⚠️ 함정 / 주의사항

- **Codex β scope creep 함정** — Codex가 *M4 사전 과제 8건*까지 다 발견 박으면 noise ↑. 입력에 "*그 외* 발견 중심" 명시 의무.
- **α 자체 점검 빠뜨림 함정** — α가 자신 작업 결과를 *덜 비판적*으로 보는 패턴. M3.6 Phase 05 학습 정합 (본인 영역은 외부 시각이 필수).
- **즉시 봉합 분류 함정** — 발견 항목을 "지금 빨리 고치자"로 즉시 봉합 시 *scope creep*. 본 Phase는 *발견 + 분류*가 본질, 봉합 자체는 Phase 02·03 또는 별 Phase.
- **보고서 파일명 날짜 정합** — `2026-05-22` 박지만 본 Phase가 다음 세션으로 미뤄지면 실제 날짜로 정정. work-pin "마지막 갱신" 정합.

---

## ➡️ 다음 Phase

- **Phase 02 (Formulas.cs 분리)** — 본 Phase 발견 결과에 따라 scope 조정 가능. 발견 0~3건 = 그대로 진행 / 4건+ = Phase 02 진입 전 plan 갱신.

---

## 📋 박제 (완료 후)

- 보통 등급 = -DONE.md 없음, work-pin + commit message 충분
- 단, 발견 양 8건+ 또는 즉시 봉합 결정 항목 있으면 *복잡 등급으로 자동 상향* → -DONE.md 박음 (위험 깃발 = harness 또는 trust-boundary 가능)

---

## 작업 로그

- 2026-05-22: Phase 정의 박힘 (M4.1 plan 박는 시점)
