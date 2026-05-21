---
owner: youngho
milestone: M3.6
phase: 04
title: 서버 코드 전수조사 (02_Server/ + 98_Shared/ + 99_Tools/)
status: pending
grade: 대규모
risk: trust-boundary
estimated: 1일+ (Coordinator + Team)
domain: server+shared+qa
---

# Phase 04: 서버 코드 전수조사 (02_Server/ + 98_Shared/ + 99_Tools/)

> **상태**: pending
> **마일스톤**: M3.6
> **등급**: 대규모 (3 도메인 — server + shared + qa / 300줄+ 점검 / trust-boundary 깃발 자동 상향)
> **담당**: Coordinator SubAgent + 도메인 Worker 3개 + reviewer

---

## 🎯 목표

**M3 응급 데모 직후 서버 측 전 코드** (네트워킹 / 게임플레이 / 영속화 / 헤드리스 봇 / PacketGenerator / 테스트) 헌법 절대 원칙 5개 위반 점검 + 구조/품질 점검. 발견 사항을 *즉시 봉합* vs *별 Phase 분리* 분기.

**왜 대규모 등급인가**: 3 도메인(server / shared / qa) 동시 점검 + trust-boundary 깃발 (헌법 #3 영역 검증) + 200줄+ 봉합 가능성. Coordinator → Worker 1단계 분해의 *첫 실측 마일스톤*.

---

## ⏪ 사전 조건

- [ ] Phase 02 (헌법/ADR/policies 정합 감사) 완료 — 점검 *시각* 결정
- [ ] Phase 03 (하네스 v1 실측 재조정) 완료 — 점검 *도구*가 신뢰 가능

---

## 📝 작업 내용

### Coordinator 분해 (대규모 등급 진입 직후)
- [ ] coordinator SubAgent 호출 → Phase 04 작업 분해 + 도메인 Worker 위임 계획
- [ ] **도메인별 3~5 시나리오 명세 의무** (γ 6/7회차 학습 정합, plan-auditor P2 #2 봉합) — 영역명 + 원칙명까지가 아니라 *구체 시나리오*까지 박음. 예: "Loop/ — Tick 안 `await` 0건 / GameMap.Tick 동기 DB 0건 / Thread.Sleep 0건 / EnqueueJob 차단 0건 grep 결과 박음". 시나리오 명세 없는 추상 점검 = 결함 누락 위험
- [ ] 분해 결과 사용자 GO 게이트

### Worker 1: server (02_Server/)
- [ ] **Network/** — Listener / Session / Buffers (ServerCore) 헌법 #3 신뢰 경계 검증 + ADR-012 Y2 정합
- [ ] **Handlers/** — Phase 03 분리(M3 Phase 03) 후 핸들러 패턴 정합 / IPacketHandler 인터페이스 / decode-only 약속
- [ ] **Loop/** — Tick scheduler 헌법 #5 (await/Task.Delay/Thread.Sleep/동기 DB 0건) 검증
- [ ] **Maps/** — GameMap actor 패턴 + `EnqueueJob` / IsClosing skip / broadcast race 패턴 정합
- [ ] **Combat/** — M3 응급 단순화 (M4 backlog 분리) 정합 확인 + EnemyKind 통합 패턴 정합
- [ ] **Persistence/** — DbContext + write queue (M5 진입 전 baseline, 현재 비어있을 가능성)
- [ ] **Program.cs** — DI / Serilog 박힘 정합

### Worker 2: shared (98_Shared/)
- [ ] **Protocol/Generated/GenPackets.cs** — PacketID 1~15 stable + ProtocolVersion.Current = 3 (Phase 06 bump 후 v3 유지) 정합
- [ ] **PDL.xml** — append-only ID 정합 + bool/string 타입 결함 fix(M3 Phase 02) 반영
- [ ] **GameData/Formulas.cs / Constants.cs / Tables/** — M2+ 채워질 예정 항목 baseline (현재 비어있을 가능성)

### Worker 3: qa (02_Server/GameServer.Tests/ + 99_Tools/headless-bot/ + 99_Tools/PacketGenerator/)
- [ ] **GameServer.Tests/** — 단위 테스트 커버리지 (M3 baseline 160 passed / 0 failed / 1 skipped 정합)
- [ ] **headless-bot/** — M3 backend smoke 2종 (EmergencyCombatSmoke + BossStageClearSmoke) 정합
- [ ] **PacketGenerator/** — `noManager` 기본값 반전(M3 Phase 01) + bool/string 결함 fix 정합 + redundant `--no-manager` 인자 호환성 정합

### reviewer 자동 호출 (Tier 2-A)
- [ ] 5축 점검 통합 (헌법 / ADR / ARCHITECTURE / 테스트 / 도메인 패턴)
- [ ] 발견 사항 우선순위 분류 (P0/P1/P2)

### 봉합 분기
- [ ] P0 (헌법 절대 원칙 위반) → 본 Phase 즉시 봉합 의무
- [ ] P1 (큰 구조 결함) → 별 Phase 또는 M4 진입 전 후속 봉합 Phase 신설 검토
- [ ] P2 (스타일 / 주석 / 미사용) → 본 Phase 묶음 봉합 또는 M4 진입 시 자연 정리

---

## ✅ 완료 조건

- [ ] Coordinator 분해 결과 박힘 + 사용자 GO
- [ ] Worker 3개 작업 결과 박힘 (서버/공유/qa 도메인별)
- [ ] reviewer 5축 점검 결과 박힘 (P0/P1/P2 분류)
- [ ] P0 발견 0건 또는 본 Phase 즉시 봉합 commit 박힘
- [ ] P1 발견 = 별 Phase 또는 후속 봉합 plan 박힘
- [ ] `dotnet build` green 유지
- [ ] `-DONE.md` 박음 (대규모 등급) + 5단계 보고 5 라벨 박힘

---

## 🧪 테스트

**자동**:
- `dotnet build Dawnholder.slnx` green
- `dotnet test`는 본 머신 SAC On 차단 — Cloud Codex 위탁 (별 환경 검증 후속)

**수동**:
- M3 headless smoke 2종 정합 확인 (BinaryReader + reflection으로 본 머신에서 dummy 실행 가능?)

---

## 📚 학습 포인트

- **Coordinator → Worker 분해 첫 실측** — 대규모 등급 처리 패턴이 정합하게 작동하는지. 분해 비용 vs 가치 가시화
- **3 도메인 동시 점검** — server / shared / qa 경계가 명확한지 + Worker 권한 범위 정합 (server Worker가 98_Shared/ 읽기는 가능, 쓰기는 shared Worker)
- **P0/P1/P2 분류 정신** — 발견 사항 우선순위 차등 = *본 Phase 내 처리* vs *별 Phase 분리* 의사결정 패턴. 한국 게임 회사 *백엔드 트리아지* 어필
- **trust-boundary 자동 깃발 발동** — `risk-detector.sh`가 02_Server/ 점검에 자동 발동. 본 Phase 등급 = 복잡 → 대규모 자동 상향 정합

---

## ⚠️ 함정 / 주의사항

- **Worker 권한 범위 X 위반 위험** — server Worker가 98_Shared/ *쓰기* 시도 금지. 분해 시 명시
- **본 Phase는 *점검* 위주, 큰 변경 X** — P0 외 봉합은 별 Phase 분리 의무. 본 Phase 내 욱여넣으면 변경 폭 폭증 → 등급 상향 자동 발동
- **dotnet test 본 머신 차단** — 본 Phase 검증은 build green까지. test 실측은 Cloud Codex 위탁 별 시점
- **Persistence/ + GameData/ 비어있음 정합** — M5/M4 진입 전이라 baseline 상태가 *비어있음*인 게 맞을 수 있음. "비어있다 = 결함"이 아님 (PRD 마일스톤 정합)

---

## ➡️ 다음 Phase

- Phase 06 (외부 리뷰 4건 흡수 + 종합 마감) — Phase 04 + 05 둘 다 끝나야 진입

---

## 📋 박제 (완료 후)

- 등급 대규모 → **`-DONE.md` 박음 + 5단계 보고 5 라벨 박힘** (🎯 무엇 / 🤔 왜 / 🛠️ 어떻게 / 🧪 테스트 / ➡️ 다음)
- MD + HTML 이중 박음은 Phase 06 종합 마감 보고에서 (본 Phase는 MD만)
- 학습 키워드 후보:
  - `coordinator-worker-decomposition-first-instance` (★★★ 첫 실측)
  - `triage-priority-pattern-p0-p1-p2`
  - `worker-permission-boundary-physical-enforcement`
