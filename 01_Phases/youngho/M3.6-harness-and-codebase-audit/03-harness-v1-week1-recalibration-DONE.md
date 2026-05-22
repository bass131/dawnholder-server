---
summary: M3.6 Phase 03 마감 (대규모 자동 상향) — 정책 실측 10 항목 + Hook hardening bundle 5건. dangerous-cmd-guard Python 본질 봉합 (β2 13/13 PASS) + HTML 페어 강화 + harness 깃발 신설 + trust-boundary 매처 stale 봉합 (가짜 약속 4번째 발본) + reviewer-auto-trigger Hard hook 신설 (ADR-019 1주 약속 봉합). ADR-019/020 갱신 + setup-steps Python 9단계 + Hook 풀세트 7→8.
phase: 03
status: done
grade: 대규모
owner: youngho
---

# Phase 03 — 하네스 v1 실측 1주차 재조정 + Hook hardening bundle (마감)

## TL;DR

옛 추측 정책의 *첫 실측 사이클* 통과 + Hook 안전망 *본질 봉합* 5건. **Phase 02-A에서 "가짜 약속 0건" 박았던 진단이 본 Phase 진행 중 정정** — `risk-detector.sh` trust-boundary 매처가 실제 경로(`02_Server/GameServer/Handlers/`)와 불일치 발견 = 가짜 약속 4번째 발본. 시뮬 발동 검증 없이는 정합 감사가 *file:line 박힘*까지만 잡는 한계 증명.

**자동 등급 상향 (복잡 → 대규모)**: 옛 추측 복잡 (4-1만 가정)이 실제 5건 Hook 변경 + ADR-019/020 갱신 + setup-steps + CHANGELOG [H] = 대규모 정합. work-pin 자동 상향 사유 박힘. 4-4 harness 깃발 자기 참조 = 솔직한 등급 평가 학습 자산화 ★★★.

**핵심 변경 7건**:
1. dangerous-cmd-guard Python shlex.split 본질 봉합 (false positive 차단)
2. phase-gate-validator HTML 페어 강화 (대규모 등급 의무)
3. risk-detector `harness` 깃발 신설 + trust-boundary 매처 stale 봉합
4. reviewer-auto-trigger 신설 (ADR-019 Hard hook, 1주 약속 봉합)
5. ADR-020 갱신 (Python 3.6+ 의존성 + Windows CRLF 함정 명시)
6. ADR-019 갱신 (Hard hook 박힘 명시)
7. setup-steps/02-common.md Python 9단계 + Hook 풀세트 7→8

## AC 검증 결과

### grade-and-risk §8 5 항목 + subagent-routing §9 5 항목 = 10 항목 실측 ✅

> Phase 정의 본문 "13 항목" 명세 오류 정정 — grade-and-risk §8 본문은 5 항목 (Phase 정의 "8개"는 추측). 실측 5+5 = 10 항목.

| 정책 | 항목 | 실측 | 결론 |
|---|---|---|---|
| grade-and-risk §8 | 줄 수 임계 적정성 | M3.5 Phase 06 = 99 files / +1906 / -5262 대규모 정합 / M3.6 Phase 02 = +217줄 복잡 정합 | 정정 X |
| | false positive 빈도 | risk-flags.txt 145줄 누적, 정량 grep 후속 | 정정 X (후속) |
| | 누락 후보 | `.claude/{hooks,agents,commands}/**` 깃발 X → Phase 03-B 4-4 봉합 | **봉합 완료** |
| | 등급 상향 마찰 | 본 세션 자동 상향 0건 → 본 Phase 03 자동 상향 1건 (자기 참조) | 정정 X (자연) |
| | 처리 패턴 효율 | Explore Agent 큰 매핑 효율 ↑ / 메인 직접 작은 매핑 효율 ↑ | 현 정책 정합 |
| subagent-routing §9 | 위임 false hit | 0건 (Explore Agent 1회 = 큰 매핑 정합) | 정정 X |
| | 재귀 차단 마찰 | 0건 (단 Hook 부재 발견 = work-pin 별 시점) | 정정 X (별 시점) |
| | 에스컬레이션 빈도 | 0건 (Sonnet 실패 0) | 정정 X |
| | plan-auditor 가치 | M3.6 plan 통과 (P0=0/P1=1 옵션 B/P2=2 봉합 = γ 6/7회차 패턴 정합) | **가치 증명** |
| | unity-bridge 효과 | prefab 변경 0건 = 간접 증명 (Phase 08 사고 학습 정착) | **간접 증명** |

### Hook hardening bundle 5건 ✅

