# Orca 작업 운영

역할·파일 소유권·병합 승인은 [AGENTS](../../AGENTS.md), 위임 계약은 [Orca 작업 지침](../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 따른다. PR 생성은 허용하지만 **각 PR 병합 직전 사용자 명시 승인이 필요하다.** 포괄 승인·메인 판단·자동 병합 예약으로 대신하지 않는다.

## 실행과 감독

메인 Claude·GameDev Astra·Management Astra는 [RESUME](RESUME.md#세션-진입-배치)의 좌우 pane 배치를 사용하고 작업자는 담당 Astra 아래 pane을 사용한다. Management 실제 경로는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`다. 화면 소속과 작업 경로를 구분하고 runtime·terminal handle·incarnation은 사용할 때 확인한다. 채택 근거와 시범 결과는 [CURRENT](CURRENT.md)의 goal을 따른다.

- 큰 독립 목표는 별도 Orca 세션·worktree·작업 브랜치로 나눈다. worktree 부모 관계와 Git 시작 커밋은 별개이므로 최신 main 기준점을 확인한다.
- 설치된 `orca-cli`·`orchestration` 스킬에서 선택한 CLI와 버전에 맞는 가이드를 사용한다. 구현·테스트 작성·검증 판정은 외부 세션 작업자가 맡고 실제 Run·Task·Dispatch를 기록한다. 내부 서브에이전트는 읽기 전용 조사·요약에만 쓰며 외부 실행 근거가 아니다.
- 모델은 직접 agent 시작의 요청/적용값과 화면 표시를 대조한다. 새 pane의 최초 `--terminal` 연결은 split 명령과 화면 표시를 근거로 쓰고 백엔드 실제 모델은 확인 불가 시 `unknown`이다. 구체적인 생성·준비·거부 시 처리는 [Orca 위임 지침](../../.agents/skills/dawnholder-goal-loop/references/orca-work.md#pane-생성과-작업-연결)에 둔다.
- 메시지 subject/body와 타 세션 입력은 [AGENTS 태그 규칙](../../AGENTS.md#메시지와-보고)을 따른다. 메인은 세션 시작 때 자기 handle을 두 Astra에 공유한다.
- worker는 live preamble의 확인·heartbeat·결과 절차를 따른다. 작업 하나가 끝나면 `worker_done`과 원문 대조 → release → 정확한 pane 확인·close로 정리하고 재사용하지 않는다. 실패·막힘·무응답은 진단을 보존한 뒤 공식 정산/중단·종료를 수행한다. 수정·재검증은 새 세션으로 발행하고 전체 delivery 처리 뒤 acknowledge한다.
- 실패 시 `failedStage`·`residualResources`와 공식 recovery 명령을 따른다. 타임아웃은 종료 증거가 아니다. 해당 작업의 자원만 정리하고 사용자·메인 터미널은 유지한다.
- 실행 정책·권한 우회 기준은 [AGENTS 공학 조건](../../AGENTS.md#공학-조건)을 따른다.

## 공유 자원

| 자원 | 확인할 경계 |
|---|---|
| Shared 프로토콜·생성 코드·DLL | 담당자 하나를 지정해 동시 생성·복사를 피한다. 소비자 양쪽 빌드와 호환성을 확인한다. |
| Unity | checkout 간 Library·Editor 프로젝트를 공유하지 않는다. 필요한 검증만 지정한 작업자가 실행한다. |
| WSL | [실행 안내](DEVELOPMENT.md)의 원본 경로 해시별 복제 공간·소유 marker·lock을 사용한다. override도 목표 전용 Linux 경로여야 한다. |
| 게임 포트 7777 | 단일 실행 소유자를 정하고 전역 lock·listener 검사를 따른다. 다른 프로세스를 임의 종료하지 않는다. |
| DB | 대상 DB와 마이그레이션 담당자를 정한다. worktree 분리만으로 DB가 격리되지는 않는다. |

원문 로그는 목표 evidence 또는 TEMP에 보관한다. 메인에는 변경·검증 요약과 근거 위치를 전달한다. 이 지침은 운영 규약이며 실행·컨텍스트 격리를 기술적으로 강제하는 별도 시스템이 아니다.

## 관찰 기록: 2026-09-29

두 시도는 시작 방식과 확인 범위가 다르다. 연결·작업 주입·완료 수신을 각각 구분한다.

| 시도 | 실제 관찰 | 남은 확인 |
|---|---|---|
| 초기 새 worker 자동 시작 | Orca 1.4.216 연결, 요청·실제 모델 `gpt-6-astra` 일치. Codex TUI는 열렸으나 `agent_readiness`에서 60초 timeout. Task 주입과 `worker_done` 없음. 공식 release 후 `reclaimable=0`, 기존 메인 터미널 유지. | 새 worker 자동 시작·지침 선택·완료 수신은 미검증. 정확한 readiness 실패 원인은 미확정. |
| 사용자 기존 터미널 재사용, MSSQL 목표 | 실제 작업 주입·heartbeat·질문 응답 후 기본 테이블 구현과 독립 Windows 테스트 40개 통과. 사용자 승인 하 관리자 설정 적용 후 네이티브 WSL 로그인·저장·rowversion·rollback·최소 권한 검사 통과. | 기록 시점 최종 push·CI와 `worker_done` 수신 마감 중. 서버 런타임 저장 통합과 관리자 Restore 실행은 미검증. 새 worker 자동 시작 성공으로 해석하지 않는다. |

초기 시도: Run `run_85f26e01d393`, Task `task_d783e29c2337`, Dispatch `ctx_7e635e1b1d19`. 로컬 원본은 `%TEMP%/dawnholder-orca-smoke-20260929/`에 있다.

기존 터미널 재사용: Run `run_af1e4581e074`, Task `task_8973cd06c2c8`, Dispatch `ctx_cbd3c2c78d02`, Terminal `term_103c9e14-2792-432e-8a0e-b00215dbe0fd`. 로컬 상태 증거는 `.backups/mssql-orca-coordination-20260929/worker-show.json`이다. 작업 공간은 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/feat-mssql-game-schema`, 브랜치는 `bass131/feat-mssql-game-schema`로 문서 정비 공간과 분리했다. 결과와 당시 인계는 별도 [PR127](https://github.com/bass131/dawnholder-server/pull/127)에 기록했다. SQL 테스트의 트랜잭션 rollback 통과와 관리자 설정 Restore 실행 검증은 구분한다. 당시 관찰 시점에는 PR 병합 승인이 없었다.
