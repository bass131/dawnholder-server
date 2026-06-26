# Loop Driver — 루프 엔진 (사람=방향+판단, 엔진=구동)

> **근거**: [`../ADR/harness/ADR-032-loop-driven-operation.md`](../ADR/harness/ADR-032-loop-driven-operation.md) (accepted).
> 헌법 본문 링크는 P02에서 추가 — 충돌 시 **헌법이 이깁니다.**
>
> **이 문서의 역할**: loop-driven 운영 모드의 *단일 진실(SSOT)*. "무엇을 루프에 맡기나"(판정자)는 [`work-judge.md`](work-judge.md), "리뷰를 어떻게 쳐내나"(처리량)는 [`review-throughput.md`](review-throughput.md)가 분담. 본 문서는 *엔진·기동·done 판사·세션 종류*를 정의.

본 문서는 작업 구동 방식을 *"사람이 매 스텝 프롬프트"*에서 **"사람=방향(목표·done 조건)+판단(게이트)만, 엔진이 매 스텝 대신 구동"**으로 전환하는 운영 모드를 정의합니다. Addy Osmani의 **Loop Engineering**을 우리 하네스에 적용한 것입니다.

---

## 1. 왜 루프인가 (배경)

동기는 **throughput(처리량) 병목**입니다. AI가 산출하는 속도를 사람이 매 스텝 리뷰로 따라가면 직렬 병목이 됩니다 (PR 100개가 쌓이면 사람 시간으로 다 못 봄). 역할을 *방향+판단*으로 전환해 천장을 높입니다.

실측상 우리 하네스는 **"loop-ready인데 loop-less"** — 자율 루프의 어렵고 드문 부품은 이미 다 있습니다:

- **done 판사** = WSL2 회귀 게이트 ([ADR-029](../ADR/harness/ADR-029-wsl2-dotnet-execution-standard.md)) — 기계가 통과/실패 판정
- **정지 게이트** = "비가역은 영호 GO" ([pr-and-merge-gate.md](pr-and-merge-gate.md))
- **maker-checker** = `reviewer` SubAgent + cross-review
- **메모리** = work-pin + knowledge + CHANGELOG
- **4종 Stop + 자동 진행** = [ADR-031](../ADR/harness/ADR-031-auto-phase-progression-async-reporting.md) (루프의 절반을 선취)

빠진 건 **① 범용 루프 드라이버(엔진)와 ② "무엇을 루프에 맡기나" 명시 분류**뿐. 본 문서가 ①, [`work-judge.md`](work-judge.md)가 ②.

> ⚠️ **루프는 refactor-sweep만이 아니다.** Loop Engineering은 *특정 작업 일부*가 아니라 **대부분의 모든 작업에 수행 루프를 구성**하고 판단·방향성만 사람이 잡는 것입니다. refactor-sweep는 그 *첫 검증된 인스턴스*일 뿐 (§7).

---

## 2. 엔진 구성 (내장 substrate + 우리 글루)

반복·오케스트레이션 substrate는 **이미 내장**으로 있다("loop-ready인데 loop-less" — 엔진을 새로 안 만듦). 우리는 그 위에 *done 심판·버킷·정지 게이트* 글루만 얹는다:

| 축 | 무엇 | 제공 | done 심판 |
|---|---|---|---|
| **`/loop`** | 간격 반복 또는 self-pace | Claude Code 내장 | 없음 (self-pace) |
| **`Workflow`** (도구) | 구조적 fan-out·pipeline·예산 상한 | Claude Code 내장 | 없음 |
| **`/engine:goal`** | done 조건 충족까지 자율 + **외부 기계 done 심판** | M7.5 신규 (`engine/` 네임스페이스) | WSL2/dangling 게이트 |

- **#3 결정 (2026-06-18, 영호)**: 내장 `/loop`·`Workflow`를 *몸통으로 재사용* + 어긋나는 핵심(**외부 done 심판** — 내장 self-pace는 AI 자기판단이라 편향 위험)만 `/engine:goal` 커스텀 신규. 내장 `/loop`과 구분 위해 **`engine/` 폴더 네임스페이스**(`/engine:goal`). `refactor-sweep` = `/engine:goal`의 *refactor 프리셋*(§7).
- `coordinator` SubAgent는 **Workflow의 부분 구현**으로 인용 — 복잡/대규모 Phase 분해는 coordinator, 대규모 병렬은 Workflow.
- **done 조건은 기계 판정 가능한 문장으로.** `/engine:goal`은 외부 게이트(WSL2/dangling) 출력이 *트랜스크립트에 박히게* 실행 — AI 자기판단으로 done 선언 X (§4 함정).

---

## 3. 기동 (cadence) — v1 attended only

| 버전 | 기동층 | 상태 |
|---|---|---|
| **v1** | 터미널 또는 `claude rc`(원격 control — 폰/웹에서 로컬 세션 조종) | **adopt** |
| **v2** | Desktop scheduled task (무인) | **defer (별도 ADR)** |

