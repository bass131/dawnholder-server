# 계층형 모델 라우팅 시범과 코드 기준 재정립 (S0 후속)

상태: **B 시범 준비 진행 — 계획 커밋의 메인 검토 대기**. A/B는 사용자 합의, C는 미확정이다. PR152 인계 문서화와 승인된 브랜치 정리는 완료됐다. 아래 B의 착수 계획을 반영하며, Sol 발행·제품 구현·Opus 검증·테스트는 메인의 계획 승인 후 진행한다.

## PR152 문서화 기록 — 완료

아래 문서화 범위와 검토 결과는 PR152 당시 기록이며 B 시범의 실행 결과가 아니다.

- branch `bass131/hierarchical-routing-handoff`, base `36fb5ec751f4c482a993e77c9547968e8f828e34`(당시 최신 origin/main). PR149·150·151 병합 이후 기준이다.
- 결정 원문: `msg_8ca511af0885`, PR150 종료 회신 `msg_c563cebaea65`. 로컬 보존: `.backups/handoffs/2026-10-01-routing-documentation-request.json`.
- 이번 문서 작성·독립 검토는 기존 AGENTS의 Astra 라우팅을 따른다. 지정 `gpt-6-astra`, 실제 runtime `unknown`. 별도 Astra의 문서 정적 검토는 PASS이며 시범 완료 판정은 아니다.
- AGENTS·.agents/는 아래 CLI 규칙 삭제만 예외로 허용하며 라우팅 규칙은 이번에 개정하지 않는다. CODE_CONVENTION·CLAUDE.md·제품 코드·설정은 수정하지 않는다. PR 발행 후 Claude 메인에 링크·head·CI 결과를 전달하며, 메인의 검증·승인 전 사용자에게 병합 승인을 요청하지 않는다.
- 2026-10-01 사용자 지시로 `--no-daemon` 규칙 폐지(`msg_8a09e2e92e17`, 로컬 `.backups/handoffs/2026-10-01-no-daemon-rule-removal-request.json`). 시범 성공 이후가 아닌 이번 문서 PR의 예외이며, 과거 완료 goal의 실행 기록은 보존한다. 관련 이슈 해결이나 새 CLI 실행 검증을 주장하지 않는다.
- 추가 3([`msg_b1fb9688460f` 원문](../../../.backups/handoffs/2026-10-01-context-refresh-branch-cleanup-request.json)): 이번 PR에 현행 GameDev 문서·ADR-029의 날짜별 사실 정정을 포함했다. PR152 병합 후 GameDev Astra는 [최신 정정 `msg_dc989cc54bad`](../../../.backups/handoffs/2026-10-01-branch-cleanup-correction.json)에 따라 원격 삭제 명령 없이 `fetch --prune`과 승인된 로컬 9개 `branch -d`를 완료했다(모두 exit 0, 거부 없음). stash 전체·로컬 PR152 branch·main·archive는 보존했으며 Management 정리 대상은 해당 담당자 소유다. 결과는 로컬 `.backups/verification/2026-10-01-hierarchical-routing-handoff/cleanup-after.json`과 `prune-result.json`에 있다.
- PR152는 2026-10-01 07:03:49 UTC에 `f32dbbe9a4aec7cafd64fc7f1897a1f5e47370f6`으로 병합됐다. 같은 근거 폴더의 `final-status.json`에서 상단 `pr.state: OPEN`은 병합 전 스냅샷이며 후행 `mergeConfirmation.state: MERGED`와 `branchCleanup`이 후속 결과다. 원시 기록을 덮어쓰지 않고 시점을 구분한다. 이번 재개 시 원격 main `dd5c253763c55a151a78678869a43792d7b7c34f`의 이력에서도 PR152·153 병합을 확인했다.

## A. 확정 운영 합의 — 시범 적용 전 기록

