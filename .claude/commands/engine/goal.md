---
description: 목표 도달형 루프 드라이버 — done 조건(WSL2 게이트 / dangling·hook·reviewer)에 도달할 때까지 자율 반복. done은 외부 기계 심판이 판정(AI 자기판단 X). v1 attended. 내장 /loop·Workflow 위에 done 심판·3버킷·정지 게이트 글루.
argument-hint: "<목표 한 줄> [--done='조건'] [--domains=server,shared,client] [--max=N] [--dry-run]"
---

loop-driven 운영의 **목표 도달형 엔진**. "<목표>를 done 조건 충족까지 자율 구동"한다. 반복 substrate(간격·self-pace)는 **내장 `/loop`**, 구조적 병렬은 **내장 `Workflow` 도구**를 재사용하고, 본 슬래시는 그 위에 **① 외부 done 심판(기계 게이트) ② 3버킷 판정 ③ 정지 게이트** 글루를 얹는다. 정책 단일 진실 = [`../../../00_Document/policies/loop-driver.md`](../../../00_Document/policies/loop-driver.md).

목표/모드: **$ARGUMENTS** (목표 한 줄 필수. `--done` 미지정 시 아래 기본 done 조건)

> ⚠️ **핵심 = done은 외부 기계 심판이 판정.** AI가 "다 됐다" *자기판단으로 done 선언 X* (자기평가 편향 차단 — refactor-sweep 첫 dry-run에서 Codex가 적발한 그 함정). 비가역(버킷 c)은 사람 게이트에서 정지.

---

### 내장과의 차이 (왜 별도 슬래시인가)

| | 무엇 | done 심판 |
|---|---|---|
| 내장 `/loop` | 간격·self-pace 반복 | 없음 (AI self-pace) |
| 내장 `Workflow`(도구) | 구조적 병렬·pipeline·예산 상한 | 없음 |
| **`/engine:goal`** (본 슬래시) | 목표 도달까지 구동 | **외부 기계 게이트** ← 우리 글루 |

내부에서 `Workflow`(병렬 진단)·내장 `/loop`(인터벌 폴링)을 호출할 수 있다. `refactor-sweep` = 본 드라이버의 *refactor 프리셋*.

---

### done 심판 (외부 기계 — [`work-judge.md`](../../../00_Document/policies/work-judge.md) 버킷 a)

| 작업 종류 | 기본 done 조건 |
|---|---|
| **게임 코드** | WSL2 회귀 green (build 0/0 + test baseline 비감소 + 봇) + reviewer 🔴 0 (ADR-029) |
| **문서·하네스** | dangling 참조 0 + hook 정합 smoke + reviewer 🔴 0 |

`--done='<조건>'`으로 override. **게이트 출력은 트랜스크립트에 박히게** 실행 (`/loop` self-pace 평가자·사람이 done을 봄).

---

### 작업 흐름 (Step 0~5 — 범용 골격)

- **Step 0. 전제 게이트**: `git status --porcelain` clean 확인(`03_Client/ProjectSettings.asset` cloud 라인만 dirty면 무음 통과 — session:start C-1 정합). 작업 브랜치 기록. baseline 측정(게임=WSL2 test 수 / 문서=dangling 수). 깨진 baseline이면 *중단*.
- **Step 1. 목표 분해**: 목표 → 작업 단위. 등급 산정([`grade-and-risk.md`](../../../00_Document/policies/grade-and-risk.md)) + 위험 깃발 → 버킷. Phase 정의 신설·갱신이면 `plan-auditor` 사전 검증.
- **Step 2. Worker 위임**: 도메인 라우팅([`_routing`](../../agents/_routing.md)). 복잡/대규모 = `coordinator` 분해. Worker는 *수정만*.
- **Step 3. done 심판 루프**: 외부 기계 게이트 1회 실행 → **미통과면 진단→수정 반복**(`--max=N` 상한, 기본 8) → **통과면 done**. 자기판단 X.
- **Step 4. 검증 fan-out**: 변경분 `reviewer` 재점검. 시선 = `max(위험, 학습가치)` ([`review-throughput.md`](../../../00_Document/policies/review-throughput.md)). 🔴 = 사람 게이트.
- **Step 5. 리포트 + 정지**: 진행 요약. **버킷 (c) 도달 시 STOP** — 영호 GO 대기.

---

### 3버킷 분기 ([`work-judge.md`](../../../00_Document/policies/work-judge.md))

- **(a) 기계 판정** (빌드·테스트·WSL2·dangling) → 루프 자율 진행.
- **(b) 취향·육안** (아트·사운드·Unity) → placeholder 꽂고 `pending-art` 원장(P05)에 적재, 사람 병행 트랙(루프 안 막음).
- **(c) 판단·비가역** (설계 분기·push/PR/merge·`Protocol.Version`·DB·trust-boundary) → **사람 게이트 정지**.

---

### Hard rules

1. **done = 외부 기계 심판**. AI 자기판단으로 done 선언 절대 X.
2. **버킷 (c) 사람 게이트 절대 보존** — `ask(gh pr merge/create)`·`Protocol.Version` bump·trust-boundary = 영호 GO 게이트. 루프가 약화·우회 X ([`pr-and-merge-gate.md`](../../../00_Document/policies/pr-and-merge-gate.md)).
3. **push / PR 생성 / merge 자동 X** (v1) — 로컬 진행까지만, 외부 행위는 영호 명시 GO.
4. **`--max` 상한 + circuit halt 감지**: 무한 루프 방지. `.claude/state/circuit-tripped.txt`(P05) 신호 감지 시 즉시 정지.
5. **v1 = attended** (터미널 또는 `claude rc` 원격, PC-on 전제). 무인 v2 = defer(별도 ADR).
6. **baseline green 위에서만** 진행 (깨진 baseline이면 시작 중단).
7. **Worker 수정만, commit은 최상단** (헌법 "Worker commit 금지").

---

### 관련

- 엔진·기동·done 판사 → [`loop-driver.md`](../../../00_Document/policies/loop-driver.md)
- 판정자 3버킷 → [`work-judge.md`](../../../00_Document/policies/work-judge.md) / 시선 배분 → [`review-throughput.md`](../../../00_Document/policies/review-throughput.md)
- refactor 프리셋 → [`../refactor-sweep.md`](../refactor-sweep.md) (본 드라이버의 refactor 모드)
- 무인 정지 신호 → `circuit-breaker.sh` + `.claude/state/circuit-tripped.txt` (P05)
