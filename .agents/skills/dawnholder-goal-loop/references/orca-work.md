# Orca 외부 세션 작업자 위임

구현·테스트 작성·검증 판정을 외부 세션에 맡길 때 읽는다. 역할·모델·태그는 [AGENTS](../../../../AGENTS.md), 공유 자원과 관찰 기록은 [ORCA.md](../../../../00_Document/operations/ORCA.md)에 있다. 내부 서브에이전트는 읽기 전용 조사·요약에만 쓴다.

여기서 외부 세션 작업자는 **이 머신에서 담당 리드가 기동하는 로컬 Orca 작업자**다. 사람 팀원이 자기 머신에서 실행하는 세션에는 이 기동·goal·Sol·검증자·배치 절차를 적용하지 않는다. 팀원 권한은 [AGENTS 외부 팀원 세션](../../../../AGENTS.md#외부-팀원-세션), 산출물 안내는 `00_Document/game-design/README.md`를 따른다. 담당 문서의 품질 기준을 적용해도 `game-design/` 밖 쓰기 권한은 생기지 않는다.

## 역할과 작업 계약

메인 Claude는 목표·사용자 결정을 관리하고, 지정 파트 리드만 승인된 목표·공간·권한 안에서 좁은 구현·검증 작업을 한 단계 분할한다. 일반 작업자는 재귀 위임하지 않는다. 새 목표·승인 밖 세션 생성은 메인에 올린다. 새 후보·범위 안 수정·애매함/완료조건 변경의 판정은 [goal-loop 범위 정본](../SKILL.md#기준과-상태)을 따른다. 독립 목표는 최신 main의 별도 worktree/branch, 같은 목표의 구현·검증은 파일 소유를 순차 이전하는 같은 branch를 사용한다.

위임 입력은 아래 정도로 제한한다.

- 요구항목·보존 계약·판정 기준과 관찰 가능한 완료조건, [검증 등급·이유](../SKILL.md#검증-강도-4주-시범).
- 필요한 파일·이미 합의한 결정·입력 자료의 위치.
- [작업 맥락 스킬](../../dawnholder-task-context/SKILL.md)의 역할별 읽기 묶음·기존 예시 1~2개와 쓰기 전 메모 경로, 관련 CODE_CONVENTION 절 원문 전체·출처 경로·기준 commit 또는 미커밋 hash. 링크·요약만으로 원문을 대체하지 않는다.
- 작업자가 수정할 파일 범위와 건드리지 않을 영역.
- 검증 기준과 실제 진입 실행 범위 및 DB·7777·Unity·WSL 작업 공간의 실행 소유권.
- 작업 결과를 기록할 위치와 막히면 메인에 올릴 판단 사항.

작업 spec에는 다음 계약도 포함한다: 역할과 자기 발신 태그, 지정 모델과 관찰 모델 구분, **할당된 작업 하나만 수행하고 추가 위임·commit/push 금지**, 제품/테스트 쓰기 소유와 종료 시점, 실사·미실행 구분, 원문 경로, live preamble의 완료 절차. 수정·재검증도 새 세션으로 발행하며 완료 세션은 재사용하지 않는다.

발행 전 계약 경로를 실제 작업자 폴더에서 기계 확인하고 관련 원문/제외 이유·harness 저장·설계 대안·원시 수치 필드는 [task-context의 계약 기준](../../dawnholder-task-context/SKILL.md#파일-쓰기-전-메모와-원문-계약)과 [계약 양식](../../dawnholder-task-context/references/templates.md#위임-계약)을 따른다. [R-7 설계 시범](../../../../00_Document/operations/ORCA.md#r7-fable-pilot) 해당 작업은 계약 발행 전에 시범 검토·메인 원문 확인·보완 goal 승인을 마치고 불변식 파일을 고정 입력에 넣는다.

지시는 메시지 자체와 명시한 goal/계약만으로 이해할 수 있게 쓰며 앞 대화 기억을 가정하지 않는다. 압축 뒤 현재 goal·계약·고정 입력을 다시 읽고 이어간다. 특정 컨텍스트 크기 숫자를 현재 모든 모델 사양으로 고정하지 않는다.

이미 발행한 작업에는 새 범위를 주입하지 않는다. 범위 축소는 현재 작업자에게 명시 통지하고 빠진 범위·후속 요청은 다음 계약에 반영한다. 진행 중인 검증의 고정 입력을 조용히 바꾸지 않으며 변경된 요구/메인 판단의 원문·시각·계약 버전·적용 단계와 다음 입력 hash를 남긴다. 안전한 쓰기 종료 뒤 다음 버전을 발행하고 이전 판정의 입력/원문을 보존한다. 필수 판정 기준의 보충도 전달 여부와 적용 버전을 기록한다.

신규 검증자는 [시범 모델 배정](../SKILL.md#검증자-모델-시범2026-10-31까지)에 따라 보고와 실제 수행을 실사한 뒤 [강/약/문서의 필수 검증](../SKILL.md#검증-강도-4주-시범)을 수행한다. 독립 세션은 생략하지 않는다. 제품 결함은 번호로 반환하고 제품 파일은 수정하지 않는다. 리드는 **변경 요약 / 검증 근거 위치 / 리스크 / 결정 요청 / 판정 원문 경로**를 메인에게 전달하며 보고 유형은 [R-4](../../../../00_Document/operations/ORCA.md#r4-report-type)를 따른다.

작업자는 [맥락 메모·계약·판정 양식](../../dawnholder-task-context/references/templates.md)에 실제 준수 위치를 남긴다. 신규 검증자는 자기 파일 쓰기 전 메모를 작성하고 내용·가독성·배치/이름을 실제 diff와 대조한다. 메모 부재·관련 원문 누락·적용 규칙 위반·메모/결과 불일치는 수정 또는 메인 결정 전 통과 차단이며, 등급별 필수 검증·실제 진입 실행과 문서 실사를 구분한다.

추가 차단사유·기존 실패 전수 분류는 [task-context 판정](../../dawnholder-task-context/SKILL.md#독립-판정과-통과-차단), 확정 실패3회 뒤 Sol/Fable의 직접 질문·파일 권한·기동 전 보고·네 번째/다섯 번째 경계는 [ORCA 실패 정본](../../../../00_Document/operations/ORCA.md#confirmed-failures)을 따른다. R-7 구현 전 설계 검토 시범으로 대체하지 않는다.

## pane 생성과 작업 연결

- 세션과 작업자의 배치는 [R-1](../../../../00_Document/operations/ORCA.md#r1-management-placement)을 따른다.
- 생성·준비·최초 연결·모델 근거·실패 처리는 [R-5](../../../../00_Document/operations/ORCA.md#r5-worker-launch)를 따른다.
- 첫 화면과 선택창 판단은 [R-6](../../../../00_Document/operations/ORCA.md#r6-first-screen), 준비 확인 절차는 [세션 인계 스킬](../../dawnholder-session-handoff/SKILL.md)을 따른다.
- capacity 한정 재시도/신규 `gpt-6-astra` 작업자 예외와 공식 계약 draft의 조건부 Enter 제출은 [R-5 상세](../../../../00_Document/operations/ORCA.md#capacity-retry)·[draft 정본](../../../../00_Document/operations/ORCA.md#official-contract-draft)을 따른다. Run 바인딩 직후 [현재 회신 주소](../../../../00_Document/operations/ORCA.md#run-reply-address)를 메인에게 알린다.

실패 이력과 후속 실증의 범위는 [R-5 근거](../../../../00_Document/operations/ORCA.md#r5-worker-launch)에 둔다. 단순 전체 handoff에는 감독형 추적을 만들지 않는다.

## 수행·정산·종료

- worker는 live preamble의 check·heartbeat·완료 절차를 따르며 메시지 subject/body에도 자기 태그를 붙인다. 회신 subject는 [R-3](../../../../00_Document/operations/ORCA.md#r3-reply-tag)을 따른다. Claude 권한 확인에 막히면 명령·화면 상태를 보고한다. 실행 정책·권한 우회 기준은 [AGENTS 공학 조건](../../../../AGENTS.md#공학-조건)을 따른다.
- 내용 없는 heartbeat와 공식 blocking ask의 subject 한정 예외·세 identity 대조·상태 변이 없는 수신 helper는 [수신 정본](../../../../00_Document/operations/ORCA.md#dispatch-message-policy)을 따른다. 태그/정책 판정을 worker_done 정산이나 자동 ack로 해석하지 않는다.
- 정상 완료는 정확한 `worker_done`과 원문을 리드가 대조한 뒤 `worker-release`, 해당 작업 pane의 정산·동일성을 확인한 뒤 `terminal close`로 닫는다. 이미 닫혔으면 다시 닫지 않는다. release가 `retained`여도 승인된 작업 하나가 끝난 정확한 pane인지 확인한 뒤 프로젝트의 종료 규칙을 적용한다. 메인·리드·다른 사용자 세션은 종료하지 않는다.
- 실패·막힘·무응답은 관측 사실·명령·미완료 결과·원문을 진단 기록으로 보존한 뒤 상태에 맞는 공식 정산/중단 절차와 종료를 수행한다. timeout만으로 성공·프로세스 종료를 주장하거나 중복 작업자를 만들지 않는다. 원격/대상 동일성을 확인하지 못하면 임의 다른 대상을 닫지 않고 메인에 보고한다.
- 크래시 중단은 [크래시 정본](../../../../00_Document/operations/ORCA.md#crash-recovery)을 따른다. 확정 실패 집계에서 제외하고 죽은 세션은 새 세션으로 대체한다. 중간 산출물은 크래시로 중단된 부분 결과로 보존하며 통과 판정에 재사용하지 않는다. 쓰기 종료가 확인된 산출물의 리드 로컬 checkpoint 허용과 기존 Git·병합 권한을 구분한다.
- 결함 수정과 재검증은 새 세션으로 발행한다. 완료 세션을 `worker-start --terminal`로 다음 작업에 재사용하지 않는다. 정산과 terminal close 결과를 goal 근거에 남기고 전체 Delivery를 처리한 뒤 acknowledge한다.
- 리드의 목표 종료와 재진입은 [R-8](../../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)을 따른다.
- 전체 goal의 제품 PR·로컬 결과 뒤 Gardener·종료 기록 PR·사용자 종료 점검·R-8 순서는 [종료 정본](../../../../00_Document/operations/ORCA.md#r8-astra-lifecycle)을 따른다. 첫 PR마다 새 Gardener를 자동 추가하거나 다음 goal을 자동 발행하지 않는다.
- PR 생성은 허용하지만 각 PR 병합 직전에는 사용자 명시 승인이 필요하다. 메인 판단·이전 포괄 승인·CI 통과로 대신하지 않으며 자동 병합은 금지한다.

`98_Shared/Protocol`과 생성 파일의 동시 수정, 공유 WSL clone 동기화, 7777 포트 경쟁, 공용 DB 쓰기, Unity Library 공유를 피한다. 구체적인 실행 방식은 [DEVELOPMENT.md](../../../../00_Document/operations/DEVELOPMENT.md)에서 확인한다.

이것은 운영 규약이며 기술적 강제·맥락의 완전한 격리·무인 복구를 보장하지 않는다. 내부 서브에이전트 실행을 외부 작업자나 Orca runtime 검증의 근거로 삼지 않는다.
