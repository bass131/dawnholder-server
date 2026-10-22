# Core 캐릭터 저장 API 오류 계약 보충 v1

- 버전: 1.0.0, 선택 acquire 거부 원인 보존의 의미 보충. 구현/실행 완료를 뜻하지 않는다.
- 원천: 메인 범위 판정 `msg_fd0687ba7cb8`(2026-10-22T02:16:08Z), Core·World의 `msg_310d314d83cc`/`msg_b4ab8eba6a8a`, 메인 보충 `msg_c5811871fac5`(2026-10-22T02:24:26Z). 사용자 결정으로 격상하지 않는다.
- [API v1](character-storage-api-v1.md)의 Unavailable 원래 정의와 [API v2](character-storage-api-v2.md)의 수명·기존 의미는 보존한다. 이 보충은 아래 선택 acquire 경계에만 적용하며 생성/그 밖의 API 정의를 넓히지 않는다. SQL·원 enum 값·wire 번호·재시도 정책은 바꾸지 않는다.

## 원 code와 상태 보존

저장소의 직접 `AcquireAndLoadResult.Rejected(code)`와 resolver의 `ResolveOperationResult.NotApplied(code,current)`는 구분한다. owner는 원 code를 해당 operation의 작업 결과에 운반하고, selection은 상태 사실을 먼저 확인한 뒤 아래 의미로 해석한다. 같은 owner의 다른 작업/늦은 완료로 code를 대체하지 않는다.

기존 genuine `PersistenceWorkStatus.FenceLost`/`Unresolved`는 `OutcomeUnknown`을 유지한다. 원 code를 보존했다는 이유로 current/fence 검증 실패·적용 미확정을 확정 무변경 결과로 바꾸지 않는다. 기존 `Conflict`·`NotDispatched`·성공 class/lease 검증·Close/늦은 완료·원 ID/deadline 수명은 보존한다.

## acquire 거부 의미 표

wire는 World의 고정 패킷 의미를 대조하기 위한 표기다. Core 제품은 wire 번호를 넣지 않고 `CatalogStatus`로 반환한다.

| 원 PersistenceResultCode | Core CatalogStatus | World wire | 적용 사실/이유 |
|---|---|---:|---|
| Busy200 | Busy | 8 | 확정 거부의 타 owner 보유 의미 |
| StaleFence201 | RecoveryRequired | 9 | 직접 Rejected/검증된 NotApplied의 무변경은 확정되지만 현재 타 runtime 소유는 입증되지 않음. genuine FenceLost 상태와 구별 |
| Conflict202 | OperationConflict | 15 | 동일 원 ID/종류/payload 충돌 |
| IdentityMismatch203 | IntegrityFailure | 14 | 서버 identity 문맥 불변식 |
| InvalidClass204 | IntegrityFailure | 14 | 저장/서버 class 불변식 |
| CancelledBeforeApply205 | Unavailable | 11 | **아래 확정 무변경 seal 예외만** |
| SequenceMismatch206 | IntegrityFailure | 14 | 저장 sequence 문맥 불변식 |
| IntegrityFailure207 | IntegrityFailure | 14 | 저장 데이터 불변식 |

## 확정 무변경 seal 예외

다음은 메인 확인 문구이며 World 패킷 계약의 해석 메모에도 같은 글로 기록한다.

> 205→11 확인(메인 범위 판정, 사용자 결정 아님): resolver가 원 ID를 seal해 NotApplied와 게임 무변경이 확정되고 current·fence 검증을 통과한 경우만 wire 11로 내보낸다. 원 code·원 ID·deadline은 내부에 보존하고 자동 재시도는 하지 않는다. 그 밖의 205는 이 문구로 넓히지 않는다. 10·12·8은 쓰지 않는다.

`CancelledBeforeApply`라는 이름만 보고 RPC 미dispatch를 주장하지 않는다. 직접 `Rejected(205)`처럼 seal/검증된 NotApplied 사실이 입증되지 않은 결과는 이 예외의 Unavailable로 확정하지 않는다. genuine FenceLost/Unresolved가 관측되면 그 상태의 기존 의미를 유지하며, 이 예외의 적용 대상으로 바꾸지 않는다. 나머지 잘못된 shape/도달 불가능 상태는 타 owner의 Busy로 추정하지 않고 서버 문맥 불변식 실패로 닫는다.

## 구현·검증 상태

수리 계약과 원시 근거는 로컬 `.backups/verification/2026-10-17-character-storage-foundation/catalog-rejection-repair-30/`에 있다. 신규 Sol max 한 명이 같은 PR에서 제품3파일·기존시험3파일을 작성한다. 정확 실행 argv와 메인 배정 창 전 빌드/시험/실제 서버/DB는 미실행이며 PR 직전 신규 Opus1회가 별도로 남는다. 이 표를 실행 PASS나 사용자 화면 완료로 보고하지 않는다.
