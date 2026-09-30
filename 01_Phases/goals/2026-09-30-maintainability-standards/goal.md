# 유지보수 기준과 목표 루프 정비

상태: **완료·PR #139 병합**. base main `863be8c3831de1130c4f128036521e480311a49c`, branch `docs/maintainability-principles`, 최종 head `a0bba1b1deb262bacccf928be51c3c56517b3571`. 사용자의 “OK 승인”을 받아 동일 head의 독립 검토·CI 성공·CLEAN을 확인하고 [PR #139](https://github.com/bass131/dawnholder-server/pull/139)를 2026-09-30T03:53:37Z에 squash 병합했다. main은 `a3c4e15e7d655511ced06bd1303360761413c5ab`다. CI 근거는 [36666053956](https://github.com/bass131/dawnholder-server/actions/runs/36666053956)과 `.backups/verification/2026-09-30-maintainability-standards/pr-final.json`이다.

## 목표와 범위

S0는 프로젝트의 코드 작성 기준과 구현·독립 검증 적용 방식을 정리하고 목표 루프의 중복·불필요한 과거 맥락을 줄인다. 현재 `docs/maintainability-principles`의 PR #139를 같은 목표로 확장한다. 게임 코드·CI 설정·DB 구현·SQL 운영·게임 정책 변경은 S0 범위 밖이다.

산출물은 AGENTS 운영 원칙과 라우팅, CODE_CONVENTION의 설계·역할별 판단 기준, 목표 루프의 실행 흐름, 이 목표와 연결된 마일스톤 로드맵이다. 문서 간 권한·모델·검증 계약이 일치하고, 불필요한 중복 없이 필요한 기준을 찾을 수 있으면 S0 완료조건을 충족한다. 문서 수정은 기술적 강제나 실제 코드 적용 완료를 뜻하지 않는다.

## 사용자 선택과 진행 경계

- 자동 판정 가능한 정적 검사는 첫 실제 적용부터 CI에 포함한다. 책임 경계·테스트 품질·AI 탐색은 독립 검토와 별도 측정으로 다룬다.
- 첫 파일럿은 평타와 Dash의 공통 즉시 피해 처리다. 현행 동작을 보존하며 실제 책임 통합을 검증한다.
- 파일럿이 현행 동작 보존·독립 테스트 코드 검증·CI·실제 책임 통합 조건을 충족하면 프로젝트 전체로 순차 확대한다. 충족 전에는 후속 영역 소스 쓰기를 시작하지 않는다.
- 기준 정비와 마일스톤 진행은 합의됐다. 각 PR 병합은 별도 명시 승인이 필요하며 전체 진행 요청은 포괄 병합 승인이 아니다. PR #139 승인은 해당 PR에만 적용했다.

## 현재 상태와 검증

CODE_CONVENTION의 기존 상속 깊이·중복 추출·표기 규칙을 보존하고 설계 판단·역할별 적용·측정 조건을 보완했다. 목표 루프는 해당 기준을 연결하고 모델 배정 충돌과 과거 산출물 열거를 제거했다. AGENTS는 완료된 로드맵에만 적용되던 만료 병합 예외를 제거해 현재 PR별 승인 원칙을 명확히 했다. 종료 근거는 [D0 통합 결과와 다음 진입점](../2026-09-29-persistence-design/goal.md#통합-결과와-다음-진입점)에 있다.

구현 작업자는 AGENTS.md, CODE_CONVENTION.md, 목표 루프 SKILL.md, 이 goal.md, CURRENT 링크와 로드맵의 쓰기를 종료했다. 구현 지정 모델은 `gpt-6.1-sol`, 별도 검토 지정 모델은 `gpt-6-astra`이며 실제 런타임 모델은 독립 확인하지 못해 `unknown`이다. 메인은 이 목표의 결과 기록을 인계받았다.

독립 검토는 요구사항·모델·권한·문서 참조 일관성과 세 가지 문서상 적용 시나리오(오탈자 수정, 코드 위임, 테스트 실패 또는 병합 미승인)를 확인해 통과했다. 근거는 `.backups/reviews/2026-09-30-maintainability-standards-review.md`다. 실제 워커 행동이나 새 CI의 실행 검증을 대신하지 않는다.

`git diff --check`와 6개 변경 문서의 로컬 링크 30개 확인이 통과했다. Windows Python은 사용 불가였으나 기존 WSL Ubuntu Python 3.14.4로 `skill-creator/scripts/quick_validate.py`를 실행해 `Skill is valid!`를 확인했다. 설치·전역 설정 변경은 없었다. 근거는 `.backups/verification/2026-09-30-maintainability-standards/static-checks.json`이다. S0에는 게임 테스트 코드·실제 플레이·DB 검증을 추가하지 않았고 해당 실행 검증도 수행하지 않았다.

## 후속 S1 설계 경계

평타·Dash 공통 피해 경로를 제한적으로 조사해 보존 계약·상태 소유권·변경 파일과 테스트 소유권을 정한 뒤 별도 작은 목표로 착수한다. 파일럿 production 3개 파일 범위에서 기존 SA1201·SA1202·IDE0011을 CI error로 검사하는 계획이며 전역 warnings-as-errors는 적용하지 않는다. 먼저 .editorconfig 인라인 주석의 severity 파싱 유효성을 증명하고, 격리 복사에서 의도적 위반이 CI와 같은 빌드를 실패시키는지 독립 검증한다.

읽기 전용 조사에서 좁힌 대상은 `GameMap.cs`와 `Maps/Actions/MeleeAction.cs`, `DashAction.cs`다. 피해 적용·공격자 지정·결과 전송·사망 후처리 조정을 맵 소유의 작은 진입점에 모으고, 대상 선택·피해 계산·시전 상태·생존 시 넉백은 액션에 둔다. 즉시 피해의 음수 HP 전송, Hit→Death→StageClear·처치 콜백 순서, 공격별 넉백 방향과 시전 패킷 시점을 보존한다. 지연 피해 큐는 이번 파일럿에서 변경하지 않는다. S1의 독립 테스트는 공통 진입점과 실제 평타·Dash 경로를 함께 확인한다.

CA1502·CA1506은 파일럿 .NET 10 범위의 warning 관찰로 시작할 수 있다. 구체 패키지·수치는 아직 확정하지 않았으며 CI 적용이나 전체 연속 측정이 완료된 상태가 아니다. 현재 Ubuntu CI의 검증과 Unity 검증을 구분한다. S1은 요구사항·보존 동작 기반 독립 테스트 코드와 CI를 포함하며 실패·취소·종료 경로 및 실제 통합의 미실행 범위를 따로 기록한다.

순서·의존성은 [확대 로드맵](../../milestones/2026-09-30-maintainability-rollout/roadmap.md)에 둔다. 각 후속 목표는 착수 시 생성하며, 영역별 제한적 조사 → 계약·책임 확인 → 필요한 작은 리팩토링으로 진행한다. 이미 기준을 충족한 부분은 변경하지 않고 근거를 남긴다.
