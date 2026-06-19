---
milestone: M7.6
title: Architecture Cleanup — 아키텍처 논리 정리 (M8 영속화 전 토대)
owner: youngho
status: planning
grade: 대규모
depends_on: M7.5 (loop-harness, merged #116)
blocks: M8 (DB persistence)
---

# M7.6 — Architecture Cleanup (아키텍처 논리 정리)

> 근거: [`../../../00_Document/reviews/2026-06-19-architecture-logic-audit.html`](../../../00_Document/reviews/2026-06-19-architecture-logic-audit.html) (UltraCode 34에이전트 감사)
> 목적: M8(DB 영속화) 진입 전, 책임이 엉뚱한 계층에 굳은 자국을 바로잡아 깨끗한 영속화 경계를 확보.

## 🎯 마일스톤 목표

감사에서 검증 통과한 리팩토링 제안 8건을 적용해 **도메인 경계·책임 위치·계층 정합**을 회복한다. 게임 *동작*은 불변(리팩토링 — 외부 행동 0 변경), 구조만 정리. 특히 **preM8 2건**(QuestRegistry 분리·치트 게이트)이 M8 스키마 오염을 막는 핵심.

## 📏 Baseline (실측, 2026-06-19)

- WSL2 회귀 = **657/0/5 green** (M7 마감 기준, build 0err/0warn).
- 게임 동작 불변이 원칙 → 각 Phase done 판정 = **WSL2 회귀 green(테스트 수 비감소) + reviewer 🔴 0** (ADR-029).
- 8건 file:line은 감사 리포트에 실측 박힘 (추측 아님).

## 🚫 비목표 (이 마일스톤이 안 하는 것)

- 새 기능 0 (순수 리팩토링). 게임플레이 변경 X.
- 미검증 low 16건은 범위 밖 (감사 리포트 "범위 밖" 절 — 별도 sweep 후보).
- Unity prefab/scene 저작 X (이펙트 통합은 *코드* 정리, 외관 동치는 영호 육안 트랙).
- Protocol.Version bump X (와이어 포맷 불변 — 전부 서버/클라 내부 구조 정리).

## 🗂️ Phase 분해 (6)

| Phase | 내용 | 감사 항목 | 등급 | risk |
|---|---|---|---|---|
| **P01** | QuestRegistry 분리 (PartyRegistry → Quest/) | #1 (preM8) | 복잡 | trust-boundary (권위 퀘스트 상태) |
| **P02** | 치트 게이트 빌드 종속 (#if DEBUG 등록) | #2 (preM8) | 복잡 | trust-boundary |
| **P03** | 파티 오케스트레이션 추출 (GameSession → PartyFlow) | #3 | 복잡 | trust-boundary (세션 lifecycle) |
| **P04** | 사망 진입점(HandlePlayerDeath) + 전제조건 게이트 선언화 | #8 + #5 | 복잡 | trust-boundary (게이트=신뢰경계) |
| **P05** | dead code 정리(JobQueue/PriorityQueue/SendBuffer + 문서) + 봇 ProbeBase | #4 + #7 | 보통 | — (저위험, 도구/dead) |
| **P06** | 이펙트 스폰 통합(클라) + 통합 회귀 + 마감 | #6 | 복잡 | unity 외관(육안=영호 트랙) |

**위험 깃발 → 모델**: P01~P04 = `복잡 + trust-boundary` → 구현 Worker **Opus** ([opus-routing-by-complexity]). P05 = Sonnet. P06 = Sonnet(코드) + 영호 육안.

## 🔗 의존성 그래프

```
P01 (QuestRegistry 분리) ─┬─→ P03 (파티 추출 — 같은 PartyRegistry 건드림)
                          └─★hard→ P02 (DebugCompleteQuest가 P01에서 QuestRegistry로 이동
                                       → P02 치트 경로 호출 대상 클래스가 바뀜. P01 *필수 선행*)
P04 (사망/게이트) ── 독립
P05 (dead code/봇) ── 독립
모두 → P06 (이펙트 + 통합 회귀 + 마감)
```

> **★ P01→P02 = hard 의존** (plan-auditor 봉합): `PartyRegistry.DebugCompleteQuest`(line 248)가 치트 경로(`GameSession.SubmitCheatCommand:494`)의 *유일* 호출 대상. P01이 이를 QuestRegistry로 옮기므로 **P01 done 조건에 "치트 경로가 참조할 `DebugCompleteQuest` 신규 위치(QuestRegistry) 명시" 포함**, P02는 그 위치를 #if DEBUG 게이트로 감쌈.

순서: **P01 → P02 → P03 → P04 → P05 → P06** (P01→P02 hard 필수. P04·P05는 독립이라 순서 유연).

## ✅ 마일스톤 완료 조건

- [ ] 8건 감사 항목 적용 (또는 적용 불가 사유 박제)
- [ ] WSL2 회귀 green — 테스트 수 657 비감소 (동작 불변 증명) + build 0err/0warn
- [ ] 게임 동작 회귀 0 (리팩토링이라 외부 행동 불변 — 봇 시나리오 통과)
- [ ] reviewer 🔴 0 (각 Phase)
- [ ] Protocol.Version 불변 확인 (와이어 포맷 0 변경)
- [ ] M8 영속화 경계가 깨끗해짐 (preM8 2건 = QuestRegistry·치트게이트 완료)
- [ ] **P02 위반 봉합 *별도* 증명** (plan-auditor 봉합): Release 구성 빌드 후 `C_CheatCommand`가 HandlerRegistry에 미등록 = unknown PacketID drop 확인 (테스트 또는 빌드 산출 grep, *트랜스크립트에 박힘*). ⚠️ WSL2 회귀(통상 DEBUG)는 F8 유지가 정상이라 green이어도 봉합 증거 0 — 위반 제거는 별도 정량 조건으로.

## ⚠️ 핵심 함정

- **리팩토링 = 동작 불변**. 테스트 수 감소/동작 변경은 리팩토링 실패 신호. 매 Phase WSL2 회귀로 증명.
- **★ "회귀 green ≠ 위반 봉합" 함정** (plan-auditor): 헌법 위반을 봉합하는 Phase(P02)는 *봉합 전에도* 회귀가 green임. 회귀는 "기존 테스트가 잡던 동작 불변"만 증명하지 "위반 제거"를 증명 못 함 → 위반 봉합 Phase는 *위반 제거 증명*(예: P02 Release drop 확인)을 done에 *별도로* 박아야 loop 평가자가 "초록불=끝" 오판 안 함.
- **★ P05 dead code = 동작 불변, 단 PriorityQueue 실버그 수정은 예외** (plan-auditor 🟡): 삭제(동작 불변) vs `Pop()` line 59 실버그 수정(동작 변경)은 다른 성격. P05 Phase 정의에서 "삭제 vs 수정"을 설계 분기로 명시 — 수정 선택 시 회귀 테스트 동반.
- **★ P04 부활 동치 증명** (plan-auditor 🟡): `HandlePlayerDeath` 정리의 "사망→풀피 부활" 동치를 어느 봇 시나리오가 증명하는지 P04 정의에 명시 (없으면 "동작 불변" 주장이 검증 공백).
- **P01 권위 상태 이동**: 퀘스트(보스 해금)는 권위 상태 — 도메인만 옮기고 검증 로직은 동치 보존. trust-boundary 약화 0.
- **P06 이펙트 통합**: 시각 동치는 AI가 판정 X → 영호 Play 모드 육안 (버킷 b).
- **append-only**: 감사 리포트(`reviews/`)는 동결 — 정정은 새 리뷰로.

## ➡️ 다음 (M7.6 마감 후)

M8 = DB 영속화 재개 (`feature/m8-persistence` 브랜치 보존됨). 깨끗해진 도메인 경계 위에 EF Core + LocalDB + 큐드 라이터.
