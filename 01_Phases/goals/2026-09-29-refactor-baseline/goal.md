# M0 — 리팩토링 운영과 실행·평가 기준선

상태: 완료. [PR #130](https://github.com/bass131/dawnholder-server/pull/130)은 사용자 명시 승인 후 2026-09-29 13:47:53 UTC에 병합했다. 승인 대상 head `8807ec11981e941c49f0ba347cf933821a321c42`의 CI 성공과 CLEAN 상태를 확인하고 해당 SHA를 고정해 squash 병합했다. main 병합 커밋은 `95058e585ac4a9c12f305b7135db124d53171941`이다. 코드 리팩토링은 후속 목표다.

## 목표와 출발점

DB 설계 전에 코드베이스의 상태 소유·흐름·도메인 경계를 단계적으로 정리할 실행 기준을 만든다. 버그 개수보다 현재 설계의 비용, 대안의 비용, 동작 보존 여부를 우선 평가한다.

- 로드맵: [DB 설계 전 리팩토링](../../milestones/2026-09-29-refactor-before-persistence/roadmap.md).
- 브랜치: `feat/refactor-milestone-baseline`.
- 출발 main: `4b2c84e3bb94d91452d0b50af0ed76b107d5d6a6`, 착수 시 clean(메인 확인).
- 참고 감사: `.backups/reviews/2026-09-29-code-flow-audit.md`는 로컬 읽기 전용 조사 근거이며 실행 통과 증거가 아니다. 후속 목표에 필요한 판단은 각 목표에 요약했다.
- DB schema·접속은 완료된 인계 사항이지만 GameServer 저장·복원 연동은 미구현이다. 이번 목표에서 SQL 변경이나 DB 설계를 시작하지 않는다.

## 범위와 제외

포함: 기존 프로젝트 목표 루프의 마일스톤 재개 안내, M0–M3 및 후속 DB 설계 목표 링크, 기존 동작의 서버/Unity 실행 기준선, [로컬 평가 방법](../../milestones/2026-09-29-refactor-before-persistence/assessment.md) 확정과 실제 측정 가능 범위 확인.

제외: 게임 소스·프로토콜 변경, DB 연결 구현, PC/Legacy 정리 재개, 임의 전역 설정 변경. 미래 목표 파일 작성은 구현 착수가 아니다. 각 목표는 최신 main의 별도 브랜치와 독립 검증·PR을 거치고, 각 병합 직전 사용자 명시 승인을 받는다.

## 파일 소유와 병렬 경계

| 역할 | 소유/허용 범위 | 경계 |
|---|---|---|
| 메인 | 범위·결정·goal 결과/PR 통합 | 구현자 쓰기 종료 후 문서 소유권 인수 |
| 문서 작업자 | 목표 루프와 references/milestones.md, 본 로드맵/평가 방법, 목표들, CURRENT·INDEX 링크 | 게임 소스·Git·테스트 실행 금지 |
| 서버 검증 작업자 | 별도 로그/결과 산출물 | DEVELOPMENT 부작용 확인, 다른 실행 소유자와 포트/WSL 작업 공간 조율 |
| Unity 검증 작업자 | 별도 로그/결과 산출물 | 에디터 사용·DLL 반영 범위를 메인과 조율, 자산 원본 보존 |
| 독립 검증자 | 완료조건·근거 검토 | 구현자와 분리, 소스/문서 동시 쓰기 금지 |

## 완료조건과 검증

- [x] 로드맵은 의존성/링크, 각 goal은 기준/상태/결과, CURRENT는 링크만 갖는다.
- [x] 예정 목표마다 범위·제외·완료조건·소유/병렬 경계·검증·인계 기준이 있다.
- [x] 서버 빌드/테스트/선택 봇과 Unity 컴파일/EditMode/실제 플레이를 구분해 기준선 결과를 기록한다. 실행하지 못한 항목은 원인·영향·후속 확인 위치를 명시한다.
- [x] 평가지표·고정 과제·비교 조건을 확정하고 M0 측정 결과 또는 구체적 미측정 범위를 기록한다. 미측정은 성공·0점·공인점수로 대체하지 않는다.
- [x] 독립 검증에서 문서 링크·현재 범위·결과의 근거·미실행 표시를 확인한다.
- [x] 변경·실행 한계·남은 결정을 보고하고 PR을 준비한다. 병합 승인/병합 여부를 별도 기록한다.

실행자는 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)를 따른다. Shared/ClientNet DLL 복사와 WSL 복사본 차이, Unity 에디터 사용 상태, 기존 서버 포트 소유자를 확인한다. 원시 로그는 별도 경로에 남기고 아래 표에는 요약만 넣는다.

