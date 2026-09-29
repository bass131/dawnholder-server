# 정리 후 남은 Legacy 파일과 디렉터리 정리

상태: Legacy 정리·독립 검증·[PR129 발행](https://github.com/bass131/dawnholder-server/pull/129) 완료. 사용자가 PR128·PR127·PR129 병합을 명시 승인했다. PR128·PR127 병합 후 최신 main을 반영하고 PR129의 최종 CI를 확인하는 단계이며, 이 기록 시점에는 PR129를 아직 병합하지 않았다.

## 목표와 범위

- 앞선 문서 정리 후 남은 파일·디렉터리에서 현재 참조와 용도가 없는 Legacy 후보를 확인한다.
- 후보마다 현재 사용 여부, 보존 근거, 삭제 영향과 확인 방법을 기록하고 확정된 항목만 정리한다.
- 파일 이름이나 빈 디렉터리처럼 보인다는 이유만으로 삭제하지 않는다. 승인된 설정·상태 14개만 활성 위치에서 제거하고 로컬 백업에 원본 바이트를 보존한다. 그 밖의 사용자 원본·설정·로그·Unity 메타데이터와 별도 작업 공간은 보호한다.
- 구현자와 분리한 검증자가 삭제 범위·참조·보존 상태를 확인한 뒤 종속 PR을 만든다. 병합은 각 PR에 대한 사용자 명시 승인 전까지 진행하지 않는다.

## 시작점과 PR 의존 관계

- 브랜치: `chore/legacy-cleanup`.
- 출발 커밋: `bc5b2d6f61da482e879197f748db39681c4efc95`, 문서 정리 브랜치 `docs/ai-readiness-docs`의 검증된 결과.
- [PR128](https://github.com/bass131/dawnholder-server/pull/128)은 시작 시점 OPEN·미병합이며 해당 커밋의 CI가 통과했다. [앞선 목표](../2026-09-29-ai-readiness-docs/goal.md)의 정리 결과에 의존하므로 메인이 이번 목표에 한해 최신 main 출발 원칙의 예외를 승인했다.
- cleanup PR은 우선 `docs/ai-readiness-docs`를 base로 만들어 정리 변경만 검토한다. 이 상태에서 병합하면 문서 브랜치가 바뀌므로 병합하지 않는다.
- PR128이 사용자 승인 후 병합되면 최신 main을 cleanup 브랜치에 정상 merge하고 충돌·변경 범위를 다시 검토한다. 이후 cleanup PR의 base를 main으로 바꾼다. 강제 push나 이력 재작성으로 의존 관계를 숨기지 않는다.
- cleanup PR의 병합 직전에도 별도 사용자 명시 승인이 필요하다. 이전 승인·CI 통과·메인의 판단으로 대신하거나 자동 병합을 예약하지 않는다.

## 보호 대상과 제외 범위

- main `bf93c66c8ac31b8b16192c9aab5af1da25b94673`, 고정 보관 `archive/claude-setup-2026-09-29`의 `f0f23f781dc49d8bf67c0019b948696c36ed6c00`, 원격 문서 브랜치 `bc5b2d6f61da482e879197f748db39681c4efc95`를 변경하지 않는다.
- `.backups/`와 `.claude/settings.local.json`은 시작 검사에서 Git ignored로 확인했다. 메인이 승인한 개인 설정 1개와 옛 상태·로그 13개는 새 전용 백업에 바이트·해시를 대조해 보존한 뒤 활성 위치에서 제거했다. 기존 백업은 수정하지 않으며 개인 내용과 백업 원본은 커밋하지 않는다.
- 사용자 원본 `03_Client/Assets/Resources/MinimapRT.renderTexture`의 시작 SHA-256은 `7123AF77311397F6AF85F1BDFD44DA20B46FB153C3DA274EE9D29E5B9D39F85F`다. Unity `.meta`·GUID·직렬화 값도 보존한다.
- 별도 DB [PR127](https://github.com/bass131/dawnholder-server/pull/127), `bass131/feat-mssql-game-schema` 브랜치와 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/feat-mssql-game-schema` 작업 공간은 건드리지 않는다.
- 과거 Git worktree의 orphan/prunable 메타데이터 정리, 원격 브랜치 삭제, DB·서버 설정 변경은 이번 범위에서 제외한다.

## 완료조건

- [x] 안전한 출발 커밋을 확인하고 별도 브랜치·목표·CURRENT를 구성했다.
- [x] 삭제 후보의 사용 여부·근거·영향을 조사하고 메인이 구현 범위를 확정했다.
- [x] 확정한 항목만 정리하고 필요한 참조를 수정했다.
- [x] 독립 검증으로 삭제 범위·남은 참조·보호 대상 보존을 확인했다.
- [x] 변경과 검증 결과를 보고하고 종속 PR을 생성했다. 이후 사용자 병합 승인과 main 기준 전환을 확인했다.

## 현재 결과

시작 작업트리는 clean이었다. 로컬·원격 출발 커밋, main·archive 불변, 원본 Minimap 해시와 보호 경로의 ignored 상태를 확인한 뒤 별도 브랜치에서 다음 승인 범위만 구현했다.

| 변경 | 실제 결과 |
|---|---|
| 옛 보고서 CSS | `01_Phases/youngho/M4.1-combat-precision/LagComp_Report/style.css` 1개 삭제. 현재 참조가 없고 고정 원문과 줄바꿈 외 내용이 같음을 확인했다. |
| 편집기 추천 | `.vscode/extensions.json`의 `anthropic.claude-code` 추천 1개만 제거했다. 설치된 확장이나 다른 추천은 변경하지 않았다. |
| 문서 참조 주석 | PacketRoundTripTests·HandlerRegistry·SendBuffer·ProtocolVersion의 없어진 CLAUDE 문서 참조를 현재 protocol/server 계약과 ClientNet README로 교체했다. |
| 개인 설정·옛 상태 | 설정 1개와 상태·로그 13개, 총 68,934바이트를 상대 경로대로 백업·해시 검증 후 활성 위치에서 제거했다. root `.claude`, 문서·옛 Phase의 로그 2개, Assets 안의 정확한 로그 5개가 범위다. |
| 빈 디렉터리 | 승인된 빈 tree 39개와 정리 후 빈 부모를 포함해 실제 디렉터리 68개를 비재귀 방식으로 제거했다. 매 삭제 직전에 빈 상태·경계·reparse point·worktree 여부를 확인했다. `01_Phases/goals`와 Assets 주변 폴더는 유지했다. |

## 보존과 복구 근거

- 로컬 전용 백업: `.backups/legacy-cleanup-20260929-201525-456/`. `manifest.json`에 설정·상태 14개 각각의 원래 상대 경로·바이트·SHA-256·복사 위치와 CSS 복구 정보를 기록했다. `cleanup-result.json`에는 실제 제거한 디렉터리 목록을 기록했다. 원문 값과 비밀은 이 목표나 Git에 넣지 않았다.
- CSS 작업트리 원본도 같은 백업의 `preserved/style.css`에 보존했다. 설정·상태와 CSS를 합한 실제 원본 보존량은 15파일, 79,305바이트다.
- CSS의 [고정 원문](https://github.com/bass131/dawnholder-server/blob/59c7f087dc630df79650cedc3ede29765397bd8d/01_Phases/youngho/M4.1-combat-precision/LagComp_Report/style.css)은 Git blob `ff355d09cc8c512b085eb345b35a08dc32390877`, LF 10,018바이트, SHA-256 `33a805fe81789a12f47061fa0b7be9b9546aae74fb28ec464e9cd30bafae805f`다. Windows 작업 원본은 CRLF 10,371바이트, SHA-256 `b8987ae4f646dd5334f887e6bd873b583d9488a54a71861817e658efcd502556`이며 줄바꿈 정규화 후 내용이 일치한다.
- 과거 목표·제안 HTML·결과 HTML·영역별 아카이브·기술 ADR·현재 hook·실행 도구를 유지했다. Unity vendor 내부의 `.claude`처럼 승인 범위 밖의 동명 경로는 정리하지 않았다.

## 자체 검증

- 백업 15파일의 바이트·SHA-256 일치와 활성 설정·상태 14개의 제거를 확인했다.
- C# 네 파일은 변경된 모든 행이 `//` 주석이며, 전체 주석 행을 제외한 코드 내용이 변경 전과 같음을 확인했다. 새 문서 참조 대상은 존재한다.
- 확장 JSON은 기존 객체에서 추천 ID 1개만 뺀 값과 일치한다. `.meta`·Unity 직렬화 에셋·csproj·DLL·실행 스크립트 변경 0건, 원본 Minimap 해시 동일.
- 로컬 증거는 `%TEMP%/dawnholder-legacy-implementation-20260929/self-validation.json`에 있다. 게임·DB·Unity 실행 테스트는 수행하지 않았다. 이번 C# 수정은 주석만이므로 동작 불변 대조로 검증했다.

독립 최종 검토도 통과했다. 백업 15개, 사전 조사 로그 해시 13개, Assets 주변 폴더와 68개 삭제 경로, 주석 외 코드 불변, 보호 파일 및 기존 안전 백업의 원본 해시를 별도로 대조했다. 근거는 `%TEMP%/dawnholder-legacy-impact-20260929/final-independent-checks.json`이다. 개인 백업·ignored 파일 삭제·빈 디렉터리 정리는 로컬 작업이며 Git PR을 받는 다른 checkout에 자동 적용되지 않는다.

별도 DB 작업은 메인이 최종 점검과 `worker_done` 수신을 확인했다. DB 브랜치는 로컬·원격 `c810057`에서 clean이며, SQL 서비스와 DB는 실행 상태로 유지했다. 공식 release 뒤 사용자 명시 지시로 해당 터미널만 종료했다. 이 cleanup에서는 DB 결과·PR·서비스를 변경하지 않는다.
