# 계층형 모델 라우팅 시범과 코드 기준 재정립 (S0 후속)

상태: **일부 합의·착수 전**. A/B는 사용자 합의, C는 미확정 논의 안건이다. 이번 작업은 다음 세션을 위한 문서화이며 시범 구현·Opus 검증자 실행은 미착수다.

## 문서화 기준과 범위

- branch `bass131/hierarchical-routing-handoff`, base `36fb5ec751f4c482a993e77c9547968e8f828e34`(최신 origin/main). PR149·150·151 병합 이후 기준이다.
- 결정 원문: `msg_8ca511af0885`, PR150 종료 회신 `msg_c563cebaea65`. 로컬 보존: `.backups/handoffs/2026-10-01-routing-documentation-request.json`.
- 이번 문서 작성·독립 검토는 기존 AGENTS의 Astra 라우팅을 따른다. 지정 `gpt-6-astra`, 실제 runtime `unknown`. 별도 Astra의 문서 정적 검토는 PASS이며 시범 완료 판정은 아니다.
- AGENTS·.agents/는 아래 CLI 규칙 삭제만 예외로 허용하며 라우팅 규칙은 이번에 개정하지 않는다. CODE_CONVENTION·CLAUDE.md·제품 코드·설정은 수정하지 않는다. PR 발행 후 Claude 메인에 링크·head·CI 결과를 전달하며, 메인의 검증·승인 전 사용자에게 병합 승인을 요청하지 않는다.
- 2026-10-01 사용자 지시로 `--no-daemon` 규칙 폐지(`msg_8a09e2e92e17`, 로컬 `.backups/handoffs/2026-10-01-no-daemon-rule-removal-request.json`). 시범 성공 이후가 아닌 이번 문서 PR의 예외이며, 과거 완료 goal의 실행 기록은 보존한다. 관련 이슈 해결이나 새 CLI 실행 검증을 주장하지 않는다.

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

## B. 다음 세션 시범

- **이 goal에 한해 새 라우팅을 적용한다, 사용자 승인.** 전역 라우팅 규칙은 시범 성공 후 변경한다. 이번 문서화에는 기존 라우팅을 적용하며 위 CLI 규칙 삭제만 별도 승인된 예외다.
- GameDev 대상: `?? AddComponent` 4곳 — `ProjectileSpawner.cs:29`, `ProjectileLaunchHandler.cs:94`, `EnemyAttackHandler.cs:102`, `RemoteEntityRegistry.cs:260`. 정확한 경로는 [후속 후보](../2026-10-01-refactor-record-corrections/open-items.md#2026-10-01-추가--36fb5ec7-기준)에 있다. Editor fake-null에서 AddComponent가 호출되지 않을 수 있다는 후보이며 시범 구현·재현은 미실행이다.
- 성공 조건: ① Astra가 Opus 검증자를 열 수 있음 ② 실사가 끝까지 진행됨 ③ 판정 원문이 메인까지 도달함 ④ 사용자가 받은 보고가 이해하기 쉬움.
- 성공 후 순서: GameDev Astra가 AGENTS·.agents/ 수정 → 메인 검증, 메인이 CLAUDE.md 수정 → 모두 PR·사용자 병합 승인 → 그다음 Management 적용.
- 실패하면 기존 라우팅을 유지하고 다시 논의한다.

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
