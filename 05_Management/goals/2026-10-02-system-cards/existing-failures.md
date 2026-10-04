# 기존 suite 실패 분류 — 2026-10-05 재개

작성: Management Astra. 조사 기준 HEAD `719767f2276b14bace29ef98b621e191efef99e9`, 최초 작성 당시 문서 변경은 미커밋이었다. **독립 판정 전 분류 초안**이며 수정 승인이 아니다. 범위와 다음 결정은 [goal](goal.md#현재-재개-범위--2026-10-05)을 따른다.

기존 실패 12건의 직접 실패 원인은 **테스트 결함 10건, 환경 2건**으로 분류한다. 이 12건에서 제품 결함을 확정한 항목은 없지만, 중도 실패 뒤의 assertion까지 통과했다는 뜻은 아니다. 제품·기존 테스트·canonical MCP 출력은 이번 조사에서 변경하지 않았다.

## 실제 실행과 근거

원시 루트 E는 저장소 로컬 `.backups/verification/2026-10-05-system-cards-close/`다(Git 제외, 다른 PC에서는 접근 불가). 신규 Sol `task_7db1243fd848` / `ctx_0b6a8639498b`가 실행했고, Astra가 reporter·문서 diff·현재 소스와 대조했다. 수집 작업 성공과 테스트 suite 성공을 구분한다.

| 실행 | 조건 | 원시 결과 |
|---|---|---|
| 이전 기준선 | `2026-10-03-system-cards-resume/bundle-review/cmd/all-run.reporter.json` | 864개 중 852 통과, 12 실패 |
| 이번 전체 실행 | Windows 11 Pro 10.0.26200 x64, Node v24.15.0, npm 11.13.0, Vitest 5.0.2; 2026-10-04 15:10:59.949~15:11:38.327 UTC | 864개 중 851 통과, 13 실패, exit 1. 기존 12개 file+fullName 모두 포함, 추가 timeout 1건 |
| 추가 timeout 파일 단독 | 설정·제품·테스트 무변경, `tests/diagram-loader-policy.test.ts` | 23/23 통과. 해당 테스트는 전체 실행 5006.3099ms, 파일 실행 344.2209ms |

실제 전체 재현 명령은 frontend에서 다음과 같다. TEMP/TMP는 소유 마커가 있는 E/sol/tmp에 한정해 실행하고 정리했다. 정확한 argv·환경·시각·exit는 `E/sol/all-run.meta.json`, stdout/stderr와 reporter는 같은 stem의 파일에 있다.

```powershell
npm test -- --reporter=json --outputFile=C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/.backups/verification/2026-10-05-system-cards-close/sol/all-run.reporter.json
```

과거 reporter SHA256은 `a39df050d9f18f28fb32901057b33bc519a380308715af8c70722bc61b388b80`, 이번 reporter는 `9473d578bf8ad3e3d8c8faf91cd56f90c5659524c59933a20ab757011075519c`다. 개별 원시는 `E/sol/facts.json`의 `baselineFailures`와 `E/sol/failures/B01.txt`~`B12.txt`에 고정했다. `E/sol/baseline-comparison.json`은 정확한 이름 대조, `E/sol/build-observation.json`은 빌드 입력·출력 비교, `E/sol-astra-audit.json`은 Astra 원천 대조다.

## 실패별 분류

아래 명령의 cwd는 `05_Management/frontend`다. 각 항목은 위 **전체 실행에서 실제 재현**됐다. 표의 `-t` 명령은 해당 항목만 좁혀 재현하기 위한 복사용 명령이며, 이번 Sol이 12개를 각각 단독 실행했다는 뜻은 아니다.

| 번호 | 실패 대상 | 분류 | 직접 원인과 판단 근거 | 항목별 재현 명령 |
|---|---|---|---|---|
| B01 | App: 운영 명령·외부 호출 금지 | 테스트 결함 | `App.test.tsx:89`가 모든 link 부재를 기대해 `App.tsx:68`의 본문 이동용 `#management-section` 링크를 거부한다. R-02의 외부 관리 접근 차단과 내부 접근성 이동은 다르다. 원시 B01. | `npm test -- src/App.test.tsx -t "cannot issue operating commands"` |
| B02 | 개발 기록: 읽기·검색·키보드 진입 | 테스트 결함 | 공통 `enter()`가 개발 현황 진입 직후 기록 검색창을 찾는다. 현재 기본은 시스템 카드이며 기록은 별도 탭이다(`App.tsx:64,87-89`, `DevelopmentRecords.test.tsx:24-26`). 기록 보존 요구를 없앤 것이 아니라 테스트의 진입 절차가 현재 UI와 다르다. 원시 B02. | `npm test -- src/DevelopmentRecords.test.tsx -t "loads through the runtime bridge"` |
| B03 | 개발 기록: 관련 시스템·고정 근거 탐색 | 테스트 결함 | B02와 같은 공통 `enter()`에서 중단한다. 이 실행은 후속 탐색 assertion에 도달하지 않았다. 원시 B03. | `npm test -- src/DevelopmentRecords.test.tsx -t "navigates shared records"` |
| B04 | 편집: 영역 전환 초안 유지·명시 저장 | 테스트 결함 | `editor()`가 같은 `enter()`에 의존한다. 기록 탭을 열기 전 검색창 대기에서 실패했으며 초안/저장 판정은 미도달이다. 원시 B04. | `npm test -- src/DevelopmentRecords.test.tsx -t "retains a draft across area switches"` |
| B05 | 편집: 다른 작성자 변경과 초안 보존 | 테스트 결함 | 동일한 진입 helper 실패다. 원본 버전·동시 변경 보존의 제품 성공/실패까지 이번 결과로 판단하지 않는다. 원시 B05. | `npm test -- src/DevelopmentRecords.test.tsx -t "preserves an unsaved draft"` |
| B06 | 편집: 잘못된 초안·쓰기 실패 보존 | 테스트 결함 | 동일한 진입 helper 실패로 유효성/쓰기 오류 assertion에 도달하지 않았다. 원시 B06. | `npm test -- src/DevelopmentRecords.test.tsx -t "rejects malformed and dangling drafts"` |
| B07 | 편집: 초안 불러오기·실패 뒤 화면 보존 | 테스트 결함 | 동일한 진입 helper 실패로 불러오기/재읽기 후 보존 assertion에 도달하지 않았다. 원시 B07. | `npm test -- src/DevelopmentRecords.test.tsx -t "imports only a draft"` |
| B08 | 개발 기록: 파일 연결 없음 안내 | 테스트 결함 | `DevelopmentRecords.test.tsx:137-142`는 기록 탭을 열지 않고 숨겨진 기록 안내의 가시성을 검사한다. `getByText`는 요소를 찾지만 `toBeVisible`이 실패한다. `App.tsx:87-89`의 탭 경계와 `DevelopmentRecords.tsx`의 안내를 대조했다. 원시 B08. | `npm test -- src/DevelopmentRecords.test.tsx -t "reports missing runtime authority"` |
| B09 | 창 생성: 1280×720 외곽 크기 | 테스트 결함 | `desktop-main.test.ts:71-76`의 이전 외곽 1280×720 기대와 새 R-16/goal의 내용 1600×900 DIP·125%가 다르다. 실제 main은 `useContentSize:true`다. R-16 전체 복원/저장 기능의 완료 판정은 아니다. 원시 B09. | `npm test -- tests/desktop-main.test.ts -t "uses the agreed 1280 by 720"` |
| B10 | IPC: 허용 main frame의 catalog 접근 | 테스트 결함 | `desktop-main.test.ts:95`가 IPC 목록을 두 records 채널로 고정한다. R-14의 고정 guide 조회를 위해 `system-guide:read`가 추가됐으며 `main.ts:25-36`에서 같은 `trustedSender` 검사를 거친다. sender 거부 동작은 이 실패 테스트의 후속 assertion이므로 미도달이다. 원시 B10. | `npm test -- tests/desktop-main.test.ts -t "allows only the owned main frame"` |
| B11 | MCP: 같은 소스의 빌드 digest | 환경 | 현재 소스로 만든 임시 출력 `85d3abc8…`을 기존 ignored 출력 `fd60b03a…`과 비교한다. 현재 canonical 소스와 임시 소스 입력 19개가 같고 출력 차이는 `mcp/build-info.js`뿐이다. 비교 기준 출력이 현재 소스 빌드와 맞지 않는 환경으로 분류한다. 원래 canonical 입력 집합은 미상이며, 누가 언제 무엇을 바꿨는지는 확정하지 않는다. 원시 B11 및 build-observation. | `npm test -- tests/mcp-v3-build.test.ts -t "build identity is content based"` |
| B12 | MCP: 한글·공백 경로의 digest | 환경 | 임시 경로끼리의 버전 일치 뒤 기존 canonical 출력과 비교하는 지점(`mcp-v3-r1-build.test.ts:100`)에서 실패한다. 현재 측정은 B11과 같은 기준 출력 불일치이며 한글·공백 경로 자체의 실패 근거는 없다. 원시 B12 및 build-observation. | `npm test -- tests/mcp-v3-r1-build.test.ts -t "the same sources under a root"` |

## 추가 관찰 A01 — 도식 빌드 시험 timeout

`tests/diagram-loader-policy.test.ts:133`의 `product renderer graph with the policy keeps the allowed loader modules and replaces every other registered loader before resolution`이 전체 실행에서 5000ms 한도를 넘겼다. 같은 설정의 파일 실행은 통과했으므로 **전체 실행에서의 timeout 관찰**로 남긴다. 환경 부하와 테스트 시간 예산 중 정확한 인과는 아직 확정하지 않는다. 기존 12건에 섞거나 해결 완료로 세지 않는다.

실제 제한 실행 명령은 `npm test -- tests/diagram-loader-policy.test.ts --reporter=json --outputFile=<E/sol/diagram-file.reporter.json의 절대 경로>`다. 정확한 argv는 `E/sol/diagram-file.meta.json`, 원시는 reporter와 `failures/A01.txt`다. 제품·설정·timeout을 바꾸지 않았으며 전체 suite를 반복해 통과 결과를 고르지 않았다. 신규 Opus가 영향과 차단 여부를 독립 판정한다.

## 한계와 결정 경계

- B01~B10은 직접 실패 지점의 분류다. 이를 고친 뒤 추가 실패가 없다고 보장하지 않으며, 중도 실패 뒤 저장·충돌·IPC 거부 등의 assertion은 이번 실행에서 검증되지 않았다.
- B11~B12는 현재 출력 불일치 관찰이다. 기존 빌드 helper와 같은 digest 공식을 계산한 것은 독립적인 빌드 알고리즘 정확성 증명이 아니다. canonical 출력은 보존했다.
- 이번 단위는 기존 실패를 분류하고 SAC 문서만 정정했다. 12건의 수정 여부는 메인·사용자가 결정한다. 사용자 물리 125% 화면, .NET/WSL·DB·Unity·게임, 배포본과 원격 CI는 이번 조사에서 실행하지 않았다.
- 신규 Opus 판정과 원문 경로는 아직 없다. 독립 실사 후 goal에 최종 판정·추가 한계와 연결하며 이 초안을 독립 통과로 인용하지 않는다.
