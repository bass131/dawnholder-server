# Electron 셸 독립 검증

2026-09-30. 정본 `C:/Dev/DawnHolder_Project/05_Management`의 미커밋 변경을 검증했다. 지정 모델 `gpt-6-astra`, 확인된 실제 런타임 모델 `unknown`. 목표·상태·최종 결과의 원본은 [goal.md](goal.md)다.

## 판정과 실행 범위

Electron main 계약 6개와 기존 UI 계약 3개, strict 타입검사, 최종 desktop build, 실제 Windows Electron의 렌더·격리·탐색·창/트레이 수명이 통과했다. 이 범위에서 수정이 필요한 제품 결함은 발견하지 않았다. 디자인의 마지막 `color-scheme: dark` 변경 후 다시 빌드하고 최종 이미지를 갱신했다.

독립 테스트는 [desktop-main.test.ts](../../frontend/tests/desktop-main.test.ts)에 작성했다. 기존 `App.test.tsx`는 수정하지 않고 실행했다. 테스트 타입검사용 [tests/tsconfig.json](../../frontend/tests/tsconfig.json)은 product main 출력에서 분리했다.

| 검증 | 결과 | 근거 |
|---|---|---|
| `npm test` | 2파일/9테스트 통과: UI 3 + main 6 | [tests](../../.verification/desktop-independent-all-tests.log) |
| `npm run desktop:typecheck` | strict main 타입검사 exit 0 | [main typecheck](../../.verification/desktop-independent-main-typecheck.log) |
| `node node_modules/typescript/bin/tsc --project tests/tsconfig.json --noEmit` | 독립 테스트 strict 타입검사 exit 0 | [test typecheck](../../.verification/desktop-independent-test-typecheck.log) |
| `npm run desktop:build` | strict UI 타입검사 + Vite production + main NodeNext compile exit 0 | [build](../../.verification/desktop-independent-build.log) |
| Electron 의존성/lock 대조 | 선언·root lock·설치항목 `44.5.0` 일치 | [lock](../../.verification/desktop-independent-lock.json) |
| 실제 실행/자원 정리 | 최종 PID `21376`, exit 0, 소유 프로세스 잔존 0, 관측 리스너 0 | [process result](../../.verification/desktop-independent-process-result.json), [cleanup](../../.verification/desktop-independent-final-cleanup.json) |

main 테스트는 실제 Electron host를 모의해 격리 옵션/권한 거부, navigation/redirect/subframe/window.open 차단, X의 close 경로에서 hide/앱 생존, 같은 창 복원, 명시 quit 시 close 차단 해제와 tray 자원 정리, PNG 아이콘 및 아이콘/로컬 bundle 로드 실패를 검증한다. 실패경로 테스트를 제품 실제 실패로 해석하지 않는다.

## 실제 Electron 관찰

추가 browser 설치나 CDP/Node 디버그 포트를 사용하지 않았다. [검증 전용 하니스](../../.verification/desktop-independent-runtime.mjs)가 컴파일된 product main을 import해 실제 `BrowserWindow`와 `Tray`를 관찰했다. 제품 소스/스크립트/renderer IPC를 수정하지 않았으며 하니스의 main 권한은 제품 renderer 권한이 아니다.

- Windows에서 독립 visible 창과 정본 `frontend/dist/index.html`의 `file:` URL 로드를 확인했다. 외부 브라우저나 Vite 서버가 필요하지 않았다.
- 실제 `getLastWebPreferences()`에서 `contextIsolation/sandbox/webSecurity=true`, `nodeIntegration/webviewTag=false`, preload 부재를 확인했다. renderer의 `require/process/ipcRenderer/electron`은 모두 `undefined`였다.
- production CSP의 `connect-src 'none'`, 상대 asset 경로와 로컬 bundle 로드를 확인했다. 임의 `window.open`은 null, 외부 navigation 시도 뒤에도 같은 local URL/한 창을 유지했다.
- 하니스는 HTTP 등 네트워크 시도를 기록하고 실제 전송 전에 취소하도록 관찰 경계를 뒀다. 관측된 네트워크 요청 시도는 0이며, 테스트를 위해서도 외부/실제 관리서버와 통신하지 않았다. 전체 OS 트래픽 캡처 검증은 아니다.
- 실제 native `Tray` 생성과 열기/종료 메뉴 등록을 확인했다. `BrowserWindow.close()` 후 같은 창 ID의 `destroyed=false/visible=false`, tray 생존을 확인했다. 실제 등록된 열기 callback을 호출해 같은 창 `visible=true/focused=true`로 복원했다.
- 실제 종료 callback 뒤 정상 exit 0, 최종 `trayDestroyed=true/windowCount=0`이었다. 초기 will-quit 관찰자 시점에는 tray가 살아 있고 product 정리 handler 뒤에는 파괴되는 순서도 기록했다.
- runtime에서는 Return/char 키 이벤트로 세 영역을 전환하고, Tab 이동과 2px solid 초점 외곽선을 확인했다. 미연결·미수집 값과 disabled 제어를 보존했다.

