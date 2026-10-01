# Management 상태·근거 표기 정정

## 목표와 허용 범위

2026-10-01 사용자가 Claude 세션의 정정 요청 `msg_2d3481de71f2`를 읽고 요청 범위대로 진행하도록 지시했다. 감사 질의 `msg_67f177b3db6f`와 답변 `msg_4d569fa41aa3`를 바탕으로 기존 사실·합의·검증 한계를 문서에서 일관되게 찾도록 고친다. 이 목표가 이번 정정의 범위·상태·결과 원본이다.

정본은 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management`다. 변경은 `05_Management` 문서와 catalog 출처 메타데이터에 한정한다. 새 기능·공동조회 MCP·운영백엔드·CI 추가·앱/서버/SQL 실행·원시 근거 외부 공개는 포함하지 않는다. 공동조회 후속 구현 보류는 유지한다.

## 정정 항목과 완료조건

| 항목 | 완료조건 |
| --- | --- |
| MC1 | 로컬 checkpoint `8c6fbd57f2f54825a937a82a6ad949755b43411c`와 기존 브랜치를 보존하고 최신 main 기반 새 브랜치로 재개 문서를 이식해 PR을 생성한다. |
| MC2 | PR147의 실제 병합 상태와 Game Dev 최종 의미검토 PASS를 안내한다. 검증 문서의 당시 대기는 역사로 보존하고 후속 결과로 연결한다. |
| MC3 | requirements/decisions에서 이미 합의한 catalog 파일 방식·공동조회→로그 순서·로컬 stdio MCP·7일/1GB 보존을 찾을 수 있다. 갱신 정책·쓰기 권한 등 미정은 확정으로 바꾸지 않는다. |
| MC4 | 과거 goal의 정본 표기를 당시 경로로 구분하고 현재 README로 연결한다. 과거 실행 위치·실적을 소급 변경하지 않는다. |
| MC5 | dotnet-tests CI가 05 npm 테스트를 실행하지 않는다는 한계와 실제 launcher 더블클릭·detached 실행 경로 미검증을 goal·사용자 진입 안내·PR에 명시한다. |
| MC6 | 이미 추적되는 05 문서 출처를 새 catalog revision에서 고정 commit/path로 전환하고 별도 커밋한다. 역사 asOf·상태·기록과 원시 근거의 local-only 표시를 보존한다. 범위 초과 시 보류 이유를 명시한다. |

별도 Astra가 정확한 변경과 원본 근거를 독립 정적 검토한다. catalog 변경에는 기존 계약 적합성, 출처 commit/path 존재와 내용, 변경 전후 역사 상태·ID·참조 보존을 확인한다. 코드·테스트 구현을 바꾸지 않으므로 제품 실행 테스트를 정적 검토로 가장하지 않는다. PR 생성까지 진행하되 이 PR의 병합 직전 사용자 명시 승인이 필요하며 자동 병합은 금지한다.

## 기준과 소유권

- 기준 main: `ef5f1023fe3353ee9eec5da04422232a33855233`. 2026-10-01 `git fetch origin main`으로 확인했다.
- 새 브랜치: `docs/management-context-corrections`. 기존 `feat/management-system-records`는 `8c6fbd57f2f54825a937a82a6ad949755b43411c`로 보존한다.
- checkpoint 이식: `dd4e7ea` (`git cherry-pick 8c6fbd57...`), 충돌 없음. 이후 변경 전에 clean을 확인했다.
- catalog 출처 전환은 `ef6c50d0226247a6ac6d704e8ef2f1f560f7c782` 한 커밋에 `records/catalog.json`만 담아 문서 정정과 분리했다.
- 문서 정정·독립 검토 근거는 `aa17541`로 커밋했다. 이후 PR 생성 상태를 이 goal에 기록한다.
- 메인: 이번 goal·통합·PR·사용자/Claude 회신. 문서·기록 서술 작성자: 기존 05 문서와 catalog. 독립 검증자: 이 목표의 검토 기록만 소유하며 제품·정정 문서를 수정하지 않는다.
- 지정 모델: 메인·문서/기록 작성·독립 검토 `gpt-6-astra`. 실제 backend 모델은 확인 불가로 `unknown`이다. 이번에는 일반 구현과 새 Codex CLI 프로세스를 시작하지 않는다.

## 현재 상태

MC1–MC6의 정정·별도 커밋·독립 정적 검토와 [PR #151](https://github.com/bass131/dawnholder-server/pull/151) 생성을 완료했다. 검토에서 발견한 사실 표기 1건은 작성자 수정 후 재확인해 PASS를 받았다. PR151은 OPEN이며 사용자 개별 병합 승인 전이다. 자동 병합을 설정하지 않았고 main 병합은 수행하지 않았다. PR147의 기존 병합 완료와 이번 정정 PR의 통합 대기를 구분한다. 새 MCP·운영 기능 착수 보류는 유지한다.

2026-10-01 05:12 UTC의 독립 정적 검토는 기존 catalog 계약의 데이터 수용, 출처 6개의 Git blob과 이전 SHA256 일치, 시스템·기록·asOf·sourceCommit 및 나머지 29개 출처 보존, 상대 링크 181개를 확인했다. 당시 r2 catalog SHA256은 `4D81FAAAF3DD375D2EEBF0ACB95983CA0AA2311038782DBA6EE7D35EC9554488`이다. 이는 과거 검토 결과 요약이며 제품 코드·테스트 스위트 변경이나 앱 실행 검증이 아니다.

사용자는 후속 정정 요청 `msg_ace72b027aa0`를 읽고 해당 범위의 처리를 지시했다. 이에 두 검토 원본을 바이트 그대로 `.backups/verification/2026-10-01-management-context-corrections/static-review.mjs`와 `.backups/verification/2026-10-01-management-context-corrections/static-review-result.json`에 복사하고 원본·사본 SHA256 일치를 확인했다. 두 파일은 **공유되지 않는 로컬 근거**이며 GitHub나 다른 PC의 조회를 보장하지 않는 보조근거다. 검토 스크립트는 당시 경로를 전제로 하므로 보관 사본을 현재 실행 명령으로 안내하지 않는다.

이번 후속 변경은 두 파일을 저장소 추적에서 제외하고 관련 문서 참조를 정리한다. 원본 보존과 참조 정리는 과거 catalog 검토의 재실행이 아니다. 같은 PR 브랜치에서 반영하며 새 HEAD의 CI 결과는 [PR151 Checks](https://github.com/bass131/dawnholder-server/pull/151/checks)와 최종 회신에서 확인한다. 이전 HEAD의 CI 성공을 새 결과로 사용하지 않는다. main rebase/merge와 PR 병합·자동 병합은 수행하지 않으며 사용자 개별 병합 승인 대기를 유지한다.

별도 Astra가 이 후속 변경을 읽기 전용으로 검토해 원래 Git blob과 보관 사본의 바이트 일치, `.backups` 제외 규칙, 두 문서·두 삭제 파일의 범위, 깨진 파일 링크·실행 안내 제거를 확인했다. catalog와 제품 코드, 과거 검증 결과는 바꾸지 않았다. 새 검증 파일을 만들거나 기존 검토 스크립트·제품 테스트를 실행하지 않았다.

## 근거와 검증 한계

- [이전 구현·병합과 후속 보류](../2026-09-30-system-records/goal.md), [공동조회·로그 합의](../2026-09-30-system-records/shared-read-agreements.md).
- [PR147](https://github.com/bass131/dawnholder-server/pull/147)은 `gh pr view`로 MERGED, 병합 commit `715bff5bfc62ab5c1f1cdf0aabdf7358492bb1d3`, 2026-09-30 10:41:59 UTC를 확인했다. Game Dev 의미검토 PASS는 이미 main의 이전 goal에 기록돼 있다.
- 기존 Vitest·배치 fixture·Electron 실행은 당시 검증 기록이다. 이번 작업에서 다시 수행한 결과로 보고하지 않는다.
- PR151 본문에도 dotnet-tests CI의 05 npm test/build 비실행과 실제 launcher 더블클릭·START detached 경로 미검증을 명시했다. 원격 CI의 현재 결과는 PR에서 확인하며, CI 성공이 Management 제품 실행 검증을 대신하지 않는다.
- 실제 launcher 더블클릭·START detached 경로, 서버·SQL·Unity·MCP, 최신 게임 상태·catalog 자동 갱신을 검증하지 않는다. 원시 로그·이미지는 기존 로컬 전용 범위를 유지한다.

## 남은 결정

catalog 갱신 주기·책임의 구체 정책, 05 CI 추가, 원시 근거 공유, 실제 launcher 실행 검증과 새 기능 우선순위 논의는 이번 정정의 완료로 처리하지 않는다. 이미 기록된 후속 합의와 그 구현 착수 보류를 보존하며, 새 결정은 사용자가 정한 뒤 별도 범위로 반영한다.
