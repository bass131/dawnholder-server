---
owner: youngho
milestone: M3.6
phase: 02
title: 헌법 + ADR + policies 정합 감사
status: done
grade: 복잡
estimated: 2~3h
domain: cross
---

# Phase 02: 헌법 + ADR + policies 정합 감사

> **상태**: pending
> **마일스톤**: M3.6
> **등급**: 복잡 (2 도메인 — 헌법 + 코드 cross / ~100~200줄 봉합 예상 / 일부 비가역)
> **담당**: reviewer SubAgent + 영호 단독 봉합

---

## 🎯 목표

ADR 22개 + policies 8개 + 헌법 절대 원칙 5개가 **코드/Hook/SubAgent 정의와 정합한지 감사**. 헌법 #2 ProtocolVersion handshake / 헌법 #4 Shared Code Discipline / Handlers/ 폴더처럼 *주석 약속은 박혀있는데 코드는 안 박힌* 가짜 약속 패턴 발본.

**핵심 정신**: 옛 운영에서 3번 발본된 가짜 약속 봉합 시리즈 (Phase 03 Hook as Policy Physical Enforcement)의 *4번째 발본*은 *주기적 감사*가 잡아야 함. 본 Phase가 그 감사 첫 박힘.

---

## ⏪ 사전 조건

- [ ] Phase 01 완료 (인프라 작동 baseline 확정)

---

## 📝 작업 내용

### ADR 22개 × 코드 상태 매핑
- [ ] tech-stack/ 9개 (ADR-001~012, ADR-017) — 약속한 스택/도구가 *실제* 사용 중인지
  - ADR-002 PDL — `99_Tools/PacketGenerator/` + `98_Shared/Protocol/Generated/GenPackets.cs` 실재 확인
  - ADR-010 Shared DLL — `Shared.dll` + Embedded PDB 정합
  - ADR-012 Y2 socket 분리 — `02_Server/Network/` + `04_ClientNet/` 양쪽 존재
- [ ] gameplay/ 4개 (ADR-006~009) — *결정 X 위배 코드*가 박혔는지 (예: ADR-008 단일 서버 — 분산/샤딩 코드 없음)
- [ ] harness/ 9개 (ADR-013~022) — `-DONE.md` 페어 / Post-flight 게이트 / Notion 3자 분업 / reviewer / hook env / UI Scene 분리 / 새 하네스 v1 모두 *실재* 박힘 확인
- [ ] 결과 표: ADR × {완전 정합 / 부분 정합 / 가짜 약속 / supersede 누락}

