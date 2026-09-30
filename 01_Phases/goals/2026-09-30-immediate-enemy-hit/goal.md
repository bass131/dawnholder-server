# S1 — 즉시 피해 처리 책임 통합과 정적 CI 첫 적용

상태: **구현·독립 검증 완료, PR 통합 대기**. base main `a3c4e15e7d655511ced06bd1303360761413c5ab`, branch `refactor/immediate-enemy-hit`. 사용자는 S0 PR #139 병합을 명시 승인했고 2026-09-30T03:53:37Z에 병합했다. S1 PR의 병합 승인은 별도로 받는다. 원격 CI·PR의 최신 head와 승인·병합 여부는 통합 직전에 확인한다.

## 목표와 완료조건

평타·Dash의 즉시 피해 반영·공격자 지정·결과 통지·사망 후처리 조정을 맵 소유의 한 진입점으로 모은다. 액션별 동작은 보존하고, 이 변경을 프로젝트 코드 기준과 워커 분리·정적 CI의 첫 실제 적용으로 검증한다.

- 공통 후처리의 변경 지점이 두 액션에서 한 소유자로 모이고 책임·의존성 증가가 없는지 독립 검토한다.
- 실제 평타·Dash 경로와 공통 진입점에 대해 별도 Astra 작업자가 테스트 코드를 작성·보완하고 실행한다.
- 첫 대상 production 파일에서 SA1201·SA1202·IDE0011을 CI 실패 조건으로 연결한다. 정상 코드 통과와 의도적 규칙 위반 실패를 격리 복사본에서 확인한다.
- 기존 서버 회귀와 해당 전투 봇 검증을 수행하고 미실행 Unity·실제 플레이·DB 범위를 구분한다.
- 동일 범위의 변경 전후 근거와 남은 위험을 기록하고 PR을 만든다. 파일럿 성공과 PR 통합 전에는 S2 이후 소스 변경을 시작하지 않는다.

## 선택한 설계와 보존 계약

`GameMap`에 작은 내부 전용 즉시 피해 적용 진입점을 둔다. 계산된 피해·대상·공격자·효과를 받아 HP 변경, 공격 대상 지정, Hit 통지와 기존 `HandleEnemyDeath`를 조정하고 생존 결과를 돌려준다. 별도 상태나 규칙이 생기지 않아 새로운 인터페이스·시스템 계층을 추가하지 않는다.

대상 선택·피해 공식·시전 상태·생존 시 넉백은 `MeleeAction`과 `DashAction`에 둔다. 평타의 상대 위치 기반 넉백과 Dash 진행 방향 기반 넉백을 보존한다. 공격·스킬 패킷 시점, 즉시 피해의 음수 currentHp 전송, 공격자 attribution, Hit→Death→StageClear와 처치 콜백 순서를 바꾸지 않는다. 지연 피해 큐·프로토콜·공유 DLL·Unity 자산·DB·게임 정책은 변경하지 않는다.

정적 CI는 기존 분석기를 사용한다. 대상은 `GameMap.cs`, `Maps/Actions/MeleeAction.cs`, `DashAction.cs` 세 production 파일이며 전역 warnings-as-errors는 적용하지 않는다. `.editorconfig` 인라인 주석의 실제 해석을 확인하고 필요한 문법 정리를 한다. 테스트·도구 완화와 생성물 제외를 유지하며 무관한 기존 경고를 소스 대량 수정으로 없애지 않는다. CA1502·CA1506은 지원이 확인된 .NET 10 대상 범위에서 warning 관찰로 시작한다. 임계 진단을 연속 복잡도 점수나 전체 아키텍처 검증으로 해석하지 않는다.

## 위임과 파일 소유권

- 메인: 이 goal, S0 완료 기록, CURRENT, 로드맵, 통합·PR·보고. 합의한 범위와 진행을 관리한다.
- Sol 구현자: `02_Server/GameServer/Maps/GameMap.cs`, `Maps/Actions/MeleeAction.cs`, `DashAction.cs`, 필요한 `00_Document/domains/server.md` 계약 설명. 테스트·CI 파일을 수정하지 않는다.
- Sol CI 작업자: 루트 `.editorconfig`, 필요한 `02_Server/GameServer.Tests/.editorconfig`, `99_Tools/.editorconfig`, `.github/workflows/dotnet-tests.yml`, 코드 기준의 현재 자동 검사 설명. 게임·테스트 소스를 수정하지 않는다.
- Astra 독립 검증자: 관련 서버 테스트 파일과 격리 검증 산출물. 변경 전 기준선, 요구사항 테스트, 정상·위반 CI 검사, 서버·봇 실행 공간을 단독 소유한다. 구현 결함은 Sol 구현자에게 돌리고 재검증한다.

작업자는 AGENTS·CODE_CONVENTION의 관련 부분과 최소 파일만 읽는다. 추가 위임·브랜치·커밋·푸시·병합은 하지 않는다. 구현 쓰기 종료와 테스트 파일 소유권을 확인한 뒤 독립 검증을 수행한다. 모델은 구현 `gpt-6.1-sol`, 메인·검증 `gpt-6-astra`로 지정하고 실제 런타임 확인 여부는 결과에 구분한다.

