---
owner: youngho
milestone: M3.6
title: AI Harness 점검 + 프로젝트 코드 전수조사
status: in-progress
grade: 대규모
risk: irreversible
estimated: 3~5일 (총합)
domain: cross
---

# M3.6 — AI Harness 점검 + 프로젝트 코드 전수조사

> **상태**: in-progress
> **시작**: 2026-05-22
> **마감 목표**: M4 진입 전 (별 시점, 본인 호흡)

---

## 🎯 마일스톤 목표

M3.5에서 *옛 운영 100% → 새 운영 100% atomic 전환* 박힌 직후, **새 하네스 v1 + 프로젝트 코드 베이스 양쪽을 정합 점검**하고 M4 진입 전 cleanup 완료.

**두 축**:

1. **하네스 점검** — 헌법/ADR/policies/Hook/SubAgent/Knowledge 정합. 가짜 약속(주석은 박혔는데 코드는 안 박힌) 패턴 발본 + 실측 0건 정책들의 1주차 재조정
2. **코드 전수조사** — 02_Server/ + 98_Shared/ + 03_Client/ + 04_ClientNet/ + 99_Tools/ 헌법 절대 원칙 5개 위반 + 구조/품질 점검

**왜 본 마일스톤이 필요한가**:

- M3.5 atomic 전환은 *옛/새 운영 동시 검증*은 했으나 *새 운영 단독으로 실측 1주 누적*은 없음. M3.5 박힌 `grade-and-risk.md` + `subagent-routing.md` 둘 다 본문에 "M4 진입 후 첫 1주 안에 재조정 예정" 명시 — M3.6이 그 1주가 됨
- M3 응급 데모 직후 코드 베이스는 *시연 위주 단순화* + *별 시점 backlog* 누적 (work-pin "별 시점 대기 액션" 6건). M4 진입 전 정합 cleanup 없이 가면 부채가 M4 작업 위에 쌓임
- 새 PR/머지 게이트(PR #43 박힘) + Coordinator → Worker 분해 패턴 모두 *첫 실측* — M3.6이 그 실측 마일스톤

---

## 📋 Phase 분해 (6개)

| # | Phase | 등급 | 도메인 | 예상 | 담당 |
|---|---|---|---|---|---|
| 01 | Pre-flight — 인프라 작동 baseline | 보통 | cross | 1.5~2h | 메인 직접 (영호) |
| 02 | 헌법 + ADR + policies 정합 감사 | 복잡 | cross | 2~3h | reviewer + 영호 |
| 03 | 하네스 v1 실측 1주차 재조정 | 복잡 | cross | 2~3h | 영호 단독 |
| 04 | 서버 코드 전수조사 (02_Server/ + 98_Shared/) | 대규모 | server+shared+qa | 1일+ | Coordinator + Team |
| 05 | 클라 코드 전수조사 (03_Client/ + 04_ClientNet/) | 복잡 | client | 4~6h | client Worker + reviewer |
| 06 | 외부 리뷰 4건 흡수 + 종합 마감 | 대규모 | cross | 3~5h | Coordinator + Team |

**총 등급 = 대규모** (마일스톤 자체) — 5단계 보고 MD + HTML 이중 박음 의무 (캡스톤 평가 자산, Phase 06에서).

---

## 🔗 의존성 그래프

```
Phase 01 (Pre-flight)
   │
   ├─→ Phase 02 (헌법/ADR/policies 감사) ──┐
   │                                         │ (둘 다 끝나야 04/05 진입 정합)
   └─→ Phase 03 (하네스 실측 재조정)  ──────┤
                                             │
                  ┌──────────────────────────┘
                  │
                  ├─→ Phase 04 (서버 코드 전수조사) ──┐
                  │                                    │ (둘 다 끝나야 06 진입)
                  └─→ Phase 05 (클라 코드 전수조사) ──┤
                                                       │
                                          ┌────────────┘
                                          │
                                          └─→ Phase 06 (외부 리뷰 흡수 + 종합 마감)
```

**병렬 가능**:
- Phase 02 ↔ Phase 03 (둘 다 영호 단독, 토픽 분리)
- Phase 04 ↔ Phase 05 (server/shared vs client 도메인 분리)

**의존성 사유**:
- 01 → 02/03 = 인프라 작동 검증 없이 점검 결과 신뢰 X (Hook이 안 도는지 알아야 false negative 사전 차단)
- 02/03 → 04/05 = 정합 감사 결과가 코드 전수조사 *시각*을 결정 (어떤 약속 검증할지)
- 04/05 → 06 = 코드 발견 사항 + 외부 리뷰 합쳐서 종합 보고 + PR 마감

---

## ✅ 마일스톤 완료 조건

- [ ] Phase 01~06 모두 `status: done` + `-DONE.md` 박음 (복잡/대규모 등급)
- [ ] M3.5 정책 2개 (`grade-and-risk.md` + `subagent-routing.md`)의 "M4 진입 후 1주 안에 재조정" 항목 ≥80% 봉합
- [ ] 헌법 절대 원칙 5개 모두 코드 시연 검증 (가짜 약속 0건)
- [ ] 외부 리뷰 4건 (`Dawnholder-harness-review-2026-05-19.md`) 흡수 또는 별 Phase 분리
- [ ] M3.6 종합 5단계 보고 MD + HTML 이중 박음 (캡스톤 평가 자산)
- [ ] CHANGELOG [H] entry 박음 + 디스코드/슬랙 공지 (모든 팀원 영향)
- [ ] PR 생성 + main 머지 (PR/머지 게이트 4-D/4-E/4-F 첫 실측)
- [ ] work-pin → M4 진입 좌표로 clear/이전

---

## ⚠️ 주의할 약속

- **새 하네스 v1 = 영호 단독 통제** — Phase 02/03은 reviewer/plan-auditor + 영호. 다른 Worker가 헌법/ADR/policies/Hook 수정 X
- **PR/머지 게이트** — Phase 06 마감 PR `gh pr create/merge` = 사용자 명시 GO 의무. CODEOWNERS 거절 시 예외 경로 (사유 박힘 + GO + 박제). PR body literal X
- **유현 영역 변경 = 재논의 시점** — Phase 05에서 발견된 client 측 영역 경계 이슈는 *변경 X, 보고만*. 재논의는 별 시점
- **dotnet test 본 머신 SAC On 차단** — 본 마일스톤 검증은 `dotnet build green`까지만 본 머신에서 확인. test 실측은 별 환경 (Cloud Codex) 위탁
- **새 운영 첫 실측 마일스톤** — 점검 결과 자체가 *학습 자산* (M3.6 종합 5단계 보고 = 면접 "방향성 검증 / 자가 점검" 어필 결정타)

---

## 📚 학습 포인트 (마일스톤 차원)

- **점검 마일스톤의 정합** — 한국 게임 회사 백엔드 실무 = *기능 추가만이 아니라 자가 점검 사이클*이 있음. M3.6 = 그 사이클 첫 박힘
- **새 운영 실측** — *추측 정책*이 *실측 정책*으로 진화하는 과정. M3.5 박힌 시점에 "1주 안에 재조정 예정"이라 명시한 게 M3.6에서 봉합되는 정합
- **외부 리뷰 흡수 패턴** — 별 시점 backlog가 본 마일스톤에 자연 흡수되는 *Rule of Three* 정합 (γ 방식 4~7회 누적 후 슬래시화 정합과 같은 정신)
- **Coordinator → Worker 분해 첫 실측** — Phase 04/06이 대규모 등급 = Coordinator 동원 첫 실측. 분해 비용 vs 가치 가시화

---

## ➡️ 다음 마일스톤

- **M4 — Combat & Map Transition** (진짜 4맵 + 정밀 전투 + lag compensation + portal handoff + 몬스터/보스 정식). M3.6 마감 후 진입.

---

## 갱신 이력

- 2026-05-22 — `/work:plan` 호출로 박힘. 사용자 의도 = "AI Harness 점검 + 프로젝트 코드 전수조사", 대규모 등급 + Coordinator 분해 선택.
