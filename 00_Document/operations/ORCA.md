# Orca 작업 운영

역할·파일 소유권·병합 승인은 [AGENTS](../../AGENTS.md), 위임 계약은 [Orca 작업 지침](../../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 따른다. PR 생성은 허용하지만 **각 PR 병합 직전 사용자 명시 승인이 필요하다.** 포괄 승인·메인 판단·자동 병합 예약으로 대신하지 않는다.

## 실행과 감독

- 큰 독립 목표는 별도 Orca 세션·worktree·작업 브랜치로 나눈다. worktree 부모 관계와 Git 시작 커밋은 별개이므로 최신 main 기준점을 확인한다.
- 설치된 `orca-cli`·`orchestration` 스킬에서 선택한 CLI와 버전에 맞는 가이드를 사용한다. 실제 세션의 Run·Task·Dispatch를 기록한다. 일반 서브에이전트 호출은 Orca 실행 증거가 아니다.
- 요청 모델과 실제 launch 설정을 대조한다. 목표 담당자는 메인이 명시한 단일 목표·공간·권한 안에서만 좁은 구현·검증 작업을 분할한다.
- worker는 주입된 live preamble의 확인·heartbeat·결과 절차를 따른다. `worker_done` 수신 후 계약과 결과를 대조하고 재사용·보존·release를 결정한 뒤 delivery를 acknowledge한다.
- 실패 시 `failedStage`·`residualResources`와 공식 recovery 명령을 따른다. 타임아웃은 종료 증거가 아니다. 해당 작업의 자원만 정리하고 사용자·메인 터미널은 유지한다.

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
| 사용자 기존 터미널 재사용, MSSQL 목표 | `state=ready`, `stage=input_accepted`, `turnStart=observed`; prompt 단계 `input_accepted`·`turn_started`. heartbeat 2건, 메인 reply/ack, WSL 권한 질문의 응답·ack 확인. | 문서 기록 시점 `worker_done` 미수신. DB 구현·독립 검증·목표 완료를 뜻하지 않는다. |

초기 시도: Run `run_85f26e01d393`, Task `task_d783e29c2337`, Dispatch `ctx_7e635e1b1d19`. 로컬 원본은 `%TEMP%/dawnholder-orca-smoke-20260929/`에 있다.

기존 터미널 재사용: Run `run_af1e4581e074`, Task `task_8973cd06c2c8`, Dispatch `ctx_cbd3c2c78d02`, Terminal `term_103c9e14-2792-432e-8a0e-b00215dbe0fd`. 로컬 상태 증거는 `.backups/mssql-orca-coordination-20260929/worker-show.json`이다. 작업 공간은 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/feat-mssql-game-schema`, 브랜치는 `bass131/feat-mssql-game-schema`로 문서 정비 공간과 분리했다. 후속 상태는 해당 목표의 실제 결과를 확인한다.