## 실제 결과

| 항목 | 현재 결과 | 실행 조건·근거 |
|---|---|---|
| 운영 문서/스킬 | 11파일 작성, 메인에 쓰기 소유권 이전. skill quick_validate 및 독립 내용/링크 검토 통과 | Windows python alias는 실행 불가, WSL python3로 설치된 validator 실행. 설계·작업분할 선행조건 보완 |
| 서버 빌드·테스트 | 빌드 오류 0/경고 7, 테스트 713개 중 통과 708/실패 0/skip 5. 전체 호출 exit 127과 테스트 통과는 구분 | Ubuntu WSL2, SDK 10.0.300, 출발 main과 동일한 게임 코드. 아래 실행 상세와 로컬 server/summary.md |
| 봇 | suite의 실제 TCP·봇 통합 9개 통과 + production MapTransition smoke 1개 통과(exit 0) | suite의 synthetic 맵 공백을 실제 Program/MapDataLoader·맵 바이너리 실행으로 보완. 보스 해금 치트를 사용하므로 실제 처치/보스 전투 검증 아님 |
| Unity 컴파일·EditMode | 재시도에서 147개 전부 통과, 실패/skip 0, 프로세스 exit 0 | 요구/설치 6000.4.7f1 (f3c3c4248748) 일치. 첫 시도 라이선스 중단 후 사용자 Hub 재로그인으로 재시도. 2026-09-29 13:19:06 UTC XML과 exit.json 확인 |
| Unity 실제 플레이 | 미실행 | EditMode와 구분. M1에서 해당 전환 동작 검증 전 반드시 실행 조건을 확보 |
| AI 작업 준비도 | 4고정과제의 근거 설명 보조 평가 29/32. 전체 코드 품질·탐색속도 점수가 아님 | 답안자 refactor_scope_review와 채점자 client_flow_audit 분리. 사전 감사 노출·모델/effort 미확인 조건은 아래 기록 |
| 구조 지표 | 아래 4개 경계의 정성 기준선 기록 | 전체 코드 건수나 전체 품질 점수로 일반화하지 않음 |
| 독립 검증 | 문서 11파일·상대 링크 42개 정상. 지적 1건 보완 후 재검토 통과 | db_scope_review가 작성자와 별도로 절차/원시 실행 결과 확인, review/docs-review.md |
| PR·병합 | [#130](https://github.com/bass131/dawnholder-server/pull/130) 사용자 명시 승인 후 squash 병합 완료 | 2026-09-29 13:47:53 UTC, main `95058e5`. 승인 head `8807ec1` CI 성공·CLEAN 확인 및 SHA 고정 |

기준선에서 발견한 기존 실패는 리팩토링 후 회귀와 구분한다. 기준선 검증만으로 정적 감사 후보의 실행 재현을 주장하지 않는다.

### 실행 상세와 한계

서버 실행은 재사용 작업자의 이전 제한 권한과 새 작업자 생성 한도로 인해 메인이 명령 호출을 맡고, 별도 작업자가 원시 TRX/콘솔의 수치와 검증 범위를 확인했다. 실행 시각은 2026-09-29 13:15:20–13:17:26 UTC다. Windows 빌드/DLL 복사는 수행하지 않았다.

```powershell
wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh test --logger 'trx;LogFileName=baseline.trx' --results-directory /mnt/c/Dev/DawnHolder_Project/.backups/verification/2026-09-29-refactor-baseline/server/results
```

이 호출의 logger 세미콜론이 WSL shell에서 분리되어 테스트는 `--logger trx`로 실행됐고, 뒤의 `--results-directory`가 명령으로 해석돼 전체 호출은 127로 종료됐다. 실제 테스트는 통과했고 WSL TestResults에 생성된 TRX를 로컬 근거 경로로 복사해 독립 검증했다. 같은 테스트를 재실행해 오류 기록을 숨기지 않았다. 다음 실행은 세미콜론 없는 logger 인자 또는 검증된 `--exec` 인자 전달을 사용한다.

skip 5개는 `MapTransition_TenRuns_AllSucceed`, `Hundred_runs_all_succeed`, `CombatSmoke_Lag100ms_KillSucceeds`, `LagSim_250ms_SilentDrop_KillFails`, `BossSmoke_ZeroLag_Succeeds`다. 테스트용 실제 TCP 검증만으로 production 맵/서버 시작, 전체 보스 플레이, Unity, DB를 검증했다고 보지 않는다. 테스트 종료 후 관련 GameServer/testhost/HeadlessBot 프로세스가 남지 않았음을 확인했다.

production 경로는 `wsl -d Ubuntu -- bash 99_Tools/sync-wsl.sh bot MapTransition`으로 별도 확인했다(13:23:22–13:23:50 UTC). 실제 맵 바이너리로 서버가 시작했고 HG → Boss → Ending → Town 이동에서 entity ID와 spawn 좌표 검사가 통과했다. `PASS=1 FAIL=0`, 전체 호출 exit 0이다. Debug 해금 치트를 사용하는 시나리오이며 실제 20킬/보스 처치나 `Console.ReadLine → world.Stop` 정상 종료를 검증하지 않는다. wrapper 종료 후 관련 실행 프로세스 잔류 없음도 확인했다. 로그는 server/production-smoke.log, production-smoke-run.json, MapTransition-server.log, MapTransition-bot.log다.

원시 근거는 로컬 `.backups/verification/2026-09-29-refactor-baseline/` 아래 `server/{run.json,server-baseline.log,results/baseline.trx,summary.md}`, `unity/summary.md`와 `unity/retry-1/`에 둔다. ignored 경로는 다른 PC/checkout에서 없을 수 있으므로 위 검증 요약을 이 goal에 유지한다. 필요한 원시 자료 전달은 별도로 수행한다.

Unity는 설치된 에디터의 `-batchmode -nographics -runTests -testPlatform EditMode`로 실행했다. 테스트 어셈블리 실행에 필요한 스크립트 컴파일/로드가 완료됐으며 nullable·패키지 어셈블리 경고는 남아 있다. Editor 프로세스 종료와 실행 전후 tracked 코드·에셋·설정·DLL 자동 변경 없음을 확인했다. Unity AI/MCP는 사용하지 않았다. 첫 시도의 라이선스 실패 로그도 보존하며 실제 UI 플레이·PlayMode·서버와 Unity의 통합 동작은 이 성공에 포함하지 않는다.

### 구조 기준선

| 고정 경계 | 현재 확인 사실 | 개선 후 같은 경계에서 확인할 것 |
|---|---|---|
| 서버 전이 소유 | GameSession 종료 경로와 MapMigration 목적지 작업이 closing/migrating/mapId를 나눠 해석 | 진입·이동·종료를 직렬화하는 소유자와 엔티티 0/1 불변식 |
| 클라이언트 연결/진입 | NetworkService 연결 bool과 UnityClientSession 종료 정리가 분리, MapTransitionHandler가 여러 화면/예측 부작용을 순서대로 수행 | 세션 종료 원본과 맵 진입의 완료/취소 책임 |
| 파티·퀘스트 | QuestRegistry.OnKill이 PartyState.KillCount를 직접 변경하고 PartyNotifier로 전달 | 멤버십·진행 상태·패킷 통보의 경계 |
| 플레이어 전달값 | PlayerStats.Hp와 PlayerEntity.Hp가 분리돼 있고 CaptureSnapshot은 mutable Stats 참조를 유지 | 현재 HP의 소유권, 캡처 후 불변성, 이동 상태와 저장 상태의 구분 |

이는 확인한 네 경계의 근거 기반 정성 기록이며 전체 교차 쓰기 지점 수나 AI 과제 점수를 산출한 결과가 아니다. 근거 심볼은 각 후속 goal의 범위에 연결돼 있다.

### 고정 과제의 근거 기반 보조 평가

대상 게임 코드는 출발 main `4b2c84e3bb94d91452d0b50af0ed76b107d5d6a6`와 동일하다. rubric v1을 작성한 작업자가 T1–T4에 답하고, 다른 작업자가 실제 코드의 필요한 부분을 확인해 채점했다. 응답자와 채점자 모두 이전 감사 맥락이 있으므로 blind 평가가 아니다. 정확한 하위 런타임 모델 ID/버전·reasoning 설정은 독립 확인하지 못했다. 사용자 제공 root 실행 명령을 하위 런타임 검증으로 대체하지 않았다.

| 과제 | 흐름 | 경계 | 검증 | 사실/불확실성 | 합계 | 답변의 핵심과 보완 |
|---|---:|---:|---:|---:|---:|---|
| T1 연결 수명 | 1 | 2 | 1 | 2 | 6/8 | 연결 bool/세션 종료·서버 actor 정리는 설명. 메뉴 경유 명시 정리/재진입·클라이언트 전송 정리와 중복 완료 검증 설명 누락 |
| T2 맵 진입 | 2 | 2 | 2 | 2 | 8/8 | 출발 제거→목적지 큐→closing 검사→등록 및 클라이언트 pending/씬/roster 경계, 정적 경쟁과 실제 재현을 구분 |
| T3 파티·퀘스트 | 2 | 2 | 2 | 2 | 8/8 | 처치 콜백→Quest 큐→멤버십/진행/개인 해금→포탈 게이트, 해산 정책의 소비자·검증 범위 설명 |
| T4 상태 전달 | 2 | 1 | 2 | 2 | 7/8 | 기본값/현재 HP/얕은 스냅샷·이동 복사를 설명. tick 소유 경계 안의 일관된 캡처 시점 설명 누락 |

총점 29/32이며 항목별 주요 누락 묶음 3개, 확인된 명백한 사실 정정 요청 0개다. 보완 요청을 답안에 사후 반영해 점수를 올리지 않았다. 이는 네 답안의 근거·경계 설명 적합도이며 높은 점수가 현재 구조의 결함이 적다는 뜻은 아니다. 리팩토링 효과는 위 구조 경계의 실제 변경과 함께 판단한다.

응답 재확인/기록은 2026-09-29 13:19:53–13:23:15 UTC에 이루어졌고, 이 구간에서 shell 읽기 7회/rg 12회/고유 파일 21개를 기록했다. 사전 탐색량·탐색시간은 미계측이며 이번 시간/횟수를 처음 접하는 코드의 발견 성과로 사용하지 않는다. 채점자는 13:27:24 UTC 이후 근거와 점수를 기록했다. 원시 답안·16항목별 채점은 로컬 `assessment/answers.md`, `assessment/grades.md`에 있다. M3에서 노출/모델/도구 조건이 다르면 직접적인 향상률로 해석하지 않는다.

## 다음 인계

문서 작성자·서버 결과 검토자·Unity 실행자·독립 평가자 모두 쓰기를 종료했다. 사용자 병합 승인과 통합을 마쳤으며 최신 main에서 [M1](../2026-09-29-refactor-lifecycle/goal.md)의 설계/구현 범위·작업 분할로 이어간다. 이 승인은 후속 PR 병합 승인이 아니다. 서버/Unity/SQL 등 다른 소유자의 실행 자원을 임의 종료하지 않는다. 기본 Git credential helper의 대기로 중단한 푸시는 로그인된 gh helper를 해당 push 명령에만 지정해 완료했으며 전역 인증 설정은 변경하지 않았다.