- 구조: 메인(Claude Code, Opus 5.5) → 파트 리드(GPT-6 Astra: GameDev·Management) → 구현(GPT-6.1 Sol). 검증자는 신규 Opus 5.5 세션. 모델 대체 금지는 유지한다.
- 메인: 방향 설정·파트 분할·사용자 조율·결과 통합·사용자용 보고서·PR 병합 승인 요청. 저장소 파일은 쓰지 않으며 CLAUDE.md만 예외다. Astra는 파트 결과를 goal에 기록한다.
- 검증자는 각 Astra가 자기 파트용으로 직접 열고 판정 뒤 닫는다. 파트당 동시에 하나만 연다. Orca의 새 Claude Code 세션 방식이 유력하나 미검증이다. 열 수 없으면 메인에 요청해 대신 열며 판정은 해당 Astra가 받는다.
- 검증 순서: 보고한 작업의 실제 수행·문제·미완료를 완료로 보고했는지를 먼저 실사하고, 그 결과로 테스트 코드를 작성·실행한다. 문서 변경은 실사만, 코드 변경은 실사와 테스트 모두 수행한다.
- 검증자는 Sol 쓰기 종료 후 같은 브랜치의 테스트 파일만 쓴다. 제품 코드는 고치지 않고 결함을 보고한다. Unity 플레이 등 자동 실행할 수 없는 범위는 판정에 `미실행`으로 구분한다.
- 재검증마다 새 검증자를 열어 이전 번호별 결함 목록을 전달한다. 같은 번호의 결함이 3번 재검증에 실패하면 메인에 보고한다.
- 거짓 보고는 보고와 실제 커밋·diff·실행 기록이 다른 경우이며 실수/의도를 구분하지 않는다. 미실행 테스트를 통과로 적는 경우도 포함한다. 발견 즉시 수정과 별개로 메인에 보고한다.
- Astra 보고: 변경 요약·검증 근거 위치·리스크·결정 요청·판정 원문 경로. 원문은 로컬 `.backups/verification/`에 보존하고 메인은 승인 전 항상 읽는다.
- 커밋·push는 브랜치의 Astra 한 명이 맡는다. Sol·검증자는 파일만 쓴다. 원격 변경은 아래 사용자 판단 경계를 따른다.
- 파트 간 기술 계약은 두 Astra가 직접 조율한다. 사용자 판단 영역은 메인에 보고하고 메인이 사용자와 정한 결과를 작업 지시로 내린다. 나머지 기술 선택은 Astra가 결정하고 결과만 보고한다.
- 반드시 메인에 보고: 플레이어가 보거나 느끼는 게임 정책·UX·밸런스 변화, 범위 확대·새 목표·새 파트, PR 병합·원격 저장소/외부 서비스 변경·설치·전역 설정, 데이터 삭제 등 되돌리기 어려운 작업, 파트 간 미합의 계약.
- CURRENT·RESUME·goal은 Astra가 쓴다. 메인은 합의의 정확한 반영·미합의 내용의 부재·분량·링크를 검증·승인한 뒤 다음 작업을 허가한다. 이 승인은 진행 허가이며 **PR 병합은 매 PR 사용자 명시 승인**이 필요하다.
- 사용자는 Astra에 직접 지시할 수 있고 Astra는 메인에 공유한다. 충돌하면 사용자 지시를 따른다.
- 메인이 Orca로 Astra 터미널에 넣는 입력은 항상 `[메인 Claude]`로 시작하고 “Orca 메시지를 확인하라”는 안내만 담는다. 지시 내용은 orchestration 메시지로만 보낸다(합의 전달 출처 `msg_4483ae6889ac`).
- 표식 없는 터미널 입력만 사용자 직접 지시로 취급한다. `[메인 Claude]` 입력과 orchestration 메시지는 메인 지시이며, 메인이 사용자 결정을 전달해도 사용자 직접 지시의 우선 규칙을 적용하지 않는다.
- 메인 부재 시 답이 필요한 항목만 멈추고 독립 작업은 계속한다. 메인은 세션 시작 시 두 Astra에 자기 terminal handle을 알린다.
- WSL·7777·DB 실행 자원은 [DEVELOPMENT](../../../00_Document/operations/DEVELOPMENT.md)의 소유 규칙을 따른다. 검증자가 여럿이면 Astra가 순서를 정한다.

