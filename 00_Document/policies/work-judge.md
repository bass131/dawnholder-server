# Work Judge — 3버킷 판정자 (무엇을 루프에 맡기나)

> **근거**: [`../ADR/harness/ADR-032-loop-driven-operation.md`](../ADR/harness/ADR-032-loop-driven-operation.md) §B. 헌법 링크는 P02 — 충돌 시 **헌법이 이깁니다.**
>
> **이 문서의 역할**: 각 작업을 *"루프 자율 / 사람 트랙 / 사람 게이트"* 중 어디로 보낼지 판정하는 축. 엔진은 [`loop-driver.md`](loop-driver.md), 리뷰 처리량은 [`review-throughput.md`](review-throughput.md)가 분담.

본 문서는 [ADR-031](../ADR/harness/ADR-031-auto-phase-progression-async-reporting.md)의 *4종 Stop + 자동 진행*을 **판정자(judge) 축으로 재서술**한 것입니다. 새 규칙이 아니라, "누가 이 작업의 done을 판정하나"로 분류를 다시 그린 것. [`grade-and-risk.md`](grade-and-risk.md)의 **위험 깃발이 1차 분류기**입니다.

---

## 1. 3버킷

| 버킷 | 판정자 | 처리 | risk 깃발 | ADR-031 대응 |
|---|---|---|---|---|
| **(a) 기계 판정** | 빌드·테스트·WSL2 회귀([ADR-029](../ADR/harness/ADR-029-wsl2-dotnet-execution-standard.md))·정적분석·dangling·hook smoke | **루프 자율** (멈추지 않음) | 무깃발 | "공학 게이트 자동 진행" |
| **(b) 취향·육안** | 사람 (아트·사운드·Unity 외관) | **사람 트랙** (병행, 루프 안 막음) | unity-asset | — (병행 트랙) |
| **(c) 판단·비가역** | 사람 (설계 분기·push/PR/merge·`Protocol.Version`·DB 마이그·trust-boundary) | **사람 게이트 (Stop)** | irreversible / trust-boundary | "4종 Stop" |

- **(a)**: 기계가 통과/실패를 판정하면 사람 개입 없이 루프가 진행. done 판사 상세 = [`loop-driver.md`](loop-driver.md) §4.
- **(b)**: 아트·사운드 등 *취향*은 자동화가 힘듦 → placeholder로 진행하고 사람이 *같은 슬롯에 실리소스* 교체 (코드 무변경). placeholder rot 방지 = `pending-art` 원장 (P05).
- **(c)**: 되돌리는 비용이 크거나 사람 판단이 필요한 것 → 루프가 멈추고 영호 GO 대기. `ask(gh pr merge/create)` 게이트는 절대 보존 ([pr-and-merge-gate.md](pr-and-merge-gate.md)).

---

## 2. 깃발 → 버킷 매핑

**깃발 정의 자체는 여기서 재정의하지 않습니다** — 단일 진실은 [`grade-and-risk.md`](grade-and-risk.md) §3. 본 절은 그 깃발을 버킷에 *매핑*만 합니다 (중복 0):

| 깃발 | 버킷 |
|---|---|
| 무깃발 | (a) 루프 자율 |
| `unity-asset` | (b) 사람 트랙 |
| `irreversible` | (c) 사람 게이트 |
| `trust-boundary` | (c) 사람 게이트 |
| `harness` | 기본 (a) — 문서·config는 기계 검사(dangling·hook smoke). **단 권한·게이트 변경**(settings `ask(pr)` 매처 등) 동반 시 **(c)로 상향** |

---

## 3. v1 / v2 강제 차이 (중요)

- **버킷 (c)의 *물리적* 강제는 v1(attended)에서 사람 게이트로 성립**합니다 — 사람이 그 자리에 있어 GO를 누르므로.
- `risk-detector.sh`는 **advisory**(알림만, 차단 X)입니다. 따라서 **v2(무인)는 "깃발 → 사람 게이트 자동 적재" hook이 선결**되어야 (c)가 물리적으로 강제됨. 그 hook 전까지 v2에서 (c) 버킷은 *서류상 분류*에 불과 → **v2 미adopt** (ADR-032 미결 #1).

---

## 4. 불필요한 사람 게이트 최소화

> ⚠️ 영호 우려: *"행동 잠금(사람 게이트)이 생각보다 자주 일어날 이벤트일 것 같다."* 사람 게이트가 남발되면 throughput 이득이 깎입니다.

- 사람 게이트는 **진짜 (c)에만**. 가역적 보통/단순 작업이 습관적으로 (c)로 새지 않게.
- 판정이 애매하면 *기계 판정 우선* — [`grade-and-risk.md`](grade-and-risk.md) 등급 + 깃발로 먼저 거름. 깃발 0 + 가역이면 (a).
- 사람 게이트 **빈도를 모니터링** → 자주 멈추는 유형은 [`review-throughput.md`](review-throughput.md)의 *신뢰 졸업* 후보 (안전 증명되면 배치 GO로 강등).

---

## 5. 변경 시 동기화 책임

본 정책 수정 시 *반드시* 함께 점검:

- [`loop-driver.md`](loop-driver.md) (엔진 — 본 문서를 가리킴)
- [`grade-and-risk.md`](grade-and-risk.md) §3 (깃발 정의 *원천* — 본 문서는 매핑만)
- [`review-throughput.md`](review-throughput.md) (시선 배분·신뢰 졸업 연동)
- `../../.claude/hooks/risk-detector.sh` (깃발 검출 — advisory 한계)
- [`pr-and-merge-gate.md`](pr-and-merge-gate.md) (버킷 (c) 게이트)
- [`INDEX.md`](INDEX.md) (본 폴더 카탈로그)

---

## 갱신 이력

- 2026-06-18 — M7.5 Phase 01에서 신설. ADR-032 §B(4종 Stop을 판정자 축으로 재서술) 위에 작성. 깃발 정의는 grade-and-risk §3 단일 진실 참조(중복 0). 사람 게이트 최소화 절은 영호 우려 반영.
