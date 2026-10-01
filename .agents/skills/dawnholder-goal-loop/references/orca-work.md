# Orca 외부 세션 작업자 위임

구현·테스트 작성·검증 판정을 외부 세션에 맡길 때 읽는다. 역할·모델·태그는 [AGENTS](../../../../AGENTS.md), 공유 자원과 관찰 기록은 [ORCA.md](../../../../00_Document/operations/ORCA.md)에 있다. 내부 서브에이전트는 읽기 전용 조사·요약에만 쓴다.

## 역할과 작업 계약

메인 Claude는 목표·사용자 결정을 관리하고, 지정 파트 리드 Astra만 승인된 목표·공간·권한 안에서 좁은 구현·검증 작업을 한 단계 분할한다. 일반 작업자는 재귀 위임하지 않는다. 새 목표·범위 확대·승인 밖 세션 생성은 메인에 올린다. 독립 목표는 최신 main의 별도 worktree/branch, 같은 목표의 구현·검증은 파일 소유를 순차 이전하는 같은 branch를 사용한다.

위임 입력은 아래 정도로 제한한다.

- 목표와 관찰 가능한 완료 조건.
- 필요한 파일·이미 합의한 결정·입력 자료의 위치.
- 작업자가 수정할 파일 범위와 건드리지 않을 영역.
- 검증 기준 및 DB·7777·Unity·WSL 작업 공간의 실행 소유권.
- 작업 결과를 기록할 위치와 막히면 메인에 올릴 판단 사항.

작업 spec에는 다음 계약도 포함한다: 역할과 자기 발신 태그, 지정 모델과 관찰 모델 구분, **할당된 작업 하나만 수행하고 추가 위임·commit/push 금지**, 제품/테스트 쓰기 소유와 종료 시점, 실사·미실행 구분, 원문 경로, live preamble의 완료 절차. 수정·재검증도 새 세션으로 발행하며 완료 세션은 재사용하지 않는다.

신규 Opus 검증자는 보고와 실제 수행을 실사한 뒤 독립 테스트를 작성·실행한다. 제품 결함은 번호로 반환하고 제품 파일은 수정하지 않는다. Astra는 **변경 요약 / 검증 근거 위치 / 리스크 / 결정 요청 / 판정 원문 경로**를 메인에게 전달한다.

## pane 생성과 작업 연결

1. 설치된 `orca-cli`·`orchestration` 스킬에서 CLI를 선택하고 버전 일치 가이드를 읽는다. 현재 runtime·담당 Astra pane·정확한 작업 경로를 확인한다. 아래 `orca` 예시는 선택한 실행 파일로 바꿔 사용한다.
2. **담당 Astra pane 아래로 분할**하며 새 작업자를 연다. Sol은 `orca terminal split --terminal <Astra handle> --direction vertical --command 'codex --model gpt-6.1-sol -c model_reasoning_effort=xhigh'`, 검증자는 같은 split에 `--command 'claude --model claude-opus-5-5'`를 쓴다. 시작 경로가 다르면 해당 CLI의 작업 경로 지정 방법으로 승인된 checkout을 명시하고 실제 경로를 확인한다.
3. `orca terminal wait --terminal <새 handle> --for tui-idle --timeout-ms 90000`의 `satisfied`를 확인한다. timeout·모달·busy만으로 입력하지 않는다. 준비 확인은 [세션 인계 스킬](../../dawnholder-session-handoff/SKILL.md)을 따른다.
4. 준비된 **새 세션의 최초 작업**을 `orca orchestration worker-start --terminal <새 handle> --worktree <확인한 작업 공간>`에 `--task <준비된 Task>` 또는 `--spec <작업 계약>`을 붙여 연결한다. `--terminal`에는 `--model`을 함께 쓸 수 없으므로 split 명령의 모델과 화면 표시를 기록하고 백엔드는 확인 불가 시 `unknown`으로 둔다. 해당 목표의 Run·현재 Task/Dispatch와 실제 시작 receipt를 기록한다.
5. 연결이 명시적으로 거부되면 receipt와 미사용 여부를 확인한 뒤 새 pane을 닫고 `worker-start --agent <codex|claude> --model <지정 모델>` 방식으로 진행한다. 이 경로는 `launch.requested/effective`와 화면 표시를 기록하고 배치 한계·거부 결과를 메인에 보고한다. 결과 미확정이나 주입 여부가 불명확하면 중복 발행하지 않고 공식 recovery를 따른다.

이 분할→최초 연결 절차는 2026-10-01 사용자 채택 사항이며 초기 시범 두 작업자는 직접 `worker-start --agent --model`로 실행했다. 실제 분할→연결 성공은 아직 미검증이다. 세부 관찰은 [현재 goal](../../../../00_Document/operations/CURRENT.md)에 둔다. 단순 전체 handoff에는 감독형 추적을 만들지 않는다.

## 수행·정산·종료

- worker는 live preamble의 check·heartbeat·완료 절차를 따르며 메시지 subject/body에도 자기 태그를 붙인다. Claude 권한 확인에 막히면 명령·화면 상태를 보고한다. 실행 정책·권한 우회 기준은 [AGENTS 공학 조건](../../../../AGENTS.md#공학-조건)을 따른다.
- 정상 완료는 정확한 `worker_done`과 원문을 Astra가 대조한 뒤 `worker-release`, 해당 작업 pane의 정산·동일성을 확인한 뒤 `terminal close`로 닫는다. 이미 닫혔으면 다시 닫지 않는다. release가 `retained`여도 승인된 작업 하나가 끝난 정확한 pane인지 확인한 뒤 프로젝트의 종료 규칙을 적용한다. 메인·Astra·다른 사용자 세션은 종료하지 않는다.
- 실패·막힘·무응답은 관측 사실·명령·미완료 결과·원문을 진단 기록으로 보존한 뒤 상태에 맞는 공식 정산/중단 절차와 종료를 수행한다. timeout만으로 성공·프로세스 종료를 주장하거나 중복 작업자를 만들지 않는다. 원격/대상 동일성을 확인하지 못하면 임의 다른 대상을 닫지 않고 메인에 보고한다.
- 결함 수정과 재검증은 새 세션으로 발행한다. 완료 세션을 `worker-start --terminal`로 다음 작업에 재사용하지 않는다. 정산과 terminal close 결과를 goal 근거에 남기고 전체 Delivery를 처리한 뒤 acknowledge한다.
- PR 생성은 허용하지만 각 PR 병합 직전에는 사용자 명시 승인이 필요하다. 메인 판단·이전 포괄 승인·CI 통과로 대신하지 않으며 자동 병합은 금지한다.

`98_Shared/Protocol`과 생성 파일의 동시 수정, 공유 WSL clone 동기화, 7777 포트 경쟁, 공용 DB 쓰기, Unity Library 공유를 피한다. 구체적인 실행 방식은 [DEVELOPMENT.md](../../../../00_Document/operations/DEVELOPMENT.md)에서 확인한다.

이것은 운영 규약이며 기술적 강제·맥락의 완전한 격리·무인 복구를 보장하지 않는다. 내부 서브에이전트 실행을 외부 작업자나 Orca runtime 검증의 근거로 삼지 않는다.