## B. GameDev 시범 — 착수 계획

- **이 goal에 한해 새 라우팅을 적용한다, 사용자 승인.** 전역 라우팅 규칙은 시범 성공 후 변경한다. 완료된 PR152 문서화에는 기존 라우팅을 적용했으며 위 CLI 규칙 삭제만 별도 승인된 예외였다.
- GameDev 대상: `?? AddComponent` 4곳 — `ProjectileSpawner.cs:29`, `ProjectileLaunchHandler.cs:94`, `EnemyAttackHandler.cs:102`, `RemoteEntityRegistry.cs:260`. 정확한 경로는 [후속 후보](../2026-10-01-refactor-record-corrections/open-items.md#2026-10-01-추가--36fb5ec7-기준)에 있다. Editor fake-null에서 AddComponent가 호출되지 않을 수 있다는 후보이며 시범 구현·재현은 미실행이다.
- 성공 조건: ① Astra가 Opus 검증자를 열 수 있음 ② 실사가 끝까지 진행됨 ③ 판정 원문이 메인까지 도달함 ④ 사용자가 받은 보고가 이해하기 쉬움.
- 성공 후 순서: GameDev Astra가 AGENTS·.agents/ 수정 → 메인 검증, 메인이 CLAUDE.md 수정 → 모두 PR·사용자 병합 승인 → 그다음 Management 적용.
- 실패하면 기존 라우팅을 유지하고 다시 논의한다.

### 착수 기준과 승인 순서

- 메인 착수 지시: `msg_90fa4b6d4ed9`(2026-10-01), 초안 회신 `msg_dc98dd494aca`에 보완 6건을 적용한다. 작업 경로는 `C:/Dev/DawnHolder_Project`, branch는 `bass131/unity-component-null-pilot`이다. `git fetch origin` 후 확인한 base는 `dd5c253763c55a151a78678869a43792d7b7c34f`다.
- **현재 단계 (a): branch 생성·이 goal 갱신을 로컬 커밋하고 메인에게 전달한다. 메인의 해당 계획 승인 전 Sol을 발행하지 않으며 push도 하지 않는다.** local main은 Management worktree에 있으므로 main 전환 없이 origin/main에서 새 branch를 만들었다.
- 단계 (b): 구현 후 독립 판정 원문 경로·변경 요약·리스크·결정 요청을 메인에게 전달한다. PASS이면 push·PR 생성까지 허용되며 PR 링크·head·CI를 보고한다. 병합은 해당 PR의 사용자 명시 승인 전까지 하지 않는다.
- 단계 (c): 막힘·지정 모델 불가·보고와 실제 수행 불일치·사용자 판단 영역은 즉시 메인에 보고한다. C 논의, 다른 후보, 전역 규칙·설정 변경을 구현 범위에 추가하지 않는다.

### 설계와 파일 소유

기본 설계는 아래 4개 파일 안에서 `TryGetComponent`로 기존 컴포넌트를 재사용하고 없을 때만 추가하는 최소 수정이다. 컴포넌트 초기화·Launch/Flash 호출·원격 등록과 driver 준비 순서, 메인 스레드 적용, prefab·scene·직렬화 값·기존 `.meta`/GUID를 보존한다. 공용 helper가 더 적절하면 Astra가 이유와 새 `.cs`/`.meta` 소유를 이 goal에 먼저 기록한 뒤 Sol에 쓰기를 허용한다. 현재 helper 추가는 선택하지 않았다.

| 소유자 | 쓰기 범위 | 경계 |
|---|---|---|
| Sol 구현자 | `03_Client/Assets/Scripts/Combat/Effects/ProjectileSpawner.cs`, `Network/Handlers/Skill/ProjectileLaunchHandler.cs`, `Network/Handlers/Combat/EnemyAttackHandler.cs`, `State/RemoteEntityRegistry.cs`(뒤 3개도 같은 Scripts 기준) | 제품 코드만 수정. 테스트·문서·commit/push·추가 위임 금지 |
| 신규 Opus 검증자 | `03_Client/Assets/Tests/EditMode/`의 시범용 특성화·회귀 테스트 `.cs`와 필요한 신규 `.meta`; 관련 기존 `RemoteEntityRegistryTests.cs`, `RemoteEntityAdapterContractTests.cs` 보완 | Sol 쓰기 종료 후 소유권 이전. 제품 코드는 수정하지 않고 결함을 번호로 보고. 기존 GUID 보존 |
| GameDev Astra | 이 `goal.md`, 로컬 `.backups/verification/2026-10-01-unity-component-null-pilot/`의 실행·판정 근거, branch/commit/push/PR | 구현·검증 원문은 각각 별도 하위 경로에 보존. Git 쓰기는 Astra 한 명만 수행 |

동시 쓰기를 금지한다. 결함 수정은 검증자 쓰기 종료 후 Sol에 돌리고 재검증마다 신규 Opus 세션을 연다. 같은 번호의 결함이 3번 재검증에 실패하면 메인에 보고한다. 기존 stash·handoff branch·main·archive와 manifest의 로컬 변경을 보존한다.

### 실행 세션과 모델

- Sol: 같은 checkout을 정확히 지정한 Orca 감독형 `worker-start --agent codex --model gpt-6.1-sol`로 발행한다. 새로운 Run/Task/Dispatch를 사용하며 일반 작업자의 추가 위임은 금지한다. 모델 미노출 시 버전·노출을 확인하고 대체·업데이트 없이 메인에 보고한다.
- 검증자: Sol 쓰기 종료 후 Astra가 같은 branch에 새 Orca Claude 세션을 `worker-start --agent claude --model claude-opus-5-5`로 연다. 이 ID는 메인이 확인해 전달했다. 파트당 동시에 하나만 열고 판정 원문 전달·정상 정산 후 release한다. 기동 실패나 모델 확인 불가는 대체하지 않고 메인에 보고한다.
- 요청 모델·Orca `launch.requested/effective`·세션 화면의 실제 표시 모델을 별도 기록한다. 백엔드 실제 모델을 정확히 확인하지 못하면 `unknown`으로 둔다. 현재 Astra 화면은 `GPT-6-Astra xhigh`, 백엔드 모델은 `unknown`이며 Sol/Opus 세션은 아직 없다.
- 신규 Claude가 Bash·파일 쓰기 권한 확인에서 멈추면 권한 우회 플래그·설정 변경을 하지 않는다. 멈춘 지점과 화면 상태를 메인에게 전달하고 성공 조건 ① 기동·② 실사 진행의 관찰 기록에 남긴다. 직접 열 수 없으면 메인에게 대신 기동을 요청하며 판정은 Astra가 받는다.

### 독립 검증과 Unity 실행 조건

1. 검증자는 보고·실제 diff·원시 실행 기록부터 실사한다. 이어 별도 EditMode 특성화 테스트로 컴포넌트 없는 GameObject의 `GetComponent<T>()` 결과에 대해 `ReferenceEquals(x, null)`과 Unity null 비교를 기록한다. 전자가 false인 fake-null 객체가 실제 반환되는지와 기존 경로의 컴포넌트 추가 누락을 구분해 확인한다. 재현되면 결함 확인으로, 재현되지 않으면 동작 보존 검증을 거친 무해한 정리로 분류하며 결함 주장은 미입증으로 보고한다.
2. 4개 실제 진입 경로의 누락 컴포넌트 추가·초기화, 기존 컴포넌트 재사용·중복 방지, 발사·피격·원격 등록의 후속 동작과 순서 보존을 검증자가 독립 테스트로 작성·실행한다. 텍스트 검색이나 구현과 같은 분기 복제로 행동 검증을 대신하지 않는다.
3. Unity 실행은 [M0 선례](../2026-09-29-refactor-baseline/goal.md#실행-상세와-한계)의 `-batchmode -nographics -runTests -testPlatform EditMode`를 따른다. 프로젝트 요구 버전/revision `6000.4.7f1 (f3c3c4248748)`과 설치를 확인한다. **실행 전에** `03_Client`를 연 에디터·프로젝트 잠금과 라이선스 상태를 확인한다. 에디터가 열려 있으면 닫지 않고 메인에게 보고한다. 라이선스 문제는 기록·보고하며 과거 Hub 재로그인을 현재 유효성 근거로 쓰지 않는다.
4. Unity 실행 직전·직후 tracked 파일 목록/상태와 내용 hash, `Assets/Plugins` DLL, `ProjectSettings`, manifest의 skip-worktree flag·SHA256을 비교한다. 허용된 제품/테스트 수정과 에디터 자동 변경을 구분한다. 예기치 않은 변경을 임의 복구하지 않고 근거와 함께 보고한다. Windows 솔루션 빌드의 DLL 복사, 서버 7777·DB 실행을 이 검증에 추가하지 않는다.
5. 실행 명령·환경·exit·XML·로그·판정 원문을 로컬 근거 폴더에 보존한다. 테스트 작성·컴파일·EditMode 실행·수동 Unity 플레이를 각각 판정하며 실행 불가와 통과를 구분한다. 수동 플레이, 시각/음향 체감, PlayMode, 실제 서버 연결·DB는 별도 수행 전까지 **미실행**이다. 과거 CI/테스트 성공은 이 변경의 결과로 재사용하지 않는다.

### 현재 관찰과 미실행

- 준비 전 HEAD는 `8f777aba281022e391e3cc4da11226259df57435`, tracked status는 clean이었다. branch 전환 전 보호 대상 Plugins·ProjectSettings·manifest 34파일 hash와 보호 ref·stash를 로컬 `stage-a-before.json`에 기록했다. manifest는 `S`, SHA256 `3E194274509B32D18F4BE03F2D9462B5CDBB721C14EEE0ED0B6A17B1360AD781`이며 clean 표기가 로컬 패키지 변경 부재를 뜻하지 않는다.
- branch 생성 후 읽기 전용 프로세스 조회에서 `Unity.exe`는 없었다. 프로젝트 잠금·라이선스 확인과 실행 직전 프로세스 재확인은 아직 남아 있다.
- Sol/Opus 기동, 제품 수정, fake-null 특성화, 회귀 테스트, 빌드·플레이는 **미실행**이다. 시범 성공 조건 ①–④도 아직 충족으로 판정하지 않는다. 현재 다음 행동은 단계 (a)의 계획 커밋에 대한 메인 검토다.

## C. 미확정 — 다음 세션 논의

S0·CODE_CONVENTION 후속 초안이며 현재 규칙으로 확정하지 않는다.

1. 상태 소유자는 하나다.
2. 변경은 시나리오의 전/후로 설명한다.
3. 클래스가 바뀌는 이유는 하나다.
4. 이름은 무엇을, 주석은 왜를 말한다.
5. 문서는 코드가 말하지 못하는 것만 쓰고, 코드와 어긋나면 결함이다.
6. 저장소에는 재사용할 것만 둔다.
7. 절차는 작업 크기에 비례한다.

적용 방식 제안도 미확정이다: CODE_CONVENTION 압축·AGENTS 보고 규칙 조정.

## D. 후속과 검토 인계

후속 후보는 [기존 목록의 날짜별 추가 구획](../2026-10-01-refactor-record-corrections/open-items.md#2026-10-01-추가--36fb5ec7-기준)에 둔다. 메뉴 연결·RegisterSend·HUD 2개·UnityClientSession 분리 후보를 유지하며 후보 등록을 구현 승인으로 확대하지 않는다.

문서 독립 검토 원문·입력 확인은 `.backups/verification/2026-10-01-hierarchical-routing-handoff/verdict.md`와 `checks.json`에 있으며, 최종 PR/head/CI는 같은 경로의 `final-status.json`과 Claude 회신으로 인계한다. 시범 실사·테스트·판정 전달·보고 성공 조건은 모두 미실행이다.
