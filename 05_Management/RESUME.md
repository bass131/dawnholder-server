# Management 다음 세션 진입

정본 worktree는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active`, 소유 영역은 `05_Management`다. 과거 root05나 management-foundation을 정본으로 사용하지 않는다. 이 문서는 읽기 순서와 다음 행동만 제공하며 상태를 별도 집계하지 않는다.

새 세션의 배치·진입은 루트 [RESUME의 세션 진입 배치](../00_Document/operations/RESUME.md#세션-진입-배치)와 [R-1](../00_Document/operations/ORCA.md#r1-management-placement), 작업자 기동과 첫 화면 확인은 [R-5](../00_Document/operations/ORCA.md#r5-worker-launch)·[R-6](../00_Document/operations/ORCA.md#r6-first-screen)을 따른다. Orca pane 소속과 실제 작업 경로·runtime·모델 표시·준비 상태를 함께 확인한다. 실제 파일·Git 작업은 위 `management-active`에서만 수행하며 `C:/Dev/DawnHolder_Project`를 사용하지 않는다. 과거 배치 관찰이나 handle을 현재 세션의 실행 권한으로 재사용하지 않는다.

목표 종료 후 Management 리드 교체는 메인이 [R-8](../00_Document/operations/ORCA.md#r8-astra-lifecycle)에 따라 수행하며, 새 리드는 루트 RESUME의 진입 절차를 따른다. 문서 변경은 실제 세션 교체 완료를 뜻하지 않는다.

## 최소 읽기 순서

1. 루트 [AGENTS](../AGENTS.md)와 [CURRENT](../00_Document/operations/CURRENT.md)의 Management 행이 가리키는 현재 goal. 현재 goal의 「진척 단계」·「재개 지점」·적용 중인 결정을 먼저 읽는다. 이 문서는 현재 goal 이름을 따로 적지 않는다.
2. 끝난 goal은 필요한 근거만 읽는다: [개발 기록 탐색 goal](goals/2026-10-05-development-record-navigation/goal.md)(PR189·PR194), [시스템 카드와 구현 설명 goal](goals/2026-10-02-system-cards/goal.md)(PR179·PR186), [운영 규칙 적용 goal](goals/2026-10-01-routing-adoption/goal.md)(PR156), [공동 조회 goal](goals/2026-10-01-shared-read-mcp/goal.md)(PR159·PR161). 후속 로그 계약은 [공동 조회·로그 합의](goals/2026-09-30-system-records/shared-read-agreements.md), PR147 병합과 당시 검증은 [기존 goal](goals/2026-09-30-system-records/goal.md#현재-상태)을 따른다.
3. 세션 배치는 루트 [RESUME의 세션 진입 배치](../00_Document/operations/RESUME.md#세션-진입-배치)를 따른다. 실제 다단계 작업을 재개할 때 [목표 루프](../.agents/skills/dawnholder-goal-loop/SKILL.md)와 [실행 안내](../00_Document/operations/DEVELOPMENT.md)의 영향 범위를 읽는다. 과거 전체 소스·로그·대화를 모으지 않는다.

## 재개 첫 행동

이번 세션에 허용된 범위와 지시 출처를 먼저 확인한다. 이어 `git status --short --branch`와 `git log -1 --format="%H %s" -- 05_Management`로 실제 로컬 변경·문서 커밋을 확인하고 위 goal의 관찰과 비교한다. 맥락 갱신만으로 후속 보류가 해제되지는 않는다.

과거 문서 checkpoint `8c6fbd57` 이식과 문맥 정정은 PR151 병합으로 끝났다. [당시 종료 기록](goals/2026-09-30-system-records/goal.md#오늘-작업-종료다음-세션-재개)의 이식 절차를 다시 실행하지 않는다. 현재 재개 범위는 CURRENT가 가리키는 goal을 따른다. 실제 로컬 변경과 해당 goal의 최신 결정·활성 작업 계약을 대조해 이어가며, 자동 압축 뒤에도 이 확인을 반복한다. 목표가 없을 때는 다음 goal을 자동으로 시작하지 않는다. MCP 정본은 management-active 작업 트리다.

## 소유권과 다음 결정

Game Dev의 root CURRENT/goal·게임 코드·실행 wrapper는 해당 메인 소유다. Management는 05의 기록 조회와 후속 서버 등록·조회·보존을 담당한다. 첫 공동 조회 이후에는 Game Dev와 로그의 실행 식별·시각/수준·cursor·보존 계약을 맞춘다. [합의 문서의 다음 결정](goals/2026-09-30-system-records/shared-read-agreements.md#다음-결정)을 기준으로 이미 정한 질문을 반복하지 않는다.

Management에도 이번 세션부터 [계층형 모델 라우팅](../AGENTS.md#모델-라우팅)을 적용한다. 공동 조회 합의와 기존 goal의 역할·모델·CLI 지정은 당시 기준이며, 현재 배정은 루트 AGENTS를 따른다. 적용 결정의 전달 경위와 PR154 병합 근거는 [운영 규칙 적용 goal](goals/2026-10-01-routing-adoption/goal.md)에 둔다. 규칙 문서 변경과 실제 세션의 모델 관측을 구분한다.

Management 리드의 발신 태그는 `[Management 리드 Opus]`다. 전환은 [리드 태그 전환](../AGENTS.md#lead-tag-transition)을 따른다. 수신 출처 대조와 터미널 알림·실제 지시 구분은 [메시지와 보고 규칙](../AGENTS.md#메시지와-보고)을 따른다. 새 세션에서는 루트 RESUME의 진입 배치에 따라 메인 handle과 실제 경로·모델 표시·준비 상태를 확인한다.

작업자·검증자는 담당 리드 아래 pane에서 작업 하나를 마친 뒤 정산·종료하고 재사용하지 않는다. 준비·최초 작업 연결·거부 시 처리·정산 절차는 [Orca 위임 지침](../.agents/skills/dawnholder-goal-loop/references/orca-work.md)을 따른다. 이 종료 규칙을 메인·리드·다른 사용자 세션의 종료 권한으로 확대하지 않는다. `--no-daemon`은 더 이상 필수가 아니며, 파일 소유권과 PR별 명시 병합 승인은 [AGENTS](../AGENTS.md)를 따른다.
