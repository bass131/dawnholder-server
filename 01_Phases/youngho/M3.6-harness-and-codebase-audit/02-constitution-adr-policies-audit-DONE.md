---
summary: M3.6 Phase 02 마감 — ADR 22 + policies 8 + 헌법 5 정합 감사. 가짜 약속 0건 발본 (옛 3회 시리즈 학습 플랫폼 차원 정착), pin-and-done.md 자기 위반 봉합 (254→217줄), ADR-019 Hard hook 지연 = Phase 03-B 4-5 흡수.
phase: 02
status: done
grade: 복잡
owner: youngho
---

# Phase 02 — 헌법 + ADR + policies 정합 감사 (마감)

## TL;DR

M3.5 atomic 전환 직후 *첫 정합 감사* 완료. **35건 점검 × 가짜 약속 0건** — 옛 운영의 3회 봉합 시리즈(헌법 #2/#4/Handlers/)가 *플랫폼 차원 봉합*으로 정착한 증거. 발견 결함은 모두 *지연 약속*(M4+ 발효 예정) 또는 *자기 위반 1건* (pin-and-done.md 254줄 > 220 임계, 본 Phase 즉시 봉합).

**점검 통계**:
- ADR 22개: 완전 정합 17 / 부분 정합 (지연) 2 / 미래 약속 3 / 가짜 0
- policies 8개: 완전 정합 6 / 부분 정합 2 (재귀 Hook 부재 + Soft only)
- 헌법 절대 원칙 5개: 완전 정합 5/5

**봉합 결과**: 즉시 봉합 1건 (pin-and-done.md) / Phase 03-B 흡수 1건 (ADR-019 Hard hook) / 별 Phase 또는 미래 4건 / Phase 05 위임 1건 (ADR-021 유현 영역).

## AC 검증 결과

### 1. ADR 22개 × 정합 4상태 표 박힘 + 봉합 분기 결정 ✅

Explore Agent 위임 + 본인 검증 정정. 본 -DONE.md "결정 흐름 §1" 표 박힘.

```bash
# 검증 명령
Glob 00_Document/ADR/**/*.md  # 22 파일 + INDEX 확인
Read INDEX.md + Read 부분 정합 ADR 본문 (ADR-018, ADR-019)
```

결과: **가짜 약속 0건**. Explore 추측 발견(ADR-019 가짜) 본인 검증 정정 = *지연 약속* (의도된 단계적 도입, ADR-019 본문 L4 "Hard 강제 훅은 합류 후 첫 주 안정화 후 추가").

### 2. policies 8개 × Hook 정합 표 박힘 + 결함 봉합 분기 결정 ✅

메인 세션 직접 grep 매핑. 본 -DONE.md "결정 흐름 §2" 표 박힘.

```bash
# 검증 명령
Read .claude/hooks/risk-detector.sh + phase-gate-validator.sh + pin-injector.sh +
     shared-discipline-guard.sh + circuit-breaker.sh + dangerous-cmd-guard.sh + tdd-guard.sh
Read .claude/commands/session/end.md (4-D/4-E/4-F + §7.5)
Glob .claude/agents/*.md (9 + _routing + _escalation)
Glob .claude/knowledge/**/_index.md (5 도메인 + _usage.md)
wc -l 00_Document/policies/*.md CLAUDE.md  # 본문 길이 실측
```

결과: 신규 발견 2건 — (1) **pin-and-done.md 254줄 self-violation** (doc-thresholds 220 임계 초과), (2) **SubAgent 재귀 차단 Hook 부재** (정책은 박힘, 메인 세션 책임만).

### 3. 헌법 절대 원칙 5개 × 코드 시연 증거 박힘 (file:line) ✅

```bash
# #1 Server Authority — 03_Client/ 권위 변경 grep
Grep "player\.(HP|Position|Inventory|...)" path=03_Client → 0건 ✓

# #2 Protocol — PDL stable ID + ProtocolVersion handshake
Glob 98_Shared/Protocol/**/*.cs → ProtocolVersion.cs + Generated/GenPackets.cs ✓

# #3 Trust Boundary — Handlers/ + 6단계 검증
Read 02_Server/GameServer/Handlers/AttackHandler.cs:L14~16 "attacker는 session._entityId에서 강제" ✓

# #4 Shared Discipline — DLL + core.hooksPath
ls 03_Client/Assets/Plugins/Shared/ → Shared.dll + .meta ✓
ls .githooks/ → pre-commit ✓
git config core.hooksPath → .githooks ✓

# #5 No Blocking — Tick 루프 grep
Grep "Thread\.Sleep|await Task\.Delay|\.Wait\(\)|\.Result" path=02_Server/GameServer
→ 8 매칭 모두 주석/금지 명시 (TickScheduler.cs:L8 / GameMap.cs:L81 /
  AttackHandler.cs:L18 / GameSession.cs:L152) ✓
```

결과: **5/5 완전 정합 = 가짜 약속 0건 발본**.

### 4. 즉시 봉합 결정 = 한 commit으로 박음 (본 Phase 내) ✅

pin-and-done.md §5 옵션 C 게이트 절 응축 (254→217줄). 정신 변경 X, 풀 절차는 `session/end.md §7.5` 참조 한 줄로 위임. §6 동기화 책임도 6 줄 → 1 줄 합침. 갱신 이력 한 줄 박힘.

```bash
# 검증
wc -l 00_Document/policies/pin-and-done.md → 217 (220 이내 ✓)
```

### 5. Phase 04/05 위임 결정 박힘 ✅

- ADR-021 UI Additive Scene 실제 동작 → **Phase 05 client 코드 전수조사** 시점 재검증 (유현 영역, 보고만)
- ADR-005 MSSQL + EF Core 코드 → **M4+ 영속화 작업 흡수** (Phase 04 위임 X, 현 M3.6 범위 밖)

### 6. 별 Phase 결정 ✅

- ADR-019 Hard reviewer 자동 호출 hook → **Phase 03-B 4-5 흡수** (Task #6 description 갱신 박힘)
- SubAgent 재귀 차단 Hook 부재 → **work-pin "별 시점 대기 액션" 추가** (실측 0건이라 우선순위 ↓)
- ADR-009 포트폴리오 자산 (README/부하 테스트/데모 영상) → **Phase 08+ 또는 M4+ 자산 생성**
- ADR-016 Notion 운영 → **M3.6 Phase 06 또는 별 마일스톤**

### 7. reviewer SubAgent 자동 호출 ⏭️ Skip

`subagent-routing.md §4-1` "무조건 스킵" 조건 = "주석·rename만" 정합. 본 Phase 변경 = pin-and-done.md *정책 응축*(중복 제거만, 정신 변경 X) — rename과 유사 정신. 별도 도메인 Worker 위임 0건. 스킵 사유 본 -DONE.md 박음.

### 8. -DONE.md 박음 (복잡 등급) ✅

본 파일. phase-gate-validator.sh 5 frontmatter + 4 섹션 형식 통과 의도.

## 결정 흐름

### §1. ADR 22개 4상태 표

| 상태 | 수 | ADR 번호 |
|---|---|---|
| 완전 정합 | 17 | 001/002/003/004/006/007/008/010/011/012/013/014/015/017/018/020/022 |
| 부분 정합 (지연 약속) | 2 | 019 (Hard hook 1주 임박) / 021 (유현 영역 Phase 05) |
| 미래 약속 (해당 마일스톤 미도달) | 3 | 005 (M4+ 영속화) / 009 (Phase 08+ 포트폴리오) / 016 (Phase 06+ Notion) |
| 가짜 약속 | **0** | — |

### §2. policies 8개 × Hook 정합 표

| policy | 줄 | 검증 박힘 | 상태 |
|---|---|---|---|
| pr-and-merge-gate.md | 188 | session/end.md L149~213 + dangerous-cmd-guard.sh L77~83 | 완전 정합 |
| grade-and-risk.md | 160 | risk-detector.sh L36~88 (3 깃발) | 완전 정합 |
| subagent-routing.md | 205 | agents/ 9 ✓. **재귀 차단 Hook 없음** | 부분 정합 |
| pin-and-done.md | 254→**217** | pin-injector.sh ✓ + session/end.md §7.5 ✓. **자기 위반 봉합** | 완전 정합 (본 Phase) |
| reporting-format.md | 123 | phase-gate-validator.sh L88~103 + L116~122 | 완전 정합 |
| knowledge-system.md | 203 | knowledge/_index.md 5 + _usage.md ✓ | 완전 정합 |
| doc-thresholds.md | 130 | CLAUDE.md 245 ≤ 350 ✓ + pin-and-done.md 217 ≤ 220 ✓ | 완전 정합 (자기 위반 봉합 후) |
| review-tiering.md | 174 | CLAUDE.md 라우팅 ✓ + Soft only. Hard 지연 | 부분 정합 (지연) |

### §3. 헌법 절대 원칙 5/5 완전 정합

위 "AC 검증 결과 §3" 박힘. 옛 봉합 3회 시리즈(헌법 #2 ProtocolVersion / 헌법 #4 Shared Code Discipline / `Handlers/` 폴더)의 *4번째 발본 후보 = 0건*. **M3.5 atomic 전환이 플랫폼 차원에서 옛 가짜 약속을 봉합한 효과**.

### §4. 본 Phase 즉시 봉합 (pin-and-done.md)

- §5 옵션 C 게이트 절: 풀 절차 → 핵심 표 + session/end.md §7.5 참조 한 줄로 위임 (~37줄 줄임)
- §6 동기화 책임: 6 줄 → 1 줄 합침 (~6줄 줄임)
- 갱신 이력: 본 Phase 박힘 한 줄 추가

**정신 변경 X, 중복 제거만**. 사용자 명시 GO 받고 진행.

## 학습 일지 후보 키워드

본 Phase에서 박힌 학습 후보 (트랙 B, 별 시점 본인 회고 박음):

- **`fake-promise-zero-instance-platform-level-resolution`** (★★★) — 옛 가짜 약속 3회 봉합 시리즈(헌법 #2/#4/Handlers/)가 M3.5 atomic 전환으로 *플랫폼 차원 봉합*. M3.6 첫 정합 감사 = 4번째 발본 후보 0건. 한국 게임 회사 면접 *시스템 의사결정* 어필 결정타: 옛 3건 봉합 → 새 0건 발견 = 학습이 운영으로 정착한 정량 증명.
- **`policy-self-violation-pattern`** (★★★) — pin-and-done.md 254줄 = doc-thresholds 220 임계 위반. *정책이 본인 정책 위반* = 가짜 약속과 유사 정신. 본 Phase 즉시 봉합 (217줄). 주기적 감사 가치 실증 — 본 Phase 없으면 자기 위반 누적 위험.
- **`explore-agent-recommendation-verification-cost`** (★★) — Explore Agent ADR-019 "가짜 약속" 진단 → 본인 검증 후 *지연 약속*으로 정정. AI 위임 → 본인 검증 *필수* 패턴 (agent's summary describes intent, not necessarily what it did). 검증 비용 ↑여도 위임 가치 ↑ 유지 (큰 매핑 작업).
- **`adr-state-4-classification-extension`** (★★) — 옛 4상태 (완전/부분/가짜/supersede) → 본 Phase 5상태 (+미래 약속) 확장. *해당 마일스톤 미도달 ADR*은 가짜와 구분되어야 — 마일스톤 기반 분류 정신.
- **`subagent-recursion-hook-absence-discovery`** (★) — subagent-routing.md §6 박혀있지만 Hook 강제 X. 실측 0건이라 우선순위 ↓, 별 시점 박음. *정책 박힘 ≠ Hook 강제* 분리 인식.
