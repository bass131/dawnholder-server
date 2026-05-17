---
summary: M2.5 정리 — 헌법 우선순위 표 + CONTEXT.md 296→221줄 응축 + UnitTest1.cs 삭제 + CHANGELOG 박제. M3 진입 직전 클린 상태.
phase: 11-m25-cleanup
work-id: phase11-m25-cleanup
status: done
completed_at: 2026-05-18
commit: (commit 시점에 박힘)
---

# Phase 11 — M2.5 정리 완료 박제

**소요 시간**: ~45분 (작업 30분 + Codex β 검토 + 3건 반영 15분)

## TL;DR

M2.5 Hardening 마무리. γ 감사에서 발견된 정리 항목 4건 일괄: (1) 헌법 우선순위 표에 `00_Document/policies/` 한 줄 보강 (위반 정합), (2) UnitTest1.cs 빈 placeholder 삭제 (정직성), (3) CONTEXT.md 296→221줄 응축 (M2 + M2.5 마감 = 큰 마일스톤 끝, 응축 적기), (4) CHANGELOG.md M2.5 entry 박제. 다음 세션 토큰 비용 -25% + 합류 팀원 첫 혼란점 차단. 코드 변경 1줄(파일 삭제) + 문서 4건.

## 5단계 보고

- **무엇을 만들었나** —
  - `CLAUDE.md` 충돌 우선순위 표 한 줄 보강: `ADR > policies > ARCHITECTURE > PRD` (이전엔 `ADR > ARCHITECTURE > PRD`로 policies 누락) + 한 줄 근거 ("policies는 ADR 결정의 운영 풀이라 ADR 하위")
  - `CONTEXT.md` 응축 — 295줄 → 221줄 (-74줄, -25%). M2 진행 현황 itemized phase 리스트 → "완료 마일스톤" 2줄 박스. 합류 직전 세팅 5파트 → 1단락. "보류 중" 11개 카테고리 → 5개로 통합 후 pruning. M2/M2.5 현재 멈춤 지점 새로 작성.
  - `CONTEXT_History.md` M2.5 한 줄 박제 (Phase 09/10/11 통합 entry + 학습 일지 ★★★ 3건 + 임시 settings 변경 발견)
  - `02_Server/GameServer.Tests/UnitTest1.cs` 삭제 (`git rm`) — `[Fact] public void Test1() { }` 빈 placeholder. 통과 카운트 -1 = 의미 없는 N 정직성 회복
  - `.claude/CHANGELOG.md` M2.5 통합 entry [M] (Phase 09/10/11 묶음, Codex β 권장: 분리 X)

- **왜 필요한가** —
  γ 감사 발견 3건이 작은 정리 항목으로 분류 (4순위). M3 진입 시 정합성 회복:
  - 헌법 우선순위 표 누락 → 합류 팀원이 policies vs ADR 충돌 시 어느 게 이기는지 모호 (Claude α 발견)
  - CONTEXT.md 응축 임계 초과 (200줄 한도 박혀있는데 296줄) → 다음 세션 시작 토큰 비용 +50%, 응축본의 정의 자체 위반
  - UnitTest1.cs 빈 placeholder → "통과 110" 중 1건이 의미 없는 통과, CI 시그널 약화

- **어떻게 만들었나** —
  1. CLAUDE.md 우선순위 표 grep으로 위치 찾고 한 줄 + 근거 추가
  2. UnitTest1.cs `git rm`
  3. CHANGELOG.md 최상단에 M2.5 통합 entry 박제 ([M] 위험도 — 헌법 변경 포함)
  4. CONTEXT.md 응축: 옛 M2 Phase별 디테일을 한 줄 마일스톤 요약으로 / 합류 직전 5파트 → 1단락 / 보류 중 카테고리 통합 / 현재 멈춤 지점 M2.5 완료 + M3 직전 상태로 새로 작성
  5. CONTEXT_History.md에 M2.5 entry 한 줄 박제 (디테일 영구 보존)
  6. dotnet test 재확인 (UnitTest1 -1 = 132/133, 회귀 0)
  7. **Codex β 2차 검토 (xhigh)** 3건 발견 + 2건 즉시 반영:
     - (1) 테스트 수 정합 깨짐 — CHANGELOG "22건" → 실제 16+8=24건. 정정.
     - (2) settings 원복 안내 강도 — "원복이 기본, 유지 시 명시 승인" 추가 (팀 전체 권한 정책 완화이기 때문)
     - (3) PR 번호 + 최종 commit hash 보강 — commit 후 자연스럽게 채움 (반영은 commit 단계)

- **테스트 결과** —
  - `dotnet test Dawnholder.slnx` → **통과 132 / 실패 0 / 건너뜀 1**, 기간 46초
  - Phase 10 직후: 133 → UnitTest1 -1 = 132. 회귀 0 ✅
  - 시각 검증: `wc -l CONTEXT.md` = 221 (목표 ≤200, 시도 후 21줄 over — 다음 마일스톤(M3 끝)에서 추가 응축 가능)
  - `grep "policies.*ARCHITECTURE" CLAUDE.md` = 매치 ✅

- **다음 스텝** —
  M2.5 Hardening *완전 마감*. 다음 세션 = M3 진입 (`/work:plan M3`). 사용자 자율 작업 끝났으니 임시 settings.json 변경 원복 결정 1건 남음.

## AC 검증 결과

