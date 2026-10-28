# 거부 코드와 확정 무변경 seal 해석

Main `msg_d8e3d5fc0227`(2026-10-22T02:24:27Z)의 **메인 범위 판정**과 Core/World 리드 합의를 기록한다. 사용자 직접 결정으로 격상하지 않는다. [전송 계약 v1](packet-contract-v1.md)의 번호·기존 정의와 Core API v1 원문은 유지하며, 아래 확정 무변경 seal 예외만 보충한다. 이 문서는 제품 수리·실행 성공이나 독립 PASS가 아니다.

## 확정 무변경 seal 예외

아래 문구는 Main의 원문이다. Core API 계약 보충에도 같은 문구를 남긴다.

> 205→11 확인(메인 범위 판정, 사용자 결정 아님): resolver가 원 ID를 seal해 NotApplied와 게임 무변경이 확정되고 current·fence 검증을 통과한 경우만 wire 11로 내보낸다. 원 code·원 ID·deadline은 내부에 보존하고 자동 재시도는 하지 않는다. 그 밖의 205는 이 문구로 넓히지 않는다. 10·12·8은 쓰지 않는다.

Core API v1의 Unavailable 정의(읽기 실패 또는 변경 dispatch 전 실패)는 삭제하거나 일반화하지 않는다. 위 조건을 모두 통과한 `CancelledBeforeApply(205)`의 확정 무변경 seal에 한해 `CatalogStatus.Unavailable`/wire11로 표현한다. RPC가 전송되지 않았다는 뜻인 wire10, 적용 여부가 불명이라는 뜻인 wire12, 다른 runtime owner라는 뜻인 wire8로 바꾸지 않는다.

## 수리에 적용할 의미표

기본 일곱 코드의 의미표와 위205 예외를 구분한다. 내부의 원 code와 확정 무변경·현재 authority/fence 근거를 함께 운반하며, raw SQL 코드를 패킷에 새로 싣지 않는다.

| 내부 원 code | 의미 | CatalogStatus / wire |
|---:|---|---|
| 200 | Busy | Busy / 8 |
| 201 | StaleFence | RecoveryRequired / 9 |
| 202 | Conflict | OperationConflict / 15 |
| 203 | IdentityMismatch | IntegrityFailure / 14 |
| 204 | InvalidClass | IntegrityFailure / 14 |
| 206 | SequenceMismatch | IntegrityFailure / 14 |
| 207 | IntegrityFailure | IntegrityFailure / 14 |
| 205 | CancelledBeforeApply | 위 확정 무변경 seal 조건에서만 Unavailable / 11 |

201은 직접 Rejected 또는 검증된 NotApplied로 적용 안 됨이 확정됐으나 현재 소유를 확정할 수 없는 거부를 가리킨다. 기존 genuine `PersistenceWorkStatus.FenceLost`→`OutcomeUnknown`/wire12 경로는 별도로 보존한다. 적용 불명 결과를 이 표로 확정 실패로 바꾸지 않는다.

이 표는 원래 operation ID·descriptor·deadline, 현재 generation/owner·fence, 원래 예산과 resolver의 NotApplied 검증을 보존하는 수리에 사용한다. 강제 탈취·새 ID·새 예산·자동 재시도·SQL 동작·wire 번호·host/session/Close 수명 변경 권한은 추가하지 않는다.

## 근거와 적용 상태

- Core 질문 `msg_c3e1578b392a`, World 답변 `msg_310d314d83cc`, Core 합의 `msg_b4ab8eba6a8a`: 201→9와 genuine FenceLost→12 보존, 205의 기존 명시 합의 부재와 최소 보충 제안.
- Main 최종 확인 `msg_d8e3d5fc0227`: 위205 예외와 기본 일곱 코드, 기존 API v1 정의 보존 지시.
- 원시는 `.backups/verification/2026-10-18-character-server/approval-01/pr-blocker-repair-01/lead-inbox-wake-08.json` 및 같은 폴더의 조율 receipt다.
- 구현은 Core 수리 계약과 고정 원천에 반영한 뒤 World가 정확 commit/hash로 소비한다. 해당 새 구현·시험·실제 실행은 아직 이 문서의 성과가 아니다.
