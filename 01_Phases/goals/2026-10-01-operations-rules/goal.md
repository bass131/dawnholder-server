# 2026-10-01 운영 규칙 문서 정리

## 목표와 범위

2026-10-01 운영에서 확정한 결정과 관측 사실 R-1~R-8을 현재 운영 문서에 반영한다. 메인 Claude가 전달한 사용자 승인 범위이며 전달 메시지를 사용자 직접 입력으로 격상하지 않는다. 승인 원문은 로컬 `.backups/verification/2026-10-01-operations-rules/main-request.json`과 `main-request-r8.json`에 보존한다.

- 새 규칙 정본: [ORCA 운영 문서](../../../00_Document/operations/ORCA.md). 다른 현재 문서와 프로젝트 스킬은 정본 링크를 사용하고 상충하는 기존 설명을 제거한다.
- 대상: `AGENTS.md`, `CLAUDE.md`, `00_Document/operations/{ORCA,RESUME,CURRENT}.md`, `.agents/skills/dawnholder-goal-loop/`의 본문·Orca 위임 지침, `.agents/skills/dawnholder-session-handoff/SKILL.md`, 이 goal.
- 제외: `05_Management/RESUME.md`, 제품·테스트 코드, SQL·Unity·게임 실행, Claude/Codex 설정 변경, 새 Fable 시범 목표 실행, PR 병합.

## 완료조건과 보존 계약

1. R-1~R-8이 ORCA 문서의 각각 고정 anchor에 있고 사용자 결정·관측·한정 시범·미확인 범위가 구분된다.
2. AGENTS·CLAUDE·RESUME·관련 스킬의 진입 링크가 유효하고 이전 배치/미검증 설명과 모순되지 않는다. 새 규칙 상세를 여러 문서에 복제하지 않는다.
3. 메인 CLAUDE 쓰기 종료와 Sol 쓰기 종료 후 신규 외부 Opus가 실제 diff·근거·권한 경계를 실사한다. 필수 결함은 새 Sol/Opus 세션으로 수정·재검증한다.
4. goal에 검증 원문·실제 수행/미수행·정산을 기록하고 PR을 만든다. 각 PR 병합은 사용자 명시 승인 대기다.

Unity 사용자 3파일의 skip-worktree·내용과 stash 2개를 보존한다. 두 checkout의 `.claude/settings.local.json`은 읽기 전용이며 기초 해시는 evidence의 `settings-before.json`에 있다. 고정 보관 브랜치는 변경하지 않는다.

## 소유권과 실행 계획

- 메인 Claude: `CLAUDE.md` 단독 작성, 사용자 결정·최종 원문 확인·승인 전 원천 표본 대조.
- GameDev Astra: goal/CURRENT, 설계·범위·외부 위임, Git·PR·결과 통합. 같은 파일의 동시 쓰기와 작업자 commit/push를 금지한다.
- 신규 GameDev Sol (`gpt-6.1-sol`, xhigh): CLAUDE·goal·CURRENT를 제외한 허용 운영 문서와 스킬 수정. 작업 하나 후 정산·종료.
- 신규 GameDev 검증자 (`claude-opus-5-5`): 문서 정적 실사와 로컬 verdict만 작성. 제품 문서 수정 금지. 구현자와 분리하며 작업 하나 후 정산·종료.

최초 실행 명령·화면 모델과 백엔드 확인값을 구분한다. 백엔드 실제 모델을 확인할 수 없으면 `unknown`이다. 문서 실사는 코드·DB·게임 실행 검증을 대신하지 않는다.

## 현재 상태

- 기준: `origin/main` = `e06a74b90fd570807fd34c95193e8368ab109623` (PR157 병합).
- checkout: `C:/Dev/DawnHolder_Project`, branch: `bass131/operations-rules-20261001`.
- Orca Run: `run_b807dbc5e6af`. 이전 목표의 Run/Task/Dispatch를 새 작업 권한으로 재사용하지 않는다.
- 2026-10-01: 메인 R-1~R-8 요청 수신, 정본·파일 소유권 확정. 메인 `msg_3cbdcc8c99c2`로 CLAUDE 쓰기 종료. 자동 로딩되는 CLAUDE에는 메인 의무 한 줄과 정본 anchor를 남기고 상세 규칙은 ORCA에만 둔다는 메인 보완 설명을 수신했다.
- Sol: Task `task_3c9cb392e40c`, Dispatch `ctx_ee89e4f2875d`; 새 pane split 후 최초 연결. `sol-1-start.json`에 ready·input_accepted·turn_started를 보존했다. 화면 `GPT-6.1-Sol xhigh`, backend `unknown`. `msg_bed347ea543e`로 쓰기 종료·worker_done succeeded를 받았다. 원문은 `sol-1-report.md`, 최초 실행 명령은 `sol-1-spec.txt`의 요청과 Astra 실행 기록에 있으며 attach launch값은 null이다.
- Sol 보고와 실제 허용 6파일 diff를 대조했다. 자체 점검의 109개 상대 링크·57개 anchor 참조·고정 anchor 8개 확인은 독립 실사를 대신하지 않는다. 정확한 runtime·incarnation 대조 후 release(retained/external_terminal)·close(ptyKilled=true)를 완료했다. 근거: `sol-1-before-close-show.json`, `sol-1-release.json`, `sol-1-close.json`, `sol-1-done.json`.
- Sol이 R-3의 CLI 제약을 발견했고 Astra·메인이 `reply --help`로 대조했다. 메인 `msg_fc7e6335130c`의 보완 결정은 `main-r3-decision.json`에 보존하며 실행 가능한 회신 절차의 정본은 [R-3](../../../00_Document/operations/ORCA.md#r3-reply-tag)에 둔다. CLAUDE 요약은 정본 링크와 함께 유지한다는 메인 지시다.
- 근거 루트: `.backups/verification/2026-10-01-operations-rules/` (Git 제외).
- 메인과 Sol 모두 쓰기 종료. 검증 대상 커밋을 고정한 뒤 신규 Opus 실사 예정이며 PR은 아직 생성 전이다.

## 결과와 인계

작업 진행 후 실제 검증 결과·원문·PR·남은 우려를 갱신한다. 이번 목표 종료 뒤 Astra 세션 인계는 [R-8](../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)을 따른다. D1b와 M-1 후속 작업은 이 문서 목표의 범위가 아니다.
