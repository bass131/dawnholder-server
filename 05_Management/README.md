# Dawnholder Management

정본과 작업 루트는 `C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management`다. Game Dev와 별도 worktree에서 관리한다. 과거 목표·검증 문서의 이전 경로는 당시 관찰 근거이므로 보존한다.

라이브 서버 운영, 유저/GM 관리, 개발 현황을 사람과 에이전트가 같은 근거로 확인하기 위한 관리 영역이다.

## 문서 지도

- [현재 문맥 정정 목표](goals/2026-10-01-context-corrections/goal.md): 사실·상태·출처 정정과 독립 검토·PR 상태의 원본. 새 MCP·서버 구현 착수는 계속 보류한다.
- [다음 세션 재개](RESUME.md): 최소 읽기 순서와 보존된 checkpoint·새 문서 브랜치 안내.
- [요구사항](requirements.md): 확정 요구, 설계 제안, 미결정, 조사 관찰과 향후 완료조건의 원본.
- [결정](decisions.md): 확정된 선택과 이유, 변경된 방향을 요구사항 ID로 연결.
- [개발기록 재구성 목표와 현재 상태](goals/2026-09-30-system-records/goal.md): 재배치 범위·공동 협의·구현·검증·병합과 후속 보류 상태의 원본.
- [공동 조회·로그 확정 합의](goals/2026-09-30-system-records/shared-read-agreements.md): 다음 작업의 경계·완료조건·미결정. 구현 실적은 목표와 구분한다.
- [재배치 대응표](goals/2026-09-30-system-records/migration-map.md): 기존 문서와 시스템/기록의 연결, 원문 유지와 후속 범위.
- [시스템 기록 데이터](records/catalog.json): 앱이 읽는 시스템·변경·결정·검증·계획과 버전별 근거. 사람과 에이전트가 파일로도 읽을 수 있다.
- [실행 배치 목표](goals/2026-09-30-launcher/goal.md): 실행 배치와 기본 창 크기 변경 범위·당시 결과 원본.
- [콘솔 재정비 목표](goals/2026-09-30-console-refinement/goal.md): 화면 정보 위계·밀도 개선의 범위·상태·결과 원본.
- [데스크톱 창 목표](goals/2026-09-30-desktop-shell/goal.md): Electron·트레이·화면 개선의 범위·상태·검증 결과·미실행 원본.
- [초기화 목표](goals/2026-09-30-foundation/goal.md): 기존 화면 기반과 정본 경로 검증 결과.

## 간편 실행

[Start-Management.bat](Start-Management.bat)를 더블클릭한다. 다른 위치에서 실행해도 frontend 경로를 찾고, 최신 UI/main 빌드 성공 후 로컬 Electron 창을 연다. 성공하면 배치 콘솔은 닫히고 앱은 남는다. 이미 열린 앱과 별도 창이 중복 실행될 수 있다.