### policies 8개 × Hook + 본문 정합
- [ ] `pr-and-merge-gate.md` (PR #43 박힘) — 4-D/4-E/4-F 게이트가 `commands/session/end.md`에 실재 박혔는지
- [ ] `grade-and-risk.md` — 위험 깃발 3종 (trust-boundary / irreversible / unity-asset)이 `risk-detector.sh` Hook에 실재 검출 패턴 박혔는지
- [ ] `subagent-routing.md` — 풀 9 명세가 `.claude/agents/` 9 파일과 정합 (knowledge-gc 포함)
- [ ] `pin-and-done.md` — work-pin ↔ CONTEXT 옵션 C 게이트가 `session/end.md`에 실재 박혔는지
- [ ] `reporting-format.md` — 등급별 5단계 보고 조건부화가 `phase-gate-validator.sh`에 박혔는지 (대규모 등급만 5단계 라벨 검사)
- [ ] `knowledge-system.md` — 트랙 A/B 분리 + AI 자율 박제 차단 게이트가 `_usage.md`에 박혔는지
- [ ] `doc-thresholds.md` — 220/350줄 임계 + 헌법 예외가 실제 본문 길이 정합
- [ ] `review-tiering.md` — Tier 2-A/2-B 분리 + 자동 호출 트리거 명세

### 헌법 절대 원칙 5개 × 코드 시연
- [ ] **#1 Server Authority** — `02_Server/`에 권위 검증 박힘 / `03_Client/`에 권위 변경 코드 없음
- [ ] **#2 Protocol is Sacred** — PDL stable ID + ProtocolVersion handshake 코드 실재
- [ ] **#3 Trust Boundary** — `02_Server/Handlers/` + GameMap 6단계 검증 코드 실재
- [ ] **#4 Shared Code Discipline** — `98_Shared/` DLL 컴파일 + `core.hooksPath` pre-commit 박힘
- [ ] **#5 No Blocking Calls in Server Game Loop** — `02_Server/Loop/` + GameMap.Tick에 `await Task.Delay` / 동기 DB / `Thread.Sleep` 0건

### 봉합 분기
- 단순 결함 (rename / 한 줄 정정) → 본 Phase 내 즉시 봉합
- 복잡 결함 (코드 큰 변경 필요) → Phase 04/05로 분리 또는 별 Phase 신설
- 헌법/ADR 자체 정정 → 본 Phase 내 직접 (영호 단독 통제 영역)

---

## ✅ 완료 조건

> **정합 정신**: 완료 조건은 *발견 결과*가 아니라 *박힘 + 결정*. 발견 N건이어도 *분기 결정* 박혀있으면 통과 (plan-auditor P2 #1 봉합 정합).

- [ ] **ADR 22개 × 정합 4상태 표 박힘** (완전/부분/가짜/supersede 누락) + 각 결함의 *봉합 분기 결정* 박힘 (즉시 / Phase 04~05 위임 / 별 Phase / 보류)
- [ ] **policies 8개 × Hook 정합 표 박힘** + 결함의 봉합 분기 결정
- [ ] **헌법 절대 원칙 5개 × 코드 시연 증거 박힘** (파일 경로 + line:number, "OK 표시"만으로 부족)
- [ ] 즉시 봉합 결정 = 한 commit으로 박음 (본 Phase 내)
- [ ] Phase 04/05 위임 결정 = 해당 Phase 정의 `.md` 작업 항목 추가 (본 Phase에서 미리 위임)
- [ ] 별 Phase 결정 = 새 Phase 정의 `.md` 생성 또는 work-pin "별 시점 대기 액션" 추가
- [ ] reviewer SubAgent 자동 호출 (10+ 줄 변경 + cross-cutting → 무조건 호출)
- [ ] `-DONE.md` 박음 (복잡 등급)

---

## 🧪 테스트

**자동**:
- Hook 정합 시뮬 — `shared-discipline-guard.sh`가 98_Shared/ 변경 시 실제 차단하는지 본 Phase 안 임시 Edit으로 확인 (commit X)
- `dotnet build` green 유지 (봉합이 빌드 깨지면 즉시 원복)

**수동**:
- ADR 본문 × 실재 코드 grep 매핑 (예: ADR-012 → `04_ClientNet/Connector.cs` 존재)
- 헌법 #1~5 × 코드 시연 line:number 박음

---

## 📚 학습 포인트

- **약속 박힘 ≠ 약속 실재** — 옛 운영 3번 발본 시리즈(헌법 #2 #4 + Handlers/) 정신. 본 Phase는 그 정신을 *주기적 감사*로 박음
- **가짜 약속 발본의 정량 검증** — line:number 박음 의무 = "그냥 있어요" X, "여기 박혔어요"
- **봉합 분기 = 등급 진화** — 단순 결함은 본 Phase, 복잡 결함은 다른 Phase. 등급별 양식 부담 정합

---

## ⚠️ 함정 / 주의사항

- **헌법 정정은 *마지막 수단*** — ADR 22개 + policies 8개 정합 깨진 게 *코드 결함*인지 *헌법 결함*인지 우선순위 헌법 > ADR > policies (`CLAUDE.md` 표 정합). 헌법 변경은 [H] CHANGELOG entry + 디스코드 공지 의무
- **본 Phase는 *발견 위주*** — 발견 후 *즉시 봉합 vs 별 Phase* 분기는 등급으로. 복잡 결함을 본 Phase 내 욱여넣으면 등급 자동 상향 → 양식 부담 ↑
- **reviewer는 코드 스타일 Scope 제외** — 헌법/ADR/도메인 패턴 5축만 점검 (Roslyn analyzer 미도입 정합)

---

## ➡️ 다음 Phase

- Phase 04 (서버 코드 전수조사) + Phase 05 (클라 코드 전수조사) — 본 Phase 03와 함께 끝나야 진입

---

## 📋 박제 (완료 후)

- 등급 복잡 → **`-DONE.md` 박음** (요약 + 사실 + 학습 키워드 후보)
- 5단계 보고 X (복잡 등급은 면제, 대규모만 의무)
- Tier 2-A reviewer 자동 호출 결과는 -DONE.md "AC 검증 결과" 섹션에 박음
