# Codex 프로젝트 초기 정비

2026-09-29 검토 시점 상태: 초기 셋업 구현·검증 및 보고 완료 / PR 통합 확인 대기 / Orca 감독 연동 보류. 아래는 검토 시점 기록이며 이후 병합 상태는 PR의 최신 정보를 확인한다.

## 목표와 범위

- 기존 게임 코드와 사용자 변경을 보존하고, 프로젝트 내부의 Codex 운영 기준·목표 루프·개발 안내를 정비한다.
- 오래된 Claude 운영 자산은 고정 보관 브랜치로 보존하고 현행 안내와 분리한다.
- 실행 스크립트의 경로 문제와 환경 안내를 점검하고 실제 확인한 범위만 기록한다.
- 이번 요청에 따른 HTML 결과 보고를 제공한다. HTML을 이후 모든 목표의 필수 산출물로 삼지 않는다.
- 게임 기능 변경, 사용자 전역 스킬 설치, 별도 통제 프로그램 구현은 범위 밖이다.

## 완료조건

- [x] 루트 지침, 프로젝트 목표 스킬, 개발 안내와 문서 진입점이 현재 합의에 맞고 활성 링크가 유효하다.
- [x] Claude 운영 자산의 보관 및 현행 파일 정리가 확인된다.
- [x] 실행 스크립트 변경과 검증 결과, 미검증 항목이 구분되어 있다.
- [x] 독립 검증 결과의 필요한 수정을 반영한다.
- [x] HTML 결과 보고와 남은 문제를 사용자에게 전달한다.
- [x] 승인 범위의 PR·병합 상태를 기록한다. 기록 시점은 draft PR·통합 확인 대기이며, 이 체크는 병합 완료를 뜻하지 않는다.

## 주요 결정

- 메인은 사용자 대화·목표·범위·완료조건·주요 결정·위임·통합·종합 보고를 담당한다.
- 구현·검증·테스트는 작업자를 최대한 활용하고, 구현자와 독립 검증자는 분리한다.
- 작업자에게 최소 맥락·파일 소유권·완료조건·검증 범위·권한을 전달하며 동일 파일 동시 쓰기를 금지한다.
- 메인이 명시 지정한 Orca 목표 담당자는 허용된 단일 목표·작업 공간·권한 안에서 좁은 작업자를 한 단계 분할할 수 있다. 일반 작업자는 추가 위임하지 않는다.
- 메인이 통합·발행을 결정하며 브랜치 전환·커밋·푸시의 기계 작업은 명시한 범위에서 위임할 수 있다. 임의 Git 변경 권한을 주는 규칙은 아니다.
- 원문 로그는 TEMP 또는 목표별 산출물 경로에 두고 메인은 필요한 부분만 읽는다.
- 목표 상태는 이 파일 한 곳, CURRENT는 링크만 유지한다.
- 앞으로 각 목표는 최신 `main` → 개별 작업 브랜치 → 구현·독립 검증 → PR → `main` 병합으로 진행한다.
- `chore/codex-project-setup`은 이번 정비 후 종료한다. 보관 브랜치는 고정한다.
- 프로젝트 스킬은 `.agents/skills/`에만 둔다. 학습 장부·고정 모델 배정·상시 HTML 보고는 도입하지 않는다.

## 확인된 안전포인트

- 2026-09-29 정비 시작 전, `archive/claude-setup-2026-09-29`와 `chore/codex-project-setup`의 로컬·원격 기준 커밋은 `f0f23f781dc49d8bf67c0019b948696c36ed6c00`이다.
- 당시 `main` 로컬·원격은 `4cac353db8d1672d2dcb1b0b7f46cb12e0ca5db5`로 유지되었다.
- 기존 변경 4건의 상태를 보존했다. 보관 커밋의 실제 diff는 CLAUDE 문서 2개와 generator 1개이며, Minimap의 정규화 Git blob은 기존 기준과 동일하다. Minimap 원본 바이트는 로컬 사본과 현재 파일에 보존했고 기존 제안 HTML도 미추적 상태로 보존했다.
- 로컬 안전 사본의 5개 파일 SHA-256이 사전 manifest와 일치함을 별도 확인했다. 작업 트리 Minimap 원본 바이트도 일치했다.
- 로컬 근거: `.backups/claude-pre-codex-20260929-184221-164/manifest-before.json`, `verification-final.json`, `files/`. 이 경로는 로컬 보관물이며 Git 배포 산출물이 아니다.