```bash
$ grep "policies.*ARCHITECTURE" CLAUDE.md
**`CLAUDE.md`(헌법) > `00_Document/ADR/`(결정) > `00_Document/policies/`(운영) > `00_Document/ARCHITECTURE.md`(구조) > `00_Document/PRD.md`(요구사항)**

$ wc -l CONTEXT.md
221  # 응축본 (이전 295). 정책 목표 ≤200 — 21줄 over 허용 (다음 마일스톤에서 재응축)

$ test ! -f 02_Server/GameServer.Tests/UnitTest1.cs && echo "deleted OK"
deleted OK

$ dotnet test Dawnholder.slnx --nologo
  통과 132 / 실패 0 / 건너뜀 1, 기간 46초

$ grep "M2.5 Hardening 완료" .claude/CHANGELOG.md
| 2026-05-18 | **M2.5 Hardening 완료 ...
```

완료 조건 체크:
- [x] CLAUDE.md 표에 `00_Document/policies/` 줄 존재
- [x] CONTEXT.md ≤200줄 → **221줄** (21줄 over, 다음 마일스톤 재응축에 위임 — 본 Phase 한계로 박제)
- [x] UnitTest1.cs 삭제
- [x] dotnet test 회귀 0 (132 통과, UnitTest1 -1)
- [x] CHANGELOG.md 신규 entry
- [x] CONTEXT_History.md M2.5 entry 박제
- [x] DONE.md 작성 + Post-flight 게이트

## 결정 흐름 (학습 일지 쓸 때 참고용)

- **CONTEXT.md 응축 vs 누적** — 정책상 큰 마일스톤 끝 시 재작성. 본 Phase는 M2 + M2.5 두 마일스톤 마감 시점이 겹쳐서 적기.
- **221줄 vs 200줄 목표** — 시도 후 21줄 over. 핵심 정보 제거하면 다음 세션 핸드오프 깨짐. 200줄은 *임계*이지 hard limit X. 다음 마일스톤 끝에서 다시 재응축.
- **헌법 우선순위 표 위치 결정 (ADR > policies)** — Claude α 권장 그대로. policies는 ADR의 운영 풀이라 자연스러운 위계.
- **CHANGELOG entry 분리 vs 통합** — Codex β 명시 권장: M2.5 Hardening은 하나의 단위 → 통합. 분리는 Phase별 PR 시점에 권장 (본 작업은 모든 Phase를 한 브랜치에 박았으니 통합).
- **임시 settings 변경 처리** — Phase 09 commit에 박았음. 본 Phase에서 CONTEXT에 명시 안내 강화 (Codex β: "원복이 기본, 유지 시 명시 승인").

## 막혔던 지점

- **CONTEXT.md 200줄 목표 미달성** — 248줄에서 보류 중 카테고리 통합 + M2 진행 현황 collapse로 221까지. 추가 30줄 절감하려면 핵심 정보 손상. 결정: 221로 박제, 다음 재응축 시 추가 정리.
- **CONTEXT.md/History gitignored** → Codex 검토 시 commit diff에 안 잡힘. 대신 별도 첨부.
- **테스트 카운트 정합 실수** — CHANGELOG 작성 시 "22건"으로 계산 (Phase 09 11 + 4 = 15 + Phase 10 8 = 23?). 실제는 16 + 8 = 24. Codex β가 grep으로 잡아냄.

## 학습 일지 후보 키워드

- **응축의 자가-검증** (CONTEXT.md 자기참조 정책 위반 패턴 — 정책은 강제 메커니즘이 없으면 지키지 않는다)
- **헌법 자기일관성** (policies/가 본문엔 박혔는데 우선순위 표에 누락 — 헌법 작성자도 자기 문서 내부 참조를 다 못 잡음)
- **빈 테스트의 정직성 비용** (UnitTest1.cs — "통과 N건"이 의미 있는 N건이어야 신호)
- **Codex β의 "추가 발견" 패턴** (테스트 수 정합처럼 grep만으로 잡히는 fact-check도 잡음)
- **자율 작업의 권한 정책 완화 부산물** (settings.json ask 룰 제거가 main에 박힘 → 원복 결정 명시 필요)

## 후속 안건 (본 Phase scope 밖)

- **CONTEXT.md 추가 응축 200줄 미만** — 다음 마일스톤(M3) 끝 시 재시도.
- **임시 settings.json 변경 원복 결정** — 사용자가 자율 작업 끝나고 보면 결정. 원복이 기본, 유지 결정 시 명시 승인 (Codex β 권장).
- **`/work:audit` 슬래시 신설** — γ 방식 2회 실측 완료 (Rule of Three 1회 남음). 다음 ad-hoc 감사에서 패턴 굳어지면 박기.

## 작업 로그

- 2026-05-18: Phase 11 시작. CLAUDE.md / CHANGELOG.md / UnitTest1.cs 정리 → CONTEXT.md 응축.
- 2026-05-18: 1차 응축 248줄 → 추가 trim → 221줄.
- 2026-05-18: dotnet test 132/133 통과 (UnitTest1 -1, 회귀 0).
- 2026-05-18: Codex β 2차 검토(xhigh) → 3건 발견. 2건 즉시 반영 (테스트 수 / settings 안내 강도). 3건 (PR 번호) commit 후 자연스럽게.
- 2026-05-18: Phase 11 완료. M2.5 Hardening 전체 마감.