근거: [runtime JSON](../../.verification/desktop-independent-runtime.json), [stdout](../../.verification/desktop-independent-runtime-stdout.log), [stderr](../../.verification/desktop-independent-runtime-stderr.log), [프로세스 소유](../../.verification/desktop-independent-owner.json), [소유 프로세스 트리](../../.verification/desktop-independent-processes.json), [리스너](../../.verification/desktop-independent-listeners.json).

이 검증은 실제 객체의 close/등록 callback 경로를 실행했다. Windows 트레이 아이콘을 마우스로 열고 메뉴를 누르는 OS UI 조작 검증은 아니다. 이전 provider의 `windowNotFound` 한계를 우회해 성공했다고 주장하지 않는다.

## 최종 화면

실제 Electron `webContents.capturePage()`의 renderer 내용 PNG다. Windows 제목줄·작업표시줄까지 포함한 데스크톱 전체 캡처는 아니다. 아래 이미지를 직접 확인했으며 메뉴/상태/카드의 가로 잘림과 가짜 정상·접속자 0·저장완료 표시가 없었다. 기본 창과 1280 폭에서는 내용 높이에 따른 세로 스크롤이 있으며 오류로 숨겨진 내용은 아니다.

- [기본 1040×800 창](../../.verification/desktop-independent-default-1040.png): Windows frame을 제외한 content `1024×761`.
- [1280×800 content](../../.verification/desktop-independent-1280.png), [960×800 content](../../.verification/desktop-independent-960.png).
- [유저 관리와 키보드 초점](../../.verification/desktop-independent-users-1280.png), [개발 현황과 키보드 초점](../../.verification/desktop-independent-development-1280.png).

검증한 폭에서 horizontal overflow가 없었고 미연결/미수집 설명과 비활성 제어가 분명했다. computed CSS로 관측한 표시 텍스트/불투명 배경의 최소 명암비는 약 `6.00:1`이었다. 비활성 제어·아이콘·모든 접근성 기준을 포함하는 인증 검사는 아니다.

## 정적 검토와 제한

dev/preview의 `127.0.0.1`, strictPort, cors=false 설정은 보존했다. 이번에 Vite dev/preview 서버를 별도로 재기동하지 않았으며 기존 리스너 관찰과 구분한다. `desktop-dist`, frontend profile 및 검증 로그는 [ignore](../../.verification/desktop-independent-ignore.log) 대상이다. [Git 상태](../../.verification/desktop-independent-scope.log)는 Management 범위만 읽고 다른 Game Dev 변경을 이 작업의 변경으로 해석하지 않았다.

Electron exe의 독립 관찰은 ProductVersion `44.5.0`, Authenticode `NotSigned`였다. [바이너리 SHA/서명 근거](../../.verification/desktop-independent-binary.json). 보안정책 변경 없이 해당 로컬 개발 실행이 성공한 사실만 확인했다. 최종 커스텀 EXE/설치프로그램의 서명·배포·SAC 신뢰를 보장하지 않는다. 새 npm ci/바이너리 다운로드는 반복하지 않았고 구현자 설치 이후의 lock/파일/빌드/실제 실행을 검증했다.

첫 하니스는 ESM 진입점의 top-level await 때문에 ready 대기에서 멈춰 소유 PID `7140`만 정리했다. 다음 시도의 단순 Enter 입력은 native character 이벤트가 없어 키보드 assertion에 실패했다. 비동기 진입과 Return/char 입력으로 하니스만 보완한 뒤 실제 실행이 통과했다. 이 두 검증 도구 문제를 제품 결함으로 판정하지 않았다. 관련 `desktop-independent-attempt1-*`, `attempt2-*` 근거를 보존했다.

production·기존 UI 테스트·공통 문서·브랜치·Git 원격·보안정책·기존 프로세스를 변경하지 않았다. 실제 장애/Windows 알림, WSL ASP.NET Core 백엔드, 게임서버·DB·저장·예약·복구·GM·실제 운영 API, 배포 패키징은 미실행이다. 원시 근거와 PNG는 정본 작업공간의 ignore된 `.verification`에 있다.
