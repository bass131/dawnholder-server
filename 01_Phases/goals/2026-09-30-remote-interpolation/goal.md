# S4 — 원격 보간 계산과 Unity 적용 경계 분리

상태: 구현·독립 검증 완료, [PR #144](https://github.com/bass131/dawnholder-server/pull/144) 통합 대기. 최신 main `a3c4e15e7d655511ced06bd1303360761413c5ab` 기반 `refactor/remote-interpolation-state`, 구현·테스트 commit `f948848`. [S3 PR #143](https://github.com/bass131/dawnholder-server/pull/143)의 독립 검증·계약 검토 완료 후 진행하며, 앞선 미병합 서버 변경은 이 독립 브랜치에 포함하지 않는다. 기존 클라이언트 수명·설정·예측 분리를 재작업하지 않고 필요한 작은 책임 경계에 기준을 적용한다. PR은 최종 보고서 확인·사용자 승인 전 병합하지 않는다.

## 목표와 설계

`RemoteEntity`가 함께 소유하는 snapshot buffer·render clock·보간 계산과 Unity 프레임/Transform 적용을 나눈다. 같은 컴포넌트를 원격 플레이어와 적이 사용한다. 보간 상태 전체를 일반 내부 C# 타입 한 곳으로 옮겨 명시적인 deltaTime 입력으로 검증할 수 있게 한다. MonoBehaviour는 기존 public API·component 계약을 유지하고 계산 결과를 Transform에 반영한다.

기존 Vector2/Mathf와 float 연산 순서를 유지해 수치 동작을 보존한다. engine-free 전환, Shared DLL 이동, clock interface나 새 서비스 계층은 필요하지 않다. registry·enemy visual offset·callback 정책은 기존 소유자에 둔다. adapter와 계산 객체가 clock/buffer 상태를 중복 소유하지 않는다.

## 보존 계약

- serverTick×TickDuration 시간축과 보간 지연0.15s·보관1.0s·재동기 임계0.5s·프레임 catch-up0.1을 유지한다. 프레임률 독립 보정으로 바꾸지 않는다.
- buffer0의 마지막 위치 유지, 하나/과거/미래 경계와 작은 span fallback, 구간 선형 보간, extrapolation 없음, retention 경계를 보존한다.
- Initialize의 즉시 Transform 적용, SnapInterpolation의 buffer/clock reset·현재 Transform 유지, ClearBuffer의 buffer만 삭제를 구분한다.
- 새 snapshot 적재 후 teleport 도착 callback1회의 시점은 현재대로 유지한다. 적 VisualFootOffset과 registry/public API를 바꾸지 않는다.
- 기존 `.meta`·GUID·prefab/scene·직렬화 값·문자열 에셋 경로를 보존한다. 새 파일에는 충돌 없는 meta를 추가한다.

## 구현·독립 검증 소유권

메인은 goal·CURRENT·로드맵·통합·보고, Sol6.1은 `State/RemoteEntity.cs`와 새 보간 타입·meta·최소 client 계약 설명을 맡는다. 별도 Astra는 EditMode 테스트·meta 및 필요한 adapter 계약 테스트를 작성·실행하고 생산 수정은 구현자에게 반환한다. 다른 Astra가 책임/동작 보존 diff를 검토한다. 시작 전 정확한 경로·테스트 assembly 접근과 쓰기 경계를 확인한다.

범위 밖: registry 통합, 로컬 예측 정책, 네트워크/PDL/DB 변경, 기존 에셋 재저장·씬 재구성, UI·오디오·전투 효과 일괄정리. 기존 대표 영역의 유지 근거와 한계는 `.backups/reviews/2026-09-30-s4-client-assessment.md`에 있다.

## 완료조건과 실행

알려진 snapshot/explicit dt의 기대 위치, 초기화·clamp·reset·retention·긴 정지 복귀와 callback/Transform adapter를 독립 TestCode로 검증한다. 구현 본문을 oracle로 복제하지 않는다. 가능한 기존 adapter 경로 테스트는 기준 소스에서도 실행해 동일 계약을 확인한다.

DEVELOPMENT와 프로젝트 Unity 버전6000.4.7f1을 확인한다. 기존 사용자 Editor/프로세스가 있으면 종료하지 않는다. 허용된 batch EditMode와 필요한 PlayMode를 작업 소유 프로세스로 실행하고 결과 XML을 확인한다. .NET 성공으로 Unity 검증을 대체하지 않는다. editor라이선스·다른소유실행으로 막히면 원인과 미실행 범위를 기록한다. 검사 전후 tracked 에셋과 DLL hash/diff를 확인한다.

원시 결과는 `.backups/verification/2026-09-30-remote-interpolation/`에 남긴다. 비교는 단일 상태 소유·명시 시간 입력·보존 궤적·독립 검증 범위이며 줄수/주석수/단일AI점수로 개선을 주장하지 않는다. 실제 원격 플레이·시각적 jitter/느낌·성능은 테스트 결과와 구분하며 실행하지 않았다면 미실행이다.

## 결과와 다음 작업

구현·독립 TestCode·별도 읽기 리뷰 완료 상태다. Sol이 wrapper·새 internal state/meta·client 계약을 수정했고 별도 Astra가 신규 EditMode 테스트2파일과 meta를 작성·실행했다. 구현자와 검증자 모두 쓰기를 종료했다. 요청 모델 Sol6.1/Astra와 실제 런타임 unknown을 구분한다.

| 실행 | 실제 결과 |
|---|---|
| 현재 소스 대상 Unity EditMode | 31/31 통과: 새state14·adapter12·기존registry5 |
| 같은 adapter 계약의 기준선 비교 | 12/12 통과 |
| 구현 복원 후 전체 EditMode | 275/275 통과, fail/skip/inconclusive0, exit0 |
| 보호 대상 | 기존29파일 중 허용 RemoteEntity.cs 외28개 hash불변, 새GUID3개 충돌없음 |
| 독립 읽기 리뷰 | 확정 결함 없음 |

실행은 Windows Unity6000.4.7f1/f3c3c4248748의 작업 소유 Hidden batch이며 XML 결과와 exit를 모두 확인했다. 라이선스·컴파일 차단이 없었고 완료 시 Unity 프로세스0개다. 메인이 허용한 RemoteEntity.cs 정확1파일의 기준선 임시치환은 try/finally로 구현 bytes를 복원하고 hash/meta 불변을 확인했다. 그 뒤 최종 전체 EditMode를 실행했다. 새 internal state API는 기준선에 없어 adapter 비교와 구분했다.

독립 테스트는 명시적 dt와 알려진 궤적, strict 재동기 경계, retention, 프레임당 catch-up, Reset/Clear 차이 및 callback 일회성·재진입·throw/취소 시점을 확인했다. 실제 player registry와 Normal/Golem/Boss prefab 소비 경로의 offset도 확인했다. 원시 실행기·XML·로그·manifest·복원 근거는 `.backups/verification/2026-09-30-remote-interpolation/summary.md`, 읽기 리뷰는 `.backups/reviews/2026-09-30-s4-interpolation-contract-review.md`에 연결돼 있다.

검증은 실제 Unity EditMode와 명시적 Update 호출까지다. 자동 PlayerLoop·PlayMode·실서버 원격 플레이·시각 jitter/느낌·성능·DB는 미실행이다. 정수tick/50ms 시간축에서 양의 극소 span을 만들 수 없어 tiny-span fallback 직접 실행을 주장하지 않는다. 해당 분기/float 연산 순서 보존은 읽기 리뷰로 확인했으며 수치 테스트 허용 오차0.0001을 bitwise 전수 동치로 확대하지 않는다. GitHub .NET CI는 PR 생성 후 확인하며 Unity 테스트를 대신하지 않는다.

[로드맵](../../milestones/2026-09-30-maintainability-rollout/roadmap.md)의 S5에서 전체 회귀·영역별 유지/변경 판단·최종 PR 의존성과 보고서를 종합한다.
