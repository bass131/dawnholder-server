# Dawnholder Management

정본과 작업 루트는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management`다. Game Dev와 별도 worktree에서 관리한다. 과거 목표·검증 문서의 이전 경로는 당시 관찰 근거이므로 보존한다.

라이브 서버 운영, 유저/GM 관리, 개발 현황을 사람과 에이전트가 같은 근거로 확인하기 위한 관리 영역이다.

## 문서 지도

- [현재 목표 — 운영툴 기록 원본 일원화와 역할 정리](goals/2026-10-06-record-source-unification/goal.md): 승인 범위·적용 중인 결정·PR 경계·진척의 원본.
- [개발 기록 탐색 목표](goals/2026-10-05-development-record-navigation/goal.md): 목록과 전체 페이지 상세의 범위·검증·병합 결과(PR189·PR194, 종료).
- [시스템 카드와 구현 설명 목표](goals/2026-10-02-system-cards/goal.md): 대표 도식 확인 관문과 첫 PR의 범위·결과(PR179·PR186, 종료).
- [운영 규칙 적용 목표](goals/2026-10-01-routing-adoption/goal.md): 계층형 라우팅 적용과 브랜치 정리(PR156, 종료).
- [공동 조회 MCP 목표](goals/2026-10-01-shared-read-mcp/goal.md): 기존 읽기 전용 공동 조회의 범위·계약·검증 근거(PR159·PR161, 종료).
- [공동 조회 MCP 사용 안내](MCP.md): 빌드·조회 순서·버전 처리와 실제 세션 연결 전 검토할 설정 예시.
- [이전 세션 종료 정리 목표](goals/2026-10-01-session-closeout/goal.md).
- [문맥 정정 목표](goals/2026-10-01-context-corrections/goal.md): 사실·상태·출처 정정과 독립 검토·병합 결과의 원본.
- [다음 세션 재개](RESUME.md): 최소 읽기 순서와 다음 행동.
- [요구사항](requirements.md): 확정 요구, 설계 제안, 미결정, 조사 관찰과 향후 완료조건의 원본.
- [결정](decisions.md): 확정된 선택과 이유, 변경된 방향을 요구사항 ID로 연결.
- [개발기록 재구성 목표와 현재 상태](goals/2026-09-30-system-records/goal.md): 재배치 범위·공동 협의·구현·검증·병합과 후속 보류 상태의 원본.
- [공동 조회·로그 확정 합의](goals/2026-09-30-system-records/shared-read-agreements.md): 다음 작업의 경계·완료조건·미결정. 구현 실적은 목표와 구분한다.
- [재배치 대응표](goals/2026-09-30-system-records/migration-map.md): 기존 문서와 시스템/기록의 연결, 원문 유지와 후속 범위.
- [기록 색인](records/catalog.json): 앱이 읽는 원문 색인. 시스템·기록·출처의 ID·제목·연결과 병합 PR만 담고, 서술과 상태는 출처가 가리키는 프로젝트 문서에 있다([D-16](decisions.md#d-16)). 사람과 에이전트가 파일로도 읽을 수 있다.
- [시스템 카드 자료](records/system-guide.json): 시스템 카드와 구현 설명 문서·도식의 원문. 다른 문서의 사본이 아니다.
- [실행 배치 목표](goals/2026-09-30-launcher/goal.md): 실행 배치와 기본 창 크기 변경 범위·당시 결과 원본.
- [콘솔 재정비 목표](goals/2026-09-30-console-refinement/goal.md): 화면 정보 위계·밀도 개선의 범위·상태·결과 원본.
- [데스크톱 창 목표](goals/2026-09-30-desktop-shell/goal.md): Electron·트레이·화면 개선의 범위·상태·검증 결과·미실행 원본.
- [초기화 목표](goals/2026-09-30-foundation/goal.md): 기존 화면 기반과 정본 경로 검증 결과.

## 간편 실행

[Start-Management.bat](Start-Management.bat)를 더블클릭한다. 다른 위치에서 실행해도 frontend 경로를 찾고, 최신 UI/main 빌드 성공 후 로컬 Electron 창을 연다. 성공하면 배치 콘솔은 닫히고 앱은 남는다. 이미 열린 앱과 별도 창이 중복 실행될 수 있다.

위는 구현된 실행 경로의 사용 안내다. 배치 분기 검증의 성공 START는 stub이며, **사람의 실제 더블클릭과 START detached 실행의 끝까지 이어지는 성공은 미검증**이다. 따라서 [launcher 목표](goals/2026-09-30-launcher/goal.md)의 사용자 진입점 완료조건을 모두 검증했다고 표시하지 않는다. 이후 실제 Electron main의 1280×720 창은 [system-records 검증](goals/2026-09-30-system-records/verification.md#실제-electron-관찰)에서 확인했지만 배치 경로 성공을 입증하지 않는다.

Node/npm, 로컬 의존성 또는 Electron 바이너리가 없으면 자동 설치/다운로드 없이 오류를 표시하고 키 입력 후 실패로 종료한다. 사전 준비는 frontend에서 `npm ci`를 실행한 뒤 `node node_modules/electron/install.js`로 공식 Electron 바이너리를 캐시에서 풀거나 다운로드하는 것이다. 현재 고정 버전은 `npm ci`만으로 바이너리를 준비하지 않으며, 다시 `npm ci`를 실행하면 기존 바이너리도 지워지므로 설치 명령을 다시 실행해야 한다. 빌드 실패 시 이전 빌드를 대신 띄우지 않는다. 현재 첫 창의 내용 영역은 1600×900 DIP(`useContentSize`)이고 화면 배율은 125%다([R-16](requirements.md#r-16), `frontend/electron/main.ts`). Windows 100%에서는 CSS 1280×720 구성이 된다. 이전 기본값 1280×720은 launcher 목표 당시의 기록이다.

## 프런트엔드 개발

`frontend`는 Electron 독립 창과 브라우저 개발 화면의 기반이다. WSL ASP.NET Core 관리백엔드는 채택한 후속 기술이며 백엔드/운영 연동은 이후 목표다. 아래 명령은 frontend에서 실행한다.

```powershell
Set-Location 'C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management/frontend'
npm ci
node node_modules/electron/install.js
npm run dev
npm run typecheck
npm run build
npm run preview
npm test
npm run records:check
npm run desktop:start
```

Node 지원 범위는 [package.json](frontend/package.json)의 engines를 따른다. 의존성은 lockfile로 고정한다. dev는 `127.0.0.1:5173`, preview는 `127.0.0.1:4173`만 사용하며 포트 충돌 시 실패한다. 다른 프로세스를 종료하지 않는다.

실제 운영툴(Electron)을 띄우는 E2E·스크린샷·화면 확인은 보조 디스플레이에서 수행한다. 실행마다 앱 내부 `screen` API로 주/보조 디스플레이의 이름·id, bounds/workArea, scaleFactor를 다시 확인하고, 가능하면 처음 표시 전에 창을 배치한다. 실행 근거에 표시된 디스플레이 이름과 실제 창 bounds를 기록하고, 창이 보조 workArea에 완전히 들어갔는지도 기록한다. Electron의 DIP 좌표·물리 px·OS 배율과 앱 zoom을 구분하며, 당일 좌표를 고정 설정으로 쓰거나 과거 실행의 준수를 소급하지 않는다.

보조 디스플레이가 없거나 요청한 창이 그 안에 들어가지 않으면 임의 축소·배율 변경·주 모니터 대체 없이 중지하고 파트 리드에게 보고한다. OS 수준 합성 마우스·키보드·computer use와 전면화는 금지하며, 앱 내부 창 배치·관측·캡처 수단을 사용한다. 실행 하네스에 위치 지정 방법을 두고, 제품 코드 변경이 필요하면 먼저 파트 리드를 통해 메인에 범위 판단을 요청한다.

저장소의 CI 워크플로는 [code-rules](../.github/workflows/code-rules.yml), [module-boundaries](../.github/workflows/module-boundaries.yml), [architecture-tests](../.github/workflows/architecture-tests.yml), [dotnet-tests](../.github/workflows/dotnet-tests.yml) 네 개다. code-rules는 Management lockfile을 설치 스크립트 없이 복원하지만 네 워크플로 모두 `05_Management`의 vitest(`npm test`)와 build는 실행하지 않는다. CI 성공과 위 로컬 Management 검증 실적은 별개다. 운영툴 시험의 CI 편입은 다음 계획(규칙·운영 미반영 정리의 묶음 2) 범위다.

`desktop:start`는 UI/main TS를 빌드해 로컬 정적 화면을 Electron 창으로 연다. 첫 실행에는 공식 Electron release 바이너리 다운로드가 필요할 수 있다. X 닫기는 트레이에 숨기고, 트레이 열기는 창을 복원하며, 종료는 이 앱만 닫는다. 장애 알림·서버/백엔드는 아직 연결하지 않는다. production CSP는 build에만 적용하며 Vite 개발/HMR에는 삽입하지 않는다.

서버 운영·유저 관리는 미연결 상태다. 「개발 기록」 탭은 기록 색인을 읽기 전용으로 보여 주며 실시간 PR·CI·게임 상태가 아니다. 목록은 시스템(분야·제목), 변경·결정·검증·계획 기록(종류·제목), 출처의 세 가지이고 항목을 고르면 전체 페이지 상세가 열린다. 상세의 관련 시스템·연결 기록으로 들어갔다가 뒤로 돌아오면 이전 화면과 검색·필터·목록 위치를 되찾는다.

상세의 「원문」 목록에서 「<출처 제목> 원문 읽기」를 누르면 지금 실행 중인 checkout의 Markdown 구간을 앱 안에서 읽는다. Markdown은 HTML로 바꾸지 않고 글자 그대로 보인다. 로컬 전용 자료, 전달 메시지, Markdown이 아닌 파일은 「앱에서 읽지 않음」과 경로·ID만 보인다. 파일이 없거나 구간·크기·경로가 맞지 않으면 「끊긴 링크」로 표시하고 이전에 읽은 본문을 보이지 않는다. 화면에는 읽은 checkout의 branch와 HEAD 앞 12자리, 기록 색인 버전(파일 내용 SHA-256 앞 12자리)이 보인다. 자동 갱신은 없고 「기록 새로고침」으로 다시 읽는다. 화면에서 임의 파일 실행이나 외부 이동을 제공하지 않는다. 경로 거절 규칙과 상한은 [색인 v2 설계](goals/2026-10-06-record-source-unification/index-v2-design.md)의 「원문 구간 읽기 경계」를 따른다.

개발 기록 자료는 `records/catalog.json`, 시스템 카드 자료는 `records/system-guide.json`으로 나뉘어 있다. [D-16](decisions.md#d-16)에 따라 기록의 원본은 프로젝트 문서이고 catalog는 원문 색인이다. 색인 전환으로 수기 `revision`·`asOf`·`sourceCommit`·`scopeNote`와 서술·상태 필드를 없앴고, 색인 버전은 파일 내용의 SHA-256이다. 전환 결과는 [기록 원본 일원화 목표](goals/2026-10-06-record-source-unification/goal.md)의 「PR2 데이터 전환」을 따른다.

앱은 두 파일을 읽기만 하고 편집·불러오기·저장은 없다. 파일 읽기는 Electron 창에서만 제공하며 브라우저 개발 화면에는 파일 연결이 없어 미연결 안내를 표시한다. 색인을 바꾸려면 파일을 직접 고친 뒤 frontend에서 `npm run records:check`로 검사하고 앱의 「기록 새로고침」으로 다시 읽는다. 기록 내용 변경에는 앱 재빌드가 필요하지 않다. 예전 저장 기능이 남긴 `.verification/system-records-last-good.json`은 사용자 산출물로 그대로 두며 앱은 더 쓰지 않는다.

`npm run records:check`는 앱과 같은 원문 읽기 모듈로 색인의 형식·참조와 git 출처의 경로·파일·구간을 검사한다. 색인에 없는 goal과 BACKLOG 표의 ID·goal 링크 어긋남은 warning으로 알린다. 진단마다 원인·위치·고치는 방법을 한 줄로 내고, 마지막 줄에 묶음별 실행 상태와 error·warning 수를 낸다. 종료 코드는 파일을 읽지 못한 묶음이 있으면 2, error가 있으면 1, 아니면 0이다. warning은 시범 단계이며 CI에는 아직 연결하지 않았다. 진단 코드는 [색인 v2 설계](goals/2026-10-06-record-source-unification/index-v2-design.md)의 「색인 검사」에 있다.

별도 DB는 없다. 읽기 전용 MCP와 공통 조회 모듈의 구현·검증 상태는 [공동 조회 목표](goals/2026-10-01-shared-read-mcp/goal.md)를 따른다. Three.js·Git/서버 자동 갱신·서버 제어·유저 변경·운영 API는 후속 범위다. Game Dev 규칙·기술 계약·진행 goal/CURRENT와 과거 근거는 정본 소유권을 유지한다. 기존 테스트와 실제 화면 검증 결과는 [개발기록 재구성 목표](goals/2026-09-30-system-records/goal.md)에서 확인한다.

프로젝트 공통 권한·역할은 [AGENTS](../AGENTS.md), 실행 부작용은 [개발 안내](../00_Document/operations/DEVELOPMENT.md)를 따른다. Management 상세 맥락은 이 영역에서 관리하고 기존 Game Dev 메인과 범위·소유권을 조율한다.