**4-1 dangerous-cmd-guard Python shlex.split 본질 봉합**:
- 옛 = bash regex word-boundary 한계 (PR #43 응급 봉합 후속)
- 새 = Python shlex.split 토큰화 + `tr -d '\r'` Windows CRLF 봉합
- β2 매트릭스 시뮬 **13/13 PASS** (5 실행 차단 + 5 PR body literal 통과 + 3 정상 명령 통과)
- 검증 명령:
  ```bash
  for case in "rm -rf /tmp/foo"(exit 2) 'gh pr create --body "...rm -rf..."'(exit 0) ... ; do
    echo $payload | bash .claude/hooks/dangerous-cmd-guard.sh
  done
  ```

**4-2 must-pass / advisory 분리**:
- 검증 결과 = README.md "우회·차단 정책" 표(L43~52)에 이미 분류 박힘. 추가 변경 X
- Phase 03 정의의 옛 분류 "risk-detector = must-pass"는 README와 불일치 (정답 = advisory, risk-detector exit 0 = 알림만)
- 정합 정정 박음 (Hook README.md)

**4-3 phase-gate-validator HTML 페어 강화**:
- 옛 WARNINGS → 새 ERRORS + FAIL=1 (대규모 등급 -DONE.md HTML 페어 의무)
- 옛 자산 (M3.5 대규모 3건 + M3 8건) HTML 페어 X → grandfathered (변경 안 하면 hook 미발동, 자연 회피)
- 시뮬 2/2 PASS (HTML X = 차단 ❌ / HTML 박음 = 통과 ✅)

**4-4 risk-detector 4번째 깃발 `harness` 신설 + trust-boundary 매처 stale 봉합**:
- 신설 깃발: `.claude/{hooks,agents,commands}/**` 변경 시 발동
- **stale 발견**: 옛 매처 `*/02_Server/Handlers/*`가 실제 경로 `02_Server/GameServer/Handlers/` (한 단계 깊음) 누락 = 가짜 약속 4번째 발본
- 봉합: 한 단계 + 두 단계 깊이 매처 흡수 (`*/02_Server/Handlers/*|*/02_Server/*/Handlers/*|*Validation*|*/Network/*Auth*` 풀세트)
- 시뮬 3/3 PASS (Handlers/AttackHandler.cs 발동 / Network/GameSession.cs 발동 / Program.cs 미발동)
- 시뮬 2/2 PASS (.claude/hooks/* 변경 → harness 발동 / .claude/agents/* 변경 → harness 발동)
- 정책 본문 갱신: grade-and-risk.md 깃발 표 3→4

**4-5 reviewer-auto-trigger 신설 (ADR-019 Hard hook)**:
- 옛 Soft (메인 세션 판단) → 새 Hard hook (조건 충족 시 명확한 알림 + 누적)
- 트리거 = 98_Shared/ + 02_Server/Handlers/ + Protocol + GameSession.cs (subagent-routing §4-1 무조건 호출 조건 정합)
- Hook 권한 한계: SubAgent 직접 호출 X, *알림 + 누적*까지. 호출은 메인 세션 책임 (Agent tool, subagent_type=reviewer)
- settings.json PostToolUse Edit/Write 매처에 등록
- 시뮬 5/5 PASS (Protocol/Handlers 발동 / Tests/CONTEXT/03_Client 스킵)
- 정유현 5/16 합류 → 5/22 = "합류 후 첫 주 안정화 후 추가" 약속 봉합 (ADR-019 본문 명시)

### ADR + setup-steps + README + CHANGELOG 정합 갱신 ✅

- **ADR-020** — Python 3.6+ 의존성 명시 + Windows CRLF 함정 박음 (PR #43 암묵 박힘 + Phase 03-B 명시 박힘)
- **ADR-019** — Hard hook 신설 명시 (옛 본문 "합류 후 첫 주 안정화 후 추가" 약속 봉합)
- **setup-steps/02-common.md** — 9단계 Python 3 검증 신설 + 옛 9 → 10 재번호
- **`.claude/hooks/README.md`** — Hook 풀세트 7→8 갱신 + must-pass/advisory 분리 표 + Python 의존성 + CRLF 함정 박음
- **`.claude/CHANGELOG.md`** — [H] entry 박음 (모든 팀원 영향 = Python 의무 + reviewer Hard hook)
- **policies 신선도 한 줄 갱신** — grade-and-risk §8 + subagent-routing §9 첫 실측 사이클 통과 명시 (본문 수정 X)

### 자동 등급 상향 박음 (work-pin) ✅

- 옛 추측 복잡 → 실제 대규모 (5건 Hook + ADR 갱신 + setup-steps + CHANGELOG [H] = 정합)
- work-pin "자동 상향" 사유 박힘
- Phase 03 정의 frontmatter `grade: 대규모` + `risk: irreversible+harness` + `note:` 박힘

## 결정 흐름

### §1. 옵션 A (Python shlex 도입) GO 결정

| 옵션 | 선택 | 사유 |
|---|---|---|
| A. Python shlex.split | ✅ | 본질 봉합 + 학습 자산화 + 미래 부담 ↓. Python 3.14.4 본인 머신 박혀있음 확인 |
| B. bash regex 개선 | ❌ | 완전 봉합 X (whack-a-mole) |
| C. 본 Phase 보류 | ❌ | 약속 지연 누적 |

### §2. Windows CRLF 함정 발견 + 봉합

본 Phase 진행 중 첫 시뮬 5/10 FAIL → set -x trace로 진단:
```
+ CMD=$'rm\r'    ← \r 잔재로 "rm\r" ≠ "rm" 비교 실패
```
봉합: Python text mode `\r\n` → mapfile split → 토큰 끝 `\r`. `| tr -d '\r'` 후처리 박음. 재시뮬 13/13 PASS.

학습 자산 ★★★ — 한국 게임 회사 Windows 환경 백엔드 함정. ADR-020 본문 박음.

### §3. 자기 참조 함정 결정 (4-4)

옵션 (a) 등급 대규모 상향 + 4-4 깃발 박음 + 5단계 보고 의무 선택. 사유:
- 솔직함 정신 (CONTEXT 톤 #3) — 옛 추측이 실제와 다르면 정정
- 자기 참조 함정 학습 자산화 ★★★ — 정책 박는 commit이 자기 정책에 잡히는 패턴
- 5단계 보고 = 캡스톤 평가 자산

### §4. trust-boundary 매처 stale 발견 (4-4 자연 발견)

Phase 02-A "가짜 약속 0건" 진단이 본 Phase 시뮬에서 정정 — 매처는 박혀있는데 실제 경로와 불일치. 봉합 = 한 단계 + 두 단계 깊이 흡수. **정합 감사를 *시뮬 발동 검증까지 확장* 필요** — file:line 매핑만으로는 매처 정합까지 못 잡음.

## 5단계 보고

### 🎯 무엇을 만들었나

새 하네스 v1의 **첫 1주 실측 사이클 통과 + Hook 안전망 본질 봉합 5건**:
1. `dangerous-cmd-guard.sh` Python shlex.split 토큰화 (false positive 봉합)
2. `phase-gate-validator.sh` 대규모 등급 HTML 페어 의무화
3. `risk-detector.sh` 4번째 깃발 `harness` + trust-boundary 매처 stale 봉합
4. `reviewer-auto-trigger.sh` 신설 (ADR-019 Hard hook)
5. ADR-019/020 본문 갱신 + setup-steps Python 9단계 + Hook 풀세트 7→8 + CHANGELOG [H]

### 🤔 왜 필요한가

- **추측 정책 → 실측 정책 진화** — M3.5에서 박힌 grade-and-risk + subagent-routing 본문에 명시된 "M4 진입 후 1주 안에 재조정 예정" 약속이 M3.6 Phase 03-A에서 봉합되는 *정책 생명주기*
- **false positive 봉합** — PR #43 응급 봉합 (eval shlex.quote) → 본질 봉합 (Python shlex.split). bypass 습관화 위험 차단
- **가짜 약속 4번째 발본** — Phase 02-A "0건" 진단이 *시뮬 검증* 없이는 한계. trust-boundary 매처 stale 발견 = 정합 감사 *시뮬까지 확장 필요* 정신
- **1주 약속 봉합** — ADR-019 "합류 후 첫 주 안정화 후 추가" 박힘 = 정유현 5/16 합류 → 5/22 = 1주 임박. 약속 지연 누적 차단

### 🛠️ 어떻게 만들었나

**핵심 결정 패턴**:
1. **본인 자동 진행 GO + 큰 분기만 짚음** — 자기 참조 함정 (4-4) + Python 도입 (4-1) = [H] 변경 영향이라 본인 명시 GO 의무
2. **본질 봉합 정신** — bash regex 한계 → 적절한 공구 교체 (Python shlex). 옛 PR #43 응급 봉합 → 새 Phase 03-B 본질 봉합
3. **시뮬 발동 검증 의무** — Hook 변경 시 β2 매트릭스 시뮬 + 정상 케이스 + 발견 케이스 (CRLF 함정)
4. **자기 참조 함정 인지** — 본 Phase 자체가 *하네스 변경* = harness 깃발 발동. 등급 자동 상향 정합 박음

**기술 스택**:
- Python 3.14.4 shlex.split (Windows 11 + Microsoft Store 박힘)
- bash `mapfile -t TOKENS` + `tr -d '\r'` CRLF 봉합
- POSIX bash + Git for Windows shebang (`#!/usr/bin/env bash`)

### 🧪 테스트 결과

| 시뮬 | 결과 | 비고 |
|---|---|---|
| 4-1 β2 매트릭스 (5 실행 + 5 literal + 3 정상) | **13/13 PASS** | CRLF 봉합 후 |
| 4-3 HTML 페어 (없으면 차단 / 박음 통과) | **2/2 PASS** | 옛 자산 grandfathered (변경 X 자연 회피) |
| 4-4 harness 깃발 (.claude/hooks + .claude/agents) | **2/2 PASS** | 정상 파일 미발동 1 PASS |
| 4-4 trust-boundary 매처 stale 봉합 | **3/3 PASS** | Handlers/Network/Program.cs |
| 4-5 reviewer-auto-trigger (Protocol/Handlers 발동, Tests/CONTEXT/Client 스킵) | **5/5 PASS** | |
| **총** | **25/25 PASS** | |

dotnet build green 본 머신 검증 (SAC On 차단 회피, dotnet test는 Cloud Codex 위탁).

### ➡️ 다음 스텝

**M3.6 진척**: Phase 01 ✅ + Phase 02 ✅ + Phase 03 ✅ → **Phase 04 + Phase 05 진입** (4/6, 코드 전수조사).

- **Phase 04** (서버 코드 전수조사 02_Server/ + 98_Shared/) — 대규모, Coordinator + Team (server + shared + qa Worker) 첫 실측
- **Phase 05** (클라 코드 전수조사 03_Client/ + 04_ClientNet/) — 복잡, client Worker + reviewer
- 병렬 가능 (server/shared vs client 도메인 분리)
- **Phase 06** (외부 리뷰 흡수 + 종합 마감) — 대규모, M3.6 마일스톤 PR 마감

**별 시점**:
- M3 + M3.5 + M3.6 학습 일지 트랙 B 노션 박제 (누적 ★★★ 43건+ 예상)
- SubAgent 재귀 차단 Hook (work-pin 별 시점, 실측 0건)
- 정합 감사 *시뮬 발동 확장* 별 마일스톤 (Phase 02-A "0건" 정정 학습 흡수)

## 학습 일지 후보 키워드

본 Phase에서 박힌 학습 후보 (트랙 B, 별 시점 본인 회고 박음):

- **`bash-regex-to-python-shlex-essence-fix`** (★★★) — 공구의 한계 인식 → 적절한 공구 교체 결정. PR #43 응급 → Phase 03-B 본질. 한국 게임 회사 백엔드 *기술 의사결정* 어필 결정타
- **`windows-crlf-trap-git-bash-python`** (★★★) — Python text mode stdout `\r\n` + mapfile `\n` split → 토큰 끝 `\r` 잔재. `tr -d '\r'` 후처리. 한국 PC 환경 백엔드 보편 함정
- **`self-referential-flag-pattern-conscious-grade-elevation`** (★★★) — 정책 박는 commit이 자기 정책에 잡히는 패턴. 옛 추측 등급 → 솔직한 등급 평가 (4-4 자기 참조 → 대규모 자동 상향). 시스템 의사결정 어필
- **`matcher-stale-after-folder-restructure`** (★★★) — Phase 02-A "가짜 약속 0건"이 본 Phase 시뮬 발동 검증으로 *4번째 발본 발견*. trust-boundary 매처가 실제 경로와 불일치. 정합 감사 한계 인지 = *시뮬 발동 확장 필요* 정신
- **`policy-week-1-recalibration-cycle`** (★★) — 추측 정책 → 실측 정책 진화 사이클. M3.5 박힘 시점에 명시한 "1주 후 재조정" 약속이 M3.6에서 봉합. 정책 생명주기 인식
- **`hard-vs-soft-hook-claude-context-decay-defense`** (★★) — Soft (메인 판단)는 까먹기 위험, Hard hook (알림 + 누적)이 안전망. SubAgent 호출 권한 한계는 알림 강도 ↑로 우회
- **`grandfathered-without-explicit-policy`** (★) — 옛 자산 = 변경 안 하면 자연 회피. 명시 grandfathered 정책 박지 X (정신 약화 회피). PostToolUse 발동 시점 특성 활용
