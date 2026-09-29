# Orca 작업 운영

메인 세션은 사용자와 목표·범위·완료 조건을 합의하고 중요한 결정을 다룬다. 구현·검증·테스트는 작업자에게 위임하고 메인은 요약과 근거 위치를 받아 통합한다. 이 문서는 프로젝트 운영 규약이며, 자동 스케줄러나 컨텍스트 격리를 기술적으로 보장하는 시스템은 아니다. 별도 AgentDeck 프로그램 개발은 이번 범위에서 제외한다.

## 시작 지점

일반 작업 진입점은 [dawnholder-goal-loop](../../.agents/skills/dawnholder-goal-loop/SKILL.md), Orca를 사용하는 경우의 작업 계약은 [orca-work](../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 따른다. 일반 하위 작업 분담과 실제 Orca 세션의 실행·감독은 구분한다. 일반 서브에이전트 호출만으로 Orca 실행을 검증했다고 보고하지 않는다.

명령은 설치된 `orca-cli`·`orchestration` 스킬이 선택한 CLI의 버전 일치 가이드에서 확인한다. 매 세션 CLI 선택 규칙을 적용하고 선택한 실행 파일을 끝까지 사용한다. 가이드·상태 확인이 실패하면 오류를 기록하며 다른 실행 파일로 바꾸지 않는다. 모델은 사용자 선택 Astra를 존중하고, 명시해 실행했다면 요청 모델과 실제 launch 설정을 대조한다.

## 작업을 나누는 기준

| 범위 | 작업 공간과 책임 |
| --- | --- |
| 큰 독립 목표 | 별도 Orca 세션 + worktree + 작업 브랜치. 최신 main에서 시작하고 PR로 통합한다. |
| 한 목표 안의 좁은 작업 | 메인이 명시 지정한 Orca 목표 담당자만 허용된 단일 목표·작업 공간·권한 안에서 구현·검증 작업을 한 단계 분할한다. 일반 작업자는 추가 위임하지 않는다. |
| 별도 검증 | 검증자가 diff·완료 조건·검증 기준으로 검사하고 필요한 실행을 담당한다. 결함은 구현 담당자에게 반환한다. |
| 중요한 설계·범위 분기 | 작업자가 선택지·근거·영향을 메인에 전달하고 메인이 사용자와 결정한다. |

새 독립 목표·범위 확대·추가 Orca 세션 생성은 원래 메인에 요청한다. 같은 파일을 동시에 수정하지 않는다. 중요한 결정과 사용자 논의는 원래 메인이 맡는다.

worktree의 부모 관계와 Git 시작 커밋은 별개다. 독립 목표를 현재 기능 브랜치에서 우연히 분기하지 않는다. main 갱신 여부와 시작 커밋을 기록하고, 작업 브랜치 완료·원격 반영·PR·main 병합을 각각 구분한다. 병합 후에는 통합된 코드 기준으로 영향 범위를 재검증한다. 이 문서가 외부 발행 범위나 기존 사용자 변경의 소유권을 확대하지는 않는다.

## 공유 자원과 실행 소유권

- `98_Shared/Protocol`, 생성 코드, Shared/ClientNet DLL 배포는 여러 목표에 영향을 준다. 변경 담당자를 하나 정하고 동시 생성·복사·버전 변경을 피한다. 통합 뒤 소비자 빌드와 패킷 호환성을 확인한다.
- Unity Editor는 목표마다 자동 실행하지 않는다. 각 checkout의 `Library`는 크고 재생성 비용이 있다. 서로 다른 worktree가 같은 Library나 Editor 프로젝트를 공유하지 않으며, 필요한 경우 지정한 Unity 검증 작업자 하나가 실행한다.
- WSL 명령은 [DEVELOPMENT.md](DEVELOPMENT.md)와 `99_Tools/sync-wsl.sh`를 따른다. 스크립트는 Windows 원본 루트별 해시로 `~/.cache/dawnholder/workspaces/<20hex>`를 나누고 소유 marker·workspace lock을 사용한다. 서로 다른 목표를 과거의 단일 clone으로 동기화하지 않는다.
- `DAWNHOLDER_WSL_ROOT`를 쓰면 해당 목표의 전용 Linux 작업 공간을 지정한다. 다른 목표의 경로를 재사용하지 않는다. 동일 저장소 원본에서 같은 복제 공간으로 실행하는 명령도 lock을 존중한다.
- 게임 서버 포트 `7777`은 현재 단일 실행 소유자만 사용한다. WSL 전역 lock과 실제 listener 검사를 통과해야 하며, 다른 작업의 프로세스를 강제 종료하지 않는다. 임의 포트 변경으로 격리가 끝났다고 간주하지 않는다.
- DB 대상과 스키마 변경도 실행 소유권을 정한다. 복수 작업자가 같은 개발 DB에 쓰기·마이그레이션을 병행하지 않는다. worktree가 다르다는 이유로 DB가 격리된 것은 아니다.
- 원본 실행 로그는 목표의 evidence 또는 임시 증거 폴더에 보관한다. 메인에는 변경 요약·검증 근거 위치·리스크·결정 요청만 전달한다. 필요할 때 해당 근거를 읽는다.

## 실제 세션 검증과 종료

감독형 작업은 실제 Orca Run·Task·Dispatch의 결과로 추적한다. 작업 계약에는 목표·입력·소유 파일·금지 범위·완료 조건·검증 기준을 포함한다. worker는 주입된 live preamble의 확인·heartbeat·결과 보고 절차를 따른다.

검증 가능한 `worker_done`을 받은 뒤 결과와 Dispatch를 대조하고, 재사용·명시적 보존·release 중 하나를 결정한 다음 delivery를 acknowledge한다. 마지막에 정리가 필요한 terminal이 남지 않았는지 확인한다. 타임아웃이나 화면 정지만으로 프로세스가 종료됐다고 판단하지 않는다. 실패 시작의 복구는 응답의 `failedStage`, `residualResources`, 공식 recovery 명령을 따른다. 기존 메인·사용자 터미널을 종료하지 않는다.

## 2026-09-29 초기 smoke 결과

**Orca 연결과 실패 자원 정리는 확인했지만, worker 작업 수행·완료 수신까지는 통과하지 못했다.** 이후 환경 또는 통합 문제가 해결되기 전에는 큰 작업의 자동 감독을 검증 완료로 표시하지 않는다.

| 확인 항목 | 결과 |
| --- | --- |
| 런타임 | Orca 1.4.216, running / ready / reachable / connected |
| 프로젝트 | 현재 DawnHolder checkout과 `chore/codex-project-setup` 등록 확인 |
| 제한된 시도 | 현재 checkout에서 읽기 전용 worker 1개. 소스·Git 변경, 빌드·테스트·DB·게임 서버·Unity 실행 금지 |
| 모델 | `launch.requested.model`과 `launch.effective.model` 모두 `gpt-6-astra` |
| 시작 실패 | `state=failed`, `failedStage=agent_readiness`, `lastError=timeout` (60초) |
| 실제 관찰 | Codex 0.159.0 초기 입력 화면까지 표시. 명시적인 trust/auth 질문은 보이지 않았음. Orca `agentWait=null`, `missing_status`, `session_not_reported` |
| 원인 범위 | Codex TUI의 app-server 연결·초기 frame 로그 확인. Orca의 readiness 상태 연결 문제 가능성이 있으나 정확한 원인은 미확정 |
| 수행 여부 | Task 주입 전 실패. `worker_done` 미발생, 새 프로젝트 지침·스킬 자동 발견도 미검증 |
| 복구 | 응답이 지정한 `worker-release`로 해당 신규 worker만 종료·출력 보관. `reclaimable=0`, 기존 메인 터미널 1개 유지 |
| 수신함 | `deliveryId=null`, messages 0. acknowledge할 delivery가 없었음 |

근거: Run `run_85f26e01d393`, Task `task_d783e29c2337`, Dispatch `ctx_7e635e1b1d19`. 원본 응답은 당시 로컬 `%TEMP%/dawnholder-orca-smoke-20260929/`의 `01`–`10` 파일에 보관했다. 임시 파일이 사라졌을 때는 Orca의 해당 Dispatch 보관 출력과 이 요약을 구분해 사용한다.

다음 확인은 Orca–Codex readiness 연결의 원인을 좁히는 것이다. 초기 화면 경고·상태 보고 경로를 확인하고 구체적인 원인과 해결 근거가 생긴 뒤 같은 제한 계약으로 1회 재검증한다. 모델·CLI 교체나 timeout 증가만으로 반복 기동하지 않는다. 실제 작업·완료 수신·정리가 모두 통과한 뒤에만 이 상태를 갱신한다.