## 결과와 남은 검증

- 작성 완료: AGENTS, 목표 루프 스킬, README·문서 지도, DEVELOPMENT, CURRENT. 공식 스킬 형식 검사 통과, Orca 문서 포함 9개 문서의 로컬 링크 누락 없음.
- WSL Ubuntu SDK 10.0.300으로 빌드 통과: 오류 0개, 경고 7개(SA1201 1개·SA1202 4개·xUnit2031 2개). 근거: `.backups/codex-runtime-20260929/build.log`.
- 최초 DashSmoke 통과: PASS 1 / FAIL 0. 근거: `.backups/codex-runtime-20260929/dash-smoke.log`.
- 독립 .NET 테스트: Passed 708 / Failed 0 / Skipped 5 / Total 713, 1분 52초. 근거: `.backups/codex-runtime-20260929/independent-test.log`. 생략된 5건은 테스트 소스의 Skip 지정과 대조했다.
- 독립 실행 시나리오 DashSmoke·MapTransition·PartyQuest: PASS 3 / FAIL 0. 근거: `.backups/codex-runtime-20260929/independent-bots.log`. PartyQuest는 파티 결성·맵 이동·해산을 확인했으며 공동 처치 카운트까지 검증한 것은 아니다.
- 맵 바이너리 6개의 원본·배포 SHA-256 일치, 동일 공간 동시 실행 거부, 위험한 홈 경로 지정 거부, 종료 후 7777 listener 정리를 확인했다. 근거: 같은 폴더의 `independent-map-deployment.txt`, `independent-lock.log`, `independent-path-guard.log`, `independent-runtime-summary.json`.
- 실행 helper의 경로·소유 표시·잠금·프로세스 정리 코드를 독립 검토했다. 읽기 전용 `path` 검사로 루트·사용자 홈·원본·원본 상위 경로의 override 거부와 공백 포함 전용 경로 처리를 확인했다. Git 훅 LF 규칙도 확인했다.
- Orca 연결과 실패 자원 정리는 확인했으나 worker 준비 단계 시간 초과로 실제 작업 수행·완료 수신은 미통과다. 상세 근거는 [ORCA](../../../00_Document/operations/ORCA.md)에 있다.
- 새 세션의 스킬 자동 선택, Unity 에디터 컴파일·플레이, SQL 연결·영속화는 미검증이다. 서버·봇 실행 성공과 구분한다.

## 보고와 통합

- [초기 정비 HTML 결과 보고](../../../00_Document/reports/2026-09-29-codex-setup-report.html)는 생성 시점의 검토 자료다. 성공한 검사와 Orca 외부 문제·미검증 범위를 함께 표시한다.
- HTML 브라우저 QA 43/43 및 내용 독립 검토 통과. 폭 1440·768·390·320에서 가로 넘침·JS 오류 없음, 필터와 로컬 링크 확인. 로컬 근거: `%TEMP%/dawnholder-setup-report-qa-20260929/qa-report.json`.
- [PR #125](https://github.com/bass131/dawnholder-server/pull/125)는 검토 시점에 draft/open, 대상 `main`이며 아직 병합하지 않았다. 최초 정비 커밋은 `4e1c6868b572ca16c236210a5909bdcfccf741d9`; 최종 보고·목표 기록은 후속 변경이다.
- 메인이 HTML 화면을 직접 확인하고 사용자에게 보고서 링크와 Orca의 남은 문제를 전달했다. PR 검토·병합은 아직 완료로 표시하지 않는다. 이후 원격 상태는 위 PR에서 확인하고 실제 병합·후속 검증을 확인한 경우에만 목표 종료를 기록한다.