## 실행·측정과 근거

실행은 DEVELOPMENT의 WSL 격리 복사본을 사용하며 Windows Unity DLL 복사나 SQL 작업은 하지 않는다. 검증자가 기준선 복사를 완료하기 전 구현 소스·CI 설정을 변경하지 않는다. WSL 동기화·빌드·테스트·봇은 검증자 한 명이 순서대로 실행하고, 7777 포트의 기존 프로세스가 있으면 시작·종료하지 않고 보고한다. 본 작업이 시작한 봇용 서버만 정리한다.

검증 원본과 짧은 결과는 `.backups/verification/2026-09-30-immediate-enemy-hit/`에 둔다. 기존 테스트의 정상·경계·치사·과잉 피해와 통지 순서를 확인하고 새 테스트는 구현 본문을 복제하지 않는다. 정적 검사 위반용 수정은 별도 임시 복사본에서만 수행한다.

비교 범위는 즉시 피해 후처리의 소유 지점, 실제 액션별 보존 동작, 정적 진단의 적용 범위·정상/위반 결과다. 기준 commit·변경 상태·환경·생성물 제외를 기록한다. 탐색 시간이나 연속 복잡도는 계측하지 않았으면 미측정으로 남기며 단일 AI 점수를 만들지 않는다.

## 결과와 다음 작업

변경 전 기준선: WSL Ubuntu·.NET SDK 10.0.300(global.json의 rollForward 범위)에서 808 total / 803 pass / 0 fail / 5 기존 skip. 대상 3파일의 기존 진단은 GameMap의 SA1202 두 곳이었다. 원본은 `.backups/verification/2026-09-30-immediate-enemy-hit/`의 `baseline-tests.log`, `baseline-static.log`, `baseline-target-diagnostics.log`, `environment.log`다.

두 Sol 작업자는 각각 게임·서버 계약 4파일과 CI·코드 기준 5파일 쓰기를 종료했다. 별도 Astra 검증자는 `02_Server/GameServer.Tests/Maps/ImmediateEnemyHitTests.cs`를 작성·실행하고 쓰기를 종료했다. 다른 Astra 작업자의 독립 아키텍처·계약 읽기 검토에서도 수정이 필요한 결함이 없었다. 지정 모델과 실제 런타임 확인은 구분하며 실제 모델은 독립 확인하지 못해 `unknown`이다.

| 검증 | 실제 결과 |
|---|---|
| CI 동등 강제 solution 빌드 | 성공, 대상 밖 기존 경고 4개 유지 |
| 전체 서버 테스트 | 820 total / 815 pass / 0 fail / 5 기존 skip |
| 신규 독립 계약 테스트 | 12/12 통과 |
| 동일 액션 테스트를 기준 소스에서 재실행 | 9/9 통과; 새 공통 API 3개는 기준선에 없어 제외 |
| 전투 봇 | DashSmoke·EmergencyCombatSmoke 2/2 성공 |
| 세 파일 × 세 규칙 위반 주입 | 9/9에서 해당 diagnostic error와 exit1 확인 |
| 적용 범위 | 대상 밖 production warning, Tests·Tools none, 정상 빌드 성공 |
| CA1502·CA1506 지원 | 별도 임계 초과 probe에서 실제 warning·exit0; 현재 대상 임계 진단 0 |

실측 환경은 WSL Ubuntu 26.04, SDK 10.0.300/runtime 10.0.8이다. 원본 소스에 위반을 주입하지 않았고 격리 검증 전후 원본 SHA256이 같았다. 봇 helper가 시작한 서버는 정상 정리됐으며 종료 후 7777 listener가 없었다. 증거와 명령은 `.backups/verification/2026-09-30-immediate-enemy-hit/summary.md` 및 연결된 원시 로그에 있다. 별도 읽기 리뷰는 `.backups/reviews/2026-09-30-immediate-enemy-hit-contract-review.md`다. GitHub CI·PR 최종 확인은 같은 verification 폴더의 `pr-final.json`에 별도로 보존한다.

확인한 개선은 즉시 피해 후처리의 소유 지점 2→1, 액션별 동작 보존, 대상 정적 규칙의 실제 실패 조건 적용이다. 새로운 상태·인터페이스·I/O는 추가하지 않았다. 기준선에는 같은 CA 관찰 설정이 없어 진단 개수로 전후 복잡도 개선을 주장하지 않는다. 연속 복잡도·결합·AI 탐색 시간은 미측정이다. Unity 빌드·에디터·실제 플레이·시각 효과·DB 검증은 미실행이며 이번 변경의 서버·봇 결과와 구분한다.

순서와 후속 영역은 [전체 로드맵](../../milestones/2026-09-30-maintainability-rollout/roadmap.md)을 따른다. S2–S5는 파일럿 성공 뒤 영역별 제한적 조사와 필요한 작은 리팩토링으로 진행한다. 이미 기준을 충족한 부분은 변경 불필요 근거를 남긴다.
