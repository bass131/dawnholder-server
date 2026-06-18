# Ledgers — 미룸 장부 (loop-driven pending 원장 3종)

> 루프 드라이버가 "지금 안 하고 *미뤄둔 것*"을 빠짐없이 적어두는 장부. 루프가 빨리 진행해도 *사람 몫이 조용히 증발하지 않게* 가시화한다 (ADR-032 §C·§D / [`../ADR/harness/ADR-032-loop-driven-operation.md`](../ADR/harness/ADR-032-loop-driven-operation.md)).
>
> **위치 결정 (#5, 2026-06-18 영호)**: `00_Document/ledgers/` **committed** — `.claude/state`(로컬·gitignored) 대신 repo 추적 = 머신 바뀌어도 영속 + 마일스톤 감사([ADR-024](../ADR/harness/ADR-024-false-promise-cadence.md)) 가시. **영호 개인 작업**(팀 유지 X) — 본인 미룸 추적용.

---

## 원장 3종

| 원장 | 뭘 적나 | 언제·어디서 처리 |
|---|---|---|
| [`pending-art.md`](pending-art.md) | 루프가 placeholder(임시 그림·소리) 꽂은 슬롯 — "진짜 아트 필요" | 영호가 같은 슬롯에 실아트 교체 (코드 무변경) |
| [`pending-comprehension.md`](pending-comprehension.md) | 루프가 빨리 구현하고 넘어간 것 중 영호가 *아직 깊게 안 본* 항목 | `/session:review`(pull 세션)에서 깊게 봄 |
| [`pending-knowledge.md`](pending-knowledge.md) | 루프가 발견한 *knowledge 캐시 박을* 후보 | 영호 승인 후 박제 (AI 자율 박제 X) |

---

## 운영 원칙

- **루프가 적재, 사람이 소비**: 루프 드라이버([`/engine:goal`](../../.claude/commands/engine/goal.md))가 진행 중 미룬 것을 *추가*만. 처리(아트 교체·학습·박제)는 영호.
- **AI 자율 비움 X**: 항목 *제거*는 영호가 처리 완료 확인 후(pending-knowledge 박제는 영호 승인 게이트 — knowledge-gc 정합).
- **가시성이 유일 방어**: ADR-032 critic 노트 — "나중에 봐야지"가 안 일어나면 부채가 조용히 쌓임(ADR-025가 죽인 트랙 B 함정). 본 장부의 *가시성* + pull-분리가 그 회피.

---

## 갱신 이력

- 2026-06-18 — M7.5 Phase 05에서 신설. #5 위치 결정 = `00_Document/ledgers/` committed (영호 영속·감사 가시). 3 원장 시드(비어있음).