- **둘 다 PC-on 전제**: 로컬 WSL2 게이트가 done 판사라, 클라우드 Routines는 탈락 (게이트를 못 돌림). 외부에서 돌리려면 로컬 PC가 켜져 있고 세션 접근이 열려 있어야 함.
- **v2를 미루는 이유**: 권한 승격·circuit halt 폴링·trust-boundary 자율 침범 3대 위험이 동시 유입 → v1 검증 + 방어 hook 선결 후 별도 ADR (ADR-032 미결 #1). 엔진은 v1·v2 동일, 기동층만 교체라 v1 시작에 손해 0 (보조바퀴 졸업 패턴).

---

## 4. done 판사 (기계 판정)

루프가 "끝났다"고 선언하려면 *기계가 검증*해야 합니다 (사람 신뢰 아님):

- **게임 코드 작업**: WSL2 회귀 게이트 ([ADR-029](../ADR/harness/ADR-029-wsl2-dotnet-execution-standard.md)) — build 0/0 + test green + 봇 회귀.
- **문서·하네스 작업**: WSL 회귀 불요. done 판사 = ① dangling 참조 0 ② hook 정합 smoke ③ `reviewer` 🔴 0.

> **함정**: `/goal` 평가자는 트랜스크립트만 봅니다. done 증적(게이트 출력)이 트랜스크립트에 안 박히면 평가자가 done을 못 봅니다 → 게이트 명령을 루프 스텝 안에서 실행해 출력이 남게.

---

## 5. 정지 게이트 (사람 판단)

루프는 **버킷 (c) 판단·비가역**에서 멈춥니다 (상세 = [`work-judge.md`](work-judge.md)):

- 설계 분기 / `git push`·PR 생성·머지 / `Protocol.Version` bump / DB 마이그 / trust-boundary.
- **`ask(gh pr merge/create)` 사람 게이트는 절대 보존** (헌법 §3 / [pr-and-merge-gate.md](pr-and-merge-gate.md)). 루프가 이 게이트를 약화시키면 위반.
- 아트·사운드·Unity 외관(버킷 b)은 *병행 사람 트랙* — 루프를 막지 않고, placeholder로 진행 후 사람이 실리소스 교체 (pending-art 원장, P05).

> ⚠️ **모니터링**: 사람 게이트 정지가 *너무 자주* 일어나면 throughput 이득이 깎임. work-judge·review-throughput은 *불필요한 정지 최소화*를 의식해 설계 (영호 우려 — "행동 잠금이 자주 일어날 이벤트일 것"). 빈도 관찰 후 재조정.

---

## 6. 세션 2종 (작업 / 리뷰)

구현과 학습을 *같은 세션에 욱여넣지 않습니다* — 루프는 구현 처리량 단일 목표, 깊은 학습은 별도 pull 세션 (ADR-032 §D).

| | **작업용(구현) 세션** | **리뷰용(pull) 세션** |
|---|---|---|
| 진입 | `/session:start` | `/session:review` (P04 신설) |
| 목적 | 루프 구동 — 엔진이 스텝, 사람=방향+게이트 | 영호가 깊게 파보기 — "이거 어떻게 구현했어/왜?" |
| 핵심 동작 | git 게이트 + work-pin + 다음 액션 GO | pending-comprehension 원장 열기 + 깊은 설명 |
| 톤 | 구현 위주, 흐름 안 끊김 | 멘토링·학습 집중 |

- **이해 게이트는 루프에 남음**: "책임지고 GO할 만큼"은 작업 세션에서. "어떻게/왜 구현"의 깊은 학습은 리뷰 세션으로 분리.
- **pending-comprehension 원장**(P05)이 둘을 잇는 다리 — 작업 세션이 "아직 깊게 안 판 항목"을 큐에 적재, 리뷰 세션이 소비. 인지적 항복을 숨기지 않는 가시 목록.

---

## 7. refactor-sweep = 첫 검증된 인스턴스

[`refactor-sweep`](../../.claude/commands/refactor-sweep.md)는 폐기가 아니라 **범용 드라이버의 첫 프리셋**입니다. Step0~5 골격(전제 게이트 → 진단 fan-out → Worker → 회귀 게이트 → 재검증 → 리포트 + G1~G9 안전 가드)이 *도메인 무관 드라이버*로 추출되고, refactor-sweep는 그 드라이버의 *"refactor 모드 프리셋"*으로 재정의됩니다 (P04). **G1~G9 안전 가드는 한 줄도 약화 X.**

---

## 8. 버킷별 SubAgent 구동

루프는 기존 SubAgent 8종을 *Worker/checker로 재사용*합니다 (새로 안 만듦):

- 도메인 작업 = `server`/`shared`/`client`/`qa` Worker (Unity asset/scene/prefab + MCP는 메인 세션 직접)
- checker = `reviewer`(통합 리뷰) + `plan-auditor`(설계 사전 검증)
- 분해·위임 = `coordinator` (복잡/대규모)
- 라우팅·시선 배분 = [`subagent-routing.md`](subagent-routing.md) + [`review-throughput.md`](review-throughput.md) (시선 = `max(위험, 학습가치)`)

---

## 9. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 점검:

- [`work-judge.md`](work-judge.md) (3버킷 판정자 — 본 문서가 가리킴)
- [`review-throughput.md`](review-throughput.md) (리뷰 처리량 — 본 문서가 가리킴)
- [`../../.claude/commands/engine/goal.md`](../../.claude/commands/engine/goal.md) (`/engine:goal` 드라이버 — 내장 `/loop`·`Workflow` 재사용)
- `../../.claude/commands/session/{start,review}.md` (세션 2종 — P04)
- [`pr-and-merge-gate.md`](pr-and-merge-gate.md) (정지 게이트 — ask(pr) 보존)
- [`../ledgers/`](../ledgers/INDEX.md) `pending-{art,comprehension,knowledge}.md` (원장 3종 — 00_Document/ledgers)
- [`INDEX.md`](INDEX.md) (본 폴더 카탈로그)

---

## 갱신 이력

- 2026-06-18 — M7.5 Phase 01에서 신설. ADR-032(accepted) + ultracode triage + 계획 세션 영호 의도(루프=대부분 작업/refactor만 아님, 세션 2종, throughput 병목, 학습 pull 분리) 위에 작성. v1 attended only.