위는 구현된 실행 경로의 사용 안내다. 배치 분기 검증의 성공 START는 stub이며, **사람의 실제 더블클릭과 START detached 실행의 끝까지 이어지는 성공은 미검증**이다. 따라서 [launcher 목표](goals/2026-09-30-launcher/goal.md)의 사용자 진입점 완료조건을 모두 검증했다고 표시하지 않는다. 이후 실제 Electron main의 1280×720 창은 [system-records 검증](goals/2026-09-30-system-records/verification.md#실제-electron-관찰)에서 확인했지만 배치 경로 성공을 입증하지 않는다.

Node/npm, 로컬 의존성 또는 Electron 바이너리가 없으면 자동 설치/다운로드 없이 오류를 표시하고 키 입력 후 실패로 종료한다. 사전 준비는 아래 개발 명령의 `npm ci`, 바이너리만 준비하려면 frontend에서 `node node_modules/electron/install.js`를 직접 실행한다. 빌드 실패 시 이전 빌드를 대신 띄우지 않는다. 새 기본 창 크기 `1280×720`은 다음 실행부터 적용하며 현재 열린 창은 유지한다.

## 프런트엔드 개발

`frontend`는 Electron 독립 창과 브라우저 개발 화면의 기반이다. WSL ASP.NET Core 관리백엔드는 채택한 후속 기술이며 백엔드/운영 연동은 이후 목표다. 아래 명령은 frontend에서 실행한다.

```powershell
Set-Location 'C:/Users/bass1/orca/workspaces/DawnHolder_Project/management-active/05_Management/frontend'
npm ci
npm run dev
npm run typecheck
npm run build
npm run preview
npm test
npm run desktop:start
```

Node 지원 범위는 [package.json](frontend/package.json)의 engines를 따른다. 의존성은 lockfile로 고정한다. dev는 `127.0.0.1:5173`, preview는 `127.0.0.1:4173`만 사용하며 포트 충돌 시 실패한다. 다른 프로세스를 종료하지 않는다.

저장소의 [dotnet-tests CI](../.github/workflows/dotnet-tests.yml)는 .NET build/test만 실행하며 `05_Management`의 npm test/build는 실행하지 않는다. CI 성공과 위 로컬 Management 검증 실적은 별개다.

`desktop:start`는 UI/main TS를 빌드해 로컬 정적 화면을 Electron 창으로 연다. 첫 실행에는 공식 Electron release 바이너리 다운로드가 필요할 수 있다. X 닫기는 트레이에 숨기고, 트레이 열기는 창을 복원하며, 종료는 이 앱만 닫는다. 장애 알림·서버/백엔드는 아직 연결하지 않는다. production CSP는 build에만 적용하며 Vite 개발/HMR에는 삽입하지 않는다.

서버 운영·유저 관리는 미연결 상태다. 개발 현황의 시스템 기록은 기준일과 근거 버전이 있는 파일 기반 자료이며 실시간 PR·CI·게임 상태가 아니다. 검색과 상세 탐색으로 관련 기록과 출처를 확인한다. 로컬 전용 근거는 이 PC 밖에서 접근되지 않을 수 있으며, 화면에서 임의 파일 실행이나 외부 이동을 제공하지 않는다.

catalog revision `2026-10-01-r2`는 추적된 Management 문서 6개의 출처를 고정 공개 commit과 저장소 상대경로로 바꾼 것이다. `asOf`는 계속 `2026-09-30T09:05:09Z`이며 내용의 최신성이나 검증 상태를 갱신한 revision이 아니다. 상세 근거와 로컬 전용 자료의 경계는 [재배치 대응표](goals/2026-09-30-system-records/migration-map.md#출처-고정과-가용성)를 따른다.

기록 원본은 `records/catalog.json` 한 곳이다. 실행 중 새로고침으로 파일 변경을 읽고, 기록 편집에서 JSON 파일을 불러오거나 수정한 뒤 저장한다. 기록 내용 변경에는 앱 재빌드가 필요하지 않다. 연결된 ID·원문 버전·시점·검증 한계를 함께 유지한다. 기록을 외부에서 수정할 때는 앱의 미저장 편집과 겹치지 않도록 하고, 충돌이 표시되면 최신 내용을 확인한다.

파일 읽기·저장은 Electron 창에서 제공한다. 브라우저 개발 화면에는 파일 연결이 없어 미연결 안내를 표시한다. JSON 불러오기는 편집 초안만 바꾸며, `검증 후 기록 저장`을 눌러야 원본에 반영된다. 직전 정상본은 `.verification/system-records-last-good.json`에 복구용으로 보관한다. 같은 파일을 여러 앱에서 편집한 경우 이전 초안의 저장을 거부하므로 초안을 보존하고 최신본과 비교한다.

별도 DB/색인은 없으며 실제 MCP·Three.js·Git/서버 자동 갱신은 후속 범위다. Game Dev 규칙·기술 계약·진행 goal/CURRENT와 과거 근거는 정본 소유권을 유지한다. 실제 서버 제어/유저 변경/운영 API 호출은 없다. 테스트와 실제 화면 검증 결과는 현재 목표에서 확인한다.

프로젝트 공통 권한·역할은 [AGENTS](../AGENTS.md), 실행 부작용은 [개발 안내](../00_Document/operations/DEVELOPMENT.md)를 따른다. Management 상세 맥락은 이 영역에서 관리하고 기존 Game Dev 메인과 범위·소유권을 조율한다.
